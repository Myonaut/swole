#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole
{
    public class PersistentJobDataTracker : SingletonBehaviour<PersistentJobDataTracker>, IDisposable
    {

        // Refrain from update calls
        public override bool ExecuteInStack => false;
        public override void OnUpdate() { }
        public override void OnLateUpdate() { }
        public override void OnFixedUpdate() { }
        //

        public override bool DestroyOnLoad => false;

        protected List<IDisposable> disposables = new List<IDisposable>();

        private bool disposing;
        public void Dispose()
        {

            if (disposables == null) return;

            disposing = true;
            foreach (IDisposable disposable in disposables) 
            {
                try
                {
                    disposable.Dispose();
                }
                catch(Exception ex) 
                {
                    swole.LogError($"Encountered an exception while disposing persistent job data!");
                    swole.LogError(ex);
                }
            }
            disposing = false;

            disposables = null;

        }

        public static bool Track(IDisposable disposer) 
        {
            var instance = Instance;
            if (instance == null || instance.disposables == null) return false;

            instance.disposables.Add(disposer);
            return true;
        }
        public class WrappedDisposable : IDisposable, IEquatable<IDisposable>
        {
            public IDisposable disposable;
            private bool isDisposed;
            public bool IsDisposed => isDisposed;
            public void Dispose()
            {
                if (disposable != null) disposable.Dispose();
                disposable = default;
                isDisposed = true;
            }

            public bool Equals(IDisposable other)
            {
                if (other is WrappedDisposable wrapper) return ReferenceEquals(wrapper.disposable, disposable) || (disposable != null && disposable.Equals(wrapper.disposable)); 
                return ReferenceEquals(disposable, other) || (disposable != null && disposable.Equals(other));  
            }

            public WrappedDisposable(IDisposable disposable)
            {
                this.disposable = disposable;
            }
        }
        public static WrappedDisposable WrapAndTrack(IDisposable disposer)
        {
            var instance = Instance;
            if (instance == null || instance.disposables == null) return null;

            var wrapper = new WrappedDisposable(disposer);
            instance.disposables.Add(wrapper);
            return wrapper;
        }
        /// <summary>
        /// NOTE: For guaranteed consistency, a disposable struct should be wrapped in a disposable class if you plan on allowing it to be untracked. You can easily create a wrapped disposable using this class's WrapAndTrack method.
        /// </summary>
        public static bool Untrack(IDisposable disposer) 
        {
            var instance = InstanceOrNull;
            if (instance == null || instance.disposables == null) return false;

            return instance.disposing ? true : instance.disposables.RemoveAll(i => ReferenceEquals(i, disposer) || (i != null && i.Equals(disposer)) || (i is WrappedDisposable wrapper && (ReferenceEquals(wrapper.disposable, disposer) || (wrapper.disposable != null && wrapper.disposable.Equals(disposer))))) > 0;    
        }

        public override void OnDestroyed()
        {

            base.OnDestroyed();

            Dispose();

        }

        public override void OnQuit()
        {

            base.OnQuit();

            Dispose();

        }

    }
}

#endif
