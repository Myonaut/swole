Shader "Hidden/GaussianBlur"
{

    SubShader
    {
        Tags 
        { 
            "RenderType"="Opaque"
            "RenderPipeline"="UniversalPipeline"
        }

        HLSLINCLUDE

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        #define E 2.71828f

        uint _GridSize;
        float _Spread;
        
        float4 _BlitTexture_TexelSize;

        float gauss(int x)
        {
            float s = max(0.5f, _Spread);
            float sigmaSq = s * s;
            return (1.0f / sqrt(TWO_PI * sigmaSq)) * pow(E, -(x * x) / (2.0f * sigmaSq));
        }

        ENDHLSL

        Pass
        {
            Name "Horizontal"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag_hor

            float4 frag_hor(Varyings input) : SV_Target
			{
				float3 color = float3(0, 0, 0);
                float gridSum = 0.0f;

                int upper = ((_GridSize - 1) / 2);
                int lower = -upper;

				for (int x = lower; x <= upper; x++)
				{
                    float g = gauss(x);
                    gridSum += g;

                    float2 uv = input.texcoord + float2(_BlitTexture_TexelSize.x * x, 0.0f);
					color += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv).rgb * g;
				}

                color /= gridSum;

				return float4(color, 1.0f);
			}

            ENDHLSL
        }

        Pass
        {
            Name "Vertical"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag_ver

            float4 frag_ver(Varyings input) : SV_Target
			{

                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

				float3 color = float3(0, 0, 0);
                float gridSum = 0.0f;

                int upper = ((_GridSize - 1) / 2);
                int lower = -upper;

				for (int y = lower; y <= upper; y++)
				{
                    float g = gauss(y);
                    gridSum += g;

                    float2 uv = input.texcoord + float2(0.0f, _BlitTexture_TexelSize.y * y);
					color += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv).rgb * g;
				}

                color /= gridSum;

				return float4(color.rgb, 1.0f);
			}

            ENDHLSL
        }
    }
}
