#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

using TMPro;

namespace Swole.UI
{

    public class UITabButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {

        public string disableUnlockParameter;

        public bool disable = false;

        private bool prevDisable = false;

        public bool toggle = false;
        public bool IsToggledOn => toggle;

        [Tooltip("Allow hover events to be called while the button is toggled on?")]
        public bool hoverWhileActive = false;

        [Tooltip("Allow click events to be called while the button is toggled on?")]
        public bool clickWhileActive = false;

        public bool disableToggleObjectsOnHover = true;

        public GameObject toggleOnObject;
        public GameObject toggleOffObject;
        public GameObject toggleHoverObject;
        public GameObject toggleDisabledObject;

        public UnityEvent OnClick = new UnityEvent();
        public UnityEvent OnHoverBegin = new UnityEvent();
        public UnityEvent OnHoverEnd = new UnityEvent();
        public UnityEvent OnToggleOn = new UnityEvent();
        public UnityEvent OnToggleOff = new UnityEvent();

        protected void OnDestroy()
        {

            OnClick?.RemoveAllListeners();
            OnHoverBegin?.RemoveAllListeners();
            OnHoverEnd?.RemoveAllListeners();
            OnToggleOn?.RemoveAllListeners();
            OnToggleOff?.RemoveAllListeners();

        }

        public GameObject[] togglableObjects;

        public void Start()
        {

            if (!string.IsNullOrEmpty(disableUnlockParameter))
            {

                disable = !ContentWall.IsUnlocked(disableUnlockParameter);

            }

            UpdateGraphic();

        }

        public void Update()
        {

            if (prevDisable != disable)
            {

                prevDisable = disable;

                UpdateGraphic();

            }

        }

        public void UpdateGraphic()
        {

            if (toggleOnObject != null)
            {

                toggleOnObject.SetActive(disable ? false : (toggleOnObject == toggleOffObject) ? (toggleHoverObject == null ? true : (hoverWhileActive ? (isHovering ? !disableToggleObjectsOnHover : true) : true)) : (toggleHoverObject == null ? toggle : (hoverWhileActive ? (isHovering ? !disableToggleObjectsOnHover : toggle) : toggle)));

            }

            if (toggleOffObject != null)
            {

                toggleOffObject.SetActive(disable ? false : (toggleHoverObject == null ? !toggle : (isHovering ? !disableToggleObjectsOnHover : !toggle)));

            }

            if (toggleHoverObject != null)
            {

                toggleHoverObject.SetActive(disable ? false : (!toggle ? isHovering : (toggleOffObject == null ? isHovering : (hoverWhileActive ? isHovering : false))));
            }

            if (toggleDisabledObject != null)
            {

                toggleDisabledObject.SetActive(disable);

            }

        }

        public void ToggleOn()
        {

            if (disable) return;

            toggle = true;

            if (togglableObjects != null) foreach (GameObject obj in togglableObjects) if (obj != null) obj.SetActive(true);

            UpdateGraphic();

            OnToggleOn.Invoke();

        }

        public void ToggleOff()
        {

            if (disable) return;

            toggle = false;

            if (togglableObjects != null) foreach (GameObject obj in togglableObjects) if (obj != null) obj.SetActive(false);

            UpdateGraphic();

            OnToggleOff.Invoke();

        }

        public void OnPointerClick(PointerEventData eventData)
        {

            if (disable || (eventData == null ? false : eventData.button != PointerEventData.InputButton.Left)) return;

            bool toggleState = toggle;

            ToggleOn();

            if (toggleState ? clickWhileActive : true) OnClick.Invoke();

        }

        public bool isHovering = false;

        public void OnPointerEnter(PointerEventData eventData)
        {

            if (disable) return;

            isHovering = hoverWhileActive || !toggle;

            UpdateGraphic();

            if (isHovering) OnHoverBegin.Invoke();

        }

        public void OnPointerExit(PointerEventData eventData)
        {

            if (disable) return;

            bool setHover = isHovering;

            isHovering = false;

            UpdateGraphic();

            if (setHover) OnHoverEnd.Invoke();

        }

        protected void OnEnable()
        {

            LeanTween.delayedCall(0.01f, UpdateGraphic);

        }

        protected void OnDisable()
        {

            isHovering = false;

        }

    }

}

#endif
