#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Unity.Mathematics;

namespace Swole.API.Unity
{
    public class BipedalCharacterHeight : MonoBehaviour
    {

        // Concept: If a foot is grounded, move the pelvis to achieve the original knee bend angle and foot position. Otherwise, change the bend of the knee to get the same foot position.

#if UNITY_EDITOR

        [NonSerialized]
        protected float prevSpineLength = 1;

        [NonSerialized]
        protected float prevArmLength = 1;
        [NonSerialized]
        protected float prevForearmLength = 1;

        [NonSerialized]
        protected float prevLegLength = 1;
        [NonSerialized]
        protected float prevCalfLength = 1;


        [NonSerialized]
        protected float prevHeightPreserveHandPositionLeft = 1;
        [NonSerialized]
        protected float prevHeightPreserveHandPositionRight = 1;

        [NonSerialized]
        protected float prevHeightPreserveFootPositionLeft = 1;
        [NonSerialized]
        protected float prevHeightPreserveFootPositionRight = 1;

        [NonSerialized]
        protected float prevHeightPreserveKneeBendLeft = 1;
        [NonSerialized]
        protected float prevHeightPreserveKneeBendRight = 1;


        protected void OnValidate()
        {
            if (spineLength != prevSpineLength)
            {
                SetSpineLength(spineLength);
                prevSpineLength = spineLength;
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
        }

#endif

        public Transform pelvis;

        public Transform spine1;
        public Transform spine2;
        public Transform spine3;

        public Transform armLeft;
        public Transform forearmLeft;
        public Transform wristLeft;

        public Transform armRight;
        public Transform forearmRight;
        public Transform wristRight;

        public Transform legLeft;
        public Transform calfLeft;
        public Transform footLeft;

        public Transform legRight;
        public Transform calfRight;
        public Transform footRight;

        [SerializeField, Range(0.5f, 2f)]
        protected float spineLength = 1;

        public float RealSpineLength => spineLength * defaultSpineLength;

        public void SetSpineLength(float length)
        {
            spineLength = length;

            if (jobReference != null && jobReference.IsValid) jobReference.SetSpineLength(RealSpineLength);
        }
        public void SetPreviousSpineLength(float length)
        {
            if (jobReference != null && jobReference.IsValid) jobReference.SetPrevousSpineLength(length * defaultSpineLength);
        }

        [NonSerialized]
        protected float defaultSpineLength;


        [SerializeField, Range(0, 1), AnimatableProperty]
        protected float heightPreserveHandPositionLeft = 1;
        [SerializeField, Range(0, 1), AnimatableProperty]
        protected float heightPreserveHandPositionRight = 1;

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

        [SerializeField, Range(0.5f, 2f)]
        protected float armLength = 1;
        [SerializeField, Range(0.5f, 2f)]
        protected float forearmLength = 1;

        public float RealArmLength => armLength * defaultArmLength;
        public float RealForearmLength => forearmLength * defaultForearmLength;

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
        protected float defaultArmLength;
        [NonSerialized]
        protected float defaultForearmLength;

        [SerializeField, Range(0, 1), AnimatableProperty]
        protected float heightPreserveFootPositionLeft = 1;
        [SerializeField, Range(0, 1), AnimatableProperty]
        protected float heightPreserveFootPositionRight = 1;

        [SerializeField, Range(0, 1), AnimatableProperty]
        protected float heightPreserveKneeBendLeft = 1;
        [SerializeField, Range(0, 1), AnimatableProperty]
        protected float heightPreserveKneeBendRight = 1; 

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

        public bool flipArmAxisForMirror = true;

        [SerializeField, Range(0.5f, 2f)]
        protected float legLength = 1;
        [SerializeField, Range(0.5f, 2f)]
        protected float calfLength = 1;

        public float RealLegLength => legLength * defaultLegLength;
        public float RealCalfLength => calfLength * defaultCalfLength;

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

        [NonSerialized]
        protected float defaultLegLength;
        [NonSerialized]
        protected float defaultCalfLength;

        public bool flipLegAxisForMirror = false;

        public Vector3 extensionAxisSpine = Vector3.up;
        public Vector3 bendAxisSpine = Vector3.right;

        public Vector3 extensionAxisArms = Vector3.up;
        public Vector3 bendAxisArms = Vector3.forward;

        public Vector3 extensionAxisLegs = Vector3.up;
        public Vector3 bendAxisLegs = Vector3.right; 

        protected BipedalCharacterHeightJobs.IndexReference jobReference;

        public static float CalculateLimbLength(Transform transformA, Transform transformB, Vector3 extensionAxis)
        {
            Quaternion toAxis = Quaternion.FromToRotation(transformA.TransformDirection(extensionAxis), Vector3.up);
            return (toAxis * (transformB.position - transformA.position)).y; 
        }

        protected virtual void Awake()
        {
            spineLength = armLength = forearmLength = legLength = calfLength = 1;

            defaultSpineLength = CalculateLimbLength(spine1, spine2, extensionAxisSpine);

            defaultArmLength = CalculateLimbLength(armLeft, forearmLeft, extensionAxisArms);
            defaultForearmLength = CalculateLimbLength(forearmLeft, wristLeft, extensionAxisArms);
             
            defaultLegLength = CalculateLimbLength(legLeft, calfLeft, extensionAxisLegs);
            defaultCalfLength = CalculateLimbLength(calfLeft, footLeft, extensionAxisLegs);
        }

        protected virtual void Start()
        {
            jobReference = BipedalCharacterHeightJobs.Register(extensionAxisSpine, bendAxisSpine,
                spine1, spine2, spine3, defaultSpineLength,
                extensionAxisArms, bendAxisArms, extensionAxisLegs, bendAxisLegs, flipArmAxisForMirror, flipLegAxisForMirror,
                armLeft.parent, armLeft, forearmLeft, wristLeft, defaultArmLength, defaultForearmLength,
                armRight.parent, armRight, forearmRight, wristRight, defaultArmLength, defaultForearmLength,
                legLeft.parent, legLeft, calfLeft, footLeft, defaultLegLength, defaultCalfLength,
                legRight.parent, legRight, calfRight, footRight, defaultLegLength, defaultCalfLength,
                pelvis);

            SetPreserveHandPositionLeftWeight(heightPreserveHandPositionLeft);
            SetPreserveHandPositionRightWeight(heightPreserveHandPositionRight); 

            SetPreserveFootPositionLeftWeight(heightPreserveFootPositionLeft);
            SetPreserveFootPositionRightWeight(heightPreserveFootPositionRight);
            SetPreserveKneeBendLeftWeight(heightPreserveKneeBendLeft);
            SetPreserveKneeBendRightWeight(heightPreserveKneeBendRight);
        }

    }
}

#endif