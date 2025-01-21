#if (UNITY_EDITOR || UNITY_STANDALONE)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole.API.Unity
{
    [Serializable]
    public enum PoolGrowthMethod
    {
        Incremental, Multiplicative
    }

    public interface IObjectPool
    {

        public bool TryGetNewInstance(out UnityEngine.Object instance);

        public void Claim(UnityEngine.Object instance);
        public void Claim(PooledObject pooledObject);

        public void Release(UnityEngine.Object instance);
        public void Release(PooledObject pooledObject);

        public void Invalidate(UnityEngine.Object instance);
        public void Invalidate(PooledObject pooledObject);

    }

    public class ObjectPool<T> : MonoBehaviour, IObjectPool where T : UnityEngine.Object
    {

        /// <summary>
        /// Should instances be spared when the pool is destroyed?
        /// </summary>
        public bool dontDestroyInstances;
        protected virtual void OnDestroy()
        {
            if (pooledObjects != null)
            {
                if (!dontDestroyInstances)
                {
                    foreach (var inst in pooledObjects)
                    {
                        try
                        {
                            if (inst == null) continue;
                            Destroy(inst);
                        }
                        catch { }
                    }
                }

                pooledObjects.Clear();
                pooledObjects = null;
            }
            if (claimedObjects != null)
            {
                if (!dontDestroyInstances)
                {
                    foreach (var inst in claimedObjects)
                    {
                        try
                        {
                            if (inst == null) continue;
                            Destroy(inst);
                        }
                        catch { }
                    }
                }

                claimedObjects.Clear();
                claimedObjects = null;
            }
        }

        [SerializeField]
        private T prototype;
        public T Prototype => prototype;

        [SerializeField]
        private PoolGrowthMethod growthMethod;

        [SerializeField]
        private int growthFactor;

        [SerializeField]
        private int initialSize;

        [SerializeField]
        private int maxSize;

        public delegate void PooledObjectDelegate(T pooledObject);

        public event PooledObjectDelegate OnCreateNewInstance;
        public event PooledObjectDelegate OnClaimInstance;
        public event PooledObjectDelegate OnReleaseInstance;
        public event PooledObjectDelegate OnInvalidateInstance;

        protected List<T> pooledObjects;
        protected List<T> claimedObjects;

        public bool IsValid => pooledObjects != null && claimedObjects != null;

        public bool IsInPool(T inst)
        {
            if (!IsValid) return false;
            return pooledObjects.Contains(inst) || claimedObjects.Contains(inst);
        }

        public int Size => (pooledObjects == null ? 0 : pooledObjects.Count) + (claimedObjects == null ? 0 : claimedObjects.Count);
        public int Pooled => pooledObjects == null ? 0 : pooledObjects.Count;

        private bool initialized;
        public bool Initialized => initialized;

        private T CreateNewInstance()
        {
            T instance = Instantiate(prototype);
            if (instance is GameObject go)
            {
                var pooledObj = go.AddOrGetComponent<PooledObject>();
                pooledObj.pool = this;
            } 
            else if (instance is Component comp)
            {
                var pooledObj = comp.gameObject.AddOrGetComponent<PooledObject>();
                pooledObj.pool = this;
            }
            OnCreateNew(instance);
            OnCreateNewInstance?.Invoke(instance);
            return instance;
        }
        public void Initialize()
        {
            if (initialized) return;

            if (prototype != null)
            {
                pooledObjects = new List<T>(initialSize);
                claimedObjects = new List<T>(initialSize);

                for (int a = 0; a < initialSize; a++) pooledObjects.Add(CreateNewInstance());
                initialized = true;
            }
        }

        public void Reinitialize(T prototype, PoolGrowthMethod growthMethod, int growthFactor, int initialSize, int maxSize)
        {
            initialized = false;

            if (pooledObjects != null) foreach (var obj in pooledObjects.ToArray()) if (obj != null) 
                    {
                        if (obj is GameObject go) DestroyImmediate(go); else if (obj is Component comp) Destroy(comp.gameObject); else Destroy(obj);
                    }
            if (claimedObjects != null) foreach (var obj in claimedObjects.ToArray()) if (obj != null)
                    {
                        if (obj is GameObject go) DestroyImmediate(go); else if (obj is Component comp) Destroy(comp.gameObject); else Destroy(obj);
                    }

            pooledObjects?.Clear();
            claimedObjects?.Clear();

            this.prototype = prototype;
            this.growthMethod = growthMethod;
            this.growthFactor = growthFactor;
            this.initialSize = initialSize;
            this.maxSize = maxSize;

            Initialize();
        }

        private void Awake()
        {
            OnAwake();

            Initialize(); 
        }

        private void Start()
        {
            OnStart();

            Initialize();
        }

        protected virtual void OnAwake() { }
        protected virtual void OnStart() { }
        protected virtual void OnCreateNew(T inst) { }
        protected virtual void OnClaim(T inst) { }
        protected virtual void OnRelease(T inst) { }
        protected virtual void OnInvalidate(T inst) { }

        private bool Grow()
        {
            if (!IsValid) return false;

            int prevSize = pooledObjects.Capacity;
            int size = prevSize;
            switch(growthMethod)
            {
                case PoolGrowthMethod.Incremental:
                    growthFactor = Mathf.Max(1, growthFactor);
                    size = size + growthFactor;
                    break;

                case PoolGrowthMethod.Multiplicative:
                    growthFactor = Mathf.Max(2, growthFactor);
                    size = size * growthFactor;
                    break;
            }
            if (maxSize > 0) size = Mathf.Min(maxSize, size);
            pooledObjects.Capacity = claimedObjects.Capacity = size;

            while (pooledObjects.Count < (pooledObjects.Capacity - claimedObjects.Count)) pooledObjects.Add(CreateNewInstance());
            return prevSize != size;
        }

        public bool TryGetNewInstance(out UnityEngine.Object instance) 
        {
            instance = null;
            if (TryGetNewInstance(out T instance_))
            {
                instance = instance_;
                return true;
            }
            return false;
        }
        public bool TryGetNewInstance(out T instance)
        {
            instance = null;
            if (!IsValid || (maxSize > 0 && claimedObjects.Count >= maxSize)) return false;

            T inst = null;
            while (inst == null && pooledObjects.Count > 0)
            {
                inst = pooledObjects[0];
                pooledObjects.RemoveAt(0);
            }
            if (inst == null)
            {
                Grow();
                while (inst == null && pooledObjects.Count > 0)
                {
                    inst = pooledObjects[0];
                    pooledObjects.RemoveAt(0);
                }
                if (inst == null) return false;
            }
            claimedObjects.Add(inst);
            OnClaim(inst);
            OnClaimInstance?.Invoke(inst);
            instance = inst;
            return true;
        }

        /// <summary>
        /// Tries to get 'count' number of instances and adds them to the 'instancesList'. Returns number of instances added.
        /// </summary>
        public int GetNewInstances(int count, ref List<T> instancesList)
        {
            if (instancesList == null) instancesList = new List<T>();

            if (!IsValid) return 0;

            while (count > Pooled && Grow()) continue;
            int i = 0;
            while(i < count)
            {
                while (pooledObjects.Count <= 0 && Grow()) continue;
                if (pooledObjects.Count <= 0) break;
                T inst = pooledObjects[0];
                pooledObjects.RemoveAt(0);
                if (inst != null)
                {
                    i++;
                    instancesList.Add(inst);
                    claimedObjects.Add(inst);
                    OnClaim(inst);
                    OnClaimInstance?.Invoke(inst);
                }
            }
            return i;
        }

        public void Claim(UnityEngine.Object instance)
        {
            if (!IsValid) return;

            if (instance is T inst)
            {
                int i = pooledObjects.IndexOf(inst);
                if (i >= 0)
                {
                    pooledObjects.RemoveAt(i);
                    claimedObjects.Add(inst);
                    OnClaim(inst);
                    OnClaimInstance?.Invoke(inst);
                }
            }
            else if (typeof(GameObject).IsAssignableFrom(typeof(T)) && instance is Component comp)
            {
                Claim(comp.gameObject);
            }
        }
        public void Claim(T instance) => Claim((UnityEngine.Object)instance);
        public void Claim(PooledObject pooledObject)
        {
            if (pooledObject == null) return;

            if (typeof(Component).IsAssignableFrom(typeof(T)))
            {
                T inst = pooledObject.GetComponent<T>();
                if (inst == null) return;
                Claim(inst);
                return;
            }
            else if (typeof(GameObject).IsAssignableFrom(typeof(T)))
            {
                Claim((UnityEngine.Object)pooledObject.gameObject);
                return;
            }

            Claim((UnityEngine.Object)pooledObject);
        }

        public void Release(UnityEngine.Object instance)
        {
            if (!IsValid) return;

            if (instance is T inst)
            {
                int i = claimedObjects.IndexOf(inst);
                if (i >= 0)
                {
                    claimedObjects.RemoveAt(i);
                    pooledObjects.Add(inst);
                    OnRelease(inst);
                    OnReleaseInstance?.Invoke(inst); 
                }
            } 
            else if (typeof(GameObject).IsAssignableFrom(typeof(T)) && instance is Component comp)
            {
                Release(comp.gameObject);
            }
        }
        public void Release(T instance) => Release((UnityEngine.Object)instance);
        public void Release(PooledObject pooledObject)
        {
            if (pooledObject == null) return;

            if (typeof(Component).IsAssignableFrom(typeof(T)))
            {
                T inst = pooledObject.GetComponent<T>();
                if (inst == null) return;
                Release(inst);
                return;
            }
            else if (typeof(GameObject).IsAssignableFrom(typeof(T)))
            {
                Release((UnityEngine.Object)pooledObject.gameObject);
                return;
            }

            Release((UnityEngine.Object)pooledObject);
        }

        public void Invalidate(UnityEngine.Object instance)
        {
            if (!IsValid) return;

            if (instance is T inst)
            {
                pooledObjects.RemoveAll(i => i == inst);
                claimedObjects.RemoveAll(i => i == inst);
                OnInvalidate(inst);
                OnInvalidateInstance?.Invoke(inst);
            }
            else if (typeof(GameObject).IsAssignableFrom(typeof(T)) && instance is Component comp)
            {
                Invalidate(comp.gameObject);
            }
        }
        public void Invalidate(T instance) => Invalidate((UnityEngine.Object)instance);
        public void Invalidate(PooledObject pooledObject)
        {
            if (pooledObject == null) return;

            if (typeof(Component).IsAssignableFrom(typeof(T)))
            {
                T inst = pooledObject.GetComponent<T>();
                if (inst == null) return;
                Invalidate(inst);
                return;
            }
            else if (typeof(GameObject).IsAssignableFrom(typeof(T)))
            {
                Invalidate((UnityEngine.Object)pooledObject.gameObject);
                return;
            }

            Invalidate((UnityEngine.Object)pooledObject);
        }
    }

}

#endif