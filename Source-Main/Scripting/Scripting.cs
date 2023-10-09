using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Miniscript;
using static Swole.EngineInternal;

namespace Swole.Script
{

    public static class Scripting
    {

        #region MiniScript

        /// <summary>
        /// Should not be called directly. Use IVar.ValueMS instead.
        /// </summary>
        public static Value GetValueMS(IVar variable, Value previousValueInstance = null)
        {

            if (variable == null) return null;

            return AsValueMS(variable.GetValueType(), variable.Value, null, previousValueInstance);

        }

        private const string keyStr_X = "x";
        private const string keyStr_Y = "y";
        private const string keyStr_Z = "z";
        private const string keyStr_W = "w";

        /// <summary>
        /// Try to convert a C# type to a MiniScript value.
        /// </summary>
        public static Value AsValueMS(Type valueType, object value, object parent = null, Value previousValueInstance = null)
        {

            if (value == null || value == parent) return null;

            if (valueType == typeof(double)
                || valueType == typeof(float)
                || valueType == typeof(decimal)
                || valueType == typeof(long)
                || valueType == typeof(ulong)
                || valueType == typeof(int)
                || valueType == typeof(uint)
                || valueType == typeof(short)
                || valueType == typeof(ushort)
                || valueType == typeof(byte)
                || valueType == typeof(sbyte)) 
            { 
                
                if (previousValueInstance != null && previousValueInstance is ValNumber num)
                {
                    num.value = (double)value;
                    return num;
                }

                return new ValNumber((double)value); 
            
            }

            if (valueType == typeof(Vector3))
            {
                Vector3 val = (Vector3)value;

                ValMap internalVal;
                if (previousValueInstance is ValMap) internalVal = (ValMap)previousValueInstance; else internalVal = new ValMap();

                Value v;
                ValNumber x, y, z;
                if (internalVal.TryGetValue(keyStr_X, out v) && v is ValNumber) x = (ValNumber)v; else { internalVal[keyStr_X] = x = new ValNumber(val.x); }
                if (internalVal.TryGetValue(keyStr_Y, out v) && v is ValNumber) y = (ValNumber)v; else { internalVal[keyStr_Y] = y = new ValNumber(val.y); }
                if (internalVal.TryGetValue(keyStr_Z, out v) && v is ValNumber) z = (ValNumber)v; else { internalVal[keyStr_Z] = z = new ValNumber(val.z); }

                x.value = val.x;
                y.value = val.y;
                z.value = val.z;

                return internalVal;
            }

            if (valueType == typeof(Quaternion) || valueType == typeof(Vector4))
            {
                Vector4 val = (Vector4)value;
                ValMap internalVal;
                if (previousValueInstance is ValMap) internalVal = (ValMap)previousValueInstance; else internalVal = new ValMap();

                Value v;
                ValNumber x, y, z, w;
                if (internalVal.TryGetValue(keyStr_X, out v) && v is ValNumber) x = (ValNumber)v; else { internalVal[keyStr_X] = x = new ValNumber(val.x); }
                if (internalVal.TryGetValue(keyStr_Y, out v) && v is ValNumber) y = (ValNumber)v; else { internalVal[keyStr_Y] = y = new ValNumber(val.y); }
                if (internalVal.TryGetValue(keyStr_Z, out v) && v is ValNumber) z = (ValNumber)v; else { internalVal[keyStr_Z] = z = new ValNumber(val.z); }
                if (internalVal.TryGetValue(keyStr_W, out v) && v is ValNumber) w = (ValNumber)v; else { internalVal[keyStr_W] = w = new ValNumber(val.w); }

                x.value = val.x;
                y.value = val.y;
                z.value = val.z;
                w.value = val.w;

                return internalVal;
            }

            if (valueType == typeof(Vector2))
            {
                Vector2 val = (Vector2)value;

                ValMap internalVal;
                if (previousValueInstance is ValMap) internalVal = (ValMap)previousValueInstance; else internalVal = new ValMap();

                Value v;
                ValNumber x, y;
                if (internalVal.TryGetValue(keyStr_X, out v) && v is ValNumber) x = (ValNumber)v; else { internalVal[keyStr_X] = x = new ValNumber(val.x); }
                if (internalVal.TryGetValue(keyStr_Y, out v) && v is ValNumber) y = (ValNumber)v; else { internalVal[keyStr_Y] = y = new ValNumber(val.y); }

                x.value = val.x;
                y.value = val.y;

                return internalVal;
            }
             
            if (typeof(ICollection).IsAssignableFrom(valueType))
            {
                ICollection collection = (ICollection)value;
                if (valueType.IsGenericType)
                {
                    var genArgs = valueType.GetGenericArguments();
                    if (genArgs != null && genArgs.Length == 1)
                    {
                        var genType = genArgs[0];
                        if (typeof(KeyValuePair<,>).IsAssignableFrom(genType))
                        {
                            genArgs = genType.GetGenericArguments();
                            if (genArgs != null && genArgs.Length == 2)
                            {
                                var keyType = genArgs[0];
                                if (typeof(string).IsAssignableFrom(keyType))
                                {
                                    // Store KeyValuePair collection as ValMap
                                    PropertyInfo keyProp = genType.GetProperty("Key");
                                    PropertyInfo valueProp = genType.GetProperty("Value");
                                    if (keyProp != null && valueProp != null)
                                    {
                                        ValMap internalVal;
                                        if (previousValueInstance is ValMap) internalVal = (ValMap)previousValueInstance; else internalVal = new ValMap();
                                        foreach (var element in collection)
                                        {
                                            string key = (string)keyProp.GetValue(element);
                                            internalVal.TryGetValue(key, out Value existingVal);
                                            internalVal[key] = AsValueMS(genArgs[1], valueProp.GetValue(element), value, existingVal);
                                        }
                                        return internalVal;
                                    }
                                }
                            }
                        }

                        ValList internalList;
                        if (previousValueInstance is ValList) internalList = (ValList)previousValueInstance; else internalList = new ValList();
                        int i = 0;
                        foreach (var element in collection) 
                        {
                            Value existingVal = null;
                            if (i < internalList.values.Count)
                            {
                                existingVal = internalList.values[i];
                                internalList.values[i] = AsValueMS(genType, element, value, existingVal);
                            } 
                            else
                            {
                                internalList.values.Add(AsValueMS(genType, element, value));
                            }
                            i++;
                        }
                        return internalList;
                    }
                }
            }

            return null;

        }

        #endregion

    }

}
