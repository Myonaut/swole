#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Unity.Mathematics;

namespace Swole.API.Unity.Animation
{ 
    [Serializable]
    public class PropertyCurve : SwoleObject<PropertyCurve, PropertyCurve.Serialized>, IPropertyCurve
    {

        public bool HasKeyframes
        {
            get
            {
                if (propertyValueCurve != null && propertyValueCurve.length > 0) return true;

                return false;
            }
        }
        public float GetClosestKeyframeTime(float referenceTime, int framesPerSecond, bool includeReferenceTime = true, IntFromDecimalDelegate getFrameIndex = null)
        {
            float closestTime = 0;
            float minDiff = -1;
            int frameIndex = getFrameIndex == null ? 0 : getFrameIndex((decimal)referenceTime);

            void GetClosestKeyframeTimeFromCurve(AnimationCurve curve)
            {
                if (curve == null) return;

                for (int a = 0; a < curve.length; a++)
                {
                    var key = curve[a];
                    float diff = referenceTime - key.time;
                    if (diff < 0 || (diff > minDiff && minDiff >= 0) || (!includeReferenceTime && (diff == 0 || (getFrameIndex != null && getFrameIndex((decimal)key.time) == frameIndex)))) continue;
                    closestTime = key.time;
                    minDiff = diff;
                }
            }

            GetClosestKeyframeTimeFromCurve(propertyValueCurve);

            return closestTime;
        }

        public object Clone() => Duplicate();
        public PropertyCurve Duplicate()
        {

            var clone = ShallowCopy();

            if (propertyValueCurve != null) clone.propertyValueCurve = propertyValueCurve.AsSerializableStruct();

            return clone;

        }
        public PropertyCurve ShallowCopy()
        {
            PropertyCurve copy = new PropertyCurve();
            copy.name = name;
            copy.preWrapMode = preWrapMode;
            copy.postWrapMode = postWrapMode;

            copy.propertyValueCurve = propertyValueCurve;

            return copy;
        }

        #region Serialization

        public override string AsJSON(bool prettyPrint = false) => AsSerializableStruct().AsJSON(prettyPrint);

        [Serializable]
        public struct Serialized : ISerializableContainer<PropertyCurve, PropertyCurve.Serialized>
        {

            public string name;
            public string SerializedName => name;

            public int preWrapMode; 
            public int postWrapMode;

            public SerializedAnimationCurve propertyValueCurve;

            public PropertyCurve AsOriginalType(PackageInfo packageInfo = default) => new PropertyCurve(this);
            public string AsJSON(bool prettyPrint = false) => swole.Engine.ToJson(this, prettyPrint);

            public object AsNonserializableObject(PackageInfo packageInfo = default) => AsOriginalType(packageInfo);
        }

        public static implicit operator Serialized(PropertyCurve inst)
        {
            if (inst == null) return default;
            var s = new Serialized()
            {
                name = inst.name,
                preWrapMode = (int)inst.preWrapMode,
                postWrapMode = (int)inst.postWrapMode,

                propertyValueCurve = inst.propertyValueCurve

            };
            return s;
        }

        public override PropertyCurve.Serialized AsSerializableStruct() => this;

        public PropertyCurve(PropertyCurve.Serialized serializable) : base(serializable)
        {
            this.name = serializable.name;
            this.preWrapMode = (WrapMode)serializable.preWrapMode;
            this.postWrapMode = (WrapMode)serializable.postWrapMode;

            propertyValueCurve = serializable.propertyValueCurve;
        }

        #endregion

        public PropertyCurve() : base(default) { }

        public static PropertyCurve NewInstance
        {
            get
            {
                PropertyCurve instance = new PropertyCurve();

                instance.name = "new_property_curve";
                instance.preWrapMode = WrapMode.Clamp;
                instance.postWrapMode = WrapMode.Clamp;

                instance.propertyValueCurve = new EditableAnimationCurve();//new AnimationCurve();
                instance.propertyValueCurve.preWrapMode = instance.preWrapMode;
                instance.propertyValueCurve.postWrapMode = instance.postWrapMode;

                return instance;
            }
        }

        public string name;
        public override string SerializedName => name;
        public string PropertyString => name;

        public WrapMode preWrapMode;
        public WrapMode postWrapMode;

        public WrapMode PreWrapMode
        {
            get => preWrapMode;
            set => preWrapMode = value;
        }
        public WrapMode PostWrapMode
        {
            get => postWrapMode;
            set => postWrapMode = value;
        }

        public EditableAnimationCurve propertyValueCurve;

        public StateData State
        {
            get => new StateData(this);
            set => value.ApplyTo(this);
        }
        [Serializable]
        public struct StateData
        {
            public int preWrapMode;
            public int postWrapMode;

            public AnimationCurveEditor.State propertyValueCurveState;

            public StateData(PropertyCurve pc)
            {
                if (pc != null)
                {
                    preWrapMode = (int)pc.preWrapMode;
                    postWrapMode = (int)pc.postWrapMode;

                    propertyValueCurveState = pc.propertyValueCurve == null ? default : pc.propertyValueCurve.CloneState();
                }
                else
                {
                    preWrapMode = postWrapMode = 0;
                    propertyValueCurveState = default;
                }
            }

            public void ApplyTo(PropertyCurve curve)
            {
                curve.preWrapMode = (WrapMode)preWrapMode;
                curve.postWrapMode = (WrapMode)postWrapMode;

                void SetCurveState(string curvePropertyName, AnimationCurveEditor.State curveState, ref EditableAnimationCurve animCurve)
                {
                    if (animCurve == null && curveState.keyframes != null && curveState.keyframes.Length > 0) animCurve = new EditableAnimationCurve(default, null, curvePropertyName);
                    if (animCurve != null) animCurve.State = curveState;
                }

                SetCurveState(nameof(curve.propertyValueCurve), propertyValueCurveState, ref curve.propertyValueCurve);
            }
        }

        [NonSerialized]
        private float m_cachedLength = -1;
        public float CachedLengthInSeconds
        {

            get
            {

                if (m_cachedLength <= 0) RefreshCachedLength(CustomAnimation.DefaultFrameRate);

                return m_cachedLength;

            }

        }
        public void RefreshCachedLength(int framesPerSecond) => m_cachedLength = GetLengthInSeconds(framesPerSecond);

        public float Evaluate(float normalizedTime)
        {

            normalizedTime = CustomAnimation.WrapNormalizedTime(normalizedTime, preWrapMode, postWrapMode);
            normalizedTime = normalizedTime * CachedLengthInSeconds;

            return propertyValueCurve == null || propertyValueCurve.length <= 0 ? 0 : propertyValueCurve.Evaluate(normalizedTime);

        }

        public float GetLengthInSeconds(int framesPerSecond)
        {

            float length = 0;

            if (propertyValueCurve != null && propertyValueCurve.length > 0) length = math.max(length, propertyValueCurve[propertyValueCurve.length - 1].time);

            return length;

        }

        public int GetMaxFrameCountInTimeSlice(int framesPerSecond, float sampleLength = 1)
        {
            int maxFrameCount = 0;
            void GetMaxFrameCountFromCurve(AnimationCurve curve)
            {
                if (curve == null) return;

                for (int a = 0; a < curve.length; a++)
                { 
                    var key = curve[a];
                    int count = 1;
                    for (int b = a + 1; b < curve.length; b++)
                    {
                        var key2 = curve[b];
                        if (key2.time - key.time > sampleLength) break;
                        count++;
                    }
                    maxFrameCount = Mathf.Max(count, maxFrameCount);
                }
            }

            GetMaxFrameCountFromCurve(propertyValueCurve);

            return maxFrameCount;
        }

        public List<float> GetFrameTimes(int framesPerSecond, List<float> inputList = null)
        {
            if (inputList == null) inputList = new List<float>();

            if (propertyValueCurve != null)
            {
                for (int a = 0; a < propertyValueCurve.length; a++) inputList.Add(propertyValueCurve[a].time);
            }

            return inputList;
        }
        public List<CustomAnimation.FrameHeader> GetFrameTimes(int framesPerSecond, List<CustomAnimation.FrameHeader> inputList)
        {
            if (inputList == null) inputList = new List<CustomAnimation.FrameHeader>();

            if (propertyValueCurve != null)
            {
                for (int a = 0; a < propertyValueCurve.length; a++) inputList.Add(new CustomAnimation.FrameHeader() { time = propertyValueCurve[a].time });
            }

            return inputList;
        }

    }
}

#endif