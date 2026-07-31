using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Unity.Mathematics;

namespace Swole.API.Unity
{
    public class BoundMesh : MonoBehaviour, IDisposable
    {
        public const string defaultMatProp_meshProxyBindings = "_MeshProxyBindings";

        public string proxyKeyword = "USE_PROXY_MESH";

        public string bindingsMaterialPropertyName = "_MeshProxyBindings";
        public string skinningBindingsMaterialPropertyName = "_SkinBindings";
        public string skinningMatricesMaterialPropertyName = "_SkinningMatrices";

        [SerializeField]
        protected MeshBindingData bindings; 

        [Serializable]
        public class TargetRenderer : IDisposable
        {
            public Renderer renderer;

            public string proxyKeywordOverride;

            public string bindingsMaterialPropertyNameOverride;
            public string skinningBindingsMaterialPropertyNameOverride;
            public string skinningMatricesMaterialPropertyNameOverride;

            public Material[] materials;

            public void Init(BoundMesh bm)
            {
                if (renderer != null)
                {
                    materials = renderer.materials;

                    var bindingsPropertyName = string.IsNullOrWhiteSpace(bindingsMaterialPropertyNameOverride) ? bm.bindingsMaterialPropertyName : bindingsMaterialPropertyNameOverride;
                    var skinningBindingsPropertyName = string.IsNullOrWhiteSpace(skinningBindingsMaterialPropertyNameOverride) ? bm.skinningBindingsMaterialPropertyName : skinningBindingsMaterialPropertyNameOverride;
                    foreach (var material in materials) 
                    {
                        if (material != null)
                        {
                            material.EnableKeyword(string.IsNullOrWhiteSpace(proxyKeywordOverride) ? bm.proxyKeyword : proxyKeywordOverride);
                            material.SetBuffer(bindingsPropertyName, bm.bindings.BindingsBuffer);

                            material.SetBuffer(skinningBindingsPropertyName, bm.bindings.BoneWeightsBuffer);
                        }
                    }
                }
            }

            public void Dispose()
            {
                if (materials != null)
                {
                    foreach (var mat in materials) Destroy(mat);
                    materials = null;
                }
            }
        }

        [SerializeField]
        protected TargetRenderer[] targetRenderers;

        protected bool initialized;
        public bool IsInitialized => initialized;

        public void Initialize()
        {
            if (initialized) return;

            if (targetRenderers != null)
            {
                foreach (var target in targetRenderers)
                {
                    if (target != null)
                    {
                        target.Init(this); 
                    }
                }
            }

            initialized = true;
        }

        protected void Awake()
        {
            Initialize();
        }

        public IEnumerable<(Material, string)> SkinnedMaterials()
        {
            if (targetRenderers != null)
            {
                foreach (var target in targetRenderers)
                {
                    if (target != null && target.materials != null)
                    {
                        var skinningMatricesPropertyName = string.IsNullOrWhiteSpace(target.skinningMatricesMaterialPropertyNameOverride) ? skinningMatricesMaterialPropertyName : target.skinningMatricesMaterialPropertyNameOverride;
                        foreach (var mat in target.materials)
                        {
                            if (mat != null)
                            {
                                yield return (mat, skinningMatricesPropertyName);
                            }
                        }
                    }
                }
            }
        }

        public void SetSkinningMatrices(ComputeBuffer buffer) 
        {
            foreach(var v in SkinnedMaterials())
            {
                v.Item1.SetBuffer(v.Item2, buffer);
            }
        }
        public void SetSkinningMatrices(GraphicsBuffer buffer) 
        {
            foreach (var v in SkinnedMaterials())
            {
                v.Item1.SetBuffer(v.Item2, buffer);
            }
        }

        private List<InstanceBuffer<float4x4>> boundInstanceBuffers;
        public void BindSkinningMatrices(InstanceBuffer<float4x4> buffer)
        {
            if (boundInstanceBuffers == null) boundInstanceBuffers = new List<InstanceBuffer<float4x4>>();
            if (!boundInstanceBuffers.Contains(buffer)) boundInstanceBuffers.Add(buffer);

            foreach (var v in SkinnedMaterials())
            {
                buffer.BindMaterialProperty(v.Item1, v.Item2);  
            }
        }
        public void UnbindSkinningMatrices(InstanceBuffer<float4x4> buffer, bool removeFromBoundList = true)
        {
            foreach (var v in SkinnedMaterials())
            {
                buffer.UnbindMaterialProperty(v.Item1, v.Item2);
            }

            if (removeFromBoundList && boundInstanceBuffers != null) boundInstanceBuffers.Remove(buffer);
        }

        public void Dispose()
        {
            if (boundInstanceBuffers != null)
            {
                foreach (var buffer in boundInstanceBuffers)
                {
                    if (buffer != null && buffer.IsValid()) UnbindSkinningMatrices(buffer, false);  
                }

                boundInstanceBuffers = null;
            }

            if (targetRenderers != null)
            {
                foreach (var target in targetRenderers)
                {
                    if (target != null) target.Dispose();
                }
                targetRenderers = null;
            }
        }

        public void OnDestroy()
        {
            Dispose();
        }

        public IEnumerable<Material> Materials()
        {
            if (targetRenderers != null)
            {
                foreach (var target in targetRenderers)
                {
                    if (target != null && target.materials != null)
                    {
                        foreach (var mat in target.materials)
                        {
                            yield return mat;
                        }
                    }
                }
            }
        }

        public IEnumerable<Renderer> Renderers()
        {
            if (targetRenderers != null)
            {
                foreach (var target in targetRenderers)
                {
                    if (target != null && target.renderer != null)
                    {
                        yield return target.renderer;
                    }
                }
            }
        }

    }
}
