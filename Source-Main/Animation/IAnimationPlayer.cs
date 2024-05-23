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
        #endregion

        public int Index { get; set; }

        public IAnimator Animator { get; }
        public IAnimationAsset Animation { get; }

        public float LengthInSeconds { get; }

        public AnimationLoopMode LoopMode { get; set; } 

        public bool IsAdditive { get; set; }
        public bool IsBlend { get; set; }

        public float Time { get; set; }

        public float Speed { get; set; }
        public float InternalSpeed { get; }

        public float Mix { get; set; }
        public bool Paused { get; set; }


        public void ResetLoop();

        public TransformHierarchy Hierarchy { get; }

        public bool HasDerivativeHierarchyOf(IAnimationPlayer other) => Hierarchy == other.Hierarchy || (Hierarchy != null && Hierarchy.IsDerivative(other.Hierarchy));

    }
}
