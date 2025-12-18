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
using Swole.API.Unity;

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
        public struct TextureToVertexColor
        {
            public UVChannelURP uvChannel;
            public bool useBaseMeshGroup;
            public bool treatAsMeshIslands;
            public Texture2D texture;
            public RGBAChannel targetTextureChannelsR;
            public RGBAChannel targetTextureChannelsG;
            public RGBAChannel targetTextureChannelsB;
            public RGBAChannel targetTextureChannelsA;
        }

        [Serializable]
        public struct UVTransferData
        {
            public UVChannelURP uvChannel;
            public Vector4[] data;
        }

        [Serializable]
        public struct NameFloat
        {
            public string name;
            public float value;
        }

        [Serializable]
        public struct MeshObject
        {

            public string name;

            public Material[] materials;

            public Mesh mainMesh;

            public MeshLOD[] lods;
            public bool preserveLodMeshVertexColors;
            public MeshMergeSlot[] meshesToCombine;
            public BlendShapeTarget[] prepShapes;

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

            public bool snapToBaseSurface;
            public float snapSurfaceOffset;
            public List<NameFloat> snapSurfaceOffsetShapeOverrides;
            public float GetSnapSurfaceOffsetForShape(string shapeName)
            {
                if (snapSurfaceOffsetShapeOverrides == null) return snapSurfaceOffset;
                foreach (var ovrd in snapSurfaceOffsetShapeOverrides) if (ovrd.name == shapeName) return ovrd.value;
                return snapSurfaceOffset;
            }
            public bool onlySnapOpenEdges;
            public float snapNormalsTransferWeight;
            public bool snapBlendShapes;
            [Tooltip("If Snap Blend Shapes is true and this list is empty, all shapes will be snapped.")]
            public List<string> shapesToSnap;

            public bool allowSurfaceDataTransfer;
            public bool surfaceTransferFromExistingSetup;
            public List<string> vertexGroupsToPreserve;
            public bool copyMeshShapeDataFromExistingSetup;
            public List<string> meshShapesToPreserve;
            public CustomizableCharacterMeshV2_DATA existingSetup;
            public bool createTransferDebugObject;
            public bool treatAsMeshIslands;

            public bool transferNormals;
            [Range(0, 1)]
            public float transferNormalsWeight;

            public bool transferVertexColors;

            public bool transferBoneWeights;

            public bool transferUVs;
            public bool preserveExistingUVData;
            public Vector2Int uvTransferRange;
            [Tooltip("UV Channel Index Offset to use when transferring UVs")]
            public int uvTransferOffset;

            public bool transferBlendShapes;
            public bool preserveExistingBlendShapeData;

            public Vector3 transferOffset;
            public Vector3 transferRotationOffset;

            public string shapesInstanceParent;
            public string rigInstanceParent;
            public string characterInstanceParent;

            public bool applyInitialOffset;
            public Vector3 initialOffset;
            public Vector3 initialRotationOffset;

            public float solidifyThickness;

            public TextureToVertexColor[] textureToVertexColorBakes;

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
        protected int FetchIndexFromUV(Vector4 uv)
        {
            switch (nearestVertexIndexElement)
            {
                case RGBAChannel.R:
                    return (int)uv.x;
                case RGBAChannel.G:
                    return (int)uv.y;
                case RGBAChannel.B:
                    return (int)uv.z;
                case RGBAChannel.A:
                    return (int)uv.w;
            }

            return 0;
        }

        public int primaryMeshObject;

        public MeshObject[] meshObjects;

        public int IndexOfMeshObject(string targetMeshName)
        {
            if (meshObjects == null || string.IsNullOrWhiteSpace(targetMeshName)) return -1;

            for (int a = 0; a < meshObjects.Length; a++) if (meshObjects[a].name == targetMeshName) return a;
            return -1;
        }

        [Header("Initial Data")]
        public MultiBlendShapeTarget[] auxiliaryShapes;
        [Tooltip("Shapes that are applied within the shader.")]
        public MultiBlendShapeTarget[] standaloneShapes;

        public BlendShapeTarget[] standaloneVertexGroups;
        public CompositeVertexGroupBuilder[] compositeVertexGroups;

        public bool normalizeVertexGroups;
        public float defaultVertexGroupMaxWeight;

        public string midlineVertexGroup;

        public string bustVertexGroup;
        public string bustNerfVertexGroup;
        public string bustSizeShape;
        public string bustShapeShape;
        public string bustSizeMuscleShape;

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

        protected Dictionary<int, UVTransferData> tempUVTransfers = new Dictionary<int, UVTransferData>();

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

            if (tempUVTransfers == null) tempUVTransfers = new Dictionary<int, UVTransferData>();

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
                        var baseMeshData = new BaseMeshData();

                        baseMeshData.UpdateBaseMeshData(mainMesh);

                        foreach (var merger in objectSetup.meshesToCombine)
                        {
                            void CombineWithMeshLOD(int lodSlot)
                            {
                                var meshToCombine = merger.mesh;
                                meshToCombine = MeshUtils.DuplicateMesh(meshToCombine);
                                meshToCombine.name = merger.mesh.name;

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

                                    if (merger.recalculateNormals) meshToCombine.normals = normals;
                                    if (merger.recalculateTangents) meshToCombine.tangents = tangents;

                                    meshToCombine.ClearBlendShapes();
                                    foreach (var shape in shapes) 
                                    { 
                                        shape.AddToMesh(meshToCombine);
                                    }
                                }

                                #endregion

                                #region Transfer Surface Data

                                if (merger.allowSurfaceDataTransfer)
                                {

                                    var settings = new MorphUtils.TransferSurfaceDataSettings()
                                    {
                                        baseMeshTriangles = baseMeshData.baseMeshTriangles,
                                        baseMeshVertices = baseMeshData.baseMeshVertices,
                                        baseMeshNormals = baseMeshData.baseMeshNormals,
                                        baseMeshUVs = baseMeshData.baseMeshUVs,
                                        baseMeshBlendShapes = baseMeshData.baseMeshBlendShapes,
                                        baseMeshBoneWeights = baseMeshData.baseMeshBoneWeights,
                                        baseMeshBonesPerVertex = baseMeshData.baseMeshBonesPerVertex,
                                        baseMeshBoneWeightStartIndices = baseMeshData.baseMeshBoneWeightStartIndices,

                                        treatAsMeshIslands = merger.treatAsMeshIslands,

                                        transferNormals = merger.transferNormals,
                                        transferNormalsWeight = merger.transferNormalsWeight,

                                        transferVertexColors = merger.transferVertexColors,

                                        transferBoneWeights = merger.transferBoneWeights,

                                        transferUVs = merger.transferUVs,
                                        preserveExistingUVData = merger.preserveExistingUVData,
                                        uvTransferRange = merger.uvTransferRange,
                                        uvChannelIndexTransferOffset = merger.uvTransferOffset,

                                        transferBlendShapes = merger.transferBlendShapes,
                                        preserveExistingBlendShapeData = merger.preserveExistingBlendShapeData,

                                        alignmentOffset = merger.alignmentOffset,
                                        alignmentRotationOffsetEuler = merger.alignmentRotationOffset
                                    };
                                    
                                    meshToCombine = MorphUtils.TransferSurfaceData(meshToCombine, false, settings);

                                }

                                #endregion

                                if (lodSlot == 0)
                                {
                                    int origVertexCount = mainMesh.vertexCount;
                                    string meshName = mainMesh.name;
                                    var mergeMesh = merger.mergeSeam ? MergeMeshSeam(meshToCombine, mainMesh, merger.seamShape, merger.seamMergeMethod, out _, true, true, merger.mergeSeamTangents, merger.mergeSeamUVs, nonSeamMergableBlendShapes) : meshToCombine;
                                    mainMesh = MeshDataTools.CombineMeshes(mainMesh, new Mesh[] { mergeMesh });
                                    mainMesh.name = meshName;

                                    if (merger.updateBaseMeshData)
                                    {
                                        baseMeshData.UpdateBaseMeshData(mainMesh);
                                    }
                                }
                                else if (lodMeshes != null && lodSlot - 1 >= 0 && lodSlot - 1 < lodMeshes.Length)
                                {
                                    int slot = lodSlot - 1;

                                    string meshName = lodMeshes[slot].name;
                                    var mergeMesh = merger.mergeSeam ? MergeMeshSeam(meshToCombine, lodMeshes[slot], merger.seamShape, merger.seamMergeMethod, out _, true, true, merger.mergeSeamTangents, merger.mergeSeamUVs, nonSeamMergableBlendShapes) : meshToCombine;
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

                        baseMeshData.Dispose();
                    }

                    #endregion

                    var mainVertices = mainMesh.vertices;
                    var mainNormals = mainMesh.normals;
                    var mainTangents = mainMesh.tangents;

                    MeshDataTools.WeldedVertex[] mainWeld = null;

                    #region Create Prep Shapes

                    if (objectSetup.prepShapes != null && objectSetup.prepShapes.Length > 0)
                    {
                        if (mainWeld == null) mainWeld = MeshDataTools.WeldVertices(mainVertices);

                        var prepMesh = MeshUtils.DuplicateMesh(mainMesh);
                        prepMesh.name = mainMesh.name;

                        foreach(var prepTarget in objectSetup.prepShapes)
                        {
                            if (prepTarget.TryGetBlendShape(objectSetup.name, mainMesh, out var blendShape, mainVertices, mainNormals, mainTangents, mainWeld))
                            {
                                blendShape.AddToMesh(prepMesh);
                            }
                        }

                        mainMesh = prepMesh;
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

                    Mesh surfaceTransferredMesh = null;
                    Mesh surfaceTransferredMeshIslands = null;
                    SurfaceDataTransferVertex[] surfaceDataTransferVertexData = null;

                    #region Transfer Surface Data

                    Mesh surfaceTransferBaseMesh = objectSetup.surfaceTransferFromExistingSetup && objectSetup.existingSetup != null ? objectSetup.existingSetup.SerializedData.Mesh : (baseMeshIndex >= 0 ? meshObjects[baseMeshIndex].mainMesh : null);
                    if (surfaceTransferBaseMesh != null && surfaceTransferBaseMesh != objectSetup.mainMesh)
                    {
                        var baseMeshData = new BaseMeshData();

                        baseMeshData.UpdateBaseMeshData(surfaceTransferBaseMesh);

                        if (objectSetup.snapToBaseSurface)
                        {
                            Mesh SnapMeshToSurface(Mesh origMesh)
                            {
                                var triangles = origMesh.triangles;
                                var vertices = origMesh.vertices;
                                var normals = origMesh.normals;
                                var blendShapes = objectSetup.snapBlendShapes ? origMesh.GetBlendShapes() : null;
                                var weld = MeshDataTools.WeldVertices(vertices);
                                var openEdgeData = objectSetup.onlySnapOpenEdges ? MeshDataTools.GetOpenEdgeData(origMesh.vertexCount, triangles, weld) : null;

                                var settings = new MorphUtils.TransferSurfaceDataSettings()
                                {
                                    baseMeshTriangles = baseMeshData.baseMeshTriangles,
                                    baseMeshVertices = baseMeshData.baseMeshVertices,
                                    baseMeshNormals = baseMeshData.baseMeshNormals,
                                    baseMeshUVs = baseMeshData.baseMeshUVs,
                                    baseMeshBlendShapes = baseMeshData.baseMeshBlendShapes,
                                    baseMeshBoneWeights = baseMeshData.baseMeshBoneWeights,
                                    baseMeshBonesPerVertex = baseMeshData.baseMeshBonesPerVertex,
                                    baseMeshBoneWeightStartIndices = baseMeshData.baseMeshBoneWeightStartIndices,

                                    treatAsMeshIslands = false,

                                    alignmentOffset = objectSetup.transferOffset,
                                    alignmentRotationOffsetEuler = objectSetup.transferRotationOffset
                                };

                                var newVertices = origMesh.vertices;
                                var newNormals = origMesh.normals;

                                var tempSurfaceDataTransferVertexData = new SurfaceDataTransferVertex[origMesh.vertexCount];
                                var surfaceTransferredMesh = MorphUtils.TransferSurfaceData(origMesh, true, settings, tempSurfaceDataTransferVertexData);
                                for(int i = 0; i < vertices.Length; i++)
                                {
                                    if (objectSetup.onlySnapOpenEdges && !openEdgeData[i].IsOpenEdge()) continue;

                                    var transferData = tempSurfaceDataTransferVertexData[i];

                                    Vector3 originalNormal = normals[i];
                                    Vector3 newNormal = (settings.baseMeshNormals[transferData.closestIndex0] * transferData.closestWeight0)
                                        + (settings.baseMeshNormals[transferData.closestIndex1] * transferData.closestWeight1)
                                        + (settings.baseMeshNormals[transferData.closestIndex2] * transferData.closestWeight2);

                                    Vector3 originalPosition = vertices[i];
                                    Vector3 newPosition = (settings.baseMeshVertices[transferData.closestIndex0] * transferData.closestWeight0) 
                                        + (settings.baseMeshVertices[transferData.closestIndex1] * transferData.closestWeight1) 
                                        + (settings.baseMeshVertices[transferData.closestIndex2] * transferData.closestWeight2);

                                    newPosition = newPosition - newNormal * objectSetup.snapSurfaceOffset;
                                    newVertices[i] = newPosition;

                                    newNormals[i] = Vector3.LerpUnclamped(originalNormal, newNormal, objectSetup.snapNormalsTransferWeight).normalized;
                                }

                                if (blendShapes != null)
                                {
                                    var shapeMesh = MeshUtils.DuplicateMesh(origMesh);
                                    shapeMesh.ClearBlendShapes();
                                    var tempVerts = shapeMesh.vertices;
                                    var tempNormals = shapeMesh.normals;
                                    foreach (var shape in blendShapes)
                                    {
                                        if (shape == null || shape.frames == null || (objectSetup.shapesToSnap != null && objectSetup.shapesToSnap.Count > 0 && !objectSetup.shapesToSnap.Contains(shape.name))) continue;

                                        float snapSurfaceOffsetOverride = objectSetup.GetSnapSurfaceOffsetForShape(shape.name);
                                        for (int f = 0; f < shape.frames.Length; f++)
                                        {
                                            var frame = shape.frames[f];

                                            var shapeVerts = (Vector3[])tempVerts.Clone();
                                            var shapeNormals = (Vector3[])tempNormals.Clone();

                                            for(int v = 0; v < tempVerts.Length; v++)
                                            {
                                                shapeVerts[v] = tempVerts[v] + frame.deltaVertices[v];
                                                shapeNormals[v] = tempNormals[v] + frame.deltaNormals[v];
                                            }

                                            shapeMesh.vertices = shapeVerts;
                                            shapeMesh.normals = shapeNormals;

                                            MorphUtils.TransferSurfaceData(shapeMesh, false, settings, tempSurfaceDataTransferVertexData);

                                            for (int i = 0; i < tempVerts.Length; i++)
                                            {
                                                if (objectSetup.onlySnapOpenEdges && !openEdgeData[i].IsOpenEdge()) continue;

                                                var transferData = tempSurfaceDataTransferVertexData[i];

                                                Vector3 originalNormal = shapeNormals[i];
                                                Vector3 newNormal = (settings.baseMeshNormals[transferData.closestIndex0] * transferData.closestWeight0)
                                                    + (settings.baseMeshNormals[transferData.closestIndex1] * transferData.closestWeight1)
                                                    + (settings.baseMeshNormals[transferData.closestIndex2] * transferData.closestWeight2); 

                                                Vector3 originalPosition = shapeVerts[i];
                                                Vector3 newPosition = (settings.baseMeshVertices[transferData.closestIndex0] * transferData.closestWeight0)
                                                    + (settings.baseMeshVertices[transferData.closestIndex1] * transferData.closestWeight1)
                                                    + (settings.baseMeshVertices[transferData.closestIndex2] * transferData.closestWeight2);

                                                newPosition = newPosition - newNormal * snapSurfaceOffsetOverride;
                                                shapeVerts[i] = newPosition;

                                                shapeNormals[i] = Vector3.LerpUnclamped(originalNormal, newNormal, objectSetup.snapNormalsTransferWeight).normalized;
                                            }

                                            for (int v = 0; v < tempVerts.Length; v++)
                                            {
                                                frame.deltaVertices[v] = shapeVerts[v] - newVertices[v];
                                                frame.deltaNormals[v] = shapeNormals[v] - newNormals[v];
                                            }

                                        }

                                    }
                                }

                                var newMesh = MeshUtils.DuplicateMesh(origMesh);
                                newMesh.name = origMesh.name;

                                newMesh.vertices = newVertices;
                                newMesh.normals = newNormals;
                                if (objectSetup.snapBlendShapes)
                                {
                                    newMesh.ClearBlendShapes();
                                    foreach(var shape in blendShapes) shape.AddToMesh(newMesh);
                                }

                                return newMesh;
                            }

                            mainMesh = SnapMeshToSurface(mainMesh);
                            if (lodMeshes != null)
                            {
                                for(int i = 0; i < lodMeshes.Length; i++) lodMeshes[i] = SnapMeshToSurface(lodMeshes[i]);
                            }
                        }
                        
                        if (objectSetup.allowSurfaceDataTransfer)
                        {
                            surfaceDataTransferVertexData = new SurfaceDataTransferVertex[mainMesh.vertexCount];

                            var settings = new MorphUtils.TransferSurfaceDataSettings()
                            {
                                baseMeshTriangles = baseMeshData.baseMeshTriangles,
                                baseMeshVertices = baseMeshData.baseMeshVertices,
                                baseMeshNormals = baseMeshData.baseMeshNormals,
                                baseMeshUVs = baseMeshData.baseMeshUVs,
                                baseMeshBlendShapes = baseMeshData.baseMeshBlendShapes,
                                baseMeshBoneWeights = baseMeshData.baseMeshBoneWeights,
                                baseMeshBonesPerVertex = baseMeshData.baseMeshBonesPerVertex,
                                baseMeshBoneWeightStartIndices = baseMeshData.baseMeshBoneWeightStartIndices,

                                treatAsMeshIslands = objectSetup.treatAsMeshIslands,

                                transferNormals = objectSetup.transferNormals,
                                transferNormalsWeight = objectSetup.transferNormalsWeight,

                                transferVertexColors = objectSetup.transferVertexColors,

                                transferBoneWeights = objectSetup.transferBoneWeights,

                                transferUVs = objectSetup.transferUVs,
                                preserveExistingUVData = objectSetup.preserveExistingUVData,
                                uvTransferRange = objectSetup.uvTransferRange,
                                uvChannelIndexTransferOffset = objectSetup.uvTransferOffset,

                                transferBlendShapes = objectSetup.transferBlendShapes,
                                preserveExistingBlendShapeData = objectSetup.preserveExistingBlendShapeData,

                                alignmentOffset = objectSetup.transferOffset,
                                alignmentRotationOffsetEuler = objectSetup.transferRotationOffset
                            };

                            if (objectSetup.treatAsMeshIslands)
                            {
                                surfaceTransferredMeshIslands = MorphUtils.TransferSurfaceData(mainMesh, true, settings, surfaceDataTransferVertexData);
                            }
                            else
                            {
                                surfaceTransferredMesh = MorphUtils.TransferSurfaceData(mainMesh, true, settings, surfaceDataTransferVertexData);
                            }

                            if (objectSetup.allowSurfaceDataTransfer) mainMesh = surfaceTransferredMesh;

                        }

                        baseMeshData.Dispose();
                    }

                    #endregion

                    if (objectSetup.createTransferDebugObject && objectSetup.skinnedRendererReference != null)
                    {
                        var debugRenderer = GameObject.Instantiate(objectSetup.skinnedRendererReference);
                        debugRenderer.sharedMesh = surfaceTransferredMesh;
                    }

                    #region Merge Seams

                    if (baseMeshIndex >= 0)
                    {

                        seamShapes.Clear();
                        if (!string.IsNullOrWhiteSpace(objectSetup.seamShape))
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

                    mainVertices = mainMesh.vertices;
                    mainNormals = mainMesh.normals;
                    mainTangents = mainMesh.tangents;

                    mainWeld = MeshDataTools.WeldVertices(mainVertices);

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
                    var origBindPose = mainMesh.bindposes;
                    if ((origBindPose == null || origBindPose.Length <= 0) && surfaceTransferBaseMesh != null) origBindPose = surfaceTransferBaseMesh.bindposes;
                    if ((origBindPose == null || origBindPose.Length <= 0) && (objectSetup.skinnedRendererReference != null && objectSetup.skinnedRendererReference.sharedMesh != null)) origBindPose = objectSetup.skinnedRendererReference.sharedMesh.bindposes;
                    if (objectSetup.skinnedRendererReference == null || objectSetup.avatar == null)
                    {
                        tempMatrices.AddRange(origBindPose);
                    }
                    else
                    {
                        for (int i = 0; i < objectSetup.avatar.SkinnedBonesCount; i++) tempMatrices.Add(Matrix4x4.identity);

                        var originalBindPoses = origBindPose;
                        foreach(var entry in tempIndexRemapper)
                        {
                            if (entry.Value >= tempMatrices.Count || entry.Key >= originalBindPoses.Length) continue;
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

                    #region Create Mesh Shapes

                    if (auxiliaryShapes != null)
                    {
                        foreach (var auxiliaryShape in auxiliaryShapes)
                        {
                            if (auxiliaryShape.targets == null || auxiliaryShape.targets.Length <= 0) continue;

                            tempBlendShapeTargets.Clear();
                            tempBlendShapeTargets.AddRange(auxiliaryShape.targets);
                            tempBlendShapeTargets.Sort(MorphUtils.CompareBlendShapeTargetsByWeight);

                            tempBlendShapes.Clear();
                            foreach (var shape in tempBlendShapeTargets)
                            {
                                if (shape.TryGetBlendShape(objectSetup.name, mainMesh, out var blendShape, finalMeshShapes, mainVertices, mainNormals, mainTangents, mainWeld))
                                {
                                    if (shape.weight > 0f) blendShape.RemapFrameWeights(shape.weight);
                                    tempBlendShapes.Add(blendShape);
                                }
                                else
                                {
                                    blendShape = new BlendShape(shape.targetName);
                                    blendShape.AddFrame(shape.weight, emptyVertices, emptyNormals, emptyTangents);
                                    tempBlendShapes.Add(blendShape);
                                }
                            }

                            var finalShape = MeshShape.CreateFromBlendShapes(auxiliaryShape.name, tempBlendShapes, true);
                            finalMeshShapes.Add(finalShape);
                        }
                    }

                    serializedData.standaloneShapes = indicesDefault;
                    if (standaloneShapes != null)
                    {
                        int rangeStartIndex = finalMeshShapes.Count;
                        foreach (var standaloneShape in standaloneShapes)
                        {
                            if (standaloneShape.targets == null || standaloneShape.targets.Length <= 0) continue;

                            tempBlendShapeTargets.Clear();
                            tempBlendShapeTargets.AddRange(standaloneShape.targets);
                            tempBlendShapeTargets.Sort(MorphUtils.CompareBlendShapeTargetsByWeight);

                            tempBlendShapes.Clear();
                            foreach (var shape in tempBlendShapeTargets)
                            {
                                Debug.Log($"Processing standalone shape '{shape.targetName}' for mesh '{objectSetup.name}'...");
                                if (shape.TryGetBlendShape(objectSetup.name, mainMesh, out var blendShape, finalMeshShapes, mainVertices, mainNormals, mainTangents, mainWeld))
                                {
                                    Debug.Log($" - Successfully extracted blend shape '{blendShape.name}' for standalone shape.");
                                    if (shape.weight > 0f) blendShape.RemapFrameWeights(shape.weight);
                                    tempBlendShapes.Add(blendShape);
                                }
                                else
                                {
                                    Debug.Log($" - Created empty blend shape '{shape.targetName}' for standalone shape."); 
                                    blendShape = new BlendShape(shape.targetName);
                                    blendShape.AddFrame(shape.weight, emptyVertices, emptyNormals, emptyTangents);
                                    tempBlendShapes.Add(blendShape);
                                }
                            }

                            var finalShape = MeshShape.CreateFromBlendShapes(standaloneShape.name, tempBlendShapes, true);
                            finalShape.animatable = standaloneShape.animatable;
                            finalMeshShapes.Add(finalShape);
                        }

                        serializedData.standaloneShapes = new Vector2Int(rangeStartIndex, finalMeshShapes.Count - 1);
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

                    if (variationShapes != null)
                    {
                        tempBlendShapeTargets.Clear();
                        tempBlendShapeTargets.AddRange(variationShapes);
                        tempBlendShapeTargets.Sort(MorphUtils.CompareBlendShapeTargetsByWeight);

                        int startIndex = finalMeshShapes.Count;
                        foreach (var shape in tempBlendShapeTargets)
                        {
                            if (shape.TryGetBlendShape(objectSetup.name, mainMesh, out var blendShape, finalMeshShapes, mainVertices, mainNormals, mainTangents, mainWeld))
                            {
                                var variationShape = MeshShape.CreateFromBlendShape(blendShape);
                                finalMeshShapes.Add(variationShape);
                            }
                        }

                        serializedData.variationShapes = new Vector2Int(startIndex, finalMeshShapes.Count - 1);
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
                        MorphUtils.ExtractVertexGroups(mainMesh, standaloneVertexGroups, finalVertexGroups, objectSetup.name, normalizeVertexGroups, 0.00001f, defaultVertexGroupMaxWeight, true, mainVertices, mainNormals, mainTangents, mainWeld);
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

                        MorphUtils.ExtractVertexGroups(mainMesh, tempBlendShapeTargets, finalVertexGroups, objectSetup.name, false, 0.00001f, 0f, false, mainVertices, mainNormals, mainTangents, mainWeld);
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

                        MorphUtils.ExtractVertexGroups(mainMesh, tempBlendShapeTargets, finalVertexGroups, objectSetup.name, false, 0.00001f, 0f, false, mainVertices, mainNormals, mainTangents, mainWeld);
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

                        MorphUtils.ExtractVertexGroups(mainMesh, tempBlendShapeTargets, finalVertexGroups, objectSetup.name, false, 0.00001f, 0f, false, mainVertices, mainNormals, mainTangents, mainWeld);
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

                        int count = compositeVariationGroupIndices.y - compositeVariationGroupIndices.x + 1;
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

                    #region Bake Textures To Vertex Colors

                    if (objectSetup.textureToVertexColorBakes != null && objectSetup.textureToVertexColorBakes.Length > 0)
                    {
                        Color[] vertexColors = mainMesh.colors;
                        if (vertexColors == null || vertexColors.Length <= 0) vertexColors = new Color[mainMesh.vertexCount];

                        bool flag = false;
                        foreach(var bake in objectSetup.textureToVertexColorBakes)
                        {
                            if (bake.texture == null) continue;

                            List<Vector2> uvs = null;
                            if (bake.useBaseMeshGroup)
                            {
                                if (surfaceTransferBaseMesh == null)
                                {
                                    Debug.LogError($"A texture bake for mesh {objectSetup.name} was ignored because the base mesh group was not specified!");
                                }
                                else
                                {
                                    var baseMeshData = new BaseMeshData();

                                    baseMeshData.UpdateBaseMeshData(surfaceTransferBaseMesh);

                                    var settings = new MorphUtils.TransferSurfaceDataSettings()
                                    {
                                        baseMeshTriangles = baseMeshData.baseMeshTriangles,
                                        baseMeshVertices = baseMeshData.baseMeshVertices,
                                        baseMeshNormals = baseMeshData.baseMeshNormals,
                                        baseMeshUVs = baseMeshData.baseMeshUVs,
                                        baseMeshBlendShapes = baseMeshData.baseMeshBlendShapes,
                                        baseMeshBoneWeights = baseMeshData.baseMeshBoneWeights,
                                        baseMeshBonesPerVertex = baseMeshData.baseMeshBonesPerVertex,
                                        baseMeshBoneWeightStartIndices = baseMeshData.baseMeshBoneWeightStartIndices,

                                        treatAsMeshIslands = bake.treatAsMeshIslands,

                                        transferNormals = false,

                                        transferVertexColors = false,

                                        transferBoneWeights = false,

                                        transferUVs = true,
                                        preserveExistingUVData = false,
                                        uvTransferRange = new Vector2Int((int)bake.uvChannel, (int)bake.uvChannel),
                                        uvChannelIndexTransferOffset = 0, 

                                        transferBlendShapes = false,

                                        alignmentOffset = objectSetup.transferOffset,
                                        alignmentRotationOffsetEuler = objectSetup.transferRotationOffset
                                    };
                                    
                                    var tempMesh = MorphUtils.TransferSurfaceData(mainMesh, true, settings, surfaceDataTransferVertexData);

                                    baseMeshData.Dispose();

                                    uvs = tempMesh.GetUVsByChannelAsList(0);
                                }
                            } 
                            else
                            {
                                uvs = mainMesh.GetUVsByChannelAsList(bake.uvChannel);
                            }

                            if (uvs != null && uvs.Count > 0)
                            {
                                flag = true;

                                for(int i = 0; i < uvs.Count; i++)
                                {
                                    var uv = uvs[i];
                                    var vColor = vertexColors[i];

                                    var texColor = bake.texture.GetPixelBilinear(uv.x, uv.y);
                                    if (bake.targetTextureChannelsR != RGBAChannel.None)
                                    {
                                        vColor.r = texColor.GetChannel(bake.targetTextureChannelsR);
                                    }
                                    if (bake.targetTextureChannelsG != RGBAChannel.None)
                                    {
                                        vColor.g = texColor.GetChannel(bake.targetTextureChannelsG);
                                    }
                                    if (bake.targetTextureChannelsB != RGBAChannel.None)
                                    {
                                        vColor.b = texColor.GetChannel(bake.targetTextureChannelsB);
                                    }
                                    if (bake.targetTextureChannelsA != RGBAChannel.None)
                                    {
                                        vColor.a = texColor.GetChannel(bake.targetTextureChannelsA);
                                    }

                                    vertexColors[i] = vColor;
                                }
                            }
                        }

                        if (flag) mainMesh.colors = vertexColors;
                    }

                    #endregion

                    #region Final Data Transfers

                    var existingSetup = objectSetup.existingSetup;
                    if (existingSetup == null && baseMeshIndex >= 0)
                    {
                        existingSetup = outputDatas[baseMeshIndex];
                    }

                    if (surfaceDataTransferVertexData != null && existingSetup != null)
                    {
                        float[] tempWeights = new float[existingSetup.SerializedData.Mesh.vertexCount];
                        foreach (var vertexGroup in finalVertexGroups)
                        {
                            if (vertexGroup == null || (objectSetup.vertexGroupsToPreserve != null && objectSetup.vertexGroupsToPreserve.Contains(vertexGroup.name))) continue; 

                            var refGroupIndex = existingSetup.IndexOfVertexGroup(vertexGroup.name, true);
                            if (refGroupIndex >= 0)
                            {
                                var refGroup = existingSetup.GetVertexGroup(refGroupIndex);
                                if (refGroup != null)
                                {
                                    refGroup.AsLinearWeightArray(tempWeights);
                                    vertexGroup.Clear();
                                    for (int v = 0; v < surfaceDataTransferVertexData.Length; v++)
                                    {
                                        var transferData = surfaceDataTransferVertexData[v];
                                        float weight = (tempWeights[transferData.closestIndex0] * transferData.closestWeight0) + (tempWeights[transferData.closestIndex1] * transferData.closestWeight1) + (tempWeights[transferData.closestIndex2] * transferData.closestWeight2);
                                        if (weight != 0f) vertexGroup.SetWeight(v, weight);
                                    }
                                }
                            }
                        }

                        if (objectSetup.copyMeshShapeDataFromExistingSetup)
                        {
                            foreach(var meshShape in finalMeshShapes)
                            {
                                if (meshShape == null || meshShape.frames == null || (objectSetup.meshShapesToPreserve != null && objectSetup.meshShapesToPreserve.Contains(meshShape.name))) continue;

                                var refShapeIndex = existingSetup.IndexOfShape(meshShape.name, true);
                                if (refShapeIndex >= 0)
                                {
                                    var refShape = existingSetup.GetShape(refShapeIndex);
                                    if (refShape != null && refShape.frames != null)
                                    {
                                        for (int f = 0; f < Mathf.Min(meshShape.frames.Length, refShape.frames.Length); f++)
                                        {
                                            var localFrame = meshShape.frames[f];
                                            var refFrame = refShape.frames[f];
                                            for (int v = 0; v < surfaceDataTransferVertexData.Length; v++)
                                            {
                                                var transferData = surfaceDataTransferVertexData[v];
                                                localFrame.deltas[v] = (refFrame.deltas[transferData.closestIndex0] * transferData.closestWeight0) + (refFrame.deltas[transferData.closestIndex1] * transferData.closestWeight1) + (refFrame.deltas[transferData.closestIndex2] * transferData.closestWeight2);
                                            }
                                            meshShape.frames[f] = localFrame;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    #endregion

                    #region Create Final Meshes

                    Mesh outputMeshMain = null;
                    var baseVertices = mainMesh.vertices;
                    var baseColors = mainMesh.colors;
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

                        if (outputMeshMain == null) 
                        {
                            outputMeshMain = MeshUtils.DuplicateMesh(mainMesh);
                            outputMeshMain.name = mainMesh.name;
                        }
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

                                if (outputMesh == null) 
                                { 
                                    outputMesh = MeshUtils.DuplicateMesh(lodMesh);
                                    outputMesh.name = lodMesh.name;
                                }
                                outputMesh.SetUVs(3, uv3);

                                if ((!lodMesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Color) || !objectSetup.preserveLodMeshVertexColors) && (baseColors != null && baseColors.Length > 0))
                                {
                                    Color[] copyColors = new Color[outputMesh.vertexCount];
                                    for (int c = 0; c < outputMesh.vertexCount; c++)
                                    {
                                        int baseIndex = FetchIndexFromUV(uv3[c]); 
                                        if (baseIndex < 0) continue; 

                                        copyColors[c] = baseColors[baseIndex];
                                    }
                                    outputMesh.colors = copyColors;
                                }
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

                                outputMesh.name = objectSetup.name + $"_lod{(b + 1)}";
                                outputMesh.bounds = new Bounds() { center = boundsCenter, extents = boundsExtents };
                                outputMeshes.Add(outputMesh);
                            }
                            else outputMesh = lodMesh;

                            meshLODs.Add(new MeshLOD() { mesh = outputMesh, screenRelativeTransitionHeight = lod.screenRelativeTransitionHeight });
                        }

                        meshLODs.Sort((MeshLOD lodA, MeshLOD lodB) => Math.Sign(lodB.screenRelativeTransitionHeight - lodA.screenRelativeTransitionHeight)); // descending order
                    }
                    outputMeshMain.name = objectSetup.name;
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
                    serializedData.bustSizeMuscleShape = serializedData.IndexOfMeshShape(bustSizeMuscleShape);
                    serializedData.bustShapeShape = serializedData.IndexOfMeshShape(bustShapeShape);                 
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

                    if (primaryMeshObject == objectIndex)
                    {
                        var heightControl = outputPrefab.gameObject.GetComponent<BipedalCharacterHeight>();
                        if (heightControl != null) heightControl.CharacterMeshV2 = characterMesh;

                        var autoFlexer = outputPrefab.gameObject.GetComponent<CharacterMuscleAutoFlexer>();
                        if (autoFlexer != null) autoFlexer.characterMeshV2 = characterMesh;
                    }

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

                var characterMeshesCollection = outputPrefab.AddOrGetComponent<CustomizableCharacterMeshCollection>();
                foreach(var characterMesh in characterMeshes)
                {
                    characterMeshesCollection.AddToCollection(characterMesh.name, characterMesh);
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
                    EditorUtility.SetDirty(mesh);
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
                    EditorUtility.SetDirty(outputData);
                    AssetDatabase.SaveAssetIfDirty(outputData);
                }

                outputData.Precache();
            }

            var prefab = PrefabUtility.SaveAsPrefabAsset(outputPrefab, Extensions.CreateUnityAssetPathString(prefabSavePath, outputPrefab.name, ".prefab"), out bool success);
            if (success) outputPrefab = prefab; else Debug.LogError($"Failed to save prefab '{outputPrefab.name}'!");
#endif
            #endregion

            PostSave();

#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif
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