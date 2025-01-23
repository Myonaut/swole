using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Swole
{
    [CreateAssetMenu(fileName = "CustomizableCharacterMeshData", menuName = "InstancedMeshData/CustomizableCharacterMeshData", order = 3)]
    public class CustomizableCharacterMeshData : MorphableMeshData
    {

        protected override void DisposeLocal()
        {
            base.DisposeLocal();

            try
            {
                if (variationGroupIndicesBuffer != null && variationGroupIndicesBuffer.IsValid())
                {
                    variationGroupIndicesBuffer.Dispose();
                }
                variationGroupIndicesBuffer = null; 
            }
            catch (Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogException(ex);
#endif
            }

            try
            {
                if (frameWeightsMuscleShapesBuffer != null && frameWeightsMuscleShapesBuffer.IsValid())
                {
                    frameWeightsMuscleShapesBuffer.Dispose();
                }
                frameWeightsMuscleShapesBuffer = null; 
            }
            catch (Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogException(ex);
#endif
            }
            try
            {
                if (frameWeightsFlexShapesBuffer != null && frameWeightsFlexShapesBuffer.IsValid())
                {
                    frameWeightsFlexShapesBuffer.Dispose();
                }
                frameWeightsFlexShapesBuffer = null;
            }
            catch (Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogException(ex);
#endif
            }
            try
            {
                if (frameWeightsFatShapesBuffer != null && frameWeightsFatShapesBuffer.IsValid())
                {
                    frameWeightsFatShapesBuffer.Dispose();
                }
                frameWeightsFatShapesBuffer = null;
            }
            catch (Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogException(ex);
#endif
            }
        }


        public override void Initialize()
        {
            if (initialized) return;

            base.Initialize();

            ApplyFloatToMaterials(MidlineVertexGroupIndexPropertyName, midlineVertexGroupIndex);

            ApplyFloatToMaterials(VariationGroupCountPropertyName, VariationVertexGroupCount);
            ApplyBufferToMaterials(VariationGroupIndicesPropertyName, VariationGroupIndicesBuffer);

            ApplyFloatToMaterials(BustVertexGroupIndexPropertyName, bustVertexGroupIndex);
            ApplyFloatToMaterials(NippleMaskVertexGroupIndexPropertyName, nippleMaskVertexGroupIndex);
            ApplyFloatToMaterials(GenitalMaskVertexGroupIndexPropertyName, genitalMaskVertexGroupIndex); 

            ApplyFloatToMaterials(FatMuscleBlendShapeIndexPropertyName, fatMuscleBlendShapeIndex);
            ApplyVectorToMaterials(FatMuscleBlendWeightRangePropertyName, new Vector4(fatMuscleBlendWeightRange.x, fatMuscleBlendWeightRange.y, 0, 0));

            ApplyFloatToMaterials(DefaultShapeMuscleWeightPropertyName, defaultShapeMuscleWeight);         

            ApplyVectorToMaterials(VertexGroupsBufferRangePropertyName, new Vector4(vertexGroupsBufferRange.x, vertexGroupsBufferRange.y, 0, 0));
            ApplyVectorToMaterials(MuscleVertexGroupsBufferRangePropertyName, new Vector4(muscleVertexGroupsBufferRange.x, muscleVertexGroupsBufferRange.y, 0, 0));
            ApplyVectorToMaterials(FatVertexGroupsBufferRangePropertyName, new Vector4(fatVertexGroupsBufferRange.x, fatVertexGroupsBufferRange.y, 0, 0));

            ApplyVectorToMaterials(StandaloneShapesBufferRangePropertyName, new Vector4(standaloneShapesBufferRange.x, standaloneShapesBufferRange.y, 0, 0));
            ApplyVectorToMaterials(MuscleShapesBufferRangePropertyName, new Vector4(muscleShapesBufferRange.x, muscleShapesBufferRange.y, 0, 0));
            ApplyVectorToMaterials(FlexShapesBufferRangePropertyName, new Vector4(flexShapesBufferRange.x, flexShapesBufferRange.y, 0, 0)); 
            ApplyVectorToMaterials(FatShapesBufferRangePropertyName, new Vector4(fatShapesBufferRange.x, fatShapesBufferRange.y, 0, 0));
            ApplyVectorToMaterials(VariationShapesBufferRangePropertyName, new Vector4(variationShapesBufferRange.x, variationShapesBufferRange.y, 0, 0));

            ApplyBufferToMaterials(FrameWeightsMuscleShapesPropertyName, FrameWeightsMuscleShapesBuffer);
            ApplyBufferToMaterials(FrameWeightsFlexShapesPropertyName, FrameWeightsFlexShapesBuffer); 
            ApplyBufferToMaterials(FrameWeightsFatShapesPropertyName, FrameWeightsFatShapesBuffer);
        }


        public const string _frameWeightsMuscleShapesDefaultPropertyName = "_FrameWeightsMuscleShapes";
        public string frameWeightsMuscleShapesPropertyNameOverride;
        public string FrameWeightsMuscleShapesPropertyName => string.IsNullOrWhiteSpace(frameWeightsMuscleShapesPropertyNameOverride) ? _frameWeightsMuscleShapesDefaultPropertyName : frameWeightsMuscleShapesPropertyNameOverride;
        public float[] frameWeightsMuscleShapes;
        [NonSerialized]
        protected ComputeBuffer frameWeightsMuscleShapesBuffer;
        public ComputeBuffer FrameWeightsMuscleShapesBuffer
        {
            get
            {
                if (frameWeightsMuscleShapesBuffer == null)
                {
                    if (frameWeightsMuscleShapes != null && frameWeightsMuscleShapes.Length > 0)
                    {
                        frameWeightsMuscleShapesBuffer = new ComputeBuffer(frameWeightsMuscleShapes.Length, UnsafeUtility.SizeOf(typeof(float)), ComputeBufferType.Structured, ComputeBufferMode.Immutable);
                        frameWeightsMuscleShapesBuffer.SetData(frameWeightsMuscleShapes);
                    }

                    TrackDisposables();
                }

                return frameWeightsMuscleShapesBuffer;
            }
        }

        public const string _frameWeightsFlexShapesDefaultPropertyName = "_FrameWeightsFlexShapes";
        public string frameWeightsFlexShapesPropertyNameOverride;
        public string FrameWeightsFlexShapesPropertyName => string.IsNullOrWhiteSpace(frameWeightsFlexShapesPropertyNameOverride) ? _frameWeightsFlexShapesDefaultPropertyName : frameWeightsFlexShapesPropertyNameOverride;
        public float[] frameWeightsFlexShapes;
        [NonSerialized]
        protected ComputeBuffer frameWeightsFlexShapesBuffer;
        public ComputeBuffer FrameWeightsFlexShapesBuffer
        {
            get
            {
                if (frameWeightsFlexShapesBuffer == null)
                {
                    if (frameWeightsFlexShapes != null && frameWeightsFlexShapes.Length > 0)
                    {
                        frameWeightsFlexShapesBuffer = new ComputeBuffer(frameWeightsFlexShapes.Length, UnsafeUtility.SizeOf(typeof(float)), ComputeBufferType.Structured, ComputeBufferMode.Immutable);
                        frameWeightsFlexShapesBuffer.SetData(frameWeightsFlexShapes);
                    }

                    TrackDisposables();
                }

                return frameWeightsFlexShapesBuffer;
            }
        }

        public const string _frameWeightsFatShapesDefaultPropertyName = "_FrameWeightsFatShapes";
        public string frameWeightsFatShapesPropertyNameOverride;
        public string FrameWeightsFatShapesPropertyName => string.IsNullOrWhiteSpace(frameWeightsFatShapesPropertyNameOverride) ? _frameWeightsFatShapesDefaultPropertyName : frameWeightsFatShapesPropertyNameOverride;
        public float[] frameWeightsFatShapes;
        [NonSerialized]
        protected ComputeBuffer frameWeightsFatShapesBuffer;
        public ComputeBuffer FrameWeightsFatShapesBuffer
        {
            get
            {
                if (frameWeightsFatShapesBuffer == null)
                {
                    if (frameWeightsFatShapes != null && frameWeightsFatShapes.Length > 0) 
                    {
                        frameWeightsFatShapesBuffer = new ComputeBuffer(frameWeightsFatShapes.Length, UnsafeUtility.SizeOf(typeof(float)), ComputeBufferType.Structured, ComputeBufferMode.Immutable);
                        frameWeightsFatShapesBuffer.SetData(frameWeightsFatShapes);
                    }

                    TrackDisposables();
                }

                return frameWeightsFatShapesBuffer; 
            }
        }


        public const string _vertexGroupsBufferRangeDefaultPropertyName = "_RangeVertexGroups";
        public string vertexGroupsBufferRangePropertyNameOverride;
        public string VertexGroupsBufferRangePropertyName => string.IsNullOrWhiteSpace(vertexGroupsBufferRangePropertyNameOverride) ? _vertexGroupsBufferRangeDefaultPropertyName : vertexGroupsBufferRangePropertyNameOverride;
        public Vector2Int vertexGroupsBufferRange;
        public int StandaloneVertexGroupCount => Mathf.Max(0, (vertexGroupsBufferRange.y - vertexGroupsBufferRange.x) + 1);
        public int IndexOfStandaloneVertexGroup(string name)
        {
            for (int a = 0; a < StandaloneVertexGroupCount; a++)
            {
                var shape = vertexGroups[a + vertexGroupsBufferRange.x];
                if (shape.name == name) return a;
            }

            return -1;
        }
        public VertexGroup GetStandaloneVertexGroup(int index)
        {
            if (index < 0 || index >= StandaloneVertexGroupCount) return null;
            return vertexGroups[vertexGroupsBufferRange.x + index];
        }

        public const string _muscleVertexGroupsBufferRangeDefaultPropertyName = "_RangeMuscleGroups"; 
        public string muscleVertexGroupsBufferRangePropertyNameOverride;
        public string MuscleVertexGroupsBufferRangePropertyName => string.IsNullOrWhiteSpace(muscleVertexGroupsBufferRangePropertyNameOverride) ? _muscleVertexGroupsBufferRangeDefaultPropertyName : muscleVertexGroupsBufferRangePropertyNameOverride;
        public Vector2Int muscleVertexGroupsBufferRange;
        public int MuscleVertexGroupCount => Mathf.Max(0, (muscleVertexGroupsBufferRange.y - muscleVertexGroupsBufferRange.x) + 1);
        public int IndexOfMuscleGroup(string name)
        {
            for (int a = 0; a < MuscleVertexGroupCount; a++)
            {
                var shape = vertexGroups[a + muscleVertexGroupsBufferRange.x];
                if (shape.name == name) return a;
            }

            return -1;
        }
        public VertexGroup GetMuscleVertexGroup(int index)
        {
            if (index < 0 || index >= MuscleVertexGroupCount) return null;
            return vertexGroups[muscleVertexGroupsBufferRange.x + index];
        }

        public const string _fatVertexGroupsBufferRangeDefaultPropertyName = "_RangeFatGroups";
        public string fatVertexGroupsBufferRangePropertyNameOverride;
        public string FatVertexGroupsBufferRangePropertyName => string.IsNullOrWhiteSpace(fatVertexGroupsBufferRangePropertyNameOverride) ? _fatVertexGroupsBufferRangeDefaultPropertyName : fatVertexGroupsBufferRangePropertyNameOverride;
        public Vector2Int fatVertexGroupsBufferRange;
        public float2[] fatGroupModifiers;
        public int FatVertexGroupCount => Mathf.Max(0, (fatVertexGroupsBufferRange.y - fatVertexGroupsBufferRange.x) + 1);
        public int IndexOfFatGroup(string name)
        {
            for (int a = 0; a < FatVertexGroupCount; a++)
            {
                var shape = vertexGroups[a + fatVertexGroupsBufferRange.x]; 
                if (shape.name == name) return a;
            }

            return -1;
        }
        public VertexGroup GetFatVertexGroup(int index)
        {
            if (index < 0 || index >= FatVertexGroupCount) return null;
            return vertexGroups[fatVertexGroupsBufferRange.x + index];
        }
        public static float2 DefaultFatGroupModifier => new float2(1, 0);
        /// <summary>
        /// modifier.x is how much to nerf muscle mass by based on fat level
        public float2 GetFatGroupModifier(int index) 
        {
            if (index < 0 || fatGroupModifiers == null || index >= fatGroupModifiers.Length) return DefaultFatGroupModifier;
            return fatGroupModifiers[index];
        }

        public const string _midlineVertexGroupIndexDefaultPropertyName = "_MidlineVertexGroupIndex";
        public string midlineVertexGroupIndexPropertyNameOverride;
        public string MidlineVertexGroupIndexPropertyName => string.IsNullOrWhiteSpace(midlineVertexGroupIndexPropertyNameOverride) ? _midlineVertexGroupIndexDefaultPropertyName : midlineVertexGroupIndexPropertyNameOverride;
        public int midlineVertexGroupIndex;

        public const string _bustMixDefaultPropertyName = "_BustMix";
        public string bustMixPropertyNameOverride;
        public string BustMixPropertyName => string.IsNullOrWhiteSpace(bustMixPropertyNameOverride) ? _bustMixDefaultPropertyName : bustMixPropertyNameOverride;

        public const string _hideNipplesDefaultPropertyName = "_HideNipples";
        public string hideNipplesPropertyNameOverride;
        public string HideNipplesPropertyName => string.IsNullOrWhiteSpace(hideNipplesPropertyNameOverride) ? _hideNipplesDefaultPropertyName : hideNipplesPropertyNameOverride;

        public const string _hideGenitalsDefaultPropertyName = "_HideGenitals";
        public string hideGenitalsPropertyNameOverride;
        public string HideGenitalsPropertyName => string.IsNullOrWhiteSpace(hideGenitalsPropertyNameOverride) ? _hideGenitalsDefaultPropertyName : hideGenitalsPropertyNameOverride;

        public const string _bustVertexGroupIndexDefaultPropertyName = "_BustVertexGroupIndex";
        public string bustVertexGroupIndexPropertyNameOverride;
        public string BustVertexGroupIndexPropertyName => string.IsNullOrWhiteSpace(bustVertexGroupIndexPropertyNameOverride) ? _bustVertexGroupIndexDefaultPropertyName : bustVertexGroupIndexPropertyNameOverride;
        public int bustVertexGroupIndex;
        public int bustSizeShapeIndex;

        public const string _nippleMaskVertexGroupIndexDefaultPropertyName = "_NippleMaskVertexGroupIndex";
        public string nippleMaskVertexGroupIndexPropertyNameOverride;
        public string NippleMaskVertexGroupIndexPropertyName => string.IsNullOrWhiteSpace(nippleMaskVertexGroupIndexPropertyNameOverride) ? _nippleMaskVertexGroupIndexDefaultPropertyName : nippleMaskVertexGroupIndexPropertyNameOverride;
        public int nippleMaskVertexGroupIndex;

        public const string _genitalMaskVertexGroupIndexDefaultPropertyName = "_GenitalMaskVertexGroupIndex";
        public string genitalMaskVertexGroupIndexPropertyNameOverride;
        public string GenitalMaskVertexGroupIndexPropertyName => string.IsNullOrWhiteSpace(genitalMaskVertexGroupIndexPropertyNameOverride) ? _genitalMaskVertexGroupIndexDefaultPropertyName : genitalMaskVertexGroupIndexPropertyNameOverride;
        public int genitalMaskVertexGroupIndex;

        public const string _fatMuscleBlendShapeIndexDefaultPropertyName = "_FatMuscleBlendShapeIndex";
        public string fatMuscleBlendShapeIndexPropertyNameOverride;
        public string FatMuscleBlendShapeIndexPropertyName => string.IsNullOrWhiteSpace(fatMuscleBlendShapeIndexPropertyNameOverride) ? _fatMuscleBlendShapeIndexDefaultPropertyName : fatMuscleBlendShapeIndexPropertyNameOverride;
        public int fatMuscleBlendShapeIndex;

        public const string _fatMuscleBlendWeightRangeDefaultPropertyName = "_FatMuscleBlendWeightRange";
        public string fatMuscleBlendWeightRangePropertyNameOverride;
        public string FatMuscleBlendWeightRangePropertyName => string.IsNullOrWhiteSpace(fatMuscleBlendWeightRangePropertyNameOverride) ? _fatMuscleBlendWeightRangeDefaultPropertyName : fatMuscleBlendWeightRangePropertyNameOverride;
        public Vector2 fatMuscleBlendWeightRange;

        public const string _defaultShapeMuscleWeightDefaultPropertyName = "_DefaultShapeMuscleWeight";
        public string defaultShapeMuscleWeightPropertyNameOverride;
        public string DefaultShapeMuscleWeightPropertyName => string.IsNullOrWhiteSpace(defaultShapeMuscleWeightPropertyNameOverride) ? _defaultShapeMuscleWeightDefaultPropertyName : defaultShapeMuscleWeightPropertyNameOverride;
        public float defaultShapeMuscleWeight;
        

        public const string _variationGroupCountDefaultPropertyName = "_VariationGroupCount";
        public string variationGroupCountPropertyNameOverride;
        public string VariationGroupCountPropertyName => string.IsNullOrWhiteSpace(variationGroupCountPropertyNameOverride) ? _variationGroupCountDefaultPropertyName : variationGroupCountPropertyNameOverride;

        public const string _variationGroupIndicesDefaultPropertyName = "_VariationGroups";
        public string variationGroupIndicesPropertyNameOverride;
        public string VariationGroupIndicesPropertyName => string.IsNullOrWhiteSpace(variationGroupIndicesPropertyNameOverride) ? _variationGroupIndicesDefaultPropertyName : variationGroupIndicesPropertyNameOverride;
        public int[] variationGroupIndices;
        public int VariationVertexGroupCount => variationGroupIndices == null ? 0 : variationGroupIndices.Length;
        [NonSerialized]
        protected ComputeBuffer variationGroupIndicesBuffer;
        public ComputeBuffer VariationGroupIndicesBuffer
        {
            get
            {
                if (variationGroupIndicesBuffer == null)
                {
                    if (variationGroupIndices != null && variationGroupIndices.Length > 0)
                    {
                        variationGroupIndicesBuffer = new ComputeBuffer(variationGroupIndices.Length, UnsafeUtility.SizeOf(typeof(int)), ComputeBufferType.Structured, ComputeBufferMode.Immutable);
                        variationGroupIndicesBuffer.SetData(variationGroupIndices);
                    }

                    TrackDisposables();
                }

                return variationGroupIndicesBuffer;
            }
        }


        public const string _standaloneShapesBufferRangeDefaultPropertyName = "_RangeStandaloneShapes";
        public string standaloneShapesBufferRangePropertyNameOverride;
        public string StandaloneShapesBufferRangePropertyName => string.IsNullOrWhiteSpace(standaloneShapesBufferRangePropertyNameOverride) ? _standaloneShapesBufferRangeDefaultPropertyName : standaloneShapesBufferRangePropertyNameOverride;
        public Vector2Int standaloneShapesBufferRange;
        public int StandaloneShapesCount => Mathf.Max(0, (standaloneShapesBufferRange.y - standaloneShapesBufferRange.x) + 1);
        public int IndexOfStandaloneShape(string name)
        {
            for (int a = 0; a < StandaloneShapesCount; a++)
            {
                var shape = morphShapes[a + standaloneShapesBufferRange.x];
                if (shape.name == name) return a;
            }

            return -1;
        }
        public MorphShape GetStandaloneShape(int index)
        {
            if (index < 0 || index >= StandaloneShapesCount) return null;
            return morphShapes[standaloneShapesBufferRange.x + index];
        }

        public const string _muscleShapesBufferRangeDefaultPropertyName = "_RangeMuscleShapes";
        public string muscleShapesBufferRangePropertyNameOverride;
        public string MuscleShapesBufferRangePropertyName => string.IsNullOrWhiteSpace(muscleShapesBufferRangePropertyNameOverride) ? _muscleShapesBufferRangeDefaultPropertyName : muscleShapesBufferRangePropertyNameOverride;
        public Vector2Int muscleShapesBufferRange;
        public int MuscleShapesCount => Mathf.Max(0, (muscleShapesBufferRange.y - muscleShapesBufferRange.x) + 1);
        public int IndexOfMuscleShape(string name)
        {
            for (int a = 0; a < MuscleShapesCount; a++)
            {
                var shape = morphShapes[a + muscleShapesBufferRange.x];
                if (shape.name == name) return a;
            }

            return -1;
        }
        public MorphShape GetMuscleShape(int index)
        {
            if (index < 0 || index >= MuscleShapesCount) return null;
            return morphShapes[muscleShapesBufferRange.x + index];
        }

        public const string _flexShapesBufferRangeDefaultPropertyName = "_RangeFlexShapes";
        public string flexShapesBufferRangePropertyNameOverride;
        public string FlexShapesBufferRangePropertyName => string.IsNullOrWhiteSpace(flexShapesBufferRangePropertyNameOverride) ? _flexShapesBufferRangeDefaultPropertyName : flexShapesBufferRangePropertyNameOverride;
        public Vector2Int flexShapesBufferRange;
        public int FlexShapesCount => Mathf.Max(0, (flexShapesBufferRange.y - flexShapesBufferRange.x) + 1);
        public int IndexOfFlexShape(string name)
        {
            for (int a = 0; a < FlexShapesCount; a++)
            {
                var shape = morphShapes[a + flexShapesBufferRange.x];
                if (shape.name == name) return a;
            }

            return -1;
        }
        public MorphShape GetFlexShape(int index)
        {
            if (index < 0 || index >= FlexShapesCount) return null;
            return morphShapes[flexShapesBufferRange.x + index];
        }

        public const string _fatShapesBufferRangeDefaultPropertyName = "_RangeFatShapes";
        public string fatShapesBufferRangePropertyNameOverride;
        public string FatShapesBufferRangePropertyName => string.IsNullOrWhiteSpace(fatShapesBufferRangePropertyNameOverride) ? _fatShapesBufferRangeDefaultPropertyName : fatShapesBufferRangePropertyNameOverride;
        public Vector2Int fatShapesBufferRange;
        public int FatShapesCount => Mathf.Max(0, (fatShapesBufferRange.y - fatShapesBufferRange.x) + 1);
        public int IndexOfFatShape(string name)
        {
            for (int a = 0; a < FatShapesCount; a++)
            {
                var shape = morphShapes[a + fatShapesBufferRange.x];
                if (shape.name == name) return a;
            }

            return -1;
        }
        public MorphShape GetFatShape(int index)
        {
            if (index < 0 || index >= FatShapesCount) return null;
            return morphShapes[fatShapesBufferRange.x + index];
        }

        public const string _variationShapesBufferRangeDefaultPropertyName = "_RangeVariationShapes";
        public string variationShapesBufferRangePropertyNameOverride; 
        public string VariationShapesBufferRangePropertyName => string.IsNullOrWhiteSpace(variationShapesBufferRangePropertyNameOverride) ? _variationShapesBufferRangeDefaultPropertyName : variationShapesBufferRangePropertyNameOverride;
        public Vector2Int variationShapesBufferRange;
        public int VariationShapesCount => Mathf.Max(0, (variationShapesBufferRange.y - variationShapesBufferRange.x) + 1);
        public int IndexOfVariationShape(string name)
        {
            for (int a = 0; a < VariationShapesCount; a++)
            {
                var shape = morphShapes[a + variationShapesBufferRange.x]; 
                if (shape.name == name) return a;
            }

            return -1;
        }
        public MorphShape GetVariationShape(int index)
        {
            if (index < 0 || index >= VariationShapesCount) return null;
            return morphShapes[variationShapesBufferRange.x + index];
        }

        public const string _standaloneShapesControlDefaultPropertyName = "_ControlStandaloneShapes";
        public string standaloneShapesControlPropertyNameOverride;
        public string StandaloneShapesControlPropertyName => string.IsNullOrWhiteSpace(standaloneShapesControlPropertyNameOverride) ? _standaloneShapesControlDefaultPropertyName : standaloneShapesControlPropertyNameOverride;

        public const string _muscleGroupsControlDefaultPropertyName = "_ControlMuscleGroups";
        public string muscleGroupsControlPropertyNameOverride;
        public string MuscleGroupsControlPropertyName => string.IsNullOrWhiteSpace(muscleGroupsControlPropertyNameOverride) ? _muscleGroupsControlDefaultPropertyName : muscleGroupsControlPropertyNameOverride; 

        public const string _fatGroupsControlDefaultPropertyName = "_ControlFatGroups";
        public string fatGroupsControlPropertyNameOverride;
        public string FatGroupsControlPropertyName => string.IsNullOrWhiteSpace(fatGroupsControlPropertyNameOverride) ? _fatGroupsControlDefaultPropertyName : fatGroupsControlPropertyNameOverride; 

        public const string _variationShapesControlDefaultPropertyName = "_ControlVariationShapes"; 
        public string variationShapesControlPropertyNameOverride;
        public string VariationShapesControlPropertyName => string.IsNullOrWhiteSpace(variationShapesControlPropertyNameOverride) ? _variationShapesControlDefaultPropertyName : variationShapesControlPropertyNameOverride;


        public const string _shapesInstanceIDPropertyName = "_ShapesInstanceID";
        public string shapesInstanceIDPropertyNameOverride;
        public string ShapesInstanceIDPropertyName => string.IsNullOrWhiteSpace(shapesInstanceIDPropertyNameOverride) ? _shapesInstanceIDPropertyName : shapesInstanceIDPropertyNameOverride;

        public const string _rigInstanceIDPropertyName = "_RigInstanceID";
        public string rigInstanceIDPropertyNameOverride;
        public string RigInstanceIDPropertyName => string.IsNullOrWhiteSpace(rigInstanceIDPropertyNameOverride) ? _rigInstanceIDPropertyName : rigInstanceIDPropertyNameOverride;

        public const string _characterInstanceIDPropertyName = "_CharacterInstanceID";
        public string characterInstanceIDPropertyNameOverride;
        public string CharacterInstanceIDPropertyName => string.IsNullOrWhiteSpace(characterInstanceIDPropertyNameOverride) ? _characterInstanceIDPropertyName : characterInstanceIDPropertyNameOverride;

    }
}
