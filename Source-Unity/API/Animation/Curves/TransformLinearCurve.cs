#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Unity.Mathematics;

namespace Swole.API.Unity.Animation
{
    [Serializable]
    public class TransformLinearCurve : SwoleObject<TransformLinearCurve, TransformLinearCurve.Serialized>, ITransformCurve
    {

        public bool HasKeyframes
        {
            get
            {
                if (frames != null && frames.Length > 0) return true;

                return false;
            }
        }
        public float GetClosestKeyframeTime(float referenceTime, int framesPerSecond, bool includeReferenceTime = true, IntFromDecimalDelegate getFrameIndex = null)
        {
            float closestTime = 0;
            float minDiff = -1;
            int frameIndex = getFrameIndex == null ? 0 : getFrameIndex((decimal)referenceTime);

            void GetClosestKeyframeTimeFromCurve(ITransformCurve.Frame[] frames)
            {
                if (frames == null) return;

                for (int a = 0; a < frames.Length; a++)
                {
                    var key = frames[a];
                    float keyTime = (key.timelinePosition / (float)framesPerSecond);
                    float diff = referenceTime - keyTime;
                    if (diff < 0 || (diff > minDiff && minDiff >= 0) || (!includeReferenceTime && (diff == 0 || (getFrameIndex != null && key.timelinePosition == frameIndex)))) continue;
                    closestTime = keyTime;
                    minDiff = diff;
                }
            }

            GetClosestKeyframeTimeFromCurve(frames);

            return closestTime;
        }

        public object Clone() => Duplicate();
        public TransformLinearCurve Duplicate()
        {

            var clone = ShallowCopy();

            if (frames != null)
            {
                clone.frames = new ITransformCurve.Frame[frames.Length];
                for (int a = 0; a < frames.Length; a++)
                {
                    var frame = frames[a];
                    var cloneFrame = new ITransformCurve.Frame();

                    cloneFrame.timelinePosition = frame.timelinePosition;
                    cloneFrame.data = frame.data;
                    if (frame.interpolationCurve != null) cloneFrame.interpolationCurve = frame.interpolationCurve.AsSerializableStruct();
                    clone.frames[a] = cloneFrame;
                }
            }

            return clone;

        }
        public TransformLinearCurve ShallowCopy()
        {
            TransformLinearCurve copy = new TransformLinearCurve();
            copy.name = name;
            copy.preWrapMode = preWrapMode;
            copy.postWrapMode = postWrapMode;
            copy.isBone = isBone;

            copy.frames = frames;

            return copy;
        }

        #region Serialization

        public override string AsJSON(bool prettyPrint = false) => AsSerializableStruct().AsJSON(prettyPrint);

        [Serializable]
        public struct Serialized : ISerializableContainer<TransformLinearCurve, TransformLinearCurve.Serialized>
        {

            public string name;
            public string SerializedName => name;

            public bool isBone;

            public int preWrapMode;
            public int postWrapMode;

            public ITransformCurve.Frame.Serialized[] frames;

            public TransformLinearCurve AsOriginalType(PackageInfo packageInfo = default) => new TransformLinearCurve(this);
            public string AsJSON(bool prettyPrint = false) => swole.Engine.ToJson(this, prettyPrint);

            public object AsNonserializableObject(PackageInfo packageInfo = default) => AsOriginalType(packageInfo);
        }

        public static implicit operator Serialized(TransformLinearCurve inst)
        {
            if (inst == null) return default;
            var s = new Serialized() { name = inst.name, isBone = inst.isBone, preWrapMode = (int)inst.preWrapMode, postWrapMode = (int)inst.postWrapMode };
            if (inst.frames != null)
            {
                s.frames = new ITransformCurve.Frame.Serialized[inst.frames.Length];
                for (int a = 0; a < s.frames.Length; a++) s.frames[a] = inst.frames[a];
            }
            return s;
        }

        public override TransformLinearCurve.Serialized AsSerializableStruct() => this;

        public TransformLinearCurve(TransformLinearCurve.Serialized serializable) : base(serializable)
        {
            this.name = serializable.name;
            this.isBone = serializable.isBone;
            this.preWrapMode = (WrapMode)serializable.preWrapMode;
            this.postWrapMode = (WrapMode)serializable.postWrapMode;
            if (serializable.frames != null)
            {
                this.frames = new ITransformCurve.Frame[serializable.frames.Length];
                for (int a = 0; a < serializable.frames.Length; a++) this.frames[a] = serializable.frames[a].AsOriginalType();
            }
        }

        #endregion

        public TransformLinearCurve() : base(default) { }

        public string name;
        public override string SerializedName => name;

        public string TransformName => name;

        public bool isBone;
        public bool IsBone => isBone;

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

        public ITransformCurve.Frame[] frames;

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

            public ITransformCurve.Frame.Serialized[] frames;

            public StateData(TransformLinearCurve tlc)
            {
                if (tlc != null)
                {
                    preWrapMode = (int)tlc.preWrapMode;
                    postWrapMode = (int)tlc.postWrapMode;

                    frames = new ITransformCurve.Frame.Serialized[tlc.frames.Length];
                    for (int a = 0; a < frames.Length; a++) frames[a] = tlc.frames[a] == null ? null : tlc.frames[a].Duplicate();
                }
                else
                {
                    preWrapMode = postWrapMode = 0;
                    frames = null;
                }
            }

            public void ApplyTo(TransformLinearCurve curve)
            {
                curve.preWrapMode = (WrapMode)preWrapMode;
                curve.postWrapMode = (WrapMode)postWrapMode;

                curve.frames = frames == null ? null : new ITransformCurve.Frame[frames.Length];
                for (int a = 0; a < frames.Length; a++) curve.frames[a] = frames[a].AsOriginalType();
            }
        }

        public void AddFrame(ITransformCurve.Frame frame, bool addTimeZeroKeyframeIfEmpty = false, bool useSeparateDataForTimeZeroKey = false, ITransformCurve.Frame timeZeroFrame = default)
        {
            if (frame == null) return;
            if (frames == null) frames = new ITransformCurve.Frame[0];

            int pos = frames.Length;
            bool replace = false;
            for (int a = 0; a < frames.Length; a++)
            {
                var frame_ = frames[a];

                if (frame_.timelinePosition == frame.timelinePosition)
                {
                    pos = a;
                    replace = true;
                    break;
                }
                else if (frame_.timelinePosition > frame.timelinePosition)
                {
                    pos = a - 1;
                    break;
                }
            }
            pos = Mathf.Max(0, pos);

            if (replace)
            {
                frames[pos] = frame;
            }
            else
            {
                if (frames.Length <= 0 && addTimeZeroKeyframeIfEmpty && frame.timelinePosition != 0)
                {
                    if (useSeparateDataForTimeZeroKey && timeZeroFrame == null) timeZeroFrame = new ITransformCurve.Frame();
                    var kfzero = useSeparateDataForTimeZeroKey ? timeZeroFrame : frame.Duplicate();
                    kfzero.timelinePosition = 0;
                    frames = (ITransformCurve.Frame[])frames.Add(kfzero, 0);
                    pos++;
                }
                frames = (ITransformCurve.Frame[])frames.Add(frame, pos);
            }
        }

        public ITransformCurve.Frame GetOrCreateKeyframe(int timelinePosition, bool addTimeZeroKeyframeIfEmpty = false, bool useSeparateDataForTimeZeroKey = false, ITransformCurve.Frame timeZeroFrame = default)
        {
            List<ITransformCurve.Frame> newFrames = frames == null ? new List<ITransformCurve.Frame>() : new List<ITransformCurve.Frame>(frames);
            if (newFrames.Count <= 0 && addTimeZeroKeyframeIfEmpty && useSeparateDataForTimeZeroKey)
            {
                if (timeZeroFrame == null) timeZeroFrame = new ITransformCurve.Frame();
                timeZeroFrame.timelinePosition = 0;
                newFrames.Add(timeZeroFrame);
            }
            newFrames.Sort((x, y) => (int)Mathf.Sign(x.timelinePosition - y.timelinePosition));

            ITransformCurve.Frame frame = null;

            ITransformCurve.Frame frameA, frameB;
            frameA = frameB = null;
            frameB = null;

            for (int a = 0; a < newFrames.Count; a++)
            {
                var fr = newFrames[a];

                if (fr.timelinePosition > timelinePosition)
                {
                    if (a == 0)
                    {
                        frameA = fr;
                        break;
                    }
                    else
                    {
                        frameA = newFrames[a - 1];
                        frameB = newFrames[a];
                        break;
                    }
                }
                else if (fr.timelinePosition == timelinePosition)
                {
                    return fr;
                }
            }

            if (frameA == null && frameB == null)
            {
                if (newFrames.Count <= 0)
                {
                    frame = new ITransformCurve.Frame();
                    frame.timelinePosition = timelinePosition;
                }
                else
                {
                    frame = newFrames[newFrames.Count - 1].Duplicate();
                    frame.timelinePosition = timelinePosition;
                }
            }
            else if (frameA != null)
            {
                frame = frameA.Duplicate();
                frame.timelinePosition = timelinePosition;
            }
            else if (frameB != null)
            {
                frame = frameB.Duplicate();
                frame.timelinePosition = timelinePosition;
            }
            else
            {
                frame = frameA.Slerp(frameB, (timelinePosition - frameA.timelinePosition) / (float)(frameB.timelinePosition - frameA.timelinePosition));
                frame.timelinePosition = timelinePosition;
            }

            if (frame != null)
            {
                if (newFrames.Count <= 0 && addTimeZeroKeyframeIfEmpty && !useSeparateDataForTimeZeroKey && frame.timelinePosition != 0)
                {
                    var kfzero = frame.Duplicate();
                    kfzero.timelinePosition = 0;
                    newFrames.Add(kfzero);
                }
                newFrames.Add(frame);
            }

            newFrames.Sort((x, y) => (int)Mathf.Sign(x.timelinePosition - y.timelinePosition));
            frames = newFrames.ToArray();

            return frame;
        }

        private static readonly List<ITransformCurve.Frame> tempFrames = new List<ITransformCurve.Frame>();
        public void DeleteFramesAt(int frameIndex)
        {
            tempFrames.Clear();
            tempFrames.AddRange(frames);

            tempFrames.RemoveAll(i => i.timelinePosition == frameIndex);

            frames = tempFrames.ToArray();
        }

        public int FrameLength => frames == null || frames.Length == 0 ? 0 : frames[frames.Length - 1].timelinePosition;

        public ITransformCurve.Data Evaluate(float normalizedTime)
        {

            if (frames == null || frames.Length == 0) return default;

            normalizedTime = CustomAnimation.WrapNormalizedTime(normalizedTime, preWrapMode, postWrapMode);

            float timelinePosition = normalizedTime * FrameLength;

            int startFrameIndex = 0;
            int endFrameIndex = 0;
            for (int a = 0; a < frames.Length; a++)
            {

                var frame = frames[a];

                startFrameIndex = a - 1;
                endFrameIndex = a;

                if (frame.timelinePosition > timelinePosition) break;

            }

            startFrameIndex = math.max(0, startFrameIndex);
            var startFrame = frames[startFrameIndex];
            var endFrame = frames[endFrameIndex];

            float timeRange = endFrame.timelinePosition - startFrame.timelinePosition;

            if (timeRange <= 0) return startFrame.data;

            return startFrame.SlerpData(endFrame, math.min(1, (timelinePosition - startFrame.timelinePosition) / timeRange));

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

        public float GetLengthInSeconds(int framesPerSecond)
        {

            if (frames == null || frames.Length <= 0) return 0;

            m_cachedLength = frames[frames.Length - 1].timelinePosition / (float)framesPerSecond;
            return m_cachedLength;

        }

        public int GetMaxFrameCountInTimeSlice(int framesPerSecond, float sampleLength = 1)
        {
            if (frames == null) return 0;

            int maxFrameCount = 0;
            for (int a = 0; a < frames.Length; a++)
            {
                var frame = frames[a];
                float frameTime = frame.timelinePosition / (float)framesPerSecond;
                int count = 1;
                for (int b = a + 1; b < frames.Length; b++)
                {
                    var frame2 = frames[b];
                    float frameTime2 = frame2.timelinePosition / (float)framesPerSecond;

                    if (frameTime2 - frameTime > sampleLength) break;

                    count++;
                }
                maxFrameCount = Mathf.Max(count, maxFrameCount);
            }

            return maxFrameCount;
        }

        public bool3 ValidityPosition => frames != null && frames.Length > 0;
        public bool4 ValidityRotation => frames != null && frames.Length > 0;
        public bool3 ValidityScale => frames != null && frames.Length > 0;

        public List<float> GetFrameTimes(int framesPerSecond, List<float> inputList = null)
        {
            if (inputList == null) inputList = new List<float>();

            if (frames != null)
            {
                for (int a = 0; a < frames.Length; a++) inputList.Add(frames[a].timelinePosition / (float)framesPerSecond);
            }

            return inputList;
        }
        public List<CustomAnimation.FrameHeader> GetFrameTimes(int framesPerSecond, List<CustomAnimation.FrameHeader> inputList)
        {
            if (inputList == null) inputList = new List<CustomAnimation.FrameHeader>();

            if (frames != null)
            {
                for (int a = 0; a < frames.Length; a++) inputList.Add(new CustomAnimation.FrameHeader() { time = frames[a].timelinePosition / (float)framesPerSecond });
            }

            return inputList;
        }

        public void ClearKeys()
        {
            ClearTransformKeys();
        }
        public void ClearTransformKeys()
        {
            if (frames != null) frames = new ITransformCurve.Frame[0];
        }

    }

}

#endif