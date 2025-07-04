#if (UNITY_EDITOR || UNITY_STANDALONE)

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using UnityEngine.EventSystems;

using Unity.Mathematics;

using TMPro;

#if BULKOUT_ENV
using TriLibCore;
using RLD; // Paid Asset Integration https://assetstore.unity.com/packages/tools/modeling/runtime-level-design-52325
#endif

using Swole.API.Unity;
using Swole.API.Unity.Animation;
using Swole.UI;
using Swole.Morphing;
using Swole.Animation;

namespace Swole
{
    public class ModelEditor : MonoBehaviour
    {

        public bool loadInEditor;
        public bool inEditorResetToBindpose;

        public UnityEngine.AnimationClip clipToLoad;
        public GameObject prefabToLoad;

        #if UNITY_EDITOR
        public void OnValidate()
        {
            if (loadInEditor)
            {
                loadInEditor = false;

                if (prefabToLoad != null && clipToLoad != null)
                {
                    var instance = Instantiate(prefabToLoad, Vector3.zero, Quaternion.identity);

                    var preset = _defaultImportPreset.Duplicate();
                    preset.resetToBindpose = inEditorResetToBindpose;
                    LoadModelAndStartNewSession(preset, instance, (ITransformCurve[] defaultTransformCurves, IPropertyCurve[] defaultPropertyCurves, Transform rootBone, Transform rigContainer) =>
                    {
                          
                        return CustomAnimationConversion.Convert(null, defaultTransformCurves, defaultPropertyCurves, new CustomAnimationConversion.ClipPair[] 
                        { 
                            new CustomAnimationConversion.ClipPair()
                            {
                                mainClip = clipToLoad
                            }
                        }, 1.0f, rootBone == null ? (rigContainer == null ? preset.rootBoneName : rigContainer.name) : rootBone.name, CustomAnimation.DefaultFrameRate, CustomAnimation.DefaultJobCurveSampleRate);

                    }, RegisterSession);
                }
            }
        }
        #endif

        public static string ModelEditorImportPresetsDirectoryPath => Path.Combine(swole.AssetDirectory.FullName, ContentManager.folderNames_Editors, ContentManager.folderNames_ModelEditor, ContentManager.folderNames_Presets);
        public static string ModelEditorRemapPresetsDirectoryPath => Path.Combine(ModelEditorImportPresetsDirectoryPath, ContentManager.folderNames_Remapping);

        private readonly ModelImportSettings _defaultImportPreset = new ModelImportSettings();
        public static List<ModelImportSettings> _importPresets = null;  

        public static void ReloadImportPresets()
        {
            if (_importPresets == null)
            {
                _importPresets = new List<ModelImportSettings>(); 
            }

            _importPresets.Clear();

            //ContentManager.folderNames_Editors
        }

        private readonly RemapPreset _defaultRemapPreset = new RemapPreset("default", "");
        public static List<RemapPreset> _remapPresets = null;

        public static void ReloadRemapPresets()
        {
            if (_remapPresets == null)
            {
                _remapPresets = new List<RemapPreset>();
            }

            _remapPresets.Clear();

            var dir = Directory.CreateDirectory(ModelEditorRemapPresetsDirectoryPath);

            foreach (var file in dir.EnumerateFiles($"*.{ContentManager.fileExtension_JSON}", SearchOption.TopDirectoryOnly).Where(p => p.Extension.Equals($".{ContentManager.fileExtension_JSON}", StringComparison.CurrentCultureIgnoreCase)))
            {
                if (ContentManager.LoadContent(false, default, file, null) is JsonData json)
                {
                    try 
                    {
                        var preset = swole.FromJson<RemapPreset>(json.json);
                        
                        if (preset != null)
                        {
                            _remapPresets.Add(preset);
                        }
                    }
                    catch
                    {
                        swole.LogWarning($"Failed to load invalid json remap preset '{file.Name}'"); 
                    }
                }
            }
        }

        public class AnimationImport
        {
            public CustomAnimation asset;
            public CustomAnimationLayer controlLayer;

            public AnimationImport(CustomAnimation asset, CustomAnimationLayer controlLayer)
            {
                this.asset = asset;
                this.controlLayer = controlLayer;
            }
        }

        private static readonly List<GameObject> _tempGameObjects = new List<GameObject>();
        private static readonly List<Transform> _tempTransforms = new List<Transform>();
        private static readonly List<Transform> _tempTransforms2 = new List<Transform>();
        private static readonly List<TransformState> _tempTransformStates = new List<TransformState>();

        public string additiveEditorScene = "sc_RLD-Add";

        [Tooltip("Primary object used for manipulating the scene.")]
        public RuntimeEditor runtimeEditor;

        public void RefreshRootObjects()
        {
            if (runtimeEditor == null) return;

            _tempGameObjects.Clear();
            var scene = SceneManager.GetActiveScene();
            scene.GetRootGameObjects(_tempGameObjects);
            runtimeEditor.SetExternalRootObjects(_tempGameObjects);
            _tempGameObjects.Clear(); 
        }

        public Text setupErrorTextOutput;
        public TMP_Text setupErrorTextOutputTMP;

        public Sprite icon_bone;
        public Sprite icon_humanoidRig;
        public Sprite icon_mechanicalRig;

        protected Sprite GetRigIcon(AnimatableAsset.ObjectType type)
        {
            switch (type)
            {
                case AnimatableAsset.ObjectType.Humanoid:
                    return icon_humanoidRig;

                case AnimatableAsset.ObjectType.Mechanical:
                    return icon_mechanicalRig;
            }

            return null;
        }

        public Color unboundRemapBoneColor = Color.red;
        public Color boundRemapBoneColor = Color.green;
        public Color unboundTargetBoneColor = Color.cyan; 
        public Color boundTargetBoneColor = Color.yellow;

        protected void ShowSetupError(string msg)
        {

            if (setupErrorTextOutput != null)
            {
                setupErrorTextOutput.gameObject.SetActive(true);
                setupErrorTextOutput.enabled = true; 
                setupErrorTextOutput.text = msg;
            }

            if (setupErrorTextOutputTMP != null)
            {
                setupErrorTextOutputTMP.gameObject.SetActive(true);
                setupErrorTextOutputTMP.enabled = true;
                setupErrorTextOutputTMP.text = msg;
            }

        }

        [Serializable]
        public class WeightedBoneBinding : ICloneable
        {
            public string bone;
            public float weight;

            public float3 boundWorldPosition;
            public float3 boundLocalPosition;
            public quaternion boundWorldRotation;
            public quaternion boundLocalRotation;

            public float3 boundParentWorldPosition;
            public float3 boundParentLocalPosition;
            public quaternion boundParentWorldRotation;
            public quaternion boundParentLocalRotation;

            public WeightedBoneBinding Duplicate()
            {
                var clone = new WeightedBoneBinding();

                clone.bone = bone;
                clone.weight = weight;

                clone.boundWorldPosition = boundWorldPosition;
                clone.boundLocalPosition = boundLocalPosition;
                clone.boundWorldRotation = boundWorldRotation;
                clone.boundLocalRotation = boundLocalRotation;

                clone.boundParentWorldPosition = boundParentWorldPosition;
                clone.boundParentLocalPosition = boundParentLocalPosition;
                clone.boundParentWorldRotation = boundParentWorldRotation;
                clone.boundParentLocalRotation = boundParentLocalRotation;

                return clone;
            }
            public object Clone() => Duplicate();
        }

        [Serializable]
        public class BoneBindings : ICloneable
        {
            public string targetBone;

            [NonSerialized]
            public bool delaySync;

            public bool preserveChildrenAfterRevert;

            public float3 boundWorldPosition;
            public float3 boundLocalPosition;
            public quaternion boundWorldRotation;
            public quaternion boundLocalRotation;

            public float3 boundParentWorldPosition;
            public float3 boundParentLocalPosition;
            public quaternion boundParentWorldRotation;
            public quaternion boundParentLocalRotation;

            public List<WeightedBoneBinding> bindings = new List<WeightedBoneBinding>();

            public BoneBindings Duplicate()
            {
                var clone = new BoneBindings();

                clone.targetBone = targetBone;

                clone.boundWorldPosition = boundWorldPosition;
                clone.boundLocalPosition = boundLocalPosition;
                clone.boundWorldRotation = boundWorldRotation;
                clone.boundLocalRotation = boundLocalRotation;

                clone.boundParentWorldPosition = boundParentWorldPosition;
                clone.boundParentLocalPosition = boundParentLocalPosition;
                clone.boundParentWorldRotation = boundParentWorldRotation;
                clone.boundParentLocalRotation = boundParentLocalRotation;

                clone.bindings = new List<WeightedBoneBinding>();
                foreach (var binding in bindings) clone.bindings.Add(binding.Duplicate());

                return clone;
            }
            public object Clone() => Duplicate();
        }

        [Serializable]
        public enum RootMotionPositionMode
        {
            Default, Planar, Vertical, LeftRight, ForwardBack, LeftRightVertical, ForwardBackVertical
        }

        [Serializable]
        public class RemapPreset : ICloneable
        {
            public string name;
            public string animatable;

            public RemapPreset(string name, string animatable)
            {
                this.name = name;
                this.animatable = animatable;
            }

            // !!!IMPORTANT!!! new fields must be added to the Duplicate() method!

            public bool bakeRootMotion = false;
            public bool bakeRootMotionAtSeparateInverval = false;
            public float bakeRootMotionIntervalPos = 0.1f;
            public float bakeRootMotionIntervalRot = 0.1f;
            public float bakeRootMotionMaxTime = 0.998f;
            public string targetRootBone;
            public RootMotionPositionMode rootMotionPositionMode;

            public List<string> referencePositionBones;
            public bool BakeRootMotionPosition => referencePositionBones != null && referencePositionBones.Count > 0;

            public List<string> referenceRotationBones;
            public bool BakeRootMotionRotation => referenceRotationBones != null && referenceRotationBones.Count > 0;

            public static string GetFileName(string presetName, string animatable) => (string.IsNullOrWhiteSpace(animatable) ? "" : $"{animatable}_") + presetName + $".{ContentManager.fileExtension_JSON}";
            public string FileName => GetFileName(name, animatable);
            public static string GetFilePath(string presetName, string animatable) => GetFilePath(GetFileName(presetName, animatable));
            public static string GetFilePath(string fileName) => Path.Combine(ModelEditorRemapPresetsDirectoryPath, fileName);
            public string FilePath => Path.Combine(ModelEditorRemapPresetsDirectoryPath, FileName);

            public static bool CheckIfPresetFileExists(string presetName, string animatable) => File.Exists(GetFilePath(presetName, animatable));
            public static bool CheckIfPresetFileExists(string fileName) => File.Exists(GetFilePath(fileName));          

            public List<BoneBindings> remapBindings = new List<BoneBindings>();

            public RemapPreset Duplicate()
            {
                var clone = new RemapPreset(name, animatable);

                clone.bakeRootMotion = bakeRootMotion;
                clone.targetRootBone = targetRootBone;
                clone.rootMotionPositionMode = rootMotionPositionMode;

                clone.bakeRootMotionMaxTime = bakeRootMotionMaxTime;

                clone.bakeRootMotionAtSeparateInverval = bakeRootMotionAtSeparateInverval;
                clone.bakeRootMotionIntervalPos = bakeRootMotionIntervalPos;
                clone.bakeRootMotionIntervalRot = bakeRootMotionIntervalRot;

                clone.referencePositionBones = referencePositionBones == null ? null : new List<string>(referencePositionBones);
                clone.referenceRotationBones = referenceRotationBones == null ? null : new List<string>(referenceRotationBones);

                if (remapBindings != null) 
                {
                    clone.remapBindings = new List<BoneBindings>();
                    foreach (var binding in remapBindings) if (binding != null) clone.remapBindings.Add(binding.Duplicate()); 
                }

                return clone;
            }
            public object Clone() => Duplicate();

            public void Clear() => remapBindings?.Clear();

            public bool Save()
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(name)) return false;

                    var json = swole.ToJson(this, true);
                    if (json != null)
                    {
                        File.WriteAllText(FilePath, json);
                    }
                }
                catch (Exception e)
                {
                    swole.LogError(e);

                    return false;
                }

                return true;
            }

            public bool Delete()
            {
                bool flag = false;

                if (_remapPresets != null)
                {
                    _remapPresets.Remove(this);
                    flag = true;
                }

                var filePath = FilePath;
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    flag = true;
                }

                return flag;
            }

            public bool IsSavedPreset => _remapPresets.Contains(this); 

            public bool SaveAsPreset()
            {
                if (!Save()) return false;

                _remapPresets.RemoveAll(i => i.name == name && i.animatable == animatable);
                _remapPresets.Add(this); 

                return true;
            }
        }

        [Serializable]
        public class Session
        {

            public GameObject rootObject;
            public CustomAnimator animator;

            public List<AnimationImport> animationImports = new List<AnimationImport>();
            public List<int> selectedAnimations = new List<int>();

            private bool isPlaying;

            public void StartPlayback(ModelEditor editor)
            {
                if (animationImports == null) return;

                editor.IgnorePreviewObjects();

                foreach (var import in animationImports)
                {
                    if (import.controlLayer == null) continue;

                    import.controlLayer.mix = 0;
                }

                foreach(var index in selectedAnimations)
                {
                    var import = animationImports[index];
                    if (import.controlLayer == null) continue;

                    import.controlLayer.mix = 1;
                    if (import.controlLayer.HasActiveState && import.controlLayer.ActiveState is CustomAnimationLayerState state && state.MotionControllerIndex >= 0)
                    {
                        var mc = import.controlLayer.GetMotionControllerUnsafe(state.MotionControllerIndex);
                        if (mc is AnimationReference ar)
                        {
                            ar.BaseSpeed = 1;  
                        }
                    }
                }

                isPlaying = true;

                if (animator != null)
                {
                    if (animator.OnPostLateUpdate == null) animator.OnPostLateUpdate = new UnityEvent();

                    animator.OnPostLateUpdate.RemoveListener(SyncRemapTargetPoseDefault);
                    animator.OnPostLateUpdate.AddListener(SyncRemapTargetPoseDefault);  
                }
            }

            public void PausePlayback(ModelEditor editor)
            {
                if (animationImports == null) return;

                editor.StopIgnoringPreviewObjects();

                foreach (var import in animationImports)
                {
                    if (import.controlLayer == null) continue;

                    if (import.controlLayer.HasActiveState && import.controlLayer.ActiveState is CustomAnimationLayerState state && state.MotionControllerIndex >= 0)
                    {
                        var mc = import.controlLayer.GetMotionControllerUnsafe(state.MotionControllerIndex);
                        if (mc is AnimationReference ar)
                        {
                            ar.BaseSpeed = 0;
                        }
                    }
                }

                isPlaying = false;

                if (animator != null && animator.OnPostLateUpdate != null)
                {
                    animator.OnPostLateUpdate.RemoveListener(SyncRemapTargetPoseDefault);
                }

                SyncRemapTargetPoseDelayed(2, true, false); 
            }

            public void StopPlayback(ModelEditor editor)
            {
                if (animationImports == null) return;

                editor.StopIgnoringPreviewObjects();

                foreach (var import in animationImports)
                {
                    if (import.controlLayer == null) continue;

                    import.controlLayer.mix = 0;

                    if (import.controlLayer.HasActiveState && import.controlLayer.ActiveState is CustomAnimationLayerState state && state.MotionControllerIndex >= 0)
                    {
                        var mc = import.controlLayer.GetMotionControllerUnsafe(state.MotionControllerIndex);
                        if (mc is AnimationReference ar)
                        {
                            ar.BaseSpeed = 0;
                            ar.SetTime(import.controlLayer, 0);
                        }
                    }
                }

                isPlaying = false;

                if (animator != null && animator.OnPostLateUpdate != null)
                {
                    animator.OnPostLateUpdate.RemoveListener(SyncRemapTargetPoseDefault); 
                }

                SyncRemapTargetPoseDelayed(2, true, false);
            }

            public List<ImportedAnimatable> importedObjects;

            public ImportedAnimatable ImportNewAnimatable(ModelEditor editor, string id) => ImportNewAnimatable(editor, AnimationLibrary.FindAnimatable(id));
            public ImportedAnimatable ImportNewAnimatable(ModelEditor editor, AnimatableAsset animatable)
            {
                if (animatable != null)
                {
                    if (importedObjects == null) importedObjects = new List<ImportedAnimatable>();

                    int i = 0;
                    foreach (var iobj in importedObjects) if (iobj != null && iobj.displayName.AsID().StartsWith(animatable.name.AsID())) i++;

                    var instance = AnimationEditor.AddAnimatableToScene(animatable, i, false);  
                    AddAnimatable(editor, instance);

                    editor.RefreshRootObjects();

                    AnimationEditor.EnableAllProceduralAnimationComponents(instance.instance);
                    return instance;
                }

                return null;
            }

            public void AddAnimatable(ModelEditor editor, ImportedAnimatable animatable)
            {
                if (animatable == null) return;
                if (importedObjects == null) importedObjects = new List<ImportedAnimatable>();

                if (animatable != null && animatable.instance != null)
                {
                    animatable.instance.SetActive(true);
                    importedObjects.Add(animatable);
                }

                
            }

            private ImportedAnimatable remapTarget;
            public ImportedAnimatable RemapTarget => remapTarget;
            private TransformState[] remapDefaultPose;
            public int IndexOfRemapBone(Transform bone)
            {
                if (remapTarget == null || remapTarget.animator == null || remapTarget.animator.Bones == null) return - 1;

                var bones = remapTarget.animator.Bones.bones;
                if (bones != null)
                {
                    for (int a = 0; a < bones.Length; a++) if (ReferenceEquals(bones[a], bone)) return a;
                }

                return -1;
            }
            public bool HasRemapTarget => remapTarget != null;
            private readonly List<string> dependentRemapBones = new List<string>();
            public void SetRemapTarget(ImportedAnimatable remapTarget)
            {
                if (this.remapTarget != null)
                {
                    importedObjects.RemoveAll(i => ReferenceEquals(i, remapTarget));
                    if (this.remapTarget.instance != null) GameObject.Destroy(this.remapTarget.instance);
                }

                this.remapTarget = remapTarget;

                dependentRemapBones.Clear();
                if (remapTarget != null && remapTarget.instance != null && remapTarget.animator != null && remapTarget.animator.Bones != null)
                {
                    remapTarget.animator.ResetToPreInitializedBindPose();

                    var bones = remapTarget.animator.Bones.bones; 

                    if (bones != null)
                    {
                        remapDefaultPose = new TransformState[bones.Length];
                        for (int a = 0; a < bones.Length; a++) remapDefaultPose[a] = new TransformState(bones[a], false); 

                        var proxyTransforms = remapTarget.instance.GetComponentsInChildren<ProxyTransform>(true);
                        var proxyBones = remapTarget.instance.GetComponentsInChildren<ProxyBone>(true);

                        foreach (var bone in bones)
                        {
                            bool isDependent = false;

                            foreach (var proxy in proxyTransforms)
                            {
                                if (proxy.name == bone.name)
                                {
                                    isDependent = true;
                                    break;
                                }
                            }

                            if (!isDependent)
                            {
                                foreach (var proxy in proxyBones)
                                {
                                    if (proxy.bindings != null)
                                    {
                                        foreach (var b in proxy.bindings)
                                        {
                                            if (b.bone != null && b.bone.name == bone.name)
                                            {
                                                isDependent = true; 
                                                break;
                                            }
                                        }
                                    }

                                    if (isDependent) break;
                                }
                            }

                            if (isDependent) dependentRemapBones.Add(bone.name); 
                        }

                        foreach(var bone in bones)
                        {
                            if (dependentRemapBones.Contains(bone.name)) continue;

                            var parent = bone.parent;
                            while(parent != null)
                            {
                                if (dependentRemapBones.Contains(parent.name))
                                {
                                    dependentRemapBones.Add(bone.name);
                                    break;
                                }

                                parent = parent.parent;
                            }
                        }
                    }
                }

                InstantiateRemapPreset();
                remapData.animatable = remapTarget.displayName;
                OnRemapPresetChange.Invoke(remapData);
            }

            public bool IsATargetBone(Transform bone)
            {
                if (animator == null) return false;

                var bone_ = animator.GetUnityBone(bone.name);
                if (ReferenceEquals(bone_, bone)) return true;

                return false;
            }
            public bool IsARemapTargetBone(Transform bone)
            {
                if (remapTarget == null || remapTarget.animator == null) return false;

                var bone_ = remapTarget.animator.GetUnityBone(bone.name);
                if (ReferenceEquals(bone_, bone)) return true;

                return false;
            }

            public void HideObjects()
            {
                if (rootObject != null) rootObject.SetActive(false);
                if (remapTarget != null && remapTarget.instance != null) remapTarget.instance.SetActive(false);

                if (importedObjects != null)
                {
                    foreach (var import in importedObjects)
                    {
                        if (import != null && import.instance != null) import.instance.SetActive(false);
                    }
                }
            }
            public void ShowObjects()
            {
                if (rootObject != null) rootObject.SetActive(true);
                if (remapTarget != null && remapTarget.instance != null) remapTarget.instance.SetActive(true);

                if (importedObjects != null)
                {
                    foreach (var import in importedObjects)
                    {
                        if (import != null && import.instance != null) import.instance.SetActive(true);
                    }
                }
            }

            public bool hideImport = false;
            public bool hideRemap = false;

            private readonly Dictionary<Component, bool> cachedComponentStates = new Dictionary<Component, bool>(); 
            private void SetVisibilityForRoot(GameObject root, bool visible)
            {
                if (root != null)
                {
                    var meshRenderers = root.GetComponentsInChildren<MeshRenderer>(true);
                    foreach (var meshRenderer in meshRenderers)
                    {
                        if (!cachedComponentStates.TryGetValue(meshRenderer, out bool state)) 
                        {
                            state = meshRenderer.enabled;
                            cachedComponentStates[meshRenderer] = state;
                        }

                        meshRenderer.enabled = visible ? state : false;
                    }

                    var skinnedRenderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                    foreach (var skinnedRenderer in skinnedRenderers) 
                    {
                        if (!cachedComponentStates.TryGetValue(skinnedRenderer, out bool state))
                        {
                            state = skinnedRenderer.enabled;
                            cachedComponentStates[skinnedRenderer] = state;
                        }

                        skinnedRenderer.enabled = visible ? state : false;
                    }

                    var customizableCharacters = root.GetComponentsInChildren<CustomizableCharacterMesh>(true);
                    foreach (var customizableCharacter in customizableCharacters) 
                    {
                        if (!cachedComponentStates.TryGetValue(customizableCharacter, out bool state))
                        {
                            state = customizableCharacter.enabled;
                            cachedComponentStates[customizableCharacter] = state;
                        }

                        customizableCharacter.enabled = visible ? state : false;
                    }
                }
            }
            public void RefreshImportVisibility()
            {
                SetVisibilityForRoot(rootObject, !hideImport);
            }
            public void RefreshRemapVisibility()
            {
                SetVisibilityForRoot(remapTarget.instance, !hideRemap); 
            }
            public void RefreshVisibility()
            {
                RefreshImportVisibility();
                RefreshRemapVisibility();
            }
            public void SetImportVisibility(bool visible)
            {
                hideImport = !visible;
                RefreshImportVisibility(); 
            }
            public void SetRemapVisibility(bool visible)
            {
                hideRemap = !visible;
                RefreshRemapVisibility();
            }

            public bool mirrorPoseEditing = true;
            public bool editRemapTargetPose = false;

            public void StartEditingRemapTargetPose(ModelEditor editor)
            {
                StopPlayback(editor);
                
                StopEditingRemapBoneBindings(editor);

                editRemapTargetPose = true;

                if (editor.renderedBonesManager != null) editor.renderedBonesManager.Clear();

                if (remapTarget != null)
                {
                    var animator = remapTarget.animator;//.GetComponentInChildren<CustomAnimator>(true);

                    if (animator != null && animator.Bones != null)
                    {
                        if (animator.Bones != null)
                        {
                            var bones = animator.Bones.bones;
                            if (bones != null)
                            {
                                for (int i = 0; i < bones.Length; i++)
                                {
                                    var bone = bones[i];
                                    if (animator.Bones.IsIKBone(bone)) continue; 

                                    AnimationEditor.TryCreatePoseableBone(animator, bone, editor.boneOverlayLayer, editor.renderedBonesManager, out _);
                                }
                            }
                        }
                    }
                }

                editor.RefreshRootObjects();
            }
            public void StopEditingRemapTargetPose(ModelEditor editor)
            {
                if (editRemapTargetPose)
                {
                    editRemapTargetPose = false;

                    if (editor.renderedBonesManager != null) editor.renderedBonesManager.Clear();

                    editor.RefreshRootObjects();
                }
            }
            public void ToggleEditingRemapTargetPose(ModelEditor editor)
            {
                if (editRemapTargetPose)
                {
                    StopEditingRemapTargetPose(editor);
                }
                else
                {
                    StartEditingRemapTargetPose(editor);
                }
            }

            public bool mirrorBoneBindings = true;
            public bool editRemapBoneBindings = false;
            public string selectedRemapBone = null;

            public void StartEditingRemapBoneBindings(ModelEditor editor)
            {
                StopPlayback(editor);

                StopEditingRemapTargetPose(editor);

                editRemapBoneBindings = true;

                selectedRemapBone = null;

                ResetBoneBindingsSelection(editor);

                editor.RefreshRootObjects();
            }
            public void StopEditingRemapBoneBindings(ModelEditor editor)
            {
                selectedRemapBone = null;

                if (editRemapBoneBindings)
                {
                    editRemapBoneBindings = false;

                    if (editor.renderedBonesManager != null) editor.renderedBonesManager.Clear();

                    editor.RefreshRootObjects();
                }
            }
            public void ToggleEditingRemapBoneBindings(ModelEditor editor)
            {
                if (editRemapBoneBindings)
                {
                    StopEditingRemapBoneBindings(editor);
                }
                else
                {
                    StartEditingRemapBoneBindings(editor);
                }
            }

            public void ResetBoneBindingsSelection(ModelEditor editor)
            {
                if (!editRemapBoneBindings) return;

                selectedRemapBone = null; 

                if (editor.renderedBonesManager != null) editor.renderedBonesManager.Clear();

                if (remapTarget != null)
                {
                    var animator = remapTarget.animator;

                    if (animator != null && animator.Bones != null)
                    {
                        if (animator.Bones != null)
                        {
                            var bones = animator.Bones.bones;
                            if (bones != null)
                            {
                                for (int i = 0; i < bones.Length; i++)
                                {
                                    var bone = bones[i];
                                    if (animator.Bones.IsIKBone(bone)) continue; 

                                    AnimationEditor.TryCreatePoseableBone(animator, bone, editor.boneOverlayLayer, editor.renderedBonesManager, out _, (PoseableRig.BoneInfo boneInfo, Transform bone) =>
                                    {
                                        if (bone == null) return Color.white;

                                        var bindings = GetRemapBindingsForBone(bone.name);
                                        if (bindings != null && bindings.bindings != null && bindings.bindings.Count > 0) return editor.boundRemapBoneColor;

                                        return editor.unboundRemapBoneColor;
                                    });
                                }
                            }
                        }
                    }
                }

                editor.RefreshRootObjects();
            }

            public void SelectRemapBone(ModelEditor editor, string boneName)
            {
                if (!editRemapBoneBindings) return;

                if (editor.renderedBonesManager != null) editor.renderedBonesManager.Clear();

                if (animator != null && animator.Bones != null)
                {
                    if (animator.Bones != null)
                    {
                        var bones = animator.Bones.bones;
                        if (bones != null)
                        {
                            for (int i = 0; i < bones.Length; i++)
                            {
                                var bone = bones[i];
                                if (animator.Bones.IsIKBone(bone)) continue;

                                AnimationEditor.TryCreatePoseableBone(animator, bone, editor.boneOverlayLayer, editor.renderedBonesManager, out _, (PoseableRig.BoneInfo boneInfo, Transform bone) =>
                                {
                                    if (bone == null) return Color.white;

                                    if (TargetBoneHasRemapBindings(bone.name)) return editor.boundTargetBoneColor;

                                    return editor.unboundTargetBoneColor;
                                });
                            }
                        }
                    }
                }

                selectedRemapBone = boneName;

                editor.RefreshRootObjects();
            }

            public void SelectRemapTargetBone(ModelEditor editor, string boneName)
            {
                if (!editRemapBoneBindings) return; 

                void AddBinding(Transform remapBone, Transform targetBone)
                {
                    Vector3 remapParentPos = Vector3.zero;
                    Quaternion remapParentRot = Quaternion.identity; 
                    Vector3 remapParentPosLocal = Vector3.zero;
                    Quaternion remapParentRotLocal = Quaternion.identity;
                    if (remapBone.parent != null)
                    {
                        remapBone.parent.GetPositionAndRotation(out remapParentPos, out remapParentRot);
                        remapBone.parent.GetLocalPositionAndRotation(out remapParentPosLocal, out remapParentRotLocal);
                    }
                    remapBone.GetPositionAndRotation(out var remapPos, out var remapRot);
                    remapBone.GetLocalPositionAndRotation(out var remapPosLocal, out var remapRotLocal);

                    Vector3 parentPos = Vector3.zero;
                    Quaternion parentRot = Quaternion.identity;
                    Vector3 parentPosLocal = Vector3.zero;
                    Quaternion parentRotLocal = Quaternion.identity;
                    if (targetBone.parent != null)
                    {
                        targetBone.parent.GetPositionAndRotation(out parentPos, out parentRot);
                        targetBone.parent.GetLocalPositionAndRotation(out parentPosLocal, out parentRotLocal);
                    }
                    targetBone.GetPositionAndRotation(out var pos, out var rot);
                    targetBone.GetLocalPositionAndRotation(out var lpos, out var lrot);

                    AddRemapBinding(remapBone.name,
                        remapPos, remapRot, remapPosLocal, remapRotLocal,
                        remapParentPos, remapParentRot, remapParentPosLocal, remapParentRotLocal, 

                        new WeightedBoneBinding()
                        {
                            bone = targetBone.name,
                            weight = 1.0f,
                            boundWorldPosition = pos,
                            boundWorldRotation = rot,
                            boundLocalPosition = lpos,
                            boundLocalRotation = lrot,
                            boundParentWorldPosition = parentPos,
                            boundParentWorldRotation = parentRot,
                            boundParentLocalPosition = parentPosLocal,
                            boundParentLocalRotation = parentRotLocal
                        });
                }

                void AddMirrorableBinding(Transform remapBone, Transform targetBone, bool mirror)
                {
                    RemoveAllRemapBindings(remapBone.name);
                    AddBinding(remapBone, targetBone);

                    if (mirror)
                    {
                        string remapBoneNameMirrored = Utils.GetMirroredName(remapBone.name);
                        string targetBoneNameMirrored = Utils.GetMirroredName(targetBone.name);

                        if (remapBoneNameMirrored == remapBone.name || targetBoneNameMirrored == targetBone.name) return;

                        Transform remapBoneMirrored = remapTarget.animator.GetUnityBone(remapBoneNameMirrored);
                        if (remapBoneMirrored == null) return;

                        Transform targetBoneMirrored = animator.GetUnityBone(targetBoneNameMirrored);
                        if (targetBoneMirrored == null) return;

                        RemoveAllRemapBindings(remapBoneMirrored.name);
                        AddBinding(remapBoneMirrored, targetBoneMirrored);
                    }
                }

                if (!string.IsNullOrWhiteSpace(selectedRemapBone))
                {
                    if (remapTarget != null && remapTarget.animator != null)
                    {
                        var remapBone = remapTarget.animator.GetUnityBone(selectedRemapBone);
                        var targetBone = animator.GetUnityBone(boneName);

                        if (remapBone != null && targetBone != null)
                        {
                            AddMirrorableBinding(remapBone, targetBone, mirrorBoneBindings);

                            if (editor.boneBindingsWindow != null && editor.boneBindingsWindow.gameObject.activeInHierarchy) editor.SyncBoneBindingsWindow(this, editor.boneBindingsWindow); 
                        }
                    }
                }
                 
                ResetBoneBindingsSelection(editor);
            }

            private RemapPreset remapData;
            public RemapPreset RemapData
            {
                get => remapData;
                private set
                {
                    remapData = value;
                    OnRemapPresetChange?.Invoke(remapData);
                }
            }
            public bool RemapDataIsDirty => remapData == null || !remapData.IsSavedPreset;
            public void LoadRemapPreset(RemapPreset preset)
            {
                RemapData = preset;

                // delay bone sync for transforms that are dependent on other transforms
                if (remapData.remapBindings != null && dependentRemapBones != null)
                {
                    foreach (var binding in remapData.remapBindings)
                    {
                        binding.delaySync = dependentRemapBones.Contains(binding.targetBone);
                    }
                }

                SyncRemapTargetPoseDelayed(2, true, false);
            }
            public void ValidateRemapPreset()
            {
                if (remapData == null) RemapData = new RemapPreset("new", remapTarget == null ? string.Empty : remapTarget.displayName);
            }
            public void InstantiateRemapPreset()
            { 
                ValidateRemapPreset();
                if (remapData.IsSavedPreset) RemapData = remapData.Duplicate(); 
            }

            public event UnityAction<RemapPreset> OnRemapPresetChange;

            public BoneBindings GetRemapBindingsForBone(string boneName)
            {
                if (remapData == null || remapData.remapBindings == null) return null;

                foreach(var binding in remapData.remapBindings)
                {
                    if (binding.targetBone == boneName) return binding;
                }

                return null;
            }
            public bool TargetBoneHasRemapBindings(string boneName)
            {
                if (remapData == null || remapData.remapBindings == null) return false;

                foreach (var binding in remapData.remapBindings)
                {
                    if (binding.bindings == null) continue;

                    foreach(var b in binding.bindings)
                    {
                        if (b.bone == boneName) return true;
                    }
                }

                return false;
            }

            public void ClearRemapBindings()
            {
                if (remapData == null) return;

                InstantiateRemapPreset();
                remapData.Clear();
            }

            public void AddRemapBinding(string targetRemapBone, float3 boundWorldPosition, quaternion boundWorldRotation, float3 boundLocalPosition, quaternion boundLocalRotation, float3 boundParentWorldPosition, quaternion boundParentWorldRotation, float3 boundParentLocalPosition, quaternion boundParentLocalRotation, WeightedBoneBinding binding)
            {
                InstantiateRemapPreset();

                if (remapData.remapBindings == null) remapData.remapBindings = new List<BoneBindings>();

                BoneBindings bindings = null;

                foreach(var b in remapData.remapBindings)
                {
                    if (b.targetBone == targetRemapBone)
                    {
                        bindings = b;
                        break;
                    }
                } 

                if (bindings == null)
                {
                    bindings = new BoneBindings();
                    bindings.targetBone = targetRemapBone;
                    remapData.remapBindings.Add(bindings);  

                    // delay bone sync for transforms that are dependent on other transforms
                    if (dependentRemapBones != null) bindings.delaySync = dependentRemapBones.Contains(bindings.targetBone);
                }

                if (bindings.bindings == null) bindings.bindings = new List<WeightedBoneBinding>();

                int index = -1;
                for(int i = 0; i < bindings.bindings.Count; i++)
                {
                    if (bindings.bindings[i].bone == binding.bone)
                    {
                        index = i;
                        break;
                    }
                }

                if (index < 0) bindings.bindings.Add(binding); else bindings.bindings[index] = binding;

                bindings.boundWorldPosition = boundWorldPosition;
                bindings.boundWorldRotation = boundWorldRotation;

                bindings.boundLocalPosition = boundLocalPosition;
                bindings.boundLocalRotation = boundLocalRotation;

                bindings.boundParentWorldPosition = boundParentWorldPosition;
                bindings.boundParentWorldRotation = boundParentWorldRotation;

                bindings.boundParentLocalPosition = boundParentLocalPosition;
                bindings.boundParentLocalRotation = boundParentLocalRotation; 
            }

            public void RemoveAllRemapBindings(string targetRemapBone)
            {
                if (remapData == null || remapData.remapBindings == null) return;

                InstantiateRemapPreset();

                foreach (var b in remapData.remapBindings)
                {
                    if (b.targetBone == targetRemapBone)
                    {
                        if (b.bindings != null)
                        {
                            b.bindings.Clear();
                        }

                        break;
                    }
                }
            }
            public void RemoveRemapBinding(string targetRemapBone, string bindingBone)
            {
                if (remapData == null || remapData.remapBindings == null) return;

                InstantiateRemapPreset();

                foreach (var b in remapData.remapBindings)
                {
                    if (b.targetBone == targetRemapBone)
                    {
                        if (b.bindings != null)
                        {
                            b.bindings.RemoveAll(i => i.bone == bindingBone);
                        }

                        break;
                    }
                }
            }

            public struct BoneRemapSync
            {
                public int depth;
                public Transform syncBone;
                public BoneBindings binding;

                public void Sync(Session session, ICollection<Transform> referenceBones, bool revertToDefaultPosition = true, bool revertToDefaultRotation = false)
                {
                    if (binding != null)
                    {

                        var syncBone_ = syncBone;
                        var binding_ = binding;

                        if (binding.bindings != null && binding.bindings.Count > 0)
                        {

                            if (syncBone_.parent != null)
                            {
                                var parentBinding = session.GetRemapBindingsForBone(syncBone_.parent.name);
                                if (parentBinding != null && parentBinding.preserveChildrenAfterRevert)
                                {
                                    revertToDefaultPosition = false;
                                    revertToDefaultRotation = false;
                                } 
                            }

                            Vector3 offset = Vector3.zero;
                            Quaternion rotOffset = Quaternion.identity; 

                            foreach (var bindingToReference in binding.bindings)
                            {
                                foreach (var referenceBone in referenceBones)
                                {
                                    if (referenceBone.name == bindingToReference.bone)
                                    {
                                        Vector3 offset_ = ((Quaternion)bindingToReference.boundParentWorldRotation * (referenceBone.localPosition - (Vector3)bindingToReference.boundLocalPosition));
                                        offset_ = Quaternion.Inverse(binding.boundParentWorldRotation) * offset_; 

                                        offset = offset + offset_ * bindingToReference.weight;

                                        Quaternion rotOffset_ = (((bindingToReference.boundParentWorldRotation * referenceBone.localRotation))) * Quaternion.Inverse(bindingToReference.boundWorldRotation);
                                        rotOffset_ = Quaternion.Inverse(binding.boundParentWorldRotation) * (rotOffset_ * binding.boundWorldRotation);
                                        rotOffset_ = Quaternion.Slerp(Quaternion.identity, rotOffset_ * Quaternion.Inverse(binding.boundLocalRotation), bindingToReference.weight);

                                        rotOffset = rotOffset_ * rotOffset;
                                         
                                        break;
                                    }
                                }
                            }

                            int boneIndex = session.IndexOfRemapBone(syncBone);
                            void SyncTransform(bool revertToDefaultPosition, bool revertToDefaultRotation) 
                            {
                                var localPos = (revertToDefaultPosition && boneIndex >= 0 ? session.remapDefaultPose[boneIndex].position : (Vector3)binding_.boundLocalPosition) + offset;
                                var localRot = rotOffset * (revertToDefaultRotation && boneIndex >= 0 ? session.remapDefaultPose[boneIndex].rotation : (Quaternion)binding_.boundLocalRotation);

                                syncBone_.SetLocalPositionAndRotation(localPos, localRot);
                            }

                            bool preserveChildren = binding.preserveChildrenAfterRevert && (revertToDefaultPosition || revertToDefaultRotation);
                            if (preserveChildren)
                            {
                                SyncTransform(false, false);
                                 
                                _tempTransformStates.Clear();
                                for (int a = 0; a < syncBone.childCount; a++) _tempTransformStates.Add(new TransformState(syncBone.GetChild(a), true));
                            }

                            SyncTransform(revertToDefaultPosition, revertToDefaultRotation);
                            
                            if (preserveChildren)
                            {
                                for (int a = 0; a < _tempTransformStates.Count; a++) _tempTransformStates[a].ApplyWorld(syncBone.GetChild(a));  
                                _tempTransformStates.Clear();
                            }
                        }
                    }
                }
            }

            private readonly List<BoneRemapSync> boneSyncs = new List<BoneRemapSync>();
            private readonly List<BoneRemapSync> boneSyncsPreserve = new List<BoneRemapSync>();
            private readonly List<BoneRemapSync> boneSyncsDelayed = new List<BoneRemapSync>();
            private readonly List<BoneRemapSync> boneSyncsPreserveDelayed = new List<BoneRemapSync>();
            public void SyncRemapTargetPoseDefault() => SyncRemapTargetPose(true, false); 
            public void SyncRemapTargetPose(bool revertToDefaultPositions, bool revertToDefaultRotations)
            {
                if (remapData == null || remapData.remapBindings == null || animator == null || animator.Bones == null || animator.Bones.bones == null || remapTarget == null || remapTarget.animator == null) return;

                boneSyncs.Clear();
                boneSyncsPreserve.Clear();
                boneSyncsDelayed.Clear();
                boneSyncsPreserveDelayed.Clear();

                foreach (var bindings in remapData.remapBindings)
                {
                    var bone = remapTarget.animator.GetUnityBone(bindings.targetBone);
                    if (bone == null) continue;

                    int depth = 0;
                    var parent = bone.parent;
                    while(parent != null)
                    {
                        depth++;
                        parent = parent.parent;
                    }

                    var sync = new BoneRemapSync()
                    {
                        depth = depth,
                        syncBone = bone,
                        binding = bindings
                    };

                    if (bindings.delaySync)
                    {
                        if (bindings.preserveChildrenAfterRevert) boneSyncsPreserveDelayed.Add(sync); else boneSyncsDelayed.Add(sync); 
                    }
                    else 
                    {
                        if (bindings.preserveChildrenAfterRevert) boneSyncsPreserve.Add(sync); else boneSyncs.Add(sync);
                    }
                }
                
                boneSyncs.Sort((BoneRemapSync syncA, BoneRemapSync syncB) => Math.Sign(syncA.depth - syncB.depth));
                foreach (var sync in boneSyncs) sync.Sync(this, animator.Bones.bones, revertToDefaultPositions, revertToDefaultRotations);

                boneSyncsPreserve.Sort((BoneRemapSync syncA, BoneRemapSync syncB) => Math.Sign(syncA.depth - syncB.depth));
                foreach (var sync in boneSyncsPreserve) sync.Sync(this, animator.Bones.bones, revertToDefaultPositions, revertToDefaultRotations);

                if (boneSyncsDelayed.Count > 0)
                {
                    IEnumerator SyncDelayed() 
                    {
                        yield return null;
                        //yield return new WaitForEndOfFrame(); 

                        boneSyncsDelayed.Sort((BoneRemapSync syncA, BoneRemapSync syncB) => Math.Sign(syncA.depth - syncB.depth));
                        foreach (var sync in boneSyncsDelayed) sync.Sync(this, animator.Bones.bones, revertToDefaultPositions, revertToDefaultRotations);

                        boneSyncsPreserveDelayed.Sort((BoneRemapSync syncA, BoneRemapSync syncB) => Math.Sign(syncA.depth - syncB.depth));
                        foreach (var sync in boneSyncsPreserveDelayed) sync.Sync(this, animator.Bones.bones, revertToDefaultPositions, revertToDefaultRotations);
                    }

                    CoroutineProxy.Start(SyncDelayed());
                }
            }

            private IEnumerator SyncRemapTargetPoseRoutine(int frameDelay, bool revertToDefaultPositions, bool revertToDefaultRotations)
            {
                for(int a = 0; a < frameDelay; a++)
                {
                    yield return null;
                }

                SyncRemapTargetPose(revertToDefaultPositions, revertToDefaultRotations);
            }
            public void SyncRemapTargetPoseDelayed(int frameDelay, bool revertToDefaultPositions, bool revertToDefaultRotations)
            {
                if (frameDelay > 0)
                {
                    CoroutineProxy.Start(SyncRemapTargetPoseRoutine(frameDelay, revertToDefaultPositions, revertToDefaultRotations));
                } 
                else
                {
                    SyncRemapTargetPose(revertToDefaultPositions, revertToDefaultRotations);
                }
            }

        }

        public GameObject promptMessageYesNo;
        public GameObject promptMessageYesNoCancel;
        public RectTransform confirmationMessageWindow;

        public RectTransform createPresetWindow;

        public delegate void CreatePresetCallbackDelegate(string presetName);
        public delegate bool CheckIfPresetExistsDelegate(string presetName); 

        public void OpenCreatePresetWindow(CheckIfPresetExistsDelegate checkExists, CreatePresetCallbackDelegate callback, Action cancelCallback) => OpenCreatePresetWindow(createPresetWindow, checkExists, callback, cancelCallback, promptMessageYesNo);
        public static void OpenCreatePresetWindow(RectTransform createPresetWindow, CheckIfPresetExistsDelegate checkExists, CreatePresetCallbackDelegate callback, Action cancelCallback, GameObject promptMessageYesNo)
        {
            UIPopup popup = createPresetWindow.GetComponentInChildren<UIPopup>(true);

            var errorObj = createPresetWindow.FindDeepChildLiberal("error");
            UIPopupMessageFadable errorPopup = null;
            if (errorObj != null) errorPopup = errorObj.GetComponentInChildren<UIPopupMessageFadable>(true);

            CustomEditorUtils.SetButtonOnClickActionByName(createPresetWindow, "confirm", () =>
            {
                void Finalize(string presetName)
                {
                    createPresetWindow.gameObject.SetActive(false);
                    callback.Invoke(presetName); 
                }

                var presetName = CustomEditorUtils.GetInputFieldTextByName(createPresetWindow, "name");
                if (string.IsNullOrWhiteSpace(presetName))
                {
                    if (errorPopup != null) errorPopup.SetDisplayTime(2.0f).SetMessageAndShow("Please enter a name for the preset.");
                    return;
                }

                if (checkExists != null && checkExists(presetName))
                {
                    if (promptMessageYesNo != null)
                    {
                        AnimationEditor.ShowPopupYesNo(promptMessageYesNo, "Overwrite Preset", $"A preset with the name '{presetName}' already exists. Do you want to overwrite it?", () => Finalize(presetName), null);
                        return;
                    }
                }

                Finalize(presetName);
            });

            createPresetWindow.gameObject.SetActive(true);
            createPresetWindow.SetAsLastSibling();
            if (popup != null) popup.Elevate();

            if (cancelCallback != null)
            {
                if (popup.OnClose == null) popup.OnClose = new UnityEvent();
                popup.OnClose.RemoveAllListeners();
                popup.OnClose.AddListener(cancelCallback.Invoke);
            }
        }

        #region Model Importing
        public RectTransform modelImportWindow;

        private ModelImportSettings activeImportSettings;
        public void OpenModelImportWindow()
        {
            if (modelImportWindow == null) return;

            modelImportWindow.gameObject.SetActive(true);
            RefreshModelImportWindow(modelImportWindow, activeImportSettings);
        }
        public void RefreshModelImportWindow(RectTransform modelImportWindow, ModelImportSettings activePreset = null)
        {
            if (activePreset == null) activePreset = _defaultImportPreset;
            activeImportSettings = activePreset.Duplicate();

            var presetObj = modelImportWindow.transform.FindDeepChildLiberal("preset");
            if (presetObj != null)
            {
                var presetDropdown = presetObj.gameObject.GetComponentInChildren<UIDynamicDropdown>(true); 

                presetDropdown.ClearMenuItems();

                foreach(var preset in _importPresets)
                {
                    var preset_ = preset;
                    var presetItem = presetDropdown.CreateNewMenuItem(preset.name);
                    CustomEditorUtils.SetButtonOnClickAction(presetItem, () => { RefreshModelImportWindow(modelImportWindow, preset_); });
                }
            }

            var importBlendshapesObj = modelImportWindow.transform.FindDeepChildLiberal("importBlendshapes");
            if (importBlendshapesObj != null)
            {
                var toggle = importBlendshapesObj.GetComponent<Toggle>();
                if (toggle != null)
                {
                    toggle.SetIsOnWithoutNotify(activePreset.importBlendShapes);
                    if (toggle.onValueChanged == null) toggle.onValueChanged = new Toggle.ToggleEvent(); else toggle.onValueChanged.RemoveAllListeners();
                    toggle.onValueChanged.AddListener((bool val) => activeImportSettings.importBlendShapes = val);
                }
            }

            var importMaterialsObj = modelImportWindow.transform.FindDeepChildLiberal("importMaterials");
            if (importMaterialsObj != null)
            {
                var toggle = importMaterialsObj.GetComponent<Toggle>();
                if (toggle != null)
                {
                    toggle.SetIsOnWithoutNotify(activePreset.importMaterials);
                    if (toggle.onValueChanged == null) toggle.onValueChanged = new Toggle.ToggleEvent(); else toggle.onValueChanged.RemoveAllListeners();
                    toggle.onValueChanged.AddListener((bool val) => activeImportSettings.importMaterials = val);
                }
            }

            var importAnimationObj = modelImportWindow.transform.FindDeepChildLiberal("importAnimations");
            if (importAnimationObj != null)
            {
                var toggle = importAnimationObj.GetComponent<Toggle>();
                if (toggle != null)
                {
                    toggle.SetIsOnWithoutNotify(activePreset.importAnimations);
                    if (toggle.onValueChanged == null) toggle.onValueChanged = new Toggle.ToggleEvent(); else toggle.onValueChanged.RemoveAllListeners();
                    toggle.onValueChanged.AddListener((bool val) => activeImportSettings.importAnimations = val);
                }
            }

            var scaleCompensationObj = modelImportWindow.transform.FindDeepChildLiberal("scaleCompensation");
            if (scaleCompensationObj != null)
            {
                CustomEditorUtils.SetInputFieldOnEndEditAction(scaleCompensationObj, (string valStr) =>
                {
                    if (float.TryParse(valStr, out float scaleCompensation))
                    {
                        activeImportSettings.scaleCompensation = scaleCompensation; 
                    }
                });
            }

            var importObj = modelImportWindow.transform.FindDeepChildLiberal("import");
            if ( importObj != null)
            {
                CustomEditorUtils.SetButtonOnClickAction(importObj, () =>
                {
                    ImportModelFromFile(RegisterSession);
                    modelImportWindow.gameObject.SetActive(false);
                });
            }
        }
        
        public static Transform DeleteAssetImporterDummyTransforms(Transform rootTransform, bool canDeleteRoot = true)
        {
            while(rootTransform != null)
            {
                Transform[] transforms = rootTransform.GetComponentsInChildren<Transform>(); 

                bool flag = true;
                foreach(var t in transforms)
                {
                    if (t == null || (!canDeleteRoot && t == rootTransform)) continue;

                    var tName = t.name.ToLower();

                    if (tName.IndexOf("assimp") > 0) // look for dummy transforms that sometimes get added by Open Asset Import Library
                    {
                        for(int a = 0; a < t.childCount; a++)
                        {
                            var child = t.GetChild(a);
                            child.SetParent(t.parent, true);         
                        }

                        if (t == rootTransform) rootTransform = t.parent;

                        GameObject.DestroyImmediate(t.gameObject);
                        flag = false;
                        break;
                    }
                }

                if (flag) break;
            }

            return rootTransform;
        }



        public delegate void ModelLoadIntoNewSessionDelegate(Session session);
        public delegate CustomAnimationAsset[] ConvertAnimationsDelegate(ITransformCurve[] defaultTransformCurves, IPropertyCurve[] defaultPropertyCurves, Transform rootBone, Transform rigContainer);
        public void LoadModelAndStartNewSession(ModelImportSettings activePreset, GameObject rootGameObject, ConvertAnimationsDelegate convertAnims, ModelLoadIntoNewSessionDelegate callback)
        {
            if (rootGameObject != null)
            {

                Session session = new Session();
                session.rootObject = rootGameObject;

                if (activePreset.deleteDummyTransforms)
                {
                    DeleteAssetImporterDummyTransforms(rootGameObject.transform, false);
                }
                
                if (Mathf.Abs(1.0f - activePreset.scaleCompensation) > 0.0001f)
                {
                    void ScaleMesh(Mesh mesh)
                    {
                        var verts = mesh.vertices;

                        for (int a = 0; a < verts.Length; a++) verts[a] = verts[a] * activePreset.scaleCompensation;

                        mesh.vertices = verts;
                    }

                    Transform[] transforms = rootGameObject.GetComponentsInChildren<Transform>(true);
                    foreach (var t in transforms)
                    {
                        t.localPosition = t.localPosition * activePreset.scaleCompensation;
                    }

                    MeshFilter[] meshFilters = rootGameObject.GetComponentsInChildren<MeshFilter>(true);
                    foreach (var filter in meshFilters)
                    {
                        ScaleMesh(filter.sharedMesh);
                    }

                    SkinnedMeshRenderer[] skinnedRenderers = rootGameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                    foreach (var smr in skinnedRenderers)
                    {
                        ScaleMesh(smr.sharedMesh);

                        var bindposes = smr.sharedMesh.bindposes;
                        if (bindposes != null)
                        {
                            for (int a = 0; a < bindposes.Length; a++)
                            {
                                var bindpose = bindposes[a].inverse;
                                bindpose.SetColumn(3, new Vector4(bindpose.m03 * activePreset.scaleCompensation, bindpose.m13 * activePreset.scaleCompensation, bindpose.m23 * activePreset.scaleCompensation, bindpose.m33));
                                bindposes[a] = bindpose.inverse;
                            }

                            smr.sharedMesh.bindposes = bindposes;
                        }
                    }
                }

                SkinnedMeshRenderer mainSkinnedRenderer = rootGameObject.GetComponentInChildren<SkinnedMeshRenderer>(true);

                if (mainSkinnedRenderer != null)
                {
                    var bones = mainSkinnedRenderer.bones;
                    if (activePreset.resetToBindpose)
                    {
                        var bindposes = mainSkinnedRenderer.sharedMesh.bindposes;

                        if (bindposes != null && bones != null)
                        {
                            for (int a = 0; a < Mathf.Min(bindposes.Length, bones.Length); a++)
                            {
                                var bindpose = bindposes[a].inverse;
                                var bone = bones[a];


                                //bone.localPosition = bindpose.GetPosition();
                                //bone.localRotation = bindpose.rotation;
                                //bone.localScale = bindpose.lossyScale;

                                bone.position = bindpose.GetPosition();
                                bone.rotation = ((float4x4)bindpose).GetRotation();
                            }
                        }
                    }

                    Transform FindBone(string boneName)
                    {
                        if (bones == null) return null;

                        foreach (var bone in bones)
                        {
                            if (bone == null) continue;
                            if (bone.name == boneName) return bone;
                        }

                        boneName = boneName.ToLower().Trim();
                        foreach (var bone in bones)
                        {
                            if (bone == null) continue;
                            if (bone.name.ToLower().Trim() == boneName) return bone;
                        }

                        return null;
                    }

                    Transform rootBone = mainSkinnedRenderer.rootBone;
                    Transform rigContainer = null;

                    if (rootBone == null)
                    {
                        if (!string.IsNullOrWhiteSpace(activePreset.rootBoneName))
                        {
                            rootBone = FindBone(activePreset.rootBoneName);
                        }

                        if (rootBone == null && bones != null && bones.Length > 0) rootBone = bones[0];
                    }

                    if (rigContainer == null)
                    {
                        if (!string.IsNullOrWhiteSpace(activePreset.rigName))
                        {
                            rigContainer = FindBone(activePreset.rigName);
                        }

                        if (rigContainer == null && rootBone != null) rigContainer = rootBone.parent == null ? rootBone : rootBone.parent;
                    }

                    if (activePreset.importAnimations && bones != null && bones.Length > 0)
                    {
                        CustomAvatar avatar = null;
                        if (!string.IsNullOrWhiteSpace(activePreset.avatar))
                        {
                            ResourceLib.ResolveAssetIdString(activePreset.avatar, out var avatarName, out var collectionName, out bool collectionisConfirmedPackage);
                            //avatar = ResourceLib.FindAvatar(avatarName, collectionName, collectionisConfirmedPackage); // TODO: Implement loading avatar assets
                        }

                        if (avatar == null)
                        {
                            avatar = CustomAvatar.NewInstance();

                            avatar.rigContainer = rigContainer == null ? "null" : rigContainer.name;
                            avatar.containerIsRoot = rigContainer == rootBone;
                            if (bones != null)
                            {
                                bool includeRigContainerAsBone = rigContainer != null && activePreset.includeRigContainerAsBone;
                                if (includeRigContainerAsBone)
                                {
                                    for (int a = 0; a < bones.Length; a++)
                                    {
                                        if (bones[a] != null && bones[a].name == avatar.rigContainer) 
                                        { 
                                            includeRigContainerAsBone = false; // already present
                                            break;
                                        }
                                    }
                                }
                                 
                                avatar.bones = new string[bones.Length + (includeRigContainerAsBone ? 1 : 0)];
                                for (int a = 0; a < bones.Length; a++)
                                {
                                    avatar.bones[a] = bones[a] == null ? "" : bones[a].name;
                                }

                                if (includeRigContainerAsBone) avatar.bones[avatar.bones.Length - 1] = avatar.rigContainer;
                            }
                        }

                        CustomAnimationConversion.CreateDefaultCurvesFromPosedObject(avatar, bones, out var defaultA, out var defaultB);
                        var anims = convertAnims(defaultA, defaultB, rootBone, rigContainer);

                        var animator = rootGameObject.AddOrGetComponent<CustomAnimator>();
                        session.animator = animator;
                        animator.avatar = avatar;
                        animator.ReinitializeBindPose(); 

                        var controller = CustomAnimationController.BuildDefaultController("imported", anims, 0);

                        animator.ApplyController(controller);

                        foreach (var anim in anims)
                        {
                            session.animationImports.Add(new AnimationImport(anim.Animation, animator.FindTypedLayer($"imported/{anim.Name}")));
                        }

                        GameObject.Destroy(controller); 
                    }
                }

                callback?.Invoke(session);
            }
        }
        public void ImportModelFromFile(ModelLoadIntoNewSessionDelegate callback)
        {
            var activePreset = activeImportSettings;
            if (activePreset == null) activePreset = _defaultImportPreset;

            void OnLoad(AssetLoaderContext context)
            {
                LoadModelAndStartNewSession(activePreset, context.RootGameObject, (ITransformCurve[] defaultTransformCurves, IPropertyCurve[] defaultPropertyCurves, Transform rootBone, Transform rigContainer) =>
                {
                    return CustomAnimationConversion.Convert(context, defaultTransformCurves, defaultPropertyCurves, rootBone == null ? (rigContainer == null ? activePreset.rootBoneName : rigContainer.name) : rootBone.name, CustomAnimation.DefaultFrameRate, CustomAnimation.DefaultJobCurveSampleRate, activePreset.scaleCompensation);
                }, callback);
            }

            void OnError(IContextualizedError contextualizedError)
            {
                Debug.LogException(contextualizedError.GetInnerException());
            }

            var assetLoaderFilePicker = AssetLoaderFilePicker.Create();
            assetLoaderFilePicker.LoadModelFromFilePickerAsync("Select a Model File", OnLoad, null, null, null, OnError, null, activePreset.GetLoaderOptions());
        }
        #endregion

        #region Animations
        public RectTransform curveEditorWindow;

        public void OpenCurveEditorWindow(EditableAnimationCurve curveToEdit, UnityAction<AnimationCurveEditor.State, AnimationCurveEditor.State> onStateChange) => OpenCurveEditorWindow(curveToEdit, curveEditorWindow, onStateChange);
        public void OpenCurveEditorWindow(EditableAnimationCurve curveToEdit, RectTransform curveEditorWindow, UnityAction<AnimationCurveEditor.State, AnimationCurveEditor.State> onStateChange)
        {
            UIPopup popup = curveEditorWindow.GetComponentInChildren<UIPopup>(true);

            curveEditorWindow.gameObject.SetActive(true);
            curveEditorWindow.SetAsLastSibling();
            if (popup != null) popup.Elevate();

            RefreshCurveEditorWindow(curveToEdit, curveEditorWindow, onStateChange);
        }

        public void RefreshCurveEditorWindow(EditableAnimationCurve curveToEdit, RectTransform curveEditorWindow, UnityAction<AnimationCurveEditor.State, AnimationCurveEditor.State> onStateChange)
        {
            if (curveEditorWindow == null) return;

            var curveEditor = curveEditorWindow.GetComponentInChildren<SwoleCurveEditor>(true);
            if (curveEditor != null)
            {
                curveEditor.SetCurve(curveToEdit);
                curveEditor.Redraw();

                if (onStateChange != null)
                {
                    if (curveEditor.OnStateChange == null) curveEditor.OnStateChange = new UnityEvent<AnimationCurveEditor.State, AnimationCurveEditor.State>();
                    curveEditor.OnStateChange.RemoveAllListeners();
                    curveEditor.OnStateChange.AddListener(onStateChange);
                }
            }
        }

        public RectTransform animationsWindow;

        public void UpdateAnimationsWindow(Session session) => UpdateAnimationsWindow(animationsWindow, session);
        public void UpdateAnimationsWindow(RectTransform animationsWindow, Session session)
        {

            var playT = animationsWindow.FindDeepChildLiberal("play");
            var pauseT = animationsWindow.FindDeepChildLiberal("pause");
            var stopT = animationsWindow.FindDeepChildLiberal("stop");

            if (playT != null)
            {
                CustomEditorUtils.SetButtonOnClickAction(playT, () =>
                {
                    playT.gameObject.SetActive(false);
                    pauseT.gameObject.SetActive(true);
                    session.StartPlayback(this);
                });
            }
            if (pauseT != null)
            {
                CustomEditorUtils.SetButtonOnClickAction(pauseT, () =>
                {
                    playT.gameObject.SetActive(true);
                    pauseT.gameObject.SetActive(false);
                    session.PausePlayback(this);
                });

                pauseT.gameObject.SetActive(false); 
            }
            if (stopT != null)
            {
                CustomEditorUtils.SetButtonOnClickAction(stopT, () =>
                {
                    playT.gameObject.SetActive(true);
                    pauseT.gameObject.SetActive(false);
                    session.StopPlayback(this);
                });
            }

            var list = animationsWindow.GetComponentInChildren<UIRecyclingList>(true);
            list.Clear();

            for (int a = 0; a < session.animationImports.Count; a++)
            {
                var anim = session.animationImports[a];
                int index = a;

                if (anim.asset.timeCurve == null || anim.asset.timeCurve.length < 1)
                {
                    anim.asset.timeCurve = new EditableAnimationCurve();
                    anim.asset.timeCurve.InsertKey(new AnimationCurveEditor.KeyframeStateRaw()
                    {
                        time = 0,
                        value = 0
                    }, 
                    false, false, default, AnimationUtils.InsertAutoSmoothBehaviour.AlwaysLinear);
                    anim.asset.timeCurve.InsertKey(new AnimationCurveEditor.KeyframeStateRaw()
                    {
                        time = 1,
                        value = 1
                    },
                    false, false, default, AnimationUtils.InsertAutoSmoothBehaviour.AlwaysLinear);
                    AnimationUtils.ForceLinear(anim.asset.timeCurve, null, false); 
                }

                list.AddNewMember(anim.asset.Name, null, false, (UIRecyclingList.MemberData memberData, GameObject instance) =>
                {
                    CustomEditorUtils.SetToggleOnValueChangeAction(instance, (bool selected) =>
                    {
                        if (selected)
                        {
                            if (!session.selectedAnimations.Contains(index))
                            {  
                                session.selectedAnimations.Add(index);
                                session.StopPlayback(this);
                            }
                        }
                        else
                        {
                            session.selectedAnimations.Remove(index);
                            session.StopPlayback(this);
                        }
                    });

                    CustomEditorUtils.SetToggleValue(instance, session.selectedAnimations.Contains(index));

                    CustomEditorUtils.SetInputFieldOnEndEditActionByName(instance, "length", (string val) =>
                    {
                        float timeLength = anim.asset.LengthInSeconds * (anim.asset.timeCurve.length <= 1 ? 0f : anim.asset.timeCurve[anim.asset.timeCurve.length - 1].time);

                        if (float.TryParse(val, out float result) && result > 0f && timeLength > 0f)
                        {
                            anim.asset.timeCurve.Scale(result / timeLength);
                            if (curveEditorWindow.gameObject.activeInHierarchy) RefreshCurveEditorWindow(anim.asset.timeCurve, curveEditorWindow, (AnimationCurveEditor.State a, AnimationCurveEditor.State b) =>
                            {
                                CustomEditorUtils.SetInputFieldTextByName(instance, "length", (anim.asset.LengthInSeconds * (b.keyframes.Length <= 1 ? 0f : b.keyframes[b.keyframes.Length - 1].time)).ToString());
                            });
                        } 
                        else
                        {
                            CustomEditorUtils.SetInputFieldTextByName(instance, "length", "0"); 
                        }
                    });
                    CustomEditorUtils.SetInputFieldTextByName(instance, "length", (anim.asset.LengthInSeconds * (anim.asset.timeCurve.length <= 1 ? 0f : anim.asset.timeCurve[anim.asset.timeCurve.length - 1].time)).ToString());

                    CustomEditorUtils.SetButtonOnClickActionByName(instance, "timeCurve", () =>
                    {
                        OpenCurveEditorWindow(anim.asset.timeCurve, curveEditorWindow, (AnimationCurveEditor.State a, AnimationCurveEditor.State b) =>
                        {
                            CustomEditorUtils.SetInputFieldTextByName(instance, "length", (anim.asset.LengthInSeconds * (b.keyframes.Length <= 1 ? 0f : b.keyframes[b.keyframes.Length - 1].time)).ToString()); 
                        });
                    });
                });
            } 

            list.Refresh();
        }

        #endregion

        #region Remapping

        public RectTransform remapWindow;

        public RectTransform boneBindingsWindow;

        public RectTransform boneSelectionWindow;

        public RectTransform rootMotionWindow;

        public void OpenRemapWindow() => OpenRemapWindow(activeSession);
        public void OpenRemapWindow(Session session)
        {
            RefreshRemapWindow(session, remapWindow);
        }

        public void RefreshRemapWindowPreset(Session session) => RefreshRemapWindowPreset(session, remapWindow);
        public void RefreshRemapWindowPreset(Session session, RectTransform remapWindow)
        {
            if (session == null || remapWindow == null) return;

            var presetObj = remapWindow.FindDeepChildLiberal("preset");
            if (presetObj != null)
            {
                bool isValidPreset = !session.RemapDataIsDirty;

                var dropdown = presetObj.GetComponentInChildren<UIDynamicDropdown>(true);
                if (dropdown != null)
                {
                    dropdown.ClearMenuItems();
                    foreach (var preset in _remapPresets)
                    {
                        var preset_ = preset;

                        if (session.RemapTarget == null && !string.IsNullOrWhiteSpace(preset_.animatable)) continue;
                        if (preset_.animatable != session.RemapTarget.displayName) continue; // TODO: Add support for multiple animatables in remap preset (currently only one animatable per preset is supported

                        var presetItem = dropdown.CreateNewMenuItem(preset.name);
                        CustomEditorUtils.SetButtonOnClickAction(presetItem,
                        () => 
                        {
                            session.LoadRemapPreset(preset_);
                            RefreshRemapWindow(session, remapWindow); 
                        });
                    }

                    if (isValidPreset) 
                    {
                        dropdown.SetSelectionText(session.RemapData.name);   
                    } 
                    else 
                    {
                        dropdown.SetSelectionText("...");
                    }
                }

                var buttons = presetObj.FindDeepChildLiberal("buttons");
                if (buttons != null)
                {
                    var deleteObj = presetObj.FindDeepChildLiberal("delete");
                    if (deleteObj != null)
                    {
                        if (isValidPreset)
                        {
                            deleteObj.gameObject.SetActive(true);
                            var preset_ = session.RemapData;
                            CustomEditorUtils.SetButtonOnClickAction(deleteObj, () =>  
                            {
                                if (promptMessageYesNo != null)
                                {
                                    AnimationEditor.ShowPopupYesNo(promptMessageYesNo, "Delete Preset", $"Are you sure you want to delete the preset '{preset_.name}'?", () =>
                                    {
                                        preset_.Delete();
                                        RefreshRemapWindowPreset(session, remapWindow);
                                    }, null);
                                }
                            });
                        }  
                        else
                        {
                            deleteObj.gameObject.SetActive(false);
                        }
                    }

                    var saveObj = remapWindow.FindDeepChildLiberal("save");
                    if (saveObj != null)
                    {
                        if (!isValidPreset)
                        {
                            saveObj.gameObject.SetActive(true);
                            session.ValidateRemapPreset();
                            var preset_ = session.RemapData;
                            CustomEditorUtils.SetButtonOnClickAction(saveObj, () => 
                            {
                                OpenCreatePresetWindow((string presetName) =>
                                {
                                    return RemapPreset.CheckIfPresetFileExists(presetName, session.RemapTarget == null ? string.Empty : session.RemapTarget.displayName);
                                }, 

                                (string presetName) =>
                                {
                                    preset_.name = presetName;
                                    preset_.animatable = session.RemapTarget == null ? string.Empty : session.RemapTarget.displayName;
                                    preset_.SaveAsPreset();

                                    RefreshRemapWindowPreset(session, remapWindow);
                                }, null);

                            });
                        }
                        else
                        {
                            saveObj.gameObject.SetActive(false);
                        }
                    }
                }
            }
        }

        public void RefreshRemapWindow(Session session, RectTransform remapWindow)
        {
            if (session == null || remapWindow == null) return;

            RefreshRemapWindowPreset(session, remapWindow);

            UIPopup popup = remapWindow.GetComponentInChildren<UIPopup>(true);

            CustomEditorUtils.SetButtonOnClickActionByName(remapWindow, "importTarget", () =>
            {
                OpenAnimatableImporter(session, (ImportedAnimatable animatable) => RefreshRemapWindow(session, remapWindow));
            });
            
            var poseEditingObj = remapWindow.FindDeepChildLiberal("poseEditing");
            if (poseEditingObj != null)
            {
                var buttonObj = poseEditingObj.FindDeepChildLiberal("button");
                Transform buttonActiveObj = null;
                if (buttonObj != null)
                {
                    buttonActiveObj = buttonObj.FindDeepChildLiberal("active");
                    buttonActiveObj.gameObject.SetActive(session.editRemapTargetPose && session.HasRemapTarget);

                    CustomEditorUtils.SetButtonOnClickAction(buttonObj, () =>
                    {
                        session.ToggleEditingRemapTargetPose(this);
                        if (buttonActiveObj != null) buttonActiveObj.gameObject.SetActive(session.editRemapTargetPose && session.HasRemapTarget);

                        RefreshRemapWindow(session, remapWindow);
                    });
                    CustomEditorUtils.SetButtonInteractable(buttonObj, session.HasRemapTarget);
                }

                var mirrorObj = poseEditingObj.FindDeepChildLiberal("mirror");
                Transform mirrorActiveObj = null;
                if (mirrorObj != null)
                {
                    mirrorActiveObj = mirrorObj.FindDeepChildLiberal("active");
                    mirrorActiveObj.gameObject.SetActive(session.mirrorPoseEditing && session.HasRemapTarget);

                    CustomEditorUtils.SetButtonOnClickAction(mirrorObj, () =>
                    {
                        session.mirrorPoseEditing = !session.mirrorPoseEditing;
                        if (mirrorActiveObj != null) mirrorActiveObj.gameObject.SetActive(session.mirrorPoseEditing && session.HasRemapTarget);
                    });
                    CustomEditorUtils.SetButtonInteractable(mirrorObj, session.HasRemapTarget);
                }

                var resetObj = poseEditingObj.FindDeepChildLiberal("reset");
                if (resetObj != null)
                {
                    CustomEditorUtils.SetButtonOnClickAction(resetObj, () =>
                    {
                        session.InstantiateRemapPreset();

                        if (session.HasRemapTarget)
                        {
                            if (session.RemapTarget.animator != null)
                            {
                                session.RemapTarget.animator.ResetToPreInitializedBindPose();

                                var bones = session.RemapTarget.animator.Bones.bones;
                                if (bones != null)
                                {
                                    foreach (var remapBone in bones)
                                    {
                                        var bindings = session.GetRemapBindingsForBone(remapBone.name); 
                                        if (bindings != null)
                                        {
                                            Vector3 remapParentPos = Vector3.zero;
                                            Quaternion remapParentRot = Quaternion.identity;
                                            Vector3 remapParentPosLocal = Vector3.zero;
                                            Quaternion remapParentRotLocal = Quaternion.identity;
                                            if (remapBone.parent != null)
                                            {
                                                remapBone.parent.GetPositionAndRotation(out remapParentPos, out remapParentRot);
                                                remapBone.parent.GetLocalPositionAndRotation(out remapParentPosLocal, out remapParentRotLocal);
                                            }
                                            remapBone.GetPositionAndRotation(out var remapPos, out var remapRot);
                                            remapBone.GetLocalPositionAndRotation(out var remapPosLocal, out var remapRotLocal);

                                            bindings.boundWorldPosition = remapPos;
                                            bindings.boundWorldRotation = remapRot;

                                            bindings.boundLocalPosition = remapPosLocal;
                                            bindings.boundLocalRotation = remapRotLocal;

                                            bindings.boundParentWorldPosition = remapParentPos;
                                            bindings.boundParentWorldRotation = remapParentRot;

                                            bindings.boundParentLocalPosition = remapParentPosLocal;
                                            bindings.boundParentLocalRotation = remapParentRotLocal;
                                        }
                                    }
                                }
                            }
                        }
                    });
                    CustomEditorUtils.SetButtonInteractable(resetObj, session.HasRemapTarget);
                }
            }

            var editBindingsObj = remapWindow.FindDeepChildLiberal("editBindings");
            if (editBindingsObj != null)
            {
                var buttonObj = editBindingsObj.FindDeepChildLiberal("button");
                Transform buttonActiveObj = null;
                if (buttonObj != null)
                {
                    buttonActiveObj = buttonObj.FindDeepChildLiberal("active");
                    buttonActiveObj.gameObject.SetActive(session.editRemapBoneBindings && session.HasRemapTarget);

                    CustomEditorUtils.SetButtonOnClickAction(buttonObj, () =>
                    {
                        session.ToggleEditingRemapBoneBindings(this);
                        if (buttonActiveObj != null) buttonActiveObj.gameObject.SetActive(session.editRemapBoneBindings && session.HasRemapTarget);

                        if (session.editRemapBoneBindings)
                        {
                            OpenBoneBindingsWindow();
                        }
                        else
                        {
                            if (boneBindingsWindow != null) boneBindingsWindow.gameObject.SetActive(false);
                            if (boneSelectionWindow != null) boneSelectionWindow.gameObject.SetActive(false);
                        }

                        RefreshRemapWindow(session, remapWindow);
                    });
                    CustomEditorUtils.SetButtonInteractable(buttonObj, session.HasRemapTarget);
                }

                var mirrorObj = editBindingsObj.FindDeepChildLiberal("mirror"); 
                Transform mirrorActiveObj = null;
                if (mirrorObj != null)
                {
                    mirrorActiveObj = mirrorObj.FindDeepChildLiberal("active");
                    mirrorActiveObj.gameObject.SetActive(session.mirrorBoneBindings && session.HasRemapTarget);

                    CustomEditorUtils.SetButtonOnClickAction(mirrorObj, () =>
                    {
                        session.mirrorBoneBindings = !session.mirrorBoneBindings; 
                        if (mirrorActiveObj != null) mirrorActiveObj.gameObject.SetActive(session.mirrorBoneBindings && session.HasRemapTarget);
                    });
                    CustomEditorUtils.SetButtonInteractable(mirrorObj, session.HasRemapTarget);
                }
            }

            var visibilityObj = remapWindow.FindDeepChildLiberal("visibility");
            if (visibilityObj != null)
            {
                var showReferenceObj = visibilityObj.FindDeepChildLiberal("showReference");
                Transform showReferenceActiveObj = null;
                if (showReferenceObj != null)
                {
                    showReferenceActiveObj = showReferenceObj.FindDeepChildLiberal("active");
                    showReferenceActiveObj.gameObject.SetActive(!session.hideImport);

                    CustomEditorUtils.SetButtonOnClickAction(showReferenceObj, () =>
                    {
                        session.SetImportVisibility(session.hideImport); 
                        if (showReferenceActiveObj != null) showReferenceActiveObj.gameObject.SetActive(!session.hideImport);
                    });
                    CustomEditorUtils.SetButtonInteractable(showReferenceObj, session.HasRemapTarget); 
                }

                var showTargetObj = visibilityObj.FindDeepChildLiberal("showTarget");
                Transform showTargetActiveObj = null;
                if (showTargetObj != null)
                { 
                    showTargetActiveObj = showTargetObj.FindDeepChildLiberal("active");
                    showTargetActiveObj.gameObject.SetActive(!session.hideRemap);

                    CustomEditorUtils.SetButtonOnClickAction(showTargetObj, () =>
                    {
                        session.SetRemapVisibility(session.hideRemap);
                        if (showTargetActiveObj != null) showTargetActiveObj.gameObject.SetActive(!session.hideRemap);
                    });
                    CustomEditorUtils.SetButtonInteractable(showTargetObj, session.HasRemapTarget); 
                }
            }

            session.ValidateRemapPreset();
            var preset = session.RemapData;

            var editRootMotionObj = remapWindow.FindDeepChildLiberal("editRootMotion");
            if (editRootMotionObj != null)
            {
                CustomEditorUtils.SetButtonOnClickAction(editRootMotionObj, () =>
                {
                    OpenRootMotionWindow(session, rootMotionWindow);
                });
                CustomEditorUtils.SetButtonInteractable(editRootMotionObj, session.HasRemapTarget && preset.bakeRootMotion);
            }

            var rootMotionToggleObj = remapWindow.FindDeepChildLiberal("rootMotionToggle"); 
            if (rootMotionToggleObj != null)
            {
                var toggle = rootMotionToggleObj.GetComponentInChildren<Toggle>(true);  
                if (toggle != null)
                {
                    toggle.SetIsOnWithoutNotify(preset.bakeRootMotion);

                    if (toggle.onValueChanged == null) toggle.onValueChanged = new Toggle.ToggleEvent(); else toggle.onValueChanged.RemoveAllListeners();
                    toggle.onValueChanged.AddListener((bool val) =>
                    { 
                        session.InstantiateRemapPreset();
                        preset = session.RemapData;
                        preset.bakeRootMotion = val;
                        
                        if (editRootMotionObj != null) CustomEditorUtils.SetButtonInteractable(editRootMotionObj, session.HasRemapTarget && preset.bakeRootMotion);
                    });
                }
            }

            CustomEditorUtils.SetButtonOnClickActionByName(remapWindow, "exportAnimation", () =>
            {
                OpenAnimationBakeWindow(session); 
            });
            
            remapWindow.gameObject.SetActive(true); 
            remapWindow.SetAsLastSibling();  
            if (popup != null) popup.Elevate(); 
        }

        public void OpenRootMotionWindow() => OpenRootMotionWindow(activeSession, rootMotionWindow);

        public void OpenRootMotionWindow(Session session, RectTransform rootMotionWindow)
        {
            if (session == null || rootMotionWindow == null) return;

            UIPopup popup = rootMotionWindow.GetComponentInChildren<UIPopup>(true);

            rootMotionWindow.gameObject.SetActive(true);
            rootMotionWindow.SetAsLastSibling();
            if (popup != null) popup.Elevate();

            RefreshRootMotionWindow(session, rootMotionWindow);
        }

        public void RefreshRootMotionWindow(Session session, RectTransform rootMotionWindow)
        {
            if (session == null || rootMotionWindow == null) return;

            session.ValidateRemapPreset();
            var preset = session.RemapData;

            var targetRootBoneObj = rootMotionWindow.FindDeepChildLiberal("rootBone");
            if (targetRootBoneObj != null)
            {
                CustomEditorUtils.SetComponentText(targetRootBoneObj, string.IsNullOrWhiteSpace(preset.targetRootBone) && session.RemapTarget != null && session.RemapTarget.animator.avatar != null ? session.RemapTarget.animator.avatar.RootBone : preset.targetRootBone);
                CustomEditorUtils.SetButtonOnClickAction(targetRootBoneObj, () =>
                {
                    if (session.RemapTarget != null && session.RemapTarget.animator != null && session.RemapTarget.animator.Bones != null)
                    {
                        OpenBoneSelectWindow(session, session.RemapTarget.animator.Bones.bones, (string boneName) =>
                        {
                            session.InstantiateRemapPreset();
                            preset = session.RemapData;

                            preset.targetRootBone = boneName;

                            CustomEditorUtils.SetComponentText(targetRootBoneObj, string.IsNullOrWhiteSpace(preset.targetRootBone) && session.RemapTarget != null && session.RemapTarget.animator.avatar != null ? session.RemapTarget.animator.avatar.RootBone : preset.targetRootBone);
                        });
                    }
                });
            }

            var rootPositionModeObj = rootMotionWindow.FindDeepChildLiberal("rootPositionMode");
            if (rootPositionModeObj != null)
            {
                var dropdown = rootPositionModeObj.GetComponentInChildren<UIDynamicDropdown>(true);
                if (dropdown != null)
                {
                    dropdown.ClearMenuItems();
                    foreach (var mode in Enum.GetNames(typeof(RootMotionPositionMode)))
                    {
                        dropdown.CreateNewMenuItem(mode);
                    }
                    
                    if (dropdown.OnSelectionChanged == null) dropdown.OnSelectionChanged = new UnityEvent<string>();
                    dropdown.OnSelectionChanged.RemoveAllListeners();
                    dropdown.SetSelectionText(preset.rootMotionPositionMode.ToString(), false); 
                    dropdown.OnSelectionChanged.AddListener((string selection) =>
                    {  
                        if (Enum.TryParse<RootMotionPositionMode>(selection, true, out var result))
                        {
                            session.InstantiateRemapPreset();
                            preset = session.RemapData;
                            preset.rootMotionPositionMode = result;
                        }
                    });
                }
            }

            CustomEditorUtils.SetInputFieldTextByName(rootMotionWindow, "maxTime", preset.bakeRootMotionMaxTime.ToString());
            CustomEditorUtils.SetInputFieldOnEndEditActionByName(rootMotionWindow, "maxTime", (string val) => 
            {
                if (float.TryParse(val, out float result))
                {
                    session.InstantiateRemapPreset();
                    preset = session.RemapData;
                    preset.bakeRootMotionMaxTime = result;
                }
            });

            var bakeIntervalToggleObj = rootMotionWindow.FindDeepChildLiberal("separateInterval");
            if (bakeIntervalToggleObj != null)
            {
                var toggle = bakeIntervalToggleObj.GetComponentInChildren<Toggle>(true);
                if (toggle != null)
                {
                    
                    if (toggle.onValueChanged == null) toggle.onValueChanged = new Toggle.ToggleEvent();
                    toggle.onValueChanged.RemoveAllListeners();
                    toggle.SetIsOnWithoutNotify(preset.bakeRootMotionAtSeparateInverval);
                    toggle.onValueChanged.AddListener((bool val) =>
                    {
                        session.InstantiateRemapPreset();
                        preset = session.RemapData;
                        preset.bakeRootMotionAtSeparateInverval = val;
                    });
                }
            }
            
            CustomEditorUtils.SetInputFieldTextByName(rootMotionWindow, "intervalPos", preset.bakeRootMotionIntervalPos.ToString());
            CustomEditorUtils.SetInputFieldOnEndEditActionByName(rootMotionWindow, "intervalPos", (string val) =>
            {
                if (float.TryParse(val, out float result))
                {
                    session.InstantiateRemapPreset();
                    preset = session.RemapData;
                    preset.bakeRootMotionIntervalPos = result;
                }
            });

            CustomEditorUtils.SetInputFieldTextByName(rootMotionWindow, "intervalRot", preset.bakeRootMotionIntervalRot.ToString());
            CustomEditorUtils.SetInputFieldOnEndEditActionByName(rootMotionWindow, "intervalRot", (string val) =>
            {
                if (float.TryParse(val, out float result))
                {
                    session.InstantiateRemapPreset();
                    preset = session.RemapData;
                    preset.bakeRootMotionIntervalRot = result;  
                }
            });

            var referencePositionBonesObj = rootMotionWindow.FindDeepChildLiberal("rootPositionBones");
            if (referencePositionBonesObj != null)
            {
                var list = referencePositionBonesObj.GetComponentInChildren<UIRecyclingList>(true);
                if (list != null)
                {
                    list.Clear();

                    CustomEditorUtils.SetButtonOnClickActionByName(referencePositionBonesObj, "addBone", () =>
                    {
                        if (session.RemapTarget != null && session.RemapTarget.animator != null && session.RemapTarget.animator.Bones != null)
                        {
                            OpenBoneSelectWindow(session, session.RemapTarget.animator.Bones.bones, (string boneName) =>
                            {
                                session.InstantiateRemapPreset();
                                preset = session.RemapData;

                                if (preset.referencePositionBones == null) preset.referencePositionBones = new List<string>();
                                preset.referencePositionBones.Add(boneName);

                                RefreshRootMotionWindow(session, rootMotionWindow);
                            });
                        }
                    });

                    if (preset.referencePositionBones != null)
                    {
                        foreach (var bone in preset.referencePositionBones)
                        {
                            if (string.IsNullOrWhiteSpace(bone)) continue;

                            list.AddNewMember(bone, null, false, (UIRecyclingList.MemberData memberData, GameObject instance) =>
                            {
                                CustomEditorUtils.SetButtonOnClickActionByName(instance, "remove", () =>
                                {
                                    session.InstantiateRemapPreset();
                                    preset = session.RemapData;

                                    if (preset.referencePositionBones != null) preset.referencePositionBones.Remove(bone);
                                    RefreshRootMotionWindow(session, rootMotionWindow);
                                });
                            });
                        }

                        list.Refresh();
                    }
                }
            }

            var referenceRotationBonesObj = rootMotionWindow.FindDeepChildLiberal("rootRotationBones");
            if (referenceRotationBonesObj != null)
            {
                var list = referenceRotationBonesObj.GetComponentInChildren<UIRecyclingList>(true);
                if (list != null)
                {
                    list.Clear();

                    CustomEditorUtils.SetButtonOnClickActionByName(referenceRotationBonesObj, "addBone", () =>
                    {
                        if (session.RemapTarget != null && session.RemapTarget.animator != null && session.RemapTarget.animator.Bones != null)
                        {
                            OpenBoneSelectWindow(session, session.RemapTarget.animator.Bones.bones, (string boneName) =>
                            {
                                session.InstantiateRemapPreset();
                                preset = session.RemapData;

                                if (preset.referenceRotationBones == null) preset.referenceRotationBones = new List<string>();
                                preset.referenceRotationBones.Add(boneName);

                                RefreshRootMotionWindow(session, rootMotionWindow);
                            });
                        }
                    });

                    if (preset.referenceRotationBones != null)
                    {
                        foreach (var bone in preset.referenceRotationBones)
                        {
                            if (string.IsNullOrWhiteSpace(bone)) continue;

                            list.AddNewMember(bone, null, false, (UIRecyclingList.MemberData memberData, GameObject instance) =>
                            {
                                CustomEditorUtils.SetButtonOnClickActionByName(instance, "remove", () =>
                                {
                                    session.InstantiateRemapPreset();
                                    preset = session.RemapData;

                                    if (preset.referenceRotationBones != null) preset.referenceRotationBones.Remove(bone);
                                    RefreshRootMotionWindow(session, rootMotionWindow);
                                });
                            });
                        }
                         
                        list.Refresh();
                    }
                }
            }
        }

        public RectTransform animatableImporterWindow;

        public delegate void ImportAnimatableDelegate(ImportedAnimatable animatable);

        private List<AnimatableAsset> animatablesCache = new List<AnimatableAsset>(); 
        public void OpenAnimatableImporter() => OpenAnimatableImporter(activeSession);
        public void OpenAnimatableImporter(Session session, ImportAnimatableDelegate importCallback = null)
        {
            if (animatableImporterWindow == null) return;

            UIPopup popup = animatableImporterWindow.GetComponentInChildren<UIPopup>(true);

            UICategorizedList list = animatableImporterWindow.GetComponentInChildren<UICategorizedList>(true);
            if (list != null)
            {
                list.Clear(false);

                animatablesCache.Clear();
                animatablesCache = AnimationLibrary.GetAllAnimatables(animatablesCache);

                foreach (var animatable in animatablesCache)
                {
                    list.AddNewListMember(animatable.name, animatable.type.ToString().ToUpper(), () => {

                        if (popup != null) popup.Close();
                        var obj = session.ImportNewAnimatable(this, animatable); 
                        if (obj != null) 
                        { 
                            session.SetRemapTarget(obj);
                            importCallback?.Invoke(obj);
                        }

                    }, GetRigIcon(animatable.type));
                }
            }

            animatableImporterWindow.gameObject.SetActive(true);
            animatableImporterWindow.SetAsLastSibling();
            if (popup != null) popup.Elevate();
        }

        public void OpenBoneBindingsWindow() => OpenBoneBindingsWindow(activeSession);
        public void OpenBoneBindingsWindow(Session session) => OpenBoneBindingsWindow(session, boneBindingsWindow);
        public void OpenBoneBindingsWindow(Session session, RectTransform boneBindingsWindow)
        {
            if (boneBindingsWindow == null) return;

            UIPopup popup = boneBindingsWindow.GetComponentInChildren<UIPopup>(true);

            boneBindingsWindow.gameObject.SetActive(true);
            boneBindingsWindow.SetAsLastSibling();
            if (popup != null) popup.Elevate();

            SyncBoneBindingsWindow(session, boneBindingsWindow);
        }
        public void SyncBoneBindingsWindow(Session session, RectTransform boneBindingsWindow)
        {
            if (boneBindingsWindow == null || session.animator == null) return;

            var list = boneBindingsWindow.gameObject.GetComponentInChildren<UICategorizedList>(true);
            list.Clear();

            void AddBinding(UICategorizedList.Category category, 
                float3 remapWorldPos, quaternion remapWorldRot, float3 remapLocalPos, quaternion remapLocalRot,
                float3 remapParentWorldPos, quaternion remapParentWorldRot, float3 remapParentLocalPos, quaternion remapParentLocalRot,
                string boneName, WeightedBoneBinding binding, bool addToSession = true)
            {
                if (addToSession) session.AddRemapBinding(category.name, 
                    remapWorldPos, remapWorldRot, remapLocalPos, remapLocalRot,
                    remapParentWorldPos, remapParentWorldRot, remapParentLocalPos, remapParentLocalRot,
                    binding);

                var mem = list.AddNewListMember(boneName, category);

                if (mem.rectTransform != null)
                {
                    CustomEditorUtils.SetButtonOnClickActionByName(mem.rectTransform, "remove", () =>
                    {
                        session.RemoveRemapBinding(category.name, boneName);
                        list.RemoveListMember(mem);  
                    });

                    var weightObj = mem.rectTransform.FindDeepChildLiberal("weight"); 
                    if (weightObj != null)
                    {
                        CustomEditorUtils.SetInputFieldOnEndEditActionByName(weightObj, "input", (string val) =>
                        {
                            if (float.TryParse(val, out float value))
                            {
                                binding.weight = value;
                                CustomEditorUtils.SetSliderValueByName(weightObj, "slider", value);
                            }
                        });

                        CustomEditorUtils.SetSliderOnValueChangeActionByName(weightObj, "slider", (float val) =>
                        {
                            binding.weight = val;
                            CustomEditorUtils.SetInputFieldTextByName(weightObj, "input", val.ToString());  

                        });
                    }
                }
            }

            void AddValidBinding(UICategorizedList.Category category, string bindingBoneName, Transform localBone, Transform referenceBone, float weight)
            {
                Vector3 remapParentPos = Vector3.zero;
                Quaternion remapParentRot = Quaternion.identity;
                Vector3 remapParentPosLocal = Vector3.zero;
                Quaternion remapParentRotLocal = Quaternion.identity;
                if (localBone.parent != null)
                {
                    localBone.parent.GetPositionAndRotation(out remapParentPos, out remapParentRot);
                    localBone.parent.GetLocalPositionAndRotation(out remapParentPosLocal, out remapParentRotLocal);
                }
                localBone.GetPositionAndRotation(out var remapPos, out var remapRot);
                localBone.GetLocalPositionAndRotation(out var remapPosLocal, out var remapRotLocal);

                Vector3 parentPos = Vector3.zero;
                Quaternion parentRot = Quaternion.identity;
                Vector3 parentPosLocal = Vector3.zero;
                Quaternion parentRotLocal = Quaternion.identity;
                if (referenceBone.parent != null)
                {
                    referenceBone.parent.GetPositionAndRotation(out parentPos, out parentRot);
                    referenceBone.parent.GetLocalPositionAndRotation(out parentPosLocal, out parentRotLocal);
                }
                referenceBone.GetPositionAndRotation(out var pos, out var rot);
                referenceBone.GetLocalPositionAndRotation(out var lpos, out var lrot);
                AddBinding(category,
                    remapPos, remapRot, remapPosLocal, remapRotLocal,
                    remapParentPos, remapParentRot, remapParentPosLocal, remapParentRotLocal,
                    bindingBoneName, new WeightedBoneBinding()
                    {
                        bone = bindingBoneName,
                        weight = weight,
                        boundWorldPosition = pos,
                        boundWorldRotation = rot,
                        boundLocalPosition = lpos,
                        boundLocalRotation = lrot,
                        boundParentWorldPosition = parentPos,
                        boundParentWorldRotation = parentRot,
                        boundParentLocalPosition = parentPosLocal,
                        boundParentLocalRotation = parentRotLocal
                    });
            }

            void AddValidMirrorableBinding(Transform[] bones, UICategorizedList.Category category, string bindingBoneName, Transform localBone, Transform referenceBone, float weight, bool mirror)
            {
                AddValidBinding(category, bindingBoneName, localBone, referenceBone, weight);

                if (mirror)
                {
                    string localBoneNameMirrored = Utils.GetMirroredName(localBone.name);
                    string referenceBoneNameMirrored = Utils.GetMirroredName(referenceBone.name);

                    if (localBoneNameMirrored == localBone.name || referenceBoneNameMirrored == referenceBone.name) return;

                    Transform localBoneMirrored = null;
                    for(int a = 0; a < bones.Length; a++)
                    {
                        var bone = bones[a];
                        if (bone == null || bone.name != localBoneNameMirrored) continue;

                        localBoneMirrored = bone;
                        break;
                    }

                    if (localBoneMirrored == null) return;

                    Transform referenceBoneMirrored = session.animator.GetUnityBone(referenceBoneNameMirrored);
                    if (referenceBoneMirrored == null) return;

                    string categoryNameMirrored = Utils.GetMirroredName(category.name);
                    string bindingBoneNameMirror = Utils.GetMirroredName(bindingBoneName);

                    var mirrorCategory = list.AddOrGetCategory(categoryNameMirrored, icon_bone);
                    AddValidBinding(mirrorCategory, bindingBoneNameMirror, localBoneMirrored, referenceBoneMirrored, weight);
                }
            }

            if (session.HasRemapTarget && session.RemapTarget.animator != null && session.RemapTarget.animator.Bones != null)
            {
                var targetBoneNames = new string[session.animator.Bones.bones.Length];
                for (int a = 0; a < targetBoneNames.Length; a++) targetBoneNames[a] = session.animator.Bones.bones[a].name;

                var bones = session.RemapTarget.animator.Bones.bones;
                if (bones != null)
                {

                    foreach (var bone in bones)
                    {
                        var cat = list.AddOrGetCategory(bone.name, icon_bone);  
                        cat.Expand();
                        if (cat.gameObject != null) 
                        {
                            void UpdatePreserveChildrenButton()
                            {
                                var preserveChildrenObj = cat.gameObject.transform.FindDeepChildLiberal("preserveChildren"); 
                                if (preserveChildrenObj != null)
                                {
                                    var bindings = session.GetRemapBindingsForBone(bone.name);

                                    var activeObj = preserveChildrenObj.FindDeepChildLiberal("active");
                                    if (activeObj != null) activeObj.gameObject.SetActive(bindings != null && bindings.preserveChildrenAfterRevert);
                                    
                                    if (bindings == null)
                                    {
                                        CustomEditorUtils.SetButtonInteractable(preserveChildrenObj, false);
                                    }
                                    else
                                    {
                                        CustomEditorUtils.SetButtonOnClickAction(preserveChildrenObj, () =>
                                        {
                                            session.InstantiateRemapPreset();

                                            bindings = session.GetRemapBindingsForBone(bone.name); // refetch after instantiation
                                            bindings.preserveChildrenAfterRevert = !bindings.preserveChildrenAfterRevert;
                                            if (activeObj != null) activeObj.gameObject.SetActive(bindings.preserveChildrenAfterRevert); 
                                        });
                                    }
                                }
                            }

                            UpdatePreserveChildrenButton();

                            CustomEditorUtils.SetButtonOnClickActionByName(cat.gameObject, "addBinding", () =>
                            {
                                OpenBoneSelectWindow(session, session.animator.Bones.bones, (string selection) =>
                                {
                                    var boneTransform = session.animator.GetUnityBone(selection);  

                                    if (boneTransform != null)
                                    {
                                        AddValidMirrorableBinding(bones, cat, selection, bone, boneTransform, 1.0f, session.mirrorBoneBindings);

                                        if (session.editRemapBoneBindings) session.ResetBoneBindingsSelection(this);

                                        UpdatePreserveChildrenButton();
                                    }
                                });
                            });

                            CustomEditorUtils.SetButtonOnClickActionByName(boneBindingsWindow, "autoBind", () =>
                            {
                                var bindings = session.GetRemapBindingsForBone(bone.name);
                                if (bindings != null && bindings.bindings != null && bindings.bindings.Count > 0) return;

                                var output = RigHelpers.MapBones(new string[] { bone.name }, targetBoneNames);

                                if (output.Count > 0)
                                {
                                    var mapping = output[0];
                                    if (mapping.cost < mapping.cleanName1.Length * 0.5f)
                                    {
                                        //Debug.Log($"Auto mapped {bone.name} to {mapping.rig2Bone} with cost {mapping.cost}");

                                        var boneTransform = session.animator.GetUnityBone(mapping.rig2Bone);

                                        var cat = list.AddOrGetCategory(bone.name, icon_bone);
                                        if (boneTransform != null)
                                        {
                                            AddValidMirrorableBinding(bones, cat, mapping.rig2Bone, bone, boneTransform, 1.0f, session.mirrorBoneBindings);

                                            if (session.editRemapBoneBindings) session.ResetBoneBindingsSelection(this);

                                            UpdatePreserveChildrenButton();
                                        }
                                    }
                                }
                            });
                        }

                        var bindings = session.GetRemapBindingsForBone(bone.name); 
                        if (bindings != null)
                        {
                            if (bindings.bindings != null)
                            {
                                foreach (var binding in bindings.bindings) AddBinding(cat, default, default, default, default, default, default, default, default, binding.bone, binding, false); 
                            }
                        }
                    }
                }

                CustomEditorUtils.SetButtonOnClickActionByName(boneBindingsWindow, "autoBind", () =>
                {
                    if (bones != null) 
                    {
                        string[] singleName = new string[1];
                        foreach (var bone in bones)
                        {
                            var bindings = session.GetRemapBindingsForBone(bone.name);
                            if (bindings != null && bindings.bindings != null && bindings.bindings.Count > 0) continue;

                            singleName[0] = bone.name;
                            var output = RigHelpers.MapBones(singleName, targetBoneNames);

                            if (output.Count > 0)
                            {
                                var mapping = output[0];
                                if (mapping.cost < mapping.cleanName1.Length * 0.5f) 
                                {
                                    //Debug.Log($"Auto mapped {bone.name} to {mapping.rig2Bone} with cost {mapping.cost}");

                                    var boneTransform = session.animator.GetUnityBone(mapping.rig2Bone);

                                    var cat = list.AddOrGetCategory(bone.name, icon_bone);
                                    if (boneTransform != null)
                                    {
                                        AddValidMirrorableBinding(bones, cat, mapping.rig2Bone, bone, boneTransform, 1.0f, false);
                                    }
                                }
                            }
                        }

                        if (session.editRemapBoneBindings) session.ResetBoneBindingsSelection(this);
                    }
                });
            } 
            else
            {
                CustomEditorUtils.SetButtonInteractableByName(boneBindingsWindow, "autoBind", false);
            }
        }
         
        public void OpenBoneSelectWindow() => OpenBoneSelectWindow(activeSession, null); 
        public void OpenBoneSelectWindow(Session session, Action<string> callback) => OpenBoneSelectWindow(session, boneSelectionWindow, callback); 
        public void OpenBoneSelectWindow(Session session, IEnumerable<Transform> bones, Action<string> callback) => OpenBoneSelectWindow(session, boneSelectionWindow, bones, callback);
        public void OpenBoneSelectWindow(Session session, IEnumerable<string> bones, Action<string> callback) => OpenBoneSelectWindow(session, boneSelectionWindow, bones, callback);
        public void OpenBoneSelectWindow(Session session, RectTransform boneSelectionWindow, Action<string> callback) 
        {
            if (session.animator != null && session.animator.Bones != null)
            {
                OpenBoneSelectWindow(session, boneSelectionWindow, session.animator.Bones.bones, callback); 
            }
        }
        public void OpenBoneSelectWindow(Session session, RectTransform boneSelectionWindow, IEnumerable<Transform> bones, Action<string> callback)
        {
            if (boneSelectionWindow == null) return;

            UIPopup popup = boneSelectionWindow.GetComponentInChildren<UIPopup>(true);

            boneSelectionWindow.gameObject.SetActive(true);
            boneSelectionWindow.SetAsLastSibling();
            if (popup != null) popup.Elevate();

            SyncBoneSelectWindow(session, boneSelectionWindow, bones, callback);
        }
        public void OpenBoneSelectWindow(Session session, RectTransform boneSelectionWindow, IEnumerable<string> bones, Action<string> callback)
        {
            if (boneSelectionWindow == null) return;

            UIPopup popup = boneSelectionWindow.GetComponentInChildren<UIPopup>(true);

            boneSelectionWindow.gameObject.SetActive(true);
            boneSelectionWindow.SetAsLastSibling();
            if (popup != null) popup.Elevate();

            SyncBoneSelectWindow(session, boneSelectionWindow, bones, callback);
        }
        public void SyncBoneSelectWindow(Session session, RectTransform boneSelectionWindow, IEnumerable<Transform> bones, Action<string> callback)
        {
            if (boneSelectionWindow == null) return;

            var list = boneSelectionWindow.gameObject.GetComponentInChildren<UIRecyclingList>(true);
            list.Clear();

            if (bones != null)
            {
                foreach (var bone in bones)
                {
                    list.AddNewMember(bone.name, () =>
                    {
                        boneSelectionWindow.gameObject.SetActive(false);
                        callback(bone.name);
                    });
                }
                 
                list.Refresh();
            }
        }
        public void SyncBoneSelectWindow(Session session, RectTransform boneSelectionWindow, IEnumerable<string> bones, Action<string> callback)
        {
            if (boneSelectionWindow == null) return;

            var list = boneSelectionWindow.gameObject.GetComponentInChildren<UIRecyclingList>(true);
            list.Clear();

            if (bones != null)
            {
                foreach (var bone in bones)
                {
                    list.AddNewMember(bone, () =>
                    {
                        boneSelectionWindow.gameObject.SetActive(false);
                        callback(bone);
                    });
                }

                list.Refresh();
            }
        }

        public RectTransform animationBakeWindow;

        public void OpenAnimationBakeWindow() => OpenAnimationBakeWindow(activeSession);
        public void OpenAnimationBakeWindow(Session session)
        {
            RefreshAnimationBakeWindow(session, animationBakeWindow);
        }

        public void RefreshAnimationBakeWindow(Session session) => RefreshAnimationBakeWindow(session, animationBakeWindow);
        public void RefreshAnimationBakeWindow(Session session, RectTransform animationBakeWindow)
        {
            if (session == null || animationBakeWindow == null) return;

            UIPopup popup = animationBakeWindow.GetComponentInChildren<UIPopup>(true);

            var errorObj = animationBakeWindow.FindDeepChildLiberal("error");
            UIPopupMessageFadable errorMsg = null;
            if (errorObj != null) errorMsg = errorObj.GetComponentInChildren<UIPopupMessageFadable>(true);

            CustomEditorUtils.SetButtonOnClickActionByName(animationBakeWindow, "start", () =>
            {
                bool openInNewTab = false;
                var openInNewTabObj = animationBakeWindow.FindDeepChildLiberal("openInNewTab");
                if (openInNewTabObj != null)
                {
                    var toggle = openInNewTabObj.GetComponentInChildren<Toggle>(true);
                    if (toggle != null) openInNewTab = toggle.isOn;
                }

                var mainObj = animationBakeWindow.FindDeepChildLiberal("main");
                if (mainObj != null)
                {

                    string animName = CustomEditorUtils.GetInputFieldTextByName(mainObj, "name");
                    if (!ProjectManager.ValidateContentName(animName))
                    {
                        if (errorMsg != null) errorMsg.SetMessageAndShowFor("Invalid animation name!", 1.5f);
                        return;
                    }

                    string pkgName = CustomEditorUtils.GetInputFieldTextByName(mainObj, "package");
                    if (!pkgName.IsPackageName())
                    {
                        if (errorMsg != null) errorMsg.SetMessageAndShowFor("Invalid package name!", 1.5f);
                        return;
                    }

                    if (!ContentManager.TryFindLocalPackage(new PackageIdentifier(pkgName), out var lpkg))
                    {
                        if (errorMsg != null) errorMsg.SetMessageAndShowFor("Package not found!", 1.5f);
                        return;
                    }

                    var bakeType = AnimationBakeType.ResamplePerKey;
                    var bakeTypeObj = mainObj.FindDeepChildLiberal("bakeType");
                    if (bakeTypeObj != null)
                    {
                        var dropdown = bakeTypeObj.GetComponentInChildren<UIDynamicDropdown>(true);
                        if (dropdown != null)
                        {
                            var selectionIndex = dropdown.GetSelectionIndexFromText();
                            if (selectionIndex >= 0) bakeType = (AnimationBakeType)selectionIndex; 
                        }
                        else
                        {
                            var selectionText = CustomEditorUtils.GetComponentTextByName(bakeTypeObj, "current-text").ToLower().RemoveWhitespace();
                            foreach(var type in Enum.GetValues(typeof(AnimationBakeType)))
                            {
                                if (selectionText == type.ToString().ToLower().RemoveWhitespace())
                                {
                                    bakeType = (AnimationBakeType)type; 
                                    break;
                                }
                            }
                        }
                    } 

                    bool bakeFkToIk = true;
                    bool forceBakeIkPosition = true;
                    int samplesPerKey = 1;
                    float resampleIntervalPos = 0.1f;
                    float resampleIntervalRot = 0.1f;
                    float resampleIntervalScale = 0.1f;

                    var bakeFkIkObj = animationBakeWindow.FindDeepChildLiberal("bakeFkIk");
                    if (bakeFkIkObj != null)
                    {
                        var toggle = bakeFkIkObj.GetComponentInChildren<Toggle>(true);
                        if (toggle != null) bakeFkToIk = toggle.isOn; 
                    }

                    var forceBakeIkPosObj = animationBakeWindow.FindDeepChildLiberal("forceBakeFkIkPosition");
                    if (forceBakeIkPosObj != null)
                    {
                        var toggle = forceBakeIkPosObj.GetComponentInChildren<Toggle>(true);
                        if (toggle != null) forceBakeIkPosition = toggle.isOn; 
                    }

                    var bakeOptionsObj = animationBakeWindow.FindDeepChildLiberal("bakeOptions");
                    if (bakeOptionsObj != null)
                    {
                        switch (bakeType)
                        {
                            case AnimationBakeType.ResamplePerKey:
                                var rpk = bakeOptionsObj.FindDeepChildLiberal("rpk");
                                if (rpk != null)
                                {
                                    if (int.TryParse(CustomEditorUtils.GetInputFieldTextByName(rpk, "samplesPerKey"), out var val))
                                    {
                                        samplesPerKey = val;
                                    }
                                }
                                break;

                            case AnimationBakeType.ResampleInterval:
                                var ri = bakeOptionsObj.FindDeepChildLiberal("ri");
                                if (ri != null)
                                {
                                    if (float.TryParse(CustomEditorUtils.GetInputFieldTextByName(ri, "resampleIntervalPos"), out var val))
                                    {
                                        resampleIntervalPos = val;
                                    }
                                    if (float.TryParse(CustomEditorUtils.GetInputFieldTextByName(ri, "resampleIntervalRot"), out val))
                                    {
                                        resampleIntervalRot = val;
                                    }
                                    if (float.TryParse(CustomEditorUtils.GetInputFieldTextByName(ri, "resampleIntervalScale"), out val))
                                    {
                                        resampleIntervalScale = val; 
                                    }
                                }
                                break;
                        }
                    }

                    if (activeSession.selectedAnimations != null && activeSession.selectedAnimations.Count > 0)
                    {
                        List<int> toBake = new List<int>(activeSession.selectedAnimations);
                        List<CustomAnimation> bakedAnims = new List<CustomAnimation>(); 
                        void BakeNext()
                        {
                            if (toBake.Count <= 0) return;

                            var next = toBake[0];
                            toBake.RemoveAt(0);

                            var bake = BeginAnimationBake(activeSession.animationImports[next].asset, animName + (activeSession.selectedAnimations.Count > 1 ? $"_{(activeSession.selectedAnimations.Count - toBake.Count)}" : string.Empty), bakeType, openInNewTabObj && toBake.Count <= 0, (AnimationBake bakeObj) =>
                            {
                                if (bakeObj != null && bakeObj.OutputAnimation != null)
                                {
                                    var pkg = SwolePackage.Create(lpkg);
                                    pkg.AddOrReplace(bakeObj.OutputAnimation);  
                                    lpkg.Content = pkg; 
                                    ContentManager.SavePackage(lpkg);
                                }
                            }, null, bakedAnims);

                            bake.bakeFkToIk = bakeFkToIk;
                            bake.forceBakeIkPosition = forceBakeIkPosition;
                            bake.samplesPerKey = samplesPerKey;
                            bake.resampleIntervalPos = resampleIntervalPos;
                            bake.resampleIntervalRot = resampleIntervalRot;
                            bake.resampleIntervalScale = resampleIntervalScale;
                        }
                         
                        BakeNext();
                    }
                }

                animationBakeWindow.gameObject.SetActive(false);

            });

            animationBakeWindow.gameObject.SetActive(true);
            animationBakeWindow.SetAsLastSibling();
            if (popup != null) popup.Elevate();
        }

        public RectTransform animationBakeProgressBar;
        private UIProgressBar animationBakeProgressBarComp;
        public void SetVisualAnimationBakeProgress(float progress)
        {
            if (animationBakeProgressBar == null) return;

            if (animationBakeProgressBarComp == null) animationBakeProgressBarComp = animationBakeProgressBar.GetComponentInChildren<UIProgressBar>();
            if (animationBakeProgressBarComp == null) return;

            animationBakeProgressBarComp.SetProgress(progress);
        }

        private AnimationBake animationBake;
        public bool IsBakingAnimation => animationBake != null && !animationBake.IsComplete;

        public AnimationBake BeginAnimationBake(CustomAnimation animationToTransfer, string newAnimationName, AnimationBakeType bakeType, bool openInNewSession, Action<AnimationBake> OnComplete = null, Action<AnimationBake> OnCancel = null, List<CustomAnimation> bakedAnimations = null)
        {
            if (activeSession == null || animationToTransfer == null)
            {
                OnCancel?.Invoke(null);
                return null;
            }

            animationBake = new AnimationBake(activeSession.animator, activeSession.RemapTarget.animator, animationToTransfer, activeSession.RemapTarget.RestPose, () => activeSession.SyncRemapTargetPoseDelayed(1, true, false));   
            animationBake.insertionFrameDelay = 3;

            activeSession.ValidateRemapPreset();
            var preset = activeSession.RemapData;
            animationBake.bakeRootMotion = preset.bakeRootMotion;
            animationBake.bakeRootMotionMaxTime = preset.bakeRootMotionMaxTime;
            animationBake.rootMotionPositionMode = preset.rootMotionPositionMode;
            animationBake.bakeRootMotionAtSeparateInterval = preset.bakeRootMotionAtSeparateInverval;
            animationBake.rootMotionBakeIntervalPos = preset.bakeRootMotionIntervalPos; 
            animationBake.rootMotionBakeIntervalRot = preset.bakeRootMotionIntervalRot;

            IEnumerator Bake()
            {
                activeSession.RemapTarget.animator.ResetToPreInitializedBindPose(); 
                activeSession.RemapTarget.animator.enabled = false;
                activeSession.animator.enabled = false; 

                yield return null;

                List<TransformAnimationBakePair> bindings = new List<TransformAnimationBakePair>();
                
                var bones = activeSession.RemapTarget.animator.Bones.bones;
                if (bones != null)
                {
                    for (int a = 0; a < bones.Length; a++) 
                    {
                        var bone = bones[a];
                        if (bone == null) continue;

                        var binding = activeSession.GetRemapBindingsForBone(bone.name);
                        if (binding == null || binding.bindings == null) continue;

                        foreach(var subBinding in binding.bindings) bindings.Add(new TransformAnimationBakePair(subBinding.bone, bone.name)); 
                    }
                }

                animationBake.Initialize(newAnimationName, bakeType, bindings, null, preset.targetRootBone, preset.referencePositionBones, preset.referenceRotationBones); 

                yield return null;

                bool cancel = false;

                if (animationBakeProgressBar != null) 
                { 
                    animationBakeProgressBar.gameObject.SetActive(true);
                    animationBakeProgressBar.SetAsLastSibling();

                    var popup = animationBakeProgressBar.GetComponentInChildren<UIPopup>(true); 
                    if (popup != null)
                    {
                        popup.Elevate();
                        if (popup.OnClose == null) popup.OnClose = new UnityEvent();
                        popup.OnClose.RemoveAllListeners();
                        popup.OnClose.AddListener(() => cancel = true);
                    }
                }
                SetVisualAnimationBakeProgress(0);

                activeSession.animator.enabled = true;

                while (!cancel && !ProgressAnimationBake())
                {                 
                    yield return null;
                }

                if (animationBakeProgressBar != null) animationBakeProgressBar.gameObject.SetActive(false); 

                if (cancel)
                {
                    OnCancel?.Invoke(animationBake);
                } 
                else
                {
                    OnComplete?.Invoke(animationBake);
                }

                activeSession.RemapTarget.animator.enabled = true;
                activeSession.animator.enabled = true;

                if (bakedAnimations != null) bakedAnimations.Add(animationBake.OutputAnimation);

                if (openInNewSession)
                {
                    Session session = new Session();

                    activeSession.RemapTarget.animator.ResetToPreInitializedBindPose();
                    session.rootObject = Instantiate(activeSession.RemapTarget.instance);

                    session.animator = session.rootObject.GetComponentInChildren<CustomAnimator>(true);
                    session.animator.ClearLayers(false);
                    session.animator.enabled = true;
                    session.animator.ResetToPreInitializedBindPose();

                    var controller = CustomAnimationController.BuildDefaultController("imported", bakedAnimations, 0); 
                    session.animator.ApplyController(controller);

                    foreach (var anim in bakedAnimations)
                    {
                        session.animationImports.Add(new AnimationImport(anim, session.animator.FindTypedLayer($"imported/{anim.Name}")));
                    }
                    
                    GameObject.Destroy(controller);

                    RegisterSession(session);
                }
            }

            StartCoroutine(Bake());

            return animationBake;
        }
        public bool ProgressAnimationBake()
        {
            if (activeSession == null || animationBake == null) return true;

            if (animationBake.ProgressBake())
            {
                animationBake.Complete();
                return true;
            }

            SetVisualAnimationBakeProgress(animationBake.Progress);

            return false;
        }

        #endregion

        private List<Session> sessions = new List<Session>();

        public void RegisterSession(Session session)
        {
            if (!sessions.Contains(session)) sessions.Add(session);

            SwitchToSession(session);
        }

        private Session activeSession;
        public void SwitchToSession(Session session)
        {
            if (activeSession != null) 
            { 
                activeSession.StopPlayback(this);
                activeSession.HideObjects();
                activeSession.OnRemapPresetChange -= OnRemapPresetChange; 
            }

            activeSession = session;

            if (activeSession != null)
            {
                activeSession.OnRemapPresetChange += OnRemapPresetChange;

                activeSession.ShowObjects();

                if (animationsWindow != null)
                {
                    if (activeSession.animationImports != null && activeSession.animationImports.Count > 0)
                    {
                        animationsWindow.gameObject.SetActive(true);
                        UpdateAnimationsWindow(activeSession);
                    }
                    else
                    {
                        animationsWindow.gameObject.SetActive(false);
                    }
                }

                if (remapWindow != null)
                {
                    if (activeSession.RemapTarget != null)
                    {
                        remapWindow.gameObject.SetActive(true);
                        RefreshRemapWindow(activeSession, remapWindow);
                    }
                    else
                    {
                        remapWindow.gameObject.SetActive(false);
                    }
                }
            } 
            else
            {
                if (animationsWindow != null) animationsWindow.gameObject.SetActive(false);
                if (remapWindow != null) remapWindow.gameObject.SetActive(false);
            }

            if (animationBakeWindow != null) animationBakeWindow.gameObject.SetActive(false);
        }
        private void OnRemapPresetChange(RemapPreset preset)
        {
            RefreshRemapWindowPreset(activeSession);
        }

        [Header("Bone Rendering")]
        public int boneOverlayLayer = 29;

        public GameObject boneRootPrototype;
        public GameObject boneLeafPrototype;

        private AnimationEditor.RenderedBonesManager renderedBonesManager;

        [Header("Events"), SerializeField]
        private UnityEvent OnSetupSuccess = new UnityEvent();
        [SerializeField]
        private UnityEvent OnSetupFail = new UnityEvent();

        private bool initialized;
        protected void Awake()
        {
            ReloadImportPresets();
            ReloadRemapPresets();

            //UnitySceneLocalData

            void FailSetup(string msg)
            {
                swole.LogError(msg);
                OnSetupFail?.Invoke();
                ShowSetupError(msg);
                Destroy(this);
            }

            if (renderedBonesManager == null)
            {
                renderedBonesManager = new AnimationEditor.RenderedBonesManager(boneRootPrototype, boneLeafPrototype, null, null);
            }

            if (string.IsNullOrEmpty(additiveEditorScene))
            {
                string msg = "No additive editor scene was set. There will be no way to control the scene camera or to load and manipulate prefabs without it!";
                swole.LogWarning(msg);
                //OnSetupFail?.Invoke();
                //ShowSetupError(msg);
            }
            else
            {

                try
                {

                    Camera mainCam = Camera.main;
                    EventSystem[] eventSystems = GameObject.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);

                    SceneManager.LoadScene(additiveEditorScene, LoadSceneMode.Additive);
                    var scn = SceneManager.GetSceneByName(additiveEditorScene);
                    if (scn.IsValid())
                    {

                        bool hadExistingCamera = false;
                        Vector3 existingCameraPos = Vector3.zero;
                        Quaternion existingCameraRot = Quaternion.identity;
                        if (mainCam != null)
                        {
                            hadExistingCamera = true;
                            var camT = mainCam.transform;
                            existingCameraPos = camT.position;
                            existingCameraRot = camT.rotation;
                            Destroy(mainCam.gameObject); // Make room for new main camera in setup scene
                        }
                        foreach (var system in eventSystems) if (system != null) Destroy(system.gameObject); // Remove any existing event systems and use the one in the setup scene

                        IEnumerator FindRuntimeEditor()
                        {
                            while (!scn.isLoaded)
                            {
                                yield return null;
                                scn = SceneManager.GetSceneByBuildIndex(scn.buildIndex);
                            }

                            if (runtimeEditor == null) runtimeEditor = GameObject.FindFirstObjectByType<RuntimeEditor>();
                            if (runtimeEditor == null)
                            {
                                FailSetup("No RuntimeEditor object was found! Objects cannot be edited without it. The additive editor setup scene should contain one.");
                            }
                            else
                            {
                                runtimeEditor.OnPreSelect += OnPreSelectCustomize;  
                                //runtimeEditor.OnSelectionChanged += OnSelectionChange;
                                //runtimeEditor.OnBeginManipulateTransforms += BeginManipulationAction;
                                runtimeEditor.OnManipulateTransformsStep += ManipulationActionStep;
                                runtimeEditor.OnManipulateTransforms += ManipulationActionEnd;

                                runtimeEditor.DisableGrid = true;
                                runtimeEditor.DisableUndoRedo = true;
                                runtimeEditor.DisableGroupSelect = true;
                                runtimeEditor.DisableSelectionBoundingBox = true;

#if BULKOUT_ENV
                                var rtSelect = RTObjectSelection.Get;
                                if (rtSelect != null)
                                {
                                    rtSelect.Settings.SetObjectLayerSelectable(boneOverlayLayer, true); // Make bone overlay layer selectable 
                                }
#endif

                                if (hadExistingCamera)
                                { 
                                    mainCam = Camera.main;
                                    if (mainCam != null)
                                    {
                                        var camT = mainCam.transform;
                                        camT.SetPositionAndRotation(existingCameraPos, existingCameraRot);
                                    }
                                }

                                initialized = true;
                                OnSetupSuccess.Invoke();
                            }

                        }

                        StartCoroutine(FindRuntimeEditor()); // Wait for scene to fully load, then find the runtime editor.

                    }
                    else
                    {
                        ShowSetupError($"Invalid scene name '{additiveEditorScene}'");
                    }

                }
                catch (Exception ex)
                {
                    ShowSetupError(ex.Message);
                    throw ex;
                }

            }
        }

        protected virtual void Update()
        {
            if (activeSession != null)
            {
                if (activeSession.editRemapBoneBindings && !string.IsNullOrWhiteSpace(activeSession.selectedRemapBone))
                {
                    if (InputProxy.CursorSecondaryButton) 
                    { 
                        activeSession.ResetBoneBindingsSelection(this);
                        if (runtimeEditor != null) runtimeEditor.DeselectAll(); 
                    }
                }
            }
        }

        protected virtual void OnPreSelectCustomize(int selectReason, List<GameObject> toSelect, List<GameObject> toIgnore)
        {
            AnimationEditor.OnPreSelectCustomize(selectReason, toSelect, toIgnore);

            if (activeSession == null || !activeSession.editRemapBoneBindings) return;
            
            foreach(var selection in toSelect)
            {
                if (string.IsNullOrWhiteSpace(activeSession.selectedRemapBone))
                {
                    if (activeSession.IsARemapTargetBone(selection.transform))
                    {
                        activeSession.SelectRemapBone(this, selection.name);
                        break;
                    }
                } 
                else
                {
                    if (activeSession.IsATargetBone(selection.transform))
                    {
                        activeSession.SelectRemapTargetBone(this, selection.name); 
                        break;
                    }
                }
            }

            toSelect.Clear(); 
        }

        protected virtual void ManipulationActionStep(List<Transform> affectedTransforms, Vector3 relativeOffsetWorld, Quaternion relativeRotationWorld, Vector3 relativeScale, Vector3 gizmoWorldPosition, Quaternion gizmoWorldRotation)
        {
            if (activeSession == null) return;

            var obj = activeSession.RemapTarget;
            if (obj == null) return;

            if (obj.animator != null) affectedTransforms.RemoveAll(i => obj.animator.IsIKControlledBone(i) || obj.animator.IsFKControlledBone(i));

            if (activeSession.mirrorPoseEditing)
            {
                affectedTransforms = AnimationEditor.MirrorTransformManipulation(obj.animator == null ? (obj.instance == null ? null : obj.instance.transform) : obj.animator.RootBoneUnity, affectedTransforms, true, relativeOffsetWorld, relativeRotationWorld, relativeScale, gizmoWorldPosition, gizmoWorldRotation);
            }
        }

        protected virtual void ManipulationActionEnd(List<Transform> affectedTransforms, Vector3 relativeOffsetWorld, Quaternion relativeRotationWorld, Vector3 relativeScale, Vector3 gizmoWorldPosition, Quaternion gizmoWorldRotation)
        {
            if (activeSession == null) return;   
             
            IEnumerator DelayedCall()
            {

                yield return null;
                yield return null;

                var obj = activeSession.RemapTarget;
                if (obj == null) yield break;

                if (activeSession.editRemapTargetPose)
                {
                    if (obj.instance != null)
                    {
                        activeSession.InstantiateRemapPreset();

                        if (activeSession.mirrorPoseEditing)
                        {
                            _tempTransforms.Clear();
                            _tempTransforms.AddRange(affectedTransforms);
                            AnimationEditor.GetMirrorTransforms(obj.animator == null ? (obj.instance == null ? null : obj.instance.transform) : obj.animator.RootBoneUnity, affectedTransforms, _tempTransforms, true);
                        }

                        var proxyBones = obj.instance.GetComponentsInChildren<ProxyBone>(true);
                        var proxyTransforms = obj.instance.GetComponentsInChildren<ProxyTransform>(true);

                        foreach (var proxyBone in proxyBones)
                        {
                            bool contains = false;
                            if (_tempTransforms.Contains(proxyBone.transform))
                            {
                                contains = true;
                            }
                            else
                            {
                                if (proxyBone.bindings != null)
                                {
                                    foreach (var binding in proxyBone.bindings)
                                    {
                                        if (binding.bone != null && _tempTransforms.Contains(binding.bone))
                                        {
                                            contains = true;
                                            break;
                                        }
                                    }
                                }
                            }

                            if (contains)
                            {
                                if (!_tempTransforms.Contains(proxyBone.transform)) _tempTransforms.Add(proxyBone.transform);

                                if (proxyBone.bindings != null)
                                {
                                    foreach (var binding in proxyBone.bindings)
                                    {
                                        if (binding.bone != null && !_tempTransforms.Contains(binding.bone))
                                        {
                                            _tempTransforms.Add(binding.bone);
                                        }
                                    }
                                }
                            }
                        }
                         
                        foreach (var proxyTransform in proxyTransforms)
                        {
                            bool contains = false;
                            if (_tempTransforms.Contains(proxyTransform.transform))
                            {
                                contains = true;
                            }
                            else if (proxyTransform.transformToCopy != null && _tempTransforms.Contains(proxyTransform.transformToCopy))
                            {
                                contains = true;
                            }

                            if (contains)
                            {
                                if (!_tempTransforms.Contains(proxyTransform.transform)) _tempTransforms.Add(proxyTransform.transform);
                                if (proxyTransform.transformToCopy != null && !_tempTransforms.Contains(proxyTransform.transformToCopy)) _tempTransforms.Add(proxyTransform.transformToCopy);
                            }
                        }

                        _tempTransforms2.Clear();
                        void AddChildren(Transform parent)
                        {
                            for (int a = 0; a < parent.childCount; a++)
                            {
                                var child = parent.GetChild(a);
                                if (child != null) _tempTransforms2.Add(child); 

                                AddChildren(child);
                            }
                        }

                        foreach (var remapBone in _tempTransforms) AddChildren(remapBone); 
                        foreach (var childRemapBone in _tempTransforms2) if (!_tempTransforms.Contains(childRemapBone)) _tempTransforms.Add(childRemapBone); // child bone world space data must be recalculated as well

                        foreach (var remapBone in _tempTransforms)
                        {
                            var bindings = activeSession.GetRemapBindingsForBone(remapBone.name);
                            if (bindings != null)
                            {
                                activeSession.InstantiateRemapPreset();

                                Vector3 remapParentPos = Vector3.zero;
                                Quaternion remapParentRot = Quaternion.identity;
                                Vector3 remapParentPosLocal = Vector3.zero;
                                Quaternion remapParentRotLocal = Quaternion.identity;
                                if (remapBone.parent != null)
                                {
                                    remapBone.parent.GetPositionAndRotation(out remapParentPos, out remapParentRot);
                                    remapBone.parent.GetLocalPositionAndRotation(out remapParentPosLocal, out remapParentRotLocal);
                                }
                                remapBone.GetPositionAndRotation(out var remapPos, out var remapRot);
                                remapBone.GetLocalPositionAndRotation(out var remapPosLocal, out var remapRotLocal);

                                bindings.boundWorldPosition = remapPos;
                                bindings.boundWorldRotation = remapRot;

                                bindings.boundLocalPosition = remapPosLocal;
                                bindings.boundLocalRotation = remapRotLocal;

                                bindings.boundParentWorldPosition = remapParentPos;
                                bindings.boundParentWorldRotation = remapParentRot;

                                bindings.boundParentLocalPosition = remapParentPosLocal;
                                bindings.boundParentLocalRotation = remapParentRotLocal;

                                if (bindings.bindings != null)
                                {
                                    foreach (var binding in bindings.bindings)
                                    {
                                        var targetBone = activeSession.animator.GetUnityBone(binding.bone);
                                        if (targetBone == null) continue;

                                        Vector3 parentPos = Vector3.zero;
                                        Quaternion parentRot = Quaternion.identity;
                                        Vector3 parentPosLocal = Vector3.zero;
                                        Quaternion parentRotLocal = Quaternion.identity;
                                        if (targetBone.parent != null)
                                        {
                                            targetBone.parent.GetPositionAndRotation(out parentPos, out parentRot);
                                            targetBone.parent.GetLocalPositionAndRotation(out parentPosLocal, out parentRotLocal);
                                        }
                                        targetBone.GetPositionAndRotation(out var pos, out var rot);
                                        targetBone.GetLocalPositionAndRotation(out var lpos, out var lrot);

                                        binding.boundWorldPosition = pos;
                                        binding.boundWorldRotation = rot;
                                        binding.boundLocalPosition = lpos;
                                        binding.boundLocalRotation = lrot;
                                        binding.boundParentWorldPosition = parentPos;
                                        binding.boundParentWorldRotation = parentRot;
                                        binding.boundParentLocalPosition = parentPosLocal;
                                        binding.boundParentLocalRotation = parentRotLocal;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            StartCoroutine(DelayedCall());
        }

        private static readonly List<GameObject> tempGameObjects = new List<GameObject>();

        public void IgnorePreviewObjects()
        {
            if (activeSession == null) return;

            tempGameObjects.Clear();

            if (activeSession.importedObjects != null)
            {
                foreach (var obj in activeSession.importedObjects)
                {
                    if (obj.animator == null) continue;
                    tempGameObjects.Add(obj.animator.gameObject);
                }
            }

            if (activeSession.animator != null) tempGameObjects.Add(activeSession.animator.gameObject);

            AnimationEditor.IgnorePreviewObjects(runtimeEditor, tempGameObjects); 
        }

        public void StopIgnoringPreviewObjects()
        {
            if (activeSession == null) return;

            tempGameObjects.Clear();

            if (activeSession.importedObjects != null)
            {
                foreach (var obj in activeSession.importedObjects)
                {
                    if (obj.animator == null) continue;
                    tempGameObjects.Add(obj.animator.gameObject);
                }
            }

            if (activeSession.animator != null) tempGameObjects.Add(activeSession.animator.gameObject);

            AnimationEditor.StopIgnoringPreviewObjects(runtimeEditor, tempGameObjects);
        }

    }

    [Serializable]
    public class ModelImportSettings : ICloneable
    {
        public string name;

        public float scaleCompensation = 1.0f;

        public string avatar = string.Empty;
        public bool resetToBindpose = true;

        public string rigName = string.Empty;
        public string rootBoneName = string.Empty;
        public bool deleteDummyTransforms = true;
        public bool includeRigContainerAsBone = true;

        public bool importBlendShapes = true;
        public bool importMaterials = true;
        public bool importAnimations = true;

#if BULKOUT_ENV
        public AssetLoaderOptions cachedLoaderOptions;

        public AssetLoaderOptions GetLoaderOptions()
        {
            if (cachedLoaderOptions == null) cachedLoaderOptions = AssetLoader.CreateDefaultLoaderOptions(false, true);

            cachedLoaderOptions.ImportBlendShapes = importBlendShapes;
            cachedLoaderOptions.ImportMaterials = importMaterials;

            return cachedLoaderOptions;
        }
#endif

        public ModelImportSettings Duplicate()
        {
            var settings = new ModelImportSettings();

            settings.name = name;
            settings.importBlendShapes = importBlendShapes;
            settings.importMaterials = importMaterials;
            settings.importAnimations = importAnimations;

            return settings;
        }
        public object Clone() => Duplicate();

    }

    #region Animation Baking

    [Serializable]
    public enum AnimationBakeType
    {
        ResamplePerKey, ResampleInterval, IdenticalPerChannel
    }

    [Serializable]
    public enum TransformDataChannel
    {
        None, LocalPositionX, LocalPositionY, LocalPositionZ, LocalRotationX, LocalRotationY, LocalRotationZ, LocalRotationW, LocalScaleX, LocalScaleY, LocalScaleZ
    }
    public struct TransformDataInsertion
    {
        public Transform bone;

        public float time;

        public TransformDataChannel channel;

        public bool useExistingKeyframe;
        public AnimationCurveEditor.KeyframeStateRaw keyframe;

        public float referenceAmplitude;

        public bool hasPreceedingKeyframe;
        public AnimationCurveEditor.KeyframeStateRaw preceedingKeyframe;
        public bool hasProceedingKeyframe;
        public AnimationCurveEditor.KeyframeStateRaw proceedingKeyframe;
    }
    [Serializable]
    public struct TransformAnimationBakePair
    {
        public string referenceBone;
        public string targetBone;

        public TransformAnimationBakePair(string referenceBone, string targetBone)
        {
            this.referenceBone = referenceBone;
            this.targetBone = targetBone;
        }
    }

    public class AnimationBake
    {
        public struct BoneRelation
        {
            public string boneName;
            public bool applyOffsets;

            public bool forceInsertPosition;
            public bool forceInsertRotation;
            public bool forceInsertScale;

            public float3 offsetPosition;
            public quaternion offsetRotation;
        }

        private AnimationBakeType bakeType = AnimationBakeType.ResamplePerKey;

        private CustomAnimator animatorReference;
        private CustomAnimator animatorTarget;

        private CustomAnimation animationReference;
        private AnimationSource animationSource;
        public CustomAnimation OutputAnimation => animationSource == null ? null : animationSource.CompiledAnimation;

        private AnimationUtils.Pose restPoseTarget;

        private Action syncPose;

        private int lastFrame;

        private bool waiting = false;

        public bool bakeFkToIk = true;
        public bool forceBakeIkPosition = true;

        public bool bakeRootMotion = false;
        public float bakeRootMotionMaxTime = 0.998f;
        public bool bakeRootMotionAtSeparateInterval = false;
        public float rootMotionBakeIntervalPos = 0.1f;
        public float rootMotionBakeIntervalRot = 0.1f;
        public ModelEditor.RootMotionPositionMode rootMotionPositionMode;
        private Transform rootMotionBone;
        private struct WeightedTransform
        {
            public Transform transform;
            public float weight;
        }
        private List<WeightedTransform> rootMotionPositionReferenceBones = new List<WeightedTransform>();
        private List<WeightedTransform> rootMotionRotationReferenceBones = new List<WeightedTransform>();
        private Quaternion worldToRootRot;
        private Quaternion rootToWorldRot;
        private Matrix4x4 worldToRoot;
        private Matrix4x4 rootToWorld;
        private Vector3 baseRootLocalPosition;
        private Quaternion baseRootLocalRotation;
        private Vector3 currentRootLocalPosition;
        private Quaternion currentRootLocalRotation;
        private Vector3 startRootPosition;
        private Quaternion startRootRotation;
        private Vector3 previousRootPosition;
        private Quaternion previousRootRotation;
        private bool rootMotionInitialized;

        private void CalculateRootMotionPositionAndRotation(out Vector3 rootPosition, out Quaternion rootRotation)
        {
            rootPosition = Vector3.zero;
            rootRotation = Quaternion.identity;

            if (rootMotionPositionReferenceBones != null && rootMotionPositionReferenceBones.Count > 0) 
            {
                foreach(var wt in rootMotionPositionReferenceBones)
                {
                    rootPosition += wt.transform.position * wt.weight;
                }
            }

            if (rootMotionRotationReferenceBones != null && rootMotionRotationReferenceBones.Count > 0)
            {
                foreach (var wt in rootMotionRotationReferenceBones)
                {
                    rootRotation = Quaternion.Slerp(Quaternion.identity, wt.transform.rotation, wt.weight) * rootRotation; 
                }
            }
        }

        public int insertionFrameDelay = 3;

        public int samplesPerKey = 1;
        public float resampleIntervalPos = 0.1f;
        public float resampleIntervalRot = 0.1f;
        public float resampleIntervalScale = 0.5f;

        private List<IAnimationLayer> originalLayers = new List<IAnimationLayer>();
        private CustomAnimationLayer controlLayer;

        private Dictionary<string, List<BoneRelation>> boneRelations = new Dictionary<string, List<BoneRelation>>();
        private Dictionary<string, Dictionary<float, Quaternion>> boneRotations = new Dictionary<string, Dictionary<float, Quaternion>>();
        private Quaternion GetPreviousBoneRotation(string bone, float time)
        {
            if (boneRotations.TryGetValue(bone, out var timeline))
            {
                if (timeline != null)
                {
                    float closestTime = -1;
                    Quaternion closestRot = Quaternion.identity;

                    foreach (var entry in timeline)
                    {
                        if (entry.Key < time && entry.Key > closestTime)
                        {
                            closestTime = entry.Key;
                            closestRot = entry.Value;
                        }
                    }

                    return closestRot;
                }
            }

            return Quaternion.identity;
        }
        private void StoreBoneRotation(string bone, float time, Quaternion rotation)
        {
            if (!boneRotations.TryGetValue(bone, out var timeline))
            {
                timeline = new Dictionary<float, Quaternion>();
                boneRotations[bone] = timeline;
            }

            timeline[time] = rotation;
        }
        private Quaternion CalculateAndStoreBoneRotation(Transform bone, float time)
        {
            var localRot = bone.localRotation;
            var prevLocalRot = GetPreviousBoneRotation(bone.name, time);

            localRot = Maths.EnsureQuaternionContinuity(prevLocalRot, localRot);
            StoreBoneRotation(bone.name, time, localRot);

            return localRot;
        }

        private int insertionCount;
        private List<TransformDataInsertion> insertionLine = new List<TransformDataInsertion>();
        private List<TransformDataInsertion> toInsert = new List<TransformDataInsertion>();
        private List<TransformDataInsertion> completionLine = new List<TransformDataInsertion>();

        private bool isComplete;
        public bool IsComplete => isComplete; 

        public AnimationBake(CustomAnimator animatorReference, CustomAnimator animatorTarget, CustomAnimation animationReference, AnimationUtils.Pose restPoseTarget, Action syncPose)
        {
            this.animatorReference = animatorReference;
            this.animatorTarget = animatorTarget;
            this.animationReference = animationReference;
            this.restPoseTarget = restPoseTarget;
            this.syncPose = syncPose;
        }

        public void Initialize(string animationName, AnimationBakeType bakeType, List<TransformAnimationBakePair> boneBindings, CustomAnimation animationTarget = null, string rootMotionBoneName = null, IEnumerable<string> rootMotionPositionReferenceBones = null, IEnumerable<string> rootMotionRotationReferenceBones = null)
        {
            /*string GetReferenceBoneName(string targetBoneName)
            {
                foreach (var pair in boneBindings) if (pair.targetBone == targetBoneName) return pair.referenceBone;
                return string.Empty;
            }*/
            string GetTargetBoneName(string referenceBoneName)
            {
                foreach (var pair in boneBindings) if (pair.referenceBone == referenceBoneName) return pair.targetBone;
                return string.Empty;
            }

            if (!isComplete) Complete();

            this.isComplete = false;

            this.bakeType = bakeType;

            for (int a = 0; a < animatorReference.LayerCount; a++) originalLayers.Add(animatorReference.GetLayer(a));
            animatorReference.ClearLayers(false);

            this.lastFrame = Time.frameCount;

            var controller = CustomAnimationController.BuildDefaultController("bake", new CustomAnimation[] { animationReference }, 1);
            animatorReference.ApplyController(controller);
            controlLayer = animatorReference.FindTypedLayer($"bake/{animationReference.Name}");
            controlLayer.mix = 1;
            if (controlLayer.HasActiveState && controlLayer.ActiveState is CustomAnimationLayerState state && state.MotionControllerIndex >= 0)
            {
                var mc = controlLayer.GetMotionControllerUnsafe(state.MotionControllerIndex);
                if (mc is AnimationReference ar)
                {
                    ar.BaseSpeed = 0;
                    ar.SetNormalizedTime(controlLayer, 0);
                }
            }

            GameObject.Destroy(controller);

            float length = animationReference.LengthInSeconds; 
            if (animationReference.timeCurve != null && animationReference.timeCurve.length > 1) length = Mathf.Abs((length * animationReference.timeCurve[animationReference.timeCurve.length - 1].time) - (length * animationReference.timeCurve[0].time));

            var contentInfo = animationReference.ContentInfo;
            contentInfo.name = animationName;
            contentInfo.lastEditDate = contentInfo.creationDate = DateTime.Now.ToString();
            animationSource = new AnimationSource() { DisplayName = animationName, timelineLength = length, rawAnimation = animationTarget == null ? new CustomAnimation(contentInfo, animationReference.framesPerSecond, animationReference.jobCurveSampleRate, null, null, null, null, null, null, null, null) : animationTarget };
            animationSource.MarkAsDirty();

            insertionLine.Clear();
            completionLine.Clear();

            switch (bakeType)
            {
                case AnimationBakeType.ResamplePerKey:
                    if (animationReference.transformAnimationCurves != null)
                    {
                        HashSet<float> insertionTimesPos = new HashSet<float>();
                        HashSet<float> insertionTimesRot = new HashSet<float>();
                        HashSet<float> insertionTimesScale = new HashSet<float>();

                        foreach (var curveInfo in animationReference.transformAnimationCurves)
                        {
                            if (curveInfo.infoMain.curveIndex >= 0)
                            {
                                Transform targetBone = null;

                                insertionTimesPos.Clear();
                                insertionTimesRot.Clear();
                                insertionTimesScale.Clear();

                                if (curveInfo.infoMain.isLinear)
                                {
                                    var curve = animationReference.transformLinearCurves[curveInfo.infoMain.curveIndex];
                                    if (curve != null && curve.frames != null)
                                    {
                                        var targetBoneName = GetTargetBoneName(curve.TransformName);
                                        if (string.IsNullOrWhiteSpace(targetBoneName)) continue;

                                        targetBone = animatorTarget.GetUnityBone(targetBoneName);
                                        if (targetBone == null) continue;

                                        for (int f = 0; f < curve.frames.Length; f++)
                                        {
                                            var frame = curve.frames[f];
                                            float time = frame.GetTime(animationReference.framesPerSecond);

                                            insertionTimesPos.Add(time);
                                            insertionTimesRot.Add(time);
                                            insertionTimesScale.Add(time);

                                            if (f < curve.frames.Length - 1)
                                            {
                                                var nextFrame = curve.frames[f + 1];
                                                float nextTime = nextFrame.GetTime(animationReference.framesPerSecond);

                                                for (int g = 1; g < samplesPerKey; g++)
                                                {
                                                    float t = Mathf.Lerp(time, nextTime, g / (float)samplesPerKey);

                                                    insertionTimesPos.Add(t);
                                                    insertionTimesRot.Add(t);
                                                    insertionTimesScale.Add(t);
                                                }
                                            } 
                                        }
                                    }
                                }
                                else
                                {
                                    var curve = animationReference.transformCurves[curveInfo.infoMain.curveIndex];
                                    if (curve != null)
                                    {
                                        var targetBoneName = GetTargetBoneName(curve.TransformName);
                                        if (string.IsNullOrWhiteSpace(targetBoneName))
                                        {
#if UNITY_EDITOR
                                            Debug.Log($"Skipping curves for bone {curve.TransformName}");   
#endif
                                            continue;
                                        }

                                        targetBone = animatorTarget.GetUnityBone(targetBoneName);
                                        if (targetBone == null)
                                        {
#if UNITY_EDITOR
                                            Debug.Log($"{targetBoneName} not found");
#endif
                                            continue;
                                        }

                                        void AddPositionInsertions(EditableAnimationCurve curve)
                                        {
                                            for (int f = 0; f < curve.length; f++)
                                            {
                                                var kf = curve[f];
                                                insertionTimesPos.Add(kf.time);

                                                if (f < curve.length - 1)
                                                {
                                                    var nextKf = curve[f + 1];

                                                    for (int g = 1; g < samplesPerKey; g++)
                                                    {
                                                        float t = Mathf.Lerp(kf.time, nextKf.time, g / (float)samplesPerKey);
                                                        insertionTimesPos.Add(t);
                                                    }
                                                }
                                            }
                                        }
                                        void AddRotationInsertions(EditableAnimationCurve curve)
                                        {
                                            for (int f = 0; f < curve.length; f++)
                                            {
                                                var kf = curve[f];
                                                insertionTimesRot.Add(kf.time);

                                                if (f < curve.length - 1)
                                                {
                                                    var nextKf = curve[f + 1];

                                                    for (int g = 1; g < samplesPerKey; g++)
                                                    {
                                                        float t = Mathf.Lerp(kf.time, nextKf.time, g / (float)samplesPerKey);
                                                        insertionTimesRot.Add(t);
                                                    }
                                                }
                                            }
                                        }
                                        void AddScaleInsertions(EditableAnimationCurve curve)
                                        {
                                            for (int f = 0; f < curve.length; f++)
                                            {
                                                var kf = curve[f];
                                                insertionTimesScale.Add(kf.time);

                                                if (f < curve.length - 1)
                                                {
                                                    var nextKf = curve[f + 1];

                                                    for (int g = 1; g < samplesPerKey; g++)
                                                    {
                                                        float t = Mathf.Lerp(kf.time, nextKf.time, g / (float)samplesPerKey);
                                                        insertionTimesScale.Add(t);
                                                    }
                                                }
                                            }
                                        }

                                        if (curve.localPositionCurveX != null) AddPositionInsertions(curve.localPositionCurveX);
                                        if (curve.localPositionCurveY != null) AddPositionInsertions(curve.localPositionCurveY);
                                        if (curve.localPositionCurveZ != null) AddPositionInsertions(curve.localPositionCurveZ);

                                        if (curve.localRotationCurveX != null) AddRotationInsertions(curve.localRotationCurveX);
                                        if (curve.localRotationCurveY != null) AddRotationInsertions(curve.localRotationCurveY);
                                        if (curve.localRotationCurveZ != null) AddRotationInsertions(curve.localRotationCurveZ);
                                        if (curve.localRotationCurveW != null) AddRotationInsertions(curve.localRotationCurveW);

                                        if (curve.localScaleCurveX != null) AddScaleInsertions(curve.localScaleCurveX);
                                        if (curve.localScaleCurveY != null) AddScaleInsertions(curve.localScaleCurveY);
                                        if (curve.localScaleCurveZ != null) AddScaleInsertions(curve.localScaleCurveZ);
                                    }
                                }

                                foreach (var time in insertionTimesPos)
                                {
                                    insertionLine.Add(new TransformDataInsertion()
                                    {
                                        bone = targetBone,
                                        time = time,
                                        channel = TransformDataChannel.LocalPositionX
                                    });
                                    insertionLine.Add(new TransformDataInsertion()
                                    {
                                        bone = targetBone,
                                        time = time,
                                        channel = TransformDataChannel.LocalPositionY
                                    });
                                    insertionLine.Add(new TransformDataInsertion()
                                    {
                                        bone = targetBone,
                                        time = time,
                                        channel = TransformDataChannel.LocalPositionZ
                                    });
                                }
                                foreach (var time in insertionTimesRot)
                                {
                                    insertionLine.Add(new TransformDataInsertion()
                                    {
                                        bone = targetBone,
                                        time = time,
                                        channel = TransformDataChannel.LocalRotationX
                                    });
                                    insertionLine.Add(new TransformDataInsertion()
                                    {
                                        bone = targetBone,
                                        time = time,
                                        channel = TransformDataChannel.LocalRotationY
                                    });
                                    insertionLine.Add(new TransformDataInsertion()
                                    {
                                        bone = targetBone,
                                        time = time,
                                        channel = TransformDataChannel.LocalRotationZ
                                    });
                                    insertionLine.Add(new TransformDataInsertion()
                                    {
                                        bone = targetBone,
                                        time = time,
                                        channel = TransformDataChannel.LocalRotationW
                                    });
                                }
                                foreach (var time in insertionTimesScale)
                                {
                                    insertionLine.Add(new TransformDataInsertion()
                                    {
                                        bone = targetBone,
                                        time = time,
                                        channel = TransformDataChannel.LocalScaleX
                                    });
                                    insertionLine.Add(new TransformDataInsertion()
                                    {
                                        bone = targetBone,
                                        time = time,
                                        channel = TransformDataChannel.LocalScaleY
                                    });
                                    insertionLine.Add(new TransformDataInsertion()
                                    {
                                        bone = targetBone,
                                        time = time,
                                        channel = TransformDataChannel.LocalScaleZ
                                    });
                                }
                            }
                        }
                    }
                    break;

                case AnimationBakeType.ResampleInterval:

                    var length_ = length * 0.998f;

                    int insertionsPos = resampleIntervalPos > 0 ? Mathf.FloorToInt(length_ / resampleIntervalPos) : 0;
                    int insertionsRot = resampleIntervalRot > 0 ? Mathf.FloorToInt(length_ / resampleIntervalRot) : 0;
                    int insertionsScale = resampleIntervalScale > 0 ? Mathf.FloorToInt(length_ / resampleIntervalScale) : 0;

                    foreach (var curveInfo in animationReference.transformAnimationCurves)
                    {
                        if (curveInfo.infoMain.curveIndex >= 0)
                        {
                            Transform targetBone = null;

                            bool insertPos = false;
                            bool insertRot = false;
                            bool insertScale = false;

                            if (curveInfo.infoMain.isLinear)
                            {
                                var curve = animationReference.transformLinearCurves[curveInfo.infoMain.curveIndex];
                                if (curve != null && curve.frames != null)
                                {
                                    var targetBoneName = GetTargetBoneName(curve.TransformName);
                                    if (string.IsNullOrWhiteSpace(targetBoneName)) continue;

                                    targetBone = animatorTarget.GetUnityBone(targetBoneName);
                                    if (targetBone == null) continue;

                                    insertPos = true;
                                    insertRot = true;
                                    insertScale = true;
                                }
                            }
                            else
                            {
                                var curve = animationReference.transformCurves[curveInfo.infoMain.curveIndex];
                                if (curve != null)
                                {
                                    var targetBoneName = GetTargetBoneName(curve.TransformName);
                                    if (string.IsNullOrWhiteSpace(targetBoneName)) continue;

                                    targetBone = animatorTarget.GetUnityBone(targetBoneName);
                                    if (targetBone == null) continue;

                                    insertPos = (curve.localPositionCurveX != null && curve.localPositionCurveX.length > 0) || (curve.localPositionCurveY != null && curve.localPositionCurveY.length > 0) || (curve.localPositionCurveZ != null && curve.localPositionCurveZ.length > 0);
                                    insertRot = (curve.localRotationCurveX != null && curve.localRotationCurveX.length > 0) || (curve.localRotationCurveY != null && curve.localRotationCurveY.length > 0) || (curve.localRotationCurveZ != null && curve.localRotationCurveZ.length > 0) || (curve.localRotationCurveW != null && curve.localRotationCurveW.length > 0);
                                    insertScale = (curve.localScaleCurveX != null && curve.localScaleCurveX.length > 0) || (curve.localScaleCurveY != null && curve.localScaleCurveY.length > 0) || (curve.localScaleCurveZ != null && curve.localScaleCurveZ.length > 0);
                                }
                            }

                            if (targetBone == null) continue;

                            if (insertPos)
                            {
                                for (int i = 0; i < insertionsPos; i++)
                                {
                                    float time = i == insertionsPos - 1 ? length_ : (i * resampleIntervalPos);

                                    insertionLine.Add(new TransformDataInsertion()
                                    {
                                        bone = targetBone,
                                        time = time,
                                        channel = TransformDataChannel.LocalPositionX
                                    });
                                    insertionLine.Add(new TransformDataInsertion()
                                    {
                                        bone = targetBone,
                                        time = time,
                                        channel = TransformDataChannel.LocalPositionY
                                    });
                                    insertionLine.Add(new TransformDataInsertion()
                                    {
                                        bone = targetBone,
                                        time = time,
                                        channel = TransformDataChannel.LocalPositionZ
                                    });
                                }
                            }
                            if (insertRot)
                            {
                                for (int i = 0; i < insertionsRot; i++)
                                {
                                    float time = i == insertionsRot - 1 ? length_ : (i * resampleIntervalPos);

                                    insertionLine.Add(new TransformDataInsertion()
                                    {
                                        bone = targetBone,
                                        time = time,
                                        channel = TransformDataChannel.LocalRotationX
                                    });
                                    insertionLine.Add(new TransformDataInsertion()
                                    {
                                        bone = targetBone,
                                        time = time,
                                        channel = TransformDataChannel.LocalRotationY
                                    });
                                    insertionLine.Add(new TransformDataInsertion()
                                    {
                                        bone = targetBone,
                                        time = time,
                                        channel = TransformDataChannel.LocalRotationZ
                                    });
                                    insertionLine.Add(new TransformDataInsertion()
                                    {
                                        bone = targetBone,
                                        time = time,
                                        channel = TransformDataChannel.LocalRotationW
                                    });
                                }
                            }
                            if (insertScale)
                            {
                                for (int i = 0; i < insertionsScale; i++)
                                {
                                    float time = i == insertionsScale - 1 ? length_ : (i * resampleIntervalPos);

                                    insertionLine.Add(new TransformDataInsertion()
                                    {
                                        bone = targetBone,
                                        time = time,
                                        channel = TransformDataChannel.LocalScaleX
                                    });
                                    insertionLine.Add(new TransformDataInsertion()
                                    {
                                        bone = targetBone,
                                        time = time,
                                        channel = TransformDataChannel.LocalScaleY
                                    });
                                    insertionLine.Add(new TransformDataInsertion()
                                    {
                                        bone = targetBone,
                                        time = time,
                                        channel = TransformDataChannel.LocalScaleZ
                                    });
                                }
                            }
                        }
                    }

                    break;

                case AnimationBakeType.IdenticalPerChannel:
                    if (animationReference.transformAnimationCurves != null)
                    {
                        foreach (var curveInfo in animationReference.transformAnimationCurves)
                        {
                            if (curveInfo.infoMain.curveIndex >= 0)
                            {
                                if (curveInfo.infoMain.isLinear)
                                {
                                    var curve = animationReference.transformLinearCurves[curveInfo.infoMain.curveIndex];
                                    if (curve != null && curve.frames != null)
                                    {
                                        var targetBoneName = GetTargetBoneName(curve.TransformName);
                                        if (string.IsNullOrWhiteSpace(targetBoneName)) continue;

                                        var targetBone = animatorTarget.GetUnityBone(targetBoneName);
                                        if (targetBone == null) continue;

                                        foreach (var frame in curve.frames)
                                        {
                                            float time = frame.GetTime(animationReference.framesPerSecond);

                                            insertionLine.Add(new TransformDataInsertion()
                                            {
                                                bone = targetBone,
                                                time = time,
                                                channel = TransformDataChannel.LocalPositionX
                                            });
                                            insertionLine.Add(new TransformDataInsertion()
                                            {
                                                bone = targetBone,
                                                time = time,
                                                channel = TransformDataChannel.LocalPositionY
                                            });
                                            insertionLine.Add(new TransformDataInsertion()
                                            {
                                                bone = targetBone,
                                                time = time,
                                                channel = TransformDataChannel.LocalPositionZ
                                            });

                                            insertionLine.Add(new TransformDataInsertion()
                                            {
                                                bone = targetBone,
                                                time = time,
                                                channel = TransformDataChannel.LocalRotationX
                                            });
                                            insertionLine.Add(new TransformDataInsertion()
                                            {
                                                bone = targetBone,
                                                time = time,
                                                channel = TransformDataChannel.LocalRotationY
                                            });
                                            insertionLine.Add(new TransformDataInsertion()
                                            {
                                                bone = targetBone,
                                                time = time,
                                                channel = TransformDataChannel.LocalRotationZ
                                            });
                                            insertionLine.Add(new TransformDataInsertion()
                                            {
                                                bone = targetBone,
                                                time = time,
                                                channel = TransformDataChannel.LocalRotationW
                                            });

                                            insertionLine.Add(new TransformDataInsertion()
                                            {
                                                bone = targetBone,
                                                time = time,
                                                channel = TransformDataChannel.LocalScaleX
                                            });
                                            insertionLine.Add(new TransformDataInsertion()
                                            {
                                                bone = targetBone,
                                                time = time,
                                                channel = TransformDataChannel.LocalScaleY
                                            });
                                            insertionLine.Add(new TransformDataInsertion()
                                            {
                                                bone = targetBone,
                                                time = time,
                                                channel = TransformDataChannel.LocalScaleZ
                                            });
                                        }
                                    }
                                }
                                else
                                {
                                    var curve = animationReference.transformCurves[curveInfo.infoMain.curveIndex];
                                    if (curve != null)
                                    {
                                        var targetBoneName = GetTargetBoneName(curve.TransformName);
                                        if (string.IsNullOrWhiteSpace(targetBoneName)) continue;

                                        var targetBone = animatorTarget.GetUnityBone(targetBoneName);
                                        if (targetBone == null) continue;

                                        if (curve.localPositionCurveX != null)
                                        {
                                            float amp = AnimationUtils.CalculateAmplitude(curve.localPositionCurveX);

                                            for (int f = 0; f < curve.localPositionCurveX.length; f++)
                                            {
                                                var key = curve.localPositionCurveX[f];

                                                insertionLine.Add(new TransformDataInsertion()
                                                {
                                                    bone = targetBone,
                                                    time = key.time,
                                                    channel = TransformDataChannel.LocalPositionX,

                                                    useExistingKeyframe = true,
                                                    keyframe = key,

                                                    referenceAmplitude = amp,

                                                    hasPreceedingKeyframe = f > 0,
                                                    preceedingKeyframe = curve.localPositionCurveX[Mathf.Max(0, f - 1)],

                                                    hasProceedingKeyframe = f < curve.localPositionCurveX.length - 1,
                                                    proceedingKeyframe = curve.localPositionCurveX[Mathf.Min(f + 1, curve.localPositionCurveX.length - 1)]
                                                });
                                            }
                                        }
                                        if (curve.localPositionCurveY != null)
                                        {
                                            float amp = AnimationUtils.CalculateAmplitude(curve.localPositionCurveY);

                                            for (int f = 0; f < curve.localPositionCurveY.length; f++)
                                            {
                                                var key = curve.localPositionCurveY[f];

                                                insertionLine.Add(new TransformDataInsertion()
                                                {
                                                    bone = targetBone,
                                                    time = key.time,
                                                    channel = TransformDataChannel.LocalPositionY,

                                                    useExistingKeyframe = true,
                                                    keyframe = key,
                                                    
                                                    referenceAmplitude = amp,

                                                    hasPreceedingKeyframe = f > 0,
                                                    preceedingKeyframe = curve.localPositionCurveY[Mathf.Max(0, f - 1)],

                                                    hasProceedingKeyframe = f < curve.localPositionCurveY.length - 1,
                                                    proceedingKeyframe = curve.localPositionCurveY[Mathf.Min(f + 1, curve.localPositionCurveY.length - 1)]
                                                });
                                            }
                                        }
                                        if (curve.localPositionCurveZ != null)
                                        {
                                            float amp = AnimationUtils.CalculateAmplitude(curve.localPositionCurveZ);

                                            for (int f = 0; f < curve.localPositionCurveZ.length; f++)
                                            {
                                                var key = curve.localPositionCurveZ[f];

                                                insertionLine.Add(new TransformDataInsertion()
                                                {
                                                    bone = targetBone,
                                                    time = key.time,
                                                    channel = TransformDataChannel.LocalPositionZ,

                                                    useExistingKeyframe = true,
                                                    keyframe = key,

                                                    referenceAmplitude = amp,

                                                    hasPreceedingKeyframe = f > 0,
                                                    preceedingKeyframe = curve.localPositionCurveZ[Mathf.Max(0, f - 1)],

                                                    hasProceedingKeyframe = f < curve.localPositionCurveZ.length - 1,
                                                    proceedingKeyframe = curve.localPositionCurveZ[Mathf.Min(f + 1, curve.localPositionCurveZ.length - 1)]
                                                });
                                            }
                                        }

                                        if (curve.localRotationCurveX != null)
                                        {
                                            float amp = AnimationUtils.CalculateAmplitude(curve.localRotationCurveX);

                                            for (int f = 0; f < curve.localRotationCurveX.length; f++)
                                            {
                                                var key = curve.localRotationCurveX[f];

                                                insertionLine.Add(new TransformDataInsertion()
                                                {
                                                    bone = targetBone,
                                                    time = key.time,
                                                    channel = TransformDataChannel.LocalRotationX,

                                                    useExistingKeyframe = true,
                                                    keyframe = key,

                                                    referenceAmplitude = amp,

                                                    hasPreceedingKeyframe = f > 0,
                                                    preceedingKeyframe = curve.localRotationCurveX[Mathf.Max(0, f - 1)],

                                                    hasProceedingKeyframe = f < curve.localRotationCurveX.length - 1,
                                                    proceedingKeyframe = curve.localRotationCurveX[Mathf.Min(f + 1, curve.localRotationCurveX.length - 1)]
                                                });
                                            }
                                        }
                                        if (curve.localRotationCurveY != null)
                                        {
                                            float amp = AnimationUtils.CalculateAmplitude(curve.localRotationCurveY);

                                            for (int f = 0; f < curve.localRotationCurveY.length; f++)
                                            {
                                                var key = curve.localRotationCurveY[f];

                                                insertionLine.Add(new TransformDataInsertion()
                                                {
                                                    bone = targetBone,
                                                    time = key.time,
                                                    channel = TransformDataChannel.LocalRotationY,
                                                    useExistingKeyframe = true,
                                                    keyframe = key,

                                                    referenceAmplitude = amp,

                                                    hasPreceedingKeyframe = f > 0,
                                                    preceedingKeyframe = curve.localRotationCurveY[Mathf.Max(0, f - 1)],

                                                    hasProceedingKeyframe = f < curve.localRotationCurveY.length - 1,
                                                    proceedingKeyframe = curve.localRotationCurveY[Mathf.Min(f + 1, curve.localRotationCurveY.length - 1)]
                                                });
                                            }
                                        }
                                        if (curve.localRotationCurveZ != null)
                                        {
                                            float amp = AnimationUtils.CalculateAmplitude(curve.localRotationCurveZ);

                                            for (int f = 0; f < curve.localRotationCurveZ.length; f++)
                                            {
                                                var key = curve.localRotationCurveZ[f];

                                                insertionLine.Add(new TransformDataInsertion()
                                                {
                                                    bone = targetBone,
                                                    time = key.time,
                                                    channel = TransformDataChannel.LocalRotationZ,

                                                    useExistingKeyframe = true,
                                                    keyframe = key,

                                                    referenceAmplitude = amp,

                                                    hasPreceedingKeyframe = f > 0,
                                                    preceedingKeyframe = curve.localRotationCurveZ[Mathf.Max(0, f - 1)],

                                                    hasProceedingKeyframe = f < curve.localRotationCurveZ.length - 1,
                                                    proceedingKeyframe = curve.localRotationCurveZ[Mathf.Min(f + 1, curve.localRotationCurveZ.length - 1)]
                                                });
                                            }
                                        }
                                        if (curve.localRotationCurveW != null)
                                        {
                                            float amp = AnimationUtils.CalculateAmplitude(curve.localRotationCurveW);

                                            for (int f = 0; f < curve.localRotationCurveW.length; f++)
                                            {
                                                var key = curve.localRotationCurveW[f];

                                                insertionLine.Add(new TransformDataInsertion()
                                                {
                                                    bone = targetBone,
                                                    time = key.time,
                                                    channel = TransformDataChannel.LocalRotationW,

                                                    useExistingKeyframe = true,
                                                    keyframe = key,

                                                    referenceAmplitude = amp,

                                                    hasPreceedingKeyframe = f > 0,
                                                    preceedingKeyframe = curve.localRotationCurveW[Mathf.Max(0, f - 1)],

                                                    hasProceedingKeyframe = f < curve.localRotationCurveW.length - 1,
                                                    proceedingKeyframe = curve.localRotationCurveW[Mathf.Min(f + 1, curve.localRotationCurveW.length - 1)]
                                                });
                                            }
                                        }

                                        if (curve.localScaleCurveX != null)
                                        {
                                            float amp = AnimationUtils.CalculateAmplitude(curve.localScaleCurveX);

                                            for (int f = 0; f < curve.localScaleCurveX.length; f++)
                                            {
                                                var key = curve.localScaleCurveX[f];

                                                insertionLine.Add(new TransformDataInsertion()
                                                {
                                                    bone = targetBone,
                                                    time = key.time,
                                                    channel = TransformDataChannel.LocalScaleX,

                                                    useExistingKeyframe = true,
                                                    keyframe = key,

                                                    referenceAmplitude = amp,

                                                    hasPreceedingKeyframe = f > 0,
                                                    preceedingKeyframe = curve.localScaleCurveX[Mathf.Max(0, f - 1)],

                                                    hasProceedingKeyframe = f < curve.localScaleCurveX.length - 1,
                                                    proceedingKeyframe = curve.localScaleCurveX[Mathf.Min(f + 1, curve.localScaleCurveX.length - 1)]
                                                });
                                            }
                                        }
                                        if (curve.localScaleCurveY != null)
                                        {
                                            float amp = AnimationUtils.CalculateAmplitude(curve.localScaleCurveY);

                                            for (int f = 0; f < curve.localScaleCurveY.length; f++)
                                            {
                                                var key = curve.localScaleCurveY[f];

                                                insertionLine.Add(new TransformDataInsertion()
                                                {
                                                    bone = targetBone,
                                                    time = key.time,
                                                    channel = TransformDataChannel.LocalScaleY,

                                                    useExistingKeyframe = true,
                                                    keyframe = key,

                                                    referenceAmplitude = amp,

                                                    hasPreceedingKeyframe = f > 0,
                                                    preceedingKeyframe = curve.localScaleCurveY[Mathf.Max(0, f - 1)],

                                                    hasProceedingKeyframe = f < curve.localScaleCurveY.length - 1,
                                                    proceedingKeyframe = curve.localScaleCurveY[Mathf.Min(f + 1, curve.localScaleCurveY.length - 1)]
                                                });
                                            }
                                        }
                                        if (curve.localScaleCurveZ != null)
                                        {
                                            float amp = AnimationUtils.CalculateAmplitude(curve.localScaleCurveZ);

                                            for (int f = 0; f < curve.localScaleCurveZ.length; f++)
                                            {
                                                var key = curve.localScaleCurveZ[f];

                                                insertionLine.Add(new TransformDataInsertion()
                                                {
                                                    bone = targetBone,
                                                    time = key.time,
                                                    channel = TransformDataChannel.LocalScaleZ,

                                                    useExistingKeyframe = true,
                                                    keyframe = key,

                                                    referenceAmplitude = amp,

                                                    hasPreceedingKeyframe = f > 0,
                                                    preceedingKeyframe = curve.localScaleCurveZ[Mathf.Max(0, f - 1)],

                                                    hasProceedingKeyframe = f < curve.localScaleCurveZ.length - 1,
                                                    proceedingKeyframe = curve.localScaleCurveZ[Mathf.Min(f + 1, curve.localScaleCurveZ.length - 1)]
                                                });
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    break;
            }

            if (bakeRootMotion)
            {
                if (this.rootMotionPositionReferenceBones == null) this.rootMotionPositionReferenceBones = new List<WeightedTransform>();
                this.rootMotionPositionReferenceBones.Clear();

                if (rootMotionPositionReferenceBones != null)
                {
                    foreach (var boneName in rootMotionPositionReferenceBones)
                    {
                        if (string.IsNullOrWhiteSpace(boneName)) continue;

                        var targetBone = animatorTarget.GetUnityBone(boneName);
                        if (targetBone == null) continue;

                        this.rootMotionPositionReferenceBones.Add(new WeightedTransform() { transform = targetBone });
                    }

                    float weight = 1f / this.rootMotionPositionReferenceBones.Count();
                    for (int a = 0; a < this.rootMotionPositionReferenceBones.Count(); a++)
                    {
                        var wt = this.rootMotionPositionReferenceBones[a];
                        wt.weight = weight;
                        this.rootMotionPositionReferenceBones[a] = wt;
                    }
                }

                if (this.rootMotionRotationReferenceBones == null) this.rootMotionRotationReferenceBones = new List<WeightedTransform>();
                this.rootMotionRotationReferenceBones.Clear();

                if (rootMotionRotationReferenceBones != null)
                {
                    foreach (var boneName in rootMotionRotationReferenceBones)
                    {
                        if (string.IsNullOrWhiteSpace(boneName)) continue;

                        var targetBone = animatorTarget.GetUnityBone(boneName);
                        if (targetBone == null) continue;

                        this.rootMotionRotationReferenceBones.Add(new WeightedTransform() { transform = targetBone });
                    }

                    float weight = 1f / this.rootMotionRotationReferenceBones.Count();
                    for (int a = 0; a < this.rootMotionRotationReferenceBones.Count(); a++)
                    {
                        var wt = this.rootMotionRotationReferenceBones[a];
                        wt.weight = weight;
                        this.rootMotionRotationReferenceBones[a] = wt;
                    }
                }

                worldToRootRot = Quaternion.identity;
                rootToWorldRot = Quaternion.identity;
                worldToRoot = Matrix4x4.identity;
                rootToWorld = Matrix4x4.identity;

                baseRootLocalPosition = currentRootLocalPosition = Vector3.zero;
                baseRootLocalRotation = currentRootLocalRotation = Quaternion.identity;

                rootMotionBone = string.IsNullOrWhiteSpace(rootMotionBoneName) ? animatorTarget.RootMotionBone : animatorTarget.GetUnityBone(rootMotionBoneName);
                if (rootMotionBone != null)
                {
                    baseRootLocalPosition = currentRootLocalPosition = rootMotionBone.localPosition;
                    baseRootLocalRotation = baseRootLocalRotation = rootMotionBone.localRotation;

                    if (rootMotionBone.parent != null)
                    {
                        rootToWorldRot = rootMotionBone.parent.rotation;
                        worldToRootRot = Quaternion.Inverse(rootToWorldRot);

                        rootToWorld = rootMotionBone.parent.localToWorldMatrix;
                        worldToRoot = rootMotionBone.parent.worldToLocalMatrix;
                    }
                }

                rootMotionInitialized = false;

                bool IsRootMotionBoneChild(Transform bone)
                {
                    if (bone == null) return false;
                    if (bone == rootMotionBone) return true;

                    return bone.parent == rootMotionBone;
                }
                HashSet<float> posTimes = new HashSet<float>();
                HashSet<float> rotTimes = new HashSet<float>();
                foreach (var wt in this.rootMotionPositionReferenceBones)
                {
                    if (wt.transform == null) continue;

                    foreach (var insertion in insertionLine)
                    {
                        if (insertion.bone != wt.transform) continue;

                        if (insertion.channel == TransformDataChannel.LocalPositionX
                            || insertion.channel == TransformDataChannel.LocalPositionY
                            || insertion.channel == TransformDataChannel.LocalPositionZ) posTimes.Add(insertion.time);
                    }
                }
                foreach (var wt in this.rootMotionRotationReferenceBones)
                {
                    if (wt.transform == null) continue;

                    foreach (var insertion in insertionLine)
                    {
                        if (insertion.bone != wt.transform) continue;

                        if (insertion.channel == TransformDataChannel.LocalRotationX
                            || insertion.channel == TransformDataChannel.LocalRotationY
                            || insertion.channel == TransformDataChannel.LocalRotationZ
                            || insertion.channel == TransformDataChannel.LocalRotationW) rotTimes.Add(insertion.time);
                    }
                }
                foreach (var insertion in insertionLine)
                {
                    if (!IsRootMotionBoneChild(insertion.bone)) continue;

                    if (insertion.channel == TransformDataChannel.LocalPositionX
                        || insertion.channel == TransformDataChannel.LocalPositionY
                        || insertion.channel == TransformDataChannel.LocalPositionZ) posTimes.Add(insertion.time);
                    else if (insertion.channel == TransformDataChannel.LocalRotationX
                        || insertion.channel == TransformDataChannel.LocalRotationY
                        || insertion.channel == TransformDataChannel.LocalRotationZ
                        || insertion.channel == TransformDataChannel.LocalRotationW) rotTimes.Add(insertion.time);
                }
                insertionLine.RemoveAll(i => i.channel != TransformDataChannel.LocalScaleX && i.channel != TransformDataChannel.LocalScaleY && i.channel != TransformDataChannel.LocalScaleZ && IsRootMotionBoneChild(i.bone)); // children will get readded further along

                if (bakeRootMotionAtSeparateInterval)
                {
                    var length_ = length * 0.998f;

                    int insertionsPos = rootMotionBakeIntervalPos > 0 ? Mathf.FloorToInt(length_ / rootMotionBakeIntervalPos) : 0;
                    int insertionsRot = rootMotionBakeIntervalRot > 0 ? Mathf.FloorToInt(length_ / rootMotionBakeIntervalRot) : 0;

                    for (int i = 0; i < insertionsPos; i++)
                    {
                        float time = i == insertionsPos - 1 ? length_ : (i * resampleIntervalPos);
                        posTimes.Add(time);
                    }

                    for (int i = 0; i < insertionsRot; i++)
                    {
                        float time = i == insertionsRot - 1 ? length_ : (i * resampleIntervalPos); 
                        rotTimes.Add(time);
                    }
                }

                foreach (var time in posTimes)
                {
                    insertionLine.Add(new TransformDataInsertion()
                    {
                        bone = rootMotionBone,
                        time = time,
                        channel = TransformDataChannel.LocalPositionX
                    });
                    insertionLine.Add(new TransformDataInsertion()
                    {
                        bone = rootMotionBone,
                        time = time,
                        channel = TransformDataChannel.LocalPositionY
                    });
                    insertionLine.Add(new TransformDataInsertion()
                    {
                        bone = rootMotionBone,
                        time = time,
                        channel = TransformDataChannel.LocalPositionZ
                    });

                    for (int a = 0; a < rootMotionBone.childCount; a++)
                    {
                        var child = rootMotionBone.GetChild(a);
                        insertionLine.Add(new TransformDataInsertion()
                        {
                            bone = child,
                            time = time,
                            channel = TransformDataChannel.LocalPositionX
                        });
                        insertionLine.Add(new TransformDataInsertion()
                        {
                            bone = child,
                            time = time,
                            channel = TransformDataChannel.LocalPositionY
                        });
                        insertionLine.Add(new TransformDataInsertion()
                        {
                            bone = child,
                            time = time,
                            channel = TransformDataChannel.LocalPositionZ
                        });
                    }
                }
                foreach (var time in rotTimes)
                {
                    insertionLine.Add(new TransformDataInsertion()
                    {
                        bone = rootMotionBone,
                        time = time,
                        channel = TransformDataChannel.LocalRotationX
                    });
                    insertionLine.Add(new TransformDataInsertion()
                    {
                        bone = rootMotionBone,
                        time = time,
                        channel = TransformDataChannel.LocalRotationY
                    });
                    insertionLine.Add(new TransformDataInsertion()
                    {
                        bone = rootMotionBone,
                        time = time,
                        channel = TransformDataChannel.LocalRotationZ
                    });
                    insertionLine.Add(new TransformDataInsertion()
                    {
                        bone = rootMotionBone,
                        time = time,
                        channel = TransformDataChannel.LocalRotationW
                    });

                    for (int a = 0; a < rootMotionBone.childCount; a++)
                    {
                        var child = rootMotionBone.GetChild(a);
                        insertionLine.Add(new TransformDataInsertion()
                        {
                            bone = child,
                            time = time, 
                            channel = TransformDataChannel.LocalRotationX
                        });
                        insertionLine.Add(new TransformDataInsertion() 
                        {
                            bone = child,
                            time = time,
                            channel = TransformDataChannel.LocalRotationY
                        });
                        insertionLine.Add(new TransformDataInsertion()
                        {
                            bone = child,
                            time = time,
                            channel = TransformDataChannel.LocalRotationZ
                        });
                        insertionLine.Add(new TransformDataInsertion()
                        {
                            bone = child,
                            time = time,
                            channel = TransformDataChannel.LocalRotationW
                        });
                    }
                }

                float rootMotionMaxTime = length * bakeRootMotionMaxTime;
                insertionLine.RemoveAll(i => i.time > rootMotionMaxTime && i.bone == rootMotionBone); 
            }

            insertionLine.Sort((TransformDataInsertion dataA, TransformDataInsertion dataB) => (int)Mathf.Sign(dataA.time - dataB.time));
            insertionCount = insertionLine.Count;

            boneRelations.Clear();
            if (bakeFkToIk)
            {
                if (animatorTarget.avatar != null && animatorTarget.avatar.ikBones != null)
                {
                    foreach(var ikBone in animatorTarget.avatar.ikBones)
                    {
                        if (string.IsNullOrWhiteSpace(ikBone.fkParent)) continue;

                        if (!boneRelations.TryGetValue(ikBone.fkParent, out var relations)) relations = new List<BoneRelation>();

                        relations.Add(new BoneRelation()
                        {
                            boneName = ikBone.name,
                            applyOffsets = false,
                            forceInsertPosition = forceBakeIkPosition
                        });

                        boneRelations[ikBone.fkParent] = relations;            
                    }
                }
            }

            boneRotations.Clear();

#if UNITY_EDITOR
            Debug.Log($"Initialized animation bake ({bakeType.ToString()}) with {insertionCount} insertions");
#endif
        }

        public float Progress => (insertionCount - insertionLine.Count) / (float)insertionCount;

        public bool ProgressBake()
        {
            if (IsComplete) return true;

            if (insertionLine.Count > 0 && !waiting)
            {
                lastFrame = Time.frameCount;

                float nextTime = insertionLine[0].time;

                controlLayer.mix = 1;
                if (controlLayer.HasActiveState && controlLayer.ActiveState is CustomAnimationLayerState state && state.MotionControllerIndex >= 0)
                {
                    var mc = controlLayer.GetMotionControllerUnsafe(state.MotionControllerIndex);
                    if (mc is AnimationReference ar)
                    {
                        ar.BaseSpeed = 0;
                        ar.SetTime(controlLayer, nextTime);
#if UNITY_EDITOR
                        //Debug.Log(nextTime);
#endif
                    }
                }

                animatorTarget.ResetToBindPose();
                syncPose?.Invoke();

                if (bakeFkToIk) animatorTarget.SyncIKFK(false); 

                waiting = true;
            }
            
            if ((waiting && Time.frameCount - lastFrame >= insertionFrameDelay) || insertionFrameDelay <= 0)
            {
                waiting = false;

                float bakeTime = insertionLine[0].time;
                toInsert.Clear();
                for (int a = 0; a < insertionLine.Count; a++)
                {
                    var insertion = insertionLine[a];
                    if (insertion.time == bakeTime)
                    {
                        toInsert.Add(insertion);
                        completionLine.Add(insertion);
                    }
                }
                insertionLine.RemoveAll(i => i.time == bakeTime);

                if (bakeRootMotion)
                {
                    if (!rootMotionInitialized)
                    {
                        rootMotionInitialized = true; 

                        CalculateRootMotionPositionAndRotation(out var rootPos_, out var rootRot_);
                        startRootPosition = previousRootPosition = rootPos_;
                        startRootRotation = previousRootRotation = rootRot_;
                    }

                    CalculateRootMotionPositionAndRotation(out var rootPos, out var rootRot);

                    if (rootMotionPositionReferenceBones.Count > 0)
                    {
                        Vector3 rootTranslation = rootPos - startRootPosition;

                        //Debug.DrawLine(startRootPosition, rootPos, Color.Lerp(Color.red, Color.green, 1 - (insertionLine.Count / (float)insertionCount)), 120); 

                        float3 verticalAxis = math.abs((float3)animatorTarget.transform.TransformDirection(animatorTarget.yawAxis));
                        float3 forwardAxis = math.abs((float3)animatorTarget.transform.TransformDirection(animatorTarget.forwardAxis));
                        float3 sidewaysAxis = math.abs(math.cross(forwardAxis, verticalAxis));
                        switch (rootMotionPositionMode)
                        {
                            case ModelEditor.RootMotionPositionMode.Planar: 
                                //rootTranslation.y = 0; 
                                rootTranslation = Vector3.ProjectOnPlane(rootTranslation, verticalAxis); 
                                break;
                            case ModelEditor.RootMotionPositionMode.Vertical:
                                rootTranslation = verticalAxis * Vector3.Dot(rootTranslation, verticalAxis);
                                break;
                            case ModelEditor.RootMotionPositionMode.LeftRight:
                                rootTranslation = sidewaysAxis * Vector3.Dot(rootTranslation, sidewaysAxis);
                                break;
                            case ModelEditor.RootMotionPositionMode.ForwardBack:
                                rootTranslation = forwardAxis * Vector3.Dot(rootTranslation, forwardAxis);
                                break;
                            case ModelEditor.RootMotionPositionMode.LeftRightVertical:
                                rootTranslation = (sidewaysAxis * Vector3.Dot(rootTranslation, sidewaysAxis)) + (verticalAxis * Vector3.Dot(rootTranslation, verticalAxis));  
                                break;
                            case ModelEditor.RootMotionPositionMode.ForwardBackVertical:
                                rootTranslation = (forwardAxis * Vector3.Dot(rootTranslation, forwardAxis)) + (verticalAxis * Vector3.Dot(rootTranslation, verticalAxis));
                                break;
                        }

                        rootMotionBone.position = rootTranslation + (rootMotionBone.parent.rotation * baseRootLocalPosition);
                        for (int a = 0; a < rootMotionBone.childCount; a++)
                        {
                            var child = rootMotionBone.GetChild(a);
                            child.position = child.position - rootTranslation; 
                        }
                    } 

                    if (rootMotionRotationReferenceBones.Count > 0)
                    {
                        Quaternion rootRotation = rootRot * Quaternion.Inverse(startRootRotation);
                        float yaw = Vector3.SignedAngle(animatorTarget.forwardAxis, rootRotation * animatorTarget.forwardAxis, animatorTarget.yawAxis);

                        switch (rootMotionPositionMode)
                        {
                            case ModelEditor.RootMotionPositionMode.LeftRight:
                                yaw = 0;
                                break;
                            case ModelEditor.RootMotionPositionMode.ForwardBack: 
                                yaw = 0;
                                break;
                        } 

                        Quaternion yawRot = Quaternion.AngleAxis(yaw, animatorTarget.yawAxis);
                        Quaternion inverseYawRot = Quaternion.Inverse(yawRot);

                        //Debug.DrawRay(startRootPosition, yawRot * animatorTarget.forwardAxis, Color.Lerp(Color.blue, Color.magenta, 1 - (insertionLine.Count / (float)insertionCount)), 120);

                        rootMotionBone.rotation = yawRot * (rootMotionBone.parent.rotation * baseRootLocalRotation);
                        for (int a = 0; a < rootMotionBone.childCount; a++)
                        {
                            var child = rootMotionBone.GetChild(a);
                            child.rotation = inverseYawRot * child.rotation;
                        } 
                    }

                    previousRootPosition = rootPos;
                    previousRootRotation = rootRot;
                }

                if (bakeFkToIk) animatorTarget.SyncIKFK(false);
                
                List<Transform> insertionBones = new List<Transform>();
                foreach (var insertion in toInsert)
                {
                    if (insertion.bone == null) continue;

                    insertionBones.Clear();
                    insertionBones.Add(insertion.bone);

                    if (boneRelations.TryGetValue(insertion.bone.name, out List<BoneRelation> relations))
                    {
                        foreach (var relation in relations)
                        {
                            var relationBone = animatorTarget.GetUnityBone(relation.boneName);
                            if (relationBone == null) continue;

                            if (relation.applyOffsets)
                            {
                                relationBone.SetPositionAndRotation(insertion.bone.TransformPoint(relation.offsetPosition), insertion.bone.rotation * relation.offsetRotation);
                            }

                            insertionBones.Add(relationBone); 
                        }
                    }
                    
                    void InsertInChannel(TransformDataChannel channel)
                    {
                        switch (channel)
                        {
                            case TransformDataChannel.LocalPositionX:
                                foreach (var bone in insertionBones) AnimationUtils.Pose.InsertElement(new KeyValuePair<string, float>(AnimationUtils.TransformLocalPositionXKey(bone.name), bone.localPosition.x), animatorTarget.avatar, animationSource.rawAnimation, bakeTime, restPoseTarget, null, false, null, false, true, false, false, insertion.useExistingKeyframe, insertion.keyframe, AnimationUtils.InsertAutoSmoothBehaviour.AlwaysLinear);
                                break;
                            case TransformDataChannel.LocalPositionY:
                                foreach (var bone in insertionBones) AnimationUtils.Pose.InsertElement(new KeyValuePair<string, float>(AnimationUtils.TransformLocalPositionYKey(bone.name), bone.localPosition.y), animatorTarget.avatar, animationSource.rawAnimation, bakeTime, restPoseTarget, null, false, null, false, true, false, false, insertion.useExistingKeyframe, insertion.keyframe, AnimationUtils.InsertAutoSmoothBehaviour.AlwaysLinear);
                                break;
                            case TransformDataChannel.LocalPositionZ:
                                foreach (var bone in insertionBones) AnimationUtils.Pose.InsertElement(new KeyValuePair<string, float>(AnimationUtils.TransformLocalPositionZKey(bone.name), bone.localPosition.z), animatorTarget.avatar, animationSource.rawAnimation, bakeTime, restPoseTarget, null, false, null, false, true, false, false, insertion.useExistingKeyframe, insertion.keyframe, AnimationUtils.InsertAutoSmoothBehaviour.AlwaysLinear);
                                break;
                                 
                            case TransformDataChannel.LocalRotationX:
                                foreach (var bone in insertionBones) AnimationUtils.Pose.InsertElement(new KeyValuePair<string, float>(AnimationUtils.TransformLocalRotationXKey(bone.name), CalculateAndStoreBoneRotation(bone, bakeTime).x), animatorTarget.avatar, animationSource.rawAnimation, bakeTime, restPoseTarget, null, false, null, false, false, true, false, insertion.useExistingKeyframe, insertion.keyframe, AnimationUtils.InsertAutoSmoothBehaviour.Always);
                                break; 
                            case TransformDataChannel.LocalRotationY:
                                foreach (var bone in insertionBones) AnimationUtils.Pose.InsertElement(new KeyValuePair<string, float>(AnimationUtils.TransformLocalRotationYKey(bone.name), CalculateAndStoreBoneRotation(bone, bakeTime).y), animatorTarget.avatar, animationSource.rawAnimation, bakeTime, restPoseTarget, null, false, null, false, false, true, false, insertion.useExistingKeyframe, insertion.keyframe, AnimationUtils.InsertAutoSmoothBehaviour.Always);
                                break;
                            case TransformDataChannel.LocalRotationZ:
                                foreach (var bone in insertionBones) AnimationUtils.Pose.InsertElement(new KeyValuePair<string, float>(AnimationUtils.TransformLocalRotationZKey(bone.name), CalculateAndStoreBoneRotation(bone, bakeTime).z), animatorTarget.avatar, animationSource.rawAnimation, bakeTime, restPoseTarget, null, false, null, false, false, true, false, insertion.useExistingKeyframe, insertion.keyframe, AnimationUtils.InsertAutoSmoothBehaviour.Always);
                                break;
                            case TransformDataChannel.LocalRotationW:
                                foreach (var bone in insertionBones) AnimationUtils.Pose.InsertElement(new KeyValuePair<string, float>(AnimationUtils.TransformLocalRotationWKey(bone.name), CalculateAndStoreBoneRotation(bone, bakeTime).w), animatorTarget.avatar, animationSource.rawAnimation, bakeTime, restPoseTarget, null, false, null, false, false, true, false, insertion.useExistingKeyframe, insertion.keyframe, AnimationUtils.InsertAutoSmoothBehaviour.Always);
                                break;

                            case TransformDataChannel.LocalScaleX:
                                foreach (var bone in insertionBones) AnimationUtils.Pose.InsertElement(new KeyValuePair<string, float>(AnimationUtils.TransformLocalScaleXKey(bone.name), bone.localScale.x), animatorTarget.avatar, animationSource.rawAnimation, bakeTime, restPoseTarget, null, false, null, false, false, false, true, insertion.useExistingKeyframe, insertion.keyframe, AnimationUtils.InsertAutoSmoothBehaviour.AlwaysLinear);
                                break;
                            case TransformDataChannel.LocalScaleY:
                                foreach (var bone in insertionBones) AnimationUtils.Pose.InsertElement(new KeyValuePair<string, float>(AnimationUtils.TransformLocalScaleYKey(bone.name), bone.localScale.y), animatorTarget.avatar, animationSource.rawAnimation, bakeTime, restPoseTarget, null, false, null, false, false, false, true, insertion.useExistingKeyframe, insertion.keyframe, AnimationUtils.InsertAutoSmoothBehaviour.AlwaysLinear);
                                break;
                            case TransformDataChannel.LocalScaleZ:
                                foreach (var bone in insertionBones) AnimationUtils.Pose.InsertElement(new KeyValuePair<string, float>(AnimationUtils.TransformLocalScaleZKey(bone.name), bone.localScale.z), animatorTarget.avatar, animationSource.rawAnimation, bakeTime, restPoseTarget, null, false, null, false, false, false, true, insertion.useExistingKeyframe, insertion.keyframe, AnimationUtils.InsertAutoSmoothBehaviour.AlwaysLinear); 
                                break;
                        }
                    }
                    
                    InsertInChannel(insertion.channel);

                    insertionBones.Clear();
                    if (relations != null) // Force position insertions
                    {
                        foreach(var relation in relations)
                        {
                            if (!relation.forceInsertPosition) continue; 

                            var relationBone = animatorTarget.GetUnityBone(relation.boneName);
                            if (relationBone == null) continue;

                            insertionBones.Add(relationBone);
                        }
                        if (insertionBones.Count > 0)
                        {
                            InsertInChannel(TransformDataChannel.LocalPositionX);
                            InsertInChannel(TransformDataChannel.LocalPositionY);
                            InsertInChannel(TransformDataChannel.LocalPositionZ);
                        }
                    }
                    insertionBones.Clear();
                    if (relations != null) // Force rotation insertions
                    {
                        foreach (var relation in relations)
                        {
                            if (!relation.forceInsertRotation) continue;

                            var relationBone = animatorTarget.GetUnityBone(relation.boneName);
                            if (relationBone == null) continue;

                            insertionBones.Add(relationBone);
                        }
                        if (insertionBones.Count > 0)
                        {
                            InsertInChannel(TransformDataChannel.LocalRotationX);
                            InsertInChannel(TransformDataChannel.LocalRotationY);
                            InsertInChannel(TransformDataChannel.LocalRotationZ);
                            InsertInChannel(TransformDataChannel.LocalRotationW);
                        }
                    }
                    insertionBones.Clear();
                    if (relations != null) // Force scale insertions
                    {
                        foreach (var relation in relations)
                        {
                            if (!relation.forceInsertScale) continue;

                            var relationBone = animatorTarget.GetUnityBone(relation.boneName);
                            if (relationBone == null) continue;

                            insertionBones.Add(relationBone); 
                        }
                        if (insertionBones.Count > 0)
                        {
                            InsertInChannel(TransformDataChannel.LocalScaleX);
                            InsertInChannel(TransformDataChannel.LocalScaleY);
                            InsertInChannel(TransformDataChannel.LocalScaleZ);
                        }
                    }
                }
            }

            return insertionLine.Count <= 0 && !waiting;
        }

        public void Complete()
        {
            if (isComplete) return; 

            if (animationSource != null) animationSource.MarkForRecompilation();

            if (completionLine != null)
            {
                void UpdateCurveKeyTangents(EditableAnimationCurve curve, TransformDataInsertion completion) // scale and or flip keyframe tangents based on differences from reference data
                {
                    if (curve.HasKeyAtTime(completion.time, out var key, out var keyIndex))
                    {
                        float amp = AnimationUtils.CalculateAmplitude(curve);
                        float scale = amp / completion.referenceAmplitude;

                        if (completion.hasPreceedingKeyframe && keyIndex - 1 >= 0)
                        {
                            var inKey = curve[keyIndex - 1];
                            key.inTangent *= scale * (Mathf.Sign(completion.preceedingKeyframe.value - completion.keyframe.value) == Mathf.Sign(inKey.value - key.value) ? 1 : -1);
                        }
                        if (completion.hasProceedingKeyframe && keyIndex + 1 <= curve.length)
                        {
                            var outKey = curve[keyIndex + 1];
                            key.outTangent *= scale * (Mathf.Sign(completion.proceedingKeyframe.value - completion.keyframe.value) == Mathf.Sign(outKey.value - key.value) ? 1 : -1);
                        }

                        curve[keyIndex] = key;
                    }
                }

                foreach(var completion in completionLine)
                {
                    if (!completion.useExistingKeyframe || (!completion.hasPreceedingKeyframe && !completion.hasProceedingKeyframe) || completion.referenceAmplitude == 0) continue;

                    if (!animationSource.rawAnimation.TryGetTransformCurve(completion.bone.name, out var curve)) continue;

                    switch (completion.channel)
                    {
                        case TransformDataChannel.LocalPositionX:
                            if (curve.localPositionCurveX != null) UpdateCurveKeyTangents(curve.localPositionCurveX, completion);
                            break;
                        case TransformDataChannel.LocalPositionY:
                            if (curve.localPositionCurveY != null) UpdateCurveKeyTangents(curve.localPositionCurveY, completion);
                            break;
                        case TransformDataChannel.LocalPositionZ:
                            if (curve.localPositionCurveZ != null) UpdateCurveKeyTangents(curve.localPositionCurveZ, completion);
                            break;

                        case TransformDataChannel.LocalRotationX:
                            if (curve.localRotationCurveX != null) UpdateCurveKeyTangents(curve.localRotationCurveX, completion);
                            break;
                        case TransformDataChannel.LocalRotationY:
                            if (curve.localRotationCurveY != null) UpdateCurveKeyTangents(curve.localRotationCurveY, completion);
                            break;
                        case TransformDataChannel.LocalRotationZ:
                            if (curve.localRotationCurveZ != null) UpdateCurveKeyTangents(curve.localRotationCurveZ, completion);
                            break;
                        case TransformDataChannel.LocalRotationW:
                            if (curve.localRotationCurveW != null) UpdateCurveKeyTangents(curve.localRotationCurveW, completion);
                            break;

                        case TransformDataChannel.LocalScaleX:
                            if (curve.localScaleCurveX != null) UpdateCurveKeyTangents(curve.localScaleCurveX, completion);
                            break;
                        case TransformDataChannel.LocalScaleY:
                            if (curve.localScaleCurveY != null) UpdateCurveKeyTangents(curve.localScaleCurveY, completion);
                            break;
                        case TransformDataChannel.LocalScaleZ:
                            if (curve.localScaleCurveZ != null) UpdateCurveKeyTangents(curve.localScaleCurveZ, completion);
                            break;
                    }
                }
            }

            isComplete = true;

            if (animatorReference != null && originalLayers.Count > 0)
            {
                animatorReference.ClearLayers(true);
                animatorReference.AddLayers(originalLayers, false);

                originalLayers.Clear();
            }
        }
    }

    #endregion

}

#endif