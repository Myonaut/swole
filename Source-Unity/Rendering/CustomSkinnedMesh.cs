#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Swole.API.Unity.Animation;

namespace Swole
{

    public class CustomSkinnedMesh : SingletonBehaviour<CustomSkinnedMesh>
    {

        public static int ExecutionPriority => CustomAnimatorUpdater.FinalAnimationBehaviourPriority + 1; // update after animation
        public override int Priority => ExecutionPriority;
        public override void OnFixedUpdate() { }

        public const int maxBlendShapeCount = 64;
        public const int maxBlendShapeFrameCount = 8;

        public const string blendShapeCountMaterialProperty = "_BlendShapeCount";

        public const string blendShapeFrameCountsMaterialProperty = "_BlendShapeFrameCounts";
        public const string frameWeightsMaterialProperty = "_BlendShapeFrameWeights";
        public const string blendShapeDataMaterialProperty = "_BlendShapeData";

        public const string blendShapeWeightsMaterialCBuffer = "BlendShapeHeaderData";

        public const string skinningMatricesMaterialProperty = "_SkinningMatrices";
        public const string skinBindingsMaterialProperty = "_SkinBindings";

        public class RenderedMesh : IDisposable
        {

            public bool disposed;

            public CustomSkinnedMeshData data;

            protected int m_blendShapeCount;
            public int BlendShapeCount => m_blendShapeCount;

            public string GetBlendShapeName(int shapeIndex) => data == null ? "" : data.GetBlendShapeName(shapeIndex);

            public int GetBlendShapeFrameCount(int shapeIndex) => data == null ? 0 : data.GetBlendShapeFrameCount(shapeIndex);

            public float GetBlendShapeFrameWeight(int shapeIndex, int frameIndex) => data == null ? 0 : data.GetBlendShapeFrameWeight(shapeIndex, frameIndex);

            public RenderedMesh(CustomSkinnedMeshData data, Rigs.StandaloneSampler sampler, string rigId, MeshRenderer meshRenderer, Material[] materials)
            {

                m_blendShapeCount = data.blendShapeNames.Length;

                var skinBindingsBuffer = data.UseBoneWeightsBuffer();
                var blendShapeDataBuffer = data.UseBlendShapeDataBuffer();

                blendShapeWeights = new NativeArray<float4>(Mathf.CeilToInt(m_blendShapeCount / 4f), Allocator.Persistent);
                blendShapeWeightsBuffer = new ComputeBuffer(blendShapeWeights.Length, UnsafeUtility.SizeOf(typeof(float4)), ComputeBufferType.Constant, ComputeBufferMode.SubUpdates);

                sampler.RegisterAsUser();

                this.data = data;
                this.sampler = sampler;
                this.rigId = rigId; 
                this.meshRenderer = meshRenderer;
                this.materials = materials;

                for (int a = 0; a < materials.Length; a++)
                {

                    var material = materials[a];

                    if (material == null) continue;

                    material.SetInt(blendShapeCountMaterialProperty, m_blendShapeCount);

                    material.SetFloatArray(blendShapeFrameCountsMaterialProperty, data.blendShapeFrameCounts);
                    material.SetFloatArray(frameWeightsMaterialProperty, data.blendShapeFrameWeights);

                    material.SetConstantBuffer(blendShapeWeightsMaterialCBuffer, blendShapeWeightsBuffer, 0, blendShapeWeightsBuffer.count * UnsafeUtility.SizeOf(typeof(float4)));

                    material.SetBuffer(blendShapeDataMaterialProperty, blendShapeDataBuffer);

                    material.SetBuffer(skinBindingsMaterialProperty, skinBindingsBuffer);
                    material.SetBuffer(skinningMatricesMaterialProperty, sampler.Buffer);

                }

                UpdateBlendShapeWeights();

            }

            public string rigId;
            public Rigs.StandaloneSampler sampler;
            public MeshRenderer meshRenderer;
            public Material[] materials;

            public ComputeBuffer blendShapeWeightsBuffer;

            public NativeArray<float4> blendShapeWeights;

            protected bool m_dirty;
            public bool IsDirty(bool reset = true)
            {

                bool dirty = m_dirty;

                if (reset) m_dirty = false;

                return dirty;

            }

            public int GetBlendShapeIndex(string blendShapeName)
            {

                if (data.blendShapeNames == null) return -1;

                for (int a = 0; a < data.blendShapeNames.Length; a++) if (data.blendShapeNames[a] == blendShapeName) return a;

                return -1;

            }

            public void SetBlendShapeWeight(string blendShapeName, float weight)
            {

                SetBlendShapeWeight(GetBlendShapeIndex(blendShapeName), weight);

            }

            protected float4 SetWeight(float4 oldWeight, int compIndex, float weight)
            {

                if (compIndex <= 0)
                {
                    oldWeight.x = weight;
                }
                else if (compIndex == 1)
                {
                    oldWeight.y = weight;
                }
                else if (compIndex == 2)
                {
                    oldWeight.z = weight;
                }
                else
                {
                    oldWeight.w = weight;
                }

                return oldWeight;

            }

            public void SetBlendShapeWeight(int index, float weight)
            {

                int subIndex = index / 4;
                int compIndex = index % 4;

                if (subIndex < 0 || subIndex >= blendShapeWeights.Length) return;

                var oldWeight = blendShapeWeights[subIndex];
                var newWeight = SetWeight(oldWeight, compIndex, weight);

                if (math.any(oldWeight != newWeight))
                {

                    blendShapeWeights[subIndex] = newWeight;

                    m_dirty = true;

                }

            }

            public void UpdateBlendShapeWeights()
            {

                var array = blendShapeWeightsBuffer.BeginWrite<float4>(0, blendShapeWeights.Length);

                blendShapeWeights.CopyTo(array);

                blendShapeWeightsBuffer.EndWrite<float4>(blendShapeWeights.Length);

            }

            public void Dispose()
            {

                disposed = true;

                rigId = null;
                meshRenderer = null;
                materials = null;

                if (blendShapeWeights.IsCreated) blendShapeWeights.Dispose();
                blendShapeWeights = default;

                if (blendShapeWeightsBuffer != null) blendShapeWeightsBuffer.Dispose();
                blendShapeWeightsBuffer = null;

                if (data != null)
                {

                    data.EndUseBoneWeightsBuffer();
                    data.EndUseBlendShapeDataBuffer();

                }

                data = null;

                if (sampler != null) 
                { 
                    sampler.UnregisterAsUser();
                    sampler.TryDispose();
                }
                sampler = null;

            }

        }

        protected List<RenderedMesh> renderedMeshes = new List<RenderedMesh>();

        public override void OnDestroyed()
        {

            base.OnDestroyed();

            if (renderedMeshes != null)
            {

                for (int a = 0; a < renderedMeshes.Count; a++)
                {

                    var mesh = renderedMeshes[a];

                    if (mesh == null) continue;

                    mesh.Dispose();

                }

                renderedMeshes = null;

            }

        }

        public static bool TryAddMesh(out RenderedMesh mesh, string rigId, CustomSkinnedMeshData data, MeshRenderer renderer, Material[] materials = null, bool instantiateMaterials = true)
        {

            mesh = null;

            if (string.IsNullOrEmpty(rigId) || renderer == null || data.boneWeights == null || data.boneWeights.Length == 0) return false;

            if (materials == null) materials = renderer.sharedMaterials;

            if (materials == null || materials.Length == 0) return false;

            if (instantiateMaterials)
            {

                for (int a = 0; a < materials.Length; a++) if (materials[a] != null) materials[a] = Instantiate(materials[a]);
                renderer.sharedMaterials = materials;

            }

            if (!Rigs.TryGetStandaloneSampler(rigId, out var sampler)) return false;

            mesh = new RenderedMesh(data, sampler, rigId, renderer, materials);

            Instance.renderedMeshes.Add(mesh);

            return true;

        }

        public static bool TryAddMesh(out RenderedMesh mesh, CustomSkinnedMeshData data, Transform[] bones, MeshRenderer renderer, Material[] materials = null, bool instantiateMaterials = true, string rigId = null)
        {

            mesh = null;

            if (string.IsNullOrEmpty(rigId)) rigId = (bones == null || bones.Length <= 0 || bones[0] == null) ? renderer.GetInstanceID().ToString() : bones[0].GetInstanceID().ToString();//renderer.name;

            if (data == null || string.IsNullOrEmpty(rigId) || bones == null || data.bindpose == null || renderer == null || data.boneWeights == null || data.boneWeights.Length == 0) return false;

            if (materials == null || materials.Length <= 0) materials = renderer.sharedMaterials;

            if (materials == null || materials.Length == 0) return false;

            if (instantiateMaterials)
            {

                for (int a = 0; a < materials.Length; a++) if (materials[a] != null) materials[a] = Instantiate(materials[a]);
                renderer.sharedMaterials = materials;

            }

            if (!Rigs.CreateStandaloneSampler(rigId, bones, data.bindpose, out var sampler))
            {

                if (!Rigs.TryGetStandaloneSampler(rigId, out sampler)) return false;

            }

            mesh = new RenderedMesh(data, sampler, rigId, renderer, materials);
            
            Instance.renderedMeshes.Add(mesh);

            return true;

        }

        public override void OnUpdate()
        {

            renderedMeshes.RemoveAll(i => i.disposed);

            /*for (int a = 0; a < renderedMeshes.Count; a++)
            {

                var mesh = renderedMeshes[a];

                if (mesh.disposed || mesh.meshRenderer == null)
                {

                    mesh.Dispose();

                    continue;

                }

                if (mesh.sampler != null) mesh.sampler.Refresh();

            }*/

        }

        public override void OnLateUpdate()
        {

            // moved from OnUpdate
            for (int a = 0; a < renderedMeshes.Count; a++)
            {

                var mesh = renderedMeshes[a];

                if (mesh.disposed || mesh.meshRenderer == null)
                {

                    mesh.Dispose();

                    continue;

                }

                //if (mesh.sampler != null) mesh.sampler.Refresh(true);

            }
            //

            for (int a = 0; a < renderedMeshes.Count; a++)
            {

                var mesh = renderedMeshes[a];

                if (mesh.disposed || mesh.meshRenderer == null)
                {

                    mesh.Dispose();

                    continue;

                }

                //if (mesh.sampler != null) mesh.sampler.UpdatePoseInBuffer();

                if (mesh.IsDirty()) mesh.UpdateBlendShapeWeights();

            }

        }

    }

}

#endif