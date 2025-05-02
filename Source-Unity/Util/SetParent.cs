#if (UNITY_EDITOR || UNITY_STANDALONE)

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Swole
{
    public class SetParent : MonoBehaviour
    {

        public bool onAwake = true;

        public Transform parent;

        private void Awake()
        {

            if (!onAwake) return;

            transform.SetParent(parent, true);

            Destroy(this);

        }

        void Start()
        {

            if (onAwake) return;

            transform.SetParent(parent, true);

            Destroy(this);

        }
    }
}

#endif