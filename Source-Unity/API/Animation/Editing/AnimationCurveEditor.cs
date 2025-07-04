#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using TMPro;

using Swole.UI;

/*

Standalone dependencies:

- ObjectPool
- PrefabPool
- PooledObject
- UIAnimationCurveRenderer
- UILineRenderer
- UISimpleLine
- UIAnchoredLine
- MouseButtonMask
- UIDraggable
- UILightweightGridRenderer

*/
namespace Swole.API.Unity.Animation
{ 
    public static class AnimationCurveEditorUtils
    { 
        public static void SetParentKeepWorldPosition(this Transform transform, Transform parent)
        {
            var worldPosition = transform.position;
            transform.SetParent(parent, false);
            transform.position = worldPosition;
        }
        public static Vector2 ScreenToCanvasSpace(this Canvas canvas, Vector2 screenPos) => ScreenToCanvasPosition(canvas, screenPos);
        public static Vector2 ScreenToCanvasPosition(this Canvas canvas, Vector2 screenPos) => ScreenToCanvasPosition((RectTransform)canvas.transform, screenPos, canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera);
        public static Vector2 ScreenToCanvasPosition(this RectTransform rectTransform, Vector2 screenPos, Camera camera)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPos, camera, out Vector2 localPos);
            return localPos;
        }

        public static T AddOrGetComponent<T>(this GameObject gameObject) where T : Component
        {
            T comp = gameObject.GetComponent<T>();
            if (comp == null) comp = gameObject.AddComponent<T>();
            return comp;
        }

        public static void SetAllRaycastTargets(Transform rootTransform, bool enabled) => SetAllRaycastTargets(rootTransform == null ? null : rootTransform.gameObject, enabled);
        public static void SetAllRaycastTargets(GameObject rootObject, bool enabled)
        {
            if (rootObject == null) return;

            var graphics = rootObject.GetComponentsInChildren<Graphic>();
            foreach (var graphic in graphics) graphic.raycastTarget = enabled;
        }

        public static void SetComponentText(Transform rootObject, string text, bool includeInactive = true) => SetComponentText(rootObject == null ? null : rootObject.gameObject, text, includeInactive);
        public static void SetComponentText(GameObject rootObject, string text, bool includeInactive = true)
        {
            if (rootObject == null) return;

            Text textUI = rootObject.GetComponentInChildren<Text>(includeInactive);
            if (textUI != null) textUI.text = text;
            TMP_Text textTMP = rootObject.GetComponentInChildren<TMP_Text>(includeInactive);
            if (textTMP != null) textTMP.text = text;
        }

        public static void SetInputFieldText(Transform rootObject, string text, bool includeInactive = true, bool withoutNotify = true) => SetInputFieldText(rootObject == null ? null : rootObject.gameObject, text, includeInactive, withoutNotify);
        public static void SetInputFieldText(GameObject rootObject, string text, bool includeInactive = true, bool withoutNotify = true)
        {
            if (rootObject == null) return;

            InputField input = rootObject.GetComponentInChildren<InputField>(includeInactive);
            if (input != null) 
            {
                if (withoutNotify)
                {
                    input.SetTextWithoutNotify(text);
                }
                else
                {
                    input.text = text;
                }
            }
            TMP_InputField tmpInput = rootObject.GetComponentInChildren<TMP_InputField>(includeInactive);
            if (tmpInput != null) 
            {
                if (withoutNotify)
                {
                    tmpInput.SetTextWithoutNotify(text);
                }
                else
                {
                    tmpInput.text = text;
                }
            }
        }

        public static void SetInputFieldOnValueChangeAction(GameObject rootObject, UnityAction<string> action, bool includeInactive = true, bool removeAllListeners = true) => SetInputFieldOnValueChangeAction(rootObject == null ? null : rootObject.transform, action, includeInactive, removeAllListeners);
        public static void SetInputFieldOnValueChangeAction(Transform rootObject, UnityAction<string> action, bool includeInactive = true, bool removeAllListeners = true)
        {
            if (rootObject == null) return;

            InputField input = rootObject.GetComponentInChildren<InputField>(includeInactive);
            if (input != null)
            {
                if (input.onValueChanged == null) input.onValueChanged = new InputField.OnChangeEvent(); else if (removeAllListeners) input.onValueChanged.RemoveAllListeners();
                if (action != null) input.onValueChanged.AddListener(action);
            }
            TMP_InputField tmpInput = rootObject.GetComponentInChildren<TMP_InputField>(includeInactive);
            if (tmpInput != null)
            {
                if (tmpInput.onValueChanged == null) tmpInput.onValueChanged = new TMP_InputField.OnChangeEvent(); else if (removeAllListeners) tmpInput.onValueChanged.RemoveAllListeners();
                if (action != null) tmpInput.onValueChanged.AddListener(action);
            }
        }

        public static string GetInputFieldText(Transform rootObject, bool includeInactive = true) => GetInputFieldText(rootObject == null ? null : rootObject.gameObject, includeInactive);
        public static string GetInputFieldText(GameObject rootObject, bool includeInactive = true)
        {
            if (rootObject == null) return string.Empty;

            TMP_InputField tmpInput = rootObject.GetComponentInChildren<TMP_InputField>(includeInactive);
            if (tmpInput != null) return tmpInput.text;
            InputField input = rootObject.GetComponentInChildren<InputField>(includeInactive);
            if (input != null) return input.text;

            return string.Empty;
        }

        public static void SetButtonOnClickAction(Transform rootObject, UnityAction action, bool includeInactive = true, bool removeAllListeners = true) => SetButtonOnClickAction(rootObject == null ? null : rootObject.gameObject, action, includeInactive, removeAllListeners);
        public static void SetButtonOnClickAction(GameObject rootObject, UnityAction action, bool includeInactive = true, bool removeAllListeners = true)
        {
            if (rootObject == null) return;

            var button = rootObject.GetComponentInChildren<Button>(includeInactive);
            if (button != null)
            {
                button.gameObject.SetActive(true);
                if (button.onClick == null) button.onClick = new Button.ButtonClickedEvent(); else if (removeAllListeners) button.onClick.RemoveAllListeners(); 
                if (action != null) button.onClick.AddListener(action);
            }
        }

        public static void SetDropdownOptions(GameObject rootObject, List<TMP_Dropdown.OptionData> options, bool includeInactive = true) => SetDropdownOptions(rootObject == null ? null : rootObject.transform, options, includeInactive);
        public static void SetDropdownOptions(Transform rootObject, List<TMP_Dropdown.OptionData> options, bool includeInactive = true)
        {
            if (rootObject == null) return;

            Dropdown dropdown = rootObject.GetComponentInChildren<Dropdown>(includeInactive);
            if (dropdown != null)
            {
                var newList = new List<Dropdown.OptionData>();
                for (int a = 0; a < options.Count; a++) newList.Add(new Dropdown.OptionData(options[a].text, options[a].image));
                dropdown.options = newList;
            }
            TMP_Dropdown tmpDropdown = rootObject.GetComponentInChildren<TMP_Dropdown>(includeInactive);
            if (tmpDropdown != null)
            {
                tmpDropdown.options = options;
            }
        }

        public static void SetSelectedDropdownOption(GameObject rootObject, int optionIndex, bool includeInactive = true, bool withoutNotify = true) => SetSelectedDropdownOption(rootObject == null ? null : rootObject.transform, optionIndex, includeInactive, withoutNotify);
        public static void SetSelectedDropdownOption(Transform rootObject, int optionIndex, bool includeInactive = true, bool withoutNotify = true)
        {
            if (rootObject == null) return;

            Dropdown dropdown = rootObject.GetComponentInChildren<Dropdown>(includeInactive);
            if (dropdown != null)
            {
                if (withoutNotify)
                {
                    dropdown.SetValueWithoutNotify(optionIndex);
                } 
                else
                {
                    dropdown.value = optionIndex;
                }
            }
            TMP_Dropdown tmpDropdown = rootObject.GetComponentInChildren<TMP_Dropdown>(includeInactive);
            if (tmpDropdown != null)
            {
                if (withoutNotify)
                {
                    tmpDropdown.SetValueWithoutNotify(optionIndex);
                }
                else
                {
                    tmpDropdown.value = optionIndex;
                }
            }
        }

        public static List<TMP_Dropdown.OptionData> GetDropdownOptions(GameObject rootObject, List<TMP_Dropdown.OptionData> optionsList = null, bool includeInactive = true) => GetDropdownOptions(rootObject == null ? null : rootObject.transform, optionsList, includeInactive);
        public static List<TMP_Dropdown.OptionData> GetDropdownOptions(Transform rootObject, List<TMP_Dropdown.OptionData> optionsList = null, bool includeInactive = true)
        {
            if (optionsList == null) optionsList = new List<TMP_Dropdown.OptionData>();
            if (rootObject == null) return optionsList;

            TMP_Dropdown tmpDropdown = rootObject.GetComponentInChildren<TMP_Dropdown>(includeInactive);
            if (tmpDropdown != null)
            {
                optionsList.AddRange(tmpDropdown.options);
                return optionsList;
            }
            Dropdown dropdown = rootObject.GetComponentInChildren<Dropdown>(includeInactive);
            if (dropdown != null)
            {
                var options = dropdown.options;
                for (int a = 0; a < options.Count; a++) optionsList.Add(new TMP_Dropdown.OptionData(options[a].text, options[a].image));
            }

            return optionsList;
        }

        public static void SetDropdownOnValueChangeAction(GameObject rootObject, UnityAction<int, string> action, bool includeInactive = true, bool removeAllListeners = true) => SetDropdownOnValueChangeAction(rootObject == null ? null : rootObject.transform, action, includeInactive, removeAllListeners);
        public static void SetDropdownOnValueChangeAction(Transform rootObject, UnityAction<int, string> action, bool includeInactive = true, bool removeAllListeners = true)
        {
            if (rootObject == null) return;

            TMP_Dropdown tmpDropdown = rootObject.GetComponentInChildren<TMP_Dropdown>(includeInactive);
            if (tmpDropdown != null)
            {
                if (tmpDropdown.onValueChanged == null) tmpDropdown.onValueChanged = new TMP_Dropdown.DropdownEvent(); else if (removeAllListeners) tmpDropdown.onValueChanged.RemoveAllListeners();
                if (action != null) tmpDropdown.onValueChanged.AddListener((int choice) => action(choice, tmpDropdown.options[choice].text));
            }
            Dropdown dropdown = rootObject.GetComponentInChildren<Dropdown>(includeInactive);
            if (dropdown != null)
            {
                if (dropdown.onValueChanged == null) dropdown.onValueChanged = new Dropdown.DropdownEvent(); else if (removeAllListeners) dropdown.onValueChanged.RemoveAllListeners();
                if (action != null) dropdown.onValueChanged.AddListener((int choice) => action(choice, dropdown.options[choice].text));
            }
        }

        public static void SetDropdownOptionChooseAction(GameObject rootObject, string optionName, UnityAction action, bool includeInactive = true, bool removeAllListeners = false) => SetDropdownOptionChooseAction(rootObject == null ? null : rootObject.transform, optionName, action, includeInactive, removeAllListeners);
        public static void SetDropdownOptionChooseAction(Transform rootObject, string optionName, UnityAction action, bool includeInactive = true, bool removeAllListeners = false)
        {
            if (rootObject == null) return;

            optionName = optionName.Trim().ToLower();

            TMP_Dropdown tmpDropdown = rootObject.GetComponentInChildren<TMP_Dropdown>(includeInactive);
            if (tmpDropdown != null)
            {
                if (tmpDropdown.onValueChanged == null) tmpDropdown.onValueChanged = new TMP_Dropdown.DropdownEvent(); else if (removeAllListeners) tmpDropdown.onValueChanged.RemoveAllListeners();
                if (action != null) tmpDropdown.onValueChanged.AddListener((int choice) => { if (tmpDropdown.options[choice].text.Trim().ToLower() == optionName) action(); });
            }
            Dropdown dropdown = rootObject.GetComponentInChildren<Dropdown>(includeInactive);
            if (dropdown != null)
            {
                if (dropdown.onValueChanged == null) dropdown.onValueChanged = new Dropdown.DropdownEvent(); else if (removeAllListeners) dropdown.onValueChanged.RemoveAllListeners();
                if (action != null) dropdown.onValueChanged.AddListener((int choice) => { if (dropdown.options[choice].text.Trim().ToLower() == optionName) action(); });
            }
        }
        public static void SetDropdownOptionChooseAction(GameObject rootObject, int option, UnityAction action, bool includeInactive = true, bool removeAllListeners = false) => SetDropdownOptionChooseAction(rootObject == null ? null : rootObject.transform, option, action, includeInactive, removeAllListeners);
        public static void SetDropdownOptionChooseAction(Transform rootObject, int option, UnityAction action, bool includeInactive = true, bool removeAllListeners = false)
        {
            if (rootObject == null) return;

            TMP_Dropdown tmpDropdown = rootObject.GetComponentInChildren<TMP_Dropdown>(includeInactive);
            if (tmpDropdown != null)
            {
                if (tmpDropdown.onValueChanged == null) tmpDropdown.onValueChanged = new TMP_Dropdown.DropdownEvent(); else if (removeAllListeners) tmpDropdown.onValueChanged.RemoveAllListeners();
                if (action != null) tmpDropdown.onValueChanged.AddListener((int choice) => { if (choice == option) action(); });
            }
            Dropdown dropdown = rootObject.GetComponentInChildren<Dropdown>(includeInactive);
            if (dropdown != null)
            {
                if (dropdown.onValueChanged == null) dropdown.onValueChanged = new Dropdown.DropdownEvent(); else if (removeAllListeners) dropdown.onValueChanged.RemoveAllListeners();
                if (action != null) dropdown.onValueChanged.AddListener((int choice) => { if (choice == option) action(); });
            }
        }

        public static void SetToggleValue(GameObject rootObject, bool value, bool includeInactive = true, bool withoutNotify = true) => SetToggleValue(rootObject == null ? null : rootObject.transform, value, includeInactive, withoutNotify);
        public static void SetToggleValue(Transform rootObject, bool value, bool includeInactive = true, bool withoutNotify = true)
        {
            if (rootObject == null) return;

            Toggle toggle = rootObject.GetComponentInChildren<Toggle>(includeInactive);
            if (toggle != null)
            {
                if (withoutNotify)
                {
                    toggle.SetIsOnWithoutNotify(value);
                } 
                else
                {
                    toggle.isOn = value;
                }
            }
        }

        public static void SetToggleOnValueChangeAction(GameObject rootObject, UnityAction<bool> action, bool includeInactive = true, bool removeAllListeners = true) => SetToggleOnValueChangeAction(rootObject == null ? null : rootObject.transform, action, includeInactive, removeAllListeners);
        public static void SetToggleOnValueChangeAction(Transform rootObject, UnityAction<bool> action, bool includeInactive = true, bool removeAllListeners = true)
        {
            if (rootObject == null) return;

            Toggle toggle = rootObject.GetComponentInChildren<Toggle>(includeInactive);
            if (toggle != null)
            {
                if (toggle.onValueChanged == null) toggle.onValueChanged = new Toggle.ToggleEvent(); else if (removeAllListeners) toggle.onValueChanged.RemoveAllListeners();
                if (action != null) toggle.onValueChanged.AddListener(action);
            }
        }

        public static void SetScrollbarOnValueChangeAction(GameObject rootObject, UnityAction<float> action, bool includeInactive = true, bool removeAllListeners = true) => SetScrollbarOnValueChangeAction(rootObject == null ? null : rootObject.transform, action, includeInactive, removeAllListeners);
        public static void SetScrollbarOnValueChangeAction(Transform rootObject, UnityAction<float> action, bool includeInactive = true, bool removeAllListeners = true)
        {
            if (rootObject == null) return;

            Scrollbar scrollbar = rootObject.GetComponentInChildren<Scrollbar>(includeInactive);
            if (scrollbar != null) SetScrollbarOnValueChangeAction(scrollbar, action, removeAllListeners);
        }
        public static void SetScrollbarOnValueChangeAction(Scrollbar scrollbar, UnityAction<float> action, bool removeAllListeners = true)
        {
            if (scrollbar == null) return;

            if (scrollbar.onValueChanged == null) scrollbar.onValueChanged = new Scrollbar.ScrollEvent(); else if (removeAllListeners) scrollbar.onValueChanged.RemoveAllListeners();
            if (action != null) scrollbar.onValueChanged.AddListener(action);
        }

        public static Transform FindNestedChild(Transform aParent, string aName, bool includeSelf = true)
        {
            aName = aName.ToLower().Trim();
            if (includeSelf) if (aParent.name.ToLower().Trim() == aName) return aParent;

            for (int a = 0; a < aParent.childCount; a++)
            {
                var child = aParent.GetChild(a);
                if (child == null) continue;
                if (child.name.ToLower().Trim() == aName) return child;
            }
            foreach (Transform child in aParent)
            {
                Transform result = FindNestedChild(child, aName, false);
                if (result != null)
                    return result;
            }
            return null;
        }
    }

    public class AnimationCurveEditorInput
    {

        public virtual float ScrollSpeed => 0.3f;

        private bool InputNotSupported() 
        { 
            Debug.LogError($"[{nameof(AnimationCurveEditor)}] Current input system is not supported! Please create a subclass of '{typeof(AnimationCurveEditorInput).FullName}' and override its input related properties to fit your needs; then set '{nameof(AnimationCurveEditor)}.{nameof(AnimationCurveEditor.InputProxy)}' to a new instance of your subclass.");
            return false;
        }
        private float InputValueNotSupported()
        {
            InputNotSupported();
            return 0;
        }
        private Vector2 InputVectorNotSupported()
        {
            InputNotSupported();
            return Vector2.zero;
        }

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER

        public virtual bool IsShiftPressed => InputNotSupported();
        public virtual bool IsCtrlPressed => InputNotSupported();
        public virtual bool IsAltPressed => InputNotSupported();
        public virtual bool IsSpacePressed => InputNotSupported();

        public virtual bool PressedCurveFocusKey => InputNotSupported();
         public virtual bool PressedDeleteKey => InputNotSupported();
         public virtual bool PressedSelectAllDeselectAllKey => InputNotSupported();

        public virtual float Scroll => InputValueNotSupported();

        public virtual Vector3 CursorScreenPosition => InputVectorNotSupported();

#else
        public virtual bool IsShiftPressed => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        public virtual bool IsCtrlPressed => Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);
        public virtual bool IsAltPressed => Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        public virtual bool IsSpacePressed => Input.GetKey(KeyCode.Space);

        public virtual bool PressedCurveFocusKey => Input.GetKeyDown(KeyCode.F);
        public virtual bool PressedDeleteKey => Input.GetKeyDown(KeyCode.Delete);
        public virtual bool PressedSelectAllDeselectAllKey => Input.GetKeyDown(KeyCode.A);

        public virtual float Scroll => Input.mouseScrollDelta.y * ScrollSpeed;

        public virtual Vector3 CursorScreenPosition => Input.mousePosition;
#endif

        protected List<BaseRaycaster> raycasters;
        public virtual List<BaseRaycaster> Raycasters
        {
            get
            {
                if (raycasters == null) UpdateRaycasterList();
                return raycasters;
            }
        }

        public virtual void UpdateRaycasterList()
        {
            if (raycasters == null) raycasters = new List<BaseRaycaster>();

            raycasters.Clear();
            raycasters.AddRange(GameObject.FindObjectsOfType<BaseRaycaster>());
        }

        protected readonly List<GameObject> objectsUnderCursor = new List<GameObject>();
        public virtual List<GameObject> ObjectsUnderCursor => GetObjectsUnderCursor();

        protected readonly List<RaycastResult> raycastResults = new List<RaycastResult>();
        protected int lastQueryFrame;
        public virtual List<GameObject> GetObjectsUnderCursor(List<GameObject> appendList = null, bool forceQuery = false, bool refreshRaycastersList = false)
        {
            int frame = Time.frameCount;

            if ((lastQueryFrame == frame && !forceQuery))
            {
                if (appendList != null)
                {
                    appendList.AddRange(objectsUnderCursor);
                    return appendList;
                }
                return objectsUnderCursor;
            }

            lastQueryFrame = frame;

            objectsUnderCursor.Clear();

            EventSystem system = EventSystem.current;

            if (system == null) return objectsUnderCursor;

            var eventData = new PointerEventData(system) { position = CursorScreenPosition };

            raycastResults.Clear();

            if (refreshRaycastersList)
            { 
                UpdateRaycasterList();
                refreshRaycastersList = false;
            }
            foreach (var raycaster in Raycasters)
            {
                if (raycaster == null)
                {
                    refreshRaycastersList = true; 
                    continue;
                }

                raycaster.Raycast(eventData, raycastResults);
            }
            if (refreshRaycastersList)
            {
                UpdateRaycasterList();
                refreshRaycastersList = false;
            }

            foreach (var result in raycastResults)
            {
                if (result.gameObject != null) objectsUnderCursor.Add(result.gameObject);
            }

            if (appendList != null)
            {
                appendList.AddRange(objectsUnderCursor);
                return appendList;
            }
            return objectsUnderCursor;
        }

    }

    public class AnimationCurveEditor : MonoBehaviour
    {
        protected static AnimationCurveEditorInput inputProxyGlobal = new AnimationCurveEditorInput();
        /// <summary>
        /// The default and fallback input manager for all animation curve editors.
        /// </summary>
        public static AnimationCurveEditorInput InputProxyGlobal
        {
            get
            {
                if (inputProxyGlobal == null) inputProxyGlobal = new AnimationCurveEditorInput();
                return inputProxyGlobal;
            }
            set
            {
                inputProxyGlobal = value;
                if (inputProxyGlobal == null) inputProxyGlobal = new AnimationCurveEditorInput();
            }
        }

        protected AnimationCurveEditorInput inputProxy;
        /// <summary>
        /// The input manager for this editor. Will use the global default if not set.
        /// </summary>
        public virtual AnimationCurveEditorInput InputProxy
        {
            get
            {
                if (inputProxy == null) return InputProxyGlobal;
                return inputProxy;
            }
            set
            {
                inputProxy = value;
            }
        }

        public static float defaultTangentWeight = 1f / 3f;
        public static int maxInstanceCountPerPool = 1024;

        protected static readonly Vector3[] fourCornersArray = new Vector3[4];

        [Serializable]
        public enum TangentMode
        {
            Broken, Auto, Smooth, Flat
        }
        [Serializable]
        public enum BrokenTangentMode
        {
            Free, Linear, Constant
        }

        [Serializable]
        public struct KeyframeTangentSettings
        {
            public TangentMode tangentMode;

            public BrokenTangentMode inTangentMode;
            public BrokenTangentMode outTangentMode;

            public static KeyframeTangentSettings Default = new KeyframeTangentSettings() { tangentMode = TangentMode.Broken, inTangentMode = BrokenTangentMode.Free, outTangentMode = BrokenTangentMode.Free };
        }

        protected static RectTransform GetRectTransform(GameObject instance, UIDraggable draggable)
        {
            RectTransform rt;
            if (draggable == null)
            {
                if (instance == null) return null;
                rt = instance.GetComponent<RectTransform>();
            }
            else
            {
                rt = draggable.Root;
            }
            return rt;
        }
        public static float GetValueInRange(float value, Vector2 range) => GetValueInRange(value, range.x, range.y);
        public static float GetValueInRange(float value, float rangeX, float rangeY) => (rangeX == rangeY) ? 0 : (value - rangeX) / (rangeY - rangeX);
        public static float ValueFromRange(float valueInRange, Vector2 range) => ValueFromRange(valueInRange, range.x, range.y);
        public static float ValueFromRange(float valueInRange, float rangeX, float rangeY) => ((rangeY - rangeX) * valueInRange) + rangeX;

        [Serializable]
        public struct KeyframeState
        {
            public Keyframe data;
            public KeyframeTangentSettings tangentSettings;

            /// <summary>
            /// Used to store a float during animation editing.
            /// </summary>
            [NonSerialized]
            public float tempFloat;

            public static implicit operator KeyframeStateRaw(KeyframeState state) => new KeyframeStateRaw() 
            { 
                time = state.data.time, 
                value = state.data.value, 
                inTangent = state.data.inTangent, 
                outTangent = state.data.outTangent, 
                inWeight = state.data.inWeight, 
                outWeight = state.data.outWeight, 

                weightedMode = (int)state.data.weightedMode,

                tangentMode = (int)state.tangentSettings.tangentMode,

                inTangentMode = (int)state.tangentSettings.inTangentMode,
                outTangentMode = (int)state.tangentSettings.outTangentMode
            };

            public static implicit operator Keyframe(KeyframeState state) => state.data;
            public static implicit operator KeyframeTangentSettings(KeyframeState state) => state.tangentSettings;
            public static implicit operator KeyframeState(Keyframe key) => new KeyframeState() { data = key, tangentSettings = KeyframeTangentSettings.Default };
        }

        [Serializable]
        public struct KeyframeStateRaw
        {
            public float time;
            public float value;
            public float inTangent;
            public float outTangent;
            public float inWeight;
            public float outWeight;

            public int weightedMode;

            public int tangentMode;

            public int inTangentMode;
            public int outTangentMode;

            /// <summary>
            /// Used to store a float during animation editing.
            /// </summary>
            [NonSerialized]
            public float tempFloat;

            public static implicit operator KeyframeState(KeyframeStateRaw state) => new KeyframeState()
            {
                data = state,
                tangentSettings = state
            };

            public static implicit operator Keyframe(KeyframeStateRaw state) => new Keyframe() { time = state.time, value = state.value, inTangent = state.inTangent, outTangent = state.outTangent, inWeight = state.inWeight, outWeight = state.outWeight, weightedMode = (WeightedMode)state.weightedMode };
            public static implicit operator KeyframeTangentSettings(KeyframeStateRaw state) => new KeyframeTangentSettings() { tangentMode = (TangentMode)state.tangentMode, inTangentMode = (BrokenTangentMode)state.inTangentMode, outTangentMode = (BrokenTangentMode)state.outTangentMode };
            public static implicit operator KeyframeStateRaw(Keyframe key) => (KeyframeState)key; 
        }

        [Serializable]
        public class KeyframeData
        {

            public Color baseColor;
            public Color selectedColor;

            public int index;

            public GameObject instance;
            public UIDraggable draggable;
            private RectTransform rectTransform;
            public RectTransform RectTransform
            {
                get
                {
                    if (rectTransform == null) rectTransform = GetRectTransform(instance, draggable);
                    return rectTransform;
                }
            }
            private Image keyImage;
            public Image KeyImage
            {
                get
                {
                    if (keyImage == null) keyImage = instance == null ? null : instance.GetComponentInChildren<Image>();
                    return keyImage;
                }
            }

            public KeyframeState state;
            public Keyframe Key
            {
                get => state.data;
                set => state.data = value;
            }
            public float Time
            {
                get => state.data.time;
                set
                {
                    var data = state.data;
                    data.time = value;
                    state.data = data;
                }
            }
            public float Value
            {
                get => state.data.value;
                set
                {
                    var data = state.data;
                    data.value = value;
                    state.data = data;
                }
            }
            public WeightedMode WeightedMode
            {
                get => state.data.weightedMode;
                set
                {
                    var data = state.data;
                    data.weightedMode = value;
                    state.data = data;
                }
            }
            public float InTangentWeight
            {
                get => state.data.inWeight;
                set
                {
                    var data = state.data;
                    data.inWeight = value;
                    state.data = data;
                }
            }
            public float InTangent
            {
                get => state.data.inTangent;
                set
                {
                    var data = state.data;
                    data.inTangent = value;
                    state.data = data;
                }
            }
            public float OutTangentWeight
            {
                get => state.data.outWeight;
                set
                {
                    var data = state.data;
                    data.outWeight = value;
                    state.data = data;
                }
            }
            public float OutTangent
            {
                get => state.data.outTangent;
                set
                {
                    var data = state.data;
                    data.outTangent = value;
                    state.data = data;
                }
            }

            public KeyframeTangentSettings TangentSettings
            {
                get => state.tangentSettings;
                set => state.tangentSettings = value;
            }
            public TangentMode TangentMode
            {
                get => TangentSettings.tangentMode;
                set
                {
                    var settings = state.tangentSettings;

                    settings.tangentMode = value;

                    switch(value)
                    {
                        case TangentMode.Broken:
                            break;

                        default:

                            settings.inTangentMode = BrokenTangentMode.Free;
                            settings.outTangentMode = BrokenTangentMode.Free;

                            if (InTangentWeight <= 0) InTangentWeight = defaultTangentWeight;
                            if (OutTangentWeight <= 0) OutTangentWeight = defaultTangentWeight;

                            if (float.IsInfinity(InTangent)) InTangent = 0;
                            if (float.IsInfinity(OutTangent)) OutTangent = 0; 

                            break;
                    }

                    state.tangentSettings = settings;
                }
            }
            public BrokenTangentMode InTangentMode
            {
                get => TangentSettings.inTangentMode;
                set
                {
                    var settings = state.tangentSettings;
                    settings.inTangentMode = value;
                    state.tangentSettings = settings;
                }
            }
            public BrokenTangentMode OutTangentMode
            {
                get => TangentSettings.outTangentMode;
                set
                {
                    var settings = state.tangentSettings;
                    settings.outTangentMode = value;
                    state.tangentSettings = settings;
                }
            }

            public GameObject inTangentInstance;
            public UIDraggable inTangentDraggable;
            private RectTransform inTangentRectTransform;
            public RectTransform InTangentRectTransform
            {
                get
                {
                    if (inTangentRectTransform == null) inTangentRectTransform = GetRectTransform(inTangentInstance, inTangentDraggable);
                    return inTangentRectTransform;
                }
            }
            private Image inTangentImage;
            public Image InTangentImage
            {
                get
                {
                    if (inTangentImage == null) inTangentImage = inTangentInstance == null ? null : inTangentInstance.GetComponentInChildren<Image>();
                    return inTangentImage;
                }
            }
            public UIAnchoredLine inTangentLine;

            public GameObject outTangentInstance;
            public UIDraggable outTangentDraggable;
            private RectTransform outTangentRectTransform;
            public RectTransform OutTangentRectTransform
            {
                get
                {
                    if (outTangentRectTransform == null) outTangentRectTransform = GetRectTransform(outTangentInstance, outTangentDraggable);
                    return outTangentRectTransform;
                }
            }
            private Image outTangentImage;
            public Image OutTangentImage
            {
                get
                {
                    if (outTangentImage == null) outTangentImage = outTangentInstance == null ? null : outTangentInstance.GetComponentInChildren<Image>();
                    return outTangentImage;
                }
            }
            public UIAnchoredLine outTangentLine;

            public bool IsUsingInWeight 
            { 
                get => Key.weightedMode == WeightedMode.Both || Key.weightedMode == WeightedMode.In;
                set
                {
                    var key = Key;
                    if (key.weightedMode == WeightedMode.None)
                    {
                        key.weightedMode = value ? WeightedMode.In : WeightedMode.None;
                    }
                    else if (key.weightedMode == WeightedMode.In)
                    {
                        key.weightedMode = value ? WeightedMode.In : WeightedMode.None;
                    }
                    else if (key.weightedMode == WeightedMode.Out)
                    {
                        key.weightedMode = value ? WeightedMode.Both : WeightedMode.Out;
                    }
                    else if (key.weightedMode == WeightedMode.Both)
                    {
                        key.weightedMode = value ? WeightedMode.Both : WeightedMode.Out;
                    }
                    Key = key;
                }
            }
            public bool IsUsingOutWeight 
            { 
                get => Key.weightedMode == WeightedMode.Both || Key.weightedMode == WeightedMode.Out; 
                set
                {
                    var key = Key;
                    if (key.weightedMode == WeightedMode.None)
                    {
                        key.weightedMode = value ? WeightedMode.Out : WeightedMode.None;
                    }
                    else if (key.weightedMode == WeightedMode.In)
                    {
                        key.weightedMode = value ? WeightedMode.Both : WeightedMode.In;
                    }
                    else if (key.weightedMode == WeightedMode.Out)
                    {
                        key.weightedMode = value ? WeightedMode.Out : WeightedMode.None;
                    }
                    else if (key.weightedMode == WeightedMode.Both)
                    {
                        key.weightedMode = value ? WeightedMode.Both : WeightedMode.In;
                    }
                    Key = key;
                }
            }

            public virtual void UpdateInUI(KeyframeData[] keyframeData, RectTransform timelineTransform, Vector2 rangeX, Vector2 rangeY, bool showTangents = true, bool isSelected = false, float unweightedTangentHandleSize = 50, bool onlyUnweightedTangents = false)
            {
                if (timelineTransform == null) return;

                Color color = isSelected ? selectedColor : baseColor;

                float ClampInfinity(float value)
                {
                    if (float.IsPositiveInfinity(value)) return 1;
                    if (float.IsNegativeInfinity(value)) return -1;
                    if (float.IsNaN(value)) return 0;

                    return value;
                }

                timelineTransform.GetWorldCorners(fourCornersArray);
                Vector3 GetUIPosition(float tX, float tY)
                {
                    return Vector3.LerpUnclamped(Vector3.LerpUnclamped(fourCornersArray[0], fourCornersArray[3], tX), Vector3.LerpUnclamped(fourCornersArray[1], fourCornersArray[2], tX), tY);
                }
                void SetUIPosition(RectTransform rt, float tX, float tY)
                {
                    if (rt == null) return;
                    rt.anchorMin = rt.anchorMax = new Vector2(tX, tY);
                    rt.position = GetUIPosition(tX, tY);
                }

                var keyRt = RectTransform;
                if (!onlyUnweightedTangents) 
                {
                    KeyImage.color = color;
                    SetUIPosition(keyRt, GetValueInRange(Time, rangeX), GetValueInRange(Value, rangeY)); 
                }

                if (keyframeData == null || index < 0 || index >= keyframeData.Length) return; 

                if (showTangents && index > 0 && TangentSettings.inTangentMode == BrokenTangentMode.Free) 
                {
                    // Move the in-tangent UI handle
                    var leftNeighbor = keyframeData[index - 1];
                    if (leftNeighbor != null)
                    {
                        inTangentInstance.SetActive(true);
                        InTangentImage.color = color;
                        var rt = InTangentRectTransform;

                        float x;
                        float y;
                        if (IsUsingInWeight)
                        {
                            if (!onlyUnweightedTangents)
                            {
                                x = Mathf.LerpUnclamped(Time, leftNeighbor.Time, Key.inWeight);
                                y = Value + ClampInfinity(Key.inTangent) * (leftNeighbor.Time - Time) * Key.inWeight;

                                SetUIPosition(rt, GetValueInRange(x, rangeX), GetValueInRange(y, rangeY));
                            }
                        } 
                        else
                        {
                            Vector2 dir;
                            if (float.IsInfinity(Key.inTangent) || float.IsNaN(Key.inTangent))
                            {
                                dir = new Vector2(0, float.IsNegativeInfinity(Key.inTangent) ? 1 : -1);
                            } 
                            else
                            {
                                dir = new Vector2(1, -Key.inTangent);
                            }

                            dir = dir.normalized;
                            x = Time + (((leftNeighbor.Time - Time) * dir.x) * defaultTangentWeight);
                            y = Value + ((Mathf.Abs(leftNeighbor.Time - Time) * dir.y) * defaultTangentWeight);

                            Vector3 pos = GetUIPosition(GetValueInRange(x, rangeX), GetValueInRange(y, rangeY));
                            Vector3 offset = pos - keyRt.position;
                            float dist = offset.magnitude;
                            if (dist > 0)
                            {
                                offset = offset / dist;
                                offset = timelineTransform.TransformVector(timelineTransform.InverseTransformDirection(offset) * unweightedTangentHandleSize);
                                pos = keyRt.position + offset;
                                Vector3 localPos = timelineTransform.InverseTransformPoint(pos);
                                rt.anchorMin = rt.anchorMax = CalculateCoordsInTimelineRect(timelineTransform, localPos);
                            }
                            rt.position = pos;
                        }

                        if (inTangentLine != null) 
                        {
                            inTangentLine.gameObject.SetActive(true);
                            inTangentLine.Color = color;
                            inTangentLine.RefreshIfUpdated();
                        }
                    }
                } 
                else
                {
                    // Hide the tangent in the UI when it's not in free mode or when there's no left neighbor
                    if (inTangentInstance != null) inTangentInstance.SetActive(false);
                    if (inTangentLine != null) inTangentLine.gameObject.SetActive(false);
                }

                if (showTangents && index < keyframeData.Length - 1 && TangentSettings.outTangentMode == BrokenTangentMode.Free) 
                {
                    // Move the out-tangent UI handle
                    var rightNeighbor = keyframeData[index + 1];
                    if (rightNeighbor != null)
                    {
                        outTangentInstance.SetActive(true);
                        OutTangentImage.color = color;
                        var rt = OutTangentRectTransform;

                        float x;
                        float y;
                        if (IsUsingOutWeight)
                        {
                            if (!onlyUnweightedTangents)
                            {
                                x = Mathf.LerpUnclamped(Time, rightNeighbor.Time, Key.outWeight);
                                y = Value + ClampInfinity(Key.outTangent) * (rightNeighbor.Time - Time) * Key.outWeight;

                                SetUIPosition(rt, GetValueInRange(x, rangeX), GetValueInRange(y, rangeY));
                            }
                        }
                        else
                        {
                            Vector2 dir;
                            if (float.IsInfinity(Key.outTangent) || float.IsNaN(Key.outTangent))
                            {
                                dir = new Vector2(0, float.IsNegativeInfinity(Key.outTangent) ? -1 : 1);
                            }
                            else
                            {
                                dir = new Vector2(1, Key.outTangent); 
                            }

                            dir = dir.normalized;
                            x = Time + (((rightNeighbor.Time - Time) * dir.x) * defaultTangentWeight);
                            y = Value + ((Mathf.Abs(rightNeighbor.Time - Time) * dir.y) * defaultTangentWeight);

                            Vector3 pos = GetUIPosition(GetValueInRange(x, rangeX), GetValueInRange(y, rangeY)); 
                            Vector3 offset = pos - keyRt.position;
                            float dist = offset.magnitude;
                            if (dist > 0)
                            {
                                offset = offset / dist;
                                offset = timelineTransform.TransformVector(timelineTransform.InverseTransformDirection(offset) * unweightedTangentHandleSize);
                                pos = keyRt.position + offset;
                                Vector3 localPos = timelineTransform.InverseTransformPoint(pos);
                                rt.anchorMin = rt.anchorMax = CalculateCoordsInTimelineRect(timelineTransform, localPos);
                            }
                            rt.position = pos;
                        }

                        if (outTangentLine != null)
                        {
                            outTangentLine.gameObject.SetActive(true);
                            outTangentLine.Color = color;
                            outTangentLine.RefreshIfUpdated();
                        }
                    }
                }
                else
                {
                    // Hide the tangent in the UI when it's not in free mode or when there's no right neighbor
                    if (outTangentInstance != null) outTangentInstance.SetActive(false);
                    if (outTangentLine != null) outTangentLine.gameObject.SetActive(false);
                }
            }

            public virtual void Destroy(PrefabPool keyframePool = null, PrefabPool tangentPool = null, PrefabPool tangentLinePool = null)
            {

                if (instance != null)
                {
                    if (keyframePool == null)
                    {
                        GameObject.Destroy(instance);
                    }
                    else
                    {
                        instance.SetActive(false);
                        keyframePool.Release(instance);
                    }
                }

                if (inTangentInstance != null)
                {
                    if (tangentPool == null)
                    {
                        GameObject.Destroy(inTangentInstance);
                    }
                    else
                    {
                        inTangentInstance.SetActive(false);
                        tangentPool.Release(inTangentInstance);
                    }
                }
                if (outTangentInstance != null)
                {
                    if (tangentPool == null)
                    {
                        GameObject.Destroy(outTangentInstance);
                    }
                    else
                    {
                        inTangentInstance.SetActive(false);
                        tangentPool.Release(outTangentInstance);
                    }
                }

                if (inTangentLine != null)
                {
                    if (tangentLinePool == null)
                    {
                        GameObject.Destroy(inTangentLine.gameObject);
                    }
                    else
                    {
                        inTangentLine.gameObject.SetActive(false);
                        tangentLinePool.Release(inTangentLine.gameObject);
                    }
                }
                if (outTangentLine != null)
                {
                    if (tangentLinePool == null)
                    {
                        GameObject.Destroy(outTangentLine.gameObject);
                    }
                    else
                    {
                        outTangentLine.gameObject.SetActive(false);
                        tangentLinePool.Release(outTangentLine.gameObject);
                    }
                } 

            }
        }

        [SerializeField]
        protected AnimationCurve curve;
        public virtual AnimationCurve Curve
        {
            get => curve;
            set => SetCurve(value);
        }
        public virtual void SetCurve(AnimationCurve curve)
        {
            PrepNewState();

            this.curve = curve;
            CurveRenderer.curve = this.curve;

            if (keyframeData != null)
            {
                foreach (var data in keyframeData)
                {
                    if (data == null) continue;
                    data.Destroy(keyframePool, tangentPool, tangentLinePool);
                }
            }
            selectedKeys.Clear();
            keyframes = null;
            if (curve == null) return;

            keyframes = this.curve.keys;
            if (keyframes != null)
            {
                keyframeData = new KeyframeData[keyframes.Length];

                var tangentSettings = KeyframeTangentSettings.Default;
                tangentSettings.tangentMode = defaultKeyTangentMode;
                for (int a = 0; a < keyframes.Length; a++) keyframeData[a] = CreateNewKeyframeData(keyframes[a], a, tangentSettings);

            }
            else
            {
                keyframeData = new KeyframeData[0];
            }

            FinalizeState();

            rangeX = CurveRangeX;
            rangeY = CurveRangeY;

            Redraw();
        }

        [SerializeField]
        protected UIAnimationCurveRenderer curveRenderer;
        public virtual UIAnimationCurveRenderer CurveRenderer
        {
            get
            {
                if (curveRenderer == null)
                {
                    curveRenderer = gameObject.GetComponent<UIAnimationCurveRenderer>();
                    if (curveRenderer == null)
                    {
                        curveRenderer = new GameObject("curve").AddComponent<UIAnimationCurveRenderer>();
                        curveRenderer.LineThickness = 1;
                    }
                    RectTransform curveRectTransform = AnimationCurveEditorUtils.AddOrGetComponent<RectTransform>(curveRenderer.gameObject);
                    curveRectTransform.SetParent(TimelineTransform, false);
                    curveRectTransform.anchorMin = Vector2.zero;
                    curveRectTransform.anchorMax = Vector2.one;
                    curveRectTransform.anchoredPosition3D  = Vector3.zero;
                    curveRectTransform.sizeDelta = Vector2.zero;

                    curveRenderer.SetRaycastTarget(false);
                    curveRenderer.SetRendererContainer(CurveRendererContainer);

                    curveRenderer.LineColor = curveColor;
                }
                return curveRenderer;
            }
            set
            {
                curveRenderer = value;
                if (curveRenderer != null)
                {
                    if (!(TimelineTransform.gameObject == gameObject && curveRenderer.gameObject == gameObject))
                    {
                        RectTransform curveRectTransform = AnimationCurveEditorUtils.AddOrGetComponent<RectTransform>(curveRenderer.gameObject);
                        curveRectTransform.SetParent(TimelineTransform, false);
                        curveRectTransform.anchorMin = Vector2.zero;
                        curveRectTransform.anchorMax = Vector2.one;
                        curveRectTransform.anchoredPosition3D  = Vector3.zero;
                        curveRectTransform.sizeDelta = Vector2.zero;
                    }
                    curveRenderer.SetRaycastTarget(false);
                    curveRenderer.SetRendererContainer(CurveRendererContainer);

                    curveRenderer.LineColor = curveColor;

                    RebuildCurveMesh();
                }
            }
        }

        #region Settings

        [SerializeField, Header("Settings")]
        protected bool disableNavigation;
        public virtual bool NavigationEnabled
        {
            get => !disableNavigation;
            set => SetNavigation(value);
        }
        [SerializeField]
        protected bool disableInteraction;
        public virtual bool InteractionEnabled
        {
            get => !disableInteraction;
            set => SetInteraction(value);
        }

        public virtual void EnableNavigation() => SetNavigation(true);
        public virtual void DisableNavigation() => SetNavigation(false);
        public virtual void SetNavigation(bool enabled)
        {
            if (scrollbarHorizontal != null) scrollbarHorizontal.interactable = enabled;
            if (scrollbarVertical != null) scrollbarVertical.interactable = enabled;

            Backboard.interactable = enabled;
             
            IEnumerator WaitOneFrame()
            {
                yield return null;
                backboard.enabled = enabled;
            }
            StartCoroutine(WaitOneFrame());

            disableNavigation = !enabled;
        }

        public virtual void EnableInteraction() => SetInteraction(true);
        public virtual void DisableInteraction() => SetInteraction(false);
        public virtual void SetInteraction(bool enabled)
        {
            if (keyframeData != null)
            {
                foreach(var key in keyframeData)
                {
                    if (key == null) continue;

                    if (key.draggable != null) 
                    {
                        key.draggable.interactable = enabled;               
                    }

                    if (key.inTangentDraggable != null)
                    {
                        key.inTangentDraggable.interactable = enabled;
                    }

                    if (key.outTangentDraggable != null)
                    {
                        key.outTangentDraggable.interactable = enabled;
                    }
                }

                IEnumerator WaitOneFrame()
                {
                    yield return null;
                    foreach (var key in keyframeData)
                    {
                        if (key == null) continue;

                        if (key.draggable != null)
                        {
                            key.draggable.enabled = enabled;
                        }

                        if (key.inTangentDraggable != null)
                        {
                            key.inTangentDraggable.enabled = enabled;
                        }

                        if (key.outTangentDraggable != null)
                        {
                            key.outTangentDraggable.enabled = enabled;
                        }
                    }
                }
                StartCoroutine(WaitOneFrame());

            }

            disableInteraction = !enabled;
        }

        [SerializeField, Tooltip("Editor state will update every frame when an element is being dragged if set to true. If you plan on using an undo system, this should be set to false. ")]
        public bool updateStateDuringDragStep;
        [SerializeField]
        protected bool showAllTangents;
        public void SetShowAllTangents(bool show)
        {
            showAllTangents = show;

            if (keyframeData == null) return;
            foreach (var key in keyframeData)
            {
                bool isSelected = IsSelected(key);
                key?.UpdateInUI(keyframeData, TimelineTransform, rangeX, rangeY, showAllTangents || isSelected, isSelected, unweightedTangentHandleSize);
            }
        }
        public bool ShowAllTangents
        {
            get => showAllTangents;
            set => SetShowAllTangents(value);
        }

        [SerializeField, Tooltip("The size in the UI of an unweighted tangent handle")]
        protected float unweightedTangentHandleSize = 50;
        [Tooltip("The amount of padding to apply to the time range that a tangent handle can reside in. Used to prevent a tangent handle from pointing straight up or down, which currently causes a bug.")]
        public float tangentTimeThresholdPadding = 0.001f;

        public string keyCoordinatesStringFormat = "0.#####";

        public float zoomSensitivity = 0.3f;
        public float clickZoomScale = 0.008f;
        public Vector2 curveBoundsPadding = new Vector2(60, 60);
        public Vector2 rangeExtremesX = new Vector2(-100000, 100000);
        public Vector2 rangeExtremesY = new Vector2(-100000, 100000);

        [SerializeField, Range(0.0001f, 1)]
        protected float clampedAutoFalloff = 1/3f;
        public void SetClampedAutoFalloff(float falloff)
        {
            clampedAutoFalloff = Mathf.Clamp01(falloff);
            ReevaluateAllKeys();
        }
        public float ClampedAutoFalloff
        {
            get => clampedAutoFalloff;
            set => SetClampedAutoFalloff(value);
        }
         
        [Tooltip("The tangent mode that all keys will start with when loading an existing Unity AnimationCurve. To save and restore tangent mode states alongside keyframe data, the GetState and SetState methods of this editor should be used instead of working with an AnimationCurve directly.")]
        public TangentMode defaultKeyTangentMode = TangentMode.Broken;
        [Tooltip("The tangent mode that a newly created key will use.")]
        public TangentMode newKeyTangentMode = TangentMode.Auto;

        #endregion

        [SerializeField, Header("Colors")]
        protected Color curveColor = Color.green;
        public void SetCurveColor(Color col)
        {
            curveColor = col;
            if (curveRenderer != null) curveRenderer.SetLineColor(col);
        }
        public Color CurveColor
        {
            get => curveColor;
            set => SetCurveColor(value);
        }
        [SerializeField]
        protected Color gridColor = Color.white;
        public void SetGridColor(Color col)
        {
            gridColor = col;

            if (gridRenderer != null) gridRenderer.SetColor(col);
        }
        public Color GridColor
        {
            get => gridColor;
            set => SetGridColor(value);
        }
        [SerializeField]
        protected Color keyColorBase = Color.green;
        public void SetKeyColorBase(Color col)
        {
            keyColorBase = col;
            if (keyframeData != null)
            {
                foreach(var key in keyframeData)
                {
                    if (key == null) continue;
                    key.baseColor = keyColorBase;
                    key.selectedColor = keyColorSelected;
                    bool isSelected = IsSelected(key);
                    key.UpdateInUI(keyframeData, timelineTransform, rangeX, rangeY, showAllTangents || isSelected, isSelected, unweightedTangentHandleSize);
                }
            }
        }
        public Color KeyColorBase
        {
            get => keyColorBase;
            set => SetKeyColorBase(value);
        }
        [SerializeField]
        protected Color keyColorSelected = Color.white;
        public void SetKeyColorSelected(Color col)
        {
            keyColorSelected = col;
            if (keyframeData != null)
            {
                foreach (var key in keyframeData)
                {
                    if (key == null) continue;
                    key.baseColor = keyColorBase;
                    key.selectedColor = keyColorSelected;
                    bool isSelected = IsSelected(key);
                    key.UpdateInUI(keyframeData, timelineTransform, rangeX, rangeY, showAllTangents || isSelected, isSelected, unweightedTangentHandleSize);
                }
            }
        }
        public Color KeyColorSelected
        {
            get => keyColorSelected;
            set => SetKeyColorSelected(value);
        }

        protected virtual void PurgeKeyframeData()
        {
            if (keyframeData != null)
            {
                foreach (var key in keyframeData) key?.Destroy(keyframePool, tangentPool, tangentLinePool);
                keyframeData = null;
            }
        }
        [SerializeField, Header("Pooling"), Tooltip("The prefab that will be instantiated to represent keyframes in the UI. Looks for a draggable component and subscribes to its events, which will control the editing of keyframes.")]
        protected GameObject keyframePrototype;
        protected PrefabPool keyframePool;
        public void SetKeyframePrototype(GameObject prefab)
        {
            keyframePrototype = prefab;
            if (keyframePool == null)
            {
                keyframePool = AnimationCurveEditorUtils.AddOrGetComponent<PrefabPool>(new GameObject("_keyframePool"));
                keyframePool.transform.SetParent(transform, false);
            }
            keyframePool.SetContainerTransform(KeyframeContainer, false, false, false);
            keyframePool.Reinitialize(keyframePrototype, PoolGrowthMethod.Incremental, 1, 1, maxInstanceCountPerPool);
            keyframePrototype.SetActive(false);

            PurgeKeyframeData();
        }
        public GameObject KeyframePrototype
        {
            get => keyframePrototype;
            set => SetKeyframePrototype(value);
        }

        [SerializeField, Tooltip("The prefab that will be instantiated to represent tangent handle knobs in the UI. Looks for a draggable component and subscribes to its events, which will control the editing of tangents.")]
        protected GameObject tangentPrototype;
        protected PrefabPool tangentPool;
        public void SetTangentPrototype(GameObject prefab)
        {
            tangentPrototype = prefab;
            if (tangentPool == null)
            {
                tangentPool = AnimationCurveEditorUtils.AddOrGetComponent<PrefabPool>(new GameObject("_tangentPool"));
                tangentPool.transform.SetParent(transform, false);
            }
            tangentPool.SetContainerTransform(TangentContainer, false, false, false);
            tangentPool.Reinitialize(tangentPrototype, PoolGrowthMethod.Incremental, 1, 1, maxInstanceCountPerPool);
            tangentPrototype.SetActive(false);

            PurgeKeyframeData();
        }
        public GameObject TangentPrototype
        {
            get => tangentPrototype;
            set => SetTangentPrototype(value);
        }

        [SerializeField, Tooltip("The prefab that will be instantiated to represent tangent handle lines in the UI. Looks for an anchored line component and sets its endpoints accordingly.")]
        protected GameObject tangentLinePrototype;
        protected PrefabPool tangentLinePool;
        public void SetTangentLinePrototype(GameObject prefab)
        {
            tangentLinePrototype = prefab;
            if (tangentLinePool == null)
            {
                tangentLinePool = AnimationCurveEditorUtils.AddOrGetComponent<PrefabPool>(new GameObject("_tangentLinePool"));
                tangentLinePool.transform.SetParent(transform, false);
            }
            tangentLinePool.SetContainerTransform(TangentLineContainer, false, false, false);
            tangentLinePool.Reinitialize(tangentLinePrototype, PoolGrowthMethod.Incremental, 1, 1, maxInstanceCountPerPool);
            tangentLinePrototype.SetActive(false);

            PurgeKeyframeData();
        }
        public GameObject TangentLinePrototype
        {
            get => tangentLinePrototype;
            set => SetTangentLinePrototype(value);
        }

        [SerializeField, Tooltip("The prefab that will be instantiated to display values on the grid. Looks for a text component and changes its text accordingly.")]
        protected GameObject gridValuePrototype;
        protected PrefabPool gridValuePool;
        public void SetGridValuePrototype(GameObject prefab)
        {
            gridValuePrototype = prefab;
            if (gridValuePrototype != null)
            {
                if (gridValuePool == null)
                {
                    gridValuePool = AnimationCurveEditorUtils.AddOrGetComponent<PrefabPool>(new GameObject("_gridValuePool"));
                    gridValuePool.transform.SetParent(transform, false);
                }
                gridValuePool.SetContainerTransform(GridMarkerContainer, false, false, false); 
                gridValuePool.Reinitialize(gridValuePrototype, PoolGrowthMethod.Incremental, 1, 1, maxInstanceCountPerPool);
                gridValuePrototype.SetActive(false);
            } 
            else if (gridValuePool != null)
            {
                gridValuePool.dontDestroyInstances = false;
                Destroy(gridValuePool.gameObject);
            }
        }
        public GameObject GridValuePrototype
        {
            get => gridValuePrototype;
            set => SetGridValuePrototype(value);
        }

        [SerializeField, Header("Containers"), Tooltip("(Best to leave empty) A transform for storing the curve renderer elements. Used for sorting in the UI. Will be auto-created if left empty.")]
        protected RectTransform curveRendererContainer;
        public RectTransform CurveRendererContainer
        {
            get
            {
                if (curveRendererContainer == null)
                {
                    curveRendererContainer = AnimationCurveEditorUtils.AddOrGetComponent<RectTransform>(new GameObject("curveRenderers"));
                    curveRendererContainer.SetParent(TimelineTransform, false);
                    curveRendererContainer.SetSiblingIndex(PostGridContainer.GetSiblingIndex() + 1);
                    curveRendererContainer.anchorMin = Vector2.zero;
                    curveRendererContainer.anchorMax = Vector2.one;
                    curveRendererContainer.anchoredPosition3D  = Vector3.zero;
                    curveRendererContainer.sizeDelta = Vector2.zero;

                    CurveRenderer.SetRendererContainer(curveRendererContainer);
                }
                return curveRendererContainer;
            }
            set
            {
                curveRendererContainer = value;
                if (curveRendererContainer != null)
                {
                    if (!(TimelineTransform.gameObject == gameObject && curveRendererContainer.gameObject == gameObject))
                    {
                        curveRendererContainer.SetParent(TimelineTransform, false);
                        curveRendererContainer.SetSiblingIndex(PostGridContainer.GetSiblingIndex() + 1);
                        curveRendererContainer.anchorMin = Vector2.zero;
                        curveRendererContainer.anchorMax = Vector2.one;
                        curveRendererContainer.anchoredPosition3D  = Vector3.zero;
                        curveRendererContainer.sizeDelta = Vector2.zero;
                    }
                }
                CurveRenderer.SetRendererContainer(curveRendererContainer);
            }
        }

        [SerializeField, Tooltip("(Best to leave empty) A transform for storing the keyframe objects. Used for sorting in the UI. Will be auto-created if left empty.")]
        protected RectTransform keyframeContainer;
        public RectTransform KeyframeContainer
        {
            get
            {
                if (keyframeContainer == null) 
                {
                    keyframeContainer = AnimationCurveEditorUtils.AddOrGetComponent<RectTransform>(new GameObject("keyframes"));
                    keyframeContainer.SetParent(TimelineTransform, false);
                    keyframeContainer.anchorMin = Vector2.zero;
                    keyframeContainer.anchorMax = Vector2.one;
                    keyframeContainer.anchoredPosition3D  = Vector3.zero;
                    keyframeContainer.sizeDelta = Vector2.zero;
                    keyframeContainer.SetSiblingIndex(TangentLineContainer.GetSiblingIndex() + 1);

                    if (keyframePool != null) keyframePool.SetContainerTransform(keyframeContainer, true, true);
                }
                return keyframeContainer;
            }
            set
            {
                keyframeContainer = value;
                if (keyframeContainer != null)
                {
                    if (!(TimelineTransform.gameObject == gameObject && keyframeContainer.gameObject == gameObject))
                    {
                        keyframeContainer.SetParent(TimelineTransform, false);
                        keyframeContainer.anchorMin = Vector2.zero;
                        keyframeContainer.anchorMax = Vector2.one;
                        keyframeContainer.anchoredPosition3D  = Vector3.zero;
                        keyframeContainer.sizeDelta = Vector2.zero;
                        keyframeContainer.SetSiblingIndex(TangentLineContainer.GetSiblingIndex() + 1);
                    }
                }
                if (keyframePool != null) keyframePool.SetContainerTransform(keyframeContainer, true, true);
            }
        }
        [SerializeField, Tooltip("(Best to leave empty) A transform for storing the tangent handle knobs. Used for sorting in the UI. Will be auto-created if left empty.")]
        protected RectTransform tangentContainer;
        public RectTransform TangentContainer
        {
            get
            {
                if (tangentContainer == null)
                {
                    tangentContainer = AnimationCurveEditorUtils.AddOrGetComponent<RectTransform>(new GameObject("tangent knobs"));
                    tangentContainer.SetParent(TimelineTransform, false);
                    tangentContainer.anchorMin = Vector2.zero;
                    tangentContainer.anchorMax = Vector2.one;
                    tangentContainer.anchoredPosition3D  = Vector3.zero;
                    tangentContainer.sizeDelta = Vector2.zero;
                    tangentContainer.SetSiblingIndex(KeyframeContainer.GetSiblingIndex() + 1);

                    if (tangentPool != null) tangentPool.SetContainerTransform(tangentContainer, true, true);
                }
                return tangentContainer;
            }
            set
            {
                tangentContainer = value;
                if (tangentContainer != null)
                {
                    if (!(TimelineTransform.gameObject == gameObject && tangentContainer.gameObject == gameObject))
                    {
                        tangentContainer.SetParent(TimelineTransform, false);
                        tangentContainer.anchorMin = Vector2.zero;
                        tangentContainer.anchorMax = Vector2.one;
                        tangentContainer.anchoredPosition3D  = Vector3.zero;
                        tangentContainer.sizeDelta = Vector2.zero;
                        tangentContainer.SetSiblingIndex(KeyframeContainer.GetSiblingIndex() + 1);
                    }
                }
                if (tangentPool != null) tangentPool.SetContainerTransform(tangentContainer, true, true);
            }
        }
        [SerializeField, Tooltip("(Best to leave empty) A transform for storing the tangent handle lines. Used for sorting in the UI. Will be auto-created if left empty.")]
        protected RectTransform tangentLineContainer;
        public RectTransform TangentLineContainer
        {
            get
            {
                if (tangentLineContainer == null)
                {
                    tangentLineContainer = AnimationCurveEditorUtils.AddOrGetComponent<RectTransform>(new GameObject("tangent lines"));
                    tangentLineContainer.SetParent(TimelineTransform, false);
                    tangentLineContainer.anchorMin = Vector2.zero;
                    tangentLineContainer.anchorMax = Vector2.one;
                    tangentLineContainer.anchoredPosition3D  = Vector3.zero;
                    tangentLineContainer.sizeDelta = Vector2.zero;
                    tangentLineContainer.SetSiblingIndex(CurveRendererContainer.GetSiblingIndex() + 1);

                    if (tangentLinePool != null) tangentLinePool.SetContainerTransform(tangentLineContainer, true, true);
                }
                return tangentLineContainer;
            }
            set
            {
                tangentLineContainer = value;
                if (tangentLineContainer != null)
                {
                    if (!(TimelineTransform.gameObject == gameObject && tangentLineContainer.gameObject == gameObject))
                    {
                        tangentLineContainer.SetParent(TimelineTransform, false);
                        tangentLineContainer.anchorMin = Vector2.zero;
                        tangentLineContainer.anchorMax = Vector2.one;
                        tangentLineContainer.anchoredPosition3D  = Vector3.zero;
                        tangentLineContainer.sizeDelta = Vector2.zero;
                        tangentLineContainer.SetSiblingIndex(CurveRendererContainer.GetSiblingIndex() + 1);
                    }
                }
                if (tangentLinePool != null) tangentLinePool.SetContainerTransform(tangentLineContainer, true, true);
            }
        }

        [SerializeField, Tooltip("(Best to leave empty) A transform for storing the grid value markers. Used for sorting in the UI. Will be auto-created if left empty.")]
        protected RectTransform gridMarkerContainer;
        public RectTransform GridMarkerContainer
        {
            get
            {
                if (gridMarkerContainer == null)
                {
                    gridMarkerContainer = AnimationCurveEditorUtils.AddOrGetComponent<RectTransform>(new GameObject("grid value markers"));
                    gridMarkerContainer.SetParent(TimelineTransform, false);
                    gridMarkerContainer.anchorMin = Vector2.zero;
                    gridMarkerContainer.anchorMax = Vector2.one;
                    gridMarkerContainer.anchoredPosition3D  = Vector3.zero;
                    gridMarkerContainer.sizeDelta = Vector2.zero;
                    if (renderGridMarkersOnTop)
                    {
                        gridMarkerContainer.SetSiblingIndex(PostCurveContainer.GetSiblingIndex() + 1);
                    }
                    else
                    {
                        if (gridRenderer == null)
                        {
                            gridMarkerContainer.SetSiblingIndex(Backboard.transform.GetSiblingIndex() + 1);
                        }
                        else
                        {
                            gridMarkerContainer.SetSiblingIndex(gridRenderer.transform.GetSiblingIndex());
                        }
                    }

                    if (gridValuePool != null) gridValuePool.SetContainerTransform(gridMarkerContainer, true, true);
                }
                return gridMarkerContainer;
            }
            set
            {
                gridMarkerContainer = value;
                if (gridMarkerContainer != null)
                {
                    if (!(TimelineTransform.gameObject == gameObject && gridMarkerContainer.gameObject == gameObject))
                    {
                        gridMarkerContainer.SetParent(TimelineTransform, false);
                        gridMarkerContainer.anchorMin = Vector2.zero;
                        gridMarkerContainer.anchorMax = Vector2.one;
                        gridMarkerContainer.anchoredPosition3D  = Vector3.zero;
                        gridMarkerContainer.sizeDelta = Vector2.zero;
                        if (renderGridMarkersOnTop)
                        {
                            gridMarkerContainer.SetSiblingIndex(PostCurveContainer.GetSiblingIndex() + 1);
                        } 
                        else
                        {
                            if (gridRenderer == null)
                            {
                                gridMarkerContainer.SetSiblingIndex(Backboard.transform.GetSiblingIndex() + 1);
                            }
                            else
                            {
                                gridMarkerContainer.SetSiblingIndex(gridRenderer.transform.GetSiblingIndex());
                            }
                        }

                    }
                }
                if (gridValuePool != null) gridValuePool.SetContainerTransform(gridMarkerContainer, true, true);
            }
        }

        [SerializeField, Tooltip("A transform for sorting UI elements above the grid. Will be auto-created if left empty.")]
        protected RectTransform postGridContainer;
        public RectTransform PostGridContainer
        {
            get
            {
                if (postGridContainer == null)
                {
                    postGridContainer = AnimationCurveEditorUtils.AddOrGetComponent<RectTransform>(new GameObject("post grid"));
                    postGridContainer.SetParent(TimelineTransform, false);
                    postGridContainer.anchorMin = Vector2.zero;
                    postGridContainer.anchorMax = Vector2.one;
                    postGridContainer.anchoredPosition3D  = Vector3.zero;
                    postGridContainer.sizeDelta = Vector2.zero;
                    postGridContainer.SetSiblingIndex(GridMarkerContainer.GetSiblingIndex() + 1);
                }
                return postGridContainer;
            }
            set
            {
                postGridContainer = value;
                if (postGridContainer != null)
                {
                    if (!(TimelineTransform.gameObject == gameObject && postGridContainer.gameObject == gameObject))
                    {
                        postGridContainer.SetParent(TimelineTransform, false);
                        postGridContainer.anchorMin = Vector2.zero;
                        postGridContainer.anchorMax = Vector2.one;
                        postGridContainer.anchoredPosition3D  = Vector3.zero;
                        postGridContainer.sizeDelta = Vector2.zero;
                        postGridContainer.SetSiblingIndex(GridMarkerContainer.GetSiblingIndex() + 1);
                    }
                }
            }
        }

        [SerializeField, Tooltip("A transform for sorting UI elements above the curve. Will be auto-created if left empty.")]
        protected RectTransform postCurveContainer;
        public RectTransform PostCurveContainer
        {
            get
            {
                if (postCurveContainer == null)
                {
                    postCurveContainer = AnimationCurveEditorUtils.AddOrGetComponent<RectTransform>(new GameObject("post grid"));
                    postCurveContainer.SetParent(TimelineTransform, false);
                    postCurveContainer.anchorMin = Vector2.zero;
                    postCurveContainer.anchorMax = Vector2.one;
                    postCurveContainer.anchoredPosition3D  = Vector3.zero;
                    postCurveContainer.sizeDelta = Vector2.zero;
                    postCurveContainer.SetSiblingIndex(TangentContainer.GetSiblingIndex() + 1);
                }
                return postCurveContainer;
            }
            set
            {
                postCurveContainer = value;
                if (postCurveContainer != null)
                {
                    if (!(TimelineTransform.gameObject == gameObject && postCurveContainer.gameObject == gameObject))
                    {
                        postCurveContainer.SetParent(TimelineTransform, false);
                        postCurveContainer.anchorMin = Vector2.zero;
                        postCurveContainer.anchorMax = Vector2.one;
                        postCurveContainer.anchoredPosition3D  = Vector3.zero;
                        postCurveContainer.sizeDelta = Vector2.zero;
                        postCurveContainer.SetSiblingIndex(TangentContainer.GetSiblingIndex() + 1);
                    }
                }
            }
        }


        [SerializeField, Header("Components"), Tooltip("The transform that holds all curve editor UI elements. Will be set to the transform of this game object if left empty.")]
        protected RectTransform timelineTransform; 
        public void SetTimelineTransform(RectTransform transform)
        {
            timelineTransform = transform;
            SortUI();
        }
        public virtual void SortUI()
        {
            Backboard.transform.SetParentKeepWorldPosition(TimelineTransform);
            backboard.transform.SetAsFirstSibling();

            if (gridRenderer != null)
            {
                gridRenderer.transform.SetParentKeepWorldPosition(timelineTransform);
                gridRenderer.transform.SetAsLastSibling();
            }
            if (!renderGridMarkersOnTop)
            {
                GridMarkerContainer.SetParentKeepWorldPosition(timelineTransform);
                gridMarkerContainer.SetAsLastSibling();
            }

            PostGridContainer.SetParentKeepWorldPosition(timelineTransform);
            postGridContainer.SetAsLastSibling();

            CurveRenderer.transform.SetParentKeepWorldPosition(timelineTransform);
            curveRenderer.transform.SetAsLastSibling();
            CurveRendererContainer.SetParentKeepWorldPosition(timelineTransform);
            curveRendererContainer.SetAsLastSibling();

            TangentLineContainer.SetParentKeepWorldPosition(timelineTransform);
            tangentLineContainer.SetAsLastSibling();
            KeyframeContainer.SetParentKeepWorldPosition(timelineTransform);
            keyframeContainer.SetAsLastSibling();
            TangentContainer.SetParentKeepWorldPosition(timelineTransform);
            tangentContainer.SetAsLastSibling();

            PostCurveContainer.SetParentKeepWorldPosition(timelineTransform);
            postCurveContainer.SetAsLastSibling();

            if (renderGridMarkersOnTop) 
            {
                GridMarkerContainer.SetParentKeepWorldPosition(timelineTransform);
                gridMarkerContainer.SetAsLastSibling();
            }

            if (keyDragDisplayTransform != null)
            {
                keyDragDisplayTransform.SetParentKeepWorldPosition(timelineTransform);
                keyDragDisplayTransform.SetAsLastSibling();
            }
            if (boxSelectTransform != null)
            {
                boxSelectTransform.SetParentKeepWorldPosition(timelineTransform);
                boxSelectTransform.SetAsLastSibling();
            }
            if (editKeyMenuTransform != null)
            {
                editKeyMenuTransform.SetParentKeepWorldPosition(timelineTransform);
                editKeyMenuTransform.SetAsLastSibling();
            }
            if (editTangentMenuTransform != null)
            {
                editTangentMenuTransform.SetParentKeepWorldPosition(timelineTransform);
                editTangentMenuTransform.SetAsLastSibling();
            }
            if (contextMenuTransform != null)
            {
                contextMenuTransform.SetParentKeepWorldPosition(timelineTransform);
                contextMenuTransform.SetAsLastSibling();
            }
        }
        public RectTransform TimelineTransform
        {
            get
            {
                if (timelineTransform == null) timelineTransform = AnimationCurveEditorUtils.AddOrGetComponent<RectTransform>(gameObject);
                return timelineTransform;
            }
            set => SetTimelineTransform(value);
        }

        [SerializeField, Tooltip("The transform of an object that will be enabled and resized during a box select action.")]
        protected RectTransform boxSelectTransform;
        public RectTransform BoxSelectTransform
        {
            get => boxSelectTransform;
            set
            {
                boxSelectTransform = value;
                if (boxSelectTransform != null)
                {
                    boxSelectTransform.gameObject.SetActive(false);
                    SortUI();
                }
            }
        }
        [SerializeField, Tooltip("The transform of a menu object that will be enabled when a key is right clicked. Its child components will be analyzed and any valid data changes will be applied to the key that was clicked.")]
        protected RectTransform editKeyMenuTransform;
        public RectTransform EditKeyMenuTransform
        {
            get => editKeyMenuTransform;
            set
            {
                editKeyMenuTransform = value;
                if (editKeyMenuTransform != null)
                {
                    editKeyMenuTransform.gameObject.SetActive(false);
                    SortUI();
                }
            }
        }
        [SerializeField, Tooltip("The transform of a menu object that will be enabled when a tangent knob is right clicked. Its child components will be analyzed and any valid data changes will be applied to the tangent that was clicked.")]
        protected RectTransform editTangentMenuTransform;
        public RectTransform EditTangentMenuTransform
        {
            get => editTangentMenuTransform;
            set
            {
                editTangentMenuTransform = value;
                if (editTangentMenuTransform != null)
                {
                    editTangentMenuTransform.gameObject.SetActive(false);
                    SortUI();
                }
            }
        }
        [SerializeField, Tooltip("The transform of a menu object that will be enabled when the backboard is right clicked.")]
        protected RectTransform contextMenuTransform;
        public RectTransform ContextMenuTransform
        {
            get => contextMenuTransform;
            set
            {
                contextMenuTransform = value;
                if (contextMenuTransform != null)
                {
                    contextMenuTransform.gameObject.SetActive(false);
                    SortUI();
                }
            }
        }
        [SerializeField, Tooltip("The transform of an object that displays the time and value of a keyframe when it's being dragged.")]
        protected RectTransform keyDragDisplayTransform;
        public RectTransform KeyDragDisplayTransform
        {
            get => keyDragDisplayTransform;
            set
            {
                keyDragDisplayTransform = value;
                if (keyDragDisplayTransform != null)
                {
                    keyDragDisplayTransform.gameObject.SetActive(false);
                    SortUI();
                }
            }
        }

        public Scrollbar scrollbarHorizontal;
        public Scrollbar scrollbarVertical;

        [NonSerialized]
        protected bool isPanning;
        [NonSerialized]
        protected bool isZooming;
        [NonSerialized]
        protected UIDraggable backboard;
        protected Vector3 backboardLastClickWorldPos;
        public UIDraggable Backboard
        {
            get
            {
                if (backboard == null)
                {
                    var backboardRT = AnimationCurveEditorUtils.AddOrGetComponent<RectTransform>(new GameObject("backboard"));
                    backboardRT.SetParent(TimelineTransform, false);
                    backboardRT.anchorMin = Vector2.zero;
                    backboardRT.anchorMax = Vector2.one;
                    backboardRT.anchoredPosition3D = Vector2.zero;
                    backboardRT.sizeDelta = Vector2.zero;

                    var proxyImg = backboardRT.gameObject.AddComponent<Image>();
                    proxyImg.color = Color.clear;
                    proxyImg.raycastTarget = true;

                    backboard = backboardRT.gameObject.AddComponent<UIDraggable>();
                    backboard.targetGraphic = proxyImg;
                    backboard.navigation = new Navigation() { mode = Navigation.Mode.None };
                    backboard.freeze = true;

                    if (backboard.OnClick == null) backboard.OnClick = new UnityEvent();
                    backboard.OnClick.AddListener(() => {

                        backboardLastClickWorldPos = backboard.LastClickPosition;

                        if (backboard.lastClickButton == PointerEventData.InputButton.Left)
                        {
                            if (isPanning || InputProxy.IsShiftPressed) return;

                            bool flag = true;
                            if (editKeyMenuTransform != null)
                            {
                                var obj = editKeyMenuTransform.gameObject;
                                if (obj.activeSelf)
                                {
                                    flag = false;
                                    obj.SetActive(false);
                                }
                            }
                            if (editTangentMenuTransform != null)
                            {
                                var obj = editTangentMenuTransform.gameObject;
                                if (obj.activeSelf)
                                {
                                    flag = false;
                                    obj.SetActive(false);
                                }
                            }
                            if (contextMenuTransform != null)
                            {
                                var obj = contextMenuTransform.gameObject;
                                if (obj.activeSelf)
                                {
                                    flag = false;
                                    obj.SetActive(false);
                                }
                            }

                            CancelBoxSelect();

                            if (flag) // Only Deselect if no menus were open
                            {
                                DeselectAll();
                            }
                        }
                        else if (backboard.lastClickButton == PointerEventData.InputButton.Right)
                        {
                            if (isZooming || InputProxy.IsAltPressed || InputProxy.IsSpacePressed) return;

                            CloseAllMenus();
                            OpenContextMenu(backboard.LastClickPosition);
                        }

                    });

                    if (backboard.OnDragStart == null) backboard.OnDragStart = new UnityEvent();
                    if (backboard.OnDragStep == null) backboard.OnDragStep = new UnityEvent();
                    if (backboard.OnDragStop == null) backboard.OnDragStop = new UnityEvent();

                    void DragStart()
                    {
                        if (backboard.lastDragButton == PointerEventData.InputButton.Left && !InputProxy.IsAltPressed && !InputProxy.IsSpacePressed)
                        {
                            if (!disableInteraction) StartBoxSelect();
                        }
                        else if (backboard.lastDragButton == PointerEventData.InputButton.Middle || (backboard.lastDragButton == PointerEventData.InputButton.Left && (InputProxy.IsAltPressed || InputProxy.IsSpacePressed)))
                        {
                            isPanning = !disableNavigation;
                        }
                        else if (backboard.lastDragButton == PointerEventData.InputButton.Right && (InputProxy.IsAltPressed || InputProxy.IsSpacePressed))
                        {
                            isZooming = !disableNavigation;
                        }
                    }
                    void DragStep()
                    {
                        if (backboard.lastDragButton == PointerEventData.InputButton.Left && !InputProxy.IsAltPressed && !InputProxy.IsSpacePressed)
                        {
                            if (disableInteraction) CompleteBoxSelect(); else ContinueBoxSelect();
                        }
                        else if (isPanning && (backboard.lastDragButton == PointerEventData.InputButton.Middle || (backboard.lastDragButton == PointerEventData.InputButton.Left && (InputProxy.IsAltPressed || InputProxy.IsSpacePressed))))
                        {
                            PanWorld(-backboard.LastDragTranslation);
                        }
                        else if (isZooming && (backboard.lastDragButton == PointerEventData.InputButton.Right && (InputProxy.IsAltPressed || InputProxy.IsSpacePressed)))
                        {
                            float amount = ((Vector2)backboard.LastDragTranslationCanvas).magnitude;
                            if (amount != 0)
                            {
                                Vector2 dir = ((Vector2)backboard.LastDragTranslation) / amount;
                                float dotX = Vector3.Dot(Vector3.right, dir);
                                float dotY = Vector3.Dot(Vector3.down, dir);

                                float sign = Mathf.Sign(dotY);
                                if (Mathf.Abs(dotX) > Mathf.Abs(dotY)) sign = Mathf.Sign(dotX);

                                Zoom(amount * clickZoomScale * zoomSensitivity * sign, backboard.DragStartClickPosition);
                            }
                        }
                    }
                    void DragStop()
                    {
                        IEnumerator WaitOneFrame()
                        {
                            yield return null; // Delay for OnClick
                            isPanning = false;
                            isZooming = false;
                        }

                        StartCoroutine(WaitOneFrame());

                        CompleteBoxSelect();
                    }

                    backboard.OnDragStart.AddListener(DragStart);
                    backboard.OnDragStep.AddListener(DragStep);
                    backboard.OnDragStop.AddListener(DragStop);
                }
                return backboard;
            }
        }

        [SerializeField, Header("Grid")]
        protected bool renderGrid = true;
        public bool RenderGrid => renderGrid;
        public void SetRenderGrid(bool shouldRenderGrid)
        {
            this.renderGrid = shouldRenderGrid;
            RedrawGrid();
        }
        [SerializeField, Tooltip("Should grid value markers be rendered above the curve?")]
        protected bool renderGridMarkersOnTop;
        public void SetRenderGridMarkersOnTop(bool flag, bool redraw = true)
        {
            renderGridMarkersOnTop = flag;
            if (flag)
            {
                GridMarkerContainer.SetSiblingIndex(PostCurveContainer.GetSiblingIndex() + 1);
            }
            else
            {
                if (gridRenderer == null)
                {
                    GridMarkerContainer.SetSiblingIndex(Backboard.transform.GetSiblingIndex() + 1);
                }
                else
                {
                    GridMarkerContainer.SetSiblingIndex(gridRenderer.transform.GetSiblingIndex());
                }
            }

            if (redraw) RedrawGrid();
        }
        public bool RenderGridMarkersOnTop
        {
            get => renderGridMarkersOnTop;
            set => SetRenderGridMarkersOnTop(value);
        }
        [SerializeField]
        protected UILightweightGridRenderer gridRenderer;
        public UILightweightGridRenderer GridRenderer
        {
            get => gridRenderer;
            set
            {
                gridRenderer = value;
                if (gridRenderer != null)
                {
                    gridRenderer.transform.SetParent(TimelineTransform, false);
                    gridRenderer.transform.SetSiblingIndex(Backboard.transform.GetSiblingIndex() + 1);
                    gridRenderer.raycastTarget = false;
                    gridRenderer.SetThickness(curveRenderer == null ? 1 : curveRenderer.LineThickness);
                }
                else
                {
                    renderGrid = false;
                }

                RedrawGrid();
            }
        }
        [SerializeField, Tooltip("The target number of lines to render for each axis of the grid. This is simply a starting value, as the zoom level ultimately determines how many lines to render.")]
        protected Vector2Int gridSize = new Vector2Int(10, 10);
        public Vector2Int GridSize 
        {
            get => gridSize;
            set => SetGridSize(value);
        }
        public void SetGridSize(Vector2Int gridSize, bool redraw = true)
        {
            this.gridSize = gridSize;
            if (redraw) RedrawGrid();
        }
        public void SetGridThickness(float thickness)
        {
            if (gridRenderer == null) RedrawGrid();

            gridRenderer.SetThickness(thickness);
            gridRenderer.SetColor(gridColor);
        }
        [SerializeField, Tooltip("The amount of scaling to apply to the grid increment per axis everytime the increment is too small to fit into the range.")]
        protected float gridMarkerIncrementScaling = 10;
        public float GridMarkerIncrementScaling
        {
            get => gridMarkerIncrementScaling;
            set => SetGridMarkerIncrementScaling(value);
        }
        public void SetGridMarkerIncrementScaling(float gridMarkerIncrementScaling, bool redraw = true)
        {
            this.gridMarkerIncrementScaling = gridMarkerIncrementScaling;
            if (redraw) RedrawGrid();
        }
        [SerializeField, Tooltip("The increments at which to attempt to draw a line and display a value on the grid. This is scaled according to the size of the visible range and the target grid line count (GridSize). This value should represent the lowest increment that can be used.")]
        protected float gridMarkerIncrement = 0.005f;
        public float GridMarkerIncrement
        {
            get => gridMarkerIncrement;
            set => SetGridMarkerIncrement(value);
        }
        public void SetGridMarkerIncrement(float gridMarkerIncrement, bool redraw = true)
        {
            this.gridMarkerIncrement = gridMarkerIncrement;
            if (redraw) RedrawGrid();
        }
        [SerializeField, Tooltip("The offset applied to the position of x-axis grid value markers in reference to the start position of the line they represent.")]
        protected Vector2 gridMarkerOffsetX = new Vector2(5, 5);
        public Vector2 GridMarkerOffsetX
        {
            get => gridMarkerOffsetX;
            set => SetGridMarkerOffsetX(value);
        }
        public void SetGridMarkerOffsetX(Vector2 gridValueOffsetX, bool redraw = true)
        {
            this.gridMarkerOffsetX = gridValueOffsetX;
            if (redraw) RedrawGrid();
        }
        [SerializeField, Tooltip("The offset applied to the position of y-axis grid value markers in reference to the start position of the line they represent.")]
        protected Vector2 gridMarkerOffsetY = new Vector2(5, 5);
        public Vector2 GridMarkerOffsetY
        {
            get => gridMarkerOffsetY;
            set => SetGridMarkerOffsetY(value);
        }
        public void SetGridMarkerOffsetY(Vector2 gridValueOffsetY, bool redraw = true)
        {
            this.gridMarkerOffsetY = gridValueOffsetY; 
            if (redraw) RedrawGrid();
        }

        [Serializable]
        public enum GridValueClampMethod
        {
            Off, NormalizedRange, NormalizedMinimum, NormalizedMaximum, PaddedRange, PaddedMinimum, PaddedMaximum
        }

        [SerializeField, Tooltip("Should grid values along the x-axis only be shown in a specific portion of the grid?")]
        protected GridValueClampMethod clampVisibleGridValuesX;
        public GridValueClampMethod ClampVisibleGridValuesX
        {
            get => clampVisibleGridValuesX;
            set => SetClampVisibleGridValuesX(value);
        }
        public void SetClampVisibleGridValuesX(GridValueClampMethod clamp, bool redraw = true)
        {
            clampVisibleGridValuesX = clamp;
            if (redraw) RedrawGrid();
        }
        [SerializeField, Tooltip("Range of the visible grid along the x-axis to show grid values, if clamping is enabled.")]
        protected Vector2 clampVisibleGridValuesRangeX = new Vector2(0, 1);
        public Vector2 ClampVisibleGridValuesRangeX
        {
            get => clampVisibleGridValuesRangeX;
            set => SetClampVisibleGridValuesRangeX(value);
        }
        public void SetClampVisibleGridValuesRangeX(Vector2 clampVisibleGridValuesRangeX, bool redraw = true)
        {
            this.clampVisibleGridValuesRangeX = clampVisibleGridValuesRangeX;
            if (redraw) RedrawGrid();
        }

        [SerializeField, Tooltip("Should grid values along the y-axis only be shown in a specific portion of the grid?")]
        protected GridValueClampMethod clampVisibleGridValuesY;
        public GridValueClampMethod ClampVisibleGridValuesY
        {
            get => clampVisibleGridValuesY;
            set => SetClampVisibleGridValuesY(value);
        }
        public void SetClampVisibleGridValuesY(GridValueClampMethod clamp, bool redraw = true)
        {
            clampVisibleGridValuesY = clamp;
            if (redraw) RedrawGrid();
        }
        [SerializeField, Tooltip("Range of the visible grid along the y-axis to show grid values, if clamping is enabled.")]
        protected Vector2 clampVisibleGridValuesRangeY = new Vector2(0, 1);
        public Vector2 ClampVisibleGridValuesRangeY
        {
            get => clampVisibleGridValuesRangeY;
            set => SetClampVisibleGridValuesRangeY(value);
        }
        public void SetClampVisibleGridValuesRangeY(Vector2 clampVisibleGridValuesRangeY, bool redraw = true)
        {
            this.clampVisibleGridValuesRangeY = clampVisibleGridValuesRangeY;
            if (redraw) RedrawGrid();
        }

        public static void RefreshScrollbar(Scrollbar scrollbar, Vector2 curveRange, Vector2 visibleRange, UnityAction<Vector2> setVisibleRange, float scrollMultiplier = 1)
        {
            float rangeCurve = (curveRange.y - curveRange.x);
            if (rangeCurve <= 0)
            {
                if (scrollbar.onValueChanged != null) scrollbar.onValueChanged.RemoveAllListeners();
                scrollbar.size = 1;
            }
            else
            {
                Vector2 scrollRange = new Vector2(Mathf.Min(curveRange.x, visibleRange.x), Mathf.Max(curveRange.y, visibleRange.y));
                float rangeScroll = (scrollRange.y - scrollRange.x);
                float rangeVisible = (visibleRange.y - visibleRange.x);
                float scrollMargin = Mathf.Abs(rangeVisible - rangeScroll);
                if (rangeScroll <= 0 || scrollMargin <= 0 || (scrollRange.x < curveRange.x && scrollRange.y > curveRange.y))
                {
                    if (scrollbar.onValueChanged != null) scrollbar.onValueChanged.RemoveAllListeners();
                    scrollbar.size = 1;
                    return;
                } 

                scrollbar.size = Mathf.Clamp01(rangeVisible / rangeScroll); 
                float initialValue = Mathf.Clamp01((visibleRange.x - scrollRange.x) / scrollMargin);
                if (scrollMultiplier < 0) initialValue = 1 - initialValue;

                void OnScroll(float value)
                {
                    value = Mathf.Clamp01(value);
                    float offset = (value - initialValue) * scrollMargin * scrollMultiplier;
                    setVisibleRange(visibleRange + new Vector2(offset, offset)); 
                }
                AnimationCurveEditorUtils.SetScrollbarOnValueChangeAction(scrollbar, OnScroll);

                scrollbar.SetValueWithoutNotify(initialValue);
            }
        }
        public virtual void RefreshScrollbars()
        { 
            if (scrollbarHorizontal != null) RefreshScrollbar(scrollbarHorizontal, CurveRangeX, rangeX, SetRangeXAndRedraw, scrollbarHorizontal.direction == Scrollbar.Direction.TopToBottom || scrollbarHorizontal.direction == Scrollbar.Direction.LeftToRight ? 1 : -1);        
            if (scrollbarVertical != null) RefreshScrollbar(scrollbarVertical, CurveRangeY, rangeY, SetRangeYAndRedraw, scrollbarVertical.direction == Scrollbar.Direction.TopToBottom || scrollbarVertical.direction == Scrollbar.Direction.LeftToRight ? -1 : 1);   
        }

        protected readonly List<GameObject> gridValueMarkers = new List<GameObject>();
        protected float grid_minX, grid_minY, grid_maxX, grid_maxY, grid_incrementX, grid_incrementY;
        protected Vector2Int grid_size;
        public virtual void RedrawGrid() 
        {
            RefreshScrollbars();

            if (gridRenderer == null)
            {
                gridRenderer = new GameObject("grid").AddComponent<UILightweightGridRenderer>();
                gridRenderer.transform.SetParent(TimelineTransform, false);
                gridRenderer.transform.SetSiblingIndex(Backboard.transform.GetSiblingIndex() + 1);
                gridRenderer.raycastTarget = false;
                gridRenderer.SetThickness(curveRenderer == null ? 1 : curveRenderer.LineThickness);
            }
            if (!renderGrid)
            {
                gridRenderer.gameObject.SetActive(false);
                RedrawGridValueMarkers();
                return;
            }

            float rangeSizeX = rangeX.y - rangeX.x;
            float rangeSizeY = rangeY.y - rangeY.x;

            grid_minX = 0;
            grid_minY = 0;

            grid_maxX = 1;
            grid_maxY = 1;

            grid_incrementX = gridMarkerIncrement;
            grid_incrementY = gridMarkerIncrement;
            grid_size = gridSize;
            if (gridMarkerIncrement != 0) 
            {

                float SnapAnchorPoint(float gridMarkerIncrement, float rangeStart, float rangeSize)
                {
                    return ((Mathf.Floor(rangeStart / gridMarkerIncrement) * gridMarkerIncrement) - rangeStart) / rangeSize;
                }
                float CalculateAnchorMax(float gridMarkerIncrement, float rangeStart, float rangeEnd, float rangeScale, int maxLineCount, out int outMaxLineCount)
                {
                    outMaxLineCount = maxLineCount;
                    float currentRangeSize = rangeEnd - rangeStart;

                    float ratio = currentRangeSize / gridMarkerIncrement;
                    int markerCount = Mathf.CeilToInt(ratio);
                    int minLineCount = Mathf.Max(1, markerCount);

                    int lineCount = minLineCount;
                    while (lineCount * 2 <= maxLineCount) lineCount = lineCount * 2;
                    if (lineCount > maxLineCount) outMaxLineCount = lineCount;

                    float size = (markerCount * gridMarkerIncrement);
                    float lineSize = size / lineCount;

                    float anchor = ((lineSize * (outMaxLineCount - 1)) + rangeStart);
                    if (outMaxLineCount >= maxLineCount)
                    {
                        outMaxLineCount = outMaxLineCount + 1; // Quick fix for grid becoming smaller than its container when expanding lineCount past maxLineCount
                        anchor = anchor + lineSize;
                    }
                    return anchor / rangeScale;
                }

                grid_incrementX = gridMarkerIncrement;
                if (gridMarkerIncrementScaling > 0) while (Mathf.FloorToInt(rangeSizeX / grid_incrementX) > gridSize.x) grid_incrementX *= gridMarkerIncrementScaling;

                grid_incrementY = gridMarkerIncrement;
                if (gridMarkerIncrementScaling > 0) while (Mathf.FloorToInt(rangeSizeY / grid_incrementY) > gridSize.y) grid_incrementY *= gridMarkerIncrementScaling;
                 
                grid_minX = SnapAnchorPoint(grid_incrementX, rangeX.x, rangeSizeX);
                grid_minY = SnapAnchorPoint(grid_incrementY, rangeY.x, rangeSizeY);

                grid_maxX = CalculateAnchorMax(grid_incrementX, grid_minX * rangeSizeX, grid_maxX * rangeSizeX, rangeSizeX, gridSize.x, out int lineCountX);
                grid_maxY = CalculateAnchorMax(grid_incrementY, grid_minY * rangeSizeY, grid_maxY * rangeSizeY, rangeSizeY, gridSize.y, out int lineCountY);

                grid_size.x = lineCountX;
                grid_size.y = lineCountY;
            }

            var gridTransform = gridRenderer.GetComponent<RectTransform>();
            gridTransform.anchorMin = new Vector2(grid_minX, grid_minY);
            gridTransform.anchorMax = new Vector2(grid_maxX, grid_maxY);
            gridTransform.anchoredPosition3D  = Vector3.zero;
            gridTransform.sizeDelta = Vector2.zero;
            gridRenderer.gameObject.SetActive(renderGrid); 

            gridRenderer.SetGridSize(grid_size);
            gridRenderer.SetColor(gridColor);

            RedrawGridValueMarkers();
        }
        public static int RoundedIncrementCount(float value, float increment)
        {
            if (increment == 0) return 0;

            return Mathf.RoundToInt(value / increment);
        }

        private static string DecimalSeparator => CultureInfo.InstalledUICulture.NumberFormat.NumberDecimalSeparator;
        protected float ShiftIncrement(float increment, int count)
        {
            if (count <= 0) return increment;

            string val = increment.ToString();
            
            int dot = val.IndexOf(DecimalSeparator);
            if (dot >= 0)
            {
                string zeros = new string('0', count);
                val = val.Substring(0, dot + 1) + zeros + (dot + 1 < val.Length ? val.Substring(dot + 1) : "");
            } 
            else
            {
                if (count == val.Length)
                {
                    val = $"0{DecimalSeparator}" + val;
                }
                else if (count > val.Length)
                {
                    string zeros = new string('0', count - val.Length);
                    val = $"0{DecimalSeparator}" + zeros + val;
                } 
                else
                {
                    val = val.Substring(0, val.Length - count) + DecimalSeparator + val.Substring(count);  
                }
            }
            return float.Parse(val/*, CultureInfo.InvariantCulture*/);
        }
        public virtual void RedrawGridValueMarkers()
        {
            foreach (var marker in gridValueMarkers) // Clear previous grid value markers
            {
                if (marker == null) continue;
                if (gridValuePool == null)
                {
                    Destroy(marker);
                }
                else
                {
                    gridValuePool.Release(marker);
                }
            }
            gridValueMarkers.Clear();

            if (!renderGrid) return;
            if (gridRenderer == null)
            {
                RedrawGrid();
                return;
            }

            if (gridValuePool != null && gridValuePrototype != null) // Render grid value markers
            {
                float rangeSizeX = rangeX.y - rangeX.x;
                float rangeSizeY = rangeY.y - rangeY.x;

                TimelineTransform.GetLocalCorners(fourCornersArray);
                Rect timelineRect = timelineTransform.rect;
                Vector2 min = new Vector2(Mathf.Min(fourCornersArray[0].x, fourCornersArray[1].x, fourCornersArray[2].x, fourCornersArray[3].x), Mathf.Min(fourCornersArray[0].y, fourCornersArray[1].y, fourCornersArray[2].y, fourCornersArray[3].y));
                Vector2 max = new Vector2(Mathf.Max(fourCornersArray[0].x, fourCornersArray[1].x, fourCornersArray[2].x, fourCornersArray[3].x), Mathf.Max(fourCornersArray[0].y, fourCornersArray[1].y, fourCornersArray[2].y, fourCornersArray[3].y));
                Vector2 range = new Vector2(max.x - min.x, max.y - min.y);
                if (range.x == 0) range.x = 1;
                if (range.y == 0) range.y = 1;

                Vector3 offsetX = gridMarkerOffsetX;
                Vector3 offsetY = gridMarkerOffsetY;

                float sizeX = (((grid_maxX - grid_minX) * timelineRect.width) / Mathf.Max(1, (grid_size.x - 1))) * 0.6f;
                float sizeY = gridValuePrototype == null ? sizeX : AnimationCurveEditorUtils.AddOrGetComponent<RectTransform>(gridValuePrototype).rect.width;

                int shiftX = Mathf.Max(1, Mathf.FloorToInt(grid_size.x / 100f));
                int shiftY = Mathf.Max(1, Mathf.FloorToInt(grid_size.y / 100f));
                decimal incX = (decimal)ShiftIncrement(grid_incrementX, shiftX);
                decimal incY = (decimal)ShiftIncrement(grid_incrementY, shiftY);

                for (int x = 0; x < grid_size.x; x++)
                {
                    float t = x / (grid_size.x - 1f);
                    t = grid_minX + (grid_maxX - grid_minX) * t;

                    float T;
                    switch (clampVisibleGridValuesX)
                    {
                        default:
                            break;

                        case GridValueClampMethod.NormalizedRange:
                            if (t < clampVisibleGridValuesRangeX.x || t > clampVisibleGridValuesRangeX.y) continue;
                            break;
                        case GridValueClampMethod.NormalizedMinimum:
                            if (t < clampVisibleGridValuesRangeX.x) continue;
                            break;
                        case GridValueClampMethod.NormalizedMaximum:
                            if (t > clampVisibleGridValuesRangeX.y) continue;
                            break;

                        case GridValueClampMethod.PaddedRange:
                            T = t * range.x;
                            if (T < clampVisibleGridValuesRangeX.x || T > range.x - clampVisibleGridValuesRangeX.y) continue;
                            break;
                        case GridValueClampMethod.PaddedMinimum:
                            T = t * range.x;
                            if (T < clampVisibleGridValuesRangeX.x) continue;
                            break;
                        case GridValueClampMethod.PaddedMaximum:
                            T = t * range.x;
                            if (T > range.x - clampVisibleGridValuesRangeX.y) continue;
                            break;
                    }

                    if (gridValuePool.TryGetNewInstance(out GameObject inst))
                    {
                        gridValueMarkers.Add(inst);
                        var instRT = AnimationCurveEditorUtils.AddOrGetComponent<RectTransform>(inst);
                        var pos = timelineTransform.InverseTransformPoint(instRT.position);
                        pos.x = min.x + range.x * t;
                        pos.y = min.y;
                        pos = pos + offsetX;
                        instRT.anchorMin = instRT.anchorMax = new Vector2((pos.x - min.x) / range.x, (pos.y - min.y) / range.y);
                        instRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, sizeX);
                        instRT.position = timelineTransform.TransformPoint(pos);

                        AnimationCurveEditorUtils.SetComponentText(instRT, $"{(RoundedIncrementCount(rangeX.x + rangeSizeX * t, (float)incX) * incX).ToString("0.##########")}");
                        AnimationCurveEditorUtils.SetAllRaycastTargets(instRT, false);
                    }
                }
                for (int y = 0; y < grid_size.y; y++)
                {
                    float t = y / (grid_size.y - 1f);
                    t = grid_minY + (grid_maxY - grid_minY) * t;

                    float T;
                    switch (clampVisibleGridValuesY)
                    {
                        default:
                            break;

                        case GridValueClampMethod.NormalizedRange:
                            if (t < clampVisibleGridValuesRangeY.x || t > clampVisibleGridValuesRangeY.y) continue;
                            break;
                        case GridValueClampMethod.NormalizedMinimum:
                            if (t < clampVisibleGridValuesRangeY.x) continue;
                            break;
                        case GridValueClampMethod.NormalizedMaximum:
                            if (t > clampVisibleGridValuesRangeY.y) continue;
                            break;

                        case GridValueClampMethod.PaddedRange:
                            T = t * range.y;
                            if (T < clampVisibleGridValuesRangeY.x || T > range.y - clampVisibleGridValuesRangeY.y) continue;
                            break;
                        case GridValueClampMethod.PaddedMinimum:
                            T = t * range.y;
                            if (T < clampVisibleGridValuesRangeY.x) continue;
                            break;
                        case GridValueClampMethod.PaddedMaximum:
                            T = t * range.y;
                            if (T > range.y - clampVisibleGridValuesRangeY.y) continue;
                            break;
                    }

                    if (gridValuePool.TryGetNewInstance(out GameObject inst))
                    {
                        gridValueMarkers.Add(inst);
                        var instRT = AnimationCurveEditorUtils.AddOrGetComponent<RectTransform>(inst);
                        var pos = timelineTransform.InverseTransformPoint(instRT.position);
                        pos.x = min.x;
                        pos.y = min.y + range.y * t;
                        pos = pos + offsetY;
                        instRT.anchorMin = instRT.anchorMax = new Vector2((pos.x - min.x) / range.x, (pos.y - min.y) / range.y);
                        instRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, sizeY);
                        instRT.position = timelineTransform.TransformPoint(pos);

                        AnimationCurveEditorUtils.SetComponentText(instRT, $"{(RoundedIncrementCount(rangeY.x + rangeSizeY * t, (float)incY) * incY).ToString("0.##########")}");
                        AnimationCurveEditorUtils.SetAllRaycastTargets(instRT, false);
                    }
                }
            }
        }

        [SerializeField, Header("Visible Ranges"), Tooltip("The visible value range of the x-axis (Time)")]
        protected Vector2 rangeX = new Vector2(0, 1);
        public void SetRangeXAndRedraw(Vector2 rangeX) => SetRangeX(rangeX, true);
        public void SetRangeX(Vector2 rangeX, bool redraw=true)
        {
            this.rangeX = rangeX;

            if (redraw) Redraw();
        }
        [SerializeField, Tooltip("The visible value range of the y-axis (Value)")]
        protected Vector2 rangeY = new Vector2(0, 1);
        public void SetRangeYAndRedraw(Vector2 rangeY) => SetRangeY(rangeY, true);
        public void SetRangeY(Vector2 rangeY, bool redraw = true)
        {
            this.rangeY = rangeY;

            if (redraw) Redraw();
        }

        public Vector2 CurveRangeX => GetCurveRangeX(curveBoundsPadding.x);
        public Vector2 CurveRangeY => GetCurveRangeY(curveBoundsPadding.y);    

        public Vector2 GetCurveRangeX() => GetCurveRangeX(0);
        public Vector2 GetCurveRangeX(float paddingX_UI_space)
        {
            float min = 0;
            float max = 1;

            if (keyframeData == null)
            {
                if (keyframes != null && keyframes.Length > 0)
                {
                    min = float.MaxValue;
                    max = float.MinValue;

                    int count = 0;
                    foreach (var key in keyframes)
                    {
                        if (!float.IsFinite(key.time)) continue;
                        if (key.time < min) min = key.time;
                        if (key.time > max) max = key.time;
                        count++;
                    }
                    if (count <= 0)
                    {
                        min = 0;
                        max = 1;
                    }
                }
            }
            else if (keyframeData.Length > 0)
            {
                min = float.MaxValue;
                max = float.MinValue;

                int count = 0;
                foreach (var key in keyframeData)
                {
                    if (!float.IsFinite(key.Time)) continue;
                    if (key.Time < min) min = key.Time;
                    if (key.Time > max) max = key.Time;
                    count++;
                }
                if (count <= 0)
                {
                    min = 0;
                    max = 1;
                }
            }

            if (min == max)
            {
                min = min - 0.01f;
                max = max + 0.01f;
            }

            float padding = 0;
            if (paddingX_UI_space != 0)
            {
                var rect = TimelineTransform.rect;
                if (rect.width > 0) padding = (paddingX_UI_space / rect.width) * (max - min);
            }

            return new Vector2(min - padding, max + padding);
        }

        public Vector2 GetCurveRangeY() => GetCurveRangeY(0);
        public Vector2 GetCurveRangeY(float paddingY_UI_space)
        {
            float min = 0;
            float max = 1;

            if (keyframeData == null)
            {
                if (keyframes != null && keyframes.Length > 0)
                {
                    min = float.MaxValue;
                    max = float.MinValue;

                    int count = 0;
                    foreach (var key in keyframes)
                    {
                        if (!float.IsFinite(key.value)) continue;
                        if (key.value < min) min = key.value;
                        if (key.value > max) max = key.value;
                        count++;
                    }
                    if (count <= 0)
                    {
                        min = 0;
                        max = 1;
                    }
                }
            }
            else if (keyframeData.Length > 0)
            {
                min = float.MaxValue;
                max = float.MinValue;

                int count = 0;
                foreach (var key in keyframeData)
                {
                    if (!float.IsFinite(key.Value)) continue;
                    if (key.Value < min) min = key.Value;
                    if (key.Value > max) max = key.Value;
                    count++;
                }
                if (count <= 0)
                {
                    min = 0;
                    max = 1;
                }
            }

            if (min == max)
            {
                min = min - 0.01f;
                max = max + 0.01f;
            }

            float padding = 0; 
            if (paddingY_UI_space != 0)
            {
                var rect = TimelineTransform.rect;
                if (rect.height > 0) padding = (paddingY_UI_space / rect.height) * (max - min);
            }

            return new Vector2(min - padding, max + padding);
        }

        protected readonly HashSet<int> boxSelection = new HashSet<int>();
        protected Vector3 startDragSelectionPos, currentDragSelectionPos;
        protected virtual void StartBoxSelect()
        {
            if (boxSelectTransform == null || timelineTransform == null || Canvas == null || backboard == null) return;

            CloseAllMenus();

            if (InputProxy.IsShiftPressed) backboard.cancelNextClick = true; else DeselectAll(); 

            boxSelectTransform.gameObject.SetActive(true);
            boxSelectTransform.anchorMin = boxSelectTransform.anchorMax = new Vector2(0.5f, 0.5f);

            boxSelection.Clear();

            backboard.cancelNextClick = false;

            startDragSelectionPos = timelineTransform.InverseTransformPoint(canvas.transform.TransformPoint(AnimationCurveEditorUtils.ScreenToCanvasPosition(canvas, InputProxy.CursorScreenPosition)));
            ContinueBoxSelect();
        }

        protected virtual void ContinueBoxSelect()
        {
            if (boxSelectTransform == null || !boxSelectTransform.gameObject.activeSelf || timelineTransform == null || keyframeData == null || Canvas == null || backboard == null) return;

            currentDragSelectionPos = timelineTransform.InverseTransformPoint(canvas.transform.TransformPoint(AnimationCurveEditorUtils.ScreenToCanvasPosition(canvas, InputProxy.CursorScreenPosition)));
            Vector3 dragSelectionOffset = currentDragSelectionPos - startDragSelectionPos;

            if (!backboard.cancelNextClick && dragSelectionOffset.sqrMagnitude > 0.8f) backboard.cancelNextClick = true;

            boxSelectTransform.pivot = new Vector2(dragSelectionOffset.x < 0 ? 1 : 0, dragSelectionOffset.y < 0 ? 1 : 0);
            boxSelectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Abs(dragSelectionOffset.x));
            boxSelectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Abs(dragSelectionOffset.y));
            boxSelectTransform.position = timelineTransform.TransformPoint(startDragSelectionPos);

            boxSelectTransform.GetWorldCorners(fourCornersArray);
            Vector2 min = new Vector2(Mathf.Min(fourCornersArray[0].x, fourCornersArray[1].x, fourCornersArray[2].x, fourCornersArray[3].x), Mathf.Min(fourCornersArray[0].y, fourCornersArray[1].y, fourCornersArray[2].y, fourCornersArray[3].y));
            Vector2 max = new Vector2(Mathf.Max(fourCornersArray[0].x, fourCornersArray[1].x, fourCornersArray[2].x, fourCornersArray[3].x), Mathf.Max(fourCornersArray[0].y, fourCornersArray[1].y, fourCornersArray[2].y, fourCornersArray[3].y));

            var root = KeyframeContainer == null ? timelineTransform : keyframeContainer;
            for(int a = 0; a < root.childCount; a++)
            {
                var child = root.GetChild(a);
                if (child == null || child is not RectTransform) continue;
                var childRT = (RectTransform)child;

                childRT.GetWorldCorners(fourCornersArray);
                Vector2 child_min = new Vector2(Mathf.Min(fourCornersArray[0].x, fourCornersArray[1].x, fourCornersArray[2].x, fourCornersArray[3].x), Mathf.Min(fourCornersArray[0].y, fourCornersArray[1].y, fourCornersArray[2].y, fourCornersArray[3].y));
                Vector2 child_max = new Vector2(Mathf.Max(fourCornersArray[0].x, fourCornersArray[1].x, fourCornersArray[2].x, fourCornersArray[3].x), Mathf.Max(fourCornersArray[0].y, fourCornersArray[1].y, fourCornersArray[2].y, fourCornersArray[3].y));
                bool inside = !(child_max.x < min.x || child_min.x > max.x || child_max.y < min.y || child_min.y > max.y);

                var childGO = childRT.gameObject;
                if (!childGO.activeSelf) continue;

                if (inside)
                {
                    for (int b = 0; b < keyframeData.Length; b++)
                    {
                        if (selectedKeys.Contains(b)) continue;
                        var key = keyframeData[b];
                        if (key == null) continue;

                        if (childGO == key.instance)
                        {
                            boxSelection.Add(b);
                            selectedKeys.Add(b);
                            key.UpdateInUI(keyframeData, timelineTransform, rangeX, rangeY, true, true, unweightedTangentHandleSize);
                        }
                        else if (key.draggable != null && (key.draggable.gameObject == childGO || (key.draggable.Root != null && key.draggable.Root.gameObject == childGO)))
                        {
                            boxSelection.Add(b);
                            selectedKeys.Add(b);
                            key.UpdateInUI(keyframeData, timelineTransform, rangeX, rangeY, true, true, unweightedTangentHandleSize);
                        }
                    }
                } 
                else
                {
                    for (int b = 0; b < keyframeData.Length; b++)
                    {
                        var key = keyframeData[b];
                        if (key == null) continue;

                        if (childGO == key.instance && boxSelection.Contains(b))
                        {
                            boxSelection.Remove(b);
                            selectedKeys.Remove(b);
                            key.UpdateInUI(keyframeData, timelineTransform, rangeX, rangeY, showAllTangents, false, unweightedTangentHandleSize);
                        }
                        else if ((key.draggable != null && (key.draggable.gameObject == childGO || (key.draggable.Root != null && key.draggable.Root.gameObject == childGO))) && boxSelection.Contains(b))
                        {
                            boxSelection.Remove(b);
                            selectedKeys.Remove(b);
                            key.UpdateInUI(keyframeData, timelineTransform, rangeX, rangeY, showAllTangents, false, unweightedTangentHandleSize);
                        }
                    }
                }
            }
        }
        protected virtual void CompleteBoxSelect()
        {
            if (boxSelectTransform != null) 
            {
                if (!boxSelectTransform.gameObject.activeSelf) return;
                boxSelectTransform.gameObject.SetActive(false); 
            }

            foreach (var selection in boxSelection) 
            { 
                selectedKeys.Add(selection); 
                if (keyframeData != null)
                {
                    var key = keyframeData[selection];
                    if (key != null)
                    {
                        key.UpdateInUI(keyframeData, timelineTransform, rangeX, rangeY, true, true, unweightedTangentHandleSize);
                    }
                }
            }
            boxSelection.Clear();
        }
        protected virtual void CancelBoxSelect()
        {
            if (boxSelectTransform != null) boxSelectTransform.gameObject.SetActive(false);

            foreach (var selection in boxSelection) 
            { 
                selectedKeys.Remove(selection);
                if (keyframeData != null)
                {
                    var key = keyframeData[selection];
                    if (key != null)
                    {
                        key.UpdateInUI(keyframeData, timelineTransform, rangeX, rangeY, showAllTangents, false, unweightedTangentHandleSize);
                    }
                }
            }
            boxSelection.Clear();
        }

        private Canvas canvas;
        public Canvas Canvas
        {
            get
            {
                if (canvas == null) canvas = gameObject.GetComponentInParent<Canvas>(true);
                return canvas;
            }
        }
        private RectTransform canvasRectTransform;
        public RectTransform CanvasRectTransform
        {
            get
            {
                if (canvasRectTransform == null) canvasRectTransform = Canvas.gameObject.AddOrGetComponent<RectTransform>();
                return canvasRectTransform;
            }
        }

        protected void Awake()
        {
            Initialize();
        }

        protected bool initialized;
        public bool IsInitialized => initialized;
        public void Initialize()
        {
            SetTimelineTransform(TimelineTransform);

            if (keyframePrototype == null) 
            {
                Debug.LogError($"[{nameof(AnimationCurveEditor)}]: {nameof(keyframePrototype)} has not been set for '{name}'");
                return; 
            }
            SetKeyframePrototype(keyframePrototype);

            if (tangentPrototype == null)
            {
                Debug.LogError($"[{nameof(AnimationCurveEditor)}]: {nameof(tangentPrototype)} has not been set for '{name}'");
                return;
            }
            SetTangentPrototype(tangentPrototype);

            if (tangentLinePrototype == null)
            {
                Debug.LogError($"[{nameof(AnimationCurveEditor)}]: {nameof(tangentLinePrototype)} has not been set for '{name}'");
                return;
            }
            SetTangentLinePrototype(tangentLinePrototype);

            SetGridValuePrototype(gridValuePrototype); 

            if (curve == null || curve.length <= 0)
            {
                curve = CurveRenderer.curve;
                if (curve == null) curve = new AnimationCurve();
            }
            SetCurve(curve);

            if (boxSelectTransform != null)
            {
                var imgs = boxSelectTransform.GetComponentsInChildren<Image>(true);
                foreach (var img in imgs) img.raycastTarget = false;

                boxSelectTransform.gameObject.SetActive(false);
            }
            if (editKeyMenuTransform != null)
            {
                editKeyMenuTransform.gameObject.SetActive(false); 
            }
            if (editTangentMenuTransform != null)
            {
                editTangentMenuTransform.gameObject.SetActive(false);
            }
            if (contextMenuTransform != null)
            {
                contextMenuTransform.gameObject.SetActive(false);
            }
            if (keyDragDisplayTransform != null)
            {
                keyDragDisplayTransform.gameObject.SetActive(false);
            }

            initialized = true;

            SetNavigation(!disableNavigation);
            SetInteraction(!disableInteraction);

            rangeX = CurveRangeX;
            rangeY = CurveRangeY;
        }

        protected virtual KeyframeData CreateNewKeyframeData(Keyframe initialData, int index = -1, KeyframeTangentSettings tangentSettings = default, bool autoUpdateTangentSettings = true)
        {
            if (keyframePool == null) return null;
            if (!keyframePool.TryGetNewInstance(out GameObject instance)) return null;

            void SetDraggableLogic(UIDraggable draggable, UnityAction onClick, UnityAction onDragStart, UnityAction onDrag, UnityAction onDragStop)
            {
                if (draggable.OnClick == null) draggable.OnClick = new UnityEvent();

                draggable.OnClick.RemoveAllListeners();
                draggable.OnClick.AddListener(onClick);

                if (draggable.OnDragStart == null) draggable.OnDragStart = new UnityEvent();
                if (draggable.OnDragStep == null) draggable.OnDragStep = new UnityEvent();
                if (draggable.OnDragStop == null) draggable.OnDragStop = new UnityEvent();

                draggable.OnDragStart.RemoveAllListeners();
                draggable.OnDragStep.RemoveAllListeners();
                draggable.OnDragStop.RemoveAllListeners();

                draggable.OnDragStart.AddListener(onDragStart == null ? onDrag : onDragStart);
                draggable.OnDragStep.AddListener(onDrag);
                draggable.OnDragStop.AddListener(onDragStop == null ? onDrag : onDragStop);
            }

            var keyData = new KeyframeData();
            keyData.index = index;

            keyData.baseColor = keyColorBase;
            keyData.selectedColor = keyColorSelected;

            void ClickKey()
            {
                if (keyData.draggable.lastClickButton == PointerEventData.InputButton.Left)
                {
                    CloseAllMenus();
                    if (InputProxy.IsCtrlPressed)
                    {
                        if (IsSelected(keyData)) Deselect(keyData); else Select(keyData, true);
                    }
                    else
                    {
                        Select(keyData, InputProxy.IsShiftPressed);
                    }
                }
                else if (keyData.draggable.lastClickButton == PointerEventData.InputButton.Right)
                {
                    CloseAllMenus();
                    OpenEditKeyMenu(keyData);
                }
            }
            void SelectKeyIfNotSelected() { if (!selectedKeys.Contains(keyData.index)) Select(keyData, InputProxy.IsShiftPressed); }
            void SelectKeyAdd() => Select(keyData, true);
            void ClickInTangent()
            {
                if (keyData.inTangentDraggable == null) return;
                if (keyData.inTangentDraggable.lastClickButton == PointerEventData.InputButton.Left)
                {
                    CloseAllMenus();
                    Select(keyData, true);
                }
                else if (keyData.inTangentDraggable.lastClickButton == PointerEventData.InputButton.Right)
                {
                    Select(keyData, true);
                    CloseAllMenus();
                    OpenEditTangentMenu(keyData, true);
                }
            }
            void ClickOutTangent()
            {
                if (keyData.outTangentDraggable == null) return;
                if (keyData.outTangentDraggable.lastClickButton == PointerEventData.InputButton.Left)
                {
                    CloseAllMenus();
                    Select(keyData, true);
                }
                else if (keyData.outTangentDraggable.lastClickButton == PointerEventData.InputButton.Right)
                {
                    Select(keyData, true);
                    CloseAllMenus();
                    OpenEditTangentMenu(keyData, false);
                }
            }
            void ReevaluateDrag(bool updateState)
            {
                CloseAllMenus();

                if (keyData.inTangentLine != null) keyData.inTangentLine.RefreshIfUpdated();
                if (keyData.outTangentLine != null) keyData.outTangentLine.RefreshIfUpdated();

                if (updateState) PrepNewState();

                ReevaluateKeyframeData(keyData, false, false, false, false, false);

                ShowKeyCoords(keyData.RectTransform.position, keyData.Time, keyData.Value);

                ApplyToSelectedKeysSafe((KeyframeData key) => Translate(key, keyData.draggable.LastDragTranslation, false), keyData);

                if (updateState) FinalizeState();
                RefreshKeyframes();
            }
            void DragKey() => ReevaluateDrag(updateStateDuringDragStep);
            void StopDragging()
            {
                ReevaluateDrag(true);
                HideKeyCoords();
            }
            void ReevaluateInTangent(bool updateState)
            {
                CloseAllMenus();

                if (keyData.inTangentLine != null) keyData.inTangentLine.RefreshIfUpdated();
                ReevaluateKeyframeData(keyData, true, false, true, false, updateState);
            }
            void DragInTangent() => ReevaluateInTangent(updateStateDuringDragStep);
            void DragStopInTangent() => ReevaluateInTangent(true);
            void ReevaluateOutTangent(bool updateState)
            {
                CloseAllMenus();

                if (keyData.outTangentLine != null) keyData.outTangentLine.RefreshIfUpdated();
                ReevaluateKeyframeData(keyData, false, true, false, true, updateState);
            }
            void DragOutTangent() => ReevaluateOutTangent(updateStateDuringDragStep);
            void DragStopOutTangent() => ReevaluateOutTangent(true);

            var state = keyData.state;

            state.data = initialData;
            if (autoUpdateTangentSettings)
            {
                tangentSettings.inTangentMode = float.IsInfinity(initialData.inTangent) ? BrokenTangentMode.Constant : (initialData.inWeight > 0 ? tangentSettings.inTangentMode : BrokenTangentMode.Linear);
                tangentSettings.outTangentMode = float.IsInfinity(initialData.outTangent) ? BrokenTangentMode.Constant : (initialData.outWeight > 0 ? tangentSettings.outTangentMode : BrokenTangentMode.Linear);
            }
            state.tangentSettings = tangentSettings;

            keyData.state = state;

            keyData.instance = instance;
            keyData.instance.SetActive(true);
            keyData.draggable = instance.GetComponentInChildren<UIDraggable>();
            if (keyData.draggable != null)
            {
                keyData.draggable.clickMouseButtonMask = MouseButtonMask.LeftMouseButton | MouseButtonMask.RightMouseButton;
                keyData.draggable.dragMouseButtonMask = MouseButtonMask.LeftMouseButton;
                SetDraggableLogic(keyData.draggable, ClickKey, SelectKeyIfNotSelected, DragKey, StopDragging);

                var instanceTransform = keyData.draggable.Root;
                instanceTransform.SetParent(KeyframeContainer, false);

                keyData.draggable.interactable = !disableInteraction;
                IEnumerator WaitOneFrame()
                {
                    yield return null;
                    keyData.draggable.enabled = !disableInteraction;
                }
                StartCoroutine(WaitOneFrame());
            }

            if (tangentPool != null && tangentPool.TryGetNewInstance(out keyData.inTangentInstance))
            {
                if (tangentLinePool.TryGetNewInstance(out GameObject lineInstance))
                {
                    keyData.inTangentLine = AnimationCurveEditorUtils.AddOrGetComponent<UIAnchoredLine>(lineInstance);
                    keyData.inTangentLine.SetRaycastTarget(false);
                    keyData.inTangentLine.anchorA = keyData.draggable == null ? keyData.instance.GetComponent<RectTransform>() : keyData.draggable.Root;
                }
                keyData.inTangentDraggable = keyData.inTangentInstance.GetComponent<UIDraggable>();
                if (keyData.inTangentDraggable != null)
                {
                    keyData.inTangentDraggable.clickMouseButtonMask = MouseButtonMask.LeftMouseButton | MouseButtonMask.RightMouseButton;
                    keyData.inTangentDraggable.dragMouseButtonMask = MouseButtonMask.LeftMouseButton;
                    keyData.inTangentLine.anchorB = keyData.inTangentDraggable.Root;
                    SetDraggableLogic(keyData.inTangentDraggable, ClickInTangent, SelectKeyAdd, DragInTangent, DragStopInTangent);

                    keyData.inTangentDraggable.interactable = !disableInteraction;
                    IEnumerator WaitOneFrame()
                    {
                        yield return null;
                        keyData.inTangentDraggable.enabled = !disableInteraction;
                    }
                    StartCoroutine(WaitOneFrame());
                }
                else
                {
                    keyData.inTangentLine.anchorB = keyData.inTangentInstance.GetComponent<RectTransform>();
                }
            }

            if (tangentPool != null && tangentPool.TryGetNewInstance(out keyData.outTangentInstance))
            {
                if (tangentLinePool.TryGetNewInstance(out GameObject lineInstance))
                {
                    keyData.outTangentLine = AnimationCurveEditorUtils.AddOrGetComponent<UIAnchoredLine>(lineInstance);
                    keyData.outTangentLine.SetRaycastTarget(false);
                    keyData.outTangentLine.anchorA = keyData.draggable == null ? keyData.instance.GetComponent<RectTransform>() : keyData.draggable.Root;
                }
                keyData.outTangentDraggable = keyData.outTangentInstance.GetComponent<UIDraggable>();
                if (keyData.outTangentDraggable != null)
                {
                    keyData.outTangentDraggable.clickMouseButtonMask = MouseButtonMask.LeftMouseButton | MouseButtonMask.RightMouseButton;
                    keyData.outTangentDraggable.dragMouseButtonMask = MouseButtonMask.LeftMouseButton;
                    keyData.outTangentLine.anchorB = keyData.outTangentDraggable.Root;
                    SetDraggableLogic(keyData.outTangentDraggable, ClickOutTangent, SelectKeyAdd, DragOutTangent, DragStopOutTangent);

                    keyData.outTangentDraggable.interactable = !disableInteraction;
                    IEnumerator WaitOneFrame()
                    {
                        yield return null;
                        keyData.outTangentDraggable.enabled = !disableInteraction;
                    }
                    StartCoroutine(WaitOneFrame());
                }
                else
                {
                    keyData.outTangentLine.anchorB = keyData.outTangentInstance.GetComponent<RectTransform>();
                }
            }

            return keyData;
        }

        /// <summary>
        /// Rebuilds the mesh used to render the animation curve in the UI
        /// </summary>
        public virtual void RebuildCurveMesh()
        {
            if (CurveRenderer != null)
            {
                curveRenderer.SetLineColor(curveColor);
                curveRenderer.curve = curve;
                curveRenderer.useExternalRangeX = true;
                curveRenderer.useExternalRangeY = true;
                curveRenderer.externalRangeX = rangeX;
                curveRenderer.externalRangeY = rangeY;
                curveRenderer.Rebuild();
                curveRenderer.SetRaycastTarget(false); 
            }
        }

        protected readonly HashSet<int> selectedKeys = new HashSet<int>();
        public virtual bool IsSelected(KeyframeData key) => key.index < 0 ? false : selectedKeys.Contains(key.index);
        public virtual bool ShowTangents(KeyframeData key) => ShowAllTangents || IsSelected(key);
        public virtual void SelectSolo(int keyIndex) => Select(keyIndex, false);
        public virtual void SelectAdd(int keyIndex) => Select(keyIndex, true);
        public virtual void Select(int keyIndex, bool additive = false)
        {
            if (keyframeData == null || keyIndex < 0) return;

            if (!additive) DeselectAll();      

            selectedKeys.Add(keyIndex);
        }
        public virtual void Select(KeyframeData key, bool additive = false)
        {
            if (keyframeData == null || key == null || key.index < 0) return;

            Select(key.index, additive);
            key.UpdateInUI(keyframeData, TimelineTransform, rangeX, rangeY, true, true, unweightedTangentHandleSize);
        }
        public virtual void SelectAll()
        {
            if (keyframeData == null) return;

            foreach (var key in keyframeData) if (key != null && key.index >= 0) 
                { 
                    selectedKeys.Add(key.index);
                    key.UpdateInUI(keyframeData, TimelineTransform, rangeX, rangeY, true, true, unweightedTangentHandleSize);
                }
        }

        public virtual void Deselect(int keyIndex)
        {
            if (keyframeData == null || keyIndex < 0) return;

            selectedKeys.Remove(keyIndex);
        }
        public virtual void Deselect(KeyframeData key)
        {
            if (keyframeData == null || key == null || key.index < 0) return;

            Deselect(key.index);
            key.UpdateInUI(keyframeData, TimelineTransform, rangeX, rangeY, showAllTangents, false, unweightedTangentHandleSize);
        }
        public virtual void DeselectAll()
        {
            foreach (var selectedIndex in selectedKeys)
            {
                if (selectedIndex < 0 || selectedIndex >= keyframeData.Length) continue;
                var key = keyframeData[selectedIndex];
                if (key == null) continue;
                key.UpdateInUI(keyframeData, TimelineTransform, rangeX, rangeY, showAllTangents, false, unweightedTangentHandleSize);
            }
            selectedKeys.Clear();
        }
        protected KeyframeData[] keyframeData;
        public virtual int KeyCount => keyframeData == null ? 0 : keyframeData.Length;
        public virtual KeyframeData this[int index]
        {
            get => GetKey(index);
            set => SetKey(index, value);
        }
        public virtual KeyframeData GetKey(int index) => keyframeData == null || index < 0 || index >= keyframeData.Length ? null : keyframeData[index];
        public virtual void SetKey(int index, KeyframeData key)
        {
            if (keyframeData == null || index < 0 || index >= keyframeData.Length) return;
            keyframeData[index] = key;
        }
        protected Keyframe[] keyframes;
        protected virtual void RefreshKeyframes()
        {
            if (keyframeData == null) return;
            if (keyframes == null || keyframes.Length != keyframeData.Length) keyframes = new Keyframe[keyframeData.Length];

            for (int a = 0; a < keyframes.Length; a++) keyframes[a] = keyframeData[a].state;

            UpdateCurveInstance();
        }
        public void Translate(int keyIndex, Vector3 translation, bool updateState = true)
        {
            if (keyframeData == null || keyIndex < 0 || keyIndex >= keyframeData.Length) return;

            Translate(keyframeData[keyIndex], translation, updateState);
        }
        public void Translate(KeyframeData key, Vector3 translation, bool updateState = true)
        {
            if (key == null) return;
            var rT = key.RectTransform;
            if (rT == null) return;

            rT.position = rT.position + translation;
            ReevaluateKeyframeData(key, false, false, false, false, updateState);
        }
        public virtual void SetKeyframeIndex(KeyframeData key, int index)
        {
            if (key == null) return;
            if (selectedKeys.Remove(key.index)) selectedKeys.Add(index);
            key.index = index;
        }

        protected virtual void UpdateCurveInstance()
        {
            curve.keys = keyframes;
            RebuildCurveMesh();
        }

        private KeyframeTangentSettings[] keyframeTangentSettings;
        public KeyframeTangentSettings[] CurrentTangentSettings
        {
            get
            {
                if (keyframeTangentSettings == null || keyframeTangentSettings.Length != (keyframeData == null ? 0 : keyframeData.Length))
                {
                    keyframeTangentSettings = new KeyframeTangentSettings[keyframeData == null ? 0 : keyframeData.Length];
                }
                if (keyframeData != null) for (int a = 0; a < keyframeData.Length; a++) if (keyframeData[a] != null) keyframeTangentSettings[a] = keyframeData[a].state.tangentSettings;

                return keyframeTangentSettings;
            }

            set
            {
                keyframeTangentSettings = value;
                if (keyframeTangentSettings != null)
                {
                    if (keyframeData != null)
                    {
                        PrepNewState();

                        for (int a = 0; a < Mathf.Min(keyframeTangentSettings.Length, keyframeData.Length); a++)
                        {
                            var key = keyframeData[a];
                            if (key == null) continue;
                            OnKeyEditStart?.Invoke(a);
                            var state = key.state;
                            state.tangentSettings = keyframeTangentSettings[a];
                            key.state = state;
                            OnKeyEditEnd?.Invoke(a, a);
                        }

                        FinalizeState();

                        RefreshKeyframes();
                        Redraw();
                    }
                }
            }
        }

        public int CalculateKeyframeIndex(float timelinePosition)
        {
            if (keyframeData == null) return -1;
            for(int a = 0; a < keyframeData.Length; a++)
            {
                var data = keyframeData[a];

                if (timelinePosition < data.Time) return Mathf.Max(0, a - 1);
                if (timelinePosition == data.Time) return a;
            }

            return keyframeData.Length - 1;
        }
        public int RecalculateKeyframeIndex(int currentIndex, float timelinePosition)
        {
            if (keyframeData == null) return currentIndex;
            for (int a = 0; a < keyframeData.Length; a++)
            {
                if (a == currentIndex) continue;
                var data = keyframeData[a];

                if (timelinePosition < data.Time) return Mathf.Max(0, a - 1);
                if (timelinePosition == data.Time) return a;
            }

            return keyframeData.Length - 1;
        }
        public static float CalculateTimelinePosition(RectTransform timelineTransform, Vector3 localPosition, Vector2 rangeX)
        {
            if (timelineTransform == null) return 0;

            float width = timelineTransform.rect.width;
            return (((localPosition.x + timelineTransform.pivot.x * width) / width) * (rangeX.y - rangeX.x)) + rangeX.x;
        }
        public static float CalculateTimelineValue(RectTransform timelineTransform, Vector3 localPosition, Vector2 rangeY)
        {
            if (timelineTransform == null) return 0;

            float height = timelineTransform.rect.height;
            return (((localPosition.y + timelineTransform.pivot.y * height) / height) * (rangeY.y - rangeY.x)) + rangeY.x;
        }
        public static Vector2 CalculateCoordsInTimelineRect(RectTransform timelineTransform, Vector3 localPosition)
        {
            if (timelineTransform == null) return Vector2.zero;

            float width = timelineTransform.rect.width;
            float height = timelineTransform.rect.height;
            return new Vector2((localPosition.x + timelineTransform.pivot.x * width) / width, (localPosition.y + timelineTransform.pivot.y * height) / height);
        }
        public Vector2 UIPositionToTimelinePosition(Rect timelineRect, Vector3 localUIPos)
        {
            var pos = new Vector2(GetValueInRange(localUIPos.x, new Vector2(timelineRect.min.x, timelineRect.max.x)), GetValueInRange(localUIPos.y, new Vector2(timelineRect.min.y, timelineRect.max.y)));
            pos.x = ValueFromRange(pos.x, rangeX);
            pos.y = ValueFromRange(pos.y, rangeY);
            return pos;
        }

        public static float CalculateLinearTangent(Keyframe key, Keyframe neighbor) => (neighbor.time == key.time) ? 0 : (neighbor.value - key.value) / (neighbor.time - key.time);
        public static Keyframe CalculateLinearInTangent(Keyframe key, Keyframe neighbor)
        {
            key.inWeight = 0;
            key.inTangent = CalculateLinearTangent(key, neighbor);
            return key;
        }
        public static Keyframe CalculateLinearOutTangent(Keyframe key, Keyframe neighbor)
        {
            key.outWeight = 0;
            key.outTangent = CalculateLinearTangent(key, neighbor);
            return key;
        }

        public static float CalculateWeightedFreeTangent(float tangentValue, float keyTime, float keyValue, float neighborTime, float weight) => weight == 0 || neighborTime == keyTime ? 0 : (((tangentValue - keyValue) / (neighborTime - keyTime)) / weight);
        public static float CalculateFreeTangent(Vector2 tangentPos, float keyTime, float keyValue) => CalculateFreeTangent(tangentPos, new Vector2(keyTime, keyValue));
        public static float CalculateFreeTangent(Vector2 tangentPos, Vector2 keyPos) 
        {
            var dir = tangentPos - keyPos;
            return dir.x == 0 ? 0 : (dir.y / dir.x);
        }
        public static Keyframe CalculateWeightedFreeInTangent(Vector2 tangentPos, Keyframe key, float leftNeighborTime)
        {
            key.inWeight = GetValueInRange(tangentPos.x, new Vector2(key.time, leftNeighborTime));
            key.inTangent = CalculateWeightedFreeTangent(tangentPos.y, key.time, key.value, leftNeighborTime, key.inWeight);
            return key;
        }
        public static Keyframe CalculateWeightedFreeOutTangent(Vector2 tangentPos, Keyframe key, float rightNeighborTime)
        {
            key.outWeight = GetValueInRange(tangentPos.x, new Vector2(key.time, rightNeighborTime));
            key.outTangent = CalculateWeightedFreeTangent(tangentPos.y, key.time, key.value, rightNeighborTime, key.outWeight);
            return key;
        }
        public static Keyframe CalculateFreeInTangent(Vector2 tangentPos, Keyframe key, float leftNeighborTime, float tangentTimeThresholdPadding)
        {
            if (key.weightedMode == WeightedMode.Both || key.weightedMode == WeightedMode.In)
            {
                tangentPos.x = Mathf.Clamp(tangentPos.x, leftNeighborTime + tangentTimeThresholdPadding, key.time - tangentTimeThresholdPadding); // Don't let tangent handle move past the left neighboring key or to the right of its own key position
                key = CalculateWeightedFreeInTangent(tangentPos, key, leftNeighborTime);
            }
            else
            {
                tangentPos.x = Mathf.Min(tangentPos.x, key.time - tangentTimeThresholdPadding); // Don't let tangent handle move to the right of its own key position
                key.inTangent = CalculateFreeTangent(tangentPos, key.time, key.value);
            }

            return key;
        }
        public static Keyframe CalculateFreeOutTangent(Vector2 tangentPos, Keyframe key, float rightNeighborTime, float tangentTimeThresholdPadding)
        {
            if (key.weightedMode == WeightedMode.Both || key.weightedMode == WeightedMode.Out) 
            {
                tangentPos.x = Mathf.Clamp(tangentPos.x, key.time + tangentTimeThresholdPadding, rightNeighborTime - tangentTimeThresholdPadding); // Don't let tangent handle move past the right neighboring key or to the left of its own key position
                key = CalculateWeightedFreeOutTangent(tangentPos, key, rightNeighborTime);
            }
            else
            {
                tangentPos.x = Mathf.Max(tangentPos.x, key.time + tangentTimeThresholdPadding); // Don't let tangent handle move to the left of its own key position
                key.outTangent = CalculateFreeTangent(tangentPos, key.time, key.value);
            }

            return key;
        }
        /* orig auto tangent
            float mul = Mathf.Clamp01(Mathf.Abs((GetValueInRange(keyframe.value, leftNeighbor.Value, rightNeighbor.Value) - 0.5f) / 0.5f));
            mul = 1 - (Mathf.Max(0, mul - (1 - clampedAutoFalloff)) / clampedAutoFalloff);
            keyframe.inTangent = ((rightNeighbor.Value - leftNeighbor.Value) / (rightNeighbor.Time - leftNeighbor.Time)) * mul;
         */
        public static float CalculateAutoTangent(float keyVal, float leftNeighborTime, float leftNeighborVal, float rightNeighborTime, float rightNeighborVal, float clampedAutoFalloff = 1/3f)
        {
            float mul = leftNeighborVal == rightNeighborVal ? 0 : Mathf.Clamp01(Mathf.Abs((GetValueInRange(keyVal, leftNeighborVal, rightNeighborVal) - 0.5f) / 0.5f));
            mul = 1 - (Mathf.Max(0, mul - (1 - clampedAutoFalloff)) / clampedAutoFalloff);
            return ((rightNeighborVal - leftNeighborVal) / (rightNeighborTime - leftNeighborTime)) * mul;   
        }
        public static Keyframe CalculateAutoTangents(Keyframe key, Keyframe leftNeighbor, Keyframe rightNeighbor, float clampedAutoFalloff = 1 / 3f)
        {
            key.inTangent = key.outTangent = CalculateAutoTangent(key.value, leftNeighbor.time, leftNeighbor.value, rightNeighbor.time, rightNeighbor.value, clampedAutoFalloff);   
            return key;
        }
        public float GetNonCollidingTime(float time, KeyframeData keyReference = null)
        {
            if (keyframeData == null) return time;

            bool flag = true;
            while(flag)
            {
                flag = false;
                foreach(var key in keyframeData)
                {
                    if (key == null || ReferenceEquals(key, keyReference)) continue;

                    if (key.Time == time) 
                    {
                        flag = true;
                        time += 0.0001f; 
                    }
                }
            }

            return time;
        }

        /// <summary>
        /// Update the keyframe data based on the positioning of its UI elements. Called whenever the user moves a keyframe or moves a tangent handle.
        /// </summary>
        protected virtual void ReevaluateKeyframeData(KeyframeData keyData, bool updateInTangent = false, bool updateOutTangent = false, bool lockInTangent = false, bool lockOutTangent = false, bool updateState = true, bool canPropagateLeft = true, bool canPropagateRight = true, bool overrideTimeValue = false, float newTime = 0)
        {
            if ((keyData.InTangentMode != BrokenTangentMode.Free || keyData.OutTangentMode != BrokenTangentMode.Free) || (lockInTangent && lockOutTangent) || ((keyData.TangentMode == TangentMode.Auto /*|| data.TangentMode == TangentMode.Flat*/) && (lockInTangent || lockOutTangent)))
            {
                keyData.TangentMode = TangentMode.Broken;
            }

            int keyframeIndex = keyData.index;
            int origKeyframeIndex = keyframeIndex;

            if (keyframeIndex < 0) return;

            var keyframe = keyData.Key;
            var oldKeyframe = keyframe;
            var position = keyData.RectTransform.position;
            var localPosition = TimelineTransform.InverseTransformPoint(position);

            bool isSelected = selectedKeys.Contains(keyframeIndex);

            OnKeyEditStart?.Invoke(keyframeIndex);

            if (updateState) PrepNewState();

            if (!overrideTimeValue) newTime = CalculateTimelinePosition(timelineTransform, localPosition, rangeX);

            newTime = GetNonCollidingTime(newTime, keyData); // Make sure the key's time is not the same as any other key's time

            int newIndex = RecalculateKeyframeIndex(keyframeIndex, newTime);
            if (newIndex != keyframeIndex && newIndex >= 0)
            {
                if (isSelected) selectedKeys.Remove(keyframeIndex);
                if (newIndex < keyframeIndex)
                {
                    for (int i = keyframeIndex; i > newIndex; i--) 
                    {
                        keyframeData[i] = keyframeData[i - 1]; // Shift keys right in the array
                        SetKeyframeIndex(keyframeData[i], i);
                    }
                }
                else
                {
                    for (int i = keyframeIndex; i < newIndex; i++)
                    {
                        keyframeData[i] = keyframeData[i + 1]; // Shift keys left in the array
                        SetKeyframeIndex(keyframeData[i], i);
                    }
                }

                keyframeData[newIndex] = keyData;
                keyData.index = newIndex;
                keyframeIndex = newIndex;
                if (isSelected) selectedKeys.Add(keyframeIndex);
            } 

            keyframe.time = newTime;
            keyframe.value = CalculateTimelineValue(timelineTransform, localPosition, rangeY);
            keyframeData[keyframeIndex] = keyData;
            keyData.index = keyframeIndex;
            keyData.Key = keyframe;

            Rect timelineRect = timelineTransform.rect;
            //var keyTimelinePos = UIPositionToTimelinePosition(timelineRect, localPosition);

            KeyframeData leftNeighbor = null;
            KeyframeData rightNeighbor = null;

            if (keyData.index > 0) leftNeighbor = keyframeData[keyData.index - 1];
            if (keyData.index < keyframeData.Length - 1) rightNeighbor = keyframeData[keyData.index + 1];

            float tangentTimeThresholdPadding = (rangeX.y - rangeX.x) * this.tangentTimeThresholdPadding;

            if (leftNeighbor != null)
            {
                updateInTangent = updateInTangent || keyData.TangentMode != TangentMode.Broken || keyData.InTangentMode == BrokenTangentMode.Linear || keyData.InTangentMode == BrokenTangentMode.Constant;
                if (updateInTangent)
                {
                    var leftNeighborTimelinePos = UIPositionToTimelinePosition(timelineRect, timelineTransform.InverseTransformPoint(leftNeighbor.RectTransform.position));
                     
                    switch (keyData.TangentMode)
                    {

                        default:
                            if (keyData.InTangentMode == BrokenTangentMode.Linear)
                            {
                                keyframe = CalculateLinearInTangent(keyframe, leftNeighbor.Key);
                            }
                            else if (keyData.TangentSettings.inTangentMode == BrokenTangentMode.Constant)
                            {
                                keyframe.inTangent = float.PositiveInfinity;
                            }
                            else if (keyData.InTangentMode == BrokenTangentMode.Free)
                            {
                                var tangentPos = UIPositionToTimelinePosition(timelineRect, timelineTransform.InverseTransformPoint(keyData.InTangentRectTransform.position));
                                keyframe = CalculateFreeInTangent(tangentPos, keyframe, leftNeighborTimelinePos.x, tangentTimeThresholdPadding);
                            }
                            break;

                        case TangentMode.Auto:
                            if (rightNeighbor == null)
                            {
                                keyframe.inTangent = 0; 
                            } 
                            else
                            {
                                if (clampedAutoFalloff <= 0) clampedAutoFalloff = 1 / 3f;
                                keyframe.inTangent = CalculateAutoTangent(keyframe.value, leftNeighbor.Time, leftNeighbor.Value, rightNeighbor.Time, rightNeighbor.Value, clampedAutoFalloff);
                            }
                            break;

                        case TangentMode.Smooth:
                            if (lockInTangent)
                            {
                                var tangentPos = UIPositionToTimelinePosition(timelineRect, timelineTransform.InverseTransformPoint(keyData.InTangentRectTransform.position));
                                keyframe = CalculateFreeInTangent(tangentPos, keyframe, leftNeighborTimelinePos.x, tangentTimeThresholdPadding);
                                keyframe.outTangent = keyframe.inTangent;
                            }
                            break;

                        case TangentMode.Flat:
                            if (lockInTangent && !keyData.IsUsingInWeight)
                            {
                                if (keyframe.weightedMode == WeightedMode.None)
                                {
                                    keyframe.weightedMode = WeightedMode.In;
                                } 
                                else
                                {
                                    keyframe.weightedMode = WeightedMode.Both;
                                }
                                keyData.Key = keyframe;
                            }
                            if (!keyData.IsUsingInWeight) 
                            { 
                                keyframe.inWeight = defaultTangentWeight; 
                            }
                            else
                            {
                                var tangentPos = UIPositionToTimelinePosition(timelineRect, timelineTransform.InverseTransformPoint(keyData.InTangentRectTransform.position));
                                tangentPos.x = Mathf.Clamp(tangentPos.x, leftNeighborTimelinePos.x + tangentTimeThresholdPadding, keyframe.time - tangentTimeThresholdPadding);
                                keyframe.inWeight = GetValueInRange(tangentPos.x, new Vector2(keyframe.time, leftNeighborTimelinePos.x));
                            }
                            keyframe.inTangent = 0;
                            break;
                    }
                }
            } 
            if (rightNeighbor != null)
            {
                updateOutTangent = updateOutTangent || keyData.TangentMode != TangentMode.Broken || keyData.OutTangentMode == BrokenTangentMode.Linear || keyData.OutTangentMode == BrokenTangentMode.Constant;
                if (updateOutTangent)
                {
                    var rightNeighborTimelinePos = UIPositionToTimelinePosition(timelineRect, timelineTransform.InverseTransformPoint(rightNeighbor.RectTransform.position));

                    switch (keyData.TangentMode)
                    {

                        default:
                            if (keyData.OutTangentMode == BrokenTangentMode.Linear)
                            {
                                keyframe = CalculateLinearOutTangent(keyframe, rightNeighbor.Key);
                            }
                            else if (keyData.OutTangentMode == BrokenTangentMode.Constant)
                            {
                                keyframe.outTangent = float.PositiveInfinity;
                            }
                            else if (keyData.OutTangentMode == BrokenTangentMode.Free)
                            {
                                var tangentPos = UIPositionToTimelinePosition(timelineRect, timelineTransform.InverseTransformPoint(keyData.OutTangentRectTransform.position));
                                keyframe = CalculateFreeOutTangent(tangentPos, keyframe, rightNeighborTimelinePos.x, tangentTimeThresholdPadding); 
                            }
                            break;

                        case TangentMode.Auto:
                            if (leftNeighbor == null) 
                            { 
                                keyframe.outTangent = 0; 
                            } 
                            else
                            {
                                keyframe.outTangent = keyframe.inTangent;
                            }
                            break;

                        case TangentMode.Smooth:
                            if (lockOutTangent)
                            {
                                var tangentPos = UIPositionToTimelinePosition(timelineRect, timelineTransform.InverseTransformPoint(keyData.OutTangentRectTransform.position));
                                keyframe = CalculateFreeOutTangent(tangentPos, keyframe, rightNeighborTimelinePos.x, tangentTimeThresholdPadding);
                                keyframe.inTangent = keyframe.outTangent;
                            }
                            break;

                        case TangentMode.Flat:
                            if (lockOutTangent && !keyData.IsUsingOutWeight)
                            {
                                if (keyframe.weightedMode == WeightedMode.None)
                                {
                                    keyframe.weightedMode = WeightedMode.Out;
                                }
                                else
                                {
                                    keyframe.weightedMode = WeightedMode.Both;
                                }
                                keyData.Key = keyframe;
                            }
                            if (!keyData.IsUsingOutWeight)
                            {
                                keyframe.outWeight = defaultTangentWeight; 
                            }
                            else
                            {
                                var tangentPos = UIPositionToTimelinePosition(timelineRect, timelineTransform.InverseTransformPoint(keyData.OutTangentRectTransform.position));
                                tangentPos.x = Mathf.Clamp(tangentPos.x, keyframe.time + tangentTimeThresholdPadding, rightNeighborTimelinePos.x - tangentTimeThresholdPadding);
                                keyframe.outWeight = GetValueInRange(tangentPos.x, new Vector2(keyframe.time, rightNeighborTimelinePos.x));
                            }
                            keyframe.outTangent = 0;
                            break;

                    }
                }
            } 

            keyData.Key = keyframe;

            OnKeyEditEnd?.Invoke(origKeyframeIndex, keyframeIndex);

            bool hasMoved = (keyframe.time != oldKeyframe.time || keyframe.value != oldKeyframe.value);
            keyData.UpdateInUI(keyframeData, timelineTransform, rangeX, rangeY, showAllTangents || isSelected, isSelected, unweightedTangentHandleSize); 
            if (leftNeighbor != null && canPropagateLeft) 
            {
                bool skip = false;
                if (leftNeighbor.OutTangentMode == BrokenTangentMode.Linear) 
                { 
                    leftNeighbor.Key = CalculateLinearOutTangent(leftNeighbor.Key, keyframe); 
                } 
                else if (leftNeighbor.TangentMode == TangentMode.Auto && hasMoved)
                {
                    skip = true;
                    ReevaluateKeyframeData(leftNeighbor, false, false, false, false, false, true, false);
                }

                if (!skip)
                {
                    isSelected = IsSelected(leftNeighbor);
                    leftNeighbor.UpdateInUI(keyframeData, timelineTransform, rangeX, rangeY, showAllTangents || isSelected, isSelected, unweightedTangentHandleSize);
                }
            }
            if (rightNeighbor != null && canPropagateRight)  
            {
                bool skip = false;
                if (rightNeighbor.InTangentMode == BrokenTangentMode.Linear) 
                { 
                    rightNeighbor.Key = CalculateLinearInTangent(rightNeighbor.Key, keyframe); 
                } 
                else if (rightNeighbor.TangentMode == TangentMode.Auto && hasMoved)
                {
                    skip = true;
                    ReevaluateKeyframeData(rightNeighbor, false, false, false, false, false, false, true);
                }

                if (!skip)
                {
                    isSelected = IsSelected(rightNeighbor);
                    rightNeighbor.UpdateInUI(keyframeData, timelineTransform, rangeX, rangeY, showAllTangents || isSelected, isSelected, unweightedTangentHandleSize);
                }
            }

            if (origKeyframeIndex != keyframeIndex && origKeyframeIndex >= 0 && origKeyframeIndex < keyframeData.Length)
            {
                var shiftedKey = keyframeData[origKeyframeIndex];
                if (shiftedKey != null && !ReferenceEquals(shiftedKey, keyData)) ReevaluateKeyframeData(shiftedKey, false, false, false, false, false); // Refreshes automatic tangents of the keyframe at the end of an index shift
                 
            }

            if (updateState) 
            {
                FinalizeState();    
            }
            RefreshKeyframes();
        }

        protected virtual void ReevaluateAllKeys(bool updateState = true)
        {
            if (keyframeData == null) return;

            if (updateState) PrepNewState();

            foreach (var key in keyframeData) ReevaluateKeyframeData(key, true, true, false, false, false, false, false);

            if (updateState) 
            {
                FinalizeState();
            }
            RefreshKeyframes();
        }

        protected virtual void RefreshKeyUI(KeyframeData key, bool isSelected, bool onlyUnweightedTangents = false)
        {
            if (key == null) return;

            key.UpdateInUI(keyframeData, timelineTransform, rangeX, rangeY, showAllTangents || isSelected, isSelected, unweightedTangentHandleSize, onlyUnweightedTangents);
        }
         
        /// <summary>
        /// Redraw the entirety of the curve editor (expensive)
        /// </summary>
        public virtual void Redraw()
        {
            rangeX = new Vector2(Mathf.Max(rangeExtremesX.x, rangeX.x), Mathf.Min(rangeExtremesX.y, rangeX.y)); // Prevent infinite zoom out on the x-axis
            rangeY = new Vector2(Mathf.Max(rangeExtremesY.x, rangeY.x), Mathf.Min(rangeExtremesY.y, rangeY.y)); // Prevent infinite zoom out on the y-axis

            float innerOffsetX = Mathf.Abs(rangeX.y - rangeX.x);
            innerOffsetX = Mathf.Max(0, Mathf.Abs(gridMarkerIncrement * 0.1f) - innerOffsetX); // Prevent infinite zoom in on the x-axis
            rangeX.x = rangeX.x - innerOffsetX;
            rangeX.y = rangeX.y + innerOffsetX;

            float innerOffsetY = Mathf.Abs(rangeY.y - rangeY.x);
            innerOffsetY = Mathf.Max(0, Mathf.Abs(gridMarkerIncrement * 0.1f) - innerOffsetY); // Prevent infinite zoom in on the y-axis
            rangeY.x = rangeY.x - innerOffsetY;
            rangeY.y = rangeY.y + innerOffsetY;

            RebuildCurveMesh();
            RedrawGrid();

            if (keyframeData != null)
            {
                foreach (var key in keyframeData)
                {
                    bool isSelected = IsSelected(key);
                    key.UpdateInUI(keyframeData, timelineTransform, rangeX, rangeY, showAllTangents || isSelected, isSelected, unweightedTangentHandleSize);
                }
            }
        }

        protected Vector2 prevTimelineSize;
        protected virtual void OnGUI()
        {
            if (!IsInitialized || timelineTransform == null) return;
            Vector2 timelineSize = timelineTransform.rect.size;
            if (timelineSize != prevTimelineSize)
            {
                prevTimelineSize = timelineSize;
                if (showAllTangents)
                {
                    foreach (var key in keyframeData)
                    {
                        if (key == null) continue;
                        key.UpdateInUI(keyframeData, timelineTransform, rangeX, rangeY, true, IsSelected(key), unweightedTangentHandleSize, true); // Force update tangent handle sizes for unweighted tangents when timeline rect changes size
                    }
                }
                else
                {
                    foreach (int keyIndex in selectedKeys)
                    {
                        if (keyIndex < 0 || keyIndex >= keyframeData.Length) continue;
                        var key = keyframeData[keyIndex];
                        key.UpdateInUI(keyframeData, timelineTransform, rangeX, rangeY, true, true, unweightedTangentHandleSize, true); // Force update tangent handle sizes for unweighted tangents when timeline rect changes size
                    }
                }

                if (clampVisibleGridValuesX != GridValueClampMethod.Off || clampVisibleGridValuesY != GridValueClampMethod.Off) RedrawGridValueMarkers();
            }
        }

        #region Zoom

        public virtual void Zoom(float amount, Vector3 zoomPointWorld)
        {
            ZoomHorizontal(amount, zoomPointWorld, false);
            ZoomVertical(amount, zoomPointWorld, false);
             
            Redraw();
        }
        public virtual void ZoomHorizontal(float amount, Vector3 zoomPointWorld, bool redraw=true)
        {
            if (timelineTransform == null) return;
            amount = amount * (rangeX.y - rangeX.x);

            timelineTransform.GetWorldCorners(fourCornersArray);
            float min = Mathf.Min(fourCornersArray[0].x, fourCornersArray[1].x, fourCornersArray[2].x, fourCornersArray[3].x);
            float max = Mathf.Max(fourCornersArray[0].x, fourCornersArray[1].x, fourCornersArray[2].x, fourCornersArray[3].x);

            float t = Mathf.Clamp01((zoomPointWorld.x - min) / (max - min));
            rangeX = new Vector2(rangeX.x + amount * t, rangeX.y - amount * (1 - t));

            if (redraw) Redraw();
        }
        public virtual void ZoomVertical(float amount, Vector3 zoomPointWorld, bool redraw =true)
        {
            if (timelineTransform == null) return;
            amount = amount * (rangeY.y - rangeY.x);

            timelineTransform.GetWorldCorners(fourCornersArray);
            float min = Mathf.Min(fourCornersArray[0].y, fourCornersArray[1].y, fourCornersArray[2].y, fourCornersArray[3].y);
            float max = Mathf.Max(fourCornersArray[0].y, fourCornersArray[1].y, fourCornersArray[2].y, fourCornersArray[3].y);

            float t = Mathf.Clamp01((zoomPointWorld.y - min) / (max - min));
            rangeY = new Vector2(rangeY.x + amount * t, rangeY.y - amount * (1 - t));

            if (redraw) Redraw();
        }

        public virtual void FocusViewToCurve()
        {
            rangeX = CurveRangeX;
            rangeY = CurveRangeY;

            Redraw();
        }

        public virtual void FocusViewToSelection()
        {
            if (selectedKeys.Count <= 0) return;

            if (keyframeData != null)
            {
                float minX = 0; 
                float maxX = 1;

                float minY = 0;
                float maxY = 1;

                if (selectedKeys.Count == 1) 
                {
                    using (var e = selectedKeys.GetEnumerator())
                    {
                        if (e.MoveNext())
                        {
                            int selectedIndex = e.Current;
                            if (selectedIndex >= 0 && selectedIndex < keyframeData.Length)
                            {
                                var key = keyframeData[selectedIndex];
                                if (key != null)
                                {
                                    minX = maxX = key.Time;
                                    minY = maxY = key.Value;

                                    if (key.index > 0)
                                    {
                                        var leftKey = keyframeData[key.index - 1];
                                        if (leftKey != null)
                                        {
                                            minX = leftKey.Time;
                                            minY = Mathf.Min(leftKey.Value, minY);
                                            maxY = Mathf.Max(leftKey.Value, maxY);
                                        }
                                    }
                                    if (key.index < keyframeData.Length - 1)
                                    {
                                        var rightKey = keyframeData[key.index + 1];
                                        if (rightKey != null)
                                        {
                                            maxX = rightKey.Time;
                                            minY = Mathf.Min(rightKey.Value, minY);
                                            maxY = Mathf.Max(rightKey.Value, maxY);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    minX = float.MaxValue;
                    maxX = float.MinValue;

                    minY = float.MaxValue;
                    maxY = float.MinValue;

                    int count = 0;
                    foreach (var selectedIndex in selectedKeys)
                    {
                        if (selectedIndex < 0 || selectedIndex >= keyframeData.Length) continue;
                        var key = keyframeData[selectedIndex];
                        if (key == null) continue;

                        minX = Mathf.Min(key.Time, minX);
                        maxX = Mathf.Max(key.Time, maxX);

                        minY = Mathf.Min(key.Value, minY);
                        maxY = Mathf.Max(key.Value, maxY); 
                        count++;
                    }
                    if (count <= 0)
                    {
                        minX = 0;
                        maxX = 1;

                        minY = 0;
                        maxY = 1;
                    }
                }

                if (minX == maxX)
                {
                    minX = minX - 0.01f;
                    maxX = maxX + 0.01f;
                }
                if (minY == maxY)
                {
                    minY = minY - 0.01f;
                    maxY = maxY + 0.01f;
                }

                var rect = TimelineTransform.rect;

                float paddingX = 0;
                if (rect.width > 0) paddingX = (curveBoundsPadding.x / rect.width) * (maxX - minX);        
                rangeX = new Vector2(minX - paddingX, maxX + paddingX);

                float paddingY = 0;
                if (rect.height > 0) paddingY = (curveBoundsPadding.y / rect.height) * (maxY - minY);          
                rangeY = new Vector2(minY - paddingY, maxY + paddingY);
            }

            Redraw(); 
        }

        #endregion

        /// <summary>
        /// The logic that is typically called every frame.
        /// </summary>
        public virtual void UpdateStep(float deltaTime) 
        {
            if (!disableNavigation && InputProxy.PressedCurveFocusKey) 
            {
                if (selectedKeys.Count > 0) FocusViewToSelection(); else FocusViewToCurve(); 
            }

            if (!disableInteraction)
            {
                if (InputProxy.PressedDeleteKey) DeleteSelectedKeys();
                if (InputProxy.PressedSelectAllDeselectAllKey) 
                {
                    if (selectedKeys.Count > 0) DeselectAll(); else SelectAll();
                }
            }

            float zoom = InputProxy.Scroll * zoomSensitivity;
            if (zoom != 0 && !disableNavigation)
            {
                var hovered = InputProxy.ObjectsUnderCursor; 
                var transform = this.transform;

                bool flag = false;
                foreach (var obj in hovered)
                {
                    if (obj == null) continue;
                    if (obj == gameObject || obj.transform.IsChildOf(transform))
                    {
                        flag = true;
                        break;
                    }
                } 
                if (flag)
                {
                    bool hor = InputProxy.IsCtrlPressed;
                    bool ver = InputProxy.IsShiftPressed;

                    var canvas = Canvas;
                    if (hor && ver || !hor && !ver)
                    {
                        Zoom(zoom, canvas.transform.TransformPoint(AnimationCurveEditorUtils.ScreenToCanvasPosition(canvas, InputProxy.CursorScreenPosition)));
                    }
                    else if (hor)
                    {
                        ZoomHorizontal(zoom, canvas.transform.TransformPoint(AnimationCurveEditorUtils.ScreenToCanvasPosition(canvas, InputProxy.CursorScreenPosition)));
                    }
                    else if (ver)
                    {
                        ZoomVertical(zoom, canvas.transform.TransformPoint(AnimationCurveEditorUtils.ScreenToCanvasPosition(canvas, InputProxy.CursorScreenPosition))); 
                    }
                }
            }
        }
        protected virtual void Update()
        {
            UpdateStep(Time.deltaTime);
        } 

        protected virtual void SortKeys(List<KeyframeData> list) 
        {
            if (list == null) return;
            list.Sort((KeyframeData x, KeyframeData y) => (int)Mathf.Sign(x.Time - y.Time));
        }

        protected readonly List<KeyframeData> tempKeys = new List<KeyframeData>();
        protected readonly List<KeyframeData> tempKeys2 = new List<KeyframeData>();

        #region State

        [Serializable]
        public struct State : ICloneable
        {
            public int preWrapMode;
            public int postWrapMode;

            public KeyframeStateRaw[] keyframes;
            public int[] selectedKeys;

            public AnimationCurve AsNewAnimationCurve()
            {
                var curve = new AnimationCurve();
                ApplyToAnimationCurve(curve);
                return curve;
            }
            public void ApplyToAnimationCurve(AnimationCurve curve)
            {
                if (curve == null) return;

                curve.preWrapMode = (WrapMode)preWrapMode;
                curve.postWrapMode = (WrapMode)postWrapMode;

                Keyframe[] keys = new Keyframe[keyframes == null ? 0 : keyframes.Length];
                if (keyframes != null)
                {
                    for (int a = 0; a < keyframes.Length; a++) keys[a] = keyframes[a];
                }

                curve.keys = keys;
            }

            public State Duplicate()
            {
                var state = this;
                state.keyframes = keyframes == null ? null : ((KeyframeStateRaw[])keyframes.Clone());
                state.selectedKeys = selectedKeys == null ? null : ((int[])selectedKeys.Clone()); 
                return state;
            }
            public object Clone() => Duplicate();
        }

        protected State currentState;
        protected bool isDirty;
        /// <summary>
        /// Have changes been made to the curve since the last state query?
        /// </summary>
        public virtual bool IsDirty => isDirty;
        public virtual void SetDirty() => isDirty = true; 
        public virtual State GetState()
        {
            if (currentState.keyframes != null && !isDirty) return currentState;

            var state = new State();

            if (curve != null)
            {
                state.preWrapMode = (int)curve.preWrapMode;
                state.postWrapMode = (int)curve.postWrapMode;
            }
            if (keyframeData != null)
            {
                state.keyframes = new KeyframeStateRaw[keyframeData.Length];
                for (int a = 0; a < keyframeData.Length; a++) state.keyframes[a] = keyframeData[a].state;
            }
            if (selectedKeys.Count > 0)
            {
                state.selectedKeys = new int[selectedKeys.Count];
                selectedKeys.CopyTo(state.selectedKeys);
            }

            isDirty = false;
            currentState = state;
            return state;
        }
        public virtual void SetState(State state, bool notifyListeners = false, bool redraw = true)
        {
            State preState = default;
            if (notifyListeners) preState = CurrentState;

            if (keyframeData != null)
            {
                foreach (var data in keyframeData)
                {
                    if (data == null) continue;
                    data.Destroy(keyframePool, tangentPool, tangentLinePool);
                }
            }
            selectedKeys.Clear();
            keyframes = null;

            if (curve == null) curve = new AnimationCurve();

            curve.preWrapMode = (WrapMode)state.preWrapMode;
            curve.postWrapMode = (WrapMode)state.postWrapMode;

            if (state.selectedKeys != null) foreach (var index in state.selectedKeys) selectedKeys.Add(index);

            keyframes = new Keyframe[state.keyframes == null ? 0 : state.keyframes.Length];
            if (state.keyframes != null) for (int a = 0; a < keyframes.Length; a++) keyframes[a] = state.keyframes[a];
            curve.keys = keyframes;

            keyframeData = new KeyframeData[keyframes.Length];
            for (int a = 0; a < keyframes.Length; a++) keyframeData[a] = CreateNewKeyframeData(keyframes[a], a, state.keyframes[a], false);

            currentState = state;
            isDirty = false;

            if (notifyListeners) OnStateChange?.Invoke(preState, state); 

            if (redraw) Redraw();
        }
        public virtual State CurrentState
        {
            get => GetState();
            set => SetState(value);
        }

        protected State oldState;
        protected virtual void PrepNewState()
        {
            oldState = default;
            if (OnStateChange != null) oldState = CurrentState;

            SetDirty();
        }
        protected virtual void FinalizeState()
        {
            if (OnStateChange != null)
            {
                var newState = CurrentState;
                OnStateChange.Invoke(oldState, newState);
            }
        }

        protected virtual void PrepKeyCountEdit()
        {
            if (keyframeData == null) return;

            tempKeys2.Clear();
            foreach (var selectedIndex in selectedKeys)
            {
                if (selectedIndex < 0 || selectedIndex >= keyframeData.Length) continue;
                var key = keyframeData[selectedIndex];
                if (key == null) continue;
                tempKeys2.Add(key);
            }
            selectedKeys.Clear();
        }

        protected virtual void FinalizeKeyCountEdit()
        {
            foreach (var key in tempKeys2) if (key.index >= 0) selectedKeys.Add(key.index);
        }

        #endregion

        #region Events

        [Header("Events")]
        public UnityEvent<State, State> OnStateChange = new UnityEvent<State, State>();

        public UnityEvent<int> OnKeyEditStart = new UnityEvent<int>();
        public UnityEvent<int, int> OnKeyEditEnd = new UnityEvent<int, int>();

        public UnityEvent<int> OnKeyAdd = new UnityEvent<int>();
        public UnityEvent<int> OnKeyDelete = new UnityEvent<int>();

        #endregion

        #region Actions

        public virtual void PanWorld(Vector2 translationWorld) => PanLocal(TimelineTransform.InverseTransformVector(translationWorld));
        public virtual void PanLocal(Vector2 translationLocal) 
        {
            var rect = timelineTransform.rect;

            if (rect.width > 0) translationLocal.x = translationLocal.x / rect.width; else translationLocal.x = 0;
            if (rect.height > 0) translationLocal.y = translationLocal.y / rect.height; else translationLocal.y = 0;

            PanVisible(translationLocal);
        }
        public virtual void PanVisible(Vector2 translationNormalized)
        {
            rangeX += new Vector2(translationNormalized.x, translationNormalized.x) * (rangeX.y - rangeX.x);
            rangeY += new Vector2(translationNormalized.y, translationNormalized.y) * (rangeY.y - rangeY.x);

            Redraw();
        }

        public virtual void PanUpWorld(float amount) => PanWorld(new Vector2(0, amount));
        public virtual void PanUpLocal(float amount) => PanLocal(new Vector2(0, amount));
        public virtual void PanUpVisible(float amount) => PanVisible(new Vector2(0, amount));

        public virtual void PanDownWorld(float amount) => PanWorld(new Vector2(0, -amount));
        public virtual void PanDownLocal(float amount) => PanLocal(new Vector2(0, -amount));
        public virtual void PanDownVisible(float amount) => PanVisible(new Vector2(0, -amount)); 

        public virtual void PanLeftWorld(float amount) => PanWorld(new Vector2(-amount, 0));
        public virtual void PanLeftLocal(float amount) => PanLocal(new Vector2(-amount, 0));
        public virtual void PanLeftVisible(float amount) => PanVisible(new Vector2(-amount, 0));

        public virtual void PanRightWorld(float amount) => PanWorld(new Vector2(amount, 0));
        public virtual void PanRightLocal(float amount) => PanLocal(new Vector2(amount, 0));
        public virtual void PanRightVisible(float amount) => PanVisible(new Vector2(amount, 0));

        public static string _timeTextComponentName = "Time";
        public static string _valueTextComponentName = "Value";
        public virtual void ShowKeyCoords(Vector3 worldPosition, float time, float value)
        {
            if (keyDragDisplayTransform == null) return;

            if (string.IsNullOrWhiteSpace(keyCoordinatesStringFormat)) keyCoordinatesStringFormat = "0.#####";

            keyDragDisplayTransform.gameObject.SetActive(true);
            keyDragDisplayTransform.position = worldPosition;

            var timeText = AnimationCurveEditorUtils.FindNestedChild(keyDragDisplayTransform, _timeTextComponentName);
            if (timeText != null) 
            {
                AnimationCurveEditorUtils.SetComponentText(timeText, time.ToString(keyCoordinatesStringFormat));
            }

            var valueText = AnimationCurveEditorUtils.FindNestedChild(keyDragDisplayTransform, _valueTextComponentName);
            if (valueText != null)
            {
                AnimationCurveEditorUtils.SetComponentText(valueText, value.ToString(keyCoordinatesStringFormat));
            }
        }

        public virtual void HideKeyCoords()
        {
            if (keyDragDisplayTransform == null) return;
            keyDragDisplayTransform.gameObject.SetActive(false);
        }

        public virtual void MoveElementIntoView(RectTransform transform)
        {
            TimelineTransform.GetLocalCorners(fourCornersArray);
            Vector2 min = new Vector2(Mathf.Min(fourCornersArray[0].x, fourCornersArray[1].x, fourCornersArray[2].x, fourCornersArray[3].x), Mathf.Min(fourCornersArray[0].y, fourCornersArray[1].y, fourCornersArray[2].y, fourCornersArray[3].y));
            Vector2 max = new Vector2(Mathf.Max(fourCornersArray[0].x, fourCornersArray[1].x, fourCornersArray[2].x, fourCornersArray[3].x), Mathf.Max(fourCornersArray[0].y, fourCornersArray[1].y, fourCornersArray[2].y, fourCornersArray[3].y));

            transform.GetWorldCorners(fourCornersArray);
            fourCornersArray[0] = timelineTransform.InverseTransformPoint(fourCornersArray[0]);
            fourCornersArray[1] = timelineTransform.InverseTransformPoint(fourCornersArray[1]);
            fourCornersArray[2] = timelineTransform.InverseTransformPoint(fourCornersArray[2]);
            fourCornersArray[3] = timelineTransform.InverseTransformPoint(fourCornersArray[3]);
            Vector2 min_element = new Vector2(Mathf.Min(fourCornersArray[0].x, fourCornersArray[1].x, fourCornersArray[2].x, fourCornersArray[3].x), Mathf.Min(fourCornersArray[0].y, fourCornersArray[1].y, fourCornersArray[2].y, fourCornersArray[3].y));
            Vector2 max_element = new Vector2(Mathf.Max(fourCornersArray[0].x, fourCornersArray[1].x, fourCornersArray[2].x, fourCornersArray[3].x), Mathf.Max(fourCornersArray[0].y, fourCornersArray[1].y, fourCornersArray[2].y, fourCornersArray[3].y));

            Vector3 pos = timelineTransform.InverseTransformPoint(transform.position);

            float offset;

            offset = Mathf.Min(max.x - max_element.x, 0);
            pos = pos + new Vector3(offset, 0);
            min_element.x += offset;
            //max_element.x += offset;

            offset = Mathf.Max(min.y - min_element.y, 0);
            pos = pos + new Vector3(0, offset);
            //min_element.y += offset;
            max_element.y += offset;

            offset = Mathf.Max(min.x - min_element.x, 0);
            pos = pos + new Vector3(offset, 0);
            //min_element.x += offset;
            //max_element.x += offset;

            offset = Mathf.Min(max.y - max_element.y, 0);
            pos = pos + new Vector3(0, offset);
            //min_element.y += offset;
            //max_element.y += offset;

            transform.position = timelineTransform.TransformPoint(pos);
        }

        public virtual void CloseAllMenus()
        {
            CloseEditKeyMenu();
            CloseEditTangentMenu();
            CloseContextMenu();
        }

        public virtual void CloseEditKeyMenu()
        {
            if (editKeyMenuTransform != null) editKeyMenuTransform.gameObject.SetActive(false);
        }
        public virtual void CloseEditTangentMenu()
        {
            if (editTangentMenuTransform != null) editTangentMenuTransform.gameObject.SetActive(false);
        }
        public virtual void CloseContextMenu()
        {
            if (contextMenuTransform != null) contextMenuTransform.gameObject.SetActive(false);
        }

        [NonSerialized]
        protected KeyframeData lastMenuEditedKey;
        public static string _deleteKeyComponentName = "DeleteKey";
        public static string _timeInputComponentName = "Time";
        public static string _valueInputComponentName = "Value";
        public static string _weightInputComponentName = "Weight";
        public static string _tangentModeDropdownComponentName = "TangentMode";
        public static string _brokenTangentModeDropdownComponentName = _tangentModeDropdownComponentName;
        public static string _inTangentModeDropdownComponentName = "InTangentMode";
        public static string _outTangentModeDropdownComponentName = "OutTangentMode";
        public static string _inTangentWeightedToggleComponentName = "InWeighted";
        public static string _outTangentWeightedToggleComponentName = "OutWeighted";
        public static string _tangentSideComponentName = "TangentSide";
        public static string _isWeightedComponentName = "IsWeighted";

        public static string _tangentSideInName = "IN";
        public static string _tangentSideOutName = "OUT";

        public static List<TMP_Dropdown.OptionData> _tangentModeDropdownOptions = new List<TMP_Dropdown.OptionData>() 
        { 
            new TMP_Dropdown.OptionData(TangentMode.Broken.ToString().ToUpper()),
            new TMP_Dropdown.OptionData(TangentMode.Auto.ToString().ToUpper()),
            new TMP_Dropdown.OptionData(TangentMode.Smooth.ToString().ToUpper()),
            new TMP_Dropdown.OptionData(TangentMode.Flat.ToString().ToUpper())
        };

        public static List<TMP_Dropdown.OptionData> _brokenTangentModeDropdownOptions = new List<TMP_Dropdown.OptionData>()
        {
            new TMP_Dropdown.OptionData(BrokenTangentMode.Free.ToString().ToUpper()),
            new TMP_Dropdown.OptionData(BrokenTangentMode.Linear.ToString().ToUpper()),
            new TMP_Dropdown.OptionData(BrokenTangentMode.Constant.ToString().ToUpper()),
        };

        protected delegate void SetKeyDataDelegate(KeyframeData key);
        public virtual void OpenEditKeyMenu(int keyIndex, bool fitInWindow = true) => OpenEditKeyMenu(keyframeData == null || keyIndex < 0 || keyIndex >= keyframeData.Length ? null : keyframeData[keyIndex], fitInWindow);
        public virtual void OpenEditKeyMenu(int keyIndex, Vector3 worldPosition, bool fitInWindow = true) => OpenEditKeyMenu(keyframeData == null || keyIndex < 0 || keyIndex >= keyframeData.Length ? null : keyframeData[keyIndex], worldPosition, fitInWindow);
        public virtual void OpenEditKeyMenu(KeyframeData key, bool fitInWindow = true) => OpenEditKeyMenu(key, key == null || key.RectTransform == null ? TimelineTransform.position : key.RectTransform.position, fitInWindow);
        public virtual void OpenEditKeyMenu(KeyframeData key, Vector3 worldPosition, bool fitInWindow = true) 
        {
            if (key == null || editKeyMenuTransform == null) return;

            editKeyMenuTransform.gameObject.SetActive(true);
            editKeyMenuTransform.position = worldPosition;
            if (fitInWindow) MoveElementIntoView(editKeyMenuTransform);

            lastMenuEditedKey = key;

            void ReevaluateTargetAndSelected(SetKeyDataDelegate setData = null)
            {
                if (lastMenuEditedKey == null) return;

                bool isSelected = IsSelected(lastMenuEditedKey);
                if (!isSelected)
                {
                    setData?.Invoke(lastMenuEditedKey);
                    lastMenuEditedKey.UpdateInUI(keyframeData, TimelineTransform, rangeX, rangeY, showAllTangents, false, unweightedTangentHandleSize);
                    ReevaluateKeyframeData(lastMenuEditedKey, false, false, false, false, false);
                }

                ApplyToSelectedKeysSafe((KeyframeData key) =>
                {
                    setData?.Invoke(key);
                    key.UpdateInUI(keyframeData, TimelineTransform, rangeX, rangeY, true, true, unweightedTangentHandleSize); 
                    ReevaluateKeyframeData(key, false, false, false, false, false);
                });

            }
            void RefreshMenu()
            {
                if (lastMenuEditedKey == null)
                {
                    CloseEditKeyMenu();
                }
                else
                {
                    OpenEditKeyMenu(lastMenuEditedKey, editKeyMenuTransform.position, false);
                }
            }

            var deleteKey = AnimationCurveEditorUtils.FindNestedChild(editKeyMenuTransform, _deleteKeyComponentName);
            if (deleteKey != null)
            {
                AnimationCurveEditorUtils.SetButtonOnClickAction(deleteKey, () =>
                {
                    DeleteKey(lastMenuEditedKey);
                    lastMenuEditedKey = null;
                    CloseEditKeyMenu();
                });
            }

            var timeInput = AnimationCurveEditorUtils.FindNestedChild(editKeyMenuTransform, _timeInputComponentName);
            if (timeInput != null)
            {
                AnimationCurveEditorUtils.SetInputFieldOnValueChangeAction(timeInput, (string timeStr) => {
                    if (lastMenuEditedKey == null || string.IsNullOrEmpty(timeStr) || timeStr.EndsWith('.')) return; 

                    if (float.TryParse(timeStr, out float time))
                    {
                        ReevaluateKeyframeData(lastMenuEditedKey, false, false, false, false, true, true, true, true, time);
                        
                        RefreshMenu();
                    } 
                });
                AnimationCurveEditorUtils.SetInputFieldText(timeInput, lastMenuEditedKey.Time.ToString());
            }

            var valueInput = AnimationCurveEditorUtils.FindNestedChild(editKeyMenuTransform, _valueInputComponentName);
            if (valueInput != null)
            {
                AnimationCurveEditorUtils.SetInputFieldOnValueChangeAction(valueInput, (string valueStr) => {
                    if (lastMenuEditedKey == null || string.IsNullOrEmpty(valueStr) || valueStr.EndsWith('.')) return;

                    if (float.TryParse(valueStr, out float value))
                    {
                        PrepNewState();

                        ReevaluateTargetAndSelected((KeyframeData key) => key.Value = value);

                        FinalizeState();
                        RefreshKeyframes();
                    }
                });
                AnimationCurveEditorUtils.SetInputFieldText(valueInput, lastMenuEditedKey.Value.ToString());
            }

            var tangentMode = AnimationCurveEditorUtils.FindNestedChild(editKeyMenuTransform, _tangentModeDropdownComponentName);
            if (tangentMode != null)
            {
                AnimationCurveEditorUtils.SetDropdownOnValueChangeAction(tangentMode, (int optionIndex, string option) => {
                    if (lastMenuEditedKey == null) return;

                    PrepNewState();

                    ReevaluateTargetAndSelected((KeyframeData key) => key.TangentMode = (TangentMode)optionIndex);

                    FinalizeState();
                    RefreshKeyframes();

                    RefreshMenu();
                });
                AnimationCurveEditorUtils.SetDropdownOptions(tangentMode, _tangentModeDropdownOptions);
                AnimationCurveEditorUtils.SetSelectedDropdownOption(tangentMode, (int)lastMenuEditedKey.TangentMode);
            }

            var inTangentMode = AnimationCurveEditorUtils.FindNestedChild(editKeyMenuTransform, _inTangentModeDropdownComponentName);
            if (inTangentMode != null)
            {
                AnimationCurveEditorUtils.SetDropdownOnValueChangeAction(inTangentMode, (int optionIndex, string option) => {
                    if (lastMenuEditedKey == null) return;

                    PrepNewState();

                    ReevaluateTargetAndSelected((KeyframeData key) => 
                    {
                        var prevMode = key.InTangentMode;
                        key.InTangentMode = (BrokenTangentMode)optionIndex;
                        if (prevMode == BrokenTangentMode.Linear && key.InTangentMode == BrokenTangentMode.Free) key.InTangentWeight = defaultTangentWeight;
                    });

                    FinalizeState();
                    RefreshKeyframes();

                    RefreshMenu();
                });
                AnimationCurveEditorUtils.SetDropdownOptions(inTangentMode, _brokenTangentModeDropdownOptions);
                AnimationCurveEditorUtils.SetSelectedDropdownOption(inTangentMode, (int)lastMenuEditedKey.InTangentMode);
            }

            var outTangentMode = AnimationCurveEditorUtils.FindNestedChild(editKeyMenuTransform, _outTangentModeDropdownComponentName);
            if (outTangentMode != null)
            {
                AnimationCurveEditorUtils.SetDropdownOnValueChangeAction(outTangentMode, (int optionIndex, string option) => {
                    if (lastMenuEditedKey == null) return;

                    PrepNewState();

                    ReevaluateTargetAndSelected((KeyframeData key) =>
                    {
                        var prevMode = key.OutTangentMode;
                        key.OutTangentMode = (BrokenTangentMode)optionIndex;
                        if (prevMode == BrokenTangentMode.Linear && key.OutTangentMode == BrokenTangentMode.Free) key.OutTangentWeight = defaultTangentWeight;
                    });

                    FinalizeState();
                    RefreshKeyframes();

                    RefreshMenu();
                });
                AnimationCurveEditorUtils.SetDropdownOptions(outTangentMode, _brokenTangentModeDropdownOptions);
                AnimationCurveEditorUtils.SetSelectedDropdownOption(outTangentMode, (int)lastMenuEditedKey.OutTangentMode);
            }

            var inWeighted = AnimationCurveEditorUtils.FindNestedChild(editKeyMenuTransform, _inTangentWeightedToggleComponentName);
            if (inWeighted != null)
            {
                AnimationCurveEditorUtils.SetToggleOnValueChangeAction(inWeighted, (bool isWeighted) => {
                    if (lastMenuEditedKey == null) return;

                    PrepNewState();

                    ReevaluateTargetAndSelected((KeyframeData key) => key.IsUsingInWeight = isWeighted);

                    FinalizeState();
                    RefreshKeyframes();

                    RefreshMenu();
                });
                AnimationCurveEditorUtils.SetToggleValue(inWeighted, lastMenuEditedKey.IsUsingInWeight);
            }

            var outWeighted = AnimationCurveEditorUtils.FindNestedChild(editKeyMenuTransform, _outTangentWeightedToggleComponentName);
            if (outWeighted != null)
            {
                AnimationCurveEditorUtils.SetToggleOnValueChangeAction(outWeighted, (bool isWeighted) => { 
                    if (lastMenuEditedKey == null) return;

                    PrepNewState();

                    ReevaluateTargetAndSelected((KeyframeData key) => key.IsUsingOutWeight = isWeighted);

                    FinalizeState();
                    RefreshKeyframes();

                    RefreshMenu(); 
                });
                AnimationCurveEditorUtils.SetToggleValue(outWeighted, lastMenuEditedKey.IsUsingOutWeight);
            }
        }
        [NonSerialized]
        protected bool editInTangentFromMenu;
        public virtual void OpenEditTangentMenu(int keyIndex, bool inTangent, bool fitInWindow = true) => OpenEditTangentMenu(keyframeData == null || keyIndex < 0 || keyIndex >= keyframeData.Length ? null : keyframeData[keyIndex], inTangent, fitInWindow);
        public virtual void OpenEditTangentMenu(int keyIndex, bool inTangent, Vector3 worldPosition, bool fitInWindow = true) => OpenEditTangentMenu(keyframeData == null || keyIndex < 0 || keyIndex >= keyframeData.Length ? null : keyframeData[keyIndex], inTangent, worldPosition, fitInWindow);
        public virtual void OpenEditTangentMenu(KeyframeData key, bool inTangent, bool fitInWindow = true) => OpenEditTangentMenu(key, inTangent, key == null || (inTangent ? key.InTangentRectTransform == null : key.OutTangentRectTransform == null) ? (key == null || key.RectTransform == null ? TimelineTransform.position : key.RectTransform.position) : (inTangent ? key.InTangentRectTransform.position : key.OutTangentRectTransform.position), fitInWindow);
        public virtual void OpenEditTangentMenu(KeyframeData key, bool inTangent, Vector3 worldPosition, bool fitInWindow = true)
        {
            if (key == null || editTangentMenuTransform == null) return;

            editTangentMenuTransform.gameObject.SetActive(true);
            editTangentMenuTransform.position = worldPosition;
            if (fitInWindow) MoveElementIntoView(editTangentMenuTransform); 

            lastMenuEditedKey = key;
            editInTangentFromMenu = inTangent;

            void ReevaluateTarget(SetKeyDataDelegate setData = null)
            {
                if (lastMenuEditedKey == null) return;

                setData?.Invoke(lastMenuEditedKey);

                bool isSelected = IsSelected(lastMenuEditedKey);
                lastMenuEditedKey.UpdateInUI(keyframeData, TimelineTransform, rangeX, rangeY, showAllTangents || isSelected, isSelected, unweightedTangentHandleSize);
                ReevaluateKeyframeData(lastMenuEditedKey, false, false, false, false, false);


            }
            void RefreshMenu()
            {
                if (lastMenuEditedKey == null)
                {
                    CloseEditKeyMenu();
                }
                else
                {
                    OpenEditTangentMenu(lastMenuEditedKey, inTangent, editTangentMenuTransform.position, false);
                }
            }

            var valueInput = AnimationCurveEditorUtils.FindNestedChild(editTangentMenuTransform, _valueInputComponentName);
            if (valueInput != null)
            {
                AnimationCurveEditorUtils.SetInputFieldOnValueChangeAction(valueInput, (string valueStr) =>
                {
                    if (lastMenuEditedKey == null || string.IsNullOrEmpty(valueStr) || valueStr.EndsWith('.')) return;

                    if (float.TryParse(valueStr, out float value))
                    {
                        PrepNewState();

                        ReevaluateTarget((KeyframeData key) => 
                        { 
                            if (inTangent)
                            {
                                if (float.IsFinite(value))
                                {
                                    if (Mathf.Abs(value) >= 9999)
                                    {
                                        key.InTangentMode = BrokenTangentMode.Constant;
                                    }
                                    else
                                    {
                                        key.InTangent = Mathf.Clamp(value, -9999, 9999);
                                        if (key.TangentMode == TangentMode.Smooth)
                                        {
                                            key.OutTangent = key.InTangent;
                                        }
                                        else
                                        {
                                            key.TangentMode = TangentMode.Broken;
                                        }
                                    }
                                }
                                else key.InTangentMode = BrokenTangentMode.Constant;
                            } 
                            else
                            {
                                if (float.IsFinite(value))
                                {
                                    if (Mathf.Abs(value) >= 9999)
                                    {
                                        key.OutTangentMode = BrokenTangentMode.Constant; 
                                    }
                                    else
                                    {
                                        key.OutTangent = Mathf.Clamp(value, -9999, 9999);
                                        if (key.TangentMode == TangentMode.Smooth)
                                        {
                                            key.InTangent = key.OutTangent;
                                        }
                                        else
                                        {
                                            key.TangentMode = TangentMode.Broken;
                                        }
                                    }
                                }
                                else key.OutTangentMode = BrokenTangentMode.Constant;
                            }
                        });

                        FinalizeState();
                        RefreshKeyframes();

                        RefreshMenu();
                    }
                });
                AnimationCurveEditorUtils.SetInputFieldText(valueInput, (inTangent ? lastMenuEditedKey.InTangent : lastMenuEditedKey.OutTangent).ToString());
            }

            var weightInput = AnimationCurveEditorUtils.FindNestedChild(editTangentMenuTransform, _weightInputComponentName);
            if (weightInput != null)
            {
                AnimationCurveEditorUtils.SetInputFieldOnValueChangeAction(weightInput, (string weightStr) =>
                {
                    if (lastMenuEditedKey == null || string.IsNullOrEmpty(weightStr) || weightStr.EndsWith('.')) return;

                    if (float.TryParse(weightStr, out float weight))
                    {
                        PrepNewState();

                        ReevaluateTarget((KeyframeData key) =>
                        {
                            if (inTangent)
                            {
                                key.IsUsingInWeight = true;
                                key.InTangentWeight = Mathf.Clamp01(weight);
                            }
                            else
                            {
                                key.IsUsingOutWeight = true;
                                key.OutTangentWeight = Mathf.Clamp01(weight);
                            }
                        });

                        FinalizeState();
                        RefreshKeyframes();

                        RefreshMenu();
                    }
                });
                AnimationCurveEditorUtils.SetInputFieldText(weightInput, (inTangent ? lastMenuEditedKey.InTangentWeight : lastMenuEditedKey.OutTangentWeight).ToString());
            }

            var brokenTangentMode = AnimationCurveEditorUtils.FindNestedChild(editTangentMenuTransform, _brokenTangentModeDropdownComponentName);
            if (brokenTangentMode != null)
            {
                AnimationCurveEditorUtils.SetDropdownOnValueChangeAction(brokenTangentMode, (int optionIndex, string option) =>
                {
                    if (lastMenuEditedKey == null) return;

                    PrepNewState();

                    ReevaluateTarget((KeyframeData key) =>
                    {
                        if (inTangent)
                        {
                            var prevMode = key.InTangentMode;
                            key.InTangentMode = (BrokenTangentMode)optionIndex; 
                            if (prevMode == BrokenTangentMode.Linear && key.InTangentMode == BrokenTangentMode.Free) key.InTangentWeight = defaultTangentWeight; 
                        }
                        else
                        {
                            var prevMode = key.OutTangentMode;
                            key.OutTangentMode = (BrokenTangentMode)optionIndex;
                            if (prevMode == BrokenTangentMode.Linear && key.OutTangentMode == BrokenTangentMode.Free) key.OutTangentWeight = defaultTangentWeight;
                        }
                    });

                    FinalizeState();
                    RefreshKeyframes();

                    RefreshMenu();
                });
                AnimationCurveEditorUtils.SetDropdownOptions(brokenTangentMode, _brokenTangentModeDropdownOptions);
                AnimationCurveEditorUtils.SetSelectedDropdownOption(brokenTangentMode, (int)(inTangent ? lastMenuEditedKey.InTangentMode : lastMenuEditedKey.OutTangentMode));
            }

            var isWeighted = AnimationCurveEditorUtils.FindNestedChild(editTangentMenuTransform, _isWeightedComponentName); 
            if (isWeighted != null)
            {
                AnimationCurveEditorUtils.SetToggleOnValueChangeAction(isWeighted, (bool isWeighted) =>
                {
                    if (lastMenuEditedKey == null) return;

                    PrepNewState();

                    ReevaluateTarget((KeyframeData key) =>
                    {
                        if (inTangent)
                        {
                            key.IsUsingInWeight = isWeighted;
                        }
                        else
                        {
                            key.IsUsingOutWeight = isWeighted;
                        }
                    });

                    FinalizeState();
                    RefreshKeyframes();

                    RefreshMenu();
                });
                AnimationCurveEditorUtils.SetToggleValue(isWeighted, (inTangent ? lastMenuEditedKey.IsUsingInWeight : lastMenuEditedKey.IsUsingOutWeight));
            }

            var tangentSide = AnimationCurveEditorUtils.FindNestedChild(editTangentMenuTransform, _tangentSideComponentName);
            if (tangentSide != null)
            {
                for (int a = 0; a < tangentSide.childCount; a++)
                {
                    var child = tangentSide.GetChild(a);
                    if (child == null || child == isWeighted) continue;

                    AnimationCurveEditorUtils.SetComponentText(child, inTangent ? _tangentSideInName : _tangentSideOutName);
                }
            }

        }

        public static string _addKeyButtonName = "AddKey";
        public virtual void OpenContextMenu(Vector3 clickPosWorld, bool fitInWindow = true)
        {
            if (disableInteraction || contextMenuTransform == null) return;

            TimelineTransform.GetWorldCorners(fourCornersArray);
            Vector2 min = new Vector2(Mathf.Min(fourCornersArray[0].x, fourCornersArray[1].x, fourCornersArray[2].x, fourCornersArray[3].x), Mathf.Min(fourCornersArray[0].y, fourCornersArray[1].y, fourCornersArray[2].y, fourCornersArray[3].y));
            Vector2 max = new Vector2(Mathf.Max(fourCornersArray[0].x, fourCornersArray[1].x, fourCornersArray[2].x, fourCornersArray[3].x), Mathf.Max(fourCornersArray[0].y, fourCornersArray[1].y, fourCornersArray[2].y, fourCornersArray[3].y));

            contextMenuTransform.gameObject.SetActive(true);
            Vector3 finalPos = clickPosWorld;
            if (fitInWindow)
            {
                contextMenuTransform.position = clickPosWorld;
                MoveElementIntoView(contextMenuTransform);
                finalPos = contextMenuTransform.position;
            }
            contextMenuTransform.anchorMin = contextMenuTransform.anchorMax = new Vector2((finalPos.x - min.x) / (max.x - min.x), (finalPos.y - min.y) / (max.y - min.y));
            contextMenuTransform.position = finalPos;

            var addKey = AnimationCurveEditorUtils.FindNestedChild(contextMenuTransform, _addKeyButtonName);
            if (addKey != null)
            {
                AnimationCurveEditorUtils.SetButtonOnClickAction(addKey, () =>
                {
                    AddKeyFromWorldPosition(clickPosWorld);
                    CloseContextMenu();

                    Redraw();
                });
            }
        }

        public virtual void AddKeyByTransformWorldPosition(Transform transform) => AddKey(transform);     
        public virtual int AddKey(Transform transform)
        {
            if (transform == null) return -1;
            return AddKeyFromWorldPosition(transform.position);  
        }
        public virtual int AddKeyFromWorldPosition(Vector3 worldPosition)
        {
            Vector3 localPos = TimelineTransform.InverseTransformPoint(worldPosition);

            float time = CalculateTimelinePosition(timelineTransform, localPos, rangeX);
            float value = CalculateTimelineValue(timelineTransform, localPos, rangeY);

            return AddKey(time, value); 
        }
        public virtual int AddKey(float time, float value) => AddKey(new Keyframe() { time = time, value = value, inWeight = 1/3f, outWeight = 1/3f, inTangent = 0, outTangent = 0, weightedMode = WeightedMode.Both });
        public virtual int AddKey(Keyframe data)
        {
            int index = -1;

            PrepNewState();
            PrepKeyCountEdit();

            var tangentSettings = KeyframeTangentSettings.Default;
            tangentSettings.tangentMode = newKeyTangentMode;
            if (keyframeData != null)
            {
                tempKeys.Clear();
                tempKeys.AddRange(keyframeData);

                var key = CreateNewKeyframeData(data, -1, tangentSettings);
                tempKeys.Add(key);
                
                tempKeys.RemoveAll(i => i == null);
                SortKeys(tempKeys);
                for (int a = 0; a < tempKeys.Count; a++) tempKeys[a].index = a;
                keyframeData = tempKeys.ToArray();

                index = key.index;
            }
            else
            {
                index = 0; 
                keyframeData = new KeyframeData[] { CreateNewKeyframeData(data, 0, tangentSettings) };
            }

            FinalizeKeyCountEdit();
            FinalizeState();
            RefreshKeyframes();

            OnKeyAdd?.Invoke(index);

            return index;
        }

        public virtual void DeleteSelectedKeys(bool redraw = true)
        {
            if (selectedKeys.Count == 0) return;

            PrepNewState();

            tempKeys.Clear();
            tempKeys.AddRange(keyframeData);

            foreach (var index in selectedKeys) 
            {
                if (index < 0 || index >= keyframeData.Length) continue;
                var key = keyframeData[index];
                if (key == null) continue;
                OnKeyDelete?.Invoke(index);

                tempKeys.RemoveAll(i => ReferenceEquals(i, key));

                key.index = -1;
                key.Destroy(keyframePool, tangentPool, tangentLinePool);
            }
            selectedKeys.Clear();

            tempKeys.RemoveAll(i => i == null);
            SortKeys(tempKeys);
            for (int a = 0; a < tempKeys.Count; a++) tempKeys[a].index = a;
            keyframeData = tempKeys.ToArray();

            FinalizeState();
            RefreshKeyframes();

            if (redraw) Redraw();
        }
        public virtual void DeleteSingleKey(int keyIndex) => DeleteKey(keyIndex, true);
        public virtual void DeleteKeyStateless(int keyIndex, bool redraw = true)
        {
            if (keyframeData == null || keyIndex < 0 || keyIndex >= keyframeData.Length) return;
            OnKeyDelete?.Invoke(keyIndex);

            SetDirty();

            PrepKeyCountEdit();

            tempKeys.Clear();
            tempKeys.AddRange(keyframeData);

            var key = tempKeys[keyIndex];
            if (key != null) 
            { 
                key.index = -1;
                key.Destroy(keyframePool, tangentPool, tangentLinePool);
            }

            tempKeys.RemoveAt(keyIndex);
            tempKeys.RemoveAll(i => i == null);
            SortKeys(tempKeys);
            for (int a = 0; a < tempKeys.Count; a++) tempKeys[a].index = a;
            keyframeData = tempKeys.ToArray();

            FinalizeKeyCountEdit();
        }
        public virtual void DeleteKeyStateless(KeyframeData key, bool redraw = true)
        {
            if (key == null || keyframeData == null) return;
            OnKeyDelete?.Invoke(key.index);

            SetDirty();

            PrepKeyCountEdit();

            tempKeys.Clear();
            tempKeys.AddRange(keyframeData);

            key.index = -1;
            key.Destroy(keyframePool, tangentPool, tangentLinePool);

            tempKeys.RemoveAll(i => i == null || ReferenceEquals(i, key));
            SortKeys(tempKeys);
            for (int a = 0; a < tempKeys.Count; a++) tempKeys[a].index = a;
            keyframeData = tempKeys.ToArray();

            FinalizeKeyCountEdit();
        }
        public virtual void DeleteKey(int keyIndex, bool redraw = true)
        {
            PrepNewState();

            DeleteKeyStateless(keyIndex, redraw);

            FinalizeState();
            RefreshKeyframes();
        }
        public virtual void DeleteKey(KeyframeData key, bool redraw = true)
        {
            PrepNewState();

            DeleteKeyStateless(key, redraw);

            FinalizeState();
            RefreshKeyframes();
        }

        public void SetKeyTimeAndRefresh(int keyIndex, float time) => SetKeyTime(keyIndex, time);
        public void SetKeyTime(int keyIndex, float time, bool refreshUI = true) => SetKeyTime(keyframeData == null || keyIndex < 0 || keyIndex >= keyframeData.Length ? null : keyframeData[keyIndex], time, refreshUI);
        public virtual void SetKeyTime(KeyframeData key, float time, bool refreshUI = true) 
        {
            if (key == null) return;

            key.Time = time;

            if (refreshUI)
            {
                RefreshKeyUI(key, IsSelected(key));
                RebuildCurveMesh();
            }
        }
        public void SetKeyValueAndRefresh(int keyIndex, float value) => SetKeyValue(keyIndex, value);
        public void SetKeyValue(int keyIndex, float value, bool refreshUI = true) => SetKeyValue(keyframeData == null || keyIndex < 0 || keyIndex >= keyframeData.Length ? null : keyframeData[keyIndex], value, refreshUI);
        public virtual void SetKeyValue(KeyframeData key, float value, bool refreshUI = true) 
        {
            if (key == null) return;

            key.Value = value;

            if (refreshUI)
            {
                RefreshKeyUI(key, IsSelected(key));
                RebuildCurveMesh();
            }
        }

        public void SetKeyWeightedMode(int keyIndex, WeightedMode mode, bool refreshUI = true) => SetKeyWeightedMode(keyframeData == null || keyIndex < 0 || keyIndex >= keyframeData.Length ? null : keyframeData[keyIndex], mode, refreshUI);
        public virtual void SetKeyWeightedMode(KeyframeData key, WeightedMode mode, bool refreshUI = true) 
        {
            if (key == null) return;

            key.WeightedMode = mode;
            
            if (refreshUI)
            {
                RefreshKeyUI(key, IsSelected(key));
                RebuildCurveMesh();
            }
        }
        public void SetKeyTangentSettings(int keyIndex, KeyframeTangentSettings settings, bool refreshUI = true) => SetKeyTangentSettings(keyframeData == null || keyIndex < 0 || keyIndex >= keyframeData.Length ? null : keyframeData[keyIndex], settings, refreshUI);
        public virtual void SetKeyTangentSettings(KeyframeData key, KeyframeTangentSettings settings, bool refreshUI = true)
        {
            if (key == null) return;

            key.TangentSettings = settings;

            if (refreshUI)
            {
                RefreshKeyUI(key, IsSelected(key));
                RebuildCurveMesh();
            }
        }
        public void SetKeyTangentMode(int keyIndex, TangentMode mode, bool refreshUI = true) => SetKeyTangentMode(keyframeData == null || keyIndex < 0 || keyIndex >= keyframeData.Length ? null : keyframeData[keyIndex], mode, refreshUI);
        public virtual void SetKeyTangentMode(KeyframeData key, TangentMode mode, bool refreshUI = true) 
        {
            if (key == null) return;

            key.TangentMode = mode;

            if (refreshUI)
            {
                RefreshKeyUI(key, IsSelected(key));
                RebuildCurveMesh();
            }
        }
        public void SetKeyInBrokenTangentMode(int keyIndex, BrokenTangentMode mode, bool refreshUI = true) => SetKeyInBrokenTangentMode(keyframeData == null || keyIndex < 0 || keyIndex >= keyframeData.Length ? null : keyframeData[keyIndex], mode, refreshUI);
        public virtual void SetKeyInBrokenTangentMode(KeyframeData key, BrokenTangentMode mode, bool refreshUI = true)
        {
            if (key == null) return;

            key.InTangentMode = mode;

            if (refreshUI)
            {
                RefreshKeyUI(key, IsSelected(key));
                RebuildCurveMesh();
            }
        }
        public void SetKeyOutBrokenTangentMode(int keyIndex, BrokenTangentMode mode, bool refreshUI = true) => SetKeyOutBrokenTangentMode(keyframeData == null || keyIndex < 0 || keyIndex >= keyframeData.Length ? null : keyframeData[keyIndex], mode, refreshUI);
        public virtual void SetKeyOutBrokenTangentMode(KeyframeData key, BrokenTangentMode mode, bool refreshUI = true)
        {
            if (key == null) return;

            key.OutTangentMode = mode;

            if (refreshUI)
            {
                RefreshKeyUI(key, IsSelected(key));
                RebuildCurveMesh();
            }
        }
        public void SetInTangentWeight(int keyIndex, float weight, bool refreshUI = true) => SetInTangentWeight(keyframeData == null || keyIndex < 0 || keyIndex >= keyframeData.Length ? null : keyframeData[keyIndex], weight, refreshUI);
        public virtual void SetInTangentWeight(KeyframeData key, float weight, bool refreshUI = true)
        {
            if (key == null) return;

            key.InTangentWeight = weight;

            if (refreshUI)
            {
                RefreshKeyUI(key, IsSelected(key));
                RebuildCurveMesh();
            }
        }
        public void SetOutTangentWeight(int keyIndex, float weight, bool refreshUI = true) => SetOutTangentWeight(keyframeData == null || keyIndex < 0 || keyIndex >= keyframeData.Length ? null : keyframeData[keyIndex], weight, refreshUI);
        public virtual void SetOutTangentWeight(KeyframeData key, float weight, bool refreshUI = true)
        {
            if (key == null) return;

            key.OutTangentWeight = weight;

            if (refreshUI)
            {
                RefreshKeyUI(key, IsSelected(key));
                RebuildCurveMesh();
            }
        }
        public void SetInTangent(int keyIndex, float value, bool refreshUI = true) => SetInTangent(keyframeData == null || keyIndex < 0 || keyIndex >= keyframeData.Length ? null : keyframeData[keyIndex], value, refreshUI);
        public virtual void SetInTangent(KeyframeData key, float value, bool refreshUI = true)
        {
            if (key == null) return;

            key.InTangent = value;

            if (refreshUI)
            {
                RefreshKeyUI(key, IsSelected(key));
                RebuildCurveMesh();
            }
        }
        public void SetOutTangent(int keyIndex, float value, bool refreshUI = true) => SetOutTangent(keyframeData == null || keyIndex < 0 || keyIndex >= keyframeData.Length ? null : keyframeData[keyIndex], value, refreshUI);
        public virtual void SetOutTangent(KeyframeData key, float value, bool refreshUI = true)
        {
            if (key == null) return;

            key.OutTangent = value;

            if (refreshUI)
            {
                RefreshKeyUI(key, IsSelected(key));
                RebuildCurveMesh();
            }
        }
        public void SetInTangentWeightedState(int keyIndex, bool isWeighted, bool refreshUI = true) => SetInTangentWeightedState(keyframeData == null || keyIndex < 0 || keyIndex >= keyframeData.Length ? null : keyframeData[keyIndex], isWeighted, refreshUI);
        public virtual void SetInTangentWeightedState(KeyframeData key, bool isWeighted, bool refreshUI = true)
        {
            if (key == null) return;

            if (key.WeightedMode == WeightedMode.Both)
            {
                key.WeightedMode = isWeighted ? WeightedMode.Both : WeightedMode.Out;
            } 
            else if (key.WeightedMode == WeightedMode.Out)
            {
                if (isWeighted) key.WeightedMode = WeightedMode.Both;
            } 
            else
            {
                key.WeightedMode = isWeighted ? WeightedMode.In : WeightedMode.None;
            }

            if (refreshUI)
            {
                RefreshKeyUI(key, IsSelected(key));
                RebuildCurveMesh();
            }
        }
        public void SetOutTangentWeightedState(int keyIndex, bool isWeighted, bool refreshUI = true) => SetOutTangentWeightedState(keyframeData == null || keyIndex < 0 || keyIndex >= keyframeData.Length ? null : keyframeData[keyIndex], isWeighted, refreshUI);
        public virtual void SetOutTangentWeightedState(KeyframeData key, bool isWeighted, bool refreshUI = true)
        {
            if (key == null) return;

            if (key.WeightedMode == WeightedMode.Both)
            {
                key.WeightedMode = isWeighted ? WeightedMode.Both : WeightedMode.In;
            }
            else if (key.WeightedMode == WeightedMode.In)
            {
                if (isWeighted) key.WeightedMode = WeightedMode.Both;
            }
            else
            {
                key.WeightedMode = isWeighted ? WeightedMode.Out : WeightedMode.None;
            }

            if (refreshUI)
            {
                RefreshKeyUI(key, IsSelected(key));
                RebuildCurveMesh();
            }
        }

        public static WrapMode GetWrapModeByIndex(int index)
        {
            switch (index)
            {
                case 1:
                    return WrapMode.Loop;

                case 2:
                    return WrapMode.PingPong;
            }

            return WrapMode.Clamp;
        }
        public static int GetWrapModeIndex(WrapMode wrapMode)
        {
            switch (wrapMode)
            {
                case WrapMode.Loop:
                    return 1;

                case WrapMode.PingPong:
                    return 2;
            }

            return 0;
        }
        public void SetPreWrapModeByIndex(int preWrapModeIndex) => SetPreWrapMode(GetWrapModeByIndex(preWrapModeIndex));
        public virtual void SetPreWrapMode(WrapMode preWrapMode, bool redraw = true)
        {
            if (curve == null) return;

            PrepNewState();
            curve.preWrapMode = preWrapMode;
            FinalizeState();

            if (redraw) Redraw();
        }
        public void SetPostWrapModeByIndex(int postWrapModeIndex) => SetPostWrapMode(GetWrapModeByIndex(postWrapModeIndex));
        public virtual void SetPostWrapMode(WrapMode postWrapMode, bool redraw = true)
        {
            if (curve == null) return;

            PrepNewState();
            curve.postWrapMode = postWrapMode;
            FinalizeState();

            if (redraw) Redraw();
        }

        public delegate void KeyAlterationDelegate(KeyframeData key);
        protected void ApplyToSelectedKeys(KeyAlterationDelegate toApply)
        {
            if (keyframeData == null || toApply == null) return;
            foreach(var keyIndex in selectedKeys)
            {
                if (keyIndex < 0 || keyIndex >= keyframeData.Length) continue;
                var key = keyframeData[keyIndex];
                if (key == null) continue; 

                toApply(key);
            }
        }
        protected void ApplyToSelectedKeys(KeyAlterationDelegate toApply, KeyframeData exemptKey)
        {
            if (keyframeData == null || toApply == null) return;
            foreach (var keyIndex in selectedKeys)
            {
                if (keyIndex < 0 || keyIndex >= keyframeData.Length) continue;
                var key = keyframeData[keyIndex];
                if (key == null || ReferenceEquals(key, exemptKey)) continue;

                toApply(key);
            }
        }
        /// <summary>
        /// A slower way to iterate over the currently selected keys which avoids problems that arise when the selectedKeys collection is modified
        /// </summary>
        public void ApplyToSelectedKeysSafe(KeyAlterationDelegate toApply)
        {
            if (keyframeData == null || toApply == null) return;
            tempKeys.Clear();
            foreach (var index in selectedKeys)
            {
                if (index < 0 || index >= keyframeData.Length) continue;
                var key = keyframeData[index];
                if (key != null) tempKeys.Add(key); // Place keys in a temporary collection before doing any work, which prevents problems when keys change index and the selectedKeys collection gets modified 
            }
            foreach (var key in tempKeys) toApply(key);
        }
        /// <summary>
        /// A slower way to iterate over the currently selected keys which avoids problems that arise when the selectedKeys collection is modified
        /// </summary>
        public void ApplyToSelectedKeysSafe(KeyAlterationDelegate toApply, KeyframeData exemptKey)
        {
            if (keyframeData == null || toApply == null) return;
            tempKeys.Clear();
            foreach (var index in selectedKeys)
            {
                if (index < 0 || index >= keyframeData.Length) continue;
                var key = keyframeData[index];
                if (key != null && !ReferenceEquals(key, exemptKey)) tempKeys.Add(key); // Place keys in a temporary collection before doing any work, which prevents problems when keys change index and the selectedKeys collection gets modified 
            }
            foreach (var key in tempKeys) toApply(key);
        }

        protected void ApplyToAllKeys(KeyAlterationDelegate toApply)
        {
            if (keyframeData == null || toApply == null) return;
            foreach (var key in keyframeData)
            {
                if (key == null) continue; 

                toApply(key);
            }
        }
        protected void ApplyToAllKeys(KeyAlterationDelegate toApply, KeyframeData exemptKey)
        {
            if (keyframeData == null || toApply == null) return;
            foreach (var key in keyframeData)
            {
                if (key == null || ReferenceEquals(key, exemptKey)) continue;

                toApply(key);
            }
        }
        /// <summary>
        /// A slower way to iterate over all keys which avoids problems that arise when the keyframeData array is modified
        /// </summary>
        public void ApplyToAllKeysSafe(KeyAlterationDelegate toApply)
        {
            if (keyframeData == null || toApply == null) return;
            tempKeys.Clear();
            foreach (var key in keyframeData)
            {
                if (key != null) tempKeys.Add(key); // Place keys in a temporary collection before doing any work
            }
            foreach (var key in tempKeys) toApply(key);
        }
        /// <summary>
        /// A slower way to iterate over all keys which avoids problems that arise when the keyframeData array is modified
        /// </summary>
        public void ApplyToAllKeysSafe(KeyAlterationDelegate toApply, KeyframeData exemptKey)
        {
            if (keyframeData == null || toApply == null) return;
            tempKeys.Clear();
            foreach (var key in keyframeData)
            {
                if (key != null && !ReferenceEquals(key, exemptKey)) tempKeys.Add(key); // Place keys in a temporary collection before doing any work
            }
            foreach (var key in tempKeys) toApply(key);
        }

        #endregion

    }
}

#endif