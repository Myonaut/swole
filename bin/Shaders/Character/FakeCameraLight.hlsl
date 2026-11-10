#ifndef FAKE_CAMERA_LIGHT_INCLUDED
#define FAKE_CAMERA_LIGHT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

// --- Unity 6 URP Global Uniforms ---
// Declared globally. Do NOT put these inside a Material CBUFFER.
float4 _GlobalFakeLightColorIntensity; // RGB = Color, A = Intensity
float4 _GlobalFakeLightOffsetVS; // X, Y, Z = View Space Offset Offset

// Main function to calculate the light properties
Light GetFakeCameraPointLight(float3 worldPosition, float3 cameraWorldPosition)
{
    Light lightOut;

    // 1. Extract Camera vectors directly from URP's current View Matrix
    float3 camRight = float3(unity_MatrixV[0].xyz);
    float3 camUp = float3(unity_MatrixV[1].xyz);
    float3 camForward = float3(unity_MatrixV[2].xyz);
    
    // 2. Compute the offset position in world space
    float3 lightWorldPos = cameraWorldPosition
                         + camRight * _GlobalFakeLightOffsetVS.x 
                         + camUp * _GlobalFakeLightOffsetVS.y 
                         + camForward * _GlobalFakeLightOffsetVS.z;

    // 3. Populate native URP Light struct fields
    lightOut.direction = normalize(lightWorldPos - worldPosition);
    lightOut.color = _GlobalFakeLightColorIntensity.rgb * _GlobalFakeLightColorIntensity.a;
    
    // 4. Force maximum attenuation scalars (Removes distance falloff & shadows)
    lightOut.distanceAttenuation = 1.0f;
    lightOut.shadowAttenuation = 1.0f;
    
    // 5. Default layer mask to match everything
    lightOut.layerMask = 0xFFFFFFFF;

    return lightOut;
}

#endif // FAKE_CAMERA_LIGHT_INCLUDED