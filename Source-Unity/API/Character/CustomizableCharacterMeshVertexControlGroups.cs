using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Burst;

using Swole.API;
using Swole.API.Unity.Animation;

namespace Swole.Morphing 
{

    public class CustomizableCharacterMeshVertexControlGroups : MonoBehaviour, ICustomUpdatableBehaviour, IDisposable
    {

        public void Dispose()
        {
            if (groups != null)
            {
                foreach(var group in groups) if (group != null) group.Dispose(); 
                groups = null;
            }

            CustomizableCharacterMeshVertexControlGroupsUpdater.Unregister(this); 
        }

        [SerializeField]
        protected CustomizableCharacterMeshV2 mesh;
        public CustomizableCharacterMeshV2 Mesh => mesh;

        [SerializeField]
        protected CustomizableCharacterMeshVertexControlGroup[] groups;
        public void SetGroups(CustomizableCharacterMeshVertexControlGroup[] groups)
        {
            this.groups = groups;
        }

        public bool TryGetControlGroup(string id, out CustomizableCharacterMeshVertexControlGroup group)
        {
            group = null;

            if (groups != null)
            {
                foreach(var group_ in groups)
                {
                    if (group_ == null) continue;
                    if (group_.BaseID == id)
                    {
                        group = group_;
                        return true;
                    }
                }
            }

            return false;
        }
        public CustomizableCharacterMeshVertexControlGroup GetControlGroup(int index)
        {
            if (index < 0 || groups == null || index >= groups.Length) return null;
            return GetControlGroupUnsafe(index);
        }
        public CustomizableCharacterMeshVertexControlGroup GetControlGroupUnsafe(int index) => groups[index];

        public int IndexOfControlGroup(string id)
        {
            if (TryGetControlGroup(id, out var group)) return group.GroupIndex; 
            return -1;
        }

        protected virtual void Awake()
        {
            if (mesh == null) mesh = GetComponent<CustomizableCharacterMeshV2>();

            Initialize();
        }

        protected virtual void Start()
        {
            CustomizableCharacterMeshVertexControlGroupsUpdater.Register(this);
        }

        public virtual void Initialize()
        {
            if (groups != null)
            {
                for(int i = 0; i < groups.Length; i++)
                {
                    var group = groups[i];
                    if (group == null) continue; 

                    group.Init(mesh, i); 
                }
            }
        }

        protected virtual void OnDestroy()
        {
            Dispose();
        }

        private readonly List<CustomizableCharacterMeshVertexControlGroup> updatedGroups = new List<CustomizableCharacterMeshVertexControlGroup> ();
        public virtual void CustomUpdate() 
        {
            updatedGroups.Clear();
            if (groups != null)
            {
                foreach (var group in groups)
                {
                    if (group.UpdateIfDirty())
                    {
                        updatedGroups.Add(group);
                    }
                }
            }
        }
        public virtual void CustomLateUpdate()
        {
            if (mesh == null) return;

            int vertexCount = mesh.SubData.vertexCount;
            int indexOffset = mesh.InstanceID * vertexCount;
            foreach (var group in updatedGroups)
            {
                group.JobDependency.Complete();
                if (group.HasInstanceBuffer())
                {
                    var instanceBuffer = group.GetInstanceBufferNoCheck();
                    var outputBuffer = group.GetOutputBufferWithoutJobWait();

                    instanceBuffer.WriteToBuffer(outputBuffer.AsArray(), indexOffset, indexOffset, vertexCount);
                }
            }
        }
        public virtual void CustomFixedUpdate() { }

    }

    [Serializable]
    public class CustomizableCharacterMeshVertexControlGroup : IDisposable
    {

        public void Dispose() 
        {
            if (controlWeights.IsCreated)
            {
                controlWeights.Dispose();
                controlWeights = default;
            }
        }

        private static Dictionary<string, NativeList<float>> outputBuffers = new Dictionary<string, NativeList<float>>(); // there can be multiple instances of the same control group - so we need to use global buffers, and write to them using the character mesh instance ID
        private static Dictionary<string, InstanceBuffer<float>> outputInstanceBuffers = new Dictionary<string, InstanceBuffer<float>>();

        public InstanceBuffer<float> GetOrCreateInstanceBuffer()
        {
            if (mesh == null) 
            {
                swole.LogError($"Character mesh was null for {ID} while fetching instance buffer"); 
                return null; 
            }

            if (!outputInstanceBuffers.TryGetValue(ID, out var instanceBuffer) || !instanceBuffer.IsValid())
            {
                if (!mesh.MeshGroup2.TryGetInstanceBuffer(materialPropertyName, out var iInstanceBuffer) || iInstanceBuffer is not InstanceBuffer<float>)
                {
                    mesh.MeshGroup2.CreateInstanceMaterialBuffer(materialPropertyName, materialSlots, mesh.SubData.vertexCount, 2, true, out instanceBuffer);
                }
                else
                {
                    instanceBuffer = (InstanceBuffer<float>)iInstanceBuffer;
                }

                outputInstanceBuffers[ID] = instanceBuffer; 
            }

            return instanceBuffer;
        }
        public bool HasInstanceBuffer() => outputInstanceBuffers.ContainsKey(ID);
        public InstanceBuffer<float> GetInstanceBufferNoCheck() => outputInstanceBuffers[ID];
        
        private static Dictionary<string, NativeArray<float>> subGroupVertexWeights = new Dictionary<string, NativeArray<float>>();

        private static Dictionary<string, JobHandle> jobHandles = new Dictionary<string, JobHandle>();

        [Serializable]
        public class SubGroup
        {
            public string displayName;
            public string vertexGroupName;
            public int vertexGroupIndex;

            [NonSerialized]
            private float controlWeight;
            public float ControlWeight
            {
                get => controlWeight;
                set
                {
                    controlWeight = value;

                    if (controlGroup != null)
                    {
                        if (controlGroup.controlWeights.IsCreated && controlGroup.controlWeights[localIndex] != value)
                        {
                            controlGroup.controlWeights[localIndex] = value;
                            controlGroup.isDirty = true;
                        }
                    }
                }
            }

            [NonSerialized]
            private int localIndex;

            private CustomizableCharacterMeshVertexControlGroup controlGroup;
            public CustomizableCharacterMeshVertexControlGroup ControlGroup => controlGroup;

            public void Init(CustomizableCharacterMeshVertexControlGroup controlGroup, int localIndex)
            {
                this.controlGroup = controlGroup;
                this.localIndex = localIndex;

                if (!string.IsNullOrWhiteSpace(vertexGroupName) && controlGroup != null && controlGroup.mesh != null)
                {
                    vertexGroupIndex = controlGroup.mesh.IndexOfVertexGroup(vertexGroupName, true);  
                }
            }
        }

        [NonSerialized]
        private NativeArray<float> controlWeights;

        [SerializeField]
        private string materialPropertyName;
        [SerializeField]
        private int[] materialSlots;
        [SerializeField]
        private string id;
        public string BaseID => id;
        public string ID => $"{(mesh == null ? "null" : mesh.Data.name)}.{id}";

        [SerializeField]
        private SubGroup[] subGroups;
        public int SubGroupCount => subGroups == null ? 0 : subGroups.Length;

        public SubGroup GetSubGroup(int index)
        {
            if (index < 0 || subGroups == null || index >= subGroups.Length) return null;
            return subGroups[index];
        }

        public CustomizableCharacterMeshVertexControlGroup(string id, SubGroup[] subGroups, string materialPropertyName, int[] materialSlots = null)
        {
            this.id = id;
            this.subGroups = subGroups;
            this.materialPropertyName = materialPropertyName;
            this.materialSlots = materialSlots;            
        }

        [NonSerialized]
        private CustomizableCharacterMeshV2 mesh;
        public CustomizableCharacterMeshV2 Mesh => mesh;

        public bool clampWeights;
        public float2 weightRange;

        private static readonly List<float> tempWeights = new List<float>();

        [NonSerialized]
        private int groupIndex;
        public int GroupIndex => groupIndex;

        public void Init(CustomizableCharacterMeshV2 mesh, int groupindex)
        {
            var id = ID;

            this.mesh = mesh;
            this.groupIndex= groupindex;

            for(int i = 0; i < subGroups.Length; i++)
            {
                var group = subGroups[i];
                group.Init(this, i);
            }

            if (!subGroupVertexWeights.TryGetValue(id, out NativeArray<float> vertexWeights))
            {
                var vertexCount = mesh.SubData.vertexCount;
                vertexWeights = new NativeArray<float>(subGroups.Length * vertexCount, Allocator.Persistent);

                tempWeights.Clear();
                for (int i = 0; i < subGroups.Length; i++)
                {
                    var subGroup = subGroups[i];
                    if (subGroup.vertexGroupIndex < 0) continue;

                    var vg = mesh.GetVertexGroup(subGroup.vertexGroupIndex);
                    vg.AsLinearWeightList(vertexCount, tempWeights, tempWeights.Count > 0); 

                    var indexOffset = i * vertexCount;
                    for(int j = 0; j < vertexCount; j++)
                    {
                        vertexWeights[indexOffset + j] = tempWeights[j]; 
                    }
                }

                subGroupVertexWeights[id] = vertexWeights;
            }

            cachedOutputBuffer = OutputBuffer;

            controlWeights = new NativeArray<float>(subGroups.Length, Allocator.Persistent);

            if (weightRange.x == weightRange.y) weightRange = new float2(0f, 1f);

            if (!string.IsNullOrWhiteSpace(materialPropertyName)) GetOrCreateInstanceBuffer();
        }

        public JobHandle JobDependency
        {
            get
            {
                if (jobHandles.TryGetValue(ID, out var handle)) return handle;
                return default;
            }
        }

        [NonSerialized]
        private NativeList<float> cachedOutputBuffer;
        public NativeList<float> OutputBuffer
        {
            get
            {
                JobDependency.Complete();
                return GetOutputBufferWithoutJobWait();
            }
        }

        public NativeList<float> GetOutputBufferWithoutJobWait()
        {
            if (mesh == null) return default;

            if (!outputBuffers.TryGetValue(ID, out var buffer))
            {
                buffer = new NativeList<float>(mesh.SubData.vertexCount * Mathf.Max(1, mesh.InstanceID + 1), Allocator.Persistent);
                buffer.AddReplicated(0f, buffer.Capacity);

                PersistentJobDataTracker.Track(buffer);

                outputBuffers[ID] = buffer;
            }

            if (buffer.Length < mesh.SubData.vertexCount * (mesh.InstanceID + 1))
            {
                buffer.AddReplicated(0f, (mesh.SubData.vertexCount * (mesh.InstanceID + 1)) - buffer.Length);
            }

            return buffer;
        }

        protected bool isDirty;
        public bool IsDirty => isDirty;

        public bool UpdateIfDirty()
        {
            if (isDirty && mesh != null && mesh.InstanceID >= 0)
            {
                var id = ID;

                if (subGroupVertexWeights.TryGetValue(id, out var vertexWeights))
                {
                    isDirty = false;

                    jobHandles.TryGetValue(id, out var jobHandle);

                    int vertexCount = mesh.SubData.vertexCount;
                    int indexOffset = vertexCount * mesh.InstanceID;
                    jobHandle = new ResetOutputWeights()
                    {
                        indexOffset = indexOffset,
                        outputBuffer = cachedOutputBuffer
                    }.Schedule(vertexCount, 256, jobHandle);

                    for (int i = 0; i < subGroups.Length; i++)
                    {
                        jobHandle = new BuildOutputWeights()
                        {
                            indexOffset = indexOffset,
                            groupIndex = i,
                            groupWeightsIndexStart = i * vertexCount,
                            vertexWeights = vertexWeights,
                            controlWeights = controlWeights,
                            outputBuffer = cachedOutputBuffer
                        }.Schedule(vertexCount, 64, jobHandle);
                    }

                    if (clampWeights)
                    {
                        jobHandle = new FinalizeOutputWeightsClamped()
                        {
                            indexOffset = indexOffset,
                            weightRange = weightRange,
                            outputBuffer = cachedOutputBuffer
                        }.Schedule(vertexCount, 64, jobHandle);
                    }
                    else if (weightRange.x != 0f || weightRange.y != 1f)
                    {
                        jobHandle = new FinalizeOutputWeights()
                        {
                            indexOffset = indexOffset,
                            weightRange = weightRange,
                            outputBuffer = cachedOutputBuffer
                        }.Schedule(vertexCount, 64, jobHandle); 
                    }

                    jobHandles[id] = jobHandle;
                }

                return true;
            }

            return false;
        }

        [BurstCompile]
        public struct ResetOutputWeights : IJobParallelFor
        {
            public int indexOffset;

            [NativeDisableParallelForRestriction]
            public NativeList<float> outputBuffer;

            public void Execute(int index)
            {
                outputBuffer[index + indexOffset] = 0f;
            }
        }
        [BurstCompile]
        public struct BuildOutputWeights : IJobParallelFor
        {
            public int indexOffset;
            public int groupIndex;
            public int groupWeightsIndexStart;

            [ReadOnly]
            public NativeArray<float> vertexWeights;

            [ReadOnly]
            public NativeArray<float> controlWeights;

            [NativeDisableParallelForRestriction]
            public NativeList<float> outputBuffer;

            public void Execute(int index)
            {
                float controlWeight = controlWeights[groupIndex];

                int outputIndex = indexOffset + index;
                outputBuffer[outputIndex] = outputBuffer[outputIndex] + (controlWeight * vertexWeights[groupWeightsIndexStart + index]);
            }
        }
        [BurstCompile]
        public struct FinalizeOutputWeights : IJobParallelFor
        {
            public int indexOffset;

            public float2 weightRange;

            [NativeDisableParallelForRestriction]
            public NativeList<float> outputBuffer;

            public void Execute(int index)
            {
                int outputIndex = indexOffset + index;
                outputBuffer[outputIndex] = math.lerp(weightRange.x, weightRange.y, outputBuffer[outputIndex]);
            }
        }
        [BurstCompile]
        public struct FinalizeOutputWeightsClamped : IJobParallelFor
        {
            public int indexOffset;

            public float2 weightRange;

            [NativeDisableParallelForRestriction]
            public NativeList<float> outputBuffer; 

            public void Execute(int index)
            {
                int outputIndex = indexOffset + index;
                outputBuffer[outputIndex] = math.lerp(weightRange.x, weightRange.y, math.saturate(outputBuffer[outputIndex]));
            }
        }

    }

    public class CustomizableCharacterMeshVertexControlGroupsUpdater : CustomBehaviourUpdater<CustomizableCharacterMeshVertexControlGroups>
    {

        public static int ExecutionPriority => CustomAnimatorUpdater.ExecutionPriority - 10;

        public override int Priority => ExecutionPriority;

    }

}
