#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;

using Swole.API.Unity.Animation;

namespace Swole
{
    public class CullingLODs : SingletonBehaviour<CullingLODs>, IDisposable
    {

        /* Useful links
         * 
         * - http://davidlively.com/programming/graphics/frustum-calculation-and-culling-hopefully-demystified/
         * 
         */

        public static int ExecutionPriority => Rigs.ExecutionPriority + 1;
        public override int Priority => ExecutionPriority;

        public static int SortLODsDescending(MeshLOD a, MeshLOD b) => Math.Sign(b.screenRelativeTransitionHeight - a.screenRelativeTransitionHeight);
        public static int SortLODsDescending(CullingLODs.LOD a, CullingLODs.LOD b) => Math.Sign(b.screenRelativeTransitionHeight - a.screenRelativeTransitionHeight);

        public class PerCameraCullLOD : IDisposable
        {
            private Camera camera;
            public Camera Camera => camera;

            private Transform cameraTransform;
            public Transform CameraTransform => cameraTransform;

            private readonly List<RendererCullLOD> renderers = new List<RendererCullLOD>();
            public int RendererCount => renderers.Count;
            public RendererCullLOD GetRenderer(int index) => renderers[index];

            public RendererCullLOD AddRenderer(Transform boundsRootTransform, float3 boundsCenter, float3 boundsExtents, ICollection<LOD> lods, float screenRelativeHeightBias = 1f)
            {
                var renderer = new RendererCullLOD(this, boundsRootTransform, renderers.Count);
                renderers.Add(renderer);

                EnsureJobCompletion();

                centers.Add(boundsCenter);
                extents.Add(new float4(boundsExtents, screenRelativeHeightBias <= 0f ? 1f : screenRelativeHeightBias));

                int lodIndex = this.lods.Length;
                int lodCount = 0;
                if (lods != null) 
                {
                    lodCount = lods.Count;
                    foreach (var lod in lods) this.lods.Add(lod);
                }
                lodStartIndicesCounts.Add(new int2(lodIndex, lodCount));

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
                renderer.Invalidate();

                if (IsValid)
                {
                    EnsureJobCompletion();

                    centers.RemoveAtSwapBack(rendererIndex);
                    extents.RemoveAtSwapBack(rendererIndex);
                    transformIndices.RemoveAtSwapBack(rendererIndex);
                    outputCullLOD.RemoveAtSwapBack(rendererIndex);
                    prevOutputCullLOD.RemoveAtSwapBack(rendererIndex);

                    int2 lodDataStartIndexCount = lodStartIndicesCounts[rendererIndex];
                    lodStartIndicesCounts.RemoveAtSwapBack(rendererIndex);
                    if (lodDataStartIndexCount.x >= 0 && lodDataStartIndexCount.y > 0) 
                    {
                        for (int a = 0; a < lodDataStartIndexCount.y; a++) lods.RemoveAt(lodDataStartIndexCount.x); // continually remove each entry at start index for given count
                        for (int a = 0; a < lodStartIndicesCounts.Length; a++) 
                        {
                            var element = lodStartIndicesCounts[a]; 
                            if (element.x >= lodDataStartIndexCount.x) element.x = element.x - lodDataStartIndexCount.y; // make sure any indices that came after are shifted down by the removed count
                            lodStartIndicesCounts[a] = element;
                        }
                    }
                }

                renderer.Dispose();
            }

            private NativeList<float3> centers; 
            //public NativeList<float3> Centers => centers;

            private NativeList<float4> extents;
            //public NativeList<float4> Extents => extents;

            public void SetExtents(int index, float3 extents_)
            {
                EnsureJobCompletion();
                if (index < 0 || !extents.IsCreated || index >= extents.Length) return;

                var val = extents[index];
                val.xyz = extents_;
                extents[index] = val;
            }
            public void SetScreenRelativeHeightBias(int index, float bias)
            {
                EnsureJobCompletion();
                if (index < 0 || !extents.IsCreated || index >= extents.Length) return;

                bias = bias <= 0f ? 1f : bias;

                var val = extents[index];
                val.w = bias;
                extents[index] = val;
            }
            public void SetExtentsAndBias(int index, float3 extents_, float screenRelativeHeightBias)
            {
                EnsureJobCompletion();
                if (index < 0 || !extents.IsCreated || index >= extents.Length) return;

                screenRelativeHeightBias = screenRelativeHeightBias <= 0f ? 1f : screenRelativeHeightBias; 

                var val = extents[index];
                val.xyz = extents_;
                val.w = screenRelativeHeightBias;
                extents[index] = val;
            }

            private NativeList<LOD> lods;
            //public NativeList<LOD> Lods => lods;

            private NativeList<int2> lodStartIndicesCounts;
            //public NativeList<int2> LodStartIndicesCounts => lodStartIndicesCounts;

            private NativeList<int> transformIndices;
            public NativeList<int> TransformIndices 
            { 
                get 
                {
                    EnsureJobCompletion();
                    return transformIndices; 
                } 
            }

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
                extents = new NativeList<float4>(0, Allocator.Persistent);
                lods = new NativeList<LOD>(0, Allocator.Persistent);
                lodStartIndicesCounts = new NativeList<int2>(0, Allocator.Persistent);
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
                if (!IsValid) return;

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

                    inverseCameraViewMatrix = camera.cameraToWorldMatrix,
                    cameraViewProjectionMatrix = camera.CalculateViewProjectionMatrix(),

                    centers = centers,
                    extents = extents,
                    lods = lods,
                    lodStartIndicesCounts = lodStartIndicesCounts,
                    transformIndices = transformIndices,
                    localToWorlds = TransformTracking.TrackedTransformsToWorld,

                    output = outputCullLOD,
                    prevOutput = prevOutputCullLOD
                };

#if UNITY_2022_3_OR_NEWER
                updateJobHandle = cullingLODJob.ScheduleAppend(updatedIndices, renderers.Count, TransformTracking.JobDependency); 
#else
                updateJobHandle = cullingLODJob.ScheduleAppend(updatedIndices, renderers.Count, 4, TransformTracking.JobDependency);
#endif
                TransformTracking.AddInputDependency(updateJobHandle); 
            }

            public void EnsureJobCompletion()
            {
                updateJobHandle.Complete(); 
            }

            public void RefreshRenderers()
            {
                if (!IsValid) return;

                EnsureJobCompletion();
                foreach (var index in updatedIndices) renderers[index].Refresh(); 
            }

            private bool isDisposed;
            public bool IsValid => !isDisposed; 
            public void Dispose()
            {
                isDisposed = true;

                EnsureJobCompletion();

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
                if (lodStartIndicesCounts.IsCreated)
                {
                    lodStartIndicesCounts.Dispose();
                    lodStartIndicesCounts = default;
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
            public void Invalidate()
            {
                index = -1;
                owner = null;
            }
            public bool IsValid => index >= 0 && owner != null;

            public RendererCullLOD(PerCameraCullLOD owner, Transform boundsRootTransform, int index)
            {
                this.owner = owner;
                this.index = index;
                transformTracker = TransformTracking.Track(boundsRootTransform); 
                transformTracker.AddUser(OnTransformIndexChange);
            }

            private void OnTransformIndexChange(int oldIndex, int newIndex)
            {
                if (!IsValid || !owner.IsValid) return;

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
                //if (!IsValid) return; 

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

            public void SetExtents(float3 extents_)
            {
                if (!IsValid) return;
                owner.SetExtents(index, extents_);
            }
            public void SetScreenRelativeHeightBias(float bias)
            {
                if (!IsValid) return;
                owner.SetScreenRelativeHeightBias(index, bias);
            }
            public void SetExtentsAndBias(float3 extents_, float screenRelativeHeightBias)
            {
                if (!IsValid) return;
                owner.SetExtentsAndBias(index, extents_, screenRelativeHeightBias);
            }
        }

        [Serializable]
        public struct LOD
        {
            public float screenRelativeTransitionHeight;
            public int detailLevel;
        }
        [Serializable]
        public struct CullLOD
        {
            //public float distanceToCamera;
            public float screenRelativeHeight;
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
                pair.Value.RefreshRenderers();
            }
        }
        public void EnsureJobCompletion()
        {
            foreach (var pair in perCameraCullLODs)
            {
                pair.Value.EnsureJobCompletion();
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
#if UNITY_2022_3_OR_NEWER
        struct CullingLODJob : IJobFilter
#else
        struct CullingLODJob : IJobParallelForFilter
#endif
        {
            public float3 cameraWorldPosition;
            public float4 frustrumPlane0;
            public float4 frustrumPlane1;
            public float4 frustrumPlane2;
            public float4 frustrumPlane3;
            public float4 frustrumPlane4;
            public float4 frustrumPlane5;

            public float4x4 inverseCameraViewMatrix;
            public float4x4 cameraViewProjectionMatrix;

            [ReadOnly] public NativeList<float3> centers;
            [ReadOnly] public NativeList<float4> extents;
            [ReadOnly] public NativeList<LOD> lods;
            [ReadOnly] public NativeList<int2> lodStartIndicesCounts;

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
                float4 ext = extents[i];

                float4 bboxMin = new float4(pos - ext.xyz, 1f);
                float4 bboxMax = new float4(pos + ext.xyz, 1f);
                //float3 bboxMin = pos - ext.xyz;
                //float3 bboxMax = pos + ext.xyz;
                bool visible = FrustumContainsBox(bboxMin.xyz, bboxMax.xyz);
                //float distanceToCamera = math.distance(cameraWorldPosition, pos);

                float3 heightExt = math.rotate(inverseCameraViewMatrix, new float3(0f, ext.y, 0f));
                bboxMin.xyz = pos - heightExt;
                bboxMax.xyz = pos + heightExt; 
                bboxMin = math.mul(cameraViewProjectionMatrix, bboxMin);
                bboxMax = math.mul(cameraViewProjectionMatrix, bboxMax);
                float screenRelativeHeight = math.abs(((bboxMax.y / bboxMax.w) - (bboxMin.y / bboxMin.w)) * 0.5f * ext.w); // we're using ext.w to store a scaling property that can bias LOD calculation
                  
                var clod = output[i];
                //clod.distanceToCamera = distanceToCamera;
                clod.screenRelativeHeight = screenRelativeHeight;
                clod.culled = !visible;

                float lodDist = float.MinValue;
                int minLod = 0;
                float minLodWeight = float.MaxValue;
                int2 lodStartIndexCount = lodStartIndicesCounts[i];
                for (int l = 0; l < lodStartIndexCount.y; l++)
                {
                    var lod = lods[l + lodStartIndexCount.x];

                    //bool inRange = lod.screenRelativeTransitionHeight > lodDist & distanceToCamera >= lod.screenRelativeTransitionHeight;
                    bool inRange = lod.screenRelativeTransitionHeight > lodDist & screenRelativeHeight >= lod.screenRelativeTransitionHeight;
                    //lodDist = math.select(lodDist, lod.screenRelativeTransitionHeight, inRange);
                    lodDist = math.select(lodDist, lod.screenRelativeTransitionHeight, inRange);
                    clod.lodLevel = math.select(clod.lodLevel, lod.detailLevel, inRange);

                    bool isMin = lod.screenRelativeTransitionHeight < minLodWeight;
                    minLodWeight = math.select(minLodWeight, lod.screenRelativeTransitionHeight, isMin);
                    minLod = math.select(minLod, lod.detailLevel, isMin); 
                }

                clod.lodLevel = math.select(clod.lodLevel, minLod, lodDist < 0);

                output[i] = clod;

                CullLOD prevClod = prevOutput[i]; 
                prevOutput[i] = clod;

                //Debug.Log(bboxMin + " : " + bboxMax + " : " + screenRelativeHeight + ": " + clod.lodLevel); /// UNCOMMENT BURST COMPILE 

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