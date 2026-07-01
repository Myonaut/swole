using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;

namespace Swole
{
    public class ColliderMouseEventBroadcaster : MonoBehaviour
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
        private UnityEvent onMouseEnter;
        [SerializeField]
        private UnityEvent onMouseStay;
        [SerializeField]
        private UnityEvent onMouseExit;

        void OnMouseEnter()
        {
            if (target != null) target.SendMessage("OnMouseEnter", SendMessageOptions.DontRequireReceiver);
            if (targets != null && targets.Count > 0)
            {
                foreach(var target in targets)
                {
                    target.SendMessage("OnMouseEnter", SendMessageOptions.DontRequireReceiver);
                }
            }
            onMouseEnter?.Invoke();
        }

        void OnMouseOver()
        {
            if (target != null) target.SendMessage("OnMouseOver", SendMessageOptions.DontRequireReceiver);
            if (targets != null && targets.Count > 0)
            {
                foreach (var target in targets)
                {
                    target.SendMessage("OnMouseOver", SendMessageOptions.DontRequireReceiver);
                }
            }
            onMouseStay?.Invoke();
        }

        void OnMouseExit()
        {
            if (target != null) target.SendMessage("OnMouseExit", SendMessageOptions.DontRequireReceiver);
            if (targets != null && targets.Count > 0)
            {
                foreach (var target in targets)
                {
                    target.SendMessage("OnMouseExit", SendMessageOptions.DontRequireReceiver);
                }
            }
            onMouseExit?.Invoke();
        }

        public void ListenEnter(UnityAction listener)
        {
            if (onMouseEnter == null) onMouseEnter = new UnityEvent();
            onMouseEnter.AddListener(listener);
        }

        public void ListenStay(UnityAction listener)
        {
            if (onMouseStay == null) onMouseStay = new UnityEvent();
            onMouseStay.AddListener(listener);
        }

        public void ListenExit(UnityAction listener)
        {
            if (onMouseExit == null) onMouseExit = new UnityEvent();
            onMouseExit.AddListener(listener);
        }

        public void EndListenEnter(UnityAction listener)
        {
            if (onMouseEnter == null) return;
            onMouseEnter.RemoveListener(listener);
        }

        public void EndListenStay(UnityAction listener)
        {
            if (onMouseStay == null) return;
            onMouseStay.RemoveListener(listener);
        }

        public void EndListenExit(UnityAction listener)
        {
            if (onMouseExit == null) return;
            onMouseExit.RemoveListener(listener); 
        }

    }
}
