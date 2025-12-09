#if (UNITY_STANDALONE || UNITY_EDITOR)

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

using static Swole.ICustomizableCharacter.Defaults;

namespace Swole.Morphing
{

    public class CustomizableCharacterMeshV2 : InstanceableSkinnedMeshBase, ICustomizableCharacter
    {

        #region Editor

        public void UpdateInEditor()
        {
#if UNITY_EDITOR
            if (Application.isPlaying && isActiveAndEnabled)
            {
                var meshData = SubData;

                if (prevBustSizeEditor != bustSizeEditor)
                {
                    prevBustSizeEditor = bustSizeEditor;
                    SetBustSize(bustSizeEditor);
                }
                if (prevShapeWeightsEditor == null || prevShapeWeightsEditor.Length == 0)
                {
                    prevShapeWeightsEditor = new NamedFloat[meshData.StandaloneShapesCount];
                    for (int a = 0; a < prevShapeWeightsEditor.Length; a++) prevShapeWeightsEditor[a] = new NamedFloat() { name = meshData.GetStandaloneShape(a).name };
                }
                if (shapeWeightsEditor == null || shapeWeightsEditor.Length == 0)
                {
                    shapeWeightsEditor = new NamedFloat[meshData.StandaloneShapesCount];
                    for (int a = 0; a < shapeWeightsEditor.Length; a++) shapeWeightsEditor[a] = new NamedFloat() { name = meshData.GetStandaloneShape(a).name };
                }

                if (prevMuscleWeightsEditor == null || prevMuscleWeightsEditor.Length == 0)
                {
                    prevMuscleWeightsEditor = new NamedMuscleData[meshData.MuscleVertexGroupCount];
                    for (int a = 0; a < prevMuscleWeightsEditor.Length; a++) prevMuscleWeightsEditor[a] = new NamedMuscleData() { name = meshData.GetVertexGroup(a + meshData.muscleGroups.x).name };
                }
                if (muscleWeightsEditor == null || muscleWeightsEditor.Length == 0)
                {
                    muscleWeightsEditor = new NamedMuscleData[meshData.MuscleVertexGroupCount];
                    for (int a = 0; a < muscleWeightsEditor.Length; a++) muscleWeightsEditor[a] = new NamedMuscleData() { name = meshData.GetVertexGroup(a + meshData.muscleGroups.x).name };
                }

                if (prevFatWeightsEditor == null || prevFatWeightsEditor.Length == 0)
                {
                    prevFatWeightsEditor = new NamedFloat[meshData.FatVertexGroupCount];
                    for (int a = 0; a < prevFatWeightsEditor.Length; a++) prevFatWeightsEditor[a] = new NamedFloat() { name = meshData.GetVertexGroup(a + meshData.fatGroups.x).name };
                }
                if (fatWeightsEditor == null || fatWeightsEditor.Length == 0)
                {
                    fatWeightsEditor = new NamedFloat[meshData.FatVertexGroupCount];
                    for (int a = 0; a < fatWeightsEditor.Length; a++) fatWeightsEditor[a] = new NamedFloat() { name = meshData.GetVertexGroup(a + meshData.fatGroups.x).name };
                }

                if (prevVariationWeightsEditor == null || prevVariationWeightsEditor.Length == 0)
                {
                    prevVariationWeightsEditor = new NamedFloat2[VariationShapesControlDataSize];
                    for (int a = 0; a < prevVariationWeightsEditor.Length; a++)
                    {
                        int groupIndex = a / meshData.VariationShapesCount;
                        int shapeIndex = a % meshData.VariationShapesCount;
                        prevVariationWeightsEditor[a] = new NamedFloat2() { name = meshData.GetVertexGroup(a + meshData.variationGroups.x).name + "_" + meshData.GetVariationShape(shapeIndex).name };
                    }
                }
                if (variationWeightsEditor == null || variationWeightsEditor.Length == 0)
                {
                    variationWeightsEditor = new NamedFloat2[VariationShapesControlDataSize];
                    for (int a = 0; a < variationWeightsEditor.Length; a++)
                    {
                        int groupIndex = a / meshData.VariationShapesCount;
                        int shapeIndex = a % meshData.VariationShapesCount;
                        variationWeightsEditor[a] = new NamedFloat2() { name = meshData.GetVertexGroup(a + meshData.variationGroups.x).name + "_" + meshData.GetVariationShape(shapeIndex).name };
                    }
                }

                for (int a = 0; a < shapeWeightsEditor.Length; a++)
                {
                    if (prevShapeWeightsEditor[a].value != shapeWeightsEditor[a].value)
                    {
                        SetStandaloneShapeWeightUnsafe(a, shapeWeightsEditor[a].value);
                        prevShapeWeightsEditor[a].value = shapeWeightsEditor[a].value;
                    }
                }

                if (prevGlobalMass != globalMass)
                {
                    prevGlobalMass = globalMass;
                    for (int a = 0; a < muscleWeightsEditor.Length; a++)
                    {
                        var values = muscleWeightsEditor[a].value;
                        var vl = values.valuesLeft;
                        var vr = values.valuesRight;
                        vl.mass = globalMass;
                        vr.mass = globalMass;
                        values.valuesLeft = vl;
                        values.valuesRight = vr;

                        SetMuscleDataUnsafe(a, values);

                        prevMuscleWeightsEditor[a].value = muscleWeightsEditor[a].value = values;
                    }
                }
                if (prevGlobalFlex != globalFlex)
                {
                    prevGlobalFlex = globalFlex;
                    for (int a = 0; a < muscleWeightsEditor.Length; a++)
                    {
                        var values = muscleWeightsEditor[a].value;
                        var vl = values.valuesLeft;
                        var vr = values.valuesRight;
                        vl.flex = globalFlex;
                        vr.flex = globalFlex;
                        values.valuesLeft = vl;
                        values.valuesRight = vr;

                        SetMuscleDataUnsafe(a, values);

                        prevMuscleWeightsEditor[a].value = muscleWeightsEditor[a].value = values;
                    }
                }
                if (prevGlobalPump != globalPump)
                {
                    prevGlobalPump = globalPump;
                    for (int a = 0; a < muscleWeightsEditor.Length; a++)
                    {
                        var values = muscleWeightsEditor[a].value;
                        var vl = values.valuesLeft;
                        var vr = values.valuesRight;
                        vl.pump = globalPump;
                        vr.pump = globalPump;
                        values.valuesLeft = vl;
                        values.valuesRight = vr;

                        SetMuscleDataUnsafe(a, values);

                        prevMuscleWeightsEditor[a].value = muscleWeightsEditor[a].value = values;
                    }
                }

                if (prevGlobalFat != globalFat)
                {
                    prevGlobalFat = globalFat;
                    for (int a = 0; a < fatWeightsEditor.Length; a++)
                    {
                        SetFatLevelUnsafe(a, globalFat);

                        prevFatWeightsEditor[a].value = fatWeightsEditor[a].value = globalFat;
                    }
                }

                for (int a = 0; a < muscleWeightsEditor.Length; a++)
                {
                    if (prevMuscleWeightsEditor[a].value != muscleWeightsEditor[a].value)
                    {
                        SetMuscleDataUnsafe(a, muscleWeightsEditor[a].value);
                        prevMuscleWeightsEditor[a].value = muscleWeightsEditor[a].value;
                    }
                }

                for (int a = 0; a < fatWeightsEditor.Length; a++)
                {
                    if (prevFatWeightsEditor[a].value != fatWeightsEditor[a].value)
                    {
                        SetFatLevelUnsafe(a, fatWeightsEditor[a].value);
                        prevFatWeightsEditor[a].value = fatWeightsEditor[a].value;
                    }
                }

                for (int a = 0; a < variationWeightsEditor.Length; a++)
                {
                    if (math.any(prevVariationWeightsEditor[a].value != variationWeightsEditor[a].value))
                    {
                        SetVariationWeightUnsafe(a, variationWeightsEditor[a].value);
                        prevVariationWeightsEditor[a].value = variationWeightsEditor[a].value;
                    }
                }
            }
#endif
        }

        public bool debug;
        public string configSaveDir;
        public string configAssetName;
        public bool saveConfig;

        //public EditorCharacterCustomizationConfig editorCustomizationConfig;

        /*public void LoadEditorConfig(EditorCharacterCustomizationConfig config)
        {
            if (config != null)
            {
                UpdateInEditor();
                //config.Apply(this);
            }
        }*/

#if UNITY_EDITOR

        public void OnValidate()
        {
            UpdateInEditor();

            if (saveConfig)
            {
                saveConfig = false;
                SaveNewEditorConfig();
            }
        }

        public void SaveNewEditorConfig() => SaveNewEditorConfig(configSaveDir, configAssetName);
        public void SaveNewEditorConfig(string configSaveDir) => SaveNewEditorConfig(configSaveDir, configAssetName);
        public void SaveNewEditorConfig(string configSaveDir, string configAssetName)
        {
            //editorCustomizationConfig = EditorCharacterCustomizationConfig.CreateAndSave(configSaveDir, configAssetName, this);
        }

#endif

        [Serializable]
        public struct NamedFloat
        {
            public string name;
            public float value;
        }
        [Serializable]
        public struct NamedFloat2
        {
            public string name;
            public float2 value;
        }
        [Serializable]
        public struct NamedMuscleData
        {
            public string name;
            public MuscleDataLR value;
        }

        private float prevGlobalMass;
#if UNITY_EDITOR
        [SerializeField]
#endif
        [Range(0f, 3f)]
        private float globalMass;

        private float prevGlobalFlex;
#if UNITY_EDITOR
        [SerializeField]
#endif
        [Range(0f, 2f)]
        private float globalFlex;

        private float prevGlobalPump;
#if UNITY_EDITOR
        [SerializeField]
#endif
        [Range(0f, 1f)]
        private float globalPump; 

        private float prevGlobalFat;
#if UNITY_EDITOR
        [SerializeField]
#endif
        [Range(0f, 1f)]
        private float globalFat;

        private float prevBustSizeEditor;
#if !UNITY_EDITOR
        [NonSerialized]
#endif
        [Range(0, 2)]
        public float bustSizeEditor;

#if !UNITY_EDITOR
        [NonSerialized]
#endif
        [HideInInspector]
        public NamedFloat[] prevShapeWeightsEditor;
#if !UNITY_EDITOR
        [NonSerialized]
#endif
        public NamedFloat[] shapeWeightsEditor;

#if !UNITY_EDITOR
        [NonSerialized]
#endif
        [HideInInspector]
        public NamedMuscleData[] prevMuscleWeightsEditor;
#if !UNITY_EDITOR
        [NonSerialized]
#endif
        public NamedMuscleData[] muscleWeightsEditor;

#if !UNITY_EDITOR
        [NonSerialized]
#endif
        [HideInInspector]
        public NamedFloat[] prevFatWeightsEditor;
#if !UNITY_EDITOR
        [NonSerialized]
#endif
        public NamedFloat[] fatWeightsEditor;

#if !UNITY_EDITOR
        [NonSerialized]
#endif
        [HideInInspector]
        public NamedFloat2[] prevVariationWeightsEditor;
#if !UNITY_EDITOR
        [NonSerialized]
#endif
        public NamedFloat2[] variationWeightsEditor;

        #endregion

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
                    meshGroup = new MeshGroupV2(data.SerializedData);
                    singleton.meshGroups[data.GetInstanceID()] = meshGroup;
                }

                return meshGroup.ClaimNewInstance();
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

        protected class InstanceV2 : IDisposable
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

            public void MarkForPhysiqueUpdateUnsafe()
            {
                ownerGroup.MarkForPhysiqueUpdate(localID);
            }

            public void MarkForVariationUpdateUnsafe()
            {
                ownerGroup.MarkForVariationUpdate(localID);
            }

            public void SetMuscleGroupWeightUnsafe(int groupIndex, float2 massWeight)
            {
                ownerGroup.SetMuscleGroupWeightUnsafe(localID, groupIndex, massWeight);
            }

            public void SetFatGroupWeightUnsafe(int groupIndex, float fatWeight)
            {
                ownerGroup.SetFatGroupWeightUnsafe(localID, groupIndex, fatWeight);
            }

            public void SetVariationGroupWeightUnsafe(int groupIndex, float2 variationWeight)
            {
                ownerGroup.SetVariationGroupWeightUnsafe(localID, groupIndex, variationWeight);
            }

        }

        /// <summary>
        /// Handles realtime updates of meshes that use the same serialized data.
        /// </summary>
        protected class MeshGroupV2 : IDisposable
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
                    if (variationGroupVertexDeltasLR.IsCreated)
                    {
                        variationGroupVertexDeltasLR.Dispose();
                        variationGroupVertexDeltasLR = default;
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

                        material.SetBuffer(data.SkinningDataPropertyName, data.BoneWeightsBuffer);
                        material.SetInteger(data.BoneCountPropertyName, data.BoneCount);

                        material.SetVector(data.MuscleVertexGroupsBufferRangePropertyName, new Vector4(data.muscleGroups.x, data.muscleGroups.y, 0f, 0f));
                        material.SetVector(data.FatVertexGroupsBufferRangePropertyName, new Vector4(data.fatGroups.x, data.fatGroups.y, 0f, 0f));
                        material.SetVector(data.VariationVertexGroupsBufferRangePropertyName, new Vector4(data.variationGroups.x, data.variationGroups.y, 0f, 0f));

                        material.SetBuffer(data.MuscleGroupInfluencesPropertyName, data.MuscleGroupInfluencesBuffer);
                        material.SetBuffer(data.FatGroupInfluencesPropertyName, data.FatGroupInfluencesBuffer);

                        material.SetInteger(data.MuscleMassShapeIndexPropertyName, data.massShape);
                        material.SetInteger(data.FlexShapeIndexPropertyName, data.flexShape);
                        material.SetInteger(data.FatShapeIndexPropertyName, data.fatShape);

                        material.SetFloat(data.DefaultShapeMuscleWeightPropertyName, data.defaultMassShapeWeight);

                        material.SetInteger(data.MidlineVertexGroupIndexPropertyName, data.midlineVertexGroup);
                        material.SetInteger(data.BustVertexGroupIndexPropertyName, data.bustVertexGroup);
                        material.SetInteger(data.BustNerfVertexGroupIndexPropertyName, data.bustNerfVertexGroup);
                        material.SetInteger(data.NippleMaskVertexGroupIndexPropertyName, data.nippleMaskVertexGroup);
                        material.SetInteger(data.genitalMaskVertexGroup, data.genitalMaskVertexGroup);

                        material.SetBuffer(data.VertexGroupsPropertyName, data.VertexGroupsBuffer);
                        material.SetBuffer(data.MeshShapeFrameDeltasPropertyName, data.MeshShapeFrameDeltasBuffer);
                        material.SetBuffer(data.MeshShapeFrameWeightsPropertyName, data.MeshShapeFrameWeightsBuffer);
                        material.SetBuffer(data.MeshShapeIndicesPropertyName, data.MeshShapeIndicesBuffer);
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

            private NativeList<GroupControlWeight2> muscleGroupControlWeights;
            private NativeArray<GroupVertexControlWeight> muscleGroupVertexWeights;
            private NativeArray<GroupControlWeight2> blankMuscleGroupControlWeights;
            private NativeList<MeshVertexDeltaLR> muscleGroupVertexDeltasLR;

            public void SetMuscleGroupWeightUnsafe(int instanceIndex, int groupIndex, float2 massWeight)
            {
                int index = (instanceIndex * data.MuscleGroupsCount) + groupIndex;

                var val = muscleGroupControlWeights[index];
                val.weight = math.max(massWeight, data.minMassShapeWeight);
                muscleGroupControlWeights[index] = val;
            }

            private NativeList<GroupControlWeight2> fatGroupControlWeights;
            private NativeArray<GroupVertexControlWeight> fatGroupVertexWeights;
            private NativeArray<GroupControlWeight2> blankFatGroupControlWeights;
            private NativeList<MeshVertexDeltaLR> fatGroupVertexDeltasLR;
            private NativeList<float4> fatGroupVertexDataLR;
            private NativeList<float2> fatValuesPerVertex;

            public void SetFatGroupWeightUnsafe(int instanceIndex, int groupIndex, float fatWeight)
            {
                int index = (instanceIndex * data.FatGroupsCount) + groupIndex;

                var val = fatGroupControlWeights[index];
                var weight = val.weight;
                weight.x = fatWeight;
                val.weight = weight;
                fatGroupControlWeights[index] = val;
            }

            private NativeList<GroupControlWeight2> variationGroupControlWeights;
            private NativeArray<GroupVertexControlWeight> variationGroupVertexWeights;
            private NativeArray<GroupControlWeight2> blankVariationGroupControlWeights;
            private NativeList<MeshVertexDeltaLR> variationGroupVertexDeltasLR;

            public void SetVariationGroupWeightUnsafe(int instanceIndex, int groupIndex, float2 variationWeight)
            {
                int index = (instanceIndex * data.VariationGroupsCount) + groupIndex;

                var val = variationGroupControlWeights[index];
                val.weight = variationWeight;
                variationGroupControlWeights[index] = val;
            }

            private NativeList<MeshVertexDelta> finalVertexDeltas;

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
                            Debug.Log($"Wrote to deltas buffer {finalVertexDeltas.Length}");
                        }
                    }
                }

                indicesToPhysiqueUpdate.Clear();
                indicesToVariationUpdate.Clear();
                foreach (var index in indicesToPhysiqueUpdateNext) indicesToPhysiqueUpdate.Add(index);
                foreach (var index in indicesToVariationUpdateNext) indicesToVariationUpdateNext.Add(index);
                indicesToPhysiqueUpdateNext.Clear();
                indicesToVariationUpdateNext.Clear();
            }
            public void BeginNewJob()
            {
                hasActiveJob = false;
                activeJob.Complete();
                activeJob = default;

                insertionFlags.Clear();
                insertionFlags.AddReplicate(false, MaxInstanceCount);

                indicesToUpdate.Clear();

                int midlineVertexGroupPreMul = data.midlineVertexGroup * data.vertexCount;

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

                        vertexDeltas = fatGroupVertexDeltasLR,
                        fatData = fatGroupVertexDataLR
                    }.Schedule(fatGroupVertexWeights.Length * indicesToPhysiqueUpdate.Length, 1, default);

                    var fatResetHandle = new ResetFatDataJob()
                    {
                        finalFatData = fatValuesPerVertex,
                        meshIndicesToUpdate = indicesToPhysiqueUpdate,
                        vertexCount = data.vertexCount
                    }.Schedule(indicesToPhysiqueUpdate.Length * data.vertexCount, 64, default);

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

                    physiqueUpdateHandle = new UpdateMeshMuscleVertexDeltasJob()
                    {
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

                        fatMuscleBlendShapeIndex = fatMuscleBlendShapeInfo,
                        fatValuesPerVertex = fatValuesPerVertex,

                        vertexDeltas = muscleGroupVertexDeltasLR
                    }.Schedule(muscleGroupVertexWeights.Length * indicesToPhysiqueUpdate.Length, 1, fatHandle);
                }

                JobHandle variationUpdateHandle = default;
                if (indicesToVariationUpdate.Length > 0)
                {

                }

                if (indicesToUpdate.Length > 0)
                {
                    Debug.Log($"Updating {indicesToUpdate.Length} meshes");

                    JobHandle resetHandle = new ResetFinalVertexDeltasJob()
                    {
                        finalVertexDeltas = finalVertexDeltas,
                        meshIndicesToUpdate = indicesToUpdate,
                        vertexCount = data.vertexCount
                    }.Schedule(indicesToUpdate.Length * data.vertexCount, 64, default);

                    JobHandle finalizeHandle = JobHandle.CombineDependencies(resetHandle, JobHandle.CombineDependencies(physiqueUpdateHandle, variationUpdateHandle));
                    if (indicesToPhysiqueUpdate.Length > 0)
                    {
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
                                meshIndicesToUpdate = indicesToPhysiqueUpdate,
                                vertexDeltasLR = fatGroupVertexDeltasLR,
                                midlineVertexGroupIndexPreMul = midlineVertexGroupPreMul,

                                finalVertexDeltas = finalVertexDeltas
                            }.Schedule(groupInfo.vertexCount * indicesToPhysiqueUpdate.Length, 1, finalizeHandle);
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
                                meshIndicesToUpdate = indicesToPhysiqueUpdate,
                                vertexDeltasLR = muscleGroupVertexDeltasLR,
                                midlineVertexGroupIndexPreMul = midlineVertexGroupPreMul,

                                finalVertexDeltas = finalVertexDeltas
                            }.Schedule(groupInfo.vertexCount * indicesToPhysiqueUpdate.Length, 1, finalizeHandle);
                        }
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
            public void Initialize()
            {
                if (IsInitialized || data == null || disposed) return;

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

                meshShapeDeltas = new NativeArray<MeshVertexDelta>(data.MeshShapeDeltasCount, Allocator.Persistent);
                meshShapeInfos = new NativeArray<int2>(data.MeshShapeCount, Allocator.Persistent);
                if (data.meshShapes != null)
                {
                    int ind = 0;
                    int frameInd = 0;
                    for (int i = 0; i < data.meshShapes.Length; i++)
                    {
                        var shape = data.meshShapes[i];

                        if (shape == null) continue;

                        meshShapeInfos[i] = new int2(frameInd, shape.frames == null ? 0 : shape.frames.Length); // x = start index in frame weights buffer, y = frame count

                        if (shape.frames == null) continue;

                        for(int j = 0;  j < shape.frames.Length; j++)
                        {
                            var frame = shape.frames[j];
                            tempFloats.Add(frame.weight);
                            frameInd++;

                            if (frame.deltas == null)
                            {
                                ind += data.vertexCount;
                                continue;
                            }

                            int subCount = Mathf.Min(data.vertexCount, frame.deltas.Length);
                            for(int k = 0; k < subCount; k++)
                            {
                                meshShapeDeltas[ind] = frame.deltas[k];
                                ind++;
                            }

                            ind += data.vertexCount - subCount;

                        }
                    }
                }
                meshShapeFrameWeights = new NativeArray<float>(tempFloats.ToArray(), Allocator.Persistent);

                vertexGroups = new NativeArray<float>(data.VertexGroupCount * data.vertexCount, Allocator.Persistent);
                if (data.vertexGroups != null)
                {
                    for (int i = 0; i < data.vertexGroups.Length; i++)
                    {
                        var group = data.vertexGroups[i];
                        if (group == null) continue;

                        group.InsertIntoNativeArray(vertexGroups, i * data.vertexCount);
                    }
                }

                leftRightFlagBuffer = new NativeArray<bool>(data.leftRightFlags, Allocator.Persistent);


                #region Init Muscle Groups

                tempGroupControlWeights.Clear();
                tempGroupVertexWeights.Clear();
                if (data.muscleGroups.y >= data.muscleGroups.x)
                {
                    for (int g = data.muscleGroups.x; g <= data.muscleGroups.y; g++)
                    {
                        int localGroupIndex = g - data.muscleGroups.x;
                        var vertexGroup = data.vertexGroups[g];
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
                            if (vertexWeight > 1f) Debug.LogError($"Vertex index {vertexIndex} for muscle group {vertexGroup.name} has weight of {vertexWeight}");
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

                blankMuscleGroupControlWeights = new NativeArray<GroupControlWeight2>(tempGroupControlWeights.ToArray(), Allocator.Persistent);
                muscleGroupControlWeights = new NativeList<GroupControlWeight2>(blankMuscleGroupControlWeights.Length * maxInstanceCount, Allocator.Persistent);
                for (int i = 0; i < maxInstanceCount; i++) muscleGroupControlWeights.AddRange(blankMuscleGroupControlWeights);
                muscleGroupVertexWeights = new NativeArray<GroupVertexControlWeight>(tempGroupVertexWeights.ToArray(), Allocator.Persistent);
                int muscleGroupDeltasCount = muscleGroupVertexWeights.Length * maxInstanceCount;
                muscleGroupVertexDeltasLR = new NativeList<MeshVertexDeltaLR>(muscleGroupDeltasCount, Allocator.Persistent);
                if (muscleGroupDeltasCount > 0) muscleGroupVertexDeltasLR.AddReplicate(MeshVertexDeltaLR.Default, muscleGroupDeltasCount);

                #endregion

                #region Init Fat Groups

                tempGroupControlWeights.Clear();
                tempGroupVertexWeights.Clear();
                if (data.fatGroups.y >= data.fatGroups.x)
                {
                    for (int g = data.fatGroups.x; g <= data.fatGroups.y; g++)
                    {
                        int localGroupIndex = g - data.fatGroups.x;
                        var vertexGroup = data.vertexGroups[g];
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

                blankFatGroupControlWeights = new NativeArray<GroupControlWeight2>(tempGroupControlWeights.ToArray(), Allocator.Persistent);
                fatGroupControlWeights = new NativeList<GroupControlWeight2>(blankFatGroupControlWeights.Length * maxInstanceCount, Allocator.Persistent);
                for (int i = 0; i < maxInstanceCount; i++) fatGroupControlWeights.AddRange(blankFatGroupControlWeights);
                fatGroupVertexWeights = new NativeArray<GroupVertexControlWeight>(tempGroupVertexWeights.ToArray(), Allocator.Persistent);
                int fatGroupDeltasCount = fatGroupVertexWeights.Length * maxInstanceCount;
                fatGroupVertexDeltasLR = new NativeList<MeshVertexDeltaLR>(fatGroupDeltasCount, Allocator.Persistent);
                fatGroupVertexDataLR = new NativeList<float4>(fatGroupDeltasCount, Allocator.Persistent);
                if (fatGroupDeltasCount > 0) 
                { 
                    fatGroupVertexDeltasLR.AddReplicate(MeshVertexDeltaLR.Default, fatGroupDeltasCount);
                    fatGroupVertexDataLR.AddReplicate(float4.zero, fatGroupDeltasCount);
                }

                fatValuesPerVertex = new NativeList<float2>(initialVertexCount, Allocator.Persistent);
                fatValuesPerVertex.AddReplicate(float2.zero, initialVertexCount);

                #endregion

                #region Init Variation Groups

                tempGroupControlWeights.Clear();
                tempGroupVertexWeights.Clear();
                if (data.variationGroups.y >= data.variationGroups.x)
                {
                    for (int g = data.variationGroups.x; g <= data.variationGroups.y; g++)
                    {
                        int localGroupIndex = g - data.variationGroups.x;
                        var vertexGroup = data.vertexGroups[g];
                        tempGroupControlWeights.Add(new GroupControlWeight2()
                        {
                            groupIndex = localGroupIndex,
                            vertexCount = vertexGroup.EntryCount,
                            vertexSequenceStartIndex = tempGroupVertexWeights.Count,
                            weight = 0f
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

                blankVariationGroupControlWeights = new NativeArray<GroupControlWeight2>(tempGroupControlWeights.ToArray(), Allocator.Persistent);
                variationGroupControlWeights = new NativeList<GroupControlWeight2>(blankVariationGroupControlWeights.Length * maxInstanceCount, Allocator.Persistent);
                for (int i = 0; i < maxInstanceCount; i++) variationGroupControlWeights.AddRange(blankVariationGroupControlWeights);
                variationGroupVertexWeights = new NativeArray<GroupVertexControlWeight>(tempGroupVertexWeights.ToArray(), Allocator.Persistent);
                int variationGroupDeltasCount = variationGroupVertexWeights.Length * maxInstanceCount;
                variationGroupVertexDeltasLR = new NativeList<MeshVertexDeltaLR>(variationGroupDeltasCount, Allocator.Persistent);
                if (variationGroupDeltasCount > 0) variationGroupVertexDeltasLR.AddReplicate(MeshVertexDeltaLR.Default, variationGroupDeltasCount);

                #endregion

                int initialDeltasCount = data.vertexCount * maxInstanceCount;
                finalVertexDeltas = new NativeList<MeshVertexDelta>(initialDeltasCount, Allocator.Persistent);
                finalVertexDeltas.AddReplicate(MeshVertexDelta.Default, initialDeltasCount);

                finalVertexDeltasBufferIndex = CreateInstanceMaterialBuffer<MeshVertexDelta>(data.PerVertexDeltaDataPropertyName, data.vertexCount, 3, true, out var finalVertexDeltasBuffer);
                EnsureInstanceBufferSize(finalVertexDeltasBuffer);
                Debug.Log($"{MaxInstanceCount} -- {finalVertexDeltasBuffer.InstanceCount}"); 
                finalVertexDeltasBuffer.WriteToBuffer(finalVertexDeltas.AsArray(), 0, 0, finalVertexDeltas.Length); 

                initialized = true;

                TrackDisposables();
            }

            public InstanceV2 ClaimNewInstance()
            {
                if (disposed) return null;

                Initialize();

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

                    muscleGroupControlWeights.AddRange(blankMuscleGroupControlWeights);
                    muscleGroupVertexDeltasLR.AddReplicate(MeshVertexDeltaLR.Default, muscleGroupVertexWeights.Length);

                    fatGroupControlWeights.AddRange(blankFatGroupControlWeights);
                    fatGroupVertexDeltasLR.AddReplicate(MeshVertexDeltaLR.Default, fatGroupVertexWeights.Length);
                    fatGroupVertexDataLR.AddReplicate(float4.zero, fatGroupVertexWeights.Length);

                    fatValuesPerVertex.AddReplicate(float2.zero, data.vertexCount);

                    variationGroupControlWeights.AddRange(blankVariationGroupControlWeights);
                    variationGroupVertexDeltasLR.AddReplicate(MeshVertexDeltaLR.Default, variationGroupVertexWeights.Length);

                    finalVertexDeltas.AddReplicate(MeshVertexDelta.Default, data.vertexCount); // expand the vertex delta buffer

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

        [Serializable, StructLayout(LayoutKind.Sequential)]
        public struct ControlWeight
        {
            public int index;
            public float weight;
        }
        [Serializable, StructLayout(LayoutKind.Sequential)]
        public struct ControlWeight2
        {
            public int index;
            public float2 weight;
        }
        [Serializable, StructLayout(LayoutKind.Sequential)]
        public struct GroupControlWeight2
        {
            public int groupIndex;
            public int vertexSequenceStartIndex;
            public int vertexCount;
            public float2 weight;
        }
        [Serializable, StructLayout(LayoutKind.Sequential)]
        public struct GroupVertexControlWeight
        {
            public int groupIndex;
            public int vertexIndex;

            public float weight;
        }
        [Serializable, StructLayout(LayoutKind.Sequential)]
        public struct GroupVertexControlWeight2
        {
            public int groupIndex;
            public int vertexIndex;

            public float2 weight;
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

            public int2 fatShapeIndex;

            [ReadOnly]
            public NativeList<int> meshIndicesToUpdate;

            [ReadOnly]
            public NativeArray<MeshVertexDelta> meshShapeDeltas;
            [ReadOnly]
            public NativeArray<float> meshShapeFrameWeights;

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

                GroupControlWeight2 fatGroupControlData = fatGroupControlWeights[groupVertexWeight.groupIndex];

                var shapeDelta = meshShapeDeltas.SampleDeltaShapeBuffer(groupVertexWeight.vertexIndex, meshShapeFrameWeights, fatShapeIndex.x, fatShapeIndex.y, fatGroupControlData.weight.x, vertexCount) * groupVertexWeight.weight;
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
        public struct UpdateMeshMuscleVertexDeltasJob : IJobParallelFor
        {

            public int vertexCount;

            /// <summary>
            /// The combined vertex count for all muscle groups
            /// </summary>
            public int combinedVertexCount;

            public int2 muscleShapeIndex;
            public int2 fatMuscleBlendShapeIndex;

            public float defaultShapeMassWeight;
            public float muscleMassRange;

            [ReadOnly]
            public NativeList<int> meshIndicesToUpdate;

            [ReadOnly]
            public NativeArray<MeshVertexDelta> meshShapeDeltas;
            [ReadOnly]
            public NativeArray<float> meshShapeFrameWeights;

            [ReadOnly]
            public NativeList<GroupControlWeight2> muscleGroupControlWeights;

            /// <summary>
            /// Static array that contains the packed vertex indices and weights for each muscle group, stored sequentially.
            /// </summary>
            [ReadOnly]
            public NativeArray<GroupVertexControlWeight> muscleGroupVertexWeights;

            [ReadOnly]
            public NativeList<float2> fatValuesPerVertex;

            [NativeDisableParallelForRestriction]
            public NativeList<MeshVertexDeltaLR> vertexDeltas;

            public void Execute(int vertexSequenceIndex)
            {
                int meshIndexBufferIndex = vertexSequenceIndex / combinedVertexCount;
                int meshIndex = meshIndicesToUpdate[meshIndexBufferIndex];

                var localVertexSequenceIndex = vertexSequenceIndex - (meshIndexBufferIndex * combinedVertexCount);
                var groupVertexWeight = muscleGroupVertexWeights[localVertexSequenceIndex];

                GroupControlWeight2 muscleGroupMass = muscleGroupControlWeights[groupVertexWeight.groupIndex];

                float2 fat = fatValuesPerVertex[groupVertexWeight.vertexIndex];

                float muscleWeight = groupVertexWeight.weight * (1f - fat.y); // fat.y determines how much of the muscle shape to fade out based on fat level

                float minMassShapeWeight = defaultShapeMassWeight * math.pow(math.saturate(fat.x), 0.3f) * 0.7f;
                float2 muscleGroupMassWeight = math.max(minMassShapeWeight, muscleGroupMass.weight);
                var shapeDeltaL = meshShapeDeltas.SampleDeltaShapeBuffer(groupVertexWeight.vertexIndex, meshShapeFrameWeights, muscleShapeIndex.x, muscleShapeIndex.y, muscleGroupMassWeight.x, vertexCount) * muscleWeight;
                var shapeDeltaR = meshShapeDeltas.SampleDeltaShapeBuffer(groupVertexWeight.vertexIndex, meshShapeFrameWeights, muscleShapeIndex.x, muscleShapeIndex.y, muscleGroupMassWeight.y, vertexCount) * muscleWeight;

                float fatMuscleWeight = muscleWeight * fat.x;

                float2 muscleGroupMassSat = math.saturate((muscleGroupMassWeight - defaultShapeMassWeight) / muscleMassRange);
                shapeDeltaL = shapeDeltaL + meshShapeDeltas.SampleDeltaShapeBuffer(groupVertexWeight.vertexIndex, meshShapeFrameWeights, fatMuscleBlendShapeIndex.x, fatMuscleBlendShapeIndex.y, muscleGroupMassSat.x, vertexCount) * fatMuscleWeight;
                shapeDeltaR = shapeDeltaR + meshShapeDeltas.SampleDeltaShapeBuffer(groupVertexWeight.vertexIndex, meshShapeFrameWeights, fatMuscleBlendShapeIndex.x, fatMuscleBlendShapeIndex.y, muscleGroupMassSat.y, vertexCount) * fatMuscleWeight;

                vertexDeltas[(meshIndex * combinedVertexCount) + localVertexSequenceIndex] = new MeshVertexDeltaLR()
                {
                    deltaLeft = shapeDeltaL,
                    deltaRight = shapeDeltaR
                };
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

        [Serializable]
        public class SerializedData : IDisposable
        {

            #region Fields

            [Header("Rendering")]
            public Material[] materials;

            public int vertexCount;

            public MeshLOD[] meshLODs;
            public Mesh Mesh => meshLODs == null || meshLODs.Length <= 0 ? null : meshLODs[0].mesh;
            public int LevelsOfDetail => meshLODs == null ? 0 : meshLODs.Length;
            public Mesh GetMesh(int lod) => meshLODs == null ? null : GetMeshUnsafe(Mathf.Clamp(lod, 0, meshLODs.Length - 1));
            public Mesh GetMeshUnsafe(int lod) => meshLODs[lod].mesh;
            public MeshLOD GetLOD(int lod) => meshLODs == null ? default : GetLODUnsafe(Mathf.Clamp(lod, 0, meshLODs.Length - 1));
            public MeshLOD GetLODUnsafe(int lod) => meshLODs[lod];

            [SerializeField]
            public Vector3 boundsCenter;
            [SerializeField]
            public Vector3 boundsExtents;

            [HideInInspector]
            public bool[] leftRightFlags;

            [HideInInspector]
            public BoneWeight8[] baseBoneWeights;

            [HideInInspector]
            public Matrix4x4[] baseBindPose;
            public int BoneCount => baseBindPose == null ? 0 : baseBindPose.Length;

            [Header("Shapes")]
            public MeshShape[] meshShapes;

            public Vector2Int standaloneShapes;

            public Vector2Int variationShapes;

            public int massShape;

            public int flexShape;

            public int fatShape;

            public int fatMuscleBlendShape;
            public Vector2 fatMuscleBlendWeightRange;

            public int bustSizeShape;

            [Header("Vertex Groups")]
            public VertexGroup[] vertexGroups;

            public int midlineVertexGroup;
            public int bustVertexGroup;
            public int bustNerfVertexGroup;

            public int nippleMaskVertexGroup;
            public int genitalMaskVertexGroup;

            public float defaultMassShapeWeight;
            public float minMassShapeWeight;

            public Vector2Int standaloneGroups;

            public Vector2Int variationGroups;

            public Vector2Int muscleGroups;      

            public Vector2Int fatGroups;

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


            public string shapesInstanceIDPropertyNameOverride;
            public string ShapesInstanceIDPropertyName => string.IsNullOrWhiteSpace(shapesInstanceIDPropertyNameOverride) ? _shapesInstanceIDPropertyName : shapesInstanceIDPropertyNameOverride;

            public string rigInstanceIDPropertyNameOverride;
            public string RigInstanceIDPropertyName => string.IsNullOrWhiteSpace(rigInstanceIDPropertyNameOverride) ? InstancedSkinnedMeshData._rigInstanceIDPropertyName : rigInstanceIDPropertyNameOverride;

            public string characterInstanceIDPropertyNameOverride;
            public string CharacterInstanceIDPropertyName => string.IsNullOrWhiteSpace(characterInstanceIDPropertyNameOverride) ? _characterInstanceIDPropertyName : characterInstanceIDPropertyNameOverride;


            [Header("Other")]
            public float2[] fatGroupModifiers;

            #endregion

            #region Interface

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
            public ComputeBuffer BoneWeightsBuffer
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
            public ComputeBuffer MuscleGroupInfluencesBuffer
            {
                get
                {
                    if (muscleGroupInfluencesBuffer == null)
                    {
                        tempBoneWeights.Clear();

                        int muscleGroupCount = MuscleGroupsCount;
                        for(int vertexIndex = 0; vertexIndex < vertexCount; vertexIndex++)
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
                            for(int a = 0; a < Mathf.Min(tempWeights.Count, 8); a++)
                            {
                                var weight = tempWeights[a];
                                finalWeights = finalWeights.Modify(a, weight.boneIndex, weight.weight); 

                                totalWeight += weight.weight;
                            }
#if UNITY_EDITOR
                            //if (totalWeight < 0.99f) Debug.LogWarning($"Customizable Mesh vertex {vertexIndex} muscle group influences do not total or exceed 1! (total: {totalWeight})");
#endif
                            if (totalWeight > 0f)
                            {
                                finalWeights.weightsA = finalWeights.weightsA / totalWeight;
                                finalWeights.weightsB = finalWeights.weightsB / totalWeight;
                            }
                            tempBoneWeights.Add(finalWeights);
                        }

                        muscleGroupInfluencesBuffer = new ComputeBuffer(tempBoneWeights.Count, UnsafeUtility.SizeOf(typeof(BoneWeight8Float)), ComputeBufferType.Structured, ComputeBufferMode.Immutable); // we use the bone weight structs because the data is identical (boneIndex = vertex group Index, boneWeight = vertex group weight)
                        if (tempBoneWeights.Count > 0) muscleGroupInfluencesBuffer.SetData(tempBoneWeights);

                        tempBoneWeights.Clear();

                        TrackDisposables();
                    }

                    return muscleGroupInfluencesBuffer;
                }
            }
            [NonSerialized]
            protected ComputeBuffer fatGroupInfluencesBuffer;
            public ComputeBuffer FatGroupInfluencesBuffer
            {
                get
                {
                    if (fatGroupInfluencesBuffer == null)
                    {
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

                        fatGroupInfluencesBuffer = new ComputeBuffer(tempBoneWeights.Count, UnsafeUtility.SizeOf(typeof(BoneWeight8Float)), ComputeBufferType.Structured, ComputeBufferMode.Immutable); // we use the bone weight structs because the data is identical (boneIndex = vertex group Index, boneWeight = vertex group weight)
                        if (tempBoneWeights.Count > 0) fatGroupInfluencesBuffer.SetData(tempBoneWeights);

                        tempBoneWeights.Clear();

                        TrackDisposables();
                    }

                    return fatGroupInfluencesBuffer;
                }
            }

            public Matrix4x4[] ManagedBindPose => baseBindPose;

            public int MeshShapeCount => meshShapes == null ? 0 : meshShapes.Length;
            public int MeshShapeDeltasCount
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
            public int IndexOfShape(string shapeName, bool caseSensitive = false)
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

            public int IndexOfMeshShape(string name)
            {
                if (meshShapes == null) return -1;

                for (int a = 0; a < meshShapes.Length; a++)
                {
                    var shape = meshShapes[a];
                    if (shape.name == name) return a;
                }

                return -1;
            }

            public int VertexGroupCount => vertexGroups == null ? 0 : vertexGroups.Length;
            public VertexGroup GetVertexGroup(int index)
            {
                if (index < 0 || vertexGroups == null || index >= vertexGroups.Length) return null;
                return GetVertexGroupUnsafe(index);
            }
            public VertexGroup GetVertexGroupUnsafe(int index) => vertexGroups[index];
            public int IndexOfVertexGroup(string vertexGroupName, bool caseSensitive = false)
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

            public int IndexOfVertexGroup(string name)
            {
                if (vertexGroups == null) return -1;

                for (int a = 0; a < vertexGroups.Length; a++)
                {
                    var group = vertexGroups[a];
                    if (group.name == name) return a;
                }

                return -1;
            }

            public string VertexGroupsBufferRangePropertyName => string.IsNullOrWhiteSpace(standaloneVertexGroupsBufferRangePropertyNameOverride) ? _vertexGroupsBufferRangeDefaultPropertyName : standaloneVertexGroupsBufferRangePropertyNameOverride;
            public int StandaloneGroupsCount => standaloneGroups.y < standaloneGroups.x ? 0 : ((standaloneGroups.y - standaloneGroups.x) + 1);
            public int StandaloneVertexGroupCount => StandaloneGroupsCount;
            public int IndexOfStandaloneVertexGroup(string name)
            {
                for (int a = 0; a < StandaloneVertexGroupCount; a++)
                {
                    var group = vertexGroups[a + standaloneGroups.x];
                    if (group.name == name) return a;
                }

                return -1;
            }
            public VertexGroup GetStandaloneVertexGroup(int index)
            {
                if (index < 0 || index >= StandaloneVertexGroupCount) return null;
                return vertexGroups[standaloneGroups.x + index];
            }

            public string MuscleVertexGroupsBufferRangePropertyName => string.IsNullOrWhiteSpace(muscleVertexGroupsBufferRangePropertyNameOverride) ? _muscleVertexGroupsBufferRangeDefaultPropertyName : muscleVertexGroupsBufferRangePropertyNameOverride;
            public int MuscleGroupsCount => muscleGroups.y < muscleGroups.x ? 0 : ((muscleGroups.y - muscleGroups.x) + 1);
            public int MuscleVertexGroupCount => MuscleGroupsCount;
            public int IndexOfMuscleGroup(string name)
            {
                for (int a = 0; a < MuscleVertexGroupCount; a++)
                {
                    var shape = vertexGroups[a + muscleGroups.x];
                    if (shape.name == name) return a;
                }

                return -1;
            }
            public VertexGroup GetMuscleVertexGroup(int index)
            {
                if (index < 0 || index >= MuscleVertexGroupCount) return null;
                return vertexGroups[muscleGroups.x + index];
            }

            public string FatVertexGroupsBufferRangePropertyName => string.IsNullOrWhiteSpace(fatVertexGroupsBufferRangePropertyNameOverride) ? _fatVertexGroupsBufferRangeDefaultPropertyName : fatVertexGroupsBufferRangePropertyNameOverride;
            public int FatGroupsCount => fatGroups.y < fatGroups.x ? 0 : ((fatGroups.y - fatGroups.x) + 1);
            public int FatVertexGroupCount => FatGroupsCount;
            public int IndexOfFatGroup(string name)
            {
                for (int a = 0; a < FatVertexGroupCount; a++)
                {
                    var shape = vertexGroups[a + fatGroups.x];
                    if (shape.name == name) return a;
                }

                return -1;
            }
            public VertexGroup GetFatVertexGroup(int index)
            {
                if (index < 0 || index >= FatVertexGroupCount) return null;
                return vertexGroups[fatGroups.x + index];
            }
            public static float2 DefaultFatGroupModifier => new float2(1, 0);
            /// <summary>
            /// modifier.x is how much to nerf muscle mass by based on fat level
            public float2 GetFatGroupModifier(int index)
            {
                if (index < 0 || fatGroupModifiers == null || index >= fatGroupModifiers.Length) return DefaultFatGroupModifier;
                return fatGroupModifiers[index];
            }
            
            public string VariationVertexGroupsBufferRangePropertyName => string.IsNullOrWhiteSpace(variationVertexGroupsBufferRangePropertyNameOverride) ? _variationVertexGroupsBufferRangeDefaultPropertyName : variationVertexGroupsBufferRangePropertyNameOverride;
            public int VariationGroupsCount => variationGroups.y < variationGroups.x ? 0 : ((variationGroups.y - variationGroups.x) + 1);
            public int VariationVertexGroupCount => VariationGroupsCount;
            public int IndexOfVariationGroup(string name)
            {
                for (int a = 0; a < VariationVertexGroupCount; a++)
                {
                    var shape = vertexGroups[a + variationGroups.x];
                    if (shape.name == name) return a;
                }

                return -1;
            }
            public VertexGroup GetVariationVertexGroup(int index)
            {
                if (index < 0 || index >= VariationVertexGroupCount) return null;
                return vertexGroups[index + variationGroups.x];
            }


            public string standaloneShapesBufferRangePropertyNameOverride;
            public string StandaloneShapesBufferRangePropertyName => string.IsNullOrWhiteSpace(standaloneShapesBufferRangePropertyNameOverride) ? _standaloneShapesBufferRangeDefaultPropertyName : standaloneShapesBufferRangePropertyNameOverride;
            public int StandaloneShapesCount => standaloneShapes.y < standaloneShapes.x ? 0 : ((standaloneShapes.y - standaloneShapes.x) + 1);
            public int IndexOfStandaloneShape(string name)
            {
                for (int a = 0; a < StandaloneShapesCount; a++)
                {
                    var shape = meshShapes[a + standaloneShapes.x];
                    if (shape.name == name) return a;
                }

                return -1;
            }
            public MeshShape GetStandaloneShape(int index)
            {
                if (index < 0 || index >= StandaloneShapesCount) return null;
                return meshShapes[standaloneShapes.x + index];
            }

            public MeshShape MassShape => massShape >= 0 && meshShapes != null ? meshShapes[massShape] : null;
            public int MassShapeFrameCount => massShape >= 0 && meshShapes != null ? meshShapes[massShape].FrameCount : 0;

            public MeshShape FlexShape => flexShape >= 0 && meshShapes != null ? meshShapes[flexShape] : null;
            public int FlexShapeFrameCount => flexShape >= 0 && meshShapes != null ? meshShapes[flexShape].FrameCount : 0;

            public MeshShape FatShape => fatShape >= 0 && meshShapes != null ? meshShapes[fatShape] : null;
            public int FatShapeFrameCount => fatShape >= 0 && meshShapes != null ? meshShapes[fatShape].FrameCount : 0;

            public MeshShape FatMuscleBlendShape => fatMuscleBlendShape >= 0 && meshShapes != null ? meshShapes[fatMuscleBlendShape] : null;
            public int FatMuscleBlendShapeFrameCount => fatMuscleBlendShape >= 0 && meshShapes != null ? meshShapes[fatMuscleBlendShape].FrameCount : 0;

            public MeshShape BustSizeShape => bustSizeShape >= 0 && meshShapes != null ? meshShapes[bustSizeShape] : null;
            public int BustSizeShapeFrameCount => bustSizeShape >= 0 && meshShapes != null ? meshShapes[bustSizeShape].FrameCount : 0;


            public string variationShapesBufferRangePropertyNameOverride;
            public string VariationShapesBufferRangePropertyName => string.IsNullOrWhiteSpace(variationShapesBufferRangePropertyNameOverride) ? _variationShapesBufferRangeDefaultPropertyName : variationShapesBufferRangePropertyNameOverride;
            public int VariationShapesCount => variationShapes.y < variationShapes.x ? 0 : ((variationShapes.y - variationShapes.x) + 1);
            public int IndexOfVariationShape(string name)
            {
                for (int a = 0; a < VariationShapesCount; a++)
                {
                    var shape = meshShapes[a + variationShapes.x];
                    if (shape.name == name) return a;
                }

                return -1;
            }
            public MeshShape GetVariationShape(int index)
            {
                if (index < 0 || index >= VariationShapesCount) return null;
                return meshShapes[variationShapes.x + index];
            }

            protected static readonly List<float> tempFloats = new List<float>();
            [NonSerialized]
            protected ComputeBuffer vertexGroupsBuffer;
            public ComputeBuffer VertexGroupsBuffer
            {
                get
                {
                    if (vertexGroupsBuffer == null)
                    {
                        tempFloats.Clear();

                        foreach(var vertexGroup in vertexGroups)
                        {
                            for (int vertexIndex = 0; vertexIndex < vertexCount; vertexIndex++) tempFloats.Add(vertexGroup[vertexIndex]);
                        }

                        vertexGroupsBuffer = new ComputeBuffer(tempFloats.Count, UnsafeUtility.SizeOf(typeof(float)), ComputeBufferType.Structured, ComputeBufferMode.Immutable);
                        if (tempFloats.Count > 0) vertexGroupsBuffer.SetData(tempFloats);

                        tempFloats.Clear();

                        TrackDisposables();
                    }

                    return vertexGroupsBuffer;
                }
            }

            protected static readonly List<MorphShapeVertex> tempFrameDeltas = new List<MorphShapeVertex>();
            [NonSerialized]
            protected ComputeBuffer meshShapeFrameDeltasBuffer;
            public ComputeBuffer MeshShapeFrameDeltasBuffer
            {
                get
                {
                    if (meshShapeFrameDeltasBuffer == null)
                    {
                        tempFrameDeltas.Clear();

                        foreach (var meshShape in meshShapes)
                        {
                            if (meshShape == null || meshShape.frames == null) continue;

                            for(int frameIndex = 0; frameIndex < meshShape.frames.Length; frameIndex++)
                            {
                                var frame = meshShape.frames[frameIndex];
                                for (int vertexIndex = 0; vertexIndex < vertexCount; vertexIndex++) tempFrameDeltas.Add(frame.deltas == null ? default : frame.deltas[vertexIndex]);
                            }
                        }

                        meshShapeFrameDeltasBuffer = new ComputeBuffer(tempFrameDeltas.Count, UnsafeUtility.SizeOf(typeof(MorphShapeVertex)), ComputeBufferType.Structured, ComputeBufferMode.Immutable);
                        if (tempFrameDeltas.Count > 0) meshShapeFrameDeltasBuffer.SetData(tempFrameDeltas);

                        tempFrameDeltas.Clear();

                        TrackDisposables();
                    }

                    return meshShapeFrameDeltasBuffer;
                }
            }
            [NonSerialized]
            protected ComputeBuffer meshShapeFrameWeightsBuffer;
            public ComputeBuffer MeshShapeFrameWeightsBuffer
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

                        meshShapeFrameWeightsBuffer = new ComputeBuffer(tempFloats.Count, UnsafeUtility.SizeOf(typeof(float)), ComputeBufferType.Structured, ComputeBufferMode.Immutable);
                        if (tempFloats.Count > 0) meshShapeFrameWeightsBuffer.SetData(tempFloats);

                        tempFloats.Clear();

                        TrackDisposables();
                    }

                    return meshShapeFrameWeightsBuffer;
                }
            }
            protected static readonly List<int2> tempRanges = new List<int2>();
            [NonSerialized]
            protected ComputeBuffer meshShapeIndicesBuffer;
            public ComputeBuffer MeshShapeIndicesBuffer
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

                        meshShapeIndicesBuffer = new ComputeBuffer(tempRanges.Count, UnsafeUtility.SizeOf(typeof(int2)), ComputeBufferType.Structured, ComputeBufferMode.Immutable);
                        if (tempRanges.Count > 0) meshShapeIndicesBuffer.SetData(tempRanges);

                        tempRanges.Clear();

                        TrackDisposables();
                    }

                    return meshShapeIndicesBuffer;
                }
            }

            #endregion

            #region Disposal

            [NonSerialized]
            private bool trackingDisposables;
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

            public void Dispose()
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

                /*try
                {
                    if (vertices.IsCreated)
                    {
                        vertices.Dispose();
                        vertices = default;
                    }
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR
                    Debug.LogException(ex);
#endif
                }*/
            }

            #endregion

        }

        #endregion

        #region Skinned Mesh Sync

        protected List<BlendShapeSync>[] standaloneShapeSyncs;
        protected List<BlendShapeSyncLR>[] muscleMassShapeSyncs;
        protected List<BlendShapeSyncLR>[] muscleFlexShapeSyncs;
        protected List<BlendShapeSync>[] fatShapeSyncs;
        protected List<BlendShapeSyncLR>[] variationShapeSyncs;

        protected override void SetupSkinnedMeshSyncs()
        {
            if (standaloneShapeSyncs == null) standaloneShapeSyncs = new List<BlendShapeSync>[Data.StandaloneShapesCount];
            if (muscleMassShapeSyncs == null) muscleMassShapeSyncs = new List<BlendShapeSyncLR>[Data.MassShapeFrameCount * Data.MuscleVertexGroupCount];
            if (muscleFlexShapeSyncs == null) muscleFlexShapeSyncs = new List<BlendShapeSyncLR>[Data.FlexShapeFrameCount * Data.MuscleVertexGroupCount];
            if (fatShapeSyncs == null) fatShapeSyncs = new List<BlendShapeSync>[Data.FatShapeFrameCount * Data.FatVertexGroupCount];
            if (variationShapeSyncs == null) variationShapeSyncs = new List<BlendShapeSyncLR>[VariationShapesControlDataSize];

            for (int a = 0; a < standaloneShapeSyncs.Length; a++)
            {
                var list = standaloneShapeSyncs[a];
                if (list == null) list = new List<BlendShapeSync>();

                list.Clear();
                standaloneShapeSyncs[a] = list;
            }
            for (int a = 0; a < muscleMassShapeSyncs.Length; a++)
            {
                var list = muscleMassShapeSyncs[a];
                if (list == null) list = new List<BlendShapeSyncLR>();

                list.Clear();
                muscleMassShapeSyncs[a] = list;
            }
            for (int a = 0; a < muscleFlexShapeSyncs.Length; a++)
            {
                var list = muscleFlexShapeSyncs[a];
                if (list == null) list = new List<BlendShapeSyncLR>();

                list.Clear();
                muscleFlexShapeSyncs[a] = list;
            }
            for (int a = 0; a < fatShapeSyncs.Length; a++)
            {
                var list = fatShapeSyncs[a];
                if (list == null) list = new List<BlendShapeSync>();

                list.Clear();
                fatShapeSyncs[a] = list;
            }
            for (int a = 0; a < variationShapeSyncs.Length; a++)
            {
                var list = variationShapeSyncs[a];
                if (list == null) list = new List<BlendShapeSyncLR>();

                list.Clear();
                variationShapeSyncs[a] = list;
            }

            syncedSkinnedMeshes.RemoveAll(i => i == null || i.sharedMesh == null);

            var charData = SubData;
            for (int a = 0; a < syncedSkinnedMeshes.Count; a++)
            {
                var mesh = syncedSkinnedMeshes[a];
                if (mesh == null) continue;

                for (int b = 0; b < charData.StandaloneShapesCount; b++)
                {
                    var shape = charData.GetStandaloneShape(b);

                    int shapeIndex = mesh.sharedMesh.GetBlendShapeIndex(shape.name);
                    if (shapeIndex >= 0) standaloneShapeSyncs[b].Add(new BlendShapeSync() { listenerIndex = a, listenerShapeIndex = shapeIndex });
                }

                for (int b = 0; b < charData.MuscleVertexGroupCount; b++)
                {
                    for (int c = 0; c < charData.MassShapeFrameCount; c++)
                    {
                        int shapeIndexL = mesh.sharedMesh.GetBlendShapeIndex(GetMuscleMassShapeSyncNameLeft(b, c));
                        int shapeIndexR = mesh.sharedMesh.GetBlendShapeIndex(GetMuscleMassShapeSyncNameRight(b, c));

                        if (shapeIndexL >= 0 || shapeIndexR >= 0) muscleMassShapeSyncs[(b * charData.MassShapeFrameCount) + c].Add(new BlendShapeSyncLR() { listenerIndex = a, listenerShapeIndexLeft = shapeIndexL, listenerShapeIndexRight = shapeIndexR });
                    }

                    for (int c = 0; c < charData.FlexShapeFrameCount; c++)
                    {
                        int shapeIndexL = mesh.sharedMesh.GetBlendShapeIndex(GetMuscleFlexShapeSyncNameLeft(b, c));
                        int shapeIndexR = mesh.sharedMesh.GetBlendShapeIndex(GetMuscleFlexShapeSyncNameRight(b, c));

                        if (shapeIndexL >= 0 || shapeIndexR >= 0) muscleFlexShapeSyncs[(b * charData.FlexShapeFrameCount) + c].Add(new BlendShapeSyncLR() { listenerIndex = a, listenerShapeIndexLeft = shapeIndexL, listenerShapeIndexRight = shapeIndexR });
                    }
                }

                for (int b = 0; b < charData.FatVertexGroupCount; b++)
                {
                    for (int c = 0; c < charData.FatShapeFrameCount; c++)
                    {
                        int shapeIndex = mesh.sharedMesh.GetBlendShapeIndex(GetFatShapeSyncName(b, c));

                        if (shapeIndex >= 0) fatShapeSyncs[(b * charData.FatShapeFrameCount) + c].Add(new BlendShapeSync() { listenerIndex = a, listenerShapeIndex = shapeIndex });
                    }
                }

                for (int b = 0; b < charData.VariationVertexGroupCount; b++)
                {
                    for (int c = 0; c < charData.VariationShapesCount; c++)
                    {
                        int shapeIndexL = mesh.sharedMesh.GetBlendShapeIndex(GetVariationShapeSyncNameLeft(b, c));
                        int shapeIndexR = mesh.sharedMesh.GetBlendShapeIndex(GetVariationShapeSyncNameRight(b, c));

                        if (shapeIndexL >= 0 || shapeIndexR >= 0) variationShapeSyncs[(b * charData.VariationShapesCount) + c].Add(new BlendShapeSyncLR() { listenerIndex = a, listenerShapeIndexLeft = shapeIndexL, listenerShapeIndexRight = shapeIndexR });
                    }
                }
            }
        }

        protected void SyncMuscleMassData(int groupIndex, float massL, float massR)
        {
            var frameWeights = SubData.MassShape.FrameWeights;

            SyncPartialShapeData(muscleMassShapeSyncs, groupIndex, massL, massR, frameWeights.Length, frameWeights);
        }
        protected void SyncMuscleFlexData(int groupIndex, float flexL, float flexR)
        {
            var frameWeights = SubData.FlexShape.FrameWeights;

            SyncPartialShapeData(muscleFlexShapeSyncs, groupIndex, flexL, flexR, frameWeights.Length, frameWeights);
        }
        protected void SyncFatLevel(int groupIndex, float weight)
        {
            var frameWeights = SubData.FatShape.FrameWeights;

            SyncPartialShapeData(fatShapeSyncs, groupIndex, weight, frameWeights.Length, frameWeights);
        }
        protected void SyncVariationData(int groupIndex, int shapeIndex, float weightL, float weightR)
        {
            var count = SubData.VariationShapesCount;

            var list = variationShapeSyncs[(groupIndex * count) + shapeIndex];
            if (list != null && list.Count > 0)
            {
                foreach (var sync in list)
                {
                    var mesh = syncedSkinnedMeshes[sync.listenerIndex];
                    if (mesh != null)
                    {
                        if (sync.listenerShapeIndexLeft >= 0) mesh.SetBlendShapeWeight(sync.listenerShapeIndexLeft, weightL);
                        if (sync.listenerShapeIndexRight >= 0) mesh.SetBlendShapeWeight(sync.listenerShapeIndexRight, weightR);
                    }
                }
            }
        }

        #endregion

        #region Disposal

        public override void Dispose()
        {
            base.Dispose();

            try
            {
                if (skinningMatricesBuffer != null)
                {
                    var materialInstances = MaterialInstances;
                    if (materialInstances != null)
                    {
                        foreach (var mat in materialInstances)
                        {
                            if (mat == null) continue;
                            skinningMatricesBuffer.UnbindMaterialProperty(mat, SubData.SkinningMatricesPropertyName);
                        }
                    }
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
                if (standaloneShapeControlBuffer != null)
                {
                    var materialInstances = MaterialInstances;
                    if (materialInstances != null)
                    {
                        foreach (var mat in materialInstances)
                        {
                            if (mat == null) continue;
                            standaloneShapeControlBuffer.UnbindMaterialProperty(mat, SubData.StandaloneShapesControlPropertyName);
                        }
                    }
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
                if (muscleGroupsControlBuffer != null)
                {
                    var materialInstances = MaterialInstances;
                    if (materialInstances != null)
                    {
                        foreach (var mat in materialInstances)
                        {
                            if (mat == null) continue;
                            muscleGroupsControlBuffer.UnbindMaterialProperty(mat, SubData.MuscleGroupsControlPropertyName);
                        }
                    }
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
                if (fatGroupsControlBuffer != null)
                {
                    var materialInstances = MaterialInstances;
                    if (materialInstances != null)
                    {
                        foreach (var mat in materialInstances)
                        {
                            if (mat == null) continue;
                            fatGroupsControlBuffer.UnbindMaterialProperty(mat, SubData.FatGroupsControlPropertyName);
                        }
                    }
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
                if (variationShapesControlBuffer != null)
                {
                    var materialInstances = MaterialInstances;
                    if (materialInstances != null)
                    {
                        foreach (var mat in materialInstances)
                        {
                            if (mat == null) continue;
                            variationShapesControlBuffer.UnbindMaterialProperty(mat, SubData.VariationShapesControlPropertyName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogException(ex);
#endif
            }

            if (instance != null) Updater.Unregister(ref instance);
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();
            
            if (children != null)
            {
                children.Clear();
                children = null;
            }

            if (animatablePropertiesController != null)
            {
                string id = GetInstanceID().ToString();
                for (int a = 0; a < animatablePropertiesController.PropertyCount; a++)
                {
                    var prop = animatablePropertiesController.GetPropertyUnsafe(a);
                    prop.ClearListeners(id);
                }
            }
        }

        #endregion

        new protected InstanceV2 instance;
        public int InstanceID => instance == null ? 0 : instance.localID;
        public override int InstanceSlot => InstanceID;

        protected override void CreateInstance()
        {
            if (instance != null && instance.IsValid) return;

            instance = Updater.Register(Data);
            InitBuffers();
            InitializeRendering();
        }
        protected override void CreateInstance(List<InstancedRendering.MaterialPropertyInstanceOverride<float>> floatOverrides, List<InstancedRendering.MaterialPropertyInstanceOverride<Color>> colorOverrides, List<InstancedRendering.MaterialPropertyInstanceOverride<Vector4>> vectorOverrides)
        {
            CreateInstance();
        }

        public GameObject GameObject => gameObject;

        protected override void OnStart()
        {
            base.OnStart();
        }

        #region Data

        [SerializeField]
        protected CustomizableCharacterMeshV2_DATA data;
        public void SetData(CustomizableCharacterMeshV2_DATA data) => this.data = data;
        public CustomizableCharacterMeshV2_DATA Data => data;
        public SerializedData SubData => data == null ? null : data.SerializedData;


        public override InstanceableMeshDataBase MeshData => null;
        public override InstancedMeshGroup MeshGroup => null;

        #endregion

        #region Children

        protected List<CustomizableCharacterMeshV2> children;
        public void AddChild(CustomizableCharacterMeshV2 child)
        {
            if (child == null) return;

            if (children == null) children = new List<CustomizableCharacterMeshV2>();
            children.Add(child);
        }
        public void RemoveChild(CustomizableCharacterMeshV2 child)
        {
            if (children == null) return;

            children.RemoveAll(i => ReferenceEquals(i, child));
        }

        #endregion

        #region Material Handling

        public Material[] MaterialInstances
        {
            get
            {
                if (instance != null) return instance.Materials;

                return null;
            }
        }

        protected virtual void SetFloatOverride(string property, float value, bool updateMaterials = true)
        {
            var materialInstances = MaterialInstances;
            if (materialInstances == null) return;

            foreach(var mat in materialInstances)
            {
                if (mat != null) mat.SetFloat(property, value);
            }
        }

        #endregion

        #region Rendering

        protected struct DefaultRenderedMesh
        {
            public MeshFilter meshFilter;
            public MeshRenderer meshRenderer;
        }

        protected LODGroup lodGroup;

        protected DefaultRenderedMesh[] defaultRenderedMeshes;

        public bool IsRendering() => lodGroup != null && defaultRenderedMeshes != null;
        public override bool IsRendering(int index) => IsRendering();
        public bool CanRender => instance != null && instance.IsValid;

        public void InitializeRendering()
        {
            if (IsRendering() || !CanRender) return;

            var meshData = SubData;
            var bounds = new Bounds(meshData.boundsCenter, meshData.boundsExtents * 2f);

            GameObject lodObj = new GameObject("renderers");

            var lodRootTransform = lodObj.transform;
            lodRootTransform.SetParent(transform, false);
            lodRootTransform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            lodRootTransform.localScale = Vector3.one;

            lodGroup = lodObj.AddComponent<LODGroup>();
            defaultRenderedMeshes = new DefaultRenderedMesh[meshData.LevelsOfDetail];
            var lods = new LOD[meshData.LevelsOfDetail];
            for (int a = 0; a < meshData.LevelsOfDetail; a++)
            {
                var meshLOD = meshData.GetLODUnsafe(a);
                var defaultRenderedMesh = new DefaultRenderedMesh();

                GameObject rendererObj = new GameObject($"LOD_{a}");
                rendererObj.transform.SetParent(lodRootTransform, false);
                
                defaultRenderedMesh.meshFilter = rendererObj.AddComponent<MeshFilter>();
                defaultRenderedMesh.meshFilter.sharedMesh = meshLOD.mesh;
                defaultRenderedMesh.meshRenderer = rendererObj.AddComponent<MeshRenderer>();
                defaultRenderedMesh.meshRenderer.sharedMaterials = MaterialInstances;
                defaultRenderedMesh.meshRenderer.localBounds = bounds;

                var lod = new LOD()
                {
                    screenRelativeTransitionHeight = meshLOD.screenRelativeTransitionHeight * 100f,
                    renderers = new Renderer[] { defaultRenderedMesh.meshRenderer }
                };

                defaultRenderedMeshes[a] = defaultRenderedMesh;
                lods[a] = lod;
            }

            lodGroup.SetLODs(lods);

            var matricesBuffer = SkinningMatricesBuffer;
            if (RigSampler != null) rigSampler.AddWritableInstanceBuffer(matricesBuffer, RigInstanceID * SkinningBoneCount);

            var materialInstances = MaterialInstances;
            if (materialInstances != null)
            {

                if (matricesBuffer != null)
                {
                    foreach(var mat in materialInstances)
                    {
                        if (mat == null) continue;

                        Debug.Log($"Binding {meshData.SkinningMatricesPropertyName} to {mat.name}");
                        matricesBuffer.BindMaterialProperty(mat, meshData.SkinningMatricesPropertyName);
                    }
                }
                
                SetFloatOverride(meshData.RigInstanceIDPropertyName, RigInstanceID);

                var shapesBuffer = StandaloneShapeControlBuffer;
                if (shapesBuffer != null)
                {
                    foreach (var mat in materialInstances)
                    {
                        if (mat == null) continue;

                        Debug.Log($"Binding {meshData.StandaloneShapesControlPropertyName} to {mat.name}");
                        shapesBuffer.BindMaterialProperty(mat, meshData.StandaloneShapesControlPropertyName);
                    }
                }

                SetFloatOverride(meshData.ShapesInstanceIDPropertyName, ShapesInstanceID);

                var muscleBuffer = MuscleGroupsControlBuffer;
                if (muscleBuffer != null)
                {
                    foreach (var mat in materialInstances)
                    {
                        if (mat == null) continue;

                        Debug.Log($"Binding {meshData.MuscleGroupsControlPropertyName} to {mat.name}");
                        muscleBuffer.BindMaterialProperty(mat, meshData.MuscleGroupsControlPropertyName);
                    }
                }

                var fatBuffer = FatGroupsControlBuffer;
                if (fatBuffer != null)
                {
                    foreach (var mat in materialInstances)
                    {
                        if (mat == null) continue;

                        Debug.Log($"Binding {meshData.FatGroupsControlPropertyName} to {mat.name}");
                        fatBuffer.BindMaterialProperty(mat, meshData.FatGroupsControlPropertyName);
                    }
                }

                var variationBuffer = VariationShapesControlBuffer;
                if (variationBuffer != null)
                {
                    foreach (var mat in materialInstances)
                    {
                        if (mat == null) continue;

                        Debug.Log($"Binding {meshData.VariationShapesControlPropertyName} to {mat.name}");
                        variationBuffer.BindMaterialProperty(mat, meshData.VariationShapesControlPropertyName);
                    }
                }

                SetFloatOverride(meshData.CharacterInstanceIDPropertyName, CharacterInstanceID);

            }
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

        #region Body Settings

        [NonSerialized]
        protected float bustSize;
        public float BustSize
        {
            get => bustSize;
            set => SetBustSize(value);
        }
        [NonSerialized]
        protected bool hasBustSizeProperty;
        public void SetBustSize(float value)
        {
            bustSize = value;

            if (SubData.bustSizeShape >= 0) SetStandaloneShapeWeightUnsafe(SubData.bustSizeShape, value);

            if (hasBustSizeProperty)
            {
                SetFloatOverride(SubData.BustMixPropertyName, Mathf.Clamp01(value), true);

                if (children != null)
                {
                    foreach (var child in children) if (child != null) child.SetBustSize(value);
                }
            }
        }
        [NonSerialized]
        protected bool hideNipples;
        public bool HideNipples
        {
            get => hideNipples;
            set => SetHideNipples(value);
        }
        [NonSerialized]
        protected bool hasHideNipplesProperty;
        public void SetHideNipples(bool value)
        {
            hideNipples = value;

            if (hasHideNipplesProperty)
            {
                SetFloatOverride(SubData.HideNipplesPropertyName, hideNipples ? 1 : 0, true);

                if (children != null)
                {
                    foreach (var child in children) if (child != null) child.SetHideNipples(value);
                }
            }
        }
        [NonSerialized]
        protected bool hideGenitals;
        public bool HideGenitals
        {
            get => hideGenitals;
            set => SetHideGenitals(value);
        }
        [NonSerialized]
        protected bool hasHideGenitalsProperty;
        public void SetHideGenitals(bool value)
        {
            hideGenitals = value;

            if (hasHideGenitalsProperty)
            {
                SetFloatOverride(SubData.HideGenitalsPropertyName, hideGenitals ? 1 : 0, true);

                if (children != null)
                {
                    foreach (var child in children) if (child != null) child.SetHideGenitals(value);
                }
            }
        }

        #endregion

        #region Customization Shapes & Groups

        public void MarkForPhysiqueUpdate()
        {
            if (instance == null || !instance.IsValid) return;
            instance.MarkForPhysiqueUpdateUnsafe();
        }

        public void MarkForVariationUpdate()
        {
            if (instance == null || !instance.IsValid) return;
            instance.MarkForVariationUpdateUnsafe();
        }

        public int FirstStandaloneShapesControlIndex => InstanceID * SubData.StandaloneShapesCount;
        public float GetStandaloneShapeWeightUnsafe(int shapeIndex) => StandaloneShapeControlBuffer[FirstStandaloneShapesControlIndex + shapeIndex];//StandaloneShapesControl[shapeIndex];
        public float GetStandaloneShapeWeight(int shapeIndex)
        {
            if (instance == null || shapeIndex < 0 || shapeIndex >= SubData.StandaloneShapesCount) return 0;
            return GetStandaloneShapeWeightUnsafe(shapeIndex);
        }
        public void SetStandaloneShapeWeightUnsafe(int shapeIndex, float weight)
        {
            if (shapesInstanceReference != null) return;
            StandaloneShapeControlBuffer[FirstStandaloneShapesControlIndex + shapeIndex] = weight;

            SyncStandaloneShape(standaloneShapeSyncs, shapeIndex, weight);
        }
        public void SetStandaloneShapeWeight(int shapeIndex, float weight)
        {
            if (instance == null || shapeIndex < 0 || shapeIndex >= SubData.StandaloneShapesCount) return;
            SetStandaloneShapeWeightUnsafe(shapeIndex, weight);
        }

        public int FirstMuscleGroupsControlIndex => InstanceID * SubData.MuscleVertexGroupCount;
        public MuscleDataLR GetMuscleDataUnsafe(int groupIndex) => MuscleGroupsControlBuffer[FirstMuscleGroupsControlIndex + groupIndex];//MuscleGroupsControl[groupIndex];
        public MuscleDataLR GetMuscleData(int groupIndex)
        {
            if (instance == null || groupIndex < 0 || groupIndex >= SubData.MuscleVertexGroupCount) return default;
            return GetMuscleDataUnsafe(groupIndex);
        }
        public UnityEvent<int> OnMuscleDataChanged;
        public void SetMuscleDataUnsafe(int groupIndex, MuscleDataLR data)
        {
            //var array = MuscleGroupsControl;
            //array[groupIndex] = data;

            if (characterInstanceReference != null) return;
            int controlIndex = FirstMuscleGroupsControlIndex + groupIndex;
            var prevData = MuscleGroupsControlBuffer[controlIndex];
            MuscleGroupsControlBuffer[controlIndex] = data;

            SyncMuscleMassData(groupIndex, data.valuesLeft.mass, data.valuesRight.mass);
            SyncMuscleFlexData(groupIndex, data.valuesLeft.flex, data.valuesRight.flex);

            //dirtyFlag_muscleGroupsControl = true;

            if (instance != null && instance.IsValid && (prevData.valuesLeft.mass != data.valuesLeft.mass || prevData.valuesRight.mass != data.valuesRight.mass))
            {
                instance.SetMuscleGroupWeightUnsafe(groupIndex, new float2(data.valuesLeft.mass, data.valuesRight.mass));
                instance.MarkForPhysiqueUpdateUnsafe();
            }

            OnMuscleDataChanged?.Invoke(groupIndex);
        }
        public void SetMuscleData(int groupIndex, MuscleDataLR data)
        {
            if (instance == null || groupIndex < 0 || groupIndex >= SubData.MuscleVertexGroupCount) return;
            SetMuscleDataUnsafe(groupIndex, data);
        }
        public int IndexOfMuscleGroup(string groupName) => Data == null ? -1 : SubData.IndexOfMuscleGroup(groupName);

        public int FirstFatGroupsControlIndex => InstanceID * SubData.FatVertexGroupCount;
        public float GetFatLevelUnsafe(int groupIndex) => FatGroupsControlBuffer[FirstFatGroupsControlIndex + groupIndex].x;//FatGroupsControl[groupIndex].x;
        public float GetFatLevel(int groupIndex)
        {
            if (instance == null || groupIndex < 0 || groupIndex >= SubData.FatVertexGroupCount) return 0;  
            return GetFatLevelUnsafe(groupIndex);
        }
        public UnityEvent<int> OnFatDataChanged;
        public void SetFatLevelUnsafe(int groupIndex, float level)
        {
            //var array = FatGroupsControl;
            //var val = array[groupIndex];
            //val.x = level;
            //array[groupIndex] = val;

            if (characterInstanceReference != null) return;
            int controlIndex = FirstFatGroupsControlIndex + groupIndex;
            var val = FatGroupsControlBuffer[controlIndex];
            val.x = level;
            FatGroupsControlBuffer[controlIndex] = val;

            SyncFatLevel(groupIndex, level);

            //dirtyFlag_fatGroupsControl = true;

            if (instance != null && instance.IsValid)
            {
                instance.SetFatGroupWeightUnsafe(groupIndex, level);
                instance.MarkForPhysiqueUpdateUnsafe();
            }

            OnFatDataChanged?.Invoke(groupIndex);
        }
        public void SetFatLevel(int groupIndex, float level)
        {
            if (instance == null || groupIndex < 0 || groupIndex >= SubData.FatVertexGroupCount) return;
            SetFatLevelUnsafe(groupIndex, level);
        }
        public int IndexOfFatGroup(string groupName) => Data == null ? -1 : SubData.IndexOfFatGroup(groupName);

        public int VariationShapesControlDataSize => Data.VariationShapesCount * Data.VariationVertexGroupCount;
        public int FirstVariationShapesControlIndex => InstanceID * VariationShapesControlDataSize;

        public int GetPartialVariationShapeIndex(int variationGroupIndex, int shapeIndex)
        {
            if (variationGroupIndex < 0 || variationGroupIndex >= Data.VariationVertexGroupCount || shapeIndex < 0 || shapeIndex >= Data.VariationShapesCount) return -1;
            return (variationGroupIndex * Data.VariationShapesCount) + shapeIndex;
        }

        public float2 GetVariationWeightUnsafe(int variationShapeIndex, int groupIndex) => VariationShapesControlBuffer[FirstVariationShapesControlIndex + (groupIndex * Data.VariationShapesCount) + variationShapeIndex];//VariationShapesControl[(groupIndex * CharacterMeshData.VariationShapesCount) + variationShapeIndex];
        public float2 GetVariationWeight(int variationShapeIndex, int groupIndex)
        {
            if (instance == null || groupIndex < 0 || groupIndex >= Data.VariationVertexGroupCount || variationShapeIndex < 0 || variationShapeIndex >= Data.VariationShapesCount) return 0;
            return GetVariationWeightUnsafe(variationShapeIndex, groupIndex);
        }
        public void SetVariationWeightUnsafe(int variationShapeIndex, int groupIndex, float2 weight)
        {
            //var array = VariationShapesControl;
            //array[(groupIndex * CharacterMeshData.VariationShapesCount) + variationShapeIndex] = weight;

            if (characterInstanceReference != null)
            {
                characterInstanceReference.SetVariationWeightUnsafe(variationShapeIndex, groupIndex, weight);
                return;
            }
            int variationIndex = (groupIndex * Data.VariationShapesCount) + variationShapeIndex;
            VariationShapesControlBuffer[FirstVariationShapesControlIndex + variationIndex] = weight;

            SyncVariationData(groupIndex, variationShapeIndex, weight.x, weight.y);

            //dirtyFlag_variationShapesControl = true;

            if (instance != null && instance.IsValid)
            {
                instance.SetVariationGroupWeightUnsafe(variationIndex, weight);
                instance.MarkForVariationUpdateUnsafe();
            }
        }
        public void SetVariationWeight(int variationShapeIndex, int groupIndex, float2 weight)
        {
            if (instance == null || groupIndex < 0 || groupIndex >= Data.VariationVertexGroupCount || variationShapeIndex < 0 || variationShapeIndex >= Data.VariationShapesCount) return;
            SetVariationWeightUnsafe(variationShapeIndex, groupIndex, weight);
        }

        public float2 GetVariationWeightUnsafe(int indexInArray) => VariationShapesControlBuffer[FirstVariationShapesControlIndex + indexInArray]; //VariationShapesControl[indexInArray];
        public float2 GetVariationWeight(int indexInArray)
        {
            if (indexInArray < 0 || instance == null || indexInArray >= VariationShapesControlDataSize) return 0;
            return GetVariationWeightUnsafe(indexInArray);
        }
        public void SetVariationWeightUnsafe(int indexInArray, float2 weight) => SetVariationWeightUnsafe(indexInArray % Data.VariationShapesCount, indexInArray / Data.VariationShapesCount, weight);
        public void SetVariationWeight(int indexInArray, float2 weight)
        {
            if (indexInArray < 0 || instance == null || indexInArray >= VariationShapesControlDataSize) return;
            SetVariationWeightUnsafe(indexInArray, weight);
        }

        #endregion

        #region Animation

        [SerializeField]
        protected CustomAvatar avatar;
        public void SetAvatar(CustomAvatar av) => avatar = av;
        public CustomAvatar Avatar => avatar;
        [SerializeField]
        protected Transform rigRoot;
        public void SetRigRoot(Transform root) => rigRoot = root;
        public Transform RigRoot
        {
            get
            {
                if (rigRoot == null)
                {
                    if (avatar != null && !string.IsNullOrWhiteSpace(avatar.rigContainer))
                    {
                        rigRoot = (transform.parent == null ? transform : transform.parent).FindDeepChildLiberal(avatar.rigContainer);
                    }

                    if (rigRoot == null) rigRoot = transform.parent == null ? transform : transform.parent;
                }

                return rigRoot;
            }
        }
        public override Transform BoundsRootTransform => RigRoot;

        [SerializeField]
        protected DynamicAnimationProperties animatablePropertiesController;
        [NonSerialized]
        protected List<DynamicAnimationProperties.Property> dynamicAnimationProperties;
        public void SetAnimatablePropertiesController(DynamicAnimationProperties controller)
        {
            if (dynamicAnimationProperties != null)
            {
                if (animatablePropertiesController != null)
                {
                    foreach (var prop in dynamicAnimationProperties) animatablePropertiesController.RemoveProperty(prop);
                }
                dynamicAnimationProperties.Clear();
            }

            animatablePropertiesController = controller;
            if (animatablePropertiesController == null) return;

#if UNITY_EDITOR
            if (!Application.isPlaying) return;
#endif

            if (dynamicAnimationProperties == null) dynamicAnimationProperties = new List<DynamicAnimationProperties.Property>(); ;

            string id = GetInstanceID().ToString();
            for (int a = 0; a < Data.StandaloneShapesCount; a++)
            {
                var shape = Data.GetStandaloneShape(a);
                if (shape == null || !shape.animatable) continue;

                int shapeIndex = a;

                string name = $"SHAPE:{shape.name}";
                int index = animatablePropertiesController.IndexOf(name);
                if (index >= 0)
                {
                    var prop = animatablePropertiesController.GetPropertyUnsafe(index);
                    prop.Listen(id, (float value) => SetStandaloneShapeWeightUnsafe(shapeIndex, value));
                    continue;
                }

                var prop_ = animatablePropertiesController.CreateProperty(name, () => GetStandaloneShapeWeightUnsafe(shapeIndex), (float value) => SetStandaloneShapeWeightUnsafe(shapeIndex, value));
                dynamicAnimationProperties.Add(prop_);
            }

            var meshData = SubData;
            for (int a = 0; a < meshData.MuscleVertexGroupCount; a++)
            {
                var group = meshData.GetMuscleVertexGroup(a);
                if (group == null || !group.flag) continue; // animatable permission is stored in vertex group flag field

                int shapeIndex = a;

                string name = $"FLEX_LEFT:{group.name}";
                int index = animatablePropertiesController.IndexOf(name);
                if (index >= 0)
                {
                    var prop = animatablePropertiesController.GetPropertyUnsafe(index);
                    prop.Listen(id, (float value) =>
                    {
                        var values = GetMuscleDataUnsafe(shapeIndex);
                        var valuesLeft = values.valuesLeft;
                        valuesLeft.flex = value;
                        values.valuesLeft = valuesLeft;
                        SetMuscleDataUnsafe(shapeIndex, values);
                    });
                }
                else
                {
                    var prop = animatablePropertiesController.CreateProperty(name, () => GetMuscleDataUnsafe(shapeIndex).valuesLeft.flex, (float value) =>
                    {
                        var values = GetMuscleDataUnsafe(shapeIndex);
                        var valuesLeft = values.valuesLeft;
                        valuesLeft.flex = value;
                        values.valuesLeft = valuesLeft;
                        SetMuscleDataUnsafe(shapeIndex, values);
                    });
                    dynamicAnimationProperties.Add(prop);
                }

                name = $"FLEX_RIGHT:{group.name}";
                index = animatablePropertiesController.IndexOf(name);
                if (index >= 0)
                {
                    var prop = animatablePropertiesController.GetPropertyUnsafe(index);
                    prop.Listen(id, (float value) =>
                    {
                        var values = GetMuscleDataUnsafe(shapeIndex);
                        var valuesRight = values.valuesRight;
                        valuesRight.flex = value;
                        values.valuesRight = valuesRight;
                        SetMuscleDataUnsafe(shapeIndex, values);
                    });
                }
                else
                {
                    var prop = animatablePropertiesController.CreateProperty(name, () => GetMuscleDataUnsafe(shapeIndex).valuesLeft.flex, (float value) =>
                    {
                        var values = GetMuscleDataUnsafe(shapeIndex);
                        var valuesRight = values.valuesRight;
                        valuesRight.flex = value;
                        values.valuesRight = valuesRight;
                        SetMuscleDataUnsafe(shapeIndex, values);
                    });
                    dynamicAnimationProperties.Add(prop);
                }
            }
        }

        public override CustomAnimator Animator
        {
            get => animator;
            set
            {
                if (animator != null)
                {
                    animator.RemoveListener(CustomAnimator.BehaviourEvent.OnResetPose, OnAnimatorResetPose);
                }

                animator = value;
                if (animator != null)
                {
                    animator.AddListener(CustomAnimator.BehaviourEvent.OnResetPose, OnAnimatorResetPose);
                }
            }
        }

        protected void OnAnimatorResetPose()
        {
            if (!HasInstance) return;

            for (int a = 0; a < SubData.MuscleVertexGroupCount; a++)
            {
                var data = GetMuscleDataUnsafe(a);
                data.valuesLeft.flex = 0f;
                data.valuesRight.flex = 0f;
                SetMuscleDataUnsafe(a, data);
            }
        }

        [NonSerialized]
        protected Transform[] bones;
        public override Transform[] Bones
        {
            get
            {
                if (bones == null)
                {
                    var rig_root = RigRoot;

                    if (avatar == null)
                    {
                        bones = new Transform[] { rig_root };
                    }
                    else
                    {
                        bones = new Transform[avatar.bones.Length];
                        for (int a = 0; a < bones.Length; a++) bones[a] = rig_root.FindDeepChildLiberal(avatar.bones[a]); 
                    }
                }

                return bones;
            }
        }
        [NonSerialized]
        protected Transform[] skinnedBones;
        public override Transform[] SkinnedBones
        {
            get
            {
                if (skinnedBones == null)
                {
                    var rig_root = RigRoot;

                    if (avatar == null)
                    {
                        skinnedBones = new Transform[] { rig_root };
                    }
                    else
                    {
                        skinnedBones = new Transform[avatar.SkinnedBonesCount];
                        for (int a = 0; a < skinnedBones.Length; a++) 
                        {
                            skinnedBones[a] = rig_root.FindDeepChildLiberal(avatar.bones[a]);
                        }
                    }
                }

                return skinnedBones;
            }
        }


        public override int BoneCount => avatar == null ? 1 : avatar.bones.Length;

        public override Matrix4x4[] BindPose => SubData.ManagedBindPose;

        #endregion

        #region IDs

        [NonSerialized]
        protected string rigID;
        public override string RigID // => rigRoot.GetInstanceID().ToString(); // some renderers do not have the same bone/bindpose array as others, so this causes problems. Instead we'll generate a new rig id unless a rig instance reference is given.
        {
            get
            {
                if (RigInstanceReferenceIsValid) return rigInstanceReference.RigID;

                if (string.IsNullOrWhiteSpace(rigID))
                {
                    rigID = System.Guid.NewGuid().ToString();
                    while (Rigs.TryGetStandaloneSampler(RigID, out _))
                    {
                        rigID = System.Guid.NewGuid().ToString();
                    }
                }

                return rigID;
            }
        }

        [SerializeField]
        protected string shapeBufferId;
        public void SetShapeBufferID(string id) => shapeBufferId = id;
        public string LocalShapeBufferID => shapeBufferId;
        public override string ShapeBufferID => shapesInstanceReference != null ? shapesInstanceReference.ShapeBufferID : shapeBufferId;

        [SerializeField]
        protected string morphBufferId;
        public void SetMorphBufferID(string id) => morphBufferId = id;
        public string LocalMorphBufferID => morphBufferId;
        public virtual string MorphBufferID => characterInstanceReference != null ? characterInstanceReference.MorphBufferID : morphBufferId;

        public CustomizableCharacterMeshV2 shapesInstanceReference;
        public int ShapesInstanceID => shapesInstanceReference == null ? InstanceSlot : shapesInstanceReference.ShapesInstanceID;

        public InstanceableSkinnedMeshBase rigInstanceReference;
        public bool RigInstanceReferenceIsValid => rigInstanceReference != null && rigInstanceReference.SkinningBoneCount == SkinningBoneCount;
        public override int RigInstanceID => rigInstanceReference == null ? InstanceSlot : rigInstanceReference.RigInstanceID;
        public void SetRigBufferID(string id) { }
        public string LocalRigBufferID => string.Empty;
        public override string RigBufferID => RigInstanceReferenceIsValid ? rigInstanceReference.RigBufferID : LocalRigBufferID; 

        public override Rigs.StandaloneSampler RigSampler
        {
            get
            {
                if (!RigInstanceReferenceIsValid) return base.RigSampler;
                return rigInstanceReference.RigSampler;
            }
        }

        public CustomizableCharacterMeshV2 characterInstanceReference;
        public int CharacterInstanceID => characterInstanceReference == null ? InstanceSlot : characterInstanceReference.CharacterInstanceID;

        public void SetShapesInstanceID(int id)
        {
            if (id < 0)
            {
                SetFloatOverride(SubData.ShapesInstanceIDPropertyName, InstanceID);
            }
            else
            {
                SetFloatOverride(SubData.ShapesInstanceIDPropertyName, id);
            }

        }
        public void SetRigInstanceID(int id)
        {
            if (id < 0)
            {
                SetFloatOverride(SubData.RigInstanceIDPropertyName, InstanceID);
            }
            else
            {
                SetFloatOverride(SubData.RigInstanceIDPropertyName, id);
            }
        }
        public void SetCharacterInstanceID(int id)
        {
            if (id < 0)
            {
                SetFloatOverride(SubData.CharacterInstanceIDPropertyName, InstanceID);
            }
            else
            {
                SetFloatOverride(SubData.CharacterInstanceIDPropertyName, id);
            }
        }

        [Obsolete]
        protected virtual void InitInstanceIDs()
        {
            if (shapesInstanceReference != null)
            {
                SetShapesInstanceID(shapesInstanceReference.InstanceSlot);
                shapesInstanceReference.OnCreateInstanceID += SetShapesInstanceID;
            }

            if (RigInstanceReferenceIsValid)
            {
                SetRigInstanceID(rigInstanceReference.InstanceSlot);
                rigInstanceReference.OnCreateInstanceID += SetRigInstanceID;
            }
            else if (rigInstanceReference != null)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"rigInstanceReference '{rigInstanceReference.name}' for '{name}' does not have identical skinning bone count ({rigInstanceReference.SkinningBoneCount}:{SkinningBoneCount}) and will be ignored");
#endif
            }

            if (characterInstanceReference != null)
            {
                SetCharacterInstanceID(characterInstanceReference.InstanceSlot);
                characterInstanceReference.OnCreateInstanceID += SetCharacterInstanceID;

                characterInstanceReference.AddChild(this);
            }
        }

        #endregion

        #region Buffers

        protected void InitBuffers()
        {
            standaloneShapeControlBuffer = StandaloneShapeControlBuffer;
            muscleGroupsControlBuffer = MuscleGroupsControlBuffer;
            fatGroupsControlBuffer = FatGroupsControlBuffer;
            variationShapesControlBuffer = VariationShapesControlBuffer;

            if (instance != null)
            {
                var meshData = SubData;

                if (shapesInstanceReference == null)
                {
                    if (meshData.StandaloneShapesCount > 0) SetStandaloneShapeWeightUnsafe(0, 0);
                }

                if (characterInstanceReference == null)
                {

                    if (meshData.MuscleVertexGroupCount > 0) SetMuscleDataUnsafe(0, new MuscleDataLR());
                    if (meshData.FatVertexGroupCount > 0) // Apply fat group modifiers (.y controls how much mass is nerfed by fat)
                    {
                        int indexStart = FirstFatGroupsControlIndex;
                        if (meshData.fatGroupModifiers == null)
                        {
                            for (int a = 0; a < meshData.FatVertexGroupCount; a++) fatGroupsControlBuffer.WriteToBufferFast(indexStart + a, new float3(0, _defaultFatGroupModifier.x, _defaultFatGroupModifier.y));
                        }
                        else
                        {
                            for (int a = 0; a < meshData.FatVertexGroupCount; a++)
                            {
                                var modifier = meshData.GetFatGroupModifier(a);
                                fatGroupsControlBuffer.WriteToBufferFast(indexStart + a, new float3(0, modifier.x, modifier.y));
                            }
                        }

                        fatGroupsControlBuffer.TrySetWriteIndices(indexStart, Data.FatVertexGroupCount);
                        fatGroupsControlBuffer.RequestUpload();
                    }
                    if (data.VariationVertexGroupCount > 0) SetVariationWeightUnsafe(0, 0);
                }
            }
        }
        
        public override InstanceBuffer<float4x4> SkinningMatricesBuffer
        {
            get
            {
                if ((skinningMatricesBuffer == null || !skinningMatricesBuffer.IsValid()) && !IsDestroyed)
                {
                    if (rigInstanceReference != null)
                    {
                        skinningMatricesBuffer = rigInstanceReference.SkinningMatricesBuffer;
                    }
                    else
                    {
                        if (instance != null && instance.IsValid)
                        {
                            string matricesProperty = SubData.SkinningMatricesPropertyName;
                            if (!instance.OwnerGroup.TryGetInstanceBuffer<float4x4>(matricesProperty, out skinningMatricesBuffer))
                            {
                                instance.OwnerGroup.CreateInstanceMaterialBuffer<float4x4>(matricesProperty, SkinningBoneCount, 3, false, out skinningMatricesBuffer);
                            }
                        }
                    }
                }

                return skinningMatricesBuffer;
            }
        }

        protected InstanceBuffer<float> standaloneShapeControlBuffer;
        public InstanceBuffer<float> StandaloneShapeControlBuffer
        {
            get
            {
                if ((standaloneShapeControlBuffer == null || !standaloneShapeControlBuffer.IsValid()) && !IsDestroyed)
                {
                    if (shapesInstanceReference != null)
                    {
                        standaloneShapeControlBuffer = shapesInstanceReference.StandaloneShapeControlBuffer;
                    }
                    else
                    {
                        if (instance != null && instance.IsValid)
                        {
                            string matProperty = SubData.StandaloneShapesControlPropertyName;
                            if (!instance.OwnerGroup.TryGetInstanceBuffer<float>(matProperty, out standaloneShapeControlBuffer))
                            {
                                instance.OwnerGroup.CreateInstanceMaterialBuffer<float>(matProperty, SubData.StandaloneShapesCount, 2, false, out standaloneShapeControlBuffer);
                            }
                        }
                    }
                }

                return standaloneShapeControlBuffer;
            }
        }

        protected InstanceBuffer<MuscleDataLR> muscleGroupsControlBuffer;
        public InstanceBuffer<MuscleDataLR> MuscleGroupsControlBuffer
        {
            get
            {
                if ((muscleGroupsControlBuffer == null || !muscleGroupsControlBuffer.IsValid()) && !IsDestroyed)
                {
                    if (characterInstanceReference != null)
                    {
                        muscleGroupsControlBuffer = characterInstanceReference.MuscleGroupsControlBuffer;
                    }
                    else
                    {
                        if (instance != null && instance.IsValid)
                        {
                            string matProperty = SubData.MuscleGroupsControlPropertyName;
                            if (!instance.OwnerGroup.TryGetInstanceBuffer<MuscleDataLR>(matProperty, out muscleGroupsControlBuffer))
                            {
                                instance.OwnerGroup.CreateInstanceMaterialBuffer<MuscleDataLR>(matProperty, SubData.MuscleVertexGroupCount, 2, false, out muscleGroupsControlBuffer);
                            }
                        }
                    }
                }

                return muscleGroupsControlBuffer;
            }
        }

        protected InstanceBuffer<float3> fatGroupsControlBuffer;
        public InstanceBuffer<float3> FatGroupsControlBuffer
        {
            get
            {
                if ((fatGroupsControlBuffer == null || !fatGroupsControlBuffer.IsValid()) && !IsDestroyed)
                {
                    if (characterInstanceReference != null)
                    {
                        fatGroupsControlBuffer = characterInstanceReference.FatGroupsControlBuffer;
                    }
                    else
                    {
                        if (instance != null && instance.IsValid)
                        {
                            string matProperty = SubData.FatGroupsControlPropertyName;
                            if (!instance.OwnerGroup.TryGetInstanceBuffer<float3>(matProperty, out fatGroupsControlBuffer))
                            {
                                instance.OwnerGroup.CreateInstanceMaterialBuffer<float3>(matProperty, SubData.FatVertexGroupCount, 2, false, out fatGroupsControlBuffer);
                            }
                        }
                    }
                }

                return fatGroupsControlBuffer;
            }
        }

        protected InstanceBuffer<float2> variationShapesControlBuffer;
        public InstanceBuffer<float2> VariationShapesControlBuffer
        {
            get
            {
                if ((variationShapesControlBuffer == null || !variationShapesControlBuffer.IsValid()) && !IsDestroyed)
                {
                    if (characterInstanceReference != null)
                    {
                        variationShapesControlBuffer = characterInstanceReference.VariationShapesControlBuffer;
                    }
                    else
                    {
                        if (instance != null && instance.IsValid)
                        {
                            string matProperty = SubData.VariationShapesControlPropertyName;
                            if (!instance.OwnerGroup.TryGetInstanceBuffer<float2>(matProperty, out variationShapesControlBuffer))
                            {
                                instance.OwnerGroup.CreateInstanceMaterialBuffer<float2>(matProperty, SubData.VariationShapesCount * SubData.VariationVertexGroupCount, 2, false, out variationShapesControlBuffer);
                            }
                        }
                    }
                }

                return variationShapesControlBuffer;
            }
        }

        #endregion

        #region Events

        public void AddListener(ICustomizableCharacter.ListenableEvent event_, UnityAction<int> listener)
        {
            switch (event_)
            {
                case ICustomizableCharacter.ListenableEvent.OnMuscleDataChanged:
                    if (OnMuscleDataChanged == null) OnMuscleDataChanged = new UnityEvent<int>();
                    OnMuscleDataChanged.AddListener(listener);
                    break;
                case ICustomizableCharacter.ListenableEvent.OnFatDataChanged:
                    if (OnFatDataChanged == null) OnFatDataChanged = new UnityEvent<int>();
                    OnFatDataChanged.AddListener(listener);
                    break;
            }
        }
        public void RemoveListener(ICustomizableCharacter.ListenableEvent event_, UnityAction<int> listener)
        {
            switch (event_)
            {
                case ICustomizableCharacter.ListenableEvent.OnMuscleDataChanged:
                    if (OnMuscleDataChanged != null) OnMuscleDataChanged.RemoveListener(listener);
                    break;
                case ICustomizableCharacter.ListenableEvent.OnFatDataChanged:
                    if (OnFatDataChanged != null) OnFatDataChanged.RemoveListener(listener);
                    break;
            }
        }
        public void ClearListeners()
        {
            if (OnMuscleDataChanged != null) OnMuscleDataChanged.RemoveAllListeners();
            if (OnFatDataChanged != null) OnFatDataChanged.RemoveAllListeners();
        }

        #endregion

    }

}

#endif