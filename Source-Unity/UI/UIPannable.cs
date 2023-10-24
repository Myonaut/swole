#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Swole.UI
{

    /// <summary>
    /// A UI element that allows the user to scroll around its contents. Requires at least one scroll bar to be assigned for it to work.
    /// </summary>
    public class UIPannable : UIFocusable
    {

        //public bool constrainSizeToContentSize;

        public float panningSpeedMultiplier = 1;

        public float scollSpeedMultiplier = 1;

        /// <summary>
        /// The normalized starting horizontal scroll position.
        /// </summary>
        [Range(0, 1)]
        public float startHor = 0f;

        /// <summary>
        /// The normalized starting vertical scroll position.
        /// </summary>
        [Range(0, 1)]
        public float startVer = 0f; 

        /// <summary>
        /// The normalized horizontal position of the content's center point.
        /// </summary>
        [Range(0, 1)]
        public float contentCenterHor = 0.5f;

        /// <summary>
        /// The normalized vertical position of the content's center point.
        /// </summary>
        [Range(0, 1)]
        public float contentCenterVer = 0.5f;

        public RectOffset padding;

        public RectTransform content;

        public Scrollbar scrollbarHor;

        public bool invertHor = false;

        public Scrollbar scrollbarVer;

        public bool invertVer = true;

        protected Vector3[] localCorners = new Vector3[4];

        protected Vector3[] contentCorners = new Vector3[4];

        public override void AwakeLocal()
        {

            base.AwakeLocal();

            if (padding == null) padding = new RectOffset();

            rectTransform = gameObject.AddOrGetComponent<RectTransform>();

            Vector2 localSize = rectTransform.sizeDelta;
            Vector2 contentSize = content.sizeDelta;

            float sizeX = localSize.x == 0 ? 1 : (contentSize.x / localSize.x); // Prevent divide by zero
            float sizeY = localSize.x == 0 ? 1 : (contentSize.y / localSize.y); // Prevent divide by zero

            if (scrollbarHor != null)
            {

                scrollbarHor.value = startHor;
                
                scrollbarHor.size = 1 / sizeX;

            }

            if (scrollbarVer != null)
            {

                scrollbarVer.value = startVer;

                scrollbarVer.size = 1 / sizeY;

            }

            prevCursorState = InputProxy.CursorLockState;

            lockedCursor = false;

            cursorVisible = InputProxy.IsCursorVisible;

        }

        protected virtual void Start()
        { 


            if (scrollbarHor != null && scrollbarHor.handleRect != null) // Possible workaround to stop scrollbar handle from occasionally disappearing? (It disappears because its position becomes NaN)
            {

                scrollbarHor.handleRect.anchoredPosition = Vector2.zero;

            }

            if (scrollbarVer != null && scrollbarVer.handleRect != null) // Possible workaround to stop scrollbar handle from occasionally disappearing? (It disappears because its position becomes NaN)
            {

                scrollbarVer.handleRect.anchoredPosition = Vector2.zero;

            }

        }

        protected bool lockedCursor = false;

        protected CursorLockMode prevCursorState = CursorLockMode.None;

        protected Vector2 cursorLockPosition;

        protected bool cursorVisible = true;

        public override void LateUpdateLocal()
        {

            base.LateUpdateLocal();

            if (content == null) return;

            if (lockedCursor && (!InputProxy.Panning || !InputProxy.CursorPrimaryButton))
            {

                InputProxy.CursorLockState = prevCursorState;

                InputProxy.CursorPosition = cursorLockPosition;

                InputProxy.IsCursorVisible = cursorVisible;

                lockedCursor = false;

            }

            if (IsInFocus || lockedCursor/* || CursorProxy.ObjectsUnderCursor.IsInHierarchy(transform)*/)
            {

                if (InputProxy.Panning && InputProxy.CursorPrimaryButton)
                {

                    if (InputProxy.CursorLockState != CursorLockMode.Locked)
                    {

                        cursorLockPosition = InputProxy.CursorPosition;

                        prevCursorState = InputProxy.CursorLockState;

                        InputProxy.CursorLockState = CursorLockMode.Locked;

                        cursorVisible = InputProxy.IsCursorVisible;

                        InputProxy.IsCursorVisible = false;

                        lockedCursor = true;

                    }

                    if (scrollbarHor != null)
                    {

                        scrollbarHor.value = Mathf.Clamp01(scrollbarHor.value + InputProxy.CursorAxisX * panningSpeedMultiplier * InputProxy.PanningSpeed * (invertHor ? 1 : -1));

                    }

                    if (scrollbarVer != null)
                    {

                        scrollbarVer.value = Mathf.Clamp01(scrollbarVer.value + InputProxy.CursorAxisY * panningSpeedMultiplier * InputProxy.PanningSpeed * (invertVer ? 1 : -1));

                    }

                }

                if (scrollbarVer != null && scrollbarHor != null)
                {

                    if (InputProxy.ItemCombineKey)
                    {

                        scrollbarHor.value = Mathf.Clamp01(scrollbarHor.value + InputProxy.Scroll * scollSpeedMultiplier * InputProxy.ScrollSpeed * (invertHor ? 1 : -1));

                    }
                    else
                    {

                        scrollbarVer.value = Mathf.Clamp01(scrollbarVer.value + InputProxy.Scroll * scollSpeedMultiplier * InputProxy.ScrollSpeed * (invertVer ? -1 : 1));

                    }

                }
                else if (scrollbarVer != null)
                {

                    scrollbarVer.value = Mathf.Clamp01(scrollbarVer.value + InputProxy.Scroll * scollSpeedMultiplier * InputProxy.ScrollSpeed * (invertVer ? -1 : 1));

                }
                else if (scrollbarHor != null)
                {

                    scrollbarHor.value = Mathf.Clamp01(scrollbarHor.value + InputProxy.Scroll * scollSpeedMultiplier * InputProxy.ScrollSpeed * (invertHor ? 1 : -1));

                }

            }

            RectTransform parent = rectTransform.parent.GetComponent<RectTransform>();

            rectTransform.GetWorldCorners(localCorners);
            content.GetWorldCorners(contentCorners);

            for (int a = 0; a < 4; a++)
            {

                localCorners[a] = parent.InverseTransformPoint(localCorners[a]);

                contentCorners[a] = parent.InverseTransformPoint(contentCorners[a]);

            }

            Vector2 localSize = rectTransform.rect.size;
            Vector2 contentSize = content.rect.size;

            float paddingX = padding.left + padding.right;
            float paddingY = padding.top + padding.bottom;

            contentSize.x = contentSize.x + paddingX;
            contentSize.y = contentSize.y + paddingY;

            /*
            if (constrainSizeToContentSize)
            {

                bool updated = false;

                if (localSize.x > contentSize.x)
                {

                    rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, contentSize.x);

                    rectTransform.localPosition = rectTransform.localPosition + new Vector3((contentSize.x - localSize.x) * (rectTransform.pivot.x - 0.5f), 0, 0);

                    updated = true;

                }
                if (localSize.y > contentSize.y)
                {

                    rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentSize.y);

                    rectTransform.localPosition = rectTransform.localPosition + new Vector3(0, (contentSize.y - localSize.y) * (rectTransform.pivot.y - 0.5f), 0);

                    updated = true;

                }

                if (updated)
                {

                    localSize = rectTransform.rect.size;

                    rectTransform.GetWorldCorners(localCorners);

                    for (int a = 0; a < 4; a++)
                    {

                        localCorners[a] = parent.InverseTransformPoint(localCorners[a]);

                    }

                }

            }
            */

            if (scrollbarHor != null)
            {

                float sizeX = localSize.x == 0 ? 1 : (contentSize.x / localSize.x); // Prevent divide by zero

                scrollbarHor.size = 1 / sizeX;

                scrollbarHor.gameObject.SetActive(scrollbarHor.size < 1);
            }

            if (scrollbarVer != null)
            {

                float sizeY = localSize.x == 0 ? 1 : (contentSize.y / localSize.y); // Prevent divide by zero

                scrollbarVer.size = 1 / sizeY;

                scrollbarVer.gameObject.SetActive(scrollbarVer.size < 1);

            }

            float flexibleWidth = Mathf.Max(0, contentSize.x - localSize.x);
            float flexibleHeight = Mathf.Max(0, contentSize.y - localSize.y);

            float emptyWidth = Mathf.Max(0, localSize.x - contentSize.x);
            float emptyHeight = Mathf.Max(0, localSize.y - contentSize.y);

            Vector3 current = (contentCorners[0] + contentCorners[1] + contentCorners[2] + contentCorners[3]) / 4f;

            float offsetX = contentSize.x * 0.5f;
            float offsetY = contentSize.y * 0.5f;

            float addEmptyWidth = emptyWidth * (localSize.x > contentSize.x ? contentCenterHor : 0);
            float addEmptyHeight = emptyHeight * (localSize.y > contentSize.y ? contentCenterVer : 0);

            Vector3 target = new Vector3(localCorners[0].x + addEmptyWidth + offsetX, localCorners[0].y + addEmptyHeight + offsetY, 0) - new Vector3((flexibleWidth * (scrollbarHor == null ? 0 : (invertHor ? (1 - scrollbarHor.value) : scrollbarHor.value))) - padding.left, (flexibleHeight * (scrollbarVer == null ? 0 : (invertVer ? (1 - scrollbarVer.value) : scrollbarVer.value))) - padding.bottom, 0);

            content.localPosition = content.localPosition + (target - current);

        }

    }

}

#endif