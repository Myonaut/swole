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

        public void Dispose()
        {

            if (disposables == null) return;

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

            disposables = null;

        }

        public static void Track(IDisposable disposer) 
        {
            var instance = Instance;
            if (instance == null || instance.disposables == null) return;

            instance.disposables.Add(disposer);  
        }
        /// <summary>
        /// NOTE: A disposable struct should be wrapped in a disposable class if you plan on allowing it to be untracked.
        /// </summary>
        public static bool Untrack(IDisposable disposer) 
        {
            var instance = Instance;
            if (instance == null || instance.disposables == null) return false;

            return instance.disposables.RemoveAll(i => ReferenceEquals(i, disposer)) > 0; 
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
