#if (UNITY_EDITOR || UNITY_STANDALONE)

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Swole.API.Unity
{

    public abstract class SubstepPhysicsBehaviour : MonoBehaviour
    {

        protected virtual void Awake()
        {

            SubstepPhysicsManager.Register(this);

        }

        protected virtual void OnDestroy()
        {

            SubstepPhysicsManager.Unregister(this);

        }

        public virtual void SubstepUpdate() { }
        public virtual void SubstepLateUpdate() { }

    }

}

#endif