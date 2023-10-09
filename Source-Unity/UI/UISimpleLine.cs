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

        protected Image image;

        public Image Image
        {

            get
            {

                if (image == null) image = gameObject.AddOrGetComponent<Image>();

                return image;

            }

        }

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

            if (pointStart == pointEnd) return;

            Vector3 min = pointStart;
            Vector3 max = pointEnd;

            rectTransform.localPosition = (min + max) / 2; 

            Vector3 dif = max - min;

            float mag = dif.magnitude;

            if (dif.x == 0) return;

            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, mag);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, thickness);

            rectTransform.localRotation = Quaternion.Euler(new Vector3(0, 0, 180 * Mathf.Atan(dif.y / dif.x) / Mathf.PI));

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