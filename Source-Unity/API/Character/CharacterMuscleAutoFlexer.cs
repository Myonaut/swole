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
            [Tooltip("How much the angular speed multiplier is allowed to affect the output (0 = ignore velocity, 1 = full effect)."), Range(0f, 1f)]
            public float angularSpeedInfluence = 1f;
            [Tooltip("Ignore the pose angle entirely and drive output only by the total angular speed of the transform (axis independent). Output fades to zero when the transform stops moving.")]
            public bool angularSpeedOnly;

            public TransformBinding MemberwiseCloneBinding() => (TransformBinding)MemberwiseClone();
        }

        [Serializable]
        public enum FlexMixType
        {
            Max, Mul, Add
        }

        [Serializable]
        public enum InputMixType
        {
            [Tooltip("Output is scaled towards the input (lerp(1, input, weight)).")]
            Multiply,
            [Tooltip("Input * weight is added to the output.")]
            Add,
            [Tooltip("Output becomes max(output, input * weight).")]
            Max
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
            [Tooltip("If true, this flexer's contribution is reduced by how much the muscle is already flexed (e.g. by the animation) before any auto flexing is applied.")]
            public bool limitByExistingFlex;
            [Tooltip("Existing flex range remapped to the limit (x = limiting starts, y = contribution fully blocked).")]
            public Vector2 existingFlexLimitRange = new Vector2(0f, 1f);
            [Range(0f, 1f), Tooltip("How strongly existing flex blocks this contribution (1 = fully blocked at the end of the range).")]
            public float existingFlexLimitStrength = 1f;

            public float GetExistingFlexLimit(float existingFlex)
            {
                if (!limitByExistingFlex) return 1f;
                float t = existingFlexLimitRange.y > existingFlexLimitRange.x ? Mathf.InverseLerp(existingFlexLimitRange.x, existingFlexLimitRange.y, existingFlex) : (existingFlex >= existingFlexLimitRange.x ? 1f : 0f);
                return 1f - Mathf.SmoothStep(0f, 1f, t) * existingFlexLimitStrength;
            }

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

            [Header("Pose Gate"), Tooltip("Optional: output is faded out as the gate transform's axis points away from the reference direction (e.g. blend out when the upper arm is raised).")]
            public Transform gateTransformLeft;
            public Transform gateTransformRight;
            [Tooltip("Local axis of the gate transform to measure (bone direction).")]
            public Vector3 gateLocalAxis = Vector3.up;
            [Tooltip("Space the reference direction is defined in. Defaults to the auto flexer's transform.")]
            public Transform gateReferenceSpace;
            public Vector3 gateReferenceDirection = Vector3.down;
            [Tooltip("Angle (degrees) from the reference direction where the gate starts fading (x) and is fully closed (y).")]
            public Vector2 gateAngleRange = new Vector2(35f, 75f);
            [Tooltip("If true the gate only affects transform outputs, not muscle flex.")]
            public bool gateTransformsOnly;

            [Header("Bust Size Blend"), Tooltip("Blend transform offsets towards the 'large bust' offsets based on the character's bust size.")]
            public bool useBustSizeBlend;
            public Vector2 bustSizeRange = new Vector2(0f, 1f);
            public Vector3 largeBustLocalPositionOffset;
            public Vector3 largeBustLocalRotationOffsetEuler;
            public Vector3 largeBustLocalScaleOffset;

            [Header("External Input"), Tooltip("Optional named input channel (e.g. 'exertion', 'load', 'breathExhale') read from the component's input providers or SetInput. Leave empty to ignore.")]
            public string inputChannel;
            [Tooltip("If true, '<channel>.L' / '<channel>.R' are queried first for each side, falling back to '<channel>'.")]
            public bool inputPerSide;
            [Tooltip("Raw input range remapped to 0-1.")]
            public Vector2 inputRange = new Vector2(0f, 1f);
            public InputMixType inputMixType;
            public float inputWeight = 1f;
            [Tooltip("If true, the input only affects transform outputs, not muscle flex.")]
            public bool inputTransformsOnly;

            [Header("Antagonist"), Tooltip("Optional name of another auto flexer on this component whose output suppresses this one (e.g. triceps suppressing biceps).")]
            public string antagonistFlexer;
            [Range(0f, 1f)]
            public float antagonistSuppression = 0.5f;

            [NonSerialized]
            public AutoFlexer antagonist;

            [NonSerialized]
            public float bustBlend;
            [NonSerialized]
            public Transform owner;
            [NonSerialized]
            public CharacterMuscleAutoFlexer component;

            private static float MixInput(float output, float input, InputMixType mixType, float weight)
            {
                switch (mixType)
                {
                    case InputMixType.Add: return output + input * weight;
                    case InputMixType.Max: return Mathf.Max(output, input * weight);
                    default: return output * Mathf.LerpUnclamped(1f, input, weight);
                }
            }

            private void ApplyExternalModifiers(ref float flexLeft, ref float flexRight, ref float transformFlexLeft, ref float transformFlexRight)
            {
                if (component != null && !string.IsNullOrEmpty(inputChannel))
                {
                    float inL = component.GetInput(inputChannel, inputPerSide ? ".L" : null);
                    float inR = component.GetInput(inputChannel, inputPerSide ? ".R" : null);
                    if (inputRange.y != inputRange.x)
                    {
                        inL = Mathf.InverseLerp(inputRange.x, inputRange.y, inL);
                        inR = Mathf.InverseLerp(inputRange.x, inputRange.y, inR);
                    }
                    transformFlexLeft = MixInput(transformFlexLeft, inL, inputMixType, inputWeight);
                    transformFlexRight = MixInput(transformFlexRight, inR, inputMixType, inputWeight);
                    if (!inputTransformsOnly)
                    {
                        flexLeft = MixInput(flexLeft, inL, inputMixType, inputWeight);
                        flexRight = MixInput(flexRight, inR, inputMixType, inputWeight);
                    }
                }
                if (antagonist != null && antagonistSuppression != 0f)
                {
                    float sL = 1f - Mathf.Clamp01(antagonist.flexOutputLeft) * antagonistSuppression;
                    float sR = 1f - Mathf.Clamp01(antagonist.flexOutputRight) * antagonistSuppression;
                    flexLeft *= sL;
                    flexRight *= sR;
                    transformFlexLeft *= sL;
                    transformFlexRight *= sR;
                }
            }

            public AutoFlexer MemberwiseCloneFlexer() => (AutoFlexer)MemberwiseClone();

            private float EvaluateGate(Transform gateTransform)
            {
                if (gateTransform == null) return 1f;
                var space = gateReferenceSpace != null ? gateReferenceSpace : owner;
                Vector3 refDir = space != null ? space.TransformDirection(gateReferenceDirection) : gateReferenceDirection;
                Vector3 axis = gateTransform.TransformDirection(gateLocalAxis);
                float angle = Vector3.Angle(axis, refDir);
                if (gateAngleRange.y <= gateAngleRange.x) return angle <= gateAngleRange.x ? 1f : 0f;
                return 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(gateAngleRange.x, gateAngleRange.y, angle));
            }

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
                ApplyExternalModifiers(ref flexLeft, ref flexRight, ref transformFlexLeft, ref transformFlexRight);
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

                if (gateTransformLeft != null || gateTransformRight != null)
                {
                    float gateL = EvaluateGate(gateTransformLeft);
                    float gateR = EvaluateGate(gateTransformRight);
                    transformFlexLeft *= gateL;
                    transformFlexRight *= gateR;
                    if (!gateTransformsOnly)
                    {
                        flexLeft *= gateL;
                        flexRight *= gateR;
                    }
                }

                if (useBustSizeBlend && characterMesh != null && characterMesh.IsInitialized)
                {
                    bustBlend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(bustSizeRange.x, bustSizeRange.y, characterMesh.BustSize));
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

                gradualFlexOutputLeft = SmoothTowards(gradualFlexOutputLeft, flexLeft, deltaTime, gradualFlexingSyncSpeed);
                gradualFlexOutputRight = SmoothTowards(gradualFlexOutputRight, flexRight, deltaTime, gradualFlexingSyncSpeed);

                transformFlexOutputLeft = transformFlexLeft;
                transformFlexOutputRight = transformFlexRight;

                gradualTransformFlexOutputLeft = SmoothTowards(gradualTransformFlexOutputLeft, transformFlexLeft, deltaTime, gradualFlexingSyncSpeed);
                gradualTransformFlexOutputRight = SmoothTowards(gradualTransformFlexOutputRight, transformFlexRight, deltaTime, gradualFlexingSyncSpeed);
            }

            /// <summary>
            /// Frame-rate independent exponential approach with a linear speed cap, so large target jumps ease in instead of popping.
            /// </summary>
            private static float SmoothTowards(float current, float target, float deltaTime, float speed)
            {
                if (speed <= 0f) return target;
                float smoothed = Mathf.Lerp(current, target, 1f - Mathf.Exp(-speed * 1.5f * deltaTime));
                return Mathf.MoveTowards(current, smoothed, deltaTime * speed * 1.5f);
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
            public void Initialize(Transform owner)
            {
                this.owner = owner;
                Initialize();
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

                Vector3 posOffsetL = muscleTargetLocalPositionOffset;
                Vector3 posOffsetR = muscleTargetLocalPositionOffset_Right;
                // Scale euler angles directly (rather than slerping a quaternion) so large offsets (>= 180 degrees) scale linearly instead of taking the shortest arc.
                Vector3 rotEulerL = muscleTargetLocalRotationOffsetEuler;
                Vector3 rotEulerR = Vector3.Scale(muscleTargetLocalRotationOffsetEuler, rightMuscleTargetLocalRotationOffsetEulerMultiplier);
                Vector3 scaleOffset = muscleTargetLocalScaleOffset;
                if (useBustSizeBlend && bustBlend > 0f)
                {
                    posOffsetL = Vector3.Lerp(posOffsetL, largeBustLocalPositionOffset, bustBlend);
                    posOffsetR = Vector3.Lerp(posOffsetR, Vector3.Scale(largeBustLocalPositionOffset, rightMuscleTargetLocalPositionOffsetMultiplier), bustBlend);
                    rotEulerL = Vector3.Lerp(muscleTargetLocalRotationOffsetEuler, largeBustLocalRotationOffsetEuler, bustBlend);
                    rotEulerR = Vector3.Scale(rotEulerL, rightMuscleTargetLocalRotationOffsetEulerMultiplier);
                    scaleOffset = Vector3.Lerp(scaleOffset, largeBustLocalScaleOffset, bustBlend);
                }

                if (muscleTransformLeft != null)
                { 
                    //Debug.Log($"{muscleTransformLeft} : {flexValues.x}");
                    if (resetStates) muscleTransformLeftStartState.ApplyLocal(muscleTransformLeft);

                    muscleTransformLeft.GetLocalPositionAndRotation(out var localPos, out var localRot);
                    var localScale = muscleTransformLeft.localScale;
                    localPos = localPos + posOffsetL * flexValues.x;
                    localRot = Quaternion.Euler(rotEulerL * flexValues.x) * localRot;
                    localScale = localScale + scaleOffset * flexValues.x;
                    muscleTransformLeft.SetLocalPositionAndRotation(localPos, localRot);
                    muscleTransformLeft.localScale = localScale;
                }
                if (muscleTransformRight != null)
                {
                    //Debug.Log($"{muscleTransformRight} : {flexValues.y}");

                    if (resetStates) muscleTransformRightStartState.ApplyLocal(muscleTransformRight);

                    muscleTransformRight.GetLocalPositionAndRotation(out var localPos, out var localRot);
                    var localScale = muscleTransformRight.localScale;
                    localPos = localPos + posOffsetR * flexValues.y;
                    localRot = Quaternion.Euler(rotEulerR * flexValues.y) * localRot;
                    localScale = localScale + scaleOffset * flexValues.y;
                    muscleTransformRight.SetLocalPositionAndRotation(localPos, localRot);
                    muscleTransformRight.localScale = localScale;
                }
            }
        }

#if UNITY_EDITOR
        [SerializeField, Tooltip("Auto flexer to copy data from. Bone references are remapped to bones at the same hierarchy path under this component's root.")]
#endif
        private CharacterMuscleAutoFlexer copySource;
#if UNITY_EDITOR
        [SerializeField, Tooltip("Check to copy all auto flexer data from the copy source. Resets itself afterwards.")]
#endif
        private bool copyFromSource;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (copyFromSource)
            {
                copyFromSource = false;
                if (copySource != null && copySource != this)
                {
                    UnityEditor.Undo.RecordObject(this, "Copy Auto Flexer Data");
                    CopyFrom(copySource);
                    UnityEditor.EditorUtility.SetDirty(this);
                }
            }

            if (autoFlexers != null)
            {
                foreach (var flexer in autoFlexers) if (flexer != null) flexer.ReinitializeOffsets(); 
            }
        }
#endif

        /// <summary>
        /// Copies all auto flexer data from another component. Transform references are remapped to transforms with the same relative hierarchy path under this component's transform.
        /// </summary>
        public void CopyFrom(CharacterMuscleAutoFlexer source)
        {
            if (source == null || source == this) return;

            autoFlexingMix = source.autoFlexingMix;

            if (source.autoFlexers == null)
            {
                autoFlexers = null;
                return;
            }

            var srcRoot = source.transform;
            var dstRoot = transform;
            var cache = new Dictionary<Transform, Transform>();

            Transform Remap(Transform t)
            {
                if (t == null) return null;
                if (cache.TryGetValue(t, out var result)) return result;

                result = null;
                if (t == srcRoot)
                {
                    result = dstRoot;
                }
                else if (t.IsChildOf(srcRoot))
                {
                    var path = new List<string>();
                    var current = t;
                    while (current != null && current != srcRoot)
                    {
                        path.Add(current.name);
                        current = current.parent;
                    }
                    path.Reverse();
                    result = dstRoot.Find(string.Join("/", path));
                    if (result == null) Debug.LogWarning($"[{nameof(CharacterMuscleAutoFlexer)}] Could not find '{string.Join("/", path)}' under '{dstRoot.name}' while copying auto flexer data.", this);
                }
                else
                {
                    result = t; // external reference, keep as is
                }

                cache[t] = result;
                return result;
            }

            TransformBinding CopyBinding(TransformBinding b)
            {
                if (b == null) return null;
                var c = (TransformBinding)b.MemberwiseCloneBinding();
                c.transformLeft = Remap(b.transformLeft);
                c.transformRight = Remap(b.transformRight);
                return c;
            }

            var newFlexers = new AutoFlexer[source.autoFlexers.Length];
            for (int i = 0; i < newFlexers.Length; i++)
            {
                var src = source.autoFlexers[i];
                if (src == null) continue;

                var dst = src.MemberwiseCloneFlexer();
                dst.bindingA = CopyBinding(src.bindingA);
                dst.bindingB = CopyBinding(src.bindingB);
                dst.bindingC = CopyBinding(src.bindingC);
                dst.bindingD = CopyBinding(src.bindingD);

                if (src.muscles != null)
                {
                    dst.muscles = new MuscleDependency[src.muscles.Length];
                    for (int m = 0; m < src.muscles.Length; m++)
                    {
                        var sm = src.muscles[m];
                        if (sm == null) continue;
                        dst.muscles[m] = new MuscleDependency()
                        {
                            name = sm.name,
                            massScaling = sm.massScaling,
                            massScalingTransforms = sm.massScalingTransforms,
                            massMax = sm.massMax,
                            flexScale = sm.flexScale,
                            flexRemap = sm.flexRemap,
                            flexMixType = sm.flexMixType,
                            limitByExistingFlex = sm.limitByExistingFlex,
                            existingFlexLimitRange = sm.existingFlexLimitRange,
                            existingFlexLimitStrength = sm.existingFlexLimitStrength,
                            cachedMuscleIndex = -1
                        };
                    }
                }

                dst.muscleTransformLeft = Remap(src.muscleTransformLeft);
                dst.muscleTransformRight = Remap(src.muscleTransformRight);
                dst.gateTransformLeft = Remap(src.gateTransformLeft);
                dst.gateTransformRight = Remap(src.gateTransformRight);
                dst.gateReferenceSpace = Remap(src.gateReferenceSpace);
                dst.owner = dstRoot;
                dst.component = this;
                dst.antagonist = null;
                dst.jobIndexLeft = -1;
                dst.jobIndexRight = -1;
                dst.ReinitializeOffsets();

                newFlexers[i] = dst;
            }

            autoFlexers = newFlexers;
        }

        public CustomizableCharacterMesh characterMesh;
        public CustomizableCharacterMeshBase characterMeshV2;
        public ICustomizableCharacter CharacterMesh => characterMeshV2 == null ? characterMesh : characterMeshV2;
        public CustomAnimator animator;

        [AnimatableProperty(true, 1f), Range(0, 2f)]
        public float autoFlexingMix = 1f;

        public AutoFlexer[] autoFlexers;
        [NonSerialized]
        private AutoFlexer[] autoFlexersWithTransforms;

        [Tooltip("Components implementing IAutoFlexInputProvider that supply named input channels. Providers on this object's hierarchy are found automatically if this is empty.")]
        public List<MonoBehaviour> inputProviders = new List<MonoBehaviour>();

        [NonSerialized]
        private readonly Dictionary<string, float> inputOverrides = new Dictionary<string, float>();

        /// <summary>Sets a named input directly
        public void SetInput(string channel, float value) { if (!string.IsNullOrEmpty(channel)) inputOverrides[channel] = value; }
        public void ClearInput(string channel) { if (!string.IsNullOrEmpty(channel)) inputOverrides.Remove(channel); }

        public bool TryGetInput(string channel, out float value)
        {
            if (inputOverrides.TryGetValue(channel, out value)) return true;
            return AutoFlexInputUtility.TryGetInput(inputProviders, channel, out value);
        }
        /// <summary>Gets a named input, trying '<channel><sideSuffix>' first if a suffix is given. Returns 0 if not supplied.</summary>
        public float GetInput(string channel, string sideSuffix = null)
        {
            if (!string.IsNullOrEmpty(sideSuffix) && TryGetInput(channel + sideSuffix, out float sided)) return sided;
            return TryGetInput(channel, out float value) ? value : 0f;
        }

        public AutoFlexer FindAutoFlexer(string name)
        {
            if (autoFlexers == null || string.IsNullOrEmpty(name)) return null;
            foreach (var f in autoFlexers) if (f != null && f.name == name) return f;
            return null;
        }

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
            if (characterMeshV2 == null) characterMeshV2 = gameObject.GetComponentInChildren<CustomizableCharacterMeshBase>(true);
            if (animator == null) animator = gameObject.GetComponentInChildren<CustomAnimator>(true); 

            if (inputProviders == null) inputProviders = new List<MonoBehaviour>();
            inputProviders.RemoveAll(p => p == null);
            if (inputProviders.Count == 0) AutoFlexInputUtility.FindProviders(transform.root, inputProviders);

            if (autoFlexers != null)
            {
                List<AutoFlexer> autoFlexersWithTransforms = new List<AutoFlexer>();
                foreach (var autoFlexer in autoFlexers)
                {
                    if (autoFlexer == null) continue;

                    autoFlexer.component = this;
                    autoFlexer.antagonist = string.IsNullOrEmpty(autoFlexer.antagonistFlexer) ? null : FindAutoFlexer(autoFlexer.antagonistFlexer);
                    if (ReferenceEquals(autoFlexer.antagonist, autoFlexer)) autoFlexer.antagonist = null;
                    autoFlexer.Initialize(transform);
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
                    float existingFlexL = muscleData.valuesLeft.flex;
                    float existingFlexR = muscleData.valuesRight.flex;
                    bool isFirstL = true;
                    bool isFirstR = true;
                    foreach (var flexerBinding in entry.Value)
                    {
                        var flexer = flexerBinding.flexer; 
                        var localMuscle = flexer.muscles[flexerBinding.localMuscleIndex];
                        if (localMuscle.flexScale == 0f) continue; 
                        
                        float2 flexValues = math.remap(0f, 1f, localMuscle.flexRemap.x, localMuscle.flexRemap.y, flexer.useGradualFlexingForCharacterMesh ? new float2(flexer.gradualFlexOutputLeft, flexer.gradualFlexOutputRight) : new float2(flexer.flexOutputLeft, flexer.flexOutputRight)) * localMuscle.flexScale;
                        if (localMuscle.limitByExistingFlex) flexValues *= new float2(localMuscle.GetExistingFlexLimit(existingFlexL), localMuscle.GetExistingFlexLimit(existingFlexR));

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
            binding.angularSpeedInfluences = math.saturate(new float4(bindingA.angularSpeedInfluence, bindingB.angularSpeedInfluence, bindingC.angularSpeedInfluence, bindingD.angularSpeedInfluence));
            binding.smoothedAngularSpeeds = float4.zero;
            binding.angularSpeedOnlyFlags = new bool4(bindingA.angularSpeedOnly, bindingB.angularSpeedOnly, bindingC.angularSpeedOnly, bindingD.angularSpeedOnly);

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

        public override void OnDestroyed()
        {
            base.OnDestroyed();

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
            public float4 angularSpeedInfluences;
            public float4 smoothedAngularSpeeds;
            public bool4 angularSpeedOnlyFlags;
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
                lengthWeights = math.smoothstep(0f, 1f, lengthWeights); // soften range edges to avoid visible kinks
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
                axisWeights = math.smoothstep(0f, 1f, axisWeights); // smooth axis falloff so off-axis limb orientations fade instead of kinking
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
                rotWeights = math.smoothstep(0f, 1f, rotWeights); // soften range edges to avoid visible kinks


                var angularSpeedWeightA = Vector3.SignedAngle(binding.targetStartVectors.c0, angSpdVecA, axis2A);
                var angularSpeedWeightB = Vector3.SignedAngle(binding.targetStartVectors.c1, angSpdVecB, axis2B);
                var angularSpeedWeightC = Vector3.SignedAngle(binding.targetStartVectors.c2, angSpdVecC, axis2C);
                var angularSpeedWeightD = Vector3.SignedAngle(binding.targetStartVectors.c3, angSpdVecD, axis2D);
                // Clamp delta time so hitches/tiny frames can't explode the velocity estimate, then low-pass filter it so single-frame spikes don't pop the flex.
                float safeDeltaTime = math.max(deltaTime, 1f / 240f);
                float4 rawAngularSpeeds = new float4(angularSpeedWeightA, angularSpeedWeightB, angularSpeedWeightC, angularSpeedWeightD);
                float4 totalAngularDeltas = math.degrees(new float4(math.angle(quaternion.identity, rotTemporalOffsetA), math.angle(quaternion.identity, rotTemporalOffsetB), math.angle(quaternion.identity, rotTemporalOffsetC), math.angle(quaternion.identity, rotTemporalOffsetD)));
                rawAngularSpeeds = math.select(rawAngularSpeeds, totalAngularDeltas, binding.angularSpeedOnlyFlags) / safeDeltaTime;
                rawAngularSpeeds = math.select(rawAngularSpeeds, float4.zero, deltaTime <= 0f);
                float speedFilter = 1f - math.exp(-safeDeltaTime / 0.09f);
                binding.smoothedAngularSpeeds = math.lerp(binding.smoothedAngularSpeeds, rawAngularSpeeds, speedFilter);
                float4 angularSpeedWeights = binding.smoothedAngularSpeeds;
                angularSpeedWeights = math.select(angularSpeedWeights, math.abs(angularSpeedWeights), binding.absoluteAngleFlags);
                float4 angularSpeedRanges = binding.angularSpeedRangesMax - binding.angularSpeedRangesMin;
                angularSpeedWeights = (angularSpeedWeights - binding.angularSpeedRangesMin) / angularSpeedRanges;
                angularSpeedWeights = math.saturate(math.select(1f - math.min(1f, math.abs(angularSpeedWeights - 1f)), angularSpeedWeights, binding.angleWeightClampFlags));
                angularSpeedWeights = math.smoothstep(0f, 1f, angularSpeedWeights);
                angularSpeedWeights = math.lerp(binding.angularSpeedFlexMultipliersMin, binding.angularSpeedFlexMultipliersMax, angularSpeedWeights);
                bool4 angularSpeedWeightValidation = angularSpeedRanges != 0f;
                angularSpeedWeights = math.select(new float4(1f, 1f, 1f, 1f), angularSpeedWeights, angularSpeedWeightValidation);
                angularSpeedWeights = math.lerp(1f, angularSpeedWeights, binding.angularSpeedInfluences);

                // Always multiply (never replace) so the result stays continuous with the pose-based weight; negative multipliers can still pull flex below zero.
                rotWeights = math.select(rotWeights, 1f, binding.angularSpeedOnlyFlags) * angularSpeedWeights; 
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