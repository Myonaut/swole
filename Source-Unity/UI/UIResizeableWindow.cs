#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using TMPro;

namespace Swole.UI
{

    /// <summary>
    /// Used as a collection of ui elements that make up a resizing window.
    /// </summary>
    [ExecuteInEditMode]
    public class UIResizeableWindow : MonoBehaviour
    {

        public UIPopup rootWindowElement;

        public Text titleText;
        public TMP_Text titleTextTMP;

        public Button closingButton;
        public UITabButton closingButtonAlt;

        public UIResizer resizer;

        public RectTransform contentContainer;

        protected virtual void Awake()
        {

            if (rootWindowElement == null) 
            { 
                
                rootWindowElement = gameObject.GetComponent<UIPopup>();

                if (rootWindowElement == null) rootWindowElement = gameObject.GetComponentInParent<UIPopup>();
            
            }

            if (resizer == null) resizer = gameObject.GetComponentInChildren<UIResizer>();

            if (titleText == null)
            {
                var t = transform.FindDeepChildLiberal("Title");
                if (t != null) titleText = t.GetComponent<Text>();
            }
            if (titleTextTMP == null)
            {
                var t = transform.FindDeepChildLiberal("Title");
                if (t != null) titleTextTMP = t.GetComponent<TMP_Text>();
            }


            if (closingButton == null)
            {
                var t = transform.FindDeepChildLiberal("Closer");
                if (t != null) closingButton = t.GetComponent<Button>();
            }
            if (closingButtonAlt == null)
            {
                var t = transform.FindDeepChildLiberal("Closer");
                if (t != null) closingButtonAlt = t.GetComponent<UITabButton>();
            }

            if (contentContainer == null)
            {
                var t = transform.FindDeepChildLiberal("Content");
                if (t is RectTransform) contentContainer = (RectTransform)t;
            }

        }

    }

}

#endif