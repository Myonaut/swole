#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Jobs;

using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections.LowLevel.Unsafe;

namespace Swole.API.Unity
{
    public class Rigs : SingletonBehaviour<Rigs>
    {

        protected int lastInputDependencyFrame, lastOutputDependencyFrame;

        protected JobHandle inputDependency, outputDependency;

        public static JobHandle InputDependency
        {

            get
            {

                var instance = Instance;

                if (instance.lastInputDependencyFrame != Time.frameCount)
                {

                    instance.lastInputDependencyFrame = Time.frameCount;
                    instance.inputDependency = new JobHandle();

                }

                return instance.inputDependency;

            }

        }

        public static JobHandle OutputDependency
        {

            get
            {

                var instance = Instance;

                if (instance.lastOutputDependencyFrame != Time.frameCount)
                {

                    instance.lastOutputDependencyFrame = Time.frameCount;
                    instance.outputDependency = new JobHandle();

                }

                return instance.outputDependency;

            }

        }

        public static JobHandle AddInputDependency(JobHandle dependency)
        {

            var instance = Instance;

            if (instance.lastInputDependencyFrame != Time.frameCount)
            {

                instance.lastInputDependencyFrame = Time.frameCount;
                instance.inputDependency = new JobHandle();

            }

            instance.inputDependency = JobHandle.CombineDependencies(instance.inputDependency, dependency);

            return instance.inputDependency;

        }

        public static JobHandle AddOutputDependency(JobHandle dependency)
        {

            var instance = Instance;

            if (instance.lastOutputDependencyFrame != Time.frameCount)
            {

                instance.lastOutputDependencyFrame = Time.frameCount;
                instance.outputDependency = new JobHandle();

            }

            instance.outputDependency = JobHandle.CombineDependencies(instance.outputDependency, dependency);

            return instance.outputDependency;

        }

        public class Sampler : IDisposable
        {

            protected bool invalid;
            public bool Valid => !invalid;

            public Sampler(SkinnedMeshRenderer renderer)
            {

                if (renderer != null && renderer.sharedMesh != null)
                {

                    m_Renderer = renderer;

                    m_RendererBones = renderer.bones;

                    m_Trackers = new NativeArray<int>(m_RendererBones.Length, Allocator.Persistent);
                    m_Pose = new NativeArray<float4x4>(m_RendererBones.Length, Allocator.Persistent);
                    m_TrackedBones = new NativeList<int>(4, Allocator.Persistent);


                    Matrix4x4[] bindpose = m_Renderer.sharedMesh.bindposes;

                    m_Bindpose = new NativeArray<float4x4>(bindpose.Length, Allocator.Persistent);
                    for (int a = 0; a < bindpose.Length; a++) m_Bindpose[a] = (float4x4)bindpose[a];

                }

            }

            public Sampler(SkinnedMeshRenderer renderer, Matrix4x4[] bindpose)
            {

                if (renderer != null && bindpose != null)
                {

                    m_Renderer = renderer;

                    m_RendererBones = renderer.bones;

                    m_Trackers = new NativeArray<int>(m_RendererBones.Length, Allocator.Persistent);
                    m_Pose = new NativeArray<float4x4>(m_RendererBones.Length, Allocator.Persistent);
                    m_TrackedBones = new NativeList<int>(4, Allocator.Persistent);

                    m_Bindpose = new NativeArray<float4x4>(bindpose.Length, Allocator.Persistent);
                    for (int a = 0; a < bindpose.Length; a++) m_Bindpose[a] = (float4x4)bindpose[a];

                }

            }

            protected SkinnedMeshRenderer m_Renderer;
            public SkinnedMeshRenderer Renderer => m_Renderer;

            protected Transform[] m_RendererBones;

            public Transform GetBone(int index) => m_RendererBones == null ? null : m_RendererBones[index];
            public int BoneCount => m_RendererBones == null ? 0 : m_RendererBones.Length;

            protected TransformAccessArray m_Bones;
            public TransformAccessArray Bones => m_Bones;

            protected virtual void UpdateTransformAccess()
            {

                Dependency.Complete(); 

                if (m_Renderer == null)
                {

                    Dispose();

                    return;

                }

                if (m_Bones.isCreated) m_Bones.Dispose();

                Transform[] transforms = new Transform[m_TrackedBones.Length];

                if (m_RendererBones == null) m_RendererBones = m_Renderer.bones;

                for (int a = 0; a < m_TrackedBones.Length; a++) transforms[a] = m_RendererBones[m_TrackedBones[a]];

                m_Bones = new TransformAccessArray(transforms);

            }

            protected NativeArray<float4x4> m_Bindpose;
            public NativeArray<float4x4> Bindpose => m_Bindpose;

            protected NativeArray<float4x4> m_Pose;
            public NativeArray<float4x4> Pose => m_Pose;

            protected int lastManagedPoseFrame;

            protected Matrix4x4[] m_ManagedPose;
            public Matrix4x4[] ManagedPose
            {

                get
                {

                    int frame = Time.frameCount;

                    if (lastManagedPoseFrame != frame || m_ManagedPose == null)
                    {

                        Dependency.Complete();

                        if (m_ManagedPose == null) m_ManagedPose = new Matrix4x4[m_Pose.Length];

                        for (int a = 0; a < m_Pose.Length; a++) m_ManagedPose[a] = m_Pose[a];

                        lastManagedPoseFrame = frame;

                    }

                    return m_ManagedPose;

                }

            }

            protected int lastBufferFrame;
            protected ComputeBuffer m_Buffer;
            public ComputeBuffer Buffer
            {

                get
                {

                    UpdateBuffer();

                    return m_Buffer;

                }

            }
            public void UpdateBuffer()
            {

                int frame = Time.frameCount;

                if (lastBufferFrame != frame || m_Buffer == null)
                {

                    Dependency.Complete();

                    if (m_Buffer == null) m_Buffer = new ComputeBuffer(m_Pose.Length, UnsafeUtility.SizeOf(typeof(float4x4)), ComputeBufferType.Structured, ComputeBufferMode.SubUpdates);

                    var tempArray = m_Buffer.BeginWrite<float4x4>(0, m_Pose.Length);

                    m_Pose.CopyTo(tempArray);

                    m_Buffer.EndWrite<float4x4>(m_Pose.Length);

                    lastBufferFrame = frame;

                }

            }

            protected NativeArray<int> m_Trackers;

            protected NativeList<int> m_TrackedBones;
            public NativeList<int> TrackedBones => m_TrackedBones;

            public virtual void Dispose()
            {

                invalid = true;
                Dependency.Complete();

                if (m_Bones.isCreated) m_Bones.Dispose();
                if (m_Bindpose.IsCreated) m_Bindpose.Dispose();
                if (m_Pose.IsCreated) m_Pose.Dispose();
                if (m_TrackedBones.IsCreated) m_TrackedBones.Dispose();
                if (m_Trackers.IsCreated) m_Trackers.Dispose();

                m_Bones = default;
                m_Bindpose = default;
                m_Pose = default;
                m_TrackedBones = default;
                m_Trackers = default;

                if (m_Buffer != null) m_Buffer.Dispose();

                m_Buffer = null;

            }

            public void Track(int boneIndex)
            {

                if (!Valid) return;

                Dependency.Complete();

                m_Trackers[boneIndex] = m_Trackers[boneIndex] + 1;

                if (m_TrackedBones.Contains(boneIndex)) return;

                m_TrackedBones.Add(boneIndex);

                UpdateTransformAccess();

            }

            public void Untrack(int boneIndex)
            {

                if (!Valid) return;

                Dependency.Complete();

                int trackers = math.max(0, m_Trackers[boneIndex] - 1);

                m_Trackers[boneIndex] = trackers;

                if (trackers <= 0)
                {

                    int i = m_TrackedBones.IndexOf(boneIndex);

                    if (i >= 0)
                    {

                        m_TrackedBones.RemoveAt(i);

                        UpdateTransformAccess();

                    }

                }

            }

            public void TrackAll()
            {

                if (m_RendererBones == null || !Valid) return;

                Dependency.Complete();

                m_TrackedBones.Clear();

                for (int a = 0; a < m_RendererBones.Length; a++)
                {

                    m_Trackers[a] = m_Trackers[a] + 1;

                    m_TrackedBones.Add(a);

                }

                UpdateTransformAccess();

            }

            public void UntrackAll()
            {

                if (!Valid) return;

                Dependency.Complete();

                for (int a = 0; a < m_Trackers.Length; a++)
                {

                    int trackers = math.max(0, m_Trackers[a] - 1);

                    m_Trackers[a] = trackers;

                    if (trackers <= 0)
                    {

                        int i = m_TrackedBones.IndexOf(a);

                        if (i >= 0) m_TrackedBones.RemoveAt(i);

                    }

                }

                UpdateTransformAccess();

            }

            /* Moved this behaviour inside the pose update job
            public float4x4 GetTransformationMatrix(int boneIndex)
            {

                return math.mul(Pose[boneIndex], Bindpose[boneIndex]);

            }
            */

            protected int m_LastRefreshFrame;
            public int LastRefreshFrame => m_LastRefreshFrame;

            protected JobHandle m_JobsHandle = default;
            public JobHandle Dependency => m_JobsHandle;

            public JobHandle Refresh(JobHandle inputDeps = default, bool force = false)
            {

                if (!Valid) return Dependency;

                int frame = Time.frameCount;

                if (LastRefreshFrame == frame && !force) return Dependency;

                m_LastRefreshFrame = frame;

                if (m_TrackedBones.Length <= 0) return Dependency;

                Dependency.Complete();

                inputDeps = JobHandle.CombineDependencies(Dependency, inputDeps, InputDependency);
                m_JobsHandle = new UpdatePoseJob()
                {

                    poseArray = m_Pose,
                    trackedBones = m_TrackedBones,
                    bindpose = m_Bindpose

                }.Schedule(m_Bones, inputDeps);

                AddOutputDependency(Dependency);

                return Dependency;

            }

            [BurstCompile]
            public struct UpdatePoseJob : IJobParallelForTransform
            {

                [NativeDisableParallelForRestriction]
                public NativeArray<float4x4> poseArray;

                [ReadOnly]
                public NativeArray<int> trackedBones;

                [ReadOnly]
                public NativeArray<float4x4> bindpose;

                public void Execute(int index, TransformAccess transform)
                {

                    //poseArray[trackedBones[index]] = (float4x4)transform.localToWorldMatrix;

                    int boneIndex = trackedBones[index];

                    poseArray[boneIndex] = math.mul((float4x4)transform.localToWorldMatrix, bindpose[boneIndex]);

                }

            }

        }

        /// <summary>
        /// Uses a provided bone array instead of fetching it from a skinned renderer
        /// </summary>
        public class StandaloneSampler : Sampler
        {

            protected string m_id;
            public string ID => m_id;

            public StandaloneSampler(string id, Transform[] bones, Matrix4x4[] bindpose) : base(null)
            {

                m_id = id;

                m_RendererBones = bones;

                m_Trackers = new NativeArray<int>(m_RendererBones.Length, Allocator.Persistent);
                m_Pose = new NativeArray<float4x4>(m_RendererBones.Length, Allocator.Persistent);
                m_TrackedBones = new NativeList<int>(4, Allocator.Persistent);
                m_Bindpose = new NativeArray<float4x4>(bindpose.Length, Allocator.Persistent);
                for (int a = 0; a < bindpose.Length; a++) m_Bindpose[a] = (float4x4)bindpose[a];

            }

            protected override void UpdateTransformAccess()
            {

                Dependency.Complete();

                if (m_RendererBones == null)
                {

                    Dispose();

                    return;

                }

                if (m_Bones.isCreated) m_Bones.Dispose();

                Transform[] transforms = new Transform[m_TrackedBones.Length];

                for (int a = 0; a < m_TrackedBones.Length; a++)
                {

                    var bone = m_RendererBones[m_TrackedBones[a]];

                    if (bone == null)
                    {

                        Dispose();

                        return;

                    }

                    transforms[a] = bone;

                }

                m_Bones = new TransformAccessArray(transforms);

            }

        }

        private Dictionary<int, Sampler> activeSamplers = new Dictionary<int, Sampler>();
        private Dictionary<string, StandaloneSampler> activeStandaloneSamplers = new Dictionary<string, StandaloneSampler>();

        public override void OnDestroyed()
        {

            base.OnDestroyed();

            if (activeSamplers != null)
            {

                foreach (var entry in activeSamplers) if (entry.Value != null) entry.Value.Dispose();

                activeSamplers = null;

            }

            if (activeStandaloneSamplers != null)
            {

                foreach (var entry in activeStandaloneSamplers) if (entry.Value != null) entry.Value.Dispose();

                activeStandaloneSamplers = null;

            }

        }

        public static Sampler GetSampler(SkinnedMeshRenderer renderer)
        {

            int instanceID = renderer.GetInstanceID();

            Rigs singleton = Instance;

            if (!singleton.activeSamplers.TryGetValue(instanceID, out Sampler sampler))
            {

                sampler = new Sampler(renderer);

                singleton.activeSamplers[instanceID] = sampler;

            }

            return sampler;

        }

        public static bool CreateStandaloneSampler(string id, Transform[] bones, Matrix4x4[] bindpose, out StandaloneSampler sampler)
        {

            Rigs singleton = Instance;

            if (!singleton.activeStandaloneSamplers.TryGetValue(id, out sampler))
            {

                sampler = new StandaloneSampler(id, bones, bindpose);

                singleton.activeStandaloneSamplers[id] = sampler;

            }
            else return false;

            return true;

        }

        public static bool TryGetStandaloneSampler(string id, out StandaloneSampler sampler)
        {

            Rigs singleton = Instance;

            if (!singleton.activeStandaloneSamplers.TryGetValue(id, out sampler)) return false;

            return true;

        }

        public override bool ExecuteInStack => false;

        public override void OnUpdate() { }

        public override void OnLateUpdate() { }

        public override void OnFixedUpdate() { }

    }
}

#endif