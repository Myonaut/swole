#if UNITY_2017_1_OR_NEWER

using System;
using System.IO;
using System.Runtime.InteropServices;

using UnityEngine;

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

using Swole.DataStructures;

namespace Swole
{
    public class MeshBindingData : ScriptableObject, IDisposable
    {
        [SerializeField]
        protected Mesh mesh;
        public Mesh OriginalMesh => mesh;
        public void SetOriginalMesh(Mesh mesh) => this.mesh = mesh;

        [SerializeField]
        private Mesh editedMesh;
        public Mesh EditedMesh
        {
            get
            {
                if (editedMesh == null)
                {
                    if (eulerOffset != Vector3.zero && mesh != null)
                    {
                        editedMesh = Instantiate(mesh);
                        editedMesh.name = mesh.name + "_BindingEdited";

                        var rotOffset = Quaternion.Euler(eulerOffset);
                        var vertices = editedMesh.vertices;
                        var normals = editedMesh.normals;
                        var tangents = editedMesh.tangents;

                        var blendShapes = editedMesh.GetBlendShapes();
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

                        editedMesh.vertices = vertices;

                        if (hasNormals)
                        {
                            editedMesh.normals = normals;
                        }
                        else
                        {
                            editedMesh.RecalculateNormals();
                        }

                        if (hasTangents)
                        {
                            editedMesh.tangents = tangents;
                        }
                        else if (hasNormals)
                        {
                            editedMesh.RecalculateTangents();
                        }

                        if (hasBlendShapes)
                        {
                            editedMesh.ClearBlendShapes();
                            foreach (var shape in blendShapes) shape.AddToMesh(editedMesh, false);
                        }

                        editedMesh.UploadMeshData(false);

#if UNITY_EDITOR
                        string assetDir = UnityEditor.AssetDatabase.GetAssetPath(mesh);
                        assetDir = Path.GetDirectoryName(assetDir);
                        string assetPath = Path.Combine(assetDir, editedMesh.name + ".asset");
                        int j = 0;
                        while(UnityEditor.AssetDatabase.AssetPathExists(assetPath))
                        {
                            j++;
                            assetPath = Path.Combine(assetDir, editedMesh.name + "_" + j + ".asset"); 
                        }

                        UnityEditor.AssetDatabase.CreateAsset(editedMesh, assetPath);
                        UnityEditor.AssetDatabase.SaveAssets();
#endif
                    }
                    else
                    {
                        return mesh;
                    }
                }

                return editedMesh;
            }
        }

        public Vector3 eulerOffset;

        public bool useCustomSkinning;

        [System.Serializable, StructLayout(LayoutKind.Sequential)]
        public struct VertexBinding
        {
            public int triangleIndex;
            [HideInInspector]
            public float pad0;
            [HideInInspector]
            public float pad1;
            [HideInInspector]
            public float pad2;

            public Vector4 vertexIndices;
            /// <summary>
            /// Barycentric weights
            /// </summary>
            public Vector4 weights;
            /// <summary>
            /// Optional: Distance from the actual triangle plane
            /// </summary>
            public Vector4 localOffset; 
        }

        //[HideInInspector] 
        public VertexBinding[] bindings;

        [NonSerialized]
        private GraphicsBuffer bindingsBuffer;
        public GraphicsBuffer BindingsBuffer
        {
            get
            {
                if (bindingsBuffer == null)
                {
                    TrackDisposables();
                    bindingsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured,
                        GraphicsBuffer.UsageFlags.None, // Prevents CPU modification flags
                        bindings.Length, UnsafeUtility.SizeOf<VertexBinding>());

                    using (var tempData = new NativeArray<VertexBinding>(bindings, Allocator.Temp))
                    {
                        bindingsBuffer.SetData(tempData);
                    }
                }

                return bindingsBuffer;
            }
        }

        [NonSerialized]
        private GraphicsBuffer boneWeightsBuffer;
        public GraphicsBuffer BoneWeightsBuffer
        {
            get
            {
                if (boneWeightsBuffer == null)
                {
                    TrackDisposables();

                    var mesh = EditedMesh;
                    if (mesh == null)
                    {
                        boneWeightsBuffer = PersistentJobDataTracker.GetEmptyGraphicsBuffer();
                    }
                    else
                    {
                        using (var boneWeights = new NativeArray<BoneWeight8Float>(mesh.vertexCount, Allocator.Temp))
                        {
                            var boneWeights_ = boneWeights;

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

                                boneWeights_[a] = customBW;

                                i += count;
                            }
                            _boneCounts.Dispose();
                            _boneWeights.Dispose();

                            boneWeightsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured,
                                GraphicsBuffer.UsageFlags.None, // Prevents CPU modification flags
                                boneWeights_.Length, UnsafeUtility.SizeOf<BoneWeight8Float>());
                            boneWeightsBuffer.SetData(boneWeights_);  

                            Debug.Log("CREATED BONE WEIGHTS BUFFER FOR MESH: " + mesh.name + " with vertex count: " + boneWeights_.Length);
                        }
                    }
                }

                return boneWeightsBuffer;
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
            if (bindingsBuffer != null)
            {
                bindingsBuffer.Dispose();
                bindingsBuffer = null;
            }
            if (boneWeightsBuffer != null)
            {
                boneWeightsBuffer.Dispose();
                boneWeightsBuffer = null;
            }
        }

    }
}

#endif