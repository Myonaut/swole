using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;

using Swole.Morphing;
using Swole.DataStructures;

namespace Swole.API.Unity
{
    [CreateAssetMenu(menuName = "Swole/Body Mold Bindings", fileName = "BodyMoldBindings")]
    public class BodyMoldBindings : ScriptableObject, IDisposable
    {
        [Serializable]
        public class GeneratedMeshLOD
        {
#if !UNITY_EDITOR
            [NonSerialized]
#endif
            public Mesh originalMesh;

            public MeshLOD meshLod;

#if UNITY_EDITOR
            [SerializeField]
            protected string originalMeshHash; 
#endif

#if UNITY_EDITOR
            public void RegenerateMesh(GeneratedMeshLOD highestDetailMesh, BodyMoldBindings bindings)
            {
                if (originalMesh == null) return; 

                meshLod.mesh = Instantiate(originalMesh);
                meshLod.mesh.name = originalMesh.name + "_MoldEdited";

                var rotOffset = Quaternion.Euler(bindings.eulerOffset);
                var vertices = meshLod.mesh.vertices;
                var normals = meshLod.mesh.normals;
                var tangents = meshLod.mesh.tangents;
                var colors = meshLod.mesh.colors;
                if (colors == null || colors.Length != vertices.Length)
                {
                    colors = new Color[vertices.Length];
                    for (int a = 0; a < colors.Length; a++)
                    {
                        colors[a] = Color.white;
                    }
                }

                if (ReferenceEquals(highestDetailMesh, this))
                {
                    switch (bindings.vertexIndexChannel)
                    {
                        case RGBAChannel.R:
                            {
                                for (int i = 0; i < colors.Length; i++)
                                {
                                    var c = colors[i];
                                    c.r = i;
                                    colors[i] = c; 
                                }
                            }
                            break;

                        case RGBAChannel.G:
                            {
                                for (int i = 0; i < colors.Length; i++)
                                {
                                    var c = colors[i];
                                    c.g = i;
                                    colors[i] = c;
                                }
                            }
                            break;

                        case RGBAChannel.B:
                            {
                                for (int i = 0; i < colors.Length; i++)
                                {
                                    var c = colors[i];
                                    c.b = i;
                                    colors[i] = c;
                                }
                            }
                            break;

                        case RGBAChannel.A:
                            {
                                for (int i = 0; i < colors.Length; i++)
                                {
                                    var c = colors[i];
                                    c.a = i;
                                    colors[i] = c;
                                }
                            }
                            break;
                    }
                }
                else
                {
                    int[] nearestVertices = new int[meshLod.mesh.vertexCount];
                    if (bindings.useUVsToFindClosestVertex)
                    {
                        var baseUV = highestDetailMesh.originalMesh.GetUVsByChannelAsList((int)bindings.nearestVertexUVChannel);
                        var lodUV = meshLod.mesh.GetUVsByChannelAsList((int)bindings.nearestVertexUVChannel);
                        MeshDataTools.FindClosestVerticesUV(lodUV, baseUV, nearestVertices);
                    }
                    else
                    {
                        var baseV = highestDetailMesh.originalMesh.vertices;
                        var lodV = originalMesh.vertices;
                        MeshDataTools.FindClosestVertices(lodV, baseV, nearestVertices);
                    }

                    switch (bindings.vertexIndexChannel)
                    {
                        case RGBAChannel.R:
                            {
                                for (int i = 0; i < colors.Length; i++)
                                {
                                    var c = colors[i];
                                    c.r = nearestVertices[i];
                                    colors[i] = c;
                                }
                            }
                            break;

                        case RGBAChannel.G:
                            {
                                for (int i = 0; i < colors.Length; i++)
                                {
                                    var c = colors[i];
                                    c.g = nearestVertices[i];
                                    colors[i] = c;
                                }
                            }
                            break;

                        case RGBAChannel.B:
                            {
                                for (int i = 0; i < colors.Length; i++)
                                {
                                    var c = colors[i];
                                    c.b = nearestVertices[i];
                                    colors[i] = c;
                                }
                            }
                            break;

                        case RGBAChannel.A:
                            {
                                for (int i = 0; i < colors.Length; i++)
                                {
                                    var c = colors[i];
                                    c.a = nearestVertices[i];
                                    colors[i] = c;
                                }
                            }
                            break;
                    }
                }

                meshLod.mesh.colors = colors;

                var blendShapes = meshLod.mesh.GetBlendShapes();
                bool hasBlendShapes = blendShapes.Count > 0;

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

                    if (hasBlendShapes)
                    {
                        foreach (var shape in blendShapes)
                        {
                            if (shape.frames == null) continue;
                            foreach (var frame in shape.frames)
                            {
                                frame.deltaVertices[i] = rotOffset * frame.deltaVertices[i];
                                frame.deltaNormals[i] = rotOffset * frame.deltaNormals[i];
                                frame.deltaTangents[i] = rotOffset * frame.deltaTangents[i];
                            }
                        }
                    }
                }

                meshLod.mesh.vertices = vertices;

                if (hasNormals)
                {
                    meshLod.mesh.normals = normals;
                }
                else
                {
                    meshLod.mesh.RecalculateNormals();
                }

                if (hasTangents)
                {
                    meshLod.mesh.tangents = tangents;
                }
                else if (hasNormals)
                {
                    meshLod.mesh.RecalculateTangents();
                }

                if (hasBlendShapes)
                {
                    meshLod.mesh.ClearBlendShapes();
                    foreach (var shape in blendShapes) shape.AddToMesh(meshLod.mesh, false);
                }

                meshLod.mesh.UploadMeshData(false);

                string assetDir = UnityEditor.AssetDatabase.GetAssetPath(originalMesh);
                assetDir = Path.GetDirectoryName(assetDir);
                string assetPath = Path.Combine(assetDir, meshLod.mesh.name + ".asset");
                /*int j = 0;
                while (UnityEditor.AssetDatabase.AssetPathExists(assetPath))
                {
                    j++;
                    assetPath = Path.Combine(assetDir, meshLod.mesh.name + "_" + j + ".asset");
                }*/
                if (UnityEditor.AssetDatabase.AssetPathExists(assetPath))
                {
                    // This deletes the asset file and its corresponding .meta file
                    UnityEditor.AssetDatabase.DeleteAsset(assetPath);
                } 

                UnityEditor.AssetDatabase.CreateAsset(meshLod.mesh, assetPath);
                UnityEditor.AssetDatabase.SaveAssets();

                var origPath = UnityEditor.AssetDatabase.GetAssetPath(originalMesh);
                originalMeshHash = UnityEditor.AssetDatabase.GetAssetDependencyHash(origPath).ToString();
            }
#endif

            public Mesh GetMesh(BodyMoldBindings bindings)
            {
#if UNITY_EDITOR
                if (originalMesh != null)
                {
                    bool flag = string.IsNullOrWhiteSpace(originalMeshHash) || meshLod.mesh == null;
                    if (!flag)
                    {
                        var meshPath = UnityEditor.AssetDatabase.GetAssetPath(originalMesh);
                        var meshHash = UnityEditor.AssetDatabase.GetAssetDependencyHash(meshPath);
                        var origHash = Hash128.Parse(originalMeshHash);
                        flag = meshHash != origHash;
                    }

                    if (flag)
                    {
                        RegenerateMesh(bindings.GetGeneratedMesh(0), bindings); 
                    }
                }
#endif

                return meshLod.mesh;
            }
        }

        [SerializeField]
        public RGBAChannel vertexIndexChannel = RGBAChannel.R;
        [SerializeField]
        public bool useUVsToFindClosestVertex = true;
        [SerializeField]
        public UVChannelURP nearestVertexUVChannel = UVChannelURP.UV0;

        [SerializeField]
        protected GeneratedMeshLOD[] meshLods;
        public int LodCount => meshLods != null ? meshLods.Length : 0;
        public void SetGeneratedMeshes(GeneratedMeshLOD[] lods)
        {
            meshLods = lods;
        }
        public GeneratedMeshLOD GetGeneratedMesh(int lod)
        {
            if (meshLods == null || meshLods.Length <= 0 || lod < 0 || lod >= meshLods.Length) return null;
            return meshLods[lod];
        }
        public Mesh GetMesh(int lod)
        {
            var generatedMesh = GetGeneratedMesh(lod);
            if (generatedMesh == null) return null;

            return generatedMesh.GetMesh(this);
        }
        public Mesh GetMesh() => GetMesh(0);
        public MeshLOD GetMeshLOD(int lod)
        {
            var generatedMesh = GetGeneratedMesh(lod);
            if (generatedMesh == null) return default;

            var meshLod = generatedMesh.meshLod;
            meshLod.mesh = generatedMesh.GetMesh(this);
            return meshLod;
        }

        [SerializeField]
        protected Material[] materials;
        public void SetMaterials(Material[] mats) => materials = mats;
        public Material[] GetMaterials() => materials;

        public bool dynamicBoneWeights;
        public Vector3 eulerOffset; 

        public BodyMeshBinding[] perBodyBindings;

        [NonSerialized]
        protected BoneWeight8Float[] boneWeights;
        public bool BoneWeightsAreInitialized => boneWeights != null && boneWeights.Length > 0;
        public void InitializeBoneWeights()
        {
            if (BoneWeightsAreInitialized) return;

            var mesh = GetMesh(0);
            boneWeights = new BoneWeight8Float[mesh.vertexCount];

            var _boneCounts = mesh.GetBonesPerVertex();
            var _boneWeights = mesh.GetAllBoneWeights();
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
                    var mesh = GetMesh(0);
                    var triangles = mesh.triangles;
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
                    var mesh = GetMesh(0);
                    var vColors = mesh.colors;
                    if (vColors == null || vColors.Length <= 0)
                    {
                        vColors = new Color[mesh.vertexCount];
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

        [NonSerialized]
        private ComputeBuffer vertexDataBuffer;
        public ComputeBuffer VertexDataBuffer
        {
            get
            {
                if (vertexDataBuffer == null)
                {
                    TrackDisposables();
                    var mesh = GetMesh(0);
                    var vertices = mesh.vertices;
                    var normals = mesh.normals;
                    var tangents = mesh.tangents;
                    var vertexData = new MeshVertexData[mesh.vertexCount];
                    for (int a = 0; a < vertexData.Length; a++)
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

            var mesh = GetMesh(0);
            var vertices = mesh.vertices;
            var arrays = MeshDataTools.GetDistanceWeightedVertexConnections(mesh.triangles, vertices, MeshDataTools.WeldVertices(vertices));

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
                    if (!VertexConnectionsAreInitialized) InitializeVertexConnections();
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
                    if (!VertexConnectionsAreInitialized) InitializeVertexConnections();
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
                    if (!VertexConnectionsAreInitialized) InitializeVertexConnections();
                    vertexConnectionStartIndicesBuffer = new ComputeBuffer(vertexConnectionStartIndices.Length, UnsafeUtility.SizeOf(typeof(int)), ComputeBufferType.Structured, ComputeBufferMode.Immutable);
                    vertexConnectionStartIndicesBuffer.SetData(vertexConnectionStartIndices);
                }

                return vertexConnectionStartIndicesBuffer;
            }
        }

        [HideInInspector]
        public uint[] vertexWeld;
        public bool VertexWeldingIsInitialized => vertexWeld != null && vertexWeld.Length > 0;
        public void InitializeVertexWelding()
        {
            if (VertexWeldingIsInitialized) return;
            
            var mesh = GetMesh(0);
            var vertices = mesh.vertices;
            var weld = MeshDataTools.WeldVertices(vertices);
            var vertexWeld = new uint[weld.Length];

            for (int a = 0; a < weld.Length; a++)
            {
                vertexWeld[a] = (uint)weld[a].firstIndex;
            }

            this.vertexWeld = vertexWeld; 
        }

        [NonSerialized]
        private ComputeBuffer vertexWeldBuffer;

        public ComputeBuffer VertexWeldBuffer
        {
            get
            {
                if (vertexWeldBuffer == null)
                {
                    TrackDisposables();
                    if (!VertexWeldingIsInitialized) InitializeVertexWelding();
                    vertexWeldBuffer = new ComputeBuffer(vertexWeld.Length, UnsafeUtility.SizeOf(typeof(uint)), ComputeBufferType.Structured, ComputeBufferMode.Immutable);
                    vertexWeldBuffer.SetData(vertexWeld);
                }

                return vertexWeldBuffer;
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

            if (vertexWeldBuffer != null)
            {
                vertexWeldBuffer.Dispose();
                vertexWeldBuffer = null;
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

            public bool maskOnly;

            //[HideInInspector]
            public int[] vertexBindingLocalIndices;
            //[HideInInspector]
            public int4[] vertexBindingIndices;
            //[HideInInspector]
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
