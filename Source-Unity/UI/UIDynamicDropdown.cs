#if (UNITY_EDITOR || UNITY_STANDALONE)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using TMPro;

namespace Swole.UI
{

    public class UIDynamicDropdown : UIProxyFoldout
    {

        [SerializeField]
        private Text selectedText;
        [SerializeField]
        private TMP_Text selectedTextTMP;

        [SerializeField]
        private GameObject menuItemPrototype;

        [SerializeField]
        private VerticalLayoutGroup layoutGroup;
        [SerializeField]
        private RectTransform layoutGroupTransform;
        public RectTransform LayoutGroupTransform => layoutGroupTransform;

        protected virtual void Awake()
        {
            layoutGroup = gameObject.GetComponent<VerticalLayoutGroup>();
            if (layoutGroupTransform == null && layoutGroup != null) layoutGroupTransform = layoutGroup.GetComponent<RectTransform>();
            if (layoutGroupTransform == null) layoutGroupTransform = gameObject.GetComponent<RectTransform>();
        }

        public static void SetItemName(GameObject menuItem, string name)
        {
            if (menuItem == null) return;
            if (name == null) name = string.Empty;

            var textTransform = menuItem.transform.FindDeepChildLiberal("Name");
            if (textTransform == null) textTransform = menuItem.transform.FindDeepChildLiberal("Text");
            if (textTransform != null)
            {
                var text = textTransform.gameObject.GetComponentInChildren<Text>();
                if (text != null) text.text = name;

                var textTMP = textTransform.gameObject.GetComponentInChildren<TMP_Text>();
                if (textTMP != null) textTMP.SetText(name);
            }
        }

        public GameObject CreateNewMenuItem(string name = null)
        {
            if (string.IsNullOrEmpty(name)) name = $"item_{layoutGroupTransform.childCount}";
            GameObject item;
            if (menuItemPrototype != null) item = Instantiate(menuItemPrototype); else item = new GameObject();
            item.name = name;
            if (layoutGroupTransform != null) item.transform.SetParent(layoutGroupTransform, false); else item.transform.SetParent(transform, false);
            SetItemName(item, name);
            item.SetActive(true);
            return item;
        }

        public void ClearMenuItems()
        {
            if (layoutGroupTransform != null) for (int a = 0; a < layoutGroupTransform.childCount; a++) 
                {
                    Destroy(layoutGroupTransform.GetChild(a).gameObject); 
                }
        }

        public void SetSelectionText(string text)
        {
            if (selectedText != null) selectedText.text = text;
            if (selectedTextTMP != null) selectedTextTMP.SetText(text);
        }

        public void SetSelectionText(Text text)
        {
            if (text == null) return;
            if (selectedText != null) selectedText.text = text.text;
            if (selectedTextTMP != null) selectedTextTMP.SetText(text.text);
        }
        public void SetSelectionText(TMP_Text text)
        {
            if (text == null) return;
            if (selectedText != null) selectedText.text = text.text;
            if (selectedTextTMP != null) selectedTextTMP.SetText(text.text);
        }

    }

}

#endif