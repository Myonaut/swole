using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;

using Unity.Mathematics;

using Swole.Morphing;
using Swole.Cloth;
using Swole.DataStructures;
using UnityEngine.Events;

namespace Swole.Modding
{
    // Runtime system to mold clothing to a body mesh using a compute shader.
    // Usage:
    //  - Attach this MonoBehaviour somewhere, assign ComputeShader (BodyMold.compute)
    //  - Call Setup(clothingMesh, bodyMesh, bindingIndicesFlattened, bindingWeightsFlattened, maxBindings)
    //  - Call ApplyBodyPositions(newBodyPositions) when the body changes
    //  - Call DispatchIterations(n) each frame or as needed to run the iterative smoothing
    public class BodyMoldSystem : MonoBehaviour, IDisposable
    {
        public Transform rootTransform;
        public Transform RootTransform => rootTransform == null ? transform.parent : rootTransform;

        public ComputeShader computeShader;

        public Mesh tempMesh;

        [SerializeField]
        protected BodyMoldBindings bindings;

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

        public enum CollisionMode { VertexCloud = 0, Triangle = 1, SDF = 2, TriangleSDF = 3 }
        public CollisionMode collisionMode = CollisionMode.VertexCloud;

        /// <summary>
        /// cbClothingOffsets: holds the current per-vertex offset/deformation applied to the clothing (Vector3 per vertex).
        /// </summary>
        ComputeBuffer cbClothingOffsetsA;
        ComputeBuffer cbClothingOffsetsB;
        bool bufferSwapFlag;

        ComputeBuffer stretchBuffer;

        ComputeBuffer dynamicBoneWeightsBuffer;

        protected int GetOffsetsBufferStartIndexAndGrow()
        {
            if (clothingMesh == null) return 0;

            int startIndex = cbClothingOffsetsA == null ? 0 : cbClothingOffsetsA.count;
            int newCount = startIndex + clothingMesh.vertexCount;
            var newBufferA = new ComputeBuffer(newCount, UnsafeUtility.SizeOf(typeof(MeshVertexDelta)));
            var newBufferB = new ComputeBuffer(newCount, UnsafeUtility.SizeOf(typeof(MeshVertexDelta)));
            // Copy existing data to new buffers
            if (cbClothingOffsetsA != null || cbClothingOffsetsB != null) 
            {
                MeshVertexDelta[] existingData = new MeshVertexDelta[startIndex];

                if (cbClothingOffsetsA != null)
                {
                    cbClothingOffsetsA.GetData(existingData);
                    newBufferA.SetData(existingData);
                    cbClothingOffsetsA.Release();
                }

                if (cbClothingOffsetsB != null)
                {
                    cbClothingOffsetsB.GetData(existingData);
                    newBufferB.SetData(existingData);
                    cbClothingOffsetsB.Release();
                }
            }
            // Assign new buffers
            cbClothingOffsetsA = newBufferA;
            cbClothingOffsetsB = newBufferB;


            return startIndex;
        }

        /// <summary>
        /// Mapping from SDF texture ID to the offset in the clothing vertex offset buffer.
        /// </summary>
        protected readonly Dictionary<string, int> sdfIdToBufferIndexOffsets = new Dictionary<string, int>();

        Mesh clothingMesh;
        List<Material> clothingMaterials;
        [SerializeField]
        protected MeshRenderer[] targetClothingRenderers;
        public int clothingFlexShapeStartIndex = 1;

        Vector3[] clothingOriginalPositions;
        Vector3[] clothingOriginalNormals;
        Vector4[] clothingOriginalTangents;
        // Per-mesh triangle indices when available (used by triangle kernel)

        int kernelHandle = -1;
        int vertexCount = 0;

        // Per-bound-character mesh data
        protected class BoundMeshData
        {
            public int index;

            public CustomizableCharacterMeshV2 attachedMeshV2;
            public int vertexCount;

            public int clothingVertexIndexCount;
            public ComputeBuffer cbBindingLocalIndices;
            public ComputeBuffer cbBindingIndices;
            public ComputeBuffer cbBindingWeights;
            public ComputeBuffer cbCollisionTriangles;

            public InstanceBuffer<MeshVertexDelta> deltaInstanceBuffer;
            public InstanceBuffer<MeshVertexDelta> prevDeltaInstanceBuffer;

            public SignedDistanceFieldTexture sdfTexture;

            public UnityAction meshListener;

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
                return flag;
            }

            private readonly HashSet<string> evaluatedSdfIds = new HashSet<string>();
            public bool HasEvaluatedSdfId(string sdfId)
            {
                return evaluatedSdfIds.Contains(sdfId);
            }
            public void MarkSdfIdEvaluated(string sdfId)
            {
                evaluatedSdfIds.Add(sdfId);
            }

            public void Release()
            {
                // These buffers are persistent and come from the BodyMoldBindings asset, so we don't release them here. They will be released when the asset is destroyed.
                //if (cbBindingLocalIndices != null) { cbBindingLocalIndices.Release(); cbBindingLocalIndices = null; }
                //if (cbBindingIndices != null) { cbBindingIndices.Release(); cbBindingIndices = null; }
                //if (cbBindingWeights != null) { cbBindingWeights.Release(); cbBindingWeights = null; }
                //if (cbCollisionTriangles != null) { cbCollisionTriangles.Release(); cbCollisionTriangles = null; }
            }
        }

        List<BoundMeshData> boundMeshes = new List<BoundMeshData>();

        public void SetupFromBindings(BodyMoldBindings bindings)
        {
            if (bindings == null) throw new ArgumentNullException(nameof(bindings));

            this.bindings = bindings;
            // Initialize core buffers (clothing positions/offsets) and kernel
            Setup(bindings.EditedClothingMesh);

            stretchBuffer = new ComputeBuffer(bindings.OriginalMesh.vertexCount, sizeof(float));
            float[] emptyStretch = new float[bindings.OriginalMesh.vertexCount];
            stretchBuffer.SetData(emptyStretch);

            if (bindings.dynamicBoneWeights)
            {
                dynamicBoneWeightsBuffer = new ComputeBuffer(bindings.BoneWeightsBuffer.count, UnsafeUtility.SizeOf(typeof(BoneWeight8Float)));
                ComputeBuffer.CopyCount(bindings.BoneWeightsBuffer, dynamicBoneWeightsBuffer, 0);  
            } 

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

        // Setup with multiple body meshes. bodyTrianglesPerMesh can be null or contain triangle arrays parallel to bodies.
        public void Setup(Mesh clothing)
        {
            if (computeShader == null) throw new InvalidOperationException("ComputeShader not assigned.");
            if (clothing == null) throw new ArgumentNullException(nameof(clothing));

            clothingMesh = clothing;
            if (targetClothingRenderers != null && targetClothingRenderers.Length > 0)
            {
                clothingMaterials = new List<Material>();
                foreach (var rend in targetClothingRenderers)
                {
                    if (rend == null) continue;
                    clothingMaterials.AddRange(rend.materials); 
                    var meshFilter = rend.gameObject.GetComponent<MeshFilter>();
                    if (meshFilter != null) meshFilter.sharedMesh = bindings.EditedClothingMesh; 
                }
            }

            clothingOriginalPositions = clothing.vertices;
            clothingOriginalNormals = clothing.normals;
            clothingOriginalTangents = clothing.tangents;

            vertexCount = clothingOriginalPositions.Length;

            // Release existing buffers if any
            ReleaseBuffers();

            // Create buffers
            //cbClothingOffsetsA = new ComputeBuffer(vertexCount, UnsafeUtility.SizeOf(typeof(MeshVertexDelta)));
            //cbClothingOffsetsB = new ComputeBuffer(vertexCount, UnsafeUtility.SizeOf(typeof(MeshVertexDelta)));

            // offsets start zero
            //MeshVertexDelta[] zeroDeltas = new MeshVertexDelta[vertexCount];
            //for (int i = 0; i < vertexCount; i++) zeroDeltas[i] = MeshVertexDelta.Default;
            //cbClothingOffsetsA.SetData(zeroDeltas);
            //cbClothingOffsetsB.SetData(zeroDeltas);

            // Choose specialized kernel based on selected collision mode to avoid runtime branching inside the shader.
            string kernelName = "BodyMold_VertexCloud";
            switch (collisionMode)
            {
                case CollisionMode.Triangle:
                    kernelName = "BodyMold_Triangle";
                    break;

                case CollisionMode.SDF:
                    kernelName = "BodyMold_SDF"; 
                    break;

                case CollisionMode.TriangleSDF:
                    kernelName = "BodyMold_TriangleSDF"; 
                    break;
            }
            kernelHandle = computeShader.FindKernel(kernelName);

            //computeShader.SetBuffer(kernelHandle, "_ClothingVertexDeltasReadOnly", cbClothingOffsetsA);
            //computeShader.SetBuffer(kernelHandle, "_ClothingVertexDeltas", cbClothingOffsetsB);

            // Set static params
            computeShader.SetFloat("_PenetrationDistance", penetrationDistance);
            computeShader.SetFloat("_PenetrationRecovery", penetrationRecovery);
            computeShader.SetInt("_VertexCount", vertexCount);
            // Collision mode is encoded by choosing the kernel; no runtime _CollisionMode value required.
        } 

        protected virtual void PrepareDispatch(out int threadGroupSize)
        {
            if (kernelHandle < 0) throw new InvalidOperationException("Kernel not initialized");
            // update dynamic params
            computeShader.SetFloat("_PenetrationDistance", penetrationDistance);
            computeShader.SetFloat("_PenetrationRecovery", penetrationRecovery);
            computeShader.SetFloat("_PreserveFactor", preserveFactor);
            computeShader.SetFloat("_DeltaMultiplier", deltaMultiplier);
            computeShader.SetInt("_VertexCount", vertexCount);
            // _BodyVertexCount and body buffers are set per-bound-character-mesh before dispatching that mesh
            computeShader.SetInt("_CollisionMode", (int)collisionMode);

            computeShader.SetFloat("_FrontPushFactor", forceDepenStrength);
            computeShader.SetFloat("_CollisionPasses", collisionPasses);
            computeShader.SetFloat("_CollisionRelaxation", collisionRelaxation); 

            threadGroupSize = 256;
        }
        public void DispatchIterations(int iterations)
        {
            PrepareDispatch(out int threadGroupSize);

            for (int i = 0; i < iterations; i++)
            {
                foreach (var bm in boundMeshes)
                {
                    if (bm == null) continue;

                    DispatchIteration(bm, threadGroupSize);
                }
            }
        }

        public void DispatchIterations(int boundMeshIndex, int iterations) 
        { 
            if (boundMeshIndex < 0 || boundMeshIndex >= boundMeshes.Count) throw new ArgumentOutOfRangeException(nameof(boundMeshIndex));
            DispatchIterations(boundMeshes[boundMeshIndex], iterations); 
        }
        protected void DispatchIterations(BoundMeshData bm, int iterations)
        {
            PrepareDispatch(out int threadGroupSize);

            for (int i = 0; i < iterations; i++)
            {
                DispatchIteration(bm, threadGroupSize);
            }
        }

        protected void SetDefaultShaderProperties(BoundMeshData bm, int kernelHandle, ComputeShader computeShader)
        {
            computeShader.SetBuffer(kernelHandle, "_ClothingBaseVertexData", bindings.VertexDataBuffer);

            computeShader.SetBuffer(kernelHandle, "_VertexConnections", bindings.VertexConnectionsBuffer);
            computeShader.SetBuffer(kernelHandle, "_VertexConnectionCounts", bindings.VertexConnectionCountsBuffer);
            computeShader.SetBuffer(kernelHandle, "_VertexConnectionStartIndices", bindings.VertexConnectionStartIndicesBuffer); 

            // bind the character mesh vertex/triangle buffers provided by mesh.SubData
            var sub = bm.attachedMeshV2.SubData;
            if (sub != null)
            {
                computeShader.SetBuffer(kernelHandle, "_BodyVertexPositions", sub.VerticesBuffer);
                computeShader.SetBuffer(kernelHandle, "_BodyVertexNormals", sub.NormalsBuffer); 
                computeShader.SetBuffer(kernelHandle, "_BodyTriangles", sub.TrianglesBuffer);
                // set vertex count for bounds checks
                computeShader.SetInt("_BodyVertexCount", bm.vertexCount);
            }

            computeShader.SetBuffer(kernelHandle, "_BindingLocalIndices", bm.cbBindingLocalIndices);
            computeShader.SetInt("_ClothingVertexIndexCount", bm.clothingVertexIndexCount); 
            computeShader.SetFloat("_ClothThickness", thickness); 

            // bind the per-clothing binding arrays for this target mesh
            computeShader.SetBuffer(kernelHandle, "_BindingIndices", bm.cbBindingIndices);
            computeShader.SetBuffer(kernelHandle, "_BindingWeights", bm.cbBindingWeights);
            computeShader.SetBuffer(kernelHandle, "_NearbyTriangles", bm.cbCollisionTriangles);
            computeShader.SetBuffer(kernelHandle, "_ClothingTriangles", bindings.TrianglesBuffer);

            // bind the instance buffers for current/previous deltas for this attached mesh (if present)
            if (bm.deltaInstanceBuffer != null) bm.deltaInstanceBuffer.BindShaderProperty(computeShader, kernelHandle, "_BodyDeltas", false);
            if (bm.prevDeltaInstanceBuffer != null) bm.prevDeltaInstanceBuffer.BindShaderProperty(computeShader, kernelHandle, "_BodyPreviousDeltas", false);
        }
        protected virtual void DispatchIteration(BoundMeshData bm, int threadGroupSize)
        {
            SetDefaultShaderProperties(bm, kernelHandle, computeShader);

            // indicate whether this dispatch is the first after a body update (applies delta diff once)
            computeShader.SetInt("_BodyDeltasChanged", bm.ConsumeDirtyFlag() ? 1 : 0);

            ComputeBuffer currentOffsetsBuffer;
            if (bufferSwapFlag)
            {
                currentOffsetsBuffer = cbClothingOffsetsA;
                computeShader.SetBuffer(kernelHandle, "_ClothingVertexDeltasReadOnly", cbClothingOffsetsB);
                computeShader.SetBuffer(kernelHandle, "_ClothingVertexDeltas", currentOffsetsBuffer);

                if (bm.index == 0 && clothingMaterials != null && clothingMaterials.Count > 0)
                {
                    foreach(var mat in clothingMaterials)
                    {
                        if (mat == null) continue;
                        mat.SetBuffer("_ClothingVertexDeltas", currentOffsetsBuffer);
                        mat.SetInt("_ClothingFlexShapeIndexInBufferStart", clothingFlexShapeStartIndex);
                    }
                }
            }
            else
            {
                currentOffsetsBuffer = cbClothingOffsetsB;
                computeShader.SetBuffer(kernelHandle, "_ClothingVertexDeltasReadOnly", cbClothingOffsetsA);
                computeShader.SetBuffer(kernelHandle, "_ClothingVertexDeltas", currentOffsetsBuffer);

                if (bm.index == 0 && clothingMaterials != null && clothingMaterials.Count > 0)
                {
                    foreach (var mat in clothingMaterials)
                    {
                        if (mat == null) continue;
                        mat.SetBuffer("_ClothingVertexDeltas", currentOffsetsBuffer);
                        mat.SetInt("_ClothingFlexShapeIndexInBufferStart", clothingFlexShapeStartIndex);
                    }
                }
            }

            computeShader.SetBuffer(kernelHandle, "_ClothingStretch", stretchBuffer);

            foreach (var entry in sdfIdToBufferIndexOffsets)
            {
                string sdfId = entry.Key;
                var bufferOffset = entry.Value;
                computeShader.SetInt($"_IndexOffset", bufferOffset);
                bool firstRun = !bm.HasEvaluatedSdfId(sdfId);
                computeShader.SetInt($"_FirstRun", firstRun ? 1 : 0);
                if (firstRun) bm.MarkSdfIdEvaluated(sdfId);

                if ((collisionMode == CollisionMode.SDF || collisionMode == CollisionMode.TriangleSDF) && bm.sdfTexture != null)
                {
                    bm.sdfTexture.ApplyToShader(sdfId, computeShader, kernelHandle);
                }

                int dispatchGroups = Mathf.CeilToInt(bm.clothingVertexIndexCount / (float)threadGroupSize);
                computeShader.Dispatch(kernelHandle, dispatchGroups, 1, 1);
            }

            if (bindings.dynamicBoneWeights)
            {
                var recalBwKernel = computeShader.FindKernel("AdjustSkinBindings");

                SetDefaultShaderProperties(bm, recalBwKernel, computeShader);
                computeShader.SetInt($"_IndexOffset", 0);
                computeShader.SetBuffer(recalBwKernel, "_ClothingVertexDeltasReadOnly", currentOffsetsBuffer);
                computeShader.SetBuffer(recalBwKernel, "_SkinBindingsReadOnly", bindings.BoneWeightsBuffer);
                computeShader.SetBuffer(recalBwKernel, "_SkinBindings", dynamicBoneWeightsBuffer);
                computeShader.SetBuffer(recalBwKernel, "_BodySkinBindings", bm.attachedMeshV2.SubData.BoneWeightsBuffer);
                computeShader.SetBuffer(recalBwKernel, "_Deltas", PersistentJobDataTracker.GetEmptyBuffer<MeshVertexDelta>()); 
                computeShader.SetInt("_DeltasIndexOffset", -1);

                int dispatchGroups = Mathf.CeilToInt(bm.clothingVertexIndexCount / (float)threadGroupSize);
                computeShader.Dispatch(recalBwKernel, dispatchGroups, 1, 1);
            }

            bufferSwapFlag = !bufferSwapFlag;
        }

        public void GetDeformedData(int sdfIndex, out Vector3[] outPos, out Vector3[] outNorm, out Vector4[] outTan)
        {
            var buffer = bufferSwapFlag ? cbClothingOffsetsB : cbClothingOffsetsA;
            MeshVertexDelta[] offsets = new MeshVertexDelta[buffer.count];
            buffer.GetData(offsets);
            outPos = new Vector3[vertexCount];
            outNorm = new Vector3[vertexCount];
            outTan = new Vector4[vertexCount];
            int indexOffset = sdfIndex * vertexCount;
            for (int i = 0; i < vertexCount; i++)
            {
                var j = indexOffset + i;
                outPos[i] = clothingOriginalPositions[i] + (Vector3)offsets[j].positionDelta; 
                outNorm[i] = clothingOriginalNormals[i] + (Vector3)offsets[j].normalDelta;
                outTan[i] = new float4(((float4)clothingOriginalTangents[i]).xyz + offsets[j].tangentDelta, clothingOriginalTangents[i].w);
            }
        }

        // Apply current deformed positions to a mesh (modifies mesh.vertices)
        public void ApplyToMesh(int sdfIndex, Mesh targetMesh)
        {
            if (targetMesh == null) return;
            GetDeformedData(sdfIndex, out var verts, out var norms, out var tans); 
            targetMesh.vertices = verts;
            targetMesh.normals = norms;
            targetMesh.tangents = tans;
            targetMesh.RecalculateBounds();
            // normals/tangents can be recalculated later or supplied if needed
        }

        void OnDestroy() 
        { 
            Dispose();

            if (boundMeshes != null && boundMeshes.Count > 0)
            {
                var bm = boundMeshes[0];
                if (bm.attachedMeshV2 != null)
                {
                    if (clothingMaterials != null) 
                    { 
                        bm.attachedMeshV2.UnbindSkinningMatricesBufferFromMaterials(clothingMaterials);
                        bm.attachedMeshV2.UnbindStandaloneShapesControlBufferFromMaterials(clothingMaterials);
                        bm.attachedMeshV2.UnbindMuscleGroupsControlBufferFromMaterials(clothingMaterials);
                        bm.attachedMeshV2.UnbindFatGroupsControlBufferFromMaterials(clothingMaterials); 
                    }
                }

                boundMeshes.Clear();
            }

            if (clothingMaterials != null)
            {
                foreach(var mat in clothingMaterials)
                {
                    if (mat != null) Destroy(mat);
                }
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

            if (stretchBuffer != null) { stretchBuffer.Release(); stretchBuffer = null; }

            if (dynamicBoneWeightsBuffer != null) { dynamicBoneWeightsBuffer.Release(); dynamicBoneWeightsBuffer = null; }

            if (boundMeshes != null)
            {
                foreach (var bm in boundMeshes)
                {
                    if (bm == null) continue;
                    bm.Release();
                }
                boundMeshes.Clear();
            }

            sdfIdToBufferIndexOffsets.Clear();
        }

        public bool BindCharacterMesh(CustomizableCharacterMeshV2 meshV2, int index)
        {
            if (bindings == null) throw new InvalidOperationException("Call SetupFromBindings(...) before binding character meshes."); 

            if (meshV2 == null) throw new ArgumentNullException(nameof(meshV2));

            var bm = new BoundMeshData();
            bm.index = index;
            bm.attachedMeshV2 = meshV2;
            bm.vertexCount = meshV2.SubData != null ? meshV2.SubData.VertexCount : 0;

            var binding = bindings.perBodyBindings[index]; 
            bm.clothingVertexIndexCount = binding.MaskLength;
            bm.cbBindingLocalIndices = binding.LocalIndicesBuffer;
            bm.cbBindingIndices = binding.IndicesBuffer;
            bm.cbBindingWeights = binding.WeightsBuffer;
            bm.cbCollisionTriangles = binding.CollisionTrianglesBuffer;

            bm.sdfTexture = meshV2.GetComponent<SignedDistanceFieldTexture>();
            if (bm.sdfTexture != null)
            {
                foreach(var output in bm.sdfTexture.Outputs())
                {
                    if (!sdfIdToBufferIndexOffsets.ContainsKey(output.ID)) 
                    {
                        sdfIdToBufferIndexOffsets[output.ID] = GetOffsetsBufferStartIndexAndGrow();
                    }
                }
            }

            // attach instance buffers if available
            meshV2.TryGetInstanceBuffer<MeshVertexDelta>(meshV2.SubData.PerVertexDeltaDataPropertyName, out bm.deltaInstanceBuffer);
            meshV2.TryGetInstanceBuffer<MeshVertexDelta>(meshV2.SubData.PerVertexPreviousDeltaDataPropertyName, out bm.prevDeltaInstanceBuffer);

            boundMeshes.Add(bm);

            bm.meshListener = () =>
            {
                bm.MarkDirty();
                if (!enabled || !gameObject.activeInHierarchy) return;

                StartCoroutine(OnBodyShapeChanged(bm));
            };
            meshV2.AddListener(API.Unity.ICustomizableCharacter.ListenableEvent.OnAnyDataChanged, bm.meshListener);

            if (index == 0)
            {
                if (clothingMaterials != null)
                {
                    //float cx = math.cos(-bm.eulerOffsetRadians.x);
                    //float sx = math.sin(-bm.eulerOffsetRadians.x);
                    //float cy = math.cos(-bm.eulerOffsetRadians.y);
                    //float sy = math.sin(-bm.eulerOffsetRadians.y);
                    //float cz = math.cos(-bm.eulerOffsetRadians.z);
                    //float sz = math.sin(-bm.eulerOffsetRadians.z);

                    meshV2.BindSkinningMatricesBufferToMaterials(clothingMaterials);
                    meshV2.BindStandaloneShapesControlBufferToMaterials(clothingMaterials);
                    meshV2.BindMuscleGroupsControlBufferToMaterials(clothingMaterials);
                    meshV2.BindFatGroupsControlBufferToMaterials(clothingMaterials); 
                    foreach (var mat in clothingMaterials)
                    {
                        if (mat == null) continue;

                        meshV2.MeshGroup2.ApplyMainMaterialOverrides(mat);

                        mat.SetInt(meshV2.SubData.RigInstanceIDPropertyName, meshV2.RigInstanceID);
                        mat.SetInt(meshV2.SubData.CharacterInstanceIDPropertyName, meshV2.CharacterInstanceID);
                        mat.SetFloat(meshV2.SubData.BustMixPropertyName, Mathf.Clamp01(meshV2.BustSize));
                        mat.SetBuffer(meshV2.SubData.MeshShapeIndicesPropertyName, meshV2.SubData.MeshShapeIndicesBuffer);
                        mat.SetBuffer(meshV2.SubData.MeshShapeFrameDeltasPropertyName, meshV2.SubData.MeshShapeFrameDeltasBuffer);
                        mat.SetBuffer(meshV2.SubData.MuscleGroupInfluencesPropertyName, meshV2.SubData.MuscleGroupInfluencesBuffer);
                        mat.SetBuffer(meshV2.SubData.FatGroupInfluencesPropertyName, meshV2.SubData.FatGroupInfluencesBuffer);
                        mat.SetInteger(meshV2.SubData.FlexShapeIndexPropertyName, meshV2.SubData.FlexShape);
                        mat.SetInteger("_CharacterVertexCount", meshV2.SubData.VertexCount);
                        mat.SetBuffer("_CharacterVertices", meshV2.SubData.VerticesBuffer);
                        mat.SetBuffer("_BindingIndices", bm.cbBindingIndices);
                        mat.SetBuffer("_BindingWeights", bm.cbBindingWeights);
                        mat.SetBuffer("_ClothingStretch", stretchBuffer); 

                        mat.SetInt("_VertexCount", vertexCount);

                        mat.SetBuffer(meshV2.SubData.SkinningDataPropertyName, dynamicBoneWeightsBuffer == null ? bindings.BoneWeightsBuffer : dynamicBoneWeightsBuffer);   
                    }
                }
            }

            return true; 
        }

        protected IEnumerator OnBodyShapeChanged(BoundMeshData bindings)
        {
            yield return null;

            DispatchIterations(bindings, shapeChangeFollowUpIterations); 
            //ApplyToMesh(0, tempMesh); 
        }

        // Unbind and release buffers associated with a previously bound character mesh
        public bool UnbindCharacterMesh(CustomizableCharacterMeshV2 meshV2) 
        {
            if (meshV2 == null) return false;
            if (boundMeshes == null) return false;
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
                    if (bm == null || bm.attachedMeshV2 == null || bm.meshListener == null) continue;
                    bm.attachedMeshV2.AddListener(API.Unity.ICustomizableCharacter.ListenableEvent.OnAnyDataChanged, bm.meshListener);
                }
            }
        }

        protected void OnDisable()
        {
            if (boundMeshes != null)
            {
                foreach (var bm in boundMeshes)
                {
                    if (bm == null || bm.attachedMeshV2 == null || bm.meshListener == null) continue;
                    bm.attachedMeshV2.RemoveListener(API.Unity.ICustomizableCharacter.ListenableEvent.OnAnyDataChanged, bm.meshListener); 
                }
            }
        }
    }

    [Serializable]
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

}
