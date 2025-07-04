#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Swole
{
    public class GaussianBlurBlitRenderPass : ScriptableRenderPass
    {

        public float blurStrength = 5.0f;
        public float resolutionScale = 1.0f;
        public RenderTexture outputTarget;

        private Material material; 
        private GaussianBlurSettings settings;

        private RenderTextureDescriptor blurTextureDescriptor;
        private RTHandle blurTextureHandle;
         
        public bool Setup()
        {
            material = new Material(Shader.Find("Hidden/GaussianBlur"));

            blurTextureDescriptor = new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.Default, 0);

            return true;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {

            // Set the blur texture size to be the same as the camera target size.
            blurTextureDescriptor.width = Mathf.CeilToInt(cameraTextureDescriptor.width * resolutionScale);
            blurTextureDescriptor.height = Mathf.CeilToInt(cameraTextureDescriptor.height * resolutionScale);

            if (outputTarget != null && (outputTarget.width != blurTextureDescriptor.width || outputTarget.height != blurTextureDescriptor.height))
            {
                outputTarget.Release();
                outputTarget.width = blurTextureDescriptor.width;
                outputTarget.height = blurTextureDescriptor.height; 
                outputTarget.Create();
            } 

            // Check if the descriptor has changed, and reallocate the RTHandle if necessary
            RenderingUtils.ReAllocateIfNeeded(ref blurTextureHandle, blurTextureDescriptor);

            base.Configure(cmd, cameraTextureDescriptor);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {

            if (material == null || !Application.isPlaying || blurTextureHandle == null) return;

            CommandBuffer cmd = CommandBufferPool.Get("Gaussian Blur"); 

            int gridSize = Mathf.Max(2, Mathf.CeilToInt(blurStrength * 6.0f));

            if (gridSize % 2 == 0)
            {
               gridSize++;
            }

            material.SetInteger("_GridSize", gridSize);
            material.SetFloat("_Spread", blurStrength);   

            RTHandle cameraTargetHandle = renderingData.cameraData.renderer.cameraColorTargetHandle;

            if (cameraTargetHandle != null)
            {
                Blit(cmd, cameraTargetHandle, blurTextureHandle, material, 0);

                if (outputTarget == null)
                {
                    Blit(cmd, blurTextureHandle, cameraTargetHandle, material, 1);
                }
                else
                {
                    //CoreUtils.SetRenderTarget(cmd, outputTarget);
                    Blitter.BlitTexture(cmd, blurTextureHandle.nameID, outputTarget, material, 1);
                }

                context.ExecuteCommandBuffer(cmd);
            }

            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (blurTextureHandle != null) blurTextureHandle.Release();

            base.FrameCleanup(cmd);
        }
    }
}

#endif