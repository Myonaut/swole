#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Swole
{
    public class GaussianBlurBlitRendererFeature : ScriptableRendererFeature
    {

        public RenderPassEvent whenToRender = RenderPassEvent.BeforeRenderingTransparents;

        public float blurStrength = 1.0f;
        [Range(0.1f, 1.0f)]
        public float resolutionScale = 1.0f;
        public RenderTexture outputTarget;

        GaussianBlurBlitRenderPass renderPass;

        public override void Create()
        {
            name = "Gaussian Blur Blit";

            renderPass = new GaussianBlurBlitRenderPass();

            renderPass.renderPassEvent = whenToRender;
            renderPass.blurStrength = blurStrength;
            renderPass.resolutionScale = resolutionScale; 
            renderPass.outputTarget = outputTarget;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderPass.Setup())
            {
                renderer.EnqueuePass(renderPass); 
            }
        }
    }
}

#endif