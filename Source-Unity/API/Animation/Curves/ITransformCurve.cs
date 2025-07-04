#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using UnityEngine;

using Unity.Mathematics;

namespace Swole.API.Unity.Animation
{
    public interface ITransformCurve : ICloneable
    {

        [Serializable, StructLayout(LayoutKind.Sequential)]
        public struct Data
        {

            public float3 localPosition;
            public float3 localScale;

            public quaternion localRotation;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Data Slerp(Data other, float interp)
            {

                Data inbetween = default;

                inbetween.localPosition = math.lerp(localPosition, other.localPosition, interp);
                inbetween.localScale = math.lerp(localScale, other.localScale, interp);
                inbetween.localRotation = math.slerp(localRotation, other.localRotation, interp);

                return inbetween;

            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Data Lerp(Data other, float interp)
            {

                Data inbetween = default;

                inbetween.localPosition = math.lerp(localPosition, other.localPosition, interp);
                inbetween.localScale = math.lerp(localScale, other.localScale, interp);
                inbetween.localRotation = math.lerp(localRotation.value, other.localRotation.value, interp);

                return inbetween;

            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Data operator +(Data dataA, Data dataB)
            {

                dataA.localPosition = dataA.localPosition + dataB.localPosition;
                dataA.localRotation = math.mul(dataA.localRotation, dataB.localRotation);
                dataA.localScale = dataA.localScale + dataB.localScale;

                return dataA;

            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Data operator -(Data dataA, Data dataB)
            {
                dataA.localPosition = dataA.localPosition - dataB.localPosition;
                dataA.localRotation = math.mul(math.inverse(dataB.localRotation), dataA.localRotation);
                //dataA.localRotation = Maths.ShortestRotationGlobal(dataB.localRotation, dataA.localRotation);   
                //dataA.localRotation = Quaternion.Euler(((Quaternion)math.mul(math.inverse(dataB.localRotation), dataA.localRotation)).eulerAngles);
                //var rot = (Quaternion)math.mul(math.inverse(dataB.localRotation), dataA.localRotation);
                //dataA.localRotation = Quaternion.Euler(new Vector3(Maths.NormalizeDegrees(rot.eulerAngles.x), Maths.NormalizeDegrees(rot.eulerAngles.y), Maths.NormalizeDegrees(rot.eulerAngles.z)));
                dataA.localScale = dataA.localScale - dataB.localScale;   

                return dataA;

            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Data Subtract(Data dataA, Data dataB, quaternion baseRotation)
            {
                dataA.localPosition = dataA.localPosition - dataB.localPosition;
                var rotA = Maths.ShortestRotationGlobal(baseRotation, dataA.localRotation);
                var rotB = Maths.ShortestRotationGlobal(baseRotation, dataB.localRotation);
                /*var rotA = Maths.ShortestRotationGlobal(baseRotation, dataA.localRotation);
                var rotB = Maths.ShortestRotationGlobal(baseRotation, dataB.localRotation);
                var rotC = Maths.ShortestRotationGlobal(rotA, rotB);
                rotB = math.mul(baseRotation, math.mul(rotA, rotC));
                rotA = math.mul(baseRotation, rotA);*/
                /*var rotA = math.mul(baseRotation, Quaternion.Euler(((Quaternion)math.mul(math.inverse(baseRotation), dataA.localRotation)).eulerAngles));
                var rotB = math.mul(baseRotation, Quaternion.Euler(((Quaternion)math.mul(math.inverse(baseRotation), dataB.localRotation)).eulerAngles));*/
                /*var rotA = Rebuild(math.mul(math.inverse(baseRotation), dataA.localRotation));
                var rotB = Rebuild(math.mul(math.inverse(baseRotation), dataB.localRotation));
                var rotC = Rebuild(math.mul(math.inverse(rotA), rotB));
                rotB = math.mul(baseRotation, math.mul(rotA, rotC));
                rotA = math.mul(baseRotation, rotA);*/
                //dataA.localRotation = math.mul(math.inverse(rotB), rotA);
                dataA.localRotation = Maths.ShortestRotationGlobal(rotB, rotA);   
                dataA.localScale = dataA.localScale - dataB.localScale;

                return dataA;

            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Data operator *(Data data, float scalar)
            {

                data.localPosition = data.localPosition * scalar;
                data.localRotation = math.slerp(quaternion.identity, data.localRotation, scalar);
                data.localScale = data.localScale * scalar;

                return data;

            }

        }

        [Serializable]
        public class Frame : SwoleObject<Frame, Frame.Serialized>, ICloneable
        {

            #region Serialization

            public override string SerializedName => nameof(Frame);

            public override string AsJSON(bool prettyPrint = false) => AsSerializableStruct().AsJSON(prettyPrint);

            [Serializable]
            public struct Serialized : ISerializableContainer<Frame, Frame.Serialized>
            {

                public string SerializedName => nameof(Frame);

                public int timelinePosition;
                public SerializedAnimationCurve interpolationCurve;
                public Data data;

                public Frame AsOriginalType(PackageInfo packageInfo = default) => new Frame(this);
                public string AsJSON(bool prettyPrint = false) => swole.Engine.ToJson(this, prettyPrint);

                public object AsNonserializableObject(PackageInfo packageInfo = default) => AsOriginalType(packageInfo);
            }

            public static implicit operator Serialized(Frame frame)
            {
                if (frame == null) return default;
                return new Serialized() { interpolationCurve = frame.interpolationCurve, timelinePosition = frame.timelinePosition, data = frame.data };
            }

            public override Frame.Serialized AsSerializableStruct() => this;

            public Frame() : base(default) {}
            public Frame(Frame.Serialized serializable) : base(serializable)
            {
                this.timelinePosition = serializable.timelinePosition;
                this.interpolationCurve = serializable.interpolationCurve;
                this.data = serializable.data;
            }

            #endregion

            public int timelinePosition;
            public float GetTime(int framesPerSecond) => timelinePosition / (float)framesPerSecond;

            public EditableAnimationCurve interpolationCurve;

            public Data data;

            /// <summary>
            /// Used to store a float during animation editing.
            /// </summary>
            [NonSerialized]
            public float tempFloat;

            public Frame Slerp(Frame other, float t)
            {
                Frame inbetween = new Frame();
                float interp = interpolationCurve == null ? t : interpolationCurve.Evaluate(t);
                inbetween.data = data.Slerp(other.data, interp); 
                return inbetween;
            }
            public Data SlerpData(Frame other, float t)
            {
                float interp = interpolationCurve == null ? t : interpolationCurve.Evaluate(t);
                return data.Slerp(other.data, interp);
            }
            public Frame Lerp(Frame other, float t)
            {
                Frame inbetween = new Frame();
                float interp = interpolationCurve == null ? t : interpolationCurve.Evaluate(t);
                inbetween.data = data.Lerp(other.data, interp);
                return inbetween;
            }
            public Data LerpData(Frame other, float t)
            {
                float interp = interpolationCurve == null ? t : interpolationCurve.Evaluate(t);
                return data.Lerp(other.data, interp);
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
                clone.data = data;

                return clone;
            }
            public object Clone() => Duplicate();
        }

        string TransformName
        {

            get;

        }

        Data Evaluate(float normalizedTime);

        float GetLengthInSeconds(int framesPerSecond);

        float CachedLengthInSeconds { get; }

        void RefreshCachedLength(int framesPerSecond);

        public int GetMaxFrameCountInTimeSlice(int framesPerSecond, float sampleLength = 1);

        bool IsBone { get; }

        public WrapMode PreWrapMode { get; set; }
        public WrapMode PostWrapMode { get; set; }

        public bool3 ValidityPosition { get; }
        public bool4 ValidityRotation { get; }
        public bool3 ValidityScale { get; }

        public bool HasKeyframes { get; }
        public float GetClosestKeyframeTime(float referenceTime, int framesPerSecond, bool includeReferenceTime = true, IntFromDecimalDelegate getFrameIndex = null);

        public List<float> GetFrameTimes(int framesPerSecond, List<float> inputList = null);
        public List<CustomAnimation.FrameHeader> GetFrameTimes(int framesPerSecond, List<CustomAnimation.FrameHeader> inputList);

        public void ClearKeys();
        public void ClearTransformKeys();

    }
}

#endif