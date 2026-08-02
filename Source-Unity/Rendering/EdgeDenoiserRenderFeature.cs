using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

namespace Swole.Rendering
{

    public class EdgeDenoiserRenderFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class FeatureSettings
        {
            public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
            public Shader denoiseShader;
            [HideInInspector]
            public Material denoiserMaterial;

            [Header("Radius Settings")]
            [Range(0.1f, 2.0f)] public float minRadius = 0.8f;
            [Range(1.0f, 5.0f)] public float maxRadius = 2.2f;
            [Range(1.0f, 10.0f)] public float noiseSensitivity = 4.0f;

            [Header("Bilateral Filtering Sensitivity")]
            [Range(0.001f, 0.1f)] public float baseDepthThreshold = 0.015f;
            [Range(0.05f, 1.0f)] public float baseColorThreshold = 0.35f;
        }

        public FeatureSettings settings = new FeatureSettings();
        private Material m_Material;
        private DenoiserRenderPass m_DenoiserPass;

        public override void Create()
        {
            if (settings.denoiseShader != null)
            {
                m_Material = CoreUtils.CreateEngineMaterial(settings.denoiseShader);
            }
            settings.denoiserMaterial = m_Material;
            m_DenoiserPass = new DenoiserRenderPass(settings);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType != CameraType.Game) return;
            m_DenoiserPass.renderPassEvent = settings.renderPassEvent;
            renderer.EnqueuePass(m_DenoiserPass);
        }

        protected override void Dispose(bool disposing) 
        {
            CoreUtils.Destroy(m_Material);
        }

        private class DenoiserRenderPass : ScriptableRenderPass
        {
            private FeatureSettings m_Settings;

            public DenoiserRenderPass(FeatureSettings settings)
            {
                m_Settings = settings;
            }

            private class PassData
            {
                public TextureHandle sourceTex;
                public TextureHandle tempTarget;
                public Material material;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var resourceData = frameData.Get<UniversalResourceData>();
                var cameraData = frameData.Get<UniversalCameraData>();

                if (!resourceData.activeColorTexture.IsValid() ||
                    !resourceData.cameraDepthTexture.IsValid() ||
                    m_Settings.denoiserMaterial == null)
                    return;

                // Update Material Instance properties safely inside the render loop registry stage
                m_Settings.denoiserMaterial.SetFloat("_MinRadius", m_Settings.minRadius);
                m_Settings.denoiserMaterial.SetFloat("_MaxRadius", m_Settings.maxRadius);
                m_Settings.denoiserMaterial.SetFloat("_NoiseSensitivity", m_Settings.noiseSensitivity);
                m_Settings.denoiserMaterial.SetFloat("_BaseDepthThreshold", m_Settings.baseDepthThreshold);
                m_Settings.denoiserMaterial.SetFloat("_BaseColorThreshold", m_Settings.baseColorThreshold);

                var desc = cameraData.cameraTargetDescriptor;
                desc.msaaSamples = 1;
                desc.depthBufferBits = 0;
                TextureHandle tempTarget = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_GeneralDenoiserTemp", false);

                // PASS 1: PRE-BLIT BACKDROP COPY
                using (var builder = renderGraph.AddRasterRenderPass<PassData>("Denoiser PreCopy", out var pd))
                {
                    pd.sourceTex = resourceData.activeColorTexture;
                    pd.tempTarget = tempTarget;
                    builder.UseTexture(pd.sourceTex, AccessFlags.Read);
                    builder.SetRenderAttachment(pd.tempTarget, 0, AccessFlags.Write);
                    builder.AllowPassCulling(false);

                    builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
                    {
                        Blitter.BlitTexture(ctx.cmd, data.sourceTex, new Vector4(1, 1, 0, 0), 0, false);
                    });
                }

                // PASS 2: EXECUTE ADAPTIVE DENOISER
                using (var builder = renderGraph.AddRasterRenderPass<PassData>("Denoiser Execute", out var pd))
                {
                    pd.sourceTex = tempTarget;
                    pd.tempTarget = resourceData.activeColorTexture;
                    pd.material = m_Settings.denoiserMaterial;

                    builder.UseTexture(pd.sourceTex, AccessFlags.Read);
                    builder.SetRenderAttachment(pd.tempTarget, 0, AccessFlags.Write);
                    builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Read, 0, -1);
                    builder.AllowPassCulling(false);

                    builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
                    {
                        Blitter.BlitTexture(ctx.cmd, data.sourceTex, new Vector4(1, 1, 0, 0), data.material, 0);
                    });
                }
            }
        }
    }

}
