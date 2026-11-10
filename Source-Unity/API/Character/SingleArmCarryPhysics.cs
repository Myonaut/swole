#if UNITY_2017_1_OR_NEWER

using System;

using UnityEngine;

using Swole.API.Unity.Animation;

namespace Swole.API.Unity
{
    [Serializable]
    public class SingleArmCarryPhysics
    {
        [Serializable]
        public struct CustomCollisionSphere
        {
            [Tooltip("The bone or transform this collision sphere sticks to (e.g. Hip, Thigh).")]
            public Transform referenceBone;
            [Tooltip("Local offset relative to the reference bone.")]
            public Vector3 localOffset;
            [Tooltip("Radius of the collision sphere.")]
            public float radius;
        }

        [Header("IK Skeleton Transforms")]
        [Tooltip("The root pelvis/hips bone used to establish the base of the spine column vector.")]
        public Transform pelvisBone;
        [Tooltip("The chest or spine bone used to determine the upper end of the spine column vector.")]
        public Transform chestBone;
        [Tooltip("The collarbone or clavicle transform bone of the target arm.")]
        public Transform shoulderBone;
        [Tooltip("The upper arm humerus transform bone of the target arm.")]
        public Transform upperArmBone;
        [Tooltip("The lower arm forearm transform bone of the target arm.")]
        public Transform forearmBone;
        [Tooltip("The wrist or hand transform bone of the target arm.")]
        public Transform handBone;

        [Header("Output Targets")]
        [Tooltip("Calculated target position output applied to the IK controller target transform.")]
        public Vector3 calculatedTargetPos;
        [Tooltip("Calculated elbow hint position output applied to the IK controller bend goal transform.")]
        public Vector3 calculatedBendGoalPos;
        [Tooltip("Calculated target rotation output applied to the IK controller target transform.")]
        public Quaternion calculatedTargetRot;
        [Tooltip("Hand rotation derived only from the character frame and the solver's own inertia (no animated hand/forearm input). Used to drive carried objects.")]
        public Quaternion calculatedStableTargetRot = Quaternion.identity;

        [Header("Sizing")]
        [Tooltip("Automatically calculates length by reading the distance between upperArm, forearm, and hand bones.")]
        public bool autoCalculateArmLength = true;
        [SerializeField, Tooltip("Manual override arm length used if auto-calculation is disabled.")]
        private float customArmLength = 1.0f;
        public void SetCustomArmLength(float length)
        {
            if (length <= 0f)
            {
                RecalculateArmLength();
                return;
            } 

            _totalArmLength = customArmLength = length;
            autoCalculateArmLength = false; 
        }
        public void RecalculateArmLength()
        {
            _totalArmLength = Vector3.Distance(upperArmBone.position, forearmBone.position) + Vector3.Distance(forearmBone.position, handBone.position);
        }
        [Range(0.5f, 1.1f), Tooltip("Controls how straight the arm can get. Set slightly below 1.0 (e.g., 0.95) to force a natural, aesthetic elbow bend under load.")]
        public float armLengthMultiplier = 0.96f; 

        [Header("Physics Settings")]
        [Range(0, 1), Tooltip("Overall master weight blending value for the dynamic carry system.")]
        public float weight = 1f;
        [Tooltip("Mass of the object in kilograms. Higher mass causes the arm to drop lower, swing wider, and lag behind quick movements.")]
        public float objectMass = 5.0f;
        [Tooltip("Controls how heavily movement accelerations delay the target item position.")]
        public float inertiaStrength = 0.5f;
        [Tooltip("Persistent layout array assigned for procedural tracking dampening loops.")]
        public float[] dampingArray = new float[] { 5f };
        [Tooltip("Spring tension dampening used by the physical tracking solver.")]
        public float damping = 5f;
        [Tooltip("Downward force strength acting on the mass of the object.")]
        public float gravityStrength = 9.81f;

        [Header("Game Feel Tuning")]
        [Tooltip("Blends the core physics tracking space. 0.0 = Pure World Space (Heavy drag lag). 1.0 = Pure Local Space (Zero sprint lag). 0.4 = Ideal hybrid look for heavy item weight.")]
        [Range(0f, 1f)] public float localSpaceBlend = 0.4f;
        [Tooltip("Extra collision thickness buffer around the hand. Set larger for wide items like boxes or dumbbells so the item doesn't clip the thigh.")]
        public float itemThicknessRadius = 0.05f;

        [Header("Character Space Local Bounds (Relative to Dynamic Slatted Pocket)")]
        [Tooltip("Toggles mirror inversion layouts across the X axis for accurate side clamping.")]
        public bool isLeftArm = false;
        [Tooltip("Maximum distance the hand can flare outward away from the body side.")]
        public float maxOutwardPocket = 0.45f;
        [Tooltip("Maximum distance the hand can compress inward towards the ribs.")]
        public float maxInwardPocket = 0.05f;
        [Tooltip("Maximum forward boundary limit extension for the hand pocket.")]
        public float maxForwardPocket = 0.5f;
        [Tooltip("Maximum backward boundary limit extension for the hand pocket.")]
        public float maxBackwardPocket = 0.4f;
        [Tooltip("Maximum vertical height extension allowed for the hand pocket.")]
        public float maxUpwardPocket = 0.05f;

        [Header("Bend Goal Space Directional Bounds (Relative to Dynamic Slatted Torso Axes)")]
        [Tooltip("Maximum horizontal side flare allowed for the elbow bend hint target.")]
        public float maxBendOutward = 0.5f;
        [Tooltip("Maximum horizontal inward compression allowed for the elbow bend hint target.")]
        public float maxBendInward = 0.05f;
        [Tooltip("Maximum forward distance allowed for the elbow bend hint target.")]
        public float maxBendForward = 0.4f;
        [Tooltip("Maximum backward distance allowed for the elbow bend hint target.")]
        public float maxBendBackward = 0.2f;

        [Header("Rig Alignment Axes (Crucial for Rotations)")]
        [Tooltip("The local forward axis vector direction pointing out of the hand bone.")]
        public Vector3 handLocalForwardAxis = Vector3.forward;
        [Tooltip("The local vertical up axis vector direction pointing out of the hand bone.")]
        public Vector3 handLocalUpAxis = Vector3.up;
        [Tooltip("The local forward axis direction used by the shoulder bone to project look trajectories.")]
        public Vector3 shoulderLocalForwardAxis = Vector3.forward;

        [Header("Wrist Alignment - Forearm Track Axes")]
        [Tooltip("The hand local vector axis that points straight up the forearm toward the elbow.")]
        public Vector3 handLocalArmExtensionAxis = Vector3.up;
        [Tooltip("The hand local vector axis that points outward horizontally to the side.")]
        public Vector3 handLocalLateralAxis = Vector3.left;

        [Header("Wrist Alignment - Dedicated Gravity Track Axes")]
        [Tooltip("The hand local vector axis that maps directly to the horizontal horizon forward.")]
        public Vector3 handLocalGravityForwardAxis = Vector3.forward;
        [Tooltip("The hand local vector axis that maps directly to the vertical gravity up line.")]
        public Vector3 handLocalGravityUpAxis = Vector3.up;

        [Header("Wrist Alignment - Dedicated Twist Execution Axis")]
        [Tooltip("Exposed local vector axis of the hand bone that the object twists around along the forearm direction line.")]
        public Vector3 handLocalTwistAxis = Vector3.up;

        [Header("Dynamic Forward Blending Settings")]
        [Range(0f, 1f), Tooltip("How much the pocket orientation locks to the character's root forward instead of the chest. 0 = Pure Chest Twist, 1 = Pure Character Root Forward. High values protect leans.")]
        public float pocketCharacterRootLeanBlend = 0.5f;

        [Header("Wrist Gravity Tracking Settings")]
        [Tooltip("Maximum allowed drift angle the wrist can deflect away from the level gravity baseline.")]
        public float maxWristDeviationAngle = 30f;
        [Tooltip("The elastic force drawing the loose wrist swing back toward its flat horizontal baseline.")]
        public float wristTwistStiffness = 6f;
        [Tooltip("Spring dampening that controls how long the loose wrist pendulum oscillates before settling.")]
        public float wristTwistDamping = 1.2f;
        [Tooltip("Scale modifier mapping lateral accelerations and yaw turn drops into kinetic wrist torque forces.")]
        public float wristTwistInertiaFactor = 8.0f;
        [Range(0f, 1f), Tooltip("Randomized force scaling factor to prevent mechanical predictability in the twist lines.")]
        public float wristTwistRandomization = 0.1f;
        [Tooltip("Maximum anatomical safety limit for wrist twisting to protect bone joint integrity.")]
        public float maxWristTwistAngle = 20f;
        [Tooltip("Energy conservation coefficient applied when hitting maximum angles. Controls boundary bounce intensity.")]
        [Range(0f, 0.8f)] public float wristLimitElasticity = 0.45f;

        [Header("Shoulder Dip Alignment Axes")]
        [Tooltip("The localized roll axis vector used to displace the collarbone under weight loads.")]
        public Vector3 shoulderDipAxis = Vector3.right;
        [Tooltip("The localized swing axis vector used to displace the collarbone under movements.")]
        public Vector3 shoulderForwardSwingAxis = Vector3.up;
        [Tooltip("Sensitivity multiplier for weight forces converting into visible collarbone dips.")]
        public float shoulderDipSensitivity = 1.0f;
        [Tooltip("Maximum threshold clamping angle for procedural collarbone dip displacement loops.")]
        public float maxShoulderDipAngle = 15f;

        [Header("Rotation Physics Settings")]
        [Tooltip("Scale modifier mapping kinetic velocities into procedural item pitch and roll tilts.")]
        public float rotationInertiaFactor = 3.5f;
        [Tooltip("Maximum allowed tilting threshold angle for physics item sway.")]
        public float maxRotationTiltAngle = 35f;

        [Header("Custom Sphere Collisions")]
        [Tooltip("Array containing obstacle bones and local radius profiles used for physical hand avoidance.")]
        public CustomCollisionSphere[] obstacleSpheres;

        private Vector3 _dynamicHandPosWorld;
        private Vector3 _handVelocityWorld;
        private Vector3 _dynamicHandPosLocal;
        private Vector3 _handVelocityLocal;
        private Vector3 _lastShoulderPos;
        private float _totalArmLength;
        private float _shoulderDipScale;
        private bool _wasActiveLastFrame;
        private Quaternion _chestBindRotationInRootSpace;
        private bool _hasInitializedBindPose;

        private float _proceduralTwistAngle;
        private float _proceduralTwistVelocity;
        private float _lastCharacterYaw;

        public void Init()
        {
            if (autoCalculateArmLength)
            {
                RecalculateArmLength();
            }
            else
            {
                _totalArmLength = customArmLength;
            }
            _dynamicHandPosWorld = handBone.position;
            _handVelocityWorld = Vector3.zero;
            _dynamicHandPosLocal = Vector3.zero;
            _handVelocityLocal = Vector3.zero;
            _lastShoulderPos = upperArmBone.position;
            calculatedTargetRot = handBone.rotation;
            _shoulderDipScale = 1f;
            _wasActiveLastFrame = false;
            _proceduralTwistAngle = 0f; _proceduralTwistVelocity = 0f;
            _lastCharacterYaw = 0f;
        }

        public void UpdatePhysics(float deltaTime, CustomIKManager.IKController ikController, Transform characterTransform, float blend = 1f)
        {
            float weight = this.weight * blend;

            if (weight <= 0f || deltaTime <= 0f || !shoulderBone || !upperArmBone || !handBone || !chestBone || !pelvisBone || characterTransform == null)
            {
                _wasActiveLastFrame = false;
                return;
            }

            if (!_hasInitializedBindPose)
            {
                _chestBindRotationInRootSpace = Quaternion.Inverse(characterTransform.rotation) * chestBone.rotation;
                _hasInitializedBindPose = true;
                _lastCharacterYaw = characterTransform.eulerAngles.y;
            }

            if (!_wasActiveLastFrame)
            {
                _dynamicHandPosWorld = handBone.position;
                _handVelocityWorld = Vector3.zero;
                _dynamicHandPosLocal = characterTransform.InverseTransformPoint(handBone.position);
                _handVelocityLocal = Vector3.zero;
                _lastShoulderPos = upperArmBone.position;
                _shoulderDipScale = 1f;

                _proceduralTwistAngle = 0f;
                _proceduralTwistVelocity = 0f;
                _lastCharacterYaw = characterTransform.eulerAngles.y;
                _wasActiveLastFrame = true;
            }

            float mass = Mathf.Max(0.1f, objectMass);
            float effectiveArmLength = _totalArmLength * Mathf.Max(0.1f, armLengthMultiplier);
            Vector3 currentArmSocketPos = upperArmBone.position;

            if (_lastShoulderPos == Vector3.zero) _lastShoulderPos = currentArmSocketPos;

            // Calculate armpit acceleration for inertia drag forces
            Vector3 socketMovement = currentArmSocketPos - _lastShoulderPos;
            Vector3 accelerationForce = -socketMovement / deltaTime;
            _lastShoulderPos = currentArmSocketPos;

            Vector3 gravityForceWorld = Vector3.down * gravityStrength * mass;
            Vector3 totalExternalForcesWorld = gravityForceWorld + (accelerationForce * inertiaStrength * mass);

            Vector3 localAcceleration = characterTransform.InverseTransformDirection(accelerationForce);
            Vector3 localGravity = characterTransform.InverseTransformDirection(Vector3.down * gravityStrength);
            Vector3 totalLocalForces = (localGravity * mass) + (localAcceleration * inertiaStrength * mass);

            if (_dynamicHandPosLocal == Vector3.zero)
            {
                _dynamicHandPosLocal = characterTransform.InverseTransformPoint(handBone.position);
            }

            Vector3 unDippedSocketWorldPos = upperArmBone.position;
            Quaternion unDippedChestWorldRot = chestBone.rotation;
            Quaternion baseAnimatedShoulderRot = shoulderBone.localRotation;

            // Execute Collarbone Dip Pass 
            if (weight > 0.01f)
            {
                Vector3 localWeightForce = shoulderBone.InverseTransformDirection(totalExternalForcesWorld);
                float weightInfluence = Mathf.Clamp(0.3f / mass, 0.05f, 0.6f);

                float targetDipScale = 1f;
                Vector3 mixedVelocity = Vector3.Lerp(characterTransform.TransformDirection(_handVelocityLocal), _handVelocityWorld, localSpaceBlend);
                if (mixedVelocity.y > 0.01f)
                {
                    targetDipScale = Mathf.Clamp01(1f - (mixedVelocity.y / 3f));
                }

                _shoulderDipScale = Mathf.MoveTowards(_shoulderDipScale, targetDipScale, 4f * deltaTime);

                Quaternion shoulderDipOffset = Quaternion.Euler(
                    localWeightForce.z * weightInfluence * _shoulderDipScale,
                    0,
                    localWeightForce.x * weightInfluence * _shoulderDipScale
                );

                shoulderBone.localRotation *= shoulderDipOffset;
                currentArmSocketPos = upperArmBone.position;
            }

            Vector3 localSocketPos = characterTransform.InverseTransformPoint(currentArmSocketPos);
            float restLengthModifier = Mathf.Clamp(0.9f + (mass * 0.01f), 0.9f, 0.98f);

            // Track 1 Solver: Pure World Space
            Vector3 restPositionWorld = currentArmSocketPos + (Vector3.down * effectiveArmLength * restLengthModifier);
            Vector3 springForceWorld = (restPositionWorld - _dynamicHandPosWorld) * damping * 10f;
            Vector3 netAccelerationWorld = (totalExternalForcesWorld + springForceWorld) / mass;

            _handVelocityWorld += netAccelerationWorld * deltaTime;
            _handVelocityWorld = Vector3.ClampMagnitude(_handVelocityWorld, Mathf.Clamp(25f / mass, 5f, 20f));
            _dynamicHandPosWorld += _handVelocityWorld * deltaTime;

            // Track 2 Solver: Pure Local Space
            Vector3 localRestPosition = localSocketPos + (Vector3.down * effectiveArmLength * restLengthModifier);
            Vector3 localSpringForce = (localRestPosition - _dynamicHandPosLocal) * damping * 10f;
            Vector3 localNetAcceleration = (totalLocalForces + localSpringForce) / mass;

            _handVelocityLocal += localNetAcceleration * deltaTime;
            _handVelocityLocal = Vector3.ClampMagnitude(_handVelocityLocal, Mathf.Clamp(25f / mass, 5f, 20f));
            _dynamicHandPosLocal += _handVelocityLocal * deltaTime;

            // Step C: Combined Solver & Iterative Constraint Pass
            for (int iteration = 0; iteration < 2; iteration++)
            {
                if (obstacleSpheres != null)
                {
                    for (int i = 0; i < obstacleSpheres.Length; i++)
                    {
                        CustomCollisionSphere sphere = obstacleSpheres[i];
                        if (sphere.referenceBone == null || sphere.radius <= 0f) continue;

                        Vector3 sphereWorldPos = sphere.referenceBone.TransformPoint(sphere.localOffset);
                        Vector3 sphereLocalPos = characterTransform.InverseTransformPoint(sphereWorldPos);
                        float combinedHandBuffer = sphere.radius + itemThicknessRadius + 0.001f;

                        Vector3 obstacleToHandW = _dynamicHandPosWorld - sphereWorldPos;
                        if (obstacleToHandW.magnitude < combinedHandBuffer)
                        {
                            Vector3 normalW = obstacleToHandW.magnitude < 0.001f ? sphere.referenceBone.right : obstacleToHandW.normalized;
                            _dynamicHandPosWorld = sphereWorldPos + (normalW * (sphere.radius + itemThicknessRadius));
                            _handVelocityWorld = Vector3.ProjectOnPlane(_handVelocityWorld, normalW) * 0.5f;
                        }

                        Vector3 obstacleToHandL = _dynamicHandPosLocal - sphereLocalPos;
                        if (obstacleToHandL.magnitude < combinedHandBuffer)
                        {
                            Vector3 normalL = obstacleToHandL.magnitude < 0.001f ? characterTransform.InverseTransformDirection(sphere.referenceBone.right) : obstacleToHandL.normalized;
                            _dynamicHandPosLocal = sphereLocalPos + (normalL * (sphere.radius + itemThicknessRadius));
                            _handVelocityLocal = Vector3.ProjectOnPlane(_handVelocityLocal, normalL) * 0.5f;
                        }
                    }
                }

                Vector3 socketToHandW = _dynamicHandPosWorld - currentArmSocketPos;
                if (socketToHandW.magnitude > effectiveArmLength)
                {
                    _dynamicHandPosWorld = currentArmSocketPos + (socketToHandW.normalized * effectiveArmLength);
                    _handVelocityWorld = Vector3.ProjectOnPlane(_handVelocityWorld, socketToHandW.normalized);
                }

                Vector3 socketToHandL = _dynamicHandPosLocal - localSocketPos;
                if (socketToHandL.magnitude > effectiveArmLength)
                {
                    _dynamicHandPosLocal = localSocketPos + (socketToHandL.normalized * effectiveArmLength);
                    _handVelocityLocal = Vector3.ProjectOnPlane(_handVelocityLocal, socketToHandL.normalized);
                }
            }

            Vector3 handPosition = handBone.position;
            Quaternion handRotation = handBone.rotation;
            bool controllerWasActive = false;

            if (ikController.IsActive)
            {
                float targetBlend = Mathf.Clamp01(ikController.GetPositionWeight() * ikController.GetWeight());
                float rotBlend = Mathf.Clamp01(ikController.GetRotationWeight() * ikController.GetWeight());

                handPosition = Vector3.LerpUnclamped(handPosition, ikController.Target.position, targetBlend);
                handRotation = Quaternion.SlerpUnclamped(handRotation, ikController.Target.rotation, rotBlend);
                controllerWasActive = true;
            }

            // Step D: Pre-Interpolated Gravity-Slatted Bounding Pass
            Vector3 track2WorldPos = characterTransform.TransformPoint(_dynamicHandPosLocal);
            Vector3 proceduralTargetPhysics = Vector3.Lerp(_dynamicHandPosWorld, track2WorldPos, localSpaceBlend);
            Vector3 blendedTargetWorld = Vector3.Lerp(handPosition, proceduralTargetPhysics, weight);

            Vector3 targetInRootSpace = characterTransform.InverseTransformPoint(blendedTargetWorld);
            Vector3 activeSocketInRootSpace = characterTransform.InverseTransformPoint(currentArmSocketPos);

            // Construct stable look directions from spine vector vs shoulder layout
            Vector3 spineLineDirection = (chestBone.position - pelvisBone.position).normalized;
            float spineVerticalDot = Mathf.Abs(Vector3.Dot(spineLineDirection, Vector3.up));

            // Dynamically shift vertical tracking to character up on steep leans to block gimbal flips
            Vector3 blendedUpReference = Vector3.Lerp(characterTransform.up, Vector3.up, spineVerticalDot).normalized;
            Vector3 shoulderGirdleWidthDirection = (upperArmBone.position - chestBone.position).normalized;
            Vector3 skeletalDerivedForward = Vector3.Cross(shoulderGirdleWidthDirection, blendedUpReference).normalized;

            if (isLeftArm) skeletalDerivedForward = -skeletalDerivedForward;

            Vector3 flatSkeletalForward = skeletalDerivedForward;
            flatSkeletalForward.y = 0f;

            if (flatSkeletalForward.sqrMagnitude < 0.001f) flatSkeletalForward = characterTransform.forward;
            else flatSkeletalForward.Normalize();

            Vector3 flatCharacterForward = characterTransform.forward;
            Vector3 finalBlendedForwardHeading = Vector3.Lerp(flatSkeletalForward, flatCharacterForward, pocketCharacterRootLeanBlend).normalized;

            Quaternion slattedChestWorldRot = Quaternion.LookRotation(finalBlendedForwardHeading, Vector3.up);
            Quaternion relativeSlattedChestDelta = Quaternion.Inverse(characterTransform.rotation) * slattedChestWorldRot;

            Vector3 handOffsetFromActiveSocket = targetInRootSpace - activeSocketInRootSpace;
            Vector3 alignedHandOffset = Quaternion.Inverse(relativeSlattedChestDelta) * handOffsetFromActiveSocket;

            float sideSign = isLeftArm ? -1f : 1f;
            float clampedX = Mathf.Clamp(alignedHandOffset.x * sideSign, -maxInwardPocket, maxOutwardPocket) * sideSign;
            float clampedY = Mathf.Clamp(alignedHandOffset.y, -effectiveArmLength, maxUpwardPocket);
            float clampedZ = Mathf.Clamp(alignedHandOffset.z, -maxBackwardPocket, maxForwardPocket);

            Vector3 constrainedHandOffset = relativeSlattedChestDelta * new Vector3(clampedX, clampedY, clampedZ);
            targetInRootSpace = activeSocketInRootSpace + constrainedHandOffset;
            Vector3 proceduralTarget = characterTransform.TransformPoint(targetInRootSpace);

            Vector3 finalSocketToHand = proceduralTarget - currentArmSocketPos;
            if (finalSocketToHand.magnitude > effectiveArmLength)
            {
                proceduralTarget = currentArmSocketPos + (finalSocketToHand.normalized * effectiveArmLength);
                finalSocketToHand = proceduralTarget - currentArmSocketPos;
            }
            // Step E: Geometric Base Wrist Realignment Pass
            if (weight > 0.01f && forearmBone != null)
            {
                Vector3 worldForearmDirection = (proceduralTarget - forearmBone.position).normalized;
                Vector3 characterForward = characterTransform.forward;
                Quaternion targetForearmRotation = Quaternion.LookRotation(worldForearmDirection, characterForward);
                Quaternion forearmRigAdjustment = Quaternion.Inverse(Quaternion.LookRotation(handLocalArmExtensionAxis.normalized, handLocalLateralAxis.normalized));
                Quaternion finalForearmTrackRotation = targetForearmRotation * forearmRigAdjustment;

                Vector3 gravityUp = Vector3.up;
                Vector3 trueHorizontalForward = characterTransform.forward;
                trueHorizontalForward.y = 0f;

                if (trueHorizontalForward.sqrMagnitude < 0.001f) trueHorizontalForward = characterTransform.forward;
                else trueHorizontalForward = trueHorizontalForward.normalized;

                Quaternion gravityLockedBaseRotation = Quaternion.LookRotation(trueHorizontalForward, gravityUp);
                Quaternion gravityRigAdjustment = Quaternion.Inverse(Quaternion.LookRotation(handLocalGravityForwardAxis.normalized, handLocalGravityUpAxis.normalized));
                Quaternion finalGravityRotation = gravityLockedBaseRotation * gravityRigAdjustment;

                float currentCharacterYaw = characterTransform.eulerAngles.y;
                float yawDelta = Mathf.DeltaAngle(_lastCharacterYaw, currentCharacterYaw);
                _lastCharacterYaw = currentCharacterYaw;

                // CACHED: Consolidated velocity lerps shared below
                Vector3 combinedWorldVelocity = Vector3.Lerp(_handVelocityWorld, characterTransform.TransformDirection(_handVelocityLocal), localSpaceBlend);
                float lateralVelocity = Vector3.Dot(combinedWorldVelocity, characterTransform.right);

                // Integrate frictionless string momentum drift forces
                float directDriftForce = (lateralVelocity * wristTwistInertiaFactor * 2.0f) - (yawDelta * wristTwistInertiaFactor * deltaTime * 65f);
                directDriftForce *= (1f - (wristTwistRandomization * UnityEngine.Random.value));

                float baselineReturnSpringForce = (0f - _proceduralTwistAngle) * wristTwistStiffness * (1f - (wristTwistRandomization * UnityEngine.Random.value));
                float dampingForce = _proceduralTwistVelocity * wristTwistDamping;

                _proceduralTwistVelocity += (directDriftForce + baselineReturnSpringForce - dampingForce) * deltaTime;
                _proceduralTwistAngle += _proceduralTwistVelocity * deltaTime;

                // Elastic coefficient limit reflection checks
                if (_proceduralTwistAngle > maxWristTwistAngle)
                {
                    _proceduralTwistAngle = maxWristTwistAngle;
                    if (_proceduralTwistVelocity > 0f)
                    {
                        _proceduralTwistVelocity = -_proceduralTwistVelocity * wristLimitElasticity * (1f - (0.7f * wristTwistRandomization * UnityEngine.Random.value));
                    }
                }
                else if (_proceduralTwistAngle < -maxWristTwistAngle)
                {
                    _proceduralTwistAngle = -maxWristTwistAngle;
                    if (_proceduralTwistVelocity < 0f)
                    {
                        _proceduralTwistVelocity = -_proceduralTwistVelocity * wristLimitElasticity * (1f - (0.7f * wristTwistRandomization * UnityEngine.Random.value));
                    }
                }

                float deviationAngle = Quaternion.Angle(finalGravityRotation, finalForearmTrackRotation);
                Quaternion resolvedCarryRotation = finalGravityRotation;

                if (deviationAngle > maxWristDeviationAngle)
                {
                    float overflowRatio = (deviationAngle - maxWristDeviationAngle) / deviationAngle;
                    resolvedCarryRotation = Quaternion.Slerp(finalGravityRotation, finalForearmTrackRotation, overflowRatio);
                }

                handRotation = Quaternion.Slerp(handRotation, resolvedCarryRotation, weight);
            }

            // OPTIMIZED: Reuses combinedWorldVelocity allocated up in Section E safely
            Vector3 currentWorldVelocity = Vector3.Lerp(_handVelocityWorld, characterTransform.TransformDirection(_handVelocityLocal), localSpaceBlend);
            Vector3 worldHandForward = handRotation * handLocalForwardAxis.normalized;
            Vector3 worldHandUp = handRotation * handLocalUpAxis.normalized;
            Vector3 worldHandRight = Vector3.Cross(worldHandUp, worldHandForward).normalized;

            float forwardVelocity = Vector3.Dot(currentWorldVelocity, worldHandForward);
            float rightVelocity = Vector3.Dot(currentWorldVelocity, worldHandRight);

            float pitchAngle = -forwardVelocity * rotationInertiaFactor * (1f + (mass * 0.03f));
            float rollAngle = rightVelocity * rotationInertiaFactor * (1f + (mass * 0.03f));

            pitchAngle = Mathf.Clamp(pitchAngle, -maxRotationTiltAngle, maxRotationTiltAngle);
            rollAngle = Mathf.Clamp(rollAngle, -maxRotationTiltAngle, maxRotationTiltAngle);

            Quaternion localPitch = Quaternion.AngleAxis(pitchAngle, handLocalUpAxis.normalized == Vector3.right ? handLocalForwardAxis : Vector3.right);
            Quaternion localRoll = Quaternion.AngleAxis(rollAngle, Vector3.forward);
            Quaternion proceduralRotation = handRotation * (localPitch * localRoll);

            if (weight > 0.01f)
            {
                proceduralRotation *= Quaternion.AngleAxis(_proceduralTwistAngle * weight, handLocalTwistAxis.normalized);
            }

            // Step F: Skeletal-Plane Procedural Forearm Orbital Blend Solver
            Vector3 worldArmExtensionAxis = finalSocketToHand.normalized;
            Vector3 stableForwardDirAnim = slattedChestWorldRot * shoulderLocalForwardAxis.normalized;
            Vector3 stableForwardDirPhysics = shoulderBone.TransformDirection(shoulderLocalForwardAxis.normalized);

            Vector3 projAnim = Vector3.ProjectOnPlane(stableForwardDirAnim, worldArmExtensionAxis).normalized;
            Vector3 projPhysics = Vector3.ProjectOnPlane(stableForwardDirPhysics, worldArmExtensionAxis).normalized;

            if (projAnim.sqrMagnitude < 0.001f) projAnim = stableForwardDirAnim;
            if (projPhysics.sqrMagnitude < 0.001f) projPhysics = stableForwardDirPhysics;

            Quaternion orbitRotationDelta = Quaternion.FromToRotation(projAnim, projPhysics);
            Vector3 blendedWorldOrbitDir = Quaternion.Slerp(Quaternion.identity, orbitRotationDelta, weight) * projAnim;

            // Perform spatial distance calculations to enforce the cyan bounding envelope
            Vector3 rawWorldBendGoal = upperArmBone.position + (finalSocketToHand * 0.5f) + (blendedWorldOrbitDir * (effectiveArmLength * 0.25f));
            Vector3 bendInRootSpace = characterTransform.InverseTransformPoint(rawWorldBendGoal);
            Vector3 bendOffsetFromActiveSocket = bendInRootSpace - activeSocketInRootSpace;
            Vector3 alignedBendOffset = Quaternion.Inverse(relativeSlattedChestDelta) * bendOffsetFromActiveSocket;

            float bendClampedX = Mathf.Clamp(alignedBendOffset.x * sideSign, -maxBendInward, maxBendOutward) * sideSign;
            float bendClampedY = Mathf.Clamp(alignedBendOffset.y, -effectiveArmLength, effectiveArmLength);
            float bendClampedZ = Mathf.Clamp(alignedBendOffset.z, -maxBendBackward, maxBendForward);

            Vector3 constrainedLocalBendOffset = relativeSlattedChestDelta * new Vector3(bendClampedX, bendClampedY, bendClampedZ);
            Vector3 proceduralBendGoal = characterTransform.TransformPoint(activeSocketInRootSpace + constrainedLocalBendOffset);

            ikController.SetActive(true);
            ikController.SetWeight(controllerWasActive ? Mathf.LerpUnclamped(ikController.GetWeight(), 1f, weight) : weight);
            ikController.SetPositionWeight(controllerWasActive ? Mathf.LerpUnclamped(ikController.GetPositionWeight(), 1f, weight) : 1f);
            ikController.SetRotationWeight(controllerWasActive ? Mathf.LerpUnclamped(ikController.GetRotationWeight(), 1f, weight) : 1f);

            ikController.Target.position = calculatedTargetPos = proceduralTarget;
            ikController.BendGoal.position = calculatedBendGoalPos = proceduralBendGoal;
            ikController.Target.rotation = calculatedTargetRot = proceduralRotation;

            Vector3 stableForward = characterTransform.forward;
            stableForward.y = 0f;
            if (stableForward.sqrMagnitude < 0.001f) stableForward = Vector3.forward;
            Quaternion stableBase = Quaternion.LookRotation(stableForward.normalized, Vector3.up) * Quaternion.Inverse(Quaternion.LookRotation(handLocalGravityForwardAxis.normalized, handLocalGravityUpAxis.normalized));
            Vector3 stableHandForward = stableBase * handLocalForwardAxis.normalized;
            Vector3 stableHandUp = stableBase * handLocalUpAxis.normalized;
            Vector3 stableHandRight = Vector3.Cross(stableHandUp, stableHandForward).normalized;
            float stablePitch = Mathf.Clamp(-Vector3.Dot(currentWorldVelocity, stableHandForward) * rotationInertiaFactor * (1f + (mass * 0.03f)), -maxRotationTiltAngle, maxRotationTiltAngle);
            float stableRoll = Mathf.Clamp(Vector3.Dot(currentWorldVelocity, stableHandRight) * rotationInertiaFactor * (1f + (mass * 0.03f)), -maxRotationTiltAngle, maxRotationTiltAngle);
            calculatedStableTargetRot = stableBase
                * Quaternion.AngleAxis(stablePitch, handLocalUpAxis.normalized == Vector3.right ? handLocalForwardAxis : Vector3.right)
                * Quaternion.AngleAxis(stableRoll, Vector3.forward)
                * Quaternion.AngleAxis(_proceduralTwistAngle, handLocalTwistAxis.normalized);
        }


        public void DrawGizmos(Transform characterTransform)
        {
            if (characterTransform == null || upperArmBone == null || chestBone == null || pelvisBone == null) return;

            // Safety fallback initialization to handle editor view changes when play mode is stopped
            if (!_hasInitializedBindPose)
            {
                _chestBindRotationInRootSpace = Quaternion.Inverse(characterTransform.rotation) * chestBone.rotation;
                _hasInitializedBindPose = true;
            }

            // Custom Collision Sphere loops with thickness multipliers
            if (obstacleSpheres != null)
            {
                for (int i = 0; i < obstacleSpheres.Length; i++)
                {
                    if (obstacleSpheres[i].referenceBone == null) continue;
                    Vector3 sphereWorldPos = obstacleSpheres[i].referenceBone.TransformPoint(obstacleSpheres[i].localOffset);
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(sphereWorldPos, obstacleSpheres[i].radius);
                    if (itemThicknessRadius > 0.001f)
                    {
                        Gizmos.color = new Color(1f, 0f, 0f, 0.15f);
                        Gizmos.DrawWireSphere(sphereWorldPos, obstacleSpheres[i].radius + itemThicknessRadius);
                    }
                }
            }

            if (shoulderBone)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(calculatedTargetPos, 0.04f);
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(calculatedBendGoalPos, 0.04f);

                Vector3 worldFwd = calculatedTargetRot * handLocalForwardAxis.normalized;
                Vector3 worldUp = calculatedTargetRot * handLocalUpAxis.normalized;
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(calculatedTargetPos, worldFwd * 0.15f);
                Gizmos.color = Color.green;
                Gizmos.DrawRay(calculatedTargetPos, worldUp * 0.15f);
            }

            Matrix4x4 originalMatrix = Gizmos.matrix;

            // 1. Reconstruct the dynamic world-space Spine Column tracking vector direction
            Vector3 spineLineDirection = (chestBone.position - pelvisBone.position).normalized;

            // 2. Measure lean depth by checking how vertical the spine is relative to world gravity up
            float spineVerticalDot = Mathf.Abs(Vector3.Dot(spineLineDirection, Vector3.up));

            // 3. Dynamically interpolate the tracking Up reference vector to match Step D perfectly
            Vector3 blendedUpReference = Vector3.Lerp(characterTransform.up, Vector3.up, spineVerticalDot).normalized;

            // 4. Construct stable cross planes from the torso girdle width
            Vector3 shoulderGirdleWidthDirection = (upperArmBone.position - chestBone.position).normalized;
            Vector3 skeletalDerivedForward = Vector3.Cross(shoulderGirdleWidthDirection, blendedUpReference).normalized;

            float sideSign = isLeftArm ? -1f : 1f;
            if (isLeftArm) skeletalDerivedForward = -skeletalDerivedForward;

            // Flatten the newly stabilized matrix heading onto the horizon floor plane securely
            Vector3 flatSkeletalForward = skeletalDerivedForward;
            flatSkeletalForward.y = 0f;

            if (flatSkeletalForward.sqrMagnitude < 0.001f) flatSkeletalForward = characterTransform.forward;
            else flatSkeletalForward.Normalize();

            // Blend with character capsule root heading based on your slider threshold properties
            Vector3 flatCharacterForward = characterTransform.forward;
            Vector3 finalBlendedForwardHeading = Vector3.Lerp(flatSkeletalForward, flatCharacterForward, pocketCharacterRootLeanBlend).normalized;

            // Reconstruct the safe slatted chest matrix frame
            Quaternion slattedChestWorldRot = Quaternion.LookRotation(finalBlendedForwardHeading, Vector3.up);
            Quaternion relativeSlattedChestDelta = Quaternion.Inverse(characterTransform.rotation) * slattedChestWorldRot;

            // Apply the singularity-free matrix to the gizmo transformer origin
            Gizmos.matrix = Matrix4x4.TRS(upperArmBone.position, characterTransform.rotation * relativeSlattedChestDelta, Vector3.one);

            Vector3 localOrigin = Vector3.zero;

            // Draw Yellow Hand Box
            float minX_Hand = Mathf.Min(-maxInwardPocket * sideSign, maxOutwardPocket * sideSign);
            float maxX_Hand = Mathf.Max(-maxInwardPocket * sideSign, maxOutwardPocket * sideSign);
            float minY_Hand = -_totalArmLength;
            float maxY_Hand = maxUpwardPocket;
            float minZ_Hand = -maxBackwardPocket;
            float maxZ_Hand = maxForwardPocket;

            Vector3 handBoxCenter = new Vector3((minX_Hand + maxX_Hand) * 0.5f, (minY_Hand + maxY_Hand) * 0.5f, (minZ_Hand + maxZ_Hand) * 0.5f);
            Vector3 handBoxSize = new Vector3(Mathf.Abs(maxX_Hand - minX_Hand), Mathf.Abs(maxY_Hand - minY_Hand), Mathf.Abs(maxZ_Hand - minZ_Hand));

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(localOrigin + handBoxCenter, handBoxSize);
            Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.04f);
            Gizmos.DrawCube(localOrigin + handBoxCenter, handBoxSize);

            // Draw Cyan Elbow Box
            float minX_Bend = Mathf.Min(-maxBendInward * sideSign, maxBendOutward * sideSign);
            float maxX_Bend = Mathf.Max(-maxBendInward * sideSign, maxBendOutward * sideSign);
            float minY_Bend = -_totalArmLength * 0.4f;
            float maxY_Bend = _totalArmLength * 0.4f;
            float minZ_Bend = -maxBendBackward;
            float maxZ_Bend = maxBendForward;

            Vector3 bendBoxCenter = new Vector3((minX_Bend + maxX_Bend) * 0.5f, (minY_Bend + maxY_Bend) * 0.5f, (minZ_Bend + maxZ_Bend) * 0.5f);
            Vector3 bendBoxSize = new Vector3(Mathf.Abs(maxX_Bend - minX_Bend), Mathf.Abs(maxY_Bend - minY_Bend), Mathf.Abs(maxZ_Bend - minZ_Bend));

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(localOrigin + bendBoxCenter, bendBoxSize);
            Gizmos.color = new Color(0f, 0.85f, 1f, 0.04f);
            Gizmos.DrawCube(localOrigin + bendBoxCenter, bendBoxSize);

            Gizmos.matrix = originalMatrix;
        }
    }
}

#endif