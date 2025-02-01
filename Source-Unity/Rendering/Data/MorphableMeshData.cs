#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine;

using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Swole
{
    [CreateAssetMenu(fileName = "MorphableMeshData", menuName = "InstancedMeshData/MorphableMeshData", order = 3)]
    public class MorphableMeshData : InstanceableSkinnedMeshDataBase
    {

        protected override void DisposeLocal()
        {
            base.DisposeLocal();

            if (morphShapes != null)
            {
                foreach (var shape in morphShapes) if (shape != null) shape.Dispose(); 
            }

            try
            {
                if (vertexGroupDataBuffer != null && vertexGroupDataBuffer.IsValid())
                {
                    vertexGroupDataBuffer.Dispose();
                }
                vertexGroupDataBuffer = null;
            }
            catch (Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogException(ex);
#endif
            }

            try
            {
                if (morphShapeDataBuffer != null && morphShapeDataBuffer.IsValid())
                {
                    morphShapeDataBuffer.Dispose();
                }
                morphShapeDataBuffer = null;
            }
            catch (Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogException(ex);
#endif
            }
        }


        public override void Initialize()
        {
            if (initialized) return;

            base.Initialize();

            ApplyBufferToMaterials(VertexGroupsPropertyName, VertexGroupDataBuffer);
            ApplyBufferToMaterials(MorphShapesPropertyName, MorphShapeDataBuffer); 
        }


        [SerializeField]
        protected MorphShape[] morphShapes;
        public void SetMorphShapes(MorphShape[] morphShapes) => this.morphShapes = morphShapes;
        public int MorphShapeCount => morphShapes == null ? 0 : morphShapes.Length;
        public MorphShape GetMorph(int index)
        {
            if (index < 0 || morphShapes == null || index >= morphShapes.Length) return null;
            return GetMorphUnsafe(index);
        }
        public MorphShape GetMorphUnsafe(int index) => morphShapes[index];
        public int IndexOfMorph(string morphName, bool caseSensitive = false)
        {
            for (int a = 0; a < morphShapes.Length; a++)
            {
                var morph = morphShapes[a];
                if (morph == null) continue;

                if (morph.name == morphName) return a;
            }
            if (caseSensitive) return -1;

            morphName = morphName.ToLower().Trim();
            for (int a = 0; a < morphShapes.Length; a++)
            {
                var morph = morphShapes[a];
                if (morph == null) continue;

                if (!string.IsNullOrWhiteSpace(morph.name) && morph.name.ToLower().Trim() == morphName) return a;
            }

            return -1;
        }
        public List<MorphShape> GetMorphs(List<MorphShape> outputList = null)
        {
            if (outputList == null) outputList = new List<MorphShape>();

            outputList.AddRange(morphShapes);

            return outputList;
        }

        [SerializeField]
        protected VertexGroup[] vertexGroups;
        public void SetVertexGroups(VertexGroup[] vertexGroups) => this.vertexGroups = vertexGroups;
        public int VertexGroupsCount => vertexGroups == null ? 0 : vertexGroups.Length;
        public VertexGroup GetVertexGroup(int index)
        {
            if (index < 0 || vertexGroups == null || index >= vertexGroups.Length) return null;
            return GetVertexGroupUnsafe(index);
        }
        public VertexGroup GetVertexGroupUnsafe(int index) => vertexGroups[index];
        public int IndexOfVertexGroup(string vertexGroupName, bool caseSensitive = false)
        {
            for (int a = 0; a < vertexGroups.Length; a++)
            {
                var vg = vertexGroups[a];
                if (vg == null) continue;

                if (vg.name == vertexGroupName) return a;
            }
            if (caseSensitive) return -1;

            vertexGroupName = vertexGroupName.ToLower().Trim();
            for (int a = 0; a < vertexGroups.Length; a++)
            {
                var vg = vertexGroups[a];
                if (vg == null) continue;

                if (!string.IsNullOrWhiteSpace(vg.name) && vg.name.ToLower().Trim() == vertexGroupName) return a;
            }

            return -1;
        }
        public List<VertexGroup> GetVertexGroups(List<VertexGroup> outputList = null)
        {
            if (outputList == null) outputList = new List<VertexGroup>();

            outputList.AddRange(vertexGroups);

            return outputList;
        }


        public const string _vertexGroupsDefaultPropertyName = "_VertexGroups";
        public string vertexGroupsPropertyNameOverride;
        public string VertexGroupsPropertyName => string.IsNullOrWhiteSpace(vertexGroupsPropertyNameOverride) ? _vertexGroupsDefaultPropertyName : vertexGroupsPropertyNameOverride;

        protected static readonly List<float> tempVertexGroupData = new List<float>();

        [NonSerialized]
        protected ComputeBuffer vertexGroupDataBuffer;
        public ComputeBuffer VertexGroupDataBuffer
        {
            get
            {
                if (vertexGroupDataBuffer == null)
                {
                    int vertexCount = VertexCount;

                    tempVertexGroupData.Clear();
                    if (vertexGroups != null)
                    {
                        for (int a = 0; a < vertexGroups.Length; a++)
                        {
                            var group = vertexGroups[a];
                            if (group == null)
                            {
                                for(int b = 0; b < vertexCount; b++) tempVertexGroupData.Add(0.0f);
                            }
                            else
                            {
                                group.AsLinearWeightList(vertexCount, tempVertexGroupData); 
                            }
                        }
                    }

                    if (tempVertexGroupData.Count > 0)
                    {
                        vertexGroupDataBuffer = new ComputeBuffer(tempVertexGroupData.Count, UnsafeUtility.SizeOf(typeof(float)), ComputeBufferType.Structured, ComputeBufferMode.Immutable);
                        vertexGroupDataBuffer.SetData(tempVertexGroupData);
                        tempVertexGroupData.Clear();
                    }

                    TrackDisposables();
                }

                return vertexGroupDataBuffer;
            }
        }


        public const string _morphShapesDefaultPropertyName = "_MorphShapes";
        public string morphShapesPropertyNameOverride;
        public string MorphShapesPropertyName => string.IsNullOrWhiteSpace(morphShapesPropertyNameOverride) ? _morphShapesDefaultPropertyName : morphShapesPropertyNameOverride;

        protected static readonly List<MorphShapeVertex> tempMorphVertexData = new List<MorphShapeVertex>();

        [NonSerialized]
        protected ComputeBuffer morphShapeDataBuffer;
        public ComputeBuffer MorphShapeDataBuffer
        {
            get
            {
                if (morphShapeDataBuffer == null)
                {
                    int vertexCount = VertexCount;

                    tempMorphVertexData.Clear();
                    if (morphShapes != null)
                    {
                        for (int a = 0; a < morphShapes.Length; a++)
                        {
                            var shape = morphShapes[a];

                            for (int b = 0; b < vertexCount; b++)
                            {
                                var data = new MorphShapeVertex();

                                if (shape != null)
                                {
                                    if (shape.DeltaVertices != null && b < shape.DeltaVertices.Length) data.deltaVertex = shape.DeltaVertices[b];
                                    if (shape.DeltaNormals != null && b < shape.DeltaNormals.Length) data.deltaNormal = shape.DeltaNormals[b];
                                    if (shape.DeltaTangents != null && b < shape.DeltaTangents.Length) data.deltaTangent = shape.DeltaTangents[b];
                                }

                                tempMorphVertexData.Add(data);
                            }
                        }
                    }

                    if (tempMorphVertexData.Count > 0)
                    {
                        morphShapeDataBuffer = new ComputeBuffer(tempMorphVertexData.Count, UnsafeUtility.SizeOf(typeof(MorphShapeVertex)), ComputeBufferType.Structured, ComputeBufferMode.Immutable);
                        morphShapeDataBuffer.SetData(tempMorphVertexData);
                        tempMorphVertexData.Clear();
                    }

                    TrackDisposables();
                }

                return morphShapeDataBuffer;
            }
        }

    }


    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct MorphShapeVertex
    {
        public float3 deltaVertex;
        public float3 deltaNormal;
        public float3 deltaTangent;
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
            for(int a = 0; a < deltaVertices.Length; a++)
            {
                float weight = vg.GetWeight(a);
                deltaVertices[a] = deltaVertices[a] * weight;
                if (deltaNormals != null) deltaNormals[a] = deltaNormals[a] * weight;
                if (deltaTangents != null) deltaTangents[a] = deltaTangents[a] * weight;
            }
        }
        public void ApplyVertexGroupAsCookie(VertexGroup vg, float minWeight = 0.0001f)
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

}

#endif