#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Unity.Collections;
using Unity.Mathematics;

namespace Swole
{

    public class SharedMeshData : SingletonBehaviour<SharedMeshData>
    {

        // Refrain from update calls
        public override bool ExecuteInStack => false;
        public override void OnUpdate() { }
        public override void OnLateUpdate() { }
        public override void OnFixedUpdate() { }
        //

        [Serializable]
        public struct BlendShapeVertex
        {

            public BlendShapeVertex(int vertexIndex, BlendShape.Frame blendShapeData)
            {

                data = new float3x3(
                    new float3(blendShapeData.deltaVertices[vertexIndex].x, blendShapeData.deltaVertices[vertexIndex].y, blendShapeData.deltaVertices[vertexIndex].z),
                    new float3(blendShapeData.deltaNormals[vertexIndex].x, blendShapeData.deltaNormals[vertexIndex].y, blendShapeData.deltaNormals[vertexIndex].z),
                    new float3(blendShapeData.deltaNormals[vertexIndex].x, blendShapeData.deltaNormals[vertexIndex].y, blendShapeData.deltaNormals[vertexIndex].z)
                    );

            }

            public BlendShapeVertex(int vertexIndex, BlendShape.Frame blendShapeData, BlendShape.Frame blendShapeRecalculatedData)
            {

                data = new float3x3(
                    new float3(blendShapeData.deltaVertices[vertexIndex].x, blendShapeData.deltaVertices[vertexIndex].y, blendShapeData.deltaVertices[vertexIndex].z),
                    new float3(blendShapeData.deltaNormals[vertexIndex].x, blendShapeData.deltaNormals[vertexIndex].y, blendShapeData.deltaNormals[vertexIndex].z),
                    new float3(blendShapeRecalculatedData.deltaNormals[vertexIndex].x, blendShapeRecalculatedData.deltaNormals[vertexIndex].y, blendShapeRecalculatedData.deltaNormals[vertexIndex].z)
                    );

            }

            public float3x3 data;

            public float3 DeltaVertex => data.c0;
            public float3 DeltaNormal => data.c1;
            public float3 DeltaSurfaceNormal => data.c2;

            public static BlendShapeVertex operator *(BlendShapeVertex bsv, float scalar) => new BlendShapeVertex() { data = bsv.data * scalar };
            public static BlendShapeVertex operator /(BlendShapeVertex bsv, float scalar) => new BlendShapeVertex() { data = bsv.data / scalar };

            public static BlendShapeVertex operator +(BlendShapeVertex bsvA, BlendShapeVertex bsvB) => new BlendShapeVertex() { data = bsvA.data + bsvB.data };
            public static BlendShapeVertex operator -(BlendShapeVertex bsvA, BlendShapeVertex bsvB) => new BlendShapeVertex() { data = bsvA.data - bsvB.data };

        }

        [Serializable]
        public class Cache : IDisposable
        {

            public Cache(Mesh mesh)
            {

                if (mesh == null) return;

                m_vertexCount = mesh.vertexCount;

                Vector3[] meshVertices = mesh.vertices;
                Vector3[] meshNormals = mesh.normals;

                var clones = MeshUtils.CalculateClones(meshVertices);

                Mesh temp = MeshUtils.DuplicateMesh(mesh);
                temp.RecalculateNormals();

                Vector3[] recalculatedMeshNormals = temp.normals;

                for (int a = 0; a < m_vertexCount; a++)
                {

                    var clone = clones[a];

                    if (clone.first != a) continue;

                    Vector3 normal = Vector3.zero;

                    for (int b = 0; b < clone.indices.Length; b++)
                    {

                        normal += recalculatedMeshNormals[clone.indices[b]];

                    }

                    normal = normal.normalized;

                    for (int b = 0; b < clone.indices.Length; b++)
                    {

                        recalculatedMeshNormals[clone.indices[b]] = normal;

                    }

                }

                m_vertices = new NativeArray<Vector3>(meshVertices, Allocator.Persistent).Reinterpret<float3>();
                m_normals = new NativeArray<Vector3>(meshNormals, Allocator.Persistent).Reinterpret<float3>();
                m_surfaceNormals = new NativeArray<Vector3>(recalculatedMeshNormals, Allocator.Persistent).Reinterpret<float3>();

                m_boneWeights = new NativeArray<BoneWeight1>(mesh.GetAllBoneWeights(), Allocator.Persistent);
                m_boneCounts = new NativeArray<byte>(mesh.GetBonesPerVertex(), Allocator.Persistent);

                List<BlendShape> blendShapes = mesh.GetBlendShapes();
                BlendShape[] recalculatedBlendShapes = MeshUtils.GetRecalculatedBlendShapeNormals(mesh, blendShapes, meshVertices, recalculatedMeshNormals, false, true, clones);

                m_blendShapeFrameCounts = new NativeArray<int>(blendShapes.Count, Allocator.Persistent);

                List<float> frameWeights = new List<float>();
                List<BlendShapeVertex> blendShapeData = new List<BlendShapeVertex>();

                for (int a = 0; a < blendShapes.Count; a++)
                {

                    var blendShape = blendShapes[a];

                    if (blendShape == null || blendShape.frames == null) continue;

                    var recalculatedShape = recalculatedBlendShapes[a];

                    m_blendShapeFrameCounts[a] = blendShape.frames.Length;

                    for (int b = 0; b < blendShape.frames.Length; b++)
                    {

                        var frame = blendShape.frames[b];

                        var recalculatedFrame = recalculatedShape == null ? frame : recalculatedShape.frames[b];

                        frameWeights.Add(frame.weight);

                        for (int c = 0; c < m_vertexCount; c++) blendShapeData.Add(new BlendShapeVertex(c, frame, recalculatedFrame));

                    }

                }

                m_blendShapeFrameWeights = new NativeArray<float>(frameWeights.ToArray(), Allocator.Persistent);
                m_blendShapeData = new NativeArray<BlendShapeVertex>(blendShapeData.ToArray(), Allocator.Persistent);

            }

            protected int m_vertexCount;
            public int VertexCount => m_vertexCount;

            protected NativeArray<float3> m_vertices;
            protected NativeArray<float3> m_normals;
            protected NativeArray<float3> m_surfaceNormals;

            protected NativeArray<BoneWeight1> m_boneWeights;
            protected NativeArray<byte> m_boneCounts;

            public int BlendShapeCount => m_blendShapeFrameCounts.Length;

            protected NativeArray<int> m_blendShapeFrameCounts;
            protected NativeArray<float> m_blendShapeFrameWeights;
            protected NativeArray<BlendShapeVertex> m_blendShapeData;

            public NativeArray<float3> Vertices => m_vertices;
            public NativeArray<float3> Normals => m_normals;
            public NativeArray<float3> SurfaceNormals => m_surfaceNormals;

            public NativeArray<BoneWeight1> BoneWeights => m_boneWeights;
            public NativeArray<byte> BoneCounts => m_boneCounts;

            public NativeArray<int> BlendShapeFrameCounts => m_blendShapeFrameCounts;
            public NativeArray<float> BlendShapeFrameWeights => m_blendShapeFrameWeights;
            public NativeArray<BlendShapeVertex> BlendShapeData => m_blendShapeData;

            public void Dispose()
            {

                if (m_vertices.IsCreated) m_vertices.Dispose();
                if (m_normals.IsCreated) m_normals.Dispose();
                if (m_surfaceNormals.IsCreated) m_surfaceNormals.Dispose();

                if (m_boneWeights.IsCreated) m_boneWeights.Dispose();
                if (m_boneCounts.IsCreated) m_boneCounts.Dispose();

                if (m_blendShapeFrameCounts.IsCreated) m_blendShapeFrameCounts.Dispose();
                if (m_blendShapeFrameWeights.IsCreated) m_blendShapeFrameWeights.Dispose();
                if (m_blendShapeData.IsCreated) m_blendShapeData.Dispose();

                m_vertices = default;
                m_normals = default;
                m_surfaceNormals = default;

                m_boneWeights = default;
                m_boneCounts = default;

                m_blendShapeFrameCounts = default;
                m_blendShapeFrameWeights = default;
                m_blendShapeData = default;

            }

        }

        protected Dictionary<int, Cache> m_Caches = new Dictionary<int, Cache>();

        public void ClearAll()
        {

            foreach (var data in m_Caches) if (data.Value != null) data.Value.Dispose();

            m_Caches.Clear();

        }

        public override void OnDestroyed()
        {

            base.OnDestroyed();

            ClearAll();

        }

        public static Cache GetData(Mesh mesh)
        {

            int instanceID = mesh.GetInstanceID();

            var singleton = Instance;

            if (!singleton.m_Caches.TryGetValue(instanceID, out Cache cache))
            {

                cache = new Cache(mesh);

                singleton.m_Caches[instanceID] = cache;

            }

            return cache;

        }

    }

}

#endif