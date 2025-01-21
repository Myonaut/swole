#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole
{
    public abstract class ControlProxy : MonoBehaviour
    {

        protected virtual bool IsReadyToBind => true;

        protected void Awake()
        {
            IEnumerator WaitToBind()
            {
                while (!IsReadyToBind) yield return null;
                Rebind();
            }

            StartCoroutine(WaitToBind());

            OnAwake();
        }

        protected virtual void OnAwake() { }

        public abstract void Rebind();

        public abstract int FindBindingIndex(string binding);

        protected void Bind(string defaultName, string[] bindings, ref List<int> indicesList)
        {
            if (bindings == null || bindings.Length <= 0)
            {
                if (string.IsNullOrWhiteSpace(defaultName)) return;
                bindings = new string[] { defaultName };
            }
            if (indicesList == null) indicesList = new List<int>();

            foreach (var binding in bindings)
            {
                var index = FindBindingIndex(binding);
                if (index < 0) continue;
                indicesList.Add(index);
            }
        }

    }
}

#endif