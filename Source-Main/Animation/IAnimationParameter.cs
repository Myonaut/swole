using System;

namespace Swole.Animation
{
    public interface IAnimationParameter : ICloneable, IDisposable
    {
        public string Name { get; set; }

        public int IndexInAnimator { get; set; }

        public float Value { get; }

        public float GetDefaultValue();

        public float UpdateAndGetValue();

        public void SetValue(float value);

        public void Initialize(IAnimator animator, object obj = null);

        public bool DisposeIfHasPrefix(string prefix);
    }
    public interface IAnimationParameterBoolean : IAnimationParameter
    {

        public bool IsTrue { get; }

    }
    public interface IAnimationParameterTrigger : IAnimationParameterBoolean
    {

        public void Arm();

        public bool TryConsume();

    }

    [Serializable]
    public enum AnimationParameterValueType
    {

        Float, Boolean, Trigger

    }

}
