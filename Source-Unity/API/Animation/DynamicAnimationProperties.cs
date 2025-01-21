#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;
using UnityEngine.Events;

namespace Swole.API.Unity.Animation
{
    public class DynamicAnimationProperties : MonoBehaviour
    {
        [Serializable]
        public struct PropertyCreator
        {
            public string name;
            public UnityEngine.Object objectReference;
            public string propertyName;

            public UnityEvent<float> OnValueChange;
        }

        public delegate float GetPropertyValueDelegate();
        public delegate void SetPropertyValueDelegate(float value);
        public class Property
        {
            public string name;
            public string displayName;
            public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;

            public float defaultValue;

            [NonSerialized]
            private int index;
            public int Index
            {
                get => index;
                set => index = value;
            }

            protected GetPropertyValueDelegate getValue;
            public void SetGetter(GetPropertyValueDelegate getValue) => this.getValue = getValue;
            public float GetValue() => getValue == null ? 0 : getValue();

            protected SetPropertyValueDelegate setValue;
            public void SetSetter(SetPropertyValueDelegate setValue) => this.setValue = setValue;
            public void SetValue(float value)
            {
                setValue?.Invoke(value);
                OnValueChange?.Invoke(value);

                if (listeners != null)
                {
                    foreach(var list in listeners.Values)
                    {
                        foreach(var listener in list) listener.Invoke(value); 
                    }
                }
            }

            public UnityEvent<float> OnValueChange;

            private Dictionary<string, List<SetPropertyValueDelegate>> listeners; 

            public void Listen(string listenId, SetPropertyValueDelegate listener)
            {
                if (string.IsNullOrWhiteSpace(listenId) || listener == null) return;

                if (listeners == null) listeners = new Dictionary<string, List<SetPropertyValueDelegate>>();
                if (!listeners.TryGetValue(listenId, out List<SetPropertyValueDelegate> list))
                {
                    list = new List<SetPropertyValueDelegate>();
                    listeners[listenId] = list;
                }

                list.Add(listener);
            }
            public void StopListening(string listenId, SetPropertyValueDelegate listener)
            {
                if (string.IsNullOrWhiteSpace(listenId) || listener == null || listeners == null) return;

                if (listeners.TryGetValue(listenId, out List<SetPropertyValueDelegate> list))
                {
                    list.RemoveAll(i => ReferenceEquals(i, listener));
                }
            }
            public void ClearListeners(string listenId)
            {
                if (string.IsNullOrWhiteSpace(listenId) || listeners == null) return;

                if (listeners.TryGetValue(listenId, out List<SetPropertyValueDelegate> list))
                {
                    list.Clear();
                }
            }
            public void ClearAllListeners()
            {
                if (listeners == null) return;
                listeners.Clear();
            }
        }

        [SerializeField]
        protected PropertyCreator[] creators;
        [NonSerialized]
        protected List<Property> properties = new List<Property>();
        public int PropertyCount => properties.Count;
        public Property GetPropertyUnsafe(int index) => properties[index];
        public Property GetProperty(int index)
        {
            if (index < 0 || index >= properties.Count) return null; 
            return GetPropertyUnsafe(index);
        }
        public void RemoveProperty(int index)
        {
            if (index < 0 || index >= properties.Count) return;

            if (index < properties.Count - 1)
            {
                var removed = properties[index];
                removed.ClearAllListeners();
                removed.Index = -1;

                var last = properties[properties.Count - 1];
                last.Index = index; 
                properties.RemoveAt(properties.Count - 1); 
                properties[index] = last;
            } 
            else
            {
                properties.RemoveAt(index);
            }
        }
        public void RemoveProperty(Property prop)
        {
            if (prop == null) return;

            RemoveProperty(properties.IndexOf(prop));
        }

        [NonSerialized]
        protected Dictionary<string, int> propertiesLookup;

        public int IndexOf(string propertyName)
        {
            if (propertiesLookup.TryGetValue(propertyName, out var index)) return index;
            return -1;
        }

        public float GetValueUnsafe(int propertyIndex) => properties[propertyIndex].GetValue();
        public float GetValue(int propertyIndex)
        {
            if (propertyIndex < 0 || propertyIndex >= properties.Count) return 0;
            return GetValueUnsafe(propertyIndex);
        }

        public void SetValueUnsafe(int propertyIndex, float value) => properties[propertyIndex].SetValue(value); 
        public void SetValue(int propertyIndex, float value)
        {
            if (propertyIndex < 0 || propertyIndex >= properties.Count) return; 
            SetValueUnsafe(propertyIndex, value);
        }

        protected virtual void Awake()
        {
            if (creators != null)
            {
                foreach (var creator in creators)
                {
                    if (creator.objectReference == null) continue;

                    var refType = creator.objectReference.GetType();
                    FieldInfo fieldInfo = refType.GetField(creator.propertyName);
                    PropertyInfo propInfo = null;
                    if (fieldInfo == null) propInfo = refType.GetProperty(creator.propertyName);
                    if (propInfo == null) continue;

                    Type valueType = fieldInfo == null ? fieldInfo.FieldType : propInfo.PropertyType;

                    var prop = new Property();
                    prop.name = creator.name;

                    if (typeof(float).IsAssignableFrom(valueType))
                    {
                        if (fieldInfo == null)
                        {
                            prop.SetGetter(() =>
                            {
                                if (creator.objectReference == null)
                                {
                                    prop.SetGetter(null);
                                    return 0f;
                                }
                                else
                                {
                                    return (float)propInfo.GetValue(creator.objectReference);
                                }
                            });

                            prop.SetSetter((float val) =>
                            {
                                if (creator.objectReference == null)
                                {
                                    prop.SetSetter(null);
                                }
                                else
                                {
                                    propInfo.SetValue(creator.objectReference, val);

                                }
                            });

                            prop.defaultValue = prop.GetValue();
                        }
                        else
                        {
                            prop.SetGetter(() =>
                            {
                                if (creator.objectReference == null)
                                {
                                    prop.SetGetter(null);
                                    return 0f;
                                }
                                else
                                {
                                    return (float)fieldInfo.GetValue(creator.objectReference);
                                }
                            });

                            prop.SetSetter((float val) =>
                            {
                                if (creator.objectReference == null)
                                {
                                    prop.SetSetter(null);
                                }
                                else
                                {
                                    fieldInfo.SetValue(creator.objectReference, val);

                                }
                            });

                            prop.defaultValue = prop.GetValue();
                        }
                    }
                    else if (typeof(int).IsAssignableFrom(valueType))
                    {
                        if (fieldInfo == null)
                        {
                            prop.SetGetter(() =>
                            {
                                if (creator.objectReference == null)
                                {
                                    prop.SetGetter(null);
                                    return 0f;
                                }
                                else
                                {
                                    return (float)propInfo.GetValue(creator.objectReference);
                                }
                            });

                            prop.SetSetter((float val) =>
                            {
                                if (creator.objectReference == null)
                                {
                                    prop.SetSetter(null);
                                }
                                else
                                {
                                    propInfo.SetValue(creator.objectReference, (int)val);

                                }
                            });

                            prop.defaultValue = prop.GetValue();
                        }
                        else
                        {
                            prop.SetGetter(() =>
                            {
                                if (creator.objectReference == null)
                                {
                                    prop.SetGetter(null);
                                    return 0f;
                                }
                                else
                                {
                                    return (float)fieldInfo.GetValue(creator.objectReference);
                                }
                            });

                            prop.SetSetter((float val) =>
                            {
                                if (creator.objectReference == null)
                                {
                                    prop.SetSetter(null);
                                }
                                else
                                {
                                    fieldInfo.SetValue(creator.objectReference, (int)val);

                                }
                            });

                            prop.defaultValue = prop.GetValue();
                        }
                    }
                    else if (typeof(bool).IsAssignableFrom(valueType))
                    {
                        if (fieldInfo == null)
                        {
                            prop.SetGetter(() =>
                            {
                                if (creator.objectReference == null)
                                {
                                    prop.SetGetter(null);
                                    return 0f;
                                }
                                else
                                {
                                    return ((bool)propInfo.GetValue(creator.objectReference)) ? 1f : 0f;
                                }
                            });

                            prop.SetSetter((float val) =>
                            {
                                if (creator.objectReference == null)
                                {
                                    prop.SetSetter(null);
                                }
                                else
                                {
                                    propInfo.SetValue(creator.objectReference, val > 0);

                                }
                            });

                            prop.defaultValue = prop.GetValue();
                        }
                        else
                        {
                            prop.SetGetter(() =>
                            {
                                if (creator.objectReference == null)
                                {
                                    prop.SetGetter(null);
                                    return 0f;
                                }
                                else
                                {
                                    return ((bool)fieldInfo.GetValue(creator.objectReference)) ? 1f : 0f;
                                }
                            });

                            prop.SetSetter((float val) =>
                            {
                                if (creator.objectReference == null)
                                {
                                    prop.SetSetter(null);
                                }
                                else
                                {
                                    fieldInfo.SetValue(creator.objectReference, val > 0);

                                }
                            });

                            prop.defaultValue = prop.GetValue(); 
                        }
                    }

                    prop.OnValueChange = creator.OnValueChange;
                }
            }

            propertiesLookup = new Dictionary<string, int>();
            for(int a = 0; a < properties.Count; a++)
            {
                var prop = properties[a];
                prop.Index = a;
                propertiesLookup[prop.name] = a;
            }
        }

        public Property CreateProperty(string name, GetPropertyValueDelegate getValue, SetPropertyValueDelegate setValue, float defaultValue = 0, string displayName = null)
        {
            var prop = new Property(); 

            prop.name = name;
            prop.displayName = displayName;
            prop.SetGetter(getValue);
            prop.SetSetter(setValue);

            prop.defaultValue = defaultValue;

            var index = properties.Count;
            properties.Add(prop);
            propertiesLookup[prop.name] = index; 

            return prop;
        }
    }
}

#endif