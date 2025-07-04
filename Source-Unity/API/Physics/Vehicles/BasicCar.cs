#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole.API.Unity
{
    public class BasicCar : MonoBehaviour
    {
        /*
         * Possible improvements:
         * Increase height of raycast start point. If wheel is pushed too far up, apply upward forces to the car body and prevent the wheel from moving up past the threshold.
         * 
         * 
         * 
         * 
         * 
         * 
         */


        protected const float _PI_2 = Mathf.PI * 2f;
        protected const float _RPM_TO_RPS = _PI_2 / 60f;
        protected const float _RPS_TO_RPM = 60f / _PI_2;
        protected const float _HP_TO_WATTS = 745.7f; // 1 HP = 745.7 Watts

        [Range(-1f, 1f)]
        public float inputX;
        [Range(-1f, 1f)]
        public float inputY;
        [Range(-1f, 1f)]
        public float inputThrottle;

        [Range(-1f, 1f)]
        public float turn;
        public float maxTurnDegrees = 35f;
        public float minTurnDegrees = 15f;
        [Tooltip("Top speed used to determine the max steer angle. (Meters per second)")]
        public float topTurnSpeed = 15f;

        [Range(0f, 1f)]
        public float brake = 0f;

        public bool handbrake = false;
        [Range(0f, 1f), Tooltip("Friction multiplier for wheels getting affected by the handbrake.")]
        public float handebrakeFrictionMultiplier = 1f;

        [NonSerialized]
        public bool reverse = false;

        public bool tractionControl = true;
        [Tooltip("how much the traction control will reduce the accelerator input when slip is too high (slip error * strength)")]
        public float tractionControlStrength = 3f;
        public float targetLongitudinalSlip = 0.2f; // target longitudinal slip ratio for traction control
        public float targetPower = 0.7f; 

        [Range(0f, 1f)]
        public float throttle = 0f;
        protected float throttleMod = 0f;
        public float Throttle
        {
            get
            {
                return throttleMod;
            }
        }

        public Vector3 centerOfMass;

        public LayerMask wheelCollidableLayers = ~0;

        public float rollingFriction = 0.1f;

        [Tooltip("Speed at which the car is considered fully stopped. (Prevents endless creeping)")]
        public float fullStopSpeed = 0.05f;
        [Tooltip("Speed at which the car is no longer considered stopped.")]
        public float fullStopEndSpeed = 0.055f;

        [SerializeField]
        protected Rigidbody carBody;
        [NonSerialized]
        protected Transform carTransform;

        //[Tooltip("Maximum amount of torque force that can be applied to the drive wheels.")]
        //public float maxTorque = 1000f;
        public float maxHorsepower = 300f;
        [Tooltip("Fraction of available horsepower (y-axis) (0-1) based on current RPM vs Max RPM (x-axis) (0-1).")]
        public AnimationCurve powerCurve;
        [Tooltip("Fraction of available horsepower (y-axis) (0-1) based on current RPM vs Max RPM (x-axis) (0-1). Will interpolate between the output of this curve and the default power curve based on throttle if provided.")]
        public AnimationCurve powerCurveLowThrottle;
        public float lowThrottleThreshold = 0.2f;
        public float GetEnginePower(float normalizedRPM, float throttle)
        {
            float pwrHigh = powerCurve.Evaluate(normalizedRPM);
            if (powerCurveLowThrottle != null && powerCurveLowThrottle.length > 0) 
            {
                float pwrLow = powerCurveLowThrottle.Evaluate(normalizedRPM);
                return Mathf.Clamp01(Mathf.LerpUnclamped(pwrLow, pwrHigh, (throttle - lowThrottleThreshold) / (1f - lowThrottleThreshold))) * maxHorsepower;
            } 

            return pwrHigh * maxHorsepower * throttle;   
        }

        [Tooltip("Maximum speed the car can reach in meters per second.")]
        public float topSpeed = 11f;

        public float steeringResponse = 500f;

        public float brakeTorque = 500f;

        public float maxRPM = 6000f;
        public float idleRPM = 100f;
        public float RPM;

        protected float averageLongitudinalSlip = 0f;

        [Serializable]
        public class Wheel
        {
            public string name;

            public float radius;
            [NonSerialized]
            public float circumference;

            public float mass = 4.0f;

            [Tooltip("Fraction of available engine torque (0-1) that this wheel can use."), Range(0f, 1f)]
            public float torqueFactor = 1f;

            [Tooltip("Fraction of available brake torque (0-1) that this wheel can use."), Range(0f, 1f)]
            public float brakeFactor = 1f;

            [Tooltip("Fraction of handbrake brake contribution when the handbrake is applied (0-1)"), Range(0f, 1f)]
            public float handbrakeFactor = 0f;

            [Range(0f, 1f)]
            public float turnFactor;
            [NonSerialized]
            public float currentTurnAngle;

            public Vector3 suspensionSpringPivot;
            public Vector3 GetSuspensionSpringPivot() => suspensionSpringPivot + startPosition;
            public float suspensionStrength = 1000f;
            public float suspensionDamping = 15f;

            public Vector3 rollingAxis = Vector3.forward;
            public Vector3 slipAxis = Vector3.right;

            [NonSerialized]
            protected Vector3 suspensionAxis;
            public Vector3 SuspensionAxis => suspensionAxis;
            [NonSerialized]
            protected float suspensionRestLength;
            public float SuspensionRestLength => suspensionRestLength;

            public float suspensionMaxCompression;
            public float suspensionMaxExtension;

            public Transform transform;

            [NonSerialized]
            protected Vector3 startPosition;
            public Vector3 StartPosition => startPosition;

            [NonSerialized]
            protected Quaternion startRotation;
            public Quaternion StartRotation => startRotation;

            [Tooltip("x-axis (0-1) is how much of the wheel's current velocity is aligned with the slip axis. y-axis (0-1) is how much grip the tire will have.")]
            public AnimationCurve gripCurve;

            [NonSerialized]
            public float spin;
            [NonSerialized]
            public float spinSpeed;
            public float spinDrag = 0.1f;

            [NonSerialized]
            public float longitudinalSlip = 0f;
            [NonSerialized]
            public float lateralSlip = 0f;

            [NonSerialized]
            public bool isGrounded;

            public bool lockDifferential;

            //public PhysicMaterial forwardPhysicsMaterial;
            //public PhysicMaterial sidewaysPhysicsMaterial;
            public PhysicMaterial physicsMaterial;

            [Tooltip("Curve for how much longitudinal slip affects the wheel's traction. x-axis (0-1) is the slip ratio, y-axis (0-1) is a multiplier of traction force.")]
            public AnimationCurve longitudinalSlipCurve;
            [Tooltip("Curve for how much lateral slip affects the wheel's resistance to lateral forces. x-axis is the absolute slip angle (in degrees), y-axis (0-1) is a multiplier of lateral force.")]
            public AnimationCurve lateralSlipCurve;
            [Tooltip("Curve for how much longitudinal slip affects the wheel's braking. x-axis (0-1) is the slip ratio, y-axis (0-1) is a multiplier of braking force.")]
            public AnimationCurve brakingSlipCurve;

            [NonSerialized]
            public Vector3 forcesToApplyAtPosition;
            [NonSerialized]
            public Vector3 forcesToApplyAtSuspension;

            public void Initialize(BasicCar car)
            {
                if (transform == null)
                {
                    swole.LogError($"Wheel transform not set for {name}!");
                    return;
                }
                if (gripCurve == null)
                {
                    swole.LogError($"Wheel grip curve not set for {name}!");
                    return;
                }

                circumference = radius * _PI_2;

                transform.GetPositionAndRotation(out var wheelPos, out var wheelRot);

                startPosition = car.carTransform.InverseTransformPoint(wheelPos);
                startRotation = Quaternion.Inverse(car.carTransform.rotation) * wheelRot;

                suspensionAxis = suspensionSpringPivot; 
                suspensionRestLength = suspensionAxis.magnitude; 
                suspensionAxis = suspensionAxis / suspensionRestLength;

                if (longitudinalSlipCurve == null || longitudinalSlipCurve.length <= 0) longitudinalSlipCurve = car.defautLongitudinalSlipCurve;
                if (lateralSlipCurve == null || lateralSlipCurve.length <= 0) lateralSlipCurve = car.defaultLateralSlipCurve;
                if (brakingSlipCurve == null || brakingSlipCurve.length <= 0) brakingSlipCurve = car.defaultBrakingSlipCurve;
            }
        }

        [SerializeField]
        protected Wheel[] wheels;

        [Tooltip("Default curve for how much longitudinal slip affects the wheel's traction. x-axis (0-1) is the slip ratio, y-axis (0-1) is a multiplier of traction force.")]
        public AnimationCurve defautLongitudinalSlipCurve;
        [Tooltip("Default curve for how much lateral slip affects the wheel's resistance to lateral forces. x-axis is the absolute slip angle (in degrees), y-axis (0-1) is a multiplier of lateral force.")]
        public AnimationCurve defaultLateralSlipCurve;
        [Tooltip("Default curve for how much longitudinal slip affects the wheel's braking. x-axis (0-1) is the slip ratio, y-axis (0-1) is a multiplier of braking force. Will use longitudinalSlipCurve if left empty.")]
        public AnimationCurve defaultBrakingSlipCurve;

        public int mainGearStartIndex = 2;
        public float[] gearRatios = new float[] { -3f, 0f, 4.15f, 2.37f, 1.56f, 1.16f, 0.86f, 0.52f }; 
        public float driveRatio = 3.8f;

        public int currentGear;
        protected bool clutchEngaged = true;
        protected float clutch = 1.0f;
        public float gearShiftDelay = 0.1f;

        [Tooltip("How fast the RPM used to check if a gear change is necessary is synced with the actual engine RPM (sync speed is how many RPMs to sync per second)")]
        public float gearRPM_syncSpeed = 1000f;
        [Tooltip("Fraction of max RPM that when exceeded will cause an automatic upwards gear shift.")]
        public float gearShiftUpThreshold = 0.9f;
        [Tooltip("Fraction of max RPM that when not met will cause an automatic downwards gear shift.")]
        public float gearShiftDownThreshold = 0.2f;

        protected float accumRPM;
        protected virtual void TransmissionControl()
        {
            if (reverse)
            {
                if (Throttle > 0f && currentGear != 0)
                {
                    ChangeGear(0);
                }
            }
            else
            {
                if (currentGear < mainGearStartIndex)
                {
                    if (Throttle > 0f) ChangeGear(mainGearStartIndex);
                }
                else
                {
                    accumRPM = Mathf.MoveTowards(accumRPM, RPM, Time.fixedDeltaTime * gearRPM_syncSpeed);
                    if (currentGear >= mainGearStartIndex && accumRPM > maxRPM * gearShiftUpThreshold)
                    {
                        accumRPM = maxRPM * 0.5f;
                        ShiftUp();
                    }
                    else if (currentGear > mainGearStartIndex && accumRPM < maxRPM * gearShiftDownThreshold)
                    {
                        accumRPM = maxRPM * 0.5f;
                        ShiftDown();
                    }
                }
            }

            clutch = Mathf.MoveTowards(clutch, clutchEngaged ? 1f : 0f, Time.fixedDeltaTime * 100f);
        }

        public void ShiftUp(int amount = 1) => ChangeGear(currentGear + amount);
        public void ShiftDown(int amount = 1) => ChangeGear(currentGear - amount);
        public void ChangeGear(int gearIndex)
        {
            if (clutchEngaged) StartCoroutine(ChangeGearRoutine(gearIndex));
        }
        protected IEnumerator ChangeGearRoutine(int gearIndex)
        {
            if (gearIndex < 0 || gearIndex >= gearRatios.Length)
            {
                yield break; // invalid gear index
            }

            throttle = 0f;
            throttleMod = 0f;
            clutchEngaged = false; // disengage clutch
            yield return new WaitForSeconds(gearShiftDelay); // wait for clutch to disengage
            currentGear = gearIndex;
            clutchEngaged = true;
        }

        protected void RunVehicleSystems(float deltaTime)
        {
            if (tractionControl)
            {
                float slipError = targetLongitudinalSlip - averageLongitudinalSlip;
                float correction = Math.Min(0, slipError * tractionControlStrength);
                /*if (correction >= 0)
                {
                    if (slipError > 0f) correction = Math.Min(0, targetPower - (RPM / maxRPM)) * slipError * tractionControlStrength; 
                }*/

                throttleMod = Mathf.Clamp01(Mathf.MoveTowards(throttleMod, throttle + correction, deltaTime * 10f));  
                //Debug.Log("TC " + acceleratorMod + " : " + averageLongitudinalSlip);
            }
            else
            {
                throttleMod = throttle;
            }
        }

#if UNITY_EDITOR
        protected virtual void OnDrawGizmos()
        {
            if (wheels == null || carBody == null) return;

            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(carBody.transform.TransformPoint(centerOfMass), 0.1f);

            var carTransform = carBody.transform;
            foreach (var wheel in wheels)
            {
                if (wheel.transform == null) return;

                var pos = wheel.transform.position;
                var susPos = carTransform.TransformVector(wheel.suspensionSpringPivot) + pos;
                var susDir = (susPos - pos).normalized;

                Gizmos.color = Color.green;
                Gizmos.DrawLine(pos, susPos); 

                Gizmos.color = Color.blue;
                Gizmos.DrawRay(pos, carTransform.TransformDirection(wheel.rollingAxis));

                Gizmos.color = Color.red;
                Gizmos.DrawRay(pos, carTransform.TransformDirection(wheel.slipAxis));

                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(pos, wheel.radius);

                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(pos, susDir * wheel.suspensionMaxCompression);
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(pos, susDir * -wheel.suspensionMaxExtension); 
            }

        }
#endif

        [NonSerialized]
        protected float carSpeed;
        public float CarSpeed => carSpeed;

        [NonSerialized]
        protected Vector3 carVelocity;
        public Vector3 CarVelocity => carVelocity;

        [NonSerialized]
        protected float carDriveSpeed;
        public float CarDriveSpeed => carDriveSpeed;

        protected float totalWheelMass = 0f;
        protected float totalWheelTorqueFactor = 0f;
        public void InitializeWheels()
        {
            totalWheelMass = 0f;
            totalWheelTorqueFactor = 0f;

            if (wheels != null)
            {
                foreach (var wheel in wheels)
                {
                    wheel.Initialize(this);
                    totalWheelMass += wheel.mass;
                    totalWheelTorqueFactor += wheel.torqueFactor;

                    //if (wheel.forwardPhysicsMaterial == null) wheel.forwardPhysicsMaterial = DefaultForwardWheelPhysicsMaterial;
                    //if (wheel.sidewaysPhysicsMaterial == null) wheel.sidewaysPhysicsMaterial = DefaultSidewaysWheelPhysicsMaterial;
                    if (wheel.physicsMaterial == null) wheel.physicsMaterial = DefaultForwardWheelPhysicsMaterial;
                }
            }
        }

        private static PhysicMaterial defaultForwardWheelPhysicsMaterial;
        public static PhysicMaterial DefaultForwardWheelPhysicsMaterial
        {
            get
            {
                if (defaultForwardWheelPhysicsMaterial == null)
                {
                    defaultForwardWheelPhysicsMaterial = new PhysicMaterial("DefaultForwardWheelPhysicsMaterial")
                    {
                        dynamicFriction = 0.95f,
                        staticFriction = 0.95f,
                        bounciness = 0f,
                        frictionCombine = PhysicMaterialCombine.Multiply,
                        bounceCombine = PhysicMaterialCombine.Multiply
                    };
                }

                return defaultForwardWheelPhysicsMaterial;
            }
        }
        private static PhysicMaterial defaultSidewaysWheelPhysicsMaterial;
        public static PhysicMaterial DefaultSidewaysWheelPhysicsMaterial
        {
            get
            {
                if (defaultSidewaysWheelPhysicsMaterial == null)
                {
                    defaultSidewaysWheelPhysicsMaterial = new PhysicMaterial("DefaultSidewaysWheelPhysicsMaterial")
                    {
                        dynamicFriction = 0.5f,
                        staticFriction = 0.95f,

                        bounciness = 0f,
                        frictionCombine = PhysicMaterialCombine.Multiply,
                        bounceCombine = PhysicMaterialCombine.Multiply
                    };
                }

                return defaultSidewaysWheelPhysicsMaterial;
            }
        }
        protected virtual void Awake()
        {
            if (carBody == null) carBody = gameObject.GetComponent<Rigidbody>();
            if (carBody == null)
            {
                enabled = false;
                swole.LogError("BasicCar requires a reference to a Rigidbody component. Disabling script.");
                return;
            }

            carBody.useGravity = false;
            carBody.automaticCenterOfMass = false;
            carBody.centerOfMass = centerOfMass; 

            carTransform = carBody.transform;
            InitializeWheels();
        }

        protected void Update()
        {
            inputX = InputProxy.MoveAxisX;
            inputY = InputProxy.MoveAxisY;

            inputThrottle = InputProxy.Driving_ThrottleForward - InputProxy.Driving_BrakeThrottleReverse;
            if (inputThrottle < 0f)
            {
                if (carDriveSpeed < 0.1f)
                {
                    reverse = true;
                    brake = Mathf.MoveTowards(brake, 0f, Time.deltaTime * 10f);
                } 
                else
                {
                    reverse = false;
                    brake = Mathf.MoveTowards(brake, -inputThrottle, Time.deltaTime * 10f); 
                }
            } 
            else
            {
                reverse = false;
                brake = Mathf.MoveTowards(brake, 0f, Time.deltaTime * 10f);
            }

            handbrake = InputProxy.JumpButton;
            turn = InputProxy.Driving_SteeringAxis;
            throttle = Mathf.MoveTowards(throttle, Mathf.Abs(inputThrottle), Time.deltaTime);
        }

        private bool grounded;
        private bool hasTraction;
        private Vector3 groundNormal;
        private Vector3 stopPos;
        private Quaternion stopRot;
        private bool stopped = false;
        protected virtual void FixedUpdate()
        {
            float orientation = (Vector3.Dot(carBody.rotation * Vector3.up, Vector3.up) + 1f) * 0.5f;
            if (!grounded || orientation < 0.75f)
            {
                float mul = grounded ? (1f - (Mathf.Max(0f, orientation - 0.5f)) / 0.25f) : 1f;  
                carBody.AddTorque(((((Vector3.SlerpUnclamped(carBody.rotation * Vector3.back, Vector3.up, orientation)) * inputX) + (carBody.rotation * Vector3.right * inputY)) * 2f * mul) / Mathf.Max(1f, carBody.angularVelocity.sqrMagnitude), ForceMode.Acceleration);
            }

            TransmissionControl();
            //currentGear = 2;

            Vector3 gravity = Physics.gravity;
            float gravityMag = gravity.magnitude;
            var gravityDir = gravityMag > 0f ? gravity / gravityMag : gravity;

            carVelocity = carBody.velocity;
            carSpeed = carVelocity.magnitude;

            carDriveSpeed = Vector3.Dot(carBody.rotation * Vector3.forward, carVelocity);

            CalculateWheelForces(gravityMag, gravityDir, Time.fixedDeltaTime);

            RunVehicleSystems(Time.fixedDeltaTime);

            /*if (stopped)
            {
                if (!grounded) 
                { 
                    stopped = false; 
                }
                else
                {

                    if (carSpeed > fullStopEndSpeed)
                    {
                        stopped = false;
                    }
                    else
                    {
                        carBody.Move(new Vector3(stopPos.x, transform.position.y, stopPos.z), stopRot);
                    }
                }
            } 
            else
            {
                if (carSpeed < fullStopSpeed)
                {
                    stopPos = carTransform.position;  
                    stopRot = carTransform.rotation;
                    stopped = true;
                }
            }
            */
            

            carVelocity = carBody.velocity;
            
            /*if (grounded)
            {
                float speed = carVelocity.magnitude;
                var gravNerf = Vector3.ProjectOnPlane(gravity, groundNormal) / Mathf.Max(1f, speed * 100);

                gravity = gravity - gravNerf * Mathf.Max(Vector3.Dot(groundNormal, -gravityDir)); 
            }*/
            carVelocity = carVelocity + gravity * Time.fixedDeltaTime; 
            carBody.velocity = carVelocity;
        }

        [Range(0f, 0.2f)]
        public float weakVelocityNerf = 0.05f;
        protected virtual Vector3 NerfWeakVelocity(Vector3 velocity)
        {
            float a = 1f - weakVelocityNerf;
            velocity.x = (Mathf.Max(0, Mathf.Abs(velocity.x) - weakVelocityNerf) / a) * Mathf.Sign(velocity.x);
            velocity.y = (Mathf.Max(0, Mathf.Abs(velocity.y) - weakVelocityNerf) / a) * Mathf.Sign(velocity.y);
            velocity.z = (Mathf.Max(0, Mathf.Abs(velocity.z) - weakVelocityNerf) / a) * Mathf.Sign(velocity.z); 

            return velocity;
        }
        private readonly List<Wheel> temp_wheels = new List<Wheel>();
        protected virtual void CalculateWheelForces(float gravityMagnitude, Vector3 gravityDir, float deltaTime)
        {
            if (wheels == null) return;

            float gas = Throttle;

            var gravity = gravityDir * gravityMagnitude;

            float gearRatio = gearRatios[currentGear];
            float fullGearRatio = driveRatio * gearRatio;
            float fullGearRatioABS = Mathf.Abs(fullGearRatio);

            float engineHorsePower = 0f;
            float engineTorque = 0f;
            float engineToWheelTorque = 0f;
            float normalizedRPM = Mathf.Clamp01(RPM / maxRPM);
            if (normalizedRPM < 1f)
            {
                engineHorsePower = GetEnginePower(normalizedRPM, Throttle);
                //engineTorque = ((engineHorsePower * 5252f) / RPM); // this is for ft-lb...
                engineTorque = ((_HP_TO_WATTS * engineHorsePower) * 60f) / (_PI_2 * RPM); // HP to Torque (Nm)
                engineToWheelTorque = engineTorque * fullGearRatio * clutch;// * gas; // GetEnginePower handles throttle already
            }

            float engineTargetRPM = Mathf.LerpUnclamped(idleRPM, maxRPM + UnityEngine.Random.Range(-25f, 75f), gas); 
            float engineTargetRPS = engineTargetRPM * _RPM_TO_RPS;
            RPM = Mathf.MoveTowards(RPM, engineTargetRPM, deltaTime * 10000f);  
            bool wheelRPM_flag = false;

            temp_wheels.Clear();
            averageLongitudinalSlip = 0;
            groundNormal = Vector3.zero;
            grounded = false;
            hasTraction = false;
            int groundedDriveWheels = 0;
            int groundedWheels = 0;
            float totalMass = carBody.mass + totalWheelMass;
            foreach (var wheel in wheels)
            {
                wheel.forcesToApplyAtPosition = Vector3.zero;
                wheel.forcesToApplyAtSuspension = Vector3.zero;  

                wheel.currentTurnAngle = Mathf.MoveTowards(wheel.currentTurnAngle, Mathf.Lerp(maxTurnDegrees, minTurnDegrees, carSpeed / topTurnSpeed) * wheel.turnFactor * turn, Time.fixedDeltaTime * steeringResponse);
                Vector3 turnEuler = (wheel.SuspensionAxis * wheel.currentTurnAngle); 
                Quaternion turnEulerRot = Quaternion.Euler(turnEuler);
                Quaternion wheelTurnRot = turnEulerRot * wheel.StartRotation;
                Quaternion wheelLocalRot = Quaternion.Euler(turnEuler + (wheel.slipAxis * wheel.spin * Mathf.Rad2Deg));  
                Quaternion wheelRot = carTransform.rotation * wheelLocalRot;
                wheel.transform.rotation = wheelRot;

                var suspensionPivot = wheel.GetSuspensionSpringPivot();
                var suspensionPosWorld = carTransform.TransformPoint(suspensionPivot);
                var suspensionAxisWorld = carTransform.TransformDirection(wheel.SuspensionAxis);
                var wheelPosWorld = wheel.transform.position;
                var offsetWorld = wheelPosWorld - suspensionPosWorld;

                if (Physics.Raycast(suspensionPosWorld, -suspensionAxisWorld, out RaycastHit hit, wheel.SuspensionRestLength + wheel.radius, wheelCollidableLayers, QueryTriggerInteraction.Ignore))
                {
                    grounded = true;
                    wheel.isGrounded = true;
                    if (wheel.torqueFactor > 0f) 
                    { 
                        hasTraction = true; 
                        groundedDriveWheels++;
                    }
                    groundedWheels++;

                    //float gravityAlignment = Mathf.Abs(Vector3.Dot(gravityDir, hit.normal));

                    float massRatio = wheel.mass / totalWheelMass;
                    float carMassPortion = carBody.mass * massRatio;


                    bool isDriveWheel = wheel.torqueFactor > 0f;
                    float appliedHandbrake = (handbrake ? wheel.handbrakeFactor : 0f);
                    float appliedBrake = Mathf.Max(brake, appliedHandbrake);
                    float braking = Mathf.Clamp01(appliedBrake * wheel.brakeFactor);


                    float frictionDynamic = 1f;
                    float frictionStatic = 1f;
                    if (hit.collider.sharedMaterial != null)
                    {
                        frictionDynamic = hit.collider.sharedMaterial.dynamicFriction;
                        frictionStatic = hit.collider.sharedMaterial.staticFriction;
                    }
                    frictionDynamic = frictionDynamic * wheel.physicsMaterial.dynamicFriction;
                    frictionStatic = frictionStatic * wheel.physicsMaterial.staticFriction;

                    frictionDynamic = Mathf.LerpUnclamped(frictionDynamic, frictionDynamic * handebrakeFrictionMultiplier, appliedHandbrake);
                    frictionStatic = Mathf.LerpUnclamped(frictionStatic, frictionStatic * handebrakeFrictionMultiplier, appliedHandbrake);

                    float frictionForceStatic = Maths.CalculateFrictionForce(carBody.mass + wheel.mass, gravity, hit.normal/*suspensionAxisWorld use suspension axis for more stable results*/, frictionStatic);



                    float offset = wheel.SuspensionRestLength - (hit.distance - wheel.radius);
                    wheel.transform.position = carTransform.TransformPoint(wheel.StartPosition + wheel.SuspensionAxis * Mathf.Max(offset, -wheel.suspensionMaxCompression));

                    var wheelVelocity = carBody.GetPointVelocity(wheelPosWorld);
                    var wheelSpeed = wheelVelocity.magnitude;
                    var wheelVelocityDir = Vector3.zero;
                    if (wheelSpeed > 0f) wheelVelocityDir = wheelVelocity / wheelSpeed;

                    var wheelVelocityPlanar = Vector3.ProjectOnPlane(wheelVelocity, suspensionAxisWorld/*hit.normal use suspension axis for more stable results*/);
                    float wheelSpeedPlanar = wheelVelocityPlanar.magnitude;
                    var wheelVelocityDirPlanar = wheelSpeedPlanar > 0f ? wheelVelocityPlanar / wheelSpeedPlanar : Vector3.zero;
                    float planarForce = (carBody.mass + wheel.mass) * (wheelSpeedPlanar / deltaTime);

                    float suspensionStrength = offset * wheel.suspensionStrength;
                    float suspensionDamping = (wheelSpeed > 0f ? Vector3.Dot(wheelVelocity, suspensionAxisWorld) : 0f) * wheel.suspensionDamping;
                    float suspensionForce = (suspensionStrength - suspensionDamping/*Mathf.Clamp(suspensionDamping, 0, suspensionStrength)*/);

                    //Debug.DrawRay(suspensionPosWorld, suspensionAxisWorld * suspensionForce, Color.yellow);


                    Vector3 rollingAxisWorld = carTransform.TransformDirection(turnEulerRot * wheel.rollingAxis);
                    float rollingSpeed = wheelSpeed > 0f ? Vector3.Dot(wheelVelocity, rollingAxisWorld) : 0f;
                    float rollingAngularSpeed = rollingSpeed / wheel.radius;
                    float rollingForce = (carBody.mass + wheel.mass) * (rollingSpeed / deltaTime);
                    float rollingTorque = rollingForce * wheel.radius;

                    float rollingFrictionForce = frictionForceStatic * rollingFriction;
                    float generalFrictionForce = frictionForceStatic * rollingFriction * 0.25f;
                    Debug.DrawRay(wheelPosWorld, rollingAxisWorld * Mathf.Min(rollingFrictionForce, Mathf.Abs(rollingForce)) * Mathf.Sign(-rollingForce), Color.red);
                    if (rollingForce != 0f) carBody.AddForceAtPosition(rollingAxisWorld * Mathf.Min(rollingFrictionForce, Mathf.Abs(rollingForce)) * Mathf.Sign(-rollingForce), hit.point, ForceMode.Force); // apply rolling resistance forces
                    if (planarForce != 0f) carBody.AddForceAtPosition(wheelVelocityDirPlanar * -Mathf.Min(generalFrictionForce, planarForce), hit.point, ForceMode.Force); // apply a small general resistance force
                    if (suspensionForce != 0f) carBody.AddForceAtPosition(suspensionAxisWorld * suspensionForce, suspensionPosWorld, ForceMode.Force);

                     

                    wheelVelocity = NerfWeakVelocity(carBody.GetPointVelocity(wheelPosWorld));
                    wheelSpeed = wheelVelocity.magnitude;
                    wheelVelocityDir = Vector3.zero;
                    if (wheelSpeed > 0f) wheelVelocityDir = wheelVelocity / wheelSpeed;

                    wheelVelocityPlanar = Vector3.ProjectOnPlane(wheelVelocity, suspensionAxisWorld/*hit.normal use suspension axis for more stable results*/);
                    wheelSpeedPlanar = wheelVelocityPlanar.magnitude;

                    if (wheelSpeedPlanar > 0.3f) // only apply lateral forces when significant forces are being applied to the wheel
                    {
                        wheelSpeedPlanar = (wheelSpeedPlanar - 0.3f) / (1f - 0.3f); 
                        planarForce = (carBody.mass + wheel.mass) * (wheelSpeedPlanar / deltaTime);

                        Vector3 lateralAxis = carTransform.TransformDirection(turnEulerRot * wheel.slipAxis);
                        float slipAngle = Vector3.SignedAngle(rollingAxisWorld, wheelVelocityDir, -suspensionAxisWorld); 

                        carBody.AddForceAtPosition(lateralAxis * Mathf.Min(planarForce, frictionForceStatic) * wheel.lateralSlipCurve.Evaluate(Mathf.Abs(slipAngle)) * Mathf.Sign(slipAngle), ((wheelPosWorld * 0.8f) + (hit.point * 0.2f)), ForceMode.Force);
                        Debug.DrawRay(wheelPosWorld, lateralAxis * Mathf.Min(planarForce, frictionForceStatic) * wheel.lateralSlipCurve.Evaluate(Mathf.Abs(slipAngle)) * Mathf.Sign(slipAngle), Color.yellow);
                    }

                    float torque = ((wheel.torqueFactor * engineToWheelTorque) * (1f - appliedBrake)) - (Mathf.Min(Mathf.Abs(rollingTorque), brakeTorque) * Mathf.Sign(rollingTorque) * braking); 
                    if (Mathf.Abs(torque) > 0.001f)
                    {
                        wheel.spinSpeed = Mathf.MoveTowards(wheel.spinSpeed, (isDriveWheel ? Mathf.LerpUnclamped(rollingAngularSpeed, wheel.torqueFactor * (engineTargetRPS / fullGearRatio), clutch * gas) : rollingAngularSpeed) * (1f - appliedBrake), deltaTime * 200f);

                        float frictionForceDynamic = Maths.CalculateFrictionForce(carBody.mass + wheel.mass, gravity, suspensionAxisWorld/*hit.normal use suspension axis for more stable results*/, frictionDynamic);
                        float tractionForce = torque / wheel.radius; // to linear force (torque is force * distance)

                        float tractionForceSign = Mathf.Sign(tractionForce);
                        float rollingForceSign = Mathf.Sign(rollingForce);

                        wheel.longitudinalSlip = rollingAngularSpeed != 0f ? ((wheel.spinSpeed - rollingAngularSpeed) / rollingAngularSpeed) : 0f;
                        averageLongitudinalSlip += Mathf.Abs(wheel.longitudinalSlip);
                        if (tractionForceSign != rollingForceSign) // braking effect
                        {
                            var brakingSlipCurve = wheel.brakingSlipCurve != null && wheel.brakingSlipCurve.length > 0 ? wheel.brakingSlipCurve : wheel.longitudinalSlipCurve;
                            tractionForce = (Mathf.Min(Mathf.Abs(tractionForce), frictionForceDynamic) * tractionForceSign * brakingSlipCurve.Evaluate(Mathf.Abs(wheel.longitudinalSlip))); 
                            //Debug.Log(wheel.name + " braking: " + wheel.longitudinalSlip + " : " + brakingSlipCurve.Evaluate(-wheel.longitudinalSlip) + " : " + tractionForce + " : " + (wheel.brakingSlipCurve == null || wheel.brakingSlipCurve.length <= 0));
                            Debug.DrawRay(wheelPosWorld, rollingAxisWorld * tractionForce, Color.magenta);
                        }
                        else
                        {
                            tractionForce = Mathf.Min(tractionForce * wheel.longitudinalSlipCurve.Evaluate(Mathf.Abs(wheel.longitudinalSlip)), frictionForceDynamic);
                            //Debug.Log(wheel.name + " accelerating: " + wheel.longitudinalSlip + " : " + wheel.longitudinalSlipCurve.Evaluate(wheel.longitudinalSlip) + " : " + tractionForce);
                        }

                        if (tractionForce != 0f) carBody.AddForceAtPosition(rollingAxisWorld * tractionForce, wheelPosWorld, ForceMode.Force);
                    }
                    else
                    {
                        wheel.spinSpeed = rollingAngularSpeed;
                    }
                    
                    wheel.spin += wheel.spinSpeed * deltaTime;

                    if (wheel.torqueFactor > 0f) // is drive wheel
                    {
                        if (!wheelRPM_flag)
                        {
                            RPM = 0;
                            wheelRPM_flag = true;
                        }

                        //RPM += Mathf.Abs(wheel.spinSpeed) * _RPS_TO_RPM * (wheel.torqueFactor / totalWheelTorqueFactor) * fullGearRatioABS; 
                        RPM = Mathf.Max(RPM, (Mathf.Abs(wheel.spinSpeed) / wheel.torqueFactor) * _RPS_TO_RPM * fullGearRatioABS);
                    }
                }
                else
                {
                    wheel.isGrounded = false;

                    if (wheel.torqueFactor > 0f)
                    {
                        temp_wheels.Add(wheel); // defer wheel speed calculation for airborne drive wheels until after engine RPM is known
                    } 
                    else
                    {
                        wheel.spinSpeed = wheel.spinSpeed - (wheel.spinSpeed * wheel.spinDrag * deltaTime); 
                        wheel.spin += wheel.spinSpeed * deltaTime;
                    }

                    wheel.transform.position = Vector3.MoveTowards(wheel.transform.position, carTransform.TransformPoint(wheel.StartPosition), deltaTime * 6f); 
                }
            }

            if (groundedDriveWheels > 0) averageLongitudinalSlip = averageLongitudinalSlip / groundedDriveWheels;

            RPM = Mathf.Clamp(RPM, idleRPM, maxRPM);

            float wheelRPM = RPM / fullGearRatio;
            foreach (var wheel in temp_wheels)
            {
                wheel.spinSpeed = wheelRPM * wheel.torqueFactor * _RPM_TO_RPS;
                wheel.spin += wheel.spinSpeed * deltaTime; 
            }
            
            /*foreach (var wheel in wheels)
            {
                var suspensionPivot = wheel.GetSuspensionSpringPivot();
                var suspensionPosWorld = carTransform.TransformPoint(suspensionPivot);
                var wheelPosWorld = wheel.transform.position;

                if (wheel.torqueFactor > 0f)
                {
                    wheel.spinSpeed = wheelRPM * wheel.torqueFactor * _RPM_TO_RPS;
                    wheel.spin += wheel.spinSpeed * deltaTime;
                }

                carBody.AddForceAtPosition(wheel.forcesToApplyAtSuspension, suspensionPosWorld, ForceMode.Force);
                carBody.AddForceAtPosition(wheel.forcesToApplyAtPosition, wheelPosWorld, ForceMode.Force); 
            }*/

            groundNormal = groundNormal.normalized;
        }
    }
}

#endif