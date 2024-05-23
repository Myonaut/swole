#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using Swole.API.Unity.Animation;

namespace Swole.UI
{
    public class UIDraggable : Selectable, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {

        [Tooltip("How long to wait after being pressed before the element can be dragged.")]
        public float dragDelay;
        protected float dragCooldown;
        public bool disableChildDrag;
        public bool freeze;
        public bool disableChildClick;
        public bool cancelClickOnDrag;
        [HideInInspector]
        public bool cancelNextClick;

        public bool destroyOnClose;

        public RectTransform root;

        public MouseButtonMask clickMouseButtonMask = MouseButtonMask.All;

        public UnityEvent OnClick = new UnityEvent();
        public UnityEvent OnPress = new UnityEvent();
        [NonSerialized]
        public PointerEventData.InputButton lastClickButton;

        public MouseButtonMask dragMouseButtonMask = MouseButtonMask.All;

        public UnityEvent OnDragStart = new UnityEvent();
        public UnityEvent OnDragStep = new UnityEvent();
        public UnityEvent OnDragStop = new UnityEvent();
        [NonSerialized]
        public PointerEventData.InputButton lastDragButton;

        public bool IsDragging => (dragCooldown <= 0 && this.IsPressed());
        protected Vector2 prevCursorPosition;
        public Vector2 DragCursorPositionCanvas => prevCursorPosition;
        protected Vector2 prevCursorPositionLocal;
        public Vector2 DragCursorPositionLocal => prevCursorPositionLocal;
        protected Vector2 prevCursorPositionWorld;
        public Vector2 DragCursorPosition => prevCursorPositionWorld;
        public virtual void ForceRefreshPrevCursorPosition()
        {
            if (!CheckCanvas()) return;

            prevCursorPosition = AnimationCurveEditorUtils.ScreenToCanvasSpace(canvas, AnimationCurveEditor.InputProxyGlobal.CursorScreenPosition);
            prevCursorPositionWorld = canvasRect.TransformPoint(prevCursorPosition);
            prevCursorPositionLocal = rectTransform.InverseTransformPoint(prevCursorPositionWorld);
        }

        protected Vector3 preDragPosition;
        /// <summary>
        /// The world position of the element before it started being dragged.
        /// </summary>
        public Vector3 PreDragPosition => preDragPosition;

        protected Vector3 preDragLocalPosition;
        /// <summary>
        /// The local position of the element before it started being dragged.
        /// </summary>
        public Vector3 PreDragLocalPosition => preDragLocalPosition;

        protected Vector3 dragStartClickPosition;
        /// <summary>
        /// The world position of the cursor when the element started being dragged.
        /// </summary>
        public Vector3 DragStartClickPosition => dragStartClickPosition;

        protected Vector3 dragStartLocalClickPosition;
        /// <summary>
        /// The local position of the cursor when the element started being dragged.
        /// </summary>
        public Vector3 DragStartLocalClickPosition => dragStartLocalClickPosition;

        protected Vector3 dragStartScreenClickPosition;
        /// <summary>
        /// The screen position of the cursor when the element started being dragged.
        /// </summary>
        public Vector3 DragStartScreenClickPosition => dragStartScreenClickPosition;

        protected RectTransform rectTransform;
        private Canvas canvas;
        protected RectTransform canvasRect;
        protected bool CheckCanvas()
        {
            if (canvas == null)
            {
                canvas = GetComponentInParent<Canvas>();
                if (canvas != null) canvasRect = canvas.GetComponent<RectTransform>(); else return false;
            }

            return true;
        }

        protected override void OnDestroy()
        {

#if UNITY_EDITOR
            if (!Application.isPlaying) return;
#endif

            OnDragStart?.RemoveAllListeners();
            OnDragStep?.RemoveAllListeners();
            OnDragStop?.RemoveAllListeners();

            base.OnDestroy();

        }

        protected override void Awake()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) return;
#endif

            rectTransform = gameObject.GetComponent<RectTransform>();

            if (root == null) root = rectTransform;

            CheckCanvas();

            base.Awake();

        }

        public virtual void LateUpdate()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) return;
#endif
            if (dragCooldown > 0 && this.IsPressed())
            {
                dragCooldown -= Time.unscaledDeltaTime;
                if (dragCooldown <= 0)
                {
                    ForceRefreshPrevCursorPosition();
                    if (cancelClickOnDrag) cancelNextClick = true; 
                    OnDragStart?.Invoke();

                }
            }
        }

        public virtual void OnBeginDrag(PointerEventData eventData)
        {
            if (!CheckCanvas()) return;

            if (eventData.button == PointerEventData.InputButton.Left && !dragMouseButtonMask.HasFlag(MouseButtonMask.LeftMouseButton) ||
                eventData.button == PointerEventData.InputButton.Right && !dragMouseButtonMask.HasFlag(MouseButtonMask.RightMouseButton) ||
                eventData.button == PointerEventData.InputButton.Middle && !dragMouseButtonMask.HasFlag(MouseButtonMask.MiddleMouseButton)) return;

            if (disableChildDrag)
            {
                var obj = eventData.rawPointerPress;
                if (obj == null) obj = eventData.lastPress;
                if (obj != null && obj != gameObject) return;
            }

            lastDragButton = eventData.button;

            dragCooldown = dragDelay;
            preDragLocalPosition = rectTransform.localPosition;
            preDragPosition = rectTransform.position;

            prevCursorPosition = AnimationCurveEditorUtils.ScreenToCanvasSpace(canvas, eventData.position);

            dragStartScreenClickPosition = eventData.position;
            dragStartClickPosition = canvasRect.TransformPoint(prevCursorPosition);
            dragStartLocalClickPosition = root == null ? dragStartClickPosition : root.InverseTransformPoint(dragStartClickPosition); 

            if (dragDelay <= 0) 
            {
                if (cancelClickOnDrag) cancelNextClick = true;
                OnDragStart?.Invoke();
            }
        }

        private Vector3 lastDragTranslation;
        /// <summary>
        /// Last drag translation vector in world space.
        /// </summary>
        public Vector3 LastDragTranslation => lastDragTranslation;
        private Vector3 lastDragTranslationCanvas;
        /// <summary>
        /// Last drag translation vector in canvas space.
        /// </summary>
        public Vector3 LastDragTranslationCanvas => lastDragTranslationCanvas;
        public virtual void OnDrag(PointerEventData eventData)
        {
            if (dragCooldown > 0) return;
            if (!CheckCanvas()) return;

            if (eventData.button == PointerEventData.InputButton.Left && !dragMouseButtonMask.HasFlag(MouseButtonMask.LeftMouseButton) ||
                eventData.button == PointerEventData.InputButton.Right && !dragMouseButtonMask.HasFlag(MouseButtonMask.RightMouseButton) ||
                eventData.button == PointerEventData.InputButton.Middle && !dragMouseButtonMask.HasFlag(MouseButtonMask.MiddleMouseButton)) return;

            if (disableChildDrag)
            {
                var obj = eventData.rawPointerPress;
                if (obj == null) obj = eventData.lastPress;
                if (obj != null && obj != gameObject) return;
            }

            lastDragButton = eventData.button;

            var cursorPos = AnimationCurveEditorUtils.ScreenToCanvasSpace(canvas, eventData.position);
            lastDragTranslationCanvas = cursorPos - prevCursorPosition;
            lastDragTranslation = canvasRect.TransformVector(lastDragTranslationCanvas);
            if (!freeze) root.position = root.position + lastDragTranslation; 
            prevCursorPosition = cursorPos;
            prevCursorPositionWorld = canvasRect.TransformPoint(prevCursorPosition);
            prevCursorPositionLocal = rectTransform.InverseTransformPoint(prevCursorPositionWorld);

            OnDragStep?.Invoke();
        }

        public virtual void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && !dragMouseButtonMask.HasFlag(MouseButtonMask.LeftMouseButton) ||
                eventData.button == PointerEventData.InputButton.Right && !dragMouseButtonMask.HasFlag(MouseButtonMask.RightMouseButton) ||
                eventData.button == PointerEventData.InputButton.Middle && !dragMouseButtonMask.HasFlag(MouseButtonMask.MiddleMouseButton)) return;

            if (disableChildDrag)
            {
                var obj = eventData.rawPointerPress;
                if (obj == null) obj = eventData.lastPress;
                if (obj != null && obj != gameObject) return;
            }

            lastDragButton = eventData.button;

            if (dragCooldown <= 0) OnDragStop?.Invoke();
            dragCooldown = 0;
        }

        protected Vector3 lastClickPosition;
        /// <summary>
        /// The world position of the cursor when the element was last clicked.
        /// </summary>
        public Vector3 LastClickPosition => lastClickPosition;

        protected Vector3 lastClickLocalPosition;
        /// <summary>
        /// The local position of the cursor when the element was last clicked.
        /// </summary>
        public Vector3 LastClickLocalPosition => lastClickLocalPosition;

        protected Vector3 lastClickScreenPosition;
        /// <summary>
        /// The screen position of the cursor when the element was last clicked.
        /// </summary>
        public Vector3 LastClickScreenPosition => lastClickScreenPosition;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && !clickMouseButtonMask.HasFlag(MouseButtonMask.LeftMouseButton) ||
                eventData.button == PointerEventData.InputButton.Right && !clickMouseButtonMask.HasFlag(MouseButtonMask.RightMouseButton) ||
                eventData.button == PointerEventData.InputButton.Middle && !clickMouseButtonMask.HasFlag(MouseButtonMask.MiddleMouseButton)) return;

            if (disableChildClick)
            {
                var obj = eventData.rawPointerPress;
                if (obj == null) obj = eventData.lastPress;
                if (obj != null && obj != gameObject) return;
            }

            if (cancelNextClick)
            {
                cancelNextClick = false;
                return;
            }

            lastClickButton = eventData.button;
            if (CheckCanvas())
            {
                lastClickScreenPosition = eventData.pressPosition;
                lastClickPosition = canvasRect.TransformPoint(AnimationCurveEditorUtils.ScreenToCanvasSpace(canvas, lastClickScreenPosition));
                lastClickLocalPosition = root == null ? lastClickPosition : root.InverseTransformPoint(lastClickPosition);
            } 

            OnClick?.Invoke();
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && !clickMouseButtonMask.HasFlag(MouseButtonMask.LeftMouseButton) ||
                eventData.button == PointerEventData.InputButton.Right && !clickMouseButtonMask.HasFlag(MouseButtonMask.RightMouseButton) ||
                eventData.button == PointerEventData.InputButton.Middle && !clickMouseButtonMask.HasFlag(MouseButtonMask.MiddleMouseButton)) return;

            base.OnPointerDown(eventData);

            if (disableChildClick)
            {
                var obj = eventData.rawPointerPress;
                if (obj == null) obj = eventData.lastPress;
                if (obj != null && obj != gameObject) return;
            }

            lastClickButton = eventData.button;
            if (CheckCanvas())
            {
                lastClickScreenPosition = eventData.pressPosition;
                lastClickPosition = canvasRect.TransformPoint(AnimationCurveEditorUtils.ScreenToCanvasSpace(canvas, lastClickScreenPosition));
                lastClickLocalPosition = root == null ? lastClickPosition : root.InverseTransformPoint(lastClickPosition);
            }

            OnPress?.Invoke();
        }
    }
}

#endif