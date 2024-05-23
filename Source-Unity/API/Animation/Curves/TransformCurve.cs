#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Unity.Mathematics;

namespace Swole.API.Unity.Animation
{
    [Serializable]
    public class TransformCurve : SwoleObject<TransformCurve, TransformCurve.Serialized>, ITransformCurve, ICloneable
    {

        public bool HasKeyframes 
        {         
            get
            {
                if (localPositionCurveX != null && localPositionCurveX.length > 0) return true;
                if (localPositionCurveY != null && localPositionCurveY.length > 0) return true;
                if (localPositionCurveZ != null && localPositionCurveZ.length > 0) return true;

                if (localRotationCurveX != null && localRotationCurveX.length > 0) return true;
                if (localRotationCurveY != null && localRotationCurveY.length > 0) return true;
                if (localRotationCurveZ != null && localRotationCurveZ.length > 0) return true;
                if (localRotationCurveW != null && localRotationCurveW.length > 0) return true;

                if (localScaleCurveX != null && localScaleCurveX.length > 0) return true;
                if (localScaleCurveY != null && localScaleCurveY.length > 0) return true;
                if (localScaleCurveZ != null && localScaleCurveZ.length > 0) return true;

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

                for(int a = 0; a < curve.length; a++)
                {
                    var key = curve[a];
                    float diff = referenceTime - key.time;
                    if (diff < 0 || (diff > minDiff && minDiff >= 0) || (!includeReferenceTime && (diff == 0 || (getFrameIndex != null && getFrameIndex((decimal)key.time) == frameIndex)))) continue; 
                    closestTime = key.time;
                    minDiff = diff;
                }
            }

            GetClosestKeyframeTimeFromCurve(localPositionCurveX);
            GetClosestKeyframeTimeFromCurve(localPositionCurveY);
            GetClosestKeyframeTimeFromCurve(localPositionCurveZ);

            GetClosestKeyframeTimeFromCurve(localRotationCurveX);
            GetClosestKeyframeTimeFromCurve(localRotationCurveY);
            GetClosestKeyframeTimeFromCurve(localRotationCurveZ);
            GetClosestKeyframeTimeFromCurve(localRotationCurveW);

            GetClosestKeyframeTimeFromCurve(localScaleCurveX);
            GetClosestKeyframeTimeFromCurve(localScaleCurveY);
            GetClosestKeyframeTimeFromCurve(localScaleCurveZ);

            return closestTime;
        }

        public object Clone() => Duplicate();
        public TransformCurve Duplicate()
        {

            var clone = ShallowCopy();

            if (localPositionTimeCurve != null) clone.localPositionTimeCurve = localPositionTimeCurve.AsSerializableStruct();
            if (localPositionCurveX != null) clone.localPositionCurveX = localPositionCurveX.AsSerializableStruct();
            if (localPositionCurveY != null) clone.localPositionCurveY = localPositionCurveY.AsSerializableStruct();
            if (localPositionCurveZ != null) clone.localPositionCurveZ = localPositionCurveZ.AsSerializableStruct();

            if (localRotationTimeCurve != null) clone.localRotationTimeCurve = localRotationTimeCurve.AsSerializableStruct();
            if (localRotationCurveX != null) clone.localRotationCurveX = localRotationCurveX.AsSerializableStruct();
            if (localRotationCurveY != null) clone.localRotationCurveY = localRotationCurveY.AsSerializableStruct();
            if (localRotationCurveZ != null) clone.localRotationCurveZ = localRotationCurveZ.AsSerializableStruct();
            if (localRotationCurveW != null) clone.localRotationCurveW = localRotationCurveW.AsSerializableStruct();

            if (localScaleTimeCurve != null) clone.localScaleTimeCurve = localScaleTimeCurve.AsSerializableStruct();
            if (localScaleCurveX != null) clone.localScaleCurveX = localScaleCurveX.AsSerializableStruct();
            if (localScaleCurveY != null) clone.localScaleCurveY = localScaleCurveY.AsSerializableStruct();
            if (localScaleCurveZ != null) clone.localScaleCurveZ = localScaleCurveZ.AsSerializableStruct();

            return clone; 

        }
        public TransformCurve ShallowCopy()
        {
            TransformCurve copy = new TransformCurve();
            copy.name = name;
            copy.preWrapMode = preWrapMode;
            copy.postWrapMode = postWrapMode;
            copy.isBone = isBone;

            copy.localPositionTimeCurve = localPositionTimeCurve;
            copy.localPositionCurveX = localPositionCurveX;
            copy.localPositionCurveY = localPositionCurveY;
            copy.localPositionCurveZ = localPositionCurveZ;

            copy.localRotationTimeCurve = localRotationTimeCurve;
            copy.localRotationCurveX = localRotationCurveX;
            copy.localRotationCurveY = localRotationCurveY;
            copy.localRotationCurveZ = localRotationCurveZ;
            copy.localRotationCurveW = localRotationCurveW;

            copy.localScaleTimeCurve = localScaleTimeCurve;
            copy.localScaleCurveX = localScaleCurveX;
            copy.localScaleCurveY = localScaleCurveY;
            copy.localScaleCurveZ = localScaleCurveZ;

            return copy;
        }

        #region Serialization

        public override string AsJSON(bool prettyPrint = false) => AsSerializableStruct().AsJSON(prettyPrint);

        [Serializable]
        public struct Serialized : ISerializableContainer<TransformCurve, TransformCurve.Serialized>
        {

            public string name;

            public bool isBone;

            public int preWrapMode;
            public int postWrapMode;

            public SerializedAnimationCurve localPositionTimeCurve;
            public SerializedAnimationCurve localPositionCurveX;
            public SerializedAnimationCurve localPositionCurveY;
            public SerializedAnimationCurve localPositionCurveZ;

            public SerializedAnimationCurve localRotationTimeCurve;
            public SerializedAnimationCurve localRotationCurveX;
            public SerializedAnimationCurve localRotationCurveY;
            public SerializedAnimationCurve localRotationCurveZ;
            public SerializedAnimationCurve localRotationCurveW;

            public SerializedAnimationCurve localScaleTimeCurve;
            public SerializedAnimationCurve localScaleCurveX;
            public SerializedAnimationCurve localScaleCurveY;
            public SerializedAnimationCurve localScaleCurveZ;

            public TransformCurve AsOriginalType(PackageInfo packageInfo = default) => new TransformCurve(this);
            public string AsJSON(bool prettyPrint = false) => swole.Engine.ToJson(this, prettyPrint);

            public object AsNonserializableObject(PackageInfo packageInfo = default) => AsOriginalType(packageInfo);
        }

        public static implicit operator Serialized(TransformCurve inst)
        {
            if (inst == null) return default;
            var s = new Serialized()
            {
                name = inst.name,
                isBone = inst.isBone,
                preWrapMode = (int)inst.preWrapMode,
                postWrapMode = (int)inst.postWrapMode,

                localPositionTimeCurve = inst.localPositionTimeCurve,
                localPositionCurveX = inst.localPositionCurveX,
                localPositionCurveY = inst.localPositionCurveY,
                localPositionCurveZ = inst.localPositionCurveZ,

                localRotationTimeCurve = inst.localRotationTimeCurve,
                localRotationCurveX = inst.localRotationCurveX,
                localRotationCurveY = inst.localRotationCurveY,
                localRotationCurveZ = inst.localRotationCurveZ,
                localRotationCurveW = inst.localRotationCurveW,

                localScaleTimeCurve = inst.localScaleTimeCurve,
                localScaleCurveX = inst.localScaleCurveX,
                localScaleCurveY = inst.localScaleCurveY,
                localScaleCurveZ = inst.localScaleCurveZ,

            };
            return s;
        }

        public override TransformCurve.Serialized AsSerializableStruct() => this;

        public TransformCurve(TransformCurve.Serialized serializable) : base(serializable)
        {
            this.name = serializable.name;
            this.isBone = serializable.isBone;
            this.preWrapMode = (WrapMode)serializable.preWrapMode;
            this.postWrapMode = (WrapMode)serializable.postWrapMode;

            localPositionTimeCurve = serializable.localPositionTimeCurve;
            localPositionCurveX = serializable.localPositionCurveX;
            localPositionCurveY = serializable.localPositionCurveY;
            localPositionCurveZ = serializable.localPositionCurveZ;

            localRotationTimeCurve = serializable.localRotationTimeCurve;
            localRotationCurveX = serializable.localRotationCurveX;
            localRotationCurveY = serializable.localRotationCurveY;
            localRotationCurveZ = serializable.localRotationCurveZ;
            localRotationCurveW = serializable.localRotationCurveW;

            localScaleTimeCurve = serializable.localScaleTimeCurve;
            localScaleCurveX = serializable.localScaleCurveX;
            localScaleCurveY = serializable.localScaleCurveY;
            localScaleCurveZ = serializable.localScaleCurveZ; 
        }

        #endregion

        public TransformCurve() : base(default) { }

        public static TransformCurve NewInstance
        {
            get
            {
                TransformCurve instance = new TransformCurve();

                instance.name = "new_transform_curve";
                instance.preWrapMode = WrapMode.Clamp;
                instance.postWrapMode = WrapMode.Clamp;

                instance.localPositionCurveX = new AnimationCurve();
                instance.localPositionCurveX.preWrapMode = instance.preWrapMode;
                instance.localPositionCurveX.postWrapMode = instance.postWrapMode;
                instance.localPositionCurveY = new AnimationCurve();
                instance.localPositionCurveY.preWrapMode = instance.preWrapMode;
                instance.localPositionCurveY.postWrapMode = instance.postWrapMode;
                instance.localPositionCurveZ = new AnimationCurve();
                instance.localPositionCurveZ.preWrapMode = instance.preWrapMode;
                instance.localPositionCurveZ.postWrapMode = instance.postWrapMode;

                instance.localRotationCurveX = new AnimationCurve();
                instance.localRotationCurveX.preWrapMode = instance.preWrapMode;
                instance.localRotationCurveX.postWrapMode = instance.postWrapMode;
                instance.localRotationCurveY = new AnimationCurve();
                instance.localRotationCurveY.preWrapMode = instance.preWrapMode;
                instance.localRotationCurveY.postWrapMode = instance.postWrapMode;
                instance.localRotationCurveZ = new AnimationCurve();
                instance.localRotationCurveZ.preWrapMode = instance.preWrapMode;
                instance.localRotationCurveZ.postWrapMode = instance.postWrapMode;
                instance.localRotationCurveW = new AnimationCurve();
                instance.localRotationCurveW.preWrapMode = instance.preWrapMode;
                instance.localRotationCurveW.postWrapMode = instance.postWrapMode;

                instance.localScaleCurveX = new AnimationCurve();
                instance.localScaleCurveX.preWrapMode = instance.preWrapMode;
                instance.localScaleCurveX.postWrapMode = instance.postWrapMode;
                instance.localScaleCurveY = new AnimationCurve();
                instance.localScaleCurveY.preWrapMode = instance.preWrapMode;
                instance.localScaleCurveY.postWrapMode = instance.postWrapMode;
                instance.localScaleCurveZ = new AnimationCurve();
                instance.localScaleCurveZ.preWrapMode = instance.preWrapMode;
                instance.localScaleCurveZ.postWrapMode = instance.postWrapMode;

                return instance;
            }
        }

        public string name;

        public string TransformName => name;

        public bool isBone;
        public bool IsBone => isBone;

        public WrapMode preWrapMode;
        public WrapMode postWrapMode;

        public AnimationCurve localPositionTimeCurve;
        public AnimationCurve localPositionCurveX;
        public AnimationCurve localPositionCurveY;
        public AnimationCurve localPositionCurveZ;

        public AnimationCurve localRotationTimeCurve;
        public AnimationCurve localRotationCurveX;
        public AnimationCurve localRotationCurveY;
        public AnimationCurve localRotationCurveZ;
        public AnimationCurve localRotationCurveW;

        public AnimationCurve localScaleTimeCurve;
        public AnimationCurve localScaleCurveX;
        public AnimationCurve localScaleCurveY;
        public AnimationCurve localScaleCurveZ;

        public static bool Validate(AnimationCurve curve) => curve != null && curve.length > 0; // length is number of keys not time length

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

        public ITransformCurve.Data Evaluate(float normalizedTime)
        {

            normalizedTime = CustomAnimation.WrapNormalizedTime(normalizedTime, preWrapMode, postWrapMode);

            float timePos = CustomAnimation.ScaleNormalizedTime(normalizedTime, localPositionTimeCurve) * CachedLengthInSeconds;
            float timeRot = CustomAnimation.ScaleNormalizedTime(normalizedTime, localRotationTimeCurve) * CachedLengthInSeconds;
            float timeScale = CustomAnimation.ScaleNormalizedTime(normalizedTime, localScaleTimeCurve) * CachedLengthInSeconds;

            ITransformCurve.Data data = default;

            data.localPosition = new float3(!Validate(localPositionCurveX) ? 0 : localPositionCurveX.Evaluate(timePos), !Validate(localPositionCurveY) ? 0 : localPositionCurveY.Evaluate(timePos), !Validate(localPositionCurveZ) ? 0 : localPositionCurveZ.Evaluate(timePos));
            data.localRotation = new quaternion(!Validate(localRotationCurveX) ? 0 : localRotationCurveX.Evaluate(timeRot), !Validate(localRotationCurveY) ? 0 : localRotationCurveY.Evaluate(timeRot), !Validate(localRotationCurveZ) ? 0 : localRotationCurveZ.Evaluate(timeRot), !Validate(localRotationCurveW) ? 0 : localRotationCurveW.Evaluate(timeRot));
            data.localScale = new float3(!Validate(localScaleCurveX) ? 1 : localScaleCurveX.Evaluate(timeScale), !Validate(localScaleCurveY) ? 1 : localScaleCurveY.Evaluate(timeScale), !Validate(localScaleCurveZ) ? 1 : localScaleCurveZ.Evaluate(timeScale));

            return data;

        }

        public float GetLengthInSeconds(int framesPerSecond)
        {

            float length = 0;

            void CheckLength(AnimationCurve curve)
            {

                if (curve != null && curve.length > 0) length = math.max(length, curve[curve.length - 1].time);

            }

            CheckLength(localPositionCurveX);
            CheckLength(localPositionCurveY);
            CheckLength(localPositionCurveZ);

            CheckLength(localRotationCurveX);
            CheckLength(localRotationCurveY);
            CheckLength(localRotationCurveZ);
            CheckLength(localRotationCurveW);

            CheckLength(localScaleCurveX);
            CheckLength(localScaleCurveY);
            CheckLength(localScaleCurveZ);

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

            GetMaxFrameCountFromCurve(localPositionCurveX);
            GetMaxFrameCountFromCurve(localPositionCurveY);
            GetMaxFrameCountFromCurve(localPositionCurveZ);

            GetMaxFrameCountFromCurve(localRotationCurveX);
            GetMaxFrameCountFromCurve(localRotationCurveY);
            GetMaxFrameCountFromCurve(localRotationCurveZ);
            GetMaxFrameCountFromCurve(localRotationCurveW);

            GetMaxFrameCountFromCurve(localScaleCurveX);
            GetMaxFrameCountFromCurve(localScaleCurveY);
            GetMaxFrameCountFromCurve(localScaleCurveZ);

            return maxFrameCount;
        }

        public bool3 ValidityPosition => new bool3(Validate(localPositionCurveX), Validate(localPositionCurveY), Validate(localPositionCurveZ));
        public bool4 ValidityRotation => new bool4(Validate(localRotationCurveX), Validate(localRotationCurveY), Validate(localRotationCurveZ), Validate(localRotationCurveW));
        public bool3 ValidityScale => new bool3(Validate(localScaleCurveX), Validate(localScaleCurveY), Validate(localScaleCurveZ));

    }
}

#endif