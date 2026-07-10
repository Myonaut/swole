#ifndef CUSTOMIZABLE_GARMENT
#define CUSTOMIZABLE_GARMENT

#include "CustomizableCharacterMesh.hlsl"

struct VertexData
{
    float3 position;
    float3 normal;
    float4 tangent;
};

// MeshVertexDelta layout must match the C# struct in MorphUtils.cs
struct MeshVertexDelta
{
    float3 positionDelta;
    float3 normalDelta;
    float3 tangentDelta;
};

#ifdef SHADERGRAPH_PREVIEW

void SampleClothingStretch_float(int localID, int vertexIndex, int vertexCount, out float outStretch)
{
    outStretch = 1.0;
}

void SkinBoundGarmentPreCalculated_float(int localID, int rigID, int characterID, int vertexIndex, int vertexCount, float bustMix, float3 inPosition, float3 inNormal, float3 inTangent, out float3 outPosition, out float3 outNormal, out float3 outTangent, out float4 muscleData, out float4 fatData, out float alpha, out float midlineWeight)
{
    outPosition = inPosition;
    outNormal = inNormal;
    outTangent = inTangent;

    muscleData = float4(0,0, 0, 0);
    fatData = float4(0,0, 0, 0);
    midlineWeight = 0;
    
    alpha = 1.0;

}

void SampleRippage_float(int localID, int vertexIndex, int vertexCount, out float outRippage)
{
	outRippage = 0;
}

#else

StructuredBuffer<float3> _CharacterVertices;
int _CharacterVertexCount;

StructuredBuffer<MeshVertexDelta> _ClothingVertexDeltas; 
int _ClothingFlexShapeIndexInBufferStart;

StructuredBuffer<float> _ClothingStretch;  

StructuredBuffer<int4> _BindingIndices;
StructuredBuffer<float4> _BindingWeights;

void SampleClothingStretch_float(int localID, int vertexIndex, int vertexCount, out float outStretch)
{
    outStretch = _ClothingStretch[(localID * vertexCount) + vertexIndex];
}

void BuildBasePhysiqueData_float(int characterID, int characterVertexIndex, int characterVertexCount, out float4 muscleData, out float4 fatData, out float midlineWeight, out float2 maskingLR)
{
    float3 characterPosition = _CharacterVertices[characterVertexIndex];
    BuildPhysiqueDataPreCalculated_float(characterID, characterVertexIndex, characterVertexCount, float2(characterPosition.x + 0.5, 0), characterPosition, muscleData, fatData, midlineWeight, maskingLR);
}

void SkinBoundGarmentPreCalculated_float(int localID, int rigID, int characterID, int vertexIndex, int vertexCount, float bustMix, float3 inPosition, float3 inNormal, float3 inTangent, out float3 outPosition, out float3 outNormal, out float3 outTangent, out float4 muscleData, out float4 fatData, out float alpha, out float midlineWeight)
{
    outPosition = inPosition;
    outNormal = inNormal;
    outTangent = inTangent;
    
    alpha = 1.0;
    
    MeshVertexDelta baseDelta = _ClothingVertexDeltas[vertexIndex];
    
    int4 bindingIndices = _BindingIndices[vertexIndex];
    float4 bindingWeights = _BindingWeights[vertexIndex];
    
    float4 A_muscleData = float4(0,0,0,0);
    float4 A_fatData = float4(0, 0, 0, 0); 
    float A_midlineWeight = 0;
    float2 A_maskingLR = float2(0,0);
    if (bindingWeights.x > 0)
        BuildBasePhysiqueData_float(characterID, bindingIndices.x, _CharacterVertexCount, A_muscleData, A_fatData, A_midlineWeight, A_maskingLR);
    
    float4 B_muscleData = float4(0, 0, 0, 0);
    float4 B_fatData = float4(0, 0, 0, 0);
    float B_midlineWeight = 0;
    float2 B_maskingLR = float2(0, 0);
    if (bindingWeights.y > 0)
        BuildBasePhysiqueData_float(characterID, bindingIndices.y, _CharacterVertexCount, B_muscleData, B_fatData, B_midlineWeight, B_maskingLR);
    
    float4 C_muscleData = float4(0, 0, 0, 0);
    float4 C_fatData = float4(0, 0, 0, 0);
    float C_midlineWeight = 0;
    float2 C_maskingLR = float2(0, 0);
    if (bindingWeights.z > 0)
        BuildBasePhysiqueData_float(characterID, bindingIndices.z, _CharacterVertexCount, C_muscleData, C_fatData, C_midlineWeight, C_maskingLR);
    
    float4 D_muscleData = float4(0, 0, 0, 0);
    float4 D_fatData = float4(0, 0, 0, 0);
    float D_midlineWeight = 0;
    float2 D_maskingLR = float2(0, 0);
    if (bindingWeights.w > 0)
        BuildBasePhysiqueData_float(characterID, bindingIndices.w, _CharacterVertexCount, D_muscleData, D_fatData, D_midlineWeight, D_maskingLR);
    
    muscleData = A_muscleData * bindingWeights.x + B_muscleData * bindingWeights.y + C_muscleData * bindingWeights.z + D_muscleData * bindingWeights.w;
    fatData = A_fatData * bindingWeights.x + B_fatData * bindingWeights.y + C_fatData * bindingWeights.z + D_fatData * bindingWeights.w;
    midlineWeight = A_midlineWeight * bindingWeights.x + B_midlineWeight * bindingWeights.y + C_midlineWeight * bindingWeights.z + D_midlineWeight * bindingWeights.w;
    
    float A_bustFactor;
    float A_bustNerfFactor;
    CalculateBustFactors_float(vertexIndex, vertexCount, bustMix, A_bustFactor, A_bustNerfFactor);
    
    float B_bustFactor;
    float B_bustNerfFactor;
    CalculateBustFactors_float(vertexIndex, vertexCount, bustMix, B_bustFactor, B_bustNerfFactor);
    
    float C_bustFactor;
    float C_bustNerfFactor;
    CalculateBustFactors_float(vertexIndex, vertexCount, bustMix, C_bustFactor, C_bustNerfFactor);
    
    float D_bustFactor;
    float D_bustNerfFactor;
    CalculateBustFactors_float(vertexIndex, vertexCount, bustMix, D_bustFactor, D_bustNerfFactor);
    
    float bustFactor = A_bustFactor * bindingWeights.x + B_bustFactor * bindingWeights.y + C_bustFactor * bindingWeights.z + D_bustFactor * bindingWeights.w;
    float bustNerfFactor = A_bustNerfFactor * bindingWeights.x + B_bustNerfFactor * bindingWeights.y + C_bustNerfFactor * bindingWeights.z + D_bustNerfFactor * bindingWeights.w;
    
    float muscleNerf;
    float fatSat;
    CalculateMuscleShapeAffectors(muscleData, fatData, bustNerfFactor, muscleNerf, fatSat);
    

    float flexWeight = muscleData.y; 
    float flexMult = muscleNerf;
    int2 baseFlexShapeIndices = _MeshShapeIndices[_FlexShapeIndex];
    int flexShapeStartIndex = baseFlexShapeIndices.x;
    int flexShapeFrameCount = baseFlexShapeIndices.y;
    if (flexShapeFrameCount > 2)
    {
        int frameCountM1 = flexShapeFrameCount - 1;
        for (int shapeIndexA = 0; shapeIndexA < flexShapeFrameCount; shapeIndexA++)
        {
            int shapeFullIndexA = flexShapeStartIndex + shapeIndexA;
            float frameWeightA = _MeshShapeFrameWeights[shapeFullIndexA];

            int shapeIndexB = shapeIndexA + 1;
            int shapeFullIndexB = flexShapeStartIndex + shapeIndexB;
            float frameWeightB = _MeshShapeFrameWeights[shapeFullIndexB];

            float weightRange = frameWeightB - frameWeightA;

            float weightA = 0;
            float weightB = (flexWeight - frameWeightA) / weightRange;
            if (weightB < 0)
            {
                if (shapeIndexA == 0)
                {
                    if (frameWeightA != 0)
                    {
                        weightA = flexWeight / frameWeightA;
                        weightB = 0;
                    }
                    else
                    {
                        weightA = 1 + abs(flexWeight / weightRange);
                        weightB = 0;
                    }
                }
                else
                {
                    weightA = abs(weightB);
                    weightB = 0;
                }
            }
            else
            {
                weightA = 1 - weightB;
                if (weightA < 0 && shapeIndexB < frameCountM1)
                    continue;
                weightA = max(0, weightA);
            }

            int shapeStartIndex;
            shapeStartIndex = ((_ClothingFlexShapeIndexInBufferStart + shapeIndexA) * vertexCount) + vertexIndex;
            MeshVertexDelta deltaA = _ClothingVertexDeltas[shapeStartIndex];
            ApplyShapeDeltaRaw_float(deltaA.positionDelta - baseDelta.positionDelta, deltaA.normalDelta - baseDelta.normalDelta, deltaA.tangentDelta - baseDelta.tangentDelta, weightA, outPosition, outNormal, outTangent, outPosition, outNormal, outTangent);
            
            shapeStartIndex = ((_ClothingFlexShapeIndexInBufferStart + shapeIndexB) * vertexCount) + vertexIndex;
            MeshVertexDelta deltaB = _ClothingVertexDeltas[shapeStartIndex];
            ApplyShapeDeltaRaw_float(deltaB.positionDelta - baseDelta.positionDelta, deltaB.normalDelta - baseDelta.normalDelta, deltaB.tangentDelta - baseDelta.tangentDelta, weightB, outPosition, outNormal, outTangent, outPosition, outNormal, outTangent);
            
            break;
        }
    }
    else if (flexShapeFrameCount > 1)
    {
        int shapeIndexA = 0;
        int shapeFullIndexA = flexShapeStartIndex;
        float frameWeightA = _MeshShapeFrameWeights[shapeFullIndexA];

        int shapeIndexB = 1;
        int shapeFullIndexB = flexShapeStartIndex + shapeIndexB;
        float frameWeightB = _MeshShapeFrameWeights[shapeFullIndexB];

        float weightA = flexWeight / frameWeightA;
        float weightB = 0;
        if (weightA > 1)
        {
            weightA = flexWeight - frameWeightA;
            float range = frameWeightB - frameWeightA;
            weightB = weightA / range;
            weightA = max(0, 1 - weightB);
        }

        int shapeStartIndex;
        shapeStartIndex = ((_ClothingFlexShapeIndexInBufferStart + shapeIndexA) * vertexCount) + vertexIndex;
        MeshVertexDelta deltaA = _ClothingVertexDeltas[shapeStartIndex];
        ApplyShapeDeltaRaw_float(deltaA.positionDelta - baseDelta.positionDelta, deltaA.normalDelta - baseDelta.normalDelta, deltaA.tangentDelta - baseDelta.tangentDelta, weightA, outPosition, outNormal, outTangent, outPosition, outNormal, outTangent);
            
        shapeStartIndex = ((_ClothingFlexShapeIndexInBufferStart + shapeIndexB) * vertexCount) + vertexIndex;
        MeshVertexDelta deltaB = _ClothingVertexDeltas[shapeStartIndex];
        ApplyShapeDeltaRaw_float(deltaB.positionDelta - baseDelta.positionDelta, deltaB.normalDelta - baseDelta.normalDelta, deltaB.tangentDelta - baseDelta.tangentDelta, weightB, outPosition, outNormal, outTangent, outPosition, outNormal, outTangent);
    }
    else if (flexShapeFrameCount > 0)
    {
        float maxWeight = _MeshShapeFrameWeights[flexShapeStartIndex];
        int shapeStartIndex = (_ClothingFlexShapeIndexInBufferStart * vertexCount) + vertexIndex;
        MeshVertexDelta delta = _ClothingVertexDeltas[shapeStartIndex];
        ApplyShapeDeltaRaw_float(delta.positionDelta - baseDelta.positionDelta, delta.normalDelta - baseDelta.normalDelta, delta.tangentDelta - baseDelta.tangentDelta, flexWeight / maxWeight, outPosition, outNormal, outTangent, outPosition, outNormal, outTangent);
    }

    outPosition = lerp(inPosition, outPosition, flexMult);
    outNormal = lerp(inNormal, outNormal, flexMult);
    outTangent = lerp(inTangent, outTangent, flexMult);
    
    ApplyShapeDeltaRaw_float(baseDelta.positionDelta, baseDelta.normalDelta, baseDelta.tangentDelta, 1.0, outPosition, outNormal, outTangent, outPosition, outNormal, outTangent);
    
    SkinNoShapes_float(rigID, vertexIndex, outPosition, outNormal, outTangent, outPosition, outNormal, outTangent);
}

StructuredBuffer<float> _Rippage;

void SampleRippage_float(int localID, int vertexIndex, int vertexCount, out float outRippage)
{
	outRippage = _Rippage[(localID * vertexCount) + vertexIndex];
}

#endif
#endif