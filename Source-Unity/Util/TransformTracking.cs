#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

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
        public static int ExecutionPriority => CustomAnimatorUpdater.FinalAnimationBehaviourPriority + 5; // update after animation
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

    [Serializable]
    public struct TransformState : IEquatable<TransformState>
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 localScale;
        public TransformState(Vector3 position, Quaternion rotation, Vector3 localScale)
        {
            this.position = position;
            this.rotation = rotation;
            this.localScale = localScale;
        }
        public TransformState(Transform t, bool worldSpace = false)
        {
            if (worldSpace)
            {
                t.GetPositionAndRotation(out position, out rotation);
            } 
            else
            {
                t.GetLocalPositionAndRotation(out position, out rotation);
            }
            this.localScale = t.localScale;
        }



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object other)
        {
            if (!(other is TransformState))
            {
                return false;
            }

            return Equals((TransformState)other);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(TransformState other)
        {
            return position == other.position && rotation == other.rotation && localScale == other.localScale; 
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => base.GetHashCode();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(TransformState lhs, TransformState rhs)
        {
            return lhs.Equals(rhs);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(TransformState lhs, TransformState rhs)
        {
            return !(lhs == rhs);
        }



        public void ApplyLocal(Transform t)
        {
            t.SetLocalPositionAndRotation(position, rotation);
            t.localScale = localScale;
        }
        public void ApplyLocalPosition(Transform t)
        {
            t.localPosition = position;
        }
        public void ApplyLocalRotation(Transform t)
        {
            t.localRotation = rotation;
        }
        public void ApplyScale(Transform t)
        {
            t.localScale = localScale;
        }

        public void ApplyWorld(Transform t)
        {
            t.SetPositionAndRotation(position, rotation);
            t.localScale = localScale;
        }
        public void ApplyWorldPosition(Transform t)
        {
            t.position = position;
        }
        public void ApplyWorldRotation(Transform t)
        {
            t.rotation = rotation;
        }

        public static TransformStateDelta operator -(TransformState stateA, TransformState stateB) => new TransformStateDelta(stateA, stateB);
        public static TransformState operator +(TransformState state, TransformStateDelta delta)
        {
            state.position = state.position + delta.deltaPosition;
            state.rotation = delta.deltaRotation * state.rotation;
            state.localScale = state.localScale + delta.deltaScale;

            return state;
        }

        public static implicit operator TransformStateDelta(TransformState state) => new TransformStateDelta(state.position, state.rotation, state.localScale);
        public static implicit operator TransformState(TransformStateDelta delta) => new TransformState(delta.deltaPosition, delta.deltaRotation, delta.deltaScale);


    }

    [Serializable]
    public struct TransformStateDelta : IEquatable<TransformStateDelta>
    {
        public Vector3 deltaPosition;
        public Quaternion deltaRotation;
        public Vector3 deltaScale;

        public TransformStateDelta(Vector3 deltaPosition, Quaternion deltaRotation, Vector3 deltaScale)
        {
            this.deltaPosition = deltaPosition;
            this.deltaRotation = deltaRotation;
            this.deltaScale = deltaScale;
        }
        public TransformStateDelta(Vector3 localPositionA, Quaternion localRotationA, Vector3 localScaleA, Vector3 localPositionB, Quaternion localRotationB, Vector3 localScaleB)
        {
            deltaPosition = localPositionB - localPositionA;
            deltaRotation = Quaternion.Inverse(localRotationA) * localRotationB;
            deltaScale = localScaleB - localScaleA;
        }
        public TransformStateDelta(TransformState stateA, TransformState stateB)
        {
            deltaPosition = stateB.position - stateA.position;
            deltaRotation = Quaternion.Inverse(stateA.rotation) * stateB.rotation;
            deltaScale = stateB.localScale - stateA.localScale;
        }

        public static TransformStateDelta operator -(TransformStateDelta deltaA, TransformStateDelta deltaB) => new TransformStateDelta(deltaA, deltaB);
        public static TransformStateDelta operator +(TransformStateDelta deltaA, TransformStateDelta deltaB)
        {
            deltaA.deltaPosition = deltaA.deltaPosition + deltaB.deltaPosition;
            deltaA.deltaRotation = deltaB.deltaRotation * deltaA.deltaRotation;
            deltaA.deltaScale = deltaA.deltaScale + deltaB.deltaScale;

            return deltaA;
        }



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object other)
        {
            if (!(other is TransformStateDelta))
            {
                return false;
            }

            return Equals((TransformStateDelta)other);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(TransformStateDelta other)
        {
            return deltaPosition == other.deltaPosition && deltaRotation == other.deltaRotation && deltaScale == other.deltaScale;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => base.GetHashCode();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(TransformStateDelta lhs, TransformStateDelta rhs)
        {
            return lhs.Equals(rhs);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(TransformStateDelta lhs, TransformStateDelta rhs)
        {
            return !(lhs == rhs);
        }



        public void ApplyLocal(Transform t)
        {
            t.GetLocalPositionAndRotation(out var localPosition, out var localRotation);
            var localScale = t.localScale;

            localPosition = localPosition + deltaPosition;
            localRotation = deltaRotation * localRotation;
            localScale = localScale + deltaScale;

            t.SetLocalPositionAndRotation(localPosition, localRotation);
            t.localScale = localScale;
        }

        public void ApplyLocalPosition(Transform t)
        {
            var localPosition = t.localPosition;
            localPosition = deltaPosition + localPosition;
            t.localPosition = localPosition;
        }
        public void ApplyLocalRotation(Transform t)
        {
            var localRotation = t.localRotation;
            localRotation = deltaRotation * localRotation;
            t.localRotation = localRotation;
        }
        public void ApplyScale(Transform t)
        {
            var localScale = t.localScale;
            localScale = deltaScale + localScale;
            t.localScale = localScale;
        }

        public void ApplyWorld(Transform t)
        {
            t.GetPositionAndRotation(out var position, out var rotation);
            var localScale = t.localScale;

            position = position + deltaPosition;
            rotation = deltaRotation * rotation;
            localScale = localScale + deltaScale;

            t.SetPositionAndRotation(position, rotation);
            t.localScale = localScale;
        }

        public void ApplyWorldPosition(Transform t)
        {
            var position = t.position;
            position = deltaPosition + position;
            t.position = position;
        }
        public void ApplyWorldRotation(Transform t)
        {
            var rotation = t.rotation;
            rotation = deltaRotation * rotation;
            t.rotation = rotation;
        }

    }

}

#endif