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

using Swole.API.Unity.Animation;

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

                    instance.outputDependency.Complete();
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

        public class ComputeBufferWithStartIndex
        {
            protected ComputeBuffer buffer;
            public ComputeBuffer Buffer => buffer;

            public int startIndex;

            public ComputeBufferWithStartIndex(ComputeBuffer buffer, int startIndex)
            {
                this.buffer = buffer;
                this.startIndex = startIndex;
            }
        }
        public class InstanceBufferWithStartIndex
        {
            protected IInstanceBuffer buffer;
            public IInstanceBuffer Buffer => buffer;

            public int startIndex;

            public InstanceBufferWithStartIndex(IInstanceBuffer buffer, int startIndex)
            {
                this.buffer = buffer;
                this.startIndex = startIndex;
            }
        }

        public class Sampler : IDisposable
        {

            protected int m_instanceId;
            public virtual string ID => GetHashCode().ToString();

            protected bool invalid;
            public bool Valid => !invalid;

            protected static readonly List<Matrix4x4> tempMatrices = new List<Matrix4x4>(); 
            public Sampler(SkinnedMeshRenderer renderer)
            {

                if (renderer != null && renderer.sharedMesh != null)
                {
                    m_instanceId = renderer.GetInstanceID();

                    m_Renderer = renderer;

                    m_RendererBones = renderer.bones;

                    m_Pose = new NativeArray<float4x4>(m_RendererBones.Length, Allocator.Persistent);

                    tempMatrices.Clear();
                    m_Renderer.sharedMesh.GetBindposes(tempMatrices);

                    m_Bindpose = new NativeArray<float4x4>(tempMatrices.Count, Allocator.Persistent);
                    for (int a = 0; a < tempMatrices.Count; a++) m_Bindpose[a] = (float4x4)tempMatrices[a];
                    tempMatrices.Clear();
                }

            }

            public Sampler(SkinnedMeshRenderer renderer, Matrix4x4[] bindpose)
            {

                if (renderer != null && bindpose != null)
                {
                    m_instanceId = renderer.GetInstanceID();

                    m_Renderer = renderer;

                    m_RendererBones = renderer.bones;

                    m_Pose = new NativeArray<float4x4>(m_RendererBones.Length, Allocator.Persistent);

                    m_ManagedBindPose = bindpose;
                    m_Bindpose = new NativeArray<float4x4>(bindpose.Length, Allocator.Persistent);
                    for (int a = 0; a < bindpose.Length; a++) m_Bindpose[a] = (float4x4)bindpose[a];
                }

            }

            protected SkinnedMeshRenderer m_Renderer;
            public SkinnedMeshRenderer Renderer => m_Renderer;

            protected Transform[] m_RendererBones;

            public Transform GetBone(int index) => m_RendererBones == null ? null : m_RendererBones[index];
            public int BoneCount => m_RendererBones == null ? 0 : m_RendererBones.Length;

            protected Matrix4x4[] m_ManagedBindPose;
            protected Matrix4x4[] ManagedBindPose
            {
                get
                {
                    if (m_ManagedBindPose == null)
                    {
                        m_ManagedBindPose = new Matrix4x4[m_Bindpose.Length];
                        for (int a = 0; a < m_Pose.Length; a++) m_ManagedBindPose[a] = m_Bindpose[a];
                    }

                    return m_ManagedBindPose;
                }
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

                    if (m_Buffer == null)
                    {
                        Debug.Log("HUH");
                        if (m_Pose.Length <= 0)
                        {
                            swole.LogError($"Tried to create zero length compute buffer for rig sampler {ID}. Aborting...");
                            return null;
                        }

                        m_Buffer = new ComputeBuffer(m_Pose.Length, UnsafeUtility.SizeOf(typeof(float4x4)), ComputeBufferType.Structured, ComputeBufferMode.SubUpdates);
                        WriteToBuffers();
                    }

                    return m_Buffer;

                }

            }

            /*protected List<ComputeBufferWithStartIndex> m_Buffers;
            public void AddWritableBuffer(ComputeBuffer buffer, int startIndex)
            {
                if (m_Buffers == null) m_Buffers = new List<ComputeBufferWithStartIndex>();

                m_Buffers.Add(new ComputeBufferWithStartIndex(buffer, startIndex));
            }
            public void RemoveWritableBuffer(ComputeBuffer buffer)
            {
                if (m_Buffers == null) return;

                m_Buffers.RemoveAll(i => i == null || ReferenceEquals(i.Buffer, buffer));
            }*/
            protected List<InstanceBufferWithStartIndex> m_InstanceBuffers;
            public void AddWritableInstanceBuffer(IInstanceBuffer buffer, int startIndex)
            {
                if (m_InstanceBuffers == null) m_InstanceBuffers = new List<InstanceBufferWithStartIndex>();

                m_InstanceBuffers.Add(new InstanceBufferWithStartIndex(buffer, startIndex));
            }
            public void RemoveWritableInstanceBuffer(IInstanceBuffer buffer)
            {
                if (m_InstanceBuffers == null) return;

                m_InstanceBuffers.RemoveAll(i => i == null || ReferenceEquals(i.Buffer, buffer));
            }

            protected void WriteToBuffers()
            {
                if (trackingGroup == null) return;

                if (m_Buffer != null) trackingGroup.CopyIntoBuffer(m_Buffer, 0);
                //if (m_Buffers != null) trackingGroup.CopyIntoBuffers(m_Buffers);
                if (m_InstanceBuffers != null) trackingGroup.CopyIntoBuffers(m_InstanceBuffers); 
            }

            public void Dispose() => Dispose(true);         
            public virtual void Dispose(bool removeFromActiveSamplers)
            {

                StopTrackingPoseData();

                invalid = true;

                if (m_Bindpose.IsCreated) m_Bindpose.Dispose();
                if (m_Pose.IsCreated) m_Pose.Dispose();

                m_Bindpose = default;
                m_Pose = default;

                if (m_Buffer != null) m_Buffer.Dispose(); 

                m_Buffer = null;

                if (removeFromActiveSamplers)
                {
                    var inst = Rigs.InstanceOrNull;
                    if (inst != null) inst.activeSamplers.Remove(m_instanceId);
                }

            }

            protected TrackedTransformGroup trackingGroup;

            public void StartTrackingPoseData()
            {
                if (!Valid) return;

                if (trackingGroup != null) StopTrackingPoseData();
                trackingGroup = Rigs.Track(m_RendererBones, Bindpose);

                Rigs.ListenForPoseDataChanges(WriteToBuffers);
            }

            public void StopTrackingPoseData()
            {
                if (trackingGroup != null) trackingGroup.UntrackAllTransforms();
                trackingGroup = null;

                Rigs.StopListeningForPoseDataChanges(WriteToBuffers);
            }

            protected int users;
            public int Users => users;

            public void RegisterAsUser()
            {
                users++;

                if (users == 1) StartTrackingPoseData();
            }
            public void UnregisterAsUser()
            {
                users--;

                if (users <= 1) StopTrackingPoseData();
            }

            public void TryDispose()
            {
                if (users > 0) return;
                Dispose();
            }

        }

        /// <summary>
        /// Uses a provided bone array instead of fetching it from a skinned renderer
        /// </summary>
        public class StandaloneSampler : Sampler
        {

            protected string m_id;
            public override string ID => m_id;

            public StandaloneSampler(string id, Transform[] bones, Matrix4x4[] bindpose) : base(null)
            {

                m_id = id;

                m_RendererBones = bones;

                m_Pose = new NativeArray<float4x4>(m_RendererBones.Length, Allocator.Persistent);

                m_ManagedBindPose = bindpose;
                m_Bindpose = new NativeArray<float4x4>(bindpose.Length, Allocator.Persistent);
                for (int a = 0; a < bindpose.Length; a++) m_Bindpose[a] = (float4x4)bindpose[a];

            }
            public StandaloneSampler(string id, ICollection<Transform> bones, Matrix4x4[] bindpose) : base(null)
            {

                m_id = id;

                m_RendererBones = new Transform[bones.Count]; 
                int i = 0;
                foreach(var bone in bones)
                {
                    m_RendererBones[i] = bone;
                    i++;
                }

                m_Pose = new NativeArray<float4x4>(m_RendererBones.Length, Allocator.Persistent);

                m_ManagedBindPose = bindpose;
                m_Bindpose = new NativeArray<float4x4>(bindpose.Length, Allocator.Persistent);
                for (int a = 0; a < bindpose.Length; a++) m_Bindpose[a] = (float4x4)bindpose[a];

            }

            public override void Dispose(bool removeFromActiveSamplers)
            {
                base.Dispose(removeFromActiveSamplers);

                if (removeFromActiveSamplers)
                {
                    var inst = Rigs.InstanceOrNull;
                    if (inst != null) inst.activeStandaloneSamplers.Remove(ID); 
                }
            }

        }

        private Dictionary<int, Sampler> activeSamplers = new Dictionary<int, Sampler>();
        private Dictionary<string, StandaloneSampler> activeStandaloneSamplers = new Dictionary<string, StandaloneSampler>();

        private NativeList<float4x4> globalPoseData;
        private NativeList<float4x4> globalBindPoseData;
        private TransformAccessArray globalTrackedTransforms;
        private readonly List<int> openIndices = new List<int>();
        private class DummyTransform
        {
            public Transform transform;
            public int currentIndex;
        }
        private readonly List<DummyTransform> dummyTransforms = new List<DummyTransform>();
        private DummyTransform CreateNewDummyTransform()
        {
            var dummy = new DummyTransform() { transform = new GameObject($"dummy_{dummyTransforms.Count}").transform, currentIndex = -1 };
            dummy.transform.SetParent(transform, false);
            dummyTransforms.Add(dummy);
            return dummy;
        }
        private DummyTransform UseDummyTransform(int trackingIndex)
        {
            DummyTransform dummy = null;
            foreach (var d in dummyTransforms) if (d.currentIndex < 0) dummy = d;

            if (dummy == null) dummy = CreateNewDummyTransform();
            dummy.currentIndex = trackingIndex;
            return dummy;
        }
        private void ReleaseDummyTransform(Transform transform)
        {
            foreach (var dummy in dummyTransforms) if (dummy.transform == transform) dummy.currentIndex = -1;
        }

        public int GetTrackingIndexLocal(Transform transform)
        {
            if (transform == null || !globalTrackedTransforms.isCreated) return -1;

            for (int a = 0; a < globalTrackedTransforms.length; a++) if (transform == globalTrackedTransforms[a]) return a; 
            return -1;
        }
        public static int GetTrackingIndex(Transform transform)
        {
            var instance = Instance;
            if (instance == null) return -1;

            return instance.GetTrackingIndexLocal(transform);
        }

        public void SortOpenIndicesLocal()
        {
            openIndices.Sort();
        }
        public static void SortOpenIndices()
        {
            var instance = Instance;
            if (instance == null) return;

            instance.SortOpenIndicesLocal();
        }
        public int TrackLocal(Transform transform, Matrix4x4 bindPose)
        {
            if (transform == null || !globalTrackedTransforms.isCreated) return -1;

            OutputDependency.Complete();

            int trackingIndex = -1;
            if (openIndices.Count <= 0)
            {
                globalTrackedTransforms.Add(transform);
                globalBindPoseData.Add(bindPose);
                globalPoseData.Add(math.mul((float4x4)transform.localToWorldMatrix, bindPose));
                trackingIndex = globalTrackedTransforms.length - 1;
            } 
            else
            {
                trackingIndex = openIndices[0];
                openIndices.RemoveAt(0);

                var removed = globalTrackedTransforms[trackingIndex];
                globalTrackedTransforms[trackingIndex] = transform;
                globalBindPoseData[trackingIndex] = bindPose;
                globalPoseData[trackingIndex] = math.mul((float4x4)transform.localToWorldMatrix, bindPose);
                ReleaseDummyTransform(removed);
            }

            return trackingIndex;
        }
        public static int Track(Transform transform, Matrix4x4 bindPose)
        {
            var instance = Instance;
            if (instance == null) return -1;

            return instance.TrackLocal(transform, bindPose);
        }

        public bool UntrackLocal(int trackingIndex)
        {
            if (!globalTrackedTransforms.isCreated || trackingIndex < 0 || trackingIndex >= globalTrackedTransforms.length || openIndices.Contains(trackingIndex)) return false;

            var dummy = UseDummyTransform(trackingIndex);
            openIndices.Add(trackingIndex);
            globalTrackedTransforms.Add(dummy.transform);
            globalTrackedTransforms.RemoveAtSwapBack(trackingIndex); 

            return true;
        }
        public bool UntrackLocal(Transform transform) => UntrackLocal(GetTrackingIndex(transform));
        public static bool Untrack(int trackingIndex)
        {
            var instance = Instance;
            if (instance == null) return false;

            return instance.UntrackLocal(trackingIndex);
        }
        public static bool Untrack(Transform transform)
        {
            var instance = Instance;
            if (instance == null) return false;

            return instance.UntrackLocal(transform);
        }
        public void UntrackNullTransformsLocal()
        {
            if (!globalTrackedTransforms.isCreated) return; 

            for (int a = 0; a < globalTrackedTransforms.length; a++) if (globalTrackedTransforms[a] == null) UntrackLocal(a);
        }
        public static void UntrackNullTransforms()
        {
            var instance = Instance;
            if (instance == null) return;

            instance.UntrackNullTransformsLocal();
        }

        public class TrackedTransformGroup
        {

            internal readonly List<int> trackingIndices = new List<int>();
            internal bool isSequential;

            public void UntrackAllTransforms()
            {
                var inst = Rigs.InstanceOrNull;
                if (inst != null)
                {
                    foreach(var index in trackingIndices) inst.UntrackLocal(index);
                }

                trackingIndices.Clear();
            }

            public TrackedTransformGroup(ICollection<Transform> transforms, ICollection<Matrix4x4> bindpose)
            {
                var instance = Rigs.InstanceOrNull;
                if (instance == null) return;

                instance.SortOpenIndicesLocal();
                isSequential = true;
                int prevIndex = -1;
                using(var enu0  = transforms.GetEnumerator())
                {
                    using (var enu1 = bindpose.GetEnumerator())
                    {
                        while(enu0.MoveNext() && enu1.MoveNext())
                        {
                            int ind = instance.TrackLocal(enu0.Current, enu1.Current);
                            trackingIndices.Add(ind);
                            if (prevIndex >= 0 && (ind - prevIndex) != 1) isSequential = false;
                            prevIndex = ind;
                        }
                    }
                }

#if UNITY_EDITOR
                if (!isSequential)
                {
                    Debug.LogWarning($"Created a non-sequential tracked transform group! ({trackingIndices.Count} transforms)"); 
                }
#endif
            } 
            public TrackedTransformGroup(Transform[] transforms, NativeArray<float4x4> bindpose)
            {
                var instance = Rigs.Instance;
                if (instance == null) return;

                instance.SortOpenIndicesLocal();
                isSequential = true;
                int prevIndex = -1; 
                for (int a = 0; a < transforms.Length; a++)
                {
                    int ind = instance.TrackLocal(transforms[a], bindpose[a]);
                    trackingIndices.Add(ind);
                    if (prevIndex >= 0 && (ind - prevIndex) != 1) isSequential = false; 
                    prevIndex = ind;

                }

#if UNITY_EDITOR
                if (!isSequential)
                {
                    Debug.LogWarning($"Created a non-sequential tracked transform group! ({trackingIndices.Count} transforms)");
                }
#endif
            }

            public void CopyIntoArray(NativeArray<float4x4> poseArray, int startIndex)
            {
                var instance = Rigs.InstanceOrNull;
                if (instance == null) return;

                if (isSequential)
                {
                    NativeArray<float4x4>.Copy(instance.globalPoseData.AsArray(), trackingIndices[0], poseArray, startIndex, trackingIndices.Count);
                } 
                else
                {
                    for(int a = 0; a < trackingIndices.Count; a++)
                    {
                        var ind = trackingIndices[a];
                        poseArray[startIndex + a] = instance.globalPoseData[ind];
                    }
                }
            }
            public void CopyIntoArrayNoChecks(NativeArray<float4x4> poseArray, int startIndex)
            {
                var instance = Rigs.InstanceOrNull;

                if (isSequential)
                {
                    NativeArray<float4x4>.Copy(instance.globalPoseData.AsArray(), trackingIndices[0], poseArray, startIndex, trackingIndices.Count);
                }
                else
                {
                    for (int a = 0; a < trackingIndices.Count; a++)
                    {
                        var ind = trackingIndices[a];
                        poseArray[startIndex + a] = instance.globalPoseData[ind];
                    }
                }
            }
            public void CopyIntoArrayNoChecksSequential(NativeArray<float4x4> poseArray, int startIndex)
            {
                var instance = Rigs.InstanceOrNull;
                NativeArray<float4x4>.Copy(instance.globalPoseData.AsArray(), trackingIndices[0], poseArray, startIndex, trackingIndices.Count);
            }
            public void CopyIntoArrayNoChecksNonSequential(NativeArray<float4x4> poseArray, int startIndex)
            {
                var instance = Rigs.InstanceOrNull;
                for (int a = 0; a < trackingIndices.Count; a++)
                {
                    var ind = trackingIndices[a];
                    poseArray[startIndex + a] = instance.globalPoseData[ind];
                }
            }
            public void CopyIntoBuffer(ComputeBuffer buffer, int startIndex)
            {
                var instance = Rigs.InstanceOrNull;
                if (instance == null) return;

                var tempArray = buffer.BeginWrite<float4x4>(startIndex, trackingIndices.Count);
                if (isSequential)
                {
                    NativeArray<float4x4>.Copy(instance.globalPoseData.AsArray(), trackingIndices[0], tempArray, 0, trackingIndices.Count);
                }
                else
                {
                    for (int a = 0; a < trackingIndices.Count; a++)
                    {
                        var ind = trackingIndices[a];
                        tempArray[a] = instance.globalPoseData[ind]; 
                    }
                }
                buffer.EndWrite<float4x4>(trackingIndices.Count); 
            }

            public void CopyIntoArrays(ICollection<NativeArray<float4x4>> poseArrays, ICollection<int> startIndices)
            {
                var instance = Rigs.InstanceOrNull;
                if (instance == null) return;

                if (isSequential)
                {
                    using (var enu0 = poseArrays.GetEnumerator())
                    {
                        if (startIndices == null)
                        {
                            while (enu0.MoveNext())
                            {
                                NativeArray<float4x4>.Copy(instance.globalPoseData.AsArray(), trackingIndices[0], enu0.Current, 0, trackingIndices.Count);
                            }
                        }
                        else
                        {
                            using (var enu1 = startIndices.GetEnumerator())
                            {
                                while (enu0.MoveNext() && enu1.MoveNext())
                                {
                                    NativeArray<float4x4>.Copy(instance.globalPoseData.AsArray(), trackingIndices[0], enu0.Current, enu1.Current, trackingIndices.Count);
                                }
                            }
                        }
                    }
                }
                else
                {
                    using (var enu0 = poseArrays.GetEnumerator())
                    {
                        if (startIndices == null)
                        {
                            while (enu0.MoveNext())
                            {
                                var poseArray = enu0.Current;
                                var startIndex = 0;
                                for (int a = 0; a < trackingIndices.Count; a++)
                                {
                                    var ind = trackingIndices[a];
                                    poseArray[startIndex + a] = instance.globalPoseData[ind];
                                }
                            }
                        }
                        else
                        {
                            using (var enu1 = startIndices.GetEnumerator())
                            {
                                while (enu0.MoveNext() && enu1.MoveNext())
                                {
                                    var poseArray = enu0.Current;
                                    var startIndex = enu1.Current;
                                    for (int a = 0; a < trackingIndices.Count; a++)
                                    {
                                        var ind = trackingIndices[a];
                                        poseArray[startIndex + a] = instance.globalPoseData[ind];
                                    }
                                }
                            }
                        }
                    }
                }
            }

            private readonly List<NativeArray<float4x4>> tempArrays = new List<NativeArray<float4x4>>();
            /*public void CopyIntoBuffers(ICollection<ComputeBuffer> buffers, ICollection<int> startIndices)
            {
                var instance = Rigs.InstanceOrNull;
                if (instance == null) return;

                tempArrays.Clear();
                using (var enu0 = buffers.GetEnumerator())
                {
                    if (startIndices == null)
                    {
                        while (enu0.MoveNext())
                        {
                            var tempArray = enu0.Current.BeginWrite<float4x4>(0, trackingIndices.Count); 
                            tempArrays.Add(tempArray);
                        }
                    } 
                    else
                    {
                        using (var enu1 = startIndices.GetEnumerator())
                        {
                            while (enu0.MoveNext() && enu1.MoveNext())
                            {
                                var tempArray = enu0.Current.BeginWrite<float4x4>(enu1.Current, trackingIndices.Count);
                                tempArrays.Add(tempArray);
                            }
                        }
                    }
                }

                CopyIntoArrays(tempArrays, null);

                foreach(var buffer in buffers) buffer.EndWrite<float4x4>(trackingIndices.Count);
                tempArrays.Clear();
            }*/
            /*public void CopyIntoBuffers(ICollection<ComputeBufferWithStartIndex> buffers)
            {
                var instance = Rigs.InstanceOrNull;
                if (instance == null) return;

                tempArrays.Clear();
                foreach (var buffer in buffers)
                {
                    var tempArray = buffer.Buffer.BeginWrite<float4x4>(buffer.startIndex, trackingIndices.Count);
                    tempArrays.Add(tempArray);
                }

                CopyIntoArrays(tempArrays, null);

                foreach (var buffer in buffers) buffer.Buffer.EndWrite<float4x4>(trackingIndices.Count);
                tempArrays.Clear();
            }*/
            public void CopyIntoBuffers(ICollection<InstanceBufferWithStartIndex> buffers)
            {
                var instance = Rigs.InstanceOrNull;
                if (instance == null) return; 
                 
                /*tempArrays.Clear();
                foreach (var buffer in buffers)
                {
                    var tempArray = buffer.Buffer.Buffer.BeginWrite<float4x4>(buffer.startIndex, trackingIndices.Count);
                    tempArrays.Add(tempArray);
                }

                CopyIntoArrays(tempArrays, null);

                foreach (var buffer in buffers) buffer.Buffer.Buffer.EndWrite<float4x4>(trackingIndices.Count);
                tempArrays.Clear();*/
                if (isSequential)
                {
                    foreach (var buffer in buffers)
                    {
                        if (buffer.Buffer is InstanceBuffer<float4x4> buffer4x4) buffer4x4.WriteToBufferCallback(buffer.startIndex, trackingIndices.Count, CopyIntoArrayNoChecksSequential); 
                    }
                } 
                else
                {
                    foreach (var buffer in buffers)
                    {
                        if (buffer.Buffer is InstanceBuffer<float4x4> buffer4x4) buffer4x4.WriteToBufferCallback(buffer.startIndex, trackingIndices.Count, CopyIntoArrayNoChecksNonSequential);
                    }
                }
            }
        }
        public static TrackedTransformGroup Track(ICollection<Transform> transforms, ICollection<Matrix4x4> bindpose) => new TrackedTransformGroup(transforms, bindpose);
        public static TrackedTransformGroup Track(Transform[] transforms, NativeArray<float4x4> bindpose) => new TrackedTransformGroup(transforms, bindpose);

        [BurstCompile]
        public struct UpdateGlobalPoseDataJob : IJobParallelForTransform
        {

            [NativeDisableParallelForRestriction]
            public NativeArray<float4x4> poseData;
            [ReadOnly]
            public NativeArray<float4x4> bindPoseData;

            public void Execute(int index, TransformAccess transform)
            {
                poseData[index] = math.mul((float4x4)transform.localToWorldMatrix, bindPoseData[index]);
            }

        }
        public JobHandle UpdateGlobalPoseData(JobHandle inputDeps = default)
        {
            inputDeps = JobHandle.CombineDependencies(inputDeps, InputDependency, CustomAnimatorUpdater.FinalAnimationJobHandle);
            inputDeps = JobHandle.CombineDependencies(inputDeps, OutputDependency);

            outputDependency = new UpdateGlobalPoseDataJob()
            {
                poseData = globalPoseData.AsArray(),
                bindPoseData = globalBindPoseData.AsArray(),
            }.Schedule(globalTrackedTransforms, inputDeps);

            return outputDependency;
        }
        public event VoidParameterlessDelegate PostUpdateGlobalPoseData;
        public void ClearListeners()
        {
            PostUpdateGlobalPoseData = null;
        }
        public void ListenForPoseDataChangesLocal(VoidParameterlessDelegate listener)
        {
            PostUpdateGlobalPoseData += listener;
        }
        public void StopListeningForPoseDataChangesLocal(VoidParameterlessDelegate listener)
        {
            PostUpdateGlobalPoseData -= listener;
        }
        public static void ListenForPoseDataChanges(VoidParameterlessDelegate listener)
        {
            var instance = Rigs.Instance;
            if (instance == null) return;  

            instance.ListenForPoseDataChangesLocal(listener);
        }
        public static void StopListeningForPoseDataChanges(VoidParameterlessDelegate listener)
        {
            var instance = Rigs.InstanceOrNull;
            if (instance == null) return;

            instance.StopListeningForPoseDataChangesLocal(listener);
        }

        protected override void OnAwake()
        {
            base.OnAwake();

            globalPoseData = new NativeList<float4x4>(0, Allocator.Persistent);
            globalBindPoseData = new NativeList<float4x4>(0, Allocator.Persistent);
            globalTrackedTransforms = new TransformAccessArray(0, -1);
        }

        public override void OnDestroyed()
        {

            try
            {
                ClearListeners();
                base.OnDestroyed();
            } 
            catch(Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogError(ex); 
#endif
            }

            if (activeSamplers != null)
            {

                foreach (var entry in activeSamplers) 
                {
                    try
                    {
                        if (entry.Value != null) entry.Value.Dispose(false);
                    }
                    catch (Exception ex)
                    {
#if UNITY_EDITOR
                        Debug.LogError(ex);
#endif
                    }
                }

                activeSamplers = null;

            }

            if (activeStandaloneSamplers != null)
            {

                foreach (var entry in activeStandaloneSamplers)
                {
                    try
                    {
                        if (entry.Value != null) entry.Value.Dispose(false);
                    }
                    catch (Exception ex)
                    {
#if UNITY_EDITOR
                        Debug.LogError(ex);
#endif
                    }
                }

                activeStandaloneSamplers = null;

            }

            try
            {
                if (globalPoseData.IsCreated)
                {
                    globalPoseData.Dispose();
                    globalPoseData = default;
                }
            }
            catch (Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogError(ex);
#endif
            }

            try
            {
                if (globalBindPoseData.IsCreated)
                {
                    globalBindPoseData.Dispose();
                    globalBindPoseData = default;
                }
            }
            catch (Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogError(ex);
#endif
            }

            try
            {
                if (globalTrackedTransforms.isCreated)
                {
                    globalTrackedTransforms.Dispose();
                    globalTrackedTransforms = default;
                }
            }
            catch (Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogError(ex);
#endif
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
                //sampler.StartTrackingPoseData();
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
                //sampler.StartTrackingPoseData();

            }
            else return false;

            return true;
        }
        public static bool CreateStandaloneSampler(string id, ICollection<Transform> bones, Matrix4x4[] bindpose, out StandaloneSampler sampler)
        {
            Rigs singleton = Instance;

            if (!singleton.activeStandaloneSamplers.TryGetValue(id, out sampler))
            {

                sampler = new StandaloneSampler(id, bones, bindpose);
                singleton.activeStandaloneSamplers[id] = sampler;
                //sampler.StartTrackingPoseData();

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

        public override bool ExecuteInStack => true;
        public static int ExecutionPriority => TransformTracking.ExecutionPriority + 1; // update after animation
        public override int Priority => ExecutionPriority; 

        public override void OnUpdate() { }

        public override void OnLateUpdate() 
        {
            //UntrackNullTransforms();
            UpdateGlobalPoseData().Complete();
            PostUpdateGlobalPoseData?.Invoke(); 
        }

        public override void OnFixedUpdate() { }

    }
}

#endif