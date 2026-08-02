Shader "Custom/StochasticTransparency"
{
    Properties
    {
        _MainTex ("Hair Texture", 2D) = "white" {}
        _BaseColor ("Color Tint", Color) = (1,1,1,1)
        _GlobalClipTarget ("Baseline Alpha Clip", Range(0,1)) = 0.1
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry+1" }
        Cull Off
        
        // ========================================================
        // GLOBAL SKINNING DEFINITION BLOCK
        // ========================================================
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        /*#include "Assets/SwoleScript/bin/Shaders/Character/CustomizableCharacterGarment.hlsl"

        // Global properties provided by your C# custom skinning instance
        int _RigInstanceID;
        int _CharacterInstanceID;
        int _VertexCount;
        float _BustMix;*/

        // Helper function to skin a single vertex into world space safely across passes
        float3 SkinVertexToWorldSpace(float3 posOS, float4 vertexColor)
        {
            // --- TEMPORARY UN-SKINNED BYPASS ---
            // Transforms raw object vertices directly to world space, 
            // ignoring the character rig properties entirely.
            return TransformObjectToWorld(posOS); 

            // Extract the vertex index out of the color's red channel (mapped 0-1 to index count)
            /*int vertexIndex = (int)vertexColor.r;

            float3 outPosWS, outNormalWS, outTangentWS; 
            float4 discardMuscle, discardFat;
            float discardAlpha, discardMidline;

            SkinBoundGarmentPreCalculated_float(
                0, // localID
                _RigInstanceID, 
                _CharacterInstanceID, 
                vertexIndex, 
                _VertexCount, 
                _BustMix, 
                1.0, // normalBlend
                0.0, // tangentBlend
                posOS, 
                float3(0,1,0), // dummy normal input since it's discarded/unused for clip-space position
                float3(1,0,0), // dummy tangent input
                outPosWS, outNormalWS, outTangentWS, 
                discardMuscle, discardFat, discardAlpha, discardMidline
            );

            return outPosWS;*/
        }
        ENDHLSL

        // ==========================================
        // PASS 0 (Index 0): TOTAL ALPHA GENERATION
        // ==========================================
        Pass
        {
            Name "TotalAlphaPass"
            Tags { "LightMode" = "StochasticAlpha" }
            Blend One OneMinusSrcAlpha // Standard transparent blend to accumulate alpha mathematically
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                float4 color        : COLOR;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
            };

            Texture2D _MainTex; SamplerState sampler_MainTex;

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = SkinVertexToWorldSpace(input.positionOS.xyz, input.color);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.uv = input.uv;
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float alpha = _MainTex.Sample(sampler_MainTex, input.uv).a;
                // Output ONLY to the Red channel of our R8 texture
                return float4(alpha, 0.0, 0.0, alpha);
            }
            ENDHLSL
        }

        // ==========================================
        // PASS 1 (Index 1): STOCHASTIC MSAA DEPTH WRITE
        // ==========================================
        Pass
        {
            Name "StochasticDepthPass"
            Tags { "LightMode" = "StochasticDepth" }
            ZWrite On 

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct Attributes 
            { 
                float4 positionOS : POSITION; 
                float2 uv         : TEXCOORD0; 
                float4 color      : COLOR; // Added for vertex indexing
            };
            
            struct Varyings 
            { 
                float4 positionCS : SV_POSITION; 
                float2 uv         : TEXCOORD0; 
                float3 positionWS : TEXCOORD1; // Changed from positionOS to positionWS
            };

            Texture2D _MainTex; SamplerState sampler_MainTex;
            float _GlobalClipTarget;

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                // 1. Calculate the skinned world space position
                float3 posWS = SkinVertexToWorldSpace(input.positionOS.xyz, input.color);
                
                // 2. Transform directly from World Space to Clip Space
                output.positionCS = TransformWorldToHClip(posWS);
                output.uv = input.uv;
                
                // 3. Pass the stable skinned world position to the fragment shader
                output.positionWS = posWS; 
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float alpha = _MainTex.Sample(sampler_MainTex, input.uv).a;
                
                // FIXED: Use input.positionWS.xy instead of input.positionOS.xy 
                // This keeps the noise perfectly "painted" onto your skinned mesh as it moves!
                float stratifiedNoise = frac(sin(dot(input.positionWS.xy, float2(12.9898, 78.233))) * 43758.5453);
                
                float threshold = lerp(stratifiedNoise, _GlobalClipTarget, alpha);
                if (alpha < threshold) discard;

                return float4(input.positionCS.z, 0.0, 0.0, 1.0);
            }
            ENDHLSL
        }

        // ==========================================
        // PASS 2 (Index 2): COLOR ACCUMULATION & CORRECTION
        // ==========================================
        Pass
        {
            Name "AccumulationPass"
            Tags { "LightMode" = "UniversalForward" } 
            Blend One One 
            //ZTest Always
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct Attributes 
            { 
                float4 positionOS : POSITION; 
                float2 uv         : TEXCOORD0; 
                float4 color      : COLOR; // Added for vertex indexing
            };
            
            struct Varyings 
            { 
                float4 positionCS : SV_POSITION; 
                float2 uv         : TEXCOORD0; 
                float4 screenPos  : TEXCOORD1; 
            };

            Texture2D _MainTex; SamplerState sampler_MainTex; float4 _BaseColor;
            Texture2D _ExactTotalAlphaTex;
            Texture2DMS<float, 8> _StochasticDepthMSAA; 

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                // 1. Calculate the skinned world space position
                float3 posWS = SkinVertexToWorldSpace(input.positionOS.xyz, input.color);
                
                // 2. Transform directly from World Space to Clip Space
                output.positionCS = TransformWorldToHClip(posWS);
                output.uv = input.uv;
                
                // 3. Feed the finalized clip position into the native screen-space calculator
                output.screenPos = ComputeScreenPos(output.positionCS);
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float4 texColor = _MainTex.Sample(sampler_MainTex, input.uv) * _BaseColor;
                float fragAlpha = texColor.a;

                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                int2 pixelCoords = int2(screenUV * _ScreenParams.xy);

                float visibleSamples = 0.0;
                float currentDepth = input.positionCS.z; 

                [unroll]
                for (int s = 0; s < 8; s++) 
                {
                    float sampleDepth = _StochasticDepthMSAA.Load(pixelCoords, s);
                    if (currentDepth >= sampleDepth)
                    {
                        visibleSamples += 1.0;
                    }
                }
                
                float visibilityOracle = visibleSamples / 8.0; 
                float exactTotalAlpha = _ExactTotalAlphaTex.Load(int3(pixelCoords, 0)).r;
                float correctionFactor = exactTotalAlpha / max(visibilityOracle, 0.001);

                // Keeping your neon green fallback active for the pipeline connection check!
                return float4(0.0, 1.0, 0.0, 1.0); 

                float3 finalColor = texColor.rgb * fragAlpha * visibilityOracle * correctionFactor; 
                return float4(finalColor, 1.0);
            }
            ENDHLSL
        }

    }
}