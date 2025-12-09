#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine;

using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Swole.DataStructures;

namespace Swole.Morphing
{
    [CreateAssetMenu(fileName = "MorphableMeshData", menuName = "Swole/InstancedMeshData/MorphableMeshData", order = 3)]
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

}

#endif