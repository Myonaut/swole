#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

using TMPro;

namespace Swole.UI
{
    public class UIInputFieldContentVisibility : MonoBehaviour
    {
        public bool initialVisibilityState;
        public bool forceLabelUpdateOnReveal;

        public TMP_InputField.ContentType tmpVisibleContentType = TMP_InputField.ContentType.Standard;
        public TMP_InputField.ContentType tmpHiddenContentType = TMP_InputField.ContentType.Password;
        public TMP_InputField tmpInput;

        public InputField.ContentType visibleContentType = InputField.ContentType.Standard;
        public InputField.ContentType hiddenContentType = InputField.ContentType.Password;
        public InputField input;

        public UnityEvent<bool> OnSetVisibility;
        public UnityEvent OnShowContent;
        public UnityEvent OnHideContent;

        protected bool isVisible;

        protected void Awake()
        {
            SetContentVisibility(initialVisibilityState); 
        }

        public void SetContentVisibility(bool visible)
        {
            isVisible = visible;

            if (tmpInput != null) 
            { 
                tmpInput.contentType = visible ? tmpVisibleContentType : tmpHiddenContentType;
                if (!visible || forceLabelUpdateOnReveal) tmpInput.ForceLabelUpdate(); 
            }
            if (input != null) 
            { 
                input.contentType = visible ? visibleContentType : hiddenContentType;
                if (!visible || forceLabelUpdateOnReveal) input.ForceLabelUpdate();  
            }

            OnSetVisibility?.Invoke(visible);
            if (visible)
            {
                OnShowContent?.Invoke();
            } 
            else
            {
                OnHideContent?.Invoke(); 
            }
        }
        public void ShowContent() => SetContentVisibility(true);
        public void HideContent() => SetContentVisibility(false);
        public void ToggleContentVisibility() => SetContentVisibility(!isVisible);
    }
}

#endif