using System;
using System.Collections.Generic;

using UnityEngine;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;

using Swole.API.Unity;

namespace Swole.Modding
{
    public static class BodyMoldBindingGenerator
    {

        private static readonly Dictionary<string, int> boneNameIndexConverter = new Dictionary<string, int>();
        public static void GenerateBindingsForBodies(SkinnedMeshRenderer clothing, string optionalBindingShapeName, float optionalBindingShapeWeight, ClothingEditor.WeightedRenderer[] bodies, Quaternion clothingToBodyRot, out int[][] localIndicesPerBody, out int4[][] indicesPerBody, out float4[][] weightsPerBody, out Triangles32[][] collisionTriangles, out PushBackVertex[][] pushBackVertices, out MaskedVertex[][] maskedVertices, bool includeCollisionTriangles = true, bool includePushBackVertices = true, bool includeCoverageMask = true, string clothingVertexMaskName = null, float distanceBindingWeight = 0.1f, bool maskByDistanceToFirst = false)
        {
            if (bodies == null) throw new ArgumentNullException(nameof(bodies));

            boneNameIndexConverter.Clear();
            var bindingData = ClothingEditor.GenerateSkinningBasedBindingData(boneNameIndexConverter, clothingVertexMaskName, false, false, false, false, clothing, bodies);

            localIndicesPerBody = new int[bodies.Length][];
            indicesPerBody = new int4[bodies.Length][];
            weightsPerBody = new float4[bodies.Length][];
            collisionTriangles = new Triangles32[bodies.Length][];
            pushBackVertices = new PushBackVertex[bodies.Length][];
            maskedVertices = new MaskedVertex[bodies.Length][];
            var finalData = new Dictionary<int, (int4, float4)>();

            // Use per-body influences to ensure each body mesh gets proper bindings
            var perBodyInfluences = ClothingEditor.GeneratePerBodyInfluences(bodies, bindingData, distanceBindingWeight);
            var influenceLists = new List<float3>[bodies.Length];

            for(int bIndex = 0; bIndex < bodies.Length; bIndex++) 
            {
                influenceLists[bIndex] = new List<float3>();

                // Build influence list from this body's per-vertex influences
                var bodyInfluences = perBodyInfluences[bIndex];
                for (int cVIndex = 0; cVIndex < bodyInfluences.Length; cVIndex++)
                {
                    var inf = bodyInfluences[cVIndex];
                    if (inf.influenceA.meshIndex >= 0 && inf.influenceA.weight > 0f)
                    {
                        influenceLists[bIndex].Add(new float3(cVIndex, inf.influenceA.vertexIndex, inf.influenceA.weight));  
                    }
                    if (inf.influenceB.meshIndex >= 0 && inf.influenceB.weight > 0f)
                    {
                        influenceLists[bIndex].Add(new float3(cVIndex, inf.influenceB.vertexIndex, inf.influenceB.weight));  
                    }
                }
            }

            // Track minimum distances from clothing vertices to first body mesh vertices (for distance masking)
            Dictionary<int, float> firstBodyMinDistances = null;
            if (maskByDistanceToFirst && bodies.Length > 1)
            {
                firstBodyMinDistances = new Dictionary<int, float>();
            }

            for (int bIndex = 0; bIndex < bodies.Length; bIndex++)
            {
                var renderer = bodies[bIndex].renderer;
                bool isMaskOnly = bodies[bIndex].weight < 0f;
                if (renderer == null || renderer.sharedMesh == null || isMaskOnly)
                {
                    localIndicesPerBody[bIndex] = new int[0];
                    indicesPerBody[bIndex] = new int4[0];
                    weightsPerBody[bIndex] = new float4[0];
                    collisionTriangles[bIndex] = new Triangles32[0];
                    pushBackVertices[bIndex] = new PushBackVertex[0];
                    if (!isMaskOnly)
                    {
                        maskedVertices[bIndex] = new MaskedVertex[0];
                        continue;
                    }
                }

                var bodyMesh = renderer.sharedMesh;
                var bodyVerts = bodyMesh.vertices;
                var bodyTris = bodyMesh.triangles;
                int bodyTriCount = bodyTris.Length / 3;

                if (!isMaskOnly)
                {
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

                    // Get body weight for distance scaling
                    float bodyWeight = bodies[bIndex].weight;
                    if (bodyWeight <= 0f) bodyWeight = 1f; // Avoid division by zero

                    for (int i = 0; i < list.Count; i++)
                    {
                        var listData = list[i];

                        int localIndex = (int)listData.x;
                        int bodyVIndex = (int)listData.y;
                        float weight = listData.z;

                        // Apply distance masking for subsequent body meshes
                        if (maskByDistanceToFirst && bIndex > 0 && firstBodyMinDistances != null)
                        {
                            // Calculate distance from clothing vertex to current body vertex
                            Vector3 clothingPos = bindingData.skinningData_clothing[localIndex].vertex.worldPosition;
                            Vector3 bodyPos = bindingData.skinningData_character[bIndex][bodyVIndex].vertex.worldPosition;
                            float currentDistance = Vector3.Distance(clothingPos, bodyPos) / bodyWeight;

                            // Check if this vertex has a binding to the first body
                            if (firstBodyMinDistances.TryGetValue(localIndex, out float firstBodyDistance))
                            {
                                // If current distance is further than first body distance, skip this binding
                                if (currentDistance > firstBodyDistance)
                                {
                                    continue;
                                }
                            }
                        }

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
                        float weightSum = bodyVWeights.x + bodyVWeights.y + bodyVWeights.z + bodyVWeights.w;
                        bodyVWeights = bodyVWeights / weightSum;

                        localIndicesPerBody[bIndex][entryIndex] = clothingVIndex;
                        indicesPerBody[bIndex][entryIndex] = bodyVIndices;
                        weightsPerBody[bIndex][entryIndex] = bodyVWeights;

                        if (!includeCollisionTriangles)
                        {
                            // marker: fill with -1 to indicate empty
                            collisionTriangles[bIndex][entryIndex] = new Triangles32
                            {
                                trianglesA = new int4(-1, -1, -1, -1),
                                trianglesB = new int4(-1, -1, -1, -1),
                                trianglesC = new int4(-1, -1, -1, -1),
                                trianglesD = new int4(-1, -1, -1, -1),
                                trianglesE = new int4(-1, -1, -1, -1),
                                trianglesF = new int4(-1, -1, -1, -1),
                                trianglesG = new int4(-1, -1, -1, -1),
                                trianglesH = new int4(-1, -1, -1, -1),
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

                    // For the first body mesh, populate the minimum distances dictionary
                    if (maskByDistanceToFirst && bIndex == 0 && firstBodyMinDistances != null)
                    {
                        float firstBodyWeight = bodies[0].weight;
                        if (firstBodyWeight <= 0f) firstBodyWeight = 1f;

                        foreach (var entry in finalData)
                        {
                            int clothingVIndex = entry.Key;
                            int4 bodyVIndices = entry.Value.Item1;
                            float4 bodyVWeights = entry.Value.Item2;

                            Vector3 clothingPos = bindingData.skinningData_clothing[clothingVIndex].vertex.worldPosition;
                            float minDist = float.MaxValue;

                            // Calculate weighted average distance or minimum distance to bound body vertices
                            for (int k = 0; k < 4; k++)
                            {
                                int bodyVIdx = -1;
                                if (k == 0) bodyVIdx = bodyVIndices.x;
                                else if (k == 1) bodyVIdx = bodyVIndices.y;
                                else if (k == 2) bodyVIdx = bodyVIndices.z;
                                else if (k == 3) bodyVIdx = bodyVIndices.w;

                                if (bodyVIdx >= 0 && bodyVIdx < bindingData.skinningData_character[0].Length)
                                {
                                    Vector3 bodyPos = bindingData.skinningData_character[0][bodyVIdx].vertex.worldPosition;
                                    float dist = Vector3.Distance(clothingPos, bodyPos) / firstBodyWeight;
                                    if (dist < minDist)
                                    {
                                        minDist = dist;
                                    }
                                }
                            }

                            if (minDist < float.MaxValue)
                            {
                                firstBodyMinDistances[clothingVIndex] = minDist;
                            }
                        }
                    }

                    if (includePushBackVertices)
                    {
                        pushBackVertices[bIndex] = GeneratePushBackTriangles(bodyMesh, clothing.sharedMesh, clothingToBodyRot, distanceBindingWeight);
                    }
                    else
                    {
                        pushBackVertices[bIndex] = new PushBackVertex[0];
                    }

                } 

                if (includeCoverageMask)
                {
                    //maskedVertices[bIndex] = GenerateCoveredVertexMask(bodyMesh, clothing.sharedMesh, clothingToBodyRot);
                    maskedVertices[bIndex] = GenerateCoveredVertexMaskFast(bodyMesh, clothing.sharedMesh, clothingToBodyRot);
                }
                else
                {
                    maskedVertices[bIndex] = new MaskedVertex[0];
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

        public static void GenerateBindings(SkinnedMeshRenderer clothing, string optionalBindingShapeName, float optionalBindingShapeWeight, SkinnedMeshRenderer body, Quaternion clothingToBodyRot, out int[][] localIndicesPerBody, out int4[][] indicesPerBody, out float4[][] weightsPerBody, out Triangles32[][] collisionTriangles, out PushBackVertex[][] pushBackVertices, out MaskedVertex[][] maskedVertices, bool includeCollisionTriangles = true, bool includePushBackVertices = true, bool includeCoverageMask = true, string clothingVertexMaskName = null, float distanceBindingWeight = 0.1f, bool maskByDistanceToFirst = false)
        {
            GenerateBindingsForBodies(clothing, optionalBindingShapeName, optionalBindingShapeWeight, new ClothingEditor.WeightedRenderer[] { new ClothingEditor.WeightedRenderer() { renderer = body, weight = 1f } }, clothingToBodyRot, out localIndicesPerBody, out indicesPerBody, out weightsPerBody, out collisionTriangles, out pushBackVertices, out maskedVertices, includeCollisionTriangles, includePushBackVertices, includeCoverageMask, clothingVertexMaskName, distanceBindingWeight, maskByDistanceToFirst);
        }
        public static void GenerateBindings(SkinnedMeshRenderer clothing, string optionalBindingShapeName, float optionalBindingShapeWeight, SkinnedMeshRenderer[] bodies, Quaternion clothingToBodyRot, out int[][] localIndicesPerBody, out int4[][] indicesPerBody, out float4[][] weightsPerBody, out Triangles32[][] collisionTriangles, out PushBackVertex[][] pushBackVertices, out MaskedVertex[][] maskedVertices, bool includeCollisionTriangles = true, bool includePushBackVertices = true, bool includeCoverageMask = true, string clothingVertexMaskName = null, float distanceBindingWeight = 0.1f, bool maskByDistanceToFirst = false)
        {
            var arr = new ClothingEditor.WeightedRenderer[bodies.Length];
            for(int a = 0; a < bodies.Length; a++)
            {
                arr[a] = new ClothingEditor.WeightedRenderer() { renderer = bodies[a], weight = 1f };
            }

            GenerateBindingsForBodies(clothing, optionalBindingShapeName, optionalBindingShapeWeight, arr, clothingToBodyRot, out localIndicesPerBody, out indicesPerBody, out weightsPerBody, out collisionTriangles, out pushBackVertices, out maskedVertices, includeCollisionTriangles, includePushBackVertices, includeCoverageMask, clothingVertexMaskName, distanceBindingWeight, maskByDistanceToFirst);
        }

        // Job struct for push-back triangle generation
        [BurstCompile]
        private struct PushBackTrianglesJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<float3> bodyVerts;
            [ReadOnly] public NativeArray<float3> bodyNormals;
            [ReadOnly] public NativeArray<float3> triCentroids;
            [ReadOnly] public NativeArray<float3> triNormals;
            [ReadOnly] public NativeArray<int> triAdjacencyOffsets;
            [ReadOnly] public NativeArray<int> triAdjacencyData;
            [ReadOnly] public float minDistSq;
            [ReadOnly] public float minOffsetDot;

            [WriteOnly] public NativeArray<PushBackVertex> results;
            [WriteOnly] public NativeArray<int> validFlags;

            public void Execute(int bv)
            {
                float3 bodyVertPos = bodyVerts[bv];
                float3 bodyVertNormal = math.normalizesafe(bodyNormals[bv], new float3(0, 1, 0));

                int closestTriIndex = -1;
                float closestScore = float.MaxValue;
                float closestDistSq = float.MaxValue;

                int clothingTriCount = triCentroids.Length;

                // Find closest valid triangle
                for (int t = 0; t < clothingTriCount; t++)
                {
                    float3 triCenter = triCentroids[t];
                    float3 triNormal = triNormals[t];
                    float3 bodyToTri = triCenter - bodyVertPos;
                    float distSq = math.lengthsq(bodyToTri);
                    float3 bodyToTriDir = math.normalizesafe(bodyToTri);

                    float offsetDot = math.dot(bodyToTriDir, bodyVertNormal);
                    if (offsetDot < minOffsetDot) continue;

                    float dot = math.dot(bodyToTriDir, triNormal);
                    if (dot > 0f)
                    {
                        float normalAlignment = math.dot(bodyVertNormal, triNormal) + offsetDot;
                        float score = distSq / (1f + math.max(0f, normalAlignment));

                        if (score < closestScore)
                        {
                            closestScore = score;
                            closestTriIndex = t;
                            closestDistSq = distSq;
                        }
                    }
                }

                if (closestTriIndex < 0 || closestDistSq > minDistSq)
                {
                    validFlags[bv] = 0;
                    return;
                }

                // Radial expansion using BFS
                NativeList<int> selectionOrder = new NativeList<int>(32, Allocator.Temp);
                NativeHashSet<int> selectedSet = new NativeHashSet<int>(32, Allocator.Temp);
                NativeQueue<int> queue = new NativeQueue<int>(Allocator.Temp);

                selectedSet.Add(closestTriIndex);
                selectionOrder.Add(closestTriIndex);
                queue.Enqueue(closestTriIndex);

                int maxIterations = 1000; // Hard limit to prevent infinite loops
                int iteration = 0;

                while (selectionOrder.Length < 32 && queue.Count > 0 && iteration < maxIterations)
                {
                    iteration++;
                    int currentTri = queue.Dequeue();

                    // Bounds check for adjacency offset
                    if (currentTri < 0 || currentTri >= triAdjacencyOffsets.Length)
                        continue;

                    int adjStart = triAdjacencyOffsets[currentTri];
                    int adjEnd = currentTri < triAdjacencyOffsets.Length - 1 ? triAdjacencyOffsets[currentTri + 1] : triAdjacencyData.Length;

                    // Validate adjacency range
                    if (adjStart < 0 || adjStart > triAdjacencyData.Length || adjEnd < adjStart || adjEnd > triAdjacencyData.Length)
                        continue;

                    for (int i = adjStart; i < adjEnd && selectionOrder.Length < 32; i++)
                    {
                        int neighborTri = triAdjacencyData[i];

                        // Validate neighbor index and check if already selected
                        if (neighborTri < 0 || neighborTri >= triCentroids.Length || selectedSet.Contains(neighborTri)) 
                            continue;

                        float3 triCenter = triCentroids[neighborTri];
                        float3 triNormal = triNormals[neighborTri];
                        float3 bodyToTri = triCenter - bodyVertPos;
                        float dot = math.dot(math.normalizesafe(bodyToTri), triNormal); 

                        if (dot > 0f)
                        {
                            selectedSet.Add(neighborTri);
                            selectionOrder.Add(neighborTri);

                            // Only enqueue if we haven't reached the limit
                            if (selectionOrder.Length < 32)
                            {
                                queue.Enqueue(neighborTri);
                            }
                        }
                    }
                }

                // Pack triangles
                PushBackVertex pushBack = new PushBackVertex { vertexIndex = bv };
                Triangles32 tris = new Triangles32();
                int4 empty = new int4(-1, -1, -1, -1);

                for (int i = 0; i < 32; i++)
                {
                    int val = i < selectionOrder.Length ? selectionOrder[i] : -1;
                    if (i < 4) { if (i == 0) tris.trianglesA.x = val; else if (i == 1) tris.trianglesA.y = val; else if (i == 2) tris.trianglesA.z = val; else tris.trianglesA.w = val; }
                    else if (i < 8) { if (i == 4) tris.trianglesB.x = val; else if (i == 5) tris.trianglesB.y = val; else if (i == 6) tris.trianglesB.z = val; else tris.trianglesB.w = val; }
                    else if (i < 12) { if (i == 8) tris.trianglesC.x = val; else if (i == 9) tris.trianglesC.y = val; else if (i == 10) tris.trianglesC.z = val; else tris.trianglesC.w = val; }
                    else if (i < 16) { if (i == 12) tris.trianglesD.x = val; else if (i == 13) tris.trianglesD.y = val; else if (i == 14) tris.trianglesD.z = val; else tris.trianglesD.w = val; }
                    else if (i < 20) { if (i == 16) tris.trianglesE.x = val; else if (i == 17) tris.trianglesE.y = val; else if (i == 18) tris.trianglesE.z = val; else tris.trianglesE.w = val; }
                    else if (i < 24) { if (i == 20) tris.trianglesF.x = val; else if (i == 21) tris.trianglesF.y = val; else if (i == 22) tris.trianglesF.z = val; else tris.trianglesF.w = val; }
                    else if (i < 28) { if (i == 24) tris.trianglesG.x = val; else if (i == 25) tris.trianglesG.y = val; else if (i == 26) tris.trianglesG.z = val; else tris.trianglesG.w = val; }
                    else { if (i == 28) tris.trianglesH.x = val; else if (i == 29) tris.trianglesH.y = val; else if (i == 30) tris.trianglesH.z = val; else tris.trianglesH.w = val; }
                }

                pushBack.pushBackTriangles = tris;
                results[bv] = pushBack;
                validFlags[bv] = 1;

                selectionOrder.Dispose();
                selectedSet.Dispose();
                queue.Dispose();
            }
        }

        /// <summary>
        /// Generate push-back triangles for body vertices: maps up to 32 clothing triangles to each body vertex.
        /// Only includes body vertices that are within minDistance of any clothing triangle and where the body vertex
        /// is behind (opposite direction of normal) the clothing triangles.
        /// JOBIFIED VERSION - Uses Unity Jobs System for parallel processing.
        /// </summary>
        public static PushBackVertex[] GeneratePushBackTriangles(Mesh bodyMesh, Mesh clothingMesh, Quaternion clothingToBodyRot, float minDistance = 0.08f, float minOffsetDot = 0.75f)
        {
            if (bodyMesh == null || clothingMesh == null)
                throw new ArgumentNullException(bodyMesh == null ? nameof(bodyMesh) : nameof(clothingMesh));

            var bodyVerts = bodyMesh.vertices;
            var bodyNormals = bodyMesh.normals;
            var clothingVerts = clothingMesh.vertices;
            var clothingNormals = clothingMesh.normals;
            var clothingTris = clothingMesh.triangles;
            int clothingTriCount = clothingTris.Length / 3;

            bool hasBodyNormals = bodyNormals != null && bodyNormals.Length == bodyVerts.Length;
            bool hasClothingNormals = clothingNormals != null && clothingNormals.Length == clothingVerts.Length;

            // Build vertex->triangle adjacency
            List<int>[] vertexToTriangles = new List<int>[clothingVerts.Length];
            for (int v = 0; v < vertexToTriangles.Length; v++) vertexToTriangles[v] = new List<int>();
            for (int t = 0; t < clothingTriCount; t++)
            {
                int a = clothingTris[t * 3 + 0];
                int b = clothingTris[t * 3 + 1];
                int c = clothingTris[t * 3 + 2];
                vertexToTriangles[a].Add(t);
                vertexToTriangles[b].Add(t);
                vertexToTriangles[c].Add(t);
            }

            // Build triangle->triangle adjacency and flatten for NativeArray
            List<int> adjDataList = new List<int>();
            List<int> adjOffsetsList = new List<int>();

            for (int t = 0; t < clothingTriCount; t++)
            {
                adjOffsetsList.Add(adjDataList.Count);
                int a = clothingTris[t * 3 + 0];
                int b = clothingTris[t * 3 + 1];
                int c = clothingTris[t * 3 + 2];
                var neighborSet = new HashSet<int>();
                foreach (var neighbor in vertexToTriangles[a]) if (neighbor != t) neighborSet.Add(neighbor);
                foreach (var neighbor in vertexToTriangles[b]) if (neighbor != t) neighborSet.Add(neighbor);
                foreach (var neighbor in vertexToTriangles[c]) if (neighbor != t) neighborSet.Add(neighbor);
                adjDataList.AddRange(neighborSet);
            }

            // Pre-compute triangle centroids and normals
            NativeArray<float3> triCentroids = new NativeArray<float3>(clothingTriCount, Allocator.TempJob);
            NativeArray<float3> triNormals = new NativeArray<float3>(clothingTriCount, Allocator.TempJob);

            for (int t = 0; t < clothingTriCount; t++)
            {
                int a = clothingTris[t * 3 + 0];
                int b = clothingTris[t * 3 + 1];
                int c = clothingTris[t * 3 + 2];

                float3 vA = clothingToBodyRot * (Vector3)clothingVerts[a];
                float3 vB = clothingToBodyRot * (Vector3)clothingVerts[b];
                float3 vC = clothingToBodyRot * (Vector3)clothingVerts[c];

                triCentroids[t] = (vA + vB + vC) / 3f;

                float3 edge1 = vB - vA;
                float3 edge2 = vC - vA;
                float3 faceNormal = math.normalizesafe(math.cross(edge1, edge2));

                if (hasClothingNormals)
                {
                    float3 avgNormal = math.normalizesafe(clothingToBodyRot * ((Vector3)(clothingNormals[a] + clothingNormals[b] + clothingNormals[c])));
                    triNormals[t] = avgNormal;
                }
                else
                {
                    triNormals[t] = faceNormal;
                }
            }

            // Prepare job data
            NativeArray<float3> nativeBodyVerts = new NativeArray<float3>(bodyVerts.Length, Allocator.TempJob);
            NativeArray<float3> nativeBodyNormals = new NativeArray<float3>(bodyVerts.Length, Allocator.TempJob);
            for (int i = 0; i < bodyVerts.Length; i++)
            {
                nativeBodyVerts[i] = bodyVerts[i];
                nativeBodyNormals[i] = hasBodyNormals ? (float3)bodyNormals[i] : new float3(0, 1, 0);
            }

            NativeArray<int> triAdjacencyOffsets = new NativeArray<int>(adjOffsetsList.ToArray(), Allocator.TempJob);
            NativeArray<int> triAdjacencyData = new NativeArray<int>(adjDataList.ToArray(), Allocator.TempJob);
            NativeArray<PushBackVertex> results = new NativeArray<PushBackVertex>(bodyVerts.Length, Allocator.TempJob);
            NativeArray<int> validFlags = new NativeArray<int>(bodyVerts.Length, Allocator.TempJob);

            // Schedule job
            var job = new PushBackTrianglesJob
            {
                bodyVerts = nativeBodyVerts,
                bodyNormals = nativeBodyNormals,
                triCentroids = triCentroids,
                triNormals = triNormals,
                triAdjacencyOffsets = triAdjacencyOffsets,
                triAdjacencyData = triAdjacencyData,
                minDistSq = minDistance * minDistance,
                minOffsetDot = minOffsetDot,
                results = results,
                validFlags = validFlags
            };

            JobHandle handle = job.Schedule(bodyVerts.Length, 32);
            handle.Complete();

            // Collect valid results
            var finalResult = new List<PushBackVertex>();
            for (int i = 0; i < bodyVerts.Length; i++)
            {
                if (validFlags[i] == 1)
                {
                    finalResult.Add(results[i]);
                }
            }

            // Cleanup
            nativeBodyVerts.Dispose();
            nativeBodyNormals.Dispose();
            triCentroids.Dispose();
            triNormals.Dispose();
            triAdjacencyOffsets.Dispose();
            triAdjacencyData.Dispose();
            results.Dispose();
            validFlags.Dispose();

            if (finalResult.Count <= 0) Debug.LogError($"No push back vertices found for body {bodyMesh.name}.");

            return finalResult.ToArray();
        }

        /// <summary>
        /// Generate push-back triangles for body vertices: maps up to 32 clothing triangles to each body vertex.
        /// ORIGINAL NON-JOBIFIED VERSION - Kept for reference/fallback.
        /// </summary>
        private static PushBackVertex[] GeneratePushBackTriangles_Original(Mesh bodyMesh, Mesh clothingMesh, Quaternion clothingToBodyRot, float minDistance = 0.08f, float minOffsetDot = 0.75f)
        {
            if (bodyMesh == null || clothingMesh == null)
                throw new ArgumentNullException(bodyMesh == null ? nameof(bodyMesh) : nameof(clothingMesh));

            var bodyVerts = bodyMesh.vertices;
            var bodyNormals = bodyMesh.normals;
            var clothingVerts = clothingMesh.vertices;
            var clothingNormals = clothingMesh.normals;
            var clothingTris = clothingMesh.triangles;
            int clothingTriCount = clothingTris.Length / 3;

            bool hasBodyNormals = bodyNormals != null && bodyNormals.Length == bodyVerts.Length;
            bool hasClothingNormals = clothingNormals != null && clothingNormals.Length == clothingVerts.Length;

            var result = new List<PushBackVertex>();

            // Build triangle adjacency: for each clothing triangle, store its neighbors
            List<int>[] triToTriangles = new List<int>[clothingTriCount];
            for (int t = 0; t < clothingTriCount; t++) triToTriangles[t] = new List<int>();

            // Build vertex->triangle adjacency for clothing mesh
            List<int>[] vertexToTriangles = new List<int>[clothingVerts.Length];
            for (int v = 0; v < vertexToTriangles.Length; v++) vertexToTriangles[v] = new List<int>();
            for (int t = 0; t < clothingTriCount; t++)
            {
                int a = clothingTris[t * 3 + 0];
                int b = clothingTris[t * 3 + 1];
                int c = clothingTris[t * 3 + 2];
                vertexToTriangles[a].Add(t);
                vertexToTriangles[b].Add(t);
                vertexToTriangles[c].Add(t);
            }

            // Build triangle->triangle adjacency (triangles sharing at least one vertex)
            for (int t = 0; t < clothingTriCount; t++)
            {
                int a = clothingTris[t * 3 + 0];
                int b = clothingTris[t * 3 + 1];
                int c = clothingTris[t * 3 + 2];
                var neighborSet = new HashSet<int>();
                foreach (var neighbor in vertexToTriangles[a]) if (neighbor != t) neighborSet.Add(neighbor);
                foreach (var neighbor in vertexToTriangles[b]) if (neighbor != t) neighborSet.Add(neighbor);
                foreach (var neighbor in vertexToTriangles[c]) if (neighbor != t) neighborSet.Add(neighbor);
                triToTriangles[t].AddRange(neighborSet);
            }

            // Pre-compute triangle centroids and normals
            Vector3[] triCentroids = new Vector3[clothingTriCount];
            Vector3[] triNormals = new Vector3[clothingTriCount];
            //Vector3[] triPoints = new Vector3[clothingTriCount * 3];
            for (int t = 0; t < clothingTriCount; t++)
            {
                var t3 = t * 3;
                int a = clothingTris[t3 + 0];
                int b = clothingTris[t3 + 1];
                int c = clothingTris[t3 + 2];

                Vector3 vA = clothingToBodyRot * clothingVerts[a];
                Vector3 vB = clothingToBodyRot * clothingVerts[b];
                Vector3 vC = clothingToBodyRot * clothingVerts[c];

                triCentroids[t] = (vA + vB + vC) / 3f;
                //triPoints[t3 + 0] = vA;
                //triPoints[t3 + 1] = vB;
                //triPoints[t3 + 2] = vC;

                // Calculate face normal
                Vector3 edge1 = vB - vA;
                Vector3 edge2 = vC - vA;
                Vector3 faceNormal = Vector3.Cross(edge1, edge2).normalized;

                // If vertex normals available, average them for better accuracy
                if (hasClothingNormals)
                {
                    Vector3 avgNormal = (clothingToBodyRot * (clothingNormals[a] + clothingNormals[b] + clothingNormals[c])).normalized;
                    triNormals[t] = avgNormal;
                }
                else
                {
                    triNormals[t] = faceNormal;
                }
            }

            float minDistSq = minDistance * minDistance;

            // For each body vertex, find clothing triangles using radial expansion
            for (int bv = 0; bv < bodyVerts.Length; bv++)
            {
                Vector3 bodyVertPos = bodyVerts[bv];
                Vector3 bodyVertNormal = hasBodyNormals ? bodyNormals[bv].normalized : Vector3.up;

                // Find the closest clothing triangle that meets the "behind" criteria
                int closestTriIndex = -1;
                float closestScore = float.MaxValue;
                float closestDistSq = float.MaxValue;

                for (int t = 0; t < clothingTriCount; t++)
                {
                    Vector3 triCenter = triCentroids[t];
                    Vector3 triNormal = triNormals[t];

                    // Vector from body vertex to triangle centroid
                    Vector3 bodyToTri = triCenter - bodyVertPos;
                    float distSq = bodyToTri.sqrMagnitude;
                    Vector3 bodyToTriDir = bodyToTri.normalized;
                    
                    float offsetDot = Vector3.Dot(bodyToTriDir, bodyVertNormal);
                    if (offsetDot < minOffsetDot)
                        continue; // Skip if body vertex normal is not sufficiently aligned with direction to triangle

                    // Check if body vertex is behind the triangle
                    float dot = Vector3.Dot(bodyToTriDir, triNormal);

                    // Only consider if dot > 0 (body vertex is behind the triangle)
                    if (dot > 0f)
                    {
                        // Score: prefer closer triangles weighted by normal alignment
                        float normalAlignment = Vector3.Dot(bodyVertNormal, triNormal) + offsetDot; 
                        float score = distSq / (1f + Mathf.Max(0f, normalAlignment));

                        if (score < closestScore)
                        {
                            closestScore = score;
                            closestTriIndex = t;
                            closestDistSq = distSq;
                        }
                    }
                }

                // If no valid triangle found or closest is too far, skip this body vertex
                if (closestTriIndex < 0 || closestDistSq > minDistSq)
                    continue;

                // Radial expansion: fan out from the closest triangle to collect up to 32 triangles
                var selectedSet = new HashSet<int>();
                var selectionOrder = new List<int>();
                var queue = new Queue<int>();

                // Start with the closest triangle
                selectedSet.Add(closestTriIndex);
                selectionOrder.Add(closestTriIndex);
                queue.Enqueue(closestTriIndex);

                // Expand radially through adjacency until we have 32 triangles or run out
                while (selectedSet.Count < 32 && queue.Count > 0)
                {
                    int currentTri = queue.Dequeue();

                    // Add neighbors that are also "behind" the body vertex
                    foreach (var neighborTri in triToTriangles[currentTri])
                    {
                        if (selectedSet.Contains(neighborTri))
                            continue;

                        Vector3 triCenter = triCentroids[neighborTri];
                        Vector3 triNormal = triNormals[neighborTri];
                        Vector3 bodyToTri = triCenter - bodyVertPos;
                        float dot = Vector3.Dot(bodyToTri.normalized, triNormal);

                        // Only include if body vertex is behind this triangle too
                        if (dot > 0f)
                        {
                            selectedSet.Add(neighborTri);
                            selectionOrder.Add(neighborTri);
                            queue.Enqueue(neighborTri);

                            if (selectedSet.Count >= 32)
                                break;
                        }
                    }
                }

                // Pack up to 32 triangle indices
                int[] packed = new int[32];
                for (int k = 0; k < 32; k++) packed[k] = -1;
                for (int k = 0; k < selectionOrder.Count && k < 32; k++)
                {
                    packed[k] = selectionOrder[k];
                }

                var pushBack = new PushBackVertex
                {
                    vertexIndex = bv,
                    pushBackTriangles = new Triangles32
                    {
                        trianglesA = new int4(packed[0], packed[1], packed[2], packed[3]),
                        trianglesB = new int4(packed[4], packed[5], packed[6], packed[7]),
                        trianglesC = new int4(packed[8], packed[9], packed[10], packed[11]),
                        trianglesD = new int4(packed[12], packed[13], packed[14], packed[15]),
                        trianglesE = new int4(packed[16], packed[17], packed[18], packed[19]),
                        trianglesF = new int4(packed[20], packed[21], packed[22], packed[23]),
                        trianglesG = new int4(packed[24], packed[25], packed[26], packed[27]),
                        trianglesH = new int4(packed[28], packed[29], packed[30], packed[31])
                    }
                };

                result.Add(pushBack);
            }

            if (result.Count <= 0) Debug.LogError($"No push back vertices found for body {bodyMesh.name}. Are the meshes properly aligned?");

            return result.ToArray();
        }

        // Job struct for covered vertex mask generation
        [BurstCompile]
        private struct CoveredVertexMaskJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<float3> bodyVerts;
            [ReadOnly] public NativeArray<float3> bodyNormals;
            [ReadOnly] public NativeArray<float3> clothingTriA;
            [ReadOnly] public NativeArray<float3> clothingTriB;
            [ReadOnly] public NativeArray<float3> clothingTriC;
            [ReadOnly] public NativeArray<float3> clothingTriNormals;
            [ReadOnly] public NativeArray<float3> bodyTriA;
            [ReadOnly] public NativeArray<float3> bodyTriB;
            [ReadOnly] public NativeArray<float3> bodyTriC;
            [ReadOnly] public NativeArray<float3> bodyTriNormals;
            [ReadOnly] public NativeArray<float3> viewDirections;
            [ReadOnly] public NativeArray<float3> hemisphereSamples;
            [ReadOnly] public float maxPrimaryDistance;
            [ReadOnly] public int bounceCount;
            [ReadOnly] public float bounceDistance;
            [ReadOnly] public float selfOcclusionDistance;
            [ReadOnly] public float minMaskThreshold;

            [WriteOnly] public NativeArray<MaskedVertex> results;
            [WriteOnly] public NativeArray<int> validFlags;

            public void Execute(int bv)
            {
                float3 bodyVertPos = bodyVerts[bv];
                float3 bodyVertNormal = math.normalizesafe(bodyNormals[bv], new float3(0, 1, 0));

                int clothingBlockedCount = 0; // Only count clothing occlusion
                int selfOccludedBehindClothing = 0; // Self-occlusion only matters if there's clothing
                float totalOcclusionDepth = 0f;
                int testedViewAngles = 0;

                for (int v = 0; v < viewDirections.Length; v++)
                {
                    float3 viewDir = viewDirections[v];

                    float normalAlignment = math.dot(viewDir, bodyVertNormal);
                    if (normalAlignment < -0.1f) continue; // Skip back-facing views

                    testedViewAngles++;

                    // Test primary ray against clothing FIRST
                    bool clothingBlocked = false;
                    float clothingHitDist = float.MaxValue;
                    float3 clothingHitPoint = float3.zero;
                    float3 clothingHitNormal = float3.zero;

                    for (int t = 0; t < clothingTriA.Length; t++)
                    {
                        if (RayTriangleIntersect(bodyVertPos, viewDir, clothingTriA[t], clothingTriB[t], clothingTriC[t], clothingTriNormals[t], out float hitDist))
                        {
                            if (hitDist > 0f && hitDist < clothingHitDist && hitDist <= maxPrimaryDistance)
                            {
                                clothingHitDist = hitDist;
                                clothingBlocked = true;
                                clothingHitPoint = bodyVertPos + viewDir * hitDist;
                                clothingHitNormal = clothingTriNormals[t];
                            }
                        }
                    }

                    // If clothing is blocking this view, test for escape via bounces
                    if (clothingBlocked)
                    {
                        bool escapeFound = false;

                        // Try bounce rays to see if vertex is still visible indirectly
                        // Much more restrictive - need a clear path out
                        for (int b = 0; b < bounceCount && !escapeFound; b++)
                        {
                            float3 reflectedDir = math.reflect(viewDir, clothingHitNormal);

                            // Use fewer bounce samples to be less forgiving
                            int bounceSamplesToTest = hemisphereSamples.Length / 2; // Only test half

                            for (int bd = 0; bd < bounceSamplesToTest; bd++)
                            {
                                float3 bounceDir = OrientToNormal(hemisphereSamples[bd], reflectedDir);

                                bool bounceBlocked = false;
                                for (int t = 0; t < clothingTriA.Length; t++)
                                {
                                    if (RayTriangleIntersect(clothingHitPoint + bounceDir * 0.001f, bounceDir, clothingTriA[t], clothingTriB[t], clothingTriC[t], clothingTriNormals[t], out float bounceDist))
                                    {
                                        if (bounceDist > 0f && bounceDist < bounceDistance)
                                        {
                                            bounceBlocked = true;
                                            break;
                                        }
                                    }
                                }

                                if (!bounceBlocked)
                                {
                                    escapeFound = true;
                                    break;
                                }
                            }
                        }

                        // If no escape found, this view is definitely blocked by clothing
                        if (!escapeFound)
                        {
                            clothingBlockedCount++;

                            // Depth contribution: inverse square to heavily favor close clothing
                            float normalizedDist = clothingHitDist / maxPrimaryDistance;
                            float depthContribution = 1f - (normalizedDist * normalizedDist);
                            totalOcclusionDepth += depthContribution;

                            // Now check if there's also body self-occlusion behind the clothing
                            // This adds extra masking weight for deep coverage
                            for (int t = 0; t < bodyTriA.Length; t++)
                            {
                                if (RayTriangleIntersect(bodyVertPos + viewDir * 0.001f, viewDir, bodyTriA[t], bodyTriB[t], bodyTriC[t], bodyTriNormals[t], out float bodyHitDist))
                                {
                                    if (bodyHitDist > 0f && bodyHitDist < clothingHitDist && bodyHitDist < selfOcclusionDistance)
                                    {
                                        selfOccludedBehindClothing++;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                // Only create masked vertex if there's actual clothing occlusion
                if (testedViewAngles > 0 && clothingBlockedCount > 0)
                {
                    // Ratio of views blocked by clothing (0-1)
                    float clothingBlockRatio = (float)clothingBlockedCount / testedViewAngles;

                    // Average depth of occlusion (0-1, where 1 = very close, 0 = far)
                    float avgDepth = totalOcclusionDepth / clothingBlockedCount;

                    // Aggressive base masking: heavily weight the block ratio
                    // Use power curve to make partial coverage more aggressive
                    float baseMasking = math.pow(clothingBlockRatio, 0.7f); // Power < 1 = more aggressive

                    // Depth bonus: closer clothing gets much higher masking
                    // Square the depth to heavily favor close coverage
                    float depthBonus = math.pow(avgDepth, 1.5f) * 0.5f;

                    float masking = math.min(1f, baseMasking + depthBonus);

                    // Major boost for deep coverage (self-occlusion behind clothing)
                    if (selfOccludedBehindClothing > 0)
                    {
                        float deepCoverageFactor = (float)selfOccludedBehindClothing / clothingBlockedCount;
                        // Deep vertices get pushed much harder toward full masking
                        masking = math.lerp(masking, 1f, deepCoverageFactor * 0.7f);
                    }

                    // Additional boost based on how many views are blocked
                    // If > 50% of views blocked, add exponential bonus
                    if (clothingBlockRatio > 0.5f)
                    {
                        float majorityBonus = math.pow((clothingBlockRatio - 0.5f) * 2f, 1.5f) * 0.3f;
                        masking = math.min(1f, masking + majorityBonus);
                    }

                    if (masking >= minMaskThreshold)
                    {
                        results[bv] = new MaskedVertex
                        {
                            vertexIndex = bv,
                            masking = math.clamp(masking, 0f, 1f)
                        };
                        validFlags[bv] = 1;
                        return;
                    }
                }

                validFlags[bv] = 0;
            }

            private bool RayTriangleIntersect(float3 rayOrigin, float3 rayDir, float3 v0, float3 v1, float3 v2, float3 triNormal, out float hitDistance)
            {
                hitDistance = 0f;

                float ndotRay = math.dot(triNormal, rayDir);
                if (math.abs(ndotRay) < 1e-6f) return false;

                float3 edge1 = v1 - v0;
                float3 edge2 = v2 - v0;
                float3 h = math.cross(rayDir, edge2);
                float a = math.dot(edge1, h);

                if (math.abs(a) < 1e-6f) return false;

                float f = 1f / a;
                float3 s = rayOrigin - v0;
                float u = f * math.dot(s, h);

                if (u < 0f || u > 1f) return false;

                float3 q = math.cross(s, edge1);
                float v = f * math.dot(rayDir, q);

                if (v < 0f || u + v > 1f) return false;

                float t = f * math.dot(edge2, q);
                hitDistance = t;

                return t > 1e-6f;
            }

            private float3 OrientToNormal(float3 direction, float3 normal)
            {
                float3 up = math.abs(normal.y) < 0.999f ? new float3(0, 1, 0) : new float3(0, 0, 1);
                float3 tangent = math.normalize(math.cross(up, normal));
                float3 bitangent = math.cross(normal, tangent);

                return tangent * direction.x + bitangent * direction.y + normal * direction.z;
            }
        }

        // Fast job struct for simplified coverage mask based on triangle area
        [BurstCompile]
        private struct FastCoveredVertexMaskJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<float3> bodyVerts;
            [ReadOnly] public NativeArray<float3> bodyNormals;
            [ReadOnly] public NativeArray<float3> clothingTriCentroids;
            [ReadOnly] public NativeArray<float3> clothingTriNormals;
            [ReadOnly] public NativeArray<float> clothingTriAreas;
            [ReadOnly] public float maxDistance;
            [ReadOnly] public float areaThreshold;
            [ReadOnly] public float minNormalDot;

            [WriteOnly] public NativeArray<float> maskingResults;

            public void Execute(int bv)
            {
                float3 bodyVertPos = bodyVerts[bv];
                float3 bodyVertNormal = math.normalizesafe(bodyNormals[bv], new float3(0, 1, 0));

                float maxDistSq = maxDistance * maxDistance;  

                // Find closest valid triangle FAST
                int closestTriIndex = -1;
                float closestDistSq = float.MaxValue;
                float closestNormalDot = 0f;

                // Early-out check: sample every Nth triangle first for quick rejection
                int sampleStride = math.max(1, clothingTriCentroids.Length / 100);
                bool anyNearby = false;

                for (int t = 0; t < clothingTriCentroids.Length; t += sampleStride)
                {
                    float distSq = math.distancesq(bodyVertPos, clothingTriCentroids[t]);
                    if (distSq < maxDistSq * 4f) // Generous range for early detection
                    {
                        anyNearby = true;
                        break;
                    }
                }

                if (!anyNearby)
                {
                    maskingResults[bv] = 0f;
                    return;
                }

                // Find closest triangle with valid normal
                for (int t = 0; t < clothingTriCentroids.Length; t++)
                {
                    float3 triCenter = clothingTriCentroids[t];
                    float distSq = math.distancesq(bodyVertPos, triCenter);

                    if (distSq > maxDistSq)
                        continue;

                    float3 triNormal = clothingTriNormals[t];
                    float normalDot = math.dot(bodyVertNormal, triNormal);

                    if (normalDot < minNormalDot)
                        continue;

                    if (distSq < closestDistSq)
                    {
                        closestDistSq = distSq;
                        closestTriIndex = t;
                        closestNormalDot = normalDot;
                    }
                }

                if (closestTriIndex < 0)
                {
                    maskingResults[bv] = 0f;
                    return;
                }

                // Accumulate area from ONLY nearby triangles (within 2x distance of closest)
                float searchRadiusSq = closestDistSq * 4f; // 2x radius
                searchRadiusSq = math.min(searchRadiusSq, maxDistSq);

                float totalArea = 0f;
                float closestDist = math.sqrt(closestDistSq);

                // Sample triangles near the closest one
                for (int t = 0; t < clothingTriCentroids.Length; t++)
                {
                    float3 triCenter = clothingTriCentroids[t];
                    float distSq = math.distancesq(bodyVertPos, triCenter);

                    if (distSq > searchRadiusSq)
                        continue;

                    float3 triNormal = clothingTriNormals[t];
                    float normalDot = math.dot(bodyVertNormal, triNormal);

                    if (normalDot < minNormalDot)
                        continue;

                    // Weight by distance and normal
                    float dist = math.sqrt(distSq);
                    float distWeight = 1f - (dist / maxDistance);
                    float weight = distWeight * distWeight * normalDot;

                    totalArea += clothingTriAreas[t] * weight;
                }

                // Calculate masking
                float normalizedArea = math.min(totalArea / areaThreshold, 2f);
                float masking = 1f - math.exp(-normalizedArea * 3f);

                // Proximity boost
                if (closestDist < maxDistance * 0.3f)
                {
                    float proximityBoost = 1f - (closestDist / (maxDistance * 0.3f));
                    masking = math.lerp(masking, 1f, proximityBoost * 0.3f);
                }

                // Store masking value directly (threshold applied after blur)
                maskingResults[bv] = math.clamp(masking, 0f, 1f);
            }
        }

        /// <summary>
        /// FAST simplified coverage mask generation based on triangle area accumulation.
        /// Much faster than the ray-casting version - use this for real-time or frequent regeneration.
        /// Determines masking by calculating the weighted area of nearby clothing triangles.
        /// </summary>
        /// <param name="bodyMesh">The body mesh</param>
        /// <param name="clothingMesh">The clothing mesh</param>
        /// <param name="clothingToBodyRot">Rotation to align clothing to body space</param>
        /// <param name="maxDistance">Maximum distance to consider clothing triangles (default 0.15)</param>
        /// <param name="areaThreshold">Area threshold for full masking (default 0.04)</param>
        /// <param name="minNormalDot">Minimum dot product between normals (default 0.3)</param>
        /// <param name="minMaskThreshold">Minimum masking value to include vertex</param>
        /// <returns>Array of masked vertices with masking values</returns>
        public static MaskedVertex[] GenerateCoveredVertexMaskFast(
            Mesh bodyMesh,
            Mesh clothingMesh,
            Quaternion clothingToBodyRot,
            float maxDistance = 0.15f,
            float areaThreshold = 0.04f,
            float minNormalDot = 0.3f,
            float minMaskThreshold = 0.001f)
        {
            if (bodyMesh == null || clothingMesh == null)
                throw new ArgumentNullException(bodyMesh == null ? nameof(bodyMesh) : nameof(clothingMesh));

            var bodyVerts = bodyMesh.vertices;
            var bodyNormals = bodyMesh.normals;
            var clothingVerts = clothingMesh.vertices;
            var clothingTris = clothingMesh.triangles;
            int clothingTriCount = clothingTris.Length / 3;

            bool hasBodyNormals = bodyNormals != null && bodyNormals.Length == bodyVerts.Length;

            // ULTRA-FAST: Just find closest triangle and sample its neighbors within distance
            // Pre-compute triangle data
            NativeArray<float3> clothingTriCentroids = new NativeArray<float3>(clothingTriCount, Allocator.TempJob);
            NativeArray<float3> clothingTriNormals = new NativeArray<float3>(clothingTriCount, Allocator.TempJob);
            NativeArray<float> clothingTriAreas = new NativeArray<float>(clothingTriCount, Allocator.TempJob);

            for (int t = 0; t < clothingTriCount; t++)
            {
                int idxA = clothingTris[t * 3 + 0];
                int idxB = clothingTris[t * 3 + 1];
                int idxC = clothingTris[t * 3 + 2];

                float3 vA = clothingToBodyRot * (Vector3)clothingVerts[idxA];
                float3 vB = clothingToBodyRot * (Vector3)clothingVerts[idxB];
                float3 vC = clothingToBodyRot * (Vector3)clothingVerts[idxC];

                clothingTriCentroids[t] = (vA + vB + vC) / 3f;

                float3 edge1 = vB - vA;
                float3 edge2 = vC - vA;
                float3 crossProd = math.cross(edge1, edge2);

                clothingTriNormals[t] = math.normalize(crossProd);
                clothingTriAreas[t] = math.length(crossProd) * 0.5f;
            }

            // Prepare body vertex data
            NativeArray<float3> nativeBodyVerts = new NativeArray<float3>(bodyVerts.Length, Allocator.TempJob);
            NativeArray<float3> nativeBodyNormals = new NativeArray<float3>(bodyVerts.Length, Allocator.TempJob);
            for (int i = 0; i < bodyVerts.Length; i++)
            {
                nativeBodyVerts[i] = bodyVerts[i];
                nativeBodyNormals[i] = hasBodyNormals ? (float3)bodyNormals[i] : new float3(0, 1, 0);
            }

            NativeArray<float> maskingResults = new NativeArray<float>(bodyVerts.Length, Allocator.TempJob);

            // Schedule job with much smaller batch size for better parallelization
            var job = new FastCoveredVertexMaskJob
            {
                bodyVerts = nativeBodyVerts,
                bodyNormals = nativeBodyNormals,
                clothingTriCentroids = clothingTriCentroids,
                clothingTriNormals = clothingTriNormals,
                clothingTriAreas = clothingTriAreas,
                maxDistance = maxDistance,
                areaThreshold = areaThreshold,
                minNormalDot = minNormalDot,
                maskingResults = maskingResults
            };

            // CRITICAL: Use batch size of 1 for maximum parallelization
            JobHandle handle = job.Schedule(bodyVerts.Length, 1);
            handle.Complete();

            // POST-PROCESSING: Directional blur to reduce masking near openings
            // Allow unmasked areas to bleed INTO masked areas (not vice versa)
            // This creates falloff gradients near hems, sleeves, collars, etc.

            // WELD VERTICES: Treat duplicate vertices as the same vertex during blur
            var weldedVertices = MeshDataTools.WeldVertices(bodyVerts, 0.00001f);

            // Build welded vertex groups (only store unique groups)
            Dictionary<int, List<int>> weldedGroups = new Dictionary<int, List<int>>();
            for (int i = 0; i < weldedVertices.Length; i++)
            {
                var welded = weldedVertices[i];
                int groupId = welded.firstIndex;
                if (!weldedGroups.ContainsKey(groupId))
                {
                    weldedGroups[groupId] = welded.indices ?? new List<int> { groupId };
                }
            }

            // Build masking map with unified welded vertices
            NativeArray<float> maskingMap = new NativeArray<float>(bodyVerts.Length, Allocator.TempJob);
            foreach (var group in weldedGroups.Values)
            {
                // Average masking across all welded vertices in this group
                float avgMasking = 0f;
                foreach (int idx in group)
                {
                    avgMasking += maskingResults[idx];
                }
                avgMasking /= group.Count;

                // Apply same masking to ALL vertices in the welded group
                foreach (int idx in group)
                {
                    maskingMap[idx] = avgMasking;
                }
            }

            // Build vertex connectivity from body mesh triangles
            var bodyTris = bodyMesh.triangles;
            List<int>[] vertexNeighbors = new List<int>[bodyVerts.Length];
            for (int v = 0; v < vertexNeighbors.Length; v++) vertexNeighbors[v] = new List<int>();

            for (int t = 0; t < bodyTris.Length / 3; t++)
            {
                int a = bodyTris[t * 3 + 0];
                int b = bodyTris[t * 3 + 1];
                int c = bodyTris[t * 3 + 2];

                if (!vertexNeighbors[a].Contains(b)) vertexNeighbors[a].Add(b);
                if (!vertexNeighbors[a].Contains(c)) vertexNeighbors[a].Add(c);
                if (!vertexNeighbors[b].Contains(a)) vertexNeighbors[b].Add(a);
                if (!vertexNeighbors[b].Contains(c)) vertexNeighbors[b].Add(c);
                if (!vertexNeighbors[c].Contains(a)) vertexNeighbors[c].Add(a);
                if (!vertexNeighbors[c].Contains(b)) vertexNeighbors[c].Add(b); 
            }

            // Flatten neighbors for NativeArray
            List<int> neighborData = new List<int>();
            List<int> neighborOffsets = new List<int>();
            for (int v = 0; v < vertexNeighbors.Length; v++)
            {
                neighborOffsets.Add(neighborData.Count);
                neighborData.AddRange(vertexNeighbors[v]);
            }

            NativeArray<int> nativeNeighborOffsets = new NativeArray<int>(neighborOffsets.ToArray(), Allocator.TempJob);
            NativeArray<int> nativeNeighborData = new NativeArray<int>(neighborData.ToArray(), Allocator.TempJob);
            NativeArray<float> blurredMaskingMap = new NativeArray<float>(bodyVerts.Length, Allocator.TempJob);

            // Flatten welded groups for job system and build vertex-to-group lookup
            List<int> weldedGroupData = new List<int>();
            List<int> weldedGroupOffsets = new List<int>();
            int[] vertexToGroupIndex = new int[bodyVerts.Length];
            for (int i = 0; i < vertexToGroupIndex.Length; i++) vertexToGroupIndex[i] = -1; // -1 = not welded

            int currentGroupIndex = 0;
            foreach (var group in weldedGroups.Values)
            {
                weldedGroupOffsets.Add(weldedGroupData.Count);
                foreach (int vertexIndex in group)
                {
                    weldedGroupData.Add(vertexIndex);
                    vertexToGroupIndex[vertexIndex] = currentGroupIndex;
                }
                currentGroupIndex++;
            }
            weldedGroupOffsets.Add(weldedGroupData.Count); // Add final offset for bounds checking

            NativeArray<int> nativeWeldedGroupOffsets = new NativeArray<int>(weldedGroupOffsets.ToArray(), Allocator.TempJob);
            NativeArray<int> nativeWeldedGroupData = new NativeArray<int>(weldedGroupData.ToArray(), Allocator.TempJob);
            NativeArray<int> nativeVertexToGroupIndex = new NativeArray<int>(vertexToGroupIndex, Allocator.TempJob);

            // Run multiple blur passes for heavy smoothing
            int blurPasses = 1024;
            /*int batchSize = 32; // Complete every N passes to prevent job queue overflow

            JobHandle previousHandle = default;

            for (int pass = 0; pass < blurPasses; pass++)
            {
                // Blur: always read from maskingMap, write to blurredMaskingMap
                var blurJob = new DirectionalBlurJob
                {
                    inputMasking = maskingMap,
                    outputMasking = blurredMaskingMap,
                    neighborOffsets = nativeNeighborOffsets,
                    neighborData = nativeNeighborData,
                    blurStrength = 0.99f
                };

                JobHandle blurHandle = blurJob.Schedule(bodyVerts.Length, 64, previousHandle);

                // Sync: read blur output (blurredMaskingMap), write back to maskingMap
                var syncJob = new SyncWeldedVerticesJob
                {
                    inputMasking = blurredMaskingMap,
                    outputMasking = maskingMap,
                    weldedGroupOffsets = nativeWeldedGroupOffsets,
                    weldedGroupData = nativeWeldedGroupData,
                    vertexToGroupIndex = nativeVertexToGroupIndex
                };

                previousHandle = syncJob.Schedule(bodyVerts.Length, 64, blurHandle);

                // CRITICAL: Complete in batches to prevent Unity job scheduler deadlock
                // Without this, 1024 chained jobs can overflow the job system's internal queue
                if ((pass + 1) % batchSize == 0 || pass == blurPasses - 1)
                {
                    previousHandle.Complete();
                    previousHandle = default; 
                }
            }  */

            // Allocate a temporary 3rd tracking buffer for the sync ping-pong step
            var syncMaskingMap = new NativeArray<float>(maskingMap.Length, Allocator.TempJob);  

            var singleFrameJob = new CombinedBlurAndSyncJob
            {
                maskingMap = maskingMap,
                blurredMaskingMap = blurredMaskingMap,
                syncMaskingMap = syncMaskingMap,
                neighborOffsets = nativeNeighborOffsets,
                neighborData = nativeNeighborData,
                weldedGroupOffsets = nativeWeldedGroupOffsets,
                weldedGroupData = nativeWeldedGroupData,
                vertexToGroupIndex = nativeVertexToGroupIndex,
                totalBlurPasses = blurPasses,
                blurStrength = 0.99f,
                totalVertices = bodyVerts.Length
            };

            // 1. Schedule EXACTLY one job handle
            JobHandle finalHandle = singleFrameJob.Schedule();

            // 2. Call Complete EXACTLY once. Burst handles the thousands of iterations instantly.
            finalHandle.Complete();

            // 3. Dispose of temporary buffer
            syncMaskingMap.Dispose();

            // Result is always in maskingMap after sync - no swap needed!

            // Apply minMaskThreshold AFTER blurring is complete
            var finalResult = new List<MaskedVertex>();
            for (int i = 0; i < bodyVerts.Length; i++)
            {
                float blurredMasking = maskingMap[i];

                // Only now apply the threshold after all smoothing is done
                if (blurredMasking >= minMaskThreshold)
                {
                    finalResult.Add(new MaskedVertex
                    {
                        vertexIndex = i,
                        masking = Mathf.Clamp01(blurredMasking)
                    });
                }
            }

            // Cleanup
            nativeBodyVerts.Dispose();
            nativeBodyNormals.Dispose();
            clothingTriCentroids.Dispose();
            clothingTriNormals.Dispose();
            clothingTriAreas.Dispose();
            maskingResults.Dispose();
            maskingMap.Dispose();
            blurredMaskingMap.Dispose();
            nativeNeighborOffsets.Dispose();
            nativeNeighborData.Dispose();
            nativeWeldedGroupOffsets.Dispose();
            nativeWeldedGroupData.Dispose();
            nativeVertexToGroupIndex.Dispose();

            return finalResult.ToArray();
        }

        // Directional blur job: allows low masking to bleed into high masking, not vice versa
        [BurstCompile]
        private struct DirectionalBlurJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<float> inputMasking;
            [ReadOnly] public NativeArray<int> neighborOffsets;
            [ReadOnly] public NativeArray<int> neighborData;
            [ReadOnly] public float blurStrength;

            [WriteOnly] public NativeArray<float> outputMasking;

            public void Execute(int v)
            {
                float currentMasking = inputMasking[v];

                // Get neighbors
                int start = neighborOffsets[v];
                int end = v < neighborOffsets.Length - 1 ? neighborOffsets[v + 1] : neighborData.Length;

                if (start >= end)
                {
                    outputMasking[v] = currentMasking; 
                    return;
                }

                // Collect neighbor masking values
                float neighborSum = 0f;
                int neighborCount = 0;
                float minNeighborMasking = currentMasking;

                for (int i = start; i < end; i++)
                {
                    int neighborIdx = neighborData[i];
                    float neighborMasking = inputMasking[neighborIdx];
                    neighborSum += neighborMasking;
                    neighborCount++;
                    minNeighborMasking = math.min(minNeighborMasking, neighborMasking);
                }

                if (neighborCount == 0)
                {
                    outputMasking[v] = currentMasking;
                    return;
                }

                float avgNeighborMasking = neighborSum / neighborCount;

                // DIRECTIONAL BLUR: Allow unmasked to reduce masked, prevent masked from increasing unmasked
                float blurred;

                if (currentMasking < 0.001f)
                {
                    // Current vertex is unmasked (zero) - do NOT let masked neighbors bleed in
                    blurred = currentMasking;
                }
                else if (avgNeighborMasking < currentMasking)
                {
                    // Neighbors are less masked - create strong falloff
                    blurred = math.lerp(currentMasking, avgNeighborMasking, blurStrength);
                }
                else
                {
                    // Neighbors are more masked - weak smoothing only
                    blurred = math.lerp(currentMasking, avgNeighborMasking, blurStrength * 0.1f);
                }

                outputMasking[v] = blurred;
            }
        }

        // Job to synchronize masking values across welded vertices
        [BurstCompile]
        private struct SyncWeldedVerticesJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<float> inputMasking;
            [WriteOnly] public NativeArray<float> outputMasking;
            [ReadOnly] public NativeArray<int> weldedGroupOffsets;
            [ReadOnly] public NativeArray<int> weldedGroupData;
            [ReadOnly] public NativeArray<int> vertexToGroupIndex;

            public void Execute(int vertexIndex)
            {
                int groupIndex = vertexToGroupIndex[vertexIndex];

                // If vertex is not part of any welded group, just copy the value
                if (groupIndex < 0)
                {
                    outputMasking[vertexIndex] = inputMasking[vertexIndex];
                    return;
                }

                int start = weldedGroupOffsets[groupIndex];
                int end = weldedGroupOffsets[groupIndex + 1];

                // Calculate average masking across all vertices in this welded group
                float sum = 0f;
                int count = end - start;

                for (int i = start; i < end; i++)
                {
                    int weldedVertexIndex = weldedGroupData[i];
                    sum += inputMasking[weldedVertexIndex];
                }

                float avgMasking = sum / count;

                // Write the average to THIS vertex only (no race condition)
                outputMasking[vertexIndex] = avgMasking;
            }
        }

        [BurstCompile(FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Standard, CompileSynchronously = true)]
        public struct CombinedBlurAndSyncJob : IJob
        {
            // Pass 3 arrays to swap cleanly without race conditions
            public NativeArray<float> maskingMap;         // Buffer A
            public NativeArray<float> blurredMaskingMap;  // Buffer B
            public NativeArray<float> syncMaskingMap;     // Buffer C

            [ReadOnly] public NativeArray<int> neighborOffsets;
            [ReadOnly] public NativeArray<int> neighborData;
            [ReadOnly] public NativeArray<int> weldedGroupOffsets;
            [ReadOnly] public NativeArray<int> weldedGroupData;
            [ReadOnly] public NativeArray<int> vertexToGroupIndex;

            public int totalBlurPasses;
            public float blurStrength;
            public int totalVertices;

            public void Execute()
            {
                // Treat 'maskingMap' as our active source buffer at the start
                // We use boolean pointers to determine which buffer is source vs destination
                bool readFromBufferA = true;

                for (int pass = 0; pass < totalBlurPasses; pass++)
                {
                    // Pick our active buffers for this specific pass
                    NativeArray<float> currentSource = readFromBufferA ? maskingMap : syncMaskingMap;
                    NativeArray<float> currentBlurDest = blurredMaskingMap;
                    NativeArray<float> currentSyncDest = readFromBufferA ? syncMaskingMap : maskingMap;

                    // STEP 1: BLUR PASS (Loop through every vertex)
                    for (int v = 0; v < totalVertices; v++)
                    {
                        float currentMasking = currentSource[v];
                        int start = neighborOffsets[v];
                        int end = v < neighborOffsets.Length - 1 ? neighborOffsets[v + 1] : neighborData.Length;

                        if (start >= end)
                        {
                            currentBlurDest[v] = currentMasking;
                            continue;
                        }

                        float neighborSum = 0f;
                        int neighborCount = 0;

                        for (int i = start; i < end; i++)
                        {
                            int neighborIdx = neighborData[i];
                            neighborSum += currentSource[neighborIdx]; // Read safely from immutable source
                            neighborCount++;
                        }

                        if (neighborCount == 0)
                        {
                            currentBlurDest[v] = currentMasking;
                            continue;
                        }

                        float avgNeighborMasking = neighborSum / neighborCount;
                        float blurred;

                        if (currentMasking < 0.001f)
                        {
                            blurred = currentMasking;
                        }
                        else if (avgNeighborMasking < currentMasking)
                        {
                            blurred = math.lerp(currentMasking, avgNeighborMasking, blurStrength);
                        }
                        else
                        {
                            blurred = math.lerp(currentMasking, avgNeighborMasking, blurStrength * 0.1f);
                        }

                        currentBlurDest[v] = blurred; // Write cleanly to scratchpad buffer
                    }

                    // STEP 2: SYNC PASS (Loop through every vertex)
                    for (int v = 0; v < totalVertices; v++)
                    {
                        int groupIndex = vertexToGroupIndex[v];

                        if (groupIndex < 0)
                        {
                            currentSyncDest[v] = currentBlurDest[v];
                            continue;
                        }

                        int start = weldedGroupOffsets[groupIndex];
                        int end = weldedGroupOffsets[groupIndex + 1];

                        float sum = 0f;
                        int count = end - start;

                        for (int i = start; i < end; i++)
                        {
                            int weldedVertexIndex = weldedGroupData[i];
                            sum += currentBlurDest[weldedVertexIndex]; // Read from completed blur pass data
                        }

                        currentSyncDest[v] = sum / count; // Write back to the source for the next pass
                    }

                    // Ping-pong: the destination sync map becomes the next pass's source map
                    readFromBufferA = !readFromBufferA;
                }

                // Final Safety Check: If we ended on an odd pass, copy results back to maskingMap
                if (!readFromBufferA)
                {
                    maskingMap.CopyFrom(syncMaskingMap);
                }
            }
        }

        /// <summary>
        /// Generate masked vertices for body vertices that are obscured by the clothing mesh.
        /// Uses a multi-angle ray-casting approach with bounces to determine if a vertex is visible from any angle.
        /// A vertex is masked if it's deeply covered by clothing and not easily visible from multiple viewing angles.
        /// JOBIFIED VERSION - Uses Unity Jobs System for parallel processing.
        /// </summary>
        /// <param name="bodyMesh">The body mesh</param>
        /// <param name="clothingMesh">The clothing mesh</param>
        /// <param name="clothingToBodyRot">Rotation to align clothing to body space</param>
        /// <param name="viewAngleCount">Number of viewing angles to test (default 64 - more angles = more aggressive)</param>
        /// <param name="maxPrimaryDistance">Maximum distance for primary ray cast (default 0.15 - tighter range)</param>
        /// <param name="bounceCount">Number of ray bounces to test indirect visibility (default 1 - less forgiving)</param>
        /// <param name="bounceDistance">Distance for each bounce ray (default 0.1 - shorter escape distance)</param>
        /// <param name="selfOcclusionDistance">Distance to check for body self-occlusion (default 0.02)</param>
        /// <param name="minMaskThreshold">Minimum masking value to include vertex in result (default 0.15 - lower threshold)</param>
        /// <returns>Array of masked vertices with masking values</returns>
        public static MaskedVertex[] GenerateCoveredVertexMask(
            Mesh bodyMesh, 
            Mesh clothingMesh, 
            Quaternion clothingToBodyRot, 
            int viewAngleCount = 64, 
            float maxPrimaryDistance = 0.15f, 
            int bounceCount = 1,
            float bounceDistance = 0.1f,
            float selfOcclusionDistance = 0.02f,
            float minMaskThreshold = 0.15f)
        {
            if (bodyMesh == null || clothingMesh == null)
                throw new ArgumentNullException(bodyMesh == null ? nameof(bodyMesh) : nameof(clothingMesh));  

            var bodyVerts = bodyMesh.vertices;
            var bodyNormals = bodyMesh.normals;
            var bodyTris = bodyMesh.triangles;
            var clothingVerts = clothingMesh.vertices;
            var clothingTris = clothingMesh.triangles;
            int clothingTriCount = clothingTris.Length / 3;
            int bodyTriCount = bodyTris.Length / 3;

            bool hasBodyNormals = bodyNormals != null && bodyNormals.Length == bodyVerts.Length;

            // Transform clothing vertices to body space
            Vector3[] transformedClothingVerts = new Vector3[clothingVerts.Length]; 
            for (int i = 0; i < clothingVerts.Length; i++)
            {
                transformedClothingVerts[i] = clothingToBodyRot * clothingVerts[i];
            }

            // Pre-compute clothing triangle data
            NativeArray<float3> clothingTriA = new NativeArray<float3>(clothingTriCount, Allocator.TempJob);
            NativeArray<float3> clothingTriB = new NativeArray<float3>(clothingTriCount, Allocator.TempJob);
            NativeArray<float3> clothingTriC = new NativeArray<float3>(clothingTriCount, Allocator.TempJob);
            NativeArray<float3> clothingTriNormals = new NativeArray<float3>(clothingTriCount, Allocator.TempJob);

            for (int t = 0; t < clothingTriCount; t++)
            {
                int idxA = clothingTris[t * 3 + 0];
                int idxB = clothingTris[t * 3 + 1];
                int idxC = clothingTris[t * 3 + 2];

                clothingTriA[t] = transformedClothingVerts[idxA];
                clothingTriB[t] = transformedClothingVerts[idxB];
                clothingTriC[t] = transformedClothingVerts[idxC];

                float3 edge1 = clothingTriB[t] - clothingTriA[t];
                float3 edge2 = clothingTriC[t] - clothingTriA[t];
                clothingTriNormals[t] = math.normalize(math.cross(edge1, edge2));
            }

            // Pre-compute body triangle data
            NativeArray<float3> bodyTriA = new NativeArray<float3>(bodyTriCount, Allocator.TempJob);
            NativeArray<float3> bodyTriB = new NativeArray<float3>(bodyTriCount, Allocator.TempJob);
            NativeArray<float3> bodyTriC = new NativeArray<float3>(bodyTriCount, Allocator.TempJob);
            NativeArray<float3> bodyTriNormals = new NativeArray<float3>(bodyTriCount, Allocator.TempJob);

            for (int t = 0; t < bodyTriCount; t++)
            {
                int idxA = bodyTris[t * 3 + 0];
                int idxB = bodyTris[t * 3 + 1];
                int idxC = bodyTris[t * 3 + 2];

                bodyTriA[t] = bodyVerts[idxA];
                bodyTriB[t] = bodyVerts[idxB];
                bodyTriC[t] = bodyVerts[idxC];

                float3 edge1 = bodyTriB[t] - bodyTriA[t];
                float3 edge2 = bodyTriC[t] - bodyTriA[t];
                bodyTriNormals[t] = math.normalize(math.cross(edge1, edge2));
            }

            // Generate viewing directions
            Vector3[] viewDirs = GenerateSphereSamples(viewAngleCount);
            NativeArray<float3> viewDirections = new NativeArray<float3>(viewDirs.Length, Allocator.TempJob);
            for (int i = 0; i < viewDirs.Length; i++)
            {
                viewDirections[i] = viewDirs[i];
            }

            // Generate hemisphere samples for bounces
            Vector3[] hemiSamples = GenerateHemisphereSamples(8);
            NativeArray<float3> hemisphereSamples = new NativeArray<float3>(hemiSamples.Length, Allocator.TempJob);
            for (int i = 0; i < hemiSamples.Length; i++)
            {
                hemisphereSamples[i] = hemiSamples[i];
            }

            // Prepare body vertex data
            NativeArray<float3> nativeBodyVerts = new NativeArray<float3>(bodyVerts.Length, Allocator.TempJob);
            NativeArray<float3> nativeBodyNormals = new NativeArray<float3>(bodyVerts.Length, Allocator.TempJob);
            for (int i = 0; i < bodyVerts.Length; i++)
            {
                nativeBodyVerts[i] = bodyVerts[i];
                nativeBodyNormals[i] = hasBodyNormals ? (float3)bodyNormals[i] : new float3(0, 1, 0);
            }

            NativeArray<MaskedVertex> results = new NativeArray<MaskedVertex>(bodyVerts.Length, Allocator.TempJob);
            NativeArray<int> validFlags = new NativeArray<int>(bodyVerts.Length, Allocator.TempJob);

            // Schedule job
            var job = new CoveredVertexMaskJob
            {
                bodyVerts = nativeBodyVerts,
                bodyNormals = nativeBodyNormals,
                clothingTriA = clothingTriA,
                clothingTriB = clothingTriB,
                clothingTriC = clothingTriC,
                clothingTriNormals = clothingTriNormals,
                bodyTriA = bodyTriA,
                bodyTriB = bodyTriB,
                bodyTriC = bodyTriC,
                bodyTriNormals = bodyTriNormals,
                viewDirections = viewDirections,
                hemisphereSamples = hemisphereSamples,
                maxPrimaryDistance = maxPrimaryDistance,
                bounceCount = bounceCount,
                bounceDistance = bounceDistance,
                selfOcclusionDistance = selfOcclusionDistance,
                minMaskThreshold = minMaskThreshold,
                results = results,
                validFlags = validFlags
            };

            JobHandle handle = job.Schedule(bodyVerts.Length, 32);
            handle.Complete();

            // Collect valid results
            var finalResult = new List<MaskedVertex>();
            for (int i = 0; i < bodyVerts.Length; i++)
            {
                if (validFlags[i] == 1)
                {
                    finalResult.Add(results[i]);

                    // Debug visualization
                    var mv = results[i];
                    Vector3 vertPos = bodyVerts[mv.vertexIndex];
                    Vector3 vertNormal = hasBodyNormals ? bodyNormals[mv.vertexIndex] : Vector3.up; 
                    Debug.DrawRay(vertPos, vertNormal * 0.01f, Color.Lerp(Color.green, Color.red, mv.masking), 200f); 
                }
            }

            // Cleanup
            nativeBodyVerts.Dispose();
            nativeBodyNormals.Dispose();
            clothingTriA.Dispose();
            clothingTriB.Dispose();
            clothingTriC.Dispose();
            clothingTriNormals.Dispose();
            bodyTriA.Dispose();
            bodyTriB.Dispose();
            bodyTriC.Dispose();
            bodyTriNormals.Dispose();
            viewDirections.Dispose();
            hemisphereSamples.Dispose();
            results.Dispose();
            validFlags.Dispose();

            return finalResult.ToArray();
        }

        /// <summary>
        /// Generate evenly distributed sample directions on a full sphere using Fibonacci sphere
        /// </summary>
        private static Vector3[] GenerateSphereSamples(int count)
        {
            Vector3[] samples = new Vector3[count];
            float goldenRatio = (1f + Mathf.Sqrt(5f)) / 2f;

            for (int i = 0; i < count; i++)
            {
                float theta = 2f * Mathf.PI * i / goldenRatio;
                float phi = Mathf.Acos(1f - 2f * (i + 0.5f) / count);

                float x = Mathf.Cos(theta) * Mathf.Sin(phi);
                float y = Mathf.Sin(theta) * Mathf.Sin(phi);
                float z = Mathf.Cos(phi);

                samples[i] = new Vector3(x, y, z);
            }

            return samples;
        }

        /// <summary>
        /// Generate evenly distributed sample directions on a hemisphere using Fibonacci sphere
        /// </summary>
        private static Vector3[] GenerateHemisphereSamples(int count)
        {
            Vector3[] samples = new Vector3[count];
            float goldenRatio = (1f + Mathf.Sqrt(5f)) / 2f;

            for (int i = 0; i < count; i++)
            {
                float theta = 2f * Mathf.PI * i / goldenRatio;
                float phi = Mathf.Acos(1f - 2f * (i + 0.5f) / count);

                // Only upper hemisphere (z >= 0 in local space)
                phi = phi * 0.5f; // Constrain to hemisphere

                float x = Mathf.Cos(theta) * Mathf.Sin(phi);
                float y = Mathf.Sin(theta) * Mathf.Sin(phi);
                float z = Mathf.Cos(phi);

                samples[i] = new Vector3(x, y, z);
            }

            return samples;
        }

        /// <summary>
        /// Orient a direction vector from Z-up space to align with a target normal
        /// </summary>
        private static Vector3 OrientToNormal(Vector3 direction, Vector3 normal)
        {
            // Build orthonormal basis from normal
            Vector3 up = Mathf.Abs(normal.y) < 0.999f ? Vector3.up : Vector3.forward;
            Vector3 tangent = Vector3.Cross(up, normal).normalized;
            Vector3 bitangent = Vector3.Cross(normal, tangent);

            // Transform direction to normal-oriented space
            return tangent * direction.x + bitangent * direction.y + normal * direction.z;  
        }

        /// <summary>
        /// Ray-triangle intersection test using Möller–Trumbore algorithm
        /// </summary>
        private static bool RayTriangleIntersect(Vector3 rayOrigin, Vector3 rayDir, Vector3 v0, Vector3 v1, Vector3 v2, Vector3 triNormal, out float hitDistance)
        {
            hitDistance = 0f;

            // Check if ray is parallel to triangle
            float ndotRay = Vector3.Dot(triNormal, rayDir);
            if (Mathf.Abs(ndotRay) < 1e-6f)
                return false;

            // Möller–Trumbore intersection
            Vector3 edge1 = v1 - v0;
            Vector3 edge2 = v2 - v0;
            Vector3 h = Vector3.Cross(rayDir, edge2);
            float a = Vector3.Dot(edge1, h);

            if (Mathf.Abs(a) < 1e-6f)
                return false;

            float f = 1f / a;
            Vector3 s = rayOrigin - v0;
            float u = f * Vector3.Dot(s, h);

            if (u < 0f || u > 1f)
                return false;

            Vector3 q = Vector3.Cross(s, edge1);
            float v = f * Vector3.Dot(rayDir, q);

            if (v < 0f || u + v > 1f)
                return false;

            float t = f * Vector3.Dot(edge2, q);
            hitDistance = t;

            return t > 1e-6f; // Ray hits triangle
        }
    }
}
