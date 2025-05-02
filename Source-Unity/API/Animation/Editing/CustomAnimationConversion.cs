#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;

#if BULKOUT_ENV
using TriLibCore;
#endif

namespace Swole.API.Unity.Animation
{
    [ExecuteInEditMode]
    public class CustomAnimationConversion : MonoBehaviour
    {

        public bool apply;  

        public string savePath = "Resources/Animations";

        [Tooltip("The expected frame rate of the animation.")]
        public int defaultFrameRate = 30;
        [Tooltip("The resampling rate for transform curves used in jobs.")]
        public int defaultSampleRate = CustomAnimation.DefaultJobCurveSampleRate;//16;

        public float scaleCompensation = 1;

        public Transform scaleCompensationTransformReference;

        public string rootBoneName = "root";

        [Tooltip("Required. Used for additive animation.")]
        public AnimationClip defaultBaseClip;

        public bool createBaseClipFromPosedObject;
        public CustomAnimator animatableObject;

        [Serializable]
        public struct ClipPair
        {

            [Tooltip("Optional clip used for additive animation. If not set, the default base clip will be used.")]
            public AnimationClip baseClip;
            [Tooltip("The clip to be converted.")]
            public AnimationClip mainClip;

        }

        public ClipPair[] inputClips;
        
        public CustomAnimationAsset[] outputAnimations;

        public void Update()
        {
            if (apply)
            {
                apply = false;
                Apply();
            }
        }

        public void Apply()
        {
            if (defaultBaseClip == null && createBaseClipFromPosedObject)
            {
                if (animatableObject == null || animatableObject.avatar == null)
                {
#if UNITY_EDITOR
                    Debug.LogError("Cannot create base clip data from invalid animatable object!");
#else
                    swole.LogError("Cannot create base clip data from invalid animatable object!");
#endif
                    return;
                }

                CreateDefaultCurvesFromPosedObject(animatableObject, out var defaultBaseTransformCurves, out var defaultBasePropertyCurves);

                Apply(defaultBaseTransformCurves, defaultBasePropertyCurves);

                return;
            }

            outputAnimations = Convert(savePath, defaultBaseClip, inputClips,
                scaleCompensationTransformReference == null ? scaleCompensation : Mathf.Max(scaleCompensationTransformReference.transform.localScale.x, scaleCompensationTransformReference.transform.localScale.y, scaleCompensationTransformReference.transform.localScale.z) * scaleCompensation,
                rootBoneName,
                defaultFrameRate, defaultSampleRate);
        }
        public void Apply(ITransformCurve[] defaultBaseTransformCurves, IPropertyCurve[] defaultBasePropertyCurves)
        {
            outputAnimations = Convert(savePath, defaultBaseTransformCurves, defaultBasePropertyCurves, inputClips,
                scaleCompensationTransformReference == null ? scaleCompensation : Mathf.Max(scaleCompensationTransformReference.transform.localScale.x, scaleCompensationTransformReference.transform.localScale.y, scaleCompensationTransformReference.transform.localScale.z) * scaleCompensation,
                rootBoneName,
                defaultFrameRate, defaultSampleRate);
        }

        public static void CreateDefaultCurvesFromPosedObject(CustomAnimator animatableObject, out ITransformCurve[] defaultBaseTransformCurves, out IPropertyCurve[] defaultBasePropertyCurves)
        {
            var bones = animatableObject.Bones;
            CreateDefaultCurvesFromPosedObject(animatableObject.avatar, bones == null ? null : bones.bones, out defaultBaseTransformCurves, out defaultBasePropertyCurves);  
        }
        public static void CreateDefaultCurvesFromPosedObject(CustomAvatar avatar, Transform[] bones, out ITransformCurve[] defaultBaseTransformCurves, out IPropertyCurve[] defaultBasePropertyCurves)
        {
            defaultBaseTransformCurves = null;
            defaultBasePropertyCurves = new IPropertyCurve[0];

            if (bones != null)
            {
                var pose = new AnimationUtils.Pose(bones, avatar, null, 0);
                var tempAnim = new CustomAnimation();
                pose.Insert(avatar, tempAnim, 0);
                defaultBaseTransformCurves = tempAnim.transformCurves;
                tempAnim.Dispose();
            }
            else
            {
                defaultBaseTransformCurves = new ITransformCurve[0];
            }
        }

        public const string _localPositionLabel = "localPosition";
        public const string _localRotationLabel = "localRotation";
        public const string _localScaleLabel = "localScale";

        public const string _localPositionLabel2 = "m_LocalPosition";
        public const string _localRotationLabel2 = "m_LocalRotation";
        public const string _localScaleLabel2 = "m_LocalScale";

        [Serializable]
        public enum TransformPropertyType
        {

            None, LocalPosition, LocalRotation, LocalScale

        }

        [Serializable]
        public enum TransformPropertyComponent
        {

            None, X, Y, Z, W

        }

        public static bool IsTransformProperty(string path, string propertyName, out TransformPropertyType propertyType, out TransformPropertyComponent component, out string outputTransformName, out string outputPropertyName)
        {

            propertyType = TransformPropertyType.None;
            component = TransformPropertyComponent.None;

            outputTransformName = outputPropertyName = "null";

            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(propertyName)) return false;

            string propertyNameLiberal = propertyName.ToLower().Trim();

            if (propertyNameLiberal.Contains(_localPositionLabel.ToLower()) || propertyNameLiberal.Contains(_localPositionLabel2.ToLower())) propertyType = TransformPropertyType.LocalPosition;
            else if (propertyNameLiberal.Contains(_localRotationLabel.ToLower()) || propertyNameLiberal.Contains(_localRotationLabel2.ToLower())) propertyType = TransformPropertyType.LocalRotation;
            else if (propertyNameLiberal.Contains(_localScaleLabel.ToLower()) || propertyNameLiberal.Contains(_localScaleLabel2.ToLower())) propertyType = TransformPropertyType.LocalScale;

            if (propertyNameLiberal.EndsWith("." + TransformPropertyComponent.X.ToString().ToLower())) component = TransformPropertyComponent.X;
            else if (propertyNameLiberal.EndsWith("." + TransformPropertyComponent.Y.ToString().ToLower())) component = TransformPropertyComponent.Y;
            else if (propertyNameLiberal.EndsWith("." + TransformPropertyComponent.Z.ToString().ToLower())) component = TransformPropertyComponent.Z;
            else if (propertyNameLiberal.EndsWith("." + TransformPropertyComponent.W.ToString().ToLower())) component = TransformPropertyComponent.W;

            bool isTransform = propertyType != TransformPropertyType.None && component != TransformPropertyComponent.None;

            if (isTransform)
            {

                int finalSlash = path.LastIndexOf('/');

                outputTransformName = finalSlash >= 0 ? path.Substring(finalSlash + 1, path.Length - (finalSlash + 1)) : path;

                int finalPeriod = propertyName.LastIndexOf('.');

                outputPropertyName = finalPeriod >= 0 ? propertyName.Substring(finalPeriod + 1, propertyName.Length - (finalPeriod + 1)) : propertyName;

            }

            return isTransform;

        }

#if UNITY_EDITOR
        public static void AddDataToCurve(TransformCurve curve, AnimationClip clip, EditorCurveBinding binding, TransformPropertyType propertyType, TransformPropertyComponent component, float scaleCompensation = 1)
        {
            AnimationCurve origCurve = AnimationUtility.GetEditorCurve(clip, binding);
            AddDataToCurve(curve, origCurve, propertyType, component, scaleCompensation);
        }
#endif
        public static void AddDataToCurve(TransformCurve curve, AnimationCurve origCurve, TransformPropertyType propertyType, TransformPropertyComponent component, float scaleCompensation = 1)
        {
            if (origCurve == null) return;

            switch (propertyType)
            {

                default:
                    break;

                case TransformPropertyType.LocalPosition:

                    if (scaleCompensation != 1)
                    {

                        AnimationCurve tempCurve = new AnimationCurve(null);

                        tempCurve.preWrapMode = origCurve.preWrapMode;
                        tempCurve.postWrapMode = origCurve.postWrapMode;

                        Keyframe[] keys = origCurve.keys;

                        for (int a = 0; a < keys.Length; a++)
                        {

                            var keyframe = keys[a];

                            keyframe.value = keyframe.value * scaleCompensation;

                            keys[a] = keyframe;

                        }

                        tempCurve.keys = keys;

                        origCurve = tempCurve;

                    }

                    switch (component)
                    {

                        default:
                            break;

                        case TransformPropertyComponent.X:
                            curve.localPositionCurveX = new EditableAnimationCurve(origCurve);//origCurve;
                            break;

                        case TransformPropertyComponent.Y:
                            curve.localPositionCurveY = new EditableAnimationCurve(origCurve);//origCurve;
                            break;

                        case TransformPropertyComponent.Z:
                            curve.localPositionCurveZ = new EditableAnimationCurve(origCurve);//origCurve;
                            break;

                    }

                    break;

                case TransformPropertyType.LocalRotation:

                    switch (component)
                    {

                        default:
                            break;

                        case TransformPropertyComponent.X:
                            curve.localRotationCurveX = new EditableAnimationCurve(origCurve);//origCurve;
                            break;

                        case TransformPropertyComponent.Y:
                            curve.localRotationCurveY = new EditableAnimationCurve(origCurve);//origCurve;
                            break;

                        case TransformPropertyComponent.Z:
                            curve.localRotationCurveZ = new EditableAnimationCurve(origCurve);//origCurve;
                            break;

                        case TransformPropertyComponent.W:
                            curve.localRotationCurveW = new EditableAnimationCurve(origCurve);//origCurve;
                            break;

                    }

                    break;

                case TransformPropertyType.LocalScale:

                    switch (component)
                    {

                        default:
                            break;

                        case TransformPropertyComponent.X:
                            curve.localScaleCurveX = new EditableAnimationCurve(origCurve);//origCurve;
                            break;

                        case TransformPropertyComponent.Y:
                            curve.localScaleCurveY = new EditableAnimationCurve(origCurve);//origCurve;
                            break;

                        case TransformPropertyComponent.Z:
                            curve.localScaleCurveZ = new EditableAnimationCurve(origCurve);//origCurve;
                            break;

                    }

                    break;

            }

        }

        public static void ExtractBaseCurves(AnimationClip clip, out ITransformCurve[] baseTransformCurves, out IPropertyCurve[] basePropertyCurves, float scaleCompensation = 1, string rootBoneName = null)
        {

            baseTransformCurves = null;
            basePropertyCurves = null;

#if UNITY_EDITOR

            var floatCurveBindings = AnimationUtility.GetCurveBindings(clip);
            var objectReferenceCurveBindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);

            Dictionary<string, TransformCurve> convertedTransformCurves = new Dictionary<string, TransformCurve>();

            foreach (var binding in floatCurveBindings)
            {

                if (IsTransformProperty(binding.path, binding.propertyName, out TransformPropertyType propertyType, out TransformPropertyComponent component, out string outputTransformName, out string outputPropertyName))
                {

                    if (!convertedTransformCurves.TryGetValue(outputTransformName, out TransformCurve transformCurve))
                    {

                        transformCurve = new TransformCurve();
                        transformCurve.name = outputTransformName;
                        transformCurve.isBone = string.IsNullOrEmpty(rootBoneName) ? false : binding.path.Contains(rootBoneName);  

                        convertedTransformCurves[outputTransformName] = transformCurve;

                    }

                    AddDataToCurve(transformCurve, clip, binding, propertyType, component, scaleCompensation);

                }

            }

            baseTransformCurves = convertedTransformCurves.Values.ToArray();

#endif

        }

        public static CustomAnimationAsset[] Convert(string savePath, AnimationClip defaultBaseClip, ClipPair[] inputClips, float scaleCompensation = 1, string rootBoneName = null, int defaultFrameRate = 30, int defaultSampleRate = 16)
        {

            if (defaultBaseClip == null)
            {

                Debug.LogError($"[{nameof(CustomAnimationConversion)}] Default Base Clip cannot be null! It is required for additive animation to work.");

                return null;

            }

            if (defaultBaseClip.isHumanMotion)
            {

                Debug.LogError($"[{nameof(CustomAnimationConversion)}] Default Base Clip '{defaultBaseClip.name}' is for a humanoid rig and cannot be converted!.");

                return null;

            }

            ExtractBaseCurves(defaultBaseClip, out ITransformCurve[] defaultBaseTransformCurves, out IPropertyCurve[] defaultBasePropertyCurves, scaleCompensation, rootBoneName);

            return Convert(savePath, defaultBaseTransformCurves, defaultBasePropertyCurves, inputClips, scaleCompensation, rootBoneName, defaultFrameRate, defaultSampleRate);

        }

        private static void PrepareClipConversion(ITransformCurve[] baseTransformCurves, IPropertyCurve[] basePropertyCurves, 
            out List<CustomAnimation.CurveInfoPair> newTransformAnimationCurves, 
            out List<TransformCurve> newTransformCurves, 
            out List<TransformLinearCurve> newTransformLinearCurves, 
            out Dictionary<string, CustomAnimation.CurveInfo> baseTransformCurveInfo,
            out Dictionary<string, TransformCurve> convertedTransformCurves)
        {
            newTransformAnimationCurves = new List<CustomAnimation.CurveInfoPair>();
            newTransformCurves = new List<TransformCurve>();
            newTransformLinearCurves = new List<TransformLinearCurve>();
            baseTransformCurveInfo = new Dictionary<string, CustomAnimation.CurveInfo>();

            if (baseTransformCurves != null)
            {
                foreach (var baseCurve in baseTransformCurves)
                {

                    if (baseCurve is TransformCurve baseTransformCurve)
                    {

                        baseTransformCurveInfo[baseTransformCurve.name] = new CustomAnimation.CurveInfo() { curveIndex = newTransformCurves.Count, isLinear = false };
                        newTransformCurves.Add(baseTransformCurve);

                    }
                    else if (baseCurve is TransformLinearCurve baseTransformLinearCurve)
                    {

                        baseTransformCurveInfo[baseTransformLinearCurve.name] = new CustomAnimation.CurveInfo() { curveIndex = newTransformLinearCurves.Count, isLinear = true };
                        newTransformLinearCurves.Add(baseTransformLinearCurve);

                    }

                }
            }

            convertedTransformCurves = new Dictionary<string, TransformCurve>();
        }

#if UNITY_EDITOR
        private static void ConvertCurveData(string rootBoneName, float scaleCompensation, AnimationClip clip, EditorCurveBinding binding, Dictionary<string, TransformCurve> convertedTransformCurves)
        {
            AnimationCurve origCurve = AnimationUtility.GetEditorCurve(clip, binding);
            ConvertCurveData(rootBoneName, scaleCompensation, binding.path, binding.propertyName, origCurve, convertedTransformCurves);
        }
#endif
        private static void ConvertCurveData(string rootBoneName, float scaleCompensation, string path, string propertyName, AnimationCurve curve, Dictionary<string, TransformCurve> convertedTransformCurves)
        {
            if (IsTransformProperty(path, propertyName, out TransformPropertyType propertyType, out TransformPropertyComponent component, out string outputTransformName, out string outputPropertyName))
            {
                if (!convertedTransformCurves.TryGetValue(outputTransformName, out TransformCurve transformCurve))
                {

                    transformCurve = new TransformCurve();
                    transformCurve.name = outputTransformName;
                    transformCurve.isBone = string.IsNullOrEmpty(rootBoneName) ? false : path.Contains(rootBoneName);

                    convertedTransformCurves[outputTransformName] = transformCurve;

                }

                AddDataToCurve(transformCurve, curve, propertyType, component, scaleCompensation);
            }
        }
        private static CustomAnimationAsset FinalizeClipConversion(string savePath, string clipName, int defaultFrameRate, int defaultSampleRate, Dictionary<string, TransformCurve> convertedTransformCurves, List<CustomAnimation.CurveInfoPair> newTransformAnimationCurves, List<TransformCurve> newTransformCurves, List<TransformLinearCurve> newTransformLinearCurves, Dictionary<string, CustomAnimation.CurveInfo> baseTransformCurveInfo)
        {
            foreach (var conversion in convertedTransformCurves)
            {

                var mainCurve = conversion.Value;

                if (!baseTransformCurveInfo.TryGetValue(mainCurve.name, out CustomAnimation.CurveInfo infoBase))
                {

                    Debug.LogWarning($"[{nameof(CustomAnimationConversion)}] No base curve found for '{mainCurve.name}'! Setting base curve to main curve...");

                    infoBase = new CustomAnimation.CurveInfo() { curveIndex = newTransformCurves.Count, isLinear = false }; // If all else fails, set base curve to main curve.

                }

                CustomAnimation.CurveInfo infoMain = new CustomAnimation.CurveInfo() { curveIndex = newTransformCurves.Count, isLinear = false };

                newTransformCurves.Add(mainCurve);
                newTransformAnimationCurves.Add(new CustomAnimation.CurveInfoPair() { infoBase = infoBase, infoMain = infoMain });

            }

            /*foreach (var binding in objectReferenceCurveBindings)
            {

                Debug.Log($"Object Reference curve [{binding.path}] [{binding.propertyName}]");

            }*/

            if (newTransformAnimationCurves.Count <= 0)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"Created empty animation for clip '{clipName}'");
#else
                swole.LogWarning($"Created empty animation for clip '{clipName}'");
#endif
            }

            CustomAnimation newAnimation = new CustomAnimation(clipName, string.Empty, DateTime.Now, DateTime.Now, string.Empty, defaultFrameRate, defaultSampleRate, null, newTransformLinearCurves.ToArray(), newTransformCurves.ToArray(), null, null, newTransformAnimationCurves.ToArray(), null, null, default);

            return CustomAnimationAsset.Create(savePath, clipName, newAnimation, true);  
        }

        public static CustomAnimationAsset[] Convert(string savePath, ITransformCurve[] defaultBaseTransformCurves, IPropertyCurve[] defaultBasePropertyCurves, ClipPair[] inputClips, float scaleCompensation = 1, string rootBoneName = null, int defaultFrameRate = 30, int defaultSampleRate = 16)
        {

            if (inputClips == null) return new CustomAnimationAsset[0];

            if (savePath != null && !savePath.StartsWith("Assets/")) savePath = "Assets/" + savePath;

            CustomAnimationAsset[] outputAnims = new CustomAnimationAsset[inputClips.Length];

#if UNITY_EDITOR

            for (int a = 0; a < inputClips.Length; a++)
            {

                var pair = inputClips[a];

                var clip = pair.mainClip;
                if (clip == null) continue;

                if (clip.isHumanMotion)
                {

                    Debug.LogWarning($"[{nameof(CustomAnimationConversion)}] Input clip '{clip.name}' is for a humanoid rig and cannot be converted!");

                    continue;

                }

                ITransformCurve[] baseTransformCurves;
                IPropertyCurve[] basePropertyCurves;

                var baseClip = pair.baseClip;
                if (baseClip == null)
                {

                    baseTransformCurves = defaultBaseTransformCurves;
                    basePropertyCurves = defaultBasePropertyCurves;

                }
                else
                {

                    ExtractBaseCurves(baseClip, out baseTransformCurves, out basePropertyCurves, scaleCompensation, rootBoneName);

                }

                PrepareClipConversion(baseTransformCurves, basePropertyCurves, out var newTransformAnimationCurves, out var newTransformCurves, out var newTransformLinearCurves, out var baseTransformCurveInfo, out var convertedTransformCurves);
                
                var floatCurveBindings = AnimationUtility.GetCurveBindings(clip);
                var objectReferenceCurveBindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);

                foreach (var binding in floatCurveBindings)
                {

                    //Debug.Log($"Float curve [{binding.path}] [{binding.propertyName}]");
                    ConvertCurveData(rootBoneName, scaleCompensation, clip, binding, convertedTransformCurves);
                }

                string clipName = clip.name;
                if (string.IsNullOrWhiteSpace(clipName))
                {
                    clipName = $"anim_{a}";
                }

                var newAnimationAsset = FinalizeClipConversion(savePath, clipName, defaultFrameRate, defaultSampleRate, convertedTransformCurves, newTransformAnimationCurves, newTransformCurves, newTransformLinearCurves, baseTransformCurveInfo);
                if (!string.IsNullOrWhiteSpace(savePath))
                {
                    EditorUtility.SetDirty(newAnimationAsset);
                    AssetDatabase.SaveAssetIfDirty(newAnimationAsset);
                }

                outputAnims[a] = newAnimationAsset;

            }

            AssetDatabase.Refresh();

#endif
            
            return outputAnims; 

        }

        #region TriLib

#if BULKOUT_ENV

        public static List<CustomAnimationAsset> ConvertIntoList(AssetLoaderContext assetLoaderContext, ITransformCurve[] baseTransformCurves, IPropertyCurve[] basePropertyCurves, string rootBoneName, int defaultFrameRate, int defaultSampleRate, List<CustomAnimationAsset> outputList, float scaleCompensation = 1)
        {
            if (outputList == null) outputList = new List<CustomAnimationAsset>();

            // referenced from TriLib AssetLoader.cs CreateAnimation (line 925)
            var clips = assetLoaderContext.RootModel.AllAnimations;
            foreach (var clip in clips)
            {
                var animationCurveBindings = clip.AnimationCurveBindings;
                if (animationCurveBindings == null) continue;

                PrepareClipConversion(baseTransformCurves, basePropertyCurves, out var newTransformAnimationCurves, out var newTransformCurves, out var newTransformLinearCurves, out var baseTransformCurveInfo, out var convertedTransformCurves);

                for (var i = animationCurveBindings.Count - 1; i >= 0; i--)
                {
                    var animationCurveBinding = animationCurveBindings[i];
                    var animationCurves = animationCurveBinding.AnimationCurves;
                    if (!assetLoaderContext.GameObjects.ContainsKey(animationCurveBinding.Model))
                    {
                        continue;
                    }
                    var gameObject = assetLoaderContext.GameObjects[animationCurveBinding.Model];
                    for (var j = 0; j < animationCurves.Count; j++)
                    {
                        var animationCurve = animationCurves[j];
                        var unityAnimationCurve = animationCurve.AnimationCurve;
                        var gameObjectPath = assetLoaderContext.GameObjectPaths[gameObject]; 
                        var propertyName = animationCurve.Property;
                        var propertyType = animationCurve.AnimatedType;

                        //Debug.Log(gameObjectPath + " :::: " + propertyName + " :::::: " + propertyType);
                        if (propertyType == typeof(UnityEngine.Transform))
                        {
                            ConvertCurveData(rootBoneName, scaleCompensation, gameObjectPath, propertyName, unityAnimationCurve, convertedTransformCurves);  
                        }
                    }
                }

                string clipName = clip.Name;
                if (string.IsNullOrWhiteSpace(clipName))
                {
                    clipName = $"anim_{outputList.Count}";
                }

                var newAnimationAsset = FinalizeClipConversion(null, clipName, defaultFrameRate, defaultSampleRate, convertedTransformCurves, newTransformAnimationCurves, newTransformCurves, newTransformLinearCurves, baseTransformCurveInfo);
                outputList.Add(newAnimationAsset);
            }

            return outputList;
        }
        public static List<CustomAnimationAsset> ConvertIntoList(AssetLoaderContext assetLoaderContext, ITransformCurve[] baseTransformCurves, IPropertyCurve[] basePropertyCurves, string rootBoneName, int defaultFrameRate, int defaultSampleRate, float scaleCompensation = 1) => ConvertIntoList(assetLoaderContext, baseTransformCurves, basePropertyCurves, rootBoneName, defaultFrameRate, defaultSampleRate, null, scaleCompensation);
        public static CustomAnimationAsset[] Convert(AssetLoaderContext assetLoaderContext, ITransformCurve[] baseTransformCurves, IPropertyCurve[] basePropertyCurves, string rootBoneName, int defaultFrameRate, int defaultSampleRate, float scaleCompensation = 1) => ConvertIntoList(assetLoaderContext, baseTransformCurves, basePropertyCurves, rootBoneName, defaultFrameRate, defaultSampleRate, null, scaleCompensation).ToArray();

#endif

        #endregion

        [Serializable]
        public struct BoneRemapping
        {
            public string originalBone;
            public string newBone;

            public Vector3 originalRollAxis;
            public Vector3 originalPitchAxis;

            public Vector3 newRollAxis;
            public Vector3 newPitchAxis;

            public string originalParent;
            public string newParent;
        }
        [Serializable]
        public struct NamedBindpose
        {
            public string bone;
            public Matrix4x4 pose;
        }
        public static void Remap(Vector3 originalForwardWorld, Vector3 originalUpWorld, Vector3 newForwardWorld, Vector3 newUpWorld, CustomAnimation animation, IEnumerable<BoneRemapping> remappings, IEnumerable<NamedBindpose> originalBindpose = null, IEnumerable<NamedBindpose> newBindpose = null)
        {

            Quaternion axisConvert = Quaternion.FromToRotation(originalUpWorld.normalized, newUpWorld.normalized);
            Vector3 currentForwardAxis = axisConvert * originalForwardWorld.normalized;
            axisConvert = Quaternion.AngleAxis(Vector3.SignedAngle(currentForwardAxis, newForwardWorld.normalized, newUpWorld.normalized), newUpWorld.normalized) * axisConvert; 

            Matrix4x4 GetOriginalBP(string boneName)
            {
                foreach (var bp in originalBindpose)
                {
                    if (bp.bone == boneName) return bp.pose;
                }

                return Matrix4x4.identity;
            }
            Matrix4x4 GetNewBP(string boneName)
            {
                foreach (var bp in newBindpose)
                {
                    if (bp.bone == boneName) return bp.pose;
                }

                return Matrix4x4.identity;
            }

            foreach (var remapping in remappings)
            {
                if (originalBindpose != null && newBindpose != null)
                {
                    bool flagA = false;
                    Matrix4x4 origBP = Matrix4x4.identity;
                    foreach (var bp in originalBindpose)
                    {
                        if (bp.bone == remapping.originalBone)
                        {
                            flagA = true;
                            origBP = bp.pose;
                            break;
                        }
                    }
                    bool flagB = false;
                    Matrix4x4 newBP = Matrix4x4.identity;
                    foreach (var bp in newBindpose)
                    {
                        if (bp.bone == remapping.newBone)
                        {
                            flagB = true;
                            newBP = bp.pose;
                            break;
                        }
                    }
                    if (flagA && flagB)
                    {
                        //Quaternion axisConvert = Quaternion.FromToRotation(remapping.originalRollAxis.normalized, remapping.newRollAxis.normalized);
                        //Vector3 currentPitchAxis = axisConvert * remapping.originalPitchAxis.normalized;
                        //axisConvert = Quaternion.AngleAxis(Vector3.SignedAngle(currentPitchAxis, remapping.newPitchAxis.normalized, remapping.newRollAxis.normalized), remapping.newRollAxis.normalized) * axisConvert;

                        //Matrix4x4 offsetBP =  origBP * Matrix4x4.Inverse(newBP);
                        //Quaternion offsetR = offsetBP.rotation;



                        Matrix4x4 PorigLTW = GetOriginalBP(remapping.originalParent);
                        Matrix4x4 PorigWTL = Matrix4x4.Inverse(PorigLTW);
                        Quaternion PorigWTL_R = PorigWTL.rotation;
                        Quaternion PorigLTW_R = PorigLTW.rotation;

                        Matrix4x4 PnewLTW = GetOriginalBP(remapping.newParent);
                        Matrix4x4 PnewWTL = Matrix4x4.Inverse(PnewLTW);
                        Quaternion PnewWTL_R = PnewWTL.rotation;
                        Quaternion PnewLTW_R = PnewLTW.rotation;




                        Matrix4x4 origLTW = origBP;// GetOriginalBP(remapping.originalParent);
                        Matrix4x4 origWTL = Matrix4x4.Inverse(origLTW);
                        Quaternion origWTL_R = origWTL.rotation;
                        Quaternion origLTW_R = origLTW.rotation;

                        Matrix4x4 newLTW = newBP;// GetOriginalBP(remapping.newParent);
                        Matrix4x4 newWTL = Matrix4x4.Inverse(newLTW);
                        Quaternion newWTL_R = newWTL.rotation;
                        Quaternion newLTW_R = newLTW.rotation;

                        Matrix4x4 offsetBP = Matrix4x4.Inverse(origLTW) * newLTW;
                        Quaternion offsetR = offsetBP.rotation;

                        Vector3 origRollAxisW = origLTW.MultiplyVector(remapping.originalRollAxis.normalized);
                        Vector3 origPitchAxisW = origLTW.MultiplyVector(remapping.originalPitchAxis.normalized);
                        Vector3 newRollAxisW = newLTW.MultiplyVector(remapping.newRollAxis.normalized);
                        Vector3 newPitchAxisW = newLTW.MultiplyVector(remapping.newPitchAxis.normalized);

                        //axisConvert = Quaternion.FromToRotation(origRollAxisW, newRollAxisW);
                        //Vector3 currentPitchAxis = axisConvert * origPitchAxisW;
                        //axisConvert = Quaternion.AngleAxis(Vector3.SignedAngle(currentPitchAxis, newPitchAxisW, newRollAxisW), newRollAxisW) * axisConvert;
                        //axisConvert = newWTL_R * axisConvert;

                        void ApplyOffsetToCurve(CustomAnimation.CurveInfo curveInfo)
                        {
                            if (curveInfo.curveIndex < 0) return;

                            if (curveInfo.isLinear)
                            {
                                var curve = animation.transformLinearCurves[curveInfo.curveIndex];
                                if (curve.TransformName != remapping.originalBone) return;

                                List<ITransformCurve.Frame> newFrames = new List<ITransformCurve.Frame>();
                                for (int a = 0; a < curve.frames.Length; a++)
                                {
                                    var oldFrame = curve.frames[a];
                                    var newFrame = new ITransformCurve.Frame();

                                    newFrame.interpolationCurve = oldFrame.interpolationCurve;
                                    newFrame.timelinePosition = oldFrame.timelinePosition;
                                    var newData = oldFrame.data;

                                    //newData.localPosition = newWTL.MultiplyPoint(offsetBP.MultiplyPoint(origLTW.MultiplyPoint(oldFrame.data.localPosition)));
                                    //newData.localRotation = newWTL_R * (offsetR * (origLTW_R * oldFrame.data.localRotation));

                                    newFrame.data = newData;

                                    newFrames.Add(newFrame);
                                }
                                curve.frames = newFrames.ToArray();
                            }
                            else
                            {
                                var curve = animation.transformCurves[curveInfo.curveIndex];
                                if (curve.TransformName != remapping.originalBone) return; 

                                var frameTimes = curve.GetFrameTimes(30); 
                                float length = curve.GetLengthInSeconds(30);
                                Dictionary<float, ITransformCurve.Data> newFrames = new Dictionary<float, ITransformCurve.Data>();
                                foreach (var t in frameTimes)
                                {
                                    var data = curve.Evaluate(length > 0 ? (t / length) : t);
                                    //Debug.Log(t + " : " + data.localPosition + " :::: " + ((Quaternion)data.localRotation).eulerAngles + " ::::: " + data.localScale);

                                    //data.localPosition = axisConvert * offsetBP.MultiplyPoint(data.localPosition); 
                                    //data.localRotation = axisConvert * (offsetR * data.localRotation);  

                                    //data.localPosition = newWTL.MultiplyPoint(axisConvert * origLTW.MultiplyPoint(data.localPosition));
                                    //data.localRotation = newWTL_R * (axisConvert * (origLTW_R * data.localRotation));

                                    //data.localPosition = newWTL.MultiplyPoint(offsetBP.MultiplyPoint(origLTW.MultiplyPoint(data.localPosition)));
                                    //data.localRotation = newWTL_R * (offsetR * (origLTW_R * data.localRotation));

                                    //data.localPosition = axisConvert * (newWTL.MultiplyPoint(origLTW.MultiplyPoint(data.localPosition)));  
                                    //data.localRotation = axisConvert * (newWTL_R * (origLTW_R * data.localRotation));

                                    //data.localPosition = PnewWTL.MultiplyPoint(newLTW.MultiplyPoint(origWTL.MultiplyPoint(PorigLTW.MultiplyPoint(data.localPosition))));  
                                    //data.localRotation = PnewWTL_R * (newLTW_R * (origWTL_R * (PorigLTW_R * data.localRotation))); 

                                    data.localPosition = (PnewLTW_R * newWTL_R) * PnewWTL.MultiplyVector(axisConvert * PorigLTW.MultiplyVector(Quaternion.Inverse(PorigLTW_R * origWTL_R) * data.localPosition)); 
                                    data.localRotation = (PnewLTW_R * newWTL_R) * PnewWTL_R * (axisConvert * (PorigLTW_R * (Quaternion.Inverse(PorigLTW_R * origWTL_R) * data.localRotation)));  

                                    newFrames[t] = data;  
                                }

                                curve.ClearKeys();
                                var tangentSettings = new AnimationCurveEditor.KeyframeTangentSettings() { inTangentMode = AnimationCurveEditor.BrokenTangentMode.Free, outTangentMode = AnimationCurveEditor.BrokenTangentMode.Free, tangentMode = AnimationCurveEditor.TangentMode.Broken };
                                foreach (var frame in newFrames)
                                {
                                    //Debug.Log(frame.Key + " : " + frame.Value.localPosition + " :::: " + ((Quaternion)frame.Value.localRotation).eulerAngles + " ::::: " + frame.Value.localScale);
                                    curve.InsertData(frame.Key, frame.Value, tangentSettings);
                                }
                            }
                        }

                        if (animation.transformAnimationCurves != null)
                        {
                            foreach (var pair in animation.transformAnimationCurves)
                            {
                                ApplyOffsetToCurve(pair.infoBase); 
                                ApplyOffsetToCurve(pair.infoMain); 
                            }
                        }
                    }
                }

                if (animation.transformCurves != null)
                {
                    for(int a = 0; a < animation.transformCurves.Length; a++)
                    {
                        var curve = animation.transformCurves[a];
                        if (curve == null || curve.TransformName != remapping.originalBone) continue; 

                        curve.name = remapping.newBone; 
                    }
                }

                if (animation.transformLinearCurves != null)
                {
                    for (int a = 0; a < animation.transformLinearCurves.Length; a++)
                    {
                        var curve = animation.transformLinearCurves[a];
                        if (curve == null || curve.TransformName != remapping.originalBone) continue;

                        curve.name = remapping.newBone;
                    }
                }
            }

            animation.FlushJobData();  
        }

    }
}

#endif