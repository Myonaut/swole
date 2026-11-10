using UnityEngine;

namespace Swole.API.Unity.Worlds
{
    /// <summary>
    /// Defines a reusable brush "type" - optional texture-based falloff (or an animation curve fallback),
    /// optional local size/strength overrides that combine with the terrain editor's global brush
    /// size/strength, and optional bound terrain-texture channels so a brush stroke can paint terrain
    /// textures alongside height changes.
    ///
    /// Assign one of these to <see cref="RuntimeTerrainEditor"/> to select the active brush "type". Kept
    /// data-only (ScriptableObject) so brush types can be authored/tuned as assets and swapped at runtime
    /// via UI (e.g. a brush picker dropdown/list).
    /// </summary>
    [CreateAssetMenu(fileName = "New Brush Preset", menuName = "Swole/World/Terrain/Brush Preset")]
    public class BrushPreset : ScriptableObject, Swole.Modding.IPreviewable
    {
        [Header("Identity")]
        public string displayName;

        [Header("Falloff")]
        [Tooltip("Optional grayscale texture sampled radially (or per-footprint UV) to determine brush strength falloff. If unset, falloffCurve is used instead.")]
        public Texture2D falloffTexture;

        [Tooltip("Used when falloffTexture is unset. X axis is normalized distance from brush center (0 = center, 1 = edge), Y axis is strength multiplier.")]
        public AnimationCurve falloffCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

        [Header("Local Overrides (combine with global brush settings)")]
        [Tooltip("If true, localRadius is multiplied with the terrain editor's global brush radius. If false, this brush ignores localRadius and uses the global radius as-is.")]
        public bool useLocalRadius;
        [Tooltip("Multiplier applied to the global brush radius when useLocalRadius is enabled.")]
        public float localRadius = 1f;

        [Tooltip("If true, localStrength is multiplied with the terrain editor's global brush strength. If false, this brush ignores localStrength and uses the global strength as-is.")]
        public bool useLocalStrength;
        [Tooltip("Multiplier applied to the global brush strength when useLocalStrength is enabled.")]
        public float localStrength = 1f;

        [Header("Texture Channels")]
        [Tooltip("Optional terrain-texture channels this brush paints alongside height changes. Each can be toggled/mixed independently in the terrain editor UI.")]
        public TerrainTextureChannel[] textureChannels;

        /// <summary>Samples the falloff at a normalized distance (0 = center, 1 = brush edge).</summary>
        public float SampleFalloff(float normalizedDistance)
        {
            normalizedDistance = Mathf.Clamp01(normalizedDistance);

            if (falloffTexture != null)
            {
                // Treat the texture as a 1D radial falloff ramp: sample along U at V=0.5.
                return falloffTexture.GetPixelBilinear(normalizedDistance, 0.5f).r;
            }

            return falloffCurve != null ? falloffCurve.Evaluate(normalizedDistance) : (1f - normalizedDistance);
        }

        /// <summary>Combines this preset's local radius override (if any) with a global brush radius.</summary>
        public float ResolveRadius(float globalRadius) => useLocalRadius ? globalRadius * localRadius : globalRadius;

        /// <summary>Combines this preset's local strength override (if any) with a global brush strength.</summary>
        public float ResolveStrength(float globalStrength) => useLocalStrength ? globalStrength * localStrength : globalStrength;

        private Texture2D previewImage;
        private Texture2D GetOrGeneratePreviewImage()
        {
            if (previewImage != null) return previewImage;

            previewImage = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            if (falloffCurve != null && falloffCurve.length > 0)
            {
                var pixels = previewImage.GetPixels();

                for (int y = 0; y < previewImage.height; y++)
                {
                    for (int x = 0; x < previewImage.width; x++)
                    {
                        float u = (float)x / (previewImage.width - 1);
                        float v = (float)y / (previewImage.height - 1);
                        float distance = Mathf.Sqrt((u - 0.5f) * (u - 0.5f) + (v - 0.5f) * (v - 0.5f)) * Mathf.Sqrt(2f);
                        float falloffValue = SampleFalloff(distance);
                        pixels[y * previewImage.width + x] = new Color(falloffValue, falloffValue, falloffValue);
                    }
                }

                previewImage.SetPixels(pixels);
                previewImage.Apply();
            }

            return previewImage;
        }
        public Texture2D PreviewImageRaw => falloffTexture == null ? GetOrGeneratePreviewImage() : falloffTexture;

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
            if (previewImage != null)
            {
                DestroyImmediate(previewImage);
                previewImage = null;
            }
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
