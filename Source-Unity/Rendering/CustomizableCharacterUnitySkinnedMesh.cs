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

    public class CustomizableCharacterUnitySkinnedMesh : CustomizableCharacterMeshBase
    {

        #region Static Methods

        public static string GetMuscleMassBlendShapeNameForGroup(string groupName, int muscleShapeFrame)
        {
            return $"mass_{groupName}_{muscleShapeFrame}";
        }
        public static string GetMuscleMassBlendShapeNameForGroupLR(string groupName, int muscleShapeFrame, bool isLeft)
        {
            return $"{GetMuscleMassBlendShapeNameForGroup(groupName, muscleShapeFrame)}_{(isLeft ? "L" : "R")}";
        }

        public static string GetMuscleFlexBlendShapeNameForGroup(string groupName, int muscleShapeFrame)
        {
            return $"flex_{groupName}_{muscleShapeFrame}";
        }
        public static string GetMuscleFlexBlendShapeNameForGroupLR(string groupName, int muscleShapeFrame, bool isLeft)
        {
            return $"{GetMuscleFlexBlendShapeNameForGroup(groupName, muscleShapeFrame)}_{(isLeft ? "L" : "R")}";
        }

        public static string GetFatBlendShapeNameForGroup(string groupName, int fatShapeFrame)
        {
            return $"fat_{groupName}_{fatShapeFrame}";
        }

        public static string GetVariationBlendShapeNameForGroup(string variationGroup, string variationShape, int shapeFrame)
        {
            return $"variation_{variationGroup}_{variationShape}_{shapeFrame}";
        }
        public static string GetVariationBlendShapeNameForGroupLR(string variationGroup, string variationShape, int shapeFrame, bool isLeft)
        {
            return $"{GetVariationBlendShapeNameForGroup(variationGroup, variationShape, shapeFrame)}_{(isLeft ? "L" : "R")}";
        }

        public static void CalculateShapeFrames(float weight, float[] frameWeights, out int frameIndexA, out float frameWeightA, out int frameIndexB, out float frameWeightB)
        {
            frameIndexA = frameIndexB = 0;
            frameWeightA = 1f;
            frameWeightB = 0f;

            bool hasFrames = frameWeights != null && frameWeights.Length > 0;
            int frameCount = hasFrames ? frameWeights.Length : 1; 
            if (frameCount == 1)
            {
                frameWeightA = weight / (hasFrames ? frameWeights[0] : 1f);
            }
            else
            {
                for (int a = 0; a < frameCount; a++)
                {
                    float frameWeight = frameWeights[a];

                    if (weight < frameWeight)
                    {
                        if (a == 0)
                        {
                            frameWeightA = weight / frameWeight;
                        }
                        else
                        {
                            frameIndexA = a - 1;
                            frameIndexB = a;

                            var prevFrameWeight = frameWeights[frameIndexA];
                            frameWeightB = (weight - prevFrameWeight) / (frameWeight - prevFrameWeight);
                            frameWeightA = 1f - frameWeightB;
                        }

                        break;
                    }
                    else if (a == frameCount - 1)
                    {
                        frameIndexA = a;
                        frameIndexB = a;

                        frameWeightA = 1 + ((weight - frameWeight) / frameWeight);
                    }
                }
            }
        }

        #endregion

        #region Sub Types

        [Serializable, NonAnimatable]
        public class SerializedData : CustomizableCharacterMeshV2.SerializedDataBase
        {

            #region Fields

            [Header("Shapes")]
            public ShapeInfo[] meshShapes;

            [Header("Vertex Groups")]
            public VertexGroupInfo[] vertexGroups;

            [Header("Other")]
            public bool monoGroups;

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
                        if (shape.IsInvalid) continue;
                        count = count + shape.frameCount * vertexCount;
                    }

                    return count;
                }
            }
            public override ShapeInfo GetShapeInfo(int index)
            {
                if (index < 0 || meshShapes == null || index >= meshShapes.Length) return default;
                return GetShapeInfoUnsafe(index);
            }
            public override ShapeInfo GetShapeInfoUnsafe(int index) => meshShapes[index];
            public override int IndexOfShape(string shapeName, bool caseSensitive = false)
            {
                if (meshShapes == null) return -1;

                for (int a = 0; a < meshShapes.Length; a++)
                {
                    var morph = meshShapes[a];
                    if (morph.IsInvalid) continue;

                    if (morph.name == shapeName) return a;
                }
                if (caseSensitive) return -1;

                shapeName = shapeName.ToLower().Trim();
                for (int a = 0; a < meshShapes.Length; a++)
                {
                    var morph = meshShapes[a];
                    if (morph.IsInvalid) continue;

                    if (!string.IsNullOrWhiteSpace(morph.name) && morph.name.ToLower().Trim() == shapeName) return a;
                }

                return -1;
            }
            public override List<ShapeInfo> GetShapeInfos(List<ShapeInfo> outputList = null)
            {
                if (outputList == null) outputList = new List<ShapeInfo>();
                if (meshShapes != null)
                {
                    foreach (var shape in meshShapes)
                    {
                        if (shape.IsInvalid) continue;
                        outputList.Add(shape);
                    }
                }
                return outputList;
            }

            public override int VertexGroupCount => vertexGroups == null ? 0 : vertexGroups.Length;
            public override VertexGroupInfo GetVertexGroupInfo(int index)
            {
                if (index < 0 || vertexGroups == null || index >= vertexGroups.Length) return default;
                return GetVertexGroupInfoUnsafe(index);
            }
            public override VertexGroupInfo GetVertexGroupInfoUnsafe(int index) => vertexGroups[index];
            public override int IndexOfVertexGroup(string vertexGroupName, bool caseSensitive = false)
            {
                if (vertexGroups == null) return -1;

                for (int a = 0; a < vertexGroups.Length; a++)
                {
                    var vg = vertexGroups[a];
                    if (vg.IsInvalid) continue;

                    if (vg.name == vertexGroupName) return a;
                }
                if (caseSensitive) return -1;

                vertexGroupName = vertexGroupName.ToLower().Trim();
                for (int a = 0; a < vertexGroups.Length; a++)
                {
                    var vg = vertexGroups[a];
                    if (vg.IsInvalid) continue;

                    if (!string.IsNullOrWhiteSpace(vg.name) && vg.name.ToLower().Trim() == vertexGroupName) return a;
                }

                return -1;
            }
            public override List<VertexGroupInfo> GetVertexGroupInfos(List<VertexGroupInfo> outputList = null)
            {
                if (outputList == null) outputList = new List<VertexGroupInfo>();
                if (vertexGroups != null)
                {
                    foreach (var vg in vertexGroups)
                    {
                        if (vg.IsInvalid) continue;
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
                    if (vg.IsInvalid) continue;

                    if (vg.name == name) return a;
                }
                if (caseSensitive) return -1;

                name = name.ToLower().Trim();
                for (int a = 0; a < StandaloneVertexGroupCount; a++)
                {
                    var vg = vertexGroups[a + standaloneGroups.x];
                    if (vg.IsInvalid) continue;

                    if (!string.IsNullOrWhiteSpace(vg.name) && vg.name.ToLower().Trim() == name) return a;
                }

                return -1;
            }
            public override VertexGroupInfo GetStandaloneVertexGroupInfo(int index)
            {
                if (index < 0 || index >= StandaloneVertexGroupCount) return default;
                return vertexGroups[standaloneGroups.x + index];
            }

            public override int IndexOfMuscleGroup(string name, bool caseSensitive = false)
            {
                if (vertexGroups == null) return -1;

                for (int a = 0; a < MuscleVertexGroupCount; a++)
                {
                    var vg = vertexGroups[a + muscleGroups.x];
                    if (vg.IsInvalid) continue;

                    if (vg.name == name) return a;
                }
                if (caseSensitive) return -1;

                name = name.ToLower().Trim();
                for (int a = 0; a < MuscleVertexGroupCount; a++)
                {
                    var vg = vertexGroups[a + muscleGroups.x];
                    if (vg.IsInvalid) continue;

                    if (!string.IsNullOrWhiteSpace(vg.name) && vg.name.ToLower().Trim() == name) return a;
                }

                return -1;
            }
            public override VertexGroupInfo GetMuscleVertexGroupInfo(int index)
            {
                if (index < 0 || index >= MuscleVertexGroupCount) return default;
                return vertexGroups[muscleGroups.x + index];
            }

            public override int IndexOfFatGroup(string name, bool caseSensitive = false)
            {
                if (vertexGroups == null) return -1;

                for (int a = 0; a < FatVertexGroupCount; a++)
                {
                    var vg = vertexGroups[a + fatGroups.x];
                    if (vg.IsInvalid) continue;

                    if (vg.name == name) return a;
                }
                if (caseSensitive) return -1;

                name = name.ToLower().Trim();
                for (int a = 0; a < FatVertexGroupCount; a++)
                {
                    var vg = vertexGroups[a + fatGroups.x];
                    if (vg.IsInvalid) continue;

                    if (!string.IsNullOrWhiteSpace(vg.name) && vg.name.ToLower().Trim() == name) return a;
                }

                return -1;
            }
            public override VertexGroupInfo GetFatVertexGroupInfo(int index)
            {
                if (index < 0 || index >= FatVertexGroupCount) return default;
                return vertexGroups[fatGroups.x + index];
            }

            public override int IndexOfVariationGroup(string name, bool caseSensitive = false)
            {
                if (vertexGroups == null) return -1;

                for (int a = 0; a < VariationVertexGroupCount; a++)
                {
                    var vg = vertexGroups[a + variationGroups.x];
                    if (vg.IsInvalid) continue;

                    if (vg.name == name) return a;
                }
                if (caseSensitive) return -1;

                name = name.ToLower().Trim();
                for (int a = 0; a < VariationVertexGroupCount; a++)
                {
                    var vg = vertexGroups[a + variationGroups.x];
                    if (vg.IsInvalid) continue;

                    if (!string.IsNullOrWhiteSpace(vg.name) && vg.name.ToLower().Trim() == name) return a;
                }

                return -1;
            }
            public override VertexGroupInfo GetVariationVertexGroupInfo(int index)
            {
                if (index < 0 || index >= VariationVertexGroupCount) return default;
                return vertexGroups[index + variationGroups.x];
            }
            public override VertexGroupInfo GetVariationGroupInfo(int index) => GetVariationVertexGroupInfo(index);

            public override int IndexOfStandaloneShape(string name, bool caseSensitive = false)
            {
                if (meshShapes == null) return -1;

                for (int a = 0; a < StandaloneShapesCount; a++)
                {
                    var shape = meshShapes[a + standaloneShapes.x];
                    if (shape.IsInvalid) continue;

                    if (shape.name == name) return a;
                }
                if (caseSensitive) return -1;

                name = name.ToLower().Trim();
                for (int a = 0; a < StandaloneShapesCount; a++)
                {
                    var shape = meshShapes[a + standaloneShapes.x];
                    if (shape.IsInvalid) continue;

                    if (!string.IsNullOrWhiteSpace(shape.name) && shape.name.ToLower().Trim() == name) return a;
                }

                return -1;
            }
            public override ShapeInfo GetStandaloneShapeInfo(int index)
            {
                if (index < 0 || index >= StandaloneShapesCount) return default;
                return meshShapes[standaloneShapes.x + index];
            }

            public override ShapeInfo MassShapeInfo => massShape >= 0 && meshShapes != null ? meshShapes[massShape] : default;
            public override int MassShapeFrameCount => massShape >= 0 && meshShapes != null ? meshShapes[massShape].FrameCount : 0;

            public override ShapeInfo FlexShapeInfo => flexShape >= 0 && meshShapes != null ? meshShapes[flexShape] : default;
            public override int FlexShapeFrameCount => flexShape >= 0 && meshShapes != null ? meshShapes[flexShape].FrameCount : 0;

            public override ShapeInfo FatShapeInfo => fatShape >= 0 && meshShapes != null ? meshShapes[fatShape] : default;
            public override int FatShapeFrameCount => fatShape >= 0 && meshShapes != null ? meshShapes[fatShape].FrameCount : 0;

            public override ShapeInfo FatMuscleBlendShapeInfo => fatMuscleBlendShape >= 0 && meshShapes != null ? meshShapes[fatMuscleBlendShape] : default;
            public override int FatMuscleBlendShapeFrameCount => fatMuscleBlendShape >= 0 && meshShapes != null ? meshShapes[fatMuscleBlendShape].FrameCount : 0;

            public override ShapeInfo BustSizeShapeInfo => bustSizeShape >= 0 && meshShapes != null ? meshShapes[bustSizeShape] : default;
            public override int BustSizeShapeFrameCount => bustSizeShape >= 0 && meshShapes != null ? meshShapes[bustSizeShape].FrameCount : 0;

            public override ShapeInfo BustSizeMuscleShapeInfo => bustSizeMuscleShape >= 0 && meshShapes != null ? meshShapes[bustSizeMuscleShape] : default;
            public override int BustSizeMuscleShapeFrameCount => bustSizeMuscleShape >= 0 && meshShapes != null ? meshShapes[bustSizeMuscleShape].FrameCount : 0;


            public override int IndexOfVariationShape(string name, bool caseSensitive = false)
            {
                if (meshShapes == null) return -1;

                for (int a = 0; a < VariationShapesCount; a++)
                {
                    var shape = meshShapes[a + variationShapes.x];
                    if (shape.IsInvalid) continue;

                    if (shape.name == name) return a;
                }
                if (caseSensitive) return -1;

                name = name.ToLower().Trim();
                for (int a = 0; a < VariationShapesCount; a++)
                {
                    var shape = meshShapes[a + variationShapes.x];
                    if (shape.IsInvalid) continue;

                    if (!string.IsNullOrWhiteSpace(shape.name) && shape.name.ToLower().Trim() == name) return a;
                }

                return -1;
            }
            public override ShapeInfo GetVariationShapeInfo(int index)
            {
                if (index < 0 || index >= VariationShapesCount) return default;
                return meshShapes[variationShapes.x + index];
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
                            if (meshShape.IsInvalid || meshShape.frameWeights == null) continue;

                            for (int frameIndex = 0; frameIndex < meshShape.frameWeights.Length; frameIndex++)
                            {
                                var frame = meshShape.frameWeights[frameIndex];
                                tempFloats.Add(frame);
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
                            if (meshShape.IsInvalid || meshShape.frameWeights == null) continue;

                            tempRanges.Add(new int2(startIndex, meshShape.frameCount));

                            startIndex += meshShape.frameCount;
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

            #region Pre-Caching

            protected Dictionary<string, int> cachedIdToBlendShapeIndexConversions = new Dictionary<string, int>();
            public bool TryGetCachedBlendShapeIndex(string blendShapeName, out int blendShapeIndex)
            {
                return cachedIdToBlendShapeIndexConversions.TryGetValue(blendShapeName, out blendShapeIndex);
            }

            public override bool Precache()
            {
                var mesh = Mesh;
                if (mesh != null)
                {
                    var massShapeInfo = MassShapeInfo;
                    var flexShapeInfo = FlexShapeInfo;
                    for (int a = 0; a < MuscleGroupsCount; a++)
                    {
                        var group = GetMuscleVertexGroupInfo(a);

                        if (massShapeInfo.IsValid)
                        {
                            for (int b = 0; b < massShapeInfo.frameCount; b++)
                            {
                                if (monoGroups)
                                {
                                    var shapeName = GetMuscleMassBlendShapeNameForGroup(group.name, b);
                                    cachedIdToBlendShapeIndexConversions[shapeName] = mesh.GetBlendShapeIndex(shapeName);
                                } 
                                else
                                {
                                    var shapeName = GetMuscleMassBlendShapeNameForGroupLR(group.name, b, true);
                                    cachedIdToBlendShapeIndexConversions[shapeName] = mesh.GetBlendShapeIndex(shapeName);
                                    shapeName = GetMuscleMassBlendShapeNameForGroupLR(group.name, b, false);
                                    cachedIdToBlendShapeIndexConversions[shapeName] = mesh.GetBlendShapeIndex(shapeName);
                                }
                                    
                            }
                        }
                        if (flexShapeInfo.IsValid)
                        {
                            for (int b = 0; b < flexShapeInfo.frameCount; b++)
                            {
                                if (monoGroups)
                                {
                                    var shapeName = GetMuscleFlexBlendShapeNameForGroup(group.name, b);
                                    cachedIdToBlendShapeIndexConversions[shapeName] = mesh.GetBlendShapeIndex(shapeName);
                                }
                                else
                                {
                                    var shapeName = GetMuscleFlexBlendShapeNameForGroupLR(group.name, b, true);
                                    cachedIdToBlendShapeIndexConversions[shapeName] = mesh.GetBlendShapeIndex(shapeName);
                                    shapeName = GetMuscleFlexBlendShapeNameForGroupLR(group.name, b, false);
                                    cachedIdToBlendShapeIndexConversions[shapeName] = mesh.GetBlendShapeIndex(shapeName);
                                }
                            }
                        }
                    }

                    var fatShapeInfo = FatShapeInfo;
                    if (fatShapeInfo.IsValid)
                    {
                        for (int a = 0; a < FatGroupsCount; a++)
                        {
                            var group = GetFatVertexGroupInfo(a);
                            for (int b = 0; b < fatShapeInfo.frameCount; b++)
                            {
                                var shapeName = GetFatBlendShapeNameForGroup(group.name, b);
                                cachedIdToBlendShapeIndexConversions[shapeName] = mesh.GetBlendShapeIndex(shapeName);
                            }
                        }
                    }

                    for(int a = 0; a < VariationShapesCount; a++)
                    {
                        var shape = GetVariationShapeInfo(a);
                        for (int b = 0; b < VariationGroupsCount; b++)
                        {
                            var group = GetVariationGroupInfo(b);
                            for (int c = 0; c < shape.frameCount; c++)
                            {
                                if (monoGroups)
                                {
                                    var shapeName = GetVariationBlendShapeNameForGroup(group.name, shape.name, c);
                                    cachedIdToBlendShapeIndexConversions[shapeName] = mesh.GetBlendShapeIndex(shapeName);
                                }
                                else
                                {
                                    var shapeName = GetVariationBlendShapeNameForGroupLR(group.name, shape.name, c, true);
                                    cachedIdToBlendShapeIndexConversions[shapeName] = mesh.GetBlendShapeIndex(shapeName);
                                    shapeName = GetVariationBlendShapeNameForGroupLR(group.name, shape.name, c, false);
                                    cachedIdToBlendShapeIndexConversions[shapeName] = mesh.GetBlendShapeIndex(shapeName);
                                }
                            }
                        }
                    }
                }

                return base.Precache();
            }

            #endregion

        }

        #endregion

        protected override void OnAwake()
        {
            base.OnAwake();
        }

        public override bool IsInitialized => true;
        public override bool HasValidInstance => true; 

        protected static readonly Dictionary<CustomizableCharacterUnitySkinnedMesh_DATA, List<CustomizableCharacterUnitySkinnedMesh>> allMeshes = new Dictionary<CustomizableCharacterUnitySkinnedMesh_DATA, List<CustomizableCharacterUnitySkinnedMesh>>();
        protected int instanceId = -1;
        public override int InstanceID
        {
            get
            {
                if (instanceId < 0)
                {
                    if (!allMeshes.TryGetValue(data, out var meshes))
                    {
                        meshes = new List<CustomizableCharacterUnitySkinnedMesh>();
                        allMeshes[data] = meshes;
                    }

                    for(int i = 0; i < meshes.Count; i++)
                    {
                        var mesh = meshes[i];
                        if (mesh == null || ReferenceEquals(mesh, this))
                        {
                            instanceId = i;
                            break;
                        }
                    }

                    if (instanceId < 0)
                    {
                        instanceId = meshes.Count;
                        meshes.Add(this);
                    }
                }

                return instanceId;
            }
        }

        #region Data

        [SerializeField]
        protected CustomizableCharacterUnitySkinnedMesh_DATA data;
        protected override bool TrySetData(ICustomizableCharacterMeshBaseData data)
        {
            if (data is not CustomizableCharacterUnitySkinnedMesh_DATA meshData)
            {
                Debug.LogError($"Invalid data type assigned to {name}: {data.GetType().Name} (expected {typeof(CustomizableCharacterUnitySkinnedMesh_DATA).Name})");
                return false;
            }

            var prevData = this.data;
            this.data = meshData;

            if (Application.isPlaying)
            {
                SetupSkinnedMeshSyncs();
                if (enabled) StartRendering();
            }

            return true;
        }
        public CustomizableCharacterUnitySkinnedMesh_DATA Data => data;
        public SerializedData SubData => data == null ? null : data.SerializedData;
        public override ICustomizableCharacterMeshBaseData CustomizationData => SubData;

        #endregion

        #region Rendering

        [NonSerialized]
        protected Material[] materialInstances;
        public override Material[] MaterialInstances
        {
            get
            {
                if (materialInstances == null)
                {
                    materialInstances = new Material[data.MaterialCount];
                    for (int a = 0; a < data.MaterialCount; a++) materialInstances[a] = data.GetMaterial(a) == null ? null : data.GetMaterial(a);
                }

                return materialInstances;
            }
        }

        public override bool CanRender => true;

        protected virtual void ApplyRigTo(SkinnedMeshRenderer smr)
        {
            var meshData = SubData;
            if (smr == null || meshData == null || !meshData.HasBonesArray) return;

            var rigRoot = RigRoot;
            var boneNames = meshData.boneNames;
            var bones = new Transform[boneNames.Length];
            string rootBoneName = avatar == null ? null : avatar.RootBone;
            Transform rootBone = null;
            for (int i = 0; i < boneNames.Length; i++)
            {
                var boneName = boneNames[i];
                var bone = rigRoot.FindDeepChild(boneName, true);
                if (bone == null)
                {
                    Debug.LogError($"Could not find bone '{boneName}' for customizable skinned mesh '{name}'");
                    continue;
                }

                if (bone.name == rootBoneName) rootBone = bone;
                bones[i] = bone;
            }

            smr.bones = bones;
            smr.rootBone = rootBone;
        }
        protected override Renderer[] CreateRenderersForLOD(int lod, MeshLOD meshLOD, Transform renderersRoot, DefaultRenderedMesh defaultRenderedMesh)
        {
            var meshData = SubData;
            var bounds = new Bounds(meshData.boundsCenter, meshData.boundsExtents * 2f); 

            Renderer[] renderers = null;
            if (meshData.renderSets == null || meshData.renderSets.Length <= 1)
            {
                defaultRenderedMesh.meshFilter = null;
                defaultRenderedMesh.meshRenderer = renderersRoot.gameObject.AddComponent<SkinnedMeshRenderer>();
                ((SkinnedMeshRenderer)defaultRenderedMesh.meshRenderer).sharedMesh = meshLOD.mesh;
                defaultRenderedMesh.meshRenderer.sharedMaterials = MaterialInstances;
                defaultRenderedMesh.meshRenderer.localBounds = bounds;
                ApplyRigTo(defaultRenderedMesh.meshRenderer as SkinnedMeshRenderer);

                renderers = new Renderer[] { defaultRenderedMesh.meshRenderer };
            }
            else // render sets render the same mesh instance using different materials (useful for toon outline materials)
            {
                var materials = MaterialInstances;

                renderers = new Renderer[meshData.renderSets.Length];

                MeshFilter[] additionalFilters = new MeshFilter[meshData.renderSets.Length - 1];
                Renderer[] additionalRenderers = new Renderer[additionalFilters.Length];
                for (int b = 0; b < meshData.renderSets.Length; b++)
                {
                    var renderSet = meshData.renderSets[b];

                    var renderSetObj = new GameObject($"set_{b}");
                    renderSetObj.layer = gameObject.layer;
                    renderSetObj.transform.SetParent(renderersRoot.transform, false);

                    SkinnedMeshRenderer meshRenderer;
                    if (b == 0)
                    {
                        defaultRenderedMesh.meshRenderer = meshRenderer = renderSetObj.AddComponent<SkinnedMeshRenderer>();
                    }
                    else
                    {
                        int ind = b - 1;

                        additionalRenderers[ind] = meshRenderer = renderSetObj.AddComponent<SkinnedMeshRenderer>();
                    }

                    meshRenderer.sharedMesh = meshLOD.mesh;
                    var mats = new Material[renderSet.materialCount];
                    for (int c = 0; c < renderSet.materialCount; c++) mats[c] = materials[renderSet.materialIndexStart + c];
                    meshRenderer.sharedMaterials = mats;
                    meshRenderer.localBounds = bounds;
                    ApplyRigTo(meshRenderer);

                    renderers[b] = meshRenderer;
                }

                defaultRenderedMesh.additionalFilters = additionalFilters;
                defaultRenderedMesh.additionalRenderers = additionalRenderers;
            }

            return renderers;
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
        }

        public override void MarkForVariationUpdate()
        {
        }

        public override JobHandle MeshUpdateJobHandle => default;

        protected float[] standaloneShapeWeights;
        protected override void SetStandaloneShapeWeightInternal(int shapeIndex, ref float weight)
        {
            if (standaloneShapeWeights == null) standaloneShapeWeights = new float[CustomizationData.StandaloneShapesCount];
            standaloneShapeWeights[shapeIndex] = weight;
        }
        public override float GetStandaloneShapeWeightUnsafe(int shapeIndex) 
        {
            if (standaloneShapeWeights == null) return 0f;
            return standaloneShapeWeights[shapeIndex];
        }

        protected MuscleDataLR[] muscleDataValues;
        protected override void SetMuscleDataInternal(int groupIndex, ref MuscleDataLR data)
        {
            if (muscleDataValues == null) muscleDataValues = new MuscleDataLR[CustomizationData.MuscleGroupsCount];
            muscleDataValues[groupIndex] = data;
        }
        public override MuscleDataLR GetMuscleDataUnsafe(int groupIndex)
        {
            if (muscleDataValues == null) return default;
            return muscleDataValues[groupIndex];
        }

        protected override void OnSetMuscleData(int groupIndex, MuscleDataLR data)
        {
            var meshData = SubData;
            var groupInfo = meshData.GetMuscleVertexGroupInfo(groupIndex);
            if (groupInfo.IsValid)
            {
                var mass = Mathf.Max(meshData.minMassShapeWeight, data.valuesLeft.mass);
                var flex = data.valuesLeft.flex;

                if (meshData.monoGroups)
                {
                    // Mass
                    if (meshData.MassShapeInfo.IsValid)
                    {
                        for (int i = 0; i < meshData.MassShapeInfo.FrameCount; i++)
                        {
                            var shapeName = GetMuscleMassBlendShapeNameForGroup(groupInfo.name, i);
                            SetBlendShapeWeight(shapeName, 0f);
                        }
                        CalculateShapeFrames(mass, meshData.MassShapeInfo.frameWeights, out int massFrameA, out float massWeightA, out int massFrameB, out float massWeightB);
                        if (massFrameA >= 0)
                        {
                            var shapeName = GetMuscleMassBlendShapeNameForGroup(groupInfo.name, massFrameA);
                            SetBlendShapeWeight(shapeName, massWeightA);
                        }
                        if (massFrameB >= 0 && massFrameB != massFrameA)
                        {
                            var shapeName = GetMuscleMassBlendShapeNameForGroup(groupInfo.name, massFrameB);
                            SetBlendShapeWeight(shapeName, massWeightB);
                        }
                    }

                    // Flex
                    if (meshData.FlexShapeInfo.IsValid)
                    {
                        for (int i = 0; i < meshData.FlexShapeInfo.FrameCount; i++)
                        {
                            var shapeName = GetMuscleFlexBlendShapeNameForGroup(groupInfo.name, i);
                            SetBlendShapeWeight(shapeName, 0f);
                        }
                        CalculateShapeFrames(flex, meshData.FlexShapeInfo.frameWeights, out int flexFrameA, out float flexWeightA, out int flexFrameB, out float flexWeightB);
                        if (flexFrameA >= 0)
                        {
                            var shapeName = GetMuscleFlexBlendShapeNameForGroup(groupInfo.name, flexFrameA);
                            SetBlendShapeWeight(shapeName, flexWeightA);
                        }
                        if (flexFrameB >= 0 && flexFrameB != flexFrameA)
                        {
                            var shapeName = GetMuscleFlexBlendShapeNameForGroup(groupInfo.name, flexFrameB);
                            SetBlendShapeWeight(shapeName, flexWeightB);
                        }
                    }
                }
                else
                {
                    // Mass
                    if (meshData.MassShapeInfo.IsValid)
                    {
                        for (int i = 0; i < meshData.MassShapeInfo.FrameCount; i++)
                        {
                            var shapeName = GetMuscleMassBlendShapeNameForGroupLR(groupInfo.name, i, true);
                            SetBlendShapeWeight(shapeName, 0f);

                            shapeName = GetMuscleMassBlendShapeNameForGroupLR(groupInfo.name, i, false);
                            SetBlendShapeWeight(shapeName, 0f);
                        }
                        CalculateShapeFrames(mass, meshData.MassShapeInfo.frameWeights, out int massFrameAL, out float massWeightAL, out int massFrameBL, out float massWeightBL);
                        if (massFrameAL >= 0)
                        {
                            var shapeName = GetMuscleMassBlendShapeNameForGroupLR(groupInfo.name, massFrameAL, true);
                            SetBlendShapeWeight(shapeName, massWeightAL);
                        }
                        if (massFrameBL >= 0 && massFrameBL != massFrameAL)
                        {
                            var shapeName = GetMuscleMassBlendShapeNameForGroupLR(groupInfo.name, massFrameBL, true);
                            SetBlendShapeWeight(shapeName, massWeightBL);
                        }

                        CalculateShapeFrames(mass, meshData.MassShapeInfo.frameWeights, out int massFrameAR, out float massWeightAR, out int massFrameBR, out float massWeightBR);
                        if (massFrameAR >= 0)
                        {
                            var shapeName = GetMuscleMassBlendShapeNameForGroupLR(groupInfo.name, massFrameAR, false);
                            SetBlendShapeWeight(shapeName, massWeightAR);
                        }
                        if (massFrameBR >= 0 && massFrameBR != massFrameAR)
                        {
                            var shapeName = GetMuscleMassBlendShapeNameForGroupLR(groupInfo.name, massFrameBR, false);
                            SetBlendShapeWeight(shapeName, massWeightBR);
                        }
                    }

                    // Flex
                    if (meshData.FlexShapeInfo.IsValid)
                    {
                        for (int i = 0; i < meshData.FlexShapeInfo.FrameCount; i++)
                        {
                            var shapeName = GetMuscleFlexBlendShapeNameForGroupLR(groupInfo.name, i, true);
                            SetBlendShapeWeight(shapeName, 0f);

                            shapeName = GetMuscleFlexBlendShapeNameForGroupLR(groupInfo.name, i, false);
                            SetBlendShapeWeight(shapeName, 0f);
                        }
                        CalculateShapeFrames(mass, meshData.FlexShapeInfo.frameWeights, out int flexFrameAL, out float flexWeightAL, out int flexFrameBL, out float flexWeightBL);
                        if (flexFrameAL >= 0)
                        {
                            var shapeName = GetMuscleFlexBlendShapeNameForGroupLR(groupInfo.name, flexFrameAL, true);
                            SetBlendShapeWeight(shapeName, flexWeightAL);
                        }
                        if (flexFrameBL >= 0 && flexFrameBL != flexFrameAL)
                        {
                            var shapeName = GetMuscleFlexBlendShapeNameForGroupLR(groupInfo.name, flexFrameBL, true);
                            SetBlendShapeWeight(shapeName, flexWeightBL);
                        }

                        CalculateShapeFrames(flex, meshData.MassShapeInfo.frameWeights, out int flexFrameAR, out float flexWeightAR, out int flexFrameBR, out float flexWeightBR);
                        if (flexFrameAR >= 0)
                        {
                            var shapeName = GetMuscleFlexBlendShapeNameForGroupLR(groupInfo.name, flexFrameAR, false);
                            SetBlendShapeWeight(shapeName, flexWeightAR);
                        }
                        if (flexFrameBR >= 0 && flexFrameBR != flexFrameAR)
                        {
                            var shapeName = GetMuscleFlexBlendShapeNameForGroupLR(groupInfo.name, flexFrameBR, false);
                            SetBlendShapeWeight(shapeName, flexWeightBR);
                        }
                    }
                }
            }
        }

        protected float4[] fatValues;
        protected override void SetFatLevelInternal(int groupIndex, ref float level)
        {
            if (fatValues == null) fatValues = new float4[CustomizationData.FatGroupsCount]; 
            fatValues[groupIndex].x = level;
        }
        public override float GetFatLevelUnsafe(int groupIndex)
        {
            if (fatValues == null) return default;
            return fatValues[groupIndex].x;
        }

        protected override void SetBodyHairLevelInternal(int groupIndex, ref float level, ref float blend)
        {
            if (fatValues == null) fatValues = new float4[CustomizationData.FatGroupsCount];
            fatValues[groupIndex].zw = new float2(level, blend);
        }
        public override float2 GetBodyHairLevelUnsafe(int groupIndex)
        {
            if (fatValues == null) return default;
            return fatValues[groupIndex].zw;
        }

        protected override void OnSetFatLevel(int groupIndex, float level)
        {
            var meshData = SubData;
            var groupInfo = meshData.GetFatVertexGroupInfo(groupIndex);
            if (meshData.FatShapeInfo.IsValid && groupInfo.IsValid) 
            {
                string shapeName;
                for (int i = 0; i < meshData.FatShapeInfo.FrameCount; i++)
                {
                    shapeName = GetFatBlendShapeNameForGroup(groupInfo.name, i);
                    SetBlendShapeWeight(shapeName, 0f);
                }

                CalculateShapeFrames(level, meshData.FatShapeInfo.frameWeights, out int fatFrameA, out float fatWeightA, out int fatFrameB, out float fatWeightB);
                if (fatFrameA >= 0)
                {
                    shapeName = GetFatBlendShapeNameForGroup(groupInfo.name, fatFrameA);
                    SetBlendShapeWeight(shapeName, fatWeightA);
                }
                if (fatFrameB >= 0 && fatFrameB != fatFrameA)
                {
                    shapeName = GetFatBlendShapeNameForGroup(groupInfo.name, fatFrameB);
                    SetBlendShapeWeight(shapeName, fatWeightB);
                }
            }
        }

        protected float2[] variationWeights;
        protected override void SetVariationWeightInternal(int variationShapeIndex, int groupIndex, ref float2 weight)
        {
            if (variationWeights == null) variationWeights = new float2[CustomizationData.VariationShapesControlDataSize];
            int variationIndex = GetPartialVariationShapeIndexUnsafe(groupIndex, variationShapeIndex);
            variationWeights[variationIndex] = weight;
        }
        public override float2 GetVariationWeightUnsafe(int variationShapeIndex, int groupIndex)
        {
            int variationIndex = (groupIndex * CustomizationData.VariationShapesCount) + variationShapeIndex;
            return GetVariationWeightUnsafe(variationIndex); 
        }
        public override float2 GetVariationWeightUnsafe(int indexInArray)
        {
            if (variationWeights == null) return default;
            return variationWeights[indexInArray]; 
        }

        protected override void OnSetVariationWeight(int variationShapeIndex, int groupIndex, float2 weight)
        {
            var meshData = SubData;
            var shapeInfo = meshData.GetVariationShapeInfo(variationShapeIndex);
            var groupInfo = meshData.GetVariationGroupInfo(groupIndex);
            if (shapeInfo.IsValid && groupInfo.IsValid)
            {
                if (meshData.monoGroups)
                {
                    for (int i = 0; i < shapeInfo.FrameCount; i++)
                    {
                        var shapeName = GetVariationBlendShapeNameForGroup(groupInfo.name, shapeInfo.name, i);
                        SetBlendShapeWeight(shapeName, 0f);
                    }

                    CalculateShapeFrames(weight.x, shapeInfo.frameWeights, out int frameA, out float weightA, out int frameB, out float weightB);
                    if (frameA >= 0)
                    {
                        var shapeName = GetVariationBlendShapeNameForGroup(groupInfo.name, shapeInfo.name, frameA);
                        SetBlendShapeWeight(shapeName, weightA);
                    }
                    if (frameB >= 0 && frameB != frameA)
                    {
                        var shapeName = GetVariationBlendShapeNameForGroup(groupInfo.name, shapeInfo.name, frameB);
                        SetBlendShapeWeight(shapeName, weightB);
                    }
                }
                else
                {
                    for (int i = 0; i < shapeInfo.FrameCount; i++)
                    {
                        var shapeName = GetVariationBlendShapeNameForGroupLR(groupInfo.name, shapeInfo.name, i, true);
                        SetBlendShapeWeight(shapeName, 0f);

                        shapeName = GetVariationBlendShapeNameForGroupLR(groupInfo.name, shapeInfo.name, i, false);
                        SetBlendShapeWeight(shapeName, 0f);
                    }

                    CalculateShapeFrames(weight.x, shapeInfo.frameWeights, out int frameAL, out float weightAL, out int frameBL, out float weightBL);
                    if (frameAL >= 0)
                    {
                        var shapeName = GetVariationBlendShapeNameForGroupLR(groupInfo.name, shapeInfo.name, frameAL, true);
                        SetBlendShapeWeight(shapeName, weightAL);
                    }
                    if (frameBL >= 0 && frameBL != frameAL)
                    {
                        var shapeName = GetVariationBlendShapeNameForGroupLR(groupInfo.name, shapeInfo.name, frameBL, true);
                        SetBlendShapeWeight(shapeName, weightBL);
                    }
                    CalculateShapeFrames(weight.y, shapeInfo.frameWeights, out int frameAR, out float weightAR, out int frameBR, out float weightBR);
                    if (frameAR >= 0)
                    {
                        var shapeName = GetVariationBlendShapeNameForGroupLR(groupInfo.name, shapeInfo.name, frameAR, false);
                        SetBlendShapeWeight(shapeName, weightAR);
                    }
                    if (frameBR >= 0 && frameBR != frameAR)
                    {
                        var shapeName = GetVariationBlendShapeNameForGroupLR(groupInfo.name, shapeInfo.name, frameBR, false);
                        SetBlendShapeWeight(shapeName, weightBR);
                    }
                }
            }
        }

        public void SetBlendShapeWeight(string blendShapeName, float weight)
        {
            var meshData = SubData;
            if (meshData.TryGetCachedBlendShapeIndex(blendShapeName, out int blendShapeIndex))
            {
                Debug.Log($"{blendShapeName} was cached {blendShapeIndex}"); 
                SetBlendShapeWeight(blendShapeIndex, weight);
            } 
            else
            {
                if (defaultRenderedMeshes != null)
                {
                    foreach (var renderedMesh in defaultRenderedMeshes)
                    {
                        if (renderedMesh.meshRenderer is SkinnedMeshRenderer smr && smr.sharedMesh != null)
                        {
                            blendShapeIndex = smr.sharedMesh.GetBlendShapeIndex(blendShapeName);
                            if (blendShapeIndex >= 0) smr.SetBlendShapeWeight(blendShapeIndex, weight);
                        }

                        if (renderedMesh.additionalRenderers != null)
                        {
                            foreach (var additionalRenderer in renderedMesh.additionalRenderers)
                            {
                                if (additionalRenderer is SkinnedMeshRenderer additionalSmr && additionalSmr.sharedMesh != null)
                                {
                                    blendShapeIndex = additionalSmr.sharedMesh.GetBlendShapeIndex(blendShapeName);
                                    if (blendShapeIndex >= 0) additionalSmr.SetBlendShapeWeight(blendShapeIndex, weight); 
                                }
                            }
                        }
                    }
                }
            }
        }
        protected void SetBlendShapeWeight(int blendShapeIndex, float weight)
        {
            if (defaultRenderedMeshes != null)
            {
                foreach (var renderedMesh in defaultRenderedMeshes)
                {
                    if (renderedMesh.meshRenderer is SkinnedMeshRenderer smr)
                    {
                        smr.SetBlendShapeWeight(blendShapeIndex, weight);
                    }

                    if (renderedMesh.additionalRenderers != null)
                    {
                        foreach (var additionalRenderer in renderedMesh.additionalRenderers)
                        {
                            if (additionalRenderer is SkinnedMeshRenderer additionalSmr)
                            {
                                additionalSmr.SetBlendShapeWeight(blendShapeIndex, weight);
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region Buffers

        protected override void InitBuffers()
        {
        }

        #endregion

        protected override bool PreRaycastAgainst(ref int lod, ref float3 origin, ref float3 offset) => false;
        protected override bool RaycastAgainstMesh(int lod, float3 origin, float3 offset, out Maths.RaycastHitResult result, float errorMargin = 0.01f)
        {
            result = default;
            return false;
        }

    }

}

#endif