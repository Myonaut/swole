#if (UNITY_EDITOR || UNITY_STANDALONE)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;

namespace Swole.API.Unity
{

    public abstract class SubstepPhysicsBehaviour : MonoBehaviour
    {

        protected virtual void Awake()
        {
            SubstepPhysicsManager.Register(this);
        }
        protected virtual void OnEnable()
        {

            SubstepPhysicsManager.Register(this);
        }

        protected virtual void OnDisable()
        {
            SubstepPhysicsManager.Unregister(this);
        }

        public virtual void SubstepEarlyUpdate() { }
        public virtual void SubstepUpdate() { }
        public virtual void SubstepLateUpdate() { }

    }

    public class SubstepPhysicsBehaviourProxy : SubstepPhysicsBehaviour
    {

        public UnityEvent OnEarlyPhysicsUpdate;
        public UnityEvent OnPhysicsUpdate;
        public UnityEvent OnLatePhysicsUpdate;

        public override void SubstepEarlyUpdate() 
        {
            OnEarlyPhysicsUpdate?.Invoke();
        }
        public override void SubstepUpdate() 
        {
            OnPhysicsUpdate?.Invoke();
        }
        public override void SubstepLateUpdate() 
        {
            OnLatePhysicsUpdate?.Invoke();
        }

    }

}

#endif