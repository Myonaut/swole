#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;


#if BULKOUT_ENV
using RootMotion;
using RootMotion.Dynamics;
#endif

namespace Swole.API.Unity.Animation
{
#if BULKOUT_ENV
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

        protected override void OnCollisionEnter(Collision collision)
        {
            base.OnCollisionEnter(collision);
            OnStartColliding?.Invoke(collision);
        }
        protected override void OnCollisionStay(Collision collision)
        {
            base.OnCollisionStay(collision);
            OnContinueColliding?.Invoke(collision);
        }
        protected override void OnCollisionExit(Collision collision)
        {
            base.OnCollisionExit(collision);
            OnStopColliding?.Invoke(collision); 
        }

        public virtual void EnterTriggerMode()
        {
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
        }
        public virtual void ExitTriggerMode()
        {
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
        }

        protected virtual void OnTriggerEnter(Collider collider)
        {
            if (!enabled) return;
#if BULKOUT_ENV
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
#if BULKOUT_ENV
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
#if BULKOUT_ENV
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