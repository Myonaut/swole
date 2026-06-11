using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;

namespace Swole.Unity
{
    public class OwnerModule : MonoBehaviour
    {

        [SerializeField]
        protected Behaviour initBehaviourProxy;
        protected IBehaviourStateListener behaviourProxy;
        public bool HasBehaviourProxy => behaviourProxy != null;
        public void SetBehaviourProxy(IBehaviourStateListener behaviourProxy)
        {
            if (HasBehaviourProxy)
            {
                this.behaviourProxy.EndListenForEnable(OnEnableApply);
                this.behaviourProxy.EndListenForDisable(OnDisableApply); 
                this.behaviourProxy.EndListenForDestroy(OnDestroyApply);
            }

            this.behaviourProxy = behaviourProxy;

            if (behaviourProxy != null)
            {
                behaviourProxy.ListenForEnable(OnEnableApply);
                behaviourProxy.ListenForDisable(OnDisableApply);
                behaviourProxy.ListenForDestroy(OnDestroyApply);
            }
        }

        protected void Awake()
        {
            SetBehaviourProxy(initBehaviourProxy as IBehaviourStateListener); 
        }

        [Serializable]
        public class StateSet : IDisposable
        {

            [SerializeField]
            protected List<GameObject> gameObjectsToDestroy = new List<GameObject>();
            [SerializeField]
            protected List<Component> componentsToDestroy = new List<Component>();

            [SerializeField]
            protected List<GameObject> gameObjectsToEnable = new List<GameObject>();
            [SerializeField]
            protected List<Behaviour> componentsToEnable = new List<Behaviour>();

            [SerializeField]
            protected List<GameObject> gameObjectsToDisable = new List<GameObject>();
            [SerializeField]
            protected List<Behaviour> componentsToDisable = new List<Behaviour>();

            public void AddToDestroy(GameObject go)
            {
                if (go != null && gameObjectsToDestroy != null && !gameObjectsToDestroy.Contains(go))
                {
                    gameObjectsToDestroy?.Add(go);
                }
            }

            public void AddToDestroy(Component c)
            {
                if (c != null && componentsToDestroy != null && !componentsToDestroy.Contains(c))
                {
                    componentsToDestroy?.Add(c);
                }
            }

            public void AddToEnable(GameObject go)
            {
                if (go != null && gameObjectsToEnable != null && !gameObjectsToEnable.Contains(go))
                {
                    gameObjectsToEnable?.Add(go);
                }
            }

            public void AddToEnable(Behaviour c)
            {
                if (c != null && componentsToEnable != null && !componentsToEnable.Contains(c))
                {
                    componentsToEnable?.Add(c);
                }
            }

            public void AddToDisable(GameObject go)
            {
                if (go != null && gameObjectsToDisable != null && !gameObjectsToDisable.Contains(go))
                {
                    gameObjectsToDisable?.Add(go);
                }
            }

            public void AddToDisable(Behaviour c)
            {
                if (c != null && componentsToDisable != null && !componentsToDisable.Contains(c))
                {
                    componentsToDisable?.Add(c);
                }
            }

            public void RemoveFromDestroy(GameObject go)
            {
                if (go != null && gameObjectsToDestroy != null)
                {
                    gameObjectsToDestroy?.Remove(go);
                }
            }

            public void RemoveFromDestroy(Component c)
            {
                if (c != null && componentsToDestroy != null)
                {
                    componentsToDestroy?.Remove(c);
                }
            }

            public void RemoveFromEnable(GameObject go)
            {
                if (go != null && gameObjectsToEnable != null)
                {
                    gameObjectsToEnable?.Remove(go);
                }
            }

            public void RemoveFromEnable(Behaviour c)
            {
                if (c != null && componentsToEnable != null)
                {
                    componentsToEnable?.Remove(c);
                }
            }

            public void RemoveFromDisable(GameObject go)
            {
                if (go != null && gameObjectsToDisable != null)
                {
                    gameObjectsToDisable?.Remove(go);
                }
            }

            public void RemoveFromDisable(Behaviour c)
            {
                if (c != null && componentsToDisable != null)
                {
                    componentsToDisable?.Remove(c);
                }
            }

            public void Apply()
            {
                if (gameObjectsToDestroy != null) foreach (var go in gameObjectsToDestroy)
                {
                    if (go != null)
                    {
                        Destroy(go);
                    }
                }
                if (componentsToDestroy != null) foreach (var c in componentsToDestroy)
                {
                    if (c != null)
                    {
                        Destroy(c);
                    }
                }
                gameObjectsToDestroy.Clear();
                componentsToDestroy.Clear();

                if (gameObjectsToEnable != null) foreach (var go in gameObjectsToEnable)
                {
                    if (go != null)
                    {
                        go.SetActive(true);
                    }
                }
                if (componentsToEnable != null) foreach (var c in componentsToEnable)
                {
                    if (c != null)
                    {
                        c.enabled = true;
                    }
                }

                if (gameObjectsToDisable != null) foreach (var go in gameObjectsToDisable)
                {
                    if (go != null)
                    {
                        go.SetActive(false);
                    }
                }
                if (componentsToDisable != null) foreach (var c in componentsToDisable)
                {
                    if (c != null)
                    {
                        c.enabled = false;
                    }
                }
            }

            public void Dispose()
            {
                gameObjectsToDestroy?.Clear();
                componentsToDestroy?.Clear();
                gameObjectsToEnable?.Clear();
                componentsToEnable?.Clear();
                gameObjectsToDisable?.Clear();
                componentsToDisable?.Clear();

                gameObjectsToDestroy = null;
                componentsToDestroy = null;
                gameObjectsToEnable = null;
                componentsToEnable = null;
                gameObjectsToDisable = null;
                componentsToDisable = null;
            }

        }

        [SerializeField]
        protected StateSet onDestroySet = new StateSet();

        public void DestroyOnDestroy(GameObject go)
        {
            onDestroySet.AddToDestroy(go);
        }
        public void DestroyOnDestroy(Component c)
        {
            onDestroySet.AddToDestroy(c);
        }
        public void EnableOnDestroy(GameObject go)
        {
            onDestroySet.AddToEnable(go);
        }
        public void EnableOnDestroy(Behaviour b)
        {
            onDestroySet.AddToEnable(b);
        }
        public void DisableOnDestroy(GameObject go)
        {
            onDestroySet.AddToDisable(go);
        }
        public void DisableOnDestroy(Behaviour b)
        {
            onDestroySet.AddToDisable(b);
        }

        public void DontDestroyOnDestroy(GameObject go)
        {
            onDestroySet.RemoveFromDestroy(go);
        }
        public void DontDestroyOnDestroy(Component c)
        {
            onDestroySet.RemoveFromDestroy(c);
        }
        public void DontEnableOnDestroy(GameObject go)
        {
            onDestroySet.RemoveFromEnable(go);
        }
        public void DontEnableOnDestroy(Behaviour b)
        {
            onDestroySet.RemoveFromEnable(b);
        }
        public void DontDisableOnDestroy(GameObject go)
        {
            onDestroySet.RemoveFromDisable(go);
        }
        public void DontDisableOnDestroy(Behaviour b)
        {
            onDestroySet.RemoveFromDisable(b);
        }

        [SerializeField]
        protected StateSet onEnableSet = new StateSet();

        public void DestroyOnEnable(GameObject go)
        {
            onEnableSet.AddToDestroy(go);
        }
        public void DestroyOnEnable(Component c)
        {
            onEnableSet.AddToDestroy(c);
        }
        public void EnableOnEnable(GameObject go)
        {
            onEnableSet.AddToEnable(go);
        }
        public void EnableOnEnable(Behaviour b)
        {
            onEnableSet.AddToEnable(b);
        }
        public void DisableOnEnable(GameObject go)
        {
            onEnableSet.AddToDisable(go);
        }
        public void DisableOnEnable(Behaviour b)
        {
            onEnableSet.AddToDisable(b);
        }

        public void DontDestroyOnEnable(GameObject go)
        {
            onEnableSet.RemoveFromDestroy(go);
        }
        public void DontDestroyOnEnable(Component c)
        {
            onEnableSet.RemoveFromDestroy(c);
        }
        public void DontEnableOnEnable(GameObject go)
        {
            onEnableSet.RemoveFromEnable(go);
        }
        public void DontEnableOnEnable(Behaviour b)
        {
            onEnableSet.RemoveFromEnable(b);
        }
        public void DontDisableOnEnable(GameObject go)
        {
            onEnableSet.RemoveFromDisable(go);
        }
        public void DontDisableOnEnable(Behaviour b)
        {
            onEnableSet.RemoveFromDisable(b);
        }

        [SerializeField]
        protected StateSet onDisableSet = new StateSet();

        public void DestroyOnDisable(GameObject go)
        {
            onDisableSet.AddToDestroy(go);
        }
        public void DestroyOnDisable(Component c) 
        {
            onDisableSet.AddToDestroy(c);
        }
        public void EnableOnDisable(GameObject go)
        {
            onDisableSet.AddToEnable(go);
        }
        public void EnableOnDisable(Behaviour b)
        {
            onDisableSet.AddToEnable(b);
        }
        public void DisableOnDisable(GameObject go)
        {
            onDisableSet.AddToDisable(go);
        }
        public void DisableOnDisable(Behaviour b)
        {
            onDisableSet.AddToDisable(b);
        }

        public void DontDestroyOnDisable(GameObject go)
        {
            onDisableSet.RemoveFromDestroy(go);
        }
        public void DontDestroyOnDisable(Component c)
        {
            onDisableSet.RemoveFromDestroy(c);
        }
        public void DontEnableOnDisable(GameObject go)
        {
            onDisableSet.RemoveFromEnable(go);
        }
        public void DontEnableOnDisable(Behaviour b)
        {
            onDisableSet.RemoveFromEnable(b);
        }
        public void DontDisableOnDisable(GameObject go)
        {
            onDisableSet.RemoveFromDisable(go);
        }
        public void DontDisableOnDisable(Behaviour b)
        {
            onDisableSet.RemoveFromDisable(b);
        }

        public void OnDestroyApply()
        {
            onDestroySet?.Apply();
        }
        public void OnEnableApply()
        {
            onEnableSet?.Apply();
        }
        public void OnDisableApply()
        {
            onDisableSet?.Apply();
        }

        protected virtual void OnDestroy()
        {
            if (HasBehaviourProxy)
            {
                behaviourProxy.EndListenForEnable(OnEnableApply);
                behaviourProxy.EndListenForDisable(OnDisableApply);
                behaviourProxy.EndListenForDestroy(OnDestroyApply); 
            }
            else
            {
                OnDestroyApply();
            }
        }

        protected virtual void OnEnable()
        {
            if (!HasBehaviourProxy) OnEnableApply();
        }

        protected virtual void OnDisable()
        {
            if (!HasBehaviourProxy) OnDisableApply();
        }
    }

    public interface IBehaviourStateListener
    {
        public void ListenForEnable(UnityAction listener);
        public void EndListenForEnable(UnityAction listener);

        public void ListenForDisable(UnityAction listener);
        public void EndListenForDisable(UnityAction listener);

        public void ListenForDestroy(UnityAction listener);
        public void EndListenForDestroy(UnityAction listener);
    }
}
