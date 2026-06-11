#if UNITY_2017_1_OR_NEWER

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Unity.Mathematics;
using Unity.Collections;
using Swole.DataStructures;

using static Swole.API.Unity.ICustomizableCharacter.Defaults;

namespace Swole.Morphing
{
    [NonAnimatable]
    public class CustomizableCharacterUnitySkinnedMesh_DATA : ScriptableObject, CustomizableCharacterMeshBase.ICustomizableCharacterMeshBaseData
    {

        public static CustomizableCharacterUnitySkinnedMesh_DATA CreateInstance(string name, CustomizableCharacterUnitySkinnedMesh.SerializedData data)
        {
            var instance = ScriptableObject.CreateInstance<CustomizableCharacterUnitySkinnedMesh_DATA>(); 
            instance.name = name;
            instance.serializedData = data; 

            return instance;
        }

        [SerializeField]
        protected CustomizableCharacterUnitySkinnedMesh.SerializedData serializedData;
        public CustomizableCharacterUnitySkinnedMesh.SerializedData SerializedData => serializedData;

#if UNITY_EDITOR
        public void ReplaceData(CustomizableCharacterMeshBase.ICustomizableCharacterMeshBaseData data)
        {
            Debug.Log($"Replacing serializedData for {name} : {data != null}");
            if (data is CustomizableCharacterUnitySkinnedMesh.SerializedData data_) serializedData = data_;
        }
#endif

        public bool Precache()
        {
            if (serializedData != null)
            {
                if (serializedData.Precache())
                {
#if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(this);
                    UnityEditor.AssetDatabase.SaveAssetIfDirty(this);
#endif
                    return true;
                }
            }

            return false;
        }

        public string Name => name;

        public bool IsPrecached => Data.IsPrecached;
        public bool TryPrecache() => Data.TryPrecache();

        // Forwarding property to the underlying serializedData which implements the interface.
        private CustomizableCharacterMeshBase.ICustomizableCharacterMeshBaseData Data => serializedData as CustomizableCharacterMeshBase.ICustomizableCharacterMeshBaseData;

        public void Dispose() => Data.Dispose();

        public bool HasMaterials => Data.HasMaterials;
        public int MaterialCount => Data.MaterialCount;
        public Material GetMaterial(int index) => Data.GetMaterial(index);

        public bool HasRenderSets => Data.HasRenderSets;
        public int RenderSetCount => Data.RenderSetCount;
        public CustomizableCharacterMeshBase.RenderSet GetRenderSet(int index) => Data.GetRenderSet(index);
        public CustomizableCharacterMeshBase.RenderSet[] RenderSets => Data.RenderSets;

        public int VertexCount => Data.VertexCount;

        public Mesh Mesh => Data.Mesh;
        public int LevelsOfDetail => Data.LevelsOfDetail;
        public Mesh GetMesh(int lod) => Data.GetMesh(lod);
        public Mesh GetMeshUnsafe(int lod) => Data.GetMeshUnsafe(lod);
        public MeshLOD GetLOD(int lod) => Data.GetLOD(lod);
        public MeshLOD GetLODUnsafe(int lod) => Data.GetLODUnsafe(lod);

        public bool TryGetVertices(int lod, out NativeArray<float3> array) => Data.TryGetVertices(lod, out array);
        public bool TryGetNormals(int lod, out NativeArray<float3> array) => Data.TryGetNormals(lod, out array);
        public bool TryGetTangents(int lod, out NativeArray<float4> array) => Data.TryGetTangents(lod, out array);
        public bool TryGetColors(int lod, out NativeArray<float4> array) => Data.TryGetColors(lod, out array);
        public bool TryGetTriangles(int lod, out NativeArray<int> array) => Data.TryGetTriangles(lod, out array);

        public bool TryGetBoneWeights(int lod, out NativeArray<BoneWeight8> array) => Data.TryGetBoneWeights(lod, out array);

        public bool TryGetUV0(int lod, out NativeArray<float4> array) => Data.TryGetUV0(lod, out array);
        public bool TryGetUV1(int lod, out NativeArray<float4> array) => Data.TryGetUV1(lod, out array);
        public bool TryGetUV2(int lod, out NativeArray<float4> array) => Data.TryGetUV2(lod, out array);
        public bool TryGetUV3(int lod, out NativeArray<float4> array) => Data.TryGetUV3(lod, out array);
        public bool TryGetUV(int lod, UVChannelURP channel, out NativeArray<float4> array) => Data.TryGetUV(lod, channel, out array);

        public Vector3 BoundsCenter => Data.BoundsCenter;
        public Vector3 BoundsExtents => Data.BoundsExtents;

        public bool HasLeftRightFlags => Data.HasLeftRightFlags;
        public bool GetLeftRightFlag(int vertexIndex) => Data.GetLeftRightFlag(vertexIndex);
        public bool[] LeftRightFlags => Data.LeftRightFlags;

        public bool HasBaseBoneWeights => Data.HasBaseBoneWeights;
        public BoneWeight8 GetBaseBoneWeight(int vertexIndex) => Data.GetBaseBoneWeight(vertexIndex);
        public BoneWeight8[] BaseBoneWeights => Data.BaseBoneWeights;

        public NativeArray<BoneWeight8> BaseBoneWeightsJob => Data.BaseBoneWeightsJob;

        public bool HasBonesArray => Data.HasBonesArray;
        public string[] BoneNames => Data.BoneNames;
        public bool HasManagedBindPose => Data.HasManagedBindPose;
        public Matrix4x4[] ManagedBindPose => Data.ManagedBindPose;

        public int BoneCount => Data.BoneCount;

        public Vector2Int StandaloneShapes => Data.StandaloneShapes;
        public Vector2Int VariationShapes => Data.VariationShapes;

        public int MassShape => Data.MassShape;
        public int FlexShape => Data.FlexShape;
        public int FatShape => Data.FatShape;

        public int FatMuscleBlendShape => Data.FatMuscleBlendShape;
        public Vector2 FatMuscleBlendWeightRange => Data.FatMuscleBlendWeightRange;

        public int BustSizeShape => Data.BustSizeShape;
        public int BustShapeShape => Data.BustShapeShape;
        public int BustSizeMuscleShape => Data.BustSizeMuscleShape;

        public int MidlineVertexGroup => Data.MidlineVertexGroup;
        public int BustVertexGroup => Data.BustVertexGroup;
        public int BustNerfVertexGroup => Data.BustNerfVertexGroup;

        public int NippleMaskVertexGroup => Data.NippleMaskVertexGroup;
        public int GenitalMaskVertexGroup => Data.GenitalMaskVertexGroup;

        public float DefaultMassShapeWeight => Data.DefaultMassShapeWeight;
        public float MinMassShapeWeight => Data.MinMassShapeWeight;

        public Vector2Int StandaloneGroups => Data.StandaloneGroups;
        public Vector2Int VariationGroups => Data.VariationGroups;
        public Vector2Int MuscleGroups => Data.MuscleGroups;
        public Vector2Int FatGroups => Data.FatGroups;

        public string VertexCountPropertyName => Data.VertexCountPropertyName;
        public string SkinningDataPropertyName => Data.SkinningDataPropertyName;
        public string BoneCountPropertyName => Data.BoneCountPropertyName;
        public string SkinningMatricesPropertyName => Data.SkinningMatricesPropertyName;
        public string MidlineVertexGroupIndexPropertyName => Data.MidlineVertexGroupIndexPropertyName;
        public string BustMixPropertyName => Data.BustMixPropertyName;
        public string HideNipplesPropertyName => Data.HideNipplesPropertyName;
        public string HideGenitalsPropertyName => Data.HideGenitalsPropertyName;
        public string BustVertexGroupIndexPropertyName => Data.BustVertexGroupIndexPropertyName;
        public string BustNerfVertexGroupIndexPropertyName => Data.BustNerfVertexGroupIndexPropertyName;
        public string NippleMaskVertexGroupIndexPropertyName => Data.NippleMaskVertexGroupIndexPropertyName;
        public string GenitalMaskVertexGroupIndexPropertyName => Data.GenitalMaskVertexGroupIndexPropertyName;

        public string BustSizeShapeIndexPropertyName => Data.BustSizeShapeIndexPropertyName;
        public string BustSizeMuscularShapeIndexPropertyName => Data.BustSizeMuscularShapeIndexPropertyName;

        public string FatMuscleBlendShapeIndexPropertyName => Data.FatMuscleBlendShapeIndexPropertyName;
        public string FatMuscleBlendWeightRangePropertyName => Data.FatMuscleBlendWeightRangePropertyName;
        public string DefaultShapeMuscleWeightPropertyName => Data.DefaultShapeMuscleWeightPropertyName;
        public string StandaloneShapesControlPropertyName => Data.StandaloneShapesControlPropertyName;
        public string MuscleGroupsControlPropertyName => Data.MuscleGroupsControlPropertyName;
        public string FatGroupsControlPropertyName => Data.FatGroupsControlPropertyName;
        public string VariationShapesControlPropertyName => Data.VariationShapesControlPropertyName;

        public string MuscleMassShapeIndexPropertyName => Data.MuscleMassShapeIndexPropertyName;
        public string FlexShapeIndexPropertyName => Data.FlexShapeIndexPropertyName;
        public string FatShapeIndexPropertyName => Data.FatShapeIndexPropertyName;

        public string VertexGroupsPropertyName => Data.VertexGroupsPropertyName;
        public string MeshShapeFrameDeltasPropertyName => Data.MeshShapeFrameDeltasPropertyName;
        public string MeshShapeFrameWeightsPropertyName => Data.MeshShapeFrameWeightsPropertyName;
        public string MeshShapeIndicesPropertyName => Data.MeshShapeIndicesPropertyName;

        public string MuscleGroupInfluencesPropertyName => Data.MuscleGroupInfluencesPropertyName;
        public string FatGroupInfluencesPropertyName => Data.FatGroupInfluencesPropertyName;
        public string PerVertexDeltaDataPropertyName => Data.PerVertexDeltaDataPropertyName;

        public string LocalInstanceIDPropertyName => Data.LocalInstanceIDPropertyName;
        public string ShapesInstanceIDPropertyName => Data.ShapesInstanceIDPropertyName;
        public string RigInstanceIDPropertyName => Data.RigInstanceIDPropertyName;
        public string CharacterInstanceIDPropertyName => Data.CharacterInstanceIDPropertyName;

        public string VertexColorDeltasPropertyName => Data.VertexColorDeltasPropertyName;

        public string MinMassShapeWeightPropertyName => Data.MinMassShapeWeightPropertyName;

        public string FlexEndPointWeightPropertyName => Data.FlexEndPointWeightPropertyName;
        public string FlexExponentPropertyName => Data.FlexExponentPropertyName;
        public string FlexNerfThresholdPropertyName => Data.FlexNerfThresholdPropertyName;
        public string FlexNerfExponentPropertyName => Data.FlexNerfExponentPropertyName;

        public float FlexEndPointWeight => Data.FlexEndPointWeight;
        public float FlexExponent => Data.FlexExponent;
        public float FlexNerfThreshold => Data.FlexNerfThreshold;
        public float FlexNerfExponent => Data.FlexNerfExponent;
        public int RaycastLOD => Data.RaycastLOD;

        public UVChannelURP NearestVertexUVChannel => Data.NearestVertexUVChannel;
        public RGBAChannel NearestVertexIndexElement => Data.NearestVertexIndexElement;

        public string ConvertDefaultMuscleGroupName(MuscleGroupsDefault defaultGroup) => Data.ConvertDefaultMuscleGroupName(defaultGroup);
        public int ConvertDefaultMuscleGroupToIndex(MuscleGroupsDefault defaultGroup) => Data.ConvertDefaultMuscleGroupToIndex(defaultGroup);
        public MuscleGroupsDefault ConvertLocalMuscleGroupToDefault(string muscleGroupName) => Data.ConvertLocalMuscleGroupToDefault(muscleGroupName);
        public MuscleGroupsDefault ConvertLocalMuscleGroupToDefault(int muscleGroupIndex) => Data.ConvertLocalMuscleGroupToDefault(muscleGroupIndex);
        public MuscleGroupsDefault ConvertMuscleGroupIndexToDefault(int muscleGroupIndex) => Data.ConvertMuscleGroupIndexToDefault(muscleGroupIndex);

        public List<BoneWeight8Float> GetConvertedBoneWeightData(List<BoneWeight8Float> outputList = null) => Data.GetConvertedBoneWeightData(outputList);

        public ComputeBuffer BoneWeightsBuffer => Data.BoneWeightsBuffer;
        public ComputeBuffer MuscleGroupInfluencesBuffer => Data.MuscleGroupInfluencesBuffer;
        public ComputeBuffer FatGroupInfluencesBuffer => Data.FatGroupInfluencesBuffer;

        public int MeshShapeCount => Data.MeshShapeCount;
        public int MeshShapeDeltasCount => Data.MeshShapeDeltasCount;
        public ShapeInfo GetShapeInfo(int index) => Data.GetShapeInfo(index);
        public ShapeInfo GetShapeInfoUnsafe(int index) => Data.GetShapeInfoUnsafe(index);
        public int IndexOfShape(string shapeName, bool caseSensitive = false) => Data.IndexOfShape(shapeName, caseSensitive);
        public List<ShapeInfo> GetShapeInfos(List<ShapeInfo> outputList = null) => Data.GetShapeInfos(outputList);

        public string VertexGroupsBufferRangePropertyName => Data.VertexGroupsBufferRangePropertyName;
        public int VertexGroupCount => Data.VertexGroupCount;
        public VertexGroupInfo GetVertexGroupInfo(int index) => Data.GetVertexGroupInfo(index);
        public VertexGroupInfo GetVertexGroupInfoUnsafe(int index) => Data.GetVertexGroupInfoUnsafe(index);
        public int IndexOfVertexGroup(string vertexGroupName, bool caseSensitive = false) => Data.IndexOfVertexGroup(vertexGroupName, caseSensitive);
        public List<VertexGroupInfo> GetVertexGroupInfos(List<VertexGroupInfo> outputList = null) => Data.GetVertexGroupInfos(outputList);

        public string StandaloneVertexGroupsBufferRangePropertyName => Data.StandaloneVertexGroupsBufferRangePropertyName;
        public int StandaloneGroupsCount => Data.StandaloneGroupsCount;
        public int StandaloneVertexGroupCount => Data.StandaloneVertexGroupCount;
        public int IndexOfStandaloneVertexGroup(string name, bool caseSensitive = false) => Data.IndexOfStandaloneVertexGroup(name, caseSensitive);
        public VertexGroupInfo GetStandaloneVertexGroupInfo(int index) => Data.GetStandaloneVertexGroupInfo(index);

        public string MuscleVertexGroupsBufferRangePropertyName => Data.MuscleVertexGroupsBufferRangePropertyName;
        public int MuscleGroupsCount => Data.MuscleGroupsCount;
        public int MuscleVertexGroupCount => Data.MuscleVertexGroupCount;
        public int IndexOfMuscleGroup(string name, bool caseSensitive = false) => Data.IndexOfMuscleGroup(name, caseSensitive);
        public VertexGroupInfo GetMuscleVertexGroupInfo(int index) => Data.GetMuscleVertexGroupInfo(index);

        public string FatVertexGroupsBufferRangePropertyName => Data.FatVertexGroupsBufferRangePropertyName;
        public int FatGroupsCount => Data.FatGroupsCount;
        public int FatVertexGroupCount => Data.FatVertexGroupCount;
        public int IndexOfFatGroup(string name, bool caseSensitive = false) => Data.IndexOfFatGroup(name, caseSensitive);
        public VertexGroupInfo GetFatVertexGroupInfo(int index) => Data.GetFatVertexGroupInfo(index);
        public float2 GetFatGroupModifier(int index) => Data.GetFatGroupModifier(index);
        public bool HasFatGroupModifiers => Data.HasFatGroupModifiers;

        public string VariationVertexGroupsBufferRangePropertyName => Data.VariationVertexGroupsBufferRangePropertyName;
        public int VariationGroupsCount => Data.VariationGroupsCount;
        public int VariationVertexGroupCount => Data.VariationVertexGroupCount;
        public int IndexOfVariationGroup(string name, bool caseSensitive = false) => Data.IndexOfVariationGroup(name, caseSensitive);
        public VertexGroupInfo GetVariationGroupInfo(int index) => Data.GetVariationGroupInfo(index);

        public int VariationShapesControlDataSize => Data.VariationShapesControlDataSize;

        public string StandaloneShapesBufferRangePropertyName => Data.StandaloneShapesBufferRangePropertyName;
        public int StandaloneShapesCount => Data.StandaloneShapesCount;
        public int IndexOfStandaloneShape(string name, bool caseSensitive = false) => Data.IndexOfStandaloneShape(name, caseSensitive);
        public ShapeInfo GetStandaloneShapeInfo(int index) => Data.GetStandaloneShapeInfo(index);

        public ShapeInfo MassShapeInfo => Data.MassShapeInfo;
        public int MassShapeFrameCount => Data.MassShapeFrameCount;

        public ShapeInfo FlexShapeInfo => Data.FlexShapeInfo;
        public int FlexShapeFrameCount => Data.FlexShapeFrameCount;

        public ShapeInfo FatShapeInfo => Data.FatShapeInfo;
        public int FatShapeFrameCount => Data.FatShapeFrameCount;

        public ShapeInfo FatMuscleBlendShapeInfo => Data.FatMuscleBlendShapeInfo;
        public int FatMuscleBlendShapeFrameCount => Data.FatMuscleBlendShapeFrameCount;

        public ShapeInfo BustSizeShapeInfo => Data.BustSizeShapeInfo;
        public int BustSizeShapeFrameCount => Data.BustSizeShapeFrameCount;

        public ShapeInfo BustSizeMuscleShapeInfo => Data.BustSizeMuscleShapeInfo;
        public int BustSizeMuscleShapeFrameCount => Data.BustSizeMuscleShapeFrameCount;

        public string VariationShapesBufferRangePropertyName => Data.VariationShapesBufferRangePropertyName;
        public int VariationShapesCount => Data.VariationShapesCount;
        public int IndexOfVariationShape(string name, bool caseSensitive = false) => Data.IndexOfVariationShape(name, caseSensitive);
        public ShapeInfo GetVariationShapeInfo(int index) => Data.GetVariationShapeInfo(index);

        public ComputeBuffer VertexGroupsBuffer => Data.VertexGroupsBuffer;
        public ComputeBuffer MeshShapeFrameDeltasBuffer => Data.MeshShapeFrameDeltasBuffer;
        public ComputeBuffer MeshShapeFrameWeightsBuffer => Data.MeshShapeFrameWeightsBuffer;
        public ComputeBuffer MeshShapeIndicesBuffer => Data.MeshShapeIndicesBuffer;
        public ComputeBuffer VertexColorDeltasBuffer => Data.VertexColorDeltasBuffer;

    }
}

#endif