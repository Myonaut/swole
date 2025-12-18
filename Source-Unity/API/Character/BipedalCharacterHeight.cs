#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Unity.Mathematics;

using Swole.API.Unity.Animation;
using Swole.DataStructures;
using Swole.Morphing;

namespace Swole.API.Unity
{
    public class BipedalCharacterHeight : MonoBehaviour
    {

        // Concept: If a foot is grounded, move the pelvis to achieve the original knee bend angle and foot position. Otherwise, change the bend of the knee to get the same foot position.

#if UNITY_EDITOR

        [NonSerialized]
        protected float prevSpineLength = 1f;

        [NonSerialized]
        protected float prevNeckLength = 1f;

        [NonSerialized]
        protected float prevShoulderWidth = 1f;
        [NonSerialized]
        protected float prevArmLength = 1f;
        [NonSerialized]
        protected float prevForearmLength = 1f;

        [NonSerialized]
        protected float prevHipWidth = 1f;
        [NonSerialized]
        protected float prevLegLength = 1f;
        [NonSerialized]
        protected float prevCalfLength = 1f;


        [NonSerialized]
        protected float prevHeightPreserveHandPositionLeft = 1f;
        [NonSerialized]
        protected float prevHeightPreserveHandPositionRight = 1f;

        [NonSerialized]
        protected float prevHeightPreserveFootPositionLeft = 1f;
        [NonSerialized]
        protected float prevHeightPreserveFootPositionRight = 1f;

        [NonSerialized]
        protected float prevHeightPreserveKneeBendLeft = 1f;
        [NonSerialized]
        protected float prevHeightPreserveKneeBendRight = 1f;


        [NonSerialized]
        protected float prevShoulderArmWidthSplit = 0.5f;
        [NonSerialized]
        protected float prevHipThighWidthSplit = 0.5f;


        protected void OnValidate()
        {
            if (spineLength != prevSpineLength)
            {
                SetSpineLength(spineLength);
                prevSpineLength = spineLength;
            }

            if (neckLength != prevNeckLength)
            {
                SetNeckLength(neckLength);
                prevNeckLength = neckLength; 
            }

            if (shoulderWidth != prevShoulderWidth)
            {
                SetShoulderWidth(shoulderWidth);
                prevShoulderWidth = shoulderWidth;
            }
            if (armLength != prevArmLength)
            {
                SetArmLength(armLength);
                prevArmLength = armLength;
            }
            if (forearmLength != prevForearmLength)
            {
                SetForearmLength(forearmLength);
                prevForearmLength = forearmLength;
            }

            if (hipWidth != prevHipWidth)
            {
                SetHipWidth(hipWidth);
                prevHipWidth = hipWidth; 
            }
            if (legLength != prevLegLength)
            {
                SetLegLength(legLength);
                prevLegLength = legLength;
            }
            if (calfLength != prevCalfLength)
            {
                SetCalfLength(calfLength);
                prevCalfLength = calfLength;
            }

            if (heightPreserveHandPositionLeft != prevHeightPreserveHandPositionLeft)
            {
                SetPreserveHandPositionLeftWeight(heightPreserveHandPositionLeft);
                prevHeightPreserveHandPositionLeft = heightPreserveHandPositionLeft;
            }
            if (heightPreserveHandPositionRight != prevHeightPreserveHandPositionRight)
            {
                SetPreserveHandPositionRightWeight(heightPreserveHandPositionRight);
                prevHeightPreserveHandPositionRight = heightPreserveHandPositionRight;
            }

            if (heightPreserveFootPositionLeft != prevHeightPreserveFootPositionLeft)
            {
                SetPreserveFootPositionLeftWeight(heightPreserveFootPositionLeft);
                prevHeightPreserveFootPositionLeft = heightPreserveFootPositionLeft;
            }
            if (heightPreserveFootPositionRight != prevHeightPreserveFootPositionRight)
            {
                SetPreserveFootPositionRightWeight(heightPreserveFootPositionRight);
                prevHeightPreserveFootPositionRight = heightPreserveFootPositionRight;
            }
            if (heightPreserveKneeBendLeft != prevHeightPreserveKneeBendLeft)
            {
                SetPreserveKneeBendLeftWeight(heightPreserveKneeBendLeft);
                prevHeightPreserveKneeBendLeft = heightPreserveKneeBendLeft; 
            }
            if (heightPreserveKneeBendRight != prevHeightPreserveKneeBendRight)
            {
                SetPreserveKneeBendRightWeight(heightPreserveKneeBendRight);
                prevHeightPreserveKneeBendRight = heightPreserveKneeBendRight;
            }

            if (shoulderArmWidthSplit != prevShoulderArmWidthSplit)
            {
                SetShoulderArmWidthSplit(shoulderArmWidthSplit);
                prevShoulderArmWidthSplit = shoulderArmWidthSplit;
            }
            if (hipThighWidthSplit != prevHipThighWidthSplit)
            {
                SetHipThighWidthSplit(hipThighWidthSplit);
                prevHipThighWidthSplit = hipThighWidthSplit;
            }
        }

#endif

        public Transform pelvis;

        public Transform spine1;
        public Transform spine2;
        public Transform spine3;

        public Transform neck;

        public Transform shoulderLeft;
        public Transform armLeft;
        public Transform forearmLeft;
        public Transform wristLeft;

        public Transform shoulderRight;
        public Transform armRight;
        public Transform forearmRight;
        public Transform wristRight;

        public Transform hipLeft;
        public Transform legLeft;
        public Transform calfLeft;
        public Transform footLeft;

        public Transform hipRight;
        public Transform legRight;
        public Transform calfRight;
        public Transform footRight;

        public List<Transform> GetAffectedTransforms(List<Transform> list = null)
        {
            if (list == null) list = new List<Transform>();

            list.Add(pelvis);

            list.Add(spine1);
            list.Add(spine2);
            list.Add(spine3);

            list.Add(neck);

            list.Add(shoulderLeft);
            list.Add(armLeft);
            list.Add(forearmLeft);
            list.Add(wristLeft);

            list.Add(shoulderRight);
            list.Add(armRight);
            list.Add(forearmRight);
            list.Add(wristRight);

            list.Add(hipLeft);
            list.Add(legLeft);
            list.Add(calfLeft);
            list.Add(footLeft);

            list.Add(hipRight);
            list.Add(legRight);
            list.Add(calfRight);
            list.Add(footRight);

            return list;
        }

        public float StandingHeightContribution
        {
            get
            {
                float contribution = 0f;

                contribution += (RealSpineLength - DefaultSpineLength) * 2;
                contribution += RealNeckLength - DefaultNeckLength;
                contribution += RealLegLength - DefaultLegLength;
                contribution += RealCalfLength - DefaultCalfLength;

                return contribution;
            }
        }

        [SerializeField, Range(0.5f, 2f)]
        protected float spineLength = 1f;
        [SerializeField, Range(0.5f, 2f)]
        protected float neckLength = 1f;

        public float RealSpineLength => spineLength * defaultSpineLength;
        public float RealNeckLength => neckLength * defaultNeckLength; 

        public void SetSpineLength(float length)
        {
            spineLength = length;

            if (jobReference != null && jobReference.IsValid) jobReference.SetSpineLength(RealSpineLength);
            ApplySpineComponentEdits();
        }
        public void SetPreviousSpineLength(float length)
        {
            if (jobReference != null && jobReference.IsValid) jobReference.SetPrevousSpineLength(length * defaultSpineLength);
        }

        public void SetNeckLength(float length)
        {
            neckLength = length;

            if (jobReference != null && jobReference.IsValid) jobReference.SetNeckLength(RealNeckLength);
            ApplyNeckComponentEdits();
        }
        public void SetPreviousNeckLength(float length)
        {
            if (jobReference != null && jobReference.IsValid) jobReference.SetPreviousNeckLength(length * defaultNeckLength);
        }

        [NonSerialized]
        protected float defaultSpineLength;
        public float DefaultSpineLength => defaultSpineLength;
        [NonSerialized]
        protected float defaultNeckLength;
        public float DefaultNeckLength => defaultNeckLength;


        [SerializeField, Range(0, 1), AnimatableProperty(true, 0f)]
        protected float heightPreserveHandPositionLeft = 0f;
        [SerializeField, Range(0, 1), AnimatableProperty(true, 0f)]
        protected float heightPreserveHandPositionRight = 0f;

        public void SetPreserveHandPositionLeftWeight(float weight)
        {
            heightPreserveHandPositionLeft = weight;

            if (jobReference != null && jobReference.IsValid)
            {
                var weights = jobReference.GetLimbPreservationWeights();
                weights.x = weight;
                jobReference.SetLimbPreservationWeights(weights);
            }
        }
        public void SetPreserveHandPositionRightWeight(float weight)
        {
            heightPreserveHandPositionRight = weight;

            if (jobReference != null && jobReference.IsValid)
            {
                var weights = jobReference.GetLimbPreservationWeights();
                weights.y = weight;
                jobReference.SetLimbPreservationWeights(weights);
            }
        }

        [SerializeField, Range(0.5f, 10f)]
        protected float shoulderWidth = 1f;
        [SerializeField, Range(0.5f, 2f)]
        protected float armLength = 1f;
        [SerializeField, Range(0.5f, 2f)]
        protected float forearmLength = 1f;

        public float RealShoulderWidth => shoulderWidth * defaultShoulderWidth;
        public float RealArmLength => armLength * defaultArmLength;
        public float RealForearmLength => forearmLength * defaultForearmLength;

        public void SetShoulderWidth(float width)
        {
            shoulderWidth = width;

            if (jobReference != null && jobReference.IsValid)
            {
                float widthA = RealShoulderWidth;
                var widths = jobReference.GetWidths();
                widths.x = widthA;
                widths.y = widthA;
                jobReference.SetWidths(widths + new float4(widthContributions.xy, 0f, 0f));
            }

            ApplyShoulderComponentEdits();
        }
        public void SetPreviousShoulderWidth(float width)
        {
            if (jobReference != null && jobReference.IsValid)
            {
                float widthA = width * defaultShoulderWidth;
                float4 widths = jobReference.GetPreviousWidths();
                widths.x = widthA;
                widths.y = widthA;
                jobReference.SetPreviousWidths(widths);
            }
        }

        public void SetArmLength(float length)
        {
            armLength = length;

            if (jobReference != null && jobReference.IsValid)
            {
                float lengthA = RealArmLength;
                float4 lengths = jobReference.GetArmLengths(); 
                lengths.x = lengthA;
                lengths.z = lengthA;
                jobReference.SetArmLengths(lengths);
            }

            ApplyArmComponentEdits();
        }
        public void SetPreviousArmLength(float length)
        {
            if (jobReference != null && jobReference.IsValid)
            {
                float lengthA = length * defaultArmLength;
                float4 lengths = jobReference.GetPreviousArmLengths();
                lengths.x = lengthA;
                lengths.z = lengthA;
                jobReference.SetPrevousArmLengths(lengths);
            }
        }

        public void SetForearmLength(float length)
        {
            forearmLength = length;

            if (jobReference != null && jobReference.IsValid)
            {
                float lengthA = RealForearmLength;
                float4 lengths = jobReference.GetArmLengths();
                lengths.y = lengthA;
                lengths.w = lengthA;
                jobReference.SetArmLengths(lengths);
            }

            ApplyForearmComponentEdits();
        }
        public void SetPreviousForearmLength(float length)
        {
            if (jobReference != null && jobReference.IsValid)
            {
                float lengthA = length * defaultForearmLength;
                float4 lengths = jobReference.GetPreviousArmLengths();
                lengths.y = lengthA;
                lengths.w = lengthA;
                jobReference.SetPrevousArmLengths(lengths);
            }
        }

        [NonSerialized]
        protected float defaultShoulderWidth;
        public float DefaultShoulderWidth => defaultShoulderWidth;
        [NonSerialized]
        protected float defaultArmLength;
        public float DefaultArmLength => defaultArmLength;
        [NonSerialized]
        protected float defaultForearmLength;
        public float DefaultForearmLength => defaultForearmLength;

        [SerializeField, Range(0, 1), AnimatableProperty(true, 0f)]
        protected float heightPreserveFootPositionLeft = 0f;
        [SerializeField, Range(0, 1), AnimatableProperty(true, 0f)]
        protected float heightPreserveFootPositionRight = 0f;

        [SerializeField, Range(0, 1), AnimatableProperty(true, 1f)]
        protected float heightPreserveKneeBendLeft = 1f;
        [SerializeField, Range(0, 1), AnimatableProperty(true, 1f)]
        protected float heightPreserveKneeBendRight = 1f; 

        public void SetPreserveFootPositionLeftWeight(float weight)
        {
            heightPreserveFootPositionLeft = weight;

            if (jobReference != null && jobReference.IsValid)
            {
                var weights = jobReference.GetLimbPreservationWeights();
                weights.z = weight;
                jobReference.SetLimbPreservationWeights(weights);
            }
        }
        public void SetPreserveFootPositionRightWeight(float weight)
        {
            heightPreserveFootPositionRight = weight;

            if (jobReference != null && jobReference.IsValid)
            {
                var weights = jobReference.GetLimbPreservationWeights();
                weights.w = weight;
                jobReference.SetLimbPreservationWeights(weights);
            }
        }

        public void SetPreserveKneeBendLeftWeight(float weight)
        {
            heightPreserveKneeBendLeft = weight;

            if (jobReference != null && jobReference.IsValid)
            {
                var weights = jobReference.GetKneePreservationWeights();
                weights.x = weight;
                jobReference.SetKneePreservationWeights(weights);
            }
        }
        public void SetPreserveKneeBendRightWeight(float weight)
        {
            heightPreserveKneeBendRight = weight;

            if (jobReference != null && jobReference.IsValid)
            {
                var weights = jobReference.GetKneePreservationWeights();
                weights.y = weight;
                jobReference.SetKneePreservationWeights(weights);
            }
        }

        public bool flipShoulderAxisForMirror = true;
        public bool flipArmWidthAxisForMirror = false; 
        public bool flipArmAxisForMirror = true;

        [SerializeField, Range(0.5f, 10f)] 
        protected float hipWidth = 1;
        [SerializeField, Range(0.5f, 2f)]
        protected float legLength = 1;
        [SerializeField, Range(0.5f, 2f)]
        protected float calfLength = 1;

        public float RealHipWidth => hipWidth * defaultHipWidth;
        public float RealLegLength => legLength * defaultLegLength;
        public float RealCalfLength => calfLength * defaultCalfLength;

        public void SetHipWidth(float width)
        {
            hipWidth = width;

            if (jobReference != null && jobReference.IsValid)
            {
                float widthA = RealHipWidth;
                var widths = jobReference.GetWidths();
                widths.z = widthA;
                widths.w = widthA;
                jobReference.SetWidths(widths + new float4(0f, 0f, widthContributions.zw));
            }

            ApplyHipComponentEdits();
        }
        public void SetPreviousHipWidth(float width)
        {
            if (jobReference != null && jobReference.IsValid)
            {
                float widthA = width * defaultHipWidth;
                float4 widths = jobReference.GetPreviousWidths();
                widths.z = widthA;
                widths.w = widthA;
                jobReference.SetPreviousWidths(widths);
            }
        }

        public void SetLegLength(float length)
        {
            legLength = length;

            if (jobReference != null && jobReference.IsValid)
            {
                float lengthA = RealLegLength;
                float4 lengths = jobReference.GetLegLengths();
                lengths.x = lengthA;
                lengths.z = lengthA;
                jobReference.SetLegLengths(lengths);
            }

            ApplyThighComponentEdits();
        }
        public void SetPreviousLegLength(float length)
        {
            if (jobReference != null && jobReference.IsValid)
            {
                float lengthA = length * defaultLegLength;
                float4 lengths = jobReference.GetPreviousLegLengths();
                lengths.x = lengthA;
                lengths.z = lengthA;
                jobReference.SetPreviousLegLengths(lengths);
            }
        }

        public void SetCalfLength(float length)
        {
            calfLength = length;

            if (jobReference != null && jobReference.IsValid)
            {
                float lengthA = RealCalfLength;
                float4 lengths = jobReference.GetLegLengths();
                lengths.y = lengthA;
                lengths.w = lengthA;
                jobReference.SetLegLengths(lengths);
            }

            ApplyCalfComponentEdits();
        }
        public void SetPreviousCalfLength(float length)
        {
            if (jobReference != null && jobReference.IsValid)
            {
                float lengthA = length * defaultCalfLength;
                float4 lengths = jobReference.GetPreviousLegLengths(); 
                lengths.y = lengthA;
                lengths.w = lengthA;
                jobReference.SetPreviousLegLengths(lengths); 
            }
        }


        [SerializeField, Range(0, 1)]
        protected float shoulderArmWidthSplit = 0.5f;
        [SerializeField, Range(0, 1)]
        protected float hipThighWidthSplit = 0.5f;

        public void SetShoulderArmWidthSplit(float weight)
        {
            shoulderArmWidthSplit = weight;

            if (jobReference != null && jobReference.IsValid)
            {
                var weights = jobReference.GetSocketLimbSplits();
                weights.x = weight;
                jobReference.SetSocketLimbSplits(weights);
            }
        }
        public void SetHipThighWidthSplit(float weight)
        {
            hipThighWidthSplit = weight;

            if (jobReference != null && jobReference.IsValid)
            {
                var weights = jobReference.GetSocketLimbSplits();
                weights.y = weight;
                jobReference.SetSocketLimbSplits(weights);
            }
        }


        [NonSerialized]
        protected float defaultHipWidth;
        public float DefaultHipWidth => defaultHipWidth; 
        [NonSerialized]
        protected float defaultLegLength;
        public float DefaultLegLength => defaultLegLength;
        [NonSerialized]
        protected float defaultCalfLength;
        public float DefaultCalfLength => defaultCalfLength;

        public bool flipHipAxisForMirror = true;
        public bool flipLegWidthAxisForMirror = false;
        public bool flipLegAxisForMirror = false;

        public Vector3 extensionAxisSpine = Vector3.up;
        public Vector3 bendAxisSpine = Vector3.right;

        public Vector3 extensionAxisNeck = Vector3.up;
        public Vector3 bendAxisNeck = Vector3.right;

        public Vector3 extensionAxisShoulders = Vector3.right;
        public Vector3 extensionWidthAxisArms = Vector3.right;
        public Vector3 extensionAxisArms = Vector3.up;
        public Vector3 bendAxisArms = Vector3.forward;

        public Vector3 extensionAxisHips = Vector3.right;
        public Vector3 extensionWidthAxisLegs = Vector3.right;
        public Vector3 extensionAxisLegs = Vector3.up;
        public Vector3 bendAxisLegs = Vector3.right; 

        protected BipedalCharacterHeightJobs.IndexReference jobReference;

        public static float CalculateLimbLength(Transform transformA, Transform transformB, Vector3 extensionAxis)
        {
            //Quaternion toAxis = Quaternion.FromToRotation(transformA.TransformDirection(extensionAxis), Vector3.up);
            //return (toAxis * (transformB.position - transformA.position)).y; 
            var worldAxis = transformA.TransformDirection(extensionAxis);
            return math.abs(math.dot(transformB.position - transformA.position, worldAxis)); 
        }

        [SerializeField]
        protected CustomAnimator animator;
        public CustomAnimator Animator
        {
            set => animator = value;
            get
            {
                if (animator == null) animator = gameObject.GetComponent<CustomAnimator>();
                return animator;
            }
        }

        [SerializeField]
        protected CustomizableCharacterMesh characterMesh;
        public CustomizableCharacterMesh CharacterMeshV1
        {
            set => characterMesh = value;
            get
            {
                if (characterMesh == null) characterMesh = gameObject.GetComponent<CustomizableCharacterMesh>();
                return characterMesh;
            }
        }

        [SerializeField]
        protected CustomizableCharacterMeshV2 characterMeshV2;
        public CustomizableCharacterMeshV2 CharacterMeshV2
        {
            set => characterMeshV2 = value;
            get
            {
                if (characterMeshV2 == null) characterMeshV2 = gameObject.GetComponent<CustomizableCharacterMeshV2>();
                return characterMeshV2;
            }
        }

        public ICustomizableCharacter CharacterMesh => CharacterMeshV2 != null ? characterMeshV2 : CharacterMeshV1;

        [Serializable]
        public class WidthContributingMuscle
        {
            public string name;
            public float2 shoulderWidthContribution;
            public float2 hipWidthContribution;

            public float minMass = 0f;
            public float maxMass = 1f;
            public bool clampMassContribution;

            public float minFlex;
            public float maxFlex;
            public bool clampFlexContribution;

            [NonSerialized]
            public int cachedIndex;
        }
        [Serializable]
        public class WidthContributionGroup
        {
            public string name;

            public WidthContributingMuscle[] muscles;

            public void Initialize(ICustomizableCharacter characterMesh)
            {
                if (characterMesh == null) return;

                if (muscles != null)
                {
                    for(int a = 0; a < muscles.Length; a++)
                    {
                        var muscle = muscles[a];
                        if (muscle == null) continue;

                        muscle.cachedIndex = characterMesh.IndexOfMuscleGroup(muscle.name) + 1;
#if UNITY_EDITOR
                        if (muscle.cachedIndex <= 0)
                        {
                            Debug.LogWarning($"Muscle '{muscle.name}' not found in character mesh. Please check the name or add it to the mesh." +
                                             $" This will cause issues with width contributions.", characterMesh.GameObject);
                        }
#endif
                    }
                }
            }

            [NonSerialized]
            public float4 currentContribution; 
            public float4 GetWidthContributions(ICustomizableCharacter characterMesh)
            {
                if (characterMesh == null) return 0f;

                currentContribution = 0f;
                if (muscles != null)
                {
                    for (int a = 0; a < muscles.Length; a++)
                    {
                        var muscle = muscles[a];
                        if (muscle == null || muscle.cachedIndex <= 0) continue;

                        float2 minMass = muscle.minMass;
                        float2 maxMass = muscle.maxMass;
                        float2 minFlex = muscle.minFlex;
                        float2 maxFlex = muscle.maxFlex;

                        int muscleIndex = muscle.cachedIndex - 1;
                        var muscleData = characterMesh.GetMuscleDataUnsafe(muscleIndex);

                        bool skip = true;
                        float2 massContr = new float2(1f, 1f);
                        if (muscle.minMass != muscle.maxMass)
                        {
                            massContr = math.remap(minMass, maxMass, 0f, 1f, new float2(muscleData.valuesLeft.mass, muscleData.valuesRight.mass));
                            if (muscle.clampMassContribution) massContr = math.saturate(massContr);
                            skip = false;
                        }

                        float2 flexContr = new float2(1f, 1f);
                        if (muscle.minFlex != muscle.maxFlex)
                        {
                            flexContr = math.remap(minFlex, maxFlex, 0f, 1f, new float2(muscleData.valuesLeft.flex, muscleData.valuesRight.flex));
                            if (muscle.clampFlexContribution) flexContr = math.saturate(flexContr);
                            skip = false;
                        }

                        if (skip) continue;

                        float2 contr = flexContr * massContr;   
                        float2 shoulderWidthContr = math.lerp(muscle.shoulderWidthContribution.x, muscle.shoulderWidthContribution.y, contr);  
                        float2 hipWidthContr = math.lerp(muscle.hipWidthContribution.x, muscle.hipWidthContribution.y, contr);

                        currentContribution.x += shoulderWidthContr.x;
                        currentContribution.y += shoulderWidthContr.y;
                        currentContribution.z += hipWidthContr.x;
                        currentContribution.w += hipWidthContr.y;
                    }
                }

                return currentContribution; 
            }
        }

        protected Dictionary<int, List<WidthContributionGroup>> widthContributionGroupsByMuscleIndices;
        [SerializeField]
        protected WidthContributionGroup[] widthContributionGroups;
        public void InitializeWidthContributionGroups()
        {
            if (CharacterMesh != null) 
            {
                widthContributionGroupsByMuscleIndices = new Dictionary<int, List<WidthContributionGroup>>();
                if (widthContributionGroups != null)
                {
                    foreach (var group in widthContributionGroups)
                    {
                        group.Initialize(CharacterMesh);

                        if (group.muscles != null)
                        {
                            foreach (var muscle in group.muscles)
                            {
                                if (muscle.cachedIndex > 0)
                                {
                                    int muscleIndex = muscle.cachedIndex - 1;
                                    if (!widthContributionGroupsByMuscleIndices.TryGetValue(muscleIndex, out var groupList))
                                    {
                                        groupList = new List<WidthContributionGroup>();
                                        widthContributionGroupsByMuscleIndices[muscleIndex] = groupList;
                                    }

                                    groupList.Add(group);
                                }
                            }
                        }
                    }
                }

                CharacterMesh.AddListener(ICustomizableCharacter.ListenableEvent.OnMuscleDataChanged, OnMuscleDataChange); 
            }
        }
        private float4 widthContributions;
        public float4 RecalculateWidthContributions()
        {
            if (widthContributionGroups == null) return 0f;

            var characterMesh = CharacterMesh;

            widthContributions = 0f;
            foreach (var group in widthContributionGroups)
            {
                widthContributions += group.GetWidthContributions(characterMesh);
            }

            return widthContributions;
        } 
        protected void OnMuscleDataChange(int muscleIndex)
        {
            if (widthContributionGroupsByMuscleIndices.TryGetValue(muscleIndex, out var groups))
            {
                var characterMesh = CharacterMesh;

                var prevContributions = widthContributions;
                foreach (var group in groups)
                {
                    widthContributions -= group.currentContribution;
                    widthContributions += group.GetWidthContributions(characterMesh); 
                }

                var flags = prevContributions != widthContributions;
                if (math.any(flags.xy))
                {
                    SetShoulderWidth(shoulderWidth);
                }
                if (math.any(flags.zw))
                {
                    SetHipWidth(hipWidth);
                }
            }
        }


        [Serializable]
        public enum SizeSource
        {
            UpperArmleft,
            UpperArmRight,
            ForearmLeft,
            ForearmRight,
            ThighLeft,
            ThighRight,
            CalfLeft,
            CalfRight,
            Spine,
            Neck,
            LeftShoulder,
            RightShoulder,
            LeftHip,
            RightHip
        }

        public interface IComponentEdit
        {
            public SizeSource SourceOfSize { get; }

            public void Initialize();
            public void Resize(float sizeChange);
        }

        [Serializable]
        public class CapsuleColliderEdit : IComponentEdit
        {
            public CapsuleCollider collider;
            public SizeSource sizeSource;
            public SizeSource SourceOfSize => sizeSource;

            [NonSerialized]
            private Vector3 originalCenter;
            [NonSerialized]
            private float originalHeight;

            public void Initialize()
            {
                if (collider == null) return;

                originalCenter = collider.center;
                originalHeight = collider.height;
            }

            public void Resize(float sizeChange)
            {
                if (collider == null) return;

                collider.height = originalHeight + sizeChange;
                
                if (collider.direction == 0)
                {
                    collider.center = originalCenter + new Vector3(sizeChange * 0.5f, 0f, 0f);
                } 
                else if (collider.direction == 1)
                {
                    collider.center = originalCenter + new Vector3(0f, sizeChange * 0.5f, 0f);
                } 
                else
                {
                    collider.center = originalCenter + new Vector3(0f, 0f, sizeChange * 0.5f);
                }
            }
        }
        [SerializeField]
        private List<CapsuleColliderEdit> capsuleColliderEdits;

        [Serializable]
        public class BoxColliderEdit : IComponentEdit
        {
            public BoxCollider collider;
            public SizeSource sizeSource;
            public SizeSource SourceOfSize => sizeSource;

            public XYZChannel resizeAxis;
            [NonSerialized]
            private Vector3 originalCenter;
            [NonSerialized]
            private Vector3 originalSize;

            public void Initialize()
            {
                if (collider == null) return;

                originalCenter = collider.center;
                originalSize = collider.size;
            }

            public void Resize(float sizeChange)
            {
                if (collider == null) return;

                float halfSizeChange = sizeChange * 0.5f;
                if (resizeAxis.HasFlag(XYZChannel.X))
                {
                    collider.center = originalCenter + new Vector3(halfSizeChange, 0f, 0f);
                    collider.size = originalSize + new Vector3(sizeChange, 0f, 0f);
                }
                
                if (resizeAxis.HasFlag(XYZChannel.Y))
                {
                    collider.center = originalCenter + new Vector3(0f, halfSizeChange, 0f);
                    collider.size = originalSize + new Vector3(0f, sizeChange, 0f);
                }

                if (resizeAxis.HasFlag(XYZChannel.Z))
                {
                    collider.center = originalCenter + new Vector3(0f, 0f, halfSizeChange);
                    collider.size = originalSize + new Vector3(0f, 0f, sizeChange);
                }
            }
        }
        [SerializeField]
        private List<BoxColliderEdit> boxColliderEdits;

        [Serializable]
        public class ConfigurableJointEdit : IComponentEdit
        {
            public ConfigurableJoint joint;
            public SizeSource sizeSource;
            public SizeSource SourceOfSize => sizeSource;

            public Vector3 extensionAxis;
            [NonSerialized]
            private Vector3 originalConnectedAnchor;

            public void Initialize()
            {
                if (joint == null) return;

                originalConnectedAnchor = joint.connectedBody == null ? Vector3.zero : joint.connectedAnchor;
            }

            public void Resize(float sizeChange)
            {
                if (joint == null) return;

                joint.connectedAnchor = originalConnectedAnchor + extensionAxis * sizeChange;
            }
        }
        [SerializeField]
        private List<BoxColliderEdit> configurableJointEdits;

        [Serializable]
        public class CharacterJointEdit : IComponentEdit
        {
            public CharacterJoint joint;
            public SizeSource sizeSource;
            public SizeSource SourceOfSize => sizeSource;

            public Vector3 extensionAxis;
            [NonSerialized]
            private Vector3 originalConnectedAnchor;

            public void Initialize()
            {
                if (joint == null) return;

                originalConnectedAnchor = joint.connectedBody == null ? Vector3.zero : joint.connectedAnchor;
            }

            public void Resize(float sizeChange)
            {
                if (joint == null) return;

                joint.connectedAnchor = originalConnectedAnchor + extensionAxis * sizeChange;
            }
        }
        [SerializeField]
        private List<BoxColliderEdit> characterJointEdits;

        [Serializable]
        public class TransformEdit : IComponentEdit
        {
            public Transform transform;
            public SizeSource sizeSource;
            public SizeSource SourceOfSize => sizeSource;
            public bool applyToChildren;

            public Vector3 extensionAxis;
            [NonSerialized]
            private Vector3 originalLocalPosition;

            public void Initialize()
            {
                if (transform == null) return;

                originalLocalPosition = transform.localPosition;
            }

            public void Resize(float sizeChange)
            {
                if (transform == null) return;

                transform.localPosition = originalLocalPosition + extensionAxis * sizeChange;
            }
        }
        [SerializeField]
        private List<TransformEdit> transformEdits;

        protected void InitializeComponentEdits()
        {
            if (transformEdits != null)
            {
                foreach (var transform in transformEdits)
                {
                    transform.Initialize();
                    RegisterComponentEdit(transform);
                }
            }
            if (capsuleColliderEdits != null)
            {
                foreach (var collider in capsuleColliderEdits) 
                { 
                    collider.Initialize(); 
                    RegisterComponentEdit(collider);
                }
            }
            if (boxColliderEdits != null)
            {
                foreach (var collider in boxColliderEdits) 
                { 
                    collider.Initialize();
                    RegisterComponentEdit(collider);
                }
            }
            if (configurableJointEdits != null)
            {
                foreach (var joint in configurableJointEdits) 
                { 
                    joint.Initialize();
                    RegisterComponentEdit(joint);
                }
            }
            if (characterJointEdits != null)
            {
                foreach (var joint in characterJointEdits) 
                {
                    joint.Initialize();
                    RegisterComponentEdit(joint);
                }
            }
        }

        public void AddComponentEdit(IComponentEdit edit, bool initialize = true)
        {
            if (edit == null) return;

            if (initialize) edit.Initialize();
            RegisterComponentEdit(edit);
        }

        private List<IComponentEdit> componentEdits_upperArmLeft;
        private List<IComponentEdit> componentEdits_upperArmRight;
        private List<IComponentEdit> componentEdits_forearmLeft;
        private List<IComponentEdit> componentEdits_forearmRight;
        private List<IComponentEdit> componentEdits_thighLeft;
        private List<IComponentEdit> componentEdits_thighRight;
        private List<IComponentEdit> componentEdits_calfLeft;
        private List<IComponentEdit> componentEdits_calfRight;
        private List<IComponentEdit> componentEdits_spine;
        private List<IComponentEdit> componentEdits_neck;
        private List<IComponentEdit> componentEdits_leftShoulder;
        private List<IComponentEdit> componentEdits_rightShoulder;
        private List<IComponentEdit> componentEdits_leftHip;
        private List<IComponentEdit> componentEdits_rightHip;

        private void RegisterComponentEdit(IComponentEdit edit)
        {
            if (edit is TransformEdit transformEdit)
            {
                if (transformEdit.applyToChildren)
                {
                    if (transformEdit.transform != null)
                    {
                        for (int a = 0; a < transformEdit.transform.childCount; a++)
                        {
                            var child = transformEdit.transform.GetChild(a);
                            var childEdit = new TransformEdit
                            {
                                transform = child,
                                sizeSource = edit.SourceOfSize,
                                extensionAxis = transformEdit.extensionAxis,
                                applyToChildren = false
                            };

                            childEdit.Initialize();
                            RegisterComponentEdit(childEdit); 
                        }
                    }

                    return;
                }
            }

            switch (edit.SourceOfSize)
            {
                case SizeSource.UpperArmleft:
                    if (componentEdits_upperArmLeft == null) componentEdits_upperArmLeft = new List<IComponentEdit>();
                    componentEdits_upperArmLeft.Add(edit);
                    break;
                case SizeSource.UpperArmRight:
                    if (componentEdits_upperArmRight == null) componentEdits_upperArmRight = new List<IComponentEdit>();
                    componentEdits_upperArmRight.Add(edit);
                    break;
                case SizeSource.ForearmLeft:
                    if (componentEdits_forearmLeft == null) componentEdits_forearmLeft = new List<IComponentEdit>();
                    componentEdits_forearmLeft.Add(edit);
                    break;
                case SizeSource.ForearmRight:
                    if (componentEdits_forearmRight == null) componentEdits_forearmRight = new List<IComponentEdit>();
                    componentEdits_forearmRight.Add(edit);
                    break;
                case SizeSource.ThighLeft:
                    if (componentEdits_thighLeft == null) componentEdits_thighLeft = new List<IComponentEdit>();
                    componentEdits_thighLeft.Add(edit);
                    break;
                case SizeSource.ThighRight:
                    if (componentEdits_thighRight == null) componentEdits_thighRight = new List<IComponentEdit>();
                    componentEdits_thighRight.Add(edit);
                    break;
                case SizeSource.CalfLeft:
                    if (componentEdits_calfLeft == null) componentEdits_calfLeft = new List<IComponentEdit>();
                    componentEdits_calfLeft.Add(edit);
                    break;
                case SizeSource.CalfRight:
                    if (componentEdits_calfRight == null) componentEdits_calfRight = new List<IComponentEdit>();
                    componentEdits_calfRight.Add(edit);
                    break;
                case SizeSource.Spine:
                    if (componentEdits_spine == null) componentEdits_spine = new List<IComponentEdit>();
                    componentEdits_spine.Add(edit);
                    break;
                case SizeSource.Neck:
                    if (componentEdits_neck == null) componentEdits_neck = new List<IComponentEdit>();
                    componentEdits_neck.Add(edit);
                    break;
                case SizeSource.LeftShoulder:
                    if (componentEdits_leftShoulder == null) componentEdits_leftShoulder = new List<IComponentEdit>();
                    componentEdits_leftShoulder.Add(edit);
                    break;
                case SizeSource.RightShoulder:
                    if (componentEdits_rightShoulder == null) componentEdits_rightShoulder = new List<IComponentEdit>();
                    componentEdits_rightShoulder.Add(edit);
                    break;
                case SizeSource.LeftHip:
                    if (componentEdits_leftHip == null) componentEdits_leftHip = new List<IComponentEdit>();
                    componentEdits_leftHip.Add(edit);
                    break;
                case SizeSource.RightHip:
                    if (componentEdits_rightHip == null) componentEdits_rightHip = new List<IComponentEdit>();
                    componentEdits_rightHip.Add(edit);
                    break;
            }
        }

        public void ApplyShoulderComponentEdits()
        {
            float shoulderWidthOffset = RealShoulderWidth - defaultShoulderWidth;
            if (componentEdits_leftShoulder != null)
            {
                foreach (var edit in componentEdits_leftShoulder) edit.Resize(shoulderWidthOffset);
            }
            if (componentEdits_rightShoulder != null)
            {
                foreach (var edit in componentEdits_rightShoulder) edit.Resize(shoulderWidthOffset);
            }
        }
        public void ApplyArmComponentEdits()
        {
            float armLengthOffset = RealArmLength - defaultArmLength;
            if (componentEdits_upperArmLeft != null)
            {
                foreach (var edit in componentEdits_upperArmLeft) edit.Resize(armLengthOffset);
            }
            if (componentEdits_upperArmRight != null)
            {
                foreach (var edit in componentEdits_upperArmRight) edit.Resize(armLengthOffset);
            }
        }
        public void ApplyForearmComponentEdits()
        {
            float forearmLengthOffset = RealForearmLength - defaultForearmLength;
            if (componentEdits_forearmLeft != null)
            {
                foreach (var edit in componentEdits_forearmLeft) edit.Resize(forearmLengthOffset);
            }
            if (componentEdits_forearmRight != null)
            {
                foreach (var edit in componentEdits_forearmRight) edit.Resize(forearmLengthOffset);
            }
        }
        public void ApplyHipComponentEdits()
        {
            float hipWidthOffset = RealHipWidth - defaultHipWidth;
            if (componentEdits_leftHip != null)
            {
                foreach (var edit in componentEdits_leftHip) edit.Resize(hipWidthOffset);
            }
            if (componentEdits_rightHip != null)
            {
                foreach (var edit in componentEdits_rightHip) edit.Resize(hipWidthOffset);
            }
        }
        public void ApplyThighComponentEdits()
        {
            float thighLengthOffset = RealLegLength - defaultLegLength;
            if (componentEdits_thighLeft != null)
            {
                foreach (var edit in componentEdits_thighLeft) edit.Resize(thighLengthOffset);
            }
            if (componentEdits_thighRight != null)
            {
                foreach (var edit in componentEdits_thighRight) edit.Resize(thighLengthOffset);
            }
        }
        public void ApplyCalfComponentEdits()
        {
            float calfLengthOffset = RealCalfLength - defaultCalfLength;
            if (componentEdits_calfLeft != null)
            {
                foreach (var edit in componentEdits_calfLeft) edit.Resize(calfLengthOffset);
            }
            if (componentEdits_calfRight != null)
            {
                foreach (var edit in componentEdits_calfRight) edit.Resize(calfLengthOffset);
            }
        }
        public void ApplySpineComponentEdits()
        {
            float spineLengthOffset = RealSpineLength - defaultSpineLength;
            if (componentEdits_spine != null)
            {
                foreach (var edit in componentEdits_spine) edit.Resize(spineLengthOffset);
            }
        }
        public void ApplyNeckComponentEdits()
        {
            float neckLengthOffset = RealNeckLength - defaultNeckLength;
            if (componentEdits_neck != null)
            {
                foreach (var edit in componentEdits_neck) edit.Resize(neckLengthOffset);
            }
        }
        public void ApllyAllComponentEdits()
        {
            ApplyShoulderComponentEdits();

            ApplyArmComponentEdits();

            ApplyForearmComponentEdits();

            ApplyHipComponentEdits();

            ApplyThighComponentEdits();

            ApplyCalfComponentEdits();

            ApplySpineComponentEdits();

            ApplyNeckComponentEdits();
        }

        protected virtual void Awake()
        {
            spineLength = armLength = forearmLength = legLength = calfLength = 1; 

            defaultSpineLength = CalculateLimbLength(spine1, spine2, extensionAxisSpine);
            defaultNeckLength = CalculateLimbLength(neck.parent, neck, extensionAxisNeck);

            defaultShoulderWidth = CalculateLimbLength(shoulderLeft.parent, shoulderLeft, extensionAxisShoulders);
            defaultArmLength = CalculateLimbLength(armLeft, forearmLeft, extensionAxisArms);
            defaultForearmLength = CalculateLimbLength(forearmLeft, wristLeft, extensionAxisArms);

            defaultHipWidth = CalculateLimbLength(hipLeft.parent, hipLeft, extensionAxisHips);
            defaultLegLength = CalculateLimbLength(legLeft, calfLeft, extensionAxisLegs);
            defaultCalfLength = CalculateLimbLength(calfLeft, footLeft, extensionAxisLegs);

            if (Animator != null)
            {
                animator.AddListener(CustomAnimator.BehaviourEvent.OnEnable, Enable);
                animator.AddListener(CustomAnimator.BehaviourEvent.OnDisable, Disable); 
            }

            InitializeComponentEdits();
            InitializeWidthContributionGroups();
        }

        [NonSerialized]
        protected bool initialized = false;
        protected virtual void Start()
        {
            initialized = true;

            Enable();
        }

        protected void Enable()
        {
            if (initialized && isActiveAndEnabled && Animator != null && animator.isActiveAndEnabled)
            {
                if (jobReference != null && jobReference.IsValid)
                {
                    jobReference.Dispose();
                }

                jobReference = BipedalCharacterHeightJobs.Register(extensionAxisSpine, bendAxisSpine,
                    spine1, spine2, spine3, defaultSpineLength,
                    extensionAxisShoulders, extensionWidthAxisArms, extensionAxisArms, bendAxisArms, extensionAxisHips, extensionWidthAxisLegs, extensionAxisLegs, bendAxisLegs, flipShoulderAxisForMirror, flipArmWidthAxisForMirror, flipArmAxisForMirror, flipHipAxisForMirror, flipLegWidthAxisForMirror, flipLegAxisForMirror,
                    shoulderArmWidthSplit,
                    shoulderLeft, defaultShoulderWidth, armLeft.parent, armLeft, forearmLeft, wristLeft, defaultArmLength, defaultForearmLength,
                    shoulderRight, defaultShoulderWidth, armRight.parent, armRight, forearmRight, wristRight, defaultArmLength, defaultForearmLength,
                    hipThighWidthSplit,
                    hipLeft, defaultHipWidth, legLeft.parent, legLeft, calfLeft, footLeft, defaultLegLength, defaultCalfLength,
                    hipRight, defaultHipWidth, legRight.parent, legRight, calfRight, footRight, defaultLegLength, defaultCalfLength,
                    pelvis,
                    extensionAxisNeck, bendAxisNeck, neck, defaultNeckLength); 

                SetSpineLength(spineLength);
                SetNeckLength(neckLength);
                SetShoulderWidth(shoulderWidth);
                SetArmLength(armLength);
                SetForearmLength(forearmLength);
                SetHipWidth(hipWidth);
                SetLegLength(legLength);
                SetCalfLength(calfLength);

                SetPreserveHandPositionLeftWeight(heightPreserveHandPositionLeft);
                SetPreserveHandPositionRightWeight(heightPreserveHandPositionRight);
                SetPreserveFootPositionLeftWeight(heightPreserveFootPositionLeft);
                SetPreserveFootPositionRightWeight(heightPreserveFootPositionRight);
                SetPreserveKneeBendLeftWeight(heightPreserveKneeBendLeft);
                SetPreserveKneeBendRightWeight(heightPreserveKneeBendRight);

                SetShoulderArmWidthSplit(shoulderArmWidthSplit);
                SetHipThighWidthSplit(hipThighWidthSplit);
            }
        }
        protected void Disable()
        {
            if (jobReference != null && jobReference.IsValid)
            {
                jobReference.Dispose();
                jobReference = default;
            }
        }

        protected void OnEnable()
        {
            Enable();
        }
        protected void OnDisable()
        {
            Disable();
        }

        protected void OnDestroy()
        {
            Disable();
            if (animator != null)
            {
                animator.RemoveListener(CustomAnimator.BehaviourEvent.OnEnable, Enable);
                animator.RemoveListener(CustomAnimator.BehaviourEvent.OnDisable, Disable); 
            }

            if (CharacterMesh != null)
            {
                CharacterMesh.RemoveListener(ICustomizableCharacter.ListenableEvent.OnMuscleDataChanged, OnMuscleDataChange); 
            }
        }
    }
}

#endif