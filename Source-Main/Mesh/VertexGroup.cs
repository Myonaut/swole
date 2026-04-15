using System;
using System.Collections.Generic;

#if (UNITY_STANDALONE || UNITY_EDITOR)
using UnityEngine;
using Unity.Collections;
#endif

namespace Swole
{

    [Serializable, NonAnimatable]
    public class VertexGroup
    {

#if (UNITY_STANDALONE || UNITY_EDITOR)
        public static float[] ConvertToVertexWeightArray(BlendShape shape, bool normalize = true, string keyword = "", float threshold = 0.0001f, float normalizationSetMaxWeight = 0f, bool clampWeights = true)
        {
            return ConvertToVertexWeightArray(shape, null, normalize, keyword, threshold, normalizationSetMaxWeight, clampWeights);
        }
        public static float[] ConvertToVertexWeightArray(BlendShape shape, float[] weightsArray, bool normalize = true, string keyword = "", float threshold = 0.0001f, float normalizationSetMaxWeight = 0f, bool clampWeights = true)
        {
            float maxWeight = normalizationSetMaxWeight;
            if (shape.frames != null && shape.frames.Length > 0)
            {

                Dictionary<int, float> weightDic = new Dictionary<int, float>();

                for (int b = 0; b < shape.frames.Length; b++)
                {

                    BlendShape.Frame frame = shape.frames[b];

                    for (int c = 0; c < frame.deltaVertices.Length; c++)
                    {

                        float mag = frame.deltaVertices[c].magnitude;

                        if (mag - threshold > 0)
                        {
                            weightDic.TryGetValue(c, out float w);

                            float cw = w + mag;

                            if (normalizationSetMaxWeight <= 0f && cw > maxWeight) maxWeight = cw;

                            weightDic[c] = cw;

                        }

                    }

                }

                if (weightsArray == null) weightsArray = new float[shape.frames[0].deltaVertices.Length];

                if (normalize && maxWeight > 0f)
                {
                    foreach (var weight in weightDic)
                    {
                        weightsArray[weight.Key] = (clampWeights ? Mathf.Clamp01(weight.Value / maxWeight) : (weight.Value / maxWeight));
                    }
                }
                else
                {
                    foreach (var weight in weightDic)
                    {
                        weightsArray[weight.Key] = weight.Value;
                    }
                }

            }

            return weightsArray;

        }

        /// <summary>
        /// Creates a new vertex group instance using blend shape delta vertex magnitudes as the weights.
        /// </summary>
        /// <param name="normalize">Should the weights be normalized to a range of [0, 1]?</param>
        /// <param name="keyword">An optional keyword to remove from the vertex group's name, which is set to the name of the input shape.</param>
        /// <param name="threshold">The minimum magnitude of a delta vertex required to have a weight greater than zero.</param>
        /// <param name="normalizationSetMaxWeight">If set to a value above zero, it will be used as the maximum weight in the group during normalization.</param>
        /// <returns></returns>
        public static VertexGroup ConvertToVertexGroup(BlendShape shape, bool normalize = true, string keyword = "", float threshold = 0.0001f, float normalizationSetMaxWeight = 0f, bool clampWeights = true)
        {

            float maxWeight = normalizationSetMaxWeight;
            if (shape.frames != null && shape.frames.Length > 0)
            {

                Dictionary<int, float> weightDic = new Dictionary<int, float>();

                for (int b = 0; b < shape.frames.Length; b++)
                {

                    BlendShape.Frame frame = shape.frames[b];

                    for (int c = 0; c < frame.deltaVertices.Length; c++)
                    {

                        float mag = frame.deltaVertices[c].magnitude;

                        if (mag - threshold > 0)
                        {
                            weightDic.TryGetValue(c, out float w);

                            float cw = w + mag;

                            if (normalizationSetMaxWeight <= 0f && cw > maxWeight) maxWeight = cw;

                            weightDic[c] = cw;

                        }

                    }

                }

                List<int> indices = new List<int>();
                List<float> weights = new List<float>();

                if (normalize && maxWeight > 0f)
                {
                    foreach (var weight in weightDic)
                    {

                        indices.Add(weight.Key);
                        weights.Add(clampWeights ? Mathf.Clamp01(weight.Value / maxWeight) : (weight.Value / maxWeight));

                    }
                }
                else
                {
                    foreach (var weight in weightDic)
                    {

                        indices.Add(weight.Key);
                        weights.Add(weight.Value);

                    }
                }

                if (indices.Count > 0)
                {

                    VertexGroup group = new VertexGroup(string.IsNullOrEmpty(keyword) || string.IsNullOrEmpty(shape.name) ? shape.name : shape.name.Replace(keyword, ""), indices, weights);

                    return group;

                }

            }

            return new VertexGroup(string.IsNullOrEmpty(keyword) || string.IsNullOrEmpty(shape.name) ? shape.name : shape.name.Replace(keyword, ""), new List<int>(), new List<float>());

        }

        public BlendShape AsBlendShape(int vertexCount, float frameWeight = 1f)
        {
            var deltaV = new Vector3[vertexCount];
            var deltaN = new Vector3[vertexCount];
            var deltaT = new Vector3[vertexCount];

            for(int i = 0; i < EntryCount; i++)
            {
                int vertexIndex = GetEntryIndex(i);
                if (vertexIndex < 0 || vertexIndex >= vertexCount) continue;

                float weight = GetEntryWeight(i);
                deltaV[vertexIndex] += new Vector3(0f, 0f, weight); 
            }

            var shape = new BlendShape(name);
            shape.AddFrame(frameWeight, deltaV, deltaN, deltaT);

            return shape;
        }
#endif

        public string name;

        /// <summary>
        /// Convenience field for storing a string
        /// </summary>
        public string tag;

        /// <summary>
        /// Convenience field for storing an index
        /// </summary>
        public int index;

        /// <summary>
        /// Convenience field for storing a boolean value
        /// </summary>
        public bool flag;

        [Serializable]
        public enum SampleSource
        {
            Default,
            ExternalArray,
            ExternalNativeArray
        }

#if (UNITY_STANDALONE || UNITY_EDITOR)
        [SerializeField]
#endif
        protected SampleSource weightSampleSource = SampleSource.Default;
        public SampleSource WeightSampleSource => weightSampleSource;
#if (UNITY_STANDALONE || UNITY_EDITOR)
        [SerializeField]
#endif
        protected int weightSampleSourceStartIndex;
        public int WeightSampleSourceStartIndex => weightSampleSourceStartIndex;
#if (UNITY_STANDALONE || UNITY_EDITOR)
        [SerializeField]
#endif
        protected int weightSampleSourceSize;
        public int WeightSampleSourceSize => weightSampleSourceSize;

        protected float[] externalWeights;
#if (UNITY_STANDALONE || UNITY_EDITOR)
        protected NativeArray<float> externalNativeWeights;
#endif

        public void SetExternalWeightSource(float[] weights, int startIndex = 0, int size = -1, bool clearDefaultLists = false, bool nullifyDefaultLists = false)
        {
            weightSampleSource = SampleSource.ExternalArray;
            weightSampleSourceStartIndex = startIndex;
            weightSampleSourceSize = size < 0 ? weights.Length - startIndex : size;
            externalWeights = weights;

            if (clearDefaultLists)
            {
                this.indices.Clear();
                this.weights.Clear();
            }
            if (nullifyDefaultLists)
            {
                this.indices = null;
                this.weights = null;
            }
        }

#if (UNITY_STANDALONE || UNITY_EDITOR)
        public void SetExternalWeightSource(NativeArray<float> weights, int startIndex = 0, int size = -1, bool clearDefaultLists = false, bool nullifyDefaultLists = false)
        {
            weightSampleSource = SampleSource.ExternalNativeArray;
            weightSampleSourceStartIndex = startIndex;
            weightSampleSourceSize = size < 0 ? weights.Length - startIndex : size;
            externalNativeWeights = weights;

            if (clearDefaultLists)
            {
                this.indices.Clear();
                this.weights.Clear();
            }
            if (nullifyDefaultLists)
            {
                this.indices = null;
                this.weights = null;
            }
        }
#endif

#if (UNITY_STANDALONE || UNITY_EDITOR)
        [SerializeField, HideInInspector]
#endif
        protected List<int> indices;

        public int EntryIndexOf(int vertexIndex)
        {
            if (indices == null) return -1;
            return indices.IndexOf(vertexIndex);
        }

#if (UNITY_STANDALONE || UNITY_EDITOR)
        [SerializeField, HideInInspector]
#endif
        protected List<float> weights;

        public void Normalize(float maxWeight = 0f)
        {
            if (maxWeight == 0f) for (int a = 0; a < weights.Count; a++) maxWeight = Mathf.Max(maxWeight, weights[a]);
            if (maxWeight != 0f) for (int a = 0; a < weights.Count; a++) weights[a] = weights[a] / maxWeight;
        }
        public void Clamp(float minWeight, float maxWeight)
        {
            for (int a = 0; a < weights.Count; a++) weights[a] = Mathf.Clamp(weights[a], minWeight, maxWeight);
        }
        public void Add(VertexGroup group, bool limitMaxWeight = false, float maxWeight = 1, float multiplier = 1)
        {
            if (weightSampleSource != SampleSource.Default) 
            { 
                swole.LogWarning($"VertexGroup.Add() operation is not supported when using an external weight source (VertexGroup: {name}).");
                return;
            }

            using (var enu1 = group.indices.GetEnumerator())
            {
                using (var enu2 = group.weights.GetEnumerator())
                {
                    while (enu1.MoveNext() && enu2.MoveNext())
                    {
                        var ind = enu1.Current;
                        var weight = enu2.Current * multiplier;

                        var entryIndex = EntryIndexOf(ind);
                        if (entryIndex < 0)
                        {
                            SetWeight(ind, limitMaxWeight ? Mathf.Min(weight, maxWeight) : weight);
                        }
                        else
                        {
                            weight = weight + GetEntryWeight(entryIndex);
                            SetEntryWeight(entryIndex, limitMaxWeight ? Mathf.Min(weight, maxWeight) : weight);
                        }
                    }
                }
            }
        }
        public void Subtract(VertexGroup group, bool limitMinWeight = true, float minWeight = 0, float multiplier = 1)
        {
            if (weightSampleSource != SampleSource.Default)
            {
                swole.LogWarning($"VertexGroup.Subtract() operation is not supported when using an external weight source (VertexGroup: {name}).");
                return;
            }

            using (var enu1 = group.indices.GetEnumerator())
            {
                using (var enu2 = group.weights.GetEnumerator())
                {
                    while (enu1.MoveNext() && enu2.MoveNext())
                    {
                        var ind = enu1.Current;
                        var weight = -(enu2.Current * multiplier);

                        var entryIndex = EntryIndexOf(ind);
                        if (entryIndex < 0)
                        {
                            SetWeight(ind, limitMinWeight ? Mathf.Max(weight, minWeight) : weight);
                        }
                        else
                        {
                            weight = weight + GetEntryWeight(entryIndex);
                            SetEntryWeight(entryIndex, limitMinWeight ? Mathf.Max(weight, minWeight) : weight);
                        }
                    }
                }
            }
        }

        public void Clear()
        {
            if (weightSampleSource != SampleSource.Default)
            {
                swole.LogWarning($"VertexGroup.Clear() is not supported when using an external weight source (VertexGroup: {name}).");
                return;
            }

            indices.Clear();
            weights.Clear();
        }

        public int EntryCount => indices == null ? 0 : indices.Count;
        public int GetEntryIndex(int entryIndex) => weightSampleSource != SampleSource.Default ? entryIndex : indices[entryIndex];
        public float GetEntryWeight(int entryIndex) => weightSampleSource != SampleSource.Default ? GetWeight(entryIndex) : weights[entryIndex];
        public void GetEntry(int entryIndex, out int vertexIndex, out float weight)
        {
            if (weightSampleSource != SampleSource.Default)
            {
                vertexIndex = entryIndex;
                weight = GetWeight(entryIndex);
                return;
            }

            vertexIndex = -1;
            weight = 0f;
            if (entryIndex < 0 || entryIndex >= indices.Count) return;

            vertexIndex = indices[entryIndex];
            weight = weights[entryIndex];
        }

        public void SetEntryIndex(int entryIndex, int vertexIndex)
        {
            if (weightSampleSource != SampleSource.Default) return;
            indices[entryIndex] = vertexIndex;
        }
        public void SetEntryWeight(int entryIndex, float weight)
        {
            if (weightSampleSource != SampleSource.Default)
            {
                SetWeight(entryIndex, weight);
                return;
            }

            weights[entryIndex] = weight;
        }
        public void RemoveEntry(int entryIndex)
        {
            if (weightSampleSource != SampleSource.Default || entryIndex < 0 || entryIndex >= indices.Count) return;

            indices.RemoveAt(entryIndex);
            weights.RemoveAt(entryIndex);
        }
        public void SetEntry(int entryIndex, int vertexIndex, float weight)
        {
            SetEntryIndex(entryIndex, vertexIndex);
            SetEntryWeight(entryIndex, weight); 
        }

        public float GetWeight(int vertexIndex)
        {
            switch(weightSampleSource)
            {
                default:
                    for (int a = 0; a < indices.Count; a++)
                    {
                        if (indices[a] == vertexIndex) return weights[a];
                    }

                    return 0f;

                case SampleSource.ExternalArray:
                    return externalWeights[weightSampleSourceStartIndex + vertexIndex];

#if (UNITY_STANDALONE || UNITY_EDITOR)
                case SampleSource.ExternalNativeArray:
                    return externalNativeWeights[weightSampleSourceStartIndex + vertexIndex];
#endif
            }
        }
        public void SetWeight(int vertexIndex, float weight)
        {
            switch (weightSampleSource)
            {
                default:
                    for (int a = 0; a < indices.Count; a++)
                    {
                        if (indices[a] == vertexIndex)
                        {
                            weights[a] = weight;
                            return;
                        }
                    }

                    indices.Add(vertexIndex);
                    weights.Add(weight);
                    break;

                case SampleSource.ExternalArray:
                    externalWeights[weightSampleSourceStartIndex + vertexIndex] = weight;
                    break;

#if (UNITY_STANDALONE || UNITY_EDITOR)
                case SampleSource.ExternalNativeArray:
                    externalNativeWeights[weightSampleSourceStartIndex + vertexIndex] = weight;
                    break;
#endif
            }
        }
        public bool RemoveWeight(int vertexIndex)
        {
            if (weightSampleSource != SampleSource.Default) return false;

            int ind = indices.IndexOf(vertexIndex);
            if (ind >= 0)
            {
                indices.RemoveAt(ind);
                weights.RemoveAt(ind);

                return true;
            }

            return false;
        }

        public void SetWeights(ICollection<int> indices, ICollection<float> weights)
        {
            using (var iEn = indices.GetEnumerator())
            {
                using (var wEn = weights.GetEnumerator())
                {
                    while (iEn.MoveNext() && wEn.MoveNext())
                    {
                        int ind = iEn.Current;
                        float weight = wEn.Current;
                        SetWeight(ind, weight);
                    }
                }
            }
        }

        public float this[int vertexIndex]
        {
            get => GetWeight(vertexIndex);
            set => SetWeight(vertexIndex, value);
        }

        public VertexGroup(string name) : this(name, new List<int>(), new List<float>()) { }
        protected VertexGroup(string name, List<int> indices, List<float> weights)
        {
            this.name = name;
            this.indices = indices;
            this.weights = weights;
        }

        public VertexGroup(string name, IEnumerable<float> linearWeights, bool ignoreZeroWeights = true)
        {
            this.name = name;

            this.indices = new List<int>();
            this.weights = new List<float>();

            if (linearWeights != null)
            {
                int i = 0;

                if (ignoreZeroWeights)
                {
                    foreach (var weight in linearWeights)
                    {
                        if (weight != 0f)
                        {
                            indices.Add(i);
                            weights.Add(weight);
                        }

                        i++;
                    }
                }
                else
                {
                    foreach (var weight in linearWeights)
                    {
                        indices.Add(i);
                        weights.Add(weight);

                        i++;
                    }
                }
            }
        }

        public float[] AsLinearWeightArray(float[] array, bool clearArray = true, int indexOffset = 0)
        {
            if (clearArray) for (int a = 0; a < array.Length; a++) array[a] = 0f; // clear the array first to avoid leftover data if reusing the array

            switch (weightSampleSource)
            {
                default:
                    for (int a = 0; a < indices.Count; a++)
                    {
                        int index = indices[a] + indexOffset;
                        if (index < 0 || index >= array.Length) continue;

                        array[index] = this.weights[a];
                    }
                    break;

                case SampleSource.ExternalArray:
                    for (int a = 0; a < weightSampleSourceSize; a++)
                    {
                        array[a + indexOffset] = externalWeights[weightSampleSourceStartIndex + a];
                    }
                    break;

#if (UNITY_STANDALONE || UNITY_EDITOR)
                case SampleSource.ExternalNativeArray:
                    for (int a = 0; a < weightSampleSourceSize; a++)
                    {
                        array[a + indexOffset] = externalNativeWeights[weightSampleSourceStartIndex + a];
                    }
                    break;
#endif
            }

            return array;
        }
        public float[] AsLinearWeightArray(int vertexCount)
        {
            float[] weights = new float[vertexCount];
            AsLinearWeightArray(weights, false, 0);
            return weights;
        }
        public IList<float> AsLinearWeightList(int vertexCount, IList<float> outputList = null)
        {
            if (outputList == null) outputList = new List<float>(vertexCount);
            int indexOffset = outputList.Count;
            for (int a = 0; a < vertexCount; a++) outputList.Add(0f);
            switch (weightSampleSource)
            {
                default:
                    for (int a = 0; a < indices.Count; a++)
                    {
                        int index = indices[a];
                        if (index < 0 || index + indexOffset >= outputList.Count) continue;

                        outputList[index + indexOffset] = this.weights[a];
                    }
                    break;
                case SampleSource.ExternalArray:
                    for (int a = 0; a < Mathf.Min(vertexCount, weightSampleSourceSize); a++)
                    {
                        outputList[a + indexOffset] = externalWeights[weightSampleSourceStartIndex + a];
                    }
                    break;

#if (UNITY_STANDALONE || UNITY_EDITOR)
                case SampleSource.ExternalNativeArray:
                    for (int a = 0; a < Mathf.Min(vertexCount, weightSampleSourceSize); a++)
                    {
                        outputList[a + indexOffset] = externalNativeWeights[weightSampleSourceStartIndex + a];
                    }
                    break;
#endif
            }

            return weights;
        }

        public void InsertIntoArray(float[] array, int startIndex) => AsLinearWeightArray(array, false, startIndex);

#if (UNITY_STANDALONE || UNITY_EDITOR)
        public void InsertIntoNativeArray(NativeArray<float> array, int startIndex)
        {
            switch (weightSampleSource)
            {
                default:
                    for (int a = 0; a < indices.Count; a++)
                    {
                        int index = indices[a] + startIndex;
                        if (index < 0 || index >= array.Length) continue;

                        array[index] = this.weights[a];
                    }
                    break;

                case SampleSource.ExternalArray:
                    for (int a = 0; a < weightSampleSourceSize; a++)
                    {
                        array[a + startIndex] = externalWeights[weightSampleSourceStartIndex + a];
                    }
                    break;

#if (UNITY_STANDALONE || UNITY_EDITOR)
                case SampleSource.ExternalNativeArray:
                    for (int a = 0; a < weightSampleSourceSize; a++)
                    {
                        array[a + startIndex] = externalNativeWeights[weightSampleSourceStartIndex + a];
                    }
                    break;
#endif
            }
        }
#endif

    }

}