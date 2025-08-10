#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Events;
using UnityEngine.EventSystems;

using UnityEngine.UI;

namespace Swole
{

    public class PointerEventsProxy : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
    {

        [SerializeField]
        protected GameObject rootObject;
        public GameObject RootObject
        {
            get 
            {
                if (rootObject == null) return gameObject;
                return rootObject;
            }
            set => rootObject = value;
        }

        public UnityEvent OnClick;
        public UnityEvent OnDown;
        public UnityEvent OnUp;
        public UnityEvent OnEnter;
        public UnityEvent OnExit;
        public UnityEvent OnMove;

        [Header("Left Click")]
        public UnityEvent OnLeftClick;
        public UnityEvent OnLeftDown;
        public UnityEvent OnLeftUp;

        [Header("Middle Click")]
        public UnityEvent OnMiddleClick;
        public UnityEvent OnMiddleDown;
        public UnityEvent OnMiddleUp;

        [Header("Right Click")]
        public UnityEvent OnRightClick;
        public UnityEvent OnRightDown;
        public UnityEvent OnRightUp;

        public delegate void PointerDelegate(PointerEventData eventData);

        public event PointerDelegate OnClickEvent;
        public event PointerDelegate OnDownEvent;
        public event PointerDelegate OnUpEvent;
        public event PointerDelegate OnEnterEvent;
        public event PointerDelegate OnExitEvent;
        public event PointerDelegate OnMoveEvent;

        public event PointerDelegate OnLeftClickEvent;
        public event PointerDelegate OnLeftDownEvent;
        public event PointerDelegate OnLeftUpEvent;

        public event PointerDelegate OnMiddleClickEvent;
        public event PointerDelegate OnMiddleDownEvent;
        public event PointerDelegate OnMiddleUpEvent;

        public event PointerDelegate OnRightClickEvent;
        public event PointerDelegate OnRightDownEvent;
        public event PointerDelegate OnRightUpEvent;

        public void OnPointerClick(PointerEventData eventData)
        {
            RefreshMasking();
            OnClick?.Invoke();
            OnClickEvent?.Invoke(eventData);

            if (eventData.button == PointerEventData.InputButton.Left)
            {
                OnLeftClick?.Invoke();
                OnLeftClickEvent?.Invoke(eventData);
            }
            else if (eventData.button == PointerEventData.InputButton.Middle)
            {
                OnMiddleClick?.Invoke();
                OnMiddleClickEvent?.Invoke(eventData);
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                OnRightClick?.Invoke();
                OnRightClickEvent?.Invoke(eventData);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            OnDown?.Invoke();
            OnDownEvent?.Invoke(eventData);

            if (eventData.button == PointerEventData.InputButton.Left)
            {
                OnLeftDown?.Invoke();
                OnLeftDownEvent?.Invoke(eventData);
            }
            else if (eventData.button == PointerEventData.InputButton.Middle)
            {
                OnMiddleDown?.Invoke();
                OnMiddleDownEvent?.Invoke(eventData);
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                OnRightDown?.Invoke();
                OnRightDownEvent?.Invoke(eventData);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            OnUp?.Invoke();
            OnUpEvent?.Invoke(eventData);

            if (eventData.button == PointerEventData.InputButton.Left)
            {
                OnLeftUp?.Invoke();
                OnLeftUpEvent?.Invoke(eventData);
            }
            else if (eventData.button == PointerEventData.InputButton.Middle)
            {
                OnMiddleUp?.Invoke();
                OnMiddleUpEvent?.Invoke(eventData);
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                OnRightUp?.Invoke();
                OnRightUpEvent?.Invoke(eventData);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            RefreshMasking();
            OnEnter?.Invoke();
            OnEnterEvent?.Invoke(eventData);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            RefreshMasking();
            OnExit?.Invoke();
            OnExitEvent?.Invoke(eventData);
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            OnMove?.Invoke();
            OnMoveEvent?.Invoke(eventData);
        }

        protected void OnDisable()
        {
            OnPointerExit(null);
        }

        protected RectTransform rectTransform;

        public RectTransform RectTransform => rectTransform ?? (rectTransform = GetComponent<RectTransform>());

        protected Canvas canvas;

        public Canvas Canvas => canvas ?? (canvas = GetComponentInParent<Canvas>());

        protected Vector3[] corners = new Vector3[4];

        protected Mask mask;

        protected bool isMasked;

        public bool IsMasked => isMasked;

        public void RefreshMasking()
        {

            isMasked = false;

            if (mask == null)
            {

                mask = gameObject.GetComponentInParent<Mask>();

                if (mask == null) return;

            }

            Transform canvasTransform = null;

            if (Canvas != null) canvasTransform = canvas.transform;

            mask.rectTransform.GetWorldCorners(corners);

            Vector3 min, max;

            Bounds maskBounds = new Bounds();

            min = corners[0];
            max = corners[2];

            if (canvasTransform != null)
            {

                min = canvasTransform.InverseTransformPoint(min);
                max = canvasTransform.InverseTransformPoint(max);

            }

            maskBounds.min = new Vector3(min.x, min.y, -1);
            maskBounds.max = new Vector3(max.x, max.y, 1);


            RectTransform.GetWorldCorners(corners);

            Bounds proxyBounds = new Bounds();

            min = corners[0];
            max = corners[2];

            if (canvasTransform != null)
            {

                min = canvasTransform.InverseTransformPoint(min);
                max = canvasTransform.InverseTransformPoint(max);

            }

            proxyBounds.min = new Vector3(min.x, min.y, -1);
            proxyBounds.max = new Vector3(max.x, max.y, 1);

            isMasked = !(maskBounds.Intersects(proxyBounds) || maskBounds.Contains(proxyBounds.center));

        }

        protected int lastFrame;

        protected bool isHovering;

        public bool CheckIfCursorIsInBounds()
        {

            if (Canvas == null) return false;

            Camera camera = Camera.main;
            
            if (canvas.worldCamera != null) camera = canvas.worldCamera;
           
            Vector3 cursor = RectTransform.InverseTransformPoint(canvas.transform.TransformPoint(canvas.ScreenToCanvasSpace(InputProxy.CursorScreenPosition))); 

            rectTransform.GetLocalCorners(corners);

            float minX = Mathf.Min(corners[0].x, corners[3].x);
            float maxX = Mathf.Max(corners[0].x, corners[3].x);

            float minY = Mathf.Min(corners[0].y, corners[1].y);
            float maxY = Mathf.Max(corners[0].y, corners[1].y);

            return !(cursor.x < minX || cursor.x > maxX || cursor.y < minY || cursor.y > maxY);

        }

        public bool IsHovering
        {

            get
            {

                if (!enabled) return false;

                int frame = Time.frameCount;

                if (lastFrame == frame)
                {

                    return isHovering;

                }

                lastFrame = frame;

                //isHovering = isMasked || CheckIfCursorIsInBounds();

                isHovering = gameObject.activeInHierarchy && !isMasked && CheckIfCursorIsInBounds();

                return isHovering;

            }

        }

    }

}

#endif
