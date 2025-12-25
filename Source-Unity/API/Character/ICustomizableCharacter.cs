#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Unity.Mathematics;

using UnityEngine;
using UnityEngine.Events;

using Swole.API.Unity.Animation;

namespace Swole
{

    public interface ICustomizableCharacter
    {

        public bool IsInitialized { get; }

        public string Name
        {
            get;
        }

        public GameObject GameObject
        {
            get;
        }

        public float BustSize
        {
            get;
            set;
        }
        public void SetBustSize(float value);

        public bool HideNipples
        {
            get;
            set;
        }
        public void SetHideNipples(bool value);

        public bool HideGenitals
        {
            get;
            set;
        }
        public void SetHideGenitals(bool value);


        public void SetAvatar(CustomAvatar av);
        public CustomAvatar Avatar { get; }

        public void SetRigRoot(Transform root);
        public Transform RigRoot
        {
            get;
        }
        public Transform BoundsRootTransform { get; }

        public void SetAnimatablePropertiesController(DynamicAnimationProperties controller);

        public CustomAnimator Animator
        {
            get;
            set;
        }

        public string RigID
        {
            get;
        }


        public void SetRigBufferID(string id);
        public string LocalRigBufferID { get; }
        public bool RigInstanceReferenceIsValid { get; }
        public string RigBufferID { get; }


        public void SetShapeBufferID(string id);
        public string LocalShapeBufferID { get; }
        public string ShapeBufferID { get; }


        public void SetMorphBufferID(string id);
        public string LocalMorphBufferID { get; }
        public string MorphBufferID { get; }

        public int ShapesInstanceID { get; }

        public int RigInstanceID { get; }

        public Rigs.StandaloneSampler RigSampler { get; }

        public int CharacterInstanceID { get; }

        public void SetShapesInstanceID(int id);
        public void SetRigInstanceID(int id);
        public void SetCharacterInstanceID(int id);

        public ICustomizableCharacter ShapesInstanceReference { get; set; }
        public InstanceableSkinnedMeshBase RigInstanceReference { get; set; }
        public ICustomizableCharacter CharacterInstanceReference { get; set; }

        public Transform[] Bones
        {
            get;
        }
        public Transform[] SkinnedBones
        {
            get;
        }

        public int BoneCount { get; }

        public Matrix4x4[] BindPose { get; }


        public int IndexOfVertexGroup(string groupName, bool caseSensitive = false);
        public VertexGroup GetVertexGroup(int index);
        public int IndexOfStandaloneVertexGroup(string groupName, bool caseSensitive = false);
        public VertexGroup GetStandaloneVertexGroup(int index);


        public int FirstStandaloneShapesControlIndex { get; }
        public float GetStandaloneShapeWeightUnsafe(int shapeIndex);
        public float GetStandaloneShapeWeight(int shapeIndex);
        public void SetStandaloneShapeWeightUnsafe(int shapeIndex, float weight);
        public void SetStandaloneShapeWeight(int shapeIndex, float weight);
        public InstanceBuffer<float> StandaloneShapeControlBuffer
        {
            get;
        }
        public int IndexOfStandaloneShape(string shapeName, bool caseSensitive = false);

        public int FirstMuscleGroupsControlIndex { get; }
        public MuscleDataLR GetMuscleDataUnsafe(int groupIndex);
        public MuscleDataLR GetMuscleData(int groupIndex);
        public void SetMuscleDataUnsafe(int groupIndex, MuscleDataLR data);
        public void SetMuscleData(int groupIndex, MuscleDataLR data);
        public InstanceBuffer<MuscleDataLR> MuscleGroupsControlBuffer
        {
            get;
        }
        public int IndexOfMuscleGroup(string groupName, bool caseSensitive = false);

        public int FirstFatGroupsControlIndex { get; }
        public float GetFatLevelUnsafe(int groupIndex);
        public float GetFatLevel(int groupIndex);
        public void SetFatLevelUnsafe(int groupIndex, float level);
        public void SetFatLevel(int groupIndex, float level);
        public float2 GetBodyHairLevelUnsafe(int groupIndex);
        public float2 GetBodyHairLevel(int groupIndex);
        public void SetBodyHairLevelUnsafe(int groupIndex, float level, float blend = 1f);
        public void SetBodyHairLevel(int groupIndex, float level, float blend = 1f);
        public InstanceBuffer<float4> FatGroupsControlBuffer
        {
            get;
        }
        public int IndexOfFatGroup(string groupName, bool caseSensitive = false);

        public int VariationShapesControlDataSize { get; }
        public int FirstVariationShapesControlIndex { get; }

        public int GetPartialVariationShapeIndex(int variationGroupIndex, int shapeIndex);

        public float2 GetVariationWeightUnsafe(int variationShapeIndex, int groupIndex);
        public float2 GetVariationWeight(int variationShapeIndex, int groupIndex);
        public void SetVariationWeightUnsafe(int variationShapeIndex, int groupIndex, float2 weight);
        public void SetVariationWeight(int variationShapeIndex, int groupIndex, float2 weight);

        public float2 GetVariationWeightUnsafe(int indexInArray);
        public float2 GetVariationWeight(int indexInArray);
        public void SetVariationWeightUnsafe(int indexInArray, float2 weight);
        public void SetVariationWeight(int indexInArray, float2 weight);

        public InstanceBuffer<float2> VariationShapesControlBuffer
        {
            get;
        }
        public int IndexOfVariationGroup(string groupName, bool caseSensitive = false);
        public int IndexOfVariationShape(string shapeName, bool caseSensitive = false);

        public void SetFloatOverride(string propertyName, float value, bool updateMaterials = true);
        public void SetIntegerOverride(string property, int value, bool updateMaterials = true);
        public void SetVectorOverride(string propertyName, Vector4 vector, bool updateMaterials = true);
        public void SetColorOverride(string propertyName, Color color, bool updateMaterials = true);

        [Serializable]
        public enum ListenableEvent
        {
            OnMuscleDataChanged,
            OnFatDataChanged
        }

        public void AddListener(ListenableEvent event_, UnityAction<int> listener);

        public void RemoveListener(ListenableEvent event_, UnityAction<int> listener);

        public void ClearListeners();

        #region Defaults

        public static class Defaults
        {

            public static string GetMuscleMassShapeSyncNameLeft(int groupIndex, int shapeIndex) => $"MASS_LEFT:{groupIndex}:{shapeIndex}";
            public static string GetMuscleMassShapeSyncNameRight(int groupIndex, int shapeIndex) => $"MASS_RIGHT:{groupIndex}:{shapeIndex}";
            public static string GetMuscleFlexShapeSyncNameLeft(int groupIndex, int shapeIndex) => $"FLEX_LEFT:{groupIndex}:{shapeIndex}";
            public static string GetMuscleFlexShapeSyncNameRight(int groupIndex, int shapeIndex) => $"FLEX_RIGHT:{groupIndex}:{shapeIndex}";

            public static string GetFatShapeSyncName(int groupIndex, int shapeIndex) => $"FAT:{groupIndex}:{shapeIndex}";

            public static string GetVariationShapeSyncNameLeft(int groupIndex, int shapeIndex) => $"VARIATION_LEFT:{groupIndex}:{shapeIndex}";
            public static string GetVariationShapeSyncNameRight(int groupIndex, int shapeIndex) => $"VARIATION_RIGHT:{groupIndex}:{shapeIndex}";

            public const string _lodLevelDefaultPropertyName = "_LOD_Level";

            public const string _vertexCountDefaultPropertyName = "_VertexCount";

            public const string _skinningDataDefaultPropertyName = "_SkinBindings";

            public const string _boneCountDefaultPropertyName = "_BoneCount";

            public const string _skinningMatricesDefaultPropertyName = "_SkinningMatrices";

            public const string _frameWeightsMuscleShapesDefaultPropertyName = "_FrameWeightsMuscleShapes";

            public const string _frameWeightsFlexShapesDefaultPropertyName = "_FrameWeightsFlexShapes";

            public const string _frameWeightsFatShapesDefaultPropertyName = "_FrameWeightsFatShapes";


            public const string _vertexGroupsBufferRangeDefaultPropertyName = "_RangeVertexGroups";

            public const string _muscleVertexGroupsBufferRangeDefaultPropertyName = "_RangeMuscleGroups";

            public const string _fatVertexGroupsBufferRangeDefaultPropertyName = "_RangeFatGroups";

            public const string _variationVertexGroupsBufferRangeDefaultPropertyName = "_RangeVariationGroups";

            public const string _midlineVertexGroupIndexDefaultPropertyName = "_MidlineVertexGroupIndex";

            public const string _bustMixDefaultPropertyName = "_BustMix";

            public const string _hideNipplesDefaultPropertyName = "_HideNipples";

            public const string _hideGenitalsDefaultPropertyName = "_HideGenitals";

            public const string _bustVertexGroupIndexDefaultPropertyName = "_BustVertexGroupIndex";

            public const string _bustNerfVertexGroupIndexDefaultPropertyName = "_BustNerfVertexGroupIndex";

            public const string _nippleMaskVertexGroupIndexDefaultPropertyName = "_NippleMaskVertexGroupIndex";

            public const string _genitalMaskVertexGroupIndexDefaultPropertyName = "_GenitalMaskVertexGroupIndex";

            public const string _fatMuscleBlendShapeIndexDefaultPropertyName = "_FatMuscleBlendShapeIndex";

            public const string _fatMuscleBlendWeightRangeDefaultPropertyName = "_FatMuscleBlendWeightRange";

            public const string _defaultShapeMuscleWeightDefaultPropertyName = "_DefaultShapeMuscleWeight";


            public const string _variationGroupCountDefaultPropertyName = "_VariationGroupCount";

            public const string _variationGroupIndicesDefaultPropertyName = "_VariationGroups";


            public const string _standaloneShapesBufferRangeDefaultPropertyName = "_RangeStandaloneShapes";

            public const string _muscleShapesBufferRangeDefaultPropertyName = "_RangeMuscleShapes";

            public const string _flexShapesBufferRangeDefaultPropertyName = "_RangeFlexShapes";

            public const string _fatShapesBufferRangeDefaultPropertyName = "_RangeFatShapes";

            public const string _variationShapesBufferRangeDefaultPropertyName = "_RangeVariationShapes";

            public const string _standaloneShapesControlDefaultPropertyName = "_ControlStandaloneShapes";

            public const string _muscleGroupsControlDefaultPropertyName = "_ControlMuscleGroups";

            public const string _fatGroupsControlDefaultPropertyName = "_ControlFatGroups";

            public const string _variationShapesControlDefaultPropertyName = "_ControlVariationShapes";

            public const string _localInstanceIDPropertyName = "_InstanceID";

            public const string _shapesInstanceIDPropertyName = "_ShapesInstanceID";

            public const string _characterInstanceIDPropertyName = "_CharacterInstanceID";

            public const string _muscleGroupInfluencesDefaultPropertyName = "_MuscleGroupInfluences";
            public const string _fatGroupInfluencesDefaultPropertyName = "_FatGroupInfluences";
            public const string _perVertexDeltaDataDefaultPropertyName = "_PerVertexDeltaData";

            public const string _muscleMassShapeIndexDefaultPropertyName = "_MuscleMassShapeIndex";
            public const string _flexShapeIndexDefaultPropertyName = "_FlexShapeIndex";
            public const string _fatShapeIndexDefaultPropertyName = "_FatShapeIndex";

            public const string _vertexGroupsDefaultPropertyName = "_VertexGroups";
            public const string _meshShapeFrameDeltasDefaultPropertyName = "_MeshShapeFrameDeltas";
            public const string _meshShapeFrameWeightsDefaultPropertyName = "_MeshShapeFrameWeights";
            public const string _meshShapeIndicesDefaultPropertyName = "_MeshShapeIndices";

            public static float2 _defaultFatGroupModifier => new float2(1f, 0f);

        }

        #endregion

    }

    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct MuscleData : IEquatable<MuscleData>
    {
        [Range(0, 3)]
        public float mass;
        [Range(0, 1.5f)]
        public float flex;
        [Range(0, 1)]
        public float pump;
        [Range(0, 1)]
        public float varicose;

        public static implicit operator MuscleData(float4 data) => new MuscleData() { mass = data.x, flex = data.y, pump = data.z, varicose = data.w };
        public static implicit operator float4(MuscleData data) => new float4(data.mass, data.flex, data.pump, data.varicose);

        public override bool Equals(object obj)
        {
            if (obj is MuscleData dat) return dat.mass == mass && dat.flex == flex && dat.pump == pump && dat.varicose == varicose;
            return false;
        }
        public bool Equals(MuscleData dat) => dat.mass == mass && dat.flex == flex && dat.pump == pump && dat.varicose == varicose;

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator ==(MuscleData dat1, MuscleData dat2) => dat1.Equals(dat2);
        public static bool operator !=(MuscleData dat1, MuscleData dat2) => !dat1.Equals(dat2);
    }

    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct MuscleDataLR : IEquatable<MuscleDataLR>
    {
        public MuscleData valuesLeft;
        public MuscleData valuesRight;

        public override bool Equals(object obj)
        {
            if (obj is MuscleDataLR dat) return dat.valuesLeft == valuesLeft && dat.valuesRight == valuesRight;
            return false;
        }
        public bool Equals(MuscleDataLR dat) => dat.valuesLeft == valuesLeft && dat.valuesRight == valuesRight;

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator ==(MuscleDataLR dat1, MuscleDataLR dat2) => dat1.Equals(dat2);
        public static bool operator !=(MuscleDataLR dat1, MuscleDataLR dat2) => !dat1.Equals(dat2);
    }

}

#endif