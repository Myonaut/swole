using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using UnityEngine.Rendering;
#if UNITY_2021_1_OR_NEWER
#else
using UnityEngine.Rendering.Universal;
#endif

using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;

using Swole.Morphing;
using Swole.Unity;

namespace Swole.Cloth
{
    public class Collision3dTexture : MonoBehaviour
    {
        public static void Copy3dTex(RenderTexture source, RenderTexture destination)
        {
            if (source == null || destination == null) return;
            Graphics.CopyTexture(source, destination);
        }

        public static RenderTexture Create3DTexture(string name, RenderTextureFormat format, int resolution, bool useMipMaps = false)
        {
            RenderTexture rt = new RenderTexture(resolution, resolution, 0, format);
            rt.volumeDepth = resolution;
            rt.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
            rt.enableRandomWrite = true; // Crucial for RWTexture3D access in compute
            rt.useMipMap = useMipMaps;
            rt.filterMode = FilterMode.Bilinear;
            rt.wrapMode = TextureWrapMode.Clamp;
            rt.name = name;
            rt.Create();
            return rt;
        }

        public static RenderTexture CreateSdfTexture(string name, int resolution, bool useMipMaps = false) => Create3DTexture(name, RenderTextureFormat.RFloat, resolution, useMipMaps);

        public static RenderTexture CreateAocvTexture(string name, int resolution, bool useMipMaps = true) => Create3DTexture(name, RenderTextureFormat.ARGBHalf, resolution, useMipMaps);

        [SerializeField]
        protected ComputeShader boundsComputeShader;
        public ComputeShader BoundsShader => boundsComputeShader == null ? sdfComputeShader : boundsComputeShader; 

        [SerializeField]
        protected ComputeShader sdfComputeShader;
        public ComputeShader SdfShader => sdfComputeShader == null ? boundsComputeShader : sdfComputeShader;

        [SerializeField]
        protected ComputeShader aocvComputeShader;
        public ComputeShader AocvShader => aocvComputeShader == null ? sdfComputeShader : aocvComputeShader;

        public int textureResolution = 64; // 64x64x64 is usually ideal for performance/quality
        public float boundsPaddingScale = 1.15f; // Padding scale for bounds calculation to prevent clamping artifacts

        [Tooltip("Maximum depth for negative (inside) SDF values. Set to > 0 to prevent artifacts from open/non-watertight meshes. Recommended: 0.05 - 0.2 for open meshes, 0 for closed meshes.")]
        public float maxNegativeDistance = 0f; // 0 = unlimited (for closed meshes), > 0 = clamp negative distances (for open meshes)

        public float maxNegativeDistanceAOCV = 0.1f;

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

        /*[NonSerialized]
        protected ComputeBuffer persistentDeformedVerticesBuffer;
        [NonSerialized]
        protected ComputeBuffer persistentPartialBoundsBuffer;
        [NonSerialized]
        protected ComputeBuffer persistentBoundsMinBuffer;
        [NonSerialized]
        protected ComputeBuffer persistentBoundsMaxBuffer;
        [NonSerialized]
        protected ComputeBuffer persistentBoundsMaxDistanceBuffer;
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
            persistentBoundsMaxDistanceBuffer = new ComputeBuffer(1, sizeof(float));
        }

        protected void ReleaseGPUBuffers()
        {
            tempCharacterMeshOutputData?.Release();
            persistentDeformedVerticesBuffer?.Release();
            persistentPartialBoundsBuffer?.Release();
            persistentBoundsMinBuffer?.Release();
            persistentBoundsMaxBuffer?.Release();
            persistentBoundsMaxDistanceBuffer?.Release(); 
            allocatedVertexCount = -1;
        }*/

        protected void LeaseGPUBuffers(ComputeBufferLeaseScope scope, int maxVertexCount, out GraphicsBuffer deformedVerticesBuffer, out GraphicsBuffer partialBoundsBuffer, out GraphicsBuffer boundsMinBuffer, out GraphicsBuffer boundsMaxBuffer, out GraphicsBuffer boundsMaxDistanceBuffer)
        {
            deformedVerticesBuffer = scope.Require(GraphicsBuffer.Target.Structured, maxVertexCount, sizeof(float) * 3);

            int maxGroups = (maxVertexCount + 255) / 256;
            partialBoundsBuffer = scope.Require(GraphicsBuffer.Target.Structured, maxGroups, sizeof(float) * 6);
            boundsMinBuffer = scope.Require(GraphicsBuffer.Target.Structured, 1, sizeof(float) * 3);
            boundsMaxBuffer = scope.Require(GraphicsBuffer.Target.Structured, 1, sizeof(float) * 3);
            boundsMaxDistanceBuffer = scope.Require(GraphicsBuffer.Target.Structured, 1, sizeof(float));
        }

        [Serializable]
        public class OutputCollision
        {
            [NonSerialized]
            public Collision3dTexture manager;

            [SerializeField]
            private string id;
            public string ID => string.IsNullOrWhiteSpace(id) ? MeshShapeName : id;

            [SerializeField, Tooltip("Optional mesh shape to apply for a customizable character mesh")]
            private string meshShapeName;
            public string MeshShapeName => meshShapeName;

            [SerializeField]
            private int meshShapeFrame;
            public int MeshShapeFrame => meshShapeFrame;

            [SerializeField]
            private float meshShapeWeight = 1f;
            public float MeshShapeWeight => meshShapeWeight;

            [NonSerialized]
            private int meshShapeIndex = -1;
            public int MeshShapeIndex => meshShapeIndex;
            public bool HasMeshShape => meshShapeIndex >= 0 || !string.IsNullOrWhiteSpace(meshShapeName);  

            [SerializeField]
            private RenderTexture sdf3DTexture;
            public RenderTexture Texture => sdf3DTexture;

            [SerializeField]
            private RenderTexture sdf3DTextureSimplified;
            public RenderTexture SimplifiedTexture => sdf3DTextureSimplified;

            private RenderTexture sdf3DTextureTemp;

            private RenderTexture interlockingGrid;

            public void InitializeSdfTexture(int textureResolution)
            {
                // Initialize the 3D Render Texture if not already done
                if (sdf3DTexture == null || !sdf3DTexture.IsCreated())
                {
                    sdf3DTexture = CreateSdfTexture($"SDF_{id}", textureResolution);
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

            public void InitializeSimplifiedSdfTexture(int textureResolution)
            {
                // Initialize the 3D Render Texture if not already done
                if (sdf3DTextureSimplified == null || !sdf3DTextureSimplified.IsCreated())
                {
                    sdf3DTextureSimplified = CreateSdfTexture($"SDF_Simplified_{id}", textureResolution);
                }
            }
            protected void InitializeTemporarySdfTexture(int textureResolution)
            {
                // Initialize the 3D Render Texture if not already done
                if (sdf3DTextureTemp == null || !sdf3DTextureTemp.IsCreated())
                {
                    sdf3DTextureTemp = CreateSdfTexture($"SDF_Temp_{id}", textureResolution);
                }
            }
            protected void InitializeInterlockingGridTexture(int textureResolution)
            {
                // Initialize the 3D Render Texture if not already done
                if (interlockingGrid == null || !interlockingGrid.IsCreated())
                {
                    interlockingGrid = Create3DTexture($"SDF_InterlockingGrid_{id}", RenderTextureFormat.RInt, textureResolution, false);
                }
            }

            [SerializeField]
            protected RenderTexture aocvCoreTexture;
            [SerializeField]
            protected RenderTexture aocvSurfaceNormalTexture;
            public RenderTexture AocvCoreTexture => aocvCoreTexture;
            public RenderTexture AocvSurfaceNormalTexture => aocvSurfaceNormalTexture;

            public void InitializeAocvTextures(int textureResolution)
            {
                if (aocvCoreTexture == null || !aocvCoreTexture.IsCreated())
                {
                    // Configure the Core Mapping Volume (RGB = Anchor, A = Safe Radius)
                    aocvCoreTexture = CreateAocvTexture($"AOCV_Core_Volume_{id}", textureResolution);
                }
                if (aocvSurfaceNormalTexture == null || !aocvSurfaceNormalTexture.IsCreated())
                {
                    // Configure the Surface Orientation Volume (RGB = Normal, A = SDF Distance)
                    aocvSurfaceNormalTexture = CreateAocvTexture($"AOCV_Normal_Volume_{id}", textureResolution);
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

                if (sdf3DTextureSimplified != null)
                {
                    sdf3DTextureSimplified.Release();
                    sdf3DTextureSimplified = null;
                }

                if (sdf3DTextureTemp != null)
                {
                    sdf3DTextureTemp.Release();
                    sdf3DTextureTemp = null;
                }

                if (aocvCoreTexture != null)
                {
                    aocvCoreTexture.Release();
                    aocvCoreTexture = null;
                }

                if (aocvSurfaceNormalTexture != null)
                {
                    aocvSurfaceNormalTexture.Release();
                    aocvSurfaceNormalTexture = null;
                }
            }

            /*[NonSerialized]
            public Vector3 boundsMin;
            public Vector3 BoundsMin => boundsMin;

            [NonSerialized]
            public Vector3 boundsMax;
            public Vector3 BoundsMax => boundsMax;*/

            protected bool GetBoundsMeshAndMeshBuffers(
                CommandBuffer cmd,
                ComputeBufferLeaseScope bufferLeaseScope,
                out GraphicsBuffer deformedVerticesBuffer,
                out GraphicsBuffer partialBoundsBuffer,
                out GraphicsBuffer boundsMinBuffer,
                out GraphicsBuffer boundsMaxBuffer,
                out GraphicsBuffer maxDistanceBuffer,
                float boundsPaddingScale, out bool cleanup, out Mesh mesh, out ComputeBuffer vertexBuffer, out ComputeBuffer triangleBuffer, out ComputeBuffer shapeFrameIndicesBuffer, out ComputeBuffer shapeDeltasBuffer/*, out Bounds localBounds, out Vector3 boundsMin, out Vector3 boundsMax*/)
            {
                deformedVerticesBuffer = null;
                partialBoundsBuffer = null;
                boundsMinBuffer = null;
                boundsMaxBuffer = null;
                maxDistanceBuffer = null;

                cleanup = true;
                mesh = null;
                vertexBuffer = null;
                triangleBuffer = null;
                shapeFrameIndicesBuffer = null; 
                shapeDeltasBuffer = null;
                //localBounds = default;
                //boundsMin = Vector3.zero;
                //boundsMax = Vector3.zero;
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

                    manager.LeaseGPUBuffers(bufferLeaseScope, mesh.vertexCount, out deformedVerticesBuffer, out partialBoundsBuffer, out boundsMinBuffer, out boundsMaxBuffer, out maxDistanceBuffer);

                    //localBounds = manager.CalculateDeformedBoundsGPU(
                    manager.CalculateDeformedBoundsGPU_NoReadback(
                        cmd,
                        deformedVerticesBuffer,
                        partialBoundsBuffer,
                        boundsMinBuffer,
                        boundsMaxBuffer,
                        maxDistanceBuffer,
                        manager.BoundsShader,
                        vertexBuffer,
                        deltasBuffer,
                        deltasOffset,
                        shapeDeltasBuffer,
                        shapeDeltasOffset,
                        vertexCount,
                        boundsPaddingScale); // Padding scale

                    //boundsMin = localBounds.min;
                    //boundsMax = localBounds.max;
                }
                else if (manager.meshFilter != null)
                {
                    mesh = manager.meshFilter.sharedMesh;

                    //localBounds = mesh.bounds;
                    float padding = 0.05f * boundsPaddingScale; // 5cm padding to prevent clamping artifacts at edges
                    //boundsMin = localBounds.min - new Vector3(padding, padding, padding);
                    //boundsMax = localBounds.max + new Vector3(padding, padding, padding);

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

                using (var buffers = new ComputeBufferLeaseScope())
                {
                    CommandBuffer cmd = CommandBufferPool.Get("BakeSDF");

                    if (!GetBoundsMeshAndMeshBuffers(cmd, buffers,
                        out var deformedVerticesBuffer, out var partialBoundsBuffer, out var boundsMinBuffer, out var boundsMaxBuffer, out var maxDistanceBuffer,
                        manager.boundsPaddingScale, out bool cleanup, out Mesh mesh, out ComputeBuffer vertexBuffer, out ComputeBuffer triangleBuffer, out ComputeBuffer shapeFrameIndicesBuffer, out ComputeBuffer shapeDeltasBuffer/*, out Bounds localBounds, out boundsMin, out boundsMax*/)) return;

                    int kernel = sdfComputeShader.FindKernel(characterMesh == null ? "Generate" : "GenerateWithDeltas");

                    // Set Compute Shader Parameters
                    cmd.SetComputeTextureParam(sdfComputeShader, kernel, "_Result", sdf3DTexture);
                    cmd.SetComputeBufferParam(sdfComputeShader, kernel, "_Vertices", vertexBuffer);
                    cmd.SetComputeBufferParam(sdfComputeShader, kernel, "_Triangles", triangleBuffer);
                    if (characterMesh != null && characterMesh.TryGetInstanceBuffer<MeshVertexDelta>(characterMesh.SubData.PerVertexDeltaDataPropertyName, out var deltaInstanceBuffer))
                    {
                        cmd.SetComputeBufferParam(sdfComputeShader, kernel, "_Deltas", deltaInstanceBuffer.BufferThisFrame);
                    }

                    if (shapeDeltasBuffer != null)
                    {
                        cmd.SetComputeBufferParam(sdfComputeShader, kernel, "_ShapeDeltas", shapeDeltasBuffer);
                        cmd.SetComputeIntParam(sdfComputeShader, "_ShapeDeltasIndexOffset", ((meshShapeFrame + meshShapeIndex) * manager.characterMesh.SubData.VertexCount));
                    }
                    else
                    {
                        cmd.SetComputeBufferParam(sdfComputeShader, kernel, "_ShapeDeltas", PersistentJobDataTracker.GetEmptyGraphicsBuffer());
                        cmd.SetComputeIntParam(sdfComputeShader, "_ShapeDeltasIndexOffset", -1);
                    }

                    cmd.SetComputeIntParam(sdfComputeShader, "_VertexCount", vertexBuffer.count);
                    cmd.SetComputeIntParam(sdfComputeShader, "_TriangleCount", triangleBuffer.count);
                    cmd.SetComputeIntParam(sdfComputeShader, "_Resolution", textureResolution);

                    //cmd.SetComputeVectorParam(sdfComputeShader, "_BoundsMin", boundsMin);
                    //cmd.SetComputeVectorParam(sdfComputeShader, "_BoundsMax", boundsMax);
                    cmd.SetComputeBufferParam(sdfComputeShader, kernel, "_BoundsMin", boundsMinBuffer);
                    cmd.SetComputeBufferParam(sdfComputeShader, kernel, "_BoundsMax", boundsMaxBuffer);

                    // Set max negative distance to prevent artifacts from open meshes
                    cmd.SetComputeFloatParam(sdfComputeShader, "_MaxNegativeDistance", manager.maxNegativeDistance);

                    // Dispatch Compute Shader (Thread groups of 8x8x8 to match the HLSL layout)
                    int threadGroups = Mathf.CeilToInt((float)textureResolution / 8f);
                    cmd.DispatchCompute(sdfComputeShader, kernel, threadGroups, threadGroups, threadGroups);

                    Graphics.ExecuteCommandBuffer(cmd);
                    CommandBufferPool.Release(cmd);

                    if (cleanup)
                    {
                        // Clean up CPU-GPU buffers immediately
                        vertexBuffer.Release();
                        triangleBuffer.Release();
                    }

                }

                //SmoothSDFVolume(5);
            }

            public void FastBakeSDF()
            {
                var characterMesh = manager.characterMesh;
                var sdfComputeShader = manager.SdfShader;
                if (sdfComputeShader == null) return;
                 
                var textureResolution = manager.textureResolution;
                InitializeSdfTexture(textureResolution);

                using (var buffers = new ComputeBufferLeaseScope())
                {
                    CommandBuffer cmd = CommandBufferPool.Get("FastBakeSDF");

                    if (!GetBoundsMeshAndMeshBuffers(cmd, buffers,
                        out var deformedVerticesBuffer, out var partialBoundsBuffer, out var boundsMinBuffer, out var boundsMaxBuffer, out var maxDistanceBuffer,
                        manager.boundsPaddingScale, out bool cleanup, out Mesh mesh, out ComputeBuffer vertexBuffer, out ComputeBuffer triangleBuffer, out ComputeBuffer shapeFrameIndicesBuffer, out ComputeBuffer shapeDeltasBuffer/*, out Bounds localBounds, out boundsMin, out boundsMax*/)) return;

                    // 1. Create a temporary interlocking Texture if it doesn't exist
                    InitializeInterlockingGridTexture(textureResolution);

                    // 2. Clear the texture to uint.MaxValue (equivalent to infinite distance)
                    int kernelInit = sdfComputeShader.FindKernel("InitializeGrid");
                    cmd.SetComputeTextureParam(sdfComputeShader, kernelInit, "_InterlockingGrid", interlockingGrid);
                    cmd.SetComputeIntParam(sdfComputeShader, "_Resolution", textureResolution);

                    int initGroups = Mathf.CeilToInt((float)textureResolution / 8f);
                    cmd.DispatchCompute(sdfComputeShader, kernelInit, initGroups, initGroups, initGroups);

                    // Rasterize Triangles
                    int kernelRaster = sdfComputeShader.FindKernel(characterMesh == null ? "RasterizeTriangles" : "RasterizeTrianglesWithDeltas");
                    cmd.SetComputeTextureParam(sdfComputeShader, kernelRaster, "_InterlockingGrid", interlockingGrid);

                    cmd.SetComputeBufferParam(sdfComputeShader, kernelRaster, "_Vertices", vertexBuffer);
                    cmd.SetComputeBufferParam(sdfComputeShader, kernelRaster, "_Triangles", triangleBuffer);
                    if (characterMesh != null && characterMesh.TryGetInstanceBuffer<MeshVertexDelta>(characterMesh.SubData.PerVertexDeltaDataPropertyName, out var deltaInstanceBuffer))
                    {
                        cmd.SetComputeBufferParam(sdfComputeShader, kernelRaster, "_Deltas", deltaInstanceBuffer.BufferThisFrame);
                    }

                    if (shapeDeltasBuffer != null)
                    {
                        cmd.SetComputeBufferParam(sdfComputeShader, kernelRaster, "_ShapeDeltas", shapeDeltasBuffer);
                        cmd.SetComputeIntParam(sdfComputeShader, "_ShapeDeltasIndexOffset", ((meshShapeFrame + meshShapeIndex) * manager.characterMesh.SubData.VertexCount));
                    }
                    else
                    {
                        cmd.SetComputeBufferParam(sdfComputeShader, kernelRaster, "_ShapeDeltas", PersistentJobDataTracker.GetEmptyGraphicsBuffer());
                        cmd.SetComputeIntParam(sdfComputeShader, "_ShapeDeltasIndexOffset", -1);
                    }

                    cmd.SetComputeIntParam(sdfComputeShader, "_VertexCount", vertexBuffer.count);
                    cmd.SetComputeIntParam(sdfComputeShader, "_TriangleCount", triangleBuffer.count);
                    cmd.SetComputeIntParam(sdfComputeShader, "_Resolution", textureResolution);

                    //cmd.SetComputeVectorParam(sdfComputeShader, "_BoundsMin", boundsMin);
                    //cmd.SetComputeVectorParam(sdfComputeShader, "_BoundsMax", boundsMax);
                    // Calculate the maximum possible distance inside your bounding box space
                    //float maxDistance = Vector3.Distance(boundsMin, boundsMax);
                    // Assign the parameter to your compute shader script properties
                    //cmd.SetComputeFloatParam(sdfComputeShader, "_MaxDistance", maxDistance);

                    cmd.SetComputeBufferParam(sdfComputeShader, kernelRaster, "_BoundsMin", boundsMinBuffer);
                    cmd.SetComputeBufferParam(sdfComputeShader, kernelRaster, "_BoundsMax", boundsMaxBuffer);
                    cmd.SetComputeBufferParam(sdfComputeShader, kernelRaster, "_MaxDistance", maxDistanceBuffer);

                    // Dispatch 1 thread per triangle
                    int triCount = mesh.triangles.Length / 3;
                    int groupsX = Mathf.CeilToInt((float)triCount / 64f);
                    cmd.DispatchCompute(sdfComputeShader, kernelRaster, groupsX, 1, 1);

                    // 3. Run Pass 2 to convert integers to float and final cloth texture
                    int kernelFinal = sdfComputeShader.FindKernel("FinalizeSDF");
                    cmd.SetComputeTextureParam(sdfComputeShader, kernelFinal, "_InterlockingGrid", interlockingGrid);
                    cmd.SetComputeTextureParam(sdfComputeShader, kernelFinal, "_Result", sdf3DTexture);

                    int finalGroups = Mathf.CeilToInt((float)textureResolution / 8f);
                    cmd.DispatchCompute(sdfComputeShader, kernelFinal, finalGroups, finalGroups, finalGroups);

                    Graphics.ExecuteCommandBuffer(cmd);
                    CommandBufferPool.Release(cmd);

                    if (cleanup)
                    {
                        // Clean up CPU-GPU buffers immediately
                        vertexBuffer.Release();
                        triangleBuffer.Release();
                    }
                }
            }

            public void FastSpatialBakeSDF()
            {
                var characterMesh = manager.characterMesh;
                var sdfComputeShader = manager.SdfShader;
                if (sdfComputeShader == null) return;

                var textureResolution = manager.textureResolution;
                InitializeSdfTexture(textureResolution);

                using (var buffers = new ComputeBufferLeaseScope())
                {
                    CommandBuffer cmd = CommandBufferPool.Get("FastSpatialBakeSDF_PASS1");

                    if (!GetBoundsMeshAndMeshBuffers(cmd, buffers,
                        out var deformedVerticesBuffer, out var partialBoundsBuffer, out var boundsMinBuffer, out var boundsMaxBuffer, out var maxDistanceBuffer,
                        manager.boundsPaddingScale, out bool cleanup, out Mesh mesh, out ComputeBuffer vertexBuffer, out ComputeBuffer triangleBuffer, out ComputeBuffer shapeFrameIndicesBuffer, out ComputeBuffer shapeDeltasBuffer/*, out Bounds localBounds, out boundsMin, out boundsMax*/)) return;

                    int cellCount = 16 * 16 * 16;
                    int triCount = triangleBuffer.count;

                    GraphicsBuffer countBuffer = buffers.Require(GraphicsBuffer.Target.Structured, cellCount, sizeof(uint));
                    GraphicsBuffer startBuffer = buffers.Require(GraphicsBuffer.Target.Structured, cellCount, sizeof(uint));
                    GraphicsBuffer offsetBuffer = buffers.Require(GraphicsBuffer.Target.Structured, cellCount, sizeof(uint));
                    GraphicsBuffer listBuffer = buffers.Require(GraphicsBuffer.Target.Structured, triCount * 8, sizeof(uint)); 
                    GraphicsBuffer counterBuffer = buffers.Require(GraphicsBuffer.Target.Structured, 1, sizeof(uint));  

                    // Calculate the maximum possible distance inside your bounding box space
                    //float maxDistance = Vector3.Distance(boundsMin, boundsMax);

                    // Allocate continuous GPU structures **Switched to using ComputeBufferLeaseScope**
                    //ComputeBuffer countBuffer = new ComputeBuffer(cellCount, sizeof(uint));
                    //ComputeBuffer startBuffer = new ComputeBuffer(cellCount, sizeof(uint));
                    //ComputeBuffer offsetBuffer = new ComputeBuffer(cellCount, sizeof(uint));
                    //ComputeBuffer listBuffer = new ComputeBuffer(triCount * 8, sizeof(uint));
                    //ComputeBuffer counterBuffer = new ComputeBuffer(1, sizeof(uint));
                     
                    // 1. Clear Grid Pass
                    int kClear = sdfComputeShader.FindKernel("CS_ClearGrid");
                    cmd.SetComputeBufferParam(sdfComputeShader, kClear, "_CellTriangleCount", countBuffer);
                    cmd.SetComputeBufferParam(sdfComputeShader, kClear, "_CellPopulateOffsets", offsetBuffer);
                    cmd.SetComputeBufferParam(sdfComputeShader, kClear, "_GlobalCounter", counterBuffer);
                    cmd.DispatchCompute(sdfComputeShader, kClear, Mathf.CeilToInt(cellCount / 64f), 1, 1);

                    // 2. Count Intersections Pass
                    int kCount = sdfComputeShader.FindKernel(characterMesh == null ? "CS_CountTriangles" : "CS_CountTrianglesWithDeltas");
                    cmd.SetComputeBufferParam(sdfComputeShader, kCount, "_CellTriangleCount", countBuffer);

                    cmd.SetComputeBufferParam(sdfComputeShader, kCount, "_Vertices", vertexBuffer);
                    cmd.SetComputeBufferParam(sdfComputeShader, kCount, "_Triangles", triangleBuffer);
                    if (characterMesh != null && characterMesh.TryGetInstanceBuffer<MeshVertexDelta>(characterMesh.SubData.PerVertexDeltaDataPropertyName, out var deltaInstanceBuffer))
                    {
                        cmd.SetComputeBufferParam(sdfComputeShader, kCount, "_Deltas", deltaInstanceBuffer.BufferThisFrame);
                    }


                    if (shapeDeltasBuffer != null)
                    {
                        cmd.SetComputeBufferParam(sdfComputeShader, kCount, "_ShapeDeltas", shapeDeltasBuffer);
                        cmd.SetComputeIntParam(sdfComputeShader, "_ShapeDeltasIndexOffset", ((meshShapeFrame + meshShapeIndex) * manager.characterMesh.SubData.VertexCount));
                    }
                    else
                    {
                        cmd.SetComputeBufferParam(sdfComputeShader, kCount, "_ShapeDeltas", PersistentJobDataTracker.GetEmptyGraphicsBuffer());
                        cmd.SetComputeIntParam(sdfComputeShader, "_ShapeDeltasIndexOffset", -1);
                    }

                    cmd.SetComputeIntParam(sdfComputeShader, "_VertexCount", vertexBuffer.count);
                    cmd.SetComputeIntParam(sdfComputeShader, "_TriangleCount", triangleBuffer.count);
                    cmd.SetComputeIntParam(sdfComputeShader, "_Resolution", textureResolution);

                    //cmd.SetComputeVectorParam(sdfComputeShader, "_BoundsMin", boundsMin);
                    //cmd.SetComputeVectorParam(sdfComputeShader, "_BoundsMax", boundsMax);
                    cmd.SetComputeBufferParam(sdfComputeShader, kCount, "_BoundsMin", boundsMinBuffer);
                    cmd.SetComputeBufferParam(sdfComputeShader, kCount, "_BoundsMax", boundsMaxBuffer);
                    cmd.SetComputeBufferParam(sdfComputeShader, kCount, "_MaxDistance", maxDistanceBuffer);
                    cmd.DispatchCompute(sdfComputeShader, kCount, Mathf.CeilToInt(triCount / 64f), 1, 1);

                    /*Graphics.ExecuteCommandBuffer(cmd);
                    CommandBufferPool.Release(cmd);

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

                    cmd = CommandBufferPool.Get("FastSpatialBakeSDF_PASS2");*/

                    // 3. GPU Prefix Sum (Replaces the slow CPU loop and GetData/SetData)
                    int kSum = sdfComputeShader.FindKernel("CS_PrefixSum");
                    cmd.SetComputeBufferParam(sdfComputeShader, kSum, "_CellTriangleCount", countBuffer);
                    cmd.SetComputeBufferParam(sdfComputeShader, kSum, "_CellStartIndices", startBuffer);
                    // Dispatch exactly 1 thread group, because our kernel handles all 4096 elements internally
                    cmd.DispatchCompute(sdfComputeShader, kSum, 1, 1, 1);

                    // 4. Populate List Pass
                    int kPopulate = sdfComputeShader.FindKernel(characterMesh == null ? "CS_PopulateGrid" : "CS_PopulateGridWithDeltas");
                    cmd.SetComputeBufferParam(sdfComputeShader, kPopulate, "_CellStartIndices", startBuffer);
                    cmd.SetComputeBufferParam(sdfComputeShader, kPopulate, "_CellPopulateOffsets", offsetBuffer);
                    cmd.SetComputeBufferParam(sdfComputeShader, kPopulate, "_GridTriangleList", listBuffer);

                    cmd.SetComputeBufferParam(sdfComputeShader, kPopulate, "_Vertices", vertexBuffer);
                    cmd.SetComputeBufferParam(sdfComputeShader, kPopulate, "_Triangles", triangleBuffer);
                    if (characterMesh != null && characterMesh.TryGetInstanceBuffer<MeshVertexDelta>(characterMesh.SubData.PerVertexDeltaDataPropertyName, out deltaInstanceBuffer))
                    {
                        cmd.SetComputeBufferParam(sdfComputeShader, kPopulate, "_Deltas", deltaInstanceBuffer.BufferThisFrame);
                    }

                    if (shapeDeltasBuffer != null)
                    {
                        cmd.SetComputeBufferParam(sdfComputeShader, kPopulate, "_ShapeDeltas", shapeDeltasBuffer);
                        cmd.SetComputeIntParam(sdfComputeShader, "_ShapeDeltasIndexOffset", ((meshShapeFrame + meshShapeIndex) * manager.characterMesh.SubData.VertexCount));
                    }
                    else
                    {
                        cmd.SetComputeBufferParam(sdfComputeShader, kPopulate, "_ShapeDeltas", PersistentJobDataTracker.GetEmptyGraphicsBuffer());
                        cmd.SetComputeIntParam(sdfComputeShader, "_ShapeDeltasIndexOffset", -1);
                    }

                    cmd.SetComputeIntParam(sdfComputeShader, "_VertexCount", vertexBuffer.count);
                    cmd.SetComputeIntParam(sdfComputeShader, "_TriangleCount", triangleBuffer.count);
                    cmd.SetComputeIntParam(sdfComputeShader, "_Resolution", textureResolution);

                    //cmd.SetComputeVectorParam(sdfComputeShader, "_BoundsMin", boundsMin);
                    //cmd.SetComputeVectorParam(sdfComputeShader, "_BoundsMax", boundsMax);
                    cmd.SetComputeBufferParam(sdfComputeShader, kPopulate, "_BoundsMin", boundsMinBuffer);
                    cmd.SetComputeBufferParam(sdfComputeShader, kPopulate, "_BoundsMax", boundsMaxBuffer);
                    cmd.SetComputeBufferParam(sdfComputeShader, kPopulate, "_MaxDistance", maxDistanceBuffer);
                    cmd.DispatchCompute(sdfComputeShader, kPopulate, Mathf.CeilToInt(triCount / 64f), 1, 1);

                    // Reset start indices back to baseline before Pass 4 reads them
                    // (Subtracting the local counts shifts our indices back to perfect start markers)
                    //for (int i = 0; i < cellCount; i++) { starts[i] -= counts[i]; }
                    //startBuffer.SetData(starts);

                    // 5. Final High-Fidelity SDF Generation Pass
                    int kSDF = sdfComputeShader.FindKernel(characterMesh == null ? "CS_BuildHighDetailSDF" : "CS_BuildHighDetailSDFWithDeltas");
                    cmd.SetComputeBufferParam(sdfComputeShader, kSDF, "_CellTriangleCount", countBuffer);
                    cmd.SetComputeBufferParam(sdfComputeShader, kSDF, "_CellStartIndices", startBuffer);
                    cmd.SetComputeBufferParam(sdfComputeShader, kSDF, "_GridTriangleList", listBuffer);
                    cmd.SetComputeTextureParam(sdfComputeShader, kSDF, "_Result", sdf3DTexture);

                    cmd.SetComputeBufferParam(sdfComputeShader, kSDF, "_Vertices", vertexBuffer);
                    cmd.SetComputeBufferParam(sdfComputeShader, kSDF, "_Triangles", triangleBuffer);
                    if (characterMesh != null && characterMesh.TryGetInstanceBuffer<MeshVertexDelta>(characterMesh.SubData.PerVertexDeltaDataPropertyName, out deltaInstanceBuffer))
                    {
                        cmd.SetComputeBufferParam(sdfComputeShader, kSDF, "_Deltas", deltaInstanceBuffer.BufferThisFrame);
                    }

                    if (shapeDeltasBuffer != null)
                    {
                        cmd.SetComputeBufferParam(sdfComputeShader, kSDF, "_ShapeDeltas", shapeDeltasBuffer);
                        cmd.SetComputeIntParam(sdfComputeShader, "_ShapeDeltasIndexOffset", ((meshShapeFrame + meshShapeIndex) * manager.characterMesh.SubData.VertexCount));
                    }
                    else
                    {
                        cmd.SetComputeBufferParam(sdfComputeShader, kSDF, "_ShapeDeltas", PersistentJobDataTracker.GetEmptyGraphicsBuffer());
                        cmd.SetComputeIntParam(sdfComputeShader, "_ShapeDeltasIndexOffset", -1);
                    }

                    cmd.SetComputeIntParam(sdfComputeShader, "_VertexCount", vertexBuffer.count);
                    cmd.SetComputeIntParam(sdfComputeShader, "_TriangleCount", triangleBuffer.count);
                    cmd.SetComputeIntParam(sdfComputeShader, "_Resolution", textureResolution);

                    //cmd.SetComputeVectorParam(sdfComputeShader, "_BoundsMin", boundsMin);
                    //cmd.SetComputeVectorParam(sdfComputeShader, "_BoundsMax", boundsMax);
                    //cmd.SetComputeFloatParam(sdfComputeShader, "_MaxDistance", maxDistance);
                    cmd.SetComputeBufferParam(sdfComputeShader, kSDF, "_BoundsMin", boundsMinBuffer);
                    cmd.SetComputeBufferParam(sdfComputeShader, kSDF, "_BoundsMax", boundsMaxBuffer);
                    cmd.SetComputeBufferParam(sdfComputeShader, kSDF, "_MaxDistance", maxDistanceBuffer);

                    // Set max negative distance to prevent artifacts from open meshes
                    cmd.SetComputeFloatParam(sdfComputeShader, "_MaxNegativeDistance", manager.maxNegativeDistance);

                    int groups = Mathf.CeilToInt(textureResolution / 8f);
                    cmd.DispatchCompute(sdfComputeShader, kSDF, groups, groups, groups);

                    Graphics.ExecuteCommandBuffer(cmd);
                    CommandBufferPool.Release(cmd);

                }

                // Release temporary grid structural buffers
                //countBuffer.Release(); startBuffer.Release(); offsetBuffer.Release(); listBuffer.Release(); counterBuffer.Release();

                //SmoothSDFVolume(5);
            }

            public void SimplifySDFVolume() 
            {
                if (sdf3DTexture == null) return;

                var sdfComputeShader = manager.SdfShader;
                if (sdfComputeShader == null) return;

                InitializeSimplifiedSdfTexture(sdf3DTexture.width);

                CommandBuffer cmd = CommandBufferPool.Get("SimplifySDFVolume");

                int kernelIdx = sdfComputeShader.FindKernel("CS_MorphologicalClosingSDF"); 

                // 1. Extract dimensions from your active 3D input assets
                int resX = sdf3DTexture.width;
                int resY = sdf3DTexture.height;
                int resZ = sdf3DTexture.volumeDepth; // volumeDepth extracts 3D Layer metrics

                // 2. Set structural integer uniform variables inside the shader
                //sdfComputeShader.SetInt("_SDFResolutionX", resX);
                //sdfComputeShader.SetInt("_SDFResolutionY", resY);
                //sdfComputeShader.SetInt("_SDFResolutionZ", resZ);
                cmd.SetComputeIntParam(sdfComputeShader, "_Resolution", resX);

                // 3. Bind the respective read/write textures
                cmd.SetComputeTextureParam(sdfComputeShader, kernelIdx, "_Source", sdf3DTexture);
                cmd.SetComputeTextureParam(sdfComputeShader, kernelIdx, "_Result", sdf3DTextureSimplified);

                // 4. Calculate matching thread group allocation counts (divided by [numthreads(8,8,8)])
                int threadGroupsX = Mathf.CeilToInt(resX / 8f);
                int threadGroupsY = Mathf.CeilToInt(resY / 8f);
                int threadGroupsZ = Mathf.CeilToInt(resZ / 8f);

                // 5. Fire execution command to the GPU
                cmd.DispatchCompute(sdfComputeShader, kernelIdx, threadGroupsX, threadGroupsY, threadGroupsZ);

                Graphics.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            public void SmoothSDFVolume(int smoothingIterations)
            {
                if (sdf3DTexture == null) return;

                var sdfComputeShader = manager.SdfShader;
                if (sdfComputeShader == null) return;

                InitializeSimplifiedSdfTexture(sdf3DTexture.width);

                CommandBuffer cmd = CommandBufferPool.Get("SmoothSDFVolume");

                Copy3dTex(sdf3DTexture, sdf3DTextureSimplified);

                int kernelIdx = sdfComputeShader.FindKernel("CS_SmoothSDFCrevices");

                // 1. Extract non-symmetrical dimensions from the 3D asset
                int resX = sdf3DTexture.width;
                int resY = sdf3DTexture.height;
                int resZ = sdf3DTexture.volumeDepth;

                // 2. Pass resolutions to the compute uniform parameters
                //sdfComputeShader.SetInt("_SDFResolutionX", resX);
                //sdfComputeShader.SetInt("_SDFResolutionY", resY);
                //sdfComputeShader.SetInt("_SDFResolutionZ", resZ);
                cmd.SetComputeIntParam(sdfComputeShader, "_Resolution", resX);

                // 3. Bind the single read-write target texture to the matching kernel parameter
                cmd.SetComputeTextureParam(sdfComputeShader, kernelIdx, "_Result", sdf3DTextureSimplified);

                // 4. Calculate matching thread group allocation blocks (divided by [numthreads(8,8,8)])
                int threadGroupsX = Mathf.CeilToInt(resX / 8f);
                int threadGroupsY = Mathf.CeilToInt(resY / 8f);
                int threadGroupsZ = Mathf.CeilToInt(resZ / 8f);

                // 5. Fire sequential loops to dissolve crevices progressively
                for (int i = 0; i < smoothingIterations; i++)
                {
                    cmd.DispatchCompute(sdfComputeShader, kernelIdx, threadGroupsX, threadGroupsY, threadGroupsZ);  
                    // Note: Since we are modifying a single texture in-place via an UAV texture block,
                    // the GPU natively queues sequential dispatches so it safely reads the results 
                    // of iteration 1 at the start of iteration 2.
                }

                Graphics.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            public void ApplyToMaterialSDF(Material material) => ApplyToMaterialSDF(material, "_BodySDFTex", "_SDFBoundsMin", "_SDFBoundsMax", "_Deltas", "_DeltasIndexOffset");
            public void ApplyToMaterialSDF(Material material, string textureProperty, string boundsMinProperty, string boundsMaxProperty, string shapeDeltasProperty, string shapeDeltasIndexOffsetProperty)
            {
                material.SetTexture(textureProperty, sdf3DTexture);
                if (sdf3DTextureSimplified != null) material.SetTexture(textureProperty + "Simplified", sdf3DTextureSimplified);
                //material.SetVector(boundsMinProperty, boundsMin);
                //material.SetVector(boundsMaxProperty, boundsMax);
                 
                if (meshShapeIndex >= 0 && manager.characterMesh != null)
                {
                    material.SetBuffer(shapeDeltasProperty, manager.characterMesh.SubData.MeshShapeFrameDeltasBuffer);
                    material.SetInt(shapeDeltasIndexOffsetProperty, ((meshShapeFrame + meshShapeIndex) * manager.characterMesh.SubData.VertexCount));
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(shapeDeltasProperty)) material.SetBuffer(shapeDeltasProperty, PersistentJobDataTracker.GetEmptyGraphicsBuffer());
                    if (!string.IsNullOrWhiteSpace(shapeDeltasIndexOffsetProperty)) material.SetInt(shapeDeltasIndexOffsetProperty, -1);
                }
            }

            public void ApplyToShaderSDF(ComputeShader shader, int kernelIndex) => ApplyToShaderSDF(shader, kernelIndex, "_BodySDFTex", "_SDFBoundsMin", "_SDFBoundsMax", "_SDFResolution", "_Deltas", "_DeltasIndexOffset");
            public void ApplyToShaderSDF(ComputeShader shader, int kernelIndex, string textureProperty, string boundsMinProperty, string boundsMaxProperty, string resolutionProperty, string shapeDeltasProperty, string shapeDeltasIndexOffsetProperty)
            {
                shader.SetTexture(kernelIndex, textureProperty, sdf3DTexture);
                if (sdf3DTextureSimplified != null) shader.SetTexture(kernelIndex, textureProperty + "Simplified", sdf3DTextureSimplified);
                //shader.SetVector(boundsMinProperty, boundsMin);
                //shader.SetVector(boundsMaxProperty, boundsMax);
                shader.SetInt(resolutionProperty, manager.textureResolution);

                if (meshShapeIndex >= 0 && manager.characterMesh != null)
                {
                    shader.SetBuffer(kernelIndex, shapeDeltasProperty, manager.characterMesh.SubData.MeshShapeFrameDeltasBuffer);
                    shader.SetInt(shapeDeltasIndexOffsetProperty, ((meshShapeFrame + meshShapeIndex) * manager.characterMesh.SubData.VertexCount));
                } 
                else
                {
                    if (!string.IsNullOrWhiteSpace(shapeDeltasProperty)) shader.SetBuffer(kernelIndex, shapeDeltasProperty, PersistentJobDataTracker.GetEmptyGraphicsBuffer()); 
                    if (!string.IsNullOrWhiteSpace(shapeDeltasIndexOffsetProperty)) shader.SetInt(shapeDeltasIndexOffsetProperty, -1);
                }
            }

            public void ApplyToCommandBufferSDF(CommandBuffer cmd, ComputeShader shader, int kernelIndex) => ApplyToCommandBufferSDF(cmd, shader, kernelIndex, "_BodySDFTex", "_SDFBoundsMin", "_SDFBoundsMax", "_SDFResolution", "_Deltas", "_DeltasIndexOffset");
            public void ApplyToCommandBufferSDF(CommandBuffer cmd, ComputeShader shader, int kernelIndex, string textureProperty, string boundsMinProperty, string boundsMaxProperty, string resolutionProperty, string shapeDeltasProperty, string shapeDeltasIndexOffsetProperty)
            {
                cmd.SetComputeTextureParam(shader, kernelIndex, textureProperty, sdf3DTexture);
                if (sdf3DTextureSimplified != null) cmd.SetComputeTextureParam(shader, kernelIndex, textureProperty + "Simplified", sdf3DTextureSimplified);
                //cmd.SetComputeVectorParam(shader, boundsMinProperty, boundsMin);
                //cmd.SetComputeVectorParam(shader, boundsMaxProperty, boundsMax);
                cmd.SetComputeIntParam(shader, resolutionProperty, manager.textureResolution);

                if (meshShapeIndex >= 0 && manager.characterMesh != null)
                {
                    cmd.SetComputeBufferParam(shader, kernelIndex, shapeDeltasProperty, manager.characterMesh.SubData.MeshShapeFrameDeltasBuffer);
                    cmd.SetComputeIntParam(shader, shapeDeltasIndexOffsetProperty, ((meshShapeFrame + meshShapeIndex) * manager.characterMesh.SubData.VertexCount));
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(shapeDeltasProperty)) cmd.SetComputeBufferParam(shader, kernelIndex, shapeDeltasProperty, PersistentJobDataTracker.GetEmptyGraphicsBuffer());
                    if (!string.IsNullOrWhiteSpace(shapeDeltasIndexOffsetProperty)) cmd.SetComputeIntParam(shader, shapeDeltasIndexOffsetProperty, -1);
                }
            }

            public void BakeAOCV()
            {
                var characterMesh = manager.characterMesh;
                var aocvShader = manager.aocvComputeShader;
                if (aocvShader == null) return;

                var textureResolution = manager.textureResolution;
                InitializeAocvTextures(textureResolution);

                using (var buffers = new ComputeBufferLeaseScope())
                {
                    CommandBuffer cmd = CommandBufferPool.Get("BakeAOCV"); 

                    if (!GetBoundsMeshAndMeshBuffers(cmd, buffers,
                        out var deformedVerticesBuffer, out var partialBoundsBuffer, out var boundsMinBuffer, out var boundsMaxBuffer, out var maxDistanceBuffer,
                        manager.boundsPaddingScale, out bool cleanup, out Mesh mesh, out ComputeBuffer vertexBuffer, out ComputeBuffer triangleBuffer, out ComputeBuffer shapeFrameIndicesBuffer, out ComputeBuffer shapeDeltasBuffer/*, out Bounds localBounds, out boundsMin, out boundsMax*/)) return;

                    // 1. Find the core kernel entry point
                    var kernelHandle = aocvShader.FindKernel(characterMesh == null ? "GenerateAOCV" : "GenerateAOCV_WithDeltas");

                    // 2. Pass global volume settings
                    cmd.SetComputeIntParam(aocvShader, "_Resolution", textureResolution);
                    cmd.SetComputeIntParam(aocvShader, "_TriangleCount", triangleBuffer.count);
                    cmd.SetComputeFloatParam(aocvShader, "_MaxNegativeDistance", manager.maxNegativeDistanceAOCV);
                    //cmd.SetComputeVectorParam(aocvShader, "_BoundsMin", boundsMin);
                    //cmd.SetComputeVectorParam(aocvShader, "_BoundsMax", boundsMax);
                    cmd.SetComputeBufferParam(aocvShader, kernelHandle, "_BoundsMin", boundsMinBuffer);
                    cmd.SetComputeBufferParam(aocvShader, kernelHandle, "_BoundsMax", boundsMaxBuffer);
                    cmd.SetComputeBufferParam(aocvShader, kernelHandle, "_MaxDistance", maxDistanceBuffer);

                    // 3. Bind your source mesh topology data streams
                    cmd.SetComputeBufferParam(aocvShader, kernelHandle, "_Vertices", vertexBuffer);
                    cmd.SetComputeBufferParam(aocvShader, kernelHandle, "_Triangles", triangleBuffer);
                    if (characterMesh != null && characterMesh.TryGetInstanceBuffer<MeshVertexDelta>(characterMesh.SubData.PerVertexDeltaDataPropertyName, out var deltaInstanceBuffer)) deltaInstanceBuffer.BindShaderProperty(aocvShader, kernelHandle, "_Deltas", false);

                    if (shapeDeltasBuffer != null)
                    {
                        cmd.SetComputeBufferParam(aocvShader, kernelHandle, "_ShapeDeltas", shapeDeltasBuffer);
                        cmd.SetComputeIntParam(aocvShader, "_ShapeDeltasIndexOffset", ((meshShapeFrame + meshShapeIndex) * manager.characterMesh.SubData.VertexCount));
                    }
                    else
                    {
                        cmd.SetComputeBufferParam(aocvShader, kernelHandle, "_ShapeDeltas", PersistentJobDataTracker.GetEmptyGraphicsBuffer());
                        cmd.SetComputeIntParam(aocvShader, "_ShapeDeltasIndexOffset", -1);
                    }

                    // 4. Bind the 3D Write Targets
                    cmd.SetComputeTextureParam(aocvShader, kernelHandle, "_ResultCore", aocvCoreTexture);
                    cmd.SetComputeTextureParam(aocvShader, kernelHandle, "_ResultSurfaceNormal", aocvSurfaceNormalTexture);

                    // 5. Calculate thread group counts matching your [numthreads(8,8,8)] layout
                    // We use Mathf.CeilToInt to handle resolutions that aren't perfectly divisible by 8
                    int threadGroupsX = Mathf.CeilToInt(textureResolution / 8f);
                    int threadGroupsY = Mathf.CeilToInt(textureResolution / 8f);
                    int threadGroupsZ = Mathf.CeilToInt(textureResolution / 8f);

                    // 6. Execute the GPU pass
                    cmd.DispatchCompute(aocvShader, kernelHandle, threadGroupsX, threadGroupsY, threadGroupsZ);

                    Graphics.ExecuteCommandBuffer(cmd);
                    CommandBufferPool.Release(cmd);

                }
            }

            public void ApplyToMaterialAOCV(Material material) => ApplyToMaterialAOCV(material, "_BodyCoreTexture", "_BodySurfaceNormalTexture", "_SDFBoundsMin", "_SDFBoundsMax", "_Deltas", "_DeltasIndexOffset");
            public void ApplyToMaterialAOCV(Material material, string coreTextureProperty, string surfaceNormalTextureProperty, string boundsMinProperty, string boundsMaxProperty, string shapeDeltasProperty, string shapeDeltasIndexOffsetProperty)
            {
                material.SetTexture(coreTextureProperty, aocvCoreTexture);
                material.SetTexture(surfaceNormalTextureProperty, aocvSurfaceNormalTexture);
                //material.SetVector(boundsMinProperty, boundsMin);
                //material.SetVector(boundsMaxProperty, boundsMax);

                if (meshShapeIndex >= 0 && manager.characterMesh != null)
                {
                    material.SetBuffer(shapeDeltasProperty, manager.characterMesh.SubData.MeshShapeFrameDeltasBuffer);
                    material.SetInt(shapeDeltasIndexOffsetProperty, ((meshShapeFrame + meshShapeIndex) * manager.characterMesh.SubData.VertexCount)); 
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(shapeDeltasProperty)) material.SetBuffer(shapeDeltasProperty, PersistentJobDataTracker.GetEmptyGraphicsBuffer());
                    if (!string.IsNullOrWhiteSpace(shapeDeltasIndexOffsetProperty)) material.SetInt(shapeDeltasIndexOffsetProperty, -1);
                }
            }

            public void ApplyToShaderAOCV(ComputeShader shader, int kernelIndex) => ApplyToShaderAOCV(shader, kernelIndex, "_BodyCoreTexture", "_BodySurfaceNormalTexture", "_SDFBoundsMin", "_SDFBoundsMax", "_SDFResolution", "_Deltas", "_DeltasIndexOffset");
            public void ApplyToShaderAOCV(ComputeShader shader, int kernelIndex, string coreTextureProperty, string surfaceNormalTextureProperty, string boundsMinProperty, string boundsMaxProperty, string resolutionProperty, string shapeDeltasProperty, string shapeDeltasIndexOffsetProperty)
            {
                shader.SetTexture(kernelIndex, coreTextureProperty, aocvCoreTexture);
                shader.SetTexture(kernelIndex, surfaceNormalTextureProperty, aocvSurfaceNormalTexture);
                //shader.SetVector(boundsMinProperty, boundsMin);
                //shader.SetVector(boundsMaxProperty, boundsMax);
                shader.SetInt(resolutionProperty, manager.textureResolution);

                if (meshShapeIndex >= 0 && manager.characterMesh != null)
                {
                    shader.SetBuffer(kernelIndex, shapeDeltasProperty, manager.characterMesh.SubData.MeshShapeFrameDeltasBuffer);
                    shader.SetInt(shapeDeltasIndexOffsetProperty, ((meshShapeFrame + meshShapeIndex) * manager.characterMesh.SubData.VertexCount));
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(shapeDeltasProperty)) shader.SetBuffer(kernelIndex, shapeDeltasProperty, PersistentJobDataTracker.GetEmptyGraphicsBuffer());
                    if (!string.IsNullOrWhiteSpace(shapeDeltasIndexOffsetProperty)) shader.SetInt(shapeDeltasIndexOffsetProperty, -1);
                }
            }

            public void ApplyToCommandBufferAOCV(CommandBuffer cmd, ComputeShader shader, int kernelIndex) => ApplyToCommandBufferAOCV(cmd, shader, kernelIndex, "_BodyCoreTexture", "_BodySurfaceNormalTexture", "_SDFBoundsMin", "_SDFBoundsMax", "_SDFResolution", "_Deltas", "_DeltasIndexOffset");
            public void ApplyToCommandBufferAOCV(CommandBuffer cmd, ComputeShader shader, int kernelIndex, string coreTextureProperty, string surfaceNormalTextureProperty, string boundsMinProperty, string boundsMaxProperty, string resolutionProperty, string shapeDeltasProperty, string shapeDeltasIndexOffsetProperty)
            {
                cmd.SetComputeTextureParam(shader, kernelIndex, coreTextureProperty, aocvCoreTexture);
                cmd.SetComputeTextureParam(shader, kernelIndex, surfaceNormalTextureProperty, aocvSurfaceNormalTexture); 
                //cmd.SetComputeVectorParam(shader, boundsMinProperty, boundsMin);
                //cmd.SetComputeVectorParam(shader, boundsMaxProperty, boundsMax);
                cmd.SetComputeIntParam(shader, resolutionProperty, manager.textureResolution);

                if (meshShapeIndex >= 0 && manager.characterMesh != null)
                {
                    cmd.SetComputeBufferParam(shader, kernelIndex, shapeDeltasProperty, manager.characterMesh.SubData.MeshShapeFrameDeltasBuffer);
                    cmd.SetComputeIntParam(shader, shapeDeltasIndexOffsetProperty, ((meshShapeFrame + meshShapeIndex) * manager.characterMesh.SubData.VertexCount));
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(shapeDeltasProperty)) cmd.SetComputeBufferParam(shader, kernelIndex, shapeDeltasProperty, PersistentJobDataTracker.GetEmptyGraphicsBuffer());
                    if (!string.IsNullOrWhiteSpace(shapeDeltasIndexOffsetProperty)) cmd.SetComputeIntParam(shader, shapeDeltasIndexOffsetProperty, -1);
                }
            }

        }

        [SerializeField]
        protected OutputCollision[] outputs; 
        public OutputCollision GetOutput(string id)
        {
            foreach (var output in outputs)
            {
                if (output.ID == id) return output;
            }

            return null;
        }
        public OutputCollision GetOutput(int index)
        {
            if (index < 0 || index >= outputs.Length) return null;
            return outputs[index];
        }
        public OutputCollision Output => GetOutput(0);
        public int OutputCount => outputs?.Length ?? 0;
        public IEnumerable<OutputCollision> Outputs()
        {
            if (outputs != null)
            {
                foreach (var output in outputs) yield return output;
            }
        }

        public bool HasOutputId(string id)
        {
            foreach (var output in outputs)
            {
                if (output.ID == id) return true;
            }

            return false;
        }

        protected virtual void Awake()
        {
            characterMesh = gameObject.GetComponent<CustomizableCharacterMeshV2>();
            meshFilter = gameObject.GetComponent<MeshFilter>(); 

            if (outputs != null)
            {
                foreach (var output in outputs) output.manager = this;
            }
        }

        [Serializable]
        public enum AutoBakeMode
        {
            None,
            Default,
            Fast,
            FastSpatial,
            AOCV
        }

        public AutoBakeMode autoBakeMode = AutoBakeMode.FastSpatial;

        private UnityEngine.Events.UnityAction baseMeshListener; 
        private void AutoBake()
        {
            switch (autoBakeMode)
            {
                case AutoBakeMode.Default:
                    BakeSDF();
                    break;

                case AutoBakeMode.Fast:
                    FastBakeSDF();
                    break;

                case AutoBakeMode.FastSpatial:
                    FastSpatialBakeSDF();
                    break;

                case AutoBakeMode.AOCV:
                    BakeAOCV();
                    break;
            }
        }
        private void AutoBakeAfterBufferUploads()
        {
            ComputeBufferPoolUploader.QueuePostUploadAction(AutoBake);   
        }
        protected virtual void OnEnable()
        {
            if (characterMesh != null) 
            {
                baseMeshListener = AutoBakeAfterBufferUploads;

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
            GraphicsBuffer deformedVerticesBuffer,
            GraphicsBuffer partialBoundsBuffer,
            GraphicsBuffer boundsMinBuffer,
            GraphicsBuffer boundsMaxBuffer,
            GraphicsBuffer maxDistanceBuffer,
            ComputeShader computeShader,
            ComputeBuffer baseVerticesBuffer,
            ComputeBuffer deltasBuffer,
            int deltasIndexOffset,
            ComputeBuffer shapeDeltasBuffer,
            int shapeDeltasIndexOffset,
            int vertexCount,
            float padding = 1.15f)
        {
            CalculateDeformedBoundsGPU_NoReadback(null, deformedVerticesBuffer, partialBoundsBuffer, boundsMinBuffer, boundsMaxBuffer, maxDistanceBuffer, computeShader, baseVerticesBuffer, deltasBuffer, deltasIndexOffset, shapeDeltasBuffer, shapeDeltasIndexOffset, vertexCount, padding);

            boundsMinBuffer.GetData(_minResult);
            boundsMaxBuffer.GetData(_maxResult);

            Vector3 center = (_minResult[0] + _maxResult[0]) * 0.5f;
            Vector3 size = _maxResult[0] - _minResult[0];

            return new Bounds(center, size);  
        }
        protected void CalculateDeformedBoundsGPU_NoReadback(
            CommandBuffer cmd,
            GraphicsBuffer deformedVerticesBuffer,
            GraphicsBuffer partialBoundsBuffer,
            GraphicsBuffer boundsMinBuffer,
            GraphicsBuffer boundsMaxBuffer,
            GraphicsBuffer maxDistanceBuffer,
            ComputeShader computeShader,
            ComputeBuffer baseVerticesBuffer,
            ComputeBuffer deltasBuffer,
            int deltasIndexOffset,
            ComputeBuffer shapeDeltasBuffer,
            int shapeDeltasIndexOffset,
            int vertexCount,
            float padding = 1.15f)
        {
            //InitializeGPUBuffers(vertexCount);

            bool isLocalCommandBuffer = cmd == null;
            if (isLocalCommandBuffer) cmd = CommandBufferPool.Get("CalculateDeformedBounds");

            int kernelDeform = shapeDeltasBuffer != null
                ? computeShader.FindKernel("DeformVerticesWithShape")
                : computeShader.FindKernel("DeformVertices");
            int kernelBounds = computeShader.FindKernel("CalculateBounds");
            int kernelBoundsReduce = computeShader.FindKernel("CalculateBounds_Reduce");

            cmd.SetComputeBufferParam(computeShader, kernelDeform, "_BaseVertices", baseVerticesBuffer);
            cmd.SetComputeBufferParam(computeShader, kernelDeform, "_Deltas", deltasBuffer);
            cmd.SetComputeBufferParam(computeShader, kernelDeform, "_DeformedVerticesOutput", deformedVerticesBuffer);
            cmd.SetComputeIntParam(computeShader, "_DeltasIndexOffset", deltasIndexOffset);
            cmd.SetComputeIntParam(computeShader, "_VertexCount", vertexCount);

            if (shapeDeltasBuffer != null)
            {
                cmd.SetComputeBufferParam(computeShader, kernelDeform, "_ShapeDeltas", shapeDeltasBuffer);
                cmd.SetComputeIntParam(computeShader, "_ShapeDeltasIndexOffset", shapeDeltasIndexOffset);
            }
            else
            {
                cmd.SetComputeBufferParam(computeShader, kernelDeform, "_ShapeDeltas", PersistentJobDataTracker.GetEmptyGraphicsBuffer()); 
                cmd.SetComputeIntParam(computeShader, "_ShapeDeltasIndexOffset", -1);
            }

            cmd.DispatchCompute(computeShader, kernelDeform, (vertexCount + 255) / 256, 1, 1);

            int numGroups = (vertexCount + 255) / 256;
            cmd.SetComputeBufferParam(computeShader, kernelBounds, "DeformedVertices", deformedVerticesBuffer);
            cmd.SetComputeBufferParam(computeShader, kernelBounds, "PartialBounds", partialBoundsBuffer);
            cmd.SetComputeIntParam(computeShader, "_DeformedVertexCount", vertexCount);
            cmd.DispatchCompute(computeShader, kernelBounds, numGroups, 1, 1);

            cmd.SetComputeBufferParam(computeShader, kernelBoundsReduce, "PartialBounds", partialBoundsBuffer);
            cmd.SetComputeBufferParam(computeShader, kernelBoundsReduce, "OutputBoundsMin", boundsMinBuffer);
            cmd.SetComputeBufferParam(computeShader, kernelBoundsReduce, "OutputBoundsMax", boundsMaxBuffer);
            cmd.SetComputeBufferParam(computeShader, kernelBoundsReduce, "OutputBoundsMaxDistance", maxDistanceBuffer); 
            cmd.SetComputeFloatParam(computeShader, "_PaddingScale", padding);
            cmd.SetComputeIntParam(computeShader, "_DeformedVertexCount", vertexCount);
            cmd.DispatchCompute(computeShader, kernelBoundsReduce, 1, 1, 1);

            if (isLocalCommandBuffer)
            {
                Graphics.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
        }

        protected virtual void OnDestroy()
        {
            if (outputs != null)
            {
                foreach (var sdf in outputs) sdf.Release(); 
            }

            //ReleaseGPUBuffers();
        }

        public void ApplyToMaterial(string outputId, Material material)
        {
            var output = outputId == null ? Output : GetOutput(outputId);
            if (output == null) output = Output;
            if (output != null)
            {
                switch (autoBakeMode)
                {
                    case AutoBakeMode.AOCV:
                        output.ApplyToMaterialAOCV(material);
                        break;
                    default:
                        output.ApplyToMaterialSDF(material);
                        break;
                }
            }
        }
        public void ApplyToMaterialSDF(string outputId, Material material, string textureProperty, string boundsMinProperty, string boundsMaxProperty, string shapeDeltasProperty, string shapeDeltasIndexOffsetProperty)
        {
            var output = outputId == null ? Output : GetOutput(outputId);
            if (output == null) output = Output;
            if (output != null) output.ApplyToMaterialSDF(material, textureProperty, boundsMinProperty, boundsMaxProperty, shapeDeltasProperty, shapeDeltasIndexOffsetProperty);
        }

        public void ApplyToShader(string outputId, ComputeShader shader, int kernelIndex)
        {
            var output = outputId == null ? Output : GetOutput(outputId);
            if (output == null) output = Output;
            if (output != null)
            {
                switch (autoBakeMode)
                {
                    case AutoBakeMode.AOCV:
                        output.ApplyToShaderAOCV(shader, kernelIndex); 
                        break;
                    default:
                        output.ApplyToShaderSDF(shader, kernelIndex);
                        break;
                }
            }
        }
        public void ApplyToShaderSDF(string outputId, ComputeShader shader, int kernelIndex, string textureProperty, string boundsMinProperty, string boundsMaxProperty, string resolutionProperty, string shapeDeltasProperty, string shapeDeltasIndexOffsetProperty)
        {
            var output = outputId == null ? Output : GetOutput(outputId);
            if (output == null) output = Output;
            if (output != null) output.ApplyToShaderSDF(shader, kernelIndex, textureProperty, boundsMinProperty, boundsMaxProperty, resolutionProperty, shapeDeltasProperty, shapeDeltasIndexOffsetProperty);
        }

        public void BakeSDF()
        {
            if (outputs != null) 
            {
                foreach (var output in outputs) output.BakeSDF(); 
            }
        }
        public void BakeSDF(string outputId)
        {
            var output = GetOutput(outputId);
            if (output != null) output.BakeSDF();
        }

        public void FastBakeSDF()
        {
            if (outputs != null)
            {
                foreach (var output in outputs) output.FastBakeSDF();
            }
        }
        public void FastBakeSDF(string outputId)
        {
            var output = GetOutput(outputId);
            if (output != null) output.FastBakeSDF();
        }

        public void FastSpatialBakeSDF()
        {
            if (outputs != null)
            {
                foreach (var output in outputs) output.FastSpatialBakeSDF();
            }
        }
        public void FastSpatialBakeSDF(string outputId)
        {
            var output = GetOutput(outputId);
            if (output != null) output.FastSpatialBakeSDF();
        }

        public void BakeAOCV()
        {
            if (outputs != null)
            {
                foreach (var output in outputs) output.BakeAOCV(); 
            }
        }
        public void BakeAOCV(string outputId)
        {
            var output = GetOutput(outputId);
            if (output != null) output.BakeAOCV();
        }

    }
}
