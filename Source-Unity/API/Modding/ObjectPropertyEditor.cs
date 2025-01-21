#if (UNITY_EDITOR || UNITY_STANDALONE)

using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

using TMPro;

using Swole.UI;
using Swole.API.Unity;


namespace Swole.Modding
{
    public class ObjectPropertyEditor : MonoBehaviour
    {

        public delegate void UpdateFieldDelegate(RectTransform uiInputRoot, object preEditedObject, object postEditedObject, string fieldName, object preEditedFieldValue, object postEditedFieldValue);
        public delegate void CompleteEditDelegate(object preEditedObject, object finalEditedObject);
        public delegate void SetValueDelegate(object val);
        public delegate object GetValueDelegate();
        public delegate void SetFieldValueDelegate(object instance, object val);
        public delegate object GetFieldValueDelegate(object instance);

        public delegate object ConvertFieldDelegate(EditorInstance.CustomFieldInfo[] fieldInfoChain, string fieldName, object fieldValue);
        public delegate object ConvertObjectWithContextDelegate(EditorInstance.CustomFieldInfo[] fieldInfoChain, object obj, object context);
        public delegate void AddFieldConvertersDelegate(Dictionary<string, FieldValueConverter> fieldValueConverters, Dictionary<string, ObjectWithContextConverter> objectWithContextConverters);

        public delegate void IdsToTypesDelegate(Dictionary<string, Type> idsToTypes);

        public class FieldValueConverter
        {
            public Type inputType;
            public ConvertFieldDelegate convert;

            public FieldValueConverter(Type inputType, ConvertFieldDelegate convert)
            {
                this.inputType = inputType;
                this.convert = convert;
            }
        }
        public class ObjectWithContextConverter
        {
            public Type inputType;
            public ConvertObjectWithContextDelegate convert;

            public ObjectWithContextConverter(Type inputType, ConvertObjectWithContextDelegate convert)
            {
                this.inputType = inputType;
                this.convert = convert;
            }
        }

        protected readonly Dictionary<string, FieldValueConverter> fieldValueConverters = new Dictionary<string, FieldValueConverter>();
        protected readonly Dictionary<string, ObjectWithContextConverter> objectWithContextConverters = new Dictionary<string, ObjectWithContextConverter>();
        public void SetFieldValueConversionForMaskId(string typeMaskId, FieldValueConverter converter) => fieldValueConverters[typeMaskId] = converter;
        public static bool TryConvertFieldValue(EditorInstance.CustomFieldInfo[] fieldInfoChain, Dictionary<string, FieldValueConverter> fieldValueConverters, string typeMaskId, string fieldName, object fieldVal, out object convertedValue)
        {
            convertedValue = null;
            if (fieldValueConverters.TryGetValue(typeMaskId, out var converter) && converter != null && converter.convert != null)
            {
                convertedValue = converter.convert(fieldInfoChain, fieldName, fieldVal);
                return true; 
            }

            return false;
        }
        public void SetObjectWithContextConversionForMaskId(string typeMaskId, ObjectWithContextConverter converter) => objectWithContextConverters[typeMaskId] = converter;
        public static bool TryConvertObjectWithContext(EditorInstance.CustomFieldInfo[] fieldInfoChain, Dictionary<string, ObjectWithContextConverter> objectWithContextConverters, string typeMaskId, object input, object context, out object output)
        {
            output = null;
            if (objectWithContextConverters.TryGetValue(typeMaskId, out var converter) && converter != null && converter.convert != null)
            {
                output = converter.convert(fieldInfoChain, input, context);
                return true; 
            }
             
            return false;
        }
        protected readonly Dictionary<string, Type> enumIdsToTypes = new Dictionary<string, Type>();
        public void SetEnumIdType(string enumId, Type type) => enumIdsToTypes[enumId] = type;
        public static bool TryConvertEnumIdToType(Dictionary<string, Type> enumIdsToTypes, string enumId, out Type type) => enumIdsToTypes.TryGetValue(enumId, out type);

        public static object ConvertToTypeFromString(Type type, string val)
        {

            if (ReferenceEquals(val, null))
            {
                try
                {
                    if (type.IsValueType) return Activator.CreateInstance(type);
                } 
                catch(Exception ex)
                {
#if UNITY_EDITOR
                    Debug.LogError($"Error creating default instance of type {type.Name}"); 
                    Debug.LogError(ex);
#endif
                }

                return null;
            } 

            object output = null;
            var converter = TypeDescriptor.GetConverter(type);

            try
            {
                output = converter.ConvertFromString(val);
            }
            catch
            {
                try
                {
                    output = converter.ConvertFromInvariantString(val);
                }
                catch
                {
                    try
                    {
                        output = converter.ConvertFrom(val);
                    }
                    catch
                    {
                        try
                        {
                            if (type.IsValueType) return Activator.CreateInstance(type); 
                        }
                        catch (Exception ex)
                        {
#if UNITY_EDITOR
                            Debug.LogError($"Error creating default instance of type {type.Name}");
                            Debug.LogError(ex);
#endif
                        }

                        return null;
                    }
                }
            }

            return output;
        }

        public GameObject listPrototype;
        protected PrefabPool listPool;
        public RectTransform mainListWindow;
        public RectTransform mainListContainer;
        public RectTransform subListWindow;
        public RectTransform subListContainer;

        public float indentWidthStart = 6;
        public float indentWidthIncrement = 16;

        [Header("Input Prototypes")]
        public RectTransform protoBoolInput;
        public RectTransform protoFloatInput;
        public RectTransform protoIntInput;
        public RectTransform protoStringInput;
        public RectTransform protoEnumDropdown;
        public RectTransform protoFloatSlider;
        public RectTransform protoIntSlider;
        public RectTransform protoArray;
        public RectTransform protoArrayElement;

        [Header("Events")]
        public UnityEvent OnRedraw;

        public class EditorInstance
        {
            public RectTransform listRoot;
            public UICategorizedList list;
            public void SetListRoot(RectTransform listRoot, RectTransform windowRoot, RectTransform windowContainer)
            {
                if (windowRoot == null) windowRoot = windowContainer;

                this.listRoot = listRoot;
                if (listRoot != null)
                {
                    listRoot.gameObject.SetActive(true);
                    if (windowContainer != null)
                    {
                        listRoot.SetParent(windowContainer, false);
                        if (listRoot.anchorMin.x != listRoot.anchorMax.x) listRoot.sizeDelta = new Vector2(0, listRoot.sizeDelta.y);
                        if (listRoot.anchorMin.y != listRoot.anchorMax.y) listRoot.sizeDelta = new Vector2(listRoot.sizeDelta.x, 0);
                        listRoot.anchoredPosition = Vector2.zero;
                    }

                    list = listRoot.GetComponentInChildren<UICategorizedList>(true);
                    var popup = (windowRoot == null ? listRoot : windowRoot).GetComponentInChildren<UIPopup>(true);
                    if (popup != null)
                    {
                        if (popup.OnClose == null) popup.OnClose = new UnityEngine.Events.UnityEvent();
                        popup.OnClose.RemoveAllListeners();
                        popup.OnClose.AddListener(CompleteEdit);
                    }

                    CustomEditorUtils.SetButtonOnClickActionByName((windowRoot == null ? listRoot : windowRoot), "finalize", CompleteEdit);  
                }
            }
            public bool IsValid => listRoot != null;
            protected object preEditedObject;
            public SetValueDelegate setObject;
            public GetValueDelegate getObject;
            public VoidParameterlessDelegate saveObject;
            public event UpdateFieldDelegate OnFieldUpdate;
            public event CompleteEditDelegate OnCompleteEdit;
            public event VoidParameterlessDelegate OnRedraw;
            public void ClearListeners()
            {
                OnFieldUpdate = null;
                OnCompleteEdit = null;
                OnRedraw = null;
            }

            //protected readonly List<EditorInstance> subEditors = new List<EditorInstance>();

            protected PrefabPool listPool;

            public RectTransform listWindow;
            public RectTransform listContainer;
            public RectTransform subListWindow;
            public RectTransform subListContainer;

            public float indentWidthStart = 6;
            public float indentWidthIncrement = 16;

            public EditorInstance(object obj, SetValueDelegate setObject, GetValueDelegate getObject, VoidParameterlessDelegate saveObject, PrefabPool listPool, RectTransform listRoot, RectTransform listWindow, RectTransform listContainer, RectTransform subListWindow, RectTransform subListContainer)
            {
                preEditedObject = obj;
                this.setObject = setObject;
                this.getObject = getObject;
                this.saveObject = saveObject;
                this.listPool = listPool;
                SetListRoot(listRoot, listWindow, listContainer);

                this.listWindow = listWindow;
                this.listContainer = listContainer;
                this.subListWindow = subListWindow;
                this.subListContainer = subListContainer;
            }

            public void CompleteEdit()
            {
                //foreach (var subEditor in subEditors) subEditor.CompleteEdit();

                if (!ReferenceEquals(preEditedObject, null))
                {
                    OnCompleteEdit?.Invoke(preEditedObject, getObject()); 
                }

                ClearListeners();
                if (listPool != null && listRoot != null)
                {
                    listPool.Release(listRoot);
                    listRoot.gameObject.SetActive(false);
                    listRoot = null;
                    list = null;
                }

                preEditedObject = getObject();
            }

            public class CustomFieldInfo
            {

                public GetValueDelegate getBoundValue;
                public object BoundValue => getBoundValue == null ? null : getBoundValue.Invoke();

                protected FieldInfo fieldInfo;
                public FieldInfo FieldInfo => fieldInfo;
                public bool HasFieldInfo => fieldInfo != null;
                protected string fieldName;
                protected Type fieldType;

                protected string elementName;

                protected SetFieldValueDelegate setFieldValue;
                protected GetFieldValueDelegate getFieldValue;

                public CustomFieldInfo(FieldInfo fieldInfo) : this(fieldInfo, fieldInfo == null ? string.Empty : fieldInfo.Name) { }
                public CustomFieldInfo(FieldInfo fieldInfo, string elementName)
                {
                    this.fieldInfo = fieldInfo;
                    this.elementName = elementName; 
                }

                public CustomFieldInfo(string fieldName, Type fieldType, SetFieldValueDelegate setFieldValue, GetFieldValueDelegate getFieldValue) : this(fieldName, fieldName, fieldType, setFieldValue, getFieldValue) { }
                public CustomFieldInfo(string fieldName, string elementName, Type fieldType, SetFieldValueDelegate setFieldValue, GetFieldValueDelegate getFieldValue)
                {
                    this.fieldInfo = null;
                    this.fieldName = fieldName;
                    this.elementName = elementName;
                    this.fieldType = fieldType;
                    this.setFieldValue = setFieldValue;
                    this.getFieldValue = getFieldValue;
                }

                public string Name => fieldInfo == null ? fieldName : fieldInfo.Name;
                public string ElementName => elementName;
                public Type FieldType => fieldInfo == null ? fieldType : fieldInfo.FieldType;

                public void SetValue(object instance, object value)
                {
                    if (fieldInfo != null)
                    {
                        fieldInfo.SetValue(instance, value);
                    } 
                    else if (setFieldValue != null)
                    {
                        setFieldValue(instance, value);
                    }
                }
                public object GetValue(object instance)
                {
                    if (fieldInfo != null)
                    {
                        return fieldInfo.GetValue(instance);
                    }
                    else if (setFieldValue != null)
                    {
                        return getFieldValue(instance);
                    }

                    return null;
                }

            }

            public const string button_AddMainElement = "AddMainElement";
            public const string button_AddElement = "AddElement";
            public const string button_DeleteElement = "DeleteElement";
            public const string button_MoveElementUp = "MoveElementUp";
            public const string button_MoveElementDown = "MoveElementDown";

            protected void DisableButton(RectTransform root, string buttonName)
            {
                var buttonRoot = root.FindDeepChildLiberal(buttonName);
                if (buttonRoot != null) buttonRoot.gameObject.SetActive(false);
            }
            protected void DisableButtons(RectTransform root)
            {
                DisableButton(root, button_AddMainElement);
                DisableButton(root, button_AddElement);
                DisableButton(root, button_DeleteElement);
                DisableButton(root, button_MoveElementUp);
                DisableButton(root, button_MoveElementDown);
            }
            private readonly List<CustomFieldInfo> _fieldInfoChain = new List<CustomFieldInfo>();
            public IEnumerator DelayedRedraw(RectTransform protoBoolInput, RectTransform protoFloatInput, RectTransform protoIntInput, RectTransform protoStringInput, RectTransform protoEnumDropdown, RectTransform protoFloatSlider, RectTransform protoIntSlider, RectTransform protoArray, RectTransform protoArrayElement, ICollection<string> validFields, int parentNameStartDepth, Dictionary<string, FieldValueConverter> fieldValueConverters, Dictionary<string, ObjectWithContextConverter> objectWithContextConverters, Dictionary<string, Type> enumIdsToTypes)
            {
                yield return null;
                yield return null;
                Redraw(protoBoolInput, protoFloatInput, protoIntInput, protoStringInput, protoEnumDropdown, protoFloatSlider, protoIntSlider, protoArray, protoArrayElement, validFields, parentNameStartDepth, fieldValueConverters, objectWithContextConverters, enumIdsToTypes);
            }
            private readonly Dictionary<string, bool> categoryExpandedStates = new Dictionary<string, bool>();
            private struct CategoryBinding
            {
                public string fullElementName;
                public UICategorizedList.Category category;
            }
            private readonly List<CategoryBinding> categoryBindings = new List<CategoryBinding>();
            public bool Redraw(RectTransform protoBoolInput, RectTransform protoFloatInput, RectTransform protoIntInput, RectTransform protoStringInput, RectTransform protoEnumDropdown, RectTransform protoFloatSlider, RectTransform protoIntSlider, RectTransform protoArray, RectTransform protoArrayElement, ICollection<string> validFields, int parentNameStartDepth = 3, Dictionary<string, FieldValueConverter> fieldValueConverters = null, Dictionary<string, ObjectWithContextConverter> objectWithContextConverters = null, Dictionary<string, Type> enumIdsToTypes = null)
            {
                categoryBindings.Clear();
                void RedrawAgain()
                {
                    foreach(var binding in categoryBindings)
                    {
                        if (string.IsNullOrWhiteSpace(binding.fullElementName) || binding.category == null) continue; 
                        categoryExpandedStates[binding.fullElementName] = binding.category.IsExpanded;
                    }
                    categoryBindings.Clear();
                    CoroutineProxy.Start(DelayedRedraw(protoBoolInput, protoFloatInput, protoIntInput, protoStringInput, protoEnumDropdown, protoFloatSlider, protoIntSlider, protoArray, protoArrayElement, validFields, parentNameStartDepth, fieldValueConverters, objectWithContextConverters, enumIdsToTypes));
                }

                _fieldInfoChain.Clear();

                bool IsValidField(string fullFieldName)
                {
                    if (validFields == null) return true;

                    foreach(var validField in validFields) if (validField == fullFieldName) return true;
                    return false;
                }
                if (preEditedObject == null) return false;
                var objectType = preEditedObject.GetType();
                var fields = objectType.GetFields(BindingFlags.Public | BindingFlags.Instance);
                if (fields == null || (fields.Length <= 0 && !objectType.IsArray && (!typeof(IList).IsAssignableFrom(objectType) || !objectType.IsGenericType))) return false;
                if (list == null)
                {
                    if (listRoot == null)
                    {
                        if (!listPool.TryGetNewInstance(out GameObject listRootGO)) return false; 
                        listRoot = listRootGO.AddOrGetComponent<RectTransform>();
                    }

                    SetListRoot(listRoot, listWindow, listContainer);
                }
                if (list == null) return false;

                list.Clear(true, true);  
                void AddField(List<CustomFieldInfo> fieldInfoChain, int depth, int index, UICategorizedList.Category category, bool useDisplayPrefix, string displayPrefix, string parentName, CustomFieldInfo fieldInfo, object fieldValue, SetValueDelegate setOwner, GetValueDelegate getOwner, SetValueDelegate setValue, GetValueDelegate getValue)
                {
                    if (depth < parentNameStartDepth) parentName = string.Empty;

                    object fieldOwner = getOwner();
                    int depthM1 = depth - 1;

                    var nonSerializedAttr = fieldInfo.HasFieldInfo ? fieldInfo.FieldInfo.GetCustomAttribute<NonSerializedAttribute>(true) : null;
                    if (nonSerializedAttr != null) return;
                    var nonEditableAttr = fieldInfo.HasFieldInfo ? fieldInfo.FieldInfo.GetCustomAttribute<NonEditableAttribute>(true) : null; 
                    if (nonEditableAttr != null) return;

                    bool isTopLevel = false;
                    string fullElementName = string.Empty;

                    var overridableAttr = fieldInfo.HasFieldInfo ? fieldInfo.FieldInfo.GetCustomAttribute<OverrideDependencyAttribute>(true) : null;
                    bool isOverridable = false;
                    SetValueDelegate setOverridableValue = null;
                    FieldInfo overridableFieldInfo = fieldInfo.FieldInfo;
                    IOverridable overridable = null;
                    bool overridableIsOwner = false;
                    if (fieldValue is IOverridable) 
                    {
                        if (fieldValue is OverridableFlag) return;

                        overridable = (IOverridable)fieldValue;  
                    } 
                    else if (fieldOwner is IOverridable)
                    {
                        overridable = (IOverridable)fieldOwner;
                        overridableIsOwner = true;
                    }

                    if (overridableAttr != null && !string.IsNullOrWhiteSpace(overridableAttr.DependencyFieldName))
                    {
                        try
                        {
                            overridableFieldInfo = fieldOwner.GetType().GetField(overridableAttr.DependencyFieldName, BindingFlags.Instance | BindingFlags.Public);
                            if (overridableFieldInfo != null && (typeof(IOverridable).IsAssignableFrom(overridableFieldInfo.FieldType)))
                            {
                                var val = overridableFieldInfo.GetValue(fieldOwner);
                                if (ReferenceEquals(val, null)) return;

                                var overridable_ = (IOverridable)val;
                                if (ReferenceEquals(overridable, null) || !overridable_.IsOverridden) overridable = overridable_; // if field is also overridable, only set this if the dependency isn't overriding
                            }
                        }
                        catch (Exception ex) 
                        {
                            swole.LogError(ex);
                            return;
                        }
                    }
                    if (overridable != null && !(fieldOwner is Array) && !(fieldOwner is IList))
                    {
                        if (!overridable.IsOverridden) return;
                        isOverridable = true;

                        if (overridableFieldInfo != null)
                        {
                            setOverridableValue = (object val) =>
                            {
                                if (overridableIsOwner)
                                {
                                    setOwner(val);
                                }
                                else
                                {
                                    var owner = getOwner();
                                    overridableFieldInfo.SetValue(owner, val);
                                    setOwner(owner);
                                }
                            };
                        }
                    }

                    if (fieldInfo == null) return;
                    var fieldType = fieldInfo.FieldType;  
                    if (fieldType == null) return;                

                    if (ReferenceEquals(fieldValue, null))
                    {
                        if (typeof(string).IsAssignableFrom(fieldType))
                        {
                            fieldValue = string.Empty;
                            setValue(fieldValue);
                        }
                        else if (typeof(Array).IsAssignableFrom(fieldType))
                        {
                            fieldValue = Array.CreateInstance(fieldType.GetElementType(), 0);
                            setValue(fieldValue);
                        }
                        else// if (typeof(IList).IsAssignableFrom(fieldType))
                        {
                            try
                            {
                                fieldValue = System.Activator.CreateInstance(fieldType);
                                setValue(fieldValue);
                            }
                            catch //(Exception ex)
                            {
#if UNITY_EDITOR
                                //swole.LogError(ex);
#endif
                                return;
                            }
                        }
                        //else return;
                    }

                    string displayName = fieldInfo.Name;
                    string baseDisplayName = displayName;
                    string fullFieldName = string.IsNullOrWhiteSpace(parentName) ? displayName : $"{parentName}.{displayName}";
                    if (!IsValidField(fullFieldName)) return;
                    fullElementName = string.IsNullOrWhiteSpace(parentName) ? fieldInfo.ElementName : $"{parentName}.{fieldInfo.ElementName}"; 

                    fieldInfo.getBoundValue = getValue;

                    var redrawAttr = fieldInfo.HasFieldInfo ? fieldInfo.FieldInfo.GetCustomAttribute<RedrawOnEditAttribute>() : null;
                    bool redrawOnEdit = redrawAttr != null;

                    var aliasAttr = fieldInfo.HasFieldInfo ? fieldInfo.FieldInfo.GetCustomAttribute<EditableAliasAttribute>() : null;
                    if (aliasAttr != null) 
                    {
                        displayName = aliasAttr.Alias;
                        baseDisplayName = displayName;
                    }
                    var prefixAttr = fieldInfo.HasFieldInfo ? fieldInfo.FieldInfo.GetCustomAttribute<UseEditablePrefixAttribute>(true) : null;
                    if (prefixAttr != null) useDisplayPrefix = prefixAttr.Flag;
                    //displayName = string.IsNullOrWhiteSpace(parentName) ? displayName : $"{parentName}.{displayName}"; 

                    if (useDisplayPrefix && !string.IsNullOrWhiteSpace(displayPrefix)) displayName = $"{displayPrefix}{displayName}"; 

                    if (category == null) 
                    {
                        category = list.AddNewCategory(displayName);
                        categoryBindings.Add(new CategoryBinding() { fullElementName = fullElementName, category = category });
                        isTopLevel = true;
                        DisableButtons(category.rectTransform); 

                        if (categoryExpandedStates.TryGetValue(fullElementName, out var expandedState)) 
                        {
                            IEnumerator SetExpandedStateDelayed()
                            {
                                yield return null;
                                if (category != null)
                                {
                                    if (expandedState) category.Expand(); else category.Retract();
                                }
                            }
                            CoroutineProxy.Start(SetExpandedStateDelayed());
                        }
                    }

                    void CreateMember(string memberName, out UICategorizedList.Member member, out RectTransform container, RectTransform prototype, out RectTransform inputRoot)
                    {
                        member = list.AddNewListMember(memberName, category, null);
                        //container = member.rectTransform.FindDeepChildLiberal("ValueContainer") as RectTransform;
                        container = null;
                        for(int a = 0; a < member.rectTransform.childCount; a++)
                        {
                            var child = member.rectTransform.GetChild(a);
                            if (child.name.AsID() == "valuecontainer") 
                            { 
                                container = child as RectTransform;
                                break;
                            }
                        }
                        if (container == null) container = member.rectTransform;

                        for (int a = 0; a < container.childCount; a++) 
                        { 
                            var child = container.GetChild(a);
                            if (child.name.AsID() == "editvalue") GameObject.DestroyImmediate(child.gameObject);  
                        }

                        inputRoot = Instantiate(prototype);
                        inputRoot.name = "EditValue";
                        inputRoot.SetParent(container, false);
                        if (inputRoot.anchorMin.x != inputRoot.anchorMax.x) inputRoot.sizeDelta = new Vector2(0, inputRoot.sizeDelta.y);
                        if (inputRoot.anchorMin.y != inputRoot.anchorMax.y) inputRoot.sizeDelta = new Vector2(inputRoot.sizeDelta.x, 0); 
                        inputRoot.anchoredPosition = Vector2.zero;
                        inputRoot.gameObject.SetActive(true);

                        var indent = member.rectTransform.FindDeepChildLiberal("indent");
                        if (indent is RectTransform indentRT)
                        {
                            indentRT.sizeDelta = new Vector2(indentWidthStart + indentWidthIncrement * (depth - 2), indentRT.sizeDelta.y); // top level will be a category so subtract 2 instead of 1
                        } 
                    }

                    bool ownerIsCollection = false;
                    UICategorizedList.Member fieldMember = null;
                    RectTransform fieldContainer = null;
                    RectTransform inputRoot = null;
                    if (fieldOwner is Array ownerArray)
                    {
                        ownerIsCollection = true;

                        RectTransform rootT = category.rectTransform;
                        if (!isTopLevel)
                        {
                            CreateMember(displayName + ".control", out fieldMember, out fieldContainer, protoArrayElement, out inputRoot);
                            rootT = fieldMember.rectTransform; 
                        }
                        DisableButtons(rootT);
                        
                        CustomEditorUtils.SetButtonOnClickActionByName(rootT, button_DeleteElement, () =>
                        {
                            var newArray = Array.CreateInstance(ownerArray.GetType().GetElementType(), ownerArray.Length - 1);
                            for (int a = 0; a < index; a++) newArray.SetValue(ownerArray.GetValue(a), a);
                            for (int a = index + 1; a < ownerArray.Length; a++) newArray.SetValue(ownerArray.GetValue(a), a - 1);
                            ownerArray = newArray;
                            fieldOwner = newArray;
                            setOwner(newArray);

                            RedrawAgain();
                        });
                        if (index > 0) CustomEditorUtils.SetButtonOnClickActionByName(rootT, button_MoveElementUp, () =>
                        {
                            var temp = ownerArray.GetValue(index - 1);
                            ownerArray.SetValue(getValue(), index - 1);
                            ownerArray.SetValue(temp, index);

                            RedrawAgain();
                        });
                        if (index < ownerArray.Length - 1) CustomEditorUtils.SetButtonOnClickActionByName(rootT, button_MoveElementDown, () =>
                        {
                            var temp = ownerArray.GetValue(index + 1);
                            ownerArray.SetValue(getValue(), index + 1);
                            ownerArray.SetValue(temp, index);

                            RedrawAgain();
                        });

                    }
                    else if (fieldOwner is IList ownerList)
                    {
                        ownerIsCollection = true;

                        RectTransform rootT = category.rectTransform;
                        if (!isTopLevel)
                        {
                            CreateMember(displayName + ".control", out fieldMember, out fieldContainer, protoArrayElement, out inputRoot);
                            rootT = fieldMember.rectTransform;
                        }
                        DisableButtons(rootT);  

                        CustomEditorUtils.SetButtonOnClickActionByName(rootT, button_DeleteElement, () =>
                        {
                            ownerList.RemoveAt(index);
                            RedrawAgain();
                        });
                        if (index > 0) CustomEditorUtils.SetButtonOnClickActionByName(rootT, button_MoveElementUp, () =>
                        {
                            var temp = ownerList[index - 1];
                            ownerList[index - 1] = getValue();
                            ownerList[index] = temp;

                            RedrawAgain();
                        });
                        if (index < ownerList.Count - 1) CustomEditorUtils.SetButtonOnClickActionByName(rootT, button_MoveElementDown, () =>
                        {
                            var temp = ownerList[index + 1];
                            ownerList[index + 1] = getValue(); 
                            ownerList[index] = temp;

                            RedrawAgain();
                        });
                    } 
                    else if (isOverridable)
                    {
                        if (isTopLevel)
                        {
                            CustomEditorUtils.SetButtonOnClickActionByName(category.rectTransform, button_DeleteElement, () =>
                            {
                                overridable.IsOverridden = false;
                                setOverridableValue?.Invoke(overridable);

                                RedrawAgain();
                            });
                        }
                    }


                    if (fieldInfoChain.Count > depthM1) fieldInfoChain.RemoveRange(depthM1, fieldInfoChain.Count - depthM1);
                    fieldInfoChain.Add(fieldInfo);
                    var localFieldInfoChain = fieldInfoChain.ToArray();


                    int conversionDepthOffset = 0;
                    var getEditValue = getValue;
                    var setEditValue = setValue;
                    var fieldEditType = fieldType;
                    var conversionAttrs = fieldInfo.HasFieldInfo ? fieldInfo.FieldInfo.GetCustomAttributes<PropertyTypeConverterAttribute>() : null;
                    if (conversionAttrs == null && ownerIsCollection && depthM1 > 0)
                    {
                        var ownerFieldInfo = localFieldInfoChain[depthM1 - 1];
                        if (ownerFieldInfo != null) 
                        {
                            conversionAttrs = ownerFieldInfo.HasFieldInfo ? ownerFieldInfo.FieldInfo.GetCustomAttributes<PropertyTypeConverterAttribute>() : null;
                            conversionDepthOffset = -1; 
                        } 
                    }
                    if (conversionAttrs != null)
                    {
                        foreach (var conversionAttr in conversionAttrs)
                        {
                            if (conversionAttr.Depth + conversionDepthOffset != 0) continue;  

                            if (conversionAttr.HasCondition)
                            {
                                var condition = conversionAttr.Condition;
                                int conditionDepth = depthM1 + condition.depth;
                                if (conditionDepth > depthM1 || conditionDepth < 0 || conditionDepth >= localFieldInfoChain.Length) continue;

                                if (!condition.CheckInstance(fieldInfoChain[conditionDepth].BoundValue)) continue; 
                            }

                            bool flag = true;
                            if (fieldValueConverters.TryGetValue(conversionAttr.ConverterID, out var fieldValueConverter))
                            {
                                fieldEditType = fieldValueConverter.inputType;
                                setEditValue = (object val) =>
                                {
                                    var temp = val;
                                    val = fieldValueConverter.convert(localFieldInfoChain, fieldInfo.Name, val);
                                    setValue(val);
                                };
                            }
                            else flag = false;

                            if (flag && objectWithContextConverters.TryGetValue(conversionAttr.ConverterID, out var objectWithContextConverter))
                            {
                                getEditValue = () =>
                                {
                                    var val = getValue();
                                    var temp = val;
                                    val = objectWithContextConverter.convert(localFieldInfoChain, val, null);

                                    return val;
                                };
                            }
                            else flag = false;

                            if (!flag)
                            {
                                getEditValue = getValue;
                                setEditValue = setValue;
                                fieldEditType = fieldType;
                            }
                            else
                            {
                            }
                        }
                    }
                    var editAsEnumAttr = fieldInfo.HasFieldInfo ? fieldInfo.FieldInfo.GetCustomAttribute<EditAsEnumAttribute>() : null;
                    if (editAsEnumAttr != null)
                    {
                        Type enumType = null;
                        if (string.IsNullOrWhiteSpace(editAsEnumAttr.EnumId))
                        {
                            enumType = editAsEnumAttr.EnumType;
                        }
                        else
                        {
                            if (enumIdsToTypes != null) enumIdsToTypes.TryGetValue(editAsEnumAttr.EnumId, out enumType);
                        }
                        if (enumType != null)
                        {
                            fieldEditType = enumType;
                            getEditValue = () =>
                            {
                                var val = getValue();

                                var valType = fieldType;
                                if (val is IOverridable overridable)
                                {
                                    valType = overridable.ValueType;
                                    val = overridable.Value;
                                }

                                if (typeof(string).IsAssignableFrom(valType))
                                {
                                    val = val == null ? Enum.ToObject(enumType, 0) : (string.IsNullOrWhiteSpace((string)val) ? Enum.ToObject(enumType, 0) : Enum.Parse(enumType, (string)val));
                                }
                                else
                                {
                                    val = Enum.ToObject(enumType, val); 
                                }


                                return val;
                            };
                            setEditValue = (object val) =>
                            {
                                var valType = fieldType;
                                if (isOverridable) valType = overridable.ValueType;                      

                                if (typeof(string).IsAssignableFrom(valType))
                                {
                                    val = val.ToString();
                                } 
                                else
                                {
                                    val = (int)val; 
                                }

                                if (isOverridable)
                                {
                                    var currentVal = getValue();
                                    if (currentVal is IOverridable overridable_) 
                                    { 
                                        overridable_.Value = val;
                                        val = overridable_; 
                                    }
                                }

                                setValue(val);
                            };
                        }
                    }

                    Toggle toggle = null;
                    InputField inputField = null;
                    TMP_InputField inputFieldTMP = null;
                    Slider slider = null;

                    if (typeof(bool).IsAssignableFrom(fieldEditType))
                    {
                        if (isOverridable && fieldInfo.Name.AsID().IndexOf("override") >= 0) return; // ignore any field that is used to indicate that a value is being overridden

                        CreateMember(displayName, out fieldMember, out fieldContainer, protoBoolInput, out inputRoot);
                        toggle = inputRoot.GetComponentInChildren<Toggle>(true);
                    }
                    else if (typeof(float).IsAssignableFrom(fieldEditType))
                    {
                        var rangeAttr = fieldInfo.HasFieldInfo ? fieldInfo.FieldInfo.GetCustomAttribute<RangeAttribute>(true) : null;
                        if (rangeAttr != null)
                        {
                            CreateMember(displayName, out fieldMember, out fieldContainer, protoFloatSlider, out inputRoot);
                            slider = inputRoot.GetComponentInChildren<Slider>(true);
                            if (slider != null)
                            {
                                slider.wholeNumbers = false;
                                slider.minValue = rangeAttr.min;
                                slider.maxValue = rangeAttr.max;
                            }
                        }
                        else
                        {
                            CreateMember(displayName, out fieldMember, out fieldContainer, protoFloatInput, out inputRoot);
                        }
                        inputField = inputRoot.GetComponentInChildren<InputField>(true);
                        inputFieldTMP = inputRoot.GetComponentInChildren<TMP_InputField>(true);
                    }
                    else if (typeof(int).IsAssignableFrom(fieldEditType))
                    {
                        var rangeAttr = fieldInfo.HasFieldInfo ? fieldInfo.FieldInfo.GetCustomAttribute<RangeAttribute>(true) : null; 
                        if (rangeAttr != null)
                        {
                            CreateMember(displayName, out fieldMember, out fieldContainer, protoIntSlider, out inputRoot); 
                            slider = inputRoot.GetComponentInChildren<Slider>(true);
                            if (slider != null)
                            {
                                slider.wholeNumbers = true;
                                slider.minValue = rangeAttr.min;
                                slider.maxValue = rangeAttr.max; 
                            }
                        }
                        else
                        {
                            CreateMember(displayName, out fieldMember, out fieldContainer, protoIntInput, out inputRoot);
                        }
                        inputField = inputRoot.GetComponentInChildren<InputField>(true);
                        inputFieldTMP = inputRoot.GetComponentInChildren<TMP_InputField>(true);
                    }
                    else if (typeof(string).IsAssignableFrom(fieldEditType))
                    {
                        CreateMember(displayName, out fieldMember, out fieldContainer, protoStringInput, out inputRoot); 
                        inputField = inputRoot.GetComponentInChildren<InputField>(true);
                        inputFieldTMP = inputRoot.GetComponentInChildren<TMP_InputField>(true);
                    }
                    else if (fieldEditType.IsEnum)
                    {
                        CreateMember(displayName, out fieldMember, out fieldContainer, protoEnumDropdown, out inputRoot);
                        var dropdown = inputRoot.GetComponentInChildren<UIDynamicDropdown>(true);
                        dropdown.ClearMenuItems();
                        var options = Enum.GetNames(fieldEditType);
                        if (options != null)
                        {
                            foreach(var option in options) dropdown.CreateNewMenuItem(option); 
                        }
                        if (dropdown.OnSelectionChanged == null) dropdown.OnSelectionChanged = new UnityEngine.Events.UnityEvent<string>();
                        dropdown.OnSelectionChanged.RemoveAllListeners();
                        dropdown.OnSelectionChanged.AddListener((string val) => 
                        {
                            if (Enum.TryParse(fieldEditType, val, true, out var result))
                            {
                                var preEdit = fieldOwner;
                                var preEditVal = getEditValue();
                                setEditValue(result);
                                OnFieldUpdate?.Invoke(inputRoot, preEdit, getOwner(), fieldInfo.Name, preEditVal, result);
                                if (redrawOnEdit && preEditVal != result) RedrawAgain(); 
                            }
                        });
                        dropdown.SetSelectionText(getEditValue().ToString(), false); 
                    } 
                    else if (fieldValue is Array array)
                    {
                        var elementType = array.GetType().GetElementType();
                        CreateMember(displayName, out fieldMember, out fieldContainer, protoArray, out inputRoot);
                        CustomEditorUtils.SetButtonOnClickActionByName(inputRoot, button_AddElement, () => 
                        {
                            object newElement = null;
                            try
                            {
                                if (typeof(string).IsAssignableFrom(elementType)) newElement = string.Empty; else newElement = System.Activator.CreateInstance(elementType);
                            }
                            catch (Exception ex)
                            {
#if UNITY_EDITOR
                                swole.LogError(ex);
#endif
                            }
                            var newArray = Array.CreateInstance(elementType, array.Length + 1);
                            array.CopyTo(newArray, 0);
                            newArray.SetValue(newElement, array.Length);
                            array = newArray;
                            fieldValue = array;
                            setValue(array);

                            RedrawAgain();
                        });
                        for (int a = 0; a < array.Length; a++)
                        {
                            var elementIndex = a;

                            var element = array.GetValue(elementIndex); 
                            if (ReferenceEquals(element, null))
                            {
                                try
                                {
                                    element = System.Activator.CreateInstance(elementType);
                                    array.SetValue(element, elementIndex);
                                }
                                catch (Exception ex)
                                {
#if UNITY_EDITOR
                                    swole.LogError(ex);
#endif
                                    continue;
                                }
                            }

                            string elementName = $"Element {a}";
                            string baseElementName = elementName;
                            try
                            {
                                var nameField = elementType.GetField("id");
                                if (nameField == null) nameField = elementType.GetField("name");
                                if (nameField == null) nameField = elementType.GetField("binding");
                                if (nameField == null) nameField = elementType.GetField("settings");
                                if (nameField != null)
                                {
                                    string tempName = string.Empty;
                                    if (typeof(string).IsAssignableFrom(nameField.FieldType))
                                    {
                                        tempName = nameField.GetValue(element) as string;
                                    }
                                    else
                                    {
                                        var strField = nameField.FieldType.GetField("id");
                                        if (strField == null) strField = nameField.FieldType.GetField("name");
                                        if (strField != null && typeof(string).IsAssignableFrom(strField.FieldType)) tempName = strField.GetValue(nameField.GetValue(element)) as string;
                                    }

                                    if (!string.IsNullOrWhiteSpace(tempName)) elementName = $"{tempName}[{a}]";
                                }
                            }
                            catch { }
                             
                            var field_ = new CustomFieldInfo(elementName, baseElementName, elementType, (object instance, object val) => (instance as Array).SetValue(val, elementIndex), (object instance) => (instance as Array).GetValue(elementIndex));
                            AddField(fieldInfoChain, depth + 1, elementIndex, category, useDisplayPrefix, $"{baseDisplayName}.", displayName, field_, element, setValue, getValue, (object val) => 
                            { 
                                field_.SetValue(getValue(), val);
                                saveObject?.Invoke();
                            }, () => field_.GetValue(getValue()));
                        }
                    }   
                    else if (fieldValue is IList list && fieldType.IsGenericType) 
                    {
                        var elementType = list.GetType().GetGenericArguments()[0];
                        CreateMember(displayName, out fieldMember, out fieldContainer, protoArray, out inputRoot);
                        CustomEditorUtils.SetButtonOnClickActionByName(inputRoot, button_AddElement, () =>
                        {
                            object newElement = null;
                            try
                            {
                                if (typeof(string).IsAssignableFrom(elementType)) newElement = string.Empty; else newElement = System.Activator.CreateInstance(elementType);
                            }
                            catch (Exception ex)
                            {
#if UNITY_EDITOR
                                swole.LogError(ex);
#endif
                            }
                            list.Add(newElement);

                            RedrawAgain();
                        });
                        for (int a = 0; a < list.Count; a++)
                        {
                            var elementIndex = a;

                            var element = list[elementIndex];
                            if (ReferenceEquals(element, null))
                            {
                                try
                                {
                                    element = System.Activator.CreateInstance(elementType);
                                    list[elementIndex] = element;
                                }
                                catch (Exception ex)
                                {
#if UNITY_EDITOR
                                    swole.LogError(ex);
#endif
                                    continue;
                                }
                            }

                            string elementName = $"Element {a}";
                            string baseElementName = elementName;
                            try
                            {
                                var nameField = elementType.GetField("id");
                                if (nameField == null) nameField = elementType.GetField("name");
                                if (nameField == null) nameField = elementType.GetField("binding");
                                if (nameField == null) nameField = elementType.GetField("settings");
                                if (nameField != null)
                                {
                                    string tempName = string.Empty;
                                    if (typeof(string).IsAssignableFrom(nameField.FieldType))
                                    {
                                        tempName = nameField.GetValue(element) as string;
                                    }
                                    else
                                    {
                                        var strField = nameField.FieldType.GetField("id");
                                        if (strField == null) strField = nameField.FieldType.GetField("name");
                                        if (strField != null && typeof(string).IsAssignableFrom(strField.FieldType)) tempName = strField.GetValue(nameField.GetValue(element)) as string;
                                    }

                                    if (!string.IsNullOrWhiteSpace(tempName)) elementName = $"{tempName}[{a}]";
                                }
                            }
                            catch { }

                            var field_ = new CustomFieldInfo(elementName, baseElementName, elementType, (object instance, object val) => (instance as IList)[elementIndex] = val, (object instance) => (instance as IList)[elementIndex]);
                            AddField(fieldInfoChain, depth + 1, elementIndex, category, useDisplayPrefix, $"{baseDisplayName}.", displayName, field_, element, setValue, getValue, (object val) => 
                            { 
                                field_.SetValue(getValue(), val);
                                saveObject?.Invoke();
                            }, () => field_.GetValue(getValue())); 
                        }
                    }
                    else
                    {
                        try
                        {

                            var fields = fieldType.GetFields(BindingFlags.Public | BindingFlags.Instance);
                            int i = 0;
                            foreach (var field in fields)
                            {
                                var field_ = new CustomFieldInfo(field);
                                var temp = field;
                                var value = field_.GetValue(fieldValue);
                                if (ReferenceEquals(fieldValue, value)) continue;
                                
                                AddField(fieldInfoChain, depth + 1, i, category, useDisplayPrefix, $"{baseDisplayName}.", displayName, field_, value, setValue, getValue, (object val) =>
                                {
                                    var owner = getValue(); 
                                    field_.SetValue(owner, val);
                                    setValue.Invoke(owner);

                                    saveObject?.Invoke();
                                }, 
                                () => field_.GetValue(getValue()));
                                i++;
                            }

                        }
                        catch (Exception ex)
                        {
#if UNITY_EDITOR
                            swole.LogError(ex);
#endif
                        }
                    }

                    if (toggle != null)
                    {
                        if (toggle.onValueChanged == null) toggle.onValueChanged = new Toggle.ToggleEvent();
                        toggle.onValueChanged.RemoveAllListeners();
                        toggle.onValueChanged.AddListener((bool val) => {
                            var preEdit = fieldOwner;
                            var preEditVal = getEditValue();
                            setEditValue(val);
                            OnFieldUpdate?.Invoke(inputRoot, preEdit, getOwner(), fieldInfo.Name, preEditVal, val);
                            if (redrawOnEdit && preEditVal != getEditValue()) RedrawAgain();
                        });
                        toggle.SetIsOnWithoutNotify((bool)getEditValue()); 
                    }
                    if (inputField != null)
                    {
                        if (inputField.onEndEdit == null) inputField.onEndEdit = new InputField.EndEditEvent();
                        inputField.onEndEdit.RemoveAllListeners();
                        inputField.onEndEdit.AddListener((string val) => {
                            var preEdit = fieldOwner;
                            var preEditVal = getEditValue();
                            if (ReferenceEquals(preEditVal, null) || preEditVal is string)
                            {
                                setEditValue(val);
                            }
                            else
                            {
                                setEditValue(ConvertToTypeFromString(preEditVal.GetType(), val)); 
                            }
                            if (slider != null)
                            {
                                if (float.TryParse(val, out var result))
                                {
                                    slider.SetValueWithoutNotify(result);
                                }
                            }
                            OnFieldUpdate?.Invoke(inputRoot, preEdit, getOwner(), fieldInfo.Name, preEditVal, val);
                            if (redrawOnEdit && preEditVal != getEditValue()) RedrawAgain();
                        });
                        var currentVal = getEditValue();
                        inputField.SetTextWithoutNotify(ReferenceEquals(currentVal, null) ? string.Empty : currentVal.ToString());
                    }
                    if (inputFieldTMP != null)
                    {
                        if (inputFieldTMP.onEndEdit == null) inputFieldTMP.onEndEdit = new TMP_InputField.SubmitEvent();
                        inputFieldTMP.onEndEdit.RemoveAllListeners();
                        inputFieldTMP.onEndEdit.AddListener((string val) => { 
                            var preEdit = fieldOwner;
                            var preEditVal = getEditValue();
                            if (ReferenceEquals(preEditVal, null) || preEditVal is string)
                            {
                                setEditValue(val);
                            }
                            else
                            {
                                setEditValue(ConvertToTypeFromString(preEditVal.GetType(), val));
                            }
                            if (slider != null)
                            {
                                if (float.TryParse(val, out var result))
                                {
                                    slider.SetValueWithoutNotify(result);
                                }
                            }
                            OnFieldUpdate?.Invoke(inputRoot, preEdit, getOwner(), fieldInfo.Name, preEditVal, val);
                            if (redrawOnEdit && preEditVal != getEditValue()) RedrawAgain();
                        });
                        var currentVal = getEditValue();
                        inputFieldTMP.SetTextWithoutNotify(ReferenceEquals(currentVal, null) ? string.Empty : currentVal.ToString());
                    }
                    if (slider != null)
                    {
                        if (slider.onValueChanged == null) slider.onValueChanged = new Slider.SliderEvent();
                        slider.onValueChanged.RemoveAllListeners();
                        slider.onValueChanged.AddListener((float val) =>
                        {
                            var preEdit = fieldOwner;
                            var preEditVal = getEditValue(); 
                            try
                            {
                                var convertedVal = System.Convert.ChangeType(val, fieldType);
                                setEditValue(convertedVal);
                                if (inputField != null) inputField.SetTextWithoutNotify(convertedVal.ToString());
                                if (inputFieldTMP != null) inputFieldTMP.SetTextWithoutNotify(convertedVal.ToString());
                                OnFieldUpdate?.Invoke(inputRoot, preEdit, getOwner(), fieldInfo.Name, preEditVal, val);
                                if (redrawOnEdit && preEditVal != getEditValue()) RedrawAgain(); 
                            }
                            catch (Exception ex)
                            {
                                swole.LogError(ex);
                            }
                        });
                        try
                        {
                            var currentVal = getEditValue();
                            slider.SetValueWithoutNotify((float)System.Convert.ChangeType(currentVal, typeof(float))); 
                        }
                        catch (Exception ex)
                        {
                            swole.LogError(ex);
                        }
                    }
                }

                var editedObject = getObject();
                RectTransform rootTransform = listWindow == null ? listContainer == null ? listRoot : listContainer : listWindow;
                DisableButtons(rootTransform);
                if (editedObject is Array array)
                {
                    var elementType = array.GetType().GetElementType();
                    CustomEditorUtils.SetButtonOnClickActionByName(rootTransform, button_AddMainElement, () =>
                    {
                        object newElement = null;
                        try
                        {
                            if (typeof(string).IsAssignableFrom(elementType)) newElement = string.Empty; else newElement = System.Activator.CreateInstance(elementType);
                        }
                        catch { }
                        var newArray = Array.CreateInstance(elementType, array.Length + 1);
                        array.CopyTo(newArray, 0);
                        newArray.SetValue(newElement, array.Length);
                        array = newArray;
                        setObject(array);

                        RedrawAgain();
                    });
                    for (int a = 0; a < array.Length; a++)
                    {
                        var index = a;

                        var element = array.GetValue(index);
                        if (ReferenceEquals(element, null))
                        {
                            try
                            {
                                element = System.Activator.CreateInstance(elementType); 
                                array.SetValue(element, index);
                            }
                            catch(Exception ex)
                            {
#if UNITY_EDITOR
                                swole.LogError(ex);
#endif
                                continue;
                            }
                        }

                        string elementName = $"Element {a}";
                        string baseElementName = elementName;
                        try
                        {
                            var nameField = elementType.GetField("id");
                            if (nameField == null) nameField = elementType.GetField("name");
                            if (nameField == null) nameField = elementType.GetField("binding");
                            if (nameField == null) nameField = elementType.GetField("settings");
                            if (nameField != null)
                            {
                                string tempName = string.Empty;
                                if (typeof(string).IsAssignableFrom(nameField.FieldType))
                                {
                                    tempName = nameField.GetValue(element) as string;
                                }
                                else
                                {
                                    var strField = nameField.FieldType.GetField("id");
                                    if (strField == null) strField = nameField.FieldType.GetField("name");
                                    if (strField != null && typeof(string).IsAssignableFrom(strField.FieldType)) tempName = strField.GetValue(nameField.GetValue(element)) as string;
                                }

                                if (!string.IsNullOrWhiteSpace(tempName)) elementName = $"{tempName}[{a}]";
                            }
                        }
                        catch { }

                        var field_ = new CustomFieldInfo(elementName, baseElementName, elementType, (object instance, object val) => (instance as Array).SetValue(val, index), (object instance) => (instance as Array).GetValue(index));
                        AddField(_fieldInfoChain, 1, a, null, false, string.Empty, string.Empty, field_, element, setObject, getObject, (object val) =>
                        {
                            var obj = getObject();
                            field_.SetValue(obj, val);
                            setObject(obj);
                            saveObject?.Invoke();
                        }, () => field_.GetValue(getObject())); 
                    }
                }
                else if (editedObject is IList list)
                {
                    var elementType = list.GetType().GetGenericArguments()[0];
                    CustomEditorUtils.SetButtonOnClickActionByName(rootTransform, button_AddMainElement, () =>
                    {
                        object newElement = null;
                        try
                        {
                            if (typeof(string).IsAssignableFrom(elementType)) newElement = string.Empty; else newElement = System.Activator.CreateInstance(elementType);
                        }
                        catch (Exception ex)
                        {
#if UNITY_EDITOR
                            swole.LogError(ex);
#endif
                        }
                        list.Add(newElement);
                        setObject(list);

                        RedrawAgain();
                    });
                    for (int a = 0; a < list.Count; a++)
                    {
                        var index = a;

                        var element = list[index];
                        if (ReferenceEquals(element, null))
                        {
                            try
                            {
                                element = System.Activator.CreateInstance(elementType); 
                                list[index] = element;
                            }
                            catch (Exception ex)
                            {
#if UNITY_EDITOR
                                swole.LogError(ex);
#endif
                                continue;
                            }
                        }

                        string elementName = $"Element {a}";
                        string baseElementName = elementName;
                        try
                        {
                            var nameField = elementType.GetField("id");
                            if (nameField == null) nameField = elementType.GetField("name");
                            if (nameField == null) nameField = elementType.GetField("binding");
                            if (nameField == null) nameField = elementType.GetField("settings");
                            if (nameField != null)
                            {
                                string tempName = string.Empty;
                                if (typeof(string).IsAssignableFrom(nameField.FieldType))
                                {
                                    tempName = nameField.GetValue(element) as string;
                                }
                                else
                                {
                                    var strField = nameField.FieldType.GetField("id");
                                    if (strField == null) strField = nameField.FieldType.GetField("name");
                                    if (strField != null && typeof(string).IsAssignableFrom(strField.FieldType)) tempName = strField.GetValue(nameField.GetValue(element)) as string;
                                }

                                if (!string.IsNullOrWhiteSpace(tempName)) elementName = $"{tempName}[{a}]"; 
                            }
                        }
                        catch { }

                        var field_ = new CustomFieldInfo(elementName, baseElementName, elementType, (object instance, object val) => (instance as IList)[index] = val, (object instance) => (instance as IList)[index]);
                        AddField(_fieldInfoChain, 1, a, null, false, string.Empty, string.Empty, field_, element, setObject, getObject, (object val) =>
                        {
                            var obj = getObject();
                            field_.SetValue(obj, val);
                            setObject(obj);
                            saveObject?.Invoke();
                        }, () => field_.GetValue(getObject()));
                    }
                }
                else
                { 
                    int i = 0;
                    foreach (var field in fields) 
                    {
                        var field_ = new CustomFieldInfo(field);
                        AddField(_fieldInfoChain, 1, i, null, false, string.Empty, string.Empty, field_, field_.GetValue(editedObject), setObject, getObject, (object val) => 
                        {
                            var obj = getObject();
                            field_.SetValue(obj, val); 
                            setObject(obj);
                            saveObject?.Invoke();
                        }, () => field_.GetValue(getObject())); 
                        i++;
                    }
                }

                /*foreach(var subEditor in subEditors)
                {
                    if (subEditor == null) continue;
                    subEditor.Redraw(protoBoolInput, protoFloatInput, protoIntInput, protoStringInput, protoEnumDropdown, protoFloatSlider, protoIntSlider, protoArray, protoArrayElement, validFields, parentNameStartDepth, fieldValueConverters, objectWithContextConverters);
                }*/

                try
                {
                    OnRedraw?.Invoke();
                }
                catch (Exception ex)
                {
                    swole.LogError(ex);
                }

                return true;
            }
        }

        protected EditorInstance mainEditor;

        protected virtual void Awake()
        {
            if (listPool == null) listPool = gameObject.AddComponent<PrefabPool>();
            if (mainListWindow == null) mainListWindow = gameObject.AddOrGetComponent<RectTransform>();

            listPool.Reinitialize(listPrototype, PoolGrowthMethod.Incremental, 1, 1, 50);
            listPool.worldPositionStays = false;// subListContainer == null;
            listPool.SetContainerTransform(subListContainer, false, false, false);  
        }

        public void StartEditingObject(object obj, SetValueDelegate setValue, GetValueDelegate getValue, VoidParameterlessDelegate saveObject, ICollection<string> validFields = null, AddFieldConvertersDelegate addFieldConverters = null, IdsToTypesDelegate addEnumIdToTypes = null)
        {
            if (mainEditor != null) mainEditor.CompleteEdit();
            mainEditor = null;

            if (ReferenceEquals(obj, null)) return;

            fieldValueConverters.Clear();
            objectWithContextConverters.Clear();
            addFieldConverters?.Invoke(fieldValueConverters, objectWithContextConverters);

            enumIdsToTypes.Clear();
            addEnumIdToTypes?.Invoke(enumIdsToTypes); 
            
            mainEditor = new EditorInstance(obj, setValue, getValue, saveObject, listPool, null, mainListWindow, mainListContainer, subListWindow, subListContainer);
            mainEditor.indentWidthStart = indentWidthStart;
            mainEditor.indentWidthIncrement = indentWidthIncrement;
            if (OnRedraw != null) mainEditor.OnRedraw += OnRedraw.Invoke; 
            mainEditor.Redraw(protoBoolInput, protoFloatInput, protoIntInput, protoStringInput, protoEnumDropdown, protoFloatSlider, protoIntSlider, protoArray, protoArrayElement, validFields, 3, fieldValueConverters, objectWithContextConverters, enumIdsToTypes);
        }
    }

    public class PropertyTypeConditionalConversion
    {
        public int depth;
        public string fieldName;
        public object[] validValues;

        public PropertyTypeConditionalConversion(int depth, string fieldName, object[] validValues)
        {
            this.depth = depth;
            this.fieldName = fieldName;
            this.validValues = validValues;
        }

        public bool Check(object value)
        {
            if (!ReferenceEquals(validValues, null))
            {
                foreach (var validValue in validValues) if (validValue.Equals(value)) return true;// else Debug.Log(validValue.ToString() + " != " + value.ToString());
            }
             
            return false;
        }
        public bool CheckInstance(object instance)
        {
            if (ReferenceEquals(instance, null) || string.IsNullOrWhiteSpace(fieldName)) return false; 

            var type = instance.GetType();
            var field = type.GetField(fieldName);
            if (field == null) return false;

            return Check(field.GetValue(instance)); 
        }
    }
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class PropertyTypeConverterAttribute : Attribute
    {
        private string converterId;
        public string ConverterID => converterId;

        private int depth;
        public int Depth => depth;

        private PropertyTypeConditionalConversion condition;
        public PropertyTypeConditionalConversion Condition => condition;
        public bool HasCondition => !ReferenceEquals(condition, null);

        public PropertyTypeConverterAttribute(string converterId, int depth = 0, int conditionDepth = 0, string conditionFieldName = null, params object[] conditionValidValues)
        {
            this.converterId = converterId;
            this.depth = depth;
            this.condition = (string.IsNullOrWhiteSpace(conditionFieldName) || ReferenceEquals(conditionValidValues, null)) ? null : new PropertyTypeConditionalConversion(conditionDepth, conditionFieldName, conditionValidValues);
        }

        public bool Check(object value)
        {
            if (!HasCondition) return true;
            return Condition.Check(value);
        }
        public bool CheckInstance(object instance)
        {
            if (!HasCondition) return true;
            return Condition.CheckInstance(instance); 
        }
    }
    [AttributeUsage(AttributeTargets.Field)]
    public class NonEditableAttribute : Attribute
    {
        public NonEditableAttribute()
        {
        }
    }
    [AttributeUsage(AttributeTargets.Field)]
    public class RedrawOnEditAttribute : Attribute
    {
        public RedrawOnEditAttribute()
        {
        }
    }
    [AttributeUsage(AttributeTargets.Field)]
    public class EditableAliasAttribute : Attribute
    {
        private string alias;
        public string Alias => alias;

        public EditableAliasAttribute(string alias)
        {
            this.alias = alias;
        }
    }
    [AttributeUsage(AttributeTargets.Field)]
    public class UseEditablePrefixAttribute : Attribute
    {
        private bool flag;
        public bool Flag => flag;

        public UseEditablePrefixAttribute(bool flag)
        {
            this.flag = flag;
        }
    }
    [AttributeUsage(AttributeTargets.Field)]
    public class EditAsEnumAttribute : Attribute
    {
        private Type enumType;
        public Type EnumType => enumType;

        private string enumId;
        public string EnumId => enumId;

        public EditAsEnumAttribute(Type enumType)
        {
            this.enumType = enumType;
            this.enumId = null;
        }

        public EditAsEnumAttribute(string enumId)
        {
            this.enumType = null;
            this.enumId = enumId;
        }
    }

}

#endif