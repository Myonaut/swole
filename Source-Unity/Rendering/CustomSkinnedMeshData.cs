#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;

using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

using Swole.DataStructures;

namespace Swole
{

    public class CustomSkinnedMeshData : ScriptableObject
    {

        public static CustomSkinnedMeshData Create(string path, string fileName, bool incrementIfExists = false)
        {

            CustomSkinnedMeshData asset = ScriptableObject.CreateInstance<CustomSkinnedMeshData>();

#if UNITY_EDITOR

            string fullPath = $"{(path + (path.EndsWith('/') ? "" : "/"))}{fileName}.asset";
            if (incrementIfExists) fullPath = AssetDatabase.GenerateUniqueAssetPath(fullPath);
            AssetDatabase.CreateAsset(asset, fullPath);
            AssetDatabase.SaveAssets();

#endif

            return asset;

        }

        public int vertexCount;

        public int BlendShapeCount => blendShapeNames == null ? 0 : blendShapeNames.Length;

        public string[] blendShapeNames;

        public string GetBlendShapeName(int shapeIndex)
        {

            if (blendShapeNames == null || shapeIndex < 0 || shapeIndex >= blendShapeNames.Length) return "";

            return blendShapeNames[shapeIndex];

        }

        [HideInInspector]
        public float[] blendShapeFrameCounts;

        public int GetBlendShapeFrameCount(int shapeIndex)
        {

            if (blendShapeFrameCounts == null || shapeIndex < 0 || shapeIndex >= blendShapeFrameCounts.Length) return 0;

            return (int)blendShapeFrameCounts[shapeIndex];

        }

        [HideInInspector]
        public float[] blendShapeFrameWeights;

        public float GetBlendShapeFrameWeight(int shapeIndex, int frameIndex)
        {

            if (blendShapeFrameCounts == null || frameIndex < 0 || shapeIndex < 0 || shapeIndex >= blendShapeFrameCounts.Length) return 0;

            int i = 0;
            for (int a = 0; a < math.min(frameIndex, blendShapeFrameCounts.Length - 1); a++) i += (int)blendShapeFrameCounts[a];

            i = i + frameIndex;
            if (i < blendShapeFrameWeights.Length)
            {

                return blendShapeFrameWeights[i];

            }

            return 0;

        }

        [HideInInspector]
        public BlendShapeDeltas[] blendShapeDeltas;

        public string rigId;

        [HideInInspector]
        public BoneWeight8Float[] boneWeights;

        [HideInInspector]
        public Matrix4x4[] bindpose;

        public void SetBlendShapes(int vertexCount, ICollection<BlendShape> inputShapes)
        {

            this.vertexCount = vertexCount;

            List<string> names = new List<string>();
            List<float> frameCounts = new List<float>();
            List<float> frameWeights = new List<float>();
            List<BlendShapeDeltas> deltas = new List<BlendShapeDeltas>();

            int i = 0;
            foreach (var inputShape in inputShapes)
            {

                if (inputShape == null || inputShape.frames == null || inputShape.frames.Length == 0) continue;

                int frameCount = math.min(inputShape.frames.Length, CustomSkinnedMesh.maxBlendShapeFrameCount);

                names.Add(inputShape.name);
                frameCounts.Add(frameCount);

                for (int a = 0; a < frameCount; a++)
                {

                    var frame = inputShape.frames[a];

                    frameWeights.Add(frame.weight);

                    for (int b = 0; b < vertexCount; b++)
                    {

                        deltas.Add(new BlendShapeDeltas()
                        {


                            deltaVertex = new float4(frame.deltaVertices[b], 1),
                            deltaNormal = new float4(frame.deltaNormals[b], 0),
                            deltaTangent = new float4(frame.deltaTangents[b], 0)

                        });

                    }

                }

                i++;
                if (i >= CustomSkinnedMesh.maxBlendShapeCount) break;

            }

            blendShapeNames = names.ToArray();
            blendShapeFrameCounts = frameCounts.ToArray();
            blendShapeFrameWeights = frameWeights.ToArray();
            blendShapeDeltas = deltas.ToArray();

        }

        [NonSerialized]
        protected ComputeBuffer m_boneWeightsBuffer;
        [NonSerialized]
        protected ComputeBuffer m_blendShapeDataBuffer;
        [NonSerialized]
        protected int m_boneWeightsUsers;
        [NonSerialized]
        protected int m_blendShapeDataUsers;

        public ComputeBuffer UseBoneWeightsBuffer()
        {

            if (m_boneWeightsBuffer == null)
            {

                m_boneWeightsBuffer = new ComputeBuffer(boneWeights.Length, UnsafeUtility.SizeOf(typeof(BoneWeight8Float)), ComputeBufferType.Structured, ComputeBufferMode.Immutable);
                m_boneWeightsBuffer.SetData(boneWeights);

            }

            m_boneWeightsUsers++;

            return m_boneWeightsBuffer;

        }

        public void EndUseBoneWeightsBuffer()
        {

            m_boneWeightsUsers--;

            if (m_boneWeightsUsers <= 0)
            {

                m_boneWeightsUsers = 0;
                if (m_boneWeightsBuffer != null) m_boneWeightsBuffer.Dispose();
                m_boneWeightsBuffer = null;

            }

        }

        public ComputeBuffer UseBlendShapeDataBuffer()
        {

            if (m_blendShapeDataBuffer == null)
            {

                m_blendShapeDataBuffer = new ComputeBuffer(blendShapeDeltas.Length, UnsafeUtility.SizeOf(typeof(BlendShapeDeltas)), ComputeBufferType.Structured, ComputeBufferMode.Immutable);
                m_blendShapeDataBuffer.SetData(blendShapeDeltas);

            }

            m_blendShapeDataUsers++;

            return m_blendShapeDataBuffer;

        }

        public void EndUseBlendShapeDataBuffer()
        {

            m_blendShapeDataUsers--;

            if (m_blendShapeDataUsers <= 0)
            {

                m_blendShapeDataUsers = 0;
                if (m_blendShapeDataBuffer != null) m_blendShapeDataBuffer.Dispose();
                m_blendShapeDataBuffer = null;

            }

        }

    }

}

#endif