#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;

using Miniscript;

using Swole.Script;
namespace Swole.API.Unity
{

    [Serializable]
    public class CreationRuntimeParameter : IVar
    {

        #region Conversions
        public static double ConvertToDouble(object val) => (double)val;
        public static object ConvertFromDouble(double val) => val;

        public static float ConvertToFloat(object val) => (float)val;
        public static object ConvertFromFloat(float val) => val;

        public static int ConvertToInt(object val) => (int)val;
        public static object ConvertFromInt(int val) => val;

        public static bool ConvertToBool(object val) 
        {
            if (val is bool b) return b; else if (val.GetType().IsNumeric()) return ((double)val) != 0;
            return false;
        }
        public static object ConvertFromBool(bool val) => val;

        public static string ConvertToString(object val)
        {
            if (val is string s) return s; else if (val != null) return val.ToString();
            return string.Empty;
        }
        public static object ConvertFromString(string val) => val;

        public static EngineInternal.Vector2 ConvertToVector2(object val)
        {
            if (val is EngineInternal.Vector2 v2) return v2;
            if (val is EngineInternal.Vector3 v3) return v3;
            if (val is EngineInternal.Vector4 v4) return v4;
            if (val is UnityEngine.Vector2 uv2) return UnityEngineHook.AsSwoleVector(uv2);
            if (val is UnityEngine.Vector3 uv3) return UnityEngineHook.AsSwoleVector(uv3);
            if (val is UnityEngine.Vector4 uv4) return UnityEngineHook.AsSwoleVector(uv4);

            return EngineInternal.Vector2.zero;
        }
        public static object ConvertFromVector2(EngineInternal.Vector2 val) => val;
        public static object ConvertFromAsUnityVector2(EngineInternal.Vector2 val) => UnityEngineHook.AsUnityVector(val);

        public static EngineInternal.Vector3 ConvertToVector3(object val)
        {
            if (val is EngineInternal.Vector2 v2) return v2;
            if (val is EngineInternal.Vector3 v3) return v3;
            if (val is EngineInternal.Vector4 v4) return v4;
            if (val is UnityEngine.Vector2 uv2) return UnityEngineHook.AsSwoleVector(uv2);
            if (val is UnityEngine.Vector3 uv3) return UnityEngineHook.AsSwoleVector(uv3);
            if (val is UnityEngine.Vector4 uv4) return UnityEngineHook.AsSwoleVector(uv4);

            return EngineInternal.Vector3.zero;
        }
        public static object ConvertFromVector3(EngineInternal.Vector3 val) => val;
        public static object ConvertFromAsUnityVector3(EngineInternal.Vector3 val) => UnityEngineHook.AsUnityVector(val);

        public static EngineInternal.Vector4 ConvertToVector4(object val)
        {
            if (val is EngineInternal.Vector2 v2) return v2;
            if (val is EngineInternal.Vector3 v3) return v3;
            if (val is EngineInternal.Vector4 v4) return v4;
            if (val is EngineInternal.Quaternion q) return q;
            if (val is UnityEngine.Vector2 uv2) return UnityEngineHook.AsSwoleVector(uv2);
            if (val is UnityEngine.Vector3 uv3) return UnityEngineHook.AsSwoleVector(uv3);
            if (val is UnityEngine.Vector4 uv4) return UnityEngineHook.AsSwoleVector(uv4);
            if (val is UnityEngine.Quaternion uq) return UnityEngineHook.AsSwoleQuaternion(uq);

            return EngineInternal.Vector4.zero;
        }
        public static object ConvertFromVector4(EngineInternal.Vector4 val) => val;
        public static object ConvertFromAsUnityVector4(EngineInternal.Vector4 val) => UnityEngineHook.AsUnityVector(val);

        public static EngineInternal.Quaternion ConvertToQuaternion(object val)
        {
            if (val is EngineInternal.Vector4 v4) return v4;
            if (val is EngineInternal.Quaternion q) return q;
            if (val is UnityEngine.Vector4 uv4) return UnityEngineHook.AsSwoleVector(uv4);
            if (val is UnityEngine.Quaternion uq) return UnityEngineHook.AsSwoleQuaternion(uq);

            return EngineInternal.Quaternion.identity;
        }
        public static object ConvertFromQuaternion(EngineInternal.Quaternion val) => val;
        public static object ConvertFromAsUnityQuaternion(EngineInternal.Quaternion val) => UnityEngineHook.AsUnityQuaternion(val);

        public static EngineInternal.Matrix4x4 ConvertToMatrix4x4(object val)
        {
            if (val is EngineInternal.Matrix4x4 m) return m;
            if (val is UnityEngine.Matrix4x4 um) return UnityEngineHook.AsSwoleMatrix(um);

            return EngineInternal.Matrix4x4.identity;
        }
        public static object ConvertFromMatrix4x4(EngineInternal.Matrix4x4 val) => val;
        public static object ConvertFromAsUnityMatrix4x4(EngineInternal.Matrix4x4 val) => UnityEngineHook.AsUnityMatrix(val);
        #endregion

        protected readonly CreationParameter baseParameter;
        public CreationParameter BaseParameter => baseParameter;

        protected readonly IVar variable;
        public IVar Variable => variable;

        public class ValueContainer<T>
        {

            public T value;

            public ValueContainer(T value)
            {
                this.value = value;
            }

        }

        public CreationRuntimeParameter(CreationParameter baseParameter, CreationBehaviour creationInstance = null, SwoleLogger startupLogger = null, bool optimizeReferenceChain = true)
        {

            this.baseParameter = baseParameter;

            if (!string.IsNullOrEmpty(baseParameter.objectLink))
            {

                if (creationInstance != null)
                {

                    EngineInternal.Transform rootTransform = new EngineInternal.Transform(creationInstance.transform);

                    List<string> memberNameChain = new List<string>();

                    GameObject unityObject = creationInstance.FindUnityObject(baseParameter.objectLink);

                    if (unityObject != null)
                    {

                        EngineInternal.Transform swoleTransform = new EngineInternal.Transform(unityObject.transform);
                        
                        object linkedObj = unityObject;
                        System.Type linkedType = linkedObj.GetType();

                        bool invalid = false;
                        if (!string.IsNullOrEmpty(baseParameter.memberLink))
                        {

                            string[] memberPath = baseParameter.memberLink.Split('.');
                            for (int a = 0; a < memberPath.Length; a++)
                            {
                                string memberName = memberPath[a];
                                if (string.IsNullOrEmpty(memberName))
                                {
                                    invalid = true;
                                    break;
                                }
                                MemberInfo memberInfo = linkedType.GetField(memberName);
                                if (memberInfo == null) memberInfo = linkedType.GetProperty(memberName);
                                if (memberInfo != null)
                                {
                                    if (memberInfo is FieldInfo fieldInfo)
                                    {
                                        linkedType = fieldInfo.FieldType;
                                        if (!optimizeReferenceChain || linkedType.IsValueType || !typeof(UnityEngine.Object).IsAssignableFrom(linkedType))
                                        {
                                            memberNameChain.Add(memberName);
                                        }
                                        else
                                        {
                                            memberNameChain.Clear();
                                            linkedObj = (UnityEngine.Object)fieldInfo.GetValue(linkedObj);
                                        }
                                    }
                                    else if (memberInfo is PropertyInfo propertyInfo)
                                    {
                                        linkedType = propertyInfo.PropertyType;
                                        if (!optimizeReferenceChain || linkedType.IsValueType || !typeof(UnityEngine.Object).IsAssignableFrom(linkedType))
                                        {
                                            memberNameChain.Add(memberName);
                                        }
                                        else
                                        {
                                            memberNameChain.Clear();
                                            linkedObj = (UnityEngine.Object)propertyInfo.GetValue(linkedObj);
                                        }
                                    }
                                    else
                                    {
                                        invalid = true;
                                        break;
                                    }
                                }
                                else
                                {
                                    invalid = true;
                                    break;
                                }

                            }

                        } 
                        else
                        {

                            switch(baseParameter.type)
                            {

                                case CreationParameter.Type.Double:
                                    linkedObj = new ValueContainer<double>(baseParameter.defaultValue);
                                    linkedType = typeof(double);
                                    memberNameChain.Add("value");
                                    break;

                                case CreationParameter.Type.Float:
                                    linkedObj = new ValueContainer<float>((float)baseParameter.defaultValue);
                                    linkedType = typeof(float);
                                    memberNameChain.Add("value");
                                    break;

                                case CreationParameter.Type.Int:
                                    linkedObj = new ValueContainer<int>((int)baseParameter.defaultValue);
                                    linkedType = typeof(int);
                                    memberNameChain.Add("value");
                                    break;

                                case CreationParameter.Type.Bool:
                                    linkedObj = new ValueContainer<bool>(baseParameter.defaultValue != 0);
                                    linkedType = typeof(bool);
                                    memberNameChain.Add("value");
                                    break;

                                case CreationParameter.Type.String:
                                    linkedObj = new ValueContainer<string>(baseParameter.defaultString);
                                    linkedType = typeof(string);
                                    memberNameChain.Add("value");
                                    break;

                                case CreationParameter.Type.Vector2:
                                    linkedObj = new ValueContainer<EngineInternal.Vector2>(baseParameter.defaultVector);
                                    linkedType = typeof(EngineInternal.Vector2);
                                    memberNameChain.Add("value");
                                    break;

                                case CreationParameter.Type.Vector3:
                                case CreationParameter.Type.PositionLocal:
                                case CreationParameter.Type.PositionRoot:
                                case CreationParameter.Type.PositionWorld:
                                case CreationParameter.Type.DirectionLocal:
                                case CreationParameter.Type.DirectionRoot:
                                case CreationParameter.Type.DirectionWorld:
                                case CreationParameter.Type.EulerAngles:
                                case CreationParameter.Type.RotationEulerLocal:
                                case CreationParameter.Type.RotationEulerRoot:
                                case CreationParameter.Type.RotationEulerWorld:
                                    linkedObj = new ValueContainer<EngineInternal.Vector3>(baseParameter.defaultVector);
                                    linkedType = typeof(EngineInternal.Vector3);
                                    memberNameChain.Add("value");
                                    break;

                                case CreationParameter.Type.Vector4:
                                case CreationParameter.Type.TangentLocal:
                                case CreationParameter.Type.TangentRoot:
                                case CreationParameter.Type.TangentWorld:
                                    linkedObj = new ValueContainer<EngineInternal.Vector4>(baseParameter.defaultVector);
                                    linkedType = typeof(EngineInternal.Vector4);
                                    memberNameChain.Add("value");
                                    break;

                                case CreationParameter.Type.Quaternion:
                                case CreationParameter.Type.RotationLocal:
                                case CreationParameter.Type.RotationRoot:
                                case CreationParameter.Type.RotationWorld:
                                    linkedObj = new ValueContainer<EngineInternal.Quaternion>(baseParameter.defaultVector);
                                    linkedType = typeof(EngineInternal.Quaternion);
                                    memberNameChain.Add("value");
                                    break;

                                case CreationParameter.Type.Matrix4x4:
                                case CreationParameter.Type.LocalToRoot:
                                case CreationParameter.Type.LocalToWorld:
                                case CreationParameter.Type.RootToLocal:
                                case CreationParameter.Type.WorldToLocal:
                                    linkedObj = new ValueContainer<EngineInternal.Matrix4x4>(baseParameter.defaultMatrix);
                                    linkedType = typeof(EngineInternal.Matrix4x4);
                                    memberNameChain.Add("value");
                                    break;

                            }
                             
                            if (memberNameChain.Count <= 0) 
                            {
                                startupLogger?.LogError($"Unable to link creation parameter '{baseParameter.name}' with child object '{baseParameter.objectLink}' as type: '{baseParameter.type.ToString()}'!");
                                return;
                            }

                        }

                        if (invalid)
                        {
                            startupLogger?.LogError($"Unable to link creation parameter '{baseParameter.name}' with child object '{baseParameter.objectLink}' at invalid member path: '{baseParameter.memberLink}'!");
                        }
                        else
                        {

                            if (linkedObj != null)
                            {

                                bool isUnityType = UnityEngineHook.IsUnityType(linkedType);

                                SwoleVarLinkedInstance<EngineInternal.Vector2>.ConvertFromDelegate convertBackV2 = isUnityType ? ConvertFromAsUnityVector2 : ConvertFromVector2;
                                SwoleVarLinkedInstance<EngineInternal.Vector3>.ConvertFromDelegate convertBackV3 = isUnityType ? ConvertFromAsUnityVector3 : ConvertFromVector3;
                                SwoleVarLinkedInstance<EngineInternal.Vector4>.ConvertFromDelegate convertBackV4 = isUnityType ? ConvertFromAsUnityVector4 : ConvertFromVector4;
                                SwoleVarLinkedInstance<EngineInternal.Quaternion>.ConvertFromDelegate convertBackQ = isUnityType ? ConvertFromAsUnityQuaternion : ConvertFromQuaternion;
                                SwoleVarLinkedInstance<EngineInternal.Matrix4x4>.ConvertFromDelegate convertBackMatrix = isUnityType ? ConvertFromAsUnityMatrix4x4 : ConvertFromMatrix4x4;

                                switch (baseParameter.type) // Handle auto-conversion of type (and space for certain parameters). An example would be a user-defined position that they want to stay relative to the creation's root transform.
                                { 

                                    case CreationParameter.Type.Double:
                                        variable = new SwoleVarLinkedInstance<double>(baseParameter.name, linkedObj, memberNameChain, (object val) => baseParameter.AsDouble(ConvertToDouble(val)), ConvertFromDouble);
                                        break;
                                    case CreationParameter.Type.Float:
                                        variable = new SwoleVarLinkedInstance<float>(baseParameter.name, linkedObj, memberNameChain, (object val) => baseParameter.AsFloat(ConvertToFloat(val)), ConvertFromFloat);
                                        break;
                                    case CreationParameter.Type.Int:
                                        variable = new SwoleVarLinkedInstance<int>(baseParameter.name, linkedObj, memberNameChain, (object val) => baseParameter.AsInt(ConvertToInt(val)), ConvertFromInt);
                                        break;
                                    case CreationParameter.Type.Bool:
                                        variable = new SwoleVarLinkedInstance<bool>(baseParameter.name, linkedObj, memberNameChain, (object val) => baseParameter.AsBool(ConvertToBool(val)), ConvertFromBool);
                                        break;
                                    case CreationParameter.Type.String:
                                        variable = new SwoleVarLinkedInstance<string>(baseParameter.name, linkedObj, memberNameChain, (object val) => baseParameter.AsString(ConvertToString(val)), ConvertFromString);
                                        break;
                                    case CreationParameter.Type.Vector2:
                                        variable = new SwoleVarLinkedInstance<EngineInternal.Vector2>(baseParameter.name, linkedObj, memberNameChain, (object val) => baseParameter.AsVector2(ConvertToVector2(val)), convertBackV2);
                                        break;
                                    case CreationParameter.Type.Vector3:
                                    case CreationParameter.Type.PositionLocal:
                                    case CreationParameter.Type.PositionRoot:
                                    case CreationParameter.Type.PositionWorld:
                                    case CreationParameter.Type.DirectionLocal:
                                    case CreationParameter.Type.DirectionRoot:
                                    case CreationParameter.Type.DirectionWorld:
                                    case CreationParameter.Type.EulerAngles:
                                    case CreationParameter.Type.RotationEulerLocal:
                                    case CreationParameter.Type.RotationEulerRoot:
                                    case CreationParameter.Type.RotationEulerWorld:
                                        if (baseParameter.conversionMethod == CreationParameter.ConversionMethod.None)
                                        {
                                            variable = new SwoleVarLinkedInstance<EngineInternal.Vector3>(baseParameter.name, linkedObj, memberNameChain, (object val) => baseParameter.AsVector3(ConvertToVector3(val)), convertBackV3);
                                        }
                                        else if (baseParameter.type == CreationParameter.Type.EulerAngles || baseParameter.type == CreationParameter.Type.RotationEulerLocal || baseParameter.type == CreationParameter.Type.RotationEulerRoot || baseParameter.type == CreationParameter.Type.RotationEulerWorld) 
                                        {
                                            switch (baseParameter.conversionMethod)
                                            {

                                                case CreationParameter.ConversionMethod.LocalToWorld:
                                                    
                                                    variable = new SwoleVarLinkedInstance<EngineInternal.Vector3>(baseParameter.name, linkedObj, memberNameChain,
                                                        (object val) => (swoleTransform.rotation * EngineInternal.Quaternion.Euler(baseParameter.AsVector3(ConvertToVector3(val)))).EulerAngles,
                                                        (EngineInternal.Vector3 v) => convertBackV3((swoleTransform.rotation.inverse * EngineInternal.Quaternion.Euler(v)).EulerAngles));

                                                    break;
                                                case CreationParameter.ConversionMethod.LocalToRoot:
                                                    
                                                    variable = new SwoleVarLinkedInstance<EngineInternal.Vector3>(baseParameter.name, linkedObj, memberNameChain,
                                                        (object val) => (rootTransform.rotation.inverse * (swoleTransform.rotation * EngineInternal.Quaternion.Euler(baseParameter.AsVector3(ConvertToVector3(val))))).EulerAngles,
                                                        (EngineInternal.Vector3 v) => convertBackV3((swoleTransform.rotation.inverse * (rootTransform.rotation * EngineInternal.Quaternion.Euler(v))).EulerAngles));

                                                    break;
                                                case CreationParameter.ConversionMethod.WorldToLocal:

                                                    variable = new SwoleVarLinkedInstance<EngineInternal.Vector3>(baseParameter.name, linkedObj, memberNameChain,
                                                        (object val) => (swoleTransform.rotation.inverse * EngineInternal.Quaternion.Euler(baseParameter.AsVector3(ConvertToVector3(val)))).EulerAngles,
                                                        (EngineInternal.Vector3 v) => convertBackV3((swoleTransform.rotation * EngineInternal.Quaternion.Euler(v)).EulerAngles));

                                                    break;
                                                case CreationParameter.ConversionMethod.RootToLocal:

                                                    variable = new SwoleVarLinkedInstance<EngineInternal.Vector3>(baseParameter.name, linkedObj, memberNameChain,
                                                        (object val) => (swoleTransform.rotation.inverse * (rootTransform.rotation * EngineInternal.Quaternion.Euler(baseParameter.AsVector3(ConvertToVector3(val))))).EulerAngles,
                                                        (EngineInternal.Vector3 v) => convertBackV3((rootTransform.rotation.inverse * (swoleTransform.rotation * EngineInternal.Quaternion.Euler(v))).EulerAngles));

                                                    break;
                                                case CreationParameter.ConversionMethod.RootToWorld:

                                                    variable = new SwoleVarLinkedInstance<EngineInternal.Vector3>(baseParameter.name, linkedObj, memberNameChain,
                                                        (object val) => (rootTransform.rotation * EngineInternal.Quaternion.Euler(baseParameter.AsVector3(ConvertToVector3(val)))).EulerAngles,
                                                        (EngineInternal.Vector3 v) => convertBackV3((rootTransform.rotation.inverse * EngineInternal.Quaternion.Euler(v)).EulerAngles));

                                                    break;
                                                case CreationParameter.ConversionMethod.WorldToRoot:

                                                    variable = new SwoleVarLinkedInstance<EngineInternal.Vector3>(baseParameter.name, linkedObj, memberNameChain,
                                                        (object val) => (rootTransform.rotation.inverse * EngineInternal.Quaternion.Euler(baseParameter.AsVector3(ConvertToVector3(val)))).EulerAngles,
                                                        (EngineInternal.Vector3 v) => convertBackV3((rootTransform.rotation * EngineInternal.Quaternion.Euler(v)).EulerAngles));

                                                    break;
                                            }
                                        }
                                        else if (baseParameter.type == CreationParameter.Type.PositionLocal || baseParameter.type == CreationParameter.Type.PositionRoot || baseParameter.type == CreationParameter.Type.PositionWorld)
                                        {
                                            switch (baseParameter.conversionMethod)
                                            {

                                                case CreationParameter.ConversionMethod.LocalToWorld:

                                                    variable = new SwoleVarLinkedInstance<EngineInternal.Vector3>(baseParameter.name, linkedObj, memberNameChain,
                                                        (object val) => swoleTransform.TransformPoint(baseParameter.AsVector3(ConvertToVector3(val))),
                                                        (EngineInternal.Vector3 v) => convertBackV3(swoleTransform.InverseTransformPoint(v)));

                                                    break;
                                                case CreationParameter.ConversionMethod.LocalToRoot:

                                                    variable = new SwoleVarLinkedInstance<EngineInternal.Vector3>(baseParameter.name, linkedObj, memberNameChain,
                                                        (object val) => rootTransform.InverseTransformPoint(swoleTransform.TransformPoint(baseParameter.AsVector3(ConvertToVector3(val)))),
                                                        (EngineInternal.Vector3 v) => convertBackV3(swoleTransform.InverseTransformPoint(rootTransform.TransformPoint(v))));

                                                    break;
                                                case CreationParameter.ConversionMethod.WorldToLocal:

                                                    variable = new SwoleVarLinkedInstance<EngineInternal.Vector3>(baseParameter.name, linkedObj, memberNameChain,
                                                        (object val) => swoleTransform.InverseTransformPoint(baseParameter.AsVector3(ConvertToVector3(val))),
                                                        (EngineInternal.Vector3 v) => convertBackV3(swoleTransform.TransformPoint(v)));

                                                    break;
                                                case CreationParameter.ConversionMethod.RootToLocal:

                                                    variable = new SwoleVarLinkedInstance<EngineInternal.Vector3>(baseParameter.name, linkedObj, memberNameChain,
                                                        (object val) => swoleTransform.InverseTransformPoint(rootTransform.TransformPoint(baseParameter.AsVector3(ConvertToVector3(val)))),
                                                        (EngineInternal.Vector3 v) => convertBackV3(rootTransform.InverseTransformPoint(swoleTransform.TransformPoint(v))));

                                                    break;
                                                case CreationParameter.ConversionMethod.RootToWorld:

                                                    variable = new SwoleVarLinkedInstance<EngineInternal.Vector3>(baseParameter.name, linkedObj, memberNameChain,
                                                        (object val) => rootTransform.TransformPoint(baseParameter.AsVector3(ConvertToVector3(val))),
                                                        (EngineInternal.Vector3 v) => convertBackV3(rootTransform.InverseTransformPoint(v)));

                                                    break;
                                                case CreationParameter.ConversionMethod.WorldToRoot:

                                                    variable = new SwoleVarLinkedInstance<EngineInternal.Vector3>(baseParameter.name, linkedObj, memberNameChain,
                                                        (object val) => rootTransform.InverseTransformPoint(baseParameter.AsVector3(ConvertToVector3(val))),
                                                        (EngineInternal.Vector3 v) => convertBackV3(rootTransform.TransformPoint(v)));

                                                    break;
                                            }
                                        }
                                        else
                                        {
                                            switch (baseParameter.conversionMethod)
                                            {

                                                case CreationParameter.ConversionMethod.LocalToWorld:

                                                    variable = new SwoleVarLinkedInstance<EngineInternal.Vector3>(baseParameter.name, linkedObj, memberNameChain,
                                                        (object val) => swoleTransform.TransformVector(baseParameter.AsVector3(ConvertToVector3(val))),
                                                        (EngineInternal.Vector3 v) => convertBackV3(swoleTransform.InverseTransformVector(v)));

                                                    break;
                                                case CreationParameter.ConversionMethod.LocalToRoot:

                                                    variable = new SwoleVarLinkedInstance<EngineInternal.Vector3>(baseParameter.name, linkedObj, memberNameChain,
                                                        (object val) => rootTransform.InverseTransformVector(swoleTransform.TransformVector(baseParameter.AsVector3(ConvertToVector3(val)))),
                                                        (EngineInternal.Vector3 v) => convertBackV3(swoleTransform.InverseTransformVector(rootTransform.TransformVector(v))));

                                                    break;
                                                case CreationParameter.ConversionMethod.WorldToLocal:

                                                    variable = new SwoleVarLinkedInstance<EngineInternal.Vector3>(baseParameter.name, linkedObj, memberNameChain,
                                                        (object val) => swoleTransform.InverseTransformVector(baseParameter.AsVector3(ConvertToVector3(val))),
                                                        (EngineInternal.Vector3 v) => convertBackV3(swoleTransform.TransformVector(v)));

                                                    break;
                                                case CreationParameter.ConversionMethod.RootToLocal:

                                                    variable = new SwoleVarLinkedInstance<EngineInternal.Vector3>(baseParameter.name, linkedObj, memberNameChain,
                                                        (object val) => swoleTransform.InverseTransformVector(rootTransform.TransformVector(baseParameter.AsVector3(ConvertToVector3(val)))),
                                                        (EngineInternal.Vector3 v) => convertBackV3(rootTransform.TransformVector(swoleTransform.TransformVector(v))));

                                                    break;
                                                case CreationParameter.ConversionMethod.RootToWorld:

                                                    variable = new SwoleVarLinkedInstance<EngineInternal.Vector3>(baseParameter.name, linkedObj, memberNameChain,
                                                        (object val) => rootTransform.TransformVector(baseParameter.AsVector3(ConvertToVector3(val))),
                                                        (EngineInternal.Vector3 v) => convertBackV3(rootTransform.InverseTransformVector(v)));

                                                    break;
                                                case CreationParameter.ConversionMethod.WorldToRoot:

                                                    variable = new SwoleVarLinkedInstance<EngineInternal.Vector3>(baseParameter.name, linkedObj, memberNameChain,
                                                        (object val) => rootTransform.InverseTransformVector(baseParameter.AsVector3(ConvertToVector3(val))),
                                                        (EngineInternal.Vector3 v) => convertBackV3(rootTransform.TransformVector(v)));

                                                    break;
                                            }
                                        }
                                        break;
                                    case CreationParameter.Type.Vector4:
                                    case CreationParameter.Type.TangentLocal:
                                    case CreationParameter.Type.TangentRoot:
                                    case CreationParameter.Type.TangentWorld:
                                        if (baseParameter.conversionMethod == CreationParameter.ConversionMethod.None || baseParameter.type == CreationParameter.Type.Vector4)
                                        {
                                            variable = new SwoleVarLinkedInstance<EngineInternal.Vector4>(baseParameter.name, linkedObj, memberNameChain, (object val) => baseParameter.AsVector4(ConvertToVector4(val)), convertBackV4);
                                        }
                                        else
                                        {
                                            switch (baseParameter.conversionMethod)
                                            {

                                                case CreationParameter.ConversionMethod.LocalToWorld:

                                                    variable = new SwoleVarLinkedInstance<EngineInternal.Vector4>(baseParameter.name, linkedObj, memberNameChain,

                                                        (object val) => {

                                                            var v4 = baseParameter.AsVector4(ConvertToVector4(val));
                                                            var v3 = swoleTransform.TransformVector(v4);
                                                            v4.x = v3.x;
                                                            v4.y = v3.y;
                                                            v4.z = v3.z;
                                                            return v4;

                                                        },
                                                        (EngineInternal.Vector4 v) => {

                                                            var v3 = swoleTransform.InverseTransformVector(v);
                                                            v.x = v3.x;
                                                            v.y = v3.y;
                                                            v.z = v3.z;
                                                            return convertBackV3(v);

                                                            });

                                                    break;
                                                case CreationParameter.ConversionMethod.LocalToRoot:

                                                    variable = new SwoleVarLinkedInstance<EngineInternal.Vector4>(baseParameter.name, linkedObj, memberNameChain,

                                                        (object val) => {

                                                            var v4 = baseParameter.AsVector4(ConvertToVector4(val));
                                                            var v3 = rootTransform.InverseTransformVector(swoleTransform.TransformVector(v4));
                                                            v4.x = v3.x;
                                                            v4.y = v3.y;
                                                            v4.z = v3.z;
                                                            return v4;

                                                        },
                                                        (EngineInternal.Vector4 v) => {

                                                            var v3 = swoleTransform.InverseTransformVector(rootTransform.TransformVector(v));
                                                            v.x = v3.x;
                                                            v.y = v3.y;
                                                            v.z = v3.z;
                                                            return convertBackV4(v);

                                                        });

                                                    break;
                                                case CreationParameter.ConversionMethod.WorldToLocal:

                                                    variable = new SwoleVarLinkedInstance<EngineInternal.Vector4>(baseParameter.name, linkedObj, memberNameChain,

                                                        (object val) => {

                                                            var v4 = baseParameter.AsVector4(ConvertToVector4(val));
                                                            var v3 = swoleTransform.InverseTransformVector(v4);
                                                            v4.x = v3.x;
                                                            v4.y = v3.y;
                                                            v4.z = v3.z;
                                                            return v4;

                                                        },
                                                        (EngineInternal.Vector4 v) => {

                                                            var v3 = swoleTransform.TransformVector(v);
                                                            v.x = v3.x;
                                                            v.y = v3.y;
                                                            v.z = v3.z;
                                                            return convertBackV4(v);

                                                        });

                                                    break;
                                                case CreationParameter.ConversionMethod.RootToLocal:

                                                    variable = new SwoleVarLinkedInstance<EngineInternal.Vector4>(baseParameter.name, linkedObj, memberNameChain,

                                                        (object val) => {

                                                            var v4 = baseParameter.AsVector4(ConvertToVector4(val));
                                                            var v3 = swoleTransform.InverseTransformVector(rootTransform.TransformVector(v4));
                                                            v4.x = v3.x;
                                                            v4.y = v3.y;
                                                            v4.z = v3.z;
                                                            return v4;

                                                        },
                                                        (EngineInternal.Vector4 v) => {

                                                            var v3 = rootTransform.InverseTransformVector(swoleTransform.TransformVector(v));
                                                            v.x = v3.x;
                                                            v.y = v3.y;
                                                            v.z = v3.z;
                                                            return convertBackV4(v);

                                                        });

                                                    break;
                                                case CreationParameter.ConversionMethod.RootToWorld:

                                                    variable = new SwoleVarLinkedInstance<EngineInternal.Vector4>(baseParameter.name, linkedObj, memberNameChain,

                                                        (object val) => {

                                                            var v4 = baseParameter.AsVector4(ConvertToVector4(val));
                                                            var v3 = rootTransform.TransformVector(v4);
                                                            v4.x = v3.x;
                                                            v4.y = v3.y;
                                                            v4.z = v3.z;
                                                            return v4;

                                                        },
                                                        (EngineInternal.Vector4 v) => {

                                                            var v3 = rootTransform.InverseTransformVector(v);
                                                            v.x = v3.x;
                                                            v.y = v3.y;
                                                            v.z = v3.z;
                                                            return convertBackV4(v);

                                                        });

                                                    break;
                                                case CreationParameter.ConversionMethod.WorldToRoot:

                                                    variable = new SwoleVarLinkedInstance<EngineInternal.Vector4>(baseParameter.name, linkedObj, memberNameChain,

                                                        (object val) => {

                                                            var v4 = baseParameter.AsVector4(ConvertToVector4(val));
                                                            var v3 = rootTransform.InverseTransformVector(v4);
                                                            v4.x = v3.x;
                                                            v4.y = v3.y;
                                                            v4.z = v3.z;
                                                            return v4;

                                                        },
                                                        (EngineInternal.Vector4 v) => {

                                                            var v3 = swoleTransform.TransformVector(v);
                                                            v.x = v3.x;
                                                            v.y = v3.y;
                                                            v.z = v3.z;
                                                            return convertBackV4(v);

                                                        });

                                                    break;
                                            }
                                        }
                                        break;
                                    case CreationParameter.Type.Quaternion:
                                    case CreationParameter.Type.RotationLocal:
                                    case CreationParameter.Type.RotationRoot:
                                    case CreationParameter.Type.RotationWorld:
                                        if (baseParameter.conversionMethod == CreationParameter.ConversionMethod.None)
                                        {
                                            variable = new SwoleVarLinkedInstance<EngineInternal.Quaternion>(baseParameter.name, linkedObj, memberNameChain, (object val) => baseParameter.AsQuaternion(ConvertToQuaternion(val)), convertBackQ);
                                        }
                                        else
                                        {

                                            switch (baseParameter.conversionMethod)
                                            {
                                                case CreationParameter.ConversionMethod.LocalToWorld:

                                                    variable = new SwoleVarLinkedInstance<EngineInternal.Quaternion>(baseParameter.name, linkedObj, memberNameChain,
                                                        (object val) => (swoleTransform.rotation * baseParameter.AsQuaternion(ConvertToQuaternion(val))),
                                                        (EngineInternal.Quaternion q) => convertBackQ(swoleTransform.rotation.inverse * q));

                                                    break;
                                                case CreationParameter.ConversionMethod.LocalToRoot:

                                                    variable = new SwoleVarLinkedInstance<EngineInternal.Quaternion>(baseParameter.name, linkedObj, memberNameChain,
                                                        (object val) => (rootTransform.rotation.inverse * (swoleTransform.rotation * baseParameter.AsQuaternion(ConvertToQuaternion(val)))),
                                                        (EngineInternal.Quaternion q) => convertBackQ(swoleTransform.rotation.inverse * (rootTransform.rotation * q)));

                                                    break;
                                                case CreationParameter.ConversionMethod.WorldToLocal:

                                                    variable = new SwoleVarLinkedInstance<EngineInternal.Quaternion>(baseParameter.name, linkedObj, memberNameChain,
                                                        (object val) => (swoleTransform.rotation.inverse * baseParameter.AsQuaternion(ConvertToQuaternion(val))),
                                                        (EngineInternal.Quaternion q) => convertBackQ(swoleTransform.rotation * q));

                                                    break;
                                                case CreationParameter.ConversionMethod.RootToLocal:

                                                    variable = new SwoleVarLinkedInstance<EngineInternal.Quaternion>(baseParameter.name, linkedObj, memberNameChain,
                                                        (object val) => (swoleTransform.rotation.inverse * (rootTransform.rotation * baseParameter.AsQuaternion(ConvertToQuaternion(val)))),
                                                        (EngineInternal.Quaternion q) => convertBackQ(rootTransform.rotation.inverse * (swoleTransform.rotation * q)));

                                                    break;
                                                case CreationParameter.ConversionMethod.RootToWorld:

                                                    variable = new SwoleVarLinkedInstance<EngineInternal.Quaternion>(baseParameter.name, linkedObj, memberNameChain,
                                                        (object val) => (rootTransform.rotation * baseParameter.AsQuaternion(ConvertToQuaternion(val))),
                                                        (EngineInternal.Quaternion q) => convertBackQ(rootTransform.rotation.inverse * q));

                                                    break;
                                                case CreationParameter.ConversionMethod.WorldToRoot:

                                                    variable = new SwoleVarLinkedInstance<EngineInternal.Quaternion>(baseParameter.name, linkedObj, memberNameChain,
                                                        (object val) => (rootTransform.rotation.inverse * baseParameter.AsQuaternion(ConvertToQuaternion(val))),
                                                        (EngineInternal.Quaternion q) => convertBackQ(rootTransform.rotation * q));

                                                    break;
                                            }
                                        } 

                                        break;
                                    case CreationParameter.Type.Matrix4x4:
                                    case CreationParameter.Type.LocalToRoot:
                                    case CreationParameter.Type.LocalToWorld:
                                    case CreationParameter.Type.RootToLocal:
                                    case CreationParameter.Type.WorldToLocal:
                                        variable = new SwoleVarLinkedInstance<EngineInternal.Matrix4x4>(baseParameter.name, linkedObj, memberNameChain, (object val) => baseParameter.AsMatrix4x4(ConvertToMatrix4x4(val)), convertBackMatrix);
                                        break;

                                }
                                if (variable == null)
                                {
                                    startupLogger?.LogWarning($"Unable to link creation parameter '{baseParameter.name}' with child object '{baseParameter.objectLink}' because the type '{(linkedType == null ? "null" : linkedType.Name)}' is not supported!");
                                }
                            } 
                            else
                            {
                                startupLogger?.LogError($"Failed to link creation parameter '{baseParameter.name}' with child object '{baseParameter.objectLink}' at member path: '{baseParameter.memberLink}'! Reason: NullReference");
                            }

                        } 

                    } 
                    else
                    {

                        startupLogger?.LogWarning($"Unable to link creation parameter '{baseParameter.name}' with child object '{baseParameter.objectLink}' because the object could not be found!");

                    }

                } 
                else
                {

                    startupLogger?.LogError($"Unable to link creation parameter '{baseParameter.name}' with child object '{baseParameter.objectLink}' because creationInstance was null!");

                }

            }

        }

        public Value ValueMS => variable == null ? null : variable.ValueMS;

        public string Name => baseParameter.name;

        public object Value { get => (variable == null ? null : variable.Value); set { if (variable != null) variable.Value = value; } }

        public object PreviousValue { get => (variable == null ? null : variable.PreviousValue); }

        public bool ValueChanged { get => (variable == null ? false : variable.ValueChanged); }

        public Type GetValueType() => (variable == null ? null : variable.GetValueType());

    }

}

#endif