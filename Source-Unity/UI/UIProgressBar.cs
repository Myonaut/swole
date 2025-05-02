#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using Swole.API.Unity;

namespace Swole.UI
{
    public class UIProgressBar : MonoBehaviour
    {

#if UNITY_EDITOR
        private Orientation prevOrientation;
        private Mode prevMode;
        private float prevProgress;
        private RectOffset prevPadding = new RectOffset();
        private bool prevPreservePipDimensions;
        private float prevSpacing;

        public void OnValidate()
        {
            if (padding == null || prevPadding == null) return;

            bool reinit = false;
            if (prevOrientation != orientation) reinit = true;
            if (prevMode != mode) reinit = true;

            if (reinit)
            {
                initialized = false;
                isDirty = true;
                prevOrientation = orientation;
                prevMode = mode;
                prevPadding.left = padding.left;
                prevPadding.right = padding.right;
                prevPadding.top = padding.top;
                prevPadding.bottom = padding.bottom;
                prevPreservePipDimensions = preservePipDimensions;
                prevSpacing = spacing;
            } 
            else if (prevPreservePipDimensions != preservePipDimensions || prevSpacing != spacing || prevPadding.left != padding.left || prevPadding.right != padding.right || prevPadding.top != padding.top || prevPadding.bottom != padding.bottom)
            {
                prevPadding.left = padding.left;
                prevPadding.right = padding.right;
                prevPadding.top = padding.top;
                prevPadding.bottom = padding.bottom;
                prevPreservePipDimensions = preservePipDimensions;
                prevSpacing = spacing;
                isDirty = true;
            }
        }
#endif

        [Serializable]
        public enum Mode
        {
            Stretch, Mask, Pips, MaskedPips
        }

        public bool IsPipBased => mode == Mode.Pips || mode == Mode.MaskedPips;
        public bool IsMaskBased => mode == Mode.Mask || mode == Mode.MaskedPips;

        [Serializable]
        public enum Orientation
        {
            LeftToRight, RightToLeft, BottomToTop, TopToBottom
        }

        [Tooltip("The rect transform that will determine the size of the progress bar and contain the bar object(s).")]
        public RectTransform barContainer;
        [Tooltip("The rect transform of the bar object. When using Pips, this object serves as a prefab for each pip. Its initial size will determine the size of a pip.")]
        public RectTransform barTransform;

        private Rect lastContainerRect;

        private bool hasInitialData;
        private Rect initialBarObjectRect;

        public float PipWidth => initialBarObjectRect.width;
        public float PipHeight => initialBarObjectRect.height;

        [SerializeField]
        private bool preservePipDimensions;
        public bool PreservePipDimensions
        {
            get => preservePipDimensions;
            set => SetPreservePipDimensions(value);
        }
        public void SetPreservePipDimensions(bool preserve)
        {
            this.preservePipDimensions = preserve;
            isDirty = true;

#if UNITY_EDITOR
            prevPreservePipDimensions = preserve;
#endif
        }

        [SerializeField]
        private RectOffset padding;
        public RectOffset Padding
        {
            get => padding;
            set => SetPadding(value);
        }
        public void SetPadding(RectOffset padding)
        {
            this.padding = padding;
            isDirty = true;

#if UNITY_EDITOR
            if (prevPadding == null) prevPadding = new RectOffset();
            prevPadding.left = padding.left;
            prevPadding.right = padding.right;
            prevPadding.bottom = padding.bottom;
            prevPadding.top = padding.top;
#endif
        }

        [SerializeField, Tooltip("Spacing between each pip when in Pip mode.")]
        private float spacing;
        /// <summary>
        /// Spacing between each pip when in Pip mode.
        /// </summary>
        public float Spacing
        {
            get => spacing;
            set => SetSpacing(value);
        }
        public void SetSpacing(float spacing)
        {
            this.spacing = spacing;
            isDirty = true;

#if UNITY_EDITOR
            prevSpacing = spacing;
#endif
        }

        public struct Pip
        {
            public RectTransform rectTransform;
            public Image image;

            public void Show() => SetVisible(true);
            public void Hide() => SetVisible(false);
            public void SetVisible(bool visible)
            {
                if (image == null) return;
                image.enabled = visible;
            }
        }
        private PrefabPool pipPool;
        private List<Pip> pips;

        public void ClearPips()
        {
            if (pips == null) return;
            foreach (var pip in pips) if (pip.rectTransform != null && pip.rectTransform != barTransform)
                {
                    if (pipPool == null)
                    {
                        GameObject.Destroy(pip.rectTransform.gameObject);
                    }
                    else
                    {
                        pipPool.Release(pip.rectTransform.gameObject);
                        pip.rectTransform.gameObject.SetActive(false);
                    }
                }
            pips.Clear();
        }

        private RectTransform maskTransform;

        [SerializeField]
        private Mode mode;
        public Mode BarMode
        {
            get => mode;
            set => SetMode(value);
        }

        [SerializeField]
        private Orientation orientation;
        public Orientation BarOrientation
        {
            get => orientation;
            set => SetOrientation(value);
        }

        public void SetMode(Mode mode)
        {
            if (this.mode == mode) return;
            initialized = false;
            Initialize(mode, orientation, true);
        }

        public void SetOrientation(Orientation orientation)
        {
            if (this.orientation == orientation) return;
            initialized = false;
            Initialize(mode, orientation, true);
        }

        public bool IsHorizontal => orientation == Orientation.LeftToRight || orientation == Orientation.RightToLeft;
        public bool IsVertical => orientation == Orientation.TopToBottom || orientation == Orientation.BottomToTop;

        private bool initialized;
        public void Initialize(Mode mode, Orientation orientation, bool force = false)
        {
            if (initialized && !force) return;

            if (barTransform == null)
            {
                swole.LogWarning($"[{nameof(UIProgressBar)}] barTransform has not been set for '{name}' so it cannot be initialized!");
                return;
            }

            if (!hasInitialData)
            {
                initialBarObjectRect = barTransform.rect;
                hasInitialData = true;
            }

            if (barContainer == null) barContainer = gameObject.GetComponent<RectTransform>();

            barTransform.SetParent(barContainer, false);
            barTransform.localPosition = Vector3.zero;
            barTransform.localRotation = Quaternion.identity;
            barTransform.localScale = Vector3.one;
            barTransform.anchorMin = barTransform.anchorMax = new Vector2(0.5f, 0.5f);
            barTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, initialBarObjectRect.width);
            barTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, initialBarObjectRect.height);

            ClearPips();

            if (maskTransform != null) maskTransform.gameObject.SetActive(false);

            this.mode = mode;
            this.orientation = orientation;

            Rect containerRect = barContainer.rect;

            if (IsPipBased)
            {
                if (pipPool == null)
                {
                    pipPool = gameObject.AddComponent<PrefabPool>();
                    pipPool.Reinitialize(barTransform.gameObject, PoolGrowthMethod.Incremental, 1, 5, 512);
                }
            }

            barTransform.gameObject.SetActive(false);

            if (IsMaskBased)
            {
                if (maskTransform == null) 
                {
                    var maskObj = new GameObject("mask");
                    maskObj.AddComponent<CanvasRenderer>();
                    maskObj.AddComponent<Image>();
                    var mask = maskObj.AddComponent<Mask>();
                    mask.showMaskGraphic = false;
                    maskTransform = maskObj.GetComponent<RectTransform>();
                }
                maskTransform.gameObject.SetActive(true);
                maskTransform.SetParent(barContainer);
                switch(orientation)
                {
                    case Orientation.LeftToRight:
                        maskTransform.SetPivot(PivotPresets.MiddleLeft);
                        break;
                    case Orientation.RightToLeft:
                        maskTransform.SetPivot(PivotPresets.MiddleRight);
                        break;
                    case Orientation.BottomToTop:
                        maskTransform.SetPivot(PivotPresets.BottomCenter);
                        break;
                    case Orientation.TopToBottom:
                        maskTransform.SetPivot(PivotPresets.TopCenter);
                        break;
                }
                maskTransform.SetAnchor(AnchorPresets.StretchAll, false);
                maskTransform.sizeDelta = Vector2.zero;
            }

            if (!IsPipBased)
            {
                barTransform.gameObject.SetActive(true);
                barTransform.SetParent(IsMaskBased ? maskTransform : barContainer);
                if (!IsMaskBased) barTransform.SetAnchor(AnchorPresets.StretchAll, false);
                switch (orientation)
                {
                    case Orientation.LeftToRight:
                        barTransform.SetPivot(PivotPresets.MiddleLeft);
                        if (IsMaskBased) 
                        { 
                            barTransform.SetAnchor(AnchorPresets.MiddleLeft, false); 
                        } 
                        else
                        {
                            barTransform.anchoredPosition = new Vector3(padding.left, (padding.bottom - padding.top) * 0.5f);
                        }
                        break;
                    case Orientation.RightToLeft:
                        barTransform.SetPivot(PivotPresets.MiddleRight);
                        if (IsMaskBased) 
                        { 
                            barTransform.SetAnchor(AnchorPresets.MiddleRight, false); 
                        } 
                        else
                        {
                            barTransform.anchoredPosition = new Vector3(-padding.right, (padding.bottom - padding.top) * 0.5f);
                        }
                        break;
                    case Orientation.BottomToTop:
                        barTransform.SetPivot(PivotPresets.BottomCenter);
                        if (IsMaskBased) 
                        { 
                            barTransform.SetAnchor(AnchorPresets.BottonCenter, false); 
                        } 
                        else
                        {
                            barTransform.anchoredPosition = new Vector3((padding.left - padding.right) * 0.5f, padding.bottom);
                        }
                        break;
                    case Orientation.TopToBottom:
                        barTransform.SetPivot(PivotPresets.TopCenter);
                        if (IsMaskBased) 
                        { 
                            barTransform.SetAnchor(AnchorPresets.TopCenter, false); 
                        } 
                        else
                        {
                            barTransform.anchoredPosition = new Vector3((padding.left - padding.right) * 0.5f, -padding.top);
                        }
                        break;
                }

                if (IsMaskBased)
                {
                    switch (orientation)
                    {
                        case Orientation.LeftToRight:
                            barTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, containerRect.width - (padding.left + padding.right));
                            barTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, containerRect.height - (padding.bottom + padding.top));
                            barTransform.localPosition = new Vector3(padding.left, (padding.bottom - padding.top) * 0.5f, 0);
                            break;
                        case Orientation.RightToLeft:
                            barTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, containerRect.width - (padding.left + padding.right));
                            barTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, containerRect.height - (padding.bottom + padding.top));
                            barTransform.localPosition = new Vector3(-padding.right, (padding.bottom - padding.top) * 0.5f, 0);
                            break;
                        case Orientation.BottomToTop:
                            barTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, containerRect.width - (padding.left + padding.right));
                            barTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, containerRect.height - (padding.bottom + padding.top));
                            barTransform.localPosition = new Vector3((padding.left - padding.right) * 0.5f, padding.bottom, 0);
                            break;
                        case Orientation.TopToBottom:
                            barTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, containerRect.width - (padding.left + padding.right));
                            barTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, containerRect.height - (padding.bottom + padding.top));
                            barTransform.localPosition = new Vector3((padding.left - padding.right) * 0.5f, -padding.top, 0);
                            break;
                    }
                } 
                else
                {
                    barTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, containerRect.width - (padding.left + padding.right));
                    barTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, containerRect.height - (padding.bottom + padding.top));
                }
            }

            initialized = true;

            Redraw();
        }

        protected virtual void Awake()
        {
            if (barTransform == null) 
            {
                swole.LogWarning($"[{nameof(UIProgressBar)}] barTransform has not been set for '{name}'");
                return; 
            }

            Initialize(mode, orientation);
        }

        [SerializeField, Range(0, 1)]
        private float progress;
        public float Progress => progress;
        /// <summary>
        /// Set the progress of the progress bar. 0 = Empty, 1 = Full
        /// </summary>
        public void SetProgress(float progress)
        {
            if (this.progress == progress) return;

            this.progress = Mathf.Clamp01(progress);

#if UNITY_EDITOR
            prevProgress = progress;
#endif

            Refresh();
        }

        public void Refresh()
        {
            if (!initialized)
            {
                Initialize(mode, orientation);
                return;
            }

            Rect containerRect = barContainer.rect;

            if (IsMaskBased)
            {
                Vector2 sizeDelta;
                switch (orientation)
                {
                    case Orientation.LeftToRight:
                    case Orientation.RightToLeft:
                        sizeDelta = maskTransform.sizeDelta;
                        sizeDelta.x = -containerRect.width * (1 - Progress);
                        maskTransform.sizeDelta = sizeDelta;
                        break;
                    case Orientation.BottomToTop:
                    case Orientation.TopToBottom:
                        sizeDelta = maskTransform.sizeDelta;
                        sizeDelta.y = -containerRect.height * (1 - Progress);
                        maskTransform.sizeDelta = sizeDelta;
                        break;
                }

                if (IsPipBased)
                {
                    // ...
                }
                else
                {
                    switch (orientation)
                    {
                        case Orientation.LeftToRight:
                            barTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, containerRect.width - (padding.left + padding.right));
                            barTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, containerRect.height - (padding.bottom + padding.top));
                            barTransform.localPosition = new Vector3(padding.left, (padding.bottom - padding.top) * 0.5f, 0);
                            break;
                        case Orientation.RightToLeft:
                            barTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, containerRect.width - (padding.left + padding.right));
                            barTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, containerRect.height - (padding.bottom + padding.top));
                            barTransform.localPosition = new Vector3(-padding.right, (padding.bottom - padding.top) * 0.5f, 0);
                            break;
                        case Orientation.BottomToTop:
                            barTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, containerRect.width - (padding.left + padding.right));
                            barTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, containerRect.height - (padding.bottom + padding.top));
                            barTransform.localPosition = new Vector3((padding.left - padding.right) * 0.5f, padding.bottom, 0);
                            break;
                        case Orientation.TopToBottom:
                            barTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, containerRect.width - (padding.left + padding.right));
                            barTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, containerRect.height - (padding.bottom + padding.top));
                            barTransform.localPosition = new Vector3((padding.left - padding.right) * 0.5f, -padding.top, 0);
                            break;
                    }
                }
            }
            else
            {
                if (IsPipBased)
                {
                    int pipCount = pips.Count;
                    float fullBarSize = (pipCount * (IsHorizontal ? initialBarObjectRect.width : initialBarObjectRect.height)) + ((pipCount - 1) * spacing);
                    float pipSize = (IsHorizontal ? initialBarObjectRect.width : initialBarObjectRect.height);
                    float pipSizeWithSpacing = pipSize + spacing;

                    float pixelProgress = fullBarSize * progress;
                    for(int a = 0; a < pipCount; a++)
                    {
                        var pip = pips[a];
                        float pos = (pipSize + pipSizeWithSpacing * a) - Mathf.Epsilon;
                        pip.SetVisible(pixelProgress >= pos); 
                    }
                }
                else
                {
                    Vector2 sizeDelta;
                    float pad;
                    switch (orientation)
                    {
                        case Orientation.LeftToRight:
                            pad = padding.horizontal;
                            sizeDelta = default;
                            sizeDelta.x = -(((containerRect.width - pad) * (1 - Progress)) + pad);
                            sizeDelta.y = -padding.vertical;
                            barTransform.sizeDelta = sizeDelta;
                            barTransform.anchoredPosition = new Vector3(padding.left, (padding.bottom - padding.top) * 0.5f);
                            break;
                        case Orientation.RightToLeft:
                            pad = padding.horizontal;
                            sizeDelta = default;
                            sizeDelta.x = -(((containerRect.width - pad) * (1 - Progress)) + pad);
                            sizeDelta.y = -padding.vertical;
                            barTransform.sizeDelta = sizeDelta;
                            barTransform.anchoredPosition = new Vector3(-padding.right, (padding.bottom - padding.top) * 0.5f);
                            break;
                        case Orientation.BottomToTop:
                            pad = padding.vertical;
                            sizeDelta = default;
                            sizeDelta.y = -(((containerRect.height - pad) * (1 - Progress)) + pad);
                            sizeDelta.x = -padding.horizontal;
                            barTransform.sizeDelta = sizeDelta;
                            barTransform.anchoredPosition = new Vector3((padding.left - padding.right) * 0.5f, padding.bottom);
                            break;
                        case Orientation.TopToBottom:
                            pad = padding.vertical;
                            sizeDelta = default;
                            sizeDelta.y = -(((containerRect.height - pad) * (1 - Progress)) + pad);
                            sizeDelta.x = -padding.horizontal;
                            barTransform.sizeDelta = sizeDelta;
                            barTransform.anchoredPosition = new Vector3((padding.left - padding.right) * 0.5f, -padding.top);
                            break;
                    }
                }
            }
        }

        private bool isDirty;
        public void Redraw()
        {
            isDirty = false;

            if (!initialized)
            {
                Initialize(mode, orientation);
                return;
            }

            Rect containerRect = barContainer.rect;

            if (IsPipBased)
            {
                barTransform.gameObject.SetActive(false);
                if (pips == null) pips = new List<Pip>();
                ClearPips();

                int pipCount = Mathf.Max(1, Mathf.FloorToInt(IsHorizontal ? (((containerRect.width + spacing) - (padding.left + padding.right)) / (initialBarObjectRect.width + spacing)) : (((containerRect.height + spacing) - (padding.bottom + padding.top)) / (initialBarObjectRect.height + spacing))));
                float fullBarSize = (pipCount * (IsHorizontal ? initialBarObjectRect.width : initialBarObjectRect.height)) + ((pipCount - 1) * spacing);
                float centering = (IsHorizontal ? ((containerRect.width - (padding.left + padding.right)) - fullBarSize) : ((containerRect.height - (padding.bottom + padding.top)) - fullBarSize)) * 0.5f;

                for (int a = 0; a < pipCount; a++)
                {
                    Pip pip = new Pip();
                    if (pipPool == null)
                    {
                        pip.rectTransform = Instantiate(barTransform);
                    } 
                    else
                    {
                        if (pipPool.TryGetNewInstance(out GameObject pipObj))
                        {
                            pip.rectTransform = pipObj.GetComponent<RectTransform>();
                        }
                    }
                    if (pip.rectTransform == null) continue;
                    pip.image = pip.rectTransform.gameObject.GetComponent<Image>();

                    if (IsMaskBased)
                    {
                        pip.rectTransform.SetParent(maskTransform);
                    } 
                    else
                    {
                        pip.rectTransform.SetParent(barContainer);
                    } 
                    pip.rectTransform.gameObject.SetActive(true);

                    float offset = centering + (IsHorizontal ? (initialBarObjectRect.width + spacing) : (initialBarObjectRect.height + spacing)) * a;
                    Vector2 sizeDelta;
                    switch (orientation)
                    {
                        case Orientation.LeftToRight:
                            pip.rectTransform.SetPivot(PivotPresets.MiddleLeft);
                            pip.rectTransform.SetAnchor(AnchorPresets.VertStretchLeft, false);
                            pip.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, initialBarObjectRect.width);

                            if (preservePipDimensions)
                            {
                                pip.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, initialBarObjectRect.height - (padding.bottom + padding.top));
                            }
                            else
                            { 
                                sizeDelta = pip.rectTransform.sizeDelta;
                                sizeDelta.y = -(padding.bottom + padding.top); // Stretch height to container border
                                pip.rectTransform.sizeDelta = sizeDelta;
                            }

                            pip.rectTransform.anchoredPosition = new Vector3(padding.left + offset, (padding.bottom - padding.top) * 0.5f);
                            break;
                        case Orientation.RightToLeft:
                            pip.rectTransform.SetPivot(PivotPresets.MiddleRight);
                            pip.rectTransform.SetAnchor(AnchorPresets.VertStretchRight, false);
                            pip.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, initialBarObjectRect.width);

                            if (preservePipDimensions)
                            {
                                pip.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, initialBarObjectRect.height - (padding.bottom + padding.top));
                            }
                            else
                            {
                                sizeDelta = pip.rectTransform.sizeDelta;
                                sizeDelta.y = -(padding.bottom + padding.top);  // Stretch height to container border
                                pip.rectTransform.sizeDelta = sizeDelta;
                            }

                            pip.rectTransform.anchoredPosition = new Vector3(-(padding.right + offset), (padding.bottom - padding.top) * 0.5f);
                            break;
                        case Orientation.BottomToTop:
                            pip.rectTransform.SetPivot(PivotPresets.BottomCenter);
                            pip.rectTransform.SetAnchor(AnchorPresets.HorStretchBottom, false);
                            pip.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, initialBarObjectRect.height);

                            if (preservePipDimensions)
                            {
                                pip.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, initialBarObjectRect.width - (padding.left + padding.right));
                            }
                            else
                            {
                                sizeDelta = pip.rectTransform.sizeDelta;
                                sizeDelta.x = -(padding.left + padding.right);  // Stretch width to container border
                                pip.rectTransform.sizeDelta = sizeDelta;
                            }

                            pip.rectTransform.anchoredPosition = new Vector3((padding.left - padding.right) * 0.5f, padding.bottom + offset);
                            break;
                        case Orientation.TopToBottom:
                            pip.rectTransform.SetPivot(PivotPresets.TopCenter);
                            pip.rectTransform.SetAnchor(AnchorPresets.HorStretchTop, false);
                            pip.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, initialBarObjectRect.height);

                            if (preservePipDimensions)
                            {
                                pip.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, initialBarObjectRect.width - (padding.left + padding.right));
                            }
                            else
                            {
                                sizeDelta = pip.rectTransform.sizeDelta;
                                sizeDelta.x = -(padding.left + padding.right);  // Stretch width to container border
                                pip.rectTransform.sizeDelta = sizeDelta;
                            }

                            pip.rectTransform.anchoredPosition = new Vector3((padding.left - padding.right) * 0.5f, -(padding.top + offset));
                            break;
                    }

                    pip.Show();
                    pips.Add(pip);
                }
            }
            else
            {
                // ...
            }

            Refresh();
        }

        protected virtual void OnGUI()
        {
            if (barContainer == null || (!IsMaskBased && !IsPipBased)) return;

            Rect containerRect = barContainer.rect;
            if (containerRect.width != lastContainerRect.width || containerRect.height != lastContainerRect.height)
            {
                lastContainerRect = containerRect;
                isDirty = true;
            } 
        }
         
        protected virtual void Update()
        {
            if (isDirty) Redraw();

#if UNITY_EDITOR
            if (prevProgress != progress)
            {
                Refresh();
                prevProgress = progress;
            }
#endif
        }

    }
}


#endif