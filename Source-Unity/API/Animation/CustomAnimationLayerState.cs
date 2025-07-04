#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

using Swole.Animation;

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

            instance.transitions = transitions;

            instance.puppetConfigurationAsset = puppetConfigurationAsset;
            instance.puppetConfigurationContentPath = puppetConfigurationContentPath;
            instance.puppetConfiguration = puppetConfiguration;

            return instance;

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
        public int motionControllerIndex;
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

            if (Layer == null) return 0;

            var controller = Layer.GetMotionController(MotionControllerIndex);
            if (controller == null) return 0;

            return controller.GetNormalizedTime(Layer, addTime);

        }

        public void SetTime(float time)
        {

            if (Layer == null) return;

            var controller = Layer.GetMotionController(MotionControllerIndex);
            if (controller == null) return;

            controller.ForceSetLoopMode(Layer, controller.GetLoopMode(Layer));
            controller.SetTime(Layer, time);

        }

        public void SetNormalizedTime(float normalizedTime)
        {

            if (Layer == null) return; 

            var controller = Layer.GetMotionController(MotionControllerIndex);
            if (controller == null) return;

            controller.ForceSetLoopMode(Layer, controller.GetLoopMode(Layer));
            controller.SetNormalizedTime(Layer, normalizedTime);

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
            controller.SetNormalizedTime(Layer, 0);

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
        public Transition[] Transitions
        {
            get => transitions;
            set => transitions = value;
        }

        [NonSerialized]
        protected Transition m_activeTransition;
        public Transition ActiveTransition => m_activeTransition;

        [NonSerialized]
        protected int m_transitionTarget;
        public int TransitionTarget => m_transitionTarget - 1;

        [NonSerialized]
        protected float m_transitionTime;
        public float TransitionTime => m_transitionTime;

        [NonSerialized]
        protected float m_transitionTimeLeft;
        public float TransitionTimeLeft => m_transitionTimeLeft;

        [NonSerialized]
        protected bool m_transitionCancelled;
        public bool TransitionCancelled => m_transitionCancelled;

        public float TransitionProgress => 1f - Mathf.Clamp01(TransitionTimeLeft / TransitionTime);

        [NonSerialized]
        protected TransitionParameterStateChange[] m_transitionParameterStateChanges;
        public TransitionParameterStateChange[] TransitionParameterStateChanges => m_transitionParameterStateChanges;

        public bool IsTransitioning => m_transitionTarget > 0;

        public void ResetTransition()
        {

            m_transitionTarget = 0;
            m_transitionTime = 0;
            m_transitionTimeLeft = 0;
            m_transitionCancelled = false;

            if (transitions != null)
            {
                foreach (var transition in transitions) transition.lastTriggerFrame = Time.frameCount;
            }

        }
        public void CompletedTransition()
        {
            if (transitions != null)
            {
                foreach (var transition in transitions) transition.lastTriggerFrame = Time.frameCount; 
            }
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

        public int Progress(TransformHierarchy nextHierarchy, bool nextIsAdditiveOrBlended, bool skipAnimationIfDerivative, float deltaTime, ref JobHandle jobHandle, bool useMultithreading = true, bool isFinal = false, bool allowTransitions = false, bool isTransitionTarget = false, bool canLoop = true, TransformHierarchy localHierarchy = null)
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

                        foreach (Transition transition in transitions)
                        {

                            if (transition == null || transition.targetStateIndex < 0) continue; 

                            var target = Layer.GetState(transition.targetStateIndex);
                            if (target == null || target.IsTransitioning || (transition.mustBeFirstInChain && isTransitionTarget) || ReferenceEquals(target, this)) continue;   

                            if (!transition.HasMetRequirements(Layer, Time.frameCount, normalizedTimeNext, controller, Layer.GetMotionController(target.MotionControllerIndex), out float transitionTime)) continue;   

                            target.Reset();
                            //Debug.Log($"Transitioning from {Name} (NT: {normalizedTimeNext}) to {target.Name} (frame_diff:{Time.frameCount - transition.lastTriggerFrame}) (SET TARGET NORMALIZED TIME ? {transition.setTargetNormalizedTime})");  
                            transition.lastTriggerFrame = Time.frameCount;
                            m_activeTransition = transition;
                            m_transitionTarget = transition.targetStateIndex + 1;
                            m_transitionTime = m_transitionTimeLeft = transitionTime;
                            m_transitionParameterStateChanges = transition.parameterStateChanges;

                            if (transition.setLocalNormalizedTime)
                            {
                                if (controller != null) controller.SetNormalizedTime(Layer, transition.localNormalizedTime);
                            }
                            if (transition.setTargetNormalizedTime)
                            {
                                target.SetNormalizedTime(transition.GetTargetNormalizedTime(controller.GetNormalizedTime(Layer)));
                                //Debug.Log($"{Name} Set Normalized Time of {target.Name}::: {controller.GetNormalizedTime(Layer)} -> {target.GetNormalizedTime()}");    
                            }

                            if (m_transitionParameterStateChanges != null)
                            {
                                foreach(var parameterStateChange in m_transitionParameterStateChanges)
                                {
                                    if (!parameterStateChange.applyAtEnd && !string.IsNullOrWhiteSpace(parameterStateChange.parameterName))
                                    {
                                        var parameter = Layer.Animator.FindParameter(parameterStateChange.parameterName);
                                        if (parameter == null) continue;

                                        parameter.SetValue(parameterStateChange.parameterValue); 
                                    }
                                }
                            }
                            
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

                        m_transitionTimeLeft -= deltaTime;

                        if (TransitionTimeLeft <= 0)
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
                                SetWeight(1f);
                                target.SetWeight(0f);

                                var targetController = Layer.GetMotionController(target.MotionControllerIndex);  
                                if (targetController is CustomMotionController target_cmc)
                                {
                                    target_cmc.Reset(Layer);
                                }
                            }
                            else
                            {
                                //Debug.Log($"Finished transition from {Name} (nt:{GetNormalizedTime()}) to {target.Name} (nt:{target.GetNormalizedTime()})");
                                SetWeight(0f);
                                target.SetWeight(1f);
                                if (hasController) controller.Reset(Layer);

                                nextIndex = csm.Progress(localHierarchy, true, skipAnimation, deltaTime, ref jobHandle, useMultithreading, isFinal, true, false, canLoop);
                            }

                            CompletedTransition();
                            target.CompletedTransition();
                            ResetTransition();

                            //Debug.Log($"Ended transition for {Name} (nt:{GetNormalizedTime()})"); 

                            return nextIndex;

                        }
                        else
                        {

                            float normalizedTimeNext = math.saturate(GetNormalizedTime(deltaTime));

                            float transitionMix = TransitionProgress;

                            if (TransitionCancelled)
                            {
                                if (ActiveTransition != null && ActiveTransition.allowCancellationRevert && ActiveTransition.HasMetRequirements(Layer, Time.frameCount, normalizedTimeNext, controller, Layer.GetMotionController(target.MotionControllerIndex), out float transitionTime))
                                {
                                    //Debug.Log($"Re-transitioning from {Name} (NT: {normalizedTimeNext}) to {target.Name}"); 
                                    m_transitionCancelled = false;
                                    ActiveTransition.lastTriggerFrame = Time.frameCount;
                                    m_transitionTimeLeft = m_transitionTime - m_transitionTimeLeft;
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
                            } 
                            else
                            {
                                if (ActiveTransition != null && ActiveTransition.CanCancel(Layer, normalizedTimeNext, controller, Layer.GetMotionController(target.MotionControllerIndex), out float cancellationTimeMultiplier))
                                {
                                    //Debug.Log($"Initiating cancellation of transition from {Name} (NT: {normalizedTimeNext}) to {target.Name}");
                                    m_transitionCancelled = true;
                                    m_transitionTime = m_transitionTimeLeft = (m_transitionTime - m_transitionTimeLeft) * cancellationTimeMultiplier;
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
                            }

                            if (TransitionCancelled)
                            {
                                //Debug.Log($"Cancelling transition from {Name} (nt:{normalizedTimeNext}) to {target.Name} (nt:{target.GetNormalizedTime()}) (mix:{transitionMix})");  
                                SetWeight(transitionMix);
                                target.SetWeight(1f - transitionMix);
                            } 
                            else
                            {
                                //Debug.Log($"Transitioning from {Name} (nt:{normalizedTimeNext}) to {target.Name} (nt:{target.GetNormalizedTime()}) (mix:{transitionMix})");
                                SetWeight(1f - transitionMix);
                                target.SetWeight(transitionMix); 
                            }

                            float targetSpeedMul = 1f;
                            if (ActiveTransition != null && ActiveTransition.syncTargetSpeedToLocalSpeed && hasController)
                            {
                                var targetController = Layer.GetMotionController(target.MotionControllerIndex);
                                bool hasTargetController = false;
                                CustomMotionController target_cmc = null;
                                if (targetController is CustomMotionController)
                                {
                                    hasTargetController = true;
                                    target_cmc = (CustomMotionController)controller;
                                }

                                if (hasTargetController)
                                {
                                    float localSpeed = cmc.GetSpeed(Layer);
                                    float targetSpeed = target_cmc.GetSpeed(Layer);
                                    float speedMul = (localSpeed != 0 && targetSpeed != 0 && Mathf.Sign(targetSpeed) == Mathf.Sign(localSpeed)) ? (targetSpeed / localSpeed) : 1f;
                                    localSpeedMul = Mathf.LerpUnclamped(1f, speedMul * ActiveTransition.localSyncSpeedMultiplier, transitionMix); 
                                    targetSpeedMul = Mathf.LerpUnclamped(speedMul * ActiveTransition.targetSyncSpeedMultiplier, 1f, transitionMix);     
                                }
                            }

                            //                                                                                                           v --- if the current active state has a motion controller, then it will be progressed after this progress call, so don't allow finalization 
                            m_transitionTarget = csm.Progress(localHierarchy, true, skipAnimation, deltaTime * targetSpeedMul, ref jobHandle, useMultithreading, isFinal && !hasController, m_activeTransition == null || !m_activeTransition.allowMultiTransition ? false : true, true, canLoop) + 1;
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

                cmc.Progress(Layer, deltaTime * localSpeedMul, ref jobHandle, useMultithreading, isFinal, canLoop);   

            }

            return nextIndex;

        }

    }
}

#endif