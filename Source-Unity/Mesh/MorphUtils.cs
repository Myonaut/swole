#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using UnityEngine;

using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

using Swole.DataStructures;

namespace Swole.Morphing
{

    public static class MorphUtils
    {

        #region Global Lists

        private static readonly List<BlendShapeTarget> tempBlendShapeTargets = new List<BlendShapeTarget>();
        private static readonly List<VertexGroup> tempVertexGroups = new List<VertexGroup>();

        #endregion

        #region Sub Types

        public struct ContainingTri
        {
            public int closestDependency;

            public int indexA;
            public int indexB;
            public int indexC;

            public float weightA;
            public float weightB;
            public float weightC;

            public ContainingTri(int closestDependency, int indexA, int indexB, int indexC, float weightA, float weightB, float weightC)
            {
                this.closestDependency = closestDependency;

                this.indexA = indexA;
                this.indexB = indexB;
                this.indexC = indexC;

                this.weightA = weightA;
                this.weightB = weightB;
                this.weightC = weightC;
            }
        }

        #endregion

        public static int CompareCompositeVertexGroupBuildersByWeight(CompositeVertexGroupBuilder groupA, CompositeVertexGroupBuilder groupB)
        {
            return (int)Mathf.Sign(groupA.weight - groupB.weight);
        }
        public static int CompareBlendShapeTargetsByWeight(BlendShapeTarget shapeA, BlendShapeTarget shapeB)
        {
            return (int)Mathf.Sign(shapeA.weight - shapeB.weight);
        }

        public static List<VertexGroup> CreateCompositeVertexGroups(ICollection<CompositeVertexGroupBuilder> compositeVertexGroups, ICollection<VertexGroup> existingVertexGroups, List<VertexGroup> outputList = null, float defaultNormalizeMaxWeight = 0f)
        {
            if (outputList == null) outputList = new List<VertexGroup>();

            if (compositeVertexGroups != null && existingVertexGroups != null)
            {
                List<CompositeVertexGroupBuilder> compGroups = new List<CompositeVertexGroupBuilder>(compositeVertexGroups);
                compGroups.Sort(CompareCompositeVertexGroupBuildersByWeight);

                foreach (var group in compGroups)
                {
                    var vg = new VertexGroup(group.name);

                    if (group.addGroups != null)
                    {
                        foreach (var groupName in group.addGroups)
                        {
                            foreach (var vg2 in existingVertexGroups)
                            {
                                if (vg2.name == groupName)
                                {
                                    vg.Add(vg2);
                                    break;
                                }
                            }
                        }
                    }
                    if (group.subtractGroups != null)
                    {
                        foreach (var groupName in group.subtractGroups)
                        {
                            foreach (var vg2 in existingVertexGroups)
                            {
                                if (vg2.name == groupName)
                                {
                                    vg.Subtract(vg2, true, 0);
                                    break;
                                }
                            }
                        }
                    }

                    if (group.normalize) vg.Normalize(group.overrideNormalizeMaxWeight ? group.normalizeMaxWeight : defaultNormalizeMaxWeight);

                    outputList.Add(vg);
                }
            }

            return outputList;
        }

        #region Vertex Group Extraction
        public static List<VertexGroup> ExtractVertexGroups(Mesh mesh, IEnumerable<BlendShapeTarget> targetShapes, List<VertexGroup> outputList = null, string queryId = null, bool normalize = true, float weightThreshold = 0.00001f, float normalizationSetMaxWeight = 0, bool clampWeights = true, Vector3[] vertices = null, Vector3[] normals = null, Vector4[] tangents = null, MeshDataTools.WeldedVertex[] mergedVertices = null)
        {
            if (outputList == null) outputList = new List<VertexGroup>();

            if (mesh != null && targetShapes != null)
            {
                tempBlendShapeTargets.Clear();
                tempBlendShapeTargets.AddRange(targetShapes);
                tempBlendShapeTargets.Sort(CompareBlendShapeTargetsByWeight);
                for (int b = 0; b < tempBlendShapeTargets.Count; b++)
                {
                    var shapeTarget = tempBlendShapeTargets[b];

                    BlendShape shape;
                    if (!shapeTarget.TryGetBlendShape(queryId, mesh, out shape, vertices, normals, tangents, mergedVertices))
                    {
                        shape = new BlendShape(shapeTarget.targetName); // force create an empty shape if it wasnt in the mesh
                        Debug.LogWarning($"Target shape '{shapeTarget.targetName}' was not found. Created an empty replacement.");
                    }
                    if (shape.frames == null || shape.frames.Length <= 0) // if shape has no frames, create an empty one
                    {
                        var frame = new BlendShape.Frame(shape, 0, 0, new Vector3[mesh.vertexCount], new Vector3[mesh.vertexCount], new Vector3[mesh.vertexCount]);
                        shape.frames = new BlendShape.Frame[] { frame };

                        Debug.LogWarning($"Target shape '{shape.name}' had no frames. Created an empty one.");
                    }

                    bool normalize_ = normalize;
                    float normalizationSetMaxWeight_ = normalizationSetMaxWeight;
                    if (shapeTarget.dontNormalize) normalize_ = false;
                    if (shapeTarget.overrideNormalizeMaxWeight) normalizationSetMaxWeight_ = shapeTarget.normalizeMaxWeight;

                    var vg = VertexGroup.ConvertToVertexGroup(shape, normalize, null, weightThreshold, normalizationSetMaxWeight, clampWeights);
                    vg.name = string.IsNullOrWhiteSpace(shapeTarget.newName) ? shapeTarget.targetName : shapeTarget.newName;
                    vg.flag = shapeTarget.animatable;
                    outputList.Add(vg);
                }
            }

            return outputList;
        }
        public static List<VertexGroup> ExtractVertexGroupsAsPool(out Vector2Int indexRange, Mesh mesh, IEnumerable<BlendShapeTarget> targetShapes, List<VertexGroup> outputList = null, string queryId = null, bool normalizeAsPool = true, bool averageAsPool = true, bool normalizeEachGroupFirst = false, float weightThreshold = 0.00001f, float normalizationSetMaxWeight = 0, bool clampWeights = false, Vector3[] vertices = null, Vector3[] normals = null, Vector4[] tangents = null, MeshDataTools.WeldedVertex[] mergedVertices = null)
        {
            indexRange = new Vector2Int(-1, -1);

            indexRange.x = outputList.Count;

            outputList = ExtractVertexGroups(mesh, targetShapes, outputList, queryId, normalizeEachGroupFirst, weightThreshold, normalizationSetMaxWeight, clampWeights, vertices, normals, tangents, mergedVertices);

            indexRange.y = outputList.Count - 1;

            FinalizeVertexGroupsAsPool(outputList, averageAsPool, normalizeAsPool, indexRange);

            return outputList;
        }

        public static void FinalizeVertexGroupsAsPool(IList<VertexGroup> vertexGroups, bool averageAsPool, bool normalizeAsPool, int startIndex = 0, int count = -1, float maxWeightAverage = 1f, float maxWeightNormalize = 0f)
        {
            if (vertexGroups == null || vertexGroups.Count <= 0) return;

            if (count < 0) count = vertexGroups.Count - startIndex;

            FinalizeVertexGroupsAsPool(vertexGroups, averageAsPool, normalizeAsPool, new Vector2Int(startIndex, startIndex + count - 1), maxWeightAverage, maxWeightNormalize);
        }
        public static void FinalizeVertexGroupsAsPool(IList<VertexGroup> vertexGroups, bool averageAsPool, bool normalizeAsPool, Vector2Int indexRange, float maxWeightAverage = 1f, float maxWeightNormalize = 0f)
        {
            if (vertexGroups == null || vertexGroups.Count <= 0 || indexRange.y < indexRange.x) return;
            indexRange.x = Mathf.Clamp(indexRange.x, 0, vertexGroups.Count - 1);
            indexRange.y = Mathf.Clamp(indexRange.y, indexRange.x, vertexGroups.Count - 1); 

            if (averageAsPool)
            {
                tempVertexGroups.Clear();
                for (int a = indexRange.x; a <= indexRange.y; a++) tempVertexGroups.Add(vertexGroups[a]);

                MeshDataTools.AverageVertexGroupWeightsPerVertex(tempVertexGroups, normalizeAsPool, 0.00001f, maxWeightAverage, maxWeightNormalize);
                tempVertexGroups.Clear();
            }
            else if (normalizeAsPool)
            {
                tempVertexGroups.Clear();
                for (int a = indexRange.x; a <= indexRange.y; a++) tempVertexGroups.Add(vertexGroups[a]);

                MeshDataTools.NormalizeVertexGroupsAsPool(tempVertexGroups, maxWeightNormalize);
                tempVertexGroups.Clear();
            }
        }
        #endregion

        #region Seam Merging

        public static Mesh MergeMeshSeam(Mesh mesh, Mesh baseMesh, string seamShapeName, SeamMergeMethod mergeMethod, out BlendShape seamShape, bool instantiateMesh = true, bool removeSeamShape = true, bool mergeTangents = true, bool mergeUVs = false, List<string> nonSeamMergableBlendShapes = null)
        {
            if (instantiateMesh)
            {
                string meshName = mesh.name;
                mesh = MeshUtils.DuplicateMesh(mesh);
                mesh.name = meshName;
            }
            List<BlendShape> shapes = mesh.GetBlendShapes();

            seamShape = null;
            foreach (var shape in shapes) if (shape.name == seamShapeName)
                {
                    seamShape = shape;
                    break;
                }

            if (seamShape != null && seamShape.frames != null && seamShape.frames.Length > 0)
            {
                if (removeSeamShape) shapes.Remove(seamShape);

                List<BlendShape> baseShapes = baseMesh.GetBlendShapes();
                var baseVertices = baseMesh.vertices;
                var baseNormals = baseMesh.normals;
                var baseTangents = baseMesh.tangents;
                var baseUVs = baseMesh.uv;
                var baseUVs2 = baseMesh.uv2;
                if (baseUVs2 != null && baseUVs2.Length <= 0) baseUVs2 = null;
                var baseColors = baseMesh.colors;

                var baseWeld = mergeMethod == SeamMergeMethod.WeldData ? MeshDataTools.WeldVertices(baseVertices) : null;

                var vertices = mesh.vertices;
                var normals = mesh.normals;
                var tangents = mesh.tangents;
                var uvs = mesh.uv;
                var uvs2 = mesh.uv2;
                if (uvs2 != null && uvs2.Length <= 0) uvs2 = null;
                var colors = mesh.colors;

                byte maxBonesPerVertex = 0;
                using (var baseBoneWeights = baseMesh.GetAllBoneWeights())
                {
                    using (var baseBonesPerVertex = baseMesh.GetBonesPerVertex())
                    {
                        int[] baseBoneWeightStartIndices = new int[baseBonesPerVertex.Length];
                        int boneWeightIndex = 0;
                        for (int a = 0; a < baseBoneWeightStartIndices.Length; a++)
                        {
                            baseBoneWeightStartIndices[a] = boneWeightIndex;
                            boneWeightIndex += baseBonesPerVertex[a];
                        }

                        using (var boneWeights = mesh.GetAllBoneWeights())
                        {
                            using (var bonesPerVertex = mesh.GetBonesPerVertex())
                            {
                                for (int a = 0; a < bonesPerVertex.Length; a++) maxBonesPerVertex = (byte)Mathf.Max(bonesPerVertex[a], maxBonesPerVertex);

                                using (NativeList<BoneWeight1> newBoneWeights = new NativeList<BoneWeight1>(mesh.vertexCount, Allocator.Persistent))
                                {
                                    using (NativeArray<byte> newBonesPerVertex = new NativeArray<byte>(bonesPerVertex.Length, Allocator.Persistent))
                                    {
                                        var editNewBonesPerVertex = newBonesPerVertex;
                                        var editNewBoneWeights = newBoneWeights;

                                        boneWeightIndex = 0;
                                        for (int a = 0; a < vertices.Length; a++)
                                        {
                                            byte bpv = bonesPerVertex[a];
                                            editNewBonesPerVertex[a] = bpv;
                                            if (seamShape.frames[0].deltaVertices[a].sqrMagnitude <= 0.0001f) // not a seam vertex
                                            {
                                                for (int b = 0; b < bpv; b++) newBoneWeights.Add(boneWeights[boneWeightIndex + b]);
                                                boneWeightIndex += bpv;

                                                continue;
                                            }


                                            Vector3 vertex = vertices[a];
                                            Vector2 localUV = uvs[a];
                                            float minDist = float.MaxValue;
                                            int closestVertex = -1;
                                            for (int b = 0; b < baseVertices.Length; b++)
                                            {
                                                float dist = (vertex - baseVertices[b]).sqrMagnitude;
                                                if (mergeMethod == SeamMergeMethod.ClosestUV) dist = dist + (localUV - baseUVs[b]).sqrMagnitude;
                                                if (dist < minDist)
                                                {
                                                    minDist = dist;
                                                    closestVertex = b;
                                                }
                                            }

                                            if (closestVertex < 0)
                                            {
                                                for (int b = 0; b < bpv; b++) newBoneWeights.Add(boneWeights[boneWeightIndex + b]);
                                                boneWeightIndex += bpv;

                                                continue;
                                            }

                                            boneWeightIndex += bpv;

                                            int indexStart = newBoneWeights.Length;
                                            bpv = baseBonesPerVertex[closestVertex];
                                            int baseBoneWeightIndex = baseBoneWeightStartIndices[closestVertex];
                                            for (int b = 0; b < Mathf.Min(maxBonesPerVertex, bpv); b++) newBoneWeights.Add(baseBoneWeights[baseBoneWeightIndex + b]);
                                            if (bpv > maxBonesPerVertex)
                                            {
                                                bpv = maxBonesPerVertex;
                                                float totalWeight = 0;
                                                for (int b = 0; b < bpv; b++) totalWeight = totalWeight + newBoneWeights[b + indexStart].weight;
                                                if (totalWeight > 0)
                                                {
                                                    for (int b = 0; b < bpv; b++) editNewBoneWeights[b + indexStart] = new BoneWeight1() { boneIndex = newBoneWeights[b + indexStart].boneIndex, weight = newBoneWeights[b + indexStart].weight / totalWeight };
                                                }
                                            }
                                            editNewBonesPerVertex[a] = bpv;

                                            Vector3 mergedNormal = baseNormals[closestVertex];
                                            Vector3 mergedTangent = baseTangents[closestVertex];
                                            Vector2 mergedUV = baseUVs == null || closestVertex >= baseUVs.Length ? Vector2.zero : baseUVs[closestVertex];
                                            Vector2 mergedUV2 = baseUVs2 == null || closestVertex >= baseUVs2.Length ? Vector2.zero : baseUVs2[closestVertex];
                                            Color mergedColor = baseColors == null || closestVertex >= baseColors.Length ? Color.clear : baseColors[closestVertex];
                                            if (mergeMethod == SeamMergeMethod.WeldData)
                                            {
                                                mergedNormal = Vector3.zero;
                                                mergedTangent = Vector3.zero;
                                                mergedUV = Vector2.zero;
                                                mergedUV2 = Vector2.zero;
                                                mergedColor = Color.clear;

                                                var weld = baseWeld[closestVertex];
                                                var count = weld.indices.Count;
                                                for (int b = 0; b < count; b++)
                                                {
                                                    int index = weld.indices[b];

                                                    mergedNormal = mergedNormal + baseNormals[index];
                                                    var t = baseTangents[index];
                                                    mergedTangent = mergedTangent + new Vector3(t.x, t.y, t.z);
                                                    mergedUV = mergedUV + (baseUVs == null ? Vector2.zero : baseUVs[index]);
                                                    mergedUV2 = mergedUV2 + (baseUVs2 == null ? Vector2.zero : baseUVs2[index]);
                                                    mergedColor = mergedColor + baseColors[index];
                                                }

                                                mergedNormal = mergedNormal.normalized;
                                                mergedTangent = mergedTangent.normalized;
                                                mergedUV = mergedUV / count;
                                                mergedUV2 = mergedUV2 / count;
                                                mergedColor = mergedColor / count;
                                            }
                                            normals[a] = mergedNormal;
                                            if (mergeTangents)
                                            {
                                                tangents[a] = new Vector4(mergedTangent.x, mergedTangent.y, mergedTangent.z, tangents[a].w);
                                            }
                                            if (mergeUVs)
                                            {
                                                uvs[a] = mergedUV;
                                                if (uvs2 != null && baseUVs2 != null) uvs2[a] = mergedUV2;
                                            }
                                            colors[a] = mergedColor;

                                            foreach (var shape in shapes)
                                            {
                                                if (shape.frames != null)
                                                {
                                                    if (nonSeamMergableBlendShapes != null && nonSeamMergableBlendShapes.Contains(shape.name)) continue;

                                                    foreach (var baseShape in baseShapes)
                                                    {
                                                        if (shape.name != baseShape.name || baseShape.frames == null) continue;

                                                        for (int c = 0; c < Mathf.Min(shape.frames.Length, baseShape.frames.Length); c++)
                                                        {
                                                            var frame = shape.frames[c];
                                                            var baseFrame = baseShape.frames[c];

                                                            var mergedDeltaVertex = baseFrame.deltaVertices[closestVertex];
                                                            var mergedDeltaNormal = baseFrame.deltaNormals[closestVertex];
                                                            var mergedDeltaTangent = baseFrame.deltaTangents[closestVertex];

                                                            if (mergeMethod == SeamMergeMethod.WeldData)
                                                            {
                                                                var weld = baseWeld[closestVertex];
                                                                var count = weld.indices.Count;
                                                                for (int b = 0; b < count; b++)
                                                                {
                                                                    int index = weld.indices[b];

                                                                    mergedDeltaVertex = mergedDeltaVertex + baseFrame.deltaVertices[index];
                                                                    mergedDeltaNormal = mergedDeltaNormal + baseFrame.deltaNormals[index];
                                                                    mergedDeltaTangent = mergedDeltaTangent + baseFrame.deltaTangents[index];
                                                                }

                                                                mergedDeltaVertex = mergedDeltaVertex / count;
                                                                mergedDeltaNormal = mergedDeltaNormal / count;
                                                                mergedDeltaTangent = mergedDeltaTangent / count;
                                                            }

                                                            frame.deltaVertices[a] = mergedDeltaVertex;
                                                            frame.deltaNormals[a] = mergedDeltaNormal;
                                                            if (mergeTangents) frame.deltaTangents[a] = mergedDeltaTangent;
                                                        }
                                                    }
                                                }
                                            }

                                        }

                                        mesh.SetBoneWeights(newBonesPerVertex, newBoneWeights.AsArray());

                                    }
                                }
                            }
                        }
                    }
                }

                mesh.vertices = vertices;
                mesh.normals = normals;
                mesh.tangents = tangents;
                mesh.uv = uvs;
                if (uvs2 != null) mesh.uv2 = uvs2;
                mesh.colors = colors;

                mesh.ClearBlendShapes();
                foreach (var shape in shapes) shape.AddToMesh(mesh);
            }

            return mesh;
        }
        
        public static void MergeVertexGroupsAtSeam(Mesh mesh, Mesh baseMesh, BlendShape seamShape, ICollection<VertexGroup> vertexGroups, ICollection<VertexGroup> baseVertexGroups, List<string> nonSeamMergable = null, Vector3[] baseVertices = null, Vector3[] vertices = null)
        {
            if (seamShape != null && seamShape.frames != null && seamShape.frames.Length > 0)
            {
                if (baseVertices == null) baseVertices = baseMesh.vertices;

                if (vertices == null) vertices = mesh.vertices;

                for (int a = 0; a < vertices.Length; a++)
                {
                    if (seamShape.frames[0].deltaVertices[a].sqrMagnitude <= 0.0001f) continue; // not a seam vertex

                    Vector3 vertex = vertices[a];
                    float minDist = float.MaxValue;
                    int closestVertex = -1;
                    for (int b = 0; b < baseVertices.Length; b++)
                    {
                        float dist = (vertex - baseVertices[b]).sqrMagnitude;
                        if (dist < minDist)
                        {
                            minDist = dist;
                            closestVertex = b;
                        }
                    }

                    if (closestVertex < 0) continue;

                    foreach (var vg in vertexGroups)
                    {
                        if (nonSeamMergable != null && nonSeamMergable.Contains(vg.name)) continue;

                        foreach (var baseVG in baseVertexGroups)
                        {
                            if (vg.name != baseVG.name) continue;

                            float baseWeight = baseVG.GetWeight(closestVertex);
                            if (baseWeight == 0)
                            {
                                vg.RemoveWeight(a);
                            }
                            else
                            {
                                vg.SetWeight(a, baseWeight);
                            }

                            break;
                        }
                    }

                }
            }
        }
        
        public static void MergeMorphsAtSeam(Mesh mesh, Mesh baseMesh, BlendShape seamShape, ICollection<MorphShape> morphShapes, ICollection<MorphShape> baseMorphShapes, List<string> nonSeamMergableShapes = null, Vector3[] baseVertices = null, Vector3[] vertices = null)
        {
            if (seamShape != null && seamShape.frames != null && seamShape.frames.Length > 0)
            {
                if (baseVertices == null) baseVertices = baseMesh.vertices;

                if (vertices == null) vertices = mesh.vertices;

                for (int a = 0; a < vertices.Length; a++)
                {
                    if (seamShape.frames[0].deltaVertices[a].sqrMagnitude <= 0.0001f) continue; // not a seam vertex

                    Vector3 vertex = vertices[a];
                    float minDist = float.MaxValue;
                    int closestVertex = -1;
                    for (int b = 0; b < baseVertices.Length; b++)
                    {
                        float dist = (vertex - baseVertices[b]).sqrMagnitude;
                        if (dist < minDist)
                        {
                            minDist = dist;
                            closestVertex = b;
                        }
                    }

                    if (closestVertex < 0) continue;

                    foreach (var morph in morphShapes)
                    {
                        if (nonSeamMergableShapes != null && nonSeamMergableShapes.Contains(morph.name)) continue;

                        foreach (var baseMorph in baseMorphShapes)
                        {
                            if (morph.name != baseMorph.name) continue;

                            if (morph.DeltaVertices != null && baseMorph.DeltaVertices != null) morph.DeltaVertices[a] = baseMorph.DeltaVertices[closestVertex];
                            if (morph.DeltaNormals != null && baseMorph.DeltaNormals != null) morph.DeltaNormals[a] = baseMorph.DeltaNormals[closestVertex];
                            if (morph.DeltaTangents != null && baseMorph.DeltaTangents != null) morph.DeltaTangents[a] = baseMorph.DeltaTangents[closestVertex];

                            break;
                        }
                    }

                }
            }
        }

        public static void MergeMeshShapesAtSeam(Mesh mesh, Mesh baseMesh, BlendShape seamShape, ICollection<MeshShape> meshShapes, ICollection<MeshShape> baseMeshShapes, List<string> nonSeamMergableShapes = null, Vector3[] baseVertices = null, Vector3[] vertices = null)
        {
            if (seamShape != null && seamShape.frames != null && seamShape.frames.Length > 0)
            {
                if (baseVertices == null) baseVertices = baseMesh.vertices;

                if (vertices == null) vertices = mesh.vertices;

                for (int a = 0; a < vertices.Length; a++)
                {
                    if (seamShape.frames[0].deltaVertices[a].sqrMagnitude <= 0.0001f) continue; // not a seam vertex

                    Vector3 vertex = vertices[a];
                    float minDist = float.MaxValue;
                    int closestVertex = -1;
                    for (int b = 0; b < baseVertices.Length; b++)
                    {
                        float dist = (vertex - baseVertices[b]).sqrMagnitude;
                        if (dist < minDist)
                        {
                            minDist = dist;
                            closestVertex = b;
                        }
                    }

                    if (closestVertex < 0) continue;

                    foreach (var shape in meshShapes)
                    {
                        if (shape.frames == null || (nonSeamMergableShapes != null && nonSeamMergableShapes.Contains(shape.name))) continue;

                        foreach (var baseShape in baseMeshShapes)
                        {
                            if (baseShape.frames == null || shape.name != baseShape.name) continue;

                            int frameCount = Math.Min(shape.frames.Length, baseShape.frames.Length);
                            for (int f = 0; f < frameCount; f++)
                            {
                                var frame = shape.frames[f];
                                var baseFrame = baseShape.frames[f];

                                if (frame.deltas != null && baseFrame.deltas != null) frame.deltas[a] = baseFrame.deltas[closestVertex];
                            }

                            break;
                        }
                    }

                }
            }
        }

        #endregion

        #region Runtime

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MeshVertexDelta SampleDeltaShapeBuffer(this NativeArray<MeshVertexDelta> deltaBuffer, int vertexIndex, NativeArray<float> frameWeightsBuffer, int frameBufferStartIndex, int frameCount, float weight, int vertexCount)
        {
            MeshVertexDelta output = MeshVertexDelta.Default; 

            if (frameCount > 1)
            {
                int frameCountM1 = frameCount - 1;
                for (int frameIndexA = 0; frameIndexA < frameCountM1; frameIndexA++)
                {
                    int frameFullIndexA = frameBufferStartIndex + frameIndexA;
                    float frameWeightA = frameWeightsBuffer[frameFullIndexA];

                    int frameIndexB = frameIndexA + 1;
                    int frameFullIndexB = frameBufferStartIndex + frameIndexB;
                    float frameWeightB = frameWeightsBuffer[frameFullIndexB];

                    float weightRange = frameWeightB - frameWeightA;

                    float weightA = 0f;
                    float weightB = (weight - frameWeightA) / weightRange;
                    if (weightB < 0f)
                    {
                        if (frameIndexA == 0)
                        {
                            if (frameWeightA != 0f)
                            {
                                weightA = weight / frameWeightA;
                                weightB = 0;
                            }
                            else
                            {
                                weightA = 1 + math.abs(weight / weightRange);
                                weightB = 0;
                            }
                        }
                        else
                        {
                            weightA = math.abs(weightB); 
                            weightB = 0;
                        }
                    }
                    else
                    {
                        weightA = 1f - weightB;
                        if (weightA < 0f && frameIndexB < frameCountM1) continue; // move to next frame pair
                        weightA = math.max(0f, weightA);
                    }

                    int shapeStartIndex;
                    shapeStartIndex = (frameFullIndexA * vertexCount); 
                    output = deltaBuffer[shapeStartIndex + vertexIndex] * weightA;
                    shapeStartIndex = (frameFullIndexB * vertexCount);
                    output = output + deltaBuffer[shapeStartIndex + vertexIndex] * weightB;

                    //if (vertexIndex == 8000) Debug.Log($"{weight} ::: {frameFullIndexA} ~ {frameWeightA} ~ {weightA} : {frameFullIndexB} ~ {frameWeightB} ~ {weightB}");

                    //Debug.Log($"{weight} -> ({frameWeightA} <-> {frameWeightB}) Frame {shapeIndexA} {weightA} : {shapeIndexB} {weightB}");

                    break;
                }
            }
            else if (frameCount > 0)
            {
                int shapeStartIndex = frameBufferStartIndex * vertexCount;
                float maxWeight = frameWeightsBuffer[frameBufferStartIndex];
                output = deltaBuffer[shapeStartIndex + vertexIndex] * (weight / maxWeight);
            }

            return output;
        }

        #endregion

    }

    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct MorphShapeVertex
    {
        public float3 deltaVertex;
        public float3 deltaNormal;
        public float3 deltaTangent;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MorphShapeVertex operator +(MorphShapeVertex a, MorphShapeVertex b)
        {
            MorphShapeVertex result = new MorphShapeVertex();
            result.deltaVertex = a.deltaVertex + b.deltaVertex;
            result.deltaNormal = a.deltaNormal + b.deltaNormal;
            result.deltaTangent = a.deltaTangent + b.deltaTangent;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MorphShapeVertex operator -(MorphShapeVertex a, MorphShapeVertex b)
        {
            MorphShapeVertex result = new MorphShapeVertex();
            result.deltaVertex = a.deltaVertex - b.deltaVertex;
            result.deltaNormal = a.deltaNormal - b.deltaNormal;
            result.deltaTangent = a.deltaTangent - b.deltaTangent;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MorphShapeVertex operator *(MorphShapeVertex a, float b)
        {
            MorphShapeVertex result = new MorphShapeVertex();
            result.deltaVertex = a.deltaVertex * b;
            result.deltaNormal = a.deltaNormal * b;
            result.deltaTangent = a.deltaTangent * b;
            return result;
        }

        public static implicit operator MeshVertexDelta(MorphShapeVertex msv)
        {
            var result = MeshVertexDelta.Default;

            result.positionDelta = msv.deltaVertex;
            result.normalDelta = msv.deltaNormal;
            result.tangentDelta = msv.deltaTangent;

            return result;
        }

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MeshVertexDelta
    {
        public float3 positionDelta;
        public float3 normalDelta;
        public float3 tangentDelta;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MeshVertexDelta operator +(MeshVertexDelta v1, MeshVertexDelta v2)
        {
            var result = new MeshVertexDelta();

            result.positionDelta = v1.positionDelta + v2.positionDelta;
            result.normalDelta = v1.normalDelta + v2.normalDelta;
            result.tangentDelta = v1.tangentDelta + v2.tangentDelta;

            return result;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MeshVertexDelta operator -(MeshVertexDelta v1, MeshVertexDelta v2)
        {
            var result = new MeshVertexDelta();

            result.positionDelta = v1.positionDelta - v2.positionDelta;
            result.normalDelta = v1.normalDelta - v2.normalDelta;
            result.tangentDelta = v1.tangentDelta - v2.tangentDelta;

            return result;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MeshVertexDelta operator *(MeshVertexDelta v1, float v)
        {
            var result = new MeshVertexDelta();

            result.positionDelta = v1.positionDelta * v;
            result.normalDelta = v1.normalDelta * v;
            result.tangentDelta = v1.tangentDelta * v;

            return result;
        }

        public static MeshVertexDelta Default => new MeshVertexDelta()
        {
            positionDelta = float3.zero,
            normalDelta = float3.zero,
            tangentDelta = float3.zero
        };
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MeshVertexDeltaLR
    {
        public MeshVertexDelta deltaLeft;
        public MeshVertexDelta deltaRight;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MeshVertexDeltaLR operator +(MeshVertexDeltaLR v1, MeshVertexDeltaLR v2)
        {
            var result = new MeshVertexDeltaLR();

            result.deltaLeft = v1.deltaLeft + v2.deltaLeft;
            result.deltaRight = v1.deltaRight + v2.deltaRight;

            return result;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MeshVertexDeltaLR operator -(MeshVertexDeltaLR v1, MeshVertexDeltaLR v2)
        {
            var result = new MeshVertexDeltaLR();

            result.deltaLeft = v1.deltaLeft - v2.deltaLeft;
            result.deltaRight = v1.deltaRight - v2.deltaRight;

            return result;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MeshVertexDeltaLR operator *(MeshVertexDeltaLR v1, float v)
        {
            var result = new MeshVertexDeltaLR();

            result.deltaLeft = v1.deltaLeft * v;
            result.deltaRight = v1.deltaRight * v;

            return result;
        }

        public static MeshVertexDeltaLR Default => new MeshVertexDeltaLR()
        {
            deltaLeft = MeshVertexDelta.Default,
            deltaRight = MeshVertexDelta.Default
        };
    }

    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct SkinningBlendVertex
    {
        public BoneWeight8 deltaWeightsA;
        public BoneWeight8 deltaWeightsB;
    }

    [Serializable]
    public class MorphShape : IDisposable
    {

        [NonSerialized]
        private bool trackingDisposables;
        public void TrackDisposables()
        {
            if (trackingDisposables) return;

            if (!PersistentJobDataTracker.Track(this))
            {
                Dispose();
                return;
            }

            trackingDisposables = true;
        }

        public void Dispose()
        {
            if (trackingDisposables)
            {
                try
                {
                    PersistentJobDataTracker.Untrack(this);
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR
                    Debug.LogError(ex);
#endif
                }
            }
            trackingDisposables = false;

            try
            {
                if (jobDeltaVertices.IsCreated)
                {
                    jobDeltaVertices.Dispose();
                    jobDeltaVertices = default;
                }
            }
            catch (Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogException(ex);
#endif
            }

            try
            {
                if (jobDeltaNormals.IsCreated)
                {
                    jobDeltaNormals.Dispose();
                    jobDeltaNormals = default;
                }
            }
            catch (Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogException(ex);
#endif
            }

            try
            {
                if (jobDeltaTangents.IsCreated)
                {
                    jobDeltaTangents.Dispose();
                    jobDeltaTangents = default;
                }
            }
            catch (Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogException(ex);
#endif
            }

            try
            {
                if (jobDeltaVertexData.IsCreated)
                {
                    jobDeltaVertexData.Dispose();
                    jobDeltaVertexData = default;
                }
            }
            catch (Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogException(ex);
#endif
            }
        }

        public string name;
        public bool animatable;

        public static MorphShape CreateFromBlendShape(BlendShape shape)
        {
            if (shape == null) return null;

            var morph = new MorphShape();
            morph.name = shape.name;

            if (shape.frames != null && shape.frames.Length > 0)
            {
                var frame = shape.frames[0];
                morph.deltaVertices = frame.deltaVertices.ToArray();
                morph.deltaNormals = frame.deltaNormals.ToArray();
                morph.deltaTangents = frame.deltaTangents.ToArray();
            }

            return morph;
        }

        [SerializeField, HideInInspector]
        protected Vector3[] deltaVertices;
        public Vector3[] DeltaVertices => deltaVertices;

        [SerializeField, HideInInspector]
        protected Vector3[] deltaNormals;
        public Vector3[] DeltaNormals => deltaNormals;

        [SerializeField, HideInInspector]
        protected Vector3[] deltaTangents;
        public Vector3[] DeltaTangents => deltaTangents;

        public void ApplyVertexGroup(VertexGroup vg)
        {
            for (int a = 0; a < deltaVertices.Length; a++)
            {
                float weight = vg.GetWeight(a);
                deltaVertices[a] = deltaVertices[a] * weight;
                if (deltaNormals != null) deltaNormals[a] = deltaNormals[a] * weight;
                if (deltaTangents != null) deltaTangents[a] = deltaTangents[a] * weight;
            }
        }
        public void ApplyVertexGroupAsCookie(VertexGroup vg, float minWeight = 0.00001f)
        {
            for (int a = 0; a < deltaVertices.Length; a++)
            {
                float weight = vg.GetWeight(a) > minWeight ? 1 : 0;
                deltaVertices[a] = deltaVertices[a] * weight;
                if (deltaNormals != null) deltaNormals[a] = deltaNormals[a] * weight;
                if (deltaTangents != null) deltaTangents[a] = deltaTangents[a] * weight;
            }
        }

        [NonSerialized]
        protected NativeArray<float3> jobDeltaVertices;
        [NonSerialized]
        protected NativeArray<float3> jobDeltaNormals;
        [NonSerialized]
        protected NativeArray<float3> jobDeltaTangents;

        [NonSerialized]
        protected NativeArray<MorphShapeVertex> jobDeltaVertexData;

        public NativeArray<float3> JobDeltaVertices
        {
            get
            {
                if (!jobDeltaVertices.IsCreated)
                {
                    jobDeltaVertices = new NativeArray<float3>(deltaVertices == null ? 0 : deltaVertices.Length, Allocator.Persistent);
                    for (int a = 0; a < jobDeltaVertices.Length; a++) jobDeltaVertices[a] = deltaVertices[a];

                    TrackDisposables();
                }

                return jobDeltaVertices;
            }
        }
        public NativeArray<float3> JobDeltaNormals
        {
            get
            {
                if (!jobDeltaNormals.IsCreated)
                {
                    jobDeltaNormals = new NativeArray<float3>(deltaNormals == null ? 0 : deltaNormals.Length, Allocator.Persistent);
                    for (int a = 0; a < jobDeltaNormals.Length; a++) jobDeltaNormals[a] = deltaNormals[a];

                    TrackDisposables();
                }

                return jobDeltaNormals;
            }
        }
        public NativeArray<float3> JobDeltaTangents
        {
            get
            {
                if (!jobDeltaTangents.IsCreated)
                {
                    jobDeltaTangents = new NativeArray<float3>(deltaTangents == null ? 0 : deltaTangents.Length, Allocator.Persistent);
                    for (int a = 0; a < jobDeltaTangents.Length; a++) jobDeltaTangents[a] = deltaTangents[a];

                    TrackDisposables();
                }

                return jobDeltaTangents;
            }
        }

        public NativeArray<MorphShapeVertex> JobDeltaVertexData
        {
            get
            {
                if (!jobDeltaVertexData.IsCreated)
                {
                    jobDeltaVertexData = new NativeArray<MorphShapeVertex>(deltaVertices == null ? (deltaNormals == null ? (deltaTangents == null ? 0 : deltaTangents.Length) : deltaNormals.Length) : deltaVertices.Length, Allocator.Persistent);
                    for (int a = 0; a < jobDeltaVertexData.Length; a++)
                    {
                        var data = new MorphShapeVertex();
                        if (deltaVertices != null) data.deltaVertex = deltaVertices[a];
                        if (deltaNormals != null) data.deltaNormal = deltaNormals[a];
                        if (deltaTangents != null) data.deltaTangent = deltaTangents[a];

                        jobDeltaVertexData[a] = data;
                    }

                    TrackDisposables();
                }

                return jobDeltaVertexData;
            }
        }

    }

    [Serializable]
    public class MeshShape
    {
        public string name;

        public bool animatable;

        public MeshShapeFrame[] frames;
        public int FrameCount => frames == null ? 0 : frames.Length;

        [NonSerialized]
        private float[] frameWeights;
        public float[] FrameWeights
        {
            get
            {
                if (frameWeights == null)
                {
                    if (frames != null)
                    {
                        frameWeights = new float[frames.Length];
                        for(int a = 0; a < frames.Length; a++) frameWeights[a] = frames[a].weight;
                    }
                    else frameWeights = new float[0];
                }

                return frameWeights;
            }
        }

        public static MeshShape CreateFromBlendShape(BlendShape shape)
        {
            if (shape == null) return null;

            var meshShape = new MeshShape();
            meshShape.name = shape.name;

            if (shape.frames != null && shape.frames.Length > 0)
            {
                meshShape.frames = new MeshShapeFrame[shape.frames.Length];

                for (int a = 0; a < shape.frames.Length; a++)
                {
                    var frame = shape.frames[a];
                    var meshFrame = new MeshShapeFrame();
                    meshFrame.weight = frame.weight;
                    meshFrame.deltas = new MorphShapeVertex[frame.deltaVertices.Length];
                    for (int b = 0; b < frame.deltaVertices.Length; b++)
                    {
                        var vertex = new MorphShapeVertex();
                        vertex.deltaVertex = frame.deltaVertices[b];
                        vertex.deltaNormal = frame.deltaNormals[b];
                        vertex.deltaTangent = frame.deltaTangents[b];
                        meshFrame.deltas[b] = vertex;
                    }

                    meshShape.frames[a] = meshFrame;
                }
            }

            return meshShape;
        }

        public static int CompareMeshShapeFramesByWeight(MeshShapeFrame frameA, MeshShapeFrame frameB)
        {
            return (int)Mathf.Sign(frameA.weight - frameB.weight);
        }

        public static MeshShape CreateFromBlendShapes(string name, IEnumerable<BlendShape> shapes, bool interleaveFrames = false)
        {
            var meshShape = new MeshShape();

            float frameWeightOffset = 0f;
            List<MeshShapeFrame> meshFrames = new List<MeshShapeFrame>();
            foreach (var shape in shapes)
            {
                if (shape == null) continue;

                if (string.IsNullOrWhiteSpace(name)) name = shape.name;

                if (shape.frames != null && shape.frames.Length > 0)
                {
                    for (int a = 0; a < shape.frames.Length; a++)
                    {
                        var frame = shape.frames[a];
                        var meshFrame = new MeshShapeFrame();
                        meshFrame.weight = frame.weight + (interleaveFrames ? 0f : frameWeightOffset);
                        meshFrame.deltas = new MorphShapeVertex[frame.deltaVertices.Length];
                        for (int b = 0; b < frame.deltaVertices.Length; b++)
                        {
                            var vertex = new MorphShapeVertex();
                            vertex.deltaVertex = frame.deltaVertices[b];
                            vertex.deltaNormal = frame.deltaNormals[b];
                            vertex.deltaTangent = frame.deltaTangents[b];
                            meshFrame.deltas[b] = vertex;
                        }

                        meshFrames.Add(meshFrame);
                    }

                    if (!interleaveFrames) frameWeightOffset += meshFrames[meshFrames.Count - 1].weight;
                }
            }

            meshFrames.Sort(CompareMeshShapeFramesByWeight);
            int frameIndex = 0;
            while(frameIndex < meshFrames.Count - 1) // remove frames with duplicate weights
            {
                var frameA = meshFrames[frameIndex];
                var frameB = meshFrames[frameIndex + 1];

                if (frameA.weight != frameB.weight)
                {
                    frameIndex++;
                    continue;
                }
                Debug.Log($"Removed duplicate frame at weight {frameB.weight}");
                meshFrames.RemoveAt(frameIndex + 1);
            }

            meshShape.name = name;
            meshShape.frames = meshFrames.ToArray();

            return meshShape;
        }

    }
    [Serializable]
    public struct MeshShapeFrame
    {
        public float weight;

        [HideInInspector]
        public MorphShapeVertex[] deltas;
    }

    [Serializable]
    public class SkinningBlend
    {
        public string name;

        public bool animatable;

        public MeshShapeFrame[] frames;
    }
    [Serializable]
    public struct SkinningBlendFrame
    {
        public float weight;

        [HideInInspector]
        public SkinningBlendVertex[] deltas;
    }
    [Serializable]
    public struct BlendShapeMix
    {
        public string shapeName;
        public float weight;
    }
    [Serializable]
    public struct BlendShapeBase
    {
        public string shapeName;
        public bool recalculateNormals;
        public bool recalculateTangents;
    }
    [Serializable]
    public struct BlendShapeBaseMix
    {
        public string shapeName;
        public bool recalculateNormals;
        public bool recalculateTangents;
        public float weight;
    }
    [Serializable]
    public struct BlendShapeTarget
    {
        public string newName;
        public string targetName;
        public string[] alternateTargetNames;
        [Tooltip("Optional. Makes this shape relative to the base target shapes.")]
        public BlendShapeBase[] baseTargetNames;
        [Tooltip("Optional. Makes this shape relative to the mix of base target shapes.")]
        public BlendShapeBaseMix[] baseTargetMix;
        [Tooltip("Optional. Makes this shape relative to the base morphs.")]
        public string[] baseMorphNames;
        public float weight;
        public bool animatable;

        [Tooltip("Only applies to vertex group shapes.")]
        public bool dontNormalize;
        public bool overrideNormalizeMaxWeight;
        public float normalizeMaxWeight;

        [Tooltip("Ids of meshes or mesh groups that will zero out the normals and tangents for this shape when it's fetched")]
        public string[] idsToZeroOutNormalsAndTangents;

        public bool recalculateNormals;
        public bool recalculateTangents;
        public BlendShapeBaseMix[] recalculateNormalsTangentsShapeMix;
        public bool dontSubtractMixPostRecalculation;

        public bool copyNormals;
        public bool copyTangents;
        public BlendShapeBaseMix[] copyNormalsTangentsShapeMix;

        [Tooltip("Ids of meshes or mesh groups that will recalculate the normals and tangents for this shape when it's fetched")]
        public string[] idsToRecalculateNormalsAndTangents;

        public bool TryGetBlendShape(string queryId, Mesh mesh, out BlendShape shape, Vector3[] vertices = null, Vector3[] normals = null, Vector4[] tangents = null, MeshDataTools.WeldedVertex[] mergedVertices = null)
        {
            return TryGetBlendShape(queryId, mesh, out shape, null, null, vertices, normals, tangents, mergedVertices);
        }
        public bool TryGetBlendShape(string queryId, Mesh mesh, out BlendShape shape, IEnumerable<MorphShape> morphShapes, Vector3[] vertices = null, Vector3[] normals = null, Vector4[] tangents = null, MeshDataTools.WeldedVertex[] mergedVertices = null)
        {
            return TryGetBlendShape(queryId, mesh, out shape, morphShapes, null, vertices, normals, tangents, mergedVertices);
        }
        public bool TryGetBlendShape(string queryId, Mesh mesh, out BlendShape shape, IEnumerable<MeshShape> meshShapes, Vector3[] vertices = null, Vector3[] normals = null, Vector4[] tangents = null, MeshDataTools.WeldedVertex[] mergedVertices = null)
        {
            return TryGetBlendShape(queryId, mesh, out shape, null, meshShapes, vertices, normals, tangents, mergedVertices);
        }
        public bool TryGetBlendShape(string queryId, Mesh mesh, out BlendShape shape, IEnumerable<MorphShape> morphShapes, IEnumerable<MeshShape> meshShapes, Vector3[] vertices = null, Vector3[] normals = null, Vector4[] tangents = null, MeshDataTools.WeldedVertex[] mergedVertices = null)
        {
            shape = null;
            if (mesh.GetBlendShapeIndex(targetName) < 0)
            {
                if (alternateTargetNames != null)
                {
                    foreach (var altName in alternateTargetNames)
                    {
                        if (mesh.GetBlendShapeIndex(altName) < 0) continue;

                        shape = new BlendShape(mesh, altName);
                        break;
                    }
                }
                if (shape == null) return false;
            }
            else
            {
                shape = new BlendShape(mesh, targetName);
            }

            bool recalculateNorms = recalculateNormals;
            bool recalculateTans = recalculateTangents;
            if (idsToRecalculateNormalsAndTangents != null)
            {
                foreach (var id in idsToRecalculateNormalsAndTangents)
                {
                    if (id == queryId || id == mesh.name)
                    {
                        recalculateNorms = true;
                        recalculateTans = true;
                        break;
                    }
                }
            }
            if (recalculateNorms || recalculateTans)
            {
                if (vertices == null) vertices = mesh.vertices;
                if (normals == null) normals = mesh.normals;
                if (tangents == null) tangents = mesh.tangents;

                Mesh tempMesh = GameObject.Instantiate(mesh);

                for (int a = 0; a < shape.frames.Length; a++)
                {
                    var frame = shape.frames[a];

                    var tempVertices = mesh.vertices;
                    for (int b = 0; b < tempVertices.Length; b++)
                    {
                        tempVertices[b] = vertices[b] + frame.deltaVertices[b];
                    }

                    Vector3[] tempBaseNormals = null;
                    Vector4[] tempBaseTangents = null;
                    if (recalculateNormalsTangentsShapeMix != null && recalculateNormalsTangentsShapeMix.Length > 0)
                    {
                        tempBaseNormals = mesh.normals;
                        tempBaseTangents = mesh.tangents;
                        foreach (var targetMixShape in recalculateNormalsTangentsShapeMix)
                        {
                            var baseTargetSetup = new BlendShapeTarget()
                            {
                                newName = targetMixShape.shapeName,
                                targetName = targetMixShape.shapeName,
                                recalculateNormals = targetMixShape.recalculateNormals,
                                recalculateTangents = targetMixShape.recalculateTangents
                            };

                            if (baseTargetSetup.TryGetBlendShape(queryId, mesh, out var mixShape, vertices, normals, tangents, mergedVertices))
                            {
                                tempVertices = mixShape.GetTransformedVertices(tempVertices, targetMixShape.weight, false);
                                tempBaseNormals = mixShape.GetTransformedNormals(tempBaseNormals, targetMixShape.weight, false);
                                tempBaseTangents = mixShape.GetTransformedTangents(tempBaseTangents, targetMixShape.weight, false);
                            }
                        }
                    }

                    tempMesh.vertices = tempVertices;

                    if (recalculateNorms)
                    {
                        tempMesh.RecalculateNormals();
                        var tempNormals = tempMesh.normals;

                        if (mergedVertices == null) mergedVertices = MeshDataTools.WeldVertices(vertices);
                        for (int b = 0; b < mergedVertices.Length; b++)
                        {
                            var mv = mergedVertices[b];
                            if (b != mv.firstIndex) continue;

                            var normal = Vector3.zero;
                            for (int c = 0; c < mv.indices.Count; c++) normal = normal + tempNormals[mv.indices[c]];

                            normal = normal.normalized;
                            for (int c = 0; c < mv.indices.Count; c++) tempNormals[mv.indices[c]] = normal;
                        }

                        var subNormals = (!dontSubtractMixPostRecalculation && tempBaseNormals != null) ? tempBaseNormals : normals;
                        for (int b = 0; b < tempVertices.Length; b++) frame.deltaNormals[b] = tempNormals[b] - subNormals[b];

                        tempMesh.normals = tempNormals;
                    }
                    if (recalculateTans)
                    {
                        tempMesh.RecalculateTangents();
                        var tempTangents = tempMesh.tangents;

                        var subTangents = (!dontSubtractMixPostRecalculation && tempBaseTangents != null) ? tempBaseTangents : tangents;
                        for (int b = 0; b < tempVertices.Length; b++) frame.deltaTangents[b] = tempTangents[b] - subTangents[b];
                    }
                }
            }

            if ((copyNormals || copyTangents) && copyNormalsTangentsShapeMix != null && copyNormalsTangentsShapeMix.Length > 0)
            {
                for (int a = 0; a < shape.frames.Length; a++)
                {
                    var frame = shape.frames[a];

                    for (int b = 0; b < mesh.vertexCount; b++)
                    {
                        if (copyNormals) frame.deltaNormals[b] = Vector3.zero;
                        if (copyTangents) frame.deltaTangents[b] = Vector3.zero;
                    }
                }

                foreach (var targetMixShape in copyNormalsTangentsShapeMix)
                {
                    var baseTargetSetup = new BlendShapeTarget()
                    {
                        newName = targetMixShape.shapeName,
                        targetName = targetMixShape.shapeName,
                        recalculateNormals = targetMixShape.recalculateNormals,
                        recalculateTangents = targetMixShape.recalculateTangents
                    };

                    if (baseTargetSetup.TryGetBlendShape(queryId, mesh, out var mixShape, vertices, normals, tangents, mergedVertices))
                    {
                        int frameCount = Mathf.Min(shape.frames.Length, mixShape.frames.Length);
                        for (int a = 0; a < frameCount; a++)
                        {
                            var frame = shape.frames[a];
                            var baseFrame = mixShape.frames[a];

                            for (int b = 0; b < mesh.vertexCount; b++)
                            {
                                if (copyNormals) frame.deltaNormals[b] = frame.deltaNormals[b] + baseFrame.deltaNormals[b] * targetMixShape.weight;
                                if (copyTangents) frame.deltaTangents[b] = frame.deltaTangents[b] + baseFrame.deltaTangents[b] * targetMixShape.weight;
                            }
                        }
                    }
                }
            }

            if (baseTargetNames != null)
            {
                foreach (var baseTarget in baseTargetNames)
                {
                    var baseTargetSetup = new BlendShapeTarget()
                    {
                        newName = baseTarget.shapeName,
                        targetName = baseTarget.shapeName,
                        recalculateNormals = baseTarget.recalculateNormals,
                        recalculateTangents = baseTarget.recalculateTangents
                    };

                    if (baseTargetSetup.TryGetBlendShape(queryId, mesh, out var baseShape, vertices, normals, tangents, mergedVertices))
                    {
                        int frameCount = Mathf.Min(shape.frames.Length, baseShape.frames.Length);
                        for (int a = 0; a < frameCount; a++)
                        {
                            var frame = shape.frames[a];
                            var baseFrame = baseShape.frames[a];

                            for (int b = 0; b < mesh.vertexCount; b++)
                            {
                                frame.deltaVertices[b] = frame.deltaVertices[b] - baseFrame.deltaVertices[b];
                                frame.deltaNormals[b] = frame.deltaNormals[b] - baseFrame.deltaNormals[b];
                                frame.deltaTangents[b] = frame.deltaTangents[b] - baseFrame.deltaTangents[b];
                            }
                        }
                    }
                }
            }

            if (baseTargetMix != null)
            {
                var tempDeltaVertices = new Vector3[mesh.vertexCount];
                var tempDeltaNormals = new Vector3[mesh.vertexCount];
                var tempDeltaTangents = new Vector4[mesh.vertexCount];
                foreach (var mix in baseTargetMix)
                {
                    var baseTargetSetup = new BlendShapeTarget()
                    {
                        newName = mix.shapeName,
                        targetName = mix.shapeName,
                        recalculateNormals = mix.recalculateNormals,
                        recalculateTangents = mix.recalculateTangents
                    };

                    if (baseTargetSetup.TryGetBlendShape(queryId, mesh, out var baseShape, vertices, normals, tangents, mergedVertices))
                    {
                        for (int v = 0; v < mesh.vertexCount; v++)
                        {
                            tempDeltaVertices[v] = Vector3.zero;
                            tempDeltaNormals[v] = Vector3.zero;
                            tempDeltaTangents[v] = Vector4.zero;
                        }

                        baseShape.GetTransformedData(tempDeltaVertices, tempDeltaNormals, tempDeltaTangents, mix.weight, out tempDeltaVertices, out tempDeltaNormals, out tempDeltaTangents, false);

                        int frameCount = shape.frames.Length;
                        for (int a = 0; a < frameCount; a++)
                        {
                            var frame = shape.frames[a];
                            for (int b = 0; b < mesh.vertexCount; b++)
                            {
                                frame.deltaVertices[b] = frame.deltaVertices[b] - tempDeltaVertices[b];
                                frame.deltaNormals[b] = frame.deltaNormals[b] - tempDeltaNormals[b];
                                frame.deltaTangents[b] = frame.deltaTangents[b] - new Vector3(tempDeltaTangents[b].x, tempDeltaTangents[b].y, tempDeltaTangents[b].z);
                            }
                        }
                    }
                }
            }

            if (baseMorphNames != null)
            {
                bool flag = true;

                if (morphShapes != null)
                {
                    foreach (var baseMorphName in baseMorphNames)
                    {
                        if (!string.IsNullOrWhiteSpace(baseMorphName))
                        {
                            MorphShape baseMorph = null;
                            foreach (var morph in morphShapes)
                            {
                                if (morph.name == baseMorphName)
                                {
                                    baseMorph = morph;
                                    break;
                                }
                            }

                            if (baseMorph != null)
                            {
                                flag = false;

                                int frameCount = shape.frames.Length;
                                for (int a = 0; a < frameCount; a++)
                                {
                                    var frame = shape.frames[a];
                                    for (int b = 0; b < mesh.vertexCount; b++)
                                    {
                                        if (baseMorph.DeltaVertices != null) frame.deltaVertices[b] = frame.deltaVertices[b] - baseMorph.DeltaVertices[b];
                                        if (baseMorph.DeltaNormals != null) frame.deltaNormals[b] = frame.deltaNormals[b] - baseMorph.DeltaNormals[b];
                                        if (baseMorph.DeltaTangents != null) frame.deltaTangents[b] = frame.deltaTangents[b] - baseMorph.DeltaTangents[b];
                                    }
                                }
                            }
                        }
                    }
                }

                if (flag && meshShapes != null)
                {
                    foreach (var baseMorphName in baseMorphNames)
                    {
                        if (!string.IsNullOrWhiteSpace(baseMorphName))
                        {
                            MeshShape baseMeshShape = null;
                            foreach (var meshShape in meshShapes)
                            {
                                if (meshShape.name == baseMorphName)
                                {
                                    baseMeshShape = meshShape;
                                    break;
                                }
                            }
                            if (baseMeshShape != null && baseMeshShape.frames != null)
                            {
                                int frameCount = Mathf.Min(shape.frames.Length, baseMeshShape.frames.Length);
                                for (int a = 0; a < frameCount; a++)
                                {
                                    var frame = shape.frames[a];
                                    var baseFrame = baseMeshShape.frames[a];
                                    for (int b = 0; b < mesh.vertexCount; b++)
                                    {
                                        frame.deltaVertices[b] = frame.deltaVertices[b] - (Vector3)baseFrame.deltas[b].deltaVertex;
                                        frame.deltaNormals[b] = frame.deltaNormals[b] - (Vector3)baseFrame.deltas[b].deltaNormal;
                                        frame.deltaTangents[b] = frame.deltaTangents[b] - (Vector3)baseFrame.deltas[b].deltaTangent;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (idsToZeroOutNormalsAndTangents != null)
            {
                bool zeroOut = false;
                foreach (var id in idsToZeroOutNormalsAndTangents)
                {
                    if (id == queryId || id == mesh.name)
                    {
                        zeroOut = true;
                        break;
                    }
                }

                if (zeroOut)
                {
                    for (int a = 0; a < shape.frames.Length; a++)
                    {
                        var frame = shape.frames[a];

                        for (int b = 0; b < mesh.vertexCount; b++)
                        {
                            frame.deltaNormals[b] = Vector3.zero;
                            frame.deltaTangents[b] = Vector3.zero;
                        }
                    }
                }
            }

            return true;
        }
    }

    [Serializable]
    public struct CompositeVertexGroupBuilder
    {

        public string name;

        public string[] addGroups;
        public string[] subtractGroups;

        public float weight;

        public bool normalize;
        public bool overrideNormalizeMaxWeight;
        public float normalizeMaxWeight;

    }

    [Serializable]
    public struct MeshMergeSlot
    {
        [Header("Which LOD mesh to merge with.")]
        public int lodSlot;
        [Header("Additional LOD meshes to merge with.")]
        public int[] additionalLodSlots;

        [Header("The mesh that will be combined with other meshes.")]
        public Mesh mesh;

        [Tooltip("Should the data being used for surface data transfers be updated?")]
        public bool updateBaseMeshData;

        public bool recalculateNormals;
        public bool recalculateTangents;

        public bool treatAsMeshIslands;

        public bool allowSurfaceDataTransfer;

        public bool transferNormals;
        [Range(0, 1)]
        public float transferNormalsWeight;
        public bool transferBoneWeights;

        public bool transferBlendShapes;
        public bool preserveExistingBlendShapeData;

        public Vector3 alignmentOffset;
        public Vector3 alignmentRotationOffset;
    }

    [Serializable]
    public enum SeamMergeMethod
    {
        ClosestUV, WeldData
    }

}

#endif