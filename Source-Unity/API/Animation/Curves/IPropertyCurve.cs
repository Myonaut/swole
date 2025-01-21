#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Unity.Mathematics;

namespace Swole.API.Unity.Animation
{
    public interface IPropertyCurve : ICloneable
    {

        [Serializable]
        public class Frame : SwoleObject<Frame, Frame.Serialized>
        {

            public override string SerializedName => nameof(Frame);

            #region Serialization

            public override string AsJSON(bool prettyPrint = false) => AsSerializableStruct().AsJSON(prettyPrint);

            [Serializable]
            public struct Serialized : ISerializableContainer<Frame, Frame.Serialized>
            {

                public string SerializedName => nameof(Frame);

                public int timelinePosition;
                public SerializedAnimationCurve interpolationCurve;
                public float value;

                public Frame AsOriginalType(PackageInfo packageInfo = default) => new Frame(this);
                public string AsJSON(bool prettyPrint = false) => swole.Engine.ToJson(this, prettyPrint);

                public object AsNonserializableObject(PackageInfo packageInfo = default) => AsOriginalType(packageInfo);
            }

            public static implicit operator Serialized(Frame frame)
            {
                if (frame == null) return default;
                return new Serialized() { interpolationCurve = frame.interpolationCurve, timelinePosition = frame.timelinePosition, value = frame.value };
            }

            public override Frame.Serialized AsSerializableStruct() => this;

            public Frame() : base(default) { }
            public Frame(Frame.Serialized serializable) : base(serializable)
            {
                this.timelinePosition = serializable.timelinePosition;
                this.interpolationCurve = serializable.interpolationCurve;
                this.value = serializable.value;
            }

            #endregion

            public int timelinePosition;

            public EditableAnimationCurve interpolationCurve;

            public float value;

            public Frame Lerp(Frame other, float t)
            {

                Frame inbetween = new Frame();

                float interp = interpolationCurve == null ? t : interpolationCurve.Evaluate(t);

                inbetween.value = math.lerp(value, other.value, interp);

                return inbetween;

            }

            public float LerpData(Frame other, float t)
            {

                float interp = interpolationCurve == null ? t : interpolationCurve.Evaluate(t);

                return math.lerp(value, other.value, interp);

            }

            public Frame Duplicate()
            {
                var clone = new Frame();

                clone.timelinePosition = timelinePosition;
                if (interpolationCurve != null)
                {
                    clone.interpolationCurve = interpolationCurve.Duplicate();//new AnimationCurve(interpolationCurve.keys);
                    clone.interpolationCurve.preWrapMode = interpolationCurve.preWrapMode;
                    clone.interpolationCurve.postWrapMode = interpolationCurve.postWrapMode;
                }
                clone.value = value;

                return clone;
            }
            public object Clone() => Duplicate();

            public static Frame operator +(Frame frameA, Frame frameB)
            {

                frameA.value = frameA.value + frameB.value;

                return frameA;

            }

            public static Frame operator -(Frame frameA, Frame frameB)
            {

                frameA.value = frameA.value - frameB.value;

                return frameA;

            }

            public static Frame operator *(Frame frameA, Frame frameB)
            {

                frameA.value = frameA.value * frameB.value;

                return frameA;

            }

            public static Frame operator +(Frame frameA, float scalar)
            {

                frameA.value = frameA.value + scalar;

                return frameA;

            }

            public static Frame operator -(Frame frameA, float scalar)
            {

                frameA.value = frameA.value - scalar;

                return frameA;

            }

            public static Frame operator *(Frame frameA, float scalar)
            {

                frameA.value = frameA.value * scalar;

                return frameA;

            }

        }

        string PropertyString { get; }

        public WrapMode PreWrapMode { get; set; }
        public WrapMode PostWrapMode { get; set; }

        float Evaluate(float normalizedTime);

        float GetLengthInSeconds(int framesPerSecond);

        float CachedLengthInSeconds { get; }

        void RefreshCachedLength(int framesPerSecond);

        public int GetMaxFrameCountInTimeSlice(int framesPerSecond, float sampleLength = 1);

        public bool HasKeyframes { get; }
        public float GetClosestKeyframeTime(float referenceTime, int framesPerSecond, bool includeReferenceTime = true, IntFromDecimalDelegate getFrameIndex = null);

        public List<float> GetFrameTimes(int framesPerSecond, List<float> inputList = null);
        public List<CustomAnimation.FrameHeader> GetFrameTimes(int framesPerSecond, List<CustomAnimation.FrameHeader> inputList);

    }
}

#endif