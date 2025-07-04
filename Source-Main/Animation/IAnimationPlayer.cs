using System;

using Swole.Script;

namespace Swole.Animation
{
    public interface IAnimationPlayer : IDisposable, IRuntimeEventHandler
    {

        #region Animation Events
        public IRuntimeEnvironment EventRuntimeEnvironment { get; set; }
        public SwoleLogger EventLogger { get; set; }
        public void CallAnimationEvents(float startTime, float endTime);
        public void CallAnimationEvents(float startTime, float endTime, float currentSpeed);
        public void CallAnimationEvents(float startTime, float endTime, float currentSpeed, object sender);
        #endregion

        public int Index { get; set; }

        public IAnimationLayer Layer { get; }
        public IAnimator Animator { get; }
        public IAnimationAsset Animation { get; }

        public float LengthInSeconds { get; }

        public AnimationLoopMode LoopMode { get; set; } 

        public bool IsAdditive { get; set; }
        public bool IsBlend { get; set; }

        public float Time { get; set; }
        public void SetTime(float time, bool resetFlags = true);
        public bool OverrideTime { get; set; }
        public float TimeOverride { get; set; }

        public float GetLoopedTime(float time, bool canLoop = true);

        public float Speed { get; set; }
        public float InternalSpeed { get; }

        public float Mix { get; set; }
        /// <summary>
        /// Last mix used during the animation step
        /// </summary>
        public float DynamicMix { get; }


        public WeightedAvatarMaskComposite GetInvertedMask(WeightedAvatarMaskComposite mask);

        public void SetTopAvatarMask(WeightedAvatarMask mask, bool invertMask);
        public void SetTopAvatarMask(WeightedAvatarMaskComposite mask, bool invertMask);

        public void SetAvatarMask(WeightedAvatarMask mask, bool invertMask);
        public void SetAvatarMask(WeightedAvatarMaskComposite mask, bool invertMask);


        public bool Paused { get; set; }

        public bool HasAnimationEvents { get; }


        public void ResetLoop();

        public TransformHierarchy Hierarchy { get; }

        public bool HasDerivativeHierarchyOf(IAnimationPlayer other) => Hierarchy == other.Hierarchy || (Hierarchy != null && Hierarchy.IsDerivative(other.Hierarchy));

    }
}
