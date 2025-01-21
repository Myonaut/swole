#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Jobs;

using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;

using Swole.API.Unity.Animation;

namespace Swole
{
    public class TransformTracking : SingletonBehaviour<TransformTracking>, IDisposable
    {

        public override bool ExecuteInStack => true;
        public static int ExecutionPriority => CustomAnimatorUpdater.FinalAnimationBehaviourPriority + 1; // update after animation
        public override int Priority => ExecutionPriority; 

        public override void OnFixedUpdate()
        {
        }

        private JobHandle inputDependency; 
        public void AddInputDependencyLocal(JobHandle inputDependency)
        {
            this.inputDependency = JobHandle.CombineDependencies(inputDependency, this.inputDependency);
        }
        public static void AddInputDependency(JobHandle inputDependency)
        {
            var instance = InstanceOrNull;
            if (instance == null) return;

            instance.AddInputDependencyLocal(inputDependency); 
        }
        public static JobHandle JobDependency => Instance.inputDependency;

        public override void OnLateUpdate()
        {
            new TrackTransformsJob()
            {
                trackedTransformsToWorld = trackedTransformsToWorld,
                trackedTransformsToLocal = trackedTransformsToLocal
            }.Schedule(trackedTransformsJobs, inputDependency).Complete();

            inputDependency = default;  
        }

        public override void OnUpdate()
        {
        }

        public void Dispose()
        {
            if (trackedTransformsJobs.isCreated) 
            {
                trackedTransformsJobs.Dispose();
                trackedTransformsJobs = default;
            }

            if (trackedTransformsToWorld.IsCreated)
            {
                trackedTransformsToWorld.Dispose();
                trackedTransformsToWorld = default;
            }
            if (trackedTransformsToLocal.IsCreated)
            {
                trackedTransformsToLocal.Dispose();
                trackedTransformsToLocal = default; 
            }
        }

        protected void OnDestroy()
        {
            Dispose();
        }

        private readonly List<TrackedTransform> trackedTransforms = new List<TrackedTransform>(100);

        public static TrackedTransform GetTrackedTransform(int index) => Instance.trackedTransforms[index];

        private TransformAccessArray trackedTransformsJobs;
        private NativeList<float4x4> trackedTransformsToWorld;
        private NativeList<float4x4> trackedTransformsToLocal;

        public static NativeList<float4x4> TrackedTransformsToWorld => Instance.trackedTransformsToWorld;
        public static NativeList<float4x4> TrackedTransformsToLocal => Instance.trackedTransformsToLocal;

        public static float4x4 GetTransformToWorld(int index) => Instance.trackedTransformsToWorld[index];
        public static float4x4 GetTransformToLocal(int index) => Instance.trackedTransformsToLocal[index];

        protected override void OnAwake()
        {
            base.OnAwake();

            trackedTransformsJobs = new TransformAccessArray(100);
            trackedTransformsToWorld = new NativeList<float4x4>(100, Allocator.Persistent);
            trackedTransformsToLocal = new NativeList<float4x4>(100, Allocator.Persistent); 
        }

        public TrackedTransform TrackLocal(Transform transform)
        {
            if (transform == null) return null;

            foreach (var t in trackedTransforms) if (t.transform == transform) return t;
             
            var tt = new TrackedTransform() { transform = transform, Index = trackedTransforms.Count };
            trackedTransforms.Add(tt);
            trackedTransformsJobs.Add(transform);
            trackedTransformsToWorld.Add(transform.localToWorldMatrix);
            trackedTransformsToLocal.Add(transform.worldToLocalMatrix);
            return tt;
        }
        public static TrackedTransform Track(Transform transform)
        {
            var instance = Instance;
            if (instance == null) return null;

            return instance.TrackLocal(transform);
        }

        public void RemoveLocal(int index)
        {
            var tt = trackedTransforms[index];
            var swapTT = trackedTransforms[trackedTransforms.Count - 1];
            trackedTransforms[index] = swapTT;
            trackedTransforms.RemoveAt(trackedTransforms.Count - 1);
            swapTT.Index = index;
            tt.Index = -1;
            trackedTransformsJobs.RemoveAtSwapBack(index); 
            trackedTransformsToWorld.RemoveAtSwapBack(index);
            trackedTransformsToLocal.RemoveAtSwapBack(index);
        }
        public void RemoveLocal(TrackedTransform trackedTransform)
        {
            RemoveLocal(trackedTransforms.IndexOf(trackedTransform));
        }
        public void RemoveLocal(Transform transform)
        {
            for (int a = 0; a < trackedTransforms.Count; a++)
            {
                if (trackedTransforms[a].transform == transform)
                {
                    RemoveLocal(a);
                    break;
                }
            }
        }
        public static void Remove(int index)
        {
            var instance = InstanceOrNull;
            if (instance == null) return;

            instance.RemoveLocal(index);
        }
        public static void Remove(TrackedTransform trackedTransform)
        {
            var instance = InstanceOrNull;
            if (instance == null) return;

            instance.RemoveLocal(trackedTransform);
        }
        public static void Remove(Transform transform)
        {
            var instance = InstanceOrNull;
            if (instance == null) return;

            instance.RemoveLocal(transform);
        }

        public delegate void IndexChangedDelegate(int oldIndex, int newIndex);

        public class TrackedTransform : IDisposable
        {
            private int index;
            public int Index
            {
                get => index;
                set
                {
                    try
                    {
                        OnIndexChange?.Invoke(index, value);
                    } 
                    catch(Exception ex)
                    {
                        swole.LogError(ex);
                    }

                    index = value;
                }
            }

            public event IndexChangedDelegate OnIndexChange;

            public void RemoveAllListeners()
            {
                OnIndexChange = null;
            }

            public Transform transform;

            public bool IsValid => index >= 0 && transform != null;

            public void Dispose()
            {
                RemoveAllListeners();
                TransformTracking.Remove(this);
            }

            private int users;
            public int Users => users;
            public void AddUser(IndexChangedDelegate onIndexChange = null)
            {
                users++;
                OnIndexChange += onIndexChange;  
            }
            public void RemoveUser(IndexChangedDelegate onIndexChange = null, bool disposeIfNoUsers = true)
            {
                users--;
                OnIndexChange -= onIndexChange;

                if (users <= 0 && disposeIfNoUsers) Dispose();
            }
        }

        [BurstCompile]
        public struct TrackTransformsJob : IJobParallelForTransform
        {
            [NativeDisableParallelForRestriction]
            public NativeList<float4x4> trackedTransformsToWorld;
            [NativeDisableParallelForRestriction]
            public NativeList<float4x4> trackedTransformsToLocal;

            public void Execute(int index, TransformAccess transform)
            {
                trackedTransformsToWorld[index] = transform.localToWorldMatrix;
                trackedTransformsToLocal[index] = transform.worldToLocalMatrix; 
            }
        }
    }
}

#endif