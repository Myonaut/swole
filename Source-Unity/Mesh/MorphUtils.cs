#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Linq;

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
                        Debug.LogWarning($"({mesh.name}) Target shape '{shapeTarget.targetName}' was not found. Created an empty replacement.");
                    }
                    if (shape.frames == null || shape.frames.Length <= 0) // if shape has no frames, create an empty one
                    {
                        var frame = new BlendShape.Frame(shape, 0, 0, new Vector3[mesh.vertexCount], new Vector3[mesh.vertexCount], new Vector3[mesh.vertexCount]);
                        shape.frames = new BlendShape.Frame[] { frame };

                        Debug.LogWarning($"({mesh.name}) Target shape '{shape.name}' had no frames. Created an empty one."); 
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

        public static Mesh MergeMeshSeam(Mesh mesh, Mesh baseMesh, string seamShapeName, SeamMergeMethod mergeMethod, out BlendShape seamShape, bool instantiateMesh = true, bool removeSeamShape = true, bool mergeTangents = true, bool mergeUVs = false, List<string> nonSeamMergableBlendShapes = null, bool mergeVertexColors = true)
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
                List<BlendShape> baseShapes = baseMesh.GetBlendShapes();
                foreach(var baseShape in baseShapes)
                {
                    if (nonSeamMergableBlendShapes != null && nonSeamMergableBlendShapes.Contains(baseShape.name)) continue;  

                    bool exists = false;
                    foreach(var shape in shapes)
                    {
                        if (shape.name == baseShape.name)
                        {
                            exists = true;
                            break;
                        }
                    }
                    if (exists) continue;

                    var newShape = new BlendShape(baseShape.name, baseShape.frames, mesh.vertexCount);
                    shapes.Add(newShape);
                }

                if (removeSeamShape) shapes.Remove(seamShape);

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
                var colors = mergeVertexColors ? mesh.colors : null;
                if (mergeVertexColors)
                {
                    if ((colors == null || colors.Length <= 0) && (baseColors != null && baseColors.Length > 0)) colors = new Color[mesh.vertexCount];
                    mergeVertexColors = colors != null && colors.Length > 0;
                }

                Debug.Log($"Merging seam '{seamShape.name}' for mesh '{mesh.name}' using base mesh '{baseMesh.name}' with method '{mergeMethod}'"); 
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

                                            Debug.DrawRay(vertex, Vector3.up * 0.001f, Color.cyan, 500f);
                                            Debug.DrawLine(vertex, baseVertices[closestVertex], Color.red, 500f);    

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
                                            if (mergeVertexColors) colors[a] = mergedColor;

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

                                                            var mergedDeltaVertex = Vector3.zero;
                                                            var mergedDeltaNormal = Vector3.zero;
                                                            var mergedDeltaTangent = Vector3.zero;

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

                                                                if (count > 1)
                                                                {
                                                                    mergedDeltaVertex = mergedDeltaVertex / count;
                                                                    mergedDeltaNormal = mergedDeltaNormal / count;
                                                                    mergedDeltaTangent = mergedDeltaTangent / count;
                                                                }
                                                            } 
                                                            else
                                                            {
                                                                mergedDeltaVertex = baseFrame.deltaVertices[closestVertex];
                                                                mergedDeltaNormal = baseFrame.deltaNormals[closestVertex];
                                                                mergedDeltaTangent = baseFrame.deltaTangents[closestVertex]; 
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

        public static void MergeVertexColorDeltasAtSeam(Mesh mesh, Mesh baseMesh, BlendShape seamShape, ICollection<VertexColorDelta> colorDeltas, ICollection<VertexColorDelta> baseColorDeltas, List<string> nonSeamMergableDeltas = null, Vector3[] baseVertices = null, Vector3[] vertices = null)
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

                    foreach (var delta in colorDeltas)
                    {
                        if (delta.deltaColors == null || delta.deltaColors.Length <= 0 || (nonSeamMergableDeltas != null && nonSeamMergableDeltas.Contains(delta.name))) continue;

                        foreach (var baseDelta in baseColorDeltas)
                        {
                            if (baseDelta.deltaColors == null || baseDelta.deltaColors.Length <= 0 || delta.name != baseDelta.name) continue;

                            delta.deltaColors[a] = baseDelta.deltaColors[closestVertex];

                            break;
                        }
                    }

                }
            }
        }

        public static void MergeV3AtSeam(Mesh mesh, Mesh baseMesh, BlendShape seamShape, Vector3[] mainV3, Vector3[] baseV3, XYZChannel channels, Vector3[] baseVertices = null, Vector3[] vertices = null)
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

                    var v = mainV3[a];
                    var baseV = baseV3[closestVertex];

                    if (channels.HasFlag(XYZWChannel.X)) v.x = baseV.x;
                    if (channels.HasFlag(XYZWChannel.Y)) v.y = baseV.y;
                    if (channels.HasFlag(XYZWChannel.Z)) v.z = baseV.z;

                    mainV3[a] = v;
                }
            }
        }

        public static void MergeV4AtSeam(Mesh mesh, Mesh baseMesh, BlendShape seamShape, Vector4[] mainV4, Vector4[] baseV4, XYZWChannel channels, Vector3[] baseVertices = null, Vector3[] vertices = null)
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

                    var v = mainV4[a];
                    var baseV = baseV4[closestVertex];

                    if (channels.HasFlag(XYZWChannel.X)) v.x = baseV.x;
                    if (channels.HasFlag(XYZWChannel.Y)) v.y = baseV.y;
                    if (channels.HasFlag(XYZWChannel.Z)) v.z = baseV.z;
                    if (channels.HasFlag(XYZWChannel.W)) v.w = baseV.w;

                    mainV4[a] = v;
                }
            }
        }

        #endregion

        #region Surface Data Transfer

        public struct TransferSurfaceDataBase : IDisposable
        {
            public float costWeight;
            public float CostWeight => costWeight == 0f ? 1f : costWeight;

            public Dictionary<string, float> shapeCostWeights;
            public float GetCostWeight(string shape)
            {
                if (string.IsNullOrWhiteSpace(shape) || shapeCostWeights == null) return CostWeight;
                if (shapeCostWeights.TryGetValue(shape, out float costWeight)) return costWeight;
                return CostWeight;
            }

            public Mesh baseMesh;

            public int[] baseMeshTriangles;
            public Vector3[] baseMeshVertices;

            public NativeArray<int> baseMeshTrianglesJob;
            public NativeArray<float3> baseMeshVerticesJob;
            public NativeArray<float3> baseMeshNormalsJob;
            public NativeArray<float2> baseMeshTransferUVsJob;

            public TransferSurfaceDataBase WithJobData(Allocator allocator, bool includeTransferUV = false, UVChannelURP transferChannel = UVChannelURP.UV0)
            {
                var output = this;

                output.baseMeshTrianglesJob = new NativeArray<int>(baseMeshTriangles == null ? new int[0] : baseMeshTriangles, allocator);
                output.baseMeshVerticesJob = new NativeArray<Vector3>(baseMeshVertices == null ? new Vector3[0] : baseMeshVertices, allocator).Reinterpret<float3>();
                output.baseMeshNormalsJob = new NativeArray<Vector3>(baseMeshNormals == null ? new Vector3[0] : baseMeshNormals, allocator).Reinterpret<float3>();

                if (includeTransferUV)
                {
                    int uvIndex = (int)transferChannel;
                    if (uvIndex >= 0 && baseMeshUVs != null && uvIndex < baseMeshUVs.Count)
                    {
                        var uvs = baseMeshUVs[uvIndex];
                        output.baseMeshTransferUVsJob = new NativeArray<float2>(uvs.Length, allocator);
                        for (int i = 0; i < uvs.Length; i++) output.baseMeshTransferUVsJob[i] = new float2(uvs[i].x, uvs[i].y);
                    } 
                    else
                    {
                        Debug.LogError($"UV Transfer Channel {uvIndex} was out of range for mesh {baseMesh.name}");
                        output.baseMeshTransferUVsJob = new NativeArray<float2>(output.baseMeshVerticesJob.Length, allocator);
                    }
                }

                return output;
            }

            public Vector3[] baseMeshNormals;

            public Vector4[] baseMeshTangents;

            public List<Vector4[]> baseMeshUVs;

            public Color[] baseMeshVertexColors;

            public NativeArray<BoneWeight1> baseMeshBoneWeights;
            public NativeArray<byte> baseMeshBonesPerVertex;
            public int[] baseMeshBoneWeightStartIndices;

            public List<BlendShape> baseMeshBlendShapes;
            public bool TryGetBlendShape(string shapeName, out BlendShape blendShape)
            {
                blendShape = null;
                if (baseMeshBlendShapes == null) return false;

                foreach(var b in baseMeshBlendShapes)
                {
                    if (b != null && b.name == shapeName)
                    {
                        blendShape = b;
                        return true;
                    }
                }
                return false;
            }

            public Vector3 alignmentOffset;
            public Vector3 alignmentRotationOffsetEuler;

            public TransferSurfaceDataBase ApplyBlendShapesToBaseData(IEnumerable<NameFloat> shapes)
            {
                var output = this;

                output.baseMeshVertices = (Vector3[])baseMeshVertices.Clone();
                output.baseMeshNormals = (Vector3[])baseMeshNormals.Clone();
                output.baseMeshTangents = (Vector4[])baseMeshTangents.Clone(); 

                foreach (var shapeTarget in shapes)
                {
                    if (!TryGetBlendShape(shapeTarget.name, out var blendShape)) continue;

                    blendShape.GetTransformedData(output.baseMeshVertices, output.baseMeshNormals, output.baseMeshTangents, shapeTarget.value, out output.baseMeshVertices, out output.baseMeshNormals, out output.baseMeshTangents, false);
                }

                return output;
            }

            public void Dispose()
            {
                if (this.baseMeshTrianglesJob.IsCreated) 
                {
                    this.baseMeshTrianglesJob.Dispose();
                    this.baseMeshTrianglesJob = default;
                }
                if (this.baseMeshVerticesJob.IsCreated) 
                {
                    this.baseMeshVerticesJob.Dispose();
                    this.baseMeshVerticesJob = default;
                }
                if (this.baseMeshNormalsJob.IsCreated)
                {
                    this.baseMeshNormalsJob.Dispose();
                    this.baseMeshNormalsJob = default;
                }
                if (this.baseMeshTransferUVsJob.IsCreated)
                {
                    this.baseMeshTransferUVsJob.Dispose();
                    this.baseMeshTransferUVsJob = default;
                }
            }
        }

        public struct TransferSurfaceDataSettings : IDisposable
        {
            public bool useUVsForBaseDataTransfers;
            public UVChannelURP baseUV_transferChannel;
            public UVChannelURP localUV_transferChannel;

            public float uvDistanceWeight;
            public float positionDistanceWeight;
            public float normalDotWeight;
            public float triCoordinatesWeight;

            public float maxDistanceUV;
            public float maxDistancePosition;

            public bool treatAsMeshIslands;

            public float[] meshIslandRootWeights;
            public bool HasMeshIslandRootWeights => meshIslandRootWeights != null && meshIslandRootWeights.Length > 0;
            public float[] meshIslandBlendWeights;
            public bool HasMeshIslandBlendWeights => meshIslandBlendWeights != null && meshIslandBlendWeights.Length > 0;

            public bool transferNormals;
            public float transferNormalsWeight;

            public bool transferUVs;
            public bool preserveExistingUVData;
            public Vector2Int uvTransferRange;
            public int uvChannelIndexTransferOffset;

            public bool transferVertexColors;

            public bool transferBoneWeights;

            public bool transferBlendShapes;
            public bool preserveExistingBlendShapeData;

            public NameFloat[] shapesToApplyBeforeTransfer;

            public TransferSurfaceDataBase baseDataMain;
            public TransferSurfaceDataBase[] baseDatas;

            public TransferSurfaceDataSettings WithJobData(Allocator allocator)
            {
                var output = this;

                output.baseDataMain = baseDataMain.WithJobData(allocator, useUVsForBaseDataTransfers, baseUV_transferChannel);
                if (baseDatas != null)
                {
                    output.baseDatas = new TransferSurfaceDataBase[baseDatas.Length];
                    for (int a = 0; a < output.baseDatas.Length; a++) output.baseDatas[a] = baseDatas[a].WithJobData(allocator, useUVsForBaseDataTransfers, baseUV_transferChannel);
                }

                return output;
            }

            public void Dispose()
            {
                baseDataMain.Dispose();
                if (baseDatas != null)
                {
                    foreach(var baseData in baseDatas)
                    {
                        baseData.Dispose();
                    }
                }
            }

            public int BaseDataCount => baseDatas == null ? 1 : (1 + baseDatas.Length);
            public TransferSurfaceDataBase GetBaseData(int index)
            {
                if (baseDatas == null || baseDatas.Length <= 0 || index <= 0 || index >= BaseDataCount) return baseDataMain; 

                return baseDatas[index - 1]; 
            }

            public TransferSurfaceDataSettings ApplyBlendShapesToBaseData(IEnumerable<NameFloat> shapes)
            {
                var output = this;

                output.baseDataMain = output.baseDataMain.ApplyBlendShapesToBaseData(shapes);
                if (baseDatas != null)
                {
                    for (int a = 0; a < baseDatas.Length; a++) baseDatas[a] = baseDatas[a].ApplyBlendShapesToBaseData(shapes);
                }

                return output;
            }

            public TransferSurfaceDataSettings Default => new TransferSurfaceDataSettings()
            {
                transferNormalsWeight = 1f,
                preserveExistingBlendShapeData = true
            };
        }

        [Serializable]
        public struct SurfaceDataTransferVertex
        {
            public int closestMesh;

            public int closestIndex0;
            public int closestIndex1;
            public int closestIndex2;
            
            public float closestWeight0;
            public float closestWeight1;
            public float closestWeight2;

            public bool hasSecondaryBinding;

            public int closestSecondaryMesh;

            public int closestSecondaryIndex0;
            public int closestSecondaryIndex1;
            public int closestSecondaryIndex2;

            public float closestSecondaryWeight0;
            public float closestSecondaryWeight1;
            public float closestSecondaryWeight2;
        }

        public struct TempVertexInfo
        {
            public Vector3 centerPoint;

            public int meshIslandIndex;
            public MeshDataTools.MeshIsland meshIsland;
            public ContainingTri meshIslandTri;

            public Vector3 originOffset;
            public float originOffsetDist;

            public bool hasOrigin;

            public float closestDistance;
            public int closestBaseIndex;
            public int closestIndex0;
            public int closestIndex1;
            public int closestIndex2;
            public float closestWeight0;
            public float closestWeight1;
            public float closestWeight2;

            public bool hasSecondaryBinding;
            public int closestSecondaryBaseIndex;
            public int closestSecondaryIndex0;
            public int closestSecondaryIndex1;
            public int closestSecondaryIndex2;
            public float closestSecondaryWeight0;
            public float closestSecondaryWeight1;
            public float closestSecondaryWeight2;

            public static TempVertexInfo Default => new TempVertexInfo()
            {
                closestIndex0 = -1,
                closestIndex1 = -1,
                closestIndex2 = -1,
                closestSecondaryIndex0 = -1,
                closestSecondaryIndex1 = -1,
                closestSecondaryIndex2 = -1
            };
        }

        /// <summary>
        /// basically if the surface that a vertex is bound to changes normal direction, rotate the vertex around the center point accordingly
        /// </summary>
        public static Vector3 AddNormalBasedRotationToDelta(TempVertexInfo vertexInfo, Vector3 localVertexPosition, Vector3 dependencyNormal, Vector3 deltaVertex, Vector3 deltaNormal, float weight)
        {
            if (vertexInfo.hasOrigin)
            {
                Quaternion rotOffset = Quaternion.FromToRotation(dependencyNormal, (dependencyNormal + deltaNormal).normalized);
                deltaVertex = deltaVertex + ((vertexInfo.centerPoint + (rotOffset * vertexInfo.originOffset) * vertexInfo.originOffsetDist) - localVertexPosition) * weight;
            }

            return deltaVertex;
        }

        public static Mesh TransferSurfaceData(
            Mesh localMesh, bool instantiateMesh,
            TransferSurfaceDataSettings settings_, SurfaceDataTransferVertex[] vertexTransferDataArray = null, Dictionary<string, SurfaceDataTransferVertex[]> perShapeVertexTransferDataArrays = null)
        {
            using (var settings = settings_.WithJobData(Allocator.Persistent))
            {
                var mesh = instantiateMesh ? MeshDataTools.Duplicate(localMesh) : localMesh;

                int baseDataCount = settings.BaseDataCount;

                bool hasRootWeights = settings.HasMeshIslandRootWeights;
                bool hasIslandBlendWeights = settings.HasMeshIslandBlendWeights;

                Dictionary<BlendShape.Frame, Vector3> tempFrameNormals = new Dictionary<BlendShape.Frame, Vector3>();
                List<string> originalBlendShapes = new List<string>();
                List<BlendShape> blendShapes = mesh.GetBlendShapes();
                foreach (var shape in blendShapes) originalBlendShapes.Add(shape.name);

                bool TryGetOriginalBlendShape(string name, out BlendShape blendShape)
                {
                    blendShape = null;
                    foreach (var shape in blendShapes)
                    {
                        if (shape.name == name)
                        {
                            blendShape = shape;
                            return true;
                        }
                    }
                    return false;
                }

                bool IsOriginalBlendShape(string name)
                {
                    foreach (var shapeName in originalBlendShapes) if (shapeName == name) return true;
                    return false;
                }
                BlendShape AddOrGetBlendShape(string name, BlendShape.Frame[] referenceFrames)
                {
                    foreach (var shape in blendShapes)
                    {
                        if (shape.name == name)
                        {
                            // if the shape has no frames, add a default frame
                            if (shape.frames == null || shape.frames.Length <= 0) shape.AddFrame(referenceFrames == null || referenceFrames.Length <= 0 ? 1 : referenceFrames[0].weight, new Vector3[mesh.vertexCount], new Vector3[mesh.vertexCount], new Vector3[mesh.vertexCount]);

                            return shape;
                        }
                    }

                    // if the shape does not exist, create it
                    BlendShape newShape = new BlendShape(name, referenceFrames, mesh.vertexCount, false);
                    blendShapes.Add(newShape);

                    return newShape;
                }

                List<MeshDataTools.MeshIsland> meshIslands = null;
                int[] meshIslandBindings = null;
                ContainingTri[] meshIslandTriBindings = null; // stores the triangle from the base mesh that each island is bound to
                Dictionary<string, ContainingTri[]> perShapeMeshIslandTriBindings = null;
                if (settings.treatAsMeshIslands) meshIslands = MeshDataTools.CalculateMeshIslands(mesh);

                Vector3[] localVertices = mesh.vertices;
                Vector2[] localTransferUVs = settings.useUVsForBaseDataTransfers ? mesh.GetUVsByChannel(settings.localUV_transferChannel) : null;
                Vector3[] localNormals = mesh.normals;
                Vector4[] localTangents = mesh.tangents;

                if (settings.shapesToApplyBeforeTransfer != null && settings.shapesToApplyBeforeTransfer.Length > 0)
                {
                    foreach (var shapeTarget in settings.shapesToApplyBeforeTransfer)
                    {
                        if (TryGetOriginalBlendShape(shapeTarget.name, out var blendShape))
                        {
                            blendShape.GetTransformedData(localVertices, localNormals, localTangents, shapeTarget.value, out localVertices, out localNormals, out localTangents, false); 
                        }
                    }
                }

                List<Vector4[]> localUVs = null;
                bool[] initialUVDataStates = null;
                int uvTransferCount = 0;
                if (settings.transferUVs && settings.baseDataMain.baseMeshUVs != null)
                {
                    localUVs = mesh.GetAllUVs();

                    if (settings.preserveExistingUVData)
                    {
                        initialUVDataStates = new bool[localUVs.Count];
                        for (int a = 0; a < localUVs.Count; a++) initialUVDataStates[a] = localUVs[a] != null;
                    }

                    uvTransferCount = Mathf.Min(Mathf.Max(0, (Mathf.Min(settings.uvTransferRange.y, 7) - settings.uvTransferRange.x) + 1), settings.baseDataMain.baseMeshUVs.Count);
                    for (int a = 0; a < uvTransferCount; a++)
                    {
                        int baseIndex = a + settings.uvTransferRange.x;
                        if (baseIndex < 0) continue;

                        int localIndex = a + settings.uvChannelIndexTransferOffset;
                        if (localIndex < 0 || localIndex >= 8) continue;

                        if (localUVs[localIndex] == null && settings.baseDataMain.baseMeshUVs[baseIndex] != null) localUVs[localIndex] = new Vector4[mesh.vertexCount];
                    }
                }

                Color[] localColors = null;
                if (settings.transferVertexColors)
                {
                    localColors = mesh.colors;
                    if (localColors == null || localColors.Length < mesh.vertexCount) localColors = new Color[mesh.vertexCount];
                }

                Dictionary<int2, float> newBoneWeights = new Dictionary<int2, float>();

                void CalculateMeshIslandTriBindings(string shape)
                {
                    if (meshIslands != null)
                    {
                        if (meshIslandBindings == null)
                        {
                            meshIslandBindings = new int[localVertices.Length];
                            for (int c = 0; c < meshIslandBindings.Length; c++) meshIslandBindings[c] = -1;
                        }

                        ContainingTri[] meshIslandTriBindings_;
                        if (string.IsNullOrWhiteSpace(shape))
                        {
                            if (meshIslandTriBindings != null) return;
                            meshIslandTriBindings = meshIslandTriBindings_ = new ContainingTri[meshIslands.Count];
                        }
                        else
                        {
                            if (perShapeMeshIslandTriBindings == null) perShapeMeshIslandTriBindings = new Dictionary<string, ContainingTri[]>();
                            if (perShapeMeshIslandTriBindings.ContainsKey(shape)) return;
                            perShapeMeshIslandTriBindings[shape] = meshIslandTriBindings_ = new ContainingTri[meshIslands.Count];
                        }

                        for (int c = 0; c < meshIslands.Count; c++)
                        {

                            var island = meshIslands[c];

                            if (island.vertices == null || island.vertices.Length <= 0) continue;

                            Vector3 localVertex;

                            float closestDistance = float.MaxValue;
                            int closestLocalIndex = -1;
                            int closestBaseIndex = 0;

                            for (int d = 0; d < island.vertices.Length; d++)
                            {
                                float distanceWeight = 1f; // TODO: Add possible distance weighting based on position in the mesh island or vertex group or vertex colors

                                int localIndex = island.vertices[d];
                                localVertex = localVertices[localIndex];

                                meshIslandBindings[localIndex] = c;

                                if (hasRootWeights)
                                {
                                    float rootWeight = settings.meshIslandRootWeights[localIndex];
                                    if (rootWeight <= 0.001f) continue;

                                    distanceWeight = distanceWeight + Mathf.Max(0f, (10f * (1f - rootWeight)));
                                }
                                for (int e = 0; e < baseDataCount; e++)
                                {
                                    var baseData = settings.GetBaseData(e);
                                    float costWeight = baseData.GetCostWeight(shape);
                                    foreach (var v in baseData.baseMeshVertices)
                                    {
                                        float dista = (v - localVertex).sqrMagnitude * distanceWeight * costWeight;
                                        if (dista < closestDistance)
                                        {
                                            closestBaseIndex = e;
                                            closestLocalIndex = d;
                                            closestDistance = dista;
                                        }
                                    }
                                }
                            }

                            island.originIndex = closestLocalIndex;

                            closestDistance = float.MaxValue;
                            //closestLocalIndex = -1;
                            int closestIndex0 = -1;
                            int closestIndex1 = -1;
                            int closestIndex2 = -1;
                            float closestWeight0 = 0;
                            float closestWeight1 = 0;
                            float closestWeight2 = 0;

                            int localVertexIndex = island.vertices[island.originIndex];
                            localVertex = localVertices[localVertexIndex];
                            var localNormal = localNormals[localVertexIndex];

                            var baseDataFinal = settings.GetBaseData(closestBaseIndex);
                            if (settings.useUVsForBaseDataTransfers)
                            {
                                if (MeshDataTools.GetClosestContainingTriangleFromJobWithUV(baseDataFinal.baseMeshTransferUVsJob, settings.uvDistanceWeight, settings.positionDistanceWeight, settings.normalDotWeight, settings.triCoordinatesWeight, baseDataFinal.baseMeshVerticesJob, baseDataFinal.baseMeshNormalsJob, baseDataFinal.baseMeshTrianglesJob, localTransferUVs[localVertexIndex], localVertex, localNormal, out var index0, out var index1, out var index2, out var weight0, out var weight1, out var weight2, out var dist, settings.maxDistanceUV, settings.maxDistancePosition, 0f, 0.2f))
                                {
                                    if (dist < closestDistance)
                                    {
                                        closestDistance = dist;
                                        closestIndex0 = index0;
                                        closestIndex1 = index1;
                                        closestIndex2 = index2;
                                        closestWeight0 = weight0;
                                        closestWeight1 = weight1;
                                        closestWeight2 = weight2;
                                    }
                                }
                            } 
                            else
                            {
                                if (MeshDataTools.GetClosestContainingTriangleFromJob(baseDataFinal.baseMeshVerticesJob, baseDataFinal.baseMeshTrianglesJob, localVertex, out var index0, out var index1, out var index2, out var weight0, out var weight1, out var weight2, out var dist, settings.maxDistancePosition, 0.2f))
                                {
                                    if (dist < closestDistance)
                                    {
                                        closestDistance = dist;
                                        closestIndex0 = index0;
                                        closestIndex1 = index1;
                                        closestIndex2 = index2;
                                        closestWeight0 = weight0;
                                        closestWeight1 = weight1;
                                        closestWeight2 = weight2;
                                    }
                                }
                            }

                            meshIslandTriBindings_[c] = new ContainingTri(closestBaseIndex, closestIndex0, closestIndex1, closestIndex2, closestWeight0, closestWeight1, closestWeight2);
                        }
                    }
                }

                CalculateMeshIslandTriBindings(null);
                HashSet<string> costWeightedShapes = new HashSet<string>();
                for (int e = 0; e < baseDataCount; e++)
                {
                    var baseData = settings.GetBaseData(e);
                    if (baseData.shapeCostWeights != null)
                    {
                        foreach (var entry in baseData.shapeCostWeights)
                        {
                            costWeightedShapes.Add(entry.Key);
                            CalculateMeshIslandTriBindings(entry.Key); // Calculate containing tri arrays per shape if different cost weights are provided
                        }
                    }
                }

                Dictionary<string, TempVertexInfo> perShapeVertexInfos = new Dictionary<string, TempVertexInfo>();
                for (int vIndex = 0; vIndex < localVertices.Length; vIndex++)
                {
                    var localVertex = localVertices[vIndex];
                    var localNormal = localNormals[vIndex];

                    TempVertexInfo CalculateVertexInfo(string shape)
                    {
                        TempVertexInfo vertexInfo = TempVertexInfo.Default;

                        bool isDefault = string.IsNullOrWhiteSpace(shape);
                        ContainingTri[] meshIslandTriBindings_ = meshIslandTriBindings;
                        if (!isDefault && perShapeMeshIslandTriBindings != null)
                        {
                            if (!perShapeMeshIslandTriBindings.TryGetValue(shape, out meshIslandTriBindings_)) meshIslandTriBindings_ = meshIslandTriBindings; // no special mesh island data for the shape was found, so revert to default
                        }

                        vertexInfo.centerPoint = localVertex;

                        vertexInfo.meshIslandIndex = meshIslandBindings == null ? -1 : meshIslandBindings[vIndex];
                        vertexInfo.meshIsland = vertexInfo.meshIslandIndex >= 0 ? meshIslands[vertexInfo.meshIslandIndex] : default;
                        vertexInfo.meshIslandTri = vertexInfo.meshIslandIndex >= 0 ? meshIslandTriBindings_[vertexInfo.meshIslandIndex] : default;
                        if (vertexInfo.meshIslandIndex >= 0) vertexInfo.centerPoint = localVertices[vertexInfo.meshIsland.OriginVertex];

                        vertexInfo.originOffset = localVertex - vertexInfo.centerPoint;
                        vertexInfo.originOffsetDist = vertexInfo.originOffset.magnitude;

                        vertexInfo.hasOrigin = false;
                        if (vertexInfo.originOffsetDist > 0.0001f)
                        {
                            vertexInfo.originOffset = vertexInfo.originOffset / vertexInfo.originOffsetDist;
                            vertexInfo.hasOrigin = true;
                        }

                        vertexInfo.closestDistance = float.MaxValue;
                        vertexInfo.closestBaseIndex = 0;
                        vertexInfo.closestIndex0 = -1;
                        vertexInfo.closestIndex1 = -1;
                        vertexInfo.closestIndex2 = -1;
                        vertexInfo.closestWeight0 = 0;
                        vertexInfo.closestWeight1 = 0;
                        vertexInfo.closestWeight2 = 0;

                        vertexInfo.hasSecondaryBinding = false;
                        vertexInfo.closestSecondaryBaseIndex = 0;
                        vertexInfo.closestSecondaryIndex0 = -1;
                        vertexInfo.closestSecondaryIndex1 = -1;
                        vertexInfo.closestSecondaryIndex2 = -1;
                        vertexInfo.closestSecondaryWeight0 = 0;
                        vertexInfo.closestSecondaryWeight1 = 0;
                        vertexInfo.closestSecondaryWeight2 = 0;

                        float meshIslandBlend = 1f;
                        if (hasIslandBlendWeights) meshIslandBlend = math.saturate(settings.meshIslandBlendWeights[vIndex]);
                        if (vertexInfo.meshIslandIndex < 0 || meshIslandBlend < 1f)
                        {
                            bool isSecondary = vertexInfo.meshIslandIndex >= 0 && meshIslandBlend > 0f;
                            float meshIslandInverseBlend = 1f - meshIslandBlend;

                            for (int e = 0; e < baseDataCount; e++)
                            {
                                var baseData = settings.GetBaseData(e);
                                if (settings.useUVsForBaseDataTransfers)
                                {
                                    if (MeshDataTools.GetClosestContainingTriangleFromJobWithUV(baseData.baseMeshTransferUVsJob, settings.uvDistanceWeight, settings.positionDistanceWeight, settings.normalDotWeight, settings.triCoordinatesWeight, baseData.baseMeshVerticesJob, baseData.baseMeshNormalsJob, baseData.baseMeshTrianglesJob, localTransferUVs[vIndex], localVertex, localNormal, out var index0, out var index1, out var index2, out var weight0, out var weight1, out var weight2, out var dist, settings.maxDistanceUV, settings.maxDistancePosition, 0f, 0.2f))
                                    {
                                        float cost = dist * baseData.GetCostWeight(shape);
                                        if (cost < vertexInfo.closestDistance)
                                        {
                                            vertexInfo.closestDistance = cost;
                                            if (isSecondary)
                                            {
                                                vertexInfo.hasSecondaryBinding = true;
                                                vertexInfo.closestSecondaryBaseIndex = e;
                                                vertexInfo.closestSecondaryIndex0 = index0;
                                                vertexInfo.closestSecondaryIndex1 = index1;
                                                vertexInfo.closestSecondaryIndex2 = index2;
                                                vertexInfo.closestSecondaryWeight0 = weight0 * meshIslandInverseBlend;
                                                vertexInfo.closestSecondaryWeight1 = weight1 * meshIslandInverseBlend;
                                                vertexInfo.closestSecondaryWeight2 = weight2 * meshIslandInverseBlend;
                                            }
                                            else
                                            {
                                                vertexInfo.closestBaseIndex = e;
                                                vertexInfo.closestIndex0 = index0;
                                                vertexInfo.closestIndex1 = index1;
                                                vertexInfo.closestIndex2 = index2;
                                                vertexInfo.closestWeight0 = weight0;
                                                vertexInfo.closestWeight1 = weight1;
                                                vertexInfo.closestWeight2 = weight2;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (MeshDataTools.GetClosestContainingTriangleFromJob(baseData.baseMeshVerticesJob, baseData.baseMeshTrianglesJob, localVertex, out var index0, out var index1, out var index2, out var weight0, out var weight1, out var weight2, out var dist, settings.maxDistancePosition, 0.2f))
                                    {
                                        float cost = dist * baseData.GetCostWeight(shape);
                                        if (cost < vertexInfo.closestDistance)
                                        {
                                            vertexInfo.closestDistance = cost;
                                            if (isSecondary)
                                            {
                                                vertexInfo.closestSecondaryBaseIndex = e;
                                                vertexInfo.closestSecondaryIndex0 = index0;
                                                vertexInfo.closestSecondaryIndex1 = index1;
                                                vertexInfo.closestSecondaryIndex2 = index2;
                                                vertexInfo.closestSecondaryWeight0 = weight0 * meshIslandInverseBlend;
                                                vertexInfo.closestSecondaryWeight1 = weight1 * meshIslandInverseBlend;
                                                vertexInfo.closestSecondaryWeight2 = weight2 * meshIslandInverseBlend;
                                            }
                                            else
                                            {
                                                vertexInfo.closestBaseIndex = e;
                                                vertexInfo.closestIndex0 = index0;
                                                vertexInfo.closestIndex1 = index1;
                                                vertexInfo.closestIndex2 = index2;
                                                vertexInfo.closestWeight0 = weight0;
                                                vertexInfo.closestWeight1 = weight1;
                                                vertexInfo.closestWeight2 = weight2;
                                            }

                                        }
                                    }
                                }
                            }

                        }
                        
                        if (vertexInfo.meshIslandIndex >= 0 && meshIslandBlend > 0f)
                        {
                            vertexInfo.closestBaseIndex = vertexInfo.meshIslandTri.closestDependency;
                            vertexInfo.closestIndex0 = vertexInfo.meshIslandTri.indexA;
                            vertexInfo.closestIndex1 = vertexInfo.meshIslandTri.indexB;
                            vertexInfo.closestIndex2 = vertexInfo.meshIslandTri.indexC;
                            vertexInfo.closestWeight0 = vertexInfo.meshIslandTri.weightA * meshIslandBlend;
                            vertexInfo.closestWeight1 = vertexInfo.meshIslandTri.weightB * meshIslandBlend;
                            vertexInfo.closestWeight2 = vertexInfo.meshIslandTri.weightC * meshIslandBlend;
                        }

                        return vertexInfo;
                    }

                    TempVertexInfo defaultVertexInfo = CalculateVertexInfo(null);
                    perShapeVertexInfos.Clear();
                    foreach (var shape in costWeightedShapes)
                    {
                        perShapeVertexInfos[shape] = CalculateVertexInfo(shape);
                    }

                    // DEBUG
                    /*if (defaultVertexInfo.closestIndex0 >= 0)
                    {
                        var debug_baseData = settings.GetBaseData(defaultVertexInfo.closestBaseIndex); 
                        var debug_surfaceVertex = debug_baseData.baseMeshVertices[defaultVertexInfo.closestIndex0];
                        Debug.DrawLine(localVertex, debug_surfaceVertex, Color.red, 1000f);
                    }*/
                    //

                    if (defaultVertexInfo.closestBaseIndex >= 0 && defaultVertexInfo.closestIndex0 >= 0 && defaultVertexInfo.closestIndex1 >= 0 && defaultVertexInfo.closestIndex2 >= 0)
                    {
                        var defaultBaseData = settings.GetBaseData(defaultVertexInfo.closestBaseIndex);
                        var defaultBaseData2 = defaultBaseData;
                        if (defaultVertexInfo.hasSecondaryBinding)
                        {
                            defaultBaseData2 = settings.GetBaseData(defaultVertexInfo.closestSecondaryBaseIndex);
                        }

                        if (vertexTransferDataArray != null && vIndex < vertexTransferDataArray.Length)
                        {
                            vertexTransferDataArray[vIndex] = new SurfaceDataTransferVertex()
                            {
                                closestMesh = defaultVertexInfo.closestBaseIndex,
                                closestIndex0 = defaultVertexInfo.closestIndex0,
                                closestIndex1 = defaultVertexInfo.closestIndex1,
                                closestIndex2 = defaultVertexInfo.closestIndex2,
                                closestWeight0 = defaultVertexInfo.closestWeight0,
                                closestWeight1 = defaultVertexInfo.closestWeight1,
                                closestWeight2 = defaultVertexInfo.closestWeight2,
                                hasSecondaryBinding = defaultVertexInfo.hasSecondaryBinding,
                                closestSecondaryMesh = defaultVertexInfo.closestSecondaryBaseIndex,
                                closestSecondaryIndex0 = defaultVertexInfo.closestSecondaryIndex0,
                                closestSecondaryIndex1 = defaultVertexInfo.closestSecondaryIndex1,
                                closestSecondaryIndex2 = defaultVertexInfo.closestSecondaryIndex2,
                                closestSecondaryWeight0 = defaultVertexInfo.closestSecondaryWeight0,
                                closestSecondaryWeight1 = defaultVertexInfo.closestSecondaryWeight1,
                                closestSecondaryWeight2 = defaultVertexInfo.closestSecondaryWeight2
                            };
                        }
                        if (perShapeVertexTransferDataArrays != null)
                        {
                            foreach (var entry in perShapeVertexInfos)
                            {
                                if (!perShapeVertexTransferDataArrays.TryGetValue(entry.Key, out var transferDataArray)) 
                                {
                                    transferDataArray = new SurfaceDataTransferVertex[localMesh.vertexCount];
                                    perShapeVertexTransferDataArrays[entry.Key] = transferDataArray;
                                }

                                if (transferDataArray != null && vIndex < transferDataArray.Length)
                                {
                                    var vertexInfo = entry.Value;
                                    transferDataArray[vIndex] = new SurfaceDataTransferVertex()
                                    {
                                        closestMesh = vertexInfo.closestBaseIndex,
                                        closestIndex0 = vertexInfo.closestIndex0,
                                        closestIndex1 = vertexInfo.closestIndex1,
                                        closestIndex2 = vertexInfo.closestIndex2,
                                        closestWeight0 = vertexInfo.closestWeight0,
                                        closestWeight1 = vertexInfo.closestWeight1,
                                        closestWeight2 = vertexInfo.closestWeight2,
                                        hasSecondaryBinding = vertexInfo.hasSecondaryBinding,
                                        closestSecondaryMesh = vertexInfo.closestSecondaryBaseIndex,
                                        closestSecondaryIndex0 = vertexInfo.closestSecondaryIndex0,
                                        closestSecondaryIndex1 = vertexInfo.closestSecondaryIndex1,
                                        closestSecondaryIndex2 = vertexInfo.closestSecondaryIndex2,
                                        closestSecondaryWeight0 = vertexInfo.closestSecondaryWeight0,
                                        closestSecondaryWeight1 = vertexInfo.closestSecondaryWeight1,
                                        closestSecondaryWeight2 = vertexInfo.closestSecondaryWeight2
                                    };
                                }
                            }
                        }

                        if (settings.transferBlendShapes)
                        {
                            for (int d = 0; d < defaultBaseData.baseMeshBlendShapes.Count; d++)
                            {
                                var shape = defaultBaseData.baseMeshBlendShapes[d];
                                if (shape != null && (!settings.preserveExistingBlendShapeData || !IsOriginalBlendShape(shape.name)))
                                {
                                    var blendShape = AddOrGetBlendShape(shape.name, shape.frames);
                                    if (blendShape != null)
                                    {
                                        var vertexInfo = defaultVertexInfo;
                                        var baseData = defaultBaseData;

                                        if (perShapeVertexInfos.TryGetValue(blendShape.name, out vertexInfo))
                                        {
                                            baseData = settings.GetBaseData(vertexInfo.closestBaseIndex);

                                            if (baseData.TryGetBlendShape(shape.name, out var shape_))
                                            {
                                                shape = shape_;
                                            }
                                            else
                                            {
                                                vertexInfo = defaultVertexInfo;
                                                baseData = defaultBaseData;
                                            }
                                        }
                                        else
                                        {
                                            vertexInfo = defaultVertexInfo;
                                            baseData = defaultBaseData;
                                        }

                                        Vector3 dependencyNormal = Vector3.zero;
                                        if (baseData.baseMeshNormals != null)
                                        {
                                            dependencyNormal = (baseData.baseMeshNormals[vertexInfo.closestIndex0] * vertexInfo.closestWeight0) + (baseData.baseMeshNormals[vertexInfo.closestIndex1] * vertexInfo.closestWeight1) + (baseData.baseMeshNormals[vertexInfo.closestIndex2] * vertexInfo.closestWeight2);
                                            dependencyNormal = dependencyNormal.normalized;
                                        }

                                        float dependencyWeight = 1f;
                                        if (vertexInfo.hasSecondaryBinding)
                                        {
                                            dependencyWeight = vertexInfo.closestWeight0 + vertexInfo.closestWeight1 + vertexInfo.closestWeight2;
                                        }
                                        for (int e = 0; e < shape.frames.Length; e++)
                                        {
                                            var frame = blendShape.frames[Mathf.Min(e, blendShape.frames.Length - 1)];
                                            var baseFrame = shape.frames[e];

                                            var deltaVertex = (baseFrame.deltaVertices[vertexInfo.closestIndex0] * vertexInfo.closestWeight0) + (baseFrame.deltaVertices[vertexInfo.closestIndex1] * vertexInfo.closestWeight1) + (baseFrame.deltaVertices[vertexInfo.closestIndex2] * vertexInfo.closestWeight2);
                                            var deltaNormal = (baseFrame.deltaNormals[vertexInfo.closestIndex0] * vertexInfo.closestWeight0) + (baseFrame.deltaNormals[vertexInfo.closestIndex1] * vertexInfo.closestWeight1) + (baseFrame.deltaNormals[vertexInfo.closestIndex2] * vertexInfo.closestWeight2);
                                            var deltaTangent = (baseFrame.deltaTangents[vertexInfo.closestIndex0] * vertexInfo.closestWeight0) + (baseFrame.deltaTangents[vertexInfo.closestIndex1] * vertexInfo.closestWeight1) + (baseFrame.deltaTangents[vertexInfo.closestIndex2] * vertexInfo.closestWeight2);

                                            deltaVertex = AddNormalBasedRotationToDelta(vertexInfo, localVertex, dependencyNormal, deltaVertex, deltaNormal, dependencyWeight);

                                            frame.deltaVertices[vIndex] = deltaVertex;
                                            frame.deltaNormals[vIndex] = deltaNormal;
                                            frame.deltaTangents[vIndex] = deltaTangent;
                                        }
                                    }
                                }
                            }

                            if (defaultVertexInfo.hasSecondaryBinding)
                            {
                                for (int d = 0; d < defaultBaseData2.baseMeshBlendShapes.Count; d++)
                                {
                                    var shape = defaultBaseData2.baseMeshBlendShapes[d];
                                    if (shape != null && (!settings.preserveExistingBlendShapeData || !IsOriginalBlendShape(shape.name)))
                                    {
                                        var blendShape = AddOrGetBlendShape(shape.name, shape.frames);
                                        if (blendShape != null)
                                        {
                                            var vertexInfo = defaultVertexInfo;
                                            var baseData = defaultBaseData2;

                                            if (perShapeVertexInfos.TryGetValue(blendShape.name, out vertexInfo) && vertexInfo.hasSecondaryBinding)
                                            {
                                                baseData = settings.GetBaseData(vertexInfo.closestSecondaryBaseIndex);

                                                if (baseData.TryGetBlendShape(shape.name, out var shape_))
                                                {
                                                    shape = shape_;
                                                }
                                                else
                                                {
                                                    vertexInfo = defaultVertexInfo;
                                                    baseData = defaultBaseData2;
                                                }
                                            }
                                            else
                                            {
                                                vertexInfo = defaultVertexInfo;
                                                baseData = defaultBaseData2;
                                            }

                                            Vector3 dependencyNormal = Vector3.zero;
                                            if (baseData.baseMeshNormals != null)
                                            {
                                                dependencyNormal = (baseData.baseMeshNormals[vertexInfo.closestSecondaryIndex0] * vertexInfo.closestSecondaryWeight0) + (baseData.baseMeshNormals[vertexInfo.closestSecondaryIndex1] * vertexInfo.closestSecondaryWeight1) + (baseData.baseMeshNormals[vertexInfo.closestSecondaryIndex2] * vertexInfo.closestSecondaryWeight2);
                                                dependencyNormal = dependencyNormal.normalized;
                                            }

                                            float dependencyWeight = vertexInfo.closestSecondaryWeight0 + vertexInfo.closestSecondaryWeight1 + vertexInfo.closestSecondaryWeight2;

                                            for (int e = 0; e < shape.frames.Length; e++)
                                            {
                                                var frame = blendShape.frames[Mathf.Min(e, blendShape.frames.Length - 1)];
                                                var baseFrame = shape.frames[e];

                                                var deltaVertex = (baseFrame.deltaVertices[vertexInfo.closestSecondaryIndex0] * vertexInfo.closestSecondaryWeight0) + (baseFrame.deltaVertices[vertexInfo.closestSecondaryIndex1] * vertexInfo.closestSecondaryWeight1) + (baseFrame.deltaVertices[vertexInfo.closestSecondaryIndex2] * vertexInfo.closestSecondaryWeight2);
                                                var deltaNormal = (baseFrame.deltaNormals[vertexInfo.closestSecondaryIndex0] * vertexInfo.closestSecondaryWeight0) + (baseFrame.deltaNormals[vertexInfo.closestSecondaryIndex1] * vertexInfo.closestSecondaryWeight1) + (baseFrame.deltaNormals[vertexInfo.closestSecondaryIndex2] * vertexInfo.closestSecondaryWeight2);
                                                var deltaTangent = (baseFrame.deltaTangents[vertexInfo.closestSecondaryIndex0] * vertexInfo.closestSecondaryWeight0) + (baseFrame.deltaTangents[vertexInfo.closestSecondaryIndex1] * vertexInfo.closestSecondaryWeight1) + (baseFrame.deltaTangents[vertexInfo.closestSecondaryIndex2] * vertexInfo.closestSecondaryWeight2);

                                                deltaVertex = AddNormalBasedRotationToDelta(vertexInfo, localVertex, dependencyNormal, deltaVertex, deltaNormal, dependencyWeight);

                                                frame.deltaVertices[vIndex] = frame.deltaVertices[vIndex] + deltaVertex;
                                                frame.deltaNormals[vIndex] = frame.deltaNormals[vIndex] + deltaNormal;
                                                frame.deltaTangents[vIndex] = frame.deltaTangents[vIndex] + deltaTangent;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    

                        if (settings.transferNormals)
                        {
                            var origNormal = localNormals[vIndex];
                            
                            localNormals[vIndex] = defaultBaseData.baseMeshNormals == null ? Vector3.zero : ((defaultBaseData.baseMeshNormals[defaultVertexInfo.closestIndex0] * defaultVertexInfo.closestWeight0) + (defaultBaseData.baseMeshNormals[defaultVertexInfo.closestIndex1] * defaultVertexInfo.closestWeight1) + (defaultBaseData.baseMeshNormals[defaultVertexInfo.closestIndex2] * defaultVertexInfo.closestWeight2));
                            if (defaultVertexInfo.hasSecondaryBinding && defaultBaseData2.baseMeshNormals != null)
                            {
                                localNormals[vIndex] = localNormals[vIndex] + ((defaultBaseData2.baseMeshNormals[defaultVertexInfo.closestSecondaryIndex0] * defaultVertexInfo.closestSecondaryWeight0) + (defaultBaseData2.baseMeshNormals[defaultVertexInfo.closestSecondaryIndex1] * defaultVertexInfo.closestSecondaryWeight1) + (defaultBaseData2.baseMeshNormals[defaultVertexInfo.closestSecondaryIndex2] * defaultVertexInfo.closestSecondaryWeight2));
                            }                                                     
                            localNormals[vIndex] = Vector3.LerpUnclamped(origNormal, localNormals[vIndex], settings.transferNormalsWeight).normalized;

                            tempFrameNormals.Clear();
                            for (int d = 0; d < defaultBaseData.baseMeshBlendShapes.Count; d++)
                            {
                                var shape = defaultBaseData.baseMeshBlendShapes[d];
                                if (shape != null)
                                {
                                    var blendShape = AddOrGetBlendShape(shape.name, shape.frames);
                                    if (blendShape != null)
                                    {
                                        var vertexInfo = defaultVertexInfo;
                                        var baseData = defaultBaseData;

                                        if (perShapeVertexInfos.TryGetValue(blendShape.name, out vertexInfo))
                                        {
                                            baseData = settings.GetBaseData(vertexInfo.closestBaseIndex);
                                            if (baseData.TryGetBlendShape(shape.name, out var shape_))
                                            {
                                                shape = shape_;
                                            }
                                            else
                                            {
                                                vertexInfo = defaultVertexInfo;
                                                baseData = defaultBaseData;
                                            }
                                        }
                                        else
                                        {
                                            vertexInfo = defaultVertexInfo;
                                            baseData = defaultBaseData;
                                        }

                                        for (int e = 0; e < blendShape.frames.Length; e++)
                                        {
                                            var frame = blendShape.frames[Mathf.Min(e, blendShape.frames.Length - 1)];
                                            var baseFrame = shape.frames[e];

                                            var deltaNormal = (baseFrame.deltaNormals[vertexInfo.closestIndex0] * vertexInfo.closestWeight0) + (baseFrame.deltaNormals[vertexInfo.closestIndex1] * vertexInfo.closestWeight1) + (baseFrame.deltaNormals[vertexInfo.closestIndex2] * vertexInfo.closestWeight2);

                                            //Vector3.LerpUnclamped(frame.deltaNormals[vIndex], deltaNormal, settings.transferNormalsWeight);

                                            //frame.deltaNormals[vIndex] = deltaNormal;
                                            tempFrameNormals[frame] = deltaNormal;
                                        }
                                    }
                                }
                            }
                            if (defaultVertexInfo.hasSecondaryBinding)
                            {
                                for (int d = 0; d < defaultBaseData2.baseMeshBlendShapes.Count; d++)
                                {
                                    var shape = defaultBaseData2.baseMeshBlendShapes[d];
                                    if (shape != null)
                                    {
                                        var blendShape = AddOrGetBlendShape(shape.name, shape.frames);
                                        if (blendShape != null)
                                        {
                                            var vertexInfo = defaultVertexInfo;
                                            var baseData = defaultBaseData2;

                                            if (perShapeVertexInfos.TryGetValue(blendShape.name, out vertexInfo) && vertexInfo.hasSecondaryBinding)
                                            {
                                                baseData = settings.GetBaseData(vertexInfo.closestSecondaryBaseIndex);
                                                if (baseData.TryGetBlendShape(shape.name, out var shape_))
                                                {
                                                    shape = shape_;
                                                }
                                                else
                                                {
                                                    vertexInfo = defaultVertexInfo;
                                                    baseData = defaultBaseData2;
                                                }
                                            }
                                            else
                                            {
                                                vertexInfo = defaultVertexInfo;
                                                baseData = defaultBaseData2;
                                            }

                                            for (int e = 0; e < blendShape.frames.Length; e++)
                                            {
                                                var frame = blendShape.frames[Mathf.Min(e, blendShape.frames.Length - 1)];
                                                var baseFrame = shape.frames[e];

                                                var deltaNormal = (baseFrame.deltaNormals[vertexInfo.closestSecondaryIndex0] * vertexInfo.closestSecondaryWeight0) + (baseFrame.deltaNormals[vertexInfo.closestSecondaryIndex1] * vertexInfo.closestSecondaryWeight1) + (baseFrame.deltaNormals[vertexInfo.closestSecondaryIndex2] * vertexInfo.closestSecondaryWeight2);

                                                tempFrameNormals.TryGetValue(frame, out var existingDeltaNormal);
                                                tempFrameNormals[frame] = existingDeltaNormal + deltaNormal;
                                            }
                                        }
                                    }
                                }
                            }

                            foreach(var entry in tempFrameNormals)
                            {
                                var frame = entry.Key;
                                frame.deltaNormals[vIndex] = Vector3.LerpUnclamped(frame.deltaNormals[vIndex], entry.Value, settings.transferNormalsWeight);
                            }
                        }

                        if (settings.transferUVs)
                        {
                            for (int a = 0; a < uvTransferCount; a++)
                            {
                                int baseIndex = a + settings.uvTransferRange.x;
                                if (baseIndex < 0) continue;

                                int localIndex = a + settings.uvChannelIndexTransferOffset;
                                if (localIndex < 0 || localIndex >= 8) continue;

                                var uvsLocal = localUVs[localIndex];
                                if ((settings.preserveExistingUVData && initialUVDataStates[localIndex]) || uvsLocal == null) continue;

                                var uvsBase = defaultBaseData.baseMeshUVs != null && baseIndex < defaultBaseData.baseMeshUVs.Count ? defaultBaseData.baseMeshUVs[baseIndex] : null;
                                uvsLocal[vIndex] = uvsBase == null ? Vector4.zero : ((uvsBase[defaultVertexInfo.closestIndex0] * defaultVertexInfo.closestWeight0) + (uvsBase[defaultVertexInfo.closestIndex1] * defaultVertexInfo.closestWeight1) + (uvsBase[defaultVertexInfo.closestIndex2] * defaultVertexInfo.closestWeight2));
                                
                                if (defaultVertexInfo.hasSecondaryBinding)
                                {
                                    uvsBase = defaultBaseData2.baseMeshUVs != null && baseIndex < defaultBaseData2.baseMeshUVs.Count ? defaultBaseData2.baseMeshUVs[baseIndex] : null;
                                    uvsLocal[vIndex] = uvsLocal[vIndex] + (uvsBase == null ? Vector4.zero : ((uvsBase[defaultVertexInfo.closestSecondaryIndex0] * defaultVertexInfo.closestSecondaryWeight0) + (uvsBase[defaultVertexInfo.closestSecondaryIndex1] * defaultVertexInfo.closestSecondaryWeight1) + (uvsBase[defaultVertexInfo.closestSecondaryIndex2] * defaultVertexInfo.closestSecondaryWeight2)));
                                }
                            }
                        }

                        if (settings.transferVertexColors)
                        {
                            localColors[vIndex] = defaultBaseData.baseMeshVertexColors == null ? Color.clear : ((defaultBaseData.baseMeshVertexColors[defaultVertexInfo.closestIndex0] * defaultVertexInfo.closestWeight0) + (defaultBaseData.baseMeshVertexColors[defaultVertexInfo.closestIndex1] * defaultVertexInfo.closestWeight1) + (defaultBaseData.baseMeshVertexColors[defaultVertexInfo.closestIndex2] * defaultVertexInfo.closestWeight2));
                        
                            if (defaultVertexInfo.hasSecondaryBinding && defaultBaseData2.baseMeshVertexColors != null)
                            {
                                localColors[vIndex] = localColors[vIndex] + ((defaultBaseData2.baseMeshVertexColors[defaultVertexInfo.closestSecondaryIndex0] * defaultVertexInfo.closestSecondaryWeight0) + (defaultBaseData2.baseMeshVertexColors[defaultVertexInfo.closestSecondaryIndex1] * defaultVertexInfo.closestSecondaryWeight1) + (defaultBaseData2.baseMeshVertexColors[defaultVertexInfo.closestSecondaryIndex2] * defaultVertexInfo.closestSecondaryWeight2));
                            }
                        }

                        if (settings.transferBoneWeights)
                        {
                            var boneWeights = defaultBaseData.baseMeshBoneWeights;
                            var bonesPerVertex = defaultBaseData.baseMeshBonesPerVertex;
                            var boneWeightStartIndices = defaultBaseData.baseMeshBoneWeightStartIndices;

                            if (boneWeights.IsCreated && bonesPerVertex.IsCreated && boneWeightStartIndices != null)
                            {
                                int startIndex = boneWeightStartIndices[defaultVertexInfo.closestIndex0];
                                int count = bonesPerVertex[defaultVertexInfo.closestIndex0];
                                for (int d = 0; d < count; d++)
                                {
                                    int index = startIndex + d;
                                    var bw = boneWeights[index];

                                    int2 i = new int2(vIndex, bw.boneIndex);

                                    newBoneWeights.TryGetValue(i, out float currentWeight);
                                    newBoneWeights[i] = currentWeight + (bw.weight * defaultVertexInfo.closestWeight0);
                                }

                                startIndex = boneWeightStartIndices[defaultVertexInfo.closestIndex1];
                                count = bonesPerVertex[defaultVertexInfo.closestIndex1];
                                for (int d = 0; d < count; d++)
                                {
                                    int index = startIndex + d;
                                    var bw = boneWeights[index];

                                    int2 i = new int2(vIndex, bw.boneIndex);

                                    newBoneWeights.TryGetValue(i, out float currentWeight);
                                    newBoneWeights[i] = currentWeight + (bw.weight * defaultVertexInfo.closestWeight1);
                                }

                                startIndex = boneWeightStartIndices[defaultVertexInfo.closestIndex2];
                                count = bonesPerVertex[defaultVertexInfo.closestIndex2];
                                for (int d = 0; d < count; d++)
                                {
                                    int index = startIndex + d;
                                    var bw = boneWeights[index];

                                    int2 i = new int2(vIndex, bw.boneIndex);

                                    newBoneWeights.TryGetValue(i, out float currentWeight);
                                    newBoneWeights[i] = currentWeight + (bw.weight * defaultVertexInfo.closestWeight2);
                                }
                            }

                            if (defaultVertexInfo.hasSecondaryBinding)
                            {
                                boneWeights = defaultBaseData2.baseMeshBoneWeights;
                                bonesPerVertex = defaultBaseData2.baseMeshBonesPerVertex;
                                boneWeightStartIndices = defaultBaseData2.baseMeshBoneWeightStartIndices;

                                if (boneWeights.IsCreated && bonesPerVertex.IsCreated && boneWeightStartIndices != null)
                                {
                                    int startIndex = boneWeightStartIndices[defaultVertexInfo.closestSecondaryIndex0];
                                    int count = bonesPerVertex[defaultVertexInfo.closestSecondaryIndex0];
                                    for (int d = 0; d < count; d++)
                                    {
                                        int index = startIndex + d;
                                        var bw = boneWeights[index];

                                        int2 i = new int2(vIndex, bw.boneIndex);

                                        newBoneWeights.TryGetValue(i, out float currentWeight);
                                        newBoneWeights[i] = currentWeight + (bw.weight * defaultVertexInfo.closestSecondaryWeight0);
                                    }

                                    startIndex = boneWeightStartIndices[defaultVertexInfo.closestSecondaryIndex1];
                                    count = bonesPerVertex[defaultVertexInfo.closestSecondaryIndex1];
                                    for (int d = 0; d < count; d++)
                                    {
                                        int index = startIndex + d;
                                        var bw = boneWeights[index];

                                        int2 i = new int2(vIndex, bw.boneIndex);

                                        newBoneWeights.TryGetValue(i, out float currentWeight);
                                        newBoneWeights[i] = currentWeight + (bw.weight * defaultVertexInfo.closestSecondaryWeight1);
                                    }

                                    startIndex = boneWeightStartIndices[defaultVertexInfo.closestSecondaryIndex2];
                                    count = bonesPerVertex[defaultVertexInfo.closestSecondaryIndex2];
                                    for (int d = 0; d < count; d++)
                                    {
                                        int index = startIndex + d;
                                        var bw = boneWeights[index];

                                        int2 i = new int2(vIndex, bw.boneIndex);

                                        newBoneWeights.TryGetValue(i, out float currentWeight);
                                        newBoneWeights[i] = currentWeight + (bw.weight * defaultVertexInfo.closestSecondaryWeight2);
                                    }
                                }
                            }
                        }
                    }
                }

                if (settings.transferBlendShapes || settings.transferNormals)
                {
                    mesh.ClearBlendShapes();
                    foreach (var shape in blendShapes)
                    {
                        shape.AddToMesh(mesh);
                    }
                }

                if (settings.transferNormals)
                {
                    mesh.normals = localNormals;
                }

                if (settings.transferVertexColors)
                {
                    mesh.colors = localColors;
                }

                if (settings.transferUVs)
                {
                    for (int a = 0; a < uvTransferCount; a++)
                    {
                        int localIndex = a + settings.uvChannelIndexTransferOffset;
                        if (localIndex < 0 || localIndex >= 8) continue;

                        var uvsLocal = localUVs[localIndex];
                        if ((settings.preserveExistingUVData && initialUVDataStates[localIndex]) || uvsLocal == null) continue;

                        mesh.SetUVs(localIndex, uvsLocal);
                    }
                }

                if (settings.transferBoneWeights && newBoneWeights.Count > 0)
                {
                    var boneWeights = new NativeList<BoneWeight1>(mesh.vertexCount, Allocator.Persistent);
                    var bonesPerVertex = new NativeArray<byte>(mesh.vertexCount, Allocator.Persistent);

                    List<BoneWeight1> vertexBoneWeights = new List<BoneWeight1>();
                    for (int c = 0; c < mesh.vertexCount; c++)
                    {
                        vertexBoneWeights.Clear();
                        foreach (var pair in newBoneWeights)
                        {
                            if (pair.Key.x == c)
                            {
                                var bw = new BoneWeight1();
                                bw.boneIndex = pair.Key.y;
                                bw.weight = pair.Value;

                                vertexBoneWeights.Add(bw);
                            }
                        }
                        vertexBoneWeights.Sort((BoneWeight1 a, BoneWeight1 b) => (int)Mathf.Sign(b.weight - a.weight));
                        if (vertexBoneWeights.Count > 8) vertexBoneWeights.RemoveRange(8, vertexBoneWeights.Count - 8);

                        float totalWeight = 0f;
                        foreach (var bw in vertexBoneWeights) totalWeight += bw.weight;
                        if (totalWeight > 0)
                        {
                            for (int d = 0; d < vertexBoneWeights.Count; d++)
                            {
                                var bw = vertexBoneWeights[d];
                                bw.weight /= totalWeight;
                                vertexBoneWeights[d] = bw;
                            }
                        }

                        if (vertexBoneWeights.Count <= 0) vertexBoneWeights.Add(new BoneWeight1() { boneIndex = 0, weight = 1f });

                        for (int d = 0; d < vertexBoneWeights.Count; d++)
                        {
                            var bw = vertexBoneWeights[d];
                            boneWeights.Add(bw);
                        }
                        bonesPerVertex[c] = (byte)vertexBoneWeights.Count; 
                    }

                    var baseMesh = settings.baseDataMain.baseMesh;
                    if (baseMesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.BlendWeight))
                    {
                        var attributes = new List<UnityEngine.Rendering.VertexAttributeDescriptor>(mesh.GetVertexAttributes());
                        bool hasBlendWeightAttribute = false;
                        for (int i = 0; i < attributes.Count; i++)
                        {
                            var attr = attributes[i];
                            if (attr.attribute != UnityEngine.Rendering.VertexAttribute.BlendWeight) continue;

                            attr.dimension = baseMesh.GetVertexAttributeDimension(UnityEngine.Rendering.VertexAttribute.BlendWeight);
                            attr.format = baseMesh.GetVertexAttributeFormat(UnityEngine.Rendering.VertexAttribute.BlendWeight);

                            attributes[i] = attr;

                            hasBlendWeightAttribute = true;
                        }
                        if (!hasBlendWeightAttribute)
                        {
                            var attr = new UnityEngine.Rendering.VertexAttributeDescriptor(UnityEngine.Rendering.VertexAttribute.BlendWeight, baseMesh.GetVertexAttributeFormat(UnityEngine.Rendering.VertexAttribute.BlendWeight), baseMesh.GetVertexAttributeDimension(UnityEngine.Rendering.VertexAttribute.BlendWeight));
                            attributes.Add(attr);
                        }
                        mesh.SetVertexBufferParams(mesh.vertexCount, attributes.ToArray()); 
                    }
                    mesh.SetBoneWeights(bonesPerVertex, boneWeights.AsArray());  

                    boneWeights.Dispose();
                    bonesPerVertex.Dispose(); 
                }

                return mesh;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 StoreIndexInUV(RGBAChannel nearestVertexIndexElement, Vector4 uv, int index)
        {
            switch (nearestVertexIndexElement)
            {
                case RGBAChannel.R:
                    uv.x = index;
                    break;
                case RGBAChannel.G:
                    uv.y = index;
                    break;
                case RGBAChannel.B:
                    uv.z = index;
                    break;
                case RGBAChannel.A:
                    uv.w = index;
                    break;
            }

            return uv;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FetchIndexFromUV(RGBAChannel nearestVertexIndexElement, Vector4 uv)
        {
            switch (nearestVertexIndexElement)
            {
                case RGBAChannel.R:
                    return (int)uv.x;
                case RGBAChannel.G:
                    return (int)uv.y;
                case RGBAChannel.B:
                    return (int)uv.z;
                case RGBAChannel.A:
                    return (int)uv.w;
            }

            return 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 StoreIndexInUV(RGBAChannel nearestVertexIndexElement, float4 uv, int index)
        {
            switch (nearestVertexIndexElement)
            {
                case RGBAChannel.R:
                    uv.x = index;
                    break;
                case RGBAChannel.G:
                    uv.y = index;
                    break;
                case RGBAChannel.B:
                    uv.z = index;
                    break;
                case RGBAChannel.A:
                    uv.w = index;
                    break;
            }

            return uv;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FetchIndexFromUV(RGBAChannel nearestVertexIndexElement, float4 uv)
        {
            switch (nearestVertexIndexElement)
            {
                case RGBAChannel.R:
                    return (int)uv.x;
                case RGBAChannel.G:
                    return (int)uv.y;
                case RGBAChannel.B:
                    return (int)uv.z;
                case RGBAChannel.A:
                    return (int)uv.w;
            }

            return 0;
        }

        #endregion

    }

    [NonAnimatable]
    public class BaseMeshData : IDisposable
    {
        public Mesh baseMesh;

        public int[] baseMeshTriangles = null;

        public Vector3[] baseMeshVertices = null;
        public Vector3[] baseMeshNormals = null;
        public Vector4[] baseMeshTangents = null;
        public Color[] baseMeshColors = null;
        public List<Vector4[]> baseMeshUVs = null;

        public NativeArray<BoneWeight1> baseMeshBoneWeights = default;
        public NativeArray<byte> baseMeshBonesPerVertex = default;
        public int[] baseMeshBoneWeightStartIndices = null;

        public List<BlendShape> baseMeshBlendShapes = new List<BlendShape>();

        public BaseMeshData() { }
        public BaseMeshData(Mesh baseMesh)
        {
            if (baseMesh != null) UpdateBaseMeshData(baseMesh);
        }

        public void UpdateBaseMeshData(Mesh baseMesh)
        {
            this.baseMesh = baseMesh;

            baseMeshTriangles = baseMesh.triangles;

            baseMeshVertices = baseMesh.vertices;
            baseMeshNormals = baseMesh.normals;
            baseMeshTangents = baseMesh.tangents;
            if (baseMeshTangents == null || baseMeshTangents.Length <= 0) baseMeshTangents = new Vector4[baseMesh.vertexCount];
            baseMeshColors = baseMesh.colors;
            baseMeshUVs = baseMesh.GetAllUVs(4, baseMeshUVs, true);

            if (baseMeshBoneWeights.IsCreated) baseMeshBoneWeights.Dispose();
            if (baseMeshBonesPerVertex.IsCreated) baseMeshBonesPerVertex.Dispose();

            baseMeshBoneWeights = new NativeArray<BoneWeight1>(baseMesh.GetAllBoneWeights(), Allocator.Persistent);
            baseMeshBonesPerVertex = new NativeArray<byte>(baseMesh.GetBonesPerVertex(), Allocator.Persistent);
            baseMeshBoneWeightStartIndices = MeshUtils.GetBoneWeightStartIndices(baseMeshBonesPerVertex, null);

            baseMeshBlendShapes.Clear();
            baseMeshBlendShapes = baseMesh.GetBlendShapes(baseMeshBlendShapes);
        }

        public void Dispose()
        {
            if (baseMeshBoneWeights.IsCreated)
            {
                baseMeshBoneWeights.Dispose();
                baseMeshBoneWeights = default;
            }

            if (baseMeshBonesPerVertex.IsCreated)
            {
                baseMeshBonesPerVertex.Dispose();
                baseMeshBonesPerVertex = default;
            }
        }

        public static implicit operator MorphUtils.TransferSurfaceDataBase(BaseMeshData baseMeshData)
        {
            var output = new MorphUtils.TransferSurfaceDataBase();

            output.baseMesh = baseMeshData.baseMesh;
            output.baseMeshTriangles = baseMeshData.baseMeshTriangles;
            output.baseMeshVertices = baseMeshData.baseMeshVertices;
            output.baseMeshNormals = baseMeshData.baseMeshNormals;
            output.baseMeshTangents = baseMeshData.baseMeshTangents;
            output.baseMeshUVs = baseMeshData.baseMeshUVs;
            output.baseMeshBlendShapes = baseMeshData.baseMeshBlendShapes;
            output.baseMeshBoneWeights = baseMeshData.baseMeshBoneWeights;
            output.baseMeshBonesPerVertex = baseMeshData.baseMeshBonesPerVertex;
            output.baseMeshBoneWeightStartIndices = baseMeshData.baseMeshBoneWeightStartIndices;

            return output;
        }
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

    [Serializable, StructLayout(LayoutKind.Sequential)]
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

    [Serializable, StructLayout(LayoutKind.Sequential)]
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

    [Serializable, NonAnimatable]
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

    [Serializable, NonAnimatable]
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
                Debug.Log($"Removed duplicate frame at weight {frameB.weight} for mesh shape {name}");
                meshFrames.RemoveAt(frameIndex + 1);
            }

            meshShape.name = name;
            meshShape.frames = meshFrames.ToArray();

            return meshShape;
        }

        [NonSerialized]
        private BlendShape blendShape;
        public BlendShape BlendShape
        {
            get
            {
                if (blendShape == null)
                {
                    blendShape = new BlendShape(name);
                    if (frames != null)
                    {
                        int vertexCount = frames.Length < 1 ? 0 : (frames[0].deltas == null ? 0 : frames[0].deltas.Length); 
                        foreach (var f in frames)
                        {
                            Vector3[] deltaVertices = new Vector3[vertexCount];
                            Vector3[] deltaNormals = new Vector3[vertexCount];
                            Vector3[] deltaTangents = new Vector3[vertexCount];

                            if (f.deltas != null)
                            {
                                for(int i = 0; i < Mathf.Min(vertexCount, f.deltas.Length); i++)
                                {
                                    var delta = f.deltas[i];

                                    deltaVertices[i] = delta.deltaVertex;
                                    deltaNormals[i] = delta.deltaNormal;
                                    deltaTangents[i] = delta.deltaTangent;
                                }
                            }

                            blendShape.AddFrame(f.weight, deltaVertices, deltaNormals, deltaTangents);
                        }
                    }
                }

                return blendShape;
            }
        }

    }
    [Serializable, NonAnimatable]
    public struct MeshShapeFrame
    {
        public float weight;

        [HideInInspector]
        public MorphShapeVertex[] deltas;
    }

    [Serializable, NonAnimatable]
    public class SkinningBlend
    {
        public string name;

        public bool animatable;

        public MeshShapeFrame[] frames;
    }
    [Serializable, NonAnimatable]
    public struct SkinningBlendFrame
    {
        public float weight;

        [HideInInspector]
        public SkinningBlendVertex[] deltas;
    }
    [Serializable, NonAnimatable]
    public struct BlendShapeMix
    {
        public string shapeName;
        public float weight;
    }
    [Serializable, NonAnimatable]
    public struct BlendShapeBase
    {
        public string shapeName;
        public bool recalculateNormals;
        public bool recalculateTangents;
    }
    [Serializable, NonAnimatable]
    public struct BlendShapeBaseMix
    {
        public string shapeName;
        public bool recalculateNormals;
        public bool recalculateTangents;
        public float weight;
    }
    [Serializable, NonAnimatable]
    public struct IdToMesh
    {
        public string[] ids;
        public Mesh mesh;
    }
    [Serializable, NonAnimatable]
    public struct BlendShapeTarget
    {
        public string newName;
        public string targetName;
        public string[] alternateTargetNames;
        public float frameWeightNormalizationThreshold;
        [Tooltip("Optional. Makes this shape relative to the base target shapes.")]
        public BlendShapeBase[] baseTargetNames;
        [Tooltip("Optional. Makes this shape relative to the mix of base target shapes.")]
        public BlendShapeBaseMix[] baseTargetMix;
        [Tooltip("Optional. Makes this shape relative to the base morphs.")]
        public string[] baseMorphNames;
        [Tooltip("Used by external systems for varyious functions")]
        public float weight;
        public bool animatable;
        public float deltaMultiplier;

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

        public Mesh meshToCopyNormalsFrom;
        public IdToMesh[] meshToCopyNormalsFromPerID;
        [Tooltip("Should the ids in the mesh copy ids array be used as the only ids that receive the copy? If not, the ids in the array are treated as exclusive.")]
        public bool meshCopyIdsArrayIsInclusive;
        public string[] meshCopyIds;

        [Tooltip("Ids of meshes or mesh groups that will recalculate the normals and tangents for this shape when it's fetched")]
        public string[] idsToRecalculateNormalsAndTangents;

        public bool TryGetBlendShape(string queryId, Mesh mesh, out BlendShape shape, Vector3[] vertices = null, Vector3[] normals = null, Vector4[] tangents = null, MeshDataTools.WeldedVertex[] mergedVertices = null, int[] triangles = null, UVChannelURP nearVertexUVchannel = UVChannelURP.UV0, Vector2[] nearVertexUVs = null)
        {
            return TryGetBlendShape(queryId, mesh, out shape, null, null, vertices, normals, tangents, mergedVertices, triangles, nearVertexUVchannel, nearVertexUVs);
        }
        public bool TryGetBlendShape(string queryId, Mesh mesh, out BlendShape shape, IEnumerable<MorphShape> morphShapes, Vector3[] vertices = null, Vector3[] normals = null, Vector4[] tangents = null, MeshDataTools.WeldedVertex[] mergedVertices = null, int[] triangles = null, UVChannelURP nearVertexUVchannel = UVChannelURP.UV0, Vector2[] nearVertexUVs = null)
        {
            return TryGetBlendShape(queryId, mesh, out shape, morphShapes, null, vertices, normals, tangents, mergedVertices, triangles, nearVertexUVchannel, nearVertexUVs);
        }
        public bool TryGetBlendShape(string queryId, Mesh mesh, out BlendShape shape, IEnumerable<MeshShape> meshShapes, Vector3[] vertices = null, Vector3[] normals = null, Vector4[] tangents = null, MeshDataTools.WeldedVertex[] mergedVertices = null, int[] triangles = null, UVChannelURP nearVertexUVchannel = UVChannelURP.UV0, Vector2[] nearVertexUVs = null)
        {
            return TryGetBlendShape(queryId, mesh, out shape, null, meshShapes, vertices, normals, tangents, mergedVertices, triangles, nearVertexUVchannel, nearVertexUVs);
        }
        public bool TryGetBlendShape(string queryId, Mesh mesh, out BlendShape shape, IEnumerable<MorphShape> morphShapes, IEnumerable<MeshShape> meshShapes, Vector3[] vertices = null, Vector3[] normals = null, Vector4[] tangents = null, MeshDataTools.WeldedVertex[] mergedVertices = null, int[] triangles = null, UVChannelURP nearVertexUVchannel = UVChannelURP.UV0, Vector2[] nearVertexUVs = null)
        {
            float deltaMultiplier = this.deltaMultiplier;
            if (deltaMultiplier == 0f) deltaMultiplier = 1f;

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

            if (shape != null && !string.IsNullOrWhiteSpace(newName)) shape.name = newName;

            if (deltaMultiplier != 1f)
            {
                if (shape != null && shape.frames != null)
                {
                    foreach(var frame in shape.frames)
                    {
                        for(int v = 0; v < mesh.vertexCount; v++)
                        {
                            frame.deltaVertices[v] = frame.deltaVertices[v] * deltaMultiplier;
                            frame.deltaNormals[v] = frame.deltaNormals[v] * deltaMultiplier;
                            frame.deltaTangents[v] = frame.deltaTangents[v] * deltaMultiplier;
                        }
                    }
                }
            }

            bool recalculateNorms = recalculateNormals;
            bool recalculateTans = recalculateTangents;
            if (idsToRecalculateNormalsAndTangents != null)
            {
                foreach (var id in idsToRecalculateNormalsAndTangents)
                {
                    if (id == queryId || id == mesh.name)
                    {
                        Debug.Log($"RECALCULATING NORMALS & TANGENTS FOR SHAPE {shape.name} ({id} == ({queryId}) or ({mesh.name}))");  
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

            if (meshToCopyNormalsFrom != null || (meshToCopyNormalsFromPerID != null && meshToCopyNormalsFromPerID.Length > 0))
            {
                bool include = meshCopyIds == null || meshCopyIds.Length <= 0 || !meshCopyIdsArrayIsInclusive;
                if (meshCopyIds != null && meshCopyIds.Length > 0)
                {
                    foreach(var id in meshCopyIds)
                    {
                        if (id == queryId || id == mesh.name)
                        {
                            include = meshCopyIdsArrayIsInclusive;
                            break;
                        }
                    }
                }

                if (include)
                {
                    var meshToCopyNormalsFrom_ = meshToCopyNormalsFrom;
                    if (meshToCopyNormalsFromPerID != null && meshToCopyNormalsFromPerID.Length > 0)
                    {
                        foreach (var copyMesh in meshToCopyNormalsFromPerID)
                        {
                            if (copyMesh.ids == null) continue;

                            bool flag = false;
                            foreach (var id in copyMesh.ids)
                            {
                                if (id == queryId || id == mesh.name)
                                {
                                    meshToCopyNormalsFrom_ = copyMesh.mesh;
                                    flag = true;
                                    break;
                                }
                            }

                            if (flag) break; 
                        }
                    }

                    if (meshToCopyNormalsFrom_ != null)
                    {
                        if (triangles == null || triangles.Length <= 0) triangles = mesh.triangles;
                        if (nearVertexUVs == null || nearVertexUVs.Length <= 0) nearVertexUVs = mesh.GetUVsByChannel(nearVertexUVchannel);

                        var targetTriangles = meshToCopyNormalsFrom_.triangles;
                        var targetNearVertexUVs = meshToCopyNormalsFrom_.GetUVsByChannel(nearVertexUVchannel);
                        var targetNormals = meshToCopyNormalsFrom_.normals;
                        var targetVertices = meshToCopyNormalsFrom_.vertices;

                        if (triangles != null && nearVertexUVs != null && targetTriangles != null && targetTriangles.Length > 0 && targetNearVertexUVs != null && targetNearVertexUVs.Length > 0 && targetNormals != null && targetNormals.Length > 0)
                        {
                            //for (int z = 0; z < nearVertexUVs.Length; z++) Debug.DrawRay(nearVertexUVs[z] + Vector2.right * 2f, Vector3.up * 0.1f, Color.blue, 100f); 
                            //for (int z = 0; z < targetNearVertexUVs.Length; z++) Debug.DrawRay(targetNearVertexUVs[z] - Vector2.right * 2f, Vector3.up * 0.1f, Color.cyan, 100f);  

                            var closestIndices = MeshDataTools.FindClosestVerticesUV(nearVertexUVs, targetNearVertexUVs, triangles, targetTriangles, null);
                            if (normals == null) normals = mesh.normals;
                            Vector3[] transferredNormals = (Vector3[])normals.Clone();
                            for (int i = 0; i < closestIndices.Length; i++)
                            {
                                transferredNormals[i] = targetNormals[closestIndices[i]];
                                //Debug.DrawLine(vertices[i], targetVertices[closestIndices[i]], Color.red, 60f); 
                            }

                            for (int a = 0; a < shape.frames.Length; a++)
                            {
                                var frame = shape.frames[a];

                                for (int b = 0; b < mesh.vertexCount; b++)
                                {
                                    frame.deltaNormals[b] = transferredNormals[b] - normals[b];
                                }
                            }
                        }
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

            if (frameWeightNormalizationThreshold != 0f && shape.frames != null)
            {
                foreach (var frame in shape.frames) frame.weight = frame.weight / frameWeightNormalizationThreshold;
            }

            return true;
        }
    }

    [Serializable]
    public struct MultiBlendShapeTarget
    {
        public string name;
        public bool animatable;

        public BlendShapeTarget[] targets;
    }

    [Serializable]
    public struct VertexColorDeltaTarget
    {
        public string name;
        [Tooltip("Optional material property to set to this delta's index")]
        public string indexPropertyName;
        public RGBAChannel targetChannels; 
        public Mesh targetMesh;

        public bool TryGet(Mesh localMesh, out VertexColorDelta delta, Color[] localVertexColors = null, int[] triangles = null, UVChannelURP nearVertexUVchannel = UVChannelURP.UV0, Vector2[] nearVertexUVs = null)
        {
            delta = null;

            if (localVertexColors == null) localVertexColors = localMesh.colors;
            if (localVertexColors == null || localVertexColors.Length <= 0) return false; 

            if (triangles == null || triangles.Length <= 0) triangles = localMesh.triangles;
            if (nearVertexUVs == null || nearVertexUVs.Length <= 0) nearVertexUVs = localMesh.GetUVsByChannel(nearVertexUVchannel); 

            var targetTriangles = targetMesh.triangles;
            var targetNearVertexUVs = targetMesh.GetUVsByChannel(nearVertexUVchannel);
            var targetColors = targetMesh.colors;

            if (triangles != null && nearVertexUVs != null && targetTriangles != null && targetTriangles.Length > 0 && targetNearVertexUVs != null && targetNearVertexUVs.Length > 0 && targetColors != null && targetColors.Length > 0)
            {
                var closestIndices = MeshDataTools.FindClosestVerticesUV(nearVertexUVs, targetNearVertexUVs, triangles, targetTriangles, null);
                Color[] deltaColors = (Color[])localVertexColors.Clone();
                for (int i = 0; i < closestIndices.Length; i++)
                {
                    var localC = localVertexColors[i];
                    var deltaC = targetColors[closestIndices[i]] - localC;

                    if (!targetChannels.HasFlag(RGBAChannel.R)) deltaC.r = 0f;
                    if (!targetChannels.HasFlag(RGBAChannel.G)) deltaC.g = 0f;
                    if (!targetChannels.HasFlag(RGBAChannel.B)) deltaC.b = 0f;
                    if (!targetChannels.HasFlag(RGBAChannel.A)) deltaC.a = 0f;

                    deltaColors[i] = deltaC;
                }

                delta = new VertexColorDelta()
                {
                    name = name,
                    indexPropertyName = indexPropertyName,
                    deltaColors = deltaColors
                };

                return true;
            }

            return false;
        }
    }

    [Serializable]
    public class VertexColorDelta
    {
        public string name;

        [Tooltip("Optional material property to set to this delta's index")]
        public string indexPropertyName;

        [HideInInspector]
        public Color[] deltaColors;
    }

    [Serializable]
    public struct NameFloat
    {
        public string name;
        public float value;
    }

    [Serializable]
    public struct NameProperty
    {
        public string name;
        public string property;
    }

    [Serializable]
    public struct MixedVertexGroupBoneWeight
    {
        public string boneName;
        public bool afterShapes;
    }

    [Serializable]
    public struct MixedVertexGroupBuilder
    {

        public string name;

        public float defaultWeight;

        public bool ignoreZeroWeights;

        public bool normalize;
        public float normalizeMaxWeight; 

        public bool clamp;
        public Vector2 clampRange;

        public bool invert;

        public MixedVertexGroupBoneWeight[] boneWeightsToAdd;
        public MixedVertexGroupBoneWeight[] boneWeightsToSubtract;
        public MixedVertexGroupBoneWeight[] boneWeightsToMultiply;
        public MixedVertexGroupBoneWeight[] boneWeightsToDivide;

        public BlendShapeTarget[] shapesToAdd;
        public BlendShapeTarget[] shapesToSubtract;
        public BlendShapeTarget[] shapesToMultiply;
        public BlendShapeTarget[] shapesToDivide;

        public VertexGroup Create(string queryId, Mesh mesh, Transform[] bones, IEnumerable<MorphShape> morphShapes, IEnumerable<MeshShape> meshShapes, Vector3[] vertices = null, Vector3[] normals = null, Vector4[] tangents = null, MeshDataTools.WeldedVertex[] mergedVertices = null, int[] triangles = null, UVChannelURP nearVertexUVchannel = UVChannelURP.UV0, Vector2[] nearVertexUVs = null)
        {
            string[] boneNames = bones == null ? null : bones.Select(b => b.name).ToArray();
            return Create(queryId, mesh, boneNames, morphShapes, meshShapes, vertices, normals, tangents, mergedVertices, triangles, nearVertexUVchannel, nearVertexUVs);
        }
        public VertexGroup Create(string queryId, Mesh mesh, string[] boneNames, IEnumerable<MorphShape> morphShapes, IEnumerable<MeshShape> meshShapes, Vector3[] vertices = null, Vector3[] normals = null, Vector4[] tangents = null, MeshDataTools.WeldedVertex[] mergedVertices = null, int[] triangles = null, UVChannelURP nearVertexUVchannel = UVChannelURP.UV0, Vector2[] nearVertexUVs = null)
        {

            if (mesh == null) return null;

            VertexGroup vertexGroup = null;

            int IndexOfBone(string boneName)
            {
                if (boneNames == null) return -1;
                for (int i = 0; i < boneNames.Length; i++)
                {
                    if (boneNames[i] == boneName) return i;
                }
                return -1;
            }

            bool hasBoneWeights = (boneWeightsToAdd != null && boneWeightsToAdd.Length > 0) || (boneWeightsToSubtract != null && boneWeightsToSubtract.Length > 0) || (boneWeightsToMultiply != null && boneWeightsToMultiply.Length > 0) || (boneWeightsToDivide != null && boneWeightsToDivide.Length > 0);

            NativeArray<BoneWeight1> boneWeights = default;
            NativeArray<byte> boneCounts = default;

            if (hasBoneWeights)
            {
                boneWeights = new NativeArray<BoneWeight1>( mesh.GetAllBoneWeights(), Allocator.Persistent);
                boneCounts = new NativeArray<byte>(mesh.GetBonesPerVertex(), Allocator.Persistent);
            }

            try
            {

                float[] weights = new float[mesh.vertexCount]; 

                #region Bone Weights Pre Shapes

                if (boneWeightsToAdd != null)
                {
                    foreach (var boneTarget in boneWeightsToAdd)
                    {
                        if (boneTarget.afterShapes || string.IsNullOrWhiteSpace(boneTarget.boneName)) continue;

                        int boneIndex = IndexOfBone(boneTarget.boneName);
                        if (boneIndex < 0) continue;

                        int boneWeightIndex = 0;
                        for (int i = 0; i < boneCounts.Length; i++)
                        {
                            var boneCount = boneCounts[i];
                            for (int j = 0; j < boneCount; j++)
                            {
                                if (boneWeights[boneWeightIndex].boneIndex == boneIndex)
                                {
                                    weights[i] = weights[i] + boneWeights[boneWeightIndex].weight;
                                }

                                boneWeightIndex++;
                            }
                        }
                    }
                }
                if (boneWeightsToSubtract != null)
                {
                    foreach (var boneTarget in boneWeightsToSubtract)
                    {
                        if (boneTarget.afterShapes || string.IsNullOrWhiteSpace(boneTarget.boneName)) continue;

                        int boneIndex = IndexOfBone(boneTarget.boneName);
                        if (boneIndex < 0) continue;

                        int boneWeightIndex = 0;
                        for (int i = 0; i < boneCounts.Length; i++)
                        {
                            var boneCount = boneCounts[i];
                            for (int j = 0; j < boneCount; j++)
                            {
                                if (boneWeights[boneWeightIndex].boneIndex == boneIndex)
                                {
                                    weights[i] = weights[i] - boneWeights[boneWeightIndex].weight;
                                }

                                boneWeightIndex++;
                            }
                        }
                    }
                }
                if (boneWeightsToMultiply != null)
                {
                    foreach (var boneTarget in boneWeightsToMultiply)
                    {
                        if (boneTarget.afterShapes || string.IsNullOrWhiteSpace(boneTarget.boneName)) continue;

                        int boneIndex = IndexOfBone(boneTarget.boneName);
                        if (boneIndex < 0) continue;

                        int boneWeightIndex = 0;
                        for (int i = 0; i < boneCounts.Length; i++)
                        {
                            var boneCount = boneCounts[i];
                            for (int j = 0; j < boneCount; j++)
                            {
                                if (boneWeights[boneWeightIndex].boneIndex == boneIndex)
                                {
                                    weights[i] = weights[i] * boneWeights[boneWeightIndex].weight;
                                }

                                boneWeightIndex++;
                            }
                        }
                    }
                }
                if (boneWeightsToDivide != null)
                {
                    foreach (var boneTarget in boneWeightsToDivide)
                    {
                        if (boneTarget.afterShapes || string.IsNullOrWhiteSpace(boneTarget.boneName)) continue;

                        int boneIndex = IndexOfBone(boneTarget.boneName);
                        if (boneIndex < 0) continue;

                        int boneWeightIndex = 0;
                        for (int i = 0; i < boneCounts.Length; i++)
                        {
                            var boneCount = boneCounts[i];
                            for (int j = 0; j < boneCount; j++)
                            {
                                if (boneWeights[boneWeightIndex].boneIndex == boneIndex)
                                {
                                    weights[i] = weights[i] / boneWeights[boneWeightIndex].weight;
                                }

                                boneWeightIndex++;
                            }
                        }
                    }
                }

                #endregion

                #region Shape Weights

                if (shapesToAdd != null)
                {
                    foreach (var shape in shapesToAdd)
                    {
                        if (shape.TryGetBlendShape(queryId, mesh, out var blendShape, morphShapes, meshShapes, vertices, normals, tangents, mergedVertices, triangles, nearVertexUVchannel, nearVertexUVs))
                        {
                            var shapeWeights = VertexGroup.ConvertToVertexWeightArray(blendShape, false, null, 0.0001f, 0f, false);
                            if (shapeWeights != null)
                            {
                                for (int i = 0; i < weights.Length; i++) weights[i] = weights[i] + shapeWeights[i];
                            }
                        }
                    }
                }
                if (shapesToSubtract != null)
                {
                    foreach (var shape in shapesToSubtract)
                    {
                        if (shape.TryGetBlendShape(queryId, mesh, out var blendShape, morphShapes, meshShapes, vertices, normals, tangents, mergedVertices, triangles, nearVertexUVchannel, nearVertexUVs))
                        {
                            var shapeWeights = VertexGroup.ConvertToVertexWeightArray(blendShape, false, null, 0.0001f, 0f, false);
                            if (shapeWeights != null)
                            {
                                for (int i = 0; i < weights.Length; i++) weights[i] = weights[i] - shapeWeights[i];
                            }
                        }
                    }
                }
                if (shapesToMultiply != null)
                {
                    foreach (var shape in shapesToMultiply)
                    {
                        if (shape.TryGetBlendShape(queryId, mesh, out var blendShape, morphShapes, meshShapes, vertices, normals, tangents, mergedVertices, triangles, nearVertexUVchannel, nearVertexUVs))
                        {
                            var shapeWeights = VertexGroup.ConvertToVertexWeightArray(blendShape, false, null, 0.0001f, 0f, false);
                            if (shapeWeights != null)
                            {
                                for (int i = 0; i < weights.Length; i++) weights[i] = weights[i] * shapeWeights[i];
                            }
                        }
                    }
                }
                if (shapesToDivide != null)
                {
                    foreach (var shape in shapesToDivide)
                    {
                        if (shape.TryGetBlendShape(queryId, mesh, out var blendShape, morphShapes, meshShapes, vertices, normals, tangents, mergedVertices, triangles, nearVertexUVchannel, nearVertexUVs))
                        {
                            var shapeWeights = VertexGroup.ConvertToVertexWeightArray(blendShape, false, null, 0.0001f, 0f, false);
                            if (shapeWeights != null)
                            {
                                for (int i = 0; i < weights.Length; i++) weights[i] = weights[i] / shapeWeights[i];
                            }
                        }
                    }
                }

                #endregion

                #region Bone Weights Post Shapes

                if (boneWeightsToAdd != null)
                {
                    foreach (var boneTarget in boneWeightsToAdd)
                    {
                        if (!boneTarget.afterShapes || string.IsNullOrWhiteSpace(boneTarget.boneName)) continue;

                        int boneIndex = IndexOfBone(boneTarget.boneName);
                        if (boneIndex < 0) continue;

                        int boneWeightIndex = 0;
                        for (int i = 0; i < boneCounts.Length; i++)
                        {
                            var boneCount = boneCounts[i];
                            for (int j = 0; j < boneCount; j++)
                            {
                                if (boneWeights[boneWeightIndex].boneIndex == boneIndex)
                                {
                                    weights[i] = weights[i] + boneWeights[boneWeightIndex].weight;
                                }

                                boneWeightIndex++;
                            }
                        }
                    }
                }
                if (boneWeightsToSubtract != null)
                {
                    foreach (var boneTarget in boneWeightsToSubtract)
                    {
                        if (!boneTarget.afterShapes || string.IsNullOrWhiteSpace(boneTarget.boneName)) continue;

                        int boneIndex = IndexOfBone(boneTarget.boneName);
                        if (boneIndex < 0) continue;

                        int boneWeightIndex = 0;
                        for (int i = 0; i < boneCounts.Length; i++)
                        {
                            var boneCount = boneCounts[i];
                            for (int j = 0; j < boneCount; j++)
                            {
                                if (boneWeights[boneWeightIndex].boneIndex == boneIndex)
                                {
                                    weights[i] = weights[i] - boneWeights[boneWeightIndex].weight;
                                }

                                boneWeightIndex++;
                            }
                        }
                    }
                }
                if (boneWeightsToMultiply != null)
                {
                    foreach (var boneTarget in boneWeightsToMultiply)
                    {
                        if (!boneTarget.afterShapes || string.IsNullOrWhiteSpace(boneTarget.boneName)) continue;

                        int boneIndex = IndexOfBone(boneTarget.boneName);
                        if (boneIndex < 0) continue;

                        int boneWeightIndex = 0;
                        for (int i = 0; i < boneCounts.Length; i++)
                        {
                            var boneCount = boneCounts[i];
                            for (int j = 0; j < boneCount; j++)
                            {
                                if (boneWeights[boneWeightIndex].boneIndex == boneIndex)
                                {
                                    weights[i] = weights[i] * boneWeights[boneWeightIndex].weight;
                                }

                                boneWeightIndex++;
                            }
                        }
                    }
                }
                if (boneWeightsToDivide != null)
                {
                    foreach (var boneTarget in boneWeightsToDivide)
                    {
                        if (!boneTarget.afterShapes || string.IsNullOrWhiteSpace(boneTarget.boneName)) continue;

                        int boneIndex = IndexOfBone(boneTarget.boneName);
                        if (boneIndex < 0) continue;

                        int boneWeightIndex = 0;
                        for (int i = 0; i < boneCounts.Length; i++)
                        {
                            var boneCount = boneCounts[i];
                            for (int j = 0; j < boneCount; j++)
                            {
                                if (boneWeights[boneWeightIndex].boneIndex == boneIndex)
                                {
                                    weights[i] = weights[i] / boneWeights[boneWeightIndex].weight;
                                }

                                boneWeightIndex++;
                            }
                        }
                    }
                }

                #endregion

                if (invert)
                {
                    for (int i = 0; i < weights.Length; i++) weights[i] = 1f - weights[i];
                }

                vertexGroup = new VertexGroup(name, weights, ignoreZeroWeights);

                if (normalize) vertexGroup.Normalize(normalizeMaxWeight);
                if (clamp) vertexGroup.Clamp(clampRange.x, (clampRange.x == 0f && clampRange.y == 0f) ? 1f : clampRange.y);

            } 
            finally
            {
                if (hasBoneWeights)
                {
                    if (boneWeights.IsCreated) boneWeights.Dispose();
                    if (boneCounts.IsCreated) boneCounts.Dispose();
                }
            }

            return vertexGroup;
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
        public BlendShapeTarget meshIslandRootsVertexGroup;
        public BlendShapeTarget meshIslandBlendVertexGroup;

        public bool mergeSeam;
        public string seamShape;
        public SeamMergeMethod seamMergeMethod;
        public bool mergeSeamTangents;
        public bool mergeSeamUVs;

        public bool allowSurfaceDataTransfer;

        public bool transferNormals;
        [Range(0, 1)]
        public float transferNormalsWeight;
        public bool transferVertexColors;
        public bool transferBoneWeights;

        public bool transferUVs;
        public bool preserveExistingUVData;
        public Vector2Int uvTransferRange;
        [Tooltip("UV Channel Index Offset to use when transferring UVs")]
        public int uvTransferOffset;

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