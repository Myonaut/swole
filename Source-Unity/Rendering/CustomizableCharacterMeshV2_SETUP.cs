#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;

using Swole.DataStructures;
using Swole.API.Unity.Animation;

using static Swole.Morphing.MorphUtils;

namespace Swole.Morphing
{

    [ExecuteAlways]
    public class CustomizableCharacterMeshV2_SETUP : MonoBehaviour
    {

        public const string massShapeNameDefault = "MUSCLE_MASS";
        public const string flexShapeNameDefault = "MUSCLE_FLEX";
        public const string fatShapeNameDefault = "FAT";
        public const string fatMuscleBlendShapeNameDefault = "FAT_MUSCLE_BLEND";

        #region Sub Types

        [Serializable]
        public struct BodyMask
        {

            public string name;

            [Tooltip("Will groups using this mask allow control through animation?")]
            public bool animatable;

            public BlendShapeTarget initialShapeTarget;

            public bool isComposite;
            public CompositeVertexGroupBuilder composite;

        }

        [Serializable]
        public struct MeshObject
        {

            public string name;

            public Material[] materials;

            public Mesh mainMesh;

            public MeshLOD[] lods;
            public MeshMergeSlot[] meshesToCombine;

            public CustomAvatar avatar;
            public SkinnedMeshRenderer skinnedRendererReference;

            public Vector3 boundsCenter;
            public Vector3 boundsExtents;

            public bool isBreastMesh;

            public string rigRootName;
            public string rigBufferId;
            public string shapeBufferId;
            public string morphBufferId;

            public string seamShape;
            public string baseMeshGroup;
            public SeamMergeMethod seamMergeMethod;
            public bool mergeSeamTangents;
            public bool mergeSeamUVs;

            public string shapesInstanceParent;
            public string rigInstanceParent;
            public string characterInstanceParent;

            public bool applyInitialOffset;
            public Vector3 initialOffset;
            public Vector3 initialRotationOffset;

            public float solidifyThickness;

        }

        #endregion

        [Header("Control")]
        public bool execute;

        [Header("Asset Creation")]
        public string assetSavePath = "Baked/Meshes";
        public string dataSavePath = "Baked/Customization";
        public string prefabSavePath = "Prefabs";

        public string prefabName;
        [Tooltip("The game object that will be instantiated and have the mesh objects parented to it.")]
        public GameObject prefabRootReference;
        public bool deleteInactiveChildrenInPrefab = true;

        [Header("Meshes")]
        public bool storeNearestVertexInUV = true;
        public bool useUVsToFindClosestVertex = true;
        [Tooltip("The uv channel to use for determining the nearest vertex.")]
        public UVChannelURP nearestVertexUVChannel = UVChannelURP.UV0;
        [Tooltip("The uv channel to store the nearest vertex index in.")]
        public UVChannelURP nearestVertexIndexUVChannel = UVChannelURP.UV3;
        [Tooltip("The uv element to store the nearest vertex index in.")]
        public RGBAChannel nearestVertexIndexElement = RGBAChannel.R;
        protected Vector4 StoreIndexInUV(Vector4 uv, int index)
        {
            switch (nearestVertexIndexElement)
            {
                case RGBAChannel.R:
                    uv.x = index;
                    break;
                case RGBAChannel.G:
                    uv.y = index;
                    break;
                case RGBAChannel.B:
                    uv.z = index;
                    break;
                case RGBAChannel.A:
                    uv.w = index;
                    break;
            }

            return uv;
        }

        public MeshObject[] meshObjects;

        public int IndexOfMeshObject(string targetMeshName)
        {
            if (meshObjects == null || string.IsNullOrWhiteSpace(targetMeshName)) return -1;

            for (int a = 0; a < meshObjects.Length; a++) if (meshObjects[a].name == targetMeshName) return a;
            return -1;
        }

        [Header("Initial Data")]
        public BlendShapeTarget[] standaloneShapes;

        public BlendShapeTarget[] standaloneVertexGroups;
        public CompositeVertexGroupBuilder[] compositeVertexGroups;

        public bool normalizeVertexGroups;
        public float defaultVertexGroupMaxWeight;

        public string midlineVertexGroup;

        public string bustVertexGroup;
        public string bustNerfVertexGroup;
        public string bustSizeShape;

        public string nippleMaskVertexGroup;
        public string genitalMaskVertexGroup;

        public string massShapeName = massShapeNameDefault;
        public string MassShapeName => string.IsNullOrWhiteSpace(massShapeName) ? massShapeNameDefault : massShapeName;

        public string flexShapeName = flexShapeNameDefault;
        public string FlexShapeName => string.IsNullOrWhiteSpace(flexShapeName) ? flexShapeNameDefault : flexShapeName;

        public string fatShapeName = fatShapeNameDefault;
        public string FatShapeName => string.IsNullOrWhiteSpace(fatShapeName) ? fatShapeNameDefault : fatShapeName;

        public string fatMuscleBlendShapeName = fatMuscleBlendShapeNameDefault;
        public string FatMuscleBlendShapeName => string.IsNullOrWhiteSpace(fatMuscleBlendShapeName) ? fatMuscleBlendShapeNameDefault : fatMuscleBlendShapeName;

        [Header("Physique")]
        public BlendShapeTarget[] massShapes;
        public BlendShapeTarget[] flexShapes;
        public BlendShapeTarget[] fatShapes;
        public BlendShapeTarget[] fatMuscleBlendShapes;

        public float defaultMassShapeWeight;
        public float minMassShapeWeight;

        public bool normalizeMuscleGroupsAsPool = true;
        public float normalizeMuscleGroupsAsPoolMaxWeight = 0f;
        public bool averageMuscleGroupsAsPool = true;
        public float averageMuscleGroupsAsPoolMaxWeight = 1f;
        public BodyMask[] muscleGroups;

        public bool normalizeFatGroupsAsPool = true;
        public float normalizeFatGroupsAsPoolMaxWeight = 0f;
        public bool averageFatGroupsAsPool = true;
        public float averageFatGroupsAsPoolMaxWeight = 1f;
        public BodyMask[] fatGroups;

        [Header("Customization")]
        public BlendShapeTarget[] variationShapes;

        public bool normalizeVariationGroupsAsPool = true;
        public float normalizeVariationGroupsAsPoolMaxWeight = 0f;
        public bool averageVariationGroupsAsPool = true;
        public float averageVariationGroupsAsPoolMaxWeight = 1f;
        public BodyMask[] variationGroups;

        [Header("Other")]
        public List<string> nonSeamMergableBlendShapes;

        [Header("Output")]
        public GameObject outputPrefab;
        public List<Mesh> outputMeshes = new List<Mesh>();
        public List<CustomizableCharacterMeshV2_DATA> outputDatas = new List<CustomizableCharacterMeshV2_DATA>();

        #region Utility Fields

        protected List<MeshShape> finalMeshShapes = new List<MeshShape>();
        protected List<VertexGroup> finalVertexGroups = new List<VertexGroup>();
        protected List<CustomizableCharacterMeshV2> finalMeshObjects = new List<CustomizableCharacterMeshV2>();
        protected List<SkinnedMeshRenderer> finalSkinnedRenderers = new List<SkinnedMeshRenderer>();

        protected List<BlendShape> seamShapes = new List<BlendShape>();
        protected List<MeshLOD> meshLODs = new List<MeshLOD>();

        protected List<BlendShapeTarget> tempBlendShapeTargets = new List<BlendShapeTarget>();
        protected List<BlendShape> tempBlendShapes = new List<BlendShape>();
        protected List<VertexGroup> tempVertexGroups = new List<VertexGroup>();
        protected List<VertexGroup> tempVertexGroups2 = new List<VertexGroup>();
        protected List<CompositeVertexGroupBuilder> tempCompositeGroups = new List<CompositeVertexGroupBuilder>();
        protected List<MeshShape> tempMeshShapes = new List<MeshShape>();
        protected List<Matrix4x4> tempMatrices = new List<Matrix4x4>();

        protected Dictionary<int, int> tempIndexRemapper = new Dictionary<int, int>();

        #endregion

        protected void Update()
        {
            if (execute)
            {
                execute = false;
                Execute();
            }
        }

        public virtual void Execute()
        {

            if (outputDatas == null) outputDatas = new List<CustomizableCharacterMeshV2_DATA>();
            outputDatas.Clear();
            if (outputMeshes == null) outputMeshes = new List<Mesh>();
            outputMeshes.Clear();

            #region Utility Field Initialization

            if (finalMeshShapes == null) finalMeshShapes = new List<MeshShape>();
            if (finalVertexGroups == null) finalVertexGroups = new List<VertexGroup>();
            if (finalMeshObjects == null) finalMeshObjects = new List<CustomizableCharacterMeshV2>();
            if (finalSkinnedRenderers == null) finalSkinnedRenderers = new List<SkinnedMeshRenderer>();

            if (seamShapes == null) seamShapes = new List<BlendShape>();
            if (meshLODs == null) meshLODs = new List<MeshLOD>();

            if (tempBlendShapes == null) tempBlendShapes = new List<BlendShape>();
            if (tempBlendShapeTargets == null) tempBlendShapeTargets = new List<BlendShapeTarget>();
            if (tempVertexGroups == null) tempVertexGroups = new List<VertexGroup>();
            if (tempVertexGroups2 == null) tempVertexGroups2 = new List<VertexGroup>();
            if (tempCompositeGroups == null) tempCompositeGroups = new List<CompositeVertexGroupBuilder>();
            if (tempMeshShapes == null) tempMeshShapes = new List<MeshShape>();
            if (tempMatrices == null) tempMatrices = new List<Matrix4x4>();

            if (tempIndexRemapper == null) tempIndexRemapper = new Dictionary<int, int>();

            #endregion

            PreSetup();

            #region Prepare Ouput Prefab

            outputPrefab = Instantiate(prefabRootReference);
            outputPrefab.name = string.IsNullOrWhiteSpace(prefabName) ? prefabRootReference.name : prefabName;
            if (deleteInactiveChildrenInPrefab)
            {
                var children = outputPrefab.GetComponentsInChildren<Transform>(true);
                foreach (var child in children)
                {
                    if (child == null || child.gameObject == outputPrefab) continue;

                    if (!child.gameObject.activeSelf) DestroyImmediate(child.gameObject);
                }
            }

            #endregion

            Vector2Int indicesDefault = new Vector2Int(0, -1);

            if (meshObjects != null)
            {
                List<CustomizableCharacterMeshV2> characterMeshes = new List<CustomizableCharacterMeshV2>();
                for(int objectIndex = 0; objectIndex < meshObjects.Length; objectIndex++)
                {
                    var objectSetup = meshObjects[objectIndex];
                    if (objectSetup.mainMesh == null)
                    {
                        Debug.LogWarning($"Mesh setup '{objectSetup.name}' has no main mesh assigned, skipping...");
                        continue;
                    }

                    int baseMeshIndex = IndexOfMeshObject(objectSetup.baseMeshGroup);

                    #region Initialize Output Data Object

                    string dataName = $"DATA_{objectSetup.name}";
                    CustomizableCharacterMeshV2_DATA outputData = null;
#if UNITY_EDITOR
                    string savePath = Extensions.CreateUnityAssetPathString(dataSavePath, dataName, string.Empty);
                    outputData = AssetDatabase.LoadAssetAtPath<CustomizableCharacterMeshV2_DATA>(savePath);
                    if (outputData == null) outputData = ScriptableObject.CreateInstance<CustomizableCharacterMeshV2_DATA>();
#endif
                    outputData.name = dataName;

                    #endregion

                    var mainMesh = objectSetup.mainMesh;
                    Mesh[] lodMeshes = objectSetup.lods == null ? null : new Mesh[objectSetup.lods.Length];
                    if (lodMeshes != null) for (int b = 0; b < objectSetup.lods.Length; b++) lodMeshes[b] = objectSetup.lods[b].mesh;

                    CustomizableCharacterMeshV2 meshObject = null;
                    CustomizableCharacterMeshV2.SerializedData serializedData = new CustomizableCharacterMeshV2.SerializedData();

                    finalMeshShapes.Clear();
                    finalVertexGroups.Clear();

                    PreMeshObjectSetup(objectIndex, objectSetup, meshObject, ref mainMesh, lodMeshes);

                    #region Combine Specified Meshes

                    if (objectSetup.meshesToCombine != null)
                    {

                        int[] baseMeshTriangles = null;

                        Vector3[] baseMeshVertices = null;
                        Vector3[] baseMeshNormals = null;

                        NativeArray<BoneWeight1> baseMeshBoneWeights = default;
                        NativeArray<byte> baseMeshBonesPerVertex = default;
                        int[] baseMeshBoneWeightStartIndices = null;

                        List<BlendShape> baseMeshBlendShapes = new List<BlendShape>();

                        void UpdateBaseMeshData(Mesh baseMesh)
                        {
                            baseMeshTriangles = mainMesh.triangles;

                            baseMeshVertices = mainMesh.vertices;
                            baseMeshNormals = mainMesh.normals;

                            if (baseMeshBoneWeights.IsCreated) baseMeshBoneWeights.Dispose();
                            if (baseMeshBonesPerVertex.IsCreated) baseMeshBonesPerVertex.Dispose();

                            baseMeshBoneWeights = new NativeArray<BoneWeight1>(mainMesh.GetAllBoneWeights(), Allocator.Persistent);
                            baseMeshBonesPerVertex = new NativeArray<byte>(mainMesh.GetBonesPerVertex(), Allocator.Persistent);
                            baseMeshBoneWeightStartIndices = new int[mainMesh.vertexCount];

                            baseMeshBlendShapes.Clear();
                            baseMeshBlendShapes = mainMesh.GetBlendShapes(baseMeshBlendShapes);

                            if (baseMeshBonesPerVertex.IsCreated)
                            {
                                int i = 0;
                                for (int c = 0; c < baseMeshBonesPerVertex.Length; c++)
                                {
                                    int count = baseMeshBonesPerVertex[c];
                                    baseMeshBoneWeightStartIndices[c] = i;
                                    i += count;
                                }
                            }
                        }

                        UpdateBaseMeshData(mainMesh);

                        foreach (var merger in objectSetup.meshesToCombine)
                        {
                            var meshToCombine = merger.mesh;

                            void CombineWithMeshLOD(int lodSlot)
                            {

                                #region Recalculate Normals/Tangents

                                if (merger.recalculateNormals || merger.recalculateTangents) // recalculate normals/tangents of mesh and its shapes if specified
                                {
                                    var vertices = meshToCombine.vertices;

                                    Mesh tempMesh = GameObject.Instantiate(meshToCombine);

                                    if (merger.recalculateNormals) tempMesh.RecalculateNormals();
                                    if (merger.recalculateTangents) tempMesh.RecalculateTangents();

                                    var normals = tempMesh.normals;
                                    var tangents = tempMesh.tangents;

                                    MeshDataTools.WeldedVertex[] mergedVertices = null;

                                    var shapes = tempMesh.GetBlendShapes();
                                    foreach (var shape in shapes)
                                    {
                                        for (int b = 0; b < shape.frames.Length; b++)
                                        {
                                            var frame = shape.frames[b];

                                            var tempVertices = meshToCombine.vertices;
                                            for (int c = 0; c < tempVertices.Length; c++)
                                            {
                                                tempVertices[c] = vertices[c] + frame.deltaVertices[c];
                                            }

                                            tempMesh.vertices = tempVertices;

                                            if (merger.recalculateNormals)
                                            {
                                                tempMesh.RecalculateNormals();
                                                var tempNormals = tempMesh.normals;

                                                if (mergedVertices == null) mergedVertices = MeshDataTools.WeldVertices(vertices);
                                                for (int d = 0; d < mergedVertices.Length; d++)
                                                {
                                                    var mv = mergedVertices[d];
                                                    if (d != mv.firstIndex) continue;

                                                    var normal = Vector3.zero;
                                                    for (int e = 0; e < mv.indices.Count; e++) normal = normal + tempNormals[mv.indices[e]];

                                                    normal = normal.normalized;
                                                    for (int e = 0; e < mv.indices.Count; e++) tempNormals[mv.indices[e]] = normal;
                                                }

                                                for (int c = 0; c < tempVertices.Length; c++) frame.deltaNormals[c] = tempNormals[c] - normals[c];
                                            }

                                            if (merger.recalculateTangents)
                                            {
                                                tempMesh.RecalculateTangents();
                                                var tempTangents = tempMesh.tangents;

                                                for (int c = 0; c < tempVertices.Length; c++) frame.deltaTangents[c] = tempTangents[c] - tangents[c];
                                            }
                                        }
                                    }

                                    meshToCombine = MeshUtils.DuplicateMesh(meshToCombine);
                                    meshToCombine.name = merger.mesh.name;

                                    if (merger.recalculateNormals) meshToCombine.normals = normals;
                                    if (merger.recalculateTangents) meshToCombine.tangents = tangents;

                                    meshToCombine.ClearBlendShapes();
                                    foreach (var shape in shapes) shape.AddToMesh(meshToCombine);
                                }

                                #endregion

                                #region Transfer Surface Data

                                if (merger.allowSurfaceDataTransfer)
                                {

                                    var mesh = meshToCombine;

                                    Quaternion alignmentRotationOffset = Quaternion.Euler(merger.alignmentRotationOffset);

                                    List<string> originalBlendShapes = new List<string>();
                                    List<BlendShape> blendShapes = meshToCombine.GetBlendShapes();
                                    foreach (var shape in blendShapes) originalBlendShapes.Add(shape.name);

                                    bool IsOriginalBlendShape(string name)
                                    {
                                        foreach (var shapeName in originalBlendShapes) if (shapeName == name) return true;
                                        return false;
                                    }
                                    BlendShape AddOrGetBlendShape(string name, BlendShape.Frame[] referenceFrames)
                                    {
                                        foreach (var shape in blendShapes)
                                        {
                                            if (shape.name == name)
                                            {
                                                // if the shape has no frames, add a default frame
                                                if (shape.frames == null || shape.frames.Length <= 0) shape.AddFrame(referenceFrames == null || referenceFrames.Length <= 0 ? 1 : referenceFrames[0].weight, new Vector3[mesh.vertexCount], new Vector3[mesh.vertexCount], new Vector3[mesh.vertexCount]);

                                                return shape;
                                            }
                                        }

                                        // if the shape does not exist, create it
                                        BlendShape newShape = new BlendShape(name, referenceFrames, mesh.vertexCount, false);
                                        blendShapes.Add(newShape);

                                        return newShape;
                                    }

                                    List<MeshDataTools.MeshIsland> meshIslands = null;
                                    int[] meshIslandBindings = null;
                                    ContainingTri[] meshIslandTriBindings = null; // stores the triangle from the base mesh that each island is bound to
                                    if (merger.treatAsMeshIslands) meshIslands = MeshDataTools.CalculateMeshIslands(mesh);

                                    Vector3[] localVertices = mesh.vertices;
                                    for (int c = 0; c < localVertices.Length; c++) localVertices[c] = (alignmentRotationOffset * localVertices[c]) + merger.alignmentOffset;

                                    Vector3[] localNormals = mesh.normals;

                                    Dictionary<int2, float> newBoneWeights = new Dictionary<int2, float>();

                                    if (meshIslands != null)
                                    {
                                        meshIslandBindings = new int[localVertices.Length];
                                        for (int c = 0; c < meshIslandBindings.Length; c++) meshIslandBindings[c] = -1;

                                        meshIslandTriBindings = new ContainingTri[meshIslands.Count];

                                        for (int c = 0; c < meshIslands.Count; c++)
                                        {

                                            var island = meshIslands[c];

                                            if (island.vertices == null || island.vertices.Length <= 0) continue;

                                            Vector3 localVertex;

                                            float closestDistance = float.MaxValue;
                                            int closestLocalIndex = -1;

                                            for (int d = 0; d < island.vertices.Length; d++)
                                            {
                                                float distanceWeight = 1; // TODO: Add possible distance weighting based on position in the mesh island or vertex group or vertex colors

                                                int localIndex = island.vertices[d];
                                                localVertex = localVertices[localIndex];

                                                meshIslandBindings[localIndex] = c;

                                                foreach (var v in baseMeshVertices)
                                                {
                                                    float dista = (v - localVertex).sqrMagnitude * distanceWeight;
                                                    if (dista < closestDistance)
                                                    {
                                                        closestLocalIndex = d;
                                                        closestDistance = dista;
                                                    }
                                                }
                                            }

                                            island.originIndex = closestLocalIndex;

                                            closestDistance = float.MaxValue;
                                            closestLocalIndex = -1;
                                            int closestIndex0 = -1;
                                            int closestIndex1 = -1;
                                            int closestIndex2 = -1;
                                            float closestWeight0 = 0;
                                            float closestWeight1 = 0;
                                            float closestWeight2 = 0;

                                            localVertex = localVertices[island.vertices[island.originIndex]];

                                            if (MeshDataTools.GetClosestContainingTriangle(baseMeshVertices, baseMeshTriangles, localVertex, out var index0, out var index1, out var index2, out var weight0, out var weight1, out var weight2, out var dist, 0.5f, 0.2f))
                                            {
                                                if (dist < closestDistance)
                                                {
                                                    closestDistance = dist;
                                                    closestIndex0 = index0;
                                                    closestIndex1 = index1;
                                                    closestIndex2 = index2;
                                                    closestWeight0 = weight0;
                                                    closestWeight1 = weight1;
                                                    closestWeight2 = weight2;
                                                }
                                            }

                                            meshIslandTriBindings[c] = new ContainingTri(0, closestIndex0, closestIndex1, closestIndex2, closestWeight0, closestWeight1, closestWeight2);
                                        }
                                    }

                                    for (int vIndex = 0; vIndex < localVertices.Length; vIndex++)
                                    {
                                        var localVertex = localVertices[vIndex];
                                        var centerPoint = localVertex;

                                        int meshIslandIndex = meshIslandBindings == null ? -1 : meshIslandBindings[vIndex];
                                        var meshIsland = meshIslandIndex >= 0 ? meshIslands[meshIslandIndex] : default;
                                        var meshIslandTri = meshIslandIndex >= 0 ? meshIslandTriBindings[meshIslandIndex] : default;
                                        if (meshIslandIndex >= 0) centerPoint = localVertices[meshIsland.OriginVertex];

                                        Vector3 originOffset = localVertex - centerPoint;
                                        float originOffsetDist = originOffset.magnitude;

                                        bool hasOrigin = false;
                                        if (originOffsetDist > 0.0001f)
                                        {
                                            originOffset = originOffset / originOffsetDist;
                                            hasOrigin = true;
                                        }

                                        float closestDistance = float.MaxValue;
                                        int closestIndex0 = -1;
                                        int closestIndex1 = -1;
                                        int closestIndex2 = -1;
                                        float closestWeight0 = 0;
                                        float closestWeight1 = 0;
                                        float closestWeight2 = 0;

                                        if (meshIslandIndex < 0)
                                        {

                                            if (MeshDataTools.GetClosestContainingTriangle(baseMeshVertices, baseMeshTriangles, localVertex, out var index0, out var index1, out var index2, out var weight0, out var weight1, out var weight2, out var dist, 0.5f, 0.2f))
                                            {
                                                if (dist < closestDistance)
                                                {
                                                    closestDistance = dist;
                                                    closestIndex0 = index0;
                                                    closestIndex1 = index1;
                                                    closestIndex2 = index2;
                                                    closestWeight0 = weight0;
                                                    closestWeight1 = weight1;
                                                    closestWeight2 = weight2;
                                                }
                                            }

                                        }
                                        else
                                        {
                                            closestIndex0 = meshIslandTri.indexA;
                                            closestIndex1 = meshIslandTri.indexB;
                                            closestIndex2 = meshIslandTri.indexC;
                                            closestWeight0 = meshIslandTri.weightA;
                                            closestWeight1 = meshIslandTri.weightB;
                                            closestWeight2 = meshIslandTri.weightC;
                                        }

                                        if (closestIndex0 >= 0 && closestIndex1 >= 0 && closestIndex2 >= 0)
                                        {
                                            if (merger.transferBlendShapes)
                                            {
                                                var dependencyNormal = ((baseMeshNormals[closestIndex0] * closestWeight0) + (baseMeshNormals[closestIndex1] * closestWeight1) + (baseMeshNormals[closestIndex2] * closestWeight2)).normalized;

                                                // basically if the surface that a vertex is bound to changes normal direction, rotate the vertex around the center point accordingly
                                                Vector3 AddNormalBasedRotationToDelta(Vector3 deltaVertex, Vector3 deltaNormal)
                                                {
                                                    if (hasOrigin)
                                                    {
                                                        Quaternion rotOffset = Quaternion.FromToRotation(dependencyNormal, (dependencyNormal + deltaNormal).normalized);
                                                        deltaVertex = deltaVertex + ((centerPoint + (rotOffset * originOffset) * originOffsetDist) - localVertex);
                                                    }

                                                    return deltaVertex;
                                                }

                                                for (int d = 0; d < baseMeshBlendShapes.Count; d++)
                                                {
                                                    var shape = baseMeshBlendShapes[d];
                                                    if (shape != null && (!merger.preserveExistingBlendShapeData || !IsOriginalBlendShape(shape.name)))
                                                    {
                                                        var blendShape = AddOrGetBlendShape(shape.name, shape.frames);
                                                        if (blendShape != null)
                                                        {
                                                            for (int e = 0; e < blendShape.frames.Length; e++)
                                                            {
                                                                var frame = blendShape.frames[0];
                                                                var baseFrame = shape.frames[e];

                                                                var deltaVertex = (baseFrame.deltaVertices[closestIndex0] * closestWeight0) + (baseFrame.deltaVertices[closestIndex1] * closestWeight1) + (baseFrame.deltaVertices[closestIndex2] * closestWeight2);
                                                                var deltaNormal = (baseFrame.deltaNormals[closestIndex0] * closestWeight0) + (baseFrame.deltaNormals[closestIndex1] * closestWeight1) + (baseFrame.deltaNormals[closestIndex2] * closestWeight2);
                                                                var deltaTangent = (baseFrame.deltaTangents[closestIndex0] * closestWeight0) + (baseFrame.deltaTangents[closestIndex1] * closestWeight1) + (baseFrame.deltaTangents[closestIndex2] * closestWeight2);

                                                                deltaVertex = AddNormalBasedRotationToDelta(deltaVertex, deltaNormal);

                                                                frame.deltaVertices[vIndex] = deltaVertex;
                                                                frame.deltaNormals[vIndex] = deltaNormal;
                                                                frame.deltaTangents[vIndex] = deltaTangent;
                                                            }
                                                        }
                                                    }
                                                }
                                            }

                                            if (merger.transferNormals)
                                            {
                                                localNormals[vIndex] = Vector3.LerpUnclamped(localNormals[vIndex], (baseMeshNormals[closestIndex0] * closestWeight0) + (baseMeshNormals[closestIndex1] * closestWeight1) + (baseMeshNormals[closestIndex2] * closestWeight2), merger.transferNormalsWeight).normalized;

                                                for (int d = 0; d < baseMeshBlendShapes.Count; d++)
                                                {
                                                    var shape = baseMeshBlendShapes[d];
                                                    if (shape != null)
                                                    {
                                                        var blendShape = AddOrGetBlendShape(shape.name, shape.frames);
                                                        if (blendShape != null)
                                                        {
                                                            for (int e = 0; e < blendShape.frames.Length; e++)
                                                            {
                                                                var frame = blendShape.frames[0];
                                                                var baseFrame = shape.frames[e];

                                                                var deltaNormal = (baseFrame.deltaNormals[closestIndex0] * closestWeight0) + (baseFrame.deltaNormals[closestIndex1] * closestWeight1) + (baseFrame.deltaNormals[closestIndex2] * closestWeight2);

                                                                Vector3.LerpUnclamped(frame.deltaNormals[vIndex], deltaNormal, merger.transferNormalsWeight);

                                                                frame.deltaNormals[vIndex] = deltaNormal;
                                                            }
                                                        }
                                                    }
                                                }
                                            }

                                            if (merger.transferBoneWeights)
                                            {
                                                var boneWeights = baseMeshBoneWeights;
                                                var bonesPerVertex = baseMeshBonesPerVertex;
                                                var boneWeightStartIndices = baseMeshBoneWeightStartIndices;

                                                if (boneWeights.IsCreated && bonesPerVertex.IsCreated && boneWeightStartIndices != null)
                                                {
                                                    int startIndex = boneWeightStartIndices[closestIndex0];
                                                    int count = bonesPerVertex[closestIndex0];
                                                    for (int d = 0; d < count; d++)
                                                    {
                                                        int index = startIndex + d;
                                                        var bw = boneWeights[index];

                                                        int2 i = new int2(vIndex, bw.boneIndex);

                                                        newBoneWeights.TryGetValue(i, out float currentWeight);
                                                        newBoneWeights[i] = currentWeight + (bw.weight * closestWeight0);
                                                    }

                                                    startIndex = boneWeightStartIndices[closestIndex1];
                                                    count = bonesPerVertex[closestIndex1];
                                                    for (int d = 0; d < count; d++)
                                                    {
                                                        int index = startIndex + d;
                                                        var bw = boneWeights[index];

                                                        int2 i = new int2(vIndex, bw.boneIndex);

                                                        newBoneWeights.TryGetValue(i, out float currentWeight);
                                                        newBoneWeights[i] = currentWeight + (bw.weight * closestWeight1);
                                                    }

                                                    startIndex = boneWeightStartIndices[closestIndex2];
                                                    count = bonesPerVertex[closestIndex2];
                                                    for (int d = 0; d < count; d++)
                                                    {
                                                        int index = startIndex + d;
                                                        var bw = boneWeights[index];

                                                        int2 i = new int2(vIndex, bw.boneIndex);

                                                        newBoneWeights.TryGetValue(i, out float currentWeight);
                                                        newBoneWeights[i] = currentWeight + (bw.weight * closestWeight2);
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    if (merger.transferBlendShapes || merger.transferNormals)
                                    {
                                        meshToCombine.ClearBlendShapes();
                                        foreach (var shape in blendShapes) shape.AddToMesh(meshToCombine);
                                    }

                                    if (merger.transferNormals)
                                    {
                                        mesh.normals = localNormals;
                                    }

                                    if (merger.transferBoneWeights && newBoneWeights.Count > 0)
                                    {
                                        var boneWeights = new NativeList<BoneWeight1>(meshToCombine.vertexCount, Allocator.Persistent);
                                        var bonesPerVertex = new NativeArray<byte>(meshToCombine.vertexCount, Allocator.Persistent);

                                        List<BoneWeight1> vertexBoneWeights = new List<BoneWeight1>();
                                        for (int c = 0; c < meshToCombine.vertexCount; c++)
                                        {
                                            vertexBoneWeights.Clear();
                                            foreach (var pair in newBoneWeights)
                                            {
                                                if (pair.Key.x == c)
                                                {
                                                    var bw = new BoneWeight1();
                                                    bw.boneIndex = pair.Key.y;
                                                    bw.weight = pair.Value;

                                                    vertexBoneWeights.Add(bw);
                                                }
                                            }
                                            vertexBoneWeights.Sort((BoneWeight1 a, BoneWeight1 b) => (int)Mathf.Sign(b.weight - a.weight));
                                            if (vertexBoneWeights.Count > 8) vertexBoneWeights.RemoveRange(8, vertexBoneWeights.Count - 8);

                                            float totalWeight = 0;
                                            foreach (var bw in vertexBoneWeights) totalWeight += bw.weight;
                                            if (totalWeight > 0)
                                            {
                                                for (int d = 0; d < vertexBoneWeights.Count; d++)
                                                {
                                                    var bw = vertexBoneWeights[d];
                                                    bw.weight /= totalWeight;
                                                    vertexBoneWeights[d] = bw;
                                                }
                                            }

                                            for (int d = 0; d < vertexBoneWeights.Count; d++)
                                            {
                                                var bw = vertexBoneWeights[d];
                                                boneWeights.Add(bw);
                                            }
                                            bonesPerVertex[c] = (byte)vertexBoneWeights.Count;
                                        }

                                        meshToCombine.SetBoneWeights(bonesPerVertex, boneWeights.AsArray());

                                        boneWeights.Dispose();
                                        bonesPerVertex.Dispose();
                                    }

                                }

                                #endregion

                                if (lodSlot == 0)
                                {
                                    string meshName = mainMesh.name;
                                    var mergeMesh = MergeMeshSeam(meshToCombine, mainMesh, objectSetup.seamShape, objectSetup.seamMergeMethod, out _, true, true, objectSetup.mergeSeamTangents, objectSetup.mergeSeamUVs, nonSeamMergableBlendShapes);
                                    mainMesh = MeshDataTools.CombineMeshes(mainMesh, new Mesh[] { mergeMesh });
                                    mainMesh.name = meshName;

                                    if (merger.updateBaseMeshData)
                                    {
                                        UpdateBaseMeshData(mainMesh);
                                    }
                                }
                                else if (lodMeshes != null && lodSlot - 1 >= 0 && lodSlot - 1 < lodMeshes.Length)
                                {
                                    int slot = lodSlot - 1;

                                    string meshName = lodMeshes[slot].name;
                                    var mergeMesh = MergeMeshSeam(meshToCombine, lodMeshes[slot], objectSetup.seamShape, objectSetup.seamMergeMethod, out _, true, true, objectSetup.mergeSeamTangents, objectSetup.mergeSeamUVs, nonSeamMergableBlendShapes);
                                    lodMeshes[slot] = MeshDataTools.CombineMeshes(lodMeshes[slot], new Mesh[] { mergeMesh });
                                    lodMeshes[slot].name = meshName;
                                }
                            }

                            if (merger.lodSlot >= 0) CombineWithMeshLOD(merger.lodSlot);

                            if (merger.additionalLodSlots != null)
                            {
                                for(int l = 0; l < merger.additionalLodSlots.Length; l++) CombineWithMeshLOD(merger.additionalLodSlots[l]);
                            }
                        }

                        if (baseMeshBoneWeights.IsCreated) baseMeshBoneWeights.Dispose();
                        if (baseMeshBonesPerVertex.IsCreated) baseMeshBonesPerVertex.Dispose();
                    }

                    #endregion

                    var rendererBoundsCenter = objectSetup.boundsCenter;
                    var rendererBoundsExtents = objectSetup.boundsExtents;
                    var boundsCenter = rendererBoundsCenter;
                    var boundsExtents = rendererBoundsExtents;

                    #region Apply Initial Offsets

                    if (objectSetup.applyInitialOffset)
                    {
                        Quaternion offsetRot = Quaternion.Euler(objectSetup.initialRotationOffset);
                        Mesh ApplyInitialOffsetToMesh(Mesh mesh)
                        {
                            if (mesh == null) return null;

                            var meshOut = MeshUtils.DuplicateMesh(mesh);
                            meshOut.name = mesh.name;

                            var verts = meshOut.vertices;
                            var norms = meshOut.normals;
                            var tans = meshOut.tangents;
                            var shaps = meshOut.GetBlendShapes();

                            for (int i = 0; i < meshOut.vertexCount; i++)
                            {
                                verts[i] = (offsetRot * verts[i]) + objectSetup.initialOffset;
                                if (i < norms.Length) norms[i] = offsetRot * norms[i];
                                if (i < tans.Length) tans[i] = new float4(math.rotate(offsetRot, new float3(tans[i].x, tans[i].y, tans[i].z)), tans[i].w);

                                for (int j = 0; j < shaps.Count; j++)
                                {
                                    var shape = shaps[j];
                                    if (shape == null || shape.frames == null) continue;

                                    for (int k = 0; k < shape.frames.Length; k++)
                                    {
                                        var frame = shape.frames[k];

                                        frame.deltaVertices[i] = offsetRot * frame.deltaVertices[i];
                                        frame.deltaNormals[i] = offsetRot * frame.deltaNormals[i];
                                        frame.deltaTangents[i] = offsetRot * frame.deltaTangents[i];
                                    }
                                }
                            }

                            meshOut.vertices = verts;
                            meshOut.normals = norms;
                            meshOut.tangents = tans;

                            meshOut.ClearBlendShapes();
                            foreach (var shape in shaps) shape.AddToMesh(meshOut);

                            meshOut.RecalculateBounds();
                            return meshOut;
                        }

                        mainMesh = ApplyInitialOffsetToMesh(mainMesh);
                        if (lodMeshes != null)
                        {
                            for (int l = 0; l < lodMeshes.Length; l++) lodMeshes[l] = ApplyInitialOffsetToMesh(lodMeshes[l]);
                        }

                        boundsCenter = (offsetRot * boundsCenter) + objectSetup.initialOffset;
                        boundsExtents = offsetRot * boundsExtents;
                    }

                    #endregion

                    #region Merge Seams

                    seamShapes.Clear();
                    if (!string.IsNullOrWhiteSpace(objectSetup.seamShape) && baseMeshIndex >= 0)
                    {
                        mainMesh = MergeMeshSeam(mainMesh, meshObjects[baseMeshIndex].mainMesh, objectSetup.seamShape, objectSetup.seamMergeMethod, out var seamShape, true, true, objectSetup.mergeSeamTangents, objectSetup.mergeSeamUVs, nonSeamMergableBlendShapes);
                        seamShapes.Add(seamShape);

                        for (int b = 0; b < lodMeshes.Length; b++)
                        {
                            if (b >= meshObjects[baseMeshIndex].lods.Length) continue; // base mesh doesn't have the same amount of lods

                            lodMeshes[b] = MergeMeshSeam(lodMeshes[b], meshObjects[baseMeshIndex].lods[b].mesh, objectSetup.seamShape, objectSetup.seamMergeMethod, out seamShape, true, true, objectSetup.mergeSeamTangents, objectSetup.mergeSeamUVs, nonSeamMergableBlendShapes);
                            seamShapes.Add(seamShape);
                        }
                    }

                    #endregion

                    #region Solidify

                    if (objectSetup.solidifyThickness > 0.00001f)
                    {
                        var solidifiedMesh = MeshDataTools.Solidify(mainMesh, objectSetup.solidifyThickness, 0f, true, true);
                        solidifiedMesh.name = mainMesh.name;
                        mainMesh = solidifiedMesh;
                        Debug.Log($"SOLIDIFIED: {objectSetup.name} : {objectSetup.solidifyThickness} : {solidifiedMesh.name} :: {solidifiedMesh.GetType().FullName}");
                    }

                    #endregion

                    var mainVertices = mainMesh.vertices;
                    var mainNormals = mainMesh.normals;
                    var mainTangents = mainMesh.tangents;

                    var emptyVertices = new Vector3[mainMesh.vertexCount];
                    var emptyNormals = new Vector3[mainMesh.vertexCount];
                    var emptyTangents = new Vector3[mainMesh.vertexCount];

                    Transform[] bones = null;
                    if (objectSetup.skinnedRendererReference != null)
                    {
                        bones = objectSetup.skinnedRendererReference.bones;
                    }

                    #region Bindpose

                    if (objectSetup.avatar != null)
                    {
                        if (bones != null)
                        {
                            for(int originalBoneIndex = 0; originalBoneIndex < bones.Length; originalBoneIndex++)
                            {
                                var bone = bones[originalBoneIndex];
                                var boneIndex = objectSetup.avatar.GetBoneIndex(bone.name);
                                if (boneIndex >= 0) tempIndexRemapper[originalBoneIndex] = boneIndex;
                            }
                        }
                    }

                    tempMatrices.Clear();
                    if (objectSetup.skinnedRendererReference == null || objectSetup.avatar == null)
                    {
                        tempMatrices.AddRange(mainMesh.bindposes);
                    }
                    else
                    {
                        for (int i = 0; i < objectSetup.avatar.SkinnedBonesCount; i++) tempMatrices.Add(Matrix4x4.identity);

                        var originalBindPoses = mainMesh.bindposes;
                        foreach(var entry in tempIndexRemapper)
                        {
                            tempMatrices[entry.Value] = originalBindPoses[entry.Key];
                        }
                    }

                    #endregion

                    #region Extract Bone Weights

                    var mainBoneWeights = new BoneWeight8[mainVertices.Length];

                    using (var tempBoneWeights = new NativeArray<BoneWeight1>(mainMesh.GetAllBoneWeights(), Allocator.Persistent))
                    {
                        using (var tempBoneCounts = new NativeArray<byte>(mainMesh.GetBonesPerVertex(), Allocator.Persistent))
                        {

                            int boneWeightIndex = 0;
                            for (int b = 0; b < mainBoneWeights.Length; b++)
                            {
                                var count = tempBoneCounts[b];
                                BoneWeight8 bw = new BoneWeight8();
                                for (int i = 0; i < count; i++)
                                {
                                    if (i < 8)
                                    {
                                        var sourceBW = tempBoneWeights[boneWeightIndex];
                                        if (tempIndexRemapper.Count > 0)
                                        {
                                            if (tempIndexRemapper.TryGetValue(sourceBW.boneIndex, out int newBoneIndex))
                                            {
                                                sourceBW.boneIndex = newBoneIndex;
                                            } 
                                            else
                                            {
                                                sourceBW.boneIndex = 0;
                                            }
                                        }
                                        bw = bw.Modify(i, sourceBW.boneIndex, sourceBW.weight);
                                    }

                                    boneWeightIndex++;
                                }

                                mainBoneWeights[b] = bw;
                            }
                        }
                    }

                    #endregion

                    var mainWeld = MeshDataTools.WeldVertices(mainVertices);

                    #region Create Mesh Shapes

                    if (standaloneShapes != null)
                    {
                        tempBlendShapeTargets.Clear();
                        tempBlendShapeTargets.AddRange(standaloneShapes);
                        tempBlendShapeTargets.Sort(MorphUtils.CompareBlendShapeTargetsByWeight);

                        foreach (var shape in tempBlendShapeTargets)
                        {
                            if (shape.TryGetBlendShape(objectSetup.name, mainMesh, out var blendShape, finalMeshShapes, mainVertices, mainNormals, mainTangents, mainWeld))
                            {
                                var meshShape = MeshShape.CreateFromBlendShape(blendShape);
                                meshShape.animatable = shape.animatable;
                                finalMeshShapes.Add(meshShape);
                            }
                        }
                    }

                    if (massShapes != null)
                    {
                        tempBlendShapeTargets.Clear();
                        tempBlendShapeTargets.AddRange(massShapes);
                        tempBlendShapeTargets.Sort(MorphUtils.CompareBlendShapeTargetsByWeight);

                        tempBlendShapes.Clear();
                        foreach (var shape in tempBlendShapeTargets)
                        {
                            Debug.Log($"Processing mass shape '{shape.targetName}' for mesh '{objectSetup.name}'...");
                            if (shape.TryGetBlendShape(objectSetup.name, mainMesh, out var blendShape, finalMeshShapes, mainVertices, mainNormals, mainTangents, mainWeld))
                            {
                                Debug.Log($" - Successfully extracted blend shape '{blendShape.name}' for mass shape.");
                                if (shape.weight > 0f) blendShape.RemapFrameWeights(shape.weight);
                                tempBlendShapes.Add(blendShape);
                            } 
                            else
                            {
                                Debug.Log($" - Created empty blend shape '{shape.targetName}' for mass shape.");  
                                blendShape = new BlendShape(shape.targetName);
                                blendShape.AddFrame(shape.weight, emptyVertices, emptyNormals, emptyTangents);
                                tempBlendShapes.Add(blendShape);
                            }
                        }

                        var massShape = MeshShape.CreateFromBlendShapes(massShapeName, tempBlendShapes, true);
                        serializedData.massShape = finalMeshShapes.Count;
                        finalMeshShapes.Add(massShape);
                    }
                    if (flexShapes != null)
                    {
                        tempBlendShapeTargets.Clear();
                        tempBlendShapeTargets.AddRange(flexShapes);
                        tempBlendShapeTargets.Sort(MorphUtils.CompareBlendShapeTargetsByWeight);

                        tempBlendShapes.Clear();
                        foreach (var shape in tempBlendShapeTargets)
                        {
                            if (shape.TryGetBlendShape(objectSetup.name, mainMesh, out var blendShape, finalMeshShapes, mainVertices, mainNormals, mainTangents, mainWeld))
                            {
                                if (shape.weight > 0f) blendShape.RemapFrameWeights(shape.weight);
                                tempBlendShapes.Add(blendShape);
                            }
                        }

                        var flexShape = MeshShape.CreateFromBlendShapes(flexShapeName, tempBlendShapes, true);
                        serializedData.flexShape = finalMeshShapes.Count;
                        finalMeshShapes.Add(flexShape);
                    }
                    if (fatShapes != null)
                    {
                        tempBlendShapeTargets.Clear();
                        tempBlendShapeTargets.AddRange(fatShapes);
                        tempBlendShapeTargets.Sort(MorphUtils.CompareBlendShapeTargetsByWeight);

                        tempBlendShapes.Clear();
                        foreach (var shape in tempBlendShapeTargets)
                        {
                            if (shape.TryGetBlendShape(objectSetup.name, mainMesh, out var blendShape, finalMeshShapes, mainVertices, mainNormals, mainTangents, mainWeld))
                            {
                                if (shape.weight > 0f) blendShape.RemapFrameWeights(shape.weight);
                                tempBlendShapes.Add(blendShape);
                            }
                        }

                        var fatShape = MeshShape.CreateFromBlendShapes(fatShapeName, tempBlendShapes, true);
                        serializedData.fatShape = finalMeshShapes.Count;
                        finalMeshShapes.Add(fatShape);
                    }
                    if (fatMuscleBlendShapes != null)
                    {
                        tempBlendShapeTargets.Clear();
                        tempBlendShapeTargets.AddRange(fatMuscleBlendShapes);
                        tempBlendShapeTargets.Sort(MorphUtils.CompareBlendShapeTargetsByWeight);

                        tempBlendShapes.Clear();
                        foreach (var shape in tempBlendShapeTargets)
                        {
                            if (shape.TryGetBlendShape(objectSetup.name, mainMesh, out var blendShape, finalMeshShapes, mainVertices, mainNormals, mainTangents, mainWeld))
                            {
                                if (shape.weight > 0f) blendShape.RemapFrameWeights(shape.weight);
                                tempBlendShapes.Add(blendShape);
                            }
                        }

                        var fatMuscleBlendShape = MeshShape.CreateFromBlendShapes(fatMuscleBlendShapeName, tempBlendShapes, true);
                        serializedData.fatMuscleBlendShape = finalMeshShapes.Count;
                        finalMeshShapes.Add(fatMuscleBlendShape);
                    }

                    #endregion

                    #region Merge Mesh Shapes At Seam

                    if (baseMeshIndex >= 0 && baseMeshIndex < objectIndex && seamShapes.Count > 0)
                    {
                        tempMeshShapes.Clear();
                        outputDatas[baseMeshIndex].GetShapes(tempMeshShapes);
                        MergeMeshShapesAtSeam(mainMesh, meshObjects[baseMeshIndex].mainMesh, seamShapes[0], finalMeshShapes, tempMeshShapes, nonSeamMergableBlendShapes);
                    }

                    #endregion

                    #region Extract Main Vertex Groups

                    if (standaloneVertexGroups != null)
                    {
                        MorphUtils.ExtractVertexGroups(objectSetup.mainMesh, standaloneVertexGroups, finalVertexGroups, objectSetup.name, normalizeVertexGroups, 0.00001f, defaultVertexGroupMaxWeight, true, mainVertices, mainNormals, mainTangents, mainWeld);
                    }

                    int standaloneIndicesEnd = finalVertexGroups.Count - 1;

                    Vector2Int muscleGroupIndices = indicesDefault;
                    muscleGroupIndices.x = finalVertexGroups.Count;
                    if (muscleGroups != null)
                    {
                        tempBlendShapeTargets.Clear();
                        foreach(var group in muscleGroups)
                        {
                            if (group.isComposite) continue;

                            var target = group.initialShapeTarget;
                            if (string.IsNullOrWhiteSpace(target.newName)) target.newName = group.name;
                            tempBlendShapeTargets.Add(target);
                        }

                        MorphUtils.ExtractVertexGroups(objectSetup.mainMesh, tempBlendShapeTargets, finalVertexGroups, objectSetup.name, false, 0.00001f, 0f, false, mainVertices, mainNormals, mainTangents, mainWeld);
                        muscleGroupIndices.y = finalVertexGroups.Count - 1;
                    }

                    Vector2Int fatGroupIndices = indicesDefault;
                    fatGroupIndices.x = finalVertexGroups.Count;
                    if (fatGroups != null)
                    {
                        tempBlendShapeTargets.Clear();
                        foreach (var group in fatGroups)
                        {
                            if (group.isComposite) continue;

                            var target = group.initialShapeTarget;
                            if (string.IsNullOrWhiteSpace(target.newName)) target.newName = group.name;
                            tempBlendShapeTargets.Add(target);
                        }

                        MorphUtils.ExtractVertexGroups(objectSetup.mainMesh, tempBlendShapeTargets, finalVertexGroups, objectSetup.name, false, 0.00001f, 0f, false, mainVertices, mainNormals, mainTangents, mainWeld);
                        fatGroupIndices.y = finalVertexGroups.Count - 1;
                    }

                    Vector2Int variationGroupIndices = indicesDefault;
                    variationGroupIndices.x = finalVertexGroups.Count;
                    if (variationGroups != null)
                    {
                        tempBlendShapeTargets.Clear();
                        foreach (var group in variationGroups)
                        {
                            if (group.isComposite) continue;

                            var target = group.initialShapeTarget;
                            if (string.IsNullOrWhiteSpace(target.newName)) target.newName = group.name;
                            tempBlendShapeTargets.Add(target);
                        }

                        MorphUtils.ExtractVertexGroups(objectSetup.mainMesh, tempBlendShapeTargets, finalVertexGroups, objectSetup.name, false, 0.00001f, 0f, false, mainVertices, mainNormals, mainTangents, mainWeld);
                        variationGroupIndices.y = finalVertexGroups.Count - 1;
                    }

                    #endregion

                    bool hasMuscleGroups = muscleGroupIndices.x <= muscleGroupIndices.y;
                    bool hasFatGroups = fatGroupIndices.x <= fatGroupIndices.y;
                    bool hasVariationGroups = variationGroupIndices.x <= variationGroupIndices.y;

                    #region Create And Insert Composite Vertex Groups

                    tempVertexGroups.Clear();

                    Vector2Int compositeStandaloneGroupIndices = indicesDefault;
                    if (compositeVertexGroups != null)
                    {
                        compositeStandaloneGroupIndices.x = tempVertexGroups.Count;
                        MorphUtils.CreateCompositeVertexGroups(compositeVertexGroups, finalVertexGroups, tempVertexGroups, defaultVertexGroupMaxWeight);
                        compositeStandaloneGroupIndices.y = tempVertexGroups.Count - 1;
                    }

                    Vector2Int compositeMuscleGroupIndices = indicesDefault;
                    if (muscleGroups != null)
                    {
                        tempCompositeGroups.Clear();
                        foreach (var group in muscleGroups)
                        {
                            if (!group.isComposite) continue;

                            var composite = group.composite;
                            if (string.IsNullOrWhiteSpace(composite.name)) composite.name = group.name;
                            tempCompositeGroups.Add(composite);
                        }

                        compositeMuscleGroupIndices.x = tempVertexGroups.Count;
                        MorphUtils.CreateCompositeVertexGroups(tempCompositeGroups, finalVertexGroups, tempVertexGroups, defaultVertexGroupMaxWeight);
                        compositeMuscleGroupIndices.y = tempVertexGroups.Count - 1;
                    }

                    Vector2Int compositeFatGroupIndices = indicesDefault;
                    if (fatGroups != null)
                    {
                        tempCompositeGroups.Clear();
                        foreach (var group in fatGroups)
                        {
                            if (!group.isComposite) continue;

                            var composite = group.composite;
                            if (string.IsNullOrWhiteSpace(composite.name)) composite.name = group.name;
                            tempCompositeGroups.Add(composite);
                        }

                        compositeFatGroupIndices.x = tempVertexGroups.Count;
                        MorphUtils.CreateCompositeVertexGroups(tempCompositeGroups, finalVertexGroups, tempVertexGroups, defaultVertexGroupMaxWeight);
                        compositeFatGroupIndices.y = tempVertexGroups.Count - 1;
                    }

                    Vector2Int compositeVariationGroupIndices = indicesDefault;
                    if (variationGroups != null)
                    {
                        tempCompositeGroups.Clear();
                        foreach (var group in variationGroups)
                        {
                            if (!group.isComposite) continue;

                            var composite = group.composite;
                            if (string.IsNullOrWhiteSpace(composite.name)) composite.name = group.name;
                            tempCompositeGroups.Add(composite);
                        }

                        compositeVariationGroupIndices.x = tempVertexGroups.Count;
                        MorphUtils.CreateCompositeVertexGroups(tempCompositeGroups, finalVertexGroups, tempVertexGroups, defaultVertexGroupMaxWeight);
                        compositeVariationGroupIndices.y = tempVertexGroups.Count - 1;
                    }

                    if (compositeStandaloneGroupIndices.x <= compositeStandaloneGroupIndices.y)
                    {
                        tempVertexGroups2.Clear();
                        for(int i = compositeStandaloneGroupIndices.x; i <= compositeStandaloneGroupIndices.y; i++)
                        {
                            tempVertexGroups2.Add(tempVertexGroups[i]);
                        }

                        int insertIndex = Mathf.Max(0, standaloneIndicesEnd + 1);
                        if (insertIndex >= finalVertexGroups.Count)
                        {
                            finalVertexGroups.AddRange(tempVertexGroups2);
                        }
                        else
                        {
                            finalVertexGroups.InsertRange(insertIndex, tempVertexGroups2);
                        }

                        int count = compositeStandaloneGroupIndices.y - compositeStandaloneGroupIndices.x + 1;
                        Vector2Int add = new Vector2Int(count, count);

                        if (hasMuscleGroups) muscleGroupIndices = muscleGroupIndices + add; else muscleGroupIndices.x = muscleGroupIndices.x + count;
                        if (hasFatGroups) fatGroupIndices = fatGroupIndices + add; else fatGroupIndices.x = fatGroupIndices.x + count;
                        if (hasVariationGroups) variationGroupIndices = variationGroupIndices + add; else variationGroupIndices.x = variationGroupIndices.x + count;
                    }
                    if (compositeMuscleGroupIndices.x <= compositeMuscleGroupIndices.y)
                    {
                        tempVertexGroups2.Clear();
                        for (int i = compositeMuscleGroupIndices.x; i <= compositeMuscleGroupIndices.y; i++)
                        {
                            tempVertexGroups2.Add(tempVertexGroups[i]);
                        }

                        int insertIndex = muscleGroupIndices.y + 1;
                        if (insertIndex >= finalVertexGroups.Count)
                        {
                            finalVertexGroups.AddRange(tempVertexGroups2);
                        }
                        else
                        {
                            finalVertexGroups.InsertRange(insertIndex, tempVertexGroups2);
                        }

                        int count = compositeMuscleGroupIndices.y - compositeMuscleGroupIndices.x + 1;
                        if (!hasMuscleGroups) muscleGroupIndices = new Vector2Int(muscleGroupIndices.x, muscleGroupIndices.x - 1);
                        muscleGroupIndices.y = muscleGroupIndices.y + count;
                        Vector2Int add = new Vector2Int(count, count);
                        if (hasFatGroups) fatGroupIndices = fatGroupIndices + add; else fatGroupIndices.x = fatGroupIndices.x + count;
                        if (hasVariationGroups) variationGroupIndices = variationGroupIndices + add; else variationGroupIndices.x = variationGroupIndices.x + count;
                    }
                    if (compositeFatGroupIndices.x <= compositeFatGroupIndices.y)
                    {
                        tempVertexGroups2.Clear();
                        for (int i = compositeFatGroupIndices.x; i <= compositeFatGroupIndices.y; i++)
                        {
                            tempVertexGroups2.Add(tempVertexGroups[i]);
                        }

                        int insertIndex = fatGroupIndices.y + 1;
                        if (insertIndex >= finalVertexGroups.Count)
                        {
                            finalVertexGroups.AddRange(tempVertexGroups2);
                        }
                        else
                        {
                            finalVertexGroups.InsertRange(insertIndex, tempVertexGroups2);
                        }

                        int count = compositeFatGroupIndices.y - compositeFatGroupIndices.x + 1;
                        if (!hasFatGroups) fatGroupIndices = new Vector2Int(fatGroupIndices.x, fatGroupIndices.x - 1);
                        fatGroupIndices.y = fatGroupIndices.y + count;
                        Vector2Int add = new Vector2Int(count, count);
                        if (hasVariationGroups) variationGroupIndices = variationGroupIndices + add; else variationGroupIndices.x = variationGroupIndices.x + count;
                    }
                    if (compositeVariationGroupIndices.x <= compositeVariationGroupIndices.y)
                    {
                        tempVertexGroups2.Clear();
                        for (int i = compositeVariationGroupIndices.x; i <= compositeVariationGroupIndices.y; i++)
                        {
                            tempVertexGroups2.Add(tempVertexGroups[i]);
                        }

                        int insertIndex = variationGroupIndices.y + 1;
                        if (insertIndex >= finalVertexGroups.Count)
                        {
                            finalVertexGroups.AddRange(tempVertexGroups2);
                        }
                        else
                        {
                            finalVertexGroups.InsertRange(insertIndex, tempVertexGroups2);
                        }

                        int count = compositeFatGroupIndices.y - compositeFatGroupIndices.x + 1;
                        if (!hasVariationGroups) variationGroupIndices = new Vector2Int(variationGroupIndices.x, variationGroupIndices.x - 1);
                        variationGroupIndices.y = variationGroupIndices.y + count;
                    }

                    #endregion

                    #region Finalize Vertex Groups

                    MorphUtils.FinalizeVertexGroupsAsPool(finalVertexGroups, averageMuscleGroupsAsPool, normalizeMuscleGroupsAsPool, muscleGroupIndices, averageMuscleGroupsAsPoolMaxWeight, normalizeMuscleGroupsAsPoolMaxWeight);
                    MorphUtils.FinalizeVertexGroupsAsPool(finalVertexGroups, averageFatGroupsAsPool, normalizeFatGroupsAsPool, fatGroupIndices, averageFatGroupsAsPoolMaxWeight, normalizeFatGroupsAsPoolMaxWeight);
                    MorphUtils.FinalizeVertexGroupsAsPool(finalVertexGroups, averageVariationGroupsAsPool, normalizeVariationGroupsAsPool, variationGroupIndices, averageVariationGroupsAsPoolMaxWeight, normalizeVariationGroupsAsPoolMaxWeight);

                    #region Merge Vertex Groups At Seam

                    if (baseMeshIndex >= 0 && baseMeshIndex < objectIndex && seamShapes.Count > 0)
                    {
                        tempVertexGroups.Clear();
                        outputDatas[baseMeshIndex].GetVertexGroups(tempVertexGroups); 
                        MergeVertexGroupsAtSeam(mainMesh, meshObjects[baseMeshIndex].mainMesh, seamShapes[0], finalVertexGroups, tempVertexGroups, nonSeamMergableBlendShapes);
                    }

                    #endregion

                    #endregion

                    #region Create Final Meshes

                    Mesh outputMeshMain = null;
                    var baseVertices = mainMesh.vertices;
                    var baseUV = useUVsToFindClosestVertex ? mainMesh.GetUVsByChannelAsList((int)nearestVertexUVChannel) : null;
                    if (storeNearestVertexInUV)
                    {
                        List<Vector4> uv3 = mainMesh.GetUVsByChannelAsListV4(3);
                        if (uv3 == null || uv3.Count < mainMesh.vertexCount)
                        {
                            uv3 = new List<Vector4>();
                            for (int b = 0; b < mainMesh.vertexCount; b++) uv3.Add(Vector4.zero);
                        }

                        for (int b = 0; b < uv3.Count; b++) uv3[b] = StoreIndexInUV(uv3[b], b);

                        if (outputMeshMain == null) outputMeshMain = MeshUtils.DuplicateMesh(mainMesh);
                        outputMeshMain.SetUVs(3, uv3);
                    }

                    if (outputMeshMain == null && !ReferenceEquals(mainMesh, objectSetup.mainMesh)) outputMeshMain = mainMesh;
                    if (outputMeshMain != null)
                    {
                        outputMeshMain.ClearBlendShapes();

                        outputMeshMain.name = mainMesh.name + "_" + objectSetup.name;
                        outputMeshes.Add(outputMeshMain);
                    }
                    else outputMeshMain = mainMesh;

                    meshLODs.Clear();
                    if (objectSetup.lods != null)
                    {
                        for (int b = 0; b < objectSetup.lods.Length; b++)
                        {
                            var lod = objectSetup.lods[b];

                            Mesh lodMesh = lodMeshes[b];
                            Mesh outputMesh = null;
                            if (storeNearestVertexInUV)
                            {
                                var lodVertices = lodMesh.vertices;
                                var mergedVertices = MeshDataTools.WeldVertices(lodVertices);

                                List<Vector4> uv3 = lodMesh.GetUVsByChannelAsListV4(3);
                                if (uv3 == null || uv3.Count < lodMesh.vertexCount)
                                {
                                    uv3 = new List<Vector4>();
                                    for (int c = 0; c < lodMesh.vertexCount; c++) uv3.Add(Vector4.zero);
                                }

                                int[] nearestVertices = new int[lodMesh.vertexCount];
                                if (useUVsToFindClosestVertex)
                                {
                                    var lodUV = lodMesh.GetUVsByChannelAsList((int)nearestVertexUVChannel);
                                    MeshDataTools.FindClosestVerticesUV(lodUV, baseUV, nearestVertices);
                                }
                                else
                                {
                                    MeshDataTools.FindClosestVertices(lodVertices, baseVertices, nearestVertices);
                                }

                                for (int c = 0; c < uv3.Count; c++) uv3[c] = StoreIndexInUV(uv3[c], nearestVertices[c]);
                                if (mergedVertices != null)
                                {
                                    for (int c = 0; c < mergedVertices.Length; c++)
                                    {
                                        var merge = mergedVertices[c];
                                        uv3[c] = uv3[merge.firstIndex];
                                    }
                                }

                                if (outputMesh == null) outputMesh = MeshUtils.DuplicateMesh(lodMesh);
                                outputMesh.SetUVs(3, uv3);
                            }

                            if (outputMesh != null)
                            {
                                outputMesh.ClearBlendShapes();

                                if (objectSetup.solidifyThickness > 0f)
                                {
                                    var solidifiedMesh = MeshDataTools.Solidify(outputMesh, objectSetup.solidifyThickness, 0f, true, true);
                                    solidifiedMesh.name = outputMesh.name;
                                    outputMesh = solidifiedMesh;
                                }

                                outputMesh.name = lodMesh.name + "_" + objectSetup.name;
                                outputMesh.bounds = new Bounds() { center = boundsCenter, extents = boundsExtents };
                                outputMeshes.Add(outputMesh);
                            }
                            else outputMesh = lodMesh;

                            meshLODs.Add(new MeshLOD() { mesh = outputMesh, screenRelativeTransitionHeight = lod.screenRelativeTransitionHeight });
                        }

                        meshLODs.Sort((MeshLOD lodA, MeshLOD lodB) => Math.Sign(lodB.screenRelativeTransitionHeight - lodA.screenRelativeTransitionHeight)); // descending order
                    }
                    meshLODs.Insert(0, new MeshLOD() { mesh = outputMeshMain, screenRelativeTransitionHeight = meshLODs.Count == 0 ? 0.001f : (meshLODs[0].screenRelativeTransitionHeight < 1f ? 1f : (meshLODs[0].screenRelativeTransitionHeight + 0.1f)) });

                    #endregion
                    
                    #region Finalize Data
                    
                    serializedData.meshShapes = finalMeshShapes.ToArray();
                    serializedData.vertexGroups = finalVertexGroups.ToArray();

                    serializedData.standaloneGroups = new Vector2Int(0, muscleGroupIndices.x - 1);
                    serializedData.muscleGroups = muscleGroupIndices;
                    serializedData.fatGroups = fatGroupIndices;
                    serializedData.variationGroups = variationGroupIndices;
                    
                    serializedData.midlineVertexGroup = serializedData.IndexOfVertexGroup(midlineVertexGroup);
                    serializedData.bustVertexGroup = serializedData.IndexOfVertexGroup(bustVertexGroup);
                    serializedData.bustNerfVertexGroup = serializedData.IndexOfVertexGroup(bustNerfVertexGroup);
                    serializedData.bustSizeShape = serializedData.IndexOfMeshShape(bustSizeShape);
                    serializedData.nippleMaskVertexGroup = serializedData.IndexOfVertexGroup(nippleMaskVertexGroup);
                    serializedData.genitalMaskVertexGroup = serializedData.IndexOfVertexGroup(genitalMaskVertexGroup);

                    serializedData.defaultMassShapeWeight = defaultMassShapeWeight;
                    serializedData.minMassShapeWeight = minMassShapeWeight;

                    serializedData.vertexCount = outputMeshMain.vertexCount;

                    serializedData.leftRightFlags = new bool[outputMeshMain.vertexCount];
                    var mainUV = outputMeshMain.uv;
                    if (mainUV != null && mainUV.Length > 0)
                    {
                        for (int i = 0; i < mainUV.Length; i++) serializedData.leftRightFlags[i] = mainUV[i].x < 0.5f;
                    }

                    serializedData.baseBoneWeights = mainBoneWeights;
                    serializedData.baseBindPose = tempMatrices.ToArray();

                    serializedData.materials = objectSetup.materials;
                    serializedData.boundsCenter = objectSetup.boundsCenter;
                    serializedData.boundsExtents = objectSetup.boundsExtents;
                    meshLODs.Sort(CullingLODs.SortLODsDescending);
                    serializedData.meshLODs = meshLODs.ToArray();

                    if (outputData == null) outputData = CustomizableCharacterMeshV2_DATA.CreateInstance(dataName, serializedData); else outputData.ReplaceData(serializedData);
                    outputDatas.Add(outputData);

                    GameObject prefabChild = new GameObject(objectSetup.name);
                    prefabChild.transform.SetParent(outputPrefab.transform, false);

                    var characterMesh = prefabChild.AddComponent<CustomizableCharacterMeshV2>();
                    characterMesh.SetData(outputData);
                    characterMesh.SetRigRoot(outputPrefab.transform.FindDeepChildLiberal(objectSetup.rigRootName));
                    characterMesh.SetAvatar(objectSetup.avatar);
                    characterMesh.SetRigBufferID(objectSetup.rigBufferId);
                    characterMesh.SetShapeBufferID(objectSetup.shapeBufferId);
                    characterMesh.SetMorphBufferID(objectSetup.morphBufferId);

                    characterMeshes.Add(characterMesh);

                    #endregion

                    PostMeshObjectSetup(objectIndex, objectSetup, meshObject); 

                }

                for (int objectIndex = 0; objectIndex < meshObjects.Length; objectIndex++)
                {
                    var target = meshObjects[objectIndex];
                    var characterMesh = characterMeshes[objectIndex];

                    int shapeInstanceIndex = IndexOfMeshObject(target.shapesInstanceParent);
                    if (shapeInstanceIndex >= 0) characterMesh.shapesInstanceReference = characterMeshes[shapeInstanceIndex];

                    int rigInstanceIndex = IndexOfMeshObject(target.rigInstanceParent);
                    if (rigInstanceIndex >= 0) characterMesh.rigInstanceReference = characterMeshes[rigInstanceIndex]; 

                    int characterInstanceIndex = IndexOfMeshObject(target.characterInstanceParent);
                    if (characterInstanceIndex >= 0) characterMesh.characterInstanceReference = characterMeshes[characterInstanceIndex];

                    /*if (gc != null && target.isBreastMesh)
                    {
                        if (gc.breastMeshes == null) gc.breastMeshes = new List<CustomizableCharacterMesh>();
                        gc.breastMeshes.Add(characterMesh);
                    }*/
                }
            }


            PostSetup();

            #region Asset Saving
#if UNITY_EDITOR
            foreach (var mesh in outputMeshes)
            {
                string meshSavePath = Extensions.CreateUnityAssetPathString(assetSavePath, mesh.name, ".asset");

                if (!AssetDatabase.IsNativeAsset(mesh))
                {
                    AssetDatabase.DeleteAsset(meshSavePath);
                    AssetDatabase.CreateAsset(mesh, meshSavePath); 

                    Debug.Log("Saved mesh " + mesh.name);
                }
                else
                {
                    AssetDatabase.SaveAssetIfDirty(mesh);

                    Debug.Log("Saved native mesh " + mesh.name);
                }
            }

            foreach (var outputData in outputDatas)
            {
                string savePath = Extensions.CreateUnityAssetPathString(dataSavePath, outputData.name, ".asset");

                if (!AssetDatabase.IsNativeAsset(outputData))
                {
                    AssetDatabase.DeleteAsset(savePath);
                    AssetDatabase.CreateAsset(outputData, savePath);
                }
                else
                {
                    AssetDatabase.SaveAssetIfDirty(outputData);
                }
            }

            var prefab = PrefabUtility.SaveAsPrefabAsset(outputPrefab, Extensions.CreateUnityAssetPathString(prefabSavePath, outputPrefab.name, ".prefab"), out bool success);
            if (success) outputPrefab = prefab; else Debug.LogError($"Failed to save prefab '{outputPrefab.name}'!");
#endif
            #endregion

            PostSave();
        }

        #region Extendible Methods

        protected virtual void PreSetup()
        {
        }
        protected virtual void PostSetup()
        {
        }
        protected virtual void PostSave()
        {
        }

        protected virtual void PreMeshObjectSetup(int objectIndex, MeshObject objectSetup, CustomizableCharacterMeshV2 meshObject, ref Mesh mainMesh, Mesh[] lodMeshes)
        {
        }
        protected virtual void PostMeshObjectSetup(int objectIndex, MeshObject objectSetup, CustomizableCharacterMeshV2 meshObject)
        {
        }

        #endregion

    }

}

#endif