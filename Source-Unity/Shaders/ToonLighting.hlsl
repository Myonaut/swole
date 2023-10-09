#ifndef TOON_LIGHTING
#define TOON_LIGHTING

half3 ComputeRadiance(Light light, half3 normalWS, half alpha)
{
    half NdotL = saturate(dot(normalWS, light.direction));
    half lightAttenuation = light.distanceAttenuation;

    lightAttenuation *= light.shadowAttenuation * NdotL;

    half3 radiance = light.color * lightAttenuation;
    return radiance;
}

#ifdef SHADERGRAPH_PREVIEW

void ToonBasicLitFragment_float(half4 inColor, float3 positionWS, float3 normalWS, out half4 outColor) 
{
    
    outColor = inColor;

}

#else

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"

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

#endif

#endif