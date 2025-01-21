#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Unity.Mathematics;

#if UNITY_EDITOR
using UnityEditor;
#endif

using Swole.DataStructures;

namespace Swole.API.Unity
{

    public class MuscularCharacterSharedData : ScriptableObject
    {


        public static MuscularCharacterSharedData Create(string path, string fileName, bool incrementIfExists = false)
        {

            MuscularCharacterSharedData asset = ScriptableObject.CreateInstance<MuscularCharacterSharedData>();

#if UNITY_EDITOR
            string fullPath = $"{(path + (path.EndsWith('/') ? "" : "/"))}{fileName}.asset";
            if (incrementIfExists) fullPath = AssetDatabase.GenerateUniqueAssetPath(fullPath);
            AssetDatabase.CreateAsset(asset, fullPath);
            AssetDatabase.SaveAssets();
#endif

            return asset;
        }

        [Serializable]
        public class MuscleGroup
        {

            public string name;

            public int mirroredIndex;

            public SurfaceBoundWeights[] surfaceWeights;

            public void AddSurfaceWeights(SurfaceBoundWeights data)
            {

                if (surfaceWeights == null) surfaceWeights = new SurfaceBoundWeights[0];

                SurfaceBoundWeights[] temp = new SurfaceBoundWeights[surfaceWeights.Length + 1];
                surfaceWeights.CopyTo(temp, 0);

                temp[surfaceWeights.Length] = data;

                surfaceWeights = temp;

            }

            public int GetSurfaceBoundWeightsIndex(string surfaceName)
            {

                if (surfaceWeights == null) return -1;

                for (int a = 0; a < surfaceWeights.Length; a++) if (surfaceWeights[a].surfaceName == surfaceName) return a;

                return -1;

            }

        }

        [Serializable]
        public class SurfaceBoundWeights
        {

            public string surfaceName;
            public int vertexCount;

            [HideInInspector]
            public WeightIndexPair[] weights;

            [NonSerialized]
            protected float[] cachedWeights;

            public float GetCachedWeight(int index)
            {

                if (cachedWeights == null)
                {

                    cachedWeights = new float[vertexCount];

                    for (int a = 0; a < weights.Length; a++) cachedWeights[weights[a].index] = weights[a].weight;

                }

                if (index < 0 || index >= cachedWeights.Length) return 0;

                return cachedWeights[index];

            }

            public float GetCachedWeightUnsafe(int index)
            {

                return cachedWeights[index];

            }

        }

        [Serializable]
        public class ShapeData
        {

            public string shapeName;

            [HideInInspector]
            public float3[] deltaVertices;
            [HideInInspector]
            public float3[] deltaNormals;
            [HideInInspector]
            public float3[] deltaRecalculatedNormals;

            public static ShapeData FromBlendShape(BlendShape blendShape)
            {

                ShapeData data = new ShapeData();

                data.shapeName = blendShape.name;

                if (blendShape.frames != null && blendShape.frames.Length > 0)
                {

                    BlendShape.Frame frame = blendShape.frames[0];

                    data.deltaVertices = new float3[frame.deltaVertices.Length];
                    data.deltaNormals = new float3[frame.deltaNormals.Length];
                    data.deltaRecalculatedNormals = data.deltaNormals;

                    for (int a = 0; a < frame.deltaVertices.Length; a++)
                    {

                        data.deltaVertices[a] = frame.deltaVertices[a];
                        data.deltaNormals[a] = frame.deltaNormals[a];

                    }

                }

                return data;

            }

            public static ShapeData FromBlendShape(BlendShape blendShape, BlendShape recalculatedBlendShape)
            {

                ShapeData data = new ShapeData();

                data.shapeName = blendShape.name;

                if (blendShape.frames != null && blendShape.frames.Length > 0)
                {

                    BlendShape.Frame frame = blendShape.frames[0];
                    BlendShape.Frame recalculatedFrame = recalculatedBlendShape == null ? frame : recalculatedBlendShape.frames[0];

                    data.deltaVertices = new float3[frame.deltaVertices.Length];
                    data.deltaNormals = new float3[frame.deltaNormals.Length];
                    data.deltaRecalculatedNormals = new float3[frame.deltaNormals.Length];

                    for (int a = 0; a < frame.deltaVertices.Length; a++)
                    {

                        data.deltaVertices[a] = frame.deltaVertices[a];
                        data.deltaNormals[a] = frame.deltaNormals[a];
                        data.deltaRecalculatedNormals[a] = recalculatedFrame.deltaNormals[a];

                    }

                }

                return data;

            }

            public float3 GetTransformedVertex(float3 vertex, int vertexIndex, float weight = 1)
            {

                return vertex + deltaVertices[vertexIndex] * weight;

            }

            public float3 GetTransformedNormal(float3 normal, int vertexIndex, float weight = 1)
            {

                return math.normalizesafe(normal + deltaNormals[vertexIndex] * weight);

            }

            public float3 GetTransformedSurfaceNormal(float3 normal, int vertexIndex, float weight = 1)
            {

                return math.normalizesafe(normal + deltaRecalculatedNormals[vertexIndex] * weight);

            }

        }

        [Serializable]
        public class SurfaceShapeData
        {

            public string surfaceName;
            public int vertexCount;

            public ShapeData shapeMuscleFit;
            public ShapeData shapeMuscleMax;
            public ShapeData shapeMuscleFlex;
            public ShapeData shapeBreastFull;
            public ShapeData shapeBreastFullMuscleMax;

        }

        public MuscleGroup midpointMuscleGroup;

        public MuscleGroup[] muscleGroups;

        public SurfaceShapeData[] surfaceShapeData;

        public int GetSurfaceShapeDataIndex(string surfaceName)
        {

            if (surfaceShapeData == null) return -1;

            for (int a = 0; a < surfaceShapeData.Length; a++) if (surfaceShapeData[a].surfaceName == surfaceName) return a;

            return -1;

        }

        public MuscleGroup AddOrGetMuscleGroup(string name, out int index, int mirroredIndex = -1)
        {

            if (muscleGroups == null) muscleGroups = new MuscleGroup[0];

            for (int a = 0; a < muscleGroups.Length; a++)
            {

                var mg = muscleGroups[a];

                if (mg == null) continue;

                index = a;

                if (mg.name == name) return mg;

            }

            MuscleGroup[] temp = new MuscleGroup[muscleGroups.Length + 1];
            muscleGroups.CopyTo(temp, 0);

            MuscleGroup newGroup = new MuscleGroup() { name = name };
            temp[muscleGroups.Length] = newGroup;

            muscleGroups = temp;
            index = temp.Length - 1;

            if (mirroredIndex >= 0)
            {

                newGroup.mirroredIndex = mirroredIndex;
                muscleGroups[mirroredIndex].mirroredIndex = index;

            }
            else
            {

                newGroup.mirroredIndex = -1;

            }

            return newGroup;

        }

        public void AddSurfaceShapeData(SurfaceShapeData data)
        {

            if (surfaceShapeData == null) surfaceShapeData = new SurfaceShapeData[0];

            SurfaceShapeData[] temp = new SurfaceShapeData[surfaceShapeData.Length + 1];
            surfaceShapeData.CopyTo(temp, 0);

            temp[surfaceShapeData.Length] = data;

            surfaceShapeData = temp;

        }

    }

}

#endif