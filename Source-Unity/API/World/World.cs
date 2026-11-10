using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Swole.API.Unity.Worlds
{

    public class World : MonoBehaviour
    {
        public static World Instance { get; private set; }

        [Header("Systems")]
        public TerrainPool terrainPoolManager;
        public WorldEdit worldEditSystem;

        private Vector3 worldOffset;
        public Vector3 WorldOffset
        {
            get => worldOffset;
            set => SetWorldOffset(value);
        }
        public void SetWorldOffset(Vector3 newOffset)
        {
            worldOffset = newOffset; 
        }

        [Header("Initial Continent Map Settings")]
        public Texture2D macroHeightmap; // Single 8k/16k linear grayscale texture
        public float maxContinentHeight = 1000f; // Scale height in meters
        public int worldSeed = 1337;

        [Header("Procedural Micro Detail Settings")]
        public float noiseFrequency = 0.005f; // Scale of the procedural hills
        public float noiseAmplitude = 15f;    // Max height variation added by noise (in meters)
        public int noiseOctaves = 3;

        [Header("Stamp Asset Registry")]
        public Texture2D[] stampTextures; // Array of pre-defined reusable grayscale stamps

        private float worldSizeMeters;

        // Tracks approved stamp placements grouped by chunk coordinates
        private Dictionary<Vector2Int, List<TerrainStampPlacement>> activeWorldStamps = new Dictionary<Vector2Int, List<TerrainStampPlacement>>();

        /// <summary>
        /// Returns the world-space origin (X/Z corner, Y always 0) of the given chunk coordinate. All
        /// stamp/prop/feature positions are stored relative to this origin (additive), rather than as
        /// exact world positions, so they remain valid under floating-origin world shifts.
        /// </summary>
        public Vector3 GetChunkOrigin(Vector2Int chunkCoord)
        {
            float chunkSize = terrainPoolManager.chunkSize;
            return new Vector3(chunkCoord.x * chunkSize, 0f, chunkCoord.y * chunkSize);
        }

        /// <summary>Returns the chunk coordinate that owns the given world-space position.</summary>
        public Vector2Int WorldToChunkCoord(Vector3 worldPosition)
        {
            float chunkSize = terrainPoolManager.chunkSize;
            return new Vector2Int(Mathf.FloorToInt(worldPosition.x / chunkSize), Mathf.FloorToInt(worldPosition.z / chunkSize));
        }

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            // Map dimensions: 64km = 64,000 meters
            worldSizeMeters = terrainPoolManager.chunkSize * 64f;
        }

        #region World Features (rivers, lakes, fences, roads, etc)

        private readonly Dictionary<string, IWorldFeatureInstance> liveFeatureInstances = new Dictionary<string, IWorldFeatureInstance>();

        public IWorldFeatureInstance GetLiveFeatureInstance(string id)
        {
            liveFeatureInstances.TryGetValue(id, out IWorldFeatureInstance instance);
            return instance;
        }

        public void LoadFeaturesFromWorldEdit()
        {
            ClearLiveFeatureInstances();

            if (worldEditSystem == null) return;

            foreach (var record in worldEditSystem.AllFeatures)
            {
                IWorldFeatureInstance instance = WorldFeatureRegistry.Instantiate(record);
                if (instance == null)
                {
                    Debug.LogWarning($"No registered factory for world feature type '{record.featureTypeId}' (feature id: {record.id}). Skipping.");
                    continue;
                }

                liveFeatureInstances[record.id] = instance;
            }
        }

        public void RegisterLiveFeatureInstance(string id, IWorldFeatureInstance instance)
        {
            if (string.IsNullOrEmpty(id) || instance == null) return;
            liveFeatureInstances[id] = instance;
        }

        public void RemoveLiveFeatureInstance(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (liveFeatureInstances.TryGetValue(id, out IWorldFeatureInstance instance))
            {
                instance.Destroy();
                liveFeatureInstances.Remove(id);
            }
        }

        private void ClearLiveFeatureInstances()
        {
            foreach (var instance in liveFeatureInstances.Values) instance.Destroy();
            liveFeatureInstances.Clear();
        }

        #endregion

        /// <summary>
        /// Registers a stamp placement to the given chunk. <paramref name="worldSpaceStamp"/>'s
        /// <see cref="TerrainStampPlacement.localPosition"/> must be an exact world-space position at the
        /// time of the call; this is the sole conversion point where it gets rebased to be relative
        /// (additive) to the chunk's world origin before being stored.
        /// </summary>
        public void RegisterApprovedStamp(Vector2Int chunkCoord, TerrainStampPlacement worldSpaceStamp)
        {
            worldSpaceStamp.localPosition -= GetChunkOrigin(chunkCoord);

            if (!activeWorldStamps.ContainsKey(chunkCoord))
            {
                activeWorldStamps[chunkCoord] = new List<TerrainStampPlacement>();
            }
            activeWorldStamps[chunkCoord].Add(worldSpaceStamp);
        }

        // Natively creates an optimized slice matching the exact slice height window needed
        public float[,] GenerateInitialChunkHeights(Vector2Int chunkCoord, int resolution, float terrainBaseHeight, int startZ, int sliceHeight)
        { 
            return GenerateInitialChunkHeights(chunkCoord, resolution, terrainBaseHeight, startZ, sliceHeight, default);
        }

        /// <summary>
        /// Generates a heights slice for a chunk. When <paramref name="edgeQuantization"/> flags any sides,
        /// sampling positions near those sides are snapped to the coarser tier's vertex spacing (rather than
        /// this chunk's own finer spacing) so the height values along that border only change in the same
        /// "steps" the coarser neighboring terrain uses. This makes the fine mesh's edge curvature match the
        /// coarse mesh's curvature exactly at the shared border, eliminating the visible seam/crease that
        /// otherwise appears where a high-resolution chunk meets a low-resolution one.
        ///
        /// <paramref name="edgeQuantization"/>'s heightBias additionally lowers affected vertices by a small
        /// amount (in meters): the whole chunk when <see cref="EdgeQuantization.applyUniformBias"/> is set
        /// (far-tier chunks), or just the quantized edge band otherwise (close-tier borders against the far
        /// tier). Since far-tier chunks shift LOD/resolution as the player moves, any residual crack at the
        /// close/far border is biased downward, so it reads as a tiny recess rather than a floating gap and
        /// is far less likely to be visible from the camera.
        /// </summary>
        public float[,] GenerateInitialChunkHeights(Vector2Int chunkCoord, int resolution, float terrainBaseHeight, int startZ, int sliceHeight, EdgeQuantization edgeQuantization) 
        {
            // Allocate a localized 2D array scaled precisely to the slice dimension (height x width)
            float[,] finalHeights = new float[sliceHeight, resolution];
            float chunkSize = terrainPoolManager.chunkSize;
            float worldOriginX = chunkCoord.x * chunkSize;
            float worldOriginZ = chunkCoord.y * chunkSize;
            float vertexSpacing = chunkSize / (resolution - 1);

            bool hasQuantization = edgeQuantization.coarseVertexSpacing > 0f &&
                (edgeQuantization.west || edgeQuantization.east || edgeQuantization.south || edgeQuantization.north);
            float coarseSpacing = edgeQuantization.coarseVertexSpacing;

            bool hasHeightBias = edgeQuantization.heightBias > 0f;
            float normalizedUniformBias = hasHeightBias && edgeQuantization.applyUniformBias ? edgeQuantization.heightBias / terrainBaseHeight : 0f;

            // 1. Generate Base Macro and Micro Procedural Heights strictly for this window row block
            for (int z = 0; z < sliceHeight; z++)
            {
                // Translate the local slice index to its absolute position inside the 513x513 chunk scale
                int absoluteChunkZ = startZ + z;

                for (int x = 0; x < resolution; x++)
                {
                    float worldX = worldOriginX + (x * vertexSpacing);
                    float worldZ = worldOriginZ + (absoluteChunkZ * vertexSpacing);

                    bool inEdgeBand = false;

                    if (hasQuantization)
                    {
                        // Snap the sampling coordinate near a flagged edge to the coarse grid, so the fine
                        // mesh only picks up new height values at the same intervals the coarse neighbor does.
                        if (edgeQuantization.west && worldX <= worldOriginX + coarseSpacing)
                        {
                            worldX = worldOriginX + Mathf.Round((worldX - worldOriginX) / coarseSpacing) * coarseSpacing;
                            inEdgeBand = true;
                        }
                        else if (edgeQuantization.east && worldX >= worldOriginX + chunkSize - coarseSpacing)
                        {
                            worldX = worldOriginX + chunkSize - Mathf.Round((worldOriginX + chunkSize - worldX) / coarseSpacing) * coarseSpacing;
                            inEdgeBand = true;
                        }

                        if (edgeQuantization.south && worldZ <= worldOriginZ + coarseSpacing)
                        {
                            worldZ = worldOriginZ + Mathf.Round((worldZ - worldOriginZ) / coarseSpacing) * coarseSpacing;
                            inEdgeBand = true;
                        }
                        else if (edgeQuantization.north && worldZ >= worldOriginZ + chunkSize - coarseSpacing)
                        {
                            worldZ = worldOriginZ + chunkSize - Mathf.Round((worldOriginZ + chunkSize - worldZ) / coarseSpacing) * coarseSpacing;
                            inEdgeBand = true;
                        }
                    }

                    float macro = SampleMacroHeightmap(worldX, worldZ);
                    float micro = GetProceduralNoise(worldX, worldZ);

                    float heightValue = Mathf.Max(0, macro + micro) / terrainBaseHeight;

                    // Bias this vertex downward: uniformly for far-tier chunks, or only within the
                    // quantized edge band for close-tier chunks bordering the far tier.
                    if (hasHeightBias)
                    {
                        float bias = edgeQuantization.applyUniformBias ? normalizedUniformBias : (inEdgeBand ? edgeQuantization.heightBias / terrainBaseHeight : 0f);
                        if (bias > 0f) heightValue = Mathf.Max(0f, heightValue - bias);
                    }

                    finalHeights[z, x] = heightValue;

                    // 2. LIVE BRUSHED DELTA LAYER: Check if WorldEdit has an active user edit override for this exact point
                    if (worldEditSystem != null && worldEditSystem.HasDelta(chunkCoord, x, absoluteChunkZ, out float approvedOverrideHeight))
                    {
                        finalHeights[z, x] = approvedOverrideHeight;
                    }
                }
            }

            // 3. STAMP OVERLAY LAYER: Blends stamps dynamically inside the slice bounding constraints
            if (activeWorldStamps.TryGetValue(chunkCoord, out List<TerrainStampPlacement> stampsForThisChunk))
            {
                foreach (var stamp in stampsForThisChunk)
                {
                    if (stamp.stampAssetId >= stampTextures.Length) continue;
                    Texture2D stampTex = stampTextures[stamp.stampAssetId];

                    // Passes the slice indices to clamp stamp evaluations to this frame's row strip
                    ApplyStampToSliceArray(finalHeights, stamp, worldOriginX, worldOriginZ, resolution, terrainBaseHeight, stampTex, startZ, sliceHeight);
                }
            }

            return finalHeights;
        }

        /// <summary>
        /// Flags indicating which sides of a chunk border a lower-resolution neighbor, plus that
        /// neighbor's vertex spacing, so height sampling near those sides can be snapped to match it.
        /// Also carries an optional downward height bias to reduce the visibility of any LOD-shift cracks.
        /// </summary>
        public struct EdgeQuantization
        {
            public bool west, east, south, north;
            public float coarseVertexSpacing;

            /// <summary>Amount (in meters) to lower affected vertices by.</summary>
            public float heightBias;
            /// <summary>When true, heightBias applies to every vertex in the chunk rather than just the quantized edge band.</summary>
            public bool applyUniformBias;

            public static readonly EdgeQuantization None = default;
        }

        private void ApplyStampToSliceArray(float[,] heights, TerrainStampPlacement stamp, float chunkOriginX, float chunkOriginZ, int res, float terrainMaxHeight, Texture2D stampTex, int sliceStartZ, int sliceHeight)
        {
            float scaleFactor = (res - 1) / terrainPoolManager.chunkSize;

            // stamp.localPosition is relative (additive) to the owning chunk's world origin, so its true
            // world-space X/Z is reconstructed here before evaluating footprint bounds/UVs.
            float stampWorldX = chunkOriginX + stamp.localPosition.x;
            float stampWorldZ = chunkOriginZ + stamp.localPosition.z;

            float extentX = stamp.radius * stamp.scale.x;
            float extentZ = stamp.radius * stamp.scale.z;
            float maxExtent = Mathf.Max(extentX, extentZ);

            // Calculate the stamp's bounding box boundaries inside full chunk array index units
            int stampStartX = Mathf.Clamp(Mathf.FloorToInt((stampWorldX - maxExtent - chunkOriginX) * scaleFactor), 0, res - 1);
            int stampStartZ = Mathf.Clamp(Mathf.FloorToInt((stampWorldZ - maxExtent - chunkOriginZ) * scaleFactor), 0, res - 1);
            int stampEndX = Mathf.Clamp(Mathf.CeilToInt((stampWorldX + maxExtent - chunkOriginX) * scaleFactor), 0, res - 1);
            int stampEndZ = Mathf.Clamp(Mathf.CeilToInt((stampWorldZ + maxExtent - chunkOriginZ) * scaleFactor), 0, res - 1);

            // Intersection bounds check: Restricts vertical parsing loop to the current horizontal frame slice
            int currentSliceEndIdxZ = sliceStartZ + sliceHeight - 1;
            int activeBoundsStartZ = Mathf.Max(stampStartZ, sliceStartZ);
            int activeBoundsEndZ = Mathf.Min(stampEndZ, currentSliceEndIdxZ);

            if (activeBoundsStartZ > activeBoundsEndZ || stampStartX > stampEndX) return;

            for (int absoluteChunkZ = activeBoundsStartZ; absoluteChunkZ <= activeBoundsEndZ; absoluteChunkZ++)
            {
                // Determine the localized vertical target array cell coordinate row index
                int localizedSliceIdxZ = absoluteChunkZ - sliceStartZ;

                for (int x = stampStartX; x <= stampEndX; x++)
                {
                    float worldX = chunkOriginX + (x / scaleFactor);
                    float worldZ = chunkOriginZ + (absoluteChunkZ / scaleFactor);

                    float dx = worldX - stampWorldX;
                    float dz = worldZ - stampWorldZ;

                    float rad = stamp.RotationYDegrees * Mathf.Deg2Rad;
                    float rotatedX = dx * Mathf.Cos(rad) - dz * Mathf.Sin(rad);
                    float rotatedZ = dx * Mathf.Sin(rad) + dz * Mathf.Cos(rad);

                    if (stamp.scale.x > 0f) rotatedX /= stamp.scale.x;
                    if (stamp.scale.z > 0f) rotatedZ /= stamp.scale.z;

                    float u = (rotatedX / (stamp.radius * 2f)) + 0.5f;
                    float v = (rotatedZ / (stamp.radius * 2f)) + 0.5f;

                    if (u >= 0f && u <= 1f && v >= 0f && v <= 1f)
                    {
                        Color pixelColor = stampTex.GetPixelBilinear(u, v);
                        float stampHeightValue = pixelColor.r * stamp.opacity;
                        float stampValueNormalized = (stampHeightValue * stamp.localPosition.y) / terrainMaxHeight;

                        switch (stamp.mode)
                        {
                            case StampBlendMode.Raise:
                                heights[localizedSliceIdxZ, x] += stampValueNormalized;
                                break;
                            case StampBlendMode.Lower:
                                heights[localizedSliceIdxZ, x] = Mathf.Max(0, heights[localizedSliceIdxZ, x] - stampValueNormalized);
                                break;
                            case StampBlendMode.Flatten:
                                float targetTargetHeight = stamp.localPosition.y / terrainMaxHeight;
                                heights[localizedSliceIdxZ, x] = Mathf.Lerp(heights[localizedSliceIdxZ, x], targetTargetHeight, stampHeightValue);
                                break;
                        }
                    }
                }
            }
        }

        public float SampleMacroHeightmap(float worldX, float worldZ)
        {
            var worldOffset = WorldOffset;
            return SampleMacroHeightmapRaw(worldX + worldOffset.x, worldZ + worldOffset.z);
        }
        public float SampleMacroHeightmapRaw(float worldX, float worldZ)
        {
            if (macroHeightmap == null) return 0f;

            float halfWorld = worldSizeMeters / 2f;
            float u = (worldX + halfWorld) / worldSizeMeters;
            float v = (worldZ + halfWorld) / worldSizeMeters;
            if (u < 0 || u > 1 || v < 0 || v > 1) return 0f;
            Color pixel = macroHeightmap.GetPixelBilinear(u, v);
            return pixel.r * maxContinentHeight;
        }

        private float GetProceduralNoise(float worldX, float worldZ)
        {
            var worldOffset = WorldOffset;
            return GetProceduralNoiseRaw(worldX + worldOffset.x, worldZ + worldOffset.z);
        }
        private float GetProceduralNoiseRaw(float worldX, float worldZ)
        {
            float totalNoise = 0f;
            float currentFrequency = noiseFrequency;
            float currentAmplitude = noiseAmplitude;
            for (int i = 0; i < noiseOctaves; i++)
            {
                float sampleX = (worldX + worldSeed) * currentFrequency;
                float sampleZ = (worldZ + worldSeed) * currentFrequency;
                float noiseValue = Mathf.PerlinNoise(sampleX, sampleZ) - 0.5f;
                totalNoise += noiseValue * currentAmplitude;
                currentFrequency *= 2f;
                currentAmplitude *= 0.5f;
            }
            return totalNoise;
        }
    }
}