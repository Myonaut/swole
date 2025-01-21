#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

using Swole.DataStructures;
using static Swole.SharedMeshData;

namespace Swole.API.Unity
{

    [Serializable]
    public class MuscularCharacterSurface : CharacterSurface
    {

        public MuscularRenderedCharacter MuscularCharacter => (MuscularRenderedCharacter)Character;

        protected NativeArray<float> m_midpointMuscleGroup;
        protected NativeArray<float> m_muscleGroups;
        protected NativeArray<BlendShapeVertex> m_muscleShapes;

        protected float m_muscleFitThreshold = 0.35f;
        public float MuscleFitThreshold => m_muscleFitThreshold;

        protected float m_fullFlexThreshold = 0.5f;
        public float FullFlexThreshold => m_fullFlexThreshold;

        public MuscularCharacterSurface(MuscularCharacterSharedData sharedData, MuscularRenderedCharacter character, SkinnedMeshRenderer renderer, float muscleFitThreshold = 0.35f, float fullFlexThreshold = 0.5f) : base(character, renderer)
        {

            m_muscleFitThreshold = muscleFitThreshold;
            m_fullFlexThreshold = fullFlexThreshold;

            if (sharedData != null)
            {

                if (renderer != null && renderer.sharedMesh != null)
                {

                    int vertexCount = renderer.sharedMesh.vertexCount;
                    string meshName = renderer.sharedMesh.name.ToLower().Trim();

                    List<float> midpointWeights = new List<float>();
                    List<float> weights = new List<float>();
                    int indexOffset = 0;

                    if (sharedData.muscleGroups != null)
                    {

                        void AddMuscleGroup(List<float> weights, MuscularCharacterSharedData.MuscleGroup group, ref int indexOffset)
                        {

                            for (int b = 0; b < vertexCount; b++) weights.Add(0);

                            if (group != null && group.surfaceWeights != null)
                            {

                                for (int b = 0; b < group.surfaceWeights.Length; b++)
                                {

                                    var surfaceWeights = group.surfaceWeights[b];

                                    if (surfaceWeights == null || surfaceWeights.surfaceName == null || surfaceWeights.weights == null) continue;

                                    string surfaceName = surfaceWeights.surfaceName.ToLower().Trim();

                                    if (!surfaceName.StartsWith(meshName)) continue;

                                    if (vertexCount != surfaceWeights.vertexCount)
                                    {

                                        Debug.LogWarning($"Vertex Count mismatch on muscle group '{group.name}' for surface '{surfaceName}' and mesh '{meshName}' - Expected {surfaceWeights.vertexCount} but found {vertexCount}");

                                        continue;

                                    }

                                    for (int c = 0; c < surfaceWeights.weights.Length; c++)
                                    {

                                        WeightIndexPair pair = surfaceWeights.weights[c];

                                        weights[indexOffset + pair.index] = pair.weight;

                                    }

                                    break;

                                }

                            }

                            indexOffset += vertexCount;

                        }

                        AddMuscleGroup(midpointWeights, sharedData.midpointMuscleGroup, ref indexOffset);

                        indexOffset = 0;

                        for (int a = 0; a < sharedData.muscleGroups.Length; a++) AddMuscleGroup(weights, sharedData.muscleGroups[a], ref indexOffset);

                    }

                    m_midpointMuscleGroup = new NativeArray<float>(midpointWeights.ToArray(), Allocator.Persistent);
                    m_muscleGroups = new NativeArray<float>(weights.ToArray(), Allocator.Persistent);

                    List<BlendShapeVertex> shapes = new List<BlendShapeVertex>();

                    void AddShape(MuscularCharacterSharedData.ShapeData data)
                    {

                        if (data == null || string.IsNullOrEmpty(data.shapeName) || (data.deltaVertices == null || data.deltaVertices.Length <= 0) || (data.deltaNormals == null || data.deltaNormals.Length <= 0) || (data.deltaRecalculatedNormals == null || data.deltaRecalculatedNormals.Length <= 0)) return;

                        //Debug.Log(renderer.name + " : " + data.shapeName + " : " + data.deltaVertices.Length + " : " + vertexCount);
                        for (int a = 0; a < vertexCount; a++) 
                        {

                            shapes.Add(new BlendShapeVertex()
                            {

                                data = new float3x3(data.deltaVertices[a], data.deltaNormals[a], data.deltaRecalculatedNormals[a])

                            });

                        }

                    }

                    if (sharedData.surfaceShapeData != null)
                    {

                        for (int a = 0; a < sharedData.surfaceShapeData.Length; a++)
                        {

                            var shapeData = sharedData.surfaceShapeData[a];

                            if (shapeData == null || shapeData.surfaceName == null) continue;

                            string surfaceName = shapeData.surfaceName.ToLower().Trim();

                            if (!surfaceName.StartsWith(meshName)) continue;

                            AddShape(shapeData.shapeMuscleFit);
                            AddShape(shapeData.shapeMuscleMax);
                            AddShape(shapeData.shapeMuscleFlex);
                            AddShape(shapeData.shapeBreastFull);
                            AddShape(shapeData.shapeBreastFullMuscleMax);

                            break;

                        }

                    }

                    m_muscleShapes = new NativeArray<BlendShapeVertex>(shapes.ToArray(), Allocator.Persistent);

                }

            }

        }

        public override void Dispose()
        {

            base.Dispose();

            if (m_midpointMuscleGroup.IsCreated) m_midpointMuscleGroup.Dispose();
            if (m_muscleGroups.IsCreated) m_muscleGroups.Dispose();
            if (m_muscleShapes.IsCreated) m_muscleShapes.Dispose();

            m_midpointMuscleGroup = default;
            m_muscleGroups = default;
            m_muscleShapes = default;

        }

        protected override JobHandle UpdateSurface(Rigs.Sampler rigSampler, JobHandle inputDeps = default)
        {

            return new UpdateMuscularSurfaceJob()
            {

                samplePoints = m_SamplePoints,
                boneMatrices = rigSampler.Pose,
                vertexCount = m_sharedMeshData.VertexCount,
                blendShapeCount = m_sharedMeshData.BlendShapeCount,
                blendShapeWeights = m_blendShapeWeights,
                blendShapeFrameCounts = m_sharedMeshData.BlendShapeFrameCounts,
                blendShapeFrameWeights = m_sharedMeshData.BlendShapeFrameWeights,
                blendShapeData = m_sharedMeshData.BlendShapeData,
                muscleGroupCount = MuscularCharacter.MuscleGroupCount,
                muscleFitThreshold = m_muscleFitThreshold,
                fullFlexThreshold = m_fullFlexThreshold,
                breastPresence = MuscularCharacter.BreastPresence,
                muscleValues = MuscularCharacter.MuscleGroupValues,
                midpointMuscleGroup = m_midpointMuscleGroup,
                muscleGroups = m_muscleGroups,
                muscleShapes = m_muscleShapes

            }.Schedule(m_SamplePoints.Length, 8, inputDeps);

        }

        public static void ApplyMuscleShapes(float midlineWeight, SamplePoint samplePoint, int vertexCount, int muscleGroupCount, float muscleFitThreshold, float fullFlexThreshold, float breastPresence, NativeArray<MuscleGroupInfo> muscleValues, NativeArray<float> muscleGroups, NativeArray<BlendShapeVertex> muscleShapes, ref float3 vertex, ref float3 normal, ref float3 surfaceNormal)
        {

            for (int a = 0; a < muscleGroupCount; a++)
            {

                var info = muscleValues[a];

                int i = a * vertexCount;

                float weight = muscleGroups[i + samplePoint.index];

                int mirroredIndex = math.select(a, info.mirroredIndex, info.mirroredIndex >= 0);
                var mirroredInfo = muscleValues[mirroredIndex];

                BlendShapeVertex deltaDataFit = muscleShapes[samplePoint.index];
                BlendShapeVertex deltaDataMax = muscleShapes[samplePoint.index + vertexCount];
                BlendShapeVertex deltaDataFlex = muscleShapes[samplePoint.index + vertexCount * 2];
                BlendShapeVertex deltaDataBreastFull = muscleShapes[samplePoint.index + vertexCount * 3];
                BlendShapeVertex deltaDataBreastFullMax = muscleShapes[samplePoint.index + vertexCount * 4];

                float massFactor = math.max(info.mass, mirroredInfo.mass * midlineWeight);
                float flexFactor = math.max(info.flex, mirroredInfo.flex * midlineWeight);

                float fitFactor = math.saturate(massFactor / muscleFitThreshold);
                float maxMuscleFactor = math.max(0, (massFactor - muscleFitThreshold) / (1 - muscleFitThreshold));

                float maxMuscleFactorSat = math.min(1, maxMuscleFactor);

                fitFactor = fitFactor * (1 - maxMuscleFactorSat);
                flexFactor = math.saturate(massFactor / fullFlexThreshold) * flexFactor;

                deltaDataFit = deltaDataFit * fitFactor;
                deltaDataMax = deltaDataMax * maxMuscleFactor;
                deltaDataFlex = deltaDataFlex * flexFactor;
                deltaDataBreastFull = deltaDataBreastFull * (1 - maxMuscleFactor) * breastPresence;
                deltaDataBreastFullMax = deltaDataBreastFullMax * maxMuscleFactor * breastPresence;

                BlendShapeVertex combinedDeltaData = (deltaDataFit + deltaDataMax + deltaDataFlex + deltaDataBreastFull + deltaDataBreastFullMax) * weight;

                vertex = vertex + combinedDeltaData.DeltaVertex;
                normal = normal + combinedDeltaData.DeltaNormal;
                surfaceNormal = surfaceNormal + combinedDeltaData.DeltaSurfaceNormal;

            }

        }

        [BurstCompile]
        public struct UpdateMuscularSurfaceJob : IJobParallelFor
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

            public int muscleGroupCount;
            public float muscleFitThreshold;
            public float fullFlexThreshold;
            public float breastPresence;

            [ReadOnly]
            public NativeArray<MuscleGroupInfo> muscleValues;
            [ReadOnly]
            public NativeArray<float> midpointMuscleGroup;
            [ReadOnly]
            public NativeArray<float> muscleGroups;
            [ReadOnly]
            public NativeArray<BlendShapeVertex> muscleShapes;

            public void Execute(int index)
            {

                SamplePoint samplePoint = samplePoints[index];

                float3 vertex = samplePoint.localVertex;
                float3 normal = samplePoint.localNormal;
                float3 surfaceNormal = samplePoint.localSurfaceNormal;

                ApplyMuscleShapes(midpointMuscleGroup[samplePoint.index], samplePoint, vertexCount, muscleGroupCount, muscleFitThreshold, fullFlexThreshold, breastPresence, muscleValues, muscleGroups, muscleShapes, ref vertex, ref normal, ref surfaceNormal);

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