#if (UNITY_STANDALONE || UNITY_EDITOR) && BULKOUT_ENV

using UnityEngine;

using Unity.Mathematics;

using PathCreation;
using PathCreation.Utility;

namespace Swole.API.Unity
{

    public class ExternalBezierCurve : SwoleObject<ICurve, SerializedCurve>, IBezierCurve
    {

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
                for(int a = 0; a < serialized.points.Length; a++) points[a] = UnityEngineHook.AsUnityVector(serialized.points[a]); 
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

        protected Vector3[] points;
        protected Vector2[] points2D;

        public Vector3[] GetPoints()
        {
            if (path == null) return null;
            if (points == null || points.Length != PointCount) points = new Vector3[PointCount];
            for(int a = 0; a < points.Length; a++) points[a] = path.GetPoint(a);
            return points;
        }

        public Vector2[] GetPoints2D()
        {
            if (path == null) return null;
            if (points2D == null || points2D.Length != PointCount) points2D = new Vector2[PointCount];
            for (int a = 0; a < points2D.Length; a++) 
            { 
                var point = path.GetPoint(a);
                points2D[a] = path.Space == PathSpace.xz ? new Vector2 (point.x, point.z) : point; 
            }
            return points2D;
        }

        public Vector3[] GetNormalizedPoints(float startX = 0, float endX = 1, float startY = 0, float endY = 1, float startZ = 0, float endZ = 1)
        {
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

            var points = (Vector3[])GetPoints().Clone(); 
            for (int a = 0; a < points.Length; a++) 
            { 
                var point = points[a];
                points[a] = new Vector3(math.remap(minX, maxX, startX, endX, point.x), math.remap(minY, maxY, startY, endY, point.y), math.remap(minZ, maxZ, startZ, endZ, point.z));
            }

            return points;
        }
        public Vector2[] GetNormalizedPoints2D(float startX = 0, float endX = 1, float startY = 0, float endY = 1)
        {
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

            var points = (Vector2[])GetPoints2D().Clone(); 
            for (int a = 0; a < points.Length; a++)
            {
                var point = points[a];
                points[a] = new Vector2(math.remap(minX, maxX, startX, endX, point.x), math.remap(minY, maxY, startY, endY, point.y));
            }

            return points;
        }

        public bool SetPoints(Vector3[] points)
        {
            if (points == null) return false;

            int segCount = Mathf.FloorToInt(points.Length / 3f);
            if (segCount <= 0) return false;

            dirtyVertices = true;

            if (path == null)
            {
                path = new BezierPath(points, false, PathSpace.xyz); // this is adding control points, so needs to be reevaluated by the code below, since the control points are already included in the points array
                //return true;
            }
            
            bool closed = path.IsClosed;
            path.IsClosed = false;

            while (path.NumSegments > segCount) 
            {
                var prev = path.NumSegments;
                path.DeleteSegment(path.NumSegments - 1);
                if (prev <= path.NumSegments) return false;
            }

            while(path.NumSegments < segCount)
            {
                var prev = path.NumSegments;
                path.AddSegmentToEnd(Vector3.zero);
                if (prev >= path.NumSegments) return false;
            }
            
            for(int a = 0; a < points.Length; a++)
            {
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

        public float Evaluate(float t) => Evaluate3(t).x;
        public EngineInternal.Vector2 Evaluate2(float t) => Evaluate3(t);
        public EngineInternal.Vector3 Evaluate3(float t) => UnityEngineHook.AsSwoleVector(GetPositionOnCurve(t));  

        public Vector3 GetPositionOnCurve(float normalizedPos)
        {
            if (path == null) return Vector3.zero;
            return VertexPath.GetPointAtTime(normalizedPos, EndOfPathInstruction.Stop);
        }

        public Vector2 GetPositionOnCurve2D(float normalizedPos)
        {
            if (path == null) return Vector2.zero;
            var pos = GetPositionOnCurve(normalizedPos);
            return path.Space == PathSpace.xz ? new Vector2(pos.x, pos.z) : pos;
        }

        public Vector3 GetPositionOnCurveFromDistance(float distance)
        {
            if (path == null) return Vector3.zero;
            return VertexPath.GetPointAtDistance(distance, EndOfPathInstruction.Stop); 
        }

        public Vector2 GetPositionOnCurveFromDistance2D(float distance)
        {
            if (path == null) return Vector2.zero;
            var pos = GetPositionOnCurveFromDistance(distance);
            return path.Space == PathSpace.xz ? new Vector2(pos.x, pos.z) : pos;
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

        public ExternalBezierCurve Duplicate()
        {
            var clone = new ExternalBezierCurve();

            clone.Name = Name;
            clone.SetPoints(GetPoints());  

            return clone;
        }
        public object Clone() => Duplicate();
    }

}

#endif