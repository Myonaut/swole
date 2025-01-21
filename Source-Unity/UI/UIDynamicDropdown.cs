#if (UNITY_EDITOR || UNITY_STANDALONE)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

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

        [SerializeField]
        protected bool overrideSortingOnFoldOut;
        public int sortingOrder = 99;
        public RectTransform sortingTransform;
        protected Canvas sortingCanvas;
        protected GraphicRaycaster sortingRaycaster;
        protected CanvasGroup canvasGroup;
        protected bool isSubscribedSorting;

        public void SetOverrideSortingOnFoldOut(bool overrideSorting)
        {
            overrideSortingOnFoldOut = overrideSorting;
            if (sortingTransform == null) sortingTransform = gameObject.AddOrGetComponent<RectTransform>();
            
            if (overrideSorting)
            {
                if (!isSubscribedSorting)
                {
                    OnFoldOutEnd.AddListener(ApplySortingOverride); 
                    OnFoldBegin.AddListener(StopSortingOverride);

                    isSubscribedSorting = true;
                }

                if (state) ApplySortingOverride(); else StopSortingOverride();
            } 
            else
            {
                if (isSubscribedSorting)
                {
                    OnFoldOutEnd.RemoveListener(ApplySortingOverride);
                    OnFoldBegin.RemoveListener(StopSortingOverride);  
                     
                    isSubscribedSorting = false;
                }

                StopSortingOverride();
                GameObject.Destroy(sortingRaycaster);
                GameObject.Destroy(canvasGroup);
                GameObject.Destroy(sortingCanvas);
            }
        }
        protected void ApplySortingOverride()
        {
            if (sortingCanvas == null) 
            { 
                var parentCanvas = sortingTransform.GetComponentInParent<Canvas>();
                var parentRaycaster = parentCanvas.GetComponent<GraphicRaycaster>();
                sortingCanvas = sortingTransform.gameObject.AddOrGetComponent<Canvas>(); 
                sortingCanvas.additionalShaderChannels = parentCanvas.additionalShaderChannels;
                sortingRaycaster = sortingCanvas.gameObject.AddOrGetComponent<GraphicRaycaster>();
                if (parentRaycaster != null)
                {
                    sortingRaycaster.ignoreReversedGraphics = parentRaycaster.ignoreReversedGraphics;
                    sortingRaycaster.blockingObjects = parentRaycaster.blockingObjects;
                    sortingRaycaster.blockingMask = parentRaycaster.blockingMask; 
                }
                canvasGroup = sortingCanvas.gameObject.AddOrGetComponent<CanvasGroup>();
                canvasGroup.alpha = 1;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            sortingCanvas.enabled = true; 
            sortingCanvas.overrideSorting = true;
            sortingCanvas.sortingOrder = sortingOrder;
        }
        protected void StopSortingOverride()
        {
            //if (sortingRaycaster != null) GameObject.Destroy(sortingRaycaster);
            //if (sortingCanvas != null) GameObject.Destroy(sortingCanvas);
            //sortingRaycaster = null;
            //sortingCanvas = null;
            if (sortingCanvas != null) sortingCanvas.enabled = false;
        }

        protected virtual void Awake()
        {
            layoutGroup = gameObject.GetComponent<VerticalLayoutGroup>();
            if (layoutGroupTransform == null && layoutGroup != null) layoutGroupTransform = layoutGroup.GetComponent<RectTransform>();
            if (layoutGroupTransform == null) layoutGroupTransform = gameObject.GetComponent<RectTransform>();

            if (sortingTransform == null) sortingTransform = gameObject.AddOrGetComponent<RectTransform>();
            SetOverrideSortingOnFoldOut(overrideSortingOnFoldOut);
        }

        public static void SetItemName(GameObject menuItem, string name)
        {
            if (menuItem == null) return;
            if (name == null) name = string.Empty;

            var textTransform = menuItem.transform.FindDeepChildLiberal("Name");
            if (textTransform == null) textTransform = menuItem.transform.FindDeepChildLiberal("Text");
            if (textTransform != null)
            {
                var text = textTransform.gameObject.GetComponentInChildren<Text>(true);
                if (text != null) text.text = name;

                var textTMP = textTransform.gameObject.GetComponentInChildren<TMP_Text>(true);
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
                    var child = layoutGroupTransform.GetChild(a);
                    if (child == null || ReferenceEquals(child.gameObject, menuItemPrototype)) continue; 

                    Destroy(layoutGroupTransform.GetChild(a).gameObject); 
                }
        }

        public int MenuItemCount => layoutGroupTransform == null ? 0 : layoutGroupTransform.childCount;
        public RectTransform GetMenuItem(int index) => layoutGroupTransform == null || index < 0 || index >= layoutGroupTransform.childCount ? null : layoutGroupTransform.GetChild(index).GetComponent<RectTransform>();
        public RectTransform FindMenuItem(string name, bool caseSensitive = false)
        {
            if (!caseSensitive) name = name.ToLower().Trim();

            for(int a = 0; a < MenuItemCount; a++)
            {
                var item = GetMenuItem(a);
                if (item == null) continue;

                if ((caseSensitive ? item.name : item.name.ToLower().Trim()) == name) return item; 
            }

            return null;
        }

        public UnityEvent<string> OnSelectionChanged = new UnityEvent<string>();

        public void SetSelectionText(string text) => SetSelectionText(text, true);
        public void SetSelectionText(string text, bool notifyListeners)
        {
            if (selectedText != null) selectedText.text = text;
            if (selectedTextTMP != null) selectedTextTMP.SetText(text);

            if (notifyListeners) OnSelectionChanged?.Invoke(text);
        }

        public void SetSelectionText(Text text) => SetSelectionText(text, true);
        public void SetSelectionText(Text text, bool notifyListeners)
        {
            if (text == null) return;
            SetSelectionText(text.text, notifyListeners);
        }
        public void SetSelectionText(TMP_Text text) => SetSelectionText(text, true);
        public void SetSelectionText(TMP_Text text, bool notifyListeners)
        {
            if (text == null) return;
            SetSelectionText(text.text, notifyListeners);
        }

        public string GetSelectionText()
        {
            if (selectedText != null) return selectedText.text;
            if (selectedTextTMP != null) return selectedTextTMP.text; 

            return string.Empty;
        }

    }

}

#endif