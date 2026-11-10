#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

using Swole.Animation;
using System.Linq;

namespace Swole.API.Unity.Animation
{

    /// <summary>
    /// Handles a single motion controller and any transitions between other states.
    /// </summary>
    [Serializable]
    public class CustomAnimationLayerState : IAnimationLayerState
    {
         
        public CustomAnimationLayerState NewInstance(IAnimationLayer layer, int newMotionControllerIndex = -1)
        {

            var instance = new CustomAnimationLayerState(); 

            instance.m_layer = layer;

            instance.name = name;
            instance.index = index;
            instance.motionControllerIndex = newMotionControllerIndex >= 0 ? newMotionControllerIndex : motionControllerIndex;

            if (transitions != null)
            {
                instance.transitions = new Transition[transitions.Length];
                for (int a = 0; a < transitions.Length; a++)
                {
                    var transition = transitions[a];
                    if (transition != null) instance.transitions[a] = transition.Duplicate();
                }
            }

            instance.puppetConfigurationAsset = puppetConfigurationAsset;
            instance.puppetConfigurationContentPath = puppetConfigurationContentPath;
            instance.puppetConfiguration = puppetConfiguration;

            return instance;

        }

        public CustomAnimationLayerState() { }

        public CustomAnimationLayerState(string name)
        {
            this.name = name;
        }

        public CustomAnimationLayerState(string name, int motionControllerIndex) : this(name)
        {
            this.motionControllerIndex = motionControllerIndex;
        }

        [NonSerialized]
        protected IAnimationLayer m_layer;
        public IAnimationLayer Layer => m_layer; 

        public string name;
        public string Name
        {
            get => name;
            set => name = value;
        }

        public int index;
        public int Index
        {
            get => index;
            set => index = value;
        }

        [SerializeField, Tooltip("This index is local to the motion controller identifiers array in the owning layer. If the layer and state have been instantiated at runtime, then it's local to the layer's motion controller array instead.")]
        public int motionControllerIndex = -1;
        //protected int motionControllerIndex;
        /// <summary>
        /// This index is local to the motion controller identifiers array in the owning layer. If the layer and state have been instantiated at runtime, then it's local to the layer's motion controller array instead.
        /// </summary>
        public int MotionControllerIndex 
        {
            get => motionControllerIndex;
            set => motionControllerIndex = value;
        } 

        public bool IsActive()
        {

            if (Layer == null) return false;

            var controller = Layer.GetMotionController(MotionControllerIndex);
            if (controller == null) return false;

            return controller.HasAnimationPlayer(Layer);

        }

        public void SetWeight(float weight)
        {

            var controller = Layer.GetMotionController(MotionControllerIndex);
            if (controller == null) return;

            controller.SetWeight(weight);

        }

        public float GetWeight()
        {

            var controller = Layer.GetMotionController(MotionControllerIndex);
            if (controller == null) return 0;

            return controller.GetWeight();

        }

        public float GetTime(float addTime = 0)
        {

            if (Layer == null) return 0;

            var controller = Layer.GetMotionController(MotionControllerIndex);
            if (controller == null) return 0;

            return controller.GetTime(Layer, addTime);

        }

        public float GetNormalizedTime(float addTime = 0)
        {

            if (Layer == null) return 0f;

            var controller = Layer.GetMotionController(MotionControllerIndex);
            if (controller == null) return 0f;

            return controller.GetNormalizedTime(Layer, addTime);

        }

        public float GetPreviousTime()
        {

            if (Layer == null) return 0;

            var controller = Layer.GetMotionController(MotionControllerIndex);
            if (controller == null) return 0;

            return controller.GetPreviousTime(Layer);

        }

        public float GetPreviousNormalizedTime()
        {

            if (Layer == null) return 0f;

            var controller = Layer.GetMotionController(MotionControllerIndex);
            if (controller == null) return 0f;

            return controller.GetPreviousNormalizedTime(Layer);

        }

        public void SetTime(float time, bool resetFlags = true)
        {

            if (Layer == null) return;

            var controller = Layer.GetMotionController(MotionControllerIndex);
            if (controller == null) return;

            controller.ForceSetLoopMode(Layer, controller.GetLoopMode(Layer));
            controller.SetTime(Layer, time, resetFlags);

        }

        public void SetNormalizedTime(float normalizedTime, bool resetFlags = true)
        {

            if (Layer == null) return; 

            var controller = Layer.GetMotionController(MotionControllerIndex);
            if (controller == null) return;

            controller.ForceSetLoopMode(Layer, controller.GetLoopMode(Layer));
            controller.SetNormalizedTime(Layer, normalizedTime, resetFlags);

        }

        public void ForceSetPreviousTime(float time)
        {

            if (Layer == null) return;

            var controller = Layer.GetMotionController(MotionControllerIndex);
            if (controller is not CustomMotionController cmc) return;

            cmc.ForceSetPreviousTime(Layer, time);

        }

        public void ForceSetPreviousNormalizedTime(float normalizedTime)
        {

            if (Layer == null) return;

            var controller = Layer.GetMotionController(MotionControllerIndex);
            if (controller is not CustomMotionController cmc) return;

            cmc.ForceSetPreviousNormalizedTime(Layer, normalizedTime);

        }

        public float GetEstimatedDuration()
        {

            if (Layer == null) return 0;

            var controller = Layer.GetMotionController(MotionControllerIndex);
            if (controller == null) return 0;
            return controller.GetDuration(Layer);

        }

        public void Reset()
        {

            ResetTransition();

            if (Layer == null) return;

            var controller = Layer.GetMotionController(MotionControllerIndex);
            if (controller == null) return;

            controller.Reset(Layer);
            controller.ForceSetLoopMode(Layer, controller.GetLoopMode(Layer));

        }

        public void RestartAnims()
        {

            if (Layer == null) return;

            var controller = Layer.GetMotionController(MotionControllerIndex);
            if (controller == null) return;

            controller.ForceSetLoopMode(Layer, controller.GetLoopMode(Layer));
            controller.SetNormalizedTime(Layer, 0, true);

        }

        public void ResyncAnims()
        {

            if (Layer == null) return;

            var controller = Layer.GetMotionController(MotionControllerIndex);
            if (controller == null) return;

            controller.ForceSetLoopMode(Layer, controller.GetLoopMode(Layer));
            controller.SetNormalizedTime(Layer, controller.GetNormalizedTime(Layer));

        }

        [SerializeField]
        public Transition[] transitions;
        //protected Transition[] transitions;
        [NonSerialized]
        protected int[] transitionIndicesOrdered;
        protected void EnsureTransitionIndicesAreOrdered()
        {
            if (transitionIndicesOrdered == null && transitions != null)
            {
                transitionIndicesOrdered = transitions.OrderByDescending(t => t == null ? int.MinValue : t.priority).Select(t => Array.IndexOf(transitions, t)).ToArray();
            }
        }
        public Transition[] Transitions
        {
            get => transitions;
            set => transitions = value;
        }
        public int TransitionCount => transitions != null ? transitions.Length : 0;
        public Transition GetTransition(int index)
        {
            if (transitions == null || index < 0 || index >= transitions.Length) return null;
            return GetTransitionUnsafe(index);
        }
        public Transition GetTransitionUnsafe(int index) => transitions[index];
        public int GetTransitionIndex(Transition transition)
        {
            if (transitions == null || transition == null) return -1;

            for (int i = 0; i < transitions.Length; i++)
            {
                if (ReferenceEquals(transitions[i], transition)) return i;
            }

            return -1;
        }
        public int FindTransitionIndex(string transitionName)
        {
            if (transitions == null || string.IsNullOrWhiteSpace(transitionName)) return -1;
            for (int i = 0; i < transitions.Length; i++)
            {
                if (transitions[i] != null && transitions[i].name == transitionName) return i;
            }
            return -1;
        }
        public Transition FindTransition(string transitionName)
        {
            var index = FindTransitionIndex(transitionName);
            return index >= 0 ? transitions[index] : null;
        }
        public bool TryFindTransition(string transitionName, out Transition transition)
        {
            var index = FindTransitionIndex(transitionName);
            if (index >= 0)
            {
                transition = transitions[index];
                return true;
            }
            transition = null;
            return false;
        }

        public int AddTransition(Transition t)
        {
            if (t == null) return -1;

            if (transitions == null)
            {
                transitions = new Transition[1];
            }
            else
            {
                Array.Resize(ref transitions, transitions.Length + 1);
            }

            int index = transitions.Length - 1;
            transitions[index] = t;
            return index;
        }
        
        [NonSerialized]
        protected Transition m_activeTransition;
        public Transition ActiveTransition => m_activeTransition;

        [NonSerialized]
        protected Transition m_lastCancelledTransition;
        public Transition LastCancelledTransition => m_lastCancelledTransition;

        [NonSerialized]
        protected int m_transitionTarget;
        public int TransitionTarget => m_transitionTarget - 1;
        public int TransitionTargetIndex => m_transitionTarget - 1;

        [NonSerialized]
        protected float m_transitionTime;
        public float TransitionTime => m_transitionTime;

        [NonSerialized]
        protected float m_transitionTimeLeft;
        public float TransitionTimeLeft => m_transitionTimeLeft;

        [NonSerialized]
        protected float m_transitionTargetFreezeTime;
        public float TransitionTargetFreezeTime => m_transitionTargetFreezeTime;

        [NonSerialized]
        protected bool m_transitionCancelled;
        public bool TransitionCancelled => m_transitionCancelled;

        [NonSerialized]
        protected float m_transitionCancelTime;
        public float TransitionCancelTime => m_transitionCancelTime;

        [NonSerialized]
        protected float m_transitionCancelTimeLeft;
        public float TransitionCancelTimeLeft => m_transitionCancelTimeLeft;

        [NonSerialized]
        protected float m_transitionCancelTimeRatio;
        public float TransitionCancelTimeRatio => m_transitionCancelTimeRatio;

        public float TransitionProgress => 1f - math.saturate(TransitionTimeLeft / TransitionTime);

        [NonSerialized]
        protected TransitionParameterStateChange[] m_transitionParameterStateChanges;
        public TransitionParameterStateChange[] TransitionParameterStateChanges => m_transitionParameterStateChanges;

        public bool IsTransitioning => m_transitionTarget > 0;

        private static readonly Transition DefaultTransition = new Transition() { name = "Default", disableAutoTransition = true, transitionTime = 1f };
         
        public void ForceStartDefaultTransition(int targetStateIndex, float transitionTimeLeft, bool cancelCurrentTransition = false) => ForceStartTransition(DefaultTransition, targetStateIndex, transitionTimeLeft, cancelCurrentTransition, false); 

        public void ForceStartTransition(int transitionIndex, bool cancelCurrentTransition = false) => ForceStartTransition(transitionIndex, -1, -1f, cancelCurrentTransition);
        public void ForceStartTransition(int transitionIndex, float transitionTimeLeft, bool cancelCurrentTransition = false, bool useTransitionNativeMaxTime = true) => ForceStartTransition(transitionIndex, -1, transitionTimeLeft, cancelCurrentTransition, useTransitionNativeMaxTime);
        public void ForceStartTransition(int transitionIndex, int targetStateIndex, float transitionTimeLeft, bool cancelCurrentTransition = false, bool useTransitionNativeMaxTime = true) 
        {
            if (transitions == null || transitionIndex < 0 || transitionIndex >= transitions.Length) return;
            ForceStartTransition(transitions[transitionIndex], targetStateIndex, transitionTimeLeft, cancelCurrentTransition, useTransitionNativeMaxTime);
        }

        public void ForceStartTransition(Transition transition, bool cancelCurrentTransition = false) => ForceStartTransition(transition, -1, -1f, cancelCurrentTransition);
        public void ForceStartTransition(Transition transition, float transitionTimeLeft, bool cancelCurrentTransition = false, bool useTransitionNativeMaxTime = true) => ForceStartTransition(transition, -1, transitionTimeLeft, cancelCurrentTransition, useTransitionNativeMaxTime);
        public void ForceStartTransition(Transition transition, int targetStateIndex, float transitionTimeLeft, bool cancelCurrentTransition = false, bool useTransitionNativeMaxTime = true)
        {
            if ((IsTransitioning && !cancelCurrentTransition) || transition == null) return; 

            var layer = Layer;
            if (layer == null) return;

            if (targetStateIndex < 0) targetStateIndex = transition.targetStateIndex;
            var target = layer.GetState(targetStateIndex);
            if (target == null || ReferenceEquals(target, this)) return;

            // Forcing a transition over one in flight would orphan the old target at a partial weight/time (it would never be zeroed or reset), so collapse it first.
            if (IsTransitioning)
            {
                int oldTargetIndex = TransitionTarget;
                if (oldTargetIndex != targetStateIndex)
                {
                    var oldTarget = layer.GetState(oldTargetIndex);
                    if (oldTarget != null && !ReferenceEquals(oldTarget, this))
                    {
                        oldTarget.SetWeight(0f);
                        if (oldTarget is CustomAnimationLayerState oldAls) oldAls.ResetTransitionChain();
                        else oldTarget.Reset();
                    }
                }
            }
            // A target that is itself mid-transition (nested chain) must be collapsed before being entered.
            if (target is CustomAnimationLayerState targetAls && targetAls.IsTransitioning) targetAls.ResetTransitionChain();

            var controller = layer.GetMotionController(MotionControllerIndex);
            if (transitionTimeLeft < 0f) transitionTimeLeft = transition.GetTransitionTime(controller.GetNormalizedTime(layer));

            ResetTransition();

            target.Reset();
            transition.lastTriggerFrame = Time.frameCount;
            m_activeTransition = transition;
            m_transitionTarget = targetStateIndex + 1;//transition.targetStateIndex + 1;
            m_transitionTime = useTransitionNativeMaxTime ? transition.transitionTime : transitionTimeLeft;
            m_transitionTimeLeft = transitionTimeLeft;
            m_transitionParameterStateChanges = transition.parameterStateChanges; 

            transition.Start(layer, controller, target, -1);
        }
        public void CancelTransition(float cancelTime, float cancelTimeLeft)
        {
            if (m_transitionCancelled || !IsTransitioning) return;

            m_lastCancelledTransition = m_activeTransition;
            m_transitionCancelled = true;
            m_transitionCancelTime = cancelTime;
            m_transitionCancelTimeLeft = cancelTimeLeft;
            m_transitionCancelTimeRatio = m_transitionTime > m_transitionTimeLeft ? cancelTimeLeft / (m_transitionTime - m_transitionTimeLeft) : 0f;
            m_transitionParameterStateChanges = ActiveTransition.cancellationParameterStateChanges;

            if (m_transitionParameterStateChanges != null)
            {
                foreach (var parameterStateChange in m_transitionParameterStateChanges)
                {
                    if (!parameterStateChange.applyAtEnd && !string.IsNullOrWhiteSpace(parameterStateChange.parameterName))
                    {
                        var parameter = Layer.Animator.FindParameter(parameterStateChange.parameterName);
                        if (parameter == null) continue;

                        parameter.SetValue(parameterStateChange.parameterValue);
                    }
                }
            }
        }
        public void UncancelTransition() => UncancelTransition(m_transitionTime, m_transitionTimeLeft);
        public void UncancelTransition(float transitionTime, float transitionTimeLeft)
        {
            if (!m_transitionCancelled || !IsTransitioning) return;

            m_transitionCancelled = false;
            ActiveTransition.lastTriggerFrame = Time.frameCount;
            m_transitionTimeLeft = transitionTimeLeft;
            m_transitionTime = transitionTime;
            m_transitionParameterStateChanges = ActiveTransition.parameterStateChanges;

            if (m_transitionParameterStateChanges != null)
            {
                foreach (var parameterStateChange in m_transitionParameterStateChanges)
                {
                    if (!parameterStateChange.applyAtEnd && !string.IsNullOrWhiteSpace(parameterStateChange.parameterName))
                    {
                        var parameter = Layer.Animator.FindParameter(parameterStateChange.parameterName);
                        if (parameter == null) continue;

                        parameter.SetValue(parameterStateChange.parameterValue);
                    }
                }
            }
        }

        public void ResetTransition()
        {

            m_transitionTarget = 0;
            m_transitionTime = 0f;
            m_transitionTimeLeft = 0f;
            m_transitionTargetFreezeTime = 0f;

            m_transitionCancelled = false;
            m_transitionCancelTime = 0f;
            m_transitionCancelTimeLeft = 0f;
            m_transitionCancelTimeRatio = 0f;
            
            if (transitions != null)
            {
                foreach (var transition in transitions) transition.lastTriggerFrame = ReferenceEquals(m_activeTransition, transition) ? swole.Frame + 1 : swole.Frame;
            }

            m_activeTransition = null;

        }
        public void CompletedTransition()
        {
            if (!TransitionCancelled) m_lastCancelledTransition = null; 

            if (transitions != null)
            {
                foreach (var transition in transitions) transition.lastTriggerFrame = ReferenceEquals(m_activeTransition, transition) ? swole.Frame + 1 : swole.Frame; 
            }
        }

        /// <summary>
        /// Resets this state, but first recursively collapses any nested transition chain (a transition target that is itself mid-transition to another state, via allowMultiTransition).
        /// Without this, forcibly resetting a state that owns an in-progress nested transition would abandon its target state at whatever weight it was blended to, orphaning it (never getting zeroed) since only the immediate target's weight is touched by the caller.
        /// </summary>
        public void ResetTransitionChain()
        {
            if (IsTransitioning)
            {
                var nestedTarget = Layer == null ? null : Layer.GetState(TransitionTarget);
                if (nestedTarget != null && !ReferenceEquals(nestedTarget, this))
                {
                    nestedTarget.SetWeight(0f);
                    if (nestedTarget is CustomAnimationLayerState nestedAls)
                    {
                        nestedAls.ResetTransitionChain();
                    }
                    else
                    {
                        nestedTarget.Reset();
                    }
                }
            }

            Reset();
        }
        
        protected string puppetConfigurationContentPath;
        [SerializeField]
        protected SwolePuppetMuscleConfigurationAsset puppetConfigurationAsset;
        [NonSerialized]
        protected SwolePuppetMuscleConfigurationObject puppetConfiguration;
        public SwolePuppetMuscleConfigurationObject PuppetConfiguration
        {
            get
            {
                if (puppetConfiguration == null)
                {
                    if (puppetConfigurationAsset != null) 
                    { 
                        puppetConfiguration = puppetConfigurationAsset.Content; 
                    } 
                    else if (!string.IsNullOrWhiteSpace(puppetConfigurationContentPath)) 
                    {
                        // TODO: Find configuration in content library using given path.
                    }
                }

                return puppetConfiguration;
            }

            set
            {
                puppetConfiguration = value;
            }
        }

        public int Progress(TransformHierarchy nextHierarchy, bool nextIsAdditiveOrBlended, bool skipAnimationIfDerivative, float deltaTime, ref JobHandle jobHandle, bool useMultithreading, ref bool isFinal, bool previousWasFinal = false, bool allowTransitions = false, bool isTransitionTarget = false, bool canLoop = true, TransformHierarchy localHierarchy = null)
        {

            if (Layer == null) return Index;

            var controller = Layer.GetMotionController(MotionControllerIndex);
            bool hasController = false;
            CustomMotionController cmc = null;
            if (controller is CustomMotionController) 
            { 
                hasController = true;
                cmc = (CustomMotionController)controller; 
            }

            if (localHierarchy == null) localHierarchy = hasController ? Layer.Animator.GetTransformHierarchy(controller.GetLongestHierarchyIndex(Layer)) : null;

            bool isDerivativeHierarchy = !isFinal && nextHierarchy != null && localHierarchy != null && localHierarchy.IsDerivative(nextHierarchy);
            isFinal = !isDerivativeHierarchy;

            bool skipAnimation = isDerivativeHierarchy && (skipAnimationIfDerivative || !nextIsAdditiveOrBlended); // Saves some processing by skipping animations that will just get overridden.

            int nextIndex = Index; 

            float localSpeedMul = 1f;
            if (allowTransitions)
            {

                if (TransitionTarget < 0)
                {

                    if (transitions != null)
                    {

                        float normalizedTimeNext = math.saturate(GetNormalizedTime(deltaTime)); 

                        EnsureTransitionIndicesAreOrdered(); 
                        //foreach (Transition transition in transitions)
                        for(int a = 0; a < transitionIndicesOrdered.Length; a++)
                        {
                            var transition = transitions[transitionIndicesOrdered[a]];
                            if (transition == null || transition.targetStateIndex < 0) continue; 

                            var target = Layer.GetState(transition.targetStateIndex);
                            if (target == null || ReferenceEquals(target, this) || ReferenceEquals(m_lastCancelledTransition, transition)) continue; 
                            
                            if (target.IsTransitioning)
                            {
                                //if (isTransitionTarget) continue; 
                                continue;
                            }

                            if (!transition.HasMetRequirements(Layer, swole.Frame, normalizedTimeNext, controller, Layer.GetMotionController(target.MotionControllerIndex), isTransitionTarget, out float transitionTime, out int pathIndex))
                            {
                                //Debug.Log($"CANT TRANSITION TO {target.Name}: REQUIREMENTS NOT MET");
                                continue; 
                            }

                            target.Reset();
                            //Debug.Log($"Transitioning from {Name} (NT: {normalizedTimeNext}) to {target.Name} (frame_diff:{swole.Frame - transition.lastTriggerFrame}) (SET TARGET NORMALIZED TIME ? {transition.setTargetNormalizedTime})");  
                            
                            m_activeTransition = transition;
                            m_transitionTarget = transition.targetStateIndex + 1;
                            m_transitionTime = m_transitionTimeLeft = transitionTime;
                            m_transitionParameterStateChanges = transition.parameterStateChanges;
                            m_lastCancelledTransition = null; 

                            transition.Start(Layer, controller, target, pathIndex);  
                            
                            break;

                        }

                    }

                }

                int transitionTarget = TransitionTarget;
                if (transitionTarget >= 0)
                {
                    var target = Layer.GetState(transitionTarget);
                    if (ReferenceEquals(target, this))
                    {

                        swole.LogError($"Layer state '{name}' tried to transition to itself!");

                        ResetTransition();

                    }
                    else if (target is not CustomAnimationLayerState csm)
                    {

                        ResetTransition();  
                        
                    }
                    else
                    {

                        bool isFirstFrameOfTransition = !TransitionCancelled && m_transitionTimeLeft == m_transitionTime;
                        if (isFirstFrameOfTransition) m_transitionTargetFreezeTime = target.GetTime();
                        if (TransitionCancelled)
                        {
                            m_transitionCancelTimeLeft -= deltaTime;
                            m_transitionTimeLeft = Mathf.Min(m_transitionTimeLeft + deltaTime * TransitionCancelTimeRatio, m_transitionTime);  
                        } 
                        else
                        {
                            m_transitionTimeLeft -= deltaTime;
                        }

                        if ((TransitionCancelled ? TransitionCancelTimeLeft : TransitionTimeLeft) <= 0)
                        {

                            if (TransitionParameterStateChanges != null)
                            {
                                foreach (var parameterStateChange in TransitionParameterStateChanges)
                                {
                                    if (parameterStateChange.applyAtEnd && !string.IsNullOrWhiteSpace(parameterStateChange.parameterName))
                                    {
                                        var parameter = Layer.Animator.FindParameter(parameterStateChange.parameterName);
                                        if (parameter == null) continue;

                                        parameter.SetValue(parameterStateChange.parameterValue);
                                    }
                                }
                            }

                            if (TransitionCancelled)
                            {
                                //Debug.Log($"Finished cancelling transition from {Name} (nt:{GetNormalizedTime()}) to {target.Name} (nt:{target.GetNormalizedTime()})"); 
                                if (!isTransitionTarget) SetWeight(1f);
                                target.SetWeight(0f);
                                csm.ResetTransitionChain();

                                CompletedTransition();
                                target.CompletedTransition();
                                ResetTransition();

                                // Do NOT return early here. Ownership of the active state remains with 'this' (nextIndex == Index),
                                // so it must still fall through to the final cmc.Progress(...) call below to have its pose evaluated
                                // this frame. Returning early (as previously done) skipped that call entirely, leaving the active
                                // state's animation un-progressed for one frame right as the cancellation completed, causing a
                                // visible T-pose glitch.
                            }
                            else
                            {
                                //Debug.Log($"Finished transition from {Name} (nt:{GetNormalizedTime()}) to {target.Name} (nt:{target.GetNormalizedTime()})");
                                target.SetWeight(isTransitionTarget ? GetWeight() : 1f);
                                SetWeight(0f);
                                if (hasController) controller.Reset(Layer);

                                bool isFinal_ = isFinal;
                                nextIndex = csm.Progress(localHierarchy, true, skipAnimation, deltaTime, ref jobHandle, useMultithreading, ref isFinal_, previousWasFinal, allowTransitions, isTransitionTarget, canLoop);

                                CompletedTransition();
                                target.CompletedTransition();
                                ResetTransition();

                                //Debug.Log($"Ended transition for {Name} (nt:{GetNormalizedTime()})"); 

                                // Ownership moved to 'target' (nextIndex), whose controller was already progressed via the
                                // recursive csm.Progress(...) call above, so it's safe (and necessary) to return early here
                                // without reaching the bottom cmc.Progress(...) call for 'this'.
                                return nextIndex;
                            }

                        } 
                        else
                        {

                            float normalizedTimeNext = math.saturate(GetNormalizedTime(deltaTime));

                            float transitionMix = TransitionProgress;
                            float transitionMixWeight = isTransitionTarget ? GetWeight() : 1f;

                            var activeTransition = ActiveTransition; 
                            if (activeTransition != null)
                            {
                                if (TransitionCancelled)
                                {
                                    if (activeTransition.allowCancellationRevert && activeTransition.HasMetRequirements(Layer, swole.Frame, normalizedTimeNext, controller, Layer.GetMotionController(target.MotionControllerIndex), isTransitionTarget, out float transitionTime, out int pathIndex))
                                    {
                                        activeTransition.ConsumeTriggers(pathIndex, Layer); 
                                        //Debug.Log($"Re-transitioning from {Name} (NT: {normalizedTimeNext}) to {target.Name}"); 
                                        UncancelTransition();
                                    }
                                }
                                else
                                {
                                    if (activeTransition.CanCancel(Layer, normalizedTimeNext, transitionMix, controller, Layer.GetMotionController(target.MotionControllerIndex), out float cancellationTimeMultiplier, out int pathIndex))
                                    {
                                        activeTransition.ConsumeCancellationTriggers(pathIndex, Layer); 
                                        //Debug.Log($"Initiating cancellation of transition from {Name} (NT: {normalizedTimeNext}) to {target.Name}");
                                        float cancelTime = (m_transitionTime - m_transitionTimeLeft) * cancellationTimeMultiplier; 
                                        CancelTransition(cancelTime, cancelTime); 
                                    }
                                }
                            }

                            //Debug.Log($"Transitioning from {Name} (nt:{normalizedTimeNext}) to {target.Name} (nt:{target.GetNormalizedTime()}) (mix:{transitionMix})");
                            SetWeight((1f - transitionMix) * transitionMixWeight); 
                            target.SetWeight(transitionMix * transitionMixWeight); 

                            if (!TransitionCancelled && activeTransition != null && m_transitionTimeLeft > m_transitionTime - activeTransition.GetScaledFreezeTargetTransitionTimeThresholdByTransitionTime(m_transitionTime)) target.SetTime(m_transitionTargetFreezeTime);  

                            float targetSpeedMul = 1f;
                            float targetDeltaTime = deltaTime;
                            if (activeTransition != null && hasController)
                            {
                                var targetController = Layer.GetMotionController(target.MotionControllerIndex);
                                bool hasTargetController = false;
                                CustomMotionController target_cmc = null;
                                if (targetController is CustomMotionController)
                                {
                                    hasTargetController = true;
                                    target_cmc = (CustomMotionController)targetController;
                                }

                                if (hasTargetController)
                                {
                                    if (activeTransition.syncTargetSpeedToLocalSpeed)
                                    { 
                                        float localSpeed = cmc.GetSpeed(Layer);
                                        float targetSpeed = target_cmc.GetSpeed(Layer);
                                        float speedMul = (localSpeed != 0 && targetSpeed != 0 && Mathf.Sign(targetSpeed) == Mathf.Sign(localSpeed)) ? (targetSpeed / localSpeed) : 1f;
                                        localSpeedMul = Mathf.LerpUnclamped(1f, speedMul * activeTransition.localSyncSpeedMultiplier, transitionMix);
                                        targetSpeedMul = Mathf.LerpUnclamped(speedMul * activeTransition.targetSyncSpeedMultiplier, 1f, transitionMix);
                                    }

                                    targetDeltaTime = deltaTime * targetSpeedMul;
                                    switch (activeTransition.timeSyncMode)
                                    {
                                        case Transition.TimeSyncMode.Local:
                                            {
                                                float prevT = GetPreviousNormalizedTime();
                                                float t = GetNormalizedTime(deltaTime * localSpeedMul);
                                                target.ForceSetPreviousNormalizedTime(prevT);
                                                target.SetNormalizedTime(t, false);
                                                targetDeltaTime = 0f;
                                            }
                                            break;

                                        case Transition.TimeSyncMode.Target:
                                            {
                                                float prevT = target.GetPreviousNormalizedTime();
                                                float t = target.GetNormalizedTime(targetDeltaTime);
                                                ForceSetPreviousNormalizedTime(prevT);
                                                SetNormalizedTime(t, false);
                                                localSpeedMul = 0f;
                                            }
                                            break;

                                        case Transition.TimeSyncMode.Blend:
                                            {
                                                float prevLocalTime = GetPreviousNormalizedTime();
                                                float localTime = GetNormalizedTime(deltaTime * localSpeedMul);
                                                float prevTargetTime = target.GetPreviousNormalizedTime();
                                                float targetTime = target.GetNormalizedTime(targetDeltaTime);
                                                float prevBlendedTime = Mathf.LerpUnclamped(prevLocalTime + (prevTargetTime - prevLocalTime >= 0.5f ? 1f : 0f), prevTargetTime + (prevLocalTime - prevTargetTime >= 0.5f ? 1f : 0f), transitionMix);
                                                float blendedTime = Mathf.LerpUnclamped(localTime + (targetTime - localTime >= 0.5f ? 1f : 0f), targetTime + (localTime - targetTime >= 0.5f ? 1f : 0f), transitionMix);
                                                ForceSetPreviousNormalizedTime(prevBlendedTime);
                                                SetNormalizedTime(controller.GetLoopMode(Layer) == AnimationLoopMode.Loop ? Maths.wrap(blendedTime, 1f) : blendedTime, false);
                                                target.ForceSetPreviousNormalizedTime(prevBlendedTime);
                                                target.SetNormalizedTime(targetController.GetLoopMode(Layer) == AnimationLoopMode.Loop ? Maths.wrap(blendedTime, 1f) : blendedTime, false);
                                                targetDeltaTime = 0f;
                                                localSpeedMul = 0f;
                                            }
                                            break; 
                                    }
                                }
                            }
                            
                            //                                                                                                                                                                      v --- if the current active state has a motion controller, then it will be progressed after this progress call, so don't allow finalization 
                            bool isFinal_ = isFinal && !hasController;
                            m_transitionTarget = csm.Progress(localHierarchy, true, skipAnimation, targetDeltaTime, ref jobHandle, useMultithreading, ref isFinal_, previousWasFinal, m_activeTransition == null || !m_activeTransition.allowMultiTransition ? false : true, true, canLoop) + 1;
                            if (activeTransition != null && !TransitionCancelled && activeTransition.GetScaledFreezeTargetTransitionTimeThresholdByTransitionTime(m_transitionTime) > 0f && transitionTarget != m_transitionTarget - 1 && m_transitionTarget > 0) m_transitionTargetFreezeTime = Layer.GetState(m_transitionTarget - 1).GetTime(); 
                        }

                    }

                }

            }
            else
            {

                ResetTransition();

            }

            if (hasController && !skipAnimation)
            {

                cmc.Progress(Layer, deltaTime * localSpeedMul, ref jobHandle, useMultithreading, isFinal, previousWasFinal, canLoop);

                //Debug.Log($"Progressing {Name} : {cmc.GetNormalizedTime(Layer)} : {localSpeedMul} {IsTransitioning} {allowTransitions} : {GetWeight()} "); 

            }

            return nextIndex;

        }

        public HashSet<int> GetActiveParameters(HashSet<int> parameterIndices = null) => GetActiveParameters(Layer, parameterIndices);
        public HashSet<int> GetActiveParameters(IAnimationLayer layer, HashSet<int> parameterIndices = null)
        {
            if (parameterIndices == null) parameterIndices = new HashSet<int>();

            if (layer == null) return parameterIndices;

            var controller = layer.GetMotionController(MotionControllerIndex);
            if (controller == null) return parameterIndices;

            controller.GetActiveParameters(Layer, parameterIndices);

            if (IsTransitioning)
            {
                var targetState = layer.GetState(TransitionTarget);
                if (targetState != null)
                {
                    var targetController = layer.GetMotionController(targetState.MotionControllerIndex);
                    if (targetController != null)
                    {
                        targetController.GetActiveParameters(Layer, parameterIndices);
                    }

                    var transition = ActiveTransition;
                    if (transition != null)
                    {
                        transition.GetActiveParameters(Layer, parameterIndices);
                    }
                }
            }
            else
            {
                if (transitions != null)
                {
                    foreach (var transition in transitions)
                    {
                        if (transition == null) continue;

                        if (transition.validRequirementPaths != null)
                        {
                            foreach(var path in transition.validRequirementPaths)
                            {
                                if (path.parameters != null)
                                {
                                    foreach(var p in path.parameters)
                                    {
                                        if (string.IsNullOrWhiteSpace(p.parameterName)) continue;

                                        var parameter = layer.Animator.FindParameterIndex(p.parameterName);
                                        if (parameter >= 0) parameterIndices.Add(parameter);
                                    }
                                }
                            }
                        }

                        if (transition.cancellationParameterStateChanges != null)
                        {
                            foreach (var parameterStateChange in transition.cancellationParameterStateChanges)
                            {
                                if (string.IsNullOrWhiteSpace(parameterStateChange.parameterName)) continue;

                                var parameter = layer.Animator.FindParameterIndex(parameterStateChange.parameterName);
                                if (parameter >= 0) parameterIndices.Add(parameter);
                            }
                        }
                    }
                }
            }

            return parameterIndices;
        }

        public float GetCurrentBoneHeight(string boneName) => GetBoneHeight(boneName, 0f);
        public float GetBoneHeight(string boneName, float timeOffset) => GetBoneHeight(boneName, timeOffset, out _);
        public float GetBoneHeight(string boneName, float timeOffset, out bool foundCurve)
        {
            foundCurve = false;

            var controller = Layer.GetMotionController(MotionControllerIndex);
            if (controller == null) return 0f;

            return controller.GetBoneHeight(Layer, boneName, timeOffset, out foundCurve);

        }
        public float GetBoneHeightAtTime(string boneName, float time) => GetBoneHeightAtTime(boneName, time, out _);
        public float GetBoneHeightAtTime(string boneName, float time, out bool foundCurve)
        {
            foundCurve = false;

            var controller = Layer.GetMotionController(MotionControllerIndex);
            if (controller == null) return 0f;

            return controller.GetBoneHeightAtTime(Layer, boneName, time, out foundCurve); 

        }

        /// <summary>Samples where a bone would be (relative to <paramref name="space"/>) at this state's current time plus <paramref name="timeOffset"/> seconds.</summary>
        public bool TrySampleBonePosition(Transform bone, Transform space, float timeOffset, out Vector3 position, IReadOnlyDictionary<int, float> parameterOverrides = null)
        {
            position = default;
            if (Layer.GetMotionController(MotionControllerIndex) is not CustomMotionController controller) return false;
            return controller.TrySampleBonePosition(Layer, bone, space, timeOffset, out position, parameterOverrides);
        }

        public bool AffectsTransform(string transformName)
        {
            var controller = Layer.GetMotionController(MotionControllerIndex);
            if (controller == null) return false;

            return controller.AffectsTransform(Layer, transformName); 
        }

        public void NonAdditivePrepass(bool useMultithreading)
        {
            var layer = Layer; 

            var controller = Layer.GetMotionController(MotionControllerIndex);
            if (controller != null)
            {
                controller.NonAdditivePrepass(useMultithreading, layer);
            }

            if (IsTransitioning)
            {
                var targetState = layer.GetState(TransitionTarget);
                if (targetState != null)
                {
                    targetState.NonAdditivePrepass(useMultithreading);
                }
            }
        }

        public void ApplyPropertyStates()
        {
            var controller = Layer.GetMotionController(MotionControllerIndex);
            if (controller == null) return;

            controller.ApplyPropertyStates(Layer);

        }

    }
}

#endif