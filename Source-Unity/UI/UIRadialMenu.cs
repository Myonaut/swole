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

#if UNITY_EDITOR
        [SerializeField, TextArea(12, 24)]
#endif
        protected string setupInstructions =
            "MENU ITEM PROTOTYPE (required):\n" +
            "- A RectTransform object, used as a template for each wedge. It is deactivated at runtime and instantiated through a pool.\n" +
            "- Should contain a Button (its Target Graphic receives a PointerEventsProxy automatically) to handle hover/click.\n" +
            "\n" +
            "WEDGE MASKING:\n" +
            "Each wedge is the intersection of two 180 degree (half-plane) masks, so it works for any item count:\n" +
            "1. WHAT THE 'HALF PLANE' IS: a plain rectangle (the default square UI Image works) used as a Mask. Its pivot is X = 0.5, Y = 0 (bottom center) and its height is about the radius of the circle (width should be at least the circle's diameter). It sits exactly on the circle's center, so only the part of the circle on one side of its flat bottom edge, i.e. a half of the circle, is revealed, and rotating the rectangle swings it around the circle's center.\n" +
            "2. TWO MASKS MAKE A WEDGE: the prototype root is the first half-plane mask and a child named after 'Menu Item Object Size Mask Name' (default 'size') is the second, nested inside the first. For N items on a page, angleStep = 360 / N and sizeAngle = 180 - angleStep. The menu rotates the root by (itemAngle + sizeAngle/2) and the 'size' child locally by -sizeAngle. The overlap of the two half-planes is a wedge exactly angleStep degrees wide, so it works for any item count.\n" +
            "3. The visible graphic (the colored circle/ring and the Button's Target Graphic) must be a descendant of the 'size' mask so both masks clip it. A full circle graphic works; only the wedge-shaped overlap is shown.\n" +
            "4. Optionally, name that graphic's object after 'Menu Item Object Circle Name' (default 'circle'). When Preserve Circle Rotation is on, it is rotated by -itemAngle so gradients/textures on the circle keep a fixed orientation while the masks rotate around it.\n" +
            "5. The mask rectangles (bottom-center pivot) and the circle must all rest on the same point: the menu's center. Rotating them then sweeps around the center. Spacing/Spacing Per Item offsets the wedge outward from the center.\n" +
            "6. Because masks clip raycasts as well as rendering, the Button only receives pointer events inside the wedge.\n" +
            "6b. Optionally, a descendant named after 'Menu Item Object Display Name Name' (default 'name') containing a UI Text or TMP_Text receives the item's display name. After the wedge is rotated, this object's world rotation is reset to the menu's rotation so the text stays upright. Place it under the 'size' mask at the position inside the wedge where the label should appear.\n" +
            "7. AUTOMATIC MASK COMPONENTS: only the Mask components are added automatically (with the mask graphic hidden). If the prototype root or the size child already has a Mask (or RectMask2D), it is left alone. Otherwise a Mask component is added to it, which requires that object to already have a Graphic (e.g. an Image with the default square sprite, pivot (0.5, 0)). Graphics and the size child are NEVER created: if the root/size object has no Graphic, or no child matches 'Menu Item Object Size Mask Name', an error is logged.\n" +
            "\n" +
            "MENU ITEM ICON PROTOTYPE (optional):\n" +
            "- A RectTransform object containing a child UnityEngine.UI.Image that displays the item's sprite. If left empty, no icons are created.\n" +
            "- The Image's anchored position (distance from center) is used as the base icon push; raycast target is disabled automatically.\n" +
            "- Keep its pivot at the menu center so rotating the root places the icon around the circle.\n" +
            "\n" +
            "SELECTION TEXT (optional): assign a UI Text and/or TMP_Text to display the highlighted item's name.";

        [Tooltip("The items displayed in the menu. Items beyond Max Menu Items Per Page are placed on additional pages.")]
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
                menuItem.onHighlightEnd.AddListener(highlightEndListener);
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
        private bool hasDisplayNameOffset;
        private Vector3 displayNameOffset;
        [Tooltip("Required. Template wedge object (RectTransform) with a Button, a 'size' child, and optionally a 'circle' child. Deactivated at runtime.")]
        [SerializeField]
        protected RectTransform menuItemPrototype;
        protected virtual bool EnsureMask(Transform target, string description)
        {
            if (target.GetComponent<Mask>() != null || target.GetComponent<RectMask2D>() != null) return true;

            if (target.GetComponent<Graphic>() == null)
            {
                Debug.LogError($"[{nameof(UIRadialMenu)}] {description} '{target.name}' has no Graphic (e.g. Image) to use as a mask shape. Add a half-plane graphic to it.", target);
                return false;
            }

            var mask = target.gameObject.AddComponent<Mask>(); 
            mask.showMaskGraphic = false;
            return true;
        }

        protected virtual void EnsureMenuItemMasks(RectTransform prototype)
        {
            if (prototype == null) return;

            EnsureMask(prototype, "Menu item prototype root");

            var sizeMask = prototype.FindDeepChildLiberal(menuItemObjectSizeMaskName);
            if (sizeMask == null)
            {
                Debug.LogError($"[{nameof(UIRadialMenu)}] Menu item prototype '{prototype.name}' has no child named '{menuItemObjectSizeMaskName}' to use as the size mask.", prototype);
                return;
            }

            EnsureMask(sizeMask, "Size mask object");
        }

        public void SetMenuItemPrototype(RectTransform menuItemPrototype, bool rebuildUI = true)
        {
            this.menuItemPrototype = menuItemPrototype;
            if (menuItemPrototype == null) return;

            EnsureMenuItemMasks(menuItemPrototype);

            var prototypeDisplayName = menuItemPrototype.FindDeepChildLiberal(menuItemObjectDisplayNameName);
            hasDisplayNameOffset = prototypeDisplayName != null;
            if (hasDisplayNameOffset)
            {
                displayNameOffset = this.transform.InverseTransformVector(prototypeDisplayName.position - menuItemPrototype.position);
            }

            if (menuItemPool != null) 
            { 
                menuItemPool.Reinitialize(menuItemPrototype.gameObject, PoolGrowthMethod.Incremental, 1, 8, 64); 
            }

            if (rebuildUI) Rebuild();
        }
        protected List<RectTransform> activeMenuItemObjects = new List<RectTransform>();

        [Tooltip("Name of the child object in the menu item prototype that is rotated to size the wedge.")]
        public string menuItemObjectSizeMaskName = "size";
        [Tooltip("Name of the child object in the menu item prototype that is counter-rotated when Preserve Circle Rotation is enabled.")]
        public string menuItemObjectCircleName = "circle";
        [Tooltip("Optional. Name of a child object in the menu item prototype containing a UI Text or TMP_Text that displays the item's display name. It is kept upright after the wedge is rotated.")]
        public string menuItemObjectDisplayNameName = "name";


        protected PrefabPool menuItemIconPool;
        [Tooltip("Optional. Template icon object (RectTransform) containing a child Image. If empty, no icons are created.")]
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

        [Tooltip("Optional. UI Text that displays the highlighted item's display name.")]
        public UnityEngine.UI.Text selectionText;
        [Tooltip("Optional. TextMeshPro text that displays the highlighted item's display name.")]
        public TMPro.TMP_Text selectionTextTMP;

        [Tooltip("Extra distance icons are pushed outward for each item on the page beyond 2.")]
        public float iconPushPerItem;
        private float baseIconPush;

        [Tooltip("Maximum number of items shown at once; additional items go to further pages.")]
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

        [Tooltip("If true, the currently stick-highlighted item is selected when this menu is disabled.")]
        public bool selectOnDeactivate;
        [Tooltip("If true, the right joystick selects items; otherwise the left joystick is used.")]
        public bool useRightStick;
        [Tooltip("Invert the horizontal axis of the stick/cursor input used to pick a wedge.")]
        public bool invertHorizontalInput;
        [Tooltip("Invert the vertical axis of the stick/cursor input used to pick a wedge.")]
        public bool invertVerticalInput;

        [Tooltip("If true, the 'circle' child of each wedge is counter-rotated so its graphic keeps a fixed orientation.")]
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

        [Tooltip("Base distance wedges and icons are offset from the menu center.")]
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

        [Tooltip("Additional offset from center for each item on the page beyond 2.")]
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

            if (menuItemIconPrototype != null)
            {
                menuItemIconPool = new GameObject("icons").AddOrGetComponent<PrefabPool>();
                menuItemIconPool.transform.SetParent(this.transform, false);
                menuItemIconPool.SetContainerTransform(menuItemIconPool.transform, false, false, true);
                SetMenuItemIconPrototype(menuItemIconPrototype, false);
                menuItemIconPrototype.gameObject.SetActive(false);
            }

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
        protected float cursorX;
        protected float cursorY;
        protected virtual void Update()
        {
            var eventSystem = EventSystem.current; 

            bool selectionFlag = false;
            var stickHor = useRightStick ? UIInput.MainRightJoystickHorizontal : UIInput.MainLeftJoystickHorizontal;
            var stickVer = useRightStick ? UIInput.MainRightJoystickVertical : UIInput.MainLeftJoystickVertical; 
            if (Mathf.Abs(stickHor) <= 0.2f && Mathf.Abs(stickVer) <= 0.2f)
            {
                cursorX = Mathf.Clamp(cursorX + UIInput.CursorHorizontal, -1f, 1f);
                cursorY = Mathf.Clamp(cursorY + UIInput.CursorVertical, -1f, 1f); 

                stickHor = cursorX;
                stickVer = cursorY; 
            }
            if (Mathf.Abs(stickHor) > 0.2f || Mathf.Abs(stickVer) > 0.2f) 
            {
                float inputX = invertHorizontalInput ? -stickHor : stickHor;
                float inputY = invertVerticalInput ? -stickVer : stickVer;
                float angle = Vector2.SignedAngle(new Vector2(0f, 1f), new Vector2(inputX, -inputY).normalized) + 180f;
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
                if (obj == null || menuItemIconPool == null) continue;
                menuItemIconPool.Release(obj.gameObject);
            }
            activeMenuItemIconObjects.Clear();

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
                GameObject iconObj = null;
                if (menuItemIconPool != null && !menuItemIconPool.TryGetNewInstance(out iconObj)) 
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
                RectTransform rectTransformIcon = iconObj == null ? null : iconObj.GetComponent<RectTransform>();
                if (iconObj != null && rectTransformIcon == null)
                {
                    menuItemPool.Release(obj);
                    menuItemIconPool.Release(iconObj);
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
                if (rectTransformIcon != null) rectTransformIcon.localRotation = rot;

                var pos = rot * Vector2.up * localSpacing;
                rectTransform.anchoredPosition = pos;
                if (rectTransformIcon != null) rectTransformIcon.anchoredPosition = pos; 

                var displayNameObj = rectTransform.FindDeepChildLiberal(menuItemObjectDisplayNameName);
                if (displayNameObj != null)
                {
                    string displayName = item == null ? string.Empty : item.displayName;

                    var displayNameText = displayNameObj.GetComponentInChildren<UnityEngine.UI.Text>(true);
                    if (displayNameText != null) displayNameText.text = displayName;

                    var displayNameTextTMP = displayNameObj.GetComponentInChildren<TMPro.TMP_Text>(true);
                    if (displayNameTextTMP != null) displayNameTextTMP.text = displayName;

                    if (hasDisplayNameOffset)
                    {
                        displayNameObj.position = rectTransform.position + this.transform.TransformVector(rot * displayNameOffset);
                    }
                    displayNameObj.rotation = this.transform.rotation;
                }

                activeMenuItemObjects.Add(rectTransform);
                if (rectTransformIcon != null) activeMenuItemIconObjects.Add(rectTransformIcon);

                var iconImage = iconObj == null ? null : iconObj.GetComponentInChildren<UnityEngine.UI.Image>();
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