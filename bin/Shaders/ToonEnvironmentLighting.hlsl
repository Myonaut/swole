#ifndef TOON_LIGHTING
#define TOON_LIGHTING

#ifdef SHADERGRAPH_PREVIEW

void ToonEnvironmentLitFragmentPBR_float(half4 inColor, float3 positionWS, float3 normalWS, float4 tangentWS, float2 uv, float3 normalTS, float4 specGlossSample, float4 specColor, float smoothness, float metallic, float clearCoatMask, float clearCoatSmoothness, float fogFactor, out half4 outColor)
{
    outColor = inColor;
}

#else

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

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

// Copied from Lighting.hlsl (UniversalFragmentPBR)
half4 UniversalFragmentPBRCustom(InputData inputData, SurfaceData surfaceData)
{
    #if defined(_SPECULARHIGHLIGHTS_OFF)
    bool specularHighlightsOff = true;
    #else
    bool specularHighlightsOff = false;
    #endif
    BRDFData brdfData;

    // NOTE: can modify "surfaceData"...
    InitializeBRDFData(surfaceData, brdfData);

    // Clear-coat calculation...
    BRDFData brdfDataClearCoat = CreateClearCoatBRDFData(surfaceData, brdfData);
    half4 shadowMask = CalculateShadowMask(inputData);
    AmbientOcclusionFactor aoFactor = (AmbientOcclusionFactor)1;//CreateAmbientOcclusionFactor(inputData, surfaceData); // this is probably already done in the unlit shader graph? also doesn't work without correct inputData.normalizedScreenSpaceUV
    uint meshRenderingLayers = GetMeshRenderingLightLayer(); 
    Light mainLight = GetMainLight(inputData, shadowMask, aoFactor);

    // NOTE: We don't apply AO to the GI here because it's done in the lighting calculation below...
    MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI);

    LightingData lightingData = CreateLightingData(inputData, surfaceData);

    lightingData.giColor = GlobalIllumination(brdfData, brdfDataClearCoat, surfaceData.clearCoatMask,
                                              inputData.bakedGI, aoFactor.indirectAmbientOcclusion, inputData.positionWS,
                                              inputData.normalWS, inputData.viewDirectionWS);

    if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
    {
        lightingData.mainLightColor = LightingPhysicallyBased(brdfData, brdfDataClearCoat,
                                                              mainLight,
                                                              inputData.normalWS, inputData.viewDirectionWS,
                                                              surfaceData.clearCoatMask, specularHighlightsOff);
    }

    #if defined(_ADDITIONAL_LIGHTS)
    uint pixelLightCount = GetAdditionalLightsCount();

    #if USE_CLUSTERED_LIGHTING
    for (uint lightIndex = 0; lightIndex < min(_AdditionalLightsDirectionalCount, MAX_VISIBLE_LIGHTS); lightIndex++)
    {
        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);

        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
        {
            lightingData.additionalLightsColor += LightingPhysicallyBased(brdfData, brdfDataClearCoat, light,
                                                                          inputData.normalWS, inputData.viewDirectionWS,
                                                                          surfaceData.clearCoatMask, specularHighlightsOff);
        }
    }
    #endif

    LIGHT_LOOP_BEGIN(pixelLightCount)
        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);

        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
        {
            lightingData.additionalLightsColor += LightingPhysicallyBased(brdfData, brdfDataClearCoat, light,
                                                                          inputData.normalWS, inputData.viewDirectionWS,
                                                                          surfaceData.clearCoatMask, specularHighlightsOff);
        }
    LIGHT_LOOP_END
    #endif

    return CalculateFinalColor(lightingData, surfaceData.alpha);
}

// Found in Input.hlsl
// InputData
//    float3  positionWS;
//    float4  positionCS;
//    float3   normalWS;
//    half3   viewDirectionWS;
//    float4  shadowCoord;
//    half    fogCoord;
//    half3   vertexLighting;
//    half3   bakedGI;
//    float2  normalizedScreenSpaceUV;
//    half4   shadowMask;
//    half3x3 tangentToWorld;

// Copied from LitForwardPass.hlsl
void InitializeInputData(half3 positionWS, half3 normalWS, half4 tangentWS, half fogFactor, half3 normalTS, out InputData inputData)
{
    inputData = (InputData)0;

#if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
    inputData.positionWS = positionWS;
#endif

    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(positionWS); 
#if defined(_NORMALMAP) || defined(_DETAIL)
    float sgn = tangentWS.w;      // should be either +1 or -1
    float3 bitangent = sgn * cross(normalWS.xyz, tangentWS.xyz);
    half3x3 tangentToWorld = half3x3(tangentWS.xyz, bitangent.xyz, normalWS.xyz);

    #if defined(_NORMALMAP)
    inputData.tangentToWorld = tangentToWorld;
    #endif
    inputData.normalWS = TransformTangentToWorld(normalTS, tangentToWorld);
#else
    inputData.normalWS = normalWS;
#endif

    inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS); 
    inputData.viewDirectionWS = viewDirWS;

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    inputData.shadowCoord = TransformWorldToShadowCoord(positionWS);//input.shadowCoord;
#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
#else
    inputData.shadowCoord = float4(0, 0, 0, 0);
#endif

//#ifdef _ADDITIONAL_LIGHTS_VERTEX
//    inputData.fogCoord = InitializeInputDataFog(float4(positionWS, 1.0), input.fogFactorAndVertexLight.x);
//    inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
//#else
    inputData.fogCoord = InitializeInputDataFog(float4(positionWS, 1.0), fogFactor);
//#endif
 
    inputData.bakedGI = half3(1, 1, 1);

    //inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(positionCS); 
    inputData.shadowMask = half4(1, 1, 1, 1);//SAMPLE_SHADOWMASK(input.staticLightmapUV);
}

// Copied from LitInput.hlsl
// Returns clear coat parameters
// .x/.r == mask
// .y/.g == smoothness
half2 SampleClearCoatCustom(float mask, float smoothness)
{
#if defined(_CLEARCOAT) || defined(_CLEARCOATMAP)
    half2 clearCoatMaskSmoothness = half2(mask, smoothness);
    return clearCoatMaskSmoothness;
#else
    return half2(0.0, 1.0);
#endif  // _CLEARCOAT
}

// Copied from LitInput.hlsl
half4 SampleMetallicSpecGlossCustom(half4 specGloss, half4 specColor, half smoothness, half metallic, half albedoAlpha)
{

#ifdef _METALLICSPECGLOSSMAP
    #ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
        specGloss.a = albedoAlpha * smoothness;
    #else
        specGloss.a *= smoothness;
    #endif
#else // _METALLICSPECGLOSSMAP
    #if _SPECULAR_SETUP
        specGloss.rgb = specColor.rgb;
    #else
        specGloss.rgb = metallic.rrr;
    #endif

    #ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
        specGloss.a = albedoAlpha * smoothness;
    #else
        specGloss.a = smoothness;
    #endif
#endif

    return specGloss;
}

// Copied from LitInput.hlsl
inline void InitializeSurfaceData(float4 albedo, float3 normalTS, float4 specGlossSample, float4 specColor, float smoothness, float metallic, float clearCoatMask, float clearCoatSmoothness, out SurfaceData outSurfaceData)
{
    outSurfaceData = (SurfaceData)0;

    outSurfaceData.alpha = albedo.a;

    half4 specGloss = SampleMetallicSpecGlossCustom(specGlossSample, specColor, smoothness, metallic, albedo.a);
    outSurfaceData.albedo = albedo;

#if _SPECULAR_SETUP
    outSurfaceData.metallic = half(1.0);
    outSurfaceData.specular = specGloss.rgb;
#else
    outSurfaceData.metallic = specGloss.r;
    outSurfaceData.specular = half3(0.0, 0.0, 0.0);
#endif

    outSurfaceData.smoothness = specGloss.a;
    outSurfaceData.normalTS = normalTS;
    outSurfaceData.occlusion = half(1.0);

#if defined(_CLEARCOAT) || defined(_CLEARCOATMAP)
    half2 clearCoat = SampleClearCoatCustom(clearCoatMask, clearCoatSmoothness);
    outSurfaceData.clearCoatMask       = clearCoat.r;
    outSurfaceData.clearCoatSmoothness = clearCoat.g;
#else
    outSurfaceData.clearCoatMask       = half(0.0);
    outSurfaceData.clearCoatSmoothness = half(0.0); 
#endif

}

// Copied from LitForwardPass.hlsl (LitPassFragment)
void ToonEnvironmentLitFragmentPBR_float(half4 inColor, float3 positionWS, float3 normalWS, float4 tangentWS, float2 uv, float3 normalTS, float4 specGlossSample, float4 specColor, float smoothness, float metallic, float clearCoatMask, float clearCoatSmoothness, float fogFactor, out half4 outColor)
{

    SurfaceData surfaceData;
    InitializeSurfaceData(inColor, normalTS, specGlossSample, specColor, smoothness, metallic, clearCoatMask, clearCoatSmoothness, surfaceData);

    InputData inputData;
    InitializeInputData(positionWS, normalWS, tangentWS, fogFactor, surfaceData.normalTS, inputData); 

#ifdef _DBUFFER
    ApplyDecalToSurfaceData(input.positionCS, surfaceData, inputData);
#endif

    outColor = UniversalFragmentPBRCustom(inputData, surfaceData);

    outColor.rgb = MixFog(outColor.rgb, inputData.fogCoord); 
    //outColor.a = OutputAlpha(outColor.a, _Surface); 
}

#endif

void ToonEnvironmentLitFragmentPBR_float(half4 inColor, float3 positionWS, float3 normalWS, float4 tangentWS, float2 uv, float3 normalTS, float4 specGlossSample, float4 specColor, float smoothness, float metallic, float clearCoatMask, float clearCoatSmoothness, out half4 outColor)
{
    ToonEnvironmentLitFragmentPBR_float(inColor, positionWS, normalWS, tangentWS, uv, normalTS, specGlossSample, specColor, smoothness, metallic, clearCoatMask, clearCoatSmoothness, 1, outColor);
}

#endif