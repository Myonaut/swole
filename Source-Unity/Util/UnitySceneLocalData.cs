#if (UNITY_EDITOR || UNITY_STANDALONE)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace Swole
{
    public class UnitySceneLocalData : SingletonBehaviour<UnitySceneLocalData>
    {
        // Refrain from update calls
        public override bool ExecuteInStack => false;
        public override void OnUpdate() { }
        public override void OnLateUpdate() { }
        public override void OnFixedUpdate() { }
        //

        public override bool DestroyOnLoad => false; 

        protected override void OnInit()
        {
            base.OnInit();
            SceneManager.activeSceneChanged += OnSceneChange;
        }

        public override void OnDestroyed()
        {
            base.OnDestroyed();
            SceneManager.activeSceneChanged -= OnSceneChange;
        }

        protected void OnSceneChange(Scene oldScene, Scene newScene)
        {
            PurgeDataLocal();
        }

        protected readonly List<UnityEngine.Object> trackedObjects = new List<UnityEngine.Object>();

        public void TrackLocal(UnityEngine.Object obj)
        {
            if (obj == null || trackedObjects.Contains(obj)) return;
            trackedObjects.Add(obj);
        }
        public void UntrackLocal(UnityEngine.Object obj)
        {
            if (obj == null) return;
            trackedObjects.RemoveAll(i => ReferenceEquals(i, obj));
        }

        public void Track(UnityEngine.Object obj)
        {
            var instance = Instance;
            if (instance == null) return;

            instance.TrackLocal(obj);
        }
        public void Untrack(UnityEngine.Object obj)
        {
            var instance = InstanceOrNull;
            if (instance == null) return; 

            instance.UntrackLocal(obj);
        }

        public void PurgeDataLocal()
        {
            foreach (var obj in trackedObjects)
            {
                if (obj == null) continue;

                try
                {
                    UnityEngine.Object.Destroy(obj);
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR
                    Debug.LogException(ex);
#else
                    swole.LogError(ex);
#endif
                }
            }

            trackedObjects.Clear();
        }

        public static void PurgeData()
        {
            var instance = InstanceOrNull;
            if (instance == null) return;

            instance.PurgeDataLocal();
        }

    }
}

#endif