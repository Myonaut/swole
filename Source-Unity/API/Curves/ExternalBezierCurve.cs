#if UNITY_2017_1_OR_NEWER && BULKOUT_ENV

using System;
using System.Collections.Generic;

using UnityEngine;

using Unity.Mathematics;

using PathCreation;
using PathCreation.Utility;

namespace Swole.API.Unity
{

    public class ExternalBezierCurve : SwoleObject<ICurve, SerializedCurve>, IBezierCurve
    {

        public void Dispose()
        {
            if (path != null) path = null;
            if (vertexPath != null) vertexPath = null;
        }

        public ExternalBezierCurve Duplicate()
        {
            var clone = new ExternalBezierCurve();

            clone.Name = Name;
            clone.SetPoints(GetPoints());

            return clone;
        }
        public object Clone() => Duplicate();

        #region Serialization

        public override string AsJSON(bool prettyPrint = false) => AsSerializableStruct().AsJSON(prettyPrint);
        public override SerializedCurve AsSerializableStruct()
        {
            var serialized = new SerializedCurve();

            serialized.name = Name;
            serialized.curveType = CurveType.Bezier;

            if (path != null)
            {
                serialized.points = new EngineInternal.Vector3[PointCount];
                for (int a = 0; a < serialized.points.Length; a++) serialized.points[a] = UnityEngineHook.AsSwoleVector(GetPoint(a));
            }

            return serialized;
        }

        public ExternalBezierCurve(SerializedCurve serialized) : base(serialized)
        {
            name = serialized.name;
            if (serialized.points != null)
            {
                var points = new Vector3[serialized.points.Length];
                for (int a = 0; a < serialized.points.Length; a++) points[a] = UnityEngineHook.AsUnityVector(serialized.points[a]);
                SetPoints(points);
            }
        }

        public ISwoleSerializable Serialize() => AsSerializableStruct();

        #endregion

        public ExternalBezierCurve() : this(default) { }

        public string name;
        public string Name
        {
            get => name;
            set => name = value;
        }
        public override string SerializedName => Name;

        public CurveType Type => CurveType.Bezier;

        public BezierPath path;
        protected VertexPath vertexPath;
        protected bool dirtyVertices;
        public VertexPath VertexPath
        {
            get
            {
                if ((vertexPath == null || dirtyVertices) && path != null)
                {
                    dirtyVertices = false;
                    if (vertexSpacing <= 0) vertexSpacing = 0.1f;
                    if (vertexAccuracy <= 0) vertexAccuracy = 10;
                    vertexPath = new VertexPath(path, VertexPathUtility.SplitBezierPathEvenly(path, vertexSpacing, vertexAccuracy), null);

                    for (int a = 0; a < path.NumPoints; a++)
                    {
                        var p = path.GetPoint(a);
                        Debug.DrawRay(p, Vector3.forward, Color.blue, 100f);
                    }
                    for (int a = 0; a < vertexPath.NumPoints; a++)
                    {
                        var p = vertexPath.GetPoint(a);
                        Debug.DrawRay(p, Vector3.forward, Color.green, 100f); 
                    }
                }

                return vertexPath;
            }
        }

        protected float vertexSpacing = 0.1f;
        protected int vertexAccuracy = 10;
        public float VertexSpacing
        {
            get => vertexSpacing;
            set
            {
                vertexSpacing = value;
                dirtyVertices = true;
            }
        }
        public int VertexAccuracy
        {
            get => vertexAccuracy;
            set
            {
                vertexAccuracy = value;
                dirtyVertices = true;
            }
        }

        public bool Is2D => path == null ? false : path.Space != PathSpace.xyz;

        public bool IsClosed
        {
            get => path == null ? false : path.IsClosed;
            set
            {
                if (path == null) return;
                path.IsClosed = value;
            }
        }

        public int PointCount => path == null ? 0 : (path.IsClosed ? (path.NumPoints - 2) : path.NumPoints);

        public int VertexCount => VertexPath == null ? 0 : vertexPath.NumPoints;

        public float EstimatedLength => VertexPath == null ? 0 : vertexPath.length;

        public Vector3 GetPoint(int index)
        {
            if (path == null) return Vector3.zero;
            return path.GetPoint(index);
        }
        public Vector2 GetPoint2D(int index) => GetPoint(index);

        public Vector3 GetNormal(int index)
        {
            if (path == null) return Vector3.zero;
            return VertexPath.GetNormal(index);
        }
        public Vector3 GetTangent(int index)
        {
            if (path == null) return Vector3.zero;
            return VertexPath.GetTangent(index);
        }

        protected Vector3[] points;
        protected Vector2[] points2D;

        public Vector3[] GetPoints()
        {
            if (path == null) return null;
            if (points == null || points.Length != PointCount) points = new Vector3[PointCount];
            for (int a = 0; a < points.Length; a++) points[a] = path.GetPoint(a);
            return points;
        }

        public Vector2[] GetPoints2D()
        {
            if (path == null) return null;
            if (points2D == null || points2D.Length != PointCount) points2D = new Vector2[PointCount];
            for (int a = 0; a < points2D.Length; a++)
            {
                var point = path.GetPoint(a);
                points2D[a] = path.Space == PathSpace.xz ? new Vector2(point.x, point.z) : point;
            }
            return points2D;
        }

        public Vector3[] GetNormalizedPoints(float startX = 0, float endX = 1, float startY = 0, float endY = 1, float startZ = 0, float endZ = 1)
        {
            if (path == null) return null;

            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float minZ = float.MaxValue;

            float maxX = float.MinValue;
            float maxY = float.MinValue;
            float maxZ = float.MinValue;

            for (int a = 0; a < PointCount; a++)
            {
                var point = path.GetPoint(a);
                minX = Mathf.Min(minX, point.x);
                minY = Mathf.Min(minY, point.y);
                minZ = Mathf.Min(minZ, point.z);

                maxX = Mathf.Max(maxX, point.x);
                maxY = Mathf.Max(maxY, point.y);
                maxZ = Mathf.Max(maxZ, point.z);
            }

            return GetNormalizedPoints(minX, minY, minZ, maxX, maxY, maxZ, startX, endX, startY, endY, startZ, endZ);
        }
        public Vector3[] GetNormalizedPoints(float minX, float minY, float minZ, float maxX, float maxY, float maxZ, float startX = 0, float endX = 1, float startY = 0, float endY = 1, float startZ = 0, float endZ = 1)
        {
            if (minX == maxX) maxX = minX + 1;
            if (minY == maxY) maxY = minY + 1;
            if (minZ == maxZ) maxZ = minZ + 1;

            var points = GetPoints();
            if (points != null)
            {
                points = (Vector3[])points.Clone();
                for (int a = 0; a < points.Length; a++)
                {
                    var point = points[a];
                    points[a] = new Vector3(math.remap(minX, maxX, startX, endX, point.x), math.remap(minY, maxY, startY, endY, point.y), math.remap(minZ, maxZ, startZ, endZ, point.z));
                }
            }

            return points;
        }
        public Vector2[] GetNormalizedPoints2D(float startX = 0, float endX = 1, float startY = 0, float endY = 1)
        {
            if (path == null) return null;

            float minX = float.MaxValue;
            float minY = float.MaxValue;

            float maxX = float.MinValue;
            float maxY = float.MinValue;

            for (int a = 0; a < PointCount; a++)
            {
                var point = path.GetPoint(a);
                point = path.Space == PathSpace.xz ? new Vector2(point.x, point.z) : point;

                minX = Mathf.Min(minX, point.x);
                minY = Mathf.Min(minY, point.y);

                maxX = Mathf.Max(maxX, point.x);
                maxY = Mathf.Max(maxY, point.y);
            }

            return GetNormalizedPoints2D(minX, minY, maxX, maxY, startX, endX, startY, endY);
        }
        public Vector2[] GetNormalizedPoints2D(float minX, float minY, float maxX, float maxY, float startX = 0, float endX = 1, float startY = 0, float endY = 1)
        {
            if (minX == maxX) maxX = minX + 1;
            if (minY == maxY) maxY = minY + 1;

            var points = GetPoints2D();
            if (points != null)
            {
                points = (Vector2[])points.Clone();
                for (int a = 0; a < points.Length; a++)
                {
                    var point = points[a];
                    points[a] = new Vector2(math.remap(minX, maxX, startX, endX, point.x), math.remap(minY, maxY, startY, endY, point.y));
                }
            }

            return points;
        }

        public bool SetPoints(Vector3[] points)
        {
            if (points == null || points.Length < 2) return false;
            
            int targetSegmentCount = Mathf.Max(1, Mathf.CeilToInt((points.Length - 1) / 3f));

            dirtyVertices = true;

            if (path == null)
            {
                path = new BezierPath(points, false, PathSpace.xyz); // this constructor is adding control points, so the points need to be reevaluated by the code below, since the control points are already included in the points array and should not be added again
                //return true;
            }
            
            bool closed = path.IsClosed;
            path.IsClosed = false;
            
            while (path.NumSegments > targetSegmentCount) 
            {
                var prev = path.NumSegments;
                path.DeleteSegment(path.NumSegments - 1);
                if (prev <= path.NumSegments) return false;
            }

            while (path.NumSegments < targetSegmentCount - 1)
            {
                var prev = path.NumSegments;
                path.AddSegmentToEnd(Vector3.zero);
                if (prev >= path.NumSegments) return false;
            }

            int segPointCount = (targetSegmentCount * 3) + 1; 
            for(int a = 0; a < Mathf.Min(points.Length, segPointCount); a++)
            {
                if (a == points.Length - 1 && segPointCount > points.Length)
                {
                    path.AddSegmentToEnd(points[a]);
                    continue;
                }

                path.SetPoint(a, points[a], true);  
            }

            path.IsClosed = closed;

            return true;
        }

        protected Keyframe[] keyframes;

        public Keyframe[] GetPointsAsKeyframes()
        {
            if (path == null) return null;
            if (keyframes == null || keyframes.Length != PointCount) keyframes = new Keyframe[PointCount];
            for (int a = 0; a < keyframes.Length; a++)
            {
                var point = path.GetPoint(a);
                keyframes[a] = path.Space == PathSpace.xz ? new Keyframe() { time = point.x, value = point.z } : new Keyframe() { time = point.x, value = point.y }; 
            }
            return keyframes;
        }

        public Vector3 GetVertex(int index)
        {
            if (VertexPath == null) return Vector3.zero;
            return vertexPath.GetPoint(index);
        }

        public Vector2 GetVertex2D(int index)
        {
            if (VertexPath == null) return Vector3.zero;
            var pos = vertexPath.GetPoint(index);
            return path.Space == PathSpace.xz ? new Vector2(pos.x, pos.z) : pos;
        }

        protected Vector3[] vertices;
        protected Vector2[] vertices2D; 

        public Vector3[] GetVertices()
        {
            if (VertexPath == null) return null;
            if (vertices == null || vertices.Length != vertexPath.NumPoints) vertices = new Vector3[vertexPath.NumPoints]; 
            for (int a = 0; a < vertices.Length; a++) vertices[a] = vertexPath.GetPoint(a);
            return vertices;
        }

        public Vector2[] GetVertices2D()
        {
            if (VertexPath == null) return null;
            if (vertices2D == null || vertices2D.Length != vertexPath.NumPoints) vertices2D = new Vector2[vertexPath.NumPoints];
            for (int a = 0; a < vertices2D.Length; a++)
            {
                var point = vertexPath.GetPoint(a);
                vertices2D[a] = path.Space == PathSpace.xz ? new Vector2(point.x, point.z) : point; 
            }
            return vertices2D;
        }

        public Vector3[] GetNormalizedVertices(float startX = 0, float endX = 1, float startY = 0, float endY = 1, float startZ = 0, float endZ = 1)
        {
            var verts = GetVertices();

            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float minZ = float.MaxValue;

            float maxX = float.MinValue;
            float maxY = float.MinValue;
            float maxZ = float.MinValue;

            for (int a = 0; a < verts.Length; a++)
            {
                var vert = verts[a];
                minX = Mathf.Min(minX, vert.x);
                minY = Mathf.Min(minY, vert.y);
                minZ = Mathf.Min(minZ, vert.z);

                maxX = Mathf.Max(maxX, vert.x);
                minY = Mathf.Max(maxY, vert.y);
                minZ = Mathf.Max(maxZ, vert.z);
            }

            return GetNormalizedVertices(minX, minY, minZ, maxX, maxY, maxZ, startX, endX, startY, endY, startZ, endZ);
        }
        public Vector3[] GetNormalizedVertices(float minX, float minY, float minZ, float maxX, float maxY, float maxZ, float startX = 0, float endX = 1, float startY = 0, float endY = 1, float startZ = 0, float endZ = 1)
        {
            var verts = (Vector3[])GetVertices().Clone();
            for (int a = 0; a < verts.Length; a++)
            {
                var vert = verts[a];
                verts[a] = new Vector3(math.remap(minX, maxX, startX, endX, vert.x), math.remap(minY, maxY, startY, endY, vert.y), math.remap(minZ, maxZ, startZ, endZ, vert.z));
            }

            return verts;
        }
        public Vector2[] GetNormalizedVertices2D(float startX = 0, float endX = 1, float startY = 0, float endY = 1)
        {
            var verts = GetVertices2D();

            float minX = float.MaxValue;
            float minY = float.MaxValue;

            float maxX = float.MinValue;
            float maxY = float.MinValue;

            for (int a = 0; a < PointCount; a++)
            {
                var vert = path.GetPoint(a);

                minX = Mathf.Min(minX, vert.x);
                minY = Mathf.Min(minY, vert.y);

                maxX = Mathf.Max(maxX, vert.x);
                minY = Mathf.Max(maxY, vert.y);
            }

            return GetNormalizedVertices2D(minX, minY, maxX, maxY, startX, endX, startY, endY);
        }
        public Vector2[] GetNormalizedVertices2D(float minX, float minY, float maxX, float maxY, float startX = 0, float endX = 1, float startY = 0, float endY = 1)
        {
            var verts = (Vector2[])GetVertices2D().Clone();
            for (int a = 0; a < verts.Length; a++)
            {
                var vert = verts[a];
                verts[a] = new Vector2(math.remap(minX, maxX, startX, endX, vert.x), math.remap(minY, maxY, startY, endY, vert.y));
            }

            return verts;
        }

        public float Evaluate(float t) => Evaluate3(t).x;
        public EngineInternal.Vector2 Evaluate2(float t) => Evaluate3(t);
        public EngineInternal.Vector3 Evaluate3(float t) => UnityEngineHook.AsSwoleVector(GetPositionOnCurve(t, SwoleEndOfPathInstruction.Loop));

        public Vector3 GetPositionOnCurve(float normalizedPos, SwoleEndOfPathInstruction endOfPathInstruction = SwoleEndOfPathInstruction.Stop)
        {
            if (path == null) return Vector3.zero;
            return VertexPath.GetPointAtTime(normalizedPos, (EndOfPathInstruction)endOfPathInstruction);
        }

        public Vector2 GetPositionOnCurve2D(float normalizedPos, SwoleEndOfPathInstruction endOfPathInstruction = SwoleEndOfPathInstruction.Stop)
        {
            if (path == null) return Vector2.zero;
            var pos = GetPositionOnCurve(normalizedPos, endOfPathInstruction);
            return path.Space == PathSpace.xz ? new Vector2(pos.x, pos.z) : pos;
        }

        public Vector3 GetPositionFromDistanceAlongCurve(float distance, SwoleEndOfPathInstruction endOfPathInstruction = SwoleEndOfPathInstruction.Stop)
        {
            if (path == null) return Vector3.zero;
            return VertexPath.GetPointAtDistance(distance, (EndOfPathInstruction)endOfPathInstruction);
        }

        public Vector2 GetPositionFromDistanceAlongCurve2D(float distance, SwoleEndOfPathInstruction endOfPathInstruction = SwoleEndOfPathInstruction.Stop)
        {
            if (path == null) return Vector2.zero;
            var pos = GetPositionFromDistanceAlongCurve(distance, endOfPathInstruction);
            return path.Space == PathSpace.xz ? new Vector2(pos.x, pos.z) : pos;
        }

        public Vector3 GetPointAtTime(float t, SwoleEndOfPathInstruction endOfPathInstruction = SwoleEndOfPathInstruction.Stop)
        {
            if (path == null) return Vector3.zero;
            return VertexPath.GetPointAtTime(t, (EndOfPathInstruction)endOfPathInstruction);
        }

        public Vector3 GetDirection(float t, SwoleEndOfPathInstruction endOfPathInstruction = SwoleEndOfPathInstruction.Stop)
        {
            if (path == null) return Vector3.zero;
            return VertexPath.GetDirection(t, (EndOfPathInstruction)endOfPathInstruction);
        }

        public Vector3 GetNormal(float t, SwoleEndOfPathInstruction endOfPathInstruction = SwoleEndOfPathInstruction.Stop)
        {
            if (path == null) return Vector3.zero;
            return VertexPath.GetNormal(t, (EndOfPathInstruction)endOfPathInstruction);
        }

        public Quaternion GetRotation(float t, SwoleEndOfPathInstruction endOfPathInstruction = SwoleEndOfPathInstruction.Stop)
        {
            if (path == null) return Quaternion.identity;
            return VertexPath.GetRotation(t, (EndOfPathInstruction)endOfPathInstruction);
        }

        public Vector3 GetClosestPointOnPath(Vector3 worldPoint)
        {
            if (path == null) return Vector3.zero;
            return VertexPath.GetClosestPointOnPath(worldPoint);
        }

        public float GetClosestTimeOnPath(Vector3 worldPoint)
        {
            if (path == null) return 0f;
            return VertexPath.GetClosestTimeOnPath(worldPoint);
        }

        public float GetClosestDistanceAlongPath(Vector3 worldPoint)
        {
            if (path == null) return 0f;
            return VertexPath.GetClosestDistanceAlongPath(worldPoint);
        }

        // Helper to sample old arrays by time (interpolate)
        private Vector3 SampleArray(Vector3[] arr, float[] timesArr, float t)
        {
            if (arr == null || arr.Length == 0) return Vector3.zero;
            if (timesArr == null || timesArr.Length != arr.Length) return arr[0];
            if (t <= timesArr[0]) return arr[0];
            if (t >= timesArr[timesArr.Length - 1]) return arr[arr.Length - 1];

            int prev = 0;
            int next = arr.Length - 1;
            // binary search style
            int lo = 0, hi = timesArr.Length - 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;
                if (timesArr[mid] == t) { prev = next = mid; break; }
                if (timesArr[mid] < t) { lo = mid + 1; prev = mid; }
                else { hi = mid - 1; next = mid; }
            }
            if (prev == next) return arr[prev];
            float a = Mathf.InverseLerp(timesArr[prev], timesArr[next], t);
            return Vector3.Lerp(arr[prev], arr[next], a);
        }
        public void SetVertices(Vector3[] vertices, int[] originalVertexIndices)
        {
            if (VertexPath == null || vertices == null || vertices.Length == 0) return;

            // old references
            Vector3[] oldPoints = vertexPath.localPoints ?? new Vector3[0];
            Vector3[] oldTangents = vertexPath.localTangents ?? new Vector3[0];
            Vector3[] oldNormals = vertexPath.localNormals ?? new Vector3[0];
            float[] oldTimes = vertexPath.times ?? new float[0];
            float[] oldCum = vertexPath.cumulativeLengthAtEachVertex ?? new float[0];
            int oldCount = oldPoints.Length;
            int newCount = vertices.Length;

            // validate provided originalVertexIndices - accept only if length matches and values are in range or -1
            bool validMapping = true;
            if (originalVertexIndices == null || originalVertexIndices.Length != newCount) validMapping = false;
            if (validMapping)
            {
                for (int i = 0; i < newCount; i++)
                {
                    int v = originalVertexIndices[i];
                    if (v < -1 || v >= oldCount) { validMapping = false; break; }
                }
            }

            int[] mapping = new int[newCount];
            if (validMapping)
            {
                Array.Copy(originalVertexIndices, mapping, newCount);
            }
            else
            {
                // build all pair distances and perform greedy one-to-one minimal distance matching
                var pairs = new List<(int newI, int oldJ, float dist)>(newCount * Math.Max(1, oldCount));
                for (int ni = 0; ni < newCount; ni++)
                {
                    for (int oi = 0; oi < oldCount; oi++)
                    {
                        float d = Vector3.SqrMagnitude(vertices[ni] - oldPoints[oi]);
                        pairs.Add((ni, oi, d));
                    }
                }
                pairs.Sort((a, b) => a.dist.CompareTo(b.dist));

                bool[] newAssigned = new bool[newCount];
                bool[] oldAssigned = new bool[oldCount];
                for (int i = 0; i < newCount; i++) mapping[i] = -1;

                foreach (var p in pairs)
                {
                    if (!newAssigned[p.newI] && !oldAssigned[p.oldJ])
                    {
                        mapping[p.newI] = p.oldJ;
                        newAssigned[p.newI] = true;
                        oldAssigned[p.oldJ] = true;
                    }
                }
                // remaining newIndices keep -1
            }

            // build new cumulative lengths from provided vertices
            float[] newCum = new float[newCount];
            newCum[0] = 0f;
            for (int i = 1; i < newCount; i++) newCum[i] = newCum[i - 1] + Vector3.Distance(vertices[i], vertices[i - 1]);
            float newLength = newCum[newCount - 1];

            float[] newTimes = new float[newCount];
            if (newLength > 1e-6f)
            {
                for (int i = 0; i < newCount; i++) newTimes[i] = newCum[i] / newLength;
            }
            else
            {
                // degenerate -> distribute evenly
                for (int i = 0; i < newCount; i++) newTimes[i] = (newCount == 1) ? 0f : (float)i / (newCount - 1);
            }

            Vector3[] newTangents = new Vector3[newCount];
            Vector3[] newNormals = new Vector3[newCount];

            // populate tangents and normals: prefer direct mapping when available, otherwise sample by time
            for (int i = 0; i < newCount; i++)
            {
                int orig = mapping[i];
                if (orig >= 0 && orig < oldCount)
                {
                    if (oldTangents != null && oldTangents.Length > orig) newTangents[i] = oldTangents[orig];
                    else newTangents[i] = Vector3.zero;

                    if (oldNormals != null && oldNormals.Length > orig) newNormals[i] = oldNormals[orig];
                    else newNormals[i] = Vector3.zero;
                }
                else
                {
                    // sample old arrays by time
                    float t = newTimes[i];
                    newTangents[i] = SampleArray(oldTangents, oldTimes, t);
                    newNormals[i] = SampleArray(oldNormals, oldTimes, t);
                }

                // ensure tangents/normals are normalized where reasonable
                if (newTangents[i] != Vector3.zero) newTangents[i] = newTangents[i].normalized;
                if (newNormals[i] != Vector3.zero) newNormals[i] = newNormals[i].normalized;
            }

            // Apply results to vertexPath
            vertexPath.localPoints = (Vector3[])vertices.Clone();
            vertexPath.localTangents = newTangents;
            vertexPath.localNormals = newNormals;
            vertexPath.cumulativeLengthAtEachVertex = newCum;
            vertexPath.length = newLength;
            vertexPath.times = newTimes;
        }
        public void SetVertices2D(Vector2[] vertices, int[] originalVertexIndices)
        {
            if (VertexPath == null || vertices == null || vertices.Length == 0) return;

            // convert 2D to 3D consistent with GetVertices2D
            Vector3[] verts3 = new Vector3[vertices.Length];
            if (path != null && path.Space == PathSpace.xz)
            {
                for (int i = 0; i < vertices.Length; i++) verts3[i] = new Vector3(vertices[i].x, 0f, vertices[i].y);
            }
            else
            {
                for (int i = 0; i < vertices.Length; i++) verts3[i] = new Vector3(vertices[i].x, vertices[i].y, 0f);
            }

            SetVertices(verts3, originalVertexIndices);
        }
        public void SetVertex(int index, Vector3 vertex)
        {
            if (VertexPath != null)
            {
                if (vertexPath.localPoints != null && index >= 0 && index < vertexPath.localPoints.Length) vertexPath.localPoints[index] = vertex;
            }
        }
        public void SetVertex2D(int index, Vector2 vertex)
        {
            if (VertexPath != null)
            {
                if (vertexPath.localPoints != null && index >= 0 && index < vertexPath.localPoints.Length)
                {
                    if (path != null && path.Space == PathSpace.xz) vertexPath.localPoints[index] = new Vector3(vertex.x, vertexPath.localPoints[index].y, vertex.y);
                    else vertexPath.localPoints[index] = new Vector3(vertex.x, vertex.y, vertexPath.localPoints[index].z);
                }
            }
        }

    }

}

#endif