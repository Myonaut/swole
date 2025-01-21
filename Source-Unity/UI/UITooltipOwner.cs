#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using Swole.API.Unity;
using UnityEngine.Events;

namespace Swole.UI
{

    public class UITooltipOwner : MonoBehaviour
    {

        protected float hoverTime;

        public float timeToShow = 1.35f;

        public bool dontInstantiateTooltip = false;

        public UITooltip tooltipPrefab;

        protected UITooltip tooltipInstance;
         
        public UITooltip Tooltip
        {

            get
            {

                if (tooltipInstance == null && tooltipPrefab != null)
                {

                    tooltipInstance = dontInstantiateTooltip ? tooltipPrefab : Instantiate(tooltipPrefab);

                    tooltipInstance.gameObject.SetActive(false);

                }

                return tooltipInstance;

            }

        }

        protected virtual void OnDestroy()
        {

            if (tooltipInstance != null) GameObject.DestroyImmediate(tooltipInstance.gameObject);

        }

        public UnityEvent<GameObject> OnShowTooltip;
        public void ShowTooltip(GameObject targetObject)
        {

            if (Tooltip == null) return;

            if (UITooltipManager.HasTooltipCanvas && tooltipInstance.transform.parent != UITooltipManager.TooltipCanvas.transform)
            {

                tooltipInstance.transform.SetParent(UITooltipManager.TooltipCanvas.transform, false);

                tooltipInstance.RefetchCanvas();

            }

            tooltipInstance.gameObject.SetActive(true);

            cursorCanvasPosition = tooltipInstance.Canvas.ScreenToCanvasSpace(CursorProxy.ScreenPosition);

            this.targetObject = targetObject;
            OnShowTooltip?.Invoke(targetObject);

        }

        public UnityEvent OnHideTooltip;
        public void HideTooltip()
        {

            if (tooltipInstance == null) return;

            tooltipInstance.gameObject.SetActive(false);

            hoverTime = 0;

            OnHideTooltip?.Invoke();

        }

        public bool IsShowing => tooltipInstance == null ? false : tooltipInstance.gameObject.activeInHierarchy;

        public float autoHideDistance;

        protected Vector3 cursorCanvasPosition;

        protected virtual void Update()
        {

            if (Tooltip == null) return;

            bool showing = IsShowing;

            if (autoHideDistance > 0 && showing)
            {

                if (Vector3.Distance(tooltipInstance.Canvas.ScreenToCanvasSpace(CursorProxy.ScreenPosition), cursorCanvasPosition) >= autoHideDistance) HideTooltip();

            }

            if (IsHovering)
            {
                if (!showing)
                {
                    hoverTime += Time.deltaTime;

                    if (hoverTime >= timeToShow)
                    {
                        ShowTooltip(hoveredObject);
                    }

                }
            }
            else
            {

                hoverTime = 0;

                HideTooltip();

            }

        }

        public List<PointerEventsProxy> pointerEventProxies = new List<PointerEventsProxy>();

        protected PointerEventsProxy pointerEventProxy;

        protected bool isHovering;
        protected GameObject hoveredObject;

        protected GameObject targetObject;
        /// <summary>
        /// The object that the tooltip is bound to
        /// </summary>
        public GameObject TargetObject;

        protected int lastQueryFrame;

        public bool IsHovering
        {

            get
            {

                int frame = Time.frameCount;

                if (lastQueryFrame == frame) return isHovering;

                isHovering = false;

                lastQueryFrame = frame;

                if (pointerEventProxy != null && pointerEventProxy.IsHovering)
                {
                    isHovering = true;
                    hoveredObject = pointerEventProxy.gameObject;
                }
                else if (pointerEventProxies != null)
                {

                    foreach (PointerEventsProxy proxy in pointerEventProxies)
                    {

                        if (proxy == null || !proxy.IsHovering) continue; 

                        isHovering = true;
                        hoveredObject = proxy.RootObject;

                        break;

                    }

                }

                return isHovering;

            }

        }

        protected virtual void Awake()
        {

            pointerEventProxy = gameObject.GetComponent<PointerEventsProxy>();

            if (pointerEventProxy == null)
            {

                Image img = gameObject.GetComponent<Image>();

                if (img != null && img.raycastTarget)
                {

                    pointerEventProxy = gameObject.AddComponent<PointerEventsProxy>();

                }

            }

        }

        public void SetText(string text)
        {
            var tooltip = Tooltip;
            if (tooltip == null) return;

            CustomEditorUtils.SetComponentText(tooltip.gameObject, text);
        }

    }

}

#endif