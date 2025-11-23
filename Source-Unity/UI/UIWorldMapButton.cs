#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using TMPro;

namespace Swole.UI
{
    [RequireComponent(typeof(Button))]
    public class UIWorldMapButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {

        [SerializeField]
        protected RectTransform rootTransform;

        [SerializeField]
        protected RectTransform arrowTransform;

        public Vector2 arrowStartDir = new Vector2(0f, 1f);

        public Vector3 worldPosition;
        [SerializeField]
        protected Transform positionTransform;

        public bool hasDestination;
        public Vector3 destinationWorldPosition;
        [SerializeField]
        protected Transform destinationTransform;

        protected Button button;

        protected Canvas canvas;

        [SerializeField]
        protected RectTransform scalingTransform;
        public Vector3 defaultScale = Vector3.one;
        public Vector3 hoveredScale = new Vector3(1.05f, 1.05f, 1.05f);
        public float scalingTime = 0.2f;

        [SerializeField]
        protected TMP_Text textToShow;
        private Color textColor;

        private IEnumerator StartHovering()
        {
            float t = 0f;
            while(t < scalingTime)
            {
                t = Mathf.Min(t + Time.deltaTime, scalingTime);
                float t_ = t / scalingTime;
                t_ = Mathf.SmoothStep(0f, 1f, t_);

                if (scalingTransform != null) scalingTransform.localScale = Vector3.LerpUnclamped(defaultScale, hoveredScale, t_);
                if (textToShow != null) textToShow.color = Color.LerpUnclamped(new Color(textColor.r, textColor.g, textColor.b, 0f), textColor, t_);
                yield return null;
            }
        }
        private IEnumerator StopHovering()
        {
            float t = 0f;
            while (t < scalingTime)
            {
                t = Mathf.Min(t + Time.deltaTime, scalingTime);
                float t_ = t / scalingTime;
                t_ = Mathf.SmoothStep(0f, 1f, t_);

                if (scalingTransform != null) scalingTransform.localScale = Vector3.LerpUnclamped(hoveredScale, defaultScale, t_);
                if (textToShow != null) textToShow.color = Color.LerpUnclamped(textColor, new Color(textColor.r, textColor.g, textColor.b, 0f), t_);
                yield return null;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            StartCoroutine(StartHovering());
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            StartCoroutine(StopHovering());
        }

        protected void Awake()
        {
            canvas = gameObject.GetComponentInParent<Canvas>(true);

            button = gameObject.AddOrGetComponent<Button>();

            if (rootTransform == null) rootTransform = transform.GetComponent<RectTransform>();

            if (textToShow != null) 
            { 
                textColor = textToShow.color;
                textToShow.color = new Color(textColor.r, textColor.g, textColor.b, 0f);
            }
        }

        protected void Start()
        {
            if (hasDestination)
            {
                if (arrowTransform != null) arrowTransform.gameObject.SetActive(true);
            } 
            else
            {
                if (arrowTransform != null) arrowTransform.gameObject.SetActive(false); 
            }

            if (scalingTransform == null) scalingTransform = transform.GetComponent<RectTransform>();
        }

        protected void Update()
        {
            if (positionTransform != null) worldPosition = positionTransform.position;
            if (destinationTransform != null) destinationWorldPosition = destinationTransform.position;
     
            var canvasPos = canvas.WorldToCanvasSpace(worldPosition);
            canvasPos.z = 0f;
            if (rootTransform != null) rootTransform.position = canvas.transform.TransformPoint(canvasPos);

            if (hasDestination && arrowTransform != null)
            {
                var destCanvasPos = canvas.WorldToCanvasSpace(destinationWorldPosition);
                destCanvasPos.z = 0f;

                arrowTransform.localRotation = Quaternion.Euler(0f, 0f, Vector3.SignedAngle(new Vector3(arrowStartDir.x, arrowStartDir.y, 0f), destCanvasPos - canvasPos, Vector3.forward));
            }
        }

    }
}

#endif