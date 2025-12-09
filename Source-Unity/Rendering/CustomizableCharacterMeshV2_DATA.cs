#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using static Swole.ICustomizableCharacter.Defaults;

namespace Swole.Morphing
{
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
        public int IndexOfStandaloneShape(string name) => serializedData.IndexOfStandaloneShape(name);
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


        public string VariationShapesBufferRangePropertyName => serializedData.VariationShapesBufferRangePropertyName;
        public int VariationShapesCount => serializedData.VariationShapesCount;
        public int IndexOfVariationShape(string name) => serializedData.IndexOfVariationShape(name);
        public MeshShape GetVariationShape(int index) => serializedData.GetVariationShape(index);

        public int VariationVertexGroupCount => serializedData.VariationVertexGroupCount;
        public int IndexOfVariationGroup(string name) => serializedData.IndexOfVariationGroup(name);
        public VertexGroup GetVariationVertexGroup(int index) => serializedData.GetVariationVertexGroup(index);

        public int StandaloneGroupsCount => serializedData.StandaloneGroupsCount;

        public int VariationGroupsCount => VariationVertexGroupCount;

        public int MuscleGroupsCount => serializedData.MuscleGroupsCount;
        public int MuscleVertexGroupCount => MuscleGroupsCount;

        public int FatGroupsCount => serializedData.FatGroupsCount;
        public int FatVertexGroupCount => FatGroupsCount;

    }
}

#endif