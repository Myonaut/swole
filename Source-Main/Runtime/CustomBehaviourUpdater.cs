using System;
using System.Collections;
using System.Collections.Generic;

namespace Swole.API
{
    public abstract class CustomBehaviourUpdater<T0, T1> : SingletonBehaviour<T0> where T0 : CustomBehaviourUpdater<T0, T1> where T1 : ICustomUpdatableBehaviour
    {

        protected readonly List<T1> behaviours = new List<T1>();

        protected readonly List<T1> toRegister = new List<T1>();
        public virtual void RegisterLocal(T1 b)
        {
            if (!toRegister.Contains(b)) toRegister.Add(b);
        }

        protected readonly List<T1> toRemove = new List<T1>(); 
        public virtual void UnregisterLocal(T1 b)
        {
            toRegister.Remove(b);
            toRemove.Add(b);
        }

        public static void Register(T1 b)
        {
            var instance = Instance;
            if (instance == null) return;

            instance.RegisterLocal(b);
        }

        public static void Unregister(T1 b)
        {
            var instance = InstanceOrNull;
            if (instance == null) return;

            instance.UnregisterLocal(b);
        }

        public bool IsRegisteredLocal(T1 b) => behaviours.Contains(b);
        public static bool IsRegistered(T1 b)
        {
            var instance = InstanceOrNull;
            if (instance == null) return false;

            return instance.IsRegisteredLocal(b);
        }

        public virtual void RegisterQueued()
        {
            if (toRegister.Count > 0)
            {
                foreach (var behaviour in toRegister)
                {
                    if (!behaviours.Contains(behaviour)) behaviours.Add(behaviour);
                }

                toRegister.Clear();
            }
        }
        public virtual void RemoveQueued()
        {
            if (toRemove.Count > 0)
            {
                foreach (var b in toRemove) if (b != null) behaviours.Remove(b);
                toRemove.Clear();

#if UNITY_2017_1_OR_NEWER
                behaviours.RemoveAll(b => b == null || (b is UnityEngine.Behaviour b_ && b_ == null));
#else
                behaviours.RemoveAll(b => b == null);
#endif
            }
        }

        public virtual bool CallFixedUpdate => true;

        public override void OnFixedUpdate()
        {
            if (CallFixedUpdate)
            {
                foreach (var b in behaviours)
                {
                    if (b != null) b.CustomFixedUpdate();
                }
            }
        }

        public virtual bool CallUpdate => true;

        public override void OnUpdate()
        {
            RegisterQueued();

            if (CallUpdate)
            {
                foreach (var b in behaviours)
                {
                    if (b != null) b.CustomUpdate();
                }
            }
        }

        public virtual bool CallLateUpdate => true;

        public override void OnLateUpdate()
        {
            if (CallLateUpdate)
            {
                foreach (var b in behaviours)
                {
                    if (b != null) b.CustomLateUpdate();
                }
            }

            RemoveQueued();
        }

    }

    public interface ICustomUpdatableBehaviour
    {
        public void CustomUpdate();
        public void CustomLateUpdate();
        public void CustomFixedUpdate();
    }
}
