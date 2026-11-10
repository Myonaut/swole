using System;
using UnityEngine;

namespace Swole.API.Unity.Worlds
{
    /// <summary>
    /// A single named terrain-texture channel that can be bound to a brush or stamp. Applies an optional
    /// mask texture (falls back to fully opaque if unset) to a specific <see cref="TerrainLayer"/>'s
    /// alphamap weight, blended by <see cref="defaultMix"/> and whatever the brush/stamp's own
    /// strength/opacity is at the time of application.
    ///
    /// Kept generic and reusable: both terrain height brushes/stamps and (eventually) a texture-painting
    /// tool can share this exact channel definition, since "paint a mask onto a terrain layer, blended by
    /// a mix value" is the same operation in both cases.
    /// </summary>
    [Serializable]
    public class TerrainTextureChannel
    {
        [Tooltip("Display name for this channel, shown in the terrain editor UI (e.g. \"Rock\", \"Sand\").")]
        public string channelName;

        [Tooltip("Which terrain layer (texture) this channel paints onto. Must already be present in the target TerrainData's terrainLayers.")]
        public TerrainLayer terrainLayer;

        [Tooltip("Optional mask texture controlling per-pixel weight within the brush/stamp footprint. If unset, the channel is treated as fully opaque across the whole footprint (modulated only by falloff/mix).")]
        public Texture2D maskTexture;

        [Range(0f, 1f)]
        [Tooltip("Default local mix/opacity for this channel. Exposed to the UI as a per-channel slider so it can be overridden at runtime without touching the preset asset.")]
        public float defaultMix = 1f;

        [Tooltip("Whether this channel is enabled by default. Can be toggled per-channel in the terrain editor UI.")]
        public bool enabledByDefault = true;

        /// <summary>Samples the mask at normalized footprint UV (0..1), or 1 if no mask texture is assigned.</summary>
        public float SampleMask(float u, float v)
        {
            if (maskTexture == null) return 1f;
            return maskTexture.GetPixelBilinear(u, v).r;
        }
    }

    /// <summary>
    /// Live, UI-mutable runtime state for a <see cref="TerrainTextureChannel"/> (enabled flag + mix value),
    /// separate from the source-of-truth preset asset so toggling channels in the UI doesn't modify the
    /// ScriptableObject. Shared by both brushes and stamps (and reusable by a future texture-paint tool).
    /// </summary>
    [Serializable]
    public class TerrainTextureChannelState
    {
        public TerrainTextureChannel channel;
        public bool enabled;
        [Range(0f, 1f)]
        public float mix;

        public TerrainTextureChannelState(TerrainTextureChannel channel)
        {
            this.channel = channel;
            enabled = channel != null && channel.enabledByDefault;
            mix = channel != null ? channel.defaultMix : 1f;
        }
    }
}
