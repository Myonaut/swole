#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

using UnityEngine;
using UnityEngine.Rendering;

using Swole.DataStructures;
using Swole.API.Unity;

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
        public struct WeldedVertex
        {
            public int firstIndex;

            /// <summary>
            /// Contains the indices of all merged vertices, including firstIndex
            /// </summary>
            public List<int> indices;

            public WeldedVertex(int firstIndex, List<int> indices)
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

        public static WeldedVertex[] MergeVertices(IEnumerable<Vector3> vertices, float mergeThreshold = 0.00001f) => WeldVertices(vertices, mergeThreshold);
        public static WeldedVertex[] WeldVertices(IEnumerable<Vector3> vertices, float mergeThreshold = 0.00001f)
        {

            Dictionary<float, Dictionary<float, Dictionary<float, WeldedVertex>>> sibling_stack = new Dictionary<float, Dictionary<float, Dictionary<float, WeldedVertex>>>();

            int count = 0;
            if (vertices is ICollection<Vector3> collection)
            {
                count = collection.Count;
            }
            else
            {
                foreach (var _ in vertices) count++;
            }

            WeldedVertex[] clones = new WeldedVertex[count];

            int index = 0;
            double merge = 1D / mergeThreshold;
            foreach (Vector3 vert in vertices)
            {

                float px = (float)(System.Math.Truncate((double)vert.x * merge) / merge);
                float py = (float)(System.Math.Truncate((double)vert.y * merge) / merge);
                float pz = (float)(System.Math.Truncate((double)vert.z * merge) / merge);

                Dictionary<float, Dictionary<float, WeldedVertex>> layer1;

                if (!sibling_stack.TryGetValue(px, out layer1))
                {
                    layer1 = new Dictionary<float, Dictionary<float, WeldedVertex>>();
                    sibling_stack[px] = layer1;
                }

                Dictionary<float, WeldedVertex> layer2;

                if (!layer1.TryGetValue(py, out layer2))
                {
                    layer2 = new Dictionary<float, WeldedVertex>();
                    layer1[py] = layer2;
                }

                if (!layer2.TryGetValue(pz, out WeldedVertex mergedVertex))
                {
                    mergedVertex = new WeldedVertex(index, new List<int>());
                    layer2[pz] = mergedVertex;
                }

                mergedVertex.indices.Add(index);
                for (int b = 0; b < mergedVertex.indices.Count; b++) clones[mergedVertex.indices[b]] = mergedVertex;

                layer2[pz] = mergedVertex;

                index++;
            }

            return clones;

        }

        public static List<Vector3> WeldPositions3D(IEnumerable<Vector3> positions, float weldDistance, List<Vector3> weldedPositionsOutput = null) => MergePositions3D(positions, weldDistance, weldedPositionsOutput);
        /// <summary>
        /// Sorts a collection of positions into groups based on mergeDistance and outputs a list of averaged positions per merge group.
        /// </summary>
        public static List<Vector3> MergePositions3D(IEnumerable<Vector3> positions, float mergeDistance, List<Vector3> mergedPositionsOutput = null)
        {

            if (mergedPositionsOutput == null) mergedPositionsOutput = new List<Vector3>();
            Dictionary<float, Dictionary<float, Dictionary<float, int2>>> sibling_stack = new Dictionary<float, Dictionary<float, Dictionary<float, int2>>>();

            double merge = 1D / mergeDistance;
            foreach (Vector3 pos in positions)
            {

                float px = (float)(System.Math.Truncate((double)pos.x * merge) / merge);
                float py = (float)(System.Math.Truncate((double)pos.y * merge) / merge);
                float pz = (float)(System.Math.Truncate((double)pos.z * merge) / merge);

                Dictionary<float, Dictionary<float, int2>> layer1;

                if (!sibling_stack.TryGetValue(px, out layer1))
                {
                    layer1 = new Dictionary<float, Dictionary<float, int2>>();
                    sibling_stack[px] = layer1;
                }

                Dictionary<float, int2> layer2;

                if (!layer1.TryGetValue(py, out layer2))
                {
                    layer2 = new Dictionary<float, int2>();
                    layer1[py] = layer2;
                }

                if (!layer2.TryGetValue(pz, out int2 mergedIndex))
                {
                    mergedIndex = new int2(mergedPositionsOutput.Count, 0);
                    mergedPositionsOutput.Add(Vector3.zero);
                    layer2[pz] = mergedIndex;
                }

                mergedPositionsOutput[mergedIndex.x] += pos;
                mergedIndex.y += 1;

                layer2[pz] = mergedIndex;
            }

            foreach(var x in sibling_stack)
            {
                foreach (var y in x.Value)
                {
                    foreach (var z in y.Value)
                    {
                        mergedPositionsOutput[z.Value.x] /= z.Value.y;  
                    }
                }
            }

            return mergedPositionsOutput;
        }

        #endregion

        #region Vertex Connections

        public static int[][] GetVertexConnections(int vertexCount, int[] triangles, WeldedVertex[] mergedVertices = null)
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
            void AddConnectedMergedVertex(WeldedVertex rootVertex, WeldedVertex connectedVertex)
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
        public static WeightedVertexConnection[][] GetDistanceWeightedVertexConnections(int[] triangles, Vector3[] vertices, WeldedVertex[] mergedVertices = null)
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
            void AddConnectedMergedVertex(WeldedVertex rootVertex, WeldedVertex connectedVertex)
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
        public struct Triangle : IEquatable<Triangle>
        {
            public int triIndex;
            public int i0, i1, i2;

            public TriangleOwnerIndex ownerIndex;

            public Triangle(int triIndex, int i0, int i1, int i2)
            {
                this.triIndex = triIndex;

                this.i0 = i0;
                this.i1 = i1;
                this.i2 = i2;

                ownerIndex = TriangleOwnerIndex.A;
            }

            public bool Equals(Triangle other)
            {
                return i0 == other.i0 && i1 == other.i1 && i2 == other.i2;
            }
        }

        public static Triangle[][] GetTriangleReferencesPerVertex(int vertexCount, int[] triangles, WeldedVertex[] mergedVertices = null)
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
        public static void AverageVertexGroupWeightsPerVertex(IEnumerable<VertexGroup> vertexGroups, bool normalizeAsPoolAfter = true, float minWeightThreshold = 0.00001f, float maxAverageWeight = 1f, float maxWeightNormalize = 0f)
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

                    if (!weights.TryGetValue(i, out float2 total)) continue;

                    indices.Add(i);
                    if (maxAverageWeight != 0f)
                    {
                        //weightValues.Add((total.x > maxAverageWeight ? ((vg.GetEntryWeight(a) / total.x) * maxAverageWeight) : vg.GetEntryWeight(a)));
                        weightValues.Add((total.x > 0f ? ((vg.GetEntryWeight(a) / total.x) * maxAverageWeight) : vg.GetEntryWeight(a)));
                    } 
                    else
                    {
                        //weightValues.Add(vg.GetEntryWeight(a) / (total.y >= 1.99f ? total.x : 1f)); // only divide by total if there was more than one contributor to the weight at this index
                        weightValues.Add(total.y >= 1.99f ? (vg.GetEntryWeight(a) / total.x): vg.GetEntryWeight(a)); // only divide by total if there was more than one contributor to the weight at this index
                    }
                }

                vg.Clear();
                if (indices.Count == 0) continue;

                vg.SetWeights(indices, weightValues);
            }

            if (normalizeAsPoolAfter) NormalizeVertexGroupsAsPool(vertexGroups, maxWeightNormalize);
        }

        /// <summary>
        /// Normalize all Vertex Groups weights using the maximum weight obtained from checking every index in every group.
        /// </summary>
        public static void NormalizeVertexGroupsAsPool(IEnumerable<VertexGroup> vertexGroups, float maxWeight = 0f)
        {

            if (vertexGroups == null) return;

            bool useProvidedMaxWeight = maxWeight != 0f;

            if (!useProvidedMaxWeight)
            {
                foreach (VertexGroup vg in vertexGroups)
                {
                    for (int a = 0; a < vg.EntryCount; a++)
                    {
                        float weight = vg.GetEntryWeight(a);
                        if (Mathf.Abs(weight) > maxWeight) maxWeight = weight;
                    }
                }
            }

            if (maxWeight == 0f) return;

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

            public VertexGroup ConvertToVertexGroup(BlendShape shape, bool normalize = true, string keyword = "", float threshold = 0.00001f)
            {
                VertexGroup vg = VertexGroup.ConvertToVertexGroup(shape, normalize, keyword, threshold);
                if (vg.EntryCount > 0) AddVertexGroup(vg);

                return vg;
            }

            public void ExtractVertexGroups(SkinnedMeshRenderer renderer, bool removeFromMesh = true, bool normalize = true, bool normalizeToMesh = false, string keyword = "_VGROUP", float threshold = 0.00001f)
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
            public void ExtractVertexGroups(Mesh mesh, bool removeFromMesh = true, bool normalize = true, bool averagePerVertex = false, string keyword = "_VGROUP", float threshold = 0.00001f)
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
            [HideInInspector]
            public int[] vertices;
            [HideInInspector]
            public Triangle[] triangles;

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


        [Obsolete("Extremely slow")]
        public static List<MeshIsland> CalculateMeshIslands_old(Mesh mesh, List<MeshIsland> meshIslands = null, bool weldVertices = true, bool includeTriangles = false) => CalculateMeshIslands(mesh, meshIslands, weldVertices, out _, includeTriangles);
        [Obsolete("Extremely slow")]
        public static List<MeshIsland> CalculateMeshIslands_old(Mesh mesh, List<MeshIsland> meshIslands, bool weldVertices, out WeldedVertex[] weldedVertices, bool includeTriangles = false) => CalculateMeshIslands(mesh, mesh.triangles, meshIslands, weldVertices, out weldedVertices, includeTriangles);

        [Obsolete("Extremely slow")]
        public static List<MeshIsland> CalculateMeshIslands_old(Mesh mesh, int[] triangles, List<MeshIsland> meshIslands = null, bool weldVertices = true, bool includeTriangles = false) => CalculateMeshIslands(mesh, triangles, meshIslands, weldVertices, out _, includeTriangles);
        [Obsolete("Extremely slow")]
        public static List<MeshIsland> CalculateMeshIslands_old(Mesh mesh, int[] triangles, List<MeshIsland> meshIslands, bool weldVertices, out WeldedVertex[] weldedVertices, bool includeTriangles = false)
        {
            if (meshIslands == null) meshIslands = new List<MeshIsland>();

            if (triangles == null) triangles = mesh.triangles;

            List<int> currentIsland = new List<int>();
            List<Triangle> currentIslandTris = new List<Triangle>();
            void CompleteIsland()
            {
                if (currentIsland.Count <= 0) return; // don't create empty mesh islands

                meshIslands.Add(new MeshIsland() { color = UnityEngine.Random.ColorHSV(), vertices = currentIsland.ToArray(), triangles = includeTriangles ? currentIslandTris.ToArray() : null });
                currentIsland.Clear();
                currentIslandTris.Clear();
            }
            bool[] closedVertices = new bool[mesh.vertexCount]; // flags used to determine if a vertex has already been added to a mesh island
            weldedVertices = weldVertices ? MeshDataTools.WeldVertices(mesh.vertices) : null;
            var weldedVertices_ = weldedVertices;

            int GetWeldedIndex(int originalVertexIndex)
            {
                if (!weldVertices) return originalVertexIndex;
                return weldedVertices_[originalVertexIndex].firstIndex;
            }

            void AddVertexConnections(int vertex)
            {
                int weldedConnectingIndex = GetWeldedIndex(vertex);
                for (int tri = 0; tri < triangles.Length; tri += 3) // triangles are stored as linear groups of 3 vertex indices
                {
                    int t0 = tri;
                    int t1 = tri + 1;
                    int t2 = tri + 2;

                    int v0 = triangles[t0];
                    int v1 = triangles[t1];
                    int v2 = triangles[t2];

                    bool triAdded = false;
                    var triangle = new Triangle(tri / 3, v0, v1, v2); 

                    var wv0 = GetWeldedIndex(v0);
                    var wv1 = GetWeldedIndex(v1);
                    var wv2 = GetWeldedIndex(v2);

                    if (weldedConnectingIndex != wv0 && weldedConnectingIndex != wv1 && weldedConnectingIndex != wv2) continue; // if none of the triangle indices are the connecting vertex then it's not part of the island

                    if (!closedVertices[wv0] && !currentIsland.Contains(wv0))
                    {
                        if (includeTriangles && !currentIslandTris.Contains(triangle)) 
                        {
                            triAdded = true;
                            currentIslandTris.Add(triangle);
                        }
                        
                        if (weldVertices)
                        {
                            var mv = weldedVertices_[v0];
                            for (int c = 0; c < mv.indices.Count; c++)
                            {
                                var mergedIndex = mv.indices[c];
                                closedVertices[mergedIndex] = true;

                                currentIsland.Add(mergedIndex);
                            }
                            for (int c = 0; c < mv.indices.Count; c++)
                            {
                                var mergedIndex = mv.indices[c];
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
                        if (includeTriangles && !triAdded && !currentIslandTris.Contains(triangle))
                        {
                            triAdded = true;
                            currentIslandTris.Add(triangle);
                        }

                        if (weldVertices)
                        {
                            var mv = weldedVertices_[v1];
                            for (int c = 0; c < mv.indices.Count; c++)
                            {
                                var mergedIndex = mv.indices[c];
                                closedVertices[mergedIndex] = true;

                                currentIsland.Add(mergedIndex);
                            }
                            for (int c = 0; c < mv.indices.Count; c++)
                            {
                                var mergedIndex = mv.indices[c];
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
                        if (includeTriangles && !triAdded && !currentIslandTris.Contains(triangle))
                        {
                            triAdded = true;
                            currentIslandTris.Add(triangle);
                        }

                        if (weldVertices)
                        {
                            var mv = weldedVertices_[v2];
                            for (int c = 0; c < mv.indices.Count; c++)
                            {
                                var mergedIndex = mv.indices[c];
                                closedVertices[mergedIndex] = true;

                                currentIsland.Add(mergedIndex);
                            }
                            for (int c = 0; c < mv.indices.Count; c++)
                            {
                                var mergedIndex = mv.indices[c];
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

                if (weldVertices)
                {
                    var mv = weldedVertices_[v];
                    for (int c = 0; c < mv.indices.Count; c++)
                    {
                        var mergedIndex = mv.indices[c];
                        closedVertices[mergedIndex] = true;

                        currentIsland.Add(mergedIndex);
                    }
                    for (int c = 0; c < mv.indices.Count; c++)
                    {
                        var mergedIndex = mv.indices[c];
                        AddVertexConnections(mergedIndex); // add child connections recursively
                    }
                }
                else
                {
                    closedVertices[v] = true;

                    currentIsland.Add(v);
                    AddVertexConnections(v); // add child connections recursively
                }

                AddVertexConnections(v);
            }

            CompleteIsland();

            return meshIslands;
        }


        public static List<MeshIsland> CalculateMeshIslands(Mesh mesh, List<MeshIsland> meshIslands = null, bool weldVertices = true, bool includeTriangles = false) => CalculateMeshIslands(mesh, meshIslands, weldVertices, out _, includeTriangles);
        public static List<MeshIsland> CalculateMeshIslands(Mesh mesh, List<MeshIsland> meshIslands, bool weldVertices, out WeldedVertex[] weldedVertices, bool includeTriangles = false) => CalculateMeshIslands(mesh, mesh.triangles, meshIslands, weldVertices, out weldedVertices, includeTriangles);

        public static List<MeshIsland> CalculateMeshIslands(Mesh mesh, int[] triangles, List<MeshIsland> meshIslands = null, bool weldVertices = true, bool includeTriangles = false) => CalculateMeshIslands(mesh, triangles, meshIslands, weldVertices, out _, includeTriangles);
        public static List<MeshIsland> CalculateMeshIslands(Mesh mesh, int[] triangles, List<MeshIsland> meshIslands, bool weldVertices, out WeldedVertex[] weldedVertices, bool includeTriangles = false)
        {
            if (triangles == null) triangles = mesh.triangles;
            weldedVertices = weldVertices ? MeshDataTools.WeldVertices(mesh.vertices) : null;

            return CalculateMeshIslands(mesh.vertexCount, triangles, meshIslands, weldedVertices, includeTriangles); 
        }
        public static List<MeshIsland> CalculateMeshIslands(int vertexCount, int[] triangles, List<MeshIsland> meshIslands = null, WeldedVertex[] weldedVertices = null, bool includeTriangles = false)
        {
            if (meshIslands == null) meshIslands = new List<MeshIsland>();

            bool weldVertices = weldedVertices != null;

            HashSet<int> currentIsland = new HashSet<int>();
            HashSet<Triangle> currentIslandTris = new HashSet<Triangle>();
            void CompleteIsland()
            {
                if (currentIsland.Count <= 0) return; // don't create empty mesh islands

                var vertexIndices = new int[currentIsland.Count];
                currentIsland.CopyTo(vertexIndices);
                var islandTris = includeTriangles ? new Triangle[currentIslandTris.Count] : null;
                if (includeTriangles) currentIslandTris.CopyTo(islandTris);

                meshIslands.Add(new MeshIsland() { color = UnityEngine.Random.ColorHSV(), vertices = vertexIndices, triangles = islandTris }); 
                currentIsland.Clear();
                currentIslandTris.Clear();
            }

            int GetWeldedIndex(int originalVertexIndex)
            {
                if (!weldVertices) return originalVertexIndex;
                return weldedVertices[originalVertexIndex].firstIndex;
            }

            int[] vertexIslandIndices = new int[vertexCount];
            for (int a = 0; a < vertexIslandIndices.Length; a++) vertexIslandIndices[a] = -1;
            int[] triangleIslandIndices = new int[triangles.Length / 3];
            for (int a = 0; a < triangleIslandIndices.Length; a++) triangleIslandIndices[a] = -1; 

            int islandIndex = 0;
            void AddVertexToIsland(int vertexIndex)
            {
                if (weldVertices)
                {
                    var mv = weldedVertices[vertexIndex];
                    for (int c = 0; c < mv.indices.Count; c++)
                    {
                        var mergedIndex = mv.indices[c];
                        vertexIslandIndices[mergedIndex] = islandIndex;

                        currentIsland.Add(mergedIndex);
                    }
                }
                else
                {
                    vertexIslandIndices[vertexIndex] = islandIndex;
                    currentIsland.Add(vertexIndex);
                }
            }

            int minTriIndex = 0;
            int nextTriIndex = 0;
            bool flag = true;
            while(flag)
            {
                int triIndex = nextTriIndex / 3;
                triangleIslandIndices[triIndex] = islandIndex; 

                int v0 = triangles[nextTriIndex];
                int v1 = triangles[nextTriIndex + 1];
                int v2 = triangles[nextTriIndex + 2];  

                AddVertexToIsland(v0);
                AddVertexToIsland(v1);
                AddVertexToIsland(v2);

                currentIslandTris.Add(new Triangle(triIndex, v0, v1, v2));

                flag = false;
                for (int a = minTriIndex; a < triangles.Length; a += 3)
                {
                    int i0 = GetWeldedIndex(triangles[a]);
                    int i1 = GetWeldedIndex(triangles[a + 1]);
                    int i2 = GetWeldedIndex(triangles[a + 2]);

                    var island0 = vertexIslandIndices[i0];
                    var island1 = vertexIslandIndices[i1];
                    var island2 = vertexIslandIndices[i2];

                    var triIsland = triangleIslandIndices[a / 3];

                    if ((triIsland < 0 || island0 < 0 || island1 < 0 || island2 < 0) && (island0 == islandIndex || island1 == islandIndex || island2 == islandIndex))
                    {
                        nextTriIndex = a;
                        flag = true;
                        break;
                    }
                }

                if (!flag)
                {
                    CompleteIsland();
                    nextTriIndex = -1;
                    islandIndex = meshIslands.Count;

                    for (int a = minTriIndex; a < triangles.Length; a += 3)
                    {
                        int i0 = triangles[a];
                        int i1 = triangles[a + 1];
                        int i2 = triangles[a + 2];

                        var island0 = vertexIslandIndices[i0];
                        var island1 = vertexIslandIndices[i1];
                        var island2 = vertexIslandIndices[i2];

                        var triIsland = triangleIslandIndices[a / 3];

                        if (triIsland < 0 || island0 < 0 || island1 < 0 || island2 < 0)   
                        {
                            nextTriIndex = a;
                            flag = true;
                            break;
                        } 
                        else
                        {
                            minTriIndex = a;
                        }
                    }
                }
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

        [Serializable]
        public struct MeshQuad
        {
            public int triA;
            public int triB;

            public int i0;
            public int i1;
            public int i2;
            public int i3;

            public int sharedEdge0;
            public int sharedEdge1;

            public int TriangleIndexA => triA / 3;
            public int TriangleIndexB => triB / 3;
        }

        public static List<MeshQuad> CalculateMeshQuads(int[] meshTriangles, List<MeshQuad> outputList = null)
        {
            if (outputList == null) outputList = new List<MeshQuad>(); 

            for(int a = 0; a < meshTriangles.Length; a += 3)
            {
                if (a + 5 >= meshTriangles.Length) break;

                int iA0 = meshTriangles[a];
                int iA1 = meshTriangles[a + 1];
                int iA2 = meshTriangles[a + 2];

                int iB0 = meshTriangles[a + 3];
                int iB1 = meshTriangles[a + 4];
                int iB2 = meshTriangles[a + 5];

                bool shares0 = iB0 == iA0 || iB0 == iA1 || iB0 == iA2;
                bool shares1 = iB1 == iA0 || iB1 == iA1 || iB1 == iA2;
                bool shares2 = iB2 == iA0 || iB2 == iA1 || iB2 == iA2;

                if (!((shares0 && shares1) || (shares1 && shares2) || (shares0 && shares2))) continue;

                int sharedEdge0 = iA1;
                int sharedEdge1 = iA2;
                if (shares0)
                {
                    sharedEdge0 = iB0;

                    if (shares1)
                    {
                        sharedEdge1 = iB1;
                    } 
                    else
                    {
                        sharedEdge1 = iB2;
                    }
                }
                else if (shares1)
                {
                    sharedEdge0 = iB1;

                    if (shares0)
                    {
                        sharedEdge1 = iB0;
                    }
                    else
                    {
                        sharedEdge1 = iB2;
                    }
                }
                else
                {
                    sharedEdge0 = iB2;

                    if (shares0)
                    {
                        sharedEdge1 = iB0;
                    }
                    else
                    {
                        sharedEdge1 = iB1; 
                    }
                }

                outputList.Add(new MeshQuad()
                {
                    triA = iA0,
                    triB = iB0,
                    i0 = iA0,
                    i1 = iA1,
                    i2 = iA2,
                    i3 = (!shares0 ? iB0 : (!shares1 ? iB1 : (!shares2 ? iB2 : iB0))),
                    sharedEdge0 = sharedEdge0,
                    sharedEdge1 = sharedEdge1
                });

                a += 3; // skip next tri since it is part of this quad
            }

            return outputList;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool HasBoundingCenterBetweenTris(float3 E1_flat, float3 E2_flat, float3 normalA_raw, float3 normalA, float3 vA0_flat, float3 vA1_flat, float3 vA2_flat, float3 vB0, float3 vB1, float3 vB2, out float3 boundingCenter)
        {
            boundingCenter = float3.zero;

            float3 normalB = Maths.calcNormal(vB0, vB1, vB2);
            if (math.dot(normalA, normalB) >= 0) return false;

            float3 vB0_flat = Vector3.ProjectOnPlane(vB0, normalA);
            float3 vB1_flat = Vector3.ProjectOnPlane(vB1, normalA);
            float3 vB2_flat = Vector3.ProjectOnPlane(vB2, normalA);

            float3 centerB_flat = (vB0_flat + vB1_flat + vB2_flat) / 3f;
            vB0_flat = ((vB0_flat - centerB_flat) * 0.999f) + centerB_flat;
            vB1_flat = ((vB1_flat - centerB_flat) * 0.999f) + centerB_flat;
            vB2_flat = ((vB2_flat - centerB_flat) * 0.999f) + centerB_flat;

            float t;

            bool intersectI = false;
            bool intersectJ = false;
            bool intersectK = false;

            float3 pointI0 = float3.zero;
            float3 pointI1 = float3.zero;

            float3 pointJ0 = float3.zero;
            float3 pointJ1 = float3.zero;

            float3 pointK0 = float3.zero;
            float3 pointK1 = float3.zero;

            bool flag = false;
            if (Maths.IsInTriangle(vB0_flat, vA0_flat, vA1_flat, vA2_flat, 0.01f))
            {
                intersectI = true;
                pointI0 = pointI1 = vB0;
                flag = true;
            }
            if (Maths.IsInTriangle(vB1_flat, vA0_flat, vA1_flat, vA2_flat, 0.01f))
            {
                intersectJ = true;
                pointJ0 = pointJ1 = vB1;
                flag = true;
            }
            if (Maths.IsInTriangle(vB2_flat, vA0_flat, vA1_flat, vA2_flat, 0.01f))
            {
                intersectK = true;
                pointK0 = pointK1 = vB2;
                flag = true;
            }
            
            if (!flag)
            {
                float3 edge0 = vB1 - vB0;
                float3 edge0_flat = vB1_flat - vB0_flat;
                intersectI = Maths.seg_intersect_triangle(E1_flat, E2_flat, normalA_raw, vB0_flat, edge0_flat, vA0_flat, vA1_flat, vA2_flat, out t, out _, out _);
                pointI0 = vB0 + edge0 * t;
                intersectI = Maths.seg_intersect_triangle(E1_flat, E2_flat, normalA_raw, vB1_flat, -edge0_flat, vA0_flat, vA1_flat, vA2_flat, out t, out _, out _) && intersectI;
                pointI1 = vB1 - edge0 * t;


                float3 edge1 = vB2 - vB0;
                float3 edge1_flat = vB2_flat - vB0_flat;
                intersectJ = Maths.seg_intersect_triangle(E1_flat, E2_flat, normalA_raw, vB0_flat, edge1_flat, vA0_flat, vA1_flat, vA2_flat, out t, out _, out _);
                pointJ0 = vB0 + edge1 * t;
                intersectJ = Maths.seg_intersect_triangle(E1_flat, E2_flat, normalA_raw, vB2_flat, -edge1_flat, vA0_flat, vA1_flat, vA2_flat, out t, out _, out _) && intersectJ;
                pointJ1 = vB2 - edge1 * t;


                float3 edge2 = vB2 - vB1;
                float3 edge2_flat = vB2_flat - vB1_flat;
                intersectK = Maths.seg_intersect_triangle(E1_flat, E2_flat, normalA_raw, vB1_flat, edge2_flat, vA0_flat, vA1_flat, vA2_flat, out t, out _, out _);
                pointK0 = vB1 + edge2 * t;
                intersectK = Maths.seg_intersect_triangle(E1_flat, E2_flat, normalA_raw, vB2_flat, -edge2_flat, vA0_flat, vA1_flat, vA2_flat, out t, out _, out _) && intersectK;
                pointK1 = vB2 - edge2 * t; 
            }

            bool3 intersects = new bool3(intersectI, intersectJ, intersectK);
            float3 contr = math.select(float3.zero, new float3(1f, 1f, 1f), intersects);

            boundingCenter = ((pointI0 + pointI1) * 0.5f * contr.x) + ((pointJ0 + pointJ1) * 0.5f * contr.y) + ((pointK0 + pointK1) * 0.5f * contr.z);

            float totalContr = contr.x + contr.y + contr.z;
            boundingCenter = math.select(boundingCenter, boundingCenter / totalContr, totalContr > 0f);

            return math.any(intersects);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 CalculateTriBoundingCenter(float4x4 debug, int triIndex, NativeArray<int> triangles, NativeArray<float3> vertices)
        {
            int i0 = triangles[triIndex];
            int i1 = triangles[triIndex + 1];
            int i2 = triangles[triIndex + 2];

            float3 v0 = vertices[i0];
            float3 v1 = vertices[i1];
            float3 v2 = vertices[i2];

            float3 triCenter = (v0 + v1 + v2) / 3f;

            float3 normalRaw = Maths.calcNormal(v0, v1, v2); 
            float3 normal = math.normalizesafe(normalRaw);

            float3 v0_flat = Vector3.ProjectOnPlane(v0, normal);
            float3 v1_flat = Vector3.ProjectOnPlane(v1, normal);
            float3 v2_flat = Vector3.ProjectOnPlane(v2, normal);

            float3 E1_flat = v1_flat - v0_flat;
            float3 E2_flat = v2_flat - v0_flat;

            float4 closestCenter = new float4(triCenter, float.MaxValue);
            for (int tri = 0; tri < triIndex; tri += 3)
            {
                int i3 = triangles[tri];
                int i4 = triangles[tri + 1];
                int i5 = triangles[tri + 2];

                bool containsOrIntersects = HasBoundingCenterBetweenTris(E1_flat, E2_flat, normalRaw, normal, v0_flat, v1_flat, v2_flat, vertices[i3], vertices[i4], vertices[i5], out float3 boundingCenter);
                float dist = math.distance(triCenter, boundingCenter);

                bool isCloser = containsOrIntersects && dist < closestCenter.w;
                closestCenter = math.select(closestCenter, new float4(boundingCenter, dist), isCloser);

                //if (containsOrIntersects)
                //{
                //    Debug.DrawLine(math.transform(debug, triCenter), math.transform(debug, boundingCenter), Color.red, 60);
                //    Debug.DrawLine(math.transform(debug, boundingCenter), math.transform(debug, (vertices[tri] + vertices[tri + 1] + vertices[tri + 2]) / 3f), Color.yellow, 60);
                //}
            } 
            // split to avoid local tri index
            for (int tri = triIndex+3; tri < triangles.Length; tri += 3)
            {
                int i3 = triangles[tri];
                int i4 = triangles[tri + 1];
                int i5 = triangles[tri + 2];

                bool containsOrIntersects = HasBoundingCenterBetweenTris(E1_flat, E2_flat, normalRaw, normal, v0_flat, v1_flat, v2_flat, vertices[i3], vertices[i4], vertices[i5], out float3 boundingCenter);
                float dist = math.distance(triCenter, boundingCenter);

                bool isCloser = containsOrIntersects && dist < closestCenter.w;
                closestCenter = math.select(closestCenter, new float4(boundingCenter, dist), isCloser);

                //if (containsOrIntersects)
                //{
                //    Debug.DrawLine(math.transform(debug, triCenter), math.transform(debug, boundingCenter), Color.red, 60);
                //    Debug.DrawLine(math.transform(debug, boundingCenter), math.transform(debug, (vertices[tri] + vertices[tri + 1] + vertices[tri + 2]) / 3f), Color.yellow, 60);
                //}
            }

            return (closestCenter.xyz + triCenter) * 0.5f;
        }




        [MethodImpl(MethodImplOptions.AggressiveInlining), Obsolete("Not giving correct results")]
        private static bool HasBoundingCenterBetweenTris2(float3 normalA, float3 tangent, float3 bitangent, float3 vA0, float3 vA1, float3 vA2, float2 vA0_flat, float2 vA1_flat, float2 vA2_flat, float3 vB0, float3 vB1, float3 vB2, out float3 boundingCenter)
        {
            boundingCenter = float3.zero;

            float3 normalB = Maths.calcNormal(vB0, vB1, vB2);
            if (math.dot(normalA, normalB) >= 0) return false;

            float3 centerB = (vB0 + vB1+ vB2) / 3f;
            vB0 = ((vB0 - centerB) * 0.999f) + centerB;
            vB1 = ((vB1 - centerB) * 0.999f) + centerB;
            vB2 = ((vB2 - centerB) * 0.999f) + centerB;


            float2 vB0_flat = new float2(math.dot(vB0, tangent), math.dot(vB0, bitangent));
            float2 vB1_flat = new float2(math.dot(vB1, tangent), math.dot(vB1, bitangent));
            float2 vB2_flat = new float2(math.dot(vB2, tangent), math.dot(vB2, bitangent));


            bool intersectI = false;
            bool intersectJ = false;
            bool intersectK = false;

            float3 pointI = float3.zero;
            float3 pointJ = float3.zero;
            float3 pointK = float3.zero;

            bool flag = false;
            if (Maths.IsInTriangle(vB0, vA0, vA1, vA2, 0.01f))
            {
                intersectI = true;
                pointI = vB0;
                flag = true;
            }
            if (Maths.IsInTriangle(vB1, vA0, vA1, vA2, 0.01f))
            {
                intersectJ = true;
                pointJ = vB1;
                flag = true;
            }
            if (Maths.IsInTriangle(vB2, vA0, vA1, vA2, 0.01f))
            {
                intersectK = true;
                pointK = vB2;
                flag = true;
            }

            if (!flag)
            {
                bool3 flags;
                float2 ip, ip0, ip1, ip2;


                flags = Maths.seg_intersect_tri(vB0_flat, vB1_flat, vA0_flat, vA1_flat, vA2_flat, out ip0, out ip1, out ip2);
                intersectI = math.any(flags);
                if (intersectI)
                {
                    float3 counts = math.select(float3.zero, new float3(1f, 1f, 1f), flags);
                    float count = counts.x + counts.y + counts.z;

                    ip = (math.select(float2.zero, ip0, flags.x) + math.select(float2.zero, ip1, flags.y) + math.select(float2.zero, ip2, flags.z)) / count;
                    pointI = (tangent * ip.x) + (bitangent * ip.y);
                }


                flags = Maths.seg_intersect_tri(vB1_flat, vB2_flat, vA0_flat, vA1_flat, vA2_flat, out ip0, out ip1, out ip2);
                intersectJ = math.any(flags);
                if (intersectJ)
                {
                    float3 counts = math.select(float3.zero, new float3(1f, 1f, 1f), flags);
                    float count = counts.x + counts.y + counts.z;

                    ip = (math.select(float2.zero, ip0, flags.x) + math.select(float2.zero, ip1, flags.y) + math.select(float2.zero, ip2, flags.z)) / count;
                    pointJ = (tangent * ip.x) + (bitangent * ip.y);
                }


                flags = Maths.seg_intersect_tri(vB2_flat, vB0_flat, vA0_flat, vA1_flat, vA2_flat, out ip0, out ip1, out ip2);
                intersectK = math.any(flags);
                if (intersectK)
                {
                    float3 counts = math.select(float3.zero, new float3(1f, 1f, 1f), flags);
                    float count = counts.x + counts.y + counts.z;

                    ip = (math.select(float2.zero, ip0, flags.x) + math.select(float2.zero, ip1, flags.y) + math.select(float2.zero, ip2, flags.z)) / count;
                    pointK = (tangent * ip.x) + (bitangent * ip.y); 
                }
            }

            bool3 intersects = new bool3(intersectI, intersectJ, intersectK);
            float3 contr = math.select(float3.zero, new float3(1f, 1f, 1f), intersects);
            float totalContr = contr.x + contr.y + contr.z;

            boundingCenter = (pointI * contr.x) + (pointJ * contr.y) + (pointK * contr.z);

            boundingCenter = math.select(boundingCenter, boundingCenter / totalContr, totalContr > 0f); 

            return math.any(intersects);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining), Obsolete("Not giving correct results")]
        public static float3 CalculateTriBoundingCenter2(float4x4 debug, int triIndex, NativeArray<int> triangles, NativeArray<float3> vertices)
        {
            int i0 = triangles[triIndex];
            int i1 = triangles[triIndex + 1];
            int i2 = triangles[triIndex + 2];

            float3 v0 = vertices[i0];
            float3 v1 = vertices[i1];
            float3 v2 = vertices[i2];

            float3 triCenter = (v0 + v1 + v2) / 3f;

            float3 normalRaw = Maths.calcNormal(v0, v1, v2);
            float3 normal = math.normalizesafe(normalRaw);
            float3 tangent = math.normalizesafe(v1 - v0);
            float3 bitangent = math.normalizesafe(math.cross(normal, tangent));

            float2 v0_flat = new float2(math.dot(v0, tangent), math.dot(v0, bitangent));
            float2 v1_flat = new float2(math.dot(v1, tangent), math.dot(v1, bitangent));
            float2 v2_flat = new float2(math.dot(v2, tangent), math.dot(v2, bitangent));

            float4 closestCenter = new float4(triCenter, float.MaxValue);
            for (int tri = 0; tri < triIndex; tri += 3)
            {
                int i3 = triangles[tri];
                int i4 = triangles[tri + 1];
                int i5 = triangles[tri + 2];

                bool containsOrIntersects = HasBoundingCenterBetweenTris2(normal, tangent, bitangent, v0, v1, v2, v0_flat, v1_flat, v2_flat, vertices[i3], vertices[i4], vertices[i5], out float3 boundingCenter);
                float dist = math.distance(triCenter, boundingCenter);

                bool isCloser = containsOrIntersects && dist < closestCenter.w;
                closestCenter = math.select(closestCenter, new float4(boundingCenter, dist), isCloser); 

                //if (containsOrIntersects)
                //{
                //    Debug.DrawLine(math.transform(debug, triCenter), math.transform(debug, boundingCenter), Color.red, 60);
                //    Debug.DrawLine(math.transform(debug, boundingCenter), math.transform(debug, (vertices[tri] + vertices[tri + 1] + vertices[tri + 2]) / 3f), Color.yellow, 60);
                //}
            }
            // split to avoid local tri index
            for (int tri = triIndex + 3; tri < triangles.Length; tri += 3)
            {
                int i3 = triangles[tri];
                int i4 = triangles[tri + 1];
                int i5 = triangles[tri + 2];

                bool containsOrIntersects = HasBoundingCenterBetweenTris2(normal, tangent, bitangent, v0, v1, v2, v0_flat, v1_flat, v2_flat, vertices[i3], vertices[i4], vertices[i5], out float3 boundingCenter);
                float dist = math.distance(triCenter, boundingCenter);

                bool isCloser = containsOrIntersects && dist < closestCenter.w;
                closestCenter = math.select(closestCenter, new float4(boundingCenter, dist), isCloser);

                //if (containsOrIntersects)
                //{
                //    Debug.DrawLine(math.transform(debug, triCenter), math.transform(debug, boundingCenter), Color.red, 60);
                //    Debug.DrawLine(math.transform(debug, boundingCenter), math.transform(debug, (vertices[tri] + vertices[tri + 1] + vertices[tri + 2]) / 3f), Color.yellow, 60);
                //}
            }

            return (closestCenter.xyz + triCenter) * 0.5f;
        }


        [BurstCompile]
        public struct CalculateTriBoundingCentersJob : IJobParallelFor
        {

            public float4x4 debug;

            [ReadOnly]
            public NativeArray<int> triangles;
            [ReadOnly]
            public NativeArray<float3> vertices;

            [NativeDisableParallelForRestriction]
            public NativeArray<float3> centers;

            public void Execute(int triIndex)
            {
                centers[triIndex] = CalculateTriBoundingCenter(debug, triIndex * 3, triangles, vertices);
            }
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
                                        var frame = (a >= shape.frames.Length ? default : shape.frames[a]);
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
                    foreach (var shape in secondMesh.blendShapes)
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
            var weldedVertices = WeldVertices(editMesh.VertexList);
            
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

        #region Mesh Creation

        [Serializable]
        public enum GeneratedPlaneOrientation
        {
            XY, XZ, ZY, YX, ZX, YZ
        }

        public static Mesh GeneratePlaneMesh(int faceCountHorizontal, int faceCountVertical, float sizeMetersHor, float sizeMetersVer, GeneratedPlaneOrientation orientation = GeneratedPlaneOrientation.XZ, bool flipFaces = false, float depth = 0, float centerX = 0.5f, float centerY = 0.5f, bool flipUV_u = false, bool flipUV_v = false) => GeneratePlaneMesh(faceCountHorizontal, faceCountVertical, sizeMetersHor, sizeMetersVer, out _, out _, orientation, flipFaces, depth, centerX, centerY, flipUV_u, flipUV_v);
        public static Mesh GeneratePlaneMesh(int faceCountHorizontal, int faceCountVertical, float sizeMetersHor, float sizeMetersVer, out List<int> boundaryIndices, out List<MeshQuad> quads, GeneratedPlaneOrientation orientation = GeneratedPlaneOrientation.XZ, bool flipFaces = false, float depth = 0, float centerX = 0.5f, float centerY = 0.5f, bool flipUV_u = false, bool flipUV_v = false)
        {
            faceCountHorizontal = Mathf.Max(1, faceCountHorizontal);
            faceCountVertical = Mathf.Max(1, faceCountVertical);

            int columns = Mathf.Max(2, (faceCountHorizontal * 2) - 1);
            int rows = Mathf.Max(2, (faceCountVertical * 2) - 1);

            int columnsM1 = columns - 1;
            int rowsM1 = rows - 1;

            float columnsM1f = columns - 1f;
            float rowsM1f = rows - 1f;

            switch (orientation)
            {
                case GeneratedPlaneOrientation.YX:
                case GeneratedPlaneOrientation.ZX:
                case GeneratedPlaneOrientation.YZ:
                    flipFaces = !flipFaces;
                    break;
            }

            Mesh mesh = new Mesh();

            List<int> triangles = new List<int>();
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            boundaryIndices = new List<int>();
            quads = new List<MeshQuad>();

            int GetVertexIndex(int x, int y) => (y * columns) + x; 

            for (int y = 0; y < rows; y++)
            {
                float v = y / rowsM1f;
                for (int x = 0; x < columns; x++)
                {
                    int index = GetVertexIndex(x, y);
                    
                    bool isBoundary = x == 0 || y == 0 || x == columnsM1 || y == rowsM1;

                    float u = x / columnsM1f;

                    uvs.Add(new Vector2(flipUV_u ? 1f - u : u, flipUV_v ? (1f - v) : v));

                    float u_ = u - centerX;
                    float v_ = v - centerY; 
                    switch (orientation)
                    {
                        case GeneratedPlaneOrientation.XY:
                            vertices.Add(new Vector3(u_ * sizeMetersHor, v_ * sizeMetersVer, depth));
                            normals.Add(flipFaces ? Vector3.forward : Vector3.back);
                            break;

                        case GeneratedPlaneOrientation.XZ:
                            vertices.Add(new Vector3(u_ * sizeMetersHor, depth, v_ * sizeMetersVer));
                            normals.Add(flipFaces ? Vector3.down : Vector3.up);
                            break;

                        case GeneratedPlaneOrientation.ZY:
                            vertices.Add(new Vector3(depth, v_ * sizeMetersVer, u_ * sizeMetersHor));
                            normals.Add(flipFaces ? Vector3.left : Vector3.right);
                            break;

                        case GeneratedPlaneOrientation.YX:
                            vertices.Add(new Vector3(v_ * sizeMetersVer, u_ * sizeMetersHor,  depth)); 
                            normals.Add(flipFaces ? Vector3.back : Vector3.forward);  
                            break;

                        case GeneratedPlaneOrientation.ZX:
                            vertices.Add(new Vector3(v_ * sizeMetersVer, depth, u_ * sizeMetersHor)); 
                            normals.Add(flipFaces ? Vector3.up : Vector3.down);
                            break;

                        case GeneratedPlaneOrientation.YZ:
                            vertices.Add(new Vector3(depth, u_ * sizeMetersHor, v_ * sizeMetersVer)); 
                            normals.Add(flipFaces ? Vector3.right : Vector3.left);
                            break;
                    }

                    if (y < rowsM1 && x < columnsM1)
                    {

                        MeshQuad quad = default;

                        int i0 = index;
                        int i1 = GetVertexIndex(x + 1, y);
                        int i2 = GetVertexIndex(x, y + 1);
                        int i3 = GetVertexIndex(x + 1, y + 1);

                        quad.i0 = i0;
                        quad.i1 = i1;
                        quad.i2 = i2;
                        quad.i3 = i3;

                        quad.sharedEdge0 = i1;
                        quad.sharedEdge1 = i2; 

                        if (flipFaces)
                        {
                            quad.triA = triangles.Count;
                            triangles.Add(i0);
                            triangles.Add(i1);
                            triangles.Add(i2);

                            quad.triB = triangles.Count;
                            triangles.Add(i1);
                            triangles.Add(i3);
                            triangles.Add(i2);
                        }
                        else
                        {
                            quad.triA = triangles.Count;
                            triangles.Add(i0);
                            triangles.Add(i2);
                            triangles.Add(i1); 

                            quad.triB = triangles.Count;
                            triangles.Add(i1);
                            triangles.Add(i2);
                            triangles.Add(i3);
                        }

                        quads.Add(quad);
                    }

                    if (isBoundary)
                    {
                        boundaryIndices.Add(index);
                    }
                }
            }

            mesh.indexFormat = vertices.Count >= 65534 ? IndexFormat.UInt32 : IndexFormat.UInt16;   

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);

            mesh.subMeshCount = 1;
            mesh.SetTriangles(triangles, 0); 

            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        #endregion

        #region Mesh Baking

        [Serializable]
        public class MeshBakeTexture
        {
            public Texture2D texture;
            public int width;
            public int height;

            public bool isNormalMap;

            public int Width => texture == null ? width : texture.width;
            public int Height => texture == null ? height : texture.height;

            [NonSerialized]
            public Color[] pixels;
            public Color[] Pixels
            {
                get
                {
                    if (pixels == null && texture != null) pixels = texture.GetPixels();
                    return pixels;
                }
            }

            public Color defaultPixel;
        }
        [Serializable]
        public struct MeshBakeTextureToTexture
        {
            public MeshBakeTexture referenceTex;
            public MeshBakeTexture destinationTex;

            public bool clearDestinationPixels;
        }

        public static void BakeMeshToTextures(
            Matrix4x4 worldToLocal,
            IEnumerable<Vector3> worldVerticesToBake, IEnumerable<Vector3> worldNormalsToBake, IEnumerable<Vector4> worldTangentsToBake, IEnumerable<int> trianglesToBake, IEnumerable<Vector2> uvsToBake, // Reference
            IEnumerable<Vector3> worldVerticesDest, IEnumerable<Vector3> worldNormalsDest, IEnumerable<Vector4> worldTangentsDest, IEnumerable<int> trianglesDest, IEnumerable<Vector2> uvsDest, // Destination
            IEnumerable<MeshBakeTextureToTexture> textures, IEnumerable<float> alphaValues, Vector2Int alphaTexDimensions, float alphaClipThreshold = 0.5f, bool doubleSided = false, float triTestErrorMargin = 0.01f, float depthThreshold = float.MinValue, bool flipDepth = false, List<MeshIsland> meshIslands = null)
        { 
            
            List<float3> worldVerticesToBake_ = new List<float3>();
            foreach (var val in worldVerticesToBake) worldVerticesToBake_.Add(val);
            List<float3> worldNormalsToBake_ = new List<float3>();
            foreach (var val in worldNormalsToBake) worldNormalsToBake_.Add(val);
            List<float4> worldTangentsToBake_ = new List<float4>();
            foreach (var val in worldTangentsToBake) worldTangentsToBake_.Add(val); 
            List<float2> uvsToBake_ = new List<float2>();
            foreach (var val in uvsToBake) uvsToBake_.Add(val);

            List<float3> worldVerticesDest_ = new List<float3>();
            foreach (var val in worldVerticesDest) worldVerticesDest_.Add(val);
            List<float3> worldNormalsDest_ = new List<float3>();
            foreach (var val in worldNormalsDest) worldNormalsDest_.Add(val);
            List<float4> worldTangentsDest_ = new List<float4>();
            foreach (var val in worldTangentsDest) worldTangentsDest_.Add(val);
            List<float2> uvsDest_ = new List<float2>();
            foreach (var val in uvsDest) uvsDest_.Add(val);

            BakeMeshToTextures(
                (float4x4)worldToLocal,
                worldVerticesToBake_, worldNormalsToBake_, worldTangentsToBake_, trianglesToBake, uvsToBake_,
                worldVerticesDest_, worldNormalsDest_, worldTangentsDest_, trianglesDest, uvsDest_,
                textures, alphaValues, new int2(alphaTexDimensions.x, alphaTexDimensions.y), alphaClipThreshold, doubleSided, triTestErrorMargin, depthThreshold, flipDepth,
                meshIslands  
                );
        }
            public static void BakeMeshToTextures(
            float4x4 worldToLocal,
            IEnumerable<float3> worldVerticesToBake, IEnumerable<float3> worldNormalsToBake, IEnumerable<float4> worldTangentsToBake, IEnumerable<int> trianglesToBake, IEnumerable<float2> uvsToBake, // Reference
            IEnumerable<float3> worldVerticesDest, IEnumerable<float3> worldNormalsDest, IEnumerable<float4> worldTangentsDest, IEnumerable<int> trianglesDest, IEnumerable<float2> uvsDest, // Destination
            IEnumerable<MeshBakeTextureToTexture> textures, IEnumerable<float> alphaValues, int2 alphaTexDimensions, float alphaClipThreshold = 0.5f, bool doubleSided = false, float triTestErrorMargin = 0.01f, float depthThreshold = float.MinValue, bool flipDepth = false, List<MeshIsland> meshIslands = null)
        {
            int[] trianglesDest_ = trianglesDest.AsManagedArray();
             
            if (meshIslands == null) meshIslands = CalculateMeshIslands(worldVerticesDest.GetCount(), trianglesDest_, null, null, true);
            if (meshIslands == null || meshIslands.Count <= 0) return;

            NativeList<int> meshIslandTris = new NativeList<int>(trianglesDest_.Length, Allocator.Persistent);
            //NativeArray<int2> meshIslandTriIndexBounds = new NativeArray<int2>(meshIslands.Count, Allocator.Persistent);
            for (int i = 0; i < meshIslands.Count; i++)
            {
                var meshIsland = meshIslands[i];
                if (meshIsland.triangles == null) continue;

                //int startIndex = meshIslandTris.Length;
                for (int a = 0; a < meshIsland.triangles.Length; a++)
                {
                    var tri = meshIsland.triangles[a];
                    meshIslandTris.Add(tri.i0);
                    meshIslandTris.Add(tri.i1);
                    meshIslandTris.Add(tri.i2);   
                }

                //meshIslandTriIndexBounds[i] = new int2(startIndex, meshIslandTris.Length - 1);
            }

            float flipDepth_ = flipDepth ? 1f : -1f;
            float minFaceNormalDot = doubleSided ? -1f : 0f;

            NativeArray<float3> worldVerticesToBake_ = worldVerticesToBake.AsNativeArray(out bool dispose_worldVerticesToBake);
            NativeArray<float2> uvsToBake_ = uvsToBake.AsNativeArray(out bool dispose_uvsToBake);

            NativeArray<int> trianglesToBake_ = trianglesToBake.AsNativeArray(out bool dispose_trianglesToBake);
            NativeArray<float3> triangleNormalsToBake = new NativeArray<float3>(trianglesToBake_.Length/3, Allocator.Persistent);
            for (int a = 0; a < trianglesToBake_.Length; a += 3)
            {
                int ref_i0 = trianglesToBake_[a];
                int ref_i1 = trianglesToBake_[a + 1];
                int ref_i2 = trianglesToBake_[a + 2];

                float3 ref_v0 = worldVerticesToBake_[ref_i0];
                float3 ref_v1 = worldVerticesToBake_[ref_i1];
                float3 ref_v2 = worldVerticesToBake_[ref_i2];

                triangleNormalsToBake[a / 3] = math.normalizesafe(math.cross(ref_v1 - ref_v0, ref_v2 - ref_v0));
            }

            NativeArray<float> alphaValues_ = alphaValues.AsNativeArray(out bool dispose_alphaValues);



            NativeArray<float3> worldVerticesDest_ = worldVerticesDest.AsNativeArray(out bool dispose_worldVerticesDest);
            NativeArray<float2> uvsDest_ = uvsDest.AsNativeArray(out bool dispose_uvsDest);

            NativeArray<float3> triangleNormalsDest = new NativeArray<float3>(trianglesDest_.Length / 3, Allocator.Persistent);
            for (int a = 0; a < trianglesDest_.Length; a += 3)
            {
                int ref_i0 = trianglesDest_[a];
                int ref_i1 = trianglesDest_[a + 1];
                int ref_i2 = trianglesDest_[a + 2];

                float3 ref_v0 = worldVerticesDest_[ref_i0];
                float3 ref_v1 = worldVerticesDest_[ref_i1];
                float3 ref_v2 = worldVerticesDest_[ref_i2];

                triangleNormalsDest[a / 3] = math.normalizesafe(math.cross(ref_v1 - ref_v0, ref_v2 - ref_v0)); 
            } 


            NativeQueue<TriPixel> triPixels = new NativeQueue<TriPixel>(Allocator.Persistent);

            MeshBakeTextureToTexture prevTex2Tex = default;
            foreach (var tex2tex in textures)
            {
                triPixels.Clear();
                if (tex2tex.referenceTex == null || tex2tex.destinationTex == null) continue;;


                int2 texDimsRef = new int2(tex2tex.referenceTex.Width, tex2tex.referenceTex.Height); 

                if (tex2tex.destinationTex.texture == null)
                {
                    tex2tex.destinationTex.texture = new Texture2D(tex2tex.destinationTex.width <= 0 ? texDimsRef.x : tex2tex.destinationTex.width, tex2tex.destinationTex.height <= 0 ? texDimsRef.y : tex2tex.destinationTex.height, TextureFormat.RGBA32/*tex2tex.referenceTex.texture.format*/, tex2tex.referenceTex.texture.mipmapCount > 0, TextureUtils.IsLinear(tex2tex.referenceTex.texture));
                    tex2tex.destinationTex.pixels = null;
                }

                int width = tex2tex.destinationTex.Width;
                int height = tex2tex.destinationTex.Height;
                int2 texDims = new int2(width, height);

                bool isNormalMap = tex2tex.destinationTex.isNormalMap = tex2tex.referenceTex.isNormalMap;

                var referencePixels = tex2tex.referenceTex.Pixels;
                if (referencePixels == null) continue;

                NativeArray<float4> referencePixels_ = new NativeArray<Color>(referencePixels, Allocator.Persistent).Reinterpret<float4>(); 
                
                var destinationPixels = tex2tex.destinationTex.Pixels;
                NativeArray<float4> destinationPixels_ = new NativeArray<Color>(destinationPixels, Allocator.Persistent).Reinterpret<float4>();  
                if (tex2tex.clearDestinationPixels)
                {
                    Debug.Log("Clearing pixels");
                    for (int a = 0; a < destinationPixels.Length; a++) destinationPixels_[a] = (Vector4)tex2tex.destinationTex.defaultPixel;
                }
                 
                var job = new PrepareTriPixelsJob()
                {
                    triTestErrorMargin = triTestErrorMargin,
                    uvs = uvsDest_,
                    texDims = texDims,
                    triangles = meshIslandTris,
                    triPixels = triPixels.AsParallelWriter()
                }.Schedule(meshIslandTris.Length / 3, 1, default); 

                NativeArray<TriPixel> triPixels_ = default;
                if (isNormalMap)
                {
                     
                } 
                else
                {
                    job.Complete();
                    triPixels_ = triPixels.ToArray(Allocator.TempJob);   
                    job = new BakeMeshToPixelsJob()
                    {

                        defaultPixel = (Vector4)tex2tex.destinationTex.defaultPixel, 
                        alphaClipThreshold = alphaClipThreshold,
                        flipDepth = flipDepth_,
                        minFaceNormalDot = minFaceNormalDot,
                        depthThreshold = depthThreshold,
                        triTestErrorMargin = triTestErrorMargin, 


                        texDimsReference = texDimsRef,
                        texDimsAlphaReference = alphaTexDimensions,


                        referenceTriangles = trianglesToBake_,
                        referenceTriNormals = triangleNormalsToBake,
                        referenceVertices = worldVerticesToBake_,
                        referenceUVs = uvsToBake_,
                        referencePixels = referencePixels_,
                        referencePixelAlphas = alphaValues_,


                        destVertices = worldVerticesDest_,
                        destTriNormals = triangleNormalsDest,


                        triPixels = triPixels_,
                        outputPixels = destinationPixels_,

                    }.Schedule(triPixels_.Length, 1, job);
                }

                if (prevTex2Tex.destinationTex != null && prevTex2Tex.destinationTex.pixels != null && prevTex2Tex.destinationTex.texture != null) // upload previous job output to previous texture while we wait for the current job.
                {
                    prevTex2Tex.destinationTex.texture.SetPixels(prevTex2Tex.destinationTex.pixels); 
                    prevTex2Tex.destinationTex.texture.Apply();
                    Debug.Log("Applying pixels"); 
                }

                job.Complete(); // wait for job completion if not finished
                if (triPixels_.IsCreated) triPixels_.Dispose(); 

                destinationPixels_.Reinterpret<Color>().CopyTo(destinationPixels);
                tex2tex.destinationTex.pixels = destinationPixels;

                prevTex2Tex = tex2tex;

                destinationPixels_.Dispose(); 
                referencePixels_.Dispose();   
            }

            if (prevTex2Tex.destinationTex != null && prevTex2Tex.destinationTex.pixels != null && prevTex2Tex.destinationTex.texture != null)
            {
                prevTex2Tex.destinationTex.texture.SetPixels(prevTex2Tex.destinationTex.pixels);
                prevTex2Tex.destinationTex.texture.Apply();  
            }

            triPixels.Dispose();

            if (dispose_alphaValues) alphaValues_.Dispose();

            if (dispose_worldVerticesToBake) worldVerticesToBake_.Dispose();   
            if (dispose_uvsToBake) uvsToBake_.Dispose();

            if (dispose_trianglesToBake) trianglesToBake_.Dispose();
            triangleNormalsToBake.Dispose();

            if (dispose_worldVerticesDest) worldVerticesDest_.Dispose();
            if (dispose_uvsDest) uvsDest_.Dispose();

            triangleNormalsDest.Dispose(); 


            meshIslandTris.Dispose(); 
            //meshIslandTriIndexBounds.Dispose();
        }

        private struct TriPixel : IEquatable<TriPixel>
        {
            public int triIndex;
            public int pixelIndex;
            public int2 pixelCoords;
            public int3 indices;
            public float3 barycentricCoords;

            public bool Equals(TriPixel other)
            {
                return pixelIndex == other.pixelIndex;
            }
        }
        [BurstCompile]
        private struct PrepareTriPixelsJob : IJobParallelFor
        {

            public float triTestErrorMargin; 
             
            public int2 texDims;

            [ReadOnly]
            public NativeList<int> triangles;
            [ReadOnly]
            public NativeArray<float2> uvs;

            [WriteOnly]
            public NativeQueue<TriPixel>.ParallelWriter triPixels;

            public void Execute(int index)
            {
                int tIndex = index * 3;
                int i0 = triangles[tIndex];
                int i1 = triangles[tIndex + 1];
                int i2 = triangles[tIndex + 2];

                int3 indices = new int3(i0, i1, i2);

                float2 dest_uv0 = Maths.wrap(uvs[indices.x], 1f);
                float2 dest_uv1 = Maths.wrap(uvs[indices.y], 1f);
                float2 dest_uv2 = Maths.wrap(uvs[indices.z], 1f);    

                float2 boundsMin = math.min(dest_uv0, math.min(dest_uv1, dest_uv2));
                float2 boundsMax = math.max(dest_uv0, math.max(dest_uv1, dest_uv2));
                float2 bounds = boundsMax - boundsMin;

                float2 dest_pix0 = dest_uv0 * texDims;
                float2 dest_pix1 = dest_uv1 * texDims;
                float2 dest_pix2 = dest_uv2 * texDims;

                int2 pixelsStart = new int2(math.floor(boundsMin * texDims));
                int2 pixelsXY = new int2(math.ceil(bounds * texDims));
                pixelsXY = math.min(texDims - pixelsStart, pixelsXY);
                int size = pixelsXY.x * pixelsXY.y; 

                for(int i = 0; i < size; i++)
                //for (int x = 0; x <= pixelsXY.x; x++)
                {
                    //for (int y = 0; y <= pixelsXY.y; y++) 
                    //{
                        int x = i % pixelsXY.x;
                        int y = i / pixelsXY.x;

                        var pixCoords = new int2(x, y) + pixelsStart;
                        var coords = Maths.BarycentricCoords2D(pixCoords, dest_pix0, dest_pix1, dest_pix2); 

                        if (Maths.IsInTriangle2D(coords, triTestErrorMargin))
                        {
                            triPixels.Enqueue(new TriPixel()
                            {
                                triIndex = index,
                                pixelIndex = (pixCoords.y * texDims.x) + pixCoords.x,
                                pixelCoords = pixCoords,
                                indices = indices,
                                barycentricCoords = coords
                            });
                        }
                    //}
                }

                /*float4 dest_pix0_x2 = new float4(dest_pix0, dest_pix0);
                float4 dest_pix1_x2 = new float4(dest_pix1, dest_pix1);
                float4 dest_pix2_x2 = new float4(dest_pix2, dest_pix2);
                int4 pixelsStart2 = new int4(pixelsStart, pixelsStart);
                size = ((int)math.ceil(size / 2f)) * 2;
                for (int i = 0; i < size; i += 2)
                {
                    int ip1 = i + 1;
                    int4 xy2 = new int4(i, i, ip1, ip1);  
                    xy2.xz = xy2.xz % pixelsXY.x;
                    xy2.yw = xy2.yw / pixelsXY.x;

                    int4 pixCoords = xy2 + pixelsStart2;
                    var coords = Maths.BarycentricCoords2D_x2(pixCoords, dest_pix0_x2, dest_pix1_x2, dest_pix2_x2);

                    var inTri = Maths.IsInTriangle2D_x2(coords, triTestErrorMargin);
                    if (inTri.x)
                    {
                        triPixels.Enqueue(new TriPixel()
                        {
                            triIndex = index,
                            pixelIndex = (pixCoords.y * texDims.x) + pixCoords.x,
                            pixelCoords = pixCoords.xy,
                            indices = indices,
                            barycentricCoords = new float3(coords.c0.x, coords.c1.x, coords.c2.x)
                        });
                    }
                    if (inTri.y)
                    {
                        triPixels.Enqueue(new TriPixel()
                        {
                            triIndex = index,
                            pixelIndex = (pixCoords.w * texDims.x) + pixCoords.z,
                            pixelCoords = pixCoords.zw,
                            indices = indices,
                            barycentricCoords = new float3(coords.c0.y, coords.c1.y, coords.c2.y)
                        });
                    }
                }*/
            }
        }
        [BurstCompile]
        private struct BakeMeshToPixelsJob : IJobParallelFor
        {
            public float minFaceNormalDot;
            public float alphaClipThreshold;
            public float triTestErrorMargin;
            public float depthThreshold;
            public float flipDepth;

            //public int2 texDims;
            public int2 texDimsReference;
            public int2 texDimsAlphaReference;

            public float4 defaultPixel;

            //public float4x4 worldToLocal;

            [ReadOnly]
            public NativeArray<TriPixel> triPixels;

            [ReadOnly]
            public NativeArray<int> referenceTriangles;
            [ReadOnly]
            public NativeArray<float3> referenceVertices;
            [ReadOnly]
            public NativeArray<float2> referenceUVs;
            [ReadOnly]
            public NativeArray<float3> referenceTriNormals;
            //[ReadOnly]
            //public NativeArray<float4> referenceNormals;
            //[ReadOnly]
            //public NativeArray<float4> referenceTangents;
            //[ReadOnly]
            //public NativeArray<float4> referenceBitangents;

            [ReadOnly]
            public NativeArray<float> referencePixelAlphas;
            [ReadOnly]
            public NativeArray<float4> referencePixels;


            [ReadOnly]
            public NativeArray<float3> destVertices;
            [ReadOnly]
            public NativeArray<float3> destTriNormals;
            //[ReadOnly]
            //public NativeArray<float4> destNormals;
            //[ReadOnly]
            //public NativeArray<float4> destTangents;
            //[ReadOnly]
            //public NativeArray<float4> destBitangents;

            [NativeDisableParallelForRestriction, WriteOnly]
            public NativeArray<float4> outputPixels;

            public void Execute(int index)
            {
                var triPixel = triPixels[index];
                float3 dest_v0 = destVertices[triPixel.indices.x];
                float3 dest_v1 = destVertices[triPixel.indices.y];
                float3 dest_v2 = destVertices[triPixel.indices.z];
                float3x3 dest_v012 = new float3x3(dest_v0, dest_v1, dest_v2);

                //float3 dest_n = math.normalize(math.cross(dest_v1 - dest_v0, dest_v2 - dest_v0));
                float3 dest_n = destTriNormals[triPixel.triIndex];
                float3 depth_n = dest_n * flipDepth;

                float3 dest_pos = math.mul(dest_v012, triPixel.barycentricCoords);
                float startDepth = math.dot(dest_pos, depth_n);

                int2 texDimsReferenceM1 = texDimsReference - 1;
                int2 texDimsAlphaReferenceM1 = texDimsAlphaReference - 1;

                bool hits = false;

                float closestDepth = float.MaxValue;
                float4 finalPixel = defaultPixel;
                for (int t = 0; t < referenceTriangles.Length; t += 3)
                {
                    int ref_triIndex = t / 3;

                    float3 ref_n = referenceTriNormals[ref_triIndex];  
                    float dot = math.dot(dest_n, ref_n);
                    if (dot < minFaceNormalDot) continue;

                    int ref_i0 = referenceTriangles[t];
                    int ref_i1 = referenceTriangles[t + 1];
                    int ref_i2 = referenceTriangles[t + 2];

                    float3 ref_v0 = referenceVertices[ref_i0];
                    float3 ref_v1 = referenceVertices[ref_i1];
                    float3 ref_v2 = referenceVertices[ref_i2];
                    //float3x3 ref_v012 = new float3x3(ref_v0, ref_v1, ref_v2);

                    float2 ref_uv0 = Maths.wrap(referenceUVs[ref_i0], 1f);
                    float2 ref_uv1 = Maths.wrap(referenceUVs[ref_i1], 1f);
                    float2 ref_uv2 = Maths.wrap(referenceUVs[ref_i2], 1f); 
                    float2x3 ref_uv012 = new float2x3(ref_uv0, ref_uv1, ref_uv2);

                    bool didHit = Maths.ray_intersect_triangle(dest_pos - depth_n * 10000, depth_n, ref_v0, ref_v1, ref_v2, out Maths.RaycastHitResult hit);
                    var coords = hit.barycentricCoordinate;
                    float3 ref_pos = hit.point;//math.mul(ref_v012, coords);
                    float2 ref_uv = Maths.wrap(math.mul(ref_uv012, math.saturate(coords)), 1f);   

                    int2 ref_pixelIndex2 = new int2(math.floor(ref_uv * texDimsReferenceM1));
                    int2 ref_alphaPixelIndex2 = new int2(math.floor(ref_uv * texDimsAlphaReferenceM1)); 

                    int ref_pixelIndex = (ref_pixelIndex2.y * texDimsReference.x) + ref_pixelIndex2.x;
                    int ref_alphaPixelIndex = (ref_alphaPixelIndex2.y * texDimsAlphaReference.x) + ref_alphaPixelIndex2.x;

                    float alpha = referencePixelAlphas[ref_alphaPixelIndex];
                    float depth = math.select(math.dot(ref_pos, depth_n) - startDepth, float.MaxValue, alpha < alphaClipThreshold);  

                    float4 ref_pixel = referencePixels[ref_pixelIndex]; 

                    bool isCloser = math.all(new bool3(depth < closestDepth, depth >= depthThreshold, didHit));

                    finalPixel = math.select(finalPixel, ref_pixel, isCloser);  
                    closestDepth = math.select(closestDepth, depth, isCloser);

                    hits = didHit || hits;
                }   

                outputPixels[triPixel.pixelIndex] = finalPixel;
            }
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

            public void IncrementEdgeCount(int tri2_i0, int tri2_i1, int tri2_i2, WeldedVertex[] mergedVertices = null)
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
            /// An edge is open if it is only part of a single face.
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
        public static OpenEdgeData[] GetOpenEdgeData(int vertexCount, ICollection<int> triangles, WeldedVertex[] mergedVertices = null)
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

        #region UV Unwrapping

        [Serializable]
        public struct UnwrapSettings
        {
            [Tooltip("Maximum allowed angle distortion (0..1).")]
            public float angleError;

            [Tooltip("Maximum allowed area distortion (0..1).")]  
            public float areaError;

            [Tooltip("This angle (in degrees) or greater between triangles will cause seam to be created.")]
            public float hardAngle;

            [Tooltip("How much uv-islands will be padded.")]
            public float packMargin;

            public bool IsInvalid => angleError == 0 && areaError == 0 && hardAngle == 0 && packMargin == 0;

#if UNITY_EDITOR
            public static implicit operator UnityEditor.UnwrapParam(UnwrapSettings settings) => new UnityEditor.UnwrapParam()
            {
                angleError = settings.angleError,
                areaError = settings.areaError,
                hardAngle = settings.hardAngle,
                packMargin = settings.packMargin
            }; public static implicit operator UnwrapSettings(UnityEditor.UnwrapParam settings) => new UnwrapSettings()
            {
                angleError = settings.angleError,
                areaError = settings.areaError,
                hardAngle = settings.hardAngle,
                packMargin = settings.packMargin
            };
#endif
        }

        public static Mesh Unwrap(this Mesh mesh, UVChannelURP destinationChannel, UnwrapSettings settings = default, bool instantiateMesh = false)
        {
#if UNITY_EDITOR
            bool flag = false;

            var tempMesh = instantiateMesh ? Duplicate(mesh) : mesh;
            var uv2 = tempMesh.uv2;
            bool hasUV2 = false;
            if (uv2 != null && uv2.Length > 0)  
            {
                hasUV2 = true;
                tempMesh.uv5 = uv2;
            }

            tempMesh.uv2 = null;

            if (settings.IsInvalid)
            {
                if (!UnityEditor.Unwrapping.GenerateSecondaryUVSet(tempMesh) && tempMesh.indexFormat == IndexFormat.UInt16)
                {
                    int[][] submeshes = new int[tempMesh.subMeshCount][];
                    for (int a = 0; a < tempMesh.subMeshCount; a++) submeshes[a] = tempMesh.GetTriangles(a);

                    tempMesh.indexFormat = IndexFormat.UInt32;
                    tempMesh.subMeshCount = submeshes.Length;
                    for (int a = 0; a < tempMesh.subMeshCount; a++) tempMesh.SetTriangles(submeshes[a], a);

                    if (UnityEditor.Unwrapping.GenerateSecondaryUVSet(tempMesh)) flag = true;
                }
            } 
            else
            {
                if (!UnityEditor.Unwrapping.GenerateSecondaryUVSet(tempMesh, settings) && tempMesh.indexFormat == IndexFormat.UInt16)  
                {
                    int[][] submeshes = new int[tempMesh.subMeshCount][];
                    for (int a = 0; a < tempMesh.subMeshCount; a++) submeshes[a] = tempMesh.GetTriangles(a);

                    tempMesh.indexFormat = IndexFormat.UInt32;
                    tempMesh.subMeshCount = submeshes.Length;
                    for (int a = 0; a < tempMesh.subMeshCount; a++) tempMesh.SetTriangles(submeshes[a], a);

                    if (UnityEditor.Unwrapping.GenerateSecondaryUVSet(tempMesh, settings)) flag = true;
                }
            }
             
            if (flag)
            {
                tempMesh.SetUVs((int)destinationChannel, tempMesh.uv2);
                if (hasUV2) 
                {
                    tempMesh.SetUVs(1, tempMesh.uv5);
                    tempMesh.uv5 = null;
                } 

                return tempMesh;
            }
#endif

            return mesh;
        }

        #endregion

        #region UV Packing

        [Serializable]
        public struct UVIsland
        {
            public float weight;
            public MeshIsland meshData;

            public float2 boundsMin;
            public float2 boundsMax;
        }

        public static void PackUVByArea(int[] triangles, Vector2[] uvs, float margin, float downsizingFactor = 0.001f)
        {
            if (margin >= 0.5f) return;

            downsizingFactor = 1f - downsizingFactor; 

            var meshIslands = CalculateMeshIslands(uvs.Length, triangles, null, null, false);

            float totalWeight = 0f;
            List<UVIsland> uvIslands = new List<UVIsland>();
            foreach (var meshIsland in meshIslands)
            {
                if (meshIsland.vertices == null || meshIsland.vertices.Length < 3) continue;

                var uvIsland = new UVIsland();
                uvIsland.meshData = meshIsland;

                float2 min = float.MaxValue;
                float2 max = float.MinValue;

                for (int a = 0; a < meshIsland.vertices.Length; a++)
                {
                    var uv_ = uvs[meshIsland.vertices[a]];

                    min = math.select(min, uv_, uv_ < min);
                    max = math.select(max, uv_, uv_ > max);
                }

                uvIsland.weight = math.abs((max.x - min.x) * (max.y - min.y)); // area
                totalWeight += uvIsland.weight;

                uvIsland.boundsMin = min;
                uvIsland.boundsMax = max;
                uvIslands.Add(uvIsland);
            }

            uvIslands.Sort((UVIsland A, UVIsland B) => Math.Sign(B.weight - A.weight));
            /*float size = Mathf.Sqrt(totalWeight);
            float sizeScaling = 1f / size;
            for (int a = 0; a < uvs.Length; a++) uvs[a] = uvs[a] * sizeScaling;
            for (int a = 0; a < uvIslands.Count; a++)
            {
                var uvIsland = uvIslands[a];
                uvIsland.boundsMin = uvIsland.boundsMin * sizeScaling;
                uvIsland.boundsMax = uvIsland.boundsMax * sizeScaling;
                uvIslands[a] = uvIsland;
            }*/

            int columns = Mathf.CeilToInt(1f / margin);
            int columnsM1 = columns - 1;
            float[] lineHeights = new float[columns];
            float[] prevLineHeights = new float[columns]; 
            void ResetLineHeights()
            {
                for (int a = 0; a < lineHeights.Length; a++) prevLineHeights[a] = lineHeights[a] = 0f;
            }
            void PrepareLineHeights()
            {
                for (int a = 0; a < lineHeights.Length; a++)
                {
                    prevLineHeights[a] = lineHeights[a];
                }
            }

            int ind = 0;
            float x = 0f;
            float minBound = margin;
            float maxBound = 1f - margin;

            bool Downsize()
            {
                if (downsizingFactor <= 0f) return false;

                for (int a = 0; a < uvs.Length; a++) uvs[a] = uvs[a] * downsizingFactor;
                for (int a = 0; a < uvIslands.Count; a++)
                {
                    var uvIsland = uvIslands[a];
                    uvIsland.boundsMin = uvIsland.boundsMin * downsizingFactor;
                    uvIsland.boundsMax = uvIsland.boundsMax * downsizingFactor; 
                    uvIslands[a] = uvIsland;
                }

                x = margin;

                ind = 0;
                ResetLineHeights();
                return true;
            }

            while (ind < uvIslands.Count)
            {
                x = math.max(x, minBound);

                var currentIsland = uvIslands[ind];
                float2 bounds = currentIsland.boundsMax - currentIsland.boundsMin;
                if (x + bounds.x >= maxBound)
                {
                    x = margin;
                    if (x + bounds.x >= maxBound)
                    {
                        if (!Downsize()) break;
                        continue;
                    }

                    PrepareLineHeights();
                }

                int xStart = Mathf.Min(columns, Mathf.FloorToInt(x / margin));
                int xSize = math.min(columns - xStart, Mathf.CeilToInt(bounds.x / margin) + 1);
                float heightOffset = 0f;
                for (int a = 0; a < xSize; a++) heightOffset = Mathf.Max(heightOffset, prevLineHeights[xStart + a]);

                float targetY = heightOffset + margin;
                float targetHeight = targetY + bounds.y;
                if (targetHeight > maxBound)
                {
                    if (!Downsize()) break;
                    continue;
                }

                float2 boundsMin = new float2(x, targetY);
                float2 boundsMax = boundsMin + bounds;

                for (int a = 0; a < currentIsland.meshData.vertices.Length; a++)
                {
                    int vIndex = currentIsland.meshData.vertices[a];
                    uvs[vIndex] = (uvs[vIndex] - (Vector2)currentIsland.boundsMin) + (Vector2)boundsMin;
                }

                currentIsland.boundsMin = boundsMin; 
                currentIsland.boundsMax = boundsMax;

                uvIslands[ind] = currentIsland;

                ind++;

                for (int a = 0; a < xSize; a++)
                {
                    int i = xStart + a;
                    lineHeights[i] = math.max(lineHeights[i], targetHeight);
                }

                x += bounds.x + margin;
            }
        }

        #endregion

    }
}

#endif