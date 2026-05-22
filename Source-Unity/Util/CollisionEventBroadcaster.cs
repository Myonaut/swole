using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;

namespace Swole
{
    public class CollisionEventBroadcaster : MonoBehaviour
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
        private UnityEvent<Collision> onCollisionEnter;
        [SerializeField]
        private UnityEvent<Collision> onCollisionStay;
        [SerializeField]
        private UnityEvent<Collision> onCollisionExit;

        void OnCollisionEnter(Collision collision)
        {
            if (target != null) target.SendMessage("OnCollisionEnter", collision, SendMessageOptions.DontRequireReceiver);
            if (targets != null && targets.Count > 0)
            {
                foreach(var target in targets)
                {
                    target.SendMessage("OnCollisionEnter", collision, SendMessageOptions.DontRequireReceiver);
                }
            }
            onCollisionEnter?.Invoke(collision);
        }

        void OnCollisionStay(Collision collision)
        {
            if (target != null) target.SendMessage("OnCollisionStay", collision, SendMessageOptions.DontRequireReceiver);
            if (targets != null && targets.Count > 0)
            {
                foreach (var target in targets)
                {
                    target.SendMessage("OnCollisionStay", collision, SendMessageOptions.DontRequireReceiver);
                }
            }
            onCollisionStay?.Invoke(collision);
        }

        void OnCollisionExit(Collision collision)
        {
            if (target != null) target.SendMessage("OnCollisionExit", collision, SendMessageOptions.DontRequireReceiver);
            if (targets != null && targets.Count > 0)
            {
                foreach (var target in targets)
                {
                    target.SendMessage("OnCollisionExit", collision, SendMessageOptions.DontRequireReceiver);
                }
            }
            onCollisionExit?.Invoke(collision);
        }

        public void ListenEnter(UnityAction<Collision> listener)
        {
            if (onCollisionEnter == null) onCollisionEnter = new UnityEvent<Collision>();
            onCollisionEnter.AddListener(listener);
        }

        public void ListenStay(UnityAction<Collision> listener)
        {
            if (onCollisionStay == null) onCollisionStay = new UnityEvent<Collision>();
            onCollisionStay.AddListener(listener);
        }

        public void ListenExit(UnityAction<Collision> listener)
        {
            if (onCollisionExit == null) onCollisionExit = new UnityEvent<Collision>();
            onCollisionExit.AddListener(listener);
        }

        public void EndListenEnter(UnityAction<Collision> listener)
        {
            if (onCollisionEnter == null) return;
            onCollisionEnter.RemoveListener(listener);
        }

        public void EndListenStay(UnityAction<Collision> listener)
        {
            if (onCollisionStay == null) return;
            onCollisionStay.RemoveListener(listener);
        }

        public void EndListenExit(UnityAction<Collision> listener)
        {
            if (onCollisionExit == null) return;
            onCollisionExit.RemoveListener(listener); 
        }

    }
}
