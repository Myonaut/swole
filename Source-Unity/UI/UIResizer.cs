#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Swole.UI 
{

    /// <summary>
    /// A selectable UI element that can be dragged to manipulate the size and shape of a RectTransform. Make a container object and parent this and the target to it if the target is also selectable/interactable, then set the targetRect field to the container object
    /// </summary>
    public class UIResizer : Selectable, IDragHandler
    {

        public float minWidth = 5;
        public float minHeight = 5;

        public float maxWidth = 10000;
        public float maxHeight = 10000;

        [Tooltip("Sets the anchor to be along the border of the targetRect. If the resizer is dead center inside the targetRect, then the anchor is centered instead (in which case the resizer does nothing when dragged).")]
        public bool pinToBorder = false;

        [Tooltip("Sets the anchor to the closest corner of the targetRect. If the resizer is dead center inside the targetRect, then the anchor is centered instead (in which case the resizer does nothing when dragged).")]
        public bool pinToCorner = true;

        [Tooltip("[Experimental/Unreliable] When dragging the resizer against the bounds of the canvas, should the target expand in the opposite direction?")]
        public bool expandAgainstObstructions = false;

        public RectTransform targetRect;

        protected RectTransform rectTransform;

        public Canvas canvas;

        protected RectTransform canvasRect;

        protected int signX, signY;

        protected Vector2 anchor;

        protected Vector3[] corners = new Vector3[4];

        protected override void Awake()
        {

            base.Awake();

            rectTransform = gameObject.GetComponent<RectTransform>();

            if (targetRect == null) 
            {
                Debug.LogWarning($"targetElement not set for {nameof(UIResizer)} '{name}'");
                enabled = false;
                return;
            } 
            else if (targetRect == rectTransform)
            {
                targetRect = null; 
                Debug.LogWarning($"targetElement for {nameof(UIResizer)} '{name}' was set to itself");
                enabled = false;
                return;
            }

            if (canvas == null) canvas = GetComponentInParent<Canvas>();

            if (canvas == null)
            {

                Debug.LogWarning($"{nameof(UIResizer)} '{name}' was not inside of a canvas and has been destroyed");
                GameObject.Destroy(gameObject);
                return;

            }

            canvasRect = canvas.GetComponent<RectTransform>();

            targetRect.GetLocalCorners(corners);

            Vector3 worldPos = rectTransform.position;

            Vector3 localPos = targetRect.InverseTransformPoint(worldPos);

            Vector3 localPos_ = localPos;

            localPos = new Vector3((localPos.x - corners[0].x) / (corners[3].x - corners[0].x), (localPos.y - corners[0].y) / (corners[1].y - corners[0].y), 0);

            signX = localPos.x < 0.5f ? -1 : localPos.x > 0.5f ? 1 : 0;
            signY = localPos.y < 0.5f ? -1 : localPos.y > 0.5f ? 1 : 0;

            anchor = localPos;

            if (pinToCorner)
            {

                anchor = new Vector2(signX < 0 ? 0 : signX > 0 ? 1 : 0, signY < 0 ? 0 : signY > 0 ? 1 : 0);

            } 
            else if (pinToBorder) // Sets the anchor to be along the border of the targetRect. If the resizer is dead center inside the targetRect, then the anchor is centered instead (in which case it does nothing when dragged).
            {
                
                if (signX == 0 && signY == 0) 
                {
                    anchor = new Vector2(0.5f, 0.5f);
                }
                else
                {
                    anchor = anchor - new Vector2(0.5f, 0.5f);
                    anchor = anchor.normalized;
                    float absX = Mathf.Abs(anchor.x);
                    float absY = Mathf.Abs(anchor.y);
                    float max = Mathf.Max(absX, absY);
                    float add = 1 - max;
                    anchor.x = ((anchor.x + (add * signX)) + 1) / 2f; 
                    anchor.y = ((anchor.y + (add * signY)) + 1) / 2f;
                }

            }

            rectTransform.anchorMin = rectTransform.anchorMax = anchor;
            anchor = (anchor - new Vector2(0.5f, 0.5f)) * 2f;
            rectTransform.position = targetRect.TransformPoint(localPos_);

        }

        protected Vector3 prevCursorPosition;

        public override void OnPointerDown(PointerEventData eventData)
        {

            prevCursorPosition = eventData.position;//InputProxy.CursorPosition;
            base.OnPointerDown(eventData);

        }

        public bool IsDragging => IsPressed();

        protected Vector3[] corners2 = new Vector3[4];

        public void OnDrag(PointerEventData eventData)
        {

            canvasRect.GetLocalCorners(corners2);

            Vector3 cursorPosition = eventData.position;//InputProxy.CursorPosition;

            Vector3 translation = cursorPosition - prevCursorPosition; // Calculate translation vector from cursor that is dragging the resizer

            targetRect.GetLocalCorners(corners);

            translation.x = translation.x * signX;
            translation.y = translation.y * signY;

            corners[0] = canvasRect.InverseTransformPoint(targetRect.TransformPoint(corners[0] + new Vector3(Mathf.Min(signX, 0) * translation.x, Mathf.Min(signY, 0) * translation.y, 0))); // Convert targetRect corner to canvas space with translation applied
            corners[1] = canvasRect.InverseTransformPoint(targetRect.TransformPoint(corners[1] + new Vector3(Mathf.Min(signX, 0) * translation.x, Mathf.Max(signY, 0) * translation.y, 0))); // Convert targetRect corner to canvas space with translation applied
            corners[2] = canvasRect.InverseTransformPoint(targetRect.TransformPoint(corners[2] + new Vector3(Mathf.Max(signX, 0) * translation.x, Mathf.Max(signY, 0) * translation.y, 0))); // Convert targetRect corner to canvas space with translation applied
            corners[3] = canvasRect.InverseTransformPoint(targetRect.TransformPoint(corners[3] + new Vector3(Mathf.Max(signX, 0) * translation.x, Mathf.Min(signY, 0) * translation.y, 0))); // Convert targetRect corner to canvas space with translation applied

            float width = Mathf.Clamp(corners[3].x - corners[0].x, minWidth, maxWidth); // Keep targetRect width within thresholds
            float height = Mathf.Clamp(corners[1].y - corners[0].y, minHeight, maxHeight); // Keep targetRect height within thresholds

            float constrainX = width - (corners[3].x - corners[0].x); // Calculate out-of-bounds width
            float constrainY = height - (corners[1].y - corners[0].y); // Calculate out-of-bounds height

            corners[0] = corners[0] + new Vector3(Mathf.Min(signX, 0) * constrainX, Mathf.Min(signY, 0) * constrainY, 0); // Apply size constraints
            corners[1] = corners[1] + new Vector3(Mathf.Min(signX, 0) * constrainX, Mathf.Max(signY, 0) * constrainY, 0); // Apply size constraints
            corners[2] = corners[2] + new Vector3(Mathf.Max(signX, 0) * constrainX, Mathf.Max(signY, 0) * constrainY, 0); // Apply size constraints
            corners[3] = corners[3] + new Vector3(Mathf.Max(signX, 0) * constrainX, Mathf.Min(signY, 0) * constrainY, 0); // Apply size constraints

            Vector3 ogC0 = corners[0];
            Vector3 ogC1 = corners[1];
            Vector3 ogC2 = corners[2];
            Vector3 ogC3 = corners[3];

            corners[0] = new Vector3(Mathf.Clamp(corners[0].x, corners2[0].x, corners2[3].x), Mathf.Clamp(corners[0].y, corners2[0].y, corners2[1].y), corners[0].z); // Keep corners inside canvas
            corners[1] = new Vector3(Mathf.Clamp(corners[1].x, corners2[0].x, corners2[3].x), Mathf.Clamp(corners[1].y, corners2[0].y, corners2[1].y), corners[1].z); // Keep corners inside canvas
            corners[2] = new Vector3(Mathf.Clamp(corners[2].x, corners2[0].x, corners2[3].x), Mathf.Clamp(corners[2].y, corners2[0].y, corners2[1].y), corners[2].z); // Keep corners inside canvas
            corners[3] = new Vector3(Mathf.Clamp(corners[3].x, corners2[0].x, corners2[3].x), Mathf.Clamp(corners[3].y, corners2[0].y, corners2[1].y), corners[3].z); // Keep corners inside canvas

            ogC0 = corners[0] - ogC0;
            ogC1 = corners[1] - ogC1;
            ogC2 = corners[2] - ogC2;
            ogC3 = corners[3] - ogC3;

            float pushBackXMax = Mathf.Max(ogC0.x, ogC1.x, ogC2.x, ogC3.x, 0);
            float pushBackXMin = Mathf.Min(ogC0.x, ogC1.x, ogC2.x, ogC3.x, 0);

            float pushBackYMax = Mathf.Max(ogC0.y, ogC1.y, ogC2.y, ogC3.y, 0);
            float pushBackYMin = Mathf.Min(ogC0.y, ogC1.y, ogC2.y, ogC3.y, 0);

            if (expandAgainstObstructions)
            {

                Vector3 pushBack = new Vector3(pushBackXMin + pushBackXMax, pushBackYMin + pushBackYMax, 0) * 1.001f;

                // Move element back into canvas without resizing it. This allows it to keep expanding.
                corners[0] = corners[0] + pushBack;
                corners[1] = corners[1] + pushBack;
                corners[2] = corners[2] + pushBack;
                corners[3] = corners[3] + pushBack;

            }
            else
            {

                // Force back into canvas by resizing
                corners[0] = corners[0] + new Vector3(pushBackXMax, pushBackYMax);
                corners[1] = corners[1] + new Vector3(pushBackXMax, pushBackYMin);
                corners[2] = corners[2] + new Vector3(pushBackXMin, pushBackYMin);
                corners[3] = corners[3] + new Vector3(pushBackXMin, pushBackYMax);

            }

            Vector3 elementPos = new Vector3(Mathf.Lerp(corners[0].x, corners[3].x, targetRect.pivot.x), Mathf.Lerp(corners[0].y, corners[1].y, targetRect.pivot.y), (corners[0].z + corners[1].z + corners[2].z + corners[3].z) / 4f);

            width = (corners[3].x - corners[0].x);
            height = (corners[1].y - corners[0].y);

            targetRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            targetRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);

            targetRect.position = canvasRect.TransformPoint(elementPos);

            prevCursorPosition = cursorPosition;

        }

    }

}

#endif
