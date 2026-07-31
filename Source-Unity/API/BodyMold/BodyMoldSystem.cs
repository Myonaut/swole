using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

using Swole.Morphing;
using Swole.Cloth;
using Swole.DataStructures;
 

namespace Swole.API.Unity
{
    /// <summary>
    /// Runtime system to mold clothing to a body mesh using a compute shader.
    /// </summary>
    public class BodyMoldSystem : MonoBehaviour, IDisposable
    {
        public const string clothingMatProp_skinBindings = "_SkinBindings";
        public const string clothingMatProp_characterVertices = "_CharacterVertices";
        public const string clothingMatProp_clothingVertexDeltas = "_ClothingVertexDeltas";
        public const string clothingMatProp_clothingStretch = "_ClothingStretch";
        public const string clothingMatProp_bindingIndices = "_BindingIndices";
        public const string clothingMatProp_bindingWeights = "_BindingWeights";

        public const string bodyMatProp_finalWorldDeltas = "_FinalWorldDeltas"; 

        public static void PrewarmMaterialBuffers(Material mat, bool asProxy)
        {
            if (!asProxy)
            {
                mat.SetBuffer(clothingMatProp_skinBindings, PersistentJobDataTracker.GetEmptyGraphicsBuffer());
            }
            
            mat.SetBuffer(clothingMatProp_characterVertices, PersistentJobDataTracker.GetEmptyGraphicsBuffer());
            mat.SetBuffer(clothingMatProp_clothingVertexDeltas, PersistentJobDataTracker.GetEmptyGraphicsBuffer());
            mat.SetBuffer(clothingMatProp_clothingStretch, PersistentJobDataTracker.GetEmptyGraphicsBuffer());
            mat.SetBuffer(clothingMatProp_bindingIndices, PersistentJobDataTracker.GetEmptyGraphicsBuffer());
            mat.SetBuffer(clothingMatProp_bindingWeights, PersistentJobDataTracker.GetEmptyGraphicsBuffer());
        }

        public static string SdfIdToShapeId(Collision3dTexture.OutputCollision output) => output == null ? "default" : (string.IsNullOrWhiteSpace(output.MeshShapeName) ? "default" : ($"{output.MeshShapeName}{BoundMeshData.shapeIdFrameSuffix}.{output.MeshShapeFrame}"));  

        public Transform rootTransform;
        public Transform RootTransform => rootTransform == null ? transform.parent : rootTransform;

        public ComputeShader computeShader;
        public bool dontInstantiateShader;
        private bool shaderIsInstantiated;

        public Mesh tempMesh;

        [SerializeField]
        protected BodyMoldBindings bindings;
        protected Mesh mainMesh;

        [SerializeField]
        protected BodyMoldBindings maskBindings;

        [SerializeField]
        protected BodyMoldBindings pushBackVertexBindings;

        [SerializeField, Tooltip("Meshes bound to this mold mesh as their proxy.")]
        protected BoundMesh[] boundToProxy;
        public bool IsProxy => boundToProxy != null && boundToProxy.Length > 0;

        [Header("Algorithm Parameters")]
        public float preserveFactor = 0.3f;
        public float penetrationDistance = 0.01f;
        public float penetrationRecovery = 0.002f;
        [Range(0f, 1.5f)] public float forceDepenStrength = 1f;
        public float deltaMultiplier = 1.05f;
        public int collisionPasses = 4;
        public float collisionRelaxation = 0.3f;
        public int shapeChangeFollowUpIterations = 5;
        public float thickness = 0.03f;
        public float sdfOuterThicknessStrength = 0.05f;
        public float sdfOuterThicknessStrengthCloth = 0.08f;
        public bool recalculateNormalsAfterMold = true;
        [Range(0f, 1f)]
        public float normalRecalculationBlend = 0.7f;

        public enum CollisionMode { VertexCloud = 0, Triangle = 1, SDF = 2, TriangleSDF = 3 }
        public CollisionMode collisionMode = CollisionMode.VertexCloud;

        /// <summary>
        /// cbClothingOffsets: holds the current per-vertex offset/deformation applied to the clothing (Vector3 per vertex).
        /// </summary>
        ComputeBuffer cbClothingOffsetsA;
        ComputeBuffer cbClothingOffsetsB;
        ComputeBuffer cbClothingProxyDeltasA;
        ComputeBuffer cbClothingProxyDeltasB;
        bool bufferSwapFlag;

        ComputeBuffer stretchBuffer;

        ComputeBuffer dynamicBoneWeightsBuffer;

        ComputeBuffer worldSpaceVertexDataBuffer;
         
        protected int3 GetOffsetsBufferStartIndexAndGrow(int frameIndex)
        {
            if (mainMesh == null) return default;

            int startIndex = cbClothingOffsetsA == null ? 0 : cbClothingOffsetsA.count;
            int newCount = startIndex + mainMesh.vertexCount;
            var newBufferA = new ComputeBuffer(newCount, UnsafeUtility.SizeOf(typeof(MeshVertexDelta)));
            var newBufferB = new ComputeBuffer(newCount, UnsafeUtility.SizeOf(typeof(MeshVertexDelta)));
            var newProxyBufferA = new ComputeBuffer(newCount, UnsafeUtility.SizeOf(typeof(float3)));
            var newProxyBufferB = new ComputeBuffer(newCount, UnsafeUtility.SizeOf(typeof(float3)));
            // Copy existing data to new buffers
            if (cbClothingOffsetsA != null || cbClothingOffsetsB != null || cbClothingProxyDeltasA != null || cbClothingProxyDeltasB != null) 
            {
                MeshVertexDelta[] existingData = new MeshVertexDelta[newCount];

                if (cbClothingOffsetsA != null)
                {
                    cbClothingOffsetsA.GetData(existingData, 0, 0, cbClothingOffsetsA.count);
                    newBufferA.SetData(existingData);
                    cbClothingOffsetsA.Release(); 
                }

                if (cbClothingOffsetsB != null)
                {
                    cbClothingOffsetsB.GetData(existingData, 0, 0, cbClothingOffsetsB.count);
                    newBufferB.SetData(existingData);
                    cbClothingOffsetsB.Release();
                }

                float3[] existingProxyNormals = new float3[newCount];

                if (cbClothingProxyDeltasA != null)
                {
                    cbClothingProxyDeltasA.GetData(existingProxyNormals, 0, 0, cbClothingProxyDeltasA.count); 
                    newProxyBufferA.SetData(existingProxyNormals); 
                    cbClothingProxyDeltasA.Release();
                }
                if (cbClothingProxyDeltasB != null)
                {
                    cbClothingProxyDeltasB.GetData(existingProxyNormals, 0, 0, cbClothingProxyDeltasB.count);
                    newProxyBufferB.SetData(existingProxyNormals);
                    cbClothingProxyDeltasB.Release();
                }
            }

            if (cbClothingOffsetsA == null || cbClothingOffsetsB == null)
            {
                using(var emptyData = new NativeArray<MeshVertexDelta>(newCount, Allocator.Temp, NativeArrayOptions.ClearMemory))
                {
                    newBufferA.SetData(emptyData);
                    newBufferB.SetData(emptyData);
                }
            }

            if (cbClothingProxyDeltasA == null || cbClothingProxyDeltasB == null)
            {
                using (var emptyData = new NativeArray<float3>(newCount, Allocator.Temp, NativeArrayOptions.ClearMemory))
                {
                    newProxyBufferA.SetData(emptyData);
                    newProxyBufferB.SetData(emptyData);  
                }
            }

            // Assign new buffers
            cbClothingOffsetsA = newBufferA;
            cbClothingOffsetsB = newBufferB;
            cbClothingProxyDeltasA = newProxyBufferA;
            cbClothingProxyDeltasB = newProxyBufferB;


            return new int3(startIndex / vertexCount, frameIndex, startIndex);
        }

        /// <summary>
        /// Mapping from SDF texture ID to the offset in the clothing vertex offset buffer.
        /// </summary>
        protected readonly Dictionary<string, int3> shapeIdToBufferIndexOffsets = new Dictionary<string, int3>();

        [NonSerialized]
        protected Dictionary<Material, Material> clothingMaterials;
        public bool useTargetRendererMaterials;
        [SerializeField]
        protected MeshRenderer[] targetClothingRenderers;
        public int clothingFlexShapeStartIndex = 1;

        public IEnumerable<Material> AllClothingMaterials()
        {
            if (clothingMaterials != null)
            {
                foreach (var kvp in clothingMaterials) if (kvp.Value != null) yield return kvp.Value;
            }
            if (IsProxy)
            {
                foreach (var subMesh in boundToProxy)
                {
                    if (subMesh == null) continue;
                    foreach (var mat in subMesh.Materials()) yield return mat;
                }
            }
        }
        public IEnumerable<(Material, bool)> AllClothingMaterialsWithProxyFlag()
        {
            if (clothingMaterials != null)
            {
                foreach (var kvp in clothingMaterials) if (kvp.Value != null) yield return (kvp.Value, false); 
            }
            if (IsProxy)
            {
                foreach (var subMesh in boundToProxy)
                {
                    if (subMesh == null) continue;
                    foreach (var mat in subMesh.Materials()) yield return (mat, true);
                }
            }
        } 

        Vector3[] clothingOriginalPositions;
        Vector3[] clothingOriginalNormals;
        Vector4[] clothingOriginalTangents;
        // Per-mesh triangle indices when available (used by triangle kernel)

        int kernelHandle = -1;
        int clothKernelHandle = -1;
        int fallbackKernelHandle = -1;
        int fallbackClothKernelHandle = -1;
        int vertexCount = 0;

        protected int GetKernelHandle(bool hasSdf, out bool useSdf)
        {
            useSdf = false;
            switch (collisionMode)
            {
                case CollisionMode.TriangleSDF:
                    useSdf = hasSdf;
                    return hasSdf ? kernelHandle : fallbackKernelHandle;

                case CollisionMode.SDF:
                    useSdf = hasSdf;
                    return hasSdf ? kernelHandle : fallbackKernelHandle;
            }
             
            return kernelHandle;
        }
        protected int GetClothKernelHandle(bool hasSdf, out bool useSdf)
        {
            useSdf = false;
            switch (collisionMode)
            {
                case CollisionMode.TriangleSDF:
                    useSdf = hasSdf;
                    return hasSdf ? clothKernelHandle : fallbackClothKernelHandle;

                case CollisionMode.SDF:
                    useSdf = hasSdf;
                    return hasSdf ? clothKernelHandle : fallbackClothKernelHandle;
            }

            return clothKernelHandle;
        }

        // Per-bound-character mesh data
        protected class BoundMeshData
        {
            public int index;
            public BodyMoldBindings.BodyMeshBinding binding;

            public CustomizableCharacterMeshV2 attachedMeshV2;
            public int vertexCount;

            public int clothingVertexIndexCount;
            public ComputeBuffer cbBindingLocalIndices;
            public ComputeBuffer cbBindingIndices;
            public ComputeBuffer cbBindingWeights;
            public ComputeBuffer cbCollisionTriangles;
            public ComputeBuffer cbWorldSpaceVertexDataBuffer;
            public ComputeBuffer cbPushbackVertices;
            public bool pushbackVerticesIsLocal;
            public ComputeBuffer cbFinalDeltas;

            public InstanceBuffer<MeshVertexDelta> deltaInstanceBuffer;
            public InstanceBuffer<MeshVertexDelta> prevDeltaInstanceBuffer;

            public Collision3dTexture sdfTexture;
            public readonly Dictionary<string, int3> shapeIdToLocalDeltaBufferIndexOffsets = new Dictionary<string, int3>(); // x = shape index offset by frames of previous shapes, y = frame index, z = local delta buffer index offset

            public const string shapeIdFrameSuffix = ".frame";

            public bool TryAddShapeIdToLocalDeltaBufferIndexOffset(string shapeId, int frameIndex) => TryAddShapeIdToLocalDeltaBufferIndexOffset(shapeId, frameIndex, out _); 
            public bool TryAddShapeIdToLocalDeltaBufferIndexOffset(string shapeId, int frameIndex, out int3 offset)
            {
                offset = default;
                if (shapeIdToLocalDeltaBufferIndexOffsets.TryGetValue(shapeId, out offset)) return true;

                string shapeName = shapeId;
                int shapeIdFrameSuffixIndex = shapeName.IndexOf(shapeIdFrameSuffix);
                if (shapeIdFrameSuffixIndex >= 0) shapeName = shapeName.Substring(0, shapeIdFrameSuffixIndex); 
                
                int shapeIndex = attachedMeshV2.SubData.IndexOfShapeInBuffer(shapeName);
                if (shapeIndex >= 0)
                {
                    shapeIdToLocalDeltaBufferIndexOffsets[shapeId] = new int3(shapeIndex, frameIndex, (shapeIndex + frameIndex) * vertexCount);   
                    return true;
                } 
                //else
                //{
                    //shapeIdToLocalDeltaBufferIndexOffsets[shapeId] = new int3(0, 0, 0); 
                //}
                
                return false;
            }

            public UnityAction meshListener;
            public UnityAction vertexMaskRebuildListener;

            public bool hasChangedShape;
            public int iterationsSinceLastShapeChange; 
            public void MarkDirty() 
            { 
                hasChangedShape = true;
            } 
            public bool ConsumeDirtyFlag()
            {
                bool flag = hasChangedShape;
                if (flag)
                {
                    iterationsSinceLastShapeChange = 0;
                    hasChangedShape = false;
                } 
                else
                {
                    iterationsSinceLastShapeChange++;
                }
                return flag;
            }

            private readonly HashSet<string> evaluatedShapeIds = new HashSet<string>();
            public bool HasEvaluatedShapeId(string shapeId)
            {
                return evaluatedShapeIds.Contains(shapeId);
            }
            public void MarkShapeIdEvaluated(string sdfId)
            {
                evaluatedShapeIds.Add(sdfId);
            }

            public void Release()
            {
                // These buffers are persistent and come from the BodyMoldBindings asset, so we don't release them here. They will be released when the asset is destroyed.
                //if (cbBindingLocalIndices != null) { cbBindingLocalIndices.Release(); cbBindingLocalIndices = null; }
                //if (cbBindingIndices != null) { cbBindingIndices.Release(); cbBindingIndices = null; }
                //if (cbBindingWeights != null) { cbBindingWeights.Release(); cbBindingWeights = null; }
                //if (cbCollisionTriangles != null) { cbCollisionTriangles.Release(); cbCollisionTriangles = null; }
                //if (cbPushbackVertices != null) { cbPushbackVertices.Release(); cbPushbackVertices = null; }

                if (pushbackVerticesIsLocal && cbPushbackVertices != null) { cbPushbackVertices.Release(); cbPushbackVertices = null; } 
                if (cbWorldSpaceVertexDataBuffer != null) { cbWorldSpaceVertexDataBuffer.Release(); cbWorldSpaceVertexDataBuffer = null; } 
                if (cbFinalDeltas != null) { cbFinalDeltas.Release(); cbFinalDeltas = null; }
            }
        }

        List<BoundMeshData> boundMeshes = new List<BoundMeshData>();
        List<BoundMeshData> shapeContributingBoundMeshes = new List<BoundMeshData>(); 

        public void SetupFromBindings(BodyMoldBindings bindings)
        {
            if (bindings == null) throw new ArgumentNullException(nameof(bindings));

            this.bindings = bindings;
            this.mainMesh = bindings.GetMesh(0);
            // Initialize core buffers (clothing positions/offsets) and kernel
            Setup();

            stretchBuffer = new ComputeBuffer(mainMesh.vertexCount, sizeof(float));
            using (var emptyStretch = new NativeArray<float>(mainMesh.vertexCount, Allocator.Temp, NativeArrayOptions.ClearMemory))
            {
                stretchBuffer.SetData(emptyStretch);
            }

            if (bindings.dynamicBoneWeights)
            {
                dynamicBoneWeightsBuffer = new ComputeBuffer(bindings.BoneWeightsBuffer.count, UnsafeUtility.SizeOf(typeof(BoneWeight8Float)));
                ComputeBuffer.CopyCount(bindings.BoneWeightsBuffer, dynamicBoneWeightsBuffer, 0);  
            }

            worldSpaceVertexDataBuffer = new ComputeBuffer(mainMesh.vertexCount, UnsafeUtility.SizeOf(typeof(MeshVertexData)));

            // Bind per-body binding arrays. This requires runtime CustomizableCharacterMeshV2 instances to exist so we can attach instance delta buffers.
            var runtimeMeshes = RootTransform.GetComponentsInChildren<CustomizableCharacterMeshV2>(true);

            if (bindings.perBodyBindings != null)
            {
                int bodyCount = bindings.perBodyBindings.Length;
                for (int i = 0; i < bodyCount; ++i)
                {
                    var binding = bindings.perBodyBindings[i];

                    // Find matching runtime mesh by comparing bound Mesh asset reference
                    CustomizableCharacterMeshV2 match = null;
                    Mesh targetMesh = binding.mesh; 
                    if (targetMesh != null)
                    {
                        foreach (var rm in runtimeMeshes)
                        {
                            try
                            {
                                var sub = rm.SubData;
                                if (sub != null && sub.Mesh == targetMesh) { match = rm; break; }
                            }
                            catch { }
                        }
                    }

                    if (match == null)
                    {
                        var fallbackMeshName = binding.FallbackMeshName;
                        if (!string.IsNullOrWhiteSpace(fallbackMeshName)) 
                        {
                            foreach (var rm in runtimeMeshes)
                            {
                                try
                                {
                                    var sub = rm.SubData;
                                    if (sub != null && sub.Mesh != null && sub.Mesh.name == fallbackMeshName) { match = rm; break; }
                                }
                                catch { }
                            }
                        }
                    } 

                    if (match == null)
                    {
                        Debug.LogWarning($"BodyMoldSystem.SetupFromBindings: no runtime CustomizableCharacterMeshV2 found for body index {i}; skipping binding.\nEnsure the runtime mesh exists before calling SetupFromBindings.");
                        continue;
                    }

                    BindCharacterMesh(match, i); 
                }
            }
        }

        protected void InitClothingMaterial(Material material, bool asProxy)
        {
            if (material == null) return;

            if (asProxy)
            {
                material.SetInteger("_MeshProxyVertexCount", mainMesh.vertexCount);
            } 
            else
            {
                material.SetBuffer(clothingMatProp_skinBindings, bindings == null ? PersistentJobDataTracker.GetEmptyComputeBuffer() : bindings.BoneWeightsBuffer); 
            }

            material.SetBuffer(clothingMatProp_clothingVertexDeltas, PersistentJobDataTracker.GetEmptyGraphicsBuffer());
        }

        protected void UpdateClothingMaterial(Material material, bool asProxy, ComputeBuffer currentOffsetsBuffer)
        {
            if (material == null) return;

            material.SetBuffer(clothingMatProp_clothingVertexDeltas, currentOffsetsBuffer);
            material.SetInt("_ClothingFlexShapeIndexInBufferStart", clothingFlexShapeStartIndex);
        }
        protected void UpdateClothingMaterials(ComputeBuffer currentOffsetsBuffer)
        {
            if (clothingMaterials != null)
            {
                foreach (var kvp in clothingMaterials)
                {
                    UpdateClothingMaterial(kvp.Value, false, currentOffsetsBuffer);
                }
            }
            if (IsProxy)
            {
                foreach (var subMesh in boundToProxy)
                {
                    if (subMesh == null) continue;

                    foreach (var mat in subMesh.Materials()) UpdateClothingMaterial(mat, true, currentOffsetsBuffer); 
                }
            }
        }

        public void Prewarm()
        {
            if (clothingMaterials != null) return;

            Material[] materialsArray = null; 
            if (!useTargetRendererMaterials)
            {
                if (clothingMaterials == null) clothingMaterials = new Dictionary<Material, Material>();
                var baseMaterials = bindings.GetMaterials();
                materialsArray = new Material[baseMaterials.Length];
                for(int i = 0; i < baseMaterials.Length; i++)
                {
                    var origMat = baseMaterials[i];

                    if (origMat != null)
                    {
                        if (!clothingMaterials.TryGetValue(origMat, out var mat))
                        {
                            mat = Instantiate(origMat);
                            clothingMaterials[origMat] = mat;
                        }

                        materialsArray[i] = mat;
                    }
                }
            }

            if (targetClothingRenderers != null && targetClothingRenderers.Length > 0)
            {
                if (clothingMaterials == null) clothingMaterials = new Dictionary<Material, Material>();

                if (useTargetRendererMaterials)
                {
                    foreach (var rend in targetClothingRenderers)
                    {
                        if (rend == null) continue;

                        var origMats = rend.sharedMaterials;
                        for (int i = 0; i < origMats.Length; i++)
                        {
                            var mat = origMats[i];
                            if (mat == null) continue;

                            if (!clothingMaterials.TryGetValue(mat, out var instMat))
                            {
                                instMat = Instantiate(mat);
                                clothingMaterials[mat] = instMat;
                            }

                            origMats[i] = instMat;
                        }
                        rend.sharedMaterials = origMats;
                    }
                }
                else
                {
                    foreach (var rend in targetClothingRenderers)
                    {
                        if (rend == null) continue;
                        rend.sharedMaterials = materialsArray;
                    }
                }
            }

            if (clothingMaterials != null)
            {
                foreach (var kvp in clothingMaterials)
                {
                    PrewarmMaterialBuffers(kvp.Value, false);
                }
            }

            if (IsProxy)
            {
                foreach (var subMesh in boundToProxy)
                {
                    if (subMesh == null) continue;

                    subMesh.Initialize();
                    foreach (var mat in subMesh.Materials()) PrewarmMaterialBuffers(mat, true); 
                }
            }
        }

        // Setup with multiple body meshes. bodyTrianglesPerMesh can be null or contain triangle arrays parallel to bodies.
        public void Setup()
        {
            if (computeShader == null) throw new InvalidOperationException("ComputeShader not assigned.");

            if (!dontInstantiateShader && !shaderIsInstantiated)
            {
                computeShader = Instantiate(computeShader);
                shaderIsInstantiated = true;
            }

            if (targetClothingRenderers != null && targetClothingRenderers.Length > 0)
            {
                var lastTargetRenderer = targetClothingRenderers[targetClothingRenderers.Length - 1];
                var lodControllerParent = lastTargetRenderer.transform.parent;
                var lodController = lodControllerParent == null ? null : lodControllerParent.GetComponent<LODGroup>();
                if (lodController == null)
                {
                    var lodObj = new GameObject(lastTargetRenderer.name + "_LODGroup");
                    lodController = lodObj.AddComponent<LODGroup>();
                    if (lodControllerParent != null) lodObj.transform.SetParent(lodControllerParent, false); else
                    {
                        lodObj.transform.SetPositionAndRotation(lastTargetRenderer.transform.position, lastTargetRenderer.transform.rotation);
                        lodObj.transform.localScale = Vector3.one; 
                    }
                }

                LOD[] lods = new LOD[bindings.LodCount];
                for (int i = 0; i < bindings.LodCount; i++)
                {
                    var lodMesh = bindings.GetMeshLOD(i);
                    if (lodMesh.mesh == null) continue;

                    var lodRenderer = i >= targetClothingRenderers.Length ? Instantiate(lastTargetRenderer) : targetClothingRenderers[i];
                    if (lodRenderer != null)
                    {
                        var lodFilter = lodRenderer.gameObject.GetComponent<MeshFilter>();
                        if (lodFilter != null) lodFilter.sharedMesh = lodMesh.mesh;
                    }

                    lodRenderer.transform.SetParent(lodController.transform, true);
                    var lod = new LOD() { renderers = new Renderer[] { lodRenderer }, screenRelativeTransitionHeight = lodMesh.screenRelativeTransitionHeight  };
                    lods[i] = lod;
                }

                lodController.SetLODs(lods);
                lodController.RecalculateBounds(); 
            }

            if (clothingMaterials != null)
            {
                foreach (var kvp in clothingMaterials)
                {
                    InitClothingMaterial(kvp.Value, false);
                }
            }

            if (IsProxy)
            {
                foreach (var subMesh in boundToProxy)
                {
                    if (subMesh == null) continue;

                    subMesh.Initialize();
                    foreach (var mat in subMesh.Materials()) InitClothingMaterial(mat, true);
                }
            }

            vertexCount = mainMesh.vertexCount;

            // Release existing buffers if any
            ReleaseBuffers();

            // Choose specialized kernel based on selected collision mode to avoid runtime branching inside the shader.
            switch (collisionMode)
            {
                default:
                    kernelHandle = computeShader.FindKernel("BodyMold_VertexCloud");
                    fallbackKernelHandle = -1;
                    break;

                case CollisionMode.Triangle:
                    kernelHandle = computeShader.FindKernel("BodyMold_Triangle");
                    fallbackKernelHandle = -1;
                    break;

                case CollisionMode.SDF:
                    kernelHandle = computeShader.FindKernel("BodyMold_SDF");
                    fallbackKernelHandle = -1;
                    break;

                case CollisionMode.TriangleSDF:
                    kernelHandle = computeShader.FindKernel("BodyMold_TriangleSDF"); 
                    clothKernelHandle = computeShader.FindKernel("BodyMoldCloth_TriangleSDF");
                    fallbackKernelHandle = computeShader.FindKernel("BodyMold_Triangle");  
                    break;
            }

            // Set static params
            computeShader.SetFloat("_PenetrationDistance", penetrationDistance);
            computeShader.SetFloat("_PenetrationRecovery", penetrationRecovery);
            computeShader.SetInt("_VertexCount", vertexCount);
            // Collision mode is encoded by choosing the kernel; no runtime _CollisionMode value required.
        }

        protected bool initializedShader;
        protected LocalKeyword cskw_USE_PROXY_NORMAL_DELTAS;

        protected virtual void InitializeShader()
        {
            if (initializedShader) return;

            cskw_USE_PROXY_NORMAL_DELTAS = new LocalKeyword(computeShader, "USE_PROXY_NORMAL_DELTAS");
            computeShader.DisableKeyword(cskw_USE_PROXY_NORMAL_DELTAS);

            initializedShader = true;
        }

        protected virtual void PrepareShader()
        {
            InitializeShader();

            if (computeShader.IsKeywordEnabled(cskw_USE_PROXY_NORMAL_DELTAS))
            {
                if (!recalculateNormalsAfterMold)
                {
                    computeShader.DisableKeyword(cskw_USE_PROXY_NORMAL_DELTAS); 
                }
            } 
            else
            {
                if (recalculateNormalsAfterMold)
                {
                    computeShader.EnableKeyword(cskw_USE_PROXY_NORMAL_DELTAS);  
                }
            } 
        }
        protected virtual void PrepareShader(CommandBuffer cmd)
        {
            InitializeShader();

            if (recalculateNormalsAfterMold)
            {
                cmd.EnableKeyword(computeShader, cskw_USE_PROXY_NORMAL_DELTAS);
            } 
            else
            {
                cmd.DisableKeyword(computeShader, cskw_USE_PROXY_NORMAL_DELTAS);
            }
        }
        protected virtual void PrepareDispatch(CommandBuffer cmd, out int threadGroupSize)
        {
            if (kernelHandle < 0) throw new InvalidOperationException("Kernel not initialized");

            PrepareShader(cmd); 

            // update dynamic params
            cmd.SetComputeFloatParam(computeShader, "_PenetrationDistance", penetrationDistance);
            cmd.SetComputeFloatParam(computeShader, "_PenetrationRecovery", penetrationRecovery);
            cmd.SetComputeFloatParam(computeShader, "_PreserveFactor", preserveFactor);
            cmd.SetComputeFloatParam(computeShader, "_DeltaMultiplier", deltaMultiplier);
            cmd.SetComputeIntParam(computeShader, "_VertexCount", vertexCount);

            cmd.SetComputeFloatParam(computeShader, "_FrontPushFactor", forceDepenStrength);
            cmd.SetComputeFloatParam(computeShader, "_CollisionPasses", collisionPasses);
            cmd.SetComputeFloatParam(computeShader, "_CollisionRelaxation", collisionRelaxation);

            threadGroupSize = 256;
        }
        public void DispatchIterations(CommandBuffer cmd, int iterations, bool asCloth)
        {
            PrepareDispatch(cmd, out int threadGroupSize);

            for (int i = 0; i < iterations; i++)
            {
                for(int j = 0; j < shapeContributingBoundMeshes.Count; j++)
                {
                    var bm = shapeContributingBoundMeshes[j];
                    DispatchIteration(cmd, bm, threadGroupSize, j == shapeContributingBoundMeshes.Count - 1, asCloth); // swap buffers only after last mesh
                }
            }
        }
        public void DispatchIterations(int iteractions, bool asCloth)
        {
            var cmd = CommandBufferPool.Get($"{nameof(BodyMoldSystem)}.{nameof(DispatchIterations)}");

            DispatchIterations(cmd, iteractions, asCloth);
            Graphics.ExecuteCommandBuffer(cmd);

            cmd.Release();
        }

        public void DispatchIterations(CommandBuffer cmd, int boundMeshIndex, int iterations, bool asCloth) 
        { 
            if (boundMeshIndex < 0 || boundMeshIndex >= shapeContributingBoundMeshes.Count) throw new ArgumentOutOfRangeException(nameof(boundMeshIndex));
            DispatchIterations(cmd, shapeContributingBoundMeshes[boundMeshIndex], iterations, asCloth);
        }
        public void DispatchIterations(int boundMeshIndex, int iterations, bool asCloth)
        {
            if (boundMeshIndex < 0 || boundMeshIndex >= shapeContributingBoundMeshes.Count) throw new ArgumentOutOfRangeException(nameof(boundMeshIndex));
            DispatchIterations(shapeContributingBoundMeshes[boundMeshIndex], iterations, asCloth);
        }
        protected void DispatchIterations(CommandBuffer cmd, BoundMeshData bm, int iterations, bool asCloth)
        {
            PrepareDispatch(cmd, out int threadGroupSize);

            for (int i = 0; i < iterations; i++)
            {
                DispatchIteration(cmd, bm, threadGroupSize, true, asCloth);
            }
        }
        protected void DispatchIterations(BoundMeshData bm, int iterations, bool asCloth)
        {
            var cmd = CommandBufferPool.Get($"{nameof(BodyMoldSystem)}.{nameof(DispatchIterations)}");

            PrepareDispatch(cmd, out int threadGroupSize);

            for (int i = 0; i < iterations; i++)
            {
                DispatchIteration(cmd, bm, threadGroupSize, true, asCloth);
            }

            Graphics.ExecuteCommandBuffer(cmd);
            cmd.Release();
        }

        protected void SetDefaultShaderProperties(CommandBuffer cmd, BoundMeshData bm, int kernelHandle, ComputeShader computeShader, bool asCloth)
        {
            cmd.SetComputeBufferParam(computeShader, kernelHandle, "_ClothingBaseVertexData", bindings.VertexDataBuffer);

            cmd.SetComputeBufferParam(computeShader, kernelHandle, "_VertexConnections", bindings.VertexConnectionsBuffer);
            cmd.SetComputeBufferParam(computeShader, kernelHandle, "_VertexConnectionCounts", bindings.VertexConnectionCountsBuffer);
            cmd.SetComputeBufferParam(computeShader, kernelHandle, "_VertexConnectionStartIndices", bindings.VertexConnectionStartIndicesBuffer); 

            cmd.SetComputeBufferParam(computeShader, kernelHandle, "_ClothingVertexWeld", bindings.VertexWeldBuffer);

            // bind the character mesh vertex/triangle buffers provided by mesh.SubData
            var sub = bm.attachedMeshV2.SubData;
            if (sub != null)
            {
                cmd.SetComputeBufferParam(computeShader, kernelHandle, "_BodyVertexPositions", sub.VerticesBuffer);
                cmd.SetComputeBufferParam(computeShader, kernelHandle, "_BodyVertexNormals", sub.NormalsBuffer); 
                cmd.SetComputeBufferParam(computeShader, kernelHandle, "_TargetTriangles", sub.TrianglesBuffer);
                // set vertex count for bounds checks
                cmd.SetComputeIntParam(computeShader, "_BodyVertexCount", bm.vertexCount);
            }

            cmd.SetComputeBufferParam(computeShader, kernelHandle, "_BindingLocalIndices", bm.cbBindingLocalIndices);
            cmd.SetComputeIntParam(computeShader, "_ClothingVertexIndexCount", bm.clothingVertexIndexCount); 
            cmd.SetComputeFloatParam(computeShader, "_ClothThickness", thickness); 

            // bind the per-clothing binding arrays for this target mesh
            cmd.SetComputeBufferParam(computeShader, kernelHandle, "_BindingIndices", bm.cbBindingIndices);
            cmd.SetComputeBufferParam(computeShader, kernelHandle, "_BindingWeights", bm.cbBindingWeights);
            cmd.SetComputeBufferParam(computeShader, kernelHandle, "_NearbyTriangles", bm.cbCollisionTriangles);
            cmd.SetComputeBufferParam(computeShader, kernelHandle, "_ClothingTriangles", bindings.TrianglesBuffer);
            cmd.SetComputeBufferParam(computeShader, kernelHandle, "_ClothingVertexColors", bindings.VertexColorsBuffer);  

            // bind the instance buffers for current/previous deltas for this attached mesh (if present)
            if (bm.deltaInstanceBuffer != null)
            {
                cmd.SetComputeBufferParam(computeShader, kernelHandle, "_BodyDeltas", bm.deltaInstanceBuffer.BufferThisFrame);
            }
            if (bm.prevDeltaInstanceBuffer != null) 
            {
                cmd.SetComputeBufferParam(computeShader, kernelHandle, "_BodyPreviousDeltas", bm.prevDeltaInstanceBuffer.BufferThisFrame);  
            }

            if (asCloth)
            {
                cmd.SetComputeFloatParam(computeShader, "_SDFOuterThicknessStrengthCloth", sdfOuterThicknessStrengthCloth);
            } 
            else
            {
                cmd.SetComputeFloatParam(computeShader, "_SDFOuterThicknessStrength", sdfOuterThicknessStrength);
            }
        }
        protected virtual void DispatchIteration(CommandBuffer cmd, BoundMeshData bm, int threadGroupSize, bool swapBuffers, bool asCloth)
        {
            if (bm.binding == null || bm.binding.maskOnly) return;  

            bool hasSdf = bm.sdfTexture != null;
            bool useSdf;
            var kernelHandle = asCloth ? GetClothKernelHandle(hasSdf, out useSdf) : GetKernelHandle(hasSdf, out useSdf); 
            if (kernelHandle < 0) return;

            SetDefaultShaderProperties(cmd, bm, kernelHandle, computeShader, asCloth);

            // indicate whether this dispatch is the first after a body update (applies delta diff once)
            cmd.SetComputeIntParam(computeShader, "_BodyDeltasChanged", bm.ConsumeDirtyFlag() ? 1 : 0);  

            ComputeBuffer currentOffsetsBuffer;
            if (bufferSwapFlag)
            {
                currentOffsetsBuffer = cbClothingOffsetsA;
                cmd.SetComputeBufferParam(computeShader, kernelHandle, "_ClothingVertexDeltasReadOnly", cbClothingOffsetsB);
                cmd.SetComputeBufferParam(computeShader, kernelHandle, "_ClothingVertexDeltas", currentOffsetsBuffer);

                cmd.SetComputeBufferParam(computeShader, kernelHandle, "_ClothingProxyNormalDeltasReadOnly", recalculateNormalsAfterMold ? cbClothingProxyDeltasB : PersistentJobDataTracker.GetEmptyComputeBuffer());
                cmd.SetComputeBufferParam(computeShader, kernelHandle, "_ClothingProxyNormalDeltas", recalculateNormalsAfterMold ? cbClothingProxyDeltasA : PersistentJobDataTracker.GetEmptyComputeBuffer()); 

                if (bm.index == 0)
                {
                    UpdateClothingMaterials(currentOffsetsBuffer);
                }
            }
            else
            {
                currentOffsetsBuffer = cbClothingOffsetsB;
                cmd.SetComputeBufferParam(computeShader, kernelHandle, "_ClothingVertexDeltasReadOnly", cbClothingOffsetsA);
                cmd.SetComputeBufferParam(computeShader, kernelHandle, "_ClothingVertexDeltas", currentOffsetsBuffer);

                cmd.SetComputeBufferParam(computeShader, kernelHandle, "_ClothingProxyNormalDeltasReadOnly", recalculateNormalsAfterMold ? cbClothingProxyDeltasA : PersistentJobDataTracker.GetEmptyComputeBuffer());
                cmd.SetComputeBufferParam(computeShader, kernelHandle, "_ClothingProxyNormalDeltas", recalculateNormalsAfterMold ? cbClothingProxyDeltasB : PersistentJobDataTracker.GetEmptyComputeBuffer());   

                if (bm.index == 0)
                {
                    UpdateClothingMaterials(currentOffsetsBuffer);
                }
            }
            
            cmd.SetComputeBufferParam(computeShader, kernelHandle, "_ClothingStretch", stretchBuffer);

            foreach (var entry in shapeIdToBufferIndexOffsets)
            {
                string shapeId = entry.Key;
                var bufferOffset = entry.Value;
                cmd.SetComputeIntParam(computeShader, "_IndexOffset", bufferOffset.z);
                bool firstRun = !bm.HasEvaluatedShapeId(shapeId);
                cmd.SetComputeIntParam(computeShader, "_FirstRun", firstRun ? 1 : 0);
                if (firstRun) bm.MarkShapeIdEvaluated(shapeId);

                if (useSdf)
                {
                    bm.sdfTexture.ApplyToShader(shapeId, computeShader, kernelHandle); 
                }

                if (bufferOffset.x > 0 && bm.shapeIdToLocalDeltaBufferIndexOffsets.TryGetValue(shapeId, out int3 localBufferOffset)) 
                {
                    cmd.SetComputeBufferParam(computeShader, kernelHandle, "_Deltas", bm.attachedMeshV2.SubData.MeshShapeFrameDeltasBuffer);
                    cmd.SetComputeIntParam(computeShader, "_DeltasIndexOffset", localBufferOffset.z);
                }
                else
                {
                    cmd.SetComputeBufferParam(computeShader, kernelHandle, "_Deltas", PersistentJobDataTracker.GetEmptyGraphicsBuffer());
                    cmd.SetComputeIntParam(computeShader, "_DeltasIndexOffset", -1); 
                }

                int dispatchGroups = Mathf.CeilToInt(bm.clothingVertexIndexCount / (float)threadGroupSize); 
                cmd.DispatchCompute(computeShader, kernelHandle, dispatchGroups, 1, 1);
            }

            if (bindings.dynamicBoneWeights && bm.index == 0)
            {
                var recalBwKernel = computeShader.FindKernel("AdjustSkinBindings"); 

                SetDefaultShaderProperties(cmd, bm, recalBwKernel, computeShader, asCloth);
                cmd.SetComputeIntParam(computeShader, "_IndexOffset", 0);
                cmd.SetComputeBufferParam(computeShader, recalBwKernel, "_ClothingVertexDeltasReadOnly", currentOffsetsBuffer); 
                cmd.SetComputeBufferParam(computeShader, recalBwKernel, "_SkinBindingsReadOnly", bindings.BoneWeightsBuffer);
                cmd.SetComputeBufferParam(computeShader, recalBwKernel, "_SkinBindings", dynamicBoneWeightsBuffer);
                cmd.SetComputeBufferParam(computeShader, recalBwKernel, "_BodySkinBindings", bm.attachedMeshV2.SubData.BoneWeightsBuffer);
                cmd.SetComputeBufferParam(computeShader, recalBwKernel, "_Deltas", PersistentJobDataTracker.GetEmptyGraphicsBuffer()); 
                cmd.SetComputeIntParam(computeShader, "_DeltasIndexOffset", -1);

                int dispatchGroups = Mathf.CeilToInt(bm.clothingVertexIndexCount / (float)threadGroupSize);
                cmd.DispatchCompute(computeShader, recalBwKernel, dispatchGroups, 1, 1);
            }

            if (swapBuffers) bufferSwapFlag = !bufferSwapFlag;
        }

        void OnDestroy() 
        { 
            Dispose();

            if (boundMeshes != null && boundMeshes.Count > 0)
            {
                if (shapeContributingBoundMeshes.Count > 0)
                {
                    var bm = shapeContributingBoundMeshes[0];
                    if (bm.attachedMeshV2 != null)
                    {
                        var allClothingMaterials = AllClothingMaterials();

                        bm.attachedMeshV2.UnbindSkinningMatricesBufferFromMaterials(allClothingMaterials);
                        bm.attachedMeshV2.UnbindStandaloneShapesControlBufferFromMaterials(allClothingMaterials);
                        bm.attachedMeshV2.UnbindMuscleGroupsControlBufferFromMaterials(allClothingMaterials);
                        bm.attachedMeshV2.UnbindFatGroupsControlBufferFromMaterials(allClothingMaterials);
                    }
                }

                boundMeshes.Clear();
                shapeContributingBoundMeshes.Clear();
            }

            if (clothingMaterials != null)
            {
                foreach(var kvp in clothingMaterials)
                {
                    if (kvp.Value != null) Destroy(kvp.Value);
                }
                clothingMaterials.Clear(); 
                clothingMaterials = null; 
            }

            if (shaderIsInstantiated)
            {
                Destroy(computeShader);
                computeShader = null; 
            }
        }

        public void Dispose()
        {
            ReleaseBuffers(); 
        }

        void ReleaseBuffers()
        {
            if (cbClothingOffsetsA != null) { cbClothingOffsetsA.Release(); cbClothingOffsetsA = null; } 
            if (cbClothingOffsetsB != null) { cbClothingOffsetsB.Release(); cbClothingOffsetsB = null; }
            if (cbClothingProxyDeltasA != null) { cbClothingProxyDeltasA.Release(); cbClothingProxyDeltasA = null; }
            if (cbClothingProxyDeltasB != null) { cbClothingProxyDeltasB.Release(); cbClothingProxyDeltasB = null; } 

            if (stretchBuffer != null) { stretchBuffer.Release(); stretchBuffer = null; }

            if (dynamicBoneWeightsBuffer != null) { dynamicBoneWeightsBuffer.Release(); dynamicBoneWeightsBuffer = null; }

            if (worldSpaceVertexDataBuffer != null) { worldSpaceVertexDataBuffer.Release(); worldSpaceVertexDataBuffer = null; }

            if (boundMeshes != null)
            {
                foreach (var bm in boundMeshes)
                {
                    if (bm == null) continue;
                    bm.Release();
                }
                boundMeshes.Clear();
            }
            if (shapeContributingBoundMeshes != null) shapeContributingBoundMeshes.Clear();

            shapeIdToBufferIndexOffsets.Clear(); 
        }

        protected IEnumerator BindCharacterMeshRoutine(CustomizableCharacterMeshV2 meshV2, int index)
        {
            if (bindings == null) throw new InvalidOperationException("Call SetupFromBindings(...) before binding character meshes.");

            if (meshV2 == null) throw new ArgumentNullException(nameof(meshV2));

            while (!meshV2.HasValidInstance) yield return null; 

            var subData = meshV2.SubData;

            var bm = new BoundMeshData();
            bm.index = index;
            bm.attachedMeshV2 = meshV2;
            bm.vertexCount = subData != null ? subData.VertexCount : 0;

            var binding = bindings.perBodyBindings[index];
            bm.clothingVertexIndexCount = binding.BindingMaskLength;
            bm.binding = binding;
            bool isMaskOnly = binding.maskOnly;
            if (!isMaskOnly)
            {
                bm.cbBindingLocalIndices = binding.LocalIndicesBuffer;
                bm.cbBindingIndices = binding.IndicesBuffer;
                bm.cbBindingWeights = binding.WeightsBuffer;
                bm.cbCollisionTriangles = binding.CollisionTrianglesBuffer;
            }

            var maskBinding = binding;
            if (maskBindings != null && maskBindings.perBodyBindings != null)
            {
                foreach(var b in maskBindings.perBodyBindings)
                {
                    if (ReferenceEquals(b.mesh, binding.mesh))
                    {
                        maskBinding = b;
                        break;
                    }
                }
            }

            var pushBackBinding = binding;
            bool pushBackBindingIsDifferent = false;
            if (pushBackVertexBindings != null && pushBackVertexBindings.perBodyBindings != null)
            {
                foreach (var b in pushBackVertexBindings.perBodyBindings)
                {
                    if (ReferenceEquals(b.mesh, binding.mesh))
                    {
                        pushBackBinding = b;
                        pushBackBindingIsDifferent = true;
                        break;
                    }
                }
            }

            if (!isMaskOnly && pushBackBinding.HasPushBackVertices)
            {
                if (pushBackBindingIsDifferent)
                {
                    bm.pushbackVerticesIsLocal = true;
                    using (var pushBackVertices = new NativeList<PushBackVertex>(pushBackBinding.pushBackVertices.Length, Allocator.Temp))
                    {
                        for(int i = 0; i < bm.binding.pushBackVertices.Length; i++)
                        {
                            var pushBackVertex = bm.binding.pushBackVertices[i];
                            for(int j = 0; j < pushBackBinding.pushBackVertices.Length; j++)
                            {
                                var pushBackVertex2 = pushBackBinding.pushBackVertices[j];
                                if (pushBackVertex.vertexIndex == pushBackVertex2.vertexIndex)
                                {
                                    pushBackVertices.Add(pushBackVertex);
                                    break;
                                }
                            }
                        }

                        bm.cbPushbackVertices = new ComputeBuffer(pushBackVertices.Length, UnsafeUtility.SizeOf(typeof(PushBackVertex)), ComputeBufferType.Structured, ComputeBufferMode.Immutable);
                        bm.cbPushbackVertices.SetData(pushBackVertices.AsArray()); 
                    }
                }
                else
                {
                    bm.pushbackVerticesIsLocal = false;
                    bm.cbPushbackVertices = pushBackBinding.PushBackVerticesBuffer;
                }

                bm.cbWorldSpaceVertexDataBuffer = new ComputeBuffer(subData.VertexCount, UnsafeUtility.SizeOf(typeof(MeshVertexData)));
                bm.cbFinalDeltas = new ComputeBuffer(subData.VertexCount, UnsafeUtility.SizeOf(typeof(MeshVertexDelta)));
                using (var initData = new NativeArray<MeshVertexDelta>(subData.VertexCount, Allocator.Temp, NativeArrayOptions.ClearMemory))
                {
                    bm.cbFinalDeltas.SetData(initData);
                }

                var mats = meshV2.MaterialInstances;
                if (mats != null)
                {
                    foreach (var mat in mats)
                    {
                        if (mat == null) continue;
                        //mat.EnableKeyword("USE_FINAL_WORLD_DELTAS");
                        mat.SetBuffer(bodyMatProp_finalWorldDeltas, bm.cbFinalDeltas);
                    }
                }
            }

            if (!isMaskOnly)
            {
                bm.sdfTexture = meshV2.GetComponent<Collision3dTexture>();
                if (bm.sdfTexture != null)
                {
                    foreach (var output in bm.sdfTexture.Outputs())
                    {
                        string shapeId = SdfIdToShapeId(output);
                        int shapeFrame = output.HasMeshShape ? output.MeshShapeFrame : 0;
                        if (!shapeIdToBufferIndexOffsets.ContainsKey(shapeId))
                        {
                            shapeIdToBufferIndexOffsets[shapeId] = GetOffsetsBufferStartIndexAndGrow(shapeFrame);

                            if (output.HasMeshShape)
                            {
                                foreach (var bm2 in shapeContributingBoundMeshes)
                                {
                                    if (bm2 == null || ReferenceEquals(bm, bm2)) continue;
                                    bm2.TryAddShapeIdToLocalDeltaBufferIndexOffset(shapeId, shapeFrame);
                                }
                            }
                        }

                        if (!output.HasMeshShape)
                        {
                            bm.shapeIdToLocalDeltaBufferIndexOffsets[shapeId] = new int3(0, 0, 0);
                        }
                    }
                }


                foreach (var shapeEntry in shapeIdToBufferIndexOffsets)
                {
                    var shapeId = shapeEntry.Key;
                    var bufferOffset = shapeEntry.Value;

                    if (bufferOffset.x <= 0) continue; // is default shape

                    if (!bm.shapeIdToLocalDeltaBufferIndexOffsets.ContainsKey(shapeId))
                    {
                        bm.TryAddShapeIdToLocalDeltaBufferIndexOffset(shapeId, bufferOffset.y);
                    }
                }

                // attach instance buffers if available
                meshV2.TryGetInstanceBuffer<MeshVertexDelta>(subData.PerVertexDeltaDataPropertyName, out bm.deltaInstanceBuffer);
                meshV2.TryGetInstanceBuffer<MeshVertexDelta>(subData.PerVertexPreviousDeltaDataPropertyName, out bm.prevDeltaInstanceBuffer);

            }

            boundMeshes.Add(bm);
            if (!isMaskOnly) shapeContributingBoundMeshes.Add(bm);

            if (!isMaskOnly)
            {

                bm.meshListener = () =>
                {
                    bm.MarkDirty();
                    if (!enabled || !gameObject.activeInHierarchy) return;

                    StartCoroutine(OnBodyShapeChanged(bm));
                };
                meshV2.AddListener(API.Unity.ICustomizableCharacter.ListenableEvent.OnAnyDataChanged, bm.meshListener);

                if (index == 0)
                {
                    var allClothingMaterials = AllClothingMaterials();

                    //float cx = math.cos(-bm.eulerOffsetRadians.x);
                    //float sx = math.sin(-bm.eulerOffsetRadians.x);
                    //float cy = math.cos(-bm.eulerOffsetRadians.y);
                    //float sy = math.sin(-bm.eulerOffsetRadians.y);
                    //float cz = math.cos(-bm.eulerOffsetRadians.z);
                    //float sz = math.sin(-bm.eulerOffsetRadians.z);

                    meshV2.BindSkinningMatricesBufferToMaterials(allClothingMaterials);
                    meshV2.BindStandaloneShapesControlBufferToMaterials(allClothingMaterials);
                    meshV2.BindMuscleGroupsControlBufferToMaterials(allClothingMaterials);
                    meshV2.BindFatGroupsControlBufferToMaterials(allClothingMaterials);
                    foreach (var mat in AllClothingMaterialsWithProxyFlag())
                    {
                        if (mat.Item1 == null) continue;

                        meshV2.MeshGroup2.ApplyMainMaterialOverrides(mat.Item1, false);

                        mat.Item1.SetInt(subData.RigInstanceIDPropertyName, meshV2.RigInstanceID);
                        mat.Item1.SetInt(subData.CharacterInstanceIDPropertyName, meshV2.CharacterInstanceID);
                        mat.Item1.SetFloat(subData.BustMixPropertyName, Mathf.Clamp01(meshV2.BustSize));
                        mat.Item1.SetBuffer(subData.MeshShapeIndicesPropertyName, subData.MeshShapeIndicesBuffer);
                        mat.Item1.SetBuffer(subData.MeshShapeFrameDeltasPropertyName, subData.MeshShapeFrameDeltasBuffer);
                        mat.Item1.SetBuffer(subData.MuscleGroupInfluencesPropertyName, subData.MuscleGroupInfluencesBuffer);
                        mat.Item1.SetBuffer(subData.FatGroupInfluencesPropertyName, subData.FatGroupInfluencesBuffer);
                        mat.Item1.SetInteger(subData.FlexShapeIndexPropertyName, subData.FlexShape);
                        mat.Item1.SetInteger("_CharacterVertexCount", subData.VertexCount);
                        mat.Item1.SetBuffer("_CharacterVertices", subData.VerticesBuffer);
                        mat.Item1.SetBuffer("_BindingIndices", bm.cbBindingIndices);
                        mat.Item1.SetBuffer("_BindingWeights", bm.cbBindingWeights);
                        mat.Item1.SetBuffer("_ClothingStretch", stretchBuffer);

                        if (!mat.Item2)
                        {
                            mat.Item1.SetInt("_VertexCount", vertexCount);
                            Debug.Log("Binding clothing material to skin bindings buffer: " + mat.Item1.name);
                            mat.Item1.SetBuffer(clothingMatProp_skinBindings, dynamicBoneWeightsBuffer == null ? bindings.BoneWeightsBuffer : dynamicBoneWeightsBuffer);
                        }
                    }


                    var referenceRenderer = meshV2.FirstRenderer;
                    if (referenceRenderer != null)
                    {
                        if (targetClothingRenderers != null && targetClothingRenderers.Length > 0)
                        {
                            foreach (var rend in targetClothingRenderers)
                            {
                                if (rend == null) continue;
                                rend.localBounds = referenceRenderer.localBounds;
                            }

                        }
                        if (boundToProxy != null && boundToProxy.Length > 0)
                        {
                            foreach (var subMesh in boundToProxy)
                            {
                                foreach (var rend in subMesh.Renderers())
                                {
                                    if (rend == null) continue;
                                    rend.localBounds = referenceRenderer.localBounds;
                                }
                            }
                        }
                    }
                }

            }

            if (binding.HasMaskedVertices)
            {
                bm.attachedMeshV2.MeshGroup2.UseVertexMask = true;

                bm.vertexMaskRebuildListener = () =>
                {
                    if (!enabled || !gameObject.activeInHierarchy) return;
                    bm.attachedMeshV2.WriteToVertexMask(maskBinding.maskedVertices); 
                    bm.attachedMeshV2.MeshGroup2.ApplyVertexMaskMaterialOverrides(bm.attachedMeshV2.MaterialInstances); 
                };

                bm.attachedMeshV2.ListenForVertexMaskRebuild(bm.vertexMaskRebuildListener); 
                bm.vertexMaskRebuildListener(); // <- apply the mask immediately
            }
        }
        public void BindCharacterMesh(CustomizableCharacterMeshV2 meshV2, int index)
        {
            StartCoroutine(BindCharacterMeshRoutine(meshV2, index)); 
        }

        protected void RecalculateNormals()
        {
            var cmd = CommandBufferPool.Get($"{nameof(BodyMoldSystem)}.{nameof(RecalculateNormals)}");

            RecalculateNormals(cmd);

            Graphics.ExecuteCommandBuffer(cmd);
            cmd.Release();
        }
        protected void RecalculateNormals(CommandBuffer cmd)
        {
            foreach (var bm in shapeContributingBoundMeshes)
            {
                if (bm == null) continue; 

                RecalculateNormals(cmd, bm);
            }
        } 

        protected void RecalculateNormals(BoundMeshData bm)
        {
            if (bm.binding == null || bm.binding.maskOnly) return; 

            var cmd = CommandBufferPool.Get($"{nameof(BodyMoldSystem)}.{nameof(RecalculateNormals)}");

            RecalculateNormals(cmd, bm);

            Graphics.ExecuteCommandBuffer(cmd);
            cmd.Release();
        }
        protected void RecalculateNormals(CommandBuffer cmd, BoundMeshData bm)
        {
            if (bm.binding == null || bm.binding.maskOnly) return;

            var kernel = computeShader.FindKernel("BodyMold_RecalculateNormals");  

            SetDefaultShaderProperties(cmd, bm, kernel, computeShader, false); 

            cmd.SetComputeFloatParam(computeShader, "_NormalRecalculationBlend", normalRecalculationBlend); 

            ComputeBuffer currentOffsetsBuffer;
            if (bufferSwapFlag)
            {
                currentOffsetsBuffer = cbClothingOffsetsA;
                cmd.SetComputeBufferParam(computeShader, kernel, "_ClothingVertexDeltasReadOnly", cbClothingOffsetsB);
                cmd.SetComputeBufferParam(computeShader, kernel, "_ClothingVertexDeltas", currentOffsetsBuffer);

                if (bm.index == 0)
                {
                    UpdateClothingMaterials(currentOffsetsBuffer);
                }
            }
            else
            {
                currentOffsetsBuffer = cbClothingOffsetsB;
                cmd.SetComputeBufferParam(computeShader, kernel, "_ClothingVertexDeltasReadOnly", cbClothingOffsetsA); 
                cmd.SetComputeBufferParam(computeShader, kernel, "_ClothingVertexDeltas", currentOffsetsBuffer);

                if (bm.index == 0)
                {
                    UpdateClothingMaterials(currentOffsetsBuffer);
                }
            }

            foreach (var entry in shapeIdToBufferIndexOffsets)
            {
                string shapeId = entry.Key;
                var bufferOffset = entry.Value;
                cmd.SetComputeIntParam(computeShader, $"_IndexOffset", bufferOffset.z);

                if (bufferOffset.x > 0 && bm.shapeIdToLocalDeltaBufferIndexOffsets.TryGetValue(shapeId, out int3 localBufferOffset))
                {
                    cmd.SetComputeBufferParam(computeShader, kernelHandle, "_Deltas", bm.attachedMeshV2.SubData.MeshShapeFrameDeltasBuffer); 
                    cmd.SetComputeIntParam(computeShader, "_DeltasIndexOffset", localBufferOffset.z);
                }
                else
                {
                    cmd.SetComputeBufferParam(computeShader, kernelHandle, "_Deltas", PersistentJobDataTracker.GetEmptyGraphicsBuffer());
                    cmd.SetComputeIntParam(computeShader, "_DeltasIndexOffset", -1);
                }

                int dispatchGroups = Mathf.CeilToInt(bm.clothingVertexIndexCount / 256f);  
                cmd.DispatchCompute(computeShader, kernelHandle, dispatchGroups, 1, 1);   
            }

            //bufferSwapFlag = !bufferSwapFlag;
            // DO NOT SWAP BUFFER FLAG
            // RecalculateNormals only modifies normals in the main buffer for rendering
            // It should not interfere with the molding ping-pong cycle
            // The molding kernel controls the buffer swap
        }

        protected int queuedIterations;
        protected int queuedClothIterations;

        protected IEnumerator OnBodyShapeChanged(BoundMeshData bm) 
        {
            yield return null;

            queuedIterations = 1 + shapeChangeFollowUpIterations; 
            queuedClothIterations = 1 + shapeChangeFollowUpIterations; 
        } 

        protected void PrepareLiveClothingVertexData(CommandBuffer cmd)
        {
            bool isLocalCommandBuffer = cmd == null;
            if (isLocalCommandBuffer) cmd = CommandBufferPool.Get($"{nameof(BodyMoldSystem)}.{nameof(PrepareLiveClothingVertexData)}");

            PrepareShader(cmd);

            var kernel = computeShader.FindKernel("PrepareLiveVertexData");

            ComputeBuffer currentOffsetsBuffer;
            ComputeBuffer proxyNormalOffsetsBuffer;
            if (bufferSwapFlag)
            {
                currentOffsetsBuffer = cbClothingOffsetsB;
                if (recalculateNormalsAfterMold) proxyNormalOffsetsBuffer = cbClothingProxyDeltasB; else proxyNormalOffsetsBuffer = PersistentJobDataTracker.GetEmptyComputeBuffer(); 
            }
            else
            {
                currentOffsetsBuffer = cbClothingOffsetsA;
                if (recalculateNormalsAfterMold) proxyNormalOffsetsBuffer = cbClothingProxyDeltasA; else proxyNormalOffsetsBuffer = PersistentJobDataTracker.GetEmptyComputeBuffer(); 
            }

            cmd.SetComputeBufferParam(computeShader, kernel, "_LiveBodyVertexData", bindings.VertexDataBuffer); // <- despite the property name, this is the original clothing vertex data buffer
            cmd.SetComputeBufferParam(computeShader, kernel, "_Deltas", currentOffsetsBuffer);
            cmd.SetComputeBufferParam(computeShader, kernel, "_ClothingProxyNormalDeltasReadOnly", proxyNormalOffsetsBuffer);  
            cmd.SetComputeBufferParam(computeShader, kernel, "_SkinBindingsReadOnly", bindings.BoneWeightsBuffer);
            cmd.SetComputeBufferParam(computeShader, kernel, "_SkinningMatrices", shapeContributingBoundMeshes[0].attachedMeshV2.SkinningMatricesBuffer.BufferThisFrame);
            cmd.SetComputeBufferParam(computeShader, kernel, "_OutputVertexData", worldSpaceVertexDataBuffer);
            cmd.SetComputeIntParam(computeShader, "_InstanceID", 0);
            cmd.SetComputeIntParam(computeShader, "_BoneCount", shapeContributingBoundMeshes[0].attachedMeshV2.SubData.BoneCount); 
            cmd.SetComputeIntParam(computeShader, "_VertexCount", mainMesh.vertexCount);

            int dispatchGroups = Mathf.CeilToInt(mainMesh.vertexCount / 256f);
            cmd.DispatchCompute(computeShader, kernel, dispatchGroups, 1, 1);

            if (isLocalCommandBuffer)
            {
                Graphics.ExecuteCommandBuffer(cmd);
                cmd.Release();
            }
        }

        protected void PrepareLiveBodyMeshVertexData(CommandBuffer cmd, BoundMeshData bm)
        {
            bool isLocalCommandBuffer = cmd == null;
            if (isLocalCommandBuffer) cmd = CommandBufferPool.Get($"{nameof(BodyMoldSystem)}.{nameof(PrepareLiveBodyMeshVertexData)}");

            var kernel = computeShader.FindKernel("PrepareLiveVertexData");

            cmd.SetComputeBufferParam(computeShader, kernel, "_LiveBodyVertexData", bm.attachedMeshV2.SubData.VertexDataBuffer);
            cmd.SetComputeBufferParam(computeShader, kernel, "_Deltas", bm.deltaInstanceBuffer.BufferThisFrame);
            cmd.SetComputeBufferParam(computeShader, kernel, "_SkinBindingsReadOnly", bm.attachedMeshV2.SubData.BoneWeightsBuffer);
            cmd.SetComputeBufferParam(computeShader, kernel, "_SkinningMatrices", bm.attachedMeshV2.SkinningMatricesBuffer.BufferThisFrame);
            cmd.SetComputeBufferParam(computeShader, kernel, "_OutputVertexData", bm.cbWorldSpaceVertexDataBuffer); 
            cmd.SetComputeIntParam(computeShader, "_InstanceID", 0);
            cmd.SetComputeIntParam(computeShader, "_BoneCount", bm.attachedMeshV2.SubData.BoneCount);
            cmd.SetComputeIntParam(computeShader, "_VertexCount", bm.attachedMeshV2.SubData.VertexCount);

            int dispatchGroups = Mathf.CeilToInt(bm.attachedMeshV2.SubData.VertexCount / 256f);
            cmd.DispatchCompute(computeShader, kernel, dispatchGroups, 1, 1);

            if (isLocalCommandBuffer)
            {
                Graphics.ExecuteCommandBuffer(cmd);
                cmd.Release();
            }
        }

        protected void PushBackBodyVertices(CommandBuffer cmd, BoundMeshData bm)
        {
            return;
            bool isLocalCommandBuffer = cmd == null;
            if (isLocalCommandBuffer) cmd = CommandBufferPool.Get($"{nameof(BodyMoldSystem)}.{nameof(PushBackBodyVertices)}");

            var kernel = computeShader.FindKernel("PushBackBodyFromClothing");

            cmd.SetComputeIntParam(computeShader, "_InstanceID", 0);
            cmd.SetComputeIntParam(computeShader, "_VertexCount", bm.binding.PushBackVerticesCount);
            cmd.SetComputeIntParam(computeShader, "_BodyVertexCount", bm.attachedMeshV2.SubData.VertexCount);

            cmd.SetComputeBufferParam(computeShader, kernel, "_TargetTriangles", bindings.TrianglesBuffer);

            cmd.SetComputeBufferParam(computeShader, kernel, "_LiveBodyVertexData", bm.cbWorldSpaceVertexDataBuffer);
            cmd.SetComputeBufferParam(computeShader, kernel, "_LiveClothVertexData", worldSpaceVertexDataBuffer);

            cmd.SetComputeBufferParam(computeShader, kernel, "_AnchorMap", bm.cbPushbackVertices);

            cmd.SetComputeBufferParam(computeShader, kernel, "_PushBackDeltas", bm.cbFinalDeltas); 

            int dispatchGroups = Mathf.CeilToInt(bm.binding.PushBackVerticesCount / 64f);
            cmd.DispatchCompute(computeShader, kernel, dispatchGroups, 1, 1);

            if (isLocalCommandBuffer)
            {
                Graphics.ExecuteCommandBuffer(cmd);
                cmd.Release();
            }
        }

        // Unbind and release buffers associated with a previously bound character mesh
        public bool UnbindCharacterMesh(CustomizableCharacterMeshV2 meshV2) 
        {
            if (meshV2 == null) return false;
            if (boundMeshes == null) return false;
            for(int i = shapeContributingBoundMeshes.Count - 1; i >= 0; --i)
            {
                var bm = shapeContributingBoundMeshes[i];
                if (bm == null) continue;
                if (bm.attachedMeshV2 == meshV2)
                {
                    shapeContributingBoundMeshes.RemoveAt(i);
                    break;
                }
            }
            for (int i = boundMeshes.Count - 1; i >= 0; --i)
            {
                var bm = boundMeshes[i];
                if (bm == null) continue;
                if (bm.attachedMeshV2 == meshV2)
                {
                    bm.Release();
                    boundMeshes.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        public void Awake()
        {
            Prewarm();
        }

        public void Start()
        {
            if (bindings != null) SetupFromBindings(bindings);
        }

        protected void OnEnable()
        {
            if (boundMeshes != null)
            {
                foreach (var bm in boundMeshes)
                {
                    if (bm == null || bm.attachedMeshV2 == null) continue;
                    if (bm.meshListener != null && bm.binding != null && !bm.binding.maskOnly) bm.attachedMeshV2.AddListener(API.Unity.ICustomizableCharacter.ListenableEvent.OnAnyDataChanged, bm.meshListener);
                    if (bm.vertexMaskRebuildListener != null) 
                    { 
                        bm.attachedMeshV2.ListenForVertexMaskRebuild(bm.vertexMaskRebuildListener); 
                        bm.vertexMaskRebuildListener();
                    }
                }

                ComputeBufferPoolUploader.ListenPostUpload(UpdateA);
                ComputeBufferPoolUploader.ListenPostUpload(UpdateB);
            }
        }

        protected void OnDisable()
        {
            if (boundMeshes != null)
            {
                foreach (var bm in boundMeshes)
                {
                    if (bm == null || bm.attachedMeshV2 == null) continue; 
                    if (bm.meshListener != null) bm.attachedMeshV2.RemoveListener(API.Unity.ICustomizableCharacter.ListenableEvent.OnAnyDataChanged, bm.meshListener);
                    if (bm.vertexMaskRebuildListener != null)
                    {
                        bm.attachedMeshV2.EndListenForVertexMaskRebuild(bm.vertexMaskRebuildListener); 
                        bm.attachedMeshV2.RebuildVertexMask();
                    }
                }
            }

            ComputeBufferPoolUploader.EndListenPostUpload(UpdateA);
            ComputeBufferPoolUploader.EndListenPostUpload(UpdateB); 
        }

        protected void UpdateA()
        {
            if (queuedIterations > 0)
            {
                try
                {
                    var cmd = CommandBufferPool.Get($"{nameof(BodyMoldSystem)}.{nameof(UpdateA)}");

                    DispatchIterations(cmd, 1, false);
                    if (recalculateNormalsAfterMold) RecalculateNormals(cmd);

                    Graphics.ExecuteCommandBuffer(cmd);
                    cmd.Release();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }

                queuedIterations--;
            }
            else if (queuedClothIterations > 0)
            {
                try
                {
                    var cmd = CommandBufferPool.Get($"{nameof(BodyMoldSystem)}.{nameof(UpdateA)}");

                    DispatchIterations(cmd, 1, true);
                    if (recalculateNormalsAfterMold) RecalculateNormals(cmd);

                    Graphics.ExecuteCommandBuffer(cmd); 
                    cmd.Release();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }

                queuedClothIterations--; 
            }
        }

        protected void UpdateB()
        {
            if (shapeContributingBoundMeshes != null)
            {
                foreach (var bm in shapeContributingBoundMeshes)
                {
                    if (bm == null || bm.attachedMeshV2 == null || bm.binding == null || bm.binding.maskOnly) continue;
                    if (bm.cbPushbackVertices != null)
                    {
                        var cmd = CommandBufferPool.Get($"{nameof(BodyMoldSystem)}.{nameof(UpdateB)}");

                        PrepareLiveClothingVertexData(cmd);
                        PrepareLiveBodyMeshVertexData(cmd, bm);
                        PushBackBodyVertices(cmd, bm);

                        Graphics.ExecuteCommandBuffer(cmd);
                        cmd.Release();
                    }
                }
            }
        }
    }

    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct Triangles32
    {
        public int4 trianglesA;
        public int4 trianglesB;
        public int4 trianglesC;
        public int4 trianglesD;

        public int4 trianglesE;
        public int4 trianglesF;
        public int4 trianglesG;
        public int4 trianglesH;
    }

    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct PushBackVertex
    {
        public int vertexIndex;
        public Triangles32 pushBackTriangles;
    }

    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct MaskedVertex
    {
        public int vertexIndex;
        public float masking;
    }

}
