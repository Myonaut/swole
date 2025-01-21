#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using TMPro;

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
        public static Bounds Encapsulate(List<RectTransform> children, Matrix4x4 toLocal, List<Text> childTexts = null, List<TMP_Text> childTextsTMP = null, List<Transform> toIgnore = null)
        {

            Bounds bound = new Bounds();
            bool first = true;
            for (int i = 0; i < children.Count; ++i)
            {
                RectTransform child = children[i];
                if (child == null || !child.gameObject.activeInHierarchy) continue;
                if (toIgnore != null)
                {
                    bool skip = false;
                    foreach (var ignore in toIgnore) if (ignore != null && child.IsChildOf(ignore)) 
                        {
                            skip = true;
                            break;
                        }
                    if (skip) continue;
                }
                bool replaceBounds = false;
                Bounds replacementBounds = default;
                if (childTexts != null)
                {
                    var text = childTexts[i];
                    if (text != null && !string.IsNullOrEmpty(text.text))
                    {
                        replaceBounds = true;
                        Vector2 pos = toLocal.MultiplyPoint(child.position);
                        Vector2 childSize = new Vector2(text.preferredWidth, text.preferredHeight);
                        Vector2 min = pos - childSize * child.pivot;
                        Vector2 max = min + childSize;
                        replacementBounds = new Bounds();
                        replacementBounds.SetMinMax(min, max);
                    }
                }
                if (childTextsTMP != null)
                {
                    var text = childTextsTMP[i]; 
                    if (text != null && !string.IsNullOrEmpty(text.text))
                    {
                        text.ForceMeshUpdate();
                        replaceBounds = true;
                        replacementBounds = text.textBounds;
                        //Vector2 min = toLocal.MultiplyPoint(replacementBounds.min);
                        //Vector2 max = toLocal.MultiplyPoint(replacementBounds.max);
                        Matrix4x4 toWorld = child.localToWorldMatrix;
                        Vector2 min = toWorld.MultiplyPoint(replacementBounds.min);
                        Vector2 max = toWorld.MultiplyPoint(replacementBounds.max);
                        min = toLocal.MultiplyPoint(min);
                        max = toLocal.MultiplyPoint(max);
                        replacementBounds = new Bounds();
                        replacementBounds.SetMinMax(min, max);
                    }
                }
                Encapsulate(child, toLocal, ref bound, ref first, replaceBounds, replacementBounds);
            }

            return bound;

        }

        public static Bounds Encapsulate(RectTransform[] children, Matrix4x4 toLocal, Text[] childTexts = null, TMP_Text[] childTextsTMP = null, Transform[] toIgnore = null)
        {

            Bounds bound = new Bounds();
            bool first = true;
            for (int i = 0; i < children.Length; ++i)
            {
                RectTransform child = children[i];
                if (child == null || !child.gameObject.activeInHierarchy) continue;
                if (toIgnore != null)
                {
                    bool skip = false;
                    foreach (var ignore in toIgnore) if (ignore != null && child.IsChildOf(ignore))
                        {
                            skip = true;
                            break;
                        }
                    if (skip) continue;
                }
                bool replaceBounds = false;
                Bounds replacementBounds = default;
                if (childTexts != null)
                {
                    var text = childTexts[i];
                    if (text != null)
                    {
                        replaceBounds = true;
                        Vector2 pos = toLocal.MultiplyPoint(child.position);
                        Vector2 childSize = new Vector2(text.preferredWidth, text.preferredHeight);
                        Vector2 min = pos - childSize * child.pivot;
                        Vector2 max = min + childSize;
                        replacementBounds = new Bounds();
                        replacementBounds.SetMinMax(min, max);
                    }
                }
                if (childTextsTMP != null)
                {
                    var text = childTextsTMP[i];
                    if (text != null)
                    {
                        replaceBounds = true;
                        replacementBounds = text.bounds;
                    }
                }
                Encapsulate(child, toLocal, ref bound, ref first, replaceBounds, replacementBounds);
            }

            return bound;

        }

        public static void Encapsulate(RectTransform child, Matrix4x4 toLocal, ref Bounds bound, ref bool first, bool replaceBounds = false, Bounds replacementBounds = default)
        {

            if (child != null && child.gameObject.activeSelf)
            {
                Bounds tempBounds;
                if (replaceBounds)
                {
                    tempBounds = replacementBounds;
                } 
                else
                {
                    Vector2 pos = toLocal.MultiplyPoint(child.position);
                    //Vector2 childSize = child.sizeDelta;
                    Vector2 childSize = child.rect.size;
                    Vector2 min = pos - childSize * child.pivot;
                    Vector2 max = min + childSize;
                    tempBounds = new Bounds();
                    tempBounds.SetMinMax(min, max);
                }
                if (first)
                {
                    bound = tempBounds;
                    first = false;
                }
                else
                {
                    bound.Encapsulate(tempBounds);
                }

            }

        }

        public bool updateEveryFrame;

        [Tooltip("After the encapsulator resizes, should only the top layer of children be put back to their previous positions? If the encapsulator is floating away then unchecking this may help.")]
        public bool onlyResetTopLevelChildren = true;
         
        public bool includeTextComponents = false;

        public int minChildDepth;

        public int maxChildDepth;

        public RectTransform targetRectTransform;

        public RectOffset padding;

        public List<Transform> toIgnore = new List<Transform>(); 

        protected List<RectTransform> children = new List<RectTransform>();
        protected List<Text> childTexts;
        protected List<TMP_Text> childTextsTMP;

        protected List<int> topLevelChildren = new List<int>();

        protected List<Rect> childRects = new List<Rect>();
        protected List<Vector3> childPositions = new List<Vector3>();
        protected List<int> childCounts = new List<int>();
        protected List<Transform> childParents = new List<Transform>();

        protected int prevChildCount;

        public void FetchChildren()
        {

            children.Clear();
            if (includeTextComponents)
            {
                if (childTexts == null) childTexts = new List<Text>();
                if (childTextsTMP == null) childTextsTMP = new List<TMP_Text>();
                childTexts.Clear();
                childTextsTMP.Clear();
            }

            topLevelChildren.Clear();

            childRects.Clear();
            childPositions.Clear();
            childCounts.Clear();
            childParents.Clear();

            if (targetRectTransform == null) return;

            targetRectTransform.gameObject.GetComponentsInChildren<RectTransform>(true, children); 

            children.RemoveAll(i => i == null || i == RectTransform || i == targetRectTransform || (Mathf.Max(minChildDepth, maxChildDepth) > 0 ? (i.GetChildDepth(targetRectTransform) > (Mathf.Max(minChildDepth, maxChildDepth) - 1) || i.GetChildDepth(targetRectTransform) < (minChildDepth - 1)) : false));

            for (int a = 0; a < children.Count; a++)
            {

                if (children[a].parent == targetRectTransform.transform) topLevelChildren.Add(a);

                childRects.Add(default);
                childPositions.Add(default);
                childCounts.Add(children[a].childCount);
                childParents.Add(children[a].parent);
                if (includeTextComponents)
                {
                    childTexts.Add(children[a].GetComponent<Text>());
                    childTextsTMP.Add(children[a].GetComponent<TMP_Text>());
                } 
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

        protected bool ShouldIgnoreTransform(Transform t, bool includeRootOfIgnore = true)
        {
            if (toIgnore != null)
            {
                foreach (var ignore in toIgnore) if (ignore != null && ((includeRootOfIgnore && ignore == t) || t.IsChildOf(ignore))) return true;
            }
            return false;
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

                        if (ShouldIgnoreTransform(child)) continue;
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

                        if (ShouldIgnoreTransform(child)) continue;
                        childRects[i] = child.rect;
                        childPositions[i] = child.position;

                    }

                }

            }

            Bounds bounds = Encapsulate(children, targetRectTransform.worldToLocalMatrix, childTexts, childTextsTMP, toIgnore);

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
                        if (ShouldIgnoreTransform(child)) continue;

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
                        if (ShouldIgnoreTransform(child)) continue;

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
