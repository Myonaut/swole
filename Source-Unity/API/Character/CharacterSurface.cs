#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Jobs;

using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;

using Swole.API.Unity.Animation;

using static Swole.SharedMeshData;
using static Swole.DataStructures;

namespace Swole.API.Unity
{

    [Serializable]
    public class CharacterSurface : IDisposable
    {

        protected RenderedCharacter m_Character;
        public RenderedCharacter Character => m_Character;

        protected SkinnedMeshRenderer m_Renderer;
        public SkinnedMeshRenderer Renderer => m_Renderer;

        protected CustomAnimator m_Animator;
        public CustomAnimator Animator
        {
            get
            {
                if (m_Animator == null)
                {
                    if (m_Renderer != null)
                    {
                        m_Animator = m_Renderer.GetComponentInParent<CustomAnimator>();
                    }
                    if (m_Animator == null && m_Character != null) m_Animator = m_Character.GetComponentInParent<CustomAnimator>();
                }
                return m_Animator;
            }
        }

        public string Name => m_Renderer == null ? "null" : m_Renderer.sharedMesh == null ? "null" : m_Renderer.sharedMesh.name;

        public CharacterSurface(RenderedCharacter character, SkinnedMeshRenderer renderer)
        {

            m_Character = character;
            m_Renderer = renderer;

            m_SamplePoints = new NativeList<SamplePoint>(1, Allocator.Persistent);
            m_SamplePointIndices = new List<SamplePointIndex>();

        }

        [Serializable]
        public class AccessToken : IDisposable
        {

            public bool state;

            public bool dispose;

            public bool destroyed;

            public bool HasChanged(bool reset = true)
            {

                bool s_ = state;

                if (reset) state = false;

                return s_;

            }

            public void Dispose()
            {

                dispose = true;

            }

        }

        protected List<AccessToken> m_accessTokens = new List<AccessToken>();
        protected List<AccessToken> m_tokenToRemove = new List<AccessToken>();

        protected void NotifyAccessors()
        {

            foreach (AccessToken token in m_accessTokens)
            {

                token.state = true;

                if (token.dispose) m_tokenToRemove.Add(token);

            }

            foreach (AccessToken token in m_tokenToRemove) m_accessTokens.Remove(token);

            m_tokenToRemove.Clear();

        }

        public AccessToken CreateAccessToken()
        {

            var token = new AccessToken();

            m_accessTokens.Add(token);

            return token;

        }

        [Serializable]
        public struct SamplePoint
        {

            public int index;

            public BoneWeight8 boneWeights;

            public float3 localVertex;
            public float3 localNormal;
            public float3 localSurfaceNormal;

            public float3 localReshapedVertex;
            public float3 localReshapedNormal;
            public float3 localReshapedSurfaceNormal;

            public float3 position;
            public float3 normal;
            /// <summary>
            /// Currently unused for extra performance. Check CharacterSurface and MuscularCharacterSurface Jobs to allow usage again.
            /// </summary>
            public float3 surfaceNormal;

            public float4x4 localToWorld;

            public quaternion GetRotation()
            {

                return Maths.GetRotation(localToWorld);

            }

            public bool IsReshaped => math.all(localVertex != localReshapedVertex) || math.all(localNormal != localReshapedNormal) || math.all(localSurfaceNormal != localReshapedSurfaceNormal);

        }

        public static SamplePoint _DefaultSamplePointData => new SamplePoint() { localToWorld = Matrix4x4.identity };

        [Serializable]
        public class SamplePointIndex
        {

            public int localIndex;

        }

        [Serializable]
        public class SamplePointReference
        {

            public SamplePointReference(SamplePointIndex localIndex)
            {

                this.localIndex = localIndex;

            }

            protected SamplePointIndex localIndex;

            public int LocalIndex => localIndex.localIndex;

            public bool Valid => LocalIndex >= 0;

        }

        protected NativeList<SamplePoint> m_SamplePoints;
        public NativeList<SamplePoint> SamplePoints => m_SamplePoints;
        public int SamplePointCount => m_SamplePoints.Length;

        public bool ReadyForSampling => m_SamplePoints.IsCreated;

        public SamplePoint GetSamplePoint(int samplePointIndex) => samplePointIndex < 0 || samplePointIndex >= m_SamplePoints.Length ? _DefaultSamplePointData : GetSamplePointUnsafe(samplePointIndex);
        public SamplePoint GetSamplePointUnsafe(int samplePointIndex)
        {

            Dependency.Complete();

            return GetSamplePointUnsafeUnchecked(samplePointIndex);

        }
        public SamplePoint GetSamplePointUnsafeUnchecked(int samplePointIndex) => m_SamplePoints[samplePointIndex];

        public SamplePoint GetSamplePoint(SamplePointReference reference) => GetSamplePoint(reference.LocalIndex);
        public SamplePoint GetSamplePointUnsafe(SamplePointReference reference) => GetSamplePointUnsafe(reference.LocalIndex);
        public SamplePoint GetSamplePointUnsafeUnchecked(SamplePointReference reference) => GetSamplePointUnsafeUnchecked(reference.LocalIndex);

        protected List<SamplePointIndex> m_SamplePointIndices;

        protected SharedMeshData.Cache m_sharedMeshData;
        protected NativeArray<float> m_blendShapeWeights;

        public virtual SamplePointReference GetSamplePointReference(int vertexIndex, bool vertexDataProvided = false, Vector3 localVertex = default, Vector3 localNormal = default, Vector3 localSurfaceNormal = default, BoneWeight8 localBoneWeights = default)
        {

            if (m_SamplePointIndices == null || !m_SamplePoints.IsCreated) return null;

            for (int a = 0; a < m_SamplePointIndices.Count; a++)
            {

                if (m_SamplePoints[a].index == vertexIndex) return new SamplePointReference(m_SamplePointIndices[a]);

            }

            int localIndex = m_SamplePointIndices.Count;

            if (m_sharedMeshData == null && m_Renderer != null)
            {

                m_sharedMeshData = SharedMeshData.GetData(m_Renderer.sharedMesh);

                if (m_sharedMeshData != null && !m_blendShapeWeights.IsCreated) m_blendShapeWeights = new NativeArray<float>(m_sharedMeshData.BlendShapeCount, Allocator.Persistent);

            }

            if (!vertexDataProvided && m_sharedMeshData != null)
            {

                if (vertexIndex >= 0)
                {

                    if (vertexIndex < m_sharedMeshData.Vertices.Length) localVertex = m_sharedMeshData.Vertices[vertexIndex];
                    if (vertexIndex < m_sharedMeshData.Normals.Length) localNormal = m_sharedMeshData.Normals[vertexIndex];
                    if (vertexIndex < m_sharedMeshData.SurfaceNormals.Length) localSurfaceNormal = m_sharedMeshData.SurfaceNormals[vertexIndex];

                    int boneIndex = 0;

                    for (int a = 0; a < vertexIndex; a++)
                    {

                        boneIndex += m_sharedMeshData.BoneCounts[a];

                    }

                    // Handle Bone Weights

                    Rigs.Sampler rigSampler = Rigs.GetSampler(m_Renderer);

                    if (rigSampler != null)
                    {

                        localBoneWeights = new BoneWeight8();

                        int boneCount = m_sharedMeshData.BoneCounts[vertexIndex];

                        if (boneCount > 0 && boneIndex < m_sharedMeshData.BoneWeights.Length)
                        {

                            int index = m_sharedMeshData.BoneWeights[boneIndex].boneIndex;
                            float weight = m_sharedMeshData.BoneWeights[boneIndex].weight;

                            if (weight > 0) rigSampler.Track(index);

                            localBoneWeights.boneIndex0 = index;
                            localBoneWeights.boneWeight0 = weight;

                            boneIndex++;

                        }

                        if (boneCount > 1 && boneIndex < m_sharedMeshData.BoneWeights.Length)
                        {

                            int index = m_sharedMeshData.BoneWeights[boneIndex].boneIndex;
                            float weight = m_sharedMeshData.BoneWeights[boneIndex].weight;

                            if (weight > 0) rigSampler.Track(index);

                            localBoneWeights.boneIndex1 = index;
                            localBoneWeights.boneWeight1 = weight;

                            boneIndex++;

                        }

                        if (boneCount > 2 && boneIndex < m_sharedMeshData.BoneWeights.Length)
                        {

                            int index = m_sharedMeshData.BoneWeights[boneIndex].boneIndex;
                            float weight = m_sharedMeshData.BoneWeights[boneIndex].weight;

                            if (weight > 0) rigSampler.Track(index);

                            localBoneWeights.boneIndex2 = index;
                            localBoneWeights.boneWeight2 = weight;

                            boneIndex++;

                        }

                        if (boneCount > 3 && boneIndex < m_sharedMeshData.BoneWeights.Length)
                        {

                            int index = m_sharedMeshData.BoneWeights[boneIndex].boneIndex;
                            float weight = m_sharedMeshData.BoneWeights[boneIndex].weight;

                            if (weight > 0) rigSampler.Track(index);

                            localBoneWeights.boneIndex3 = index;
                            localBoneWeights.boneWeight3 = weight;

                            boneIndex++;

                        }

                        if (boneCount > 4 && boneIndex < m_sharedMeshData.BoneWeights.Length)
                        {

                            int index = m_sharedMeshData.BoneWeights[boneIndex].boneIndex;
                            float weight = m_sharedMeshData.BoneWeights[boneIndex].weight;

                            if (weight > 0) rigSampler.Track(index);

                            localBoneWeights.boneIndex4 = index;
                            localBoneWeights.boneWeight4 = weight;

                            boneIndex++;

                        }

                        if (boneCount > 5 && boneIndex < m_sharedMeshData.BoneWeights.Length)
                        {

                            int index = m_sharedMeshData.BoneWeights[boneIndex].boneIndex;
                            float weight = m_sharedMeshData.BoneWeights[boneIndex].weight;

                            if (weight > 0) rigSampler.Track(index);

                            localBoneWeights.boneIndex5 = index;
                            localBoneWeights.boneWeight5 = weight;

                            boneIndex++;

                        }

                        if (boneCount > 6 && boneIndex < m_sharedMeshData.BoneWeights.Length)
                        {

                            int index = m_sharedMeshData.BoneWeights[boneIndex].boneIndex;
                            float weight = m_sharedMeshData.BoneWeights[boneIndex].weight;

                            if (weight > 0) rigSampler.Track(index);

                            localBoneWeights.boneIndex6 = index;
                            localBoneWeights.boneWeight6 = weight;

                            boneIndex++;

                        }

                        if (boneCount > 7 && boneIndex < m_sharedMeshData.BoneWeights.Length)
                        {

                            int index = m_sharedMeshData.BoneWeights[boneIndex].boneIndex;
                            float weight = m_sharedMeshData.BoneWeights[boneIndex].weight;

                            if (weight > 0) rigSampler.Track(index);

                            localBoneWeights.boneIndex7 = index;
                            localBoneWeights.boneWeight7 = weight;

                        }

                    }

                    //

                }

            }

            m_SamplePoints.Add(new SamplePoint() { index = vertexIndex, localToWorld = Matrix4x4.identity, localVertex = localVertex, localNormal = localNormal, localSurfaceNormal = localSurfaceNormal, boneWeights = localBoneWeights });
            m_SamplePointIndices.Add(new SamplePointIndex() { localIndex = localIndex });

            return new SamplePointReference(m_SamplePointIndices[localIndex]);

        }

        public virtual bool TryGetSamplePointReference(int vertexIndex, out SamplePointReference reference)
        {

            reference = null;

            if (m_SamplePointIndices == null || !m_SamplePoints.IsCreated) return false;

            for (int a = 0; a < m_SamplePointIndices.Count; a++)
            {

                if (m_SamplePoints[a].index == vertexIndex)
                {

                    reference = new SamplePointReference(m_SamplePointIndices[a]);

                    return true;

                }

            }

            return false;

        }

        /// <summary>
        /// The index can become invalid over time, so only use this for temporary actions. Otherwise use GetSamplePointReference(). Returns -1 if vertex is not being sampled.
        /// </summary>
        public virtual int GetSamplePointIndex(int vertexIndex)
        {

            if (TryGetSamplePointReference(vertexIndex, out var reference)) return reference.LocalIndex;

            return -1;

        }

        public virtual void UpdateSamplePointReferences(int startInclusive = 0, int endExclusive = int.MaxValue)
        {

            if (m_SamplePointIndices == null || !m_SamplePoints.IsCreated) return;

            for (int a = math.max(startInclusive, 0); a < math.min(m_SamplePointIndices.Count, endExclusive); a++) m_SamplePointIndices[a].localIndex = a;

            NotifyAccessors();

        }

        public virtual bool StopSampling(int vertexIndex)
        {

            if (m_SamplePointIndices == null || !m_SamplePoints.IsCreated) return false;

            for (int a = 0; a < m_SamplePointIndices.Count; a++)
            {

                if (m_SamplePoints[a].index == vertexIndex) return StopSampling(m_SamplePointIndices[a]);

            }

            return false;

        }

        public virtual bool StopSampling(SamplePointReference reference)
        {

            if (m_SamplePointIndices == null || !m_SamplePoints.IsCreated) return false;

            return StopSampling(m_SamplePointIndices[reference.LocalIndex]);

        }

        public virtual bool StopSampling(SamplePointIndex index)
        {

            if (m_SamplePointIndices == null || !m_SamplePoints.IsCreated) return false;

            var samplePoint = m_SamplePoints[index.localIndex];

            Rigs.Sampler rigSampler = Rigs.GetSampler(m_Renderer);

            if (rigSampler != null)
            {

                if (samplePoint.boneWeights.boneIndex0 >= 0 && samplePoint.boneWeights.boneWeight0 > 0) rigSampler.Untrack(samplePoint.boneWeights.boneIndex0);
                if (samplePoint.boneWeights.boneIndex1 >= 0 && samplePoint.boneWeights.boneWeight1 > 0) rigSampler.Untrack(samplePoint.boneWeights.boneIndex1);
                if (samplePoint.boneWeights.boneIndex2 >= 0 && samplePoint.boneWeights.boneWeight2 > 0) rigSampler.Untrack(samplePoint.boneWeights.boneIndex2);
                if (samplePoint.boneWeights.boneIndex3 >= 0 && samplePoint.boneWeights.boneWeight3 > 0) rigSampler.Untrack(samplePoint.boneWeights.boneIndex3);
                if (samplePoint.boneWeights.boneIndex4 >= 0 && samplePoint.boneWeights.boneWeight4 > 0) rigSampler.Untrack(samplePoint.boneWeights.boneIndex4);
                if (samplePoint.boneWeights.boneIndex5 >= 0 && samplePoint.boneWeights.boneWeight5 > 0) rigSampler.Untrack(samplePoint.boneWeights.boneIndex5);
                if (samplePoint.boneWeights.boneIndex6 >= 0 && samplePoint.boneWeights.boneWeight6 > 0) rigSampler.Untrack(samplePoint.boneWeights.boneIndex6);
                if (samplePoint.boneWeights.boneIndex7 >= 0 && samplePoint.boneWeights.boneWeight7 > 0) rigSampler.Untrack(samplePoint.boneWeights.boneIndex7);

            }

            m_SamplePoints.RemoveAt(index.localIndex);
            m_SamplePointIndices.RemoveAt(index.localIndex);

            UpdateSamplePointReferences(index.localIndex);

            index.localIndex = -1;

            return true;

        }

        public virtual void Dispose()
        {

            m_SamplePointIndices = null;

            if (m_SamplePoints.IsCreated) m_SamplePoints.Dispose();

            m_SamplePoints = default;

            if (m_blendShapeWeights.IsCreated) m_blendShapeWeights.Dispose();

            m_blendShapeWeights = default;

            if (m_accessTokens != null)
            {

                foreach (var token in m_accessTokens)
                {

                    token.destroyed = true;
                    token.dispose = true;

                }

                m_accessTokens.Clear();

                m_accessTokens = null;

            }

        }

        protected int m_LastRefreshFrame;
        public int LastRefreshFrame => m_LastRefreshFrame;

        protected JobHandle m_JobsHandle = default;
        public JobHandle Dependency => m_JobsHandle;

        public virtual JobHandle Refresh(bool force = false)
        {

            int frame = Time.frameCount;

            if (m_sharedMeshData == null || m_Renderer == null || LastRefreshFrame == frame && !force) return Dependency;

            m_LastRefreshFrame = frame;

            Dependency.Complete();

            Rigs.Sampler rigSampler = Rigs.GetSampler(m_Renderer);

            if (rigSampler == null || SamplePointCount <= 0) return Dependency;

            var animator = Animator;
            m_JobsHandle = JobHandle.CombineDependencies(Dependency, rigSampler.Refresh(JobHandle.CombineDependencies(animator == null ? default : animator.OutputDependency, ProxyBoneJobs.OutputDependency)));

            for (int a = 0; a < m_sharedMeshData.BlendShapeCount; a++) m_blendShapeWeights[a] = m_Renderer.GetBlendShapeWeight(a);// / m_sharedMeshData.BlendShapeFrameWeights[m_sharedMeshData.BlendShapeFrameWeights.Length - 1];

            m_JobsHandle = UpdateSurface(rigSampler, m_JobsHandle);

            return Dependency;

        }

        protected virtual JobHandle UpdateSurface(Rigs.Sampler rigSampler, JobHandle inputDeps = default)
        {

            return new UpdateSurfaceJob()
            {

                samplePoints = m_SamplePoints,
                boneMatrices = rigSampler.Pose,
                vertexCount = m_sharedMeshData.VertexCount,
                blendShapeCount = m_sharedMeshData.BlendShapeCount,
                blendShapeWeights = m_blendShapeWeights,
                blendShapeFrameCounts = m_sharedMeshData.BlendShapeFrameCounts,
                blendShapeFrameWeights = m_sharedMeshData.BlendShapeFrameWeights,
                blendShapeData = m_sharedMeshData.BlendShapeData

            }.Schedule(m_SamplePoints.Length, 8, inputDeps);

        }

        public virtual void CompleteJobs()
        {

            m_JobsHandle.Complete();

        }

        public virtual void EndFrame()
        {

            CompleteJobs();

        }

        public static int2 CalculateBlendShapePosition(NativeArray<float> blendShapeFrameWeights, int frameCount, float weight, out float interp)
        {

            int2 frames = 0;

            int fCm1 = frameCount - 1;

            for (int a = 0; a < frameCount; a++)
            {

                frames = new int2(a - 1, math.min(a, fCm1));

                if (weight <= blendShapeFrameWeights[frames.y]) break;

            }

            float w1 = (frames.x < 0 ? 0 : blendShapeFrameWeights[frames.x]);
            float w2 = blendShapeFrameWeights[frames.y];

            interp = (weight - w1) / (w2 - w1);

            return frames;

        }

        public static float4x4 CalculateSkinningMatrix(SamplePoint samplePoint, NativeArray<float4x4> boneMatrices)
        {

            float4x4 m0 = boneMatrices[samplePoint.boneWeights.boneIndex0];
            float4x4 m1 = boneMatrices[samplePoint.boneWeights.boneIndex1];
            float4x4 m2 = boneMatrices[samplePoint.boneWeights.boneIndex2];
            float4x4 m3 = boneMatrices[samplePoint.boneWeights.boneIndex3];

            float4x4 m4 = boneMatrices[samplePoint.boneWeights.boneIndex4];
            float4x4 m5 = boneMatrices[samplePoint.boneWeights.boneIndex5];
            float4x4 m6 = boneMatrices[samplePoint.boneWeights.boneIndex6];
            float4x4 m7 = boneMatrices[samplePoint.boneWeights.boneIndex7];

            m0 = m0 * samplePoint.boneWeights.boneWeight0;
            m1 = m1 * samplePoint.boneWeights.boneWeight1;
            m2 = m2 * samplePoint.boneWeights.boneWeight2;
            m3 = m3 * samplePoint.boneWeights.boneWeight3;

            m4 = m4 * samplePoint.boneWeights.boneWeight4;
            m5 = m5 * samplePoint.boneWeights.boneWeight5;
            m6 = m6 * samplePoint.boneWeights.boneWeight6;
            m7 = m7 * samplePoint.boneWeights.boneWeight7;

            return m0 + m1 + m2 + m3 + m4 + m5 + m6 + m7;

        }

        public static void ApplyBlendShapes(SamplePoint samplePoint, int vertexCount, int blendShapeCount, NativeArray<int> blendShapeFrameCounts, NativeArray<float> blendShapeWeights, NativeArray<float> blendShapeFrameWeights, NativeArray<BlendShapeVertex> blendShapeData, ref float3 vertex, ref float3 normal, ref float3 surfaceNormal)
        {

            int i = 0;
            for (int a = 0; a < blendShapeCount; a++)
            {

                int frameCount = blendShapeFrameCounts[a];

                int2 frames = CalculateBlendShapePosition(blendShapeFrameWeights, frameCount, blendShapeWeights[a], out float interp);

                int indexA = ((i + frames.x) * vertexCount) + samplePoint.index;
                int indexB = ((i + frames.y) * vertexCount) + samplePoint.index;

                BlendShapeVertex dataA = frames.x < 0 ? new BlendShapeVertex() : blendShapeData[indexA];
                BlendShapeVertex dataB = blendShapeData[indexB];

                vertex = vertex + math.lerp(dataA.DeltaVertex, dataB.DeltaVertex, interp);
                normal = normal + math.lerp(dataA.DeltaNormal, dataB.DeltaNormal, interp);
                surfaceNormal = surfaceNormal + math.lerp(dataA.DeltaSurfaceNormal, dataB.DeltaSurfaceNormal, interp);

                i += frameCount;

            }

        }

        [BurstCompile]
        public struct UpdateSurfaceJob : IJobParallelFor
        {

            [NativeDisableParallelForRestriction]
            public NativeArray<SamplePoint> samplePoints;

            [ReadOnly]
            public NativeArray<float4x4> boneMatrices;

            public int vertexCount;
            public int blendShapeCount;

            [ReadOnly]
            public NativeArray<float> blendShapeWeights;
            [ReadOnly]
            public NativeArray<int> blendShapeFrameCounts;
            [ReadOnly]
            public NativeArray<float> blendShapeFrameWeights;
            [ReadOnly]
            public NativeArray<BlendShapeVertex> blendShapeData;

            public void Execute(int index)
            {

                SamplePoint samplePoint = samplePoints[index];

                float3 vertex = samplePoint.localVertex;
                float3 normal = samplePoint.localNormal;
                float3 surfaceNormal = samplePoint.localSurfaceNormal;

                ApplyBlendShapes(samplePoint, vertexCount, blendShapeCount, blendShapeFrameCounts, blendShapeWeights, blendShapeFrameWeights, blendShapeData, ref vertex, ref normal, ref surfaceNormal);

                normal = math.normalizesafe(normal);
                surfaceNormal = math.normalizesafe(surfaceNormal);

                samplePoint.localReshapedVertex = vertex;
                samplePoint.localReshapedNormal = normal;
                samplePoint.localReshapedSurfaceNormal = surfaceNormal;

                float4x4 skinningMatrix = CalculateSkinningMatrix(samplePoint, boneMatrices);

                samplePoint.position = math.transform(skinningMatrix, vertex);
                samplePoint.normal = math.rotate(skinningMatrix, normal);
                //samplePoint.surfaceNormal = math.rotate(skinningMatrix, surfaceNormal);

                samplePoint.localToWorld = skinningMatrix;

                samplePoints[index] = samplePoint;

            }

        }

    }

}

#endif