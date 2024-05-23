#ifndef TOON_LIGHTING
#define TOON_LIGHTING

#ifdef SHADERGRAPH_PREVIEW

void ToonBasicLitFragment_float(half4 inColor, float3 positionWS, float3 normalWS, out half4 outColor) 
{
    outColor = inColor;
}

void ToonBasicLitFragmentShadows_float(half4 inColor, float3 positionWS, float3 normalWS, out half4 outColor) 
{
    outColor = inColor;
}

#else

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"

            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _LIGHT_LAYERS
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _CLUSTERED_RENDERING

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON

half3 ComputeRadiance(Light light, half3 normalWS, half alpha)
{
    half NdotL = saturate(dot(normalWS, light.direction));
    half lightAttenuation = light.distanceAttenuation;

    lightAttenuation *= light.shadowAttenuation * NdotL;

    half3 radiance = light.color * lightAttenuation;
    return radiance;
}

void ToonBasicLitFragment_float(half4 inColor, float3 positionWS, float3 normalWS, out half4 outColor)
{

    half3 finalColor = inColor; 

    //half4 shadowMask = CalculateShadowMask(inputData);
    //float4 shadowCoord = TransformWorldToShadowCoord(positionWS);
    uint meshRenderingLayers = GetMeshRenderingLightLayer();
    //Light mainLight = GetMainLight(shadowCoord, positionWS, shadowMask);
    Light mainLight = GetMainLight();

    if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
    {
        finalColor *= ComputeRadiance(mainLight, normalWS, inColor.a);
    }

    uint pixelLightCount = GetAdditionalLightsCount();

    LIGHT_LOOP_BEGIN(pixelLightCount)
        //Light light = GetAdditionalLight(lightIndex, positionWS, shadowMask);
        Light light = GetAdditionalLight(lightIndex, positionWS);

        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
        {
            finalColor += ComputeRadiance(light, normalWS, inColor.a);
        }
    LIGHT_LOOP_END

    outColor = half4(finalColor, inColor.a);

}

void ToonBasicLitFragmentShadows_float(half4 inColor, float3 positionWS, float3 normalWS, out half4 outColor)
{

    half3 finalColor = inColor; 

    InputData inputData;
    inputData.shadowMask = half4(1, 1, 1, 1);
    half4 shadowMask = CalculateShadowMask(inputData);
    float4 shadowCoord = TransformWorldToShadowCoord(positionWS);
    uint meshRenderingLayers = GetMeshRenderingLightLayer();
    Light mainLight = GetMainLight(shadowCoord, positionWS, shadowMask);

    if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
    {
        finalColor *= ComputeRadiance(mainLight, normalWS, inColor.a);
    }

    uint pixelLightCount = GetAdditionalLightsCount();

    LIGHT_LOOP_BEGIN(pixelLightCount)
        Light light = GetAdditionalLight(lightIndex, positionWS, shadowMask);

        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
        {
            finalColor += ComputeRadiance(light, normalWS, inColor.a);
        }
    LIGHT_LOOP_END

    outColor = half4(finalColor, inColor.a);

}

#endif

#endif