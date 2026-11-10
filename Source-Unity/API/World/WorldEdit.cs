using UnityEngine;

namespace Swole.API.Unity.Worlds
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using UnityEngine;
    using Swole.API.Unity;

    public class WorldEdit : MonoBehaviour
    {
        #region World Features (rivers, lakes, fences, roads, etc)

        /// <summary>
        /// Raised whenever the set of locally tracked feature submissions changes (add/remove/edit).
        /// UI can bind to this to refresh a feature list, etc.
        /// </summary>
        public event Action OnFeaturesChanged; 

        /// <summary>
        /// Raised when a feature submission is committed to the (currently local/offline) world state.
        /// Reserved so a future networked implementation can hook the same call site to also push the
        /// submission online, without callers needing to change.
        /// </summary>
        public event Action<WorldFeatureRecord> OnFeatureCommitted;

        private readonly Dictionary<string, WorldFeatureRecord> featureSubmissions = new Dictionary<string, WorldFeatureRecord>();

        public IReadOnlyCollection<WorldFeatureRecord> AllFeatures => featureSubmissions.Values;

        public bool TryGetFeature(string id, out WorldFeatureRecord record) => featureSubmissions.TryGetValue(id, out record);

        /// <summary>Adds or overwrites a locally tracked feature submission (e.g. after creation or an edit).</summary>
        public void SubmitFeature(WorldFeatureRecord record)
        {
            if (record == null || string.IsNullOrEmpty(record.id)) return;

            featureSubmissions[record.id] = record.Clone();
            OnFeaturesChanged?.Invoke();
        }

        /// <summary>Removes a locally tracked feature submission (e.g. after deletion).</summary>
        public void RemoveFeature(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (featureSubmissions.Remove(id)) OnFeaturesChanged?.Invoke();
        }

        /// <summary>
        /// Approves and commits a feature submission to the live world state. Currently this is a local,
        /// synchronous, offline operation (the submission is simply marked committed); the same call site
        /// is reserved for a future networked commit/sync flow so calling code won't need to change.
        /// </summary>
        public bool CommitFeature(string id)
        {
            if (!featureSubmissions.TryGetValue(id, out WorldFeatureRecord record)) return false;

            record.submissionState = WorldFeatureSubmissionState.Committed;
            OnFeatureCommitted?.Invoke(record.Clone());
            OnFeaturesChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Serializes all currently tracked feature submissions to a compact binary format, for local
        /// persistence or future upload. JSON is avoided here since feature control point data can grow
        /// large; binary keeps storage/transfer cost down.
        /// </summary>
        public byte[] SerializeFeatures()
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                writer.Write(featureSubmissions.Count);
                foreach (var record in featureSubmissions.Values)
                {
                    writer.Write(record.id ?? string.Empty);
                    writer.Write(record.featureTypeId ?? string.Empty);
                    writer.Write(record.profileId ?? string.Empty);
                    writer.Write(record.contributorId ?? string.Empty);
                    writer.Write((byte)record.submissionState);
                    writer.Write(record.originChunk.x);
                    writer.Write(record.originChunk.y);

                    writer.Write(record.controlPoints.Count);
                    foreach (var point in record.controlPoints)
                    {
                        writer.Write(point.x);
                        writer.Write(point.y);
                        writer.Write(point.z);
                        writer.Write(point.w);
                    }
                }

                return ms.ToArray();
            }
        }

        /// <summary>
        /// Replaces all currently tracked feature submissions with the ones described by the given binary
        /// blob (as produced by <see cref="SerializeFeatures"/>). Does not instantiate scene objects;
        /// callers (typically <see cref="World"/>) are responsible for reconstructing live instances from
        /// <see cref="AllFeatures"/> afterwards via <see cref="WorldFeatureRegistry"/>.
        /// </summary>
        public void DeserializeFeatures(byte[] data)
        {
            featureSubmissions.Clear();

            if (data != null && data.Length > 0)
            {
                using (MemoryStream ms = new MemoryStream(data))
                using (BinaryReader reader = new BinaryReader(ms))
                {
                    int count = reader.ReadInt32();
                    for (int i = 0; i < count; i++)
                    {
                        var record = new WorldFeatureRecord
                        {
                            id = reader.ReadString(),
                            featureTypeId = reader.ReadString(),
                            profileId = reader.ReadString(),
                            contributorId = reader.ReadString(),
                            submissionState = (WorldFeatureSubmissionState)reader.ReadByte()
                        };
                        record.originChunk = new Vector2Int(reader.ReadInt32(), reader.ReadInt32());

                        int pointCount = reader.ReadInt32();
                        record.controlPoints = new List<Vector4>(pointCount);
                        for (int p = 0; p < pointCount; p++)
                        {
                            record.controlPoints.Add(new Vector4(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
                        }

                        if (string.IsNullOrEmpty(record.id)) continue;
                        featureSubmissions[record.id] = record;
                    }
                }
            }

            OnFeaturesChanged?.Invoke();
        }

        #endregion

        [System.Serializable]
        public struct VertexDelta
        {
            public int index;     // Which vertex changed (0 to 263168 for a 513x513 map)
            public float height;  // The new modified height value (0.0f to 1.0f)
        }

        [System.Serializable]
        public class ChunkDeltaSubmission
        {
            public int chunkX;
            public int chunkZ;
            public string countryId;
            public string contributorId;
            public List<VertexDelta> deltas; // Only holds vertices that were brushed

            /// <summary>Serializes this submission to a compact binary format. JSON is avoided since a chunk's deltas can be numerous.</summary>
            public byte[] Serialize()
            {
                using (MemoryStream ms = new MemoryStream())
                using (BinaryWriter writer = new BinaryWriter(ms))
                {
                    writer.Write(chunkX);
                    writer.Write(chunkZ);
                    writer.Write(countryId ?? string.Empty);
                    writer.Write(contributorId ?? string.Empty);

                    int deltaCount = deltas == null ? 0 : deltas.Count;
                    writer.Write(deltaCount);
                    for (int i = 0; i < deltaCount; i++)
                    {
                        writer.Write(deltas[i].index);
                        writer.Write(deltas[i].height);
                    }

                    return ms.ToArray();
                }
            }

            /// <summary>Deserializes a submission previously written by <see cref="Serialize"/>.</summary>
            public static ChunkDeltaSubmission Deserialize(byte[] data)
            {
                var submission = new ChunkDeltaSubmission { deltas = new List<VertexDelta>() };
                if (data == null || data.Length == 0) return submission;

                using (MemoryStream ms = new MemoryStream(data))
                using (BinaryReader reader = new BinaryReader(ms))
                {
                    submission.chunkX = reader.ReadInt32();
                    submission.chunkZ = reader.ReadInt32();
                    submission.countryId = reader.ReadString();
                    submission.contributorId = reader.ReadString();

                    int deltaCount = reader.ReadInt32();
                    submission.deltas = new List<VertexDelta>(deltaCount);
                    for (int i = 0; i < deltaCount; i++)
                    {
                        submission.deltas.Add(new VertexDelta { index = reader.ReadInt32(), height = reader.ReadSingle() });
                    }
                }

                return submission;
            }
        }

        // Reads a 16-bit raw binary stream into a Unity-ready 2D float array
        public float[,] LoadBaseHeightmapFromBytes(byte[] rawBytes, int resolution)
        {
            float[,] heights = new float[resolution, resolution];

            // 16-bit means 2 bytes per vertex pointer
            if (rawBytes.Length != resolution * resolution * 2)
            {
                Debug.LogError("Byte array size does not match expected terrain resolution footprint!");
                return heights;
            }

            using (MemoryStream ms = new MemoryStream(rawBytes))
            using (BinaryReader br = new BinaryReader(ms))
            {
                for (int z = 0; z < resolution; z++)
                {
                    for (int x = 0; x < resolution; x++)
                    {
                        // Read UInt16 (0 to 65535) and normalize it to Unity's 0.0 to 1.0 terrain scale
                        ushort rawHeight = br.ReadUInt16();
                        heights[z, x] = (float)rawHeight / ushort.MaxValue;
                    }
                }
            }
            return heights;
        }

        // Compares an actively modified terrain against its unedited baseline to extract the delta
        public ChunkDeltaSubmission GenerateChunkDelta(Vector2Int coord, string country, string user, float[,] baseHeights, float[,] currentHeights, int resolution)
        {
            ChunkDeltaSubmission submission = new ChunkDeltaSubmission
            {
                chunkX = coord.x,
                chunkZ = coord.y,
                countryId = country,
                contributorId = user,
                deltas = new List<VertexDelta>()
            };

            int vertexIndex = 0;
            float tolerance = 0.0001f; // Ignore microscopic changes from floating point drift

            for (int z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float difference = Mathf.Abs(currentHeights[z, x] - baseHeights[z, x]);

                    if (difference > tolerance)
                    {
                        submission.deltas.Add(new VertexDelta
                        {
                            index = vertexIndex,
                            height = currentHeights[z, x]
                        });
                    }
                    vertexIndex++;
                }
            }

            Debug.Log($"Extracted {submission.deltas.Count} modified vertices out of {resolution * resolution} total.");
            return submission;
        }

        // Fast structural mapping pattern: [Chunk Coordinates] -> [Flattened Vertex Index ID, Height value]
        private Dictionary<Vector2Int, Dictionary<int, float>> activeCommunityDeltas = new Dictionary<Vector2Int, Dictionary<int, float>>();

        public bool HasDelta(Vector2Int chunkCoord, int x, int z, out float modifiedHeight)
        {
            modifiedHeight = 0f;

            if (activeCommunityDeltas.TryGetValue(chunkCoord, out var chunkDeltas))
            {
                // Map 2D array points to a flat unique integer hash identifier index
                int vertexIndexKey = (z * World.Instance.terrainPoolManager.heightmapResolution) + x;
                return chunkDeltas.TryGetValue(vertexIndexKey, out modifiedHeight);
            }

            return false;
        }

        // Call this whenever your community editor brush successfully alters a point
        public void RecordLiveVertexEdit(Vector2Int chunkCoord, int x, int z, float newHeightNormalized)
        {
            if (!activeCommunityDeltas.ContainsKey(chunkCoord))
            {
                activeCommunityDeltas[chunkCoord] = new Dictionary<int, float>();
            }

            int vertexIndexKey = (z * World.Instance.terrainPoolManager.heightmapResolution) + x;
            activeCommunityDeltas[chunkCoord][vertexIndexKey] = newHeightNormalized;
        }
    }

    [System.Serializable]
    public enum StampBlendMode { Raise, Lower, Flatten }

    [System.Serializable]
    public struct TerrainStampPlacement
    {
        public ushort stampAssetId; // Identifies which pre-defined texture to use (2 bytes)

        /// <summary>
        /// Position of the stamp center, relative (additive) to the world-space origin of the chunk this
        /// stamp belongs to (see <see cref="World.GetChunkOrigin"/>). This is NOT an exact world position;
        /// callers must add the owning chunk's origin to get the true world-space position. Kept relative
        /// so stamps remain valid under floating-origin world shifts.
        /// </summary>
        public Vector3 localPosition;
        public Quaternion rotation;   // Orientation of the stamp footprint (defaults to Quaternion.identity)
        public Vector3 scale;         // Per-axis scale of the stamp footprint (defaults to Vector3.one)
        public float radius;         // Size of the stamp brush
        public float opacity;        // Strength/Blend factor of the stamp
        public StampBlendMode mode;   // Height blend mode: Raise, Lower, Absolute Flatten

        /// <summary>
        /// Creates a stamp placement with default rotation (identity) and scale (one), matching the
        /// omit-if-default convention used elsewhere for transform serialization. Prefer this over the
        /// struct's default constructor, since a plain <c>default(TerrainStampPlacement)</c> would leave
        /// <see cref="rotation"/> and <see cref="scale"/> zeroed out rather than at their intended defaults.
        /// </summary>
        /// <param name="localPosition">Position relative (additive) to the owning chunk's world origin, not an exact world position.</param>
        public static TerrainStampPlacement Create(ushort stampAssetId, Vector3 localPosition, float radius, float opacity, StampBlendMode mode, Quaternion? rotation = null, Vector3? scale = null)
        {
            return new TerrainStampPlacement
            {
                stampAssetId = stampAssetId,
                localPosition = localPosition,
                rotation = rotation ?? Quaternion.identity,
                scale = scale ?? Vector3.one,
                radius = radius,
                opacity = opacity,
                mode = mode
            };
        }

        /// <summary>The stamp's rotation expressed as a Y-axis angle in degrees, for 2D footprint math.</summary>
        public float RotationYDegrees => rotation.eulerAngles.y;

        /// <summary>
        /// Serializes this stamp placement to a compact binary format, omitting the position, rotation, and
        /// scale components when they match their default values (see <see cref="CompactTransformSerializer"/>),
        /// consistent with all other transform-based serialization.
        /// </summary>
        public void Write(BinaryWriter writer)
        {
            writer.Write(stampAssetId);
            CompactTransformSerializer.WriteCompactTransform(writer, localPosition, rotation, scale);
            writer.Write(radius);
            writer.Write(opacity);
            writer.Write((byte)mode);
        }

        /// <summary>Deserializes a stamp placement previously written by <see cref="Write"/>.</summary>
        public static TerrainStampPlacement Read(BinaryReader reader)
        {
            TerrainStampPlacement stamp = new TerrainStampPlacement
            {
                stampAssetId = reader.ReadUInt16()
            };

            CompactTransformSerializer.ReadCompactTransform(reader, out stamp.localPosition, out stamp.rotation, out stamp.scale);

            stamp.radius = reader.ReadSingle();
            stamp.opacity = reader.ReadSingle();
            stamp.mode = (StampBlendMode)reader.ReadByte();

            return stamp;
        }
    }
}
