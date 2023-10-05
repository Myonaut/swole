using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Miniscript;
using static Swolescript.EngineInternal;

namespace Swolescript
{

    public static class Scripting
    {

        #region MiniScript

        /// <summary>
        /// Should not be called directly. Use IVar.ValueMS instead.
        /// </summary>
        public static Value GetValueMS(IVar variable)
        {

            if (variable == null) return null;

            return AsValueMS(variable.GetValueType(), variable.Value);

        }

        /// <summary>
        /// Try to convert a C# type to a MiniScript value.
        /// </summary>
        public static Value AsValueMS(Type valueType, object value, object parent = null)
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
                || valueType == typeof(sbyte)) return new ValNumber((double)value);

            if (valueType == typeof(Vector3))
            {
                Vector3 val = (Vector3)value;

                var internalVal = new ValMap();
                internalVal["x"] = new ValNumber(val.x);
                internalVal["y"] = new ValNumber(val.y);
                internalVal["z"] = new ValNumber(val.z);

                return internalVal;
            }

            if (valueType == typeof(Quaternion) || valueType == typeof(Vector4))
            {
                Vector4 val = (Vector4)value;

                var internalVal = new ValMap();
                internalVal["x"] = new ValNumber(val.x);
                internalVal["y"] = new ValNumber(val.y);
                internalVal["z"] = new ValNumber(val.z);
                internalVal["w"] = new ValNumber(val.w);

                return internalVal;
            }

            if (valueType == typeof(Vector2))
            {
                Vector2 val = (Vector2)value;

                var internalVal = new ValMap();
                internalVal["x"] = new ValNumber(val.x);
                internalVal["y"] = new ValNumber(val.y);

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
                                        ValMap internalVal = new ValMap();
                                        foreach (var element in collection)
                                        {
                                            internalVal[(string)keyProp.GetValue(element)] = AsValueMS(genArgs[1], valueProp.GetValue(element), value);
                                        }
                                        return internalVal;
                                    }
                                }
                            }
                        }

                        ValList internalList = new ValList();
                        foreach (var element in collection) internalList.values.Add(AsValueMS(genType, element, value));
                        return internalList;
                    }
                }
            }

            return null;

        }

        #endregion

    }

}
