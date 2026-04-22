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
        protected CustomizableCharacterMeshV2[] additionalMeshes;

        public void SetAdditionalMeshes(CustomizableCharacterMeshV2[] additionalMeshes)
        {
            this.additionalMeshes = additionalMeshes;
        }

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

        public bool SetSubGroupWeight(string controlGroup, string subGroup, float weight)
        {
            if (TryGetControlGroup(controlGroup, out var controlGroup_) && controlGroup_.TryGetSubGroup(subGroup, out var subGroup_))
            {
                subGroup_.ControlWeight = weight;
                return true;
            }

            return false;
        }
        public bool SetSubGroupWeight(int controlGroupIndex, string subGroup, float weight)
        {
            var controlGroup = GetControlGroup(controlGroupIndex);
            if (controlGroup == null) return false;
             
            if (controlGroup.TryGetSubGroup(subGroup, out var subGroup_))
            {
                subGroup_.ControlWeight = weight;
                return true;
            }

            return false;
        }
        public bool SetSubGroupWeight(int controlGroupIndex, int subGroupIndex, float weight)
        {
            var controlGroup = GetControlGroup(controlGroupIndex);
            if (controlGroup == null) return false;

            var subGroup = controlGroup.GetSubGroup(subGroupIndex);
            if (subGroup == null) return false;

            subGroup.ControlWeight = weight;
            return true;
        }
        public void SetSubGroupEditorWeight(int editorIndex, float weight)
        {
            if (editorIndex < 0 || controlGroupEditors == null || editorIndex >= controlGroupEditors.Length) return;

            var editor = controlGroupEditors[editorIndex];
            if (!SetSubGroupWeight(editor.controlGroupIndex, editor.subGroupIndex, weight)) SetSubGroupWeight(editor.controlGroup, editor.subGroup, weight);   
        }

        [Serializable]
        public class ControlGroupEditor
        {
            public string controlGroup;
            public string subGroup;

            [NonSerialized]
            public int controlGroupIndex = -1;
            [NonSerialized]
            public int subGroupIndex = -1;
        }

        [SerializeField]
        protected ControlGroupEditor[] controlGroupEditors;

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
                for (int i = 0; i < groups.Length; i++)
                {
                    var group = groups[i];
                    if (group == null) continue;

                    group.Init(mesh, i);
                    if (additionalMeshes != null && additionalMeshes.Length > 0 && group.HasInstanceBuffer())
                    {
                        var instanceBuffer = group.GetInstanceBufferNoCheck();
                        foreach (var mesh in additionalMeshes)
                        {
                            var materials = mesh.MaterialInstances;
                            if (materials != null)
                            {
                                if (group.MaterialSlots != null)
                                {
                                    foreach(var slot in group.MaterialSlots)
                                    {
                                        if (slot < 0 || slot >=  materials.Length) continue;

                                        var mat = materials[slot];
                                        if (mat == null) continue;

                                        instanceBuffer.BindMaterialProperty(mat, group.MaterialPropertyName);
                                    }
                                }
                                else
                                {
                                    foreach (var mat in materials)
                                    {
                                        if (mat == null) continue;
                                        instanceBuffer.BindMaterialProperty(mat, group.MaterialPropertyName);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (controlGroupEditors != null)
            {
                foreach(var editor in controlGroupEditors)
                {
                    if (TryGetControlGroup(editor.controlGroup, out var group) && group.TryGetSubGroup(editor.subGroup, out var subGroup))
                    {
                        editor.controlGroupIndex = group.GroupIndex;
                        editor.subGroupIndex = subGroup.Index;
                    }
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

            if (subGroups != null)
            {
                foreach (var subGroup in subGroups) if (subGroup != null) subGroup.Dispose();
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
                    mesh.MeshGroup2.CreateInstanceMaterialBuffer(materialPropertyName, materialSlots == null || materialSlots.Length <= 0 ? null : materialSlots, mesh.SubData.vertexCount, 2, true, out instanceBuffer);
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

        private class SubGroupVertexWeights : IDisposable
        {

            public NativeArray<float2> weights;
            public NativeArray<int2> startIndicesCounts;

            public void Dispose()
            {
                if (weights.IsCreated)
                {
                    weights.Dispose();
                    weights = default;
                }

                if (startIndicesCounts.IsCreated)
                {
                    startIndicesCounts.Dispose();
                    startIndicesCounts = default;
                }
            }
        }
        
        private static Dictionary<string, SubGroupVertexWeights> subGroupVertexWeights = new Dictionary<string, SubGroupVertexWeights>();

        private static Dictionary<string, JobHandle> jobHandles = new Dictionary<string, JobHandle>();

        [Serializable]
        public class SubGroup : IDisposable
        {
            public void Dispose()
            {
                if (activeChildren != null)
                {
                    foreach(var child in activeChildren)
                    {
                        if (child != null && ReferenceEquals(child.parent, this)) child.parent = null;
                    }

                    activeChildren.Clear();
                    activeChildren = null;
                }

                if (parent != null && parent.activeChildren != null)
                {
                    parent.activeChildren.Remove(this);
                }
            }

            public string displayName;
            public string vertexGroupName;
            public int vertexGroupIndex;

            public bool includeZeroWeights;

            [NonSerialized]
            public SubGroup parent;
            public SubGroupChild[] children;
            [NonSerialized]
            public List<SubGroup> activeChildren;

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

                    if (activeChildren != null)
                    {
                        foreach(var child in activeChildren)
                        {
                            if (child != null) child.ControlWeight = controlWeight;
                        }
                    }
                }
            }

            [NonSerialized]
            private int localIndex;
            public int Index => localIndex;

            [NonSerialized]
            public int vertexGroupHeaderIndex;

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

                if (children != null && children.Length > 0)
                {
                    activeChildren = new List<SubGroup>();
                    foreach(var c in children)
                    {
                        if (c.manager == null || !c.manager.TryGetControlGroup(c.groupId, out var childControlGroup)) continue;
                        if (childControlGroup == null || childControlGroup.TryGetSubGroup(c.subGroupName, out var childSubGroup) || childSubGroup == null || ReferenceEquals(childSubGroup, this)) continue;
                        activeChildren.Add(childSubGroup);
                        childSubGroup.parent = this;
                    }
                }
            }

            [Serializable]
            public class Reference
            {
                public CustomizableCharacterMeshVertexControlGroups controlGroupManager;
                public string controlGroupId;
                public string subControlGroupName; 

                private SubGroup instance;
                public SubGroup Instance
                {
                    get
                    {
                        if (instance == null && controlGroupManager != null)
                        {
                            if (controlGroupManager.TryGetControlGroup(controlGroupId, out var controlGroup) && controlGroup.TryGetSubGroup(subControlGroupName, out var subGroup))
                            {
                                instance = subGroup;
                            }
                        }

                        return instance;
                    }
                }
            }
        }

        [Serializable]
        public struct SubGroupChild
        {
            public CustomizableCharacterMeshVertexControlGroups manager;
            public string groupId;
            public string subGroupName;
        }

        [NonSerialized]
        private NativeArray<float> controlWeights;

        [SerializeField]
        private string materialPropertyName;
        public string MaterialPropertyName => materialPropertyName;

        [SerializeField]
        private int[] materialSlots;
        public int[] MaterialSlots => materialSlots;

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
            return GetSubGroupUnsafe(index);
        }
        public SubGroup GetSubGroupUnsafe(int index) => subGroups[index];

        public bool TryGetSubGroup(string name, out SubGroup group)
        {
            group = null;

            if (subGroups != null)
            {
                foreach (var group_ in subGroups)
                {
                    if (group_ == null) continue;
                    if (group_.displayName == name)
                    {
                        group = group_;
                        return true;
                    }
                }
            }

            return false;
        }
        public int IndexOfSubGroup(string id)
        {
            if (TryGetSubGroup(id, out var group)) return group.Index;
            return -1;
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

        public float resetWeight;

        public bool clampWeights;
        public float2 weightRange;

        private static readonly List<float2> tempWeights = new List<float2>();
        private static readonly List<int2> tempIndices = new List<int2>();

        [NonSerialized]
        private int groupIndex;
        public int GroupIndex => groupIndex;

        public void Init(CustomizableCharacterMeshV2 mesh, int groupindex)
        {
            this.mesh = mesh;
            this.groupIndex = groupindex;

            var id = ID;

            for (int i = 0; i < subGroups.Length; i++)
            {
                var group = subGroups[i];
                group.Init(this, i);
            }

            if (!subGroupVertexWeights.TryGetValue(id, out SubGroupVertexWeights vertexWeights))
            {
                vertexWeights = new SubGroupVertexWeights();

                tempWeights.Clear();
                tempIndices.Clear();
                int indexOffset = 0;
                for (int i = 0; i < subGroups.Length; i++)
                {
                    var subGroup = subGroups[i];
                    subGroup.vertexGroupHeaderIndex = -1;
                    if (subGroup.vertexGroupIndex < 0)
                    {
                        tempIndices.Add(new int2(0, 0));
                        continue;
                    }

                    var vg = mesh.GetVertexGroup(subGroup.vertexGroupIndex);
                    for (int j = 0; j < vg.EntryCount; j++)
                    {
                        vg.GetEntry(j, out int vIndex, out float vWeight);
                        if (vWeight == 0f && !subGroup.includeZeroWeights) continue; // ignore zero weights for better performance

                        tempWeights.Add(new float2(vIndex, vWeight));
                    }

                    subGroup.vertexGroupHeaderIndex = tempIndices.Count;
                    tempIndices.Add(new int2(indexOffset, tempWeights.Count - indexOffset));
                    indexOffset = tempWeights.Count;
                }

                vertexWeights.weights = new NativeArray<float2>(tempWeights.ToArray(), Allocator.Persistent);
                vertexWeights.startIndicesCounts = new NativeArray<int2>(tempIndices.ToArray(), Allocator.Persistent);

                subGroupVertexWeights[id] = vertexWeights;
                PersistentJobDataTracker.Track(vertexWeights);
            }
            else
            {
                for (int i = 0; i < subGroups.Length; i++)
                {
                    var subGroup = subGroups[i];
                    subGroup.vertexGroupHeaderIndex = -1;
                    if (subGroup.vertexGroupIndex < 0) continue;

                    subGroup.vertexGroupHeaderIndex = i;
                }
            }

            cachedOutputBuffer = OutputBuffer;

            controlWeights = new NativeArray<float>(subGroups.Length, Allocator.Persistent);

            if (weightRange.x == weightRange.y) weightRange = new float2(0f, 1f);

            if (!string.IsNullOrWhiteSpace(materialPropertyName)) 
            {
                CoroutineProxy.Start(InitInstanceBuffer());
            }
        }
        private IEnumerator InitInstanceBuffer()
        {
            while (mesh != null && mesh.MeshGroup2 == null) yield return null;  
            GetOrCreateInstanceBuffer();
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
                        defaultWeight = resetWeight,
                        outputBuffer = cachedOutputBuffer
                    }.Schedule(vertexCount, 256, jobHandle);

                    for (int i = 0; i < subGroups.Length; i++)
                    {
                        var subGroup = subGroups[i];
                        if (subGroup.vertexGroupHeaderIndex < 0) continue;

                        var startIndexCount = vertexWeights.startIndicesCounts[subGroup.vertexGroupHeaderIndex];
                        jobHandle = new BuildOutputWeights()
                        {
                            indexOffset = indexOffset,
                            groupIndex = i,
                            groupWeightsIndexStart = startIndexCount.x,
                            vertexWeights = vertexWeights.weights,
                            controlWeights = controlWeights,
                            outputBuffer = cachedOutputBuffer
                        }.Schedule(startIndexCount.y, 64, jobHandle);
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
            public float defaultWeight;

            [NativeDisableParallelForRestriction]
            public NativeList<float> outputBuffer;

            public void Execute(int index)
            {
                outputBuffer[index + indexOffset] = defaultWeight;
            }
        }
        [BurstCompile]
        public struct BuildOutputWeights : IJobParallelFor
        {
            public int indexOffset;
            public int groupIndex;
            public int groupWeightsIndexStart;

            [ReadOnly]
            public NativeArray<float2> vertexWeights;

            [ReadOnly]
            public NativeArray<float> controlWeights;

            [NativeDisableParallelForRestriction]
            public NativeList<float> outputBuffer;

            public void Execute(int index)
            {
                float controlWeight = controlWeights[groupIndex];
                float2 vgSample = vertexWeights[groupWeightsIndexStart + index];

                int vIndex = (int)vgSample.x;
                int outputIndex = indexOffset + vIndex;
                outputBuffer[outputIndex] = outputBuffer[outputIndex] + (controlWeight * vgSample.y);
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

    public class CustomizableCharacterMeshVertexControlGroupsUpdater : CustomBehaviourUpdater<CustomizableCharacterMeshVertexControlGroupsUpdater, CustomizableCharacterMeshVertexControlGroups>
    {

        public static int ExecutionPriority => CustomAnimatorUpdater.ExecutionPriority - 10; 

        public override int Priority => ExecutionPriority;

    }

}
