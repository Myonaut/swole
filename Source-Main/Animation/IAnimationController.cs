using System;

namespace Swole.Animation
{
    public interface IAnimationController : EngineInternal.IEngineObject
    {

        public string Prefix { get; set; }

        public IAnimationLayer[] Layers { get; set; }
        public int LayerCount { get; }
        public IAnimationLayer GetLayer(int index);
        public IAnimationLayer GetLayerUnsafe(int index);
        public void SetLayer(int index, IAnimationLayer layer);
        public void SetLayerUnsafe(int index, IAnimationLayer layer);

        public IAnimationParameter[] FloatParameters { get; set; }
        public IAnimationParameterBoolean[] BoolParameters { get; set; }
        public IAnimationParameterTrigger[] TriggerParameters { get; set; }
        public IAnimationParameter[] Parameters { get; }
        public IAnimationParameter[] GetParameters(bool instantiate = false);
        public IAnimationParameter GetAnimationParameter(AnimationParameterIdentifier identifier);

        public IAnimationReference[] AnimationReferences { get; set; }
        public IBlendTree1D[] BlendTrees1D { get; set; }
        public IAnimationMotionController GetMotionController(MotionControllerIdentifier identifier); 
    }

    [Serializable]
    public struct AnimationParameterIdentifier
    {

        public AnimationParameterValueType type;
        public int index;

    }

    [Serializable]
    public struct MotionControllerIdentifier : IEquatable<MotionControllerIdentifier>
    {

        public MotionControllerType type;
        public int index;

        public bool Equals(MotionControllerIdentifier other)
        {

            return type == other.type && index == other.index;

        }

    }

}
