#if UNITY_2017_1_OR_NEWER

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole
{
    /// <summary>
    /// Tracks objects that should be destroyed/disposed when the scene is changed.
    /// </summary>
    public class SceneDataTracker : SingletonBehaviour<SceneDataTracker>, IDisposable
    {

        // Refrain from update calls
        public override bool ExecuteInStack => false;
        public override void OnUpdate() { }
        public override void OnLateUpdate() { }
        public override void OnFixedUpdate() { }
        //

        public override bool DestroyOnLoad => true;

        protected List<IDisposable> disposables = new List<IDisposable>();
        protected List<UnityEngine.Object> unityObjects = new List<UnityEngine.Object>();

        private bool disposing;
        public void Dispose()
        {

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
                        swole.LogError($"Encountered an exception while disposing data!");
                        swole.LogError(ex);
                    }
                }
                disposing = false;

                disposables.Clear();
                disposables = null;

            }

            if (unityObjects != null)
            {

                foreach (var obj in unityObjects)
                {
                    try
                    {
                        GameObject.Destroy(obj);
                    }
                    catch (Exception ex)
                    {
                        swole.LogError($"Encountered an exception while destroying unity object!");
                        swole.LogError(ex);
                    }
                }

                unityObjects.Clear();
                unityObjects = null;
            }

        }

        public static bool Track(IDisposable disposer)
        {
            var instance = Instance;
            if (instance == null || instance.disposables == null) return false;

            instance.disposables.Add(disposer);
            return true;
        }
        public static PersistentJobDataTracker.WrappedDisposable WrapAndTrack(IDisposable disposer)
        {
            var instance = Instance;
            if (instance == null || instance.disposables == null) return null;

            var wrapper = new PersistentJobDataTracker.WrappedDisposable(disposer);
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

            return instance.disposing ? true : instance.disposables.RemoveAll(i => ReferenceEquals(i, disposer) || (i != null && i.Equals(disposer)) || (i is PersistentJobDataTracker.WrappedDisposable wrapper && (ReferenceEquals(wrapper.disposable, disposer) || (wrapper.disposable != null && wrapper.disposable.Equals(disposer))))) > 0;
        }

        public static bool Track(UnityEngine.Object obj)
        {
            var instance = Instance;
            if (instance == null || instance.unityObjects == null) return false;

            instance.unityObjects.Add(obj);
            return true;
        }
        public static bool Untrack(UnityEngine.Object obj)
        {
            var instance = InstanceOrNull;
            if (instance == null || instance.unityObjects == null) return false;

            return instance.disposables.RemoveAll(i => ReferenceEquals(i, obj)) > 0;
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
