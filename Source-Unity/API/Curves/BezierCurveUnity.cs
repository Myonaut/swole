#if (UNITY_STANDALONE || UNITY_EDITOR) && BULKOUT_ENV

using System;

using UnityEngine;

namespace Swole.API.Unity
{
    public class BezierCurveUnity : ScriptableObject, IBezierCurve
    {

        public static BezierCurveUnity Create(Vector3[] points, float vertexSpacing = 0.1f, int vertexAccuracy = 10)
        {
            var inst = ScriptableObject.CreateInstance<BezierCurveUnity>();

            inst.points = points;
            inst.vertexSpacing = vertexSpacing;
            inst.vertexAccuracy = vertexAccuracy;

            return inst;
        }
        public static BezierCurveUnity Create(IBezierCurve refCurve) => Create((Vector3[])refCurve.GetPoints().Clone(), refCurve.VertexSpacing, refCurve.VertexAccuracy);

        public float vertexSpacing = 0.1f;
        public int vertexAccuracy = 10;

        public Vector3[] points; 

        [NonSerialized]
        private ExternalBezierCurve curve;
        public ExternalBezierCurve Curve
        {
            get
            {
                if (curve == null) 
                {
                    curve = new ExternalBezierCurve(); 
                    curve.name = name;
                    curve.VertexSpacing = vertexSpacing;
                    curve.VertexAccuracy = vertexAccuracy;
                    if (points != null) curve.SetPoints(points); 
                }

                return curve;
            }
        }

        #region IBezierCurve
        public bool Is2D => Curve.Is2D;

        public bool IsClosed 
        {
            get => Curve.IsClosed;
            set => Curve.IsClosed = value;
        }

        public int PointCount => Curve.PointCount;

        public int VertexCount => Curve.VertexCount;

        public float VertexSpacing 
        {
            get => Curve.VertexSpacing;
            set => Curve.VertexSpacing = value;
        }
        public int VertexAccuracy
        {
            get => Curve.VertexAccuracy;
            set => Curve.VertexAccuracy = value;
        }

        public float EstimatedLength => Curve.EstimatedLength;

        public string Name
        {
            get => name;
            set => name = value;
        }

        public CurveType Type => CurveType.Bezier;

        public object Clone()
        {
            var clone = ScriptableObject.CreateInstance<BezierCurveUnity>();

            clone.vertexSpacing = vertexSpacing;
            clone.vertexAccuracy = vertexAccuracy;
            clone.points = (Vector3[])points.Clone();

            return clone;
        }

        public float Evaluate(float t) => Curve.Evaluate(t);

        public EngineInternal.Vector2 Evaluate2(float t) => Curve.Evaluate2(t);

        public EngineInternal.Vector3 Evaluate3(float t) => Curve.Evaluate3(t);

        public Vector3[] GetNormalizedPoints(float startX = 0, float endX = 1, float startY = 0, float endY = 1, float startZ = 0, float endZ = 1) => Curve.GetNormalizedPoints(startX, endX, startY, endY, startZ, endZ);
        public Vector3[] GetNormalizedPoints(float minX, float minY, float minZ, float maxX, float maxY, float maxZ, float startX = 0, float endX = 1, float startY = 0, float endY = 1, float startZ = 0, float endZ = 1) => Curve.GetNormalizedPoints(minX, minY, minZ, maxX, maxY, maxZ, startX, endX, startY, endY, startZ, endZ);
        public Vector2[] GetNormalizedPoints2D(float startX = 0, float endX = 1, float startY = 0, float endY = 1) => Curve.GetNormalizedPoints2D(startX, endX, startY, endY);
        public Vector2[] GetNormalizedPoints2D(float minX, float minY, float maxX, float maxY, float startX = 0, float endX = 1, float startY = 0, float endY = 1) => Curve.GetNormalizedPoints2D(minX, minY, maxX, maxY, startX, endX, startY, endY);
         
        public Vector3[] GetNormalizedVertices(float startX = 0, float endX = 1, float startY = 0, float endY = 1, float startZ = 0, float endZ = 1) => Curve.GetNormalizedVertices(startX, endX, startY, endY, startZ, endZ);
        public Vector3[] GetNormalizedVertices(float minX, float minY, float minZ, float maxX, float maxY, float maxZ, float startX = 0, float endX = 1, float startY = 0, float endY = 1, float startZ = 0, float endZ = 1) => Curve.GetNormalizedVertices(minX, minY, minZ, maxX, maxY, maxZ, startX, endX, startY, endY, startZ, endZ);
        public Vector2[] GetNormalizedVertices2D(float startX = 0, float endX = 1, float startY = 0, float endY = 1) => Curve.GetNormalizedVertices2D(startX, endX, startY, endY); 
        public Vector2[] GetNormalizedVertices2D(float minX, float minY, float maxX, float maxY, float startX = 0, float endX = 1, float startY = 0, float endY = 1) => Curve.GetNormalizedVertices2D(minX, minY, maxX, maxY, startX, endX, startY, endY);

        public Vector3 GetPoint(int index) => Curve.GetPoint(index);
        public Vector2 GetPoint2D(int index) => Curve.GetPoint2D(index);

        public Vector3 GetNormal(int index) => Curve.GetNormal(index);
        public Vector3 GetTangent(int index) => Curve.GetTangent(index);

        public Vector3[] GetPoints() => Curve.GetPoints();

        public Vector2[] GetPoints2D() => Curve.GetPoints2D();

        public Keyframe[] GetPointsAsKeyframes() => Curve.GetPointsAsKeyframes();

        public Vector3 GetPositionOnCurve(float normalizedPos, SwoleEndOfPathInstruction endOfPathInstruction = SwoleEndOfPathInstruction.Stop) => Curve.GetPositionOnCurve(normalizedPos, endOfPathInstruction);

        public Vector2 GetPositionOnCurve2D(float normalizedPos, SwoleEndOfPathInstruction endOfPathInstruction = SwoleEndOfPathInstruction.Stop) => Curve.GetPositionOnCurve2D(normalizedPos, endOfPathInstruction);

        public Vector3 GetPositionFromDistanceAlongCurve(float distance, SwoleEndOfPathInstruction endOfPathInstruction = SwoleEndOfPathInstruction.Stop) => Curve.GetPositionFromDistanceAlongCurve(distance, endOfPathInstruction);

        public Vector2 GetPositionFromDistanceAlongCurve2D(float distance, SwoleEndOfPathInstruction endOfPathInstruction = SwoleEndOfPathInstruction.Stop) => Curve.GetPositionFromDistanceAlongCurve2D(distance, endOfPathInstruction);

        public Vector3 GetPointAtTime(float t, SwoleEndOfPathInstruction endOfPathInstruction = SwoleEndOfPathInstruction.Stop) => Curve.GetPointAtTime(t, endOfPathInstruction);

        public Vector3 GetDirection(float t, SwoleEndOfPathInstruction endOfPathInstruction = SwoleEndOfPathInstruction.Stop) => Curve.GetDirection(t, endOfPathInstruction);

        public Vector3 GetNormal(float t, SwoleEndOfPathInstruction endOfPathInstruction = SwoleEndOfPathInstruction.Stop) => Curve.GetNormal(t, endOfPathInstruction);

        public Quaternion GetRotation(float t, SwoleEndOfPathInstruction endOfPathInstruction = SwoleEndOfPathInstruction.Stop) => Curve.GetRotation(t, endOfPathInstruction);

        public Vector3 GetClosestPointOnPath(Vector3 worldPoint) => Curve.GetClosestPointOnPath(worldPoint);

        public float GetClosestTimeOnPath(Vector3 worldPoint) => Curve.GetClosestTimeOnPath(worldPoint);

        public float GetClosestDistanceAlongPath(Vector3 worldPoint) => Curve.GetClosestDistanceAlongPath(worldPoint);

        public Vector3 GetVertex(int index) => Curve.GetVertex(index);

        public Vector2 GetVertex2D(int index) => Curve.GetVertex2D(index);

        public Vector3[] GetVertices() => Curve.GetVertices();

        public Vector2[] GetVertices2D() => Curve.GetVertices2D();

        public ISwoleSerializable Serialize() => Curve.Serialize();

        public bool SetPoints(Vector3[] points)
        {
            this.points = points;
            if (curve != null) curve.SetPoints(points);
            return true;
        }
        #endregion

    }
}

#endif