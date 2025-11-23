#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Swole.UI 
{

    /// <summary>
    /// A draggable (usually temporary) window or message.
    /// </summary>
    public class UIPopup : Selectable, IDragHandler
    {

        [Tooltip("The sorting logic that is applied when the element is clicked.")]
        public ElevationMethod elevationMethod;

        [Serializable, Flags]
        public enum ElevationMethod
        {

            None = 0, ChangePositionInHierarchy = 1, OverrideSorting = 2

        }

        public int sortingOrderRelativeOverride = 1;

        [Tooltip("How long to wait after being pressed before the element can be dragged.")]
        public float dragDelay;
        protected float dragCooldown;
        public bool disableChildDrag;
        public bool freeze;

        public bool closeOnClickOff;
        public MouseButtonMask clickOffButtonMask = MouseButtonMask.LeftMouseButton;
        public bool destroyOnClose;

        public RectTransform root;

        public UnityEvent OnClick = new UnityEvent();
        public UnityEvent OnClickRelease = new UnityEvent();
        public UnityEvent OnClose = new UnityEvent();
        public UnityEvent OnDragStart = new UnityEvent();
        public UnityEvent OnDragStep = new UnityEvent();
        public UnityEvent OnDragStop = new UnityEvent();

        public UnityEvent OnLeftClick = new UnityEvent();
        public UnityEvent OnLeftClickRelease = new UnityEvent();
        public UnityEvent OnMiddleClick = new UnityEvent();
        public UnityEvent OnMiddleClickRelease = new UnityEvent();
        public UnityEvent OnRightClick = new UnityEvent();
        public UnityEvent OnRightClickRelease = new UnityEvent();

        private bool ClickedOff()
        {
            if (clickOffButtonMask.HasFlag(MouseButtonMask.LeftMouseButton) && InputProxy.CursorPrimaryButtonDown)
            {
            }
            else if (clickOffButtonMask.HasFlag(MouseButtonMask.RightMouseButton) && InputProxy.CursorSecondaryButtonDown)
            {
            }
            else if (clickOffButtonMask.HasFlag(MouseButtonMask.MiddleMouseButton) && InputProxy.CursorAuxiliaryButtonDown)
            {
            }
            else return false;
            
            var objects = CursorProxy.ObjectsUnderCursor;
            if (objects == null) return true;
            for (int a = 0; a < objects.Count; a++)
            {
                var obj = objects[a];
                if (obj == null) continue;
                if (obj.transform.IsInHierarchy(root)) return false;
            }

            return true;

        }

        public void Close()
        {

#if UNITY_EDITOR
            if (!Application.isPlaying) return;
#endif

            OnClose?.Invoke();
            OnClose?.RemoveAllListeners();

            OnDragStart?.RemoveAllListeners();
            OnDragStep?.RemoveAllListeners();
            OnDragStop?.RemoveAllListeners();

            if (destroyOnClose)
            {
                GameObject.Destroy(gameObject);
            }
            else
            {
                gameObject.SetActive(false);

            }

        }

        protected override void OnDestroy()
        {

#if UNITY_EDITOR
            if (!Application.isPlaying) return;
#endif

            OnClose?.Invoke();
            OnClose.RemoveAllListeners();

            OnDragStart?.RemoveAllListeners();
            OnDragStep?.RemoveAllListeners();
            OnDragStop?.RemoveAllListeners();

            base.OnDestroy();

        }

        public delegate bool AutoCloseConditions(UIPopup popup);

        public AutoCloseConditions closeConditions;

        /// <summary>
        /// Move to target world position
        /// </summary>
        public void MoveTo(Vector3 targetWorldPosition)
        {
            Move(targetWorldPosition - transform.position);
        }
        /// <summary>
        /// Move by world translation
        /// </summary>
        public void Move(Vector3 translationWorld)
        {
            Move(rectTransform, root, canvasRect, canvas.transform.InverseTransformVector(translationWorld));
        }
        /// <summary>
        /// Move the localTransform with a translation, while making sure it stays inside the canvas.
        /// </summary>
        public static void Move(RectTransform localTransform, RectTransform root, RectTransform canvasRect, Vector3 translation)
        {

            Vector3 canvasSize = canvasRect.rect.size;

            Vector3 rootPos = canvasRect.InverseTransformPoint(root.position) + translation; 

            Vector2 rootSize = root.rect.size;

            Vector3 localPos = canvasRect.InverseTransformPoint(localTransform.position) + translation;

            Vector2 localSize = localTransform.rect.size;

            Vector3 newRootPos = new Vector3(
                Mathf.Max(Mathf.Min(rootPos.x, canvasSize.x - (rootSize.x - (rootSize.x * root.pivot.x)) - canvasSize.x * 0.5f), (rootSize.x * root.pivot.x) - canvasSize.x * 0.5f),
                Mathf.Max(Mathf.Min(rootPos.y, canvasSize.y - (rootSize.y - (rootSize.y * root.pivot.y)) - canvasSize.y * 0.5f), (rootSize.y * root.pivot.y) - canvasSize.y * 0.5f), 0);

            Vector3 newLocalPos = new Vector3(
                Mathf.Max(Mathf.Min(localPos.x, canvasSize.x - (localSize.x - (localSize.x * root.pivot.x)) - canvasSize.x * 0.5f), (localSize.x * root.pivot.x) - canvasSize.x * 0.5f),
                Mathf.Max(Mathf.Min(localPos.y, canvasSize.y - (localSize.y - (localSize.y * root.pivot.y)) - canvasSize.y * 0.5f), (localSize.y * root.pivot.y) - canvasSize.y * 0.5f), 0);

            Vector3 rootOffset = newRootPos - rootPos;
            Vector3 localOffset = newLocalPos - localPos;

            Vector3 offset = new Vector3(Mathf.Abs(localOffset.x) > Mathf.Abs(rootOffset.x) ? localOffset.x : rootOffset.x, Mathf.Abs(localOffset.y) > Mathf.Abs(rootOffset.y) ? localOffset.y : rootOffset.y, 0);

            rootPos = rootPos + offset;

            root.position = canvasRect.TransformPoint(rootPos);

        }

        public void LateUpdate()
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
                    OnDragStart?.Invoke();
                }
            }
            if (closeConditions != null)
            {

                if (closeConditions.Invoke(this))
                {

                    Close();

                    return;

                }

            }

            if (closeOnClickOff && ClickedOff())
            {
                Close();
                return;
            }

        }

        protected RectTransform rectTransform;
        public RectTransform RectTransform
        {
            get
            {
                if (rectTransform == null) rectTransform = gameObject.AddOrGetComponent<RectTransform>();

                return rectTransform;
            }
        }

        private Canvas canvas;
        protected bool CheckCanvas()
        {
            if (canvas == null)
            {
                canvas = GetComponentInParent<Canvas>();
                if (canvas != null) canvasRect = canvas.GetComponent<RectTransform>(); else return false;
            }

            return true;
        }

        protected RectTransform canvasRect;

        protected override void Awake()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) return;
#endif

            rectTransform = gameObject.AddOrGetComponent<RectTransform>();

            if (root == null) root = rectTransform;

            CheckCanvas();

            base.Awake();

        }

        public bool IsDragging => (dragCooldown <= 0 && this.IsPressed());
         
        protected Vector2 prevCursorPosition;

        private Canvas dragCanvas;
        private bool dragCanvasIsTemporary;
        private bool dragCanvasPrevOverrideSorting;
        private int dragCanvasPreSortingOrder;

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

        public virtual void Elevate()
        {
            if (!CheckCanvas()) return;
            if (elevationMethod != ElevationMethod.None)
            {
                if (elevationMethod.HasFlag(ElevationMethod.OverrideSorting)) // Move in front by using a canvas that overrides sorting. Only works while the element is being dragged.
                {
                    dragCanvasIsTemporary = false;
                    dragCanvas = gameObject.GetComponent<Canvas>();
                    if (dragCanvas == null)
                    {
                        dragCanvasIsTemporary = true;
                        dragCanvas = gameObject.AddComponent<Canvas>();
                    }
                    dragCanvasPrevOverrideSorting = dragCanvas.overrideSorting;
                    dragCanvasPreSortingOrder = dragCanvas.sortingOrder;
                    dragCanvas.overrideSorting = true;
                    dragCanvas.sortingOrder = canvas.sortingOrder + sortingOrderRelativeOverride;
                }

                if (elevationMethod.HasFlag(ElevationMethod.ChangePositionInHierarchy)) // Move in front by changing its position in the hierarchy. Only moves in front of elements in the same hierarchy. If it's parented to the top canvas then it moves in front of all other elements in that canvas.
                {
                    RectTransform.SetAsLastSibling();
                }
            }

        }

        public virtual void Delevate()
        {

            if (dragCanvasIsTemporary && dragCanvas != null) // Destroy temporary drag canvas if it exists.
            {
                Destroy(dragCanvas);
                dragCanvas = null;
            }

            if (elevationMethod != ElevationMethod.None)
            {
                if (!dragCanvasIsTemporary && dragCanvas != null) // Restore previous values if drag canvas is not temporary.
                {
                    dragCanvas.overrideSorting = dragCanvasPrevOverrideSorting;
                    dragCanvas.sortingOrder = dragCanvasPreSortingOrder;
                    dragCanvas = null;
                }
            }

        }

        private void ForceRefreshPrevCursorPosition()
        {
            if (!CheckCanvas()) return;
            prevCursorPosition = canvas.ScreenToCanvasSpace(CursorProxy.ScreenPosition);
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            if (!CheckCanvas()) return;
            OnClick?.Invoke();
            if (eventData.button == PointerEventData.InputButton.Left) OnLeftClick?.Invoke();
            if (eventData.button == PointerEventData.InputButton.Middle) OnMiddleClick?.Invoke();
            if (eventData.button == PointerEventData.InputButton.Right) OnRightClick?.Invoke();

            dragCooldown = dragDelay;
            preDragLocalPosition = rectTransform.localPosition;
            preDragPosition = rectTransform.position;

            //prevCursorPosition = canvas.ScreenToCanvasSpace(eventData.position); // not in screen space?
            prevCursorPosition = canvas.ScreenToCanvasSpace(CursorProxy.ScreenPosition);

            base.OnPointerDown(eventData);

            // Handles moving the element in front of its peers when clicked.
            Elevate();

            if (dragDelay <= 0) OnDragStart?.Invoke(); 
            

        } 

        public override void OnPointerUp(PointerEventData eventData) 
        {

            OnClickRelease?.Invoke();
            if (eventData.button == PointerEventData.InputButton.Left) OnLeftClickRelease?.Invoke();
            if (eventData.button == PointerEventData.InputButton.Middle) OnMiddleClickRelease?.Invoke();
            if (eventData.button == PointerEventData.InputButton.Right) OnRightClickRelease?.Invoke();

            base.OnPointerUp(eventData);

            // Move element back to previous level of elevation if necessary.
            Delevate();

            if (dragCooldown <= 0) OnDragStop?.Invoke();

            dragCooldown = 0; 

        }

        private int lastDragFrame; // quick fix for teleporting ui when dragging children
        public void OnDrag(PointerEventData eventData)
        {
            if (freeze || dragCooldown > 0) return;
            if (!CheckCanvas()) return;
            if (disableChildDrag)
            {
                var dragObj = eventData.rawPointerPress;
                if (dragObj == null) dragObj = eventData.lastPress;
                if (dragObj != null && dragObj != gameObject) return;
            }
             
            if (eventData.rawPointerPress != gameObject && Time.frameCount - lastDragFrame > 3)
            {
                // quick fix for teleporting ui when dragging children
                prevCursorPosition = canvas.ScreenToCanvasSpace(CursorProxy.ScreenPosition);
            }
            lastDragFrame = Time.frameCount; 

            //var cursorPos = canvas.ScreenToCanvasSpace(eventData.position); // not in screen space?
            var cursorPos = canvas.ScreenToCanvasSpace(CursorProxy.ScreenPosition);  
            Move(rectTransform, root, canvasRect, cursorPos - prevCursorPosition);
            prevCursorPosition = cursorPos;

            OnDragStep?.Invoke();
        }

    }

}

#endif