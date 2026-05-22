using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;

namespace Swole
{
    public class TriggerEventBroadcaster : MonoBehaviour
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
        private UnityEvent<Collider> onTriggerEnter;
        [SerializeField]
        private UnityEvent<Collider> onTriggerStay;
        [SerializeField]
        private UnityEvent<Collider> onTriggerExit;

        void OnTriggerEnter(Collider collider)
        {
            if (target != null) target.SendMessage("OnTriggerEnter", collider, SendMessageOptions.DontRequireReceiver);
            if (targets != null && targets.Count > 0)
            {
                foreach(var target in targets)
                {
                    target.SendMessage("OnTriggerEnter", collider, SendMessageOptions.DontRequireReceiver);
                }
            }
            onTriggerEnter?.Invoke(collider);
        }

        void OnTriggerStay(Collider collider)
        {
            if (target != null) target.SendMessage("OnTriggerStay", collider, SendMessageOptions.DontRequireReceiver);
            if (targets != null && targets.Count > 0)
            {
                foreach (var target in targets)
                {
                    target.SendMessage("OnTriggerStay", collider, SendMessageOptions.DontRequireReceiver);
                }
            }
            onTriggerStay?.Invoke(collider);
        }

        void OnTriggerExit(Collider collider)
        {
            if (target != null) target.SendMessage("OnTriggerExit", collider, SendMessageOptions.DontRequireReceiver);
            if (targets != null && targets.Count > 0)
            {
                foreach (var target in targets)
                {
                    target.SendMessage("OnTriggerExit", collider, SendMessageOptions.DontRequireReceiver);
                }
            }
            onTriggerExit?.Invoke(collider);
        }

        public void ListenEnter(UnityAction<Collider> listener)
        {
            if (onTriggerEnter == null) onTriggerEnter = new UnityEvent<Collider>();
            onTriggerEnter.AddListener(listener);
        }

        public void ListenStay(UnityAction<Collider> listener)
        {
            if (onTriggerStay == null) onTriggerStay = new UnityEvent<Collider>();
            onTriggerStay.AddListener(listener);
        }

        public void ListenExit(UnityAction<Collider> listener)
        {
            if (onTriggerExit == null) onTriggerExit = new UnityEvent<Collider>();
            onTriggerExit.AddListener(listener);
        }

        public void EndListenEnter(UnityAction<Collider> listener)
        {
            if (onTriggerEnter == null) return;
            onTriggerEnter.RemoveListener(listener);
        }

        public void EndListenStay(UnityAction<Collider> listener)
        {
            if (onTriggerStay == null) return;
            onTriggerStay.RemoveListener(listener);
        }

        public void EndListenExit(UnityAction<Collider> listener)
        {
            if (onTriggerExit == null) return;
            onTriggerExit.RemoveListener(listener); 
        }

    }
}
