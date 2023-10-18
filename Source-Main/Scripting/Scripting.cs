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

        private const string keyStr_m00 = "m00";
        private const string keyStr_m01 = "m01";
        private const string keyStr_m02 = "m02";
        private const string keyStr_m03 = "m03";

        private const string keyStr_m10 = "m10";
        private const string keyStr_m11 = "m11";
        private const string keyStr_m12 = "m12";
        private const string keyStr_m13 = "m13";

        private const string keyStr_m20 = "m20";
        private const string keyStr_m21 = "m21";
        private const string keyStr_m22 = "m22";
        private const string keyStr_m23 = "m23";

        private const string keyStr_m30 = "m30";
        private const string keyStr_m31 = "m31";
        private const string keyStr_m32 = "m32";
        private const string keyStr_m33 = "m33";

        /// <summary>
        /// Try to convert a C# type to a MiniScript value.
        /// </summary>
        public static Value AsValueMS(Type valueType, object value, object parent = null, Value previousValueInstance = null)
        {

            if (value == null || value == parent) return null;

            if (valueType.IsNumeric()) 
            { 
                
                if (previousValueInstance != null && previousValueInstance is ValNumber num)
                {
                    num.value = (double)value;
                    return num;
                }

                return new ValNumber((double)value); 
            
            }

            if (typeof(bool).IsAssignableFrom(valueType))
            {

                if (previousValueInstance != null && previousValueInstance is ValNumber num)
                {
                    num.value = ((bool)value) ? 1 : 0;
                    return num;
                }

                return new ValNumber(((bool)value) ? 1 : 0);

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

            if (valueType == typeof(Matrix4x4))
            {
                Matrix4x4 val = (Matrix4x4)value;

                ValMap internalVal;
                if (previousValueInstance is ValMap) internalVal = (ValMap)previousValueInstance; else internalVal = new ValMap();

                Value v;
                ValNumber m00, m01, m02, m03;
                ValNumber m10, m11, m12, m13;
                ValNumber m20, m21, m22, m23;
                ValNumber m30, m31, m32, m33;

                if (internalVal.TryGetValue(keyStr_m00, out v) && v is ValNumber) m00 = (ValNumber)v; else { internalVal[keyStr_m00] = m00 = new ValNumber(val.m00); }
                if (internalVal.TryGetValue(keyStr_m01, out v) && v is ValNumber) m01 = (ValNumber)v; else { internalVal[keyStr_m01] = m01 = new ValNumber(val.m01); }
                if (internalVal.TryGetValue(keyStr_m02, out v) && v is ValNumber) m02 = (ValNumber)v; else { internalVal[keyStr_m02] = m02 = new ValNumber(val.m02); }
                if (internalVal.TryGetValue(keyStr_m03, out v) && v is ValNumber) m03 = (ValNumber)v; else { internalVal[keyStr_m03] = m03 = new ValNumber(val.m03); }

                if (internalVal.TryGetValue(keyStr_m10, out v) && v is ValNumber) m10 = (ValNumber)v; else { internalVal[keyStr_m10] = m10 = new ValNumber(val.m10); }
                if (internalVal.TryGetValue(keyStr_m11, out v) && v is ValNumber) m11 = (ValNumber)v; else { internalVal[keyStr_m11] = m11 = new ValNumber(val.m11); }
                if (internalVal.TryGetValue(keyStr_m12, out v) && v is ValNumber) m12 = (ValNumber)v; else { internalVal[keyStr_m12] = m12 = new ValNumber(val.m12); }
                if (internalVal.TryGetValue(keyStr_m13, out v) && v is ValNumber) m13 = (ValNumber)v; else { internalVal[keyStr_m13] = m13 = new ValNumber(val.m13); }

                if (internalVal.TryGetValue(keyStr_m20, out v) && v is ValNumber) m20 = (ValNumber)v; else { internalVal[keyStr_m20] = m20 = new ValNumber(val.m20); }
                if (internalVal.TryGetValue(keyStr_m21, out v) && v is ValNumber) m21 = (ValNumber)v; else { internalVal[keyStr_m21] = m21 = new ValNumber(val.m21); }
                if (internalVal.TryGetValue(keyStr_m22, out v) && v is ValNumber) m22 = (ValNumber)v; else { internalVal[keyStr_m22] = m22 = new ValNumber(val.m22); }
                if (internalVal.TryGetValue(keyStr_m23, out v) && v is ValNumber) m23 = (ValNumber)v; else { internalVal[keyStr_m23] = m23 = new ValNumber(val.m23); }

                if (internalVal.TryGetValue(keyStr_m30, out v) && v is ValNumber) m30 = (ValNumber)v; else { internalVal[keyStr_m30] = m30 = new ValNumber(val.m30); }
                if (internalVal.TryGetValue(keyStr_m31, out v) && v is ValNumber) m31 = (ValNumber)v; else { internalVal[keyStr_m31] = m31 = new ValNumber(val.m31); }
                if (internalVal.TryGetValue(keyStr_m32, out v) && v is ValNumber) m32 = (ValNumber)v; else { internalVal[keyStr_m32] = m32 = new ValNumber(val.m32); }
                if (internalVal.TryGetValue(keyStr_m33, out v) && v is ValNumber) m33 = (ValNumber)v; else { internalVal[keyStr_m33] = m33 = new ValNumber(val.m33); }

                m00.value = val.m00;
                m01.value = val.m01;
                m02.value = val.m02;
                m03.value = val.m03;

                m10.value = val.m10;
                m11.value = val.m11;
                m12.value = val.m12;
                m13.value = val.m13;

                m20.value = val.m20;
                m21.value = val.m21;
                m22.value = val.m22;
                m23.value = val.m23;

                m30.value = val.m30;
                m31.value = val.m31;
                m32.value = val.m32;
                m33.value = val.m33;

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
