#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Unity.Jobs;
using Unity.Mathematics;

using Swole.Animation;

namespace Swole.API.Unity.Animation
{
    [Serializable]
    public abstract class CustomMotionController : IAnimationMotionController
    {

        private static void CloneBase(CustomMotionController reference, CustomMotionController clone)
        {

            clone.name = reference.name;

            clone.baseSpeed = reference.baseSpeed;
            clone.speedMultiplierParameter = reference.speedMultiplierParameter;

        }

        public string name;
        public string Name
        {
            get => name;
            set => name = value;
        }

        [Serializable]
        public class AnimationReference : CustomMotionController, IAnimationReference
        {

            #region Serialization

            [Serializable]
            public struct Serialized : ISerializableContainer<AnimationReference, AnimationReference.Serialized>
            {

                public string name;
                public float baseSpeed;
                public int speedMultiplierParameter;

                public PackageIdentifier packageId;
                public string animationId;
                public int loopMode;

                public string AsJSON(bool prettyPrint = false) => swole.Engine.ToJson(this, prettyPrint);

                public object AsNonserializableObject(PackageInfo packageInfo = default) => AsOriginalType(packageInfo);
                public AnimationReference AsOriginalType(PackageInfo packageInfo = default)
                {
                    var anim = new AnimationReference(name, AnimationLibrary.FindAnimation(packageId, animationId), (AnimationLoopMode)loopMode);

                    anim.baseSpeed = baseSpeed;
                    anim.speedMultiplierParameter = speedMultiplierParameter;

                    return anim;
                }

                public static implicit operator Serialized(AnimationReference inst)
                {
                    Serialized s = new Serialized();

                    s.name = inst.name;
                    s.baseSpeed = inst.baseSpeed;
                    s.speedMultiplierParameter = inst.speedMultiplierParameter;

                    s.packageId = inst.Animation.PackageInfo.GetIdentity();
                    s.animationId = inst.Animation.Name;
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

            public override object Clone()
            {

                var clone = new AnimationReference();

                CloneBase(this, clone);

                clone.loopMode = loopMode;
                clone.animation = animation;
                clone.animationAsset = animationAsset;

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
            public CustomAnimationAsset animationAsset;
            public CustomAnimationAsset AnimationAsset => animationAsset;
            public bool IsUsingAsset => AnimationAsset != null;

            [NonSerialized]
            protected IAnimationPlayer m_animationPlayer;
            public IAnimationPlayer AnimationPlayer => m_animationPlayer;

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

                        var child = layer.GetMotionController(blendTreeChildren[a].index);
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

            public override void Initialize(IAnimationLayer layer, IAnimationMotionController parent = null)
            {

                base.Initialize(layer, parent);

                m_animationPlayer = IsUsingAsset ? layer.GetNewAnimationPlayer(AnimationAsset) : layer.GetNewAnimationPlayer(Animation);
                if (m_animationPlayer != null)
                {
                    m_animationPlayer.Paused = false;
                    m_animationPlayer.LoopMode = loopMode;
                }

            }

            public override void SetWeight(float weight)
            {

                base.SetWeight(weight);

                if (m_animationPlayer == null) return;

                m_animationPlayer.Mix = GetWeight();

            }

            public override float GetDuration(IAnimationLayer layer)
            {
                if (AnimationPlayer == null) return 0;
                return AnimationPlayer.LengthInSeconds;

            }

            /// <summary>
            /// Get length of controller scaled by playback speed
            /// </summary>
            public override float GetScaledDuration(IAnimationLayer layer)
            {

                if (AnimationPlayer == null) return 0;

                return AnimationPlayer.LengthInSeconds / GetSpeed(layer.Animator);

            }

            public override float GetTime(IAnimationLayer layer, float addTime = 0)
            {

                if (AnimationPlayer == null) return 0;

                return AnimationPlayer.Time + addTime * GetSpeed(layer.Animator);

            }

            public override void SetTime(IAnimationLayer layer, float time)
            {

                if (AnimationPlayer == null) return;

                AnimationPlayer.Time = time; 

            }

            public override void SetNormalizedTime(IAnimationLayer layer, float normalizedTime)
            {

                if (AnimationPlayer == null) return;

                AnimationPlayer.Time = AnimationPlayer.LengthInSeconds * normalizedTime;

            }

            public override void Progress(IAnimationLayer layer, float deltaTime, ref JobHandle jobHandle, bool useMultithreading = true, bool isFinal = false, bool canLoop = true)
            {

                if (AnimationPlayer is not CustomAnimation.Player cap) return; 

                cap.Speed = GetSpeed(layer.Animator);
                jobHandle = cap.Progress(deltaTime, layer.Mix, jobHandle, useMultithreading, isFinal, canLoop);

            }

            public override bool HasAnimationPlayer(IAnimationLayer layer)
            {

                return AnimationPlayer != null;

            }

        }

        [Serializable]
        public abstract class BlendTree : CustomMotionController, IBlendTree
        {

            public override bool HasChildControllers => true;

            public abstract void SetParameterValues(IAnimationLayer layer);
            public abstract int ParameterCount { get; }
            /// <summary>
            /// If the tree is affected by multiple parameters; localParameterIndex is used to fetch the parameter locally first.
            /// </summary>
            public abstract int GetParameterIndex(int localParameterIndex);

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

            }

            public abstract MotionControllerIdentifier[] GetChildControllerIndices();

            public override bool HasDerivativeHierarchyOf(IAnimationLayer layer, IAnimationMotionController other)
            {

                if (layer == null || other == null) return false;

                var blendTreeChildren = GetChildControllerIndices();
                if (blendTreeChildren == null || blendTreeChildren.Length == 0) return true;

                for (int a = 0; a < blendTreeChildren.Length; a++)
                {

                    var child = layer.GetMotionController(blendTreeChildren[a].index);
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

                    var child = layer.GetMotionController(blendTreeChildren[a].index);
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

        }

        [Serializable]
        public class BlendTree1D : BlendTree, IBlendTree1D
        {

            #region Serialization

            [Serializable]
            public struct Serialized : ISerializableContainer<BlendTree1D, BlendTree1D.Serialized>
            {

                public string name;
                public float baseSpeed;
                public int speedMultiplierParameter;

                public int parameterIndex;
                public MotionField1D[] motionFields;

                public string AsJSON(bool prettyPrint = false) => swole.Engine.ToJson(this, prettyPrint);

                public object AsNonserializableObject(PackageInfo packageInfo = default) => AsOriginalType(packageInfo);
                public BlendTree1D AsOriginalType(PackageInfo packageInfo = default)
                {
                    var anim = new BlendTree1D(name, parameterIndex, motionFields);

                    anim.baseSpeed = baseSpeed;
                    anim.speedMultiplierParameter = speedMultiplierParameter;

                    return anim;
                }

                public static implicit operator Serialized(BlendTree1D inst)
                {
                    Serialized s = new Serialized();

                    s.name = inst.name;
                    s.baseSpeed = inst.baseSpeed;
                    s.speedMultiplierParameter = inst.speedMultiplierParameter;

                    s.parameterIndex = inst.parameterIndex;
                    s.motionFields = inst.motionFields;

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

            public override object Clone()
            {

                var clone = new BlendTree1D();

                CloneBase(this, clone);

                clone.parameterIndex = parameterIndex;
                clone.motionFields = motionFields;

                return clone;

            }

            public override void GetChildIndices(List<int> indices)
            {

                if (motionFields == null || indices == null) return;

                foreach (var field in motionFields) indices.Add(field.controllerIdentifier.index);

            }

            public override void GetChildIndexIdentifiers(List<MotionControllerIdentifier> indices, bool onlyAddIfNotPresent)
            {

                if (motionFields == null || indices == null) return;

                if (onlyAddIfNotPresent)
                {

                    foreach (var field in motionFields) if (!indices.Contains(field.controllerIdentifier)) indices.Add(field.controllerIdentifier);

                }
                else
                {

                    foreach (var field in motionFields) indices.Add(field.controllerIdentifier);

                }

            }

            public override void RemapChildIndices(Dictionary<int, int> remapper, bool invalidateNonRemappedIndices = false)
            {

                if (motionFields == null || remapper == null) return;
                motionFields = (MotionField1D[])motionFields.Clone();

                for (int a = 0; a < motionFields.Length; a++)
                {

                    var field = motionFields[a];

                    if (remapper.TryGetValue(field.controllerIdentifier.index, out int newIndex))
                        field.controllerIdentifier = new MotionControllerIdentifier() { type = field.controllerIdentifier.type, index = newIndex };
                    else if (invalidateNonRemappedIndices)
                        field.controllerIdentifier = new MotionControllerIdentifier() { type = field.controllerIdentifier.type, index = -1 };



                    motionFields[a] = field;

                }

            }

            public override void RemapChildIndices(Dictionary<MotionControllerIdentifier, int> remapper, bool invalidateNonRemappedIndices = false)
            {

                if (motionFields == null || remapper == null) return;
                motionFields = (MotionField1D[])motionFields.Clone();

                for (int a = 0; a < motionFields.Length; a++)
                {

                    var field = motionFields[a];

                    if (remapper.TryGetValue(field.controllerIdentifier, out int newIndex))
                        field.controllerIdentifier = new MotionControllerIdentifier() { type = field.controllerIdentifier.type, index = newIndex };
                    else if (invalidateNonRemappedIndices)
                        field.controllerIdentifier = new MotionControllerIdentifier() { type = field.controllerIdentifier.type, index = -1 };

                    motionFields[a] = field;

                }

            }

            [SerializeField]
            public int parameterIndex;
            public int ParameterIndex 
            {
                get => parameterIndex;
                set => parameterIndex = value;
            }

            public override int ParameterCount => 1;

            public override int GetParameterIndex(int localParameterIndex) => ParameterIndex;

            public override void GetParameterIndices(IAnimationLayer layer, List<int> indices)
            {

                if (indices == null) return;

                base.GetParameterIndices(layer, indices);

                if (motionFields != null)
                {

                    foreach (var field in motionFields)
                    {

                        var controller = layer.GetMotionController(field.controllerIdentifier.index);
                        if (controller == null) continue;

                        controller.GetParameterIndices(layer, indices);

                    }

                }

            }

            public override void RemapParameterIndices(IAnimationLayer layer, Dictionary<int, int> remapper, bool invalidateNonRemappedIndices = false)
            {

                if (remapper == null) return;

                base.RemapParameterIndices(layer, remapper, invalidateNonRemappedIndices);

                if (ParameterIndex >= 0)
                {

                    if (!remapper.TryGetValue(ParameterIndex, out parameterIndex) && invalidateNonRemappedIndices) parameterIndex = -1;

                }

                if (motionFields != null)
                {

                    foreach (var field in motionFields)
                    {

                        var controller = layer.GetMotionController(field.controllerIdentifier.index);
                        if (controller == null) continue;

                        controller.RemapParameterIndices(layer, remapper, invalidateNonRemappedIndices);

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
                    for(int a = 0; a < value.Length; a++)
                    {
                        var mf = value[a];
                        if (mf is MotionField1D mf1d)
                        {
                            motionFields[a] = mf1d;
                        } 
                        else
                        {
                            motionFields[a] = new MotionField1D() { speed = mf.Speed, threshold = mf.Threshold, controllerIdentifier = mf.ControllerIdentifier };
                        }
                    }
                }
            }

            [NonSerialized]
            protected MotionControllerIdentifier[] childIndices;

            public override MotionControllerIdentifier[] GetChildControllerIndices()
            {
                if (childIndices == null)
                {
                    childIndices = new MotionControllerIdentifier[motionFields == null ? 0 : motionFields.Length];
                    for (int a = 0; a < childIndices.Length; a++) childIndices[a] = motionFields[a].controllerIdentifier;
                }

                return childIndices;
            }

            public override AnimationLoopMode GetLoopMode(IAnimationLayer layer)
            {

                AnimationLoopMode loopMode = AnimationLoopMode.Loop;

                if (motionFields != null)
                {

                    foreach (var field in motionFields)
                    {

                        var controller = layer.GetMotionController(field.controllerIdentifier.index);
                        if (controller == null) continue;

                        loopMode = controller.GetLoopMode(layer);
                        break;

                    }

                }

                return loopMode;

            }

            public override void ForceSetLoopMode(IAnimationLayer layer, AnimationLoopMode loopMode)
            {

                if (motionFields != null)
                {

                    foreach (var field in motionFields)
                    {

                        var controller = layer.GetMotionController(field.controllerIdentifier.index);
                        if (controller == null) continue;

                        controller.ForceSetLoopMode(layer, loopMode);

                    }

                }

            }

            public override void Initialize(IAnimationLayer layer, IAnimationMotionController parent = null)
            {

                base.Initialize(layer, parent);

                if (motionFields != null)
                {

                    for (int a = 0; a < motionFields.Length; a++)
                    {

                        var field = motionFields[a];
                        var controller = layer.GetMotionController(motionFields[a].controllerIdentifier.index);

                        if (controller != null)
                        {

                            controller.BaseSpeed = field.speed;
                            controller.Initialize(layer, this);

                        }

                    }

                }

            }

            public override float GetDuration(IAnimationLayer layer) => GetScaledDuration(layer); // Blend tree always uses scaled lengths

            /// <summary>
            /// Get length of controller scaled by playback speed
            /// </summary>
            public override float GetScaledDuration(IAnimationLayer layer)
            {

                if (motionFields == null) return 0;

                float length = 0;

                for (int a = 0; a < motionFields.Length; a++)
                {

                    var motionField = motionFields[a];
                    var controller = layer.GetMotionController(motionField.controllerIdentifier.index);
                    if (controller == null) continue;

                    length = math.max(length, controller.GetScaledDuration(layer)); // Blend tree always uses scaled length because children can have varied playback speeds

                }

                return length;

            }

            public override float GetTime(IAnimationLayer layer, float addTime = 0)
            {

                if (motionFields == null) return 0;

                float time = 0;

                float maxLength = GetScaledDuration(layer);
                for (int a = 0; a < motionFields.Length; a++)
                {

                    var motionField = motionFields[a];
                    var controller = layer.GetMotionController(motionField.controllerIdentifier.index);
                    if (controller == null) continue;
                    float length = controller.GetScaledDuration(layer);
                    float addTimeScaled = length <= 0 ? 0 : (addTime * (maxLength / length)); // Keep add time uniform among all children
                    time = math.max(time, controller.GetTime(layer, addTimeScaled));
                }

                return time;

            }

            public override float GetNormalizedTime(IAnimationLayer layer, float addTime = 0)
            {

                if (motionFields == null) return 0;

                float time = addTime < 0 ? 0 : 1;

                float maxLength = GetScaledDuration(layer);
                for (int a = 0; a < motionFields.Length; a++)
                {

                    var motionField = motionFields[a];
                    var controller = layer.GetMotionController(motionField.controllerIdentifier.index);
                    if (controller == null) continue;
                    float length = controller.GetScaledDuration(layer); 
                    float addTimeScaled = length <= 0 ? 0 : (addTime * (maxLength / length)); // Keep add time uniform among all children
                    time = addTime < 0 ? math.max(time, controller.GetNormalizedTime(layer, addTimeScaled)) : math.min(time, controller.GetNormalizedTime(layer, addTimeScaled));
                }

                return time;

            }

            public override void SetTime(IAnimationLayer layer, float time)
            {

                if (motionFields == null) return;

                float maxLength = GetScaledDuration(layer);
                for (int a = 0; a < motionFields.Length; a++)
                {

                    var motionField = motionFields[a];
                    var controller = layer.GetMotionController(motionField.controllerIdentifier.index);
                    if (controller == null) continue;
                    float length = controller.GetScaledDuration(layer);
                    controller.SetTime(layer, length <= 0 ? 0 : (time * (maxLength / length)));
                }

            }

            public override void SetNormalizedTime(IAnimationLayer layer, float normalizedTime)
            {

                if (motionFields == null) return;

                float maxLength = GetScaledDuration(layer);
                for (int a = 0; a < motionFields.Length; a++)
                {

                    var motionField = motionFields[a];
                    var controller = layer.GetMotionController(motionField.controllerIdentifier.index);
                    if (controller == null) continue;
                    float length = controller.GetScaledDuration(layer);
                    controller.SetNormalizedTime(layer, length <= 0 ? 0 : (normalizedTime * (maxLength / length)));
                }

            }

            public override void SetParameterValues(IAnimationLayer layer)
            {

                if (layer == null || layer.Animator == null || motionFields == null) return;

                float thresholdPosition = 0;
                var parameter = layer.Animator.GetParameter(parameterIndex);
                if (parameter != null) thresholdPosition = parameter.Value;

                int minIndex, maxIndex;
                minIndex = maxIndex = 0;
                for (int a = 0; a < motionFields.Length; a++)
                {

                    var motionField = motionFields[a];
                    var controller = layer.GetMotionController(motionField.controllerIdentifier.index);
                    if (controller == null) continue;

                    if (motionField.threshold > thresholdPosition || a == motionFields.Length - 1)
                    {

                        minIndex = a - 1;
                        maxIndex = a;
                        if (minIndex < 0) minIndex = 0;

                        if (minIndex == maxIndex)
                        {

                            controller.SetWeight(1);

                            if (controller is BlendTree blendTree) blendTree.SetParameterValues(layer);

                        }
                        else
                        {

                            float interp = math.saturate((thresholdPosition - motionFields[minIndex].threshold) / (motionFields[maxIndex].threshold - motionFields[minIndex].threshold));

                            var prevMotionField = motionFields[minIndex];
                            var minController = layer.GetMotionControllerUnsafe(prevMotionField.controllerIdentifier.index);
                            var maxController = layer.GetMotionControllerUnsafe(motionField.controllerIdentifier.index);

                            minController.SetWeight(1 - interp);
                            maxController.SetWeight(interp);

                            if (minController is BlendTree prevBlendTree) prevBlendTree.SetParameterValues(layer);
                            if (maxController is BlendTree blendTree) blendTree.SetParameterValues(layer);

                        }

                        break;

                    }

                    controller.SetWeight(0);
                }

                for (int a = maxIndex + 1; a < motionFields.Length; a++) layer.GetMotionControllerUnsafe(motionFields[a].controllerIdentifier.index).SetWeight(0);

            }

            [NonSerialized]
            protected int finalFieldCached;
            public int FinalFieldIndex => finalFieldCached - 1;
            protected void RefreshFinalField(IAnimationLayer layer)
            {
                for (int a = 0; a < motionFields.Length; a++)
                {

                    var motionField = motionFields[a];
                    var controller = layer.GetMotionController(motionField.controllerIdentifier.index);
                    if (controller == null) continue;

                    finalFieldCached = a + 1;

                }
            }

            public override void Progress(IAnimationLayer layer, float deltaTime, ref JobHandle jobHandle, bool useMultithreading = true, bool isFinal = false, bool canLoop = true)
            {

                if (motionFields == null) return;

                if (finalFieldCached <= 0) RefreshFinalField(layer);

                SetParameterValues(layer);

                if (canLoop)
                {

                    float normalizedTime = GetNormalizedTime(layer, deltaTime);

                    canLoop = deltaTime < 0 && normalizedTime <= 0 || deltaTime >= 0 && normalizedTime >= 1;

                }

                for (int a = 0; a < motionFields.Length; a++)
                {

                    var motionField = motionFields[a];
                    var controller = layer.GetMotionController(motionField.controllerIdentifier.index);
                    if (controller is not CustomMotionController cmc)
                    {
                        if (a == FinalFieldIndex) RefreshFinalField(layer);
                        continue;
                    }

                    cmc.Progress(layer, deltaTime, ref jobHandle, useMultithreading, isFinal && a == FinalFieldIndex, canLoop);

                }

            }

            public override bool HasAnimationPlayer(IAnimationLayer layer)
            {

                if (motionFields == null) return false;

                for (int a = 0; a < motionFields.Length; a++)
                {

                    var motionField = motionFields[a];
                    var controller = layer.GetMotionController(motionField.controllerIdentifier.index);
                    if (controller == null || !controller.HasAnimationPlayer(layer)) return false;

                }

                return true;

            }

        }

        [NonSerialized]
        protected IAnimationMotionController m_parent;
        public IAnimationMotionController Parent => m_parent;

        public virtual void Initialize(IAnimationLayer layer, IAnimationMotionController parent = null)
        {

            if (parent != null) m_parent = parent;

        }

        public float baseSpeed = 1;
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

        public float GetSpeed(IAnimator animator)
        {

            float speed = baseSpeed;

            if (Parent != null) speed = speed * Parent.GetSpeed(animator);

            if (animator == null) return speed;

            var parameter = animator.GetParameter(speedMultiplierParameter);
            if (parameter != null) speed = speed * parameter.Value;

            return speed;

        }

        public virtual AnimationLoopMode GetLoopMode(IAnimationLayer layer) => AnimationLoopMode.Loop;

        public virtual void ForceSetLoopMode(IAnimationLayer layer, AnimationLoopMode loopMode) { }

        [NonSerialized]
        protected float m_weight;

        public virtual void SetWeight(float weight)
        {
            m_weight = weight;
        }

        public float GetWeight()
        {
            float weight = m_weight;
            if (Parent != null) weight = weight * Parent.GetWeight();
            return weight;
        }

        public abstract float GetDuration(IAnimationLayer layer);

        /// <summary>
        /// Get time length of controller scaled by playback speed
        /// </summary>
        public abstract float GetScaledDuration(IAnimationLayer layer);

        public abstract float GetTime(IAnimationLayer layer, float addTime = 0);

        public virtual float GetNormalizedTime(IAnimationLayer layer, float addTime = 0)
        {
            float length = GetDuration(layer);
            if (length <= 0) return 0;

            return GetTime(layer, addTime) / length;
        }

        public abstract void SetTime(IAnimationLayer layer, float time);

        public abstract void SetNormalizedTime(IAnimationLayer layer, float normalizedTime);

        public abstract void Progress(IAnimationLayer layer, float deltaTime, ref JobHandle jobHandle, bool useMultithreading = true, bool isFinal = false, bool canLoop = true);

        public abstract bool HasAnimationPlayer(IAnimationLayer layer);

        public abstract object Clone();

        public virtual bool HasChildControllers => false;

        public virtual void GetChildIndexIdentifiers(List<MotionControllerIdentifier> identifiers, bool onlyAddIfNotPresent = true) { }

        public virtual void RemapChildIndices(Dictionary<MotionControllerIdentifier, int> remapper, bool invalidateNonRemappedIndices = false) { }

        public virtual void GetChildIndices(List<int> indices) { }

        public virtual void RemapChildIndices(Dictionary<int, int> remapper, bool invalidateNonRemappedIndices = false) { }

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

                if (!remapper.TryGetValue(SpeedMultiplierParameter, out speedMultiplierParameter) && invalidateNonRemappedIndices) speedMultiplierParameter = -1;

            }

        }

        public virtual bool HasDerivativeHierarchyOf(IAnimationLayer layer, IAnimationMotionController other) => true;

        public virtual int GetLongestHierarchyIndex(IAnimationLayer layer) => -1;

    }
}

#endif