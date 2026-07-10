#if UNITY_2017_1_OR_NEWER

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Unity.Collections.LowLevel.Unsafe;

namespace Swole
{
    public class PersistentJobDataTracker : SingletonBehaviour<PersistentJobDataTracker>, IDisposable
    {
        protected readonly Dictionary<int, ComputeBuffer> emptyBuffers = new Dictionary<int, ComputeBuffer>();
        public static ComputeBuffer GetEmptyBuffer(int stride)
        {
            var instance = Instance;
            if (instance == null) return null;

            if (!instance.emptyBuffers.TryGetValue(stride, out var buffer) || buffer == null || !buffer.IsValid())
            {
                buffer = new ComputeBuffer(1, stride);
                instance.emptyBuffers[stride] = buffer;
            }

            return buffer;
        }
        public static ComputeBuffer GetEmptyBuffer<T>() where T : struct
        {
            return GetEmptyBuffer(UnsafeUtility.SizeOf<T>());
        }
        public static ComputeBuffer GetEmptyBuffer(Type type)
        {
            return GetEmptyBuffer(UnsafeUtility.SizeOf(type));
        }

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

            if (emptyBuffers != null)
            {
                foreach(var entry in emptyBuffers)
                {
                    if (entry.Value != null && entry.Value.IsValid())
                    {
                        entry.Value.Release();
                    }
                } 
                emptyBuffers.Clear();
            }

            if (disposables != null)
            {

                disposing = true;
                foreach (IDisposable disposable in disposables)
                {
                    try
                    {
                        disposable.Dispose();
                    }
                    catch (Exception ex)
                    {
                        swole.LogError($"Encountered an exception while disposing persistent job data!");
                        swole.LogError(ex);
                    }
                }
                disposing = false;

                disposables.Clear();
                disposables = null;

            }
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
