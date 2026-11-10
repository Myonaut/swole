using System;
using System.Collections.Generic;

namespace Swole.Animation
{
    public interface IAnimationMotionController : ICloneable
    {
        public string Name { get; set; }

        public IAnimationMotionController Parent { get; }
        public bool HasParent { get; }

        public IAnimationLayer Layer { get; }
        public bool IsInitialized { get; }

        public WeightedAvatarMask AvatarMask { get; set; }
        public bool InvertAvatarMask { get; set; }

        public void Initialize(IAnimationLayer layer, List<AvatarMaskUsage> masks, IAnimationMotionController parent = null);

        public void Reset(IAnimationLayer layer);

        public float BaseSpeed { get; set; }

        public int SpeedMultiplierParameter { get; set; }

        public float GetSpeed(IAnimationLayer layer);

        public AnimationLoopMode GetLoopMode(IAnimationLayer layer);

        public void ForceSetLoopMode(IAnimationLayer layer, AnimationLoopMode loopMode);

        public void SetWeight(float weight);
        public float GetWeight();
        public float GetBaseWeight();
        public void SyncWeight(IAnimationLayer layer);

        public float GetDuration(IAnimationLayer layer);
        /// <summary>
        /// Get time length of controller scaled by playback speed
        /// </summary>
        public float GetScaledDuration(IAnimationLayer layer);
        public float GetMaxDuration(IAnimationLayer layer);
        public float GetMaxScaledDuration(IAnimationLayer layer);

        public float GetTime(IAnimationLayer layer, float addTime = 0);
        public float GetNormalizedTime(IAnimationLayer layer, float addTime = 0);

        public float GetPreviousTime(IAnimationLayer layer);
        public float GetPreviousNormalizedTime(IAnimationLayer layer);

        public void SetTime(IAnimationLayer layer, float time, bool resetFlags = true);
        public void SetNormalizedTime(IAnimationLayer layer, float normalizedTime, bool resetFlags = true);

        public void ForceSetPreviousTime(IAnimationLayer layer, float time);
        public void ForceSetPreviousNormalizedTime(IAnimationLayer layer, float normalizedTime);

        public bool HasAnimationPlayer(IAnimationLayer layer);

        public bool HasChildControllers { get; }

        public void GetChildIndices(List<int> indices, bool onlyAddIfNotPresent = true);

        public void RemapChildIndices(Dictionary<int, int> remapper, bool invalidateNonRemappedIndices = false);

        public void RemapChildIndices(Dictionary<int, int> remapper, int minIndex, bool invalidateNonRemappedIndices = false);

        public int GetChildCount();
        public int GetChildIndex(int childIndex);
        public void SetChildIndex(int childIndex, int controllerIndex);

        public HashSet<int> GetActiveParameters(IAnimationLayer layer, HashSet<int> indices);
        public void GetParameterIndices(IAnimationLayer layer, List<int> indices);

        public void RemapParameterIndices(IAnimationLayer layer, Dictionary<int, int> remapper, bool invalidateNonRemappedIndices = false, bool remapChildren = true);

        public float GetBiasedParameterValue(IAnimationLayer layer, int parameterIndex, float defaultValue, bool updateParameter);
        public float GetBiasedParameterValue(IAnimationLayer layer, int parameterIndex, float defaultValue, bool updateParameter, out bool appliedBias);

        public bool HasDerivativeHierarchyOf(IAnimationLayer layer, IAnimationMotionController other);

        public int GetLongestHierarchyIndex(IAnimationLayer layer);

        public void NonAdditivePrepass(bool useMultithreading, IAnimationLayer layer);

        public float GetCurrentBoneHeight(IAnimationLayer layer, string boneName);
        public float GetBoneHeight(IAnimationLayer layer, string boneName, float timeOffset);
        public float GetBoneHeight(IAnimationLayer layer, string boneName, float timeOffset, out bool foundCurve);
        public float GetBoneHeightAtTime(IAnimationLayer layer, string boneName, float time);
        public float GetBoneHeightAtTime(IAnimationLayer layer, string boneName, float time, out bool foundCurve); 

        public bool AffectsTransform(IAnimationLayer layer, string transformName);

        public void ApplyPropertyStates(IAnimationLayer layer);
    }

    public delegate WeightedAvatarMaskComposite AvatarMaskInversionDelegate(WeightedAvatarMaskComposite mask);

    [Serializable]
    public struct AvatarMaskUsage
    {
        public WeightedAvatarMaskComposite mask;
        public bool invertMask;

        public static WeightedAvatarMaskComposite GetCombinedMaskAdditive(ICollection<AvatarMaskUsage> masks, AvatarMaskInversionDelegate maskInversionFunc)
        {
            if (masks == null) return default;

            var result = new WeightedAvatarMaskComposite();

            foreach (var mask in masks)
            {
                var mask_ = mask.mask;
                if (mask.invertMask)
                {
                    if (maskInversionFunc == null)
                    {
                        mask_ = 1f - mask_;
                    }
                    else
                    {
                        mask_ = maskInversionFunc(mask_);
                    }
                }

                result = result + mask_;
            }

            return result;
        }
        public static WeightedAvatarMaskComposite GetCombinedMaskMultiplicative(ICollection<AvatarMaskUsage> masks, AvatarMaskInversionDelegate maskInversionFunc)
        {
            if (masks == null) return default;

            var result = new WeightedAvatarMaskComposite();

            foreach (var mask in masks)
            {
                var mask_ = mask.mask;
                if (mask.invertMask)
                {
                    if (maskInversionFunc == null)
                    {
                        mask_ = 1f - mask_;
                    }
                    else
                    {
                        mask_ = maskInversionFunc(mask_);
                    }
                }

                result = result * mask_;
            }

            return result;
        }
    }

    [Serializable]
    public enum MotionControllerType
    {
        AnimationReference, BlendTree1D, BlendTree2D, MotionComposite
    }

    public interface IAnimationReference : IAnimationMotionController
    {

        public float NormalizedTimeStart { get; set; }

        public AnimationLoopMode LoopMode { get; set; }

        public IAnimationAsset Animation { get; set; }

        public IAnimationPlayer AnimationPlayer { get; }

    }

    [Serializable]
    public enum TimeSyncMode
    {
        None, SyncTime, SyncNormalizedTime
    }

    public interface IMotionPart : IMotionField
    {
        public TimeSyncMode SyncMode { get; set; }
        public int LocalSyncReferenceIndex { get; set; }
        public float Mix { get; set; }
        public int MixParameterIndex { get; set; }
        public float MixParameterMappingMin { get; set; }
        public float MixParameterMappingMax { get; set; }
        public bool ClampMixParameter { get; set; }
        public float GetMix(IAnimationLayer layer);
    }

    [Serializable]
    public class MotionPart : IMotionPart, ICloneable
    {

        public bool replaceBaseSpeed = false;
        public bool ReplaceBaseSpeed
        {
            get => replaceBaseSpeed;
            set => replaceBaseSpeed = value; 
        }
        public float speed = 1f;
        public float Speed
        {
            get => speed;
            set => speed = value;
        }
        public float blendSpeed = 1f;
        public float BlendSpeed
        {
            get => blendSpeed;
            set => blendSpeed = value;
        }
        public int blendSpeedParameterIndex = -1;
        public int BlendSpeedParameterIndex
        {
            get => blendSpeedParameterIndex;
            set => blendSpeedParameterIndex = value;
        }
        public float GetBlendSpeed(IAnimationLayer layer)
        {
            if (blendSpeedParameterIndex < 0) return blendSpeed;
            var param = layer.GetParameter(blendSpeedParameterIndex);
            if (param == null) return blendSpeed;
            return blendSpeed * param.Value;
        }

        public int controllerIndex = -1;
        public int ControllerIndex
        {
            get => controllerIndex;
            set => controllerIndex = value;
        }
        public TimeSyncMode syncMode;
        public TimeSyncMode SyncMode
        {
            get => syncMode;
            set => syncMode = value;
        }
        public int localSyncReferenceIndex = -1;
        public int LocalSyncReferenceIndex
        {
            get => localSyncReferenceIndex;
            set => localSyncReferenceIndex = value;
        }
        public float mix;
        public float Mix
        {
            get => mix;
            set => mix = value;
        }
        public int mixParameterIndex = -1;
        public int MixParameterIndex
        {
            get => mixParameterIndex;
            set => mixParameterIndex = value;
        }
        public float mixParameterMappingMin;
        public float MixParameterMappingMin
        {
            get => mixParameterMappingMin;
            set => mixParameterMappingMin = value;
        }
        public float mixParameterMappingMax;
        public float MixParameterMappingMax
        {
            get => mixParameterMappingMax;
            set => mixParameterMappingMax = value;
        }
        public bool clampMixParameter;
        public bool ClampMixParameter
        {
            get => clampMixParameter;
            set => clampMixParameter = value;
        }
        public float GetMix(IAnimationLayer layer)
        {
            if (mixParameterIndex < 0) return mix;
            var param = layer.GetParameter(mixParameterIndex);
            if (param == null) return mix;

            float paramVal = param.Value;
            bool mappingIsValid = mixParameterMappingMin != mixParameterMappingMax;
            if (mappingIsValid)
            {
                if (clampMixParameter)
                {
                    paramVal = Math.Clamp(paramVal, mixParameterMappingMin, mixParameterMappingMax);
                }

                paramVal = (paramVal - mixParameterMappingMin) / (mixParameterMappingMax - mixParameterMappingMin); 
            }

            return mix * paramVal;
        }

        public float normalizedStartTime = 0f;
        public float NormalizedStartTime
        {
            get => normalizedStartTime;
            set => normalizedStartTime = value;
        }

        public object Clone() => Duplicate();
        public MotionPart Duplicate()
        {
            var clone = new MotionPart();

            clone.speed = speed;
            clone.blendSpeed = blendSpeed;
            clone.blendSpeedParameterIndex = blendSpeedParameterIndex;
            clone.controllerIndex = controllerIndex;
            clone.syncMode = syncMode;
            clone.localSyncReferenceIndex = localSyncReferenceIndex;
            clone.mix = mix;
            clone.mixParameterIndex = mixParameterIndex;
            clone.normalizedStartTime = normalizedStartTime;

            return clone;
        }

    }

    public interface IChildProgressor
    {
        public bool WaitForLoops { get; set; }

        public bool ForceSyncOnProgress { get; set; }

        public int FinalFieldIndex { get; }

        public bool UseDedicatedChildForTime { get; set; }
        public int TimeDedicatedChildIndex { get; set; }
        public float GetDedicatedChildDuration(IAnimationLayer layer);
    }

    public interface IMotionComposite : IAnimationMotionController, IChildProgressor
    {

        public IMotionPart[] BaseMotionParts { get; set; }

        public bool UseBlendSpeed { get; set; }
        public bool BlendSpeedIsAdditive { get; set; }

    }

    [Serializable]
    public enum BlendValuesUpdateLimit
    {
        None, InsideNormalizedTimeRange, OutsideNormalizedTimeRange, FirstProgressCall
    }
    public interface IBlendTree : IAnimationMotionController, IChildProgressor
    {

        public void UpdateBlendValues(IAnimationLayer layer, float normalizedTime, float deltaTime);
        public int ParameterCount { get; }

        /// <summary>
        /// If the tree is affected by multiple parameters; localParameterIndex is used to fetch the parameter locally first.
        /// </summary>
        public int GetParameterIndex(int localParameterIndex);
        public float GetLastParameterValue(int localParameterIndex);
        public void SetLastParameterValue(int localParameterIndex, float value);
        public BlendValuesUpdateLimit GetParameterUpdateLimit(int localParameterIndex);
        public void GetParameterUpdateNormalizedTimeRange(int localParameterIndex, out float timeMin, out float timeMax);

        public int[] GetChildControllerIndices();

        public IMotionField[] BaseMotionFields { get; }

        public bool UseBlendSpeed { get; set; }

    }
    public interface IMotionField 
    {
        public bool ReplaceBaseSpeed { get; set; }
        public float Speed { get; set; }
        public int ControllerIndex { get; set; }
        public float NormalizedStartTime { get; set; }

        public float BlendSpeed { get; set; }
        public int BlendSpeedParameterIndex { get; set; }
        public float GetBlendSpeed(IAnimationLayer layer);
    }

    public interface IBlendTree1D : IBlendTree 
    {

        public int ParameterIndex { get; set; }

        public IMotionField1D[] MotionFields { get; set; }

    }

    public interface IBlendTree2D : IBlendTree
    {

        public int ParameterIndexX { get; set; }
        public int ParameterIndexY { get; set; }

        public IMotionField2D[] MotionFields { get; set; }

    }

    public interface IMotionField1D : IMotionField 
    {
        public float Threshold { get; set; }
    }

    [Serializable]
    public class MotionField1D : IMotionField1D, ICloneable
    {

        public float threshold;
        public float Threshold
        {
            get => threshold;
            set => threshold = value;
        }

        public bool replaceBaseSpeed = false;
        public bool ReplaceBaseSpeed
        {
            get => replaceBaseSpeed;
            set => replaceBaseSpeed = value;
        }
        public float speed = 1f;
        public float Speed
        {
            get => speed;
            set => speed = value;
        }
        public float blendSpeed = 1f;
        public float BlendSpeed
        {
            get => blendSpeed;
            set => blendSpeed = value;
        }
        public int blendSpeedParameterIndex = -1;
        public int BlendSpeedParameterIndex
        {
            get => blendSpeedParameterIndex;
            set => blendSpeedParameterIndex = value;
        }
        public float GetBlendSpeed(IAnimationLayer layer)
        {
            if (blendSpeedParameterIndex < 0) return blendSpeed;
            var param = layer.GetParameter(blendSpeedParameterIndex);
            if (param == null) return blendSpeed;
            return blendSpeed * param.Value;
        }

        public int controllerIndex = -1;
        public int ControllerIndex
        {
            get => controllerIndex;
            set => controllerIndex = value;
        }

        public float normalizedStartTime = 0f;
        public float NormalizedStartTime
        {
            get => normalizedStartTime;
            set => normalizedStartTime = value;
        }

        public object Clone() => Duplicate();
        public MotionField1D Duplicate()
        {
            var clone = new MotionField1D();

            clone.threshold = threshold;
            clone.speed = speed;
            clone.blendSpeed = blendSpeed;
            clone.blendSpeedParameterIndex = blendSpeedParameterIndex;
            clone.controllerIndex = controllerIndex;
            clone.normalizedStartTime = normalizedStartTime;

            return clone;
        }

    }

    public interface IMotionField2D : IMotionField
    {
        public float X { get; set; }
        public float Y { get; set; }
    }

    [Serializable]
    public class MotionField2D : IMotionField2D, ICloneable
    {

        public float x;
        public float X
        {
            get => x;
            set => x = value;
        }

        public float y;
        public float Y
        {
            get => y;
            set => y = value;
        }

        public bool replaceBaseSpeed = false;
        public bool ReplaceBaseSpeed
        {
            get => replaceBaseSpeed;
            set => replaceBaseSpeed = value;
        }
        public float speed = 1f;
        public float Speed
        {
            get => speed;
            set => speed = value;
        }
        public float blendSpeed = 1f;
        public float BlendSpeed
        {
            get => blendSpeed;
            set => blendSpeed = value;
        }
        public int blendSpeedParameterIndex = -1;
        public int BlendSpeedParameterIndex
        {
            get => blendSpeedParameterIndex;
            set => blendSpeedParameterIndex = value;
        }
        public float GetBlendSpeed(IAnimationLayer layer)
        {
            if (blendSpeedParameterIndex < 0) return blendSpeed;
            var param = layer.GetParameter(blendSpeedParameterIndex);
            if (param == null) return blendSpeed;
            return blendSpeed * param.Value;
        }

        public int controllerIndex = -1;
        public int ControllerIndex
        {
            get => controllerIndex;
            set => controllerIndex = value;
        }

        public float normalizedStartTime = 0f;
        public float NormalizedStartTime
        {
            get => normalizedStartTime;
            set => normalizedStartTime = value;
        }

        public object Clone() => Duplicate();
        public MotionField2D Duplicate()
        {
            var clone = new MotionField2D();

            clone.x = x;
            clone.y = y;
            clone.speed = speed;
            clone.blendSpeed = blendSpeed;
            clone.blendSpeedParameterIndex = blendSpeedParameterIndex;
            clone.controllerIndex = controllerIndex;
            clone.normalizedStartTime = normalizedStartTime;

            return clone;
        }

    }

}
