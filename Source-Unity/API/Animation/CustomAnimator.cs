#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Jobs;

using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;

using Swole.Animation;


using Swole.Script;

using static Swole.API.Unity.Animation.CustomAnimation;
using static Swole.Animation.IAnimator;

namespace Swole.API.Unity.Animation
{
    public class CustomAnimatorUpdater : SingletonBehaviour<CustomAnimatorUpdater>
    {
        public const int ExecutionPriority = 80;
        public override int Priority => ExecutionPriority;
        public override bool DestroyOnLoad => false;

        protected readonly List<CustomAnimator> animators = new List<CustomAnimator>();
        public static bool Register(CustomAnimator animator)
        {
            var instance = Instance;
            if (animator == null || instance == null) return false;

            if (!instance.animators.Contains(animator)) instance.animators.Add(animator);
            return true;
        }
        public static bool Unregister(CustomAnimator animator)
        {
            var instance = Instance;
            if (animator == null || instance == null) return false;

            return instance.animators.Remove(animator);
        }

        public override void OnFixedUpdate()
        {
        }

        public override void OnLateUpdate()
        {
            float deltaTime = Time.deltaTime;
            foreach (var animator in animators) if (animator != null) animator.LateUpdateStep(deltaTime);
            animators.RemoveAll(i => i == null || i.OverrideUpdateCalls); 
        }

        public override void OnUpdate()
        {
            float deltaTime = Time.deltaTime;
            foreach (var animator in animators) if (animator != null) animator.UpdateStep(deltaTime);
        }
    }
    public class CustomAnimator : MonoBehaviour, IAnimator
    {

        public Type EngineComponentType => GetType();

        #region IComponent

        public EngineInternal.GameObject baseGameObject => UnityEngineHook.AsSwoleGameObject(gameObject);

        public object Instance => this;
        public int InstanceID => GetInstanceID();

        public bool IsDestroyed => isDisposed;

        public bool HasEventHandler => false;
        public IRuntimeEventHandler EventHandler => null;

        public void Destroy(float timeDelay = 0) => swole.Engine.Object_Destroy(this, timeDelay);

        public void AdminDestroy(float timeDelay = 0) => swole.Engine.Object_AdminDestroy(this, timeDelay);

        #endregion

        public CustomAnimationController defaultController;
        public IAnimationController DefaultController 
        {
            get => defaultController;
            set 
            {
                if (value is CustomAnimationController controller)
                {
                    defaultController = controller;
                } 
                else if (value == null)
                {
                    defaultController = null;
                }
                else
                {
                    swole.LogError($"Tried to set {nameof(defaultController)} for {nameof(CustomAnimator)} '{name}' - but the provided controller wasn't a valid type ({value.GetType().Name}).");
                }
            }
        }
        public void Reinitialize()
        {

            ClearControllerData();
            ApplyController(defaultController, false);

        }

        public void ApplyController(IAnimationController controller, bool usePrefix = true, bool incrementDuplicateParameters = false)
        {
            if (controller == null) return;

            string prefix = usePrefix ? controller.Prefix : "";

            IAnimationParameter[] parameters = controller.GetParameters(true); 

            if (parameters != null)
            {

                if (usePrefix) for (int a = 0; a < parameters.Length; a++) if (parameters[a] != null) parameters[a].Name = prefix + parameters[a].Name;

                if (incrementDuplicateParameters)
                {

                    for (int a = 0; a < parameters.Length; a++)
                    {

                        var param = parameters[a];
                        if (param == null) continue;

                        string baseName = param.Name;
                        int increment = 2;

                        while (FindParameterIndex(param.Name) >= 0)
                        {

                            param.Name = baseName + $".{increment}";
                            increment++;

                        }

                    }

                }

                AddParameters(parameters);

            }

            List<IAnimationLayer> instantiatedLayers = new List<IAnimationLayer>();
            if (controller is CustomAnimationController cac)
            {
                AddLayers(cac.layers, true, prefix, instantiatedLayers, true, controller);
            }
            else
            {
                AddLayers(controller.Layers, true, prefix, instantiatedLayers, true, controller);
            }

            if (parameters != null)
            {

                Dictionary<int, int> parameterRemapper = new Dictionary<int, int>();
                for (int a = 0; a < parameters.Length; a++)
                {

                    var param = parameters[a];
                    if (param == null) continue;

                    int parameterIndex = FindParameterIndex(param.Name);
                    if (parameterIndex < 0) continue;

                    parameterRemapper[a] = parameterIndex;

                }

                foreach (var layer in instantiatedLayers) layer?.RemapParameterIndices(parameterRemapper, true);

            }

        }

        public bool HasControllerData(IAnimationController controller)
        {
            if (controller == null) return false;
            return HasControllerData(controller.Prefix);
        }
        public bool HasControllerData(string prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix)) return false;

            prefix = prefix.AsID();
            if (m_parameters != null) foreach (var param in m_parameters) if (param != null && param.Name.AsID().StartsWith(prefix)) return true;
            if (m_animationLayers != null) foreach (var layer in m_animationLayers) if (layer != null && layer.Name.AsID().StartsWith(prefix)) return true;

            return false;
        }

        public void RemoveControllerData(IAnimationController controller)
        {

            if (controller == null) return;
            RemoveControllerData(controller.Prefix);

        }

        public void RemoveControllerData(string prefix)
        {

            if (string.IsNullOrEmpty(prefix)) return;
            RemoveLayersStartingWith(prefix);
            RemoveParametersStartingWith(prefix);

        }

        protected void Start()
        {

            Reinitialize();

        }

        public CustomAvatar avatar;
        [NonSerialized]
        protected CustomAvatar lastAvatar;
        public string AvatarName => avatar == null ? null : avatar.name;

        [Serializable]
        public class BoneMapping : IDisposable
        {

            public Transform rigContainer;

            public Transform[] bones;

            public BoneMapping(Transform rootTransform, CustomAvatar avatar)
            {

                if (rootTransform == null || avatar == null) return;

                rigContainer = string.IsNullOrEmpty(avatar.rigContainer) ? rootTransform : rootTransform.FindDeepChild(avatar.rigContainer);

                bones = new Transform[avatar.bones == null ? 0 : avatar.bones.Length];
                for (int a = 0; a < bones.Length; a++)
                {

                    if (string.IsNullOrEmpty(avatar.bones[a])) continue;

                    bones[a] = rigContainer.FindDeepChild(avatar.bones[a]);
                    if (bones[a] == null) bones[a] = rigContainer.FindDeepChildLiberal(avatar.bones[a]);

                }

            }

            protected TransformAccessArray jobArray;
            public TransformAccessArray JobArray
            {

                get
                {

                    if (!jobArray.isCreated)
                    {

                        jobArray = new TransformAccessArray(bones == null ? new Transform[0] : bones);

                    }

                    return jobArray;

                }

            }

            public void Dispose()
            {

                if (jobArray.isCreated) jobArray.Dispose();

                jobArray = default;

            }

        }

        [NonSerialized]
        protected BoneMapping m_bones;
        public BoneMapping Bones
        {

            get
            {

                if (avatar != null && (m_bones == null || lastAvatar != avatar))
                {

                    if (m_bones != null) m_bones.Dispose();

                    lastAvatar = avatar;

                    m_bones = new BoneMapping(transform, avatar);

                }

                return m_bones;

            }

        }

        public int GetBoneIndex(string name)
        {

            if (string.IsNullOrEmpty(name) || Bones == null) return -1;

            if (m_bones.bones != null)
            {

                if (avatar != null) name = avatar.Remap(name); 
                name = name.ToLower().Trim();

                for (int a = 0; a < m_bones.bones.Length; a++)
                {
                    var bone = m_bones.bones[a];
                    if (bone == null) continue;
                    if (bone.name.ToLower().Trim() == name) return a;

                }

            }

            return -1;

        }

        public int BoneCount 
        {

            get
            {
                if (Bones == null || m_bones.bones == null) return 0;
                return m_bones.bones.Length;
            }
        
        }
        public EngineInternal.ITransform GetBone(int index)
        {
            if (Bones == null || m_bones.bones == null || index < 0 || index >= m_bones.bones.Length) return null;
            return UnityEngineHook.AsSwoleTransform(m_bones.bones[index]);
        }

        /// <summary>
        /// Main method used by animation player to match transform curves with respective transforms in rig
        /// </summary>
        public Transform FindTransformInHierarchy(string name, bool isBone)
        {

            if (string.IsNullOrEmpty(name)) return null;

            if (isBone && Bones != null)
            {

                if (m_bones.bones != null)
                {

                    string liberalName = (avatar == null ? name : avatar.Remap(name));
                    if (string.IsNullOrEmpty(liberalName)) return null; 
                    liberalName = liberalName.ToLower().Trim();

                    for (int a = 0; a < m_bones.bones.Length; a++)
                    {
                        var bone = m_bones.bones[a];
                        if (bone == null) continue;
                        if (bone.name.ToLower().Trim() == liberalName) return bone;

                    }

                }

            }

            return transform.FindDeepChild(name);

        }

        public void ClearControllerData()
        {

            if (m_animationLayers != null)
            {

                foreach (var layer in m_animationLayers) if (layer != null) layer.Dispose();

                m_animationLayers = null;

            }

            if (m_parameters != null)
            {

                foreach (var parameter in m_parameters) if (parameter != null) parameter.Dispose();

                m_parameters = null;

            }

        }

        protected bool isDisposed;
        private static void CompleteAnimationPlayerJobs(IAnimationPlayer player)
        {
            if (player is not CustomAnimation.Player cap) return;
            cap.LastJobHandle.Complete();
        }
        public void Dispose()
        {

            if (m_animationLayers != null)
            {
                foreach (var layer in m_animationLayers) if (layer != null) layer.IteratePlayers(CompleteAnimationPlayerJobs);
            }
            m_jobHandle.Complete(); 

            isDisposed = true;

            if (m_bones != null) m_bones.Dispose();

            m_bones = null;

            if (m_transformStates.IsCreated) m_transformStates.Dispose();

            m_transformStates = default;

            if (m_transforms.isCreated) m_transforms.Dispose();

            m_transforms = default;
            m_transformHierarchies = null;

            ClearControllerData();

        }

        protected virtual void Awake()
        {
            SetOverrideUpdateCalls(OverrideUpdateCalls); // Force register to updater
        }

        protected virtual void OnDestroy()
        {

            if (!OverrideUpdateCalls) CustomAnimatorUpdater.Unregister(this);

            Dispose();

        }

        [Serializable, StructLayout(LayoutKind.Sequential)]
        public struct TransformState
        {

            public float3 unmodifiedLocalPosition;
            public float3 unmodifiedLocalScale;

            public float3 modifiedLocalPosition;
            public float3 modifiedLocalScale;

            public quaternion unmodifiedLocalRotation;
            public quaternion modifiedLocalRotation;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Unmodify(TransformAccess transform)
            {

                transform.SetLocalPositionAndRotation(unmodifiedLocalPosition + ((float3)transform.localPosition - modifiedLocalPosition), math.mul(unmodifiedLocalRotation, math.mul(math.inverse(modifiedLocalRotation), transform.localRotation)));
                transform.localScale = unmodifiedLocalScale + ((float3)transform.localScale - modifiedLocalScale);

            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void UnmodifyToBindPose(TransformAccess transform)
            {

                transform.SetLocalPositionAndRotation(unmodifiedLocalPosition, unmodifiedLocalRotation);
                transform.localScale = unmodifiedLocalScale + ((float3)transform.localScale - modifiedLocalScale); // REVIEW: Why are we not setting scale back to bind pose scale?

            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Modify(TransformAccess transform)
            {

                transform.SetLocalPositionAndRotation(modifiedLocalPosition, modifiedLocalRotation);
                transform.localScale = modifiedLocalScale;

            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public TransformState Reset(TransformAccess transform)
            {

                var state = this;

                state.unmodifiedLocalPosition = transform.localPosition;
                state.unmodifiedLocalRotation = transform.localRotation;
                state.unmodifiedLocalScale = transform.localScale;

                return state.ResetModifiedData();

            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public TransformState ResetModifiedData()
            {

                var state = this;

                state.modifiedLocalPosition = state.unmodifiedLocalPosition;
                state.modifiedLocalRotation = state.unmodifiedLocalRotation;
                state.modifiedLocalScale = state.unmodifiedLocalScale;

                return state;

            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public TransformState Apply(ITransformCurve.Data data)
            {

                var state = this;

                state.modifiedLocalPosition = data.localPosition;
                state.modifiedLocalRotation = data.localRotation;
                state.modifiedLocalScale = data.localScale;

                return state;

            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public TransformState ApplyMix(ITransformCurve.Data data, float mix)
            {

                var state = this;

                state.modifiedLocalPosition = math.lerp(state.unmodifiedLocalPosition, data.localPosition, mix);
                state.modifiedLocalRotation = math.slerp(state.unmodifiedLocalRotation, data.localRotation, mix);
                state.modifiedLocalScale = math.lerp(state.unmodifiedLocalScale, data.localScale, mix);

                return state;

            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public TransformState ApplyAdditive(ITransformCurve.Data data)
            {

                var state = this;

                state.modifiedLocalPosition = state.modifiedLocalPosition + data.localPosition;
                state.modifiedLocalRotation = math.mul(state.modifiedLocalRotation, data.localRotation);
                state.modifiedLocalScale = state.modifiedLocalScale + data.localScale;

                return state;

            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public TransformState ApplyAdditiveMix(ITransformCurve.Data data, float mix)
            {

                var state = this;

                state.modifiedLocalPosition = state.modifiedLocalPosition + data.localPosition * mix;
                state.modifiedLocalRotation = math.slerp(state.modifiedLocalRotation, math.mul(state.modifiedLocalRotation, data.localRotation), mix);
                state.modifiedLocalScale = state.modifiedLocalScale + data.localScale * mix;

                return state;

            }

            /// <summary>
            /// Only apply valid data to the state. If a curve is null or has no keyframes it is invalid.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public TransformState Swizzle(TransformState newState, bool3 validityPosition, bool4 validityRotation, bool3 validityScale)
            {

                var state = this;

                state.modifiedLocalPosition = math.select(state.modifiedLocalPosition, newState.modifiedLocalPosition, validityPosition);
                state.modifiedLocalScale = math.select(state.modifiedLocalScale, newState.modifiedLocalScale, validityScale);
                state.modifiedLocalRotation = math.select(state.modifiedLocalRotation.value, newState.modifiedLocalRotation.value, validityRotation);

                return state;

            }

        }

        [Serializable]
        public class TransformStateReference
        {

            public TransformStateReference() { }

            [NonSerialized]
            private CustomAnimator m_animator;
            public CustomAnimator Animator => m_animator;

            public TransformStateReference(CustomAnimator animator)
            {
                m_animator = animator;
            }

            public int index;

            public void Unmodify(Transform transform)
            {

                if (transform == null || m_animator == null) return;

                var state = m_animator.GetTransformState(index);

                transform.SetLocalPositionAndRotation(state.unmodifiedLocalPosition + ((float3)transform.localPosition - state.modifiedLocalPosition), state.unmodifiedLocalRotation * (Quaternion.Inverse(state.modifiedLocalRotation) * transform.localRotation));
                transform.localScale = state.unmodifiedLocalScale + ((float3)transform.localScale - state.modifiedLocalScale);

            }

            public void UnmodifyToBindPose(Transform transform)
            {

                if (transform == null || m_animator == null) return;

                var state = m_animator.GetTransformState(index);

                transform.SetLocalPositionAndRotation(state.unmodifiedLocalPosition, state.unmodifiedLocalRotation);
                transform.localScale = state.unmodifiedLocalScale;

            }

            public void Modify(Transform transform)
            {

                if (transform == null || m_animator == null) return;

                var state = m_animator.GetTransformState(index);

                transform.SetLocalPositionAndRotation(state.modifiedLocalPosition, state.modifiedLocalRotation);
                transform.localScale = state.modifiedLocalScale;

            }

            public void Reset(Transform transform)
            {

                if (m_animator == null) return;

                var state = m_animator.GetTransformState(index);

                state.unmodifiedLocalPosition = transform.localPosition;
                state.unmodifiedLocalRotation = transform.localRotation;
                state.unmodifiedLocalScale = transform.localScale;

                state.modifiedLocalPosition = state.unmodifiedLocalPosition;
                state.modifiedLocalRotation = state.unmodifiedLocalRotation;
                state.modifiedLocalScale = state.unmodifiedLocalScale;

                m_animator.SetTransformState(index, state);

            }

            public void ResetModifiedData()
            {

                if (m_animator == null) return;

                var state = m_animator.GetTransformState(index);

                state.modifiedLocalPosition = state.unmodifiedLocalPosition;
                state.modifiedLocalRotation = state.unmodifiedLocalRotation;
                state.modifiedLocalScale = state.unmodifiedLocalScale;

                m_animator.SetTransformState(index, state);

            }

            public void Apply(ITransformCurve curve, float t)
            {

                if (m_animator == null) return;

                var state = m_animator.GetTransformState(index);
                var data = curve.Evaluate(t);

                state.modifiedLocalPosition = data.localPosition;
                state.modifiedLocalRotation = data.localRotation;
                state.modifiedLocalScale = data.localScale;

                m_animator.SetTransformState(index, state);

            }

            public void ApplyMix(ITransformCurve curve, float t, float mix)
            {


                if (m_animator == null) return;

                var state = m_animator.GetTransformState(index);
                var data = curve.Evaluate(t);

                state.modifiedLocalPosition = math.lerp(state.unmodifiedLocalPosition, data.localPosition, mix);
                state.modifiedLocalRotation = math.slerp(state.unmodifiedLocalRotation, data.localRotation, mix);
                state.modifiedLocalScale = math.lerp(state.unmodifiedLocalScale, data.localScale, mix);

                m_animator.SetTransformState(index, state);

            }

            public void ApplyAdditive(ITransformCurve curve, float t)
            {

                if (m_animator == null) return;

                var state = m_animator.GetTransformState(index);
                var data = curve.Evaluate(t);

                state.modifiedLocalPosition = state.modifiedLocalPosition + data.localPosition;
                state.modifiedLocalRotation = math.mul(state.modifiedLocalRotation, data.localRotation);
                state.modifiedLocalScale = state.modifiedLocalScale + data.localScale;

                m_animator.SetTransformState(index, state);

            }

            public void ApplyAdditiveMix(ITransformCurve curve, float t, float mix)
            {

                if (m_animator == null) return;

                var state = m_animator.GetTransformState(index);
                var data = curve.Evaluate(t);

                state.modifiedLocalPosition = state.modifiedLocalPosition + data.localPosition * mix;
                state.modifiedLocalRotation = Quaternion.SlerpUnclamped(state.modifiedLocalRotation, math.mul(state.modifiedLocalRotation, data.localRotation), mix);
                state.modifiedLocalScale = state.modifiedLocalScale + data.localScale * mix;

                m_animator.SetTransformState(index, state);

            }

        }

        [Serializable]
        public class PropertyState
        {

            public MemberInfo info;

            public float unmodifiedValue;

            public float modifiedValue;

            public static float GetConvertedValue(MemberInfo info, float value)
            {

                var type = typeof(PropertyInfo).IsAssignableFrom(info.GetType()) ? (((PropertyInfo)info).PropertyType) : (((FieldInfo)info).FieldType);
                if (type == typeof(float))
                {

                    return value;

                }
                else if (type == typeof(int))
                {

                    return (int)value;

                }
                else if (type == typeof(bool))
                {

                    return value >= 0.5f ? 1 : 0;

                }

                return 0;

            }

            public static float GetValue(MemberInfo info, object instance)
            {
                if (info is PropertyInfo prop)
                {
                    var type = prop.PropertyType;
                    if (type == typeof(float))
                    {

                        return (float)prop.GetValue(instance);

                    }
                    else if (type == typeof(int))
                    {

                        return (int)prop.GetValue(instance);

                    }
                    else if (type == typeof(bool))
                    {

                        return (bool)prop.GetValue(instance) ? 1 : 0;

                    }
                }
                else
                {
                    FieldInfo field = (FieldInfo)info;

                    var type = field.FieldType;
                    if (type == typeof(float))
                    {

                        return (float)field.GetValue(instance);

                    }
                    else if (type == typeof(int))
                    {

                        return (int)field.GetValue(instance);

                    }
                    else if (type == typeof(bool))
                    {

                        return (bool)field.GetValue(instance) ? 1 : 0;

                    }
                }

                return 0;
            }

            public static void SetValue(MemberInfo info, object instance, float value)
            {

                if (info is PropertyInfo prop)
                {
                    var type = prop.PropertyType;
                    if (type == typeof(float))
                    {

                        prop.SetValue(instance, value);

                    }
                    else if (type == typeof(int))
                    {

                        prop.SetValue(instance, (int)value);

                    }
                    else if (type == typeof(bool))
                    {

                        prop.SetValue(instance, value >= 0.5f);

                    }
                }
                else
                {
                    FieldInfo field = (FieldInfo)info;

                    var type = field.FieldType;
                    if (type == typeof(float))
                    {

                        field.SetValue(instance, value);

                    }
                    else if (type == typeof(int))
                    {

                        field.SetValue(instance, (int)value);

                    }
                    else if (type == typeof(bool))
                    {

                        field.SetValue(instance, value >= 0.5f);

                    }
                }
            }

            public void Unmodify(object instance)
            {

                if (instance == null) return;

                SetValue(info, instance, unmodifiedValue + (GetValue(info, instance) - GetConvertedValue(info, modifiedValue)));

            }

            public void UnmodifyToBindValue(object instance)
            {

                if (instance == null) return;

                SetValue(info, instance, unmodifiedValue);

            }

            public void Modify(object instance)
            {

                if (instance == null) return;

                SetValue(info, instance, modifiedValue);

            }

            public void Reset(object instance)
            {

                unmodifiedValue = GetValue(info, instance);

                ResetModifiedData();

            }

            public void ResetModifiedData()
            {

                modifiedValue = unmodifiedValue;

            }

            public void Apply(IPropertyCurve curve, float t)
            {

                Apply(curve.Evaluate(t));

            }

            public void ApplyMix(IPropertyCurve curve, float t, float mix)
            {

                ApplyMix(curve.Evaluate(t), mix);

            }

            public void ApplyAdditive(IPropertyCurve curve, float t)
            {

                ApplyAdditive(curve.Evaluate(t));

            }

            public void ApplyAdditiveMix(IPropertyCurve curve, float t, float mix)
            {

                ApplyAdditiveMix(curve.Evaluate(t), mix);

            }

            public void Apply(IPropertyCurve.Frame data) => Apply(data.value);
            public void ApplyMix(IPropertyCurve.Frame data, float mix) => ApplyMix(data.value, mix);
            public void ApplyAdditive(IPropertyCurve.Frame data) => ApplyAdditive(data.value);
            public void ApplyAdditiveMix(IPropertyCurve.Frame data, float mix) => ApplyAdditiveMix(data.value, mix);

            public void Apply(float data)
            {
                modifiedValue = data;
            }
            public void ApplyMix(float data, float mix)
            {
                modifiedValue = math.lerp(unmodifiedValue, data, mix);
            }
            public void ApplyAdditive(float data)
            {
                modifiedValue = modifiedValue + data;
            }
            public void ApplyAdditiveMix(float data, float mix)
            {
                modifiedValue = modifiedValue + data * mix;
            }

        }

        [NonSerialized]
        protected JobHandle m_jobHandle;
        public JobHandle OutputDependency => m_jobHandle;

        [NonSerialized]
        protected Dictionary<Transform, TransformStateReference> m_transformStateReferences = new Dictionary<Transform, TransformStateReference>();
        [NonSerialized]
        protected NativeList<TransformState> m_transformStates;
        public NativeList<TransformState> TransformStates => m_transformStates;
        [NonSerialized]
        protected TransformAccessArray m_transforms;

        public int AffectedTransformCount => m_transforms.isCreated ? m_transforms.length : 0;
        public Transform GetTransform(int index)
        {

            if (!m_transforms.isCreated || index < 0 || index >= m_transforms.length) return null;
            return m_transforms[index];
        }
        public int GetTransformIndex(EngineInternal.ITransform transform)
        {
            if (transform == null || transform.Instance is not Transform t) return -1;
            return GetTransformIndex(t);
        }
        public int GetTransformIndex(Transform transform)
        {

            if (!m_transforms.isCreated) return -1;

            for (int a = 0; a < m_transforms.length; a++) if (m_transforms[a] == transform) return a;
            return -1;

        }

        public TransformState GetTransformState(int index)
        {

            if (!m_transformStates.IsCreated || index < 0 || index >= m_transformStates.Length) return default;

            return m_transformStates[index];

        }

        public void SetTransformState(int index, TransformState state)
        {

            if (!m_transformStates.IsCreated || index < 0 || index >= m_transformStates.Length) return;

            m_transformStates[index] = state;

        }

        protected Dictionary<string, PropertyState> m_propertyStates = new Dictionary<string, PropertyState>();
        protected Dictionary<string, Component> m_propertyStateBehaviours = new Dictionary<string, Component>();


        public TransformStateReference AddOrGetState(Transform transform)
        {

            if (!m_transformStateReferences.TryGetValue(transform, out TransformStateReference state))
            {

                m_jobHandle.Complete();

                if (!m_transformStates.IsCreated) m_transformStates = new NativeList<TransformState>(Allocator.Persistent);

                state = new TransformStateReference(this);
                state.index = m_transformStates.Length;
                m_transformStates.Add(new TransformState());

                if (!m_transforms.isCreated)
                {

                    m_transforms = new TransformAccessArray(new Transform[] { transform });

                }
                else
                {

                    m_transforms.Add(transform);

                }

                state.Reset(transform);
                m_transformStateReferences[transform] = state;
            }

            return state;

        }

        public PropertyState AddOrGetState(string propertyId, MemberInfo info)
        {

            if (!m_propertyStates.TryGetValue(propertyId, out PropertyState state))
            {

                state = new PropertyState();
                state.info = info;

                m_propertyStates[propertyId] = state;

                if (m_propertyStateBehaviours != null && m_propertyStateBehaviours.TryGetValue(propertyId, out var instance)) state.Reset(instance);

            }

            return state;

        }

        public PropertyState AddOrGetState(Component component, string memberName)
        {

            string propertyId = GetPropertyId(component, memberName);

            BindComponent(propertyId, component);

            return AddOrGetState(propertyId, GetFieldOrProperty(component, memberName));
        }

        public void BindComponent(string propertyId, Component component) => m_propertyStateBehaviours[propertyId] = component;
        public Component GetComponentByProperty(string propertyId)
        {

            if (m_propertyStateBehaviours.TryGetValue(propertyId, out Component component)) return component;

            return null;

        }

        private static List<Type> allComponentTypes;

        public static Type FindComponentTypes(string name)
        {

            if (allComponentTypes == null) allComponentTypes = typeof(Component).FindDerivedTypes();

            foreach (Type type in allComponentTypes) if (type.Name == name) return type;

            return null;

        }

        public Component FindAndBindComponent(string propertyId)
        {
            if (string.IsNullOrEmpty(propertyId)) return null;

            var component = GetComponentByProperty(propertyId);
            if (component != null) return component;

            string[] substrings = propertyId.Split('.');
            if (substrings.Length < 2) return null; 

            int i = 0;
            Transform objTransform = null;
            if (substrings[0] == _animatorTransformPropertyStringPrefix)
            {
                objTransform = transform; // Default to animator transform
                i = 1;
            }
            else
            {
                if (substrings.Length >= 4)
                {
                    objTransform = FindTransformInHierarchy($"{substrings[0]}.{substrings[1]}.{substrings[2]}", false); // In case the transform name includes periods
                    i = 3;
                }
                if (objTransform == null)
                {
                    if (substrings.Length >= 3)
                    {
                        objTransform = FindTransformInHierarchy($"{substrings[0]}.{substrings[1]}", false);  // In case the transform name includes periods
                        i = 2;
                    }
                    if (objTransform == null)
                    {
                        objTransform = FindTransformInHierarchy(substrings[0], false);
                        i = 1;
                        if (objTransform == null)
                        {
                            return null;
                        }
                    }
                }
            }

            Type componentType = FindComponentTypes(substrings[i]);
            if (componentType == null) return null;

            component = objTransform.GetComponent(componentType);
            if (component == null) return null;

            BindComponent(propertyId, component);

            return component;

        }

        public bool useDynamicBindPose;
        public bool UseDynamicBindPose
        {
            get => useDynamicBindPose;
            set => useDynamicBindPose = value;
        }
        public bool disableMultithreading;
        public bool DisableMultithreading
        {
            get => disableMultithreading;
            set => disableMultithreading = value;
        }
        [SerializeField]
        protected bool overrideUpdateCalls;
        public bool OverrideUpdateCalls
        {
            get => overrideUpdateCalls;
            set => SetOverrideUpdateCalls(value);
        }

        public void SetOverrideUpdateCalls(bool value)
        {
            overrideUpdateCalls = value;
            if (value)
            {
                CustomAnimatorUpdater.Unregister(this);
            } 
            else
            {
                CustomAnimatorUpdater.Register(this);
            }
        }
        public bool forceFinalTransformUpdate;
        public bool ForceFinalTransformUpdate
        {
            get => forceFinalTransformUpdate;
            set => forceFinalTransformUpdate = value;
        }

        protected void ResetDefaultTransformStates()
        {
            foreach (var pair in m_transformStateReferences)
            {
                var state = pair.Value;
                state.Reset(pair.Key);
            }
        }

        protected void ResetTransformStates()
        {

            if (useDynamicBindPose)
            {

                foreach (var pair in m_transformStateReferences)
                {

                    var state = pair.Value;

                    state.Unmodify(pair.Key);
                    state.Reset(pair.Key);

                }

            }
            else
            {

                foreach (var pair in m_transformStateReferences)
                {

                    var state = pair.Value;

                    state.UnmodifyToBindPose(pair.Key);
                    state.ResetModifiedData();

                }

            }

        }

        protected void ResetTransformStatesAsJob()
        {

            if (!m_transforms.isCreated || !m_transformStates.IsCreated) return;

            if (useDynamicBindPose)
            {

                m_jobHandle = new ResetTransformStatesJob()
                {

                    transformStates = m_transformStates

                }.Schedule(m_transforms, m_jobHandle);

            }
            else
            {

                m_jobHandle = new ResetTransformStatesToBindPoseJob()
                {

                    transformStates = m_transformStates

                }.Schedule(m_transforms, m_jobHandle);

            }

        }

        [BurstCompile]
        public struct ResetTransformStatesJob : IJobParallelForTransform
        {

            [NativeDisableParallelForRestriction]
            public NativeList<TransformState> transformStates;

            public void Execute(int index, TransformAccess transform)
            {

                var state = transformStates[index];

                state.Unmodify(transform);
                state = state.Reset(transform);

                transformStates[index] = state;

            }

        }

        [BurstCompile]
        public struct ResetTransformStatesToBindPoseJob : IJobParallelForTransform
        {

            [NativeDisableParallelForRestriction]
            public NativeList<TransformState> transformStates;

            public void Execute(int index, TransformAccess transform)
            {

                var state = transformStates[index];

                state.UnmodifyToBindPose(transform);
                state = state.ResetModifiedData();

                transformStates[index] = state;

            }

        }

        protected void ResetPropertyStates()
        {

            if (useDynamicBindPose)
            {

                foreach (var pair in m_propertyStates)
                {

                    if (m_propertyStateBehaviours.TryGetValue(pair.Key, out var behaviour))
                    {

                        var state = pair.Value;

                        state.Unmodify(behaviour);
                        state.Reset(behaviour);

                    }

                }

            }
            else
            {

                foreach (var pair in m_propertyStates)
                {

                    if (m_propertyStateBehaviours.TryGetValue(pair.Key, out var behaviour))
                    {

                        var state = pair.Value;

                        state.UnmodifyToBindValue(behaviour);
                        state.ResetModifiedData();

                    }

                }

            }

        }

        protected void ApplyTransformStates()
        {

            m_jobHandle.Complete();

            foreach (var pair in m_transformStateReferences)
            {

                var state = pair.Value;

                state.Modify(pair.Key);

            }

        }

        protected void ApplyTransformStatesAsJob()
        {

            if (!m_transforms.isCreated || !m_transformStates.IsCreated) return;

            m_jobHandle = new ApplyTransformStatesJob()
            {

                transformStates = m_transformStates

            }.Schedule(m_transforms, m_jobHandle);

        }

        [BurstCompile]
        public struct ApplyTransformStatesJob : IJobParallelForTransform
        {

            [NativeDisableParallelForRestriction]
            public NativeList<TransformState> transformStates;

            public void Execute(int index, TransformAccess transform)
            {

                transformStates[index].Modify(transform);

            }

        }

        protected void ApplyPropertyStates()
        {

            foreach (var pair in m_propertyStates)
            {

                if (m_propertyStateBehaviours.TryGetValue(pair.Key, out var behaviour))
                {

                    var state = pair.Value;

                    state.Modify(behaviour);

                }

            }

        }

        public class UnityTransformHierarchy : TransformHierarchy
        {

            public UnityTransformHierarchy(IAnimator animator, int index, int[] transformIndices) : base(animator, index, transformIndices) { }

            public UnityTransformHierarchy(CustomAnimator animator, int index, TransformAccessArray transformIndices) : base(animator, index, null)
            {
                m_transformIndices = new int[transformIndices.length];
                for (int a = 0; a < transformIndices.length; a++) m_transformIndices[a] = animator.GetTransformIndex(transformIndices[a]);
            }

            public bool Contains(TransformAccessArray exTransformIndices)
            {

                if (m_transformIndices == null || animator is not CustomAnimator cAnimator) return false;

                if (exTransformIndices.isCreated) for (int a = 0; a < exTransformIndices.length; a++) if (!Contains(cAnimator.GetTransformIndex(exTransformIndices[a]))) return false;

                return true;

            }

        }

        /// <summary>
        /// Mainly used to save on setting final positions/rotations of transforms until the last update job with that hierarchy.
        /// </summary>
        [NonSerialized]
        protected List<UnityTransformHierarchy> m_transformHierarchies;
        public int TransformHierarchyCount => m_transformHierarchies == null ? 0 : m_transformHierarchies.Count;
        public TransformHierarchy GetTransformHierarchy(int index)
        {
            if (m_transformHierarchies == null || index < 0 || index >= m_transformHierarchies.Count) return null;
            return GetTransformHierarchyUnsafe(index);
        }
        public TransformHierarchy GetTransformHierarchyUnsafe(int index) => m_transformHierarchies[index];
        
        public UnityTransformHierarchy GetTransformHierarchy(TransformAccessArray array)
        {
            int[] indices = new int[array.length];
            for (int a = 0; a < array.length; a++) indices[a] = GetTransformIndex(array[a]);
            return (UnityTransformHierarchy)GetTransformHierarchy(indices);
        }
        public UnityTransformHierarchy GetTransformHierarchy(ICollection<Transform> collection)
        {
            return GetTransformHierarchy(collection.ToArray());
        }
        public UnityTransformHierarchy GetTransformHierarchy(Transform[] array)
        {
            int[] indices = new int[array.Length];
            for (int a = 0; a < array.Length; a++) indices[a] = GetTransformIndex(array[a]);
            return (UnityTransformHierarchy)GetTransformHierarchy(indices);
        }
        public TransformHierarchy GetTransformHierarchy(int[] transformIndices)
        {

            if (m_transformHierarchies == null) m_transformHierarchies = new List<UnityTransformHierarchy>();

            foreach (UnityTransformHierarchy th in m_transformHierarchies) if (th.IsIdentical(transformIndices)) return th;

            UnityTransformHierarchy hierarchy = new UnityTransformHierarchy(this, m_transformHierarchies.Count, transformIndices);

            int parentIndexCount = int.MaxValue;
            int parent = -1;
            for (int a = 0; a < m_transformHierarchies.Count; a++)
            {

                UnityTransformHierarchy th = m_transformHierarchies[a];
                if (th.Contains(hierarchy) && (parent < 0 || th.Count < parentIndexCount))
                {

                    parent = a;
                    parentIndexCount = th.Count;

                }

            }

            hierarchy.parent = parent;

            m_transformHierarchies.Add(hierarchy);

            return hierarchy;

        }

        //

        [SerializeField]
        protected List<IAnimationParameter> m_parameters;
        public IAnimationParameter GetParameter(int index)
        {

            if (m_parameters == null || index < 0 || index >= m_parameters.Count) return null;
            return m_parameters[index];
        }
        public void AddParameter(IAnimationParameter parameter, bool initialize = true, object initObject = null, List<IAnimationParameter> outList = null, bool onlyOutputNew = false)
        {

            if (parameter == null) return;
            string idName = parameter.Name.ToLower().Trim();

            if (m_parameters == null) m_parameters = new List<IAnimationParameter>();
            for (int a = 0; a < m_parameters.Count; a++) if (m_parameters[a] != null && (m_parameters[a] == parameter || m_parameters[a].Name.ToLower().Trim() == idName))
                {

                    if (!onlyOutputNew && outList != null) outList.Add(m_parameters[a]);

                    return;

                }

            parameter.IndexInAnimator = m_parameters.Count;
            m_parameters.Add(parameter);
            if (outList != null) outList.Add(parameter);

            if (initialize) parameter.Initialize(this, initObject);

        }
        public void AddParameters(ICollection<IAnimationParameter> toAdd, bool initialize = true, object initObject = null, List<IAnimationParameter> outList = null, bool onlyOutputNew = false)
        {
            if (toAdd == null) return;
            if (initialize && outList == null) outList = new List<IAnimationParameter>();
            foreach (var parameter in toAdd) AddParameter(parameter, false, null, outList, onlyOutputNew);
            if (initialize) foreach (var parameter in outList) parameter?.Initialize(this, initObject);
        }
        public bool RemoveParameter(IAnimationParameter parameter)
        {
            if (parameter == null || m_parameters == null) return false;
            for (int a = 0; a < m_parameters.Count; a++)
            {
                if (m_parameters[a] == parameter)
                {
                    m_parameters.RemoveAt(a);
                    parameter.Dispose();
                    RecalculateParameterIndices();
                    return true;
                }
            }
            return false;
        }
        public bool RemoveParameter(int index)
        {
            if (m_parameters == null || index < 0 || index >= m_parameters.Count) return false;

            var parameter = m_parameters[index];
            if (parameter != null) parameter.Dispose();
            m_parameters.RemoveAt(index);
            RecalculateParameterIndices();

            return true;
        }
        public int RemoveParametersStartingWith(string prefix)
        {
            if (m_parameters == null) return 0;

            prefix = prefix.ToLower().Trim();
            int i = m_parameters.RemoveAll(i => i == null || i.DisposeIfHasPrefix(prefix));

            if (i > 0) RecalculateParameterIndices();

            return i;
        }
        public int FindParameterIndex(string name)
        {

            if (m_parameters == null) return -1;

            for (int a = 0; a < m_parameters.Count; a++) if (m_parameters[a].Name == name) return a;
            name = name.ToLower().Trim();
            for (int a = 0; a < m_parameters.Count; a++) if (!string.IsNullOrEmpty(m_parameters[a].Name) && m_parameters[a].Name.ToLower().Trim() == name) return a;

            return -1;

        }
        public IAnimationParameter FindParameter(string name, out int parameterIndex)
        {

            parameterIndex = FindParameterIndex(name);
            if (parameterIndex >= 0) return null;

            return GetParameter(parameterIndex);

        }
        public IAnimationParameter FindParameter(string name)
        {
            return FindParameter(name, out _);
        }
        public Dictionary<int, int> RecalculateParameterIndices()
        {

            Dictionary<int, int> remapper = new Dictionary<int, int>();

            if (m_parameters == null) return remapper;

            for (int a = 0; a < m_parameters.Count; a++)
            {

                var parameter = m_parameters[a];
                if (parameter == null) continue;

                if (parameter.IndexInAnimator >= 0) remapper[parameter.IndexInAnimator] = a;
                parameter.IndexInAnimator = a;

            }

            if (m_animationLayers != null)
            {

                foreach (var layer in m_animationLayers) if (layer != null) layer.RemapParameterIndices(remapper, true);

            }

            return remapper;

        }

        [NonSerialized]
        protected List<IAnimationLayer> m_animationLayers;
        public void AddLayer(IAnimationLayer layer, bool instantiate = true, string prefix = "", List<IAnimationLayer> outList = null, bool onlyOutputNew = false, IAnimationController animationController = null)
        {

            if (m_animationLayers == null) m_animationLayers = new List<IAnimationLayer>();
            InsertLayer(m_animationLayers.Count, layer, instantiate, prefix, outList, onlyOutputNew, animationController);

        }
        public void InsertLayer(int index, IAnimationLayer layer, bool instantiate = true, string prefix = "", List<IAnimationLayer> outList = null, bool onlyOutputNew = false, IAnimationController animationController = null)
        {

            if (layer == null || !layer.Valid) return;
            string idName = layer.Name.AsID();

            if (m_animationLayers == null) m_animationLayers = new List<IAnimationLayer>();
            for (int a = 0; a < m_animationLayers.Count; a++) if (m_animationLayers[a] != null && (m_animationLayers[a] == layer || m_animationLayers[a].Name.AsID() == idName))
                {

                    if (!onlyOutputNew && outList != null) outList.Add(layer);

                    return;

                }

            if (instantiate)
            {
                layer = layer.NewInstance(this, animationController);
                layer.Name = prefix + layer.Name;
            }
            if (outList != null) outList.Add(layer);

            if (index < 0) index = 0;
            if (index >= m_animationLayers.Count)
            {

                layer.IndexInAnimator = m_animationLayers.Count;
                m_animationLayers.Add(layer);

            }
            else
            {

                m_animationLayers.Insert(index, layer);
                layer.IndexInAnimator = -1;
                RecalculateLayerIndices();

            }

        }
        public void AddLayers(ICollection<IAnimationLayer> toAdd, bool instantiate = true, string prefix = "", List<IAnimationLayer> outList = null, bool onlyOutputNew = false, IAnimationController animationController = null)
        {
            if (toAdd == null) return;
            foreach (var layer in toAdd) AddLayer(layer, instantiate, prefix, outList, onlyOutputNew, animationController);
        }
        /*public void AddLayers(ICollection<CustomAnimationLayer> toAdd, bool instantiate = true, string prefix = "", List<IAnimationLayer> outList = null, bool onlyOutputNew = false, IAnimationController animationController = null)
        {
            if (toAdd == null) return;
            foreach (var layer in toAdd) AddLayer(layer, instantiate, prefix, outList, onlyOutputNew, animationController);
        }*/
        public int FindLayerIndex(string layerName)
        {
            if (m_animationLayers == null) return -1;

            for (int a = 0; a < m_animationLayers.Count; a++) if (m_animationLayers[a] != null && m_animationLayers[a].Name == layerName) return a;
            layerName = layerName.ToLower().Trim();
            for (int a = 0; a < m_animationLayers.Count; a++) if (m_animationLayers[a] != null && m_animationLayers[a].Name.ToLower().Trim() == layerName) return a;

            return -1;

        }
        public IAnimationLayer FindLayer(string layerName)
        {
            if (m_animationLayers == null) return null;

            int index = FindLayerIndex(layerName);
            if (index < 0) return null;

            return m_animationLayers[index];
        }
        public CustomAnimationLayer FindTypedLayer(string layerName)
        {
            if (m_animationLayers == null) return null;

            int index = FindLayerIndex(layerName);
            if (index < 0) return null;

            var l = m_animationLayers[index];
            if (l is CustomAnimationLayer cal) return cal;
            return null;
        }
        public bool RemoveLayer(IAnimationLayer layer)
        {
            if (m_animationLayers == null || layer == null) return false;

            if (m_animationLayers.Remove(layer))
            {
                layer.Dispose();
                RecalculateLayerIndices();
                return true;
            }
            return false;

        }
        public bool RemoveLayer(int layerIndex)
        {
            if (m_animationLayers == null || layerIndex < 0 || layerIndex >= m_animationLayers.Count) return false;
            return RemoveLayer(m_animationLayers[layerIndex]);
        }
        public bool RemoveLayer(string layerName)
        {
            return RemoveLayer(FindLayer(layerName));
        }
        public int RemoveLayersStartingWith(string prefix)
        {
            if (m_animationLayers == null) return 0;

            prefix = prefix.ToLower().Trim();
            int i = m_animationLayers.RemoveAll(i => i == null || i.DisposeIfHasPrefix(prefix));

            if (i > 0) RecalculateLayerIndices();

            return i;
        }
        private void RearrangeLayerInternal(int layerIndex, int swapIndex)
        {
            if (layerIndex == swapIndex || m_animationLayers == null || layerIndex < 0 || layerIndex >= m_animationLayers.Count) return;

            if (swapIndex >= 0 && swapIndex < m_animationLayers.Count)
            {
                var swap = m_animationLayers[swapIndex];
                m_animationLayers[swapIndex] = m_animationLayers[layerIndex];
                m_animationLayers[layerIndex] = swap;
            }
            else
            {
                var layer = m_animationLayers[layerIndex];
                m_animationLayers.RemoveAt(layerIndex);
                if (swapIndex < 0 && m_animationLayers.Count > 0) { m_animationLayers.Insert(0, layer); } else { m_animationLayers.Add(layer); }
            }
        }
        public Dictionary<int, int> RearrangeLayer(int layerIndex, int swapIndex, bool recalculateIndices = true)
        {
            RearrangeLayerInternal(layerIndex, swapIndex);
            return recalculateIndices ? null : RecalculateLayerIndices();
        }
        public void RearrangeLayerNoRemap(int layerIndex, int swapIndex, bool recalculateIndices = true)
        {
            RearrangeLayerInternal(layerIndex, swapIndex);
            if (recalculateIndices) RecalculateLayerIndicesNoRemap();
        }
        public Dictionary<int, int> RecalculateLayerIndices()
        {

            Dictionary<int, int> remapper = new Dictionary<int, int>();

            if (m_animationLayers == null) return remapper;

            for (int a = 0; a < m_animationLayers.Count; a++) if (m_animationLayers[a] != null)
                {

                    var layer = m_animationLayers[a];
                    if (layer == null) continue;

                    if (layer.IndexInAnimator >= 0) remapper[layer.IndexInAnimator] = a;
                    layer.IndexInAnimator = a;

                }

            return remapper;

        }
        public void RecalculateLayerIndicesNoRemap()
        {
            if (m_animationLayers == null) return;

            for (int a = 0; a < m_animationLayers.Count; a++) if (m_animationLayers[a] != null)
                {
                    var layer = m_animationLayers[a];
                    if (layer == null) continue;

                    layer.IndexInAnimator = a;
                }
        }

        public bool IsLayerActive(int index)
        {

            if (m_animationLayers == null || index < 0 || index >= m_animationLayers.Count) return false;

            return m_animationLayers[index] == null ? false : m_animationLayers[index].HasActiveState;

        }

        protected TransformHierarchy GetNextLayerTransformHierarchy(int startIndex, out int nextIndex)
        {

            nextIndex = -1;
            if (startIndex >= m_animationLayers.Count) return null;
            startIndex = math.max(0, startIndex);

            for (int a = startIndex; a < m_animationLayers.Count; a++)
            {

                var layer = m_animationLayers[a];
                if (layer == null || !layer.IsActive) continue;

                var hierarchy = layer.GetActiveTransformHierarchy();
                if (hierarchy == null) continue;

                nextIndex = a;
                return hierarchy;

            }

            return null;

        }

        protected void UpdateAnimationLayers(float deltaTime)
        {

            int nextIndex = -1;
            TransformHierarchy nextHierarchy = null; 

            if (m_animationLayers != null)
                for (int a = 0; a < m_animationLayers.Count; a++)
                {

                    var layer = m_animationLayers[a];
                    if (layer is not CustomAnimationLayer cal || !layer.IsActive) continue;

                    TransformHierarchy localHierarchy = a == nextIndex ? nextHierarchy : null;
                    if (nextIndex <= a) nextHierarchy = GetNextLayerTransformHierarchy(a + 1, out nextIndex);

                    var nextLayer = (nextIndex <= a || nextIndex >= m_animationLayers.Count) ? null : m_animationLayers[nextIndex];
                    m_jobHandle = cal.Progress(nextHierarchy, nextLayer == null ? true : (nextLayer.IsAdditive || nextLayer.Mix != 1 || nextLayer.Deactivate), deltaTime, disableMultithreading, false, m_jobHandle, localHierarchy);

                }

        }

        //

        public UnityEvent OnPreUpdate;
        public UnityEvent OnPostUpdate;

        public virtual void UpdateStep(float deltaTime)
        {

            OnPreUpdate?.Invoke();


            if (disableMultithreading) ResetTransformStates(); else ResetTransformStatesAsJob();
            ResetPropertyStates();

            UpdateAnimationLayers(deltaTime);

            if (disableMultithreading) ApplyTransformStates(); else if (forceFinalTransformUpdate) ApplyTransformStatesAsJob();
            ApplyPropertyStates();


            OnPostUpdate?.Invoke();

        }

        public UnityEvent OnPreLateUpdate;
        public UnityEvent OnPostLateUpdate;

        public virtual void LateUpdateStep(float deltaTime)
        {

            OnPreLateUpdate?.Invoke();


            m_jobHandle.Complete();


            OnPostLateUpdate?.Invoke();

        }

        /*protected virtual void Update()
        {

            if (overrideUpdateCalls) return;

            UpdateStep(Time.deltaTime);

        }

        protected virtual void LateUpdate()
        {

            if (overrideUpdateCalls) return;

            LateUpdateStep(Time.deltaTime);

        }*/

    }
}

#endif