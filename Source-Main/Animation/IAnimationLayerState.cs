using System;

namespace Swole.Animation
{
    public interface IAnimationLayerState
    {
        public string Name { get; set; }
        public int Index { get; set; }
        public IAnimationLayer Layer { get; }
        /// <summary>
        /// This index is local to the motion controller identifiers array in the owning layer. If the layer and state have been instantiated at runtime, then it's local to the layer's motion controller array instead.
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

        public void Reset();

        public void ResyncAnims();

        public Transition ActiveTransition { get; }

        public int TransitionTarget { get; }

        public float TransitionTime { get; }

        public float TransitionTimeLeft { get; }

        public float TransitionProgress { get; }

        public bool IsTransitioning { get; }

        public void ResetTransition();

        public void CompletedTransition();

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
        public bool absoluteValue;

        public bool IsTrue(IAnimationLayer layer, IAnimationMotionController bias)
        {

            var parameterIndex = layer.FindParameterIndex(parameterName); // (?) TODO: Do something more performant?
            if (parameterIndex < 0) return false;

            float value;
            if (bias == null)
            {
                value = layer.GetParameter(parameterIndex).UpdateAndGetValue();
            } 
            else
            {
                value = bias.GetBiasedParameterValue(layer, parameterIndex, 0f, true); 
            }

            if (absoluteValue) value = Math.Abs(value);

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

    /// <summary>
    /// An animator parameter state that is applied when a given transition is triggered
    /// </summary>
    [Serializable]
    public struct TransitionParameterStateChange
    {
        public bool applyAtEnd;

        public string parameterName;
        public float parameterValue;

#if !UNITY_EDITOR
        [NonSerialized]
#endif
        public string comment;

        public void Apply(IAnimator animator)
        {
            var parameter = animator.FindParameter(parameterName); // (?) TODO: Do something more performant?
            if (parameter == null) return;

            parameter.SetValue(parameterValue);
        }
    }

    [Serializable]
    public struct TransitionTimeRange
    {
        public float minNormalizedTime;
        public float maxNormalizedTime;
    }

    [Serializable]
    public struct TransitionRequirement
    {

        public TransitionTimeRange[] validNormalizedTimeRanges;

        public TransitionParameter[] parameters;

#if UNITY_EDITOR
        [UnityEngine.Tooltip("Overrides transition time if set to a value greater than zero.")]
#endif
        public float transitionTime;
        
        public bool IsFulfilled(IAnimationLayer layer, float normalizedTime, IAnimationMotionController bias)
        {

            if (validNormalizedTimeRanges != null && validNormalizedTimeRanges.Length > 0)
            {
                bool flag = false;
                foreach(var timeRange in validNormalizedTimeRanges)
                {
                    if (normalizedTime >= timeRange.minNormalizedTime && normalizedTime <= timeRange.maxNormalizedTime)
                    {
                        flag = true;
                        break;
                    }
                }

                if (!flag) return false;
            }

            if (parameters == null || parameters.Length <= 0) return true;

            foreach (var parameter in parameters) if (!parameter.IsTrue(layer, bias)) return false;

            return true;

        }

    }

    [Serializable]
    public class Transition : ICloneable
    {

        // REMEMBER TO ADD NEW FIELDS HERE
        public Transition Duplicate()
        {
            Transition clone = new Transition();

            clone.targetStateIndex = targetStateIndex;
            clone.allowMultiTransition = allowMultiTransition;
            clone.transitionTime = transitionTime;

            clone.minNormalizedTime = minNormalizedTime;
            clone.maxNormalizedTime = maxNormalizedTime;

            clone.setLocalNormalizedTime = setLocalNormalizedTime;
            clone.localNormalizedTime = localNormalizedTime;
            clone.setTargetNormalizedTime = setTargetNormalizedTime;
            clone.relativeToLocalNormalizedTime = relativeToLocalNormalizedTime;
            clone.relativeLocalNormalizedTimeOffset = relativeLocalNormalizedTimeOffset;
            clone.clampRelativeLocalNormalizedTime = clampRelativeLocalNormalizedTime;
            clone.targetNormalizedTime = targetNormalizedTime;

            clone.syncTargetSpeedToLocalSpeed = syncTargetSpeedToLocalSpeed;
            clone.localSyncSpeedMultiplier = localSyncSpeedMultiplier;
            clone.targetSyncSpeedMultiplier = targetSyncSpeedMultiplier;

            clone.validRequirementPaths = validRequirementPaths == null ? null : ((TransitionRequirement[])validRequirementPaths.DeepClone());
            clone.parameterStateChanges = parameterStateChanges == null ? null : ((TransitionParameterStateChange[])parameterStateChanges.DeepClone());

            clone.parameterBias = parameterBias;

            clone.cooldownFrames = cooldownFrames;

            clone.overrideMuscleConfig = overrideMuscleConfig;

            clone.cancellationTimeMultiplier = cancellationTimeMultiplier;
            clone.allowCancellationRevert = allowCancellationRevert;
            clone.cancellationRequirementPaths = cancellationRequirementPaths == null ? null : ((TransitionRequirement[])cancellationRequirementPaths.DeepClone());
            clone.cancellationParameterStateChanges = cancellationParameterStateChanges == null ? null : ((TransitionParameterStateChange[])cancellationParameterStateChanges.DeepClone()); 

            return clone;
        }
        public object Clone() => Duplicate();

        public int targetStateIndex;

#if UNITY_EDITOR
        [UnityEngine.Tooltip("Should the transition target be allowed to execute its own transitions before this one is complete?")]
#endif
        public bool allowMultiTransition;

#if UNITY_EDITOR
        [UnityEngine.Tooltip("Does the transitioning state need to be the first in the transition chain?")]
#endif
        public bool mustBeFirstInChain;

#if UNITY_EDITOR
        [UnityEngine.Tooltip("How long the transition lasts. Value of zero is instant.")]
#endif
        public float transitionTime;

#if UNITY_EDITOR
        [UnityEngine.Range(0, 1), UnityEngine.Tooltip("Normalized time must be above this for the transition to trigger.")]
#endif
        public float minNormalizedTime;

#if UNITY_EDITOR
        [UnityEngine.Range(0, 1), UnityEngine.Tooltip("Normalized time must be below this for the transition to trigger.")]
#endif
        public float maxNormalizedTime = 1f;

        public TransitionRequirement[] validRequirementPaths;

        public bool setLocalNormalizedTime;
#if UNITY_EDITOR
        [UnityEngine.Range(0, 1), UnityEngine.Tooltip("The normalized time to set the current state to when this transition is triggered (if setLocalNormalizedTime is true).")]
#endif
        public float localNormalizedTime;

        public bool setTargetNormalizedTime;
#if UNITY_EDITOR
        [UnityEngine.Tooltip("Should the target normalized time be treated a value that is relative to the local normalized time of the transitioning state?")]
#endif
        public bool relativeToLocalNormalizedTime;
#if UNITY_EDITOR
        [UnityEngine.Tooltip("The offset to apply to local normalized time before it is used for setting target normalized time. (If relativeToLocalNormalizedTime is true)")]
#endif
        public float relativeLocalNormalizedTimeOffset;
        public bool clampRelativeLocalNormalizedTime;
#if UNITY_EDITOR
        [UnityEngine.Range(-2, 2), UnityEngine.Tooltip("The normalized time to set the next state to when this transition is triggered (if setTargetNormalizedTime is true). If relativeToLocalNormalizedTime is true, this is treated as an offset to the current normalized time of the local state.")]
#endif
        public float targetNormalizedTime;

        public bool syncTargetSpeedToLocalSpeed;
        public float localSyncSpeedMultiplier = 1f;
        public float targetSyncSpeedMultiplier = 1f;

        [Serializable]
        public enum ParameterBias
        {
            None, Local, Target
        }
        public ParameterBias parameterBias;

#if UNITY_EDITOR
        [UnityEngine.Tooltip("Number of frames to wait before the transition can be triggered again.")]
#endif
        public int cooldownFrames;
        [NonSerialized]
        public int lastTriggerFrame = -1;

#if UNITY_EDITOR
        [UnityEngine.Tooltip("Should the transitioning state's muscle config be immediately overridden by the new state? If false, the muscle configs of each state will be blended based on transition progress.")]
#endif
        public bool overrideMuscleConfig;

        // REMEMBER TO ADD NEW FIELDS TO Duplicate()

        public float GetTargetNormalizedTime(float localNormalizedTime)
        {
            float targetNormalizedTime = this.targetNormalizedTime;

            if (relativeToLocalNormalizedTime)
            {
                localNormalizedTime = localNormalizedTime - relativeLocalNormalizedTimeOffset;
                if (clampRelativeLocalNormalizedTime) 
                { 
                    localNormalizedTime = Math.Clamp(localNormalizedTime, 0f, 1f);
                }
                else if (localNormalizedTime < 0f || localNormalizedTime > 1f)
                {
                    localNormalizedTime = localNormalizedTime - (float)Math.Floor(localNormalizedTime);
                }

                targetNormalizedTime = localNormalizedTime + targetNormalizedTime;  
            }

            return targetNormalizedTime;
        }
        public float GetTargetNormalizedTime(float localNormalizedTime, float targetNormalizedTime) 
        {
            if (setTargetNormalizedTime)
            {
                targetNormalizedTime = GetTargetNormalizedTime(localNormalizedTime);
            }

            return targetNormalizedTime;
        }

#if UNITY_EDITOR
        [UnityEngine.Tooltip("Any changes that need to be applied to animation parameters during or after this transition.")]
#endif
        public TransitionParameterStateChange[] parameterStateChanges;

        // REMEMBER TO ADD NEW FIELDS TO Duplicate()

        public bool HasMetRequirements(IAnimationLayer layer, int currentFrame, float normalizedTime, IAnimationMotionController localController, IAnimationMotionController targetController, out float transitionTime)
        {

            transitionTime = this.transitionTime;

            if ((currentFrame >= lastTriggerFrame && (currentFrame - lastTriggerFrame) < cooldownFrames) || normalizedTime < minNormalizedTime || normalizedTime > maxNormalizedTime || layer == null) return false;

            if (validRequirementPaths == null || validRequirementPaths.Length <= 0) return true;

            IAnimationMotionController bias = null;
            switch(parameterBias)
            {
                case ParameterBias.Local:
                    bias = localController;
                    break;
                case ParameterBias.Target:
                    bias = targetController;
                    break;
            }
            for (int a = 0; a < validRequirementPaths.Length; a++)
            {
                var req = validRequirementPaths[a];
                if (req.IsFulfilled(layer, normalizedTime, bias))
                {
                    transitionTime = req.transitionTime <= 0f ? transitionTime : req.transitionTime;
                    return true;
                }
            }

            return false;

        }

#if UNITY_EDITOR
        [UnityEngine.Tooltip("Multiplies the time that has passed so far in the transition and uses it as the cancellation time. Value of zero makes the cancellation instant.")]
#endif
        public float cancellationTimeMultiplier = 1f;

#if UNITY_EDITOR
        [UnityEngine.Tooltip("Can the cancellation of the transition be reverted?")]
#endif
        public bool allowCancellationRevert;

        public TransitionRequirement[] cancellationRequirementPaths;

#if UNITY_EDITOR
        [UnityEngine.Tooltip("Any changes that need to be applied to animation parameters during or after this transition is/is being cancelled.")]
#endif
        public TransitionParameterStateChange[] cancellationParameterStateChanges;

        // REMEMBER TO ADD NEW FIELDS TO Duplicate()

        public bool CanCancel(IAnimationLayer layer, float normalizedTime, IAnimationMotionController localController, IAnimationMotionController targetController, out float cancellationTimeMultiplier)
        {

            cancellationTimeMultiplier = this.cancellationTimeMultiplier;

            if (normalizedTime < minNormalizedTime || normalizedTime > maxNormalizedTime || cancellationRequirementPaths == null || cancellationRequirementPaths.Length <= 0 || layer == null) return false;

            IAnimationMotionController bias = null;
            switch (parameterBias)
            {
                case ParameterBias.Local:
                    bias = localController;
                    break;
                case ParameterBias.Target:
                    bias = targetController;
                    break;
            }
            for (int a = 0; a < cancellationRequirementPaths.Length; a++)
            {
                var req = cancellationRequirementPaths[a];
                if (req.IsFulfilled(layer, normalizedTime, bias))
                {
                    cancellationTimeMultiplier = req.transitionTime <= 0f ? cancellationTimeMultiplier : req.transitionTime;
                    return true;
                }
            }

            return false;

        }

    }

}
