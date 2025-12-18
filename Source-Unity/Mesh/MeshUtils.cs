#if (UNITY_STANDALONE || UNITYEDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Unity.Collections;

using Swole.DataStructures;

namespace Swole
{

    public static class MeshUtils
    {

        public static Mesh DuplicateMesh(this Mesh inputMesh)
        {

            Mesh mesh = UnityEngine.Object.Instantiate(inputMesh);

            return mesh;

        }

        public static List<Vector2> GetUVsByChannelAsList(this Mesh mesh, UVChannelURP uvChannel) => GetUVsByChannelAsList(mesh, (int)uvChannel);
        public static List<Vector2> GetUVsByChannelAsList(this Mesh mesh, int uvChannel)
        {

            List<Vector2> uv = new List<Vector2>();

            mesh.GetUVs(uvChannel, uv);

            return uv;

        }

        public static Vector2[] GetUVsByChannel(this Mesh mesh, UVChannelURP uvChannel) => GetUVsByChannel(mesh, (int)uvChannel);
        public static Vector2[] GetUVsByChannel(this Mesh mesh, int uvChannel)
        {

            return GetUVsByChannelAsList(mesh, uvChannel).ToArray();

        }

        public static List<Vector3> GetUVsByChannelAsListV3(this Mesh mesh, UVChannelURP uvChannel) => GetUVsByChannelAsListV3(mesh, (int)uvChannel);
        public static List<Vector3> GetUVsByChannelAsListV3(this Mesh mesh, int uvChannel)
        {

            List<Vector3> uv = new List<Vector3>();

            mesh.GetUVs(uvChannel, uv);

            return uv;

        }

        public static Vector3[] GetUVsByChannelV3(this Mesh mesh, UVChannelURP uvChannel) => GetUVsByChannelV3(mesh, (int)uvChannel);
        public static Vector3[] GetUVsByChannelV3(this Mesh mesh, int uvChannel)
        {

            return GetUVsByChannelAsListV3(mesh, uvChannel).ToArray();

        }

        public static List<Vector4> GetUVsByChannelAsListV4(this Mesh mesh, UVChannelURP uvChannel) => GetUVsByChannelAsListV4(mesh, (int)uvChannel);
        public static List<Vector4> GetUVsByChannelAsListV4(this Mesh mesh, int uvChannel)
        {

            List<Vector4> uv = new List<Vector4>();

            mesh.GetUVs(uvChannel, uv);

            return uv;

        }

        public static Vector4[] GetUVsByChannelV4(this Mesh mesh, UVChannelURP uvChannel) => GetUVsByChannelV4(mesh, (int)uvChannel);
        public static Vector4[] GetUVsByChannelV4(this Mesh mesh, int uvChannel)
        {

            return GetUVsByChannelAsListV4(mesh, uvChannel).ToArray();

        }

        /// <summary>
        /// For UV channels that have no data, a null reference will be stored at that index in the output list
        /// </summary>
        public static List<Vector4[]> GetAllUVs(this Mesh mesh, int count = 4, List<Vector4[]> outputList = null, bool clearList = true)
        {
            if (outputList == null) outputList = new List<Vector4[]>();
            if (clearList) outputList.Clear();

            if (count > 8) count = 8;
            for (int a = 0; a < count; a++)
            {
                if (!clearList && a < outputList.Count)
                {
                    outputList[a] = mesh.HasVertexAttribute((UnityEngine.Rendering.VertexAttribute)(4 + a)) ? mesh.GetUVsByChannelV4(a) : null;
                }
                else
                {
                    outputList.Add(mesh.HasVertexAttribute((UnityEngine.Rendering.VertexAttribute)(4 + a)) ? mesh.GetUVsByChannelV4(a) : null);
                }
            }

            return outputList;
        }

        public static VertexClone[] CalculateClones(NativeArray<Vector3> vertices, float mergeThreshold = 0.00001f)
        {

            return CalculateClones(vertices.ToArray(), mergeThreshold);

        }

        public static VertexClone[] CalculateClones(Vector3[] vertices, float mergeThreshold = 0.00001f)
        {

            Dictionary<float, Dictionary<float, Dictionary<float, VertexClone>>> sibling_codex = new Dictionary<float, Dictionary<float, Dictionary<float, VertexClone>>>();

            VertexClone[] clones = new VertexClone[vertices.Length];

            double merge = 1D / mergeThreshold;

            for (int a = 0; a < vertices.Length; a++)
            {

                Vector3 pos = vertices[a];

                float px = (float)(System.Math.Truncate((double)pos.x * merge) / merge);
                float py = (float)(System.Math.Truncate((double)pos.y * merge) / merge);
                float pz = (float)(System.Math.Truncate((double)pos.z * merge) / merge);

                Dictionary<float, Dictionary<float, VertexClone>> layer1;

                if (!sibling_codex.TryGetValue(px, out layer1))
                {

                    layer1 = new Dictionary<float, Dictionary<float, VertexClone>>();

                    sibling_codex[px] = layer1;

                }

                Dictionary<float, VertexClone> layer2;

                if (!layer1.TryGetValue(py, out layer2))
                {

                    layer2 = new Dictionary<float, VertexClone>();

                    layer1[py] = layer2;

                }

                if (!layer2.TryGetValue(pz, out VertexClone clone))
                {

                    clone = new VertexClone(a, new int[0]);

                    layer2[pz] = clone;

                }

                clone.indices = (int[])clone.indices.Add(a);

                for (int b = 0; b < clone.indices.Length; b++) clones[clone.indices[b]] = clone;

                layer2[pz] = clone;

            }

            return clones;

        }

        public static BlendShape[] GetRecalculatedBlendShapeNormals(Mesh mesh, ICollection<BlendShape> blendShapes, Vector3[] vertices = null, Vector3[] normals = null, bool recalculateMeshNormalsForShapeBaseNormals = false, bool mergeNormals = true, VertexClone[] clones = null, bool cloneShape = true)
        {

            if (blendShapes == null) return null;

            if (vertices == null) vertices = mesh.vertices;
            if (normals == null) normals = mesh.normals;

            if (clones == null && mergeNormals) clones = CalculateClones(vertices);

            if (recalculateMeshNormalsForShapeBaseNormals)
            {

                Mesh m = DuplicateMesh(mesh);

                m.RecalculateNormals();

                normals = m.normals;

                if (mergeNormals)
                {

                    for (int a = 0; a < normals.Length; a++)
                    {

                        var clone = clones[a];

                        if (a != clone.first) continue;

                        Vector3 normal = Vector3.zero;

                        for (int b = 0; b < clone.indices.Length; b++) normal += normals[clone.indices[b]];

                        normal = normal.normalized;

                        for (int b = 0; b < clone.indices.Length; b++) normals[clone.indices[b]] = normal;

                    }

                }

            }

            BlendShape[] outputShapes = new BlendShape[blendShapes.Count];

            int i = 0;
            foreach (BlendShape shape in blendShapes)
            {

                outputShapes[i] = GetRecalculatedBlendShapeNormals(mesh, shape, vertices, normals, false, mergeNormals, clones, cloneShape);

                i++;

            }

            return outputShapes;

        }

        public static BlendShape GetRecalculatedBlendShapeNormals(Mesh mesh, BlendShape blendShape, Vector3[] vertices = null, Vector3[] normals = null, bool recalculateMeshNormalsForShapeBaseNormals = false, bool mergeNormals = true, VertexClone[] clones = null, bool cloneShape = true)
        {

            if (vertices == null) vertices = mesh.vertices;
            if (normals == null) normals = mesh.normals;

            if (clones == null && mergeNormals) clones = CalculateClones(vertices);

            if (recalculateMeshNormalsForShapeBaseNormals)
            {

                Mesh m = DuplicateMesh(mesh);

                m.RecalculateNormals();

                normals = m.normals;

                if (mergeNormals)
                {

                    for (int a = 0; a < normals.Length; a++)
                    {

                        normals[a] = normals[clones[a].first];

                    }

                }

            }

            if (cloneShape) blendShape = blendShape.Duplicate();

            for (int b = 0; b < blendShape.frames.Length; b++)
            {

                BlendShape.Frame frame = blendShape.frames[b];

                Mesh m = DuplicateMesh(mesh);

                Vector3[] tempV = m.vertices;
                float[] weights = new float[tempV.Length];

                for (int c = 0; c < tempV.Length; c++)
                {

                    Vector3 delta = frame.deltaVertices.GetDelta(c);
                    tempV[c] = tempV[c] + delta;

                    weights[c] = Mathf.Clamp01(delta.magnitude / 0.025f);

                }

                m.vertices = tempV;

                m.RecalculateNormals();

                Vector3[] tempN = m.normals;

                for (int c = 0; c < tempN.Length; c++)
                {

                    int d = mergeNormals ? clones[c].first : c;

                    frame.deltaNormals.SetDelta(c, (tempN[d] - normals[d]) * weights[d]);

                }

            }

            return blendShape;

        }

        public static int[] GetBoneWeightStartIndices(NativeArray<byte> bonesPerVertex, int[] array = null)
        {
            if (bonesPerVertex.IsCreated)
            {
                int vertexCount = bonesPerVertex.Length;
                if (array == null) array = new int[vertexCount];

                int i = 0;
                for (int c = 0; c < vertexCount; c++)
                {
                    int count = bonesPerVertex[c];
                    array[c] = i;
                    i += count;
                }
            }

            return array;
        }

    }



    [Serializable]
    public struct MeshLOD
    {
        public Mesh mesh;
        public float screenRelativeTransitionHeight;
    }

}

#endif
