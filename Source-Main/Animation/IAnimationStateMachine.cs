using System;

namespace Swole.Animation
{
    public interface IAnimationStateMachine
    {
        public string Name { get; set; }
        public int Index { get; set; }
        public IAnimationLayer Layer { get; }
        /// <summary>
        /// This index is local to the motion controller identifiers array in the owning layer. If the layer and machine have been instantiated at runtime, then it's local to the layer's motion controller array instead.
        /// </summary>
        public int MotionControllerIndex { get; set; }
        public Transition[] Transitions { get; set; }

        public bool IsActive();

        public void SetWeight(float weight);

        public float GetWeight();

        public float GetTime(float addTime = 0);

        public float GetNormalizedTime(float addTime = 0);

        public void SetTime(float time);

        public void SetNormalizedTime(float normalizedTime);

        public float GetEstimatedDuration();

        public void RestartAnims();

        public void ResyncAnims();

        public int TransitionTarget { get; }

        public float TransitionTime { get; }

        public float TransitionTimeLeft { get; }

        public void ResetTransition();

    }

    [Serializable]
    public enum TransitionComparisonType
    {

        EqualTo, NotEqualTo, GreaterThan, LessThan, GreaterThanOrEqualTo, LessThanOrEqualTo, True, False

    }

    /// <summary>
    /// An animator parameter state requirement for a given transition
    /// </summary>
    [Serializable]
    public struct TransitionParameter
    {

        public TransitionComparisonType comparisonType;

        public string parameterName;
        public float parameterValue;

        public bool IsTrue(IAnimator animator)
        {

            //var parameter = animator.GetParameter(parameterIndex);
            var parameter = animator.FindParameter(parameterName); // (?) TODO: Do something more performant?
            if (parameter == null) return false;

            float value = parameter.UpdateAndGetValue();

            switch (comparisonType)
            {

                case TransitionComparisonType.EqualTo:
                    return value == parameterValue;

                case TransitionComparisonType.NotEqualTo:
                    return value != parameterValue;

                case TransitionComparisonType.True:
                    return value >= 0.5f;

                case TransitionComparisonType.False:
                    return value < 0.5f;

                case TransitionComparisonType.GreaterThan:
                    return value > parameterValue;

                case TransitionComparisonType.LessThan:
                    return value < parameterValue;

                case TransitionComparisonType.GreaterThanOrEqualTo:
                    return value >= parameterValue;

                case TransitionComparisonType.LessThanOrEqualTo:
                    return value <= parameterValue;

            }

            return false;

        }

    }

    [Serializable]
    public struct TransitionRequirement
    {

        public TransitionParameter[] parameters;

        public bool IsFulfilled(IAnimator animator)
        {

            if (parameters == null || parameters.Length <= 0) return true;

            foreach (var parameter in parameters) if (!parameter.IsTrue(animator)) return false;

            return true;

        }

    }

    [Serializable]
    public class Transition
    {

        public int targetStateIndex;

#if UNITY_EDITOR
        [UnityEngine.Tooltip("How long the transition lasts. Value of zero is instant.")]
#endif
        public float transitionTime;

#if UNITY_EDITOR
        [UnityEngine.Range(0, 1)]
#endif
        public float minNormalizedTime;

        public TransitionRequirement[] validRequirementPaths;

        public bool HasPath(IAnimator animator, float normalizedTime)
        {

            if (normalizedTime < minNormalizedTime) return false;

            if (validRequirementPaths == null || validRequirementPaths.Length <= 0) return true;

            if (animator == null) return false;

            for (int a = 0; a < validRequirementPaths.Length; a++) if (validRequirementPaths[a].IsFulfilled(animator)) return true;

            return false;

        }

    }

}
