#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

namespace Swole.UI
{

    [ExecuteInEditMode, RequireComponent(typeof(CanvasRenderer))]
    public class UILineRenderer : MaskableGraphic
    {

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

            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;

            int index = 0;
            Vector2 LocalToContainer(Vector2 point)
            {
                point.x = (point.x * width) - (width * px);
                point.y = (point.y * height) - (height * py);
                if (!isLocal) point = localRT.InverseTransformPoint(rT.TransformPoint(point)); 
                return point;
            }
            for (int a = 0; a < points.Length; a++)
            {

                Vector2 p1 = LocalToContainer(points[a == 0 ? a : a - 1]);
                Vector2 p2 = LocalToContainer(points[a == 0 ? a + 1 : a]);

                Vector3 v1_, v2_;

                if (a == 0)
                {

                    GetVertices(p1, p2, out v1_, out v2_);

                    Vector3 offset = p1 - p2;

                    v1_ = v1_ + offset;
                    v2_ = v2_ + offset;

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
                            vh.AddVert(vertex);
                            vertex.position = v2_;
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
                vh.AddVert(vertex);

                vertex.position = v2_;
                vh.AddVert(vertex);

                index += 2;

            }

        }

    }

}

#endif