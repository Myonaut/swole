#if (UNITY_STANDALONE || UNITY_EDITOR)

using UnityEngine;

namespace Swole.API.Unity
{
    public interface IBezierCurve : ICurve
    { 
        public bool Is2D { get; }
        public bool IsClosed { get; set; }

        public Vector3[] GetPoints();
        public Vector2[] GetPoints2D();
        public Vector3[] GetNormalizedPoints(float startX = 0, float endX = 1, float startY = 0, float endY = 1, float startZ = 0, float endZ = 1);
        public Vector3[] GetNormalizedPoints(float minX, float minY, float minZ, float maxX, float maxY, float maxZ, float startX = 0, float endX = 1, float startY = 0, float endY = 1, float startZ = 0, float endZ = 1);
        public Vector2[] GetNormalizedPoints2D(float startX = 0, float endX = 1, float startY = 0, float endY = 1);
        public Vector2[] GetNormalizedPoints2D(float minX, float minY, float maxX, float maxY, float startX = 0, float endX = 1, float startY = 0, float endY = 1);
        public Keyframe[] GetPointsAsKeyframes(); 
        public bool SetPoints(Vector3[] points);
        public int PointCount { get; }
        public Vector3 GetPoint(int index);
        public Vector2 GetPoint2D(int index);

        public Vector3[] GetVertices();
        public Vector2[] GetVertices2D();
        public Vector3[] GetNormalizedVertices(float startX = 0, float endX = 1, float startY = 0, float endY = 1, float startZ = 0, float endZ = 1);  
        public Vector3[] GetNormalizedVertices(float minX, float minY, float minZ, float maxX, float maxY, float maxZ, float startX = 0, float endX = 1, float startY = 0, float endY = 1, float startZ = 0, float endZ = 1);
        public Vector2[] GetNormalizedVertices2D(float startX = 0, float endX = 1, float startY = 0, float endY = 1);
        public Vector2[] GetNormalizedVertices2D(float minX, float minY, float maxX, float maxY, float startX = 0, float endX = 1, float startY = 0, float endY = 1);
        public int VertexCount { get; }
        public Vector3 GetVertex(int index);
        public Vector2 GetVertex2D(int index);
        public float VertexSpacing { get; set; }
        public int VertexAccuracy { get; set; }

        public Vector3 GetPositionOnCurve(float normalizedPos);
        public Vector2 GetPositionOnCurve2D(float normalizedPos);
        public float EstimatedLength { get; }
        public Vector3 GetPositionOnCurveFromDistance(float distance);
        public Vector2 GetPositionOnCurveFromDistance2D(float distance);
    }
}

#endif