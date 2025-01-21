#if (UNITY_STANDALONE || UNITY_EDITOR)

using UnityEngine;
using UnityEngine.UI;

namespace Swole.UI
{

    [ExecuteInEditMode, RequireComponent(typeof(CanvasRenderer))]
    public class UILineRenderer : MaskableGraphic
    {

        [SerializeField]
        protected Texture2D texture;
        public override Texture mainTexture => texture == null ? base.mainTexture : texture;
        public void SetTexture(Texture2D texture)
        {
            this.texture = texture;
            SetMaterialDirty();
        }

        public bool addStartCap;
        [Tooltip("Must be at least 3")]
        public int startCapResolution = 8;

        public bool addEndCap;
        [Tooltip("Must be at least 3")]
        public int endCapResolution = 8;

        public bool useEqualUVPortionsPerSegment;
        public Vector2 uvRangeX = new Vector2(0, 1);
        public float GetUVX(float t) => (uvRangeX.x) + (uvRangeX.y - uvRangeX.x) * t;
        public Vector2 uvRangeY = new Vector2(0, 1);
        public float GetUVY(float t) => (uvRangeY.x) + (uvRangeY.y - uvRangeY.x) * t;

        public float sharpAngleThreshold = 75; 

        [SerializeField]
        private RectTransform containerTransform;
        public void SetContainerTransform(RectTransform container)
        {
            containerTransform = container;
            SetVerticesDirty();
        }

        [SerializeField]
        private float thickness = 5;
        public void SetThickness(float thickness)
        {
            this.thickness = thickness;
            SetVerticesDirty();
        }

        public void SetColor(Color color) => this.color = color;

#if UNITY_EDITOR
        /// <summary>
        /// !!! Unity Editor Only !!!
        /// </summary>
        public Vector2[] inputPoints;
#endif

        [SerializeField, HideInInspector]
        protected Vector2[] points;

        public Vector2 this[int index]
        {
            get => points == null ? default : points[index];
            set
            {
                if (points == null) return;
                points[index] = value;
                SetVerticesDirty();
            }
        }

        public int PointCount => points == null ? 0 : points.Length;

        public void SetPoints(Vector2[] pointArray)
        {
            points = pointArray;
            SetVerticesDirty();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {

            if (inputPoints != null && inputPoints.Length > 0)
            {

                points = (Vector2[])inputPoints.Clone();

            }

            base.OnValidate();

        }
#endif

        void GetVertices(Vector2 point, Quaternion rot, out Vector3 v1, out Vector3 v2)
        {

            v1 = new Vector3(point.x, point.y, 0) + rot * Vector3.up * thickness * 0.5f;
            v2 = new Vector3(point.x, point.y, 0) - rot * Vector3.up * thickness * 0.5f;

        }

        void GetVertices(Vector2 prevPoint, Vector2 point, out Vector3 v1, out Vector3 v2)
        {

            Vector2 offset = point - prevPoint;

            Quaternion rot = Quaternion.FromToRotation(Vector3.right, offset.normalized);

            GetVertices(point, rot, out v1, out v2);

        }

        bool GetVerticesMid(Vector2 prevPoint, Vector2 point, Vector2 nextPoint, out Vector3 v1, out Vector3 v2, out Vector3 v3, out Vector3 v4)
        {

            Vector2 offsetA = point - prevPoint;
            Vector2 offsetB = nextPoint - point;

            Vector2 dirA = offsetA.normalized;

            Quaternion rotA = Quaternion.FromToRotation(Vector3.right, dirA);
            Quaternion rotB = Quaternion.FromToRotation(Vector3.right, offsetB.normalized);

            float angle = Quaternion.Angle(rotA, rotB);

            if (angle >= sharpAngleThreshold)
            {
                GetVertices(point, rotA, out v1, out v2);

                Vector3 dir = dirA;

                float d1 = (new Vector2(v1.x, v1.y) - nextPoint).sqrMagnitude;
                float d2 = (new Vector2(v2.x, v2.y) - nextPoint).sqrMagnitude;

                if (d1 >= d2) 
                {
                    v3 = v2;
                    v4 = v2 - (dir * thickness);
                } 
                else
                {
                    v4 = v1;
                    v3 = v1 - (dir * thickness);
                }
                return true;
            }
            else
            {
                Quaternion rot = Quaternion.Slerp(rotA, rotB, 0.5f);
                GetVertices(point, rot, out v1, out v2);
                v3 = v1;
                v4 = v2;
            }

            return false;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {

            vh.Clear();

            if (points == null || points.Length <= 1) return;

            var localRT = rectTransform;
            var rT = containerTransform == null ? localRT : containerTransform;
            bool isLocal = localRT == rT;
            
            var rect = rT.rect;
            var width = rect.width;
            var height = rect.height;

            float px = rT.pivot.x;
            float py = rT.pivot.y;

            Vector2 LocalToContainer(Vector2 point)
            {
                point.x = (point.x * width) - (width * px);
                point.y = (point.y * height) - (height * py);
                if (!isLocal) point = localRT.InverseTransformPoint(rT.TransformPoint(point));
                return point;
            }

            float GetValueOnSegment(Vector3 worldPosition, Vector3 pointA, Vector3 pointB)
            {
                Vector3 v = pointB - pointA;
                Vector3 u = pointA - worldPosition;

                float vu = v.x * u.x + v.y * u.y + v.z * u.z;
                float vv = v.x * v.x + v.y * v.y + v.z * v.z;
                float t = -vu / vv;

                return Mathf.Clamp01(t);
            }

            if (addStartCap) addStartCap = startCapResolution > 2;
            if (addEndCap) addEndCap = endCapResolution > 2;

            float halfPI = Mathf.PI * 0.5f;
            float halfThickness = thickness * 0.5f;

            float lineLength = 0;
            for (int a = 1; a < points.Length; a++) lineLength = lineLength + Vector3.Distance(LocalToContainer(points[a - 1]), LocalToContainer(points[a]));
            float startCapLength = addStartCap ? (halfThickness/* * startCapThicknessRatio*/) : 0;
            float endCapLength = addEndCap ? (halfThickness/* * endCapThicknessRatio*/) : 0;
            float fullLength = startCapLength + lineLength + endCapLength;
            float endCapLengthRatio = endCapLength / fullLength;

            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;

            int index = 0;
            float d = startCapLength;
            for (int a = 0; a < points.Length; a++)
            {

                bool isStart = a == 0;
                bool isEnd = a == points.Length - 1;

                Vector2 p1 = LocalToContainer(points[a == 0 ? a : a - 1]);
                Vector2 p2 = LocalToContainer(points[a == 0 ? a + 1 : a]); 

                float t;
                if (useEqualUVPortionsPerSegment)
                {
                    t = (startCapLength + ((a / (points.Length - 1f)) * lineLength)) / fullLength;
                }
                else
                {
                    if (a > 0) d = d + (p2 - p1).magnitude;
                    t = d / fullLength;
                }

                Vector3 v1_, v2_;

                if (isStart)
                {

                    GetVertices(p1, p2, out v1_, out v2_);

                    Vector3 offset = p1 - p2;

                    v1_ = v1_ + offset;
                    v2_ = v2_ + offset;

                    if (addStartCap)
                    {
                        Vector3 offsetDir = offset.normalized;
                        int centerVertexIndex = vh.currentVertCount;
                        Vector3 centerPos = (v1_ + v2_) * 0.5f;
                        Vector3 startPos = centerPos + offsetDir * halfThickness;
                        vertex.position = centerPos; // the center vertex to create points around
                        vertex.uv0 = new Vector4(GetUVX(t), GetUVY(0.5f), 0, 0);
                        vh.AddVert(vertex);
                        index++;
                        Vector3 tangent = (v1_ - centerPos).normalized;
                        for (int b = 0; b < startCapResolution; b++)
                        {
                            float capT = b / (startCapResolution - 1f);
                            float signedCenterT = ((capT - 0.5f) * 2);
                            float centerT = 1 - Mathf.Abs(signedCenterT);
                            var dir = Vector3.RotateTowards(tangent * -Mathf.Sign(signedCenterT), offsetDir, halfPI * centerT, 0);
                            var pos = centerPos + dir * halfThickness;
                            vertex.position = pos;
                            vertex.uv0 = new Vector4(GetUVX(GetValueOnSegment(pos, startPos, centerPos) * t), GetUVY(GetValueOnSegment(pos, v2_, v1_)), 0, 0);
                            var vIndex = index;
                            vh.AddVert(vertex);
                            index++;

                            if (b > 0)
                            {
                                vh.AddTriangle(centerVertexIndex, vIndex, vIndex - 1);
                            }
                        }
                    }

                }
                else
                {

                    bool skipTris = false;
                    if (a < points.Length - 1)
                    {

                        Vector2 p3 = LocalToContainer(points[a + 1]);

                        if (GetVerticesMid(p1, p2, p3, out v1_, out v2_, out Vector3 v3_, out Vector3 v4_)) 
                        {
                            // True means there's a sharp corner
                            vh.AddTriangle(index - 1, index - 2, index);
                            vh.AddTriangle(index + 1, index - 1, index);
                            vertex.position = v1_;
                            vertex.uv0 = new Vector4(GetUVX(t), GetUVY(1), 0, 0);
                            vh.AddVert(vertex);
                            vertex.position = v2_;
                            vertex.uv0 = new Vector4(GetUVX(t), GetUVY(0), 0, 0);
                            vh.AddVert(vertex);

                            index += 2;

                            v1_ = v3_;
                            v2_ = v4_;

                            skipTris = true;
                        } 

                    }
                    else
                    {

                        GetVertices(p1, p2, out v1_, out v2_); 

                    }

                    if (!skipTris)
                    {
                        vh.AddTriangle(index - 1, index - 2, index);
                        vh.AddTriangle(index + 1, index - 1, index);
                    }

                }

                vertex.position = v1_;
                vertex.uv0 = new Vector4(GetUVX(t), GetUVY(1), 0, 0);
                vh.AddVert(vertex);

                vertex.position = v2_;
                vertex.uv0 = new Vector4(GetUVX(t), GetUVY(0), 0, 0);
                vh.AddVert(vertex);

                index += 2;

                if (isEnd && addEndCap)
                {

                    Vector3 offsetDir = (p2 - p1).normalized;
                    int centerVertexIndex = vh.currentVertCount;
                    Vector3 centerPos = (v1_ + v2_) * 0.5f;
                    Vector3 endPos = centerPos + offsetDir * halfThickness;
                    vertex.position = centerPos; // the center vertex to create points around
                    vertex.uv0 = new Vector4(GetUVX(t), GetUVY(0.5f), 0, 0);
                    vh.AddVert(vertex);
                    index++;
                    Vector3 tangent = (v1_ - centerPos).normalized;
                    for (int b = 0; b < endCapResolution; b++)
                    {
                        float capT = b / (endCapResolution - 1f);
                        float signedCenterT = ((capT - 0.5f) * 2);
                        float centerT = 1 - Mathf.Abs(signedCenterT);
                        var dir = Vector3.RotateTowards(tangent * -Mathf.Sign(signedCenterT), offsetDir, halfPI * centerT, 0);
                        var pos = centerPos + dir * halfThickness;
                        vertex.position = pos;
                        vertex.uv0 = new Vector4(GetUVX((GetValueOnSegment(pos, centerPos, endPos) * endCapLengthRatio) + t), GetUVY(GetValueOnSegment(pos, v2_, v1_)), 0, 0);
                        var vIndex = index;
                        vh.AddVert(vertex);
                        index++;

                        if (b > 0)
                        {
                            vh.AddTriangle(centerVertexIndex, vIndex - 1, vIndex);
                        }
                    }

                }

            }

        }

    }

}

#endif