using System;
using System.Collections.Generic;

namespace Swole.Animation
{
    public interface IAnimationMotionController : ICloneable
    {
        public string Name { get; set; }

        public IAnimationMotionController Parent { get; }

        public void Initialize(IAnimationLayer layer, IAnimationMotionController parent = null);

        public float BaseSpeed { get; set; }

        public int SpeedMultiplierParameter { get; set; }

        public float GetSpeed(IAnimator animator);

        public AnimationLoopMode GetLoopMode(IAnimationLayer layer);

        public void ForceSetLoopMode(IAnimationLayer layer, AnimationLoopMode loopMode);

        public void SetWeight(float weight);
        public float GetWeight();

        public float GetDuration(IAnimationLayer layer);
        /// <summary>
        /// Get time length of controller scaled by playback speed
        /// </summary>
        public float GetScaledDuration(IAnimationLayer layer);

        public float GetTime(IAnimationLayer layer, float addTime = 0);
        public float GetNormalizedTime(IAnimationLayer layer, float addTime = 0);

        public void SetTime(IAnimationLayer layer, float time);
        public void SetNormalizedTime(IAnimationLayer layer, float normalizedTime);

        public bool HasAnimationPlayer(IAnimationLayer layer);

        public bool HasChildControllers { get; }

        public void GetChildIndexIdentifiers(List<MotionControllerIdentifier> identifiers, bool onlyAddIfNotPresent = true);

        public void RemapChildIndices(Dictionary<MotionControllerIdentifier, int> remapper, bool invalidateNonRemappedIndices = false);

        public void GetChildIndices(List<int> indices) { }

        public void RemapChildIndices(Dictionary<int, int> remapper, bool invalidateNonRemappedIndices = false);

        public void GetParameterIndices(IAnimationLayer layer, List<int> indices);

        public void RemapParameterIndices(IAnimationLayer layer, Dictionary<int, int> remapper, bool invalidateNonRemappedIndices = false);

        public bool HasDerivativeHierarchyOf(IAnimationLayer layer, IAnimationMotionController other);

        public int GetLongestHierarchyIndex(IAnimationLayer layer);
    }

    [Serializable]
    public enum MotionControllerType
    {
        AnimationReference, BlendTree1D
    }

    public interface IAnimationReference : IAnimationMotionController 
    {

        public AnimationLoopMode LoopMode { get; set; }

        public IAnimationAsset Animation { get; set; }

        public IAnimationPlayer AnimationPlayer { get; }

    }

    public interface IBlendTree : IAnimationMotionController 
    {

        public void SetParameterValues(IAnimationLayer layer);
        public int ParameterCount { get; }

        /// <summary>
        /// If the tree is affected by multiple parameters; localParameterIndex is used to fetch the parameter locally first.
        /// </summary>
        public abstract int GetParameterIndex(int localParameterIndex);

        public MotionControllerIdentifier[] GetChildControllerIndices();

    }
    public interface IMotionField { }

    public interface IBlendTree1D : IBlendTree 
    {

        public int ParameterIndex { get; set; }

        public IMotionField1D[] MotionFields { get; set; }

        public int FinalFieldIndex { get; }

    }

    public interface IMotionField1D : IMotionField 
    {
        public float Threshold { get; set; }
        public float Speed { get; set; }
        public MotionControllerIdentifier ControllerIdentifier { get; set; }
    }

    [Serializable]
    public class MotionField1D : IMotionField1D
    {

        public float threshold;
        public float Threshold
        {
            get => threshold;
            set => threshold = value;
        }
        public float speed;
        public float Speed
        {
            get => speed;
            set => speed = value;
        }
        public MotionControllerIdentifier controllerIdentifier;
        public MotionControllerIdentifier ControllerIdentifier
        {
            get => controllerIdentifier;
            set => controllerIdentifier = value;
        }

    }

}
