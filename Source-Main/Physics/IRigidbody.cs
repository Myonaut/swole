using Swole.Script;

namespace Swole
{
    public interface IRigidbody : EngineInternal.IComponent
    {
        public EngineInternal.Vector3 velocity { get; set; }
        public EngineInternal.Vector3 angularVelocity { get; set; }

        public float drag { get; set; }
        public float angularDrag { get; set; }
        public float mass { get; set; }
        public bool useGravity { get; set; }

        public float maxDepenetrationVelocity { get; set; }

        public bool isKinematic { get; set; }

        public bool freezeRotation { get; set; }

        public string constraints { get; set; }

        public string collisionDetectionMode { get; set; }

        public EngineInternal.Vector3 centerOfMass { get; set; }
        public EngineInternal.Vector3 worldCenterOfMass { get; }

        public EngineInternal.Quaternion inertiaTensorRotation { get; set; }

        public EngineInternal.Vector3 inertiaTensor { get; set; }

        public bool detectCollisions { get; set; }

        public EngineInternal.Vector3 position { get; set; }

        public EngineInternal.Quaternion rotation { get; set; }

        public string interpolation
        {
            get;
            set;
        }

        public int solverIterations
        {
            get;
            set;
        }

        public float sleepThreshold
        {
            get;
            set;
        }

        public float maxAngularVelocity
        {
            get;
            set;
        }

        public int solverVelocityIterations
        {
            get;
            set;
        }

        public void MovePosition(EngineInternal.Vector3 position);

        public void MoveRotation(EngineInternal.Quaternion rot);

        public EngineInternal.Vector3 GetRelativePointVelocity(EngineInternal.Vector3 relativePoint);

        public EngineInternal.Vector3 GetPointVelocity(EngineInternal.Vector3 worldPoint);

        public void AddForce(EngineInternal.Vector3 force, string mode);

        public void AddForce(EngineInternal.Vector3 force);

        public void AddForce(float x, float y, float z, string mode);

        public void AddForce(float x, float y, float z);

        public void AddRelativeForce(EngineInternal.Vector3 force, string mode);

        public void AddRelativeForce(EngineInternal.Vector3 force);

        public void AddRelativeForce(float x, float y, float z, string mode);

        public void AddRelativeForce(float x, float y, float z);

        public void AddTorque(EngineInternal.Vector3 torque, string mode);

        public void AddTorque(EngineInternal.Vector3 torque);

        public void AddTorque(float x, float y, float z, string mode);

        public void AddTorque(float x, float y, float z);

        public void AddRelativeTorque(EngineInternal.Vector3 torque, string mode);

        public void AddRelativeTorque(EngineInternal.Vector3 torque);

        public void AddRelativeTorque(float x, float y, float z, string mode);

        public void AddRelativeTorque(float x, float y, float z);

        public void AddForceAtPosition(EngineInternal.Vector3 force, EngineInternal.Vector3 position, string mode);

        public void AddForceAtPosition(EngineInternal.Vector3 force, EngineInternal.Vector3 position);

        public void AddExplosionForce(float explosionForce, EngineInternal.Vector3 explosionPosition, float explosionRadius, float upwardsModifier, string mode);

        public void AddExplosionForce(float explosionForce, EngineInternal.Vector3 explosionPosition, float explosionRadius, float upwardsModifier);

        public void AddExplosionForce(float explosionForce, EngineInternal.Vector3 explosionPosition, float explosionRadius);

        public EngineInternal.Vector3 ClosestPointOnBounds(EngineInternal.Vector3 position);

        [SwoleScriptIgnore]
        public bool SweepTest(EngineInternal.Vector3 direction, out IRaycastHit hitInfo, float maxDistance, string queryTriggerInteraction);

        [SwoleScriptIgnore]
        public bool SweepTest(EngineInternal.Vector3 direction, out IRaycastHit hitInfo, float maxDistance);

        [SwoleScriptIgnore]
        public bool SweepTest(EngineInternal.Vector3 direction, out IRaycastHit hitInfo);

        [SwoleScriptIgnore]
        public IRaycastHit[] SweepTestAll(EngineInternal.Vector3 direction, float maxDistance, string queryTriggerInteraction);

        [SwoleScriptIgnore]
        public IRaycastHit[] SweepTestAll(EngineInternal.Vector3 direction, float maxDistance);

        [SwoleScriptIgnore]
        public IRaycastHit[] SweepTestAll(EngineInternal.Vector3 direction);

    }
}
