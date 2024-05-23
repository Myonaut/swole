#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;

namespace Swole.API.Unity
{
    public class SwoleGameObject : MonoBehaviour
    {

        public int id;

        public UnityEvent OnDestroyed;

        protected void OnDestroy()
        {

            OnDestroyed?.Invoke();

        }

    }
}

#endif