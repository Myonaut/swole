using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

namespace Swole.Rendering
{

    public class BilateralBlurRenderFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class BlurSettings
        {
            public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
            public Shader blurShader;
        }

        public BlurSettings settings = new BlurSettings();
        private Material m_Material;
        private BilateralBlurPass m_BlurPass;

        public override void Create()
        {
            if (settings.blurShader != null)
            {
                m_Material = CoreUtils.CreateEngineMaterial(settings.blurShader);
            }
            m_BlurPass = new BilateralBlurPass(m_Material);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            // Don't execute if material is missing, or if we are rendering scene previews/reflections
            if (m_Material == null || renderingData.cameraData.cameraType != CameraType.Game)
                return;

            m_BlurPass.renderPassEvent = settings.renderPassEvent;

            renderer.EnqueuePass(m_BlurPass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(m_Material);
        }

        private class BilateralBlurPass : ScriptableRenderPass
        {
            private Material m_BlitMaterial;

            public BilateralBlurPass(Material mat)
            {
                m_BlitMaterial = mat;
                // Tell URP we require depth access so it generates the texture buffer
                ConfigureInput(ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth);
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

                // CRASH PROTECTION: Instantly back out if buffers aren't ready on frame 1
                if (!resourceData.activeColorTexture.IsValid() ||
                    !resourceData.cameraDepthTexture.IsValid() ||
                    m_BlitMaterial == null)
                {
                    return;
                }

                // Allocate a safe temporary scratch texture matching screen specs
                var desc = cameraData.cameraTargetDescriptor;
                desc.msaaSamples = 1;
                desc.depthBufferBits = 0;
                TextureHandle tempTarget = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_BilateralBlurTemp", false);
                 
                // ==========================================
                // NEW PASS: BASELINE BACKDROP COPY
                // ==========================================
                // This copies your entire game world (everything!) to the scratch texture first
                using (var builder = renderGraph.AddRasterRenderPass<PassData>("Bilateral Blur PreCopy", out var pd))
                {
                    pd.sourceTex = resourceData.activeColorTexture;
                    pd.tempTarget = tempTarget;
                    builder.UseTexture(pd.sourceTex, AccessFlags.Read);
                    builder.SetRenderAttachment(pd.tempTarget, 0, AccessFlags.Write);
                    builder.AllowPassCulling(false);

                    builder.SetRenderFunc((PassData data, RasterGraphContext ctx) => {
                        Blitter.BlitTexture(ctx.cmd, data.sourceTex, new Vector4(1, 1, 0, 0), 0, false);
                    });
                }

                // PASS 1: Read active scene, execute bilateral shader, write to scratch texture
                using (var builder = renderGraph.AddRasterRenderPass<PassData>("Bilateral Blur", out var pd))
                {
                    pd.sourceTex = resourceData.activeColorTexture;
                    pd.tempTarget = tempTarget;
                    pd.material = m_BlitMaterial;

                    builder.UseTexture(pd.sourceTex, AccessFlags.Read);
                    builder.SetRenderAttachment(pd.tempTarget, 0, AccessFlags.Write);

                    // UNITY 6 REQUIREMENT: Bind the depth texture buffer using ReadWrite flags.
                    // This forces the GPU to keep the active stencil memory alive during our blit.
                    builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.ReadWrite, 0, -1);
                    builder.AllowPassCulling(false);

                    builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
                        Blitter.BlitTexture(ctx.cmd, data.sourceTex, new Vector4(1, 1, 0, 0), data.material, 0));
                }

                // PASS 2: Copy the filtered image from scratch texture back to the camera target
                using (var builder = renderGraph.AddRasterRenderPass<PassData>("Compose Blur Back", out var pd))
                {
                    pd.sourceTex = tempTarget;
                    pd.tempTarget = resourceData.activeColorTexture;

                    builder.UseTexture(pd.sourceTex, AccessFlags.Read);
                    builder.SetRenderAttachment(pd.tempTarget, 0, AccessFlags.Write);
                    builder.AllowPassCulling(false);

                    builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
                    {
                        Blitter.BlitTexture(ctx.cmd, data.sourceTex, new Vector4(1, 1, 0, 0), 0, false);
                    });
                }
            }
        }
    }


}
