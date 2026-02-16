#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using Swole.API.Unity;

namespace Swole.UI
{

    public class UIRadialMenu : MonoBehaviour
    {

        [Serializable]
        public class MenuItem
        {
            public string id;
            public string displayName;
            public Sprite icon;
            public UnityEvent onHighlight;
            public UnityEvent onHighlightEnd;
            public UnityEvent onSelect;

            [NonSerialized]
            public GameObject activeObject;
            //[NonSerialized]
            public float angleStart;
            //[NonSerialized]
            public float angleSize;
        }

        [SerializeField]
        protected List<MenuItem> menuItems = new List<MenuItem>();
        public bool TryGetItem(string id, out MenuItem item)
        {
            item = null;
            if (menuItems == null) return false;

            foreach(var item_ in menuItems)
            {
                if (item_.id == id)
                {
                    item = item_;
                    return true;
                }
            }

            return false;
        }
        public void AddItem(string id, string displayName, Sprite icon, UnityAction highlightListener, UnityAction selectListener, UnityAction highlightEndListener, bool rebuildUI = true)
        {
            MenuItem menuItem = null;

            int existingIndex = IndexOfItem(id);
            if (existingIndex < 0)
            {
                menuItem = new MenuItem()
                {
                    id = id,
                    displayName = displayName,
                    icon = icon
                };

                menuItems.Add(menuItem); 
            }
            else
            {
                menuItem = menuItems[existingIndex];
                menuItem.icon = icon;
            }

            if (highlightListener != null)
            {
                if (menuItem.onHighlight == null) menuItem.onHighlight = new UnityEvent(); 
                menuItem.onHighlight.AddListener(highlightListener); 
            }

            if (highlightEndListener != null)
            {
                if (menuItem.onHighlightEnd == null) menuItem.onHighlightEnd = new UnityEvent();
                menuItem.onHighlightEnd.AddListener(highlightListener);
            }

            if (selectListener != null)
            {
                if (menuItem.onSelect == null) menuItem.onSelect = new UnityEvent();
                menuItem.onSelect.AddListener(selectListener);
            }

            if (rebuildUI)
            {
                Rebuild();
            }
        }
        public int IndexOfItem(string id)
        {
            for (int i = 0; i < menuItems.Count; i++)
            {
                if (menuItems[i].id == id) return i;
            }
            return -1;
        }
        public void RemoveItem(string id, bool rebuildUI = true)
        {
            int index = IndexOfItem(id);
            if (index >= 0)
            {
                menuItems.RemoveAt(index);
                if (rebuildUI)
                {
                    Rebuild();
                }
            }
        }
        public void Clear(bool rebuildUI = true)
        {
            menuItems.Clear();

            if (rebuildUI)
            {
                Rebuild();
            }
        }

        protected PrefabPool menuItemPool;
        [SerializeField]
        protected RectTransform menuItemPrototype;
        public void SetMenuItemPrototype(RectTransform menuItemPrototype, bool rebuildUI = true)
        {
            this.menuItemPrototype = menuItemPrototype;
            if (menuItemPrototype == null) return;

            if (menuItemPool != null) 
            { 
                menuItemPool.Reinitialize(menuItemPrototype.gameObject, PoolGrowthMethod.Incremental, 1, 8, 64); 
            }

            if (rebuildUI) Rebuild();
        }
        protected List<RectTransform> activeMenuItemObjects = new List<RectTransform>();

        public string menuItemObjectSizeMaskName = "size";
        public string menuItemObjectCircleName = "circle";


        protected PrefabPool menuItemIconPool;
        [SerializeField]
        protected RectTransform menuItemIconPrototype;
        public void SetMenuItemIconPrototype(RectTransform menuItemIconPrototype, bool rebuildUI = true)
        {
            this.menuItemIconPrototype = menuItemIconPrototype;
            if (menuItemIconPrototype == null) return;

            baseIconPush = 0f;

            var img = menuItemIconPrototype.GetComponentInChildren<UnityEngine.UI.Image>();
            if (img != null)
            {
                img.raycastTarget = false;
                var rT = img.GetComponent<RectTransform>();
                if (rT != null)
                {
                    baseIconPush = rT.anchoredPosition.magnitude;
                }
            }

            if (menuItemIconPool != null)
            {
                menuItemIconPool.Reinitialize(menuItemIconPrototype.gameObject, PoolGrowthMethod.Incremental, 1, 8, 64);
            }

            if (rebuildUI) Rebuild();
        }
        protected List<RectTransform> activeMenuItemIconObjects = new List<RectTransform>();

        public UnityEngine.UI.Text selectionText;
        public TMPro.TMP_Text selectionTextTMP;

        public float iconPushPerItem;
        private float baseIconPush;

        public int maxMenuItemsPerPage = 8;
        public int Pages => menuItems == null ? 1 : Mathf.Max(1, Mathf.CeilToInt(menuItems.Count / (float)maxMenuItemsPerPage));
        protected int page;
        public int Page
        {
            get => page;
            set => SetPage(value);
        }
        public void SetPage(int page, bool rebuildUI = true)
        {
            this.page = Mathf.Clamp(page, 0, Pages - 1);
            if (rebuildUI) Rebuild();
        }

        public bool selectOnDeactivate;
        public bool useRightStick;

        [SerializeField]
        protected bool preserveCircleRotation;
        public void SetPreserveCircleRotation(bool preserveCircleRotation, bool rebuildUI = true)
        {
            this.preserveCircleRotation = preserveCircleRotation;
            if (rebuildUI) Rebuild();
        }
        public bool PreserveCircleRotation
        {
            get => preserveCircleRotation;
            set => SetPreserveCircleRotation(value);
        }

        [SerializeField]
        protected float spacing;
        public void SetSpacing(float spacing, bool rebuildUI = true)
        {
            this.spacing = spacing;
            if (rebuildUI) Rebuild();
        }
        public float Spacing
        {
            get => spacing;
            set => SetSpacing(value);
        }

        [SerializeField]
        protected float spacingPerItem;
        public void SetSpacingPerItem(float spacingPerItem, bool rebuildUI = true)
        {
            this.spacingPerItem = spacingPerItem;
            if (rebuildUI) Rebuild();
        }
        public float SpacingPerItem
        {
            get => spacingPerItem;
            set => SetSpacingPerItem(value);
        }

        protected virtual void Awake()
        {
            var pointerProx = menuItemPrototype.GetComponentInChildren<PointerEventsProxy>();
            if (pointerProx == null)
            {
                var button = menuItemPrototype.GetComponentInChildren<Button>(true); 
                if (button != null && button.targetGraphic != null)
                {
                    button.targetGraphic.gameObject.AddComponent<PointerEventsProxy>();
                }
            }

            menuItemPool = new GameObject("items").AddOrGetComponent<PrefabPool>();
            menuItemPool.transform.SetParent(this.transform, false);
            menuItemPool.SetContainerTransform(menuItemPool.transform, false, false, true); 
            SetMenuItemPrototype(menuItemPrototype, false);
            menuItemPrototype.gameObject.SetActive(false);

            menuItemIconPool = new GameObject("icons").AddOrGetComponent<PrefabPool>();
            menuItemIconPool.transform.SetParent(this.transform, false);
            menuItemIconPool.SetContainerTransform(menuItemIconPool.transform, false, false, true);
            SetMenuItemIconPrototype(menuItemIconPrototype, false);
            menuItemIconPrototype.gameObject.SetActive(false);

            Rebuild(); 
        }

        protected virtual void OnEnable()
        {
            OnActivate?.Invoke();
        }
        protected virtual void OnDisable()
        {
            if (selectOnDeactivate)
            {
                if (currentStickHighlight != null)
                {
                    Select(currentStickHighlight);
                }
            }

            OnDeactivate?.Invoke();
        }

        protected virtual void Select(MenuItem item)
        {
            currentStickHighlight = null;

            item.onSelect?.Invoke();
            OnSelectDisplay?.Invoke(item.displayName);
            OnSelectId?.Invoke(item.id);
            OnSelectItem?.Invoke(item);
        }

        protected MenuItem prevHighlight;
        protected MenuItem currentHighlight;
        protected MenuItem currentStickHighlight;
        protected virtual void Update()
        {
            var eventSystem = EventSystem.current;

            bool selectionFlag = false;
            var stickHor = useRightStick ? UIInput.MainRightJoystickHorizontal : UIInput.MainLeftJoystickHorizontal;
            var stickVer = useRightStick ? UIInput.MainRightJoystickVertical : UIInput.MainLeftJoystickVertical; 
            if (Mathf.Abs(stickHor) > 0.2f || Mathf.Abs(stickVer) > 0.2f) 
            {
                float angle = Vector2.SignedAngle(new Vector2(0f, 1f), new Vector2(stickHor, -stickVer).normalized) + 180f;
                foreach(var item in menuItems) 
                {
                    if (Mathf.Abs(Maths.GetMinDeltaAngle(angle, item.angleStart)) <= item.angleSize)
                    {
                        selectionFlag = true;

                        if (currentStickHighlight != item)
                        {
                            PointerEventData pointerData = new PointerEventData(eventSystem);
                            if (currentStickHighlight != null && currentStickHighlight.activeObject != null)
                            {                             
                                ExecuteEvents.Execute(currentStickHighlight.activeObject, pointerData, ExecuteEvents.pointerExitHandler);
                            }

                            currentStickHighlight = item;

                            if (currentStickHighlight != null && currentStickHighlight.activeObject != null)
                            {
                                ExecuteEvents.Execute(currentStickHighlight.activeObject, pointerData, ExecuteEvents.pointerEnterHandler);
                            }
                        }

                        break;
                    }
                }
            } 
            
            if (!selectionFlag)
            {
                if (currentStickHighlight != null)
                {
                    PointerEventData pointerData = new PointerEventData(eventSystem);
                    if (currentStickHighlight.activeObject != null)
                    {
                        ExecuteEvents.Execute(currentStickHighlight.activeObject, pointerData, ExecuteEvents.pointerExitHandler);
                    }

                    currentStickHighlight = null; 
                }
            }

            if (prevHighlight != currentHighlight)
            {
                if (selectionText != null) selectionText.text = currentHighlight == null ? string.Empty : currentHighlight.displayName;
                if (selectionTextTMP != null) selectionTextTMP.text = currentHighlight == null ? string.Empty : currentHighlight.displayName; 

                prevHighlight = currentHighlight;
            }
        }

        public void Rebuild()
        {

            foreach(var obj in activeMenuItemObjects)
            {
                if (obj == null) continue;
                menuItemPool.Release(obj.gameObject);
            }
            activeMenuItemObjects.Clear();
            foreach (var obj in activeMenuItemIconObjects)
            {
                if (obj == null) continue;
                menuItemIconPool.Release(obj.gameObject);
            }
            activeMenuItemObjects.Clear();

            int pageCount = Pages;
            if (page >= pageCount) page = pageCount - 1;
            int itemStartIndex = page * maxMenuItemsPerPage;
            int itemsOnThisPage = Mathf.Clamp(menuItems.Count - itemStartIndex, 2, maxMenuItemsPerPage);
            int itemEndIndex = itemStartIndex + itemsOnThisPage;
            float angleStep = 360f / itemsOnThisPage;
            float angleStepHalf = angleStep * 0.5f;
            float sizeAngle = (180f - angleStep);
            float sizeAngleHalf = sizeAngle * 0.5f;
            float iconPush = iconPushPerItem * (itemsOnThisPage - 2);
            float localSpacing = (spacingPerItem * (itemsOnThisPage - 2)) + spacing; 
            for (int i = itemStartIndex; i < itemEndIndex; i++)
            {
                var item = i >= menuItems.Count ? null : menuItems[i];
                int itemIndexOnPage = i - itemStartIndex;

                if (!menuItemPool.TryGetNewInstance(out GameObject obj)) continue;
                if (!menuItemIconPool.TryGetNewInstance(out GameObject iconObj)) 
                {
                    menuItemPool.Release(obj);
                    continue;
                }

                var rectTransform = obj.GetComponent<RectTransform>();
                if (rectTransform == null)
                {
                    menuItemPool.Release(obj);
                    continue;
                }
                var rectTransformIcon = iconObj.GetComponent<RectTransform>();
                if (rectTransformIcon == null)
                {
                    menuItemPool.Release(obj);
                    menuItemPool.Release(iconObj);
                    continue;
                }

                float angle = angleStep * itemIndexOnPage;

                var rectTransfromSizeMask = rectTransform.FindDeepChildLiberal(menuItemObjectSizeMaskName) as RectTransform;
                if (rectTransfromSizeMask != null) rectTransfromSizeMask.localRotation = Quaternion.Euler(0f, 0f, -sizeAngle);

                if (preserveCircleRotation)
                {
                    var rectTransformCircle = rectTransform.FindDeepChildLiberal(menuItemObjectCircleName) as RectTransform;
                    if (rectTransformCircle != null)
                    {
                        rectTransformCircle.localRotation = Quaternion.Euler(0f, 0f, -angle); 
                    }
                }

                var rot = Quaternion.Euler(0f, 0f, angle);
                rectTransform.localRotation = rot * Quaternion.Euler(0f, 0f, sizeAngleHalf);
                rectTransformIcon.localRotation = rot;

                var pos = rot * Vector2.up * localSpacing;
                rectTransform.anchoredPosition = pos;
                rectTransformIcon.anchoredPosition = pos; 

                activeMenuItemObjects.Add(rectTransform);
                activeMenuItemIconObjects.Add(rectTransformIcon);

                var iconImage = iconObj.GetComponentInChildren<UnityEngine.UI.Image>();
                if (iconImage != null)
                {
                    iconImage.sprite = item == null ? null : item.icon;
                    iconImage.raycastTarget = false;

                    var imgRT = iconImage.GetComponent<RectTransform>();
                    if (imgRT != null)
                    {
                        imgRT.anchoredPosition = Vector2.up * (baseIconPush + iconPush); 
                    }

                    iconImage.enabled = item != null && item.icon != null; 
                }

                var button = obj.GetComponentInChildren<UnityEngine.UI.Button>();
                if (button != null)
                {
                    if (item == null)
                    {
                        button.interactable = false;
                    }
                    else 
                    {
                        button.interactable = true;
                    }
                } 

                var pointerProx = (button == null ? obj : button.gameObject).AddOrGetComponent<PointerEventsProxy>(); 
                if (pointerProx != null)
                {
                    if (pointerProx.OnEnter == null) pointerProx.OnEnter = new UnityEvent();
                    pointerProx.OnEnter.RemoveAllListeners();

                    if (pointerProx.OnExit == null) pointerProx.OnExit = new UnityEvent();
                    pointerProx.OnExit.RemoveAllListeners();

                    if (pointerProx.OnClick == null) pointerProx.OnClick = new UnityEvent();
                    pointerProx.OnClick.RemoveAllListeners();

                    if (item != null)
                    {
                        pointerProx.OnEnter.AddListener(() =>
                        {
                            currentHighlight = item;

                            item.onHighlight?.Invoke();
                            OnHighlightDisplay?.Invoke(item.displayName); 
                            OnHighlightId?.Invoke(item.id);
                            OnHighlightItem?.Invoke(item);
                        });

                        pointerProx.OnExit.AddListener(() =>
                        {
                            if (currentHighlight == item) currentHighlight = null;

                            item.onHighlightEnd?.Invoke();
                            OnHighlightEndId?.Invoke(item.id);
                            OnHighlightEndItem?.Invoke(item);
                        });

                        pointerProx.OnClick.AddListener(() =>
                        {
                            Select(item);
                        });
                    }
                }

                if (item != null)
                {
                    item.activeObject = pointerProx.gameObject;
                    item.angleStart = angle;
                    item.angleSize = angleStepHalf;
                }
            }

        }

        #region Events

        [Header("Events")]
        public UnityEvent OnActivate;
        public UnityEvent OnDeactivate;

        public UnityEvent<string> OnHighlightDisplay;
        public UnityEvent<string> OnSelectDisplay;

        public UnityEvent<string> OnHighlightId;
        public UnityEvent<string> OnHighlightEndId;
        public UnityEvent<string> OnSelectId;

        public UnityEvent<MenuItem> OnHighlightItem;
        public UnityEvent<MenuItem> OnHighlightEndItem;
        public UnityEvent<MenuItem> OnSelectItem;

        #endregion

    }

}

#endif