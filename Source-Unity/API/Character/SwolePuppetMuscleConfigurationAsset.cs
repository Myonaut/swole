#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

#if BULKOUT_ENV
using RootMotion;
using RootMotion.Dynamics;
#endif

namespace Swole.API.Unity.Animation
{
    [CreateAssetMenu(fileName = "newPuppetMuscleConfiguration", menuName = "Swole/Character/PuppetMuscleConfiguration", order = 1)]
    public class SwolePuppetMuscleConfigurationAsset : ScriptableObject
    {
        public SwolePuppetMuscleConfiguration configuration;

#if BULKOUT_ENV
        public void Apply(PuppetMaster puppet) => configuration.Apply(puppet); 
#endif

        public static implicit operator SwolePuppetMuscleConfiguration(SwolePuppetMuscleConfigurationAsset asset)
        {
            if (asset == null) return default;
            return asset.configuration;
        }

        [NonSerialized]
        protected SwolePuppetMuscleConfigurationObject contentObject;
        public SwolePuppetMuscleConfigurationObject Content
        {
            get
            {
                if (contentObject == null)
                {
                    contentObject = new SwolePuppetMuscleConfigurationObject();
                    contentObject.configuration = configuration;
                }

                return contentObject;
            }
        }
        public static implicit operator SwolePuppetMuscleConfigurationObject(SwolePuppetMuscleConfigurationAsset asset)
        {
            if (asset == null) return default;
            return asset.Content;
        }

    }

    [Serializable]
    public class SwolePuppetMuscleConfigurationObject : IContent, ICloneable
    {
        #region IContent
        public PackageInfo PackageInfo => throw new NotImplementedException();

        public ContentInfo ContentInfo => throw new NotImplementedException();

        public string Author => throw new NotImplementedException();

        public string CreationDate => throw new NotImplementedException();

        public string LastEditDate => throw new NotImplementedException();

        public string Description => throw new NotImplementedException();

        public string OriginPath => throw new NotImplementedException();

        public string RelativePath => throw new NotImplementedException();

        public bool IsInternalAsset { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public string Name => ContentInfo.name;

        public bool IsValid => configuration.IsValid;

        public Type AssetType => throw new NotImplementedException();

        public object Asset => throw new NotImplementedException();

        public string CollectionID { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool HasCollectionID => throw new NotImplementedException();

        public IContent CreateCopyAndReplaceContentInfo(ContentInfo info)
        {
            throw new NotImplementedException();
        }

        public IContent CreateShallowCopyAndReplaceContentInfo(ContentInfo info)
        {
            throw new NotImplementedException();
        }

        public void Delete()
        {
        }

        public void Dispose()
        {
        }

        public void DisposeSelf()
        {
        }

        public List<PackageIdentifier> ExtractPackageDependencies(List<PackageIdentifier> dependencies = null)
        {
            throw new NotImplementedException();
        }

        public bool IsIdenticalAsset(ISwoleAsset otherAsset)
        {
            throw new NotImplementedException();
        }

        public IContent SetOriginPath(string path)
        {
            throw new NotImplementedException();
        }

        public IContent SetRelativePath(string path)
        {
            throw new NotImplementedException();
        }
        #endregion

        public SwolePuppetMuscleConfiguration configuration;

        public object Clone() => Duplicate();
        public SwolePuppetMuscleConfigurationObject Duplicate()
        {
            var clone = new SwolePuppetMuscleConfigurationObject();

            clone.configuration = configuration;
            if (clone.configuration.muscleStates != null) clone.configuration.muscleStates = configuration.muscleStates.Clone() as SwolePuppetMuscleState[];

            return clone;
        }

#if BULKOUT_ENV
        public void Apply(PuppetMaster puppet) => configuration.Apply(puppet);
#endif

        public static implicit operator SwolePuppetMuscleConfiguration(SwolePuppetMuscleConfigurationObject asset)
        {
            if (asset == null) return default;
            return asset.configuration;
        }
    }

    [Serializable]
    public struct SwolePuppetMuscleConfiguration
    {
        public float unbalancingSpeed;
        public float rebalancingSpeed;

        public SwolePuppetMuscleState[] muscleStates;
        public bool IsValid => muscleStates != null && muscleStates.Length > 0;

        #region Operations

        private static readonly List<SwolePuppetMuscleState> _tempMuscleStates = new List<SwolePuppetMuscleState>();
        private static readonly List<string> _tempNames = new List<string>();
        private static bool PrepareOperation(SwolePuppetMuscleConfiguration a, SwolePuppetMuscleConfiguration b, out SwolePuppetMuscleConfiguration result)
        {
            result = a;

            if (a.muscleStates == null)
            {
                if (b.muscleStates != null)
                {
                    result.muscleStates = b.muscleStates.Clone() as SwolePuppetMuscleState[];
                }

                return false;
            }
            else if (b.muscleStates == null)
            {
                if (a.muscleStates != null)
                {
                    result.muscleStates = a.muscleStates.Clone() as SwolePuppetMuscleState[];
                }

                return false;
            }

            return true;
        }
        public static SwolePuppetMuscleConfiguration operator +(SwolePuppetMuscleConfiguration a, SwolePuppetMuscleConfiguration b)         
        {
            if (PrepareOperation(a, b, out var result))
            {
                _tempMuscleStates.Clear();
                _tempNames.Clear();
                for (int i = 0; i < a.muscleStates.Length; i++)
                {
                    var stateA = a.muscleStates[i];
                    _tempNames.Add(stateA.name);

                    bool flag = false;
                    for (int j = 0; j < b.muscleStates.Length; j++)
                    {
                        var stateB = b.muscleStates[j];
                        if (stateB.name != stateA.name) continue; 

                        var newState = stateA + stateB;
                        newState.cachedIndex = stateA.HasMuscleIndex ? stateA.cachedIndex : stateB.cachedIndex;

                        _tempMuscleStates.Add(newState);
                        
                        flag = true;
                        break;
                    }

                    if (!flag) _tempMuscleStates.Add(stateA);
                }

                for (int i = 0; i < b.muscleStates.Length; i++)
                {
                    var stateB = b.muscleStates[i];
                    if (_tempNames.Contains(stateB.name)) continue;

                    _tempMuscleStates.Add(stateB);
                }

                result.muscleStates = _tempMuscleStates.ToArray();
            }

            result.unbalancingSpeed = a.unbalancingSpeed + b.unbalancingSpeed;
            result.rebalancingSpeed = a.rebalancingSpeed + b.rebalancingSpeed;

            return result;
        }
        public static SwolePuppetMuscleConfiguration operator -(SwolePuppetMuscleConfiguration a, SwolePuppetMuscleConfiguration b)
        {
            if (PrepareOperation(a, b, out var result))
            {
                _tempMuscleStates.Clear();
                _tempNames.Clear();
                for (int i = 0; i < a.muscleStates.Length; i++)
                {
                    var stateA = a.muscleStates[i];
                    _tempNames.Add(stateA.name);

                    bool flag = false;
                    for (int j = 0; j < b.muscleStates.Length; j++)
                    {
                        var stateB = b.muscleStates[j];
                        if (stateB.name != stateA.name) continue;

                        var newState = stateA - stateB;
                        newState.cachedIndex = stateA.HasMuscleIndex ? stateA.cachedIndex : stateB.cachedIndex;

                        _tempMuscleStates.Add(newState);

                        flag = true;
                        break;
                    }

                    if (!flag) _tempMuscleStates.Add(stateA);
                }

                for (int i = 0; i < b.muscleStates.Length; i++)
                {
                    var stateB = b.muscleStates[i];
                    if (_tempNames.Contains(stateB.name)) continue;

                    _tempMuscleStates.Add(-stateB);
                }

                result.muscleStates = _tempMuscleStates.ToArray();
            }

            result.unbalancingSpeed = a.unbalancingSpeed - b.unbalancingSpeed;
            result.rebalancingSpeed = a.rebalancingSpeed - b.rebalancingSpeed;

            return result;
        }

        public static SwolePuppetMuscleConfiguration operator *(SwolePuppetMuscleConfiguration a, SwolePuppetMuscleConfiguration b)
        {
            if (PrepareOperation(a, b, out var result))
            {
                _tempMuscleStates.Clear();
                for (int i = 0; i < a.muscleStates.Length; i++)
                {
                    var stateA = a.muscleStates[i];

                    bool flag = false;
                    for (int j = 0; j < b.muscleStates.Length; j++)
                    {
                        var stateB = b.muscleStates[j];
                        if (stateB.name != stateA.name) continue;

                        var newState = stateA * stateB;
                        newState.cachedIndex = stateA.HasMuscleIndex ? stateA.cachedIndex : stateB.cachedIndex;

                        _tempMuscleStates.Add(newState);

                        flag = true;
                        break;
                    }

                    if (!flag) _tempMuscleStates.Add(stateA * 0f);
                }

                result.muscleStates = _tempMuscleStates.ToArray();
            }

            result.unbalancingSpeed = a.unbalancingSpeed * b.unbalancingSpeed;
            result.rebalancingSpeed = a.rebalancingSpeed * b.rebalancingSpeed;

            return result;
        }
        public static SwolePuppetMuscleConfiguration operator /(SwolePuppetMuscleConfiguration a, SwolePuppetMuscleConfiguration b)
        {
            if (PrepareOperation(a, b, out var result))
            {
                _tempMuscleStates.Clear();
                for (int i = 0; i < a.muscleStates.Length; i++)
                {
                    var stateA = a.muscleStates[i];

                    bool flag = false;
                    for (int j = 0; j < b.muscleStates.Length; j++)
                    {
                        var stateB = b.muscleStates[j];
                        if (stateB.name != stateA.name) continue;

                        var newState = stateA / stateB;
                        newState.cachedIndex = stateA.HasMuscleIndex ? stateA.cachedIndex : stateB.cachedIndex;

                        _tempMuscleStates.Add(newState);

                        flag = true;
                        break;
                    }

                    if (!flag) _tempMuscleStates.Add(stateA);
                }

                result.muscleStates = _tempMuscleStates.ToArray();
            }

            result.unbalancingSpeed = a.unbalancingSpeed / b.unbalancingSpeed;
            result.rebalancingSpeed = a.rebalancingSpeed / b.rebalancingSpeed;

            return result;
        }

        public static SwolePuppetMuscleConfiguration operator +(SwolePuppetMuscleConfiguration a, float b)
        {
            var result = a;

            if (a.muscleStates != null)
            {
                result.muscleStates = new SwolePuppetMuscleState[a.muscleStates.Length];
                for (int i = 0; i < a.muscleStates.Length; i++)
                {
                    result.muscleStates[i] = a.muscleStates[i] + b;
                }
            }

            result.unbalancingSpeed = a.unbalancingSpeed + b;
            result.rebalancingSpeed = a.rebalancingSpeed + b;

            return result;
        }
        public static SwolePuppetMuscleConfiguration operator -(SwolePuppetMuscleConfiguration a, float b)
        {
            var result = a;

            if (a.muscleStates != null)
            {
                result.muscleStates = new SwolePuppetMuscleState[a.muscleStates.Length];
                for (int i = 0; i < a.muscleStates.Length; i++)
                {
                    result.muscleStates[i] = a.muscleStates[i] - b;
                }
            }

            result.unbalancingSpeed = a.unbalancingSpeed - b;
            result.rebalancingSpeed = a.rebalancingSpeed - b;

            return result;
        }
        public static SwolePuppetMuscleConfiguration operator *(SwolePuppetMuscleConfiguration a, float b)
        {
            var result = a;

            if (a.muscleStates != null)
            {
                result.muscleStates = new SwolePuppetMuscleState[a.muscleStates.Length];
                for(int i = 0; i < a.muscleStates.Length; i++)
                {
                    result.muscleStates[i] = a.muscleStates[i] * b;
                }
            }

            result.unbalancingSpeed = a.unbalancingSpeed * b;
            result.rebalancingSpeed = a.rebalancingSpeed * b;

            return result;
        }
        public static SwolePuppetMuscleConfiguration operator /(SwolePuppetMuscleConfiguration a, float b)
        {
            var result = a;

            if (a.muscleStates != null)
            {
                result.muscleStates = new SwolePuppetMuscleState[a.muscleStates.Length];
                for (int i = 0; i < a.muscleStates.Length; i++)
                {
                    result.muscleStates[i] = a.muscleStates[i] / b;
                }
            }

            result.unbalancingSpeed = a.unbalancingSpeed / b;
            result.rebalancingSpeed = a.rebalancingSpeed / b;

            return result;
        }

        public static SwolePuppetMuscleConfiguration operator -(SwolePuppetMuscleConfiguration a)
        {
            var result = a;

            if (a.muscleStates != null)
            {
                result.muscleStates = new SwolePuppetMuscleState[a.muscleStates.Length];
                for (int i = 0; i < a.muscleStates.Length; i++)
                {
                    result.muscleStates[i] = -a.muscleStates[i];
                }
            }

            result.unbalancingSpeed = -a.unbalancingSpeed;
            result.rebalancingSpeed = -a.rebalancingSpeed;

            return result;
        }
        #endregion

        public static SwolePuppetMuscleConfiguration Lerp(SwolePuppetMuscleConfiguration a, SwolePuppetMuscleConfiguration b, float t) => LerpUnclamped(a, b, Mathf.Clamp01(t));
        public static SwolePuppetMuscleConfiguration LerpUnclamped(SwolePuppetMuscleConfiguration a, SwolePuppetMuscleConfiguration b, float t)
        {
            var result = a;

            float t1m = 1f - t;
            if (a.muscleStates != null)
            {
                result.muscleStates = new SwolePuppetMuscleState[a.muscleStates.Length];
                if (b.muscleStates != null)
                {
                    for (int i = 0; i < a.muscleStates.Length; i++)
                    {
                        result.muscleStates[i] = SwolePuppetMuscleState.LerpUnclamped(a.muscleStates[i], i >= b.muscleStates.Length ? default : b.muscleStates[i], t); 
                    }
                }
                else
                {
                    for (int i = 0; i < a.muscleStates.Length; i++)
                    {
                        result.muscleStates[i] = SwolePuppetMuscleState.LerpUnclamped(a.muscleStates[i], default, t);
                    }
                }
            } 
            else if (b.muscleStates != null)
            {
                result.muscleStates = new SwolePuppetMuscleState[b.muscleStates.Length];
                for (int i = 0; i < a.muscleStates.Length; i++)
                {
                    result.muscleStates[i] = SwolePuppetMuscleState.LerpUnclamped(default, b.muscleStates[i], t);
                }
            }

            result.unbalancingSpeed = (a.unbalancingSpeed * t1m) + (b.unbalancingSpeed * t);
            result.rebalancingSpeed = (a.rebalancingSpeed * t1m) + (b.rebalancingSpeed * t);

            return result;
        }

#if BULKOUT_ENV
        public SwolePuppetMuscleConfiguration CacheMuscleIndices(PuppetMaster puppet)
        {
            if (muscleStates == null || puppet == null || puppet.muscles == null) return this;

            var config = this;

            for (int a = 0; a < muscleStates.Length; a++)
            {
                var state = muscleStates[a];

                for (int b = 0; b < puppet.muscles.Length; b++)
                {
                    var muscle = puppet.muscles[b];
                    if (muscle == null || muscle.name != state.name) continue; 

                    state.cachedIndex = b + 1;
                    break;
                }

                muscleStates[a] = state;
            }

            return config;
        }

        public void Apply(PuppetMaster puppet, bool useCachedIndices = true)
        {
            if (muscleStates == null || puppet == null || puppet.muscles == null) return; 

            if (puppet is SwolePuppetMaster spm)
            {
                spm.ValidateCurrentConfiguration(); 

                for (int i = 0; i < muscleStates.Length; i++)
                {
                    var state = muscleStates[i];

                    int muscleIndex = -1;
                    if (useCachedIndices && state.HasMuscleIndex)
                    {
                        muscleIndex = state.MuscleIndex;
                    }
                    else
                    {
                        for (int j = 0; j < puppet.muscles.Length; j++)
                        {
                            var muscle_ = puppet.muscles[j];
                            if (muscle_ == null || muscle_.name != state.name) continue;

                            muscleIndex = j;
                            break;
                        }

                        if (muscleIndex < 0) continue;
                    }

                    var muscle = puppet.muscles[muscleIndex];
                    if (muscle == null) continue;

                    spm.currentConfiguration.muscleStates[muscleIndex] = state;
                    muscle.state = state.Apply(muscle.state);
                }
            } 
            else
            {
                for (int i = 0; i < muscleStates.Length; i++)
                {
                    var state = muscleStates[i];

                    int muscleIndex = -1;
                    if (useCachedIndices && state.HasMuscleIndex)
                    {
                        muscleIndex = state.MuscleIndex;
                    }
                    else
                    {
                        for (int j = 0; j < puppet.muscles.Length; j++)
                        {
                            var muscle_ = puppet.muscles[j];
                            if (muscle_ == null || muscle_.name != state.name) continue;

                            muscleIndex = j;
                            break;
                        }

                        if (muscleIndex < 0) continue;
                    }

                    var muscle = puppet.muscles[muscleIndex];
                    if (muscle == null) continue;

                    muscle.state = state.Apply(muscle.state);
                }
            }
        }

        public void ApplyUsingIndexCache(PuppetMaster puppet)
        {
            if (muscleStates == null || puppet == null || puppet.muscles == null) return;

            if (puppet is SwolePuppetMaster spm)
            {
                spm.ValidateCurrentConfiguration();

                for (int i = 0; i < muscleStates.Length; i++)
                {
                    var state = muscleStates[i];

                    int muscleIndex = state.MuscleIndex;
                    var muscle = puppet.muscles[muscleIndex];
                    if (muscle == null) continue;

                    spm.currentConfiguration.muscleStates[muscleIndex] = state;
                    muscle.state = state.Apply(muscle.state); 
                }
            } 
            else
            {
                for (int i = 0; i < muscleStates.Length; i++)
                {
                    var state = muscleStates[i];

                    var muscle = puppet.muscles[state.MuscleIndex];
                    if (muscle == null) continue;

                    muscle.state = state.Apply(muscle.state);
                }
            }
        }
#endif
    }

    [Serializable]
    public struct SwolePuppetMuscleState
    {
        public string name;
        [NonSerialized]
        public int cachedIndex;
        public int MuscleIndex => cachedIndex - 1;
        public bool HasMuscleIndex => cachedIndex > 0;

        public Vector3 lastAnimatedPosition_ROOTSPACE;
        public Quaternion lastAnimatedRotation_ROOTSPACE;

        /// <summary>
        /// Convenience flag for various operations
        /// </summary>
        [NonSerialized]
        public bool flag;

        /// <summary>
        /// Multiplies the mapping weight of the muscle. 0 = fully mapped to animator, 1 = fully mapped to ragdoll
        /// </summary>
        [Tooltip("Multiplies the mapping weight of the muscle. 0 = fully mapped to animator, 1 = fully mapped to ragdoll")]
        public float mappingWeightMlp;

        /// <summary>
        /// Multiplies the pin weight of the muscle.
        /// </summary>
        [Tooltip("Multiplies the pin weight of the muscle.")]
        public float pinWeightMlp;

        /// <summary>
        /// Multiplies the muscle weight.
        /// </summary>
        [Tooltip("Multiplies the muscle weight.")]
        public float muscleWeightMlp;

        /// <summary>
        /// Multiplies slerp drive's max force.
        /// </summary>
        [Tooltip("Multiplies slerp drive's max force.")]
        public float maxForceMlp;

        /// <summary>
        /// Used by the behaviours to cancel out muscle damper so it could be set to a specific value by muscleDamperAdd.
        /// </summary>
        [Tooltip("Used by the behaviours to cancel out muscle damper so it could be set to a specific value by muscleDamperAdd.")]
        public float muscleDamperMlp;

        /// <summary>
        /// Adds to the muscle's damper value (can't multiply because it might be set to zero).
        /// </summary>
        [Tooltip("Adds to the muscle's damper value (can't multiply because it might be set to zero).")]
        public float muscleDamperAdd;

        /// <summary>
        /// Immunity reduces damage from collisions and hits in some Puppet Behaviours (BehaviourPuppet).
        /// </summary>
        [Tooltip("Immunity reduces damage from collisions and hits in some Puppet Behaviours (BehaviourPuppet).")]
        public float immunity;

        /// <summary>
        /// Larger impulse multiplier makes the muscles deal more damage to the muscles of other characters in some Puppet Behavours (BehaviourPuppet).
        /// </summary>
        [Tooltip("Larger impulse multiplier makes the muscles deal more damage to the muscles of other characters in some Puppet Behavours (BehaviourPuppet).")]
        public float impulseMlp;

        /// <summary>
        /// How much this muscle contributes to the balance of the character.
        /// </summary>
        [Tooltip("How much this muscle contributes to the balance of the character."), Range(0f, 1f)]
        public float balanceContribution;

        /// <summary>
        /// Multiplies the mapping weight of the muscle during collision.
        /// </summary>
        [Tooltip("Multiplies the mapping weight of the muscle during collision.")]
        public float collisionMappingWeightMlp;

        /// <summary>
        /// Multiplies the pin weight of the muscle during collision.
        /// </summary>
        [Tooltip("Multiplies the pin weight of the muscle during collision.")]
        public float collisionPinWeightMlp;

        /// <summary>
        /// Multiplies the muscle weight during collision.
        /// </summary>
        [Tooltip("Multiplies the muscle weight during collision.")]
        public float collisionMuscleWeightMlp;

        /// <summary>
        /// Multiplies slerp drive's max force during collision.
        /// </summary>
        [Tooltip("Multiplies slerp drive's max force during collision.")]
        public float collisionMaxForceMlp;

        /// <summary>
        /// How fast the muscle recovers from collisions (when there are no collisions in effect).
        /// </summary>
        [Tooltip("How fast the muscle recovers from collisions (when there are no collisions in effect).")]
        public float collisionRecoverySpeed;

        /// <summary>
        /// Higher values means slower collision state blending.
        /// </summary>
        [TooltipAttribute("Higher values means slower collision state blending.")]
        public float collisionResistance;

        /// <summary>
        /// How much will collisions with muscles of this group unpin parent muscles?
        /// </summary>
        [TooltipAttribute("How much will collisions with muscles of this group unpin parent muscles?")]
        [Range(0f, 1f)] public float unpinParents;

        /// <summary>
        /// How much will collisions with muscles of this group unpin child muscles?
        /// </summary>
        [TooltipAttribute("How much will collisions with muscles of this group unpin child muscles?")]
        [Range(0f, 1f)] public float unpinChildren;

        /// <summary>
        /// How much will collisions with muscles of this group unpin muscles of the same group?
        /// </summary>
        [TooltipAttribute("How much will collisions with muscles of this group unpin muscles of the same group?")]
        [Range(0f, 1f)] public float unpinGroup;

        /// <summary>
        /// The distance from target that must be reached before contributing to unbalancing. If below this threshold, the muscle will instead contribute to rebalancing.
        /// </summary>
        [TooltipAttribute("The distance from target that must be reached before contributing to unbalancing. If below this threshold, the muscle will instead contribute to rebalancing.")]
        public float unbalanceDistanceThreshold;

        /// <summary>
        /// The maximum distance over the set distance threshold.
        /// </summary>
        [TooltipAttribute("The maximum distance over the set distance threshold.")]
        public float unbalanceDistanceRange;

        /// <summary>
        /// The amount of unbalancing contributed when the distance from target is greater than the unbalanceDistanceThreshold.
        /// </summary>
        [TooltipAttribute("The amount of unbalancing contributed when the distance from target is greater than the unbalanceDistanceThreshold.")]
        public float unbalancingContribution;

        /// <summary>
        /// The amount of rebalancing contributed when the distance from target is less than the unbalanceDistanceThreshold.
        /// </summary>
        [TooltipAttribute("The amount of rebalancing contributed when the distance from target is less than the unbalanceDistanceThreshold.")]
        public float rebalancingContribution;

        /// <summary>
        /// The angle difference from target that must be reached before contributing to unbalancing. If below this threshold, the muscle will instead contribute to rebalancing.
        /// </summary>
        [TooltipAttribute("The angle difference from target that must be reached before contributing to unbalancing. If below this threshold, the muscle will instead contribute to rebalancing.")]
        public float unbalanceAngleThreshold;

        /// <summary>
        /// The maximum angle over the set angle threshold.
        /// </summary>
        [TooltipAttribute("The maximum angle over the set angle threshold.")]
        public float unbalanceAngleRange;

        /// <summary>
        /// The amount of unbalancing contributed when the angle from target is greater than the unbalanceAngleThreshold.
        /// </summary>
        [TooltipAttribute("The amount of unbalancing contributed when the angle from target is greater than the unbalanceAngleThreshold.")]
        public float unbalancingAngleContribution;

        /// <summary>
        /// The amount of rebalancing contributed when the angle from target is less than the unbalanceAngleThreshold.
        /// </summary>
        [TooltipAttribute("The amount of rebalancing contributed when the angle from target is less than the unbalanceAngleThreshold.")]
        public float rebalancingAngleContribution;

        /// <summary>
        /// Should the muscle's colliders be disabled?
        /// </summary>
        [TooltipAttribute("Should the muscle's colliders be disabled?")]
        public float disableColliders;
        /// <summary>
        /// Should the muscle's colliders be disabled when the muscle is unpinned?
        /// </summary>
        [TooltipAttribute("Should the muscle's colliders be disabled when the muscle is unpinned?")]
        public float disableUnpinnedColliders;
        /// <summary>
        /// The max pinning force that is considered 'unpinned' when disabling/enabling colliders.
        /// </summary>
        [TooltipAttribute("The max pinning force that is considered 'unpinned' when disabling/enabling colliders.")]
        public float disableCollidersUnpinThreshold;

#if BULKOUT_ENV
        public static implicit operator RootMotion.Dynamics.Muscle.State(SwolePuppetMuscleState state)
        {
            var state_ = RootMotion.Dynamics.Muscle.State.Default;

            state_.mappingWeightMlp = state.mappingWeightMlp;
            state_.pinWeightMlp = state.pinWeightMlp;
            state_.muscleWeightMlp = state.muscleWeightMlp;
            state_.maxForceMlp = state.maxForceMlp;
            state_.muscleDamperMlp = state.muscleDamperMlp;
            state_.muscleDamperAdd = state.muscleDamperAdd;
            state_.immunity = state.immunity;
            state_.impulseMlp = state.impulseMlp;

            return state_;
        }
        public static implicit operator SwolePuppetMuscleState(RootMotion.Dynamics.Muscle.State state)
        {
            var state_ = new SwolePuppetMuscleState();

            state_.mappingWeightMlp = state.mappingWeightMlp;
            state_.pinWeightMlp = state.pinWeightMlp;
            state_.muscleWeightMlp = state.muscleWeightMlp;
            state_.maxForceMlp = state.maxForceMlp;
            state_.muscleDamperMlp = state.muscleDamperMlp;
            state_.muscleDamperAdd = state.muscleDamperAdd;
            state_.immunity = state.immunity;
            state_.impulseMlp = state.impulseMlp;

            return state_;
        }
        public SwolePuppetMuscleState CopyState(RootMotion.Dynamics.Muscle.State state)
        {
            this.mappingWeightMlp = state.mappingWeightMlp;
            this.pinWeightMlp = state.pinWeightMlp;
            this.muscleWeightMlp = state.muscleWeightMlp;
            this.maxForceMlp = state.maxForceMlp;
            this.muscleDamperMlp = state.muscleDamperMlp;
            this.muscleDamperAdd = state.muscleDamperAdd;
            this.immunity = state.immunity;
            this.impulseMlp = state.impulseMlp;

            return this;
        }

        public RootMotion.Dynamics.Muscle.State Apply(RootMotion.Dynamics.Muscle.State state)
        {
            state.mappingWeightMlp = this.mappingWeightMlp;
            state.pinWeightMlp = this.pinWeightMlp;
            state.muscleWeightMlp = this.muscleWeightMlp;
            state.maxForceMlp = this.maxForceMlp;
            state.muscleDamperMlp = this.muscleDamperMlp;
            state.muscleDamperAdd = this.muscleDamperAdd;
            state.immunity = this.immunity;
            state.impulseMlp = this.impulseMlp;

            return state;
        }
        public RootMotion.Dynamics.Muscle.State Apply(RootMotion.Dynamics.Muscle.State state, float collisionBlend)
        {
            state = Apply(state); 

            state.mappingWeightMlp = Mathf.LerpUnclamped(state.mappingWeightMlp, this.collisionMappingWeightMlp, collisionBlend);
            state.pinWeightMlp = Mathf.LerpUnclamped(state.pinWeightMlp, this.collisionPinWeightMlp, collisionBlend);
            state.muscleWeightMlp = Mathf.LerpUnclamped(state.muscleWeightMlp, this.collisionMuscleWeightMlp, collisionBlend);
            state.maxForceMlp = Mathf.LerpUnclamped(state.maxForceMlp, this.collisionMaxForceMlp, collisionBlend); 

            return state;
        }

        public RootMotion.Dynamics.Muscle.State Apply(float globalPinWeight, RootMotion.Dynamics.Muscle muscle, RootMotion.Dynamics.Muscle.State state)
        {
            muscle.state = state;

            var colliders = muscle.colliders;
            if (colliders != null)
            {
                bool collidersState = true;
                float pinning = globalPinWeight * muscle.props.pinWeight * pinWeightMlp;
                if (pinning <= disableCollidersUnpinThreshold)
                {
                    collidersState = !(disableUnpinnedColliders > 0.5f);
                }
                else
                {
                    collidersState = !(disableColliders > 0.5f);
                }

                /*for(int a = 0; a < colliders.Length; a++)
                {
                    var collider = colliders[a];
                    if (collider == null) continue;

                    if (collider.enabled != collidersState)
                    {
                        collider.enabled = collidersState;
                    }
                    if (!collidersState)
                    {
                        Debug.Log($"Disabling colliders for {muscle.name}");
                    }
                }*/
                if (collidersState)
                {
                    muscle.EnableColliders();  
                }
                else
                {
                    muscle.DisableColliders();
                } 
            }

            return state;
        }
        public RootMotion.Dynamics.Muscle.State Apply(float globalPinWeight, RootMotion.Dynamics.Muscle muscle) => Apply(globalPinWeight, muscle, Apply(muscle.state));
        public RootMotion.Dynamics.Muscle.State Apply(float globalPinWeight, RootMotion.Dynamics.Muscle muscle, float collisionBlend) => Apply(globalPinWeight, muscle, Apply(muscle.state, collisionBlend));

        #region Operations

        public static SwolePuppetMuscleState operator +(SwolePuppetMuscleState a, SwolePuppetMuscleState b)
        {
            var result = a;

            result.mappingWeightMlp = a.mappingWeightMlp + b.mappingWeightMlp;
            result.pinWeightMlp = a.pinWeightMlp + b.pinWeightMlp;
            result.muscleWeightMlp = a.muscleWeightMlp + b.muscleWeightMlp;
            result.maxForceMlp = a.maxForceMlp + b.maxForceMlp;
            result.muscleDamperMlp = a.muscleDamperMlp + b.muscleDamperMlp;
            result.muscleDamperAdd = a.muscleDamperAdd + b.muscleDamperAdd;
            result.immunity = a.immunity + b.immunity;
            result.impulseMlp = a.impulseMlp + b.impulseMlp;
            result.balanceContribution = a.balanceContribution + b.balanceContribution;
            
            result.collisionMappingWeightMlp = a.collisionMappingWeightMlp + b.collisionMappingWeightMlp;
            result.collisionPinWeightMlp = a.collisionPinWeightMlp + b.collisionPinWeightMlp;
            result.collisionMuscleWeightMlp = a.collisionMuscleWeightMlp + b.collisionMuscleWeightMlp;
            result.collisionMaxForceMlp = a.collisionMaxForceMlp + b.collisionMaxForceMlp;
            result.collisionRecoverySpeed = a.collisionRecoverySpeed + b.collisionRecoverySpeed;
            result.collisionResistance = a.collisionResistance + b.collisionResistance;

            result.unpinParents = a.unpinParents + b.unpinParents;
            result.unpinChildren = a.unpinChildren + b.unpinChildren;
            result.unpinGroup = a.unpinGroup + b.unpinGroup;

            result.unbalanceDistanceThreshold = a.unbalanceDistanceThreshold + b.unbalanceDistanceThreshold;
            result.unbalanceDistanceRange = a.unbalanceDistanceRange + b.unbalanceDistanceRange;
            result.unbalancingContribution = a.unbalancingContribution + b.unbalancingContribution;
            result.rebalancingContribution = a.rebalancingContribution + b.rebalancingContribution;

            result.unbalanceAngleThreshold = a.unbalanceAngleThreshold + b.unbalanceAngleThreshold; 
            result.unbalanceAngleRange = a.unbalanceAngleRange + b.unbalanceAngleRange;
            result.unbalancingAngleContribution = a.unbalancingAngleContribution + b.unbalancingAngleContribution;
            result.rebalancingAngleContribution = a.rebalancingAngleContribution + b.rebalancingAngleContribution;

            result.disableColliders = a.disableColliders + b.disableColliders;
            result.disableUnpinnedColliders = a.disableUnpinnedColliders + b.disableUnpinnedColliders;
            result.disableCollidersUnpinThreshold = a.disableCollidersUnpinThreshold + b.disableCollidersUnpinThreshold;

            return result;
        }
        public static SwolePuppetMuscleState operator -(SwolePuppetMuscleState a, SwolePuppetMuscleState b)
        {
            var result = a;

            result.mappingWeightMlp = a.mappingWeightMlp - b.mappingWeightMlp;
            result.pinWeightMlp = a.pinWeightMlp - b.pinWeightMlp;
            result.muscleWeightMlp = a.muscleWeightMlp - b.muscleWeightMlp;
            result.maxForceMlp = a.maxForceMlp - b.maxForceMlp;
            result.muscleDamperMlp = a.muscleDamperMlp - b.muscleDamperMlp;
            result.muscleDamperAdd = a.muscleDamperAdd - b.muscleDamperAdd;
            result.immunity = a.immunity - b.immunity;
            result.impulseMlp = a.impulseMlp - b.impulseMlp;
            result.balanceContribution = a.balanceContribution - b.balanceContribution;
            
            result.collisionMappingWeightMlp = a.collisionMappingWeightMlp - b.collisionMappingWeightMlp;
            result.collisionPinWeightMlp = a.collisionPinWeightMlp - b.collisionPinWeightMlp;
            result.collisionMuscleWeightMlp = a.collisionMuscleWeightMlp - b.collisionMuscleWeightMlp;
            result.collisionMaxForceMlp = a.collisionMaxForceMlp - b.collisionMaxForceMlp;
            result.collisionRecoverySpeed = a.collisionRecoverySpeed - b.collisionRecoverySpeed;
            result.collisionResistance = a.collisionResistance - b.collisionResistance;

            result.unpinParents = a.unpinParents - b.unpinParents;
            result.unpinChildren = a.unpinChildren - b.unpinChildren;
            result.unpinGroup = a.unpinGroup - b.unpinGroup;

            result.unbalanceDistanceThreshold = a.unbalanceDistanceThreshold - b.unbalanceDistanceThreshold;
            result.unbalanceDistanceRange = a.unbalanceDistanceRange - b.unbalanceDistanceRange;
            result.unbalancingContribution = a.unbalancingContribution - b.unbalancingContribution;
            result.rebalancingContribution = a.rebalancingContribution - b.rebalancingContribution;

            result.unbalanceAngleThreshold = a.unbalanceAngleThreshold - b.unbalanceAngleThreshold;
            result.unbalanceAngleRange = a.unbalanceAngleRange - b.unbalanceAngleRange;
            result.unbalancingAngleContribution = a.unbalancingAngleContribution - b.unbalancingAngleContribution;
            result.rebalancingAngleContribution = a.rebalancingAngleContribution - b.rebalancingAngleContribution;

            result.disableColliders = a.disableColliders - b.disableColliders;
            result.disableUnpinnedColliders = a.disableUnpinnedColliders - b.disableUnpinnedColliders;
            result.disableCollidersUnpinThreshold = a.disableCollidersUnpinThreshold - b.disableCollidersUnpinThreshold;

            return result;
        }
        public static SwolePuppetMuscleState operator *(SwolePuppetMuscleState a, SwolePuppetMuscleState b)
        {
            var result = a;

            result.mappingWeightMlp = a.mappingWeightMlp * b.mappingWeightMlp;
            result.pinWeightMlp = a.pinWeightMlp * b.pinWeightMlp;
            result.muscleWeightMlp = a.muscleWeightMlp * b.muscleWeightMlp;
            result.maxForceMlp = a.maxForceMlp * b.maxForceMlp;
            result.muscleDamperMlp = a.muscleDamperMlp * b.muscleDamperMlp;
            result.muscleDamperAdd = a.muscleDamperAdd * b.muscleDamperAdd;
            result.immunity = a.immunity * b.immunity;
            result.impulseMlp = a.impulseMlp * b.impulseMlp;
            result.balanceContribution = a.balanceContribution * b.balanceContribution;
            
            result.collisionMappingWeightMlp = a.collisionMappingWeightMlp * b.collisionMappingWeightMlp;
            result.collisionPinWeightMlp = a.collisionPinWeightMlp * b.collisionPinWeightMlp;
            result.collisionMuscleWeightMlp = a.collisionMuscleWeightMlp * b.collisionMuscleWeightMlp;
            result.collisionMaxForceMlp = a.collisionMaxForceMlp * b.collisionMaxForceMlp;
            result.collisionRecoverySpeed = a.collisionRecoverySpeed * b.collisionRecoverySpeed;
            result.collisionResistance = a.collisionResistance * b.collisionResistance;

            result.unpinParents = a.unpinParents * b.unpinParents;
            result.unpinChildren = a.unpinChildren * b.unpinChildren;
            result.unpinGroup = a.unpinGroup * b.unpinGroup;

            result.unbalanceDistanceThreshold = a.unbalanceDistanceThreshold * b.unbalanceDistanceThreshold;
            result.unbalanceDistanceRange = a.unbalanceDistanceRange * b.unbalanceDistanceRange;
            result.unbalancingContribution = a.unbalancingContribution * b.unbalancingContribution;
            result.rebalancingContribution = a.rebalancingContribution * b.rebalancingContribution;

            result.unbalanceAngleThreshold = a.unbalanceAngleThreshold * b.unbalanceAngleThreshold;
            result.unbalanceAngleRange = a.unbalanceAngleRange * b.unbalanceAngleRange;
            result.unbalancingAngleContribution = a.unbalancingAngleContribution * b.unbalancingAngleContribution;
            result.rebalancingAngleContribution = a.rebalancingAngleContribution * b.rebalancingAngleContribution;

            result.disableColliders = a.disableColliders * b.disableColliders;
            result.disableUnpinnedColliders = a.disableUnpinnedColliders * b.disableUnpinnedColliders;
            result.disableCollidersUnpinThreshold = a.disableCollidersUnpinThreshold * b.disableCollidersUnpinThreshold;

            return result;
        }
        public static SwolePuppetMuscleState operator /(SwolePuppetMuscleState a, SwolePuppetMuscleState b)
        {
            var result = a;

            result.mappingWeightMlp = a.mappingWeightMlp / b.mappingWeightMlp;
            result.pinWeightMlp = a.pinWeightMlp / b.pinWeightMlp;
            result.muscleWeightMlp = a.muscleWeightMlp / b.muscleWeightMlp;
            result.maxForceMlp = a.maxForceMlp / b.maxForceMlp;
            result.muscleDamperMlp = a.muscleDamperMlp / b.muscleDamperMlp;
            result.muscleDamperAdd = a.muscleDamperAdd / b.muscleDamperAdd;
            result.immunity = a.immunity / b.immunity;
            result.impulseMlp = a.impulseMlp / b.impulseMlp;
            result.balanceContribution = a.balanceContribution / b.balanceContribution;
            
            result.collisionMappingWeightMlp = a.collisionMappingWeightMlp / b.collisionMappingWeightMlp;
            result.collisionPinWeightMlp = a.collisionPinWeightMlp / b.collisionPinWeightMlp;
            result.collisionMuscleWeightMlp = a.collisionMuscleWeightMlp / b.collisionMuscleWeightMlp;
            result.collisionMaxForceMlp = a.collisionMaxForceMlp / b.collisionMaxForceMlp;
            result.collisionRecoverySpeed = a.collisionRecoverySpeed / b.collisionRecoverySpeed;
            result.collisionResistance = a.collisionResistance / b.collisionResistance;

            result.unpinParents = a.unpinParents / b.unpinParents;
            result.unpinChildren = a.unpinChildren / b.unpinChildren;
            result.unpinGroup = a.unpinGroup / b.unpinGroup;

            result.unbalanceDistanceThreshold = a.unbalanceDistanceThreshold / b.unbalanceDistanceThreshold;
            result.unbalanceDistanceRange = a.unbalanceDistanceRange / b.unbalanceDistanceRange;
            result.unbalancingContribution = a.unbalancingContribution / b.unbalancingContribution;
            result.rebalancingContribution = a.rebalancingContribution / b.rebalancingContribution;

            result.unbalanceAngleThreshold = a.unbalanceAngleThreshold / b.unbalanceAngleThreshold;
            result.unbalanceAngleRange = a.unbalanceAngleRange / b.unbalanceAngleRange;
            result.unbalancingAngleContribution = a.unbalancingAngleContribution / b.unbalancingAngleContribution;
            result.rebalancingAngleContribution = a.rebalancingAngleContribution / b.rebalancingAngleContribution;

            result.disableColliders = a.disableColliders / b.disableColliders;
            result.disableUnpinnedColliders = a.disableUnpinnedColliders / b.disableUnpinnedColliders;
            result.disableCollidersUnpinThreshold = a.disableCollidersUnpinThreshold / b.disableCollidersUnpinThreshold;

            return result;
        }

        public static SwolePuppetMuscleState operator +(SwolePuppetMuscleState a, float b)
        {
            var result = a;

            result.mappingWeightMlp = a.mappingWeightMlp + b;
            result.pinWeightMlp = a.pinWeightMlp + b;
            result.muscleWeightMlp = a.muscleWeightMlp + b;
            result.maxForceMlp = a.maxForceMlp + b;
            result.muscleDamperMlp = a.muscleDamperMlp + b;
            result.muscleDamperAdd = a.muscleDamperAdd + b;
            result.immunity = a.immunity + b;
            result.impulseMlp = a.impulseMlp + b;
            result.balanceContribution = a.balanceContribution + b;

            result.collisionMappingWeightMlp = a.collisionMappingWeightMlp + b;
            result.collisionPinWeightMlp = a.collisionPinWeightMlp + b;
            result.collisionMuscleWeightMlp = a.collisionMuscleWeightMlp + b;
            result.collisionMaxForceMlp = a.collisionMaxForceMlp + b;
            result.collisionRecoverySpeed = a.collisionRecoverySpeed + b;
            result.collisionResistance = a.collisionResistance + b;

            result.unpinParents = a.unpinParents + b;
            result.unpinChildren = a.unpinChildren + b;
            result.unpinGroup = a.unpinGroup + b;

            result.unbalanceDistanceThreshold = a.unbalanceDistanceThreshold + b;
            result.unbalanceDistanceRange = a.unbalanceDistanceRange + b;
            result.unbalancingContribution = a.unbalancingContribution + b;
            result.rebalancingContribution = a.rebalancingContribution + b;

            result.unbalanceAngleThreshold = a.unbalanceAngleThreshold + b;
            result.unbalanceAngleRange = a.unbalanceAngleRange + b;
            result.unbalancingAngleContribution = a.unbalancingAngleContribution + b;
            result.rebalancingAngleContribution = a.rebalancingAngleContribution + b;

            result.disableColliders = a.disableColliders + b;
            result.disableUnpinnedColliders = a.disableUnpinnedColliders + b;
            result.disableCollidersUnpinThreshold = a.disableCollidersUnpinThreshold + b;

            return result;
        }
        public static SwolePuppetMuscleState operator -(SwolePuppetMuscleState a, float b)
        {
            var result = a;

            result.mappingWeightMlp = a.mappingWeightMlp - b;
            result.pinWeightMlp = a.pinWeightMlp - b;
            result.muscleWeightMlp = a.muscleWeightMlp - b;
            result.maxForceMlp = a.maxForceMlp - b;
            result.muscleDamperMlp = a.muscleDamperMlp - b;
            result.muscleDamperAdd = a.muscleDamperAdd - b;
            result.immunity = a.immunity - b;
            result.impulseMlp = a.impulseMlp - b;
            result.balanceContribution = a.balanceContribution - b; 
            
            result.collisionMappingWeightMlp = a.collisionMappingWeightMlp - b;
            result.collisionPinWeightMlp = a.collisionPinWeightMlp - b;
            result.collisionMuscleWeightMlp = a.collisionMuscleWeightMlp - b;
            result.collisionMaxForceMlp = a.collisionMaxForceMlp - b;
            result.collisionRecoverySpeed = a.collisionRecoverySpeed - b;
            result.collisionResistance = a.collisionResistance - b;

            result.unpinParents = a.unpinParents - b;
            result.unpinChildren = a.unpinChildren - b;
            result.unpinGroup = a.unpinGroup - b;

            result.unbalanceDistanceThreshold = a.unbalanceDistanceThreshold - b;
            result.unbalanceDistanceRange = a.unbalanceDistanceRange - b;
            result.unbalancingContribution = a.unbalancingContribution - b;
            result.rebalancingContribution = a.rebalancingContribution - b;

            result.unbalanceAngleThreshold = a.unbalanceAngleThreshold - b;
            result.unbalanceAngleRange = a.unbalanceAngleRange - b;
            result.unbalancingAngleContribution = a.unbalancingAngleContribution - b;
            result.rebalancingAngleContribution = a.rebalancingAngleContribution - b;

            result.disableColliders = a.disableColliders - b;
            result.disableUnpinnedColliders = a.disableUnpinnedColliders - b;
            result.disableCollidersUnpinThreshold = a.disableCollidersUnpinThreshold - b;

            return result;
        }
        public static SwolePuppetMuscleState operator *(SwolePuppetMuscleState a, float b)
        {
            var result = a;

            result.mappingWeightMlp = a.mappingWeightMlp * b;
            result.pinWeightMlp = a.pinWeightMlp * b;
            result.muscleWeightMlp = a.muscleWeightMlp * b;
            result.maxForceMlp = a.maxForceMlp * b;
            result.muscleDamperMlp = a.muscleDamperMlp * b;
            result.muscleDamperAdd = a.muscleDamperAdd * b;
            result.immunity = a.immunity * b;
            result.impulseMlp = a.impulseMlp * b;
            result.balanceContribution = a.balanceContribution * b;
            
            result.collisionMappingWeightMlp = a.collisionMappingWeightMlp * b;
            result.collisionPinWeightMlp = a.collisionPinWeightMlp * b;
            result.collisionMuscleWeightMlp = a.collisionMuscleWeightMlp * b;
            result.collisionMaxForceMlp = a.collisionMaxForceMlp * b;
            result.collisionRecoverySpeed = a.collisionRecoverySpeed * b;
            result.collisionResistance = a.collisionResistance * b;

            result.unpinParents = a.unpinParents * b;
            result.unpinChildren = a.unpinChildren * b;
            result.unpinGroup = a.unpinGroup * b;

            result.unbalanceDistanceThreshold = a.unbalanceDistanceThreshold * b;
            result.unbalanceDistanceRange = a.unbalanceDistanceRange * b;
            result.unbalancingContribution = a.unbalancingContribution * b;
            result.rebalancingContribution = a.rebalancingContribution * b;

            result.unbalanceAngleThreshold = a.unbalanceAngleThreshold * b;
            result.unbalanceAngleRange = a.unbalanceAngleRange * b;
            result.unbalancingAngleContribution = a.unbalancingAngleContribution * b;
            result.rebalancingAngleContribution = a.rebalancingAngleContribution * b;

            result.disableColliders = a.disableColliders * b;
            result.disableUnpinnedColliders = a.disableUnpinnedColliders * b;
            result.disableCollidersUnpinThreshold = a.disableCollidersUnpinThreshold * b;

            return result;
        }
        public static SwolePuppetMuscleState operator /(SwolePuppetMuscleState a, float b)
        {
            var result = a;

            result.mappingWeightMlp = a.mappingWeightMlp / b;
            result.pinWeightMlp = a.pinWeightMlp / b;
            result.muscleWeightMlp = a.muscleWeightMlp / b;
            result.maxForceMlp = a.maxForceMlp / b;
            result.muscleDamperMlp = a.muscleDamperMlp / b;
            result.muscleDamperAdd = a.muscleDamperAdd / b;
            result.immunity = a.immunity / b;
            result.impulseMlp = a.impulseMlp / b;
            result.balanceContribution = a.balanceContribution / b;
            
            result.collisionMappingWeightMlp = a.collisionMappingWeightMlp / b;
            result.collisionPinWeightMlp = a.collisionPinWeightMlp / b;
            result.collisionMuscleWeightMlp = a.collisionMuscleWeightMlp / b;
            result.collisionMaxForceMlp = a.collisionMaxForceMlp / b;
            result.collisionRecoverySpeed = a.collisionRecoverySpeed / b;
            result.collisionResistance = a.collisionResistance / b;

            result.unpinParents = a.unpinParents / b;
            result.unpinChildren = a.unpinChildren / b;
            result.unpinGroup = a.unpinGroup / b;

            result.unbalanceDistanceThreshold = a.unbalanceDistanceThreshold / b;
            result.unbalanceDistanceRange = a.unbalanceDistanceRange / b;
            result.unbalancingContribution = a.unbalancingContribution / b;
            result.rebalancingContribution = a.rebalancingContribution / b;

            result.unbalanceAngleThreshold = a.unbalanceAngleThreshold / b;
            result.unbalanceAngleRange = a.unbalanceAngleRange / b;
            result.unbalancingAngleContribution = a.unbalancingAngleContribution / b;
            result.rebalancingAngleContribution = a.rebalancingAngleContribution / b;

            result.disableColliders = a.disableColliders / b;
            result.disableUnpinnedColliders = a.disableUnpinnedColliders / b;
            result.disableCollidersUnpinThreshold = a.disableCollidersUnpinThreshold / b;

            return result;
        }

        public static SwolePuppetMuscleState operator -(SwolePuppetMuscleState a)
        {
            var result = a;

            result.mappingWeightMlp = -a.mappingWeightMlp;
            result.pinWeightMlp = -a.pinWeightMlp;
            result.muscleWeightMlp = -a.muscleWeightMlp;
            result.maxForceMlp = -a.maxForceMlp;
            result.muscleDamperMlp = -a.muscleDamperMlp;
            result.muscleDamperAdd = -a.muscleDamperAdd;
            result.immunity = -a.immunity;
            result.impulseMlp = -a.impulseMlp;
            result.balanceContribution = -a.balanceContribution;
            
            result.collisionMappingWeightMlp = -a.collisionMappingWeightMlp;
            result.collisionPinWeightMlp = -a.collisionPinWeightMlp;
            result.collisionMuscleWeightMlp = -a.collisionMuscleWeightMlp;
            result.collisionMaxForceMlp = -a.collisionMaxForceMlp;
            result.collisionRecoverySpeed = -a.collisionRecoverySpeed;
            result.collisionResistance = -a.collisionResistance;

            result.unpinParents = -a.unpinParents;
            result.unpinChildren = -a.unpinChildren;
            result.unpinGroup = -a.unpinGroup;

            result.unbalanceDistanceThreshold = -a.unbalanceDistanceThreshold;
            result.unbalanceDistanceRange = -a.unbalanceDistanceRange;
            result.unbalancingContribution = -a.unbalancingContribution;
            result.rebalancingContribution = -a.rebalancingContribution;

            result.unbalanceAngleThreshold = -a.unbalanceAngleThreshold;
            result.unbalanceAngleRange = -a.unbalanceAngleRange;
            result.unbalancingAngleContribution = -a.unbalancingAngleContribution;
            result.rebalancingAngleContribution = -a.rebalancingAngleContribution;

            result.disableColliders = -a.disableColliders;
            result.disableUnpinnedColliders = -a.disableUnpinnedColliders;
            result.disableCollidersUnpinThreshold = -a.disableCollidersUnpinThreshold;

            return result;
        }
        #endregion

        public static SwolePuppetMuscleState Lerp(SwolePuppetMuscleState a, SwolePuppetMuscleState b, float t) => LerpUnclamped(a, b, Mathf.Clamp01(t));
        public static SwolePuppetMuscleState LerpUnclamped(SwolePuppetMuscleState a, SwolePuppetMuscleState b, float t) => (a * (1f - t)) + (b * t);
        
#endif
    }
}

#endif