using UnityEngine;

namespace Swole.API.Unity.Worlds
{
    /// <summary>
    /// Defines a reusable terrain stamp "type" - a main height texture with a start scale/rotation and a
    /// default max height controlling how strongly the stamp's height texture is applied, plus optional
    /// bound terrain-texture channels so placing the stamp can also paint terrain textures.
    ///
    /// Assign one of these to <see cref="RuntimeTerrainEditor"/> (or pass directly to its stamp placement
    /// API) to place a stamp. Kept data-only (ScriptableObject) so stamp types can be authored as assets
    /// and picked at runtime via UI (e.g. a stamp picker list).
    /// </summary>
    [CreateAssetMenu(fileName = "New Stamp Preset", menuName = "Swole/World/Terrain/Stamp Preset")]
    public class StampPreset : ScriptableObject, Swole.Modding.IPreviewable
    {
        [Header("Identity")]
        public string displayName;

        [Tooltip("Asset id used to look this preset up in a registry (see StampPresetRegistry) so placed stamps can be reconstructed by id when a world is reloaded.")]
        public string presetId;

        [Header("Height")]
        [Tooltip("Main grayscale height texture for this stamp's footprint.")]
        public Texture2D heightTexture;

        [Tooltip("Default world-space footprint scale applied when the stamp is first placed (before any per-placement scale adjustments).")]
        public Vector3 startScale = Vector3.one;

        [Tooltip("Optional default rotation applied when the stamp is first placed.")]
        public Quaternion startRotation = Quaternion.identity;

        [Tooltip("Default maximum height (in the terrain's height units) used to scale the stamp's height texture when applied. Combines with the placement's global height multiplier.")]
        public float defaultMaxHeight = 10f;

        [Header("Texture Channels")]
        [Tooltip("Optional terrain-texture channels this stamp paints alongside its height, in addition to its height-texture-driven shape. Each can be toggled/mixed independently in the terrain editor UI.")]
        public TerrainTextureChannel[] textureChannels;

        /// <summary>Samples the stamp's main height texture at normalized footprint UV (0..1).</summary>
        public float SampleHeight(float u, float v)
        {
            if (heightTexture == null) return 0f;
            return heightTexture.GetPixelBilinear(u, v).r;
        }

        public Texture2D PreviewImageRaw => heightTexture;

        private Sprite previewSprite;
        private Sprite GetOrGeneratePreviewSprite()
        {
            if (previewSprite != null) return previewSprite;

            var tex = PreviewImageRaw;
            if (tex == null) return null;

            previewSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));  
            return previewSprite;
        }
        public Sprite PreviewImage => GetOrGeneratePreviewSprite();

        public void Cleanup()
        {
            if (previewSprite != null)
            {
                DestroyImmediate(previewSprite);
                previewSprite = null;
            }
        }
        public void OnDisable()
        {
            Cleanup();
        }
    }
}
