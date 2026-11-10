using System;
using System.Collections.Generic;

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
        public int TransitionCount { get; }
        public Transition GetTransition(int index);
        public Transition GetTransitionUnsafe(int index);
        public int GetTransitionIndex(Transition transition);
        public int FindTransitionIndex(string transitionName);
        public Transition FindTransition(string transitionName);
        public bool TryFindTransition(string transitionName, out Transition transition);

        public bool IsActive();

        public void SetWeight(float weight);

        public float GetWeight();

        public float GetTime(float addTime = 0);

        public float GetNormalizedTime(float addTime = 0);

        public float GetPreviousTime();

        public float GetPreviousNormalizedTime();

        public void SetTime(float time, bool resetFlags = true);

        public void SetNormalizedTime(float normalizedTime, bool resetFlags = true);

        public void ForceSetPreviousTime(float time);

        public void ForceSetPreviousNormalizedTime(float normalizedTime);

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

        public HashSet<int> GetActiveParameters(IAnimationLayer layer, HashSet<int> indices);

        public float GetCurrentBoneHeight(string boneName);
        public float GetBoneHeight(string boneName, float timeOffset);
        public float GetBoneHeight(string boneName, float timeOffset, out bool foundCurve); 
        public float GetBoneHeightAtTime(string boneName, float time);
        public float GetBoneHeightAtTime(string boneName, float time, out bool foundCurve);

        public bool AffectsTransform(string transformName);

        public void NonAdditivePrepass(bool useMultithreading);

        public void ApplyPropertyStates();

    }

    [Serializable]
    public enum TransitionComparisonType
    {

        EqualTo, NotEqualTo, GreaterThan, LessThan, GreaterThanOrEqualTo, LessThanOrEqualTo, True, False, Trigger

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
                value = layer.GetParameter(parameterIndex).Value;
            } 
            else
            {
                value = bias.GetBiasedParameterValue(layer, parameterIndex, 0f, false); 
            }

            if (absoluteValue) value = Math.Abs(value);
            
            switch (comparisonType)
            {

                case TransitionComparisonType.EqualTo:
                    return value == parameterValue;

                case TransitionComparisonType.NotEqualTo:
                    return value != parameterValue;

                case TransitionComparisonType.Trigger:
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

        public TransitionTimeRange(float minNormalizedTime, float maxNormalizedTime)
        {
            this.minNormalizedTime = minNormalizedTime;
            this.maxNormalizedTime = maxNormalizedTime;
        }
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
                    if (Transition.IsInNormalizedTimeRange(normalizedTime, timeRange.minNormalizedTime, timeRange.maxNormalizedTime, false, false))
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

        public bool RequiresParameter(string parameterName)
        {
            if (parameters == null) return false;

            foreach(var parameter in parameters) if (parameter.parameterName == parameterName) return true;
            return false;
        }
        public bool RequiresParameter(string parameterName, TransitionComparisonType compType)
        {
            if (parameters == null) return false;

            foreach (var parameter in parameters) if (parameter.parameterName == parameterName && parameter.comparisonType == compType) return true;
            return false;
        }

    }

    [Serializable]
    public class Transition : ICloneable
    {

        [Serializable]
        public enum TimeSyncMode
        {
            None, Local, Target, Blend
        }

        public Transition Duplicate()
        {
            Transition clone = new Transition();

            // Use reflection to copy all serializable fields automatically
            var fields = typeof(Transition).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            foreach (var field in fields)
            {
                // Skip fields marked with [NonSerialized]
                if (field.GetCustomAttributes(typeof(NonSerializedAttribute), false).Length > 0)
                    continue;

                var value = field.GetValue(this);

                // Deep clone arrays that implement ICloneable
                if (value != null && value.GetType().IsArray && typeof(ICloneable).IsAssignableFrom(value.GetType().GetElementType()))
                {
                    var array = value as Array;
                    var clonedArray = Array.CreateInstance(value.GetType().GetElementType(), array.Length);
                    for (int i = 0; i < array.Length; i++)
                    {
                        var element = array.GetValue(i);
                        clonedArray.SetValue(element != null ? ((ICloneable)element).Clone() : null, i);
                    }
                    field.SetValue(clone, clonedArray);
                }
                // Deep clone arrays of strings
                else if (value is string[] stringArray)
                {
                    field.SetValue(clone, (string[])stringArray.Clone());
                }
                else
                {
                    // For value types, strings, and null references, direct assignment is fine
                    field.SetValue(clone, value);
                }
            }

            return clone; 
        }
        public object Clone() => Duplicate();

        public string id;
        public string name
        {
            get => id;
            set => id = value;
        }
        public string ID => id;
        public string Name => id;

        public int targetStateIndex;

#if UNITY_EDITOR
        [UnityEngine.Tooltip("Higher means the transition is considered sooner.")]
#endif
        public int priority;

#if UNITY_EDITOR
        [UnityEngine.Tooltip("Should the transition only be allowed to execute when flagged?")]
#endif
        public bool disableAutoTransition;

        [NonSerialized]
        public bool allowTransitionFlag;

        public void Flag()
        {
            allowTransitionFlag = true;
        }
        public void Unflag()
        {
            allowTransitionFlag = false;
        }

#if UNITY_EDITOR
        [UnityEngine.Tooltip("Should the transition target be allowed to execute its own transitions before this one is complete?")]
#endif
        public bool allowMultiTransition;

#if UNITY_EDITOR
        [UnityEngine.Tooltip("if true, this transition cannot be used as a multi-transition target for another transition")]
#endif
        public bool mustBeFirstInChain;

#if UNITY_EDITOR
        [UnityEngine.Tooltip("How long the transition lasts. Value of zero is instant. NOT NORMALIZED TIME.")]
#endif
        public float transitionTime;

        public bool ignoreMinNormalizedTime;

#if UNITY_EDITOR
        [UnityEngine.Range(0, 1), UnityEngine.Tooltip("Normalized time must be above this for the transition to trigger.")]
#endif
        public float minNormalizedTime;

        public bool ignoreMaxNormalizedTime;

#if UNITY_EDITOR
        [UnityEngine.Range(-1, 1), UnityEngine.Tooltip("Normalized time must be below this for the transition to trigger. Set below minimum normalized time for no maximum requirement.")]
#endif
        public float maxNormalizedTime = -1f;

        public bool scaleTransitionTimeBasedOnNormalizedTimeRange;
        public float transitionTimeScaleByMidpoint = 1f;
        public float normalizedTimeRangeMidpoint = 0.5f;

        public float GetTransitionTime(float normalizedTime)
        {
            if (scaleTransitionTimeBasedOnNormalizedTimeRange)
            {
                return transitionTime * GetTransitionTimeScale(normalizedTime);
            }
            else
            {
                return transitionTime;
            }
        }

        public TransitionRequirement[] validRequirementPaths;
        public bool HasRequirements => validRequirementPaths != null && validRequirementPaths.Length > 0;

        public bool setLocalNormalizedTime;
#if UNITY_EDITOR
        [UnityEngine.Range(0, 1), UnityEngine.Tooltip("The normalized time to set the current state to when this transition is triggered (if setLocalNormalizedTime is true).")]
#endif
        public float localNormalizedTime;

        public bool setTargetNormalizedTime;
#if UNITY_EDITOR
        [UnityEngine.Tooltip("Should the target normalized time be treated as a value that is relative to the local normalized time of the transitioning state?")]
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

        public TimeSyncMode timeSyncMode = TimeSyncMode.None;

        public bool syncTargetSpeedToLocalSpeed;
        public float localSyncSpeedMultiplier = 1f;
        public float targetSyncSpeedMultiplier = 1f;

        public float freezeTargetTransitionTimeThreshold = 0f;
        public float GetScaledFreezeTargetTransitionTimeThresholdByTransitionTime(float transitionTime) => freezeTargetTransitionTimeThreshold * (transitionTime / this.transitionTime); 
        public float GetScaledFreezeTargetTransitionTimeThreshold(float normalizedTime)
        {
            if (scaleTransitionTimeBasedOnNormalizedTimeRange)
            {
                return freezeTargetTransitionTimeThreshold * GetTransitionTimeScale(normalizedTime);
            }
            else
            {
                return freezeTargetTransitionTimeThreshold;
            }
        }

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

        public bool IsInNormalizedTimeRange(float normalizedTime) => IsInNormalizedTimeRange(normalizedTime, minNormalizedTime, maxNormalizedTime, ignoreMinNormalizedTime, ignoreMaxNormalizedTime || maxNormalizedTime <= minNormalizedTime); 

        /// <summary>
        /// Determines if the given normalized time is within the min/max normalized time range.
        /// Supports wrapping for negative values when not ignoring min/max.
        /// </summary>
        /// <param name="normalizedTime">The normalized time to check (typically in range 0-1)</param>
        /// <returns>True if the normalized time is within the valid range</returns>
        public static bool IsInNormalizedTimeRange(float normalizedTime, float minNormalizedTime, float maxNormalizedTime, bool ignoreMinNormalizedTime, bool ignoreMaxNormalizedTime)
        {
            if (ignoreMinNormalizedTime && ignoreMaxNormalizedTime) return true;

            float min = minNormalizedTime;
            float max = maxNormalizedTime;

            // Handle ignored min/max by clamping them to valid range
            if (ignoreMinNormalizedTime)
            {
                min = float.MinValue;//Math.Max(0f, Math.Min(1f, min));
            }
            if (ignoreMaxNormalizedTime)
            {
                max = float.MaxValue;//Math.Max(0f, Math.Min(1f, max));
            } 

            // Normalize the input time to [0, 1] range
            if (normalizedTime < 0f || normalizedTime > 1f)
            {
                normalizedTime = normalizedTime - (float)Math.Floor(normalizedTime);
            }

            // Handle wrapping for min/max when not ignoring
            if (!ignoreMinNormalizedTime && min < 0f)
            {
                // Wrap negative min value
                min = min - (float)Math.Floor(min);
            }
            if (!ignoreMaxNormalizedTime && max < 0f)
            {
                // Wrap negative max value
                max = max - (float)Math.Floor(max);
            }

            // If max is less than min, the range wraps around 0/1 boundary
            if (max < min)
            {
                // Range wraps: valid if time >= min OR time <= max
                return normalizedTime >= min || normalizedTime <= max;
            }
            else
            {
                // Normal range: valid if time is between min and max
                return normalizedTime >= min && normalizedTime <= max;
            }
        }

        private static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * Math.Clamp(t, 0f, 1f);
        }

        public float GetTransitionTimeScale(float normalizedTime)
        {
            return GetTransitionTimeScale(normalizedTime, minNormalizedTime, maxNormalizedTime, ignoreMinNormalizedTime, ignoreMaxNormalizedTime, normalizedTimeRangeMidpoint, transitionTimeScaleByMidpoint);
        }

        /// <summary>
        /// Calculates the transition time scale based on the normalized time's distance from the midpoint.
        /// At the midpoint, returns transitionTimeScaleByMidpoint; at the range boundaries, returns 1f.
        /// Supports wrapping when both min and max are active.
        /// </summary>
        /// <param name="normalizedTime">The normalized time to evaluate (typically in range 0-1)</param>
        /// <returns>The transition time scale to apply</returns>
        public static float GetTransitionTimeScale(float normalizedTime, float minNormalizedTime, float maxNormalizedTime, bool ignoreMinNormalizedTime, bool ignoreMaxNormalizedTime, float normalizedTimeRangeMidpoint, float transitionTimeScaleByMidpoint)
        {
            // Normalize input to [0, 1] range
            if (normalizedTime < 0f || normalizedTime > 1f)
            {
                normalizedTime = normalizedTime - (float)Math.Floor(normalizedTime);
            }

            float midpoint = normalizedTimeRangeMidpoint;
            if (midpoint < 0f || midpoint > 1f)
            {
                midpoint = midpoint - (float)Math.Floor(midpoint);
            }

            bool minActive = !ignoreMinNormalizedTime;
            bool maxActive = !ignoreMaxNormalizedTime;

            // If neither endpoint is active, return 1f
            if (!minActive && !maxActive)
            {
                return 1f;
            }

            float min = minNormalizedTime;
            float max = maxNormalizedTime;

            // Wrap min/max if they're active and negative
            if (minActive && min < 0f)
            {
                min = min - (float)Math.Floor(min);
            }
            if (maxActive && max < 0f)
            {
                max = max - (float)Math.Floor(max);
            }

            // Both endpoints are active
            if (minActive && maxActive)
            {
                bool rangeWraps = max < min;

                // Calculate distances considering wrapping
                float distanceFromMidpoint;
                float minToMidpoint, maxToMidpoint;

                if (rangeWraps)
                {
                    // Range wraps around 0/1 boundary
                    // Valid range is [min, 1.0] - [0, max]

                    // Calculate wrapped midpoint position
                    float rangeSize = (1f - min) + max; // Total size of wrapped range
                    float midpointInRange = rangeSize * 0.5f;
                    float wrappedMidpoint = (min + midpointInRange) % 1f;

                    // Use custom midpoint if provided, otherwise calculate
                    float effectiveMidpoint = midpoint;

                    // Determine if normalizedTime is in the wrapped range
                    bool isInRange = normalizedTime >= min || normalizedTime <= max;
                    if (!isInRange)
                    {
                        return 1f; // Outside range
                    }

                    // Calculate distance from midpoint in wrapped space
                    if (normalizedTime >= min && effectiveMidpoint >= min)
                    {
                        // Both in upper part
                        distanceFromMidpoint = Math.Abs(normalizedTime - effectiveMidpoint);
                        minToMidpoint = effectiveMidpoint - min;
                        maxToMidpoint = (1f - effectiveMidpoint) + max;
                    }
                    else if (normalizedTime <= max && effectiveMidpoint <= max)
                    {
                        // Both in lower part
                        distanceFromMidpoint = Math.Abs(normalizedTime - effectiveMidpoint);
                        minToMidpoint = (1f - min) + effectiveMidpoint;
                        maxToMidpoint = max - effectiveMidpoint;
                    }
                    else if (normalizedTime >= min && effectiveMidpoint <= max)
                    {
                        // Time in upper, midpoint in lower - measure wrapped
                        distanceFromMidpoint = (1f - normalizedTime) + effectiveMidpoint;
                        minToMidpoint = (1f - min) + effectiveMidpoint;
                        maxToMidpoint = max - effectiveMidpoint;
                    }
                    else
                    {
                        // Time in lower, midpoint in upper - measure wrapped
                        distanceFromMidpoint = (1f - effectiveMidpoint) + normalizedTime;
                        minToMidpoint = effectiveMidpoint - min;
                        maxToMidpoint = (1f - effectiveMidpoint) + max;
                    }

                    // Determine which side of midpoint we're on
                    float t;
                    if (normalizedTime < effectiveMidpoint)
                    {
                        // Check if we're actually on the min side considering wrapping
                        if (effectiveMidpoint <= max && normalizedTime <= max)
                        {
                            // On max side approaching midpoint
                            t = maxToMidpoint > 0f ? distanceFromMidpoint / maxToMidpoint : 0f;
                        }
                        else
                        {
                            // On min side approaching midpoint
                            t = minToMidpoint > 0f ? distanceFromMidpoint / minToMidpoint : 0f;
                        }
                    }
                    else
                    {
                        // Check if we're actually on the max side considering wrapping
                        if (effectiveMidpoint >= min && normalizedTime >= min)
                        {
                            // On min side approaching midpoint
                            t = minToMidpoint > 0f ? distanceFromMidpoint / minToMidpoint : 0f;
                        }
                        else
                        {
                            // On max side approaching midpoint
                            t = maxToMidpoint > 0f ? distanceFromMidpoint / maxToMidpoint : 0f;
                        }
                    }

                    return Math.Clamp(Lerp(transitionTimeScaleByMidpoint, 1f, t), Math.Min(transitionTimeScaleByMidpoint, 1f), Math.Max(transitionTimeScaleByMidpoint, 1f));
                }
                else
                {
                    // Normal range: min <= max

                    // Check if in range
                    if (normalizedTime < min || normalizedTime > max)
                    {
                        return 1f; // Outside range
                    }

                    float effectiveMidpoint = midpoint;

                    // Clamp midpoint to range
                    effectiveMidpoint = Math.Clamp(effectiveMidpoint, min, max);

                    // Calculate distances
                    float distToMin = effectiveMidpoint - min;
                    float distToMax = max - effectiveMidpoint;

                    float t;
                    if (normalizedTime <= effectiveMidpoint)
                    {
                        // Between min and midpoint
                        float dist = effectiveMidpoint - normalizedTime;
                        t = distToMin > 0f ? dist / distToMin : 0f;
                    }
                    else
                    {
                        // Between midpoint and max
                        float dist = normalizedTime - effectiveMidpoint;
                        t = distToMax > 0f ? dist / distToMax : 0f;
                    }

                    return Math.Clamp(Lerp(transitionTimeScaleByMidpoint, 1f, t), Math.Min(transitionTimeScaleByMidpoint, 1f), Math.Max(transitionTimeScaleByMidpoint, 1f));
                }
            }
            // Only min is active
            else if (minActive)
            {
                if (min < 0f || min > 1f)
                {
                    min = min - (float)Math.Floor(min);
                }

                float effectiveMidpoint = midpoint;

                // Determine the range from min to midpoint
                float rangeStart = min;
                float rangeEnd = effectiveMidpoint;

                // Check if we need to wrap
                bool wraps = rangeEnd < rangeStart;

                if (wraps)
                {
                    // Range wraps around 0/1
                    bool inRange = normalizedTime >= rangeStart || normalizedTime <= rangeEnd;
                    if (!inRange)
                    {
                        return 1f;
                    }

                    // Calculate wrapped distance
                    float totalDist = (1f - rangeStart) + rangeEnd;
                    float distFromMin;

                    if (normalizedTime >= rangeStart)
                    {
                        distFromMin = (1f - rangeStart) - (1f - normalizedTime);
                    }
                    else
                    {
                        distFromMin = (1f - rangeStart) + normalizedTime;
                    }

                    float t = totalDist > 0f ? distFromMin / totalDist : 1f;
                    return Math.Clamp(Lerp(1f, transitionTimeScaleByMidpoint, t), Math.Min(transitionTimeScaleByMidpoint, 1f), Math.Max(transitionTimeScaleByMidpoint, 1f));
                }
                else
                {
                    // Normal range
                    if (normalizedTime < rangeStart || normalizedTime > rangeEnd)
                    {
                        return 1f;
                    }

                    float totalDist = rangeEnd - rangeStart;
                    float distFromMin = normalizedTime - rangeStart;
                    float t = totalDist > 0f ? distFromMin / totalDist : 1f;
                    return Math.Clamp(Lerp(1f, transitionTimeScaleByMidpoint, t), Math.Min(transitionTimeScaleByMidpoint, 1f), Math.Max(transitionTimeScaleByMidpoint, 1f));
                }
            }
            // Only max is active
            else if (maxActive)
            {
                if (max < 0f || max > 1f)
                {
                    max = max - (float)Math.Floor(max);
                }

                float effectiveMidpoint = midpoint;

                // Determine the range from midpoint to max
                float rangeStart = effectiveMidpoint;
                float rangeEnd = max;

                // Check if we need to wrap
                bool wraps = rangeEnd < rangeStart;

                if (wraps)
                {
                    // Range wraps around 0/1
                    bool inRange = normalizedTime >= rangeStart || normalizedTime <= rangeEnd;
                    if (!inRange)
                    {
                        return 1f;
                    }

                    // Calculate wrapped distance
                    float totalDist = (1f - rangeStart) + rangeEnd;
                    float distFromMidpoint;

                    if (normalizedTime >= rangeStart)
                    {
                        distFromMidpoint = (1f - rangeStart) - (1f - normalizedTime);
                    }
                    else
                    {
                        distFromMidpoint = (1f - rangeStart) + normalizedTime;
                    }

                    float t = totalDist > 0f ? distFromMidpoint / totalDist : 1f;
                    return Math.Clamp(Lerp(transitionTimeScaleByMidpoint, 1f, t), Math.Min(transitionTimeScaleByMidpoint, 1f), Math.Max(transitionTimeScaleByMidpoint, 1f));
                }
                else
                {
                    // Normal range
                    if (normalizedTime < rangeStart || normalizedTime > rangeEnd)
                    {
                        return 1f;
                    }

                    float totalDist = rangeEnd - rangeStart;
                    float distFromMidpoint = normalizedTime - rangeStart;
                    float t = totalDist > 0f ? distFromMidpoint / totalDist : 1f;
                    return Math.Clamp(Lerp(transitionTimeScaleByMidpoint, 1f, t), Math.Min(transitionTimeScaleByMidpoint, 1f), Math.Max(transitionTimeScaleByMidpoint, 1f));
                }
            }

            return 1f;
        }

#if UNITY_EDITOR
        [UnityEngine.Tooltip("Any changes that need to be applied to animation parameters during or after this transition.")]
#endif
        public TransitionParameterStateChange[] parameterStateChanges;

        public bool HasMetRequirements(IAnimationLayer layer, int currentFrame, float normalizedTime, IAnimationMotionController localController, IAnimationMotionController targetController, bool isMultiTransition, out float transitionTime, out int pathIndex)
        {

            transitionTime = this.GetTransitionTime(normalizedTime);
            pathIndex = -1;
            if ((disableAutoTransition && !allowTransitionFlag) || (isMultiTransition && mustBeFirstInChain)) return false; 

            if ((currentFrame >= lastTriggerFrame && (currentFrame - lastTriggerFrame) < Math.Max(1, cooldownFrames)) || !IsInNormalizedTimeRange(normalizedTime) || layer == null)
            {
                return false;
            }

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
                    pathIndex = a;
                    return true;
                }
            }

            return false;

        }

        public bool HasRequirementParameter(string parameterName)
        {
            if (validRequirementPaths == null) return false;

            foreach (var path in validRequirementPaths) if (path.RequiresParameter(parameterName)) return true;
            return false;
        }
        public bool HasRequirementParameter(string parameterName, TransitionComparisonType compType)
        {
            if (validRequirementPaths == null) return false;

            foreach (var path in validRequirementPaths) if (path.RequiresParameter(parameterName, compType)) return true; 
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

        public bool useMinNormalizedTransitionTimeForCancellation;

        public float minNormalizedTransitionTimeForCancellation;

        public bool useMaxNormalizedTransitionTimeForCancellation;

        public float maxNormalizedTransitionTimeForCancellation; 

        public bool CanCancel(IAnimationLayer layer, float normalizedTime, float normalizedTransitionTime, IAnimationMotionController localController, IAnimationMotionController targetController, out float cancellationTimeMultiplier, out int pathIndex)
        {

            cancellationTimeMultiplier = this.cancellationTimeMultiplier;
            pathIndex = -1;

            if (!IsInNormalizedTimeRange(normalizedTime) || !IsInNormalizedTimeRange(normalizedTransitionTime, minNormalizedTransitionTimeForCancellation, maxNormalizedTransitionTimeForCancellation, useMinNormalizedTransitionTimeForCancellation, useMaxNormalizedTransitionTimeForCancellation) || cancellationRequirementPaths == null || cancellationRequirementPaths.Length <= 0 || layer == null) return false;

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
                    pathIndex = a;
                    return true;
                }
            }

            return false;

        }

        public void Start(IAnimationLayer layer, IAnimationMotionController controller, IAnimationLayerState target, int pathIndex)
        {
            allowTransitionFlag = false; // consume the transition flag if it was set
            lastTriggerFrame = swole.Frame; 

            if (setLocalNormalizedTime)
            {
                if (controller != null) controller.SetNormalizedTime(layer, localNormalizedTime);
            }
            if (setTargetNormalizedTime)
            {
                target.SetNormalizedTime(GetTargetNormalizedTime(controller.GetNormalizedTime(layer)));
                //Debug.Log($"{Name} Set Normalized Time of {target.Name}::: {controller.GetNormalizedTime(Layer)} -> {target.GetNormalizedTime()}");    
            }

            ConsumeTriggers(pathIndex, layer);

            if (parameterStateChanges != null)
            {
                foreach (var parameterStateChange in parameterStateChanges)
                {
                    if (!parameterStateChange.applyAtEnd && !string.IsNullOrWhiteSpace(parameterStateChange.parameterName))
                    {
                        var parameter = layer.Animator.FindParameter(parameterStateChange.parameterName);
                        if (parameter == null) continue;

                        parameter.SetValue(parameterStateChange.parameterValue);
                    }
                }
            }
        }

        public void ConsumeTriggers(int pathIndex, IAnimationLayer layer)
        {
            if (pathIndex >= 0 && validRequirementPaths != null && validRequirementPaths.Length > 0) 
            {
                var path = validRequirementPaths[pathIndex];
                if (path.parameters != null)
                {
                    foreach (var p in path.parameters)
                    {
                        if (p.comparisonType == TransitionComparisonType.Trigger) 
                        {
                            var parameter = layer.Animator.FindParameter(p.parameterName);
                            if (parameter != null)
                            {
                                parameter.UpdateAndGetValue();
                            }
                        }
                    }
                }
            }
        }

        public void ConsumeCancellationTriggers(int pathIndex, IAnimationLayer layer)
        {
            if (pathIndex >= 0 && cancellationRequirementPaths != null && cancellationRequirementPaths.Length > 0)
            {
                var path = cancellationRequirementPaths[pathIndex];
                if (path.parameters != null)
                {
                    foreach (var p in path.parameters)
                    {
                        if (p.comparisonType == TransitionComparisonType.Trigger)
                        {
                            var parameter = layer.Animator.FindParameter(p.parameterName);
                            if (parameter != null) parameter.UpdateAndGetValue(); 
                        }
                    }
                }
            }
        }

        public HashSet<int> GetActiveParameters(IAnimationLayer layer, HashSet<int> indices)
        {
            if (indices == null) indices = new HashSet<int>();

            if (parameterStateChanges != null)
            {
                foreach (var parameterStateChange in parameterStateChanges)
                {
                    if (!string.IsNullOrWhiteSpace(parameterStateChange.parameterName))
                    {
                        var parameterIndex = layer.FindParameterIndex(parameterStateChange.parameterName);
                        if (parameterIndex >= 0 && !indices.Contains(parameterIndex))
                        {
                            indices.Add(parameterIndex);
                        }
                    }
                }
            }

            if (cancellationRequirementPaths != null)
            {
                foreach (var requirement in cancellationRequirementPaths)
                {
                    if (requirement.parameters != null)
                    {
                        foreach (var parameter in requirement.parameters)
                        {
                            var parameterIndex = layer.FindParameterIndex(parameter.parameterName);
                            if (parameterIndex >= 0 && !indices.Contains(parameterIndex))
                            {
                                indices.Add(parameterIndex);
                            }
                        }
                    }
                }
            }

            return indices;
        }

        public string[] tags;

        public int TagCount => tags == null ? 0 : tags.Length;

        public bool HasTag(string tag, bool ignoreCase = true)
        {
            if (tags == null || tags.Length <= 0) return false;
            foreach (var t in tags)
            {
                if (string.Equals(t, tag, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)) return true;
            }
            return false;
        }

        public IEnumerable<string> Tags()
        {
            if (tags == null) yield break;

            foreach (var t in tags)
            {
                yield return t;
            }
        }

        public bool TryGetValueTag(string prefix, out float value, bool ignoreCase = true)
        {
            value = 0f;
            if (tags == null) return false;

            foreach (var t in tags)
            {
                if (t.StartsWith(prefix, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                {
                    var parts = t.Split('=');
                    if (parts.Length == 2 && float.TryParse(parts[1], out value))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

    }

}
