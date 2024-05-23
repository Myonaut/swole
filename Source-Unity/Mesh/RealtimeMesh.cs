#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole
{

    public static class RealtimeMesh
    {

        public static Matrix4x4 GetRootBoneMatrix(SkinnedMeshRenderer smr)
        {

            return GetBoneMatrix(smr, 0);

        }

        public static Matrix4x4 GetBoneMatrix(SkinnedMeshRenderer smr, int boneIndex)
        {

            if (smr == null) return Matrix4x4.identity;

            Transform[] bones = smr.bones;

            if (bones == null || bones.Length == 0 || smr.sharedMesh == null || boneIndex >= bones.Length) return Matrix4x4.identity;

            Matrix4x4[] bindposes = smr.sharedMesh.bindposes;

            if (bindposes == null || bindposes.Length == 0) return Matrix4x4.identity;

            /*Matrix4x4[] matrices = new Matrix4x4[bones.Length];

            for (int a = 0; a < matrices.Length; a++)
            {

                Transform bone = bones[a];

                matrices[a] = bone.localToWorldMatrix * bindposes[a];

            }*/

            Transform bone = bones[0];
            Matrix4x4 bind = bindposes[0];

            if (bone == null || bind == null) return Matrix4x4.identity;

            Matrix4x4 m0 = bone.localToWorldMatrix * bind;//matrices[0];
            Matrix4x4 m1 = bone.localToWorldMatrix * bind;//matrices[0];
            Matrix4x4 m2 = bone.localToWorldMatrix * bind;//matrices[0];
            Matrix4x4 m3 = bone.localToWorldMatrix * bind;//matrices[0];

            Matrix4x4 finalMatrix = Matrix4x4.identity;

            for (int n = 0; n < 16; n++)
            {
                m0[n] *= 1;
                m1[n] *= 0;
                m2[n] *= 0;
                m3[n] *= 0;

                finalMatrix[n] = m0[n] + m1[n] + m2[n] + m3[n];
            }

            return finalMatrix;

        }

        public static Vector3 GetTransformedVertexPosition(Matrix4x4[] matrices, Vector3 vertex, BoneWeight boneWeight, int boneCount)
        {

            if (boneWeight.boneIndex0 >= boneCount)
            {

                boneWeight.boneIndex0 = 0;

                boneWeight.weight0 = 0;

            }

            if (boneWeight.boneIndex1 >= boneCount)
            {

                boneWeight.boneIndex1 = 0;

                boneWeight.weight1 = 0;

            }

            if (boneWeight.boneIndex2 >= boneCount)
            {

                boneWeight.boneIndex2 = 0;

                boneWeight.weight2 = 0;

            }

            if (boneWeight.boneIndex3 >= boneCount)
            {

                boneWeight.boneIndex3 = 0;

                boneWeight.weight3 = 0;

            }

            Matrix4x4 m0 = matrices[Mathf.Max(0, boneWeight.boneIndex0)];
            Matrix4x4 m1 = matrices[Mathf.Max(0, boneWeight.boneIndex1)];
            Matrix4x4 m2 = matrices[Mathf.Max(0, boneWeight.boneIndex2)];
            Matrix4x4 m3 = matrices[Mathf.Max(0, boneWeight.boneIndex3)];

            Matrix4x4 finalMatrix = Matrix4x4.identity;

            for (int n = 0; n < 16; n++)
            {
                m0[n] *= boneWeight.weight0;
                m1[n] *= boneWeight.weight1;
                m2[n] *= boneWeight.weight2;
                m3[n] *= boneWeight.weight3;

                finalMatrix[n] = m0[n] + m1[n] + m2[n] + m3[n];
            }

            vertex = finalMatrix.MultiplyPoint3x4(vertex);

            return vertex;

        }

        public static Vector3 GetTransformedVertexPosition(BlendShape[] blendShapes, int vertexIndex, SkinnedMeshRenderer smr, Matrix4x4[] matrices, Vector3 vertex, BoneWeight boneWeight, int boneCount)
        {

            if (smr == null || smr.sharedMesh == null) return Vector3.zero;

            for (int a = 0; a < smr.sharedMesh.blendShapeCount; a++)
            {

                BlendShape blendShape = blendShapes[a];

                float weight = smr.GetBlendShapeWeight(a);

                vertex = blendShape.GetTransformedVertex(vertex, vertexIndex, weight);

            }

            return GetTransformedVertexPosition(matrices, vertex, boneWeight, boneCount);

        }

        public static Vector3 GetTransformedVertexPosition(BlendShape[] blendShapes, int vertexIndex, SkinnedMeshRenderer smr, Vector3 vertex)
        {

            if (smr == null || smr.sharedMesh == null) return Vector3.zero;

            for (int a = 0; a < smr.sharedMesh.blendShapeCount; a++)
            {

                BlendShape blendShape = blendShapes[a];

                float weight = smr.GetBlendShapeWeight(a);

                vertex = blendShape.GetTransformedVertex(vertex, vertexIndex, weight);

            }

            return vertex;

        }

        public static Vector3[] GetTransformedVertexPositions(SkinnedMeshRenderer smr, Vector3[] input_vertices = null)
        {

            if (smr == null || smr.sharedMesh == null) return new Vector3[0];

            Vector3[] vertices = input_vertices == null ? smr.sharedMesh.vertices : input_vertices;

            Transform[] bones = smr.bones;

            Vector3[] GetWorldVertices(Vector3[] array)
            {

                for (int a = 0; a < array.Length; a++) array[a] = smr.transform.TransformPoint(array[a]);

                return array;

            }

            if (bones == null || bones.Length == 0) return GetWorldVertices(vertices);

            Matrix4x4[] bindposes = smr.sharedMesh.bindposes;

            if (bindposes == null || bindposes.Length == 0) return GetWorldVertices(vertices);

            BoneWeight[] boneWeights = smr.sharedMesh.boneWeights;

            if (boneWeights == null || boneWeights.Length == 0) return GetWorldVertices(vertices);

            Matrix4x4[] matrices = new Matrix4x4[bones.Length];

            for (int a = 0; a < matrices.Length; a++)
            {

                Transform bone = bones[a];

                matrices[a] = bone.localToWorldMatrix * bindposes[a];

            }

            int boneCount = bones.Length;

            for (int a = 0; a < vertices.Length; a++)
            {

                BoneWeight bw = boneWeights[a];

                if (bw.boneIndex0 >= boneCount)
                {

                    bw.boneIndex0 = 0;

                    bw.weight0 = 0;

                }

                if (bw.boneIndex1 >= boneCount)
                {

                    bw.boneIndex1 = 0;

                    bw.weight1 = 0;

                }

                if (bw.boneIndex2 >= boneCount)
                {

                    bw.boneIndex2 = 0;

                    bw.weight2 = 0;

                }

                if (bw.boneIndex3 >= boneCount)
                {

                    bw.boneIndex3 = 0;

                    bw.weight3 = 0;

                }

                Matrix4x4 m0 = matrices[Mathf.Max(0, bw.boneIndex0)];
                Matrix4x4 m1 = matrices[Mathf.Max(0, bw.boneIndex1)];
                Matrix4x4 m2 = matrices[Mathf.Max(0, bw.boneIndex2)];
                Matrix4x4 m3 = matrices[Mathf.Max(0, bw.boneIndex3)];

                Matrix4x4 finalMatrix = Matrix4x4.identity;

                for (int n = 0; n < 16; n++)
                {
                    m0[n] *= bw.weight0;
                    m1[n] *= bw.weight1;
                    m2[n] *= bw.weight2;
                    m3[n] *= bw.weight3;

                    finalMatrix[n] = m0[n] + m1[n] + m2[n] + m3[n];
                }

                vertices[a] = finalMatrix.MultiplyPoint3x4(vertices[a]);

            }

            return vertices;

        }

        public static Vector3[] GetTransformedVertices(SkinnedMeshRenderer smr)
        {

            if (smr == null || smr.sharedMesh == null) return null;

            Vector3[] vertices = smr.sharedMesh.vertices;

            for (int a = 0; a < smr.sharedMesh.blendShapeCount; a++)
            {

                BlendShape blendShape = new BlendShape(smr.sharedMesh, smr.sharedMesh.GetBlendShapeName(a));

                float weight = smr.GetBlendShapeWeight(a);

                vertices = blendShape.GetTransformedVertices(vertices, weight);

            }

            return GetTransformedVertexPositions(smr, vertices);

        }

        public static Vector3[] GetTransformedVertices(SkinnedMeshRenderer smr, Vector3[] input_vertices = null, BlendShape[] blendShapes = null)
        {

            if (smr == null || smr.sharedMesh == null) return null;

            Vector3[] vertices = input_vertices == null ? smr.sharedMesh.vertices : (Vector3[])input_vertices.Clone();

            for (int a = 0; a < smr.sharedMesh.blendShapeCount; a++)
            {

                BlendShape blendShape = blendShapes == null ? new BlendShape(smr.sharedMesh, smr.sharedMesh.GetBlendShapeName(a)) : blendShapes[a];

                float weight = smr.GetBlendShapeWeight(a);

                vertices = blendShape.GetTransformedVertices(vertices, weight, false);

            }

            return vertices;

        }

        public static Vector3[] GetTransformedVertices(float[] blendShapeWeights, Vector3[] input_vertices, BlendShape[] blendShapes)
        {

            Vector3[] vertices = (Vector3[])input_vertices.Clone();

            for (int a = 0; a < blendShapeWeights.Length; a++)
            {

                float weight = blendShapeWeights[a];

                vertices = blendShapes[a].GetTransformedVertices(vertices, weight, false);

            }

            return vertices;

        }

        //

        public static Vector3 GetTransformedNormal(Matrix4x4[] matrices, Vector3 normal, BoneWeight boneWeight, int boneCount)
        {

            if (boneWeight.boneIndex0 >= boneCount)
            {

                boneWeight.boneIndex0 = 0;

                boneWeight.weight0 = 0;

            }

            if (boneWeight.boneIndex1 >= boneCount)
            {

                boneWeight.boneIndex1 = 0;

                boneWeight.weight1 = 0;

            }

            if (boneWeight.boneIndex2 >= boneCount)
            {

                boneWeight.boneIndex2 = 0;

                boneWeight.weight2 = 0;

            }

            if (boneWeight.boneIndex3 >= boneCount)
            {

                boneWeight.boneIndex3 = 0;

                boneWeight.weight3 = 0;

            }

            Matrix4x4 m0 = matrices[Mathf.Max(0, boneWeight.boneIndex0)];
            Matrix4x4 m1 = matrices[Mathf.Max(0, boneWeight.boneIndex1)];
            Matrix4x4 m2 = matrices[Mathf.Max(0, boneWeight.boneIndex2)];
            Matrix4x4 m3 = matrices[Mathf.Max(0, boneWeight.boneIndex3)];

            Matrix4x4 finalMatrix = Matrix4x4.identity;

            for (int n = 0; n < 16; n++)
            {
                m0[n] *= boneWeight.weight0;
                m1[n] *= boneWeight.weight1;
                m2[n] *= boneWeight.weight2;
                m3[n] *= boneWeight.weight3;

                finalMatrix[n] = m0[n] + m1[n] + m2[n] + m3[n];
            }

            normal = finalMatrix.MultiplyVector(normal);

            return normal;

        }

        public static Vector3 GetTransformedNormal(BlendShape[] blendShapes, int normalIndex, SkinnedMeshRenderer smr, Matrix4x4[] matrices, Vector3 normal, BoneWeight boneWeight, int boneCount)
        {

            if (smr == null || smr.sharedMesh == null) return Vector3.zero;

            for (int a = 0; a < smr.sharedMesh.blendShapeCount; a++)
            {

                BlendShape blendShape = blendShapes[a];

                float weight = smr.GetBlendShapeWeight(a);

                normal = blendShape.GetTransformedNormal(normal, normalIndex, weight);

            }

            return GetTransformedNormal(matrices, normal, boneWeight, boneCount);

        }

        public static Vector3 GetTransformedNormal(BlendShape[] blendShapes, int normalIndex, SkinnedMeshRenderer smr, Vector3 normal)
        {

            if (smr == null || smr.sharedMesh == null) return Vector3.zero;

            for (int a = 0; a < smr.sharedMesh.blendShapeCount; a++)
            {

                BlendShape blendShape = blendShapes[a];

                float weight = smr.GetBlendShapeWeight(a);

                normal = blendShape.GetTransformedNormal(normal, normalIndex, weight);

            }

            return normal;

        }

        public static Vector3[] GetTransformedNormals(SkinnedMeshRenderer smr, Vector3[] input_normals = null)
        {

            if (smr == null || smr.sharedMesh == null) return new Vector3[0];

            Vector3[] normals = input_normals == null ? smr.sharedMesh.normals : input_normals;

            Transform[] bones = smr.bones;

            if (bones == null || bones.Length == 0 || smr.sharedMesh == null) return normals;

            Matrix4x4[] bindposes = smr.sharedMesh.bindposes;

            if (bindposes == null || bindposes.Length == 0) return normals;

            BoneWeight[] boneWeights = smr.sharedMesh.boneWeights;

            if (boneWeights == null || boneWeights.Length == 0) return normals;

            Matrix4x4[] matrices = new Matrix4x4[bones.Length];

            for (int a = 0; a < matrices.Length; a++)
            {

                Transform bone = bones[a];

                matrices[a] = bone.localToWorldMatrix * bindposes[a];

            }

            int boneCount = bones.Length;

            for (int a = 0; a < normals.Length; a++)
            {

                BoneWeight bw = boneWeights[a];

                if (bw.boneIndex0 >= boneCount)
                {

                    bw.boneIndex0 = 0;

                    bw.weight0 = 0;

                }

                if (bw.boneIndex1 >= boneCount)
                {

                    bw.boneIndex1 = 0;

                    bw.weight1 = 0;

                }

                if (bw.boneIndex2 >= boneCount)
                {

                    bw.boneIndex2 = 0;

                    bw.weight2 = 0;

                }

                if (bw.boneIndex3 >= boneCount)
                {

                    bw.boneIndex3 = 0;

                    bw.weight3 = 0;

                }

                Matrix4x4 m0 = matrices[Mathf.Max(0, bw.boneIndex0)];
                Matrix4x4 m1 = matrices[Mathf.Max(0, bw.boneIndex1)];
                Matrix4x4 m2 = matrices[Mathf.Max(0, bw.boneIndex2)];
                Matrix4x4 m3 = matrices[Mathf.Max(0, bw.boneIndex3)];

                Matrix4x4 finalMatrix = Matrix4x4.identity;

                for (int n = 0; n < 16; n++)
                {
                    m0[n] *= bw.weight0;
                    m1[n] *= bw.weight1;
                    m2[n] *= bw.weight2;
                    m3[n] *= bw.weight3;

                    finalMatrix[n] = m0[n] + m1[n] + m2[n] + m3[n];
                }

                normals[a] = finalMatrix.MultiplyVector(normals[a]);

            }

            return normals;

        }

        public static Vector3[] GetTransformedNormals(SkinnedMeshRenderer smr)
        {

            if (smr == null || smr.sharedMesh == null) return null;

            Vector3[] normals = smr.sharedMesh.normals;

            for (int a = 0; a < smr.sharedMesh.blendShapeCount; a++)
            {

                BlendShape blendShape = new BlendShape(smr.sharedMesh, smr.sharedMesh.GetBlendShapeName(a));

                float weight = smr.GetBlendShapeWeight(a);

                normals = blendShape.GetTransformedNormals(normals, weight);

            }

            return GetTransformedNormals(smr, normals);

        }

        //

        public static Vector4 GetTransformedTangent(Matrix4x4[] matrices, Vector4 tangent, BoneWeight boneWeight, int boneCount)
        {

            if (boneWeight.boneIndex0 >= boneCount)
            {

                boneWeight.boneIndex0 = 0;

                boneWeight.weight0 = 0;

            }

            if (boneWeight.boneIndex1 >= boneCount)
            {

                boneWeight.boneIndex1 = 0;

                boneWeight.weight1 = 0;

            }

            if (boneWeight.boneIndex2 >= boneCount)
            {

                boneWeight.boneIndex2 = 0;

                boneWeight.weight2 = 0;

            }

            if (boneWeight.boneIndex3 >= boneCount)
            {

                boneWeight.boneIndex3 = 0;

                boneWeight.weight3 = 0;

            }

            Matrix4x4 m0 = matrices[Mathf.Max(0, boneWeight.boneIndex0)];
            Matrix4x4 m1 = matrices[Mathf.Max(0, boneWeight.boneIndex1)];
            Matrix4x4 m2 = matrices[Mathf.Max(0, boneWeight.boneIndex2)];
            Matrix4x4 m3 = matrices[Mathf.Max(0, boneWeight.boneIndex3)];

            Matrix4x4 finalMatrix = Matrix4x4.identity;

            for (int n = 0; n < 16; n++)
            {
                m0[n] *= boneWeight.weight0;
                m1[n] *= boneWeight.weight1;
                m2[n] *= boneWeight.weight2;
                m3[n] *= boneWeight.weight3;

                finalMatrix[n] = m0[n] + m1[n] + m2[n] + m3[n];
            }

            tangent = finalMatrix.MultiplyVector(tangent);

            return tangent;

        }

        public static Vector4 GetTransformedTangent(BlendShape[] blendShapes, int tangentIndex, SkinnedMeshRenderer smr, Matrix4x4[] matrices, Vector4 tangent, BoneWeight boneWeight, int boneCount)
        {

            if (smr == null || smr.sharedMesh == null) return Vector3.zero;

            for (int a = 0; a < smr.sharedMesh.blendShapeCount; a++)
            {

                BlendShape blendShape = blendShapes[a];

                float weight = smr.GetBlendShapeWeight(a);

                tangent = blendShape.GetTransformedTangent(tangent, tangentIndex, weight);

            }

            return GetTransformedTangent(matrices, tangent, boneWeight, boneCount);

        }

        public static Vector4 GetTransformedTangent(BlendShape[] blendShapes, int normalIndex, SkinnedMeshRenderer smr, Vector4 tangent)
        {

            if (smr == null || smr.sharedMesh == null) return Vector3.zero;

            for (int a = 0; a < smr.sharedMesh.blendShapeCount; a++)
            {

                BlendShape blendShape = blendShapes[a];

                float weight = smr.GetBlendShapeWeight(a);

                tangent = blendShape.GetTransformedTangent(tangent, normalIndex, weight);

            }

            return tangent;

        }

        public static Vector4[] GetTransformedTangents(SkinnedMeshRenderer smr, Vector4[] input_tangents = null)
        {

            if (smr == null || smr.sharedMesh == null) return new Vector4[0];

            Vector4[] tangents = input_tangents == null ? smr.sharedMesh.tangents : input_tangents;

            Transform[] bones = smr.bones;

            if (bones == null || bones.Length == 0 || smr.sharedMesh == null) return tangents;

            Matrix4x4[] bindposes = smr.sharedMesh.bindposes;

            if (bindposes == null || bindposes.Length == 0) return tangents;

            BoneWeight[] boneWeights = smr.sharedMesh.boneWeights;

            if (boneWeights == null || boneWeights.Length == 0) return tangents;

            Matrix4x4[] matrices = new Matrix4x4[bones.Length];

            for (int a = 0; a < matrices.Length; a++)
            {

                Transform bone = bones[a];

                matrices[a] = bone.localToWorldMatrix * bindposes[a];

            }

            int boneCount = bones.Length;

            for (int a = 0; a < tangents.Length; a++)
            {

                BoneWeight bw = boneWeights[a];

                if (bw.boneIndex0 >= boneCount)
                {

                    bw.boneIndex0 = 0;

                    bw.weight0 = 0;

                }

                if (bw.boneIndex1 >= boneCount)
                {

                    bw.boneIndex1 = 0;

                    bw.weight1 = 0;

                }

                if (bw.boneIndex2 >= boneCount)
                {

                    bw.boneIndex2 = 0;

                    bw.weight2 = 0;

                }

                if (bw.boneIndex3 >= boneCount)
                {

                    bw.boneIndex3 = 0;

                    bw.weight3 = 0;

                }

                Matrix4x4 m0 = matrices[Mathf.Max(0, bw.boneIndex0)];
                Matrix4x4 m1 = matrices[Mathf.Max(0, bw.boneIndex1)];
                Matrix4x4 m2 = matrices[Mathf.Max(0, bw.boneIndex2)];
                Matrix4x4 m3 = matrices[Mathf.Max(0, bw.boneIndex3)];

                Matrix4x4 finalMatrix = Matrix4x4.identity;

                for (int n = 0; n < 16; n++)
                {
                    m0[n] *= bw.weight0;
                    m1[n] *= bw.weight1;
                    m2[n] *= bw.weight2;
                    m3[n] *= bw.weight3;

                    finalMatrix[n] = m0[n] + m1[n] + m2[n] + m3[n];
                }

                tangents[a] = finalMatrix.MultiplyVector(tangents[a]);

            }

            return tangents;

        }

        public static Vector4[] GetTransformedTangents(SkinnedMeshRenderer smr)
        {

            if (smr == null || smr.sharedMesh == null) return null;

            Vector4[] tangents = smr.sharedMesh.tangents;

            for (int a = 0; a < smr.sharedMesh.blendShapeCount; a++)
            {

                BlendShape blendShape = new BlendShape(smr.sharedMesh, smr.sharedMesh.GetBlendShapeName(a));

                float weight = smr.GetBlendShapeWeight(a);

                tangents = blendShape.GetTransformedTangents(tangents, weight);

            }

            return GetTransformedTangents(smr, tangents);

        }

    }

}

#endif