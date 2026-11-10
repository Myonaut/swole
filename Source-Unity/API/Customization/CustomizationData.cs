#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using Unity.Mathematics;
using System.Reflection;

namespace Swole
{
    public interface ICustomizableProperty
    {
        public string ID { get; }
        public object ConvertedValue { get; }
        public object GetConvertedValue(Type targetType = null);
        public void ReapplyConvertedValue(object target, object value);
        public void ReapplyConvertedValue(string id, object target, object value);

        public bool IsMultiValue { get; }
        public List<(string, object)> GetConvertedValues(List<(string, object)> list = null);
        public void ReapplyConvertedValues(object target, IEnumerable<(string, object)> convertedValues);
    }
    public interface IUserCustomizable
    {
        public CustomizationState GenerateCustomizationState();
        public CustomizationState GenerateKeyValueState();
        public AdvancedCustomizationState GenerateAdvancedCustomizationState();

        public void SetCustomizationState(CustomizationState state);
        public void SetCustomizationKeyValueState(CustomizationState state);
        public void SetAdvancedCustomizationState(AdvancedCustomizationState state);
    }

    [Serializable]
    public struct CustomizableBool : ICustomizableProperty
    {
        public string id;

        public bool value;

        public string ID => id;
        public object ConvertedValue => value;
        public object GetConvertedValue(Type targetType = null) => ConvertedValue;
        public void ReapplyConvertedValue(object target, object value)
        {
            if (target is ICustomizableProperty) return;
            CustomizationData.SetProperty(ID, value, target);
        }
        public void ReapplyConvertedValue(string id, object target, object value)
        {
            if (target is ICustomizableProperty) return;
            CustomizationData.SetProperty(id, value, target);
        }

        public bool IsMultiValue => false;
        public List<(string, object)> GetConvertedValues(List<(string, object)> list = null)
        {
            if (list == null) list = new List<(string, object)>();
            list.Add((ID, ConvertedValue));
            return list;
        }
        public void ReapplyConvertedValues(object target, IEnumerable<(string, object)> convertedValues)
        {
            if (target is ICustomizableProperty) return;
            CustomizationData.ReapplyConvertedValues(target, convertedValues);
        }

        public static implicit operator bool(CustomizableBool prop) => prop.value;

        public static CustomizableBool GetProperty(object obj, string id)
        {
            if (ReferenceEquals(obj, null)) return default;

            CustomizableBool output = default;
            try
            {
                var type = obj.GetType();
                var field = type.GetField(id, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field == null)
                {
                    var prop = type.GetProperty(id, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance); 
                    if (prop != null)
                    {
                        output.value = (bool)prop.GetValue(obj);
                    }
                }
                else
                {
                    output.value = (bool)field.GetValue(obj);
                }
            }
            catch (Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogException(ex);
#endif
            }

            return output;
        }
    }
    [Serializable]
    public struct CustomizableFloat : ICustomizableProperty
    {
        public string id;
        
        public float value;

        public string ID => id;
        public object ConvertedValue => value;
        public object GetConvertedValue(Type targetType = null) => ConvertedValue;
        public void ReapplyConvertedValue(object target, object value)
        {
            if (target is ICustomizableProperty) return;
            CustomizationData.SetProperty(ID, value, target);
        }
        public void ReapplyConvertedValue(string id, object target, object value)
        {
            if (target is ICustomizableProperty) return;
            CustomizationData.SetProperty(id, value, target);
        }

        public bool IsMultiValue => false;
        public List<(string, object)> GetConvertedValues(List<(string, object)> list = null)
        {
            if (list == null) list = new List<(string, object)>();
            list.Add((ID, ConvertedValue));
            return list;
        }
        public void ReapplyConvertedValues(object target, IEnumerable<(string, object)> convertedValues)
        {
            if (target is ICustomizableProperty) return;
            CustomizationData.ReapplyConvertedValues(target, convertedValues);
        }

        public static implicit operator float(CustomizableFloat prop) => prop.value;

        public static CustomizableFloat GetProperty(object obj, string id)
        {
            if (ReferenceEquals(obj, null)) return default;

            CustomizableFloat output = default;
            try
            {
                var type = obj.GetType();
                var field = type.GetField(id, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field == null)
                {
                    var prop = type.GetProperty(id, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (prop != null)
                    {
                        output.value = (float)prop.GetValue(obj);
                    }
                }
                else
                {
                    output.value = (float)field.GetValue(obj);
                }
            }
            catch (Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogException(ex);
#endif
            }

            return output;
        }
    }
    [Serializable]
    public struct CustomizableInt : ICustomizableProperty
    {
        public string id;
        public int value;

        public string ID => id;
        public object ConvertedValue => GetConvertedValue();
        public object GetConvertedValue(Type targetType = null)
        {
            if (targetType != null && targetType.IsEnum)
            {
                return Enum.ToObject(targetType, value);
            }

            return value;
        }
        public void ReapplyConvertedValue(object target, object value)
        {
            if (target is ICustomizableProperty) return;
            CustomizationData.SetProperty(ID, value, target);
        }
        public void ReapplyConvertedValue(string id, object target, object value)
        {
            if (target is ICustomizableProperty) return;
            CustomizationData.SetProperty(id, value, target);
        }

        public bool IsMultiValue => false;
        public List<(string, object)> GetConvertedValues(List<(string, object)> list = null)
        {
            if (list == null) list = new List<(string, object)>();
            list.Add((ID, ConvertedValue));
            return list;
        }
        public void ReapplyConvertedValues(object target, IEnumerable<(string, object)> convertedValues)
        {
            if (target is ICustomizableProperty) return;
            CustomizationData.ReapplyConvertedValues(target, convertedValues);
        }

        public static implicit operator int(CustomizableInt prop) => prop.value;

        public static CustomizableInt GetProperty(object obj, string id)
        {
            if (ReferenceEquals(obj, null)) return default;

            CustomizableInt output = default;
            try
            {
                var type = obj.GetType();
                var field = type.GetField(id, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field == null)
                {
                    var prop = type.GetProperty(id, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (prop != null)
                    {
                        output.value = (int)prop.GetValue(obj);
                    }
                }
                else
                {
                    output.value = (int)field.GetValue(obj);
                }
            }
            catch (Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogException(ex);
#endif
            }

            return output;
        }
    }
    [Serializable]
    public struct CustomizableString : ICustomizableProperty
    {
        public string id;
        public string value;

        public string ID => id;
        public object ConvertedValue => value;
        public object GetConvertedValue(Type targetType = null) => ConvertedValue;
        public void ReapplyConvertedValue(object target, object value)
        {
            if (target is ICustomizableProperty) return;
            CustomizationData.SetProperty(ID, value, target);
        }
        public void ReapplyConvertedValue(string id, object target, object value)
        {
            if (target is ICustomizableProperty) return;
            CustomizationData.SetProperty(id, value, target);
        }

        public bool IsMultiValue => false;
        public List<(string, object)> GetConvertedValues(List<(string, object)> list = null)
        {
            if (list == null) list = new List<(string, object)>();
            list.Add((ID, ConvertedValue));
            return list;
        }
        public void ReapplyConvertedValues(object target, IEnumerable<(string, object)> convertedValues)
        {
            if (target is ICustomizableProperty) return;
            CustomizationData.ReapplyConvertedValues(target, convertedValues);
        }

        public static implicit operator string(CustomizableString prop) => prop.value;

        public static CustomizableString GetProperty(object obj, string id)
        {
            if (ReferenceEquals(obj, null)) return default;

            CustomizableString output = default;
            try
            {
                var type = obj.GetType();
                var field = type.GetField(id, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field == null)
                {
                    var prop = type.GetProperty(id, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (prop != null)
                    {
                        output.value = (string)prop.GetValue(obj);
                    }
                }
                else
                {
                    output.value = (string)field.GetValue(obj);
                }
            }
            catch (Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogException(ex);
#endif
            }

            return output;
        }
    }
    [Serializable]
    public struct CustomizableVector2 : ICustomizableProperty
    {
        public string id;
        public float2 value;

        public string ID => id;
        public object ConvertedValue => Value;
        public object GetConvertedValue(Type targetType = null) => ConvertedValue;
        public void ReapplyConvertedValue(object target, object value)
        {
            if (target is ICustomizableProperty) return;
            CustomizationData.SetProperty(ID, value, target);
        }
        public void ReapplyConvertedValue(string id, object target, object value)
        {
            if (target is ICustomizableProperty) return;
            CustomizationData.SetProperty(id, value, target);
        }

        public bool IsMultiValue => false;
        public List<(string, object)> GetConvertedValues(List<(string, object)> list = null)
        {
            if (list == null) list = new List<(string, object)>();
            list.Add((ID, ConvertedValue));
            return list;
        }
        public void ReapplyConvertedValues(object target, IEnumerable<(string, object)> convertedValues)
        {
            if (target is ICustomizableProperty) return;
            CustomizationData.ReapplyConvertedValues(target, convertedValues);
        }

        public Vector2 Value => new Vector2(value.x, value.y);
        public static implicit operator Vector2(CustomizableVector2 prop) => prop.Value;

        public static CustomizableVector2 GetProperty(object obj, string id)
        {
            if (ReferenceEquals(obj, null)) return default;

            CustomizableVector2 output = default;
            try
            {
                var type = obj.GetType();
                var field = type.GetField(id, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field == null)
                {
                    var prop = type.GetProperty(id, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (prop != null)
                    {
                        output.value = (Vector2)prop.GetValue(obj);
                    }
                }
                else
                {
                    output.value = (Vector2)field.GetValue(obj);
                }
            }
            catch (Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogException(ex);
#endif
            }

            return output;
        }
    }
    [Serializable]
    public struct CustomizableVector3 : ICustomizableProperty
    {
        public string id;
        public float3 value;

        public string ID => id;
        public object ConvertedValue => Value;
        public object GetConvertedValue(Type targetType = null) => ConvertedValue;
        public void ReapplyConvertedValue(object target, object value)
        {
            if (target is ICustomizableProperty) return;
            CustomizationData.SetProperty(ID, value, target);
        }
        public void ReapplyConvertedValue(string id, object target, object value)
        {
            if (target is ICustomizableProperty) return;
            CustomizationData.SetProperty(id, value, target);
        }

        public bool IsMultiValue => false;
        public List<(string, object)> GetConvertedValues(List<(string, object)> list = null)
        {
            if (list == null) list = new List<(string, object)>();
            list.Add((ID, ConvertedValue));
            return list;
        }
        public void ReapplyConvertedValues(object target, IEnumerable<(string, object)> convertedValues)
        {
            if (target is ICustomizableProperty) return;
            CustomizationData.ReapplyConvertedValues(target, convertedValues);
        }

        public Vector3 Value => new Vector3(value.x, value.y, value.z);
        public static implicit operator Vector3(CustomizableVector3 prop) => prop.Value;

        public static CustomizableVector3 GetProperty(object obj, string id)
        {
            if (ReferenceEquals(obj, null)) return default;

            CustomizableVector3 output = default;
            try
            {
                var type = obj.GetType();
                var field = type.GetField(id, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field == null)
                {
                    var prop = type.GetProperty(id, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (prop != null)
                    {
                        output.value = (Vector3)prop.GetValue(obj);
                    }
                }
                else
                {
                    output.value = (Vector3)field.GetValue(obj);
                }
            }
            catch (Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogException(ex);
#endif
            }

            return output;
        }
    }
    [Serializable]
    public struct CustomizableVector4 : ICustomizableProperty
    {
        public string id;
        public float4 value;

        public string ID => id;
        public object ConvertedValue => Value;
        public object GetConvertedValue(Type targetType = null) => ConvertedValue;
        public void ReapplyConvertedValue(object target, object value)
        {
            if (target is ICustomizableProperty) return;
            CustomizationData.SetProperty(ID, value, target);
        }
        public void ReapplyConvertedValue(string id, object target, object value)
        {
            if (target is ICustomizableProperty) return;
            CustomizationData.SetProperty(id, value, target);
        }

        public bool IsMultiValue => false;
        public List<(string, object)> GetConvertedValues(List<(string, object)> list = null)
        {
            if (list == null) list = new List<(string, object)>();
            list.Add((ID, ConvertedValue));
            return list;
        }
        public void ReapplyConvertedValues(object target, IEnumerable<(string, object)> convertedValues)
        {
            if (target is ICustomizableProperty) return;
            CustomizationData.ReapplyConvertedValues(target, convertedValues);
        }

        public Vector4 Value => new Vector4(value.x, value.y, value.z, value.w);
        public static implicit operator Vector4(CustomizableVector4 prop) => prop.Value;

        public static CustomizableVector4 GetProperty(object obj, string id)
        {
            if (ReferenceEquals(obj, null)) return default;

            CustomizableVector4 output = default;
            try
            {
                var type = obj.GetType();
                var field = type.GetField(id, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field == null)
                {
                    var prop = type.GetProperty(id, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (prop != null)
                    {
                        output.value = (Vector4)prop.GetValue(obj);
                    }
                }
                else
                {
                    output.value = (Vector4)field.GetValue(obj);
                }
            }
            catch (Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogException(ex);
#endif
            }

            return output;
        }
    }
    [Serializable]
    public struct CustomizableColor : ICustomizableProperty
    {
        public string id;
        public float4 value;

        public string ID => id;
        public object ConvertedValue => Value;
        public object GetConvertedValue(Type targetType = null) => ConvertedValue;
        public void ReapplyConvertedValue(object target, object value)
        {
            if (target is ICustomizableProperty) return;
            CustomizationData.SetProperty(ID, value, target);
        }
        public void ReapplyConvertedValue(string id, object target, object value)
        {
            if (target is ICustomizableProperty) return;
            CustomizationData.SetProperty(id, value, target);
        }

        public bool IsMultiValue => false;
        public List<(string, object)> GetConvertedValues(List<(string, object)> list = null)
        {
            if (list == null) list = new List<(string, object)>();
            list.Add((ID, ConvertedValue));
            return list;
        }
        public void ReapplyConvertedValues(object target, IEnumerable<(string, object)> convertedValues)
        {
            if (target is ICustomizableProperty) return;
            CustomizationData.ReapplyConvertedValues(target, convertedValues);
        }

        public Color Value => new Color(value.x, value.y, value.z, value.w);
        public static implicit operator Color(CustomizableColor prop) => prop.Value;

        public static CustomizableColor GetProperty(object obj, string id)
        {
            if (ReferenceEquals(obj, null)) return default;

            CustomizableColor output = default;
            try
            {
                var type = obj.GetType();
                var field = type.GetField(id, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field == null)
                {
                    var prop = type.GetProperty(id, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (prop != null)
                    {
                        var color = (Color)prop.GetValue(obj);
                        output.value = new float4(color.r, color.g, color.b, color.a);
                    }
                }
                else
                {
                    var color = (Color)field.GetValue(obj);
                    output.value = new float4(color.r, color.g, color.b, color.a);
                }
            }
            catch (Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogException(ex);
#endif
            }

            return output;
        }
    }

    [Serializable]
    public struct CustomizationState
    {
        public CustomizableBool[] boolProps;
        public CustomizableFloat[] floatProps;
        public CustomizableInt[] intProps;
        public CustomizableString[] stringProps;
        public CustomizableColor[] colorProps;
        public CustomizableVector2[] vector2Props;
        public CustomizableVector3[] vector3Props;
        public CustomizableVector4[] vector4Props;

        public string GetDebugPrint()
        {
            string print = string.Empty; 

            if (boolProps != null)
            {
                foreach (var prop in boolProps)
                {
                    print += $"[Bool] {prop.ID} = {prop.value}\n";
                }
            }

            if (floatProps != null)
            {
                foreach (var prop in floatProps)
                {
                    print += $"[Float] {prop.ID} = {prop.value}\n";
                }
            }

            if (intProps != null)
            {
                foreach (var prop in intProps)
                {
                    print += $"[Int] {prop.ID} = {prop.value}\n";
                }
            }

            if (stringProps != null)
            {
                foreach (var prop in stringProps)
                {
                    print += $"[String] {prop.ID} = {prop.value}\n";
                }
            }

            if (colorProps != null)
            {
                foreach (var prop in colorProps)
                {
                    print += $"[Color] {prop.ID} = {prop.Value}\n";
                }
            }

            if (vector2Props != null)
            {
                foreach (var prop in vector2Props)
                {
                    print += $"[Vector2] {prop.ID} = {prop.Value}\n";
                }
            }

            if (vector3Props != null)
            {
                foreach (var prop in vector3Props)
                {
                    print += $"[Vector3] {prop.ID} = {prop.Value}\n";
                }
            }

            if (vector4Props != null)
            {
                foreach (var prop in vector4Props)
                {
                    print += $"[Vector4] {prop.ID} = {prop.Value}\n";
                }
            }

            return print;
        }

        public object Apply(object obj)
        {
            if (ReferenceEquals(obj, null)) return obj;

            obj = CustomizationData.ApplyCustomizablePropertyArray(boolProps, obj);
            obj = CustomizationData.ApplyCustomizablePropertyArray(floatProps, obj);
            obj = CustomizationData.ApplyCustomizablePropertyArray(intProps, obj);
            obj = CustomizationData.ApplyCustomizablePropertyArray(stringProps, obj);
            obj = CustomizationData.ApplyCustomizablePropertyArray(colorProps, obj); 
            obj = CustomizationData.ApplyCustomizablePropertyArray(vector2Props, obj);
            obj = CustomizationData.ApplyCustomizablePropertyArray(vector3Props, obj);
            obj = CustomizationData.ApplyCustomizablePropertyArray(vector4Props, obj); 

            return obj;
        }

        private static readonly List<ICustomizableProperty> _customizableProperties = new List<ICustomizableProperty>();
        private static readonly List<(string, object)> _convertedValues = new List<(string, object)>();
        public static CustomizationState GetState(object obj)
        {
            if (ReferenceEquals(obj, null)) return default;

            var type = obj.GetType();

            var state = new CustomizationState();

            _customizableProperties.Clear();
            void AddProperty(Type valueType, string id, object value)
            {
                if (typeof(ICustomizableProperty).IsAssignableFrom(valueType))
                {
                    var prop = (ICustomizableProperty)value;
                    if (prop.IsMultiValue)
                    {
                        _convertedValues.Clear();
                        prop.GetConvertedValues(_convertedValues);
                        foreach(var (key, val) in _convertedValues)
                        {
                            AddProperty(val.GetType(), CustomizationData.InnerIdAsMultiValuePropertyId(id, key), val); 
                        }
                        _convertedValues.Clear();
                    } 
                    else
                    {
                        value = prop.GetConvertedValue();
                        AddProperty(value.GetType(), id, value);
                    }

                    return;
                }

                if (typeof(bool).IsAssignableFrom(valueType))
                {
                    _customizableProperties.Add(new CustomizableBool()
                    {
                        id = id,
                        value = (bool)value
                    });
                }
                else if (typeof(float).IsAssignableFrom(valueType))
                {
                    _customizableProperties.Add(new CustomizableFloat()
                    {
                        id = id,
                        value = (float)value
                    });
                }
                else if (typeof(int).IsAssignableFrom(valueType) || valueType.IsEnum)
                {
                    _customizableProperties.Add(new CustomizableInt()
                    {
                        id = id,
                        value = (int)value
                    });
                }
                else if (typeof(string).IsAssignableFrom(valueType))
                {
                    _customizableProperties.Add(new CustomizableString()
                    {
                        id = id,
                        value = (string)value
                    });
                }
                else if (typeof(Color).IsAssignableFrom(valueType))
                {
                    var value_ = (Color)value;
                    _customizableProperties.Add(new CustomizableColor()
                    {
                        id = id,
                        value = new float4(value_.r, value_.g, value_.b, value_.a) 
                    });
                }
                else if (typeof(Vector2).IsAssignableFrom(valueType) || typeof(float2).IsAssignableFrom(valueType))
                {
                    _customizableProperties.Add(new CustomizableVector2()
                    {
                        id = id,
                        value = value is Vector2 v ? (float2)v : (float2)value
                    });
                }
                else if (typeof(Vector3).IsAssignableFrom(valueType) || typeof(float3).IsAssignableFrom(valueType))
                {
                    _customizableProperties.Add(new CustomizableVector3()
                    {
                        id = id,
                        value = value is Vector3 v ? (float3)v : (float3)value
                    });
                }
                else if (typeof(Vector4).IsAssignableFrom(valueType) || typeof(float4).IsAssignableFrom(valueType))
                {
                    _customizableProperties.Add(new CustomizableVector4()
                    {
                        id = id,
                        value = value is Vector4 v ? (float4)v : (float4)value
                    });
                }
                else if (typeof(Array).IsAssignableFrom(valueType))
                {
                    var array = (Array)value;
                    for (int a = 0; a < array.Length; a++)
                    {
                        var element = array.GetValue(a);
                        if (element != null)
                        {
                            AddProperty(element.GetType(), CustomizationData.AsArrayPropertyId(id, a), element); 
                        }
                    }
                }
                else if (typeof(IList).IsAssignableFrom(valueType))
                {
                    var array = (IList)value;
                    for (int a = 0; a < array.Count; a++)
                    {
                        var element = array[a];
                        if (element != null)
                        {
                            AddProperty(element.GetType(), CustomizationData.AsListPropertyId(id, a), element);
                        }
                    }
                }
            }

            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(field => field.IsDefined(typeof(UserCustomizableAttribute), false));
            foreach(var field in fields)
            {
                AddProperty(field.FieldType, field.Name, field.GetValue(obj));
            }

            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(prop => prop.IsDefined(typeof(UserCustomizableAttribute), false));  
            foreach (var property in properties)
            {
                AddProperty(property.PropertyType, property.Name, property.GetValue(obj));
            }

            state.boolProps = _customizableProperties.Where(prop => prop is CustomizableBool).OfType<CustomizableBool>().ToArray();
            state.floatProps = _customizableProperties.Where(prop => prop is CustomizableFloat).OfType<CustomizableFloat>().ToArray();
            state.intProps = _customizableProperties.Where(prop => prop is CustomizableInt).OfType<CustomizableInt>().ToArray();
            state.stringProps = _customizableProperties.Where(prop => prop is CustomizableString).OfType<CustomizableString>().ToArray();
            state.colorProps = _customizableProperties.Where(prop => prop is CustomizableColor).OfType<CustomizableColor>().ToArray();
            state.vector2Props = _customizableProperties.Where(prop => prop is CustomizableVector2).OfType<CustomizableVector2>().ToArray();
            state.vector3Props = _customizableProperties.Where(prop => prop is CustomizableVector3).OfType<CustomizableVector3>().ToArray();
            state.vector4Props = _customizableProperties.Where(prop => prop is CustomizableVector4).OfType<CustomizableVector4>().ToArray();

            _customizableProperties.Clear();

            return state;
        }
    }
    [Serializable]
    public struct AdvancedCustomizationState
    {
        public CustomizationState baseState;
        public CustomizationState keyValueState; 

        public void Apply(object obj)
        {
            if (obj is IUserCustomizable userObj)
            {
                userObj.SetCustomizationState(baseState); 
                userObj.SetCustomizationKeyValueState(keyValueState);
            } 
            else
            {
                baseState.Apply(obj);
            }
        }
    }

    [System.AttributeUsage(System.AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class UserCustomizableAttribute : Attribute
    {
    }

    public static class CustomizationData
    {

        public const string _multiValuePrefix = "$.";

        public static string AsMultiValuePropertyId(string id, string subId) => _multiValuePrefix + id + "." + subId;
        public static string InnerIdAsMultiValuePropertyId(string id, string subId)
        {
            var multiValuePrefixIndex = subId.IndexOf(_multiValuePrefix);
            if (multiValuePrefixIndex == 0)
            {
                int nextDotIndex = subId.IndexOf('.', _multiValuePrefix.Length);
                string prefix = subId.Substring(0, nextDotIndex + 1);

                return prefix + AsMultiValuePropertyId(id, subId.Substring(nextDotIndex + 1)); 
            }

            var arrayPrefixIndex = subId.IndexOf(_arrayPrefix);
            if (arrayPrefixIndex == 0)
            {
                return _arrayPrefix + AsMultiValuePropertyId(id, subId.Substring(arrayPrefixIndex + _arrayPrefix.Length));
            }

            var listPrefixIndex = subId.IndexOf(_listPrefix);
            if (listPrefixIndex == 0)
            {
                return _listPrefix + AsMultiValuePropertyId(id, subId.Substring(listPrefixIndex + _listPrefix.Length));
            }

            return AsMultiValuePropertyId(id, subId);
        }

        public const string _arrayPrefix = "array.";
        public const string _listPrefix = "list.";

        public static string AsArrayPropertyId(string id, int elementIndex) => _arrayPrefix + id + "[" + elementIndex + "]";
        public static string AsListPropertyId(string id, int elementIndex) => _listPrefix + id + "[" + elementIndex + "]";

        public delegate object ValueGetterDelegate(System.Type targetType);
        public static void SetProperty(this ICustomizableProperty cProp, object obj) => SetProperty(cProp.ID, cProp.GetConvertedValue, obj);
        public static void SetProperty(string id, object value, object obj) => SetProperty(id, _ => value, obj);
        public static void SetProperty(string id, ValueGetterDelegate valueGetter, object obj)
        {
            if (ReferenceEquals(obj, null)) return;

            try
            {
                FieldInfo field;

                var multiValuePrefixIndex = id.IndexOf(_multiValuePrefix);
                string subId = id;
                if (multiValuePrefixIndex == 0)
                {
                    int nextDotIndex = id.IndexOf('.', _multiValuePrefix.Length);
                    subId = id.Substring(nextDotIndex + 1);
                    id = id.Substring(_multiValuePrefix.Length, nextDotIndex - _multiValuePrefix.Length);

                    field = obj.GetType().GetField(id, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (field == null)
                    {
                        var prop = obj.GetType().GetProperty(id, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (prop != null)
                        {
                            SetProperty(subId, valueGetter, prop.GetValue(obj)); 
                        }
                    }
                    else
                    {
                        SetProperty(subId, valueGetter, field.GetValue(obj));
                    }
                    return;
                }
                else if (obj is ICustomizableProperty cProp)
                {
                    cProp.ReapplyConvertedValue(id, obj, valueGetter);
                    return;
                }

                var arrayPrefixIndex = id.IndexOf(_arrayPrefix);
                var listPrefixIndex = id.IndexOf(_listPrefix);

                bool isArray = arrayPrefixIndex == 0;
                bool isList = listPrefixIndex == 0;

                bool isCollection = isArray || isList;
                int elementIndex = -1;
                if (isCollection)
                {
                    id = isArray ? id.Substring(arrayPrefixIndex + _arrayPrefix.Length) : id.Substring(listPrefixIndex + _listPrefix.Length);

                    int elementStartIndex = id.IndexOf('[');
                    if (elementStartIndex >= 0)
                    {
                        int elementEndIndex = id.IndexOf(']', elementStartIndex);
                        if (elementEndIndex >= 0)
                        {
                            try
                            {
                                elementIndex = int.Parse(id.Substring(elementStartIndex + 1, elementEndIndex - elementStartIndex - 1));
                            }
                            catch (Exception e)
                            {
#if UNITY_EDITOR
                                Debug.LogError(e);
#endif
                                elementIndex = -1;
                            }
                        }

                        id = id.Substring(0, elementStartIndex);
                    }

                    if (elementIndex < 0) return;
                }

                Array array = null;
                IList list = null;

                field = obj.GetType().GetField(id, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field == null)
                {
                    var prop = obj.GetType().GetProperty(id, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (prop != null)
                    {
                        if (!isCollection) prop.SetValue(obj, valueGetter(prop.PropertyType));
                        else if (isArray) array = (Array)prop.GetValue(obj);
                        else if (isList) list = (IList)prop.GetValue(obj);
                    }
                }
                else
                {
                    if (!isCollection) field.SetValue(obj, valueGetter(field.FieldType));
                    else if (isArray) array = (Array)field.GetValue(obj);
                    else if (isList) list = (IList)field.GetValue(obj);
                }

                if (isArray) array.SetValue(valueGetter(field.FieldType), elementIndex);
                else if (isList) list[elementIndex] = valueGetter(list[elementIndex].GetType());
            }
            catch (Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogException(ex);
#endif
            }
        }

        public static void ReapplyConvertedValues(object target, IEnumerable<(string, object)> convertedValues)
        {
            if (target is ICustomizableProperty cProp)
            {
                cProp.ReapplyConvertedValues(target, convertedValues);
            }
            else if (target != null)
            {
                foreach (var (key, val) in convertedValues)
                {
                    SetProperty(key, val, target);
                }
            }
        }

        public static object ApplyCustomizablePropertyArray(Array props, object obj)
        {
            if (props == null || props.Length <= 0) return obj;

            for(int a = 0; a < props.Length; a++)
            {
                var element = props.GetValue(a);
                if (element is ICustomizableProperty prop) prop.SetProperty(obj);
            }

            return obj;
        }

        public static object ApplyCustomizablePropertyEnumerable(IEnumerable props, object obj)
        {
            if (props == null) return obj;

            foreach (var element in props)
            {
                if (element is ICustomizableProperty prop) prop.SetProperty(obj);
            }

            return obj;
        }
    }
}

#endif