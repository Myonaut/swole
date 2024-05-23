#if SWOLE_ENV

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

        private static string[] executionLayerNames = Enum.GetNames(typeof(ExecutionLayer));
        public static bool TryParseExecutionLayer(string layerName, out ExecutionLayer layer)
        {
            layer = ExecutionLayer.Initialization;
            if (string.IsNullOrWhiteSpace(layerName)) return false;

            layerName = layerName.AsID();

            foreach(var _layerName in executionLayerNames) if (_layerName.AsID() == layerName)
                {
                    layer = Enum.Parse<ExecutionLayer>(_layerName);
                    return true;
                }

            return false;
        }

        #region MiniScript

        /// <summary>
        /// Should not be called directly. Use IVar.ValueMS instead.
        /// </summary>
        public static Value GetValueMS(IVar variable, Value previousValueInstance = null)
        {

            if (variable == null) return null;

            return AsValueMS(variable.GetValueType(), variable.Value, null, previousValueInstance);

        }

        public static bool TryCastAsMSType<T>(this Value value, out T result) where T : Value
        {
            result = default;
            if (value is T)
            {
                result = (T)value;
            }
            else if (value is ValMap _map)
            {
                _map.TryGetValue(ValString.magicIsA.value, out Value parent);
                while (parent != null)
                {
                    if (parent is T)
                    {
                        result = (T)parent;
                        break;
                    }
                    else if (parent is ValMap)
                    {
                        _map = (ValMap)parent;
                        if (!_map.TryGetValue(ValString.magicIsA.value, out parent)) break;
                    }
                }
            }

            return result != null;
        }
        public static Type GetRealType(this Value msValue) 
        {
            if (msValue == null) return null;

            string valueStr = msValue.ToString();
            if (valueStr == null) valueStr = string.Empty;
            valueStr = valueStr.AsID();

            Type type = null;

            SwoleScriptType sst = null;
            if (msValue is ValString str)
            {
                foreach (var _sst in SwoleScriptType.allTypes)
                {
                    if (_sst == null) continue;
                    if (_sst.Name.AsID() == valueStr)
                    {
                        sst = _sst;
                        break;
                    }
                }
            }
            else if (msValue is SwoleScriptType)
            {
                sst = (SwoleScriptType)msValue;
            }
            else if (msValue is ValMap map)
            {
                if (map.TryCastAsMSType<SwoleScriptType>(out var _temp)) sst = _temp;
            }
            
            if (sst != null)
            {
                return sst.RealType;
            }
            else if (msValue is ValNumber || valueStr == "number")
            {
                return typeof(float);
            }
            else if (msValue is ValString || valueStr == "string")
            {
                return typeof(string);
            }
            else if (msValue is ValVector || valueStr == "vector")
            {
                return typeof(EngineInternal.Vector4);
            }
            else if (msValue is ValQuaternion || valueStr == "quaternion")
            {
                return typeof(EngineInternal.Quaternion);
            }

            return type;
        }
        public static bool TryGetRealType(Value msValue, out Type type)
        {
            type = GetRealType(msValue);
            return type != null;
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
        public static Value AsValueMS(Type valueType, object value, object parent = null, Value previousValueInstance = null, int recursionMax = 8, int recursionLevel = 0)
        {

            if (recursionLevel > recursionMax || value == null || value == parent) return null;

            if (valueType.IsNumeric()) 
            {

                if (previousValueInstance != null && previousValueInstance is ValNumber num)
                {
                    num.value = Convert.ToDouble(value);//(double)value;
                    return num;
                }

                return new ValNumber(Convert.ToDouble(value)/*(double)value*/); 
            
            }

            if (typeof(bool).IsAssignableFrom(valueType))
            {

                if (previousValueInstance != null && previousValueInstance is ValNumber num)
                {
                    num.value = Convert.ToBoolean(value) ? 1 : 0;
                    return num;
                }

                return new ValNumber(Convert.ToBoolean(value) ? 1 : 0);

            }

            if (typeof(string).IsAssignableFrom(valueType))
            {

                if (previousValueInstance != null && previousValueInstance is ValString str)
                {
                    str.value = Convert.ToString(value);
                    return str;
                }

                return new ValString((string)value);

            }

            if (valueType == typeof(Vector3))
            {
                Vector3 val = (Vector3)value;

                /*
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
                */
                ValVector internalVal;
                if (previousValueInstance is ValVector) internalVal = (ValVector)previousValueInstance; else internalVal = new ValVector(val.x, val.y, val.z);

                if (internalVal.x != null) internalVal.x.value = val.x;
                if (internalVal.y != null) internalVal.y.value = val.y;
                if (internalVal.z != null) internalVal.z.value = val.z;

                return internalVal;
            }

            if (valueType == typeof(Quaternion))
            {
                Quaternion val = (Quaternion)value;

                ValQuaternion internalVal;
                if (previousValueInstance is ValQuaternion) internalVal = (ValQuaternion)previousValueInstance; else internalVal = new ValQuaternion(val.x, val.y, val.z, val.w);

                if (internalVal.x != null) internalVal.x.value = val.x;
                if (internalVal.y != null) internalVal.y.value = val.y;
                if (internalVal.z != null) internalVal.z.value = val.z;
                if (internalVal.w != null) internalVal.w.value = val.w;

                return internalVal;
            }

            /*
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
            */

            if (valueType == typeof(Vector2))
            {
                Vector2 val = (Vector2)value;

                /*
                ValMap internalVal;
                if (previousValueInstance is ValMap) internalVal = (ValMap)previousValueInstance; else internalVal = new ValMap();

                Value v;
                ValNumber x, y;
                if (internalVal.TryGetValue(keyStr_X, out v) && v is ValNumber) x = (ValNumber)v; else { internalVal[keyStr_X] = x = new ValNumber(val.x); }
                if (internalVal.TryGetValue(keyStr_Y, out v) && v is ValNumber) y = (ValNumber)v; else { internalVal[keyStr_Y] = y = new ValNumber(val.y); }

                x.value = val.x;
                y.value = val.y;
                */
                ValVector internalVal;
                if (previousValueInstance is ValVector) internalVal = (ValVector)previousValueInstance; else internalVal = new ValVector(val.x, val.y);

                if (internalVal.x != null) internalVal.x.value = val.x;
                if (internalVal.y != null) internalVal.y.value = val.y;

                return internalVal;
            }

            if (valueType == typeof(Vector4))
            {
                Vector4 val = (Vector4)value;

                ValVector internalVal;
                if (previousValueInstance is ValVector) internalVal = (ValVector)previousValueInstance; else internalVal = new ValVector(val.x, val.y, val.z, val.w);

                if (internalVal.x != null) internalVal.x.value = val.x;
                if (internalVal.y != null) internalVal.y.value = val.y;
                if (internalVal.z != null) internalVal.z.value = val.z;
                if (internalVal.w != null) internalVal.w.value = val.w;

                return internalVal;
            }

            if (valueType == typeof(Matrix4x4))
            {
                Matrix4x4 val = (Matrix4x4)value;

                /*
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
                */
                ValMatrix internalVal;
                if (previousValueInstance is ValMatrix) internalVal = (ValMatrix)previousValueInstance; else internalVal = new ValMatrix(val.m00, val.m01, val.m02, val.m03, val.m10, val.m11, val.m12, val.m13, val.m20, val.m21, val.m22, val.m23, val.m30, val.m31, val.m32, val.m33);

                if (internalVal.c0 != null)
                {
                    if (internalVal.c0.x != null) internalVal.c0.x.value = val.m00;
                    if (internalVal.c0.y != null) internalVal.c0.y.value = val.m01;
                    if (internalVal.c0.z != null) internalVal.c0.z.value = val.m02;
                    if (internalVal.c0.w != null) internalVal.c0.w.value = val.m03;
                }
                if (internalVal.c1 != null)
                {
                    if (internalVal.c1.x != null) internalVal.c1.x.value = val.m10;
                    if (internalVal.c1.y != null) internalVal.c1.y.value = val.m11;
                    if (internalVal.c1.z != null) internalVal.c1.z.value = val.m12;
                    if (internalVal.c1.w != null) internalVal.c1.w.value = val.m13;
                }
                if (internalVal.c2 != null)
                {
                    if (internalVal.c2.x != null) internalVal.c2.x.value = val.m20;
                    if (internalVal.c2.y != null) internalVal.c2.y.value = val.m21;
                    if (internalVal.c2.z != null) internalVal.c2.z.value = val.m22;
                    if (internalVal.c2.w != null) internalVal.c2.w.value = val.m23;
                }
                if (internalVal.c3 != null)
                {
                    if (internalVal.c3.x != null) internalVal.c3.x.value = val.m30;
                    if (internalVal.c3.y != null) internalVal.c3.y.value = val.m31;
                    if (internalVal.c3.z != null) internalVal.c3.z.value = val.m32;
                    if (internalVal.c3.w != null) internalVal.c3.w.value = val.m33;
                }

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
                                            internalVal[key] = AsValueMS(genArgs[1], valueProp.GetValue(element), value, existingVal, recursionMax, recursionLevel + 1);
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
                                internalList.values[i] = AsValueMS(genType, element, value, existingVal, recursionMax, recursionLevel + 1);
                            } 
                            else
                            {
                                internalList.values.Add(AsValueMS(genType, element, value, null, recursionMax, recursionLevel + 1));
                            }
                            i++;
                        }
                        return internalList;
                    }
                }
            }

            return null;

        }

        /// <summary>
        /// Try to convert a MiniScript value to C# type.
        /// </summary>
        public static object FromValueMS(Value value)
        {
            if (value is ValNumber num)
            {
                return num.value; 
            } 
            else if (value is ValString str)
            {
                return str.value;
            }
            else if (value is ValList list)
            {
                return list.values;
            }
            else if (value is ValMap map)
            {
                return map.map;
            }
            else if (value is ValVector vec)
            {
                int compCount = vec.ComponentCount;
                if (compCount <= 2)
                {
                    return new Vector2(vec.x == null ? 0 : (float)vec.x.value, vec.y == null ? 0 : (float)vec.y.value);
                }
                else if (compCount == 3)
                {
                    return new Vector3(vec.x == null ? 0 : (float)vec.x.value, vec.y == null ? 0 : (float)vec.y.value, vec.z == null ? 0 : (float)vec.z.value);
                }
                else if (compCount >= 4)
                {
                    return new Vector4(vec.x == null ? 0 : (float)vec.x.value, vec.y == null ? 0 : (float)vec.y.value, vec.z == null ? 0 : (float)vec.z.value, vec.w == null ? 0 : (float)vec.w.value);
                }
            }
            else if (value is ValQuaternion quat)
            {
                return new Quaternion(quat.x == null ? 0 : (float)quat.x.value, quat.y == null ? 0 : (float)quat.y.value, quat.z == null ? 0 : (float)quat.z.value, quat.w == null ? 0 : (float)quat.w.value);
            }
            else if (value is ValMatrix matrix)
            {
                float m00, m01, m02, m03, m10, m11, m12, m13, m20, m21, m22, m23, m30, m31, m32, m33;
                m00 = m01 = m02 = m03 = m10 = m11 = m12 = m13 = m20 = m21 = m22 = m23 = m30 = m31 = m32 = m33 = 0;
                if (matrix.c0 != null)
                {
                    m00 = matrix.c0.x == null ? 0 : (float)matrix.c0.x.value;
                    m01 = matrix.c0.y == null ? 0 : (float)matrix.c0.y.value;
                    m02 = matrix.c0.z == null ? 0 : (float)matrix.c0.z.value;
                    m03 = matrix.c0.w == null ? 0 : (float)matrix.c0.w.value;

                    if (matrix.c1 != null)
                    {
                        m10 = matrix.c1.x == null ? 0 : (float)matrix.c1.x.value;
                        m11 = matrix.c1.y == null ? 0 : (float)matrix.c1.y.value;
                        m12 = matrix.c1.z == null ? 0 : (float)matrix.c1.z.value;
                        m13 = matrix.c1.w == null ? 0 : (float)matrix.c1.w.value;

                        if (matrix.c2 != null)
                        {
                            m20 = matrix.c2.x == null ? 0 : (float)matrix.c2.x.value;
                            m21 = matrix.c2.y == null ? 0 : (float)matrix.c2.y.value;
                            m22 = matrix.c2.z == null ? 0 : (float)matrix.c2.z.value;
                            m23 = matrix.c2.w == null ? 0 : (float)matrix.c2.w.value;

                            if (matrix.c3 != null)
                            {
                                m30 = matrix.c3.x == null ? 0 : (float)matrix.c3.x.value;
                                m31 = matrix.c3.y == null ? 0 : (float)matrix.c3.y.value;
                                m32 = matrix.c3.z == null ? 0 : (float)matrix.c3.z.value;
                                m33 = matrix.c3.w == null ? 0 : (float)matrix.c3.w.value;
                            }
                        }
                    }
                } 
                return new Matrix4x4(new Vector4(m00, m01, m02, m03), new Vector4(m10, m11, m12, m13), new Vector4(m20, m21, m22, m23), new Vector4(m30, m31, m32, m33));
            } 
            else if (value is SwoleScriptIntrinsics.ValReference valRef)
            {
                return valRef.Reference; 
            }

            return null;
        }

        #endregion

    }

}

#endif