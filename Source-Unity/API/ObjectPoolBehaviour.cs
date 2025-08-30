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

    public interface IObjectPool : IDisposable
    {

        public bool TryGetNewInstance(out UnityEngine.Object instance);

        public void Claim(UnityEngine.Object instance);
        public void Claim(PooledObject pooledObject);

        public bool Release(UnityEngine.Object instance);
        public bool Release(PooledObject pooledObject);

        public void Invalidate(UnityEngine.Object instance);
        public void Invalidate(PooledObject pooledObject);

    }

    public class ObjectPoolBehaviour<T> : MonoBehaviour, IObjectPool where T : UnityEngine.Object
    {

        protected readonly ObjectPool<T> pool = new ObjectPool<T>();

        public void SetDontDestroyInstances(bool dontDestroy) => pool.dontDestroyInstances = dontDestroy;
        public bool DontDestroyInstances
        {
            get => pool.dontDestroyInstances;
            set => SetDontDestroyInstances(value);
        }
        protected virtual void OnDestroy()
        {
            pool.Dispose();
        }
        public void Dispose()
        {
            DestroyImmediate(this);
        }

        public T Prototype => pool.Prototype;

        public bool IsValid => pool.IsValid;

        public bool IsInPool(T inst) => pool.IsInPool(inst);

        public int Size => pool.Size;
        public int Pooled => pool.Pooled;

        public bool Initialized => pool.Initialized;

        //private T CreateNewInstance() => pool.CreateNewInstance();

        public void Initialize() => pool.Initialize();

        public void Reinitialize(T prototype, PoolGrowthMethod growthMethod, int growthFactor, int initialSize, int maxSize) => pool.Reinitialize(prototype, growthMethod, growthFactor, initialSize, maxSize);

        private void Awake()
        {
            pool.OnCreateNewInstance += OnCreateNew;
            pool.OnClaimInstance += OnClaim;
            pool.OnReleaseInstance += OnRelease;
            pool.OnInvalidateInstance += OnInvalidate;

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

        //private bool Grow() => pool.Grow();

        public bool TryGetNewInstance(out UnityEngine.Object instance)  => pool.TryGetNewInstance(out instance);
        public bool TryGetNewInstance(out T instance) => pool.TryGetNewInstance(out instance);

        /// <summary>
        /// Tries to get 'count' number of instances and adds them to the 'instancesList'. Returns number of instances added.
        /// </summary>
        public int GetNewInstances(int count, ref List<T> instancesList) => pool.GetNewInstances(count, ref instancesList);

        public void Claim(UnityEngine.Object instance) => pool.Claim(instance);
        public void Claim(T instance) => pool.Claim(instance);
        public void Claim(PooledObject pooledObject) => pool.Claim(pooledObject);

        public bool Release(UnityEngine.Object instance) => pool.Release(instance);
        public bool Release(T instance) => pool.Release(instance);
        public bool Release(PooledObject pooledObject) => pool.Release(pooledObject);
        public void ReleaseAllClaims() => pool.ReleaseAllClaims();

        public void Invalidate(UnityEngine.Object instance) => pool.Invalidate(instance);
        public void Invalidate(T instance) => pool.Invalidate(instance);
        public void Invalidate(PooledObject pooledObject) => pool.Invalidate(pooledObject);
    }

    public class ObjectPool<T> : IObjectPool where T : UnityEngine.Object
    {
        /// <summary>
        /// Should instances be spared when the pool is destroyed?
        /// </summary>
        public bool dontDestroyInstances;
        public virtual void Dispose()
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
                            UnityEngine.Object.Destroy(inst);
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
                            UnityEngine.Object.Destroy(inst);
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

        internal List<T> pooledObjects;
        internal List<T> claimedObjects;

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

        internal T CreateNewInstance()
        {
            T instance = UnityEngine.Object.Instantiate(prototype);
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

            if (pooledObjects != null) foreach (var obj in pooledObjects) if (obj != null)
                    {
                        if (obj is GameObject go) UnityEngine.Object.DestroyImmediate(go); else if (obj is Component comp) UnityEngine.Object.Destroy(comp.gameObject); else UnityEngine.Object.Destroy(obj);
                    }
            if (claimedObjects != null) foreach (var obj in claimedObjects) if (obj != null)
                    {
                        if (obj is GameObject go) UnityEngine.Object.DestroyImmediate(go); else if (obj is Component comp) UnityEngine.Object.Destroy(comp.gameObject); else UnityEngine.Object.Destroy(obj);
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

        protected virtual void OnCreateNew(T inst) { }
        protected virtual void OnClaim(T inst) { }
        protected virtual void OnRelease(T inst) { }
        protected virtual void OnInvalidate(T inst) { }

        internal bool Grow()
        {
            if (!IsValid) return false;

            int prevSize = pooledObjects.Capacity;
            int size = prevSize;
            switch (growthMethod)
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
            while (i < count)
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

        public bool Release(UnityEngine.Object instance)
        {
            if (!IsValid) return false;

            if (instance is T inst)
            {
                int i = claimedObjects.IndexOf(inst);
                if (i >= 0)
                {
                    claimedObjects.RemoveAt(i);
                    pooledObjects.Add(inst);
                    OnRelease(inst);
                    OnReleaseInstance?.Invoke(inst);
                    return true;
                }
            }
            else if (typeof(GameObject).IsAssignableFrom(typeof(T)) && instance is Component comp)
            {
                return Release(comp.gameObject);
            }

            return false;
        }
        public bool Release(T instance) => Release((UnityEngine.Object)instance);
        public bool Release(PooledObject pooledObject)
        {
            if (pooledObject == null) return false;

            if (typeof(Component).IsAssignableFrom(typeof(T)))
            {
                T inst = pooledObject.GetComponent<T>();
                if (inst == null) return false;
                return Release(inst);
            }
            else if (typeof(GameObject).IsAssignableFrom(typeof(T)))
            {
                return Release((UnityEngine.Object)pooledObject.gameObject);
            }

            return Release((UnityEngine.Object)pooledObject);
        }
        public void ReleaseAllClaims()
        {
            while (claimedObjects.Count > 0)
            {
                int count = claimedObjects.Count;
                if (!Release(claimedObjects[0]))
                {
                    if (claimedObjects.Count == count) claimedObjects.RemoveAt(0); else if (claimedObjects.Count > count) break;
                }
            }
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