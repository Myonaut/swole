using System;
using System.Collections.Generic;

#if (UNITY_STANDALONE || UNITY_EDITOR)
using UnityEngine;
#endif

namespace Swole
{

#if (UNITY_STANDALONE || UNITY_EDITOR)
    public abstract class SingletonBehaviour<T> : MonoBehaviour, ISingletonBehaviour where T : SingletonBehaviour<T>
    {
         
        private static void Create()
        {
            if (!IsCreated) 
            {
                T existingInstance = (T)FindObjectOfType(typeof(T));
                if (existingInstance != null)
                {
                    SetInstance(existingInstance);
                }
                else
                {
                    if (isQuitting) return;
                    SetInstance(new GameObject("").AddComponent<T>());
                    instance.name = instance.GetType().Name + (instance.DestroyOnLoad ? "_scene" : "_persistent");
                }
            }
        }

        private static T instance;
        public static T Instance
        {

            get
            {
                if (!IsCreated) Create();

                return instance;
            }

        }
        public static T InstanceOrNull => instance;

        public static bool IsCreated => instance != null; 

        private static void SetInstance(T newInstance, bool initialize = true)
        {
            instance = newInstance;
            if (Application.isPlaying && !instance.DestroyOnLoad) DontDestroyOnLoad(instance.gameObject); 
            if (initialize) instance.Init();
        }

        private void Awake() { OnAwake(); }
        protected virtual void OnAwake() {}

        private void Init()
        {
            if (instance == null)
            {
                SetInstance(this as T, false);
            }
            else if (instance != this)
            {

                Destroy(this);
                return;

            }

            if (ExecuteInStack) SingletonCallStack.Insert(this);

            OnInit();
        }
        protected virtual void OnInit()
        {
        }

        public virtual bool ExecuteInStack => true;

        public virtual int Priority => 0;

        public int CompareTo(ISingletonBehaviour other) => other == null ? 1 : Priority.CompareTo(other.Priority);
        public int CompareTo(IExecutableBehaviour other) => other == null ? 1 : Priority.CompareTo(other.Priority);

        public abstract void OnUpdate();
        public abstract void OnLateUpdate();
        public abstract void OnFixedUpdate();

        public virtual bool DestroyOnLoad => true;

        private void OnDestroy()
        {

            if (ExecuteInStack) SingletonCallStack.Remove(this);
            if (instance == this) instance = null;

            OnDestroyed();

        }

        public virtual void OnDestroyed() { }

        protected static bool isQuitting;

        public static bool IsQuitting() => isQuitting;

        private void OnApplicationQuit()
        {

            isQuitting = true;

            OnQuit();

        }

        public virtual void OnQuit() { }
    }
#else
    public abstract class SingletonBehaviour<T> : ISingletonBehaviour where T : SingletonBehaviour<T>
    {

        private static void Create()
        {
            if (!IsCreated) SetInstance((T)Activator.CreateInstance(typeof(T)));
        }

        private static T instance;
        public static T Instance
        {

            get
            {

                if (!IsCreated) Create();

                return instance;
            }

        }

        public static bool IsCreated => instance != null;

        private static void SetInstance(T newInstance)
        {
            if (newInstance == null) return;
            instance?.OnDestroy();
            instance = newInstance;
            if (instance != null)
            {
                instance.OnAwake();
                instance.Init();
                if (instance.ExecuteInOrder) SingletonCallStack.Insert(instance);
            }
        }

        protected virtual void OnAwake()
        {
        }

        private void Init() 
        {
            OnInit();
        }
        protected virtual void OnInit()
        {
        }
        
        public virtual bool ExecuteInOrder => true;

        public virtual int Priority => 0;

        public int CompareTo(ISingletonBehaviour other) => other == null ? 1 : Priority.CompareTo(other.Priority);
        public int CompareTo(IExecutableBehaviour other) => other == null ? 1 : Priority.CompareTo(other.Priority);

        public abstract void OnUpdate();
        public abstract void OnLateUpdate();
        public abstract void OnFixedUpdate();

        public virtual bool DestroyOnLoad => true;

        protected virtual void OnDestroy()
        {

            if (ExecuteInOrder) SingletonCallStack.Remove(this);
            if (instance == this) instance = null;

            OnDestroyed();

        }

        public virtual void OnDestroyed() { }

        protected static bool isQuitting;

        public static bool IsQuitting() => isQuitting;

        protected void OnApplicationQuit()
        {

            isQuitting = true;

            OnQuit();

        }

        public virtual void OnQuit() { }

    }
#endif

    public interface IExecutableBehaviour : IComparable<IExecutableBehaviour>
    {
        /// <summary>
        /// Used for order of execution in the call stack, if using it. Lower values are called sooner.
        /// </summary>
        public int Priority { get; }

        public void OnUpdate();
        public void OnLateUpdate();
        public void OnFixedUpdate();
    }
    public interface ISingletonBehaviour : IExecutableBehaviour, IComparable<ISingletonBehaviour>
    {

        /// <summary>
        /// Does this behaviour use the call stack? (default: true) If it doesn't, the behaviour must be updated manually or by the engine.
        /// </summary>
        public bool ExecuteInStack { get; } 

    }

    internal class SingletonCallStack : SingletonBehaviour<SingletonCallStack>
    {

        public override bool ExecuteInStack => false;
        public override bool DestroyOnLoad => false;

        protected List<IExecutableBehaviour> behaviours = new List<IExecutableBehaviour>();

        public bool InsertLocal(IExecutableBehaviour behaviour)
        {
            if (behaviour == null || behaviours == null || ReferenceEquals(behaviour, this)) return false;
            if (behaviours.Contains(behaviour)) return false;

            behaviours.Add(behaviour);
            behaviours.Sort(); // Recalculate execution order.

            return true; 
        }

        public static bool Insert(IExecutableBehaviour behaviour)
        {

            var instance = Instance;
            if (instance == null) return false;
            return instance.InsertLocal(behaviour);

        }

        public bool RemoveLocal(IExecutableBehaviour behaviour)
        {
            if (behaviour == null || behaviours == null) return false;
            return behaviours.RemoveAll(i => ReferenceEquals(i, behaviour)) > 0;
        }

        public static bool Remove(IExecutableBehaviour behaviour)
        {

            var instance = Instance;
            if (instance == null) return false;
            return instance.RemoveLocal(behaviour);

        }

        private bool hasNull;

        private void RemoveNull()
        {
            if (hasNull)
            {
                behaviours.RemoveAll(i => i == null);
                hasNull = false;
            }
        }

        public static event VoidParameterlessDelegate PreUpdate;
        public static event VoidParameterlessDelegate PostUpdate;

        /// <summary>
        /// Executes the OnUpdate call stack. Must be called manually if not using an engine.
        /// </summary>
        public override void OnUpdate()
        {
            if (behaviours == null || IsQuitting()) return;

            PreUpdate?.Invoke();

            for (int i = 0; i < behaviours.Count; i++) 
            {
                var behaviour = behaviours[i];
                if (behaviour == null) 
                {
                    hasNull = true;
                    continue;
                }
                try
                {
                    behaviours[i].OnUpdate();
                }
                catch (Exception e)
                {
                    swole.LogError(e); 
                }
            }
            RemoveNull();

            PostUpdate?.Invoke();
        }

        public static event VoidParameterlessDelegate PreLateUpdate;
        public static event VoidParameterlessDelegate PostLateUpdate;

        /// <summary>
        /// Executes the OnLateUpdate call stack. Must be called manually if not using an engine.
        /// </summary>
        public override void OnLateUpdate()
        {
            if (behaviours == null || IsQuitting()) return;

            PreLateUpdate?.Invoke();

            for (int i = 0; i < behaviours.Count; i++)
            {
                var behaviour = behaviours[i]; 
                if (behaviour == null)
                {
                    hasNull = true;
                    continue;
                }
                try
                {
                    behaviours[i].OnLateUpdate();
                } 
                catch(Exception e)
                {
                    swole.LogError(e);
                }
            }
            RemoveNull();

            PostLateUpdate?.Invoke();
        }

        public static event VoidParameterlessDelegate PreFixedUpdate;
        public static event VoidParameterlessDelegate PostFixedUpdate;

        /// <summary>
        /// Executes the OnFixedUpdate call stack. Must be called manually if not using an engine.
        /// </summary>
        public override void OnFixedUpdate()
        {
            if (behaviours == null || IsQuitting()) return;

            PreFixedUpdate?.Invoke();

            for (int i = 0; i < behaviours.Count; i++)
            {
                var behaviour = behaviours[i];
                if (behaviour == null)
                {
                    hasNull = true;
                    continue;
                }
                behaviours[i].OnFixedUpdate();
            }
            RemoveNull();

            PostFixedUpdate?.Invoke();
        }

        /* ---- Revised to be handled by the engine hook instead.
        
        private void Update() => OnUpdate(); // Recgonized by Unity when using it

        private void LateUpdate() => OnLateUpdate(); // Recgonized by Unity when using it

        private void FixedUpdate() => OnFixedUpdate(); // Recgonized by Unity when using it

        */

        public override void OnDestroyed()
        {

            base.OnDestroyed();

            behaviours?.Clear();
            behaviours = null;

        }

        public static void Execute()
        {
            var instance = Instance;
            if (instance == null) return;
            instance.OnUpdate();
        }

        public static void ExecuteLate()
        {
            var instance = Instance;
            if (instance == null) return;
            instance.OnLateUpdate();
        }

        public static void ExecuteFixed()
        {
            var instance = Instance;
            if (instance == null) return;
            instance.OnFixedUpdate();
        }

    }

}
