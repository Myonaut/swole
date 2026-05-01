#if (UNITY_EDITOR || UNITY_STANDALONE)

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using UnityEngine.EventSystems;

using UnityEngine.Jobs;

using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;

using Swole.API.Unity.Animation;
using Swole.Morphing;
using Swole.Animation;
using Swole.DataStructures;

namespace Swole.API.Unity
{
    public class CharacterMuscleAutoFlexer : MonoBehaviour
    {
        [Serializable]
        public class TransformBinding
        {
            public Transform transformLeft;
            public Transform transformRight;

            public Vector3 startLocalPositionOffset;
            public Vector3 startLocalPositionOffsetSideMul = Vector3.one;
            public float minDistance;
            public float maxDistance;
            public bool clampDistanceWeight;
            public float distanceWeight;
            public bool invertDistanceOutputWeight;

            public Vector3 startLocalRotationOffsetEuler;
            public Vector3 startLocalVector;
            public Vector3 startLocalVectorRightSideMul = Vector3.one;
            public Vector3 angleAxis;
            public Vector3 angleAxisRightSideMul = Vector3.one;
            public float minAngle;
            public float maxAngle;
            public float rightSideAngleMul = 1f;
            public float rightSideAngleAdd;
            public bool absoluteAngle;
            public bool clampAngleWeight;
            [Range(-1f, 1f)]
            public float angleAxisDotFalloffLowerBound;
            public float angleWeight;
            public Vector2 angularSpeedRange;
            public Vector2 angularSpeedFlexMultiplier = Vector2.one;
            public bool invertAngleOutputWeight;
        }

        [Serializable]
        public enum FlexMixType
        {
            Max, Mul, Add
        }
        [Serializable]
        public class MuscleDependency
        {
            public string name;
            [Tooltip("A multiplier applied to flex scaling based on muscle mass.")]
            public float massScaling;
            [Tooltip("A multiplier applied to flex scaling based on muscle mass for flex transforms if seperateMassScalingForTransforms is true.")]
            public float massScalingTransforms;
            public float massMax = 1f;
            public float flexScale = 1f;
            public float2 flexRemap = new float2(0f, 1f);
            public FlexMixType flexMixType;

            [NonSerialized]
            public int cachedMuscleIndex;
        }

        [Serializable]
        public class AutoFlexer
        {
            public string name;

            public bool preIK;

            [AnimatableProperty(true, 1f), Range(0, 2f)]
            public float autoFlexMixLeft = 1f;
            [AnimatableProperty(true, 1f), Range(0, 2f)]
            public float autoFlexMixRight = 1f;

            public TransformBinding bindingA;
            public TransformBinding bindingB;
            public TransformBinding bindingC;
            public TransformBinding bindingD;

            public MuscleDependency[] muscles;

            public bool seperateMassScalingForTransforms;

            public bool clampFlexOutput;
            public bool clampFlexTransformOutput;
            public Vector2 clampRange = new Vector2(0f, 1f);

            public Transform muscleTransformLeft;
            public Transform muscleTransformRight;

            public Vector3 muscleTargetLocalPositionOffset;
            public Vector3 muscleTargetLocalRotationOffsetEuler;
            public Vector3 muscleTargetLocalScaleOffset;
            [NonSerialized]
            public Quaternion muscleTargetLocalRotationOffset;
            [NonSerialized]
            public Quaternion muscleTargetLocalRotationOffset_Right;
            [NonSerialized]
            public Vector3 muscleTargetLocalPositionOffset_Right;

            public Vector3 rightMuscleTargetLocalPositionOffsetMultiplier = Vector3.one;
            public Vector3 rightMuscleTargetLocalRotationOffsetEulerMultiplier = Vector3.one;

            [NonSerialized]
            public int jobIndexLeft;
            [NonSerialized]
            public int jobIndexRight;

            [NonSerialized]
            public float flexOutputLeft;
            [NonSerialized]
            public float flexOutputRight;

            [NonSerialized]
            public float transformFlexOutputLeft;
            [NonSerialized]
            public float transformFlexOutputRight;

            public bool useGradualFlexingForCharacterMesh;
            public bool useGradualFlexingForTransforms;
            public float gradualFlexingSyncSpeed = 5f;

            [NonSerialized]
            public float gradualFlexOutputLeft;
            [NonSerialized]
            public float gradualFlexOutputRight;

            [NonSerialized]
            public float gradualTransformFlexOutputLeft;
            [NonSerialized]
            public float gradualTransformFlexOutputRight;

            public void UpdateFlexOutputs(float deltaTime, ICustomizableCharacter characterMesh, float2 flexOutputs) => UpdateFlexOutputs(deltaTime, characterMesh, flexOutputs.x, flexOutputs.y);
            public void UpdateFlexOutputs(float deltaTime, ICustomizableCharacter characterMesh, float flexLeft, float flexRight) 
            {
                float transformFlexLeft = flexLeft;
                float transformFlexRight = flexRight;
                if (muscles != null && muscles.Length > 0 && characterMesh != null && characterMesh.IsInitialized)
                {
                    if (seperateMassScalingForTransforms)
                    {
                        bool flagA = true;
                        bool flagB = true;
                        float flexScaleLeft = 1f;
                        float flexScaleRight = 1f;
                        float flexScaleLeftTransforms = 1f;
                        float flexScaleRightTransforms = 1f;

                        float maxScalingWeight = 0f;
                        float maxScalingWeightTransforms = 0f;
                        foreach (var muscle in muscles)
                        {
                            if (muscle.cachedMuscleIndex >= 0 && muscle.massMax != 0f && (muscle.massScaling != 0f || muscle.massScalingTransforms != 0f))
                            {
                                var muscleData = characterMesh.GetMuscleDataUnsafe(muscle.cachedMuscleIndex);

                                float massRatioL = (muscleData.valuesLeft.mass / muscle.massMax);
                                float massRatioR = (muscleData.valuesRight.mass / muscle.massMax);

                                if (muscle.massScaling != 0f)
                                {
                                    if (flagA)
                                    {
                                        flagA = false;
                                        flexScaleLeft = 0f;
                                        flexScaleRight = 0f;
                                    }

                                    flexScaleLeft += massRatioL * muscle.massScaling;
                                    flexScaleRight += massRatioR * muscle.massScaling;
                                    maxScalingWeight += muscle.massScaling;
                                }
                                if (muscle.massScalingTransforms != 0f)
                                {
                                    if (flagB)
                                    {
                                        flagB = false;
                                        flexScaleLeftTransforms = 0f;
                                        flexScaleRightTransforms = 0f;
                                    }

                                    flexScaleLeftTransforms += massRatioL * muscle.massScalingTransforms;
                                    flexScaleRightTransforms += massRatioR * muscle.massScalingTransforms;
                                    maxScalingWeightTransforms += muscle.massScalingTransforms;
                                }
                            }
                        }

                        if (maxScalingWeight != 0f)
                        {
                            flexScaleLeft = flexScaleLeft / maxScalingWeight;
                            flexScaleRight = flexScaleRight / maxScalingWeight;
                        }
                        if (maxScalingWeightTransforms != 0f)
                        {
                            flexScaleLeftTransforms = flexScaleLeftTransforms / maxScalingWeightTransforms;
                            flexScaleRightTransforms = flexScaleRightTransforms / maxScalingWeightTransforms;
                        }

                        flexLeft = flexLeft * math.saturate(flexScaleLeft);
                        flexRight = flexRight * math.saturate(flexScaleRight);

                        transformFlexLeft = transformFlexLeft * flexScaleLeftTransforms;
                        transformFlexRight = transformFlexRight * flexScaleRightTransforms;
                    }
                    else
                    {
                        bool flag = true;
                        float flexScaleLeft = 1f;
                        float flexScaleRight = 1f;

                        float maxScalingWeight = 0f;
                        foreach (var muscle in muscles)
                        {
                            if (muscle.cachedMuscleIndex >= 0 && muscle.massMax != 0f && muscle.massScaling != 0f)
                            {
                                var muscleData = characterMesh.GetMuscleDataUnsafe(muscle.cachedMuscleIndex);

                                float massRatioL = (muscleData.valuesLeft.mass / muscle.massMax);
                                float massRatioR = (muscleData.valuesRight.mass / muscle.massMax);

                                if (flag)
                                {
                                    flag = false;
                                    flexScaleLeft = 0f;
                                    flexScaleRight = 0f;
                                }

                                flexScaleLeft += massRatioL * muscle.massScaling;
                                flexScaleRight += massRatioR * muscle.massScaling;
                                maxScalingWeight += muscle.massScaling;
                            }
                        }

                        if (maxScalingWeight != 0f)
                        {
                            flexScaleLeft = flexScaleLeft / maxScalingWeight;
                            flexScaleRight = flexScaleRight / maxScalingWeight;
                        }

                        transformFlexLeft = flexLeft = flexLeft * math.saturate(flexScaleLeft);
                        transformFlexRight = flexRight = flexRight * math.saturate(flexScaleRight);
                    }
                } 

                if (clampFlexOutput)
                {
                    flexLeft = Mathf.Clamp(flexLeft, clampRange.x, clampRange.y);
                    flexRight = Mathf.Clamp(flexRight, clampRange.x, clampRange.y);
                }
                if (clampFlexTransformOutput)
                {
                    transformFlexLeft = Mathf.Clamp(transformFlexLeft, clampRange.x, clampRange.y);
                    transformFlexRight = Mathf.Clamp(transformFlexRight, clampRange.x, clampRange.y); 
                }

                flexOutputLeft = flexLeft;
                flexOutputRight = flexRight;

                gradualFlexOutputLeft = Mathf.MoveTowards(gradualFlexOutputLeft, flexLeft, deltaTime * gradualFlexingSyncSpeed);
                gradualFlexOutputRight = Mathf.MoveTowards(gradualFlexOutputRight, flexRight, deltaTime * gradualFlexingSyncSpeed);

                transformFlexOutputLeft = transformFlexLeft;
                transformFlexOutputRight = transformFlexRight;

                gradualTransformFlexOutputLeft = Mathf.MoveTowards(gradualTransformFlexOutputLeft, transformFlexLeft, deltaTime * gradualFlexingSyncSpeed);
                gradualTransformFlexOutputRight = Mathf.MoveTowards(gradualTransformFlexOutputRight, transformFlexRight, deltaTime * gradualFlexingSyncSpeed);
            }

            [NonSerialized]
            public TransformState muscleTransformLeftStartState;
            [NonSerialized]
            public TransformState muscleTransformRightStartState;

            public void ReinitializeOffsets()
            {
                muscleTargetLocalRotationOffset = Quaternion.Euler(muscleTargetLocalRotationOffsetEuler);
                muscleTargetLocalRotationOffset_Right = Quaternion.Euler(Vector3.Scale(muscleTargetLocalRotationOffsetEuler, rightMuscleTargetLocalRotationOffsetEulerMultiplier));
                muscleTargetLocalPositionOffset_Right = Vector3.Scale(muscleTargetLocalPositionOffset, rightMuscleTargetLocalPositionOffsetMultiplier);
            }
            public void Initialize()
            {
                
                if (muscles != null)
                {
                    foreach (var muscle in muscles) muscle.cachedMuscleIndex = -1;
                }

                jobIndexLeft = -1;
                jobIndexRight = -1;

                ReinitializeOffsets();

                if (muscleTransformLeft != null)
                {
                    muscleTransformLeftStartState = new TransformState(muscleTransformLeft, false); 
                }
                if (muscleTransformRight != null)
                {
                    muscleTransformRightStartState = new TransformState(muscleTransformRight, false);
                }
            }

            public void UpdateMuscleTransforms(bool resetStates)
            {
                float2 flexValues = useGradualFlexingForTransforms ? new float2(gradualTransformFlexOutputLeft, gradualTransformFlexOutputRight) : new float2(transformFlexOutputLeft, transformFlexOutputRight);

                if (muscleTransformLeft != null)
                { 
                    //Debug.Log($"{muscleTransformLeft} : {flexValues.x}");
                    if (resetStates) muscleTransformLeftStartState.ApplyLocal(muscleTransformLeft);

                    muscleTransformLeft.GetLocalPositionAndRotation(out var localPos, out var localRot);
                    var localScale = muscleTransformLeft.localScale;
                    localPos = localPos + muscleTargetLocalPositionOffset * flexValues.x;
                    localRot = Quaternion.SlerpUnclamped(Quaternion.identity, muscleTargetLocalRotationOffset, flexValues.x) * localRot;
                    localScale = localScale + muscleTargetLocalScaleOffset * flexValues.x;
                    muscleTransformLeft.SetLocalPositionAndRotation(localPos, localRot);
                    muscleTransformLeft.localScale = localScale;
                }
                if (muscleTransformRight != null)
                {
                    //Debug.Log($"{muscleTransformRight} : {flexValues.y}");

                    if (resetStates) muscleTransformRightStartState.ApplyLocal(muscleTransformRight);

                    muscleTransformRight.GetLocalPositionAndRotation(out var localPos, out var localRot);
                    var localScale = muscleTransformRight.localScale;
                    localPos = localPos + muscleTargetLocalPositionOffset_Right * flexValues.y;
                    localRot = Quaternion.SlerpUnclamped(Quaternion.identity, muscleTargetLocalRotationOffset_Right, flexValues.y) * localRot;
                    localScale = localScale + muscleTargetLocalScaleOffset * flexValues.y;
                    muscleTransformRight.SetLocalPositionAndRotation(localPos, localRot);
                    muscleTransformRight.localScale = localScale;
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (autoFlexers != null)
            {
                foreach (var flexer in autoFlexers) flexer.ReinitializeOffsets(); 
            }
        }
#endif

        public CustomizableCharacterMesh characterMesh;
        public CustomizableCharacterMeshV2 characterMeshV2;
        public ICustomizableCharacter CharacterMesh => characterMeshV2 == null ? characterMesh : characterMeshV2;
        public CustomAnimator animator;

        [AnimatableProperty(true, 1f), Range(0, 2f)]
        public float autoFlexingMix = 1f;

        public AutoFlexer[] autoFlexers;
        [NonSerialized]
        private AutoFlexer[] autoFlexersWithTransforms;

        private struct BoundAutoFlexer
        {
            public AutoFlexer flexer;
            public int localMuscleIndex;
        }
        [NonSerialized]
        private readonly Dictionary<int, List<BoundAutoFlexer>> boundFlexers = new Dictionary<int, List<BoundAutoFlexer>>();

        protected void Awake()
        {
            if (characterMesh == null) characterMesh = gameObject.GetComponentInChildren<CustomizableCharacterMesh>(true);
            if (characterMeshV2 == null) characterMeshV2 = gameObject.GetComponentInChildren<CustomizableCharacterMeshV2>(true);
            if (animator == null) animator = gameObject.GetComponentInChildren<CustomAnimator>(true); 

            if (autoFlexers != null)
            {
                List<AutoFlexer> autoFlexersWithTransforms = new List<AutoFlexer>();
                foreach (var autoFlexer in autoFlexers)
                {
                    if (autoFlexer == null) continue;

                    autoFlexer.Initialize();
                    if (autoFlexer.muscleTransformLeft != null || autoFlexer.muscleTransformRight != null) autoFlexersWithTransforms.Add(autoFlexer);
                }
                if (autoFlexersWithTransforms.Count > 0) this.autoFlexersWithTransforms = autoFlexersWithTransforms.ToArray();
            }
        }
        protected void Start()
        {
            var characterMesh = CharacterMesh;
            if (characterMesh != null)
            {
                if (autoFlexers != null)
                {
                    foreach(var autoFlexer in autoFlexers)
                    {
                        if (autoFlexer == null) continue;

                        if (autoFlexer.muscles != null)
                        {
                            for (int localMuscleIndex = 0; localMuscleIndex < autoFlexer.muscles.Length; localMuscleIndex++)
                            {
                                var muscle = autoFlexer.muscles[localMuscleIndex];

                                muscle.cachedMuscleIndex = characterMesh.IndexOfMuscleGroup(muscle.name);
                                if (muscle.cachedMuscleIndex >= 0)
                                {
                                    if (!boundFlexers.TryGetValue(muscle.cachedMuscleIndex, out var list))
                                    {
                                        list = new List<BoundAutoFlexer>();
                                        boundFlexers[muscle.cachedMuscleIndex] = list;
                                    }

                                    list.Add(new BoundAutoFlexer() { flexer = autoFlexer, localMuscleIndex = localMuscleIndex });
                                }
                            }
                        }
                    }
                }
            }
        }

        [NonSerialized]
        protected bool disableValueResets;
        public void SetDisableValueResets(bool flag) => disableValueResets = flag;
        public void DisableValueResets() => SetDisableValueResets(true);
        public void EnableValueResets() => SetDisableValueResets(false);
        private static float MixFlex(float currentFlexValue, float nextFlexValue, FlexMixType flexMixType, ref bool isFirst)
        {
            switch(flexMixType)
            {
                case FlexMixType.Max:
                    nextFlexValue = math.max(isFirst && currentFlexValue == 0f ? float.MinValue : currentFlexValue, nextFlexValue);
                    break;

                case FlexMixType.Mul:
                    nextFlexValue = currentFlexValue * nextFlexValue;
                    break;

                case FlexMixType.Add:
                    nextFlexValue = currentFlexValue + nextFlexValue;
                    break;
            } 

            isFirst = false;
            return nextFlexValue;
        }
        public void UpdateFlexing()
        {
            var characterMesh = CharacterMesh;

            bool resetValues = !disableValueResets && (animator == null || !animator.isActiveAndEnabled);

            if (characterMesh != null && characterMesh.IsInitialized)
            {
                foreach (var entry in boundFlexers)
                {
                    var muscleData = characterMesh.GetMuscleDataUnsafe(entry.Key);

                    if (resetValues)
                    {
                        muscleData.valuesLeft.flex = 0f;//float.MinValue;
                        muscleData.valuesRight.flex = 0f;//float.MinValue;
                    }
                    bool isFirstL = true;
                    bool isFirstR = true;
                    foreach (var flexerBinding in entry.Value)
                    {
                        var flexer = flexerBinding.flexer; 
                        var localMuscle = flexer.muscles[flexerBinding.localMuscleIndex];
                        if (localMuscle.flexScale == 0f) continue; 
                        
                        float2 flexValues = math.remap(0f, 1f, localMuscle.flexRemap.x, localMuscle.flexRemap.y, flexer.useGradualFlexingForCharacterMesh ? new float2(flexer.gradualFlexOutputLeft, flexer.gradualFlexOutputRight) : new float2(flexer.flexOutputLeft, flexer.flexOutputRight)) * localMuscle.flexScale;

                        muscleData.valuesLeft.flex = MixFlex(muscleData.valuesLeft.flex, flexValues.x * autoFlexingMix * flexer.autoFlexMixLeft, localMuscle.flexMixType, ref isFirstL); 
                        muscleData.valuesRight.flex = MixFlex(muscleData.valuesRight.flex, flexValues.y * autoFlexingMix * flexer.autoFlexMixRight, localMuscle.flexMixType, ref isFirstR);
                    }

                    characterMesh.SetMuscleDataUnsafe(entry.Key, muscleData);
                }
            }
            if (autoFlexersWithTransforms != null)
            {
                foreach (var flexer in autoFlexersWithTransforms)
                {
                    flexer.UpdateMuscleTransforms(resetValues);
                }
            }
        }

        public void OnEnable()
        {
            CharacterMuscleAutoFlexerUpdater.Register(this); 
        }
        public void OnDisable()
        {
            CharacterMuscleAutoFlexerUpdater.Unregister(this);
        }
    }


    public class CharacterMuscleAutoFlexerUpdater : SingletonBehaviour<CharacterMuscleAutoFlexerUpdater>, IDisposable
    {

        public static int ExecutionPriority => CustomAnimatorUpdater.FinalAnimationBehaviourPriority + 1; // update once bones are in their final positions
        public override int Priority => ExecutionPriority;

        private static int PreIkExecutionPriority => CustomIKManagerUpdater.ExecutionPriority - 1;

        protected readonly List<CharacterMuscleAutoFlexer> flexers = new List<CharacterMuscleAutoFlexer>();
        protected readonly List<int> standardFlexers = new List<int>();
        protected readonly List<int> preIkFlexers = new List<int>();
        public static bool Register(CharacterMuscleAutoFlexer flexer)
        {
            var instance = Instance;
            if (flexer == null || instance == null) return false;

            instance.FinalizeAllJobsLocal();

            if (!instance.flexers.Contains(flexer))
            {
                if (flexer.autoFlexers != null && flexer.autoFlexers.Length > 0)
                {
                    bool isStandard = false;
                    bool isPreIk = false;
                    foreach (var autoFlexer in flexer.autoFlexers)
                    {
                        if (autoFlexer.preIK) isPreIk = true;
                        else isStandard = true;

                        var indices = instance.AddBindings(autoFlexer.preIK, autoFlexer.bindingA, autoFlexer.bindingB, autoFlexer.bindingC, autoFlexer.bindingD);

                        autoFlexer.jobIndexLeft = indices.x;
                        autoFlexer.jobIndexRight = indices.y;
                    }

                    int flexerIndex = instance.flexers.Count;
                    instance.flexers.Add(flexer);
                    if (isStandard) instance.standardFlexers.Add(flexerIndex);
                    if (isPreIk) instance.preIkFlexers.Add(flexerIndex);
                }
            }

            return true;
        }
        public static bool Unregister(CharacterMuscleAutoFlexer flexer)
        {
            var instance = InstanceOrNull;
            if (flexer == null || instance == null) return false;

            instance.FinalizeAllJobsLocal();

            int flexerIndex = instance.flexers.IndexOf(flexer);
            bool flag = flexerIndex >= 0;
            if (flag)
            {
                if (flexer.autoFlexers != null)
                {
                    foreach(var autoFlexer in flexer.autoFlexers)
                    {
                        if (autoFlexer.jobIndexLeft >= 0) instance.RemoveBinding(autoFlexer.preIK, autoFlexer.jobIndexLeft);
                        if (autoFlexer.jobIndexRight >= 0) instance.RemoveBinding(autoFlexer.preIK, autoFlexer.jobIndexRight);

                        autoFlexer.jobIndexLeft = -1;
                        autoFlexer.jobIndexRight = -1;
                    }
                }

                instance.standardFlexers.Remove(flexerIndex);
                instance.preIkFlexers.Remove(flexerIndex);

                instance.flexers.RemoveAt(flexerIndex); // wait until after so job index changes are applied to the flexer still
                for(int a = 0; a < instance.standardFlexers.Count; a++)
                {
                    var index = instance.standardFlexers[a];
                    if (index >= flexerIndex) index = index - 1;
                    instance.standardFlexers[a] = index;
                }
                for (int a = 0; a < instance.preIkFlexers.Count; a++)
                {
                    var index = instance.preIkFlexers[a];
                    if (index >= flexerIndex) index = index - 1;
                    instance.preIkFlexers[a] = index;
                }
            }
            return flag;
        }

        private readonly Dictionary<Transform, int2> transformIndicesPreIk = new Dictionary<Transform, int2>();
        private TransformAccessArray transformsPreIk;

        private readonly Dictionary<Transform, int2> transformIndices = new Dictionary<Transform, int2>();
        private TransformAccessArray transforms;
        private int RegisterTransformUser(Transform transform, bool preIk)
        {
            if (transform == null) return -1;

            var indicesDict = preIk ? transformIndicesPreIk : transformIndices;
            var transformsArray = preIk ? transformsPreIk : transforms;
            var transformStatesList = preIk ? transformStatesPreIk : transformStates;

            if (!indicesDict.TryGetValue(transform, out int2 index))
            {
                index.x = transformsArray.length;
                transformsArray.Add(transform);
                transformStatesList.Add(new TransformDataStateTemporal(transform, false));
            }

            index.y = index.y + 1;
            indicesDict[transform] = index;
            return index.x;
        }
        private void UnregisterTransformUser(Transform transform, bool preIk)
        {
            var indicesDict = preIk ? transformIndicesPreIk : transformIndices;

            if (indicesDict.TryGetValue(transform, out int2 index))
            {
                index.y = index.y - 1;
                if (index.y <= 0)
                {
                    if (index.x >= 0)
                    {
                        RemoveTransform(index.x, preIk);
                    } 
                    else
                    {
                        indicesDict.Remove(transform);
                    }
                } 
                else
                {
                    indicesDict[transform] = index;
                }
            }
        }
        private void RemoveTransform(int index, bool preIk)
        {
            if (index >= 0)
            {
                var indicesDict = preIk ? transformIndicesPreIk : transformIndices;
                var transformsArray = preIk ? transformsPreIk : transforms;
                var transformStatesList = preIk ? transformStatesPreIk : transformStates;
                var bindingsList = preIk ? autoFlexBindingsPreIk : autoFlexBindings;

                var toRemove = transformsArray[index];

                int swapIndex = transformsArray.length - 1;
                if (swapIndex == index) swapIndex = -1;
                transformsArray.RemoveAtSwapBack(index);
                transformStatesList.RemoveAtSwapBack(index);

                for (int a = 0; a < transformsArray.length; a++)
                {
                    var transform = transformsArray[a];
                    if (indicesDict.TryGetValue(transform, out int2 index_))
                    {
                        if (index_.x == swapIndex)
                        {
                            index_.x = index;
                            indicesDict[transform] = index_; 
                        }
                    }
                }
                for (int a = 0; a < bindingsList.Length; a++)
                {
                    var binding = bindingsList[a];
                    binding.transformIndices = math.select(binding.transformIndices, -1, binding.transformIndices == index);
                    if (swapIndex >= 0) binding.transformIndices = math.select(binding.transformIndices, index, binding.transformIndices == swapIndex);
                    bindingsList[a] = binding; 
                }

                indicesDict.Remove(toRemove);
            }
        }
        private void RemoveNullTransforms(bool preIk)
        {
            var indicesDict = preIk ? transformIndicesPreIk : transformIndices;

            bool flag = false;
            while (flag)
            {
                flag = false;

                Transform toRemove = null;
                int index = -1;
                foreach (var entry in indicesDict)
                {
                    if (entry.Key == null)
                    {
                        flag = true;

                        index = entry.Value.x;
                        toRemove = entry.Key;
                        break;
                    }
                }

                if (flag) RemoveTransform(index, preIk);
            }
        }

        private NativeList<TransformDataStateTemporal> transformStatesPreIk;
        private NativeList<AutoFlexBinding> autoFlexBindingsPreIk;
        private NativeList<TransformDataStateTemporal> transformStates;
        private NativeList<AutoFlexBinding> autoFlexBindings;
        private int2 AddBindings(bool preIk, CharacterMuscleAutoFlexer.TransformBinding bindingA, CharacterMuscleAutoFlexer.TransformBinding bindingB, CharacterMuscleAutoFlexer.TransformBinding bindingC, CharacterMuscleAutoFlexer.TransformBinding bindingD)
        {
            var transformsArray = preIk ? transformsPreIk : transforms;
            var bindingsList = preIk ? autoFlexBindingsPreIk : autoFlexBindings;

            int2 indices = -1;

            var binding = new AutoFlexBinding();
            binding.absoluteAngleFlags = new bool4(bindingA.absoluteAngle, bindingB.absoluteAngle, bindingC.absoluteAngle, bindingD.absoluteAngle);
            binding.angleWeightClampFlags = new bool4(bindingA.clampAngleWeight, bindingB.clampAngleWeight, bindingC.clampAngleWeight, bindingD.clampAngleWeight);
            binding.distanceWeightClampFlags = new bool4(bindingA.clampDistanceWeight, bindingB.clampDistanceWeight, bindingC.clampDistanceWeight, bindingD.clampDistanceWeight);
            binding.invertAngleWeightFlags = new bool4(bindingA.invertAngleOutputWeight, bindingB.invertAngleOutputWeight, bindingC.invertAngleOutputWeight, bindingD.invertAngleOutputWeight);
            binding.invertDistanceWeightFlags = new bool4(bindingA.invertDistanceOutputWeight, bindingB.invertDistanceOutputWeight, bindingC.invertDistanceOutputWeight, bindingD.invertDistanceOutputWeight);
            binding.targetMinDistances = new float4(bindingA.minDistance, bindingB.minDistance, bindingC.minDistance, bindingD.minDistance);
            binding.targetMaxDistances = new float4(bindingA.maxDistance, bindingB.maxDistance, bindingC.maxDistance, bindingD.maxDistance);
            binding.targetAnglesDegreesMin = new float4(bindingA.minAngle, bindingB.minAngle, bindingC.minAngle, bindingD.minAngle);
            binding.targetAnglesDegreesMax = new float4(bindingA.maxAngle, bindingB.maxAngle, bindingC.maxAngle, bindingD.maxAngle);
            binding.distanceWeights = new float4(bindingA.distanceWeight, bindingB.distanceWeight, bindingC.distanceWeight, bindingD.distanceWeight);
            binding.angleWeights = new float4(bindingA.angleWeight, bindingB.angleWeight, bindingC.angleWeight, bindingD.angleWeight);
            binding.angleAxisDotFalloffLowerBounds = new float4(bindingA.angleAxisDotFalloffLowerBound, bindingB.angleAxisDotFalloffLowerBound, bindingC.angleAxisDotFalloffLowerBound, bindingD.angleAxisDotFalloffLowerBound);
            //binding.targetStartVectors = new float3x4(bindingA.startLocalVector, bindingB.startLocalVector, bindingC.startLocalVector, bindingD.startLocalVector);
            binding.targetAngleAxisVectors = new float3x4(bindingA.angleAxis, bindingB.angleAxis, bindingC.angleAxis, bindingD.angleAxis);
            binding.angularSpeedRangesMin = new float4(bindingA.angularSpeedRange.x, bindingB.angularSpeedRange.x, bindingC.angularSpeedRange.x, bindingD.angularSpeedRange.x);
            binding.angularSpeedRangesMax = new float4(bindingA.angularSpeedRange.y, bindingB.angularSpeedRange.y, bindingC.angularSpeedRange.y, bindingD.angularSpeedRange.y);
            binding.angularSpeedFlexMultipliersMin = new float4(bindingA.angularSpeedFlexMultiplier.x, bindingB.angularSpeedFlexMultiplier.x, bindingC.angularSpeedFlexMultiplier.x, bindingD.angularSpeedFlexMultiplier.x);
            binding.angularSpeedFlexMultipliersMax = new float4(bindingA.angularSpeedFlexMultiplier.y, bindingB.angularSpeedFlexMultiplier.y, bindingC.angularSpeedFlexMultiplier.y, bindingD.angularSpeedFlexMultiplier.y);

            if (bindingA.transformLeft != null || bindingB.transformLeft != null || bindingC.transformLeft != null || bindingD.transformLeft != null)
            {
                var bindingLeft = binding;
                bindingLeft.transformIndices.x = RegisterTransformUser(bindingA.transformLeft, preIk);
                bindingLeft.transformIndices.y = RegisterTransformUser(bindingB.transformLeft, preIk);
                bindingLeft.transformIndices.z = RegisterTransformUser(bindingC.transformLeft, preIk);
                bindingLeft.transformIndices.w = RegisterTransformUser(bindingD.transformLeft, preIk);

                bindingLeft.targetStartLocalPositions = new float3x4(
                    bindingLeft.transformIndices.x >= 0 ? ((float3)transformsArray[bindingLeft.transformIndices.x].localPosition + (float3)bindingA.startLocalPositionOffset) : float3.zero,
                    bindingLeft.transformIndices.y >= 0 ? ((float3)transformsArray[bindingLeft.transformIndices.y].localPosition + (float3)bindingB.startLocalPositionOffset) : float3.zero,
                    bindingLeft.transformIndices.z >= 0 ? ((float3)transformsArray[bindingLeft.transformIndices.z].localPosition + (float3)bindingC.startLocalPositionOffset) : float3.zero,
                    bindingLeft.transformIndices.w >= 0 ? ((float3)transformsArray[bindingLeft.transformIndices.w].localPosition + (float3)bindingD.startLocalPositionOffset) : float3.zero);

                bindingLeft.targetInverseStartRotations = new float4x4(
                    ((quaternion)Quaternion.Inverse((bindingLeft.transformIndices.x >= 0 ? (Quaternion.Euler(bindingA.startLocalRotationOffsetEuler) * ((Quaternion)transformsArray[bindingLeft.transformIndices.x].localRotation)) : Quaternion.identity))).value,
                    ((quaternion)Quaternion.Inverse((bindingLeft.transformIndices.y >= 0 ? (Quaternion.Euler(bindingB.startLocalRotationOffsetEuler) * ((Quaternion)transformsArray[bindingLeft.transformIndices.y].localRotation)) : Quaternion.identity))).value,
                    ((quaternion)Quaternion.Inverse((bindingLeft.transformIndices.z >= 0 ? (Quaternion.Euler(bindingC.startLocalRotationOffsetEuler) * ((Quaternion)transformsArray[bindingLeft.transformIndices.z].localRotation)) : Quaternion.identity))).value,
                    ((quaternion)Quaternion.Inverse((bindingLeft.transformIndices.w >= 0 ? (Quaternion.Euler(bindingD.startLocalRotationOffsetEuler) * ((Quaternion)transformsArray[bindingLeft.transformIndices.w].localRotation)) : Quaternion.identity))).value);

                bindingLeft.targetAngleStartAxisVectors = new float3x4(
                    bindingLeft.transformIndices.x >= 0 ? ((float3)(transformsArray[bindingLeft.transformIndices.x].localRotation * bindingA.angleAxis)) : float3.zero,
                    bindingLeft.transformIndices.y >= 0 ? ((float3)(transformsArray[bindingLeft.transformIndices.y].localRotation * bindingB.angleAxis)) : float3.zero,
                    bindingLeft.transformIndices.z >= 0 ? ((float3)(transformsArray[bindingLeft.transformIndices.z].localRotation * bindingC.angleAxis)) : float3.zero,
                    bindingLeft.transformIndices.w >= 0 ? ((float3)(transformsArray[bindingLeft.transformIndices.w].localRotation * bindingD.angleAxis)) : float3.zero);

                bindingLeft.targetStartVectors = new float3x4(
                    bindingLeft.transformIndices.x >= 0 ? ((float3)(transformsArray[bindingLeft.transformIndices.x].localRotation * bindingA.startLocalVector)) : float3.zero,
                    bindingLeft.transformIndices.y >= 0 ? ((float3)(transformsArray[bindingLeft.transformIndices.y].localRotation * bindingB.startLocalVector)) : float3.zero,
                    bindingLeft.transformIndices.z >= 0 ? ((float3)(transformsArray[bindingLeft.transformIndices.z].localRotation * bindingC.startLocalVector)) : float3.zero,
                    bindingLeft.transformIndices.w >= 0 ? ((float3)(transformsArray[bindingLeft.transformIndices.w].localRotation * bindingD.startLocalVector)) : float3.zero);

                indices.x = bindingsList.Length;
                bindingsList.Add(bindingLeft);
            }
            if (bindingA.transformRight != null || bindingB.transformRight != null || bindingC.transformRight != null || bindingD.transformRight != null) 
            {
                var bindingRight = binding;
                bindingRight.transformIndices.x = RegisterTransformUser(bindingA.transformRight, preIk);
                bindingRight.transformIndices.y = RegisterTransformUser(bindingB.transformRight, preIk);
                bindingRight.transformIndices.z = RegisterTransformUser(bindingC.transformRight, preIk);
                bindingRight.transformIndices.w = RegisterTransformUser(bindingD.transformRight, preIk);

                bindingRight.targetAnglesDegreesMin = (bindingRight.targetAnglesDegreesMin * new float4(bindingA.rightSideAngleMul, bindingB.rightSideAngleMul, bindingC.rightSideAngleMul, bindingD.rightSideAngleMul)) + new float4(bindingA.rightSideAngleAdd, bindingB.rightSideAngleAdd, bindingC.rightSideAngleAdd, bindingD.rightSideAngleAdd); 
                bindingRight.targetAnglesDegreesMax = (bindingRight.targetAnglesDegreesMax * new float4(bindingA.rightSideAngleMul, bindingB.rightSideAngleMul, bindingC.rightSideAngleMul, bindingD.rightSideAngleMul)) + new float4(bindingA.rightSideAngleAdd, bindingB.rightSideAngleAdd, bindingC.rightSideAngleAdd, bindingD.rightSideAngleAdd);

                bindingRight.targetStartLocalPositions = new float3x4(
                    bindingRight.transformIndices.x >= 0 ? ((float3)transformsArray[bindingRight.transformIndices.x].localPosition + (float3)Vector3.Scale(bindingA.startLocalPositionOffset, bindingA.startLocalPositionOffsetSideMul)) : float3.zero,
                    bindingRight.transformIndices.y >= 0 ? ((float3)transformsArray[bindingRight.transformIndices.y].localPosition + (float3)Vector3.Scale(bindingB.startLocalPositionOffset, bindingB.startLocalPositionOffsetSideMul)) : float3.zero,
                    bindingRight.transformIndices.z >= 0 ? ((float3)transformsArray[bindingRight.transformIndices.z].localPosition + (float3)Vector3.Scale(bindingC.startLocalPositionOffset, bindingC.startLocalPositionOffsetSideMul)) : float3.zero, 
                    bindingRight.transformIndices.w >= 0 ? ((float3)transformsArray[bindingRight.transformIndices.w].localPosition + (float3)Vector3.Scale(bindingD.startLocalPositionOffset, bindingD.startLocalPositionOffsetSideMul)) : float3.zero);
                bindingRight.targetInverseStartRotations = new float4x4(
                    ((quaternion)Quaternion.Inverse((bindingRight.transformIndices.x >= 0 ? (Quaternion.Euler(bindingA.startLocalRotationOffsetEuler) * ((Quaternion)transformsArray[bindingRight.transformIndices.x].localRotation)) : Quaternion.identity))).value,
                    ((quaternion)Quaternion.Inverse((bindingRight.transformIndices.y >= 0 ? (Quaternion.Euler(bindingB.startLocalRotationOffsetEuler) * ((Quaternion)transformsArray[bindingRight.transformIndices.y].localRotation)) : Quaternion.identity))).value,
                    ((quaternion)Quaternion.Inverse((bindingRight.transformIndices.z >= 0 ? (Quaternion.Euler(bindingC.startLocalRotationOffsetEuler) * ((Quaternion)transformsArray[bindingRight.transformIndices.z].localRotation)) : Quaternion.identity))).value,
                    ((quaternion)Quaternion.Inverse((bindingRight.transformIndices.w >= 0 ? (Quaternion.Euler(bindingD.startLocalRotationOffsetEuler) * ((Quaternion)transformsArray[bindingRight.transformIndices.w].localRotation)) : Quaternion.identity))).value);
                 
                bindingRight.targetAngleStartAxisVectors = new float3x4(
                    bindingRight.transformIndices.x >= 0 ? ((float3)(transformsArray[bindingRight.transformIndices.x].localRotation * Vector3.Scale(bindingA.angleAxis, bindingA.angleAxisRightSideMul))) : float3.zero,
                    bindingRight.transformIndices.y >= 0 ? ((float3)(transformsArray[bindingRight.transformIndices.y].localRotation * Vector3.Scale(bindingB.angleAxis, bindingB.angleAxisRightSideMul))) : float3.zero,
                    bindingRight.transformIndices.z >= 0 ? ((float3)(transformsArray[bindingRight.transformIndices.z].localRotation * Vector3.Scale(bindingC.angleAxis, bindingC.angleAxisRightSideMul))) : float3.zero,
                    bindingRight.transformIndices.w >= 0 ? ((float3)(transformsArray[bindingRight.transformIndices.w].localRotation * Vector3.Scale(bindingD.angleAxis, bindingD.angleAxisRightSideMul))) : float3.zero);

                bindingRight.targetStartVectors = new float3x4(
                    bindingRight.transformIndices.x >= 0 ? ((float3)(transformsArray[bindingRight.transformIndices.x].localRotation * Vector3.Scale(bindingA.startLocalVector, bindingA.startLocalVectorRightSideMul))) : float3.zero,
                    bindingRight.transformIndices.y >= 0 ? ((float3)(transformsArray[bindingRight.transformIndices.y].localRotation * Vector3.Scale(bindingB.startLocalVector, bindingB.startLocalVectorRightSideMul))) : float3.zero,
                    bindingRight.transformIndices.z >= 0 ? ((float3)(transformsArray[bindingRight.transformIndices.z].localRotation * Vector3.Scale(bindingC.startLocalVector, bindingC.startLocalVectorRightSideMul))) : float3.zero,
                    bindingRight.transformIndices.w >= 0 ? ((float3)(transformsArray[bindingRight.transformIndices.w].localRotation * Vector3.Scale(bindingD.startLocalVector, bindingD.startLocalVectorRightSideMul))) : float3.zero);  

                indices.y = bindingsList.Length;
                bindingsList.Add(bindingRight);
            }

            return indices;
        }
        private int AddBinding(bool preIk, AutoFlexBinding binding)
        {
            var bindingsList = preIk ? autoFlexBindingsPreIk : autoFlexBindings;

            int index = bindingsList.Length;
            bindingsList.Add(binding);
            return index;
        }
        private void RemoveBinding(bool preIk, int index)
        {
            if (index >= 0)
            {
                var bindingsList = preIk ? autoFlexBindingsPreIk : autoFlexBindings;
                var transformsArray = preIk ? transformsPreIk : transforms;

                var binding = bindingsList[index];
                if (binding.transformIndices.x >= 0)
                {
                    UnregisterTransformUser(transformsArray[binding.transformIndices.x], preIk);
                    binding = bindingsList[index]; // refetch binding in case transform user changes modified it
                }
                if (binding.transformIndices.y >= 0)
                {
                    UnregisterTransformUser(transformsArray[binding.transformIndices.y], preIk);
                    binding = bindingsList[index]; // refetch binding in case transform user changes modified it
                }
                if (binding.transformIndices.z >= 0)
                {
                    UnregisterTransformUser(transformsArray[binding.transformIndices.z], preIk);
                    binding = bindingsList[index]; // refetch binding in case transform user changes modified it
                }
                if (binding.transformIndices.w >= 0) 
                { 
                    UnregisterTransformUser(transformsArray[binding.transformIndices.w], preIk);
                    binding = bindingsList[index]; // refetch binding in case transform user changes modified it
                }

                int swapIndex = bindingsList.Length - 1;
                if (swapIndex == index) swapIndex = -1;
                bindingsList.RemoveAtSwapBack(index);

                var indexList = preIk ? preIkFlexers : standardFlexers;
                foreach (var flexerIndex in indexList)
                {
                    //if (flexerIndex < 0 || flexerIndex >= flexers.Count) continue;

                    var flexer = flexers[flexerIndex];
                    if (flexer == null || flexer.autoFlexers == null) continue;

                    foreach(var autoFlexer in flexer.autoFlexers)
                    {
                        if (autoFlexer.preIK != preIk) continue;

                        if (autoFlexer.jobIndexLeft == index) autoFlexer.jobIndexLeft = -1;
                        if (autoFlexer.jobIndexRight == index) autoFlexer.jobIndexRight = -1;

                        if (swapIndex >= 0)
                        {
                            if (autoFlexer.jobIndexLeft == swapIndex) autoFlexer.jobIndexLeft = index;
                            if (autoFlexer.jobIndexRight == swapIndex) autoFlexer.jobIndexRight = index;
                        }
                    }
                }
            }
        }
        private void PreIkTransferBindingOutputsAndUpdateFlexing(float deltaTime)
        {
            FinalizePreIkJobsLocal();

            foreach (var flexerIndex in preIkFlexers)
            {
                var flexer = flexers[flexerIndex];
                if (flexer == null || flexer.autoFlexers == null) continue;

                foreach (var autoFlexer in flexer.autoFlexers)
                {
                    if (!autoFlexer.preIK) continue;
                    autoFlexer.UpdateFlexOutputs(deltaTime, flexer.CharacterMesh, autoFlexer.jobIndexLeft >= 0 ? autoFlexBindingsPreIk[autoFlexer.jobIndexLeft].output : 0f, autoFlexer.jobIndexRight >= 0 ? autoFlexBindingsPreIk[autoFlexer.jobIndexRight].output : 0f);
                }

                flexer.UpdateFlexing();
            }
        }
        private void TransferBindingOutputsAndUpdateFlexing(float deltaTime)
        {
            FinalizeAllJobsLocal();

            foreach (var flexerIndex in standardFlexers)
            {
                var flexer = flexers[flexerIndex];
                if (flexer == null || flexer.autoFlexers == null) continue;

                foreach (var autoFlexer in flexer.autoFlexers)
                {
                    if (autoFlexer.preIK) continue;
                    autoFlexer.UpdateFlexOutputs(deltaTime, flexer.CharacterMesh, autoFlexer.jobIndexLeft >= 0 ? autoFlexBindings[autoFlexer.jobIndexLeft].output : 0f, autoFlexer.jobIndexRight >= 0 ? autoFlexBindings[autoFlexer.jobIndexRight].output : 0f);
                }

                flexer.UpdateFlexing();
            }
        }

        public override void OnFixedUpdate() { }

        public override void OnUpdate() { }

        private class PreIkExecutor : IExecutableBehaviour
        {
            public CharacterMuscleAutoFlexerUpdater updater;

            public int Priority => CharacterMuscleAutoFlexerUpdater.PreIkExecutionPriority;

            public int UpdatePriority => Priority;

            public int LateUpdatePriority => Priority;

            public int FixedUpdatePriority => Priority;

            public int CompareTo(IExecutableBehaviour other) => other == null ? 1 : Priority.CompareTo(other.Priority);

            public void OnFixedUpdate()
            {
            }

            public void OnLateUpdate()
            {
                updater.PreIkUpdate();
            }

            public void OnPostFixedUpdate()
            {
            }

            public void OnPreFixedUpdate()
            {
            }

            public void OnUpdate()
            {
            }
        }

        private PreIkExecutor preIkExecutor;

        private JobHandle currentJobHandlePreIk;
        private JobHandle currentJobHandle;
        public JobHandle OutputDependencyLocal => JobHandle.CombineDependencies(currentJobHandlePreIk, currentJobHandle);
        public JobHandle OutputDependency
        {
            get
            {
                var instance = InstanceOrNull;
                if (instance == null) return default;

                return instance.OutputDependencyLocal;
            }
        }
        public static void FinalizePreIkJobs()
        {
            var instance = InstanceOrNull;
            if (instance == null) return;

            instance.FinalizePreIkJobsLocal();
        }
        public void FinalizePreIkJobsLocal()
        {
            currentJobHandlePreIk.Complete();
        }
        public static void FinalizeAllJobs()
        {
            var instance = InstanceOrNull;
            if (instance == null) return;

            instance.FinalizeAllJobsLocal();
        }
        public void FinalizeAllJobsLocal()
        {
            currentJobHandlePreIk.Complete();
            currentJobHandle.Complete();
        }

        protected void PreIkUpdate()
        {
            FinalizePreIkJobsLocal();

            RemoveNullTransforms(true);

            currentJobHandlePreIk = new SharedJobs.FetchTemporalTransformDataJobLocal()
            {
                transformData = transformStatesPreIk.AsArray()
            }.Schedule(transformsPreIk, default);

            currentJobHandlePreIk = new EvaluateAutoFlexBindings()
            {
                deltaTime = Time.deltaTime,
                transformStates = transformStatesPreIk,
                bindings = autoFlexBindingsPreIk
            }.Schedule(autoFlexBindingsPreIk.Length, 1, currentJobHandlePreIk);

            PreIkTransferBindingOutputsAndUpdateFlexing(Time.deltaTime);
        }

        public override void OnLateUpdate()
        {
            FinalizeAllJobsLocal();

            RemoveNullTransforms(false);

            currentJobHandle = new SharedJobs.FetchTemporalTransformDataJobLocal()
            {
                transformData = transformStates.AsArray()
            }.Schedule(transforms, default);

            currentJobHandle = new EvaluateAutoFlexBindings()
            {
                deltaTime = Time.deltaTime,
                transformStates = transformStates,
                bindings = autoFlexBindings
            }.Schedule(autoFlexBindings.Length, 1, currentJobHandle);

            TransferBindingOutputsAndUpdateFlexing(Time.deltaTime);
        }

        protected override void OnAwake()
        {
            transformsPreIk = new TransformAccessArray(128, -1);
            transformStatesPreIk = new NativeList<TransformDataStateTemporal>(128, Allocator.Persistent);
            autoFlexBindingsPreIk = new NativeList<AutoFlexBinding>(32, Allocator.Persistent);

            transforms = new TransformAccessArray(128, -1);
            transformStates = new NativeList<TransformDataStateTemporal>(128, Allocator.Persistent);
            autoFlexBindings = new NativeList<AutoFlexBinding>(32, Allocator.Persistent);

            preIkExecutor = new PreIkExecutor() { updater = this };
            SingletonCallStack.Insert(preIkExecutor); 
        }

        protected void OnDestroy()
        {
            if (preIkExecutor != null) 
            {
                SingletonCallStack.Remove(preIkExecutor);
                preIkExecutor = null;
            }

            Dispose(); 
        }

        public void Dispose()
        {
            if (transformsPreIk.isCreated) transformsPreIk.Dispose();
            transformsPreIk = default;

            if (transformStatesPreIk.IsCreated) transformStatesPreIk.Dispose();
            transformStatesPreIk = default;

            if (autoFlexBindingsPreIk.IsCreated) autoFlexBindingsPreIk.Dispose();
            autoFlexBindingsPreIk = default;

            if (transforms.isCreated) transforms.Dispose();
            transforms = default;

            if (transformStates.IsCreated) transformStates.Dispose();
            transformStates = default;

            if (autoFlexBindings.IsCreated) autoFlexBindings.Dispose();
            autoFlexBindings = default;
        }

        //[StructLayout(LayoutKind.Sequential)]
        public struct AutoFlexBinding
        {
            public float output;
            public float4 distanceWeightOutputs;
            public float4 angleWeightOutputs;

            public bool4 absoluteAngleFlags;
            public bool4 angleWeightClampFlags;
            public bool4 distanceWeightClampFlags;
            public bool4 invertAngleWeightFlags;
            public bool4 invertDistanceWeightFlags;
            public int4 transformIndices;
            public float4 targetMinDistances;
            public float4 targetMaxDistances;
            public float4 targetAnglesDegreesMin;
            public float4 targetAnglesDegreesMax;
            public float4 distanceWeights;
            public float4 angleAxisDotFalloffLowerBounds;
            public float4 angleWeights;
            public float4 angularSpeedRangesMin;
            public float4 angularSpeedRangesMax;
            public float4 angularSpeedFlexMultipliersMin;
            public float4 angularSpeedFlexMultipliersMax;
            public float3x4 targetStartLocalPositions;
            public float3x4 targetStartVectors;
            public float3x4 targetAngleAxisVectors;
            public float3x4 targetAngleStartAxisVectors;
            public float4x4 targetInverseStartRotations;
        }
        [BurstCompile]
        public struct EvaluateAutoFlexBindings : IJobParallelFor
        {
            public float deltaTime;

            [ReadOnly]
            public NativeList<TransformDataStateTemporal> transformStates;

            [NativeDisableParallelForRestriction]
            public NativeList<AutoFlexBinding> bindings;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void EvaluateTransform(bool indexIsValid, int transformIndex, out  float3 prevLocalPosition, out float4 prevLocalRotation, out float3 localPosition, out float4 localRotation)
            {
                prevLocalPosition = localPosition = float3.zero;
                prevLocalRotation = localRotation = quaternion.identity.value; 
                if (!indexIsValid) return;

                var state = transformStates[transformIndex];
                localPosition = state.position;
                localRotation = state.rotation.value;
                prevLocalPosition = state.prevPosition;
                prevLocalRotation = state.prevRotation.value; 
            }

            public void Execute(int index)
            {
                var binding = bindings[index];
                
                bool4 indexValidation = binding.transformIndices >= 0; 
                float3x4 prevLocalPositions = new float3x4();
                float4x4 prevLocalRotations = new float4x4();
                float3x4 localPositions = new float3x4();
                float4x4 localRotations = new float4x4();
                EvaluateTransform(indexValidation.x, binding.transformIndices.x, out prevLocalPositions.c0, out prevLocalRotations.c0, out localPositions.c0, out localRotations.c0);
                EvaluateTransform(indexValidation.y, binding.transformIndices.y, out prevLocalPositions.c1, out prevLocalRotations.c1, out localPositions.c1, out localRotations.c1);
                EvaluateTransform(indexValidation.z, binding.transformIndices.z, out prevLocalPositions.c2, out prevLocalRotations.c2, out localPositions.c2, out localRotations.c2);
                EvaluateTransform(indexValidation.w, binding.transformIndices.w, out prevLocalPositions.c3, out prevLocalRotations.c3, out localPositions.c3, out localRotations.c3); 

                var positionOffsets = localPositions - binding.targetStartLocalPositions;
                var positionTemporalOffsets = localPositions - prevLocalPositions;
                var lengths = new float4(math.length(positionOffsets.c0), math.length(positionOffsets.c1), math.length(positionOffsets.c2), math.length(positionOffsets.c3));

                float4 distanceRanges = math.abs(binding.targetMaxDistances - binding.targetMinDistances);
                var lengthWeightValidation = distanceRanges > 0f;

                var lengthWeights = (lengths - binding.targetMinDistances) / distanceRanges;
                lengthWeights = math.saturate(math.select(1f - math.min(1f, math.abs(lengthWeights - 1f)), lengthWeights, binding.distanceWeightClampFlags));
                lengthWeights = math.select(lengthWeights, 1f - lengthWeights, binding.invertDistanceWeightFlags);
                lengthWeights = math.select(float4.zero, lengthWeights, lengthWeightValidation);
                float distanceWeightsTotal = math.csum(binding.distanceWeights);
                lengthWeights = math.lerp(1f, lengthWeights, binding.distanceWeights);
                binding.distanceWeightOutputs = lengthWeights;

                var rotOffsetA = math.mul(new quaternion(localRotations.c0), new quaternion(binding.targetInverseStartRotations.c0));
                var rotOffsetB = math.mul(new quaternion(localRotations.c1), new quaternion(binding.targetInverseStartRotations.c1));
                var rotOffsetC = math.mul(new quaternion(localRotations.c2), new quaternion(binding.targetInverseStartRotations.c2));
                var rotOffsetD = math.mul(new quaternion(localRotations.c3), new quaternion(binding.targetInverseStartRotations.c3));

                var rotTemporalOffsetA = math.mul(new quaternion(localRotations.c0), math.inverse(new quaternion(prevLocalRotations.c0)));
                var rotTemporalOffsetB = math.mul(new quaternion(localRotations.c1), math.inverse(new quaternion(prevLocalRotations.c1)));
                var rotTemporalOffsetC = math.mul(new quaternion(localRotations.c2), math.inverse(new quaternion(prevLocalRotations.c2)));
                var rotTemporalOffsetD = math.mul(new quaternion(localRotations.c3), math.inverse(new quaternion(prevLocalRotations.c3)));

                //var rotOffsetA = math.mul(new quaternion(binding.targetInverseStartRotations.c0), new quaternion(localRotations.c0));
                //var rotOffsetB = math.mul(new quaternion(binding.targetInverseStartRotations.c1), new quaternion(localRotations.c1));
                //var rotOffsetC = math.mul(new quaternion(binding.targetInverseStartRotations.c2), new quaternion(localRotations.c2));
                //var rotOffsetD = math.mul(new quaternion(binding.targetInverseStartRotations.c3), new quaternion(localRotations.c3));

                //Debug.Log($"DIFF {(math.angle(math.inverse(new quaternion(binding.targetInverseStartRotations.c0)), new quaternion(localRotations.c0)) * Mathf.Rad2Deg)}"); 

                var vecA = math.rotate(rotOffsetA, binding.targetStartVectors.c0);
                var vecB = math.rotate(rotOffsetB, binding.targetStartVectors.c1);
                var vecC = math.rotate(rotOffsetC, binding.targetStartVectors.c2);
                var vecD = math.rotate(rotOffsetD, binding.targetStartVectors.c3);

                var angSpdVecA = math.rotate(rotTemporalOffsetA, binding.targetStartVectors.c0);
                var angSpdVecB = math.rotate(rotTemporalOffsetB, binding.targetStartVectors.c1);
                var angSpdVecC = math.rotate(rotTemporalOffsetC, binding.targetStartVectors.c2);
                var angSpdVecD = math.rotate(rotTemporalOffsetD, binding.targetStartVectors.c3);

                //var axisA = math.rotate(localRotations.c0, binding.targetAngleAxisVectors.c0);
                //var axisB = math.rotate(localRotations.c1, binding.targetAngleAxisVectors.c1);
                //var axisC = math.rotate(localRotations.c2, binding.targetAngleAxisVectors.c2);
                //var axisD = math.rotate(localRotations.c3, binding.targetAngleAxisVectors.c3);

                var axis2A = math.rotate(rotOffsetA, binding.targetAngleStartAxisVectors.c0);
                var axis2B = math.rotate(rotOffsetB, binding.targetAngleStartAxisVectors.c1);
                var axis2C = math.rotate(rotOffsetC, binding.targetAngleStartAxisVectors.c2);
                var axis2D = math.rotate(rotOffsetD, binding.targetAngleStartAxisVectors.c3);

                var axisWeightA = math.dot(binding.targetAngleStartAxisVectors.c0, axis2A);
                var axisWeightB = math.dot(binding.targetAngleStartAxisVectors.c1, axis2B);
                var axisWeightC = math.dot(binding.targetAngleStartAxisVectors.c2, axis2C);
                var axisWeightD = math.dot(binding.targetAngleStartAxisVectors.c3, axis2D);
                float4 axisWeights = math.saturate(math.remap(binding.angleAxisDotFalloffLowerBounds, 1f, 0f, 1f, new float4(axisWeightA, axisWeightB, axisWeightC, axisWeightD)));
                //Debug.Log($"AXIS: {axisWeights}"); // UNCOMMENT BURSTCOMPILE

                var rotationWeightValidation = new bool4(math.any(binding.targetStartVectors.c0 != 0f), math.any(binding.targetStartVectors.c1 != 0f), math.any(binding.targetStartVectors.c2 != 0f), math.any(binding.targetStartVectors.c3 != 0f));

                var rotWeightA = Vector3.SignedAngle(binding.targetStartVectors.c0, vecA, axis2A);
                var rotWeightB = Vector3.SignedAngle(binding.targetStartVectors.c1, vecB, axis2B);
                var rotWeightC = Vector3.SignedAngle(binding.targetStartVectors.c2, vecC, axis2C);
                var rotWeightD = Vector3.SignedAngle(binding.targetStartVectors.c3, vecD, axis2D);  
                float4 rotWeights = new float4(rotWeightA, rotWeightB, rotWeightC, rotWeightD); 
                rotWeights = math.select(rotWeights, math.abs(rotWeights), binding.absoluteAngleFlags);
                float4 targetAngleRanges = binding.targetAnglesDegreesMax - binding.targetAnglesDegreesMin;
                rotWeights = (rotWeights - binding.targetAnglesDegreesMin) / targetAngleRanges;
                rotWeights = math.select(float4.zero, rotWeights, targetAngleRanges != 0f);
                rotWeights = math.saturate(math.select(1f - math.min(1f, math.abs(rotWeights - 1f)), rotWeights, binding.angleWeightClampFlags));


                var angularSpeedWeightA = Vector3.SignedAngle(binding.targetStartVectors.c0, angSpdVecA, axis2A);
                var angularSpeedWeightB = Vector3.SignedAngle(binding.targetStartVectors.c1, angSpdVecB, axis2B);
                var angularSpeedWeightC = Vector3.SignedAngle(binding.targetStartVectors.c2, angSpdVecC, axis2C);
                var angularSpeedWeightD = Vector3.SignedAngle(binding.targetStartVectors.c3, angSpdVecD, axis2D);
                float4 angularSpeedWeights = new float4(angularSpeedWeightA, angularSpeedWeightB, angularSpeedWeightC, angularSpeedWeightD) / deltaTime;  
                angularSpeedWeights = math.select(angularSpeedWeights, math.abs(angularSpeedWeights), binding.absoluteAngleFlags);
                float4 angularSpeedRanges = binding.angularSpeedRangesMax - binding.angularSpeedRangesMin;
                angularSpeedWeights = (angularSpeedWeights - binding.angularSpeedRangesMin) / angularSpeedRanges;
                angularSpeedWeights = math.saturate(math.select(1f - math.min(1f, math.abs(angularSpeedWeights - 1f)), angularSpeedWeights, binding.angleWeightClampFlags));
                angularSpeedWeights = math.lerp(binding.angularSpeedFlexMultipliersMin, binding.angularSpeedFlexMultipliersMax, angularSpeedWeights);
                bool4 angularSpeedWeightValidation = angularSpeedRanges != 0f;
                angularSpeedWeights = math.select(new float4(1f, 1f, 1f, 1f), angularSpeedWeights, angularSpeedWeightValidation);


                rotWeights = math.select(angularSpeedWeights, rotWeights * angularSpeedWeights, (math.sign(angularSpeedWeights) >= 0f) | !angularSpeedWeightValidation); 
                //rotWeights = math.select(rotWeights, math.select(math.min(angularSpeedWeights, rotWeights), math.max(angularSpeedWeights, rotWeights), math.sign(angularSpeedWeights) >= 0f), angularSpeedWeightValidation);  

                rotWeights = math.select(rotWeights, 1f - rotWeights, binding.invertAngleWeightFlags);
                rotWeights = math.select(float4.zero, rotWeights, rotationWeightValidation);
                float angleWeightsTotal = math.csum(binding.angleWeights);
                rotWeights = math.lerp(1f, rotWeights * axisWeights, binding.angleWeights); 
                binding.angleWeightOutputs = rotWeights;

                float totalWeight = distanceWeightsTotal + angleWeightsTotal;
                //binding.output = math.select(0f, (math.csum(lengthWeights) + math.csum(rotWeights)) / totalWeight, totalWeight != 0f);
                binding.output = math.select(0f, lengthWeights.x * lengthWeights.y * lengthWeights.z * lengthWeights.w * rotWeights.x * rotWeights.y * rotWeights.z * rotWeights.w, totalWeight != 0f); 

                bindings[index] = binding;
            }
        }
    }
}

#endif