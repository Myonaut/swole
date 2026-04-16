using System;
using System.Collections;
using System.Collections.Generic;

namespace Swole.API
{
    public abstract class CustomBehaviourUpdater<T> : SingletonBehaviour<CustomBehaviourUpdater<T>> where T : ICustomUpdatableBehaviour
    {

        protected readonly List<T> behaviours = new List<T>();  

        public virtual void RegisterLocal(T b)
        {
            if (!behaviours.Contains(b)) behaviours.Add(b);
        }

        private readonly List<T> toRemove = new List<T>();
        public virtual void UnregisterLocal(T b)
        {
            toRemove.Add(b);
        }

        public static void Register(T b)
        {
            var instance = Instance;
            if (instance == null) return;

            instance.RegisterLocal(b);
        }

        public static void Unregister(T b)
        {
            var instance = InstanceOrNull;
            if (instance == null) return;

            instance.UnregisterLocal(b);
        }

        public override void OnFixedUpdate()
        {
            foreach (var b in behaviours)
            {
                if (b != null) b.CustomFixedUpdate();
            }
        }

        public override void OnUpdate()
        {
            foreach (var b in behaviours)
            {
                if (b != null) b.CustomUpdate();
            }
        }

        public override void OnLateUpdate()
        {
            foreach (var bone in behaviours)
            {
                if (bone != null) bone.CustomLateUpdate();
            }

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

    }

    public interface ICustomUpdatableBehaviour
    {
        public void CustomUpdate();
        public void CustomLateUpdate();
        public void CustomFixedUpdate();
    }
}
