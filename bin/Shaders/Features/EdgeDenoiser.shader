Shader "Hidden/EdgeDenoiser"
{
    Properties
    {
        _MinRadius ("Minimum Blur Radius", Range(0.1, 2.0)) = 0.8
        _MaxRadius ("Maximum Blur Radius", Range(1.0, 5.0)) = 2.5
        _NoiseSensitivity ("Noise Sensitivity", Range(1.0, 10.0)) = 4.0
        _BaseDepthThreshold ("Base Depth Threshold", Range(0.001, 0.1)) = 0.015
        _BaseColorThreshold ("Base Color Threshold", Range(0.05, 1.0)) = 0.35
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            Name "GeneralDenoiserPass"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 4.5
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            // Uniforms mapped securely from our URP Render Feature properties
            float _MinRadius;
            float _MaxRadius;
            float _NoiseSensitivity;
            float _BaseDepthThreshold;
            float _BaseColorThreshold;

            float4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float4 centerColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                float centerDepth = SampleSceneDepth(uv);
                float centerDepthLinear = Linear01Depth(centerDepth, _ZBufferParams);

                // 1. STATISTICAL GEOMETRY ANALYSIS
                float4 gatherDepth = GATHER_RED_TEXTURE2D_X(_CameraDepthTexture, sampler_PointClamp, uv);
                float minD = min(min(gatherDepth.x, gatherDepth.y), min(gatherDepth.z, gatherDepth.w));
                float maxD = max(max(gatherDepth.x, gatherDepth.y), max(gatherDepth.z, gatherDepth.w));
                float depthRange = abs(Linear01Depth(maxD, _ZBufferParams) - Linear01Depth(minD, _ZBufferParams));

                // 2. STATISTICAL COLOR VARIANCE RADAR
                float2 texelSizeBase = _BlitTexture_TexelSize.xy;
                float4 diagColors[4] = {
                    SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv + float2(-1, -1) * texelSizeBase),
                    SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv + float2( 1, -1) * texelSizeBase),
                    SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv + float2(-1,  1) * texelSizeBase),
                    SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv + float2( 1,  1) * texelSizeBase)
                };
                
                float colorVariance = 0.0;
                [unroll]
                for(int j = 0; j < 4; j++)
                {
                    colorVariance += distance(centerColor.rgb, diagColors[j].rgb);
                }
                colorVariance /= 4.0; 

                // 3. ADAPTIVE KERNEL RADIUS WITH RESOLUTION-INVARIANT SCALING
                float adaptiveRadius = lerp(_MinRadius, _MaxRadius, saturate(colorVariance * _NoiseSensitivity));
                
                // --- THE RESOLUTION SCALING ENGINE ---
                // Calculate scale factor relative to a 1080p target height reference block
                // _ScreenParams.y represents the active runtime window height in pixels
                float resolutionScale = _ScreenParams.y / 1080.0;
                
                // Multiply adaptive radius by our scaling factor before computing step metrics
                float2 texelSize = _BlitTexture_TexelSize.xy * (adaptiveRadius * resolutionScale); 

                float4 colorSum = centerColor;
                float weightSum = 1.0;

                float2 offsets[8] = {
                    float2(-1.0, -1.0), float2(1.0, -1.0), float2(-1.0, 1.0), float2(1.0, 1.0),
                    float2(-1.4,  0.0), float2(1.4,  0.0), float2( 0.0, -1.4), float2(0.0, 1.4)
                };

                float dynamicDepthThreshold = lerp(_BaseDepthThreshold, _BaseDepthThreshold * 0.2, saturate(depthRange * 30.0)) * lerp(0.5, 2.0, saturate(colorVariance * 3.0));
                float dynamicColorThreshold = lerp(_BaseColorThreshold, _BaseColorThreshold * 2.0, saturate(colorVariance * 5.0)); 

                bool centerIsSky = (abs(centerDepth - UNITY_RAW_FAR_CLIP_VALUE) < 0.00001);

                [unroll]
                for(int i = 0; i < 8; i++)
                {
                    float2 sampleUV = uv + offsets[i] * texelSize;
        
                    float sampleDepth = SampleSceneDepth(sampleUV);
                    float sampleDepthLinear = Linear01Depth(sampleDepth, _ZBufferParams);
                    float depthDiff = abs(centerDepthLinear - sampleDepthLinear);
        
                    float depthWeight = exp(- (depthDiff * depthDiff) / (dynamicDepthThreshold * dynamicDepthThreshold));
                    bool sampleIsSky = (abs(sampleDepth - UNITY_RAW_FAR_CLIP_VALUE) < 0.00001);

                    if (depthDiff >= dynamicDepthThreshold && !centerIsSky && sampleIsSky)
                    {
                        depthWeight = 0.35; 
                    }

                    float4 sampleColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, sampleUV);
                    float colorDiff = distance(centerColor.rgb, sampleColor.rgb);
                    float colorWeight = exp(- (colorDiff * colorDiff) / (dynamicColorThreshold * dynamicColorThreshold));

                    if (!centerIsSky && sampleIsSky)
                    {
                        colorWeight = 1.0; 
                    }

                    float spatialWeight = exp(-dot(offsets[i], offsets[i]) * 0.4);
                    float totalSampleWeight = depthWeight * colorWeight * spatialWeight;

                    colorSum += sampleColor * totalSampleWeight;
                    weightSum += totalSampleWeight;
                }

                return colorSum / weightSum;
            }
            ENDHLSL
        }
    }
}
