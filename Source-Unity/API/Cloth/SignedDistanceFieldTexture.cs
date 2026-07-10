using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;

using Swole.Morphing;

namespace Swole.Cloth
{
    public class SignedDistanceFieldTexture : MonoBehaviour
    {
        [SerializeField]
        protected ComputeShader boundsComputeShader;
        public ComputeShader BoundsShader => boundsComputeShader == null ? sdfComputeShader : boundsComputeShader; 

        [SerializeField]
        protected ComputeShader sdfComputeShader;
        public ComputeShader SdfShader => sdfComputeShader == null ? boundsComputeShader : sdfComputeShader;

        public int textureResolution = 64; // 64x64x64 is usually ideal for performance/quality

        [SerializeField]
        protected CustomizableCharacterMeshV2 characterMesh;
        [SerializeField]
        protected MeshFilter meshFilter;

        [NonSerialized]
        protected ComputeBuffer tempCharacterMeshOutputData;
        protected ComputeBuffer GetTemporaryCharacterMeshOutputDataBuffer()
        {
            if (characterMesh == null) return null;

            if (tempCharacterMeshOutputData == null)
            {
                tempCharacterMeshOutputData = new ComputeBuffer(characterMesh.SubData.VertexCount, UnsafeUtility.SizeOf<MeshVertexDelta>(), ComputeBufferType.Structured);
            }

            return tempCharacterMeshOutputData;
        }

        [NonSerialized]
        protected ComputeBuffer persistentDeformedVerticesBuffer;
        [NonSerialized]
        protected ComputeBuffer persistentPartialBoundsBuffer;
        [NonSerialized]
        protected ComputeBuffer persistentBoundsMinBuffer;
        [NonSerialized]
        protected ComputeBuffer persistentBoundsMaxBuffer;
        [NonSerialized]
        protected int allocatedVertexCount = -1;

        // Call this once when initializing
        protected void InitializeGPUBuffers(int maxVertexCount)
        {
            if (allocatedVertexCount >= maxVertexCount && persistentDeformedVerticesBuffer != null)
                return; // Already allocated

            // Release old buffers if resizing
            ReleaseGPUBuffers();

            allocatedVertexCount = maxVertexCount;
            persistentDeformedVerticesBuffer = new ComputeBuffer(maxVertexCount, sizeof(float) * 3);

            int maxGroups = (maxVertexCount + 255) / 256;
            persistentPartialBoundsBuffer = new ComputeBuffer(maxGroups, sizeof(float) * 6);
            persistentBoundsMinBuffer = new ComputeBuffer(1, sizeof(float) * 3);
            persistentBoundsMaxBuffer = new ComputeBuffer(1, sizeof(float) * 3);
        }

        protected void ReleaseGPUBuffers()
        {
            tempCharacterMeshOutputData?.Release();
            persistentDeformedVerticesBuffer?.Release();
            persistentPartialBoundsBuffer?.Release();
            persistentBoundsMinBuffer?.Release();
            persistentBoundsMaxBuffer?.Release();
            allocatedVertexCount = -1;
        }

        [Serializable]
        public class OutputSDF
        {
            [NonSerialized]
            public SignedDistanceFieldTexture manager;

            [SerializeField]
            private string id;
            public string ID => id;

            [SerializeField, Tooltip("Optional mesh shape to apply for a customizable character mesh")]
            private string meshShapeName;
            [SerializeField]
            private int meshShapeFrame;
            [SerializeField]
            private float meshShapeWeight = 1f;
            [NonSerialized]
            private int meshShapeIndex = -1;
            public bool HasMeshShape => meshShapeIndex >= 0 || !string.IsNullOrWhiteSpace(meshShapeName);  

            [SerializeField]
            private RenderTexture sdf3DTexture;
            public RenderTexture Texture => sdf3DTexture;
            public void InitializeSdfTexture(int textureResolution)
            {
                // Initialize the 3D Render Texture if not already done
                if (sdf3DTexture == null || !sdf3DTexture.IsCreated())
                {
                    sdf3DTexture = new RenderTexture(textureResolution, textureResolution, 0, RenderTextureFormat.RFloat);
                    sdf3DTexture.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
                    sdf3DTexture.volumeDepth = textureResolution;
                    sdf3DTexture.enableRandomWrite = true; // Required for Compute Shaders
                    sdf3DTexture.useMipMap = false;
                    sdf3DTexture.wrapMode = TextureWrapMode.Clamp; // Clamps edge colors to prevent bleed
                    sdf3DTexture.filterMode = FilterMode.Bilinear;
                    sdf3DTexture.Create();
                }

                if (HasMeshShape)
                {
                    meshShapeIndex = manager.characterMesh.SubData.IndexOfShapeInBuffer(meshShapeName); 
                } 
                else
                {
                    meshShapeIndex = -1;
                }
            }

            public void Release()
            {
                if (sdf3DTexture != null)
                {
                    sdf3DTexture.Release();
                    sdf3DTexture = null;
                }
            }

            [NonSerialized]
            public Vector3 boundsMin;
            public Vector3 BoundsMin => boundsMin;

            [NonSerialized]
            public Vector3 boundsMax;
            public Vector3 BoundsMax => boundsMax;

            protected bool GetBoundsMeshAndMeshBuffers(out bool cleanup, out Mesh mesh, out ComputeBuffer vertexBuffer, out ComputeBuffer triangleBuffer, out ComputeBuffer shapeFrameIndicesBuffer, out ComputeBuffer shapeDeltasBuffer, out Bounds localBounds, out Vector3 boundsMin, out Vector3 boundsMax)
            {
                cleanup = true;
                mesh = null;
                vertexBuffer = null;
                triangleBuffer = null;
                shapeFrameIndicesBuffer = null; 
                shapeDeltasBuffer = null;
                localBounds = default;
                boundsMin = Vector3.zero;
                boundsMax = Vector3.zero;
                if (manager.characterMesh != null)
                {
                    cleanup = false; 
                    mesh = manager.characterMesh.SubData.Mesh;

                    // Wait for any pending CPU jobs to complete (ensures buffers are ready)
                    manager.characterMesh.Instance2.OwnerGroup.ActiveJob.Complete();

                    int vertexCount = manager.characterMesh.SubData.vertexCount;
                    int deltasOffset = manager.characterMesh.Instance2.localID * vertexCount;
                    
                    vertexBuffer = manager.characterMesh.SubData.VerticesBuffer;
                    triangleBuffer = manager.characterMesh.SubData.TrianglesBuffer;
                    ComputeBuffer deltasBuffer = null;
                    if (manager.characterMesh.TryGetInstanceBuffer<MeshVertexDelta>(manager.characterMesh.SubData.PerVertexDeltaDataPropertyName, out var deltaInstanceBuffer)) deltasBuffer = deltaInstanceBuffer.BufferThisFrame;

                    int shapeDeltasOffset = 0;
                    if (meshShapeIndex >= 0)
                    {
                        shapeDeltasBuffer = manager.characterMesh.SubData.MeshShapeFrameDeltasBuffer;
                        shapeFrameIndicesBuffer = manager.characterMesh.SubData.MeshShapeIndicesBuffer;
                        shapeDeltasOffset = (meshShapeFrame + meshShapeIndex) * vertexCount;
                    }

                    localBounds = manager.CalculateDeformedBoundsGPU(
                        manager.BoundsShader,
                        vertexBuffer,
                        deltasBuffer,
                        deltasOffset,
                        shapeDeltasBuffer,
                        shapeDeltasOffset,
                        vertexCount,
                        1.15f); // Padding scale

                    boundsMin = localBounds.min;
                    boundsMax = localBounds.max;
                }
                else if (manager.meshFilter != null)
                {
                    mesh = manager.meshFilter.sharedMesh;

                    localBounds = mesh.bounds;
                    float padding = 0.05f; // 5cm padding to prevent clamping artifacts at edges
                    boundsMin = localBounds.min - new Vector3(padding, padding, padding);
                    boundsMax = localBounds.max + new Vector3(padding, padding, padding);

                    Vector3[] vertices = mesh.vertices;
                    int[] triangles = mesh.triangles;

                    vertexBuffer = new ComputeBuffer(vertices.Length, sizeof(float) * 3);
                    vertexBuffer.SetData(vertices);

                    triangleBuffer = new ComputeBuffer(triangles.Length, sizeof(int));
                    triangleBuffer.SetData(triangles);
                }
                else return false;

                return true;
            }

            public void BakeSDF()
            {
                var characterMesh = manager.characterMesh;
                var sdfComputeShader = manager.SdfShader;
                if (sdfComputeShader == null) return;

                var textureResolution = manager.textureResolution;
                InitializeSdfTexture(textureResolution);

                if (!GetBoundsMeshAndMeshBuffers(out bool cleanup, out Mesh mesh, out ComputeBuffer vertexBuffer, out ComputeBuffer triangleBuffer, out ComputeBuffer shapeFrameIndicesBuffer, out ComputeBuffer shapeDeltasBuffer, out Bounds localBounds, out boundsMin, out boundsMax)) return;

                int kernel = sdfComputeShader.FindKernel(characterMesh == null ? "Generate" : "GenerateWithDeltas");

                // Set Compute Shader Parameters
                sdfComputeShader.SetTexture(kernel, "_Result", sdf3DTexture);
                sdfComputeShader.SetBuffer(kernel, "_Vertices", vertexBuffer);
                sdfComputeShader.SetBuffer(kernel, "_Triangles", triangleBuffer);
                if (characterMesh != null && characterMesh.TryGetInstanceBuffer<MeshVertexDelta>(characterMesh.SubData.PerVertexDeltaDataPropertyName, out var deltaInstanceBuffer)) deltaInstanceBuffer.BindShaderProperty(sdfComputeShader, kernel, "_Deltas", false);

                if (shapeDeltasBuffer != null)
                {
                    sdfComputeShader.SetBuffer(kernel, "_ShapeDeltas", shapeDeltasBuffer); 
                    sdfComputeShader.SetInt("_ShapeDeltasIndexOffset", ((meshShapeFrame + meshShapeIndex) * manager.characterMesh.SubData.VertexCount));
                }
                else
                {
                    sdfComputeShader.SetBuffer(kernel, "_ShapeDeltas", PersistentJobDataTracker.GetEmptyBuffer<MeshVertexDelta>());
                    sdfComputeShader.SetInt("_ShapeDeltasIndexOffset", -1); 
                }

                sdfComputeShader.SetInt("_VertexCount", vertexBuffer.count);
                sdfComputeShader.SetInt("_TriangleCount", triangleBuffer.count);
                sdfComputeShader.SetInt("_Resolution", textureResolution);

                sdfComputeShader.SetVector("_BoundsMin", boundsMin);
                sdfComputeShader.SetVector("_BoundsMax", boundsMax);

                // Dispatch Compute Shader (Thread groups of 8x8x8 to match the HLSL layout)
                int threadGroups = Mathf.CeilToInt((float)textureResolution / 8f);
                sdfComputeShader.Dispatch(kernel, threadGroups, threadGroups, threadGroups);

                if (cleanup)
                {
                    // Clean up CPU-GPU buffers immediately
                    vertexBuffer.Release();
                    triangleBuffer.Release();
                }
            }

            public void FastBakeSDF()
            {
                var characterMesh = manager.characterMesh;
                var sdfComputeShader = manager.SdfShader;
                if (sdfComputeShader == null) return;
                 
                var textureResolution = manager.textureResolution;
                InitializeSdfTexture(textureResolution);

                if (!GetBoundsMeshAndMeshBuffers(out bool cleanup, out Mesh mesh, out ComputeBuffer vertexBuffer, out ComputeBuffer triangleBuffer, out ComputeBuffer shapeFrameIndicesBuffer, out ComputeBuffer shapeDeltasBuffer, out Bounds localBounds, out boundsMin, out boundsMax)) return;

                // 1. Create a temporary interlocking Texture if it doesn't exist
                RenderTexture interlockingGrid = new RenderTexture(textureResolution, textureResolution, 0, RenderTextureFormat.RInt);
                interlockingGrid.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
                interlockingGrid.volumeDepth = textureResolution;
                interlockingGrid.enableRandomWrite = true;
                interlockingGrid.Create();

                // 2. Clear the texture to uint.MaxValue (equivalent to infinite distance)
                int kernelInit = sdfComputeShader.FindKernel("InitializeGrid");
                sdfComputeShader.SetTexture(kernelInit, "_InterlockingGrid", interlockingGrid);
                sdfComputeShader.SetInt("_Resolution", textureResolution);

                int initGroups = Mathf.CeilToInt((float)textureResolution / 8f);
                sdfComputeShader.Dispatch(kernelInit, initGroups, initGroups, initGroups);

                // Rasterize Triangles
                int kernelRaster = sdfComputeShader.FindKernel(characterMesh == null ? "RasterizeTriangles" : "RasterizeTrianglesWithDeltas");
                sdfComputeShader.SetTexture(kernelRaster, "_InterlockingGrid", interlockingGrid);

                sdfComputeShader.SetBuffer(kernelRaster, "_Vertices", vertexBuffer);
                sdfComputeShader.SetBuffer(kernelRaster, "_Triangles", triangleBuffer);
                if (characterMesh != null && characterMesh.TryGetInstanceBuffer<MeshVertexDelta>(characterMesh.SubData.PerVertexDeltaDataPropertyName, out var deltaInstanceBuffer)) deltaInstanceBuffer.BindShaderProperty(sdfComputeShader, kernelRaster, "_Deltas", false);

                if (shapeDeltasBuffer != null)
                {
                    sdfComputeShader.SetBuffer(kernelRaster, "_ShapeDeltas", shapeDeltasBuffer);
                    sdfComputeShader.SetInt("_ShapeDeltasIndexOffset", ((meshShapeFrame + meshShapeIndex) * manager.characterMesh.SubData.VertexCount));
                }
                else
                {
                    sdfComputeShader.SetBuffer(kernelRaster, "_ShapeDeltas", PersistentJobDataTracker.GetEmptyBuffer<MeshVertexDelta>());
                    sdfComputeShader.SetInt("_ShapeDeltasIndexOffset", -1);
                }

                sdfComputeShader.SetInt("_VertexCount", vertexBuffer.count);
                sdfComputeShader.SetInt("_TriangleCount", triangleBuffer.count);
                sdfComputeShader.SetInt("_Resolution", textureResolution);

                sdfComputeShader.SetVector("_BoundsMin", boundsMin);
                sdfComputeShader.SetVector("_BoundsMax", boundsMax);
                // Calculate the maximum possible distance inside your bounding box space
                float maxDistance = Vector3.Distance(boundsMin, boundsMax);
                // Assign the parameter to your compute shader script properties
                sdfComputeShader.SetFloat("_MaxDistance", maxDistance);

                // Dispatch 1 thread per triangle
                int triCount = mesh.triangles.Length / 3;
                int groupsX = Mathf.CeilToInt((float)triCount / 64f);
                sdfComputeShader.Dispatch(kernelRaster, groupsX, 1, 1);

                // 3. Run Pass 2 to convert integers to float and final cloth texture
                int kernelFinal = sdfComputeShader.FindKernel("FinalizeSDF");
                sdfComputeShader.SetTexture(kernelFinal, "_InterlockingGrid", interlockingGrid);
                sdfComputeShader.SetTexture(kernelFinal, "_Result", sdf3DTexture);

                int finalGroups = Mathf.CeilToInt((float)textureResolution / 8f);
                sdfComputeShader.Dispatch(kernelFinal, finalGroups, finalGroups, finalGroups);

                // Cleanup temporary atomic texture
                interlockingGrid.Release();
                if (cleanup)
                {
                    // Clean up CPU-GPU buffers immediately
                    vertexBuffer.Release();
                    triangleBuffer.Release();
                }
            }

            public void FastSpatialBakeSDF()
            {
                var characterMesh = manager.characterMesh;
                var sdfComputeShader = manager.SdfShader;
                if (sdfComputeShader == null) return;

                var textureResolution = manager.textureResolution;
                InitializeSdfTexture(textureResolution);

                if (!GetBoundsMeshAndMeshBuffers(out bool cleanup, out Mesh mesh, out ComputeBuffer vertexBuffer, out ComputeBuffer triangleBuffer, out ComputeBuffer shapeFrameIndicesBuffer, out ComputeBuffer shapeDeltasBuffer, out Bounds localBounds, out boundsMin, out boundsMax)) return;

                // Calculate the maximum possible distance inside your bounding box space
                float maxDistance = Vector3.Distance(boundsMin, boundsMax);

                int cellCount = 16 * 16 * 16;
                int triCount = triangleBuffer.count;

                // Allocate continuous GPU structures
                ComputeBuffer countBuffer = new ComputeBuffer(cellCount, sizeof(uint));
                ComputeBuffer startBuffer = new ComputeBuffer(cellCount, sizeof(uint));
                ComputeBuffer offsetBuffer = new ComputeBuffer(cellCount, sizeof(uint));
                ComputeBuffer listBuffer = new ComputeBuffer(triCount * 8, sizeof(uint));
                ComputeBuffer counterBuffer = new ComputeBuffer(1, sizeof(uint));

                // 1. Clear Grid Pass
                int kClear = sdfComputeShader.FindKernel("CS_ClearGrid");
                sdfComputeShader.SetBuffer(kClear, "_CellTriangleCount", countBuffer);
                sdfComputeShader.SetBuffer(kClear, "_CellPopulateOffsets", offsetBuffer);
                sdfComputeShader.SetBuffer(kClear, "_GlobalCounter", counterBuffer);
                sdfComputeShader.Dispatch(kClear, Mathf.CeilToInt(cellCount / 64f), 1, 1);

                // 2. Count Intersections Pass
                int kCount = sdfComputeShader.FindKernel(characterMesh == null ? "CS_CountTriangles" : "CS_CountTrianglesWithDeltas");
                sdfComputeShader.SetBuffer(kCount, "_CellTriangleCount", countBuffer);

                sdfComputeShader.SetBuffer(kCount, "_Vertices", vertexBuffer);
                sdfComputeShader.SetBuffer(kCount, "_Triangles", triangleBuffer);
                if (characterMesh != null && characterMesh.TryGetInstanceBuffer<MeshVertexDelta>(characterMesh.SubData.PerVertexDeltaDataPropertyName, out var deltaInstanceBuffer)) deltaInstanceBuffer.BindShaderProperty(sdfComputeShader, kCount, "_Deltas", false);


                if (shapeDeltasBuffer != null)
                {
                    sdfComputeShader.SetBuffer(kCount, "_ShapeDeltas", shapeDeltasBuffer);
                    sdfComputeShader.SetInt("_ShapeDeltasIndexOffset", ((meshShapeFrame + meshShapeIndex) * manager.characterMesh.SubData.VertexCount));
                }
                else
                {
                    sdfComputeShader.SetBuffer(kCount, "_ShapeDeltas", PersistentJobDataTracker.GetEmptyBuffer<MeshVertexDelta>());
                    sdfComputeShader.SetInt("_ShapeDeltasIndexOffset", -1);
                }

                sdfComputeShader.SetInt("_VertexCount", vertexBuffer.count);
                sdfComputeShader.SetInt("_TriangleCount", triangleBuffer.count);
                sdfComputeShader.SetInt("_Resolution", textureResolution);

                sdfComputeShader.SetVector("_BoundsMin", boundsMin);
                sdfComputeShader.SetVector("_BoundsMax", boundsMax);
                sdfComputeShader.Dispatch(kCount, Mathf.CeilToInt(triCount / 64f), 1, 1);

                // 3. CPU Prefix Sum Calculation (Instantaneous for 4096 integers)
                uint[] counts = new uint[cellCount];
                countBuffer.GetData(counts);

                uint[] starts = new uint[cellCount];
                uint currentAccumulator = 0;
                for (int i = 0; i < cellCount; i++)
                {
                    starts[i] = currentAccumulator; // Index tracks where the cell STARTS
                    currentAccumulator += counts[i]; // Accumulate counts for the next block
                }
                startBuffer.SetData(starts);

                // 4. Populate List Pass
                int kPopulate = sdfComputeShader.FindKernel(characterMesh == null ? "CS_PopulateGrid" : "CS_PopulateGridWithDeltas");
                sdfComputeShader.SetBuffer(kPopulate, "_CellStartIndices", startBuffer);
                sdfComputeShader.SetBuffer(kPopulate, "_CellPopulateOffsets", offsetBuffer);
                sdfComputeShader.SetBuffer(kPopulate, "_GridTriangleList", listBuffer);

                sdfComputeShader.SetBuffer(kPopulate, "_Vertices", vertexBuffer);
                sdfComputeShader.SetBuffer(kPopulate, "_Triangles", triangleBuffer);
                if (characterMesh != null && characterMesh.TryGetInstanceBuffer<MeshVertexDelta>(characterMesh.SubData.PerVertexDeltaDataPropertyName, out deltaInstanceBuffer)) deltaInstanceBuffer.BindShaderProperty(sdfComputeShader, kPopulate, "_Deltas", false);

                if (shapeDeltasBuffer != null)
                {
                    sdfComputeShader.SetBuffer(kPopulate, "_ShapeDeltas", shapeDeltasBuffer);
                    sdfComputeShader.SetInt("_ShapeDeltasIndexOffset", ((meshShapeFrame + meshShapeIndex) * manager.characterMesh.SubData.VertexCount));
                }
                else
                {
                    sdfComputeShader.SetBuffer(kPopulate, "_ShapeDeltas", PersistentJobDataTracker.GetEmptyBuffer<MeshVertexDelta>());
                    sdfComputeShader.SetInt("_ShapeDeltasIndexOffset", -1);
                }

                sdfComputeShader.SetInt("_VertexCount", vertexBuffer.count);
                sdfComputeShader.SetInt("_TriangleCount", triangleBuffer.count);
                sdfComputeShader.SetInt("_Resolution", textureResolution);

                sdfComputeShader.SetVector("_BoundsMin", boundsMin);
                sdfComputeShader.SetVector("_BoundsMax", boundsMax);
                sdfComputeShader.Dispatch(kPopulate, Mathf.CeilToInt(triCount / 64f), 1, 1);

                // Reset start indices back to baseline before Pass 4 reads them
                // (Subtracting the local counts shifts our indices back to perfect start markers)
                //for (int i = 0; i < cellCount; i++) { starts[i] -= counts[i]; }
                //startBuffer.SetData(starts);

                // 5. Final High-Fidelity SDF Generation Pass
                int kSDF = sdfComputeShader.FindKernel(characterMesh == null ? "CS_BuildHighDetailSDF" : "CS_BuildHighDetailSDFWithDeltas");
                sdfComputeShader.SetBuffer(kSDF, "_CellTriangleCount", countBuffer);
                sdfComputeShader.SetBuffer(kSDF, "_CellStartIndices", startBuffer);
                sdfComputeShader.SetBuffer(kSDF, "_GridTriangleList", listBuffer);
                sdfComputeShader.SetTexture(kSDF, "_Result", sdf3DTexture);

                sdfComputeShader.SetBuffer(kSDF, "_Vertices", vertexBuffer);
                sdfComputeShader.SetBuffer(kSDF, "_Triangles", triangleBuffer);
                if (characterMesh != null && characterMesh.TryGetInstanceBuffer<MeshVertexDelta>(characterMesh.SubData.PerVertexDeltaDataPropertyName, out deltaInstanceBuffer)) deltaInstanceBuffer.BindShaderProperty(sdfComputeShader, kSDF, "_Deltas", false);

                if (shapeDeltasBuffer != null)
                {
                    sdfComputeShader.SetBuffer(kSDF, "_ShapeDeltas", shapeDeltasBuffer);
                    sdfComputeShader.SetInt("_ShapeDeltasIndexOffset", ((meshShapeFrame + meshShapeIndex) * manager.characterMesh.SubData.VertexCount)); 
                }
                else
                {
                    sdfComputeShader.SetBuffer(kSDF, "_ShapeDeltas", PersistentJobDataTracker.GetEmptyBuffer<MeshVertexDelta>());
                    sdfComputeShader.SetInt("_ShapeDeltasIndexOffset", -1); 
                }

                sdfComputeShader.SetInt("_VertexCount", vertexBuffer.count);
                sdfComputeShader.SetInt("_TriangleCount", triangleBuffer.count);
                sdfComputeShader.SetInt("_Resolution", textureResolution);

                sdfComputeShader.SetVector("_BoundsMin", boundsMin);
                sdfComputeShader.SetVector("_BoundsMax", boundsMax);
                sdfComputeShader.SetFloat("_MaxDistance", maxDistance);
                int groups = Mathf.CeilToInt(64f / 8f);
                sdfComputeShader.Dispatch(kSDF, groups, groups, groups);

                // Release temporary grid structural buffers
                countBuffer.Release(); startBuffer.Release(); offsetBuffer.Release(); listBuffer.Release(); counterBuffer.Release();
            }

            public void ApplyToMaterial(Material material) => ApplyToMaterial(material, "_BodySDFTex", "_SDFBoundsMin", "_SDFBoundsMax", "_Deltas", "_DeltasIndexOffset");
            public void ApplyToMaterial(Material material, string textureProperty, string boundsMinProperty, string boundsMaxProperty, string shapeDeltasProperty, string shapeDeltasIndexOffsetProperty)
            {
                material.SetTexture(textureProperty, sdf3DTexture);
                material.SetVector(boundsMinProperty, boundsMin);
                material.SetVector(boundsMaxProperty, boundsMax);

                if (meshShapeIndex >= 0 && manager.characterMesh != null)
                {
                    material.SetBuffer(shapeDeltasProperty, manager.characterMesh.SubData.MeshShapeFrameDeltasBuffer);
                    material.SetInt(shapeDeltasIndexOffsetProperty, ((meshShapeFrame + meshShapeIndex) * manager.characterMesh.SubData.VertexCount));
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(shapeDeltasProperty)) material.SetBuffer(shapeDeltasProperty, PersistentJobDataTracker.GetEmptyBuffer<MeshVertexDelta>());
                    if (!string.IsNullOrWhiteSpace(shapeDeltasIndexOffsetProperty)) material.SetInt(shapeDeltasIndexOffsetProperty, -1);
                }
            }

            public void ApplyToShader(ComputeShader shader, int kernelIndex) => ApplyToShader(shader, kernelIndex, "_BodySDFTex", "_SDFBoundsMin", "_SDFBoundsMax", "_SDFResolution", "_Deltas", "_DeltasIndexOffset");
            public void ApplyToShader(ComputeShader shader, int kernelIndex, string textureProperty, string boundsMinProperty, string boundsMaxProperty, string resolutionProperty, string shapeDeltasProperty, string shapeDeltasIndexOffsetProperty)
            {
                shader.SetTexture(kernelIndex, textureProperty, sdf3DTexture);
                shader.SetVector(boundsMinProperty, boundsMin);
                shader.SetVector(boundsMaxProperty, boundsMax);
                shader.SetInt(resolutionProperty, manager.textureResolution);

                if (meshShapeIndex >= 0 && manager.characterMesh != null)
                {
                    shader.SetBuffer(kernelIndex, shapeDeltasProperty, manager.characterMesh.SubData.MeshShapeFrameDeltasBuffer);
                    shader.SetInt(shapeDeltasIndexOffsetProperty, ((meshShapeFrame + meshShapeIndex) * manager.characterMesh.SubData.VertexCount));
                } 
                else
                {
                    if (!string.IsNullOrWhiteSpace(shapeDeltasProperty)) shader.SetBuffer(kernelIndex, shapeDeltasProperty, PersistentJobDataTracker.GetEmptyBuffer<MeshVertexDelta>());
                    if (!string.IsNullOrWhiteSpace(shapeDeltasIndexOffsetProperty)) shader.SetInt(shapeDeltasIndexOffsetProperty, -1);
                }
            }

        }

        [SerializeField]
        protected OutputSDF[] outputSDFs;
        public OutputSDF GetOutput(string id)
        {
            foreach (var output in outputSDFs)
            {
                if (output.ID == id) return output;
            }

            return null;
        }
        public OutputSDF GetOutput(int index)
        {
            if (index < 0 || index >= outputSDFs.Length) return null;
            return outputSDFs[index];
        }
        public OutputSDF Output => GetOutput(0);
        public int OutputCount => outputSDFs?.Length ?? 0;
        public IEnumerable<OutputSDF> Outputs()
        {
            if (outputSDFs != null)
            {
                foreach (var output in outputSDFs) yield return output;
            }
        }

        protected virtual void Awake()
        {
            characterMesh = gameObject.GetComponent<CustomizableCharacterMeshV2>();
            meshFilter = gameObject.GetComponent<MeshFilter>(); 

            if (outputSDFs != null)
            {
                foreach (var output in outputSDFs) output.manager = this;
            }
        }

        [Serializable]
        public enum AutoBakeMode
        {
            None,
            Default,
            Fast,
            FastSpatial
        }

        public AutoBakeMode autoBakeMode = AutoBakeMode.FastSpatial;

        private UnityEngine.Events.UnityAction baseMeshListener;
        protected virtual void OnEnable()
        {
            if (characterMesh != null) 
            {
                switch(autoBakeMode)
                {
                    case AutoBakeMode.Default:
                        baseMeshListener = BakeSDF;
                        break;

                    case AutoBakeMode.Fast:
                        baseMeshListener = FastBakeSDF;
                        break;

                    case AutoBakeMode.FastSpatial:
                        baseMeshListener = FastSpatialBakeSDF;
                        break;
                }

                characterMesh.AddListener(API.Unity.ICustomizableCharacter.ListenableEvent.OnAnyDataChanged, baseMeshListener); 
            }
        }
        protected virtual void OnDisable()
        {
            if (characterMesh != null && baseMeshListener != null)
            {
                characterMesh.RemoveListener(API.Unity.ICustomizableCharacter.ListenableEvent.OnAnyDataChanged, baseMeshListener); 
            }
        }

        [BurstCompile]
        public struct DeformVerticesJob : IJobParallelFor
        {
            public int deltasIndexOffset;

            [ReadOnly] public NativeArray<float3> BaseVertices;
            [ReadOnly] public NativeArray<MeshVertexDelta> Deltas;

            [WriteOnly] public NativeArray<float3> DeformedVertices;

            public void Execute(int index)
            {
                DeformedVertices[index] = BaseVertices[index] + Deltas[deltasIndexOffset + index].positionDelta;
            }
        }

        [BurstCompile]
        public struct DeformVerticesWithShapeJob : IJobParallelFor
        {
            public int deltasIndexOffset;
            public int shapeDeltasIndexOffset;

            [ReadOnly] public NativeArray<float3> BaseVertices;
            [ReadOnly] public NativeArray<MeshVertexDelta> Deltas;
            [ReadOnly] public NativeArray<MeshVertexDelta> ShapeDeltas;

            [WriteOnly] public NativeArray<float3> DeformedVertices;

            public void Execute(int index)
            {
                DeformedVertices[index] = BaseVertices[index] + Deltas[deltasIndexOffset + index].positionDelta + ShapeDeltas[shapeDeltasIndexOffset + index].positionDelta;
            }
        }

        // Runs a single-threaded sweep to find the bounding box
        [BurstCompile]
        public struct CalculateBoundsJob : IJob
        {
            [ReadOnly] public NativeArray<float3> DeformedVertices;
            [WriteOnly] public NativeArray<Bounds> OutputBounds;

            public float PaddingScale;

            public void Execute()
            {
                Vector3 minPoint = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                Vector3 maxPoint = new Vector3(float.MinValue, float.MinValue, float.MinValue);

                for (int i = 0; i < DeformedVertices.Length; i++)
                {
                    float3 pos = DeformedVertices[i];

                    // Native Math optimized checks
                    if (pos.x < minPoint.x) minPoint.x = pos.x;
                    if (pos.y < minPoint.y) minPoint.y = pos.y;
                    if (pos.z < minPoint.z) minPoint.z = pos.z;

                    if (pos.x > maxPoint.x) maxPoint.x = pos.x;
                    if (pos.y > maxPoint.y) maxPoint.y = pos.y;
                    if (pos.z > maxPoint.z) maxPoint.z = pos.z;
                }

                Vector3 center = (minPoint + maxPoint) * 0.5f;
                Vector3 size = (maxPoint - minPoint) * PaddingScale;

                OutputBounds[0] = new Bounds(center, size);
            }
        }

        protected static Bounds CalculateDeformedBoundsParallel(JobHandle inputDeps, NativeArray<float3> baseVertices, NativeArray<MeshVertexDelta> currentDeltas, int deltasIndexOffset, float padding = 1.15f) => CalculateDeformedBoundsParallel(inputDeps, baseVertices, currentDeltas, deltasIndexOffset, default, 0, padding);
        protected static Bounds CalculateDeformedBoundsParallel(JobHandle inputDeps, NativeArray<float3> baseVertices, NativeArray<MeshVertexDelta> currentDeltas, int deltasIndexOffset, NativeArray<MeshVertexDelta> shapeDeltas, int shapeDeltasIndexOffset, float padding = 1.15f) 
        {
            int vertexCount = baseVertices.Length; 

            // 1. Allocate Native Containers (Persistent or TempJob depending on frequency)
            NativeArray<float3> nativeDeformed = new NativeArray<float3>(vertexCount, Allocator.TempJob);
            NativeArray<Bounds> nativeResult = new NativeArray<Bounds>(1, Allocator.TempJob);

            // 2. Setup the Parallel Vertex Deformation Job
            JobHandle deformHandle;
            if (shapeDeltas.IsCreated)
            {
                deformHandle = new DeformVerticesWithShapeJob
                {
                    BaseVertices = baseVertices,
                    Deltas = currentDeltas,
                    deltasIndexOffset = deltasIndexOffset,
                    DeformedVertices = nativeDeformed,
                    shapeDeltasIndexOffset = shapeDeltasIndexOffset,
                    ShapeDeltas = shapeDeltas,
                }.Schedule(vertexCount, 64, inputDeps);
            }
            else
            {
                deformHandle = new DeformVerticesJob
                {
                    BaseVertices = baseVertices,
                    Deltas = currentDeltas,
                    deltasIndexOffset = deltasIndexOffset,
                    DeformedVertices = nativeDeformed
                }.Schedule(vertexCount, 64, inputDeps);
            }

            // 3. Setup the Bounds Consolidation Job (Chained to wait for the first job)
            CalculateBoundsJob boundsJob = new CalculateBoundsJob
            {
                DeformedVertices = nativeDeformed,
                OutputBounds = nativeResult,
                PaddingScale = padding
            };

            // Schedule to run immediately after the parallel worker threads finish
            JobHandle boundsHandle = boundsJob.Schedule(deformHandle);

            boundsHandle.Complete();

            // Extract the final calculated Bounds
            Bounds finalBounds = nativeResult[0];

            // Dispose native arrays cleanly to avoid memory leaks
            nativeDeformed.Dispose();
            nativeResult.Dispose();

            return finalBounds;
        }

        protected static readonly Vector3[] _minResult = new Vector3[1];
        protected static readonly Vector3[] _maxResult = new Vector3[1];
        /// <summary>
        /// Deforms vertices and calculates bounds entirely on GPU.
        /// </summary>
        /// <param name="computeShader">Reference to BodyMold.compute shader</param>
        /// <param name="baseVerticesBuffer">Buffer containing base mesh positions (float3[])</param>
        /// <param name="deltasBuffer">Buffer containing vertex deltas (MeshVertexDelta[])</param>
        /// <param name="deltasIndexOffset">Offset into deltasBuffer for this mesh instance</param>
        /// <param name="shapeDeltasBuffer">Optional shape deltas buffer (null if not using shapes)</param>
        /// <param name="shapeDeltasIndexOffset">Offset into shapeDeltasBuffer (0 if not used)</param>
        /// <param name="vertexCount">Number of vertices to process</param>
        /// <param name="padding">Bounds padding scale (default 1.15)</param>
        /// <returns>Final calculated bounds</returns>
        protected Bounds CalculateDeformedBoundsGPU(
            ComputeShader computeShader,
            ComputeBuffer baseVerticesBuffer,
            ComputeBuffer deltasBuffer,
            int deltasIndexOffset,
            ComputeBuffer shapeDeltasBuffer,
            int shapeDeltasIndexOffset,
            int vertexCount,
            float padding = 1.15f)
        {
            InitializeGPUBuffers(vertexCount);

            int kernelDeform = shapeDeltasBuffer != null
                ? computeShader.FindKernel("DeformVerticesWithShape")
                : computeShader.FindKernel("DeformVertices");
            int kernelBounds = computeShader.FindKernel("CalculateBounds");
            int kernelBoundsReduce = computeShader.FindKernel("CalculateBounds_Reduce");

            computeShader.SetBuffer(kernelDeform, "_BaseVertices", baseVerticesBuffer);
            computeShader.SetBuffer(kernelDeform, "_Deltas", deltasBuffer); 
            computeShader.SetBuffer(kernelDeform, "_DeformedVerticesOutput", persistentDeformedVerticesBuffer);
            computeShader.SetInt("_DeltasIndexOffset", deltasIndexOffset);
            computeShader.SetInt("_VertexCount", vertexCount);

            if (shapeDeltasBuffer != null)
            {
                computeShader.SetBuffer(kernelDeform, "_ShapeDeltas", shapeDeltasBuffer);
                computeShader.SetInt("_ShapeDeltasIndexOffset", shapeDeltasIndexOffset); 
            }
            else
            {
                computeShader.SetBuffer(kernelDeform, "_ShapeDeltas", PersistentJobDataTracker.GetEmptyBuffer<MeshVertexDelta>());
                computeShader.SetInt("_ShapeDeltasIndexOffset", -1);
            }

            computeShader.Dispatch(kernelDeform, (vertexCount + 255) / 256, 1, 1);

            int numGroups = (vertexCount + 255) / 256;
            computeShader.SetBuffer(kernelBounds, "DeformedVertices", persistentDeformedVerticesBuffer);
            computeShader.SetBuffer(kernelBounds, "PartialBounds", persistentPartialBoundsBuffer);
            computeShader.SetInt("_DeformedVertexCount", vertexCount);
            computeShader.Dispatch(kernelBounds, numGroups, 1, 1);

            computeShader.SetBuffer(kernelBoundsReduce, "PartialBounds", persistentPartialBoundsBuffer);
            computeShader.SetBuffer(kernelBoundsReduce, "OutputBoundsMin", persistentBoundsMinBuffer);
            computeShader.SetBuffer(kernelBoundsReduce, "OutputBoundsMax", persistentBoundsMaxBuffer);
            computeShader.SetFloat("_PaddingScale", padding);
            computeShader.SetInt("_DeformedVertexCount", vertexCount);
            computeShader.Dispatch(kernelBoundsReduce, 1, 1, 1);

            persistentBoundsMinBuffer.GetData(_minResult);
            persistentBoundsMaxBuffer.GetData(_maxResult);

            Vector3 center = (_minResult[0] + _maxResult[0]) * 0.5f;
            Vector3 size = _maxResult[0] - _minResult[0];

            return new Bounds(center, size);  
        }

        protected virtual void OnDestroy()
        {
            if (outputSDFs != null)
            {
                foreach (var sdf in outputSDFs) sdf.Release(); 
            }

            ReleaseGPUBuffers();
        }

        public void ApplyToMaterial(string outputId, Material material)
        {
            var output = outputId == null ? Output : GetOutput(outputId);
            if (output != null) output.ApplyToMaterial(material);
        }
        public void ApplyToMaterial(string outputId, Material material, string textureProperty, string boundsMinProperty, string boundsMaxProperty, string shapeDeltasProperty, string shapeDeltasIndexOffsetProperty)
        {
            var output = outputId == null ? Output : GetOutput(outputId);
            if (output != null) output.ApplyToMaterial(material, textureProperty, boundsMinProperty, boundsMaxProperty, shapeDeltasProperty, shapeDeltasIndexOffsetProperty);
        }

        public void ApplyToShader(string outputId, ComputeShader shader, int kernelIndex)
        {
            var output = outputId == null ? Output : GetOutput(outputId);
            if (output != null) output.ApplyToShader(shader, kernelIndex);
        }
        public void ApplyToShader(string outputId, ComputeShader shader, int kernelIndex, string textureProperty, string boundsMinProperty, string boundsMaxProperty, string resolutionProperty, string shapeDeltasProperty, string shapeDeltasIndexOffsetProperty)
        {
            var output = outputId == null ? Output : GetOutput(outputId);
            if (output != null) output.ApplyToShader(shader, kernelIndex, textureProperty, boundsMinProperty, boundsMaxProperty, resolutionProperty, shapeDeltasProperty, shapeDeltasIndexOffsetProperty);
        }

        public void BakeSDF()
        {
            if (outputSDFs != null) 
            {
                foreach (var output in outputSDFs) output.BakeSDF();
            }
        }
        public void BakeSDF(string outputId)
        {
            var output = GetOutput(outputId);
            if (output != null) output.BakeSDF();
        }

        public void FastBakeSDF()
        {
            if (outputSDFs != null)
            {
                foreach (var output in outputSDFs) output.FastBakeSDF();
            }
        }
        public void FastBakeSDF(string outputId)
        {
            var output = GetOutput(outputId);
            if (output != null) output.FastBakeSDF();
        }

        public void FastSpatialBakeSDF()
        {
            if (outputSDFs != null)
            {
                foreach (var output in outputSDFs) output.FastSpatialBakeSDF();
            }
        }
        public void FastSpatialBakeSDF(string outputId)
        {
            var output = GetOutput(outputId);
            if (output != null) output.FastSpatialBakeSDF();
        }

    }
}
