#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Unity.Mathematics;

namespace Swole.API.Unity.Animation
{
    public static class AnimationUtils
    {

        public const string _localPositionProperty = ".localPosition";
        public const string _localRotationProperty = ".localRotation";
        public const string _localScaleProperty = ".localScale";

        public const string _propertyX = ".x";
        public const string _propertyY = ".y";
        public const string _propertyZ = ".z";
        public const string _propertyW = ".w";

        [Serializable]
        public struct AnimatableElement
        {
            public string id;
            public float value;
        }

        [Serializable]
        public struct AnimatableElementDelta
        {
            public string id;
            public float change;
            public bool isAbsolute;
        }

        [Serializable]
        public struct TransformState
        {
            public Vector3 localPosition;
            public Quaternion localRotation;
            public Vector3 localScale;
        }

        [Serializable]
        public class LoopPoint
        {
            public string id;
            public int position;
        }

        private const float _clampedAutoFalloff = 1 / 3f;
        public static Keyframe AutoSmoothKeyframe(Keyframe keyframe, bool hasLeftNeighbor, bool hasRightNeighbor, Keyframe leftNeighbor, Keyframe rightNeighbor)
        {
            keyframe.inTangent = keyframe.outTangent = 0;

            if (hasLeftNeighbor && hasRightNeighbor)
            {
                float mul = Mathf.Clamp01(Mathf.Abs((AnimationCurveEditor.GetValueInRange(keyframe.value, leftNeighbor.value, rightNeighbor.value) - 0.5f) / 0.5f));
                mul = 1 - (Mathf.Max(0, mul - (1 - _clampedAutoFalloff)) / _clampedAutoFalloff);
                keyframe.outTangent = keyframe.inTangent = ((rightNeighbor.value - leftNeighbor.value) / (rightNeighbor.time - leftNeighbor.time)) * mul;
            }

            return keyframe; 
        }

        [Serializable]
        public enum InsertAutoSmoothBehaviour
        {
            Always, IfNew, Never
        }
        private static List<Keyframe> _tempKeyframes = new List<Keyframe>();
        public static void DeleteKeysAt(this AnimationCurve curve, float time, IntFromDecimalDelegate getFrameIndex = null)
        {
            _tempKeyframes.Clear();
            _tempKeyframes.AddRange(curve.keys);

            bool useFrameIndices = getFrameIndex != null;
            int frameIndex = useFrameIndices ? getFrameIndex((decimal)time) : 0;
            _tempKeyframes.RemoveAll(i => (useFrameIndices ? (getFrameIndex((decimal)i.time) == frameIndex) : (i.time == time)));

            curve.keys = _tempKeyframes.ToArray();
        }
        public static void InsertKey(this AnimationCurve curve, Keyframe keyframe, bool addTimeZeroKeyframeIfEmpty = false, bool useNewDataForTimeZeroKey = false, Keyframe timeZeroKey = default, InsertAutoSmoothBehaviour autoSmooth = default)
        {
            if (curve == null) return;
            Keyframe[] keyframes = curve.keys;
            int pos = keyframes.Length;
            bool replace = false;
            for (int a = 0; a < keyframes.Length; a++)
            {
                var kf = keyframes[a];
                if (kf.time == keyframe.time)
                {
                    pos = a;
                    replace = true;
                    break;
                }
                else if (kf.time > keyframe.time)
                {
                    pos = a - 1;
                    break;
                }
            }
            pos = Mathf.Max(0, pos);
            if (replace)
            {
                keyframes[pos] = keyframe;
            }
            else
            {
                if (keyframes.Length <= 0 && addTimeZeroKeyframeIfEmpty && keyframe.time != 0)
                {
                    var kfzero = useNewDataForTimeZeroKey ? timeZeroKey : keyframe;
                    kfzero.time = 0;
                    keyframes = (Keyframe[])keyframes.Add(kfzero, 0);
                    pos++;
                }
                keyframes = (Keyframe[])keyframes.Add(keyframe, pos);
            }

            bool autoSmooth_ = false;
            switch(autoSmooth)
            {
                case InsertAutoSmoothBehaviour.Always:
                    autoSmooth_ = true;
                    break;
                case InsertAutoSmoothBehaviour.IfNew:
                    autoSmooth_ = !replace;
                    break;
            }
            if (autoSmooth_)
            {
                keyframe = AutoSmoothKeyframe(keyframe, pos > 0, pos < keyframes.Length - 1, pos > 0 ? keyframes[pos - 1] : keyframe, pos < keyframes.Length - 1 ? keyframes[keyframes.Length - 1] : keyframe);
                keyframes[pos] = keyframe;
            }

            curve.keys = keyframes;
        }
        public static void AddOrReplaceKey(this AnimationCurve curve, float time, float value, bool addTimeZeroKeyframeIfEmpty = false, bool useNewDataForTimeZeroKey = false, float timeZeroData = 0, InsertAutoSmoothBehaviour autoSmooth = default)
        {
            if (curve == null) return;

            if (curve.length <= 0 && addTimeZeroKeyframeIfEmpty && time != 0) curve.AddKey(0, useNewDataForTimeZeroKey ? timeZeroData : value);

            bool replace = false;
            Keyframe[] keyframes = null;
            int pos = curve.AddKey(time, value);
            if (pos < 0)
            {
                replace = true;
               keyframes = curve.keys;
                for(int a = 0; a < keyframes.Length; a++)
                {
                    pos=a;
                    var key = keyframes[pos]; 
                    if (key.time == time)
                    {
                        key.value = value;
                        keyframes[pos] = key;
                        break;
                    }
                }
                curve.keys = keyframes; 
            } 

            bool autoSmooth_ = false;
            switch (autoSmooth)
            {
                case InsertAutoSmoothBehaviour.Always:
                    autoSmooth_ = true;
                    break;
                case InsertAutoSmoothBehaviour.IfNew:
                    autoSmooth_ = !replace;
                    break;
            }
            if (autoSmooth_)
            {
                if (keyframes == null) keyframes = curve.keys;

                var keyframe = keyframes[pos];
                keyframe = AutoSmoothKeyframe(keyframe, pos > 0, pos < keyframes.Length - 1, pos > 0 ? keyframes[pos - 1] : keyframe, pos < keyframes.Length - 1 ? keyframes[keyframes.Length - 1] : keyframe);
                keyframes[pos] = keyframe;

                curve.keys = keyframes;
            }
        }

        public static float GetProperty(object startInstance, string[] propertyChain, int startIndex = 0)
        {
            if (startInstance == null || propertyChain == null || startIndex >= propertyChain.Length) return 0;

            int index = startIndex;
            object instance = startInstance;

            Type type = null;
            FieldInfo field = null;
            PropertyInfo prop = null;

            while (instance != null && index < propertyChain.Length - 1) // Loop until reaching one before final index of chain
            {
                type = instance.GetType();
                field = type.GetField(propertyChain[index], BindingFlags.Instance | BindingFlags.Public);
                if (field == null)
                {
                    prop = type.GetProperty(propertyChain[index], BindingFlags.Instance | BindingFlags.Public);
                    if (prop == null) break;
                }
                else
                {
                    prop = null;
                }
                if (field != null)
                {
                    instance = field.GetValue(instance);
                }
                else
                {
                    instance = prop.GetValue(instance);
                }
                index++;
            }

            if (instance != null)
            {
                type = instance.GetType();
                field = type.GetField(propertyChain[index], BindingFlags.Instance | BindingFlags.Public);
                if (field == null)
                {
                    prop = type.GetProperty(propertyChain[index], BindingFlags.Instance | BindingFlags.Public);
                }
                else
                {
                    prop = null;
                }
                if (field != null)
                {
                    if (typeof(bool) == field.FieldType) return ((bool)field.GetValue(instance)) ? 1 : 0; else return (float)field.GetValue(instance);
                }
                else if (prop != null)
                {
                    if (typeof(bool) == prop.PropertyType) return ((bool)prop.GetValue(instance)) ? 1 : 0; else return (float)prop.GetValue(instance);
                }
            }

            return 0;
        }

        public static bool SetProperty(object startInstance, string[] propertyChain, float value, int startIndex=0)
        {
            if (startInstance == null || propertyChain == null || startIndex >= propertyChain.Length) return false;

            int index = startIndex;
            object instance = startInstance;

            Type type = null;
            FieldInfo field = null;
            PropertyInfo prop = null;

            while (instance != null && index < propertyChain.Length - 1) // Loop until reaching one before final index of chain
            {
                type = instance.GetType();
                field = type.GetField(propertyChain[index], BindingFlags.Instance | BindingFlags.Public);
                if (field == null)
                {
                    prop = type.GetProperty(propertyChain[index], BindingFlags.Instance | BindingFlags.Public);
                    if (prop == null) break;
                }
                else
                {
                    prop = null;
                }
                if (field != null)
                {
                    instance = field.GetValue(instance);
                } 
                else
                {
                    instance = prop.GetValue(instance);
                }
                index++;
            }

            if (instance != null)
            {
                type = instance.GetType();
                field = type.GetField(propertyChain[index], BindingFlags.Instance | BindingFlags.Public);
                if (field == null)
                {
                    prop = type.GetProperty(propertyChain[index], BindingFlags.Instance | BindingFlags.Public);
                }
                else
                {
                    prop = null;
                }
                if (field != null)
                {
                    if (typeof(bool) == field.FieldType) field.SetValue(instance, value.AsBool()); else field.SetValue(instance, value);
                    return true;
                }
                else if (prop != null)
                {
                    if (typeof(bool) == prop.PropertyType) prop.SetValue(instance, value.AsBool()); else prop.SetValue(instance, value);
                    return true;
                }
            }

            return false;
        }

        public delegate Transform FindTransformDelegate(string name);

        public static bool TryExtractTransformFromPropertyString(string propertyId, FindTransformDelegate findTransform, out Transform transform)
        {
            string[] substrings = propertyId.Split('.');
            return TryExtractTransformFromPropertyString(substrings, findTransform, out transform, out _);
        }
        public static bool TryExtractTransformFromPropertyString(string[] propertySubstrings, FindTransformDelegate findTransform, out Transform transform, out int finalSubstringIndex)
        {
            transform = null;
            for (int a = propertySubstrings.Length - 1; a >= 0; a--)
            {
                var t = findTransform(string.Join('.', propertySubstrings, 0, a + 1));
                if (t != null)
                {
                    transform = t;
                    finalSubstringIndex = a;
                    return true;
                }
            }

            finalSubstringIndex = -1;
            return false;
        }
        public static float GetProperty(string propertyId, FindTransformDelegate findTransform)
        {
            if (string.IsNullOrEmpty(propertyId)) return 0;

            string[] substrings = propertyId.Split('.');
            if (substrings.Length < 2) return 0;

            Transform objTransform = null;
            if (!TryExtractTransformFromPropertyString(substrings, findTransform, out objTransform, out int finalSubstringIndex) || finalSubstringIndex >= substrings.Length - 1) return 0;

            Type behaviourType = CustomAnimator.FindComponentTypes(substrings[finalSubstringIndex + 1]);
            if (behaviourType == null) behaviourType = typeof(Transform);

            Component component = objTransform.GetComponent(behaviourType);
            if (component == null) return 0;

            return GetProperty(component, substrings, finalSubstringIndex + 2);
        }
        public static bool SetProperty(string propertyId, float value, FindTransformDelegate findTransform)
        {
            if (string.IsNullOrEmpty(propertyId)) return false;

            string[] substrings = propertyId.Split('.');

            if (substrings.Length < 2) return false;

            Transform objTransform = null;
            if (!TryExtractTransformFromPropertyString(substrings, findTransform, out objTransform, out int finalSubstringIndex) || finalSubstringIndex >= substrings.Length - 1) return false;

            Type behaviourType = CustomAnimator.FindComponentTypes(substrings[finalSubstringIndex + 1]);
            if (behaviourType == null) behaviourType = typeof(Transform);

            Component component = objTransform.GetComponent(behaviourType);
            if (component == null) return false;

            return SetProperty(component, substrings, value, finalSubstringIndex + 2);
        }

        /// <summary>
        /// Creates a single keyframe base curve that represents the rest pose of the transform
        /// </summary>
        public static TransformCurve GetNewBaseTransformCurve(string transformName, Pose restPose)
        {
            if (restPose == null) return null;
            var curve = TransformCurve.NewInstance;
            curve.name = transformName;
            //curve.isBone <- might need to do something with this

            float value;

            if (restPose.TryGetValueLiberal($"{transformName}{_localPositionProperty}{_propertyX}", out value)) curve.localPositionCurveX.keys = new Keyframe[] { new Keyframe() { time = 0, value = value } };
            if (restPose.TryGetValueLiberal($"{transformName}{_localPositionProperty}{_propertyY}", out value)) curve.localPositionCurveY.keys = new Keyframe[] { new Keyframe() { time = 0, value = value } };
            if (restPose.TryGetValueLiberal($"{transformName}{_localPositionProperty}{_propertyZ}", out value)) curve.localPositionCurveZ.keys = new Keyframe[] { new Keyframe() { time = 0, value = value } };

            if (restPose.TryGetValueLiberal($"{transformName}{_localRotationProperty}{_propertyX}", out value)) curve.localRotationCurveX.keys = new Keyframe[] { new Keyframe() { time = 0, value = value } };
            if (restPose.TryGetValueLiberal($"{transformName}{_localRotationProperty}{_propertyY}", out value)) curve.localRotationCurveY.keys = new Keyframe[] { new Keyframe() { time = 0, value = value } };
            if (restPose.TryGetValueLiberal($"{transformName}{_localRotationProperty}{_propertyZ}", out value)) curve.localRotationCurveZ.keys = new Keyframe[] { new Keyframe() { time = 0, value = value } };
            if (restPose.TryGetValueLiberal($"{transformName}{_localRotationProperty}{_propertyW}", out value)) curve.localRotationCurveW.keys = new Keyframe[] { new Keyframe() { time = 0, value = value } };

            if (restPose.TryGetValueLiberal($"{transformName}{_localScaleProperty}{_propertyX}", out value)) curve.localScaleCurveX.keys = new Keyframe[] { new Keyframe() { time = 0, value = value } };
            if (restPose.TryGetValueLiberal($"{transformName}{_localScaleProperty}{_propertyY}", out value)) curve.localScaleCurveY.keys = new Keyframe[] { new Keyframe() { time = 0, value = value } };
            if (restPose.TryGetValueLiberal($"{transformName}{_localScaleProperty}{_propertyZ}", out value)) curve.localScaleCurveZ.keys = new Keyframe[] { new Keyframe() { time = 0, value = value } };

            return curve;
        }
        /// <summary>
        /// Creates a single keyframe base curve that represents the rest value of the property
        /// </summary>
        public static PropertyCurve GetNewBasePropertyCurve(string propertyString, Pose restPose)
        {
            if (restPose == null) return null;
            var curve = PropertyCurve.NewInstance;
            curve.name = propertyString;

            restPose.TryGetValueLiberal($"{propertyString}", out float value);
            curve.propertyValueCurve.keys = new Keyframe[] { new Keyframe() { time = 0, value = value } };

            return curve;
        }

        public delegate void InsertIntoTransformCurveDelegate(ITransformCurve curve);
        public delegate void InsertIntoPropertyCurveDelegate(IPropertyCurve curve);

        private static void EmptyInsertion(ITransformCurve curve) { }
        private static void EmptyInsertion(IPropertyCurve curve) { }

        public static void GetOrCreateTransformCurve(out ITransformCurve mainCurve, string transformName, CustomAnimation animation, Pose restPose = null) => GetOrCreateTransformCurve(out _, out _, out mainCurve, out _, transformName, animation, restPose);
        public static void GetOrCreateTransformCurve(out int index, out CustomAnimation.CurveInfoPair info, out ITransformCurve mainCurve, out ITransformCurve baseCurve, string transformName, CustomAnimation animation, Pose restPose = null) => InsertIntoTransformCurve(out index, out info, out mainCurve, out baseCurve, transformName, animation, EmptyInsertion, restPose);
        public static void InsertIntoTransformCurve(string transformName, CustomAnimation animation, InsertIntoTransformCurveDelegate insertionAction, Pose restPose = null) => InsertIntoTransformCurve(out _, out _, out _, out _, transformName, animation, insertionAction, restPose);
        /// <summary>
        /// Handles insertion of element data into new or existing transform curve data
        /// </summary>
        public static void InsertIntoTransformCurve(out int index, out CustomAnimation.CurveInfoPair info, out ITransformCurve mainCurve, out ITransformCurve baseCurve, string transformName, CustomAnimation animation, InsertIntoTransformCurveDelegate insertionAction, Pose restPose = null)
        {
            animation.TryGetTransformCurves(transformName, out index, out info, out mainCurve, out baseCurve);

            if (mainCurve == null)
            {
                mainCurve = TransformCurve.NewInstance;
                ((TransformCurve)mainCurve).name = transformName;
                //((TransformCurve)mainCurve).isBone <- might need to do something with this
            }
            if (baseCurve == null)
            {
                baseCurve = GetNewBaseTransformCurve(transformName, restPose);
            }

            insertionAction(mainCurve);

            if (info.infoMain.curveIndex < 0) // If main curve is new
            {
                var curveInfo = info.infoMain;
                // Add curve to animation curve arrays
                if (typeof(TransformCurve).IsAssignableFrom(mainCurve.GetType()))
                {
                    curveInfo.isLinear = false;
                    if (animation.transformCurves == null) animation.transformCurves = new TransformCurve[0];
                    curveInfo.curveIndex = animation.transformCurves.Length;
                    animation.transformCurves = (TransformCurve[])animation.transformCurves.Add(mainCurve);
                }
                else if (typeof(TransformLinearCurve).IsAssignableFrom(mainCurve.GetType()))
                {
                    curveInfo.isLinear = true;
                    if (animation.transformLinearCurves == null) animation.transformLinearCurves = new TransformLinearCurve[0];
                    curveInfo.curveIndex = animation.transformLinearCurves.Length;
                    animation.transformLinearCurves = (TransformLinearCurve[])animation.transformLinearCurves.Add(mainCurve);
                }

                info.infoMain = curveInfo;
            }
            if (baseCurve == null)
            {
                baseCurve = mainCurve;
                info.infoBase = info.infoMain;
            }
            if (info.infoBase.curveIndex < 0) // If base curve is new
            {
                var curveInfo = info.infoBase;
                // Add curve to animation curve arrays
                if (typeof(TransformCurve).IsAssignableFrom(baseCurve.GetType()))
                {
                    curveInfo.isLinear = false;
                    if (animation.transformCurves == null) animation.transformCurves = new TransformCurve[0];
                    curveInfo.curveIndex = animation.transformCurves.Length;
                    animation.transformCurves = (TransformCurve[])animation.transformCurves.Add(baseCurve);
                }
                else if (typeof(TransformLinearCurve).IsAssignableFrom(baseCurve.GetType()))
                {
                    curveInfo.isLinear = true;
                    if (animation.transformLinearCurves == null) animation.transformLinearCurves = new TransformLinearCurve[0];
                    curveInfo.curveIndex = animation.transformLinearCurves.Length;
                    animation.transformLinearCurves = (TransformLinearCurve[])animation.transformLinearCurves.Add(baseCurve);
                }

                info.infoBase = curveInfo;
            }

            if (index >= 0)
            {
                animation.transformAnimationCurves[index] = info; // Curve was already present to some capacity, so update the existing curve info
            }
            else
            {
                if (animation.transformAnimationCurves == null) animation.transformAnimationCurves = new CustomAnimation.CurveInfoPair[0];
                animation.transformAnimationCurves = (CustomAnimation.CurveInfoPair[])animation.transformAnimationCurves.Add(info);  // All curve data for this transform is new, so append the info of its existence
                index = animation.transformAnimationCurves.Length - 1;
            }
        }

        public static void GetOrCreatePropertyCurve(out IPropertyCurve mainCurve, string propertyString, CustomAnimation animation, Pose restPose = null) => GetOrCreatePropertyCurve(out _, out _, out mainCurve, out _, propertyString, animation, restPose);
        public static void GetOrCreatePropertyCurve(out int index, out CustomAnimation.CurveInfoPair info, out IPropertyCurve mainCurve, out IPropertyCurve baseCurve, string propertyString, CustomAnimation animation, Pose restPose = null) => InsertIntoPropertyCurve(out index, out info, out mainCurve, out baseCurve, propertyString, animation, EmptyInsertion, restPose);
        public static void InsertIntoPropertyCurve(string propertyString, CustomAnimation animation, InsertIntoPropertyCurveDelegate insertionAction, Pose restPose = null) => InsertIntoPropertyCurve(out _, out _, out _, out _, propertyString, animation, insertionAction, restPose);
        /// <summary>
        /// Handles insertion of element data into new or existing property curve data
        /// </summary>
        public static void InsertIntoPropertyCurve(out int index, out CustomAnimation.CurveInfoPair info, out IPropertyCurve mainCurve, out IPropertyCurve baseCurve, string propertyString, CustomAnimation animation, InsertIntoPropertyCurveDelegate innerAction, Pose restPose = null)
        {
            animation.TryGetPropertyCurves(propertyString, out index, out info, out mainCurve, out baseCurve);

            if (mainCurve == null)
            {
                mainCurve = PropertyCurve.NewInstance;
                ((PropertyCurve)mainCurve).name = propertyString;
            }
            if (baseCurve == null)
            {
                baseCurve = GetNewBasePropertyCurve(propertyString, restPose);
            }

            innerAction(mainCurve);

            if (info.infoMain.curveIndex < 0) // If main curve is new
            {
                var curveInfo = info.infoMain;
                // Add curve to animation curve arrays
                if (typeof(PropertyCurve).IsAssignableFrom(mainCurve.GetType()))
                {
                    curveInfo.isLinear = false;
                    if (animation.propertyCurves == null) animation.propertyCurves = new PropertyCurve[0];
                    curveInfo.curveIndex = animation.propertyCurves.Length;
                    animation.propertyCurves = (PropertyCurve[])animation.propertyCurves.Add(mainCurve);
                }
                else if (typeof(PropertyLinearCurve).IsAssignableFrom(mainCurve.GetType()))
                {
                    curveInfo.isLinear = true;
                    if (animation.propertyLinearCurves == null) animation.propertyLinearCurves = new PropertyLinearCurve[0];
                    curveInfo.curveIndex = animation.propertyLinearCurves.Length;
                    animation.propertyLinearCurves = (PropertyLinearCurve[])animation.propertyLinearCurves.Add(mainCurve);
                }

                info.infoMain = curveInfo;
            }
            if (baseCurve == null)
            {
                baseCurve = mainCurve;
                info.infoBase = info.infoMain;
            }
            if (info.infoBase.curveIndex < 0) // If base curve is new
            {
                var curveInfo = info.infoBase;
                // Add curve to animation curve arrays
                if (typeof(PropertyCurve).IsAssignableFrom(baseCurve.GetType()))
                {
                    curveInfo.isLinear = false;
                    if (animation.propertyCurves == null) animation.propertyCurves = new PropertyCurve[0];
                    curveInfo.curveIndex = animation.propertyCurves.Length;
                    animation.propertyCurves = (PropertyCurve[])animation.propertyCurves.Add(baseCurve);
                }
                else if (typeof(PropertyLinearCurve).IsAssignableFrom(baseCurve.GetType()))
                {
                    curveInfo.isLinear = true;
                    if (animation.propertyLinearCurves == null) animation.propertyLinearCurves = new PropertyLinearCurve[0];
                    curveInfo.curveIndex = animation.propertyLinearCurves.Length;
                    animation.propertyLinearCurves = (PropertyLinearCurve[])animation.propertyLinearCurves.Add(baseCurve);
                }

                info.infoBase = curveInfo;
            }

            if (index >= 0)
            {
                animation.propertyAnimationCurves[index] = info; // Curve was already present to some capacity, so update the existing curve info
            }
            else
            {
                if (animation.propertyAnimationCurves == null) animation.propertyAnimationCurves = new CustomAnimation.CurveInfoPair[0];
                animation.propertyAnimationCurves = (CustomAnimation.CurveInfoPair[])animation.propertyAnimationCurves.Add(info); // All curve data for this property is new, so append the info of its existence
                index = animation.propertyAnimationCurves.Length - 1;
            }
        }

        public static ITransformCurve.Frame GetPoseTransformFrame(string transformName, Pose pose)
        {
            ITransformCurve.Frame frame = null;

            if (pose != null)
            {
                frame = new ITransformCurve.Frame();

                var data = frame.data;
                float value;

                var localPosition = data.localPosition;
                if (pose.TryGetValue($"{transformName}{_localPositionProperty}{_propertyX}", out value)) localPosition.x = value;
                if (pose.TryGetValue($"{transformName}{_localPositionProperty}{_propertyY}", out value)) localPosition.y = value;
                if (pose.TryGetValue($"{transformName}{_localPositionProperty}{_propertyZ}", out value)) localPosition.z = value;
                data.localPosition = localPosition;

                var localRotation = data.localRotation.value;
                if (pose.TryGetValue($"{transformName}{_localRotationProperty}{_propertyX}", out value)) localRotation.x = value;
                if (pose.TryGetValue($"{transformName}{_localRotationProperty}{_propertyY}", out value)) localRotation.y = value;
                if (pose.TryGetValue($"{transformName}{_localRotationProperty}{_propertyZ}", out value)) localRotation.z = value;
                if (pose.TryGetValue($"{transformName}{_localRotationProperty}{_propertyW}", out value)) localRotation.w = value;
                data.localRotation = localRotation;

                var localScale = data.localPosition;
                if (pose.TryGetValue($"{transformName}{_localScaleProperty}{_propertyX}", out value)) localScale.x = value;
                if (pose.TryGetValue($"{transformName}{_localScaleProperty}{_propertyY}", out value)) localScale.y = value;
                if (pose.TryGetValue($"{transformName}{_localScaleProperty}{_propertyZ}", out value)) localScale.z = value;
                data.localScale = localScale;

                frame.data = data;
            }

            return frame;
        }
        public static IPropertyCurve.Frame GetPosePropertyFrame(string propertyName, Pose pose)
        {
            IPropertyCurve.Frame frame = null;

            if (pose != null)
            {
                frame = new IPropertyCurve.Frame();
                if (pose.TryGetValue(propertyName, out var value)) frame.value = value;
            }

            return frame;
        }

        public class Pose : SwoleObject<Pose, Pose.Serialized>, ICloneable
        {

            #region Serialization

            public override Serialized AsSerializableStruct() => this;
            public override string AsJSON(bool prettyPrint = false) => AsSerializableStruct().AsJSON(prettyPrint);
            public static Pose FromJSON(string json) => Serialized.FromJSON(json).AsOriginalType();

            [Serializable]
            public struct Serialized : ISerializableContainer<Pose, Pose.Serialized>
            {

                public List<AnimatableElement> elements;

                public Pose AsOriginalType(PackageInfo packageInfo = default) => new Pose(this, packageInfo);

                public object AsNonserializableObject(PackageInfo packageInfo = default) => AsOriginalType(packageInfo);

                public string AsJSON(bool prettyPrint = false) => swole.ToJson(this, prettyPrint);
                public static Serialized FromJSON(string json) => swole.FromJson<Serialized>(json);
            }

            public static implicit operator Serialized(Pose source)
            {
                Serialized s = new Serialized();

                if (source != null)
                {
                    if (source.elements != null) 
                    { 
                        s.elements = new List<AnimatableElement>();
                        foreach (var element in source.elements) s.elements.Add(new AnimatableElement() { id=element.Key, value=element.Value });
                    }
                }

                return s;
            }

            public Pose(Pose.Serialized serializable, PackageInfo packageInfo = default) : base(serializable)
            {
                if (serializable.elements != null)
                {
                    foreach (var element in serializable.elements) elements[element.id] = element.value;
                }
            }

            #endregion

            private Dictionary<string, float> elements = new Dictionary<string, float>();

            public bool TryGetValue(string elementId, out float value)
            {
                value = 0;
                if (elements == null) return false;
                return elements.TryGetValue(elementId, out value);
            }

            public bool TryGetValueLiberal(string elementId, out float value)
            {
                value = 0;
                if (elements == null) return false;
                elementId = elementId.AsID();
                foreach (var element in elements) if (element.Key.AsID() == elementId) 
                    { 
                        value = element.Value;
                        return true;
                    }

                return false;
            }

            private const string keyX = "x";
            private const string keyY = "y";
            private const string keyZ = "z";
            private const string keyW = "w";
            private const string keyPos = nameof(ITransformCurve.Data.localPosition);
            private const string keyRot = nameof(ITransformCurve.Data.localRotation);
            private const string keyScale = nameof(ITransformCurve.Data.localScale); 
            private const string keySeparator = ".";
            /// <summary>
            /// Get the delta element values between the two poses, using a special case for rotation elements.
            /// </summary>
            public List<AnimatableElementDelta> GetDifference(Pose to, List<AnimatableElementDelta> list = null)
            {
                if (list == null) list = new List<AnimatableElementDelta>();

                if (elements == null)
                {
                    if (to != null && to.elements != null)
                    {
                        foreach (var element in to.elements)
                        {
                            list.Add(new AnimatableElementDelta() { id = element.Key, change = element.Value, isAbsolute = true });
                        }
                    }
                }
                else
                {

                    HashSet<string> toIgnore = new HashSet<string>();
                    foreach (var element in elements)
                    {
                        if (toIgnore.Contains(element.Key)) continue;

                        string[] keys = element.Key.Split(keySeparator);
                        int elementType = 0;
                        int typeIndex = 0;
                        for(int a = 0; a < keys.Length; a++)
                        {
                            string key = keys[a].AsID();
                            if (key == keyRot.AsID())
                            {
                                elementType = 1; // Element is a local rotation value
                                typeIndex = a;
                            }
                        }

                        void DefaultAdd()
                        {
                            if (to.elements.TryGetValue(element.Key, out float otherVal))
                            {
                                list.Add(new AnimatableElementDelta() { id = element.Key, change = otherVal - element.Value, isAbsolute = false });
                            }
                        }

                        if (elementType == 0) // Default 
                        {
                            DefaultAdd();
                        }
                        else if (elementType == 1) // Local Rotation
                        {
                            string prefix = "";
                            for (int a = 0; a <= typeIndex; a++) prefix = prefix + (a == 0 ? string.Empty : keySeparator) + keys[a];

                            string keyRotX = prefix + keySeparator + keyX;
                            string keyRotY = prefix + keySeparator + keyY;
                            string keyRotZ = prefix + keySeparator + keyZ;
                            string keyRotW = prefix + keySeparator + keyW;

                            bool flagA, flagB;
                            flagA = flagB = false;

                            if (elements.TryGetValue(keyRotX, out float localRotX_A)) flagA = true;
                            if (elements.TryGetValue(keyRotY, out float localRotY_A)) flagA = true;
                            if (elements.TryGetValue(keyRotZ, out float localRotZ_A)) flagA = true;
                            if (elements.TryGetValue(keyRotW, out float localRotW_A)) flagA = true;

                            if (to.elements.TryGetValue(keyRotX, out float localRotX_B)) flagB = true;
                            if (to.elements.TryGetValue(keyRotY, out float localRotY_B)) flagB = true;
                            if (to.elements.TryGetValue(keyRotZ, out float localRotZ_B)) flagB = true;
                            if (to.elements.TryGetValue(keyRotW, out float localRotW_B)) flagB = true;

                            if (flagA && flagB)
                            {
                                Quaternion rotA = new Quaternion(localRotX_A, localRotY_A, localRotZ_A, localRotW_A);
                                Quaternion rotB = new Quaternion(localRotX_B, localRotY_B, localRotZ_B, localRotW_B);

                                //Quaternion difference = Quaternion.Inverse(rotA) * rotB;
                                Quaternion difference = Maths.ShortestRotationGlobal(rotA, rotB); 

                                list.Add(new AnimatableElementDelta() { id = keyRotX, change = difference.x, isAbsolute = false });
                                list.Add(new AnimatableElementDelta() { id = keyRotY, change = difference.y, isAbsolute = false });
                                list.Add(new AnimatableElementDelta() { id = keyRotZ, change = difference.z, isAbsolute = false });
                                list.Add(new AnimatableElementDelta() { id = keyRotW, change = difference.w, isAbsolute = false });

                                toIgnore.Add(keyRotX);
                                toIgnore.Add(keyRotY);
                                toIgnore.Add(keyRotZ);
                                toIgnore.Add(keyRotW);
                            } 
                            else
                            {
                                DefaultAdd();
                            }

                        } 
                    }
                }

                return list;
            }

            /// <summary>
            /// Get the delta element values between the two poses by calculating the difference between each element's float value.
            /// </summary>
            public List<AnimatableElementDelta> GetDifferenceRaw(Pose to, List<AnimatableElementDelta> list = null)
            {
                if (list == null) list = new List<AnimatableElementDelta>();

                if (elements == null)
                {
                    if (to != null && to.elements != null)
                    {
                        foreach (var element in to.elements)
                        {
                            list.Add(new AnimatableElementDelta() { id = element.Key, change = element.Value, isAbsolute = true });
                        }
                    }
                }
                else
                {
                    foreach (var element in elements)
                    {
                        if (to.elements.TryGetValue(element.Key, out float otherVal))
                        {
                            list.Add(new AnimatableElementDelta() { id = element.Key, change = otherVal - element.Value, isAbsolute = false });
                        }
                    }
                }

                return list;
            }

            public List<AnimatableElement> GetElements(List<AnimatableElement> list = null)
            {
                if (list == null) list = new List<AnimatableElement>();

                if (elements != null)
                {
                    foreach (var element in elements) list.Add(new AnimatableElement() { id = element.Key, value = element.Value });
                }

                return list;
            }

            public Pose ReplaceElement(AnimatableElement element)
            {
                elements[element.id] = element.value;

                return this;
            }
            public Pose AddElement(AnimatableElement element, float mix = 1)
            {
                if (!elements.TryGetValue(element.id, out float value)) value = 0;
                elements[element.id] = value + element.value * mix;

                return this;
            }

            public Pose MixElement(AnimatableElement element, float mix)
            {
                if (!elements.TryGetValue(element.id, out float value)) value = 0;
                elements[element.id] = math.lerp(value, element.value, mix);

                return this;
            }
            public Pose RemoveElement(string id)
            {
                elements.Remove(id);

                return this;
            }
            public Pose ApplyRotationElements(string prefix, Quaternion rotation, bool4 validity, bool isAdditive, float mix = 1)
            {
                string propX = prefix + _propertyX;
                string propY = prefix + _propertyY;
                string propZ = prefix + _propertyZ;
                string propW = prefix + _propertyW;

                bool noDefault = true;
                if (!elements.TryGetValue(propX, out float defaultX)) defaultX = 0; else noDefault = false;
                if (!elements.TryGetValue(propY, out float defaultY)) defaultY = 0; else noDefault = false;
                if (!elements.TryGetValue(propZ, out float defaultZ)) defaultZ = 0; else noDefault = false;
                if (!elements.TryGetValue(propW, out float defaultW)) defaultW = 0; else noDefault = false;
                if (noDefault) 
                {
                    defaultX = Quaternion.identity.x;
                    defaultY = Quaternion.identity.y;
                    defaultZ = Quaternion.identity.z;
                    defaultW = Quaternion.identity.w;
                }           
                Quaternion currentRot = new Quaternion(defaultX, defaultY, defaultZ, defaultW);

                if (isAdditive) rotation = rotation * currentRot;
                rotation.x = validity.x ? rotation.x : defaultX;
                rotation.y = validity.y ? rotation.y : defaultY;
                rotation.z = validity.z ? rotation.z : defaultZ;
                rotation.w = validity.w ? rotation.w : defaultW;

                rotation = Quaternion.SlerpUnclamped(currentRot, rotation, mix);
                 
                if (validity.x) elements[propX] = rotation.x;
                if (validity.y) elements[propY] = rotation.y;
                if (validity.z) elements[propZ] = rotation.z;
                if (validity.w) elements[propW] = rotation.w;

                return this;
            }

            public Pose ApplyDelta(AnimatableElementDelta delta, float mix = 1)
            {
                if (delta.isAbsolute)
                {
                    elements[delta.id] = mix == 1 ? delta.change : Mathf.LerpUnclamped(elements[delta.id], delta.change, mix);
                } 
                else
                {
                    if (!elements.TryGetValue(delta.id, out float value)) value = 0;
                    elements[delta.id] = value + delta.change * mix;
                }

                return this;
            }
            /// <summary>
            /// Apply delta element values to the pose with no deviations based on element type.
            /// </summary>
            public Pose ApplyDeltasRaw(ICollection<AnimatableElementDelta> deltas, float mix = 1)
            {
                foreach (var delta in deltas) ApplyDelta(delta, mix);

                return this;
            }
            /// <summary>
            /// Apply delta element values to the pose, using a special case for rotation elements.
            /// </summary>
            public Pose ApplyDeltas(ICollection<AnimatableElementDelta> deltas, float mix = 1)
            {
                bool TryFindDeltaValue(string key, out float value)
                {
                    value = 0;
                    foreach(var delta in deltas)
                    {
                        if (delta.id == key)
                        {
                            value = delta.change;
                            return true;
                        }
                    }

                    return false;
                }
                HashSet<string> toIgnore = new HashSet<string>();
                foreach (var delta in deltas)
                {
                    if (toIgnore.Contains(delta.id)) continue;

                    string[] keys = delta.id.Split(keySeparator);
                    int elementType = 0;
                    int typeIndex = 0;
                    for (int a = 0; a < keys.Length; a++)
                    {
                        string key = keys[a].AsID();
                        if (key == keyRot.AsID())
                        {
                            elementType = 1; // Element is a local rotation value
                            typeIndex = a;
                        }
                    }

                    if (elementType == 0) // Default 
                    {
                        ApplyDelta(delta, mix);
                    }
                    else if (elementType == 1) // Local Rotation
                    {
                        string prefix = "";
                        for (int a = 0; a <= typeIndex; a++) prefix = prefix + (a == 0 ? string.Empty : keySeparator) + keys[a];

                        string keyRotX = prefix + keySeparator + keyX;
                        string keyRotY = prefix + keySeparator + keyY;
                        string keyRotZ = prefix + keySeparator + keyZ;
                        string keyRotW = prefix + keySeparator + keyW;

                        bool flag = false;
                        if (elements.TryGetValue(keyRotX, out float localRotX)) flag = true;
                        if (elements.TryGetValue(keyRotY, out float localRotY)) flag = true;
                        if (elements.TryGetValue(keyRotZ, out float localRotZ)) flag = true;
                        if (elements.TryGetValue(keyRotW, out float localRotW)) flag = true;

                        Quaternion rot = Quaternion.identity;
                        if (flag)
                        {
                            rot = new Quaternion(localRotX, localRotY, localRotZ, localRotW);
                        }

                        flag = false;
                        if (TryFindDeltaValue(keyRotX, out float localRotOffsetX)) flag = true;
                        if (TryFindDeltaValue(keyRotY, out float localRotOffsetY)) flag = true;
                        if (TryFindDeltaValue(keyRotZ, out float localRotOffsetZ)) flag = true;
                        if (TryFindDeltaValue(keyRotW, out float localRotOffsetW)) flag = true;

                        Quaternion rotOffset = Quaternion.identity;
                        if (flag)
                        {
                            rotOffset = new Quaternion(localRotOffsetX, localRotOffsetY, localRotOffsetZ, localRotOffsetW);
                        }

                        rot = Quaternion.SlerpUnclamped(rot, rot * rotOffset, mix);

                        ApplyDelta(new AnimatableElementDelta() { id = keyRotX, change = rot.x, isAbsolute = true }, 1);
                        ApplyDelta(new AnimatableElementDelta() { id = keyRotY, change = rot.y, isAbsolute = true }, 1);
                        ApplyDelta(new AnimatableElementDelta() { id = keyRotZ, change = rot.z, isAbsolute = true }, 1);
                        ApplyDelta(new AnimatableElementDelta() { id = keyRotW, change = rot.w, isAbsolute = true }, 1);

                        toIgnore.Add(keyRotX);
                        toIgnore.Add(keyRotY);
                        toIgnore.Add(keyRotZ);
                        toIgnore.Add(keyRotW);
                    }
                }

                return this; 
            }

            private void ApplyElement(AnimatableElement element, bool additive, float mix)
            {
                if (additive)
                {
                    AddElement(element, mix);
                } 
                else
                {
                    MixElement(element, mix);
                }
            }
            private void ApplyTransformData(string baseId, ITransformCurve.Data data, bool3 validityPosition, bool4 validityRotation, bool3 validityScale, bool additive, float mix)
            {
                if (validityPosition.x) ApplyElement(new AnimatableElement() { id = $"{baseId}.{nameof(data.localPosition)}{_propertyX}", value = data.localPosition.x }, additive, mix);
                if (validityPosition.y) ApplyElement(new AnimatableElement() { id = $"{baseId}.{nameof(data.localPosition)}{_propertyY}", value = data.localPosition.y }, additive, mix);
                if (validityPosition.z) ApplyElement(new AnimatableElement() { id = $"{baseId}.{nameof(data.localPosition)}{_propertyZ}", value = data.localPosition.z }, additive, mix);

                /*
                if (validityRotation.x) ApplyElement(new AnimatableElement() { id = $"{baseId}.{nameof(data.localRotation)}{_propertyX}", value = data.localRotation.value.x }, additive, mix);
                if (validityRotation.y) ApplyElement(new AnimatableElement() { id = $"{baseId}.{nameof(data.localRotation)}{_propertyY}", value = data.localRotation.value.y }, additive, mix);
                if (validityRotation.z) ApplyElement(new AnimatableElement() { id = $"{baseId}.{nameof(data.localRotation)}{_propertyZ}", value = data.localRotation.value.z }, additive, mix);
                if (validityRotation.w) ApplyElement(new AnimatableElement() { id = $"{baseId}.{nameof(data.localRotation)}{_propertyW}", value = data.localRotation.value.w }, additive, mix);
                */
                
                if (math.any(validityRotation))
                {
                    ApplyRotationElements($"{baseId}.{nameof(data.localRotation)}", data.localRotation, validityRotation, additive, mix); 
                }

                if (validityScale.x) ApplyElement(new AnimatableElement() { id = $"{baseId}.{nameof(data.localScale)}{_propertyX}", value = data.localScale.x }, additive, mix);
                if (validityScale.y) ApplyElement(new AnimatableElement() { id = $"{baseId}.{nameof(data.localScale)}{_propertyY}", value = data.localScale.y }, additive, mix);
                if (validityScale.z) ApplyElement(new AnimatableElement() { id = $"{baseId}.{nameof(data.localScale)}{_propertyZ}", value = data.localScale.z }, additive, mix);
            }
            private void ApplyPropertyData(string propertyString, IPropertyCurve.Frame data, bool additive, float mix) => ApplyPropertyData(propertyString, data.value, additive, mix);
            private void ApplyPropertyData(string propertyString, float data, bool additive, float mix)
            {
                ApplyElement(new AnimatableElement() { id = propertyString, value = data }, additive, mix); 
            }
            private void ApplyAnimationInternal(CustomAnimation animation, float time, bool additive = false, float mix = 1, WrapMode preWrapMode = WrapMode.Loop, WrapMode postWrapMode = WrapMode.Loop)
            {
                if (animation == null) return;

                float length = animation.LengthInSeconds;
                //time = time - (Mathf.Floor(time / length) * length);
                float normalizedTime = length <= 0 ? 0 : (time / length);
                normalizedTime = animation.ScaleNormalizedTime(CustomAnimation.WrapNormalizedTime(normalizedTime, preWrapMode, postWrapMode));

                if (additive)
                {
                    if (animation.transformAnimationCurves != null)
                        foreach (var transformCurve in animation.transformAnimationCurves)
                        {
                            ITransformCurve mainCurve = null;
                            ITransformCurve baseCurve = null;

                            if (transformCurve.infoMain.curveIndex >= 0) mainCurve = transformCurve.infoMain.isLinear ? animation.transformLinearCurves[transformCurve.infoMain.curveIndex] : animation.transformCurves[transformCurve.infoMain.curveIndex];
                            if (transformCurve.infoBase.curveIndex >= 0) baseCurve = transformCurve.infoBase.isLinear ? animation.transformLinearCurves[transformCurve.infoBase.curveIndex] : animation.transformCurves[transformCurve.infoBase.curveIndex];

                            if (mainCurve == null) mainCurve = baseCurve;
                            if (baseCurve == null) baseCurve = mainCurve;

                            float curveLength = mainCurve.GetLengthInSeconds(animation.framesPerSecond);
                            float t = curveLength <= 0 ? 0 : normalizedTime * (length / curveLength);

                            var data = mainCurve.Evaluate(t) - baseCurve.Evaluate(t);
                            ApplyTransformData(mainCurve.TransformName, data, mainCurve.ValidityPosition, mainCurve.ValidityRotation, mainCurve.ValidityScale, true, mix);
                        }
                    if (animation.propertyAnimationCurves != null)
                        foreach (var propertyCurve in animation.propertyAnimationCurves)
                        {
                            IPropertyCurve mainCurve = null;
                            IPropertyCurve baseCurve = null;

                            if (propertyCurve.infoMain.curveIndex >= 0) mainCurve = propertyCurve.infoMain.isLinear ? animation.propertyLinearCurves[propertyCurve.infoMain.curveIndex] : animation.propertyCurves[propertyCurve.infoMain.curveIndex];
                            if (propertyCurve.infoBase.curveIndex >= 0) baseCurve = propertyCurve.infoBase.isLinear ? animation.propertyLinearCurves[propertyCurve.infoBase.curveIndex] : animation.propertyCurves[propertyCurve.infoBase.curveIndex];

                            if (mainCurve == null) mainCurve = baseCurve;
                            if (baseCurve == null) baseCurve = mainCurve;

                            float curveLength = mainCurve.GetLengthInSeconds(animation.framesPerSecond);
                            float t = curveLength <= 0 ? 0 : normalizedTime * (length / curveLength);

                            var data = mainCurve.Evaluate(t) - baseCurve.Evaluate(t);
                            ApplyPropertyData(mainCurve.PropertyString, data, true, mix);
                        }
                }
                else
                {
                    if (animation.transformAnimationCurves != null)
                        foreach (var transformCurve in animation.transformAnimationCurves)
                        {                      
                            ITransformCurve mainCurve = null;

                            if (transformCurve.infoMain.curveIndex >= 0) mainCurve = transformCurve.infoMain.isLinear ? animation.transformLinearCurves[transformCurve.infoMain.curveIndex] : animation.transformCurves[transformCurve.infoMain.curveIndex];

                            if (mainCurve == null) continue;

                            float curveLength = mainCurve.GetLengthInSeconds(animation.framesPerSecond);
                            float t = curveLength <= 0 ? 0 : normalizedTime * (length / curveLength);

                            var data = mainCurve.Evaluate(t) * mix;
                            ApplyTransformData(mainCurve.TransformName, data, mainCurve.ValidityPosition, mainCurve.ValidityRotation, mainCurve.ValidityScale, false, mix);
                        }
                    if (animation.propertyAnimationCurves != null)
                        foreach (var propertyCurve in animation.propertyAnimationCurves)
                        {
                            IPropertyCurve mainCurve = null;

                            if (propertyCurve.infoMain.curveIndex >= 0) mainCurve = propertyCurve.infoMain.isLinear ? animation.propertyLinearCurves[propertyCurve.infoMain.curveIndex] : animation.propertyCurves[propertyCurve.infoMain.curveIndex];

                            if (mainCurve == null) continue;

                            float curveLength = mainCurve.GetLengthInSeconds(animation.framesPerSecond);
                            float t = curveLength <= 0 ? 0 : normalizedTime * (length / curveLength);

                            var data = mainCurve.Evaluate(t);
                            ApplyPropertyData(mainCurve.PropertyString, data, false, mix);
                        }
                }
            }

            private void ApplyAnimationBaseInternal(CustomAnimation animation, float time, WrapMode preWrapMode = WrapMode.Loop, WrapMode postWrapMode = WrapMode.Loop)
            {
                if (animation == null) return;

                float length = animation.LengthInSeconds;
                //time = time - (Mathf.Floor(time / length) * length);
                float normalizedTime = length <= 0 ? 0 : (time / length);
                normalizedTime = animation.ScaleNormalizedTime(CustomAnimation.WrapNormalizedTime(normalizedTime, preWrapMode, postWrapMode));

                if (animation.transformAnimationCurves != null)
                    foreach (var transformCurve in animation.transformAnimationCurves)
                    {
                        ITransformCurve baseCurve = null;

                        if (transformCurve.infoBase.curveIndex >= 0) baseCurve = transformCurve.infoMain.isLinear ? animation.transformLinearCurves[transformCurve.infoBase.curveIndex] : animation.transformCurves[transformCurve.infoBase.curveIndex];

                        if (baseCurve == null) continue;

                        float curveLength = baseCurve.GetLengthInSeconds(animation.framesPerSecond);
                        float t = curveLength <= 0 ? 0 : normalizedTime * (length / curveLength);

                        ApplyTransformData(baseCurve.TransformName, baseCurve.Evaluate(t), baseCurve.ValidityPosition, baseCurve.ValidityRotation, baseCurve.ValidityScale, false, 1);
                    }
                if (animation.propertyAnimationCurves != null)
                    foreach (var propertyCurve in animation.propertyAnimationCurves)
                    {
                        IPropertyCurve baseCurve = null;

                        if (propertyCurve.infoBase.curveIndex >= 0) baseCurve = propertyCurve.infoBase.isLinear ? animation.propertyLinearCurves[propertyCurve.infoBase.curveIndex] : animation.propertyCurves[propertyCurve.infoBase.curveIndex];

                        if (baseCurve == null) continue;

                        float curveLength = baseCurve.GetLengthInSeconds(animation.framesPerSecond);
                        float t = curveLength <= 0 ? 0 : normalizedTime * (length / curveLength);

                        ApplyPropertyData(baseCurve.PropertyString, baseCurve.Evaluate(t), false, 1);
                    }
            }
            

            /// <summary>
            /// Apply an animation to the pose data.
            /// </summary>
            /// <param name="animation"></param>
            /// <param name="time"></param>
            /// <param name="additive"></param>
            /// <param name="mix"></param>
            /// <param name="startPose">If set, the pose data will be replaced with the start pose data before the animation is applied.</param>
            public Pose ApplyAnimation(CustomAnimation animation, float time, bool additive = false, float mix = 1, Pose startPose = null, WrapMode preWrapMode = WrapMode.Loop, WrapMode postWrapMode = WrapMode.Loop)
            {
                if (startPose != null)
                {
                    elements.Clear();
                    if (startPose.elements != null)
                    {
                        foreach (var pair in startPose.elements) elements[pair.Key] = pair.Value;
                    }
                }

                ApplyAnimationInternal(animation, time, additive, mix, preWrapMode, postWrapMode);

                return this;
            }
            /// <summary>
            /// Apply an animation's base data to the pose data.
            /// </summary>
            /// <param name="animation"></param>
            /// <param name="time"></param>
            /// <param name="startPose">If set, the pose data will be replaced with the start pose data before the animation base is applied.</param>
            public Pose ApplyAnimationBase(CustomAnimation animation, float time, Pose startPose = null, WrapMode preWrapMode = WrapMode.Loop, WrapMode postWrapMode = WrapMode.Loop)
            {
                if (startPose != null)
                {
                    elements.Clear();
                    if (startPose.elements != null)
                    {
                        foreach (var pair in startPose.elements) elements[pair.Key] = pair.Value;
                    }
                }

                ApplyAnimationBaseInternal(animation, time, preWrapMode, postWrapMode);

                return this;
            }

            public Pose Duplicate() => new Pose(this);
            public object Clone() => Duplicate();

            public Pose() : base(default) { }

            public Pose(Pose initialPose, CustomAnimation animation=null, float time=0, bool additive = false, float mix = 1, WrapMode preWrapMode = WrapMode.Loop, WrapMode postWrapMode = WrapMode.Loop) : base(default)
            {
                if (initialPose != null) elements = new Dictionary<string, float>(initialPose.elements);

                ApplyAnimationInternal(animation, time, additive, mix, preWrapMode, postWrapMode);
            }
            public Pose(List<AnimatableElement> initialPose, CustomAnimation animation = null, float time = 0, bool additive = false, float mix = 1, WrapMode preWrapMode = WrapMode.Loop, WrapMode postWrapMode = WrapMode.Loop) : base(default)
            {
                if (initialPose != null)
                {
                    foreach (var element in initialPose) elements[element.id] = element.value;
                }

                ApplyAnimationInternal(animation, time, additive, mix, preWrapMode, postWrapMode);
            }

            private Pose ApplyTransformHierarchy(ICollection<Transform> hierarchy)
            {
                if (hierarchy == null) return this;

                foreach(var transform in hierarchy)
                {
                    if (transform == null) continue;
                    string baseId = transform.name;

                    Vector3 localPosition = transform.localPosition;
                    Quaternion localRotation = transform.localRotation;
                    Vector3 localScale = transform.localScale;

                    ReplaceElement(new AnimatableElement() { id = $"{baseId}.{nameof(transform.localPosition)}.x", value = localPosition.x });
                    ReplaceElement(new AnimatableElement() { id = $"{baseId}.{nameof(transform.localPosition)}.y", value = localPosition.y });
                    ReplaceElement(new AnimatableElement() { id = $"{baseId}.{nameof(transform.localPosition)}.z", value = localPosition.z });

                    ReplaceElement(new AnimatableElement() { id = $"{baseId}.{nameof(transform.localRotation)}.x", value = localRotation.x });
                    ReplaceElement(new AnimatableElement() { id = $"{baseId}.{nameof(transform.localRotation)}.y", value = localRotation.y });
                    ReplaceElement(new AnimatableElement() { id = $"{baseId}.{nameof(transform.localRotation)}.z", value = localRotation.z });
                    ReplaceElement(new AnimatableElement() { id = $"{baseId}.{nameof(transform.localRotation)}.w", value = localRotation.w });

                    ReplaceElement(new AnimatableElement() { id = $"{baseId}.{nameof(transform.localScale)}.x", value = localScale.x });
                    ReplaceElement(new AnimatableElement() { id = $"{baseId}.{nameof(transform.localScale)}.y", value = localScale.y });
                    ReplaceElement(new AnimatableElement() { id = $"{baseId}.{nameof(transform.localScale)}.z", value = localScale.z }); 
                }

                return this;
            }

            public Pose(ICollection<Transform> transformHierarchy, CustomAnimation animation = null, float time = 0, bool additive = false, float mix = 1, WrapMode preWrapMode = WrapMode.Loop, WrapMode postWrapMode = WrapMode.Loop) : base(default)
            {
                ApplyTransformHierarchy(transformHierarchy);
                ApplyAnimationInternal(animation, time, additive, mix, preWrapMode, postWrapMode);
            }
            private static readonly List<Transform> tempTransforms = new List<Transform>();
            public Pose(CustomAnimator animator, CustomAnimation animation = null, float time = 0, bool additive = false, float mix = 1, WrapMode preWrapMode = WrapMode.Loop, WrapMode postWrapMode = WrapMode.Loop) : base(default)
            {
                if (animator != null)
                {
                    if (animator.avatar == null)
                    {
                        ApplyTransformHierarchy(animator.gameObject.GetComponentsInChildren<Transform>());
                    }
                    else
                    {
                        tempTransforms.Clear();
                        ApplyTransformHierarchy(animator.avatar.FindBones(animator.transform, tempTransforms)); 
                        tempTransforms.Clear();
                    }
                }
                ApplyAnimationInternal(animation, time, additive, mix, preWrapMode, postWrapMode);
            } 

            public Pose(Transform rootTransform, CustomAnimation animation = null, float time = 0, bool additive = false, float mix = 1, WrapMode preWrapMode = WrapMode.Loop, WrapMode postWrapMode = WrapMode.Loop) : base(default)
            {
                if (rootTransform != null)
                {
                    ApplyTransformHierarchy(rootTransform.gameObject.GetComponentsInChildren<Transform>());
                }

                ApplyAnimationInternal(animation, time, additive, mix, preWrapMode, postWrapMode);
            }

            /// <summary>
            /// Attempt to enforce this pose on the animator's hierarchy.
            /// </summary>
            public Pose ApplyTo(CustomAnimator animator)
            {
                if (animator == null) return this;
                Apply((string name) => animator.FindTransformInHierarchy(name, false));

                return this;
            }
            /// <summary>
            /// Attempt to enforce this pose on the transform's hierarchy.
            /// </summary>
            public Pose ApplyTo(Transform rootTransform)
            {
                if (rootTransform == null) return this;
                Apply((string name) => rootTransform.FindDeepChildLiberal(name));

                return this;
            }

            private Dictionary<string, TransformState> transformStates;
            private void Apply(FindTransformDelegate findTransform)
            {
                if (elements == null) return;

                if (transformStates == null) transformStates = new Dictionary<string, TransformState>();
                transformStates.Clear();

                foreach(var element in elements)
                {
                    string id = element.Key.ToLower();
                    int subIndex = id.IndexOf(_localPositionProperty);
                    id = id.Trim();
                    if (subIndex >= 0)
                    {
                        string transformName = element.Key.Substring(0, subIndex);
                        if (!string.IsNullOrEmpty(transformName))
                        {
                            transformStates.TryGetValue(transformName, out TransformState state);
                            Vector3 v = state.localPosition;
                            if (id.EndsWith(_propertyX))
                            {
                                v.x = element.Value;
                            } 
                            else if (id.EndsWith(_propertyY))
                            {
                                v.y = element.Value;
                            }
                            else if (id.EndsWith(_propertyZ))
                            {
                                v.z = element.Value;
                            }
                            state.localPosition = v;
                            transformStates[transformName] = state;
                        }
                        continue;
                    }
                    else
                    {

                        subIndex = id.IndexOf(_localRotationProperty);
                        if (subIndex >= 0)
                        {
                            string transformName = element.Key.Substring(0, subIndex);
                            if (!string.IsNullOrEmpty(transformName))
                            {
                                transformStates.TryGetValue(transformName, out TransformState state);
                                Quaternion v = state.localRotation;
                                if (id.EndsWith(_propertyX))
                                {
                                    v.x = element.Value;
                                }
                                else if (id.EndsWith(_propertyY))
                                {
                                    v.y = element.Value;
                                }
                                else if (id.EndsWith(_propertyZ))
                                {
                                    v.z = element.Value;
                                }
                                else if (id.EndsWith(_propertyW))
                                {
                                    v.w = element.Value;
                                }
                                state.localRotation = v;
                                transformStates[transformName] = state;
                            }
                            continue;
                        }
                        else
                        {
                            subIndex = id.IndexOf(_localScaleProperty);
                            if (subIndex >= 0)
                            {
                                string transformName = element.Key.Substring(0, subIndex);
                                if (!string.IsNullOrEmpty(transformName))
                                {
                                    transformStates.TryGetValue(transformName, out TransformState state);
                                    Vector3 v = state.localScale;
                                    if (id.EndsWith(_propertyX))
                                    {
                                        v.x = element.Value;
                                    }
                                    else if (id.EndsWith(_propertyY))
                                    {
                                        v.y = element.Value;
                                    }
                                    else if (id.EndsWith(_propertyZ))
                                    {
                                        v.z = element.Value;
                                    }
                                    state.localScale = v;
                                    transformStates[transformName] = state;
                                }
                                continue;
                            }
                        }
                    }

                    SetProperty(element.Key, element.Value, findTransform);
                }

                foreach(var transformState in transformStates)
                {
                    Transform transform = findTransform(transformState.Key);
                    if (transform == null) continue;
                     
                    var state = transformState.Value;
                    transform.localPosition = state.localPosition;
                    transform.localRotation = state.localRotation;
                    transform.localScale = state.localScale;
                }

            }

            /// <summary>
            /// Insert the pose as keyframes in the animation's curve data.
            /// </summary>
            /// <param name="animation"></param>
            /// <param name="time">The timeline position at which to insert keyframes.</param>
            /// <param name="restPose">An optional pose that serves as the additive base data for new curves.</param>
            /// <param name="originalPose">An optional pose that only allows deviations from itself to be considered for insertion.</param>
            /// <param name="useFrameIndices">Use passed in getFrameIndex method to get frame indices from time values, and use those for insertion instead.</param>
            /// <param name="minDeltaThreshold">The minimum value by which element values must differ to be considered for insertion when using an original pose</param>
            public Pose Insert(CustomAnimation animation, float time, Pose restPose = null, Pose originalPose = null, List<Transform> transformMask = null, bool useFrameIndices = false, IntFromDecimalDelegate getFrameIndex = null, bool verbose = false, float minDeltaThreshold = 0.00001f)
            {
                if (animation == null) return this;
                useFrameIndices = useFrameIndices && getFrameIndex != null;
                int frameIndex = 0;
                if (useFrameIndices)
                {
                    frameIndex = getFrameIndex((decimal)time);
                }

                void InsertElement(KeyValuePair<string, float> element)
                {
                    float diff = 0;
                    bool contained = false;
                    if (originalPose != null) // Make sure element has changed if main pose is provided
                    {
                        if (originalPose.TryGetValue(element.Key, out float mainValue))
                        {
                            contained = true;
                            diff = element.Value - mainValue;
                            if (/*mainValue == element.Value*/Mathf.Abs(diff) < minDeltaThreshold) return; // Element has not changed from main pose so do not create a keyframe for this element
                        }
                    }

                    if (verbose)
                    {
                        swole.Log($"Adding element '{element.Key}' [new:{!contained}] [val:{element.Value}] [delta:{diff}] [newCount:{elements.Count}] {(originalPose == null || originalPose.elements == null ? "" : $"[oldCount:{originalPose.elements.Count}")}]"); // Debug
                    }
                     
                    string id = element.Key.ToLower();
                    string id_untrimmed = id; // Preserve length to match up with original key 
                    id = id.Trim();
                    int subIndex = id_untrimmed.IndexOf(_localPositionProperty.ToLower());
                    if (subIndex >= 0)
                    {
                        // element is related to transform local position
                        string transformName = element.Key.Substring(0, subIndex);
                        if (!string.IsNullOrEmpty(transformName))
                        {
                            InsertIntoTransformCurve(transformName, animation, (ITransformCurve transformCurve) =>
                            {
                                if (typeof(TransformCurve).IsAssignableFrom(transformCurve.GetType()))
                                {
                                    TransformCurve curve = (TransformCurve)transformCurve;
                                    if (id.EndsWith(_propertyX.ToLower()))
                                    {
                                        if (curve.localPositionCurveX == null) curve.localPositionCurveX = new AnimationCurve();
                                        if (useFrameIndices) curve.localPositionCurveX.DeleteKeysAt(time, getFrameIndex);
                                        curve.localPositionCurveX.AddOrReplaceKey(time, element.Value, true, true, (restPose != null && restPose.TryGetValue(element.Key, out var defaultVal)) ? defaultVal : element.Value);
                                    }
                                    else if (id.EndsWith(_propertyY.ToLower()))
                                    {
                                        if (curve.localPositionCurveY == null) curve.localPositionCurveY = new AnimationCurve();
                                        if (useFrameIndices) curve.localPositionCurveY.DeleteKeysAt(time, getFrameIndex);
                                        curve.localPositionCurveY.AddOrReplaceKey(time, element.Value, true, true, (restPose != null && restPose.TryGetValue(element.Key, out var defaultVal)) ? defaultVal : element.Value);
                                    }
                                    else if (id.EndsWith(_propertyZ.ToLower()))
                                    {
                                        if (curve.localPositionCurveZ == null) curve.localPositionCurveZ = new AnimationCurve();
                                        if (useFrameIndices) curve.localPositionCurveZ.DeleteKeysAt(time, getFrameIndex);
                                        curve.localPositionCurveZ.AddOrReplaceKey(time, element.Value, true, true, (restPose != null && restPose.TryGetValue(element.Key, out var defaultVal)) ? defaultVal : element.Value);
                                    }
                                    curve.RefreshCachedLength(animation.framesPerSecond);
                                }
                                else if (typeof(TransformLinearCurve).IsAssignableFrom(transformCurve.GetType()))
                                {
                                    TransformLinearCurve curve = (TransformLinearCurve)transformCurve;
                                    ITransformCurve.Frame defaultFrame = GetPoseTransformFrame(transformName, restPose);
                                    var frame = curve.GetOrCreateKeyframe(Mathf.FloorToInt(time * animation.framesPerSecond), true, defaultFrame != null, defaultFrame);
                                    if (frame != null)
                                    {
                                        var data = frame.data;
                                        var v = data.localPosition;

                                        if (id.EndsWith(_propertyX.ToLower()))
                                        {
                                            v.x = element.Value;
                                        }
                                        else if (id.EndsWith(_propertyY.ToLower()))
                                        {
                                            v.y = element.Value;
                                        }
                                        else if (id.EndsWith(_propertyZ.ToLower()))
                                        {
                                            v.z = element.Value;
                                        }

                                        data.localPosition = v;
                                        frame.data = data;
                                    }
                                    curve.RefreshCachedLength(animation.framesPerSecond);
                                }
                            }, restPose);
                        }
                        return;
                    }
                    else
                    {

                        subIndex = id_untrimmed.IndexOf(_localRotationProperty.ToLower());
                        if (subIndex >= 0)
                        {
                            // element is related to transform local rotation
                            string transformName = element.Key.Substring(0, subIndex);
                            if (!string.IsNullOrEmpty(transformName))
                            {
                                InsertIntoTransformCurve(transformName, animation, (ITransformCurve transformCurve) =>
                                {
                                    if (typeof(TransformCurve).IsAssignableFrom(transformCurve.GetType()))
                                    {
                                        TransformCurve curve = (TransformCurve)transformCurve;
                                        if (id.EndsWith(_propertyX.ToLower()))
                                        {
                                            if (curve.localRotationCurveX == null) curve.localRotationCurveX = new AnimationCurve();
                                            if (useFrameIndices) curve.localRotationCurveX.DeleteKeysAt(time, getFrameIndex);
                                            curve.localRotationCurveX.AddOrReplaceKey(time, element.Value, true, true, (restPose != null && restPose.TryGetValue(element.Key, out var defaultVal)) ? defaultVal : element.Value);
                                        }
                                        else if (id.EndsWith(_propertyY.ToLower()))
                                        {
                                            if (curve.localRotationCurveY == null) curve.localRotationCurveY = new AnimationCurve();
                                            if (useFrameIndices) curve.localRotationCurveY.DeleteKeysAt(time, getFrameIndex);
                                            curve.localRotationCurveY.AddOrReplaceKey(time, element.Value, true, true, (restPose != null && restPose.TryGetValue(element.Key, out var defaultVal)) ? defaultVal : element.Value);
                                        }
                                        else if (id.EndsWith(_propertyZ.ToLower()))
                                        {
                                            if (curve.localRotationCurveZ == null) curve.localRotationCurveZ = new AnimationCurve();
                                            if (useFrameIndices) curve.localRotationCurveZ.DeleteKeysAt(time, getFrameIndex);
                                            curve.localRotationCurveZ.AddOrReplaceKey(time, element.Value, true, true, (restPose != null && restPose.TryGetValue(element.Key, out var defaultVal)) ? defaultVal : element.Value);
                                        }
                                        else if (id.EndsWith(_propertyW.ToLower()))
                                        {
                                            if (curve.localRotationCurveW == null) curve.localRotationCurveW = new AnimationCurve();
                                            if (useFrameIndices) curve.localRotationCurveW.DeleteKeysAt(time, getFrameIndex);
                                            curve.localRotationCurveW.AddOrReplaceKey(time, element.Value, true, true, (restPose != null && restPose.TryGetValue(element.Key, out var defaultVal)) ? defaultVal : element.Value);
                                        }
                                        curve.RefreshCachedLength(animation.framesPerSecond);
                                    }
                                    else if (typeof(TransformLinearCurve).IsAssignableFrom(transformCurve.GetType()))
                                    {
                                        TransformLinearCurve curve = (TransformLinearCurve)transformCurve;
                                        ITransformCurve.Frame defaultFrame = GetPoseTransformFrame(transformName, restPose);
                                        var frame = curve.GetOrCreateKeyframe(Mathf.FloorToInt(time * animation.framesPerSecond), true, defaultFrame != null, defaultFrame);
                                        if (frame != null)
                                        {
                                            var data = frame.data;
                                            var v = data.localRotation.value;

                                            if (id.EndsWith(_propertyX.ToLower()))
                                            {
                                                v.x = element.Value;
                                            }
                                            else if (id.EndsWith(_propertyY.ToLower()))
                                            {
                                                v.y = element.Value;
                                            }
                                            else if (id.EndsWith(_propertyZ.ToLower()))
                                            {
                                                v.z = element.Value;
                                            }
                                            else if (id.EndsWith(_propertyW.ToLower()))
                                            {
                                                v.w = element.Value;
                                            }

                                            data.localRotation = new quaternion(v.x, v.y, v.z, v.w);
                                            frame.data = data;
                                        }
                                        curve.RefreshCachedLength(animation.framesPerSecond);
                                    }
                                }, restPose);
                            }
                            return;
                        }
                        else
                        {
                            subIndex = id_untrimmed.IndexOf(_localScaleProperty.ToLower());
                            if (subIndex >= 0)
                            {
                                // element is related to transform local scale
                                string transformName = element.Key.Substring(0, subIndex);
                                if (!string.IsNullOrEmpty(transformName))
                                {
                                    InsertIntoTransformCurve(transformName, animation, (ITransformCurve transformCurve) =>
                                    {
                                        if (typeof(TransformCurve).IsAssignableFrom(transformCurve.GetType()))
                                        {
                                            TransformCurve curve = (TransformCurve)transformCurve;
                                            if (id.EndsWith(_propertyX.ToLower()))
                                            {
                                                if (curve.localScaleCurveX == null) curve.localScaleCurveX = new AnimationCurve();
                                                if (useFrameIndices) curve.localScaleCurveX.DeleteKeysAt(time, getFrameIndex);
                                                curve.localScaleCurveX.AddOrReplaceKey(time, element.Value, true, true, (restPose != null && restPose.TryGetValue(element.Key, out var defaultVal)) ? defaultVal : element.Value);
                                            }
                                            else if (id.EndsWith(_propertyY.ToLower()))
                                            {
                                                if (curve.localScaleCurveY == null) curve.localScaleCurveY = new AnimationCurve();
                                                if (useFrameIndices) curve.localScaleCurveY.DeleteKeysAt(time, getFrameIndex);
                                                curve.localScaleCurveY.AddOrReplaceKey(time, element.Value, true, true, (restPose != null && restPose.TryGetValue(element.Key, out var defaultVal)) ? defaultVal : element.Value);
                                            }
                                            else if (id.EndsWith(_propertyZ.ToLower()))
                                            {
                                                if (curve.localScaleCurveZ == null) curve.localScaleCurveZ = new AnimationCurve();
                                                if (useFrameIndices) curve.localScaleCurveZ.DeleteKeysAt(time, getFrameIndex);
                                                curve.localScaleCurveZ.AddOrReplaceKey(time, element.Value, true, true, (restPose != null && restPose.TryGetValue(element.Key, out var defaultVal)) ? defaultVal : element.Value);
                                            }
                                            curve.RefreshCachedLength(animation.framesPerSecond);
                                        }
                                        else if (typeof(TransformLinearCurve).IsAssignableFrom(transformCurve.GetType()))
                                        {
                                            TransformLinearCurve curve = (TransformLinearCurve)transformCurve;
                                            ITransformCurve.Frame defaultFrame = GetPoseTransformFrame(transformName, restPose);
                                            var frame = curve.GetOrCreateKeyframe(Mathf.FloorToInt(time * animation.framesPerSecond), true, defaultFrame != null, defaultFrame);
                                            if (frame != null)
                                            {
                                                var data = frame.data;
                                                var v = data.localScale;

                                                if (id.EndsWith(_propertyX.ToLower()))
                                                {
                                                    v.x = element.Value;
                                                }
                                                else if (id.EndsWith(_propertyY.ToLower()))
                                                {
                                                    v.y = element.Value;
                                                }
                                                else if (id.EndsWith(_propertyZ.ToLower()))
                                                {
                                                    v.z = element.Value;
                                                }

                                                data.localScale = v;
                                                frame.data = data;
                                            }
                                            curve.RefreshCachedLength(animation.framesPerSecond);
                                        }
                                    }, restPose);
                                }
                                return;
                            }
                        }
                    }

                    // element is a property
                    InsertIntoPropertyCurve(element.Key, animation, (IPropertyCurve propertyCurve) =>
                    {
                        if (typeof(PropertyCurve).IsAssignableFrom(propertyCurve.GetType()))
                        {
                            PropertyCurve curve = (PropertyCurve)propertyCurve;

                            if (curve.propertyValueCurve == null) curve.propertyValueCurve = new AnimationCurve();
                            if (useFrameIndices) curve.propertyValueCurve.DeleteKeysAt(time, getFrameIndex);
                            curve.propertyValueCurve.AddOrReplaceKey(time, element.Value, true, true, (restPose != null && restPose.TryGetValue(element.Key, out var defaultVal)) ? defaultVal : element.Value);

                            curve.RefreshCachedLength(animation.framesPerSecond);
                        }
                        else if (typeof(PropertyLinearCurve).IsAssignableFrom(propertyCurve.GetType()))
                        {
                            PropertyLinearCurve curve = (PropertyLinearCurve)propertyCurve;
                            IPropertyCurve.Frame defaultFrame = GetPosePropertyFrame(element.Key, restPose);
                            var frame = curve.GetOrCreateKeyframe(Mathf.FloorToInt(time * animation.framesPerSecond), true, defaultFrame != null, defaultFrame);
                            if (frame != null)
                            {
                                frame.value = element.Value;
                            }
                            curve.RefreshCachedLength(animation.framesPerSecond);
                        }
                    }, restPose);
                }

                if (transformMask == null)
                {
                    foreach (var element in elements)
                    {
                        InsertElement(element);
                    }
                } 
                else
                {
                    void TryInsertByID(string id)
                    {
                        if (elements.TryGetValue(id, out float val))
                        {
                            InsertElement(new KeyValuePair<string, float>(id, val));
                        } 
                        else if (elements.TryGetValue(id.AsID(), out val))
                        {
                            InsertElement(new KeyValuePair<string, float>(id, val));
                        }
                    }
                    foreach(var transform in transformMask)
                    {
                        if (transform == null) continue;

                        string transformName = transform.name;

                        TryInsertByID($"{transformName}{_localPositionProperty}{_propertyX}");
                        TryInsertByID($"{transformName}{_localPositionProperty}{_propertyY}");
                        TryInsertByID($"{transformName}{_localPositionProperty}{_propertyZ}");

                        TryInsertByID($"{transformName}{_localRotationProperty}{_propertyX}");
                        TryInsertByID($"{transformName}{_localRotationProperty}{_propertyY}");
                        TryInsertByID($"{transformName}{_localRotationProperty}{_propertyZ}");
                        TryInsertByID($"{transformName}{_localRotationProperty}{_propertyW}");

                        TryInsertByID($"{transformName}{_localScaleProperty}{_propertyX}");
                        TryInsertByID($"{transformName}{_localScaleProperty}{_propertyY}");
                        TryInsertByID($"{transformName}{_localScaleProperty}{_propertyZ}");
                    }
                }

                return this;
            }

        }

    }
}

#endif