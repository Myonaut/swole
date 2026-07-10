#ifndef CUSTOM_SKINNING
#define CUSTOM_SKINNING

struct SkinInfluence
{
	
    float4 indicesA;
    float4 indicesB;

    float4 weightsA;
    float4 weightsB;

};

struct BlendShapeDelta
{

    float3 deltaVertex;
    float3 deltaNormal;
    float3 deltaTangent;

};

#ifdef SHADERGRAPH_PREVIEW

void SkinNoShapes_float(int instanceID, int vertexIndex, float3 inPosition, float3 inNormal, float3 inTangent, out float3 outPosition, out float3 outNormal, out float3 outTangent) 
{

	outPosition = inPosition;
	outNormal = inNormal;
	outTangent = inTangent;

}

void SkinAndShapes_float(int shapesID, int rigID, int vertexIndex, int vertexCount, float2 mainUV, float3 inPosition, float3 inNormal, float3 inTangent, out float3 outPosition, out float3 outNormal, out float3 outTangent)
{

	outPosition = inPosition;
	outNormal = inNormal;
	outTangent = inTangent;

}

void ApplySingleFrameShape_float(int shapeBaseIndex, float weight, int vertexIndex, int vertexCount, float3 inPosition, float3 inNormal, float3 inTangent, out float3 outPosition, out float3 outNormal, out float3 outTangent) 
{
	outPosition = inPosition;
	outNormal = inNormal;
	outTangent = inTangent;
}
void ApplySingleFrameShapeWithMultiplier_float(int shapeBaseIndex, float weight, float multiplier, int vertexIndex, int vertexCount, float3 inPosition, float3 inNormal, float3 inTangent, out float3 outPosition, out float3 outNormal, out float3 outTangent) 
{
	outPosition = inPosition;
	outNormal = inNormal;
	outTangent = inTangent;
}

void ApplyShapeDeltaRaw_float(float3 positionDelta, float3 normalDelta, float3 tangentDelta, float weight, float3 inPosition, float3 inNormal, float3 inTangent, out float3 outPosition, out float3 outNormal, out float3 outTangent)
{
	outPosition = inPosition;
	outNormal = inNormal;
	outTangent = inTangent;
}
void ApplyShapeDeltaProvided_float(BlendShapeDelta delta, float weight, float3 inPosition, float3 inNormal, float3 inTangent, out float3 outPosition, out float3 outNormal, out float3 outTangent)
{
	outPosition = inPosition;
	outNormal = inNormal;
	outTangent = inTangent;
}
void ApplyShapeDelta_float(int indexInBuffer, float weight, float3 inPosition, float3 inNormal, float3 inTangent, out float3 outPosition, out float3 outNormal, out float3 outTangent) 
{
	outPosition = inPosition;
	outNormal = inNormal;
	outTangent = inTangent;
}

void ApplyMultiShape_float(int shapeBaseIndex, float weight, float multiplier, int vertexIndex, int vertexCount, float3 inPosition, float3 inNormal, float3 inTangent, out float3 outPosition, out float3 outNormal, out float3 outTangent) 
{
	outPosition = inPosition;
	outNormal = inNormal;
	outTangent = inTangent;
}

#endif

#ifndef SHADERGRAPH_PREVIEW

uniform int _BoneCount;

uniform int2 _RangeStandaloneShapes; 

//#if defined(SHADER_API_D3D11) || defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES3) || defined(SHADER_API_VULKAN) || defined(SHADER_API_METAL) || defined(SHADER_API_PSSL)
StructuredBuffer<SkinInfluence> _SkinBindings;
StructuredBuffer<float4x4> _SkinningMatrices;

StructuredBuffer<BlendShapeDelta> _MeshShapeFrameDeltas;
StructuredBuffer<float> _MeshShapeFrameWeights;
StructuredBuffer<int2> _MeshShapeIndices;

StructuredBuffer<float> _ControlStandaloneShapes;
//#else
//#endif

void SkinNoShapesOutMatrix_float(int instanceID, int vertexIndex, float3 inPosition, float3 inNormal, float3 inTangent, out float3 outPosition, out float3 outNormal, out float3 outTangent, out float4x4 blendedMatrix)
{

	SkinInfluence si = _SkinBindings[vertexIndex];
	
	const int boneIndexOffset = instanceID * _BoneCount;
	blendedMatrix = 
	mul(_SkinningMatrices[boneIndexOffset + int(si.indicesA.x)], si.weightsA.x)
	+ mul(_SkinningMatrices[boneIndexOffset + int(si.indicesA.y)], si.weightsA.y)
	+ mul(_SkinningMatrices[boneIndexOffset + int(si.indicesA.z)], si.weightsA.z)
	+ mul(_SkinningMatrices[boneIndexOffset + int(si.indicesA.w)], si.weightsA.w)
	+ mul(_SkinningMatrices[boneIndexOffset + int(si.indicesB.x)], si.weightsB.x)
	+ mul(_SkinningMatrices[boneIndexOffset + int(si.indicesB.y)], si.weightsB.y)
	+ mul(_SkinningMatrices[boneIndexOffset + int(si.indicesB.z)], si.weightsB.z)
    + mul(_SkinningMatrices[boneIndexOffset + int(si.indicesB.w)], si.weightsB.w);

	outPosition = mul(blendedMatrix, float4(inPosition, 1)).xyz;
    outNormal = mul(blendedMatrix, float4(inNormal, 0));
    outTangent = mul(blendedMatrix, float4(inTangent, 0));

	// DEBUG
	//outPosition = inPosition;
	//outNormal = inNormal;
	//outTangent = inTangent;

}
void SkinNoShapes_float(int instanceID, int vertexIndex, float3 inPosition, float3 inNormal, float3 inTangent, out float3 outPosition, out float3 outNormal, out float3 outTangent) 
{
	float4x4 _discard;
	SkinNoShapesOutMatrix_float(instanceID, vertexIndex, inPosition, inNormal, inTangent, outPosition, outNormal, outTangent, _discard);
}

void ApplyShapeDeltaRaw_float(float3 positionDelta, float3 normalDelta, float3 tangentDelta, float weight, float3 inPosition, float3 inNormal, float3 inTangent, out float3 outPosition, out float3 outNormal, out float3 outTangent)
{
    float weight_clamped = saturate(weight);

    outPosition = inPosition + positionDelta * weight;
    outNormal = inNormal + normalDelta * weight_clamped;
    outTangent = inTangent + tangentDelta * weight_clamped;
}
void ApplyShapeDeltaProvided_float(BlendShapeDelta delta, float weight, float3 inPosition, float3 inNormal, float3 inTangent, out float3 outPosition, out float3 outNormal, out float3 outTangent)
{
    ApplyShapeDeltaRaw_float(delta.deltaVertex, delta.deltaNormal, delta.deltaTangent, weight, inPosition, inNormal, inTangent, outPosition, outNormal, outTangent);
}
void ApplyShapeDelta_float(int indexInBuffer, float weight, float3 inPosition, float3 inNormal, float3 inTangent, out float3 outPosition, out float3 outNormal, out float3 outTangent) 
{
	const BlendShapeDelta delta = _MeshShapeFrameDeltas[indexInBuffer];
    ApplyShapeDeltaProvided_float(delta, weight, inPosition, inNormal, inTangent, outPosition, outNormal, outTangent);
}

void ApplyMultiShape_float(int shapeBaseIndex, float weight, float multiplier, int vertexIndex, int vertexCount, float3 inPosition, float3 inNormal, float3 inTangent, out float3 outPosition, out float3 outNormal, out float3 outTangent) 
{
	int2 shapeIndices = _MeshShapeIndices[shapeBaseIndex];
	int multiShapeStartIndex = shapeIndices.x;
	int shapeCount = shapeIndices.y;

	outPosition = inPosition;
	outNormal = inNormal; 
	outTangent = inTangent;

	if (shapeCount > 2) 
	{
		int shapeCountM1 = shapeCount - 1;
		for(int shapeIndexA = 0; shapeIndexA < shapeCountM1; shapeIndexA++) 
		{
			int shapeFullIndexA = multiShapeStartIndex + shapeIndexA;
			float frameWeightA = _MeshShapeFrameWeights[shapeFullIndexA];

			int shapeIndexB = shapeIndexA + 1;
			int shapeFullIndexB = multiShapeStartIndex + shapeIndexB;
			float frameWeightB = _MeshShapeFrameWeights[shapeFullIndexB];

			float weightRange = frameWeightB - frameWeightA;

			float weightA = 0;
			float weightB = (weight - frameWeightA) / weightRange;
			if (weightB < 0) 
			{
				if (shapeIndexA == 0) 
				{
					if (frameWeightA != 0) 
					{
						weightA = weight / frameWeightA; 
						weightB = 0;
					} 
					else 
					{
						weightA = 1 + abs(weight / weightRange);
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
				if (weightA < 0 && shapeIndexB < shapeCountM1) continue;
				weightA = max(0, weightA);
			}

			int shapeStartIndex;
			shapeStartIndex = (shapeFullIndexA * vertexCount);
			ApplyShapeDelta_float(shapeStartIndex + vertexIndex, weightA, outPosition, outNormal, outTangent, outPosition, outNormal, outTangent); 
			shapeStartIndex = (shapeFullIndexB * vertexCount);
			ApplyShapeDelta_float(shapeStartIndex + vertexIndex, weightB, outPosition, outNormal, outTangent, outPosition, outNormal, outTangent); 

			break;
		}
	} 
	else if (shapeCount > 1)
	{
		int shapeFullIndexA = multiShapeStartIndex;
		float frameWeightA = _MeshShapeFrameWeights[shapeFullIndexA];

		int shapeIndexB = 1;
		int shapeFullIndexB = multiShapeStartIndex + shapeIndexB;
		float frameWeightB = _MeshShapeFrameWeights[shapeFullIndexB]; 

		float weightA = weight / frameWeightA;
		float weightB = 0;
		if (weightA > 1) 
		{
			weightA = weight - frameWeightA;
			float range = frameWeightB - frameWeightA;
			weightB = weightA / range;
			weightA = max(0, 1 - weightB);
		}

		int shapeStartIndex;
		shapeStartIndex = (shapeFullIndexA * vertexCount);
		ApplyShapeDelta_float(shapeStartIndex + vertexIndex, weightA, outPosition, outNormal, outTangent, outPosition, outNormal, outTangent); 
		shapeStartIndex = (shapeFullIndexB * vertexCount);
		ApplyShapeDelta_float(shapeStartIndex + vertexIndex, weightB, outPosition, outNormal, outTangent, outPosition, outNormal, outTangent); 
	}
	else if (shapeCount > 0)
	{
		int shapeStartIndex = multiShapeStartIndex * vertexCount;
		float maxWeight = _MeshShapeFrameWeights[multiShapeStartIndex];
		ApplyShapeDelta_float(shapeStartIndex + vertexIndex, weight / maxWeight, outPosition, outNormal, outTangent, outPosition, outNormal, outTangent); 
	}

	outPosition = lerp(inPosition, outPosition, multiplier);
	outNormal = lerp(inNormal, outNormal, multiplier);
	outTangent = lerp(inTangent, outTangent, multiplier);
}

void ApplySingleFrameShape_float(int shapeBaseIndex, float weight, int vertexIndex, int vertexCount, float3 inPosition, float3 inNormal, float3 inTangent, out float3 outPosition, out float3 outNormal, out float3 outTangent) 
{
	int2 shapeIndices = _MeshShapeIndices[shapeBaseIndex];
	int multiShapeStartIndex = shapeIndices.x;

	outPosition = inPosition;
	outNormal = inNormal; 
	outTangent = inTangent;

	int shapeStartIndex = multiShapeStartIndex * vertexCount;
	float maxWeight = _MeshShapeFrameWeights[multiShapeStartIndex];
	ApplyShapeDelta_float(shapeStartIndex + vertexIndex, weight / maxWeight, outPosition, outNormal, outTangent, outPosition, outNormal, outTangent); 
}
void ApplySingleFrameShapeWithMultiplier_float(int shapeBaseIndex, float weight, float multiplier, int vertexIndex, int vertexCount, float3 inPosition, float3 inNormal, float3 inTangent, out float3 outPosition, out float3 outNormal, out float3 outTangent) 
{
	ApplySingleFrameShape_float(shapeBaseIndex, weight, vertexIndex, vertexCount, inPosition, inNormal, inTangent, outPosition, outNormal, outTangent);

	outPosition = lerp(inPosition, outPosition, multiplier);
	outNormal = lerp(inNormal, outNormal, multiplier);
	outTangent = lerp(inTangent, outTangent, multiplier);
}

void ApplyStandaloneShapes_float(int instanceID, int vertexIndex, int vertexCount, float3 inPosition, float3 inNormal, float3 inTangent, out float3 outPosition, out float3 outNormal, out float3 outTangent)
{

	const int standaloneShapeCount = max(0, (_RangeStandaloneShapes.y - _RangeStandaloneShapes.x) + 1);
	const int indexOffset = instanceID * standaloneShapeCount;
	
	outPosition = inPosition;
	outNormal = inNormal;
	outTangent = inTangent;

	for(int a = 0; a < standaloneShapeCount; a++) 
	{
		int shapeIndex = _RangeStandaloneShapes.x + a;

		float shapeWeight = _ControlStandaloneShapes[a + indexOffset];

		ApplyMultiShape_float(shapeIndex, shapeWeight, 1, vertexIndex, vertexCount, outPosition, outNormal, outTangent, outPosition, outNormal, outTangent);
	}
}

void SkinAndShapes_float(int shapesID, int rigID, int vertexIndex, int vertexCount, float2 mainUV, float3 inPosition, float3 inNormal, float3 inTangent, out float3 outPosition, out float3 outNormal, out float3 outTangent)
{

	outPosition = inPosition;
	outNormal = inNormal;
	outTangent = inTangent;

	ApplyStandaloneShapes_float(shapesID, vertexIndex, vertexCount, outPosition, outNormal, outTangent, outPosition, outNormal, outTangent);

	outNormal = normalize(outNormal);
	outTangent = normalize(outTangent);

	SkinNoShapes_float(rigID, vertexIndex, outPosition, outNormal, outTangent, outPosition, outNormal, outTangent);

}


#endif
#endif