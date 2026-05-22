#if UNITY_2017_1_OR_NEWER

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole.API.Unity
{
    public class MorphingBezierCurve : IDisposable
    {
        public struct WeightedCurve
        {
            public IBezierCurve curve;
            public float weight;
        }
        public struct WeightedVertices
        {
            public Vector3[] vertices;
            public float weight;
        }

        private bool updateUnderlyingVertexData;
        private WeightedCurve[] curves;
        private WeightedVertices[] cachedVertices;
        private int vertexCount;
        public int CurveCount => curves == null ? 0 : curves.Length;
        public float GetWeight(int curveIndex) => curves[Mathf.Clamp(curveIndex, 0, CurveCount - 1)].weight;
        public void GetInterpolation(float weight, out int curveIndexA, out float mixA, out int curveIndexB, out float mixB)
        {
            curveIndexA = -1;
            mixA = 0f;
            curveIndexB = -1;
            mixB = 0f;
            if (curves == null || curves.Length == 0) return;

            int lastIndex = curves.Length - 1;
            for (int i = 0; i < curves.Length; i++)
            {
                if (weight <= curves[i].weight || i >= lastIndex)
                {
                    if (i > 0)
                    {
                        curveIndexB = i;
                        curveIndexA = i - 1;
                        mixB = Mathf.Clamp01((weight - curves[i - 1].weight) / (curves[i].weight - curves[i - 1].weight));
                        mixA = 1f - mixB;
                    }
                    else
                    {
                        curveIndexA = i;
                        mixA = 1f;
                    }

                    break;
                }
            }
        }

        public void Dispose()
        {
            if (updateUnderlyingVertexData && curves != null)
            {
                for (int a = 0; a < curves.Length; a++)
                {
                    var c = curves[a];
                    if (c.curve != null)
                    {
                        try
                        {
                            c.curve.Dispose();
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                            // swallow to keep behavior robust in editor / runtime mixes
                        }
                    }
                }
            }
        }

        private static readonly List<WeightedCurve> tempWeightedCurves = new List<WeightedCurve>();
        private static int SortWeightedCurves(WeightedCurve a, WeightedCurve b) => a.weight.CompareTo(b.weight);
        public MorphingBezierCurve(IEnumerable<WeightedCurve> curves_, bool updateUnderlyingVertexData = true)
        {
            if (curves_ == null) return;

            tempWeightedCurves.Clear();
            tempWeightedCurves.AddRange(curves_);
            tempWeightedCurves.Sort(SortWeightedCurves);
            this.curves = tempWeightedCurves.ToArray();
            tempWeightedCurves.Clear();

            this.updateUnderlyingVertexData = updateUnderlyingVertexData;
            if (updateUnderlyingVertexData)
            {
                for (int a = 0; a < this.curves.Length; a++)
                {
                    var c = this.curves[a];
                    if (c.curve != null)
                    {
                        c.curve = (IBezierCurve)c.curve.Clone();
                    }
                    this.curves[a] = c;
                }

                CacheVertices();
            }
        }

        // Helper: build cumulative normalized lengths for a vertex array
        private float[] BuildCumulative(Vector3[] pts)
        {
            int n = pts.Length;
            float[] cum = new float[Math.Max(1, n)];
            if (n <= 1)
            {
                cum[0] = 0f;
                return cum;
            }

            float total = 0f;
            float[] seg = new float[n - 1];
            for (int j = 0; j < n - 1; j++)
            {
                float d = Vector3.Distance(pts[j], pts[j + 1]);
                seg[j] = d;
                total += d;
            }

            float running = 0f;
            cum[0] = 0f;
            for (int j = 1; j < n; j++)
            {
                running += (total > 0f) ? (seg[j - 1] / total) : (1f / (n - 1));
                cum[j] = running;
            }

            return cum;
        }
        public void CacheVertices()
        {
            if (curves == null || curves.Length == 0) 
            {
                cachedVertices = null;
                vertexCount = 0;
                return;
            }

            // First, gather all original vertex arrays and determine max count
            Vector3[][] originalVerts = new Vector3[curves.Length][];
            int maxCount = 0;
            for (int i = 0; i < curves.Length; i++)
            {
                var c = curves[i];
                Vector3[] verts = c.curve != null ? c.curve.GetVertices() : null;
                if (verts == null) verts = new Vector3[0];
                originalVerts[i] = verts;
                if (verts.Length > maxCount) maxCount = verts.Length;
            }

            if (maxCount == 0)
            {
                // no vertices on any curve
                cachedVertices = new WeightedVertices[curves.Length];
                for (int i = 0; i < curves.Length; i++)
                {
                    cachedVertices[i].vertices = new Vector3[0];
                    cachedVertices[i].weight = curves[i].weight;
                }
                vertexCount = 0;
                return;
            }

            cachedVertices = new WeightedVertices[curves.Length];
            int[][] originalVertexIndices = updateUnderlyingVertexData ? new int[curves.Length][] : null;

            // For each curve, produce a vertex array of length maxCount.
            for (int i = 0; i < curves.Length; i++)
            {
                Vector3[] verts = originalVerts[i];
                int origCount = verts.Length;
                Vector3[] newVerts = new Vector3[maxCount];

                // If there's nothing or only one vertex, fill with zeros or that one point
                if (origCount == 0)
                {
                    for (int k = 0; k < maxCount; k++) newVerts[k] = Vector3.zero;
                }
                else if (origCount == 1)
                {
                    for (int k = 0; k < maxCount; k++) newVerts[k] = verts[0];
                }
                else if (origCount == maxCount)
                {
                    // Already matches
                    Array.Copy(verts, newVerts, maxCount);
                }
                else
                {
                    // Determine "next" curve to bias where inserted vertices go (if available)
                    int nextIndex = (i + 1 < curves.Length) ? i + 1 : -1;
                    Vector3[] nextVerts = (nextIndex >= 0) ? originalVerts[nextIndex] : null;
                    float[] nextCum = null;
                    int nextCount = 0;
                    if (nextVerts != null && nextVerts.Length > 1)
                    {
                        nextCum = BuildCumulative(nextVerts);
                        nextCount = nextVerts.Length;
                    }

                    // For each target position k (0..maxCount-1) compute a desired normalized parameter.
                    for (int k = 0; k < maxCount; k++)
                    {
                        float desiredNorm = (maxCount == 1) ? 0f : (float)k / (maxCount - 1);

                        // If we have next curve info, try to use its cumulative distribution to choose a parameter
                        float sampleT;
                        if (nextCum != null && nextCum.Length >= 2)
                        {
                            // Find where desiredNorm fits into nextCum (which is normalized 0..1 along next curve)
                            int segIndex = 0;
                            while (segIndex < nextCum.Length - 1 && desiredNorm > nextCum[segIndex + 1]) segIndex++;

                            if (segIndex >= nextCum.Length - 1)
                            {
                                sampleT = 1f;
                            }
                            else
                            {
                                float segStart = nextCum[segIndex];
                                float segEnd = nextCum[segIndex + 1];
                                float local = (segEnd - segStart) > 1e-6f ? (desiredNorm - segStart) / (segEnd - segStart) : 0f;
                                // Translate to parameter along next curve vertex indices, then reuse that parameter to sample this curve
                                sampleT = ((float)segIndex + local) / (nextCount - 1);
                            }
                        }
                        else
                        {
                            // fallback: uniform distribution
                            sampleT = desiredNorm;
                        }

                        // Now sample this curve's discrete vertex array at parameter sampleT by linear interpolation between orig indices
                        float pos = sampleT * (origCount - 1);
                        int i0 = Mathf.Clamp((int)Mathf.Floor(pos), 0, origCount - 1);
                        int i1 = Mathf.Clamp(i0 + 1, 0, origCount - 1);
                        float a = (i0 == i1) ? 0f : (pos - i0);

                        newVerts[k] = Vector3.Lerp(verts[i0], verts[i1], a);
                    }
                }

                // If requested, build a one-to-one mapping from new vertices back to original indices.
                int[] mapping = null;
                if (updateUnderlyingVertexData)
                {
                    mapping = new int[maxCount];
                    for (int m = 0; m < maxCount; m++) mapping[m] = -1;

                    // If there are original vertices, perform greedy one-to-one minimal distance matching.
                    if (origCount > 0)
                    {
                        var pairs = new List<(int newI, int oldJ, float dist)>(maxCount * origCount);
                        for (int ni = 0; ni < maxCount; ni++)
                        {
                            for (int oi = 0; oi < origCount; oi++)
                            {
                                float d = Vector3.SqrMagnitude(newVerts[ni] - verts[oi]);
                                pairs.Add((ni, oi, d));
                            }
                        }
                        pairs.Sort((a, b) => a.dist.CompareTo(b.dist));

                        bool[] newAssigned = new bool[maxCount];
                        bool[] oldAssigned = new bool[origCount];

                        foreach (var p in pairs)
                        {
                            if (!newAssigned[p.newI] && !oldAssigned[p.oldJ])
                            {
                                mapping[p.newI] = p.oldJ;
                                newAssigned[p.newI] = true;
                                oldAssigned[p.oldJ] = true;
                            }
                        }
                        // any remaining mapping entries remain -1
                    }

                    originalVertexIndices[i] = mapping;
                }

                cachedVertices[i].vertices = newVerts;
                cachedVertices[i].weight = curves[i].weight;

                // After building cached array and mapping, update underlying curve if desired.
                if (updateUnderlyingVertexData)
                {
                    try
                    {
                        var targetCurve = curves[i].curve;
                        if (targetCurve != null)
                        {
                            targetCurve.SetVertices(newVerts, mapping);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        // swallow to keep behavior robust in editor / runtime mixes
                    }
                }
            }

            vertexCount = maxCount;
        }

        public Vector3[] GetVertices(float morphWeight, Vector3[] vertexArray = null)
        {
            if (cachedVertices == null) CacheVertices();
            if (vertexArray == null) vertexArray = new Vector3[vertexCount];
            for(int i = 0; i < vertexCount; i++)
            {
                vertexArray[i] = GetVertex(i, morphWeight);
            }
            return vertexArray;
        }
        public Vector2[] GetVertices2D(float morphWeight, Vector2[] vertexArray = null)
        {
            if (cachedVertices == null) CacheVertices();
            if (vertexArray == null) vertexArray = new Vector2[vertexCount];
            for (int i = 0; i < vertexCount; i++)
            {
                vertexArray[i] = GetVertex2D(i, morphWeight);
            }
            return vertexArray;
        }
        public int VertexCount
        {
            get
            {
                if (cachedVertices == null) CacheVertices();
                return vertexCount;
            }
        }
        public Vector3 GetVertex(int index, float morphWeight)
        {
            if (cachedVertices == null) CacheVertices();
            GetInterpolation(morphWeight, out int cA, out float mA, out int cB, out float mB);

            Vector3 vertex = Vector3.zero;
            if (cA >= 0)
            {
                vertex += cachedVertices[cA].vertices[index] * mA;
            }
            if (cB >= 0)
            {
                vertex += cachedVertices[cB].vertices[index] * mB;
            }

            return vertex;
        }
        public Vector2 GetVertex2D(int index, float morphWeight) => GetVertex(index, morphWeight);

        public Vector3 GetPositionOnCurve(float normalizedPos, float morphWeight, SwoleEndOfPathInstruction endOfPathInstruction = SwoleEndOfPathInstruction.Stop)
        {
            GetInterpolation(morphWeight, out int cA, out float mA, out int cB, out float mB);

            Vector3 pos = Vector3.zero;
            if (cA >= 0)
            {
                pos += curves[cA].curve.GetPositionOnCurve(normalizedPos, endOfPathInstruction) * mA;
            }
            if (cB >= 0)
            {
                pos += curves[cB].curve.GetPositionOnCurve(normalizedPos, endOfPathInstruction) * mB;
            }

            return pos;
        }
        public Vector2 GetPositionOnCurve2D(float normalizedPos, float morphWeight, SwoleEndOfPathInstruction endOfPathInstruction = SwoleEndOfPathInstruction.Stop)
        {
            GetInterpolation(morphWeight, out int cA, out float mA, out int cB, out float mB);

            Vector2 pos = Vector2.zero;
            if (cA >= 0)
            {
                pos += curves[cA].curve.GetPositionOnCurve2D(normalizedPos, endOfPathInstruction) * mA;
            }
            if (cB >= 0)
            {
                pos += curves[cB].curve.GetPositionOnCurve2D(normalizedPos, endOfPathInstruction) * mB;
            }

            return pos;
        }
        public float GetEstimatedLength(float morphWeight)
        {
            GetInterpolation(morphWeight, out int cA, out float mA, out int cB, out float mB);

            float length = 0f;
            if (cA >= 0)
            {
                length += curves[cA].curve.EstimatedLength * mA;
            }
            if (cB >= 0)
            {
                length += curves[cB].curve.EstimatedLength * mB;
            }

            return length;
        }
        public Vector3 GetPositionFromDistanceAlongCurve(float distance, float morphWeight, SwoleEndOfPathInstruction endOfPathInstruction = SwoleEndOfPathInstruction.Stop)
        {
            GetInterpolation(morphWeight, out int cA, out float mA, out int cB, out float mB);

            Vector3 pos = Vector3.zero;
            if (cA >= 0)
            {
                pos += curves[cA].curve.GetPositionFromDistanceAlongCurve(distance, endOfPathInstruction) * mA;
            }
            if (cB >= 0)
            {
                pos += curves[cB].curve.GetPositionFromDistanceAlongCurve(distance, endOfPathInstruction) * mB;
            }

            return pos;
        }
        public Vector2 GetPositionFromDistanceAlongCurve2D(float distance, float morphWeight, SwoleEndOfPathInstruction endOfPathInstruction = SwoleEndOfPathInstruction.Stop)
        {
            GetInterpolation(morphWeight, out int cA, out float mA, out int cB, out float mB);

            Vector2 pos = Vector2.zero;
            if (cA >= 0)
            {
                pos += curves[cA].curve.GetPositionFromDistanceAlongCurve2D(distance, endOfPathInstruction) * mA;
            }
            if (cB >= 0)
            {
                pos += curves[cB].curve.GetPositionFromDistanceAlongCurve2D(distance, endOfPathInstruction) * mB;
            }

            return pos;
        }

        /// <summary>
        /// Gets point on path based on 'time' (where 0 is start, and 1 is end of path).
        /// </summary>
        public Vector3 GetPointAtTime(float t, float morphWeight, SwoleEndOfPathInstruction endOfPathInstruction = SwoleEndOfPathInstruction.Stop)
        {
            GetInterpolation(morphWeight, out int cA, out float mA, out int cB, out float mB);

            Vector3 pos = Vector3.zero;
            if (cA >= 0)
            {
                pos += curves[cA].curve.GetPointAtTime(t, endOfPathInstruction) * mA;
            }
            if (cB >= 0)
            {
                pos += curves[cB].curve.GetPointAtTime(t, endOfPathInstruction) * mB;
            }

            return pos;
        }

        /// <summary>
        /// Gets forward direction on path based on 'time' (where 0 is start, and 1 is end of path).
        /// </summary>
        public Vector3 GetDirection(float t, float morphWeight, SwoleEndOfPathInstruction endOfPathInstruction = SwoleEndOfPathInstruction.Stop)
        {
            GetInterpolation(morphWeight, out int cA, out float mA, out int cB, out float mB);

            Vector3 dir = Vector3.zero;
            if (cA >= 0)
            {
                dir += curves[cA].curve.GetDirection(t, endOfPathInstruction) * mA;
            }
            if (cB >= 0)
            {
                dir += curves[cB].curve.GetDirection(t, endOfPathInstruction) * mB;
            }

            return dir;
        }

        /// <summary>
        /// Gets normal vector on path based on 'time' (where 0 is start, and 1 is end of path).
        /// </summary>
        public Vector3 GetNormal(float t, float morphWeight, SwoleEndOfPathInstruction endOfPathInstruction = SwoleEndOfPathInstruction.Stop)
        {
            GetInterpolation(morphWeight, out int cA, out float mA, out int cB, out float mB);

            Vector3 n = Vector3.zero;
            if (cA >= 0)
            {
                n += curves[cA].curve.GetNormal(t, endOfPathInstruction) * mA;
            }
            if (cB >= 0)
            {
                n += curves[cB].curve.GetNormal(t, endOfPathInstruction) * mB;
            }

            return n;
        }

        /// <summary>
        /// Gets a rotation that will orient an object in the direction of the path at this point, with local up point along the path's normal
        /// </summary>
        public Quaternion GetRotation(float t, float morphWeight, SwoleEndOfPathInstruction endOfPathInstruction = SwoleEndOfPathInstruction.Stop)
        {
            GetInterpolation(morphWeight, out int cA, out float mA, out int cB, out float mB);

            if (cA >= 0 && cB >= 0)
            {
                var ra = curves[cA].curve.GetRotation(t, endOfPathInstruction);
                var rb = curves[cB].curve.GetRotation(t, endOfPathInstruction);
                // blend using the second-curve weight so that mB==0 -> ra, mB==1 -> rb
                return Quaternion.Slerp(ra, rb, mB);
            }
            else if (cA >= 0)
            {
                return curves[cA].curve.GetRotation(t, endOfPathInstruction);
            }
            else if (cB >= 0)
            {
                return curves[cB].curve.GetRotation(t, endOfPathInstruction);
            }

            return Quaternion.identity;
        }

        /// <summary>
        /// Finds the closest point on the path from any point in the world
        /// </summary>
        public Vector3 GetClosestPointOnPath(Vector3 worldPoint, float morphWeight)
        {
            GetInterpolation(morphWeight, out int cA, out float mA, out int cB, out float mB);

            Vector3 v = Vector3.zero;
            if (cA >= 0)
            {
                v += curves[cA].curve.GetClosestPointOnPath(worldPoint) * mA;
            }
            if (cB >= 0)
            {
                v += curves[cB].curve.GetClosestPointOnPath(worldPoint) * mB;
            }

            return v;
        }

        /// <summary>
        /// Finds the 'time' (0=start of path, 1=end of path) along the path that is closest to the given point
        /// </summary>
        public float GetClosestTimeOnPath(Vector3 worldPoint, float morphWeight)
        {
            GetInterpolation(morphWeight, out int cA, out float mA, out int cB, out float mB);

            float t = 0f;
            if (cA >= 0)
            {
                t += curves[cA].curve.GetClosestTimeOnPath(worldPoint) * mA;
            }
            if (cB >= 0)
            {
                t += curves[cB].curve.GetClosestTimeOnPath(worldPoint) * mB;
            }

            return t;
        }

        /// <summary>
        /// Finds the distance along the path that is closest to the given point
        /// </summary>
        public float GetClosestDistanceAlongPath(Vector3 worldPoint, float morphWeight)
        {
            GetInterpolation(morphWeight, out int cA, out float mA, out int cB, out float mB);

            float d = 0f;
            if (cA >= 0)
            {
                d += curves[cA].curve.GetClosestDistanceAlongPath(worldPoint) * mA;
            }
            if (cB >= 0)
            {
                d += curves[cB].curve.GetClosestDistanceAlongPath(worldPoint) * mB;
            }

            return d;
        }

    }
}

#endif