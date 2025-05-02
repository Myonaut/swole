#if (UNITY_STANDALONE || UNITY_EDITOR)
#define FOUND_UNITY
#endif

using System;
using System.Collections;
using System.Collections.Generic;

#if SWOLE_ENV
using Miniscript;
using System.Reflection;
#endif

#if FOUND_UNITY
using Swole.API.Unity;
using UnityEngine;
#endif

using Swole.Animation;

namespace Swole.Script
{

    public static class SwoleScriptIntrinsics
    {

        /// <summary>
        /// The default amount of time an event listener is allowed to run
        /// </summary>
        public const double _eventTimeout = 0.05f;
        /// <summary>
        /// The minimum timeout value an event listener throttle can reach
        /// </summary>
        public const double _eventMinTimeout = 0.001f;
        /// <summary>
        /// The maximum timeout value an event listener throttle can reach
        /// </summary>
        public const double _eventMaxTimeout = 0.1f;

        public const double _eventThrottlePromotionWeight = 1.5f; 
        public const double _eventThrottleReductionWeight = 0.9f;

        #region Variable Name Constants

        public const string var_self = "self";

        public const string var_arg0 = "arg0";
        public const string var_arg1 = "arg1";
        public const string var_arg2 = "arg2";
        public const string var_arg3 = "arg3";
        public const string var_arg4 = "arg4";
        public const string var_arg5 = "arg5";
        public const string var_arg6 = "arg6";
        public const string var_arg7 = "arg7";

        public const string var_func = "func";
        public const string var_genericID = "id";
        public const string var_name = "name";
        public const string var_genericName = var_name;
        public const string var_type = "type";
        public const string var_index = "index";
        public const string var_asset = "asset";
        public const string var_assetPath = "assetPath";

        public const string var_caseSensitive = "caseSensitive";

        public const string var_engineObjectID = "engineId";
        public const string var_cachedEngineObject = "~cachedEngineObject";
        public const string var_cachedEventHandler = "~eventHandler";
        public const string var_cachedReference = "~reference";

        public const string var_eventHandler = "eventHandler";
        public const string var_hasEventHandler = "hasEventHandler";

        public const string var_swoleID = var_genericID;

        public const string var_gameObject = "gameObject";
        public const string var_transform = "transform";
        public const string var_animator = "animator";

        public const string var_position = "position";
        public const string var_rotation = "rotation";
        public const string var_scale = "scale";

        public const string var_localPosition = "localPosition";
        public const string var_localRotation = "localRotation";
        public const string var_localScale = "localScale";

        public const string var_lossyScale = "lossyScale";

        public const string var_value = "value";
        public const string var_delay = "delay";

        public const string var_weight = "weight";

        public const string var_prefix = "prefix";

        public const string var_pause = "pause";

        public const string var_inverse = "inverse";

        public const string var_source = "source";

        public const string var_isActive = "isActive";

        public const string var_root = "root";
        public const string var_rootInstance = "rootInstance";
        public const string var_rootCreationInstance = "rootCreationInstance";

        public const string var_current = "current";
        public const string var_gameplayExperience = "gameplayExperience";

        #region Math

        public const string var_vector = "vector";
        public const string var_vectorA = "vectorA";
        public const string var_vectorB = "vectorB";
        public const string var_vectorC = "vectorC";

        public const string var_quaternion = "quaternion";
        public const string var_quaternionA = "quaternionA";
        public const string var_quaternionB = "quaternionB";
        public const string var_quaternionC = "quaternionC";

        public const string var_axis = "axis";

        #endregion

        #region Directions

        public const string var_forward = "forward";
        public const string var_upward = "upward";

        #endregion

        #region Animation

        public const string var_animation = "animation";

        public const string var_loopMode = "loopMode";

        public const string var_time = "time";
        public const string var_normalizedTime = "normalizedTime";
        public const string var_addTime = "addTime";
        public const string var_speed = "speed";
        public const string var_internalSpeed = "internalSpeed";

        public const string var_length = "length";
        public const string var_lengthInSeconds = "lengthInSeconds";
        public const string var_duration = "duration";
        public const string var_estimatedDuration = "estimatedDuration";

        public const string var_step = "step";

        public const string var_bone = "bone";
        public const string var_boneCount = "boneCount";

        public const string var_blendParameterIndex = "blendParameterIndex";
        public const string var_entryStateIndex = "entryStateIndex";
        public const string var_mix = "mix";
        public const string var_isAdditive = "isAdditive";
        public const string var_isBlend = "isBlend";
        public const string var_motionControllerIdentifiers = "motionControllerIdentifiers";
        public const string var_stateMachines = "stateMachines";

        public const string var_activeStateIndex = "activeStateIndex";
        public const string var_activeState = "activeState";
        public const string var_hasActiveState = "hasActiveState";

        public const string var_motionControllerIndex = "motionControllerIndex";
        public const string var_transitions = "transitions";

        public const string var_layer = "layer";
        public const string var_layers = "layers";
        public const string var_layerName = "layerName";

        public const string var_controller = "controller";

        public const string var_animationReferences = "animationReferences";

        public const string var_animationPlayer = "animationPlayer";

        public const string var_transitionTarget = "transitionTarget";
        public const string var_transitionTime = "transitionTime";
        public const string var_transitionTimeLeft = "transitionTimeLeft";

        #endregion

        #region Audio

        public const string var_clip = "clip";
        public const string var_mixer = "mixer";
        public const string var_clips = "clips";
        public const string var_bundle = "bundle";
        public const string var_bundles = "bundles";

        public const string var_volume = "volume";
        public const string var_volumeScale = "volumeScale";

        #endregion

        #region Cameras

        public const string var_fieldOfView = "fieldOfView";
        public const string var_orthographic = "orthographic";
        public const string var_orthographicSize = "orthographicSize";
        public const string var_nearClipPlane = "nearClipPlane";
        public const string var_farClipPlane = "farClipPlane";

        #endregion

#if SWOLE_ENV
        public static readonly ValString varStr_engineObjectID = new ValString(var_engineObjectID);
        public static readonly ValString varStr_cachedEngineObject = new ValString(var_cachedEngineObject);
        public static readonly ValString varStr_cachedEventHandler = new ValString(var_cachedEventHandler);
        public static readonly ValString varStr_cachedReference = new ValString(var_cachedReference);
#endif

        #endregion

        #region Type Name Constants

        public const string type_EngineObject = nameof(EngineInternal.EngineObject);
        public const string type_GameObject = nameof(EngineInternal.GameObject);

        public const string type_RNG = "RNG";
        public const string type_RNGState = "RNGState";
        public const string type_InputManager = "InputManager";

        #region Components

        public const string type_Component = "Component";
        public const string type_Transform = "Transform";
        public const string type_Camera = "Camera";

        #region Physics

        public const string type_Rigidbody = "Rigidbody";
        public const string type_Collider = "Collider";
        public const string type_BoxCollider = "BoxCollider";
        public const string type_SphereCollider = "SphereCollider";
        public const string type_CapsuleCollider = "CapsuleCollider";
        public const string type_MeshCollider = "MeshCollider";

        #endregion

        #endregion

        public const string type_SwoleGameObject = nameof(EngineInternal.SwoleGameObject);
        public const string type_TileInstance = nameof(EngineInternal.TileInstance);
        public const string type_CreationInstance = nameof(EngineInternal.CreationInstance);

        #region Animation

        public const string type_Animation = "Animation";

        public const string type_Animator = "Animator";

        public const string type_AnimationPlayer = "AnimationPlayer";
        public const string type_AnimationLayer = "AnimationLayer";
        public const string type_AnimationController = "AnimationController";
        public const string type_AnimationStateMachine = "AnimationStateMachine";
        public const string type_AnimationMotionController = "AnimationMotionController";

        public const string type_AnimationReference = "AnimationReference";

        public const string type_Curve = "Curve";
        public const string type_BezierCurve = "BezierCurve";  

        #endregion

        #region Assets

        public const string type_Asset = "Asset";

        public const string type_CreationAsset = "CreationAsset";

        public const string type_AnimationAsset = "AnimationAsset";

        public const string type_ImageAsset = "ImageAsset";
        public const string type_TextureAsset = "TextureAsset";
        public const string type_SpriteAsset = "SpriteAsset";

        public const string type_AudioAsset = "AudioAsset";
        public const string type_SFXAsset = "SFXAsset";
        public const string type_MusicAsset = "MusicAsset";
        public const string type_VoiceAsset = "VoiceAsset";

        public const string type_MaterialAsset = "MaterialAsset";

        #endregion

        #region Audio

        public const string type_AudioClip = "AudioClip";
        public const string type_AudioSource = "AudioSource";
        public const string type_AudibleObject = "AudibleObject";
        public const string type_AudioBundle = "AudioBundle";
        public const string type_AudioMixerGroup = "AudioMixer";

        #endregion

        public const string type_EventHandler = "EventHandler";

        #endregion

        #region Function Name Constants

        public const string func_LookupEnvironmentVar = "env";

        public const string func_InvokeOverTime = "InvokeOverTime";
        public const string func_CancelToken = "CancelToken";

        public const string func_FindGameObject = "FindGameObject";
        public const string func_FindSwoleGameObject = "FindSwoleGameObject";
        public const string func_DestroyObject = "Destroy";

        public const string func_Find = "Find";

        public const string func_Listen = "Listen";
        public const string func_StopListening = "StopListening";
        public const string func_ListenForEvents = "ListenForEvents";
        public const string func_StopListeningForEvents = "StopListeningForEvents";

        public const string func_PreListen = "PreListen";
        public const string func_StopPreListening = "StopPreListening";
        public const string func_PreListenForEvents = "PreListenForEvents";
        public const string func_StopPreListeningForEvents = "StopPreListeningForEvents";

        public const string func_SetActive = "SetActive";

        #region Math

        public const string func_min = "min";
        public const string func_max = "max";
        public const string func_clamp = "clamp";

        public const string func_lerp = "lerp";
        public const string func_slerp = "slerp";

        #region Vectors

        public const string func_SignedAngle = "SignedAngle";

        #endregion

        #region Quaternions

        public const string func_EulerQuaternion = "EulerQuaternion";
        public const string func_FromToRotation = "FromToRotation";
        public const string func_LookRotation = "LookRotation";

        #endregion

        #endregion

        #region Assets

        public const string func_CreateAsset = "CreateAsset";
        public const string func_FindAsset = "FindAsset";

        public const string func_FindCreationAsset = "FindCreationAsset";
        public const string func_FindAnimationAsset = "FindAnimationAsset";

        #endregion

        #region GameObjects

        public const string func_GetComponent = "GetComponent";
        public const string func_AddComponent = "AddComponent";

        #endregion

        #region Transforms

        public const string func_GetWorldPosition = "GetWorldPos";
        public const string func_GetWorldRotation = "GetWorldRot";
        public const string func_GetLossyScale = "GetLossyScale";

        public const string func_GetLocalPosition = "GetLocalPos";
        public const string func_GetLocalRotation = "GetLocalRot";
        public const string func_GetLocalScale = "GetLocalScale";


        public const string func_SetWorldPosition = "SetWorldPos";
        public const string func_SetWorldRotation = "SetWorldRot";
        public const string func_SetLossyScale = "SetLossyScale";

        public const string func_SetLocalPosition = "SetLocalPos";
        public const string func_SetLocalRotation = "SetLocalRot";
        public const string func_SetLocalScale = "SetLocalScale";

        #endregion

        #region Cameras

        public const string func_GetMainCamera = "GetMainCamera";

        #endregion

        #region Creations

        public const string func_GetRootCreationInstance = "GetRootCreationInstance";
        public const string func_GetGameplayExperience = "GetGameplayExperience";
        public const string func_FindSwoleGameObjectInCreation = "FindSwoleGameObjectInCreation";
        public const string func_FindById = "FindById";

        #endregion

        #region Animations

        public const string func_CreateAnimationLayer = "CreateAnimationLayer";
        public const string func_CreateStateMachine = "CreateStateMachine";
        public const string func_CreateAnimationReference = "CreateAnimationReference";

        public const string func_FindLayer = "FindLayer";

        public const string func_HasAnimationPlayer = "HasAnimationPlayer";

        public const string func_RestartAnims = "RestartAnims";
        public const string func_ResyncAnims = "ResyncAnims";
        public const string func_ResetTransition = "ResetTransition";

        public const string func_GetMotionController = "GetMotionController";

        #endregion

        #endregion

        public class ExecutionThrottle
        {
            public ExecutionThrottle(double startval)
            {
                value = startval;
            }
            public double value;
        }

#if SWOLE_ENV
        public static void RunExternally(string debugName, TAC.Machine machine, ValFunction function, Value resultStorage = null, List<Value> arguments = null, ExecutionThrottle throttle = null)
        {
            machine.ManuallyPushCall(function, null, arguments);
            var cont = machine.GetTopContext();
            double startTime = machine.runTime;
            while (!cont.done)
            {
                if (machine.runTime - startTime > throttle.value) // time's up
                {
                    swole.LogWarning($"External call '{debugName}' took too long to complete and will be throttled!");
                    cont.lineNum = cont.code.Count;
                    if (throttle != null) throttle.value = System.Math.Max(throttle.value * _eventThrottleReductionWeight, _eventMinTimeout);
                    return;
                }
                //swole.Log($"Step {cont.lineNum} / {cont.code.Count - 1}");   
                machine.Step();
            }
            if (throttle != null) throttle.value = System.Math.Min(throttle.value * _eventThrottlePromotionWeight, _eventMaxTimeout);

            /*interpreter.RunUntilDone(throttle, false); // Runs any left over code on the stack which is bad(?)
            if (!machine.done)
            {
                swole.LogWarning($"Event listener '{function.ToString(machine)}' took too long to complete and will be throttled!");
                throttle = System.Math.Max(throttle * _eventThrottleReductionWeight, _eventMinTimeout);
            }
            else
            {
                throttle = System.Math.Min(throttle * _eventThrottlePromotionWeight, _eventMaxTimeout);
            }*/
        }
#endif

        private static bool initialized;
        public static void Initialize()
        {
            if (initialized) return;
            initialized = true;

#if SWOLE_ENV

            #region DEFINE MINISCRIPT INTRINSICS

            Intrinsic f;

            // env
            //	Returns the value of an environment variable if it exists in the host data
            f = Intrinsic.Create(func_LookupEnvironmentVar);
            f.AddParam(var_genericID);
            f.code = (context, partialResult) => {
#if FOUND_UNITY
                if (context.interpreter.hostData is IRuntimeHost hostData)
                {
                    if (hostData.TryGetEnvironmentVar(context.GetVar(var_genericID).ToString(), out IVar v)) return new Intrinsic.Result(v.ValueMS); 
                    return Intrinsic.Result.Null;
                }
#endif
                return Intrinsic.Result.Null;
            };

            // CancelToken
            //	cancels the token
            f = Intrinsic.Create(func_CancelToken);
            f.AddParam(var_self);
            f.code = (context, partialResult) => {

                Value self = context.self;
                if (!TryGetCachedReference<ValCancellationToken>(self, out var token) || token.reference == null) throw new RuntimeException("object is not a valid cancellation token");
                return new Intrinsic.Result(ValNumber.Truth(token.reference.Cancel()));
            };

            // InvokeOverTime(func, duration, step=0)
            //	runs the function over and over until enough time has passed
            f = Intrinsic.Create(func_InvokeOverTime);
            f.AddParam(var_self);
            f.AddParam(var_duration);
            f.AddParam(var_step);
            f.code = (context, partialResult) => { 
                if (context.interpreter.hostData is not IRuntimeEnvironment enviro) throw new RuntimeException("cannot invoke functions over time in this environment");

                Value func = context.self;
                if (func is not ValFunction function || function.function == null) throw new RuntimeException("object must be a function");

                var token = enviro.GetNewCancellationToken();
                if (token == null) throw new RuntimeException("environment is invalid");

                var interpreter = context.interpreter;
                var machine = interpreter.vm;

                var debugNameVal = context.GetLocal(var_genericName);
                string debugName = debugNameVal == null ? function.ToString(machine) : debugNameVal.ToString(machine);

                ExecutionThrottle throttle = new ExecutionThrottle(_eventTimeout);
                void Invoke()
                {
                    try
                    {
                        if (!ReferenceEquals(interpreter.vm, machine)) // interpreter got disposed or recompiled
                        {
                            return;
                        }

                        RunExternally(debugName, machine, function, null, null, throttle);
                    }
                    catch (Exception ex)
                    {
                        swole.LogError(ex);
                    }
                }
                  
                swole.Engine.InvokeOverTime(Invoke, token, context.GetLocalFloat(var_duration, 0), context.GetLocalFloat(var_step, 0));

                return new Intrinsic.Result(ConvertCancellationToken(token));
            };
            AddLocalIntrinsicToObject(Intrinsics.FunctionType(), f, func_InvokeOverTime);  

            #region Types

            // RNG type
            //	Returns a map that represents the RNG datatype.
            f = Intrinsic.Create(type_RNG);
            f.code = (context, partialResult) => {
                var type = RNGType().GetType(context);
                return new Intrinsic.Result(type);
            };

            // RNGState type
            //	Returns a map that represents the RNGState datatype.
            f = Intrinsic.Create(type_RNGState);
            f.code = (context, partialResult) => {
                var type = RNGStateType().GetType(context);
                return new Intrinsic.Result(type);
            };

            // InputManager type
            //	Returns a map that represents the InputManager datatype.
            f = Intrinsic.Create(type_InputManager);
            f.code = (context, partialResult) => {
                var type = InputManagerType().GetType(context);
                return new Intrinsic.Result(type);
            };

            // EventHandler type
            //	Returns a map that represents the EventHandler datatype.
            f = Intrinsic.Create(type_EventHandler);
            f.code = (context, partialResult) => {
                var type = EventHandlerType().GetType(context);
                return new Intrinsic.Result(type);
            };

            // EngineObject type
            //	Returns a map that represents the EngineObject datatype.
            f = Intrinsic.Create(type_EngineObject);
            f.code = (context, partialResult) => {
                var type = EngineObjectType().GetType(context);
                return new Intrinsic.Result(type);
            };
            // GameObject type
            //	Returns a map that represents the GameObject datatype.
            f = Intrinsic.Create(type_GameObject);
            f.code = (context, partialResult) => {
                var type = GameObjectType().GetType(context);
                return new Intrinsic.Result(type);
            };
            // SwoleGameObject type
            //	Returns a map that represents the SwoleGameObject datatype.
            f = Intrinsic.Create(type_SwoleGameObject);
            f.code = (context, partialResult) => {
                var type = SwoleGameObjectType().GetType(context);
                return new Intrinsic.Result(type);
            };

            // Component type
            //	Returns a map that represents the Component datatype.
            f = Intrinsic.Create(type_Component);
            f.code = (context, partialResult) => {
                var type = ComponentType().GetType(context);
                return new Intrinsic.Result(type);
            };
            // Transform type
            //	Returns a map that represents the Transform datatype.
            f = Intrinsic.Create(type_Transform);
            f.code = (context, partialResult) => {
                var type = TransformType().GetType(context);
                return new Intrinsic.Result(type);
            };
            // Camera type
            //	Returns a map that represents the Camera datatype.
            f = Intrinsic.Create(type_Camera);
            f.code = (context, partialResult) => {
                var type = CameraType().GetType(context);
                return new Intrinsic.Result(type);
            };

            // TileInstance type
            //	Returns a map that represents the TileInstance datatype.
            f = Intrinsic.Create(type_TileInstance);
            f.code = (context, partialResult) => {
                var type = TileInstanceType().GetType(context);
                return new Intrinsic.Result(type);
            };
            // CreationInstance type
            //	Returns a map that represents the CreationInstance datatype.
            f = Intrinsic.Create(type_CreationInstance);
            f.code = (context, partialResult) => {
                var type = CreationInstanceType().GetType(context);
                return new Intrinsic.Result(type);
            };

            #region Physics

            CreatePhysicsTypes();

            #endregion

            #region Audio

            CreateAudioTypes();

            #endregion

            #region Animation

            CreateAnimationTypes();

            #endregion

            #endregion

            #region Math

            // self.inverse
            //	Returns the inverse of a quaternion or a matrix
            f = Intrinsic.Create(var_inverse);
            f.AddParam(var_self);
            f.code = (context, partialResult) => {
                Value self = context.self;
                if (self is ValQuaternion quat)
                {
                    var q = new EngineInternal.Quaternion((float)quat.X, (float)quat.Y, (float)quat.Z, (float)quat.W);
                    q = q.inverse;

                    return new Intrinsic.Result(new ValQuaternion(q.x, q.y, q.z, q.w));
                }
                else if (self is ValMatrix mat)
                {
                    var m = new EngineInternal.Matrix4x4(                      
                        (float)mat.c0.X, (float)mat.c0.Y, (float)mat.c0.Z, (float)mat.c0.W,
                        (float)mat.c1.X, (float)mat.c1.Y, (float)mat.c1.Z, (float)mat.c1.W,
                        (float)mat.c2.X, (float)mat.c2.Y, (float)mat.c2.Z, (float)mat.c2.W,
                        (float)mat.c3.X, (float)mat.c3.Y, (float)mat.c3.Z, (float)mat.c3.W
                        );
                    m = m.inverse;

                    return new Intrinsic.Result(new ValMatrix(m.m00, m.m01, m.m02, m.m03, m.m10, m.m11, m.m12, m.m13, m.m20, m.m21, m.m22, m.m23, m.m30, m.m31, m.m32, m.m33));
                }
                return new Intrinsic.Result(self);
            };

            f = Intrinsic.Create(func_min);
            f.AddParam(var_arg0);
            f.AddParam(var_arg1);
            f.code = (context, partialResult) => {
                var valA = context.GetLocal(var_arg0);
                var valB = context.GetLocal(var_arg1);
                if (valA == null) throw new RuntimeException("arg 1 must be a number");
                if (valB == null) throw new RuntimeException("arg 2 must be a number");
                return new Intrinsic.Result(new ValNumber(Math.Min(valA.FloatValue(), valB.FloatValue())));
            };
            f = Intrinsic.Create(func_max);
            f.AddParam(var_arg0);
            f.AddParam(var_arg1);
            f.code = (context, partialResult) => {
                var valA = context.GetLocal(var_arg0);
                var valB = context.GetLocal(var_arg1);
                if (valA == null) throw new RuntimeException("arg 1 must be a number");
                if (valB == null) throw new RuntimeException("arg 2 must be a number");
                return new Intrinsic.Result(new ValNumber(Math.Max(valA.FloatValue(), valB.FloatValue())));
            };
            f = Intrinsic.Create(func_clamp);
            f.AddParam(var_arg0);
            f.AddParam(var_arg1);
            f.AddParam(var_arg2);
            f.code = (context, partialResult) => {
                var valA = context.GetLocal(var_arg0);
                var valB = context.GetLocal(var_arg1);
                var valC = context.GetLocal(var_arg2);
                if (valA == null) throw new RuntimeException("arg 1 must be a number");
                if (valB == null) throw new RuntimeException("arg 2 must be a number"); 
                if (valC == null) throw new RuntimeException("arg 3 must be a number");
                return new Intrinsic.Result(new ValNumber(Math.Clamp(valA.FloatValue(), valB.FloatValue(), valC.FloatValue()))); 
            };

            f = Intrinsic.Create(func_lerp);
            f.AddParam(var_vectorA);
            f.AddParam(var_vectorB);
            f.AddParam(var_time);
            f.code = (context, partialResult) => {
                var valA = context.GetLocal(var_vectorA);
                var valB = context.GetLocal(var_vectorB);
                var t = context.GetLocal(var_time);

                if (valA is not ValVector vecA) throw new RuntimeException("arg 1 must be a vector");
                if (valB is not ValVector vecB) throw new RuntimeException("arg 2 must be a vector");
                if (t == null) throw new RuntimeException("arg 3 must be a number");

                var vecA_ = new EngineInternal.Vector4((float)vecA.X, (float)vecA.Y, (float)vecA.Z, (float)vecA.W);
                var vecB_ = new EngineInternal.Vector4((float)vecB.X, (float)vecB.Y, (float)vecB.Z, (float)vecB.W);
                float time = t.FloatValue();

                var v = swole.Engine.lerp(vecA_, vecB_, time);

                if (valA is ValQuaternion || valB is ValQuaternion) return new Intrinsic.Result(new ValQuaternion(v.x, v.y, v.z, v.w));
                return new Intrinsic.Result(new ValVector(v.x, v.y, v.z, v.w));
            };

            f = Intrinsic.Create(func_slerp);
            f.AddParam(var_quaternionA);
            f.AddParam(var_quaternionB);
            f.AddParam(var_time);
            f.code = (context, partialResult) => {
                var valA = context.GetLocal(var_quaternionA);
                var valB = context.GetLocal(var_quaternionB);
                var t = context.GetLocal(var_time);

                if (valA is not ValVector vecA) throw new RuntimeException("arg 1 must be a quaternion");
                if (valB is not ValVector vecB) throw new RuntimeException("arg 2 must be a quaternion");
                if (t == null) throw new RuntimeException("arg 3 must be a number");

                var vecA_ = new EngineInternal.Quaternion((float)vecA.X, (float)vecA.Y, (float)vecA.Z, (float)vecA.W);
                var vecB_ = new EngineInternal.Quaternion((float)vecB.X, (float)vecB.Y, (float)vecB.Z, (float)vecB.W);
                float time = t.FloatValue();

                var v = swole.Engine.slerp(vecA_, vecB_, time);  

                return new Intrinsic.Result(new ValQuaternion(v.x, v.y, v.z, v.w));
            };
             
            #region Vectors

            f = Intrinsic.Create(func_SignedAngle);
            f.AddParam(var_vectorA);
            f.AddParam(var_vectorB);
            f.AddParam(var_axis);
            f.code = (context, partialResult) => {
                var valA = context.GetLocal(var_vectorA);
                var valB = context.GetLocal(var_vectorB);
                var valC = context.GetLocal(var_axis, new ValVector(0, 1, 0));

                if (valA is not ValVector vecA) throw new RuntimeException("arg 1 must be a vector");
                if (valB is not ValVector vecB) throw new RuntimeException("arg 2 must be a vector");
                if (valC is not ValVector vecC) throw new RuntimeException("arg 3 must be a vector");

                var vecA_ = new EngineInternal.Vector3((float)vecA.X, (float)vecA.Y, (float)vecA.Z);
                var vecB_ = new EngineInternal.Vector3((float)vecB.X, (float)vecB.Y, (float)vecB.Z);
                var vecC_ = new EngineInternal.Vector3((float)vecC.X, (float)vecC.Y, (float)vecC.Z);

                var a = swole.Engine.Vector3_SignedAngle(vecA_, vecB_, vecC_);

                return new Intrinsic.Result(new ValNumber(a));
            };

            #endregion
            
            #region Quaternions

            f = Intrinsic.Create(func_EulerQuaternion);
            f.AddParam(var_arg0);
            f.AddParam(var_arg1);
            f.AddParam(var_arg2);
            f.code = (context, partialResult) => { 
                EngineInternal.Vector3 vec = default;
                var arg0 = context.GetLocal(var_arg0);
                if (arg0 is ValVector vecA)
                {
                    vec = new EngineInternal.Vector3((float)vecA.X, (float)vecA.Y, (float)vecA.Z);
                } 
                else
                {
                    var arg1 = context.GetLocal(var_arg1);
                    var arg2 = context.GetLocal(var_arg2);
                    vec = new EngineInternal.Vector3(arg0 == null ? 0 : arg0.FloatValue(), arg1 == null ? 0 : arg1.FloatValue(), arg2 == null ? 0 : arg2.FloatValue());
                } 
                 
                var q = EngineInternal.Quaternion.Euler(vec);
                return new Intrinsic.Result(new ValQuaternion(q.x, q.y, q.z, q.w));
            };

            f = Intrinsic.Create(func_FromToRotation);
            f.AddParam(var_vectorA);
            f.AddParam(var_vectorB);
            f.code = (context, partialResult) => {
                var valA = context.GetLocal(var_vectorA);
                var valB = context.GetLocal(var_vectorB);

                if (valA is not ValVector vecA) throw new RuntimeException("arg 1 must be a vector");
                if (valB is not ValVector vecB) throw new RuntimeException("arg 2 must be a vector");

                var vecA_ = new EngineInternal.Vector3((float)vecA.X, (float)vecA.Y, (float)vecA.Z);
                var vecB_ = new EngineInternal.Vector3((float)vecB.X, (float)vecB.Y, (float)vecB.Z);

                var q = EngineInternal.Quaternion.FromToRotation(vecA_, vecB_);

                return new Intrinsic.Result(new ValQuaternion(q.x, q.y, q.z, q.w));
            };
             
            f = Intrinsic.Create(func_LookRotation);
            f.AddParam(var_forward);
            f.AddParam(var_upward);
            f.code = (context, partialResult) => {
                var valA = context.GetLocal(var_forward);
                var valB = context.GetLocal(var_upward); 

                if (valA is not ValVector vecA) throw new RuntimeException("arg 1 must be a forward vector");
                ValVector vecB = null;
                if (valB is ValVector) vecB = (ValVector)valB;

                var vecA_ = new EngineInternal.Vector3((float)vecA.X, (float)vecA.Y, (float)vecA.Z);
                var vecB_ = vecB == null ? EngineInternal.Vector3.up : new EngineInternal.Vector3((float)vecB.X, (float)vecB.Y, (float)vecB.Z);

                var q = EngineInternal.Quaternion.LookRotation(vecA_, vecB_);

                return new Intrinsic.Result(new ValQuaternion(q.x, q.y, q.z, q.w));
            };

            #endregion

            #endregion

            #region GameObjects

            // FindGameObject
            //	Attempts to find a GameObject by name
            // Returns a GameObject or null if not found
            f = Intrinsic.Create(func_FindGameObject);
            f.AddParam(var_genericName);
            f.code = (context, partialResult) => 
            {
                if (TryFindGameObject(context.GetLocalString(var_genericName), context.interpreter.hostData, out var engObj)) return new Intrinsic.Result(ConvertGameObject(context, engObj));
                return Intrinsic.Result.Null;
            };

            // self.destroy
            //	Destroys the object if possible
            //	May be called with function syntax or dot syntax.
            // self (gameobject, transform, etc): object to be destroyed
            // Returns whether or not the object was destroyed
            f = Intrinsic.Create(func_DestroyObject); 
            f.AddParam(var_self);
            f.AddParam(var_delay, 0);
            f.code = (context, partialResult) => {

                var engObj = GetEngineObject(context, context.self);

                if (engObj != null)
                {
                    EngineInternal.EngineObject.Destroy(engObj, context.GetLocalInt(var_delay));
                    return Intrinsic.Result.True;
                }

                return Intrinsic.Result.False;  
            };

            // GetComponent
            f = Intrinsic.Create(func_GetComponent);
            f.AddParam(var_self);
            f.AddParam(var_type);
            f.code = (context, partialResult) => {

                var engObj = GetEngineObject(context, context.self);

                EngineInternal.GameObject go = default;
                if (engObj is EngineInternal.GameObject) go = (EngineInternal.GameObject)engObj;
                else if (engObj is EngineInternal.IComponent comp) go = comp.baseGameObject;
                else if (engObj is EngineInternal.SwoleGameObject sgo) go = sgo.instance;
                if (!go.IsDestroyed)
                {
                    var type = context.GetLocal(var_type);
                    if (type == null) return Intrinsic.Result.Null;
                    var comp = go.GetComponent(type.GetRealType());
                    var msval = GetMSValueFromCSharpObject(context, comp);
                    return new Intrinsic.Result(msval);  
                }

                return Intrinsic.Result.Null;
            };

            // AddComponent
            f = Intrinsic.Create(func_AddComponent);
            f.AddParam(var_self);
            f.AddParam(var_type);
            f.code = (context, partialResult) => { 

                var engObj = GetEngineObject(context, context.self);

                EngineInternal.GameObject go = default;
                if (engObj is EngineInternal.GameObject) go = (EngineInternal.GameObject)engObj;
                else if (engObj is EngineInternal.IComponent comp) go = comp.baseGameObject;
                else if (engObj is EngineInternal.SwoleGameObject sgo) go = sgo.instance;

                if (!go.IsDestroyed)
                {
                    var type = context.GetLocal(var_type);
                    if (type == null) return Intrinsic.Result.Null;
                    return new Intrinsic.Result(GetMSValueFromCSharpObject(context, go.AddComponent(type.GetRealType()))); 
                } 
                
                return Intrinsic.Result.Null;
            };

            #endregion

            #region Transforms

            #region Get

            f = Intrinsic.Create(func_GetLocalPosition);
            f.AddParam(var_self);
            f.code = (context, partialResult) => {

                var engObj = GetEngineObject(context, context.self);
                if (EngineInternal.TryGetTransform(engObj, out var transform)) 
                {
                    var val = transform.localPosition;
                    return new Intrinsic.Result(new ValVector(val.x,val.y,val.z));
                }

                return new Intrinsic.Result(ValVector.zero3);
            };
            f = Intrinsic.Create(func_GetWorldPosition);
            f.AddParam(var_self);
            f.code = (context, partialResult) => {

                var engObj = GetEngineObject(context, context.self);
                if (EngineInternal.TryGetTransform(engObj, out var transform))
                {
                    var val = transform.position;
                    return new Intrinsic.Result(new ValVector(val.x, val.y, val.z));
                }

                return new Intrinsic.Result(ValVector.zero3);
            };

            f = Intrinsic.Create(func_GetLocalRotation);
            f.AddParam(var_self);
            f.code = (context, partialResult) => {

                var engObj = GetEngineObject(context, context.self);
                if (EngineInternal.TryGetTransform(engObj, out var transform))
                {
                    var val = transform.localRotation;
                    return new Intrinsic.Result(new ValQuaternion(val.x, val.y, val.z, val.w));
                }

                return new Intrinsic.Result(ValVector.zero3);
            };
            f = Intrinsic.Create(func_GetWorldRotation);
            f.AddParam(var_self);
            f.code = (context, partialResult) => {

                var engObj = GetEngineObject(context, context.self);
                if (EngineInternal.TryGetTransform(engObj, out var transform))
                {
                    var val = transform.rotation;
                    return new Intrinsic.Result(new ValQuaternion(val.x, val.y, val.z, val.w));
                }

                return new Intrinsic.Result(ValVector.zero3);
            };

            f = Intrinsic.Create(func_GetLocalScale);
            f.AddParam(var_self);
            f.code = (context, partialResult) => {

                var engObj = GetEngineObject(context, context.self);
                if (EngineInternal.TryGetTransform(engObj, out var transform))
                {
                    var val = transform.localScale;
                    return new Intrinsic.Result(new ValVector(val.x, val.y, val.z));
                }

                return new Intrinsic.Result(ValVector.one3);
            };
            f = Intrinsic.Create(func_GetLossyScale);
            f.AddParam(var_self);
            f.code = (context, partialResult) => {

                var engObj = GetEngineObject(context, context.self);
                if (EngineInternal.TryGetTransform(engObj, out var transform))
                {
                    var val = transform.lossyScale;
                    return new Intrinsic.Result(new ValVector(val.x, val.y, val.z));
                }

                return new Intrinsic.Result(ValVector.one3);
            };

            #endregion

            #region Set

            f = Intrinsic.Create(func_SetLocalPosition);
            f.AddParam(var_self);
            f.AddParam(var_position);
            f.code = (context, partialResult) => {

                var val = context.GetLocal(var_position);
                if (!(val is ValVector input)) return Intrinsic.Result.Null;

                var engObj = GetEngineObject(context, context.self);
                if (EngineInternal.TryGetTransform(engObj, out var transform))
                {
                    transform.localPosition = new EngineInternal.Vector3((float)input.X, (float)input.Y, (float)input.Z); 
                }

                return Intrinsic.Result.Null;
            };
            f = Intrinsic.Create(func_SetWorldPosition);
            f.AddParam(var_self);
            f.AddParam(var_position);
            f.code = (context, partialResult) => {

                var val = context.GetLocal(var_position);
                if (!(val is ValVector input)) return Intrinsic.Result.Null;

                var engObj = GetEngineObject(context, context.self);
                if (EngineInternal.TryGetTransform(engObj, out var transform))
                {
                    transform.position = new EngineInternal.Vector3((float)input.X, (float)input.Y, (float)input.Z);
                }

                return Intrinsic.Result.Null;
            };

            f = Intrinsic.Create(func_SetLocalRotation);
            f.AddParam(var_self);
            f.AddParam(var_rotation);
            f.code = (context, partialResult) => {

                var val = context.GetLocal(var_rotation);
                if (!(val is ValVector input)) return Intrinsic.Result.Null;

                var engObj = GetEngineObject(context, context.self);
                if (EngineInternal.TryGetTransform(engObj, out var transform))
                {
                    transform.localRotation = new EngineInternal.Quaternion((float)input.X, (float)input.Y, (float)input.Z, (float)input.W);
                }

                return Intrinsic.Result.Null;
            };
            f = Intrinsic.Create(func_SetWorldRotation);
            f.AddParam(var_self);
            f.AddParam(var_rotation);
            f.code = (context, partialResult) => {

                var val = context.GetLocal(var_rotation);
                if (!(val is ValVector input)) return Intrinsic.Result.Null;

                var engObj = GetEngineObject(context, context.self);
                if (EngineInternal.TryGetTransform(engObj, out var transform))
                {
                    transform.rotation = new EngineInternal.Quaternion((float)input.X, (float)input.Y, (float)input.Z, (float)input.W);
                }

                return Intrinsic.Result.Null;
            };

            f = Intrinsic.Create(func_SetLocalScale);
            f.AddParam(var_self);
            f.AddParam(var_scale);
            f.code = (context, partialResult) => {

                var val = context.GetLocal(var_scale);
                if (!(val is ValVector input)) return Intrinsic.Result.Null;

                var engObj = GetEngineObject(context, context.self);
                if (EngineInternal.TryGetTransform(engObj, out var transform))
                {
                    transform.localScale = new EngineInternal.Vector3((float)input.X, (float)input.Y, (float)input.Z);
                }

                return Intrinsic.Result.Null;
            };

            #endregion

            #endregion

            #region Cameras
             
            // GetMainCamera
            // Returns the main play mode camera in the scene
            f = Intrinsic.Create(func_GetMainCamera);
            f.code = (context, partialResult) =>
            {
                return new Intrinsic.Result(ConvertCamera(context, swole.Engine.PlayModeCamera));
            };

            #endregion

            #region Creations

            // GetGameplayExperience
            //	Gets the top level creation instance running the gameplay experience
            f = Intrinsic.Create(func_GetGameplayExperience);
            f.AddParam(var_self);
            f.code = (context, partialResult) =>
            {
                object host = GetEngineObject(context, context.self);
                if (swole.IsNull(host)) host = context.interpreter.hostData;

                if (host is IExecutableGameplayExperience ge) return new Intrinsic.Result(ConvertCreationInstance(context, ge.CreationInstance));
                if (host != null) 
                {
                    var experience = swole.Engine.GetGameplayExperienceRoot(host);
                    if (swole.IsNotNull(experience)) return new Intrinsic.Result(ConvertCreationInstance(context, experience));                  
                }

                return Intrinsic.Result.Null;
            };

            // GetRootCreationInstance
            //	Gets the root level creation instance for this environment
            f = Intrinsic.Create(func_GetRootCreationInstance);
            f.AddParam(var_self);
            f.code = (context, partialResult) =>
            {
                object host = GetEngineObject(context, context.self);
                if (swole.IsNull(host)) host = context.interpreter.hostData;

                if (host is IExecutableGameplayExperience ge) return new Intrinsic.Result(ConvertCreationInstance(context, ge.CreationInstance));
                if (host != null)
                {
                    var experience = swole.Engine.GetRootCreationInstance(host);
                    if (swole.IsNotNull(experience)) return new Intrinsic.Result(ConvertCreationInstance(context, experience));
                }

                return Intrinsic.Result.Null;
            };

            // FindSwoleGameObject
            //	Attempts to find a SwoleGameObject by swole id
            // Returns a SwoleGameObject or null if not found
            f = Intrinsic.Create(func_FindSwoleGameObject);
            f.AddParam(var_swoleID);
            f.code = (context, partialResult) =>
            {
                var val = context.GetLocal(var_swoleID);

                if (val is ValNumber vn)
                {
                    if (TryFindSwoleGameObject(vn.IntValue(), context.interpreter.hostData, out var engObj)) return new Intrinsic.Result(ConvertSwoleGameObject(context, engObj));
                }
                else if (val is ValString str)
                {
                    if (TryFindSwoleGameObject(str.value, context.interpreter.hostData, out var engObj)) return new Intrinsic.Result(ConvertSwoleGameObject(context, engObj));
                }

                return Intrinsic.Result.Null;
            };

            // FindSwoleGameObjectInCreation
            //	Attempts to find a SwoleGameObject parented to a specific creation by swole id
            // Returns a SwoleGameObject or null if not found
            f = Intrinsic.Create(func_FindSwoleGameObjectInCreation);
            f.AddParam(var_self);
            f.AddParam(var_swoleID);
            f.code = (context, partialResult) =>
            {
                var engObj = GetEngineObject(context, context.self);
                if (EngineInternal.TryGetCreationInstance(engObj, out var creation))
                {
                    var val = context.GetLocal(var_swoleID);
                    if (val is ValNumber vn)
                    {
                        if (TryFindSwoleGameObject(vn.IntValue(), creation, out var swlObj)) return new Intrinsic.Result(ConvertSwoleGameObject(context, swlObj));
                    }
                    else if (val is ValString str)
                    {
                        if (TryFindSwoleGameObject(str.value, creation, out var swlObj)) return new Intrinsic.Result(ConvertSwoleGameObject(context, swlObj));
                    }
                }
                else throw new RuntimeException("object is not a Creation instance");

                return Intrinsic.Result.Null;
            };

            #endregion

            #region Event Handlers

            List<Value> _tempArguments = new List<Value>();
            string ListenForEvents(TAC.Context context, bool isPreEvent)
            {
                if (context.interpreter.hostData is not IRuntimeEnvironment enviro) throw new RuntimeException("cannot listen for events in this environment");

                var handler = GetEventHandler(context, context.self);
                if (swole.IsNull(handler)) throw new RuntimeException("object is not an event handler");

                var func = context.GetLocal(var_func);
                if (func is not ValFunction function || function.function == null || function.function.parameters == null || function.function.parameters.Count < 1) throw new RuntimeException("listener must be a function with at least one parameter: eventName (string)");

                var interpreter = context.interpreter; 
                var machine = interpreter.vm;

                var debugNameVal = context.GetLocal(var_genericName);
                string debugName = debugNameVal == null ? function.ToString(machine) : debugNameVal.ToString(machine);

                RuntimeEventListener listener = null;
                ExecutionThrottle throttle = new ExecutionThrottle(_eventTimeout);
                void Listen(string eventName, float value, object sender)
                {
                    try
                    {
                        if (!ReferenceEquals(interpreter.vm, machine)) // interpreter got disposed or recompiled
                        {
                            enviro.UntrackEventListener(listener);
                            listener.Dispose();
                            return;
                        }

                        _tempArguments.Clear();
                        _tempArguments.Add(new ValString(eventName));
                        if (function.function.parameters.Count > 1) _tempArguments.Add(new ValNumber(value));
                        RunExternally(debugName, machine, function, null, _tempArguments, throttle);

                    } 
                    catch(Exception ex)
                    {
                        swole.LogError(ex);
                    }
                }
                listener = new RuntimeEventListener(isPreEvent, handler, Listen, function); 
                enviro.TrackEventListener(listener);

                return listener.ID;
            }
            // ListenForEvents
            // Subscribes the function to an event handler as a post-event listener
            // returns listener id (string), which can be used to unsibscribe later
            f = Intrinsic.Create(func_ListenForEvents);
            f.AddParam(var_self); // handler
            f.AddParam(var_func); // listener
            f.AddParam(var_genericName); // optional debug name
            f.code = (context, partialResult) =>
            {
                return new Intrinsic.Result(new ValString(ListenForEvents(context, false))); 
            };
            // PreListenForEvents
            // Subscribes the function to an event handler as a pre-event listener
            // returns if successful
            f = Intrinsic.Create(func_PreListenForEvents);
            f.AddParam(var_self); // handler
            f.AddParam(var_func); // listener
            f.AddParam(var_genericName); // optional debug name
            f.code = (context, partialResult) =>
            {
                ListenForEvents(context, true);
                return Intrinsic.Result.True;
            };

            Intrinsic.Result StopListeningForEvents(TAC.Context context, bool isPreEvent)
            {
                if (context.interpreter.hostData is not IRuntimeEnvironment enviro) return Intrinsic.Result.False;

                var handler = GetEventHandler(context, context.self);
                if (swole.IsNull(handler)) throw new RuntimeException("object is not an event handler");


                var input = context.GetLocal(var_genericID);
                if (input is ValString id)
                {
                    return enviro.UntrackEventListener(id.value) ? Intrinsic.Result.True : Intrinsic.Result.False;
                } 
                else if (input is ValFunction func)
                {
                    var listener = isPreEvent ? enviro.FindPreEventListener(func, handler) : enviro.FindPostEventListener(func, handler);
                    return enviro.UntrackEventListener(listener) ? Intrinsic.Result.True : Intrinsic.Result.False;
                }

                return Intrinsic.Result.False;
            }
            // StopListeningForEvents
            // Unsubscribes post-event listener from the event handler
            f = Intrinsic.Create(func_StopListeningForEvents);
            f.AddParam(var_self); // handler
            f.AddParam(var_genericID); // listener id or function
            f.code = (context, partialResult) =>
            {
                return StopListeningForEvents(context, false);
            };
            // StopPreListeningForEvents
            // Unsubscribes pre-event listener from the event handler
            f = Intrinsic.Create(func_StopPreListeningForEvents);
            f.AddParam(var_self); // handler
            f.AddParam(var_genericID); // listener id or function
            f.code = (context, partialResult) =>
            {
                return StopListeningForEvents(context, true);
            };

            #endregion

            #region Assets

            // CreateAsset
            // Tries to create an asset of type and an optional name
            // Returns an asset of the type or null
            f = Intrinsic.Create(func_CreateAsset);
            f.AddParam(var_type);
            f.AddParam(var_genericName, "unnamed_asset");
            f.code = (context, partialResult) =>
            {
                if (context.interpreter.hostData is not IRuntimeHost hostData || !hostData.Scope.HasFlag(PermissionScope.CreateAssets)) 
                {
                    hostData = null;
                    var scope = PermissionScope.None;
                    if (context.interpreter.hostData is ICreationInstance ci)
                    {
                        hostData = swole.Engine.GetGameplayExperienceRoot(ci);
                        if (hostData != null) scope = hostData.Scope;  
                    }
                    if (!scope.HasFlag(PermissionScope.CreateAssets)) throw new RuntimeException("cannot create assets in this environment");             
                }

                var type = context.GetLocal(var_type);
                var assetName = context.GetLocal(var_genericName);
                if (assetName == null) assetName = ValString.empty;

                string typeName = "null";
                if (type is SwoleScriptType sst)
                {
                    if (sst.GlobalType == AnimationControllerType())
                    {
                        var asset = swole.Engine.CreateNewAnimationController(assetName.ToString(), hostData);
                        if (asset == null) throw new RuntimeException("cannot create assets in this environment");

                        return new Intrinsic.Result(ConvertAnimationController(context, asset));  
                    }

                    if (sst.GlobalType.TryGetValue(var_genericName, out Value typeNameValue) && typeNameValue != null)
                    {
                        typeName = typeNameValue.ToString(); // for error reporting
                    }
                } 
                else if (type != null)
                {
                    typeName = type.ToString(); // for error reporting
                }

                throw new RuntimeException($"'{typeName}' not is not an asset type");
            };

            // FindAsset
            // Tries to find an asset of type and name
            // Returns an asset of the type if found or null
            f = Intrinsic.Create(func_FindAsset);
            f.AddParam(var_type);
            f.AddParam(var_assetPath);
            f.AddParam(var_caseSensitive, 0);
            f.code = (context, partialResult) =>
            {

                var assetPath = context.GetLocal(var_assetPath);
                var type = context.GetLocal(var_type);
                var caseSensitive = context.GetLocal(var_caseSensitive);

                if (assetPath != null)
                {
                    IRuntimeHost host = null;
                    if (context.interpreter.hostData is IRuntimeHost) host = (IRuntimeHost)context.interpreter.hostData;

                    if (type is SwoleScriptType sst) 
                    {
                        if (sst.GlobalType == AnimationType())
                        {
                            var asset = FindAsset<IAnimationAsset>(assetPath.ToString(), host, caseSensitive == null ? false : caseSensitive.BoolValue());
                            if (asset != null) return new Intrinsic.Result(ConvertAnimation(context, asset)); 
                            
                        } 
                        else if (sst.GlobalType == AudioAssetType())
                        {
                            var asset = FindAsset<IAudioAsset>(assetPath.ToString(), host, caseSensitive == null ? false : caseSensitive.BoolValue());
                            if (asset != null) return new Intrinsic.Result(ConvertAudioAsset(context, asset));
                        }
                    }
                }

                return Intrinsic.Result.Null;

            };

            #endregion

            #endregion

#endif

        }

#if SWOLE_ENV

        public static AnimationLoopMode ConvertToLoopMode(this Value msValue)
        {
            AnimationLoopMode loopMode = AnimationLoopMode.Loop;
            if (msValue != null)
            {
                if (msValue is ValNumber num)
                {
                    try
                    {
                        loopMode = (AnimationLoopMode)num.IntValue();
                    }
                    catch (Exception)
                    {
                        throw new RuntimeException("invalid loop mode index");
                    }
                }
                else if (msValue is ValString str)
                {
                    if (!Enum.TryParse<AnimationLoopMode>(str.ToString(), true, out loopMode)) throw new RuntimeException($"invalid loop mode '{str.ToString()}'");
                }
            }
            return loopMode;
        }

        public static T FindAsset<T>(string assetPath, IRuntimeHost host, bool caseSensitive) where T : ISwoleAsset
        {
            T asset = default;

            var _asset = FindAsset(assetPath, typeof(T), host, caseSensitive);
            if (_asset is T) asset = (T)_asset;

            return asset;
        }
        public static ISwoleAsset FindAsset(string assetPath, Type type, IRuntimeHost host, bool caseSensitive)
        {
            if (!typeof(ISwoleAsset).IsAssignableFrom(type)) return default;

            var asset_res = swole.Engine.TryFindAsset(assetPath.ToString(), type, host, out ISwoleAsset asset, caseSensitive); 
            if (asset_res != EngineHook.FindAssetResult.Success)
            {
                switch (asset_res)
                {
                    default:
                        throw new RuntimeException($"unknown error occurred while fetching asset '{assetPath}'");

                    case EngineHook.FindAssetResult.InvalidType:
                        throw new RuntimeException("provided type is not a valid asset type");

                    case EngineHook.FindAssetResult.EmptyPath:
                        throw new RuntimeException("asset path was empty");

                    case EngineHook.FindAssetResult.InvalidHost:
                        throw new RuntimeException("invalid host environment");

                    case EngineHook.FindAssetResult.InvalidPackage:
                        throw new RuntimeException("invalid package id");

                    case EngineHook.FindAssetResult.NotFound:
                        throw new RuntimeException($"asset '{assetPath}' not found"); 

                    case EngineHook.FindAssetResult.PackageNotImported:
                        throw new RuntimeException($"package in path '{assetPath}' has not been imported as a dependency or does not exist. Use 'import package.name@version' to import it as a dependency first.");
                }
            }
             
            return asset; 
        }

        public static bool TryGetCachedReference<T>(Value val, out T output) where T : Value
        {
            output = default;

            if (val is T v)
            {
                output = v;
                return true;
            }
            else if (val is ValMap obj)
            {
                var reference = obj.Lookup(varStr_cachedReference);
                if (reference == null) reference = obj.Lookup(varStr_cachedEngineObject); 
                 
                if (reference is T cached)
                {
                    output = cached;
                    return true;
                }
            }

            return false;
        }
        public static bool TryGetCachedEngineReference<T>(Value val, out T output) where T : EngineInternal.IEngineObject
        {
            output = default;
             
            if (val is ValMap obj)
            {
                var reference = obj.Lookup(varStr_cachedEngineObject);
                if (reference == null) reference = obj.Lookup(varStr_cachedReference);

                if (reference is ValEngineObject cached && cached.engineObject is T realRef)
                {
                    output = realRef;
                    return true;
                }
            }

            return false;
        }

        public static IRuntimeEventHandler GetEventHandler(TAC.Context context, Value val)
        {
            if (val is ValMap obj)
            {
                Value engObjRef = obj.Lookup(varStr_cachedEventHandler);
                if (engObjRef is ValEventHandler handler) // Check if there is a cached instance reference
                {
                    return handler.eventHandler;
                }
            }

            return default;
        }

        public static EngineInternal.IEngineObject GetEngineObject(TAC.Context context, Value val)
        {

            if (val is ValMap obj)
            {
                Value engObjId = null;
                Value engObjRef = null;

                engObjRef = obj.Lookup(varStr_cachedEngineObject);
                if (engObjRef == null) obj.Lookup(varStr_cachedReference);
                if (engObjRef is ValEngineObject engObj) // Check if there is a cached instance reference
                {
                    return engObj.engineObject;   
                }

                engObjId = obj.Lookup(varStr_engineObjectID);
                if (engObjId is ValNumber id)
                {
                    if (context.interpreter.hostData is IRuntimeHost hostData)
                    {
#if FOUND_UNITY
                        var unityObj = FindUnityObject((int)id.value, hostData);
                        if (unityObj != null)
                        {
                            var proxy = UnityEngineHook.AsEngineObject(unityObj);
                            obj.map[varStr_cachedEngineObject] = new ValEngineObject(proxy); // Cache the instance reference for any other queries
                            return proxy;
                        }
#endif
                    }

                }
            }

            return default;
        }

        public static Value GetMSValueFromCSharpObject(TAC.Context context, object obj)
        {          
            if (obj is ITileInstance tileInstance) return ConvertTileInstance(context, tileInstance);
            if (obj is EngineInternal.ITransform transform) return ConvertTransform(context, transform); 

            if (obj is IAnimator animator) return ConvertAnimator(context, animator);

            if (obj is IRigidbody rigidbody) return ConvertRigidbody(context, rigidbody); 

            if (obj is IAudibleObject ao) return ConvertAudibleObject(context, ao);
            if (obj is IAudioSource as_) return ConvertAudioSource(context, as_);
            if (obj is IAudioAsset aua) return ConvertAudioAsset(context, aua);
            if (obj is IAudioBundle bundle) return ConvertAudioBundle(context, bundle);
            if (obj is IAudioMixerGroup mixer) return ConvertAudioMixerGroup(context, mixer);

            if (obj is EngineInternal.IComponent comp) return ConvertComponent(context, comp);

            if (obj is EngineInternal.SwoleGameObject sgo) return ConvertSwoleGameObject(context, sgo);
            if (obj is EngineInternal.GameObject go) return ConvertGameObject(context, go);

            if (obj is EngineInternal.RNG rng) return ConvertRNG(context, rng);  
            if (obj is EngineInternal.RNGState rngState) return ConvertRNGState(context, rngState);

            return null;
        }

#endif

        public static bool TryFindSwoleGameObject(int swoleId, object hostData, out EngineInternal.SwoleGameObject obj)
        {
            obj = default;
            if (hostData is ICreationInstance ci)
            {
                obj = ci.FindSwoleGameObject(swoleId);  
                return obj != null;
            }

            return false;
        }
        public static bool TryFindSwoleGameObject(string swoleIds, object hostData, out EngineInternal.SwoleGameObject obj)
        {
            obj = default;
            if (hostData is ICreationInstance ci)
            {
                obj = ci.FindSwoleGameObject(swoleIds);
                return obj != null;
            }

            return false;
        }
        public static bool TryFindGameObject(string name, object hostData, out EngineInternal.GameObject obj)
        {
            obj = default;
            if (hostData is ICreationInstance ci)
            {
                obj = ci.FindGameObject(name);
                return obj != null;
            } 
            else if (hostData is EngineInternal.GameObject go)
            {
                hostData = go.transform;
            } 
            
            if (hostData is EngineInternal.ITransform tr)
            {
                obj = UnityEngineHook.AsSwoleGameObject(tr.Find(name));
                return obj != null;
            }

            return false;
        }

#if FOUND_UNITY
        public static UnityEngine.Object FindUnityObject(int instanceId, IRuntimeHost host)
        {
            if (host != null && host.Scope != PermissionScope.None)
            {
                var obj = Resources.InstanceIDToObject(instanceId);
                if (obj == null) return null;

                var scope = host.Scope;
                if (scope.HasFlag(PermissionScope.Admin)) return obj;

                GameObject hostObj = null;
                if (host is Component hostComp) hostObj = hostComp.gameObject;


                GameObject gameObject = null;

                if (obj is GameObject) gameObject = (GameObject)obj;
                if (obj is Component comp) gameObject = comp.gameObject;

                if (gameObject != null)
                {
                    var transform = gameObject.transform;
                    if (hostObj != null)
                    {
                        if (hostObj == gameObject) return obj;

                        if (scope.HasFlag(PermissionScope.SceneOnly) && gameObject.scene != hostObj.scene) return null;
                        if (scope.HasFlag(PermissionScope.ObjectOnly) && !transform.IsChildOf(hostObj.transform)) return null;
                    }

                    if (scope.HasFlag(PermissionScope.ActiveOnly) && !gameObject.activeInHierarchy) return null;

                    if (scope.HasFlag(PermissionScope.ExperienceOnly) && host is CreationBehaviour cb)
                    {
                        var root = cb.RootCreation;
                        if (root == null) return null;
                        var rootObj = root.Root;
                        if (rootObj == null) return null;

                        if (rootObj != gameObject && !UnityEngineHook.AsSwoleTransform(transform).IsChildOf(rootObj.transform)) return null;  
                    }
                } 
                else
                {
                    if (scope.HasFlag(PermissionScope.SceneOnly)) return null;
                    if (scope.HasFlag(PermissionScope.ObjectOnly)) return null;
                    if (scope.HasFlag(PermissionScope.ExperienceOnly)) return null;  
                }

                return obj;
            }

            return null;
        }
#endif

#if SWOLE_ENV

        public abstract class ValReference : Value
        {
            public abstract object Reference { get; }
        }

        public class ValCancellationToken : ValReference
        {
            public SwoleCancellationToken reference;
            public override object Reference => reference;

            public ValCancellationToken(SwoleCancellationToken reference)
            {
                this.reference = reference;
            }
            public override double Equality(Value rhs)
            {
                if (rhs is ValCancellationToken obj) return reference == obj.reference ? 1 : 0;
                return 0;
            }
            public override int Hash() => ReferenceEquals(reference, null) ? base.GetHashCode() : reference.GetHashCode();
            public override string ToString(TAC.Machine vm) => $"cancellationToken[{Hash()}]";
        }
        public static ValMap ConvertCancellationToken(SwoleCancellationToken token)
        {
            var map = new ValMap();
            AddLocalIntrinsicToObject(map, func_CancelToken, "Cancel");
            map.map[varStr_cachedReference] = new ValCancellationToken(token);
            return map;
        }

        public class ValRNG : ValReference
        {
            public EngineInternal.RNG reference;
            public override object Reference => reference;

            public ValRNG(EngineInternal.RNG reference)
            {
                this.reference = reference;
            }
            public override double Equality(Value rhs)
            {
                if (rhs is ValRNG obj) return reference == obj.reference ? 1 : 0;
                return 0;
            }
            public override int Hash() => ReferenceEquals(reference, null) ? base.GetHashCode() : reference.GetHashCode();
            public override string ToString(TAC.Machine vm) => $"rng[{Hash()}]";
        }
        public class ValRNGState : ValReference
        {
            public EngineInternal.RNGState reference;
            public override object Reference => reference;

            public ValRNGState(EngineInternal.RNGState reference)
            {
                this.reference = reference;
            }
            public override double Equality(Value rhs)
            {
                if (rhs is ValRNGState obj) return reference == obj.reference ? 1 : 0;
                return 0;
            }
            public override int Hash() => ReferenceEquals(reference, null) ? base.GetHashCode() : reference.GetHashCode();
            public override string ToString(TAC.Machine vm) => $"rngState[{Hash()}]";
        }

        public class ValInputManager : ValReference
        {
            public IInputManager reference;
            public override object Reference => reference;

            public ValInputManager(IInputManager reference)
            {
                this.reference = reference;
            }
            public override double Equality(Value rhs)
            {
                if (rhs is ValInputManager obj) return reference == obj.Reference ? 1 : 0;
                return 0;
            }
            public override int Hash() => ReferenceEquals(reference, null) ? base.GetHashCode() : reference.GetHashCode();
            public override string ToString(TAC.Machine vm) => $"input_manager[{Hash()}]";
        }

        public class ValEventHandler : ValReference
        {
            public IRuntimeEventHandler eventHandler;
            public override object Reference => eventHandler;

            public ValEventHandler(IRuntimeEventHandler eventHandler)
            {
                this.eventHandler = eventHandler;
            }
            public override double Equality(Value rhs)
            {
                if (rhs is ValEventHandler obj) return eventHandler == obj.eventHandler ? 1 : 0;
                return 0;
            }
            public override int Hash() => ReferenceEquals(eventHandler, null) ? base.GetHashCode() : eventHandler.GetHashCode();
            public override string ToString(TAC.Machine vm) => $"runtime_event_handler[{Hash()}]";
        }

        public class ValSwoleAsset : ValReference
        {
            public ISwoleAsset reference;
            public override object Reference => reference;

            public ValSwoleAsset(ISwoleAsset asset)
            {
                this.reference = asset;
            }

            public override double Equality(Value rhs)
            {
                if (rhs is ValSwoleAsset obj) return obj.reference == reference ? 1 : 0;
                return 0;
            }
            public override int Hash() => reference == null ? base.GetHashCode() : reference.GetHashCode();
            public override string ToString(TAC.Machine vm) => $"swole_asset[{(swole.Engine.IsNull(reference) ? "null" : reference.Name)}]";
        }
        public class ValContent : ValSwoleAsset
        {
            public IContent Content => reference is IContent content ? content : null;
            public ValContent(IContent content) : base(content){}
            public override string ToString(TAC.Machine vm) => $"content[{(swole.Engine.IsNull(Content) ? "null" : ((Content.PackageInfo.NameIsValid ? (Content.PackageInfo.ToString() + "/") : "") + reference.Name))}]";
        }

        public class ValEngineObject : ValReference
        {
            public EngineInternal.IEngineObject engineObject;
            public override object Reference => engineObject;

            public ValEngineObject(EngineInternal.IEngineObject engineObject)
            {
                this.engineObject = engineObject;
            }
            public override double Equality(Value rhs)
            {
                if (rhs is ValEngineObject obj) return engineObject == obj.engineObject ? 1 : 0;

                return 0; 
            }
            public override int Hash() => ReferenceEquals(engineObject, null) ? base.GetHashCode() : engineObject.GetHashCode();
            public override string ToString(TAC.Machine vm) => $"engineObj[{(swole.Engine.IsNull(engineObject) ? "null" : engineObject.InstanceID)}]"; 
        }

        #region Types

        public delegate bool AssignmentOverrideDelegate(ValMap instance, Value value);

        public static void AddLocalIntrinsicToObject(ValMap obj, string intrinsicName, string localFuncName = null) => AddLocalIntrinsicToObject(obj, Intrinsic.GetByName(intrinsicName), localFuncName);
        public static void AddLocalIntrinsicToObject(ValMap obj, Intrinsic intrinsic, string localFuncName = null)
        {
            if (obj == null || intrinsic == null) return;
            obj[string.IsNullOrWhiteSpace(localFuncName) ? intrinsic.name : localFuncName] = intrinsic.GetFunc();
        }
        public static Intrinsic CreateNewLocalIntrinsic(ValMap obj, string localFuncName)
        {
            var intrinsic = Intrinsic.Create(string.Empty);
            AddLocalIntrinsicToObject(obj, intrinsic, localFuncName);
            return intrinsic;
        }
        public static Intrinsic CreateNewIntrinsicAndAddLocally(ValMap obj, string intrinsicName, string localFuncName = null)
        {
            var intrinsic = Intrinsic.Create(intrinsicName);
            AddLocalIntrinsicToObject(obj, intrinsic, localFuncName);
            return intrinsic;
        }

        public static bool DefaultOverrideDelegate(ValMap instance, Value value) => true;
        public static Intrinsic.Result DefaultIntrinsicReturnNull(TAC.Context context, Intrinsic.Result partialResult) => Intrinsic.Result.Null;

        public static void AddLocalIntrinsicToObjectWithAssignOverride(SwoleScriptType obj, string intrinsicName, string localFuncName, Dictionary<string, AssignmentOverrideDelegate> assignmentOverrideDelegates, AssignmentOverrideDelegate overrideDelegate, bool forceSetOnCreateNewObject = false) => AddLocalIntrinsicToObjectWithAssignOverride(obj, Intrinsic.GetByName(intrinsicName), localFuncName, assignmentOverrideDelegates, overrideDelegate, forceSetOnCreateNewObject);
        public static Intrinsic AddLocalIntrinsicToObjectWithAssignOverride(SwoleScriptType obj, Intrinsic intrinsic, string localFuncName, Dictionary<string, AssignmentOverrideDelegate> assignmentOverrideDelegates, AssignmentOverrideDelegate overrideDelegate, bool forceSetOnCreateNewObject = false)
        {
            if (string.IsNullOrWhiteSpace(localFuncName)) localFuncName = intrinsic.name;
            AddLocalIntrinsicToObject(obj, intrinsic, localFuncName);

            if (overrideDelegate == null) overrideDelegate = DefaultOverrideDelegate;
            assignmentOverrideDelegates[localFuncName] = overrideDelegate;
            if (forceSetOnCreateNewObject || obj.onCreateNewObject == null)
            {
                obj.onCreateNewObject = (TAC.Context context, ValMap instance) =>
                {
                    instance.assignOverride = (Value key, Value value) =>
                    {
                        if (key != null)
                        {
                            string keyStr = key.ToString();
                            if (assignmentOverrideDelegates.TryGetValue(keyStr, out var del)) return del(instance, value);
                        }
                        return false;
                    };
                };
            }

            return intrinsic;
        }
        public static Intrinsic CreateNewLocalIntrinsicWithAssignOverride(SwoleScriptType obj, string localFuncName, Dictionary<string, AssignmentOverrideDelegate> assignmentOverrideDelegates, AssignmentOverrideDelegate overrideDelegate, bool forceSetOnCreateNewObject = false)
        {
            var intrinsic = Intrinsic.Create(string.Empty);
            AddLocalIntrinsicToObject(obj, intrinsic, localFuncName);

            if (overrideDelegate == null) overrideDelegate = DefaultOverrideDelegate;
            assignmentOverrideDelegates[localFuncName] = overrideDelegate;
            if (forceSetOnCreateNewObject || obj.onCreateNewObject == null)
            {
                obj.assignOverride = (Value key, Value value) =>
                {
                    if (key != null)
                    {
                        string keyStr = key.ToString();
                        if (assignmentOverrideDelegates.TryGetValue(keyStr, out var del)) return del(obj, value);
                    }
                    return false;
                };

                obj.onCreateNewObject = (TAC.Context context, ValMap instance) =>
                {
                    instance.assignOverride = (Value key, Value value) =>
                    {
                        if (key != null)
                        {
                            string keyStr = key.ToString();
                            if (assignmentOverrideDelegates.TryGetValue(keyStr, out var del)) return del(instance, value);
                        }
                        return false;
                    };
                };
            }

            return intrinsic;
        }

        public static void CopyParentObjectIntoChildObject(ValMap ms_obj, ValMap parent_obj)
        {
            foreach (var pair in parent_obj.map)
            {
                if (pair.Key == ValString.magicIsA) continue; // Don't copy base type

                ms_obj.map[pair.Key] = pair.Value;
            }

            if (ms_obj.assignOverride == null)
            {
                ms_obj.assignOverride = parent_obj.assignOverride; // Use same assignOverride for now
            }
            else if (parent_obj.assignOverride != null)
            {
                var existingOverride = ms_obj.assignOverride;
                ms_obj.assignOverride = (Value key, Value val) =>
                {
                    if (existingOverride != null && existingOverride(key, val)) return true;
                    if (parent_obj.assignOverride != null && parent_obj.assignOverride(key, val)) return true;
                    return false;
                };
            }
        }

        private delegate object GetValueFromReflectionInfoDelegate(TAC.Context context, object obj, object[] index);
        private delegate void SetValueOfReflectionInfoDelegate(object obj, object value);
        private static IntrinsicCode CreateGetter(bool isStatic, Type realType, GetValueFromReflectionInfoDelegate GetValue)
        {
            IntrinsicCode getter = DefaultIntrinsicReturnNull;

            object GetInstance(TAC.Context context)
            {
                if (!isStatic && TryGetCachedReference<ValReference>(context.self, out var reference)) 
                {
                    var inst = reference.Reference;
                    if (swole.IsNull(inst)) throw new RuntimeException("invalid reference");
                    return inst; 
                }
                return null;
            }

            if (realType == typeof(void))
            {
                getter = (TAC.Context context, Intrinsic.Result partialResult) =>
                {
                    GetValue(context, GetInstance(context), null); 
                    return Intrinsic.Result.Null;
                };
            }
            if (realType.IsNumeric())
            {
                getter = (TAC.Context context, Intrinsic.Result partialResult) => new Intrinsic.Result(new ValNumber((float)Convert.ChangeType(GetValue(context, GetInstance(context), null), typeof(float))));  
            }
            else if (realType == typeof(bool))
            {
                getter = (TAC.Context context, Intrinsic.Result partialResult) => new Intrinsic.Result(ValNumber.Truth((bool)GetValue(context, GetInstance(context), null)));
            }
            else if (realType.IsEnum)
            {
                getter = (TAC.Context context, Intrinsic.Result partialResult) => new Intrinsic.Result(new ValString(GetValue(context, GetInstance(context), null).ToString()));
            }
            else if (realType == typeof(EngineInternal.Vector2))
            {
                getter = (TAC.Context context, Intrinsic.Result partialResult) =>
                {
                    var v = (EngineInternal.Vector2)GetValue(context, GetInstance(context), null);
                    return new Intrinsic.Result(new ValVector(v.x, v.y));
                };
            }
            else if (realType == typeof(EngineInternal.Vector3))
            {
                getter = (TAC.Context context, Intrinsic.Result partialResult) =>
                {
                    var v = (EngineInternal.Vector3)GetValue(context, GetInstance(context), null);
                    return new Intrinsic.Result(new ValVector(v.x, v.y, v.z));
                };
            }
            else if (realType == typeof(EngineInternal.Vector4))
            {
                getter = (TAC.Context context, Intrinsic.Result partialResult) =>
                {
                    var v = (EngineInternal.Vector4)GetValue(context, GetInstance(context), null);
                    return new Intrinsic.Result(new ValVector(v.x, v.y, v.z, v.w));
                };
            }
            else if (realType == typeof(string))
            {
                getter = (TAC.Context context, Intrinsic.Result partialResult) => new Intrinsic.Result(new ValString((string)GetValue(context, GetInstance(context), null)));
            }
            else if (realType == typeof(EngineInternal.Quaternion))
            {
                getter = (TAC.Context context, Intrinsic.Result partialResult) =>
                {
                    var v = (EngineInternal.Quaternion)GetValue(context, GetInstance(context), null);
                    return new Intrinsic.Result(new ValQuaternion(v.x, v.y, v.z, v.w));
                };
            }
            else if (realType == typeof(EngineInternal.Matrix4x4))
            {
                getter = (TAC.Context context, Intrinsic.Result partialResult) =>
                {
                    var m = (EngineInternal.Matrix4x4)GetValue(context, GetInstance(context), null);
                    return new Intrinsic.Result(new ValMatrix(m.m00, m.m01, m.m02, m.m03, m.m10, m.m11, m.m12, m.m13, m.m20, m.m21, m.m22, m.m23, m.m30, m.m31, m.m32, m.m33));
                };
            }
            else if (realType == typeof(EngineInternal.RNG))
            {
                getter = (TAC.Context context, Intrinsic.Result partialResult) =>
                {
                    var res = GetValue(context, GetInstance(context), null);
                    if (res == null) return Intrinsic.Result.Null;
                    var val = (EngineInternal.RNG)res;
                    return new Intrinsic.Result(ConvertRNG(context, val));
                };
            }
            else if (realType == typeof(EngineInternal.RNGState))
            {
                getter = (TAC.Context context, Intrinsic.Result partialResult) =>
                {
                    var res = GetValue(context, GetInstance(context), null);
                    if (res == null) return Intrinsic.Result.Null;
                    var val = (EngineInternal.RNGState)res;
                    return new Intrinsic.Result(ConvertRNGState(context, val));
                };
            }
            else if (realType == typeof(IAudioSource))
            {
                getter = (TAC.Context context, Intrinsic.Result partialResult) =>
                {
                    var res = GetValue(context, GetInstance(context), null);
                    if (res == null) return Intrinsic.Result.Null; 
                    var val = (IAudioSource)res;
                    return new Intrinsic.Result(ConvertAudioSource(context, val));
                };
            }
            else if (realType == typeof(IAudioAsset))
            {
                getter = (TAC.Context context, Intrinsic.Result partialResult) =>
                {
                    var res = GetValue(context, GetInstance(context), null);
                    if (res == null) return Intrinsic.Result.Null;
                    var val = (IAudioAsset)res;
                    return new Intrinsic.Result(ConvertAudioAsset(context, val));
                };
            }

            return getter;
        }
        private static AssignmentOverrideDelegate CreateSetter(bool isStatic, Type realType, string propName, SetValueOfReflectionInfoDelegate SetValue)
        {
            AssignmentOverrideDelegate setter = DefaultOverrideDelegate;

            object GetInstance(ValMap instance)
            {
                if (!isStatic && TryGetCachedReference<ValReference>(instance, out var reference)) 
                {
                    var inst = reference.Reference;
                    if (swole.IsNull(inst)) throw new RuntimeException("invalid reference"); 
                    return inst;
                }
                return null;
            }

            if (realType.IsNumeric())
            {
                setter = (ValMap instance, Value value) =>
                {
                    try
                    {
                        SetValue(GetInstance(instance), Convert.ChangeType(value.FloatValue(), realType));
                    }
                    catch (Exception)
                    {
                        throw new RuntimeException($"tried to set {propName} to an invalid type");
                    }
                    return true;
                };
            }
            else if (realType == typeof(bool))
            {
                setter = (ValMap instance, Value value) =>
                {
                    try
                    {
                        SetValue(GetInstance(instance), value.BoolValue());
                    }
                    catch (Exception)
                    {
                        throw new RuntimeException($"tried to set {propName} to an invalid type");
                    }
                    return true;
                };
            }
            else if (realType.IsEnum)
            {
                setter = (ValMap instance, Value value) =>
                {
                    try
                    {
                        SetValue(GetInstance(instance), Enum.Parse(realType, value.ToString(), true));
                    }
                    catch (Exception)
                    {
                        throw new RuntimeException($"tried to set {propName} to an invalid type");
                    }
                    return true;
                };
            }
            else if (realType == typeof(EngineInternal.Vector2))
            {
                setter = (ValMap instance, Value value) =>
                {
                    if (value is ValVector v)
                    {
                        try
                        {
                            SetValue(GetInstance(instance), new EngineInternal.Vector2((float)v.X, (float)v.Y));
                        }
                        catch (Exception)
                        {
                            throw new RuntimeException($"tried to set {propName} to an invalid type");
                        }
                    }
                    else throw new RuntimeException($"tried to set {propName} to an invalid type");
                    return true;
                };
            }
            else if (realType == typeof(EngineInternal.Vector3))
            {
                setter = (ValMap instance, Value value) =>
                {
                    if (value is ValVector v)
                    {
                        try
                        {
                            SetValue(GetInstance(instance), new EngineInternal.Vector3((float)v.X, (float)v.Y, (float)v.Z));
                        }
                        catch (Exception)
                        {
                            throw new RuntimeException($"tried to set {propName} to an invalid type");
                        }
                    }
                    else throw new RuntimeException($"tried to set {propName} to an invalid type");
                    return true;
                };
            }
            else if (realType == typeof(EngineInternal.Vector4))
            {
                setter = (ValMap instance, Value value) =>
                {
                    if (value is ValVector v)
                    {
                        try
                        {
                            SetValue(GetInstance(instance), new EngineInternal.Vector4((float)v.X, (float)v.Y, (float)v.Z, (float)v.W));
                        }
                        catch (Exception)
                        {
                            throw new RuntimeException($"tried to set {propName} to an invalid type");
                        }
                    }
                    else throw new RuntimeException($"tried to set {propName} to an invalid type");
                    return true;
                };
            }
            else if (realType == typeof(string))
            {
                setter = (ValMap instance, Value value) =>
                {
                    try
                    {
                        SetValue(GetInstance(instance), value.ToString());
                    }
                    catch (Exception)
                    {
                        throw new RuntimeException($"tried to set {propName} to an invalid type");
                    }
                    return true;
                };
            }
            else if (realType == typeof(EngineInternal.Quaternion))
            {
                setter = (ValMap instance, Value value) =>
                {
                    if (value is ValVector v) // ValQuaternion is a ValVector
                    {
                        try
                        {
                            SetValue(GetInstance(instance), new EngineInternal.Quaternion((float)v.X, (float)v.Y, (float)v.Z, (float)v.W));
                        }
                        catch (Exception)
                        {
                            throw new RuntimeException($"tried to set {propName} to an invalid type");
                        }
                    }
                    else throw new RuntimeException($"tried to set {propName} to an invalid type"); 
                    return true;
                };
            }
            else if (realType == typeof(EngineInternal.Matrix4x4))
            {
                setter = (ValMap instance, Value value) =>
                {
                    if (value is ValMatrix m)
                    {
                        try
                        {
                            SetValue(GetInstance(instance), new EngineInternal.Matrix4x4(
                                new EngineInternal.Vector4((float)m.c0.X, (float)m.c0.Y, (float)m.c0.Z, (float)m.c0.W),
                                new EngineInternal.Vector4((float)m.c1.X, (float)m.c1.Y, (float)m.c1.Z, (float)m.c1.W),
                                new EngineInternal.Vector4((float)m.c2.X, (float)m.c2.Y, (float)m.c2.Z, (float)m.c2.W),
                                new EngineInternal.Vector4((float)m.c3.X, (float)m.c3.Y, (float)m.c3.Z, (float)m.c3.W)
                                ));
                        }
                        catch (Exception)
                        {
                            throw new RuntimeException($"tried to set {propName} to an invalid type");
                        }
                    }
                    else throw new RuntimeException($"tried to set {propName} to an invalid type");
                    return true;
                };
            }
            else if (realType == typeof(EngineInternal.RNG))
            {
                setter = (ValMap instance, Value value) =>
                {
                    if (TryGetCachedReference<ValRNG>(value, out var val))
                    {
                        try
                        {
                            SetValue(GetInstance(instance), val.Reference);
                        }
                        catch (Exception)
                        {
                            throw new RuntimeException($"tried to set {propName} to an invalid type");
                        }
                    }
                    else throw new RuntimeException($"tried to set {propName} to an invalid type");
                    return true;
                };
            }
            else if (realType == typeof(EngineInternal.RNGState))
            {
                setter = (ValMap instance, Value value) =>
                {
                    if (TryGetCachedReference<ValRNGState>(value, out var val))
                    {
                        try
                        {
                            SetValue(GetInstance(instance), val.Reference);
                        }
                        catch (Exception)
                        {
                            throw new RuntimeException($"tried to set {propName} to an invalid type");
                        }
                    }
                    else throw new RuntimeException($"tried to set {propName} to an invalid type");
                    return true;
                };
            }
            else if (typeof(EngineInternal.IEngineObject).IsAssignableFrom(realType))
            {
                setter = (ValMap instance, Value value) =>
                {
                    if (TryGetCachedReference<ValEngineObject>(value, out var val))
                    {
                        try
                        {
                            SetValue(GetInstance(instance), val.Reference);
                        }
                        catch (Exception)
                        {
                            throw new RuntimeException($"tried to set {propName} to an invalid type");
                        }
                    }
                    else throw new RuntimeException($"tried to set {propName} to an invalid type");
                    return true;
                };
            }
            else if (typeof(IAudioAsset).IsAssignableFrom(realType))
            {
                setter = (ValMap instance, Value value) =>
                {
                    if (TryGetCachedReference<ValAudioAsset>(value, out var val))
                    {
                        try
                        {
                            SetValue(GetInstance(instance), val.Reference);
                        }
                        catch (Exception)
                        {
                            throw new RuntimeException($"tried to set {propName} to an invalid type");
                        }
                    }
                    else throw new RuntimeException($"tried to set {propName} to an invalid type");
                    return true;
                };
            }

            return setter;
        }

        public static void AddPropertiesAsLocalIntrinsics(SwoleScriptType sst, PropertyInfo[] props, ref Dictionary<string, AssignmentOverrideDelegate> assignOverrideDelegates, bool forceSetFirstLetterToLowerCase = true)
        {
            if (sst == null || props == null) return;

            if (assignOverrideDelegates == null) assignOverrideDelegates = new Dictionary<string, AssignmentOverrideDelegate>();

            Intrinsic f;
            foreach (var prop_ in props)
            {
                var prop = prop_;
                if (prop == null) continue;

                var ignoreAtt = Attribute.GetCustomAttribute(prop, typeof(SwoleScriptIgnoreAttribute));
                if (ignoreAtt != null) continue; // ignore it

                string name = prop.Name;
                if (forceSetFirstLetterToLowerCase && name != null && name.Length > 0) name = name.Substring(0, 1).ToLower() + name.Substring(1);

                AssignmentOverrideDelegate setter = DefaultOverrideDelegate;
                IntrinsicCode getter = DefaultIntrinsicReturnNull;

                var getMethod = prop.GetGetMethod();
                var setMethod = prop.GetSetMethod();

                var realType = prop.PropertyType;
                if (getMethod != null && Attribute.GetCustomAttribute(getMethod, typeof(SwoleScriptIgnoreAttribute)) == null)
                {
                    getter = CreateGetter(getMethod.IsStatic, realType, (TAC.Context context, object obj, object[] index) => prop.GetValue(obj, index)); 
                }
                if (setMethod != null && Attribute.GetCustomAttribute(setMethod, typeof(SwoleScriptIgnoreAttribute)) == null)
                {
                    setter = CreateSetter(setMethod.IsStatic, realType, name, prop.SetValue);
                }
                if (setter == DefaultOverrideDelegate && getter == DefaultIntrinsicReturnNull) continue; // No point in adding it

                f = CreateNewLocalIntrinsicWithAssignOverride(sst, name, assignOverrideDelegates, setter);
                f.AddParam(var_self);
                f.code = getter;

            }
        }
        public static void AddFieldsAsLocalIntrinsics(SwoleScriptType sst, FieldInfo[] fields, ref Dictionary<string, AssignmentOverrideDelegate> assignOverrideDelegates, bool forceSetFirstLetterToLowerCase = true)
        {
            if (sst == null || fields == null) return;

            if (assignOverrideDelegates == null) assignOverrideDelegates = new Dictionary<string, AssignmentOverrideDelegate>();

            Intrinsic f;
            foreach (var field_ in fields)
            {
                var field = field_;
                if (field == null) continue;

                var ignoreAtt = Attribute.GetCustomAttribute(field, typeof(SwoleScriptIgnoreAttribute));
                if (ignoreAtt != null) continue; // ignore it

                string name = field.Name;
                if (forceSetFirstLetterToLowerCase && name != null && name.Length > 0) name = name.Substring(0, 1).ToLower() + name.Substring(1);

                AssignmentOverrideDelegate setter = DefaultOverrideDelegate;
                IntrinsicCode getter = DefaultIntrinsicReturnNull;

                var realType = field.FieldType;
                getter = CreateGetter(field.IsStatic, realType, (TAC.Context context, object inst, object[] index) => field.GetValue(inst));
                setter = CreateSetter(field.IsStatic, realType, name, field.SetValue);

                f = CreateNewLocalIntrinsicWithAssignOverride(sst, name, assignOverrideDelegates, setter);
                f.AddParam(var_self);
                f.code = getter;

            }
        }
        public static void AddMethodsAsLocalIntrinsics(SwoleScriptType sst, MethodInfo[] methods, ref Dictionary<string, AssignmentOverrideDelegate> assignOverrideDelegates)
        {
            if (sst == null || methods == null) return;

            if (assignOverrideDelegates == null) assignOverrideDelegates = new Dictionary<string, AssignmentOverrideDelegate>();

            Intrinsic f;
            foreach (var method_ in methods)
            {
                var method = method_;
                if (method == null) continue;

                var ignoreAtt = Attribute.GetCustomAttribute(method, typeof(SwoleScriptIgnoreAttribute));
                if (ignoreAtt != null) continue; // ignore it

                string name = method.Name;
                var args = method.GetParameters();

                object[] inputArgs = new object[args.Length];
                GetValueFromReflectionInfoDelegate GetVal = (TAC.Context context, object inst, object[] index) =>
                {
                    try
                    {
                        for (int a = 0; a < args.Length; a++)
                        {
                            var info = args[a];
                            inputArgs[a] = null;
                            if (info == null) continue;

                            var valParam = context.GetLocal(info.Name);
                            if (valParam != null)
                            {
                                object obj;
                                if (TryGetCachedReference<ValReference>(valParam, out var valRef))
                                {
                                    obj = valRef.Reference;
                                }
                                else
                                {
                                    obj = Scripting.FromValueMS(valParam);
                                }
                                if (obj != null && info.ParameterType.IsNumeric())
                                {
                                    inputArgs[a] = Convert.ChangeType(obj, info.ParameterType);
                                }
                                else
                                {
                                    inputArgs[a] = obj;
                                }
                            } 
                            else
                            {
                                if (info.HasDefaultValue) inputArgs[a] = info.DefaultValue;
                            }

                        }
                        return method.Invoke(inst, inputArgs);
                    }
                    catch (Exception ex)
                    {
                        throw new RuntimeException($"error invoking external function {name}", ex);   
                    }
                };

                var realType = method.ReturnType;

                string origName = name;
                int i = 1;
                while (sst.TryGetValue(name, out _)) // increment method name if it exists
                {
                    i++;
                    name = origName + i;
                }
                f = CreateNewLocalIntrinsicWithAssignOverride(sst, name, assignOverrideDelegates, DefaultOverrideDelegate);
                f.AddParam(var_self);
                if (args != null) for (int a = 0; a < args.Length; a++) f.AddParam(args[a].Name);
                f.code = CreateGetter(method.IsStatic, realType, GetVal);
            }
        }

        public static void TransferCompatibleTypeImplementations(SwoleScriptType sst, Type type, Dictionary<string, AssignmentOverrideDelegate> assignOverrideDelegates)
        {        
            if (assignOverrideDelegates == null) assignOverrideDelegates = new Dictionary<string, AssignmentOverrideDelegate>();

            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

            AddFieldsAsLocalIntrinsics(sst, fields, ref assignOverrideDelegates);
            AddPropertiesAsLocalIntrinsics(sst, properties, ref assignOverrideDelegates);
            AddMethodsAsLocalIntrinsics(sst, methods, ref assignOverrideDelegates);
        }

        #region Engine Types

        public static SwoleScriptType RNGType()
        {
            if (_rngType == null)
            {
                var type = typeof(EngineInternal.RNG); 
                var typeName = new ValString(type_RNG);
                _rngType = new SwoleScriptType(type_RNG, type); 
                _rngType[var_genericName] = typeName;

                //Intrinsic f;
                Dictionary<string, AssignmentOverrideDelegate> assignOverrideDelegates = new Dictionary<string, AssignmentOverrideDelegate>();
                TransferCompatibleTypeImplementations(_rngType, type, assignOverrideDelegates);
                 
                _rngType.map[varStr_cachedReference] = new ValRNG(EngineInternal.RNG.Global);

            }
            return _rngType;
        }
        static SwoleScriptType _rngType = null;
        public static ValMap ConvertRNG(TAC.Context context, EngineInternal.RNG rng)
        {
            if (swole.IsNull(rng)) return null;

            var ms_obj = RNGType().NewObject(context);

            // Set rng object data
            ms_obj.map[varStr_cachedReference] = new ValRNG(rng);

            return ms_obj;
        }

        public static SwoleScriptType RNGStateType()
        {
            if (_rngStateType == null)
            {
                var type = typeof(EngineInternal.RNGState);
                var typeName = new ValString(type_RNGState);
                _rngStateType = new SwoleScriptType(type_RNGState, type);
                _rngStateType[var_genericName] = typeName;

                //Intrinsic f;
                Dictionary<string, AssignmentOverrideDelegate> assignOverrideDelegates = new Dictionary<string, AssignmentOverrideDelegate>();
                TransferCompatibleTypeImplementations(_rngStateType, type, assignOverrideDelegates);

            }
            return _rngStateType;
        }
        static SwoleScriptType _rngStateType = null;
        public static ValMap ConvertRNGState(TAC.Context context, EngineInternal.RNGState state)
        {
            if (swole.IsNull(state)) return null;

            var ms_obj = RNGStateType().NewObject(context);

            // Set rng state object data
            ms_obj.map[varStr_cachedReference] = new ValRNGState(state);

            return ms_obj;
        }

        public static SwoleScriptType InputManagerType()
        {
            if (_inputManagerType == null)
            {
                _inputManagerType = new SwoleScriptType(type_InputManager, typeof(IInputManager));
                _inputManagerType[var_genericName] = new ValString(type_InputManager);

                var mainInputManager = swole.Engine.InputManager; 
                if (mainInputManager != null)
                {
                    Dictionary<string, AssignmentOverrideDelegate> assignOverrideDelegates = new Dictionary<string, AssignmentOverrideDelegate>();

                    AddFieldsAsLocalIntrinsics(_inputManagerType, mainInputManager.InputFields, ref assignOverrideDelegates);
                    AddPropertiesAsLocalIntrinsics(_inputManagerType, mainInputManager.InputProperties, ref assignOverrideDelegates);
                    AddMethodsAsLocalIntrinsics(_inputManagerType, mainInputManager.InputMethods, ref assignOverrideDelegates); 
                }
            }
            return _inputManagerType;
        }
        static SwoleScriptType _inputManagerType = null;
        public static ValMap ConvertInputManager(TAC.Context context, IInputManager manager)
        {
            if (manager == null) return null;

            var ms_obj = InputManagerType().NewObject(context); 

            ms_obj[var_cachedReference] = new ValInputManager(manager);

            return ms_obj;
        }

        public static SwoleScriptType EventHandlerType()
        {
            if (_eventHandlerType == null)
            {
                _eventHandlerType = new SwoleScriptType(type_EventHandler, typeof(IRuntimeEventHandler));
                _eventHandlerType[var_genericName] = new ValString(type_EventHandler);

                Intrinsic f;
                Dictionary<string, AssignmentOverrideDelegate> assignOverrideDelegates = new Dictionary<string, AssignmentOverrideDelegate>();

                AddLocalIntrinsicToObjectWithAssignOverride(_eventHandlerType, func_PreListenForEvents, func_PreListen, assignOverrideDelegates, DefaultOverrideDelegate);
                AddLocalIntrinsicToObjectWithAssignOverride(_eventHandlerType, func_StopPreListeningForEvents, func_StopPreListening, assignOverrideDelegates, DefaultOverrideDelegate);
                AddLocalIntrinsicToObjectWithAssignOverride(_eventHandlerType, func_ListenForEvents, func_Listen, assignOverrideDelegates, DefaultOverrideDelegate);
                AddLocalIntrinsicToObjectWithAssignOverride(_eventHandlerType, func_StopListeningForEvents, func_StopListening, assignOverrideDelegates, DefaultOverrideDelegate);
            }
            return _eventHandlerType;
        }
        static SwoleScriptType _eventHandlerType = null;
        public static ValMap ConvertEventHandler(TAC.Context context, IRuntimeEventHandler handler)
        {
            if (handler == null) return null;

            var ms_obj = EventHandlerType().NewObject(context);

            ms_obj[var_cachedEventHandler] = new ValEventHandler(handler);

            return ms_obj;
        }

        public static SwoleScriptType EngineObjectType()
        {
            if (_engineObjectType == null)
            {
                _engineObjectType = new SwoleScriptType(type_EngineObject, typeof(EngineInternal.IEngineObject));

                _engineObjectType[var_genericName] = new ValString(type_EngineObject);
                _engineObjectType[var_engineObjectID] = null;

                AddLocalIntrinsicToObject(_engineObjectType, func_DestroyObject);
                AddLocalIntrinsicToObject(_engineObjectType, func_GetGameplayExperience, var_gameplayExperience);
                AddLocalIntrinsicToObject(_engineObjectType, func_GetRootCreationInstance, var_rootCreationInstance);
            }
            return _engineObjectType; 
        }
        static SwoleScriptType _engineObjectType = null;
        public static ValMap ConvertEngineObject(TAC.Context context, EngineInternal.IEngineObject engineObject)
        {
            if (engineObject == null) return null;

            var ms_obj = EngineObjectType().NewObject(context);

            ms_obj[var_genericName] = new ValString(engineObject.name);
            ms_obj[var_engineObjectID] = new ValNumber(engineObject.InstanceID);

            ms_obj.map[varStr_cachedEngineObject] = new ValEngineObject(engineObject);
            bool hasEventHandler = engineObject.HasEventHandler;
            ms_obj[var_hasEventHandler] = ValNumber.Truth(hasEventHandler);
            if (hasEventHandler) ms_obj[var_eventHandler] = ConvertEventHandler(context, engineObject.EventHandler);

            var existingAssignOverride = ms_obj.assignOverride;
            ms_obj.assignOverride = (Value key, Value val) =>
            {

                if (key != null)
                {
                    var keyStr = key.ToString();

                    switch (keyStr)
                    {
                        case var_genericName:
                            return true; // cancel overwrite
                        case var_engineObjectID:
                            return true; // cancel overwrite
                        case var_cachedEngineObject:
                            return true; // cancel overwrite
                        case var_hasEventHandler:
                            return true; // cancel overwrite
                        case var_eventHandler:
                            return true; // cancel overwrite
                    }
                }

                if (existingAssignOverride != null) return existingAssignOverride(key, val);
                return false; // continue as normal
            };

            return ms_obj;
        }

        public static SwoleScriptType GameObjectType()
        {
            if (_gameObjectType == null)
            {
                _gameObjectType = new SwoleScriptType(type_GameObject, typeof(EngineInternal.GameObject), EngineObjectType());

                _gameObjectType[var_genericName] = new ValString(type_GameObject);

                AddLocalTransformIntrinsicsToObject(_gameObjectType); // Makes it so transform operations can be applied directly to the game object instead of having to fetch the transform component first

                Intrinsic f;
                Dictionary<string, AssignmentOverrideDelegate> assignOverrideDelegates = new Dictionary<string, AssignmentOverrideDelegate>();
                  
                AddLocalIntrinsicToObjectWithAssignOverride(_gameObjectType, func_GetComponent, null, assignOverrideDelegates, DefaultOverrideDelegate);
                AddLocalIntrinsicToObjectWithAssignOverride(_gameObjectType, func_AddComponent, null, assignOverrideDelegates, DefaultOverrideDelegate);

                // layer.SetActive(true/false)
                f = CreateNewLocalIntrinsicWithAssignOverride(_gameObjectType, func_SetActive, assignOverrideDelegates, DefaultOverrideDelegate);
                f.AddParam(var_self);
                f.AddParam(var_value);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValEngineObject>(context.self, out var gobj) || gobj.engineObject == null) return Intrinsic.Result.Null;

                    EngineInternal.GameObject go = default;
                    if (gobj.engineObject is EngineInternal.GameObject go_) go = go_;
                    else if (gobj.engineObject is EngineInternal.SwoleGameObject sgo) go = sgo.instance;
                    if (go.IsDestroyed) return Intrinsic.Result.Null; 

                    var active = context.GetLocal(var_value);
                    if (active == null) return Intrinsic.Result.Null;
                    go.SetActive(active.BoolValue());
                    return Intrinsic.Result.Null;
                };

            }
            return _gameObjectType;
        }
        static SwoleScriptType _gameObjectType = null;
        public static ValMap ConvertGameObject(TAC.Context context, EngineInternal.GameObject gameObject, ValMap transform = null)
        {
            var parent_obj = ConvertEngineObject(context, gameObject); // Get engine object data first
            var ms_obj = GameObjectType().NewObject(context);
            CopyParentObjectIntoChildObject(ms_obj, parent_obj);

            // Set game object data
            ms_obj[var_transform] = transform == null ? ms_obj[var_transform] = ConvertTransform(context, gameObject.transform, ms_obj) : transform; 

            var existingAssignOverride = ms_obj.assignOverride;
            ms_obj.assignOverride = (Value key, Value val) =>
            {

                if (swole.IsNull(gameObject)) return false;

                if (existingAssignOverride != null && existingAssignOverride(key, val)) return true;

                if (key != null)
                {
                    string keyStr = key.ToString(); 

                    switch(keyStr)
                    {
                        // ...
                    }

                    // Allow transform operations to be applied directly to the game object
                    if (TransformObjectAssignOverride(key, val, gameObject.transform)) return true;    
                }

                if (parent_obj.assignOverride != null) return parent_obj.assignOverride(key, val);
                return false;
            };

            return ms_obj;
        }

        public static SwoleScriptType ComponentType()
        {
            if (_componentType == null)
            {
                _componentType = new SwoleScriptType(type_Component, typeof(EngineInternal.IComponent), EngineObjectType());
                _componentType[var_genericName] = new ValString(type_Component);
            }
            return _componentType;
        }
        static SwoleScriptType _componentType = null;
        public static ValMap ConvertComponent(TAC.Context context, EngineInternal.IComponent component, ValMap childInstance = null, ValMap gameObject = default)
        {
            if (component == null) return null;

            var parent_obj = ConvertEngineObject(context, component); // Get engine object data first
            var ms_obj = ComponentType().NewObject(context);
            CopyParentObjectIntoChildObject(ms_obj, parent_obj);

            // Set component object data
            ms_obj[var_gameObject] = gameObject == null ? ConvertGameObject(context, component.baseGameObject, (component is EngineInternal.ITransform) ? childInstance : null) : gameObject;

            return ms_obj;
        }

        public static void AddLocalTransformIntrinsicsToObject(ValMap obj)
        {
            AddLocalIntrinsicToObject(obj, func_GetLocalPosition, var_localPosition);
            AddLocalIntrinsicToObject(obj, func_GetLocalRotation, var_localRotation);
            AddLocalIntrinsicToObject(obj, func_GetLocalScale, var_localScale);

            AddLocalIntrinsicToObject(obj, func_GetWorldPosition, var_position);
            AddLocalIntrinsicToObject(obj, func_GetWorldRotation, var_rotation);
            AddLocalIntrinsicToObject(obj, func_GetLossyScale, var_lossyScale); 
        }
        private static bool TransformObjectAssignOverride(Value key, Value val, EngineInternal.ITransform transform)
        {

            if (swole.IsNull(transform)) return false;

            switch (key.ToString())
            {
                case var_localPosition:
                    if (val is ValVector)
                    {
                        ValVector vec = (ValVector)val;
                        transform.localPosition = new EngineInternal.Vector3((float)vec.X, (float)vec.Y, (float)vec.Z);
                    }
                    return true;
                case var_position:
                    if (val is ValVector)
                    {
                        ValVector vec = (ValVector)val;
                        transform.position = new EngineInternal.Vector3((float)vec.X, (float)vec.Y, (float)vec.Z);
                    }
                    return true;

                case var_localRotation:
                    if (val is ValVector) // Quaternions are vectors in disguise
                    {
                        ValVector vec = (ValVector)val;
                        transform.localRotation = new EngineInternal.Quaternion((float)vec.X, (float)vec.Y, (float)vec.Z, (float)vec.W);
                    }
                    return true;
                case var_rotation:
                    if (val is ValVector) // Quaternions are vectors in disguise
                    {
                        ValVector vec = (ValVector)val;
                        transform.rotation = new EngineInternal.Quaternion((float)vec.X, (float)vec.Y, (float)vec.Z, (float)vec.W);
                    }
                    return true;

                case var_localScale:
                    if (val is ValVector)
                    {
                        ValVector vec = (ValVector)val;
                        transform.localScale = new EngineInternal.Vector3((float)vec.X, (float)vec.Y, (float)vec.Z);
                    }
                    return true;
            }

            return false; // don't cancel normal assignment (true will cancel it)
        }

        public static SwoleScriptType TransformType()
        {
            if (_transformType == null)
            {
                _transformType = new SwoleScriptType(type_Transform, typeof(EngineInternal.ITransform), ComponentType());

                _transformType[var_genericName] = new ValString(type_Transform);

                AddLocalTransformIntrinsicsToObject(_transformType);
            }
            return _transformType;
        }
        static SwoleScriptType _transformType = null; 
        public static ValMap ConvertTransform(TAC.Context context, EngineInternal.ITransform transform, ValMap gameObject = null)
        {
            if (transform == null) return null;

            var ms_obj = TransformType().NewObject(context);
            var parent_obj = ConvertComponent(context, transform, ms_obj, gameObject); // Get component object data first
            CopyParentObjectIntoChildObject(ms_obj, parent_obj);

            // Set transform object data

            var existingAssignOverride = ms_obj.assignOverride;
            ms_obj.assignOverride = (Value key, Value val) =>
            {
                if (existingAssignOverride != null && existingAssignOverride(key, val)) return true;
                if (key != null && TransformObjectAssignOverride(key, val, transform)) return true;
                if (parent_obj.assignOverride != null) return parent_obj.assignOverride(key, val);   
                return false;
            };
               
            return ms_obj;
        }

        public static SwoleScriptType CameraType()
        {
            if (_cameraType == null)
            {
                _cameraType = new SwoleScriptType(type_Camera, typeof(EngineInternal.ITransform), ComponentType());
                _cameraType[var_genericName] = new ValString(type_Camera);

                Intrinsic f;
                Dictionary<string, AssignmentOverrideDelegate> assignOverrideDelegates = new Dictionary<string, AssignmentOverrideDelegate>();

                // camera.fieldOfView
                f = CreateNewLocalIntrinsicWithAssignOverride(_cameraType, var_fieldOfView, assignOverrideDelegates, (ValMap instance, Value value) =>
                {
                    if (TryGetCachedEngineReference<EngineInternal.Camera>(instance, out var camera))
                    {
                        if (value != null) camera.fieldOfView = value.FloatValue(); 
                    }
                    return true;
                });
                f.AddParam(var_self);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedEngineReference<EngineInternal.Camera>(context.self, out var camera)) return Intrinsic.Result.Null;
                    return new Intrinsic.Result(new ValNumber(camera.fieldOfView));
                };

                // camera.orthographic
                f = CreateNewLocalIntrinsicWithAssignOverride(_cameraType, var_orthographic, assignOverrideDelegates, (ValMap instance, Value value) =>
                {
                    if (TryGetCachedEngineReference<EngineInternal.Camera>(instance, out var camera))
                    {
                        if (value != null) camera.orthographic = value.BoolValue();
                    }
                    return true;
                });
                f.AddParam(var_self);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedEngineReference<EngineInternal.Camera>(context.self, out var camera)) return Intrinsic.Result.Null;
                    return new Intrinsic.Result(ValNumber.Truth(camera.orthographic));
                };

                // camera.orthographicSize
                f = CreateNewLocalIntrinsicWithAssignOverride(_cameraType, var_orthographicSize, assignOverrideDelegates, (ValMap instance, Value value) =>
                {
                    if (TryGetCachedEngineReference<EngineInternal.Camera>(instance, out var camera))
                    {
                        if (value != null) camera.orthographicSize = value.FloatValue();
                    }
                    return true;
                });
                f.AddParam(var_self);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedEngineReference<EngineInternal.Camera>(context.self, out var camera)) return Intrinsic.Result.Null;
                    return new Intrinsic.Result(new ValNumber(camera.orthographicSize));
                };

                // camera.nearClipPlane
                f = CreateNewLocalIntrinsicWithAssignOverride(_cameraType, var_nearClipPlane, assignOverrideDelegates, (ValMap instance, Value value) =>
                {
                    if (TryGetCachedEngineReference<EngineInternal.Camera>(instance, out var camera))
                    {
                        if (value != null) camera.nearClipPlane = value.FloatValue();
                    }
                    return true;
                });
                f.AddParam(var_self);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedEngineReference<EngineInternal.Camera>(context.self, out var camera)) return Intrinsic.Result.Null;
                    return new Intrinsic.Result(new ValNumber(camera.nearClipPlane));
                };

                // camera.farClipPlane
                f = CreateNewLocalIntrinsicWithAssignOverride(_cameraType, var_farClipPlane, assignOverrideDelegates, (ValMap instance, Value value) =>
                {
                    if (TryGetCachedEngineReference<EngineInternal.Camera>(instance, out var camera))
                    {
                        if (value != null) camera.farClipPlane = value.FloatValue();
                    }
                    return true;
                });
                f.AddParam(var_self);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedEngineReference<EngineInternal.Camera>(context.self, out var camera)) return Intrinsic.Result.Null;
                    return new Intrinsic.Result(new ValNumber(camera.farClipPlane));
                };
                 
                // transform qol
                AddLocalTransformIntrinsicsToObject(_cameraType);
            }
            return _cameraType;
        }
        static SwoleScriptType _cameraType = null;
        public static ValMap ConvertCamera(TAC.Context context, EngineInternal.Camera camera, ValMap gameObject = null)
        {
            var ms_obj = CameraType().NewObject(context);
            var parent_obj = ConvertComponent(context, camera, ms_obj, gameObject); // Get component object data first
            CopyParentObjectIntoChildObject(ms_obj, parent_obj);

            // Set camera object data

            var existingAssignOverride = ms_obj.assignOverride;
            ms_obj.assignOverride = (Value key, Value val) =>
            {
                if (existingAssignOverride != null && existingAssignOverride(key, val)) return true;
                if (key != null && TransformObjectAssignOverride(key, val, camera.baseGameObject.transform)) return true;
                if (parent_obj.assignOverride != null) return parent_obj.assignOverride(key, val);
                return false;
            };

            return ms_obj;
        }

        #region Physics

        public class ValRigidbody : ValEngineObject
        {
            public ValRigidbody(IRigidbody reference) : base(reference) { }
            public IRigidbody Rigidbody => engineObject is IRigidbody inst ? inst : null;
            public override string ToString(TAC.Machine vm) => $"rigidbody[{(swole.Engine.IsNull(engineObject) ? "null" : engineObject.InstanceID)}]";
        }

        private static void CreatePhysicsTypes()
        {
            Intrinsic f;

            f = Intrinsic.Create(type_Rigidbody);
            f.code = (context, partialResult) => {
                var type = RigidbodyType().GetType(context);
                return new Intrinsic.Result(type);
            };

            CreatePhysicsIntrinsics();
        }

        private static void CreatePhysicsIntrinsics()
        {
            /*
            Intrinsic f;

            f = Intrinsic.Create(func_CreateAnimationLayer);
            f.AddParam(var_genericName);
            f.code = (context, partialResult) =>
            {
                return Intrinsic.Result.Null;
            };     
             */
        } 

        public static SwoleScriptType RigidbodyType()
        { 
            if (_rigidbodyType == null)
            {
                var type = typeof(IRigidbody);
                var typeName = new ValString(type_Rigidbody);
                _rigidbodyType = new SwoleScriptType(type_Rigidbody, type, ComponentType());
                _rigidbodyType[var_genericName] = typeName;

                //Intrinsic f;
                Dictionary<string, AssignmentOverrideDelegate> assignOverrideDelegates = new Dictionary<string, AssignmentOverrideDelegate>();
                TransferCompatibleTypeImplementations(_rigidbodyType, type, assignOverrideDelegates); 

            }
            return _rigidbodyType;
        }
        static SwoleScriptType _rigidbodyType = null;
        public static ValMap ConvertRigidbody(TAC.Context context, IRigidbody rigidbody)
        {
            if (rigidbody == null) return null;

            var ms_obj = RigidbodyType().NewObject(context);
            var parent_obj = ConvertComponent(context, rigidbody, ms_obj); // Get component object data first
            CopyParentObjectIntoChildObject(ms_obj, parent_obj);

            // Set rigidbody object data
            ms_obj.map[varStr_cachedReference] = new ValRigidbody(rigidbody); 

            return ms_obj;
        }

        #endregion

        #endregion

        #region Swole Types

        public static SwoleScriptType SwoleGameObjectType()
        {
            if (_swoleGameObjectType == null)
            {
                _swoleGameObjectType = new SwoleScriptType(type_SwoleGameObject, typeof(EngineInternal.SwoleGameObject), GameObjectType());

                _swoleGameObjectType[var_genericName] = new ValString(type_SwoleGameObject);
                _swoleGameObjectType[var_swoleID] = new ValNumber(-1);

            }
            return _swoleGameObjectType;
        }
        static SwoleScriptType _swoleGameObjectType = null;
        public static ValMap ConvertSwoleGameObject(TAC.Context context, EngineInternal.SwoleGameObject swoleGameObject)
        {
            var parent_obj = ConvertGameObject(context, swoleGameObject.instance); // Get game object data first
            var ms_obj = SwoleGameObjectType().NewObject(context);
            CopyParentObjectIntoChildObject(ms_obj, parent_obj);

            // Set swole object data
            ms_obj[var_swoleID] = new ValNumber(swoleGameObject.id);

            return ms_obj;
        }
         
        public static SwoleScriptType TileInstanceType()
        {
            if (_tileInstanceType == null)
            {
                var type = typeof(ITileInstance);
                var typeName = new ValString(type_TileInstance);
                _tileInstanceType = new SwoleScriptType(type_TileInstance, typeof(ITileInstance), EngineObjectType());
                _tileInstanceType[var_genericName] = typeName;
                 
                //Intrinsic f;
                Dictionary<string, AssignmentOverrideDelegate> assignOverrideDelegates = new Dictionary<string, AssignmentOverrideDelegate>();
                TransferCompatibleTypeImplementations(_tileInstanceType, type, assignOverrideDelegates);

            }
            return _tileInstanceType;
        }
        static SwoleScriptType _tileInstanceType = null;  
        public static ValMap ConvertTileInstance(TAC.Context context, ITileInstance tile)
        {
            if (tile == null) return null; 

            var parent_obj = ConvertTransform(context, tile); // Get transform object data first
            var ms_obj = TileInstanceType().NewObject(context);
            CopyParentObjectIntoChildObject(ms_obj, parent_obj);
            
            // Set tile instance object data
            // ...

            return ms_obj;
        }

        public static SwoleScriptType CreationInstanceType()
        {
            if (_creationInstanceType == null)
            {
                _creationInstanceType = new SwoleScriptType(type_CreationInstance, typeof(ICreationInstance), EngineObjectType());

                _creationInstanceType[var_genericName] = new ValString(type_CreationInstance);

                AddLocalIntrinsicToObject(_creationInstanceType, func_FindSwoleGameObjectInCreation, func_FindById);   
            }
            return _creationInstanceType;
        }
        static SwoleScriptType _creationInstanceType = null;
        public static ValMap ConvertCreationInstance(TAC.Context context, ICreationInstance creation)
        {
            if (creation == null) return null;

            var parent_obj = ConvertComponent(context, creation); // Get component object data first
            var ms_obj = CreationInstanceType().NewObject(context);
            CopyParentObjectIntoChildObject(ms_obj, parent_obj); 

            // Set creation instance object data
            // ...

            return ms_obj;
        }

        public static SwoleScriptType AssetType()
        {
            if (_assetType == null)
            {
                _assetType = new SwoleScriptType(type_Asset, typeof(ISwoleAsset));

                _assetType[var_genericName] = new ValString(type_Asset);
            }
            return _assetType;
        }
        static SwoleScriptType _assetType = null;
        public static ValMap ConvertAsset(TAC.Context context, ISwoleAsset asset)
        {
            if (asset == null) return null;
             
            var ms_obj = AssetType().NewObject(context);

            // Set asset object data
            ms_obj[var_genericName] = new ValString(asset.Name);

            var existingAssignOverride = ms_obj.assignOverride;
            ms_obj.assignOverride = (Value key, Value val) =>
            {

                if (existingAssignOverride != null && existingAssignOverride(key, val)) return true;

                if (key != null)
                {
                    var keyStr = key.ToString();

                    switch(keyStr)
                    {
                        case var_genericName:
                            return true;
                    }
                }

                return false;
            };

            return ms_obj;
        }

        #endregion

        #region Animation Types

        public class ValAnimator : ValEngineObject
        {
            public ValAnimator(IAnimator engineObject) : base(engineObject){}
            public IAnimator Animator => engineObject is IAnimator animator ? animator : null;

            public override string ToString(TAC.Machine vm) => $"animator[{(swole.Engine.IsNull(engineObject) ? "null" : engineObject.InstanceID)}]";
        }
        public class ValAnimationController : ValEngineObject
        {
            public ValAnimationController(IAnimationController engineObject) : base(engineObject){}
            public IAnimationController Controller => engineObject is IAnimationController controller ? controller : null;
            public override string ToString(TAC.Machine vm) => $"animationController[{(swole.Engine.IsNull(engineObject) ? "null" : engineObject.InstanceID)}]";
        }

        public class ValAnimation : ValReference
        {
            public IAnimationAsset reference;
            public override object Reference => reference;

            public ValAnimation(IAnimationAsset reference)
            {
                this.reference = reference;
            }
            public override double Equality(Value rhs)
            {
                if (rhs is ValAnimation obj) return reference == obj.reference ? 1 : 0;
                return 0;
            }
            public override int Hash() => ReferenceEquals(reference, null) ? base.GetHashCode() : reference.GetHashCode();
            public override string ToString(TAC.Machine vm) => $"animation[{(swole.Engine.IsNull(reference) ? "null" : ((reference.PackageInfo.NameIsValid ? ((reference.PackageInfo.VersionIsValid ? reference.PackageInfo.GetIdentityString() : reference.PackageInfo.name) + "/") : string.Empty) + reference.Name))}]";
        }

        public class ValAnimationPlayer : ValReference
        {
            public IAnimationPlayer reference;
            public override object Reference => reference;

            public ValAnimationPlayer(IAnimationPlayer reference)
            {
                this.reference = reference;
            }
            public override double Equality(Value rhs)
            {
                if (rhs is ValAnimationPlayer obj) return reference == obj.reference ? 1 : 0;
                return 0;
            }
            public override int Hash() => ReferenceEquals(reference, null) ? base.GetHashCode() : reference.GetHashCode();
            public override string ToString(TAC.Machine vm) => $"animation_player[{Hash()}]";
        }

        public class ValAnimationLayer : ValReference
        {
            public IAnimationLayer reference;
            public override object Reference => reference;

            public ValAnimationLayer(IAnimationLayer reference)
            {
                this.reference = reference;
            }
            public override double Equality(Value rhs)
            {
                if (rhs is ValAnimationPlayer obj) return reference == obj.reference ? 1 : 0;
                return 0;
            }
            public override int Hash() => ReferenceEquals(reference, null) ? base.GetHashCode() : reference.GetHashCode();
            public override string ToString(TAC.Machine vm) => $"animation_layer[{Hash()}]";
        }

        public class ValAnimationStateMachine : ValReference
        {
            public IAnimationStateMachine reference;
            public override object Reference => reference;

            public ValAnimationStateMachine(IAnimationStateMachine reference)
            {
                this.reference = reference;
            }
            public override double Equality(Value rhs)
            {
                if (rhs is ValAnimationStateMachine obj) return reference == obj.reference ? 1 : 0;
                return 0;
            }
            public override int Hash() => ReferenceEquals(reference, null) ? base.GetHashCode() : reference.GetHashCode();
            public override string ToString(TAC.Machine vm) => $"animation_state_machine[{Hash()}]";
        }

        public class ValAnimationMotionController : ValReference
        {    
            public IAnimationMotionController reference;
            public override object Reference => reference;

            public ValAnimationMotionController(IAnimationMotionController reference)
            {
                this.reference = reference;
            }
            public override double Equality(Value rhs)
            {
                if (rhs is ValAnimationMotionController obj) return reference == obj.reference ? 1 : 0;
                return 0;
            }
            public override int Hash() => ReferenceEquals(reference, null) ? base.GetHashCode() : reference.GetHashCode();
            public override string ToString(TAC.Machine vm) => $"animation_motion_controller[{Hash()}]";
        }
        public class ValAnimationReference : ValAnimationMotionController
        {
            public ValAnimationReference(IAnimationReference reference) : base(reference)
            {
            }
            public IAnimationReference AnimationReference => reference is IAnimationReference ar ? ar : null;
            public override string ToString(TAC.Machine vm) => $"animation_reference[{Hash()}]";
        }

        public class ValMotionControllerIdentifier : Value
        {
            public ValString controllerType;
            public ValNumber index;

            public ValMotionControllerIdentifier(MotionControllerType type, int index)
            {
                controllerType = new ValString(type.ToString());
                this.index = new ValNumber(index);
            }
            public ValMotionControllerIdentifier(MotionControllerIdentifier id) : this(id.type, id.index) { }

            public MotionControllerType Type
            {
                get
                {
                    if (Enum.TryParse<MotionControllerType>(controllerType.ToString(), out var t)) return t;
                    return MotionControllerType.AnimationReference;
                }
            }
            public int Index => index.IntValue();
            public MotionControllerIdentifier Identifier => new MotionControllerIdentifier() { index = Index, type = Type };

            public override double Equality(Value rhs)
            {
                if (rhs is ValMotionControllerIdentifier obj) return (controllerType.ToString() == obj.controllerType.ToString() && index.IntValue() == obj.index.IntValue()) ? 1 : 0;
                return 0;
            }
            public override int Hash() => base.GetHashCode();
            public override string ToString(TAC.Machine vm) => $"motion_controller_identifier[{controllerType.ToString()}, {index.IntValue()}]";
        }

        #region Curves

        public class ValCurve : ValReference
        {
            public ICurve curveObject;
            public override object Reference => curveObject;

            public ValCurve(ICurve curveObject)
            {
                this.curveObject = curveObject;
            }
            public override double Equality(Value rhs)
            {
                if (rhs is ValCurve obj) return ReferenceEquals(curveObject, obj.curveObject) ? 1 : 0;

                return 0;
            }
            public override int Hash() => ReferenceEquals(curveObject, null) ? base.GetHashCode() : curveObject.GetHashCode();
            public override string ToString(TAC.Machine vm) => $"curve[{(swole.Engine.IsNull(curveObject) ? "null" : Hash().ToString())}]";
        }
        /*
        public class ValBezierCurve : ValCurve
        {
            public ValBezierCurve(IBezierCurve curveObject) : base(curveObject)
            {
            } 
            public override double Equality(Value rhs)
            {
                if (rhs is ValCurve obj) return ReferenceEquals(curveObject, obj.curveObject) ? 1 : 0;

                return 0;
            }
            public override int Hash() => ReferenceEquals(curveObject, null) ? base.GetHashCode() : curveObject.GetHashCode();
            public override string ToString(TAC.Machine vm) => $"curve[{(swole.Engine.IsNull(curveObject) ? "null" : Hash().ToString())}]";
        }
        */
        #endregion

        private static void CreateAnimationTypes()
        {
            Intrinsic f;

            f = Intrinsic.Create(type_Animator);
            f.code = (context, partialResult) => {
                var type = AnimatorType().GetType(context);
                return new Intrinsic.Result(type);
            };
            f = Intrinsic.Create(type_Animation);
            f.code = (context, partialResult) => {
                var type = AnimationType().GetType(context);
                return new Intrinsic.Result(type);
            };
            f = Intrinsic.Create(type_AnimationPlayer);
            f.code = (context, partialResult) => {
                var type = AnimationPlayerType().GetType(context);
                return new Intrinsic.Result(type);
            };
            f = Intrinsic.Create(type_AnimationController);
            f.code = (context, partialResult) => {
                var type = AnimationControllerType().GetType(context);
                return new Intrinsic.Result(type);
            };
            f = Intrinsic.Create(type_AnimationLayer);
            f.code = (context, partialResult) => {
                var type = AnimationLayerType().GetType(context);
                return new Intrinsic.Result(type);
            };
            f = Intrinsic.Create(type_AnimationStateMachine);
            f.code = (context, partialResult) => {
                var type = AnimationStateMachineType().GetType(context);
                return new Intrinsic.Result(type);
            };
            f = Intrinsic.Create(type_AnimationMotionController);
            f.code = (context, partialResult) => {
                var type = AnimationMotionControllerType().GetType(context);
                return new Intrinsic.Result(type);
            };

            #region Motion Controllers

            f = Intrinsic.Create(type_AnimationReference);
            f.code = (context, partialResult) => {
                var type = AnimationReferenceType().GetType(context);
                return new Intrinsic.Result(type);
            };

            #endregion

            #region Curves

            /*
            f = Intrinsic.Create(type_Curve);
            f.code = (context, partialResult) => {
                var type = CurveType().GetType(context);
                return new Intrinsic.Result(type);
            };
            
            f = Intrinsic.Create(type_BezierCurve);
            f.code = (context, partialResult) => {
                var type = BezierCurveType().GetType(context);
                return new Intrinsic.Result(type);
            };
            */

            #endregion

            CreateAnimationIntrinsics();
        }

        private static void CreateAnimationIntrinsics()
        {
            Intrinsic f;

            // CreateAnimationLayer
            f = Intrinsic.Create(func_CreateAnimationLayer);
            f.AddParam(var_genericName);
            f.code = (context, partialResult) =>
            {
                var name = context.GetLocalString(var_genericName);

                var inst = swole.Engine.CreateNewAnimationLayer(name);
                return new Intrinsic.Result(ConvertAnimationLayer(context, inst));
            };

            // CreateStateMachine
            f = Intrinsic.Create(func_CreateStateMachine);
            f.AddParam(var_genericName);
            f.AddParam(var_motionControllerIndex);
            f.AddParam(var_transitions);
            f.code = (context, partialResult) =>
            {
                var name = context.GetLocalString(var_genericName);
                var index = context.GetLocal(var_motionControllerIndex);
                var transitions = context.GetLocal(var_transitions); // TODO: Support animation transitions

                var inst = swole.Engine.CreateNewStateMachine(name, index == null ? -1 : index.IntValue(), null);
                return new Intrinsic.Result(ConvertAnimationStateMachine(context, inst));
            };

            // CreateAnimationReference
            // Create an animation reference motion controller from an animation asset
            f = Intrinsic.Create(func_CreateAnimationReference);
            f.AddParam(var_genericName);
            f.AddParam(var_assetPath);
            f.AddParam(var_loopMode, 1);
            f.code = (context, partialResult) =>
            {
                var name = context.GetLocalString(var_genericName);
                var asset = context.GetLocal(var_assetPath);
                var loopModeVal = context.GetLocal(var_loopMode);

                AnimationLoopMode loopMode = ConvertToLoopMode(loopModeVal);

                IAnimationAsset animAsset = null;
                if (TryGetCachedReference<ValAnimation>(asset, out var _temp))
                {
                    animAsset = _temp.reference;
                } 
                else if (asset != null)
                {
                    IRuntimeHost host = null;
                    if (context.interpreter.hostData is IRuntimeHost) host = (IRuntimeHost)context.interpreter.hostData;

                    animAsset = FindAsset<IAnimationAsset>(asset.ToString(), host, false);
                }

                if (animAsset == null) throw new RuntimeException("invalid or null animation asset");

                var inst = swole.Engine.CreateNewAnimationReference(name, animAsset, loopMode);
                return new Intrinsic.Result(ConvertAnimationReference(context, inst));
            };
            
        }

        /*
        public EngineInternal.ITransform GetBone(int index)
         */
        public static SwoleScriptType AnimatorType()
        {
            if (_animatorType == null)
            {
                _animatorType = new SwoleScriptType(type_Animator, typeof(IAnimator), ComponentType());
                _animatorType[var_genericName] = new ValString(type_Animator);

                Intrinsic f;
                Dictionary<string, AssignmentOverrideDelegate> assignOverrideDelegates = new Dictionary<string, AssignmentOverrideDelegate>();

                // animator.ApplyController(controller)
                f = CreateNewLocalIntrinsicWithAssignOverride(_animatorType, nameof(IAnimator.ApplyController), assignOverrideDelegates, DefaultOverrideDelegate);
                f.AddParam(var_self);
                f.AddParam(var_controller);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAnimator>(context.self, out var animator) || animator.Animator == null) return Intrinsic.Result.Null;
                    var msController = context.GetLocal(var_controller);
                    if (msController == null) return Intrinsic.Result.Null;
                    
                    if (TryGetCachedReference<ValAnimationController>(msController, out var vac))
                    {
                        animator.Animator.ApplyController(vac.Controller);
                    }

                    return Intrinsic.Result.Null;
                };

                // animator.FindLayer(layerName, prefix)
                f = CreateNewLocalIntrinsicWithAssignOverride(_animatorType, func_FindLayer, assignOverrideDelegates, DefaultOverrideDelegate);
                f.AddParam(var_self);
                f.AddParam(var_layerName);
                f.AddParam(var_prefix, ValString.empty);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAnimator>(context.self, out var animator) || animator.Animator == null) return Intrinsic.Result.Null;
                    var layerName = context.GetLocal(var_layerName);
                    if (layerName == null) return Intrinsic.Result.Null;
                    string layerNameStr = layerName.ToString();
                    var prefix = context.GetLocal(var_prefix);
                    if (prefix != null)
                    {
                        if (prefix is ValString prefixVal)
                        {
                            layerNameStr = prefixVal.ToString() + layerNameStr; 
                        } 
                        else if (TryGetCachedReference<ValAnimationController>(prefix, out var vac) && vac.Controller != null)
                        {
                            layerNameStr = vac.Controller.Prefix + layerNameStr;
                        }
                    }
                    var layer = animator.Animator.FindLayer(layerNameStr);
                    if (layer != null)
                    {
                        return new Intrinsic.Result(ConvertAnimationLayer(context, layer));
                    }
                    return Intrinsic.Result.Null;
                };

                // animator.GetBoneIndex(controller)
                f = CreateNewLocalIntrinsicWithAssignOverride(_animatorType, nameof(IAnimator.GetBoneIndex), assignOverrideDelegates, DefaultOverrideDelegate);
                f.AddParam(var_self);
                f.AddParam(var_bone);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAnimator>(context.self, out var animator) || animator.Animator == null) return Intrinsic.Result.Null;
                    var bone = context.GetLocal(var_bone);
                    if (bone == null) return new Intrinsic.Result(new ValNumber(-1));

                    return new Intrinsic.Result(new ValNumber(animator.Animator.GetBoneIndex(bone.ToString())));
                };
                
                // animator.boneCount
                f = CreateNewLocalIntrinsicWithAssignOverride(_animatorType, var_boneCount, assignOverrideDelegates, DefaultOverrideDelegate);
                f.AddParam(var_self);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAnimator>(context.self, out var animator) || animator.Animator == null) return Intrinsic.Result.Null;
                    return new Intrinsic.Result(new ValNumber(animator.Animator.BoneCount));
                };

                // animator.GetBone(index)
                f = CreateNewLocalIntrinsicWithAssignOverride(_animatorType, nameof(IAnimator.GetBone), assignOverrideDelegates, DefaultOverrideDelegate);
                f.AddParam(var_self);
                f.AddParam(var_index);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAnimator>(context.self, out var animator) || animator.Animator == null) return Intrinsic.Result.Null;
                    var bone = context.GetLocal(var_index);
                    if (bone == null) return Intrinsic.Result.Null;

                    return new Intrinsic.Result(ConvertTransform(context, animator.Animator.GetBone(bone.IntValue())));
                };

            }
            return _animatorType;
        }
        static SwoleScriptType _animatorType = null;
        public static ValMap ConvertAnimator(TAC.Context context, IAnimator animator)
        {
            if (animator == null) return null;

            var ms_obj = AnimatorType().NewObject(context);
            var parent_obj = ConvertComponent(context, animator); // Get component object data first
            CopyParentObjectIntoChildObject(ms_obj, parent_obj);

            // Set animator object data
            ms_obj.map[varStr_cachedReference] = new ValAnimator(animator);

            return ms_obj;
        }

        public static SwoleScriptType AnimationType()
        {
            if (_animationType == null)
            {
                _animationType = new SwoleScriptType(type_Animation, typeof(IAnimationAsset), AssetType());
                _animationType[var_genericName] = new ValString(type_Animation);
            }
            return _animationType;
        }
        static SwoleScriptType _animationType = null;
        public static ValMap ConvertAnimation(TAC.Context context, IAnimationAsset asset)
        {
            if (asset == null) return null;

            var ms_obj = AnimatorType().NewObject(context);
            var parent_obj = ConvertAsset(context, asset); // Get asset object data first
            CopyParentObjectIntoChildObject(ms_obj, parent_obj);

            // Set animation object data
            ms_obj.map[varStr_cachedReference] = new ValAnimation(asset);

            return ms_obj;
        }

        public static SwoleScriptType AnimationPlayerType()
        {
            if (_animationPlayerType == null)
            {
                _animationPlayerType = new SwoleScriptType(type_AnimationPlayer, typeof(IAnimationPlayer), EventHandlerType());
                _animationPlayerType[var_genericName] = new ValString(type_AnimationPlayer);

                var typeName = new ValString(type_AnimationPlayer);

                Intrinsic f;
                Dictionary<string, AssignmentOverrideDelegate> assignOverrideDelegates = new Dictionary<string, AssignmentOverrideDelegate>();

                // animPlayer.index
                f = CreateNewLocalIntrinsicWithAssignOverride(_animationPlayerType, var_index, assignOverrideDelegates, DefaultOverrideDelegate);
                f.AddParam(var_self);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAnimationPlayer>(context.self, out var player) || player.reference == null) return Intrinsic.Result.Null;
                    return new Intrinsic.Result(new ValNumber(player.reference.Index));
                };

                // animPlayer.animation
                f = CreateNewLocalIntrinsicWithAssignOverride(_animationPlayerType, var_animation, assignOverrideDelegates, DefaultOverrideDelegate);
                f.AddParam(var_self);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAnimationPlayer>(context.self, out var player) || player.reference == null) return Intrinsic.Result.Null;
                    return new Intrinsic.Result(ConvertAnimation(context, player.reference.Animation));
                };

                // animPlayer.lengthInSeconds
                f = CreateNewLocalIntrinsicWithAssignOverride(_animationPlayerType, var_lengthInSeconds, assignOverrideDelegates, DefaultOverrideDelegate);
                f.AddParam(var_self);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAnimationPlayer>(context.self, out var player) || player.reference == null) return Intrinsic.Result.Null;
                    return new Intrinsic.Result(new ValNumber(player.reference.LengthInSeconds));
                };

                // animPlayer.loopMode
                f = CreateNewLocalIntrinsicWithAssignOverride(_animationPlayerType, var_loopMode, assignOverrideDelegates, (ValMap instance, Value value) =>
                {
                    if (!TryGetCachedReference<ValAnimationPlayer>(instance, out var player) || player.reference == null) return true;
                    player.reference.LoopMode = ConvertToLoopMode(value);
                    return true;
                });
                f.AddParam(var_self);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAnimationPlayer>(context.self, out var player) || player.reference == null) return Intrinsic.Result.Null;
                    return new Intrinsic.Result(new ValString(player.reference.LoopMode.ToString()));
                };

                // animPlayer.time
                f = CreateNewLocalIntrinsicWithAssignOverride(_animationPlayerType, var_time, assignOverrideDelegates, (ValMap instance, Value value) =>
                {
                    if (!TryGetCachedReference<ValAnimationPlayer>(instance, out var player) || player.reference == null) return true;
                    player.reference.Time = value == null ? 0 : value.FloatValue();
                    return true;
                });
                f.AddParam(var_self);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAnimationPlayer>(context.self, out var player) || player.reference == null) return Intrinsic.Result.Null; 
                    return new Intrinsic.Result(new ValNumber(player.reference.Time));
                };

                // animPlayer.speed
                f = CreateNewLocalIntrinsicWithAssignOverride(_animationPlayerType, var_speed, assignOverrideDelegates, (ValMap instance, Value value) =>
                {
                    if (!TryGetCachedReference<ValAnimationPlayer>(instance, out var player) || player.reference == null) return true;
                    player.reference.Speed = value == null ? 0 : value.FloatValue();
                    return true;
                });
                f.AddParam(var_self);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAnimationPlayer>(context.self, out var player) || player.reference == null) return Intrinsic.Result.Null;
                    return new Intrinsic.Result(new ValNumber(player.reference.Speed));
                };

                // animPlayer.internalSpeed
                f = CreateNewLocalIntrinsicWithAssignOverride(_animationPlayerType, var_internalSpeed, assignOverrideDelegates, DefaultOverrideDelegate);
                f.AddParam(var_self);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAnimationPlayer>(context.self, out var player) || player.reference == null) return Intrinsic.Result.Null;
                    return new Intrinsic.Result(new ValNumber(player.reference.InternalSpeed));
                };

                // animPlayer.mix
                f = CreateNewLocalIntrinsicWithAssignOverride(_animationPlayerType, var_mix, assignOverrideDelegates, (ValMap instance, Value value) =>
                {
                    if (!TryGetCachedReference<ValAnimationPlayer>(instance, out var player) || player.reference == null) return true; 
                    player.reference.Mix = value == null ? 0 : value.FloatValue();
                    return true;
                });
                f.AddParam(var_self);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAnimationPlayer>(context.self, out var player) || player.reference == null) return Intrinsic.Result.Null;
                    return new Intrinsic.Result(new ValNumber(player.reference.Mix));
                };

                // animPlayer.pause
                f = CreateNewLocalIntrinsicWithAssignOverride(_animationPlayerType, var_pause, assignOverrideDelegates, (ValMap instance, Value value) =>
                {
                    if (!TryGetCachedReference<ValAnimationPlayer>(instance, out var player) || player.reference == null) return true;
                    player.reference.Paused = value == null ? false : value.BoolValue();
                    return true;
                });
                f.AddParam(var_self);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAnimationPlayer>(context.self, out var player) || player.reference == null) return Intrinsic.Result.Null;
                    return new Intrinsic.Result(ValNumber.Truth(player.reference.Paused));
                };

            }
            return _animationPlayerType;
        }
        static SwoleScriptType _animationPlayerType = null;
        public static ValMap ConvertAnimationPlayer(TAC.Context context, IAnimationPlayer obj)
        {
            if (obj == null) return null;
            
            var parent_obj = ConvertEventHandler(context, obj); // Get event handler object data first
            var ms_obj = AnimationPlayerType().NewObject(context);
            CopyParentObjectIntoChildObject(ms_obj, parent_obj);

            // Set data
            ms_obj.map[varStr_cachedReference] = new ValAnimationPlayer(obj);

            return ms_obj;
        }

        public static SwoleScriptType AnimationControllerType()
        {
            if (_animationControllerType == null)
            {
                _animationControllerType = new SwoleScriptType(type_AnimationController, typeof(IAnimationController), EngineObjectType());
                var typeName = new ValString(type_AnimationController);

                Intrinsic f;
                Dictionary<string, AssignmentOverrideDelegate> assignOverrideDelegates = new Dictionary<string, AssignmentOverrideDelegate>();

                // controller.name 
                f = CreateNewLocalIntrinsicWithAssignOverride(_animationControllerType, var_genericName, assignOverrideDelegates, (ValMap instance, Value value) =>
                {
                    if (TryGetCachedReference<ValAnimationController>(instance, out var instVal) && instVal.Controller != null) instVal.Controller.Prefix = value == null ? string.Empty : value.ToString();
                    return true;
                });
                f.AddParam(var_self);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAnimationController>(context.self, out var instVal) || instVal.Controller == null) return new Intrinsic.Result(typeName);
                    return new Intrinsic.Result(new ValString(instVal.Controller.Prefix));
                };

                // controller.animationReferences 
                f = CreateNewLocalIntrinsicWithAssignOverride(_animationControllerType, var_animationReferences, assignOverrideDelegates, (ValMap instance, Value value) =>
                {
                    if (TryGetCachedReference<ValAnimationController>(instance, out var instVal) && instVal.Controller != null) 
                    {
                        var controller = instVal.Controller;

                        if (value.TryCastAsMSType<ValList>(out ValList list))
                        {
                            var array = new IAnimationReference[list.values.Count];
                            for (int a = 0; a < array.Length; a++) 
                            {
                                var ms = list.values[a];
                                if (TryGetCachedReference<ValAnimationReference>(ms, out var ar))
                                {
                                    array[a] = ar.AnimationReference;
                                }
                            }
                            controller.AnimationReferences = array;
                        }
                        else throw new RuntimeException($"{var_animationReferences} must be a list containing {type_AnimationReference} objects");

                    }
                    return true;
                });
                f.AddParam(var_self);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAnimationController>(context.self, out var instVal) || instVal.Controller == null) return Intrinsic.Result.Null;
                    var controller = instVal.Controller;
                    var array = controller.AnimationReferences;
                    var list = new ValList();
                    if (array != null)
                    {
                        for (int a = 0; a < array.Length; a++) list.values.Add(ConvertAnimationReference(context, array[a]));
                    }
                    return new Intrinsic.Result(list);
                };

                // controller.layers 
                f = CreateNewLocalIntrinsicWithAssignOverride(_animationControllerType, var_layers, assignOverrideDelegates, (ValMap instance, Value value) =>
                {
                    if (TryGetCachedReference<ValAnimationController>(instance, out var instVal) && instVal.Controller != null)
                    {
                        var controller = instVal.Controller;
                        if (value.TryCastAsMSType<ValList>(out ValList list))
                        {
                            var array = new IAnimationLayer[list.values.Count];
                            for (int a = 0; a < array.Length; a++)
                            {
                                var ms = list.values[a];
                                if (TryGetCachedReference<ValAnimationLayer>(ms, out var ar))
                                {
                                    array[a] = ar.reference;
                                }
                            }
                            controller.Layers = array;
                        }
                        else throw new RuntimeException($"{var_layers} must be a list containing {type_AnimationLayer} objects");

                    }
                    return true;
                });
                f.AddParam(var_self);
                f.code = (context, partialResult) =>
                {
                    if (TryGetCachedReference<ValAnimationController>(context.self, out var instVal) || instVal.Controller == null) return Intrinsic.Result.Null;
                    var controller = instVal.Controller;
                    var array = controller.Layers;
                    var list = new ValList();
                    if (array != null)
                    {
                        for (int a = 0; a < array.Length; a++) list.values.Add(ConvertAnimationLayer(context, array[a])); 
                    }
                    return new Intrinsic.Result(list);
                };

            }

            return _animationControllerType;
        }
        static SwoleScriptType _animationControllerType = null;
        public static ValMap ConvertAnimationController(TAC.Context context, IAnimationController obj)
        {
            if (obj == null) return null;

            var parent_obj = ConvertEngineObject(context, obj); // Get engine object data first
            var ms_obj = AnimationControllerType().NewObject(context);
            CopyParentObjectIntoChildObject(ms_obj, parent_obj);

            // Set data
            ms_obj.map[varStr_cachedReference] = new ValAnimationController(obj);

            return ms_obj;
        }

        public static SwoleScriptType AnimationLayerType()
        {
            if (_animationLayerType == null)
            {
                _animationLayerType = new SwoleScriptType(type_AnimationLayer, typeof(IAnimationLayer));
                var typeName = new ValString(type_AnimationLayer);

                Intrinsic f;
                Dictionary<string, AssignmentOverrideDelegate> assignOverrideDelegates = new Dictionary<string, AssignmentOverrideDelegate>();

                // layer.SetActive(true/false)
                f = CreateNewLocalIntrinsicWithAssignOverride(_animationLayerType, func_SetActive, assignOverrideDelegates, DefaultOverrideDelegate);
                f.AddParam(var_self);
                f.AddParam(var_value);
                f.code = (context, partialResult) => 
                {
                    if (!TryGetCachedReference<ValAnimationLayer>(context.self, out var layer) || layer.reference == null) return Intrinsic.Result.Null;
                    var active = context.GetLocal(var_value);
                    if (active == null) return Intrinsic.Result.Null; 
                    layer.reference.SetActive(active.BoolValue());
                    return Intrinsic.Result.Null;
                };

                // layer.name
                f = CreateNewLocalIntrinsicWithAssignOverride(_animationLayerType, var_genericName, assignOverrideDelegates, (ValMap instance, Value value) =>
                {
                    if (TryGetCachedReference<ValAnimationLayer>(instance, out var layer) && layer.reference != null) layer.reference.Name = value == null ? string.Empty : value.ToString();
                    return true;
                });
                f.AddParam(var_self);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAnimationLayer>(context.self, out var layer) || layer.reference == null) return new Intrinsic.Result(typeName);
                    return new Intrinsic.Result(new ValString(layer.reference.Name));
                };

                // layer.blendParameterIndex
                f = CreateNewLocalIntrinsicWithAssignOverride(_animationLayerType, var_blendParameterIndex, assignOverrideDelegates, (ValMap instance, Value value) =>
                {
                    if (TryGetCachedReference<ValAnimationLayer>(instance, out var layer) && layer.reference != null) layer.reference.BlendParameterIndex = value == null ? -1 : value.IntValue();
                    return true;
                });
                f.AddParam(var_self);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAnimationLayer>(context.self, out var layer) || layer.reference == null) return Intrinsic.Result.Null;
                    return new Intrinsic.Result(new ValNumber(layer.reference.BlendParameterIndex));  
                };

                // layer.entryStateIndex
                f = CreateNewLocalIntrinsicWithAssignOverride(_animationLayerType, var_entryStateIndex, assignOverrideDelegates, (ValMap instance, Value value) =>
                {
                    if (TryGetCachedReference<ValAnimationLayer>(instance, out var layer) && layer.reference != null) layer.reference.EntryStateIndex = value == null ? -1 : value.IntValue();
                    return true;
                });
                f.AddParam(var_self);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAnimationLayer>(context.self, out var layer) || layer.reference == null) return Intrinsic.Result.Null;
                    return new Intrinsic.Result(new ValNumber(layer.reference.EntryStateIndex));
                };

                // layer.mix
                f = CreateNewLocalIntrinsicWithAssignOverride(_animationLayerType, var_mix, assignOverrideDelegates, (ValMap instance, Value value) =>
                {
                    if (TryGetCachedReference<ValAnimationLayer>(instance, out var layer) && layer.reference != null) layer.reference.Mix = value == null ? -1 : value.FloatValue();
                    return true;
                });
                f.AddParam(var_self);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAnimationLayer>(context.self, out var layer) || layer.reference == null) return Intrinsic.Result.Null;
                    return new Intrinsic.Result(new ValNumber(layer.reference.Mix));
                };

                // layer.isAdditive
                f = CreateNewLocalIntrinsicWithAssignOverride(_animationLayerType, var_isAdditive, assignOverrideDelegates, (ValMap instance, Value value) =>
                {
                    if (TryGetCachedReference<ValAnimationLayer>(instance, out var layer) && layer.reference != null) layer.reference.IsAdditive = value == null ? false : value.BoolValue();
                    return true;
                });
                f.AddParam(var_self);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAnimationLayer>(context.self, out var layer) || layer.reference == null) return Intrinsic.Result.Null;
                    return new Intrinsic.Result(ValNumber.Truth(layer.reference.IsAdditive));
                };

                // layer.motionControllerIdentifiers
                f = CreateNewLocalIntrinsicWithAssignOverride(_animationLayerType, var_motionControllerIdentifiers, assignOverrideDelegates, (ValMap instance, Value value) =>
                {
                    if (TryGetCachedReference<ValAnimationLayer>(instance, out var layer) && layer.reference != null) 
                    { 
                        if (value.TryCastAsMSType<ValList>(out ValList list))
                        {
                            MotionControllerIdentifier[] array = new MotionControllerIdentifier[list.values.Count];
                            for (int a = 0; a < list.values.Count; a++) 
                            {
                                var val = list.values[a];
                                if (val is ValMotionControllerIdentifier id)
                                {
                                    array[a] = id.Identifier;
                                } 
                                else if (val is ValMap map)
                                {
                                    if (TryGetCachedReference<ValMotionControllerIdentifier>(map, out id))
                                    {
                                        array[a] = id.Identifier;
                                        continue;
                                    }

                                    MotionControllerType type = MotionControllerType.AnimationReference;
                                    int index = -1;

                                    if (map.TryGetValue(nameof(MotionControllerIdentifier.type), out Value typeVal) && typeVal != null)
                                    {
                                        if (typeVal is ValNumber valN)
                                        {
                                            type = (MotionControllerType)valN.IntValue(); 
                                        } 
                                        else
                                        {
                                            if (Enum.TryParse<MotionControllerType>(typeVal.ToString(), out var t)) type = t;
                                        }
                                    }
                                    if (map.TryGetValue(nameof(MotionControllerIdentifier.index), out Value indexVal) && indexVal != null)
                                    {
                                        index = indexVal.IntValue();
                                    }

                                    array[a] = new MotionControllerIdentifier() { type = type, index = index };
                                }

                            }
                            layer.reference.MotionControllerIdentifiers = array;
                            return true;
                        }

                        layer.reference.MotionControllerIdentifiers = null;
                    }
                    return true;
                });
                f.AddParam(var_self);
                f.AddParam(var_value);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAnimationLayer>(context.self, out var layer) || layer.reference == null) return Intrinsic.Result.Null;
                    var active = context.GetLocal(var_value);
                    if (active == null) return Intrinsic.Result.Null;

                    var array = layer.reference.MotionControllerIdentifiers;
                    if (array == null) return Intrinsic.Result.Null;
                    var ms = new ValList();
                    for (int a = 0; a < array.Length; a++) ms.values.Add(new ValMotionControllerIdentifier(array[a]));

                    return new Intrinsic.Result(ms);
                };

                // layer.stateMachines
                f = CreateNewLocalIntrinsicWithAssignOverride(_animationLayerType, var_stateMachines, assignOverrideDelegates, (ValMap instance, Value value) =>
                {
                    if (TryGetCachedReference<ValAnimationLayer>(instance, out var layer) && layer.reference != null)
                    {
                        if (value.TryCastAsMSType<ValList>(out ValList list))
                        {
                            IAnimationStateMachine[] array = new IAnimationStateMachine[list.values.Count];
                            for (int a = 0; a < list.values.Count; a++)
                            {
                                var val = list.values[a];

                                if (TryGetCachedReference<ValAnimationStateMachine>(val, out var sm))
                                {
                                    array[a] = sm.reference;
                                }
                            }
                            layer.reference.StateMachines = array;
                            return true;
                        }

                        layer.reference.StateMachines = null;
                    }
                    return true;
                });
                f.AddParam(var_self);
                f.AddParam(var_value);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAnimationLayer>(context.self, out var layer) || layer.reference == null) return Intrinsic.Result.Null;
                    var active = context.GetLocal(var_value);
                    if (active == null) return Intrinsic.Result.Null;

                    var array = layer.reference.StateMachines;
                    if (array == null) return Intrinsic.Result.Null;
                    var ms = new ValList();
                    for (int a = 0; a < array.Length; a++) ms.values.Add(new ValAnimationStateMachine(array[a]));

                    return new Intrinsic.Result(ms);
                };

                #region After Instantiation

                // layer.activeStateIndex
                f = CreateNewLocalIntrinsicWithAssignOverride(_animationLayerType, var_activeStateIndex, assignOverrideDelegates, DefaultOverrideDelegate);
                f.AddParam(var_self);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAnimationLayer>(context.self, out var layer) || layer.reference == null) return Intrinsic.Result.Null;
                    return new Intrinsic.Result(new ValNumber(layer.reference.ActiveStateIndex));
                };

                // layer.activeState
                f = CreateNewLocalIntrinsicWithAssignOverride(_animationLayerType, var_activeState, assignOverrideDelegates, DefaultOverrideDelegate);
                f.AddParam(var_self);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAnimationLayer>(context.self, out var layer) || layer.reference == null) return Intrinsic.Result.Null;
                    return new Intrinsic.Result(ConvertAnimationStateMachine(context, layer.reference.ActiveState));
                };

                // layer.hasActiveState
                f = CreateNewLocalIntrinsicWithAssignOverride(_animationLayerType, var_hasActiveState, assignOverrideDelegates, DefaultOverrideDelegate);
                f.AddParam(var_self);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAnimationLayer>(context.self, out var layer) || layer.reference == null) return Intrinsic.Result.Null;
                    return new Intrinsic.Result(ValNumber.Truth(layer.reference.HasActiveState));
                };
                 
                // layer.GetMotionController
                f = CreateNewLocalIntrinsicWithAssignOverride(_animationLayerType, func_GetMotionController, assignOverrideDelegates, DefaultOverrideDelegate);
                f.AddParam(var_self);
                f.AddParam(var_index);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAnimationLayer>(context.self, out var layer) || layer.reference == null) return Intrinsic.Result.Null;
                    var index = context.GetLocal(var_index);
                    if (index == null) throw new RuntimeException("index must be an integer");

                    var mc = layer.reference.GetMotionController(index.IntValue());
                    if (mc == null) return Intrinsic.Result.Null; 

                    ValMap msObj = null;
                    if (mc is IAnimationReference ar)
                    {
                        msObj = ConvertAnimationReference(context, ar);
                    } 
                    else // TODO: Support other motion controller types directly
                    {
                        msObj = ConvertAnimationMotionController(context, mc);
                    }

                    return new Intrinsic.Result(msObj);
                };
                 
                #endregion

            }
            return _animationLayerType;
        }
        static SwoleScriptType _animationLayerType = null;
        public static ValMap ConvertAnimationLayer(TAC.Context context, IAnimationLayer obj)
        {
            if (obj == null) return null;

            var ms_obj = AnimationLayerType().NewObject(context);

            // Set data
            ms_obj.map[varStr_cachedReference] = new ValAnimationLayer(obj);

            return ms_obj;
        }

        /* TODO

        public Transition[] Transitions
        {
            get => transitions;
            set => transitions = value;
        }

         */
         
        public static SwoleScriptType AnimationStateMachineType()
        {
            if (_animationStateMachineType == null)
            {
                _animationStateMachineType = new SwoleScriptType(type_AnimationStateMachine, typeof(IAnimationStateMachine));
                var typeName = new ValString(type_AnimationStateMachine);

                Intrinsic f;
                Dictionary<string, AssignmentOverrideDelegate> assignOverrideDelegates = new Dictionary<string, AssignmentOverrideDelegate>();

                // machine.name
                f = CreateNewLocalIntrinsicWithAssignOverride(_animationStateMachineType, var_genericName, assignOverrideDelegates, (ValMap instance, Value value) =>
                {
                    if (TryGetCachedReference<ValAnimationStateMachine>(instance, out var stateMachine) && stateMachine.reference != null) stateMachine.reference.Name = value == null ? string.Empty : value.ToString();
                    return true;
                });
                f.AddParam(var_self);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAnimationStateMachine>(context.self, out var layer) || layer.reference == null) return new Intrinsic.Result(typeName);
                    return new Intrinsic.Result(new ValString(layer.reference.Name));
                };

                // machine.index
                f = CreateNewLocalIntrinsicWithAssignOverride(_animationStateMachineType, var_index, assignOverrideDelegates, (ValMap instance, Value value) =>
                {
                    if (TryGetCachedReference<ValAnimationStateMachine>(instance, out var stateMachine) && stateMachine.reference != null) stateMachine.reference.Index = value == null ? -1 : value.IntValue();
                    return true;
                });
                f.AddParam(var_self);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAnimationStateMachine>(context.self, out var layer) || layer.reference == null) return Intrinsic.Result.Null;
                    return new Intrinsic.Result(new ValNumber(layer.reference.Index));
                };

                // machine.layer
                f = CreateNewLocalIntrinsicWithAssignOverride(_animationStateMachineType, var_layer, assignOverrideDelegates, DefaultOverrideDelegate);
                f.AddParam(var_self);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAnimationStateMachine>(context.self, out var stateMachine) || stateMachine.reference == null) return Intrinsic.Result.Null;
                    return new Intrinsic.Result(ConvertAnimationLayer(context, stateMachine.reference.Layer));
                };

                // machine.motionControllerIndex
                f = CreateNewLocalIntrinsicWithAssignOverride(_animationStateMachineType, var_motionControllerIndex, assignOverrideDelegates, (ValMap instance, Value value) =>
                {
                    if (TryGetCachedReference<ValAnimationStateMachine>(instance, out var stateMachine) && stateMachine.reference != null) stateMachine.reference.MotionControllerIndex = value == null ? -1 : value.IntValue();
                    return true;
                });
                f.AddParam(var_self);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAnimationStateMachine>(context.self, out var layer) || layer.reference == null) return Intrinsic.Result.Null;
                    return new Intrinsic.Result(new ValNumber(layer.reference.MotionControllerIndex));
                };

                // machine.isActive
                f = CreateNewLocalIntrinsicWithAssignOverride(_animationStateMachineType, var_isActive, assignOverrideDelegates, DefaultOverrideDelegate);
                f.AddParam(var_self);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAnimationStateMachine>(context.self, out var stateMachine) || stateMachine.reference == null) return Intrinsic.Result.Null;
                    return new Intrinsic.Result(ValNumber.Truth(stateMachine.reference.IsActive()));
                };

                // machine.weight
                f = CreateNewLocalIntrinsicWithAssignOverride(_animationStateMachineType, var_weight, assignOverrideDelegates, (ValMap instance, Value value) =>
                {
                    if (TryGetCachedReference<ValAnimationStateMachine>(instance, out var stateMachine) && stateMachine.reference != null) stateMachine.reference.SetWeight(value == null ? 0 : value.FloatValue());
                    return true;
                });
                f.AddParam(var_self);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAnimationStateMachine>(context.self, out var layer) || layer.reference == null) return Intrinsic.Result.Null;
                    return new Intrinsic.Result(new ValNumber(layer.reference.GetWeight()));
                };

                // machine.time
                f = CreateNewLocalIntrinsicWithAssignOverride(_animationStateMachineType, var_time, assignOverrideDelegates, (ValMap instance, Value value) =>
                {
                    if (TryGetCachedReference<ValAnimationStateMachine>(instance, out var stateMachine) && stateMachine.reference != null) stateMachine.reference.SetTime(value == null ? 0 : value.FloatValue());
                    return true;
                });
                f.AddParam(var_self);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAnimationStateMachine>(context.self, out var stateMachine) || stateMachine.reference == null) return Intrinsic.Result.Null;
                    return new Intrinsic.Result(new ValNumber(stateMachine.reference.GetTime()));
                };

                // machine.normalizedTime
                f = CreateNewLocalIntrinsicWithAssignOverride(_animationStateMachineType, var_normalizedTime, assignOverrideDelegates, (ValMap instance, Value value) =>
                {
                    if (TryGetCachedReference<ValAnimationStateMachine>(instance, out var stateMachine) && stateMachine.reference != null) stateMachine.reference.SetNormalizedTime(value == null ? 0 : value.FloatValue());
                    return true;
                });
                f.AddParam(var_self);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAnimationStateMachine>(context.self, out var stateMachine) || stateMachine.reference == null) return Intrinsic.Result.Null;
                    return new Intrinsic.Result(new ValNumber(stateMachine.reference.GetNormalizedTime()));
                };
                
                // machine.estimatedDuration
                f = CreateNewLocalIntrinsicWithAssignOverride(_animationStateMachineType, var_estimatedDuration, assignOverrideDelegates, DefaultOverrideDelegate);
                f.AddParam(var_self);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAnimationStateMachine>(context.self, out var stateMachine) || stateMachine.reference == null) return Intrinsic.Result.Null;
                    return new Intrinsic.Result(new ValNumber(stateMachine.reference.GetEstimatedDuration()));
                };
                
                // machine.RestartAnims
                f = CreateNewLocalIntrinsicWithAssignOverride(_animationStateMachineType, func_RestartAnims, assignOverrideDelegates, DefaultOverrideDelegate);
                f.AddParam(var_self);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAnimationStateMachine>(context.self, out var stateMachine) || stateMachine.reference == null) return Intrinsic.Result.Null;
                    stateMachine.reference.RestartAnims();
                    return Intrinsic.Result.Null;
                };

                // machine.ResyncAnims
                // set all anims to the same playback position
                f = CreateNewLocalIntrinsicWithAssignOverride(_animationStateMachineType, func_ResyncAnims, assignOverrideDelegates, DefaultOverrideDelegate);
                f.AddParam(var_self);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAnimationStateMachine>(context.self, out var stateMachine) || stateMachine.reference == null) return Intrinsic.Result.Null;
                    stateMachine.reference.ResyncAnims();
                    return Intrinsic.Result.Null;
                };

                // machine.ResetTransition
                f = CreateNewLocalIntrinsicWithAssignOverride(_animationStateMachineType, func_ResetTransition, assignOverrideDelegates, DefaultOverrideDelegate);
                f.AddParam(var_self);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAnimationStateMachine>(context.self, out var stateMachine) || stateMachine.reference == null) return Intrinsic.Result.Null;
                    stateMachine.reference.ResetTransition();
                    return Intrinsic.Result.Null;
                };

                // machine.transitionTarget
                f = CreateNewLocalIntrinsicWithAssignOverride(_animationStateMachineType, var_transitionTarget, assignOverrideDelegates, DefaultOverrideDelegate);
                f.AddParam(var_self);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAnimationStateMachine>(context.self, out var layer) || layer.reference == null) return Intrinsic.Result.Null;
                    return new Intrinsic.Result(new ValNumber(layer.reference.TransitionTarget));
                };

                // machine.transitionTime
                f = CreateNewLocalIntrinsicWithAssignOverride(_animationStateMachineType, var_transitionTime, assignOverrideDelegates, DefaultOverrideDelegate);
                f.AddParam(var_self);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAnimationStateMachine>(context.self, out var stateMachine) || stateMachine.reference == null) return Intrinsic.Result.Null;
                    return new Intrinsic.Result(new ValNumber(stateMachine.reference.TransitionTime));
                };

                // machine.transitionTimeLeft
                f = CreateNewLocalIntrinsicWithAssignOverride(_animationStateMachineType, var_transitionTimeLeft, assignOverrideDelegates, DefaultOverrideDelegate);
                f.AddParam(var_self);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAnimationStateMachine>(context.self, out var stateMachine) || stateMachine.reference == null) return Intrinsic.Result.Null;
                    return new Intrinsic.Result(new ValNumber(stateMachine.reference.TransitionTimeLeft));
                };

            }
            return _animationStateMachineType;
        }
        static SwoleScriptType _animationStateMachineType = null;
        public static ValMap ConvertAnimationStateMachine(TAC.Context context, IAnimationStateMachine obj)
        {
            if (obj == null) return null;

            var ms_obj = AnimationStateMachineType().NewObject(context);

            // Set data
            ms_obj.map[varStr_cachedReference] = new ValAnimationStateMachine(obj);

            return ms_obj;
        }

        public static SwoleScriptType AnimationMotionControllerType()
        {
            if (_animationMotionControllerType == null)
            {
                _animationMotionControllerType = new SwoleScriptType(type_AnimationMotionController, typeof(IAnimationMotionController));
                _animationMotionControllerType[var_genericName] = new ValString(type_AnimationMotionController);

                Intrinsic f;
                Dictionary<string, AssignmentOverrideDelegate> assignOverrideDelegates = new Dictionary<string, AssignmentOverrideDelegate>();

                // motionController.HasAnimationPlayer
                f = CreateNewLocalIntrinsicWithAssignOverride(_animationMotionControllerType, func_HasAnimationPlayer, assignOverrideDelegates, DefaultOverrideDelegate);
                f.AddParam(var_self);
                f.AddParam(var_layer);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAnimationMotionController>(context.self, out var controller) || controller.reference == null) return Intrinsic.Result.False;

                    var layerVal = context.GetLocal(var_layer);
                    if (layerVal == null) return Intrinsic.Result.False;
                     
                    IAnimationLayer layer = null;                
                    if (TryGetCachedReference<ValAnimationLayer>(layerVal, out var _temp)) 
                    {
                        layer = _temp.reference;
                    }

                    if (layer == null) return Intrinsic.Result.False;
                    return new Intrinsic.Result(ValNumber.Truth(controller.reference.HasAnimationPlayer(layer))); 
                };
                 
                // motionController.speed
                f = CreateNewLocalIntrinsicWithAssignOverride(_animationMotionControllerType, var_speed, assignOverrideDelegates, (ValMap instance, Value value) =>
                {
                    if (!TryGetCachedReference<ValAnimationMotionController>(instance, out var motionController) || motionController.reference == null) return true;
                    motionController.reference.BaseSpeed = value == null ? 0 : value.FloatValue();
                    return true;
                });
                f.AddParam(var_self);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAnimationMotionController>(context.self, out var motionController) || motionController.reference == null) return Intrinsic.Result.Null;
                    return new Intrinsic.Result(new ValNumber(motionController.reference.BaseSpeed));
                };

                // motionController.weight
                f = CreateNewLocalIntrinsicWithAssignOverride(_animationMotionControllerType, var_weight, assignOverrideDelegates, (ValMap instance, Value value) =>
                {
                    if (TryGetCachedReference<ValAnimationMotionController>(instance, out var stateMachine) && stateMachine.reference != null) stateMachine.reference.SetWeight(value == null ? 0 : value.FloatValue());
                    return true;
                });
                f.AddParam(var_self);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAnimationMotionController>(context.self, out var layer) || layer.reference == null) return Intrinsic.Result.Null;
                    return new Intrinsic.Result(new ValNumber(layer.reference.GetWeight()));
                };

                // motionController.SetTime(layer, time)
                f = CreateNewLocalIntrinsicWithAssignOverride(_animationMotionControllerType, nameof(IAnimationMotionController.SetTime), assignOverrideDelegates, DefaultOverrideDelegate);
                f.AddParam(var_self);
                f.AddParam(var_layer);
                f.AddParam(var_time);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAnimationMotionController>(context.self, out var motionController) || motionController.reference == null) return Intrinsic.Result.Null;

                    var layerVal = context.GetLocal(var_layer);
                    if (!TryGetCachedReference<ValAnimationLayer>(layerVal, out var layer)) layer = null;

                    var timeVal = context.GetLocal(var_time);
                    if (timeVal == null) throw new RuntimeException("no time value provided");

                    motionController.reference.SetTime(layer == null ? null : layer.reference, timeVal.FloatValue());

                    return Intrinsic.Result.Null;
                };
                // motionController.GetTime(layer)
                f = CreateNewLocalIntrinsicWithAssignOverride(_animationMotionControllerType, nameof(IAnimationMotionController.GetTime), assignOverrideDelegates, DefaultOverrideDelegate);
                f.AddParam(var_self);
                f.AddParam(var_layer);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAnimationMotionController>(context.self, out var motionController) || motionController.reference == null) return Intrinsic.Result.Null;

                    var layerVal = context.GetLocal(var_layer);
                    if (!TryGetCachedReference<ValAnimationLayer>(layerVal, out var layer)) layer = null;

                    return new Intrinsic.Result(new ValNumber(motionController.reference.GetTime(layer == null ? null : layer.reference)));
                };

                // motionController.SetNormalizedTime(layer, time)
                f = CreateNewLocalIntrinsicWithAssignOverride(_animationMotionControllerType, nameof(IAnimationMotionController.SetNormalizedTime), assignOverrideDelegates, DefaultOverrideDelegate);
                f.AddParam(var_self);
                f.AddParam(var_layer);
                f.AddParam(var_time);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAnimationMotionController>(context.self, out var motionController) || motionController.reference == null) return Intrinsic.Result.Null;

                    var layerVal = context.GetLocal(var_layer);
                    if (!TryGetCachedReference<ValAnimationLayer>(layerVal, out var layer)) layer = null;

                    var timeVal = context.GetLocal(var_time);
                    if (timeVal == null) throw new RuntimeException("no time value provided");

                    motionController.reference.SetNormalizedTime(layer == null ? null : layer.reference, timeVal.FloatValue());

                    return Intrinsic.Result.Null;
                };
                // motionController.GetNormalizedTime(layer, addTime)
                f = CreateNewLocalIntrinsicWithAssignOverride(_animationMotionControllerType, nameof(IAnimationMotionController.GetNormalizedTime), assignOverrideDelegates, DefaultOverrideDelegate);
                f.AddParam(var_self);
                f.AddParam(var_layer);
                f.AddParam(var_addTime);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAnimationMotionController>(context.self, out var motionController) || motionController.reference == null) return Intrinsic.Result.Null;

                    var layerVal = context.GetLocal(var_layer);
                    if (!TryGetCachedReference<ValAnimationLayer>(layerVal, out var layer)) layer = null;

                    var timeVal = context.GetLocal(var_addTime);

                    return new Intrinsic.Result(new ValNumber(motionController.reference.GetNormalizedTime(layer == null ? null : layer.reference, timeVal == null ? 0 : timeVal.FloatValue())));
                };

                // motionController.GetDuration
                f = CreateNewLocalIntrinsicWithAssignOverride(_animationMotionControllerType, nameof(IAnimationMotionController.GetDuration), assignOverrideDelegates, DefaultOverrideDelegate);
                f.AddParam(var_self);
                f.AddParam(var_layer);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAnimationMotionController>(context.self, out var motionController) || motionController.reference == null) return Intrinsic.Result.Null;

                    var layerVal = context.GetLocal(var_layer);
                    if (!TryGetCachedReference<ValAnimationLayer>(layerVal, out var layer)) layer = null;

                    return new Intrinsic.Result(new ValNumber(motionController.reference.GetDuration(layer == null ? null : layer.reference)));
                };
                // motionController.GetScaledDuration
                f = CreateNewLocalIntrinsicWithAssignOverride(_animationMotionControllerType, nameof(IAnimationMotionController.GetScaledDuration), assignOverrideDelegates, DefaultOverrideDelegate);
                f.AddParam(var_self);
                f.AddParam(var_layer);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAnimationMotionController>(context.self, out var motionController) || motionController.reference == null) return Intrinsic.Result.Null;

                    var layerVal = context.GetLocal(var_layer);
                    if (!TryGetCachedReference<ValAnimationLayer>(layerVal, out var layer)) layer = null;

                    return new Intrinsic.Result(new ValNumber(motionController.reference.GetScaledDuration(layer == null ? null : layer.reference)));
                };

            }
            return _animationMotionControllerType;
        }
        static SwoleScriptType _animationMotionControllerType = null;
        public static ValMap ConvertAnimationMotionController(TAC.Context context, IAnimationMotionController obj)
        {
            if (obj == null) return null;

            var ms_obj = AnimationMotionControllerType().NewObject(context);

            // Set data
            ms_obj.map[varStr_cachedReference] = new ValAnimationMotionController(obj);

            return ms_obj;
        }

        public static SwoleScriptType AnimationReferenceType()
        {
            if (_animationReferenceType == null)
            {
                _animationReferenceType = new SwoleScriptType(type_AnimationReference, typeof(IAnimationReference), AnimationMotionControllerType());
                _animationReferenceType[var_genericName] = new ValString(type_AnimationReference);

                Intrinsic f;
                Dictionary<string, AssignmentOverrideDelegate> assignOverrideDelegates = new Dictionary<string, AssignmentOverrideDelegate>();  

                // animationReference.animationPlayer
                f = CreateNewLocalIntrinsicWithAssignOverride(_animationReferenceType, var_animationPlayer, assignOverrideDelegates, DefaultOverrideDelegate);
                f.AddParam(var_self);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAnimationReference>(context.self, out var controller) || controller.AnimationReference == null) return Intrinsic.Result.Null;
                    return new Intrinsic.Result(ConvertAnimationPlayer(context, controller.AnimationReference.AnimationPlayer));
                };

            }
            return _animationReferenceType; 
        }
        static SwoleScriptType _animationReferenceType = null;
        public static ValMap ConvertAnimationReference(TAC.Context context, IAnimationReference obj)
        {
            if (obj == null) return null; 

            var parent_obj = ConvertAnimationMotionController(context, obj); // Get motion controller object data first
            var ms_obj = AnimationReferenceType().NewObject(context);
            CopyParentObjectIntoChildObject(ms_obj, parent_obj);

            // Set data
            ms_obj.map[varStr_cachedReference] = new ValAnimationReference(obj);

            return ms_obj;
        }

        #region Curves

        public static SwoleScriptType CurveType()
        {
            if (_curveType == null)
            {
                var type = typeof(ICurve);
                _curveType = new SwoleScriptType(type_Curve, type); 
                var typeName = new ValString(type_Curve);

                //Intrinsic f;
                Dictionary<string, AssignmentOverrideDelegate> assignOverrideDelegates = new Dictionary<string, AssignmentOverrideDelegate>();

                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

                AddFieldsAsLocalIntrinsics(_curveType, fields, ref assignOverrideDelegates);
                AddPropertiesAsLocalIntrinsics(_curveType, properties, ref assignOverrideDelegates);
                AddMethodsAsLocalIntrinsics(_curveType, methods, ref assignOverrideDelegates);

            }

            return _curveType;
        }
        static SwoleScriptType _curveType = null;
        public static ValMap ConvertCurve(TAC.Context context, ICurve obj)
        {
            if (obj == null) return null;

            var ms_obj = CurveType().NewObject(context);

            // Set data
            ms_obj.map[varStr_cachedReference] = new ValCurve(obj); 

            return ms_obj;
        }

        /*
        public static SwoleScriptType BezierCurveType()
        {
            if (_bezierCurveType == null)
            {
                var type = typeof(IBezierCurve);
                _bezierCurveType = new SwoleScriptType(type_BezierCurve, type);
                var typeName = new ValString(type_BezierCurve);

                Intrinsic f;
                Dictionary<string, AssignmentOverrideDelegate> assignOverrideDelegates = new Dictionary<string, AssignmentOverrideDelegate>();

                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

                AddFieldsAsLocalIntrinsics(_bezierCurveType, fields, ref assignOverrideDelegates);
                AddPropertiesAsLocalIntrinsics(_bezierCurveType, properties, ref assignOverrideDelegates);
                AddMethodsAsLocalIntrinsics(_bezierCurveType, methods, ref assignOverrideDelegates);

            }

            return _bezierCurveType;
        }
        static SwoleScriptType _bezierCurveType = null;
        public static ValMap ConvertCurve(TAC.Context context, IBezierCurve obj)
        {
            if (obj == null) return null;

            var parent_obj = ConvertCurve(context, obj);
            var ms_obj = BezierCurveType().NewObject(context);
            CopyParentObjectIntoChildObject(ms_obj, parent_obj);

            // Set data
            ms_obj.map[varStr_cachedReference] = new ValBezierCurve(obj);

            return ms_obj;
        }*/

        #endregion

        #endregion

        #region Audio Types

        public class ValAudioAsset : ValContent
        {
            public IAudioAsset Asset => reference is IAudioAsset asset ? asset : null;
            public ValAudioAsset(IAudioAsset reference) : base(reference){ }
            public override string ToString(TAC.Machine vm) => $"audio_asset[{(swole.Engine.IsNull(Asset) ? "null" : ((Asset.PackageInfo.NameIsValid ? (Asset.PackageInfo.ToString() + "/") : "") + Asset.Name))}]"; 
        }

        public class ValAudioMixerGroup : ValEngineObject
        {
            public IAudioMixerGroup MixerGroup => engineObject is IAudioMixerGroup mixer ? mixer : null;
            public ValAudioMixerGroup(IAudioMixerGroup reference) : base(reference){}
            public override string ToString(TAC.Machine vm) => $"audio_mixer_group[{(swole.Engine.IsNull(MixerGroup) ? "null" : MixerGroup.name)}]";  
        }

        public class ValAudioSource : ValEngineObject
        {
            public ValAudioSource(IAudioSource reference) : base(reference) {}
            public IAudioSource Source => engineObject is IAudioSource source ? source : null;
            public override string ToString(TAC.Machine vm) => $"audio_source[{(swole.Engine.IsNull(engineObject) ? "null" : engineObject.InstanceID)}]";
        }
        public class ValAudibleObject : ValAudioSource
        {
            public ValAudibleObject(IAudibleObject reference) : base(reference) { }
            public IAudibleObject Audible => engineObject is IAudibleObject audible ? audible : null;
            public override string ToString(TAC.Machine vm) => $"audible_object[{(swole.Engine.IsNull(engineObject) ? "null" : engineObject.InstanceID)}]";
        }

        public class ValAudioBundle : ValReference
        {
            public IAudioBundle reference;
            public override object Reference => reference;

            public ValAudioBundle(IAudioBundle reference)
            {
                this.reference = reference;
            }

            public override double Equality(Value rhs)
            {
                if (rhs is ValAudioBundle obj) return obj.reference == reference ? 1 : 0;
                return 0;
            }
            public override int Hash() => reference == null ? base.GetHashCode() : reference.GetHashCode();
            public override string ToString(TAC.Machine vm) => $"audio_bundle[{Hash()}]";
        }

        private static void CreateAudioTypes()
        {
            Intrinsic f;
             
            f = Intrinsic.Create(type_AudioSource);
            f.code = (context, partialResult) => {
                var type = AudioSourceType().GetType(context);
                return new Intrinsic.Result(type);
            };
            f = Intrinsic.Create(type_AudioAsset);
            f.code = (context, partialResult) => {
                var type = AudioAssetType().GetType(context);
                return new Intrinsic.Result(type);
            };
            f = Intrinsic.Create(type_AudioMixerGroup);
            f.code = (context, partialResult) => {
                var type = AudioMixerGroupType().GetType(context);
                return new Intrinsic.Result(type);
            };
            f = Intrinsic.Create(type_AudibleObject);
            f.code = (context, partialResult) => {
                var type = AudibleObjectType().GetType(context);
                return new Intrinsic.Result(type);
            };
            f = Intrinsic.Create(type_AudioBundle);
            f.code = (context, partialResult) => {
                var type = AudioBundleType().GetType(context);
                return new Intrinsic.Result(type);
            };

            CreateAudioIntrinsics();
        }

        private static void CreateAudioIntrinsics()
        {
            /*
            Intrinsic f;

            f = Intrinsic.Create(func_CreateAnimationLayer);
            f.AddParam(var_genericName);
            f.code = (context, partialResult) =>
            {
                return Intrinsic.Result.Null;
            };     
             */
        } 

        public static SwoleScriptType AudioSourceType()
        {
            if (_audioSourceType == null)
            {
                var type = typeof(IAudioSource);
                _audioSourceType = new SwoleScriptType(type_AudioSource, type, ComponentType());
                _audioSourceType[var_genericName] = new ValString(type_AudioSource);

                Intrinsic f;
                Dictionary<string, AssignmentOverrideDelegate> assignOverrideDelegates = new Dictionary<string, AssignmentOverrideDelegate>();

                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

                AddFieldsAsLocalIntrinsics(_audioSourceType, fields, ref assignOverrideDelegates);
                AddPropertiesAsLocalIntrinsics(_audioSourceType, properties, ref assignOverrideDelegates);
                AddMethodsAsLocalIntrinsics(_audioSourceType, methods, ref assignOverrideDelegates);

                // void source.PlayOneShot(asset, volumeScale)
                f = CreateNewLocalIntrinsicWithAssignOverride(_audioSourceType, var_clip, assignOverrideDelegates, DefaultOverrideDelegate);
                f.AddParam(var_self);
                f.AddParam(var_asset);
                f.AddParam(var_volumeScale, 1.0F);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAudioSource>(context.self, out var reference) || reference.Source == null) return Intrinsic.Result.Null;

                    var asset = context.GetLocal(var_asset);
                    var volumeScale = context.GetLocalFloat(var_volumeScale, 1.0F);

                    IAudioAsset audioAsset = null;
                    if (TryGetCachedReference<ValAudioAsset>(asset, out var _temp))
                    {
                        audioAsset = _temp.Asset;
                    }
                    else if (asset != null)
                    {
                        IRuntimeHost host = null;
                        if (context.interpreter.hostData is IRuntimeHost) host = (IRuntimeHost)context.interpreter.hostData;

                        audioAsset = FindAsset<IAudioAsset>(asset.ToString(), host, false); 
                    }

                    if (audioAsset == null) throw new RuntimeException("invalid or null audio asset"); 

                    return Intrinsic.Result.Null;
                };

                // source.clip
                f = CreateNewLocalIntrinsicWithAssignOverride(_audioSourceType, var_clip, assignOverrideDelegates, (ValMap instance, Value value) => 
                {
                    if (TryGetCachedReference<ValAudioSource>(instance, out var reference) && reference.Source != null) 
                    {
                        var source = reference.Source;
                        ValAudioAsset asset = null;
                        if (TryGetCachedReference<ValAudioAsset>(value, out var _temp))
                        {
                            asset = _temp;
                        }

                        if (asset != null) source.clip = asset.Asset; else source.clip = null;
                    }
                    return true;
                });
                f.AddParam(var_self);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAudioSource>(context.self, out var reference) || reference.Source == null) return Intrinsic.Result.Null;
                    return new Intrinsic.Result(ConvertAudioAsset(context, reference.Source.clip));
                };

                // source.mixer
                f = CreateNewLocalIntrinsicWithAssignOverride(_audioSourceType, var_mixer, assignOverrideDelegates, (ValMap instance, Value value) =>
                {
                    if (TryGetCachedReference<ValAudioSource>(instance, out var reference) && reference.Source != null)
                    {
                        var source = reference.Source;
                        ValAudioMixerGroup mixer = null;
                        if (TryGetCachedReference<ValAudioMixerGroup>(value, out var _temp))
                        {
                            mixer = _temp;
                        }

                        if (mixer != null) source.outputAudioMixerGroup = mixer.MixerGroup; else source.outputAudioMixerGroup = null;
                    }
                    return true;
                });
                f.AddParam(var_self);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAudioSource>(context.self, out var reference) || reference.Source == null) return Intrinsic.Result.Null;
                    return new Intrinsic.Result(ConvertAudioMixerGroup(context, reference.Source.outputAudioMixerGroup));
                };

            }
            return _audioSourceType;
        }
        static SwoleScriptType _audioSourceType = null;
        public static ValMap ConvertAudioSource(TAC.Context context, IAudioSource source)
        {
            if (source == null) return null;

            var ms_obj = AudioSourceType().NewObject(context);
            var parent_obj = ConvertComponent(context, source); // Get component object data first
            CopyParentObjectIntoChildObject(ms_obj, parent_obj);
            
            // Set audio source object data
            ms_obj.map[varStr_cachedReference] = new ValAudioSource(source);

            return ms_obj;
        }

        public static SwoleScriptType AudioAssetType()
        {
            if (_audioAssetType == null)
            {
                _audioAssetType = new SwoleScriptType(type_AudioAsset, typeof(IAudioAsset), AssetType());
                _audioAssetType[var_genericName] = new ValString(type_AudioAsset);

            }
            return _audioAssetType;
        }
        static SwoleScriptType _audioAssetType = null;
        public static ValMap ConvertAudioAsset(TAC.Context context, IAudioAsset asset)
        {
            if (asset == null) return null;

            var ms_obj = AudioAssetType().NewObject(context);
            var parent_obj = ConvertAsset(context, asset); // Get asset object data first
            CopyParentObjectIntoChildObject(ms_obj, parent_obj);

            // Set audio asset object data
            ms_obj.map[varStr_cachedReference] = new ValAudioAsset(asset);

            return ms_obj;
        }
         
        public static SwoleScriptType AudioMixerGroupType()
        {
            if (_audioMixerGroupType == null)
            {
                _audioMixerGroupType = new SwoleScriptType(type_AudioMixerGroup, typeof(IAudioMixerGroup));
                _audioMixerGroupType[var_genericName] = new ValString(type_AudioMixerGroup);

            }
            return _audioMixerGroupType;
        }
        static SwoleScriptType _audioMixerGroupType = null;
        public static ValMap ConvertAudioMixerGroup(TAC.Context context, IAudioMixerGroup mixer)
        {
            if (mixer == null) return null;

            var ms_obj = AudioMixerGroupType().NewObject(context);

            // Set audio mixer object data
            ms_obj.map[varStr_cachedReference] = new ValAudioMixerGroup(mixer);

            return ms_obj;
        }

        public static SwoleScriptType AudibleObjectType()
        {
            if (_audibleObjectType == null)
            {
                var type = typeof(IAudibleObject);
                _audibleObjectType = new SwoleScriptType(type_AudibleObject, type, AudibleObjectType());
                _audibleObjectType[var_genericName] = new ValString(type_AudibleObject);

                Intrinsic f;
                Dictionary<string, AssignmentOverrideDelegate> assignOverrideDelegates = new Dictionary<string, AssignmentOverrideDelegate>();
                
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

                AddFieldsAsLocalIntrinsics(_audibleObjectType, fields, ref assignOverrideDelegates);
                AddPropertiesAsLocalIntrinsics(_audibleObjectType, properties, ref assignOverrideDelegates);
                AddMethodsAsLocalIntrinsics(_audibleObjectType, methods, ref assignOverrideDelegates);

                // audible.source
                f = CreateNewLocalIntrinsicWithAssignOverride(_audibleObjectType, var_source, assignOverrideDelegates, (ValMap instance, Value value) =>
                {
                    if (TryGetCachedReference<ValAudibleObject>(instance, out var reference) && reference.Audible != null)
                    {
                        var audible = reference.Audible;
                        ValAudioSource source = null;
                        if (TryGetCachedReference<ValAudioSource>(value, out var _temp))
                        {
                            source = _temp;
                        }

                        if (source != null) audible.source = source.Source; else audible.source = null; 
                    }
                    return true;
                });
                f.AddParam(var_self);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAudibleObject>(context.self, out var reference) || reference.Audible == null) return Intrinsic.Result.Null;
                    return new Intrinsic.Result(ConvertAudioSource(context, reference.Audible.source));
                };
                
                // audible.clips
                f = CreateNewLocalIntrinsicWithAssignOverride(_audibleObjectType, var_clips, assignOverrideDelegates, (ValMap instance, Value value) =>
                {
                    if (TryGetCachedReference<ValAudibleObject>(instance, out var reference) && reference.Audible != null)
                    {
                        var audible = reference.Audible;
                        if (value.TryCastAsMSType<ValList>(out ValList list))
                        {
                            var array = new IAudioAsset[list.values.Count];
                            for (int a = 0; a < array.Length; a++)
                            {
                                var ms = list.values[a];
                                if (TryGetCachedReference<ValAudioAsset>(ms, out var asset))
                                {
                                    array[a] = asset.Asset;
                                }
                            }
                            audible.clips = array;
                        }
                        else throw new RuntimeException($"{var_clips} must be a list containing {type_AnimationAsset} objects");
                    }

                    return true;
                });
                f.AddParam(var_self);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAudibleObject>(context.self, out var reference) || reference.Audible == null) return Intrinsic.Result.Null;
                    var audible = reference.Audible;
                    var array = audible.clips;
                    var list = new ValList();
                    if (array != null)
                    {
                        for (int a = 0; a < array.Length; a++) list.values.Add(ConvertAudioAsset(context, array[a]));
                    }
                    return new Intrinsic.Result(list);
                };

                // audible.bundles
                f = CreateNewLocalIntrinsicWithAssignOverride(_audibleObjectType, var_bundles, assignOverrideDelegates, (ValMap instance, Value value) =>
                {
                    if (TryGetCachedReference<ValAudibleObject>(instance, out var reference) && reference.Audible != null)
                    {
                        var audible = reference.Audible;
                        if (value.TryCastAsMSType<ValList>(out ValList list))
                        {
                            var array = new IAudioBundle[list.values.Count];
                            for (int a = 0; a < array.Length; a++)
                            {
                                var ms = list.values[a];
                                if (TryGetCachedReference<ValAudioBundle>(ms, out var asset))
                                {
                                    array[a] = asset.reference;
                                }
                            }
                            audible.bundles = array;
                        }
                        else throw new RuntimeException($"{var_bundles} must be a list containing {type_AnimationAsset} objects");
                    }

                    return true;
                });
                f.AddParam(var_self);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAudibleObject>(context.self, out var reference) || reference.Audible == null) return Intrinsic.Result.Null;
                    var audible = reference.Audible;
                    var array = audible.bundles;
                    var list = new ValList();
                    if (array != null)
                    {
                        for (int a = 0; a < array.Length; a++) list.values.Add(ConvertAudioBundle(context, array[a]));
                    }
                    return new Intrinsic.Result(list);
                };

            }
            return _audibleObjectType;
        }
        static SwoleScriptType _audibleObjectType = null;
        public static ValMap ConvertAudibleObject(TAC.Context context, IAudibleObject audible)
        {
            if (audible == null) return null;

            var ms_obj = AudibleObjectType().NewObject(context);
            var parent_obj = ConvertAudioSource(context, audible); // Get audio source object data first
            CopyParentObjectIntoChildObject(ms_obj, parent_obj);

            // Set audible object data
            ms_obj.map[varStr_cachedReference] = new ValAudibleObject(audible);

            return ms_obj;
        }

        public static SwoleScriptType AudioBundleType()
        {
            if (_audioBundleType == null)
            {
                var type = typeof(IAudioBundle);
                _audioBundleType = new SwoleScriptType(type_AudioBundle, type, EventHandlerType());
                _audioBundleType[var_genericName] = new ValString(type_AudioBundle);

                Intrinsic f;
                Dictionary<string, AssignmentOverrideDelegate> assignOverrideDelegates = new Dictionary<string, AssignmentOverrideDelegate>();

                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

                AddFieldsAsLocalIntrinsics(_audioBundleType, fields, ref assignOverrideDelegates);
                AddPropertiesAsLocalIntrinsics(_audioBundleType, properties, ref assignOverrideDelegates);
                AddMethodsAsLocalIntrinsics(_audioBundleType, methods, ref assignOverrideDelegates);

                // bundle.clip
                f = CreateNewLocalIntrinsicWithAssignOverride(_audioBundleType, var_clip, assignOverrideDelegates, (ValMap instance, Value value) =>
                {
                    if (TryGetCachedReference<ValAudioBundle>(instance, out var reference) && reference.reference != null)
                    {
                        var bundle = reference.reference;
                        ValAudioAsset asset = null;
                        if (TryGetCachedReference<ValAudioAsset>(value, out var _temp))
                        {
                            asset = _temp;
                        }

                        if (asset != null) bundle.clip = asset.Asset; else bundle.clip = null;
                    }
                    return true;
                });
                f.AddParam(var_self);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAudioBundle>(context.self, out var reference) || reference.reference == null) return Intrinsic.Result.Null;
                    return new Intrinsic.Result(ConvertAudioAsset(context, reference.reference.clip));
                };
                
                // bundle.source
                f = CreateNewLocalIntrinsicWithAssignOverride(_audioBundleType, var_source, assignOverrideDelegates, (ValMap instance, Value value) =>
                {
                    if (TryGetCachedReference<ValAudioBundle>(instance, out var reference) && reference.reference != null)
                    {
                        var bundle = reference.reference;
                        ValAudioSource source = null;
                        if (TryGetCachedReference<ValAudioSource>(value, out var _temp))
                        {
                            source = _temp;
                        }

                        if (source != null) bundle.source = source.Source; else bundle.source = null;
                    }
                    return true;
                });
                f.AddParam(var_self);
                f.code = (context, partialResult) =>
                {
                    if (!TryGetCachedReference<ValAudioBundle>(context.self, out var reference) || reference.reference == null) return Intrinsic.Result.Null;
                    return new Intrinsic.Result(ConvertAudioSource(context, reference.reference.source));
                };

            }
            return _audioBundleType;
        }
        static SwoleScriptType _audioBundleType = null;
        public static ValMap ConvertAudioBundle(TAC.Context context, IAudioBundle bundle)
        {
            if (bundle == null) return null;

            var ms_obj = AudioBundleType().NewObject(context);
            var parent_obj = ConvertEventHandler(context, bundle); // Get event handler object data first
            CopyParentObjectIntoChildObject(ms_obj, parent_obj);

            // Set audio bundle data
            ms_obj.map[varStr_cachedReference] = new ValAudioBundle(bundle);

            return ms_obj;
        }

        #endregion

        #endregion

#endif


    }

}
