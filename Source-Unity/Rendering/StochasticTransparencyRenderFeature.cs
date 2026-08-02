using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

namespace Swole.Rendering
{
    public class StochasticTransparencyRenderFeature : ScriptableRendererFeature
    {

        [System.Serializable]
        public class FeatureSettings
        {
            public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
            // This LayerMask must contain ONLY your hair/transparent geometry objects
            public LayerMask transparentLayerMask;
            public Shader accumulationShader;
        }

        public FeatureSettings settings = new FeatureSettings();
        private StochasticRenderPass m_StochasticPass;

        // Cache the Unity Shader Property IDs globally
        private static int ExactAlphaTexID;// = Shader.PropertyToID("_ExactTotalAlphaTex");
        private static int StochasticDepthMSAAID;// = Shader.PropertyToID("_StochasticDepthMSAA");

        public override void Create()
        {
            m_StochasticPass = new StochasticRenderPass(settings);

            ExactAlphaTexID = Shader.PropertyToID("_ExactTotalAlphaTex");
            StochasticDepthMSAAID = Shader.PropertyToID("_StochasticDepthMSAA"); 
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType != CameraType.Game) return;
            m_StochasticPass.renderPassEvent = settings.renderPassEvent;
            renderer.EnqueuePass(m_StochasticPass);
        }

        protected override void Dispose(bool disposing) { }

        private class StochasticRenderPass : ScriptableRenderPass
        {
            private FeatureSettings m_Settings;
            private FilteringSettings m_FilteringSettings;
            private ShaderTagId m_ShaderTagId = new ShaderTagId("UniversalForward");

            public StochasticRenderPass(FeatureSettings settings)
            {
                m_Settings = settings;
                m_FilteringSettings = new FilteringSettings(RenderQueueRange.all, m_Settings.transparentLayerMask);
            }

            private class PassData
            {
                public TextureHandle alphaTex;
                public TextureHandle msaaDepthTex;
                public RendererListHandle rendererList;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                // UNITY 6 CORRECTION: Pull all decoupled frame contexts individually
                var resourceData = frameData.Get<UniversalResourceData>();
                var renderingData = frameData.Get<UniversalRenderingData>();
                var cameraData = frameData.Get<UniversalCameraData>();
                var lightData = frameData.Get<UniversalLightData>();

                if (!resourceData.activeColorTexture.IsValid() || m_Settings.accumulationShader == null) return;

                // --- ALLOCATE TARGET TEXTURES ---
                var desc = cameraData.cameraTargetDescriptor;

                // Texture 1: Flat R8 format for exact accumulated mathematical alpha
                RenderTextureDescriptor alphaDesc = desc;
                alphaDesc.colorFormat = RenderTextureFormat.R8;
                alphaDesc.msaaSamples = 1;
                alphaDesc.depthBufferBits = 0;
                TextureHandle exactAlphaTex = UniversalRenderer.CreateRenderGraphTexture(renderGraph, alphaDesc, "_ExactTotalAlphaTex", true);

                // Texture 2: Isolated 8x MSAA Depth Texture for the stochastic visibility oracle
                RenderTextureDescriptor depthMSDesc = desc;
                depthMSDesc.colorFormat = RenderTextureFormat.RFloat;
                depthMSDesc.msaaSamples = 8; // Locked to 8x hardware MSAA
                depthMSDesc.depthBufferBits = 0;

                // Explicitly force the engine to keep samples raw instead of planning a downscale resolve
                depthMSDesc.bindMS = true;

                TextureHandle stochasticDepthMSAA = UniversalRenderer.CreateRenderGraphTexture(renderGraph, depthMSDesc, "_StochasticDepthMSAA", true);

                // UNITY 6 CORRECTION: Create drawing settings via the new static RenderingUtils utility
                SortingCriteria sorting = cameraData.defaultOpaqueSortFlags;
                DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(m_ShaderTagId, renderingData, cameraData, lightData, sorting);

                // ==========================================
                // PASS 1: RENDER EXACT TOTAL ALPHA
                // ==========================================
                using (var builder = renderGraph.AddRasterRenderPass<PassData>("Stochastic Pass 1: Total Alpha", out var pd))
                {
                    DrawingSettings alphaSettings = drawingSettings;
                    alphaSettings.overrideShader = m_Settings.accumulationShader;
                    alphaSettings.overrideShaderPassIndex = 0; // Tells it to run the TotalAlphaPass

                    pd.alphaTex = exactAlphaTex;
                    builder.SetRenderAttachment(pd.alphaTex, 0, AccessFlags.Write);
                    builder.AllowPassCulling(false);

                    // No color masks or overrides needed. Because pd.alphaTex is an R8 texture format,
                    // the hardware natively discards G, B, and A data out-of-the-box!
                    RendererListParams rlParams = new RendererListParams(renderingData.cullResults, alphaSettings, m_FilteringSettings);
                    pd.rendererList = renderGraph.CreateRendererList(in rlParams);
                    builder.UseRendererList(pd.rendererList);

                    builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
                    {
                        // Perfectly safe, standard Render Graph drawing call
                        ctx.cmd.DrawRendererList(data.rendererList);
                    });
                }

                // ==========================================
                // PASS 2: STOCHASTIC MSAA DEPTH ONLY (FIXED)
                // ==========================================
                using (var builder = renderGraph.AddRasterRenderPass<PassData>("Stochastic Pass 2: Stochastic Depth", out var pd))
                {
                    DrawingSettings depthSettings = drawingSettings;
                    depthSettings.overrideShader = m_Settings.accumulationShader;
                    depthSettings.overrideShaderPassIndex = 1; // Tells it to run the StochasticDepthPass

                    pd.msaaDepthTex = stochasticDepthMSAA;

                    // UNITY 6 CORRECTION: We bind the texture as a color attachment instead of a depth attachment.
                    // This forces the graph to treat it as a raw multi-sampled buffer target, 
                    // completely bypassing the missing depth resolve error validation checks.
                    builder.SetRenderAttachment(pd.msaaDepthTex, 0, AccessFlags.Write);
                    builder.AllowPassCulling(false);

                    RendererListParams rlParams = new RendererListParams(renderingData.cullResults, depthSettings, m_FilteringSettings);
                    pd.rendererList = renderGraph.CreateRendererList(in rlParams);
                    builder.UseRendererList(pd.rendererList);

                    builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
                    {
                        ctx.cmd.DrawRendererList(data.rendererList);
                    });
                }

                // ==========================================
                // PASS 3: COLOR ACCUMULATION & ANALYTICAL CORRECTION (STABLE UNITY 6 IMPLEMENTATION)
                // ==========================================
                using (var builder = renderGraph.AddRasterRenderPass<PassData>("Stochastic Pass 3: Accumulate Color", out var pd))
                {
                    DrawingSettings overrideSettings = drawingSettings;
                    overrideSettings.overrideShader = m_Settings.accumulationShader;
                    overrideSettings.overrideShaderPassIndex = 2; // Run the AccumulationPass

                    builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);
                    builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Read, 0, -1);

                    // 1. Pack the handles into PassData
                    pd.alphaTex = exactAlphaTex;
                    pd.msaaDepthTex = stochasticDepthMSAA;

                    // 2. Declare texture read dependencies
                    builder.UseTexture(pd.alphaTex, AccessFlags.Read);
                    builder.UseTexture(pd.msaaDepthTex, AccessFlags.Read);
                    builder.AllowPassCulling(false);

                    // 3. UNITY 6 CRITICAL EXPLICIT HOOK: 
                    // This explicitly grants permission to call SetGlobalTexture inside the execution lambda,
                    // eliminating the "Modifying global state from this command buffer is not allowed" crash!
                    builder.AllowGlobalStateModification(true);

                    RendererListParams rlParams = new RendererListParams(renderingData.cullResults, overrideSettings, m_FilteringSettings);
                    pd.rendererList = renderGraph.CreateRendererList(in rlParams);
                    builder.UseRendererList(pd.rendererList);

                    builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
                    {
                        // 4. Bind the handles to global shader properties safely using the RasterCommandBuffer signatures
                        ctx.cmd.SetGlobalTexture(ExactAlphaTexID, data.alphaTex);
                        ctx.cmd.SetGlobalTexture(StochasticDepthMSAAID, data.msaaDepthTex);

                        ctx.cmd.DrawRendererList(data.rendererList); 
                    });
                }


            }
        }
    }

}
