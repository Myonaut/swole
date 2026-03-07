#if (UNITY_STANDALONE || UNITY_EDITOR)

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
    public class CustomizableCharacterMeshV2_DATA : ScriptableObject
    {

        public static CustomizableCharacterMeshV2_DATA CreateInstance(string name, CustomizableCharacterMeshV2.SerializedData data)
        {
            var instance = ScriptableObject.CreateInstance<CustomizableCharacterMeshV2_DATA>();
            instance.name = name;
            instance.serializedData = data;

            return instance;
        }

        [SerializeField]
        protected CustomizableCharacterMeshV2.SerializedData serializedData;
        public CustomizableCharacterMeshV2.SerializedData SerializedData => serializedData;

#if UNITY_EDITOR
        public void ReplaceData(CustomizableCharacterMeshV2.SerializedData data)
        {
            serializedData = data;
        }
#endif

        public void Precache()
        {
            if (serializedData != null) serializedData.Precache();
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssetIfDirty(this);
#endif
        }

        public int MeshShapeCount => serializedData.MeshShapeCount;
        public MeshShape GetShape(int index) => serializedData.GetShape(index);
        public MeshShape GetShapeUnsafe(int index) => serializedData.GetShapeUnsafe(index);
        public int IndexOfShape(string shapeName, bool caseSensitive = false) => serializedData.IndexOfShape(shapeName, caseSensitive);
        public List<MeshShape> GetShapes(List<MeshShape> outputList = null) => serializedData.GetShapes(outputList);
        
        public int VertexGroupCount => serializedData.VertexGroupCount;
        public VertexGroup GetVertexGroup(int index) => serializedData.GetVertexGroup(index);
        public VertexGroup GetVertexGroupUnsafe(int index) => serializedData.GetVertexGroupUnsafe(index);
        public int IndexOfVertexGroup(string vertexGroupName, bool caseSensitive = false) => serializedData.IndexOfVertexGroup(vertexGroupName, caseSensitive);
        public List<VertexGroup> GetVertexGroups(List<VertexGroup> outputList = null) => serializedData.GetVertexGroups(outputList);


        public string StandaloneShapesBufferRangePropertyName => serializedData.StandaloneShapesBufferRangePropertyName;
        public int StandaloneShapesCount => serializedData.StandaloneGroupsCount;
        public int IndexOfStandaloneShape(string name, bool caseSensitive = false) => serializedData.IndexOfStandaloneShape(name, caseSensitive);
        public MeshShape GetStandaloneShape(int index) => serializedData.GetStandaloneShape(index);

        public MeshShape MassShape => serializedData.MassShape;
        public int MassShapeFrameCount => serializedData.MassShapeFrameCount;

        public MeshShape FlexShape => serializedData.FlexShape;
        public int FlexShapeFrameCount => serializedData.FlexShapeFrameCount;

        public MeshShape FatShape => serializedData.FatShape;
        public int FatShapeFrameCount => serializedData.FatShapeFrameCount;

        public MeshShape FatMuscleBlendShape => serializedData.FatMuscleBlendShape;
        public int FatMuscleBlendShapeFrameCount => serializedData.FatMuscleBlendShapeFrameCount;

        public MeshShape BustSizeShape => serializedData.BustSizeShape;
        public int BustSizeShapeFrameCount => serializedData.BustSizeShapeFrameCount;

        public MeshShape BustSizeMuscleShape => serializedData.BustSizeMuscleShape;
        public int BustSizeMuscleShapeFrameCount => serializedData.BustSizeMuscleShapeFrameCount;


        public string VariationShapesBufferRangePropertyName => serializedData.VariationShapesBufferRangePropertyName;
        public int VariationShapesCount => serializedData.VariationShapesCount;
        public int IndexOfVariationShape(string name) => serializedData.IndexOfVariationShape(name);
        public MeshShape GetVariationShape(int index) => serializedData.GetVariationShape(index);



        public int VertexColorDeltaCount => serializedData.VertexColorDeltaCount;
        public VertexColorDelta GetVertexColorDelta(int index) => serializedData.GetVertexColorDelta(index);
        public VertexColorDelta GetVertexColorDeltaUnsafe(int index) => serializedData.GetVertexColorDeltaUnsafe(index);
        public int IndexOfVertexColorDelta(string deltaName, bool caseSensitive = false) => serializedData.IndexOfVertexColorDelta(deltaName, caseSensitive);
        public List<VertexColorDelta> GetVertexColorDeltas(List<VertexColorDelta> outputList = null) => serializedData.GetVertexColorDeltas(outputList);



        public int VariationVertexGroupCount => serializedData.VariationVertexGroupCount;
        public int IndexOfVariationGroup(string name) => serializedData.IndexOfVariationGroup(name);
        public VertexGroup GetVariationVertexGroup(int index) => serializedData.GetVariationVertexGroup(index);

        public int VariationShapesControlDataSize => serializedData.VariationShapesControlDataSize;

        public int StandaloneGroupsCount => serializedData.StandaloneGroupsCount;

        public int VariationGroupsCount => VariationVertexGroupCount;

        public int MuscleGroupsCount => serializedData.MuscleGroupsCount;
        public int MuscleVertexGroupCount => MuscleGroupsCount;

        public int FatGroupsCount => serializedData.FatGroupsCount;
        public int FatVertexGroupCount => FatGroupsCount;


        public bool TryGetVertices(int lod, out NativeArray<float3> array) => serializedData.TryGetVertices(lod, out array);
        public bool TryGetColors(int lod, out NativeArray<float4> array) => serializedData.TryGetColors(lod, out array);
        public bool TryGetTriangles(int lod, out NativeArray<int> array) => serializedData.TryGetTriangles(lod, out array);
        public bool TryGetBoneWeights(int lod, out NativeArray<BoneWeight8> array) => serializedData.TryGetBoneWeights(lod, out array);

        public bool TryGetUV0(int lod, out NativeArray<float4> array) => serializedData.TryGetUV0(lod, out array);
        public bool TryGetUV1(int lod, out NativeArray<float4> array) => serializedData.TryGetUV1(lod, out array);
        public bool TryGetUV2(int lod, out NativeArray<float4> array) => serializedData.TryGetUV2(lod, out array);
        public bool TryGetUV3(int lod, out NativeArray<float4> array) => serializedData.TryGetUV3(lod, out array);
        public bool TryGetUV(int lod, UVChannelURP channel, out NativeArray<float4> array) => serializedData.TryGetUV(lod, channel, out array);

    }
}

#endif