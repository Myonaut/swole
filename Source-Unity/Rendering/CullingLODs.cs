#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;

using Swole.API.Unity;

namespace Swole
{
    public class CullingLODs : SingletonBehaviour<CullingLODs>, IDisposable
    {

        public static int ExecutionPriority => Rigs.ExecutionPriority + 1;
        public override int Priority => ExecutionPriority;

        public class PerCameraCullLOD : IDisposable
        {
            private Camera camera;
            public Camera Camera => camera;

            private Transform cameraTransform;
            public Transform CameraTransform => cameraTransform;

            private readonly List<RendererCullLOD> renderers = new List<RendererCullLOD>();
            public int RendererCount => renderers.Count;
            public RendererCullLOD GetRenderer(int index) => renderers[index];

            public RendererCullLOD AddRenderer(Transform boundsRootTransform, float3 boundsCenter, float3 boundsExtents, ICollection<LOD> lods)
            {
                var renderer = new RendererCullLOD(this, boundsRootTransform, renderers.Count);
                renderers.Add(renderer);

                centers.Add(boundsCenter);
                extents.Add(boundsExtents);

                if (lods != null) foreach(var lod in lods) this.lods.Add(lod);
                lodCounts.Add(lods == null ? 0 : lods.Count);

                transformIndices.Add(renderer.TransformIndex);

                outputCullLOD.Add(CullLOD.Null);
                prevOutputCullLOD.Add(CullLOD.Null);

                return renderer;
            }

            public void Remove(RendererCullLOD renderer)
            {
                Remove(renderers.IndexOf(renderer));
            }
            public void Remove(int rendererIndex)
            {
                if (rendererIndex < 0 || rendererIndex >= renderers.Count) return;

                var renderer = renderers[rendererIndex];
                var swapRenderer = renderers[renderers.Count - 1];
                renderers[rendererIndex] = swapRenderer;
                renderers.RemoveAt(renderers.Count - 1);
                swapRenderer.index = rendererIndex;
                renderer.index = -1; 

                centers.RemoveAtSwapBack(rendererIndex);
                extents.RemoveAtSwapBack(rendererIndex);
                lods.RemoveAtSwapBack(rendererIndex);
                lodCounts.RemoveAtSwapBack(rendererIndex);
                transformIndices.RemoveAtSwapBack(rendererIndex);
                outputCullLOD.RemoveAtSwapBack(rendererIndex);
                prevOutputCullLOD.RemoveAtSwapBack(rendererIndex);

                renderer.Dispose();
            }

            private NativeList<float3> centers; 
            public NativeList<float3> Centers => centers;

            private NativeList<float3> extents;
            public NativeList<float3> Extents => extents;

            private NativeList<LOD> lods;
            public NativeList<LOD> Lods => lods;

            private NativeList<int> lodCounts;
            public NativeList<int> LodCounts => lodCounts;

            private NativeList<int> transformIndices;
            public NativeList<int> TransformIndices => transformIndices;

            private static readonly Plane[] frustrumPlanes = new Plane[6]; 
            public float4 frustrumPlane0;
            public float4 frustrumPlane1;
            public float4 frustrumPlane2;
            public float4 frustrumPlane3;
            public float4 frustrumPlane4;
            public float4 frustrumPlane5;

            public void RecalculateFrustrumPlanes()
            { 
                GeometryUtility.CalculateFrustumPlanes(camera, frustrumPlanes); 

                frustrumPlane0 = new float4(frustrumPlanes[0].normal.x, frustrumPlanes[0].normal.y, frustrumPlanes[0].normal.z, frustrumPlanes[0].distance);
                frustrumPlane1 = new float4(frustrumPlanes[1].normal.x, frustrumPlanes[1].normal.y, frustrumPlanes[1].normal.z, frustrumPlanes[1].distance);
                frustrumPlane2 = new float4(frustrumPlanes[2].normal.x, frustrumPlanes[2].normal.y, frustrumPlanes[2].normal.z, frustrumPlanes[2].distance);
                frustrumPlane3 = new float4(frustrumPlanes[3].normal.x, frustrumPlanes[3].normal.y, frustrumPlanes[3].normal.z, frustrumPlanes[3].distance);
                frustrumPlane4 = new float4(frustrumPlanes[4].normal.x, frustrumPlanes[4].normal.y, frustrumPlanes[4].normal.z, frustrumPlanes[4].distance);
                frustrumPlane5 = new float4(frustrumPlanes[5].normal.x, frustrumPlanes[5].normal.y, frustrumPlanes[5].normal.z, frustrumPlanes[5].distance);
            }

            private NativeList<CullLOD> outputCullLOD;
            public NativeList<CullLOD> OutputCullLOD => outputCullLOD;

            private NativeList<CullLOD> prevOutputCullLOD;
            public NativeList<CullLOD> PrevOutputCullLOD => prevOutputCullLOD;

            public PerCameraCullLOD(Camera camera)
            {
                this.camera = camera;
                this.cameraTransform = camera.transform;

                centers = new NativeList<float3>(0, Allocator.Persistent);
                extents = new NativeList<float3>(0, Allocator.Persistent);
                lods = new NativeList<LOD>(0, Allocator.Persistent);
                lodCounts = new NativeList<int>(0, Allocator.Persistent);
                transformIndices = new NativeList<int>(0, Allocator.Persistent);
                outputCullLOD = new NativeList<CullLOD>(0, Allocator.Persistent);
                prevOutputCullLOD = new NativeList<CullLOD>(0, Allocator.Persistent);

                updatedIndices = new NativeList<int>(0, Allocator.Persistent);  
            }

            private NativeList<int> updatedIndices;
            private JobHandle updateJobHandle;
            public JobHandle OutputDependency => updateJobHandle;

            public void UpdateCullingLODs()
            {
                RecalculateFrustrumPlanes(); 
                 
                var cullingLODJob = new CullingLODJob()
                {
                    cameraWorldPosition = cameraTransform.position,
                    frustrumPlane0 = frustrumPlane0,
                    frustrumPlane1 = frustrumPlane1,
                    frustrumPlane2 = frustrumPlane2,
                    frustrumPlane3 = frustrumPlane3,
                    frustrumPlane4 = frustrumPlane4,
                    frustrumPlane5 = frustrumPlane5,

                    centers = centers,
                    extents = extents,
                    lods = lods,
                    lodCounts = lodCounts,
                    transformIndices = transformIndices,
                    localToWorlds = TransformTracking.TrackedTransformsToWorld,

                    output = outputCullLOD,
                    prevOutput = prevOutputCullLOD
                };
                   
                updateJobHandle = cullingLODJob.ScheduleAppend(updatedIndices, renderers.Count, 8, TransformTracking.JobDependency);
                TransformTracking.AddInputDependency(updateJobHandle); 
            }

            public void RefreshRenderers()
            {
                foreach (var index in updatedIndices) renderers[index].Refresh();
            }

            public void Dispose()
            {
                if (centers.IsCreated)
                {
                    centers.Dispose();
                    centers = default;
                }
                if (extents.IsCreated)
                {
                    extents.Dispose();
                    extents = default;
                }
                if (lods.IsCreated)
                {
                    lods.Dispose();
                    lods = default;
                }
                if (lodCounts.IsCreated)
                {
                    lodCounts.Dispose();
                    lodCounts = default;
                }

                if (transformIndices.IsCreated)
                {
                    transformIndices.Dispose();
                    transformIndices = default;
                }

                if (outputCullLOD.IsCreated)
                {
                    outputCullLOD.Dispose();
                    outputCullLOD = default;
                }
                if (prevOutputCullLOD.IsCreated)
                {
                    prevOutputCullLOD.Dispose();
                    prevOutputCullLOD = default;
                }

                if (updatedIndices.IsCreated)
                {
                    updatedIndices.Dispose();
                    updatedIndices = default;
                }
            }
        }

        public delegate void OnLODChangeDelegate(int oldLOD, int newLOD);
        public delegate void OnVisibilityChangeDelegate(bool isVisible); 

        public class RendererCullLOD : IDisposable
        {
            public int index;

            private TransformTracking.TrackedTransform transformTracker;
            public int TransformIndex => transformTracker.Index;
            private PerCameraCullLOD owner;

            public RendererCullLOD(PerCameraCullLOD owner, Transform boundsRootTransform, int index)
            {
                this.owner = owner;
                this.index = index;
                transformTracker = TransformTracking.Track(boundsRootTransform); 
                transformTracker.AddUser(OnTransformIndexChange);
            }

            private void OnTransformIndexChange(int oldIndex, int newIndex)
            {
                var indices = owner.TransformIndices;
                indices[index] = newIndex;
            }

            public event OnLODChangeDelegate OnLODChange;
            public event OnVisibilityChangeDelegate OnVisibilityChange;

            public void RemoveAllListeners()
            {
                OnLODChange = null;
                OnVisibilityChange = null;
            }

            private int prevLodLevel = int.MinValue;
            private bool prevCulled = true;

            public void Refresh()
            {
                var cullLod = owner.OutputCullLOD[index]; 

                if (prevLodLevel != cullLod.lodLevel)
                {
                    OnLODChange?.Invoke(prevLodLevel, cullLod.lodLevel);
                    prevLodLevel = cullLod.lodLevel;
                }

                if (prevCulled != cullLod.culled)
                {
                    OnVisibilityChange?.Invoke(!cullLod.culled); 
                    prevCulled = cullLod.culled;
                }
            }

            public void Dispose()
            {
                RemoveAllListeners();

                if (owner != null) owner.Remove(index);

                owner = null;
                index = -1;

                if (transformTracker != null) 
                { 
                    transformTracker.RemoveUser(OnTransformIndexChange, true);
                    transformTracker = null;
                }
            }
        }

        [Serializable]
        public struct LOD
        {
            public float minDistance;
            public int detailLevel;
        }
        [Serializable]
        public struct CullLOD
        {
            public float distanceToCamera;
            public int lodLevel;
            public bool culled;

            public static CullLOD Null => new CullLOD() { culled = true, lodLevel = -1 };
        }

        private readonly Dictionary<Camera, PerCameraCullLOD> perCameraCullLODs = new Dictionary<Camera, PerCameraCullLOD>();

        public PerCameraCullLOD GetCameraCullLODLocal(Camera camera)
        {
            if (!perCameraCullLODs.TryGetValue(camera, out var pccl))
            {
                pccl = new PerCameraCullLOD(camera);
                perCameraCullLODs[camera] = pccl;
            }

            return pccl;
        }
        public static PerCameraCullLOD GetCameraCullLOD(Camera camera)
        {
            var instance = Instance;
            if (instance == null) return null;

            return instance.GetCameraCullLODLocal(camera);
        }

        public override void OnFixedUpdate()
        {
        }


        public override void OnUpdate()
        {
            foreach (var pair in perCameraCullLODs) pair.Value.UpdateCullingLODs(); 
        }
        public override void OnLateUpdate()
        {
            foreach (var pair in perCameraCullLODs) 
            { 
                pair.Value.OutputDependency.Complete();
                pair.Value.RefreshRenderers();
            }
        }

        private void OnDestroy()
        {
            Dispose();
        }

        public void Dispose()
        {
            foreach(var pccl in perCameraCullLODs.Values) pccl.Dispose();  
            perCameraCullLODs.Clear();
        }

        protected JobHandle jobHandle;

        /// <summary>
        /// adapted from source: https://ennogames.com/blog/frustum-culling-with-unity-jobs
        /// </summary>
        [BurstCompile]
        struct CullingLODJob : IJobParallelForFilter
        {
            public float3 cameraWorldPosition;
            public float4 frustrumPlane0;
            public float4 frustrumPlane1;
            public float4 frustrumPlane2;
            public float4 frustrumPlane3;
            public float4 frustrumPlane4;
            public float4 frustrumPlane5;

            [ReadOnly] public NativeList<float3> centers;
            [ReadOnly] public NativeList<float3> extents;
            [ReadOnly] public NativeList<LOD> lods;
            [ReadOnly] public NativeList<int> lodCounts;

            [ReadOnly] public NativeList<int> transformIndices; 
            [ReadOnly] public NativeList<float4x4> localToWorlds;

            [NativeDisableParallelForRestriction]
            public NativeList<CullLOD> output;
            [NativeDisableParallelForRestriction]
            public NativeList<CullLOD> prevOutput;

            public bool Execute(int i)
            {
                int transformIndex = transformIndices[i];
                float4x4 l2w = localToWorlds[transformIndex];
                float3 pos = math.transform(l2w, centers[i]);
                float3 ext = extents[i]; 

                bool visible = FrustumContainsBox(pos - ext, pos + ext);
                float distanceToCamera = math.distance(cameraWorldPosition, pos);

                var clod = output[i];
                clod.distanceToCamera = distanceToCamera;
                clod.culled = !visible;

                float lodDist = float.MaxValue;
                int lodCount = lodCounts[i];
                for (int l = 0; l < lodCount; l++)
                {
                    var lod = lods[l];

                    bool inRange = lod.minDistance < lodDist && distanceToCamera >= lod.minDistance;
                    lodDist = math.select(lodDist, lod.minDistance, inRange);
                    clod.lodLevel = math.select(clod.lodLevel, lod.detailLevel, inRange);
                }

                output[i] = clod;

                CullLOD prevClod = prevOutput[i]; 
                prevOutput[i] = clod;

                return prevClod.lodLevel != clod.lodLevel || prevClod.culled != clod.culled;
            }

            float DistanceToPlane(float4 plane, float3 position)
            {
                return math.dot(plane.xyz, position) + plane.w;
            }

            bool FrustumContainsBox(float3 bboxMin, float3 bboxMax)
            {
                bool flag0 = FrustumPlaneContainsBox(frustrumPlane0, bboxMin, bboxMax);
                bool flag1 = FrustumPlaneContainsBox(frustrumPlane1, bboxMin, bboxMax);
                bool flag2 = FrustumPlaneContainsBox(frustrumPlane2, bboxMin, bboxMax);
                bool flag3 = FrustumPlaneContainsBox(frustrumPlane3, bboxMin, bboxMax);
                bool flag4 = FrustumPlaneContainsBox(frustrumPlane4, bboxMin, bboxMax);
                bool flag5 = FrustumPlaneContainsBox(frustrumPlane5, bboxMin, bboxMax);

                return flag0 && flag1 && flag2 && flag3 && flag4 && flag5;
            }
            bool FrustumPlaneContainsBox(float4 plane, float3 bboxMin, float3 bboxMax)
            {
                float3 pos = math.select(bboxMin, bboxMax, plane.xyz > 0);
                return DistanceToPlane(plane, pos) >= 0;
            }
        }

    }
}

#endif