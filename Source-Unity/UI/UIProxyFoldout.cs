#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;
using UnityEngine.Events;

namespace Swole.UI
{

    public class UIProxyFoldout : UITweenable
    {

        public bool horizontal;

        public bool disableMaskOnFoldOut;

        public PivotPresets pivotPoint;

        public float foldedSize;

        protected float fullSize;

        public bool dynamicSize;

        public bool moveOnResize;

        public int proxyEncapsulatorMinChildDepth = 0;

        public int proxyEncapsulatorChildDepth = 1;

        public RectOffset proxyEncapsulatorPadding;

        public UIEncapsulator proxyEncapsulator;

        public bool showProxyGraphic;

        protected RectTransform proxy;

        protected Image proxyImage;

        protected Mask proxyMask;

        public RectTransform Proxy => proxy;

        protected Image imageLocal;

        protected RectTransform rectTransform;

        public RectTransform RectTransform
        {

            get
            {

                if (rectTransform == null) rectTransform = gameObject.AddOrGetComponent<RectTransform>();

                return rectTransform;

            }

        }

        protected bool state;

        protected bool tweening;

        protected Vector3 pivotPosition;

        protected virtual void Awake()
        {

            imageLocal = gameObject.GetComponent<Image>();

            if (proxy == null)
            {

                proxyImage = new GameObject(gameObject.name + "_foldout").AddOrGetComponent<Image>();

                if (imageLocal != null) proxyImage.sprite = imageLocal.sprite;

                proxy = proxyImage.gameObject.AddOrGetComponent<RectTransform>();

                proxyMask = proxyImage.gameObject.AddOrGetComponent<Mask>();

                proxyMask.showMaskGraphic = showProxyGraphic;

                if (showProxyGraphic)
                {

                    imageLocal.enabled = false;

                    proxyImage.raycastTarget = imageLocal.raycastTarget;

                }

                Vector3 rectSize = RectTransform.rect.size;

                fullSize = horizontal ? rectSize.x : rectSize.y;

                Vector3 anchoredPos = rectTransform.anchoredPosition3D;

                Vector3 worldPos = rectTransform.position;

                int siblingIndex = rectTransform.GetSiblingIndex();

                proxy.SetParent(rectTransform.parent, false);

                rectTransform.SetParent(proxy);

                proxy.SetSiblingIndex(siblingIndex);

                proxy.anchorMin = rectTransform.anchorMin;
                proxy.anchorMax = rectTransform.anchorMax;

                proxy.SetPivot(pivotPoint);

                Vector2 pivot = proxy.pivot;

                if (horizontal)
                {

                    pivot.y = rectTransform.pivot.y;

                }
                else
                {

                    pivot.x = rectTransform.pivot.x;

                }

                proxy.pivot = pivot;

                Vector2 anchorMin = new Vector2(horizontal ? (1 - pivot.x) : 0, horizontal ? 0 : (1 - pivot.y));
                Vector2 anchorMax = new Vector2(horizontal ? (1 - pivot.x) : 1, horizontal ? 1 : (1 - pivot.y));

                rectTransform.anchorMin = anchorMin;
                rectTransform.anchorMax = anchorMax;

                proxy.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, rectSize.x);
                proxy.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rectSize.y);

                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, rectSize.x);
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rectSize.y);

                proxy.anchoredPosition3D = anchoredPos + new Vector3((pivot.x - rectTransform.pivot.x) * rectSize.x, (pivot.y - rectTransform.pivot.y) * rectSize.y);

                rectTransform.position = worldPos;

                pivotPosition = proxy.parent.InverseTransformPoint(worldPos) + new Vector3(proxy.pivot.x * rectSize.x, proxy.pivot.y * rectSize.y, 0);

                state = true;

                LayoutGroup[] groups = proxy.gameObject.GetComponentsInChildren<LayoutGroup>();

                for (int a = 0; a < groups.Length; a++)
                {

                    LayoutGroup group = groups[a];

                    group.CalculateLayoutInputHorizontal();
                    group.CalculateLayoutInputVertical();
                    group.SetLayoutHorizontal();
                    group.SetLayoutVertical();

                }

                UpdateDynamicSize();

                state = false;

            }

            Refresh();

        }

        public float defaultFoldOutTime = 0.2f;

        public float defaultFoldInTime = 0.2f;

        public bool easeIn;

        public bool easeOut = true;

        public UnityEvent OnToggle;

        public UnityEvent OnFoldBegin;

        public UnityEvent OnFoldEnd;

        public UnityEvent OnFoldOutBegin;

        public UnityEvent OnFoldOutEnd;

        public void Refresh()
        {

            if (state) FoldOut(0); else Fold(0);

        }

        protected void OnDisable()
        {

            state = false;

            Refresh();

        }

        public void Toggle(float tweenTime, bool easeIn = false, bool easeOut = false)
        {

            OnToggle?.Invoke();

            if (state) Fold(tweenTime, easeIn, easeOut); else FoldOut(tweenTime, easeIn, easeOut);

        }

        protected List<GameObject> recentlyDisabled = new List<GameObject>();

        public void Fold(float tweenTime, bool easeIn = false, bool easeOut = false)
        {

            state = false;

            void OnComplete()
            {

                proxy.SetSizeWithCurrentAnchors(horizontal ? RectTransform.Axis.Horizontal : RectTransform.Axis.Vertical, foldedSize);

                if (foldedSize <= 0)
                {

                    foreach (Transform child in rectTransform)
                    {

                        if (child.gameObject.activeSelf)
                        {

                            child.gameObject.SetActive(false);

                            recentlyDisabled.Add(child.gameObject);

                        }

                    }

                }

                tweening = false;

                OnFoldEnd?.Invoke();

            }

            void OnStart()
            {

                UITabButton[] buttons = proxy.GetComponentsInChildren<UITabButton>();

                for (int a = 0; a < buttons.Length; a++)
                {

                    if (buttons[a].isHovering) buttons[a].OnPointerExit(null);

                    buttons[a].ToggleOff();

                }

                if (disableMaskOnFoldOut)
                {

                    proxyMask.enabled = true;

                    if (!proxyMask.showMaskGraphic) proxyImage.enabled = true;

                }

                tweening = true;

                OnFoldBegin?.Invoke();

            }

            if (tweenTime <= 0)
            {

                AppendTween(LeanTween.delayedCall(gameObject, 0.001f, OnComplete), null, OnStart);

            }
            else
            {

                float currentSize = horizontal ? proxy.rect.size.x : proxy.rect.size.y;

                void FoldProxy(float t)
                {

                    proxy.SetSizeWithCurrentAnchors(horizontal ? RectTransform.Axis.Horizontal : RectTransform.Axis.Vertical, Mathf.LerpUnclamped(currentSize, foldedSize, t));

                }

                LTDescr newTween = LeanTween.value(gameObject, 0, 1, tweenTime).setOnUpdate(FoldProxy);

                if (easeIn) newTween.setEaseInExpo();

                if (easeOut) newTween.setEaseOutExpo();

                AppendTween(newTween, OnComplete, OnStart);

            }

        }

        public void FoldOut(float tweenTime, bool easeIn = false, bool easeOut = false)
        {

            state = true;

            void OnComplete()
            {

                if (disableMaskOnFoldOut)
                {

                    proxyMask.enabled = false;

                    if (!proxyMask.showMaskGraphic) proxyImage.enabled = false;

                }

                proxy.SetSizeWithCurrentAnchors(horizontal ? RectTransform.Axis.Horizontal : RectTransform.Axis.Vertical, fullSize);

                tweening = false;

                OnFoldOutEnd?.Invoke();

            }

            void OnStart()
            {

                if (foldedSize <= 0)
                {

                    //foreach (Transform child in rectTransform) child.gameObject.SetActive(true);
                    foreach (GameObject disabled in recentlyDisabled) disabled.SetActive(true);

                    recentlyDisabled.Clear();

                    tweening = true;

                    OnFoldOutBegin?.Invoke();

                }

            }

            if (tweenTime <= 0)
            {

                AppendTween(LeanTween.delayedCall(gameObject, 0, OnComplete), null, OnStart);

            }
            else
            {

                float currentSize = horizontal ? proxy.rect.size.x : proxy.rect.size.y;

                void FoldProxy(float t)
                {

                    proxy.SetSizeWithCurrentAnchors(horizontal ? RectTransform.Axis.Horizontal : RectTransform.Axis.Vertical, Mathf.LerpUnclamped(currentSize, fullSize, t));

                }

                LTDescr newTween = LeanTween.value(gameObject, 0, 1, tweenTime).setOnUpdate(FoldProxy);

                if (easeIn) newTween.setEaseInExpo();

                if (easeOut) newTween.setEaseOutExpo();

                AppendTween(newTween, OnComplete, OnStart);

            }

        }

        public void Toggle()
        {

            Toggle(state ? defaultFoldInTime : defaultFoldOutTime, easeIn, easeOut);

        }

        public void Fold()
        {

            if (state) Fold(defaultFoldInTime, easeIn, easeOut);

        }

        public void FoldOut()
        {

            if (!state) FoldOut(defaultFoldOutTime, easeIn, easeOut);

        }

        public void ToggleInstant()
        {

            Toggle(0);

        }

        public void FoldInstant()
        {

            if (state) Fold(0);

        }

        public void FoldOutInstant()
        {

            if (!state) FoldOut(0);

        }

        public bool allowViewers = true;

        protected HashSet<PointerEventsProxy> viewers;

        public void StartViewing(PointerEventsProxy proxy)
        {

            if (viewers == null) viewers = new HashSet<PointerEventsProxy>();

            viewers.Add(proxy);

        }

        public void StopViewing(PointerEventsProxy proxy)
        {

            if (viewers == null) return;

            viewers.Remove(proxy);

        }

        protected void UpdateDynamicSize()
        {

            if (state && !tweening && dynamicSize)
            {

                Vector3 rectSize;

                if (proxyEncapsulator == null)
                {

                    proxyEncapsulator = proxy.gameObject.AddOrGetComponent<UIEncapsulator>();

                    proxyEncapsulator.targetRectTransform = proxyEncapsulator.gameObject.AddOrGetComponent<RectTransform>();

                    if (proxyEncapsulatorPadding != null) proxyEncapsulator.padding = proxyEncapsulatorPadding;

                    proxyEncapsulator.updateEveryFrame = false;
                    proxyEncapsulator.minChildDepth = proxyEncapsulatorMinChildDepth;
                    proxyEncapsulator.maxChildDepth = proxyEncapsulatorChildDepth;

                    proxyEncapsulator.FetchChildren();

                    //rectSize = rectTransform.rect.size;

                    //rectTransform.anchorMin = rectTransform.anchorMax = (rectTransform.anchorMin + rectTransform.anchorMax) / 2f;

                    //rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, rectSize.x);
                    //rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rectSize.y);

                }

                proxyEncapsulator.Recalculate();

                rectSize = proxy.rect.size;

                float newSize = horizontal ? rectSize.x : rectSize.y;

                if (moveOnResize)
                {

                    //Vector3 offset = new Vector3((horizontal ? (((rectTransform.pivot.x) - 2) * 0.5f) : 0) * (newSize - fullSize), (horizontal ? 0 : (((rectTransform.pivot.y) - 2) * 0.5f)) * (fullSize - newSize), 0);

                    //proxy.localPosition = proxy.localPosition + offset;

                    Vector3 pivotPos = proxy.localPosition + new Vector3(proxy.pivot.x * rectSize.x, proxy.pivot.y * rectSize.y, 0);

                    proxy.localPosition = proxy.localPosition + (pivotPosition - pivotPos);

                }

                fullSize = newSize;

            }

        }

        protected void Update()
        {

            UpdateDynamicSize();

            if (allowViewers && viewers != null)
            {

                viewers.RemoveWhere(i => i == null);

                bool flag = true;

                while (flag)
                {

                    flag = false;

                    PointerEventsProxy toRemove = null;

                    foreach (PointerEventsProxy viewer in viewers)
                    {

                        if (!viewer.IsHovering)
                        {

                            toRemove = viewer;

                            break;

                        }

                    }

                    if (toRemove != null)
                    {

                        flag = true;

                        viewers.Remove(toRemove);

                    }

                }

                if (state)
                {

                    if (viewers.Count <= 0) Fold();

                }
                else
                {

                    if (viewers.Count > 0) FoldOut();

                }

            }

        }

    }

}

#endif
