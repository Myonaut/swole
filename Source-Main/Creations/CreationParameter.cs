using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

using Miniscript;

using static Swole.EngineInternal;

namespace Swole
{

    [Serializable]
    public struct CreationParameter
    {

        [Serializable]
        public enum ConversionMethod
        {

            None, LocalToRoot, LocalToWorld, RootToLocal, WorldToLocal, RootToWorld, WorldToRoot

        } 

        /// <summary>
        /// Used to convert a parameter from one transform space to another (if applicable).
        /// </summary>
        public ConversionMethod conversionMethod;

        [Serializable]
        public enum Type
        {

            Float, Double, Int, Bool, String, Vector2, Vector3, Vector4, Quaternion, EulerAngles, Matrix4x4, PositionLocal, DirectionLocal, RotationLocal, RotationEulerLocal, TangentLocal, ScaleLocal, PositionRoot, DirectionRoot, RotationRoot, RotationEulerRoot, TangentRoot, PositionWorld, DirectionWorld, RotationWorld, RotationEulerWorld, TangentWorld, LocalToRoot, LocalToWorld, RootToLocal, WorldToLocal

        }

        /// <summary>
        /// This enum is used to describe a parameter. The parameter will be treated differently during space conversion depending on this type, even if the value type is identical. E.g. Type.PositionLocal will be treated as a point, whereas Type.DirectionLocal will be treated as a vector. Both are Vector3s.
        /// Note: the transform space suffix in this type's name (Local, Root, World), if present, cannot be enforced and may not be accurate. If the parameter was created through the Swole API, then the suffix should be accurate and should represent the transform space after a conversion method is applied.
        /// </summary>
        public CreationParameter.Type type;

        public System.Type TypeCSharp
        {

            get
            {

                switch (type)
                {
                    case Type.Float: return typeof(float);
                    case Type.Double: return typeof(double);
                    case Type.Int: return typeof(int);
                    case Type.Bool: return typeof(bool);
                    case Type.String: return typeof(string);
                    case Type.Vector2: return typeof(Vector2);
                    case Type.Vector3: case Type.EulerAngles: case Type.ScaleLocal: case Type.PositionLocal: case Type.PositionRoot: case Type.PositionWorld: case Type.DirectionLocal: case Type.DirectionRoot: case Type.DirectionWorld: case Type.RotationEulerLocal: case Type.RotationEulerRoot: case Type.RotationEulerWorld: return typeof(Vector3);
                    case Type.Vector4: case Type.TangentLocal: case Type.TangentRoot: case Type.TangentWorld: return typeof(Vector4);
                    case Type.Quaternion: case Type.RotationLocal: case Type.RotationRoot: case Type.RotationWorld: return typeof(Quaternion);
                    case Type.Matrix4x4: case Type.LocalToRoot: case Type.LocalToWorld: case Type.RootToLocal: case Type.WorldToLocal: return typeof(Matrix4x4);
                }

                return null;

            }

        }

        public System.Type TypeMS
        {

            get
            {

                switch (type)
                {
                    case Type.Float: return typeof(ValNumber);
                    case Type.Double: return typeof(ValNumber);
                    case Type.Int: return typeof(ValNumber);
                    case Type.Bool: return typeof(ValNumber);
                    case Type.String: return typeof(ValString);
                    case Type.Vector2: return typeof(ValMap);
                    case Type.Vector3: case Type.EulerAngles: case Type.ScaleLocal: case Type.PositionLocal: case Type.PositionRoot: case Type.PositionWorld: case Type.DirectionLocal: case Type.DirectionRoot: case Type.DirectionWorld: case Type.RotationEulerLocal: case Type.RotationEulerRoot: case Type.RotationEulerWorld: return typeof(ValMap);
                    case Type.Vector4: case Type.TangentLocal: case Type.TangentRoot: case Type.TangentWorld: return typeof(ValMap);
                    case Type.Quaternion: case Type.RotationLocal: case Type.RotationRoot: case Type.RotationWorld: return typeof(ValMap);
                    case Type.Matrix4x4: case Type.LocalToRoot: case Type.LocalToWorld: case Type.RootToLocal: case Type.WorldToLocal: return typeof(ValMap);
                }

                return null;

            }

        }

        public string name;

        public double defaultValue;
        public string defaultString;

        public Vector4 defaultVector;
        public Matrix4x4 defaultMatrix;

        [Serializable]
        public enum MutationOp
        {

            Add, Subtract, Multiply, Divide, Modulo

        }

        public static double MutateDouble(double value, MutationOp mutationOp, double mutatorValue)
        {

            switch (mutationOp)
            {

                case MutationOp.Add:
                    return value + mutatorValue;

                case MutationOp.Subtract:
                    return value - mutatorValue;

                case MutationOp.Multiply:
                    return value * mutatorValue;

                case MutationOp.Divide:
                    return value / mutatorValue;

                case MutationOp.Modulo:
                    return value % mutatorValue;

            }

            return value;

        }
        public static float MutateFloat(float value, MutationOp mutationOp, float mutatorValue)
        {

            switch (mutationOp)
            {

                case MutationOp.Add:
                    return value + mutatorValue;

                case MutationOp.Subtract:
                    return value - mutatorValue;

                case MutationOp.Multiply:
                    return value * mutatorValue;

                case MutationOp.Divide:
                    return value / mutatorValue;

                case MutationOp.Modulo:
                    return value % mutatorValue;

            }

            return value;

        }
        public static int MutateInt(int value, MutationOp mutationOp, int mutatorValue)
        {

            switch (mutationOp)
            {

                case MutationOp.Add:
                    return value + mutatorValue;

                case MutationOp.Subtract:
                    return value - mutatorValue;

                case MutationOp.Multiply:
                    return value * mutatorValue;

                case MutationOp.Divide:
                    return value / mutatorValue;

                case MutationOp.Modulo:
                    return value % mutatorValue;

            }

            return value;

        }
        public static Quaternion MutateQuaternion(Quaternion main, Quaternion delta)
        {
            if (main.IsZero) delta = Quaternion.identity;
            if (delta.IsZero) delta = Quaternion.identity;
            return swole.Engine.Mul(main, delta);
        }
        public static Quaternion MutateEulerAsQuaternion(Vector3 main, Vector3 delta) => swole.Engine.Mul(Quaternion.Euler(main), Quaternion.Euler(delta));
        public static Vector3 MutateEuler(Vector3 main, Vector3 delta) => MutateEulerAsQuaternion(main, delta).EulerAngles;

        public MutationOp mutationOp;

        /// <summary>
        /// A value that is combined with the final output using an operation if the type is numeric.
        /// </summary>
        public double mutatorValue;

        /// <summary>
        /// A delta value that is combined with the final output if the type is vectorized.
        /// </summary>
        public Vector4 offsetVector;

        /// <summary>
        /// Name of creation child object to link to.
        /// </summary>
        public string objectLink;
        /// <summary>
        /// Name of linked object field or property.
        /// </summary>
        public string memberLink;

        public double DefaultValueAsDouble => MutateDouble(defaultValue, mutationOp, mutatorValue);
        public float DefaultValueAsFloat => MutateFloat((float)defaultValue, mutationOp, (float)mutatorValue);
        public int DefaultValueAsInt => MutateInt((int)defaultValue, mutationOp, (int)mutatorValue);
        public bool DefaultValueAsBool => defaultValue.AsBool();

        public string DefaultValueAsString => defaultString;

        public Vector2 DefaultValueAsVector2 => (defaultVector + offsetVector);
        public Vector3 DefaultValueAsVector3 
        { 
            
            get 
            {

                switch(type)
                {

                    case Type.Quaternion:
                    case Type.RotationLocal:
                    case Type.RotationRoot:
                    case Type.RotationWorld:
                    case Type.EulerAngles:
                    case Type.RotationEulerLocal:
                    case Type.RotationEulerRoot:
                    case Type.RotationEulerWorld:
                        return DefaultValueAsQuaternion.EulerAngles;

                }

                return defaultVector + offsetVector;

            } 
        
        }
        public Vector4 DefaultValueAsVector4 
        { 
        
            get
            {

                switch (type)
                {

                    case Type.Quaternion:
                    case Type.RotationLocal:
                    case Type.RotationRoot:
                    case Type.RotationWorld:
                    case Type.EulerAngles:
                    case Type.RotationEulerLocal:
                    case Type.RotationEulerRoot:
                    case Type.RotationEulerWorld:
                        return DefaultValueAsQuaternion;

                }

                return defaultVector + offsetVector;

            }

        }
        public Quaternion DefaultValueAsQuaternion 
        {

            get
            {

                switch (type)
                {

                    default:
                        return MutateQuaternion(defaultVector, offsetVector);

                    case Type.EulerAngles:
                    case Type.RotationEulerLocal:
                    case Type.RotationEulerRoot:
                    case Type.RotationEulerWorld:
                        return MutateEulerAsQuaternion(defaultVector, offsetVector);

                }

            }

        }

        public Matrix4x4 DefaultValueAsMatrix4x4 => defaultMatrix;

        private void LogGetConversionError(SwoleLogger logger, Exception ex, System.Type conversionType)
        {
            if (logger != null)
            {
                logger.LogError($"Error convering parameter '{name}' to {conversionType.Name}!");
                logger.LogError($"[{ex.GetType().ToString()}] {ex.Message}");
            }
        }
        private void LogSetConversionError(SwoleLogger logger, Exception ex, System.Type conversionType)
        {
            if (logger != null)
            {
                logger.LogError($"Error setting parameter '{name}' to {conversionType.Name}!");
                logger.LogError($"[{ex.GetType().ToString()}] {ex.Message}");
            }
        }

        public bool Set<T>(FieldInfo info, object instance, T value, SwoleLogger logger = null)
        {
            if (info == null || instance == null) return false;
            try
            {
                info.SetValue(instance, value);
                return true;
            }
            catch (Exception ex) { LogSetConversionError(logger, ex, typeof(T)); }
            return false;
        }
        public bool Set<T>(PropertyInfo info, object instance, T value, SwoleLogger logger = null)
        {
            if (info == null || instance == null) return false;
            try
            {
                info.SetValue(instance, value);
                return true;
            }
            catch (Exception ex) { LogSetConversionError(logger, ex, typeof(T)); }
            return false;
        }

        public T Get<T>(FieldInfo info, object instance, SwoleLogger logger = null)
        {
            if (info == null || instance == null) return default;
            try
            {

                System.Type genericType = typeof(T);

                if (typeof(double).IsAssignableFrom(genericType))
                {
                    return (T)Convert.ChangeType(AsDouble(info, instance, logger), genericType);
                }
                else if (typeof(float).IsAssignableFrom(genericType))
                {
                    return (T)Convert.ChangeType(AsFloat(info, instance, logger), genericType);
                }
                else if (typeof(int).IsAssignableFrom(genericType))
                {
                    return (T)Convert.ChangeType(AsInt(info, instance, logger), genericType);
                }
                else if (typeof(bool).IsAssignableFrom(genericType))
                {
                    return (T)Convert.ChangeType(AsBool(info, instance, logger), genericType);
                }
                else if (typeof(string).IsAssignableFrom(genericType))
                {
                    return (T)Convert.ChangeType(AsString(info, instance, logger), genericType);
                }
                else if (typeof(Vector2).IsAssignableFrom(genericType))
                {
                    return (T)Convert.ChangeType(AsVector2(info, instance, logger), genericType);
                }
                else if (typeof(Vector3).IsAssignableFrom(genericType))
                {
                    return (T)Convert.ChangeType(AsVector3(info, instance, logger), genericType);
                }
                else if (typeof(Vector4).IsAssignableFrom(genericType))
                {
                    return (T)Convert.ChangeType(AsVector4(info, instance, logger), genericType);
                }
                else if (typeof(Quaternion).IsAssignableFrom(genericType))
                {
                    return (T)Convert.ChangeType(AsQuaternion(info, instance, logger), genericType);
                }
                else if (typeof(Matrix4x4).IsAssignableFrom(genericType))
                {
                    return (T)Convert.ChangeType(AsMatrix4x4(info, instance, logger), genericType);
                }

                return (T)info.GetValue(instance);
            }
            catch (Exception ex) { LogSetConversionError(logger, ex, typeof(T)); }
            return default;
        }
        public T Get<T>(PropertyInfo info, object instance, SwoleLogger logger = null)
        {
            if (info == null || instance == null) return default;
            try
            {

                System.Type genericType = typeof(T);

                if (typeof(double).IsAssignableFrom(genericType))
                {
                    return (T)Convert.ChangeType(AsDouble(info, instance, logger), genericType);
                }
                else if (typeof(float).IsAssignableFrom(genericType))
                {
                    return (T)Convert.ChangeType(AsFloat(info, instance, logger), genericType);
                }
                else if (typeof(int).IsAssignableFrom(genericType))
                {
                    return (T)Convert.ChangeType(AsInt(info, instance, logger), genericType);
                }
                else if (typeof(bool).IsAssignableFrom(genericType))
                {
                    return (T)Convert.ChangeType(AsBool(info, instance, logger), genericType);
                }
                else if (typeof(string).IsAssignableFrom(genericType))
                {
                    return (T)Convert.ChangeType(AsString(info, instance, logger), genericType);
                }
                else if (typeof(Vector2).IsAssignableFrom(genericType))
                {
                    return (T)Convert.ChangeType(AsVector2(info, instance, logger), genericType);
                }
                else if (typeof(Vector3).IsAssignableFrom(genericType))
                {
                    return (T)Convert.ChangeType(AsVector3(info, instance, logger), genericType);
                }
                else if (typeof(Vector4).IsAssignableFrom(genericType))
                {
                    return (T)Convert.ChangeType(AsVector4(info, instance, logger), genericType);
                }
                else if (typeof(Quaternion).IsAssignableFrom(genericType))
                {
                    return (T)Convert.ChangeType(AsQuaternion(info, instance, logger), genericType);
                }
                else if (typeof(Matrix4x4).IsAssignableFrom(genericType))
                {
                    return (T)Convert.ChangeType(AsMatrix4x4(info, instance, logger), genericType);
                }

                return (T)info.GetValue(instance);
            }
            catch (Exception ex) { LogSetConversionError(logger, ex, typeof(T)); }
            return default;
        }

        public double AsDouble(FieldInfo info, object instance, SwoleLogger logger = null)
        {
            if (info == null || instance == null) return 0.0;
            try { AsDouble((double)info.GetValue(instance)); }
            catch (Exception ex) { LogGetConversionError(logger, ex, typeof(double)); }
            return 0.0;
        }
        public double AsDouble(PropertyInfo info, object instance, SwoleLogger logger)
        {
            if (info == null || instance == null) return 0;
            try { AsDouble((double)info.GetValue(instance)); }
            catch (Exception ex) { LogGetConversionError(logger, ex, typeof(double)); }
            return 0.0;
        }
        public double AsDouble(double value) => MutateDouble(value, mutationOp, mutatorValue);

        public float AsFloat(FieldInfo info, object instance, SwoleLogger logger = null)
        {
            if (info == null || instance == null) return 0f;
            try { AsFloat((float)info.GetValue(instance)); }
            catch (Exception ex) { LogGetConversionError(logger, ex, typeof(float)); }
            return 0f;
        }
        public float AsFloat(PropertyInfo info, object instance, SwoleLogger logger = null)
        {
            if (info == null || instance == null) return 0f;
            try { AsFloat((float)info.GetValue(instance)); }
            catch (Exception ex) { LogGetConversionError(logger, ex, typeof(float)); }
            return 0f;
        }
        public float AsFloat(float value) => MutateFloat(value, mutationOp, (float)mutatorValue);

        public int AsInt(FieldInfo info, object instance, SwoleLogger logger = null)
        {
            if (info == null || instance == null) return 0;
            try { return AsInt((int)info.GetValue(instance)); }
            catch (Exception ex) { LogGetConversionError(logger, ex, typeof(int)); }
            return 0;
        }
        public int AsInt(PropertyInfo info, object instance, SwoleLogger logger = null)
        {
            if (info == null || instance == null) return 0;
            try { return AsInt((int)info.GetValue(instance)); }
            catch (Exception ex) { LogGetConversionError(logger, ex, typeof(int)); }
            return 0;
        }
        public int AsInt(int value) => MutateInt(value, mutationOp, (int)mutatorValue);

        public bool AsBool(FieldInfo info, object instance, SwoleLogger logger = null)
        {
            if (info == null || instance == null) return false;
            try 
            { 
                if (info.FieldType.IsNumeric()) return AsBool(((double)info.GetValue(instance)).AsBool());

                switch (type)
                {

                    case Type.Double:
                    case Type.Float:
                    case Type.Int:
                        return AsBool(((double)info.GetValue(instance)).AsBool());

                }
                return AsBool((bool)info.GetValue(instance)); 
            }
            catch (Exception ex) { LogGetConversionError(logger, ex, typeof(bool)); }
            return false;
        }
        public bool AsBool(PropertyInfo info, object instance, SwoleLogger logger = null)
        {
            if (info == null || instance == null) return false;
            try
            {
                if (info.PropertyType.IsNumeric()) return AsBool(((double)info.GetValue(instance)).AsBool());

                switch (type)
                {

                    case Type.Double:
                    case Type.Float:
                    case Type.Int:
                        return AsBool(((double)info.GetValue(instance)).AsBool());

                }
                return AsBool((bool)info.GetValue(instance));
            }
            catch (Exception ex) { LogGetConversionError(logger, ex, typeof(bool)); }
            return false;
        }
        public bool AsBool(bool value) => value;

        public string AsString(FieldInfo info, object instance, SwoleLogger logger = null)
        {
            if (info == null || instance == null) return "";
            try { return AsString(info.GetValue(instance).ToString()); }
            catch (Exception ex) { LogGetConversionError(logger, ex, typeof(string)); }
            return "";
        }
        public string AsString(PropertyInfo info, object instance, SwoleLogger logger = null)
        {
            if (info == null || instance == null) return "";
            try { return AsString(info.GetValue(instance).ToString()); }
            catch (Exception ex) { LogGetConversionError(logger, ex, typeof(string)); }
            return "";
        }
        public string AsString(string str) => str;

        public Vector2 AsVector2(FieldInfo info, object instance, SwoleLogger logger = null)
        {
            if (info == null || instance == null) return Vector2.zero;
            try 
            { 
                return AsVector2((Vector2)info.GetValue(instance)); 
            }
            catch (Exception ex) { LogGetConversionError(logger, ex, typeof(Vector2)); }
            return Vector2.zero;
        }
        public Vector2 AsVector2(PropertyInfo info, object instance, SwoleLogger logger = null)
        {
            if (info == null || instance == null) return Vector2.zero;
            try 
            { 
                return AsVector2((Vector2)info.GetValue(instance)); 
            }
            catch (Exception ex) { LogGetConversionError(logger, ex, typeof(Vector2)); }
            return Vector2.zero;
        }
        public Vector2 AsVector2(Vector2 val) => (val + ((Vector2)offsetVector));

        public Vector3 AsVector3(FieldInfo info, object instance, SwoleLogger logger = null)
        {
            if (info == null || instance == null) return Vector3.zero;
            try
            {
                return AsVector3((Vector3)info.GetValue(instance));
            }
            catch (Exception ex) { LogGetConversionError(logger, ex, typeof(Vector3)); }
            return Vector3.zero;
        }
        public Vector3 AsVector3(PropertyInfo info, object instance, SwoleLogger logger = null)
        {
            if (info == null || instance == null) return Vector3.zero;
            try
            {
                return AsVector3((Vector3)info.GetValue(instance));
            }
            catch (Exception ex) { LogGetConversionError(logger, ex, typeof(Vector3)); }
            return Vector3.zero;
        }
        public Vector3 AsVector3(Vector3 val)
        {
            switch (type)
            {

                case Type.Quaternion:
                case Type.RotationLocal:
                case Type.RotationRoot:
                case Type.RotationWorld:
                case Type.EulerAngles:
                case Type.RotationEulerLocal:
                case Type.RotationEulerRoot:
                case Type.RotationEulerWorld:
                    return AsQuaternion(val).EulerAngles;

            }

            return val + (Vector3)offsetVector;
        }

        public Vector4 AsVector4(FieldInfo info, object instance, SwoleLogger logger = null)
        {
            if (info == null || instance == null) return Vector4.zero;
            try
            {
                return AsVector4(((Vector4)info.GetValue(instance)));
            }
            catch (Exception ex) { LogGetConversionError(logger, ex, typeof(Vector4)); }
            return Vector4.zero;
        }
        public Vector4 AsVector4(PropertyInfo info, object instance, SwoleLogger logger = null)
        {
            if (info == null || instance == null) return Vector4.zero;
            try 
            { 
                return AsVector4(((Vector4)info.GetValue(instance))); 
            }
            catch (Exception ex) { LogGetConversionError(logger, ex, typeof(Vector4)); }
            return Vector4.zero;
        }
        public Vector4 AsVector4(Vector4 val)
        {
            switch (type)
            {
                case Type.Quaternion:
                case Type.RotationLocal:
                case Type.RotationRoot:
                case Type.RotationWorld:
                case Type.EulerAngles:
                case Type.RotationEulerLocal:
                case Type.RotationEulerRoot:
                case Type.RotationEulerWorld:
                    return AsQuaternion(val);

            }

            return val + offsetVector;
        }

        public Quaternion AsQuaternion(FieldInfo info, object instance, SwoleLogger logger = null)
        {
            if (info == null || instance == null) return Quaternion.identity;
            try 
            { 
                return AsQuaternion((Vector4)info.GetValue(instance)); 
            }
            catch (Exception ex) { LogGetConversionError(logger, ex, typeof(Quaternion)); }
            return Quaternion.identity;
        }
        public Quaternion AsQuaternion(PropertyInfo info, object instance, SwoleLogger logger = null)
        {
            if (info == null || instance == null) return Quaternion.identity;
            try 
            { 
                return AsQuaternion((Vector4)info.GetValue(instance)); 
            }
            catch (Exception ex) { LogGetConversionError(logger, ex, typeof(Quaternion)); }
            return Quaternion.identity;
        }
        public Quaternion AsQuaternion(Vector4 val)
        {
            switch (type)
            {

                default:
                    return MutateQuaternion(val, offsetVector);

                case Type.EulerAngles:
                case Type.RotationEulerLocal:
                case Type.RotationEulerRoot:
                case Type.RotationEulerWorld:
                    return MutateEulerAsQuaternion((Vector3)val, offsetVector);

            }
        }

        public Matrix4x4 AsMatrix4x4(FieldInfo info, object instance, SwoleLogger logger = null)
        {
            if (info == null || instance == null) return Matrix4x4.identity;
            try
            {
                return AsMatrix4x4((Matrix4x4)info.GetValue(instance));
            }
            catch (Exception ex) { LogGetConversionError(logger, ex, typeof(Matrix4x4)); }
            return Matrix4x4.identity;
        }
        public Matrix4x4 AsMatrix4x4(PropertyInfo info, object instance, SwoleLogger logger = null)
        {
            if (info == null || instance == null) return Matrix4x4.identity;
            try
            {
                return AsMatrix4x4((Matrix4x4)info.GetValue(instance));
            }
            catch (Exception ex) { LogGetConversionError(logger, ex, typeof(Matrix4x4)); }
            return Matrix4x4.identity;
        }
        public Matrix4x4 AsMatrix4x4(Matrix4x4 val) => val;

    }

}
