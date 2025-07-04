#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;

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
        public Vector3 GetTangent(int index);
        public Vector3 GetNormal(int index);

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

        public Vector3 GetPositionOnCurve(float normalizedPos, SwoleEndOfPathInstruction endOfPathInstruction = SwoleEndOfPathInstruction.Stop);
        public Vector2 GetPositionOnCurve2D(float normalizedPos, SwoleEndOfPathInstruction endOfPathInstruction = SwoleEndOfPathInstruction.Stop);
        public float EstimatedLength { get; }
        public Vector3 GetPositionFromDistanceAlongCurve(float distance, SwoleEndOfPathInstruction endOfPathInstruction = SwoleEndOfPathInstruction.Stop);
        public Vector2 GetPositionFromDistanceAlongCurve2D(float distance, SwoleEndOfPathInstruction endOfPathInstruction = SwoleEndOfPathInstruction.Stop);

        /// <summary>
        /// Gets point on path based on 'time' (where 0 is start, and 1 is end of path).
        /// </summary>
        public Vector3 GetPointAtTime(float t, SwoleEndOfPathInstruction endOfPathInstruction = SwoleEndOfPathInstruction.Stop);

        /// <summary>
        /// Gets forward direction on path based on 'time' (where 0 is start, and 1 is end of path).
        /// </summary>
        public Vector3 GetDirection(float t, SwoleEndOfPathInstruction endOfPathInstruction = SwoleEndOfPathInstruction.Stop);

        /// <summary>
        /// Gets normal vector on path based on 'time' (where 0 is start, and 1 is end of path).
        /// </summary>
        public Vector3 GetNormal(float t, SwoleEndOfPathInstruction endOfPathInstruction = SwoleEndOfPathInstruction.Stop);

        /// <summary>
        /// Gets a rotation that will orient an object in the direction of the path at this point, with local up point along the path's normal
        /// </summary>
        public Quaternion GetRotation(float t, SwoleEndOfPathInstruction endOfPathInstruction = SwoleEndOfPathInstruction.Stop);

        /// <summary>
        /// Finds the closest point on the path from any point in the world
        /// </summary>
        public Vector3 GetClosestPointOnPath(Vector3 worldPoint);

        /// <summary>
        /// Finds the 'time' (0=start of path, 1=end of path) along the path that is closest to the given point
        /// </summary>
        public float GetClosestTimeOnPath(Vector3 worldPoint);

        /// <summary>
        /// Finds the distance along the path that is closest to the given point
        /// </summary>
        public float GetClosestDistanceAlongPath(Vector3 worldPoint);

    }

    [Serializable]
    public enum SwoleEndOfPathInstruction { Loop, Reverse, Stop };
}

#endif