using System;

namespace Swole.Animation
{
    public interface IAnimationAsset : IContent, ICloneable
    {
        public string ID { get; }
        
        public bool HasKeyframes { get; }
        public float GetClosestKeyframeTime(float referenceTime, bool includeReferenceTime = true, IntFromDecimalDelegate getFrameIndex = null);

    }

    [Serializable]
    public enum AnimationLoopMode
    {

        PlayOnce, Loop, PingPong

    }
}
