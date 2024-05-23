#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Swole.Script;

namespace Swole.API.Unity
{
    public interface IRigidbodyProxy : IRigidbody
    {
        public Rigidbody Rigidbody { get; set; }
    }

    [Serializable]
    public struct RigidbodyProxy : IRigidbodyProxy
    {

        public Type EngineComponentType => typeof(RigidbodyProxy);

        #region IEngineObject

        public string name
        {
            get
            {
                if (rigidbody == null) return default;
                return rigidbody.name;
            }
            set
            {
                if (rigidbody == null) return;
                rigidbody.name = value;
            }
        }

        public EngineInternal.GameObject baseGameObject
        {
            get
            {
                if (rigidbody == null) return default;
                return UnityEngineHook.AsSwoleGameObject(rigidbody.gameObject);
            }
        }

        public object Instance => this;

        public int InstanceID
        {
            get
            {
                if (rigidbody == null) return default;
                return rigidbody.GetInstanceID();
            }
        }

        public bool IsDestroyed => rigidbody == null;

        public bool HasEventHandler => false;

        public IRuntimeEventHandler EventHandler => null;

        public void Destroy(float timeDelay = 0) => swole.Engine.Object_Destroy(rigidbody, timeDelay);

        public void AdminDestroy(float timeDelay = 0) => swole.Engine.Object_AdminDestroy(rigidbody, timeDelay);

        #endregion

        [SwoleScriptIgnore]
        public Rigidbody rigidbody;
        [SwoleScriptIgnore]
        public Rigidbody Rigidbody
        {
            get => rigidbody;
            set => rigidbody = value;
        }

        public RigidbodyProxy(Rigidbody rigidbody)
        {
            this.rigidbody = rigidbody;
        }

        public static bool operator ==(RigidbodyProxy lhs, object rhs)
        {
            if (ReferenceEquals(rhs, null)) return lhs.rigidbody == null;
            if (rhs is RigidbodyProxy ts) return lhs.rigidbody == ts.rigidbody;
            return ReferenceEquals(lhs.rigidbody, rhs);
        }
        public static bool operator !=(RigidbodyProxy lhs, object rhs) => !(lhs == rhs);

        public override bool Equals(object obj)
        {
            return rigidbody == null ? obj == null : rigidbody.Equals(obj);
        }
        public override int GetHashCode()
        {

            return rigidbody == null ? base.GetHashCode() : rigidbody.GetHashCode();
        }

        #region IRigidbody

        public EngineInternal.Vector3 velocity 
        {
            get 
            {
                if (rigidbody == null) return default;
                return UnityEngineHook.AsSwoleVector(rigidbody.velocity);
            }         
            set
            {
                if (rigidbody == null) return;
                rigidbody.velocity = UnityEngineHook.AsUnityVector(value);
            }    
        }
        public EngineInternal.Vector3 angularVelocity
        {
            get
            {
                if (rigidbody == null) return default;
                return UnityEngineHook.AsSwoleVector(rigidbody.angularVelocity);
            }
            set
            {
                if (rigidbody == null) return;
                rigidbody.angularVelocity = UnityEngineHook.AsUnityVector(value);
            }
        }
        public float drag
        {
            get
            {
                if (rigidbody == null) return default;
                return rigidbody.drag;
            }
            set
            {
                if (rigidbody == null) return;
                rigidbody.drag = value;
            }
        }
        public float angularDrag
        {
            get
            {
                if (rigidbody == null) return default;
                return rigidbody.angularDrag;
            }
            set
            {
                if (rigidbody == null) return;
                rigidbody.angularDrag = value;
            }
        }
        public float mass
        {
            get
            {
                if (rigidbody == null) return default;
                return rigidbody.mass;
            }
            set
            {
                if (rigidbody == null) return;
                rigidbody.mass = value;
            }
        }
        public bool useGravity
        {
            get
            {
                if (rigidbody == null) return default;
                return rigidbody.useGravity;
            }
            set
            {
                if (rigidbody == null) return;
                rigidbody.useGravity = value;
            }
        }
        public float maxDepenetrationVelocity
        {
            get
            {
                if (rigidbody == null) return default;
                return rigidbody.maxDepenetrationVelocity;
            }
            set
            {
                if (rigidbody == null) return;
                rigidbody.maxDepenetrationVelocity = value;
            }
        }
        public bool isKinematic
        {
            get
            {
                if (rigidbody == null) return default;
                return rigidbody.isKinematic;
            }
            set
            {
                if (rigidbody == null) return;
                rigidbody.isKinematic = value;
            }
        }
        public bool freezeRotation
        {
            get
            {
                if (rigidbody == null) return default;
                return rigidbody.freezeRotation;
            }
            set
            {
                if (rigidbody == null) return;
                rigidbody.freezeRotation = value;
            }
        }
        public string constraints
        {
            get
            {
                if (rigidbody == null) return default;
                return rigidbody.constraints.ToString();
            }
            set
            {
                if (rigidbody == null) return;
                rigidbody.constraints = Enum.Parse<RigidbodyConstraints>(value);
            }
        }
        public string collisionDetectionMode
        {
            get
            {
                if (rigidbody == null) return default;
                return rigidbody.collisionDetectionMode.ToString();
            }
            set
            {
                if (rigidbody == null) return;
                rigidbody.collisionDetectionMode = Enum.Parse<CollisionDetectionMode>(value);
            }
        }
        public EngineInternal.Vector3 centerOfMass
        {
            get
            {
                if (rigidbody == null) return default;
                return UnityEngineHook.AsSwoleVector(rigidbody.centerOfMass);
            }
            set
            {
                if (rigidbody == null) return;
                rigidbody.centerOfMass = UnityEngineHook.AsUnityVector(value);
            }
        }

        public EngineInternal.Vector3 worldCenterOfMass => rigidbody == null ? default : UnityEngineHook.AsSwoleVector(rigidbody.worldCenterOfMass);

        public EngineInternal.Quaternion inertiaTensorRotation
        {
            get
            {
                if (rigidbody == null) return default;
                return UnityEngineHook.AsSwoleQuaternion(rigidbody.inertiaTensorRotation);
            }
            set
            {
                if (rigidbody == null) return;
                rigidbody.inertiaTensorRotation = UnityEngineHook.AsUnityQuaternion(value);
            }
        }
        public EngineInternal.Vector3 inertiaTensor
        {
            get
            {
                if (rigidbody == null) return default;
                return UnityEngineHook.AsSwoleVector(rigidbody.inertiaTensor);
            }
            set
            {
                if (rigidbody == null) return;
                rigidbody.inertiaTensor = UnityEngineHook.AsUnityVector(value);
            }
        }
        public bool detectCollisions
        {
            get
            {
                if (rigidbody == null) return default;
                return rigidbody.detectCollisions;
            }
            set
            {
                if (rigidbody == null) return;
                rigidbody.detectCollisions = value;
            }
        }
        public EngineInternal.Vector3 position
        {
            get
            {
                if (rigidbody == null) return default;
                return UnityEngineHook.AsSwoleVector(rigidbody.position);
            }
            set
            {
                if (rigidbody == null) return;
                rigidbody.position = UnityEngineHook.AsUnityVector(value);
            }
        }
        public EngineInternal.Quaternion rotation
        {
            get
            {
                if (rigidbody == null) return default;
                return UnityEngineHook.AsSwoleQuaternion(rigidbody.rotation);
            }
            set
            {
                if (rigidbody == null) return;
                rigidbody.rotation = UnityEngineHook.AsUnityQuaternion(value);
            }
        }
        public string interpolation
        {
            get
            {
                if (rigidbody == null) return default;
                return rigidbody.interpolation.ToString();
            }
            set
            {
                if (rigidbody == null) return;
                rigidbody.interpolation = Enum.Parse<RigidbodyInterpolation>(value);
            }
        }
        public int solverIterations
        {
            get
            {
                if (rigidbody == null) return default;
                return rigidbody.solverIterations;
            }
            set
            {
                if (rigidbody == null) return;
                rigidbody.solverIterations = value;
            }
        }
        public float sleepThreshold
        {
            get
            {
                if (rigidbody == null) return default;
                return rigidbody.sleepThreshold;
            }
            set
            {
                if (rigidbody == null) return;
                rigidbody.sleepThreshold = value;
            }
        }
        public float maxAngularVelocity
        {
            get
            {
                if (rigidbody == null) return default;
                return rigidbody.maxAngularVelocity;
            }
            set
            {
                if (rigidbody == null) return;
                rigidbody.maxAngularVelocity = value;
            }
        }
        public int solverVelocityIterations
        {
            get
            {
                if (rigidbody == null) return default;
                return rigidbody.solverVelocityIterations;
            }
            set
            {
                if (rigidbody == null) return;
                rigidbody.solverVelocityIterations = value;
            }
        }

        public void MovePosition(EngineInternal.Vector3 position)
        {
            if (rigidbody == null) return;
            rigidbody.MovePosition(UnityEngineHook.AsUnityVector(position));
        }

        public void MoveRotation(EngineInternal.Quaternion rot)
        {
            if (rigidbody == null) return;
            rigidbody.MoveRotation(UnityEngineHook.AsUnityQuaternion(rot));
        }

        public EngineInternal.Vector3 GetRelativePointVelocity(EngineInternal.Vector3 relativePoint)
        {
            if (rigidbody == null) return default;
            return UnityEngineHook.AsSwoleVector(rigidbody.GetRelativePointVelocity(UnityEngineHook.AsUnityVector(relativePoint)));
        }

        public EngineInternal.Vector3 GetPointVelocity(EngineInternal.Vector3 worldPoint)
        {
            if (rigidbody == null) return default;
            return UnityEngineHook.AsSwoleVector(rigidbody.GetPointVelocity(UnityEngineHook.AsUnityVector(worldPoint)));
        }

        public void AddForce(EngineInternal.Vector3 force, string mode)
        {
            if (rigidbody == null) return;
            rigidbody.AddForce(UnityEngineHook.AsUnityVector(force), Enum.Parse<ForceMode>(mode));
        }

        public void AddForce(EngineInternal.Vector3 force)
        {
            if (rigidbody == null) return;
            rigidbody.AddForce(UnityEngineHook.AsUnityVector(force));
        }

        public void AddForce(float x, float y, float z, string mode)
        {
            if (rigidbody == null) return;
            rigidbody.AddForce(x, y, z, Enum.Parse<ForceMode>(mode));
        }

        public void AddForce(float x, float y, float z)
        {
            if (rigidbody == null) return;
            rigidbody.AddForce(x, y, z);
        }

        public void AddRelativeForce(EngineInternal.Vector3 force, string mode)
        {
            if (rigidbody == null) return;
            rigidbody.AddRelativeForce(UnityEngineHook.AsUnityVector(force), Enum.Parse<ForceMode>(mode));
        }

        public void AddRelativeForce(EngineInternal.Vector3 force)
        {
            if (rigidbody == null) return;
            rigidbody.AddRelativeForce(UnityEngineHook.AsUnityVector(force));
        }

        public void AddRelativeForce(float x, float y, float z, string mode)
        {
            if (rigidbody == null) return;
            rigidbody.AddRelativeForce(x, y, z, Enum.Parse<ForceMode>(mode));
        }

        public void AddRelativeForce(float x, float y, float z)
        {
            if (rigidbody == null) return;
            rigidbody.AddRelativeForce(x, y, z);
        }

        public void AddTorque(EngineInternal.Vector3 torque, string mode)
        {
            if (rigidbody == null) return;
            rigidbody.AddTorque(UnityEngineHook.AsUnityVector(torque), Enum.Parse<ForceMode>(mode));
        }

        public void AddTorque(EngineInternal.Vector3 torque)
        {
            if (rigidbody == null) return;
            rigidbody.AddTorque(UnityEngineHook.AsUnityVector(torque));
        }

        public void AddTorque(float x, float y, float z, string mode)
        {
            if (rigidbody == null) return;
            rigidbody.AddTorque(x, y, z, Enum.Parse<ForceMode>(mode));
        }

        public void AddTorque(float x, float y, float z)
        {
            if (rigidbody == null) return;
            rigidbody.AddTorque(x, y, z);
        }

        public void AddRelativeTorque(EngineInternal.Vector3 torque, string mode)
        {
            if (rigidbody == null) return;
            rigidbody.AddRelativeTorque(UnityEngineHook.AsUnityVector(torque), Enum.Parse<ForceMode>(mode));
        }

        public void AddRelativeTorque(EngineInternal.Vector3 torque)
        {
            if (rigidbody == null) return;
            rigidbody.AddRelativeTorque(UnityEngineHook.AsUnityVector(torque));
        }

        public void AddRelativeTorque(float x, float y, float z, string mode)
        {
            if (rigidbody == null) return;
            rigidbody.AddRelativeTorque(x, y, z, Enum.Parse<ForceMode>(mode));
        }

        public void AddRelativeTorque(float x, float y, float z)
        {
            if (rigidbody == null) return;
            rigidbody.AddRelativeTorque(x, y, z);
        }

        public void AddForceAtPosition(EngineInternal.Vector3 force, EngineInternal.Vector3 position, string mode)
        {
            if (rigidbody == null) return;
            rigidbody.AddForceAtPosition(UnityEngineHook.AsUnityVector(force), UnityEngineHook.AsUnityVector(position), Enum.Parse<ForceMode>(mode));
        }

        public void AddForceAtPosition(EngineInternal.Vector3 force, EngineInternal.Vector3 position)
        {
            if (rigidbody == null) return;
            rigidbody.AddForceAtPosition(UnityEngineHook.AsUnityVector(force), UnityEngineHook.AsUnityVector(position));
        }

        public void AddExplosionForce(float explosionForce, EngineInternal.Vector3 explosionPosition, float explosionRadius, float upwardsModifier, string mode)
        {
            if (rigidbody == null) return;
            rigidbody.AddExplosionForce(explosionForce, UnityEngineHook.AsUnityVector(explosionPosition), explosionRadius, upwardsModifier, Enum.Parse<ForceMode>(mode));
        }

        public void AddExplosionForce(float explosionForce, EngineInternal.Vector3 explosionPosition, float explosionRadius, float upwardsModifier)
        {
            if (rigidbody == null) return;
            rigidbody.AddExplosionForce(explosionForce, UnityEngineHook.AsUnityVector(explosionPosition), explosionRadius, upwardsModifier);
        }

        public void AddExplosionForce(float explosionForce, EngineInternal.Vector3 explosionPosition, float explosionRadius)
        {
            if (rigidbody == null) return;
            rigidbody.AddExplosionForce(explosionForce, UnityEngineHook.AsUnityVector(explosionPosition), explosionRadius);
        }

        public EngineInternal.Vector3 ClosestPointOnBounds(EngineInternal.Vector3 position)
        {
            if (rigidbody == null) return default;
            return UnityEngineHook.AsSwoleVector(rigidbody.ClosestPointOnBounds(UnityEngineHook.AsUnityVector(position)));
        }

        public bool SweepTest(EngineInternal.Vector3 direction, out IRaycastHit hitInfo, float maxDistance, string queryTriggerInteraction)
        {
            throw new NotImplementedException();
        }

        public bool SweepTest(EngineInternal.Vector3 direction, out IRaycastHit hitInfo, float maxDistance)
        {
            throw new NotImplementedException();
        }

        public bool SweepTest(EngineInternal.Vector3 direction, out IRaycastHit hitInfo)
        {
            throw new NotImplementedException();
        }

        public IRaycastHit[] SweepTestAll(EngineInternal.Vector3 direction, float maxDistance, string queryTriggerInteraction)
        {
            throw new NotImplementedException();
        }

        public IRaycastHit[] SweepTestAll(EngineInternal.Vector3 direction, float maxDistance)
        {
            throw new NotImplementedException();
        }

        public IRaycastHit[] SweepTestAll(EngineInternal.Vector3 direction)
        {
            throw new NotImplementedException();
        }

        #endregion

    }
    public class ExternalRigidbody : MonoBehaviour, IRigidbodyProxy
    {

        public Type EngineComponentType => typeof(ExternalRigidbody);

        #region IEngineObject

        public EngineInternal.GameObject baseGameObject
        {
            get
            {
                return UnityEngineHook.AsSwoleGameObject(gameObject);
            }
        }

        public object Instance => this;

        public int InstanceID
        {
            get
            {
                return GetInstanceID();
            }
        }

        public bool IsDestroyed => rigidbody.rigidbody == null || this == null;

        public bool HasEventHandler => false;
        public IRuntimeEventHandler EventHandler => null;

        public void Destroy(float timeDelay = 0)
        {
            rigidbody.Destroy(timeDelay);
            swole.Engine.Object_Destroy(this, timeDelay);
        }
        public void AdminDestroy(float timeDelay = 0)
        {
            rigidbody.AdminDestroy(timeDelay);
            swole.Engine.Object_AdminDestroy(this, timeDelay);
        }

        #endregion

        [SwoleScriptIgnore]
        new public RigidbodyProxy rigidbody;

        #region IRigidbodyProxy

        [SwoleScriptIgnore]
        public Rigidbody Rigidbody
        {
            get => rigidbody.rigidbody;
            set => rigidbody.rigidbody = value;
        }

        #endregion

        #region IRigidbody

        public EngineInternal.Vector3 velocity
        {
            get
            {
                return rigidbody.velocity;
            }
            set
            {
                rigidbody.velocity = value;
            }
        }
        public EngineInternal.Vector3 angularVelocity
        {
            get
            {
                return rigidbody.angularVelocity;
            }
            set
            {
                rigidbody.angularVelocity = value;
            }
        }
        public float drag
        {
            get
            {
                return rigidbody.drag;
            }
            set
            {
                rigidbody.drag = value;
            }
        }
        public float angularDrag
        {
            get
            {
                
                return rigidbody.angularDrag;
            }
            set
            {
                rigidbody.angularDrag = value;
            }
        }
        public float mass
        {
            get
            {
                
                return rigidbody.mass;
            }
            set
            {
                rigidbody.mass = value;
            }
        }
        public bool useGravity
        {
            get
            {
                
                return rigidbody.useGravity;
            }
            set
            {
                rigidbody.useGravity = value;
            }
        }
        public float maxDepenetrationVelocity
        {
            get
            {
                
                return rigidbody.maxDepenetrationVelocity;
            }
            set
            {
                rigidbody.maxDepenetrationVelocity = value;
            }
        }
        public bool isKinematic
        {
            get
            {
                
                return rigidbody.isKinematic;
            }
            set
            {
                rigidbody.isKinematic = value;
            }
        }
        public bool freezeRotation
        {
            get
            {
                
                return rigidbody.freezeRotation;
            }
            set
            {
                rigidbody.freezeRotation = value;
            }
        }
        public string constraints
        {
            get
            {

                return rigidbody.constraints;
            }
            set
            {
                rigidbody.constraints = value;
            }
        }
        public string collisionDetectionMode
        {
            get
            {

                return rigidbody.collisionDetectionMode;
            }
            set
            {
                rigidbody.collisionDetectionMode = value;
            }
        }
        public EngineInternal.Vector3 centerOfMass
        {
            get
            {
                
                return rigidbody.centerOfMass;
            }
            set
            {
                rigidbody.centerOfMass = value;
            }
        }

        public EngineInternal.Vector3 worldCenterOfMass => rigidbody.worldCenterOfMass;

        public EngineInternal.Quaternion inertiaTensorRotation
        {
            get
            {
                
                return rigidbody.inertiaTensorRotation;
            }
            set
            {
                rigidbody.inertiaTensorRotation = value;
            }
        }
        public EngineInternal.Vector3 inertiaTensor
        {
            get
            {
                
                return rigidbody.inertiaTensor;
            }
            set
            {
                rigidbody.inertiaTensor = value;
            }
        }
        public bool detectCollisions
        {
            get
            {
                
                return rigidbody.detectCollisions;
            }
            set
            {
                rigidbody.detectCollisions = value;
            }
        }
        public EngineInternal.Vector3 position
        {
            get
            {
                
                return rigidbody.position;
            }
            set
            {
                rigidbody.position = value;
            }
        }
        public EngineInternal.Quaternion rotation
        {
            get
            {
                
                return rigidbody.rotation;
            }
            set
            {
                rigidbody.rotation = value;
            }
        }
        public string interpolation
        {
            get
            {

                return rigidbody.interpolation;
            }
            set
            {
                rigidbody.interpolation = value;
            }
        }
        public int solverIterations
        {
            get
            {
                
                return rigidbody.solverIterations;
            }
            set
            {
                rigidbody.solverIterations = value;
            }
        }
        public float sleepThreshold
        {
            get
            {
                
                return rigidbody.sleepThreshold;
            }
            set
            {
                rigidbody.sleepThreshold = value;
            }
        }
        public float maxAngularVelocity
        {
            get
            {
                
                return rigidbody.maxAngularVelocity;
            }
            set
            {
                rigidbody.maxAngularVelocity = value;
            }
        }
        public int solverVelocityIterations
        {
            get
            {
                
                return rigidbody.solverVelocityIterations;
            }
            set
            {
                rigidbody.solverVelocityIterations = value;
            }
        }

        public void MovePosition(EngineInternal.Vector3 position)
        {
            rigidbody.MovePosition(position);
        }

        public void MoveRotation(EngineInternal.Quaternion rot)
        {
            rigidbody.MoveRotation(rot);
        }

        public EngineInternal.Vector3 GetRelativePointVelocity(EngineInternal.Vector3 relativePoint)
        {
            
            return rigidbody.GetRelativePointVelocity(relativePoint);
        }

        public EngineInternal.Vector3 GetPointVelocity(EngineInternal.Vector3 worldPoint)
        {
            
            return rigidbody.GetPointVelocity(worldPoint);
        }

        public void AddForce(EngineInternal.Vector3 force, string mode)
        {
            rigidbody.AddForce(force, mode);
        }

        public void AddForce(EngineInternal.Vector3 force)
        {
            rigidbody.AddForce(force);
        }

        public void AddForce(float x, float y, float z, string mode)
        {
            rigidbody.AddForce(x, y, z, mode);
        }

        public void AddForce(float x, float y, float z)
        {
            rigidbody.AddForce(x, y, z);
        }

        public void AddRelativeForce(EngineInternal.Vector3 force, string mode)
        {
            rigidbody.AddRelativeForce(force, mode);
        }

        public void AddRelativeForce(EngineInternal.Vector3 force)
        {
            rigidbody.AddRelativeForce(force);
        }

        public void AddRelativeForce(float x, float y, float z, string mode)
        {
            rigidbody.AddRelativeForce(x, y, z, mode);
        }

        public void AddRelativeForce(float x, float y, float z)
        {
            rigidbody.AddRelativeForce(x, y, z);
        }

        public void AddTorque(EngineInternal.Vector3 torque, string mode)
        {
            rigidbody.AddTorque(torque, mode);
        }

        public void AddTorque(EngineInternal.Vector3 torque)
        {
            rigidbody.AddTorque(torque);
        }

        public void AddTorque(float x, float y, float z, string mode)
        {
            rigidbody.AddTorque(x, y, z, mode);
        }

        public void AddTorque(float x, float y, float z)
        {
            rigidbody.AddTorque(x, y, z);
        }

        public void AddRelativeTorque(EngineInternal.Vector3 torque, string mode)
        {
            rigidbody.AddRelativeTorque(torque, mode);
        }

        public void AddRelativeTorque(EngineInternal.Vector3 torque)
        {
            rigidbody.AddRelativeTorque(torque);
        }

        public void AddRelativeTorque(float x, float y, float z, string mode)
        {
            rigidbody.AddRelativeTorque(x, y, z, mode);
        }

        public void AddRelativeTorque(float x, float y, float z)
        {
            rigidbody.AddRelativeTorque(x, y, z);
        }

        public void AddForceAtPosition(EngineInternal.Vector3 force, EngineInternal.Vector3 position, string mode)
        {
            rigidbody.AddForceAtPosition(force, position, mode);
        }

        public void AddForceAtPosition(EngineInternal.Vector3 force, EngineInternal.Vector3 position)
        {
            rigidbody.AddForceAtPosition(force, position);
        }

        public void AddExplosionForce(float explosionForce, EngineInternal.Vector3 explosionPosition, float explosionRadius, float upwardsModifier, string mode)
        {
            rigidbody.AddExplosionForce(explosionForce, explosionPosition, explosionRadius, upwardsModifier, mode);
        }

        public void AddExplosionForce(float explosionForce, EngineInternal.Vector3 explosionPosition, float explosionRadius, float upwardsModifier)
        {
            rigidbody.AddExplosionForce(explosionForce, explosionPosition, explosionRadius, upwardsModifier);
        }

        public void AddExplosionForce(float explosionForce, EngineInternal.Vector3 explosionPosition, float explosionRadius)
        {
            rigidbody.AddExplosionForce(explosionForce, explosionPosition, explosionRadius);
        }

        public EngineInternal.Vector3 ClosestPointOnBounds(EngineInternal.Vector3 position)
        {
            return rigidbody.ClosestPointOnBounds(position);
        }

        public bool SweepTest(EngineInternal.Vector3 direction, out IRaycastHit hitInfo, float maxDistance, string queryTriggerInteraction)
        {
            return rigidbody.SweepTest(direction, out hitInfo, maxDistance, queryTriggerInteraction);
        }

        public bool SweepTest(EngineInternal.Vector3 direction, out IRaycastHit hitInfo, float maxDistance)
        {
            return rigidbody.SweepTest(direction, out hitInfo, maxDistance);
        }

        public bool SweepTest(EngineInternal.Vector3 direction, out IRaycastHit hitInfo)
        {
            return rigidbody.SweepTest(direction, out hitInfo);
        }
         
        public IRaycastHit[] SweepTestAll(EngineInternal.Vector3 direction, float maxDistance, string queryTriggerInteraction)
        {
            return rigidbody.SweepTestAll(direction, maxDistance, queryTriggerInteraction);
        }

        public IRaycastHit[] SweepTestAll(EngineInternal.Vector3 direction, float maxDistance)
        {
            return rigidbody.SweepTestAll(direction, maxDistance);
        }

        public IRaycastHit[] SweepTestAll(EngineInternal.Vector3 direction)
        {
            return rigidbody.SweepTestAll(direction);
        }

        #endregion

    }

}

#endif