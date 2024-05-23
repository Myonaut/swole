#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using Unity.Jobs;
using UnityEngine;

using Swole.Animation;

namespace Swole.API.Unity.Animation
{

    /// <summary>
    /// Handles a single motion controller and any transitions between other state machines.
    /// </summary>
    [Serializable]
    public class CustomStateMachine : IAnimationStateMachine
    {

        public CustomStateMachine NewInstance(IAnimationLayer layer, int newMotionControllerIndex = -1)
        {

            var instance = new CustomStateMachine(); 

            instance.m_layer = layer;

            instance.name = name;
            instance.index = index;
            instance.motionControllerIndex = newMotionControllerIndex >= 0 ? newMotionControllerIndex : motionControllerIndex;

            instance.transitions = transitions;

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

        [SerializeField, Tooltip("This index is local to the motion controller identifiers array in the owning layer. If the layer and machine have been instantiated at runtime, then it's local to the layer's motion controller array instead.")]
        public int motionControllerIndex;
        //protected int motionControllerIndex;
        /// <summary>
        /// This index is local to the motion controller identifiers array in the owning layer. If the layer and machine have been instantiated at runtime, then it's local to the layer's motion controller array instead.
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
        protected int m_transitionTarget;
        public int TransitionTarget => m_transitionTarget - 1;

        [NonSerialized]
        protected float m_transitionTime;
        public float TransitionTime => m_transitionTime;

        [NonSerialized]
        protected float m_transitionTimeLeft;
        public float TransitionTimeLeft => m_transitionTimeLeft;

        public void ResetTransition()
        {

            m_transitionTarget = 0;
            m_transitionTime = 0;
            m_transitionTimeLeft = 0;

        }

        public int Progress(TransformHierarchy nextHierarchy, bool nextIsAdditiveOrBlended, bool skipAnimationIfDerivative, float deltaTime, ref JobHandle jobHandle, bool useMultithreading = true, bool isFinal = false, bool allowTransitions = false, bool canLoop = true, TransformHierarchy localHierarchy = null)
        {

            if (Layer == null) return index;

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

            bool skipAnimation = isDerivativeHierarchy && (skipAnimationIfDerivative || !nextIsAdditiveOrBlended); // Saves some processing by skipping animations that will just get overridden.

            int nextIndex = index;

            if (allowTransitions)
            {

                if (TransitionTarget < 0)
                {

                    if (transitions != null)
                    {

                        float normalizedTimeNext = GetNormalizedTime(deltaTime);

                        foreach (Transition transition in transitions)
                        {

                            if (transition == null || transition.targetStateIndex < 0) continue;

                            if (!transition.HasPath(Layer.Animator, normalizedTimeNext)) continue;

                            var target = Layer.GetStateMachine(m_transitionTarget);
                            if (target == null) continue;

                            target.RestartAnims();

                            m_transitionTarget = transition.targetStateIndex + 1;
                            m_transitionTime = m_transitionTimeLeft = transition.transitionTime;

                            break;

                        }

                    }

                }

                if (TransitionTarget >= 0)
                {

                    var target = Layer.GetStateMachine(TransitionTarget);
                    if (target is not CustomStateMachine csm)
                    {

                        ResetTransition();

                    }
                    else
                    {

                        m_transitionTimeLeft -= deltaTime;

                        if (TransitionTimeLeft <= 0)
                        {

                            SetWeight(0);
                            target.SetWeight(1);

                            nextIndex = csm.Progress(localHierarchy, true, skipAnimation, deltaTime, ref jobHandle, useMultithreading, isFinal && !hasController, true, canLoop);

                            ResetTransition();

                            return nextIndex;

                        }
                        else
                        {

                            float transitionMix = TransitionTime / TransitionTime;

                            SetWeight(1 - transitionMix);
                            target.SetWeight(transitionMix);

                            csm.Progress(localHierarchy, true, skipAnimation, deltaTime, ref jobHandle, useMultithreading, isFinal && !hasController, false, canLoop);

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

                cmc.Progress(Layer, deltaTime, ref jobHandle, useMultithreading, !isDerivativeHierarchy, canLoop);

            }

            return nextIndex;

        }

    }
}

#endif