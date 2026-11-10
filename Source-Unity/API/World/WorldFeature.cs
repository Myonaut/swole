using System;
using System.Collections.Generic;
using UnityEngine;

namespace Swole.API.Unity.Worlds
{
    /// <summary>
    /// Generic, serializable description of a placed world feature (river, lake, fence, road, etc).
    /// Contains everything needed to exactly reproduce the feature when loading a world, without any
    /// dependency on the concrete system (e.g. RAM splines) that renders/generates it at runtime.
    /// </summary>
    [Serializable]
    public class WorldFeatureRecord
    {
        /// <summary>Unique id for this placed feature instance (stable across save/load/edit).</summary>
        public string id;

        /// <summary>Identifies which registered factory should be used to instantiate/regenerate this feature (e.g. "river", "lake").</summary>
        public string featureTypeId;

        /// <summary>Identifies which profile/material/preset to use when instantiating (looked up by the concrete factory).</summary>
        public string profileId;

        /// <summary>
        /// The chunk coordinate this feature's <see cref="controlPoints"/> are relative to. Each control
        /// point's xyz is additive to <see cref="World.GetChunkOrigin"/> of this coordinate (Y is
        /// absolute), rather than an exact world position, so the feature remains valid under
        /// floating-origin world shifts.
        /// </summary>
        public Vector2Int originChunk;

        /// <summary>
        /// Ordered control points describing the feature's shape. xz is relative (additive) to the world
        /// origin of <see cref="originChunk"/>, y is an absolute height, and w is an optional per-point
        /// parameter (e.g. width) whose meaning is defined by the concrete feature type.
        /// </summary>
        public List<Vector4> controlPoints = new List<Vector4>();

        /// <summary>Id of the user who created/last edited this feature. Reserved for future online review/attribution.</summary>
        public string contributorId;

        /// <summary>Review/commit state. Reserved for future online submission workflow.</summary>
        public WorldFeatureSubmissionState submissionState = WorldFeatureSubmissionState.Local;

        public WorldFeatureRecord() { }

        public WorldFeatureRecord(string id, string featureTypeId, string profileId, IEnumerable<Vector4> controlPoints, string contributorId = null, Vector2Int originChunk = default)
        {
            this.id = id;
            this.featureTypeId = featureTypeId;
            this.profileId = profileId;
            this.controlPoints = controlPoints == null ? new List<Vector4>() : new List<Vector4>(controlPoints);
            this.contributorId = contributorId;
            this.originChunk = originChunk;
        }

        /// <summary>Deep copy, so undo/redo snapshots don't alias a live, still-being-edited record.</summary>
        public WorldFeatureRecord Clone()
        {
            return new WorldFeatureRecord(id, featureTypeId, profileId, controlPoints, contributorId, originChunk)
            {
                submissionState = submissionState
            };
        }
    }

    [Serializable]
    public enum WorldFeatureSubmissionState { Local, PendingReview, Committed, Rejected }

    /// <summary>
    /// Implemented by the concrete runtime representation of a world feature (e.g. a RAM river/lake
    /// wrapper). Lets generic world/undo code create, update and tear down feature instances without
    /// knowing about the concrete rendering/generation system behind them.
    /// </summary>
    public interface IWorldFeatureInstance
    {
        /// <summary>The GameObject that represents this feature instance in the scene.</summary>
        GameObject FeatureGameObject { get; }

        /// <summary>Applies the given record's control points/profile to this instance and regenerates it.</summary>
        void ApplyRecord(WorldFeatureRecord record);

        /// <summary>Captures the current live state of this instance into a record (used when committing edits).</summary>
        WorldFeatureRecord CaptureRecord();

        /// <summary>Destroys/removes this feature instance from the scene.</summary>
        void Destroy();
    }

    /// <summary>
    /// Creates a new, empty <see cref="IWorldFeatureInstance"/> of a specific feature type, ready to have
    /// a <see cref="WorldFeatureRecord"/> applied to it via <see cref="IWorldFeatureInstance.ApplyRecord"/>.
    /// </summary>
    public delegate IWorldFeatureInstance WorldFeatureFactory(string profileId);

    /// <summary>
    /// Central registry mapping a feature type id (e.g. "river", "lake") to a factory capable of
    /// instantiating it. Kept generic/isolated from any concrete feature system (RAM, DrawXXL, etc) so
    /// that integration layers (like a world editor) can register their own factories without Swole's
    /// core world/undo code needing to reference them.
    /// </summary>
    public static class WorldFeatureRegistry
    {
        private static readonly Dictionary<string, WorldFeatureFactory> factories = new Dictionary<string, WorldFeatureFactory>();

        public static void Register(string featureTypeId, WorldFeatureFactory factory)
        {
            if (string.IsNullOrEmpty(featureTypeId) || factory == null) return;
            factories[featureTypeId] = factory;
        }

        public static void Unregister(string featureTypeId) => factories.Remove(featureTypeId);

        public static bool IsRegistered(string featureTypeId) => !string.IsNullOrEmpty(featureTypeId) && factories.ContainsKey(featureTypeId);

        /// <summary>Instantiates a new feature instance for the given record's type, and immediately applies the record to it. Returns null if the type isn't registered.</summary>
        public static IWorldFeatureInstance Instantiate(WorldFeatureRecord record)
        {
            if (record == null || !factories.TryGetValue(record.featureTypeId, out WorldFeatureFactory factory)) return null;

            IWorldFeatureInstance instance = factory(record.profileId);
            if (instance == null) return null;

            instance.ApplyRecord(record);
            return instance;
        }
    }
}
