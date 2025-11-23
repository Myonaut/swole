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

        public static JobHandle FinalAnimationJobHandle => ProxyBoneJobs.OutputDependency;
        public static int FinalAnimationBehaviourPriority => CharacterExpressionsJobs.ExecutionPriority;
        public static int StartAnimationBehaviourPriority => ExecutionPriority; 
        
        public static int ExecutionPriority => 80;
        public override int Priority => ExecutionPriority;
        public override bool DestroyOnLoad => false;

        protected readonly List<CustomAnimator> animators = new List<CustomAnimator>();
        public static bool Register(CustomAnimator animator)
        {
            var instance = Instance;
            if (animator == null || instance == null) return false;

#if UNITY_EDITOR
            Debug.Log($"Registered Animator {animator.name}"); 
#endif

            if (!instance.animators.Contains(animator)) instance.animators.Add(animator);
            return true;
        }
        public static bool Unregister(CustomAnimator animator)
        {
            var instance = Instance;
            if (animator == null || instance == null) return false;

#if UNITY_EDITOR
            Debug.Log($"Unregistered Animator {animator.name}");
#endif

            return instance.animators.Remove(animator);
        }

        private readonly List<VoidParameterlessDelegate> postFixedUpdateWork = new List<VoidParameterlessDelegate>();
        public static void AddPostFixedUpdateWork(VoidParameterlessDelegate work)
        {
            var instance = Instance;
            if (instance == null) return;

            instance.postFixedUpdateWork.Add(work);
        }

        public override void OnFixedUpdate() => OnFixedUpdate(Time.fixedDeltaTime);
        public void OnFixedUpdate(float deltaTime)
        {
            foreach (var animator in animators)
            {
                if (animator != null)
                {
                    if (animator.disabledBySkip)
                    {
                        animator.skipTimeFixed += deltaTime;
                    }
                    else if (animator.isActiveAndEnabled) animator.FixedUpdateStep(deltaTime);
                }
            }

            foreach (var work in postFixedUpdateWork) work?.Invoke(); 
            postUpdateWork.Clear(); 
        }

        private readonly List<VoidParameterlessDelegate> postUpdateWork = new List<VoidParameterlessDelegate>();
        public static void AddPostUpdateWork(VoidParameterlessDelegate work)
        {
            var instance = Instance;
            if (instance == null) return;

            instance.postUpdateWork.Add(work); 
        }
        public override void OnUpdate() => OnUpdate(Time.deltaTime);
        public void OnUpdate(float deltaTime)
        {
            foreach (var animator in animators) 
            {
                if (animator == null) continue;

                if (animator.updateRate > 0)
                {
                    animator.skipTime += deltaTime;
                    if (animator.skipTime >= animator.updateRate)
                    {
                        animator.skipTime -= deltaTime;
                        if (animator.disabledBySkip) 
                        { 
                            animator.enabled = true;
                            animator.disabledBySkip = false;
                        }
                    } 
                    else if (animator.enabled)
                    {
                        animator.enabled = false;
                        animator.disabledBySkip = true;
                    }
                }
                
                if (animator.isActiveAndEnabled) animator.UpdateStep(deltaTime);
            }

            foreach (var work in postUpdateWork) work?.Invoke();
            postUpdateWork.Clear();
        }

        private readonly List<VoidParameterlessDelegate> postLateUpdateWork = new List<VoidParameterlessDelegate>();
        public static void AddPostLateUpdateWork(VoidParameterlessDelegate work)
        {
            var instance = Instance;
            if (instance == null) return;

            instance.postLateUpdateWork.Add(work);
        }
        public override void OnLateUpdate() => OnLateUpdate(Time.deltaTime);
        public void OnLateUpdate(float deltaTime)
        {
            foreach (var animator in animators) if (animator != null && animator.isActiveAndEnabled) animator.LateUpdateStep(deltaTime);
            animators.RemoveAll(i => i == null || i.OverrideUpdateCalls);

            foreach (var work in postLateUpdateWork) work?.Invoke();
            postLateUpdateWork.Clear();
        }
    }

    public class CustomAnimator : MonoBehaviour, IAnimator
    {

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (reinitializeBindPose)
            {
                reinitializeBindPose = false;
                ReinitializeBindPose();
            }
        }
#endif

        public const float boolTruthThreshold = 0.01f;

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

        public float updateRate;
        [NonSerialized]
        public float skipTime;
        [NonSerialized]
        public float skipTimeFixed;
        [NonSerialized]
        public bool disabledBySkip;

        public void DisableIKControllers()
        {
            if (IkManager == null) return;
            ikManager.DisableAllTogglableControllers(); 
        }
        public void EnableIKControllers()
        {
            if (IkManager == null) return;
            ikManager.EnableAllTogglableControllers();
        }
        public void ResetIKControllers()
        {
            if (IkManager == null) return;
            ikManager.ResetPositionWeightOfAllTogglableControllers();
            ikManager.ResetRotationWeightOfAllTogglableControllers();
            ikManager.ResetBendGoalWeightOfAllTogglableControllers();
            DisableIKControllers();
        }

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
        public void ReinitializeControllers()
        {

            ClearControllerData();
            ApplyController(defaultController, false);

        }

        public void ApplyController(IAnimationController controller, bool usePrefixForLayers = true, bool incrementDuplicateParameters = false, bool usePrefixForParameters = false)
        {
            if (controller == null) return;

            string prefix = controller.Prefix; 

            IAnimationParameter[] parameters = controller.GetParameters(true); 

            if (parameters != null)
            {

                if (usePrefixForParameters) for (int a = 0; a < parameters.Length; a++) if (parameters[a] != null) parameters[a].Name = prefix + parameters[a].Name;

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
                AddLayers(cac.layers, true, usePrefixForLayers ? prefix : string.Empty, instantiatedLayers, true, controller);
            }
            else
            {
                AddLayers(controller.Layers, true, usePrefixForLayers ? prefix : string.Empty, instantiatedLayers, true, controller);
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

        public void RemoveControllerData(IAnimationController controller, bool disposeLayers = true)
        {

            if (controller == null) return;
            RemoveControllerData(controller.Prefix, disposeLayers);
            RemoveLayersFromSource(controller);
        }

        public void RemoveControllerData(string prefix, bool disposeLayers = true)
        {

            if (string.IsNullOrEmpty(prefix)) return;
            RemoveLayersStartingWith(prefix, disposeLayers);
            RemoveParametersStartingWith(prefix);

        }

        protected void Start()
        {

            //ReinitializeControllers(); // moved to Awake

        }

        [SerializeField]
        protected CustomIKManager ikManager;
        public CustomIKManager IkManager
        {
            get
            {
                if (ikManager == null) ikManager = gameObject.GetComponentInChildren<CustomIKManager>();
                return ikManager;
            }
        }

        protected class IKOffsetState
        {
            public Vector3 offsetPos;
            public Quaternion offsetRot;
        }
        protected IKOffsetState[] ikOffsetStates;
        protected IKOffsetState GetIKOffsetState(int index)
        {
            if (ikOffsetStates == null)
            {
                if (Bones != null && m_bones.ikBones != null) ikOffsetStates = new IKOffsetState[m_bones.ikBones.Length]; 
            }

            return ikOffsetStates == null || index < 0 || index >= ikOffsetStates.Length ? null : ikOffsetStates[index];
        }
        public void SyncIKFKOffsets() => SyncIKFKOffsets(true);
        public void SyncIKFKOffsets(bool onlyEnabledIKControllers)
        {
            if (Bones != null && m_bones.ikBones != null)
            {
                for (int a = 0; a < m_bones.ikBones.Length; a++)
                {
                    var ikBone = m_bones.ikBones[a];
                    if (ikBone.boneIndex < 0 || ikBone.fkBoneIndex < 0) continue;

                    var bone = m_bones.bones[ikBone.boneIndex];
                    var fkBone = m_bones.bones[ikBone.fkBoneIndex];
                    if (bone == null || fkBone == null) continue;

                    if (ikBone.ikController == null && ikBone.boneIndex >= 0 && IkManager != null && ikManager.TryFindAssociatedIKController(bone, out var controller))
                    {
                        ikBone.ikController = controller;
                    }

                    if (onlyEnabledIKControllers && ikBone.ikController != null && !ikBone.ikController.IsActive) continue;

                    var offsetState = GetIKOffsetState(a);
                    if (offsetState == null && ikOffsetStates != null)
                    {
                        offsetState = new IKOffsetState();
                        ikOffsetStates[a] = offsetState;
                    }

                    if (offsetState != null)
                    {
                        offsetState.offsetPos = fkBone.InverseTransformPoint(bone.position);
                        offsetState.offsetRot = Quaternion.Inverse(fkBone.rotation) * bone.rotation;
                    }
                }
            }
        }
        public void SyncIKFK() => SyncIKFK(true);
        public void SyncIKFK(bool onlyDisabledIKControllers)
        {
            if (Bones != null && m_bones.ikBones != null)
            {
                for (int a = 0; a < m_bones.ikBones.Length; a++)
                {
                    var ikBone = m_bones.ikBones[a];
                    if (ikBone.boneIndex < 0 || ikBone.fkBoneIndex < 0) continue;

                    var bone = m_bones.bones[ikBone.boneIndex];
                    var fkBone = m_bones.bones[ikBone.fkBoneIndex];
                    if (bone == null || fkBone == null) continue;

                    if (ikBone.ikController == null && ikBone.boneIndex >= 0 && IkManager != null && ikManager.TryFindAssociatedIKController(bone, out var controller))
                    {
                        ikBone.ikController = controller;
                    }

                    if (onlyDisabledIKControllers && ikBone.ikController != null && ikBone.ikController.IsActive) continue;
                     
                    var offsetState = GetIKOffsetState(a);
                    if (offsetState == null && ikOffsetStates != null)
                    {
                        offsetState = new IKOffsetState();
                        offsetState.offsetPos = ikBone.avatarBone.fkOffsetPosition;
                        offsetState.offsetRot = Quaternion.Euler(ikBone.avatarBone.fkOffsetEulerRotation);
                        ikOffsetStates[a] = offsetState;
                    }

                    var offsetPos = ikBone.avatarBone.fkOffsetPosition;
                    var offsetRot = Quaternion.identity;
                    if (offsetState != null)
                    {
                        offsetPos = offsetState.offsetPos;
                        offsetRot = offsetState.offsetRot;
                    }
                    else
                    {
                        offsetRot = Quaternion.Euler(ikBone.avatarBone.fkOffsetEulerRotation);
                    }

                    //Vector3 currentPos = Vector3.zero;
                    //Quaternion currentRot = Quaternion.identity;
                    Vector3 worldPos = Vector3.zero;
                    Quaternion worldRot = Quaternion.identity;
                    if (ikBone.avatarBone.usePositionOffsetFK && ikBone.avatarBone.useRotationOffsetFK)
                    {
                        //if (outputTransforms != null) bone.GetPositionAndRotation(out currentPos, out currentRot); 

                        worldPos = fkBone.TransformPoint(offsetPos);
                        worldRot = fkBone.rotation * offsetRot;
                        bone.SetPositionAndRotation(worldPos, worldRot);
                    }
                    else if (ikBone.avatarBone.useRotationOffsetFK)
                    {
                        //if (outputTransforms != null) currentRot = bone.rotation;

                        worldRot = fkBone.rotation * offsetRot;
                        bone.rotation = worldRot; 
                    }
                    else if (ikBone.avatarBone.usePositionOffsetFK)
                    {
                        //if (outputTransforms != null) currentPos = bone.position;

                        worldPos = fkBone.TransformPoint(offsetPos);
                        bone.position = worldPos;
                    }

                    /*if (outputTransforms != null)
                    {
                        if (currentPos != worldPos || currentRot != worldRot) outputTransforms.Add(bone); 
                    }*/
                }
            }
        }
        public bool IsIKControlledBone(Transform bone)
        {
            if (IkManager == null || bone == null) return false; 

            for(int a = 0; a < ikManager.ControllerCount; a++)
            {
                var controller = ikManager[a];
                if (!controller.IsActive) continue;

                for(int b = 0; b < controller.ChainLength; b++)
                {
                    var chainT = controller[b];
                    if (chainT == bone) return true;
                }
            }

            return false;
        }
        public bool IsFKControlledBone(Transform bone)
        {
            if (IkManager == null || bone == null) return false;

            for (int a = 0; a < ikManager.ControllerCount; a++)
            {
                var controller = ikManager[a];
                if (controller.IsActive) continue;

                if (controller.Target == bone) return true;
                if (controller.BendGoal == bone) return true;
            }

            return false;
        }

        public CustomAvatar avatar;
        [NonSerialized]
        protected CustomAvatar lastAvatar;
        public string AvatarName => avatar == null ? null : avatar.name;
        public string RemapBoneName(string boneName) => avatar == null ? boneName : avatar.Remap(boneName);

        public bool finalizeAnimationsBeforePhysics;

        [Serializable]
        public enum RootMotionMode
        {
            Default, Physics
        }

        public bool applyRootMotion;
        public RootMotionMode rootMotionMode;
        public Vector3 forwardAxis = Vector3.forward;
        public Vector3 yawAxis = Vector3.up;

        protected Transform rootMotionBone;
        protected TransformState rootMotionBoneStartState;
        public void ReinitializeRootMotionBoneStartState() 
        {
            if (rootMotionBone == null) return;
            rootMotionBoneStartState = new TransformState(rootMotionBone, false);

            currentRootTranslation = Vector3.zero;
            currentRootRotation = Quaternion.identity;
        }
        public Transform RootMotionBone
        {
            get
            {
                if (rootMotionBone == null)
                {
                    rootMotionBone = RootBoneUnity;
                    ReinitializeRootMotionBoneStartState();
                }

                return rootMotionBone;
            }
            set
            {
                rootMotionBone = value;
            }
        }
        protected Vector3 currentRootTranslation = Vector3.zero;
        protected Quaternion currentRootRotation = Quaternion.identity;

        public void GetRootMotion(out Vector3 rootTranslation, out Quaternion rootRotation) 
        {
            rootTranslation = currentRootTranslation;
            rootRotation = currentRootRotation;
        }
        public void SetRootMotion(Vector3 rootTranslation, Quaternion rootRotation) 
        {
            currentRootTranslation = rootTranslation;
            currentRootRotation = rootRotation;
        }

        public delegate void ApplyRootMotionDelegate(Transform rootTransform, Vector3 rootTranslationIn, Quaternion rootRotationIn, out Vector3 rootTranslationOut, out Quaternion rootRotationOut);
        protected readonly List<ApplyRootMotionDelegate> rootMotionListeners = new List<ApplyRootMotionDelegate>();

        public void AddRootMotionListener(ApplyRootMotionDelegate listener)
        {
            if (listener == null || rootMotionListeners.Contains(listener)) return;
            rootMotionListeners.Add(listener);
        }
        public void RemoveRootMotionListener(ApplyRootMotionDelegate listener)
        {
            if (listener == null) return;
            rootMotionListeners.Remove(listener);
        }

        protected void ResetRootMotionBone()
        {
            if (rootMotionBone == null) return;

            rootMotionBoneStartState.ApplyLocal(rootMotionBone); // force bone back to start pose
        }
        protected void ApplyRootMotion()
        {
            var rootTranslation = currentRootTranslation;  
            var rootRotation = currentRootRotation;

            if (rootMotionBone != null && rootMotionBone.parent != null)
            {
                var toWorld = rootMotionBone.parent.rotation;

                rootTranslation = toWorld * rootTranslation; 
                rootRotation = (toWorld * (rootRotation * rootMotionBoneStartState.rotation)) * Quaternion.Inverse(toWorld * rootMotionBoneStartState.rotation);
                //rootTranslation = rootRotation * toWorld * rootTranslation;
            }

            foreach(var listener in rootMotionListeners)
            {
                listener?.Invoke(transform, rootTranslation, rootRotation, out rootTranslation, out rootRotation); 
            }

            ApplyRootMotion(rootTranslation, rootRotation);

            //Debug.DrawRay(position, rootTranslation * 100, Color.magenta, 2);

            currentRootTranslation = Vector3.zero;
            currentRootRotation = Quaternion.identity;
        }
        public void ApplyRootMotion(Vector3 rootTranslation, Quaternion rootRotation)
        {
            transform.GetPositionAndRotation(out Vector3 position, out Quaternion rotation);
            transform.SetPositionAndRotation(position + rootTranslation, rootRotation * rotation);
        }

        [Serializable]
        public class IKBone
        {
            public CustomAvatar.IkBone avatarBone;
            public int boneIndex;
            public int fkBoneIndex;
            public CustomIKManager.IKController ikController;
        }
        public const string rootLevelIdentifier = "%";
        [Serializable]
        public class BoneMapping : IDisposable
        {

            public Transform rigContainer;

            public Transform[] bones;
            public IKBone[] ikBones;

            public bool IsIKBone(int boneIndex) => IsIKBone(boneIndex, out _);
            public bool IsIKBone(int boneIndex, out IKBone ikBone) 
            {
                ikBone = null;
                return bones == null || boneIndex < 0 || boneIndex >= bones.Length ? false : IsIKBone(bones[boneIndex]);
            }
            public bool IsIKBone(Transform bone) => IsIKBone(bone, out _);
            public bool IsIKBone(Transform bone, out IKBone ikBone) 
            {
                ikBone = null;
                if (bone == null || ikBones == null) return false;

                foreach (var ikBone_ in ikBones) if (bones[ikBone_.boneIndex] == bone) 
                    { 
                        ikBone = ikBone_;
                        return true;
                    }

                return false;
            }

            public BoneMapping(Transform rootTransform, CustomAvatar avatar, CustomIKManager ikManager)
            {

                if (rootTransform == null || avatar == null) return;

                rigContainer = string.IsNullOrEmpty(avatar.rigContainer) ? rootTransform : rootTransform.FindDeepChild(avatar.rigContainer);
                if (rigContainer == null) rigContainer = rootTransform;

                bones = new Transform[(avatar.bones == null ? 0 : avatar.bones.Length) + (avatar.ikBones == null ? 0 : avatar.ikBones.Length)];
                ikBones = new IKBone[avatar.ikBones == null ? 0 : avatar.ikBones.Length];
                for (int a = 0; a < bones.Length; a++)
                {
                    string boneName = a >= avatar.bones.Length ? avatar.ikBones[a - avatar.bones.Length].name : avatar.bones[a];
                    if (string.IsNullOrEmpty(boneName)) continue;

                    if (boneName == rootLevelIdentifier)
                    {
                        bones[a] = rigContainer;
                    }
                    else
                    {
                        bones[a] = (a >= avatar.bones.Length ? rootTransform : rigContainer).FindDeepChild(boneName);
                        if (bones[a] == null) bones[a] = rigContainer.FindDeepChildLiberal(boneName);
                    }

                    if (a >= avatar.bones.Length)
                    {
                        int i = a - avatar.bones.Length;
                        ikBones[i] = new IKBone() { avatarBone = avatar.ikBones[i], boneIndex = a, fkBoneIndex = -1 };
                        if (ikManager != null && ikManager.TryFindAssociatedIKController(bones[a], out var controller))
                        {
                            ikBones[i].ikController = controller;
                        }
                    }
                }

                for(int a = 0; a < ikBones.Length; a++)
                {
                    var ikBone = ikBones[a];

                    if (string.IsNullOrWhiteSpace(ikBone.avatarBone.fkParent)) continue;

                    string fkParentName = ikBone.avatarBone.fkParent;
                    if (avatar != null) fkParentName = avatar.Remap(fkParentName); 
                    fkParentName = fkParentName.AsID();

                    for (int b = 0; b < bones.Length; b++)
                    {
                        var bone = bones[b];
                        if (bone == null) continue;
                        if (bone.name.AsID() == fkParentName) 
                        {
                            ikBone.fkBoneIndex = b; 
                            break;
                        }
                    }
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

                    m_bones = new BoneMapping(transform, avatar, IkManager);

                }

                return m_bones;

            }

        }
        public bool HasBones => Bones != null && m_bones.bones != null;

        public EngineInternal.ITransform RootBone => UnityEngineHook.AsSwoleTransform(RootBoneUnity); 
        public Transform RootBoneUnity
        {
            get
            {
                //if (Bones == null || m_bones.rigContainer == null) return transform; 
                //return !avatar.containerIsRoot && m_bones.rigContainer.childCount == 1 ? m_bones.rigContainer.GetChild(0) : m_bones.rigContainer; // Determine root bone, which can be different from the rigContainer. If the rigContainer has more than one child, the rigContainer is probably the root bone.

                if (avatar == null || string.IsNullOrWhiteSpace(avatar.RootBone)) return null;
                
                if (Bones != null && m_bones.bones != null)
                {
                    string rootBoneName = avatar.RootBone;
                    if (avatar != null) rootBoneName = avatar.Remap(rootBoneName);
                    rootBoneName = rootBoneName.AsID();

                    for (int a = 0; a < m_bones.bones.Length; a++)
                    {
                        var bone = m_bones.bones[a];
                        if (bone == null) continue;
                        if (bone.name.AsID() == rootBoneName)
                        {
                            return bone;
                        }
                    }
                }

                return null;
            }
        }

        public int GetBoneIndex(string name)
        {

            if (string.IsNullOrEmpty(name) || Bones == null) return -1;

            if (m_bones.bones != null)
            {

                if (avatar != null) name = avatar.Remap(name); 
                name = name.AsID();

                for (int a = 0; a < m_bones.bones.Length; a++)
                {
                    var bone = m_bones.bones[a];
                    if (bone == null) continue;
                    if (bone.name.AsID() == name) return a;

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
        public Transform GetUnityBone(int index)
        {
            if (Bones == null || m_bones.bones == null || index < 0 || index >= m_bones.bones.Length) return null;
            return m_bones.bones[index];
        }
        public EngineInternal.ITransform GetBone(int index) => UnityEngineHook.AsSwoleTransform(GetUnityBone(index));

        public Transform GetUnityBone(string boneName) => GetUnityBone(GetBoneIndex(boneName)); 
        public EngineInternal.ITransform GetBone(string boneName) => GetBone(GetBoneIndex(boneName));

        /// <summary>
        /// Main method used by animation player to match transform curves with respective transforms in rig
        /// </summary>
        public Transform FindTransformInHierarchy(string name/*, bool isBone*/)
        {

            if (string.IsNullOrEmpty(name)) return null;

            if (/*isBone && */Bones != null)
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
                        var libName = bone.name.ToLower().Trim();
                        if (libName == liberalName) return bone;
                        if (avatar != null)
                        {
                            libName = avatar.Remap(bone.name).ToLower().Trim();
                            if (libName == liberalName) return bone; 
                        }
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

#if UNITY_EDITOR
            Debug.Log($"Disposing animator {name}");
#endif

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

        public bool dontAutoInitialize;
        public bool dontInitializeBoneStates;
        public void Initialize()
        {
            if (!ResetToPreInitializedBindPose()) ReinitializeBindPose(); 

            if (!dontInitializeBoneStates) 
            { 
                if (HasBones)
                {
                    foreach(var bone in m_bones.bones) 
                    { 
                        if (bone != null) AddOrGetState(bone); 
                    }
                }
            }

            ResetIKControllers();
        }
        protected virtual void Awake()
        {
            SetOverrideUpdateCalls(OverrideUpdateCalls); // Force register to updater
            if (!dontAutoInitialize) Initialize();  

            if (defaultController != null)
            {
                ReinitializeControllers();
            }

            if (rootMotionBone == null) rootMotionBone = RootMotionBone;
        }

        protected virtual void OnDestroy()
        {
            if (!OverrideUpdateCalls) CustomAnimatorUpdater.Unregister(this);
            Dispose();
        }

        [Serializable, StructLayout(LayoutKind.Sequential)]
        public struct TransformAnimationState
        {

            public float3 unmodifiedLocalPosition;
            public float3 unmodifiedLocalScale;

            public float3 modifiedLocalPosition;
            public float3 modifiedLocalScale;

            public quaternion unmodifiedLocalRotation;
            public quaternion modifiedLocalRotation;

            public quaternion previousRotationMod;

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
            public TransformAnimationState Reset(TransformAccess transform)
            {

                var state = this;

                state.previousRotationMod = quaternion.identity;

                state.unmodifiedLocalPosition = transform.localPosition;
                state.unmodifiedLocalRotation = transform.localRotation;
                state.unmodifiedLocalScale = transform.localScale;

                return state.ResetModifiedData();

            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public TransformAnimationState ResetModifiedData()
            {

                var state = this;

                state.previousRotationMod = quaternion.identity;

                state.modifiedLocalPosition = state.unmodifiedLocalPosition;
                state.modifiedLocalRotation = state.unmodifiedLocalRotation;
                state.modifiedLocalScale = state.unmodifiedLocalScale;

                return state;

            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public TransformAnimationState Apply(ITransformCurve.Data data)
            {

                var state = this;

                //var rotMod = Maths.JobsEnsureQuaternionContinuity(state.previousRotationMod, data.localRotation);
                //state.previousRotationMod = rotMod;
                var rotMod = data.localRotation;

                state.modifiedLocalPosition = data.localPosition; 
                state.modifiedLocalRotation = rotMod;
                state.modifiedLocalScale = data.localScale; 

                return state;

            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public TransformAnimationState ApplyMix(ITransformCurve.Data data, float mix)
            {

                var state = this;

                //var rotMod = Maths.JobsEnsureQuaternionContinuity(state.previousRotationMod, data.localRotation);
                //state.previousRotationMod = rotMod;
                var rotMod = data.localRotation;

                state.modifiedLocalPosition = math.lerp(state.unmodifiedLocalPosition, data.localPosition, mix);
                state.modifiedLocalRotation = math.slerp(state.unmodifiedLocalRotation, rotMod, mix);
                state.modifiedLocalScale = math.lerp(state.unmodifiedLocalScale, data.localScale, mix);

                return state;

            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public TransformAnimationState ApplyAdditive(ITransformCurve.Data data)
            {

                var state = this;

                //var rotMod = Maths.JobsEnsureQuaternionContinuity(state.previousRotationMod, data.localRotation);
                //state.previousRotationMod = rotMod;
                var rotMod = data.localRotation;

                state.modifiedLocalPosition = state.modifiedLocalPosition + data.localPosition;
                state.modifiedLocalRotation = math.mul(state.modifiedLocalRotation, rotMod);
                state.modifiedLocalScale = state.modifiedLocalScale + data.localScale; 

                return state;

            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public TransformAnimationState ApplyAdditiveMix(ITransformCurve.Data data, float mix)
            {

                var state = this;

                //var rotMod = Maths.JobsEnsureQuaternionContinuity(state.previousRotationMod, data.localRotation);
                //state.previousRotationMod = rotMod;
                var rotMod = data.localRotation;

                state.modifiedLocalPosition = state.modifiedLocalPosition + data.localPosition * mix;  
                state.modifiedLocalRotation = math.slerp(state.modifiedLocalRotation, math.mul(state.modifiedLocalRotation, rotMod), mix); 
                state.modifiedLocalScale = state.modifiedLocalScale + data.localScale * mix; 

                return state;

            }

            /// <summary>
            /// Only apply valid data to the state. If a curve is null or has no keyframes it is invalid.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public TransformAnimationState Swizzle(TransformAnimationState newState, bool3 validityPosition, bool4 validityRotation, bool3 validityScale)
            {

                var state = this;

                //state.previousRotationMod = math.select(state.previousRotationMod.value, newState.previousRotationMod.value, validityRotation);

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

            public void Reset(Transform transform) => Reset(new TransformBindState(transform));
            public void Reset(TransformBindState bindState)
            {

                if (m_animator == null) return;

                var state = m_animator.GetTransformState(index);

                state.unmodifiedLocalPosition = bindState.localPosition;
                state.unmodifiedLocalRotation = bindState.localRotation;
                state.unmodifiedLocalScale = bindState.localScale;

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

        public struct PropertyMemberInfo
        {

            public MemberInfo info;
            public Type type;
            public Type elementType;
            public bool IsElement => elementType != null;
            public object[] elementIndex;
            public int ElementIndex => elementIndex == null ? 0 : elementIndex.Length <= 0 ? 0 : (int)elementIndex[0];
            public PropertyInfo indexer;

            public void SetElementValue(object instance, object value)
            {
                if (elementType == null) return;

                if (indexer == null && type.IsArray)
                {
                    ((Array)instance).SetValue(value, ElementIndex);
                }

                indexer.GetValue(instance, elementIndex);
            }
            public object GetElementValue(object instance)
            {
                if (elementType == null) return instance;

                if (indexer == null) 
                {
                    return type.IsArray ? ((Array)instance).GetValue(ElementIndex) : instance;
                }

                return indexer.GetValue(instance, elementIndex);   
            }

            public PropertyMemberInfo(MemberInfo info, int elementIndex, Type elementType)
            {
                bool isElement = elementType != null;

                this.info = info;
                this.elementIndex = isElement ? new object[] { elementIndex } : null;
                this.elementType = elementType;

                type = info is PropertyInfo prop ? prop.PropertyType : (info is FieldInfo field ? field.FieldType : null);

                if (elementType != null && !type.IsArray)
                {
                    indexer = (type == null ? null : type.GetProperty("Item", BindingFlags.Public | BindingFlags.Instance));
#if UNITY_EDITOR
                    if (indexer == null) Debug.LogError($"The type {type} does not have an indexer.");
#endif
                }
                else
                {
                    indexer = null; 
                }
            }
        }
        [Serializable]
        public class PropertyState
        {

            protected string path;
            public string Path => path;
            public string ID => path;

            protected PropertyMemberInfo[] info;
            public bool IsDynamic => ReferenceEquals(info, null);

            public int index;

            public float unmodifiedValue;

            public float modifiedValue;


            public PropertyState(string path)
            {
                this.path = path;
                index = -1;
            }
            public PropertyState(string path, int index)
            {
                this.path = path;
                this.index = index;
            }
            public PropertyState(string path, PropertyMemberInfo[] info)
            {
                this.path = path;
                index = -1;
                this.info = info;
            }

            public static float GetConvertedValue(PropertyMemberInfo[] info, float value) => info == null || info.Length <= 0 ? value : GetConvertedValue(info[info.Length - 1].info, value);
            public static float GetConvertedValue(MemberInfo info, float value)
            {

                var type = info == null ? typeof(float) : (typeof(PropertyInfo).IsAssignableFrom(info.GetType()) ? (((PropertyInfo)info).PropertyType) : (((FieldInfo)info).FieldType));
                if (type.IsArray) 
                { 
                    type = type.GetElementType(); 
                } 
                else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    type = type.GetGenericArguments()[0];
                }
                else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                {
                    type = type.GetGenericArguments()[0];
                }

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
                    return value >= boolTruthThreshold ? 1 : 0; 
                }

                return 0;

            }

            public static float GetValue(PropertyMemberInfo[] info, object instance, int index)
            {
                if (info == null || info.Length <= 0)
                {
                    if (index >= 0 && instance is DynamicAnimationProperties dap)
                    {
                        return dap.GetValueUnsafe(index);
                    }

                    return 0f;
                }

                int lengthM1 = info.Length - 1;
                object finalInstance = instance;
                for(int a = 0; a < lengthM1; a++)
                {
                    var member = info[a];
                    if (member.info is PropertyInfo prop)
                    {
                        finalInstance = prop.GetValue(finalInstance);
                    }
                    else if (member.info is FieldInfo field)
                    {
                        finalInstance = field.GetValue(finalInstance);
                    }

                    if (member.IsElement)
                    {
                        finalInstance = member.GetElementValue(finalInstance);
                    }
                }

                var finalInfo = info[lengthM1];
                var finalMemInfo = finalInfo.info;
                if (finalInfo.IsElement)
                {
                    return GetFloatValue(finalInfo.elementType, finalInfo.GetElementValue(finalInstance));
                }

                return GetValue(finalMemInfo, finalInstance, index);
            }
            public static float GetValue(MemberInfo info, object instance, int index)
            {

                if (instance is DynamicAnimationProperties dap)
                {
                    return index >= 0 ? dap.GetValueUnsafe(index) : 0f; 
                }
                else if (info is PropertyInfo prop)
                {
                    return GetFloatValue(prop.PropertyType, prop.GetValue(instance));
                }
                else if (info is FieldInfo field)
                {
                    return GetFloatValue(field.FieldType, field.GetValue(instance));
                }

                return 0;
            }
            public static float GetValueFromIndexer(MemberInfo info, object instance, object[] indices)
            {
                if (info is PropertyInfo prop)
                {
                    return GetFloatValue(prop.PropertyType, prop.GetValue(instance, indices));
                }

                return 0;
            }
            public static float GetFloatValue(Type type, object value)
            {
                if (type == typeof(float))
                {
                    return (float)value;
                }
                else if (type == typeof(int))
                {
                    return (int)value;
                }
                else if (type == typeof(bool))
                {
                    return (bool)value ? 1 : 0;
                }

                return 0f;
            }
            public static object GetTypedValue(Type type, float value)
            {
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
                    return value >= boolTruthThreshold/*value >= 0.5f*/;
                }

                if (type.IsValueType)
                {
                    return Activator.CreateInstance(type);
                }
                return null;
            }

            public static void SetValue(PropertyMemberInfo[] info, object instance, float value, int index)
            {
                if (info == null || info.Length <= 0)
                {
                    if (instance is DynamicAnimationProperties dap)
                    {
                        dap.SetValueUnsafe(index, value);
                    }

                    return;
                }

                int lengthM1 = info.Length - 1;
                object finalInstance = instance;
                for (int a = 0; a < lengthM1; a++)
                {
                    var member = info[a];
                    if (member.info is PropertyInfo prop)
                    {
                        finalInstance = prop.GetValue(finalInstance); 
                    }
                    else if (member.info is FieldInfo field)
                    {
                        finalInstance = field.GetValue(finalInstance);
                    }

                    if (member.IsElement)
                    {
                        finalInstance = member.GetElementValue(finalInstance);
                    }
                }

                var finalInfo = info[lengthM1];
                var finalMemInfo = finalInfo.info;
                if (finalInfo.IsElement)
                {
                    finalInfo.SetElementValue(finalInstance, GetTypedValue(finalInfo.elementType, value)); 
                    return;
                }

                SetValue(info[lengthM1].info, finalInstance, value, index);  
            }
            public static void SetValue(MemberInfo info, object instance, float value, int index)
            {
                if (instance is DynamicAnimationProperties dap)
                {
                    dap.SetValueUnsafe(index, value);
                }
                else if (info is PropertyInfo prop)
                {
                    prop.SetValue(instance, GetTypedValue(prop.PropertyType, value));
                }
                else if (info is FieldInfo field)
                {
                    field.SetValue(instance, GetTypedValue(field.FieldType, value));
                }
            }
            public static void SetValueFromIndexer(MemberInfo info, object instance, float value, object[] indices)
            {
                if (info is PropertyInfo prop)
                {
                    prop.SetValue(instance, GetTypedValue(prop.PropertyType, value), indices);
                }
            }

            public void Unmodify(object instance)
            {

                if (instance == null) return;

                SetValue(info, instance, unmodifiedValue + (GetValue(info, instance, index) - GetConvertedValue(info, modifiedValue)), index);

            }

            public void UnmodifyToBindValue(object instance)
            {

                if (instance == null) return;

                SetValue(info, instance, unmodifiedValue, index);

            }

            public void Modify(object instance)
            {

                if (instance == null) return;

                SetValue(info, instance, modifiedValue, index);

            }

            public void Reset(object instance)
            {

                unmodifiedValue = GetValue(info, instance, index);

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
        protected NativeList<TransformAnimationState> m_transformStates;
        public NativeList<TransformAnimationState> TransformStates => m_transformStates;
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

        public TransformAnimationState GetTransformState(int index)
        {

            if (!m_transformStates.IsCreated || index < 0 || index >= m_transformStates.Length) return default;

            return m_transformStates[index];

        }

        public void SetTransformState(int index, TransformAnimationState state)
        {

            if (!m_transformStates.IsCreated || index < 0 || index >= m_transformStates.Length) return;

            m_transformStates[index] = state;

        }

        protected Dictionary<string, PropertyState> m_propertyStates = new Dictionary<string, PropertyState>();
        protected Dictionary<string, Component> m_propertyStateBehaviours = new Dictionary<string, Component>();

        [Serializable]
        public struct TransformBindState
        {
            public Transform transform;
            public float3 localPosition;
            public float3 localScale;
            public quaternion localRotation; 

            public TransformBindState(Transform transform)
            {
                this.transform = transform;
                if (transform == null)
                {
                    this.localPosition = float3.zero;
                    this.localRotation = quaternion.identity;
                    this.localScale = new float3(1, 1, 1);
                }
                else
                {
                    this.localPosition = transform.localPosition;
                    this.localRotation = transform.localRotation;
                    this.localScale = transform.localScale;
                }
            }

            public void Apply() => Apply(this.transform);
            public void Apply(Transform transform)
            {
                transform.SetLocalPositionAndRotation(localPosition, localRotation);
                transform.localScale = localScale;
            }
        }

#if UNITY_EDITOR
        [SerializeField]
        private bool reinitializeBindPose;
#endif
        [SerializeField]
        private TransformBindState[] preInitializedBindPose;
        public TransformBindState[] PreInitializedBinePose => preInitializedBindPose;
        public bool HasPreInitializedBindPose => preInitializedBindPose != null && preInitializedBindPose.Length > 0;
        public void SetPreInitializedBinePose(TransformBindState[] preInitializedBindPose)
        {
            this.preInitializedBindPose = preInitializedBindPose;
        }
        public bool ResetToPreInitializedBindPose()
        {
            if (preInitializedBindPose == null || preInitializedBindPose.Length <= 0) return false; 

            if (IkManager != null) ikManager.ForceInitializeSolvers(); // Make sure ik solvers are initialized before changing pose. If the pose is different from the bind pose, it's typically to add more bend to limbs, so that the ik solvers know how to rotate them.

            foreach (var bindState in preInitializedBindPose) if (bindState.transform != null) 
                { 
                    bindState.Apply(); 
                }

            return true;
        }
        public void ResetToBindPose() => ResetToPreInitializedBindPose();
        public void ReinitializeBindPose() 
        {
            if (Bones != null && m_bones.bones != null)
            {
                preInitializedBindPose = new TransformBindState[m_bones.bones.Length]; 
                for (int a = 0; a < m_bones.bones.Length; a++) preInitializedBindPose[a] = new TransformBindState(m_bones.bones[a]);  
            }
        }
        public bool TryGetPreInitializedBindState(Transform transform, out TransformBindState bindState)
        {
            if (preInitializedBindPose == null || preInitializedBindPose.Length <= 0) 
            {
                bindState = new TransformBindState(transform);
                return false; 
            }

            foreach(var bindState_ in preInitializedBindPose) if (bindState_.transform == transform)
                {
                    bindState = bindState_;
                    return true;
                }

            bindState = new TransformBindState(transform);
            return false;
        }

        public TransformStateReference AddOrGetState(Transform transform)
        {

            if (!m_transformStateReferences.TryGetValue(transform, out TransformStateReference state))
            {

                m_jobHandle.Complete();

                if (!m_transformStates.IsCreated) m_transformStates = new NativeList<TransformAnimationState>(Allocator.Persistent);

                state = new TransformStateReference(this);
                state.index = m_transformStates.Length;
                m_transformStates.Add(new TransformAnimationState()
                {
                    unmodifiedLocalRotation = quaternion.identity,
                    modifiedLocalRotation = quaternion.identity,
                    previousRotationMod = quaternion.identity
                });

                if (!m_transforms.isCreated)
                {

                    m_transforms = new TransformAccessArray(new Transform[] { transform });

                }
                else
                {

                    m_transforms.Add(transform);

                }

                TryGetPreInitializedBindState(transform, out var bindState); // defaults to the current transform state if not found
                state.Reset(bindState);

                m_transformStateReferences[transform] = state;
            }

            return state;

        }

        /*public PropertyState AddOrGetState(Component component, MemberInfo info) // does not work for nested properties
        {

            string propertyId = GetPropertyId(component, info.Name);

            if (!m_propertyStates.TryGetValue(propertyId, out PropertyState state))
            {

                BindComponent(propertyId, component);

                state = new PropertyState();
                state.info = info;

                m_propertyStates[propertyId] = state;

                if (m_propertyStateBehaviours != null && m_propertyStateBehaviours.TryGetValue(propertyId, out var instance)) state.Reset(instance);

            }

            return state;

        }*/

        public PropertyState AddOrGetState(Component component, string memberPath)
        {

            string propertyId = GetPropertyId(component, memberPath);

            if (!m_propertyStates.TryGetValue(propertyId, out PropertyState state))
            {

                BindComponent(propertyId, component);

                if (component is DynamicAnimationProperties dap)
                {
                    state = new PropertyState(memberPath);
                    state.index = dap.IndexOf(memberPath);
                    if (state.index < 0) swole.LogError($"Invalid dynamic property '{memberPath}' for component '{dap.name}'");  
                } 
                else
                {
                    state = new PropertyState(memberPath, GetFieldOrProperty(component, memberPath));
                }


                m_propertyStates[propertyId] = state;

                if (m_propertyStateBehaviours != null && m_propertyStateBehaviours.TryGetValue(propertyId, out var instance)) state.Reset(instance);

            }

            return state;
        }

        public void BindComponent(string propertyId, Component component) => m_propertyStateBehaviours[propertyId] = component;
        public Component GetComponentByProperty(string propertyId, out string remainingId)
        {

            remainingId = propertyId;
            if (m_propertyStateBehaviours.TryGetValue(propertyId, out Component component)) 
            {
                if (component != null)
                {
                    string typeName = component.GetType().Name;
                    var typeIndex = remainingId.IndexOf(typeName);
                    if (typeIndex >= 0)
                    {
                        int subIndex = typeIndex + typeName.Length + 1; // +1 for the dot after the type name
                        remainingId = subIndex < remainingId.Length ? remainingId.Substring(subIndex) : string.Empty;  
                    }
                }

                return component; 
            }

            return null;

        }

        private static List<Type> allComponentTypes;

        public static Type FindComponentType(string name)
        {

            if (allComponentTypes == null) allComponentTypes = typeof(Component).FindDerivedTypes();

            foreach (Type type in allComponentTypes) if (type.Name == name) return type;

            return null;

        }

        public Component FindAndBindComponent(string propertyId, out string remainingId)
        {
            remainingId = string.Empty;
            if (string.IsNullOrEmpty(propertyId)) return null;

            var component = GetComponentByProperty(propertyId, out remainingId);
            if (component != null) return component;
            
            string[] substrings = propertyId.Split('.');
            if (substrings.Length < 2) return null;

            string compString = propertyId;
            //int i = 0;
            Transform objTransform = null;
            if (substrings[0] == _animatorTransformPropertyStringPrefix)
            {
                objTransform = transform; // Default to animator transform
                //i = 1;
                compString = string.Join(".", substrings, 1, substrings.Length - 1);
            }
            else
            {
                // In case the transform name includes periods, try to find the transform
                //if (substrings.Length >= 4) // max of 4 periods allowed in transform name
                //{
                //    objTransform = FindTransformInHierarchy($"{substrings[0]}.{substrings[1]}.{substrings[2]}"/*, false*/); 
                //    i = 3;
                //}
                //if (objTransform == null)
                //{
                //    if (substrings.Length >= 3)
                //    {
                //        objTransform = FindTransformInHierarchy($"{substrings[0]}.{substrings[1]}"/*, false*/);
                //        i = 2;
                //    }
                //    if (objTransform == null)
                //    {
                //        objTransform = FindTransformInHierarchy(substrings[0]/*, false*/); 
                //        i = 1;
                //        if (objTransform == null)
                //        {
                //            return null;
                //        }
                //    }
                //}

                if (!AnimationUtils.TryExtractTransformFromPropertyString(propertyId, FindTransformInHierarchy, out objTransform, out compString)) return null;
            }

            //Type componentType = FindComponentType(substrings[i]);        
            string compName = compString;
            int firstDot = compName.IndexOf('.');
            if (firstDot >= 0) compName = compName.Substring(0, firstDot); else return null;
            Type componentType = FindComponentType(compName);
            if (componentType == null) return null;

            component = objTransform.GetComponent(componentType);
            if (component == null) return null;

            remainingId = compString.Substring(firstDot + 1);

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

            OnResetPose?.Invoke();

            foreach (var pair in m_transformStateReferences)
            {
                var state = pair.Value;
                state.Reset(pair.Key);
            }

        }

        protected void ResetTransformStates()
        {

            OnResetPose?.Invoke();

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

            OnResetPose?.Invoke();

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
            public NativeList<TransformAnimationState> transformStates;

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
            public NativeList<TransformAnimationState> transformStates;

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
            public NativeList<TransformAnimationState> transformStates;

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
        public int ParameterCount => m_parameters == null ? 0 : m_parameters.Count;
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

#if UNITY_EDITOR
            Debug.Log($"NEW PARAMETER {parameter.GetType().Name} : {idName}");
#endif

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
            if (parameterIndex < 0) return null;

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

        protected void OnCreateAnimationPlayer(IAnimationPlayer player)
        {
            if (player is CustomAnimation.Player customPlayer)
            {
                var rootMotionBone = RootMotionBone;
                if (rootMotionBone != null)
                {
                    customPlayer.SetupRootMotion(rootMotionBone.name, GetRootMotion, SetRootMotion);
                }
            }
        }

        [NonSerialized]
        protected List<IAnimationLayer> m_animationLayers;
        public int LayerCount => m_animationLayers == null ? 0 : m_animationLayers.Count;
        public IAnimationLayer GetLayer(int layerIndex) => layerIndex < 0 || layerIndex >= LayerCount ? null : m_animationLayers[layerIndex];
        public CustomAnimationLayer GetTypedLayer(int layerIndex)
        {
            var layer = GetLayer(layerIndex);
            if (layer is CustomAnimationLayer cal) return cal;

            return null;
        }
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

            if (layer is CustomAnimationLayer customLayer)
            {
                customLayer.ClearAnimationPlayerCreationListeners();
                customLayer.OnCreateAnimationPlayer += OnCreateAnimationPlayer;
                customLayer.IteratePlayers(OnCreateAnimationPlayer);
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
            var l = FindLayer(layerName);
            if (l is CustomAnimationLayer cal) return cal;

            return null;
        }
        public bool RemoveLayer(IAnimationLayer layer, bool dispose = true)
        {
            if (m_animationLayers == null || layer == null) return false;
            
            layer.IteratePlayers(CompleteAnimationPlayerJobs);
            if (m_animationLayers.Remove(layer))
            {
                if (dispose) layer.Dispose();
                RecalculateLayerIndices();
                return true;
            }
            return false;

        }
        public bool RemoveLayer(int layerIndex, bool dispose = true)
        {
            if (m_animationLayers == null || layerIndex < 0 || layerIndex >= m_animationLayers.Count) return false;
            return RemoveLayer(m_animationLayers[layerIndex], dispose);
        }
        public bool RemoveLayer(string layerName, bool dispose = true)
        {
            return RemoveLayer(FindLayer(layerName), dispose);
        }
        public int RemoveLayersStartingWith(string prefix, bool dispose = true)
        {
            if (m_animationLayers == null) return 0;
            
            foreach (var layer in m_animationLayers) if (layer != null) layer.IteratePlayers(CompleteAnimationPlayerJobs);           

            prefix = prefix.ToLower().Trim();  

            int i = 0;
            if (dispose)
            {
                i = m_animationLayers.RemoveAll(i => i == null || i.DisposeIfHasPrefix(prefix));
            } 
            else
            {
                i = m_animationLayers.RemoveAll(i => i == null || i.HasPrefix(prefix));
            }
            
            if (i > 0) RecalculateLayerIndices();

            return i;
        } 
        public int RemoveLayersFromSource(IAnimationController source, bool dispose = true)
        {
            if (m_animationLayers == null) return 0;

            foreach (var layer in m_animationLayers) if (layer != null) layer.IteratePlayers(CompleteAnimationPlayerJobs);

            int i = 0;
            if (dispose)
            {
                i = m_animationLayers.RemoveAll(i => i == null || i.DisposeIfIsFromSource(source));
            }
            else
            {
                i = m_animationLayers.RemoveAll(i => i == null || i.IsFromSource(source));
            }

            if (i > 0) RecalculateLayerIndices();

            return i;
        }
        public void ClearLayers(bool disposeLayers)
        {
            if (m_animationLayers == null) return;

            if (disposeLayers)
            {
                foreach (var layer in m_animationLayers) if (layer != null) layer.Dispose();
            }
            m_animationLayers.Clear();
        }

        private void RearrangeLayerInternal(int layerIndex, int swapIndex)
        {
            if (layerIndex == swapIndex || m_animationLayers == null || layerIndex < 0 || layerIndex >= m_animationLayers.Count) return;

            if (swapIndex >= 0 && swapIndex < m_animationLayers.Count)
            {
                var layer = m_animationLayers[layerIndex];
                var swap = m_animationLayers[swapIndex];
                m_animationLayers[swapIndex] = layer;
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
        private void MoveLayerInternal(int layerIndex, int newIndex)
        {
            if (layerIndex == newIndex || m_animationLayers == null || layerIndex < 0 || layerIndex >= m_animationLayers.Count) return;

            var layer = m_animationLayers[layerIndex];
            m_animationLayers.RemoveAt(layerIndex);
            if (newIndex < 0 && m_animationLayers.Count > 0) { m_animationLayers.Insert(0, layer); } else if (newIndex >= m_animationLayers.Count || m_animationLayers.Count <= 0) { m_animationLayers.Add(layer); } else { m_animationLayers.Insert(newIndex, layer); } 

        }
        public Dictionary<int, int> MoveLayer(int layerIndex, int newIndex, bool recalculateIndices = true)
        {
            MoveLayerInternal(layerIndex, newIndex);
            return recalculateIndices ? null : RecalculateLayerIndices();
        }
        public void MoveLayerNoRemap(int layerIndex, int newIndex, bool recalculateIndices = true)
        {
            MoveLayerInternal(layerIndex, newIndex);
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

            for (int a = 0; a < m_animationLayers.Count; a++)
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
            {
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

        }

        //

        [Serializable]
        public enum BehaviourEvent
        {
            OnEnable,
            OnDisable,
            OnPreUpdate,
            OnPostUpdate,
            OnPreLateUpdate,
            OnPostLateUpdate,
            OnResetPose
        }
        
        public void AddListener(BehaviourEvent event_, UnityAction listener) 
        {
            switch (event_)
            {
                case BehaviourEvent.OnEnable:
                    if (OnEnabled == null) OnEnabled = new UnityEvent();
                    OnEnabled.AddListener(listener);
                    break;
                case BehaviourEvent.OnDisable:
                    if (OnDisabled == null) OnDisabled = new UnityEvent();
                    OnDisabled.AddListener(listener);
                    break;
                case BehaviourEvent.OnPreUpdate:
                    if (OnPreUpdate == null) OnPreUpdate = new UnityEvent();
                    OnPreUpdate.AddListener(listener);
                    break;
                case BehaviourEvent.OnPostUpdate:
                    if (OnPostUpdate == null) OnPostUpdate = new UnityEvent();
                    OnPostUpdate.AddListener(listener);
                    break;
                case BehaviourEvent.OnPreLateUpdate:
                    if (OnPreLateUpdate == null) OnPreLateUpdate = new UnityEvent();
                    OnPreLateUpdate.AddListener(listener);
                    break;
                case BehaviourEvent.OnPostLateUpdate:
                    if (OnPostLateUpdate == null) OnPostLateUpdate = new UnityEvent();
                    OnPostLateUpdate.AddListener(listener);
                    break;
                case BehaviourEvent.OnResetPose:
                    if (OnResetPose == null) OnResetPose = new UnityEvent();
                    OnResetPose.AddListener(listener);
                    break;
            }
        }
        public void RemoveListener(BehaviourEvent event_, UnityAction listener)
        {
            switch (event_)
            {
                case BehaviourEvent.OnEnable:
                    if (OnEnabled != null) OnEnabled.RemoveListener(listener);
                    break;
                case BehaviourEvent.OnDisable:
                    if (OnDisabled != null) OnDisabled.RemoveListener(listener);
                    break;
                case BehaviourEvent.OnPreUpdate:
                    if (OnPreUpdate != null) OnPreUpdate.RemoveListener(listener);
                    break;
                case BehaviourEvent.OnPostUpdate:
                    if (OnPostUpdate != null) OnPostUpdate.RemoveListener(listener);
                    break;
                case BehaviourEvent.OnPreLateUpdate:
                    if (OnPreLateUpdate != null) OnPreLateUpdate.RemoveListener(listener);
                    break;
                case BehaviourEvent.OnPostLateUpdate:
                    if (OnPostLateUpdate != null) OnPostLateUpdate.RemoveListener(listener);
                    break;
                case BehaviourEvent.OnResetPose:
                    if (OnResetPose != null) OnResetPose.RemoveListener(listener);
                    break;
            }
        }
        public void ClearListeners()
        {
            if (OnEnabled != null) OnEnabled.RemoveAllListeners();
            if (OnDisabled != null) OnDisabled.RemoveAllListeners();
            if (OnPreUpdate != null) OnPreUpdate.RemoveAllListeners();
            if (OnPostUpdate != null) OnPostUpdate.RemoveAllListeners();
            if (OnPreLateUpdate != null) OnPreLateUpdate.RemoveAllListeners();
            if (OnPostLateUpdate != null) OnPostLateUpdate.RemoveAllListeners();
            if (OnResetPose != null) OnResetPose.RemoveAllListeners();
        }

        public UnityEvent OnEnabled;
        public UnityEvent OnDisabled;

        protected virtual void OnEnable()
        {
            disabledBySkip = false;
            OnEnabled?.Invoke();
        }
        protected virtual void OnDisable()
        {
            disabledBySkip = false;
            OnDisabled?.Invoke();
        }

        public void FinalizeAnimations() => FinalizeAnimations(applyRootMotion);
        public void FinalizeAnimations(bool resetRootMotionBone)
        {
            m_jobHandle.Complete(); // Required so that code which is dependent on animation completion can run properly
            if (resetRootMotionBone) ResetRootMotionBone(); 
        }

        public UnityEvent OnPreUpdate;
        public UnityEvent OnPostUpdate;

        public virtual void FixedUpdateStep(float fixedDeltaTime)
        {
            fixedDeltaTime += skipTimeFixed;
            skipTimeFixed = 0;

            if (applyRootMotion && rootMotionMode == RootMotionMode.Physics) ApplyRootMotion();
            if (finalizeAnimationsBeforePhysics) FinalizeAnimations();            
        }
        public virtual void UpdateStep(float deltaTime) => UpdateStep(deltaTime, true, false, applyRootMotion);
        public virtual void UpdateStep(float deltaTime, bool notifyListeners) => UpdateStep(deltaTime, notifyListeners, false, applyRootMotion);
        public virtual void UpdateStep(float deltaTime, bool notifyListeners, bool ignoreSkipTime) => UpdateStep(deltaTime, notifyListeners, ignoreSkipTime, applyRootMotion);
        public virtual void UpdateStep(float deltaTime, bool notifyListeners, bool ignoreSkipTime, bool applyRootMotion)
        {

            if (!ignoreSkipTime) deltaTime += skipTime;
            
            if (notifyListeners) OnPreUpdate?.Invoke();


            if (disableMultithreading) ResetTransformStates(); else ResetTransformStatesAsJob();
            ResetPropertyStates();

            UpdateAnimationLayers(deltaTime);

            if (disableMultithreading) ApplyTransformStates(); else if (forceFinalTransformUpdate) ApplyTransformStatesAsJob();
            ApplyPropertyStates();

            if (applyRootMotion && (rootMotionMode == RootMotionMode.Default || !this.applyRootMotion)) ApplyRootMotion();

            if (notifyListeners) OnPostUpdate?.Invoke();

        }

        public UnityEvent OnPreLateUpdate;
        public UnityEvent OnPostLateUpdate;

        public virtual void LateUpdateStep(float deltaTime)
        {

            deltaTime += skipTime;
            skipTime = 0;

            OnPreLateUpdate?.Invoke();

            FinalizeAnimations();

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

        public UnityEvent OnResetPose;

        protected readonly List<SwolePuppetMaster.MuscleConfigCollectionMix> muscleConfigMixes = new List<SwolePuppetMaster.MuscleConfigCollectionMix>();
        public List<SwolePuppetMaster.MuscleConfigCollectionMix> GetCurrentMuscleConfigMix() 
        {
            muscleConfigMixes.Clear();

            if (m_animationLayers != null)
            {
                float mixTotal = 0f;
                int minIndex = 0;
                for (int a = 0; a < m_animationLayers.Count; a++)
                {
                    var layer = m_animationLayers[a];
                    if (layer.IsActive && layer.Mix > 0f && layer.HasActiveState && layer is CustomAnimationLayer cal)
                    {
                        if (!layer.IsAdditive)
                        {
                            minIndex = a;
                            mixTotal = 0f;
                        }
                        
                        mixTotal += layer.Mix;
                    }
                }

                if (mixTotal > 0f)
                {
                    for (int a = 0; a < m_animationLayers.Count; a++)
                    {
                        if (a < minIndex) continue;

                        var layer = m_animationLayers[a];
                        if (layer.IsActive && layer.Mix > 0 && layer.HasActiveState && layer is CustomAnimationLayer cal)
                        {
                            muscleConfigMixes.Add(new SwolePuppetMaster.MuscleConfigCollectionMix()
                            {
                                mix = (mixTotal >= 1f ? (layer.Mix / mixTotal) : layer.Mix), 
                                collection = cal.GetCurrentMuscleConfigMix()
                            });
                        }
                    }

                    if (mixTotal < 1f) // If the total mix is less than 1, add a default config mix with the remaining amount
                    {
                        muscleConfigMixes.Add(new SwolePuppetMaster.MuscleConfigCollectionMix()
                        {
                            mix = 1f - mixTotal,
                            collection = default // this will automatically get replaced with the default configuration
                        });
                    }
                }
            }

            return muscleConfigMixes;
        }

    }
}

#endif