
using System.Collections;
using System.Collections.Generic; 

using UnityEngine;

namespace Swole.API.Unity.Worlds
{
    public class TerrainPool : MonoBehaviour
    {
        [Header("Settings")]
        public Transform playerTransform;
        public int terrainLayer;

        [Header("Close Tier Settings")]
        [Tooltip("Prefab (with a Terrain component) instantiated for close/near chunks. Its TerrainData asset is cloned per instance.")]
        public GameObject closeTerrainPrefab;
        public int viewRadius = 2; // e.g., 2 means a 5x5 grid around the player
        public float chunkSize = 128f;
        [Tooltip("Heightmap resolution. Unity terrains require this to be a power of two plus one (e.g. 33, 65, 129, 257, 513, 1025, 2049).")]
        public int heightmapResolution = 513;

        [Header("Far Tier Settings")]
        [Tooltip("Prefab (with a Terrain component) instantiated for far/background chunks. Its TerrainData asset is cloned per instance.")]
        public GameObject farTerrainPrefab;
        [Tooltip("How many additional rings of chunks (beyond viewRadius) are covered by the far tier, at reduced resolution.")]
        public int farViewRadius = 2;
        [Tooltip("Heightmap resolution used for far tiles. Should generally be lower than the close tier's resolution. Unity terrains require this to be a power of two plus one (e.g. 33, 65, 129, 257, 513, 1025, 2049).")]
        public int farHeightmapResolution = 129;
        [Tooltip("Far-tier chunks (and the quantized edge band of close-tier chunks bordering them) are sampled this many meters lower. Since far-tier chunks change resolution as they shift LOD/ring position, this helps hide the resulting seam by making any residual gap read as a small recess rather than a visible crack/floating edge.")]
        public float farTierHeightBias = 0.5f;

        [Header("Adaptive Piecewise Streaming Settings")]
        [Tooltip("The slice count used for chunks close to the player. Lower = fewer frames to load completely = faster tracking.")]
        public int closeProgressiveSliceCount = 2;

        [Tooltip("The slice count used for distant chunks on the horizon. Higher = spreads physics out over more frames = smoother performance.")]
        public int distantProgressiveSliceCount = 16;

        [Tooltip("Chunks closer than this distance (in meters) will load fast using the close slice count.")]
        public float minDistanceThreshold = 100f;

        [Tooltip("Chunks farther than this distance (in meters) will trickle load using the distant slice count.")]
        public float maxDistanceThreshold = 600f;

        [Header("Altitude Acceleration Settings")]
        [Tooltip("How far out from the player should we sample ground heights to compute the average baseline?")]
        public float altitudeSampleRadius = 250f;

        [Tooltip("At or below this height above average ground (meters), loading speed scales purely by distance settings.")]
        public float minAltitudeThreshold = 10f;

        [Tooltip("At or above this height above average ground (meters), loading speeds are fully maxed out across the whole horizon.")]
        public float maxAltitudeThreshold = 300f;

        [Header("Loading Screen Optimization")]
        [Tooltip("When true, chunks bypass slicing logic entirely and process as unified blocks to clear loading screens as fast as possible.")]
        public bool loadInstantDuringLoadingScreen = false;

        [Header("Physics Bake Optimization")]
        [Tooltip("If a chunk is farther away than this distance, it will use DelayLOD to completely bypass all PhysX calculations until 100% finished.")]
        public float physicsDeferDistanceThreshold = 150f;

        /// <summary>
        /// Runtime state + static configuration for a single streaming tier (e.g. close chunks or far background tiles).
        /// </summary>
        private class TerrainTier
        {
            public GameObject prefab;
            public float tileSize;
            public int resolution;
            public int viewRadius;
            public bool isCloseTier;

            public Vector2Int currentCenterCoord = new Vector2Int(int.MaxValue, int.MaxValue);
            public Dictionary<Vector2Int, Terrain> activeTerrains = new Dictionary<Vector2Int, Terrain>();
            public Dictionary<Vector2Int, TerrainCollider> activeColliders = new Dictionary<Vector2Int, TerrainCollider>();
            public Queue<Terrain> terrainPool = new Queue<Terrain>();

            public List<Vector2Int> loadQueue = new List<Vector2Int>();
            public Dictionary<Vector2Int, int> chunkLoadingProgress = new Dictionary<Vector2Int, int>();

            // Tracks which edges (if any) were flagged as bordering the coarser tier the last time this
            // chunk's heights were generated, so a stale border quantization/bias can be detected and the
            // chunk re-queued for a full height refresh when its boundary role changes (e.g. the close grid
            // recenters and a chunk that used to be an edge chunk becomes interior, or vice versa).
            public Dictionary<Vector2Int, EdgeFlags> appliedEdgeFlags = new Dictionary<Vector2Int, EdgeFlags>();
        }

        private struct EdgeFlags
        {
            public bool west, east, south, north;

            public static EdgeFlags From(World.EdgeQuantization q) => new EdgeFlags { west = q.west, east = q.east, south = q.south, north = q.north };

            public bool Equals(EdgeFlags other) => west == other.west && east == other.east && south == other.south && north == other.north;
        }

        private TerrainTier closeTier;
        private TerrainTier farTier;
        private bool isProcessingQueue = false;

        // Cache variable to track current calculated vertical scale modifiers across coroutine iterations
        private float currentAltitudeSpeedMultiplier = 1f;

        /// <summary>
        /// Unity terrains require heightmap resolution to be (power of two) + 1 (e.g. 33, 65, 129, 257, 513,
        /// 1025, 2049). Any other value silently gets clamped by Unity in a way that can leave the last row/
        /// column of vertices undefined, producing a flat seam along that terrain's edge. This rounds the
        /// given value to the nearest valid resolution.
        /// </summary>
        private static int SnapToValidHeightmapResolution(int resolution)
        {
            const int Min = 33;
            const int Max = 4097;
            resolution = Mathf.Clamp(resolution, Min, Max);

            int candidate = resolution - 1;
            int lowerPow2 = Mathf.NextPowerOfTwo(candidate);
            if (lowerPow2 > candidate) lowerPow2 /= 2;
            if (lowerPow2 < 1) lowerPow2 = 1;
            int higherPow2 = lowerPow2 * 2;

            int lowerRes = lowerPow2 + 1;
            int higherRes = higherPow2 + 1;

            int snapped = (resolution - lowerRes <= higherRes - resolution) ? lowerRes : higherRes;
            return Mathf.Clamp(snapped, Min, Max);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            int snappedClose = SnapToValidHeightmapResolution(heightmapResolution);
            if (snappedClose != heightmapResolution) heightmapResolution = snappedClose;

            int snappedFar = SnapToValidHeightmapResolution(farHeightmapResolution);
            if (snappedFar != farHeightmapResolution) farHeightmapResolution = snappedFar; 
        }
#endif

        void Start()
        {
            heightmapResolution = SnapToValidHeightmapResolution(heightmapResolution);
            farHeightmapResolution = SnapToValidHeightmapResolution(farHeightmapResolution);

            closeTier = new TerrainTier
            {
                prefab = closeTerrainPrefab,
                tileSize = chunkSize,
                resolution = heightmapResolution,
                viewRadius = viewRadius,
                isCloseTier = true
            };

            farTier = new TerrainTier
            {
                prefab = farTerrainPrefab,
                tileSize = chunkSize,
                resolution = farHeightmapResolution,
                viewRadius = viewRadius + farViewRadius,
                isCloseTier = false
            };

            PopulateTierPool(closeTier);
            PopulateFarTierPool();

            UpdateChunks(forceUpdate: true);
            RefreshAllTerrainNeighbors();
        }

        private void PopulateTierPool(TerrainTier tier)
        {
            int sideLength = (tier.viewRadius * 2) + 1;
            int totalPoolSize = sideLength * sideLength;

            for (int i = 0; i < totalPoolSize; i++)
            {
                Terrain terrain = CreateTerrainInstance(tier);
                tier.terrainPool.Enqueue(terrain);
            }
        }

        /// <summary>
        /// The far tier only ever occupies the ring of chunks strictly outside the close tier's radius, so
        /// its pool only needs to be large enough for that ring rather than the full square its viewRadius implies.
        /// </summary>
        private void PopulateFarTierPool()
        {
            int outerSide = (farTier.viewRadius * 2) + 1;
            int innerSide = (viewRadius * 2) + 1;
            int ringPoolSize = (outerSide * outerSide) - (innerSide * innerSide);

            for (int i = 0; i < ringPoolSize; i++)
            {
                Terrain terrain = CreateTerrainInstance(farTier);
                farTier.terrainPool.Enqueue(terrain);
            }
        }

        /// <summary>
        /// Instantiates a terrain GameObject from the tier's prefab and configures a cloned TerrainData
        /// asset to match the tier's resolution/size settings.
        /// </summary>
        private Terrain CreateTerrainInstance(TerrainTier tier)
        {
            GameObject terrainGO = Instantiate(tier.prefab, this.transform);
            terrainGO.layer = terrainLayer;
            terrainGO.SetActive(false);

            Terrain terrain = terrainGO.GetComponent<Terrain>();

            TerrainData terrainData = Instantiate(terrain.terrainData);
            terrainData.heightmapResolution = tier.resolution;
            terrainData.size = new Vector3(tier.tileSize, terrainData.size.y, tier.tileSize);
            terrain.terrainData = terrainData;

            TerrainCollider terrainCollider = terrainGO.GetComponent<TerrainCollider>();
            if (terrainCollider != null) terrainCollider.terrainData = terrainData;

            terrain.drawInstanced = true;

            return terrain;
        }

        void Update()
        {
            Vector2Int playerCloseChunk = GetChunkCoords(playerTransform.position, closeTier.tileSize);
            if (playerCloseChunk != closeTier.currentCenterCoord)
            {
                closeTier.currentCenterCoord = playerCloseChunk;

                // The far tier's coverage is derived directly from the close grid's boundary (see
                // UpdateChunksForTier), so it must be refreshed any time the close grid moves too.
                UpdateChunksForTier(closeTier, forceUpdate: false);
                UpdateChunksForTier(farTier, forceUpdate: false);
            }

            // Run our mathematical altitude tracking probe array on every frame loop pass
            UpdateAltitudeSpeedModifier();
        }

        Vector2Int GetChunkCoords(Vector3 worldPos, float tileSize)
        {
            int x = Mathf.FloorToInt(worldPos.x / tileSize);
            int z = Mathf.FloorToInt(worldPos.z / tileSize);
            return new Vector2Int(x, z);
        }

        private void UpdateAltitudeSpeedModifier()
        {
            Vector3 playerPos = playerTransform.position;

            // Sample a cross-pattern grid array of 5 points in memory relative to absolute world positions
            float hCenter = World.Instance.SampleMacroHeightmap(playerPos.x, playerPos.z);
            float hNorth = World.Instance.SampleMacroHeightmap(playerPos.x, playerPos.z + altitudeSampleRadius);
            float hSouth = World.Instance.SampleMacroHeightmap(playerPos.x, playerPos.z - altitudeSampleRadius);
            float hEast = World.Instance.SampleMacroHeightmap(playerPos.x + altitudeSampleRadius, playerPos.z);
            float hWest = World.Instance.SampleMacroHeightmap(playerPos.x - altitudeSampleRadius, playerPos.z);

            // Compute the definitive macro-ground elevation line right beneath the player's movement volume
            float averageGroundHeight = (hCenter + hNorth + hSouth + hEast + hWest) / 5f;

            // Find current clean clearance height
            float playerHeightAboveGround = Mathf.Max(0f, playerPos.y - averageGroundHeight);

            // Calculate a normalized t-value mapping clearance heights directly to velocity scales
            float altitudeT = (playerHeightAboveGround - minAltitudeThreshold) / (maxAltitudeThreshold - minAltitudeThreshold);
            altitudeT = Mathf.Clamp01(altitudeT);

            // Normalized result: 1.0f if low/grounded, scaling down smoothly toward 0.0f if flying high.
            // This acts as a dampener factor that will squash our slice iterations!
            currentAltitudeSpeedMultiplier = Mathf.Lerp(1f, 0f, altitudeT);
        }

        /// <summary>
        /// Returns the close grid's currently visible chunk coordinates as a simple square ring
        /// centered on the player's close chunk.
        /// </summary>
        private HashSet<Vector2Int> GetVisibleCloseCoords()
        {
            HashSet<Vector2Int> coords = new HashSet<Vector2Int>();

            for (int x = -closeTier.viewRadius; x <= closeTier.viewRadius; x++)
            {
                for (int z = -closeTier.viewRadius; z <= closeTier.viewRadius; z++)
                {
                    coords.Add(new Vector2Int(closeTier.currentCenterCoord.x + x, closeTier.currentCenterCoord.y + z));
                }
            }

            return coords;
        }

        /// <summary>
        /// Returns the far tier's currently visible chunk coordinates. Far tiles share the exact same
        /// coordinate space and world size as close chunks (only their heightmap resolution differs), so
        /// this is simply the square ring of chunks strictly outside the close tier's radius, up to the
        /// combined close+far radius. Sharing coordinate space guarantees no overlap and no gaps.
        /// </summary>
        private HashSet<Vector2Int> GetVisibleFarCoords()
        {
            HashSet<Vector2Int> coords = new HashSet<Vector2Int>();

            for (int x = -farTier.viewRadius; x <= farTier.viewRadius; x++)
            {
                for (int z = -farTier.viewRadius; z <= farTier.viewRadius; z++)
                {
                    // Skip chunks already covered by the close tier.
                    if (Mathf.Abs(x) <= viewRadius && Mathf.Abs(z) <= viewRadius) continue;

                    coords.Add(new Vector2Int(closeTier.currentCenterCoord.x + x, closeTier.currentCenterCoord.y + z));
                }
            }

            return coords;
        }

        public void UpdateChunks(bool forceUpdate)
        {
            UpdateChunksForTier(closeTier, forceUpdate);

        }

        private void UpdateChunksForTier(TerrainTier tier, bool forceUpdate)
        {
            HashSet<Vector2Int> visibleChunkCoords = tier.isCloseTier
                ? GetVisibleCloseCoords()
                : GetVisibleFarCoords();

            List<Vector2Int> keysToRemove = new List<Vector2Int>();
            foreach (var kvp in tier.activeTerrains)
            {
                if (!visibleChunkCoords.Contains(kvp.Key) || forceUpdate)
                {
                    Terrain terrainToPool = kvp.Value;
                    terrainToPool.drawHeightmap = true;
                    terrainToPool.gameObject.SetActive(false);
                    tier.terrainPool.Enqueue(terrainToPool);
                    keysToRemove.Add(kvp.Key);

                    tier.chunkLoadingProgress.Remove(kvp.Key);
                    tier.loadQueue.Remove(kvp.Key);
                    tier.activeColliders.Remove(kvp.Key);
                    tier.appliedEdgeFlags.Remove(kvp.Key);
                }
            }
            foreach (var key in keysToRemove) tier.activeTerrains.Remove(key);

            foreach (Vector2Int coord in visibleChunkCoords)
            {
                if (!tier.activeTerrains.ContainsKey(coord) && tier.terrainPool.Count > 0)
                {
                    Terrain terrain = tier.terrainPool.Dequeue();
                    terrain.transform.position = new Vector3(coord.x * tier.tileSize, 0, coord.y * tier.tileSize);
                    terrain.drawHeightmap = false;

                    tier.activeTerrains.Add(coord, terrain);

                    TerrainCollider col = terrain.GetComponent<TerrainCollider>();
                    if (col != null) tier.activeColliders.Add(coord, col);

                    tier.chunkLoadingProgress[coord] = 0;
                    tier.loadQueue.Add(coord);
                }
            }

            // A close-tier chunk that stays active across a recenter can have its border role change (e.g.
            // it used to border the far tier and got its edges quantized/biased, but now sits interior to
            // the close grid, or vice versa). If we don't detect this and regenerate its heights, its
            // border vertices stay stale/mismatched against neighbors that were generated fresh, causing a
            // large seam between close chunks. Re-queue any active chunk whose border role changed.
            if (tier.isCloseTier)
            {
                foreach (Vector2Int coord in visibleChunkCoords)
                {
                    if (!tier.activeTerrains.ContainsKey(coord) || tier.loadQueue.Contains(coord)) continue;

                    EdgeFlags currentFlags = EdgeFlags.From(GetEdgeQuantization(coord));
                    bool hasPrevious = tier.appliedEdgeFlags.TryGetValue(coord, out EdgeFlags previousFlags);

                    if (!hasPrevious || !currentFlags.Equals(previousFlags))
                    {
                        tier.chunkLoadingProgress[coord] = 0;
                        tier.loadQueue.Add(coord);
                    }
                }
            }

            if (!isProcessingQueue && (closeTier.loadQueue.Count > 0 || farTier.loadQueue.Count > 0))
            {
                StartCoroutine(ProcessLoadQueueCoroutine());
            }

            RefreshAllTerrainNeighbors();
        }

        /// <summary>
        /// Links each active terrain (close or far tier) to its immediate left/right/top/bottom
        /// neighbor, if one is currently active in either tier. Since both tiers share the exact same
        /// tileSize and chunk-coordinate space, neighbors can be found across tier boundaries too. Unity
        /// uses these links to stitch/smooth shared edge vertices, eliminating the flat seam that
        /// otherwise appears along a terrain's border when its neighbor isn't set.
        /// </summary>
        private void RefreshAllTerrainNeighbors()
        {
            Dictionary<Vector2Int, Terrain> allActive = new Dictionary<Vector2Int, Terrain>();
            foreach (var kvp in closeTier.activeTerrains) allActive[kvp.Key] = kvp.Value;
            foreach (var kvp in farTier.activeTerrains) allActive[kvp.Key] = kvp.Value;

            foreach (var kvp in allActive)
            {
                Vector2Int coord = kvp.Key;
                Terrain terrain = kvp.Value;

                allActive.TryGetValue(coord + new Vector2Int(-1, 0), out Terrain left);
                allActive.TryGetValue(coord + new Vector2Int(0, -1), out Terrain bottom);
                allActive.TryGetValue(coord + new Vector2Int(1, 0), out Terrain right);
                allActive.TryGetValue(coord + new Vector2Int(0, 1), out Terrain top);

                terrain.SetNeighbors(left, top, right, bottom);
            }
        }

        /// <summary>
        /// Determines which sides (if any) of a close-tier chunk border the coarser far tier, so height
        /// sampling near those sides can be snapped to the far tier's vertex spacing. Only close-tier
        /// chunks need this: far-tier chunks are already the coarse side of any border, and same-tier
        /// neighbors always share identical resolution/spacing already.
        /// </summary>
        private World.EdgeQuantization GetEdgeQuantization(Vector2Int coord)
        {
            Vector2Int relative = coord - closeTier.currentCenterCoord;
            int r = closeTier.viewRadius;

            bool west = relative.x == -r;
            bool east = relative.x == r;
            bool south = relative.y == -r;
            bool north = relative.y == r;

            if (!west && !east && !south && !north) return World.EdgeQuantization.None;

            return new World.EdgeQuantization
            {
                west = west,
                east = east,
                south = south,
                north = north,
                coarseVertexSpacing = farTier.tileSize / (farTier.resolution - 1),
                heightBias = farTierHeightBias,
                applyUniformBias = false
            };
        }

        /// <summary>
        /// Far-tier chunks always get a uniform downward height bias (see farTierHeightBias), regardless of
        /// which sides border what: since the far tier is always the coarse side of any border, and its
        /// resolution/ring position changes as the player moves, biasing the whole chunk keeps its edges
        /// consistently at or below its close-tier neighbors, hiding LOD-shift cracks.
        /// </summary>
        private World.EdgeQuantization GetFarTierQuantization()
        {
            return new World.EdgeQuantization
            {
                heightBias = farTierHeightBias,
                applyUniformBias = true
            };
        }

        private IEnumerator ProcessLoadQueueCoroutine()
        {
            isProcessingQueue = true;

            while (closeTier.loadQueue.Count > 0 || farTier.loadQueue.Count > 0)
            {
                TerrainTier bestTier = null;
                int bestIndex = -1;
                float closestDistance = Mathf.Infinity;
                Vector3 playerPos = playerTransform.position;

                foreach (TerrainTier tier in new[] { closeTier, farTier })
                {
                    for (int i = 0; i < tier.loadQueue.Count; i++)
                    {
                        Vector2Int coord = tier.loadQueue[i];
                        Vector3 chunkCenterWorld = new Vector3(
                            (coord.x * tier.tileSize) + (tier.tileSize / 2f),
                            playerPos.y,
                            (coord.y * tier.tileSize) + (tier.tileSize / 2f)
                        );

                        float dist = Vector3.Distance(playerPos, chunkCenterWorld);
                        if (dist < closestDistance)
                        {
                            closestDistance = dist;
                            bestIndex = i;
                            bestTier = tier;
                        }
                    }
                }

                if (bestTier == null || bestIndex == -1) break;

                Vector2Int currentCoord = bestTier.loadQueue[bestIndex];

                if (bestTier.activeTerrains.TryGetValue(currentCoord, out Terrain terrain))
                {
                    if (!terrain.gameObject.activeSelf) terrain.gameObject.SetActive(true);

                    bestTier.activeColliders.TryGetValue(currentCoord, out TerrainCollider terrainCollider);

                    int tierResolution = bestTier.resolution;

                    // PATH A: FAST LOAD MODE
                    if (loadInstantDuringLoadingScreen)
                    {
                        World.EdgeQuantization edgeQuantization = bestTier.isCloseTier ? GetEdgeQuantization(currentCoord) : GetFarTierQuantization();

                        float[,] fullHeights = World.Instance.GenerateInitialChunkHeights(
                            currentCoord,
                            tierResolution,
                            terrain.terrainData.size.y,
                            0,
                            tierResolution,
                            edgeQuantization
                        );

                        terrain.terrainData.SetHeights(0, 0, fullHeights);
                        terrain.drawHeightmap = true;

                        bestTier.loadQueue.RemoveAt(bestIndex);
                        bestTier.chunkLoadingProgress.Remove(currentCoord);
                        if (bestTier.isCloseTier) bestTier.appliedEdgeFlags[currentCoord] = EdgeFlags.From(edgeQuantization); 

                        RefreshAllTerrainNeighbors();
                        PostLoadChunk(currentCoord, terrain, bestTier.isCloseTier);  
                    }
                    // PATH B: ADAPTIVE RUNTIME STREAMING MODE
                    else
                    {
                        int currentZRow = bestTier.chunkLoadingProgress[currentCoord];

                        float distanceT = (closestDistance - minDistanceThreshold) / (maxDistanceThreshold - minDistanceThreshold);
                        distanceT = Mathf.Clamp01(distanceT);

                        // 1. CALCULATE BASE DISTANCE STRIDE
                        float targetDistantSlices = Mathf.Lerp(closeProgressiveSliceCount, distantProgressiveSliceCount, distanceT);

                        // 2. APPLY ALTITUDE VELOCITY ACCELERATION
                        // As the player climbs higher, currentAltitudeSpeedMultiplier shrinks to 0.
                        // This compresses targetDistantSlices down to closeProgressiveSliceCount, forcing immediate loading.
                        int dynamicSliceCount = Mathf.RoundToInt(Mathf.Lerp(closeProgressiveSliceCount, targetDistantSlices, currentAltitudeSpeedMultiplier));
                        dynamicSliceCount = Mathf.Clamp(dynamicSliceCount, 1, tierResolution);

                        int baseSliceSize = tierResolution / dynamicSliceCount;
                        if (baseSliceSize <= 0) baseSliceSize = 1;

                        int startZ = currentZRow;
                        int endZ = Mathf.Min(startZ + baseSliceSize, tierResolution);
                        int sliceHeight = endZ - startZ;

                        if (sliceHeight > 0)
                        {
                            bool isNearPlayer = bestTier.isCloseTier && closestDistance <= physicsDeferDistanceThreshold;
                            ApplyHeightsAdaptiveSlice(bestTier, currentCoord, terrain, startZ, sliceHeight, isNearPlayer);
                            bestTier.chunkLoadingProgress[currentCoord] = endZ;
                        }

                        if (bestTier.chunkLoadingProgress[currentCoord] >= tierResolution)
                        {
                            if (!bestTier.isCloseTier || closestDistance > physicsDeferDistanceThreshold)
                            {
                                terrain.terrainData.SyncHeightmap();
                            }

                            terrain.drawHeightmap = true;

                            bestTier.loadQueue.RemoveAt(bestIndex);
                            bestTier.chunkLoadingProgress.Remove(currentCoord);

                            RefreshAllTerrainNeighbors();
                            PostLoadChunk(currentCoord, terrain, bestTier.isCloseTier);
                        }
                    }
                }
                else
                {
                    bestTier.loadQueue.RemoveAt(bestIndex);
                    bestTier.chunkLoadingProgress.Remove(currentCoord);
                }

                yield return null;
            }

            isProcessingQueue = false;
        }

        void ApplyHeightsAdaptiveSlice(TerrainTier tier, Vector2Int coord, Terrain terrain, int startZ, int sliceHeight, bool useStandardSync)
        {
            World.EdgeQuantization edgeQuantization = tier.isCloseTier ? GetEdgeQuantization(coord) : GetFarTierQuantization(); 
            if (tier.isCloseTier) tier.appliedEdgeFlags[coord] = EdgeFlags.From(edgeQuantization);

            float[,] sliceHeights = World.Instance.GenerateInitialChunkHeights(
                coord,
                tier.resolution,
                terrain.terrainData.size.y,
                startZ,
                sliceHeight,
                edgeQuantization
            );

            if (useStandardSync)
            {
                terrain.terrainData.SetHeights(0, startZ, sliceHeights);
            }
            else
            {
                terrain.terrainData.SetHeightsDelayLOD(0, startZ, sliceHeights);
            }
        }

        public float GetLoadingPercentage()
        {
            int totalTerrainCount = closeTier.activeTerrains.Count + farTier.activeTerrains.Count;
            if (totalTerrainCount == 0) return 100f;

            int totalPointsExpected = 0;
            int currentPointsLoaded = 0;

            foreach (TerrainTier tier in new[] { closeTier, farTier })
            {
                totalPointsExpected += tier.activeTerrains.Count * tier.resolution;

                foreach (var kvp in tier.activeTerrains)
                {
                    if (tier.chunkLoadingProgress.TryGetValue(kvp.Key, out int progress))
                    {
                        currentPointsLoaded += progress;
                    }
                    else
                    {
                        currentPointsLoaded += kvp.Value.gameObject.activeSelf ? tier.resolution : 0;
                    }
                }
            }

            if (totalPointsExpected <= 0) return 100f;

            return ((float)currentPointsLoaded / totalPointsExpected) * 100f;
        }

        protected virtual void PostLoadChunk(Vector2Int coord, Terrain terrain, bool isCloseTier)
        {
            // This method can be overridden in derived classes to perform additional actions after a chunk is fully loaded.
            // For example, you might want to add vegetation, objects, or other features to the terrain here.
            // isCloseTier distinguishes fine near-player chunks from coarse far background tiles.
        }
    }
}