using System;

using UnityEngine;

using Swole.API.Unity.Animation;

namespace Swole.API.Unity
{
    [Serializable]
    public class CharacterWeightShifter
    {
        public CustomAnimator animator;

        public Transform rootTransform;

        [Header("Global Responsiveness Profile")]
        [Range(0.01f, 1f)]
        [Tooltip("Controls how fast the physics engine reacts to shifting hand targets. 1.0 is full instantaneous responsiveness (crisp bicep curls). Lower values force the system to filter out high-frequency movement, holding balance targets for way longer (eliminates locomotive wobbling).")]
        public float globalResponsiveness = 1.0f;

        [Header("Target References")]
        [Tooltip("The character's left hand transform or left dumbbell bone.")]
        public Transform leftHandTransform;
        [Tooltip("The character's right hand transform or right dumbbell bone.")]
        public Transform rightHandTransform;
        [Tooltip("The character's root/pelvis transform for lateral shifting.")]
        public Transform pelvisBone;
        [Tooltip("The spine transforms ordered sequentially from lowest (lumbar) to highest (thoracic).")]
        public Transform[] spineBones;

        [Header("Head & Neck References")]
        [Tooltip("The character's neck bone transform.")]
        public Transform neckBone;
        [Tooltip("The character's head bone transform.")]
        public Transform headBone;

        [Header("Dynamic Weights")]
        [Range(0f, 1000f)]
        [Tooltip("Simulated mass of the item in the left hand (in kg or abstract units).")]
        public float leftHandMass = 20f;
        [Range(0f, 1000f)]
        [Tooltip("Simulated mass of the item in the right hand (in kg or abstract units).")]
        public float rightHandMass = 20f;

        [Header("Global Response Tuning")]
        [Tooltip("Global scale factor for the overall positional offset of the pelvis.")]
        public float pelvisSensitivity = 0.02f;
        [Tooltip("Global scale factor for the overall rotational tilt of the spine.")]
        public float spineSensitivity = 1.5f;

        [Header("Posture & Balance Tuning")]
        [Range(0f, 1f)]
        [Tooltip("Global intensity for how much the character structurally leans away to counter-balance heavy weights.")]
        public float leanWeight = 0.5f;
        [Range(0f, 1f)]
        [Tooltip("Controls pelvis tilt & shift behavior. 0 = Athletic Brace (Hips tilt UP and shift TOWARD the weight). 1 = Fatigue Sag (Hips tilt DOWN and shift AWAY from the weight).")]
        public float pelvisTiltBehavior = 0.0f;
        [Range(0f, 1f)]
        [Tooltip("Scales down the side-to-side lateral displacement of the pelvis bone.")]
        public float pelvisSwayMultiplier = 0.333f;

        [Header("Pelvis Coordinate Mapping")]
        public Vector3 pelvisLocalUpAxis = new Vector3(0f, 1f, 0f);
        public Vector3 pelvisLocalSwayAxis = new Vector3(1f, 0f, 0f);
        public Vector3 pelvisLocalForwardAxis = new Vector3(0f, 0f, 1f);

        [Header("Spine Coordinate Mapping")]
        public Vector3 spineLocalPitchAxis = new Vector3(1f, 0f, 0f);
        public Vector3 spineLocalYawAxis = new Vector3(0f, 1f, 0f);
        public Vector3 spineLocalRollAxis = new Vector3(0f, 0f, 1f);

        [Header("Pelvis Translational Tuning")]
        public Vector3 pelvisTranslationAxisScale = new Vector3(1f, -0.2f, 1f);

        [Header("Spine Rotational Tuning")]
        public Vector3 spineRotationAxisScale = new Vector3(1f, 1f, 1f);
        public AnimationCurve spineDistributionCurve = AnimationCurve.Linear(0f, 0.4f, 1f, 0.1f);

        [Header("Grounding Adjustments")]
        [Range(0f, 1f)]
        public float forwardDropWeight = 0.2f;
        [Range(0f, 2f)]
        public float swayDropCompensation = 0.5f;

        [Header("Pelvis Constraints (Offsets along Custom Axes)")]
        public Vector3 minPelvisTranslation = new Vector3(-0.15f, -0.05f, -0.15f);
        public Vector3 maxPelvisTranslation = new Vector3(0.15f, 0.0f, 0.15f);

        [Header("Spine Constraints (Total Chain Limits)")]
        public Vector3 minSpineRotationAngles = new Vector3(-15f, -10f, -20f);
        public Vector3 maxSpineRotationAngles = new Vector3(25f, 10f, 20f);

        [Header("Leverage Bias Adjustments")]
        public Vector3 leverageBiasOffset = new Vector3(0f, 0f, 0.2f);

        [Header("Spring Parameters (Smooth Motion Only)")]
        public float stiffness = 120f;
        public float damping = 18f;

        [Header("Dynamic Leg Tracking")]
        public Transform leftThighBone;
        public Transform leftCalfBone;
        public Transform leftAnkleBone;
        [Space(5)]
        public Transform rightThighBone;
        public Transform rightCalfBone;
        public Transform rightAnkleBone;
        [Range(0.1f, 3f)]
        public float legLengthMultiplier = 1.0f;

        [Header("Custom IK Integration")]
        public CustomIKManager.IKController leftLegIkController;
        public CustomIKManager.IKController rightLegIkController;
        public float dynamicIkActivationThreshold = 0.02f;
        public float kneePolePushDistance = 1.5f;

        [Header("Kinetic Momentum Integration")]
        [Range(0f, 5f)]
        public float velocityImpetusScale = 1.2f;

        [Header("Anatomical Spine Resistance")]
        public float spineResistanceStartAngle = 15f;
        public float spineResistanceMaxAngle = 40f;

        [Header("Kinetic Velocity Protection")]
        public float maxHandVelocityCap = 8f;
        public float maxSpringAccelerationCap = 250f;

        [Header("Head & Neck Tuning")]
        [Tooltip("Global multiplier for procedural head and neck modifications.")]
        public float headNeckSensitivity = 1.0f;
        [Range(0f, 1f)]
        [Tooltip("How much the head actively counters the spine's roll/tilt to keep the eyes parallel with the ground horizon.")]
        public float horizonTrackingWeight = 0.6f;
        [Range(0f, 2f)]
        [Tooltip("How much the head rolls AWAY from a heavy sideways weight (Nesting into the opposite shoulder).")]
        public float shoulderNestingWeight = 1.2f;
        [Range(0f, 2f)]
        [Tooltip("How much the neck pushes forward and the chin tilts up when pulling or bracing against a heavy forward strain.")]
        public float strainBraceWeight = 0.5f;

        [Header("Head & Neck Coordinate Mapping")]
        [Tooltip("The local axis vector around which the head/neck rotates to PITCH (Nod up/down). e.g. (1,0,0)")]
        public Vector3 headLocalPitchAxis = new Vector3(1f, 0f, 0f);
        [Tooltip("The local axis vector around which the head/neck rotates to YAW (Look left/right). e.g. (0,1,0)")]
        public Vector3 headLocalYawAxis = new Vector3(0f, 1f, 0f);
        [Tooltip("The local axis vector around which the head/neck rotates to ROLL (Tilt ear to shoulder). e.g. (0,0,1)")]
        public Vector3 headLocalRollAxis = new Vector3(0f, 0f, 1f);

        [Header("Advanced Head Stabilization & Gates")]
        [Tooltip("The minimum hand mass required before any procedural head/neck strain or roll begins to activate. Prevents wiggling when empty-handed.")]
        public float minMassForHeadStrain = 5f;

        [Tooltip("The mass amount at which head/neck strain reaches 100% intensity. Max out values above this.")]
        public float maxMassForHeadStrain = 30f;

        [Range(0f, 1f)]
        [Tooltip("Reduces neck pitching based on how far the spine is already bent forward by the animation. Fixes looking up at the ceiling when bending over.")]
        public float dynamicPosturePitchDampening = 0.8f;

        [Header("Advanced Procedural Blending")]
        [Tooltip("The angle of PROCEDURALLY applied spine pitch at which the head starts locking to the horizon instead of tilting with the weight.")]
        public float systemPitchBlendStart = 3f;

        [Tooltip("The angle of PROCEDURALLY applied spine pitch at which the head completely switches to horizon-locking.")]
        public float systemPitchBlendMax = 12f;

        [Header("Leverage Center & Curve Adjustments")]
        [Tooltip("Local offset from the root transform to define where the character's real balance center of mass sits. Lift this up/forward to align with the mid-torso pivot.")]
        public Vector3 balanceCenterLocalOffset = new Vector3(0f, 1.0f, 0f);

        [Tooltip("Smoothly dampens position changes when hands are very close to the center of mass. Higher values suppress aggressive body reactions to tiny arm adjustments.")]
        [Range(0.01f, 0.5f)]
        public float centerOfMassDeadZoneRadius = 0.1f;

        [Header("Advanced Pitch Curve")]
        [Range(1f, 3f)]
        [Tooltip("Controls the exponential scaling of the forward/backward torque. A value of 1 is linear. A value of 2 means doubling the hand distance quadruples the hunching effect, keeping close-range movements incredibly subtle.")]
        public float forwardLeveragePowerExponent = 1.0f;

        [Header("Pelvis Rotational Tuning")]
        [Tooltip("Scales the procedural rotational tilt of the pelvis. X = Pitch (Forward/Back tilt), Y = Twist, Z = Roll (Side-to-Side dip).")]
        public Vector3 pelvisRotationAxisScale = new Vector3(1f, 0f, -1f);

        [Header("Pelvis Rotational Constraints")]
        public Vector3 minPelvisRotationAngles = new Vector3(-8f, -5f, -10f);
        public Vector3 maxPelvisRotationAngles = new Vector3(10f, 5f, 10f);

        [Range(0f, 2f)]
        [Tooltip("Controls how much the pelvis physically shifts position laterally as a direct result of it tilting. Simulates the hips shifting over the weight-bearing leg joint.")]
        public float tiltTranslationCoupling = 0.5f;

        [Header("Pelvis Tilt Inversion")]
        [Tooltip("If the pelvis tilts into the weight instead of away from it due to your rig's bone rotation, toggle this to invert the roll rotation direction.")]
        public bool invertPelvisRollDirection = false;

        [Header("Asymmetrical Balance Ratios")]
        [Range(0f, 1f)]
        [Tooltip("Controls how much the pelvis shifts BACKWARD when the character leans forward. 1.0 is full weight counter-balance.")]
        public float forwardLeanPelvisRatio = 0.8f;

        [Range(0f, 1f)]
        [Tooltip("Controls how much the pelvis shifts FORWARD when the character arches backward. Lowering this (e.g., 0.4) allows the spine to hang beautifully behind the hips without an excessive pelvic thrust.")]
        public float backwardLeanPelvisRatio = 0.4f;

        [Header("Head Rotation Protection")]
        [Tooltip("The angle (in degrees) of head yaw look-around at which procedural head tilt begins to scale down.")]
        public float headYawTiltDampenStartAngle = 15f;
        [Tooltip("The angle (in degrees) of head yaw look-around at which procedural head tilt is completely turned off.")]
        public float headYawTiltDampenMaxAngle = 60f;

        [Header("Shoulder Tracking Bones")]
        [Tooltip("Left Collar/Clavicle bone transform.")]
        public Transform leftCollarBone;
        [Tooltip("Left Shoulder/Upper Arm bone transform.")]
        public Transform leftUpperArmBone;
        [Space(5)]
        [Tooltip("Right Collar/Clavicle bone transform.")]
        public Transform rightCollarBone;
        [Tooltip("Right Shoulder/Upper Arm bone transform.")]
        public Transform rightUpperArmBone;

        [Header("Dynamic Load Bracing Limits")]
        [Tooltip("The soft max weight ceiling (in kg or abstract units) representing maximum character effort. Used to scale posture intensity dynamically.")]
        public float maxWeightCap = 30f;

        [Header("Shoulder Axis Mapping (Local Space)")]
        [Tooltip("The local axis vector around which the Collar Bone rotates to ROLL (Tilt up/down).")]
        public Vector3 collarLocalRollAxis = new Vector3(0f, 0f, 1f);
        [Tooltip("The local axis vector around which the Collar Bone rotates to YAW (Twist forward/back).")]
        public Vector3 collarLocalYawAxis = new Vector3(0f, 1f, 0f);
        [Space(5)]
        [Tooltip("The local axis vector around which the Upper Arm bone rotates to ROLL (Tilt up/down).")]
        public Vector3 armLocalRollAxis = new Vector3(0f, 0f, 1f);
        [Tooltip("The local axis vector around which the Upper Arm bone rotates to YAW (Twist forward/back).")]
        public Vector3 armLocalYawAxis = new Vector3(0f, 1f, 0f);

        [Header("Shoulder Alignment - Unloaded (Zero Weight)")]
        [Range(0f, 1f)]
        [Tooltip("Shoulder roll correction when carrying absolutely zero weight.")]
        public float shoulderRollRestorationUnloaded = 0.0f;
        [Range(0f, 1f)]
        [Tooltip("Shoulder yaw correction when carrying absolutely zero weight.")]
        public float shoulderYawRestorationUnloaded = 0.0f;

        [Header("Shoulder Alignment - Loaded (Max Weight)")]
        [Range(0f, 1f)]
        [Tooltip("Shoulder roll correction when carrying weight at or above the soft max weight cap.")]
        public float shoulderRollRestorationMaxLoaded = 0.6f;
        [Range(0f, 1f)]
        [Tooltip("Shoulder yaw correction when carrying weight at or above the soft max weight cap.")]
        public float shoulderYawRestorationMaxLoaded = 0.4f;

        [Header("Advanced Velocity Controls")]
        [Tooltip("Global multiplier for the kinetic velocity payload. Exposing this allows external script state machines or animation events to scale the effect at runtime.")]
        public float globalVelocityMultiplier = 1.0f;

        [Tooltip("The character ground speed (in meters/sec) at which the locomotive speed gate begins to scale down kinetic reactions. e.g. 1.5 m/s for a fast walk.")]
        public float speedGateStartThreshold = 1.0f;

        [Tooltip("The character ground speed (in meters/sec) at which kinetic reactions are completely muted to prevent 'drunk running' artifacts. e.g. 4.5 m/s for a full sprint.")]
        public float speedGateMaxThreshold = 4.0f;

        public float walkSpeedResponsiveness = 0.17f;
        public float maxSprintResponsiveness = 0.02f;

        [Header("Spine Pitch Optimization")]
        [Range(0f, 1f)]
        [Tooltip("Scales the intensity of the spine's forward/backward leaning independently of your axis mapping. Setting this to 0 completely eliminates all forward/backward hunching.")] 
        public float spinePitchMultiplier = 1.0f;

        // Internal runtime caches for tracking local positions across frames
        private Vector3 _lastLeftHandLocalPos;
        private Vector3 _lastRightHandLocalPos;
        private Vector3 _lastRootWorldPos;

        // Runtime state tracking caches
        private float _calculatedLegLength = 1.0f;
        private Vector3 _currentLeverageOffset;
        private Vector3 _leverageVelocity;
        private Vector3 _targetLeverageOffset;
        private Vector3 _defaultStandingPelvisLocalPos;
        private Vector3 _lastLeftHandPos;
        private Vector3 _lastRightHandPos;

        // Internal runtime ground speed tracker mapped across execution channels
        private float _currentGroundSpeed = 0f;

        public void Init()
        {
            _currentLeverageOffset = Vector3.zero;
            _leverageVelocity = Vector3.zero;
            _targetLeverageOffset = Vector3.zero;

            if (pelvisBone != null) _defaultStandingPelvisLocalPos = pelvisBone.localPosition;

            // Cache genuine root-space coordinates at startup
            if (leftHandTransform != null && rootTransform != null)
            {
                _lastLeftHandPos = leftHandTransform.position;
                _lastLeftHandLocalPos = rootTransform.InverseTransformPoint(leftHandTransform.position);
            }
            if (rightHandTransform != null && rootTransform != null)
            {
                _lastRightHandPos = rightHandTransform.position;
                _lastRightHandLocalPos = rootTransform.InverseTransformPoint(rightHandTransform.position);
            }

            if (rootTransform != null)
            {
                _lastRootWorldPos = rootTransform.position;

                if (animator == null)
                {
                    animator = rootTransform.GetComponentInParent<CustomAnimator>(); 
                }
            }

            RecalculateLegLength();
        }

        /// <summary>
        /// Measures the exact distance of the skeletal segments. 
        /// Call this if the character switches to a model with different proportions mid-game.
        /// </summary>
        public void RecalculateLegLength()
        {
            float leftLeg = 0f;
            float rightLeg = 0f;

            if (leftThighBone != null && leftCalfBone != null && leftAnkleBone != null)
            {
                leftLeg = Vector3.Distance(leftThighBone.position, leftCalfBone.position) +
                          Vector3.Distance(leftCalfBone.position, leftAnkleBone.position);
            }

            if (rightThighBone != null && rightCalfBone != null && rightAnkleBone != null)
            {
                rightLeg = Vector3.Distance(rightThighBone.position, rightCalfBone.position) +
                           Vector3.Distance(rightCalfBone.position, rightAnkleBone.position);
            }

            if (leftLeg <= 0f && rightLeg <= 0f)
            {
                _calculatedLegLength = 1.0f;
                return;
            }

            if (leftLeg > 0f && rightLeg > 0f) _calculatedLegLength = (leftLeg + rightLeg) * 0.5f;
            else _calculatedLegLength = leftLeg > 0f ? leftLeg : rightLeg;
        }

        public void UpdateSecondaryAnimation(bool syncIkFk, float deltaTime)
        {
            if (deltaTime <= 0f || rootTransform == null) return;

            if (syncIkFk && animator != null)
            {
                animator.SyncIKFK(false, true, true, 1f, 1f); 
            }

            // 1. Calculate raw physics payload targets and cache ground speed values
            CalculateRelativeLeverageForces(deltaTime);

            // =========================================================================
            // LOCOMOTIVE DEPENDENT RESPONSIVENESS BLENDING
            // =========================================================================
            // We treat your inspector defined globalResponsiveness as the idle baseline setting.
            // As ground speed increases toward your walk/run thresholds, we automatically 
            // drop the responsiveness down towards your proven 0.17 target.
            float dynamicResponsiveness = globalResponsiveness;

            if (_currentGroundSpeed > speedGateStartThreshold)
            {
                // Compute how far along the locomotion track the character is traveling (0.0 to 1.0)
                float range = speedGateMaxThreshold - speedGateStartThreshold;
                float speedRatio = range > 0f ? Mathf.Clamp01((_currentGroundSpeed - speedGateStartThreshold) / range) : 0f;

                // Blends smoothly from your idle configuration down toward 0.17 at walk speed, 
                // and safely drops even lower (approaching rigid core) as they hit a maximum sprint.
                float targetLocomotiveResponsiveness = Mathf.Lerp(walkSpeedResponsiveness, maxSprintResponsiveness, speedRatio); 
                dynamicResponsiveness = Mathf.Lerp(globalResponsiveness, targetLocomotiveResponsiveness, speedRatio); 
            }

            // Process using your preferred 100f baseline scaling factor
            float targetSmoothingSpeed = dynamicResponsiveness * 100f;

            // Smoothly trail the master spring engine destination to completely swallow high-frequency wobble
            _targetLeverageOffset = Vector3.Lerp(_currentLeverageOffset, _targetLeverageOffset, targetSmoothingSpeed * deltaTime);

            // 2. Pass the cushioned target into the spring loop and apply shifts
            SimulateSpring(deltaTime);
            ApplyProceduralOffsets();
        }

        private void CalculateRelativeLeverageForces(float deltaTime)
        {
            if (deltaTime <= 0f || rootTransform == null) return;

            Vector3 totalStaticLeverage = Vector3.zero;
            Vector3 dynamicMomentumForce = Vector3.zero;
            Vector3 localBalancePivot = balanceCenterLocalOffset;

            // We track total rotational twist torque separately
            float isolatedYawTorque = 0f;

            // =========================================================================
            // LOCOMOTIVE SPEED GATE CALCULATION
            // =========================================================================
            // Measure the character root's actual translation velocity through the world
            Vector3 rootWorldVel = (rootTransform.position - _lastRootWorldPos) / deltaTime;
            _lastRootWorldPos = rootTransform.position;

            // Flatten it to the ground plane to measure pure locomotion speed
            float groundSpeed = Vector3.ProjectOnPlane(rootWorldVel, rootTransform.up).magnitude;
            _currentGroundSpeed = groundSpeed; // Injected to pass data cleanly to the update hub

            // Calculate the speed gate factor (1 = standing still / full reaction, 0 = full sprint / zero reaction)
            float speedGateFactor = 1f;
            if (groundSpeed > speedGateStartThreshold)
            {
                float range = speedGateMaxThreshold - speedGateStartThreshold;
                float overflow = groundSpeed - speedGateStartThreshold;
                speedGateFactor = range > 0f ? Mathf.Clamp01(1f - (overflow / range)) : 0f;
            }

            // Combine the automatic speed gate dampener with your manual external master variable field
            float finalActiveVelocityGain = globalVelocityMultiplier * speedGateFactor;

            // --- PROCESS LEFT HAND KINETICS ---
            if (leftHandTransform != null && leftHandMass > 0f)
            {
                Vector3 rawLeftLocalPos = rootTransform.InverseTransformPoint(leftHandTransform.position);
                Vector3 leverageVector = rawLeftLocalPos - localBalancePivot;

                isolatedYawTorque += (leverageVector.x * leverageVector.z) * leftHandMass;
                leverageVector.y = 0f; // Kept strictly zeroed on pure Y to guarantee zero pelvic crouching!

                float distanceToPivot = leverageVector.magnitude;
                if (distanceToPivot > 0f)
                {
                    float distanceScalingFactor = Mathf.Clamp01((distanceToPivot - centerOfMassDeadZoneRadius) / distanceToPivot);
                    if (distanceToPivot < centerOfMassDeadZoneRadius) distanceScalingFactor = 0f;

                    totalStaticLeverage += (leverageVector * distanceScalingFactor) * leftHandMass;
                }

                // FIXED: Measure velocity strictly inside the root's local space layout.
                // This strips out any forward world movement spikes generated by running capsule translation.
                Vector3 leftHandLocalVel = (rawLeftLocalPos - _lastLeftHandLocalPos) / deltaTime;
                leftHandLocalVel = Vector3.ClampMagnitude(leftHandLocalVel, maxHandVelocityCap);
                leftHandLocalVel.y = 0f; // Isolate horizontal velocity vectors

                dynamicMomentumForce += leftHandLocalVel * leftHandMass * velocityImpetusScale * finalActiveVelocityGain;

                _lastLeftHandLocalPos = rawLeftLocalPos;
                _lastLeftHandPos = leftHandTransform.position;
            }

            // --- PROCESS RIGHT HAND KINETICS ---
            if (rightHandTransform != null && rightHandMass > 0f)
            {
                Vector3 rawRightLocalPos = rootTransform.InverseTransformPoint(rightHandTransform.position);
                Vector3 leverageVector = rawRightLocalPos - localBalancePivot;

                isolatedYawTorque += (leverageVector.x * leverageVector.z) * rightHandMass;
                leverageVector.y = 0f;

                float distanceToPivot = leverageVector.magnitude;
                if (distanceToPivot > 0f)
                {
                    float distanceScalingFactor = Mathf.Clamp01((distanceToPivot - centerOfMassDeadZoneRadius) / distanceToPivot);
                    if (distanceToPivot < centerOfMassDeadZoneRadius) distanceScalingFactor = 0f;

                    totalStaticLeverage += (leverageVector * distanceScalingFactor) * rightHandMass;
                }

                // FIXED SIDE-MIRRORING: Subtraction error resolved. 
                // Correctly measures local position changes across frames without world space corruption.
                Vector3 rightHandLocalVel = (rawRightLocalPos - _lastRightHandLocalPos) / deltaTime;
                rightHandLocalVel = Vector3.ClampMagnitude(rightHandLocalVel, maxHandVelocityCap);
                rightHandLocalVel.y = 0f;

                dynamicMomentumForce += rightHandLocalVel * rightHandMass * velocityImpetusScale * finalActiveVelocityGain;

                _lastRightHandLocalPos = rawRightLocalPos;
                _lastRightHandPos = rightHandTransform.position;
            }

            totalStaticLeverage += leverageBiasOffset;
            totalStaticLeverage.y = 0f;

            // Pure, unified counter-balance leaning direction layout
            Vector3 cleanLeanVector = -totalStaticLeverage;
            Vector3 staticPostureTarget = cleanLeanVector * leanWeight;

            // Stacks kinetic swing inertia smoothly on top of your steady-state balance targets
            _targetLeverageOffset = staticPostureTarget + (dynamicMomentumForce * 0.1f);
            _targetLeverageOffset.y = isolatedYawTorque;
        }

        private void SimulateSpring(float deltaTime)
        {
            Vector3 springForce = (-stiffness * (_currentLeverageOffset - _targetLeverageOffset)) - (damping * _leverageVelocity);

            // Axis-isolated clamping loop ensures single axis spikes cannot choke orthogonal lean axes
            springForce.x = Mathf.Clamp(springForce.x, -maxSpringAccelerationCap, maxSpringAccelerationCap);
            springForce.y = Mathf.Clamp(springForce.y, -maxSpringAccelerationCap, maxSpringAccelerationCap);
            springForce.z = Mathf.Clamp(springForce.z, -maxSpringAccelerationCap, maxSpringAccelerationCap);

            _leverageVelocity += springForce * deltaTime;
            _currentLeverageOffset += _leverageVelocity * deltaTime;
        }

        private void ApplyProceduralOffsets()
        {
            ApplyPelvisOffsets();
            ApplySpineOffsets();
            ApplyShoulderOffsets();
            ApplyHeadAndNeckOffsets(); 
        }

        private void ApplyPelvisOffsets()
        {
            if (pelvisBone == null || rootTransform == null) return; 

            // 1. ISOLATE STABLE WORLD DIRECTION VECTORS FROM CHARACTER ROOT
            Vector3 stableWorldRight = rootTransform.right;
            Vector3 stableWorldForward = rootTransform.forward;
            Vector3 stableWorldVertical = rootTransform.up;

            // =========================================================================
            // 2. DYNAMIC PELVIS TILT BLENDING (SINGLE CONTROL SLIDER)
            // =========================================================================
            float rawSwayForce = _currentLeverageOffset.x;

            // Behavior 0 (Brace): Hips tilt UP into the weight (-rawSwayForce)
            float braceRollTarget = -rawSwayForce * 25f;
            // Behavior 1 (Sag): Hips sag DOWN with the weight (+rawSwayForce)
            float sagRollTarget = rawSwayForce * 25f;

            // Crossfade seamlessly using the single unified behavior slider
            float blendedRoll = Mathf.Lerp(braceRollTarget, sagRollTarget, pelvisTiltBehavior);

            float pelvisRoll = blendedRoll * pelvisRotationAxisScale.z * pelvisSensitivity;
            pelvisRoll = Mathf.Clamp(pelvisRoll, minPelvisRotationAngles.z, maxPelvisRotationAngles.z);

            if (invertPelvisRollDirection)
            {
                pelvisRoll *= -1f;
            }

            // =========================================================================
            // 3. TRANSLATION DIRECTION LINKED DIRECTLY TO PELVIC TILT BEHAVIOR
            // =========================================================================
            float baseSwayMagnitude = Mathf.Abs(_currentLeverageOffset.x * pelvisTranslationAxisScale.x * pelvisSensitivity);
            baseSwayMagnitude *= pelvisSwayMultiplier;

            // Determine direction strictly by combining force direction and our behavior slider:
            // If pelvisTiltBehavior is 0 (Brace), direction sign matches the force -> shift TOWARD weight.
            // If pelvisTiltBehavior is 1 (Sag), direction sign inverts the force -> shift AWAY from weight.
            float forceDirectionSign = Mathf.Sign(rawSwayForce);
            float translationDirectionSign = Mathf.Lerp(forceDirectionSign, -forceDirectionSign, pelvisTiltBehavior);

            float rawSwayAmount = baseSwayMagnitude * translationDirectionSign;
            rawSwayAmount = Mathf.Clamp(rawSwayAmount, minPelvisTranslation.x, maxPelvisTranslation.x);

            // Process forward/backward translation
            float rawFwdAmount = _currentLeverageOffset.z * pelvisTranslationAxisScale.z * pelvisSensitivity;
            rawFwdAmount = Mathf.Clamp(rawFwdAmount, minPelvisTranslation.z, maxPelvisTranslation.z);

            if (rawFwdAmount > 0f) rawFwdAmount *= forwardLeanPelvisRatio;
            else rawFwdAmount *= backwardLeanPelvisRatio;

            // =========================================================================
            // 4. ANATOMICAL HEIGHT COMPENSATION (LATERAL SWAY SHIFT ONLY)
            // =========================================================================
            float horizontalDisplacement = Mathf.Abs(rawSwayAmount);
            float modifiedLegLength = _calculatedLegLength * legLengthMultiplier;
            float safeDisplacement = Mathf.Min(horizontalDisplacement, modifiedLegLength * 0.95f);
            float geometricDrop = modifiedLegLength - Mathf.Sqrt((modifiedLegLength * modifiedLegLength) - (safeDisplacement * safeDisplacement));

            float rawUpAmount = _currentLeverageOffset.y * pelvisTranslationAxisScale.y * pelvisSensitivity;
            rawUpAmount -= (geometricDrop * swayDropCompensation);

            // Dynamic Height Damping Ceiling
            float currentPelvisHeightDelta = Vector3.Dot(pelvisBone.localPosition - _defaultStandingPelvisLocalPos, stableWorldVertical);
            if (currentPelvisHeightDelta < 0f)
            {
                float maxDropRange = Mathf.Abs(minPelvisTranslation.y);
                rawUpAmount *= maxDropRange > 0f ? Mathf.Clamp01(1f - (Mathf.Abs(currentPelvisHeightDelta) / maxDropRange)) : 1f;
            }

            if (rawUpAmount > 0f) rawUpAmount = 0f;
            rawUpAmount = Mathf.Clamp(rawUpAmount, minPelvisTranslation.y, 0f);

            // =========================================================================
            // 5. ASSEMBLE COMBINED WORLD POSITIONS & APPLY
            // =========================================================================
            Vector3 directedForwardTranslation = stableWorldForward * (-rawFwdAmount);
            Vector3 directedSwayTranslation = stableWorldRight * rawSwayAmount;

            Vector3 worldTranslationDelta = directedSwayTranslation +
                                            directedForwardTranslation +
                                            (stableWorldVertical * rawUpAmount);

            pelvisBone.position += worldTranslationDelta;

            Quaternion pelvisTiltOffset = Quaternion.AngleAxis(pelvisRoll, stableWorldForward);
            pelvisBone.localRotation *= pelvisTiltOffset;

            // =========================================================================
            // 6. DYNAMIC KNEE POLE SYSTEM
            // =========================================================================
            Vector3 characterUp = rootTransform.up;
            /*Vector3 characterForward = rootTransform.forward;
            Vector3 leftKneeDirectionXZ = characterForward;
            Vector3 rightKneeDirectionXZ = characterForward;

            if (leftLegIkController != null && leftLegIkController.BendGoal != null && leftCalfBone != null)
            {
                leftKneeDirectionXZ = Vector3.ProjectOnPlane(leftLegIkController.BendGoal.position - leftCalfBone.position, characterUp).normalized;
                if (leftKneeDirectionXZ.sqrMagnitude < 0.01f) leftKneeDirectionXZ = characterForward;
            }

            if (rightLegIkController != null && rightLegIkController.BendGoal != null && rightCalfBone != null)
            {
                rightKneeDirectionXZ = Vector3.ProjectOnPlane(rightLegIkController.BendGoal.position - rightCalfBone.position, characterUp).normalized;
                if (rightKneeDirectionXZ.sqrMagnitude < 0.01f) rightKneeDirectionXZ = characterForward;
            } */

            //if (horizontalDisplacement >= dynamicIkActivationThreshold)
            //{
                if (leftLegIkController != null) { leftLegIkController.SetActive(true); leftLegIkController.SetWeight(1f); leftLegIkController.SetPositionWeight(1f); }
                if (rightLegIkController != null) { rightLegIkController.SetActive(true); rightLegIkController.SetWeight(1f); rightLegIkController.SetPositionWeight(1f); }  
            //}

            //Vector3 pelvisPosAfterShift = pelvisBone.position;
            //if (leftLegIkController != null && leftLegIkController.BendGoal != null) leftLegIkController.BendGoal.position = pelvisPosAfterShift + (leftKneeDirectionXZ * kneePolePushDistance);
            //if (rightLegIkController != null && rightLegIkController.BendGoal != null) rightLegIkController.BendGoal.position = pelvisPosAfterShift + (rightKneeDirectionXZ * kneePolePushDistance);
        }

        private void ApplyShoulderOffsets()
        {
            if (leftCollarBone == null && rightCollarBone == null && leftUpperArmBone == null && rightUpperArmBone == null) return;

            // 1. Isolate the exact procedural roll and yaw displacements generated by our system
            float systemAppliedSpineRoll = _currentLeverageOffset.x * spineRotationAxisScale.z * spineSensitivity;
            systemAppliedSpineRoll = Mathf.Clamp(systemAppliedSpineRoll, minSpineRotationAngles.z, maxSpineRotationAngles.z);

            float systemAppliedSpineYaw = _currentLeverageOffset.y * spineRotationAxisScale.y * spineSensitivity;
            systemAppliedSpineYaw = Mathf.Clamp(systemAppliedSpineYaw, minSpineRotationAngles.y, maxSpineRotationAngles.y);

            // If no procedural deformation is active on the spine, skip shoulder corrections entirely
            if (Mathf.Abs(systemAppliedSpineRoll) <= 0.01f && Mathf.Abs(systemAppliedSpineYaw) <= 0.01f) return;

            // Pre-cache safe normalized tracking vectors
            Vector3 cRollAxis = collarLocalRollAxis.normalized;
            Vector3 cYawAxis = collarLocalYawAxis.normalized;
            Vector3 aRollAxis = armLocalRollAxis.normalized;
            Vector3 aYawAxis = armLocalYawAxis.normalized;

            // =========================================================================
            // 2. PROCESS AND APPLY LEFT SIDE OFFSETS (ISOLATED TRACK)
            // =========================================================================
            if (leftCollarBone != null || leftUpperArmBone != null)
            {
                // Calculate effort factor strictly for the left hand
                float leftEffortFactor = maxWeightCap > 0f ? Mathf.Clamp01(leftHandMass / maxWeightCap) : 0f;

                // Interpolate restoration weights using the left hand load only
                float leftRollRestoration = Mathf.Lerp(shoulderRollRestorationUnloaded, shoulderRollRestorationMaxLoaded, leftEffortFactor);
                float leftYawRestoration = Mathf.Lerp(shoulderYawRestorationUnloaded, shoulderYawRestorationMaxLoaded, leftEffortFactor); 

                float leftRollAngle = -systemAppliedSpineRoll * leftRollRestoration;
                float leftYawAngle = -systemAppliedSpineYaw * leftYawRestoration;

                // Apply to left collar bone additively over the clean animation frame
                if (leftCollarBone != null)
                {
                    Quaternion cRoll = Quaternion.AngleAxis(leftRollAngle, cRollAxis);
                    Quaternion cYaw = Quaternion.AngleAxis(leftYawAngle, cYawAxis);
                    leftCollarBone.localRotation *= (cRoll * cYaw);
                }

                // Apply to left upper arm bone
                if (leftUpperArmBone != null)
                {
                    Quaternion aRoll = Quaternion.AngleAxis(leftRollAngle, aRollAxis);
                    Quaternion aYaw = Quaternion.AngleAxis(leftYawAngle, aYawAxis);
                    leftUpperArmBone.localRotation *= (aRoll * aYaw);
                }
            }

            // =========================================================================
            // 3. PROCESS AND APPLY RIGHT SIDE OFFSETS (ISOLATED TRACK)
            // =========================================================================
            if (rightCollarBone != null || rightUpperArmBone != null)
            {
                // Calculate effort factor strictly for the right hand
                float rightEffortFactor = maxWeightCap > 0f ? Mathf.Clamp01(rightHandMass / maxWeightCap) : 0f;

                // Interpolate restoration weights using the right hand load only
                float rightRollRestoration = Mathf.Lerp(shoulderRollRestorationUnloaded, shoulderRollRestorationMaxLoaded, rightEffortFactor);
                float rightYawRestoration = Mathf.Lerp(shoulderYawRestorationUnloaded, shoulderYawRestorationMaxLoaded, rightEffortFactor);

                float rightRollAngle = -systemAppliedSpineRoll * rightRollRestoration;
                float rightYawAngle = -systemAppliedSpineYaw * rightYawRestoration;

                // Apply to right collar bone
                if (rightCollarBone != null)
                {
                    Quaternion cRoll = Quaternion.AngleAxis(rightRollAngle, cRollAxis);
                    Quaternion cYaw = Quaternion.AngleAxis(rightYawAngle, cYawAxis);
                    rightCollarBone.localRotation *= (cRoll * cYaw);
                }

                // Apply to right upper arm bone
                if (rightUpperArmBone != null)
                {
                    Quaternion aRoll = Quaternion.AngleAxis(rightRollAngle, aRollAxis);
                    Quaternion aYaw = Quaternion.AngleAxis(rightYawAngle, aYawAxis);
                    rightUpperArmBone.localRotation *= (aRoll * aYaw);
                }
            }
        }

        private void ApplySpineOffsets()
        {
            if (spineBones == null || spineBones.Length == 0) return;

            Vector3 pitchAxis = spineLocalPitchAxis.normalized;
            Vector3 yawAxis = spineLocalYawAxis.normalized;
            Vector3 rollAxis = spineLocalRollAxis.normalized;

            float totalPitch = _currentLeverageOffset.z * spineRotationAxisScale.x * spineSensitivity;
            float totalYaw = _currentLeverageOffset.y * spineRotationAxisScale.y * spineSensitivity;
            float totalRoll = _currentLeverageOffset.x * spineRotationAxisScale.z * spineSensitivity;

            totalPitch *= spinePitchMultiplier;

            totalPitch = Mathf.Clamp(totalPitch, minSpineRotationAngles.x, maxSpineRotationAngles.x);
            totalYaw = Mathf.Clamp(totalYaw, minSpineRotationAngles.y, maxSpineRotationAngles.y);
            totalRoll = Mathf.Clamp(totalRoll, minSpineRotationAngles.z, maxSpineRotationAngles.z);

            int boneCount = spineBones.Length;
            Quaternion cumulativeAnimRotation = Quaternion.identity;

            for (int i = 0; i < boneCount; ++i)
            {
                if (spineBones[i] == null) continue;

                cumulativeAnimRotation *= spineBones[i].localRotation;
                float totalSpineAngle = Quaternion.Angle(Quaternion.identity, cumulativeAnimRotation);
                float resistanceFactor = 1f;

                if (totalSpineAngle > spineResistanceStartAngle)
                {
                    float range = spineResistanceMaxAngle - spineResistanceStartAngle;
                    float overflow = totalSpineAngle - spineResistanceStartAngle;
                    resistanceFactor = range > 0f ? Mathf.Clamp01(1f - (overflow / range)) : 0f;
                }

                float normalizedHeight = boneCount > 1 ? (float)i / (boneCount - 1) : 0f; 
                float distributionModifier = spineDistributionCurve.Evaluate(normalizedHeight);

                float bonePitch = totalPitch * distributionModifier * resistanceFactor;
                float boneYaw = totalYaw * distributionModifier * resistanceFactor;
                float boneRoll = totalRoll * distributionModifier * resistanceFactor;

                Quaternion finalSpineCrumpleOffset = Quaternion.AngleAxis(bonePitch, pitchAxis) *
                                                      Quaternion.AngleAxis(boneYaw, yawAxis) *
                                                      Quaternion.AngleAxis(boneRoll, rollAxis);

                spineBones[i].localRotation *= finalSpineCrumpleOffset;
            }
        }

        private void ApplyHeadAndNeckOffsets()
        {
            if (neckBone == null && headBone == null) return;

            // 1. DYNAMIC MASS GATING (Effort Thresholds)
            float combinedHandMass = leftHandMass + rightHandMass;
            float massEffortFactor = Mathf.InverseLerp(minMassForHeadStrain, maxMassForHeadStrain, combinedHandMass);
            if (massEffortFactor <= 0f) return;

            Vector3 pitchAxis = headLocalPitchAxis.normalized;
            Vector3 yawAxis = headLocalYawAxis.normalized;
            Vector3 rollAxis = headLocalRollAxis.normalized;

            float sidewaysForce = _currentLeverageOffset.x;
            float forwardForce = _currentLeverageOffset.z;

            // 2. ISOLATE PROCEDURAL SPINE DEFORMATION ONLY
            float systemAppliedSpinePitch = _currentLeverageOffset.z * spineRotationAxisScale.x * spineSensitivity;
            systemAppliedSpinePitch = Mathf.Clamp(systemAppliedSpinePitch, minSpineRotationAngles.x, maxSpineRotationAngles.x);

            float systemAppliedSpineRoll = _currentLeverageOffset.x * spineRotationAxisScale.z * spineSensitivity;
            systemAppliedSpineRoll = Mathf.Clamp(systemAppliedSpineRoll, minSpineRotationAngles.z, maxSpineRotationAngles.z);

            // 3. YAW-BASED HEAD TILT DAMPENING BASED ON FORWARD LEAN INTENSITY
            // Project the head's local forward onto the root's horizontal plane to find true yaw look-around degrees
            Vector3 localHeadForward = headBone.forward;
            Vector3 flattenedForward = Vector3.ProjectOnPlane(localHeadForward, rootTransform.up).normalized;
            float currentHeadYawAngle = Vector3.Angle(rootTransform.forward, flattenedForward);

            // Compute the raw yaw reduction factor (1 = look forward/full tilt, 0 = look fully side/zero tilt)
            float rawYawDampenFactor = 1f;
            if (currentHeadYawAngle > headYawTiltDampenStartAngle)
            {
                float range = headYawTiltDampenMaxAngle - headYawTiltDampenStartAngle;
                float overflow = currentHeadYawAngle - headYawTiltDampenStartAngle;
                rawYawDampenFactor = range > 0f ? Mathf.Clamp01(1f - (overflow / range)) : 0f;
            }

            // NEW PREFERENCE INTEGRATION: Scale the application of the yaw reduction based strictly on forward lean.
            // If systemAppliedSpinePitch is negative (leaning backward to curl), forwardLeanIntensity is 0.
            float forwardLeanIntensity = systemPitchBlendMax > 0f ? Mathf.Clamp01(systemAppliedSpinePitch / systemPitchBlendMax) : 0f;

            // Blend between a completely unreduced state (1.0) and our raw yaw filter based on forward lean depth
            float activeYawTiltReduction = Mathf.Lerp(1f, rawYawDampenFactor, forwardLeanIntensity);

            // 4. POSTURE BLEND CALCULATIONS
            float absoluteSystemPitch = Mathf.Abs(systemAppliedSpinePitch);
            float horizonLockBlend = Mathf.InverseLerp(systemPitchBlendStart, systemPitchBlendMax, absoluteSystemPitch);

            // Horizon Lock Target Channel
            float horizonPitchCorrection = -systemAppliedSpinePitch * horizonTrackingWeight;

            // Tension Strain Tilt Channel
            float neckTensionPitch = forwardForce * strainBraceWeight * headNeckSensitivity;
            float headTensionPitch = forwardForce * (strainBraceWeight * 1.1f) * headNeckSensitivity;

            // Crossfade Logic Channels A & B based strictly on system spine deflection
            float finalNeckPitch = Mathf.Lerp(neckTensionPitch, horizonPitchCorrection, horizonLockBlend);
            float finalHeadPitch = Mathf.Lerp(headTensionPitch, horizonPitchCorrection, horizonLockBlend);

            // 5. LATERAL OFFSET HANDLING (MULTIPLIED BY SCALE-DAMPENED GAIN)
            float horizonRollCorrection = -systemAppliedSpineRoll * horizonTrackingWeight;
            float nestingRollOffset = -sidewaysForce * (shoulderNestingWeight * 0.4f) * headNeckSensitivity;

            // The total sideways roll is explicitly muted by our dynamic, lean-dependent yaw filter
            float finalNeckRoll = (horizonRollCorrection + nestingRollOffset) * activeYawTiltReduction;

            // 6. APPLY TRANSFORMS ADDITIVELY WITH EFFORT GATING
            if (neckBone != null)
            {
                float neckPitchValue = finalNeckPitch * massEffortFactor;
                float neckRollValue = finalNeckRoll * massEffortFactor;

                Quaternion neckOffset = Quaternion.AngleAxis(neckPitchValue, pitchAxis) *
                                        Quaternion.AngleAxis(0f, yawAxis) *
                                        Quaternion.AngleAxis(neckRollValue, rollAxis); 

                neckBone.localRotation *= neckOffset;
            }

            if (headBone != null)
            {
                float headPitchValue = finalHeadPitch * massEffortFactor;
                float headRollValue = (horizonRollCorrection + (nestingRollOffset * 0.3f)) * activeYawTiltReduction * massEffortFactor;

                Quaternion headOffset = Quaternion.AngleAxis(headPitchValue, pitchAxis) *
                                        Quaternion.AngleAxis(0f, yawAxis) *
                                        Quaternion.AngleAxis(headRollValue, rollAxis);

                headBone.localRotation *= headOffset;
            }
        }
    }
}
