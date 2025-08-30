#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using Unity.Mathematics;

namespace Swole
{
    public interface ICustomizableProperty
    {
        public string ID { get; }
        public object ConvertedValue { get; }
        public object GetConvertedValue(Type targetType = null);
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

        public static implicit operator bool(CustomizableBool prop) => prop.value;

        public static CustomizableBool GetProperty(object obj, string id)
        {
            if (ReferenceEquals(obj, null)) return default;

            CustomizableBool output = default;
            try
            {
                var type = obj.GetType();
                var field = type.GetField(id);
                if (field == null)
                {
                    var prop = type.GetProperty(id);
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

        public static implicit operator float(CustomizableFloat prop) => prop.value;

        public static CustomizableFloat GetProperty(object obj, string id)
        {
            if (ReferenceEquals(obj, null)) return default;

            CustomizableFloat output = default;
            try
            {
                var type = obj.GetType();
                var field = type.GetField(id);
                if (field == null)
                {
                    var prop = type.GetProperty(id);
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

        public static implicit operator int(CustomizableInt prop) => prop.value;

        public static CustomizableInt GetProperty(object obj, string id)
        {
            if (ReferenceEquals(obj, null)) return default;

            CustomizableInt output = default;
            try
            {
                var type = obj.GetType();
                var field = type.GetField(id);
                if (field == null)
                {
                    var prop = type.GetProperty(id);
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

        public static implicit operator string(CustomizableString prop) => prop.value;

        public static CustomizableString GetProperty(object obj, string id)
        {
            if (ReferenceEquals(obj, null)) return default;

            CustomizableString output = default;
            try
            {
                var type = obj.GetType();
                var field = type.GetField(id);
                if (field == null)
                {
                    var prop = type.GetProperty(id);
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

        public Vector2 Value => new Vector2(value.x, value.y);
        public static implicit operator Vector2(CustomizableVector2 prop) => prop.Value;

        public static CustomizableVector2 GetProperty(object obj, string id)
        {
            if (ReferenceEquals(obj, null)) return default;

            CustomizableVector2 output = default;
            try
            {
                var type = obj.GetType();
                var field = type.GetField(id);
                if (field == null)
                {
                    var prop = type.GetProperty(id);
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

        public Vector3 Value => new Vector3(value.x, value.y, value.z);
        public static implicit operator Vector3(CustomizableVector3 prop) => prop.Value;

        public static CustomizableVector3 GetProperty(object obj, string id)
        {
            if (ReferenceEquals(obj, null)) return default;

            CustomizableVector3 output = default;
            try
            {
                var type = obj.GetType();
                var field = type.GetField(id);
                if (field == null)
                {
                    var prop = type.GetProperty(id);
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

        public Vector4 Value => new Vector4(value.x, value.y, value.z, value.w);
        public static implicit operator Vector4(CustomizableVector4 prop) => prop.Value;

        public static CustomizableVector4 GetProperty(object obj, string id)
        {
            if (ReferenceEquals(obj, null)) return default;

            CustomizableVector4 output = default;
            try
            {
                var type = obj.GetType();
                var field = type.GetField(id);
                if (field == null)
                {
                    var prop = type.GetProperty(id);
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

        public Color Value => new Color(value.x, value.y, value.z, value.w);
        public static implicit operator Color(CustomizableColor prop) => prop.Value;

        public static CustomizableColor GetProperty(object obj, string id)
        {
            if (ReferenceEquals(obj, null)) return default;

            CustomizableColor output = default;
            try
            {
                var type = obj.GetType();
                var field = type.GetField(id);
                if (field == null)
                {
                    var prop = type.GetProperty(id);
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
        public static CustomizationState GetState(object obj)
        {
            if (ReferenceEquals(obj, null)) return default;

            var type = obj.GetType();

            var state = new CustomizationState();

            _customizableProperties.Clear();
            void AddProperty(Type valueType, string id, object value)
            {
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
                else if (typeof(Vector2).IsAssignableFrom(valueType) || typeof(float2).IsAssignableFrom(valueType))
                {
                    _customizableProperties.Add(new CustomizableVector2()
                    {
                        id = id,
                        value = (float2)value
                    });
                }
                else if (typeof(Vector3).IsAssignableFrom(valueType) || typeof(float3).IsAssignableFrom(valueType))
                {
                    _customizableProperties.Add(new CustomizableVector3()
                    {
                        id = id,
                        value = (float3)value
                    });
                }
                else if (typeof(Vector4).IsAssignableFrom(valueType) || typeof(float4).IsAssignableFrom(valueType))
                {
                    _customizableProperties.Add(new CustomizableVector4()
                    {
                        id = id,
                        value = (float4)value
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
            }

            var fields = type.GetFields().Where(field => field.IsDefined(typeof(UserCustomizableAttribute), false));
            foreach(var field in fields)
            {
                AddProperty(field.FieldType, field.Name, field.GetValue(obj));
            }

            var properties = type.GetProperties().Where(prop => prop.IsDefined(typeof(UserCustomizableAttribute), false));
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
                userObj.SetCustomizationState(keyValueState);
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
        public static void SetProperty(this ICustomizableProperty cProp, object obj)
        {
            if (ReferenceEquals(obj, null)) return;

            try
            {
                var field = obj.GetType().GetField(cProp.ID);
                if (field == null)
                {
                    var prop = obj.GetType().GetProperty(cProp.ID);
                    if (prop != null) prop.SetValue(obj, cProp.GetConvertedValue(prop.PropertyType));
                }
                else
                {
                    field.SetValue(obj, cProp.GetConvertedValue(field.FieldType));
                }
            }
            catch (Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogException(ex);
#endif
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
    }
}

#endif