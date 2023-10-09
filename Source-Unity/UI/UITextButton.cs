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

    [RequireComponent(typeof(TMP_Text))]
    public class UITextButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
    {

        public bool disable = false;

        public Color defaultColor;
        public Color hoverColor;
        public Color pressedColor;
        public Color disabledColor;

        public UnityEvent OnClick;

        public UnityEvent OnHover;

        private TMP_Text text;

        public void Awake()
        {

            text = gameObject.GetComponent<TMP_Text>();

        }

        public void OnPointerClick(PointerEventData eventData)
        {

            if (disable) return;

            OnClick.Invoke();

        }

        private bool isHovering = false;

        public void OnPointerEnter(PointerEventData eventData)
        {

            isHovering = true;

            OnHover.Invoke();

        }

        public void OnPointerExit(PointerEventData eventData)
        {

            isHovering = false;

        }

        private bool isPressed = false;

        public void OnPointerDown(PointerEventData eventData)
        {

            isPressed = true;

        }

        public void OnPointerUp(PointerEventData eventData)
        {

            isPressed = false;

        }

        protected void OnGUI()
        {

            if (disable)
            {

                text.color = disabledColor;

            }
            else if (isPressed)
            {

                text.color = pressedColor;

            }
            else if (isHovering)
            {

                text.color = hoverColor;

            }
            else
            {

                text.color = defaultColor;

            }

        }

        protected void OnEnable()
        {

            isHovering = false;
            isPressed = false;

        }

        protected void OnDisable()
        {

            isHovering = false;
            isPressed = false;

        }

    }

}

#endif