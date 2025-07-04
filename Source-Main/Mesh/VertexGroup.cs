using System;
using System.Collections.Generic;

#if (UNITY_STANDALONE || UNITY_EDITOR)
using UnityEngine; 
#endif

namespace Swole
{

    [Serializable]
    public class VertexGroup
    {

#if (UNITY_STANDALONE || UNITY_EDITOR)
        /// <summary>
        /// Creates a new vertex group instance using blend shape delta vertex magnitudes as the weights.
        /// </summary>
        /// <param name="normalize">Should the weights be normalized to a range of [0, 1]?</param>
        /// <param name="keyword">An optional keyword to remove from the vertex group's name, which is set to the name of the input shape.</param>
        /// <param name="threshold">The minimum magnitude of a delta vertex required to have a weight greater than zero.</param>
        /// <param name="normalizationSetMaxWeight">If set to a value above zero, it will be used as the maximum weight in the group during normalization.</param>
        /// <returns></returns>
        public static VertexGroup ConvertToVertexGroup(BlendShape shape, bool normalize = true, string keyword = "", float threshold = 0.0001f, float normalizationSetMaxWeight = 0, bool clampWeights = true)
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

                            if (normalizationSetMaxWeight <= 0 && cw > maxWeight) maxWeight = cw;

                            weightDic[c] = cw;

                        }

                    }

                }

                List<int> indices = new List<int>();
                List<float> weights = new List<float>();

                if (normalize && maxWeight > 0)
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

#if (UNITY_STANDALONE || UNITY_EDITOR)
        [SerializeField/*, HideInInspector*/]
#endif
        protected List<int> indices;

        public int EntryIndexOf(int vertexIndex)
        {
            if (indices == null) return -1;
            return indices.IndexOf(vertexIndex); 
        }

#if (UNITY_STANDALONE || UNITY_EDITOR)
        [SerializeField/*, HideInInspector*/]
#endif
        protected List<float> weights;

        public void Normalize(float maxWeight = 0)
        {
            if (maxWeight == 0) for (int a = 0; a < weights.Count; a++) maxWeight = Mathf.Max(maxWeight, weights[a]);          
            if (maxWeight != 0) for (int a = 0; a < weights.Count; a++) weights[a] = weights[a] / maxWeight;
        }
        public void Add(VertexGroup group, bool limitMaxWeight = false, float maxWeight = 1, float multiplier = 1)
        {
            using(var enu1 = group.indices.GetEnumerator())
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
            indices.Clear();
            weights.Clear();
        }

        public int EntryCount => indices == null ? 0 : indices.Count;
        public int GetEntryIndex(int entryIndex) => indices[entryIndex];
        public float GetEntryWeight(int entryIndex) => weights[entryIndex];

        public void SetEntryIndex(int entryIndex, int vertexIndex) => indices[entryIndex] = vertexIndex;
        public void SetEntryWeight(int entryIndex, float weight) => weights[entryIndex] = weight;
        public void RemoveEntry(int entryIndex)
        {
            if (entryIndex < 0 || entryIndex >= indices.Count) return;

            indices.RemoveAt(entryIndex);
            weights.RemoveAt(entryIndex);
        }

        public float GetWeight(int vertexIndex)
        {
            for (int a = 0; a < indices.Count; a++)
            {
                if (indices[a] == vertexIndex) return weights[a];
            }

            return 0f;
        }
        public void SetWeight(int vertexIndex, float weight)
        {
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
        }
        public bool RemoveWeight(int vertexIndex)
        {
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

        public VertexGroup(string name) : this(name, new List<int>(), new List<float>()) {}
        protected VertexGroup(string name, List<int> indices, List<float> weights)
        {
            this.name = name;
            this.indices = indices;
            this.weights = weights;
        }

        public float[] AsLinearWeightArray(int vertexCount)
        {
            float[] weights = new float[vertexCount];
            for (int a = 0; a < indices.Count; a++)
            {
                int index = indices[a];
                if (index < 0 || index >= weights.Length) continue; 

                weights[index] = this.weights[a];
            }

            return weights;
        }
        public IList<float> AsLinearWeightList(int vertexCount, IList<float> outputList = null)
        {
            if (outputList == null) outputList = new List<float>(vertexCount);
            int indexOffset = outputList.Count;
            for (int a = 0; a < vertexCount; a++) outputList.Add(0); 
            for (int a = 0; a < indices.Count; a++)
            {
                int index = indices[a];
                if (index < 0 || index + indexOffset >= outputList.Count) continue; 

                outputList[index + indexOffset] = this.weights[a];
            }

            return weights;
        }
        public float[] AsLinearWeightArray(float[] array, bool clearArray = true)
        {
            if (clearArray) for (int a = 0; a < array.Length; a++) array[a] = 0; // clear the array first to avoid leftover data if reusing the array
            for (int a = 0; a < indices.Count; a++)
            {
                int index = indices[a];
                if (index < 0 || index >= array.Length) continue;

                array[index] = this.weights[a]; 
            }

            return array;
        }

    }

}