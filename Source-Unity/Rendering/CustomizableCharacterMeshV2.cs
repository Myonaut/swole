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

using static Swole.API.Unity.ICustomizableCharacter.Defaults;

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
                if (meshData != null)
                {

                    if (prevBustSizeEditor != bustSizeEditor)
                    {
                        prevBustSizeEditor = bustSizeEditor;
                        SetBustSize(bustSizeEditor);
                    }
                    if (prevBustShapeEditor != bustShapeEditor)
                    {
                        prevBustShapeEditor = bustShapeEditor;
                        SetBustShape(bustShapeEditor);
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

                    if (prevBodyHairWeightsEditor == null || prevBodyHairWeightsEditor.Length == 0)
                    {
                        prevBodyHairWeightsEditor = new NamedFloat2[meshData.FatVertexGroupCount];
                        for (int a = 0; a < prevBodyHairWeightsEditor.Length; a++) prevBodyHairWeightsEditor[a] = new NamedFloat2() { name = meshData.GetVertexGroup(a + meshData.fatGroups.x).name };
                    }
                    if (bodyHairWeightsEditor == null || bodyHairWeightsEditor.Length == 0)
                    {
                        bodyHairWeightsEditor = new NamedFloat2[meshData.FatVertexGroupCount];
                        for (int a = 0; a < bodyHairWeightsEditor.Length; a++) bodyHairWeightsEditor[a] = new NamedFloat2() { name = meshData.GetVertexGroup(a + meshData.fatGroups.x).name };
                    }

                    if (prevVariationWeightsEditor == null || prevVariationWeightsEditor.Length == 0)
                    {
                        prevVariationWeightsEditor = new NamedFloat2[VariationShapesControlDataSize];
                        for (int a = 0; a < prevVariationWeightsEditor.Length; a++)
                        {
                            int groupIndex = a / meshData.VariationShapesCount;
                            int shapeIndex = a % meshData.VariationShapesCount;
                            prevVariationWeightsEditor[a] = new NamedFloat2() { name = meshData.GetVariationVertexGroup(groupIndex).name + "_" + meshData.GetVariationShape(shapeIndex).name };
                        }
                    }
                    if (variationWeightsEditor == null || variationWeightsEditor.Length == 0)
                    {
                        variationWeightsEditor = new NamedFloat2[VariationShapesControlDataSize];
                        for (int a = 0; a < variationWeightsEditor.Length; a++)
                        {
                            int groupIndex = a / meshData.VariationShapesCount;
                            int shapeIndex = a % meshData.VariationShapesCount;
                            variationWeightsEditor[a] = new NamedFloat2() { name = meshData.GetVariationVertexGroup(groupIndex).name + "_" + meshData.GetVariationShape(shapeIndex).name };
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
                    if (prevGlobalVaricose != globalVaricose)
                    {
                        prevGlobalVaricose = globalVaricose;
                        for (int a = 0; a < muscleWeightsEditor.Length; a++)
                        {
                            var values = muscleWeightsEditor[a].value;
                            var vl = values.valuesLeft;
                            var vr = values.valuesRight;
                            vl.varicose = globalVaricose;
                            vr.varicose = globalVaricose;
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

                    if (prevGlobalBodyHairLevel != globalBodyHairLevel || prevGlobalBodyHairBlend != globalBodyHairBlend)
                    {
                        prevGlobalBodyHairLevel = globalBodyHairLevel;
                        prevGlobalBodyHairBlend = globalBodyHairBlend;
                        for (int a = 0; a < bodyHairWeightsEditor.Length; a++)
                        {
                            SetBodyHairLevelUnsafe(a, globalBodyHairLevel, globalBodyHairBlend);

                            prevBodyHairWeightsEditor[a].value = bodyHairWeightsEditor[a].value = new float2(globalBodyHairLevel, globalBodyHairBlend);
                        }
                    }

                    if (prevGlobalVariationA != globalVariationA && meshData.VariationShapesCount > 0)
                    {
                        prevGlobalVariationA = globalVariationA;
                        for (int a = 0; a < variationWeightsEditor.Length; a += meshData.VariationShapesCount)
                        {
                            SetVariationWeightUnsafe(a, globalVariationA);

                            prevVariationWeightsEditor[a].value = variationWeightsEditor[a].value = globalVariationA;
                        }
                    }
                    if (prevGlobalVariationB != globalVariationB && meshData.VariationShapesCount > 1)
                    {
                        prevGlobalVariationB = globalVariationB;
                        for (int a = 1; a < variationWeightsEditor.Length; a += meshData.VariationShapesCount)
                        {
                            SetVariationWeightUnsafe(a, globalVariationB);

                            prevVariationWeightsEditor[a].value = variationWeightsEditor[a].value = globalVariationB;
                        }
                    }
                    if (prevGlobalVariationC != globalVariationC && meshData.VariationShapesCount > 2)
                    {
                        prevGlobalVariationC = globalVariationC;
                        for (int a = 2; a < variationWeightsEditor.Length; a += meshData.VariationShapesCount)
                        {
                            SetVariationWeightUnsafe(a, globalVariationC);

                            prevVariationWeightsEditor[a].value = variationWeightsEditor[a].value = globalVariationC;
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

                    for (int a = 0; a < bodyHairWeightsEditor.Length; a++)
                    {
                        if (math.any(prevBodyHairWeightsEditor[a].value != bodyHairWeightsEditor[a].value))
                        {
                            SetBodyHairLevelUnsafe(a, bodyHairWeightsEditor[a].value.x, bodyHairWeightsEditor[a].value.y);
                            prevBodyHairWeightsEditor[a].value = bodyHairWeightsEditor[a].value;
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

        [Serializable, NonAnimatable]
        public struct NamedFloat
        {
            public string name;
            public float value;
        }
        [Serializable, NonAnimatable]
        public struct NamedFloat2
        {
            public string name;
            public float2 value;
        }
        [Serializable, NonAnimatable]
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
        [Range(0f, 2f)]
        private float globalPump;

        private float prevGlobalVaricose;
#if UNITY_EDITOR
        [SerializeField]
#endif
        [Range(0f, 2f)]
        private float globalVaricose;

        private float prevGlobalFat;
#if UNITY_EDITOR
        [SerializeField]
#endif
        [Range(0f, 1f)]
        private float globalFat;

        private float prevGlobalBodyHairLevel;
#if UNITY_EDITOR
        [SerializeField]
#endif
        [Range(0f, 1f)]
        private float globalBodyHairLevel;
        private float prevGlobalBodyHairBlend;
#if UNITY_EDITOR
        [SerializeField]
#endif
        [Range(0f, 1f)]
        private float globalBodyHairBlend;

        private float prevGlobalVariationA;
#if UNITY_EDITOR
        [SerializeField]
#endif
        [Range(0f, 1f)]
        private float globalVariationA;

        private float prevGlobalVariationB;
#if UNITY_EDITOR
        [SerializeField]
#endif
        [Range(0f, 1f)]
        private float globalVariationB;

        private float prevGlobalVariationC;
#if UNITY_EDITOR
        [SerializeField]
#endif
        [Range(0f, 1f)]
        private float globalVariationC;

        private float prevBustSizeEditor;
#if !UNITY_EDITOR
        [NonSerialized]
#endif
        [Range(0, 2)]
        public float bustSizeEditor;

        private float prevBustShapeEditor;
#if !UNITY_EDITOR
        [NonSerialized]
#endif
        [Range(0, 2)]
        public float bustShapeEditor;

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
        public NamedFloat2[] prevBodyHairWeightsEditor;
#if !UNITY_EDITOR
        [NonSerialized]
#endif
        public NamedFloat2[] bodyHairWeightsEditor;

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

        #region Static Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CalculateBustNerfFactor(float bustSize, NativeArray<float> vertexGroups, int bustNerfVertexGroupIndex, int vertexCount, int vertexIndex, float multiplier = 1f)
        {
            float bustNerfFactor = math.saturate(bustSize * math.min(1f, math.pow(vertexGroups[(bustNerfVertexGroupIndex * vertexCount) + vertexIndex], 0.5f)));
            return 1f - (bustNerfFactor * multiplier);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CalculateFinalFlexFactor(float initialFlexFactor, float muscleMass, float flexEndPointWeight, float flexExp, float flexNerfThreshold, float flexNerfExp)
        {
            float flexFactor = (initialFlexFactor / (flexEndPointWeight <= 0f ? 1f : flexEndPointWeight)) * math.pow(math.saturate(muscleMass / (flexNerfThreshold <= 0f ? 0.35f : flexNerfThreshold)), flexNerfExp <= 0f ? 1f : flexNerfExp); // nerf flex for smaller masses
            flexFactor = math.pow(math.saturate(flexFactor), flexExp <= 0f ? 1f : flexExp);

            return flexFactor;
        }

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
                Debug.Log($"{MaxInstanceCount} -- {finalVertexDeltasBuffer.InstanceCount}"); 
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
        public struct ControlWeight4
        {
            public int index;
            public float4 weight;
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
        public struct GroupControlWeight4
        {
            public int groupIndex;
            public int vertexSequenceStartIndex;
            public int vertexCount;
            public float4 weight;
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
        [Serializable, StructLayout(LayoutKind.Sequential)]
        public struct GroupVertexControlWeight4
        {
            public int groupIndex;
            public int vertexIndex;

            public float4 weight;
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

        [Serializable, NonAnimatable]
        public struct RenderSet
        {
            public int materialIndexStart;
            public int materialCount;
        }

        [Serializable, NonAnimatable]
        public class SerializedData : IDisposable
        {

            #region Fields

            [Header("Rendering")]
            public Material[] materials;
            [Tooltip("Render sets render the same mesh instance using different materials (useful for toon outline materials)")]
            public RenderSet[] renderSets;
            public bool HasRenderSets => renderSets != null && renderSets.Length > 1;

            public int vertexCount;

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
                        for(int i = 0; i < array.Length; i++)
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

                switch(channel)
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
            [SerializeField]
            public Vector3 boundsExtents;

            [HideInInspector]
            public bool[] leftRightFlags;

            [HideInInspector]
            public BoneWeight8[] baseBoneWeights;

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

            [HideInInspector]
            public Matrix4x4[] baseBindPose;
            public int BoneCount => HasBonesArray ? boneNames.Length : (baseBindPose == null ? 0 : baseBindPose.Length);

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
            public int bustShapeShape;
            public int bustSizeMuscleShape;

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

            [Header("Vertex Color Deltas")]
            public VertexColorDelta[] vertexColorDeltas; 

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
            public float flexExponent;
            public float flexNerfThreshold = 0.35f;
            public float flexNerfExponent = 1f;
            public int raycastLod;
            [Tooltip("The uv channel to use for determining the nearest vertex.")]
            public UVChannelURP nearestVertexUVChannel = UVChannelURP.UV3;
            [Tooltip("The uv element to store the nearest vertex index in.")]
            public RGBAChannel nearestVertexIndexElement = RGBAChannel.R;

            public DefaultMuscleGroupConversion[] defaultMuscleGroupConversions;
            private Dictionary<MuscleGroupsDefault, int> defaultMuscleGroupConversionsCache;
            private Dictionary<string, MuscleGroupsDefault> defaultMuscleGroupConversionsReverseCache;
            private Dictionary<MuscleGroup, int> defaultBaseMuscleGroupConversionsCache;
            private Dictionary<VertexGroup, MuscleGroup> defaultBaseMuscleGroupConversionsReverseCache;

            [NonSerialized]
            private bool initializedDefaultMuscleGroupConversions = false;

            private void InitializeDefaultMuscleGroupConversions()
            {
                if (initializedDefaultMuscleGroupConversions) return;

                initializedDefaultMuscleGroupConversions = true;
                defaultMuscleGroupConversionsCache = new Dictionary<MuscleGroupsDefault, int>();
                defaultMuscleGroupConversionsReverseCache = new Dictionary<string, MuscleGroupsDefault>();
                defaultBaseMuscleGroupConversionsCache = new Dictionary<MuscleGroup, int>();
                defaultBaseMuscleGroupConversionsReverseCache = new Dictionary<VertexGroup, MuscleGroup>();

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

                        var vertexGroup = GetMuscleVertexGroup(ind);

                        defaultBaseMuscleGroupConversionsCache[conversion.basicMuscleGroup] = ind;
                        defaultBaseMuscleGroupConversionsReverseCache[vertexGroup] = conversion.basicMuscleGroup; 

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
                if (defaultMuscleGroupConversionsCache.TryGetValue(defaultGroup, out int groupIndex)) return GetMuscleVertexGroup(groupIndex).name; 
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

                var vg = GetMuscleVertexGroup(muscleGroupIndex);
                if (vg == null) return default;

                if (defaultMuscleGroupConversionsReverseCache.TryGetValue(vg.name + Side.Left.AsSuffix(), out var defaultGroup)) return defaultGroup;

                return default;
            }
            public MuscleGroupsDefault ConvertMuscleGroupIndexToDefault(int muscleGroupIndex)
            {
                if (muscleGroupIndex < 0) return default;
                
                if (!initializedDefaultMuscleGroupConversions) InitializeDefaultMuscleGroupConversions(); 

                muscleGroupIndex = ConvertDefaultMuscleGroupIndexToLocal(muscleGroupIndex, out int defaultIndex, out bool isBothSides);

                var vg = GetMuscleVertexGroup(muscleGroupIndex);
                if (vg == null) return default;

                bool isLeft = isBothSides || defaultIndex % 2 == 0;

                if (defaultMuscleGroupConversionsReverseCache.TryGetValue(vg.name + (isLeft ? Side.Left : Side.Right).AsSuffix(), out var defaultGroup)) return defaultGroup;

                return default; 
            }

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
            [NonSerialized]
            protected ComputeBuffer fatGroupInfluencesBuffer;
            public ComputeBuffer FatGroupInfluencesBuffer
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

            public string VertexGroupsBufferRangePropertyName => string.IsNullOrWhiteSpace(standaloneVertexGroupsBufferRangePropertyNameOverride) ? _vertexGroupsBufferRangeDefaultPropertyName : standaloneVertexGroupsBufferRangePropertyNameOverride;
            public int StandaloneGroupsCount => standaloneGroups.y < standaloneGroups.x ? 0 : ((standaloneGroups.y - standaloneGroups.x) + 1);
            public int StandaloneVertexGroupCount => StandaloneGroupsCount;
            public int IndexOfStandaloneVertexGroup(string name, bool caseSensitive = false)
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

            public string MuscleVertexGroupsBufferRangePropertyName => string.IsNullOrWhiteSpace(muscleVertexGroupsBufferRangePropertyNameOverride) ? _muscleVertexGroupsBufferRangeDefaultPropertyName : muscleVertexGroupsBufferRangePropertyNameOverride;
            public int MuscleGroupsCount => muscleGroups.y < muscleGroups.x ? 0 : ((muscleGroups.y - muscleGroups.x) + 1);
            public int MuscleVertexGroupCount => MuscleGroupsCount;
            public int IndexOfMuscleGroup(string name, bool caseSensitive = false)
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

            public string FatVertexGroupsBufferRangePropertyName => string.IsNullOrWhiteSpace(fatVertexGroupsBufferRangePropertyNameOverride) ? _fatVertexGroupsBufferRangeDefaultPropertyName : fatVertexGroupsBufferRangePropertyNameOverride;
            public int FatGroupsCount => fatGroups.y < fatGroups.x ? 0 : ((fatGroups.y - fatGroups.x) + 1);
            public int FatVertexGroupCount => FatGroupsCount;
            public int IndexOfFatGroup(string name, bool caseSensitive = false)
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
           
            public int IndexOfVariationGroup(string name, bool caseSensitive = false)
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

            public int VariationShapesControlDataSize => VariationShapesCount * VariationVertexGroupCount;


            public string standaloneShapesBufferRangePropertyNameOverride;
            public string StandaloneShapesBufferRangePropertyName => string.IsNullOrWhiteSpace(standaloneShapesBufferRangePropertyNameOverride) ? _standaloneShapesBufferRangeDefaultPropertyName : standaloneShapesBufferRangePropertyNameOverride;
            public int StandaloneShapesCount => standaloneShapes.y < standaloneShapes.x ? 0 : ((standaloneShapes.y - standaloneShapes.x) + 1);
            public int IndexOfStandaloneShape(string name, bool caseSensitive = false)
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

            public MeshShape BustSizeMuscleShape => bustSizeMuscleShape >= 0 && meshShapes != null ? meshShapes[bustSizeMuscleShape] : null;
            public int BustSizeMuscleShapeFrameCount => bustSizeMuscleShape >= 0 && meshShapes != null ? meshShapes[bustSizeMuscleShape].FrameCount : 0;


            public string variationShapesBufferRangePropertyNameOverride;
            public string VariationShapesBufferRangePropertyName => string.IsNullOrWhiteSpace(variationShapesBufferRangePropertyNameOverride) ? _variationShapesBufferRangeDefaultPropertyName : variationShapesBufferRangePropertyNameOverride;
            public int VariationShapesCount => variationShapes.y < variationShapes.x ? 0 : ((variationShapes.y - variationShapes.x) + 1);
            public int IndexOfVariationShape(string name, bool caseSensitive = false)
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



            protected static readonly List<float> tempFloats = new List<float>();
            [NonSerialized]
            protected ComputeBuffer vertexGroupsBuffer;
            public ComputeBuffer VertexGroupsBuffer
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

            protected static readonly List<MorphShapeVertex> tempFrameDeltas = new List<MorphShapeVertex>();
            [NonSerialized]
            protected ComputeBuffer meshShapeFrameDeltasBuffer;
            public ComputeBuffer MeshShapeFrameDeltasBuffer
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

            protected static readonly List<float4> tempColorDeltas = new List<float4>();
            [NonSerialized]
            protected ComputeBuffer vertexColorDeltasBuffer;
            public ComputeBuffer VertexColorDeltasBuffer
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

            public void PrecacheMuscleGroupInfluences()
            {
                if (precache_muscleGroupInfluences != null && precache_muscleGroupInfluences.Length > 0) return;

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
            }

            public void PrecacheFatGroupInfluences()
            {
                if (precache_fatGroupInfluences != null && precache_fatGroupInfluences.Length > 0) return;

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
            }

            [SerializeField, HideInInspector]
            public MorphShapeVertex[] precache_meshShapeFrameDeltas;

            public void PrecacheMeshShapeFrameDeltas()
            {
                if (precache_meshShapeFrameDeltas != null && precache_meshShapeFrameDeltas.Length > 0) return;

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
            }

            [SerializeField, HideInInspector]
            public float4[] precache_vertexColorDeltas;

            public void PrecacheVertexColorDeltas()
            {
                if (precache_vertexColorDeltas != null && precache_vertexColorDeltas.Length > 0) return;

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
            }

            public bool IsPrecached => (precache_meshShapeDeltas != null && precache_meshShapeDeltas.Length != 0)
                && (precache_meshShapeFrameWeights != null && precache_meshShapeFrameWeights.Length != 0)
                && (precache_meshShapeInfos != null && precache_meshShapeInfos.Length != 0)
                && (precache_vertexGroups != null && precache_vertexGroups.Length != 0)
                && (precache_blankMuscleGroupControlWeights != null && precache_blankMuscleGroupControlWeights.Length != 0)
                && (precache_muscleGroupVertexWeights != null && precache_muscleGroupVertexWeights.Length != 0)
                && (precache_blankFatGroupControlWeights != null && precache_blankFatGroupControlWeights.Length != 0)
                && (precache_fatGroupVertexWeights != null && precache_fatGroupVertexWeights.Length != 0)
                && (precache_blankVariationGroupControlWeights != null && precache_blankVariationGroupControlWeights.Length != 0)
                && (precache_variationGroupVertexWeights != null && precache_variationGroupVertexWeights.Length != 0);

            public void TryPrecache()
            {
                if (IsPrecached) return;
                Precache();
            }
            private static readonly List<GroupControlWeight2> tempGroupControlWeights = new List<GroupControlWeight2>();
            private static readonly List<GroupVertexControlWeight> tempGroupVertexWeights = new List<GroupVertexControlWeight>();
            public void Precache()
            {
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
                }

                #endregion

                #region Muscle Group Influences

                PrecacheMuscleGroupInfluences();

                #endregion

                #region Fat Group Influences

                PrecacheFatGroupInfluences();

                #endregion

                PrecacheMeshShapeFrameDeltas();

                PrecacheVertexColorDeltas();

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
            if (Data == null || syncedSkinnedMeshes == null) return;

            if (standaloneShapeSyncs == null || standaloneShapeSyncs.Length != Data.StandaloneShapesCount) standaloneShapeSyncs = new List<BlendShapeSync>[Data.StandaloneShapesCount];
            if (muscleMassShapeSyncs == null || muscleMassShapeSyncs.Length != Data.MassShapeFrameCount * Data.MuscleVertexGroupCount) muscleMassShapeSyncs = new List<BlendShapeSyncLR>[Data.MassShapeFrameCount * Data.MuscleVertexGroupCount];
            if (muscleFlexShapeSyncs == null || muscleFlexShapeSyncs.Length != Data.FlexShapeFrameCount * Data.MuscleVertexGroupCount) muscleFlexShapeSyncs = new List<BlendShapeSyncLR>[Data.FlexShapeFrameCount * Data.MuscleVertexGroupCount];
            if (fatShapeSyncs == null || fatShapeSyncs.Length != Data.FatShapeFrameCount * Data.FatVertexGroupCount) fatShapeSyncs = new List<BlendShapeSync>[Data.FatShapeFrameCount * Data.FatVertexGroupCount];
            if (variationShapeSyncs == null || variationShapeSyncs.Length != VariationShapesControlDataSize) variationShapeSyncs = new List<BlendShapeSyncLR>[VariationShapesControlDataSize];

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
            var list = variationShapeSyncs[GetPartialVariationShapeIndexUnsafe(groupIndex, shapeIndex)];
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

            StopRendering();

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
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();

            if (characterInstanceReference != null)
            {
                characterInstanceReference.RemoveChild(this);
            }

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

        /// <summary>
        /// Handles physique update jobs and buffer writes
        /// </summary>
        new protected InstanceV2 instance;
        public InstanceV2 Instance2 => instance;
        public MeshGroupV2 MeshGroup2 => instance == null ? null : instance.OwnerGroup;
        public int InstanceID => instance == null ? 0 : instance.localID;
        public override int InstanceSlot => InstanceID;
        public override bool IsInitialized => instance != null && instance.IsValid;

        public UnityEvent<InstanceV2> OnClaimInstance = new UnityEvent<InstanceV2>();

        protected override void CreateInstance()
        {
            if (instance != null && instance.IsValid) return;

            instance = Updater.Register(Data);
            instance.updateManually = updateMeshManually;

            OnClaimInstance?.Invoke(instance);
        }
        protected override void CreateInstance(List<InstancedRendering.MaterialPropertyInstanceOverride<float>> floatOverrides, List<InstancedRendering.MaterialPropertyInstanceOverride<Color>> colorOverrides, List<InstancedRendering.MaterialPropertyInstanceOverride<Vector4>> vectorOverrides)
        {
            CreateInstance(); 
        }

        public string Name => name;

        public GameObject GameObject => gameObject;

        protected override void OnAwake()
        {

            autoCreateInstance = false;  

            base.OnAwake();

            if (data != null) 
            { 
                SetData(data);

                bones = Bones; // force init bones
                skinnedBones = SkinnedBones; // force init skinned bones
            }

            if (animatablePropertiesController == null) animatablePropertiesController = gameObject.GetComponent<DynamicAnimationProperties>();
            if (animatablePropertiesController == null && transform.parent != null) animatablePropertiesController = transform.parent.GetComponent<DynamicAnimationProperties>(); 
            SetAnimatablePropertiesController(animatablePropertiesController);

            Animator = animator; // force subscribe listeners

            if (characterInstanceReference != null) CharacterInstanceReference = characterInstanceReference; 
            if (rigInstanceReference != null) RigInstanceReference = rigInstanceReference;
            if (shapesInstanceReference != null) ShapesInstanceReference = shapesInstanceReference;  

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
        public void SetData(CustomizableCharacterMeshV2_DATA data) 
        { 
            var prevData = this.data;
            this.data = data;

            if (Application.isPlaying)
            {
                SetupSkinnedMeshSyncs();
                if (enabled) StartRendering();
            }
        }
        public CustomizableCharacterMeshV2_DATA Data => data;
        public SerializedData SubData => data == null ? null : data.SerializedData;


        public bool TryGetVertices(int lod, out NativeArray<float3> array) => data.TryGetVertices(lod, out array);
        public bool TryGetColors(int lod, out NativeArray<float4> array) => data.TryGetColors(lod, out array);
        public bool TryGetTriangles(int lod, out NativeArray<int> array) => data.TryGetTriangles(lod, out array);
        public bool TryGetBoneWeights(int lod, out NativeArray<BoneWeight8> array) => data.TryGetBoneWeights(lod, out array);

        public bool TryGetUV0(int lod, out NativeArray<float4> array) => data.TryGetUV0(lod, out array);
        public bool TryGetUV1(int lod, out NativeArray<float4> array) => data.TryGetUV1(lod, out array);
        public bool TryGetUV2(int lod, out NativeArray<float4> array) => data.TryGetUV2(lod, out array);
        public bool TryGetUV3(int lod, out NativeArray<float4> array) => data.TryGetUV3(lod, out array);
        public bool TryGetUV(int lod, UVChannelURP channel, out NativeArray<float4> array) => data.TryGetUV(lod, channel, out array);

        public int DeltasStartIndex => SubData.vertexCount * instance.localID;

        private bool PrepInWorldDataFetch(int lod, int vertexIndex, out int topVertexIndex, out MuscleData muscleData, out float flexFactor, out MeshVertexDelta delta, out MeshShape flexShape, out float4x4 skinningMatrix) 
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

        public float3 GetVertexInWorld(int lod, int vertexIndex) => GetVertexInWorld(lod, vertexIndex, out _, out _);
        public float3 GetVertexInWorld(int lod, int vertexIndex, out float4x4 skinningMatrix, out float3 localDelta)
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
        public float3 GetNormalInWorld(int lod, int vertexIndex) => GetNormalInWorld(lod, vertexIndex, out _, out _);
        public float3 GetNormalInWorld(int lod, int vertexIndex, out float4x4 skinningMatrix, out float3 localDelta)
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
        public float4 GetTangentInWorld(int lod, int vertexIndex) => GetTangentInWorld(lod, vertexIndex, out _, out _);
        public float4 GetTangentInWorld(int lod, int vertexIndex, out float4x4 skinningMatrix, out float3 localDelta)
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
        public void GetVertexInWorld(int lod, int vertexIndex, out float3 pos, out float3 normal, out float4 tangent) => GetVertexInWorld(lod, vertexIndex, out pos, out normal, out tangent, out _, out _, out _, out _);
        public void GetVertexInWorld(int lod, int vertexIndex, out float3 pos, out float3 normal, out float4 tangent, out float4x4 skinningMatrix, out float3 localDeltaPos, out float3 localDeltaNorm, out float3 localDeltaTan)
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

        public List<float3> GetMuscleGroupsAffecting(int lod, int vertexIndex, List<float3> list = null)
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
        public List<float3> GetFatGroupsAffecting(int lod, int vertexIndex, List<float3> list = null)
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
        public List<float3> GetVariationGroupsAffecting(int lod, int vertexIndex, List<float3> list = null)
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


        public override InstanceableMeshDataBase MeshData => null;
        public override InstancedMeshGroup MeshGroup => null;

        public int IndexOfStandaloneShape(string name, bool caseSensitive = false)
        {
            if (SubData == null) return -1;
            return data.SerializedData.IndexOfStandaloneShape(name, caseSensitive);
        }

        public int IndexOfVertexGroup(string name, bool caseSensitive = false)
        {
            if (SubData == null) return -1;
            return data.SerializedData.IndexOfVertexGroup(name, caseSensitive);
        }
        public int IndexOfStandaloneVertexGroup(string name, bool caseSensitive = false)
        {
            if (SubData == null) return -1;
            return data.SerializedData.IndexOfStandaloneVertexGroup(name, caseSensitive);
        }
        public int IndexOfMuscleGroup(string name, bool caseSensitive = false)
        {
            if (SubData == null) return -1;
            return data.SerializedData.IndexOfMuscleGroup(name, caseSensitive);
        }
        public int IndexOfFatGroup(string name, bool caseSensitive = false)
        {
            if (SubData == null) return -1;
            return data.SerializedData.IndexOfFatGroup(name, caseSensitive);
        }
        public int IndexOfVariationGroup(string name, bool caseSensitive = false)
        {
            if (SubData == null) return -1;
            return data.SerializedData.IndexOfVariationGroup(name, caseSensitive);
        }
        public int IndexOfVariationShape(string name, bool caseSensitive = false)
        {
            if (SubData == null) return -1;
            return data.SerializedData.IndexOfVariationShape(name, caseSensitive);
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

        #region Children

        [Serializable, Flags]
        public enum ChildType
        {
            None = 0, Mesh = 1, Shapes = 2, Rig = 4
        }

        public struct Child
        {
            public ChildType type;
            public CustomizableCharacterMeshV2 instance;

            public bool IsValid => instance != null;
        }

        protected List<Child> children;
        public bool IsParentOf(CustomizableCharacterMeshV2 child) => IsParentOf(child, out _);
        public bool IsParentOf(CustomizableCharacterMeshV2 child, out int index)
        {
            index = -1;
            if (children == null) return false;

            for(int a = 0; a < children.Count; a++)
            {
                var child_ = children[a];
                if (ReferenceEquals(child_.instance, child)) 
                { 
                    index = a;
                    return true; 
                }
            }

            return false;
        }
        public void AddChild(CustomizableCharacterMeshV2 child, ChildType type)
        {
            if (child == null) return;

            if (children == null) children = new List<Child>();
            if (IsParentOf(child, out var childIndex))
            {
                var c = children[childIndex];
                c.type |= type;
                children[childIndex] = c;
            } 
            else
            {
                children.Add(new Child() { instance = child, type = type });
            }
        }
        public void RemoveChild(CustomizableCharacterMeshV2 child, ChildType type)
        {
            if (children == null) return;

            if (IsParentOf(child, out var childIndex))
            {
                var c = children[childIndex];
                c.type &= ~type;
                children[childIndex] = c;
                if (c.type == ChildType.None) children.RemoveAt(childIndex);
            }
        }
        public void RemoveChild(CustomizableCharacterMeshV2 child)
        {
            if (children == null) return;

            if (IsParentOf(child, out var childIndex))
            {
                children.RemoveAt(childIndex);
            }
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

        protected Dictionary<string, float> floatOverrides = new Dictionary<string, float>();
        protected Dictionary<string, int> intOverrides = new Dictionary<string, int>();
        protected Dictionary<string, Vector4> vectorOverrides = new Dictionary<string, Vector4>();
        protected Dictionary<string, Color> colorOverrides = new Dictionary<string, Color>();

        protected void ApplyCachedMaterialPropertyOverrides()
        {
            var materialInstances = MaterialInstances;
            if (materialInstances == null) return;

            if (floatOverrides != null)
            {
                foreach(var entry in floatOverrides)
                {
                    foreach (var mat in materialInstances)
                    {
                        if (mat != null) mat.SetFloat(entry.Key, entry.Value);
                    }
                }
            }
            if (intOverrides != null)
            {
                foreach (var entry in intOverrides)
                {
                    foreach (var mat in materialInstances)
                    {
                        if (mat != null) mat.SetInteger(entry.Key, entry.Value);
                    }
                }
            }
            if (vectorOverrides != null)
            {
                foreach (var entry in vectorOverrides)
                {
                    foreach (var mat in materialInstances)
                    {
                        if (mat != null) mat.SetVector(entry.Key, entry.Value);
                    }
                }
            }
            if (colorOverrides != null)
            {
                foreach (var entry in colorOverrides)
                {
                    foreach (var mat in materialInstances)
                    {
                        if (mat != null) mat.SetColor(entry.Key, entry.Value);
                    }
                }
            }
        }

        public virtual void SetFloatOverride(string property, float value, bool updateMaterials = true)
        {
            floatOverrides[property] = value;

            var materialInstances = MaterialInstances;
            if (materialInstances == null) return;
            
            foreach(var mat in materialInstances)
            {
                if (mat != null) mat.SetFloat(property, value);
            }
        }
        public virtual void SetFloatOverrideWithCheck(string property, float value, bool updateMaterials = true)
        {
            floatOverrides[property] = value;

            var materialInstances = MaterialInstances;
            if (materialInstances == null) return;

            foreach (var mat in materialInstances)
            {
                if (mat != null && mat.HasProperty(property)) mat.SetFloat(property, value);
            }
        }

        public virtual void SetIntegerOverride(string property, int value, bool updateMaterials = true)
        {
            intOverrides[property] = value;

            var materialInstances = MaterialInstances;
            if (materialInstances == null) return;

            foreach (var mat in materialInstances)
            {
                if (mat != null) mat.SetInteger(property, value);
            }
        }
        public virtual void SetIntegerOverrideWithCheck(string property, int value, bool updateMaterials = true)
        {
            intOverrides[property] = value;

            var materialInstances = MaterialInstances;
            if (materialInstances == null) return;

            foreach (var mat in materialInstances)
            {
                if (mat != null && mat.HasProperty(property)) mat.SetInteger(property, value);
            }
        }

        public void SetVectorOverride(string propertyName, Vector4 vector, bool updateMaterials = true)
        {
            vectorOverrides[propertyName] = vector;

            var materialInstances = MaterialInstances;
            if (materialInstances == null) return;

            foreach (var mat in materialInstances)
            {
                if (mat != null) mat.SetVector(propertyName, vector);
            }
        }
        public void SetVectorOverrideWithCheck(string propertyName, Vector4 vector, bool updateMaterials = true)
        {
            vectorOverrides[propertyName] = vector;

            var materialInstances = MaterialInstances;
            if (materialInstances == null) return;

            foreach (var mat in materialInstances)
            {
                if (mat != null && mat.HasProperty(propertyName)) mat.SetVector(propertyName, vector);
            }
        }

        public void SetColorOverride(string propertyName, Color color, bool updateMaterials = true)
        {
            colorOverrides[propertyName] = color;

            var materialInstances = MaterialInstances;
            if (materialInstances == null) return;

            foreach (var mat in materialInstances)
            {
                if (mat != null) mat.SetColor(propertyName, color);
            }
        }
        public void SetColorOverrideWithCheck(string propertyName, Color color, bool updateMaterials = true)
        {
            colorOverrides[propertyName] = color;

            var materialInstances = MaterialInstances;
            if (materialInstances == null) return;

            foreach (var mat in materialInstances)
            {
                if (mat != null && mat.HasProperty(propertyName)) mat.SetColor(propertyName, color);
            }
        }

        #endregion

        #region Rendering

        protected struct DefaultRenderedMesh
        {
            public MeshFilter meshFilter;
            public MeshRenderer meshRenderer;

            public MeshFilter[] additionalFilters;
            public MeshRenderer[] additionalRenderers;
        }

        protected LODGroup lodGroup;

        protected DefaultRenderedMesh[] defaultRenderedMeshes;

        public bool RenderingIsInitialized() => lodGroup != null && defaultRenderedMeshes != null;
        public bool IsRendering() => RenderingIsInitialized() && CanRender;
        public override bool IsRendering(int index) => IsRendering();
        public bool CanRender => instance != null && instance.IsValid;

        public void InitializeRendering()
        {

            if (RenderingIsInitialized() || !CanRender) return;

            var meshData = SubData;
            var bounds = new Bounds(meshData.boundsCenter, meshData.boundsExtents * 2f);

            GameObject lodObj = new GameObject("renderers");
            lodObj.layer = gameObject.layer;


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
                rendererObj.layer = gameObject.layer;
                rendererObj.transform.SetParent(lodRootTransform, false);

                Renderer[] renderers = null;
                if (meshData.renderSets == null || meshData.renderSets.Length <= 1)
                {
                    defaultRenderedMesh.meshFilter = rendererObj.AddComponent<MeshFilter>();
                    defaultRenderedMesh.meshFilter.sharedMesh = meshLOD.mesh;
                    defaultRenderedMesh.meshRenderer = rendererObj.AddComponent<MeshRenderer>();
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
                        renderSetObj.transform.SetParent(rendererObj.transform, false);

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

                var lod = new LOD()
                {
                    screenRelativeTransitionHeight = meshLOD.screenRelativeTransitionHeight,
                    renderers = renderers
                };

                defaultRenderedMeshes[a] = defaultRenderedMesh; 
                lods[a] = lod;
            }

            lodGroup.SetLODs(lods);
            lodGroup.RecalculateBounds();

            InitBuffers();
        }
        protected virtual void StartRendering()
        {
            if (Data == null) return;

            if (lodGroup != null) lodGroup.gameObject.SetActive(true);

            CreateInstance();
            InitializeRendering();

            var materialInstances = MaterialInstances;
            BindSkinningMatricesBufferToMaterials(materialInstances);
            BindStandaloneShapesControlBufferToMaterials(materialInstances);
            BindMuscleGroupsControlBufferToMaterials(materialInstances);
            BindFatGroupsControlBufferToMaterials(materialInstances);
            BindVariationGroupsControlBufferToMaterials(materialInstances);

            ApplyCachedMaterialPropertyOverrides();
            ApplyIDsToMaterials();

            if (defaultRenderedMeshes != null)
            {
                var renderSets = SubData.renderSets;
                if (renderSets == null || renderSets.Length <= 1)
                {
                    foreach (var renderedMesh in defaultRenderedMeshes)
                    {
                        if (renderedMesh.meshRenderer != null) renderedMesh.meshRenderer.sharedMaterials = materialInstances;
                    }
                } 
                else
                {
                    foreach (var renderedMesh in defaultRenderedMeshes)
                    {
                        for (int i = 0; i < renderSets.Length; i++)
                        {
                            var renderSet = renderSets[i];

                            MeshRenderer renderer = null;
                            if (i == 0)
                            {
                                renderer = renderedMesh.meshRenderer;
                            } 
                            else
                            {
                                renderer = renderedMesh.additionalRenderers[i - 1];
                            }

                            if (renderer != null)
                            {
                                var mats = renderer.sharedMaterials;
                                if (mats == null || mats.Length != renderSet.materialCount)
                                {
                                    mats = new Material[renderSet.materialCount];
                                }
                                for (int j = 0; j < renderSet.materialCount; j++) mats[j] = materialInstances[renderSet.materialIndexStart + j]; 

                                renderer.sharedMaterials = mats;
                            }
                        }
                    }
                }
            }
        }
        protected virtual void StopRendering()
        {
            var materialInstances = MaterialInstances;
            UnbindSkinningMatricesBufferFromMaterials(materialInstances);
            UnbindStandaloneShapesControlBufferFromMaterials(materialInstances);
            UnbindMuscleGroupsControlBufferFromMaterials(materialInstances);
            UnbindFatGroupsControlBufferFromMaterials(materialInstances);
            UnbindVariationGroupsControlBufferFromMaterials(materialInstances);

            if (lodGroup != null) lodGroup.gameObject.SetActive(false); 
            if (instance != null) Updater.Unregister(ref instance);
        }

        public void ApplyIDsToMaterials()
        {
            var meshData = SubData;
            var materialInstances = MaterialInstances;
            if (meshData != null && materialInstances != null)
            {
                SetFloatOverride(meshData.LocalInstanceIDPropertyName, InstanceID);

                SetFloatOverride(meshData.RigInstanceIDPropertyName, RigInstanceID);

                SetFloatOverride(meshData.ShapesInstanceIDPropertyName, ShapesInstanceID);

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

        #region Activation

        protected override void OnEnable()
        {
            StartRendering(); 
        }
        protected override void OnDisable()
        {
            StopRendering();
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
        protected float bustShape;
        public float BustShape
        {
            get => bustShape;
            set => SetBustSize(value);
        }
        //[NonSerialized]
        //protected bool hasBustSizeProperty;
        public void SetBustSize(float value)
        {
            float prevValue = bustSize;
            bustSize = value;

            //if (SubData.bustSizeShape >= 0) SetStandaloneShapeWeightUnsafe(SubData.bustSizeShape, value);

            if (instance != null && instance.IsValid && prevValue != bustSize)
            {
                instance.SetBustSizeUnsafe(new float2(bustSize, bustShape));
                instance.MarkForPhysiqueUpdateUnsafe();
            }

            //if (hasBustSizeProperty)
            //{
                SetFloatOverrideWithCheck(SubData.BustMixPropertyName, Mathf.Clamp01(value), true);
            //}

            if (children != null)
            {
                foreach (var child in children) if (child.IsValid && child.type.HasFlag(ChildType.Mesh)) child.instance.SetBustSize(value);
            }
        }
        public void SetBustShape(float value)
        {
            float prevValue = bustShape;
            bustShape = value;

            if (instance != null && instance.IsValid && prevValue != bustShape)
            {
                instance.SetBustSizeUnsafe(new float2(bustSize, bustShape));
                instance.MarkForPhysiqueUpdateUnsafe();
            }

            if (children != null)
            {
                foreach (var child in children) if (child.IsValid && child.type.HasFlag(ChildType.Mesh)) child.instance.SetBustShape(value);
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
                    foreach (var child in children) if (child.IsValid && child.type.HasFlag(ChildType.Mesh)) child.instance.SetHideNipples(value);
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
                    foreach (var child in children) if (child.IsValid && child.type.HasFlag(ChildType.Mesh)) child.instance.SetHideGenitals(value);
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

        public int FirstStandaloneShapesControlIndex => CharacterInstanceID * SubData.StandaloneShapesCount;
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

        public int FirstMuscleGroupsControlIndex => CharacterInstanceID * SubData.MuscleVertexGroupCount;
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

            if (!IsInitialized) return;
            data.valuesLeft.flex = math.max(data.valuesLeft.flex, -0.15f);
            data.valuesRight.flex = math.max(data.valuesRight.flex, -0.15f);

            if (characterInstanceReference == null)
            {
                int controlIndex = FirstMuscleGroupsControlIndex + groupIndex;
                var prevData = MuscleGroupsControlBuffer[controlIndex];
                MuscleGroupsControlBuffer[controlIndex] = data;
            }

            SyncMuscleMassData(groupIndex, data.valuesLeft.mass, data.valuesRight.mass);
            SyncMuscleFlexData(groupIndex, data.valuesLeft.flex, data.valuesRight.flex);

            //dirtyFlag_muscleGroupsControl = true;

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

            OnMuscleDataChanged?.Invoke(groupIndex);

            if (children != null)
            {
                foreach (var child in children) if (child.IsValid && child.type.HasFlag(ChildType.Mesh))
                    {
                        child.instance.SetMuscleDataUnsafe(groupIndex, data);
                    }
            }
        }
        public void SetMuscleData(int groupIndex, MuscleDataLR data)
        {
            if (instance == null || groupIndex < 0 || groupIndex >= SubData.MuscleVertexGroupCount) return;
            SetMuscleDataUnsafe(groupIndex, data);
        }
        public int IndexOfMuscleGroup(string groupName) => Data == null ? -1 : SubData.IndexOfMuscleGroup(groupName);


        public MuscleData GetMuscleDataForVertex(int vertexIndex)
        {
            MuscleData data = default;

            var subData = SubData;
            if (subData.muscleGroups.y >= subData.muscleGroups.x) 
            {
                float midlineWeight = subData.precache_vertexGroups[(subData.midlineVertexGroup * subData.vertexCount) + vertexIndex];
                float2 weightLeftRight = math.lerp(math.select(new float2(1f, 0f), new float2(0f, 1f), subData.leftRightFlags[vertexIndex]), new float2(0.5f, 0.5f), midlineWeight);
                for (int i = subData.muscleGroups.x; i <= subData.muscleGroups.y; i++)
                {
                    int muscleGroupIndex = i - subData.muscleGroups.x;
                    int controlIndex = FirstMuscleGroupsControlIndex + muscleGroupIndex;
                    var dataLR = MuscleGroupsControlBuffer[controlIndex];

                    data = data + (dataLR.valuesLeft * weightLeftRight.x) + (dataLR.valuesRight * weightLeftRight.y); 
                }
            }

            return data;
        }


        public int FirstFatGroupsControlIndex => CharacterInstanceID * SubData.FatVertexGroupCount;
        public float GetFatLevelUnsafe(int groupIndex) => FatGroupsControlBuffer[FirstFatGroupsControlIndex + groupIndex].x;
        public float GetFatLevel(int groupIndex)
        {
            if (instance == null || groupIndex < 0 || groupIndex >= SubData.FatVertexGroupCount) return 0f;  
            return GetFatLevelUnsafe(groupIndex);
        }
        public UnityEvent<int> OnFatDataChanged;
        public void SetFatLevelUnsafe(int groupIndex, float level)
        {
            if (!IsInitialized) return;

            if (characterInstanceReference == null)
            {
                int controlIndex = FirstFatGroupsControlIndex + groupIndex;
                var val = FatGroupsControlBuffer[controlIndex];
                val.x = level;
                FatGroupsControlBuffer[controlIndex] = val;
            }

            SyncFatLevel(groupIndex, level);

            if (instance != null && instance.IsValid)
            {
                var prevData = instance.GetFatGroupWeightUnsafe(groupIndex);
                if (prevData.x != level || prevData.y != level)
                {
                    instance.SetFatGroupWeightUnsafe(groupIndex, level);
                    instance.MarkForPhysiqueUpdateUnsafe();
                }
            }

            OnFatDataChanged?.Invoke(groupIndex);

            if (children != null)
            {
                foreach (var child in children) if (child.IsValid && child.type.HasFlag(ChildType.Mesh)) child.instance.SetFatLevelUnsafe(groupIndex, level);
            }
        }
        public void SetFatLevel(int groupIndex, float level)
        {
            if (instance == null || groupIndex < 0 || groupIndex >= SubData.FatVertexGroupCount) return;
            SetFatLevelUnsafe(groupIndex, level);
        }
        public float2 GetBodyHairLevelUnsafe(int groupIndex) => FatGroupsControlBuffer[FirstFatGroupsControlIndex + groupIndex].zw;
        public float2 GetBodyHairLevel(int groupIndex)
        {
            if (instance == null || groupIndex < 0 || groupIndex >= SubData.FatVertexGroupCount) return 0f;
            return GetBodyHairLevelUnsafe(groupIndex);
        }
        public void SetBodyHairLevelUnsafe(int groupIndex, float level, float blend = 1f)
        {
            if (!IsInitialized) return;

            if (characterInstanceReference == null)
            {
                int controlIndex = FirstFatGroupsControlIndex + groupIndex;
                var val = FatGroupsControlBuffer[controlIndex];
                val.z = level;
                val.w = blend;
                FatGroupsControlBuffer[controlIndex] = val;
            }

            OnFatDataChanged?.Invoke(groupIndex);

            if (children != null)
            {
                foreach (var child in children) if (child.IsValid && child.type.HasFlag(ChildType.Mesh)) child.instance.SetBodyHairLevelUnsafe(groupIndex, level, blend);
            }
        }
        public void SetBodyHairLevel(int groupIndex, float level, float blend = 1f)
        {
            if (instance == null || groupIndex < 0 || groupIndex >= SubData.FatVertexGroupCount) return;
            SetBodyHairLevelUnsafe(groupIndex, level, blend);
        }
        public int IndexOfFatGroup(string groupName) => Data == null ? -1 : SubData.IndexOfFatGroup(groupName);


        public int VariationShapesControlDataSize => Data.VariationShapesControlDataSize;
        public int FirstVariationShapesControlIndex => CharacterInstanceID * VariationShapesControlDataSize;
        public int GetPartialVariationShapeIndex(int variationGroupIndex, int shapeIndex)
        {
            if (variationGroupIndex < 0 || variationGroupIndex >= Data.VariationVertexGroupCount || shapeIndex < 0 || shapeIndex >= Data.VariationShapesCount) return -1;
            return GetPartialVariationShapeIndexUnsafe(variationGroupIndex, shapeIndex);
        }
        public int GetPartialVariationShapeIndexUnsafe(int variationGroupIndex, int shapeIndex)
        {
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

            if (!IsInitialized) return;
            if (characterInstanceReference == null)
            {
                int variationIndex = GetPartialVariationShapeIndexUnsafe(groupIndex, variationShapeIndex);
                VariationShapesControlBuffer[FirstVariationShapesControlIndex + variationIndex] = weight;
            }

            SyncVariationData(groupIndex, variationShapeIndex, weight.x, weight.y);

            //dirtyFlag_variationShapesControl = true;

            if (instance != null && instance.IsValid)
            {
                var prevData = instance.GetVariationGroupWeightUnsafe(variationShapeIndex, groupIndex);
                if (prevData.x != weight.x || prevData.y != weight.y)
                {
                    instance.SetVariationGroupWeightUnsafe(variationShapeIndex, groupIndex, weight);
                    instance.MarkForVariationUpdateUnsafe();
                }
            }

            if (children != null)
            {
                foreach (var child in children) if (child.IsValid && child.type.HasFlag(ChildType.Mesh)) child.instance.SetVariationWeightUnsafe(variationShapeIndex, groupIndex, weight);
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

            if (!animatablePropertiesController.IsInitialized) animatablePropertiesController.Initialize();

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
                if (group == null /*|| !group.flag*/) continue; // animatable permission is stored in vertex group flag field // TODO: uncomment group.flag check

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
            if (!IsInitialized || characterInstanceReference != null) return;

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

                    if (SubData.HasBonesArray)
                    {
                        var boneNames = SubData.boneNames;
                        skinnedBones = new Transform[boneNames.Length];
                        for (int a = 0; a < skinnedBones.Length; a++) skinnedBones[a] = rig_root.FindDeepChildLiberal(boneNames[a]); 
                    }
                    else
                    {
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
        public ICustomizableCharacter ShapesInstanceReference
        {
            get => shapesInstanceReference;
            set
            {
                if (shapesInstanceReference != null)
                {
                    shapesInstanceReference.RemoveChild(this, ChildType.Shapes);
                }

                if (value is CustomizableCharacterMeshV2 meshV2_)
                {
                    meshV2_.AddChild(this, ChildType.Shapes);
                    shapesInstanceReference = meshV2_; 
                } 
                else
                {
                    shapesInstanceReference = null;
                }
            }
        }
        public int ShapesInstanceID => shapesInstanceReference == null ? InstanceSlot : shapesInstanceReference.ShapesInstanceID;

        public InstanceableSkinnedMeshBase rigInstanceReference;
        public InstanceableSkinnedMeshBase RigInstanceReference
        {
            get => rigInstanceReference;
            set
            {
                if (rigInstanceReference is CustomizableCharacterMeshV2 meshV2)
                {
                    meshV2.RemoveChild(this, ChildType.Rig);
                }

                rigInstanceReference = value;

                if (RigInstanceReferenceIsValid)
                {
                    if (rigInstanceReference is CustomizableCharacterMeshV2 meshV2_)
                    {
                        meshV2_.AddChild(this, ChildType.Rig); 
                    }
                } 
                else
                {
                    rigInstanceReference = null;
                }
            }
        }
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
        public ICustomizableCharacter CharacterInstanceReference
        {
            get => characterInstanceReference;
            set
            {
                if (characterInstanceReference != null) characterInstanceReference.RemoveChild(this, ChildType.Mesh);

                if (value is CustomizableCharacterMeshV2 meshV2)
                {
                    characterInstanceReference = meshV2;
                    characterInstanceReference.AddChild(this, ChildType.Mesh); 
                }
                else
                {
                    characterInstanceReference = null;
                }
            }
        }
        public int CharacterInstanceID => characterInstanceReference == null ? InstanceID : characterInstanceReference.CharacterInstanceID;

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

                characterInstanceReference.AddChild(this, ChildType.Mesh);
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
                            for (int a = 0; a < meshData.FatVertexGroupCount; a++) fatGroupsControlBuffer.WriteToBufferFast(indexStart + a, new float4(0f, _defaultFatGroupModifier.x, 0f, 0f));
                        }
                        else
                        {
                            for (int a = 0; a < meshData.FatVertexGroupCount; a++)
                            {
                                var modifier = meshData.GetFatGroupModifier(a);
                                fatGroupsControlBuffer.WriteToBufferFast(indexStart + a, new float4(0f, modifier.x, 0f, 0f));
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

                            if (skinningMatricesBuffer != null)
                            {
                                if (children != null)
                                {
                                    foreach (var child in children) if (child.IsValid && child.type.HasFlag(ChildType.Rig) && child.instance.IsRendering()) child.instance.BindSkinningMatricesBufferToMaterials();
                                }
                            }
                        }
                    }
                }

                return skinningMatricesBuffer;
            }
        }
        public void BindSkinningMatricesBufferToMaterials()
        {
            BindSkinningMatricesBufferToMaterials(MaterialInstances);
        }
        public void BindSkinningMatricesBufferToMaterials(Material[] materialInstances)
        {
            var matricesBuffer = SkinningMatricesBuffer;
            if (matricesBuffer != null)
            {
                var meshData = SubData;

                int boneCount = SkinningBoneCount;

                if (!RigInstanceReferenceIsValid)
                {
                    var rigSampler = RigSampler;
                    if (rigSampler != null) rigSampler.AddWritableInstanceBuffer(matricesBuffer, RigInstanceID * boneCount);
                }

                if (materialInstances != null)
                {
                    foreach (var mat in materialInstances)
                    {
                        if (mat == null) continue;

                        Debug.Log($"{name}: Binding {meshData.SkinningMatricesPropertyName} to {mat.name}");
                        matricesBuffer.BindMaterialProperty(mat, meshData.SkinningMatricesPropertyName);
                        mat.SetInteger(meshData.BoneCountPropertyName, boneCount);
                    }
                }
            }
        }
        public void UnbindSkinningMatricesBufferFromMaterials()
        {
            UnbindSkinningMatricesBufferFromMaterials(MaterialInstances);
        }
        public void UnbindSkinningMatricesBufferFromMaterials(Material[] materialInstances)
        {
            if (skinningMatricesBuffer != null && materialInstances != null)
            {
                var meshData = SubData;

                if (!RigInstanceReferenceIsValid)
                {
                    if (rigSampler != null) rigSampler.RemoveWritableInstanceBuffer(skinningMatricesBuffer, RigInstanceID * SkinningBoneCount); 
                }

                if (materialInstances != null)
                {
                    foreach (var mat in materialInstances)
                    {
                        if (mat == null) continue;

                        skinningMatricesBuffer.UnbindMaterialProperty(mat, meshData.SkinningMatricesPropertyName);
                    }
                }
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

                            if (standaloneShapeControlBuffer != null)
                            {
                                if (children != null)
                                {
                                    foreach (var child in children) if (child.IsValid && child.type.HasFlag(ChildType.Shapes)) child.instance.BindStandaloneShapesControlBufferToMaterials();
                                }
                            }
                        }
                    }
                }

                return standaloneShapeControlBuffer;
            }
        }
        public void BindStandaloneShapesControlBufferToMaterials()
        {
            BindStandaloneShapesControlBufferToMaterials(MaterialInstances);
        }
        public void BindStandaloneShapesControlBufferToMaterials(Material[] materialInstances)
        {
            var shapeBuffer = StandaloneShapeControlBuffer;
            if (shapeBuffer != null && materialInstances != null)
            {
                var meshData = SubData;
                foreach (var mat in materialInstances)
                {
                    if (mat == null) continue;

                    Debug.Log($"{name}: Binding {meshData.StandaloneShapesControlPropertyName} to {mat.name}");
                    shapeBuffer.BindMaterialProperty(mat, meshData.StandaloneShapesControlPropertyName);
                }
            }
        }
        public void UnbindStandaloneShapesControlBufferFromMaterials()
        {
            UnbindStandaloneShapesControlBufferFromMaterials(MaterialInstances);
        }
        public void UnbindStandaloneShapesControlBufferFromMaterials(Material[] materialInstances)
        {
            if (standaloneShapeControlBuffer != null && materialInstances != null)
            {
                var meshData = SubData;
                foreach (var mat in materialInstances)
                {
                    if (mat == null) continue;

                    standaloneShapeControlBuffer.UnbindMaterialProperty(mat, meshData.StandaloneShapesControlPropertyName);
                }
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

                            if (muscleGroupsControlBuffer != null)
                            {
                                if (children != null)
                                {
                                    foreach (var child in children) if (child.IsValid && child.type.HasFlag(ChildType.Mesh)) child.instance.BindMuscleGroupsControlBufferToMaterials();
                                }
                            }
                        }
                    }
                }

                return muscleGroupsControlBuffer;
            }
        }
        public void BindMuscleGroupsControlBufferToMaterials()
        {
            BindMuscleGroupsControlBufferToMaterials(MaterialInstances);
        }
        public void BindMuscleGroupsControlBufferToMaterials(Material[] materialInstances)
        {
            var muscleBuffer = MuscleGroupsControlBuffer;
            if (muscleBuffer != null && materialInstances != null)
            {
                var meshData = SubData;
                foreach (var mat in materialInstances)
                {
                    if (mat == null) continue;

                    Debug.Log($"{name}: Binding {meshData.MuscleGroupsControlPropertyName} to {mat.name}");
                    muscleBuffer.BindMaterialProperty(mat, meshData.MuscleGroupsControlPropertyName);
                }
            }
        }
        public void UnbindMuscleGroupsControlBufferFromMaterials()
        {
            UnbindMuscleGroupsControlBufferFromMaterials(MaterialInstances);
        }
        public void UnbindMuscleGroupsControlBufferFromMaterials(Material[] materialInstances)
        {
            if (muscleGroupsControlBuffer != null && materialInstances != null)
            {
                var meshData = SubData;
                foreach (var mat in materialInstances)
                {
                    if (mat == null) continue;

                    muscleGroupsControlBuffer.UnbindMaterialProperty(mat, meshData.MuscleGroupsControlPropertyName);
                }
            }
        }

        protected InstanceBuffer<float4> fatGroupsControlBuffer;
        public InstanceBuffer<float4> FatGroupsControlBuffer
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
                            if (!instance.OwnerGroup.TryGetInstanceBuffer<float4>(matProperty, out fatGroupsControlBuffer))
                            {
                                instance.OwnerGroup.CreateInstanceMaterialBuffer<float4>(matProperty, SubData.FatVertexGroupCount, 2, false, out fatGroupsControlBuffer);
                            }

                            if (fatGroupsControlBuffer != null)
                            {
                                if (children != null)
                                {
                                    foreach (var child in children) if (child.IsValid && child.type.HasFlag(ChildType.Mesh)) child.instance.BindFatGroupsControlBufferToMaterials();
                                }
                            }
                        }
                    }
                }

                return fatGroupsControlBuffer;
            }
        }
        public void BindFatGroupsControlBufferToMaterials()
        {
            BindFatGroupsControlBufferToMaterials(MaterialInstances);
        }
        public void BindFatGroupsControlBufferToMaterials(Material[] materialInstances)
        {
            var fatBuffer = FatGroupsControlBuffer;
            if (fatBuffer != null && materialInstances != null)
            {
                var meshData = SubData;
                foreach (var mat in materialInstances)
                {
                    if (mat == null) continue;

                    Debug.Log($"{name}: Binding {meshData.FatGroupsControlPropertyName} to {mat.name}");
                    fatBuffer.BindMaterialProperty(mat, meshData.FatGroupsControlPropertyName);
                }
            }
        }
        public void UnbindFatGroupsControlBufferFromMaterials()
        {
            UnbindFatGroupsControlBufferFromMaterials(MaterialInstances);
        }
        public void UnbindFatGroupsControlBufferFromMaterials(Material[] materialInstances)
        {
            if (fatGroupsControlBuffer != null && materialInstances != null)
            {
                var meshData = SubData;
                foreach (var mat in materialInstances)
                {
                    if (mat == null) continue;

                    fatGroupsControlBuffer.UnbindMaterialProperty(mat, meshData.FatGroupsControlPropertyName);
                }
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

                            if (variationShapesControlBuffer != null)
                            {
                                if (children != null)
                                {
                                    foreach (var child in children) if (child.IsValid && child.type.HasFlag(ChildType.Mesh)) child.instance.BindVariationGroupsControlBufferToMaterials();
                                }
                            }
                        }
                    }
                }

                return variationShapesControlBuffer;
            }
        }
        public void BindVariationGroupsControlBufferToMaterials()
        {
            BindVariationGroupsControlBufferToMaterials(MaterialInstances);
        }
        public void BindVariationGroupsControlBufferToMaterials(Material[] materialInstances)
        {
            var variationBuffer = VariationShapesControlBuffer;
            if (variationBuffer != null && materialInstances != null)
            {
                var meshData = SubData;
                foreach (var mat in materialInstances)
                {
                    if (mat == null) continue;

                    Debug.Log($"{name}: Binding {meshData.VariationShapesControlPropertyName} to {mat.name}");
                    variationBuffer.BindMaterialProperty(mat, meshData.VariationShapesControlPropertyName);
                }
            }
        }
        public void UnbindVariationGroupsControlBufferFromMaterials()
        {
            UnbindVariationGroupsControlBufferFromMaterials(MaterialInstances);
        }
        public void UnbindVariationGroupsControlBufferFromMaterials(Material[] materialInstances)
        {
            if (variationShapesControlBuffer != null && materialInstances != null)
            {
                var meshData = SubData;
                foreach (var mat in materialInstances)
                {
                    if (mat == null) continue;

                    variationShapesControlBuffer.UnbindMaterialProperty(mat, meshData.VariationShapesControlPropertyName); 
                }
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

        #region Sampling

        protected struct RaycastResult
        {
            public bool didHit;
            public Maths.RaycastHitResult hitInfo;
        }

        public int DefaultRaycastLOD
        {
            get => data.SerializedData.raycastLod;
            set
            {
            }
        }

        public bool RaycastAgainst(int lod, float3 origin, float3 offset, out Maths.RaycastHitResult result, float errorMargin = 0.01f)
        {
            result = default;
            if (!IsInitialized) return false;

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

        [BurstCompile]
        protected struct RaycastMeshJob : IJobParallelFor
        {
            public int deltasStartIndex;

            public RGBAChannel indexChannel;

            public float errorMargin;

            public float3 origin;
            public float3 offset;

            [ReadOnly]
            public NativeArray<float3> vertices;
            [ReadOnly]
            public NativeArray<int> triangles;
            [ReadOnly]
            public NativeArray<BoneWeight8> boneWeights;
            [ReadOnly]
            public NativeArray<MeshVertexDelta> deltas;
            [ReadOnly]
            public NativeArray<float4x4> skinningMatrices;

            public NativeQueue<RaycastResult>.ParallelWriter results; 

            public void Execute(int index)
            {
                int triIndex = index * 3;

                int i0 = triangles[triIndex];
                int i1 = triangles[triIndex + 1];
                int i2 = triangles[triIndex + 2];

                var boneWeights0 = boneWeights[i0];
                var boneWeights1 = boneWeights[i1];
                var boneWeights2 = boneWeights[i2]; 

                var skinning0 = 
                    (skinningMatrices[boneWeights0.boneIndex0] * boneWeights0.boneWeight0) +
                    (skinningMatrices[boneWeights0.boneIndex1] * boneWeights0.boneWeight1) +
                    (skinningMatrices[boneWeights0.boneIndex2] * boneWeights0.boneWeight2) +
                    (skinningMatrices[boneWeights0.boneIndex3] * boneWeights0.boneWeight3) +
                    (skinningMatrices[boneWeights0.boneIndex4] * boneWeights0.boneWeight4) +
                    (skinningMatrices[boneWeights0.boneIndex5] * boneWeights0.boneWeight5) +
                    (skinningMatrices[boneWeights0.boneIndex6] * boneWeights0.boneWeight6) +
                    (skinningMatrices[boneWeights0.boneIndex7] * boneWeights0.boneWeight7);

                var skinning1 =
                    (skinningMatrices[boneWeights1.boneIndex0] * boneWeights1.boneWeight0) +
                    (skinningMatrices[boneWeights1.boneIndex1] * boneWeights1.boneWeight1) +
                    (skinningMatrices[boneWeights1.boneIndex2] * boneWeights1.boneWeight2) +
                    (skinningMatrices[boneWeights1.boneIndex3] * boneWeights1.boneWeight3) +
                    (skinningMatrices[boneWeights1.boneIndex4] * boneWeights1.boneWeight4) +
                    (skinningMatrices[boneWeights1.boneIndex5] * boneWeights1.boneWeight5) +
                    (skinningMatrices[boneWeights1.boneIndex6] * boneWeights1.boneWeight6) +
                    (skinningMatrices[boneWeights1.boneIndex7] * boneWeights1.boneWeight7);

                var skinning2 =
                    (skinningMatrices[boneWeights2.boneIndex0] * boneWeights2.boneWeight0) +
                    (skinningMatrices[boneWeights2.boneIndex1] * boneWeights2.boneWeight1) +
                    (skinningMatrices[boneWeights2.boneIndex2] * boneWeights2.boneWeight2) +
                    (skinningMatrices[boneWeights2.boneIndex3] * boneWeights2.boneWeight3) +
                    (skinningMatrices[boneWeights2.boneIndex4] * boneWeights2.boneWeight4) +
                    (skinningMatrices[boneWeights2.boneIndex5] * boneWeights2.boneWeight5) +
                    (skinningMatrices[boneWeights2.boneIndex6] * boneWeights2.boneWeight6) +
                    (skinningMatrices[boneWeights2.boneIndex7] * boneWeights2.boneWeight7); 

                var v0 = math.transform(skinning0, vertices[i0] + deltas[deltasStartIndex + i0].positionDelta);
                var v1 = math.transform(skinning1, vertices[i1] + deltas[deltasStartIndex + i1].positionDelta);
                var v2 = math.transform(skinning2, vertices[i2] + deltas[deltasStartIndex + i2].positionDelta);

                var output = new RaycastResult();
                output.didHit = Maths.seg_intersect_triangle_include_dist(origin, offset, v0, v1, v2, out Maths.RaycastHitResult result, errorMargin);
                result.triangleIndex = index;
                output.hitInfo = result;

                if (output.didHit) results.Enqueue(output); 
            }

        }

        [BurstCompile]
        protected struct RaycastMeshWithIndexUVJob : IJobParallelFor
        {
            public int deltasStartIndex;

            public RGBAChannel indexChannel;

            public float errorMargin;

            public float3 origin;
            public float3 offset;

            [ReadOnly]
            public NativeArray<float3> vertices;
            [ReadOnly]
            public NativeArray<float4> indexUVs;
            [ReadOnly]
            public NativeArray<int> triangles;
            [ReadOnly]
            public NativeArray<BoneWeight8> boneWeights;
            [ReadOnly]
            public NativeArray<MeshVertexDelta> deltas;
            [ReadOnly]
            public NativeArray<float4x4> skinningMatrices;

            public NativeQueue<RaycastResult>.ParallelWriter results;

            public void Execute(int index)
            {
                int triIndex = index * 3;

                int i0 = triangles[triIndex];
                int i1 = triangles[triIndex + 1];
                int i2 = triangles[triIndex + 2];

                int baseI0 = MorphUtils.FetchIndexFromUV(indexChannel, indexUVs[i0]);
                int baseI1 = MorphUtils.FetchIndexFromUV(indexChannel, indexUVs[i1]);
                int baseI2 = MorphUtils.FetchIndexFromUV(indexChannel, indexUVs[i2]);

                var boneWeights0 = boneWeights[baseI0];
                var boneWeights1 = boneWeights[baseI1];
                var boneWeights2 = boneWeights[baseI2];

                var skinning0 =
                    (skinningMatrices[boneWeights0.boneIndex0] * boneWeights0.boneWeight0) +
                    (skinningMatrices[boneWeights0.boneIndex1] * boneWeights0.boneWeight1) +
                    (skinningMatrices[boneWeights0.boneIndex2] * boneWeights0.boneWeight2) +
                    (skinningMatrices[boneWeights0.boneIndex3] * boneWeights0.boneWeight3) +
                    (skinningMatrices[boneWeights0.boneIndex4] * boneWeights0.boneWeight4) +
                    (skinningMatrices[boneWeights0.boneIndex5] * boneWeights0.boneWeight5) +
                    (skinningMatrices[boneWeights0.boneIndex6] * boneWeights0.boneWeight6) +
                    (skinningMatrices[boneWeights0.boneIndex7] * boneWeights0.boneWeight7);

                var skinning1 =
                    (skinningMatrices[boneWeights1.boneIndex0] * boneWeights1.boneWeight0) +
                    (skinningMatrices[boneWeights1.boneIndex1] * boneWeights1.boneWeight1) +
                    (skinningMatrices[boneWeights1.boneIndex2] * boneWeights1.boneWeight2) +
                    (skinningMatrices[boneWeights1.boneIndex3] * boneWeights1.boneWeight3) +
                    (skinningMatrices[boneWeights1.boneIndex4] * boneWeights1.boneWeight4) +
                    (skinningMatrices[boneWeights1.boneIndex5] * boneWeights1.boneWeight5) +
                    (skinningMatrices[boneWeights1.boneIndex6] * boneWeights1.boneWeight6) +
                    (skinningMatrices[boneWeights1.boneIndex7] * boneWeights1.boneWeight7);

                var skinning2 =
                    (skinningMatrices[boneWeights2.boneIndex0] * boneWeights2.boneWeight0) +
                    (skinningMatrices[boneWeights2.boneIndex1] * boneWeights2.boneWeight1) +
                    (skinningMatrices[boneWeights2.boneIndex2] * boneWeights2.boneWeight2) +
                    (skinningMatrices[boneWeights2.boneIndex3] * boneWeights2.boneWeight3) +
                    (skinningMatrices[boneWeights2.boneIndex4] * boneWeights2.boneWeight4) +
                    (skinningMatrices[boneWeights2.boneIndex5] * boneWeights2.boneWeight5) +
                    (skinningMatrices[boneWeights2.boneIndex6] * boneWeights2.boneWeight6) +
                    (skinningMatrices[boneWeights2.boneIndex7] * boneWeights2.boneWeight7);

                var v0 = math.transform(skinning0, vertices[i0] + deltas[deltasStartIndex + baseI0].positionDelta);
                var v1 = math.transform(skinning1, vertices[i1] + deltas[deltasStartIndex + baseI1].positionDelta);
                var v2 = math.transform(skinning2, vertices[i2] + deltas[deltasStartIndex + baseI2].positionDelta);

                var output = new RaycastResult();
                output.didHit = Maths.seg_intersect_triangle_include_dist(origin, offset, v0, v1, v2, out Maths.RaycastHitResult result, errorMargin);
                result.triangleIndex = index;
                output.hitInfo = result;

                if (output.didHit) results.Enqueue(output);
            }

        }

        [BurstCompile]
        private struct ClosestRaycastHitFinalJob : IJob
        {

            public NativeQueue<RaycastResult> outputs;

            public NativeArray<RaycastResult> finalOutput;

            public void Execute()
            {
                RaycastResult min = new RaycastResult() { hitInfo = new Maths.RaycastHitResult() { distance = float.MaxValue } };

                while (outputs.TryDequeue(out var f))
                {
                    if (f.didHit & f.hitInfo.distance < min.hitInfo.distance)
                    {
                        min = f;
                    }
                }

                finalOutput[0] = min;
            }
        }

        #endregion

        #region IMuscularBasic

        public const int _dualMuscleGroupIndexOffset = 10000;

        public static int ConvertDefaultIndexForArray(int defaultIndex)
        {
            int convertedDefaultIndex = defaultIndex;
            if (convertedDefaultIndex >= _dualMuscleGroupIndexOffset)
            {
                convertedDefaultIndex = convertedDefaultIndex - _dualMuscleGroupIndexOffset;
            }

            return convertedDefaultIndex;
        }
        public static int ConvertDefaultMuscleGroupIndexToLocal(int defaultIndex, out int convertedDefaultIndex, out bool isBothSides) 
        {
            isBothSides = false;

            convertedDefaultIndex = defaultIndex;
            if (convertedDefaultIndex >= _dualMuscleGroupIndexOffset)
            {
                isBothSides = true;
                convertedDefaultIndex = convertedDefaultIndex - _dualMuscleGroupIndexOffset;
            }

            return convertedDefaultIndex / 2; 
        }
        public static int ConvertDefaultMuscleGroupIndexToLocal(int defaultIndex, out bool isBothSides) => ConvertDefaultMuscleGroupIndexToLocal(defaultIndex, out _, out isBothSides);
        public static int ConvertDefaultMuscleGroupIndexToLocal(int defaultIndex, out int convertedDefaultIndex) => ConvertDefaultMuscleGroupIndexToLocal(defaultIndex, out convertedDefaultIndex, out _);
        public static int ConvertDefaultMuscleGroupIndexToLocal(int defaultIndex) => ConvertDefaultMuscleGroupIndexToLocal(defaultIndex, out _, out _);

        [Serializable]
        public class DefaultMuscleGroupConversion
        {
            public MuscleGroup basicMuscleGroup;
            public string muscleGroupName;

            [NonSerialized]
            public int cachedIndex;
        }

        public string GetMuscleGroupName(int index) => GetMuscleGroupNameUnsafe(index);
        public string GetMuscleGroupNameUnsafe(int index)
        {
            var defaultGroup = SubData.ConvertMuscleGroupIndexToDefault(index); 
            return defaultGroup.ToString();  
        }
        public int GetMuscleGroupIndex(string muscleGroupName) 
        {
            if (Enum.TryParse(muscleGroupName, true, out MuscleGroupsDefault defaultGroup)) return SubData.ConvertDefaultMuscleGroupToIndex(defaultGroup);

            var baseGroup = MuscleGroupsDefaultExtensions.GetMuscleGroupBase(muscleGroupName);
            if (baseGroup != MuscleGroup.Null)
            {
                if (Enum.TryParse(baseGroup.ToString(), true, out defaultGroup)) return SubData.ConvertDefaultMuscleGroupToIndex(defaultGroup); 
            }
            
            return SubData.IndexOfMuscleGroup(muscleGroupName) * 2;
        }
        public int GetMuscleGroupIndex(MuscleGroupIdentifier identifier) => GetMuscleGroupIndex(identifier.ToString());

        public int GetMuscleGroupIndexForArray(string muscleGroupName)
        {
            int ind = GetMuscleGroupIndex(muscleGroupName);
            return ConvertDefaultIndexForArray(ind);
        }
        public int GetMuscleGroupIndexForArray(MuscleGroupIdentifier identifier) => GetMuscleGroupIndexForArray(identifier.ToString()); 

        public int FindMuscleGroup(string muscleGroupName) => GetMuscleGroupIndexForArray(muscleGroupName);
        public int FindMuscleGroup(MuscleGroupIdentifier identifier) => FindMuscleGroup(identifier.ToString());

        public int MuscleGroupCount => SubData.MuscleGroupsCount * 2;

        public float BreastPresence
        {
            get => BustSize;
            set => BustSize = value;
        }

        public bool SetMuscleGroupValues(int muscleGroupIndex, float3 values, bool updateDependencies = true)
        {
            int localGroupIndex = ConvertDefaultMuscleGroupIndexToLocal(muscleGroupIndex, out bool bothSides);

            var defaultGroup = SubData.ConvertLocalMuscleGroupToDefault(localGroupIndex);
            bool isSymmetrical = bothSides || defaultGroup.IsSymmetrical();

            var data = GetMuscleData(localGroupIndex);
            if (isSymmetrical)
            {
                data.valuesLeft.mass = values.x;
                data.valuesLeft.flex = values.y;
                data.valuesLeft.pump = values.z;

                data.valuesRight.mass = values.x;
                data.valuesRight.flex = values.y;
                data.valuesRight.pump = values.z;
            }
            else
            {
                bool isLeft = muscleGroupIndex % 2 == 0;
                if (isLeft)
                {
                    data.valuesLeft.mass = values.x;
                    data.valuesLeft.flex = values.y;
                    data.valuesLeft.pump = values.z;
                }
                else
                {
                    data.valuesRight.mass = values.x;
                    data.valuesRight.flex = values.y;
                    data.valuesRight.pump = values.z;
                }
            }

            SetMuscleData(localGroupIndex, data);
            return true;
        }
        public bool SetMuscleGroupMass(int muscleGroupIndex, float mass, bool updateDependencies = true, bool hasUpdated = false)
        {
            int localGroupIndex = ConvertDefaultMuscleGroupIndexToLocal(muscleGroupIndex, out bool bothSides);

            var defaultGroup = SubData.ConvertLocalMuscleGroupToDefault(localGroupIndex);
            bool isSymmetrical = bothSides || defaultGroup.IsSymmetrical();

            var data = GetMuscleData(localGroupIndex); 
            if (isSymmetrical)
            {
                data.valuesLeft.mass = mass;
                data.valuesRight.mass = mass;
            }
            else
            {
                bool isLeft = muscleGroupIndex % 2 == 0;
                if (isLeft)
                {
                    data.valuesLeft.mass = mass;
                }
                else
                {
                    data.valuesRight.mass = mass;
                }
            }

            SetMuscleData(localGroupIndex, data);
            return true;
        }
        public bool SetMuscleGroupFlex(int muscleGroupIndex, float flex, bool updateDependencies = true, bool hasUpdated = false)
        {
            int localGroupIndex = ConvertDefaultMuscleGroupIndexToLocal(muscleGroupIndex, out bool bothSides);

            var defaultGroup = SubData.ConvertLocalMuscleGroupToDefault(localGroupIndex);
            bool isSymmetrical = bothSides || defaultGroup.IsSymmetrical();

            var data = GetMuscleData(localGroupIndex);
            if (isSymmetrical)
            {
                data.valuesLeft.flex = flex;
                data.valuesRight.flex = flex;
            }
            else
            {
                bool isLeft = muscleGroupIndex % 2 == 0;
                if (isLeft)
                {
                    data.valuesLeft.flex = flex;
                }
                else
                {
                    data.valuesRight.flex = flex;
                }
            }

            SetMuscleData(localGroupIndex, data);
            return true;
        }
        public bool SetMuscleGroupPump(int muscleGroupIndex, float pump, bool updateDependencies = true, bool hasUpdated = false)
        {
            int localGroupIndex = ConvertDefaultMuscleGroupIndexToLocal(muscleGroupIndex, out bool bothSides);

            var defaultGroup = SubData.ConvertLocalMuscleGroupToDefault(localGroupIndex);
            bool isSymmetrical = bothSides || defaultGroup.IsSymmetrical();

            var data = GetMuscleData(localGroupIndex);
            if (isSymmetrical)
            {
                data.valuesLeft.pump = pump;
                data.valuesRight.pump = pump;
            }
            else
            {
                bool isLeft = muscleGroupIndex % 2 == 0;
                if (isLeft)
                {
                    data.valuesLeft.pump = pump;
                }
                else
                {
                    data.valuesRight.pump = pump;
                }
            }

            SetMuscleData(localGroupIndex, data);
            return true;
        }

        public bool SetMuscleGroupValuesUnsafe(int muscleGroupIndex, float3 values, bool updateDependencies = true) => SetMuscleGroupValues(muscleGroupIndex, values, updateDependencies);
        public bool SetMuscleGroupMassUnsafe(int muscleGroupIndex, float mass, bool updateDependencies = true, bool hasUpdated = false) => SetMuscleGroupMass(muscleGroupIndex, mass, updateDependencies, hasUpdated);
        public bool SetMuscleGroupFlexUnsafe(int muscleGroupIndex, float flex, bool updateDependencies = true, bool hasUpdated = false) => SetMuscleGroupFlex(muscleGroupIndex, flex, updateDependencies, hasUpdated);
        public bool SetMuscleGroupPumpUnsafe(int muscleGroupIndex, float pump, bool updateDependencies = true, bool hasUpdated = false) => SetMuscleGroupPump(muscleGroupIndex, pump, updateDependencies, hasUpdated);

        public float3 GetMuscleGroupValues(int muscleGroupIndex)
        {
            int localGroupIndex = ConvertDefaultMuscleGroupIndexToLocal(muscleGroupIndex, out int defaultIndex, out bool bothSides);

            var defaultGroup = SubData.ConvertLocalMuscleGroupToDefault(localGroupIndex);
            bool isSymmetrical = bothSides || defaultGroup.IsSymmetrical();
            bool isLeft = isSymmetrical || muscleGroupIndex % 2 == 0; 

            var data = GetMuscleData(localGroupIndex);
            return isLeft ? new float3(data.valuesLeft.mass, data.valuesLeft.flex, data.valuesLeft.pump) : new float3(data.valuesRight.mass, data.valuesRight.flex, data.valuesRight.pump);
        }
        public float3 GetMuscleGroupValuesUnsafe(int muscleGroupIndex) => GetMuscleGroupValues(muscleGroupIndex);

        public float GetMuscleGroupMass(int muscleGroupIndex) => GetMuscleGroupValues(muscleGroupIndex).x;
        public float GetMuscleGroupMassUnsafe(int muscleGroupIndex) => GetMuscleGroupMass(muscleGroupIndex);

        public float GetMuscleGroupFlex(int muscleGroupIndex) => GetMuscleGroupValues(muscleGroupIndex).y;
        public float GetMuscleGroupFlexUnsafe(int muscleGroupIndex) => GetMuscleGroupFlex(muscleGroupIndex);

        public float GetMuscleGroupPump(int muscleGroupIndex) => GetMuscleGroupValues(muscleGroupIndex).z;
        public float GetMuscleGroupPumpUnsafe(int muscleGroupIndex) => GetMuscleGroupPump(muscleGroupIndex);

        public void SetGlobalMuscleValues(float3 values)
        {
            for(int a = 0; a < MuscleGroupCount; a++)
            {
                SetMuscleGroupValuesUnsafe(a, values);
            }
        }
        public void SetGlobalMass(float mass)
        {
            for (int a = 0; a < MuscleGroupCount; a++)
            {
                SetMuscleGroupMassUnsafe(a, mass);
            }
        }
        public void SetGlobalFlex(float flex)
        {
            for (int a = 0; a < MuscleGroupCount; a++)
            {
                SetMuscleGroupFlexUnsafe(a, flex);
            }
        }
        public void SetGlobalPump(float pump)
        {
            for (int a = 0; a < MuscleGroupCount; a++)
            {
                SetMuscleGroupPumpUnsafe(a, pump);
            }
        }

        public float3 GetAverageMuscleValues()
        {
            float3 values = float3.zero;
            for (int a = 0; a < MuscleGroupCount; a++)
            {
                values = values + GetMuscleGroupValues(a);
            }

            return values;
        }
        public float GetAverageMass()
        {
            float mass = 0f;
            for (int a = 0; a < MuscleGroupCount; a++)
            {
                mass = mass + GetMuscleGroupMass(a);
            }

            return mass;
        }
        public float GetAverageFlex()
        {
            float flex = 0f;
            for (int a = 0; a < MuscleGroupCount; a++)
            {
                flex = flex + GetMuscleGroupMass(a);
            }

            return flex;
        }
        public float GetAveragePump()
        {
            float pump = 0f;
            for (int a = 0; a < MuscleGroupCount; a++)
            {
                pump = pump + GetMuscleGroupMass(a);
            }

            return pump;
        }

        public void ClearEventListeners()
        {
            ClearListeners();
        }

        private List<MuscleValueListener>[] muscleValueListeners;

        public bool Listen(int muscleGroupIndex, EngineInternal.IEngineObject listeningObject, MuscleValueListenerDelegate callback, out MuscleValueListener listener)
        {
            muscleGroupIndex = ConvertDefaultMuscleGroupIndexToLocal(muscleGroupIndex, out int defaultIndex, out bool bothSides);

            listener = null;
            if (listeningObject == null || callback == null || muscleGroupIndex < 0 || muscleGroupIndex >= SubData.MuscleGroupsCount) return false;

            if (muscleValueListeners == null) muscleValueListeners = new List<MuscleValueListener>[MuscleGroupCount];

            if (bothSides) defaultIndex = muscleGroupIndex * 2;

            List<MuscleValueListener> listeners = muscleValueListeners[defaultIndex];
            if (listeners == null)
            {
                listeners = new List<MuscleValueListener>();
                muscleValueListeners[defaultIndex] = listeners;
            }

            listener = new MuscleValueListener() { listeningObject = listeningObject, callback = callback };
            listeners.Add(listener);

            return true;

        }

        public bool StopListening(int muscleGroupIndex, EngineInternal.IEngineObject listeningObject)
        {

            if (listeningObject == null || muscleValueListeners == null || muscleGroupIndex < 0 || muscleGroupIndex >= MuscleGroupCount) return false;

            List<MuscleValueListener> listeners = muscleValueListeners[muscleGroupIndex];
            if (listeners != null)
            {

                return listeners.RemoveAll(i => i.listeningObject == listeningObject) > 0;

            }

            return false;

        }

        public int StopListening(EngineInternal.IEngineObject listeningObject)
        {

            if (listeningObject == null || muscleValueListeners == null) return 0;

            int removed = 0;

            for (int a = 0; a < muscleValueListeners.Length; a++)
            {

                List<MuscleValueListener> listeners = muscleValueListeners[a];
                if (listeners != null) removed += listeners.RemoveAll(i => i.listeningObject == listeningObject); 

            }

            return removed;

        }

        private void NotifyDefaultMuscleGroupListeners(int muscleGroupIndex)
        {
            muscleGroupIndex = ConvertDefaultMuscleGroupIndexToLocal(muscleGroupIndex, out int defaultIndex);

            if (muscleValueListeners != null && muscleGroupIndex >= 0 && muscleGroupIndex < SubData.MuscleGroupsCount)
            {
                List<MuscleValueListener> listeners = muscleValueListeners[defaultIndex];
                if (listeners != null)
                {
                    bool isLeft = defaultIndex % 2 == 0;
                    int mirrorMuscleGroupIndex = isLeft ? defaultIndex + 1 : defaultIndex - 1; 
                    var data = GetMuscleData(muscleGroupIndex);

                    foreach (var listener in listeners)
                    {
                        if (listener.listeningObject != null && listener.callback != null)
                        {
                            MuscleGroupInfo info = new MuscleGroupInfo()
                            {
                                mirroredIndex = mirrorMuscleGroupIndex,
                                mass = isLeft ? data.valuesLeft.mass : data.valuesRight.mass,
                                flex = isLeft ? data.valuesLeft.flex : data.valuesRight.flex,
                                pump = isLeft ? data.valuesLeft.pump : data.valuesRight.pump
                            };

                            listener.callback.Invoke(info);
                        }
                    }
                }
            }
        }

        #endregion

    }

}

#endif