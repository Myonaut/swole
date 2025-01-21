#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

using TMPro;

using Swole.UI;
using Swole.API.Unity.Animation;

namespace Swole.API.Unity
{
    public static class CustomEditorUtils
    {

        public static void SetInputFieldText(Transform rootObject, string text, bool includeInactive = true, bool withoutNotify = true) => AnimationCurveEditorUtils.SetInputFieldText(rootObject, text, includeInactive, withoutNotify);
        public static void SetInputFieldText(GameObject rootObject, string text, bool includeInactive = true, bool withoutNotify = true) => AnimationCurveEditorUtils.SetInputFieldText(rootObject, text, includeInactive, withoutNotify);

        public static void SetInputFieldTextByName(Transform rootObject, string componentName, string text, bool includeInactive = true, bool withoutNotify = true) => SetInputFieldTextByName(rootObject == null ? null : rootObject.gameObject, componentName, text, includeInactive, withoutNotify);
        public static void SetInputFieldTextByName(GameObject rootObject, string componentName, string text, bool includeInactive = true, bool withoutNotify = true)
        {
            if (rootObject == null) return;

            InputField input = rootObject.FindFirstComponentUnderChild<InputField>(componentName, includeInactive);
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
            TMP_InputField tmpInput = rootObject.FindFirstComponentUnderChild<TMP_InputField>(componentName, includeInactive); 
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

        public static void SetInputFieldOnValueChangeAction(GameObject rootObject, UnityAction<string> action, bool includeInactive = true, bool removeAllListeners = true) => AnimationCurveEditorUtils.SetInputFieldOnValueChangeAction(rootObject, action, includeInactive, removeAllListeners);
        public static void SetInputFieldOnValueChangeAction(Transform rootObject, UnityAction<string> action, bool includeInactive = true, bool removeAllListeners = true) => AnimationCurveEditorUtils.SetInputFieldOnValueChangeAction(rootObject, action, includeInactive, removeAllListeners);

        public static void SetInputFieldOnValueChangeActionByName(GameObject rootObject, string componentName, UnityAction<string> action, bool includeInactive = true, bool removeAllListeners = true) => SetInputFieldOnValueChangeActionByName(rootObject == null ? null : rootObject.transform, componentName, action, includeInactive, removeAllListeners);
        public static void SetInputFieldOnValueChangeActionByName(Transform rootObject, string componentName, UnityAction<string> action, bool includeInactive = true, bool removeAllListeners = true)
        {
            if (rootObject == null) return;

            InputField input = rootObject.FindFirstComponentUnderChild<InputField>(componentName, includeInactive);   
            if (input != null)
            {
                if (input.onValueChanged == null) input.onValueChanged = new InputField.OnChangeEvent(); else if (removeAllListeners) input.onValueChanged.RemoveAllListeners();
                if (action != null) input.onValueChanged.AddListener(action);
            }
            TMP_InputField tmpInput = rootObject.FindFirstComponentUnderChild<TMP_InputField>(componentName, includeInactive); 
            if (tmpInput != null)
            {
                if (tmpInput.onValueChanged == null) tmpInput.onValueChanged = new TMP_InputField.OnChangeEvent(); else if (removeAllListeners) tmpInput.onValueChanged.RemoveAllListeners();
                if (action != null) tmpInput.onValueChanged.AddListener(action);
            }
        }

        public static void SetInputFieldOnEndEditAction(GameObject rootObject, UnityAction<string> action, bool includeInactive = true, bool removeAllListeners = true) => SetInputFieldOnEndEditAction(rootObject.transform, action, includeInactive, removeAllListeners);
        public static void SetInputFieldOnEndEditAction(Transform rootObject, UnityAction<string> action, bool includeInactive = true, bool removeAllListeners = true)
        {
            if (rootObject == null) return;

            InputField input = rootObject.GetComponentInChildren<InputField>(includeInactive);
            if (input != null)
            {
                if (input.onEndEdit == null) input.onEndEdit = new InputField.EndEditEvent(); else if (removeAllListeners) input.onEndEdit.RemoveAllListeners();
                if (action != null) input.onValueChanged.AddListener(action);
            }
            TMP_InputField tmpInput = rootObject.GetComponentInChildren<TMP_InputField>(includeInactive);
            if (tmpInput != null)
            {
                if (tmpInput.onEndEdit == null) tmpInput.onEndEdit = new TMP_InputField.SubmitEvent(); else if (removeAllListeners) tmpInput.onEndEdit.RemoveAllListeners();
                if (action != null) tmpInput.onEndEdit.AddListener(action); 
            }
        }

        public static void SetInputFieldOnEndEditActionByName(GameObject rootObject, string componentName, UnityAction<string> action, bool includeInactive = true, bool removeAllListeners = true) => SetInputFieldOnEndEditActionByName(rootObject == null ? null : rootObject.transform, componentName, action, includeInactive, removeAllListeners);
        public static void SetInputFieldOnEndEditActionByName(Transform rootObject, string componentName, UnityAction<string> action, bool includeInactive = true, bool removeAllListeners = true)
        {
            if (rootObject == null) return;

            InputField input = rootObject.FindFirstComponentUnderChild<InputField>(componentName, includeInactive);
            if (input != null)
            {
                if (input.onEndEdit == null) input.onEndEdit = new InputField.EndEditEvent(); else if (removeAllListeners) input.onEndEdit.RemoveAllListeners();
                if (action != null) input.onEndEdit.AddListener(action);
            }
            TMP_InputField tmpInput = rootObject.FindFirstComponentUnderChild<TMP_InputField>(componentName, includeInactive);
            if (tmpInput != null)
            {
                if (tmpInput.onEndEdit == null) tmpInput.onEndEdit = new TMP_InputField.SubmitEvent(); else if (removeAllListeners) tmpInput.onEndEdit.RemoveAllListeners();
                if (action != null) tmpInput.onEndEdit.AddListener(action);
            }
        }

        public static string GetInputFieldText(Transform rootObject, bool includeInactive = true) => AnimationCurveEditorUtils.GetInputFieldText(rootObject, includeInactive);
        public static string GetInputFieldText(GameObject rootObject, bool includeInactive = true) => AnimationCurveEditorUtils.GetInputFieldText(rootObject, includeInactive);

        public static void SetComponentText(Transform rootObject, string text, bool includeInactive = true) => AnimationCurveEditorUtils.SetComponentText(rootObject, text, includeInactive);
        public static void SetComponentText(GameObject rootObject, string text, bool includeInactive = true) => AnimationCurveEditorUtils.SetComponentText(rootObject, text, includeInactive);
        public static void SetComponentTextByName(Transform rootObject, string componentName, string text, bool includeInactive = true) => SetComponentTextByName(rootObject == null ? null : rootObject.gameObject, componentName, text, includeInactive);
        public static void SetComponentTextByName(GameObject rootObject, string componentName, string text, bool includeInactive = true)
        {
            if (rootObject == null) return;

            Text textUI = rootObject.FindFirstComponentUnderChild<Text>(componentName, includeInactive);
            if (textUI != null) textUI.text = text;
            TMP_Text textTMP = rootObject.FindFirstComponentUnderChild<TMP_Text>(componentName, includeInactive);
            if (textTMP != null) textTMP.text = text;
        }

        public static void SetComponentTextAndColor(Transform rootObject, string text, Color color, bool includeInactive = true) => SetComponentTextAndColor(rootObject == null ? null : rootObject.gameObject, text, color, includeInactive);
        public static void SetComponentTextAndColor(GameObject rootObject, string text, Color color, bool includeInactive = true)
        {
            if (rootObject == null) return;

            Text textUI = rootObject.GetComponentInChildren<Text>(includeInactive);
            if (textUI != null) 
            { 
                textUI.text = text;
                textUI.color = color; 
            }
            TMP_Text textTMP = rootObject.GetComponentInChildren<TMP_Text>(includeInactive);
            if (textTMP != null) 
            { 
                textTMP.text = text;
                textTMP.color = color;
            }
        }
        public static void SetComponentTextAndColorByName(Transform rootObject, string componentName, string text, Color color, bool includeInactive = true) => SetComponentTextAndColorByName(rootObject == null ? null : rootObject.gameObject, componentName, text, color, includeInactive);
        public static void SetComponentTextAndColorByName(GameObject rootObject, string componentName, string text, Color color, bool includeInactive = true)
        {
            if (rootObject == null) return;

            Text textUI = rootObject.FindFirstComponentUnderChild<Text>(componentName, includeInactive);
            if (textUI != null) 
            { 
                textUI.text = text;
                textUI.color = color;
            }
            TMP_Text textTMP = rootObject.FindFirstComponentUnderChild<TMP_Text>(componentName, includeInactive);
            if (textTMP != null) 
            { 
                textTMP.text = text;
                textTMP.color = color;
            }
        }

        public static string GetComponentText(Transform rootObject, bool includeInactive = true) => GetComponentText(rootObject.gameObject, includeInactive);
        public static string GetComponentText(GameObject rootObject, bool includeInactive = true)
        {
            if (rootObject == null) return string.Empty;

            TMP_Text textTMP = rootObject.GetComponentInChildren<TMP_Text>(includeInactive);
            if (textTMP != null) return textTMP.text;
            Text textUI = rootObject.GetComponentInChildren<Text>(includeInactive);
            if (textUI != null) return textUI.text;

            return string.Empty;
        }
        public static string GetComponentTextByName(Transform rootObject, string componentName, bool includeInactive = true) => GetComponentTextByName(rootObject.gameObject, componentName, includeInactive);
        public static string GetComponentTextByName(GameObject rootObject, string componentName, bool includeInactive = true)
        {
            if (rootObject == null) return string.Empty;

            TMP_Text textTMP = rootObject.FindFirstComponentUnderChild<TMP_Text>(componentName, includeInactive);
            if (textTMP != null) return textTMP.text;
            Text textUI = rootObject.FindFirstComponentUnderChild<Text>(componentName, includeInactive);
            if (textUI != null) return textUI.text;

            return string.Empty;
        }

        #region Input or Text Component

        public static void SetInputOrTextComponentText(Transform rootObject, string text, bool includeInactive = true, bool withoutNotify = true) => SetInputOrTextComponentText(rootObject == null ? null : rootObject.gameObject, text, includeInactive, withoutNotify);
        public static void SetInputOrTextComponentText(GameObject rootObject, string text, bool includeInactive = true, bool withoutNotify = true)
        {
            if (rootObject == null) return;

            bool flag = false;
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
                flag = true;
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
                flag = true;
            }
            if (flag) return;

            Text textUI = rootObject.GetComponentInChildren<Text>(includeInactive);
            if (textUI != null) textUI.text = text;
            TMP_Text textTMP = rootObject.GetComponentInChildren<TMP_Text>(includeInactive);
            if (textTMP != null) textTMP.text = text;
        }
        public static void SetInputOrTextComponentTextByName(Transform rootObject, string componentName, string text, bool includeInactive = true, bool withoutNotify = true) => SetInputOrTextComponentTextByName(rootObject == null ? null : rootObject.gameObject, componentName, text, includeInactive, withoutNotify);
        public static void SetInputOrTextComponentTextByName(GameObject rootObject, string componentName, string text, bool includeInactive = true, bool withoutNotify = true)
        {
            if (rootObject == null) return;

            bool flag = false;
            InputField input = rootObject.FindFirstComponentUnderChild<InputField>(componentName, includeInactive);
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
                flag = true;
            }
            TMP_InputField tmpInput = rootObject.FindFirstComponentUnderChild<TMP_InputField>(componentName, includeInactive); 
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
                flag = true;
            }
            if (flag) return;

            Text textUI = rootObject.FindFirstComponentUnderChild<Text>(componentName, includeInactive);
            if (textUI != null) textUI.text = text;
            TMP_Text textTMP = rootObject.FindFirstComponentUnderChild<TMP_Text>(componentName, includeInactive);
            if (textTMP != null) textTMP.text = text;
        }

        public static string GetInputOrTextComponentText(Transform rootObject, bool includeInactive = true) => GetInputOrTextComponentText(rootObject.gameObject, includeInactive);
        public static string GetInputOrTextComponentText(GameObject rootObject, bool includeInactive = true)
        {
            if (rootObject == null) return string.Empty;

            TMP_InputField tmpInput = rootObject.GetComponentInChildren<TMP_InputField>(includeInactive);
            if (tmpInput != null) return tmpInput.text;
            InputField input = rootObject.GetComponentInChildren<InputField>(includeInactive);
            if (input != null) return input.text;

            TMP_Text textTMP = rootObject.GetComponentInChildren<TMP_Text>(includeInactive);
            if (textTMP != null) return textTMP.text;
            Text textUI = rootObject.GetComponentInChildren<Text>(includeInactive);
            if (textUI != null) return textUI.text;

            return string.Empty;
        }
        public static string GetInputOrTextComponentTextByName(Transform rootObject, string componentName, bool includeInactive = true) => GetInputOrTextComponentTextByName(rootObject.gameObject, componentName, includeInactive);
        public static string GetInputOrTextComponentTextByName(GameObject rootObject, string componentName, bool includeInactive = true)
        {
            if (rootObject == null) return string.Empty;

            TMP_InputField tmpInput = rootObject.FindFirstComponentUnderChild<TMP_InputField>(componentName, includeInactive);
            if (tmpInput != null) return tmpInput.text;
            InputField input = rootObject.FindFirstComponentUnderChild<InputField>(componentName, includeInactive);
            if (input != null) return input.text;

            TMP_Text textTMP = rootObject.FindFirstComponentUnderChild<TMP_Text>(componentName, includeInactive); 
            if (textTMP != null) return textTMP.text;
            Text textUI = rootObject.FindFirstComponentUnderChild<Text>(componentName, includeInactive);
            if (textUI != null) return textUI.text;

            return string.Empty;
        }

        #endregion

        public static void SetButtonOnClickAction(Transform rootObject, UnityAction action, bool includeInactive = true, bool removeAllListeners = true, bool forceActiveAndInteractable = true) => SetButtonOnClickAction(rootObject == null ? null : rootObject.gameObject, action, includeInactive, removeAllListeners, forceActiveAndInteractable);
        public static void SetButtonOnClickAction(GameObject rootObject, UnityAction action, bool includeInactive = true, bool removeAllListeners = true, bool forceActiveAndInteractable = true)
        {
            if (rootObject == null) return;

            var button = rootObject.GetComponentInChildren<Button>(includeInactive);
            if (button != null)
            {
                if (forceActiveAndInteractable)
                {
                    button.gameObject.SetActive(true);
                    button.interactable = true;
                }
                if (button.onClick == null) button.onClick = new Button.ButtonClickedEvent(); else if (removeAllListeners) button.onClick.RemoveAllListeners();
                if (action != null) button.onClick.AddListener(action);
            }
            var tabButton = rootObject.GetComponentInChildren<UITabButton>(includeInactive);
            if (tabButton != null)
            {
                if (forceActiveAndInteractable)
                {
                    tabButton.gameObject.SetActive(true);
                    tabButton.disable = false; 
                }
                if (tabButton.OnClick == null) tabButton.OnClick = new UnityEvent(); else if (removeAllListeners) tabButton.OnClick.RemoveAllListeners();
                if (action != null) tabButton.OnClick.AddListener(action);
            }
        }

        public static void SetButtonOnClickActionByName(GameObject rootObject, string buttonName, UnityAction action, bool includeInactive = true, bool removeAllListeners = true, bool forceActiveAndInteractable=true) => SetButtonOnClickActionByName(rootObject == null ? null : rootObject.transform, buttonName, action, includeInactive, removeAllListeners, forceActiveAndInteractable);
        public static void SetButtonOnClickActionByName(Transform rootObject, string buttonName, UnityAction action, bool includeInactive = true, bool removeAllListeners = true, bool forceActiveAndInteractable = true)
        {
            var buttonComp = rootObject.FindFirstComponentUnderChild<Button>(buttonName, includeInactive);
            if (buttonComp != null)
            {
                if (forceActiveAndInteractable) 
                { 
                    buttonComp.gameObject.SetActive(true);
                    buttonComp.interactable = true;
                }
                if (buttonComp.onClick == null) buttonComp.onClick = new Button.ButtonClickedEvent(); else if (removeAllListeners) buttonComp.onClick.RemoveAllListeners();
                if (action != null) buttonComp.onClick.AddListener(action);
            }
            var editTabButton = rootObject.FindFirstComponentUnderChild<UITabButton>(buttonName, includeInactive);
            if (editTabButton != null)
            {
                if (forceActiveAndInteractable) 
                { 
                    editTabButton.gameObject.SetActive(true);
                    editTabButton.disable = false;
                }
                if (editTabButton.OnClick == null) editTabButton.OnClick = new UnityEvent(); else if (removeAllListeners) editTabButton.OnClick.RemoveAllListeners();
                if (action != null) editTabButton.OnClick.AddListener(action);
            }
        }

        public static void SetButtonInteractable(Transform rootObject, bool interactable, bool includeInactive = true, bool forceActive = false) => SetButtonInteractable(rootObject == null ? null : rootObject.gameObject, interactable, includeInactive, forceActive);
        public static void SetButtonInteractable(GameObject rootObject, bool interactable, bool includeInactive = true, bool forceActive = false)
        {
            if (rootObject == null) return;

            var button = rootObject.GetComponentInChildren<Button>(includeInactive);
            if (button != null)
            {
                if (forceActive) button.gameObject.SetActive(true);
                button.interactable = interactable;
            }
            var tabButton = rootObject.GetComponentInChildren<UITabButton>(includeInactive);
            if (tabButton != null)
            {
                if (forceActive) tabButton.gameObject.SetActive(true);
                tabButton.disable = !interactable;
            }
        }

        public static void SetButtonInteractableByName(GameObject rootObject, string buttonName, bool interactable, bool includeInactive = true, bool forceActive = false) => SetButtonInteractableByName(rootObject == null ? null : rootObject.transform, buttonName, interactable, includeInactive, forceActive);
        public static void SetButtonInteractableByName(Transform rootObject, string buttonName, bool interactable, bool includeInactive = true, bool forceActive = false)
        {
            var buttonComp = rootObject.FindFirstComponentUnderChild<Button>(buttonName, includeInactive);
            if (buttonComp != null)
            {
                if (forceActive) buttonComp.gameObject.SetActive(true);
                buttonComp.interactable = interactable;
            }
            var editTabButton = rootObject.FindFirstComponentUnderChild<UITabButton>(buttonName, includeInactive);
            if (editTabButton != null)
            {
                if (forceActive) editTabButton.gameObject.SetActive(true);
                editTabButton.disable = !interactable;
            }
        }

        public static void SetDropdownOptions(GameObject rootObject, List<TMP_Dropdown.OptionData> options, bool includeInactive = true) => AnimationCurveEditorUtils.SetDropdownOptions(rootObject, options, includeInactive);
        public static void SetDropdownOptions(Transform rootObject, List<TMP_Dropdown.OptionData> options, bool includeInactive = true) => AnimationCurveEditorUtils.SetDropdownOptions(rootObject, options, includeInactive);

        public static void SetSelectedDropdownOption(GameObject rootObject, int optionIndex, bool includeInactive = true, bool withoutNotify = true) => AnimationCurveEditorUtils.SetSelectedDropdownOption(rootObject, optionIndex, includeInactive, withoutNotify);
        public static void SetSelectedDropdownOption(Transform rootObject, int optionIndex, bool includeInactive = true, bool withoutNotify = true) => AnimationCurveEditorUtils.SetSelectedDropdownOption(rootObject, optionIndex, includeInactive, withoutNotify);

        public static List<TMP_Dropdown.OptionData> GetDropdownOptions(GameObject rootObject, List<TMP_Dropdown.OptionData> optionsList = null, bool includeInactive = true) => AnimationCurveEditorUtils.GetDropdownOptions(rootObject, optionsList, includeInactive);
        public static List<TMP_Dropdown.OptionData> GetDropdownOptions(Transform rootObject, List<TMP_Dropdown.OptionData> optionsList = null, bool includeInactive = true) => AnimationCurveEditorUtils.GetDropdownOptions(rootObject, optionsList, includeInactive);

        public static void SetDropdownOnValueChangeAction(GameObject rootObject, UnityAction<int, string> action, bool includeInactive = true, bool removeAllListeners = true) => AnimationCurveEditorUtils.SetDropdownOnValueChangeAction(rootObject, action, includeInactive, removeAllListeners);
        public static void SetDropdownOnValueChangeAction(Transform rootObject, UnityAction<int, string> action, bool includeInactive = true, bool removeAllListeners = true) => AnimationCurveEditorUtils.SetDropdownOnValueChangeAction(rootObject, action, includeInactive, removeAllListeners);

        public static void SetDropdownOptionChooseAction(GameObject rootObject, string optionName, UnityAction action, bool includeInactive = true, bool removeAllListeners = false) => AnimationCurveEditorUtils.SetDropdownOptionChooseAction(rootObject, optionName, action, includeInactive, removeAllListeners);
        public static void SetDropdownOptionChooseAction(Transform rootObject, string optionName, UnityAction action, bool includeInactive = true, bool removeAllListeners = false) => AnimationCurveEditorUtils.SetDropdownOptionChooseAction(rootObject, optionName, action, includeInactive, removeAllListeners);

        public static void SetDropdownOptionChooseAction(GameObject rootObject, int option, UnityAction action, bool includeInactive = true, bool removeAllListeners = false) => AnimationCurveEditorUtils.SetDropdownOptionChooseAction(rootObject, option, action, includeInactive, removeAllListeners);
        public static void SetDropdownOptionChooseAction(Transform rootObject, int option, UnityAction action, bool includeInactive = true, bool removeAllListeners = false) => AnimationCurveEditorUtils.SetDropdownOptionChooseAction(rootObject, option, action, includeInactive, removeAllListeners);

        public static void SetToggleValue(GameObject rootObject, bool value, bool includeInactive = true, bool withoutNotify = true) => AnimationCurveEditorUtils.SetToggleValue(rootObject, value, includeInactive, withoutNotify);
        public static void SetToggleValue(Transform rootObject, bool value, bool includeInactive = true, bool withoutNotify = true) => AnimationCurveEditorUtils.SetToggleValue(rootObject, value, includeInactive, withoutNotify);

        public static void SetToggleOnValueChangeAction(GameObject rootObject, UnityAction<bool> action, bool includeInactive = true, bool removeAllListeners = true) => AnimationCurveEditorUtils.SetToggleOnValueChangeAction(rootObject, action, includeInactive, removeAllListeners);
        public static void SetToggleOnValueChangeAction(Transform rootObject, UnityAction<bool> action, bool includeInactive = true, bool removeAllListeners = true) => AnimationCurveEditorUtils.SetToggleOnValueChangeAction(rootObject, action, includeInactive, removeAllListeners);


        public static void SetToggleValueByName(GameObject rootObject, string toggleName, bool value, bool includeInactive = true, bool withoutNotify = true) => SetToggleValueByName(rootObject == null ? null : rootObject.transform, toggleName, value, includeInactive, withoutNotify);
        public static void SetToggleValueByName(Transform rootObject, string toggleName, bool value, bool includeInactive = true, bool withoutNotify = true)
        {
            if (rootObject == null) return;

            Toggle toggle = rootObject.FindFirstComponentUnderChild<Toggle>(toggleName, includeInactive);
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

        public static void SetToggleOnValueChangeActionByName(GameObject rootObject, string toggleName, UnityAction<bool> action, bool includeInactive = true, bool removeAllListeners = true) => SetToggleOnValueChangeActionByName(rootObject == null ? null : rootObject.transform, toggleName, action, includeInactive, removeAllListeners);
        public static void SetToggleOnValueChangeActionByName(Transform rootObject, string toggleName, UnityAction<bool> action, bool includeInactive = true, bool removeAllListeners = true)
        {
            if (rootObject == null) return;

            Toggle toggle = rootObject.FindFirstComponentUnderChild<Toggle>(toggleName, includeInactive);
            if (toggle != null)
            {
                if (toggle.onValueChanged == null) toggle.onValueChanged = new Toggle.ToggleEvent(); else if (removeAllListeners) toggle.onValueChanged.RemoveAllListeners(); 
                if (action != null) toggle.onValueChanged.AddListener(action);
            }
        }


        public static void SetScrollbarOnValueChangeAction(GameObject rootObject, UnityAction<float> action, bool includeInactive = true, bool removeAllListeners = true) => AnimationCurveEditorUtils.SetScrollbarOnValueChangeAction(rootObject, action, includeInactive, removeAllListeners);
        public static void SetScrollbarOnValueChangeAction(Transform rootObject, UnityAction<float> action, bool includeInactive = true, bool removeAllListeners = true) => AnimationCurveEditorUtils.SetScrollbarOnValueChangeAction(rootObject, action, includeInactive, removeAllListeners);
        public static void SetScrollbarOnValueChangeAction(Scrollbar scrollbar, UnityAction<float> action, bool removeAllListeners = true)=> AnimationCurveEditorUtils.SetScrollbarOnValueChangeAction(scrollbar, action, removeAllListeners);

        public static void SetAllRaycastTargets(Transform rootTransform, bool enabled) => AnimationCurveEditorUtils.SetAllRaycastTargets(rootTransform, enabled);
        public static void SetAllRaycastTargets(GameObject rootObject, bool enabled) => AnimationCurveEditorUtils.SetAllRaycastTargets(rootObject, enabled);

    }
}

#endif