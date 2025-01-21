#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;

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
        public int defaultSampleRate = 16;

        public float scaleCompensation = 1;

        public Transform scaleCompensationTransformReference;

        public string rootBoneName = "root";

        [Tooltip("Required. Used for additive animation.")]
        public AnimationClip defaultBaseClip;

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

                outputAnimations = Convert(savePath, defaultBaseClip, inputClips,
                    scaleCompensationTransformReference == null ? scaleCompensation : Mathf.Max(scaleCompensationTransformReference.transform.localScale.x, scaleCompensationTransformReference.transform.localScale.y, scaleCompensationTransformReference.transform.localScale.z) * scaleCompensation,
                    rootBoneName,
                    defaultFrameRate, defaultSampleRate);

            }

        }

        public static string localPositionLabel = "localposition";
        public static string localRotationLabel = "localrotation";
        public static string localScaleLabel = "localScale";

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

            if (propertyNameLiberal.Contains(localPositionLabel)) propertyType = TransformPropertyType.LocalPosition;
            else if (propertyNameLiberal.Contains(localRotationLabel)) propertyType = TransformPropertyType.LocalRotation;
            else if (propertyNameLiberal.Contains(localScaleLabel)) propertyType = TransformPropertyType.LocalScale;

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
#endif

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

        public static CustomAnimationAsset[] Convert(string savePath, ITransformCurve[] defaultBaseTransformCurves, IPropertyCurve[] defaultBasePropertyCurves, ClipPair[] inputClips, float scaleCompensation = 1, string rootBoneName = null, int defaultFrameRate = 30, int defaultSampleRate = 16)
        {

            if (inputClips == null) return new CustomAnimationAsset[0];

            if (!savePath.StartsWith("Assets/")) savePath = "Assets/" + savePath;

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

                List<CustomAnimation.CurveInfoPair> newTransformAnimationCurves = new List<CustomAnimation.CurveInfoPair>();
                List<TransformCurve> newTransformCurves = new List<TransformCurve>();
                List<TransformLinearCurve> newTransformLinearCurves = new List<TransformLinearCurve>();
                Dictionary<string, CustomAnimation.CurveInfo> baseTransformCurveInfo = new Dictionary<string, CustomAnimation.CurveInfo>();
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

                var floatCurveBindings = AnimationUtility.GetCurveBindings(clip);
                var objectReferenceCurveBindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);

                Dictionary<string, TransformCurve> convertedTransformCurves = new Dictionary<string, TransformCurve>();

                foreach (var binding in floatCurveBindings)
                {

                    //Debug.Log($"Float curve [{binding.path}] [{binding.propertyName}]");

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

                if (newTransformAnimationCurves.Count > 0)
                {

                    CustomAnimation newAnimation = new CustomAnimation();
                    newAnimation.framesPerSecond = defaultFrameRate;
                    newAnimation.jobCurveSampleRate = defaultSampleRate;

                    newAnimation.transformCurves = newTransformCurves.ToArray();
                    newAnimation.transformLinearCurves = newTransformLinearCurves.ToArray();

                    newAnimation.transformAnimationCurves = newTransformAnimationCurves.ToArray();

                    var newAnimationAsset = CustomAnimationAsset.Create(savePath, clip.name, newAnimation, true);

                    EditorUtility.SetDirty(newAnimationAsset);
                    AssetDatabase.SaveAssetIfDirty(newAnimationAsset);

                    outputAnims[a] = newAnimationAsset;

                }

            }

            AssetDatabase.Refresh();

#endif

            return outputAnims;

        }

    }
}

#endif