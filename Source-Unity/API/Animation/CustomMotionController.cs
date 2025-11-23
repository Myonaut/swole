#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;

using Swole.Animation;

using swolescr.andywiecko.BurstTriangulator;

namespace Swole.API.Unity.Animation
{

    [Serializable]
    public abstract class CustomMotionController : IAnimationMotionController
    {

        protected static void CloneBase(CustomMotionController reference, CustomMotionController clone)
        {

            clone.name = reference.name;
             
            clone.avatarMask = reference.avatarMask;
            clone.invertAvatarMask = reference.invertAvatarMask;

            clone.baseSpeed = reference.baseSpeed;
            clone.speedMultiplierParameter = reference.speedMultiplierParameter;

        }

        public virtual void Reset(IAnimationLayer layer)
        {
        }

        public string name;
        public string Name
        {
            get => name;
            set => name = value;
        }
        public string SerializedName => Name;

        [NonSerialized]
        protected IAnimationMotionController m_parent;
        public IAnimationMotionController Parent => m_parent;
        public bool HasParent => m_parent != null;

        [NonSerialized]
        protected IAnimationLayer m_layer;
        public IAnimationLayer Layer => m_layer;
        public bool IsInitialized => m_layer != null;

        [SerializeField]
        protected WeightedAvatarMask avatarMask;
        public WeightedAvatarMask AvatarMask
        {
            get => avatarMask;
            set => SetAvatarMask(value, invertAvatarMask);
        }
        public virtual void SetAvatarMask(WeightedAvatarMask mask, bool invertMask)
        {
            avatarMask = mask;
            invertAvatarMask = invertMask;

            if (IsInitialized) Layer.ReinitializeController(this); 
        } 

        [SerializeField]
        protected bool invertAvatarMask;
        public bool InvertAvatarMask
        {
            get => invertAvatarMask;
            set => SetAvatarMask(avatarMask, value);
        }

        private static readonly List<IAnimationMotionController> tempControllers = new List<IAnimationMotionController>();
        public virtual void Initialize(IAnimationLayer layer, List<AvatarMaskUsage> masks, IAnimationMotionController parent = null)
        {
            
            this.m_layer = layer;

            tempControllers.Clear();
            bool isCyclic = false;
            var tempParent = parent;
            while (tempParent != null) // make sure the parent chain is not cyclic
            {
                if (tempControllers.Contains(tempParent))
                {
                    isCyclic = true;
                    break;
                }
                tempControllers.Add(tempParent);
                tempParent = tempParent.Parent;   
            }
            tempControllers.Clear();

            if (isCyclic) 
            {
#if UNITY_EDITOR
                Debug.LogError($"Motion Controller '{name}' was initialized with a cyclic parent '{parent.Name}'");
#else
                swole.LogError($"Motion Controller '{name}' was initialized with a cyclic parent '{parent.Name}'");
#endif
                m_parent = null; 
            } 
            else 
            { 
                m_parent = parent;
            }

            m_weight = 1;

            if (avatarMask != null) 
            {
                if (masks == null) masks = new List<AvatarMaskUsage>();
                masks.Add(new AvatarMaskUsage() { mask = avatarMask.AsComposite(true), invertMask = invertAvatarMask });
            }

        }

        public float baseSpeed = 1f;
        public float BaseSpeed
        {
            get => baseSpeed;
            set => baseSpeed = value;
        }

        [SerializeField]
        public int speedMultiplierParameter = -1;
        public int SpeedMultiplierParameter 
        {
            get => speedMultiplierParameter;
            set => speedMultiplierParameter = value;
        }

        public float GetSpeed(IAnimationLayer layer)
        {

            float speed = baseSpeed;

            if (Parent != null) speed = speed * Parent.GetSpeed(layer);

            if (layer == null) return speed;

            //GetBiasedParameterValue(layer, speedMultiplierParameter, 1f, false, out _); // causes stack overflow
            if (speedMultiplierParameter >= 0)
            {
                var p = layer.GetParameter(speedMultiplierParameter); 
                if (p != null) speed = speed * p.Value;  
            }

            return speed;

        }

        private static readonly List<int> tempParameterIndices = new List<int>();
        public virtual HashSet<int> GetActiveParameters(IAnimationLayer layer, HashSet<int> parameterIndices = null)
        {
            if (parameterIndices == null) parameterIndices = new HashSet<int>();

            tempParameterIndices.Clear();
            GetParameterIndices(layer, tempParameterIndices);
            foreach (var index in tempParameterIndices) parameterIndices.Add(index);

            return parameterIndices;
        }

        public virtual AnimationLoopMode GetLoopMode(IAnimationLayer layer) => AnimationLoopMode.Loop;

        public virtual void ForceSetLoopMode(IAnimationLayer layer, AnimationLoopMode loopMode) { }

        [NonSerialized]
        protected float m_weight;

        public virtual void SetWeight(float weight)
        {
            m_weight = weight; 
        }

        public virtual float GetWeight()
        {
            float weight = m_weight;

            if (Parent != null) weight = weight * Parent.GetWeight(); 

            return weight;
        }
        public virtual float GetBaseWeight() => m_weight;
        public virtual void SyncWeight(IAnimationLayer layer) 
        {
            //Debug.Log($"Synced weight for {name}");     
        }

        public abstract float GetDuration(IAnimationLayer layer);

        /// <summary>
        /// Get time length of controller scaled by playback speed
        /// </summary>
        public abstract float GetScaledDuration(IAnimationLayer layer);
        public virtual float GetMaxDuration(IAnimationLayer layer) => GetDuration(layer);
        public virtual float GetMaxScaledDuration(IAnimationLayer layer) => GetScaledDuration(layer);

        public abstract float GetTime(IAnimationLayer layer, float addTime = 0);

        public virtual float GetNormalizedTime(IAnimationLayer layer, float addTime = 0)
        {
            float length = GetDuration(layer);
            if (length <= 0f) return 0f;

            return GetTime(layer, addTime) / length;
        }

        public abstract void SetTime(IAnimationLayer layer, float time, bool resetFlags = true);

        public abstract void SetNormalizedTime(IAnimationLayer layer, float normalizedTime, bool resetFlags = true);

        public abstract void Progress(IAnimationLayer layer, float deltaTime, ref JobHandle jobHandle, bool useMultithreading = true, bool isFinal = false, bool canLoop = true);

        public abstract bool HasAnimationPlayer(IAnimationLayer layer);

        public abstract object Clone();

        public virtual bool HasChildControllers => false;

        public virtual void GetChildIndices(List<int> indices, bool onlyAddIfNotPresent = true) { }

        public virtual void RemapChildIndices(Dictionary<int, int> remapper, bool invalidateNonRemappedIndices = false) { }

        public virtual void RemapChildIndices(Dictionary<int, int> remapper, int minIndex, bool invalidateNonRemappedIndices = false) { }

        public virtual int GetChildCount() => 0;
        public virtual int GetChildIndex(int childIndex) => -1;
        public virtual void SetChildIndex(int childIndex, int controllerIndex) { }

        public virtual void GetParameterIndices(IAnimationLayer layer, List<int> indices)
        {

            if (indices == null) return;

            if (SpeedMultiplierParameter >= 0) indices.Add(SpeedMultiplierParameter);

        }

        public virtual void RemapParameterIndices(IAnimationLayer layer, Dictionary<int, int> remapper, bool invalidateNonRemappedIndices = false)
        {

            if (remapper == null) return;

            if (SpeedMultiplierParameter >= 0)
            {

                int origIndex = SpeedMultiplierParameter;
                if (!remapper.TryGetValue(origIndex, out speedMultiplierParameter) && invalidateNonRemappedIndices) 
                { 
                    speedMultiplierParameter = -1;

#if UNITY_EDITOR
                    Debug.LogError($"Failed to remap speed multiplier parameter index {origIndex} for {GetType().Name} '{name}'");
#else
                    swole.LogError($"Failed to remap speed multiplier parameter index {origIndex} for {GetType().Name} '{name}'");
#endif
                }

            }

        }

        public float GetBiasedParameterValue(IAnimationLayer layer, int parameterIndex, float defaultValue, bool updateParameter) => GetBiasedParameterValue(layer, parameterIndex, defaultValue, updateParameter, out _);
        public virtual float GetBiasedParameterValue(IAnimationLayer layer, int parameterIndex, float defaultValue, bool updateParameter, out bool appliedBias) 
        {
            appliedBias = false;

            var p = layer.GetParameter(parameterIndex);
            if (p == null) return defaultValue;

            return updateParameter ? p.UpdateAndGetValue() : p.Value;
        }

        public virtual bool HasDerivativeHierarchyOf(IAnimationLayer layer, IAnimationMotionController other) => true;

        public virtual int GetLongestHierarchyIndex(IAnimationLayer layer) => -1;

    }

    [Serializable]
    public class AnimationReference : CustomMotionController, IAnimationReference
    {

        #region Serialization

        [Serializable]
        public struct Serialized : ISerializableContainer<AnimationReference, AnimationReference.Serialized>
        {

            public string name;
            public string SerializedName => name;

            public string avatarMaskPath;
            public WeightedAvatarMask embeddedAvatarMask;
            public bool invertAvatarMask;

            public float baseSpeed;
            public int speedMultiplierParameter;
            public int timeOverrideParameter;

            public PackageIdentifier packageId;
            public string animationId;
            public int loopMode;

            public string AsJSON(bool prettyPrint = false) => swole.Engine.ToJson(this, prettyPrint);

            public object AsNonserializableObject(PackageInfo packageInfo = default) => AsOriginalType(packageInfo);
            public AnimationReference AsOriginalType(PackageInfo packageInfo = default)
            {
                var animAsset = AnimationLibrary.FindAnimation(packageId, animationId);
                if (animAsset == null) animAsset = AnimationLibrary.FindAnimation(animationId);
                var anim = new AnimationReference(name, animAsset, (AnimationLoopMode)loopMode);

                if (!string.IsNullOrWhiteSpace(avatarMaskPath))
                {
                    anim.avatarMask = AnimationLibrary.FindAvatarMask(packageInfo, avatarMaskPath);
                    if (anim.avatarMask == null) anim.avatarMask = AnimationLibrary.FindAvatarMask(avatarMaskPath);
                }
                else
                {
                    anim.avatarMask = embeddedAvatarMask;
                }
                anim.invertAvatarMask = invertAvatarMask;

                anim.baseSpeed = baseSpeed;
                anim.speedMultiplierParameter = speedMultiplierParameter;
                anim.timeOverrideParameter = timeOverrideParameter;

                return anim;
            }

            public static implicit operator Serialized(AnimationReference inst)
            {
                Serialized s = new Serialized();

                s.name = inst.name;

                if (inst.AvatarMask != null)
                {
                    if (inst.AvatarMask.PackageInfo.NameIsValid)
                    {
                        s.avatarMaskPath = inst.AvatarMask.PackageInfo.ConvertToAssetPath(inst.avatarMask.Name);
                    }
                    else
                    {
                        s.embeddedAvatarMask = inst.AvatarMask;
                    }
                }
                s.invertAvatarMask = inst.invertAvatarMask;

                s.baseSpeed = inst.baseSpeed;
                s.speedMultiplierParameter = inst.speedMultiplierParameter;
                s.timeOverrideParameter = inst.timeOverrideParameter;

                if (inst.Animation != null)
                {
                    s.packageId = inst.Animation.PackageInfo.GetIdentity();
                    s.animationId = inst.Animation.Name;
                }
                s.loopMode = (int)inst.loopMode;

                return s;
            }

        }

        #endregion

        public AnimationReference() { }

        public AnimationReference(string name, IAnimationAsset animation, AnimationLoopMode loopMode)
        {
            this.name = name;
            if (animation is CustomAnimationAsset asset) this.animationAsset = asset; else this.animation = animation;
            this.loopMode = loopMode;
        }

        public override object Clone() => Duplicate();
        public AnimationReference Duplicate()
        {

            var clone = new AnimationReference();

            CustomMotionController.CloneBase(this, clone);

            clone.loopMode = loopMode;
            clone.animation = animation;
            clone.animationAsset = animationAsset;
            clone.timeOverrideParameter = timeOverrideParameter;

            return clone;

        }

        [SerializeField]
        public AnimationLoopMode loopMode;
        public AnimationLoopMode LoopMode
        {
            get => loopMode;
            set => loopMode = value;
        }
        public override AnimationLoopMode GetLoopMode(IAnimationLayer layer) => loopMode;

        public override void ForceSetLoopMode(IAnimationLayer layer, AnimationLoopMode loopMode)
        {

            if (AnimationPlayer != null)
            {

                AnimationPlayer.LoopMode = loopMode;
                AnimationPlayer.ResetLoop();

            }

        }

        [NonSerialized]
        protected IAnimationAsset animation;
        public IAnimationAsset Animation
        {
            get
            {
                if (IsUsingAsset) return AnimationAsset.Animation;
                return animation;
            }
            set
            {
                if (value is CustomAnimationAsset asset)
                {
                    animationAsset = asset;
                }
                else
                {
                    animation = value;
                }
            }
        }

        [SerializeField]
        public int timeOverrideParameter = -1;
        public int TimeOverrideParameter
        {
            get => timeOverrideParameter;
            set => timeOverrideParameter = value;
        }

        public override void RemapParameterIndices(IAnimationLayer layer, Dictionary<int, int> remapper, bool invalidateNonRemappedIndices = false)
        {

            base.RemapParameterIndices(layer, remapper, invalidateNonRemappedIndices);

            if (remapper == null) return;

            if (TimeOverrideParameter >= 0)
            {

                int origIndex = TimeOverrideParameter;
                if (!remapper.TryGetValue(origIndex, out timeOverrideParameter) && invalidateNonRemappedIndices) 
                {
                    timeOverrideParameter = -1;

#if UNITY_EDITOR
                    Debug.LogError($"Failed to remap time override parameter index {origIndex} for {GetType().Name} '{name}'");
#else
                    swole.LogError($"Failed to remap time override parameter  index {origIndex} for {GetType().Name} '{name}'");
#endif
                }

            }

        }

        [SerializeField]
        public CustomAnimationAsset animationAsset;
        public CustomAnimationAsset AnimationAsset => animationAsset;
        public bool IsUsingAsset => AnimationAsset != null;

        [NonSerialized]
        protected IAnimationPlayer m_animationPlayer;
        public IAnimationPlayer AnimationPlayer => m_animationPlayer;
        public CustomAnimation.Player AnimationPlayerTyped
        {
            get
            {
                if (m_animationPlayer is CustomAnimation.Player player) return player;
                return null;
            }
        }

        public override bool HasDerivativeHierarchyOf(IAnimationLayer layer, IAnimationMotionController other)
        {

            if (layer == null || other == null) return false;

            if (other is AnimationReference animRef)
            {

                return AnimationPlayer == null ? false : animRef.AnimationPlayer == null ? false : AnimationPlayer.HasDerivativeHierarchyOf(animRef.AnimationPlayer);

            }
            else if (other is BlendTree blendTree)
            {

                var blendTreeChildren = blendTree.GetChildControllerIndices();
                if (blendTreeChildren == null || blendTreeChildren.Length == 0) return false;

                for (int a = 0; a < blendTreeChildren.Length; a++)
                {

                    var child = layer.GetMotionController(blendTreeChildren[a]);
                    if (child == null) continue;

                    if (!HasDerivativeHierarchyOf(layer, child)) return false;

                }

                return true;

            }

            return false;

        }

        public override int GetLongestHierarchyIndex(IAnimationLayer layer)
        {

            if (AnimationPlayer == null || AnimationPlayer.Hierarchy == null) return -1;

            return AnimationPlayer.Hierarchy.Index;

        }

        public override void Initialize(IAnimationLayer layer, List<AvatarMaskUsage> masks, IAnimationMotionController parent = null)
        {

            bool combineMasks = masks != null && masks.Count > 0;
            base.Initialize(layer, masks, parent); 

            m_animationPlayer = IsUsingAsset ? layer.GetNewAnimationPlayer(AnimationAsset) : layer.GetNewAnimationPlayer(Animation);
            if (m_animationPlayer != null)
            {
                m_animationPlayer.Paused = false;
                m_animationPlayer.LoopMode = loopMode;
                m_animationPlayer.OverrideTime = false;

                if (combineMasks)
                {
                    var mask = AvatarMaskUsage.GetCombinedMaskMultiplicative(masks, m_animationPlayer.GetInvertedMask);
                    m_animationPlayer.SetTopAvatarMask(mask, false);
                }
                else if (avatarMask != null)
                {
                    m_animationPlayer.SetTopAvatarMask(avatarMask, invertAvatarMask);
                }

                m_animationPlayer.Mix = GetWeight();
            }
        }

        public override void Reset(IAnimationLayer layer)
        {
            if (AnimationPlayer == null) return;  
            AnimationPlayer.Time = 0f; 
        }

        public override void SetWeight(float weight)
        {

            base.SetWeight(weight);

            if (m_animationPlayer == null) return;

            m_animationPlayer.Mix = GetWeight();

        }

        public override void SyncWeight(IAnimationLayer layer)
        {

            base.SyncWeight(layer);

            if (m_animationPlayer == null) return;

            m_animationPlayer.Mix = GetWeight();    
        }

        public override float GetDuration(IAnimationLayer layer)
        {
            if (AnimationPlayer == null) return 0f;
            return AnimationPlayer.LengthInSeconds;
        }

        /// <summary>
        /// Get length of controller scaled by playback speed
        /// </summary>
        public override float GetScaledDuration(IAnimationLayer layer)
        {

            if (AnimationPlayer == null) return 0f;

            float speed = BaseSpeed;//GetSpeed(layer); // are u insane
            return speed == 0f ? 0f : (AnimationPlayer.LengthInSeconds / math.abs(speed)); 

        }

        public override float GetTime(IAnimationLayer layer, float addTime = 0)
        {

            if (AnimationPlayer == null) return 0;

            var apTyped = AnimationPlayerTyped;
            return AnimationPlayer.Time + addTime * GetSpeed(layer) * (apTyped == null ? 1f : apTyped.InternalSpeedMultiplier);  

        }

        public override void SetTime(IAnimationLayer layer, float time, bool resetFlags = true)
        {

            if (AnimationPlayer == null) return;

            AnimationPlayer.SetTime(time, resetFlags);

        }

        public override void SetNormalizedTime(IAnimationLayer layer, float normalizedTime, bool resetFlags = true)
        {

            if (AnimationPlayer == null) return;

            AnimationPlayer.SetTime(AnimationPlayer.LengthInSeconds * normalizedTime, resetFlags);

        }

        public override void Progress(IAnimationLayer layer, float deltaTime, ref JobHandle jobHandle, bool useMultithreading = true, bool isFinal = false, bool canLoop = true)
        {

            if (AnimationPlayer is not CustomAnimation.Player cap) return;

            cap.Speed = GetSpeed(layer);
            //cap.Mix = GetWeight(); // shouldn't be necessary

            if (timeOverrideParameter >= 0)
            {
                cap.OverrideTime = true;

                var p = layer.GetParameter(timeOverrideParameter);
                if (p != null)
                {
                    cap.TimeOverride = p.UpdateAndGetValue();
                }
            }

            jobHandle = cap.Progress(deltaTime, layer.Mix, jobHandle, useMultithreading, isFinal, canLoop); 

        }

        public override bool HasAnimationPlayer(IAnimationLayer layer)
        {

            return AnimationPlayer != null;

        }

    }

    [Serializable]
    public class MotionComposite : CustomMotionController, IMotionComposite 
    {

        #region Serialization

        [Serializable]
        public struct Serialized : ISerializableContainer<MotionComposite, MotionComposite.Serialized>
        {

            public string name;
            public string SerializedName => name;

            public string avatarMaskPath;
            public WeightedAvatarMask embeddedAvatarMask;
            public bool invertAvatarMask;

            public float baseSpeed;
            public int speedMultiplierParameter;
            public bool normalizeDurations;
            public float normalizedDuration;

            public bool waitForLoops;
            public bool forceSyncOnProgress;

            public bool useDedicatedChildForTime;
            public int timeDedicatedChildIndex;

            public MotionPart[] motionParts;

            public string AsJSON(bool prettyPrint = false) => swole.Engine.ToJson(this, prettyPrint);

            public object AsNonserializableObject(PackageInfo packageInfo = default) => AsOriginalType(packageInfo);
            public MotionComposite AsOriginalType(PackageInfo packageInfo = default)
            {
                var anim = new MotionComposite(name, motionParts);

                if (!string.IsNullOrWhiteSpace(avatarMaskPath))
                {
                    anim.avatarMask = AnimationLibrary.FindAvatarMask(packageInfo, avatarMaskPath);
                    if (anim.avatarMask == null) anim.avatarMask = AnimationLibrary.FindAvatarMask(avatarMaskPath);
                }
                else
                {
                    anim.avatarMask = embeddedAvatarMask;
                }
                anim.invertAvatarMask = invertAvatarMask;

                anim.baseSpeed = baseSpeed;
                anim.speedMultiplierParameter = speedMultiplierParameter;
                anim.normalizeDurations = normalizeDurations; 
                anim.normalizedDuration = normalizedDuration;

                anim.waitForLoops = waitForLoops;
                anim.forceSyncOnProgress = forceSyncOnProgress;

                anim.useDedicatedChildForTime = useDedicatedChildForTime;
                anim.timeDedicatedChildIndex = timeDedicatedChildIndex;

                return anim;
            }

            public static implicit operator Serialized(MotionComposite inst)
            {
                Serialized s = new Serialized();

                s.name = inst.name;

                if (inst.AvatarMask != null)
                {
                    if (inst.AvatarMask.PackageInfo.NameIsValid)
                    {
                        s.avatarMaskPath = inst.AvatarMask.PackageInfo.ConvertToAssetPath(inst.avatarMask.Name);
                    } 
                    else
                    {
                        s.embeddedAvatarMask = inst.AvatarMask;
                    }
                }
                s.invertAvatarMask = inst.invertAvatarMask;

                s.baseSpeed = inst.baseSpeed;
                s.speedMultiplierParameter = inst.speedMultiplierParameter;
                s.normalizeDurations = inst.normalizeDurations;
                s.normalizedDuration = inst.normalizedDuration;

                s.motionParts = inst.motionParts;

                s.waitForLoops = inst.waitForLoops;
                s.forceSyncOnProgress = inst.forceSyncOnProgress;

                s.useDedicatedChildForTime = inst.useDedicatedChildForTime;
                s.timeDedicatedChildIndex = inst.timeDedicatedChildIndex;

                return s;
            }

        }

        #endregion

        public MotionComposite() { }

        public MotionComposite(string name, MotionPart[] motionParts)
        {
            this.name = name;
            this.motionParts = motionParts;
        }

        public override object Clone() => Duplicate();
        public MotionComposite Duplicate()
        {

            var clone = new MotionComposite();

            CustomMotionController.CloneBase(this, clone);

            clone.motionParts = motionParts == null ? null : (MotionPart[])motionParts.DeepClone();

            clone.normalizeDurations = normalizeDurations;
            clone.normalizedDuration = normalizedDuration;
            clone.waitForLoops = waitForLoops;
            clone.forceSyncOnProgress = forceSyncOnProgress;

            clone.useDedicatedChildForTime = useDedicatedChildForTime;
            clone.timeDedicatedChildIndex = timeDedicatedChildIndex;

            return clone;

        }

        public bool normalizeDurations;
        public float normalizedDuration;

        [SerializeField]
        public bool waitForLoops;
        public bool WaitForLoops
        {
            get => waitForLoops;
            set => waitForLoops = value;
        }

        [SerializeField]
        public bool forceSyncOnProgress;
        public bool ForceSyncOnProgress
        {
            get => forceSyncOnProgress;
            set => forceSyncOnProgress = value;
        }

        [SerializeField]
        public bool useDedicatedChildForTime;
        public bool UseDedicatedChildForTime 
        {
            get => useDedicatedChildForTime;
            set => useDedicatedChildForTime = value;
        }
        [SerializeField]
        public int timeDedicatedChildIndex;
        public int TimeDedicatedChildIndex 
        {
            get => timeDedicatedChildIndex;
            set => timeDedicatedChildIndex = value;
        }

        public float GetDedicatedChildDuration(IAnimationLayer layer)
        {
            if (!UseDedicatedChildForTime || TimeDedicatedChildIndex < 0) return 0f;

            var motionField = BaseMotionParts[TimeDedicatedChildIndex];
            var controller = layer.GetMotionController(motionField.ControllerIndex);
            if (controller == null) return 0f;

            return controller.GetDuration(layer);
        }

        public override void Initialize(IAnimationLayer layer, List<AvatarMaskUsage> masks, IAnimationMotionController parent = null)
        {

            base.Initialize(layer, masks, parent);

            if (MotionParts != null)
            {

                for (int a = 0; a < MotionParts.Length; a++)
                {

                    var part = MotionParts[a];
                    var controller = layer.GetMotionController(MotionParts[a].ControllerIndex);

                    if (controller != null)
                    {

                        controller.Initialize(layer, masks, this);

                        float speed = 1f;
                        if (normalizeDurations)
                        {
                            if (normalizedDuration <= 0f)
                            {
                                speed = 0f;
                            }
                            else
                            {
                                float duration = controller.GetDuration(layer);
                                speed = duration / normalizedDuration;
                            }
                        }

                        controller.BaseSpeed = speed * part.Speed;

                        controller.SetNormalizedTime(layer, part.NormalizedStartTime);
                    }

                }

            }

        }

        public override void Reset(IAnimationLayer layer)
        {
            base.Reset(layer);

            if (BaseMotionParts != null)
            {
                for (int a = 0; a < BaseMotionParts.Length; a++)
                {
                    var part = BaseMotionParts[a];
                    var controller = layer.GetMotionController(part.ControllerIndex);
                    if (controller == null) continue;

                    controller.Reset(layer);
                    controller.SetNormalizedTime(layer, part.NormalizedStartTime);
                }
            }
        } 

        public override bool HasChildControllers => true;

        [NonSerialized]
        protected int[] childIndices;
        public int[] GetChildControllerIndices()
        {
            if (childIndices == null)
            {
                childIndices = new int[MotionParts == null ? 0 : MotionParts.Length];
                for (int a = 0; a < childIndices.Length; a++) childIndices[a] = MotionParts[a].ControllerIndex;
            }

            return childIndices;
        }

        public override void GetChildIndices(List<int> indices, bool onlyAddIfNotPresent)
        {

            if (motionParts == null || indices == null) return;

            if (onlyAddIfNotPresent)
            {

                foreach (var part in motionParts) if (!indices.Contains(part.controllerIndex)) indices.Add(part.controllerIndex);

            }
            else
            {

                foreach (var part in motionParts) indices.Add(part.controllerIndex);

            }

        }

        public override void RemapChildIndices(Dictionary<int, int> remapper, bool invalidateNonRemappedIndices = false) => RemapChildIndices(remapper, 0, invalidateNonRemappedIndices);
        public override void RemapChildIndices(Dictionary<int, int> remapper, int minIndex, bool invalidateNonRemappedIndices = false)
        {

            if (motionParts == null || remapper == null) return;
            motionParts = (MotionPart[])motionParts.Clone();

            for (int a = minIndex; a < motionParts.Length; a++)
            {

                var part = motionParts[a].Duplicate();

                if (remapper.TryGetValue(part.controllerIndex, out int newIndex))
                {
                    part.controllerIndex = newIndex;
                }
                else if (invalidateNonRemappedIndices)
                {
                    part.controllerIndex = -1;
#if UNITY_EDITOR
                    Debug.LogError($"Failed to remap motion part [{a}] controller index for {GetType().Name} '{name}'");
#else
                    swole.LogError($"Failed to remap motion part [{a}] controller index for {GetType().Name} '{name}'");
#endif
                }

                motionParts[a] = part;

            }

        }

        public override int GetChildCount() => motionParts == null ? 0 : motionParts.Length;
        public override int GetChildIndex(int childIndex) => motionParts == null ? -1 : motionParts[childIndex].controllerIndex;
        public override void SetChildIndex(int childIndex, int controllerIndex) 
        { 
            if (motionParts == null) return;

            motionParts[childIndex].controllerIndex = controllerIndex;
        }

        public int ParameterCount => 0;

        public int GetParameterIndex(int localParameterIndex) => -1;

        public override void RemapParameterIndices(IAnimationLayer layer, Dictionary<int, int> remapper, bool invalidateNonRemappedIndices = false)
        {

            if (remapper == null) return;

            base.RemapParameterIndices(layer, remapper, invalidateNonRemappedIndices);

            if (motionParts != null)
            {

                for (int a = 0; a < motionParts.Length; a++)
                {
                    var part = motionParts[a];

                    if (part.MixParameterIndex >= 0)
                    {

                        int origIndex = part.MixParameterIndex;
                        if (!remapper.TryGetValue(origIndex, out part.mixParameterIndex) && invalidateNonRemappedIndices)
                        {
                            part.mixParameterIndex = -1;

#if UNITY_EDITOR
                            Debug.LogError($"Failed to remap mix parameter index {origIndex} for {GetType().Name} '{name}', (Part:{a})");
#else
                            swole.LogError($"Failed to remap mix parameter index {origIndex} for {GetType().Name} '{name}', (Part:{a})");
#endif
                        }

                    }



                    var controller = layer.GetMotionController(part.controllerIndex);
                    if (controller == null) continue;

                    controller.RemapParameterIndices(layer, remapper, invalidateNonRemappedIndices);

                }

            }

        }
         
        public override float GetBiasedParameterValue(IAnimationLayer layer, int parameterIndex, float defaultValue, bool updateParameter, out bool appliedBias)
        {
            appliedBias = false;

            if (BaseMotionParts != null)
            {
                for (int a = 0; a < BaseMotionParts.Length; a++)
                {
                    var part = BaseMotionParts[a];
                    var controller = layer.GetMotionController(part.ControllerIndex);
                    if (controller == null) continue;

                    float val = controller.GetBiasedParameterValue(layer, parameterIndex, defaultValue, updateParameter, out appliedBias);
                    if (appliedBias) return val;
                }
            }

            return base.GetBiasedParameterValue(layer, parameterIndex, defaultValue, updateParameter, out appliedBias); 
        }

        [SerializeField]
        public MotionPart[] motionParts;
        public MotionPart[] MotionParts
        {
            get => motionParts;
            set => motionParts = value;
        }
        public IMotionPart[] BaseMotionParts
        {
            get => motionParts;
            set
            {
                if (value == null)
                {
                    motionParts = null;
                    return;
                }
                motionParts = new MotionPart[value.Length];
                for (int a = 0; a < value.Length; a++)
                {
                    var mf = value[a];
                    if (mf is MotionPart mp)
                    {
                        motionParts[a] = mp;
                    }
                    else
                    {
                        motionParts[a] = new MotionPart() { speed = mf.Speed, controllerIndex = mf.ControllerIndex, syncMode = mf.SyncMode, localSyncReferenceIndex = mf.LocalSyncReferenceIndex, mix = mf.Mix, mixParameterIndex = mf.MixParameterIndex };
                    }
                }
            }
        }

        public override bool HasDerivativeHierarchyOf(IAnimationLayer layer, IAnimationMotionController other)
        {

            if (layer == null || other == null) return false;

            var children = GetChildControllerIndices();
            if (children == null || children.Length == 0) return true;

            for (int a = 0; a < children.Length; a++)
            {

                var child = layer.GetMotionController(children[a]);
                if (child == null) continue;

                if (!child.HasDerivativeHierarchyOf(layer, other)) return false;

            }

            return true;

        }

        public override int GetLongestHierarchyIndex(IAnimationLayer layer)
        {

            if (layer == null) return -1;

            var children = GetChildControllerIndices();
            if (children == null || children.Length == 0) return -1;

            int longestIndex = -1;
            int longestCount = int.MinValue;
            for (int a = 0; a < children.Length; a++)
            {

                var child = layer.GetMotionController(children[a]);
                if (child == null) continue;

                int hierarchyIndex = child.GetLongestHierarchyIndex(layer);
                if (hierarchyIndex < 0) continue;

                var hierarchy = layer.Animator.GetTransformHierarchy(hierarchyIndex);
                if (hierarchy == null) continue;

                int count = hierarchy.Count;

                if (longestIndex < 0 || count > longestCount)
                {

                    longestIndex = hierarchyIndex;
                    longestCount = count;

                }

            }

            return longestIndex;

        }

        public override AnimationLoopMode GetLoopMode(IAnimationLayer layer)
        {

            AnimationLoopMode loopMode = AnimationLoopMode.Loop;

            if (MotionParts != null)
            {

                foreach (var part in MotionParts)
                {

                    var controller = layer.GetMotionController(part.ControllerIndex);
                    if (controller == null) continue;

                    loopMode = controller.GetLoopMode(layer);
                    break;

                }

            }

            return loopMode;

        }

        public override void ForceSetLoopMode(IAnimationLayer layer, AnimationLoopMode loopMode)
        {

            if (MotionParts != null)
            {

                foreach (var field in MotionParts)
                {

                    var controller = layer.GetMotionController(field.ControllerIndex);
                    if (controller == null) continue;

                    controller.ForceSetLoopMode(layer, loopMode);

                }

            }

        }

        public override float GetDuration(IAnimationLayer layer) 
        {
            if (UseDedicatedChildForTime)
            {
                if (MotionParts == null || TimeDedicatedChildIndex < 0 || TimeDedicatedChildIndex >= MotionParts.Length) return 0f;

                var dedicatedChild = MotionParts[TimeDedicatedChildIndex];
                var controller = layer.GetMotionController(dedicatedChild.ControllerIndex);
                if (controller == null) return 0f;

                return controller.GetDuration(layer);
            }

            return GetScaledDuration(layer);
        }

        /// <summary>
        /// Get length of controller scaled by playback speed
        /// </summary>
        public override float GetScaledDuration(IAnimationLayer layer)
        {

            if (MotionParts == null) return 0;

            if (UseDedicatedChildForTime)
            {
                if (TimeDedicatedChildIndex < 0 || TimeDedicatedChildIndex >= MotionParts.Length) return 0f;

                var dedicatedChild = MotionParts[TimeDedicatedChildIndex];
                var controller = layer.GetMotionController(dedicatedChild.ControllerIndex);
                if (controller == null) return 0f;

                return controller.GetScaledDuration(layer);
            }

            float length = 0;

            for (int a = 0; a < MotionParts.Length; a++)
            {

                var motionField = MotionParts[a];
                var controller = layer.GetMotionController(motionField.ControllerIndex);
                if (controller == null) continue;

                length = math.max(length, controller.GetScaledDuration(layer)); // always uses scaled length because children can have varied playback speeds

            }

            return length;

        }

        public override float GetMaxDuration(IAnimationLayer layer)
        {

            if (BaseMotionParts == null) return 0f;

            if (UseDedicatedChildForTime)
            {
                if (TimeDedicatedChildIndex < 0 || TimeDedicatedChildIndex >= MotionParts.Length) return 0f;

                var dedicatedChild = MotionParts[TimeDedicatedChildIndex];
                var controller = layer.GetMotionController(dedicatedChild.ControllerIndex);
                if (controller == null) return 0f;

                return controller.GetMaxDuration(layer);
            }

            float length = 0f;
            for (int a = 0; a < BaseMotionParts.Length; a++)
            {

                var motionField = BaseMotionParts[a];
                var controller = layer.GetMotionController(motionField.ControllerIndex);
                if (controller == null) continue;

                length = math.max(length, controller.GetMaxDuration(layer));

            }

            return length;

        }
        public override float GetMaxScaledDuration(IAnimationLayer layer)
        {

            if (BaseMotionParts == null) return 0f;

            if (UseDedicatedChildForTime)
            {
                if (TimeDedicatedChildIndex < 0 || TimeDedicatedChildIndex >= MotionParts.Length) return 0f;

                var dedicatedChild = MotionParts[TimeDedicatedChildIndex];
                var controller = layer.GetMotionController(dedicatedChild.ControllerIndex);
                if (controller == null) return 0f;

                return controller.GetMaxScaledDuration(layer);
            }

            float length = 0f;
            for (int a = 0; a < BaseMotionParts.Length; a++)
            {

                var motionField = BaseMotionParts[a];
                var controller = layer.GetMotionController(motionField.ControllerIndex); 
                if (controller == null) continue;

                length = math.max(length, controller.GetMaxScaledDuration(layer)); 

            }

            return length;

        }

        public override float GetTime(IAnimationLayer layer, float addTime = 0)
        {

            if (MotionParts == null) return 0f;

            if (UseDedicatedChildForTime)
            {
                if (TimeDedicatedChildIndex < 0 || TimeDedicatedChildIndex >= MotionParts.Length) return 0f;

                var dedicatedChild = MotionParts[TimeDedicatedChildIndex];
                var controller = layer.GetMotionController(dedicatedChild.ControllerIndex);
                if (controller == null) return 0f;

                float duration = controller.GetDuration(layer);
                float t = controller.GetTime(layer, addTime) - (duration * dedicatedChild.NormalizedStartTime);
                if (t < 0f || t > duration) t = Maths.wrap(t, duration);
                return t;
            }

            float maxLength = GetScaledDuration(layer);
            float time = addTime < 0f ? 0f : maxLength;
            for (int a = 0; a < MotionParts.Length; a++)
            {

                var part = MotionParts[a];
                var controller = layer.GetMotionController(part.ControllerIndex);
                if (controller == null) continue;
                float length = controller.GetScaledDuration(layer);
                if (length == maxLength) // use time of controller with longest duration
                {
                    var duration = controller.GetDuration(layer);

                    float addTimeScaled = length <= 0 ? 0 : (addTime * (maxLength / length)); // Keep add time uniform among all children
                    float t = controller.GetTime(layer, addTimeScaled) - (duration * part.normalizedStartTime);
                    if (t < 0f || t > duration) t = Maths.wrap(t, duration);
                    time = addTime < 0 ? math.max(time, t) : math.min(time, t);
                }
            }

            return time;

        }

        public override float GetNormalizedTime(IAnimationLayer layer, float addTime = 0)
        {

            if (MotionParts == null) return 0f;

            if (UseDedicatedChildForTime)
            {
                if (TimeDedicatedChildIndex < 0 || TimeDedicatedChildIndex >= MotionParts.Length) return 0f;

                var dedicatedChild = MotionParts[TimeDedicatedChildIndex];
                var controller = layer.GetMotionController(dedicatedChild.ControllerIndex);
                if (controller == null) return 0f;

                float nT = controller.GetNormalizedTime(layer, addTime) - dedicatedChild.NormalizedStartTime;
                if (nT < 0f || nT > 1f) nT = Maths.wrap(nT, 1f);
                return nT;
            }

            float time = addTime < 0f ? 0f : 1f;

            float maxLength = GetScaledDuration(layer);
            for (int a = 0; a < MotionParts.Length; a++)
            {

                var part = MotionParts[a];
                var controller = layer.GetMotionController(part.ControllerIndex);
                if (controller == null) continue;
                float length = controller.GetScaledDuration(layer);
                if (length == maxLength) // use normalized time of controller with longest duration
                {
                    float addTimeScaled = length <= 0f ? 0f : (addTime * (maxLength / length)); // Keep add time uniform among all children
                    float nt = controller.GetNormalizedTime(layer, addTimeScaled) - part.NormalizedStartTime;
                    if (nt < 0f || nt > 1f) nt = Maths.wrap(nt, 1f);
                    time = addTime < 0 ? math.max(time, nt) : math.min(time, nt);
                }
            }

            return time;

        }

        public override void SetTime(IAnimationLayer layer, float time, bool resetFlags = true)
        {

            if (MotionParts == null) return;

            for (int a = 0; a < MotionParts.Length; a++)
            {

                var motionField = MotionParts[a];
                var controller = layer.GetMotionController(motionField.ControllerIndex);
                if (controller == null) continue;

                var duration = controller.GetDuration(layer);
                controller.SetTime(layer, time + (duration * motionField.NormalizedStartTime), resetFlags);
            }

        }

        public override void SetNormalizedTime(IAnimationLayer layer, float normalizedTime, bool resetFlags = true)
        {

            if (MotionParts == null) return;

            if (normalizedTime != 0f)
            {
                float maxLength = GetScaledDuration(layer);
                for (int a = 0; a < MotionParts.Length; a++)
                {

                    var motionField = MotionParts[a];
                    var controller = layer.GetMotionController(motionField.ControllerIndex);
                    if (controller == null) continue;
                    float length = controller.GetScaledDuration(layer);
                    controller.SetNormalizedTime(layer, length <= 0f ? 0f : ((normalizedTime * (maxLength / length)) + motionField.NormalizedStartTime), resetFlags);
                }
            } 
            else
            {
                for (int a = 0; a < MotionParts.Length; a++)
                {

                    var motionField = MotionParts[a];
                    var controller = layer.GetMotionController(motionField.ControllerIndex);
                    if (controller == null) continue;

                    controller.SetNormalizedTime(layer, motionField.normalizedStartTime, resetFlags);
                }
            }

        }

        [NonSerialized]
        protected int finalFieldCached;
        public int FinalFieldIndex => finalFieldCached - 1;
        protected void RefreshFinalField(IAnimationLayer layer)
        {
            finalFieldCached = 0;

            for (int a = 0; a < MotionParts.Length; a++)
            {

                var motionField = MotionParts[a];
                var controller = layer.GetMotionController(motionField.ControllerIndex);
                if (controller is not CustomMotionController) continue;

                finalFieldCached = a + 1;

            }
        }

        public override void Progress(IAnimationLayer layer, float deltaTime, ref JobHandle jobHandle, bool useMultithreading = true, bool isFinal = false, bool canLoop = true)
        {

            if (MotionParts == null) return;

            if (finalFieldCached <= 0) RefreshFinalField(layer);

            if (ForceSyncOnProgress && MotionParts.Length > 1)
            {
                float sync_time = 0f;
                float sync_duration = 0f;
                int firstIndex = -1;
                if (UseDedicatedChildForTime)
                {
                    sync_time = GetTime(layer, 0f);
                    sync_duration = GetDuration(layer);
                }
                else
                {
                    for (int a = 0; a < MotionParts.Length; a++)
                    {
                        var motionField = MotionParts[a];
                        var controller = layer.GetMotionController(motionField.ControllerIndex);
                        if (controller is not CustomMotionController cmc) continue;

                        sync_duration = controller.GetDuration(layer);
                        sync_time = controller.GetTime(layer) - (sync_duration * motionField.NormalizedStartTime);
                        firstIndex = a;
                        break;
                    }
                }

                float sync_duration_abs = math.abs(sync_duration); 
                for (int a = firstIndex + 1; a < MotionParts.Length; a++)
                {
                    if (UseDedicatedChildForTime && a == TimeDedicatedChildIndex) continue; // skip dedicated child

                    var motionField = MotionParts[a];
                    var controller = layer.GetMotionController(motionField.ControllerIndex);
                    if (controller is not CustomMotionController cmc) continue;

                    var duration = controller.GetDuration(layer);
                    if (duration == 0f) continue;

                    float duration_abs = math.abs(duration);
                    float offset = duration * motionField.NormalizedStartTime;

                    float localTime = 0f;
                    if (sync_duration != 0f)
                    {
                        bool isSmaller = sync_duration_abs >= duration_abs;
                        float ratio = isSmaller ? (sync_duration / duration) : (duration / sync_duration);
                        ratio = (ratio - math.floor(ratio)) + 1f;
                        float scalingRatio = isSmaller ? ratio : 1f; 

                        //Debug.Log($"{Name}: {controller.Name}: RATIO:{ratio}   ({duration})({sync_duration})"); 

                        float currentTime = controller.GetTime(layer);
                        float currentTimeScaled = (currentTime - offset) * scalingRatio;
                        float syncTimeScaled = isSmaller ? sync_time : (sync_time * ratio);
                        float syncDurationScaled = isSmaller ? sync_duration : (sync_duration * ratio);

                        float error = currentTimeScaled - (syncDurationScaled * math.floor(currentTimeScaled / syncDurationScaled));
                        error = error - sync_time;

                        //Debug.Log($"{Name}: {controller.Name}: ST:{sync_time}  CT:{currentTime}  ERROR:{error}"); 

                        localTime = (currentTimeScaled - error) / scalingRatio;
                    }

                    controller.SetTime(layer, localTime + offset, false); // force sync all controllers to the sync controller's time
                }
            }

            if (!waitForLoops)
            {
                canLoop = true;
            }
            else if (canLoop)
            {

                float normalizedTime = GetNormalizedTime(layer, deltaTime);
                canLoop = deltaTime < 0 && normalizedTime <= 0 || deltaTime >= 0 && normalizedTime >= 1;

            }

            for (int a = 0; a < MotionParts.Length; a++)
            {
                var motionPart = MotionParts[a];

                var controller = layer.GetMotionController(motionPart.ControllerIndex);
                if (controller == null) continue;

                MotionPart syncPart;
                IAnimationMotionController syncController;
                switch (motionPart.SyncMode)
                {
                    case TimeSyncMode.SyncTime:
                        if (motionPart.LocalSyncReferenceIndex != a)
                        {
                            syncPart = MotionParts[motionPart.LocalSyncReferenceIndex];
                            if (syncPart != null)
                            {
                                syncController = layer.GetMotionController(syncPart.ControllerIndex);
                                if (syncController != null)
                                {
                                    var duration = controller.GetDuration(layer);
                                    controller.SetTime(layer, syncController.GetTime(layer) + (duration * motionPart.NormalizedStartTime), false);
                                }
                            }
                        }
                        break;

                    case TimeSyncMode.SyncNormalizedTime:
                        if (motionPart.LocalSyncReferenceIndex != a)
                        {
                            syncPart = MotionParts[motionPart.LocalSyncReferenceIndex];
                            if (syncPart != null)
                            {
                                syncController = layer.GetMotionController(syncPart.ControllerIndex);
                                if (syncController != null)
                                {
                                    controller.SetNormalizedTime(layer, syncController.GetNormalizedTime(layer) + motionPart.NormalizedStartTime, false);
                                }
                            }
                        }
                        break;
                }
            }

            for (int a = 0; a < MotionParts.Length; a++)
            {
                var motionPart = MotionParts[a];
                var controller = layer.GetMotionController(motionPart.ControllerIndex);
                if (controller is not CustomMotionController cmc)
                {
                    if (a == FinalFieldIndex) RefreshFinalField(layer);
                    continue;
                }

                cmc.SetWeight(motionPart.GetMix(layer));
                cmc.SyncWeight(layer);
                cmc.Progress(layer, deltaTime, ref jobHandle, useMultithreading, isFinal && a == FinalFieldIndex, canLoop);
            }

        }

        public override bool HasAnimationPlayer(IAnimationLayer layer)
        {

            if (MotionParts == null) return false;

            for (int a = 0; a < MotionParts.Length; a++)
            {

                var motionField = MotionParts[a];
                var controller = layer.GetMotionController(motionField.ControllerIndex);
                if (controller == null || !controller.HasAnimationPlayer(layer)) return false;

            }

            return true;

        }

        public override void SyncWeight(IAnimationLayer layer)
        {
            base.SyncWeight(layer);

            if (MotionParts == null) return;

            for (int a = 0; a < MotionParts.Length; a++)
            {

                var part = MotionParts[a];
                var controller = layer.GetMotionController(part.ControllerIndex); 
                if (controller == null) continue;

                controller.SyncWeight(layer);
            }
        }

    }

    [Serializable]
    public abstract class BlendTree : CustomMotionController, IBlendTree
    {

        public bool normalizeDurations;
        public float normalizedDuration;

        [SerializeField]
        public bool waitForLoops;
        public bool WaitForLoops
        {
            get => waitForLoops;
            set => waitForLoops = value;
        }

        [SerializeField]
        public bool forceSyncOnProgress;
        public bool ForceSyncOnProgress
        {
            get => forceSyncOnProgress;
            set => forceSyncOnProgress = value;
        }

        [SerializeField]
        public bool useDedicatedChildForTime;
        public bool UseDedicatedChildForTime
        {
            get => useDedicatedChildForTime;
            set => useDedicatedChildForTime = value;
        }
        [SerializeField]
        public int timeDedicatedChildIndex;
        public int TimeDedicatedChildIndex
        {
            get => timeDedicatedChildIndex;
            set => timeDedicatedChildIndex = value;
        }
        public float GetDedicatedChildDuration(IAnimationLayer layer)
        {
            if (!UseDedicatedChildForTime || TimeDedicatedChildIndex < 0) return 0f;

            var motionField = BaseMotionFields[TimeDedicatedChildIndex];
            var controller = layer.GetMotionController(motionField.ControllerIndex);
            if (controller == null) return 0f;

            return controller.GetDuration(layer);
        }

        public override void Initialize(IAnimationLayer layer, List<AvatarMaskUsage> masks, IAnimationMotionController parent = null)
        {

            base.Initialize(layer, masks, parent);
            if (normalizeDurations) Debug.Log($"{Name} Normalizing Durations to {normalizedDuration}"); 
            if (BaseMotionFields != null)
            {

                for (int a = 0; a < BaseMotionFields.Length; a++)
                {

                    var field = BaseMotionFields[a];
                    var controller = layer.GetMotionController(BaseMotionFields[a].ControllerIndex); 

                    if (controller != null)
                    {

                        controller.Initialize(layer, masks, this);

                        float speed = 1f;
                        if (normalizeDurations)
                        {
                            if (normalizedDuration <= 0f)
                            {
                                speed = 0f;
                            }
                            else
                            {
                                float duration = controller.GetDuration(layer); 
                                speed = duration / normalizedDuration;
                                if (duration <= 0f) Debug.LogError($"{controller.Name} duration is negative or zero: {duration} (BaseSpeed: {controller.BaseSpeed})");
                            }
                        }

                        controller.BaseSpeed = speed * field.Speed;
                        if (normalizeDurations) Debug.Log($"{Name} Normalized Duration for {controller.Name}. (Normalized Speed:{speed}) (Base Speed:{controller.BaseSpeed}) (Duration:{controller.GetScaledDuration(layer)})");

                        controller.SetNormalizedTime(layer, field.NormalizedStartTime);
                    }

                }

            }

        }

        public override bool HasChildControllers => true;

        public abstract void UpdateBlendValues(IAnimationLayer layer, float normalizedTime, float deltaTime);
        public abstract int ParameterCount { get; }
        /// <summary>
        /// If the tree is affected by multiple parameters; localParameterIndex is used to fetch the parameter locally first.
        /// </summary>
        public abstract int GetParameterIndex(int localParameterIndex);
        public abstract float GetLastParameterValue(int localParameterIndex);
        public abstract void SetLastParameterValue(int localParameterIndex, float value);
        public abstract BlendValuesUpdateLimit GetParameterUpdateLimit(int localParameterIndex);
        public abstract void GetParameterUpdateNormalizedTimeRange(int localParameterIndex, out float timeMin, out float timeMax);

        public override float GetBiasedParameterValue(IAnimationLayer layer, int parameterIndex, float defaultValue, bool updateParameter, out bool appliedBias)
        {
            appliedBias = false;

            if (BaseMotionFields != null)
            {
                for (int a = 0; a < BaseMotionFields.Length; a++)
                {
                    var motionField = BaseMotionFields[a];
                    var controller = layer.GetMotionController(motionField.ControllerIndex);
                    if (controller == null) continue;

                    float val = controller.GetBiasedParameterValue(layer, parameterIndex, defaultValue, updateParameter, out appliedBias);
                    if (appliedBias) return val;
                }
            }

            return base.GetBiasedParameterValue(layer, parameterIndex, defaultValue, updateParameter, out appliedBias);
        }

        public override void GetParameterIndices(IAnimationLayer layer, List<int> indices)
        {

            if (indices == null) return;

            base.GetParameterIndices(layer, indices);

            if (ParameterCount > 0)
            {
                for (int a = 0; a < ParameterCount; a++)
                {
                    int paramIndex = GetParameterIndex(a);
                    if (paramIndex > 0) indices.Add(paramIndex);
                }

            }

            if (BaseMotionFields != null)
            {

                foreach (var field in BaseMotionFields)
                {

                    var controller = layer.GetMotionController(field.ControllerIndex);
                    if (controller == null) continue;

                    controller.GetParameterIndices(layer, indices);

                }

            }

        }

        [NonSerialized]
        protected int[] childIndices;
        public int[] GetChildControllerIndices()
        {
            if (childIndices == null)
            {
                childIndices = new int[BaseMotionFields == null ? 0 : BaseMotionFields.Length];
                for (int a = 0; a < childIndices.Length; a++) childIndices[a] = BaseMotionFields[a].ControllerIndex;
            }

            return childIndices;
        }

        public abstract IMotionField[] BaseMotionFields { get; }

        public override bool HasDerivativeHierarchyOf(IAnimationLayer layer, IAnimationMotionController other)
        {

            if (layer == null || other == null) return false;

            var blendTreeChildren = GetChildControllerIndices();
            if (blendTreeChildren == null || blendTreeChildren.Length == 0) return true;

            for (int a = 0; a < blendTreeChildren.Length; a++)
            {

                var child = layer.GetMotionController(blendTreeChildren[a]);
                if (child == null) continue;

                if (!child.HasDerivativeHierarchyOf(layer, other)) return false;

            }

            return true;

        }

        public override int GetLongestHierarchyIndex(IAnimationLayer layer)
        {

            if (layer == null) return -1;

            var blendTreeChildren = GetChildControllerIndices();
            if (blendTreeChildren == null || blendTreeChildren.Length == 0) return -1;

            int longestIndex = -1;
            int longestCount = int.MinValue;
            for (int a = 0; a < blendTreeChildren.Length; a++)
            {

                var child = layer.GetMotionController(blendTreeChildren[a]);
                if (child == null) continue;

                int hierarchyIndex = child.GetLongestHierarchyIndex(layer);
                if (hierarchyIndex < 0) continue;

                var hierarchy = layer.Animator.GetTransformHierarchy(hierarchyIndex);
                if (hierarchy == null) continue;

                int count = hierarchy.Count;

                if (longestIndex < 0 || count > longestCount)
                {

                    longestIndex = hierarchyIndex;
                    longestCount = count;

                }

            }

            return longestIndex;

        }

        public override AnimationLoopMode GetLoopMode(IAnimationLayer layer)
        {

            AnimationLoopMode loopMode = AnimationLoopMode.Loop;

            if (BaseMotionFields != null)
            {

                foreach (var field in BaseMotionFields)
                {

                    var controller = layer.GetMotionController(field.ControllerIndex);
                    if (controller == null) continue;

                    loopMode = controller.GetLoopMode(layer);
                    break;

                }

            }

            return loopMode;

        }

        public override void ForceSetLoopMode(IAnimationLayer layer, AnimationLoopMode loopMode)
        {

            if (BaseMotionFields != null)
            {

                foreach (var field in BaseMotionFields)
                {

                    var controller = layer.GetMotionController(field.ControllerIndex);
                    if (controller == null) continue;

                    controller.ForceSetLoopMode(layer, loopMode);

                }

            }

        }

        public override float GetDuration(IAnimationLayer layer)
        {
            if (UseDedicatedChildForTime)
            {
                if (BaseMotionFields == null || TimeDedicatedChildIndex < 0 || TimeDedicatedChildIndex >= BaseMotionFields.Length) return 0f;

                var dedicatedChild = BaseMotionFields[TimeDedicatedChildIndex];
                var controller = layer.GetMotionController(dedicatedChild.ControllerIndex);
                if (controller == null) return 0f;

                return controller.GetDuration(layer);
            }

            return GetScaledDuration(layer); // Blend tree always uses scaled lengths
        }

        /// <summary>
        /// Get length of controller scaled by playback speed
        /// </summary>
        public override float GetScaledDuration(IAnimationLayer layer)
        {

            if (BaseMotionFields == null) return 0f;

            if (UseDedicatedChildForTime)
            {
                if (TimeDedicatedChildIndex < 0 || TimeDedicatedChildIndex >= BaseMotionFields.Length) return 0f;

                var dedicatedChild = BaseMotionFields[TimeDedicatedChildIndex];
                var controller = layer.GetMotionController(dedicatedChild.ControllerIndex);
                if (controller == null) return 0f;

                return controller.GetScaledDuration(layer);
            }

            float totalWeight = 0f;
            float length = 0f;
            for (int a = 0; a < BaseMotionFields.Length; a++)
            {

                var motionField = BaseMotionFields[a];
                var controller = layer.GetMotionController(motionField.ControllerIndex);
                if (controller == null) continue;

                float weight = Mathf.Abs(controller.GetBaseWeight());
                totalWeight += weight;

                length += controller.GetScaledDuration(layer) * weight; // Blend tree always uses scaled length because children can have varied playback speeds

            }

            return totalWeight > 0f ? (length / totalWeight) : length; 

        }
        public override float GetMaxDuration(IAnimationLayer layer)
        {

            if (BaseMotionFields == null) return 0f;

            if (UseDedicatedChildForTime)
            {
                if (TimeDedicatedChildIndex < 0 || TimeDedicatedChildIndex >= BaseMotionFields.Length) return 0f;

                var dedicatedChild = BaseMotionFields[TimeDedicatedChildIndex];
                var controller = layer.GetMotionController(dedicatedChild.ControllerIndex);
                if (controller == null) return 0f;

                return controller.GetMaxDuration(layer);
            }

            float length = 0f;
            for (int a = 0; a < BaseMotionFields.Length; a++)
            {

                var motionField = BaseMotionFields[a];
                var controller = layer.GetMotionController(motionField.ControllerIndex);
                if (controller == null) continue;

                length = math.max(length, controller.GetMaxDuration(layer)); // Blend tree always uses scaled length because children can have varied playback speeds

            }

            return length;

        }
        public override float GetMaxScaledDuration(IAnimationLayer layer)
        {

            if (BaseMotionFields == null) return 0f;

            if (UseDedicatedChildForTime)
            {
                if (TimeDedicatedChildIndex < 0 || TimeDedicatedChildIndex >= BaseMotionFields.Length) return 0f;

                var dedicatedChild = BaseMotionFields[TimeDedicatedChildIndex];
                var controller = layer.GetMotionController(dedicatedChild.ControllerIndex);
                if (controller == null) return 0f;

                return controller.GetMaxScaledDuration(layer);
            }

            float length = 0f;
            for (int a = 0; a < BaseMotionFields.Length; a++)
            {

                var motionField = BaseMotionFields[a];
                var controller = layer.GetMotionController(motionField.ControllerIndex);
                if (controller == null) continue;

                length = math.max(length, controller.GetMaxScaledDuration(layer)); // Blend tree always uses scaled length because children can have varied playback speeds

            }

            return length;

        }

        public override float GetTime(IAnimationLayer layer, float addTime = 0)
        {

            if (BaseMotionFields == null) return 0f;

            if (UseDedicatedChildForTime)
            {
                if (TimeDedicatedChildIndex < 0 || TimeDedicatedChildIndex >= BaseMotionFields.Length) return 0f;

                var dedicatedChild = BaseMotionFields[TimeDedicatedChildIndex];
                var controller = layer.GetMotionController(dedicatedChild.ControllerIndex);
                if (controller == null) return 0f;

                float duration = controller.GetDuration(layer);
                float t = controller.GetTime(layer, addTime) - (duration * dedicatedChild.NormalizedStartTime);  
                if (t < 0f || t > duration) t = Maths.wrap(t, duration);
                return t;
            }

            float totalWeight = 0f;
            float time = 0f;

            float maxLength = GetMaxScaledDuration(layer);
            for (int a = 0; a < BaseMotionFields.Length; a++)
            {

                var motionField = BaseMotionFields[a];
                var controller = layer.GetMotionController(motionField.ControllerIndex);
                if (controller == null) continue;

                float length = controller.GetScaledDuration(layer);
                var duration = controller.GetDuration(layer);

                float weight = Mathf.Abs(controller.GetBaseWeight());
                totalWeight += weight;

                float addTimeScaled = length <= 0 ? 0 : (addTime * (maxLength / length)); // Keep add time uniform among all children
                float t = controller.GetTime(layer, addTimeScaled) - (duration * motionField.NormalizedStartTime);
                if (t < 0f || t > duration) t = Maths.wrap(t, duration);
                time += t * weight;

            }

            return totalWeight > 0f ? (time / totalWeight) : time;

        }

        public override float GetNormalizedTime(IAnimationLayer layer, float addTime = 0)
        {

            if (BaseMotionFields == null) return 0f;

            if (UseDedicatedChildForTime)
            {
                if (TimeDedicatedChildIndex < 0 || TimeDedicatedChildIndex >= BaseMotionFields.Length) return 0f;

                var dedicatedChild = BaseMotionFields[TimeDedicatedChildIndex];
                var controller = layer.GetMotionController(dedicatedChild.ControllerIndex);
                if (controller == null) return 0f;

                float nT = controller.GetNormalizedTime(layer, addTime) - dedicatedChild.NormalizedStartTime;
                if (nT < 0f || nT > 1f) nT = Maths.wrap(nT, 1f);
                return nT;
            }

            float totalWeight = 0f;
            float time = 0f;

            float maxLength = GetMaxScaledDuration(layer);
            for (int a = 0; a < BaseMotionFields.Length; a++)
            {
                var motionField = BaseMotionFields[a];
                var controller = layer.GetMotionController(motionField.ControllerIndex);  
                if (controller == null) continue;

                float length = controller.GetScaledDuration(layer);
                if (length == 0f) continue; // ignore controllers that have no length or aren't progressing

                float weight = Mathf.Abs(controller.GetBaseWeight());
                totalWeight += weight;

                float addTimeScaled = length <= 0f ? 0f : (addTime * (maxLength / length)); // Keep add time uniform among all children
                float nt = controller.GetNormalizedTime(layer, addTimeScaled) - motionField.NormalizedStartTime;
                if (nt < 0f || nt > 1f) nt = Maths.wrap(nt, 1f);
                time += nt * weight;
            }

            return totalWeight > 0f ? (time / totalWeight) : time;

        }

        public override void SetTime(IAnimationLayer layer, float time, bool resetFlags = true)
        {

            if (BaseMotionFields == null) return;

            for (int a = 0; a < BaseMotionFields.Length; a++)
            {

                var motionField = BaseMotionFields[a];
                var controller = layer.GetMotionController(motionField.ControllerIndex);
                if (controller == null) continue;

                var duration = controller.GetDuration(layer);
                controller.SetTime(layer, time + (duration * motionField.NormalizedStartTime), resetFlags); 
            }

        }

        public override void SetNormalizedTime(IAnimationLayer layer, float normalizedTime, bool resetFlags = true)
        {

            if (BaseMotionFields == null) return;

            if (normalizedTime != 0f)
            {
                float maxLength = GetMaxScaledDuration(layer);
                for (int a = 0; a < BaseMotionFields.Length; a++)
                {

                    var motionField = BaseMotionFields[a];
                    var controller = layer.GetMotionController(motionField.ControllerIndex);
                    if (controller == null) continue;
                    float length = controller.GetScaledDuration(layer);
                    controller.SetNormalizedTime(layer, length <= 0f ? 0f : ((normalizedTime * (maxLength / length)) + motionField.NormalizedStartTime), resetFlags);
                }
            } 
            else
            {
                for (int a = 0; a < BaseMotionFields.Length; a++)
                {

                    var motionField = BaseMotionFields[a];
                    var controller = layer.GetMotionController(motionField.ControllerIndex);
                    if (controller == null) continue;
                    controller.SetNormalizedTime(layer, motionField.NormalizedStartTime, resetFlags); 
                }
            }

        }

        [NonSerialized]
        protected int finalFieldCached;
        public int FinalFieldIndex => finalFieldCached - 1;
        protected void RefreshFinalField(IAnimationLayer layer)
        {
            finalFieldCached = 0;

            for (int a = 0; a < BaseMotionFields.Length; a++)
            {

                var motionField = BaseMotionFields[a];
                var controller = layer.GetMotionController(motionField.ControllerIndex);
                if (controller is not CustomMotionController) continue;

                finalFieldCached = a + 1;

            }
        }

        [NonSerialized]
        protected bool firstProgressCallFlag;

        public override void Reset(IAnimationLayer layer)
        {
            base.Reset(layer);
            firstProgressCallFlag = false;

            if (BaseMotionFields != null)
            {
                for (int a = 0; a < BaseMotionFields.Length; a++)
                {
                    var motionField = BaseMotionFields[a];
                    var controller = layer.GetMotionController(motionField.ControllerIndex);
                    if (controller == null) continue;

                    controller.Reset(layer);
                    controller.SetNormalizedTime(layer, motionField.NormalizedStartTime);
                }
            }
        }

        public virtual float FetchParameterValue(int localParameterIndex, IAnimationLayer layer, float prevNormalizedTime, float normalizedTime)
        {
            IAnimationParameter parameter = null;
            float timeMin, timeMax;
            bool flagA, flagB, flagC, flagD;
            switch (GetParameterUpdateLimit(localParameterIndex))
            {
                default:
                    parameter = layer.GetParameter(GetParameterIndex(localParameterIndex));
                    if (parameter != null) SetLastParameterValue(localParameterIndex, parameter.Value);
                    break;

                case BlendValuesUpdateLimit.FirstProgressCall:
                    if (!firstProgressCallFlag)
                    {
                        parameter = layer.GetParameter(GetParameterIndex(localParameterIndex));
                        if (parameter != null) SetLastParameterValue(localParameterIndex, parameter.Value);
                    }
                    break;

                case BlendValuesUpdateLimit.InsideNormalizedTimeRange:
                    GetParameterUpdateNormalizedTimeRange(localParameterIndex, out timeMin, out timeMax);
                    flagA = prevNormalizedTime >= timeMin && prevNormalizedTime <= timeMax;
                    flagB = normalizedTime >= timeMin && normalizedTime <= timeMax;
                    flagC = prevNormalizedTime < timeMin;
                    flagD = normalizedTime < timeMin;
                    if (flagA || flagB || (flagC != flagD)) 
                    {
                        parameter = layer.GetParameter(GetParameterIndex(localParameterIndex));
                        if (parameter != null) SetLastParameterValue(localParameterIndex, parameter.Value);
                    }
                    break; 

                case BlendValuesUpdateLimit.OutsideNormalizedTimeRange:
                    GetParameterUpdateNormalizedTimeRange(localParameterIndex, out timeMin, out timeMax);
                    flagA = prevNormalizedTime < timeMin || prevNormalizedTime > timeMax;
                    flagB = normalizedTime < timeMin || normalizedTime > timeMax;
                    if (flagA || flagB)
                    {
                        parameter = layer.GetParameter(GetParameterIndex(localParameterIndex));
                        if (parameter != null) SetLastParameterValue(localParameterIndex, parameter.Value);
                    }
                    break;
            }

            return GetLastParameterValue(localParameterIndex);
        }

        public override void Progress(IAnimationLayer layer, float deltaTime, ref JobHandle jobHandle, bool useMultithreading = true, bool isFinal = false, bool canLoop = true)
        {
            if (BaseMotionFields == null) return;

            if (finalFieldCached <= 0) RefreshFinalField(layer);

            if (ForceSyncOnProgress && BaseMotionFields.Length > 1)
            {
                float sync_time = 0f;
                float sync_duration = 0f;
                int firstIndex = -1;
                if (UseDedicatedChildForTime)
                {
                    sync_time = GetTime(layer, 0f);
                    sync_duration = GetDuration(layer);
                }
                else
                {
                    for (int a = 0; a < BaseMotionFields.Length; a++)
                    {
                        var motionField = BaseMotionFields[a];
                        var controller = layer.GetMotionController(motionField.ControllerIndex);
                        if (controller is not CustomMotionController cmc) continue;

                        sync_duration = controller.GetDuration(layer);
                        sync_time = controller.GetTime(layer) - (sync_duration * motionField.NormalizedStartTime);
                        firstIndex = a;
                        break;
                    }
                }

                float sync_duration_abs = math.abs(sync_duration);
                for (int a = firstIndex + 1; a < BaseMotionFields.Length; a++)
                {
                    if (UseDedicatedChildForTime && a == TimeDedicatedChildIndex) continue; // skip dedicated child

                    var motionField = BaseMotionFields[a];
                    var controller = layer.GetMotionController(motionField.ControllerIndex); 
                    if (controller is not CustomMotionController cmc) continue;

                    var duration = controller.GetDuration(layer);
                    if (duration == 0f) continue;

                    float duration_abs = math.abs(duration);
                    float offset = duration * motionField.NormalizedStartTime;

                    float localTime = 0f;
                    if (sync_duration != 0f)
                    {
                        bool isSmaller = sync_duration_abs >= duration_abs;
                        float ratio = isSmaller ? (sync_duration / duration) : (duration / sync_duration);
                        ratio = (ratio - math.floor(ratio)) + 1f;
                        float scalingRatio = isSmaller ? ratio : 1f;

                        //Debug.Log($"{Name}: {controller.Name}: RATIO:{ratio}   ({duration})({sync_duration})"); 

                        float currentTime = controller.GetTime(layer);
                        float currentTimeScaled = (currentTime - offset) * scalingRatio;
                        float syncTimeScaled = isSmaller ? sync_time : (sync_time * ratio);
                        float syncDurationScaled = isSmaller ? sync_duration : (sync_duration * ratio);

                        float error = currentTimeScaled - (syncDurationScaled * math.floor(currentTimeScaled / syncDurationScaled));  
                        error = error - sync_time;

                        //Debug.Log($"{Name}: {controller.Name}: ST:{sync_time}  CT:{currentTime}  ERROR:{error}"); 

                        localTime = (currentTimeScaled - error) / scalingRatio;   
                    }

                    controller.SetTime(layer, localTime + offset, false); // force sync all controllers to the sync controller's time
                }
            } 

            float normalizedTime = GetNormalizedTime(layer);
            UpdateBlendValues(layer, normalizedTime, deltaTime);

            if (!waitForLoops)
            {
                canLoop = true;
            }
            else if (canLoop)
            {
                normalizedTime = normalizedTime + deltaTime;
                canLoop = deltaTime < 0f && normalizedTime <= 0f || deltaTime >= 0f && normalizedTime >= 1f;
            }

            for (int a = 0; a < BaseMotionFields.Length; a++)
            {

                var motionField = BaseMotionFields[a];
                var controller = layer.GetMotionController(motionField.ControllerIndex);
                if (controller is not CustomMotionController cmc)
                {
                    if (a == FinalFieldIndex) RefreshFinalField(layer); 
                    continue;
                }

#if UNITY_EDITOR
                //if (cmc.Parent == null || !ReferenceEquals(this, cmc.Parent)) Debug.LogError($"NULL OR MISMATCHED PARENT FOR {cmc.Name} (REAL PARENT: {Name}) (CURRENT PARENT: {(cmc.Parent == null ? "null" : cmc.Parent.Name)})"); 
#endif
                cmc.Progress(layer, deltaTime, ref jobHandle, useMultithreading, isFinal && a == FinalFieldIndex, canLoop);
            }
        }

        public override bool HasAnimationPlayer(IAnimationLayer layer)
        {

            if (BaseMotionFields == null) return false;

            for (int a = 0; a < BaseMotionFields.Length; a++)
            {

                var motionField = BaseMotionFields[a];
                var controller = layer.GetMotionController(motionField.ControllerIndex);
                if (controller == null || !controller.HasAnimationPlayer(layer)) return false;

            }

            return true;

        }

        public override void SyncWeight(IAnimationLayer layer) 
        {
            base.SyncWeight(layer);

            if (BaseMotionFields == null) return;

            for (int a = 0; a < BaseMotionFields.Length; a++)
            {

                var motionField = BaseMotionFields[a];
                var controller = layer.GetMotionController(motionField.ControllerIndex);
                if (controller == null) continue;

                controller.SyncWeight(layer);
            }
        }
    }

    [Serializable]
    public class BlendTree1D : BlendTree, IBlendTree1D
    {

        #region Serialization

        [Serializable]
        public struct Serialized : ISerializableContainer<BlendTree1D, BlendTree1D.Serialized>
        {

            public string name;
            public string SerializedName => name;

            public string avatarMaskPath;
            public WeightedAvatarMask embeddedAvatarMask;
            public bool invertAvatarMask;

            public float baseSpeed;
            public int speedMultiplierParameter;
            public bool normalizeDurations;
            public float normalizedDuration;

            public int parameterIndex;
            public MotionField1D[] motionFields;

            public BlendValuesUpdateLimit parameter_UpdateLimit;
            public float2 parameter_UpdateNormalizedTimeRange;

            public bool waitForLoops;
            public bool forceSyncOnProgress;

            public bool useDedicatedChildForTime;
            public int timeDedicatedChildIndex;

            public string AsJSON(bool prettyPrint = false) => swole.Engine.ToJson(this, prettyPrint);

            public object AsNonserializableObject(PackageInfo packageInfo = default) => AsOriginalType(packageInfo);
            public BlendTree1D AsOriginalType(PackageInfo packageInfo = default)
            {
                var anim = new BlendTree1D(name, parameterIndex, motionFields);

                if (!string.IsNullOrWhiteSpace(avatarMaskPath))
                {
                    anim.avatarMask = AnimationLibrary.FindAvatarMask(packageInfo, avatarMaskPath);
                    if (anim.avatarMask == null) anim.avatarMask = AnimationLibrary.FindAvatarMask(avatarMaskPath);
                }
                else
                {
                    anim.avatarMask = embeddedAvatarMask;
                }
                anim.invertAvatarMask = invertAvatarMask;

                anim.baseSpeed = baseSpeed;
                anim.speedMultiplierParameter = speedMultiplierParameter;
                anim.normalizeDurations = normalizeDurations;
                anim.normalizedDuration = normalizedDuration;

                anim.parameter_UpdateLimit = parameter_UpdateLimit;
                anim.parameter_UpdateNormalizedTimeRange = parameter_UpdateNormalizedTimeRange;

                anim.waitForLoops = waitForLoops;
                anim.forceSyncOnProgress = forceSyncOnProgress;

                anim.useDedicatedChildForTime = useDedicatedChildForTime;
                anim.timeDedicatedChildIndex = timeDedicatedChildIndex;

                return anim;
            }

            public static implicit operator Serialized(BlendTree1D inst)
            {
                Serialized s = new Serialized();

                s.name = inst.name;

                if (inst.AvatarMask != null)
                {
                    if (inst.AvatarMask.PackageInfo.NameIsValid)
                    {
                        s.avatarMaskPath = inst.AvatarMask.PackageInfo.ConvertToAssetPath(inst.avatarMask.Name);
                    }
                    else
                    {
                        s.embeddedAvatarMask = inst.AvatarMask;
                    }
                }
                s.invertAvatarMask = inst.invertAvatarMask;

                s.baseSpeed = inst.baseSpeed;
                s.speedMultiplierParameter = inst.speedMultiplierParameter;
                s.normalizeDurations = inst.normalizeDurations;
                s.normalizedDuration = inst.normalizedDuration;

                s.parameterIndex = inst.parameterIndex;
                s.motionFields = inst.motionFields;

                s.parameter_UpdateLimit = inst.parameter_UpdateLimit;
                s.parameter_UpdateNormalizedTimeRange = inst.parameter_UpdateNormalizedTimeRange;

                s.waitForLoops = inst.waitForLoops;
                s.forceSyncOnProgress = inst.forceSyncOnProgress;

                s.useDedicatedChildForTime = inst.useDedicatedChildForTime;
                s.timeDedicatedChildIndex = inst.timeDedicatedChildIndex;

                return s;
            }

        }

        #endregion

        public BlendTree1D() { }

        public BlendTree1D(string name, int parameterIndex, MotionField1D[] motionFields)
        {
            this.name = name;
            this.parameterIndex = parameterIndex;
            this.motionFields = motionFields;
        }

        public override object Clone() => Duplicate();
        public BlendTree1D Duplicate()
        {

            var clone = new BlendTree1D();

            CustomMotionController.CloneBase(this, clone);

            clone.parameterIndex = parameterIndex;
            clone.motionFields = clone.motionFields = motionFields == null ? null : (MotionField1D[])motionFields.DeepClone();

            clone.normalizeDurations = normalizeDurations;
            clone.normalizedDuration = normalizedDuration;

            clone.parameter_UpdateLimit = parameter_UpdateLimit;
            clone.parameter_UpdateNormalizedTimeRange = parameter_UpdateNormalizedTimeRange;

            clone.waitForLoops = waitForLoops;
            clone.forceSyncOnProgress = forceSyncOnProgress;

            clone.useDedicatedChildForTime = useDedicatedChildForTime;
            clone.timeDedicatedChildIndex = timeDedicatedChildIndex;

            return clone;

        }
        public override void GetChildIndices(List<int> indices, bool onlyAddIfNotPresent)
        {

            if (motionFields == null || indices == null) return;

            if (onlyAddIfNotPresent)
            {

                foreach (var field in motionFields) if (!indices.Contains(field.controllerIndex)) indices.Add(field.controllerIndex);
                
            }
            else
            {

                foreach (var field in motionFields) indices.Add(field.controllerIndex);

            }

        }

        public override void RemapChildIndices(Dictionary<int, int> remapper, bool invalidateNonRemappedIndices = false) => RemapChildIndices(remapper, 0, invalidateNonRemappedIndices);
        public override void RemapChildIndices(Dictionary<int, int> remapper, int minIndex, bool invalidateNonRemappedIndices = false)
        {

            if (motionFields == null || remapper == null) return;
            motionFields = (MotionField1D[])motionFields.Clone();

            for (int a = minIndex; a < motionFields.Length; a++)
            {

                var field = motionFields[a].Duplicate();

                if (remapper.TryGetValue(field.controllerIndex, out int newIndex))
                {
                    field.controllerIndex = newIndex;
                }
                else if (invalidateNonRemappedIndices)
                {
                    field.controllerIndex = -1;
#if UNITY_EDITOR
                    Debug.LogError($"Failed to remap motion field [{a}] controller index for {GetType().Name} '{name}'");
#else
                    swole.LogError($"Failed to remap motion field [{a}] controller index for {GetType().Name} '{name}'");
#endif
                }

                motionFields[a] = field;

            }

        }

        public override int GetChildCount() => motionFields == null ? 0 : motionFields.Length;
        public override int GetChildIndex(int childIndex) => motionFields == null ? -1 : motionFields[childIndex].controllerIndex;
        public override void SetChildIndex(int childIndex, int controllerIndex)
        {
            if (motionFields == null) return;

            motionFields[childIndex].controllerIndex = controllerIndex;
        }

        [SerializeField]
        public int parameterIndex;
        public int ParameterIndex
        {
            get => parameterIndex;
            set => parameterIndex = value;
        }

        [NonSerialized]
        protected float lastParameterValue;

        public BlendValuesUpdateLimit parameter_UpdateLimit;
        public float2 parameter_UpdateNormalizedTimeRange = new float2(0f, 1f);
        
        public override int ParameterCount => 1;

        public override int GetParameterIndex(int localParameterIndex) => ParameterIndex;

        public override float GetLastParameterValue(int localParameterIndex) => lastParameterValue;
        public override void SetLastParameterValue(int localParameterIndex, float value) => lastParameterValue = value;
        public override BlendValuesUpdateLimit GetParameterUpdateLimit(int localParameterIndex) => parameter_UpdateLimit;
        public override void GetParameterUpdateNormalizedTimeRange(int localParameterIndex, out float timeMin, out float timeMax)
        {
            timeMin = parameter_UpdateNormalizedTimeRange.x;
            timeMax = parameter_UpdateNormalizedTimeRange.y;
        }
        public override float GetBiasedParameterValue(IAnimationLayer layer, int parameterIndex, float defaultValue, bool updateParameter, out bool appliedBias)
        {
            appliedBias = false;

            if (parameterIndex == this.parameterIndex && parameter_UpdateLimit != BlendValuesUpdateLimit.None) 
            {
                appliedBias = true;
                return lastParameterValue;
            }

            return base.GetBiasedParameterValue(layer, parameterIndex, defaultValue, updateParameter, out appliedBias);
        }

        public override void RemapParameterIndices(IAnimationLayer layer, Dictionary<int, int> remapper, bool invalidateNonRemappedIndices = false)
        {

            if (remapper == null) return;

            base.RemapParameterIndices(layer, remapper, invalidateNonRemappedIndices);

            if (ParameterIndex >= 0)
            {

                int origIndex = ParameterIndex;
                if (remapper.TryGetValue(ParameterIndex, out int parameterIndex_)) parameterIndex = parameterIndex_; else if (invalidateNonRemappedIndices) 
                { 
                    parameterIndex = -1;
#if UNITY_EDITOR
                    Debug.LogError($"Failed to remap parameter index {origIndex} for {GetType().Name} '{name}'");
#else
                    swole.LogError($"Failed to remap parameter index {origIndex} for {GetType().Name} '{name}'");
#endif
                }

            }

            if (motionFields != null)
            {

                foreach (var field in motionFields)
                {

                    var controller = layer.GetMotionController(field.controllerIndex);
                    if (controller == null) continue;

                    controller.RemapParameterIndices(layer, remapper, invalidateNonRemappedIndices);

                }

            }

        }

        public override void Initialize(IAnimationLayer layer, List<AvatarMaskUsage> masks, IAnimationMotionController parent = null)
        {
            base.Initialize(layer, masks, parent);

            if (motionFields != null)
            {
                bool needsToBeSorted = false;
                float max = float.MinValue;
                for(int a = 0; a < motionFields.Length; a++)
                {
                    var mf = motionFields[a];
                    if (mf.threshold < max)
                    {
                        needsToBeSorted = true;
                        break;
                    }

                    max = mf.threshold;
                }

                if (needsToBeSorted)
                {
                    List<MotionField1D> sortedFields = new List<MotionField1D>(motionFields);
                    sortedFields.Sort((MotionField1D mfA, MotionField1D mfB) => Math.Sign(mfA.threshold - mfB.threshold));
                    motionFields = sortedFields.ToArray();
                    sortedFields.Clear();
                }
            }
        }

        [SerializeField]
        public MotionField1D[] motionFields;
        public IMotionField1D[] MotionFields
        {
            get
            {
                if (motionFields == null) return null;
                var array = new IMotionField1D[motionFields.Length];
                for (int a = 0; a < motionFields.Length; a++) array[a] = motionFields[a];
                return array;
            }
            set
            {
                if (value == null)
                {
                    motionFields = null;
                    return;
                }
                motionFields = new MotionField1D[value.Length];
                for (int a = 0; a < value.Length; a++)
                {
                    var mf = value[a];
                    if (mf is MotionField1D mf1d)
                    {
                        motionFields[a] = mf1d;
                    }
                    else
                    {
                        motionFields[a] = new MotionField1D() { speed = mf.Speed, threshold = mf.Threshold, controllerIndex = mf.ControllerIndex };
                    }
                }
            }
        }
        public override IMotionField[] BaseMotionFields => motionFields;

        public override void UpdateBlendValues(IAnimationLayer layer, float normalizedTime, float deltaTime)
        {

            if (layer == null || layer.Animator == null || motionFields == null) return;

            float thresholdPosition = FetchParameterValue(0, layer, normalizedTime, normalizedTime + deltaTime);
            firstProgressCallFlag = true;

            int minIndex, maxIndex;
            minIndex = maxIndex = 0;
            for (int a = 0; a < motionFields.Length; a++)
            {

                var motionField = motionFields[a];
                var controller = layer.GetMotionController(motionField.controllerIndex);
                if (controller == null) continue;

                if (motionField.threshold > thresholdPosition || a == motionFields.Length - 1)
                {

                    minIndex = a - 1;
                    maxIndex = a;
                    if (minIndex < 0) minIndex = 0;

                    if (minIndex == maxIndex)
                    {

                        controller.SetWeight(1);
                        controller.SyncWeight(layer);

                        if (controller is IBlendTree blendTree) blendTree.UpdateBlendValues(layer, blendTree.GetNormalizedTime(layer), deltaTime);  
                    }
                    else
                    {

                        var prevMotionField = motionFields[minIndex];
                        float interp = math.saturate((thresholdPosition - prevMotionField.threshold) / (motionField.threshold - prevMotionField.threshold));
                        
                        var minController = layer.GetMotionControllerUnsafe(prevMotionField.controllerIndex);
                        var maxController = layer.GetMotionControllerUnsafe(motionField.controllerIndex); 

                        minController.SetWeight(1 - interp);
                        minController.SyncWeight(layer);
                        maxController.SetWeight(interp);
                        maxController.SyncWeight(layer);

                        if (minController is IBlendTree prevBlendTree) prevBlendTree.UpdateBlendValues(layer, prevBlendTree.GetNormalizedTime(layer), deltaTime); 
                        if (maxController is IBlendTree blendTree) blendTree.UpdateBlendValues(layer, blendTree.GetNormalizedTime(layer), deltaTime);  
                    }

                    break;
                     
                }
                
                controller.SetWeight(0);
                controller.SyncWeight(layer);
            }

            for (int a = maxIndex + 1; a < motionFields.Length; a++)
            {
                var controller = layer.GetMotionControllerUnsafe(motionFields[a].controllerIndex);
                if (controller != null) 
                { 
                    controller.SetWeight(0);
                    controller.SyncWeight(layer);
                }
            }

            /*string debug = "";
            for (int a = 0; a < motionFields.Length; a++)
            {
                var controller = layer.GetMotionControllerUnsafe(motionFields[a].controllerIndex);
                if (controller != null)
                {
                    debug = $"{debug}, ({controller.Name}:{controller.GetWeight()})"; 
                }
            }*/
            //if (name.Contains("test")) Debug.Log(debug);

        }

    }

    [Serializable]
    public class BlendTree2D : BlendTree, IBlendTree2D
    {

        #region Serialization

        [Serializable]
        public struct Serialized : ISerializableContainer<BlendTree2D, BlendTree2D.Serialized>
        {

            public string name;
            public string SerializedName => name;

            public string avatarMaskPath;
            public WeightedAvatarMask embeddedAvatarMask;
            public bool invertAvatarMask;

            public float baseSpeed;
            public int speedMultiplierParameter;
            public bool normalizeDurations;
            public float normalizedDuration;

            public bool waitForLoops;
            public bool forceSyncOnProgress;

            public bool useDedicatedChildForTime;
            public int timeDedicatedChildIndex;

            public int parameterIndexX;
            public int parameterIndexY;
            public MotionField2D[] motionFields;

            public BlendValuesUpdateLimit parameterX_UpdateLimit;
            public float2 parameterX_UpdateNormalizedTimeRange;

            public BlendValuesUpdateLimit parameterY_UpdateLimit;
            public float2 parameterY_UpdateNormalizedTimeRange;

            public string AsJSON(bool prettyPrint = false) => swole.Engine.ToJson(this, prettyPrint);

            public object AsNonserializableObject(PackageInfo packageInfo = default) => AsOriginalType(packageInfo);
            public BlendTree2D AsOriginalType(PackageInfo packageInfo = default)
            {
                var anim = new BlendTree2D(name, parameterIndexX, parameterIndexY, motionFields);

                if (!string.IsNullOrWhiteSpace(avatarMaskPath))
                {
                    anim.avatarMask = AnimationLibrary.FindAvatarMask(packageInfo, avatarMaskPath);
                    if (anim.avatarMask == null) anim.avatarMask = AnimationLibrary.FindAvatarMask(avatarMaskPath);
                }
                else
                {
                    anim.avatarMask = embeddedAvatarMask;
                }
                anim.invertAvatarMask = invertAvatarMask;

                anim.baseSpeed = baseSpeed;
                anim.speedMultiplierParameter = speedMultiplierParameter;
                anim.normalizeDurations = normalizeDurations;
                anim.normalizedDuration = normalizedDuration;
                anim.waitForLoops = waitForLoops;
                anim.forceSyncOnProgress = forceSyncOnProgress;

                anim.parameterX_UpdateLimit = parameterX_UpdateLimit;
                anim.parameterX_UpdateNormalizedTimeRange = parameterX_UpdateNormalizedTimeRange;
                anim.parameterY_UpdateLimit = parameterY_UpdateLimit;
                anim.parameterY_UpdateNormalizedTimeRange = parameterY_UpdateNormalizedTimeRange;

                anim.useDedicatedChildForTime = useDedicatedChildForTime;
                anim.timeDedicatedChildIndex = timeDedicatedChildIndex;

                return anim;
            }

            public static implicit operator Serialized(BlendTree2D inst)
            {
                Serialized s = new Serialized();

                s.name = inst.name;

                if (inst.AvatarMask != null)
                {
                    if (inst.AvatarMask.PackageInfo.NameIsValid)
                    {
                        s.avatarMaskPath = inst.AvatarMask.PackageInfo.ConvertToAssetPath(inst.avatarMask.Name);
                    }
                    else
                    {
                        s.embeddedAvatarMask = inst.AvatarMask;
                    }
                }
                s.invertAvatarMask = inst.invertAvatarMask;

                s.baseSpeed = inst.baseSpeed;
                s.speedMultiplierParameter = inst.speedMultiplierParameter;
                s.normalizeDurations = inst.normalizeDurations;
                s.normalizedDuration = inst.normalizedDuration;
                s.waitForLoops = inst.waitForLoops;
                s.forceSyncOnProgress = inst.forceSyncOnProgress;

                s.parameterIndexX = inst.parameterIndexX;
                s.parameterIndexY = inst.parameterIndexY;
                s.motionFields = inst.motionFields;

                s.parameterX_UpdateLimit = inst.parameterX_UpdateLimit;
                s.parameterX_UpdateNormalizedTimeRange = inst.parameterX_UpdateNormalizedTimeRange;
                s.parameterY_UpdateLimit = inst.parameterY_UpdateLimit;
                s.parameterY_UpdateNormalizedTimeRange = inst.parameterY_UpdateNormalizedTimeRange;

                s.useDedicatedChildForTime = inst.useDedicatedChildForTime;
                s.timeDedicatedChildIndex = inst.timeDedicatedChildIndex;

                return s;
            }

        }

        #endregion

        public BlendTree2D() { }

        public BlendTree2D(string name, int parameterIndexX, int parameterIndexY, MotionField2D[] motionFields)
        {
            this.name = name;
            this.parameterIndexX = parameterIndexX;
            this.parameterIndexY = parameterIndexY;
            this.motionFields = motionFields;
        }

        public override object Clone() => Duplicate();
        public BlendTree2D Duplicate()
        {

            var clone = new BlendTree2D();

            CustomMotionController.CloneBase(this, clone);

            clone.parameterIndexX = parameterIndexX;
            clone.parameterIndexY = parameterIndexY;
            clone.motionFields = motionFields == null ? null : (MotionField2D[])motionFields.DeepClone();

            clone.normalizeDurations = normalizeDurations;
            clone.normalizedDuration = normalizedDuration;
            clone.waitForLoops = waitForLoops;
            clone.forceSyncOnProgress = forceSyncOnProgress;

            clone.parameterX_UpdateLimit = parameterX_UpdateLimit;
            clone.parameterX_UpdateNormalizedTimeRange = parameterX_UpdateNormalizedTimeRange;
            clone.parameterY_UpdateLimit = parameterY_UpdateLimit;
            clone.parameterY_UpdateNormalizedTimeRange = parameterY_UpdateNormalizedTimeRange;
             
            clone.useDedicatedChildForTime = useDedicatedChildForTime;
            clone.timeDedicatedChildIndex = timeDedicatedChildIndex;

            return clone;

        }

        public override void GetChildIndices(List<int> indices, bool onlyAddIfNotPresent)
        {

            if (motionFields == null || indices == null) return;

            if (onlyAddIfNotPresent)
            {

                foreach (var field in motionFields) if (!indices.Contains(field.controllerIndex)) indices.Add(field.controllerIndex);

            }
            else
            {

                foreach (var field in motionFields) indices.Add(field.controllerIndex);

            }

        }

        public override void RemapChildIndices(Dictionary<int, int> remapper, bool invalidateNonRemappedIndices = false) => RemapChildIndices(remapper, 0, invalidateNonRemappedIndices);
        public override void RemapChildIndices(Dictionary<int, int> remapper, int minIndex, bool invalidateNonRemappedIndices = false)
        {

            if (motionFields == null || remapper == null) return;
            motionFields = (MotionField2D[])motionFields.Clone();
            
            for (int a = minIndex; a < motionFields.Length; a++)
            {

                var field = motionFields[a].Duplicate();

                if (remapper.TryGetValue(field.controllerIndex, out int newIndex))
                {
                    field.controllerIndex = newIndex;
                }
                else if (invalidateNonRemappedIndices)
                {
                    field.controllerIndex = -1;
#if UNITY_EDITOR
                    Debug.LogError($"Failed to remap motion field [{a}] controller index for {GetType().Name} '{name}'");
#else
                    swole.LogError($"Failed to remap motion field [{a}] controller index for {GetType().Name} '{name}'");
#endif
                }

                motionFields[a] = field;

            }

        }

        public override int GetChildCount() => motionFields == null ? 0 : motionFields.Length;
        public override int GetChildIndex(int childIndex) => motionFields == null ? -1 : motionFields[childIndex].controllerIndex;
        public override void SetChildIndex(int childIndex, int controllerIndex)
        {
            if (motionFields == null) return;

            motionFields[childIndex].controllerIndex = controllerIndex; 
        }

        [SerializeField]
        public int parameterIndexX;
        public int ParameterIndexX
        {
            get => parameterIndexX;
            set => parameterIndexX = value;
        }

        [NonSerialized]
        private float lastParameterValX;

        [SerializeField]
        public int parameterIndexY;
        public int ParameterIndexY
        {
            get => parameterIndexY;
            set => parameterIndexY = value;
        }

        [NonSerialized]
        private float lastParameterValY;

        [SerializeField]
        public BlendValuesUpdateLimit parameterX_UpdateLimit;
        [SerializeField]
        public float2 parameterX_UpdateNormalizedTimeRange = new float2(0f, 1f);

        [SerializeField]
        public BlendValuesUpdateLimit parameterY_UpdateLimit;
        [SerializeField]
        public float2 parameterY_UpdateNormalizedTimeRange = new float2(0f, 1f);

        public override int ParameterCount => 2;

        public override int GetParameterIndex(int localParameterIndex) => localParameterIndex > 0 ? ParameterIndexY : ParameterIndexX;
        public override float GetLastParameterValue(int localParameterIndex) => localParameterIndex > 0 ? lastParameterValY : lastParameterValX;
        public override void SetLastParameterValue(int localParameterIndex, float value)
        {
            if (localParameterIndex > 0)
            {
                lastParameterValY = value;
            }
            else
            {
                lastParameterValX = value;
            }
        }
        public override BlendValuesUpdateLimit GetParameterUpdateLimit(int localParameterIndex) => localParameterIndex > 0 ? parameterY_UpdateLimit : parameterX_UpdateLimit;
        public override void GetParameterUpdateNormalizedTimeRange(int localParameterIndex, out float timeMin, out float timeMax) 
        { 
            if (localParameterIndex > 0)
            {
                timeMin = parameterY_UpdateNormalizedTimeRange.x;
                timeMax = parameterY_UpdateNormalizedTimeRange.y;
            } 
            else
            {
                timeMin = parameterX_UpdateNormalizedTimeRange.x;
                timeMax = parameterX_UpdateNormalizedTimeRange.y;
            }
        }

        public override float GetBiasedParameterValue(IAnimationLayer layer, int parameterIndex, float defaultValue, bool updateParameter, out bool appliedBias)
        {
            appliedBias = true;

            if (parameterIndex == parameterIndexX && parameterX_UpdateLimit != BlendValuesUpdateLimit.None) return lastParameterValX;
            if (parameterIndex == parameterIndexY && parameterY_UpdateLimit != BlendValuesUpdateLimit.None) return lastParameterValY;

            appliedBias = false;

            return base.GetBiasedParameterValue(layer, parameterIndex, defaultValue, updateParameter, out appliedBias);
        }

        public override void RemapParameterIndices(IAnimationLayer layer, Dictionary<int, int> remapper, bool invalidateNonRemappedIndices = false)
        {

            if (remapper == null) return;

            base.RemapParameterIndices(layer, remapper, invalidateNonRemappedIndices);

            if (ParameterIndexX >= 0)
            {
                int origIndex = ParameterIndexX;
                if (remapper.TryGetValue(origIndex, out int parameterIndex_)) parameterIndexX = parameterIndex_; else if (invalidateNonRemappedIndices) 
                { 
                    parameterIndexX = -1;

#if UNITY_EDITOR
                    Debug.LogError($"Failed to remap parameter X index {origIndex} for {GetType().Name} '{name}'");
#else
                    swole.LogError($"Failed to remap parameter X index {origIndex} for {GetType().Name} '{name}'");
#endif
                }
            }
            if (ParameterIndexY >= 0)
            {
                int origIndex = ParameterIndexY;
                if (remapper.TryGetValue(origIndex, out int parameterIndex_)) parameterIndexY = parameterIndex_; else if (invalidateNonRemappedIndices)  
                { 
                    parameterIndexY = -1;

#if UNITY_EDITOR
                    Debug.LogError($"Failed to remap parameter Y index {origIndex} for {GetType().Name} '{name}'");
#else
                    swole.LogError($"Failed to remap parameter Y index {origIndex} for {GetType().Name} '{name}'");
#endif
                }
            }

            if (motionFields != null)
            {

                foreach (var field in motionFields)
                {

                    var controller = layer.GetMotionController(field.controllerIndex);
                    if (controller == null) continue;

                    controller.RemapParameterIndices(layer, remapper, invalidateNonRemappedIndices);

                }

            }

        }

        [SerializeField]
        public MotionField2D[] motionFields;
        public IMotionField2D[] MotionFields
        {
            get
            {
                if (motionFields == null) return null;
                var array = new IMotionField2D[motionFields.Length];
                for (int a = 0; a < motionFields.Length; a++) array[a] = motionFields[a];
                return array;
            }
            set
            {
                if (value == null)
                {
                    motionFields = null;
                    return;
                }
                motionFields = new MotionField2D[value.Length];
                for (int a = 0; a < value.Length; a++)
                {
                    var mf = value[a];
                    if (mf is MotionField2D mft)
                    {
                        motionFields[a] = mft;
                    }
                    else
                    {
                        motionFields[a] = new MotionField2D() { speed = mf.Speed, x = mf.X, y = mf.Y, controllerIndex = mf.ControllerIndex };
                    }
                }
            }
        }
        public override IMotionField[] BaseMotionFields => motionFields;

        [Serializable]
        protected struct MotionVertex
        {
            public int motionFieldIndex;
            public float x;
            public float y;

            public static implicit operator double2(MotionVertex v) => new double2(v.x, v.y);
            public static implicit operator float2(MotionVertex v) => new float2(v.x, v.y);
        }
        [Serializable]
        protected struct MotionTriangle
        {
            public int i0;
            public int i1;
            public int i2;
        }
        private MotionVertex[] motionVertices;
        private MotionTriangle[] motionTriangles;
#if UNITY_EDITOR
        [NonSerialized]
        private Vector2 debugOffset;
#endif
        public void InitializeMotionMesh()
        {
            if (motionFields == null) return;

            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;

            List<MotionVertex> vertices = new List<MotionVertex>();
            for (int a = 0; a < motionFields.Length; a++)
            {
                var mf = motionFields[a];

                minX = Mathf.Min(minX, mf.X);
                maxX = Mathf.Max(maxX, mf.X);

                minY = Mathf.Min(minY, mf.Y);
                maxY = Mathf.Max(maxY, mf.Y);

                vertices.Add(new MotionVertex()
                {
                    motionFieldIndex = a,
                    x = mf.X,
                    y = mf.Y
                });
            }

            if (minX == maxX)
            {
                if (minX != 0)
                {
                    int count = vertices.Count;
                    for (int a = 0; a < count; a++)
                    {
                        var vertex = vertices[a];
                        vertex.x = 0;
                        vertices.Add(vertex);
                    }
                }
                else
                {
                    int count = vertices.Count;
                    for (int a = 0; a < count; a++)
                    {
                        var vertex = vertices[a];
                        vertex.x = 1;
                        vertices.Add(vertex);
                    }
                }
            }
            if (minY == maxY)
            {
                if (minY != 0)
                {
                    int count = vertices.Count;
                    for (int a = 0; a < count; a++)
                    {
                        var vertex = vertices[a];
                        vertex.y = 0;
                        vertices.Add(vertex);
                    }
                }
                else
                {
                    int count = vertices.Count;
                    for (int a = 0; a < count; a++)
                    {
                        var vertex = vertices[a];
                        vertex.y = 1;
                        vertices.Add(vertex);
                    }
                }
            }

            motionVertices = vertices.ToArray();

            using (var positions = new NativeArray<double2>(motionVertices.Length, Allocator.Persistent))
            {
                var positions_ = positions;
                for (int a = 0; a < motionVertices.Length; a++) positions_[a] = motionVertices[a];

                using (var triangulator = new Triangulator(Allocator.Persistent)
                {
                    Input = { Positions = positions }
                })
                {

                    triangulator.Run();

                    var triangles = triangulator.Output.Triangles;
                    motionTriangles = new MotionTriangle[triangles.Length / 3];
                    for(int a = 0; a < triangles.Length; a += 3)
                    {
                        int b = a / 3;

                        motionTriangles[b] = new MotionTriangle()
                        {
                            i0 = triangles[a],
                            i1 = triangles[a + 1],
                            i2 = triangles[a + 2]
                        };
                    }
                }
            }

#if UNITY_EDITOR
            debugOffset = UnityEngine.Random.insideUnitCircle;
            foreach(var tri in motionTriangles)
            {
                var col = UnityEngine.Random.ColorHSV(0, 1, 0.4f, 1, 0.7f, 1);

                Debug.DrawLine(((Vector2)(float2)motionVertices[tri.i0]) + debugOffset, (Vector2)(float2)motionVertices[tri.i1] + debugOffset, col, 100);
                Debug.DrawLine(((Vector2)(float2)motionVertices[tri.i1]) + debugOffset, (Vector2)(float2)motionVertices[tri.i2] + debugOffset, col, 100);
                Debug.DrawLine(((Vector2)(float2)motionVertices[tri.i2]) + debugOffset, (Vector2)(float2)motionVertices[tri.i0] + debugOffset, col, 100); 
            }
#endif
        }

        public override void UpdateBlendValues(IAnimationLayer layer, float normalizedTime, float deltaTime)
        {

            if (layer == null || layer.Animator == null || motionFields == null) return;

            if (motionTriangles == null) 
            { 
                InitializeMotionMesh();
                if (motionTriangles == null) return;
            }

            float nextNormalizedTime = normalizedTime + deltaTime;
            float posX = FetchParameterValue(0, layer, normalizedTime, nextNormalizedTime);
            float posY = FetchParameterValue(1, layer, normalizedTime, nextNormalizedTime); 
            firstProgressCallFlag = true;

            float2 point = new float2(posX, posY);
#if UNITY_EDITOR
            Debug.DrawRay(((Vector2)point) + debugOffset, Vector3.forward, Color.blue, 1f);  
#endif

            bool hasTri = false;
            MotionVertex closestV0 = default;
            MotionVertex closestV1 = default;
            MotionVertex closestV2 = default;
            float3 closestCoords = float3.zero;
            float minDist = float.MaxValue;
            for(int a = 0; a < motionTriangles.Length; a++)
            {
                var tri = motionTriangles[a];

                var v0 = motionVertices[tri.i0];
                var v1 = motionVertices[tri.i1];
                var v2 = motionVertices[tri.i2];

                var baryCoords = Maths.BarycentricCoords2D(point, v0, v1, v2);
                if (Maths.IsInTriangle2D(baryCoords))
                {
                    hasTri = true;
                    closestV0 = v0;
                    closestV1 = v1;
                    closestV2 = v2;
                    closestCoords = baryCoords;
                    break;
                } 
                else
                {
                    float3 dist = math.abs(math.select(baryCoords, baryCoords - 1f, baryCoords > 0));

                    float distSq = math.lengthsq(dist);
                    if (distSq < minDist)
                    {
                        minDist = distSq; 
                        
                        hasTri = true;
                        closestV0 = v0;
                        closestV1 = v1;
                        closestV2 = v2;
                        closestCoords = baryCoords;
                    }
                }
            }

            if (hasTri)
            {
                for (int a = 0; a < motionFields.Length; a++) // zero out weights first
                {
                    var controller = layer.GetMotionControllerUnsafe(motionFields[a].controllerIndex);
                    if (controller != null) 
                    { 
                        controller.SetWeight(0);
                        controller.SyncWeight(layer);
                    }
                    
                }

                closestCoords = math.max(closestCoords, new float3(0, 0, 0));
                closestCoords = math.min(closestCoords, new float3(1, 1, 1));  

                void UpdateMotionField(int index, float weight)
                {
                    var motionField = motionFields[index];
                    var controller = layer.GetMotionController(motionField.controllerIndex);
                    if (controller == null) return;

                    controller.SetWeight(weight + controller.GetBaseWeight()); // sum with existing weight incase the controller appears in multiple motion fields
                    controller.SyncWeight(layer);

                    if (controller is IBlendTree blendTree) blendTree.UpdateBlendValues(layer, blendTree.GetNormalizedTime(layer), deltaTime);
                }

                float totalWeight = math.csum(math.abs(closestCoords));
                if (totalWeight > 0f) closestCoords = closestCoords / totalWeight; 

                UpdateMotionField(closestV0.motionFieldIndex, closestCoords.x); 
                UpdateMotionField(closestV1.motionFieldIndex, closestCoords.y);
                UpdateMotionField(closestV2.motionFieldIndex, closestCoords.z);

                //Debug.Log($"PARAMS: {point}"); 
                //Debug.Log($"{name} -> ({layer.GetMotionControllerUnsafe(motionFields[closestV0.motionFieldIndex].controllerIndex).Name}:{closestCoords.x})({layer.GetMotionControllerUnsafe(motionFields[closestV1.motionFieldIndex].controllerIndex).Name}:{closestCoords.y})({layer.GetMotionControllerUnsafe(motionFields[closestV2.motionFieldIndex].controllerIndex).Name}:{closestCoords.z})"); 
            }
            else
            {
                for (int a = 0; a < motionFields.Length; a++)
                {
                    var controller = layer.GetMotionControllerUnsafe(motionFields[a].controllerIndex);
                    if (controller != null) 
                    { 
                        controller.SetWeight(0);
                        controller.SyncWeight(layer);
                    }
                }
            }

        }

    }
}

#endif