#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

using TMPro;
using System.Collections.Generic; 

namespace Swole
{
    public class SettingsSync : MonoBehaviour
    {

        public static object GetDefaultExternalSettings() => swole.Settings; 
        public static void SetDefaultExternalSettings(object settings) => swole.Settings = (SwoleSettings)settings;

        public UnityEvent onClose;

        public virtual void Update()
        {
            if (InputProxy.CloseOrQuitKeyDown)
            {
                onClose?.Invoke();
            }
        }

        [Serializable]
        public enum Settingtype
        {
            Bool, Float, Int, String
        }

        [Serializable]
        public class Setting
        {
            public string name;
            public Settingtype valueType;

            public bool invertValue;

            [NonSerialized]
            public bool isDirty;

            public GameObject uiRoot;

            public InputField inputField;
            public TMP_InputField inputFieldTMP;

            public Toggle toggle;

            public Slider slider;

            #region UI Get
            public float GetFloatValueFromUI()
            {
                //if (slider == null) slider = uiRoot.GetComponentInChildren<Slider>(true);
                if (slider != null) return slider.value;

                //if (inputFieldTMP == null) inputFieldTMP = uiRoot.GetComponentInChildren<TMP_InputField>(true);
                if (inputFieldTMP != null)
                {
                    if (float.TryParse(inputFieldTMP.text, out float value)) return value;
                }

                //if (inputField == null) inputField = uiRoot.GetComponentInChildren<InputField>(true);
                if (inputField != null)
                {
                    if (float.TryParse(inputFieldTMP.text, out float value)) return value;
                }

                //if (toggle == null) toggle = uiRoot.GetComponentInChildren<Toggle>(true);
                if (toggle != null) return toggle.isOn ? 1f : 0f;

                return 0f;
            }
            public int GetIntValueFromUI()
            {
                //if (slider == null) slider = uiRoot.GetComponentInChildren<Slider>(true);
                if (slider != null) return (int)slider.value;

                //if (inputFieldTMP == null) inputFieldTMP = uiRoot.GetComponentInChildren<TMP_InputField>(true);
                if (inputFieldTMP != null)
                {
                    if (int.TryParse(inputFieldTMP.text, out int value)) return value;
                }

                //if (inputField == null) inputField = uiRoot.GetComponentInChildren<InputField>(true);
                if (inputField != null)
                {
                    if (int.TryParse(inputFieldTMP.text, out int value)) return value;
                }

                //if (toggle == null) toggle = uiRoot.GetComponentInChildren<Toggle>(true);
                if (toggle != null) return toggle.isOn ? 1 : 0;

                return 0;
            }
            public bool GetBoolValueFromUI()
            {
                //if (toggle == null) toggle = uiRoot.GetComponentInChildren<Toggle>(true);
                if (toggle != null) return toggle.isOn;

                //if (slider == null) slider = uiRoot.GetComponentInChildren<Slider>(true);
                if (slider != null) return slider.value > 0;

                //if (inputFieldTMP == null) inputFieldTMP = uiRoot.GetComponentInChildren<TMP_InputField>(true);
                if (inputFieldTMP != null)
                {
                    if (int.TryParse(inputFieldTMP.text, out int value)) return value > 0;
                }

                //if (inputField == null) inputField = uiRoot.GetComponentInChildren<InputField>(true);
                if (inputField != null)
                {
                    if (int.TryParse(inputFieldTMP.text, out int value)) return value > 0;
                }

                return false;
            }
            public string GetStringValueFromUI()
            {
                //if (inputFieldTMP == null) inputFieldTMP = uiRoot.GetComponentInChildren<TMP_InputField>(true);
                if (inputFieldTMP != null) return inputFieldTMP.text;

                //if (inputField == null) inputField = uiRoot.GetComponentInChildren<InputField>(true);
                if (inputField != null) return inputField.text;

                return string.Empty;
            }

            public T GetValueFromUI<T>()
            {
                if (uiRoot == null) return default;

                try
                {
                    switch (valueType)
                    {
                        case Settingtype.Bool:
                            bool boolVal = invertValue ? !GetBoolValueFromUI() : GetBoolValueFromUI();
                            if (typeof(bool).IsAssignableFrom(typeof(T)))
                            {
                                return boolVal.CastAs<T>();
                            }
                            else if (typeof(float).IsAssignableFrom(typeof(T)) || typeof(double).IsAssignableFrom(typeof(T)))
                            {
                                return (boolVal ? 1f : 0f).CastAs<T>();
                            }
                            else if (typeof(int).IsAssignableFrom(typeof(T)) || typeof(short).IsAssignableFrom(typeof(T)) || typeof(long).IsAssignableFrom(typeof(T)) || typeof(byte).IsAssignableFrom(typeof(T)))
                            {
                                return (boolVal ? 1 : 0).CastAs<T>();
                            }
                            else if (typeof(string).IsAssignableFrom(typeof(T)))
                            {
                                return (boolVal ? "true" : "false").CastAs<T>();
                            }
                            break;

                        case Settingtype.Float:
                            float floatVal = invertValue ? (1 - GetFloatValueFromUI()) : GetFloatValueFromUI();
                            if (typeof(float).IsAssignableFrom(typeof(T)) || typeof(double).IsAssignableFrom(typeof(T)))
                            {
                                return floatVal.CastAs<T>();
                            }
                            else if (typeof(int).IsAssignableFrom(typeof(T)) || typeof(short).IsAssignableFrom(typeof(T)) || typeof(long).IsAssignableFrom(typeof(T)) || typeof(byte).IsAssignableFrom(typeof(T)))
                            {
                                return ((int)floatVal).CastAs<T>();
                            }
                            else if (typeof(bool).IsAssignableFrom(typeof(T)))
                            {
                                return (floatVal > 0).CastAs<T>();
                            }
                            else if (typeof(string).IsAssignableFrom(typeof(T)))
                            {
                                return floatVal.ToString().CastAs<T>();
                            }
                            break;

                        case Settingtype.Int:
                            int intVal = invertValue ? (1 - GetIntValueFromUI()) : GetIntValueFromUI();
                            if (typeof(int).IsAssignableFrom(typeof(T)) || typeof(short).IsAssignableFrom(typeof(T)) || typeof(long).IsAssignableFrom(typeof(T)) || typeof(byte).IsAssignableFrom(typeof(T)))
                            {
                                return intVal.CastAs<T>();
                            }
                            else if (typeof(float).IsAssignableFrom(typeof(T)) || typeof(double).IsAssignableFrom(typeof(T)))
                            {
                                return ((float)intVal).CastAs<T>();
                            }
                            else if (typeof(bool).IsAssignableFrom(typeof(T)))
                            {
                                return (intVal > 0).CastAs<T>();
                            }
                            else if (typeof(string).IsAssignableFrom(typeof(T)))
                            {
                                return intVal.ToString().CastAs<T>();
                            }
                            break;

                        case Settingtype.String:
                            string stringVal = GetStringValueFromUI();
                            if (typeof(string).IsAssignableFrom(typeof(T)))
                            {
                                return stringVal.CastAs<T>();
                            }
                            else if (typeof(int).IsAssignableFrom(typeof(T)) || typeof(short).IsAssignableFrom(typeof(T)) || typeof(long).IsAssignableFrom(typeof(T)) || typeof(byte).IsAssignableFrom(typeof(T)))
                            {
                                return (invertValue ? 1 - int.Parse(stringVal) : int.Parse(stringVal)).CastAs<T>();
                            }
                            else if (typeof(float).IsAssignableFrom(typeof(T)) || typeof(double).IsAssignableFrom(typeof(T)))
                            {
                                return (invertValue ? 1 - float.Parse(stringVal) : float.Parse(stringVal)).CastAs<T>();
                            }
                            else if (typeof(bool).IsAssignableFrom(typeof(T)))
                            {
                                return (invertValue ? !bool.Parse(stringVal) : bool.Parse(stringVal)).CastAs<T>();
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    swole.LogError(ex);
                }

                return default;
            }
            #endregion

            #region UI Set
            public void SetFloatValueInUI(float value)
            {
                //if (slider == null) slider = uiRoot.GetComponentInChildren<Slider>(true);
                if (slider != null) slider.value = value;

                //if (inputFieldTMP == null) inputFieldTMP = uiRoot.GetComponentInChildren<TMP_InputField>(true);
                if (inputFieldTMP != null) inputFieldTMP.text = value.ToString();

                //if (inputField == null) inputField = uiRoot.GetComponentInChildren<InputField>(true);
                if (inputField != null) inputField.text = value.ToString();

                //if (toggle == null) toggle = uiRoot.GetComponentInChildren<Toggle>(true);
                if (toggle != null) toggle.isOn = value > 0;
            }
            public void SetIntValueInUI(int value)
            {
                //if (slider == null) slider = uiRoot.GetComponentInChildren<Slider>(true);
                if (slider != null) slider.value = value;

                //if (inputFieldTMP == null) inputFieldTMP = uiRoot.GetComponentInChildren<TMP_InputField>(true);
                if (inputFieldTMP != null) inputFieldTMP.text = value.ToString();

                //if (inputField == null) inputField = uiRoot.GetComponentInChildren<InputField>(true);
                if (inputField != null) inputField.text = value.ToString();

                //if (toggle == null) toggle = uiRoot.GetComponentInChildren<Toggle>(true);
                if (toggle != null) toggle.isOn = value > 0;
            }
            public void SetBoolValueInUI(bool value)
            {
                //if (slider == null) slider = uiRoot.GetComponentInChildren<Slider>(true);
                if (slider != null) slider.value = value ? 1 : 0;

                //if (inputFieldTMP == null) inputFieldTMP = uiRoot.GetComponentInChildren<TMP_InputField>(true);
                if (inputFieldTMP != null) inputFieldTMP.text = (value ? 1 : 0).ToString();

                //if (inputField == null) inputField = uiRoot.GetComponentInChildren<InputField>(true);
                if (inputField != null) inputField.text = (value ? 1 : 0).ToString();

                //if (toggle == null) toggle = uiRoot.GetComponentInChildren<Toggle>(true);
                if (toggle != null) toggle.isOn = value;
            }
            public void SetStringValueInUI(string value)
            {
                //if (inputFieldTMP == null) inputFieldTMP = uiRoot.GetComponentInChildren<TMP_InputField>(true);
                if (inputFieldTMP != null) inputFieldTMP.text = value;

                //if (inputField == null) inputField = uiRoot.GetComponentInChildren<InputField>(true);
                if (inputField != null) inputField.text = value;
            }

            public void SetValueInUI<T>(T value)
            {
                if (uiRoot == null) return;

                try
                {
                    switch (valueType)
                    {
                        case Settingtype.Bool:
                            if (typeof(bool).IsAssignableFrom(typeof(T)))
                            {
                                SetBoolValueInUI(invertValue ? !value.CastAs<bool>() : value.CastAs<bool>());
                            }
                            else if (typeof(float).IsAssignableFrom(typeof(T)) || typeof(double).IsAssignableFrom(typeof(T)))
                            {
                                SetBoolValueInUI(invertValue ? value.CastAs<float>() <= 0 : value.CastAs<float>() > 0);
                            }
                            else if (typeof(int).IsAssignableFrom(typeof(T)) || typeof(short).IsAssignableFrom(typeof(T)) || typeof(long).IsAssignableFrom(typeof(T)) || typeof(byte).IsAssignableFrom(typeof(T)))
                            {
                                SetBoolValueInUI(invertValue ? value.CastAs<int>() <= 0 : value.CastAs<int>() > 0);
                            }
                            else if (typeof(string).IsAssignableFrom(typeof(T)))
                            {
                                SetBoolValueInUI(invertValue ? !bool.Parse(value.ToString()) : bool.Parse(value.ToString()));
                            }
                            break;

                        case Settingtype.Float:
                            if (typeof(float).IsAssignableFrom(typeof(T)) || typeof(double).IsAssignableFrom(typeof(T)))
                            {
                                SetFloatValueInUI(invertValue ? 1 - value.CastAs<float>() : value.CastAs<float>());
                            }
                            else if (typeof(int).IsAssignableFrom(typeof(T)) || typeof(short).IsAssignableFrom(typeof(T)) || typeof(long).IsAssignableFrom(typeof(T)) || typeof(byte).IsAssignableFrom(typeof(T)))
                            {
                                SetFloatValueInUI(invertValue ? 1 - value.CastAs<int>() : value.CastAs<int>());
                            }
                            else if (typeof(bool).IsAssignableFrom(typeof(T)))
                            {
                                SetFloatValueInUI(invertValue ? (value.CastAs<bool>() ? 0f : 1f) : (value.CastAs<bool>() ? 1f : 0f));
                            }
                            else if (typeof(string).IsAssignableFrom(typeof(T)))
                            {
                                SetFloatValueInUI(invertValue ? 1 - float.Parse(value.ToString()) : float.Parse(value.ToString()));
                            }
                            break;

                        case Settingtype.Int:
                            if (typeof(int).IsAssignableFrom(typeof(T)) || typeof(short).IsAssignableFrom(typeof(T)) || typeof(long).IsAssignableFrom(typeof(T)) || typeof(byte).IsAssignableFrom(typeof(T)))
                            {
                                SetIntValueInUI(invertValue ? 1 - value.CastAs<int>() : value.CastAs<int>());
                            }
                            else if (typeof(float).IsAssignableFrom(typeof(T)) || typeof(double).IsAssignableFrom(typeof(T)))
                            {
                                SetIntValueInUI(invertValue ? (int)(1 - value.CastAs<float>()) : (int)(value.CastAs<float>()));
                            }
                            else if (typeof(bool).IsAssignableFrom(typeof(T)))
                            {
                                SetIntValueInUI(invertValue ? (value.CastAs<bool>() ? 0 : 1) : (value.CastAs<bool>() ? 1 : 0));
                            }
                            else if (typeof(string).IsAssignableFrom(typeof(T)))
                            {
                                SetIntValueInUI(invertValue ? 1 - int.Parse(value.ToString()) : int.Parse(value.ToString()));
                            }
                            break;

                        case Settingtype.String:
                            if (typeof(string).IsAssignableFrom(typeof(T)))
                            {
                                SetStringValueInUI(value.ToString());
                            }
                            else if (typeof(int).IsAssignableFrom(typeof(T)) || typeof(short).IsAssignableFrom(typeof(T)) || typeof(long).IsAssignableFrom(typeof(T)) || typeof(byte).IsAssignableFrom(typeof(T)))
                            {
                                SetStringValueInUI((invertValue ? 1 - value.CastAs<int>() : value.CastAs<int>()).ToString());
                            }
                            else if (typeof(float).IsAssignableFrom(typeof(T)) || typeof(double).IsAssignableFrom(typeof(T)))
                            {
                                SetStringValueInUI((invertValue ? 1 - value.CastAs<float>() : value.CastAs<float>()).ToString());
                            }
                            else if (typeof(bool).IsAssignableFrom(typeof(T)))
                            {
                                SetStringValueInUI((invertValue ? !value.CastAs<bool>() : value.CastAs<bool>()) ? "true" : "false");
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    swole.LogError(ex);
                }
            }

            #endregion

            private List<UnityAction> listeners = new List<UnityAction>();

            public void AddListener(UnityAction listener)
            {
                listeners.Add(listener);
            }
            public void RemoveListener(UnityAction listener)
            {
                listeners.RemoveAll(i => ReferenceEquals(i, listener));
            }
            public void RemoveAllListeners() => listeners.Clear();

            public void CallListeners()
            {
                foreach (var listener in listeners) listener?.Invoke(); 
            }
            public void MarkDirtyAndCallListeners()
            {
                isDirty = true;
                CallListeners(); 
            }

            private bool initialized;
            public void Initialize()
            {
                if (initialized) return;

                if (slider == null) slider = uiRoot.GetComponentInChildren<Slider>(true);
                if (slider != null)
                {
                    if (slider.onValueChanged == null) slider.onValueChanged = new Slider.SliderEvent(); 
                    //slider.onValueChanged.AddListener((float _) => MarkDirtyAndCallListeners());
                }

                if (inputFieldTMP == null) inputFieldTMP = uiRoot.GetComponentInChildren<TMP_InputField>(true);
                if (inputFieldTMP != null)
                {
                    if (inputFieldTMP.onEndEdit == null) inputFieldTMP.onEndEdit = new TMP_InputField.SubmitEvent();
                    //inputFieldTMP.onEndEdit.AddListener((string _) => MarkDirtyAndCallListeners());
                     
                    if (slider != null)
                    {
                        slider.onValueChanged.AddListener((float val) => { try { inputFieldTMP.SetTextWithoutNotify(val.ToString()); } catch { } });
                        inputFieldTMP.onEndEdit.AddListener((string val) => { try { slider.SetValueWithoutNotify(float.Parse(val)); } catch { } }); 
                    }
                }

                if (inputField == null) inputField = uiRoot.GetComponentInChildren<InputField>(true);
                if (inputField != null)
                {
                    if (inputField.onEndEdit == null) inputField.onEndEdit = new InputField.EndEditEvent();
                    //inputField.onEndEdit.AddListener((string _) => MarkDirtyAndCallListeners());

                    if (slider != null)
                    {
                        slider.onValueChanged.AddListener((float val) => { try { inputField.SetTextWithoutNotify(val.ToString()); } catch { } });
                        inputField.onEndEdit.AddListener((string val) => { try { slider.SetValueWithoutNotify(float.Parse(val)); } catch { } });
                    }
                }

                if (toggle == null) toggle = uiRoot.GetComponentInChildren<Toggle>(true);
                if (toggle != null)
                {
                    if (toggle.onValueChanged == null) toggle.onValueChanged = new Toggle.ToggleEvent();
                    //toggle.onValueChanged.AddListener((bool _) => MarkDirtyAndCallListeners());
                }

                // Subscribe last so that the elements will sync with each other first
                if (inputFieldTMP != null) inputFieldTMP.onEndEdit.AddListener((string _) => MarkDirtyAndCallListeners());
                if (inputField != null) inputField.onEndEdit.AddListener((string _) => MarkDirtyAndCallListeners());
                if (slider != null) slider.onValueChanged.AddListener((float _) => MarkDirtyAndCallListeners());
                if (toggle != null) toggle.onValueChanged.AddListener((bool _) => MarkDirtyAndCallListeners());
            }

        }

        public delegate object GetExternalSettingsObjectDelegate();
        public delegate void SetExternalSettingsObjectDelegate(object settingsObject);

        public GetExternalSettingsObjectDelegate getExternalSettings;
        public SetExternalSettingsObjectDelegate setExternalSettings;

        public Setting[] settings;

        protected virtual void Awake()
        {
            if (settings != null) foreach (var setting in settings) 
                {
                    setting.Initialize();
                    setting.AddListener(Push); 
                }
        }
        protected virtual void OnEnable()
        {
            Pull();
        }

        public void Push() => Push(true);
        public void Push(bool onlyDirty)
        {
            if (settings != null)
            {
                var settingsObject = getExternalSettings == null ? GetDefaultExternalSettings() : getExternalSettings();

                var objType = settingsObject.GetType(); 
                foreach (var setting in settings)
                {
                    if (setting == null || (onlyDirty && !setting.isDirty)) continue;

                    setting.isDirty = false;

                    var fieldInfo = objType.GetField(setting.name);
                    var propertyInfo = objType.GetProperty(setting.name);

                    try
                    {
                        if (fieldInfo != null)
                        {
                            if (typeof(float).IsAssignableFrom(fieldInfo.FieldType) || typeof(double).IsAssignableFrom(fieldInfo.FieldType))
                            {
                                fieldInfo.SetValue(settingsObject, setting.GetValueFromUI<float>());
                            }
                            else if (typeof(int).IsAssignableFrom(fieldInfo.FieldType) || typeof(short).IsAssignableFrom(fieldInfo.FieldType) || typeof(long).IsAssignableFrom(fieldInfo.FieldType) || typeof(double).IsAssignableFrom(fieldInfo.FieldType))
                            {
                                fieldInfo.SetValue(settingsObject, setting.GetValueFromUI<int>());
                            }
                            else if (typeof(bool).IsAssignableFrom(fieldInfo.FieldType))
                            {
                                fieldInfo.SetValue(settingsObject, setting.GetValueFromUI<bool>());
                            }
                            else if (typeof(string).IsAssignableFrom(fieldInfo.FieldType))
                            {
                                fieldInfo.SetValue(settingsObject, setting.GetValueFromUI<string>());
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        swole.LogError(ex);
                    }

                    try
                    {
                        if (propertyInfo != null)
                        {
                            if (typeof(float).IsAssignableFrom(propertyInfo.PropertyType) || typeof(double).IsAssignableFrom(propertyInfo.PropertyType))
                            {
                                propertyInfo.SetValue(settingsObject, setting.GetValueFromUI<float>());
                            }
                            else if (typeof(int).IsAssignableFrom(propertyInfo.PropertyType) || typeof(short).IsAssignableFrom(propertyInfo.PropertyType) || typeof(long).IsAssignableFrom(propertyInfo.PropertyType) || typeof(double).IsAssignableFrom(propertyInfo.PropertyType))
                            {
                                propertyInfo.SetValue(settingsObject, setting.GetValueFromUI<int>());
                            }
                            else if (typeof(bool).IsAssignableFrom(propertyInfo.PropertyType))
                            {
                                propertyInfo.SetValue(settingsObject, setting.GetValueFromUI<bool>());
                            }
                            else if (typeof(string).IsAssignableFrom(propertyInfo.PropertyType))
                            {
                                propertyInfo.SetValue(settingsObject, setting.GetValueFromUI<string>());
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        swole.LogError(ex);
                    }
                }

                if (setExternalSettings == null)
                {
                    SetDefaultExternalSettings(settingsObject);
                } 
                else
                {
                    setExternalSettings(settingsObject);
                }
            }
        }

        public void Pull()
        {
            if (settings != null)
            {
                var settingsObject = getExternalSettings == null ? GetDefaultExternalSettings() : getExternalSettings(); 

                var objType = settingsObject.GetType();
                foreach (var setting in settings)
                {
                    if (setting == null) continue;

                    var fieldInfo = objType.GetField(setting.name);
                    var propertyInfo = objType.GetProperty(setting.name);

                    try
                    {
                        if (fieldInfo != null)
                        {
                            if (typeof(float).IsAssignableFrom(fieldInfo.FieldType) || typeof(double).IsAssignableFrom(fieldInfo.FieldType))
                            {
                                setting.SetValueInUI((float)fieldInfo.GetValue(settingsObject));
                            }
                            else if (typeof(int).IsAssignableFrom(fieldInfo.FieldType) || typeof(short).IsAssignableFrom(fieldInfo.FieldType) || typeof(long).IsAssignableFrom(fieldInfo.FieldType) || typeof(double).IsAssignableFrom(fieldInfo.FieldType))
                            {
                                setting.SetValueInUI((int)fieldInfo.GetValue(settingsObject));
                            }
                            else if (typeof(bool).IsAssignableFrom(fieldInfo.FieldType))
                            {
                                setting.SetValueInUI((bool)fieldInfo.GetValue(settingsObject));
                            }
                            else if (typeof(string).IsAssignableFrom(fieldInfo.FieldType))
                            {
                                setting.SetValueInUI((string)fieldInfo.GetValue(settingsObject));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        swole.LogError(ex);
                    }

                    try
                    {
                        if (propertyInfo != null)
                        {
                            if (typeof(float).IsAssignableFrom(propertyInfo.PropertyType) || typeof(double).IsAssignableFrom(propertyInfo.PropertyType))
                            {
                                setting.SetValueInUI((float)propertyInfo.GetValue(settingsObject));
                            }
                            else if (typeof(int).IsAssignableFrom(propertyInfo.PropertyType) || typeof(short).IsAssignableFrom(propertyInfo.PropertyType) || typeof(long).IsAssignableFrom(propertyInfo.PropertyType) || typeof(double).IsAssignableFrom(propertyInfo.PropertyType))
                            {
                                setting.SetValueInUI((int)propertyInfo.GetValue(settingsObject));
                            }
                            else if (typeof(bool).IsAssignableFrom(propertyInfo.PropertyType))
                            {
                                setting.SetValueInUI((bool)propertyInfo.GetValue(settingsObject));
                            }
                            else if (typeof(string).IsAssignableFrom(propertyInfo.PropertyType))
                            {
                                setting.SetValueInUI((string)propertyInfo.GetValue(settingsObject)); 
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        swole.LogError(ex);
                    }
                }
            }
        }

    }
}

#endif