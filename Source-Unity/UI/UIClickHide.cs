#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole.UI
{
    public class UIClickHide : MonoBehaviour
    {
        public float delay = 0.1f;
        protected float timer;
        protected void LateUpdate()
        {
            if (timer > 0)
            {
                timer -= Time.deltaTime;
                if (timer <= 0) gameObject.SetActive(false);
            }
            if (InputProxy.CursorPrimaryButtonDown)
            {
                if (delay > 0) timer = delay; else gameObject.SetActive(false);
            }
        }
    }
}

#endif