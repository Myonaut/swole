#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace Swole.UI
{

    /// <summary>
    /// A UI element that creates a parent container element (the proxy) for itself, and uses it to show/hide (fold/unfold) the elements within it.
    /// </summary>
    public class UIProxyFoldout : UITweenable
    {

        [Header("Folding Options"), Tooltip("If the foldout is performed on the horizontal axis, check this box. Leave unchecked for vertical foldouts.")]
        public bool horizontal;

        [Tooltip("Should the element be unfolded on start?")]
        public bool startUnfolded;

        [Tooltip("The point at which the element will be anchored to, and fold out from.")]
        public PivotPresets pivotPoint;

        [Tooltip("The size of the element when folded. A size of zero will completely hide the element when folded.")]
        public float foldedSize;

        /// <summary>
        /// Calculated on initialize. If dynamicSize is set to true, it is calculated each frame when the element is unfolded.
        /// </summary>
        protected float fullSize;

        [Tooltip("Should the proxy mask be disabled when the element is on full display?")]
        public bool disableMaskOnFoldOut;

        [Tooltip("Should the proxy mask image be shown?")]
        public bool showProxyGraphic;

        [Tooltip("Should the element fold when the mouse is clicked? Will wait a frame to register mouse events.")]
        public bool autoFoldOnClickOff;

        public MouseButtonMask autoFoldMouseButtonMask = MouseButtonMask.LeftMouseButton;

        [Tooltip("Should the proxy object's parent be considered as part of the clickable hierarchy?")]
        public bool includeProxyParentAsAutoFoldPreventionObject = true;

        public float defaultFoldOutTime = 0.2f;

        public float defaultFoldInTime = 0.2f;

        public bool easeIn;

        public bool easeOut = true;

        [Header("Dynamic Size Options"), Tooltip("Can the unfolded size change based on the size of child elements? Useful for say - a list that has a dynamic number of options. If unchecked, unfolded size is only calculated once on Awake.")]
        public bool dynamicSize;

        [Tooltip("Don't use a UIEncapsulator object for calculating dynamic size?")]
        public bool noEncapsulatorForDynamicSize;

        [Tooltip("If using dynamicSize, should the root transform move to account for size changes?")]
        public bool moveOnResize;

        [Tooltip("If using dynamicSize, should the container transform stay at its current world position during size changes?")]
        public bool pinContainerOnResize = true;

        [Tooltip("The minimum transform depth to calculate unfolded size from. Child elements with a smaller depth than this will be ignored.")]
        public int proxyEncapsulatorMinChildDepth = 0;

        [Tooltip("The maximum transform depth to calculate unfolded size from. Child elements with a greater depth than this will be ignored.")]
        public int proxyEncapsulatorChildDepth = 1;

        public RectOffset proxyEncapsulatorPadding;

        [Tooltip("The encapsulator to use. Normally should be left blank, in which case a new one will be created. Only used when dynamicSize is set to true.")]
        public UIEncapsulator proxyEncapsulator;

        protected RectTransform proxy;

        protected Image proxyImage;

        protected Mask proxyMask;

        public RectTransform Proxy => proxy;
        public bool IsInitialized => proxy != null;

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

        /// <summary>
        /// Is the element currently on full display or not.
        /// </summary>
        protected bool state;

        /// <summary>
        /// Is the element in the process of changing states with a tween.
        /// </summary>
        protected bool tweening;

        protected Vector3 pivotPosition;

        protected virtual void Start()
        {

            Initialize();

        }

        protected virtual void Initialize()
        {

            if (!gameObject.activeSelf) gameObject.SetActive(true);
            if (!gameObject.activeInHierarchy) return;

            LayoutGroup[] layoutGroups = gameObject.GetComponentsInChildren<LayoutGroup>();
            foreach(var layout in layoutGroups)
            {
                layout.CalculateLayoutInputHorizontal();
                layout.CalculateLayoutInputVertical();
                layout.SetLayoutHorizontal();
                layout.SetLayoutVertical();
                var rt = layout.GetComponent<RectTransform>();
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            }

            imageLocal = gameObject.GetComponent<Image>();

            if (!IsInitialized)
            {

                proxyImage = new GameObject(gameObject.name + "_foldoutProxy").AddOrGetComponent<Image>();

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

                state = startUnfolded;

            }

            Refresh();

        }

        [Header("Events")]
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

            if (!IsInitialized)
            {
                Initialize();
                if (!IsInitialized) return;
            }

            OnToggle?.Invoke();

            if (state) Fold(tweenTime, easeIn, easeOut); else FoldOut(tweenTime, easeIn, easeOut);

        }

        protected List<GameObject> recentlyDisabled = new List<GameObject>();

        public void Fold(float tweenTime, bool easeIn = false, bool easeOut = false)
        {

            if (!IsInitialized)
            {
                Initialize();
                if (!IsInitialized) return;
            }

            state = false;
            if (!enabled) return;

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
#if SWOLE_ENV
                AppendTween(LeanTween.delayedCall(gameObject, 0.001f, OnComplete), null, OnStart);
#endif

            }
            else
            {
#if SWOLE_ENV
                float currentSize = horizontal ? proxy.rect.size.x : proxy.rect.size.y;

                void FoldProxy(float t)
                {

                    proxy.SetSizeWithCurrentAnchors(horizontal ? RectTransform.Axis.Horizontal : RectTransform.Axis.Vertical, Mathf.LerpUnclamped(currentSize, foldedSize, t));

                }

                LTDescr newTween = LeanTween.value(gameObject, 0, 1, tweenTime).setOnUpdate(FoldProxy);

                if (easeIn) newTween.setEaseInExpo();

                if (easeOut) newTween.setEaseOutExpo();

                AppendTween(newTween, OnComplete, OnStart);
#endif
            }

        }

        public void FoldOut(float tweenTime, bool easeIn = false, bool easeOut = false)
        {

            if (!IsInitialized)
            {
                Initialize();
                if (!IsInitialized) return;
            }

            state = true;
            if (!enabled) return;

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
#if SWOLE_ENV
                AppendTween(LeanTween.delayedCall(gameObject, 0, OnComplete), null, OnStart);
#endif

            }
            else
            {
#if SWOLE_ENV
                float currentSize = horizontal ? proxy.rect.size.x : proxy.rect.size.y;

                void FoldProxy(float t)
                {

                    proxy.SetSizeWithCurrentAnchors(horizontal ? RectTransform.Axis.Horizontal : RectTransform.Axis.Vertical, Mathf.LerpUnclamped(currentSize, fullSize, t));

                }

                LTDescr newTween = LeanTween.value(gameObject, 0, 1, tweenTime).setOnUpdate(FoldProxy);

                if (easeIn) newTween.setEaseInExpo();

                if (easeOut) newTween.setEaseOutExpo();

                AppendTween(newTween, OnComplete, OnStart);
#endif
            }

        }

        public void Toggle()
        {

            if (!IsInitialized)
            {
                Initialize();
                if (!IsInitialized) return;
            }

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

        [Header("Other"), Tooltip("Whether or not to use viewers. Viewers automatically fold/unfold the element. If a viewer currently has cursor focus, it will unfold the element. If no viewers have cursor focus, the element will fold automatically.")]
        public bool allowViewers = true;

        protected HashSet<PointerEventsProxy> viewers;

        public void StartViewing(PointerEventsProxy viewer)
        {

            if (viewers == null) viewers = new HashSet<PointerEventsProxy>();

            viewers.Add(viewer);

        }

        public void StopViewing(PointerEventsProxy viewer)
        {

            if (viewers == null) return;

            viewers.Remove(viewer);

        }

        protected void UpdateDynamicSize()
        {

            if (IsInitialized && state && !tweening && dynamicSize)
            {

                Vector3 rectSize;

                if (proxyEncapsulator == null && !noEncapsulatorForDynamicSize)
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

                proxyEncapsulator?.Recalculate();

                rectSize = proxyEncapsulator == null ? rectTransform.rect.size : proxy.rect.size;

                float newSize = horizontal ? rectSize.x : rectSize.y;

                Vector3 containerPos = rectTransform.position;
                if (moveOnResize)
                {

                    //Vector3 offset = new Vector3((horizontal ? (((rectTransform.pivot.x) - 2) * 0.5f) : 0) * (newSize - fullSize), (horizontal ? 0 : (((rectTransform.pivot.y) - 2) * 0.5f)) * (fullSize - newSize), 0);

                    //proxy.localPosition = proxy.localPosition + offset;

                    Vector3 pivotPos = proxy.localPosition + new Vector3(proxy.pivot.x * rectSize.x, proxy.pivot.y * rectSize.y, 0);

                    proxy.localPosition = proxy.localPosition + (pivotPosition - pivotPos);

                }

                fullSize = newSize;
                if (proxyEncapsulator == null) 
                {
                    proxy.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, rectSize.x);
                    proxy.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rectSize.y);
                }
                if (pinContainerOnResize) rectTransform.position = containerPos;
            }

        }

        protected void Update()
        {

            UpdateDynamicSize();

            if (allowViewers && viewers != null && viewers.Count > 0)
            {

                viewers.RemoveWhere(i => i == null || !i.IsHovering);

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

        protected void LateUpdate()
        {

            if (!IsInitialized) return;

            if (state && autoFoldOnClickOff && autoFoldMouseButtonMask != MouseButtonMask.None)
            {

                bool ClickedOff()
                {

                    var objects = CursorProxy.ObjectsUnderCursor;
                    if (objects == null) return true;
                    Transform root = proxy;
                    if (includeProxyParentAsAutoFoldPreventionObject && proxy.parent != null) root = proxy.parent; 
                    for(int a = 0; a < objects.Count; a++)
                    {
                        var obj = objects[a];
                        if (obj == null) continue;
                        if (obj.transform.IsInHierarchy(root)) return false;
                    }

                    return true;

                }

                IEnumerator AutoFold()
                {

                    yield return null;

                    if (ClickedOff() && state && !tweening) Fold();

                }

                if (autoFoldMouseButtonMask.HasFlag(MouseButtonMask.LeftMouseButton) && InputProxy.CursorPrimaryButtonDown) 
                {
                    if (ClickedOff()) StartCoroutine(AutoFold());
                } 
                else if (autoFoldMouseButtonMask.HasFlag(MouseButtonMask.RightMouseButton) && InputProxy.CursorSecondaryButtonDown)
                {
                    if (ClickedOff()) StartCoroutine(AutoFold());
                }
                else if (autoFoldMouseButtonMask.HasFlag(MouseButtonMask.MiddleMouseButton) && InputProxy.CursorAuxiliaryButtonDown)
                {
                    if (ClickedOff()) StartCoroutine(AutoFold());
                }

            }

        }

    }

}

#endif
