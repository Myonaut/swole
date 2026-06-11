using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;

namespace Swole
{
    public class RendererEventBroadcaster : MonoBehaviour
    {

        public GameObject target;
        [SerializeField]
        private List<GameObject> targets;

        public void AddTarget(GameObject target)
        {
            if (targets == null) targets = new List<GameObject>();
            if (!targets.Contains(target)) targets.Add(target); 
        }
        public void RemoveTarget(GameObject target)
        {
            if (targets == null) return;
            targets.Remove(target);
        }

        [SerializeField]
        private UnityEvent onBecameVisible;
        [SerializeField]
        private UnityEvent onBecameInvisible;

        void OnBecameVisible()
        {
            if (target != null) target.SendMessage("OnBecameVisible", SendMessageOptions.DontRequireReceiver);
            if (targets != null && targets.Count > 0)
            {
                foreach (var target in targets)
                {
                    target.SendMessage("OnBecameVisible", SendMessageOptions.DontRequireReceiver);
                }
            }
            onBecameVisible?.Invoke();
        }

        void OnBecameInvisible()
        {
            if (target != null) target.SendMessage("OnBecameInvisible", SendMessageOptions.DontRequireReceiver);
            if (targets != null && targets.Count > 0)
            {
                foreach (var target in targets)
                {
                    target.SendMessage("OnBecameInvisible", SendMessageOptions.DontRequireReceiver);
                }
            }
            onBecameInvisible?.Invoke();
        }

        public void ListenVisible(UnityAction listener)
        {
            if (onBecameVisible == null) onBecameVisible = new UnityEvent();
            onBecameVisible.AddListener(listener);
        }

        public void ListenInvisible(UnityAction listener)
        {
            if (onBecameInvisible == null) onBecameInvisible = new UnityEvent();
            onBecameInvisible.AddListener(listener);
        }

        public void EndListenVisible(UnityAction listener)
        {
            if (onBecameVisible == null) return;
            onBecameVisible.RemoveListener(listener);
        }

        public void EndListenInvisible(UnityAction listener)
        {
            if (onBecameInvisible == null) return;
            onBecameInvisible.RemoveListener(listener);
        }

    }
}
