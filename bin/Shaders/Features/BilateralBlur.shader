Shader "Hidden/BilateralBlur"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            Name "BilateralBlurPass"

            // --- NATIVE SHADERLAB COMPILATION TARGET ---
            // We tell the hardware to ALWAYS allow this shader to execute across the screen.
            // Our internal mathematical thresholds will handle the masking and dilation!
            //Stencil
            //{
            //    Ref 4
            //    Comp Always
            //    Pass Keep
            //}

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 4.5
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            float4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
    
                // PERFECT HALF-WAY MULTIPLIER: Expanded from 1.2 to 1.8 for stronger, 
                // wider internal smoothing while avoiding the artifact gaps of 2.5.
                float2 texelSize = _BlitTexture_TexelSize.xy * 3; 

                float4 centerColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                float centerDepth = SampleSceneDepth(uv);
                float centerDepthLinear = Linear01Depth(centerDepth, _ZBufferParams);

                // Native 2x2 Gather to compute edge thickness gradients
                float4 gatherDepth = GATHER_RED_TEXTURE2D_X(_CameraDepthTexture, sampler_PointClamp, uv);
                float minDepth = min(min(gatherDepth.x, gatherDepth.y), min(gatherDepth.z, gatherDepth.w));
                float maxDepth = max(max(gatherDepth.x, gatherDepth.y), max(gatherDepth.z, gatherDepth.w));
                float depthRange = abs(Linear01Depth(maxDepth, _ZBufferParams) - Linear01Depth(minDepth, _ZBufferParams));

                float4 colorSum = centerColor;
                float weightSum = 1.0;

                // ROTATED BOX KERNEL: Kept intact to completely eliminate the diamond artifacts
                float2 offsets[8] = {
                    float2(-1.0, -1.0), float2(1.0, -1.0), float2(-1.0, 1.0), float2(1.0, 1.0),
                    float2(-1.4,  0.0), float2(1.4,  0.0), float2( 0.0, -1.4), float2(0.0, 1.4)
                };

                // Widened thresholds to allow aggressive background-to-hair boundary blending
                float dynamicDepthThreshold = lerp(0.03, 0.006, saturate(depthRange * 30.0));
                const float COLOR_THRESHOLD = 0.55; // Raised slightly to encourage edge color blending

                bool nearHairEdge = false;

                // BULLETPROOF SKYBOX DETECTION: Check if center pixel is the sky using raw hardware limits
                bool centerIsSky = (abs(centerDepth - UNITY_RAW_FAR_CLIP_VALUE) < 0.00001);

                for(int i = 0; i < 8; i++)
                {
                    float2 sampleUV = uv + offsets[i] * texelSize;
        
                    float sampleDepth = SampleSceneDepth(sampleUV);
                    float sampleDepthLinear = Linear01Depth(sampleDepth, _ZBufferParams);
                    float depthDiff = abs(centerDepthLinear - sampleDepthLinear);
        
                    float depthWeight = exp(- (depthDiff * depthDiff) / (dynamicDepthThreshold * dynamicDepthThreshold));
        
                    // Check if the sampled neighbor pixel hits the infinite background skybox
                    bool sampleIsSky = (abs(sampleDepth - UNITY_RAW_FAR_CLIP_VALUE) < 0.00001);

                    if (depthDiff < dynamicDepthThreshold)
                    {
                        nearHairEdge = true;
                    }
                    else
                    {
                        // SKY INTERSECTION RULE: If central pixel is hair, but neighbor is sky,
                        // force nearHairEdge to true so the edge pass isn't optimized away!
                        if (!centerIsSky && sampleIsSky)
                        {
                            nearHairEdge = true;
                            depthWeight = 0.45; // Increased fallback weight to blend across the border
                        }
                        else
                        {
                            // HYBRID DILATION FALLBACK: If a sample steps across the hair silhouette edge, 
                            // force a minor blending weight to push the blur past the dithered borders.
                            depthWeight = 0.25; 
                        }
                    }

                    float4 sampleColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, sampleUV);
                    float colorDiff = distance(centerColor.rgb, sampleColor.rgb);
                    float colorWeight = exp(- (colorDiff * colorDiff) / (COLOR_THRESHOLD * COLOR_THRESHOLD));

                    // BYPASS COLOR TESTING AGAINST THE SKY: 
                    // This prevents extreme brightness differences (like a dark hair strand against a bright daytime sky)
                    // from multiplying your blur weight down to 0.
                    if (!centerIsSky && sampleIsSky)
                    {
                        colorWeight = 1.0;
                    }

                    // Gaussian spatial falloff (pixels closer to center weight more)
                    float spatialWeight = exp(-dot(offsets[i], offsets[i]) * 0.4);
                    float totalSampleWeight = depthWeight * colorWeight * spatialWeight;

                    colorSum += sampleColor * totalSampleWeight;
                    weightSum += totalSampleWeight;
                }

                // EDGE DISCARD: Perfectly keeps non-hair scene elements 100% sharp
                if (!nearHairEdge && centerDepthLinear > 0.001)
                {
                    return centerColor;
                }

                return colorSum / weightSum;
            }



            ENDHLSL
        }
    }
}