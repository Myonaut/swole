#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;

#if SWOLE_ENV
using Miniscript;
#endif

using Swole.Script;
namespace Swole.API.Unity
{

    /// <summary>
    /// An "environment variable" that is local to a creation object instance. It can be referenced from within the creation's swole script runtime.
    /// </summary>
    [Serializable]
    public class CreationRuntimeVariable : IVar
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

        protected readonly CreationVariable baseVariable;
        public CreationVariable BaseVariable => baseVariable;

        protected readonly IVar variable;
        //public IVar Variable => variable; // variable should be interacted with using the IVar interface implemented by this class instead.

        public const string _valueContainerPropertyName = "Value";
        public class ValueContainer<T>
        {

            protected T value;
            public T Value
            {
                get => GetValue();
                set => SetValue(value);
            }

            public virtual T GetValue() => value;
            public virtual void SetValue(T value) => this.value = value;

            public ValueContainer(T value)
            {
                this.value = value;
            }

        }

        /// <summary>
        /// A value driven by the output of a user defined block of Miniscript code.
        /// </summary>
        public class ValueDriver<T> : ValueContainer<T>
        {
            public const float _defaultExecutionTimeOut = 0.01f;

            protected readonly CreationBehaviour creationBehaviour;
            protected readonly ExecutableScript script;
            protected readonly string identity;
            public ValueDriver(string identity, CreationBehaviour creationBehaviour, string source, T startValue = default) : base(startValue)
            {
                this.identity = identity;
                this.creationBehaviour = creationBehaviour;
                this.script = creationBehaviour == null ? null : new ExecutableScript(identity, source, -1, creationBehaviour.Logger);
            }

            public override void SetValue(T value) {}
            public override T GetValue()
            {
                Refresh();
                return base.GetValue();
            }

            /// <summary>
            /// Allows the internal script to be executed on the same frame multiple times.
            /// </summary>
            public bool frameIndependent;

            protected int lastFrame;
            public const string driverValueName = "_driverValue";
#if SWOLE_ENV
            protected Value valueMS;
#endif

            /// <summary>
            /// Refresh the value by executing the internal script.
            /// </summary>
            public virtual void Refresh()
            {
                if (!frameIndependent)
                {
                    int frame = Time.frameCount;
                    if (frame == lastFrame) return;
                    lastFrame = frame;
                }

                if (script == null || creationBehaviour == null) return;
#if SWOLE_ENV
                try
                {
                    valueMS = Scripting.AsValueMS(typeof(T), value, null, valueMS);
                    script.Interpreter.SetGlobalValue(driverValueName, valueMS); 

                    script.ExecuteToCompletion(creationBehaviour.Environment, _defaultExecutionTimeOut, creationBehaviour.Logger, true, false);

                    valueMS = script.Interpreter.GetGlobalValue(driverValueName);

                    value = Scripting.FromValueMS(valueMS).CastAs<T>(); 

                }
                catch(Exception ex)
                {
                    var logger = creationBehaviour.Logger;
                    if (logger == null) logger = swole.DefaultLogger;

                    logger.LogError($"Encountered an exception while refreshing ValueDriver '{identity}' for '{creationBehaviour.Identifier}':");
                    logger.LogError($"{ex.GetType().Name}: {ex.Message}");
                }
#endif
            }
        }

        /// <param name="baseVariable">The variable settings</param>
        /// <param name="creationInstance">If the variable expects an object link or is a driver, a creation instance must be given (it's treated as the root/container object from which to find the object to link to and provides the script runtime environment for drivers)</param>
        /// <param name="startupLogger"></param>
        /// <param name="optimizeReferenceChain">Optimize the reference chain by clearing everything above the last reference type in the chain. E.g. 'MainGameObject/Component/SomeOtherGameObject/Transform/position' becomes 'Transform/position'. Disable this if a middleman reference can change.</param>
        public CreationRuntimeVariable(CreationVariable baseVariable, CreationBehaviour creationInstance = null, SwoleLogger startupLogger = null, bool optimizeReferenceChain = true)
        {

            this.baseVariable = baseVariable;
            bool isDriver = !string.IsNullOrEmpty(baseVariable.driverScript);

            object linkedObj = null;
            System.Type linkedType = null;
            List<string> memberNameChain = null; // Used to retrieve the value using reflection when linkedObj is set and baseVariable.memberLink is not null/empty.
            EngineInternal.Transform swoleTransform = default;
            EngineInternal.Transform rootTransform = creationInstance == null ? default : UnityEngineHook.AsSwoleTransform(creationInstance.transform); // The creation instance transform. Used for space conversions (if any).

            bool CreateValueContainerInstance() // Creates a value container instance to use as the linkedObj. This is done when there is no linked GameObject, or when the linked GameObject has no linked field/property (i.e baseVariable.memberLink is null/empty). It means the variable value will be defined manually by the creation author, or through a driver script.
            {
                linkedObj = null;
                if (isDriver)
                {
                    baseVariable.readOnly = true;
                    if (creationInstance == null)
                    {
                        startupLogger?.LogError($"Unable to set up driver variable '{baseVariable.name}' because creationInstance was null!");
                        return false;
                    }
                }
                if (memberNameChain == null) memberNameChain = new List<string>();
                switch (baseVariable.type)
                {

                    case CreationVariable.Type.Double:
                        linkedObj = isDriver ? new ValueDriver<double>(baseVariable.name, creationInstance, baseVariable.driverScript, baseVariable.defaultValue) : new ValueContainer<double>(baseVariable.defaultValue);
                        linkedType = typeof(double);
                        memberNameChain.Add(_valueContainerPropertyName);
                        break;

                    case CreationVariable.Type.Float:
                        linkedObj = isDriver ? new ValueDriver<float>(baseVariable.name, creationInstance, baseVariable.driverScript, (float)baseVariable.defaultValue) : new ValueContainer<float>((float)baseVariable.defaultValue);
                        linkedType = typeof(float);
                        memberNameChain.Add(_valueContainerPropertyName);
                        break;

                    case CreationVariable.Type.Int:
                        linkedObj = isDriver ? new ValueDriver<int>(baseVariable.name, creationInstance, baseVariable.driverScript, (int)baseVariable.defaultValue) : new ValueContainer<int>((int)baseVariable.defaultValue);
                        linkedType = typeof(int);
                        memberNameChain.Add(_valueContainerPropertyName);
                        break;

                    case CreationVariable.Type.Bool:
                        linkedObj = isDriver ? new ValueDriver<bool>(baseVariable.name, creationInstance, baseVariable.driverScript, baseVariable.defaultValue.AsBool()) : new ValueContainer<bool>(baseVariable.defaultValue.AsBool());
                        linkedType = typeof(bool);
                        memberNameChain.Add(_valueContainerPropertyName);
                        break;

                    case CreationVariable.Type.String:
                        linkedObj = isDriver ? new ValueDriver<string>(baseVariable.name, creationInstance, baseVariable.driverScript, baseVariable.defaultString) : new ValueContainer<string>(baseVariable.defaultString);
                        linkedType = typeof(string);
                        memberNameChain.Add(_valueContainerPropertyName);
                        break;

                    case CreationVariable.Type.Vector2:
                        linkedObj = isDriver ? new ValueDriver<EngineInternal.Vector2>(baseVariable.name, creationInstance, baseVariable.driverScript, baseVariable.defaultVector) : new ValueContainer<EngineInternal.Vector2>(baseVariable.defaultVector);
                        linkedType = typeof(EngineInternal.Vector2);
                        memberNameChain.Add(_valueContainerPropertyName);
                        break;

                    case CreationVariable.Type.Vector3:
                    case CreationVariable.Type.PositionLocal:
                    case CreationVariable.Type.PositionRoot:
                    case CreationVariable.Type.PositionWorld:
                    case CreationVariable.Type.DirectionLocal:
                    case CreationVariable.Type.DirectionRoot:
                    case CreationVariable.Type.DirectionWorld:
                    case CreationVariable.Type.EulerAngles:
                    case CreationVariable.Type.RotationEulerLocal:
                    case CreationVariable.Type.RotationEulerRoot:
                    case CreationVariable.Type.RotationEulerWorld:
                        linkedObj = isDriver ? new ValueDriver<EngineInternal.Vector3>(baseVariable.name, creationInstance, baseVariable.driverScript, baseVariable.defaultVector) : new ValueContainer<EngineInternal.Vector3>(baseVariable.defaultVector);
                        linkedType = typeof(EngineInternal.Vector3);
                        memberNameChain.Add(_valueContainerPropertyName);
                        break;

                    case CreationVariable.Type.Vector4:
                    case CreationVariable.Type.TangentLocal:
                    case CreationVariable.Type.TangentRoot:
                    case CreationVariable.Type.TangentWorld:
                        linkedObj = isDriver ? new ValueDriver<EngineInternal.Vector4>(baseVariable.name, creationInstance, baseVariable.driverScript, baseVariable.defaultVector) : new ValueContainer<EngineInternal.Vector4>(baseVariable.defaultVector);
                        linkedType = typeof(EngineInternal.Vector4);
                        memberNameChain.Add(_valueContainerPropertyName);
                        break;

                    case CreationVariable.Type.Quaternion:
                    case CreationVariable.Type.RotationLocal:
                    case CreationVariable.Type.RotationRoot:
                    case CreationVariable.Type.RotationWorld:
                        linkedObj = isDriver ? new ValueDriver<EngineInternal.Quaternion>(baseVariable.name, creationInstance, baseVariable.driverScript, baseVariable.defaultVector) : new ValueContainer<EngineInternal.Quaternion>(baseVariable.defaultVector);
                        linkedType = typeof(EngineInternal.Quaternion);
                        memberNameChain.Add(_valueContainerPropertyName);
                        break;

                    case CreationVariable.Type.Matrix4x4:
                    case CreationVariable.Type.LocalToRoot:
                    case CreationVariable.Type.LocalToWorld:
                    case CreationVariable.Type.RootToLocal:
                    case CreationVariable.Type.WorldToLocal:
                        linkedObj = isDriver ? new ValueDriver<EngineInternal.Matrix4x4>(baseVariable.name, creationInstance, baseVariable.driverScript, baseVariable.defaultMatrix) : new ValueContainer<EngineInternal.Matrix4x4>(baseVariable.defaultMatrix);
                        linkedType = typeof(EngineInternal.Matrix4x4);
                        memberNameChain.Add(_valueContainerPropertyName);
                        break;

                }

                if (linkedObj == null || memberNameChain.Count <= 0)
                {
                    startupLogger?.LogError($"Unable to create value container for creation variable '{baseVariable.name}'{(string.IsNullOrEmpty(baseVariable.objectLink) ? "" : $" with child object '{baseVariable.objectLink}' ")} because type '{baseVariable.type.ToString()}' is not supported! (Or there was an error)");
                    return false;
                }
                return true;
            }

            IVar CreateValueLinkWithConversions() // Sets up an auto-converting value link for anything that wants to query or change the value. (Example: A Position (Vector3) that converts from Local => World when queried, and from World => Local when set.
            {
                IVar variable = null;
                if (linkedObj != null)
                {

                    bool isUnityType = UnityEngineHook.IsUnityType(linkedType);

                    SwoleVarLinkedInstance<EngineInternal.Vector2>.ConvertFromDelegate convertBackV2 = isUnityType ? ConvertFromAsUnityVector2 : ConvertFromVector2;
                    SwoleVarLinkedInstance<EngineInternal.Vector3>.ConvertFromDelegate convertBackV3 = isUnityType ? ConvertFromAsUnityVector3 : ConvertFromVector3;
                    SwoleVarLinkedInstance<EngineInternal.Vector4>.ConvertFromDelegate convertBackV4 = isUnityType ? ConvertFromAsUnityVector4 : ConvertFromVector4;
                    SwoleVarLinkedInstance<EngineInternal.Quaternion>.ConvertFromDelegate convertBackQ = isUnityType ? ConvertFromAsUnityQuaternion : ConvertFromQuaternion;
                    SwoleVarLinkedInstance<EngineInternal.Matrix4x4>.ConvertFromDelegate convertBackMatrix = isUnityType ? ConvertFromAsUnityMatrix4x4 : ConvertFromMatrix4x4;

                    switch (baseVariable.type) // Handle auto-conversion of type (and space for certain variables).
                                               // An example would be a user-defined position (Vector3) that they want to keep relative to the creation's root transform when converted to World space. It would have [baseVariable.type == CreationVariable.Type.PositionRoot] and [baseVariable.conversionMethod == CreationVariable.ConversionMethod.RootToWorld]
                    {

                        case CreationVariable.Type.Double:
                            variable = new SwoleVarLinkedInstance<double>(baseVariable.name, linkedObj, memberNameChain, (object val) => baseVariable.AsDouble(ConvertToDouble(val)), ConvertFromDouble);
                            break;
                        case CreationVariable.Type.Float:
                            variable = new SwoleVarLinkedInstance<float>(baseVariable.name, linkedObj, memberNameChain, (object val) => baseVariable.AsFloat(ConvertToFloat(val)), ConvertFromFloat);
                            break;
                        case CreationVariable.Type.Int:
                            variable = new SwoleVarLinkedInstance<int>(baseVariable.name, linkedObj, memberNameChain, (object val) => baseVariable.AsInt(ConvertToInt(val)), ConvertFromInt);
                            break;
                        case CreationVariable.Type.Bool:
                            variable = new SwoleVarLinkedInstance<bool>(baseVariable.name, linkedObj, memberNameChain, (object val) => baseVariable.AsBool(ConvertToBool(val)), ConvertFromBool);
                            break;
                        case CreationVariable.Type.String:
                            variable = new SwoleVarLinkedInstance<string>(baseVariable.name, linkedObj, memberNameChain, (object val) => baseVariable.AsString(ConvertToString(val)), ConvertFromString);
                            break;
                        case CreationVariable.Type.Vector2:
                            variable = new SwoleVarLinkedInstance<EngineInternal.Vector2>(baseVariable.name, linkedObj, memberNameChain, (object val) => baseVariable.AsVector2(ConvertToVector2(val)), convertBackV2);
                            break;
                        case CreationVariable.Type.Vector3:
                        case CreationVariable.Type.ScaleLocal:
                        case CreationVariable.Type.PositionLocal:
                        case CreationVariable.Type.PositionRoot:
                        case CreationVariable.Type.PositionWorld:
                        case CreationVariable.Type.DirectionLocal:
                        case CreationVariable.Type.DirectionRoot:
                        case CreationVariable.Type.DirectionWorld:
                        case CreationVariable.Type.EulerAngles:
                        case CreationVariable.Type.RotationEulerLocal:
                        case CreationVariable.Type.RotationEulerRoot:
                        case CreationVariable.Type.RotationEulerWorld:
                            if (baseVariable.conversionMethod == CreationVariable.ConversionMethod.None || baseVariable.type == CreationVariable.Type.ScaleLocal)
                            {
                                variable = new SwoleVarLinkedInstance<EngineInternal.Vector3>(baseVariable.name, linkedObj, memberNameChain, (object val) => baseVariable.AsVector3(ConvertToVector3(val)), convertBackV3);
                            }
                            else if (baseVariable.type == CreationVariable.Type.EulerAngles || baseVariable.type == CreationVariable.Type.RotationEulerLocal || baseVariable.type == CreationVariable.Type.RotationEulerRoot || baseVariable.type == CreationVariable.Type.RotationEulerWorld)
                            {
                                switch (baseVariable.conversionMethod)
                                {

                                    case CreationVariable.ConversionMethod.LocalToWorld:

                                        variable = new SwoleVarLinkedInstance<EngineInternal.Vector3>(baseVariable.name, linkedObj, memberNameChain,
                                            (object val) => (swoleTransform.rotation * EngineInternal.Quaternion.Euler(baseVariable.AsVector3(ConvertToVector3(val)))).EulerAngles,
                                            (EngineInternal.Vector3 v) => convertBackV3((swoleTransform.rotation.inverse * EngineInternal.Quaternion.Euler(v)).EulerAngles));

                                        break;
                                    case CreationVariable.ConversionMethod.LocalToRoot:

                                        variable = new SwoleVarLinkedInstance<EngineInternal.Vector3>(baseVariable.name, linkedObj, memberNameChain,
                                            (object val) => (rootTransform.rotation.inverse * (swoleTransform.rotation * EngineInternal.Quaternion.Euler(baseVariable.AsVector3(ConvertToVector3(val))))).EulerAngles,
                                            (EngineInternal.Vector3 v) => convertBackV3((swoleTransform.rotation.inverse * (rootTransform.rotation * EngineInternal.Quaternion.Euler(v))).EulerAngles));

                                        break;
                                    case CreationVariable.ConversionMethod.WorldToLocal:

                                        variable = new SwoleVarLinkedInstance<EngineInternal.Vector3>(baseVariable.name, linkedObj, memberNameChain,
                                            (object val) => (swoleTransform.rotation.inverse * EngineInternal.Quaternion.Euler(baseVariable.AsVector3(ConvertToVector3(val)))).EulerAngles,
                                            (EngineInternal.Vector3 v) => convertBackV3((swoleTransform.rotation * EngineInternal.Quaternion.Euler(v)).EulerAngles));

                                        break;
                                    case CreationVariable.ConversionMethod.RootToLocal:

                                        variable = new SwoleVarLinkedInstance<EngineInternal.Vector3>(baseVariable.name, linkedObj, memberNameChain,
                                            (object val) => (swoleTransform.rotation.inverse * (rootTransform.rotation * EngineInternal.Quaternion.Euler(baseVariable.AsVector3(ConvertToVector3(val))))).EulerAngles,
                                            (EngineInternal.Vector3 v) => convertBackV3((rootTransform.rotation.inverse * (swoleTransform.rotation * EngineInternal.Quaternion.Euler(v))).EulerAngles));

                                        break;
                                    case CreationVariable.ConversionMethod.RootToWorld:

                                        variable = new SwoleVarLinkedInstance<EngineInternal.Vector3>(baseVariable.name, linkedObj, memberNameChain,
                                            (object val) => (rootTransform.rotation * EngineInternal.Quaternion.Euler(baseVariable.AsVector3(ConvertToVector3(val)))).EulerAngles,
                                            (EngineInternal.Vector3 v) => convertBackV3((rootTransform.rotation.inverse * EngineInternal.Quaternion.Euler(v)).EulerAngles));

                                        break;
                                    case CreationVariable.ConversionMethod.WorldToRoot:

                                        variable = new SwoleVarLinkedInstance<EngineInternal.Vector3>(baseVariable.name, linkedObj, memberNameChain,
                                            (object val) => (rootTransform.rotation.inverse * EngineInternal.Quaternion.Euler(baseVariable.AsVector3(ConvertToVector3(val)))).EulerAngles,
                                            (EngineInternal.Vector3 v) => convertBackV3((rootTransform.rotation * EngineInternal.Quaternion.Euler(v)).EulerAngles));

                                        break;
                                }
                            }
                            else if (baseVariable.type == CreationVariable.Type.PositionLocal || baseVariable.type == CreationVariable.Type.PositionRoot || baseVariable.type == CreationVariable.Type.PositionWorld)
                            {
                                switch (baseVariable.conversionMethod)
                                {

                                    case CreationVariable.ConversionMethod.LocalToWorld:

                                        variable = new SwoleVarLinkedInstance<EngineInternal.Vector3>(baseVariable.name, linkedObj, memberNameChain,
                                            (object val) => swoleTransform.TransformPoint(baseVariable.AsVector3(ConvertToVector3(val))),
                                            (EngineInternal.Vector3 v) => convertBackV3(swoleTransform.InverseTransformPoint(v)));

                                        break;
                                    case CreationVariable.ConversionMethod.LocalToRoot:

                                        variable = new SwoleVarLinkedInstance<EngineInternal.Vector3>(baseVariable.name, linkedObj, memberNameChain,
                                            (object val) => rootTransform.InverseTransformPoint(swoleTransform.TransformPoint(baseVariable.AsVector3(ConvertToVector3(val)))),
                                            (EngineInternal.Vector3 v) => convertBackV3(swoleTransform.InverseTransformPoint(rootTransform.TransformPoint(v))));

                                        break;
                                    case CreationVariable.ConversionMethod.WorldToLocal:

                                        variable = new SwoleVarLinkedInstance<EngineInternal.Vector3>(baseVariable.name, linkedObj, memberNameChain,
                                            (object val) => swoleTransform.InverseTransformPoint(baseVariable.AsVector3(ConvertToVector3(val))),
                                            (EngineInternal.Vector3 v) => convertBackV3(swoleTransform.TransformPoint(v)));

                                        break;
                                    case CreationVariable.ConversionMethod.RootToLocal:

                                        variable = new SwoleVarLinkedInstance<EngineInternal.Vector3>(baseVariable.name, linkedObj, memberNameChain,
                                            (object val) => swoleTransform.InverseTransformPoint(rootTransform.TransformPoint(baseVariable.AsVector3(ConvertToVector3(val)))),
                                            (EngineInternal.Vector3 v) => convertBackV3(rootTransform.InverseTransformPoint(swoleTransform.TransformPoint(v))));

                                        break;
                                    case CreationVariable.ConversionMethod.RootToWorld:

                                        variable = new SwoleVarLinkedInstance<EngineInternal.Vector3>(baseVariable.name, linkedObj, memberNameChain,
                                            (object val) => rootTransform.TransformPoint(baseVariable.AsVector3(ConvertToVector3(val))),
                                            (EngineInternal.Vector3 v) => convertBackV3(rootTransform.InverseTransformPoint(v)));

                                        break;
                                    case CreationVariable.ConversionMethod.WorldToRoot:

                                        variable = new SwoleVarLinkedInstance<EngineInternal.Vector3>(baseVariable.name, linkedObj, memberNameChain,
                                            (object val) => rootTransform.InverseTransformPoint(baseVariable.AsVector3(ConvertToVector3(val))),
                                            (EngineInternal.Vector3 v) => convertBackV3(rootTransform.TransformPoint(v)));

                                        break;
                                }
                            }
                            else // Is probably a direction or generic Vector3
                            {
                                switch (baseVariable.conversionMethod)
                                {

                                    case CreationVariable.ConversionMethod.LocalToWorld:

                                        variable = new SwoleVarLinkedInstance<EngineInternal.Vector3>(baseVariable.name, linkedObj, memberNameChain,
                                            (object val) => swoleTransform.TransformVector(baseVariable.AsVector3(ConvertToVector3(val))),
                                            (EngineInternal.Vector3 v) => convertBackV3(swoleTransform.InverseTransformVector(v)));

                                        break;
                                    case CreationVariable.ConversionMethod.LocalToRoot:

                                        variable = new SwoleVarLinkedInstance<EngineInternal.Vector3>(baseVariable.name, linkedObj, memberNameChain,
                                            (object val) => rootTransform.InverseTransformVector(swoleTransform.TransformVector(baseVariable.AsVector3(ConvertToVector3(val)))),
                                            (EngineInternal.Vector3 v) => convertBackV3(swoleTransform.InverseTransformVector(rootTransform.TransformVector(v))));

                                        break;
                                    case CreationVariable.ConversionMethod.WorldToLocal:

                                        variable = new SwoleVarLinkedInstance<EngineInternal.Vector3>(baseVariable.name, linkedObj, memberNameChain,
                                            (object val) => swoleTransform.InverseTransformVector(baseVariable.AsVector3(ConvertToVector3(val))),
                                            (EngineInternal.Vector3 v) => convertBackV3(swoleTransform.TransformVector(v)));

                                        break;
                                    case CreationVariable.ConversionMethod.RootToLocal:

                                        variable = new SwoleVarLinkedInstance<EngineInternal.Vector3>(baseVariable.name, linkedObj, memberNameChain,
                                            (object val) => swoleTransform.InverseTransformVector(rootTransform.TransformVector(baseVariable.AsVector3(ConvertToVector3(val)))),
                                            (EngineInternal.Vector3 v) => convertBackV3(rootTransform.TransformVector(swoleTransform.TransformVector(v))));

                                        break;
                                    case CreationVariable.ConversionMethod.RootToWorld:

                                        variable = new SwoleVarLinkedInstance<EngineInternal.Vector3>(baseVariable.name, linkedObj, memberNameChain,
                                            (object val) => rootTransform.TransformVector(baseVariable.AsVector3(ConvertToVector3(val))),
                                            (EngineInternal.Vector3 v) => convertBackV3(rootTransform.InverseTransformVector(v)));

                                        break;
                                    case CreationVariable.ConversionMethod.WorldToRoot:

                                        variable = new SwoleVarLinkedInstance<EngineInternal.Vector3>(baseVariable.name, linkedObj, memberNameChain,
                                            (object val) => rootTransform.InverseTransformVector(baseVariable.AsVector3(ConvertToVector3(val))),
                                            (EngineInternal.Vector3 v) => convertBackV3(rootTransform.TransformVector(v)));

                                        break;
                                }
                            }
                            break;
                        case CreationVariable.Type.Vector4:
                        case CreationVariable.Type.TangentLocal:
                        case CreationVariable.Type.TangentRoot:
                        case CreationVariable.Type.TangentWorld:
                            if (baseVariable.conversionMethod == CreationVariable.ConversionMethod.None || baseVariable.type == CreationVariable.Type.Vector4)
                            {
                                variable = new SwoleVarLinkedInstance<EngineInternal.Vector4>(baseVariable.name, linkedObj, memberNameChain, (object val) => baseVariable.AsVector4(ConvertToVector4(val)), convertBackV4);
                            }
                            else
                            {
                                switch (baseVariable.conversionMethod)
                                {

                                    case CreationVariable.ConversionMethod.LocalToWorld:

                                        variable = new SwoleVarLinkedInstance<EngineInternal.Vector4>(baseVariable.name, linkedObj, memberNameChain,

                                            (object val) =>
                                            {

                                                var v4 = baseVariable.AsVector4(ConvertToVector4(val));
                                                var v3 = swoleTransform.TransformVector(v4);
                                                v4.x = v3.x;
                                                v4.y = v3.y;
                                                v4.z = v3.z;
                                                return v4;

                                            },
                                            (EngineInternal.Vector4 v) =>
                                            {

                                                var v3 = swoleTransform.InverseTransformVector(v);
                                                v.x = v3.x;
                                                v.y = v3.y;
                                                v.z = v3.z;
                                                return convertBackV3(v);

                                            });

                                        break;
                                    case CreationVariable.ConversionMethod.LocalToRoot:

                                        variable = new SwoleVarLinkedInstance<EngineInternal.Vector4>(baseVariable.name, linkedObj, memberNameChain,

                                            (object val) =>
                                            {

                                                var v4 = baseVariable.AsVector4(ConvertToVector4(val));
                                                var v3 = rootTransform.InverseTransformVector(swoleTransform.TransformVector(v4));
                                                v4.x = v3.x;
                                                v4.y = v3.y;
                                                v4.z = v3.z;
                                                return v4;

                                            },
                                            (EngineInternal.Vector4 v) =>
                                            {

                                                var v3 = swoleTransform.InverseTransformVector(rootTransform.TransformVector(v));
                                                v.x = v3.x;
                                                v.y = v3.y;
                                                v.z = v3.z;
                                                return convertBackV4(v);

                                            });

                                        break;
                                    case CreationVariable.ConversionMethod.WorldToLocal:

                                        variable = new SwoleVarLinkedInstance<EngineInternal.Vector4>(baseVariable.name, linkedObj, memberNameChain,

                                            (object val) =>
                                            {

                                                var v4 = baseVariable.AsVector4(ConvertToVector4(val));
                                                var v3 = swoleTransform.InverseTransformVector(v4);
                                                v4.x = v3.x;
                                                v4.y = v3.y;
                                                v4.z = v3.z;
                                                return v4;

                                            },
                                            (EngineInternal.Vector4 v) =>
                                            {

                                                var v3 = swoleTransform.TransformVector(v);
                                                v.x = v3.x;
                                                v.y = v3.y;
                                                v.z = v3.z;
                                                return convertBackV4(v);

                                            });

                                        break;
                                    case CreationVariable.ConversionMethod.RootToLocal:

                                        variable = new SwoleVarLinkedInstance<EngineInternal.Vector4>(baseVariable.name, linkedObj, memberNameChain,

                                            (object val) =>
                                            {

                                                var v4 = baseVariable.AsVector4(ConvertToVector4(val));
                                                var v3 = swoleTransform.InverseTransformVector(rootTransform.TransformVector(v4));
                                                v4.x = v3.x;
                                                v4.y = v3.y;
                                                v4.z = v3.z;
                                                return v4;

                                            },
                                            (EngineInternal.Vector4 v) =>
                                            {

                                                var v3 = rootTransform.InverseTransformVector(swoleTransform.TransformVector(v));
                                                v.x = v3.x;
                                                v.y = v3.y;
                                                v.z = v3.z;
                                                return convertBackV4(v);

                                            });

                                        break;
                                    case CreationVariable.ConversionMethod.RootToWorld:

                                        variable = new SwoleVarLinkedInstance<EngineInternal.Vector4>(baseVariable.name, linkedObj, memberNameChain,

                                            (object val) =>
                                            {

                                                var v4 = baseVariable.AsVector4(ConvertToVector4(val));
                                                var v3 = rootTransform.TransformVector(v4);
                                                v4.x = v3.x;
                                                v4.y = v3.y;
                                                v4.z = v3.z;
                                                return v4;

                                            },
                                            (EngineInternal.Vector4 v) =>
                                            {

                                                var v3 = rootTransform.InverseTransformVector(v);
                                                v.x = v3.x;
                                                v.y = v3.y;
                                                v.z = v3.z;
                                                return convertBackV4(v);

                                            });

                                        break;
                                    case CreationVariable.ConversionMethod.WorldToRoot:

                                        variable = new SwoleVarLinkedInstance<EngineInternal.Vector4>(baseVariable.name, linkedObj, memberNameChain,

                                            (object val) =>
                                            {

                                                var v4 = baseVariable.AsVector4(ConvertToVector4(val));
                                                var v3 = rootTransform.InverseTransformVector(v4);
                                                v4.x = v3.x;
                                                v4.y = v3.y;
                                                v4.z = v3.z;
                                                return v4;

                                            },
                                            (EngineInternal.Vector4 v) =>
                                            {

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
                        case CreationVariable.Type.Quaternion:
                        case CreationVariable.Type.RotationLocal:
                        case CreationVariable.Type.RotationRoot:
                        case CreationVariable.Type.RotationWorld:
                            if (baseVariable.conversionMethod == CreationVariable.ConversionMethod.None)
                            {
                                variable = new SwoleVarLinkedInstance<EngineInternal.Quaternion>(baseVariable.name, linkedObj, memberNameChain, (object val) => baseVariable.AsQuaternion(ConvertToQuaternion(val)), convertBackQ);
                            }
                            else
                            {

                                switch (baseVariable.conversionMethod)
                                {
                                    case CreationVariable.ConversionMethod.LocalToWorld:

                                        variable = new SwoleVarLinkedInstance<EngineInternal.Quaternion>(baseVariable.name, linkedObj, memberNameChain,
                                            (object val) => (swoleTransform.rotation * baseVariable.AsQuaternion(ConvertToQuaternion(val))),
                                            (EngineInternal.Quaternion q) => convertBackQ(swoleTransform.rotation.inverse * q));

                                        break;
                                    case CreationVariable.ConversionMethod.LocalToRoot:

                                        variable = new SwoleVarLinkedInstance<EngineInternal.Quaternion>(baseVariable.name, linkedObj, memberNameChain,
                                            (object val) => (rootTransform.rotation.inverse * (swoleTransform.rotation * baseVariable.AsQuaternion(ConvertToQuaternion(val)))),
                                            (EngineInternal.Quaternion q) => convertBackQ(swoleTransform.rotation.inverse * (rootTransform.rotation * q)));

                                        break;
                                    case CreationVariable.ConversionMethod.WorldToLocal:

                                        variable = new SwoleVarLinkedInstance<EngineInternal.Quaternion>(baseVariable.name, linkedObj, memberNameChain,
                                            (object val) => (swoleTransform.rotation.inverse * baseVariable.AsQuaternion(ConvertToQuaternion(val))),
                                            (EngineInternal.Quaternion q) => convertBackQ(swoleTransform.rotation * q));

                                        break;
                                    case CreationVariable.ConversionMethod.RootToLocal:

                                        variable = new SwoleVarLinkedInstance<EngineInternal.Quaternion>(baseVariable.name, linkedObj, memberNameChain,
                                            (object val) => (swoleTransform.rotation.inverse * (rootTransform.rotation * baseVariable.AsQuaternion(ConvertToQuaternion(val)))),
                                            (EngineInternal.Quaternion q) => convertBackQ(rootTransform.rotation.inverse * (swoleTransform.rotation * q)));

                                        break;
                                    case CreationVariable.ConversionMethod.RootToWorld:

                                        variable = new SwoleVarLinkedInstance<EngineInternal.Quaternion>(baseVariable.name, linkedObj, memberNameChain,
                                            (object val) => (rootTransform.rotation * baseVariable.AsQuaternion(ConvertToQuaternion(val))),
                                            (EngineInternal.Quaternion q) => convertBackQ(rootTransform.rotation.inverse * q));

                                        break;
                                    case CreationVariable.ConversionMethod.WorldToRoot:

                                        variable = new SwoleVarLinkedInstance<EngineInternal.Quaternion>(baseVariable.name, linkedObj, memberNameChain,
                                            (object val) => (rootTransform.rotation.inverse * baseVariable.AsQuaternion(ConvertToQuaternion(val))),
                                            (EngineInternal.Quaternion q) => convertBackQ(rootTransform.rotation * q));

                                        break;
                                }
                            }

                            break;
                        case CreationVariable.Type.Matrix4x4:
                        case CreationVariable.Type.LocalToRoot:
                        case CreationVariable.Type.LocalToWorld:
                        case CreationVariable.Type.RootToLocal:
                        case CreationVariable.Type.WorldToLocal:
                            variable = new SwoleVarLinkedInstance<EngineInternal.Matrix4x4>(baseVariable.name, linkedObj, memberNameChain, (object val) => baseVariable.AsMatrix4x4(ConvertToMatrix4x4(val)), convertBackMatrix);
                            break;

                    }
                    if (variable == null)
                    {
                        if (swoleTransform.Instance == null)
                        {
                            startupLogger?.LogWarning($"Unable to create value link for creation variable '{baseVariable.name}' because the type '{(linkedType == null ? "null" : linkedType.Name)}' is not supported!");
                        }
                        else
                        {
                            startupLogger?.LogWarning($"Unable to link creation variable '{baseVariable.name}' with child object '{baseVariable.objectLink}' because the type '{(linkedType == null ? "null" : linkedType.Name)}' is not supported!");
                        }
                    }
                }
                else
                {
                    if (swoleTransform.Instance == null)
                    {
                        startupLogger?.LogError($"Failed to create value link for creation variable '{baseVariable.name}'! Reason: linkedObj is null");
                    }
                    else
                    {
                        startupLogger?.LogError($"Failed to link creation variable '{baseVariable.name}' with child object '{baseVariable.objectLink}' at member path: '{baseVariable.memberLink}'! Reason: linkedObj is null");
                    }
                }
                return variable;
            }

            if (!string.IsNullOrEmpty(baseVariable.objectLink)) // Variable expects a GameObject link if not null/empty. The variable will serve as a proxy for an external variable, assuming it isn't a driver. It can be read, and also written to if not set as read-only.
            {

                if (creationInstance != null) // The creation instance that holds all valid child objects and the script runtime environment (for drivers).
                {

                    memberNameChain = new List<string>();

                    GameObject unityObject = creationInstance.FindUnityObject(baseVariable.objectLink);

                    if (unityObject != null)
                    {
                         
                        swoleTransform = UnityEngineHook.AsSwoleTransform(unityObject.transform); // The linked object's transform. Used for space conversions (if any)

                        linkedObj = unityObject; // The reference object to start with when traversing the reference chain (assuming baseVariable.memberLink isn't null/empty).
                        linkedType = linkedObj.GetType();

                        bool invalid = false;
                        if (!string.IsNullOrEmpty(baseVariable.memberLink))
                        {
                            string[] memberPath = baseVariable.memberLink.Split('.');
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
                            if (!CreateValueContainerInstance()) return;
                        }

                        if (invalid)
                        {
                            startupLogger?.LogError($"Unable to link creation variable '{baseVariable.name}' with child object '{baseVariable.objectLink}' at invalid member path: '{baseVariable.memberLink}'!");
                        }
                        else
                        {
                            variable = CreateValueLinkWithConversions();
                            if (variable == null) return;
                        }
                    }
                    else
                    {
                        startupLogger?.LogWarning($"Unable to link creation variable '{baseVariable.name}' with child object '{baseVariable.objectLink}' because the object could not be found!");
                    }

                }
                else
                {
                    startupLogger?.LogError($"Unable to link creation variable '{baseVariable.name}' with child object '{baseVariable.objectLink}' because creationInstance was null!");
                }
            }
            else if (isDriver) // Variable is a dynamic value driven by a script. It cannot be written to.
            {
                if (CreateValueContainerInstance())
                {
                    variable = CreateValueLinkWithConversions();
                    if (variable == null) return;
                }
                else return;
            }
            else // Variable is a regular value that can be read, and also written to if not set as read-only.
            {
                if (baseVariable.conversionMethod == CreationVariable.ConversionMethod.None || !baseVariable.type.IsSpaceConvertible())
                {
                    switch (baseVariable.type)
                    {

                        case CreationVariable.Type.Double:
                            variable = new SwoleVar<double>(baseVariable.name, baseVariable.defaultValue);
                            break;
                        case CreationVariable.Type.Float:
                            variable = new SwoleVar<float>(baseVariable.name, (float)baseVariable.defaultValue);
                            break;
                        case CreationVariable.Type.Int:
                            variable = new SwoleVar<int>(baseVariable.name, (int)baseVariable.defaultValue);
                            break;
                        case CreationVariable.Type.Bool:
                            variable = new SwoleVar<bool>(baseVariable.name, baseVariable.defaultValue.AsBool());
                            break;
                        case CreationVariable.Type.String:
                            variable = new SwoleVar<string>(baseVariable.name, baseVariable.defaultString);
                            break;
                        case CreationVariable.Type.Vector2:
                            variable = new SwoleVar<EngineInternal.Vector2>(baseVariable.name, baseVariable.defaultVector);
                            break;
                        case CreationVariable.Type.Vector3:
                        case CreationVariable.Type.ScaleLocal:
                        case CreationVariable.Type.PositionLocal:
                        case CreationVariable.Type.PositionRoot:
                        case CreationVariable.Type.PositionWorld:
                        case CreationVariable.Type.DirectionLocal:
                        case CreationVariable.Type.DirectionRoot:
                        case CreationVariable.Type.DirectionWorld:
                        case CreationVariable.Type.EulerAngles:
                        case CreationVariable.Type.RotationEulerLocal:
                        case CreationVariable.Type.RotationEulerRoot:
                        case CreationVariable.Type.RotationEulerWorld:
                            variable = new SwoleVar<EngineInternal.Vector3>(baseVariable.name, baseVariable.defaultVector);
                            break;
                        case CreationVariable.Type.Vector4:
                        case CreationVariable.Type.TangentLocal:
                        case CreationVariable.Type.TangentRoot:
                        case CreationVariable.Type.TangentWorld:
                            variable = new SwoleVar<EngineInternal.Vector4>(baseVariable.name, baseVariable.defaultVector);

                            break;
                        case CreationVariable.Type.Quaternion:
                        case CreationVariable.Type.RotationLocal:
                        case CreationVariable.Type.RotationRoot:
                        case CreationVariable.Type.RotationWorld:
                            variable = new SwoleVar<EngineInternal.Quaternion>(baseVariable.name, baseVariable.defaultVector);
                            break;
                        case CreationVariable.Type.Matrix4x4:
                        case CreationVariable.Type.LocalToRoot:
                        case CreationVariable.Type.LocalToWorld:
                        case CreationVariable.Type.RootToLocal:
                        case CreationVariable.Type.WorldToLocal:
                            variable = new SwoleVar<EngineInternal.Matrix4x4>(baseVariable.name, baseVariable.defaultMatrix);
                            break;
                    }
                    if (variable == null) return;
                }
                else
                {
                    if (CreateValueContainerInstance())
                    {
                        variable = CreateValueLinkWithConversions();
                        if (variable == null) return;
                    }
                    else return;
                }
            }

        }

#if SWOLE_ENV
        public Value ValueMS => variable == null ? null : variable.ValueMS;
#endif

        public string Name => baseVariable.name;

        public object Value { get => (variable == null ? null : variable.Value); set { if (!baseVariable.readOnly && variable != null) variable.Value = value; } }

        public object PreviousValue { get => (variable == null ? null : variable.PreviousValue); }

        public bool ValueChanged { get => (variable == null ? false : variable.ValueChanged); }

        public Type GetValueType() => (variable == null ? null : variable.GetValueType());

    }

}

#endif