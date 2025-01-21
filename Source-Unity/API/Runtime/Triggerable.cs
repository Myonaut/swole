#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;

namespace Swole.API.Unity
{
    public class Triggerable : MonoBehaviour
    {

        public UnityEvent OnTriggered; 

        public void Trigger() => Trigger(0);
        public virtual void Trigger(int triggerIndex)
        {
            OnTriggered?.Invoke();
        }

    }
}

#endif