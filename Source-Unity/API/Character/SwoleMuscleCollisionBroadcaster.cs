#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;


#if SWOLE_ROOTMOTION
using RootMotion;
using RootMotion.Dynamics;
#endif

namespace Swole.API.Unity.Animation
{
#if SWOLE_ROOTMOTION
    public class SwoleMuscleCollisionBroadcaster : MuscleCollisionBroadcaster
#else
    public class SwoleMuscleCollisionBroadcaster : MonoBehaviour
#endif
    {
        //protected List<Collider> colliders = new List<Collider>(); 

        protected virtual void Awake()
        {
            /*if (colliders == null) colliders = new List<Collider>();
            colliders.Clear();
            colliders.AddRange(GetComponentsInChildren<Collider>(true)); 
            colliders.RemoveAll(i => i == null || i.gameObject.GetComponentInParent<SwoleMuscleCollisionBroadcaster>(true) != this); */
        }

        public UnityEvent<Collision> OnStartColliding;
        public UnityEvent<Collision> OnContinueColliding;
        public UnityEvent<Collision> OnStopColliding;

#if SWOLE_ROOTMOTION
        protected override void OnCollisionEnter(Collision collision)
        {
            base.OnCollisionEnter(collision);
#else
        protected void OnCollisionEnter(Collision collision)
        {
#endif
            OnStartColliding?.Invoke(collision);
        }
#if SWOLE_ROOTMOTION
        protected override void OnCollisionStay(Collision collision)
        {
            base.OnCollisionStay(collision);
#else
        protected void OnCollisionStay(Collision collision)
        {
#endif
            OnContinueColliding?.Invoke(collision);
        }
#if SWOLE_ROOTMOTION
        protected override void OnCollisionExit(Collision collision)
        {
            base.OnCollisionExit(collision);
#else
        protected void OnCollisionExit(Collision collision)
        {
#endif
            OnStopColliding?.Invoke(collision); 
        }

        public virtual void EnterTriggerMode()
        {
#if SWOLE_ROOTMOTION
            /*if (colliders == null) return;

            foreach(var collider in colliders)
            {
                if (collider == null) continue;  

                collider.isTrigger = true;
            }*/

            if (puppetMaster == null) return;
            var muscle = puppetMaster.muscles[muscleIndex];
            if (muscle.colliders == null) return;
            foreach (var collider in muscle.colliders)
            {
                if (collider == null) continue;
                collider.isTrigger = true;
            }
#endif
        }
        public virtual void ExitTriggerMode()
        {
#if SWOLE_ROOTMOTION
            /*if (colliders == null) return;

            foreach (var collider in colliders)
            {
                if (collider == null) continue;

                collider.isTrigger = false;
            }*/

            if (puppetMaster == null) return;
            var muscle = puppetMaster.muscles[muscleIndex];
            if (muscle.colliders == null) return;
            foreach (var collider in muscle.colliders)
            {
                if (collider == null) continue;
                collider.isTrigger = false;
            }
#endif
        }

        protected virtual void OnTriggerEnter(Collider collider)
        {
            if (!enabled) return;
#if SWOLE_ROOTMOTION
            if (puppetMaster == null || IsSelf(collider) || puppetMaster.muscles[muscleIndex].state.isDisconnected) return;

            foreach (BehaviourBase behaviour in puppetMaster.behaviours)
            {
                if (behaviour is ISwolePuppetBehaviour spb)
                {
                    spb.OnMuscleTriggerEnter(new MuscleTriggerEvent(muscleIndex, collider)); 
                }
            }
#endif
        }

        protected virtual void OnTriggerStay(Collider collider)
        {
            if (!enabled) return;
#if SWOLE_ROOTMOTION
            if (puppetMaster == null || IsSelf(collider) || puppetMaster.muscles[muscleIndex].state.isDisconnected) return;

            foreach (BehaviourBase behaviour in puppetMaster.behaviours)
            {
                if (behaviour is ISwolePuppetBehaviour spb)
                {
                    spb.OnMuscleTrigger(new MuscleTriggerEvent(muscleIndex, collider, true));
                }
            }
#endif
        }

        protected virtual void OnTriggerExit(Collider collider)
        {
            if (!enabled) return;
#if SWOLE_ROOTMOTION
            if (puppetMaster == null || IsSelf(collider) || puppetMaster.muscles[muscleIndex].state.isDisconnected) return;

            foreach (BehaviourBase behaviour in puppetMaster.behaviours)
            {
                if (behaviour is ISwolePuppetBehaviour spb)
                {
                    spb.OnMuscleTriggerExit(new MuscleTriggerEvent(muscleIndex, collider));
                }
            }
#endif
        }
    }
}

#endif