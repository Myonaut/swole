#ifndef SKINNED_MESH_DATA_INCLUDED
#define SKINNED_MESH_DATA_INCLUDED

struct SkinInfluence
{
	
    float4 indicesA;
    float4 indicesB;

    float4 weightsA;
    float4 weightsB;

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

struct Triangles32
{
    int4 trianglesA;
    int4 trianglesB;
    int4 trianglesC;
    int4 trianglesD;

    int4 trianglesE;
    int4 trianglesF;
    int4 trianglesG;
    int4 trianglesH;
};

struct WeightedVertexConnection
{
    int index;
    int triIndex;
    float weight;
};

struct BoundsMinMax
{
    float3 minPoint;
    float3 maxPoint;
};

// Rotate vector v by Euler angles (radians) in X, Y, Z order (apply X, then Y, then Z)
float3 RotateByEulerBase(float3 v, float cx, float sx, float cy, float sy, float cz, float sz)
{
    // Rotate around X
    float3 v1 = float3(v.x, v.y * cx - v.z * sx, v.y * sx + v.z * cx);
    // Rotate around Y
    float3 v2 = float3(v1.x * cy + v1.z * sy, v1.y, -v1.x * sy + v1.z * cy);
    // Rotate around Z
    float3 v3 = float3(v2.x * cz - v2.y * sz, v2.x * sz + v2.y * cz, v2.z);
    return v3;
}
// Rotate vector v by Euler angles (radians) in X, Y, Z order (apply X, then Y, then Z)
float3 RotateByEuler(float3 v, float3 eulerRad)
{
    float cx = cos(eulerRad.x);
    float sx = sin(eulerRad.x);
    float cy = cos(eulerRad.y);
    float sy = sin(eulerRad.y);
    float cz = cos(eulerRad.z);
    float sz = sin(eulerRad.z);

    return RotateByEulerBase(v, cx, sx, cy, sy, cz, sz); 
}

VertexData VD_RotateByEuler(VertexData A, float3 eulerRad)
{
    VertexData result;
    result.position = RotateByEuler(A.position, eulerRad);
    result.normal = RotateByEuler(A.normal, eulerRad);
    result.tangent.xyz = RotateByEuler(A.tangent.xyz, eulerRad);
    result.tangent.w = A.tangent.w;

    return result;
}
MeshVertexDelta MVD_RotateByEuler(MeshVertexDelta A, float3 eulerRad)
{
    MeshVertexDelta result;
    result.positionDelta = RotateByEuler(A.positionDelta, eulerRad);
    result.normalDelta = RotateByEuler(A.normalDelta, eulerRad);
    result.tangentDelta = RotateByEuler(A.tangentDelta, eulerRad);

    return result;
}

float LengthSqr(float3 v)
{
    return dot(v, v);
}

float Angle(float3 from, float3 to)
{
    float denominator = sqrt(LengthSqr(from) * LengthSqr(to));
    if (denominator < 1E-15)
        return 0.0;

    float dot_ = clamp(dot(from, to) / denominator, -1.0, 1.0);
    return acos(dot_);
}

float4 AngleAxis(float aAngle, float3 aAxis)
{
    aAxis = normalize(aAxis);
    float rad = aAngle * 0.5;
    aAxis *= sin(rad);
    return float4(aAxis.x, aAxis.y, aAxis.z, cos(rad));
}

float4 FromToRotation(float3 aFrom, float3 aTo)
{
    float3 axis = cross(aFrom, aTo);
    float angle = Angle(aFrom, aTo);
    return AngleAxis(angle, axis);
}

float3 Rotate(float4 q, float3 v)
{
    float3 float5 = 2 * cross(q.xyz, v);
    return v + q.w * float5 + cross(q.xyz, float5);
}

// Safe normalize with fallback to zero vector
float3 SafeNormalize(float3 v)
{
    float len = sqrt(dot(v, v));
    if (len > 1e-8)
        return v / len;
    return float3(0.0, 0.0, 0.0);
}

#endif