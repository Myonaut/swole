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

            m_weight = 1f;

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

        public virtual float GetSpeed(IAnimationLayer layer)
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

        public abstract float GetTime(IAnimationLayer layer, float addTime = 0f);
        public virtual float GetNormalizedTime(IAnimationLayer layer, float addTime = 0f)
        {
            float length = GetDuration(layer);
            if (length <= 0f) return 0f;

            return GetTime(layer, addTime) / length;
        }

        public abstract float GetPreviousTime(IAnimationLayer layer);
        public virtual float GetPreviousNormalizedTime(IAnimationLayer layer)
        {
            float length = GetDuration(layer);
            if (length <= 0f) return 0f;

            return GetPreviousTime(layer) / length;
        }

        public abstract void ForceSetPreviousTime(IAnimationLayer layer, float time);
        public abstract void ForceSetPreviousNormalizedTime(IAnimationLayer layer, float normalizedTime);

        public abstract void SetTime(IAnimationLayer layer, float time, bool resetFlags = true);

        public abstract void SetNormalizedTime(IAnimationLayer layer, float normalizedTime, bool resetFlags = true);

        public abstract void Progress(IAnimationLayer layer, float deltaTime, ref JobHandle jobHandle, bool useMultithreading = true, bool isFinal = false, bool previousWasFinal = false, bool canLoop = true);

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

        public virtual void RemapParameterIndices(IAnimationLayer layer, Dictionary<int, int> remapper, bool invalidateNonRemappedIndices = false, bool remapChildren = true)
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

        public virtual float GetCurrentBoneHeight(IAnimationLayer layer, string boneName) => GetBoneHeight(layer, boneName, 0f);
        public float GetBoneHeight(IAnimationLayer layer, string boneName, float timeOffset) => GetBoneHeight(layer, boneName, timeOffset, out _);
        public abstract float GetBoneHeight(IAnimationLayer layer, string boneName, float timeOffset, out bool foundCurve);

        public float GetBoneHeightAtTime(IAnimationLayer layer, string boneName, float time) => GetBoneHeightAtTime(layer, boneName, time, out _);
        public abstract float GetBoneHeightAtTime(IAnimationLayer layer, string boneName, float time, out bool foundCurve);

        /// <summary>
        /// Samples where a bone would be, relative to <paramref name="space"/>, after this controller advances <paramref name="timeOffset"/> seconds.
        /// Uses the same per-bone curve application as playback (masks, additive/blend modes, mixes). Only this layer's controller tree is sampled.
        /// Parameters use their current values, unless their layer parameter index is a key in <paramref name="parameterOverrides"/>.
        /// </summary>
        public virtual bool TrySampleBonePosition(IAnimationLayer layer, Transform bone, Transform space, float timeOffset, out Vector3 position, IReadOnlyDictionary<int, float> parameterOverrides = null)
        {
            position = default;
            if (bone == null || layer == null || layer.Animator is not CustomAnimator animator) return false;

            var pose = new CustomAnimator.SampledPose(animator);
            if (!SampleInto(layer, pose, timeOffset, 1f, parameterOverrides)) return false;

            position = pose.GetMatrix(bone, space).GetPosition();
            return true;
        }

        /// <summary>
        /// Applies this controller's animation, as it would be after advancing <paramref name="timeOffset"/> seconds, to an offline pose. Returns true if anything was sampled.
        /// <paramref name="weight"/> is the accumulated weight from parent controllers (used in place of the live weight, so no controller state is modified).
        /// </summary>
        public virtual bool SampleInto(IAnimationLayer layer, CustomAnimator.SampledPose pose, float timeOffset, float weight, IReadOnlyDictionary<int, float> parameterOverrides = null) => false;

        /// <summary>Gets a layer parameter's value for sampling: the override if one exists for <paramref name="parameterIndex"/>, otherwise the parameter's current value.</summary>
        public static bool TryGetSampleParameterValue(IAnimationLayer layer, int parameterIndex, IReadOnlyDictionary<int, float> parameterOverrides, out float value)
        {
            value = 0f;
            if (parameterIndex < 0) return false;
            if (parameterOverrides != null && parameterOverrides.TryGetValue(parameterIndex, out value)) return true;

            var p = layer?.GetParameter(parameterIndex);
            if (p == null) return false;
            value = p.Value;
            return true;
        }

        public static bool HasParameterOverride(int parameterIndex, IReadOnlyDictionary<int, float> parameterOverrides) => parameterIndex >= 0 && parameterOverrides != null && parameterOverrides.ContainsKey(parameterIndex);

        /// <summary>Mirrors <see cref="GetSpeed"/> for sampling, honoring parameter overrides.</summary>
        public virtual float GetSampleSpeed(IAnimationLayer layer, IReadOnlyDictionary<int, float> parameterOverrides)
        {
            if (parameterOverrides == null || parameterOverrides.Count == 0) return GetSpeed(layer);

            float speed = baseSpeed;
            if (Parent is CustomMotionController parent) speed = speed * parent.GetSampleSpeed(layer, parameterOverrides);
            else if (Parent != null) speed = speed * Parent.GetSpeed(layer);

            if (layer == null) return speed;
            if (TryGetSampleParameterValue(layer, speedMultiplierParameter, parameterOverrides, out float mul)) speed = speed * mul;

            return speed;
        }

        /// <summary>Mirrors <see cref="MotionPart.GetMix"/> for sampling, honoring parameter overrides.</summary>
        public static float GetSampleMix(MotionPart part, IAnimationLayer layer, IReadOnlyDictionary<int, float> parameterOverrides)
        {
            if (!HasParameterOverride(part.mixParameterIndex, parameterOverrides)) return part.GetMix(layer);

            float paramVal = parameterOverrides[part.mixParameterIndex];
            if (part.mixParameterMappingMin != part.mixParameterMappingMax)
            {
                if (part.clampMixParameter) paramVal = Math.Clamp(paramVal, part.mixParameterMappingMin, part.mixParameterMappingMax);
                paramVal = (paramVal - part.mixParameterMappingMin) / (part.mixParameterMappingMax - part.mixParameterMappingMin);
            }

            return part.mix * paramVal;
        }

        public abstract bool AffectsTransform(IAnimationLayer layer, string transformName);

        public abstract void NonAdditivePrepass(bool useMultithreading, IAnimationLayer layer);

        public abstract void ApplyPropertyStates(IAnimationLayer layer);

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

        public AnimationReference(string name, IAnimationAsset animation, AnimationLoopMode loopMode, float normalizedTimeStart = 0f)
        {
            this.name = name;
            if (animation is CustomAnimationAsset asset) this.animationAsset = asset; else this.animation = animation;
            this.normalizedTimeStart = normalizedTimeStart;
            this.loopMode = loopMode;
        }

        public override object Clone() => Duplicate();
        public AnimationReference Duplicate()
        {

            var clone = new AnimationReference();

            CustomMotionController.CloneBase(this, clone);

            clone.normalizedTimeStart = normalizedTimeStart;
            clone.loopMode = loopMode;
            clone.animation = animation;
            clone.animationAsset = animationAsset;
            clone.timeOverrideParameter = timeOverrideParameter;
            clone.useNormalizedTimeForOverriding = useNormalizedTimeForOverriding;

            clone.remapTimeOverrideParameter = remapTimeOverrideParameter;
            clone.remapTimeOverrideParameterSrcMin = remapTimeOverrideParameterSrcMin;
            clone.remapTimeOverrideParameterSrcMax = remapTimeOverrideParameterSrcMax;
            clone.remapTimeOverrideParameterDstMin = remapTimeOverrideParameterDstMin;
            clone.remapTimeOverrideParameterDstMax = remapTimeOverrideParameterDstMax;
            clone.remapTimeOverrideParameterClampMin = remapTimeOverrideParameterClampMin;
            clone.remapTimeOverrideParameterClampMax = remapTimeOverrideParameterClampMax; 

            return clone;

        }

        [SerializeField]
        public float normalizedTimeStart;
        public float NormalizedTimeStart
        {
            get => normalizedTimeStart;
            set => normalizedTimeStart = value;
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

        [SerializeField]
        public bool useNormalizedTimeForOverriding = false;
        public bool UseNormalizedTimeForOverriding
        {
            get => useNormalizedTimeForOverriding;
            set => useNormalizedTimeForOverriding = value; 
        }

        [SerializeField]
        public bool remapTimeOverrideParameter = false;
        public bool RemapTimeOverrideParameter
        {
            get => remapTimeOverrideParameter;
            set => remapTimeOverrideParameter = value;
        }

        [SerializeField]
        public float remapTimeOverrideParameterSrcMin = 0f;
        public float RemapTimeOverrideParameterSrcMin
        {
            get => remapTimeOverrideParameterSrcMin;
            set => remapTimeOverrideParameterSrcMin = value;
        }

        [SerializeField]
        public float remapTimeOverrideParameterSrcMax = 1f;
        public float RemapTimeOverrideParameterSrcMax
        {
            get => remapTimeOverrideParameterSrcMax;
            set => remapTimeOverrideParameterSrcMax = value;
        }

        [SerializeField]
        public float remapTimeOverrideParameterDstMin = 0f;
        public float RemapTimeOverrideParameterDstMin
        {
            get => remapTimeOverrideParameterDstMin;
            set => remapTimeOverrideParameterDstMin = value;
        }

        [SerializeField]
        public float remapTimeOverrideParameterDstMax = 1f;
        public float RemapTimeOverrideParameterDstMax
        {
            get => remapTimeOverrideParameterDstMax;
            set => remapTimeOverrideParameterDstMax = value;
        }

        [SerializeField]
        public bool remapTimeOverrideParameterClampMin = false;
        public bool RemapTimeOverrideParameterClampMin
        {
            get => remapTimeOverrideParameterClampMin;
            set => remapTimeOverrideParameterClampMin = value;
        }

        [SerializeField]
        public bool remapTimeOverrideParameterClampMax = false;
        public bool RemapTimeOverrideParameterClampMax
        {
            get => remapTimeOverrideParameterClampMax;
            set => remapTimeOverrideParameterClampMax = value;
        }

        public override void RemapParameterIndices(IAnimationLayer layer, Dictionary<int, int> remapper, bool invalidateNonRemappedIndices = false, bool remapChildren = true)
        {

            base.RemapParameterIndices(layer, remapper, invalidateNonRemappedIndices, remapChildren);

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
                m_animationPlayer.SetTime(normalizedTimeStart * m_animationPlayer.LengthInSeconds, true);

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
            AnimationPlayer.Time = normalizedTimeStart * m_animationPlayer.LengthInSeconds;  
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

            return Mathf.Approximately(speed, 0f) ? 0f : (AnimationPlayer.LengthInSeconds / math.abs(speed)); 

        }

        public override float GetTime(IAnimationLayer layer, float addTime = 0)
        {

            if (AnimationPlayer == null) return 0f;

            var apTyped = AnimationPlayerTyped;
            return AnimationPlayer.Time + addTime * GetSpeed(layer) * (apTyped == null ? 1f : apTyped.InternalSpeedMultiplier);  

        }

        public override float GetPreviousTime(IAnimationLayer layer)
        {

            if (AnimationPlayer is not CustomAnimation.Player cap) return 0f;

            return cap.PreviousTime;

        }

        public override void ForceSetPreviousTime(IAnimationLayer layer, float time)
        {
            if (AnimationPlayer is not CustomAnimation.Player cap) return; 
            cap.ForceSetPreviousTime(time);
        }
        public override void ForceSetPreviousNormalizedTime(IAnimationLayer layer, float normalizedTime)
        {
            if (AnimationPlayer is not CustomAnimation.Player cap) return;
            cap.ForceSetPreviousTime(normalizedTime * cap.LengthInSeconds);
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

        public override void Progress(IAnimationLayer layer, float deltaTime, ref JobHandle jobHandle, bool useMultithreading = true, bool isFinal = false, bool previousWasFinal = false, bool canLoop = true)
        {

            if (AnimationPlayer is not CustomAnimation.Player cap) return;

            cap.Speed = GetSpeed(layer);
            //cap.Mix = GetWeight(); // shouldn't be necessary because SetWeight updates Mix

            if (timeOverrideParameter >= 0)
            {
                cap.OverrideTime = true;

                var p = layer.GetParameter(timeOverrideParameter);
                if (p != null)
                {
                    float val = p.UpdateAndGetValue();
                    if (remapTimeOverrideParameter)
                    {
                        val = math.remap(remapTimeOverrideParameterSrcMin, remapTimeOverrideParameterSrcMax, remapTimeOverrideParameterDstMin, remapTimeOverrideParameterDstMax, val);
                        if (remapTimeOverrideParameterClampMin) val = math.max(val, remapTimeOverrideParameterDstMin);
                        if (remapTimeOverrideParameterClampMax) val = math.min(val, remapTimeOverrideParameterDstMax); 
                    }
                    cap.TimeOverride = val * (useNormalizedTimeForOverriding ? cap.LengthInSeconds : 1f); 
                }
            }

            jobHandle = cap.Progress(deltaTime, layer.Mix, jobHandle, useMultithreading, isFinal, previousWasFinal, canLoop);
            //Debug.Log($"{name}: {layer.Mix} {cap.Time} {cap.Speed}"); 
        }

        public override bool HasAnimationPlayer(IAnimationLayer layer)
        {

            return AnimationPlayer != null;

        }


        public override float GetBoneHeight(IAnimationLayer layer, string boneName, float timeOffset, out bool foundCurve)
        {
            foundCurve = false;

            if (AnimationPlayer != null)
            {
                return GetBoneHeightAtTime(layer, boneName, GetTime(layer, timeOffset), out foundCurve) * layer.Mix * AnimationPlayer.Mix;
            }

            return 0f;
        }
        public override float GetBoneHeightAtTime(IAnimationLayer layer, string boneName, float time, out bool foundCurve)
        {
            foundCurve = false;

            var anim = Animation;
            if (anim is CustomAnimation canim)
            {
                if (canim.TryGetBoneHeightCurve(boneName, out var curve))
                {
                    foundCurve = true;
                    return curve.Evaluate(time) * layer.Mix * AnimationPlayer.Mix; 
                }
            }

            return 0f;
        }

        public override bool SampleInto(IAnimationLayer layer, CustomAnimator.SampledPose pose, float timeOffset, float weight, IReadOnlyDictionary<int, float> parameterOverrides = null)
        {
            if (pose == null || AnimationPlayer is not CustomAnimation.Player cap) return false;

            float sampleTime;
            if (HasParameterOverride(timeOverrideParameter, parameterOverrides))
            {
                float val = parameterOverrides[timeOverrideParameter];
                if (remapTimeOverrideParameter)
                {
                    val = math.remap(remapTimeOverrideParameterSrcMin, remapTimeOverrideParameterSrcMax, remapTimeOverrideParameterDstMin, remapTimeOverrideParameterDstMax, val);
                    if (remapTimeOverrideParameterClampMin) val = math.max(val, remapTimeOverrideParameterDstMin);
                    if (remapTimeOverrideParameterClampMax) val = math.min(val, remapTimeOverrideParameterDstMax);
                }
                sampleTime = cap.GetLoopedTimeSample(val * (useNormalizedTimeForOverriding ? cap.LengthInSeconds : 1f));
            }
            else
            {
                sampleTime = cap.PredictTime(timeOffset, GetSampleSpeed(layer, parameterOverrides));
            }

            cap.SampleInto(pose, sampleTime, weight, layer.Mix);
            return true;
        }

        public override bool AffectsTransform(IAnimationLayer layer, string transformName)
        {
            var anim = IsUsingAsset ? AnimationAsset : Animation;
            if (anim is CustomAnimation canim)
            {
                return canim.AffectsTransform(transformName); 
            }

            return false;
        }

        public override void NonAdditivePrepass(bool useMultithreading, IAnimationLayer layer)
        {
            if (AnimationPlayer is not CustomAnimation.Player cap) return;

            cap.NonAdditivePrepass(useMultithreading, layer);
        }

        public override void ApplyPropertyStates(IAnimationLayer layer)
        {
            var player = AnimationPlayer;
            if (player == null) return;

            player.ApplyPropertyStates();
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

            public bool useBlendSpeed;
            public bool blendSpeedIsAdditive;

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

                anim.useBlendSpeed = useBlendSpeed;
                anim.blendSpeedIsAdditive = blendSpeedIsAdditive;

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

                s.useBlendSpeed = inst.useBlendSpeed;
                s.blendSpeedIsAdditive = inst.blendSpeedIsAdditive;

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

            clone.useBlendSpeed = useBlendSpeed;
            clone.blendSpeedIsAdditive = blendSpeedIsAdditive;

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

        [SerializeField]
        public bool useBlendSpeed;
        public bool UseBlendSpeed
        {
            get => useBlendSpeed;
            set => useBlendSpeed = value;
        }

        [SerializeField]
        public bool blendSpeedIsAdditive;
        public bool BlendSpeedIsAdditive
        {
            get => blendSpeedIsAdditive;
            set => blendSpeedIsAdditive = value;
        }

        [NonSerialized]
        public float blendSpeed = 1f;

        public override float GetSpeed(IAnimationLayer layer)
        {

            float speed = base.GetSpeed(layer);

            if (useBlendSpeed) speed = speed * blendSpeed;

            return speed;

        }

        public override float GetSampleSpeed(IAnimationLayer layer, IReadOnlyDictionary<int, float> parameterOverrides)
        {
            float speed = base.GetSampleSpeed(layer, parameterOverrides);
            if (useBlendSpeed && parameterOverrides != null && parameterOverrides.Count > 0) speed = speed * blendSpeed;
            return speed;
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

                        int initialMasksCount = masks == null ? 0 : masks.Count;
                        controller.Initialize(layer, masks, this);
                        if (masks != null) masks.RemoveRange(initialMasksCount, masks.Count - initialMasksCount);


                        float speed = part.ReplaceBaseSpeed ? 1f : controller.BaseSpeed;
                        if (normalizeDurations)
                        {
                            if (normalizedDuration <= 0f)
                            {
                                speed = 0f;
                            }
                            else
                            {
                                float duration = Mathf.Approximately(speed, 0f) ? 0f : (controller.GetDuration(layer) / math.abs(speed));
                                speed = (duration / normalizedDuration) * Mathf.Sign(speed);  
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

                foreach (var part in motionParts) if (part.controllerIndex >= 0 && !indices.Contains(part.controllerIndex)) indices.Add(part.controllerIndex);

            }
            else
            {

                foreach (var part in motionParts) if (part.controllerIndex >= 0) indices.Add(part.controllerIndex);

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

        public override void GetParameterIndices(IAnimationLayer layer, List<int> indices)
        {

            if (indices == null) return;

            base.GetParameterIndices(layer, indices);

            if (ParameterCount > 0)
            {
                for (int a = 0; a < ParameterCount; a++)
                {
                    int paramIndex = GetParameterIndex(a);
                    if (paramIndex >= 0) indices.Add(paramIndex);
                }

            }

            if (BaseMotionParts != null)
            {

                foreach (var part in BaseMotionParts)
                {

                    if (part.MixParameterIndex >= 0) indices.Add(part.MixParameterIndex);

                    if (part.BlendSpeedParameterIndex >= 0) indices.Add(part.BlendSpeedParameterIndex);

                    var controller = layer.GetMotionController(part.ControllerIndex);
                    if (controller == null) continue;

                    controller.GetParameterIndices(layer, indices);

                }

            }

        }

        public override void RemapParameterIndices(IAnimationLayer layer, Dictionary<int, int> remapper, bool invalidateNonRemappedIndices = false, bool remapChildren = true)
        {

            if (remapper == null) return;

            base.RemapParameterIndices(layer, remapper, invalidateNonRemappedIndices, remapChildren);

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

                    if (part.BlendSpeedParameterIndex >= 0)
                    {

                        int origIndex = part.BlendSpeedParameterIndex;
                        if (!remapper.TryGetValue(origIndex, out part.blendSpeedParameterIndex) && invalidateNonRemappedIndices)
                        {
                            part.blendSpeedParameterIndex = -1;

#if UNITY_EDITOR
                            Debug.LogError($"Failed to remap blend speed parameter index {origIndex} for {GetType().Name} '{name}', (Part:{a})");
#else
                            swole.LogError($"Failed to remap blend speed parameter index {origIndex} for {GetType().Name} '{name}', (Part:{a})");
#endif
                        }

                    }


                    if (remapChildren)
                    {
                        var controller = layer.GetMotionController(part.controllerIndex);
                        if (controller == null) continue;

                        controller.RemapParameterIndices(layer, remapper, invalidateNonRemappedIndices, remapChildren);
                    }

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
                        motionParts[a] = new MotionPart() { replaceBaseSpeed = mf.ReplaceBaseSpeed, speed = mf.Speed, controllerIndex = mf.ControllerIndex, syncMode = mf.SyncMode, localSyncReferenceIndex = mf.LocalSyncReferenceIndex, mix = mf.Mix, mixParameterIndex = mf.MixParameterIndex, blendSpeed = mf.BlendSpeed, blendSpeedParameterIndex = mf.BlendSpeedParameterIndex };
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

            var motionParts = MotionParts;
            if (UseDedicatedChildForTime && motionParts != null && TimeDedicatedChildIndex >= 0 && TimeDedicatedChildIndex < motionParts.Length)
            {
                var dedicatedController = layer.GetMotionController(motionParts[TimeDedicatedChildIndex].ControllerIndex);
                if (dedicatedController != null) return dedicatedController.GetLoopMode(layer);
            }

            if (motionParts != null && motionParts.Length > 0)
            {
                loopMode = AnimationLoopMode.PlayOnce;
                foreach (var part in motionParts)
                {

                    var controller = layer.GetMotionController(part.ControllerIndex);
                    if (controller == null) continue;

                    var loopMode_ = controller.GetLoopMode(layer);
                    if (loopMode_ != AnimationLoopMode.PlayOnce)
                    {
                        loopMode = loopMode_;
                        break;
                    }
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

            if (MotionParts == null) return 0f;

            if (UseDedicatedChildForTime)
            {
                if (TimeDedicatedChildIndex < 0 || TimeDedicatedChildIndex >= MotionParts.Length) return 0f;

                var dedicatedChild = MotionParts[TimeDedicatedChildIndex];
                var controller = layer.GetMotionController(dedicatedChild.ControllerIndex);
                if (controller == null) return 0f;

                return controller.GetScaledDuration(layer);
            }

            float length = 0f;

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
                if (controller.GetLoopMode(layer) != AnimationLoopMode.PlayOnce && (t < 0f || t > duration)) t = Maths.wrap(t, duration);
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
                    if (controller.GetLoopMode(layer) != AnimationLoopMode.PlayOnce && (t < 0f || t > duration)) t = Maths.wrap(t, duration);
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
                if (controller.GetLoopMode(layer) != AnimationLoopMode.PlayOnce && (nT < 0f || nT > 1f)) nT = Maths.wrap(nT, 1f);
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
                    if (controller.GetLoopMode(layer) != AnimationLoopMode.PlayOnce && (nt < 0f || nt > 1f)) nt = Maths.wrap(nt, 1f);
                    time = addTime < 0f ? math.max(time, nt) : math.min(time, nt);
                }
            }

            return time;

        }

        public override float GetPreviousTime(IAnimationLayer layer)
        {

            if (MotionParts == null) return 0f;

            if (UseDedicatedChildForTime)
            {
                if (TimeDedicatedChildIndex < 0 || TimeDedicatedChildIndex >= MotionParts.Length) return 0f;

                var dedicatedChild = MotionParts[TimeDedicatedChildIndex];
                var controller = layer.GetMotionController(dedicatedChild.ControllerIndex);
                if (controller == null) return 0f;

                return controller.GetPreviousTime(layer);
            }

            float maxLength = GetScaledDuration(layer);
            for (int a = 0; a < MotionParts.Length; a++)
            {

                var part = MotionParts[a];
                var controller = layer.GetMotionController(part.ControllerIndex);
                if (controller == null) continue;

                float length = controller.GetScaledDuration(layer);
                if (length == maxLength) // use time of controller with longest duration
                {
                    return controller.GetPreviousTime(layer);
                }
            }

            return 0f;

        }

        public override float GetPreviousNormalizedTime(IAnimationLayer layer)
        {

            if (MotionParts == null) return 0f;

            if (UseDedicatedChildForTime)
            {
                if (TimeDedicatedChildIndex < 0 || TimeDedicatedChildIndex >= MotionParts.Length) return 0f;

                var dedicatedChild = MotionParts[TimeDedicatedChildIndex];
                var controller = layer.GetMotionController(dedicatedChild.ControllerIndex);
                if (controller == null) return 0f;

                return controller.GetPreviousNormalizedTime(layer);
            }

            float maxLength = GetScaledDuration(layer);
            for (int a = 0; a < MotionParts.Length; a++)
            {
                var part = MotionParts[a];
                var controller = layer.GetMotionController(part.ControllerIndex);
                if (controller == null) continue;

                float length = controller.GetScaledDuration(layer);
                if (length == maxLength) // use normalized time of controller with longest duration
                {
                    return controller.GetPreviousNormalizedTime(layer);
                }
            }

            return 0f;

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

        public override void ForceSetPreviousTime(IAnimationLayer layer, float time)
        {
            if (MotionParts == null) return;

            for (int a = 0; a < MotionParts.Length; a++)
            {

                var motionField = MotionParts[a];
                var controller = layer.GetMotionController(motionField.ControllerIndex);
                if (controller is not CustomMotionController cmc) continue;

                cmc.ForceSetPreviousTime(layer, time);
            }
        }
        public override void ForceSetPreviousNormalizedTime(IAnimationLayer layer, float normalizedTime)
        {
            if (MotionParts == null) return;

            for (int a = 0; a < MotionParts.Length; a++)
            {

                var motionField = MotionParts[a];
                var controller = layer.GetMotionController(motionField.ControllerIndex);
                if (controller is not CustomMotionController cmc) continue;

                cmc.ForceSetPreviousNormalizedTime(layer, normalizedTime);
            }
        }

        [NonSerialized]
        protected int finalFieldCached;
        public int FinalFieldIndex => finalFieldCached - 1;

        [NonSerialized]
        protected bool[] finalIndices;

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

            if (finalIndices == null || finalIndices.Length != MotionParts.Length) finalIndices = new bool[MotionParts.Length];
            for (int a = 0; a < MotionParts.Length; a++)
            {
                finalIndices[a] = false;

                var motionField = MotionParts[a];
                var controller = layer.GetMotionController(motionField.ControllerIndex);
                if (controller is not CustomMotionController) continue;

                bool finalize = false;
                if (a == FinalFieldIndex)
                {
                    finalize = true;
                }
                else
                {
                    finalize = true;

                    var localHierarchyIndex = controller.GetLongestHierarchyIndex(Layer);
                    var localHierarchy = layer.Animator.GetTransformHierarchy(localHierarchyIndex);
                    if (localHierarchy != null)
                    {
                        for (int b = MotionParts.Length - 1; b > a; b--)
                        {
                            var motionPart2 = MotionParts[b];
                            var controller2 = layer.GetMotionController(motionPart2.ControllerIndex);
                            if (controller2 is not CustomMotionController cmc2) continue;

                            var otherHierarchyIndex = controller2.GetLongestHierarchyIndex(Layer);
                            var otherHierarchy = layer.Animator.GetTransformHierarchy(otherHierarchyIndex);
                            if (otherHierarchy != null)
                            {
                                if (localHierarchy.IsDerivative(otherHierarchy))
                                {
                                    finalize = false;
                                    break;
                                }
                            }
                        }
                    }
                }

                finalIndices[a] = finalize;
            }
        }

        public override void Progress(IAnimationLayer layer, float deltaTime, ref JobHandle jobHandle, bool useMultithreading = true, bool isFinal = false, bool previousWasFinal = false, bool canLoop = true)
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
                        if (Mathf.Approximately(sync_duration, 0f)) continue;

                        sync_time = controller.GetTime(layer) - (sync_duration * motionField.NormalizedStartTime);
                        firstIndex = a;
                        break;
                    }
                }

                float sync_duration_abs = math.abs(sync_duration);
                bool sync_duration_zero = Mathf.Approximately(sync_duration, 0f);
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
                    if (!sync_duration_zero)
                    {
                        /*bool isSmaller = sync_duration_abs >= duration_abs;
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

                        localTime = (currentTimeScaled - error) / scalingRatio;*/
                        localTime = sync_time * (duration_abs / sync_duration_abs); 
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

            blendSpeed = 0f;
            float blendSpeedWeightSum = 0f;
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

                if (useBlendSpeed)
                {
                    float mix = motionPart.GetMix(layer);
                    blendSpeed += motionPart.GetBlendSpeed(layer) * mix;
                    blendSpeedWeightSum += mix;
                }
            }
            if (useBlendSpeed && !blendSpeedIsAdditive && blendSpeedWeightSum > 0f) blendSpeed /= blendSpeedWeightSum;

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
                cmc.Progress(layer, deltaTime, ref jobHandle, useMultithreading, isFinal && finalIndices[a], previousWasFinal, canLoop);
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

        public override float GetBoneHeight(IAnimationLayer layer, string boneName, float timeOffset, out bool foundCurve)
        {
            if (MotionParts == null)
            {
                foundCurve = false;
                return 0f;
            }

            float height = 0f;
            foundCurve = false;
            for (int a = 0; a < MotionParts.Length; a++)
            {

                var part = MotionParts[a];
                var controller = layer.GetMotionController(part.ControllerIndex);
                if (controller == null) continue;

                height += controller.GetBoneHeight(layer, boneName, timeOffset, out bool partFoundCurve);
                if (partFoundCurve) foundCurve = true;

            }

            return height;
        }
        public override float GetBoneHeightAtTime(IAnimationLayer layer, string boneName, float time, out bool foundCurve)
        {
            if (MotionParts == null)
            {
                foundCurve = false;
                return 0f;
            }

            float height = 0f;
            foundCurve = false;
            for (int a = 0; a < MotionParts.Length; a++)
            {

                var part = MotionParts[a];
                var controller = layer.GetMotionController(part.ControllerIndex);
                if (controller == null) continue;

                height += controller.GetBoneHeightAtTime(layer, boneName, time, out bool partFoundCurve);
                if (partFoundCurve) foundCurve = true;

            }

            return height;
        }

        public override bool SampleInto(IAnimationLayer layer, CustomAnimator.SampledPose pose, float timeOffset, float weight, IReadOnlyDictionary<int, float> parameterOverrides = null)
        {
            if (pose == null || MotionParts == null) return false;

            bool sampled = false;
            for (int a = 0; a < MotionParts.Length; a++)
            {
                var part = MotionParts[a];
                if (layer.GetMotionController(part.ControllerIndex) is not CustomMotionController cmc) continue;
                sampled |= cmc.SampleInto(layer, pose, timeOffset, weight * GetSampleMix(part, layer, parameterOverrides), parameterOverrides);
            }

            return sampled;
        }

        public override bool AffectsTransform(IAnimationLayer layer, string transformName)
        {
            if (MotionParts == null) return false;

            for (int a = 0; a < MotionParts.Length; a++)
            {

                var part = MotionParts[a];
                var controller = layer.GetMotionController(part.ControllerIndex);
                if (controller == null) continue;

                if (controller.AffectsTransform(layer, transformName)) return true;
            }

            return false;
        }

        public override void NonAdditivePrepass(bool useMultithreading, IAnimationLayer layer)
        {
            if (MotionParts == null) return;

            for (int a = 0; a < MotionParts.Length; a++)
            {
                var motionPart = MotionParts[a];
                var controller = layer.GetMotionController(motionPart.ControllerIndex);
                if (controller == null) continue;

                controller.NonAdditivePrepass(useMultithreading, layer);
            }
        }

        public override void ApplyPropertyStates(IAnimationLayer layer)
        {
            if (MotionParts == null) return;

            for (int a = 0; a < MotionParts.Length; a++)
            {
                var motionPart = MotionParts[a];
                var controller = layer.GetMotionController(motionPart.ControllerIndex);
                if (controller == null) continue;

                controller.ApplyPropertyStates(layer);
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

        [SerializeField]
        public bool useBlendSpeed;
        public bool UseBlendSpeed
        {
            get => useBlendSpeed;
            set => useBlendSpeed = value;
        }

        [NonSerialized]
        public float blendSpeed = 1f;

        public override float GetSpeed(IAnimationLayer layer)
        {

            float speed = base.GetSpeed(layer);

            if (useBlendSpeed) speed = speed * blendSpeed; 

            return speed;

        }

        public override float GetSampleSpeed(IAnimationLayer layer, IReadOnlyDictionary<int, float> parameterOverrides)
        {
            float speed = base.GetSampleSpeed(layer, parameterOverrides);
            if (useBlendSpeed && parameterOverrides != null && parameterOverrides.Count > 0) speed = speed * blendSpeed;
            return speed;
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

                        int initialMasksCount = masks == null ? 0 : masks.Count;
                        controller.Initialize(layer, masks, this);
                        if (masks  != null) masks.RemoveRange(initialMasksCount, masks.Count - initialMasksCount);

                        float speed = field.ReplaceBaseSpeed ? 1f : controller.BaseSpeed; 
                        if (normalizeDurations)
                        {
                            if (normalizedDuration <= 0f)
                            {
                                speed = 0f;
                            }
                            else
                            {
                                float duration = Mathf.Approximately(speed, 0f) ? 0f : (controller.GetDuration(layer) / math.abs(speed));
                                speed = (duration / normalizedDuration) * Mathf.Sign(speed);
#if UNITY_EDITOR
                                if (duration <= 0f) Debug.LogWarning($"{controller.Name} duration is negative or zero: {duration} (BaseSpeed: {controller.BaseSpeed})");
#endif
                            }
                        }

                        controller.BaseSpeed = speed * field.Speed;
#if UNITY_EDITOR
                        if (normalizeDurations) Debug.Log($"{Name} Normalized Duration for {controller.Name}. (Normalized Speed:{speed}) (Base Speed:{controller.BaseSpeed}) (Duration:{controller.GetScaledDuration(layer)})");
#endif

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
                    if (paramIndex >= 0) indices.Add(paramIndex);
                }

            }

            if (BaseMotionFields != null)
            {

                foreach (var field in BaseMotionFields)
                {

                    if (field.BlendSpeedParameterIndex >= 0) indices.Add(field.BlendSpeedParameterIndex);

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

            var motionFields = BaseMotionFields;
            if (motionFields != null && motionFields.Length > 0)
            {
                loopMode = AnimationLoopMode.PlayOnce;
                foreach (var field in motionFields)
                {

                    var controller = layer.GetMotionController(field.ControllerIndex);
                    if (controller == null) continue;

                    var loopMode_ = controller.GetLoopMode(layer);
                    if (loopMode_ != AnimationLoopMode.PlayOnce)
                    {
                        loopMode = loopMode_;
                        break;
                    }
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
                if (controller.GetLoopMode(layer) != AnimationLoopMode.PlayOnce && (t < 0f || t > duration)) t = Maths.wrap(t, duration);
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
                if (controller.GetLoopMode(layer) != AnimationLoopMode.PlayOnce && (t < 0f || t > duration)) t = Maths.wrap(t, duration); 
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
                if (controller.GetLoopMode(layer) != AnimationLoopMode.PlayOnce && (nT < 0f || nT > 1f)) nT = Maths.wrap(nT, 1f);
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
                if (controller.GetLoopMode(layer) != AnimationLoopMode.PlayOnce && (nt < 0f || nt > 1f)) nt = Maths.wrap(nt, 1f);
                time += nt * weight;
            }

            return totalWeight > 0f ? (time / totalWeight) : time;

        }

        public override float GetPreviousTime(IAnimationLayer layer)
        {

            if (BaseMotionFields == null) return 0f;

            if (UseDedicatedChildForTime)
            {
                if (TimeDedicatedChildIndex < 0 || TimeDedicatedChildIndex >= BaseMotionFields.Length) return 0f;

                var dedicatedChild = BaseMotionFields[TimeDedicatedChildIndex];
                var controller = layer.GetMotionController(dedicatedChild.ControllerIndex);
                if (controller == null) return 0f;

                return controller.GetPreviousTime(layer);
            }

            float totalWeight = 0f;
            float time = 0f;

            float maxLength = GetMaxScaledDuration(layer);
            for (int a = 0; a < BaseMotionFields.Length; a++)
            {

                var motionField = BaseMotionFields[a];
                var controller = layer.GetMotionController(motionField.ControllerIndex);
                if (controller == null) continue;

                float weight = Mathf.Abs(controller.GetBaseWeight());
                totalWeight += weight;

                time += controller.GetPreviousTime(layer) * weight;

            }

            return totalWeight > 0f ? (time / totalWeight) : time;

        }

        public override float GetPreviousNormalizedTime(IAnimationLayer layer)
        {

            if (BaseMotionFields == null) return 0f;

            if (UseDedicatedChildForTime)
            {
                if (TimeDedicatedChildIndex < 0 || TimeDedicatedChildIndex >= BaseMotionFields.Length) return 0f; 

                var dedicatedChild = BaseMotionFields[TimeDedicatedChildIndex];
                var controller = layer.GetMotionController(dedicatedChild.ControllerIndex);
                if (controller == null) return 0f;

                return controller.GetPreviousNormalizedTime(layer); 
            }

            float totalWeight = 0f;
            float time = 0f;

            float maxLength = GetMaxScaledDuration(layer);
            for (int a = 0; a < BaseMotionFields.Length; a++)
            {

                var motionField = BaseMotionFields[a];
                var controller = layer.GetMotionController(motionField.ControllerIndex);
                if (controller == null) continue;

                float weight = Mathf.Abs(controller.GetBaseWeight());
                totalWeight += weight;

                time += controller.GetPreviousNormalizedTime(layer) * weight;

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

        public override void ForceSetPreviousTime(IAnimationLayer layer, float time)
        {
            if (BaseMotionFields == null) return;

            for (int a = 0; a < BaseMotionFields.Length; a++)
            {

                var motionField = BaseMotionFields[a];
                var controller = layer.GetMotionController(motionField.ControllerIndex);
                if (controller is not CustomMotionController cmc) continue;

                cmc.ForceSetPreviousTime(layer, time);
            }
        }
        public override void ForceSetPreviousNormalizedTime(IAnimationLayer layer, float normalizedTime)
        {
            if (BaseMotionFields == null) return;

            for (int a = 0; a < BaseMotionFields.Length; a++)
            {

                var motionField = BaseMotionFields[a];
                var controller = layer.GetMotionController(motionField.ControllerIndex);
                if (controller is not CustomMotionController cmc) continue;

                cmc.ForceSetPreviousNormalizedTime(layer, normalizedTime);
            }
        }

        [NonSerialized]
        protected int finalFieldCached;
        public int FinalFieldIndex => finalFieldCached - 1;
        [NonSerialized]
        protected bool[] finalIndices;

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

            if (finalIndices == null || finalIndices.Length != BaseMotionFields.Length) finalIndices = new bool[BaseMotionFields.Length];
            for (int a = 0; a < BaseMotionFields.Length; a++)
            {
                finalIndices[a] = false;

                var motionField = BaseMotionFields[a];
                var controller = layer.GetMotionController(motionField.ControllerIndex);
                if (controller is not CustomMotionController) continue;

                bool finalize = false;
                if (a == FinalFieldIndex)
                {
                    finalize = true;
                }
                else
                {
                    finalize = true;

                    var localHierarchyIndex = controller.GetLongestHierarchyIndex(Layer);
                    var localHierarchy = layer.Animator.GetTransformHierarchy(localHierarchyIndex);
                    if (localHierarchy != null)
                    {
                        for (int b = BaseMotionFields.Length - 1; b > a; b--)
                        {
                            var motionPart2 = BaseMotionFields[b];
                            var controller2 = layer.GetMotionController(motionPart2.ControllerIndex);
                            if (controller2 is not CustomMotionController cmc2) continue;

                            var otherHierarchyIndex = controller2.GetLongestHierarchyIndex(Layer);
                            var otherHierarchy = layer.Animator.GetTransformHierarchy(otherHierarchyIndex);
                            if (otherHierarchy != null)
                            {
                                if (localHierarchy.IsDerivative(otherHierarchy))
                                {
                                    finalize = false;
                                    break;
                                }
                            }
                        }
                    }
                }

                finalIndices[a] = finalize;
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

        public override void Progress(IAnimationLayer layer, float deltaTime, ref JobHandle jobHandle, bool useMultithreading = true, bool isFinal = false, bool previousWasFinal = false, bool canLoop = true)
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
                    sync_duration = GetDuration(layer);
                    sync_time = GetTime(layer, 0f);
                }
                else
                {
                    for (int a = 0; a < BaseMotionFields.Length; a++)
                    {
                        var motionField = BaseMotionFields[a];
                        var controller = layer.GetMotionController(motionField.ControllerIndex);
                        if (controller is not CustomMotionController cmc) continue;

                        sync_duration = controller.GetDuration(layer);
                        if (Mathf.Approximately(sync_duration, 0f)) continue;

                        sync_time = controller.GetTime(layer) - (sync_duration * motionField.NormalizedStartTime);
                        firstIndex = a;
                        break;
                    }
                }

                float sync_duration_abs = math.abs(sync_duration);
                bool sync_duration_zero = Mathf.Approximately(sync_duration, 0f);
                for (int a = firstIndex + 1; a < BaseMotionFields.Length; a++)
                {
                    if (UseDedicatedChildForTime && a == TimeDedicatedChildIndex) continue; // skip dedicated child

                    var motionField = BaseMotionFields[a];
                    var controller = layer.GetMotionController(motionField.ControllerIndex); 
                    if (controller is not CustomMotionController cmc) continue;

                    var duration = controller.GetDuration(layer);
                    if (Mathf.Approximately(duration, 0f)) continue;

                    float duration_abs = math.abs(duration);
                    float offset = duration * motionField.NormalizedStartTime;

                    float localTime = 0f;
                    if (!sync_duration_zero)
                    {
                        /*bool isSmaller = sync_duration_abs >= duration_abs;
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

                        localTime = (currentTimeScaled - error) / scalingRatio;  */
                        localTime = sync_time * (duration_abs / sync_duration_abs);
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

            if (useBlendSpeed)
            {
                blendSpeed = 0f;
                float blendSpeedWeightSum = 0f;
                for (int a = 0; a < BaseMotionFields.Length; a++)
                {
                    var motionField = BaseMotionFields[a];

                    var controller = layer.GetMotionController(motionField.ControllerIndex);
                    if (controller == null) continue;

                    float mix = controller.GetBaseWeight();
                    blendSpeed += motionField.GetBlendSpeed(layer) * mix;
                    blendSpeedWeightSum += mix;
                }
                if (blendSpeedWeightSum > 0f) blendSpeed /= blendSpeedWeightSum; 
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
                cmc.Progress(layer, deltaTime, ref jobHandle, useMultithreading, isFinal && finalIndices[a], previousWasFinal, canLoop); 
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

        public override float GetBoneHeight(IAnimationLayer layer, string boneName, float timeOffset, out bool foundCurve)
        {
            if (BaseMotionFields == null)
            {
                foundCurve = false;
                return 0f;
            }

            float height = 0f;
            foundCurve = false;
            for (int a = 0; a < BaseMotionFields.Length; a++)
            {

                var part = BaseMotionFields[a];
                var controller = layer.GetMotionController(part.ControllerIndex);
                if (controller == null) continue;

                height += controller.GetBoneHeight(layer, boneName, timeOffset, out bool partFoundCurve);
                if (partFoundCurve) foundCurve = true;

            }

            return height;
        }
        public override float GetBoneHeightAtTime(IAnimationLayer layer, string boneName, float time, out bool foundCurve)
        {
            if (BaseMotionFields == null)
            {
                foundCurve = false;
                return 0f;
            }

            float height = 0f;
            foundCurve = false; 
            for (int a = 0; a < BaseMotionFields.Length; a++)
            {

                var part = BaseMotionFields[a];
                var controller = layer.GetMotionController(part.ControllerIndex);
                if (controller == null) continue;

                height += controller.GetBoneHeightAtTime(layer, boneName, time, out bool partFoundCurve); 
                if (partFoundCurve) foundCurve = true;

            }

            return height;
        }

        public override bool SampleInto(IAnimationLayer layer, CustomAnimator.SampledPose pose, float timeOffset, float weight, IReadOnlyDictionary<int, float> parameterOverrides = null)
        {
            if (pose == null || BaseMotionFields == null) return false;

            float[] sampleWeights = null;
            if (parameterOverrides != null && parameterOverrides.Count > 0)
            {
                sampleWeights = new float[BaseMotionFields.Length];
                if (!TryGetSampleWeights(layer, parameterOverrides, sampleWeights)) sampleWeights = null;
            }

            bool sampled = false;
            for (int a = 0; a < BaseMotionFields.Length; a++)
            {
                if (layer.GetMotionController(BaseMotionFields[a].ControllerIndex) is not CustomMotionController cmc) continue;
                float childWeight = sampleWeights == null ? cmc.GetBaseWeight() : sampleWeights[a];
                if (childWeight == 0f) continue;
                sampled |= cmc.SampleInto(layer, pose, timeOffset, weight * childWeight, parameterOverrides);
            }

            return sampled;
        }

        /// <summary>
        /// Computes per-field blend weights for sampling when one of this tree's blend parameters is overridden, mirroring <see cref="UpdateBlendValues"/> without modifying any state.
        /// Returns false if no relevant override exists, in which case the live weights are used.
        /// </summary>
        protected virtual bool TryGetSampleWeights(IAnimationLayer layer, IReadOnlyDictionary<int, float> parameterOverrides, float[] weights) => false;

        public override bool AffectsTransform(IAnimationLayer layer, string transformName)
        {
            if (BaseMotionFields == null) return false;

            for (int a = 0; a < BaseMotionFields.Length; a++)
            {

                var part = BaseMotionFields[a];
                var controller = layer.GetMotionController(part.ControllerIndex);
                if (controller == null) continue;

                if (controller.AffectsTransform(layer, transformName)) return true;
            }

            return false;
        }

        public override void NonAdditivePrepass(bool useMultithreading, IAnimationLayer layer)
        {
            if (BaseMotionFields == null) return;

            for (int a = 0; a < BaseMotionFields.Length; a++)
            {
                var part = BaseMotionFields[a];
                var controller = layer.GetMotionController(part.ControllerIndex);
                if (controller == null) continue;

                controller.NonAdditivePrepass(useMultithreading, layer);
            }
        }

        public override void ApplyPropertyStates(IAnimationLayer layer)
        {
            if (BaseMotionFields == null) return;

            for (int a = 0; a < BaseMotionFields.Length; a++)
            {
                var part = BaseMotionFields[a];
                var controller = layer.GetMotionController(part.ControllerIndex);
                if (controller == null) continue;
                 
                controller.ApplyPropertyStates(layer);
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

            public bool useBlendSpeed;

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

                anim.useBlendSpeed = useBlendSpeed;

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

                s.useBlendSpeed = inst.useBlendSpeed;

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

            clone.useBlendSpeed = useBlendSpeed;

            return clone;

        }
        public override void GetChildIndices(List<int> indices, bool onlyAddIfNotPresent)
        {

            if (motionFields == null || indices == null) return;

            if (onlyAddIfNotPresent)
            {

                foreach (var field in motionFields) if (field.controllerIndex >= 0 && !indices.Contains(field.controllerIndex)) indices.Add(field.controllerIndex);
                
            }
            else
            {

                foreach (var field in motionFields) if (field.controllerIndex >= 0) indices.Add(field.controllerIndex);

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

        public override void RemapParameterIndices(IAnimationLayer layer, Dictionary<int, int> remapper, bool invalidateNonRemappedIndices = false, bool remapChildren = true)
        {

            if (remapper == null) return;

            base.RemapParameterIndices(layer, remapper, invalidateNonRemappedIndices, remapChildren);

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

                for (int a = 0; a < motionFields.Length; a++)
                {
                    var field = motionFields[a];

                    if (field.BlendSpeedParameterIndex >= 0)
                    {

                        int origIndex = field.BlendSpeedParameterIndex;
                        if (!remapper.TryGetValue(origIndex, out field.blendSpeedParameterIndex) && invalidateNonRemappedIndices)
                        {
                            field.blendSpeedParameterIndex = -1;

#if UNITY_EDITOR
                            Debug.LogError($"Failed to remap blend speed parameter index {origIndex} for {GetType().Name} '{name}', (Field:{a})");
#else
                            swole.LogError($"Failed to remap blend speed parameter index {origIndex} for {GetType().Name} '{name}', (Field:{a})");
#endif
                        }

                    }
                    
                    if (remapChildren)
                    {

                        var controller = layer.GetMotionController(field.controllerIndex);
                        if (controller == null) continue;

                        controller.RemapParameterIndices(layer, remapper, invalidateNonRemappedIndices, remapChildren);

                    }

                }

            }

        }

        private static int SortMotionFields(MotionField1D mfA, MotionField1D mfB) => Math.Sign(mfA.threshold - mfB.threshold);
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
                    sortedFields.Sort(SortMotionFields);
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
                        motionFields[a] = new MotionField1D() { replaceBaseSpeed = mf.ReplaceBaseSpeed, speed = mf.Speed, threshold = mf.Threshold, controllerIndex = mf.ControllerIndex, blendSpeed = mf.BlendSpeed, blendSpeedParameterIndex = mf.BlendSpeedParameterIndex };
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

                        controller.SetWeight(1f);
                        controller.SyncWeight(layer);

                        if (controller is IBlendTree blendTree) blendTree.UpdateBlendValues(layer, blendTree.GetNormalizedTime(layer), deltaTime);  
                    }
                    else
                    {

                        var prevMotionField = motionFields[minIndex];
                        float interp = math.saturate((thresholdPosition - prevMotionField.threshold) / (motionField.threshold - prevMotionField.threshold));
                        
                        var minController = layer.GetMotionControllerUnsafe(prevMotionField.controllerIndex);
                        var maxController = layer.GetMotionControllerUnsafe(motionField.controllerIndex); 

                        minController.SetWeight(1f - interp);
                        minController.SyncWeight(layer);
                        maxController.SetWeight(interp);
                        maxController.SyncWeight(layer);

                        if (minController is IBlendTree prevBlendTree) prevBlendTree.UpdateBlendValues(layer, prevBlendTree.GetNormalizedTime(layer), deltaTime); 
                        if (maxController is IBlendTree blendTree) blendTree.UpdateBlendValues(layer, blendTree.GetNormalizedTime(layer), deltaTime);  
                    }

                    break;
                     
                }
                
                controller.SetWeight(0f);
                controller.SyncWeight(layer);
            }

            for (int a = maxIndex + 1; a < motionFields.Length; a++)
            {
                var controller = layer.GetMotionControllerUnsafe(motionFields[a].controllerIndex);
                if (controller != null) 
                { 
                    controller.SetWeight(0f);
                    controller.SyncWeight(layer);
                }
            }

            /*string debug = $"(BT1) ({GetWeight()}) {name}: "; 
            for (int a = 0; a < motionFields.Length; a++)
            {
                var controller = layer.GetMotionControllerUnsafe(motionFields[a].controllerIndex);
                if (controller != null)
                {
                    debug = $"{debug}, ({controller.Name}:{controller.GetWeight()})"; 
                }
            }
            Debug.Log(debug);*/

        }

        protected override bool TryGetSampleWeights(IAnimationLayer layer, IReadOnlyDictionary<int, float> parameterOverrides, float[] weights)
        {
            if (layer == null || motionFields == null || !HasParameterOverride(ParameterIndex, parameterOverrides)) return false;

            float thresholdPosition = parameterOverrides[ParameterIndex];
            for (int a = 0; a < motionFields.Length; a++)
            {
                var motionField = motionFields[a];
                if (layer.GetMotionController(motionField.controllerIndex) == null) continue;

                if (motionField.threshold > thresholdPosition || a == motionFields.Length - 1)
                {
                    int minIndex = a - 1;
                    int maxIndex = a;
                    if (minIndex < 0) minIndex = 0;

                    if (minIndex == maxIndex)
                    {
                        weights[maxIndex] = 1f;
                    }
                    else
                    {
                        var prevMotionField = motionFields[minIndex];
                        float interp = math.saturate((thresholdPosition - prevMotionField.threshold) / (motionField.threshold - prevMotionField.threshold));
                        weights[minIndex] = 1f - interp;
                        weights[maxIndex] = interp;
                    }

                    break;
                }
            }

            return true;
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

            public bool useBlendSpeed;

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

                anim.useBlendSpeed = useBlendSpeed;

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

                s.useBlendSpeed = inst.useBlendSpeed;

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

            clone.useBlendSpeed = useBlendSpeed;

            return clone;

        }

        public override void GetChildIndices(List<int> indices, bool onlyAddIfNotPresent)
        {

            if (motionFields == null || indices == null) return;

            if (onlyAddIfNotPresent)
            {

                foreach (var field in motionFields) if (field.controllerIndex >= 0 && !indices.Contains(field.controllerIndex)) indices.Add(field.controllerIndex);

            }
            else
            {

                foreach (var field in motionFields) if (field.controllerIndex >= 0) indices.Add(field.controllerIndex); 

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
            if (localParameterIndex > 0) // if it's greater than zero it's parameter Y, otherwise it's parameter X
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
            if (localParameterIndex > 0) // if it's greater than zero it's parameter Y, otherwise it's parameter X
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

        public override void RemapParameterIndices(IAnimationLayer layer, Dictionary<int, int> remapper, bool invalidateNonRemappedIndices = false, bool remapChildren = true)
        {

            if (remapper == null) return;

            base.RemapParameterIndices(layer, remapper, invalidateNonRemappedIndices, remapChildren);

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

                for (int a = 0; a < motionFields.Length; a++)
                {
                    var field = motionFields[a];

                    if (field.BlendSpeedParameterIndex >= 0)
                    {

                        int origIndex = field.BlendSpeedParameterIndex;
                        if (!remapper.TryGetValue(origIndex, out field.blendSpeedParameterIndex) && invalidateNonRemappedIndices)
                        {
                            field.blendSpeedParameterIndex = -1;

#if UNITY_EDITOR
                            Debug.LogError($"Failed to remap blend speed parameter index {origIndex} for {GetType().Name} '{name}', (Field:{a})");
#else
                            swole.LogError($"Failed to remap blend speed parameter index {origIndex} for {GetType().Name} '{name}', (Field:{a})");
#endif
                        }

                    }

                    if (remapChildren)
                    {

                        var controller = layer.GetMotionController(field.controllerIndex);
                        if (controller == null) continue;

                        controller.RemapParameterIndices(layer, remapper, invalidateNonRemappedIndices, remapChildren);

                    }

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
                        motionFields[a] = new MotionField2D() { replaceBaseSpeed = mf.ReplaceBaseSpeed, speed = mf.Speed, x = mf.X, y = mf.Y, controllerIndex = mf.ControllerIndex, blendSpeed = mf.BlendSpeed, blendSpeedParameterIndex = mf.BlendSpeedParameterIndex };
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

            using (var positions = new NativeArray<double2>(motionVertices.Length, Allocator.TempJob))
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

        protected override bool TryGetSampleWeights(IAnimationLayer layer, IReadOnlyDictionary<int, float> parameterOverrides, float[] weights)
        {
            if (layer == null || motionFields == null || motionTriangles == null || motionVertices == null) return false;
            if (!HasParameterOverride(ParameterIndexX, parameterOverrides) && !HasParameterOverride(ParameterIndexY, parameterOverrides)) return false;

            TryGetSampleParameterValue(layer, ParameterIndexX, parameterOverrides, out float posX);
            TryGetSampleParameterValue(layer, ParameterIndexY, parameterOverrides, out float posY);
            float2 point = new float2(posX, posY);

            bool hasTri = false;
            MotionVertex closestV0 = default, closestV1 = default, closestV2 = default;
            float3 closestCoords = float3.zero;
            float minDist = float.MaxValue;
            for (int a = 0; a < motionTriangles.Length; a++)
            {
                var tri = motionTriangles[a];
                var v0 = motionVertices[tri.i0];
                var v1 = motionVertices[tri.i1];
                var v2 = motionVertices[tri.i2];

                var baryCoords = Maths.BarycentricCoords2D(point, v0, v1, v2);
                if (Maths.IsInTriangle2D(baryCoords))
                {
                    hasTri = true;
                    closestV0 = v0; closestV1 = v1; closestV2 = v2;
                    closestCoords = baryCoords;
                    break;
                }

                float3 dist = math.abs(math.select(baryCoords, baryCoords - 1f, baryCoords > 0));
                float distSq = math.lengthsq(dist);
                if (distSq < minDist)
                {
                    minDist = distSq;
                    hasTri = true;
                    closestV0 = v0; closestV1 = v1; closestV2 = v2;
                    closestCoords = baryCoords;
                }
            }

            if (hasTri)
            {
                closestCoords = math.clamp(closestCoords, 0f, 1f);
                float totalWeight = math.csum(math.abs(closestCoords));
                if (totalWeight > 0f) closestCoords = closestCoords / totalWeight;

                weights[closestV0.motionFieldIndex] += closestCoords.x;
                weights[closestV1.motionFieldIndex] += closestCoords.y;
                weights[closestV2.motionFieldIndex] += closestCoords.z;
            }

            return true;
        }

        private void UpdateMotionField(IAnimationLayer layer, int index, float weight, float deltaTime)
        {
            var motionField = motionFields[index];
            var controller = layer.GetMotionController(motionField.controllerIndex);
            if (controller == null) return;

            controller.SetWeight(weight + controller.GetBaseWeight()); // sum with existing weight incase the controller appears in multiple motion fields
            controller.SyncWeight(layer);

            if (controller is IBlendTree blendTree) blendTree.UpdateBlendValues(layer, blendTree.GetNormalizedTime(layer), deltaTime);
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

                float totalWeight = math.csum(math.abs(closestCoords));
                if (totalWeight > 0f) closestCoords = closestCoords / totalWeight; 

                UpdateMotionField(layer, closestV0.motionFieldIndex, closestCoords.x, deltaTime);
                UpdateMotionField(layer, closestV1.motionFieldIndex, closestCoords.y, deltaTime);
                UpdateMotionField(layer, closestV2.motionFieldIndex, closestCoords.z, deltaTime);

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

    public class CustomMotionControllersBuilder
    {
        public readonly List<MotionControllerIdentifier> motionControllerIds = new List<MotionControllerIdentifier>();

        public readonly List<AnimationReference> animationReferences = new List<AnimationReference>();
        public readonly List<MotionComposite> motionComposites = new List<MotionComposite>();
        public readonly List<BlendTree1D> blendTree1Ds = new List<BlendTree1D>();
        public readonly List<BlendTree2D> blendTree2Ds = new List<BlendTree2D>();

        public void Clear()
        {
            motionControllerIds.Clear();

            animationReferences.Clear();
            motionComposites.Clear();
            blendTree1Ds.Clear();
            blendTree2Ds.Clear();
        }

        public CustomMotionController GetMotionController(MotionControllerIdentifier id)
        {
            switch (id.type)
            {
                case MotionControllerType.AnimationReference:
                    if (id.index < 0 || id.index >= animationReferences.Count) return null;
                    return animationReferences[id.index];

                case MotionControllerType.MotionComposite:
                    if (id.index < 0 || id.index >= motionComposites.Count) return null;
                    return motionComposites[id.index];

                case MotionControllerType.BlendTree1D:
                    if (id.index < 0 || id.index >= blendTree1Ds.Count) return null;
                    return blendTree1Ds[id.index];

                case MotionControllerType.BlendTree2D:
                    if (id.index < 0 || id.index >= blendTree2Ds.Count) return null;
                    return blendTree2Ds[id.index];
            }

            return null;
        }

        public CustomMotionController GetMotionController(int idIndex)
        {
            if (idIndex < 0 || idIndex >= motionControllerIds.Count) return null;
            return GetMotionController(motionControllerIds[idIndex]);
        }

        public void AddAnimationReference(AnimationReference inst) => AddAnimationReference(inst, out _, out _);
        public void AddAnimationReference(AnimationReference inst, out int motionControllerIndex, out int animRefIndex)
        {
            motionControllerIndex = motionControllerIds.Count;
            animRefIndex = animationReferences.Count;

            animationReferences.Add(inst);
            motionControllerIds.Add(new MotionControllerIdentifier() { index = animRefIndex, type = MotionControllerType.AnimationReference });
        }

        public void AddMotionComposite(MotionComposite inst) => AddMotionComposite(inst, out _, out _);
        public void AddMotionComposite(MotionComposite inst, out int motionControllerIndex, out int motionCompIndex)
        {
            motionControllerIndex = motionControllerIds.Count;
            motionCompIndex = motionComposites.Count;

            motionComposites.Add(inst);
            motionControllerIds.Add(new MotionControllerIdentifier() { index = motionCompIndex, type = MotionControllerType.MotionComposite });
        }

        public void AddBlendTree1D(BlendTree1D inst) => AddBlendTree1D(inst, out _, out _);
        public void AddBlendTree1D(BlendTree1D inst, out int motionControllerIndex, out int blendTreeIndex)
        {
            motionControllerIndex = motionControllerIds.Count;
            blendTreeIndex = blendTree1Ds.Count;

            blendTree1Ds.Add(inst);
            motionControllerIds.Add(new MotionControllerIdentifier() { index = blendTreeIndex, type = MotionControllerType.BlendTree1D });
        }

        public void AddBlendTree2D(BlendTree2D inst) => AddBlendTree2D(inst, out _, out _);
        public void AddBlendTree2D(BlendTree2D inst, out int motionControllerIndex, out int blendTreeIndex)
        {
            motionControllerIndex = motionControllerIds.Count;
            blendTreeIndex = blendTree2Ds.Count;

            blendTree2Ds.Add(inst);
            motionControllerIds.Add(new MotionControllerIdentifier() { index = blendTreeIndex, type = MotionControllerType.BlendTree2D });
        }

        public int GetMotionControllerIdIndex(string name, MotionControllerType type, int minIndex = 0, int maxIndex = -1)
        {
            if (string.IsNullOrWhiteSpace(name)) return -1;

            minIndex = Mathf.Max(0, minIndex);
            for (int a = minIndex; a <= (maxIndex < 0 ? (motionControllerIds.Count-1) : maxIndex); a++)
            {
                var id = motionControllerIds[a];

                if (id.type != type) continue;

                switch(id.type)
                {
                    case MotionControllerType.AnimationReference:
                        var animRef = animationReferences[id.index];
                        if (animRef.name == name) return a;
                        break;

                    case MotionControllerType.MotionComposite:
                        var comp = motionComposites[id.index];
                        if (comp.name == name) return a;
                        break;

                    case MotionControllerType.BlendTree1D:
                        var tree1D = blendTree1Ds[id.index];
                        if (tree1D.name == name) return a;
                        break;

                    case MotionControllerType.BlendTree2D:
                        var tree2D = blendTree2Ds[id.index];
                        if (tree2D.name == name) return a;
                        break;
                }
            }

            return -1;
        }

        public int GetAnyMotionControllerIndex(string name, int minIndex = 0, int maxIndex = -1)
        {
            if (string.IsNullOrWhiteSpace(name)) return -1;

            minIndex = Mathf.Max(0, minIndex);
            for (int a = minIndex; a <= (maxIndex < 0 ? (motionControllerIds.Count - 1) : maxIndex); a++)
            {
                var id = motionControllerIds[a];
                switch (id.type)
                {
                    case MotionControllerType.AnimationReference:
                        var animRef = animationReferences[id.index];
                        if (animRef.name == name) return a;
                        break;

                    case MotionControllerType.MotionComposite:
                        var comp = motionComposites[id.index];
                        if (comp.name == name) return a;
                        break;

                    case MotionControllerType.BlendTree1D:
                        var tree1D = blendTree1Ds[id.index];
                        if (tree1D.name == name) return a;
                        break;

                    case MotionControllerType.BlendTree2D:
                        var tree2D = blendTree2Ds[id.index];
                        if (tree2D.name == name) return a;
                        break;
                }
            }

            return -1;
        }

        public bool TryFindAnimationReferenceIndex(string name, out int controllerIndex, out int animRefIndex, int minIndex = 0, int maxIndex = -1)
        {
            controllerIndex = -1;
            animRefIndex = -1;

            if (string.IsNullOrWhiteSpace(name)) return false;

            minIndex = Mathf.Max(0, minIndex);
            for (int a = minIndex; a <= (maxIndex < 0 ? (motionControllerIds.Count - 1) : maxIndex); a++)
            {
                var id = motionControllerIds[a];

                if (id.type == MotionControllerType.AnimationReference)
                {
                    var inst = animationReferences[a];
                    if (inst != null && inst.name == name)
                    {
                        controllerIndex = a;
                        animRefIndex = id.index;
                        return true;
                    }
                }
            }

            return false;
        }
        public bool TryFindMotionCompositeindex(string name, out int controllerIndex, out int motionCompIndex, int minIndex = 0, int maxIndex = -1)
        {
            controllerIndex = -1;
            motionCompIndex = -1;

            if (string.IsNullOrWhiteSpace(name)) return false;

            minIndex = Mathf.Max(0, minIndex);
            for (int a = minIndex; a <= (maxIndex < 0 ? (motionControllerIds.Count - 1) : maxIndex); a++)
            {
                var id = motionControllerIds[a];

                if (id.type == MotionControllerType.MotionComposite) 
                {
                    var inst = motionComposites[a];
                    if (inst != null && inst.name == name)
                    {
                        controllerIndex = a;
                        motionCompIndex = id.index;
                        return true;
                    }
                }
            }

            return false;
        }
        public bool TryFindBlendTree1DIndex(string name, out int controllerIndex, out int blendTree1dIndex, int minIndex = 0, int maxIndex = -1)
        {
            controllerIndex = -1;
            blendTree1dIndex = -1;

            if (string.IsNullOrWhiteSpace(name)) return false;

            minIndex = Mathf.Max(0, minIndex);
            for (int a = minIndex; a <= (maxIndex < 0 ? (motionControllerIds.Count - 1) : maxIndex); a++)
            {
                var id = motionControllerIds[a];

                if (id.type == MotionControllerType.BlendTree1D)
                {
                    var inst = blendTree1Ds[a];
                    if (inst != null && inst.name == name)
                    {
                        controllerIndex = a;
                        blendTree1dIndex = id.index;
                        return true;
                    }
                }
            }

            return false;
        }
        public bool TryFindBlendTree2DIndex(string name, out int controllerIndex, out int blendTree2dIndex, int minIndex = 0, int maxIndex = -1)
        {
            controllerIndex = -1;
            blendTree2dIndex = -1;

            if (string.IsNullOrWhiteSpace(name)) return false;

            minIndex = Mathf.Max(0, minIndex);
            for (int a = minIndex; a <= (maxIndex < 0 ? (motionControllerIds.Count - 1) : maxIndex); a++)
            {
                var id = motionControllerIds[a];

                if (id.type == MotionControllerType.BlendTree2D)
                {
                    var inst = blendTree2Ds[a];
                    if (inst != null && inst.name == name)
                    {
                        controllerIndex = a;
                        blendTree2dIndex = id.index;
                        return true;
                    }
                }
            }

            return false;
        }

        public void Finalize(CustomAnimationController controller) => Finalize(controller, false, out _, out _, out _, out _, false);
        public void Finalize(CustomAnimationController controller, bool additive, out int animRefIndexOffset, out int motionCompIndexOffset, out int blendTree1dIndexOffset, out int blendTree2dIndexOffset, bool updateInternalMotionControllerIDs = false)
        {
            animRefIndexOffset = 0;
            motionCompIndexOffset = 0;
            blendTree1dIndexOffset = 0;
            blendTree2dIndexOffset = 0; 

            if (additive)
            {
                // Animation References
                if (controller.animationReferences == null || controller.animationReferences.Length <= 0)
                {
                    controller.animationReferences = animationReferences.ToArray();
                }
                else
                {
                    animRefIndexOffset = controller.animationReferences.Length;
                    var newArray = new AnimationReference[controller.animationReferences.Length + animationReferences.Count];
                    controller.animationReferences.CopyTo(newArray, 0);
                    animationReferences.CopyTo(newArray, animRefIndexOffset);
                    controller.animationReferences = newArray;
                }

                // Motion Composites
                if (controller.motionComposites == null || controller.motionComposites.Length <= 0)
                {
                    controller.motionComposites = motionComposites.ToArray();
                }
                else
                {
                    motionCompIndexOffset = controller.motionComposites.Length;
                    var newArray = new MotionComposite[controller.motionComposites.Length + motionComposites.Count];
                    controller.motionComposites.CopyTo(newArray, 0);
                    motionComposites.CopyTo(newArray, motionCompIndexOffset);
                    controller.motionComposites = newArray;
                }

                // Blend Tree 1Ds
                if (controller.blendTrees1D == null || controller.blendTrees1D.Length <= 0)
                {
                    controller.blendTrees1D = blendTree1Ds.ToArray();
                }
                else
                {
                    blendTree1dIndexOffset = controller.blendTrees1D.Length;
                    var newArray = new BlendTree1D[controller.blendTrees1D.Length + blendTree1Ds.Count];
                    controller.blendTrees1D.CopyTo(newArray, 0);
                    blendTree1Ds.CopyTo(newArray, blendTree1dIndexOffset);
                    controller.blendTrees1D = newArray;
                }

                // Blend Tree 2Ds
                if (controller.blendTrees2D == null || controller.blendTrees2D.Length <= 0)
                {
                    controller.blendTrees2D = blendTree2Ds.ToArray();
                }
                else
                {
                    blendTree2dIndexOffset = controller.blendTrees2D.Length;
                    var newArray = new BlendTree2D[controller.blendTrees2D.Length + blendTree2Ds.Count];
                    controller.blendTrees2D.CopyTo(newArray, 0);
                    blendTree2Ds.CopyTo(newArray, blendTree2dIndexOffset);
                    controller.blendTrees2D = newArray;
                }

                if (updateInternalMotionControllerIDs)
                {
                    if (motionControllerIds != null)
                    {
                        for (int i = 0; i < motionControllerIds.Count; i++)
                        {
                            var id = motionControllerIds[i];
                            switch (id.type)
                            {
                                case MotionControllerType.AnimationReference:
                                    Debug.Log($"Updated MC [{i}] AnimationReference {animationReferences[id.index].name} local index {id.index} -> {id.index + animRefIndexOffset}");
                                    id.index = id.index + animRefIndexOffset;
                                    break;
                                case MotionControllerType.MotionComposite:
                                    Debug.Log($"Updated MC [{i}] MotionComposite {motionComposites[id.index].name} local index {id.index} -> {id.index + motionCompIndexOffset}");
                                    id.index = id.index + motionCompIndexOffset;
                                    break;
                                case MotionControllerType.BlendTree1D:
                                    Debug.Log($"Updated MC [{i}] BlendTree1D {blendTree1Ds[id.index].name} local index {id.index} -> {id.index + blendTree1dIndexOffset}");
                                    id.index = id.index + blendTree1dIndexOffset;
                                    break;
                                case MotionControllerType.BlendTree2D:
                                    Debug.Log($"Updated MC [{i}] BlendTree2D {blendTree2Ds[id.index].name} local index {id.index} -> {id.index + blendTree2dIndexOffset}");  
                                    id.index = id.index + blendTree2dIndexOffset;
                                    break;
                            }
                            motionControllerIds[i] = id;
                        }
                    }
                }
            }
            else
            {
                controller.animationReferences = animationReferences.ToArray();
                controller.motionComposites = motionComposites.ToArray();
                controller.blendTrees1D = blendTree1Ds.ToArray();
                controller.blendTrees2D = blendTree2Ds.ToArray();
            }
        }

        public void Finalize(CustomAnimationLayer layer)
        {
            int startIndex = 0;
            int count = motionControllerIds.Count;
            Finalize(layer, false, ref startIndex, ref count, out _);
        }
        public void Finalize(CustomAnimationLayer layer, bool additive, out int idsOffsetInLayerOwnedArray)
        {
            int startIndex = 0;
            int count = motionControllerIds.Count;
            Finalize(layer, additive, ref startIndex, ref count, out idsOffsetInLayerOwnedArray);
        }
        public void Finalize(CustomAnimationLayer layer, bool additive, ref int startIndex, ref int count, out int idsOffsetInLayerOwnedArray)
        {
            startIndex = Mathf.Max(0, startIndex);
            count = Mathf.Clamp(count, 0, motionControllerIds.Count - startIndex); 

            idsOffsetInLayerOwnedArray = 0;

            if (count <= 0) return;

            if (additive)
            {
                if (layer.motionControllerIdentifiers == null || layer.motionControllerIdentifiers.Length <= 0)
                {
                    if (startIndex == 0 && count == motionControllerIds.Count)
                    {
                        layer.motionControllerIdentifiers = motionControllerIds.ToArray();
                    }
                    else
                    {
                        var newArray = new MotionControllerIdentifier[count];
                        motionControllerIds.CopyTo(startIndex, newArray, 0, count);
                        layer.motionControllerIdentifiers = newArray;
                    }
                }
                else
                {
                    idsOffsetInLayerOwnedArray = layer.motionControllerIdentifiers.Length;
                    var newArray = new MotionControllerIdentifier[layer.motionControllerIdentifiers.Length + count];
                    layer.motionControllerIdentifiers.CopyTo(newArray, 0);
                    motionControllerIds.CopyTo(startIndex, newArray, idsOffsetInLayerOwnedArray, count); 
                    layer.motionControllerIdentifiers = newArray;
                }
            }
            else
            {
                layer.motionControllerIdentifiers = motionControllerIds.ToArray(); 
            }
        }
    }

}

#endif