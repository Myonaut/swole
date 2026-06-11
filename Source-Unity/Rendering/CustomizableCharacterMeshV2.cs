#if UNITY_2017_1_OR_NEWER

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using UnityEngine;
using UnityEngine.Events;

using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Burst;

using Swole.API.Unity;
using Swole.API.Unity.Animation;
using Swole.DataStructures;

using static Swole.API.Unity.ICustomizableCharacter.Defaults;

namespace Swole.Morphing
{

    //ICustomizableCharacterMeshBaseData

    public class CustomizableCharacterMeshV2 : CustomizableCharacterMeshBase
    {

        #region Singleton Updater

        protected class Updater : SingletonBehaviour<Updater>, IDisposable
        {

            public override int UpdatePriority => -999999; // start jobs very early
            public override int LateUpdatePriority => ComputeBufferPoolUploader.ExecutionPriority - 100; // wait on jobs as late as possible

            protected Dictionary<int, MeshGroupV2> meshGroups = new Dictionary<int, MeshGroupV2>();

            public static InstanceV2 Register(CustomizableCharacterMeshV2_DATA data)
            {
                var singleton = Instance;
                if (singleton == null) return null;

                if (data == null) return null;

                if (!singleton.meshGroups.TryGetValue(data.GetInstanceID(), out var meshGroup))
                {
#if UNITY_EDITOR
                    data.TryPrecache();
#endif
                    meshGroup = new MeshGroupV2(data.SerializedData);
                    singleton.meshGroups[data.GetInstanceID()] = meshGroup;
                }

#if UNITY_EDITOR
                return meshGroup.ClaimNewInstance(data.name);
#else
                return meshGroup.ClaimNewInstance();
#endif
            }
            public static void Unregister(ref InstanceV2 instance)
            {
                //var singleton = InstanceOrNull;
                if (instance == null) return;

                instance.Dispose();
                instance = null;
            }

            public override void OnUpdate()
            {
                foreach (var meshGroup in meshGroups.Values) meshGroup.BeginNewJob();
            }

            private List<int> toDispose = new List<int>();
            public override void OnLateUpdate()
            {
                toDispose.Clear();
                foreach (var entry in meshGroups) 
                {
                    var meshGroup = entry.Value;

                    meshGroup.WaitForJobCompletion();
                    if (meshGroup.InstanceCount <= 0) toDispose.Add(entry.Key);
                }

                foreach(var key in toDispose)
                {
                    var meshGroup = meshGroups[key];
                    meshGroup.Dispose();
                    meshGroups.Remove(key);
                }
            }

            public override void OnFixedUpdate()
            {
            }

            public override void OnDestroyed()
            {
                base.OnDestroyed();

                Dispose();
            }   

            public void Dispose()
            {
                if (meshGroups != null)
                {
                    foreach (var entry in meshGroups)
                    {
                        if (entry.Value != null)
                        {
                            entry.Value.Dispose();
                        }
                    }

                    meshGroups.Clear();
                    meshGroups = null;
                }
            }

        }

        #endregion

        #region Sub Types

        public class InstanceV2 : IDisposable
        {
            public int localID = -1;

            protected bool disposed;
            public bool IsDisposed => disposed;
            public bool IsValid => !IsDisposed && ownerGroup != null;

            public void Dispose() => Dispose(true);
            public void Dispose(bool releaseFromGroup)
            {
                disposed = true;

                if (ownerGroup != null)
                {
                    if (releaseFromGroup) ownerGroup.ReleaseInstance(this);
                    ownerGroup = null;
                }

                localID = -1;
            }

            private MeshGroupV2 ownerGroup;
            public MeshGroupV2 OwnerGroup => ownerGroup;

            public InstanceV2(MeshGroupV2 ownerGroup)
            {
                this.ownerGroup = ownerGroup;
            }

            public Material[] Materials => ownerGroup == null ? null : ownerGroup.GetInstanceMaterials(localID);

            public bool updateManually;

            public bool UpdateIfDirty(bool force = false, bool updateImmediately = false)
            {
                if (force)
                {
                    physiqueIsDirty = true;
                    variationIsDirty = true;
                }

                bool flag = false;

                if (physiqueIsDirty) 
                {
                    flag = true;
                    physiqueIsDirty = false;
                    ownerGroup.MarkForPhysiqueUpdate(localID); 
                }

                if (variationIsDirty) 
                {
                    flag = true;
                    variationIsDirty = false;
                    ownerGroup.MarkForVariationUpdate(localID); 
                }

                if (flag && updateImmediately)
                {
                    ownerGroup.BeginNewJob();
                    ownerGroup.WaitForJobCompletion();
                }

                return flag;
            }

            private bool physiqueIsDirty;
            private bool variationIsDirty;

            public void MarkForPhysiqueUpdateUnsafe()
            {
                if (updateManually)
                {
                    physiqueIsDirty = true;
                } 
                else
                {
                    ownerGroup.MarkForPhysiqueUpdate(localID);
                }   
            }

            public void MarkForVariationUpdateUnsafe()
            {
                if (updateManually)
                {
                    variationIsDirty = true;
                }
                else
                {
                    ownerGroup.MarkForVariationUpdate(localID);
                }
            }

            public void SetBustSizeUnsafe(float2 bustSize)
            {
                ownerGroup.SetBustSizeUnsafe(localID, bustSize);
            }

            public void SetMuscleGroupWeightUnsafe(int groupIndex, float2 massWeight)
            {
                ownerGroup.SetMuscleGroupWeightUnsafe(localID, groupIndex, massWeight);
            }
            public void SetFatGroupWeightUnsafe(int groupIndex, float fatWeight)
            {
                ownerGroup.SetFatGroupWeightUnsafe(localID, groupIndex, fatWeight);
            }
            public void SetVariationGroupWeightUnsafe(int shapeIndex, int groupIndex, float2 variationWeight)
            {
                ownerGroup.SetVariationGroupWeightUnsafe(localID, shapeIndex, groupIndex, variationWeight);
            }
            public void SetVariationGroupWeightUnsafe(int variationIndex, float2 variationWeight)
            {
                ownerGroup.SetVariationGroupWeightUnsafe(localID, variationIndex, variationWeight);
            }

            public float2 GetMuscleGroupWeightUnsafe(int groupIndex)
            {
                return ownerGroup.GetMuscleGroupWeightUnsafe(localID, groupIndex);
            }
            public float2 GetFatGroupWeightUnsafe(int groupIndex)
            {
                return ownerGroup.GetFatGroupWeightUnsafe(localID, groupIndex);
            }
            public float2 GetVariationGroupWeightUnsafe(int shapeIndex, int groupIndex)
            {
                return ownerGroup.GetVariationGroupWeightUnsafe(localID, shapeIndex, groupIndex);
            }
            public float2 GetVariationGroupWeightUnsafe(int variationIndex)
            {
                return ownerGroup.GetVariationGroupWeightUnsafe(localID, variationIndex);
            }

        }

        /// <summary>
        /// Handles realtime updates of meshes that use the same serialized data.
        /// </summary>
        public class MeshGroupV2 : IDisposable
        {

            #region Disposal

            private bool disposed;
            public bool IsDisposed => disposed;
            public bool IsValid => !IsDisposed;

            [NonSerialized]
            private bool trackingDisposables;
            public void TrackDisposables()
            {
                if (trackingDisposables || IsDisposed) return;

                if (!PersistentJobDataTracker.Track(this))
                {
                    Dispose();
                    return;
                }

                trackingDisposables = true;
            }

            public void Dispose()
            {
                activeJob.Complete();
                disposed = true;

                if (trackingDisposables)
                {
                    try
                    {
                        PersistentJobDataTracker.Untrack(this);
                    }
                    catch (Exception ex)
                    {
#if UNITY_EDITOR
                        Debug.LogError(ex);
#endif
                    }
                }
                trackingDisposables = false;

                try
                {
                    if (materialInstances != null)
                    {
                        foreach(var array in materialInstances)
                        {
                            if (array == null) continue;

                            foreach(var mat in array)
                            {
                                if (mat == null) continue;

                                Destroy(mat);
                            }
                        }

                        materialInstances.Clear();
                        materialInstances = null;;
                    }
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR
                    Debug.LogException(ex);
#endif
                }

                try
                {
                    if (instanceBuffers != null)
                    {
                        foreach (var buffer in instanceBuffers)
                        {
                            try
                            {
                                if (buffer.buffer != null) buffer.buffer.Dispose();
                            }
                            catch (Exception ex)
                            {
#if UNITY_EDITOR
                                Debug.LogError(ex);
#endif
                            }
                        }
                        instanceBuffers.Clear();
                    }
                }

                catch (Exception ex)
                {
#if UNITY_EDITOR
                    Debug.LogException(ex);
#endif
                }

                try
                {
                    if (indicesToUpdate.IsCreated)
                    {
                        indicesToUpdate.Dispose();
                        indicesToUpdate = default;
                    }
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR
                    Debug.LogException(ex);
#endif
                }

                try
                {
                    if (indicesToPhysiqueUpdate.IsCreated)
                    {
                        indicesToPhysiqueUpdate.Dispose();
                        indicesToPhysiqueUpdate = default;
                    }
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR
                    Debug.LogException(ex);
#endif
                }

                try
                {
                    if (indicesToVariationUpdate.IsCreated)
                    {
                        indicesToVariationUpdate.Dispose();
                        indicesToVariationUpdate = default;
                    }
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR
                    Debug.LogException(ex);
#endif
                }

                try
                {
                    if (insertionFlags.IsCreated)
                    {
                        insertionFlags.Dispose();
                        insertionFlags = default;
                    }
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR
                    Debug.LogException(ex);
#endif
                }

                try
                {
                    if (bustSizes.IsCreated)
                    {
                        bustSizes.Dispose();
                        bustSizes = default;
                    }
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR
                    Debug.LogException(ex);
#endif
                }

                try
                {
                    if (meshShapeDeltas.IsCreated)
                    {
                        meshShapeDeltas.Dispose();
                        meshShapeDeltas = default;
                    }
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR
                    Debug.LogException(ex);
#endif
                }

                try
                {
                    if (meshShapeInfos.IsCreated)
                    {
                        meshShapeInfos.Dispose();
                        meshShapeInfos = default;
                    }
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR
                    Debug.LogException(ex);
#endif
                }

                try
                {
                    if (meshShapeFrameWeights.IsCreated)
                    {
                        meshShapeFrameWeights.Dispose();
                        meshShapeFrameWeights = default;
                    }
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR
                    Debug.LogException(ex);
#endif
                }

                try
                {
                    if (vertexGroups.IsCreated)
                    {
                        vertexGroups.Dispose();
                        vertexGroups = default;
                    }
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR
                    Debug.LogException(ex);
#endif
                }

                try
                {
                    if (leftRightFlagBuffer.IsCreated)
                    {
                        leftRightFlagBuffer.Dispose();
                        leftRightFlagBuffer = default;
                    }
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR
                    Debug.LogException(ex);
#endif
                }

                try
                {
                    if (muscleGroupControlWeights.IsCreated)
                    {
                        muscleGroupControlWeights.Dispose();
                        muscleGroupControlWeights = default;
                    }
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR
                    Debug.LogException(ex);
#endif
                }
                try
                {
                    if (muscleGroupControlWeightsNext.IsCreated)
                    {
                        muscleGroupControlWeightsNext.Dispose();
                        muscleGroupControlWeightsNext = default;
                    }
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR
                    Debug.LogException(ex);
#endif
                }

                try
                {
                    if (muscleGroupVertexWeights.IsCreated)
                    {
                        muscleGroupVertexWeights.Dispose();
                        muscleGroupVertexWeights = default;
                    }
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR
                    Debug.LogException(ex);
#endif
                }

                try
                {
                    if (blankMuscleGroupControlWeights.IsCreated)
                    {
                        blankMuscleGroupControlWeights.Dispose();
                        blankMuscleGroupControlWeights = default;
                    }
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR
                    Debug.LogException(ex);
#endif
                }

                try
                {
                    if (fatGroupControlWeights.IsCreated)
                    {
                        fatGroupControlWeights.Dispose();
                        fatGroupControlWeights = default;
                    }
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR
                    Debug.LogException(ex);
#endif
                }
                try
                {
                    if (fatGroupControlWeightsNext.IsCreated)
                    {
                        fatGroupControlWeightsNext.Dispose();
                        fatGroupControlWeightsNext = default;
                    }
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR
                    Debug.LogException(ex);
#endif
                }

                try
                {
                    if (fatGroupVertexWeights.IsCreated)
                    {
                        fatGroupVertexWeights.Dispose();
                        fatGroupVertexWeights = default;
                    }
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR
                    Debug.LogException(ex);
#endif
                }

                try
                {
                    if (blankFatGroupControlWeights.IsCreated)
                    {
                        blankFatGroupControlWeights.Dispose();
                        blankFatGroupControlWeights = default;
                    }
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR
                    Debug.LogException(ex);
#endif
                }

                try
                {
                    if (fatValuesPerVertex.IsCreated)
                    {
                        fatValuesPerVertex.Dispose();
                        fatValuesPerVertex = default;
                    }
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR
                    Debug.LogException(ex);
#endif
                }

                try
                {
                    if (muscleValuesPerVertex.IsCreated)
                    {
                        muscleValuesPerVertex.Dispose();
                        muscleValuesPerVertex = default;
                    }
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR
                    Debug.LogException(ex);
#endif
                }

                try
                {
                    if (variationGroupControlWeights.IsCreated)
                    {
                        variationGroupControlWeights.Dispose();
                        variationGroupControlWeights = default;
                    }
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR
                    Debug.LogException(ex);
#endif
                }
                try
                {
                    if (variationGroupControlWeightsNext.IsCreated)
                    {
                        variationGroupControlWeightsNext.Dispose();
                        variationGroupControlWeightsNext = default;
                    }
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR
                    Debug.LogException(ex);
#endif
                }

                try
                {
                    if (variationGroupVertexWeights.IsCreated)
                    {
                        variationGroupVertexWeights.Dispose();
                        variationGroupVertexWeights = default;
                    }
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR
                    Debug.LogException(ex);
#endif
                }

                try
                {
                    if (blankVariationGroupControlWeights.IsCreated)
                    {
                        blankVariationGroupControlWeights.Dispose();
                        blankVariationGroupControlWeights = default;
                    }
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR
                    Debug.LogException(ex);
#endif
                }

                try
                {
                    if (muscleGroupVertexDeltasLR.IsCreated)
                    {
                        muscleGroupVertexDeltasLR.Dispose();
                        muscleGroupVertexDeltasLR = default;
                    }
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR
                    Debug.LogException(ex);
#endif
                }

                try
                {
                    if (fatGroupVertexDeltasLR.IsCreated)
                    {
                        fatGroupVertexDeltasLR.Dispose();
                        fatGroupVertexDeltasLR = default;
                    }
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR
                    Debug.LogException(ex);
#endif
                }

                try
                {
                    if (fatGroupVertexDataLR.IsCreated)
                    {
                        fatGroupVertexDataLR.Dispose();
                        fatGroupVertexDataLR = default;
                    }
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR
                    Debug.LogException(ex);
#endif
                }

                try
                {
                    if (muscleGroupVertexDataLR.IsCreated)
                    {
                        muscleGroupVertexDataLR.Dispose();
                        muscleGroupVertexDataLR = default;
                    }
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR
                    Debug.LogException(ex);
#endif
                }

                try
                {
                    if (variationVertexDeltas.IsCreated)
                    {
                        variationVertexDeltas.Dispose();
                        variationVertexDeltas = default;
                    }
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR
                    Debug.LogException(ex);
#endif
                }

                try
                {
                    if (finalVertexDeltas.IsCreated)
                    {
                        finalVertexDeltas.Dispose();
                        finalVertexDeltas = default;
                    }
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR
                    Debug.LogException(ex);
#endif
                }

            }

            #endregion

            private SerializedData data;

            #region Material Handling

            protected Material ApplyMainMaterialOverrides(Material material)
            {
                if (material != null)
                {
                    if (data != null)
                    {
                        material.SetFloat(data.VertexCountPropertyName, data.vertexCount);

                        material.SetFloat(data.MinMassShapeWeightPropertyName, data.minMassShapeWeight);                        

                        material.SetBuffer(data.SkinningDataPropertyName, data.BoneWeightsBuffer);
                        material.SetInteger(data.BoneCountPropertyName, data.BoneCount);
                        
                        material.SetVector(data.MuscleVertexGroupsBufferRangePropertyName, new Vector4(data.muscleGroups.x, data.muscleGroups.y, 0f, 0f));
                        material.SetVector(data.FatVertexGroupsBufferRangePropertyName, new Vector4(data.fatGroups.x, data.fatGroups.y, 0f, 0f));
                        material.SetVector(data.VariationVertexGroupsBufferRangePropertyName, new Vector4(data.variationGroups.x, data.variationGroups.y, 0f, 0f));

                        material.SetBuffer(data.MuscleGroupInfluencesPropertyName, data.MuscleGroupInfluencesBuffer);
                        material.SetBuffer(data.FatGroupInfluencesPropertyName, data.FatGroupInfluencesBuffer);

                        material.SetVector(data.StandaloneShapesBufferRangePropertyName, new Vector4(data.standaloneShapes.x, data.standaloneShapes.y, 0f, 0f));
                        material.SetInteger(data.MuscleMassShapeIndexPropertyName, data.massShape);
                        material.SetInteger(data.FlexShapeIndexPropertyName, data.flexShape);
                        material.SetInteger(data.FatShapeIndexPropertyName, data.fatShape);

                        material.SetFloat(data.DefaultShapeMuscleWeightPropertyName, data.defaultMassShapeWeight);
                        material.SetFloat(data.FlexEndPointWeightPropertyName, data.flexEndPointWeight);
                        material.SetFloat(data.FlexExponentPropertyName, data.flexExponent);
                        material.SetFloat(data.FlexNerfThresholdPropertyName, data.flexNerfThreshold);
                        material.SetFloat(data.FlexNerfExponentPropertyName, data.flexNerfExponent);

                        material.SetInteger(data.MidlineVertexGroupIndexPropertyName, data.midlineVertexGroup);
                        material.SetInteger(data.BustVertexGroupIndexPropertyName, data.bustVertexGroup);
                        material.SetInteger(data.BustNerfVertexGroupIndexPropertyName, data.bustNerfVertexGroup);
                        material.SetInteger(data.NippleMaskVertexGroupIndexPropertyName, data.nippleMaskVertexGroup);
                        material.SetInteger(data.GenitalMaskVertexGroupIndexPropertyName, data.genitalMaskVertexGroup);

                        material.SetInteger(data.BustSizeShapeIndexPropertyName, data.bustSizeShape);
                        material.SetInteger(data.BustSizeMuscularShapeIndexPropertyName, data.bustSizeMuscleShape); 

                        material.SetBuffer(data.VertexGroupsPropertyName, data.VertexGroupsBuffer);
                        material.SetBuffer(data.MeshShapeFrameDeltasPropertyName, data.MeshShapeFrameDeltasBuffer);
                        material.SetBuffer(data.MeshShapeFrameWeightsPropertyName, data.MeshShapeFrameWeightsBuffer);
                        material.SetBuffer(data.MeshShapeIndicesPropertyName, data.MeshShapeIndicesBuffer);
                        
                        try
                        {
                            material.SetBuffer(data.VertexColorDeltasPropertyName, data.VertexColorDeltasBuffer); 
                        } 
                        catch(Exception ex)
                        {
                            Debug.LogException(ex);
                        }

                        for(int index = 0; index < data.VertexColorDeltaCount; index++)
                        {
                            var delta = data.GetVertexColorDeltaUnsafe(index);
                            if (delta == null || string.IsNullOrWhiteSpace(delta.indexPropertyName)) continue;

                            material.SetFloat(delta.indexPropertyName, index);
                        }

                    }
                }

                return material;
            }

            private List<Material[]> materialInstances;
            private void InitializeMaterials()
            {
                if (IsDisposed) return;

                if (materialInstances == null)
                {
                    materialInstances = new List<Material[]>(MaxInstanceCount);
                }

                EnsureMaterialInstancesBufferSize();
            }
            private Material InstantiateMaterial(int slot, Material material)
            {
                if (material == null) return null;

                material = ApplyMainMaterialOverrides(Instantiate(material));

                if (instanceBuffers != null)
                {
                    foreach(var buffer in instanceBuffers)
                    {
                        if (buffer.HasSlot(slot)) 
                        {
                            buffer.BindMaterialProperty(material);
                            //Debug.Log($"Bound instance buffer {buffer.buffer.Name} to {material.name} at property {buffer.propertyName}");
                        }
                    }
                }

                return material;
            }
            private void EnsureMaterialInstancesBufferSize()
            {
                if (materialInstances == null || IsDisposed) return;

                while(materialInstances.Count < MaxInstanceCount)
                {
                    var array = new Material[data.materials == null ? 0 : data.materials.Length];
                    for (int b = 0; b < array.Length; b++) 
                    {
                        array[b] = InstantiateMaterial(b, data.materials[b]);
                    }
                    
                    materialInstances.Add(array);
                }
            }
            public Material[] GetInstanceMaterials(int instanceID)
            {
                InitializeMaterials();
                if (IsDisposed  || instanceID < 0|| instanceID >= materialInstances.Count) return null;

                return materialInstances[instanceID];
            }

            protected struct InstanceBufferWithSlots
            {
                public string propertyName;
                public IInstanceBuffer buffer;
                public List<int> slots;
                public bool autoApply;
                public bool HasSlot(int slot)
                {
                    if (slots == null) return true;
                    return slots.Contains(slot);
                }

                public void BindMaterialProperty(Material material)
                {
                    buffer.BindMaterialProperty(material, propertyName);
                }
            }
            [NonSerialized]
            protected readonly List<InstanceBufferWithSlots> instanceBuffers = new List<InstanceBufferWithSlots>();
            protected void EnsureInstanceBufferSizes()
            {
                foreach (var buffer in instanceBuffers) EnsureInstanceBufferSize(buffer.buffer);
            }
            protected void EnsureInstanceBufferSize(IInstanceBuffer buffer)
            {
                while (buffer.InstanceCount < MaxInstanceCount) buffer.Grow(2, 0);
            }
            public int BindInstanceMaterialBuffer(string propertyName, IInstanceBuffer buffer, bool autoApplyToMaterials) => BindInstanceMaterialBuffer(propertyName, null, buffer, autoApplyToMaterials);
            public int BindInstanceMaterialBuffer(string propertyName, ICollection<int> materialSlots, IInstanceBuffer buffer, bool autoApplyToMaterials)
            {
                var buffer_ = new InstanceBufferWithSlots()
                {
                    buffer = buffer,
                    propertyName = propertyName,
                    slots = materialSlots == null ? null : new List<int>(materialSlots),
                    autoApply = autoApplyToMaterials
                };

                int bufferIndex = instanceBuffers.Count;
                instanceBuffers.Add(buffer_);

                if (autoApplyToMaterials && materialInstances != null)
                {
                    foreach(var array in materialInstances)
                    {
                        if (array == null) continue;

                        if (materialSlots == null)
                        {
                            for(int a = 0; a < array.Length; a++)
                            {
                                var mat = array[a];
                                if (mat == null) continue;

                                buffer.BindMaterialProperty(mat, propertyName);
                            }
                        } 
                        else
                        {
                            foreach(var slot in materialSlots)
                            {
                                var mat = slot < 0 || slot >= array.Length ? null : array[slot];
                                if (mat == null) continue;

                                buffer.BindMaterialProperty(mat, propertyName);
                            }
                        }
                    }
                }

                return bufferIndex;
            }
            public int InstanceBufferCount => instanceBuffers.Count;
            public IInstanceBuffer GetInstanceBuffer(int index) => instanceBuffers[index].buffer;
            public bool TryGetInstanceBuffer(string propertyName, out IInstanceBuffer instanceBuffer)
            {
                instanceBuffer = default;
                if (instanceBuffers == null) return false;

                foreach(var buffer in instanceBuffers)
                {
                    if (buffer.propertyName == propertyName)
                    {
                        instanceBuffer = buffer.buffer;
                        return true;
                    }
                }

                return false;
            }
            public bool TryGetInstanceBuffer<T>(string propertyName, out InstanceBuffer<T> instanceBuffer) where T : unmanaged
            {
                instanceBuffer = null;
                if (instanceBuffers == null) return false;

                foreach (var buffer in instanceBuffers)
                {
                    if (buffer.propertyName == propertyName && buffer.buffer is InstanceBuffer<T> typedBuffer)
                    {
                        instanceBuffer = typedBuffer;
                        return true;
                    }
                }

                return false;
            }
            public int CreateInstanceMaterialBuffer<T>(string propertyName, int elementsPerInstance, int bufferPoolSize, bool autoApplyToMaterials, out InstanceBuffer<T> buffer) where T : unmanaged => CreateInstanceMaterialBuffer(propertyName, null, elementsPerInstance, bufferPoolSize, autoApplyToMaterials, out buffer);
            public int CreateInstanceMaterialBuffer<T>(string propertyName, ICollection<int> materialSlots, int elementsPerInstance, int bufferPoolSize, bool autoApplyToMaterials, out InstanceBuffer<T> buffer) where T : unmanaged
            {
                buffer = null;
                if (elementsPerInstance <= 0 || bufferPoolSize <= 0) return -1;

                Debug.Log($"Creating buffer with initial size {MaxInstanceCount}"); 
                buffer = new InstanceBuffer<T>(propertyName, MaxInstanceCount, elementsPerInstance, bufferPoolSize, ComputeBufferType.Structured, ComputeBufferMode.SubUpdates);
                return BindInstanceMaterialBuffer(propertyName, materialSlots, buffer, autoApplyToMaterials);
            }

            #endregion

            private int maxInstanceCount;
            public int MaxInstanceCount => maxInstanceCount;

            private NativeList<int> indicesToUpdate;
            private NativeList<int> indicesToPhysiqueUpdate;
            private NativeList<int> indicesToVariationUpdate;
            private NativeList<bool> insertionFlags;
            private List<int> indicesToPhysiqueUpdateNext = new List<int>();
            private List<int> indicesToVariationUpdateNext = new List<int>();
            private List<int> activeIndices = new List<int>();
            private List<int> openIndices = new List<int>();

            public int InstanceCount => activeIndices == null ? 0 : activeIndices.Count;

            public void MarkForPhysiqueUpdate(int index)
            {
                if (index < 0 || index >= MaxInstanceCount || IsDisposed || indicesToPhysiqueUpdateNext.Contains(index)) return;
                indicesToPhysiqueUpdateNext.Add(index);
            }

            public void MarkForVariationUpdate(int index)
            {
                if (index < 0 || index >= MaxInstanceCount || IsDisposed || indicesToVariationUpdateNext.Contains(index)) return;
                indicesToVariationUpdateNext.Add(index);
            }

            private NativeArray<MeshVertexDelta> meshShapeDeltas;
            private NativeArray<int2> meshShapeInfos;
            private NativeArray<float> meshShapeFrameWeights;

            private NativeArray<float> vertexGroups;
            private NativeArray<bool> leftRightFlagBuffer;

            private NativeList<float2> bustSizes;
            public void SetBustSizeUnsafe(int instanceIndex, float2 bustSize)
            {
                bustSizes[instanceIndex] = bustSize;
            }

            private NativeList<GroupControlWeight2> muscleGroupControlWeightsNext;
            private NativeList<GroupControlWeight2> muscleGroupControlWeights;
            private NativeArray<GroupVertexControlWeight> muscleGroupVertexWeights;
            private NativeArray<GroupControlWeight2> blankMuscleGroupControlWeights;
            private NativeList<MeshVertexDeltaLR> muscleGroupVertexDeltasLR;

            private NativeList<float2> muscleGroupVertexDataLR;
            private NativeList<float> muscleValuesPerVertex;

            public void SetMuscleGroupWeightUnsafe(int instanceIndex, int groupIndex, float2 massWeight)
            {
                int index = (instanceIndex * data.MuscleGroupsCount) + groupIndex;

                var val = muscleGroupControlWeightsNext[index];
                val.weight = massWeight;//math.max(massWeight, data.minMassShapeWeight);
                muscleGroupControlWeightsNext[index] = val;
            }
            public float2 GetMuscleGroupWeightUnsafe(int instanceIndex, int groupIndex)
            {
                int index = (instanceIndex * data.MuscleGroupsCount) + groupIndex;
                return muscleGroupControlWeightsNext[index].weight;
            }

            private NativeList<GroupControlWeight2> fatGroupControlWeightsNext;
            private NativeList<GroupControlWeight2> fatGroupControlWeights;
            private NativeArray<GroupVertexControlWeight> fatGroupVertexWeights;
            private NativeArray<GroupControlWeight2> blankFatGroupControlWeights;
            private NativeList<MeshVertexDeltaLR> fatGroupVertexDeltasLR;

            private NativeList<float4> fatGroupVertexDataLR;
            private NativeList<float2> fatValuesPerVertex;

            public void SetFatGroupWeightUnsafe(int instanceIndex, int groupIndex, float fatWeight)
            {
                int index = (instanceIndex * data.FatGroupsCount) + groupIndex;

                var val = fatGroupControlWeightsNext[index];
                var weight = val.weight;
                weight.x = fatWeight;
                val.weight = weight;
                fatGroupControlWeightsNext[index] = val;
            }
            public float2 GetFatGroupWeightUnsafe(int instanceIndex, int groupIndex)
            {
                int index = (instanceIndex * data.FatGroupsCount) + groupIndex;
                return fatGroupControlWeightsNext[index].weight;
            }

            private NativeList<GroupControlWeight2> variationGroupControlWeightsNext;
            private NativeList<GroupControlWeight2> variationGroupControlWeights;
            private NativeArray<GroupVertexControlWeight> variationGroupVertexWeights;
            private NativeArray<GroupControlWeight2> blankVariationGroupControlWeights;

            public void SetVariationGroupWeightUnsafe(int instanceIndex, int shapeIndex, int groupIndex, float2 variationWeight)
            {
                int index = (instanceIndex * data.VariationShapesControlDataSize) + (groupIndex * data.VariationShapesCount) + shapeIndex;

                var val = variationGroupControlWeightsNext[index];
                val.weight = variationWeight;
                variationGroupControlWeightsNext[index] = val;
            }
            public void SetVariationGroupWeightUnsafe(int instanceIndex, int variationIndex, float2 variationWeight)
            {
                SetVariationGroupWeightUnsafe(instanceIndex, variationIndex % data.VariationShapesCount, variationIndex / data.VariationShapesCount, variationWeight);
            }
            public float2 GetVariationGroupWeightUnsafe(int instanceIndex, int shapeIndex, int groupIndex)
            {
                int index = (instanceIndex * data.VariationShapesControlDataSize) + (groupIndex * data.VariationShapesCount) + shapeIndex;
                return variationGroupControlWeightsNext[index].weight;
            }
            public float2 GetVariationGroupWeightUnsafe(int instanceIndex, int variationIndex)
            {
                return GetVariationGroupWeightUnsafe(instanceIndex, variationIndex % data.VariationShapesCount, variationIndex / data.VariationShapesCount);
            }

            private NativeList<MeshVertexDelta> variationVertexDeltas;
            public NativeList<MeshVertexDelta> VariationVertexDeltas => variationVertexDeltas;

            private NativeList<MeshVertexDelta> finalVertexDeltas;
            public NativeList<MeshVertexDelta> FinalVertexDeltas => finalVertexDeltas;

            private bool hasActiveJob;
            private JobHandle activeJob;
            public JobHandle ActiveJob => activeJob;
            public void WaitForJobCompletion() 
            {
                activeJob.Complete();

                if (hasActiveJob)
                {
                    hasActiveJob = false;

                    if (finalVertexDeltasBufferIndex >= 0)
                    {
                        var finalDeltasBuffer = instanceBuffers[finalVertexDeltasBufferIndex];
                        if (finalDeltasBuffer.buffer is InstanceBuffer<MeshVertexDelta> finalDeltasBuffer_)
                        {
                            finalDeltasBuffer_.WriteToBuffer(finalVertexDeltas.AsArray(), 0, 0, finalVertexDeltas.Length);
                        }
                    }
                }

                indicesToPhysiqueUpdate.Clear();
                indicesToVariationUpdate.Clear();
                foreach (var index in indicesToPhysiqueUpdateNext) indicesToPhysiqueUpdate.Add(index);
                foreach (var index in indicesToVariationUpdateNext) indicesToVariationUpdate.Add(index);
                indicesToPhysiqueUpdateNext.Clear();
                indicesToVariationUpdateNext.Clear();
            }
            public void BeginNewJob()
            {
                hasActiveJob = false;
                activeJob.Complete();
                activeJob = default;

                insertionFlags.Clear();
                insertionFlags.AddReplicated(false, MaxInstanceCount);

                indicesToUpdate.Clear();

                int midlineVertexGroupPreMul = data.midlineVertexGroup * data.vertexCount;
#if UNITY_EDITOR
                if (midlineVertexGroupPreMul < 0) 
                {
                    Debug.LogError($"[{debugName}] Midline vertex group premul index is negative, which likely means no midline vertex group exists! ({midlineVertexGroupPreMul})");
                    return;
                }
#endif

                muscleGroupControlWeights.CopyFrom(muscleGroupControlWeightsNext);
                fatGroupControlWeights.CopyFrom(fatGroupControlWeightsNext);
                variationGroupControlWeights.CopyFrom(variationGroupControlWeightsNext);

                JobHandle physiqueUpdateHandle = default;
                if (indicesToPhysiqueUpdate.Length > 0)
                {
                    int2 fatShapeInfo = meshShapeInfos[data.fatShape];
                    int2 muscleShapeInfo = meshShapeInfos[data.massShape];
                    int2 fatMuscleBlendShapeInfo = meshShapeInfos[data.fatMuscleBlendShape];

                    foreach (var index in indicesToPhysiqueUpdate)
                    {
                        if (!insertionFlags[index])
                        {
                            indicesToUpdate.Add(index);
                            insertionFlags[index] = true;
                        }
                    }

                    var fatResetHandle = new ResetFatDataJob()
                    {
                        finalFatData = fatValuesPerVertex,
                        meshIndicesToUpdate = indicesToPhysiqueUpdate,
                        vertexCount = data.vertexCount
                    }.Schedule(indicesToPhysiqueUpdate.Length * data.vertexCount, 64, default);

                    var muscleResetHandle = new ResetMuscleDataJob()
                    {
                        finalMuscleData = muscleValuesPerVertex,
                        meshIndicesToUpdate = indicesToPhysiqueUpdate,
                        vertexCount = data.vertexCount
                    }.Schedule(indicesToPhysiqueUpdate.Length * data.vertexCount, 64, default);


                    var fatUpdateHandle = new UpdateMeshFatVertexDeltasJob()
                    {
                        vertexCount = data.vertexCount,
                        fatShapeIndex = fatShapeInfo,
                        combinedVertexCount = fatGroupVertexWeights.Length,
                        meshIndicesToUpdate = indicesToPhysiqueUpdate,
                        meshShapeDeltas = meshShapeDeltas,
                        meshShapeFrameWeights = meshShapeFrameWeights,
                        fatGroupControlWeights = fatGroupControlWeights,
                        fatGroupVertexWeights = fatGroupVertexWeights,

                        bustSizes = bustSizes,
                        bustNerfVertexGroupIndex = data.bustNerfVertexGroup,
                        vertexGroups = vertexGroups,
                        controlGroupCount = data.FatGroupsCount,

                        vertexDeltas = fatGroupVertexDeltasLR,
                        fatData = fatGroupVertexDataLR
                    }.Schedule(fatGroupVertexWeights.Length * indicesToPhysiqueUpdate.Length, 1, default);

                    var fatHandle = JobHandle.CombineDependencies(fatUpdateHandle, fatResetHandle);
                    for (int a = 0; a < data.FatGroupsCount; a++)
                    {
                        var groupInfo = fatGroupControlWeights[a];
                        fatHandle = new ApplyFatDataJob()
                        {
                            groupInfo = groupInfo,
                            meshVertexCount = data.vertexCount,
                            combinedVertexCount = fatGroupVertexWeights.Length,
                            vertexGroups = vertexGroups,
                            groupVertexWeights = fatGroupVertexWeights,
                            leftRightFlagBuffer = leftRightFlagBuffer,
                            meshIndicesToUpdate = indicesToPhysiqueUpdate,
                            fatDataLR = fatGroupVertexDataLR,
                            midlineVertexGroupIndexPreMul = midlineVertexGroupPreMul,

                            finalFatData = fatValuesPerVertex
                        }.Schedule(groupInfo.vertexCount * indicesToPhysiqueUpdate.Length, 1, fatHandle);
                    }

                    fatHandle = JobHandle.CombineDependencies(fatHandle, muscleResetHandle);

                    physiqueUpdateHandle = new UpdateMeshMuscleVertexDeltasJob()
                    {
                        minShapeMassWeight = data.minMassShapeWeight,
                        defaultShapeMassWeight = data.defaultMassShapeWeight,
                        muscleMassRange = 1f - data.defaultMassShapeWeight,

                        vertexCount = data.vertexCount,
                        muscleShapeIndex = muscleShapeInfo,
                        combinedVertexCount = muscleGroupVertexWeights.Length,
                        meshIndicesToUpdate = indicesToPhysiqueUpdate,
                        meshShapeDeltas = meshShapeDeltas,
                        meshShapeFrameWeights = meshShapeFrameWeights,
                        muscleGroupControlWeights = muscleGroupControlWeights,
                        muscleGroupVertexWeights = muscleGroupVertexWeights,

                        bustSizes = bustSizes,
                        bustNerfVertexGroupIndex = data.bustNerfVertexGroup,
                        vertexGroups = vertexGroups,
                        controlGroupCount = data.MuscleGroupsCount,

                        fatMuscleBlendShapeIndex = fatMuscleBlendShapeInfo,
                        fatValuesPerVertex = fatValuesPerVertex,

                        vertexDeltas = muscleGroupVertexDeltasLR,
                        muscleData = muscleGroupVertexDataLR
                    }.Schedule(muscleGroupVertexWeights.Length * indicesToPhysiqueUpdate.Length, 1, fatHandle);

                    for (int a = 0; a < data.MuscleGroupsCount; a++)
                    {
                        var groupInfo = muscleGroupControlWeights[a];
                        physiqueUpdateHandle = new ApplyMuscleDataJob()
                        {
                            groupInfo = groupInfo,
                            meshVertexCount = data.vertexCount,
                            combinedVertexCount = muscleGroupVertexWeights.Length,
                            vertexGroups = vertexGroups,
                            groupVertexWeights = muscleGroupVertexWeights,
                            leftRightFlagBuffer = leftRightFlagBuffer,
                            meshIndicesToUpdate = indicesToPhysiqueUpdate,
                            muscleDataLR = muscleGroupVertexDataLR,
                            midlineVertexGroupIndexPreMul = midlineVertexGroupPreMul,

                            finalMuscleData = muscleValuesPerVertex
                        }.Schedule(groupInfo.vertexCount * indicesToPhysiqueUpdate.Length, 1, physiqueUpdateHandle);
                    }
                }

                JobHandle variationUpdateHandle = default;
                if (indicesToVariationUpdate.Length > 0)
                {
                    foreach (var index in indicesToVariationUpdate)
                    {
                        if (!insertionFlags[index])
                        {
                            indicesToUpdate.Add(index);
                            insertionFlags[index] = true;
                        }
                    }
                }

                if (indicesToUpdate.Length > 0)
                {
                    //Debug.Log($"Updating {indicesToUpdate.Length} meshes");

                    JobHandle resetHandle = default;
                    if (indicesToVariationUpdate.Length > 0)
                    {
                        resetHandle = new ResetFinalVertexDeltasJob()
                        {
                            finalVertexDeltas = variationVertexDeltas,
                            meshIndicesToUpdate = indicesToVariationUpdate,
                            vertexCount = data.vertexCount
                        }.Schedule(indicesToVariationUpdate.Length * data.vertexCount, 64, resetHandle); // variation deltas are treated seperately from physique and only update when a variation group control weight is changed (which is supposed to be very rare)

                        resetHandle = JobHandle.CombineDependencies(resetHandle, variationUpdateHandle);

                        int variationControlGroupCount = data.VariationGroupsCount * data.VariationShapesCount;
                        for (int a = 0; a < data.VariationGroupsCount; a++) // Variation
                        {
                            var groupInfo = variationGroupControlWeights[a * data.VariationShapesCount]; // we only need the vertexCount and vertexSequenceStartIndex from this data, which is the same for every variation shape, so we can safely use the first shape info of each group
                            int indexCount = groupInfo.vertexCount * indicesToVariationUpdate.Length;
                            for (int b = 0; b < data.VariationShapesCount; b++)
                            {
                                //var groupInfo = variationGroupControlWeights[(a * data.VariationShapesCount) + b]; // see above comment
                                resetHandle = new UpdateMeshVariationVertexDeltasJob()
                                {
                                    groupIndex = a,
                                    shapeIndex = b,
                                    controlIndex = ((a * data.VariationShapesCount) + b),

                                    groupEntryCount = groupInfo.vertexCount,

                                    vertexCount = data.vertexCount,
                                    vertexGroups = vertexGroups,

                                    meshIndicesToUpdate = indicesToVariationUpdate,
                                    meshShapeDeltas = meshShapeDeltas,
                                    meshShapeFrameWeights = meshShapeFrameWeights,
                                    meshShapeIndices = meshShapeInfos,
                                    variationGroupControlWeights = variationGroupControlWeights,
                                    variationGroupVertexWeights = variationGroupVertexWeights,

                                    variationShapesCount = data.VariationShapesCount,
                                    variationShapesStartIndex = data.variationShapes.x,
                                    controlGroupCount = variationControlGroupCount,

                                    leftRightFlagBuffer = leftRightFlagBuffer,
                                    midlineVertexGroupIndexPreMul = midlineVertexGroupPreMul,

                                    finalVertexDeltas = variationVertexDeltas
                                }.Schedule(groupInfo.vertexCount * indicesToVariationUpdate.Length, 1, resetHandle);  
                            }
                        }
                    }

                    JobHandle finalizeHandle = JobHandle.CombineDependencies(resetHandle, physiqueUpdateHandle);

                    finalizeHandle = new ResetFinalVertexDeltasToTargetDeltasJob()
                    {
                        targetVertexDeltas = variationVertexDeltas,
                        finalVertexDeltas = finalVertexDeltas,
                        meshIndicesToUpdate = indicesToUpdate,
                        vertexCount = data.vertexCount
                    }.Schedule(indicesToUpdate.Length * data.vertexCount, 64, finalizeHandle); // use variation deltas as reset base

                    //if (indicesToPhysiqueUpdate.Length > 0)
                    //{
                    for (int a = 0; a < data.FatGroupsCount; a++) // Fat
                        {
                            var groupInfo = fatGroupControlWeights[a];
                            finalizeHandle = new ApplyGroupVertexDeltasJob()
                            {
                                groupInfo = groupInfo,
                                meshVertexCount = data.vertexCount,
                                combinedVertexCount = fatGroupVertexWeights.Length,
                                vertexGroups = vertexGroups,
                                groupVertexWeights = fatGroupVertexWeights,
                                leftRightFlagBuffer = leftRightFlagBuffer,
                                meshIndicesToUpdate = indicesToUpdate,//indicesToPhysiqueUpdate,
                                vertexDeltasLR = fatGroupVertexDeltasLR,
                                midlineVertexGroupIndexPreMul = midlineVertexGroupPreMul,

                                finalVertexDeltas = finalVertexDeltas
                            }.Schedule(groupInfo.vertexCount * indicesToUpdate.Length/*indicesToPhysiqueUpdate.Length*/, 1, finalizeHandle);
                        }

                        for (int a = 0; a < data.MuscleGroupsCount; a++) // Muscle
                        {
                            var groupInfo = muscleGroupControlWeights[a];
                            finalizeHandle = new ApplyGroupVertexDeltasJob()
                            {
                                groupInfo = groupInfo,
                                meshVertexCount = data.vertexCount,
                                combinedVertexCount = muscleGroupVertexWeights.Length,
                                vertexGroups = vertexGroups,
                                groupVertexWeights = muscleGroupVertexWeights,
                                leftRightFlagBuffer = leftRightFlagBuffer,
                                meshIndicesToUpdate = indicesToUpdate,//indicesToPhysiqueUpdate,
                                vertexDeltasLR = muscleGroupVertexDeltasLR,
                                midlineVertexGroupIndexPreMul = midlineVertexGroupPreMul,

                                finalVertexDeltas = finalVertexDeltas
                            }.Schedule(groupInfo.vertexCount * indicesToUpdate.Length/*indicesToPhysiqueUpdate.Length*/, 1, finalizeHandle);
                        }
                    //}

                    if (data.bustSizeShape >= 0 && data.bustShapeShape >= 0)
                    {
                        finalizeHandle = new ApplyBreastShapeJob()
                        {
                            bustSizes = bustSizes,
                            bustSizeShapeIndex = meshShapeInfos[data.bustSizeShape],
                            breastShapeIndex = meshShapeInfos[data.bustShapeShape],
                            meshIndicesToUpdate = indicesToUpdate,
                            meshShapeDeltas = meshShapeDeltas,
                            meshShapeFrameWeights = meshShapeFrameWeights,
                            vertexCount = data.vertexCount,

                            bustSizeMuscleShapeIndex = meshShapeInfos[data.bustSizeMuscleShape],
                            muscleData = muscleValuesPerVertex,
                            muscleMassStartWeight = data.defaultMassShapeWeight,
                            muscleMassRange = 1f - data.defaultMassShapeWeight,
                             

                            finalVertexDeltas = finalVertexDeltas
                        }.Schedule(data.vertexCount * indicesToUpdate.Length, 1, finalizeHandle); 

                    }

                    activeJob = finalizeHandle;
                    hasActiveJob = true;
                }

            }

            private bool initialized;
            public bool IsInitialized => initialized;

            public MeshGroupV2(SerializedData data)
            {
                this.data = data;
            }

            private int finalVertexDeltasBufferIndex = -1;
#if UNITY_EDITOR
            private string debugName;
            public void Initialize(string debug)
#else
            public void Initialize()
#endif
            {
                if (IsInitialized || data == null || disposed) return;

#if UNITY_EDITOR
                debugName = debug;
#endif

                List<float> tempFloats = new List<float>();
                List<GroupControlWeight2> tempGroupControlWeights = new List<GroupControlWeight2>();
                List<GroupVertexControlWeight> tempGroupVertexWeights = new List<GroupVertexControlWeight>();

                maxInstanceCount = 8;
                for (int a = maxInstanceCount - 1; a >= 0; a--) openIndices.Add(a);
                int initialVertexCount = maxInstanceCount * data.vertexCount;

                indicesToUpdate = new NativeList<int>(maxInstanceCount, Allocator.Persistent);
                indicesToPhysiqueUpdate = new NativeList<int>(maxInstanceCount, Allocator.Persistent);
                indicesToVariationUpdate = new NativeList<int>(maxInstanceCount, Allocator.Persistent);
                insertionFlags = new NativeList<bool>(maxInstanceCount, Allocator.Persistent);
                bustSizes = new NativeList<float2>(maxInstanceCount, Allocator.Persistent);
                bustSizes.AddReplicated(0f, maxInstanceCount);

                //data.TryPrecache();
                data.Precache(); // only precaches what isn't already 
                bool isPrecached = data.IsPrecached;
                if (!isPrecached)
                {
#if UNITY_EDITOR
                    Debug.LogWarning($"Customizable mesh data {debug} is not precached. Performance may be impacted.");
#else
                    Debug.LogWarning($"Customizable mesh data is not precached. Performance may be impacted.");
#endif
                }

#if UNITY_EDITOR
                Debug.Log($"CHECKING DATA FOR {debug}");
                for(int a = data.standaloneShapes.x; a <= data.standaloneShapes.y; a++) 
                {
                    var shape = data.GetShape(a);
                    if (shape == null)
                    {
                        Debug.LogWarning($"DATA HAS NULL STANDALONE SHAPE");
                        continue;
                    }
                    if (shape.frames == null || shape.frames.Length <= 0)
                    {
                        Debug.LogWarning($"STANDALONE SHAPE {shape.name} HAS NULL OR NO FRAMES");
                        continue;
                    }
                    for(int b = 0; b < shape.frames.Length; b++)
                    {
                        var frame = shape.frames[b];
                        if (frame.deltas == null || frame.deltas.Length <= 0)
                        {
                            Debug.LogWarning($"STANDALONE SHAPE {shape.name} FRAME {b} HAS NULL OR NO DELTAS");
                            continue;
                        }

                        bool emptyFlag = true;
                        for(int c = 0; c < frame.deltas.Length; c++)
                        {
                            var delta = frame.deltas[c];
                            if (math.any(delta.deltaVertex != float3.zero) || math.any(delta.deltaNormal != float3.zero) || math.any(delta.deltaTangent != float3.zero))
                            {
                                emptyFlag = false;
                                break;
                            }
                        }
                        if (emptyFlag)
                        {
                            Debug.LogWarning($"ALL DATA FOR STANDALONE SHAPE {shape.name} FRAME {b} IS ZERO");
                        }
                    }
                }
#endif
                meshShapeDeltas = new NativeArray<MeshVertexDelta>(data.precache_meshShapeDeltas, Allocator.Persistent);
                meshShapeInfos = new NativeArray<int2>(data.precache_meshShapeInfos, Allocator.Persistent);
                meshShapeFrameWeights = new NativeArray<float>(data.precache_meshShapeFrameWeights, Allocator.Persistent);

                vertexGroups = new NativeArray<float>(data.precache_vertexGroups, Allocator.Persistent);

                leftRightFlagBuffer = new NativeArray<bool>(data.leftRightFlags, Allocator.Persistent);


                #region Init Muscle Groups

                blankMuscleGroupControlWeights = new NativeArray<GroupControlWeight2>(data.precache_blankMuscleGroupControlWeights, Allocator.Persistent);
                muscleGroupVertexWeights = new NativeArray<GroupVertexControlWeight>(data.precache_muscleGroupVertexWeights, Allocator.Persistent);

                muscleGroupControlWeights = new NativeList<GroupControlWeight2>(blankMuscleGroupControlWeights.Length * maxInstanceCount, Allocator.Persistent);
                muscleGroupControlWeightsNext = new NativeList<GroupControlWeight2>(blankMuscleGroupControlWeights.Length * maxInstanceCount, Allocator.Persistent);
                for (int i = 0; i < maxInstanceCount; i++)
                {
                    muscleGroupControlWeights.AddRange(blankMuscleGroupControlWeights);
                    muscleGroupControlWeightsNext.AddRange(blankMuscleGroupControlWeights);
                }
                int muscleGroupDeltasCount = muscleGroupVertexWeights.Length * maxInstanceCount;
                muscleGroupVertexDeltasLR = new NativeList<MeshVertexDeltaLR>(muscleGroupDeltasCount, Allocator.Persistent);
                muscleGroupVertexDataLR = new NativeList<float2>(muscleGroupDeltasCount, Allocator.Persistent);
                if (muscleGroupDeltasCount > 0) 
                { 
                    muscleGroupVertexDeltasLR.AddReplicated(MeshVertexDeltaLR.Default, muscleGroupDeltasCount);
                    muscleGroupVertexDataLR.AddReplicated(float2.zero, muscleGroupDeltasCount);
                }

                muscleValuesPerVertex = new NativeList<float>(initialVertexCount, Allocator.Persistent);
                muscleValuesPerVertex.AddReplicated(0f, initialVertexCount);

                #endregion

                #region Init Fat Groups

                blankFatGroupControlWeights = new NativeArray<GroupControlWeight2>(data.precache_blankFatGroupControlWeights, Allocator.Persistent);
                fatGroupVertexWeights = new NativeArray<GroupVertexControlWeight>(data.precache_fatGroupVertexWeights, Allocator.Persistent);

                fatGroupControlWeights = new NativeList<GroupControlWeight2>(blankFatGroupControlWeights.Length * maxInstanceCount, Allocator.Persistent);
                fatGroupControlWeightsNext = new NativeList<GroupControlWeight2>(blankFatGroupControlWeights.Length * maxInstanceCount, Allocator.Persistent);
                for (int i = 0; i < maxInstanceCount; i++)
                {
                    fatGroupControlWeights.AddRange(blankFatGroupControlWeights);
                    fatGroupControlWeightsNext.AddRange(blankFatGroupControlWeights);
                }
                int fatGroupDeltasCount = fatGroupVertexWeights.Length * maxInstanceCount;
                fatGroupVertexDeltasLR = new NativeList<MeshVertexDeltaLR>(fatGroupDeltasCount, Allocator.Persistent);
                fatGroupVertexDataLR = new NativeList<float4>(fatGroupDeltasCount, Allocator.Persistent);
                if (fatGroupDeltasCount > 0) 
                { 
                    fatGroupVertexDeltasLR.AddReplicated(MeshVertexDeltaLR.Default, fatGroupDeltasCount);
                    fatGroupVertexDataLR.AddReplicated(float4.zero, fatGroupDeltasCount);
                }

                fatValuesPerVertex = new NativeList<float2>(initialVertexCount, Allocator.Persistent);
                fatValuesPerVertex.AddReplicated(float2.zero, initialVertexCount);

                #endregion

                #region Init Variation Groups
                
                blankVariationGroupControlWeights = new NativeArray<GroupControlWeight2>(data.precache_blankVariationGroupControlWeights, Allocator.Persistent);
                variationGroupVertexWeights = new NativeArray<GroupVertexControlWeight>(data.precache_variationGroupVertexWeights, Allocator.Persistent);

                variationGroupControlWeights = new NativeList<GroupControlWeight2>(blankVariationGroupControlWeights.Length * maxInstanceCount, Allocator.Persistent);
                variationGroupControlWeightsNext = new NativeList<GroupControlWeight2>(blankVariationGroupControlWeights.Length * maxInstanceCount, Allocator.Persistent);
                for (int i = 0; i < maxInstanceCount; i++)
                {
                    variationGroupControlWeights.AddRange(blankVariationGroupControlWeights);
                    variationGroupControlWeightsNext.AddRange(blankVariationGroupControlWeights);
                }

                #endregion

                int initialDeltasCount = data.vertexCount * maxInstanceCount;
                finalVertexDeltas = new NativeList<MeshVertexDelta>(initialDeltasCount, Allocator.Persistent);
                finalVertexDeltas.AddReplicated(MeshVertexDelta.Default, initialDeltasCount);

                variationVertexDeltas = new NativeList<MeshVertexDelta>(initialDeltasCount, Allocator.Persistent);
                variationVertexDeltas.AddReplicated(MeshVertexDelta.Default, initialDeltasCount); 

                finalVertexDeltasBufferIndex = CreateInstanceMaterialBuffer<MeshVertexDelta>(data.PerVertexDeltaDataPropertyName, data.vertexCount, 3, true, out var finalVertexDeltasBuffer);
                EnsureInstanceBufferSize(finalVertexDeltasBuffer);
#if UNITY_EDITOR
                Debug.Log($"{MaxInstanceCount} -- {finalVertexDeltasBuffer.InstanceCount}");
#endif
                finalVertexDeltasBuffer.WriteToBuffer(finalVertexDeltas.AsArray(), 0, 0, finalVertexDeltas.Length); 

                initialized = true;

                TrackDisposables();

#if UNITY_EDITOR
                //Utils.PrintNativeAllocationSizes($"MeshGroupV2 {debug}", this);
#endif
            }

#if UNITY_EDITOR
            public InstanceV2 ClaimNewInstance(string debug)
#else
            public InstanceV2 ClaimNewInstance()
#endif
            {
                if (disposed) return null;

                Initialize(debug);

                int index;
                if (openIndices.Count > 0) // use an existing index
                {
                    int listIndex = openIndices.Count - 1; // use last index to avoid repositioning other elements
                    index = openIndices[listIndex];
                    openIndices.RemoveAt(listIndex);
                }
                else // create a new index
                {
                    index = maxInstanceCount;
                    maxInstanceCount++;

                    bustSizes.Add(0f);

                    muscleGroupControlWeights.AddRange(blankMuscleGroupControlWeights);
                    muscleGroupControlWeightsNext.AddRange(blankMuscleGroupControlWeights);
                    muscleGroupVertexDeltasLR.AddReplicated(MeshVertexDeltaLR.Default, muscleGroupVertexWeights.Length);

                    muscleGroupVertexDataLR.AddReplicated(float2.zero, muscleGroupVertexWeights.Length);
                    muscleValuesPerVertex.AddReplicated(0f, data.vertexCount);

                    fatGroupControlWeights.AddRange(blankFatGroupControlWeights);
                    fatGroupControlWeightsNext.AddRange(blankFatGroupControlWeights);
                    fatGroupVertexDeltasLR.AddReplicated(MeshVertexDeltaLR.Default, fatGroupVertexWeights.Length);

                    fatGroupVertexDataLR.AddReplicated(float4.zero, fatGroupVertexWeights.Length);
                    fatValuesPerVertex.AddReplicated(float2.zero, data.vertexCount);

                    variationGroupControlWeights.AddRange(blankVariationGroupControlWeights);
                    variationGroupControlWeightsNext.AddRange(blankVariationGroupControlWeights);

                    finalVertexDeltas.AddReplicated(MeshVertexDelta.Default, data.vertexCount); // expand the vertex delta buffer
                    variationVertexDeltas.AddReplicated(MeshVertexDelta.Default, data.vertexCount);

                    EnsureInstanceBufferSizes();
                }

                activeIndices.Add(index);

                var instance = new InstanceV2(this);
                instance.localID = index;

                return instance;
            }

            public void ReleaseInstance(InstanceV2 instance)
            {
                if (instance == null || instance.OwnerGroup != this) return;

                if (instance.localID >= 0)
                {
                    int listIndex = activeIndices.IndexOf(instance.localID);
                    if (listIndex >= 0) activeIndices.RemoveAtSwapBack(listIndex);
                    openIndices.Add(instance.localID);

                    instance.localID = -1;
                }

                if (!instance.IsDisposed) instance.Dispose(false);
            }

        }

        #region Fat Jobs

        [BurstCompile]
        public struct UpdateMeshFatVertexDeltasJob : IJobParallelFor
        {

            public int vertexCount;

            /// <summary>
            /// The combined vertex count for all muscle groups
            /// </summary>
            public int combinedVertexCount;

            public int controlGroupCount;

            public int bustNerfVertexGroupIndex;

            public int2 fatShapeIndex;

            [ReadOnly]
            public NativeList<int> meshIndicesToUpdate;

            [ReadOnly]
            public NativeArray<float> vertexGroups;

            [ReadOnly]
            public NativeArray<MeshVertexDelta> meshShapeDeltas;
            [ReadOnly]
            public NativeArray<float> meshShapeFrameWeights;

            [ReadOnly]
            public NativeList<float2> bustSizes;

            [ReadOnly]
            public NativeList<GroupControlWeight2> fatGroupControlWeights;

            /// <summary>
            /// Static array that contains the packed vertex indices and weights for each fat group, stored sequentially.
            /// </summary>
            [ReadOnly]
            public NativeArray<GroupVertexControlWeight> fatGroupVertexWeights;

            [NativeDisableParallelForRestriction]
            public NativeList<MeshVertexDeltaLR> vertexDeltas;
            [NativeDisableParallelForRestriction]
            public NativeList<float4> fatData;

            public void Execute(int vertexSequenceIndex)
            {
                int meshIndexBufferIndex = vertexSequenceIndex / combinedVertexCount;
                int meshIndex = meshIndicesToUpdate[meshIndexBufferIndex];

                var localVertexSequenceIndex = vertexSequenceIndex - (meshIndexBufferIndex * combinedVertexCount);
                var groupVertexWeight = fatGroupVertexWeights[localVertexSequenceIndex];

                int fatGroupControlIndex = (meshIndex * controlGroupCount) + groupVertexWeight.groupIndex;
                GroupControlWeight2 fatGroupControlData = fatGroupControlWeights[fatGroupControlIndex];

                float bustNerfFactor = CalculateBustNerfFactor(bustSizes[meshIndex].x, vertexGroups, bustNerfVertexGroupIndex, vertexCount, groupVertexWeight.vertexIndex, 0.6f);

                var shapeDelta = meshShapeDeltas.SampleDeltaShapeBuffer(groupVertexWeight.vertexIndex, meshShapeFrameWeights, fatShapeIndex.x, fatShapeIndex.y, fatGroupControlData.weight.x, vertexCount) * groupVertexWeight.weight * bustNerfFactor;
                var fatDelta = fatGroupControlData.weight * groupVertexWeight.weight;

                int bufferIndex = (meshIndex * combinedVertexCount) + localVertexSequenceIndex;
                vertexDeltas[bufferIndex] = new MeshVertexDeltaLR() { deltaLeft = shapeDelta, deltaRight = shapeDelta };
                fatData[bufferIndex] = new float4(fatDelta.x, fatDelta.y, fatDelta.x, fatDelta.y);
            }

        }

        [BurstCompile]
        public struct ResetFatDataJob : IJobParallelFor
        {

            public int vertexCount;

            [ReadOnly]
            public NativeList<int> meshIndicesToUpdate;

            [NativeDisableParallelForRestriction]
            public NativeList<float2> finalFatData;

            public void Execute(int vertexSequenceIndex)
            {
                int meshIndexBufferIndex = vertexSequenceIndex / vertexCount;
                int meshIndex = meshIndicesToUpdate[meshIndexBufferIndex];

                int vertexIndex = vertexSequenceIndex - (meshIndexBufferIndex * vertexCount);

                int finalindex = vertexIndex + (meshIndex * vertexCount);
                finalFatData[finalindex] = float2.zero;
            }

        }

        /// <summary>
        /// Applies partial fat data from initial job to final fat buffer, one group per job. 
        /// </summary>
        [BurstCompile]
        public struct ApplyFatDataJob : IJobParallelFor
        {

            public GroupControlWeight2 groupInfo;

            public int meshVertexCount;

            /// <summary>
            /// The combined vertex count for all fat groups
            /// </summary>
            public int combinedVertexCount;

            public int midlineVertexGroupIndexPreMul;

            [ReadOnly]
            public NativeList<int> meshIndicesToUpdate;

            [ReadOnly]
            public NativeArray<float> vertexGroups;

            [ReadOnly]
            public NativeArray<bool> leftRightFlagBuffer;

            [ReadOnly]
            public NativeArray<GroupVertexControlWeight> groupVertexWeights;
            [ReadOnly]
            public NativeList<float4> fatDataLR;

            [NativeDisableParallelForRestriction]
            public NativeList<float2> finalFatData;

            public void Execute(int vertexSequenceIndex)
            {

                int meshIndexBufferIndex = vertexSequenceIndex / groupInfo.vertexCount;
                int meshIndex = meshIndicesToUpdate[meshIndexBufferIndex];

                var groupVertexIndex = groupInfo.vertexSequenceStartIndex + (vertexSequenceIndex - (meshIndexBufferIndex * groupInfo.vertexCount));
                var indexInGroupDeltaBuffer = (combinedVertexCount * meshIndex) + groupVertexIndex;
                var groupVertexWeight = groupVertexWeights[groupVertexIndex];

                var vertexIndex = groupVertexWeight.vertexIndex;

                float midlineWeight = vertexGroups[midlineVertexGroupIndexPreMul + vertexIndex];
                float2 weightLeftRight = math.lerp(math.select(new float2(1f, 0f), new float2(0f, 1f), leftRightFlagBuffer[vertexIndex]), new float2(0.5f, 0.5f), midlineWeight);

                var dataLR = fatDataLR[indexInGroupDeltaBuffer];
                int finalindex = (meshIndex * meshVertexCount) + vertexIndex;
                finalFatData[finalindex] = finalFatData[finalindex] + (new float2(dataLR.x, dataLR.y) * weightLeftRight.x) + (new float2(dataLR.z, dataLR.w) * weightLeftRight.y);

            }

        }

        #endregion

        #region Muscle Jobs

        [BurstCompile]
        public struct ResetMuscleDataJob : IJobParallelFor
        {

            public int vertexCount;

            [ReadOnly]
            public NativeList<int> meshIndicesToUpdate;

            [NativeDisableParallelForRestriction]
            public NativeList<float> finalMuscleData;

            public void Execute(int vertexSequenceIndex)
            {
                int meshIndexBufferIndex = vertexSequenceIndex / vertexCount;
                int meshIndex = meshIndicesToUpdate[meshIndexBufferIndex];

                int vertexIndex = vertexSequenceIndex - (meshIndexBufferIndex * vertexCount);

                int finalindex = vertexIndex + (meshIndex * vertexCount);
                finalMuscleData[finalindex] = 0f;
            }

        }

        [BurstCompile]
        public struct UpdateMeshMuscleVertexDeltasJob : IJobParallelFor
        {

            public int vertexCount;

            /// <summary>
            /// The combined vertex count for all muscle groups
            /// </summary>
            public int combinedVertexCount;

            public int controlGroupCount;

            public int bustNerfVertexGroupIndex;

            public int2 muscleShapeIndex;
            public int2 fatMuscleBlendShapeIndex;

            public float minShapeMassWeight;
            public float defaultShapeMassWeight;
            public float muscleMassRange;

            [ReadOnly]
            public NativeList<int> meshIndicesToUpdate;

            [ReadOnly]
            public NativeArray<MeshVertexDelta> meshShapeDeltas;
            [ReadOnly]
            public NativeArray<float> meshShapeFrameWeights;

            [ReadOnly]
            public NativeList<float2> bustSizes;

            [ReadOnly]
            public NativeList<GroupControlWeight2> muscleGroupControlWeights;

            /// <summary>
            /// Static array that contains the packed vertex indices and weights for each muscle group, stored sequentially.
            /// </summary>
            [ReadOnly]
            public NativeArray<GroupVertexControlWeight> muscleGroupVertexWeights;

            [ReadOnly]
            public NativeArray<float> vertexGroups;

            [ReadOnly]
            public NativeList<float2> fatValuesPerVertex;

            [NativeDisableParallelForRestriction]
            public NativeList<MeshVertexDeltaLR> vertexDeltas;
            [NativeDisableParallelForRestriction]
            public NativeList<float2> muscleData;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static MeshVertexDeltaLR CalculateMuscleVertexDelta(int groupControlIndex, float groupWeightAtIndex, int vertexIndex, int vertexCount, float minShapeMassWeight, float defaultShapeMassWeight, float muscleMassRange, int2 muscleShapeIndex, int2 fatMuscleBlendShapeIndex, NativeList<GroupControlWeight2> muscleGroupControlWeights, NativeArray<float> meshShapeFrameWeights, NativeArray<MeshVertexDelta> meshShapeDeltas, NativeList<float2> fatValuesPerVertex, out float2 muscleGroupMassWeight)
            {
                GroupControlWeight2 muscleGroupMass = muscleGroupControlWeights[groupControlIndex];

                float2 fat = fatValuesPerVertex[vertexIndex];

                float bustNerfFactor = 1f;// CalculateBustNerfFactor(bustSizes[meshIndex].x, vertexGroups, bustNerfVertexGroupIndex, vertexCount, groupVertexWeight.vertexIndex, 0.45f);

                float muscleWeight = groupWeightAtIndex * bustNerfFactor * (1f - fat.y); // fat.y determines how much of the muscle shape to fade out based on fat level

                float minMassShapeWeight = math.max(minShapeMassWeight, defaultShapeMassWeight * math.pow(math.saturate(fat.x), 0.3f) * 0.7f);
                muscleGroupMassWeight = math.max(minMassShapeWeight, muscleGroupMass.weight);
                var shapeDeltaL = meshShapeDeltas.SampleDeltaShapeBuffer(vertexIndex, meshShapeFrameWeights, muscleShapeIndex.x, muscleShapeIndex.y, muscleGroupMassWeight.x, vertexCount) * muscleWeight;
                var shapeDeltaR = meshShapeDeltas.SampleDeltaShapeBuffer(vertexIndex, meshShapeFrameWeights, muscleShapeIndex.x, muscleShapeIndex.y, muscleGroupMassWeight.y, vertexCount) * muscleWeight;

                float fatMuscleWeight = muscleWeight * fat.x;

                float2 muscleGroupMassSat = math.saturate((muscleGroupMassWeight - defaultShapeMassWeight) / muscleMassRange);
                shapeDeltaL = shapeDeltaL + meshShapeDeltas.SampleDeltaShapeBuffer(vertexIndex, meshShapeFrameWeights, fatMuscleBlendShapeIndex.x, fatMuscleBlendShapeIndex.y, muscleGroupMassSat.x, vertexCount) * fatMuscleWeight;
                shapeDeltaR = shapeDeltaR + meshShapeDeltas.SampleDeltaShapeBuffer(vertexIndex, meshShapeFrameWeights, fatMuscleBlendShapeIndex.x, fatMuscleBlendShapeIndex.y, muscleGroupMassSat.y, vertexCount) * fatMuscleWeight;

                return new MeshVertexDeltaLR()
                {
                    deltaLeft = shapeDeltaL,
                    deltaRight = shapeDeltaR
                };
            }

            public void Execute(int vertexSequenceIndex)
            {
                int meshIndexBufferIndex = vertexSequenceIndex / combinedVertexCount;
                int meshIndex = meshIndicesToUpdate[meshIndexBufferIndex];

                var localVertexSequenceIndex = vertexSequenceIndex - (meshIndexBufferIndex * combinedVertexCount);
                var groupVertexWeight = muscleGroupVertexWeights[localVertexSequenceIndex];

                int muscleGroupControlIndex = (meshIndex * controlGroupCount) + groupVertexWeight.groupIndex;
                var deltas = CalculateMuscleVertexDelta(muscleGroupControlIndex, groupVertexWeight.weight, groupVertexWeight.vertexIndex, vertexCount, minShapeMassWeight, defaultShapeMassWeight, muscleMassRange, muscleShapeIndex, fatMuscleBlendShapeIndex, muscleGroupControlWeights, meshShapeFrameWeights, meshShapeDeltas, fatValuesPerVertex, out var muscleGroupMassWeight);

                int bufferIndex = (meshIndex * combinedVertexCount) + localVertexSequenceIndex;
                vertexDeltas[bufferIndex] = deltas;
                muscleData[bufferIndex] = muscleGroupMassWeight * groupVertexWeight.weight;
            }

        }

        [BurstCompile]
        public struct ApplyMuscleDataJob : IJobParallelFor
        {

            public GroupControlWeight2 groupInfo;

            public int meshVertexCount;

            /// <summary>
            /// The combined vertex count for all muscle groups
            /// </summary>
            public int combinedVertexCount;

            public int midlineVertexGroupIndexPreMul;

            [ReadOnly]
            public NativeList<int> meshIndicesToUpdate;

            [ReadOnly]
            public NativeArray<float> vertexGroups;

            [ReadOnly]
            public NativeArray<bool> leftRightFlagBuffer;

            [ReadOnly]
            public NativeArray<GroupVertexControlWeight> groupVertexWeights;
            [ReadOnly]
            public NativeList<float2> muscleDataLR;

            [NativeDisableParallelForRestriction]
            public NativeList<float> finalMuscleData;

            public void Execute(int vertexSequenceIndex)
            {

                int meshIndexBufferIndex = vertexSequenceIndex / groupInfo.vertexCount;
                int meshIndex = meshIndicesToUpdate[meshIndexBufferIndex];

                var groupVertexIndex = groupInfo.vertexSequenceStartIndex + (vertexSequenceIndex - (meshIndexBufferIndex * groupInfo.vertexCount));
                var indexInGroupDeltaBuffer = (combinedVertexCount * meshIndex) + groupVertexIndex;
                var groupVertexWeight = groupVertexWeights[groupVertexIndex];

                var vertexIndex = groupVertexWeight.vertexIndex;

                float midlineWeight = vertexGroups[midlineVertexGroupIndexPreMul + vertexIndex];
                float2 weightLeftRight = math.lerp(math.select(new float2(1f, 0f), new float2(0f, 1f), leftRightFlagBuffer[vertexIndex]), new float2(0.5f, 0.5f), midlineWeight);

                var dataLR = muscleDataLR[indexInGroupDeltaBuffer];
                int finalindex = (meshIndex * meshVertexCount) + vertexIndex;
                finalMuscleData[finalindex] = finalMuscleData[finalindex] + (dataLR.x * weightLeftRight.x) + (dataLR.y * weightLeftRight.y);

            }

        }

        #endregion

        #region Variation Jobs

        [BurstCompile]
        public struct UpdateMeshVariationVertexDeltasJob : IJobParallelFor
        {

            public int groupIndex;
            public int shapeIndex;
            public int controlIndex; // cached ((groupIndex * variationShapesCount) + shapeIndex)

            public int groupEntryCount;

            public int vertexCount;

            public int variationShapesStartIndex;
            public int variationShapesCount;

            public int controlGroupCount;

            public int midlineVertexGroupIndexPreMul;

            [ReadOnly]
            public NativeList<int> meshIndicesToUpdate;

            [ReadOnly]
            public NativeArray<float> vertexGroups;

            [ReadOnly]
            public NativeArray<MeshVertexDelta> meshShapeDeltas;
            [ReadOnly]
            public NativeArray<float> meshShapeFrameWeights;
            [ReadOnly]
            public NativeArray<int2> meshShapeIndices;

            [ReadOnly]
            public NativeList<GroupControlWeight2> variationGroupControlWeights;

            /// <summary>
            /// Static array that contains the packed vertex indices and weights for each variation group, stored sequentially.
            /// </summary>
            [ReadOnly]
            public NativeArray<GroupVertexControlWeight> variationGroupVertexWeights;

            [ReadOnly]
            public NativeArray<bool> leftRightFlagBuffer;

            [NativeDisableParallelForRestriction]
            public NativeList<MeshVertexDelta> finalVertexDeltas;

            public void Execute(int vertexSequenceIndex)
            {
                int meshIndexBufferIndex = vertexSequenceIndex / groupEntryCount;
                int meshIndex = meshIndicesToUpdate[meshIndexBufferIndex];

                int variationGroupControlIndexOffset = (meshIndex * controlGroupCount);
                GroupControlWeight2 variationGroupControlWeight = variationGroupControlWeights[controlIndex + variationGroupControlIndexOffset];

                int localVertexSequenceIndex = vertexSequenceIndex - (meshIndexBufferIndex * groupEntryCount);
                var groupVertexWeight = variationGroupVertexWeights[variationGroupControlWeight.vertexSequenceStartIndex + localVertexSequenceIndex]; 

                int vertexIndex = groupVertexWeight.vertexIndex;

                int2 variationShapeIndex = meshShapeIndices[variationShapesStartIndex + shapeIndex]; 
                var shapeDeltaL = meshShapeDeltas.SampleDeltaShapeBuffer(vertexIndex, meshShapeFrameWeights, variationShapeIndex.x, variationShapeIndex.y, variationGroupControlWeight.weight.x, vertexCount) * groupVertexWeight.weight;
                var shapeDeltaR = meshShapeDeltas.SampleDeltaShapeBuffer(vertexIndex, meshShapeFrameWeights, variationShapeIndex.x, variationShapeIndex.y, variationGroupControlWeight.weight.y, vertexCount) * groupVertexWeight.weight;

                float midlineWeight = vertexGroups[midlineVertexGroupIndexPreMul + vertexIndex];
                float2 weightLeftRight = math.lerp(math.select(new float2(1f, 0f), new float2(0f, 1f), leftRightFlagBuffer[vertexIndex]), new float2(0.5f, 0.5f), midlineWeight);

                int finalindex = (meshIndex * vertexCount) + vertexIndex;
                finalVertexDeltas[finalindex] = finalVertexDeltas[finalindex] + (shapeDeltaL * weightLeftRight.x) + (shapeDeltaR * weightLeftRight.y);
            }

        }

        #endregion

        #region Breast Jobs

        [BurstCompile]
        public struct ApplyBreastShapeJob : IJobParallelFor
        {

            public int vertexCount;

            [ReadOnly]
            public NativeList<float2> bustSizes;

            public float muscleMassStartWeight;
            public float muscleMassRange;

            public int2 bustSizeShapeIndex;
            public int2 bustSizeMuscleShapeIndex;
            public int2 breastShapeIndex;

            [ReadOnly]
            public NativeArray<MeshVertexDelta> meshShapeDeltas;
            [ReadOnly]
            public NativeArray<float> meshShapeFrameWeights;

            [ReadOnly]
            public NativeList<int> meshIndicesToUpdate;

            [ReadOnly]
            public NativeList<float> muscleData;

            [NativeDisableParallelForRestriction]
            public NativeList<MeshVertexDelta> finalVertexDeltas;

            public void Execute(int vertexSequenceIndex)
            {
                int meshIndexBufferIndex = vertexSequenceIndex / vertexCount;
                int meshIndex = meshIndicesToUpdate[meshIndexBufferIndex];

                int vertexIndex = vertexSequenceIndex - (meshIndexBufferIndex * vertexCount);

                float2 bustData = bustSizes[meshIndex];
                float2 bustDataSat = math.saturate(bustData.x);
                float muscleMass = math.saturate((muscleData[vertexIndex] - muscleMassStartWeight) / muscleMassRange);
                var sizeDelta = meshShapeDeltas.SampleDeltaShapeBuffer(vertexIndex, meshShapeFrameWeights, bustSizeShapeIndex.x, bustSizeShapeIndex.y, bustData.x, vertexCount);
                var sizeMuscleDelta = meshShapeDeltas.SampleDeltaShapeBuffer(vertexIndex, meshShapeFrameWeights, bustSizeMuscleShapeIndex.x, bustSizeMuscleShapeIndex.y, muscleMass, vertexCount);
                var shapeDelta = meshShapeDeltas.SampleDeltaShapeBuffer(vertexIndex, meshShapeFrameWeights, breastShapeIndex.x, breastShapeIndex.y, bustData.y, vertexCount);

                sizeDelta = sizeDelta + (sizeMuscleDelta + shapeDelta) * bustDataSat.x;

                int finalindex = vertexIndex + (meshIndex * vertexCount);
                finalVertexDeltas[finalindex] = finalVertexDeltas[finalindex] + sizeDelta;
            }

        }

        #endregion

        [BurstCompile]
        public struct ResetFinalVertexDeltasJob : IJobParallelFor
        {

            public int vertexCount;

            [ReadOnly]
            public NativeList<int> meshIndicesToUpdate;

            [NativeDisableParallelForRestriction]
            public NativeList<MeshVertexDelta> finalVertexDeltas;

            public void Execute(int vertexSequenceIndex)
            {
                int meshIndexBufferIndex = vertexSequenceIndex / vertexCount;
                int meshIndex = meshIndicesToUpdate[meshIndexBufferIndex];

                int vertexIndex = vertexSequenceIndex - (meshIndexBufferIndex * vertexCount);

                int finalindex = vertexIndex + (meshIndex * vertexCount);
                finalVertexDeltas[finalindex] = MeshVertexDelta.Default;
            }

        }

        [BurstCompile]
        public struct ResetFinalVertexDeltasToTargetDeltasJob : IJobParallelFor
        {

            public int vertexCount;

            [ReadOnly]
            public NativeList<int> meshIndicesToUpdate;

            [NativeDisableParallelForRestriction]
            public NativeList<MeshVertexDelta> targetVertexDeltas;

            [NativeDisableParallelForRestriction]
            public NativeList<MeshVertexDelta> finalVertexDeltas;

            public void Execute(int vertexSequenceIndex)
            {
                int meshIndexBufferIndex = vertexSequenceIndex / vertexCount;
                int meshIndex = meshIndicesToUpdate[meshIndexBufferIndex];

                int vertexIndex = vertexSequenceIndex - (meshIndexBufferIndex * vertexCount);

                int finalindex = vertexIndex + (meshIndex * vertexCount);
                finalVertexDeltas[finalindex] = targetVertexDeltas[finalindex];
            }

        }

        /// <summary>
        /// Applies partial vertex deltas from previous jobs to final vertex deltas buffer, one group per job. 
        /// </summary>
        [BurstCompile]
        public struct ApplyGroupVertexDeltasJob : IJobParallelFor
        {

            public GroupControlWeight2 groupInfo;

            public int meshVertexCount;

            /// <summary>
            /// The combined vertex count for all groups
            /// </summary>
            public int combinedVertexCount;

            public int midlineVertexGroupIndexPreMul;

            [ReadOnly]
            public NativeList<int> meshIndicesToUpdate;

            [ReadOnly]
            public NativeArray<float> vertexGroups;

            [ReadOnly]
            public NativeArray<bool> leftRightFlagBuffer;

            [ReadOnly]
            public NativeArray<GroupVertexControlWeight> groupVertexWeights;
            [ReadOnly]
            public NativeList<MeshVertexDeltaLR> vertexDeltasLR;

            [NativeDisableParallelForRestriction]
            public NativeList<MeshVertexDelta> finalVertexDeltas;

            public void Execute(int vertexSequenceIndex)
            {

                int meshIndexBufferIndex = vertexSequenceIndex / groupInfo.vertexCount;
                int meshIndex = meshIndicesToUpdate[meshIndexBufferIndex];

                var groupVertexIndex = groupInfo.vertexSequenceStartIndex + (vertexSequenceIndex - (meshIndexBufferIndex * groupInfo.vertexCount));
                var indexInGroupDeltaBuffer = (combinedVertexCount * meshIndex) + groupVertexIndex;
                var groupVertexWeight = groupVertexWeights[groupVertexIndex];

                var vertexIndex = groupVertexWeight.vertexIndex;
                
                float midlineWeight = vertexGroups[midlineVertexGroupIndexPreMul + vertexIndex];
                float2 weightLeftRight = math.lerp(math.select(new float2(1f, 0f), new float2(0f, 1f), leftRightFlagBuffer[vertexIndex]), new float2(0.5f, 0.5f), midlineWeight);

                var deltaLR = vertexDeltasLR[indexInGroupDeltaBuffer];
                int finalindex = (meshIndex * meshVertexCount) + vertexIndex;
                finalVertexDeltas[finalindex] = finalVertexDeltas[finalindex] + (deltaLR.deltaLeft * weightLeftRight.x) + (deltaLR.deltaRight * weightLeftRight.y);

            }

        }

        [BurstCompile]
        public struct ApplyGroupVertexDeltasWithIndexGroupsJob : IJobParallelFor
        {

            public GroupControlWeight2 groupInfo;

            public int meshVertexCount;

            public int indexOffset;
            public int indexGroupSize;

            /// <summary>
            /// The combined vertex count for all groups
            /// </summary>
            public int combinedVertexCount;

            public int midlineVertexGroupIndexPreMul;

            [ReadOnly]
            public NativeList<int> meshIndicesToUpdate;

            [ReadOnly]
            public NativeArray<float> vertexGroups;

            [ReadOnly]
            public NativeArray<bool> leftRightFlagBuffer;

            [ReadOnly]
            public NativeArray<GroupVertexControlWeight> groupVertexWeights;
            [ReadOnly]
            public NativeList<MeshVertexDeltaLR> vertexDeltasLR;

            [NativeDisableParallelForRestriction]
            public NativeList<MeshVertexDelta> finalVertexDeltas;

            public void Execute(int vertexSequenceIndex)
            {

                int meshIndexBufferIndex = vertexSequenceIndex / groupInfo.vertexCount; 
                int meshIndex = meshIndicesToUpdate[meshIndexBufferIndex];

                var groupVertexIndex = groupInfo.vertexSequenceStartIndex + (vertexSequenceIndex - (meshIndexBufferIndex * groupInfo.vertexCount));
                var indexInGroupDeltaBuffer = (combinedVertexCount * meshIndex) + (groupVertexIndex * indexGroupSize) + indexOffset;
                var groupVertexWeight = groupVertexWeights[groupVertexIndex];

                var vertexIndex = groupVertexWeight.vertexIndex;

                float midlineWeight = vertexGroups[midlineVertexGroupIndexPreMul + vertexIndex];
                float2 weightLeftRight = math.lerp(math.select(new float2(1f, 0f), new float2(0f, 1f), leftRightFlagBuffer[vertexIndex]), new float2(0.5f, 0.5f), midlineWeight);

                var deltaLR = vertexDeltasLR[indexInGroupDeltaBuffer];
                int finalindex = (meshIndex * meshVertexCount) + vertexIndex;
                finalVertexDeltas[finalindex] = finalVertexDeltas[finalindex] + (deltaLR.deltaLeft * weightLeftRight.x) + (deltaLR.deltaRight * weightLeftRight.y);

            }

        }

        public abstract class SerializedDataBase : ICustomizableCharacterMeshBaseData
        {

            #region Fields

            public string Name => GetType().Name;

            [Header("Rendering")]
            public Material[] materials;
            public bool HasMaterials => materials != null && materials.Length > 0;
            public int MaterialCount => materials == null ? 0 : materials.Length;
            public Material GetMaterial(int index) => index < 0 || index >= MaterialCount ? null : materials[index];
            public Material[] Materials => materials;

            [Tooltip("Render sets render the same mesh instance using different materials (useful for toon outline materials)")]
            public RenderSet[] renderSets;
            public bool HasRenderSets => renderSets != null && renderSets.Length > 1;
            public int RenderSetCount => renderSets == null ? 0 : renderSets.Length;
            public RenderSet GetRenderSet(int index) => index < 0 || index >= RenderSetCount ? default : renderSets[index];
            public RenderSet[] RenderSets => renderSets;

            public int vertexCount;
            public int VertexCount => vertexCount;

            public MeshLOD[] meshLODs;
            public Mesh Mesh => meshLODs == null || meshLODs.Length <= 0 ? null : meshLODs[0].mesh;
            public int LevelsOfDetail => meshLODs == null ? 0 : meshLODs.Length;
            public Mesh GetMesh(int lod) => meshLODs == null ? null : GetMeshUnsafe(Mathf.Clamp(lod, 0, meshLODs.Length - 1));
            public Mesh GetMeshUnsafe(int lod) => meshLODs[lod].mesh;
            public MeshLOD GetLOD(int lod) => meshLODs == null ? default : GetLODUnsafe(Mathf.Clamp(lod, 0, meshLODs.Length - 1));
            public MeshLOD GetLODUnsafe(int lod) => meshLODs[lod];

            [NonSerialized]
            private NativeArray<float3>[] meshVertices;
            [NonSerialized]
            private NativeArray<float3>[] meshNormals;
            [NonSerialized]
            private NativeArray<float4>[] meshTangents;
            [NonSerialized]
            private NativeArray<float4>[] meshColors;
            [NonSerialized]
            private NativeArray<int>[] meshTriangles;
            [NonSerialized]
            private NativeArray<BoneWeight8>[] meshBoneWeights;
            [NonSerialized]
            private NativeArray<float4>[] meshUV0s;
            [NonSerialized]
            private NativeArray<float4>[] meshUV1s;
            [NonSerialized]
            private NativeArray<float4>[] meshUV2s;
            [NonSerialized]
            private NativeArray<float4>[] meshUV3s;

            public bool TryGetVertices(int lod, out NativeArray<float3> array)
            {
                array = default;
                if (meshLODs == null || lod < 0 || lod >= meshLODs.Length) return false;

                var mesh = GetMeshUnsafe(lod);
                if (mesh == null) return false;

                if (meshVertices != null && meshVertices.Length >= meshLODs.Length && meshVertices[lod].IsCreated)
                {
                    array = meshVertices[lod];
                    return true;
                }

                //MeshUtils._tempV3.Clear();
                //mesh.GetVertices(MeshUtils._tempV3);
                array = new NativeArray<Vector3>(mesh.vertices, Allocator.Persistent).Reinterpret<float3>();
                if (meshVertices == null || meshVertices.Length != meshLODs.Length)
                {
                    if (meshVertices != null)
                    {
                        foreach (var array_ in meshVertices) if (array_.IsCreated) array_.Dispose();
                    }

                    meshVertices = new NativeArray<float3>[meshLODs.Length];
                }

                meshVertices[lod] = array;

                return true;
            }
            public bool TryGetNormals(int lod, out NativeArray<float3> array)
            {
                array = default;
                if (meshLODs == null || lod < 0 || lod >= meshLODs.Length) return false;

                var mesh = GetMeshUnsafe(lod);
                if (mesh == null) return false;

                if (meshNormals != null && meshNormals.Length >= meshLODs.Length && meshNormals[lod].IsCreated)
                {
                    array = meshNormals[lod];
                    return true;
                }

                array = new NativeArray<Vector3>(mesh.normals, Allocator.Persistent).Reinterpret<float3>();
                if (meshNormals == null || meshNormals.Length != meshLODs.Length)
                {
                    if (meshNormals != null)
                    {
                        foreach (var array_ in meshNormals) if (array_.IsCreated) array_.Dispose();
                    }

                    meshNormals = new NativeArray<float3>[meshLODs.Length];
                }

                meshNormals[lod] = array;

                return true;
            }
            public bool TryGetTangents(int lod, out NativeArray<float4> array)
            {
                array = default;
                if (meshLODs == null || lod < 0 || lod >= meshLODs.Length) return false;

                var mesh = GetMeshUnsafe(lod);
                if (mesh == null) return false;

                if (meshTangents != null && meshTangents.Length >= meshLODs.Length && meshTangents[lod].IsCreated)
                {
                    array = meshTangents[lod];
                    return true;
                }

                array = new NativeArray<Vector4>(mesh.tangents, Allocator.Persistent).Reinterpret<float4>();
                if (meshTangents == null || meshTangents.Length != meshLODs.Length)
                {
                    if (meshTangents != null)
                    {
                        foreach (var array_ in meshTangents) if (array_.IsCreated) array_.Dispose();
                    }

                    meshTangents = new NativeArray<float4>[meshLODs.Length];
                }

                meshTangents[lod] = array;

                return true;
            }
            public bool TryGetColors(int lod, out NativeArray<float4> array)
            {
                array = default;
                if (meshLODs == null || lod < 0 || lod >= meshLODs.Length) return false;

                var mesh = GetMeshUnsafe(lod);
                if (mesh == null) return false;

                if (meshColors != null && meshColors.Length >= meshLODs.Length && meshColors[lod].IsCreated)
                {
                    array = meshColors[lod];
                    return true;
                }

                //MeshUtils._tempColor.Clear();
                //mesh.GetColors(MeshUtils._tempColor);
                array = new NativeArray<Color>(mesh.colors, Allocator.Persistent).Reinterpret<float4>();
                if (meshColors == null || meshColors.Length != meshLODs.Length)
                {
                    if (meshColors != null)
                    {
                        foreach (var array_ in meshColors) if (array_.IsCreated) array_.Dispose();
                    }

                    meshColors = new NativeArray<float4>[meshLODs.Length];
                }

                meshColors[lod] = array;

                return true;
            }
            public bool TryGetTriangles(int lod, out NativeArray<int> array)
            {
                array = default;
                if (meshLODs == null || lod < 0 || lod >= meshLODs.Length) return false;

                var mesh = GetMeshUnsafe(lod);
                if (mesh == null) return false;

                if (meshTriangles != null && meshTriangles.Length >= meshLODs.Length && meshTriangles[lod].IsCreated)
                {
                    array = meshTriangles[lod];
                    return true;
                }

                array = new NativeArray<int>(mesh.triangles, Allocator.Persistent);
                if (meshTriangles == null || meshTriangles.Length != meshLODs.Length)
                {
                    if (meshTriangles != null)
                    {
                        foreach (var array_ in meshTriangles) if (array_.IsCreated) array_.Dispose();
                    }

                    meshTriangles = new NativeArray<int>[meshLODs.Length];
                }

                meshTriangles[lod] = array;

                return true;
            }

            public bool TryGetBoneWeights(int lod, out NativeArray<BoneWeight8> array)
            {
                array = default;
                if (meshLODs == null || lod < 0 || lod >= meshLODs.Length) return false;

                var mesh = GetMeshUnsafe(lod);
                if (mesh == null) return false;

                if (meshBoneWeights != null && meshBoneWeights.Length >= meshLODs.Length && meshBoneWeights[lod].IsCreated)
                {
                    array = meshBoneWeights[lod];
                    return true;
                }

                array = lod == 0 ? new NativeArray<BoneWeight8>(baseBoneWeights, Allocator.Persistent) : new NativeArray<BoneWeight8>(mesh.vertexCount, Allocator.Persistent);
                if (meshBoneWeights == null || meshBoneWeights.Length != meshLODs.Length)
                {
                    if (meshBoneWeights != null)
                    {
                        foreach (var array_ in meshBoneWeights) if (array_.IsCreated) array_.Dispose();
                    }

                    meshBoneWeights = new NativeArray<BoneWeight8>[meshLODs.Length];
                }

                if (lod > 0)
                {
                    if (TryGetUV(lod, nearestVertexUVChannel, out var uvArray))
                    {
                        for (int i = 0; i < array.Length; i++)
                        {
                            int nearestIndex = MorphUtils.FetchIndexFromUV(nearestVertexIndexElement, uvArray[i]);
                            array[i] = baseBoneWeights[nearestIndex];
                        }
                    }
                }
                meshBoneWeights[lod] = array;

                return true;
            }

            public bool TryGetUV0(int lod, out NativeArray<float4> array)
            {
                array = default;
                if (meshLODs == null || lod < 0 || lod >= meshLODs.Length) return false;

                var mesh = GetMeshUnsafe(lod);
                if (mesh == null) return false;

                if (meshUV0s != null && meshUV0s.Length >= meshLODs.Length && meshUV0s[lod].IsCreated)
                {
                    array = meshUV0s[lod];
                    return true;
                }

                MeshUtils._tempV4.Clear();
                mesh.GetUVs(0, MeshUtils._tempV4);
                array = new NativeArray<Vector4>(MeshUtils._tempV4.ToArray(), Allocator.Persistent).Reinterpret<float4>();
                if (meshUV0s == null || meshUV0s.Length != meshLODs.Length)
                {
                    if (meshUV0s != null)
                    {
                        foreach (var array_ in meshUV0s) if (array_.IsCreated) array_.Dispose();
                    }

                    meshUV0s = new NativeArray<float4>[meshLODs.Length];
                }

                meshUV0s[lod] = array;

                return true;
            }
            public bool TryGetUV1(int lod, out NativeArray<float4> array)
            {
                array = default;
                if (meshLODs == null || lod < 0 || lod >= meshLODs.Length) return false;

                var mesh = GetMeshUnsafe(lod);
                if (mesh == null) return false;

                if (meshUV1s != null && meshUV1s.Length >= meshLODs.Length && meshUV1s[lod].IsCreated)
                {
                    array = meshUV1s[lod];
                    return true;
                }

                MeshUtils._tempV4.Clear();
                mesh.GetUVs(1, MeshUtils._tempV4);
                array = new NativeArray<Vector4>(MeshUtils._tempV4.ToArray(), Allocator.Persistent).Reinterpret<float4>();
                if (meshUV1s == null || meshUV1s.Length != meshLODs.Length)
                {
                    if (meshUV1s != null)
                    {
                        foreach (var array_ in meshUV1s) if (array_.IsCreated) array_.Dispose();
                    }

                    meshUV1s = new NativeArray<float4>[meshLODs.Length];
                }

                meshUV1s[lod] = array;

                return true;
            }
            public bool TryGetUV2(int lod, out NativeArray<float4> array)
            {
                array = default;
                if (meshLODs == null || lod < 0 || lod >= meshLODs.Length) return false;

                var mesh = GetMeshUnsafe(lod);
                if (mesh == null) return false;

                if (meshUV2s != null && meshUV2s.Length >= meshLODs.Length && meshUV2s[lod].IsCreated)
                {
                    array = meshUV2s[lod];
                    return true;
                }

                MeshUtils._tempV4.Clear();
                mesh.GetUVs(2, MeshUtils._tempV4);
                array = new NativeArray<Vector4>(MeshUtils._tempV4.ToArray(), Allocator.Persistent).Reinterpret<float4>();
                if (meshUV2s == null || meshUV2s.Length != meshLODs.Length)
                {
                    if (meshUV2s != null)
                    {
                        foreach (var array_ in meshUV2s) if (array_.IsCreated) array_.Dispose();
                    }

                    meshUV2s = new NativeArray<float4>[meshLODs.Length];
                }

                meshUV2s[lod] = array;

                return true;
            }
            public bool TryGetUV3(int lod, out NativeArray<float4> array)
            {
                array = default;
                if (meshLODs == null || lod < 0 || lod >= meshLODs.Length) return false;

                var mesh = GetMeshUnsafe(lod);
                if (mesh == null) return false;

                if (meshUV3s != null && meshUV3s.Length >= meshLODs.Length && meshUV3s[lod].IsCreated)
                {
                    array = meshUV3s[lod];
                    return true;
                }

                MeshUtils._tempV4.Clear();
                mesh.GetUVs(3, MeshUtils._tempV4);
                array = new NativeArray<Vector4>(MeshUtils._tempV4.ToArray(), Allocator.Persistent).Reinterpret<float4>();
                if (meshUV3s == null || meshUV3s.Length != meshLODs.Length)
                {
                    if (meshUV3s != null)
                    {
                        foreach (var array_ in meshUV3s) if (array_.IsCreated) array_.Dispose();
                    }

                    meshUV3s = new NativeArray<float4>[meshLODs.Length];
                }

                meshUV3s[lod] = array;

                return true;
            }
            public bool TryGetUV(int lod, UVChannelURP channel, out NativeArray<float4> array)
            {
                array = default;

                switch (channel)
                {
                    case UVChannelURP.UV0:
                        return TryGetUV0(lod, out array);

                    case UVChannelURP.UV1:
                        return TryGetUV1(lod, out array);

                    case UVChannelURP.UV2:
                        return TryGetUV2(lod, out array);

                    case UVChannelURP.UV3:
                        return TryGetUV3(lod, out array);
                }

                return false;
            }

            [SerializeField]
            public Vector3 boundsCenter;
            public Vector3 BoundsCenter => boundsCenter;

            [SerializeField]
            public Vector3 boundsExtents;
            public Vector3 BoundsExtents => boundsExtents;

            [HideInInspector]
            public bool[] leftRightFlags;
            public bool HasLeftRightFlags => leftRightFlags != null && leftRightFlags.Length == vertexCount;
            public bool GetLeftRightFlag(int vertexIndex) => HasLeftRightFlags && vertexIndex >= 0 && vertexIndex < leftRightFlags.Length ? leftRightFlags[vertexIndex] : false;
            public bool[] LeftRightFlags => leftRightFlags;

            [HideInInspector]
            public BoneWeight8[] baseBoneWeights;
            public bool HasBaseBoneWeights => baseBoneWeights != null && baseBoneWeights.Length == vertexCount;
            public BoneWeight8 GetBaseBoneWeight(int vertexIndex) => HasBaseBoneWeights && vertexIndex >= 0 && vertexIndex < baseBoneWeights.Length ? baseBoneWeights[vertexIndex] : default;
            public BoneWeight8[] BaseBoneWeights => baseBoneWeights;

            [NonSerialized]
            private NativeArray<BoneWeight8> baseBoneWeightsJob;
            public NativeArray<BoneWeight8> BaseBoneWeightsJob
            {
                get
                {
                    if (!baseBoneWeightsJob.IsCreated)
                    {
                        baseBoneWeightsJob = new NativeArray<BoneWeight8>(baseBoneWeights == null ? new BoneWeight8[0] : baseBoneWeights, Allocator.Persistent);
                    }

                    return baseBoneWeightsJob;
                }
            }

            public string[] boneNames;
            public bool HasBonesArray => boneNames != null && boneNames.Length > 0;
            public string[] BoneNames => boneNames;

            [HideInInspector]
            public Matrix4x4[] baseBindPose;
            public bool HasManagedBindPose => baseBindPose != null && baseBindPose.Length > 0;
            public Matrix4x4[] ManagedBindPose => baseBindPose;

            public int BoneCount => HasBonesArray ? boneNames.Length : (baseBindPose == null ? 0 : baseBindPose.Length);

            [Header("Shapes")]
            public Vector2Int standaloneShapes;
            public Vector2Int StandaloneShapes => standaloneShapes;

            public Vector2Int variationShapes;
            public Vector2Int VariationShapes => variationShapes;

            public int massShape;
            public int MassShape => massShape;

            public int flexShape;
            public int FlexShape => flexShape;

            public int fatShape;
            public int FatShape => fatShape;

            public int fatMuscleBlendShape;
            public int FatMuscleBlendShape => fatMuscleBlendShape;
            public Vector2 fatMuscleBlendWeightRange;
            public Vector2 FatMuscleBlendWeightRange => fatMuscleBlendWeightRange;

            public int bustSizeShape;
            public int BustSizeShape => bustSizeShape;
            public int bustShapeShape;
            public int BustShapeShape => bustShapeShape;
            public int bustSizeMuscleShape;
            public int BustSizeMuscleShape => bustSizeMuscleShape;

            [Header("Vertex Groups")]
            public int midlineVertexGroup;
            public int MidlineVertexGroup => midlineVertexGroup;
            public int bustVertexGroup;
            public int BustVertexGroup => bustVertexGroup;
            public int bustNerfVertexGroup;
            public int BustNerfVertexGroup => bustNerfVertexGroup;

            public int nippleMaskVertexGroup;
            public int NippleMaskVertexGroup => nippleMaskVertexGroup;
            public int genitalMaskVertexGroup;
            public int GenitalMaskVertexGroup => genitalMaskVertexGroup;

            public float defaultMassShapeWeight;
            public float DefaultMassShapeWeight => defaultMassShapeWeight;
            public float minMassShapeWeight;
            public float MinMassShapeWeight => minMassShapeWeight;

            public Vector2Int standaloneGroups;
            public Vector2Int StandaloneGroups => standaloneGroups;

            public Vector2Int variationGroups;
            public Vector2Int VariationGroups => variationGroups;

            public Vector2Int muscleGroups;
            public Vector2Int MuscleGroups => muscleGroups;

            public Vector2Int fatGroups;
            public Vector2Int FatGroups => fatGroups;

            [Header("Material Properties")]
            public string vertexCountPropertyNameOverride;
            public string VertexCountPropertyName => string.IsNullOrWhiteSpace(vertexCountPropertyNameOverride) ? _vertexCountDefaultPropertyName : vertexCountPropertyNameOverride;

            public string skinningDataPropertyNameOverride;
            public string SkinningDataPropertyName => string.IsNullOrWhiteSpace(skinningDataPropertyNameOverride) ? _skinningDataDefaultPropertyName : skinningDataPropertyNameOverride;

            public string boneCountPropertyNameOverride;
            public string BoneCountPropertyName => string.IsNullOrWhiteSpace(boneCountPropertyNameOverride) ? _boneCountDefaultPropertyName : boneCountPropertyNameOverride;

            public string skinningMatricesPropertyNameOverride;
            public string SkinningMatricesPropertyName => string.IsNullOrWhiteSpace(skinningMatricesPropertyNameOverride) ? _skinningMatricesDefaultPropertyName : skinningMatricesPropertyNameOverride;

            public string standaloneVertexGroupsBufferRangePropertyNameOverride;
            public string StandaloneVertexGroupsBufferRangePropertyName => string.IsNullOrWhiteSpace(standaloneVertexGroupsBufferRangePropertyNameOverride) ? _standaloneVertexGroupsBufferRangeDefaultPropertyName : standaloneVertexGroupsBufferRangePropertyNameOverride;

            public string muscleVertexGroupsBufferRangePropertyNameOverride;
            public string fatVertexGroupsBufferRangePropertyNameOverride;
            public string variationVertexGroupsBufferRangePropertyNameOverride;

            public string midlineVertexGroupIndexPropertyNameOverride;
            public string MidlineVertexGroupIndexPropertyName => string.IsNullOrWhiteSpace(midlineVertexGroupIndexPropertyNameOverride) ? _midlineVertexGroupIndexDefaultPropertyName : midlineVertexGroupIndexPropertyNameOverride;

            public string bustMixPropertyNameOverride;
            public string BustMixPropertyName => string.IsNullOrWhiteSpace(bustMixPropertyNameOverride) ? _bustMixDefaultPropertyName : bustMixPropertyNameOverride;

            public string hideNipplesPropertyNameOverride;
            public string HideNipplesPropertyName => string.IsNullOrWhiteSpace(hideNipplesPropertyNameOverride) ? _hideNipplesDefaultPropertyName : hideNipplesPropertyNameOverride;

            public string hideGenitalsPropertyNameOverride;
            public string HideGenitalsPropertyName => string.IsNullOrWhiteSpace(hideGenitalsPropertyNameOverride) ? _hideGenitalsDefaultPropertyName : hideGenitalsPropertyNameOverride;

            public string bustVertexGroupIndexPropertyNameOverride;
            public string BustVertexGroupIndexPropertyName => string.IsNullOrWhiteSpace(bustVertexGroupIndexPropertyNameOverride) ? _bustVertexGroupIndexDefaultPropertyName : bustVertexGroupIndexPropertyNameOverride;

            public string bustNerfVertexGroupIndexPropertyNameOverride;
            public string BustNerfVertexGroupIndexPropertyName => string.IsNullOrWhiteSpace(bustNerfVertexGroupIndexPropertyNameOverride) ? _bustNerfVertexGroupIndexDefaultPropertyName : bustNerfVertexGroupIndexPropertyNameOverride;

            public string nippleMaskVertexGroupIndexPropertyNameOverride;
            public string NippleMaskVertexGroupIndexPropertyName => string.IsNullOrWhiteSpace(nippleMaskVertexGroupIndexPropertyNameOverride) ? _nippleMaskVertexGroupIndexDefaultPropertyName : nippleMaskVertexGroupIndexPropertyNameOverride;

            public string genitalMaskVertexGroupIndexPropertyNameOverride;
            public string GenitalMaskVertexGroupIndexPropertyName => string.IsNullOrWhiteSpace(genitalMaskVertexGroupIndexPropertyNameOverride) ? _genitalMaskVertexGroupIndexDefaultPropertyName : genitalMaskVertexGroupIndexPropertyNameOverride;


            public string bustSizeShapeIndexPropertyNameOverride;
            public string BustSizeShapeIndexPropertyName => string.IsNullOrWhiteSpace(bustSizeShapeIndexPropertyNameOverride) ? _bustSizeShapeIndexDefaultPropertyName : bustSizeShapeIndexPropertyNameOverride;

            public string bustSizeMuscularShapeIndexPropertyNameOverride;
            public string BustSizeMuscularShapeIndexPropertyName => string.IsNullOrWhiteSpace(bustSizeMuscularShapeIndexPropertyNameOverride) ? _bustSizeMuscularShapeIndexDefaultPropertyName : bustSizeMuscularShapeIndexPropertyNameOverride;


            public string fatMuscleBlendShapeIndexPropertyNameOverride;
            public string FatMuscleBlendShapeIndexPropertyName => string.IsNullOrWhiteSpace(fatMuscleBlendShapeIndexPropertyNameOverride) ? _fatMuscleBlendShapeIndexDefaultPropertyName : fatMuscleBlendShapeIndexPropertyNameOverride;

            public string fatMuscleBlendWeightRangePropertyNameOverride;
            public string FatMuscleBlendWeightRangePropertyName => string.IsNullOrWhiteSpace(fatMuscleBlendWeightRangePropertyNameOverride) ? _fatMuscleBlendWeightRangeDefaultPropertyName : fatMuscleBlendWeightRangePropertyNameOverride;

            public string defaultShapeMuscleWeightPropertyNameOverride;
            public string DefaultShapeMuscleWeightPropertyName => string.IsNullOrWhiteSpace(defaultShapeMuscleWeightPropertyNameOverride) ? _defaultShapeMuscleWeightDefaultPropertyName : defaultShapeMuscleWeightPropertyNameOverride;

            public string standaloneShapesControlPropertyNameOverride;
            public string StandaloneShapesControlPropertyName => string.IsNullOrWhiteSpace(standaloneShapesControlPropertyNameOverride) ? _standaloneShapesControlDefaultPropertyName : standaloneShapesControlPropertyNameOverride;

            public string muscleGroupsControlPropertyNameOverride;
            public string MuscleGroupsControlPropertyName => string.IsNullOrWhiteSpace(muscleGroupsControlPropertyNameOverride) ? _muscleGroupsControlDefaultPropertyName : muscleGroupsControlPropertyNameOverride;

            public string fatGroupsControlPropertyNameOverride;
            public string FatGroupsControlPropertyName => string.IsNullOrWhiteSpace(fatGroupsControlPropertyNameOverride) ? _fatGroupsControlDefaultPropertyName : fatGroupsControlPropertyNameOverride;

            public string variationShapesControlPropertyNameOverride;
            public string VariationShapesControlPropertyName => string.IsNullOrWhiteSpace(variationShapesControlPropertyNameOverride) ? _variationShapesControlDefaultPropertyName : variationShapesControlPropertyNameOverride;


            public string muscleMassShapeIndexPropertyNameOverride;
            public string MuscleMassShapeIndexPropertyName => string.IsNullOrWhiteSpace(muscleMassShapeIndexPropertyNameOverride) ? _muscleMassShapeIndexDefaultPropertyName : muscleMassShapeIndexPropertyNameOverride;

            public string flexShapeIndexPropertyNameOverride;
            public string FlexShapeIndexPropertyName => string.IsNullOrWhiteSpace(flexShapeIndexPropertyNameOverride) ? _flexShapeIndexDefaultPropertyName : flexShapeIndexPropertyNameOverride;

            public string fatShapeIndexPropertyNameOverride;
            public string FatShapeIndexPropertyName => string.IsNullOrWhiteSpace(fatShapeIndexPropertyNameOverride) ? _fatShapeIndexDefaultPropertyName : fatShapeIndexPropertyNameOverride;


            public string vertexGroupsPropertyNameOverride;
            public string VertexGroupsPropertyName => string.IsNullOrWhiteSpace(vertexGroupsPropertyNameOverride) ? _vertexGroupsDefaultPropertyName : vertexGroupsPropertyNameOverride;

            public string meshShapeFrameDeltasPropertyNameOverride;
            public string MeshShapeFrameDeltasPropertyName => string.IsNullOrWhiteSpace(meshShapeFrameDeltasPropertyNameOverride) ? _meshShapeFrameDeltasDefaultPropertyName : meshShapeFrameDeltasPropertyNameOverride;

            public string meshShapeFrameWeightsPropertyNameOverride;
            public string MeshShapeFrameWeightsPropertyName => string.IsNullOrWhiteSpace(meshShapeFrameWeightsPropertyNameOverride) ? _meshShapeFrameWeightsDefaultPropertyName : meshShapeFrameWeightsPropertyNameOverride;

            public string meshShapeIndicesPropertyNameOverride;
            public string MeshShapeIndicesPropertyName => string.IsNullOrWhiteSpace(meshShapeIndicesPropertyNameOverride) ? _meshShapeIndicesDefaultPropertyName : meshShapeIndicesPropertyNameOverride;


            public string muscleGroupInfluencesPropertyNameOverride;
            public string MuscleGroupInfluencesPropertyName => string.IsNullOrWhiteSpace(muscleGroupInfluencesPropertyNameOverride) ? _muscleGroupInfluencesDefaultPropertyName : muscleGroupInfluencesPropertyNameOverride;

            public string fatGroupInfluencesPropertyNameOverride;
            public string FatGroupInfluencesPropertyName => string.IsNullOrWhiteSpace(fatGroupInfluencesPropertyNameOverride) ? _fatGroupInfluencesDefaultPropertyName : fatGroupInfluencesPropertyNameOverride;

            public string perVertexDeltaDataPropertyNameOverride;
            public string PerVertexDeltaDataPropertyName => string.IsNullOrWhiteSpace(perVertexDeltaDataPropertyNameOverride) ? _perVertexDeltaDataDefaultPropertyName : perVertexDeltaDataPropertyNameOverride;


            public string localInstanceIDPropertyNameOverride;
            public string LocalInstanceIDPropertyName => string.IsNullOrWhiteSpace(localInstanceIDPropertyNameOverride) ? _localInstanceIDPropertyName : localInstanceIDPropertyNameOverride;

            public string shapesInstanceIDPropertyNameOverride;
            public string ShapesInstanceIDPropertyName => string.IsNullOrWhiteSpace(shapesInstanceIDPropertyNameOverride) ? _shapesInstanceIDPropertyName : shapesInstanceIDPropertyNameOverride;

            public string rigInstanceIDPropertyNameOverride;
            public string RigInstanceIDPropertyName => string.IsNullOrWhiteSpace(rigInstanceIDPropertyNameOverride) ? InstancedSkinnedMeshData._rigInstanceIDPropertyName : rigInstanceIDPropertyNameOverride;

            public string characterInstanceIDPropertyNameOverride;
            public string CharacterInstanceIDPropertyName => string.IsNullOrWhiteSpace(characterInstanceIDPropertyNameOverride) ? _characterInstanceIDPropertyName : characterInstanceIDPropertyNameOverride;



            public string vertexColorDeltasPropertyNameOverride;
            public string VertexColorDeltasPropertyName => string.IsNullOrWhiteSpace(vertexColorDeltasPropertyNameOverride) ? _vertexColorDeltasDefaultPropertyName : vertexColorDeltasPropertyNameOverride;


            public string minMassShapeWeightPropertyNameOverride;
            public string MinMassShapeWeightPropertyName => string.IsNullOrWhiteSpace(minMassShapeWeightPropertyNameOverride) ? _minMassShapeWeightDefaultPropertyName : minMassShapeWeightPropertyNameOverride;


            public string flexEndPointWeightPropertyNameOverride;
            public string FlexEndPointWeightPropertyName => string.IsNullOrWhiteSpace(flexEndPointWeightPropertyNameOverride) ? _flexEndPointWeightDefaultPropertyName : flexEndPointWeightPropertyNameOverride;

            public string flexExponentPropertyNameOverride;
            public string FlexExponentPropertyName => string.IsNullOrWhiteSpace(flexExponentPropertyNameOverride) ? _flexExponentDefaultPropertyName : flexExponentPropertyNameOverride;

            public string flexNerfThresholdPropertyNameOverride;
            public string FlexNerfThresholdPropertyName => string.IsNullOrWhiteSpace(flexNerfThresholdPropertyNameOverride) ? _flexNerfThresholdDefaultPropertyName : flexNerfThresholdPropertyNameOverride;

            public string flexNerfExponentPropertyNameOverride;
            public string FlexNerfExponentPropertyName => string.IsNullOrWhiteSpace(flexNerfExponentPropertyNameOverride) ? _flexNerfExponentDefaultPropertyName : flexNerfExponentPropertyNameOverride;


            [Header("Other")]
            public float flexEndPointWeight;
            public float FlexEndPointWeight => flexEndPointWeight;

            public float flexExponent;
            public float FlexExponent => flexExponent;

            public float flexNerfThreshold = 0.35f;
            public float FlexNerfThreshold => flexNerfThreshold;

            public float flexNerfExponent = 1f;
            public float FlexNerfExponent => flexNerfExponent;

            public int raycastLod;
            public int RaycastLOD => raycastLod;

            [Tooltip("The uv channel to use for determining the nearest vertex.")]
            public UVChannelURP nearestVertexUVChannel = UVChannelURP.UV3;
            public UVChannelURP NearestVertexUVChannel => nearestVertexUVChannel;

            [Tooltip("The uv element to store the nearest vertex index in.")]
            public RGBAChannel nearestVertexIndexElement = RGBAChannel.R;
            public RGBAChannel NearestVertexIndexElement => nearestVertexIndexElement;

            public float2[] fatGroupModifiers;

            #endregion

            #region Interface

            public ICustomizableCharacter.DefaultMuscleGroupConversion[] defaultMuscleGroupConversions;
            private Dictionary<MuscleGroupsDefault, int> defaultMuscleGroupConversionsCache;
            private Dictionary<string, MuscleGroupsDefault> defaultMuscleGroupConversionsReverseCache;
            private Dictionary<MuscleGroup, int> defaultBaseMuscleGroupConversionsCache;
            private Dictionary<string, MuscleGroup> defaultBaseMuscleGroupConversionsReverseCache;

            [NonSerialized]
            private bool initializedDefaultMuscleGroupConversions = false;

            private void InitializeDefaultMuscleGroupConversions()
            {
                if (initializedDefaultMuscleGroupConversions) return;

                initializedDefaultMuscleGroupConversions = true;
                defaultMuscleGroupConversionsCache = new Dictionary<MuscleGroupsDefault, int>();
                defaultMuscleGroupConversionsReverseCache = new Dictionary<string, MuscleGroupsDefault>();
                defaultBaseMuscleGroupConversionsCache = new Dictionary<MuscleGroup, int>();
                defaultBaseMuscleGroupConversionsReverseCache = new Dictionary<string, MuscleGroup>();

                if (defaultMuscleGroupConversions != null)
                {
                    for (int a = 0; a < defaultMuscleGroupConversions.Length; a++)
                    {
                        var conversion = defaultMuscleGroupConversions[a];
                        if (conversion == null) continue;

                        int ind = IndexOfMuscleGroup(conversion.muscleGroupName, true);
                        if (ind < 0)
                        {
                            Debug.LogError($"Muscle group '{conversion.muscleGroupName}' not found for conversion to {conversion.basicMuscleGroup}");
                            continue;
                        }

                        var vertexGroup = GetMuscleVertexGroupInfo(ind);

                        defaultBaseMuscleGroupConversionsCache[conversion.basicMuscleGroup] = ind;
                        defaultBaseMuscleGroupConversionsReverseCache[vertexGroup.name] = conversion.basicMuscleGroup;

                        MuscleGroupsDefault mgSide;

                        ind = ind * 2;
                        mgSide = conversion.basicMuscleGroup.GetMuscleGroupSide(Side.Left);
                        defaultMuscleGroupConversionsCache[mgSide] = ind;
                        defaultMuscleGroupConversionsReverseCache[vertexGroup.name + Side.Left.AsSuffix()] = mgSide;

                        var prevSide = mgSide;
                        ind = ind + 1;
                        mgSide = conversion.basicMuscleGroup.GetMuscleGroupSide(Side.Right);
                        if (prevSide == mgSide) ind = (ind - 1) + _dualMuscleGroupIndexOffset;
                        defaultMuscleGroupConversionsCache[mgSide] = ind;
                        defaultMuscleGroupConversionsReverseCache[vertexGroup.name + Side.Right.AsSuffix()] = mgSide;
                    }
                }
            }

            public string ConvertDefaultMuscleGroupName(MuscleGroupsDefault defaultGroup)
            {
                if (!initializedDefaultMuscleGroupConversions) InitializeDefaultMuscleGroupConversions();
                if (defaultMuscleGroupConversionsCache.TryGetValue(defaultGroup, out int groupIndex)) return GetMuscleVertexGroupInfo(groupIndex).name;
                return defaultGroup.ToString();
            }
            public int ConvertDefaultMuscleGroupToIndex(MuscleGroupsDefault defaultGroup)
            {
                if (!initializedDefaultMuscleGroupConversions) InitializeDefaultMuscleGroupConversions();
                if (defaultMuscleGroupConversionsCache.TryGetValue(defaultGroup, out int groupIndex)) return groupIndex;
                return -1;
            }
            public MuscleGroupsDefault ConvertLocalMuscleGroupToDefault(string muscleGroupName)
            {
                if (!initializedDefaultMuscleGroupConversions) InitializeDefaultMuscleGroupConversions();
                if (defaultMuscleGroupConversionsReverseCache.TryGetValue(muscleGroupName, out var defaultGroup)) return defaultGroup;

                return default;
            }
            public MuscleGroupsDefault ConvertLocalMuscleGroupToDefault(int muscleGroupIndex)
            {
                if (muscleGroupIndex < 0 || muscleGroupIndex >= MuscleGroupsCount) return default;

                if (!initializedDefaultMuscleGroupConversions) InitializeDefaultMuscleGroupConversions();

                var vg = GetMuscleVertexGroupInfo(muscleGroupIndex);
                if (vg.IsInvalid) return default;

                if (defaultMuscleGroupConversionsReverseCache.TryGetValue(vg.name + Side.Left.AsSuffix(), out var defaultGroup)) return defaultGroup;

                return default;
            }
            public MuscleGroupsDefault ConvertMuscleGroupIndexToDefault(int muscleGroupIndex)
            {
                if (muscleGroupIndex < 0) return default;

                if (!initializedDefaultMuscleGroupConversions) InitializeDefaultMuscleGroupConversions();

                muscleGroupIndex = ConvertDefaultMuscleGroupIndexToLocal(muscleGroupIndex, out int defaultIndex, out bool isBothSides);

                var vg = GetMuscleVertexGroupInfo(muscleGroupIndex);
                if (vg.IsInvalid) return default;

                bool isLeft = isBothSides || defaultIndex % 2 == 0;

                if (defaultMuscleGroupConversionsReverseCache.TryGetValue(vg.name + (isLeft ? Side.Left : Side.Right).AsSuffix(), out var defaultGroup)) return defaultGroup;

                return default;
            }

            public virtual List<BoneWeight8Float> GetConvertedBoneWeightData(List<BoneWeight8Float> outputList = null)
            {
                if (outputList == null) outputList = new List<BoneWeight8Float>();

                if (baseBoneWeights != null)
                {
                    if (outputList.Capacity < baseBoneWeights.Length) outputList.Capacity = baseBoneWeights.Length;
                    foreach (var boneWeight in baseBoneWeights) outputList.Add(boneWeight);
                }

                return outputList;
            }
            protected static readonly List<BoneWeight8Float> tempBoneWeights = new List<BoneWeight8Float>();
            [NonSerialized]
            protected ComputeBuffer boneWeightsBuffer;
            public virtual ComputeBuffer BoneWeightsBuffer
            {
                get
                {
                    if (boneWeightsBuffer == null)
                    {
                        tempBoneWeights.Clear();

                        GetConvertedBoneWeightData(tempBoneWeights);

                        boneWeightsBuffer = new ComputeBuffer(tempBoneWeights.Count, UnsafeUtility.SizeOf(typeof(BoneWeight8Float)), ComputeBufferType.Structured, ComputeBufferMode.Immutable);
                        if (tempBoneWeights.Count > 0) boneWeightsBuffer.SetData(tempBoneWeights);

                        tempBoneWeights.Clear();

                        TrackDisposables();
                    }

                    return boneWeightsBuffer;
                }
            }

            protected static readonly List<BoneWeight1> tempWeights = new List<BoneWeight1>();
            protected static int CompareWeight(BoneWeight1 weight1, BoneWeight1 weight2) => (int)Mathf.Sign(weight2.weight - weight1.weight);
            [NonSerialized]
            protected ComputeBuffer muscleGroupInfluencesBuffer;
            public virtual ComputeBuffer MuscleGroupInfluencesBuffer
            {
                get
                {
                    return muscleGroupInfluencesBuffer;
                }
            }
            [NonSerialized]
            protected ComputeBuffer fatGroupInfluencesBuffer;
            public virtual ComputeBuffer FatGroupInfluencesBuffer
            {
                get
                {
                    return fatGroupInfluencesBuffer;
                }
            }

            public abstract int MeshShapeCount { get; }
            public abstract int MeshShapeDeltasCount { get; }
            public abstract ShapeInfo GetShapeInfo(int index);
            public abstract ShapeInfo GetShapeInfoUnsafe(int index);
            public abstract int IndexOfShape(string shapeName, bool caseSensitive = false);
            public abstract List<ShapeInfo> GetShapeInfos(List<ShapeInfo> outputList = null);

            public abstract int VertexGroupCount { get; }
            public abstract VertexGroupInfo GetVertexGroupInfo(int index);
            public abstract VertexGroupInfo GetVertexGroupInfoUnsafe(int index);
            public abstract int IndexOfVertexGroup(string vertexGroupName, bool caseSensitive = false);
            public abstract List<VertexGroupInfo> GetVertexGroupInfos(List<VertexGroupInfo> outputList = null);

            public string VertexGroupsBufferRangePropertyName => string.IsNullOrWhiteSpace(standaloneVertexGroupsBufferRangePropertyNameOverride) ? _vertexGroupsBufferRangeDefaultPropertyName : standaloneVertexGroupsBufferRangePropertyNameOverride;
            public int StandaloneGroupsCount => standaloneGroups.y < standaloneGroups.x ? 0 : ((standaloneGroups.y - standaloneGroups.x) + 1);
            public int StandaloneVertexGroupCount => StandaloneGroupsCount;
            public abstract int IndexOfStandaloneVertexGroup(string name, bool caseSensitive = false);
            public abstract VertexGroupInfo GetStandaloneVertexGroupInfo(int index);

            public string MuscleVertexGroupsBufferRangePropertyName => string.IsNullOrWhiteSpace(muscleVertexGroupsBufferRangePropertyNameOverride) ? _muscleVertexGroupsBufferRangeDefaultPropertyName : muscleVertexGroupsBufferRangePropertyNameOverride;
            public int MuscleGroupsCount => muscleGroups.y < muscleGroups.x ? 0 : ((muscleGroups.y - muscleGroups.x) + 1);
            public int MuscleVertexGroupCount => MuscleGroupsCount;
            public abstract int IndexOfMuscleGroup(string name, bool caseSensitive = false);
            public abstract VertexGroupInfo GetMuscleVertexGroupInfo(int index);

            public string FatVertexGroupsBufferRangePropertyName => string.IsNullOrWhiteSpace(fatVertexGroupsBufferRangePropertyNameOverride) ? _fatVertexGroupsBufferRangeDefaultPropertyName : fatVertexGroupsBufferRangePropertyNameOverride;
            public int FatGroupsCount => fatGroups.y < fatGroups.x ? 0 : ((fatGroups.y - fatGroups.x) + 1);
            public int FatVertexGroupCount => FatGroupsCount;
            public abstract int IndexOfFatGroup(string name, bool caseSensitive = false);
            public abstract VertexGroupInfo GetFatVertexGroupInfo(int index);

            public static float2 DefaultFatGroupModifier => new float2(1, 0);
            /// <summary>
            /// modifier.x is how much to nerf muscle mass by based on fat level
            public float2 GetFatGroupModifier(int index)
            {
                if (index < 0 || fatGroupModifiers == null || index >= fatGroupModifiers.Length) return DefaultFatGroupModifier;
                return fatGroupModifiers[index];
            }
            public bool HasFatGroupModifiers => fatGroupModifiers != null && fatGroupModifiers.Length > 0;

            public string VariationVertexGroupsBufferRangePropertyName => string.IsNullOrWhiteSpace(variationVertexGroupsBufferRangePropertyNameOverride) ? _variationVertexGroupsBufferRangeDefaultPropertyName : variationVertexGroupsBufferRangePropertyNameOverride;
            public int VariationGroupsCount => variationGroups.y < variationGroups.x ? 0 : ((variationGroups.y - variationGroups.x) + 1);
            public int VariationVertexGroupCount => VariationGroupsCount;

            public abstract int IndexOfVariationGroup(string name, bool caseSensitive = false);
            public abstract VertexGroupInfo GetVariationVertexGroupInfo(int index);
            public abstract VertexGroupInfo GetVariationGroupInfo(int index);

            public int VariationShapesControlDataSize => VariationShapesCount * VariationVertexGroupCount;


            public string standaloneShapesBufferRangePropertyNameOverride;
            public string StandaloneShapesBufferRangePropertyName => string.IsNullOrWhiteSpace(standaloneShapesBufferRangePropertyNameOverride) ? _standaloneShapesBufferRangeDefaultPropertyName : standaloneShapesBufferRangePropertyNameOverride;
            public int StandaloneShapesCount => standaloneShapes.y < standaloneShapes.x ? 0 : ((standaloneShapes.y - standaloneShapes.x) + 1);
            public abstract int IndexOfStandaloneShape(string name, bool caseSensitive = false);
            public abstract ShapeInfo GetStandaloneShapeInfo(int index);

            public abstract ShapeInfo MassShapeInfo { get; }
            public abstract int MassShapeFrameCount { get; }

            public abstract ShapeInfo FlexShapeInfo { get; }
            public abstract int FlexShapeFrameCount { get; }

            public abstract ShapeInfo FatShapeInfo { get; }
            public abstract int FatShapeFrameCount { get; }

            public abstract ShapeInfo FatMuscleBlendShapeInfo { get; }
            public abstract int FatMuscleBlendShapeFrameCount { get; }

            public abstract ShapeInfo BustSizeShapeInfo { get; }
            public abstract int BustSizeShapeFrameCount { get; }

            public abstract ShapeInfo BustSizeMuscleShapeInfo { get; }
            public abstract int BustSizeMuscleShapeFrameCount { get; }


            public string variationShapesBufferRangePropertyNameOverride;
            public string VariationShapesBufferRangePropertyName => string.IsNullOrWhiteSpace(variationShapesBufferRangePropertyNameOverride) ? _variationShapesBufferRangeDefaultPropertyName : variationShapesBufferRangePropertyNameOverride;
            public int VariationShapesCount => variationShapes.y < variationShapes.x ? 0 : ((variationShapes.y - variationShapes.x) + 1);
            public abstract int IndexOfVariationShape(string name, bool caseSensitive = false);
            public abstract ShapeInfo GetVariationShapeInfo(int index);


            protected static readonly List<float> tempFloats = new List<float>();
            [NonSerialized]
            protected ComputeBuffer vertexGroupsBuffer;
            public virtual ComputeBuffer VertexGroupsBuffer
            {
                get
                {
                    return vertexGroupsBuffer;
                }
            }

            protected static readonly List<MorphShapeVertex> tempFrameDeltas = new List<MorphShapeVertex>();
            [NonSerialized]
            protected ComputeBuffer meshShapeFrameDeltasBuffer;
            public virtual ComputeBuffer MeshShapeFrameDeltasBuffer
            {
                get
                {
                    return meshShapeFrameDeltasBuffer;
                }
            }

            public abstract ComputeBuffer MeshShapeFrameWeightsBuffer { get; }

            public abstract ComputeBuffer MeshShapeIndicesBuffer { get; }

            public virtual ComputeBuffer VertexColorDeltasBuffer
            {
                get
                {
                    return null;
                }
            }

            #endregion

            #region Disposal

            [NonSerialized]
            protected bool trackingDisposables;
            public void TrackDisposables()
            {
                if (trackingDisposables) return;

                if (!PersistentJobDataTracker.Track(this))
                {
                    Dispose();
                    return;
                }

                trackingDisposables = true;
            }

            public virtual void Dispose()
            {
                if (trackingDisposables)
                {
                    try
                    {
                        PersistentJobDataTracker.Untrack(this);
                    }
                    catch (Exception ex)
                    {
#if UNITY_EDITOR
                        Debug.LogError(ex);
#endif
                    }
                }
                trackingDisposables = false;

                try
                {
                    if (baseBoneWeightsJob.IsCreated)
                    {
                        baseBoneWeightsJob.Dispose();
                        baseBoneWeightsJob = default;
                    }
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR
                    Debug.LogException(ex);
#endif
                }
                try
                {
                    if (boneWeightsBuffer != null && boneWeightsBuffer.IsValid())
                    {
                        boneWeightsBuffer.Dispose();
                        boneWeightsBuffer = null;
                    }
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR
                    Debug.LogException(ex);
#endif
                }

                try
                {
                    if (muscleGroupInfluencesBuffer != null && muscleGroupInfluencesBuffer.IsValid())
                    {
                        muscleGroupInfluencesBuffer.Dispose();
                        muscleGroupInfluencesBuffer = null;
                    }
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR
                    Debug.LogException(ex);
#endif
                }

                try
                {
                    if (fatGroupInfluencesBuffer != null && fatGroupInfluencesBuffer.IsValid())
                    {
                        fatGroupInfluencesBuffer.Dispose();
                        fatGroupInfluencesBuffer = null;
                    }
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR
                    Debug.LogException(ex);
#endif
                }

                try
                {
                    if (vertexGroupsBuffer != null && vertexGroupsBuffer.IsValid())
                    {
                        vertexGroupsBuffer.Dispose();
                        vertexGroupsBuffer = null;
                    }
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR
                    Debug.LogException(ex);
#endif
                }

                try
                {
                    if (meshShapeFrameDeltasBuffer != null && meshShapeFrameDeltasBuffer.IsValid())
                    {
                        meshShapeFrameDeltasBuffer.Dispose();
                        meshShapeFrameDeltasBuffer = null;
                    }
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR
                    Debug.LogException(ex);
#endif
                }

                if (meshVertices != null)
                {
                    foreach (var array in meshVertices)
                    {
                        try
                        {
                            if (array.IsCreated)
                            {
                                array.Dispose();
                            }
                        }
                        catch (Exception ex)
                        {
#if UNITY_EDITOR
                            Debug.LogException(ex);
#endif
                        }
                    }

                    meshVertices = null;
                }
                if (meshNormals != null)
                {
                    foreach (var array in meshNormals)
                    {
                        try
                        {
                            if (array.IsCreated)
                            {
                                array.Dispose();
                            }
                        }
                        catch (Exception ex)
                        {
#if UNITY_EDITOR
                            Debug.LogException(ex);
#endif
                        }
                    }

                    meshNormals = null;
                }
                if (meshTangents != null)
                {
                    foreach (var array in meshTangents)
                    {
                        try
                        {
                            if (array.IsCreated)
                            {
                                array.Dispose();
                            }
                        }
                        catch (Exception ex)
                        {
#if UNITY_EDITOR
                            Debug.LogException(ex);
#endif
                        }
                    }

                    meshTangents = null;
                }

                if (meshColors != null)
                {
                    foreach (var array in meshColors)
                    {
                        try
                        {
                            if (array.IsCreated)
                            {
                                array.Dispose();
                            }
                        }
                        catch (Exception ex)
                        {
#if UNITY_EDITOR
                            Debug.LogException(ex);
#endif
                        }
                    }

                    meshColors = null;
                }

                if (meshTriangles != null)
                {
                    foreach (var array in meshTriangles)
                    {
                        try
                        {
                            if (array.IsCreated)
                            {
                                array.Dispose();
                            }
                        }
                        catch (Exception ex)
                        {
#if UNITY_EDITOR
                            Debug.LogException(ex);
#endif
                        }
                    }

                    meshTriangles = null;
                }

                if (meshBoneWeights != null)
                {
                    foreach (var array in meshBoneWeights)
                    {
                        try
                        {
                            if (array.IsCreated)
                            {
                                array.Dispose();
                            }
                        }
                        catch (Exception ex)
                        {
#if UNITY_EDITOR
                            Debug.LogException(ex);
#endif
                        }
                    }

                    meshBoneWeights = null;
                }

                if (meshUV0s != null)
                {
                    foreach (var array in meshUV0s)
                    {
                        try
                        {
                            if (array.IsCreated)
                            {
                                array.Dispose();
                            }
                        }
                        catch (Exception ex)
                        {
#if UNITY_EDITOR
                            Debug.LogException(ex);
#endif
                        }
                    }

                    meshUV0s = null;
                }
                if (meshUV1s != null)
                {
                    foreach (var array in meshUV1s)
                    {
                        try
                        {
                            if (array.IsCreated)
                            {
                                array.Dispose();
                            }
                        }
                        catch (Exception ex)
                        {
#if UNITY_EDITOR
                            Debug.LogException(ex);
#endif
                        }
                    }

                    meshUV1s = null;
                }
                if (meshUV2s != null)
                {
                    foreach (var array in meshUV2s)
                    {
                        try
                        {
                            if (array.IsCreated)
                            {
                                array.Dispose();
                            }
                        }
                        catch (Exception ex)
                        {
#if UNITY_EDITOR
                            Debug.LogException(ex);
#endif
                        }
                    }

                    meshUV2s = null;
                }
                if (meshUV3s != null)
                {
                    foreach (var array in meshUV3s)
                    {
                        try
                        {
                            if (array.IsCreated)
                            {
                                array.Dispose();
                            }
                        }
                        catch (Exception ex)
                        {
#if UNITY_EDITOR
                            Debug.LogException(ex);
#endif
                        }
                    }

                    meshUV3s = null;
                }

            }

            #endregion

            #region Pre-Caching

            public virtual bool IsPrecached => true;
            public virtual bool Precache() => false;

            public virtual bool TryPrecache()
            {
                if (IsPrecached) return false;
                return Precache();
            }

            #endregion

        }

        public abstract class SerializedDataGroupsShapes : SerializedDataBase
        {

            #region Fields

            [Header("Shapes")]
            public MeshShape[] meshShapes;

            [Header("Vertex Groups")]
            public VertexGroup[] vertexGroups;

            #endregion

            #region Interface

            public override int MeshShapeCount => meshShapes == null ? 0 : meshShapes.Length;
            public override int MeshShapeDeltasCount
            {
                get
                {
                    if (meshShapes == null) return 0;

                    int count = 0;

                    foreach (var shape in meshShapes)
                    {
                        if (shape == null || shape.frames == null) continue;
                        count = count + shape.frames.Length * vertexCount;
                    }

                    return count;
                }
            }
            public MeshShape GetShape(int index)
            {
                if (index < 0 || meshShapes == null || index >= meshShapes.Length) return null;
                return GetShapeUnsafe(index);
            }
            public MeshShape GetShapeUnsafe(int index) => meshShapes[index];
            public override ShapeInfo GetShapeInfo(int index)
            {
                var shape = GetShape(index);
                if (shape != null) return shape;
                return default;
            }
            public override ShapeInfo GetShapeInfoUnsafe(int index) => GetShapeUnsafe(index);
            public override int IndexOfShape(string shapeName, bool caseSensitive = false)
            {
                if (meshShapes == null) return -1;

                for (int a = 0; a < meshShapes.Length; a++)
                {
                    var morph = meshShapes[a];
                    if (morph == null) continue;

                    if (morph.name == shapeName) return a;
                }
                if (caseSensitive) return -1;

                shapeName = shapeName.ToLower().Trim();
                for (int a = 0; a < meshShapes.Length; a++)
                {
                    var morph = meshShapes[a];
                    if (morph == null) continue;

                    if (!string.IsNullOrWhiteSpace(morph.name) && morph.name.ToLower().Trim() == shapeName) return a;
                }

                return -1;
            }
            public List<MeshShape> GetShapes(List<MeshShape> outputList = null)
            {
                if (outputList == null) outputList = new List<MeshShape>();

                if (meshShapes != null) outputList.AddRange(meshShapes);

                return outputList;
            }
            public override List<ShapeInfo> GetShapeInfos(List<ShapeInfo> outputList = null)
            {
                if (outputList == null) outputList = new List<ShapeInfo>();
                if (meshShapes != null)
                {
                    foreach (var shape in meshShapes)
                    {
                        if (shape == null) continue;
                        outputList.Add(shape);
                    }
                }
                return outputList;
            }

            public override int VertexGroupCount => vertexGroups == null ? 0 : vertexGroups.Length;
            public VertexGroup GetVertexGroup(int index)
            {
                if (index < 0 || vertexGroups == null || index >= vertexGroups.Length) return null;
                return GetVertexGroupUnsafe(index);
            }
            public VertexGroup GetVertexGroupUnsafe(int index) => vertexGroups[index];
            public override VertexGroupInfo GetVertexGroupInfo(int index)
            {
                var vg = GetVertexGroup(index);
                if (vg != null) return vg;
                return default;
            }
            public override VertexGroupInfo GetVertexGroupInfoUnsafe(int index) => GetVertexGroupUnsafe(index);
            public override int IndexOfVertexGroup(string vertexGroupName, bool caseSensitive = false)
            {
                if (vertexGroups == null) return -1;

                for (int a = 0; a < vertexGroups.Length; a++)
                {
                    var vg = vertexGroups[a];
                    if (vg == null) continue;

                    if (vg.name == vertexGroupName) return a;
                }
                if (caseSensitive) return -1;

                vertexGroupName = vertexGroupName.ToLower().Trim();
                for (int a = 0; a < vertexGroups.Length; a++)
                {
                    var vg = vertexGroups[a];
                    if (vg == null) continue;

                    if (!string.IsNullOrWhiteSpace(vg.name) && vg.name.ToLower().Trim() == vertexGroupName) return a;
                }

                return -1;
            }
            public List<VertexGroup> GetVertexGroups(List<VertexGroup> outputList = null)
            {
                if (outputList == null) outputList = new List<VertexGroup>();

                if (vertexGroups != null) outputList.AddRange(vertexGroups);

                return outputList;
            }
            public override List<VertexGroupInfo> GetVertexGroupInfos(List<VertexGroupInfo> outputList = null)
            {
                if (outputList == null) outputList = new List<VertexGroupInfo>();
                if (vertexGroups != null)
                {
                    foreach (var vg in vertexGroups)
                    {
                        if (vg == null) continue;
                        outputList.Add(vg);
                    }
                }
                return outputList;
            }

            public override int IndexOfStandaloneVertexGroup(string name, bool caseSensitive = false)
            {
                if (vertexGroups == null) return -1;

                for (int a = 0; a < StandaloneVertexGroupCount; a++)
                {
                    var vg = vertexGroups[a + standaloneGroups.x];
                    if (vg == null) continue;

                    if (vg.name == name) return a;
                }
                if (caseSensitive) return -1;

                name = name.ToLower().Trim();
                for (int a = 0; a < StandaloneVertexGroupCount; a++)
                {
                    var vg = vertexGroups[a + standaloneGroups.x];
                    if (vg == null) continue;

                    if (!string.IsNullOrWhiteSpace(vg.name) && vg.name.ToLower().Trim() == name) return a;
                }

                return -1;
            }
            public VertexGroup GetStandaloneVertexGroup(int index)
            {
                if (index < 0 || index >= StandaloneVertexGroupCount) return null;
                return vertexGroups[standaloneGroups.x + index];
            }
            public override VertexGroupInfo GetStandaloneVertexGroupInfo(int index)
            {
                var vg = GetStandaloneVertexGroup(index);
                if (vg != null) return vg;
                return default;
            }

            public override int IndexOfMuscleGroup(string name, bool caseSensitive = false)
            {
                if (vertexGroups == null) return -1;

                for (int a = 0; a < MuscleVertexGroupCount; a++)
                {
                    var vg = vertexGroups[a + muscleGroups.x];
                    if (vg == null) continue;

                    if (vg.name == name) return a;
                }
                if (caseSensitive) return -1;

                name = name.ToLower().Trim();
                for (int a = 0; a < MuscleVertexGroupCount; a++)
                {
                    var vg = vertexGroups[a + muscleGroups.x];
                    if (vg == null) continue;

                    if (!string.IsNullOrWhiteSpace(vg.name) && vg.name.ToLower().Trim() == name) return a;
                }

                return -1;
            }
            public VertexGroup GetMuscleVertexGroup(int index)
            {
                if (index < 0 || index >= MuscleVertexGroupCount) return null;
                return vertexGroups[muscleGroups.x + index];
            }
            public override VertexGroupInfo GetMuscleVertexGroupInfo(int index)
            {
                var vg = GetMuscleVertexGroup(index);
                if (vg != null) return vg;
                return default;
            }

            public override int IndexOfFatGroup(string name, bool caseSensitive = false)
            {
                if (vertexGroups == null) return -1;

                for (int a = 0; a < FatVertexGroupCount; a++)
                {
                    var vg = vertexGroups[a + fatGroups.x];
                    if (vg == null) continue;

                    if (vg.name == name) return a;
                }
                if (caseSensitive) return -1;

                name = name.ToLower().Trim();
                for (int a = 0; a < FatVertexGroupCount; a++)
                {
                    var vg = vertexGroups[a + fatGroups.x];
                    if (vg == null) continue;

                    if (!string.IsNullOrWhiteSpace(vg.name) && vg.name.ToLower().Trim() == name) return a;
                }

                return -1;
            }
            public VertexGroup GetFatVertexGroup(int index)
            {
                if (index < 0 || index >= FatVertexGroupCount) return null;
                return vertexGroups[fatGroups.x + index];
            }
            public override VertexGroupInfo GetFatVertexGroupInfo(int index)
            {
                var vg = GetFatVertexGroup(index);
                if (vg != null) return vg;
                return default;
            }

            public override int IndexOfVariationGroup(string name, bool caseSensitive = false)
            {
                if (vertexGroups == null) return -1;

                for (int a = 0; a < VariationVertexGroupCount; a++)
                {
                    var vg = vertexGroups[a + variationGroups.x];
                    if (vg == null) continue;

                    if (vg.name == name) return a;
                }
                if (caseSensitive) return -1;

                name = name.ToLower().Trim();
                for (int a = 0; a < VariationVertexGroupCount; a++)
                {
                    var vg = vertexGroups[a + variationGroups.x];
                    if (vg == null) continue;

                    if (!string.IsNullOrWhiteSpace(vg.name) && vg.name.ToLower().Trim() == name) return a;
                }

                return -1;
            }
            public VertexGroup GetVariationVertexGroup(int index)
            {
                if (index < 0 || index >= VariationVertexGroupCount) return null;
                return vertexGroups[index + variationGroups.x];
            }
            public override VertexGroupInfo GetVariationVertexGroupInfo(int index)
            {
                var vg = GetVariationVertexGroup(index);
                if (vg != null) return vg;
                return default;
            }
            public VertexGroup GetVariationGroup(int index) => GetVariationVertexGroup(index);
            public override VertexGroupInfo GetVariationGroupInfo(int index) => GetVariationVertexGroupInfo(index);

            public override int IndexOfStandaloneShape(string name, bool caseSensitive = false)
            {
                if (meshShapes == null) return -1;

                for (int a = 0; a < StandaloneShapesCount; a++)
                {
                    var shape = meshShapes[a + standaloneShapes.x];
                    if (shape == null) continue;

                    if (shape.name == name) return a;
                }
                if (caseSensitive) return -1;

                name = name.ToLower().Trim();
                for (int a = 0; a < StandaloneShapesCount; a++)
                {
                    var shape = meshShapes[a + standaloneShapes.x];
                    if (shape == null) continue;

                    if (!string.IsNullOrWhiteSpace(shape.name) && shape.name.ToLower().Trim() == name) return a;
                }

                return -1;
            }
            public MeshShape GetStandaloneShape(int index)
            {
                if (index < 0 || index >= StandaloneShapesCount) return null;
                return meshShapes[standaloneShapes.x + index];
            }
            public override ShapeInfo GetStandaloneShapeInfo(int index)
            {
                var shape = GetStandaloneShape(index);
                if (shape != null) return shape;
                return default;
            }

            public MeshShape MassShapeInstance => massShape >= 0 && meshShapes != null ? meshShapes[massShape] : null;
            public override ShapeInfo MassShapeInfo => massShape >= 0 && meshShapes != null ? meshShapes[massShape] : default;
            public override int MassShapeFrameCount => massShape >= 0 && meshShapes != null ? meshShapes[massShape].FrameCount : 0;

            public MeshShape FlexShapeInstance => flexShape >= 0 && meshShapes != null ? meshShapes[flexShape] : null;
            public override ShapeInfo FlexShapeInfo => flexShape >= 0 && meshShapes != null ? meshShapes[flexShape] : default;
            public override int FlexShapeFrameCount => flexShape >= 0 && meshShapes != null ? meshShapes[flexShape].FrameCount : 0;

            public MeshShape FatShapeInstance => fatShape >= 0 && meshShapes != null ? meshShapes[fatShape] : null;
            public override ShapeInfo FatShapeInfo => fatShape >= 0 && meshShapes != null ? meshShapes[fatShape] : default;
            public override int FatShapeFrameCount => fatShape >= 0 && meshShapes != null ? meshShapes[fatShape].FrameCount : 0;

            public MeshShape FatMuscleBlendShapeInstance => fatMuscleBlendShape >= 0 && meshShapes != null ? meshShapes[fatMuscleBlendShape] : null;
            public override ShapeInfo FatMuscleBlendShapeInfo => fatMuscleBlendShape >= 0 && meshShapes != null ? meshShapes[fatMuscleBlendShape] : default;
            public override int FatMuscleBlendShapeFrameCount => fatMuscleBlendShape >= 0 && meshShapes != null ? meshShapes[fatMuscleBlendShape].FrameCount : 0;

            public MeshShape BustSizeShapeInstance => bustSizeShape >= 0 && meshShapes != null ? meshShapes[bustSizeShape] : null;
            public override ShapeInfo BustSizeShapeInfo => bustSizeShape >= 0 && meshShapes != null ? meshShapes[bustSizeShape] : default;
            public override int BustSizeShapeFrameCount => bustSizeShape >= 0 && meshShapes != null ? meshShapes[bustSizeShape].FrameCount : 0;

            public MeshShape BustSizeMuscleShapeInstance => bustSizeMuscleShape >= 0 && meshShapes != null ? meshShapes[bustSizeMuscleShape] : null;
            public override ShapeInfo BustSizeMuscleShapeInfo => bustSizeMuscleShape >= 0 && meshShapes != null ? meshShapes[bustSizeMuscleShape] : default;
            public override int BustSizeMuscleShapeFrameCount => bustSizeMuscleShape >= 0 && meshShapes != null ? meshShapes[bustSizeMuscleShape].FrameCount : 0;


            public override int IndexOfVariationShape(string name, bool caseSensitive = false)
            {
                if (meshShapes == null) return -1;

                for (int a = 0; a < VariationShapesCount; a++)
                {
                    var shape = meshShapes[a + variationShapes.x];
                    if (shape == null) continue;

                    if (shape.name == name) return a;
                }
                if (caseSensitive) return -1;

                name = name.ToLower().Trim();
                for (int a = 0; a < VariationShapesCount; a++)
                {
                    var shape = meshShapes[a + variationShapes.x];
                    if (shape == null) continue;

                    if (!string.IsNullOrWhiteSpace(shape.name) && shape.name.ToLower().Trim() == name) return a;
                }

                return -1;
            }
            public MeshShape GetVariationShape(int index)
            {
                if (index < 0 || index >= VariationShapesCount) return null;
                return meshShapes[variationShapes.x + index];
            }
            public override ShapeInfo GetVariationShapeInfo(int index)
            {
                var shape = GetVariationShape(index);
                if (shape != null) return shape;
                return default;
            }

            [NonSerialized]
            protected ComputeBuffer meshShapeFrameWeightsBuffer;
            public override ComputeBuffer MeshShapeFrameWeightsBuffer
            {
                get
                {
                    if (meshShapeFrameWeightsBuffer == null)
                    {
                        tempFloats.Clear();

                        foreach (var meshShape in meshShapes)
                        {
                            if (meshShape == null || meshShape.frames == null) continue;

                            for (int frameIndex = 0; frameIndex < meshShape.frames.Length; frameIndex++)
                            {
                                var frame = meshShape.frames[frameIndex];
                                tempFloats.Add(frame.weight);
                            }
                        }

                        if (tempFloats.Count > 0)
                        {
                            meshShapeFrameWeightsBuffer = new ComputeBuffer(tempFloats.Count, UnsafeUtility.SizeOf(typeof(float)), ComputeBufferType.Structured, ComputeBufferMode.Immutable);
                            meshShapeFrameWeightsBuffer.SetData(tempFloats);
                        }

                        tempFloats.Clear();

                        TrackDisposables();
                    }

                    return meshShapeFrameWeightsBuffer;
                }
            }
            protected static readonly List<int2> tempRanges = new List<int2>();
            [NonSerialized]
            protected ComputeBuffer meshShapeIndicesBuffer;
            public override ComputeBuffer MeshShapeIndicesBuffer
            {
                get
                {
                    if (meshShapeIndicesBuffer == null)
                    {
                        tempRanges.Clear();

                        int startIndex = 0;
                        foreach (var meshShape in meshShapes)
                        {
                            if (meshShape == null || meshShape.frames == null) continue;

                            tempRanges.Add(new int2(startIndex, meshShape.frames.Length));

                            startIndex += meshShape.frames.Length;
                        }

                        if (tempRanges.Count > 0)
                        {
                            meshShapeIndicesBuffer = new ComputeBuffer(tempRanges.Count, UnsafeUtility.SizeOf(typeof(int2)), ComputeBufferType.Structured, ComputeBufferMode.Immutable);
                            meshShapeIndicesBuffer.SetData(tempRanges);
                        }

                        tempRanges.Clear();

                        TrackDisposables();
                    }

                    return meshShapeIndicesBuffer;
                }
            }

            #endregion

            #region Disposal

            public override void Dispose()
            {
                base.Dispose();

                try
                {
                    if (meshShapeFrameWeightsBuffer != null && meshShapeFrameWeightsBuffer.IsValid())
                    {
                        meshShapeFrameWeightsBuffer.Dispose();
                        meshShapeFrameWeightsBuffer = null;
                    }
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR
                    Debug.LogException(ex);
#endif
                }

                try
                {
                    if (meshShapeIndicesBuffer != null && meshShapeIndicesBuffer.IsValid())
                    {
                        meshShapeIndicesBuffer.Dispose();
                        meshShapeIndicesBuffer = null;
                    }
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR
                    Debug.LogException(ex);
#endif
                }

            }

            #endregion

        }

        [Serializable, NonAnimatable]
        public class SerializedData : SerializedDataGroupsShapes
        {

            #region Fields

            [Header("Vertex Color Deltas")]
            public VertexColorDelta[] vertexColorDeltas;

            #endregion

            #region Interface

            public override ComputeBuffer MuscleGroupInfluencesBuffer
            {
                get
                {
                    if (muscleGroupInfluencesBuffer == null)
                    {
                        if (precache_muscleGroupInfluences == null || precache_muscleGroupInfluences.Length <= 0)
                        {
                            PrecacheMuscleGroupInfluences();
                        }

                        muscleGroupInfluencesBuffer = new ComputeBuffer(precache_muscleGroupInfluences.Length, UnsafeUtility.SizeOf(typeof(BoneWeight8Float)), ComputeBufferType.Structured, ComputeBufferMode.Immutable); // we use the bone weight structs because the data is identical (boneIndex = vertex group Index, boneWeight = vertex group weight)
                        if (precache_muscleGroupInfluences.Length > 0) muscleGroupInfluencesBuffer.SetData(precache_muscleGroupInfluences);

                        TrackDisposables();
                    }

                    return muscleGroupInfluencesBuffer;
                }
            }

            public override ComputeBuffer FatGroupInfluencesBuffer
            {
                get
                {
                    if (fatGroupInfluencesBuffer == null)
                    {
                        if (precache_fatGroupInfluences == null || precache_fatGroupInfluences.Length <= 0)
                        {
                            PrecacheFatGroupInfluences();
                        }

                        fatGroupInfluencesBuffer = new ComputeBuffer(precache_fatGroupInfluences.Length, UnsafeUtility.SizeOf(typeof(BoneWeight8Float)), ComputeBufferType.Structured, ComputeBufferMode.Immutable); // we use the bone weight structs because the data is identical (boneIndex = vertex group Index, boneWeight = vertex group weight)
                        if (precache_fatGroupInfluences.Length > 0) fatGroupInfluencesBuffer.SetData(precache_fatGroupInfluences);

                        TrackDisposables();
                    }

                    return fatGroupInfluencesBuffer;
                }
            }

            public override ComputeBuffer VertexGroupsBuffer
            {
                get
                {
                    if (vertexGroupsBuffer == null)
                    {
                        if (precache_vertexGroups != null && precache_vertexGroups.Length > 0)
                        {
                            vertexGroupsBuffer = new ComputeBuffer(precache_vertexGroups.Length, UnsafeUtility.SizeOf(typeof(float)), ComputeBufferType.Structured, ComputeBufferMode.Immutable);
                            vertexGroupsBuffer.SetData(precache_vertexGroups);
                        }
                        else
                        {
                            tempFloats.Clear();

                            foreach (var vertexGroup in vertexGroups)
                            {
                                for (int vertexIndex = 0; vertexIndex < vertexCount; vertexIndex++) tempFloats.Add(vertexGroup[vertexIndex]);
                            }

                            if (tempFloats.Count > 0)
                            {
                                vertexGroupsBuffer = new ComputeBuffer(tempFloats.Count, UnsafeUtility.SizeOf(typeof(float)), ComputeBufferType.Structured, ComputeBufferMode.Immutable);
                                vertexGroupsBuffer.SetData(tempFloats);
                            }

                            tempFloats.Clear();
                        }

                        TrackDisposables();
                    }

                    return vertexGroupsBuffer;
                }
            }

            public override ComputeBuffer MeshShapeFrameDeltasBuffer
            {
                get
                {
                    if (meshShapeFrameDeltasBuffer == null)
                    {
                        if (precache_meshShapeFrameDeltas == null || precache_meshShapeFrameDeltas.Length <= 0)
                        {
                            PrecacheMeshShapeFrameDeltas();
                        }

                        if (precache_meshShapeFrameDeltas.Length > 0)
                        {
                            meshShapeFrameDeltasBuffer = new ComputeBuffer(precache_meshShapeFrameDeltas.Length, UnsafeUtility.SizeOf(typeof(MorphShapeVertex)), ComputeBufferType.Structured, ComputeBufferMode.Immutable);
                            meshShapeFrameDeltasBuffer.SetData(precache_meshShapeFrameDeltas);
                        }

                        TrackDisposables();
                    }

                    return meshShapeFrameDeltasBuffer;
                }
            }


            public int VertexColorDeltaCount => vertexColorDeltas == null ? 0 : vertexColorDeltas.Length;
            public VertexColorDelta GetVertexColorDelta(int index)
            {
                if (index < 0 || vertexColorDeltas == null || index >= vertexColorDeltas.Length) return null;
                return GetVertexColorDeltaUnsafe(index);
            }
            public VertexColorDelta GetVertexColorDeltaUnsafe(int index) => vertexColorDeltas[index];
            public int IndexOfVertexColorDelta(string deltaName, bool caseSensitive = false)
            {
                if (vertexColorDeltas == null) return -1;

                for (int a = 0; a < vertexColorDeltas.Length; a++)
                {
                    var delta = vertexColorDeltas[a];
                    if (delta == null) continue;

                    if (delta.name == deltaName) return a;
                }
                if (caseSensitive) return -1;

                deltaName = deltaName.ToLower().Trim();
                for (int a = 0; a < vertexColorDeltas.Length; a++)
                {
                    var delta = vertexColorDeltas[a];
                    if (delta == null) continue;

                    if (!string.IsNullOrWhiteSpace(delta.name) && delta.name.ToLower().Trim() == deltaName) return a;
                }

                return -1;
            }
            public List<VertexColorDelta> GetVertexColorDeltas(List<VertexColorDelta> outputList = null)
            {
                if (outputList == null) outputList = new List<VertexColorDelta>();

                if (vertexColorDeltas != null) outputList.AddRange(vertexColorDeltas);

                return outputList;
            }

            protected static readonly List<float4> tempColorDeltas = new List<float4>();
            [NonSerialized]
            protected ComputeBuffer vertexColorDeltasBuffer;
            public override ComputeBuffer VertexColorDeltasBuffer
            {
                get
                {
                    if (vertexColorDeltasBuffer == null)
                    {
                        if (precache_vertexColorDeltas == null || precache_vertexColorDeltas.Length <= 0)
                        {
                            PrecacheVertexColorDeltas();
                        }

                        if (precache_vertexColorDeltas.Length > 0)
                        {
                            vertexColorDeltasBuffer = new ComputeBuffer(precache_vertexColorDeltas.Length, UnsafeUtility.SizeOf(typeof(float4)), ComputeBufferType.Structured, ComputeBufferMode.Immutable);
                            vertexColorDeltasBuffer.SetData(precache_vertexColorDeltas);
                        }

                        TrackDisposables();
                    }

                    return vertexColorDeltasBuffer;
                }
            }

            #endregion

            #region Disposal

            public override void Dispose()
            {
                base.Dispose();

                try
                {
                    if (vertexColorDeltasBuffer != null && vertexColorDeltasBuffer.IsValid())
                    {
                        vertexColorDeltasBuffer.Dispose();
                        vertexColorDeltasBuffer = null;
                    }
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR
                    Debug.LogException(ex);
#endif
                }

            }

            #endregion

            #region Pre-Caching

            //[SerializeField, HideInInspector] 
            [NonSerialized] // force regeneration on first call to save disk space?
            public MeshVertexDelta[] precache_meshShapeDeltas;
            //[SerializeField, HideInInspector]
            [NonSerialized] // force regeneration on first call to save disk space?
            public float[] precache_meshShapeFrameWeights;
            //[SerializeField, HideInInspector]
            [NonSerialized] // force regeneration on first call to save disk space?
            public int2[] precache_meshShapeInfos;

            //[SerializeField, HideInInspector]
            [NonSerialized] // force regeneration on first call to save disk space?
            public float[] precache_vertexGroups;

            [SerializeField, HideInInspector]
            public GroupControlWeight2[] precache_blankMuscleGroupControlWeights;
            [SerializeField, HideInInspector]
            public GroupVertexControlWeight[] precache_muscleGroupVertexWeights;

            [SerializeField, HideInInspector]
            public GroupControlWeight2[] precache_blankFatGroupControlWeights;
            [SerializeField, HideInInspector]
            public GroupVertexControlWeight[] precache_fatGroupVertexWeights;

            [SerializeField, HideInInspector]
            public GroupControlWeight2[] precache_blankVariationGroupControlWeights;
            [SerializeField, HideInInspector]
            public GroupVertexControlWeight[] precache_variationGroupVertexWeights;

            [SerializeField, HideInInspector]
            public BoneWeight8Float[] precache_muscleGroupInfluences;
            [SerializeField, HideInInspector]
            public BoneWeight8Float[] precache_fatGroupInfluences;

            public bool PrecacheMuscleGroupInfluences()
            {
                if (precache_muscleGroupInfluences != null && precache_muscleGroupInfluences.Length > 0) return false;

                Debug.Log("Pre-caching muscle group influences...");

                tempBoneWeights.Clear();

                int muscleGroupCount = MuscleGroupsCount;
                for (int vertexIndex = 0; vertexIndex < vertexCount; vertexIndex++)
                {
                    tempWeights.Clear();

                    for (int muscleGroupIndex = 0; muscleGroupIndex < muscleGroupCount; muscleGroupIndex++)
                    {
                        int vgIndex = muscleGroups.x + muscleGroupIndex;
                        float weight = vertexGroups[vgIndex][vertexIndex];
                        if (weight <= 0f) continue;

                        tempWeights.Add(new BoneWeight1()
                        {
                            boneIndex = muscleGroupIndex, // shader needs the local index
                            weight = weight
                        });
                    }

                    var finalWeights = new BoneWeight8Float();
                    tempWeights.Sort(CompareWeight);
                    float totalWeight = 0f;
#if UNITY_EDITOR
                    //if (tempWeights.Count > 8) Debug.LogWarning($"Customizable Mesh vertex {vertexIndex} is affected by more than 8 muscle groups!");
#endif
                    for (int a = 0; a < Mathf.Min(tempWeights.Count, 8); a++)
                    {
                        var weight = tempWeights[a];
                        finalWeights = finalWeights.Modify(a, weight.boneIndex, weight.weight);

                        totalWeight += weight.weight;
                    }
#if UNITY_EDITOR
                    if (totalWeight < 0.99f && totalWeight > 0f) Debug.LogWarning($"Customizable Mesh vertex {vertexIndex} muscle group influences do not total or exceed 1! (total: {totalWeight})");
#endif
                    if (totalWeight > 0f)
                    {
                        finalWeights.weightsA = finalWeights.weightsA / totalWeight;
                        finalWeights.weightsB = finalWeights.weightsB / totalWeight;
                    }
                    tempBoneWeights.Add(finalWeights);
                }

                precache_muscleGroupInfluences = tempBoneWeights.ToArray();

                tempBoneWeights.Clear();
                tempWeights.Clear();

                return true;
            }

            public bool PrecacheFatGroupInfluences()
            {
                if (precache_fatGroupInfluences != null && precache_fatGroupInfluences.Length > 0) return false;

                Debug.Log("Pre-caching fat group influences...");

                tempBoneWeights.Clear();

                int fatGroupCount = FatGroupsCount;
                for (int vertexIndex = 0; vertexIndex < vertexCount; vertexIndex++)
                {
                    tempWeights.Clear();

                    for (int fatGroupIndex = 0; fatGroupIndex < fatGroupCount; fatGroupIndex++)
                    {
                        int vgIndex = fatGroups.x + fatGroupIndex;
                        tempWeights.Add(new BoneWeight1()
                        {
                            boneIndex = fatGroupIndex, // shader needs the local index
                            weight = vertexGroups[vgIndex][vertexIndex]
                        });
                    }

                    var finalWeights = new BoneWeight8Float();
                    tempWeights.Sort(CompareWeight);
                    float totalWeight = 0f;
#if UNITY_EDITOR
                    //if (tempWeights.Count > 8) Debug.LogWarning($"Customizable Mesh vertex {vertexIndex} is affected by more than 8 fat groups!");
#endif
                    for (int a = 0; a < Mathf.Min(tempWeights.Count, 8); a++)
                    {
                        var weight = tempWeights[a];
                        finalWeights = finalWeights.Modify(a, weight.boneIndex, weight.weight);

                        totalWeight += weight.weight;
                    }

                    if (totalWeight > 0f)
                    {
                        finalWeights.weightsA = finalWeights.weightsA / totalWeight;
                        finalWeights.weightsB = finalWeights.weightsB / totalWeight;
                    }
                    tempBoneWeights.Add(finalWeights);
                }

                precache_fatGroupInfluences = tempBoneWeights.ToArray();

                tempBoneWeights.Clear();
                tempWeights.Clear();

                return true;
            }

            [SerializeField, HideInInspector]
            public MorphShapeVertex[] precache_meshShapeFrameDeltas;

            public bool PrecacheMeshShapeFrameDeltas()
            {
                if (precache_meshShapeFrameDeltas != null && precache_meshShapeFrameDeltas.Length > 0) return false;

                Debug.Log("Pre-caching mesh shape frame deltas..."); 

                tempFrameDeltas.Clear();

                foreach (var meshShape in meshShapes)
                {
                    if (meshShape == null || meshShape.frames == null) continue;

                    for (int frameIndex = 0; frameIndex < meshShape.frames.Length; frameIndex++)
                    {
                        var frame = meshShape.frames[frameIndex];
                        for (int vertexIndex = 0; vertexIndex < vertexCount; vertexIndex++) tempFrameDeltas.Add(frame.deltas == null ? default : frame.deltas[vertexIndex]);
                    }
                }

                precache_meshShapeFrameDeltas = tempFrameDeltas.ToArray();

                tempFrameDeltas.Clear();

                return true;
            }

            [SerializeField, HideInInspector]
            public float4[] precache_vertexColorDeltas;

            public bool PrecacheVertexColorDeltas()
            {
                if (precache_vertexColorDeltas != null && precache_vertexColorDeltas.Length > 0) return false;

                Debug.Log("Pre-caching vertex color deltas...");

                tempColorDeltas.Clear();

                if (vertexColorDeltas != null)
                {
                    foreach (var delta in vertexColorDeltas)
                    {
                        if (delta == null || delta.deltaColors == null) continue;

                        for (int vertexIndex = 0; vertexIndex < vertexCount; vertexIndex++) tempColorDeltas.Add((Vector4)delta.deltaColors[vertexIndex]);
                    }
                }

                precache_vertexColorDeltas = tempColorDeltas.ToArray();

                tempColorDeltas.Clear();

                return false;
            }

            public override bool IsPrecached => (precache_meshShapeDeltas != null && precache_meshShapeDeltas.Length != 0)
                && (precache_meshShapeFrameWeights != null && precache_meshShapeFrameWeights.Length != 0)
                && (precache_meshShapeInfos != null && precache_meshShapeInfos.Length != 0)
                && (precache_vertexGroups != null && precache_vertexGroups.Length != 0)
                && (precache_blankMuscleGroupControlWeights != null && precache_blankMuscleGroupControlWeights.Length != 0)
                && (precache_muscleGroupVertexWeights != null && precache_muscleGroupVertexWeights.Length != 0)
                && (precache_blankFatGroupControlWeights != null && precache_blankFatGroupControlWeights.Length != 0)
                && (precache_fatGroupVertexWeights != null && precache_fatGroupVertexWeights.Length != 0)
                && (precache_blankVariationGroupControlWeights != null && precache_blankVariationGroupControlWeights.Length != 0)
                && (precache_variationGroupVertexWeights != null && precache_variationGroupVertexWeights.Length != 0);

            private static readonly List<GroupControlWeight2> tempGroupControlWeights = new List<GroupControlWeight2>();
            private static readonly List<GroupVertexControlWeight> tempGroupVertexWeights = new List<GroupVertexControlWeight>();
            public override bool Precache()
            {
                bool didPrecache = false;
                if (precache_meshShapeDeltas == null || precache_meshShapeDeltas.Length == 0 || precache_meshShapeInfos == null || precache_meshShapeInfos.Length == 0 || precache_meshShapeFrameWeights == null || precache_meshShapeFrameWeights.Length == 0)
                {
                    Debug.Log("Pre-caching mesh shape data...");

                    tempFloats.Clear();

                    precache_meshShapeDeltas = new MeshVertexDelta[MeshShapeDeltasCount];
                    precache_meshShapeInfos = new int2[MeshShapeCount];
                    if (meshShapes != null)
                    {
                        int ind = 0;
                        int frameInd = 0;
                        for (int i = 0; i < meshShapes.Length; i++)
                        {
                            var shape = meshShapes[i];

                            if (shape == null) continue;

                            precache_meshShapeInfos[i] = new int2(frameInd, shape.frames == null ? 0 : shape.frames.Length); // x = start index in frame weights buffer, y = frame count

                            if (shape.frames == null) continue;

                            for (int j = 0; j < shape.frames.Length; j++)
                            {
                                var frame = shape.frames[j];
                                tempFloats.Add(frame.weight);
                                frameInd++;

                                if (frame.deltas == null)
                                {
                                    ind += vertexCount;
                                    continue;
                                }

                                int subCount = Mathf.Min(vertexCount, frame.deltas.Length);
                                for (int k = 0; k < subCount; k++)
                                {
                                    precache_meshShapeDeltas[ind] = frame.deltas[k];
                                    ind++;
                                }

                                ind += vertexCount - subCount;

                            }
                        }
                    }

                    precache_meshShapeFrameWeights = tempFloats.ToArray();
                    tempFloats.Clear();

                    didPrecache = true;
                }

                if (precache_vertexGroups == null || precache_vertexGroups.Length != VertexGroupCount * vertexCount)
                {
                    Debug.Log("Pre-caching vertex group data...");

                    precache_vertexGroups = new float[VertexGroupCount * vertexCount];
                    if (vertexGroups != null)
                    {
                        for (int i = 0; i < vertexGroups.Length; i++)
                        {
                            var group = vertexGroups[i];
                            if (group == null) continue;

                            group.InsertIntoArray(precache_vertexGroups, i * vertexCount);
                        }
                    }

                    didPrecache = true;
                }

                #region Muscle Groups

                if (precache_blankMuscleGroupControlWeights == null || precache_blankMuscleGroupControlWeights.Length == 0 || precache_muscleGroupVertexWeights == null || precache_muscleGroupVertexWeights.Length == 0)
                {

                    Debug.Log("Pre-caching muscle group data...");

                    tempGroupControlWeights.Clear();
                    tempGroupVertexWeights.Clear();
                    if (muscleGroups.y >= muscleGroups.x)
                    {
                        for (int g = muscleGroups.x; g <= muscleGroups.y; g++)
                        {
                            int localGroupIndex = g - muscleGroups.x;
                            var vertexGroup = vertexGroups[g];
                            int weightsStartIndex = tempGroupVertexWeights.Count;
                            tempGroupControlWeights.Add(new GroupControlWeight2()
                            {
                                groupIndex = localGroupIndex,
                                vertexCount = vertexGroup.EntryCount,
                                vertexSequenceStartIndex = weightsStartIndex,
                                weight = 0f
                            });

                            for (int i = 0; i < vertexGroup.EntryCount; i++)
                            {
                                vertexGroup.GetEntry(i, out int vertexIndex, out float vertexWeight);
#if UNITY_EDITOR
                                if (vertexWeight > 1.001f) Debug.LogWarning($"Vertex index {vertexIndex} for muscle group {vertexGroup.name} has weight of {vertexWeight}");
#endif
                                tempGroupVertexWeights.Add(new GroupVertexControlWeight()
                                {
                                    groupIndex = localGroupIndex,
                                    vertexIndex = vertexIndex,
                                    weight = vertexWeight
                                });
                            }
                        }
                    }

                    precache_blankMuscleGroupControlWeights = tempGroupControlWeights.ToArray();
                    precache_muscleGroupVertexWeights = tempGroupVertexWeights.ToArray();
                    tempGroupControlWeights.Clear();
                    tempGroupVertexWeights.Clear();

                    didPrecache = true;
                }

                #endregion

                #region Fat Groups

                if (precache_blankFatGroupControlWeights == null || precache_blankFatGroupControlWeights.Length == 0 || precache_fatGroupVertexWeights == null || precache_fatGroupVertexWeights.Length == 0)
                {

                    Debug.Log("Pre-caching fat group data...");

                    tempGroupControlWeights.Clear();
                    tempGroupVertexWeights.Clear();
                    if (fatGroups.y >= fatGroups.x)
                    {
                        for (int g = fatGroups.x; g <= fatGroups.y; g++)
                        {
                            int localGroupIndex = g - fatGroups.x;
                            var vertexGroup = vertexGroups[g];
                            tempGroupControlWeights.Add(new GroupControlWeight2()
                            {
                                groupIndex = localGroupIndex,
                                vertexCount = vertexGroup.EntryCount,
                                vertexSequenceStartIndex = tempGroupVertexWeights.Count,
                                weight = new float2(0f, 0f) // TODO: Apply fat muscle modifier value to y component
                            });

                            for (int i = 0; i < vertexGroup.EntryCount; i++)
                            {
                                vertexGroup.GetEntry(i, out int vertexIndex, out float vertexWeight);

                                tempGroupVertexWeights.Add(new GroupVertexControlWeight()
                                {
                                    groupIndex = localGroupIndex,
                                    vertexIndex = vertexIndex,
                                    weight = vertexWeight
                                });
                            }
                        }
                    }

                    precache_blankFatGroupControlWeights = tempGroupControlWeights.ToArray();
                    precache_fatGroupVertexWeights = tempGroupVertexWeights.ToArray();

                    didPrecache = true;
                }

                #endregion

                #region Variation Groups

                if (precache_blankVariationGroupControlWeights == null || precache_blankVariationGroupControlWeights.Length == 0 || precache_variationGroupVertexWeights == null || precache_variationGroupVertexWeights.Length == 0)
                {

                    Debug.Log("Pre-caching variation group data...");

                    tempGroupControlWeights.Clear();
                    tempGroupVertexWeights.Clear();
                    if (variationGroups.y >= variationGroups.x)
                    {
                        for (int g = variationGroups.x; g <= variationGroups.y; g++)
                        {
                            int localGroupIndex = g - variationGroups.x;
                            var vertexGroup = vertexGroups[g];

                            int startIndex = tempGroupVertexWeights.Count;
                            int entryCount = 0;
                            for (int i = 0; i < vertexGroup.EntryCount; i++)
                            {
                                vertexGroup.GetEntry(i, out int vertexIndex, out float vertexWeight);
                                if (vertexWeight <= 0f) continue;

                                entryCount++;
                                tempGroupVertexWeights.Add(new GroupVertexControlWeight()
                                {
                                    groupIndex = localGroupIndex,
                                    vertexIndex = vertexIndex,
                                    weight = vertexWeight
                                });
                            }

                            for (int s = variationShapes.x; s <= variationShapes.y; s++)
                            {
                                tempGroupControlWeights.Add(new GroupControlWeight2()
                                {
                                    groupIndex = (localGroupIndex * VariationShapesCount) + s,
                                    vertexCount = entryCount,
                                    vertexSequenceStartIndex = startIndex,
                                    weight = 0f
                                });
                            }
                        }
                    }

                    precache_blankVariationGroupControlWeights = tempGroupControlWeights.ToArray();
                    precache_variationGroupVertexWeights = tempGroupVertexWeights.ToArray();
                    tempGroupControlWeights.Clear();
                    tempGroupVertexWeights.Clear();

                    didPrecache = true;
                }

                #endregion

                #region Muscle Group Influences

                if (PrecacheMuscleGroupInfluences()) didPrecache = true;

                #endregion

                #region Fat Group Influences

                if (PrecacheFatGroupInfluences()) didPrecache = true;

                #endregion

                if (PrecacheMeshShapeFrameDeltas()) didPrecache = true;

                if (PrecacheVertexColorDeltas()) didPrecache = true;

#if !UNITY_EDITOR
                if (meshShapes != null)
                {
                    for (int i = 0; i < meshShapes.Length; i++)
                    {
                        var shape = meshShapes[i];
                        if (shape == null) continue;

                        shape.frames = null; // free up some memory. TODO: tell frames to reference precached array instead of nulling
                    }
                }

                if (vertexGroups != null)
                {
                    for (int i = 0; i < vertexGroups.Length; i++)
                    {
                        var group = vertexGroups[i];
                        if (group == null) continue;

                        group.SetExternalWeightSource(precache_vertexGroups, i * vertexCount, vertexCount, true, true);
                    }
                }

                if (vertexColorDeltas != null)
                {
                    for (int i = 0; i < vertexColorDeltas.Length; i++)
                    {
                        var delta = vertexColorDeltas[i];
                        if (delta == null) continue;

                        delta.deltaColors = null;
                    }
                }
#endif


                return didPrecache;
            }

            #endregion

        }

#endregion

        #region Disposal

        public override void Dispose()
        {
            base.Dispose();
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();
        }

        #endregion

        /// <summary>
        /// Handles physique update jobs and buffer writes
        /// </summary>
        new protected InstanceV2 instance;
        public InstanceV2 Instance2 => instance;
        public MeshGroupV2 MeshGroup2 => instance == null ? null : instance.OwnerGroup;
        public override int InstanceID => instance == null ? 0 : instance.localID;
        public override int InstanceSlot => InstanceID;
        public override bool IsInitialized => HasValidInstance;
        public override bool HasValidInstance => instance != null && instance.IsValid;

        public UnityEvent<InstanceV2> OnClaimInstance = new UnityEvent<InstanceV2>();

        protected override void CreateInstance()
        {
            if (instance != null && instance.IsValid) return;

            instance = Updater.Register(Data);
            instance.updateManually = updateMeshManually;

            OnClaimInstance?.Invoke(instance);
        }

        protected override void OnAwake()
        {

            autoCreateInstance = false;

            if (data != null)
            {
                SetData(data);
            }

            base.OnAwake();

        }

        protected override void OnStart()
        {
#if UNITY_EDITOR
            //Utils.PrintNativeAllocationSizes($"{nameof(CustomizableCharacterMeshV2)}:{name}", this);
#endif
        }

        #region Data

        [SerializeField]
        protected CustomizableCharacterMeshV2_DATA data;
        protected override bool TrySetData(ICustomizableCharacterMeshBaseData data) 
        { 
            if (data is not CustomizableCharacterMeshV2_DATA meshData)
            {
                Debug.LogError($"Invalid data type assigned to {name}: {data.GetType().Name} (expected {typeof(CustomizableCharacterMeshV2_DATA).Name})");
                return false;
            }

            var prevData = this.data;
            this.data = meshData;

            return true;
        }
        public CustomizableCharacterMeshV2_DATA Data => data;
        public SerializedData SubData => data == null ? null : data.SerializedData;
        public override ICustomizableCharacterMeshBaseData CustomizationData => SubData;

        public override int DeltasStartIndex => SubData.vertexCount * instance.localID;

        protected override bool PrepInWorldDataFetch(int lod, int vertexIndex, out int topVertexIndex, out MuscleData muscleData, out float flexFactor, out MeshVertexDelta delta, out MeshShape flexShape, out float4x4 skinningMatrix) 
        {
            delta = default;
            flexShape = null;
            skinningMatrix = float4x4.identity;
            muscleData = default;
            flexFactor = 0f;
            topVertexIndex = vertexIndex;

            var subData = SubData;

            if (instance == null || !subData.TryGetBoneWeights(lod, out var boneWeightsArray)) return false;

            instance.UpdateIfDirty(false, true);
            instance.OwnerGroup.ActiveJob.Complete();

            topVertexIndex = vertexIndex;
            if (lod > 0)
            {
                if (TryGetUV(lod, subData.nearestVertexUVChannel, out var uvArray))
                {
                    topVertexIndex = MorphUtils.FetchIndexFromUV(subData.nearestVertexIndexElement, uvArray[vertexIndex]);
                }
            }

            flexShape = subData.GetShapeUnsafe(subData.flexShape);
            muscleData = GetMuscleDataForVertex(topVertexIndex);
            flexFactor = CalculateFinalFlexFactor(muscleData.flex, muscleData.mass, subData.flexEndPointWeight, subData.flexExponent, subData.flexNerfThreshold, subData.flexNerfExponent);

            delta = instance.OwnerGroup.FinalVertexDeltas[DeltasStartIndex + vertexIndex];

            var rigSampler = RigSampler;
            if (rigSampler != null)  
            {
                var boneWeights = boneWeightsArray[vertexIndex];
                skinningMatrix = (rigSampler.TrackingGroup[boneWeights.boneIndex0] * boneWeights.boneWeight0) +
                    (rigSampler.TrackingGroup[boneWeights.boneIndex1] * boneWeights.boneWeight1) +
                    (rigSampler.TrackingGroup[boneWeights.boneIndex2] * boneWeights.boneWeight2) +
                    (rigSampler.TrackingGroup[boneWeights.boneIndex3] * boneWeights.boneWeight3) +
                    (rigSampler.TrackingGroup[boneWeights.boneIndex4] * boneWeights.boneWeight4) +
                    (rigSampler.TrackingGroup[boneWeights.boneIndex5] * boneWeights.boneWeight5) +
                    (rigSampler.TrackingGroup[boneWeights.boneIndex6] * boneWeights.boneWeight6) +
                    (rigSampler.TrackingGroup[boneWeights.boneIndex7] * boneWeights.boneWeight7);
            }

            return true;
        }

        public override float4x4 GetVertexLocalToWorld(int lod, int vertexIndex)
        {
            var subData = SubData;
            if (!subData.TryGetBoneWeights(lod, out var boneWeightsArray)) return float4x4.identity;

            var rigSampler = RigSampler;
            if (rigSampler != null)
            {
                var boneWeights = boneWeightsArray[vertexIndex];
                return (rigSampler.TrackingGroup[boneWeights.boneIndex0] * boneWeights.boneWeight0) +
                    (rigSampler.TrackingGroup[boneWeights.boneIndex1] * boneWeights.boneWeight1) +
                    (rigSampler.TrackingGroup[boneWeights.boneIndex2] * boneWeights.boneWeight2) +
                    (rigSampler.TrackingGroup[boneWeights.boneIndex3] * boneWeights.boneWeight3) +
                    (rigSampler.TrackingGroup[boneWeights.boneIndex4] * boneWeights.boneWeight4) +
                    (rigSampler.TrackingGroup[boneWeights.boneIndex5] * boneWeights.boneWeight5) +
                    (rigSampler.TrackingGroup[boneWeights.boneIndex6] * boneWeights.boneWeight6) +
                    (rigSampler.TrackingGroup[boneWeights.boneIndex7] * boneWeights.boneWeight7);
            }

            return float4x4.identity;
        }

        public override float3 GetVertexInWorld(int lod, int vertexIndex, out float4x4 skinningMatrix, out float3 localDelta)
        {
            skinningMatrix = float4x4.identity;
            localDelta = default;

            var subData = SubData;

            if (!subData.TryGetVertices(lod, out var vertexArray)) return default; 

            if (!PrepInWorldDataFetch(lod, vertexIndex, out int topVertexIndex, out MuscleData muscleData, out float flexFactor, out MeshVertexDelta delta, out MeshShape flexShape, out skinningMatrix)) return default;

            localDelta = delta.positionDelta;
            localDelta = flexShape.BlendShape.GetTransformedVertex(localDelta, topVertexIndex, flexFactor, 1f);

            return math.transform(skinningMatrix, vertexArray[vertexIndex] + localDelta);
        }

        public override float3 GetNormalInWorld(int lod, int vertexIndex, out float4x4 skinningMatrix, out float3 localDelta)
        {
            skinningMatrix = float4x4.identity;
            localDelta = default;

            var subData = SubData;

            if (!subData.TryGetNormals(lod, out var normalsArray)) return default;

            if (!PrepInWorldDataFetch(lod, vertexIndex, out int topVertexIndex, out MuscleData muscleData, out float flexFactor, out MeshVertexDelta delta, out MeshShape flexShape, out skinningMatrix)) return default;

            localDelta = delta.normalDelta;
            localDelta = flexShape.BlendShape.GetTransformedNormal(localDelta, topVertexIndex, flexFactor, 1f);

            return math.normalize(math.rotate(skinningMatrix, normalsArray[vertexIndex] + localDelta)); 
        }

        public override float4 GetTangentInWorld(int lod, int vertexIndex, out float4x4 skinningMatrix, out float3 localDelta)
        {
            skinningMatrix = float4x4.identity;
            localDelta = default;

            var subData = SubData;

            if (!subData.TryGetTangents(lod, out var tangentsArray)) return default;

            if (!PrepInWorldDataFetch(lod, vertexIndex, out int topVertexIndex, out MuscleData muscleData, out float flexFactor, out MeshVertexDelta delta, out MeshShape flexShape, out skinningMatrix)) return default;

            localDelta = delta.tangentDelta;
            localDelta = ((float4)flexShape.BlendShape.GetTransformedTangent(new Vector4(localDelta.x, localDelta.y, localDelta.z, 0f), topVertexIndex, flexFactor, 1f)).xyz;

            var tangent = tangentsArray[vertexIndex];
            tangent.xyz = math.normalize(math.rotate(skinningMatrix, tangent.xyz + localDelta)); 
            return tangent;
        }

        public override void GetVertexInWorld(int lod, int vertexIndex, out float3 pos, out float3 normal, out float4 tangent, out float4x4 skinningMatrix, out float3 localDeltaPos, out float3 localDeltaNorm, out float3 localDeltaTan)
        {
            skinningMatrix = float4x4.identity;
            localDeltaPos = default;
            localDeltaNorm = default;
            localDeltaTan = default;

            pos = default;
            normal = default;
            tangent = default;

            var subData = SubData;

            if (!subData.TryGetVertices(lod, out var vertexArray) || !subData.TryGetNormals(lod, out var normalsArray) || !subData.TryGetTangents(lod, out var tangentsArray)) return;

            if (!PrepInWorldDataFetch(lod, vertexIndex, out int topVertexIndex, out MuscleData muscleData, out float flexFactor, out MeshVertexDelta delta, out MeshShape flexShape, out skinningMatrix)) return;

            localDeltaPos = delta.positionDelta;
            localDeltaPos = flexShape.BlendShape.GetTransformedVertex(localDeltaPos, topVertexIndex, flexFactor, 1f);

            localDeltaNorm = delta.normalDelta;
            localDeltaNorm = flexShape.BlendShape.GetTransformedNormal(localDeltaNorm, topVertexIndex, flexFactor, 1f); 

            localDeltaTan = delta.tangentDelta;
            localDeltaTan = ((float4)flexShape.BlendShape.GetTransformedTangent(new Vector4(localDeltaTan.x, localDeltaTan.y, localDeltaTan.z, 0f), topVertexIndex, flexFactor, 1f)).xyz;

            pos = math.transform(skinningMatrix, vertexArray[vertexIndex] + localDeltaPos);

            normal = math.normalize(math.rotate(skinningMatrix, normalsArray[vertexIndex] + localDeltaNorm));

            tangent = tangentsArray[vertexIndex];
            tangent.xyz = math.normalize(math.rotate(skinningMatrix, tangent.xyz + localDeltaTan));
        }

        public override List<float3> GetMuscleGroupsAffecting(int lod, int vertexIndex, List<float3> list = null)
        {
            if (list == null) list = new List<float3>();

            var subData = SubData;
            if (subData.muscleGroups.y >= subData.muscleGroups.x)
            {
                int topVertexIndex = vertexIndex;
                if (lod > 0 && TryGetUV(lod, subData.nearestVertexUVChannel, out var uvArray))
                {
                    topVertexIndex = MorphUtils.FetchIndexFromUV(subData.nearestVertexIndexElement, uvArray[vertexIndex]);
                }

                float midlineWeight = subData.precache_vertexGroups[(subData.midlineVertexGroup * subData.vertexCount) + topVertexIndex];
                float2 weightLeftRight = math.lerp(math.select(new float2(1f, 0f), new float2(0f, 1f), subData.leftRightFlags[topVertexIndex]), new float2(0.5f, 0.5f), midlineWeight);
                for (int i = subData.muscleGroups.x; i <= subData.muscleGroups.y; i++)
                {
                    int groupIndex = i - subData.muscleGroups.x;

                    var vg = subData.GetVertexGroup(i); 
                    float weight = vg.GetWeight(topVertexIndex);
                    if (weight > 0f) list.Add(new float3(groupIndex, weight * weightLeftRight.x, weight * weightLeftRight.y));
                }
            }

            return list;
        }
        public override List<float3> GetFatGroupsAffecting(int lod, int vertexIndex, List<float3> list = null)
        {
            if (list == null) list = new List<float3>();

            var subData = SubData;
            if (subData.fatGroups.y >= subData.fatGroups.x)
            {
                int topVertexIndex = vertexIndex;
                if (lod > 0 && TryGetUV(lod, subData.nearestVertexUVChannel, out var uvArray))
                {
                    topVertexIndex = MorphUtils.FetchIndexFromUV(subData.nearestVertexIndexElement, uvArray[vertexIndex]);
                }

                float midlineWeight = subData.precache_vertexGroups[(subData.midlineVertexGroup * subData.vertexCount) + topVertexIndex];
                float2 weightLeftRight = math.lerp(math.select(new float2(1f, 0f), new float2(0f, 1f), subData.leftRightFlags[topVertexIndex]), new float2(0.5f, 0.5f), midlineWeight);
                for (int i = subData.fatGroups.x; i <= subData.fatGroups.y; i++)
                {
                    int groupIndex = i - subData.fatGroups.x;

                    var vg = subData.GetVertexGroup(i);
                    float weight = vg.GetWeight(topVertexIndex);
                    if (weight > 0f) list.Add(new float3(groupIndex, weight * weightLeftRight.x, weight * weightLeftRight.y)); 
                }
            }

            return list;
        }
        public override List<float3> GetVariationGroupsAffecting(int lod, int vertexIndex, List<float3> list = null)
        {
            if (list == null) list = new List<float3>();

            var subData = SubData;
            if (subData.variationGroups.y >= subData.variationGroups.x)
            {
                int topVertexIndex = vertexIndex;
                if (lod > 0 && TryGetUV(lod, subData.nearestVertexUVChannel, out var uvArray))
                {
                    topVertexIndex = MorphUtils.FetchIndexFromUV(subData.nearestVertexIndexElement, uvArray[vertexIndex]);
                }

                float midlineWeight = subData.precache_vertexGroups[(subData.midlineVertexGroup * subData.vertexCount) + topVertexIndex];
                float2 weightLeftRight = math.lerp(math.select(new float2(1f, 0f), new float2(0f, 1f), subData.leftRightFlags[topVertexIndex]), new float2(0.5f, 0.5f), midlineWeight);
                for (int i = subData.variationGroups.x; i <= subData.variationGroups.y; i++) 
                {
                    int groupIndex = i - subData.variationGroups.x;

                    var vg = subData.GetVertexGroup(i);
                    float weight = vg.GetWeight(topVertexIndex);
                    if (weight > 0f) list.Add(new float3(groupIndex, weight * weightLeftRight.x, weight * weightLeftRight.y)); 
                }
            }

            return list;
        }

        public VertexGroup GetVertexGroup(int index)
        {
            if (SubData == null) return null;
            return data.SerializedData.GetVertexGroup(index);
        }

        public VertexGroup GetStandaloneVertexGroup(int index)
        {
            if (SubData == null) return null;
            return data.SerializedData.GetStandaloneVertexGroup(index);
        }

        #endregion

        #region Material Handling

        public override Material[] MaterialInstances
        {
            get
            {
                if (instance != null) return instance.Materials;

                return null;
            }
        }

        #endregion

        #region Rendering

        public override bool CanRender => instance != null && instance.IsValid;

        protected override Renderer[] CreateRenderersForLOD(int lod, MeshLOD meshLOD, Transform renderersRoot, DefaultRenderedMesh defaultRenderedMesh)
        {          
            var meshData = SubData;
            var bounds = new Bounds(meshData.boundsCenter, meshData.boundsExtents * 2f);

            Renderer[] renderers = null;
            if (meshData.renderSets == null || meshData.renderSets.Length <= 1)
            {
                defaultRenderedMesh.meshFilter = renderersRoot.gameObject.AddComponent<MeshFilter>();
                defaultRenderedMesh.meshFilter.sharedMesh = meshLOD.mesh;
                defaultRenderedMesh.meshRenderer = renderersRoot.gameObject.AddComponent<MeshRenderer>();
                defaultRenderedMesh.meshRenderer.sharedMaterials = MaterialInstances;
                defaultRenderedMesh.meshRenderer.localBounds = bounds;

                renderers = new Renderer[] { defaultRenderedMesh.meshRenderer };
            }
            else // render sets render the same mesh instance using different materials (useful for toon outline materials)
            {
                var materials = MaterialInstances;

                renderers = new Renderer[meshData.renderSets.Length];

                MeshFilter[] additionalFilters = new MeshFilter[meshData.renderSets.Length - 1];
                MeshRenderer[] additionalRenderers = new MeshRenderer[additionalFilters.Length];
                for (int b = 0; b < meshData.renderSets.Length; b++)
                {
                    var renderSet = meshData.renderSets[b];

                    var renderSetObj = new GameObject($"set_{b}");
                    renderSetObj.layer = gameObject.layer;
                    renderSetObj.transform.SetParent(renderersRoot.transform, false);

                    MeshFilter meshFilter;
                    MeshRenderer meshRenderer;
                    if (b == 0)
                    {
                        defaultRenderedMesh.meshFilter = meshFilter = renderSetObj.AddComponent<MeshFilter>();
                        defaultRenderedMesh.meshRenderer = meshRenderer = renderSetObj.AddComponent<MeshRenderer>();
                    }
                    else
                    {
                        int ind = b - 1;

                        additionalFilters[ind] = meshFilter = renderSetObj.AddComponent<MeshFilter>();
                        additionalRenderers[ind] = meshRenderer = renderSetObj.AddComponent<MeshRenderer>();
                    }

                    meshFilter.sharedMesh = meshLOD.mesh;
                    var mats = new Material[renderSet.materialCount];
                    for (int c = 0; c < renderSet.materialCount; c++) mats[c] = materials[renderSet.materialIndexStart + c];
                    meshRenderer.sharedMaterials = mats;
                    meshRenderer.localBounds = bounds;

                    renderers[b] = meshRenderer;
                }

                defaultRenderedMesh.additionalFilters = additionalFilters;
                defaultRenderedMesh.additionalRenderers = additionalRenderers;
            }

            return renderers;
        }

        protected override void StopRendering()
        {
            base.StopRendering();
            if (instance != null) Updater.Unregister(ref instance);
        }

        public override void SetVisible(bool visible)
        {
        }

        public override void StartRenderingInstance()
        {
        }
        public override void StopRenderingInstance()
        {
        }

        #endregion

        #region Activation

        protected override void OnEnable()
        {
            base.OnEnable();
        }
        protected override void OnDisable()
        {
            base.OnDisable();
        }

        #endregion

        #region Customization Shapes & Groups

        public override void MarkForPhysiqueUpdate()
        {
            if (instance == null || !instance.IsValid) return;
            instance.MarkForPhysiqueUpdateUnsafe();
        }

        public override void MarkForVariationUpdate()
        {
            if (instance == null || !instance.IsValid) return;
            instance.MarkForVariationUpdateUnsafe();
        }

        public override JobHandle MeshUpdateJobHandle => instance == null ? default : instance.OwnerGroup.ActiveJob;

        protected override void OnSetMuscleData(int groupIndex, MuscleDataLR data)
        {
            if (instance != null && instance.IsValid)
            {
                var prevData = instance.GetMuscleGroupWeightUnsafe(groupIndex);
                if (prevData.x != data.valuesLeft.mass || prevData.y != data.valuesRight.mass)
                {
                    instance.SetMuscleGroupWeightUnsafe(groupIndex, new float2(data.valuesLeft.mass, data.valuesRight.mass));
                    instance.MarkForPhysiqueUpdateUnsafe();
                }

                if (prevData.x != data.valuesLeft.mass) NotifyDefaultMuscleGroupListeners(groupIndex * 2);
                if (prevData.y != data.valuesLeft.mass) NotifyDefaultMuscleGroupListeners((groupIndex * 2) + 1);
            }
        }

        protected override void OnSetFatLevel(int groupIndex, float level)
        {
            if (instance != null && instance.IsValid)
            {
                var prevData = instance.GetFatGroupWeightUnsafe(groupIndex);
                if (prevData.x != level || prevData.y != level)
                {
                    instance.SetFatGroupWeightUnsafe(groupIndex, level);
                    instance.MarkForPhysiqueUpdateUnsafe();
                }
            }
        }

        protected override void OnSetVariationWeight(int variationShapeIndex, int groupIndex, float2 weight)
        {
            if (instance != null && instance.IsValid)
            {
                var prevData = instance.GetVariationGroupWeightUnsafe(variationShapeIndex, groupIndex);
                if (prevData.x != weight.x || prevData.y != weight.y)
                {
                    instance.SetVariationGroupWeightUnsafe(variationShapeIndex, groupIndex, weight);
                    instance.MarkForVariationUpdateUnsafe(); 
                } 
            }
        }

        #endregion

        #region IDs

        #endregion

        [SerializeField]
        protected bool updateMeshManually;
        public bool UpdateMeshManually
        {
            get => updateMeshManually;
            set
            {
                updateMeshManually = value;
                if (instance != null) instance.updateManually = updateMeshManually;
            }
        }

        #region Buffers

        public override bool TryGetInstanceBuffer<T>(string matPropName, out InstanceBuffer<T> buffer)
        {
            if (instance == null || !instance.IsValid)
            {
                buffer = null;
                return false;
            }

            return instance.OwnerGroup.TryGetInstanceBuffer(matPropName, out buffer); 
        }

        public override int CreateInstanceMaterialBuffer<T>(string propertyName, ICollection<int> materialSlots, int elementsPerInstance, int bufferPoolSize, bool autoApplyToMaterials, out InstanceBuffer<T> buffer)
        {
            if (instance == null || !instance.IsValid)
            {
                buffer = null;
                return -1;
            }

            return instance.OwnerGroup.CreateInstanceMaterialBuffer(propertyName, materialSlots, elementsPerInstance, bufferPoolSize, autoApplyToMaterials, out buffer);
        }

        #endregion

        #region Sampling

        protected override bool PreRaycastAgainst(ref int lod, ref float3 origin, ref float3 offset) => true;
        protected override bool RaycastAgainstMesh(int lod, float3 origin, float3 offset, out Maths.RaycastHitResult result, float errorMargin = 0.01f)
        {
            result = default;

            if (!Data.SerializedData.TryGetVertices(lod, out var vertices)) return false;
            if (!Data.SerializedData.TryGetTriangles(lod, out var triangles)) return false;
            NativeArray<float4> indexUVs = default;
            if (lod > 0 && !Data.SerializedData.TryGetUV(lod, Data.SerializedData.nearestVertexUVChannel, out indexUVs)) return false; 

            instance.UpdateIfDirty(false, true);
            instance.OwnerGroup.ActiveJob.Complete(); 

            var rigSampler = RigSampler;
            RaycastResult finalResult = default; 
            using (var resultQueue = new NativeQueue<RaycastResult>(Allocator.TempJob))
            {
                using (var skinningMatrices = new NativeArray<float4x4>(SkinningBoneCount, Allocator.TempJob))
                {
                    if (rigSampler != null) 
                    { 
                        rigSampler.TrackingGroup.CopyIntoArray(skinningMatrices, 0);
                        for (int z = 0; z < SkinningBoneCount; z++)
                        {
                            var m = skinningMatrices[z];
                            Debug.DrawRay(math.transform(m, float3.zero), math.rotate(m, new float3(0f, 1f, 0f)) * 0.35f, Color.blue, 0.5f); 
                        }
                    }

                    var handle = instance.OwnerGroup.ActiveJob;

                    if (lod > 0)
                    {
                        handle = new RaycastMeshWithIndexUVJob()
                        {

                            deltasStartIndex = DeltasStartIndex,

                            indexChannel = Data.SerializedData.nearestVertexIndexElement,

                            errorMargin = errorMargin,
                            origin = origin,
                            offset = offset,

                            vertices = vertices,
                            triangles = triangles,
                            indexUVs = indexUVs,

                            deltas = instance.OwnerGroup.FinalVertexDeltas.AsArray(),

                            boneWeights = Data.SerializedData.BaseBoneWeightsJob,
                            skinningMatrices = skinningMatrices,

                            results = resultQueue.AsParallelWriter()

                        }.Schedule(triangles.Length / 3, 1, handle);
                    } 
                    else
                    {
                        handle = new RaycastMeshJob()
                        {

                            deltasStartIndex = DeltasStartIndex,

                            indexChannel = Data.SerializedData.nearestVertexIndexElement,

                            errorMargin = errorMargin,
                            origin = origin,
                            offset = offset,

                            vertices = vertices,
                            triangles = triangles,

                            deltas = instance.OwnerGroup.FinalVertexDeltas.AsArray(),

                            boneWeights = Data.SerializedData.BaseBoneWeightsJob,
                            skinningMatrices = skinningMatrices,

                            results = resultQueue.AsParallelWriter()

                        }.Schedule(triangles.Length / 3, 1, handle);
                    }
                    
                    using (var finalResultArray = new NativeArray<RaycastResult>(1, Allocator.TempJob))
                    {
                        handle = new ClosestRaycastHitFinalJob()
                        {
                            outputs = resultQueue,
                            finalOutput = finalResultArray
                        }.Schedule(handle);

                        handle.Complete();
                        finalResult = finalResultArray[0];
                    }
                }
            }

            result = finalResult.hitInfo;
            return finalResult.didHit;
        }

        #endregion

    }

}

#endif