#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

namespace Swole.UI
{

    [ExecuteInEditMode, RequireComponent(typeof(Image))]
    public class UISimpleLine : MonoBehaviour
    {

        [SerializeField]
        private float thickness = 5;

        public virtual void SetThickness(float thickness)
        {
            this.thickness = thickness;
            Refresh();
        }

        [SerializeField]
        protected Vector3 pointStart, pointEnd;
        public Vector3 PointStart => pointStart;
        public Vector3 PointEnd => pointEnd;

        public virtual void SetStartPoint(Vector3 pointStart)
        {
            this.pointStart = pointStart;
            Refresh();
        }
        public virtual void SetEndPoint(Vector3 pointEnd)
        {
            this.pointEnd = pointEnd;
            Refresh();
        }
        public virtual void SetStartPointAndThickness(Vector3 pointStart, float thickness)
        {
            this.thickness = thickness;
            SetStartPoint(pointStart);
        }
        public virtual void SetEndPointAndThickness(Vector3 pointEnd, float thickness)
        {
            this.thickness = thickness;
            SetEndPoint(pointEnd);
        }

        private Transform parent;
        public virtual void SetWorldStartPoint(Vector3 pointStartWorld)
        {
            if (parent == null) parent = rectTransform.parent;
            if (parent != null) pointStartWorld = parent.InverseTransformPoint(pointStartWorld);

            SetStartPoint(pointStartWorld);
        }
        public virtual void SetWorldEndPoint(Vector3 pointEndWorld)
        {
            if (parent == null) parent = rectTransform.parent;
            if (parent != null) pointEndWorld = parent.InverseTransformPoint(pointEndWorld);

            SetEndPoint(pointEndWorld);
        }
        public virtual void SetWorldStartPointAndThickness(Vector3 pointStartWorld, float thickness)
        {
            this.thickness = thickness;
            SetWorldStartPoint(pointStartWorld);
        }
        public virtual void SetWorldEndPointAndThickness(Vector3 pointEndWorld, float thickness)
        {
            this.thickness = thickness;
            SetWorldEndPoint(pointEndWorld);
        }

        public virtual void SetPoints(Vector3 pointStart, Vector3 pointEnd)
        {
            this.pointStart = pointStart;
            this.pointEnd = pointEnd;
            Refresh();
        }
        public virtual void SetPointsAndThickness(Vector3 pointStart, Vector3 pointEnd, float thickness)
        {
            this.thickness = thickness;
            SetPoints(pointStart, pointEnd);
        }

        public virtual void SetWorldPoints(Vector3 pointStartWorld, Vector3 pointEndWorld)
        {
            if (parent == null) parent = rectTransform.parent;
            if (parent != null)
            {
                pointStart = parent.InverseTransformPoint(pointStartWorld);
                pointEnd = parent.InverseTransformPoint(pointEndWorld);
            }

            Refresh();
        }
        public virtual void SetWorldPointsAndThickness(Vector3 pointStart, Vector3 pointEnd, float thickness)
        {
            this.thickness = thickness;
            SetWorldPoints(pointStart, pointEnd);
        }

        protected Image image;

        public Image Image
        {

            get
            {

                if (image == null) image = gameObject.AddOrGetComponent<Image>();

                return image;

            }

        }

        public void SetRaycastTarget(bool isRaycastTarget) => Image.raycastTarget = isRaycastTarget;

        public Color Color { get { return Image.color; } set { Image.color = value; } }

        protected RectTransform rectTransform;

        public RectTransform RectTransform
        {

            get
            {

                if (rectTransform == null) rectTransform = gameObject.AddOrGetComponent<RectTransform>();

                return rectTransform;

            }

        }


        protected void Awake()
        {

            rectTransform = gameObject.AddOrGetComponent<RectTransform>();

            image = gameObject.AddOrGetComponent<Image>();

        }

        public virtual void Refresh()
        {
            if (pointStart == pointEnd || float.IsNaN(pointStart.x) || float.IsNaN(pointStart.y) || float.IsNaN(pointEnd.x) || float.IsNaN(pointEnd.y)) 
            {
                Image.enabled = false;
                return;
            }

            Image.enabled = true; 

            Vector3 min = pointStart;
            Vector3 max = pointEnd;

            RectTransform.localPosition = (min + max) / 2; 

            Vector3 dif = max - min;

            float mag = dif.magnitude;

            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, mag);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, thickness);

            rectTransform.localRotation = Quaternion.Euler(new Vector3(0, 0, dif.x == 0 ? 90 : (180 * Mathf.Atan(dif.y / dif.x) / Mathf.PI)));
             
        }

#if UNITY_EDITOR

        public void OnValidate()
        {

            Refresh();

        }

#endif

    }

}

#endif