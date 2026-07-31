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

VertexData VD_Lerp(VertexData A, VertexData B, float t)
{
    VertexData result;
    result.position = lerp(A.position, B.position, t);
    result.normal = lerp(A.normal, B.normal, t);
    result.tangent = lerp(A.tangent, B.tangent, t);

    return result;
}
MeshVertexDelta MVD_Lerp(MeshVertexDelta A, MeshVertexDelta B, float t)
{
    MeshVertexDelta result;
    result.positionDelta = lerp(A.positionDelta, B.positionDelta, t);
    result.normalDelta = lerp(A.normalDelta, B.normalDelta, t);
    result.tangentDelta = lerp(A.tangentDelta, B.tangentDelta, t);

    return result;
}

VertexData VD_Mul(VertexData A, float scalar)
{
    VertexData result;
    result.position = A.position * scalar;
    result.normal = A.normal * scalar;
    result.tangent.xyz = A.tangent.xyz * scalar;
    result.tangent.w = A.tangent.w; // preserve tangent w (handedness)

    return result;
}
MeshVertexDelta MVD_Mul(MeshVertexDelta A, float scalar)
{
    MeshVertexDelta result;
    result.positionDelta = A.positionDelta * scalar;
    result.normalDelta = A.normalDelta * scalar;
    result.tangentDelta = A.tangentDelta * scalar;

    return result;
}
    
VertexData VD_Div(VertexData A, float scalar)
{
    VertexData result;
    result.position = A.position / scalar;
    result.normal = A.normal / scalar;
    result.tangent.xyz = A.tangent.xyz / scalar;
    result.tangent.w = A.tangent.w; // preserve tangent w (handedness)

    return result;
}
MeshVertexDelta MVD_Div(MeshVertexDelta A, float scalar)
{
    MeshVertexDelta result;
    result.positionDelta = A.positionDelta / scalar;
    result.normalDelta = A.normalDelta / scalar;
    result.tangentDelta.xyz = A.tangentDelta.xyz / scalar;

    return result;
}
    
VertexData VD_Add(VertexData A, VertexData B)
{
    VertexData result;
    result.position = A.position + B.position;
    result.normal = A.normal + B.normal;
    result.tangent.xyz = A.tangent.xyz + B.tangent.xyz;
    result.tangent.w = A.tangent.w; // preserve tangent w (handedness)

    return result;
}
MeshVertexDelta MVD_Add(MeshVertexDelta A, MeshVertexDelta B)
{
    MeshVertexDelta result;
    result.positionDelta = A.positionDelta + B.positionDelta;
    result.normalDelta = A.normalDelta + B.normalDelta;
    result.tangentDelta = A.tangentDelta + B.tangentDelta;

    return result;
}
    
VertexData VD_Subtract(VertexData A, VertexData B)
{
    VertexData result;
    result.position = A.position - B.position;
    result.normal = A.normal - B.normal;
    result.tangent.xyz = A.tangent.xyz - B.tangent.xyz;
    result.tangent.w = A.tangent.w; // preserve tangent w (handedness)

    return result;
}
MeshVertexDelta MVD_Subtract(MeshVertexDelta A, MeshVertexDelta B)
{
    MeshVertexDelta result;
    result.positionDelta = A.positionDelta - B.positionDelta;
    result.normalDelta = A.normalDelta - B.normalDelta;
    result.tangentDelta = A.tangentDelta - B.tangentDelta;

    return result;
}

VertexData VD_AddDelta(VertexData A, MeshVertexDelta B)
{
    VertexData result;
    result.position = A.position + B.positionDelta;
    result.normal = A.normal + B.normalDelta;
    result.tangent.xyz = A.tangent.xyz + B.tangentDelta;
    result.tangent.w = A.tangent.w;

    return result;
}
    
VertexData VD_SubtractDelta(VertexData A, MeshVertexDelta B)
{
    VertexData result;
    result.position = A.position - B.positionDelta;
    result.normal = A.normal - B.normalDelta;
    result.tangent.xyz = A.tangent.xyz - B.tangentDelta;
    result.tangent.w = A.tangent.w;

    return result;
}

struct VertexBinding
{
    int triangleIndex;
    float3 pad; // Matches the 3 padding floats on CPU
    
    float4 vertexIndices;
    float4 weights;
    float4 localOffset;
};

float3 SlerpNormals(float3 n1, float3 n2, float t)
{
    float dotp = clamp(dot(n1, n2), -1.0, 1.0);
    float theta = acos(dotp) * t;
    float3 relative = normalize(n2 - n1 * dotp);
    return n1 * cos(theta) + relative * sin(theta);
}
float3 SlerpNormalsShortestPath(float3 n1, float3 n2, float t)
{
    float dotp = dot(n1, n2);
    
    // Flip target vector if the angle is obtuse to ensure shortest path
    float3 target = n2;
    if (dotp < 0.0)
    {
        dotp = -dotp;
        target = -n2;
    }
    
    dotp = clamp(dotp, -1.0, 1.0);
    float theta = acos(dotp) * t;
    float3 relative = normalize(target - n1 * dotp);
    return n1 * cos(theta) + relative * sin(theta);
}

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

void ApplyFinalWorldDeltas_float(int instanceID, int vertexIndex, int vertexCount, float3 inPosition, float3 inNormal, float3 inTangent, out float3 outPosition, out float3 outNormal, out float3 outTangent) 
{

    outPosition = inPosition;
    outNormal = inNormal;
    outTangent = inTangent;

}

void SampleVertexMask_float(int instanceID, int vertexIndex, int vertexCount, out float mask)
{
	
    mask = 0;

}

#endif

#ifndef SHADERGRAPH_PREVIEW

uniform int _BoneCount;
uniform int _PrecalculatedSkinningVertexCount;

uniform int2 _RangeStandaloneShapes; 

//#if defined(SHADER_API_D3D11) || defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES3) || defined(SHADER_API_VULKAN) || defined(SHADER_API_METAL) || defined(SHADER_API_PSSL)
StructuredBuffer<SkinInfluence> _SkinBindings;
StructuredBuffer<float4x4> _SkinningMatrices;

StructuredBuffer<BlendShapeDelta> _MeshShapeFrameDeltas;
StructuredBuffer<float> _MeshShapeFrameWeights;
StructuredBuffer<int2> _MeshShapeIndices;

StructuredBuffer<float> _ControlStandaloneShapes;

StructuredBuffer<BlendShapeDelta> _FinalWorldDeltas;

StructuredBuffer<float> _VertexMask;

StructuredBuffer<VertexBinding> _MeshProxyBindings;
uniform int _MeshProxyVertexCount;
//#else
//#endif

void SkinNoShapesOutMatrix_float(int instanceID, int vertexIndex, float3 inPosition, float3 inNormal, float3 inTangent, out float3 outPosition, out float3 outNormal, out float3 outTangent, out float4x4 blendedMatrix)
{

	#ifdef USE_PRECALCULATED_SKINNING
	
	blendedMatrix = _SkinningMatrices[(instanceID * _PrecalculatedSkinningVertexCount) + vertexIndex];
	
	#else
	
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
	
	#endif

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

void ApplyFinalWorldDeltas_float(int instanceID, int vertexIndex, int vertexCount, float3 inPosition, float3 inNormal, float3 inTangent, out float3 outPosition, out float3 outNormal, out float3 outTangent) 
{

    outPosition = inPosition;
    outNormal = inNormal;
    outTangent = inTangent;

#ifdef USE_FINAL_WORLD_DELTAS
	BlendShapeDelta delta = _FinalWorldDeltas[instanceID * vertexCount + vertexIndex];
	outPosition += delta.deltaVertex;
	outNormal = normalize(outNormal + delta.deltaNormal);
	outTangent = normalize(outTangent + delta.deltaTangent);
#endif

}

void SampleVertexMask_float(int instanceID, int vertexIndex, int vertexCount, out float mask) 
{
	
    mask = 0; 

#ifdef USE_VERTEX_MASK
	mask = _VertexMask[instanceID * vertexCount + vertexIndex];
#endif

}

#endif
#endif