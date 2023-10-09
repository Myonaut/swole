#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole.UI
{

    /// <summary>
    /// UIElement that auto-resizes itself to encapsulate its child elements.
    /// </summary>
    public class UIEncapsulator : UIElement
    {

        /// <summary>
        /// source: https://stackoverflow.com/questions/54561610/unity-fit-size-of-parent-to-size-of-childs
        /// </summary>
        /// <param name="children"></param>
        /// <returns></returns>
        public static Bounds Encapsulate(List<RectTransform> children, Matrix4x4 toLocal)
        {

            Bounds bound = new Bounds();
            bool first = true;
            for (int i = 0; i < children.Count; ++i)
            {
                RectTransform child = children[i];
                Encapsulate(child, toLocal, ref bound, ref first);
            }

            return bound;

        }

        public static Bounds Encapsulate(RectTransform[] children, Matrix4x4 toLocal)
        {

            Bounds bound = new Bounds();
            bool first = true;
            for (int i = 0; i < children.Length; ++i)
            {
                RectTransform child = children[i];
                Encapsulate(child, toLocal, ref bound, ref first);
            }

            return bound;

        }

        public static void Encapsulate(RectTransform child, Matrix4x4 toLocal, ref Bounds bound, ref bool first)
        {

            if (child != null && child.gameObject.activeSelf)
            {
                Vector2 childSize = child.rect.size;
                Vector2 pos = toLocal.MultiplyPoint(child.position);
                //Vector2 min = pos - child.sizeDelta * child.pivot;
                //Vector2 max = min + child.sizeDelta;
                Vector2 min = pos - childSize * child.pivot;
                Vector2 max = min + childSize;
                Bounds temp = new Bounds();
                temp.SetMinMax(min, max);
                if (first)
                {
                    bound = temp;
                    first = false;
                }
                else
                {
                    bound.Encapsulate(temp);
                }

            }

        }

        public bool updateEveryFrame;

        public bool onlyResetTopLevelChildren = true;

        public int minChildDepth;

        public int maxChildDepth;

        public RectTransform targetRectTransform;

        public RectOffset padding;

        protected List<RectTransform> children = new List<RectTransform>();

        protected List<int> topLevelChildren = new List<int>();

        protected Rect[] childRects;
        protected Vector3[] childPositions;
        protected int[] childCounts;
        protected Transform[] childParents;

        protected int prevChildCount;

        public void FetchChildren()
        {

            children.Clear();

            topLevelChildren.Clear();

            if (targetRectTransform == null) return;

            children.AddRange(targetRectTransform.gameObject.GetComponentsInChildren<RectTransform>());

            children.RemoveAll(i => i == null || i == RectTransform || i == targetRectTransform || (Mathf.Max(minChildDepth, maxChildDepth) > 0 ? (i.GetChildDepth(targetRectTransform) > (Mathf.Max(minChildDepth, maxChildDepth) - 1) || i.GetChildDepth(targetRectTransform) < (minChildDepth - 1)) : false));

            childRects = new Rect[children.Count];
            childPositions = new Vector3[children.Count];
            childCounts = new int[children.Count];
            childParents = new Transform[children.Count];

            for (int a = 0; a < children.Count; a++)
            {

                if (children[a].parent == targetRectTransform.transform) topLevelChildren.Add(a);

                childCounts[a] = children[a].childCount;
                childParents[a] = children[a].parent;
                //Debug.Log(a + ": " + children[a].name + " : " + ((children[a].GetChildDepth(targetRectTransform) > (maxChildDepth - 1))));
            }

            prevChildCount = targetRectTransform.childCount;

        }

        public override void AwakeLocal()
        {

            if (padding == null) padding = new RectOffset();

            rectTransform = gameObject.AddOrGetComponent<RectTransform>();

            if (targetRectTransform == null) targetRectTransform = rectTransform;

            FetchChildren();

        }

        public void Recalculate()
        {

            if (targetRectTransform.childCount != prevChildCount)
            {

                FetchChildren();

            }
            else
            {

                for (int a = 0; a < children.Count; a++)
                {

                    RectTransform child = children[a];

                    if (child == null || child.childCount != childCounts[a] || child.parent != childParents[a])
                    {

                        FetchChildren();

                        break;

                    }

                }

            }

            bool isTarget = rectTransform == targetRectTransform;

            if (isTarget)
            {

                if (onlyResetTopLevelChildren)
                {

                    for (int a = 0; a < topLevelChildren.Count; a++)
                    {

                        int i = topLevelChildren[a];

                        RectTransform child = children[i];

                        if (child == null)
                        {

                            FetchChildren();

                            return;

                        }

                        childRects[i] = child.rect;
                        childPositions[i] = child.position;

                    }

                }
                else
                {

                    for (int i = 0; i < children.Count; i++)
                    {

                        RectTransform child = children[i];

                        if (child == null)
                        {

                            FetchChildren();

                            return;

                        }

                        childRects[i] = child.rect;
                        childPositions[i] = child.position;

                    }

                }

            }

            Bounds bounds = Encapsulate(children, targetRectTransform.worldToLocalMatrix);

            float minX = Mathf.Min(bounds.min.x, bounds.max.x);
            float maxX = Mathf.Max(bounds.min.x, bounds.max.x);

            float minY = Mathf.Min(bounds.min.y, bounds.max.y);
            float maxY = Mathf.Max(bounds.min.y, bounds.max.y);

            minX = minX + padding.left;
            maxX = maxX - padding.right;

            minY = minY + padding.bottom;
            maxY = maxY - padding.top;

            float width = maxX - minX;
            float height = maxY - minY;

            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);

            rectTransform.position = targetRectTransform.TransformPoint(new Vector3(minX + (maxX - minX) * rectTransform.pivot.x, minY + (maxY - minY) * rectTransform.pivot.y));

            if (isTarget)
            {

                if (onlyResetTopLevelChildren)
                {

                    for (int a = 0; a < topLevelChildren.Count; a++)
                    {

                        int i = topLevelChildren[a];

                        RectTransform child = children[i];

                        Rect childRect = childRects[i];

                        child.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, childRect.size.x);
                        child.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, childRect.size.y);

                        child.position = childPositions[i];

                    }

                }
                else
                {

                    for (int i = 0; i < children.Count; i++)
                    {

                        RectTransform child = children[i];

                        Rect childRect = childRects[i];

                        child.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, childRect.size.x);
                        child.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, childRect.size.y);

                        child.position = childPositions[i];

                    }

                }

            }

        }

        public override void LateUpdateLocal()
        {

            if (updateEveryFrame) Recalculate();

        }

    }

}

#endif
