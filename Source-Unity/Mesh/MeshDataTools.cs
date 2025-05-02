#if (UNITY_STANDALONE || UNITYEDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using Unity.Mathematics;
using Unity.Collections;

using UnityEngine;
using UnityEngine.Rendering;

namespace Swole
{
    public static class MeshDataTools
    {

        #region Tags

        public const string tag_Split = "_SPLIT";

        #endregion

        #region Misc

        public static Mesh Duplicate(this Mesh inputMesh) => UnityEngine.Object.Instantiate(inputMesh);

        /// <summary>
        /// Moves a chunk of vertices to a new index position in the mesh data arrays
        /// </summary>
        public static Mesh Rearrange(this Mesh inputMesh, int startIndex, int rearrangedVertexCount, int targetIndex)
        {
            if (rearrangedVertexCount <= 0) return inputMesh;

            targetIndex = Mathf.Clamp(targetIndex, 0, (inputMesh.vertexCount - rearrangedVertexCount));

            int GetRearrangedIndex(int originalIndex)
            {
                if (originalIndex < startIndex)
                {
                    if (originalIndex >= targetIndex)
                    {
                        return originalIndex + rearrangedVertexCount;
                    }
                    else return originalIndex;
                }
                else if (originalIndex >= startIndex + rearrangedVertexCount)
                {
                    if (originalIndex < targetIndex + rearrangedVertexCount)
                    {
                        return originalIndex - rearrangedVertexCount;
                    }
                    else return originalIndex;
                }

                return (originalIndex - startIndex) + targetIndex;
            }

            int[][] triangles = new int[inputMesh.subMeshCount][];
            for (int a = 0; a < inputMesh.subMeshCount; a++)
            {
                var tris = inputMesh.GetTriangles(a);
                for (int b = 0; b < tris.Length; b++) tris[b] = GetRearrangedIndex(tris[b]);
                triangles[a] = tris;
            }

            Vector3[] origVertices = null;
            if (inputMesh.HasVertexAttribute(VertexAttribute.Position)) origVertices = inputMesh.vertices;

            Vector3[] origNormals = null;
            if (inputMesh.HasVertexAttribute(VertexAttribute.Normal)) origNormals = inputMesh.normals;
            Vector4[] origTangents = null;
            if (inputMesh.HasVertexAttribute(VertexAttribute.Tangent)) origTangents = inputMesh.tangents;

            Color[] origColors = null;
            if (inputMesh.HasVertexAttribute(VertexAttribute.Color)) origColors = inputMesh.colors;

            Vector4[] origUV0 = null;
            if (inputMesh.HasVertexAttribute(VertexAttribute.TexCoord0)) origUV0 = inputMesh.GetUVsByChannelV4(0);
            Vector4[] origUV1 = null;
            if (inputMesh.HasVertexAttribute(VertexAttribute.TexCoord1)) origUV1 = inputMesh.GetUVsByChannelV4(1);
            Vector4[] origUV2 = null;
            if (inputMesh.HasVertexAttribute(VertexAttribute.TexCoord2)) origUV2 = inputMesh.GetUVsByChannelV4(2);
            Vector4[] origUV3 = null;
            if (inputMesh.HasVertexAttribute(VertexAttribute.TexCoord3)) origUV3 = inputMesh.GetUVsByChannelV4(3);
            Vector4[] origUV4 = null;
            if (inputMesh.HasVertexAttribute(VertexAttribute.TexCoord4)) origUV4 = inputMesh.GetUVsByChannelV4(4);
            Vector4[] origUV5 = null;
            if (inputMesh.HasVertexAttribute(VertexAttribute.TexCoord5)) origUV5 = inputMesh.GetUVsByChannelV4(5);
            Vector4[] origUV6 = null;
            if (inputMesh.HasVertexAttribute(VertexAttribute.TexCoord6)) origUV6 = inputMesh.GetUVsByChannelV4(6);
            Vector4[] origUV7 = null;
            if (inputMesh.HasVertexAttribute(VertexAttribute.TexCoord7)) origUV7 = inputMesh.GetUVsByChannelV4(7);

            NativeArray<BoneWeight1> origBoneWeights = default;
            NativeArray<byte> origBonesPerVertex = default;
            int[] boneWeightStartIndices = null;
            if (inputMesh.HasVertexAttribute(VertexAttribute.BlendWeight))
            {
                boneWeightStartIndices = new int[inputMesh.vertexCount];
                origBoneWeights = new NativeArray<BoneWeight1>(inputMesh.GetAllBoneWeights(), Allocator.Persistent);
                origBonesPerVertex = new NativeArray<byte>(inputMesh.GetBonesPerVertex(), Allocator.Persistent);
                int boneWeightStartIndex = 0;
                for (int a = 0; a < origBonesPerVertex.Length; a++)
                {
                    boneWeightStartIndices[a] = boneWeightStartIndex;
                    boneWeightStartIndex += origBonesPerVertex[a];
                }
            }

            var blendShapes = inputMesh.GetBlendShapes();

            for (int a = 0; a < inputMesh.subMeshCount; a++) inputMesh.SetTriangles(triangles[a], a);

            Array RearrangeMeshArray(Array originalArray, Array newArray)
            {
                int i = 0;
                for (int a = 0; a < startIndex; a++)
                {
                    if (i == targetIndex) i += rearrangedVertexCount;
                    newArray.SetValue(originalArray.GetValue(a), i);
                    i++;
                }
                for (int a = startIndex + rearrangedVertexCount; a < inputMesh.vertexCount; a++)
                {
                    if (i == targetIndex) i += rearrangedVertexCount;
                    newArray.SetValue(originalArray.GetValue(a), i);
                    i++;
                }

                for (int a = 0; a < rearrangedVertexCount; a++) newArray.SetValue(originalArray.GetValue(startIndex + a), targetIndex + a);

                return newArray;
            }

            if (origVertices != null) inputMesh.vertices = (Vector3[])RearrangeMeshArray(origVertices, new Vector3[origVertices.Length]);

            if (origNormals != null) inputMesh.normals = (Vector3[])RearrangeMeshArray(origNormals, new Vector3[origNormals.Length]);
            if (origTangents != null) inputMesh.tangents = (Vector4[])RearrangeMeshArray(origTangents, new Vector4[origTangents.Length]);

            if (origColors != null) inputMesh.colors = (Color[])RearrangeMeshArray(origColors, new Color[origColors.Length]);

            if (origUV0 != null) inputMesh.SetUVs(0, (Vector4[])RearrangeMeshArray(origUV0, new Vector4[origUV0.Length]));
            if (origUV1 != null) inputMesh.SetUVs(1, (Vector4[])RearrangeMeshArray(origUV1, new Vector4[origUV1.Length]));
            if (origUV2 != null) inputMesh.SetUVs(2, (Vector4[])RearrangeMeshArray(origUV2, new Vector4[origUV2.Length]));
            if (origUV3 != null) inputMesh.SetUVs(3, (Vector4[])RearrangeMeshArray(origUV3, new Vector4[origUV3.Length]));
            if (origUV4 != null) inputMesh.SetUVs(4, (Vector4[])RearrangeMeshArray(origUV4, new Vector4[origUV4.Length]));
            if (origUV5 != null) inputMesh.SetUVs(5, (Vector4[])RearrangeMeshArray(origUV5, new Vector4[origUV5.Length]));
            if (origUV6 != null) inputMesh.SetUVs(6, (Vector4[])RearrangeMeshArray(origUV6, new Vector4[origUV6.Length]));
            if (origUV7 != null) inputMesh.SetUVs(7, (Vector4[])RearrangeMeshArray(origUV7, new Vector4[origUV7.Length]));

            if (boneWeightStartIndices != null)
            {
                NativeArray<BoneWeight1> newBoneWeights = new NativeArray<BoneWeight1>(origBoneWeights.Length, Allocator.Persistent);
                NativeArray<byte> newBonesPerVertex = new NativeArray<byte>(origBonesPerVertex.Length, Allocator.Persistent);

                int i = 0;
                for (int a = 0; a < inputMesh.vertexCount; a++)
                {
                    byte boneCount = 0;
                    int origIndex;
                    if (a >= targetIndex && a < targetIndex + rearrangedVertexCount)
                    {
                        origIndex = startIndex + (a - targetIndex);
                    }
                    else
                    {
                        origIndex = a < targetIndex ? a : (a - rearrangedVertexCount);
                        if (origIndex >= startIndex) origIndex = origIndex + rearrangedVertexCount;
                    }

                    boneCount = origBonesPerVertex[origIndex];
                    newBonesPerVertex[a] = boneCount;
                    var boneWeightStartIndex = boneWeightStartIndices[origIndex];
                    for (int b = 0; b < boneCount; b++) newBoneWeights[i + b] = origBoneWeights[boneWeightStartIndex + b];

                    i += boneCount;
                }

                inputMesh.SetBoneWeights(newBonesPerVertex, newBoneWeights);

                origBoneWeights.Dispose();
                origBonesPerVertex.Dispose();
                newBoneWeights.Dispose();
                newBonesPerVertex.Dispose();
            }

            inputMesh.ClearBlendShapes();
            foreach (var blendShape in blendShapes)
            {
                for (int a = 0; a < blendShape.frames.Length; a++)
                {
                    var frame = blendShape.frames[a];
                    var deltaV = frame.deltaVertices.ToArray();
                    var deltaN = frame.deltaNormals.ToArray();
                    var deltaT = frame.deltaTangents.ToArray();
                    frame.deltaVertices.SetData((Vector3[])RearrangeMeshArray(deltaV, deltaV));
                    frame.deltaNormals.SetData((Vector3[])RearrangeMeshArray(deltaN, deltaN));
                    frame.deltaTangents.SetData((Vector3[])RearrangeMeshArray(deltaT, deltaT));
                }
                blendShape.AddToMesh(inputMesh);
            }

            return inputMesh;
        }

        /// <summary>
        /// Swaps the indices of a selection of vertices
        /// </summary>
        public static Mesh Rearrange(this Mesh inputMesh, ICollection<Vector2Int> indicesToSwap)
        {
            if (indicesToSwap.Count <= 0) return inputMesh;

            int GetRearrangedIndex(int originalIndex)
            {
                int index = originalIndex;
                foreach (var swap in indicesToSwap)
                {
                    if (swap.x == index)
                    {
                        index = swap.y;
                    }
                    else if (swap.y == index)
                    {
                        index = swap.x;
                    }
                }

                return index;
            }

            int[][] triangles = new int[inputMesh.subMeshCount][];
            for (int a = 0; a < inputMesh.subMeshCount; a++)
            {
                var tris = inputMesh.GetTriangles(a);
                for (int b = 0; b < tris.Length; b++) tris[b] = GetRearrangedIndex(tris[b]);
                triangles[a] = tris;
            }

            Vector3[] origVertices = null;
            if (inputMesh.HasVertexAttribute(VertexAttribute.Position)) origVertices = inputMesh.vertices;

            Vector3[] origNormals = null;
            if (inputMesh.HasVertexAttribute(VertexAttribute.Normal)) origNormals = inputMesh.normals;
            Vector4[] origTangents = null;
            if (inputMesh.HasVertexAttribute(VertexAttribute.Tangent)) origTangents = inputMesh.tangents;

            Color[] origColors = null;
            if (inputMesh.HasVertexAttribute(VertexAttribute.Color)) origColors = inputMesh.colors;

            Vector4[] origUV0 = null;
            if (inputMesh.HasVertexAttribute(VertexAttribute.TexCoord0)) origUV0 = inputMesh.GetUVsByChannelV4(0);
            Vector4[] origUV1 = null;
            if (inputMesh.HasVertexAttribute(VertexAttribute.TexCoord1)) origUV1 = inputMesh.GetUVsByChannelV4(1);
            Vector4[] origUV2 = null;
            if (inputMesh.HasVertexAttribute(VertexAttribute.TexCoord2)) origUV2 = inputMesh.GetUVsByChannelV4(2);
            Vector4[] origUV3 = null;
            if (inputMesh.HasVertexAttribute(VertexAttribute.TexCoord3)) origUV3 = inputMesh.GetUVsByChannelV4(3);
            Vector4[] origUV4 = null;
            if (inputMesh.HasVertexAttribute(VertexAttribute.TexCoord4)) origUV4 = inputMesh.GetUVsByChannelV4(4);
            Vector4[] origUV5 = null;
            if (inputMesh.HasVertexAttribute(VertexAttribute.TexCoord5)) origUV5 = inputMesh.GetUVsByChannelV4(5);
            Vector4[] origUV6 = null;
            if (inputMesh.HasVertexAttribute(VertexAttribute.TexCoord6)) origUV6 = inputMesh.GetUVsByChannelV4(6);
            Vector4[] origUV7 = null;
            if (inputMesh.HasVertexAttribute(VertexAttribute.TexCoord7)) origUV7 = inputMesh.GetUVsByChannelV4(7);

            NativeArray<BoneWeight1> origBoneWeights = default;
            NativeArray<byte> origBonesPerVertex = default;
            int[] boneWeightStartIndices = null;
            if (inputMesh.HasVertexAttribute(VertexAttribute.BlendWeight))
            {
                boneWeightStartIndices = new int[inputMesh.vertexCount];
                origBoneWeights = new NativeArray<BoneWeight1>(inputMesh.GetAllBoneWeights(), Allocator.Persistent);
                origBonesPerVertex = new NativeArray<byte>(inputMesh.GetBonesPerVertex(), Allocator.Persistent);
                int boneWeightStartIndex = 0;
                for (int a = 0; a < origBonesPerVertex.Length; a++)
                {
                    boneWeightStartIndices[a] = boneWeightStartIndex;
                    boneWeightStartIndex += origBonesPerVertex[a];
                }
            }

            var blendShapes = inputMesh.GetBlendShapes();

            for (int a = 0; a < inputMesh.subMeshCount; a++) inputMesh.SetTriangles(triangles[a], a);

            Array RearrangeMeshArray(Array originalArray, Array newArray)
            {
                if (!ReferenceEquals(originalArray, newArray)) Array.Copy(originalArray, newArray, originalArray.Length);
                foreach (var swap in indicesToSwap)
                {
                    var swapVal = newArray.GetValue(swap.y);
                    newArray.SetValue(newArray.GetValue(swap.x), swap.y);
                    newArray.SetValue(swapVal, swap.x);
                }

                return newArray;
            }

            if (origVertices != null) inputMesh.vertices = (Vector3[])RearrangeMeshArray(origVertices, new Vector3[origVertices.Length]);

            if (origNormals != null) inputMesh.normals = (Vector3[])RearrangeMeshArray(origNormals, new Vector3[origNormals.Length]);
            if (origTangents != null) inputMesh.tangents = (Vector4[])RearrangeMeshArray(origTangents, new Vector4[origTangents.Length]);

            if (origColors != null) inputMesh.colors = (Color[])RearrangeMeshArray(origColors, new Color[origColors.Length]);

            if (origUV0 != null) inputMesh.SetUVs(0, (Vector4[])RearrangeMeshArray(origUV0, new Vector4[origUV0.Length]));
            if (origUV1 != null) inputMesh.SetUVs(1, (Vector4[])RearrangeMeshArray(origUV1, new Vector4[origUV1.Length]));
            if (origUV2 != null) inputMesh.SetUVs(2, (Vector4[])RearrangeMeshArray(origUV2, new Vector4[origUV2.Length]));
            if (origUV3 != null) inputMesh.SetUVs(3, (Vector4[])RearrangeMeshArray(origUV3, new Vector4[origUV3.Length]));
            if (origUV4 != null) inputMesh.SetUVs(4, (Vector4[])RearrangeMeshArray(origUV4, new Vector4[origUV4.Length]));
            if (origUV5 != null) inputMesh.SetUVs(5, (Vector4[])RearrangeMeshArray(origUV5, new Vector4[origUV5.Length]));
            if (origUV6 != null) inputMesh.SetUVs(6, (Vector4[])RearrangeMeshArray(origUV6, new Vector4[origUV6.Length]));
            if (origUV7 != null) inputMesh.SetUVs(7, (Vector4[])RearrangeMeshArray(origUV7, new Vector4[origUV7.Length]));

            if (boneWeightStartIndices != null)
            {
                int[] indices = new int[inputMesh.vertexCount];
                for (int a = 0; a < indices.Length; a++) indices[a] = a;
                RearrangeMeshArray(indices, indices);

                NativeArray<BoneWeight1> newBoneWeights = new NativeArray<BoneWeight1>(origBoneWeights.Length, Allocator.Persistent);
                NativeArray<byte> newBonesPerVertex = new NativeArray<byte>(origBonesPerVertex.Length, Allocator.Persistent);

                int i = 0;
                for (int a = 0; a < indices.Length; a++)
                {
                    int dataIndex = indices[a];

                    byte boneCount = origBonesPerVertex[dataIndex];
                    newBonesPerVertex[a] = boneCount;
                    var boneWeightStartIndex = boneWeightStartIndices[dataIndex];
                    for (int b = 0; b < boneCount; b++) newBoneWeights[i + b] = origBoneWeights[boneWeightStartIndex + b];

                    i += boneCount;
                }

                inputMesh.SetBoneWeights(newBonesPerVertex, newBoneWeights);

                origBoneWeights.Dispose();
                origBonesPerVertex.Dispose();
                newBoneWeights.Dispose();
                newBonesPerVertex.Dispose();
            }

            inputMesh.ClearBlendShapes();
            foreach (var blendShape in blendShapes)
            {
                for (int a = 0; a < blendShape.frames.Length; a++)
                {
                    var frame = blendShape.frames[a];
                    var deltaV = frame.deltaVertices.ToArray();
                    var deltaN = frame.deltaNormals.ToArray();
                    var deltaT = frame.deltaTangents.ToArray();
                    frame.deltaVertices.SetData((Vector3[])RearrangeMeshArray(deltaV, deltaV));
                    frame.deltaNormals.SetData((Vector3[])RearrangeMeshArray(deltaN, deltaN));
                    frame.deltaTangents.SetData((Vector3[])RearrangeMeshArray(deltaT, deltaT));
                }
                blendShape.AddToMesh(inputMesh);
            }

            return inputMesh;
        }

        #endregion

        #region Vertex Merging

        [Serializable]
        public struct MergedVertex
        {
            public int firstIndex;

            /// <summary>
            /// Contains the indices of all merged vertices, including firstIndex
            /// </summary>
            public List<int> indices;

            public MergedVertex(int firstIndex, List<int> indices)
            {
                this.firstIndex = firstIndex;
                this.indices = indices;
            }

            public Vector3 Average(BlendShape.FrameData data)
            {
                Vector3 val = data[firstIndex];

                if (indices != null && indices.Count > 0)
                {
                    val = Vector3.zero;
                    for (int a = 0; a < indices.Count; a++) val += data[indices[a]];
                    val = val / indices.Count;
                }

                return val;
            }

            public Vector2 Average(Vector2[] data)
            {
                Vector2 val = data[firstIndex];

                if (indices != null && indices.Count > 0)
                {
                    val = Vector3.zero;
                    for (int a = 0; a < indices.Count; a++) val += data[indices[a]];
                    val = val / indices.Count;
                }

                return val;
            }

            public Vector3 Average(Vector3[] data)
            {
                Vector3 val = data[firstIndex];

                if (indices != null && indices.Count > 0)
                {
                    val = Vector3.zero;
                    for (int a = 0; a < indices.Count; a++) val += data[indices[a]];
                    val = val / indices.Count;
                }

                return val;
            }

            public Vector4 Average(Vector4[] data)
            {
                Vector4 val = data[firstIndex];

                if (indices != null && indices.Count > 0)
                {
                    val = Vector4.zero;
                    for (int a = 0; a < indices.Count; a++) val += data[indices[a]];
                    val = val / indices.Count;
                }

                return val;
            }

            public Color Average(Color[] data)
            {
                Color val = data[firstIndex];

                if (indices != null && indices.Count > 0)
                {
                    val = Color.clear;
                    for (int a = 0; a < indices.Count; a++) val += data[indices[a]];
                    val = val / indices.Count;
                }

                return val;
            }

            public float Average(float[] data)
            {
                float val = data[firstIndex];

                if (indices != null && indices.Count > 0)
                {
                    val = 0;
                    for (int a = 0; a < indices.Count; a++) val += data[indices[a]];
                    val = val / indices.Count;
                }

                return val;
            }
        }

        public static MergedVertex[] MergeVertices(ICollection<Vector3> vertices, float mergeThreshold = 0.00001f)
        {

            Dictionary<float, Dictionary<float, Dictionary<float, MergedVertex>>> sibling_stack = new Dictionary<float, Dictionary<float, Dictionary<float, MergedVertex>>>();

            MergedVertex[] clones = new MergedVertex[vertices.Count];

            double merge = 1D / mergeThreshold;

            int index = 0;
            foreach (Vector3 vert in vertices)
            {

                float px = (float)(System.Math.Truncate((double)vert.x * merge) / merge);
                float py = (float)(System.Math.Truncate((double)vert.y * merge) / merge);
                float pz = (float)(System.Math.Truncate((double)vert.z * merge) / merge);

                Dictionary<float, Dictionary<float, MergedVertex>> layer1;

                if (!sibling_stack.TryGetValue(px, out layer1))
                {
                    layer1 = new Dictionary<float, Dictionary<float, MergedVertex>>();
                    sibling_stack[px] = layer1;
                }

                Dictionary<float, MergedVertex> layer2;

                if (!layer1.TryGetValue(py, out layer2))
                {
                    layer2 = new Dictionary<float, MergedVertex>();
                    layer1[py] = layer2;
                }

                if (!layer2.TryGetValue(pz, out MergedVertex mergedVertex))
                {
                    mergedVertex = new MergedVertex(index, new List<int>());
                    layer2[pz] = mergedVertex;
                }

                mergedVertex.indices.Add(index);
                for (int b = 0; b < mergedVertex.indices.Count; b++) clones[mergedVertex.indices[b]] = mergedVertex;

                layer2[pz] = mergedVertex;

                index++;
            }

            return clones;

        }

        #endregion

        #region Vertex Connections

        public static int[][] GetVertexConnections(int vertexCount, int[] triangles, MergedVertex[] mergedVertices = null)
        {
            int[][] outputArray = new int[vertexCount][];
            void AddConnectedVertex(int rootVertex, int connectedVertex)
            {
                int[] originalArray, array;
                originalArray = array = outputArray[rootVertex];
                if (array == null) 
                { 
                    array = new int[1]; 
                }
                else
                {
                    for (int a = 0; a < originalArray.Length; a++) if (originalArray[a] == connectedVertex) return;
                    array = new int[array.Length + 1];
                    for (int a = 0; a < originalArray.Length; a++) array[a] = originalArray[a];
                }

                array[array.Length - 1] = connectedVertex;
            }
            void AddConnectedMergedVertex(MergedVertex rootVertex, MergedVertex connectedVertex)
            {
                if (rootVertex.indices != null && connectedVertex.indices != null)
                {
                    for(int a = 0; a < rootVertex.indices.Count; a++)
                    {
                        /*for (int b = 0; b < connectedVertex.indices.Count; b++)
                        {
                            AddConnectedVertex(rootVertex.indices[a], connectedVertex.indices[b]);
                        }*/
                        AddConnectedVertex(rootVertex.indices[a], connectedVertex.firstIndex);
                    }
                }
            }

            if (mergedVertices == null)
            {
                for (int a = 0; a < triangles.Length; a += 3)
                {
                    int v0 = triangles[a];
                    int v1 = triangles[a + 1];
                    int v2 = triangles[a + 2];

                    AddConnectedVertex(v0, v1);
                    AddConnectedVertex(v0, v2);

                    AddConnectedVertex(v1, v0);
                    AddConnectedVertex(v1, v2);

                    AddConnectedVertex(v2, v0);
                    AddConnectedVertex(v2, v1);
                }
            } 
            else
            {
                for (int a = 0; a < triangles.Length; a += 3)
                {
                    var v0 = mergedVertices[triangles[a]];
                    var v1 = mergedVertices[triangles[a + 1]];
                    var v2 = mergedVertices[triangles[a + 2]];

                    AddConnectedMergedVertex(v0, v1);
                    AddConnectedMergedVertex(v0, v2);

                    AddConnectedMergedVertex(v1, v0);
                    AddConnectedMergedVertex(v1, v2);

                    AddConnectedMergedVertex(v2, v0);
                    AddConnectedMergedVertex(v2, v1);
                }
            }

            return outputArray;
        }

        [Serializable]
        public struct WeightedVertexConnection
        {
            public int index;
            public float weight;
        }

        private static readonly List<int> _closedList = new List<int>();
        public static WeightedVertexConnection[][] GetDistanceWeightedVertexConnections(int[] triangles, Vector3[] vertices, MergedVertex[] mergedVertices = null)
        {
            int vertexCount = vertices.Length;

            WeightedVertexConnection[][] outputArray = new WeightedVertexConnection[vertexCount][];
            void AddConnectedVertex(int rootVertex, int connectedVertex, float distance = -1)
            {
                WeightedVertexConnection[] originalArray, array;
                originalArray = array = outputArray[rootVertex];
                if (array == null)
                {
                    array = new WeightedVertexConnection[1]; 
                }
                else
                {
                    for (int a = 0; a < originalArray.Length; a++) if (originalArray[a].index == connectedVertex) return;
                    array = new WeightedVertexConnection[array.Length + 1];
                    for (int a = 0; a < originalArray.Length; a++) array[a] = originalArray[a];
                }

                array[array.Length - 1] = new WeightedVertexConnection() { index = connectedVertex, weight = distance < 0 ? Vector3.Distance(vertices[rootVertex], vertices[connectedVertex]) : distance };
                outputArray[rootVertex] = array;
            }
            void AddConnectedMergedVertex(MergedVertex rootVertex, MergedVertex connectedVertex)
            {
                float distance = Vector3.Distance(vertices[rootVertex.firstIndex], vertices[connectedVertex.firstIndex]);
                AddConnectedVertex(rootVertex.firstIndex, connectedVertex.firstIndex, distance);              
            }

            if (mergedVertices == null)
            {
                for (int a = 0; a < triangles.Length; a += 3)
                {
                    int v0 = triangles[a];
                    int v1 = triangles[a + 1];
                    int v2 = triangles[a + 2];

                    AddConnectedVertex(v0, v1);
                    AddConnectedVertex(v0, v2);
                     
                    AddConnectedVertex(v1, v0);
                    AddConnectedVertex(v1, v2);

                    AddConnectedVertex(v2, v0);
                    AddConnectedVertex(v2, v1);
                }


                for (int a = 0; a < outputArray.Length; a++)
                {
                    float totalWeight = 0;

                    var array = outputArray[a];
                    for (int b = 0; b < array.Length; b++) totalWeight = totalWeight + array[b].weight;

                    if (totalWeight > 0)
                    {
                        for (int b = 0; b < array.Length; b++)
                        {
                            var data = array[b];
                            data.weight = data.weight / totalWeight;
                            array[b] = data;
                        }
                    }
                }
            }
            else
            {
                for (int a = 0; a < triangles.Length; a += 3)
                {
                    var v0 = mergedVertices[triangles[a]];
                    var v1 = mergedVertices[triangles[a + 1]];
                    var v2 = mergedVertices[triangles[a + 2]];

                    AddConnectedMergedVertex(v0, v1);
                    AddConnectedMergedVertex(v0, v2);

                    AddConnectedMergedVertex(v1, v0);
                    AddConnectedMergedVertex(v1, v2);

                    AddConnectedMergedVertex(v2, v0);
                    AddConnectedMergedVertex(v2, v1);
                }

                for (int a = 0; a < outputArray.Length; a++)
                {
                    float totalWeight = 0;

                    var merged = mergedVertices[a];
                    if (merged.firstIndex != a) continue;

                    var array = outputArray[merged.firstIndex];
                    for (int b = 0; b < array.Length; b++) 
                    {
                        totalWeight = totalWeight + array[b].weight; 
                    }

                    if (totalWeight > 0)
                    {
                        for (int b = 0; b < array.Length; b++)
                        {
                            var data = array[b];
                            data.weight = data.weight / totalWeight;  
                            array[b] = data;
                        }
                    }

                    if (merged.indices != null)
                    {
                        for(int b = 0;b < merged.indices.Count; b++)
                        {
                            int c = merged.indices[b];
                            outputArray[c] = array;
                        }
                    }
                }
            }
             
            return outputArray;
        }

        #endregion

        #region Triangles

        [Serializable]
        public enum TriangleOwnerIndex
        {
            A, B, C
        }
        [Serializable]
        public struct Triangle
        {
            public int i0, i1, i2;

            public TriangleOwnerIndex ownerIndex;
        }

        public static Triangle[][] GetTriangleReferencesPerVertex(int vertexCount, int[] triangles, MergedVertex[] mergedVertices = null)
        {
            Triangle[][] outputArray = new Triangle[vertexCount][];
            void AddTriangle(int rootVertex, int triangleIndex0, int triangleIndex1, int triangleIndex2)
            {
                Triangle[] originalArray, array;
                originalArray = array = outputArray[rootVertex];
                if (array == null)
                {
                    array = new Triangle[1];
                }
                else
                {
                    for (int a = 0; a < originalArray.Length; a++) if (originalArray[a].i0 == triangleIndex0 && originalArray[a].i1 == triangleIndex1 && originalArray[a].i2 == triangleIndex2) return;
                    array = new Triangle[array.Length + 1];
                    for (int a = 0; a < originalArray.Length; a++) array[a] = originalArray[a];
                }

                array[array.Length - 1] = new Triangle() { i0 = triangleIndex0, i1 = triangleIndex1, i2 = triangleIndex2, ownerIndex = rootVertex == triangleIndex0 ? TriangleOwnerIndex.A : rootVertex == triangleIndex1 ? TriangleOwnerIndex.B : TriangleOwnerIndex.C };
                outputArray[rootVertex] = array;
            }

            if (mergedVertices == null)
            {
                for (int a = 0; a < triangles.Length; a += 3)
                {
                    int v0 = triangles[a];
                    int v1 = triangles[a + 1];
                    int v2 = triangles[a + 2];

                    AddTriangle(v0, v0, v1, v2);
                    AddTriangle(v1, v0, v1, v2);
                    AddTriangle(v2, v0, v1, v2);
                }
            }
            else
            {
                for (int a = 0; a < triangles.Length; a += 3)
                {
                    var v0 = mergedVertices[triangles[a]].firstIndex;
                    var v1 = mergedVertices[triangles[a + 1]].firstIndex;
                    var v2 = mergedVertices[triangles[a + 2]].firstIndex;

                    AddTriangle(v0, v0, v1, v2);
                    AddTriangle(v1, v0, v1, v2);
                    AddTriangle(v2, v0, v1, v2);
                }

                for(int a = 0; a < mergedVertices.Length; a++)
                {
                    var mergedVertex = mergedVertices[a];
                    if (mergedVertex.firstIndex != a) continue;

                    var array = outputArray[mergedVertex.firstIndex];
                    if (mergedVertex.indices != null)
                    {
                        for (int b = 0; b < mergedVertex.indices.Count; b++) 
                        {
                            int c = mergedVertex.indices[b];
                            outputArray[c] = array;
                        }
                    }
                }
            }

            return outputArray;
        }

        #endregion

        #region Blend Shapes

        public static List<BlendShape> GetBlendShapes(this Mesh mesh, List<BlendShape> list = null)
        {
            if (list == null) list = new List<BlendShape>();

            for (int a = 0; a < mesh.blendShapeCount; a++) list.Add(new BlendShape(mesh, mesh.GetBlendShapeName(a)));
            return list;
        }

        public static BlendShape FindShape(string shapeName, ICollection<BlendShape> blendShapes)
        {
            foreach (var blendShape in blendShapes) if (blendShape.name == shapeName) return blendShape;
            return null;
        }
        public static bool TryFindShape(string shapeName, ICollection<BlendShape> blendShapes, out BlendShape result)
        {
            result = FindShape(shapeName, blendShapes);
            return result != null;
        }

        /// <summary>
        /// Splits a shape into two shapes - one for the left (negative x) and one for the right (positive x). The input shape becomes the right side shape, and the returned shape is the left side one.
        /// </summary>
        /// <param name="inputShape">The shape containing all of the data to be split, which then becomes the right side shape.</param>
        /// <param name="vertices">The vertices of the mesh that the blend shape is derived from. Used to determine if a vertex is on the left or right side (negative/positive x)</param>
        /// <param name="maxFalloffDistance">The maxium distance from the center on the x-axis (where x is zero), after which data will be zeroed out if on the opposing side. Used to create a smooth falloff in the middle of split shapes.</param>
        /// <param name="falloffExponent">Configures the falloff to be exponential</param>
        /// <param name="tag_Left">Left side tag used to replace the Split tag in the shape name (if present).</param>
        /// <param name="tag_Right">Right side tag used to replace the Split tag in the shape name (if present).</param>
        /// <returns></returns>
        public static BlendShape SplitShape(BlendShape inputShape, Vector3[] vertices, float maxFalloffDistance = 0.05f, float falloffExponent = 1.65f, string tag_Split = tag_Split, string tag_Left = "_L", string tag_Right = "_R")
        {

            BlendShape leftSideShape = inputShape.Duplicate();

            inputShape.name = inputShape.name.Replace(tag_Split, tag_Right);
            leftSideShape.name = leftSideShape.name.Replace(tag_Split, tag_Left);

            if (maxFalloffDistance > 0)
            {

                for (int a = 0; a < leftSideShape.frames.Length; a++)
                {

                    BlendShape.Frame frame_right = inputShape.frames[a];
                    BlendShape.Frame frame_left = leftSideShape.frames[a];

                    for (int b = 0; b < frame_right.deltaVertices.Length; b++)
                    {

                        Vector3 v = vertices[b];

                        if (v.x > 0)
                        {

                            float falloff = Mathf.Pow(1 - Mathf.Clamp01(v.x / maxFalloffDistance), falloffExponent);
                            frame_left.deltaVertices[b] = frame_left.deltaVertices[b] * 0.5f * falloff; // zero out data on the right (x > 0) for the left side shape, using a falloff starting at the center (where x == 0)
                            frame_right.deltaVertices[b] = (frame_right.deltaVertices[b] * 0.5f) + (frame_right.deltaVertices[b] * 0.5f * (1 - falloff));
                        }
                        else if (v.x < 0)
                        {
                            float falloff = Mathf.Pow(1 - Mathf.Clamp01((-v.x) / maxFalloffDistance), falloffExponent);
                            frame_right.deltaVertices[b] = frame_right.deltaVertices[b] * 0.5f * falloff; // zero out data on the left (x < 0) for the right side shape, using a falloff starting at the center (where x == 0)
                            frame_left.deltaVertices[b] = (frame_left.deltaVertices[b] * 0.5f) + (frame_left.deltaVertices[b] * 0.5f * (1 - falloff));
                        }
                        else // average the center data evenly between the two sides
                        {
                            frame_left.deltaVertices[b] = frame_left.deltaVertices[b] * 0.5f;
                            frame_right.deltaVertices[b] = frame_right.deltaVertices[b] * 0.5f;
                        }

                    }

                }

            }
            else // if there is no falloff then just zero out data in the opposing sides
            {

                for (int a = 0; a < leftSideShape.frames.Length; a++)
                {

                    BlendShape.Frame frame_right = inputShape.frames[a];
                    BlendShape.Frame frame_left = leftSideShape.frames[a];

                    for (int b = 0; b < frame_right.deltaVertices.Length; b++)
                    {

                        Vector3 v = vertices[b];

                        if (v.x > 0)
                        {
                            frame_left.deltaVertices[b] = Vector3.zero; // zero out data on the right (x > 0) for the left side shape
                        }
                        else if (v.x < 0)
                        {
                            frame_right.deltaVertices[b] = Vector3.zero; // zero out data on the left (x < 0) for the right side shape
                        }
                        else // average the center data evenly between the two sides
                        {
                            frame_left.deltaVertices[b] = frame_left.deltaVertices[b] * 0.5f;
                            frame_right.deltaVertices[b] = frame_right.deltaVertices[b] * 0.5f;
                        }

                    }

                }

            }

            return leftSideShape;
        }

        #endregion

        #region Vertex Groups

        /// <summary>
        /// Average all Vertex Groups weights using the combined weight at each index between all groups.
        /// </summary>
        /// <param name="normalizeAsPoolAfter">Should the vertex groups be normalized using a global maximum after the per vertex pass?</param>
        public static void AverageVertexGroupWeightsPerVertex(IEnumerable<VertexGroup> vertexGroups, bool normalizeAsPoolAfter = true, float minWeightThreshold = 0.0001f)
        {

            if (vertexGroups == null) return;

            Dictionary<int, float2> weights = new Dictionary<int, float2>();

            foreach (VertexGroup vg in vertexGroups)
            {

                for (int a = 0; a < vg.EntryCount; a++)
                {

                    int i = vg.GetEntryIndex(a);

                    weights.TryGetValue(i, out float2 w);

                    float contribution = vg.GetEntryWeight(a);
                    if (contribution <= minWeightThreshold) continue;

                    weights[i] = new float2(w.x + contribution, w.y + 1);  

                }

            }

            foreach (VertexGroup vg in vertexGroups)
            {
                List<int> indices = new List<int>();
                List<float> weightValues = new List<float>();

                for (int a = 0; a < vg.EntryCount; a++)
                {

                    int i = vg.GetEntryIndex(a);

                    float2 total = weights[i];

                    if (total.x <= minWeightThreshold) continue; 

                    indices.Add(i);
                    weightValues.Add(vg.GetEntryWeight(a) / (total.y >= 1.99f ? total.x : 1f)); // only divide by total if there was more than one contributor to the weight at this index

                }

                vg.Clear();
                if (indices.Count == 0) continue;

                vg.SetWeights(indices, weightValues);
            }

            if (normalizeAsPoolAfter) NormalizeVertexGroupsAsPool(vertexGroups);
        }

        /// <summary>
        /// Normalize all Vertex Groups weights using the maximum weight obtained from checking every index in every group.
        /// </summary>
        public static void NormalizeVertexGroupsAsPool(IEnumerable<VertexGroup> vertexGroups)
        {

            if (vertexGroups == null) return;

            float maxWeight = 0;

            foreach (VertexGroup vg in vertexGroups)
            {
                for (int a = 0; a < vg.EntryCount; a++)
                {
                    float weight = vg.GetEntryWeight(a);
                    if (Mathf.Abs(weight) > maxWeight) maxWeight = weight;
                }
            }

            if (maxWeight == 0) return;

            foreach (VertexGroup vg in vertexGroups)
            {
                for (int a = 0; a < vg.EntryCount; a++) vg.SetEntryWeight(a, vg.GetEntryWeight(a) / maxWeight); 
            }
        }

        /// <summary>
        /// Converts the vertex offset data in blend shape frames (BlendShape.Frame.deltaVertices) into scalar weight values using magnitude. The smallest vertex offset magnitude (usually 0) is used as the minimum, and the largest magnitude as the maximum, and then the data is typically normalized in some way.
        /// </summary>
        public class VertexGroupExtractor
        {

            public List<VertexGroup> vertexGroups;

            public int AddVertexGroup(VertexGroup vertexGroup)
            {
                if (vertexGroups == null) vertexGroups = new List<VertexGroup>();

                int i = vertexGroups.Count;
                vertexGroups.Add(vertexGroup);
                return i;
            }

            /// <summary>
            /// Average all Vertex Groups weights using the combined weight at each index between all groups.
            /// </summary>
            /// <param name="normalizeAsPoolAfter">Should the vertex groups be normalized using a global maximum after the per vertex pass?</param>
            public void AverageVertexGroupWeightsPerVertex(bool normalizeAsPoolAfter = true) => MeshDataTools.AverageVertexGroupWeightsPerVertex(vertexGroups, normalizeAsPoolAfter);

            /// <summary>
            /// Normalize all Vertex Groups weights using the maximum weight obtained from checking every index in every group.
            /// </summary>
            public void NormalizeVertexGroupsAsPool() => MeshDataTools.NormalizeVertexGroupsAsPool(vertexGroups);

            public VertexGroup ConvertToVertexGroup(BlendShape shape, bool normalize = true, string keyword = "", float threshold = 0.0001f)
            {
                VertexGroup vg = VertexGroup.ConvertToVertexGroup(shape, normalize, keyword, threshold);
                if (vg.EntryCount > 0) AddVertexGroup(vg);

                return vg;
            }

            public void ExtractVertexGroups(SkinnedMeshRenderer renderer, bool removeFromMesh = true, bool normalize = true, bool normalizeToMesh = false, string keyword = "_VGROUP", float threshold = 0.0001f)
            {
                if (renderer == null) return;

                Mesh mesh = renderer.sharedMesh;
                if (mesh == null) return;

                if (removeFromMesh) mesh = mesh.Duplicate();
                ExtractVertexGroups(mesh, removeFromMesh, normalize, normalizeToMesh, keyword, threshold);
                renderer.sharedMesh = mesh;
            }

            /// <summary>
            /// Converts vertex group data stored in blendshapes into useable vertex groups.
            /// </summary>
            public void ExtractVertexGroups(Mesh mesh, bool removeFromMesh = true, bool normalize = true, bool averagePerVertex = false, string keyword = "_VGROUP", float threshold = 0.0001f)
            {
                if (mesh == null) return;

                Vector3[] vertices = mesh.vertices;

                bool IsVertexGroup(string shapeName)
                {

                    return shapeName.IndexOf(keyword) >= 0;

                }

                List<BlendShape> blendShapes = new List<BlendShape>();
                for (int a = 0; a < mesh.blendShapeCount; a++)
                {
                    string id = mesh.GetBlendShapeName(a);

                    BlendShape shape = new BlendShape(mesh, id);

                    if (IsVertexGroup(id))
                    {

                        if (shape.name.IndexOf(tag_Split) >= 0)
                        {
                            ConvertToVertexGroup(SplitShape(shape, vertices));
                        }

                        ConvertToVertexGroup(shape);
                    }
                    else
                    {
                        blendShapes.Add(shape);
                    }

                }

                if (removeFromMesh)
                {
                    mesh.ClearBlendShapes();
                    for (int a = 0; a < blendShapes.Count; a++) blendShapes[a].AddToMesh(mesh);
                }

                if (averagePerVertex) AverageVertexGroupWeightsPerVertex(normalize); else if (normalize) NormalizeVertexGroupsAsPool();
            }

        }

        #endregion

        #region Mesh Relations

        /// <summary>
        /// For each vertex in querying vertices, find the closest vertex to it in targetVertices and store the index in the output array.
        /// </summary>
        public static int[] FindClosestVertices(ICollection<Vector3> queryingVertices, ICollection<Vector3> targetVertices, int[] outputIndices = null)
        {
            if (outputIndices == null) outputIndices = new int[queryingVertices.Count];

            int queryIndex = 0;
            foreach (var queryVertex in queryingVertices)
            {

                int targetIndex = 0;
                int closestIndex = 0;
                float closestDistance = float.MaxValue;
                foreach (var targetVertex in targetVertices)
                {
                    float dist = (targetVertex - queryVertex).sqrMagnitude;
                    if (dist < closestDistance)
                    {
                        closestDistance = dist;
                        closestIndex = targetIndex;
                    }
                    targetIndex++;
                }

                outputIndices[queryIndex] = closestIndex;

                queryIndex++;
                if (queryIndex >= outputIndices.Length) break;
            }

            return outputIndices;
        }

        /// <summary>
        /// For each uv in querying verticesUV, find the closest vertex UV to it in targetVerticesUV and store the index in the output array.
        /// </summary>
        public static int[] FindClosestVerticesUV(ICollection<Vector2> queryingVerticesUV, ICollection<Vector2> targetVerticesUV, int[] outputIndices = null)
        {
            if (outputIndices == null) outputIndices = new int[queryingVerticesUV.Count];

            int queryIndex = 0;
            foreach (var queryVertex in queryingVerticesUV)
            {

                int targetIndex = 0;
                int closestIndex = 0;
                float closestDistance = float.MaxValue;
                foreach (var targetVertex in targetVerticesUV)
                {
                    float dist = (targetVertex - queryVertex).sqrMagnitude;
                    if (dist < closestDistance)
                    {
                        closestDistance = dist;
                        closestIndex = targetIndex;
                    }
                    targetIndex++;
                }

                outputIndices[queryIndex] = closestIndex;

                queryIndex++;
                if (queryIndex >= outputIndices.Length) break;
            }

            return outputIndices;
        }

        [Serializable]
        public struct MeshIsland
        {
            public Color color;
            public int[] vertices;

            /// <summary>
            /// Convenience field for storing origin vertex index
            /// </summary>
            public int originIndex; 
            public int OriginVertex => vertices[originIndex];

            public Vector3 GetCenter(Vector3[] vertexPositions)
            {
                if (vertices == null || vertices.Length <= 0) return Vector3.zero;

                Vector3 center = Vector3.zero;
                foreach (var vertexIndex in vertices) center = center + vertexPositions[vertexIndex];

                return center / vertices.Length;
            }
        }

        public static List<MeshIsland> CalculateMeshIslands(Mesh mesh, List<MeshIsland> meshIslands = null, bool weldVertices = true)
        {
            if (meshIslands == null) meshIslands = new List<MeshIsland>();

            var triangles = mesh.triangles;

            List<int> currentIsland = new List<int>();
            void CompleteIsland()
            {
                if (currentIsland.Count <= 0) return; // don't create empty mesh islands

                meshIslands.Add(new MeshIsland() { color = UnityEngine.Random.ColorHSV(), vertices = currentIsland.ToArray() });
                currentIsland.Clear();
            }
            bool[] closedVertices = new bool[mesh.vertexCount]; // flags used to determine if a vertex has already been added to a mesh island
            var weldedVertices = weldVertices ? MeshDataTools.MergeVertices(mesh.vertices) : null;

            int GetWeldedIndex(int originalVertexIndex)
            {
                if (!weldVertices) return originalVertexIndex;
                return weldedVertices[originalVertexIndex].firstIndex;
            }

            void AddVertexConnections(int vertex)
            {
                int weldedConnectingIndex = GetWeldedIndex(vertex);
                for (int v = 0; v < triangles.Length; v += 3) // triangles are stored as linear groups of 3 vertex indices
                {
                    int t0 = v;
                    int t1 = v + 1;
                    int t2 = v + 2;

                    int v0 = triangles[t0];
                    int v1 = triangles[t1];
                    int v2 = triangles[t2];

                    var wv0 = GetWeldedIndex(v0);
                    var wv1 = GetWeldedIndex(v1);
                    var wv2 = GetWeldedIndex(v2);

                    if (weldedConnectingIndex != wv0 && weldedConnectingIndex != wv1 && weldedConnectingIndex != wv2) continue; // if none of the triangle indices are the connecting vertex then it's not part of the island

                    if (!closedVertices[wv0] && !currentIsland.Contains(wv0))
                    {
                        if (weldVertices)
                        {
                            var mv = weldedVertices[v0];
                            for (int c = 0; c < mv.indices.Count; c++)
                            {
                                var mergedIndex = mv.indices[c];
                                closedVertices[mergedIndex] = true;

                                currentIsland.Add(mergedIndex);
                                AddVertexConnections(mergedIndex); // add child connections recursively
                            }
                        }
                        else
                        {
                            closedVertices[wv0] = true;

                            currentIsland.Add(wv0);
                            AddVertexConnections(wv0); // add child connections recursively
                        }
                    }

                    if (!closedVertices[wv1] && !currentIsland.Contains(wv1))
                    {
                        if (weldVertices)
                        {
                            var mv = weldedVertices[v1];
                            for (int c = 0; c < mv.indices.Count; c++)
                            {
                                var mergedIndex = mv.indices[c];
                                closedVertices[mergedIndex] = true;

                                currentIsland.Add(mergedIndex);
                                AddVertexConnections(mergedIndex); // add child connections recursively
                            }
                        }
                        else
                        {
                            closedVertices[wv1] = true;

                            currentIsland.Add(wv1);
                            AddVertexConnections(wv1); // add child connections recursively
                        }
                    }

                    if (!closedVertices[wv2] && !currentIsland.Contains(wv2))
                    {
                        if (weldVertices)
                        {
                            var mv = weldedVertices[v2];
                            for (int c = 0; c < mv.indices.Count; c++)
                            {
                                var mergedIndex = mv.indices[c];
                                closedVertices[mergedIndex] = true;

                                currentIsland.Add(mergedIndex);
                                AddVertexConnections(mergedIndex); // add child connections recursively
                            }
                        }
                        else
                        {
                            closedVertices[wv2] = true;

                            currentIsland.Add(wv2);
                            AddVertexConnections(wv2); // add child connections recursively
                        }
                    }
                }
            }

            for (int v = 0; v < mesh.vertexCount; v++) // run through each vertex and find its connections
            {

                if (closedVertices[v]) continue; // vertex has already been added to an island through recursion

                CompleteIsland();

                currentIsland.Add(v);
                closedVertices[v] = true;

                AddVertexConnections(v);
            }

            CompleteIsland();

            return meshIslands;
        }

        public static bool GetClosestContainingTriangle(Vector3[] containingVertices, int[] containingTris, Vector3 targetPosition, out int i1, out int i2, out int i3, out float w1, out float w2, out float w3, float maxDistance = -1, float errorMargin = 0)
        {
            return GetClosestContainingTriangle(containingVertices, containingTris, targetPosition, out i1, out i2, out i3, out w1, out w2, out w3, out _, maxDistance, errorMargin);
        }
        public static bool GetClosestContainingTriangle(Vector3[] containingVertices, int[] containingTris, Vector3 targetPosition, out int i1, out int i2, out int i3, out float w1, out float w2, out float w3, out float closestDistance, float maxDistance = -1, float errorMargin = 0)
        {
            closestDistance = float.MaxValue; 

            i1 = -1;
            i2 = -1;
            i3 = -1;

            w1 = 0;
            w2 = 0;
            w3 = 0;

            if (containingVertices == null || containingTris == null) return false; 

            for(int a = 0; a < containingTris.Length; a += 3)
            {
                int i1_ = containingTris[a];
                int i2_ = containingTris[a + 1];
                int i3_ = containingTris[a + 2];

                Vector3 v1 = containingVertices[i1_];
                Vector3 v2 = containingVertices[i2_];
                Vector3 v3 = containingVertices[i3_];

                /*float d1 = (v1 - targetPosition).sqrMagnitude;
                float d2 = (v2 - targetPosition).sqrMagnitude;
                float d3 = (v3 - targetPosition).sqrMagnitude;

                float distance = Mathf.Min(d1, d2, d3);*/

                float distance = (((v1 + v2 + v3) / 3f) - targetPosition).sqrMagnitude; 
                if (distance < closestDistance && (maxDistance < 0 || distance < (maxDistance * maxDistance))) 
                {
                    var coords = Maths.BarycentricCoords(targetPosition, v1, v2, v3); 
                    if (Maths.IsInTriangle(coords, errorMargin))
                    {
                        closestDistance = distance;

                        i1 = i1_;
                        i2 = i2_;
                        i3 = i3_;

                        w1 = coords.x;
                        w2 = coords.y;
                        w3 = coords.z;
                    }
                }

            }

            return i1 >= 0 && i2 >= 0 && i3 >= 0;
        }

        #endregion

        #region Mesh Editing

        public struct BlendShapeFrameVertex
        {
            public Vector3 deltaVertex;
            public Vector3 deltaNormal;
            public Vector3 deltaTangent;
        }
        public struct BlendShapeVertex
        {
            public string blendShapeName;

            public BlendShapeFrameVertex[] frames;
        }
        public struct VertexData
        {
            public Vector3 position; 
            public Vector3 normal;
            public Vector4 tangent;

            public Color color;

            public Vector4 uv0;
            public Vector4 uv1;
            public Vector4 uv2;
            public Vector4 uv3;
            public Vector4 uv4;
            public Vector4 uv5;
            public Vector4 uv6;
            public Vector4 uv7;

            public BlendShapeVertex[] blendShapes;

            public byte boneCount;
            public BoneWeight1[] boneWeights; 
        }
        public class EditedMesh
        {
            protected Mesh originalMesh;
            protected Mesh finalMesh;

            protected List<int>[] triangles;
            public int SubMeshCount => triangles.Length;
            public int GetSubmeshTriangleCount(int submesh) => triangles[submesh].Count;
            public int GetTriangleVertexIndex(int submesh, int triIndex) => triangles[submesh][triIndex];
            public int SetTriangleVertexIndex(int submesh, int triIndex, int vertexIndex) => triangles[submesh][triIndex] = vertexIndex;

            public int AddTriangle(int subMesh, int index0, int index1, int index2)
            {
                var tris = triangles[subMesh];
                int triIndex = tris.Count;

                tris.Add(index0);
                tris.Add(index1);
                tris.Add(index2);

                return triIndex / 3;
            }
            public void AddTriangles(int subMesh, int[] trianglesToAdd)
            {
                var subMeshList = triangles[subMesh];
                for (int a = 0; a < trianglesToAdd.Length; a++)
                {
                    subMeshList.Add(trianglesToAdd[a]);
                }
            }

            public void SetVertexData(int vertexIndex, VertexData vertexData)
            {
                if (vertexIndex < 0 || vertexIndex >= VertexCount) return;

                SetVertex(vertexIndex, vertexData.position);
                if (HasNormals) SetNormal(vertexIndex, vertexData.normal);
                if (HasTangents) SetTangent(vertexIndex, vertexData.tangent); 

                if (HasColors) SetColor(vertexIndex, vertexData.color);

                if (HasUV0) SetUV0(vertexIndex, vertexData.uv0);
                if (HasUV1) SetUV1(vertexIndex, vertexData.uv1);
                if (HasUV2) SetUV2(vertexIndex, vertexData.uv2);
                if (HasUV3) SetUV3(vertexIndex, vertexData.uv3);
                if (HasUV4) SetUV4(vertexIndex, vertexData.uv4);
                if (HasUV5) SetUV5(vertexIndex, vertexData.uv5);
                if (HasUV6) SetUV6(vertexIndex, vertexData.uv6);
                if (HasUV7) SetUV7(vertexIndex, vertexData.uv7);

                if (HasBoneWeights) SetBoneWeights(vertexIndex, vertexData.boneCount, vertexData.boneWeights);

                if (HasBlendShapes && vertexData.blendShapes != null)
                {
                    for(int a = 0; a < vertexData.blendShapes.Length; a++)
                    {
                        var shape = vertexData.blendShapes[a];
                        int shapeIndex = GetBlendShapeIndex(shape.blendShapeName);
                        for(int b = 0; b < shape.frames.Length; b++)
                        {
                            var frame = shape.frames[b];
                            SetBlendShapeDeltaVertex(a, b, vertexIndex, frame.deltaVertex);
                            SetBlendShapeDeltaNormal(a, b, vertexIndex, frame.deltaNormal);
                            SetBlendShapeDeltaTangent(a, b, vertexIndex, frame.deltaTangent); 
                        }
                    }
                }
            }
            public VertexData GetVertexData(int vertexIndex)
            {
                if (vertexIndex < 0 || vertexIndex >= VertexCount) return default;

                var boneWeights_ = GetBoneWeights(vertexIndex);
                var blendShapes_ = blendShapes == null ? null : new BlendShapeVertex[blendShapes.Count];
                for(int a = 0; a < blendShapes_.Length; a++)
                {
                    var shape = blendShapes[a];
                    var shape_ = new BlendShapeVertex();
                    shape_.blendShapeName = shape.name;
                    shape_.frames = new BlendShapeFrameVertex[shape.frames.Length];
                    for (int b = 0; b < shape_.frames.Length; b++) shape_.frames[b] = new BlendShapeFrameVertex()
                    {
                        deltaVertex = GetBlendShapeDeltaVertex(a, b, vertexIndex),
                        deltaNormal = GetBlendShapeDeltaNormal(a, b, vertexIndex),
                        deltaTangent = GetBlendShapeDeltaTangent(a, b, vertexIndex)
                    };
                    blendShapes_[a] = shape_;  
                }

                return new VertexData()
                {
                    position = GetVertex(vertexIndex),
                    normal = GetNormal(vertexIndex),
                    tangent = GetTangent(vertexIndex),

                    color = GetColor(vertexIndex),

                    uv0 = GetUV0(vertexIndex),
                    uv1 = GetUV1(vertexIndex),
                    uv2 = GetUV2(vertexIndex),
                    uv3 = GetUV3(vertexIndex),
                    uv4 = GetUV4(vertexIndex),
                    uv5 = GetUV5(vertexIndex),
                    uv6 = GetUV6(vertexIndex),
                    uv7 = GetUV7(vertexIndex),

                    blendShapes = blendShapes_,

                    boneCount = (byte)boneWeights_.Length,
                    boneWeights = boneWeights_
                };
            }

            protected List<Vector3> vertices;
            public List<Vector3> VertexList => vertices;
            public int VertexCount => vertices.Count;
            public void SetVertex(int index, Vector3 val)
            {
                if (vertices == null || index < 0 || index >= vertices.Count) return;
                vertices[index] = val;
            }
            public Vector3 GetVertex(int index) => vertices == null || index < 0 || index >= vertices.Count ? default : vertices[index];

            protected List<Vector3> normals;
            public bool HasNormals => normals != null && normals.Count > 0;
            public void SetNormal(int index, Vector3 val)
            {
                if (normals == null || index < 0 || index >= normals.Count) return;
                normals[index] = val;
            }
            public Vector3 GetNormal(int index) => normals == null || index < 0 || index >= normals.Count ? default : normals[index];

            protected List<Vector4> tangents;
            public bool HasTangents => tangents != null && tangents.Count > 0;
            public void SetTangent(int index, Vector4 val)
            {
                if (tangents == null || index < 0 || index >= tangents.Count) return;
                tangents[index] = val;
            }
            public Vector4 GetTangent(int index) => tangents == null || index < 0 || index >= tangents.Count ? default : tangents[index];

            protected List<Color> colors;
            public bool HasColors => colors != null && colors.Count > 0;
            public void SetColor(int index, Color val)
            {
                if (colors == null || index < 0 || index >= colors.Count) return;
                colors[index] = val;
            }
            public Color GetColor(int index) => colors == null || index < 0 || index >= colors.Count ? default : colors[index];

            protected List<Vector4> uv0;
            public bool HasUV0 => uv0 != null && uv0.Count > 0;
            public void SetUV0(int index, Vector4 val)
            {
                if (uv0 == null || index < 0 || index >= uv0.Count) return;
                uv0[index] = val;
            }
            public Vector4 GetUV0(int index) => uv0 == null || index < 0 || index >= uv0.Count ? default : uv0[index];

            protected List<Vector4> uv1;
            public bool HasUV1 => uv1 != null && uv1.Count > 0;
            public void SetUV1(int index, Vector4 val)
            {
                if (uv1 == null || index < 0 || index >= uv1.Count) return;
                uv1[index] = val;
            }
            public Vector4 GetUV1(int index) => uv1 == null || index < 0 || index >= uv1.Count ? default : uv1[index];

            protected List<Vector4> uv2;
            public bool HasUV2 => uv2 != null && uv2.Count > 0;
            public void SetUV2(int index, Vector4 val)
            {
                if (uv2 == null || index < 0 || index >= uv2.Count) return;
                uv2[index] = val;
            }
            public Vector4 GetUV2(int index) => uv2 == null || index < 0 || index >= uv2.Count ? default : uv2[index];

            protected List<Vector4> uv3;
            public bool HasUV3 => uv3 != null && uv3.Count > 0;
            public void SetUV3(int index, Vector4 val)
            {
                if (uv3 == null || index < 0 || index >= uv3.Count) return;
                uv3[index] = val;
            }
            public Vector4 GetUV3(int index) => uv3 == null || index < 0 || index >= uv3.Count ? default : uv3[index];

            protected List<Vector4> uv4;
            public bool HasUV4 => uv4 != null && uv4.Count > 0;
            public void SetUV4(int index, Vector4 val)
            {
                if (uv4 == null || index < 0 || index >= uv4.Count) return;
                uv4[index] = val;
            }
            public Vector4 GetUV4(int index) => uv4 == null || index < 0 || index >= uv4.Count ? default : uv4[index];

            protected List<Vector4> uv5;
            public bool HasUV5 => uv5 != null && uv5.Count > 0;
            public void SetUV5(int index, Vector4 val)
            {
                if (uv5 == null || index < 0 || index >= uv5.Count) return;
                uv5[index] = val;
            }
            public Vector4 GetUV5(int index) => uv5 == null || index < 0 || index >= uv5.Count ? default : uv5[index];

            protected List<Vector4> uv6;
            public bool HasUV6 => uv6 != null && uv6.Count > 0;
            public void SetUV6(int index, Vector4 val)
            {
                if (uv6 == null || index < 0 || index >= uv6.Count) return;
                uv6[index] = val;
            }
            public Vector4 GetUV6(int index) => uv6 == null || index < 0 || index >= uv6.Count ? default : uv6[index];

            protected List<Vector4> uv7;
            public bool HasUV7 => uv7 != null && uv7.Count > 0;
            public void SetUV7(int index, Vector4 val)
            {
                if (uv7 == null || index < 0 || index >= uv7.Count) return;
                uv7[index] = val;
            }
            public Vector4 GetUV7(int index) => uv7 == null || index < 0 || index >= uv7.Count ? default : uv7[index];

            protected List<BlendShape> blendShapes;
            public bool HasBlendShapes => blendShapes != null && blendShapes.Count > 0;
            public int BlendShapeCount => blendShapes == null ? 0 : blendShapes.Count;
            public BlendShape GetBlendShape(string name)
            {
                if (blendShapes == null) return null;
                foreach (var shape in blendShapes) if (shape.name == name) return shape;
                return null;
            }
            public int GetBlendShapeIndex(string name)
            {
                if (blendShapes == null) return -1;
                for(int a = 0; a < blendShapes.Count; a++) if (blendShapes[a].name == name) return a;
                return -1;
            }
            public BlendShape GetBlendShape(int index)
            {
                if (blendShapes == null || index < 0 || index >= blendShapes.Count) return null;
                return blendShapes[index];
            }

            public void SetBlendShapeDeltaVertex(int shapeIndex, int frameIndex, int vertexIndex, Vector3 deltaVertex)
            {
                if (blendShapes == null || shapeIndex < 0 || shapeIndex >= blendShapes.Count || frameIndex < 0) return;
                var shape = blendShapes[shapeIndex];
                if (shape == null || shape.frames == null) return;
                var frame = shape.frames[frameIndex];
                frame.deltaVertices[vertexIndex] = deltaVertex; 
            }
            public Vector3 GetBlendShapeDeltaVertex(int shapeIndex, int frameIndex, int vertexIndex)
            {
                if (blendShapes == null || shapeIndex < 0 || shapeIndex >= blendShapes.Count || frameIndex < 0) return default;
                var shape = blendShapes[shapeIndex];
                if (shape == null || shape.frames == null) return default;
                var frame = shape.frames[frameIndex];
                return frame.deltaVertices[vertexIndex];
            }

            public void SetBlendShapeDeltaNormal(int shapeIndex, int frameIndex, int vertexIndex, Vector3 deltaNormal)
            {
                if (blendShapes == null || shapeIndex < 0 || shapeIndex >= blendShapes.Count || frameIndex < 0) return;
                var shape = blendShapes[shapeIndex];
                if (shape == null || shape.frames == null) return;
                var frame = shape.frames[frameIndex];
                frame.deltaNormals[vertexIndex] = deltaNormal;
            }
            public Vector3 GetBlendShapeDeltaNormal(int shapeIndex, int frameIndex, int vertexIndex)
            {
                if (blendShapes == null || shapeIndex < 0 || shapeIndex >= blendShapes.Count || frameIndex < 0) return default;
                var shape = blendShapes[shapeIndex];
                if (shape == null || shape.frames == null) return default;
                var frame = shape.frames[frameIndex];
                return frame.deltaNormals[vertexIndex];
            }

            public void SetBlendShapeDeltaTangent(int shapeIndex, int frameIndex, int vertexIndex, Vector3 deltaTangent)
            {
                if (blendShapes == null || shapeIndex < 0 || shapeIndex >= blendShapes.Count || frameIndex < 0) return;
                var shape = blendShapes[shapeIndex];
                if (shape == null || shape.frames == null) return;
                var frame = shape.frames[frameIndex];
                frame.deltaTangents[vertexIndex] = deltaTangent;  
            }
            public Vector3 GetBlendShapeDeltaTangent(int shapeIndex, int frameIndex, int vertexIndex)
            {
                if (blendShapes == null || shapeIndex < 0 || shapeIndex >= blendShapes.Count || frameIndex < 0) return default;
                var shape = blendShapes[shapeIndex];
                if (shape == null || shape.frames == null) return default;
                var frame = shape.frames[frameIndex];
                return frame.deltaTangents[vertexIndex];
            }

            protected List<byte> bonesPerVertex;
            protected List<BoneWeight1> boneWeights;
            public bool HasBoneWeights => bonesPerVertex != null && bonesPerVertex.Count > 0 && boneWeights != null && boneWeights.Count > 0;
            public void SetBoneWeights(int index, byte boneCount, IList<BoneWeight1> localBoneWeights)
            {
                if (bonesPerVertex == null || index < 0 || index >= bonesPerVertex.Count) return;

                int boneWeightsIndex = 0;
                for(int a = 0; a < index; a++) boneWeightsIndex += bonesPerVertex[a];
                int prevBoneCount = bonesPerVertex[index];
                boneWeights.RemoveRange(boneWeightsIndex, prevBoneCount);
                bonesPerVertex[index] = boneCount;
                for (int a = boneCount - 1; a >= 0; a--) boneWeights.Insert(boneWeightsIndex, localBoneWeights[a]);
            }
            public void SetBoneWeights(int index, byte boneCount, BoneWeight1[] localBoneWeights)
            {
                if (bonesPerVertex == null || index < 0 || index >= bonesPerVertex.Count) return;

                int boneWeightsIndex = 0;
                for (int a = 0; a < index; a++) boneWeightsIndex += bonesPerVertex[a];
                int prevBoneCount = bonesPerVertex[index];
                boneWeights.RemoveRange(boneWeightsIndex, prevBoneCount);
                bonesPerVertex[index] = boneCount;
                for (int a = boneCount - 1; a >= 0; a--) boneWeights.Insert(boneWeightsIndex, localBoneWeights[a]);
            }
            public void GetBoneWeights(int index, IList<BoneWeight1> localBoneWeights)
            {
                if (bonesPerVertex == null || index < 0 || index >= bonesPerVertex.Count) return;

                int boneWeightsIndex = 0;
                for (int a = 0; a < index; a++) boneWeightsIndex += bonesPerVertex[a];
                int boneCount = bonesPerVertex[index];

                for(int a = 0; a < boneCount; a++) localBoneWeights.Add(boneWeights[a + boneWeightsIndex]);
            }
            public BoneWeight1[] GetBoneWeights(int index)
            {
                if (bonesPerVertex == null || index < 0 || index >= bonesPerVertex.Count) return new BoneWeight1[0];

                int boneWeightsIndex = 0;
                for (int a = 0; a < index; a++) boneWeightsIndex += bonesPerVertex[a];
                int boneCount = bonesPerVertex[index];

                var array = new BoneWeight1[boneCount];
                for (int a = 0; a < boneCount; a++) array[a] = boneWeights[a + boneWeightsIndex];

                return array;
            }

            public void AddVertexData(VertexData vertexData)
            {
                vertices.Add(vertexData.position);
                if (HasNormals) normals.Add(vertexData.normal);
                if (HasTangents) tangents.Add(vertexData.tangent);

                if (HasColors) colors.Add(vertexData.color);

                if (HasUV0) uv0.Add(vertexData.uv0);
                if (HasUV1) uv1.Add(vertexData.uv1);
                if (HasUV2) uv2.Add(vertexData.uv2);
                if (HasUV3) uv3.Add(vertexData.uv3);
                if (HasUV4) uv4.Add(vertexData.uv4);
                if (HasUV5) uv5.Add(vertexData.uv5);
                if (HasUV6) uv6.Add(vertexData.uv6);
                if (HasUV7) uv7.Add(vertexData.uv7);

                if (HasBlendShapes)
                {
                    if (vertexData.blendShapes != null && vertexData.blendShapes.Length > 0)
                    {
                        foreach (var existingShape in blendShapes)
                        {
                            if (existingShape.frames == null || existingShape.frames.Length <= 0) continue;

                            bool flag = false;
                            foreach (var shape in vertexData.blendShapes)
                            {
                                if (shape.frames == null || shape.frames.Length <= 0) continue;

                                if (shape.blendShapeName == existingShape.name)
                                {
                                    for (int a = 0; a < existingShape.frames.Length; a++)
                                    {
                                        var frame = (a > shape.frames.Length ? default : shape.frames[a]);
                                        var meshShapeFrame = existingShape.frames[a];
                                        meshShapeFrame.expandable_deltaVertices.Add(frame.deltaVertex); 
                                        meshShapeFrame.expandable_deltaNormals.Add(frame.deltaNormal);
                                        meshShapeFrame.expandable_deltaTangents.Add(frame.deltaTangent);

                                        flag = true;
                                    }
                                }

                            }

                            if (!flag)
                            {
                                foreach (var frame in existingShape.frames)
                                {
                                    frame.expandable_deltaVertices.Add(Vector3.zero);
                                    frame.expandable_deltaNormals.Add(Vector3.zero);
                                    frame.expandable_deltaTangents.Add(Vector3.zero);
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (var shape in blendShapes)
                        {
                            if (shape.frames == null || shape.frames.Length <= 0) continue;

                            foreach (var frame in shape.frames)
                            {
                                frame.expandable_deltaVertices.Add(Vector3.zero);
                                frame.expandable_deltaNormals.Add(Vector3.zero);
                                frame.expandable_deltaTangents.Add(Vector3.zero); 
                            }
                        }
                    }
                }

                if (HasBoneWeights)
                {
                    bonesPerVertex.Add(vertexData.boneCount);
                    for(int a = 0; a < vertexData.boneCount; a++) boneWeights.Add(vertexData.boneWeights[a]);  
                }
            }

            public void CombineWith(EditedMesh secondMesh, int subMeshIndexOffset = 0)
            {
                if (secondMesh == null) return;

                int startVertexIndex = VertexCount;

                if (secondMesh.HasBlendShapes)
                {
                    foreach(var shape in secondMesh.blendShapes)
                    {
                        if (GetBlendShapeIndex(shape.name) >= 0) continue;

                        var newShape = new BlendShape(shape.name, shape.frames, VertexCount, true);
                        if (blendShapes == null) blendShapes = new List<BlendShape>();
                        blendShapes.Add(newShape);
                    }
                }

                for (int a = 0; a < secondMesh.VertexCount; a++) AddVertexData(secondMesh.GetVertexData(a));
                
                if (secondMesh.triangles != null)
                {
                    if (triangles == null || triangles.Length < secondMesh.triangles.Length)
                    {
                        var temp = triangles;
                        triangles = new List<int>[Mathf.Max(temp.Length, secondMesh.triangles.Length + subMeshIndexOffset)];
                        for (int a = 0; a < temp.Length; a++) triangles[a] = temp[a];
                        for (int a = temp.Length; a < triangles.Length; a++) triangles[a] = new List<int>();
                    }

                    for(int a = 0; a < secondMesh.triangles.Length; a++)
                    {
                        int b = a + subMeshIndexOffset;

                        var list = secondMesh.triangles[a];
                        var list2 = triangles[b]; 

                        for (int c = 0; c < list.Count; c++) list2.Add(list[c] + startVertexIndex); 
                    }
                }
            }

            public EditedMesh(Mesh mesh, bool instantiateOutput = true)
            {
                originalMesh = mesh;
                if (!instantiateOutput) finalMesh = mesh;

                triangles = new List<int>[mesh.subMeshCount];
                for(int a = 0; a < mesh.subMeshCount; a++) triangles[a] = new List<int>(mesh.GetTriangles(a));

                vertices = new List<Vector3>(mesh.vertices);

                var norms = mesh.normals;
                if (norms != null && norms.Length > 0) normals = new List<Vector3>(norms);

                var tans = mesh.tangents;
                if (tans != null && tans.Length > 0) tangents = new List<Vector4>(tans);

                var col = mesh.colors;
                if (col != null && col.Length > 0) colors = new List<Color>(col);

                uv0 = new List<Vector4>();
                mesh.GetUVs(0, uv0);
                if (uv0 != null && uv0.Count <= 0) uv0 = null;

                uv1 = new List<Vector4>();
                mesh.GetUVs(1, uv1);
                if (uv1 != null && uv1.Count <= 0) uv1 = null;

                uv2 = new List<Vector4>();
                mesh.GetUVs(2, uv2);
                if (uv2 != null && uv2.Count <= 0) uv2 = null;

                uv3 = new List<Vector4>();
                mesh.GetUVs(3, uv3);
                if (uv3 != null && uv3.Count <= 0) uv3 = null;

                uv4 = new List<Vector4>();
                mesh.GetUVs(4, uv4);
                if (uv4 != null && uv4.Count <= 0) uv4 = null;

                uv5 = new List<Vector4>();
                mesh.GetUVs(5, uv5);
                if (uv5 != null && uv5.Count <= 0) uv5 = null;

                uv6 = new List<Vector4>();
                mesh.GetUVs(6, uv6);
                if (uv6 != null && uv6.Count <= 0) uv6 = null;

                uv7 = new List<Vector4>();
                mesh.GetUVs(7, uv7);
                if (uv7 != null && uv7.Count <= 0) uv7 = null;

                //blendShapes = mesh.GetBlendShapes();
                blendShapes = new List<BlendShape>();
                for (int a = 0; a < mesh.blendShapeCount; a++) blendShapes.Add(new BlendShape(mesh, mesh.GetBlendShapeName(a), true));  

                NativeArray<byte> bonesPerVertex_ = mesh.GetBonesPerVertex();
                NativeArray<BoneWeight1> boneWeights_ = mesh.GetAllBoneWeights();

                if (bonesPerVertex_.IsCreated && bonesPerVertex_.Length > 0 && boneWeights_.IsCreated && boneWeights_.Length > 0)
                {
                    bonesPerVertex = new List<byte>(bonesPerVertex_);
                    boneWeights = new List<BoneWeight1>(boneWeights_);
                }

                try
                {
                    bonesPerVertex_.Dispose();
                } catch { }
                try
                {
                    boneWeights_.Dispose();
                }
                catch { }
            }

            public Mesh Finalize(bool createNewInstance = false)
            {
                if (this.finalMesh == null) this.finalMesh = MeshUtils.DuplicateMesh(originalMesh);
                var finalMesh = this.finalMesh;
                if (createNewInstance) finalMesh = MeshUtils.DuplicateMesh(finalMesh);

                finalMesh.ClearBlendShapes();
                finalMesh.Clear();

                finalMesh.SetVertices(vertices);
                if (normals != null && normals.Count > 0) finalMesh.SetNormals(normals);
                if (tangents != null && tangents.Count > 0) finalMesh.SetTangents(tangents);

                if (colors != null && colors.Count > 0) finalMesh.SetColors(colors);

                if (uv0 != null && uv0.Count > 0) finalMesh.SetUVs(0, uv0);
                if (uv1 != null && uv1.Count > 0) finalMesh.SetUVs(1, uv1);
                if (uv2 != null && uv2.Count > 0) finalMesh.SetUVs(2, uv2);
                if (uv3 != null && uv3.Count > 0) finalMesh.SetUVs(3, uv3);
                if (uv4 != null && uv4.Count > 0) finalMesh.SetUVs(4, uv4);
                if (uv5 != null && uv5.Count > 0) finalMesh.SetUVs(5, uv5);
                if (uv6 != null && uv6.Count > 0) finalMesh.SetUVs(6, uv6); 
                if (uv7 != null && uv7.Count > 0) finalMesh.SetUVs(7, uv7); 

                if (triangles != null)
                {
                    finalMesh.subMeshCount = originalMesh.subMeshCount;
                    for (int subMesh = 0; subMesh < triangles.Length; subMesh++) 
                    {
                        //finalMesh.SetSubMesh(subMesh, originalMesh.GetSubMesh(subMesh)); 
                        finalMesh.SetTriangles(triangles[subMesh], subMesh);
                    }
                }

                if (blendShapes != null)
                {
                    foreach (var blendShape in blendShapes)
                    {
                        var newShape = new BlendShape(blendShape.name);
                        for(int a = 0; a < blendShape.frames.Length; a++)
                        {
                            var frame = blendShape.frames[a]; 
                            newShape.AddFrame(frame.weight, 
                                frame.expandable_deltaVertices == null ? frame.deltaVertices.ToArray() : frame.expandable_deltaVertices.ToArray(),
                                frame.expandable_deltaNormals == null ? frame.deltaNormals.ToArray() : frame.expandable_deltaNormals.ToArray(),
                                frame.expandable_deltaTangents == null ? frame.deltaTangents.ToArray() : frame.expandable_deltaTangents.ToArray());
                        }

                        newShape.AddToMesh(finalMesh);
                    }
                }

                if (bonesPerVertex != null && bonesPerVertex.Count > 0 && boneWeights != null)
                {
                    var bonesPerVertex_ = new NativeArray<byte>(bonesPerVertex.ToArray(), Allocator.Persistent);
                    var boneWeights_ = new NativeArray<BoneWeight1>(boneWeights.ToArray(), Allocator.Persistent);
                    finalMesh.SetBoneWeights(bonesPerVertex_, boneWeights_);
                    bonesPerVertex_.Dispose();
                    boneWeights_.Dispose();

                    finalMesh.bindposes = originalMesh.bindposes;
                }

                return finalMesh;
            }
        }

        [Serializable]
        public struct SolidifiedVertexInfo
        {
            public bool isInnerLayer;
            public int distanceFromOpenEdge;
        }
        public static Mesh Solidify(Mesh inputMesh, float thickness, float offset, bool duplicateMesh = false, bool useRecalculatedNormals = true) => Solidify(inputMesh, thickness, offset, duplicateMesh, useRecalculatedNormals, false, out _);
        public static Mesh Solidify(Mesh inputMesh, float thickness, float offset, bool duplicateMesh, bool useRecalculatedNormals, bool includeVertexInfoOutput, out SolidifiedVertexInfo[] outputVertexInfo)
        {
            outputVertexInfo = null;

            Mesh outputMesh = inputMesh;
            if (duplicateMesh) outputMesh = MeshUtils.DuplicateMesh(inputMesh);

            int[] allTriangles = inputMesh.triangles;

            var editMesh = new EditedMesh(outputMesh);
            var weldedVertices = MergeVertices(editMesh.VertexList);
            
            List<int>[] vertexTris = new List<int>[weldedVertices.Length]; 
            void AddTriReferenceToVertex(int vertexIndex, int triIndex)
            {
                var welded = weldedVertices[vertexIndex];
                for(int a = 0; a < welded.indices.Count; a++)
                {
                    var vert = welded.indices[a];
                    var list = vertexTris[vert];
                    if (list == null)
                    {
                        list = new List<int>();
                        vertexTris[vert] = list;
                    }

                    list.Add(triIndex);
                }
            }

            for(int t = 0; t < allTriangles.Length; t += 3)
            {
                int v0 = allTriangles[t];
                int v1 = allTriangles[t + 1];
                int v2 = allTriangles[t + 2];

                AddTriReferenceToVertex(v0, t);
                AddTriReferenceToVertex(v1, t);
                AddTriReferenceToVertex(v2, t);
            }

            List<VertexData> newVertexData = new List<VertexData>();
            for(int a = 0; a < inputMesh.vertexCount; a++) newVertexData.Add(editMesh.GetVertexData(a)); 
            for(int a = 0; a < inputMesh.vertexCount; a++)
            {
                var dataA = editMesh.GetVertexData(a);
                var dataB = editMesh.GetVertexData(a);

                var vertexTrisA = vertexTris[a];
                var weldedA = weldedVertices[a];
                if (vertexTrisA != null)
                {
                    Vector3 recalNorm = Vector3.zero;
                    if (useRecalculatedNormals)
                    {
                        foreach(var tri in vertexTrisA)
                        {
                            int v0 = allTriangles[tri];
                            int v1 = allTriangles[tri + 1];
                            int v2 = allTriangles[tri + 2];

                            recalNorm = recalNorm + Maths.CalcNormal(editMesh.GetVertex(v0), editMesh.GetVertex(v1), editMesh.GetVertex(v2));
                        }
                    }
                    else
                    {
                        foreach(var weldedVert in weldedA.indices)
                        {
                            recalNorm = recalNorm + editMesh.GetNormal(weldedVert);
                        }
                    }
                    recalNorm = recalNorm.normalized;

                    dataA.position = dataA.position + (recalNorm * offset * thickness);
                    dataB.position = dataB.position + (recalNorm * -(1 - offset) * thickness);

                    dataB.normal = -dataB.normal;

                    if (dataA.blendShapes != null)
                    {
                        for(int b = 0; b < dataA.blendShapes.Length; b++)
                        {
                            var shapeA = dataA.blendShapes[b];
                            var shapeB = dataB.blendShapes[b];

                            for(int c = 0; c < shapeA.frames.Length; c++)
                            {
                                var frameA = shapeA.frames[c];
                                var frameB = shapeB.frames[c];

                                Vector3 recalNormFrame = Vector3.zero;
                                if (useRecalculatedNormals)
                                {
                                    foreach (var tri in vertexTrisA)
                                    {
                                        int v0 = allTriangles[tri];
                                        int v1 = allTriangles[tri + 1];
                                        int v2 = allTriangles[tri + 2];

                                        recalNormFrame = recalNormFrame + Maths.CalcNormal(editMesh.GetBlendShapeDeltaVertex(b, c, v0) + editMesh.GetVertex(v0), editMesh.GetBlendShapeDeltaVertex(b, c, v1) + editMesh.GetVertex(v1), editMesh.GetBlendShapeDeltaVertex(b, c, v2) + editMesh.GetVertex(v2));
                                    }
                                }
                                else
                                {
                                    foreach (var weldedVert in weldedA.indices)
                                    {
                                        recalNormFrame = recalNormFrame + editMesh.GetBlendShapeDeltaNormal(b, c, weldedVert) + editMesh.GetNormal(weldedVert); 
                                    }
                                }
                                recalNormFrame = recalNormFrame.normalized;

                                var origPos = editMesh.GetBlendShapeDeltaVertex(b, c, a) + editMesh.GetVertex(a); 

                                var newPosA = origPos + (recalNormFrame * offset * thickness);
                                var newPosB = origPos + (recalNormFrame * -(1 - offset) * thickness);

                                frameA.deltaVertex = newPosA - dataA.position;
                                frameB.deltaVertex = newPosB - dataB.position;

                                frameB.deltaNormal = -frameB.deltaNormal;

                                shapeA.frames[c] = frameA;
                                shapeB.frames[c] = frameB;
                            }
                        }
                    }
                }

                newVertexData[a] = dataA;
                newVertexData.Add(dataB);
                //editMesh.SetVertexData(a, dataA); // dont change mesh data directly until after loop
                //editMesh.AddVertexData(dataB);
            }
            for (int a = 0; a < newVertexData.Count; a++)
            {
                if (a < editMesh.VertexCount)
                {
                    editMesh.SetVertexData(a, newVertexData[a]); // apply solidification changes to existing verts
                } 
                else
                {
                    editMesh.AddVertexData(newVertexData[a]); // add second layer of solidifed vertices
                }
            }

            var edgeData = GetOpenEdgeData(inputMesh.vertexCount, allTriangles, weldedVertices);
            bool[] vertexEvaluationStates = new bool[inputMesh.vertexCount]; // determine if vertex has already created edge tris
            List<int3> layerConnectingFaces = new List<int3>();
            void AddEdgeTris(int subMesh, int vertexIndex)
            {
                var weldVertex = weldedVertices[vertexIndex];
                for(int a = 0; a < weldVertex.indices.Count; a++)
                {
                    var weldIndex = weldVertex.indices[a];
                    if (vertexEvaluationStates[weldIndex]) continue;

                    var edges = edgeData[weldIndex];
                    if (edges.openEdges != null && edges.openEdges.Count > 0)
                    {
                        //editMesh.SetVertex(weldIndex, editMesh.GetVertex(weldIndex) + editMesh.GetNormal(weldIndex) * 0.01f); // debug
                        //continue;

                        for(int b = 0; b < edges.openEdges.Count; b++)
                        {
                            var edge = edges.openEdges[b];
                            if (vertexEvaluationStates[edge.connectedIndex]) continue;

                            vertexEvaluationStates[edge.rootIndex] = true;

                            int v0, v1, v2, v3;
                            if (edge.triCornerIndex == 0 || edge.triCornerIndex == 1)
                            {
                                v0 = edge.rootIndex;
                                v1 = edge.connectedIndex;
                            } 
                            else
                            {
                                v0 = edge.connectedIndex;
                                v1 = edge.rootIndex;
                            }

                            v2 = v0 + inputMesh.vertexCount;
                            v3 = v1 + inputMesh.vertexCount;
                            
                            int t1 = editMesh.AddTriangle(subMesh, v0, v1, v2);
                            int t2 = editMesh.AddTriangle(subMesh, v2, v1, v3);

                            layerConnectingFaces.Add(new int3(subMesh, t1, t2));
                        }
                    }
                }
            }
            for (int a = 0; a < inputMesh.subMeshCount; a++) // add solidified triangles
            {
                var tris = inputMesh.GetTriangles(a);

                for (int b = 0; b < vertexEvaluationStates.Length; b++) vertexEvaluationStates[b] = false;

                for (int b = 0; b < tris.Length; b += 3) 
                {
                    var v0 = tris[b + 1] + inputMesh.vertexCount;
                    var v1 = tris[b] + inputMesh.vertexCount; // change winding to flip face direction
                    var v2 = tris[b + 2] + inputMesh.vertexCount;
                    
                    editMesh.AddTriangle(a, v0, v1, v2);  
                }

                // connect the two solidified layers with new triangles
                for (int b = 0; b < tris.Length; b += 3)
                {
                    var v0 = tris[b];
                    var v1 = tris[b + 1];
                    var v2 = tris[b + 2];

                    AddEdgeTris(a, v0);
                    AddEdgeTris(a, v1);
                    AddEdgeTris(a, v2); 
                }
            }
             
            // flip faces if they're facing the wrong direction. We compare the face normal to the face's offset from the center of all other connected faces. If the normal is facing the same direction as the offset, then the face gets flipped.
            foreach(var layerConnectingFace in layerConnectingFaces)
            {
                int t1 = layerConnectingFace.y * 3;
                int t2 = layerConnectingFace.z * 3;

                var t1_v0 = editMesh.GetTriangleVertexIndex(layerConnectingFace.x, t1);
                var t1_v1 = editMesh.GetTriangleVertexIndex(layerConnectingFace.x, t1 + 1);
                var t1_v2 = editMesh.GetTriangleVertexIndex(layerConnectingFace.x, t1 + 2);

                var t2_v0 = editMesh.GetTriangleVertexIndex(layerConnectingFace.x, t2);
                var t2_v1 = editMesh.GetTriangleVertexIndex(layerConnectingFace.x, t2 + 1);
                var t2_v2 = editMesh.GetTriangleVertexIndex(layerConnectingFace.x, t2 + 2);

                bool IsLocalVertex(int vertexIndex) => vertexIndex == t1_v0 || vertexIndex == t1_v1 || vertexIndex == t1_v2 || vertexIndex == t2_v0 || vertexIndex == t2_v1 || vertexIndex == t2_v2;

                Vector3 faceCenter = (editMesh.GetVertex(t1_v0) + editMesh.GetVertex(t1_v1) + editMesh.GetVertex(t1_v2) + editMesh.GetVertex(t2_v0) + editMesh.GetVertex(t2_v1) + editMesh.GetVertex(t2_v2)) / 6f;
                Vector3 faceNormal = (Maths.CalcNormal(editMesh.GetVertex(t1_v0), editMesh.GetVertex(t1_v1), editMesh.GetVertex(t1_v2)) + Maths.CalcNormal(editMesh.GetVertex(t2_v0), editMesh.GetVertex(t2_v1), editMesh.GetVertex(t2_v2))).normalized;
                //Vector3 faceNormal = (editMesh.GetNormal(t1_v0) + editMesh.GetNormal(t1_v1) + editMesh.GetNormal(t1_v2) + editMesh.GetNormal(t2_v0) + editMesh.GetNormal(t2_v1) + editMesh.GetNormal(t2_v2)).normalized;
                //Vector3 faceNormal = Maths.CalcNormal(editMesh.GetVertex(t1_v0), editMesh.GetVertex(t1_v1), editMesh.GetVertex(t1_v2)).normalized;

                int submeshTriCount = editMesh.GetSubmeshTriangleCount(layerConnectingFace.x);
                Vector3 layerCenter = Vector3.zero;
                int i = 0;
                for(int a = 0; a < submeshTriCount; a += 3)
                {
                    if (a == t1 || a == t2) continue;

                    var t3_v0 = editMesh.GetTriangleVertexIndex(layerConnectingFace.x, a);
                    var t3_v1 = editMesh.GetTriangleVertexIndex(layerConnectingFace.x, a + 1);
                    var t3_v2 = editMesh.GetTriangleVertexIndex(layerConnectingFace.x, a + 2);

                    bool flagA = !(t3_v0 != t1_v0 && t3_v0 != t1_v1 && t3_v0 != t1_v2 &&
                        t3_v0 != t2_v0 && t3_v0 != t2_v1 && t3_v0 != t2_v2);
                    bool flagB = !(t3_v1 != t1_v0 && t3_v1 != t1_v1 && t3_v1 != t1_v2 &&
                        t3_v1 != t2_v0 && t3_v1 != t2_v1 && t3_v1 != t2_v2);
                    bool flagC = !(t3_v2 != t1_v0 && t3_v2 != t1_v1 && t3_v2 != t1_v2 &&
                        t3_v2 != t2_v0 && t3_v2 != t2_v1 && t3_v2 != t2_v2);

                    if ((flagA && flagB) || (flagB && flagC) || (flagA && flagC)) // tri shares two verts with the connecting face
                    {
                        int j = 0; 
                        Vector3 add = Vector3.zero; 
                        if (!IsLocalVertex(t3_v0))  
                        {
                            add = add + editMesh.GetVertex(t3_v0);
                            j++;
                        }
                        if (!IsLocalVertex(t3_v1))
                        {
                            add = add + editMesh.GetVertex(t3_v1);
                            j++;
                        }
                        if (!IsLocalVertex(t3_v2))
                        {
                            add = add + editMesh.GetVertex(t3_v2);
                            j++;
                        }

                        if (j > 0)
                        {
                            layerCenter = layerCenter + (add / j);
                            i++;
                        }
                    }
                }

                if (i != 0) 
                {
                    layerCenter = layerCenter / i;

                    var faceToLayerCenter = (layerCenter - faceCenter).normalized;
                    float dot = Vector3.Dot(faceNormal, faceToLayerCenter);
                    if (dot > 0) // face is facing inward, so it needs to be flipped
                    {
                        editMesh.SetTriangleVertexIndex(layerConnectingFace.x, t1, t1_v1);
                        editMesh.SetTriangleVertexIndex(layerConnectingFace.x, t1 + 1, t1_v0);

                        editMesh.SetTriangleVertexIndex(layerConnectingFace.x, t2, t2_v1);
                        editMesh.SetTriangleVertexIndex(layerConnectingFace.x, t2 + 1, t2_v0); 
                    }
                }
            }

            if (includeVertexInfoOutput)
            {

                int[] depths = new int[inputMesh.vertexCount];
                for (int a = 0; a < depths.Length; a++) depths[a] = -1;
                outputVertexInfo = new SolidifiedVertexInfo[editMesh.VertexCount];

                bool flag = true;
                int max = 99;
                int i = 0;
                while(flag)
                {
                    i++;
                    if (i >= max)
                    {
                        break;
                    }
                    flag = false; 

                    for(int a = 0; a < inputMesh.vertexCount; a++)
                    {
                        if (depths[a] >= 0) continue;

                        var edgeData_ = edgeData[a];
                        if (edgeData_.IsOpenEdge())
                        {
                            depths[a] = 0;
                        } 
                        else
                        {
                            int depth = -1; 

                            void CheckIndex(int ind)
                            {
                                var weld = weldedVertices[ind];
                                for(int b = 0; b < weld.indices.Count; b++)
                                {
                                    int c = weld.indices[b];
                                    if (depths[c] >= 0)
                                    {
                                        if (depth < 0 || (depths[c] + 1) < depth) depth = depths[c] + 1;
                                    }
                                }
                            }

                            var tris = vertexTris[a];
                            if (tris != null && tris.Count > 0)
                            {
                                foreach (var tri in tris)
                                {
                                    int v0 = allTriangles[tri];
                                    int v1 = allTriangles[tri + 1];
                                    int v2 = allTriangles[tri + 2];

                                    CheckIndex(v0);
                                    CheckIndex(v1);
                                    CheckIndex(v2);
                                }

                                depths[a] = depth;
                                if (depth < 0) 
                                { 
                                    flag = true;
                                } 
                                else
                                {
                                    var weld = weldedVertices[a];
                                    for (int b = 0; b < weld.indices.Count; b++) depths[weld.indices[b]] = depth;
                                }
                            } 
                            else
                            {
                                depths[a] = 0;
                            }
                        }
                    }
                }
                for(int a = 0; a < inputMesh.vertexCount; a++) 
                {
                    int depth = depths[a];
                    outputVertexInfo[a] = (new SolidifiedVertexInfo() { isInnerLayer = false, distanceFromOpenEdge = depth });
                    outputVertexInfo[a + inputMesh.vertexCount] = (new SolidifiedVertexInfo() { isInnerLayer = true, distanceFromOpenEdge = depth });
                }

                /*bool[] closedIndices = new bool[inputMesh.vertexCount];
                outputVertexInfo = new SolidifiedVertexInfo[editMesh.VertexCount];
                for (int a = 0; a < inputMesh.vertexCount; a++)
                {
                    var info = outputVertexInfo[a];
                    if (info.distanceFromOpenEdge > 0) continue;

                    for (int b = 0; b < closedIndices.Length; b++) closedIndices[b] = false;


                    int depth = 0;
                    int openEdgeDepth = -999;
                    void SearchForOpenEdge(int index, int depth_)
                    {
                        var edgeData_ = edgeData[index];
                        if (edgeData_.IsOpenEdge())
                        {
                            if (openEdgeDepth < 0)
                            {
                                openEdgeDepth = depth_;
                            } 
                            else
                            {
                                openEdgeDepth = Mathf.Min(openEdgeDepth, depth_);
                            }
                            return; 
                        }

                        var weld = weldedVertices[index];
                        if (closedIndices[weld.firstIndex] || closedIndices[index]) return;  

                        closedIndices[weld.firstIndex] = true; 
                        closedIndices[index] = true;

                        depth = Mathf.Min(depth, depth_);
                        var tris = vertexTris[index];
                        if (tris != null)
                        {
                            foreach(var tri in tris)
                            {
                                SearchForOpenEdge(allTriangles[tri], depth_ + 1);
                                SearchForOpenEdge(allTriangles[tri + 1], depth_ + 1);
                                SearchForOpenEdge(allTriangles[tri + 2], depth_ + 1); 
                            }
                        }
                    }
                    SearchForOpenEdge(a, 0);

                    var weld = weldedVertices[a];
                    depth = openEdgeDepth < 0 ? depth : openEdgeDepth;
                    for (int b = 0; b < weld.indices.Count; b++)
                    {
                        int i = weld.indices[b];
                        outputVertexInfo[i] = (new SolidifiedVertexInfo() { isInnerLayer = false, distanceFromOpenEdge = depth });  
                        outputVertexInfo[i + inputMesh.vertexCount] = (new SolidifiedVertexInfo() { isInnerLayer = true, distanceFromOpenEdge = depth }); 
                    }
                }*/
            }

            return editMesh.Finalize();
        }

        public static Mesh CombineMeshes(Mesh baseMesh, ICollection<Mesh> meshes)
        {
            if (meshes == null || meshes.Count <= 0) return baseMesh;

            var combinedMesh = new EditedMesh(baseMesh, true); 
            foreach(var toCombine in meshes)
            {
                combinedMesh.CombineWith(new EditedMesh(toCombine, false));
            }

            return combinedMesh.Finalize();
        }

        public struct MeshWithSubmeshIndexOffset
        {
            public Mesh mesh;
            public int subMeshIndexOffset;
        }
        public static Mesh CombineMeshes(Mesh baseMesh, ICollection<MeshWithSubmeshIndexOffset> meshes)
        {
            if (meshes == null || meshes.Count <= 0) return baseMesh;

            var combinedMesh = new EditedMesh(baseMesh, true);
            foreach (var toCombine in meshes)
            {
                combinedMesh.CombineWith(new EditedMesh(toCombine.mesh, false), toCombine.subMeshIndexOffset); 
            }

            return combinedMesh.Finalize();
        }

        public static Mesh CombineMeshes(Mesh baseMesh, ICollection<Mesh> meshes, ICollection<int> subMeshIndexOffsets)
        {
            if (meshes == null || meshes.Count <= 0) return baseMesh;

            var combinedMesh = new EditedMesh(baseMesh, true);

            using(var enu1 = meshes.GetEnumerator())
            {
                using (var enu2 = subMeshIndexOffsets.GetEnumerator())
                {
                    while(enu1.MoveNext())
                    {
                        combinedMesh.CombineWith(new EditedMesh(enu1.Current, false), enu2.MoveNext() ? enu2.Current : 0);
                    }
                }
            }

            return combinedMesh.Finalize();
        }

        #endregion

        #region Edges

        [Serializable]
        public struct Edge
        {
            /// <summary>
            /// The position of the root index in the triangle. 0 - first corner, 1 - second corner, 2 - third corner
            /// </summary>
            public int triCornerIndex;

            public int rootIndex;

            /// <summary>
            /// The index that the root index is connected to.
            /// </summary>
            public int connectedIndex;
        }
        [Serializable]
        public struct OpenEdgeData
        {
            /// <summary>
            /// The position of this index in the triangle. 0 - first corner, 1 - second corner, 2 - third corner
            /// </summary>
            public List<Edge> openEdges;

            public bool IsOpenEdge() => openEdges != null && openEdges.Count > 0;
        }
        [Serializable]
        public class TriEdgeConnections
        {
            /// <summary>
            /// The position of i0 in the triangle.
            /// </summary>
            public int triCornerIndex;

            public int i0;
            public int i1;
            public int i2;

            public TriEdgeConnections(int triCornerIndex, int i0, int i1, int i2)
            {
                this.triCornerIndex = triCornerIndex;
                this.i0 = i0; this.i1 = i1; this.i2 = i2;
            }

            public int edgeCount1;
            public int edgeCount2;

            public void IncrementEdgeCount(int tri2_i0, int tri2_i1, int tri2_i2, MergedVertex[] mergedVertices = null)
            {
                int MergedIndex(int vertexIndex)
                {
                    if (mergedVertices == null) return vertexIndex;
                    return mergedVertices[vertexIndex].firstIndex;
                }

                tri2_i0 = MergedIndex(tri2_i0);
                tri2_i1 = MergedIndex(tri2_i1);
                tri2_i2 = MergedIndex(tri2_i2);

                if (tri2_i0 == MergedIndex(i0))
                {
                    if (tri2_i1 == MergedIndex(i1)) edgeCount1++;
                    if (tri2_i1 == MergedIndex(i2)) edgeCount2++;

                    if (tri2_i2 == MergedIndex(i1)) edgeCount1++;
                    if (tri2_i2 == MergedIndex(i2)) edgeCount2++;
                }
                 
                if (tri2_i1 == MergedIndex(i0))
                {
                    if (tri2_i0 == MergedIndex(i1)) edgeCount1++;
                    if (tri2_i0 == MergedIndex(i2)) edgeCount2++;

                    if (tri2_i2 == MergedIndex(i1)) edgeCount1++;
                    if (tri2_i2 == MergedIndex(i2)) edgeCount2++;
                }

                if (tri2_i2 == MergedIndex(i0))
                {
                    if (tri2_i0 == MergedIndex(i1)) edgeCount1++;
                    if (tri2_i0 == MergedIndex(i2)) edgeCount2++;

                    if (tri2_i1 == MergedIndex(i1)) edgeCount1++;
                    if (tri2_i1 == MergedIndex(i2)) edgeCount2++;
                }
            }

            /// <summary>
            /// A vertex is part of an open edge if it only shares a single connection to another vertex
            /// </summary>
            public bool IsOpenEdge() => edgeCount1 <= 0 || edgeCount2 <= 0; 

            public OpenEdgeData GetOpenEdgeData(OpenEdgeData existing = default)
            {
                if (edgeCount1 <= 0)
                {
                    if (existing.openEdges == null) existing.openEdges = new List<Edge>();
                    existing.openEdges.Add(new Edge() { triCornerIndex = triCornerIndex, rootIndex = i0, connectedIndex = i1 });
                }

                if (edgeCount2 <= 0)
                {
                    if (existing.openEdges == null) existing.openEdges = new List<Edge>();
                    existing.openEdges.Add(new Edge() { triCornerIndex = triCornerIndex, rootIndex = i0, connectedIndex = i2 });
                }

                return existing;
            }
        }
        public static OpenEdgeData[] GetOpenEdgeData(int vertexCount, ICollection<int> triangles, MergedVertex[] mergedVertices = null)
        {
            OpenEdgeData[] edgeStates = new OpenEdgeData[vertexCount];

            int ti = 0;
            using (var triEnu1 = triangles.GetEnumerator())
            {
                while (triEnu1.MoveNext())
                {
                    ti++;

                    var v0 = triEnu1.Current;
                    triEnu1.MoveNext();
                    var v1 = triEnu1.Current;
                    triEnu1.MoveNext();
                    var v2 = triEnu1.Current;

                    var edgeConnections0 = new TriEdgeConnections(0, v0, v1, v2);
                    var edgeConnections1 = new TriEdgeConnections(1, v1, v0, v2);
                    var edgeConnections2 = new TriEdgeConnections(2, v2, v1, v0);

                    int ti2 = 0;
                    using (var triEnu2 = triangles.GetEnumerator()) 
                    {
                        while (triEnu2.MoveNext())
                        {
                            ti2++;

                            var v0_2 = triEnu2.Current;
                            triEnu2.MoveNext();
                            var v1_2 = triEnu2.Current;
                            triEnu2.MoveNext();
                            var v2_2 = triEnu2.Current;

                            if (ti == ti2) continue; // same triangle should be ignored

                            edgeConnections0.IncrementEdgeCount(v0_2, v1_2, v2_2, mergedVertices); 
                            edgeConnections1.IncrementEdgeCount(v0_2, v1_2, v2_2, mergedVertices);
                            edgeConnections2.IncrementEdgeCount(v0_2, v1_2, v2_2, mergedVertices);  
                        }
                    }

                    if (edgeConnections0.IsOpenEdge()) edgeStates[v0] = edgeConnections0.GetOpenEdgeData(edgeStates[v0]);
                    if (edgeConnections1.IsOpenEdge()) edgeStates[v1] = edgeConnections1.GetOpenEdgeData(edgeStates[v1]);
                    if (edgeConnections2.IsOpenEdge()) edgeStates[v2] = edgeConnections2.GetOpenEdgeData(edgeStates[v2]);
                }
            }

            return edgeStates;
        }

        #endregion

    }
}

#endif