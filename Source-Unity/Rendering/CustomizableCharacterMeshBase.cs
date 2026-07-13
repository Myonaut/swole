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

    public abstract class CustomizableCharacterMeshBase : InstanceableSkinnedMeshBase, ICustomizableCharacter
    {

        #region Editor

        public void UpdateInEditor()
        {
#if UNITY_EDITOR
            if (Application.isPlaying && isActiveAndEnabled)
            {
                var meshData = CustomizationData;
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
                        for (int a = 0; a < prevShapeWeightsEditor.Length; a++) prevShapeWeightsEditor[a] = new NamedFloat() { name = meshData.GetStandaloneShapeInfo(a).name };
                    }
                    if (shapeWeightsEditor == null || shapeWeightsEditor.Length == 0)
                    {
                        shapeWeightsEditor = new NamedFloat[meshData.StandaloneShapesCount];
                        for (int a = 0; a < shapeWeightsEditor.Length; a++) shapeWeightsEditor[a] = new NamedFloat() { name = meshData.GetStandaloneShapeInfo(a).name };
                    }

                    if (prevMuscleWeightsEditor == null || prevMuscleWeightsEditor.Length == 0)
                    {
                        prevMuscleWeightsEditor = new NamedMuscleData[meshData.MuscleVertexGroupCount];
                        for (int a = 0; a < prevMuscleWeightsEditor.Length; a++) prevMuscleWeightsEditor[a] = new NamedMuscleData() { name = meshData.GetVertexGroupInfo(a + meshData.MuscleGroups.x).name };
                    }
                    if (muscleWeightsEditor == null || muscleWeightsEditor.Length == 0)
                    {
                        muscleWeightsEditor = new NamedMuscleData[meshData.MuscleVertexGroupCount];
                        for (int a = 0; a < muscleWeightsEditor.Length; a++) muscleWeightsEditor[a] = new NamedMuscleData() { name = meshData.GetVertexGroupInfo(a + meshData.MuscleGroups.x).name };
                    }

                    if (prevFatWeightsEditor == null || prevFatWeightsEditor.Length == 0)
                    {
                        prevFatWeightsEditor = new NamedFloat[meshData.FatVertexGroupCount];
                        for (int a = 0; a < prevFatWeightsEditor.Length; a++) prevFatWeightsEditor[a] = new NamedFloat() { name = meshData.GetVertexGroupInfo(a + meshData.FatGroups.x).name };
                    }
                    if (fatWeightsEditor == null || fatWeightsEditor.Length == 0)
                    {
                        fatWeightsEditor = new NamedFloat[meshData.FatVertexGroupCount];
                        for (int a = 0; a < fatWeightsEditor.Length; a++) fatWeightsEditor[a] = new NamedFloat() { name = meshData.GetVertexGroupInfo(a + meshData.FatGroups.x).name };
                    }

                    if (prevBodyHairWeightsEditor == null || prevBodyHairWeightsEditor.Length == 0)
                    {
                        prevBodyHairWeightsEditor = new NamedFloat2[meshData.FatVertexGroupCount];
                        for (int a = 0; a < prevBodyHairWeightsEditor.Length; a++) prevBodyHairWeightsEditor[a] = new NamedFloat2() { name = meshData.GetVertexGroupInfo(a + meshData.FatGroups.x).name };
                    }
                    if (bodyHairWeightsEditor == null || bodyHairWeightsEditor.Length == 0)
                    {
                        bodyHairWeightsEditor = new NamedFloat2[meshData.FatVertexGroupCount];
                        for (int a = 0; a < bodyHairWeightsEditor.Length; a++) bodyHairWeightsEditor[a] = new NamedFloat2() { name = meshData.GetVertexGroupInfo(a + meshData.FatGroups.x).name };
                    }

                    if (prevVariationWeightsEditor == null || prevVariationWeightsEditor.Length == 0)
                    {
                        prevVariationWeightsEditor = new NamedFloat2[VariationShapesControlDataSize];
                        for (int a = 0; a < prevVariationWeightsEditor.Length; a++)
                        {
                            int groupIndex = a / meshData.VariationShapesCount;
                            int shapeIndex = a % meshData.VariationShapesCount;
                            prevVariationWeightsEditor[a] = new NamedFloat2() { name = meshData.GetVariationGroupInfo(groupIndex).name + "_" + meshData.GetVariationShapeInfo(shapeIndex).name };
                        }
                    }
                    if (variationWeightsEditor == null || variationWeightsEditor.Length == 0)
                    {
                        variationWeightsEditor = new NamedFloat2[VariationShapesControlDataSize];
                        for (int a = 0; a < variationWeightsEditor.Length; a++)
                        {
                            int groupIndex = a / meshData.VariationShapesCount;
                            int shapeIndex = a % meshData.VariationShapesCount;
                            variationWeightsEditor[a] = new NamedFloat2() { name = meshData.GetVariationGroupInfo(groupIndex).name + "_" + meshData.GetVariationShapeInfo(shapeIndex).name };
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

        #region Sub Types

        [Serializable, NonAnimatable]
        public struct RenderSet
        {
            public int materialIndexStart;
            public int materialCount;
        }

        public interface ICustomizableCharacterMeshBaseData : IDisposable
        {

            #region Fields

            public string Name { get; }

            public bool HasMaterials { get; }
            public int MaterialCount { get; }
            public Material GetMaterial(int index);

            public bool HasRenderSets { get; }
            public int RenderSetCount { get; }
            public RenderSet GetRenderSet(int index);
            public RenderSet[] RenderSets { get; }

            public int VertexCount { get; }

            public Mesh Mesh { get; }
            public int LevelsOfDetail { get; }
            public Mesh GetMesh(int lod);
            public Mesh GetMeshUnsafe(int lod);
            public MeshLOD GetLOD(int lod);
            public MeshLOD GetLODUnsafe(int lod);

            public bool TryGetVertices(int lod, out NativeArray<float3> array);
            public bool TryGetNormals(int lod, out NativeArray<float3> array);
            public bool TryGetTangents(int lod, out NativeArray<float4> array);
            public bool TryGetColors(int lod, out NativeArray<float4> array);
            public bool TryGetTriangles(int lod, out NativeArray<int> array);

            public bool TryGetBoneWeights(int lod, out NativeArray<BoneWeight8> array);

            public bool TryGetUV0(int lod, out NativeArray<float4> array);
            public bool TryGetUV1(int lod, out NativeArray<float4> array);
            public bool TryGetUV2(int lod, out NativeArray<float4> array);
            public bool TryGetUV3(int lod, out NativeArray<float4> array);
            public bool TryGetUV(int lod, UVChannelURP channel, out NativeArray<float4> array);


            public Vector3 BoundsCenter { get; }
            public Vector3 BoundsExtents { get; }

            public bool HasLeftRightFlags { get; }
            public bool GetLeftRightFlag(int vertexIndex);
            public bool[] LeftRightFlags { get; }

            public bool HasBaseBoneWeights { get; }
            public BoneWeight8 GetBaseBoneWeight(int vertexIndex);
            public BoneWeight8[] BaseBoneWeights { get; }

            public NativeArray<BoneWeight8> BaseBoneWeightsJob { get; }


            public bool HasBonesArray { get; }
            public string[] BoneNames { get; }
            public bool HasManagedBindPose { get; }
            public Matrix4x4[] ManagedBindPose { get; }

            public int BoneCount { get; }

            #region Shapes

            public Vector2Int StandaloneShapes { get; }

            public Vector2Int VariationShapes { get; }

            public int MassShape { get; }

            public int FlexShape { get; }

            public int FatShape { get; }

            public int FatMuscleBlendShape { get; }
            public Vector2 FatMuscleBlendWeightRange { get; }

            public int BustSizeShape { get; }
            public int BustShapeShape { get; }
            public int BustSizeMuscleShape { get; }

            #endregion

            #region Groups

            public int MidlineVertexGroup { get; }
            public int BustVertexGroup { get; }
            public int BustNerfVertexGroup { get; }

            public int NippleMaskVertexGroup { get; }
            public int GenitalMaskVertexGroup { get; }

            public float DefaultMassShapeWeight { get; }
            public float MinMassShapeWeight { get; }

            public Vector2Int StandaloneGroups { get; }

            public Vector2Int VariationGroups { get; }

            public Vector2Int MuscleGroups { get; }

            public Vector2Int FatGroups { get; }

            #endregion

            #region Material Property Names

            public string VertexCountPropertyName { get; }

            public string SkinningDataPropertyName { get; }

            public string BoneCountPropertyName { get; }

            public string SkinningMatricesPropertyName { get; }

            public string MidlineVertexGroupIndexPropertyName { get; }

            public string BustMixPropertyName { get; }

            public string HideNipplesPropertyName { get; }

            public string HideGenitalsPropertyName { get; }

            public string BustVertexGroupIndexPropertyName { get; }

            public string BustNerfVertexGroupIndexPropertyName { get; }

            public string NippleMaskVertexGroupIndexPropertyName { get; }

            public string GenitalMaskVertexGroupIndexPropertyName { get; }


            public string BustSizeShapeIndexPropertyName { get; }

            public string BustSizeMuscularShapeIndexPropertyName { get; }


            public string FatMuscleBlendShapeIndexPropertyName { get; }

            public string FatMuscleBlendWeightRangePropertyName { get; }

            public string DefaultShapeMuscleWeightPropertyName { get; }

            public string StandaloneShapesControlPropertyName { get; }

            public string MuscleGroupsControlPropertyName { get; }

            public string FatGroupsControlPropertyName { get; }

            public string VariationShapesControlPropertyName { get; }


            public string MuscleMassShapeIndexPropertyName { get; }

            public string FlexShapeIndexPropertyName { get; }

            public string FatShapeIndexPropertyName { get; }


            public string VertexGroupsPropertyName { get; }

            public string MeshShapeFrameDeltasPropertyName { get; }

            public string MeshShapeFrameWeightsPropertyName { get; }

            public string MeshShapeIndicesPropertyName { get; }


            public string MuscleGroupInfluencesPropertyName { get; }

            public string FatGroupInfluencesPropertyName { get; }

            public string PerVertexDeltaDataPropertyName { get; }


            public string LocalInstanceIDPropertyName { get; }

            public string ShapesInstanceIDPropertyName { get; }

            public string RigInstanceIDPropertyName { get; }

            public string CharacterInstanceIDPropertyName { get; }



            public string VertexColorDeltasPropertyName { get; }


            public string MinMassShapeWeightPropertyName { get; }


            public string FlexEndPointWeightPropertyName { get; }

            public string FlexExponentPropertyName { get; }

            public string FlexNerfThresholdPropertyName { get; }

            public string FlexNerfExponentPropertyName { get; }

            #endregion


            public float FlexEndPointWeight { get; }
            public float FlexExponent { get; }
            public float FlexNerfThreshold { get; }
            public float FlexNerfExponent { get; }
            public int RaycastLOD { get; }

            public UVChannelURP NearestVertexUVChannel { get; }

            public RGBAChannel NearestVertexIndexElement { get; }

            public string ConvertDefaultMuscleGroupName(MuscleGroupsDefault defaultGroup);
            public int ConvertDefaultMuscleGroupToIndex(MuscleGroupsDefault defaultGroup);
            public MuscleGroupsDefault ConvertLocalMuscleGroupToDefault(string muscleGroupName);
            public MuscleGroupsDefault ConvertLocalMuscleGroupToDefault(int muscleGroupIndex);
            public MuscleGroupsDefault ConvertMuscleGroupIndexToDefault(int muscleGroupIndex);

            #endregion

            #region Interface

            public List<BoneWeight8Float> GetConvertedBoneWeightData(List<BoneWeight8Float> outputList = null);

            public ComputeBuffer BoneWeightsBuffer { get; }
            public ComputeBuffer MuscleGroupInfluencesBuffer { get; }

            public ComputeBuffer FatGroupInfluencesBuffer { get; }

            public int MeshShapeCount { get; }
            public int MeshShapeDeltasCount { get; }
            public ShapeInfo GetShapeInfo(int index);
            public ShapeInfo GetShapeInfoUnsafe(int index);
            public int IndexOfShape(string shapeName, bool caseSensitive = false);
            public List<ShapeInfo> GetShapeInfos(List<ShapeInfo> outputList = null);

            public string VertexGroupsBufferRangePropertyName { get; }
            public int VertexGroupCount { get; }
            public VertexGroupInfo GetVertexGroupInfo(int index);
            public VertexGroupInfo GetVertexGroupInfoUnsafe(int index);
            public int IndexOfVertexGroup(string vertexGroupName, bool caseSensitive = false);
            public List<VertexGroupInfo> GetVertexGroupInfos(List<VertexGroupInfo> outputList = null);

            
            public string StandaloneVertexGroupsBufferRangePropertyName { get; }
            public int StandaloneGroupsCount { get; }
            public int StandaloneVertexGroupCount { get; }
            public int IndexOfStandaloneVertexGroup(string name, bool caseSensitive = false);
            public VertexGroupInfo GetStandaloneVertexGroupInfo(int index);

            public string MuscleVertexGroupsBufferRangePropertyName { get; }
            public int MuscleGroupsCount { get; }
            public int MuscleVertexGroupCount { get; }
            public int IndexOfMuscleGroup(string name, bool caseSensitive = false);
            public VertexGroupInfo GetMuscleVertexGroupInfo(int index);

            public string FatVertexGroupsBufferRangePropertyName { get; }
            public int FatGroupsCount { get; }
            public int FatVertexGroupCount { get; }
            public int IndexOfFatGroup(string name, bool caseSensitive = false);
            public VertexGroupInfo GetFatVertexGroupInfo(int index);
            public static float2 DefaultFatGroupModifier { get; }
            /// <summary>
            /// modifier.x is how much to nerf muscle mass by based on fat level
            /// </summary>
            public float2 GetFatGroupModifier(int index);
            public bool HasFatGroupModifiers { get; }

            public string VariationVertexGroupsBufferRangePropertyName { get; }
            public int VariationGroupsCount { get; }
            public int VariationVertexGroupCount { get; }

            public int IndexOfVariationGroup(string name, bool caseSensitive = false);
            public VertexGroupInfo GetVariationGroupInfo(int index);

            public int VariationShapesControlDataSize { get; }


            public string StandaloneShapesBufferRangePropertyName { get; }
            public int StandaloneShapesCount { get; }
            public int IndexOfStandaloneShape(string name, bool caseSensitive = false);
            public ShapeInfo GetStandaloneShapeInfo(int index);

            public ShapeInfo MassShapeInfo { get; }
            public int MassShapeFrameCount { get; }

            public ShapeInfo FlexShapeInfo { get; }
            public int FlexShapeFrameCount { get; }

            public ShapeInfo FatShapeInfo { get; }
            public int FatShapeFrameCount { get; }

            public ShapeInfo FatMuscleBlendShapeInfo { get; }
            public int FatMuscleBlendShapeFrameCount { get; }

            public ShapeInfo BustSizeShapeInfo { get; }
            public int BustSizeShapeFrameCount { get; }

            public ShapeInfo BustSizeMuscleShapeInfo { get; }
            public int BustSizeMuscleShapeFrameCount { get; }


            public string VariationShapesBufferRangePropertyName { get; }
            public int VariationShapesCount { get; }
            public int IndexOfVariationShape(string name, bool caseSensitive = false);
            public ShapeInfo GetVariationShapeInfo(int index);


            public ComputeBuffer VertexGroupsBuffer { get; }

            public ComputeBuffer MeshShapeFrameDeltasBuffer { get; }

            public ComputeBuffer MeshShapeFrameWeightsBuffer { get; }

            public ComputeBuffer MeshShapeIndicesBuffer { get; }

            public ComputeBuffer VertexColorDeltasBuffer { get; }

            #endregion

            #region Pre-Caching

            public bool IsPrecached { get; }

            public bool TryPrecache();
            public bool Precache();

            #endregion

        }

        [Serializable, StructLayout(LayoutKind.Sequential), NonAnimatable]
        public struct ControlWeight
        {
            public int index;
            public float weight;
        }
        [Serializable, StructLayout(LayoutKind.Sequential), NonAnimatable]
        public struct ControlWeight2
        {
            public int index;
            public float2 weight;
        }
        [Serializable, StructLayout(LayoutKind.Sequential), NonAnimatable]
        public struct ControlWeight4
        {
            public int index;
            public float4 weight;
        }
        [Serializable, StructLayout(LayoutKind.Sequential), NonAnimatable]
        public struct GroupControlWeight2
        {
            public int groupIndex;
            public int vertexSequenceStartIndex;
            public int vertexCount;
            public float2 weight;
        }
        [Serializable, StructLayout(LayoutKind.Sequential), NonAnimatable]
        public struct GroupControlWeight4
        {
            public int groupIndex;
            public int vertexSequenceStartIndex;
            public int vertexCount;
            public float4 weight;
        }
        [Serializable, StructLayout(LayoutKind.Sequential), NonAnimatable]
        public struct GroupVertexControlWeight
        {
            public int groupIndex;
            public int vertexIndex;

            public float weight;
        }
        [Serializable, StructLayout(LayoutKind.Sequential), NonAnimatable]
        public struct GroupVertexControlWeight2
        {
            public int groupIndex;
            public int vertexIndex;

            public float2 weight;
        }
        [Serializable, StructLayout(LayoutKind.Sequential), NonAnimatable]
        public struct GroupVertexControlWeight4
        {
            public int groupIndex;
            public int vertexIndex;

            public float4 weight;
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
            var Data = CustomizationData;
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

            for (int a = 0; a < syncedSkinnedMeshes.Count; a++)
            {
                var mesh = syncedSkinnedMeshes[a];
                if (mesh == null) continue;

                for (int b = 0; b < Data.StandaloneShapesCount; b++)
                {
                    var shape = Data.GetStandaloneShapeInfo(b);

                    int shapeIndex = mesh.sharedMesh.GetBlendShapeIndex(shape.name);
                    if (shapeIndex >= 0) standaloneShapeSyncs[b].Add(new BlendShapeSync() { listenerIndex = a, listenerShapeIndex = shapeIndex });
                }

                for (int b = 0; b < Data.MuscleVertexGroupCount; b++)
                {
                    for (int c = 0; c < Data.MassShapeFrameCount; c++)
                    {
                        int shapeIndexL = mesh.sharedMesh.GetBlendShapeIndex(GetMuscleMassShapeSyncNameLeft(b, c));
                        int shapeIndexR = mesh.sharedMesh.GetBlendShapeIndex(GetMuscleMassShapeSyncNameRight(b, c));

                        if (shapeIndexL >= 0 || shapeIndexR >= 0) muscleMassShapeSyncs[(b * Data.MassShapeFrameCount) + c].Add(new BlendShapeSyncLR() { listenerIndex = a, listenerShapeIndexLeft = shapeIndexL, listenerShapeIndexRight = shapeIndexR });
                    }

                    for (int c = 0; c < Data.FlexShapeFrameCount; c++)
                    {
                        int shapeIndexL = mesh.sharedMesh.GetBlendShapeIndex(GetMuscleFlexShapeSyncNameLeft(b, c));
                        int shapeIndexR = mesh.sharedMesh.GetBlendShapeIndex(GetMuscleFlexShapeSyncNameRight(b, c));

                        if (shapeIndexL >= 0 || shapeIndexR >= 0) muscleFlexShapeSyncs[(b * Data.FlexShapeFrameCount) + c].Add(new BlendShapeSyncLR() { listenerIndex = a, listenerShapeIndexLeft = shapeIndexL, listenerShapeIndexRight = shapeIndexR });
                    }
                }

                for (int b = 0; b < Data.FatVertexGroupCount; b++)
                {
                    for (int c = 0; c < Data.FatShapeFrameCount; c++)
                    {
                        int shapeIndex = mesh.sharedMesh.GetBlendShapeIndex(GetFatShapeSyncName(b, c));

                        if (shapeIndex >= 0) fatShapeSyncs[(b * Data.FatShapeFrameCount) + c].Add(new BlendShapeSync() { listenerIndex = a, listenerShapeIndex = shapeIndex });
                    }
                }

                for (int b = 0; b < Data.VariationVertexGroupCount; b++)
                {
                    for (int c = 0; c < Data.VariationShapesCount; c++)
                    {
                        int shapeIndexL = mesh.sharedMesh.GetBlendShapeIndex(GetVariationShapeSyncNameLeft(b, c));
                        int shapeIndexR = mesh.sharedMesh.GetBlendShapeIndex(GetVariationShapeSyncNameRight(b, c));

                        if (shapeIndexL >= 0 || shapeIndexR >= 0) variationShapeSyncs[(b * Data.VariationShapesCount) + c].Add(new BlendShapeSyncLR() { listenerIndex = a, listenerShapeIndexLeft = shapeIndexL, listenerShapeIndexRight = shapeIndexR });
                    }
                }
            }
        }

        protected void SyncMuscleMassData(int groupIndex, float massL, float massR)
        {
            var frameWeights = CustomizationData.MassShapeInfo.frameWeights;

            if (frameWeights != null) SyncPartialShapeData(muscleMassShapeSyncs, groupIndex, massL, massR, frameWeights.Length, frameWeights);
        }
        protected void SyncMuscleFlexData(int groupIndex, float flexL, float flexR)
        {
            var frameWeights = CustomizationData.FlexShapeInfo.frameWeights;

            if (frameWeights != null) SyncPartialShapeData(muscleFlexShapeSyncs, groupIndex, flexL, flexR, frameWeights.Length, frameWeights);
        }
        protected void SyncFatLevel(int groupIndex, float weight)
        {
            var frameWeights = CustomizationData.FatShapeInfo.frameWeights;

            if (frameWeights != null) SyncPartialShapeData(fatShapeSyncs, groupIndex, weight, frameWeights.Length, frameWeights);
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
                            skinningMatricesBuffer.UnbindMaterialProperty(mat, CustomizationData.SkinningMatricesPropertyName);
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
                            standaloneShapeControlBuffer.UnbindMaterialProperty(mat, CustomizationData.StandaloneShapesControlPropertyName);
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
                            muscleGroupsControlBuffer.UnbindMaterialProperty(mat, CustomizationData.MuscleGroupsControlPropertyName);
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
                            fatGroupsControlBuffer.UnbindMaterialProperty(mat, CustomizationData.FatGroupsControlPropertyName);
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
                            variationShapesControlBuffer.UnbindMaterialProperty(mat, CustomizationData.VariationShapesControlPropertyName);
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

        public abstract int InstanceID { get; }
        public override int InstanceSlot => InstanceID;
        public override bool IsInitialized => HasValidInstance;
        public virtual bool HasValidInstance => false;// instance != null && instance.IsValid;

        protected override void CreateInstance()
        { 
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

            if (CustomizationData != null)
            {
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

        #region Data

        public virtual void SetData(ICustomizableCharacterMeshBaseData data)
        {
            if (!TrySetData(data)) return;

            if (Application.isPlaying)
            {
                SetupSkinnedMeshSyncs();
                if (enabled) StartRendering();
            }
        }
        protected abstract bool TrySetData(ICustomizableCharacterMeshBaseData data);
        public abstract ICustomizableCharacterMeshBaseData CustomizationData { get; }


        public virtual bool TryGetVertices(int lod, out NativeArray<float3> array) => CustomizationData.TryGetVertices(lod, out array);
        public virtual bool TryGetColors(int lod, out NativeArray<float4> array) => CustomizationData.TryGetColors(lod, out array);
        public virtual bool TryGetTriangles(int lod, out NativeArray<int> array) => CustomizationData.TryGetTriangles(lod, out array);
        public virtual bool TryGetBoneWeights(int lod, out NativeArray<BoneWeight8> array) => CustomizationData.TryGetBoneWeights(lod, out array);

        public virtual bool TryGetUV0(int lod, out NativeArray<float4> array) => CustomizationData.TryGetUV0(lod, out array);
        public virtual bool TryGetUV1(int lod, out NativeArray<float4> array) => CustomizationData.TryGetUV1(lod, out array);
        public virtual bool TryGetUV2(int lod, out NativeArray<float4> array) => CustomizationData.TryGetUV2(lod, out array);
        public virtual bool TryGetUV3(int lod, out NativeArray<float4> array) => CustomizationData.TryGetUV3(lod, out array);
        public virtual bool TryGetUV(int lod, UVChannelURP channel, out NativeArray<float4> array) => CustomizationData.TryGetUV(lod, channel, out array);

        public virtual int DeltasStartIndex => CustomizationData.VertexCount * InstanceSlot;

        protected virtual bool PrepInWorldDataFetch(int lod, int vertexIndex, out int topVertexIndex, out MuscleData muscleData, out float flexFactor, out MeshVertexDelta delta, out MeshShape flexShape, out float4x4 skinningMatrix)
        {
            delta = default;
            flexShape = null;
            skinningMatrix = float4x4.identity;
            muscleData = default;
            flexFactor = 0f;
            topVertexIndex = vertexIndex;

            return false;
        }

        public virtual float4x4 GetVertexLocalToWorld(int lod, int vertexIndex)
        {
            return float4x4.identity;
        }

        public float3 GetVertexInWorld(int lod, int vertexIndex) => GetVertexInWorld(lod, vertexIndex, out _, out _);
        public virtual float3 GetVertexInWorld(int lod, int vertexIndex, out float4x4 skinningMatrix, out float3 localDelta)
        {
            skinningMatrix = float4x4.identity;
            localDelta = default;
            
            return default;
        }
        public float3 GetNormalInWorld(int lod, int vertexIndex) => GetNormalInWorld(lod, vertexIndex, out _, out _);
        public virtual float3 GetNormalInWorld(int lod, int vertexIndex, out float4x4 skinningMatrix, out float3 localDelta)
        {
            skinningMatrix = float4x4.identity;
            localDelta = default;
            
            return default;
        }
        public float4 GetTangentInWorld(int lod, int vertexIndex) => GetTangentInWorld(lod, vertexIndex, out _, out _);
        public virtual float4 GetTangentInWorld(int lod, int vertexIndex, out float4x4 skinningMatrix, out float3 localDelta)
        {
            skinningMatrix = float4x4.identity;
            localDelta = default;
            
            return default;
        }
        public void GetVertexInWorld(int lod, int vertexIndex, out float3 pos, out float3 normal, out float4 tangent) => GetVertexInWorld(lod, vertexIndex, out pos, out normal, out tangent, out _, out _, out _, out _);
        public virtual void GetVertexInWorld(int lod, int vertexIndex, out float3 pos, out float3 normal, out float4 tangent, out float4x4 skinningMatrix, out float3 localDeltaPos, out float3 localDeltaNorm, out float3 localDeltaTan)
        {
            skinningMatrix = float4x4.identity;
            localDeltaPos = default;
            localDeltaNorm = default;
            localDeltaTan = default;

            pos = default;
            normal = default;
            tangent = default;
        }

        public virtual List<float3> GetMuscleGroupsAffecting(int lod, int vertexIndex, List<float3> list = null)
        {
            if (list == null) list = new List<float3>();
            return list;
        }
        public virtual List<float3> GetFatGroupsAffecting(int lod, int vertexIndex, List<float3> list = null)
        {
            if (list == null) list = new List<float3>();
            return list;
        }
        public virtual List<float3> GetVariationGroupsAffecting(int lod, int vertexIndex, List<float3> list = null)
        {
            if (list == null) list = new List<float3>();
            return list;
        }


        public override InstanceableMeshDataBase MeshData => null;
        public override InstancedMeshGroup MeshGroup => null;

        public int IndexOfStandaloneShape(string name, bool caseSensitive = false)
        {
            var data = CustomizationData;
            if (data == null) return -1;
            return data.IndexOfStandaloneShape(name, caseSensitive);
        }

        public int IndexOfVertexGroup(string name, bool caseSensitive = false)
        {
            var data = CustomizationData;
            if (data == null) return -1;
            return data.IndexOfVertexGroup(name, caseSensitive);
        }
        public int IndexOfStandaloneVertexGroup(string name, bool caseSensitive = false)
        {
            var data = CustomizationData;
            if (data == null) return -1;
            return data.IndexOfStandaloneVertexGroup(name, caseSensitive);
        }
        public int IndexOfMuscleGroup(string name, bool caseSensitive = false)
        {
            var data = CustomizationData;
            if (data == null) return -1;
            return data.IndexOfMuscleGroup(name, caseSensitive);
        }
        public int IndexOfFatGroup(string name, bool caseSensitive = false)
        {
            var data = CustomizationData;
            if (data == null) return -1;
            return data.IndexOfFatGroup(name, caseSensitive);
        }
        public int IndexOfVariationGroup(string name, bool caseSensitive = false)
        {
            var data = CustomizationData;
            if (data == null) return -1;
            return data.IndexOfVariationGroup(name, caseSensitive);
        }
        public int IndexOfVariationShape(string name, bool caseSensitive = false)
        {
            var data = CustomizationData;
            if (data == null) return -1;
            return data.IndexOfVariationShape(name, caseSensitive);
        }

        #endregion

        #region Children

        protected List<ICustomizableCharacter.Child> children;
        public bool IsParentOf(ICustomizableCharacter child) => IsParentOf(child, out _);
        public bool IsParentOf(ICustomizableCharacter child, out int index)
        {
            index = -1;
            if (children == null) return false;

            for (int a = 0; a < children.Count; a++)
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
        public void AddChild(ICustomizableCharacter child, ICustomizableCharacter.ChildType type)
        {
            if (child == null) return;

            if (children == null) children = new List<ICustomizableCharacter.Child>();
            if (IsParentOf(child, out var childIndex))
            {
                var c = children[childIndex];
                c.type |= type;
                children[childIndex] = c;
            }
            else
            {
                children.Add(new ICustomizableCharacter.Child() { instance = child, type = type });
            }
        }
        public void RemoveChild(ICustomizableCharacter child, ICustomizableCharacter.ChildType type)
        {
            if (children == null) return;

            if (IsParentOf(child, out var childIndex))
            {
                var c = children[childIndex];
                c.type &= ~type;
                children[childIndex] = c;
                if (c.type == ICustomizableCharacter.ChildType.None) children.RemoveAt(childIndex);
            }
        }
        public void RemoveChild(ICustomizableCharacter child)
        {
            if (children == null) return;

            if (IsParentOf(child, out var childIndex))
            {
                children.RemoveAt(childIndex);
            }
        }

        #endregion

        #region Material Handling

        public virtual Material[] MaterialInstances => null;

        protected Dictionary<string, float> floatOverrides;// = new Dictionary<string, float>();
        protected Dictionary<string, int> intOverrides;// = new Dictionary<string, int>();
        protected Dictionary<string, Vector4> vectorOverrides;// = new Dictionary<string, Vector4>();
        protected Dictionary<string, Color> colorOverrides;// = new Dictionary<string, Color>();

        protected void ApplyCachedMaterialPropertyOverrides() => ApplyCachedMaterialPropertyOverrides(MaterialInstances);
        protected virtual void ApplyCachedMaterialPropertyOverrides(IEnumerable<Material> materialInstances)
        {
            if (materialInstances == null) return;

            if (floatOverrides != null)
            {
                foreach (var entry in floatOverrides)
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
            if (floatOverrides == null) floatOverrides = new Dictionary<string, float>();
            floatOverrides[property] = value;

            var materialInstances = MaterialInstances;
            if (materialInstances == null) return;

            foreach (var mat in materialInstances)
            {
                if (mat != null) mat.SetFloat(property, value);
            }
        }
        public virtual void SetFloatOverrideWithCheck(string property, float value, bool updateMaterials = true)
        {
            if (floatOverrides == null) floatOverrides = new Dictionary<string, float>();
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
            if (intOverrides == null) intOverrides = new Dictionary<string, int>();
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
            if (intOverrides == null) intOverrides = new Dictionary<string, int>();
            intOverrides[property] = value;

            var materialInstances = MaterialInstances;
            if (materialInstances == null) return;

            foreach (var mat in materialInstances)
            {
                if (mat != null && mat.HasProperty(property)) mat.SetInteger(property, value);
            }
        }

        public virtual void SetVectorOverride(string propertyName, Vector4 vector, bool updateMaterials = true)
        {
            if (vectorOverrides == null) vectorOverrides = new Dictionary<string, Vector4>();
            vectorOverrides[propertyName] = vector;

            var materialInstances = MaterialInstances;
            if (materialInstances == null) return;

            foreach (var mat in materialInstances)
            {
                if (mat != null) mat.SetVector(propertyName, vector);
            }
        }
        public virtual void SetVectorOverrideWithCheck(string propertyName, Vector4 vector, bool updateMaterials = true)
        {
            if (vectorOverrides == null) vectorOverrides = new Dictionary<string, Vector4>();
            vectorOverrides[propertyName] = vector;

            var materialInstances = MaterialInstances;
            if (materialInstances == null) return;

            foreach (var mat in materialInstances)
            {
                if (mat != null && mat.HasProperty(propertyName)) mat.SetVector(propertyName, vector);
            }
        }

        public virtual void SetColorOverride(string propertyName, Color color, bool updateMaterials = true)
        {
            if (colorOverrides == null) colorOverrides = new Dictionary<string, Color>();
            colorOverrides[propertyName] = color;

            var materialInstances = MaterialInstances;
            if (materialInstances == null) return;

            foreach (var mat in materialInstances)
            {
                if (mat != null) mat.SetColor(propertyName, color);
            }
        }
        public virtual void SetColorOverrideWithCheck(string propertyName, Color color, bool updateMaterials = true)
        {
            if (colorOverrides == null) colorOverrides = new Dictionary<string, Color>();
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

        protected class DefaultRenderedMesh
        {
            public MeshFilter meshFilter;
            public Renderer meshRenderer;

            public MeshFilter[] additionalFilters;
            public Renderer[] additionalRenderers;
        }

        protected LODGroup lodGroup;

        protected DefaultRenderedMesh[] defaultRenderedMeshes;

        public bool RenderingIsInitialized() => lodGroup != null && defaultRenderedMeshes != null;
        public bool IsRendering() => RenderingIsInitialized() && CanRender;
        public override bool IsRendering(int index) => IsRendering();
        public abstract bool CanRender { get; }

        public virtual void InitializeRendering()
        {

            if (RenderingIsInitialized() || !CanRender) return;

            var meshData = CustomizationData;

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

                Renderer[] renderers = CreateRenderersForLOD(a, meshLOD, rendererObj.transform, defaultRenderedMesh);

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
        protected abstract Renderer[] CreateRenderersForLOD(int lod, MeshLOD meshLOD, Transform renderersRoot, DefaultRenderedMesh defaultRenderedMesh);

        protected virtual void StartRendering()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) return;
#endif

            var cData = CustomizationData;
            if (cData == null) return;

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
                var renderSets = cData.RenderSets;
                if (renderSets == null || renderSets.Length <= 1)
                {
                    foreach (var renderedMesh in defaultRenderedMeshes)
                    {
                        if (renderedMesh.meshRenderer != null && materialInstances != null) renderedMesh.meshRenderer.sharedMaterials = materialInstances;
                    }
                }
                else
                {
                    foreach (var renderedMesh in defaultRenderedMeshes)
                    {
                        for (int i = 0; i < renderSets.Length; i++)
                        {
                            var renderSet = renderSets[i];

                            Renderer renderer = null;
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
        }

        public void ApplyIDsToMaterials()
        {
            var meshData = CustomizationData;
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
            set => SetBustShape(value);
        }
        //[NonSerialized]
        //protected bool hasBustSizeProperty;
        public virtual void SetBustSize(float value)
        {
            float prevValue = bustSize;
            bustSize = value;

            //if (SubData.bustSizeShape >= 0) SetStandaloneShapeWeightUnsafe(SubData.bustSizeShape, value);

            //if (hasBustSizeProperty)
            //{
                SetFloatOverrideWithCheck(CustomizationData.BustMixPropertyName, Mathf.Clamp01(value), true);
            //}

            if (children != null)
            {
                foreach (var child in children) if (child.IsValid && child.type.HasFlag(ICustomizableCharacter.ChildType.Mesh)) child.instance.SetBustSize(value);
            }
        }
        public virtual void SetBustShape(float value)
        {
            float prevValue = bustShape;
            bustShape = value;

            if (children != null)
            {
                foreach (var child in children) if (child.IsValid && child.type.HasFlag(ICustomizableCharacter.ChildType.Mesh)) child.instance.SetBustShape(value);
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
                SetFloatOverride(CustomizationData.HideNipplesPropertyName, hideNipples ? 1 : 0, true);
            }

            if (children != null)
            {
                foreach (var child in children) if (child.IsValid && child.type.HasFlag(ICustomizableCharacter.ChildType.Mesh)) child.instance.SetHideNipples(value);
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
                SetFloatOverride(CustomizationData.HideGenitalsPropertyName, hideGenitals ? 1 : 0, true);
            }

            if (children != null)
            {
                foreach (var child in children) if (child.IsValid && child.type.HasFlag(ICustomizableCharacter.ChildType.Mesh)) child.instance.SetHideGenitals(value);
            }
        }

        #endregion

        #region Customization Shapes & Groups

        public virtual void MarkForPhysiqueUpdate()
        {
        }

        public virtual void MarkForVariationUpdate()
        {
        }

        public abstract JobHandle MeshUpdateJobHandle { get; }

        public int FirstStandaloneShapesControlIndex => CharacterInstanceID * CustomizationData.StandaloneShapesCount;
        public virtual float GetStandaloneShapeWeightUnsafe(int shapeIndex) => StandaloneShapeControlBuffer[FirstStandaloneShapesControlIndex + shapeIndex];//StandaloneShapesControl[shapeIndex];
        public float GetStandaloneShapeWeight(int shapeIndex)
        {
            if (!IsInitialized || shapeIndex < 0 || shapeIndex >= CustomizationData.StandaloneShapesCount) return 0;
            return GetStandaloneShapeWeightUnsafe(shapeIndex);
        }
        protected virtual void SetStandaloneShapeWeightInternal(int shapeIndex, ref float weight)
        {
            if (shapesInstanceReference != null) return;
            StandaloneShapeControlBuffer[FirstStandaloneShapesControlIndex + shapeIndex] = weight;
        }
        public virtual void SetStandaloneShapeWeightUnsafe(int shapeIndex, float weight)
        {
            SetStandaloneShapeWeightInternal(shapeIndex, ref weight);

            SyncStandaloneShape(standaloneShapeSyncs, shapeIndex, weight);
        }
        public void SetStandaloneShapeWeight(int shapeIndex, float weight)
        {
            if (!IsInitialized || shapeIndex < 0 || shapeIndex >= CustomizationData.StandaloneShapesCount) return;
            SetStandaloneShapeWeightUnsafe(shapeIndex, weight);
        }

        public int FirstMuscleGroupsControlIndex => CharacterInstanceID * CustomizationData.MuscleVertexGroupCount;
        public virtual MuscleDataLR GetMuscleDataUnsafe(int groupIndex) => MuscleGroupsControlBuffer[FirstMuscleGroupsControlIndex + groupIndex];//MuscleGroupsControl[groupIndex];
        public MuscleDataLR GetMuscleData(int groupIndex)
        {
            if (!IsInitialized || groupIndex < 0 || groupIndex >= CustomizationData.MuscleVertexGroupCount) return default; 
            return GetMuscleDataUnsafe(groupIndex);
        }
        public UnityEvent<int> OnMuscleDataChanged;
        public UnityEvent OnMuscleDataChangedNoArg;
        protected virtual void SetMuscleDataInternal(int groupIndex, ref MuscleDataLR data)
        {
            if (characterInstanceReference == null)
            {
                int controlIndex = FirstMuscleGroupsControlIndex + groupIndex;
                //var prevData = MuscleGroupsControlBuffer[controlIndex];
                MuscleGroupsControlBuffer[controlIndex] = data;
            }
        }
        public virtual void SetMuscleDataUnsafe(int groupIndex, MuscleDataLR data)
        {
            //var array = MuscleGroupsControl;
            //array[groupIndex] = data;

            if (!IsInitialized) return;
            data.valuesLeft.flex = math.max(data.valuesLeft.flex, -0.15f);
            data.valuesRight.flex = math.max(data.valuesRight.flex, -0.15f);

            SetMuscleDataInternal(groupIndex, ref data);

            SyncMuscleMassData(groupIndex, data.valuesLeft.mass, data.valuesRight.mass);
            SyncMuscleFlexData(groupIndex, data.valuesLeft.flex, data.valuesRight.flex);

            //dirtyFlag_muscleGroupsControl = true;

            OnSetMuscleData(groupIndex, data);

            OnMuscleDataChanged?.Invoke(groupIndex);
            OnMuscleDataChangedNoArg?.Invoke();

            if (children != null)
            {
                foreach (var child in children) if (child.IsValid && child.type.HasFlag(ICustomizableCharacter.ChildType.Mesh))
                    {
                        child.instance.SetMuscleDataUnsafe(groupIndex, data);
                    }
            }
        }
        protected virtual void OnSetMuscleData(int groupIndex, MuscleDataLR data)
        {
        }
        public void SetMuscleData(int groupIndex, MuscleDataLR data)
        {
            if (!IsInitialized || groupIndex < 0 || groupIndex >= CustomizationData.MuscleVertexGroupCount) return;
            SetMuscleDataUnsafe(groupIndex, data);
        }
        public int IndexOfMuscleGroup(string groupName) => CustomizationData == null ? -1 : CustomizationData.IndexOfMuscleGroup(groupName);


        public virtual MuscleData GetMuscleDataForVertex(int vertexIndex) => default;


        public int FirstFatGroupsControlIndex => CharacterInstanceID * CustomizationData.FatVertexGroupCount;
        public virtual float GetFatLevelUnsafe(int groupIndex) => FatGroupsControlBuffer[FirstFatGroupsControlIndex + groupIndex].x;
        public float GetFatLevel(int groupIndex)
        {
            if (!IsInitialized || groupIndex < 0 || groupIndex >= CustomizationData.FatVertexGroupCount) return 0f;
            return GetFatLevelUnsafe(groupIndex);
        }
        public UnityEvent<int> OnFatDataChanged;
        public UnityEvent OnFatDataChangedNoArg;
        protected virtual void SetFatLevelInternal(int groupIndex, ref float level)
        {
            if (characterInstanceReference == null)
            {
                int controlIndex = FirstFatGroupsControlIndex + groupIndex;
                var val = FatGroupsControlBuffer[controlIndex];
                val.x = level;
                FatGroupsControlBuffer[controlIndex] = val;
            }
        }
        public virtual void SetFatLevelUnsafe(int groupIndex, float level)
        {
            if (!IsInitialized) return;

            SetFatLevelInternal(groupIndex, ref level);

            SyncFatLevel(groupIndex, level);

            OnSetFatLevel(groupIndex, level);

            OnFatDataChanged?.Invoke(groupIndex);
            OnFatDataChangedNoArg?.Invoke();

            if (children != null)
            {
                foreach (var child in children) if (child.IsValid && child.type.HasFlag(ICustomizableCharacter.ChildType.Mesh)) child.instance.SetFatLevelUnsafe(groupIndex, level);
            }
        }
        protected virtual void OnSetFatLevel(int groupIndex, float level)
        {
        }
        public void SetFatLevel(int groupIndex, float level)
        {
            if (!IsInitialized || groupIndex < 0 || groupIndex >= CustomizationData.FatVertexGroupCount) return;
            SetFatLevelUnsafe(groupIndex, level);
        }
        public virtual float2 GetBodyHairLevelUnsafe(int groupIndex) => FatGroupsControlBuffer[FirstFatGroupsControlIndex + groupIndex].zw;
        public float2 GetBodyHairLevel(int groupIndex)
        {
            if (!IsInitialized || groupIndex < 0 || groupIndex >= CustomizationData.FatVertexGroupCount) return 0f;
            return GetBodyHairLevelUnsafe(groupIndex);
        }
        protected virtual void SetBodyHairLevelInternal(int groupIndex, ref float level, ref float blend)
        {
            if (characterInstanceReference == null)
            {
                int controlIndex = FirstFatGroupsControlIndex + groupIndex;
                var val = FatGroupsControlBuffer[controlIndex];
                val.z = level;
                val.w = blend;
                FatGroupsControlBuffer[controlIndex] = val;
            }
        }
        public virtual void SetBodyHairLevelUnsafe(int groupIndex, float level, float blend = 1f)
        {
            if (!IsInitialized) return;

            SetBodyHairLevelInternal(groupIndex, ref level, ref blend);

            OnFatDataChanged?.Invoke(groupIndex);
            OnFatDataChangedNoArg?.Invoke();

            if (children != null)
            {
                foreach (var child in children) if (child.IsValid && child.type.HasFlag(ICustomizableCharacter.ChildType.Mesh)) child.instance.SetBodyHairLevelUnsafe(groupIndex, level, blend);
            }
        }
        public void SetBodyHairLevel(int groupIndex, float level, float blend = 1f)
        {
            if (!IsInitialized || groupIndex < 0 || groupIndex >= CustomizationData.FatVertexGroupCount) return;
            SetBodyHairLevelUnsafe(groupIndex, level, blend);
        }
        public int IndexOfFatGroup(string groupName) => CustomizationData == null ? -1 : CustomizationData.IndexOfFatGroup(groupName);


        public int VariationShapesControlDataSize => CustomizationData.VariationShapesControlDataSize;
        public int FirstVariationShapesControlIndex => CharacterInstanceID * VariationShapesControlDataSize;
        public int GetPartialVariationShapeIndex(int variationGroupIndex, int shapeIndex)
        {
            if (variationGroupIndex < 0 || variationGroupIndex >= CustomizationData.VariationVertexGroupCount || shapeIndex < 0 || shapeIndex >= CustomizationData.VariationShapesCount) return -1;
            return GetPartialVariationShapeIndexUnsafe(variationGroupIndex, shapeIndex);
        }
        public int GetPartialVariationShapeIndexUnsafe(int variationGroupIndex, int shapeIndex)
        {
            return (variationGroupIndex * CustomizationData.VariationShapesCount) + shapeIndex;
        }

        public virtual float2 GetVariationWeightUnsafe(int variationShapeIndex, int groupIndex) => VariationShapesControlBuffer[FirstVariationShapesControlIndex + (groupIndex * CustomizationData.VariationShapesCount) + variationShapeIndex];
        public float2 GetVariationWeight(int variationShapeIndex, int groupIndex)
        {
            if (!IsInitialized || groupIndex < 0 || groupIndex >= CustomizationData.VariationVertexGroupCount || variationShapeIndex < 0 || variationShapeIndex >= CustomizationData.VariationShapesCount) return 0;
            return GetVariationWeightUnsafe(variationShapeIndex, groupIndex);
        }

        public UnityEvent<int> OnVariationDataChanged;
        public UnityEvent OnVariationDataChangedNoArg;
        protected virtual void SetVariationWeightInternal(int variationShapeIndex, int groupIndex, ref float2 weight)
        {
            if (characterInstanceReference == null)
            {
                int variationIndex = GetPartialVariationShapeIndexUnsafe(groupIndex, variationShapeIndex);
                VariationShapesControlBuffer[FirstVariationShapesControlIndex + variationIndex] = weight;
            }
        }
        public virtual void SetVariationWeightUnsafe(int variationShapeIndex, int groupIndex, float2 weight)
        {
            //var array = VariationShapesControl;
            //array[(groupIndex * CharacterMeshData.VariationShapesCount) + variationShapeIndex] = weight;

            if (!IsInitialized) return;
            SetVariationWeightInternal(variationShapeIndex, groupIndex, ref weight);

            SyncVariationData(groupIndex, variationShapeIndex, weight.x, weight.y);

            //dirtyFlag_variationShapesControl = true;

            OnSetVariationWeight(variationShapeIndex, groupIndex, weight);

            OnVariationDataChanged?.Invoke(GetPartialVariationShapeIndexUnsafe(groupIndex, variationShapeIndex));
            OnVariationDataChangedNoArg?.Invoke();

            if (children != null)
            {
                foreach (var child in children) if (child.IsValid && child.type.HasFlag(ICustomizableCharacter.ChildType.Mesh)) child.instance.SetVariationWeightUnsafe(variationShapeIndex, groupIndex, weight);
            }
        }
        protected virtual void OnSetVariationWeight(int variationShapeIndex, int groupIndex, float2 weight)
        {
        }
        public void SetVariationWeight(int variationShapeIndex, int groupIndex, float2 weight)
        {
            if (!IsInitialized || groupIndex < 0 || groupIndex >= CustomizationData.VariationVertexGroupCount || variationShapeIndex < 0 || variationShapeIndex >= CustomizationData.VariationShapesCount) return;
            SetVariationWeightUnsafe(variationShapeIndex, groupIndex, weight);
        }

        public virtual float2 GetVariationWeightUnsafe(int indexInArray) => VariationShapesControlBuffer[FirstVariationShapesControlIndex + indexInArray]; //VariationShapesControl[indexInArray];
        public float2 GetVariationWeight(int indexInArray)
        {
            if (indexInArray < 0 || !IsInitialized || indexInArray >= VariationShapesControlDataSize) return 0;
            return GetVariationWeightUnsafe(indexInArray);
        }
        public void SetVariationWeightUnsafe(int indexInArray, float2 weight) => SetVariationWeightUnsafe(indexInArray % CustomizationData.VariationShapesCount, indexInArray / CustomizationData.VariationShapesCount, weight);
        public void SetVariationWeight(int indexInArray, float2 weight)
        {
            if (indexInArray < 0 || !IsInitialized || indexInArray >= VariationShapesControlDataSize) return;
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

            if (!animatablePropertiesController.IsInitialized) animatablePropertiesController.Initialize();

            if (dynamicAnimationProperties == null) dynamicAnimationProperties = new List<DynamicAnimationProperties.Property>(); ;

            string id = GetInstanceID().ToString();
            var meshData = CustomizationData;
            for (int a = 0; a < meshData.StandaloneShapesCount; a++)
            {
                var shape = meshData.GetStandaloneShapeInfo(a);
                if (shape.IsInvalid || !shape.animatable) continue;

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
          
            for (int a = 0; a < meshData.MuscleVertexGroupCount; a++)
            {
                var group = meshData.GetMuscleVertexGroupInfo(a);
                if (group.IsInvalid /*|| !group.flag*/) continue; // animatable permission is stored in vertex group flag field // TODO: uncomment group.flag check

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
            if (!HasValidInstance || characterInstanceReference != null) return;

            var meshData = CustomizationData;
            for (int a = 0; a < meshData.MuscleVertexGroupCount; a++)
            {
                var data = GetMuscleDataUnsafe(a);
                if (Mathf.Approximately(data.valuesLeft.flex, 0f) && Mathf.Approximately(data.valuesRight.flex, 0f)) continue;

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

                    var meshData = CustomizationData;
                    if (meshData.HasBonesArray)
                    {
                        var boneNames = meshData.BoneNames;
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

        public override Matrix4x4[] BindPose => CustomizationData.ManagedBindPose;

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

        public CustomizableCharacterMeshBase shapesInstanceReference;
        public ICustomizableCharacter ShapesInstanceReference
        {
            get => shapesInstanceReference;
            set
            {
                if (shapesInstanceReference != null)
                {
                    shapesInstanceReference.RemoveChild(this, ICustomizableCharacter.ChildType.Shapes);
                }

                if (value is CustomizableCharacterMeshBase mesh_)
                {
                    value.AddChild(this, ICustomizableCharacter.ChildType.Shapes);
                    shapesInstanceReference = mesh_;
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
                if (rigInstanceReference is ICustomizableCharacter mesh_)
                {
                    mesh_.RemoveChild(this, ICustomizableCharacter.ChildType.Rig);
                }

                rigInstanceReference = value;

                if (RigInstanceReferenceIsValid)
                {
                    if (rigInstanceReference is ICustomizableCharacter mesh__)
                    {
                        mesh__.AddChild(this, ICustomizableCharacter.ChildType.Rig);
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

        public CustomizableCharacterMeshBase characterInstanceReference;
        public ICustomizableCharacter CharacterInstanceReference
        {
            get => characterInstanceReference;
            set
            {
                if (characterInstanceReference != null) characterInstanceReference.RemoveChild(this, ICustomizableCharacter.ChildType.Mesh);

                if (value is CustomizableCharacterMeshBase mesh_)
                {
                    characterInstanceReference = mesh_;
                    characterInstanceReference.AddChild(this, ICustomizableCharacter.ChildType.Mesh);
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
                SetFloatOverride(CustomizationData.ShapesInstanceIDPropertyName, InstanceID);
            }
            else
            {
                SetFloatOverride(CustomizationData.ShapesInstanceIDPropertyName, id);
            }

        }
        public void SetRigInstanceID(int id)
        {
            if (id < 0)
            {
                SetFloatOverride(CustomizationData.RigInstanceIDPropertyName, InstanceID);
            }
            else
            {
                SetFloatOverride(CustomizationData.RigInstanceIDPropertyName, id);
            }
        }
        public void SetCharacterInstanceID(int id)
        {
            if (id < 0)
            {
                SetFloatOverride(CustomizationData.CharacterInstanceIDPropertyName, InstanceID);
            }
            else
            {
                SetFloatOverride(CustomizationData.CharacterInstanceIDPropertyName, id);
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

                characterInstanceReference.AddChild(this, ICustomizableCharacter.ChildType.Mesh);
            }
        }

        #endregion

        #region Buffers

        public virtual bool TryGetInstanceBuffer<T>(string matPropName, out InstanceBuffer<T> buffer) where T : unmanaged
        {
            buffer = null;
            return false;
        }

        public int CreateInstanceMaterialBuffer<T>(string propertyName, int elementsPerInstance, int bufferPoolSize, bool autoApplyToMaterials, out InstanceBuffer<T> buffer) where T : unmanaged => CreateInstanceMaterialBuffer(propertyName, null, elementsPerInstance, bufferPoolSize, autoApplyToMaterials, out buffer);
        public virtual int CreateInstanceMaterialBuffer<T>(string propertyName, ICollection<int> materialSlots, int elementsPerInstance, int bufferPoolSize, bool autoApplyToMaterials, out InstanceBuffer<T> buffer) where T : unmanaged
        {
            buffer = null;
            return -1;
        }

        protected virtual void InitBuffers()
        {
            standaloneShapeControlBuffer = StandaloneShapeControlBuffer;
            muscleGroupsControlBuffer = MuscleGroupsControlBuffer;
            fatGroupsControlBuffer = FatGroupsControlBuffer;
            variationShapesControlBuffer = VariationShapesControlBuffer;

            if (HasValidInstance)
            {
                var meshData = CustomizationData;

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
                        if (!meshData.HasFatGroupModifiers)
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

                        fatGroupsControlBuffer.TrySetWriteIndices(indexStart, meshData.FatVertexGroupCount);
                        fatGroupsControlBuffer.RequestUpload();
                    }
                    if (meshData.VariationVertexGroupCount > 0) SetVariationWeightUnsafe(0, 0);
                }
            }
        }

        public override InstanceBuffer<float4x4> SkinningMatricesBuffer
        {
            get
            {
                if (skinningMatricesBuffer == null || !skinningMatricesBuffer.IsValid())
                {
                    skinningMatricesBuffer = null;
                    if (!IsDestroyed)
                    {
                        if (rigInstanceReference != null)
                        {
                            skinningMatricesBuffer = rigInstanceReference.SkinningMatricesBuffer;
                        }
                        else
                        {
                            if (HasValidInstance)
                            {
                                string matricesProperty = CustomizationData.SkinningMatricesPropertyName;
                                if (!TryGetInstanceBuffer<float4x4>(matricesProperty, out skinningMatricesBuffer))
                                {
                                    CreateInstanceMaterialBuffer<float4x4>(matricesProperty, SkinningBoneCount, 3, false, out skinningMatricesBuffer);
                                }

                                if (skinningMatricesBuffer != null)
                                {
                                    if (children != null)
                                    {
                                        foreach (var child in children) if (child.IsValid && child.type.HasFlag(ICustomizableCharacter.ChildType.Rig) && child.instance is CustomizableCharacterMeshBase childV2 && child.instance.IsRendering()) childV2.BindSkinningMatricesBufferToMaterials();
                                    }
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
        public virtual void BindSkinningMatricesBufferToMaterials(IEnumerable<Material> materialInstances)
        {
            var matricesBuffer = SkinningMatricesBuffer;
            if (matricesBuffer != null)
            {
                var meshData = CustomizationData;

                int boneCount = SkinningBoneCount;

                if (!RigInstanceReferenceIsValid)
                {
                    int writeStartIndex = RigInstanceID * boneCount;
                    var rigSampler = RigSampler;
                    if (rigSampler != null && !rigSampler.IsWritingToBufferAt(matricesBuffer, writeStartIndex)) rigSampler.AddWritableInstanceBuffer(matricesBuffer, writeStartIndex);
                }

                if (materialInstances != null)
                {
                    foreach (var mat in materialInstances)
                    {
                        if (mat == null) continue;

#if UNITY_EDITOR
                        Debug.Log($"{name}: Binding {meshData.SkinningMatricesPropertyName} to {mat.name}");
#endif
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
        public virtual void UnbindSkinningMatricesBufferFromMaterials(IEnumerable<Material> materialInstances)
        {
            if (skinningMatricesBuffer != null && materialInstances != null)
            {
                var meshData = CustomizationData;

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
                if (standaloneShapeControlBuffer == null || !standaloneShapeControlBuffer.IsValid())
                {
                    standaloneShapeControlBuffer = null;
                    if (!IsDestroyed)
                    {                      
                        if (shapesInstanceReference != null)
                        {
                            standaloneShapeControlBuffer = shapesInstanceReference.StandaloneShapeControlBuffer;
                        }
                        else
                        {
                            if (HasValidInstance)
                            {
                                string matProperty = CustomizationData.StandaloneShapesControlPropertyName;
                                if (!TryGetInstanceBuffer<float>(matProperty, out standaloneShapeControlBuffer))
                                {
                                    CreateInstanceMaterialBuffer<float>(matProperty, CustomizationData.StandaloneShapesCount, 2, false, out standaloneShapeControlBuffer);
                                }

                                if (standaloneShapeControlBuffer != null)
                                {
                                    if (children != null)
                                    {
                                        foreach (var child in children) if (child.IsValid && child.type.HasFlag(ICustomizableCharacter.ChildType.Shapes) && child.instance is CustomizableCharacterMeshBase childV2) childV2.BindStandaloneShapesControlBufferToMaterials();
                                    }
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
        public void BindStandaloneShapesControlBufferToMaterials(IEnumerable<Material> materialInstances)
        {
            var shapeBuffer = StandaloneShapeControlBuffer;
            if (shapeBuffer != null && materialInstances != null)
            {
                var meshData = CustomizationData;
                foreach (var mat in materialInstances)
                {
                    if (mat == null) continue;

#if UNITY_EDITOR
                    Debug.Log($"{name}: Binding {meshData.StandaloneShapesControlPropertyName} to {mat.name}");
#endif
                    shapeBuffer.BindMaterialProperty(mat, meshData.StandaloneShapesControlPropertyName);
                }
            }
        }
        public void UnbindStandaloneShapesControlBufferFromMaterials()
        {
            UnbindStandaloneShapesControlBufferFromMaterials(MaterialInstances);
        }
        public void UnbindStandaloneShapesControlBufferFromMaterials(IEnumerable<Material> materialInstances)
        {
            if (standaloneShapeControlBuffer != null && materialInstances != null)
            {
                var meshData = CustomizationData;
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
                if (muscleGroupsControlBuffer == null || !muscleGroupsControlBuffer.IsValid())
                {
                    muscleGroupsControlBuffer = null;

                    if (!IsDestroyed)
                    {
                        if (characterInstanceReference != null)
                        {
                            muscleGroupsControlBuffer = characterInstanceReference.MuscleGroupsControlBuffer;
                        }
                        else
                        {
                            if (HasValidInstance)
                            {
                                string matProperty = CustomizationData.MuscleGroupsControlPropertyName;
                                if (!TryGetInstanceBuffer<MuscleDataLR>(matProperty, out muscleGroupsControlBuffer))
                                {
                                    CreateInstanceMaterialBuffer<MuscleDataLR>(matProperty, CustomizationData.MuscleVertexGroupCount, 2, false, out muscleGroupsControlBuffer);
                                }

                                if (muscleGroupsControlBuffer != null)
                                {
                                    if (children != null)
                                    {
                                        foreach (var child in children) if (child.IsValid && child.type.HasFlag(ICustomizableCharacter.ChildType.Mesh) && child.instance is CustomizableCharacterMeshBase childV2) childV2.BindMuscleGroupsControlBufferToMaterials();
                                    }
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
        public void BindMuscleGroupsControlBufferToMaterials(IEnumerable<Material> materialInstances)
        {
            var muscleBuffer = MuscleGroupsControlBuffer;
            if (muscleBuffer != null && materialInstances != null)
            {
                var meshData = CustomizationData;
                foreach (var mat in materialInstances)
                {
                    if (mat == null) continue;

#if UNITY_EDITOR
                    Debug.Log($"{name}: Binding {meshData.MuscleGroupsControlPropertyName} to {mat.name}");
#endif
                    muscleBuffer.BindMaterialProperty(mat, meshData.MuscleGroupsControlPropertyName);
                }
            }
        }
        public void UnbindMuscleGroupsControlBufferFromMaterials()
        {
            UnbindMuscleGroupsControlBufferFromMaterials(MaterialInstances);
        }
        public void UnbindMuscleGroupsControlBufferFromMaterials(IEnumerable<Material> materialInstances)
        {
            if (muscleGroupsControlBuffer != null && materialInstances != null)
            {
                var meshData = CustomizationData;
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
                if (fatGroupsControlBuffer == null || !fatGroupsControlBuffer.IsValid())
                {
                    fatGroupsControlBuffer = null;

                    if (!IsDestroyed)
                    {
                        if (characterInstanceReference != null)
                        {
                            fatGroupsControlBuffer = characterInstanceReference.FatGroupsControlBuffer;
                        }
                        else
                        {
                            if (HasValidInstance)
                            {
                                string matProperty = CustomizationData.FatGroupsControlPropertyName;
                                if (!TryGetInstanceBuffer<float4>(matProperty, out fatGroupsControlBuffer))
                                {
                                    CreateInstanceMaterialBuffer<float4>(matProperty, CustomizationData.FatVertexGroupCount, 2, false, out fatGroupsControlBuffer);
                                }

                                if (fatGroupsControlBuffer != null)
                                {
                                    if (children != null)
                                    {
                                        foreach (var child in children) if (child.IsValid && child.type.HasFlag(ICustomizableCharacter.ChildType.Mesh) && child.instance is CustomizableCharacterMeshBase childV2) childV2.BindFatGroupsControlBufferToMaterials();
                                    }
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
        public void BindFatGroupsControlBufferToMaterials(IEnumerable<Material> materialInstances)
        {
            var fatBuffer = FatGroupsControlBuffer;
            if (fatBuffer != null && materialInstances != null)
            {
                var meshData = CustomizationData;
                foreach (var mat in materialInstances)
                {
                    if (mat == null) continue;

#if UNITY_EDITOR
                    Debug.Log($"{name}: Binding {meshData.FatGroupsControlPropertyName} to {mat.name}");
#endif
                    fatBuffer.BindMaterialProperty(mat, meshData.FatGroupsControlPropertyName);
                }
            }
        }
        public void UnbindFatGroupsControlBufferFromMaterials()
        {
            UnbindFatGroupsControlBufferFromMaterials(MaterialInstances);
        }
        public void UnbindFatGroupsControlBufferFromMaterials(IEnumerable<Material> materialInstances)
        {
            if (fatGroupsControlBuffer != null && materialInstances != null)
            {
                var meshData = CustomizationData;
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
                if (variationShapesControlBuffer == null || !variationShapesControlBuffer.IsValid())
                {
                    variationShapesControlBuffer = null;

                    if (!IsDestroyed)
                    {
                        if (characterInstanceReference != null)
                        {
                            variationShapesControlBuffer = characterInstanceReference.VariationShapesControlBuffer;
                        }
                        else
                        {
                            if (HasValidInstance)
                            {
                                string matProperty = CustomizationData.VariationShapesControlPropertyName;
                                if (!TryGetInstanceBuffer<float2>(matProperty, out variationShapesControlBuffer))
                                {
                                    CreateInstanceMaterialBuffer<float2>(matProperty, CustomizationData.VariationShapesCount * CustomizationData.VariationVertexGroupCount, 2, false, out variationShapesControlBuffer);
                                }

                                if (variationShapesControlBuffer != null)
                                {
                                    if (children != null)
                                    {
                                        foreach (var child in children) if (child.IsValid && child.type.HasFlag(ICustomizableCharacter.ChildType.Mesh) && child.instance is CustomizableCharacterMeshBase childV2) childV2.BindVariationGroupsControlBufferToMaterials();
                                    }
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
        public void BindVariationGroupsControlBufferToMaterials(IEnumerable<Material> materialInstances)
        {
            var variationBuffer = VariationShapesControlBuffer;
            if (variationBuffer != null && materialInstances != null)
            {
                var meshData = CustomizationData;
                foreach (var mat in materialInstances)
                {
                    if (mat == null) continue;

#if UNITY_EDITOR
                    Debug.Log($"{name}: Binding {meshData.VariationShapesControlPropertyName} to {mat.name}");
#endif
                    variationBuffer.BindMaterialProperty(mat, meshData.VariationShapesControlPropertyName);
                }
            }
        }
        public void UnbindVariationGroupsControlBufferFromMaterials()
        {
            UnbindVariationGroupsControlBufferFromMaterials(MaterialInstances);
        }
        public void UnbindVariationGroupsControlBufferFromMaterials(IEnumerable<Material> materialInstances)
        {
            if (variationShapesControlBuffer != null && materialInstances != null)
            {
                var meshData = CustomizationData;
                foreach (var mat in materialInstances)
                {
                    if (mat == null) continue;

                    variationShapesControlBuffer.UnbindMaterialProperty(mat, meshData.VariationShapesControlPropertyName);
                }
            }
        }

        #endregion

        #region Events

        public virtual void AddListener(ICustomizableCharacter.ListenableEvent event_, UnityAction<int> listener)
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
                case ICustomizableCharacter.ListenableEvent.OnVariationDataChanged:
                    if (OnVariationDataChanged == null) OnVariationDataChanged = new UnityEvent<int>();
                    OnVariationDataChanged.AddListener(listener);
                    break;

                case ICustomizableCharacter.ListenableEvent.OnAnyDataChanged:
                    AddListener(ICustomizableCharacter.ListenableEvent.OnMuscleDataChanged, listener);
                    AddListener(ICustomizableCharacter.ListenableEvent.OnFatDataChanged, listener);
                    AddListener(ICustomizableCharacter.ListenableEvent.OnVariationDataChanged, listener);
                    break;
            }
        }
        public virtual void RemoveListener(ICustomizableCharacter.ListenableEvent event_, UnityAction<int> listener)
        {
            switch (event_)
            {
                case ICustomizableCharacter.ListenableEvent.OnMuscleDataChanged:
                    if (OnMuscleDataChanged != null) OnMuscleDataChanged.RemoveListener(listener);
                    break;
                case ICustomizableCharacter.ListenableEvent.OnFatDataChanged:
                    if (OnFatDataChanged != null) OnFatDataChanged.RemoveListener(listener);
                    break;
                case ICustomizableCharacter.ListenableEvent.OnVariationDataChanged:
                    if (OnVariationDataChanged != null) OnVariationDataChanged.RemoveListener(listener);
                    break;

                case ICustomizableCharacter.ListenableEvent.OnAnyDataChanged:
                    RemoveListener(ICustomizableCharacter.ListenableEvent.OnMuscleDataChanged, listener);
                    RemoveListener(ICustomizableCharacter.ListenableEvent.OnFatDataChanged, listener);
                    RemoveListener(ICustomizableCharacter.ListenableEvent.OnVariationDataChanged, listener);
                    break;
            }
        }

        public virtual void AddListener(ICustomizableCharacter.ListenableEvent event_, UnityAction listener)
        {
            switch (event_)
            {
                case ICustomizableCharacter.ListenableEvent.OnMuscleDataChanged:
                    if (OnMuscleDataChangedNoArg == null) OnMuscleDataChangedNoArg = new UnityEvent();
                    OnMuscleDataChangedNoArg.AddListener(listener);
                    break;
                case ICustomizableCharacter.ListenableEvent.OnFatDataChanged:
                    if (OnFatDataChangedNoArg == null) OnFatDataChangedNoArg = new UnityEvent();
                    OnFatDataChangedNoArg.AddListener(listener);
                    break;
                case ICustomizableCharacter.ListenableEvent.OnVariationDataChanged:
                    if (OnVariationDataChangedNoArg == null) OnVariationDataChangedNoArg = new UnityEvent();
                    OnVariationDataChangedNoArg.AddListener(listener);
                    break;

                case ICustomizableCharacter.ListenableEvent.OnAnyDataChanged:
                    AddListener(ICustomizableCharacter.ListenableEvent.OnMuscleDataChanged, listener);
                    AddListener(ICustomizableCharacter.ListenableEvent.OnFatDataChanged, listener);
                    AddListener(ICustomizableCharacter.ListenableEvent.OnVariationDataChanged, listener); 
                    break;
            }
        }
        public virtual void RemoveListener(ICustomizableCharacter.ListenableEvent event_, UnityAction listener)
        {
            switch (event_)
            {
                case ICustomizableCharacter.ListenableEvent.OnMuscleDataChanged:
                    if (OnMuscleDataChangedNoArg != null) OnMuscleDataChangedNoArg.RemoveListener(listener);
                    break;
                case ICustomizableCharacter.ListenableEvent.OnFatDataChanged:
                    if (OnFatDataChangedNoArg != null) OnFatDataChangedNoArg.RemoveListener(listener);
                    break;
                case ICustomizableCharacter.ListenableEvent.OnVariationDataChanged:
                    if (OnVariationDataChangedNoArg != null) OnVariationDataChangedNoArg.RemoveListener(listener);
                    break;

                case ICustomizableCharacter.ListenableEvent.OnAnyDataChanged:
                    RemoveListener(ICustomizableCharacter.ListenableEvent.OnMuscleDataChanged, listener);
                    RemoveListener(ICustomizableCharacter.ListenableEvent.OnFatDataChanged, listener);
                    RemoveListener(ICustomizableCharacter.ListenableEvent.OnVariationDataChanged, listener);
                    break;
            }
        }

        public virtual void ClearListeners()
        {
            if (OnMuscleDataChanged != null) OnMuscleDataChanged.RemoveAllListeners();
            if (OnMuscleDataChangedNoArg != null) OnMuscleDataChangedNoArg.RemoveAllListeners();

            if (OnFatDataChanged != null) OnFatDataChanged.RemoveAllListeners();
            if (OnFatDataChangedNoArg != null) OnFatDataChangedNoArg.RemoveAllListeners();

            if (OnVariationDataChanged != null) OnVariationDataChanged.RemoveAllListeners();
            if (OnVariationDataChangedNoArg != null) OnVariationDataChangedNoArg.RemoveAllListeners();         
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
            get => CustomizationData.RaycastLOD;
            set
            {
            }
        }

        protected abstract bool PreRaycastAgainst(ref int lod, ref float3 origin, ref float3 offset);
        protected abstract bool RaycastAgainstMesh(int lod, float3 origin, float3 offset, out Maths.RaycastHitResult result, float errorMargin = 0.01f);
        public bool RaycastAgainst(int lod, float3 origin, float3 offset, out Maths.RaycastHitResult result, float errorMargin = 0.01f)
        {
            result = default;
            if (!IsInitialized) return false;

            if (!PreRaycastAgainst(ref lod, ref origin, ref offset)) return false;

            return RaycastAgainstMesh(lod, origin, offset, out result, errorMargin);
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
        protected struct ClosestRaycastHitFinalJob : IJob
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

        public string GetMuscleGroupName(int index) => GetMuscleGroupNameUnsafe(index);
        public string GetMuscleGroupNameUnsafe(int index)
        {
            var defaultGroup = CustomizationData.ConvertMuscleGroupIndexToDefault(index);
            return defaultGroup.ToString();
        }
        public int GetMuscleGroupIndex(string muscleGroupName)
        {
            if (Enum.TryParse(muscleGroupName, true, out MuscleGroupsDefault defaultGroup)) return CustomizationData.ConvertDefaultMuscleGroupToIndex(defaultGroup);

            var baseGroup = MuscleGroupsDefaultExtensions.GetMuscleGroupBase(muscleGroupName);
            if (baseGroup != MuscleGroup.Null)
            {
                if (Enum.TryParse(baseGroup.ToString(), true, out defaultGroup)) return CustomizationData.ConvertDefaultMuscleGroupToIndex(defaultGroup);
            }

            return CustomizationData.IndexOfMuscleGroup(muscleGroupName) * 2;
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

        public int MuscleGroupCount => CustomizationData.MuscleGroupsCount * 2;

        public float BreastPresence
        {
            get => BustSize;
            set => BustSize = value;
        }

        public bool SetMuscleGroupValues(int muscleGroupIndex, float3 values, bool updateDependencies = true)
        {
            int localGroupIndex = ConvertDefaultMuscleGroupIndexToLocal(muscleGroupIndex, out bool bothSides);

            var defaultGroup = CustomizationData.ConvertLocalMuscleGroupToDefault(localGroupIndex);
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

            var defaultGroup = CustomizationData.ConvertLocalMuscleGroupToDefault(localGroupIndex);
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

            var defaultGroup = CustomizationData.ConvertLocalMuscleGroupToDefault(localGroupIndex);
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

            var defaultGroup = CustomizationData.ConvertLocalMuscleGroupToDefault(localGroupIndex);
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

            var defaultGroup = CustomizationData.ConvertLocalMuscleGroupToDefault(localGroupIndex);
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
            for (int a = 0; a < MuscleGroupCount; a++)
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

        public override void ClearAllListeners()
        {
            base.ClearAllListeners();

            ClearEventListeners();
        }
        public void ClearEventListeners()
        {
            ClearListeners();
        }

        protected List<MuscleValueListener>[] muscleValueListeners;

        public bool Listen(int muscleGroupIndex, EngineInternal.IEngineObject listeningObject, MuscleValueListenerDelegate callback, out MuscleValueListener listener)
        {
            muscleGroupIndex = ConvertDefaultMuscleGroupIndexToLocal(muscleGroupIndex, out int defaultIndex, out bool bothSides);

            listener = null;
            if (listeningObject == null || callback == null || muscleGroupIndex < 0 || muscleGroupIndex >= CustomizationData.MuscleGroupsCount) return false;

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

        protected virtual void NotifyDefaultMuscleGroupListeners(int muscleGroupIndex)
        {
            muscleGroupIndex = ConvertDefaultMuscleGroupIndexToLocal(muscleGroupIndex, out int defaultIndex);

            if (muscleValueListeners != null && muscleGroupIndex >= 0 && muscleGroupIndex < CustomizationData.MuscleGroupsCount)
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

#if UNITY_EDITOR

    public class CustomizableCharacterMeshRefreshDetector : UnityEditor.AssetPostprocessor
    {
        // This method is automatically called by Unity after any Asset Database refresh completes
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            // Check if the Unity Editor is currently running the game
            if (UnityEditor.EditorApplication.isPlaying)
            {
                Debug.LogWarning($"[{nameof(CustomizableCharacterMeshRefreshDetector)}] ⚡ Database Refresh Detected in Play Mode");

                var allMeshes = GameObject.FindObjectsByType<CustomizableCharacterMeshBase>(FindObjectsSortMode.None);
                foreach (var mesh in allMeshes)
                {
                    if (mesh != null)
                    {
                        Debug.Log($"[{nameof(CustomizableCharacterMeshRefreshDetector)}] Reinitializing {mesh.name} due to database refresh");
                        mesh.enabled = false;
                    }
                }

                IEnumerator Reenable()
                {
                    yield return null;
                    yield return null;

                    foreach (var mesh in allMeshes)
                    {
                        if (mesh != null)
                        {
                            mesh.enabled = true;
                        }
                    }
                }

                CoroutineProxy.Start(Reenable()); 
            }
        }
    }

#endif

}

#endif