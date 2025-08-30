#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine;

using Unity.Mathematics;

namespace Swole.DataStructures
{

    [Serializable]
    public enum UVChannelURP
    {

        UV0, UV1, UV2, UV3

    }

    [Serializable, Flags]
    public enum RGBAChannel
    {

        None = 0, R = 1, G = 2, B = 4, A = 8

    }

    [Serializable, Flags]
    public enum XYZChannel
    {

        None = 0, X = 1, Y = 2, Z = 4

    }

    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct WeightIndexPair
    {

        public int index;

        public float weight;

        public WeightIndexPair(int index, float weight)
        {

            this.index = index;
            this.weight = weight;

        }

    }

    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct WeightIndexPair4
    {

        public int4 index;

        public float4 weight;

        public WeightIndexPair4(int4 index, float4 weight)
        {

            this.index = index;
            this.weight = weight;

        }

    }

    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct BlendShapeHeader
    {

        public int frameCount;
        [NonSerialized]
        public float weight;

    }

    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct BlendShapeDeltas
    {

        public float4 deltaVertex;
        public float4 deltaNormal;
        public float4 deltaTangent;

    }

    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct BoneWeight8
    {


        public BoneWeight8(int boneIndex0,
        int boneIndex1,
        int boneIndex2,
        int boneIndex3,
        int boneIndex4,
        int boneIndex5,
        int boneIndex6,
        int boneIndex7,
        float boneWeight0,
        float boneWeight1,
        float boneWeight2,
        float boneWeight3,
        float boneWeight4,
        float boneWeight5,
        float boneWeight6,
        float boneWeight7)
        {

            this.boneIndex0 = boneIndex0;
            this.boneIndex1 = boneIndex1;
            this.boneIndex2 = boneIndex2;
            this.boneIndex3 = boneIndex3;
            this.boneIndex4 = boneIndex4;
            this.boneIndex5 = boneIndex5;
            this.boneIndex6 = boneIndex6;
            this.boneIndex7 = boneIndex7;

            this.boneWeight0 = boneWeight0;
            this.boneWeight1 = boneWeight1;
            this.boneWeight2 = boneWeight2;
            this.boneWeight3 = boneWeight3;
            this.boneWeight4 = boneWeight4;
            this.boneWeight5 = boneWeight5;
            this.boneWeight6 = boneWeight6;
            this.boneWeight7 = boneWeight7;

        }

        public int boneIndex0;
        public int boneIndex1;
        public int boneIndex2;
        public int boneIndex3;
        public int boneIndex4;
        public int boneIndex5;
        public int boneIndex6;
        public int boneIndex7;

        public float boneWeight0;
        public float boneWeight1;
        public float boneWeight2;
        public float boneWeight3;
        public float boneWeight4;
        public float boneWeight5;
        public float boneWeight6;
        public float boneWeight7;

    }

    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct BoneWeight8Float
    {

        public static BoneWeight8Float Empty => new BoneWeight8Float() { indicesA = -1, indicesB = -1 };

        public BoneWeight8Float(int4 indicesA, int4 indicesB, float4 weightsA, float4 weightsB)
        {

            this.indicesA = indicesA;
            this.indicesB = indicesB;

            this.weightsA = weightsA;
            this.weightsB = weightsB;

        }

        public float4 indicesA;
        public float4 indicesB;

        public float4 weightsA;
        public float4 weightsB;

        public BoneWeight8Float Modify(int componentIndex, int boneIndex, float boneWeight)
        {

            BoneWeight8Float weights = this;

            if (componentIndex == 0)
            {

                var iA = weights.indicesA;
                var wA = weights.weightsA;

                iA.x = boneIndex;
                wA.x = boneWeight;

                weights.indicesA = iA;
                weights.weightsA = wA;

            }
            else if (componentIndex == 1)
            {

                var iA = weights.indicesA;
                var wA = weights.weightsA;

                iA.y = boneIndex;
                wA.y = boneWeight;

                weights.indicesA = iA;
                weights.weightsA = wA;

            }
            else if (componentIndex == 2)
            {

                var iA = weights.indicesA;
                var wA = weights.weightsA;

                iA.z = boneIndex;
                wA.z = boneWeight;

                weights.indicesA = iA;
                weights.weightsA = wA;

            }
            else if (componentIndex == 3)
            {

                var iA = weights.indicesA;
                var wA = weights.weightsA;

                iA.w = boneIndex;
                wA.w = boneWeight;

                weights.indicesA = iA;
                weights.weightsA = wA;

            }
            else if (componentIndex == 4)
            {

                var iB = weights.indicesB;
                var wB = weights.weightsB;

                iB.x = boneIndex;
                wB.x = boneWeight;

                weights.indicesB = iB;
                weights.weightsB = wB;

            }
            else if (componentIndex == 5)
            {

                var iB = weights.indicesB;
                var wB = weights.weightsB;

                iB.y = boneIndex;
                wB.y = boneWeight;

                weights.indicesB = iB;
                weights.weightsB = wB;

            }
            else if (componentIndex == 6)
            {

                var iB = weights.indicesB;
                var wB = weights.weightsB;

                iB.z = boneIndex;
                wB.z = boneWeight;

                weights.indicesB = iB;
                weights.weightsB = wB;

            }
            else if (componentIndex == 7)
            {

                var iB = weights.indicesB;
                var wB = weights.weightsB;

                iB.w = boneIndex;
                wB.w = boneWeight;

                weights.indicesB = iB;
                weights.weightsB = wB;

            }

            return weights;

        }

    }

    [System.Serializable]
    public struct VertexClone
    {

        public int first;

        public int[] indices;

        public VertexClone(int first, int[] indices)
        {
            this.first = first;
            this.indices = indices;
        }

        public Vector3 Average(BlendShape.FrameData data)
        {

            Vector3 val = data[first];

            if (indices != null && indices.Length > 0)
            {

                val = Vector3.zero;

                for (int a = 0; a < indices.Length; a++) val += data[indices[a]];

                val = val / indices.Length;

            }

            return val;

        }

        public Vector2 Average(Vector2[] data)
        {

            Vector2 val = data[first];

            if (indices != null && indices.Length > 0)
            {

                val = Vector3.zero;

                for (int a = 0; a < indices.Length; a++) val += data[indices[a]];

                val = val / indices.Length;

            }

            return val;

        }

        public Vector3 Average(Vector3[] data)
        {

            Vector3 val = data[first];

            if (indices != null && indices.Length > 0)
            {

                val = Vector3.zero;

                for (int a = 0; a < indices.Length; a++) val += data[indices[a]];

                val = val / indices.Length;

            }

            return val;

        }

        public Vector4 Average(Vector4[] data)
        {

            Vector4 val = data[first];

            if (indices != null && indices.Length > 0)
            {

                val = Vector4.zero;

                for (int a = 0; a < indices.Length; a++) val += data[indices[a]];

                val = val / indices.Length;

            }

            return val;

        }

        public Color Average(Color[] data)
        {

            Color val = data[first];

            if (indices != null && indices.Length > 0)
            {

                val = Color.clear;

                for (int a = 0; a < indices.Length; a++) val += data[indices[a]];

                val = val / indices.Length;

            }

            return val;

        }

        public float Average(float[] data)
        {

            float val = data[first];

            if (indices != null && indices.Length > 0)
            {

                val = 0;

                for (int a = 0; a < indices.Length; a++) val += data[indices[a]];

                val = val / indices.Length;

            }

            return val;

        }

        public float2 Average(float2[] data)
        {

            float2 val = data[first];

            if (indices != null && indices.Length > 0)
            {

                val = float2.zero;

                for (int a = 0; a < indices.Length; a++) val += data[indices[a]];

                val = val / indices.Length;

            }

            return val;

        }

        public float3 Average(float3[] data)
        {

            float3 val = data[first];

            if (indices != null && indices.Length > 0)
            {

                val = float3.zero;

                for (int a = 0; a < indices.Length; a++) val += data[indices[a]];

                val = val / indices.Length;

            }

            return val;

        }

        public float4 Average(float4[] data)
        {

            float4 val = data[first];

            if (indices != null && indices.Length > 0)
            {

                val = float4.zero;

                for (int a = 0; a < indices.Length; a++) val += data[indices[a]];

                val = val / indices.Length;

            }

            return val;

        }

    }

    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct TransformDataPair
    {
        public float3 position;
        public quaternion rotation;

        public TransformDataPair(Transform transform, bool worldSpace = false)
        {
            if (worldSpace)
            {
                transform.GetPositionAndRotation(out var position, out var rotation);
                this.position = position;
                this.rotation = rotation;
            } 
            else
            {
                transform.GetLocalPositionAndRotation(out var position, out var rotation);
                this.position = position;
                this.rotation = rotation;
            }
        }

        public void Apply(Transform t)
        {
            t.SetPositionAndRotation(position, rotation); 
        }
        public void Apply(Transform t, Transform parent)
        {
            t.SetPositionAndRotation(parent.TransformPoint(position), parent.rotation * rotation);
        }
    }

    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct TransformDataState
    {
        public float3 position;
        public quaternion rotation;

        public TransformDataState(Transform transform, bool worldSpace = false)
        {
            if (worldSpace)
            {
                transform.GetPositionAndRotation(out var position, out var rotation);
                this.position = position;
                this.rotation = rotation;
            }
            else
            {
                transform.GetLocalPositionAndRotation(out var position, out var rotation);
                this.position = position;
                this.rotation = rotation;
            }
        }
    }
    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct TransformDataState2
    {
        public float3 position;
        public float3 localScale;
        public quaternion rotation;

        public TransformDataState2(Transform transform, bool worldSpace = false)
        {
            if (worldSpace)
            {
                transform.GetPositionAndRotation(out var position, out var rotation);
                this.position = position;
                this.rotation = rotation;
            }
            else
            {
                transform.GetLocalPositionAndRotation(out var position, out var rotation);
                this.position = position;
                this.rotation = rotation;
            }
            this.localScale = transform.localScale;
        }
    }
    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct TransformDataStateTemporal
    {
        public float3 position;
        public quaternion rotation;

        public float3 prevPosition;
        public quaternion prevRotation;

        public TransformDataStateTemporal(Transform transform, bool worldSpace = false)
        {
            if (worldSpace)
            {
                transform.GetPositionAndRotation(out var position, out var rotation);
                prevPosition = this.position = position;
                prevRotation = this.rotation = rotation;
            }
            else
            {
                transform.GetLocalPositionAndRotation(out var position, out var rotation);
                prevPosition = this.position = position;
                prevRotation = this.rotation = rotation;
            }
        }
    }
    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct TransformDataWorldLocal
    {
        public float3 position;
        public quaternion rotation;

        public float3 localPosition;
        public quaternion localRotation;

        public TransformDataWorldLocal(Transform transform)
        {
            transform.GetPositionAndRotation(out var position, out var rotation);
            this.position = position;
            this.rotation = rotation;

            transform.GetLocalPositionAndRotation(out var lpos, out var lrot);
            this.localPosition = lpos;
            this.localRotation = lrot;
        }
    }
    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct TransformDataWorldLocalAndMatrix
    {
        public float4x4 toWorld;
        public float3 position;
        public quaternion rotation;

        public float3 localPosition;
        public quaternion localRotation;

        public TransformDataWorldLocalAndMatrix(Transform transform)
        {
            toWorld = transform.localToWorldMatrix;

            transform.GetPositionAndRotation(out var position, out var rotation);
            this.position = position;
            this.rotation = rotation;

            transform.GetLocalPositionAndRotation(out var lpos, out var lrot);
            this.localPosition = lpos;
            this.localRotation = lrot;
        }
    }

    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct ParentedTransformDataPair
    {
        public float4x4 localToWorld;

        public float3 localPosition;
        public quaternion localRotation;

        public ParentedTransformDataPair(Transform transform)
        {
            localToWorld = transform.localToWorldMatrix;

            transform.GetLocalPositionAndRotation(out var lpos, out var lrot);
            this.localPosition = lpos;
            this.localRotation = lrot;
        }
    }

}

#endif