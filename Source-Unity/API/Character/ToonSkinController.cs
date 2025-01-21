#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Events;

namespace Swole.API.Unity
{

    [Serializable]
    public struct MaterialSlotAndProperty
    {
        public int slot;
        public string propertyName;
    }
    public class ToonSkinController : MonoBehaviour
    {

        public int2 testIndex;
        public bool test;
        public void Update()
        {
            if (test)
            {
                test = false;
                SetActiveSkin(testIndex.x, testIndex.y); 
            }
        }

        [Serializable]
        public struct ColorSetting
        {
            public string propertyName;
            public bool isHDR;
            public Color color;
            [ColorUsage(true, true)]
            public Color hdrColor;

            public void Apply(Material material)
            {
                material.SetColor(propertyName, isHDR ? hdrColor : color); 
            }
        }
        [Serializable]
        public struct TextureSetting
        {
            public string propertyName;
            public Texture2D texture;

            public void Apply(Material material)
            {
                material.SetTexture(propertyName, texture); 
            }
        }
        [Serializable]
        public struct RendererMaterialSlot
        {
            public string rendererName;
            public int slot;
        }
        [Serializable]
        public class GlobalMaterialSettings
        {
            public Material targetMaterial;

            public ColorSetting[] colorSettings;
            public TextureSetting[] textureSettings;

            public void Apply() => Apply(targetMaterial);
            public void Apply(Material material)
            {
                if (colorSettings != null)
                {
                    foreach (var setting in colorSettings) setting.Apply(material);
                }
                if (textureSettings != null)
                {
                    foreach (var setting in textureSettings) setting.Apply(material);
                }
            }
        }
        [Serializable]
        public class MaterialSettings
        {
            [Tooltip("Optional material to put in the slot.")]
            public Material materialOverride;
            public bool instantiateMaterial;
            [NonSerialized]
            private Material materialInstance;
            public Material GetMaterial(Material currentMaterial)
            {
                if (!instantiateMaterial) return materialOverride == null ? currentMaterial : materialOverride;
                if (materialInstance == null) materialInstance = Instantiate(materialOverride == null ? currentMaterial : materialOverride);
                return materialInstance; 
            }

            public RendererMaterialSlot[] rendererMaterials;
            public RendererMaterialSlot[] skinnedRendererMaterials;
            public RendererMaterialSlot[] customSkinnedRendererMaterials;

            public ColorSetting[] colorSettings;
            public TextureSetting[] textureSettings;

            public void Apply(Material material)
            {
                if (colorSettings != null)
                {
                    foreach(var setting in colorSettings) setting.Apply(material);
                }
                if (textureSettings != null)
                {
                    foreach (var setting in textureSettings) setting.Apply(material);
                }
            }
        }
        [Serializable]
        public class ParticleSystemSettings
        {
            public ParticleSystem system;

            public Color startColor;

            public void Apply() => Apply(system);
            public void Apply(ParticleSystem system)
            {
                var main = system.main;
                main.startColor = startColor;
            }
        }
        [Serializable]
        public class Palette
        {
            public string name;
            public Color displayColor;
            public MaterialSettings[] materialSettings;
            public GlobalMaterialSettings[] globalMaterialSettings;
            public ParticleSystemSettings[] particleSystemSettings;

            private static List<int> slots = new List<int>();
            public void Apply(MeshRenderer[] renderers, SkinnedMeshRenderer[] skinnedRenderers, CustomSkinnedMeshRenderer[] customSkinnedRenderers)
            {
                if (materialSettings != null)
                {
                    if (renderers != null)
                    {
                        foreach(var renderer in renderers)
                        {
                            if (renderer == null) continue; 

                            Material[] mats = null;
                            foreach (var settings in materialSettings)
                            {
                                if (settings.rendererMaterials == null) continue;
                                slots.Clear();
                                foreach(var matSlot in settings.rendererMaterials)
                                {
                                    if (matSlot.rendererName != renderer.name) continue;
                                    slots.Add(matSlot.slot);
                                }

                                if (slots.Count > 0)
                                {
                                    if (mats == null) mats = renderer.sharedMaterials;
                                    foreach (var slot in slots)
                                    {
                                        if (slot >= 0 && slot < mats.Length)
                                        {
                                            var mat = settings.GetMaterial(mats[slot]);
                                            settings.Apply(mat);
                                            mats[slot] = mat;
                                        }
                                    }
                                }
                            }

                            if (mats != null) renderer.sharedMaterials = mats;
                        }
                    }

                    if (skinnedRenderers != null)
                    {
                        foreach (var renderer in skinnedRenderers)
                        {
                            if (renderer == null) continue;

                            Material[] mats = null;
                            foreach (var settings in materialSettings)
                            {
                                if (settings.skinnedRendererMaterials == null) continue;
                                slots.Clear();
                                foreach (var matSlot in settings.skinnedRendererMaterials)
                                {
                                    if (matSlot.rendererName != renderer.name) continue;
                                    slots.Add(matSlot.slot);
                                }

                                if (slots.Count > 0)
                                {
                                    if (mats == null) mats = renderer.sharedMaterials;
                                    foreach (var slot in slots)
                                    {
                                        if (slot >= 0 && slot < mats.Length)
                                        {
                                            var mat = settings.GetMaterial(mats[slot]);
                                            settings.Apply(mat);
                                            mats[slot] = mat;
                                        }
                                    }
                                }
                            }

                            if (mats != null) renderer.sharedMaterials = mats; 
                        }
                    }

                    if (customSkinnedRenderers != null)
                    {
                        foreach (var renderer in customSkinnedRenderers)
                        {
                            if (renderer == null) continue;

                            Material[] mats = null;
                            foreach (var settings in materialSettings)
                            {
                                if (settings.customSkinnedRendererMaterials == null) continue;
                                slots.Clear();
                                foreach (var matSlot in settings.customSkinnedRendererMaterials)
                                {
                                    if (matSlot.rendererName != renderer.name) continue;
                                    slots.Add(matSlot.slot);
                                }

                                if (slots.Count > 0)
                                {
                                    if (mats == null) mats = renderer.meshRenderer.sharedMaterials; 
                                    foreach (var slot in slots)
                                    {
                                        if (slot >= 0 && slot < mats.Length)
                                        {
                                            var mat = settings.GetMaterial(mats[slot]);
                                            settings.Apply(mat);
                                            mats[slot] = mat;
                                        }
                                    }
                                }
                            }

                            if (mats != null) renderer.meshRenderer.sharedMaterials = mats;
                        }
                    }
                }

                if (globalMaterialSettings != null)
                {
                    foreach(var settings in globalMaterialSettings) settings.Apply();
                }
                if (particleSystemSettings != null)
                {
                    foreach (var settings in particleSystemSettings) settings.Apply();
                }
            }
        }
        [Serializable]
        public class Skin
        {
            public string name;

            public bool isActive;

            public TargetMask[] masksToEdit; 

            public MeshRenderer[] renderers;
            public MaskNameRendererIndexMaterialSlotAndProperty[] maskDestinationsRenderers;

            public SkinnedMeshRenderer[] skinnedRenderers;
            public MaskNameRendererIndexMaterialSlotAndProperty[] maskDestinationsSkinnedRenderers;

            public CustomSkinnedMeshRenderer[] customSkinnedRenderers;
            public MaskNameRendererIndexMaterialSlotAndProperty[] maskDestinationsCustomSkinnedRenderers;

            public string[] clothing;

            public MeshRenderer[] renderersToDisable;
            public SkinnedMeshRenderer[] skinnedRenderersToDisable;
            public CustomSkinnedMeshRenderer[] customSkinnedRenderersToDisable;

            public string[] clothingToDisable;

            public void Reset(ToonClothingController clothingController)
            {
                if (clothingController != null && clothing != null) foreach (var cloth in clothing) clothingController.ResetClothing(cloth);
            }

            public void Activate(int palette, ToonMaterialMaskController maskController, ToonClothingController clothingController, bool reset)
            {
                isActive = true;

                if (masksToEdit != null)
                {
                    foreach(var edit in masksToEdit)
                    {
                        edit.Apply(maskController);
                    }
                }

                if (renderersToDisable != null) foreach (var renderer in renderersToDisable) if (renderer != null) renderer.gameObject.SetActive(false);
                if (skinnedRenderersToDisable != null) foreach (var renderer in skinnedRenderersToDisable) if (renderer != null) renderer.gameObject.SetActive(false);
                if (customSkinnedRenderersToDisable != null) foreach (var renderer in customSkinnedRenderersToDisable) if (renderer != null) renderer.gameObject.SetActive(false);
                if (clothingController != null && clothing != null) foreach (var cloth in clothingToDisable) clothingController.DeactivateClothing(cloth);

                if (renderers != null)
                {
                    foreach (var renderer in renderers) if (renderer != null)
                        {
                            renderer.gameObject.SetActive(true);
                            renderer.enabled = true;
                        }

                    if (maskDestinationsRenderers != null && maskController != null)
                    {
                        foreach (var dest in maskDestinationsRenderers)
                        {
                            if (dest.rendererIndex < 0 || dest.rendererIndex >= renderers.Length || dest.slotProp.slot < 0) continue;
                            var renderer = renderers[dest.rendererIndex];
                            if (renderer == null) continue;

                            var mats = renderer.sharedMaterials;
                            ToonMaterialMaskController.SetMaterialMaskProperties(dest.slotProp, mats, maskController.GetMask(dest.name));
                        }
                    }
                }
                if (skinnedRenderers != null)
                {
                    foreach (var renderer in skinnedRenderers) if (renderer != null)
                        {
                            renderer.gameObject.SetActive(true);
                            renderer.enabled = true;
                        }

                    if (maskDestinationsSkinnedRenderers != null && maskController != null)
                    {
                        foreach (var dest in maskDestinationsSkinnedRenderers)
                        {
                            if (dest.rendererIndex < 0 || dest.rendererIndex >= skinnedRenderers.Length || dest.slotProp.slot < 0) continue;
                            var renderer = skinnedRenderers[dest.rendererIndex];
                            if (renderer == null) continue;

                            var mats = renderer.sharedMaterials;
                            ToonMaterialMaskController.SetMaterialMaskProperties(dest.slotProp, mats, maskController.GetMask(dest.name));
                        }
                    }
                }
                if (customSkinnedRenderers != null)
                {
                    foreach (var renderer in customSkinnedRenderers) if (renderer != null)
                        {
                            renderer.gameObject.SetActive(true);
                            renderer.enabled = true;
                        }

                    if (maskDestinationsCustomSkinnedRenderers != null && maskController != null)
                    {
                        foreach (var dest in maskDestinationsCustomSkinnedRenderers)
                        {
                            if (dest.rendererIndex < 0 || dest.rendererIndex >= customSkinnedRenderers.Length || dest.slotProp.slot < 0) continue;
                            var renderer = customSkinnedRenderers[dest.rendererIndex];
                            if (renderer == null || renderer.meshRenderer == null) continue;

                            var mats = renderer.meshRenderer.sharedMaterials;
                            ToonMaterialMaskController.SetMaterialMaskProperties(dest.slotProp, mats, maskController.GetMask(dest.name));
                        }
                    }
                }

                if (clothingController != null && clothing != null) 
                { 
                    if (reset)
                    {
                        foreach (var cloth in clothing) clothingController.ActivateAndResetClothing(cloth);
                    } 
                    else
                    {
                        foreach (var cloth in clothing) clothingController.ActivateClothing(cloth);
                    }
                }

                ApplyPalette(palette);
            }

            public void Deactivate(ToonMaterialMaskController maskController, ToonClothingController clothingController)
            {
                isActive = false;

                if (clothingController != null && clothing != null) foreach (var cloth in clothing) clothingController.DeactivateClothing(cloth);

                if (masksToEdit != null)
                {
                    foreach (var edit in masksToEdit)
                    {
                        edit.Unapply(maskController);
                    }
                }

                if (renderers != null)
                {
                    foreach (var renderer in renderers) if (renderer != null)
                        {
                            renderer.gameObject.SetActive(false);
                        }

                    if (maskDestinationsRenderers != null && maskController != null)
                    {
                        foreach (var dest in maskDestinationsRenderers)
                        {
                            if (dest.rendererIndex < 0 || dest.rendererIndex >= renderers.Length || dest.slotProp.slot < 0) continue;
                            var renderer = renderers[dest.rendererIndex];
                            if (renderer == null) continue;

                            var mats = renderer.sharedMaterials;
                            ToonMaterialMaskController.SetMaterialMaskProperties(dest.slotProp, mats, null);
                        }
                    }
                }
                if (skinnedRenderers != null)
                {
                    foreach (var renderer in skinnedRenderers) if (renderer != null)
                        {
                            renderer.gameObject.SetActive(false);
                        }

                    if (maskDestinationsSkinnedRenderers != null && maskController != null)
                    {
                        foreach (var dest in maskDestinationsSkinnedRenderers)
                        {
                            if (dest.rendererIndex < 0 || dest.rendererIndex >= skinnedRenderers.Length || dest.slotProp.slot < 0) continue;
                            var renderer = skinnedRenderers[dest.rendererIndex];
                            if (renderer == null) continue;

                            var mats = renderer.sharedMaterials;
                            ToonMaterialMaskController.SetMaterialMaskProperties(dest.slotProp, mats, null);
                        }
                    }
                }
                if (customSkinnedRenderers != null)
                {
                    foreach (var renderer in customSkinnedRenderers) if (renderer != null)
                        {
                            renderer.gameObject.SetActive(false);
                        }

                    if (maskDestinationsCustomSkinnedRenderers != null && maskController != null)
                    {
                        foreach (var dest in maskDestinationsCustomSkinnedRenderers)
                        {
                            if (dest.rendererIndex < 0 || dest.rendererIndex >= customSkinnedRenderers.Length || dest.slotProp.slot < 0) continue;
                            var renderer = customSkinnedRenderers[dest.rendererIndex];
                            if (renderer == null || renderer.meshRenderer == null) continue;

                            var mats = renderer.meshRenderer.sharedMaterials;
                            ToonMaterialMaskController.SetMaterialMaskProperties(dest.slotProp, mats, null);
                        }
                    }
                }
            }

            public Palette defaultPalette;
            public Palette[] alternatePalettes;

            public bool ApplyPalette(int paletteIndex) 
            {
                defaultPalette.Apply(renderers, skinnedRenderers, customSkinnedRenderers);
                if (paletteIndex > 0)
                {
                    paletteIndex = paletteIndex - 1;
                    if (paletteIndex < alternatePalettes.Length)
                    {
                        alternatePalettes[paletteIndex].Apply(renderers, skinnedRenderers, customSkinnedRenderers);
                    }
                    else return false;
                }

                return true;
            }

        }

        public ToonMaterialMaskController maskController;

        public ToonClothingController clothingController;

        public void Awake()
        {
            if (maskController == null) maskController = gameObject.GetComponentInChildren<ToonMaterialMaskController>(true);
            if (clothingController == null) clothingController = gameObject.GetComponentInChildren<ToonClothingController>(true);

            ResetSkins();
        }

        public Skin[] skins;

        private int activeSkin;
        public int ActiveSkinIndex => activeSkin;
        
        private int activePalette;
        public int ActivePaletteIndex => activePalette;

        public Skin ActiveSkin
        {
            get
            {
                if (activeSkin < 0 || skins == null || activeSkin >= skins.Length) return null;
                return skins[activeSkin];
            }
        }

        public void SetActiveSkin(int index) => SetActiveSkin(index, 0);
        public void SetActiveSkin(int index, int palette)
        {
            if (skins == null) return;
            if (index < 0 || index >= skins.Length) index = 0;

            if (activeSkin >= 0 && activeSkin < skins.Length) skins[activeSkin].Deactivate(maskController, clothingController);
            activeSkin = index;
            activePalette = palette;
            skins[activeSkin].Activate(activePalette, maskController, clothingController, false);
            ReactToSkinChange();
        }
        public void SetAndResetActiveSkin(int index) => SetAndResetActiveSkin(index, 0);
        public void SetAndResetActiveSkin(int index, int palette)
        {
            if (skins == null) return;
            if (index < 0 || index >= skins.Length) index = 0;
            
            if (activeSkin >= 0 && activeSkin < skins.Length) skins[activeSkin].Deactivate(maskController, clothingController);
            activeSkin = index;
            activePalette = palette;
            skins[activeSkin].Activate(activePalette, maskController, clothingController, true);
            ReactToSkinChange();
        }

        public void SetPaletteForActiveSkin(int palette)
        {
            var activeSkin = ActiveSkin;
            if (activeSkin == null) return;

            if (activeSkin.ApplyPalette(palette)) activePalette = palette;
            ReactToSkinChange();
        }

        public void ResetSkins()
        {
            if (skins != null)
            {
                foreach (var skin in skins) 
                {
                    skin.Reset(clothingController);
                    skin.Deactivate(maskController, clothingController); 
                }
            }

            SetActiveSkin(0, 0);
        }

        [Serializable]
        public struct SetSkinReaction
        {
            public int skinIndex;
            public bool includePalette;
            public int paletteIndex;
            public UnityEvent OnSetSkin;

            public void React()
            {
                OnSetSkin?.Invoke();
            }
        }

        public SetSkinReaction[] skinReactions;

        public void ReactToSkinChange()
        {
            if (skinReactions == null) return;

            foreach(var reacion in skinReactions)
            {
                if (reacion.skinIndex == activeSkin && (!reacion.includePalette || reacion.paletteIndex == activePalette))
                {
                    reacion.React(); 
                }
            }
        }

    }
}

#endif