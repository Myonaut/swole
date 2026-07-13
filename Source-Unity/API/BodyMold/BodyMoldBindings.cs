using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;

using Swole.Morphing;
using Swole.DataStructures;

namespace Swole.Modding
{
    [CreateAssetMenu(menuName = "Swole/Body Mold Bindings", fileName = "BodyMoldBindings")]
    public class BodyMoldBindings : ScriptableObject, IDisposable
    {
        [SerializeField]
        protected Mesh clothingMesh;
        public Mesh OriginalMesh => clothingMesh;
        public void SetOriginalMesh(Mesh mesh) => clothingMesh = mesh; 

        private Mesh editedClothingMesh;
        public Mesh EditedClothingMesh 
        {
            get
            {
                if (editedClothingMesh == null)
                {
                    if (eulerOffset != Vector3.zero)
                    {
                        editedClothingMesh = Instantiate(clothingMesh);
                        editedClothingMesh.name = clothingMesh.name + "_Edited";
                        editedClothingMesh.ClearBlendShapes();

                        var rotOffset = Quaternion.Euler(eulerOffset);
                        var vertices = editedClothingMesh.vertices;
                        var normals = editedClothingMesh.normals;
                        var tangents = editedClothingMesh.tangents;

                        bool hasNormals = normals != null && normals.Length == vertices.Length;
                        bool hasTangents = tangents != null && tangents.Length == vertices.Length;

                        for (int i = 0; i < vertices.Length; i++)
                        {
                            vertices[i] = rotOffset * vertices[i];

                            if (hasNormals)
                            {
                                normals[i] = Vector3.Normalize(rotOffset * normals[i]);
                            }

                            if (hasTangents)
                            {
                                var tangent = tangents[i];
                                var tangentVec3 = new Vector3(tangent.x, tangent.y, tangent.z);
                                tangentVec3 = Vector3.Normalize(rotOffset * tangentVec3);
                                tangents[i] = new Vector4(tangentVec3.x, tangentVec3.y, tangentVec3.z, tangent.w);
                            }
                        }

                        editedClothingMesh.vertices = vertices;

                        if (hasNormals)
                        {
                            editedClothingMesh.normals = normals;
                        }
                        else
                        {
                            editedClothingMesh.RecalculateNormals();
                        }

                        if (hasTangents)
                        {
                            editedClothingMesh.tangents = tangents;
                        }
                        else if (hasNormals)
                        {
                            editedClothingMesh.RecalculateTangents();
                        }

                        editedClothingMesh.UploadMeshData(false);
                    } 
                    else
                    {
                        return clothingMesh; 
                    }
                }

                return editedClothingMesh;
            }
        }

        public bool dynamicBoneWeights;
        public Vector3 eulerOffset; 

        public BodyMeshBinding[] perBodyBindings;

        [NonSerialized]
        protected BoneWeight8Float[] boneWeights;
        public bool BoneWeightsAreInitialized => boneWeights != null && boneWeights.Length > 0;
        public void InitializeBoneWeights()
        {
            if (BoneWeightsAreInitialized) return;

            boneWeights = new BoneWeight8Float[clothingMesh.vertexCount];

            var _boneCounts = clothingMesh.GetBonesPerVertex();
            var _boneWeights = clothingMesh.GetAllBoneWeights();
            int i = 0;
            for (int a = 0; a < _boneCounts.Length; a++)
            {
                int count = _boneCounts[a];

                BoneWeight8Float customBW = new BoneWeight8Float();

                for (int b = 0; b < math.min(8, count); b++)
                {

                    BoneWeight1 bw = _boneWeights[b + i];

                    customBW = customBW.Modify(b, bw.boneIndex, bw.weight); 

                }

                boneWeights[a] = customBW;

                i += count;
            }
            _boneCounts.Dispose();
            _boneWeights.Dispose();
        }
        [NonSerialized]
        private ComputeBuffer boneWeightsBuffer;
        public ComputeBuffer BoneWeightsBuffer
        {
            get
            {
                if (boneWeightsBuffer == null)
                {
                    InitializeBoneWeights();
                    TrackDisposables();
                    boneWeightsBuffer = new ComputeBuffer(boneWeights.Length, UnsafeUtility.SizeOf(typeof(BoneWeight8Float)), ComputeBufferType.Structured, ComputeBufferMode.Immutable);
                    boneWeightsBuffer.SetData(boneWeights);
                }

                return boneWeightsBuffer;
            }
        }

        [NonSerialized]
        private ComputeBuffer trianglesBuffer;
        public ComputeBuffer TrianglesBuffer
        {
            get
            {
                if (trianglesBuffer == null)
                {
                    TrackDisposables();
                    var triangles = clothingMesh.triangles;
                    trianglesBuffer = new ComputeBuffer(triangles.Length, UnsafeUtility.SizeOf(typeof(int)), ComputeBufferType.Structured, ComputeBufferMode.Immutable);
                    trianglesBuffer.SetData(triangles); 
                }

                return trianglesBuffer;
            }
        }

        [NonSerialized]
        private ComputeBuffer vertexColorsBuffer;
        public ComputeBuffer VertexColorsBuffer
        {
            get
            {
                if (vertexColorsBuffer == null)
                {
                    TrackDisposables();
                    var vColors = clothingMesh.colors;
                    if (vColors == null || vColors.Length <= 0)
                    {
                        vColors = new Color[clothingMesh.vertexCount];
                        for (int a = 0; a < vColors.Length; a++)
                        {
                            vColors[a] = Color.white;
                        }
                    }
                    vertexColorsBuffer = new ComputeBuffer(vColors.Length, UnsafeUtility.SizeOf(typeof(float4)), ComputeBufferType.Structured, ComputeBufferMode.Immutable);
                    vertexColorsBuffer.SetData(vColors); 
                }

                return vertexColorsBuffer;
            }
        }

        [HideInInspector]
        public MeshDataTools.WeightedVertexConnection[] vertexConnections;
        [HideInInspector]
        public int[] vertexConnectionCounts;
        [HideInInspector]
        public int[] vertexConnectionStartIndices;
        public bool VertexConnectionsAreInitialized => vertexConnections != null && vertexConnections.Length > 0;
        public void InitializeVertexConnections()
        {
            if (VertexConnectionsAreInitialized) return;

            var vertexConnections = new List<MeshDataTools.WeightedVertexConnection>();
            var vertexConnectionCounts = new List<int>();
            var vertexConnectionStartIndices = new List<int>();

            var vertices = clothingMesh.vertices;
            var arrays = MeshDataTools.GetDistanceWeightedVertexConnections(clothingMesh.triangles, vertices, MeshDataTools.WeldVertices(vertices));

            int vertexConnectionIndex = 0;
            for (int a = 0; a < arrays.Length; a++)
            {
                var connections = arrays[a];

                vertexConnectionStartIndices.Add(vertexConnectionIndex);
                int connectionCount = connections.Length;
                vertexConnectionCounts.Add(connectionCount);
                for (int b = 0; b < connections.Length; b++)
                {
                    var connection = connections[b];
                    vertexConnections.Add(connection);
                }

                vertexConnectionIndex = vertexConnectionIndex + connectionCount;
            }

            this.vertexConnections = vertexConnections.ToArray();
            this.vertexConnectionCounts = vertexConnectionCounts.ToArray();
            this.vertexConnectionStartIndices = vertexConnectionStartIndices.ToArray();
        }

        [NonSerialized]
        private ComputeBuffer vertexDataBuffer;
        public ComputeBuffer VertexDataBuffer
        {
            get
            {
                if (vertexDataBuffer == null)
                {
                    TrackDisposables();
                    var clothingMesh = EditedClothingMesh;
                    var vertices = clothingMesh.vertices;
                    var normals = clothingMesh.normals;
                    var tangents = clothingMesh.tangents;
                    var vertexData = new MeshVertexData[clothingMesh.vertexCount];
                    for(int a = 0; a < vertexData.Length; a++)
                    {
                        vertexData[a] = new MeshVertexData
                        {
                            position = vertices[a],
                            normal = normals == null || a >= normals.Length ? Vector3.zero : normals[a],
                            tangent = tangents == null || a >= tangents.Length ? Vector4.zero : tangents[a]
                        };
                    }
                    vertexDataBuffer = new ComputeBuffer(vertexData.Length, UnsafeUtility.SizeOf(typeof(MeshVertexData)), ComputeBufferType.Structured, ComputeBufferMode.Immutable); 
                    vertexDataBuffer.SetData(vertexData);
                }

                return vertexDataBuffer;
            }
        }


        [NonSerialized]
        private ComputeBuffer vertexConnectionsBuffer;
        [NonSerialized]
        private ComputeBuffer vertexConnectionCountsBuffer;
        [NonSerialized]
        private ComputeBuffer vertexConnectionStartIndicesBuffer;

        public ComputeBuffer VertexConnectionsBuffer
        {
            get
            {
                if (vertexConnectionsBuffer == null)
                {
                    TrackDisposables();
                    vertexConnectionsBuffer = new ComputeBuffer(vertexConnections.Length, UnsafeUtility.SizeOf(typeof(MeshDataTools.WeightedVertexConnection)), ComputeBufferType.Structured, ComputeBufferMode.Immutable);
                    vertexConnectionsBuffer.SetData(vertexConnections);
                }

                return vertexConnectionsBuffer;
            }
        }

        public ComputeBuffer VertexConnectionCountsBuffer
        {
            get
            {
                if (vertexConnectionCountsBuffer == null)
                {
                    TrackDisposables();
                    vertexConnectionCountsBuffer = new ComputeBuffer(vertexConnectionCounts.Length, UnsafeUtility.SizeOf(typeof(int)), ComputeBufferType.Structured, ComputeBufferMode.Immutable);
                    vertexConnectionCountsBuffer.SetData(vertexConnectionCounts);
                }

                return vertexConnectionCountsBuffer;
            }
        }

        public ComputeBuffer VertexConnectionStartIndicesBuffer
        {
            get
            {
                if (vertexConnectionStartIndicesBuffer == null)
                {
                    TrackDisposables();
                    vertexConnectionStartIndicesBuffer = new ComputeBuffer(vertexConnectionStartIndices.Length, UnsafeUtility.SizeOf(typeof(int)), ComputeBufferType.Structured, ComputeBufferMode.Immutable);
                    vertexConnectionStartIndicesBuffer.SetData(vertexConnectionStartIndices);
                }

                return vertexConnectionStartIndicesBuffer;
            }
        }

        [NonSerialized]
        private bool trackingDisposables = false;
        public void TrackDisposables()
        {
            if (trackingDisposables) return;

            trackingDisposables = true;
            PersistentJobDataTracker.Track(this);
        }
        public void UntrackDisposables()
        {
            if (!trackingDisposables) return;

            trackingDisposables = false;
            PersistentJobDataTracker.Untrack(this);
        }

        public void Dispose()
        {
            if (boneWeightsBuffer != null)
            {
                boneWeightsBuffer.Dispose();
                boneWeightsBuffer = null;
            }

            if (trianglesBuffer != null)
            {
                trianglesBuffer.Dispose();
                trianglesBuffer = null; 
            }

            if (vertexColorsBuffer != null)
            {
                vertexColorsBuffer.Dispose();
                vertexColorsBuffer = null; 
            }

            if (vertexDataBuffer != null)
            {
                vertexDataBuffer.Dispose();
                vertexDataBuffer = null;
            }         

            if (vertexConnectionsBuffer != null)
            {
                vertexConnectionsBuffer.Dispose();
                vertexConnectionsBuffer = null;
            }

            if (vertexConnectionCountsBuffer != null)
            {
                vertexConnectionCountsBuffer.Dispose();
                vertexConnectionCountsBuffer = null;
            }

            if (vertexConnectionStartIndicesBuffer != null)
            {
                vertexConnectionStartIndicesBuffer.Dispose();
                vertexConnectionStartIndicesBuffer = null;
            }

            if (perBodyBindings != null)
            {
                foreach (var binding in perBodyBindings) binding.Dispose();
            }
        }

        [Serializable]
        public class BodyMeshBinding : IDisposable
        {

            public Mesh mesh;
            public string fallbackMeshName;
            public string FallbackMeshName => string.IsNullOrWhiteSpace(fallbackMeshName) ? (mesh == null ? string.Empty : mesh.name) : fallbackMeshName;

            //[HideInInspector]
            public int[] vertexBindingLocalIndices;
            [HideInInspector]
            public int4[] vertexBindingIndices;
            [HideInInspector]
            public float4[] vertexBindingWeights;
            [HideInInspector]
            public Triangles32[] collisionTriangles;
            [HideInInspector]
            public PushBackVertex[] pushBackVertices;
            [HideInInspector]
            public MaskedVertex[] maskedVertices;


            public int BindingMaskLength => vertexBindingLocalIndices != null ? vertexBindingIndices.Length : 0;

            public int PushBackVerticesCount => pushBackVertices != null ? pushBackVertices.Length : 0;
            public bool HasPushBackVertices => PushBackVerticesCount > 0;

            public int MaskedVerticesCount => maskedVertices != null ? maskedVertices.Length : 0;
            public bool HasMaskedVertices => MaskedVerticesCount > 0;

            [NonSerialized]
            private bool trackingDisposables = false; 
            public void TrackDisposables()
            {
                if (trackingDisposables) return;

                trackingDisposables = true;
                PersistentJobDataTracker.Track(this);
            }
            public void UntrackDisposables()
            {
                if (!trackingDisposables) return;

                trackingDisposables = false;
                PersistentJobDataTracker.Untrack(this);
            }

            [NonSerialized]
            private ComputeBuffer localIndicesBuffer;
            [NonSerialized]
            private ComputeBuffer indicesBuffer;
            [NonSerialized]
            private ComputeBuffer weightsBuffer;
            [NonSerialized]
            private ComputeBuffer collisionTrianglesBuffer;
            [NonSerialized]
            private ComputeBuffer pushBackVerticesBuffer;
            [NonSerialized]
            private ComputeBuffer maskedVerticesBuffer;

            public ComputeBuffer LocalIndicesBuffer
            {
                get
                {
                    if (localIndicesBuffer == null)
                    {
                        TrackDisposables();
                        localIndicesBuffer = new ComputeBuffer(vertexBindingLocalIndices.Length, UnsafeUtility.SizeOf(typeof(int)), ComputeBufferType.Structured, ComputeBufferMode.Immutable);
                        localIndicesBuffer.SetData(vertexBindingLocalIndices);
                    }

                    return localIndicesBuffer;
                }
            }

            public ComputeBuffer IndicesBuffer
            {
                get
                {
                    if (indicesBuffer == null)
                    {
                        TrackDisposables();
                        indicesBuffer = new ComputeBuffer(vertexBindingIndices.Length, UnsafeUtility.SizeOf(typeof(int4)), ComputeBufferType.Structured, ComputeBufferMode.Immutable);
                        indicesBuffer.SetData(vertexBindingIndices);
                    }

                    return indicesBuffer;
                }
            }

            public ComputeBuffer WeightsBuffer
            {
                get
                {
                    if (weightsBuffer == null)
                    {
                        TrackDisposables();
                        weightsBuffer = new ComputeBuffer(vertexBindingWeights.Length, UnsafeUtility.SizeOf(typeof(float4)), ComputeBufferType.Structured, ComputeBufferMode.Immutable);
                        weightsBuffer.SetData(vertexBindingWeights);
                    }

                    return weightsBuffer;
                }
            }

            public ComputeBuffer CollisionTrianglesBuffer
            {
                get
                {
                    if (collisionTrianglesBuffer == null)
                    {
                        TrackDisposables();
                        collisionTrianglesBuffer = new ComputeBuffer(collisionTriangles.Length, UnsafeUtility.SizeOf(typeof(Triangles32)), ComputeBufferType.Structured, ComputeBufferMode.Immutable);
                        collisionTrianglesBuffer.SetData(collisionTriangles); 
                    }

                    return collisionTrianglesBuffer; 
                }
            }

            public ComputeBuffer PushBackVerticesBuffer
            {
                get
                {
                    if (pushBackVerticesBuffer == null)
                    {
                        TrackDisposables();
                        pushBackVerticesBuffer = new ComputeBuffer(pushBackVertices.Length, UnsafeUtility.SizeOf(typeof(PushBackVertex)), ComputeBufferType.Structured, ComputeBufferMode.Immutable);
                        pushBackVerticesBuffer.SetData(pushBackVertices);
                    }

                    return pushBackVerticesBuffer;
                }
            }

            public ComputeBuffer MaskedVerticesBuffer
            {
                get
                {
                    if (maskedVerticesBuffer == null)
                    {
                        TrackDisposables();
                        maskedVerticesBuffer = new ComputeBuffer(maskedVertices.Length, UnsafeUtility.SizeOf(typeof(MaskedVertex)), ComputeBufferType.Structured, ComputeBufferMode.Immutable);
                        maskedVerticesBuffer.SetData(maskedVertices);
                    }

                    return maskedVerticesBuffer;
                }
            }

            public void Dispose()
            {
                if (localIndicesBuffer != null)
                {
                    localIndicesBuffer.Dispose();
                    localIndicesBuffer = null;
                }

                if (indicesBuffer != null)
                {
                    indicesBuffer.Dispose();
                    indicesBuffer = null;
                }

                if (weightsBuffer != null)
                {
                    weightsBuffer.Dispose();
                    weightsBuffer = null;
                }

                if (collisionTrianglesBuffer != null)
                {
                    collisionTrianglesBuffer.Dispose();
                    collisionTrianglesBuffer = null;
                }

                if (pushBackVerticesBuffer != null)
                {
                    pushBackVerticesBuffer.Dispose();
                    pushBackVerticesBuffer = null;
                }

                if (maskedVerticesBuffer != null)
                {
                    maskedVerticesBuffer.Dispose();
                    maskedVerticesBuffer = null;
                }
            }

        }
    }
}
