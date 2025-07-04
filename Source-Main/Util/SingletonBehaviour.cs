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
        public virtual int UpdatePriority => Priority;
        public virtual int LateUpdatePriority => Priority;
        public virtual int FixedUpdatePriority => Priority;

        public int CompareTo(ISingletonBehaviour other) => other == null ? 1 : Priority.CompareTo(other.Priority);
        public int CompareTo(IExecutableBehaviour other) => other == null ? 1 : Priority.CompareTo(other.Priority);

        public abstract void OnUpdate();
        public abstract void OnLateUpdate();
        public abstract void OnFixedUpdate();

        public virtual void OnPreFixedUpdate() { }
        public virtual void OnPostFixedUpdate() { }

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
        public virtual int UpdatePriority => Priority;
        public virtual int LateUpdatePriority => Priority;
        public virtual int FixedUpdatePriority => Priority;

        public int CompareTo(ISingletonBehaviour other) => other == null ? 1 : Priority.CompareTo(other.Priority);
        public int CompareTo(IExecutableBehaviour other) => other == null ? 1 : Priority.CompareTo(other.Priority);

        public abstract void OnUpdate();
        public abstract void OnLateUpdate();
        public abstract void OnFixedUpdate();

        public virtual void OnPreFixedUpdate() { }
        public virtual void OnPostFixedUpdate() { }

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

#if (UNITY_STANDALONE || UNITY_EDITOR)
    public abstract class ExecutableBehaviourObject : MonoBehaviour, IExecutableBehaviour
    {
        public abstract int Priority { get; }
        public virtual int UpdatePriority => Priority;
        public virtual int LateUpdatePriority => Priority;
        public virtual int FixedUpdatePriority => Priority;

        public int CompareTo(IExecutableBehaviour other) => other == null ? 1 : Priority.CompareTo(other.Priority);

        public virtual void OnFixedUpdate() {}

        public virtual void OnLateUpdate() {}

        public virtual void OnUpdate() {}

        public virtual void OnPreFixedUpdate() { }
        public virtual void OnPostFixedUpdate() { }

    }
#else
    public abstract class ExecutableBehaviourObject : IExecutableBehaviour
    {
        public abstract int Priority { get; }

        public int CompareTo(IExecutableBehaviour other) => other == null ? 1 : Priority.CompareTo(other.Priority);

        public virtual void OnFixedUpdate() {}

        public virtual void OnLateUpdate() {}

        public virtual void OnUpdate() {}
    }
#endif

    public interface IExecutableBehaviour : IComparable<IExecutableBehaviour>
    {
        /// <summary>
        /// Used for order of execution in the call stack, if using it. Lower values are called sooner.
        /// </summary>
        public int Priority { get; }
        public int UpdatePriority { get; }
        public int LateUpdatePriority { get; }
        public int FixedUpdatePriority { get; }

        public void OnUpdate();
        public void OnLateUpdate();
        public void OnFixedUpdate();

        public void OnPreFixedUpdate();
        public void OnPostFixedUpdate();
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

        public static int CompareUpdatePriority(IExecutableBehaviour a, IExecutableBehaviour b) => Math.Sign(a.UpdatePriority - b.UpdatePriority);
        public static int CompareLateUpdatePriority(IExecutableBehaviour a, IExecutableBehaviour b) => Math.Sign(a.LateUpdatePriority - b.LateUpdatePriority);
        public static int CompareFixedUpdatePriority(IExecutableBehaviour a, IExecutableBehaviour b) => Math.Sign(a.FixedUpdatePriority - b.FixedUpdatePriority);

        public override bool ExecuteInStack => false;
        public override bool DestroyOnLoad => false;

        protected List<IExecutableBehaviour> behaviours = new List<IExecutableBehaviour>();

        protected List<IExecutableBehaviour> behavioursUpdateSorted = new List<IExecutableBehaviour>();
        protected List<IExecutableBehaviour> behavioursLateUpdateSorted = new List<IExecutableBehaviour>();
        protected List<IExecutableBehaviour> behavioursFixedUpdateSorted = new List<IExecutableBehaviour>();

        public void CopyAndSortBehaviours()
        {
            if (behavioursUpdateSorted == null) behavioursUpdateSorted = new List<IExecutableBehaviour>();
            behavioursUpdateSorted.Clear();
            behavioursUpdateSorted.AddRange(behaviours); 

            if (behavioursLateUpdateSorted == null) behavioursLateUpdateSorted = new List<IExecutableBehaviour>();
            behavioursLateUpdateSorted.Clear();
            behavioursLateUpdateSorted.AddRange(behaviours);

            if (behavioursFixedUpdateSorted == null) behavioursFixedUpdateSorted = new List<IExecutableBehaviour>();
            behavioursFixedUpdateSorted.Clear();
            behavioursFixedUpdateSorted.AddRange(behaviours);

            behavioursUpdateSorted.Sort(CompareUpdatePriority);
            behavioursLateUpdateSorted.Sort(CompareLateUpdatePriority);
            behavioursFixedUpdateSorted.Sort(CompareFixedUpdatePriority);
        }
        public void SortBehaviours()
        {
            if (behavioursUpdateSorted != null) behavioursUpdateSorted.Sort(CompareUpdatePriority);
            if (behavioursLateUpdateSorted != null) behavioursLateUpdateSorted.Sort(CompareLateUpdatePriority);
            if (behavioursFixedUpdateSorted != null) behavioursFixedUpdateSorted.Sort(CompareFixedUpdatePriority);
        }
        protected void SortBehavioursFast()
        {
            behavioursUpdateSorted.Sort(CompareUpdatePriority);
            behavioursLateUpdateSorted.Sort(CompareLateUpdatePriority);
            behavioursFixedUpdateSorted.Sort(CompareFixedUpdatePriority);
        }

        public bool InsertLocal(IExecutableBehaviour behaviour)
        {
            if (behaviour == null || behaviours == null || ReferenceEquals(behaviour, this)) return false;
            if (behaviours.Contains(behaviour)) return false;

            behaviours.Add(behaviour);
            //behaviours.Sort(); // Recalculate execution order.
            
            if (behavioursUpdateSorted == null || behavioursLateUpdateSorted == null || behavioursFixedUpdateSorted == null)
            {
                CopyAndSortBehaviours();
            } 
            else
            {
                behavioursUpdateSorted.Add(behaviour);
                behavioursLateUpdateSorted.Add(behaviour);
                behavioursFixedUpdateSorted.Add(behaviour);
                SortBehavioursFast();
            }

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
            bool removed = behaviours.RemoveAll(i => ReferenceEquals(i, behaviour)) > 0;

            if (removed) CopyAndSortBehaviours();

            return removed;
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
                hasNull = false;
                bool removed = behaviours.RemoveAll(i => i == null) > 0;
                if (removed) CopyAndSortBehaviours();
            }
        }

        public static event VoidParameterlessDelegate PreUpdate;
        public static event VoidParameterlessDelegate PostUpdate;

        /// <summary>
        /// Executes the OnUpdate call stack. Must be called manually if not using an engine.
        /// </summary>
        public override void OnUpdate()
        {
            if (behavioursUpdateSorted == null || IsQuitting()) return;

            PreUpdate?.Invoke();

            for (int i = 0; i < behavioursUpdateSorted.Count; i++) 
            {
                var behaviour = behavioursUpdateSorted[i];
                if (behaviour == null) 
                {
                    hasNull = true;
                    continue;
                }
                try
                {
                    behavioursUpdateSorted[i].OnUpdate();
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
            if (behavioursLateUpdateSorted == null || IsQuitting()) return;

            PreLateUpdate?.Invoke();

            for (int i = 0; i < behavioursLateUpdateSorted.Count; i++)
            {
                var behaviour = behavioursLateUpdateSorted[i]; 
                if (behaviour == null)
                {
                    hasNull = true;
                    continue;
                }
                try
                {
                    behavioursLateUpdateSorted[i].OnLateUpdate();
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
        /// Executes the OnPreFixedUpdate call stack. Must be called manually if not using an engine.
        /// </summary>
        public override void OnPreFixedUpdate()
        {
            if (behavioursFixedUpdateSorted == null || IsQuitting()) return;

            for (int i = 0; i < behavioursFixedUpdateSorted.Count; i++)
            {
                var behaviour = behavioursFixedUpdateSorted[i];
                if (behaviour == null)
                {
                    hasNull = true;
                    continue;
                }
                behavioursFixedUpdateSorted[i].OnPreFixedUpdate();
            }
        }
        /// <summary>
        /// Executes the OnFixedUpdate call stack. Must be called manually if not using an engine.
        /// </summary>
        public override void OnFixedUpdate()
        {
            if (behaviours == null || IsQuitting()) return;

            PreFixedUpdate?.Invoke();

            for (int i = 0; i < behavioursFixedUpdateSorted.Count; i++)
            {
                var behaviour = behavioursFixedUpdateSorted[i];
                if (behaviour == null)
                {
                    hasNull = true;
                    continue;
                }
                behavioursFixedUpdateSorted[i].OnFixedUpdate();
            }

            PostFixedUpdate?.Invoke();
        }
        /// <summary>
        /// Executes the OnPostFixedUpdate call stack. Must be called manually if not using an engine.
        /// </summary>
        public override void OnPostFixedUpdate()
        {
            if (behavioursFixedUpdateSorted == null || IsQuitting()) return;

            for (int i = 0; i < behavioursFixedUpdateSorted.Count; i++)
            {
                var behaviour = behavioursFixedUpdateSorted[i];
                if (behaviour == null)
                {
                    hasNull = true;
                    continue;
                }
                behavioursFixedUpdateSorted[i].OnPostFixedUpdate();
            }

            RemoveNull();
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

        public static void ExecutePreFixed()
        {
            var instance = Instance;
            if (instance == null) return;
            instance.OnPreFixedUpdate();
        }
        public static void ExecuteFixed()
        {
            var instance = Instance;
            if (instance == null) return;
            instance.OnFixedUpdate();
        }
        public static void ExecutePostFixed()
        {
            var instance = Instance;
            if (instance == null) return;
            instance.OnPostFixedUpdate();
        }

    }

}
