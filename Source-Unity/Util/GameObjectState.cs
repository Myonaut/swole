#if (UNITY_STANDALONE || UNITY_EDITOR)

using UnityEngine;
using UnityEngine.Events;

namespace Swole.Unity 
{
    public class GameObjectState : MonoBehaviour
    {

        [SerializeField]
        protected UnityEvent onStart;
        [SerializeField]
        protected UnityEvent onEnable;
        [SerializeField]
        protected UnityEvent onDisable;
        [SerializeField]
        protected UnityEvent onDestroy;

        [SerializeField]
        protected UnityEvent<GameObject> onStartGO;
        [SerializeField]
        protected UnityEvent<GameObject> onEnableGO;
        [SerializeField]
        protected UnityEvent<GameObject> onDisableGO;
        [SerializeField]
        protected UnityEvent<GameObject> onDestroyGO;

        public virtual void Start()
        {
            onStart?.Invoke();
            onStartGO?.Invoke(gameObject);
        }
        public virtual void OnEnable()
        {
            onEnable?.Invoke();
            onEnableGO?.Invoke(gameObject);
        }
        public virtual void OnDisable()
        {
            onDisable?.Invoke();
            onDisableGO?.Invoke(gameObject);
        }
        public virtual void OnDestroy()
        {
            onDestroy?.Invoke();
            onDestroyGO?.Invoke(gameObject);
        }

        public void ListenOnStart(UnityAction listener)
        {
            if (onStart == null) onStart = new UnityEvent();
            onStart.AddListener(listener);
        }
        public void RemoveOnStart(UnityAction listener)
        {
            if (onStart == null) return;
            onStart.RemoveListener(listener);
        }

        public void ListenOnEnable(UnityAction listener)
        {
            if (onEnable == null) onEnable = new UnityEvent();
            onEnable.AddListener(listener);
        }
        public void RemoveOnEnable(UnityAction listener)
        {
            if (onEnable == null) return;
            onEnable.RemoveListener(listener);
        }

        public void ListenOnDisable(UnityAction listener)
        {
            if (onDisable == null) onDisable = new UnityEvent();
            onDisable.AddListener(listener);
        }
        public void RemoveOnDisable(UnityAction listener)
        {
            if (onDisable == null) return;
            onDisable.RemoveListener(listener);
        }

        public void ListenOnDestroy(UnityAction listener)
        {
            if (onDestroy == null) onDestroy = new UnityEvent();
            onDestroy.AddListener(listener); 
        }
        public void RemoveOnDestroy(UnityAction listener)
        {
            if (onDestroy == null) return;
            onDestroy.RemoveListener(listener); 
        }


        public void ListenOnStart(UnityAction<GameObject> listener)
        {
            if (onStartGO == null) onStartGO = new UnityEvent<GameObject>();
            onStartGO.AddListener(listener);
        }
        public void RemoveOnStart(UnityAction<GameObject> listener)
        {
            if (onStartGO == null) return;
            onStartGO.RemoveListener(listener);
        }

        public void ListenOnEnable(UnityAction<GameObject> listener)
        {
            if (onEnableGO == null) onEnableGO = new UnityEvent<GameObject>();
            onEnableGO.AddListener(listener);
        }
        public void RemoveOnEnable(UnityAction<GameObject> listener)
        {
            if (onEnableGO == null) return;
            onEnableGO.RemoveListener(listener);
        }

        public void ListenOnDisable(UnityAction<GameObject> listener)
        {
            if (onDisableGO == null) onDisableGO = new UnityEvent<GameObject>();
            onDisableGO.AddListener(listener);
        }
        public void RemoveOnDisable(UnityAction<GameObject> listener)
        {
            if (onDisableGO == null) return;
            onDisableGO.RemoveListener(listener);
        }

        public void ListenOnDestroy(UnityAction<GameObject> listener)
        {
            if (onDestroyGO == null) onDestroyGO = new UnityEvent<GameObject>();
            onDestroyGO.AddListener(listener);
        }
        public void RemoveOnDestroy(UnityAction<GameObject> listener)
        {
            if (onDestroyGO == null) return;
            onDestroyGO.RemoveListener(listener);
        }

    }
}

#endif