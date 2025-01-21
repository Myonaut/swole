#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine.SceneManagement;

using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

using UnityEngine;

using Swole.API.Unity.Animation;

namespace Swole.API.Unity
{
    public class TextureNativePixels : SingletonBehaviour<TextureNativePixels>, IDisposable
    {
        public override bool ExecuteInStack => true;
        public static int ExecutionPriority => CustomAnimatorUpdater.ExecutionPriority + 1;
        public override int Priority => base.Priority;

        public override bool DestroyOnLoad => false;

        protected override void OnInit()
        {
            base.OnInit();

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode) 
        {
            //if (mode == LoadSceneMode.Single) ClearListeners(); // bad idea. if behaviours subscribe in awake this will happen after that and clear them.
        }

        public override void OnFixedUpdate()
        {
        }

        public event VoidParameterlessDelegate PostLateUpdate;
        public override void OnLateUpdate()
        {
            PostLateUpdate?.Invoke();
        }
        public static void SubscribeLate(VoidParameterlessDelegate del)
        {
            var instance = Instance;
            if (instance == null) return;

            instance.PostLateUpdate += del;
        }
        public static void UnsubscribeLate(VoidParameterlessDelegate del)
        {
            var instance = Instance;
            if (instance == null) return;

            instance.PostLateUpdate -= del;
        }

        public event VoidParameterlessDelegate PostUpdate;
        public override void OnUpdate()
        {
            PostUpdate?.Invoke();
        }
        public static void Subscribe(VoidParameterlessDelegate del)
        {
            var instance = Instance;
            if (instance == null) return;

            instance.PostUpdate += del;
        }
        public static void Unsubscribe(VoidParameterlessDelegate del)
        {
            var instance = Instance;
            if (instance == null) return;

            instance.PostUpdate -= del;
        }

        public void ClearListeners()
        {
            PostUpdate = null;
            PostLateUpdate = null;
        }

        private readonly Dictionary<Texture2D, NativeArray<Color>> nativePixels = new Dictionary<Texture2D, NativeArray<Color>>();
        public NativeArray<Color> GetLocal(Texture2D texture, bool refreshPixels = false)
        {
            if (texture == null) return default;

            if (nativePixels.TryGetValue(texture, out var pixels)) 
            {
                if (refreshPixels) pixels.CopyFrom(texture.GetPixels());       
                return pixels;
            }

            pixels = new NativeArray<Color>(texture.GetPixels(), Allocator.Persistent);
            nativePixels[texture] = pixels;

            return pixels;
        }
        public static NativeArray<Color> Get(Texture2D texture, bool refreshPixels = false)
        {
            var instance = Instance;
            if (instance == null) return default;

            return instance.GetLocal(texture, refreshPixels);
        }
        public void Dispose()
        {
            ClearListeners();

            foreach (var pair in nativePixels)
            {
                if (pair.Value.IsCreated) pair.Value.Dispose();
            }

            nativePixels.Clear();
        }
        public void OnDestroy()
        {
            Dispose();
        }
    }

    [Serializable]
    public struct MaterialSlotAndPropertyAndMask
    {
        [Tooltip("Name of mask")]
        public string name;
        public MaterialSlotAndProperty slotProp;
    }
    [Serializable]
    public struct MaskNameRendererIndexMaterialSlotAndProperty
    {
        [Tooltip("Name of mask")]
        public string name;
        public int rendererIndex;
        public MaterialSlotAndProperty slotProp;
    }
    [Serializable]
    public class TargetMask
    {
        public string name;
        public ToonMaterialMaskController.TextureCombination textureSettings;

        [NonSerialized]
        private bool isApplied;

        [NonSerialized]
        private ToonMaterialMaskController.MaterialMask mask;

        public void Apply(ToonMaterialMaskController maskController, bool force = false)
        {
            if (!force && isApplied) return;

            mask = maskController.GetMask(name);
            if (mask == null) return;

            isApplied = true;
            mask.AddToCombinationStack(textureSettings);
        }
        public void Unapply(ToonMaterialMaskController maskController)
        {
            if (!isApplied) return;

            isApplied = false;
            if (mask == null) mask = maskController.GetMask(name);
            if (mask != null) mask.RemoveFromCombinationStack(textureSettings);
        }
    }
    public class ToonMaterialMaskController : MonoBehaviour, IDisposable
    {
        public static void SetMaterialMaskProperties(MaterialSlotAndProperty slotProp, Material[] mats, ToonMaterialMaskController.MaterialMask mask)
        {
            if (slotProp.slot >= mats.Length) return;

            var mat = mats[slotProp.slot];
            if (mat == null || !mat.HasProperty(slotProp.propertyName)) return;

            mat.SetTexture(slotProp.propertyName, mask == null ? null : mask.Texture);
        }

        public void Dispose()
        {
            TextureNativePixels.Unsubscribe(UpdateEarly);
            TextureNativePixels.UnsubscribeLate(UpdateLate);

            if (masks != null)
            {
                foreach (var mask in masks)
                {
                    try
                    {
                        mask.Dispose();
                    }
                    catch (Exception ex)
                    {
                        swole.LogError(ex);
                    }
                }

                masks = null;
            }
        }
        public void OnDestroy()
        {
            Dispose();
        }

        public void Awake()
        {
            TextureNativePixels.Subscribe(UpdateEarly);
            TextureNativePixels.SubscribeLate(UpdateLate);
        }
        public void UpdateEarly()
        {
            if (masks != null)
            {
                foreach (var mask in masks)
                {
                    try
                    {
                        if (!mask.StackIsDirty) continue;
                        mask.ApplyCombinationStack(true); 
                    }
                    catch (Exception ex)
                    {
                        swole.LogError(ex);
                    }
                }
            }
        }
        public void UpdateLate()
        {
            if (masks != null)
            {
                foreach(var mask in masks)
                {
                    try
                    {
                        if (!mask.IsDirty) continue;
                        mask.ApplyPixels(); 
                    } 
                    catch(Exception ex)
                    {
                        swole.LogError(ex);
                    }
                }
            }
        }

        public MaterialMask[] masks;
        public int GetMaskIndex(string maskName)
        {
            if (masks == null) return -1;

            for(int a = 0; a < masks.Length; a++) if (masks[a].name == maskName) return a;
            return -1;
        }
        public MaterialMask GetMask(string maskName)
        {
            var index = GetMaskIndex(maskName);
            if (index >= 0) return masks[index]; 

            return null;
        }

        [Serializable]
        public enum PixelComparisonMethod
        {
            Replace, Max, Min
        }
        [Serializable, Flags]
        public enum TextureChannels
        {
            None, R = 1, G = 2, B = 4, A = 8, InvertR = 16, InvertG = 32, InvertB = 64, InvertA = 128
        } 
        public static bool4 ChannelsAsBool4(TextureChannels channels) => new bool4((channels & TextureChannels.R) == TextureChannels.R, (channels & TextureChannels.G) == TextureChannels.G, (channels & TextureChannels.B) == TextureChannels.B, (channels & TextureChannels.A) == TextureChannels.A);
        public static bool4 ChannelInversionAsBool4(TextureChannels channels) => new bool4((channels & TextureChannels.InvertR) == TextureChannels.InvertR, (channels & TextureChannels.InvertG) == TextureChannels.InvertG, (channels & TextureChannels.InvertB) == TextureChannels.InvertB, (channels & TextureChannels.InvertA) == TextureChannels.InvertA);
        [Serializable]
        public struct ReferenceChannels
        {
            public TextureChannels channelsR;
            public TextureChannels channelsG;
            public TextureChannels channelsB;
            public TextureChannels channelsA;
        }
        [Serializable]
        public struct TextureCombination
        {
            public Texture2D texture;
            public ReferenceChannels referenceChannels;
            public PixelComparisonMethod comparison;
        }
        [Serializable]
        public class MaterialMask : IDisposable
        {

            public void Dispose()
            {
                Jobs.Complete();

                if (defaultPixels.IsCreated)
                {
                    defaultPixels.Dispose();
                    defaultPixels = default;
                }
                if (pixels.IsCreated)
                {
                    pixels.Dispose();
                    pixels = default;
                }
            }

            public string name;

            public Color defaultPixel;
            public int width, height;
            public TextureFormat textureFormat = TextureFormat.RGBA32;
            public bool mipMaps = true;
            public bool linear = true;

            public FilterMode filterMode = FilterMode.Bilinear;
            public TextureWrapMode wrapMode = TextureWrapMode.Clamp;

            private Texture2D texture; 
            public Texture2D Texture
            {
                get
                {
                    if (texture == null) ApplyPixels();
                    return texture;
                }
            }

            private bool isDirty;
            public bool IsDirty => isDirty;
            public void MarkAsDirty()
            {
                isDirty = true;
            }

            private NativeArray<Color> defaultPixels;  
            public NativeArray<Color> DefaultPixels
            {
                get
                {
                    if (!defaultPixels.IsCreated)
                    {
                        defaultPixels = new NativeArray<Color>(width * height, Allocator.Persistent);
                        for (int a = 0; a < defaultPixels.Length; a++) defaultPixels[a] = defaultPixel;
                    }

                    return defaultPixels;
                }
            }
            private NativeArray<Color> pixels;
            public NativeArray<Color> Pixels
            {
                get
                {
                    if (!pixels.IsCreated)
                    {
                        pixels = new NativeArray<Color>(DefaultPixels.Length, Allocator.Persistent);
                        DefaultPixels.CopyTo(pixels);
                    }

                    return pixels;
                }
            }
            private Color[] texturePixels;
            public Color[] TexturePixels
            {
                get
                {
                    if (texturePixels == null)
                    {
                        texturePixels = new Color[Pixels.Length];
                        Pixels.CopyTo(texturePixels);
                    }

                    return texturePixels;
                }
            }

            public void ApplyPixels()
            {
                Jobs.Complete();

                Pixels.CopyTo(TexturePixels);

                if (texture == null) 
                { 
                    texture = new Texture2D(width, height, textureFormat, mipMaps, linear);
                    texture.filterMode = filterMode;
                    texture.wrapMode = wrapMode;
                }

                texture.SetPixels(texturePixels);
                texture.Apply();

                isDirty = false;
            }
            public void ClearPixels()
            {
                DefaultPixels.CopyTo(Pixels);

                MarkAsDirty();
            }

            private JobHandle jobHandle;
            public JobHandle Jobs => jobHandle;

            private readonly List<TextureCombination> combinationStack = new List<TextureCombination>();
            private bool stackIsDirty;
            public bool StackIsDirty => stackIsDirty;
            public void MarkStackAsDirty()
            {
                stackIsDirty = true;
            }
            public void AddToCombinationStack(Texture2D texture, ReferenceChannels referenceChannels, PixelComparisonMethod comparison) => AddToCombinationStack(new TextureCombination() { texture = texture, referenceChannels = referenceChannels, comparison = comparison });
            public void AddToCombinationStack(TextureCombination combo) 
            {
                combinationStack.RemoveAll(i => i.texture == null || i.texture == combo.texture); 
                combinationStack.Add(combo);
                MarkStackAsDirty();
            }
            public void RemoveFromCombinationStack(TextureCombination combo) => RemoveFromCombinationStack(combo.texture);
            public void RemoveFromCombinationStack(Texture2D texture)
            { 
                if (combinationStack.RemoveAll(i => i.texture == null || i.texture == texture) > 0) MarkStackAsDirty();
            }

            public void ApplyCombinationStack(bool clearPixelsFirst = true)
            {
                if (clearPixelsFirst) ClearPixels(); 

                combinationStack.RemoveAll(i => i.texture == null);
                foreach(var combo in combinationStack) Combine(combo);

                stackIsDirty = false; 
            }
            public JobHandle Combine(TextureCombination combo) => Combine(combo.texture, combo.referenceChannels, combo.comparison);
            public JobHandle Combine(Texture2D texture, ReferenceChannels referenceChannels, PixelComparisonMethod comparison) 
            {
                if (texture == null) return jobHandle;
                return Combine(TextureNativePixels.Get(texture), referenceChannels, comparison); 
            }
            public JobHandle Combine(NativeArray<Color> inputPixels, ReferenceChannels referenceChannels, PixelComparisonMethod comparison)
            {
                if (!inputPixels.IsCreated) return jobHandle;
                if (inputPixels.Length != Pixels.Length)
                {
                    swole.LogError($"[{nameof(MaterialMask)}.{nameof(Combine)}] Input pixels were not of the correct length. Expected {this.pixels.Length}, got {inputPixels.Length}.");
                    return jobHandle;
                }

                switch(comparison)
                {

                    case PixelComparisonMethod.Replace:
                        if (   (referenceChannels.channelsR.HasFlag(TextureChannels.R) && !referenceChannels.channelsR.HasFlag(TextureChannels.InvertR)) 
                            && (referenceChannels.channelsR.HasFlag(TextureChannels.G) && !referenceChannels.channelsR.HasFlag(TextureChannels.InvertG))
                            && (referenceChannels.channelsR.HasFlag(TextureChannels.B) && !referenceChannels.channelsR.HasFlag(TextureChannels.InvertB))
                            && (referenceChannels.channelsR.HasFlag(TextureChannels.A) && !referenceChannels.channelsR.HasFlag(TextureChannels.InvertA)))
                        {
                            inputPixels.CopyTo(pixels);
                        } 
                        else
                        {
                            jobHandle = new ReplacePixelsJob() 
                            {
                                referenceChannels = referenceChannels,
                                pixelsToApply = inputPixels,
                                pixels = pixels
                            }.Schedule(pixels.Length, 16, jobHandle);
                        }

                        break;

                    case PixelComparisonMethod.Min:
                        jobHandle = new MinPixelsJob()
                        {
                            referenceChannels = referenceChannels,
                            pixelsToApply = inputPixels,
                            pixels = pixels
                        }.Schedule(pixels.Length, 8, jobHandle);
                        break;

                    case PixelComparisonMethod.Max:
                        jobHandle = new MaxPixelsJob()
                        {
                            referenceChannels = referenceChannels,
                            pixelsToApply = inputPixels,
                            pixels = pixels
                        }.Schedule(pixels.Length, 8, jobHandle);
                        break;

                }

                MarkAsDirty();
                return jobHandle;
            }
        }

        #region Jobs

        [BurstCompile]
        public struct ReplacePixelsJob : IJobParallelFor
        {
            public ReferenceChannels referenceChannels;

            [ReadOnly]
            public NativeArray<Color> pixelsToApply;

            [NativeDisableParallelForRestriction]
            public NativeArray<Color> pixels;

            public float EvaluateChannels(float value, float4 pixel, TextureChannels channels)
            {
                value = math.select(value, pixel.x, (channels & TextureChannels.R) == TextureChannels.R);
                value = math.select(value, pixel.y, (channels & TextureChannels.G) == TextureChannels.G);
                value = math.select(value, pixel.z, (channels & TextureChannels.B) == TextureChannels.B);
                value = math.select(value, pixel.w, (channels & TextureChannels.A) == TextureChannels.A);  

                return value;
            }
            public void Execute(int index)
            {
                float4 pixelF4 = (float4)(Vector4)pixels[index];
                float4 pixelToApplyF4 = (float4)(Vector4)pixelsToApply[index];
                float4 pixelToApplyF4_inverted = 1 - pixelToApplyF4;

                bool4 invertR = ChannelInversionAsBool4(referenceChannels.channelsR); 
                bool4 invertG = ChannelInversionAsBool4(referenceChannels.channelsG);
                bool4 invertB = ChannelInversionAsBool4(referenceChannels.channelsB);
                bool4 invertA = ChannelInversionAsBool4(referenceChannels.channelsA);

                pixelF4.x = EvaluateChannels(pixelF4.x, math.select(pixelToApplyF4, pixelToApplyF4_inverted, invertR), referenceChannels.channelsR);
                pixelF4.y = EvaluateChannels(pixelF4.y, math.select(pixelToApplyF4, pixelToApplyF4_inverted, invertG), referenceChannels.channelsG);
                pixelF4.z = EvaluateChannels(pixelF4.z, math.select(pixelToApplyF4, pixelToApplyF4_inverted, invertB), referenceChannels.channelsB);
                pixelF4.w = EvaluateChannels(pixelF4.w, math.select(pixelToApplyF4, pixelToApplyF4_inverted, invertA), referenceChannels.channelsA);

                pixels[index] = (Color)(Vector4)pixelF4; 
            }
        }
        [BurstCompile]
        public struct MaxPixelsJob : IJobParallelFor
        {
            public ReferenceChannels referenceChannels;

            [ReadOnly]
            public NativeArray<Color> pixelsToApply;

            [NativeDisableParallelForRestriction]
            public NativeArray<Color> pixels;

            public float EvaluateChannels(float value, float4 pixel, TextureChannels channels)
            {
                value = math.max(value, math.select(value, pixel.x, (channels & TextureChannels.R) == TextureChannels.R));
                value = math.max(value, math.select(value, pixel.y, (channels & TextureChannels.G) == TextureChannels.G));
                value = math.max(value, math.select(value, pixel.z, (channels & TextureChannels.B) == TextureChannels.B));
                value = math.max(value, math.select(value, pixel.w, (channels & TextureChannels.A) == TextureChannels.A));

                return value;
            }
            public void Execute(int index)
            {
                float4 pixelF4 = (float4)(Vector4)pixels[index];
                float4 pixelToApplyF4 = (float4)(Vector4)pixelsToApply[index];
                float4 pixelToApplyF4_inverted = 1 - pixelToApplyF4;

                bool4 invertR = ChannelInversionAsBool4(referenceChannels.channelsR);
                bool4 invertG = ChannelInversionAsBool4(referenceChannels.channelsG);
                bool4 invertB = ChannelInversionAsBool4(referenceChannels.channelsB);
                bool4 invertA = ChannelInversionAsBool4(referenceChannels.channelsA);

                pixelF4.x = EvaluateChannels(pixelF4.x, math.select(pixelToApplyF4, pixelToApplyF4_inverted, invertR), referenceChannels.channelsR);
                pixelF4.y = EvaluateChannels(pixelF4.y, math.select(pixelToApplyF4, pixelToApplyF4_inverted, invertG), referenceChannels.channelsG);
                pixelF4.z = EvaluateChannels(pixelF4.z, math.select(pixelToApplyF4, pixelToApplyF4_inverted, invertB), referenceChannels.channelsB);
                pixelF4.w = EvaluateChannels(pixelF4.w, math.select(pixelToApplyF4, pixelToApplyF4_inverted, invertA), referenceChannels.channelsA);

                pixels[index] = (Color)(Vector4)pixelF4;
            }
        }
        [BurstCompile]
        public struct MinPixelsJob : IJobParallelFor
        {
            public ReferenceChannels referenceChannels;

            [ReadOnly]
            public NativeArray<Color> pixelsToApply;

            [NativeDisableParallelForRestriction]
            public NativeArray<Color> pixels;

            public float EvaluateChannels(float value, float4 pixel, TextureChannels channels)
            {
                value = math.min(value, math.select(value, pixel.x, (channels & TextureChannels.R) == TextureChannels.R));
                value = math.min(value, math.select(value, pixel.y, (channels & TextureChannels.G) == TextureChannels.G)); 
                value = math.min(value, math.select(value, pixel.z, (channels & TextureChannels.B) == TextureChannels.B));
                value = math.min(value, math.select(value, pixel.w, (channels & TextureChannels.A) == TextureChannels.A));

                return value;
            }
            public void Execute(int index)
            {
                float4 pixelF4 = (float4)(Vector4)pixels[index];
                float4 pixelToApplyF4 = (float4)(Vector4)pixelsToApply[index];
                float4 pixelToApplyF4_inverted = 1 - pixelToApplyF4;

                bool4 invertR = ChannelInversionAsBool4(referenceChannels.channelsR);
                bool4 invertG = ChannelInversionAsBool4(referenceChannels.channelsG);
                bool4 invertB = ChannelInversionAsBool4(referenceChannels.channelsB);
                bool4 invertA = ChannelInversionAsBool4(referenceChannels.channelsA);

                pixelF4.x = EvaluateChannels(pixelF4.x, math.select(pixelToApplyF4, pixelToApplyF4_inverted, invertR), referenceChannels.channelsR);
                pixelF4.y = EvaluateChannels(pixelF4.y, math.select(pixelToApplyF4, pixelToApplyF4_inverted, invertG), referenceChannels.channelsG);
                pixelF4.z = EvaluateChannels(pixelF4.z, math.select(pixelToApplyF4, pixelToApplyF4_inverted, invertB), referenceChannels.channelsB);
                pixelF4.w = EvaluateChannels(pixelF4.w, math.select(pixelToApplyF4, pixelToApplyF4_inverted, invertA), referenceChannels.channelsA);

                pixels[index] = (Color)(Vector4)pixelF4;
            }
        }

        #endregion

    }
}

#endif