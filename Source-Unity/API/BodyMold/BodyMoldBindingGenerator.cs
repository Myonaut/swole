using System;
using System.Collections.Generic;

using UnityEngine;
using Unity.Mathematics;

namespace Swole.Modding
{
    public static class BodyMoldBindingGenerator
    {

        private static readonly Dictionary<string, int> boneNameIndexConverter = new Dictionary<string, int>();
        public static void GenerateBindingsForBodies(SkinnedMeshRenderer clothing, ClothingEditor.WeightedRenderer[] bodies, out int[][] localIndicesPerBody, out int4[][] indicesPerBody, out float4[][] weightsPerBody, out Triangles32[][] collisionTriangles, bool includeCollisionTriangles = true, string clothingVertexMaskName = null, float distanceBindingWeight = 0.1f)
        {
            if (bodies == null) throw new ArgumentNullException(nameof(bodies));

            boneNameIndexConverter.Clear();
            var bindingData = ClothingEditor.GenerateSkinningBasedBindingData(boneNameIndexConverter, clothingVertexMaskName, false, false, false, false, clothing, bodies);

            localIndicesPerBody = new int[bodies.Length][];
            indicesPerBody = new int4[bodies.Length][];
            weightsPerBody = new float4[bodies.Length][];
            collisionTriangles = new Triangles32[bodies.Length][];

            var finalData = new Dictionary<int, (int4, float4)>();

            var influenceData = ClothingEditor.GenerateInfluenceData(bodies, bindingData, 0.1f);
            var influenceLists = new List<float3>[bodies.Length];
            for(int bIndex = 0; bIndex < bodies.Length; bIndex++) influenceLists[bIndex] = new List<float3>();

            for(int cVIndex = 0; cVIndex < influenceData.Length; cVIndex++)
            {
                var infData = influenceData[cVIndex];
                if (infData.influences.influenceA.meshIndex >= 0) influenceLists[infData.influences.influenceA.meshIndex].Add(new float3(cVIndex, infData.influences.influenceA.vertexIndex, infData.influences.influenceA.weight));
                if (infData.influences.influenceB.meshIndex >= 0) influenceLists[infData.influences.influenceB.meshIndex].Add(new float3(cVIndex, infData.influences.influenceB.vertexIndex, infData.influences.influenceB.weight));
                if (infData.influences.influenceC.meshIndex >= 0) influenceLists[infData.influences.influenceC.meshIndex].Add(new float3(cVIndex, infData.influences.influenceC.vertexIndex, infData.influences.influenceC.weight));
                if (infData.influences.influenceD.meshIndex >= 0) influenceLists[infData.influences.influenceD.meshIndex].Add(new float3(cVIndex, infData.influences.influenceD.vertexIndex, infData.influences.influenceD.weight)); 

                if (cVIndex % 250 == 0)
                {
                    if (infData.influences.influenceA.meshIndex >= 0) Debug.DrawLine(bindingData.skinningData_character[infData.influences.influenceA.meshIndex][infData.influences.influenceA.vertexIndex].vertex.worldPosition, bindingData.skinningData_clothing[cVIndex].vertex.worldPosition, Color.red, 200f); 
                } 
            }

            for (int bIndex = 0; bIndex < bodies.Length; bIndex++)
            {
                var renderer = bodies[bIndex].renderer;
                if (renderer == null || renderer.sharedMesh == null)
                {
                    localIndicesPerBody[bIndex] = new int[0];
                    indicesPerBody[bIndex] = new int4[0];
                    weightsPerBody[bIndex] = new float4[0];
                    collisionTriangles[bIndex] = new Triangles32[0];
                    continue;
                }

                var bodyMesh = renderer.sharedMesh;
                var bodyVerts = bodyMesh.vertices;
                var bodyTris = bodyMesh.triangles;
                int bodyTriCount = bodyTris.Length / 3;

                // Build vertex->triangle adjacency for quick neighborhood expansion
                List<int>[] vertexToTriangles = new List<int>[bodyMesh.vertexCount];
                for (int v = 0; v < vertexToTriangles.Length; v++) vertexToTriangles[v] = new List<int>();
                for (int t = 0; t < bodyTriCount; t++)
                {
                    int a = bodyTris[t * 3 + 0];
                    int b = bodyTris[t * 3 + 1];
                    int c = bodyTris[t * 3 + 2];
                    vertexToTriangles[a].Add(t);
                    vertexToTriangles[b].Add(t);
                    vertexToTriangles[c].Add(t);
                }

                finalData.Clear();
                var list = influenceLists[bIndex];
                for(int i = 0; i < list.Count; i++)
                {
                    var listData = list[i];

                    int localIndex = (int)listData.x;
                    int bodyVIndex = (int)listData.y;
                    float weight = listData.z;

                    if (!finalData.TryGetValue(localIndex, out var finalVal))
                    {
                        finalVal = (new int4(-1, -1, -1, -1), new float4(0f, 0f, 0f, 0f));
                    }

                    if (weight > 0f)
                    {
                        var finalIndices = finalVal.Item1;
                        var finalWeights = finalVal.Item2;

                        bool flag = true; 
                        if (finalIndices.x == bodyVIndex)
                        {
                            if (weight > finalWeights.x) finalWeights.x = weight;
                            flag = false;
                        }
                        else if (finalIndices.y == bodyVIndex)
                        {
                            if (weight > finalWeights.y) finalWeights.y = weight;
                            flag = false;
                        }
                        else if (finalIndices.z == bodyVIndex)
                        {
                            if (weight > finalWeights.z) finalWeights.z = weight;
                            flag = false;
                        }
                        else if (finalIndices.w == bodyVIndex) 
                        {
                            if (weight > finalWeights.w) finalWeights.w = weight;
                            flag = false;
                        }

                        if (flag)
                        {
                            if (finalIndices.x < 0 || finalWeights.x < weight)
                            {
                                if (finalIndices.x >= 0)
                                {
                                    finalIndices.w = finalIndices.z;
                                    finalWeights.w = finalWeights.z;

                                    finalIndices.z = finalIndices.y;
                                    finalWeights.z = finalWeights.y;

                                    finalIndices.y = finalIndices.x;
                                    finalWeights.y = finalWeights.x;
                                }

                                finalIndices.x = bodyVIndex;
                                finalWeights.x = weight;
                            }
                            else if (finalIndices.y < 0 || finalWeights.y < weight)
                            {
                                if (finalIndices.y >= 0)
                                {
                                    finalIndices.w = finalIndices.z;
                                    finalWeights.w = finalWeights.z;

                                    finalIndices.z = finalIndices.y;
                                    finalWeights.z = finalWeights.y;
                                }

                                finalIndices.y = bodyVIndex;
                                finalWeights.y = weight;
                            }
                            else if (finalIndices.z < 0 || finalWeights.z < weight)
                            {
                                if (finalIndices.z >= 0)
                                {
                                    finalIndices.w = finalIndices.z;
                                    finalWeights.w = finalWeights.z;
                                }

                                finalIndices.z = bodyVIndex;
                                finalWeights.z = weight;
                            }
                            else if (finalIndices.w < 0 || finalWeights.w < weight)
                            {
                                finalIndices.w = bodyVIndex;
                                finalWeights.w = weight;
                            }
                        }

                        finalVal.Item1 = finalIndices;
                        finalVal.Item2 = finalWeights;
                    }

                    finalData[localIndex] = finalVal; 
                }

                localIndicesPerBody[bIndex] = new int[finalData.Count];
                indicesPerBody[bIndex] = new int4[finalData.Count];
                weightsPerBody[bIndex] = new float4[finalData.Count];
                collisionTriangles[bIndex] = new Triangles32[finalData.Count];
                int entryIndex = -1;
                foreach (var entry in finalData)
                {
                    entryIndex++;
                    int clothingVIndex = entry.Key;
                    int4 bodyVIndices = entry.Value.Item1;
                    float4 bodyVWeights = entry.Value.Item2;

                    localIndicesPerBody[bIndex][entryIndex] = clothingVIndex;
                    indicesPerBody[bIndex][entryIndex] = bodyVIndices;
                    weightsPerBody[bIndex][entryIndex] = bodyVWeights;

                    if (!includeCollisionTriangles)
                    {
                        // marker: fill with -1 to indicate empty
                        collisionTriangles[bIndex][entryIndex] = new Triangles32
                        {
                            trianglesA = new int4(-1,-1,-1,-1),
                            trianglesB = new int4(-1,-1,-1,-1),
                            trianglesC = new int4(-1,-1,-1,-1),
                            trianglesD = new int4(-1,-1,-1,-1),
                            trianglesE = new int4(-1,-1,-1,-1),
                            trianglesF = new int4(-1,-1,-1,-1),
                            trianglesG = new int4(-1,-1,-1,-1),
                            trianglesH = new int4(-1,-1,-1,-1),
                        };
                        continue;
                    }

                    // Collect up to 32 triangle indices using the four body vertex indices and their weights
                    var vertsArr = new int[4] { bodyVIndices.x, bodyVIndices.y, bodyVIndices.z, bodyVIndices.w };
                    var weightsArr = new float[4] { bodyVWeights.x, bodyVWeights.y, bodyVWeights.z, bodyVWeights.w };

                    var selectedSet = new HashSet<int>();
                    var selectionOrder = new List<int>();
                    var q = new Queue<int>();

                    // Seed triangles by iterating vertices in order of descending weight so higher-weight verts prioritize their triangles
                    int[] order = new int[4] { 0, 1, 2, 3 };
                    Array.Sort(order, (a, b) => weightsArr[b].CompareTo(weightsArr[a]));
                    for (int oi = 0; oi < 4; oi++)
                    {
                        int idx = order[oi];
                        int v = vertsArr[idx];
                        if (v < 0 || v >= vertexToTriangles.Length) continue;
                        foreach (var triIdx in vertexToTriangles[v])
                        {
                            if (selectedSet.Add(triIdx)) { q.Enqueue(triIdx); selectionOrder.Add(triIdx); }
                        }
                        if (selectedSet.Count >= 32) break;
                    }

                    // Expand by adjacency until we have enough triangles or run out
                    while (selectedSet.Count < 32 && q.Count > 0)
                    {
                        int tri = q.Dequeue();
                        int a = bodyTris[tri * 3 + 0];
                        int b = bodyTris[tri * 3 + 1];
                        int c = bodyTris[tri * 3 + 2];
                        int[] triVerts = new int[] { a, b, c };
                        foreach (var v in triVerts)
                        {
                            foreach (var neighborTri in vertexToTriangles[v])
                            {
                                if (selectedSet.Add(neighborTri)) { q.Enqueue(neighborTri); selectionOrder.Add(neighborTri); }
                                if (selectedSet.Count >= 32) break;
                            }
                            if (selectedSet.Count >= 32) break;
                        }
                    }

                    // If still not enough, add nearest triangles by a score combining centroid distance and overlap with weighted verts
                    if (selectedSet.Count < 32)
                    {
                        var remaining = new List<KeyValuePair<float, int>>();
                        // compute a representative position weighted by body vertex weights
                        Vector3 repPos = Vector3.zero;
                        float repTotal = 0f;
                        for (int k = 0; k < 4; k++)
                        {
                            int vv = vertsArr[k];
                            if (vv >= 0 && vv < bodyVerts.Length)
                            {
                                repPos += bodyVerts[vv] * weightsArr[k];
                                repTotal += weightsArr[k];
                            }
                        }
                        if (repTotal > 0f) repPos /= repTotal;
                        else repPos = (vertsArr[0] >= 0 && vertsArr[0] < bodyVerts.Length) ? bodyVerts[vertsArr[0]] : Vector3.zero;

                        for (int t = 0; t < bodyTriCount; t++)
                        {
                            if (selectedSet.Contains(t)) continue;
                            int a = bodyTris[t * 3 + 0];
                            int b = bodyTris[t * 3 + 1];
                            int c = bodyTris[t * 3 + 2];
                            Vector3 centroid = (bodyVerts[a] + bodyVerts[b] + bodyVerts[c]) / 3f;
                            float d = (centroid - repPos).sqrMagnitude;
                            // overlap score: sum of weights for body verts that appear in this triangle
                            float overlap = 0f;
                            for (int k = 0; k < 4; k++)
                            {
                                int vv = vertsArr[k];
                                if (vv == a || vv == b || vv == c) overlap += weightsArr[k];
                            }
                            float score = d / (1f + overlap);
                            remaining.Add(new KeyValuePair<float, int>(score, t));
                        }
                        remaining.Sort((x, y) => x.Key.CompareTo(y.Key));
                        for (int r = 0; r < remaining.Count && selectedSet.Count < 32; r++) { selectedSet.Add(remaining[r].Value); selectionOrder.Add(remaining[r].Value); }
                    }

                    // Pack up to 32 triangle indices into Triangles32 fields in order
                    int[] packed = new int[32];
                    for (int k = 0; k < 32; k++) packed[k] = -1;
                    for (int k = 0; k < selectionOrder.Count && k < 32; k++) packed[k] = selectionOrder[k];

                    collisionTriangles[bIndex][entryIndex] = new Triangles32
                    {
                        trianglesA = new int4(packed[0], packed[1], packed[2], packed[3]),
                        trianglesB = new int4(packed[4], packed[5], packed[6], packed[7]),
                        trianglesC = new int4(packed[8], packed[9], packed[10], packed[11]),
                        trianglesD = new int4(packed[12], packed[13], packed[14], packed[15]),
                        trianglesE = new int4(packed[16], packed[17], packed[18], packed[19]),
                        trianglesF = new int4(packed[20], packed[21], packed[22], packed[23]),
                        trianglesG = new int4(packed[24], packed[25], packed[26], packed[27]),
                        trianglesH = new int4(packed[28], packed[29], packed[30], packed[31]),
                    };
                }
            }

            /*for(int i = 0; i < indicesPerBody.Length; i++)  
            {
                var linds = localIndicesPerBody[i];
                var inds = indicesPerBody[i];
                var ws = weightsPerBody[i];
                for(int j = 0; j < linds.Length; j++) 
                {
                    var lind = linds[j];
                    var ind = inds[j];
                    var w = ws[j];
                    Debug.Log($"{i}: {lind}: {ind} - {w}");  
                }
            }*/

            bindingData.Dispose();
        }

        public static void GenerateBindings(SkinnedMeshRenderer clothing, SkinnedMeshRenderer body, out int[][] localIndicesPerBody, out int4[][] indicesPerBody, out float4[][] weightsPerBody, out Triangles32[][] collisionTriangles, bool includeCollisionTriangles = true, string clothingVertexMaskName = null, float distanceBindingWeight = 0.1f)
        {
            GenerateBindingsForBodies(clothing, new ClothingEditor.WeightedRenderer[] { new ClothingEditor.WeightedRenderer() { renderer = body, weight = 1f } }, out localIndicesPerBody, out indicesPerBody, out weightsPerBody, out collisionTriangles, includeCollisionTriangles, clothingVertexMaskName, distanceBindingWeight);
        }
        public static void GenerateBindings(SkinnedMeshRenderer clothing, SkinnedMeshRenderer[] bodies, out int[][] localIndicesPerBody, out int4[][] indicesPerBody, out float4[][] weightsPerBody, out Triangles32[][] collisionTriangles, bool includeCollisionTriangles = true, string clothingVertexMaskName = null, float distanceBindingWeight = 0.1f)
        {
            var arr = new ClothingEditor.WeightedRenderer[bodies.Length];
            for(int a = 0; a < bodies.Length; a++)
            {
                arr[a] = new ClothingEditor.WeightedRenderer() { renderer = bodies[a], weight = 1f };
            }

            GenerateBindingsForBodies(clothing, arr, out localIndicesPerBody, out indicesPerBody, out weightsPerBody, out collisionTriangles, includeCollisionTriangles, clothingVertexMaskName, distanceBindingWeight);
        }
    }
}
