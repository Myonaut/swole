#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Unity.Mathematics;

#if BULKOUT_ENV
using RootMotion;
using RootMotion.Dynamics;
#endif

namespace Swole.API.Unity.Animation
{
    [Serializable]
    public class CharacterRagdollSettings : ISwoleAsset
    {

        #region ISwoleAsset
        public bool IsInternalAsset { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public string Name => throw new NotImplementedException();

        public bool IsValid => throw new NotImplementedException();

        public Type AssetType => throw new NotImplementedException();

        public object Asset => throw new NotImplementedException();

        public string CollectionID { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool HasCollectionID => throw new NotImplementedException();

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void DisposeSelf()
        {
            throw new NotImplementedException();
        }

        public bool IsIdenticalAsset(ISwoleAsset otherAsset)
        {
            throw new NotImplementedException();
        }
        #endregion

        public List<RagdollJoint> joints = new List<RagdollJoint>();

        public List<UnityColliderObject> connectedColliders = new List<UnityColliderObject>();

        public float TotalJointMass
        {
            get
            {
                if (joints == null) return 0;

                float totalMass = 0;
                foreach (var joint in joints) totalMass += joint.localBody.rigidbody.mass;

                return totalMass; 
            }
        }

    }

    [Serializable]
    public enum JointMotionType
    {
        //
        // Summary:
        //     Motion along the axis will be locked.
        Locked,
        //
        // Summary:
        //     Motion along the axis will be limited by the respective limit.
        Limited,
        //
        // Summary:
        //     Motion along the axis will be completely free and completely unconstrained.
        Free
    }

    [Serializable]
    public struct RagdollJoint
    {
        [NonSerialized]
        public ConfigurableJoint component;

        public string name;
        public string layer;

        public bool isMuscle;
        public bool isPropMuscle;
        public MuscleMotorGroup muscleMotorGroup; 

        public UnityColliderObject[] colliders;

        public UnityRigidbodyObj connectedBody;
        public UnityRigidbodyObj localBody;

        [Tooltip("The Position of the anchor around which the joints motion is constrained.")]
        public float3 localCenter;

        [Tooltip("The Direction of the axis around which the body is constrained.")]
        public float3 axis;
        public float3 secondaryAxis;

        public JointMotionType xMotion;
        public JointMotionType yMotion;
        public JointMotionType zMotion;
        public JointMotionType angularXMotion;
        public JointMotionType angularYMotion;
        public JointMotionType angularZMotion;

        [Tooltip("The configuration of the spring attached to the linear limit of the joint.")]
        public SwoleSoftJointLimitSpring linearLimitSpring;
        [Tooltip("Boundary defining movement restriction, based on distance from the joint's origin.")]
        public SwoleSoftJointLimit linearLimit;

        public SwoleSoftJointLimitSpring angularXLimitSpring;
        [Tooltip("Boundary defining lower rotation restriction, based on delta from original rotation.")]
        public SwoleSoftJointLimit lowAngularXLimit;
        [Tooltip("Boundary defining upper rotation restriction, based on delta from original rotation.")]
        public SwoleSoftJointLimit highAngularXLimit;

        [Tooltip("The configuration of the spring attached to the angular Y and angular Z limits of the joint.")]
        public SwoleSoftJointLimitSpring angularYZLimitSpring;
        
        [Tooltip("This is similar to the Angular X Limit property, but applies the limit to the y-axis and regards both the upper and lower angular limits as the same.")]
        public SwoleSoftJointLimit angularYLimit;
        [Tooltip("This is similar to the Angular X Limit property, but applies the limit to the z-axis and regards both the upper and lower angular limits as the same.")]
        public SwoleSoftJointLimit angularZLimit;

        [Tooltip("Set the force that Unity uses to rotate the joint around its local x-axis by the Position Spring and Position Damper drive torques. The Maximum Force parameter limits the force. This property is only available if the Rotation Drive Mode property is set to X & YZ.")]
        public RagdollJointDrive xDrive;
        [Tooltip("Set the force that Unity uses to rotate the joint around its local y-axis by the Position Spring and Position Damper drive torques. The Maximum Force parameter limits the force.")]
        public RagdollJointDrive yDrive;
        [Tooltip("Set the force that Unity uses to rotate the joint around its local z-axis by the Position Spring and Position Damper drive torques. The Maximum Force parameter limits the force.")]
        public RagdollJointDrive zDrive;

        [Tooltip("Set how Unity applies drive force to the object to rotate it to the target orientation. Set the mode to X and YZ, to apply the torque around the axes as specified by the Angular X/YZ Drive properties. If you use Slerp mode then the Slerp Drive properties determine the drive torque.")]
        public SwoleRotationDriveMode rotationDriveMode;

        [Tooltip("This specifies how the drive torque rotates the joint around its local x-axis. This property is only available if the Rotation Drive Mode property is set to X & YZ.")]
        public RagdollJointDrive angularXDrive;

        [Tooltip("This specifies how the drive torque rotates the joint around its local y-axis and z-axis. This property is only available if the Rotation Drive Mode property is set to X & YZ.")]
        public RagdollJointDrive angularYZDrive;

        [Tooltip("This specifies how the drive torque rotates the joint around all local axes. The property is only available if the Rotation Drive Mode property is set to Slerp.")]
        public RagdollJointDrive slerpDrive;


        [Tooltip("Brings violated constraints back into alignment even when the solver fails. Projection is not a physical process and does not preserve momentum or respect collision geometry. It is best avoided if practical, but can be useful in improving simulation quality where joint separation results in unacceptable artifacts.")]
        public SwoleJointProjectionMode projectionMode;
    
        [Tooltip("Set the linear tolerance threshold for projection. If the joint separates by more than this distance along its locked degrees of freedom, the solver will move the bodies to close the distance. Setting a very small tolerance may result in simulation jitter or other artifacts. Sometimes it is not possible to project (for example when the joints form a cycle).")]
        public float projectionDistance;

        [Tooltip("Set the angular tolerance threshold (in degrees) for projection. If the joint deviates by more than this angle around its locked angular degrees of freedom, the solver will move the bodies to close the angle. Setting a very small tolerance may result in simulation jitter or other artifacts. Sometimes it is not possible to project (for example when the joints form a cycle).")]
        public float projectionAngle;


        [Tooltip("If enabled, all Target values will be calculated in world space instead of the object's local space.")]
        public bool configuredInWorldSpace;

        [Tooltip("Enable this property to swap the order in which the physics engine processes the Rigidbodies involved in the joint. This results in different joint motion but has no impact on Rigidbodies and anchors.")] 
        public bool swapBodies;

        [Tooltip("Enable collision between bodies connected with the joint.")]
        public bool enableCollision;

        [Tooltip("If preprocessing is disabled then certain 'impossible' configurations of the joint are kept more stable rather than drifting wildly out of control.")]
        public bool enablePreprocessing;

        [Tooltip("The scale to apply to the inverted mass and inertia tensor of the **local** Rigidbody, ranging from 0.00001 to infinity. This is useful when the joint connects two Rigidbodies of largely varying mass. The physics solver produces better results when the connected Rigidbodies have a similar mass. When your connected Rigidbodies vary in mass, use this property with the Connect Mass Scale property to apply fake masses to make them roughly equal to each other. This produces a high-quality and stable simulation, but reduces the physical behaviour of the Rigidbodies.")]
        public float massScale;

        [Tooltip("The scale to apply to the inverted mass and inertia tensor of the **connected** Rigidbody, ranging from 0.00001 to infinity.")]
        public float connectedMassScale;

        public ConfigurableJoint AsComponent(GameObject gameObject, Transform root, bool forceConnectToRoot = false, Transform secondRoot = null, bool useTransformPathsForFirstRoot = true, bool useTransformPathsForSecondRoot = true, string firstRoutePrefix = null, string secondRoutePrefix = null) => AsComponent(gameObject, root, out _, forceConnectToRoot, secondRoot, useTransformPathsForFirstRoot, useTransformPathsForSecondRoot, firstRoutePrefix, secondRoutePrefix);
        public ConfigurableJoint AsComponent(GameObject gameObject, Transform root, out Rigidbody localBody, bool forceConnectToRoot = false, Transform secondRoot = null, bool useTransformPathsForFirstRoot = true, bool useTransformPathsForSecondRoot = true, string firstRoutePrefix = null, string secondRoutePrefix = null) 
        {
            var comp = gameObject.AddOrGetComponent<ConfigurableJoint>();
            AsComponent(comp, root, out localBody, forceConnectToRoot, secondRoot, useTransformPathsForFirstRoot, useTransformPathsForSecondRoot, firstRoutePrefix, secondRoutePrefix); 
            return comp;
        }
        public void AsComponent(ConfigurableJoint component, Transform root, bool forceConnectToRoot = false, Transform secondRoot = null, bool useTransformPathsForFirstRoot = true, bool useTransformPathsForSecondRoot = true, string firstRoutePrefix = null, string secondRoutePrefix = null) => AsComponent(component, root, out _, forceConnectToRoot, secondRoot, useTransformPathsForFirstRoot, useTransformPathsForSecondRoot, firstRoutePrefix, secondRoutePrefix);
        public void AsComponent(ConfigurableJoint component, Transform root, out Rigidbody localBody, bool forceConnectToRoot = false, Transform secondRoot = null, bool useTransformPathsForFirstRoot = true, bool useTransformPathsForSecondRoot = true, string firstRoutePrefix = null, string secondRoutePrefix = null)
        {
            if (!string.IsNullOrWhiteSpace(layer)) component.gameObject.layer = LayerMask.NameToLayer(layer);

            if (firstRoutePrefix == null) firstRoutePrefix = string.Empty;
            if (secondRoutePrefix == null) secondRoutePrefix = string.Empty; 

            localBody = component.gameObject.AddOrGetComponent<Rigidbody>();
            this.localBody.rigidbody.ApplyParameters(localBody);

            Rigidbody connectedBody = null;
            if (forceConnectToRoot || string.IsNullOrWhiteSpace(this.connectedBody.name))
            {
                connectedBody = root.gameObject.AddOrGetComponent<Rigidbody>();
            } 
            else
            {
                string connectedBodyPathA = this.connectedBody.name;
                string connectedBodyPathB = this.connectedBody.name;
                if (connectedBodyPathA != null)
                {
                    connectedBodyPathA = connectedBodyPathB = SwoleUtil.ConvertToDirectorySeparators(connectedBodyPathA, CharacterRagdoll._transformPathSeparator, false);

                    var hierarchy = Path.GetDirectoryName(connectedBodyPathA);
                    var bodyNameA = Path.GetFileName(connectedBodyPathA);
                    var bodyNameB = secondRoutePrefix + bodyNameA;
                    bodyNameA = firstRoutePrefix + bodyNameA;

                    connectedBodyPathA = SwoleUtil.ForceSetDirectorySeparators(Path.Join(hierarchy, bodyNameA), CharacterRagdoll._transformPathSeparator);
                    connectedBodyPathB = SwoleUtil.ForceSetDirectorySeparators(Path.Join(hierarchy, bodyNameB), CharacterRagdoll._transformPathSeparator);
                }

                var child = useTransformPathsForFirstRoot ? root.GetTransformByPath(connectedBodyPathA) : root.FindTopLevelChild(Path.GetFileName(SwoleUtil.ConvertToDirectorySeparators(connectedBodyPathA, CharacterRagdoll._transformPathSeparator, false)));
                if (child == null && secondRoot != null)
                {
                    child = useTransformPathsForSecondRoot ? secondRoot.GetTransformByPath(connectedBodyPathB) : secondRoot.FindTopLevelChild(Path.GetFileName(SwoleUtil.ConvertToDirectorySeparators(connectedBodyPathB, CharacterRagdoll._transformPathSeparator, false)));
                }

                if (child != null) 
                { 
                    connectedBody = child.gameObject.AddOrGetComponent<Rigidbody>();
                } 
            }
            if (connectedBody != null) this.connectedBody.rigidbody.ApplyParameters(connectedBody);
            component.connectedBody = connectedBody; 

            component.anchor = localCenter;

            component.axis = axis;
            component.secondaryAxis = secondaryAxis;

            component.xMotion = xMotion.AsUnityType();
            component.yMotion = yMotion.AsUnityType();
            component.zMotion = zMotion.AsUnityType();

            component.angularXMotion = angularXMotion.AsUnityType();
            component.angularYMotion = angularYMotion.AsUnityType();
            component.angularZMotion = angularZMotion.AsUnityType();

            component.linearLimitSpring = linearLimitSpring;
            component.linearLimit = linearLimit;

            component.angularXLimitSpring = angularXLimitSpring;
            component.lowAngularXLimit = lowAngularXLimit;
            component.highAngularXLimit = highAngularXLimit;

            component.angularYZLimitSpring = angularYZLimitSpring;
            component.angularYLimit = angularYLimit;
            component.angularZLimit = angularZLimit;

            component.xDrive = xDrive.AsUnityType(1);
            component.yDrive = yDrive.AsUnityType(1);
            component.zDrive = zDrive.AsUnityType(1);

            component.rotationDriveMode = rotationDriveMode.AsUnityType();

            component.angularXDrive = angularXDrive.AsUnityType(1);
            component.angularYZDrive = angularYZDrive.AsUnityType(1);
            component.slerpDrive = slerpDrive.AsUnityType(1); 

            component.projectionMode = projectionMode.AsUnityType();
            component.projectionDistance = projectionDistance;
            component.projectionAngle = projectionAngle;

            component.configuredInWorldSpace = configuredInWorldSpace;
            component.swapBodies = swapBodies;
            component.enableCollision = enableCollision;
            component.enablePreprocessing = enablePreprocessing;
            component.massScale = massScale;
            component.connectedMassScale = connectedMassScale;

            if (colliders != null)
            {
                foreach(var collider in colliders)
                {
                    collider.Create(component.gameObject, firstRoutePrefix);  
                }
            }
        }

        public static RagdollJoint FromComponent(ConfigurableJoint component, bool usePathNames, string musclePrefix = null)
        {
            Transform root = component.transform;
            while (root.parent != null) root = root.parent;

            return FromComponent(component, root, null, usePathNames, musclePrefix); 
        }
        public static RagdollJoint FromComponent(ConfigurableJoint component, Transform root, Transform muscleRoot, bool usePathNames, string musclePrefix = null)
        {
            var joint = new RagdollJoint();

            joint.name = CharacterRagdollUtils.GetNonPrefixedName(usePathNames ? component.transform.GetPathString(true, CharacterRagdoll._transformPathSeparator, root) : component.name, musclePrefix, root, usePathNames, out var realLocalObj);
            if (realLocalObj == null) realLocalObj = component.transform;
            joint.layer = LayerMask.LayerToName(component.gameObject.layer);

            var localBody = component.GetComponent<Rigidbody>(); 
            if (localBody != null)
            {
                var rigidbodyObj = new UnityRigidbodyObj();
                rigidbodyObj.name = realLocalObj.name;
                rigidbodyObj.layer = LayerMask.LayerToName(localBody.gameObject.layer);

                rigidbodyObj.localPosition = realLocalObj.localPosition;
                rigidbodyObj.localRotation = realLocalObj.localRotation;
                rigidbodyObj.localScale = realLocalObj.localScale;

                rigidbodyObj.rigidbody = UnityRigidbody.FromComponent(localBody);

                joint.localBody = rigidbodyObj;
            }
            if (component.connectedBody != null)
            {
                var rigidbodyObj = new UnityRigidbodyObj();
                rigidbodyObj.name = CharacterRagdollUtils.GetNonPrefixedName(component.connectedBody.transform.GetPathString(true, CharacterRagdoll._transformPathSeparator, root), musclePrefix, root, usePathNames, out var realConnectedObj);
                if (realConnectedObj == null) realConnectedObj = component.connectedBody.transform;
                rigidbodyObj.layer = LayerMask.LayerToName(component.connectedBody.gameObject.layer);

                rigidbodyObj.localPosition = realConnectedObj.localPosition;
                rigidbodyObj.localRotation = realConnectedObj.localRotation;
                rigidbodyObj.localScale = realConnectedObj.localScale;

                rigidbodyObj.rigidbody = UnityRigidbody.FromComponent(component.connectedBody); 

                joint.connectedBody = rigidbodyObj;
            }

            joint.localCenter = component.anchor;

            joint.axis = component.axis;
            joint.secondaryAxis = component.secondaryAxis;

            joint.xMotion = component.xMotion.AsSwoleType();
            joint.yMotion = component.yMotion.AsSwoleType();
            joint.zMotion = component.zMotion.AsSwoleType();

            joint.angularXMotion = component.angularXMotion.AsSwoleType();
            joint.angularYMotion = component.angularYMotion.AsSwoleType();
            joint.angularZMotion = component.angularZMotion.AsSwoleType();

            joint.linearLimitSpring = component.linearLimitSpring;
            joint.linearLimit = component.linearLimit;

            joint.angularXLimitSpring = component.angularXLimitSpring;
            joint.lowAngularXLimit = component.lowAngularXLimit;
            joint.highAngularXLimit = component.highAngularXLimit;

            joint.angularYZLimitSpring = component.angularYZLimitSpring;
            joint.angularYLimit = component.angularYLimit;
            joint.angularZLimit = component.angularZLimit;

            joint.xDrive = component.xDrive;
            joint.yDrive = component.yDrive;
            joint.zDrive = component.zDrive;

            joint.rotationDriveMode = component.rotationDriveMode.AsSwoleType();

            joint.angularXDrive = component.angularXDrive;
            joint.angularYZDrive = component.angularYZDrive;
            joint.slerpDrive = component.slerpDrive;

            joint.projectionMode = component.projectionMode.AsSwoleType();
            joint.projectionDistance = component.projectionDistance;
            joint.projectionAngle = component.projectionAngle;

            joint.configuredInWorldSpace = component.configuredInWorldSpace;
            joint.swapBodies = component.swapBodies;
            joint.enableCollision = component.enableCollision;
            joint.enablePreprocessing = component.enablePreprocessing;
            joint.massScale = component.massScale;
            joint.connectedMassScale = component.connectedMassScale;

            List<UnityColliderObject> colliders = new List<UnityColliderObject>();
            CharacterRagdollUtils.FindColliders(component.transform, colliders, component.transform, false, false, musclePrefix);

            joint.colliders = colliders.ToArray(); 

            return joint;
        }
        public static RagdollJoint FromComponent(CharacterJoint component, bool usePathNames, string musclePrefix = null)
        {
            Transform root = component.transform;
            while (root.parent != null) root = root.parent;

            return FromComponent(component, root, null, usePathNames, musclePrefix);
        }
        public static RagdollJoint FromComponent(CharacterJoint component, Transform root, Transform muscleRoot, bool usePathNames, string musclePrefix = null)
        {
            var joint = new RagdollJoint();

            joint.name = CharacterRagdollUtils.GetNonPrefixedName(usePathNames ? component.transform.GetPathString(true, CharacterRagdoll._transformPathSeparator, root) : component.name, musclePrefix, root, usePathNames, out var realLocalObj);
            if (realLocalObj == null) realLocalObj = component.transform;
            joint.layer = LayerMask.LayerToName(component.gameObject.layer); 

            var localBody = component.GetComponent<Rigidbody>();
            if (localBody != null)
            {

                var rigidbodyObj = new UnityRigidbodyObj();
                rigidbodyObj.name = realLocalObj.name;
                rigidbodyObj.layer = LayerMask.LayerToName(localBody.gameObject.layer);

                rigidbodyObj.localPosition = realLocalObj.localPosition;
                rigidbodyObj.localRotation = realLocalObj.localRotation;
                rigidbodyObj.localScale = realLocalObj.localScale;

                rigidbodyObj.rigidbody = UnityRigidbody.FromComponent(localBody);  

                joint.localBody = rigidbodyObj;
            }
            if (component.connectedBody != null)
            {
                var rigidbodyObj = new UnityRigidbodyObj();
                rigidbodyObj.name = CharacterRagdollUtils.GetNonPrefixedName(component.connectedBody.transform.GetPathString(true, CharacterRagdoll._transformPathSeparator, root), musclePrefix, root, usePathNames, out var realConnectedObj);
                if (realConnectedObj == null) realConnectedObj = component.connectedBody.transform;
                rigidbodyObj.layer = LayerMask.LayerToName(component.connectedBody.gameObject.layer); 

                rigidbodyObj.localPosition = realConnectedObj.localPosition;
                rigidbodyObj.localRotation = realConnectedObj.localRotation;
                rigidbodyObj.localScale = realConnectedObj.localScale; 

                rigidbodyObj.rigidbody = UnityRigidbody.FromComponent(component.connectedBody);

                joint.connectedBody = rigidbodyObj;
            }

            joint.localCenter = component.anchor;

            joint.axis = component.axis;
            joint.secondaryAxis = component.swingAxis;

            joint.angularXMotion = JointMotionType.Limited;
            joint.angularYMotion = JointMotionType.Limited;
            joint.angularZMotion = JointMotionType.Limited; 

            joint.angularXLimitSpring = component.twistLimitSpring;
            joint.lowAngularXLimit = component.lowTwistLimit;
            joint.highAngularXLimit = component.highTwistLimit;

            joint.angularYZLimitSpring = component.swingLimitSpring;
            joint.angularYLimit = component.swing1Limit;
            joint.angularZLimit = component.swing2Limit;

            joint.projectionMode = component.enableProjection ? SwoleJointProjectionMode.PositionAndRotation : SwoleJointProjectionMode.None;
            joint.projectionDistance = component.projectionDistance;
            joint.projectionAngle = component.projectionAngle;

            joint.enableCollision = component.enableCollision;
            joint.enablePreprocessing = component.enablePreprocessing;
            joint.massScale = component.massScale;
            joint.connectedMassScale = component.connectedMassScale; 

            List<UnityColliderObject> colliders = new List<UnityColliderObject>();
            CharacterRagdollUtils.FindColliders(component.transform, colliders, component.transform, false, false, musclePrefix);

            joint.colliders = colliders.ToArray();

            return joint;
        }

        public void DestroyComponent(ConfigurableJoint component, Transform root, bool immediate, Transform secondRoot = null, bool useTransformPathsForFirstRoot = true, bool useTransformPathsForSecondRoot = true, string firstRoutePrefix = null, string secondRoutePrefix = null) => DestroyComponent(component.gameObject, root, immediate, secondRoot, useTransformPathsForFirstRoot, useTransformPathsForSecondRoot, firstRoutePrefix, secondRoutePrefix);
        public void DestroyComponent(CharacterJoint component, Transform root, bool immediate, Transform secondRoot = null, bool useTransformPathsForFirstRoot = true, bool useTransformPathsForSecondRoot = true, string firstRoutePrefix = null, string secondRoutePrefix = null) => DestroyComponent(component.gameObject, root, immediate, secondRoot, useTransformPathsForFirstRoot, useTransformPathsForSecondRoot, firstRoutePrefix, secondRoutePrefix);
        public void DestroyComponent(GameObject componentObj, Transform root, bool immediate, Transform secondRoot = null, bool useTransformPathsForFirstRoot = true, bool useTransformPathsForSecondRoot = true, string firstRoutePrefix = null, string secondRoutePrefix = null)
        {
            if (componentObj != null)
            {
                if (colliders != null)
                {
                    foreach (var collider in colliders)
                    {
                        collider.Destroy(componentObj.gameObject, immediate, firstRoutePrefix);
                    }
                }

                var compA = componentObj.GetComponent<ConfigurableJoint>();
                if (compA != null)
                {
                    if (immediate) GameObject.DestroyImmediate(compA); else GameObject.Destroy(compA);
                }

                var compB = componentObj.GetComponent<CharacterJoint>();
                if (compB != null)
                {
                    if (immediate) GameObject.DestroyImmediate(compB); else GameObject.Destroy(compB);
                }
            }

            Rigidbody connectedBody = null;
            if (string.IsNullOrWhiteSpace(this.connectedBody.name))
            {
                if (root != null) connectedBody = root.gameObject.GetComponent<Rigidbody>();
            }
            else
            {
                string connectedBodyPathA = this.connectedBody.name;
                string connectedBodyPathB = this.connectedBody.name;
                if (connectedBodyPathA != null)
                {
                    connectedBodyPathA = connectedBodyPathB = SwoleUtil.ConvertToDirectorySeparators(connectedBodyPathA, CharacterRagdoll._transformPathSeparator, false);

                    var hierarchy = Path.GetDirectoryName(connectedBodyPathA);
                    var bodyNameA = Path.GetFileName(connectedBodyPathA);
                    var bodyNameB = secondRoutePrefix + bodyNameA;
                    bodyNameA = firstRoutePrefix + bodyNameA;

                    connectedBodyPathA = SwoleUtil.ForceSetDirectorySeparators(Path.Join(hierarchy, bodyNameA), CharacterRagdoll._transformPathSeparator);
                    connectedBodyPathB = SwoleUtil.ForceSetDirectorySeparators(Path.Join(hierarchy, bodyNameB), CharacterRagdoll._transformPathSeparator);
                }

                var child = useTransformPathsForFirstRoot ? root.GetTransformByPath(connectedBodyPathA) : root.FindTopLevelChild(Path.GetFileName(SwoleUtil.ConvertToDirectorySeparators(connectedBodyPathA, CharacterRagdoll._transformPathSeparator, false)));
                if (child == null && secondRoot != null)
                {
                    child = useTransformPathsForSecondRoot ? secondRoot.GetTransformByPath(connectedBodyPathB) : secondRoot.FindTopLevelChild(Path.GetFileName(SwoleUtil.ConvertToDirectorySeparators(connectedBodyPathB, CharacterRagdoll._transformPathSeparator, false)));
                }

                if (child != null) connectedBody = child.gameObject.GetComponent<Rigidbody>();  
            }
            if (connectedBody != null)
            {
                var joint = connectedBody.gameObject.GetComponent<Joint>();
                if (joint == null)
                {
                    if (immediate) GameObject.DestroyImmediate(connectedBody); else GameObject.Destroy(connectedBody);
                }
            } 

            if (componentObj != null)
            {
                var localBody = componentObj.GetComponent<Rigidbody>();
                if (localBody != null)
                {
                    if (immediate) GameObject.DestroyImmediate(localBody); else GameObject.Destroy(localBody); 
                }
            }
        }
    }

    [Serializable]
    public struct SwoleSoftJointLimit
    {
        [Tooltip("The limit position/angle of the joint (in degrees).")]
        public float limit;

        [Tooltip("Set a bounce torque to apply to the object when its rotation reaches the limit angle.")]
        public float bounciness;

        [Tooltip("Determines how far ahead in space the solver can \"see\" the joint limit. The minimum angular tolerance (between the joint angle and the limit) at which the limit is enforced. A high tolerance violates the limit less often when the object is moving fast. However, this requires the physics simulation to consider the limit more often and reduces performance slightly.")]
        public float contactDistance;

        public static implicit operator SoftJointLimit(SwoleSoftJointLimit limit) => new SoftJointLimit() { limit = limit.limit, bounciness = limit.bounciness, contactDistance = limit.contactDistance };
        public static implicit operator SwoleSoftJointLimit(SoftJointLimit limit) => new SwoleSoftJointLimit() { limit = limit.limit, bounciness = limit.bounciness, contactDistance = limit.contactDistance };
    }

    [Serializable]
    public struct SwoleSoftJointLimitSpring
    {
        public float spring;
        public float damper;

        public static implicit operator SoftJointLimitSpring(SwoleSoftJointLimitSpring spring) => new SoftJointLimitSpring() { spring = spring.spring, damper = spring.damper };
        public static implicit operator SwoleSoftJointLimitSpring(SoftJointLimitSpring spring) => new SwoleSoftJointLimitSpring() { spring = spring.spring, damper = spring.damper };
    }

    [Serializable]
    public struct RagdollJointDrive
    {

        [Tooltip("Strength of a rubber-band pull toward the defined direction.")]
        private float positionSpringMax;

        [Tooltip("Resistance strength against the Position Spring.")]
        private float positionDamper;

        [Tooltip("Maximum amount of force that can be applied to push the object toward the defined direction.")]
        private float maximumForce;

        [Tooltip("Defines whether the drive is an acceleration drive or a force drive.")]
        private bool useAcceleration;

        public JointDrive AsUnityType(float strength)
        {
            var unityType = new JointDrive();

            unityType.positionSpring = positionSpringMax * strength;
            unityType.positionDamper = positionDamper;
            unityType.maximumForce = maximumForce;
            unityType.useAcceleration = useAcceleration;

            return unityType; 
        }
        public static implicit operator RagdollJointDrive(JointDrive drive) => new RagdollJointDrive() { positionSpringMax = drive.positionSpring, positionDamper = drive.positionDamper, maximumForce = drive.maximumForce, useAcceleration = drive.useAcceleration };
    }

    //
    // Summary:
    //     Control ConfigurableJoint's rotation with either X & YZ or Slerp Drive.
    [Serializable]
    public enum SwoleRotationDriveMode
    {
        //
        // Summary:
        //     Use XY & Z Drive.
        XYAndZ,
        //
        // Summary:
        //     Use Slerp drive.
        Slerp
    }

    //
    // Summary:
    //     Determines how to snap physics joints back to its constrained position when it
    //     drifts off too much.
    [Serializable]
    public enum SwoleJointProjectionMode
    {
        //
        // Summary:
        //     Don't snap at all.
        None,
        //
        // Summary:
        //     Snap both position and rotation.
        PositionAndRotation
    }

    [Serializable]
    public enum UnityColliderType
    {
        Box, Sphere, Capsule
    }

    [Serializable]
    public enum AxisXYZ
    {
        X, Y, Z
    }

    [Serializable]
    public struct UnityCollider
    {
        public UnityColliderType type;

        public string material;

        public float3 center;
        public float3 size;

        public float radius;

        public float height;

        public AxisXYZ direction;

        public void AddToUnityObject(GameObject obj)
        {
            switch (type)
            {
                case UnityColliderType.Box:
                    var box = obj.AddOrGetComponent<BoxCollider>();
                    box.center = center;
                    box.size = size;
                    break;

                case UnityColliderType.Sphere:
                    var sphere = obj.AddOrGetComponent<SphereCollider>();
                    sphere.center = center;
                    sphere.radius = radius;
                    break;

                case UnityColliderType.Capsule:
                    var capsule = obj.AddOrGetComponent<CapsuleCollider>();
                    capsule.center = center;
                    capsule.radius = radius;
                    capsule.height = height;
                    capsule.direction = (int)direction;
                    break;
            }
        }

        public void RemoveFromUnityObject(GameObject obj, bool immediate)
        {
            switch (type)
            {
                case UnityColliderType.Box:
                    var box = obj.GetComponent<BoxCollider>();
                    if (box != null)
                    {
                        if (immediate) GameObject.DestroyImmediate(box); else GameObject.Destroy(box);
                    }
                    break;

                case UnityColliderType.Sphere:
                    var sphere = obj.GetComponent<SphereCollider>();
                    if (sphere != null)
                    {
                        if (immediate) GameObject.DestroyImmediate(sphere); else GameObject.Destroy(sphere);
                    }
                    break;

                case UnityColliderType.Capsule:
                    var capsule = obj.GetComponent<CapsuleCollider>();
                    if (capsule != null)
                    {
                        if (immediate) GameObject.DestroyImmediate(capsule); else GameObject.Destroy(capsule);
                    }
                    break;
            }
        }
    }
    [Serializable]
    public struct UnityColliderObject
    {
        public string name;
        public string layer;

        public float3 localPosition;
        public quaternion localRotation;
        public float3 localScale;

        public UnityCollider collider;

        public void Create(GameObject root, string prefix = null)
        {
            if (prefix == null) prefix = string.Empty;

            if (string.IsNullOrWhiteSpace(name) || (prefix + name) == root.name || (prefix + name) == (prefix + root.name))
            {
                collider.AddToUnityObject(root);
            }
            else if (!string.IsNullOrWhiteSpace(name))
            {
                var pathName = SwoleUtil.ConvertToDirectorySeparators(name, CharacterRagdoll._transformPathSeparator, false);

                var hierarchy = Path.GetDirectoryName(pathName);
                var objName = prefix + Path.GetFileName(pathName);
                pathName = SwoleUtil.ForceSetDirectorySeparators(Path.Join(hierarchy, objName), CharacterRagdoll._transformPathSeparator);  

                var child = root.transform.Find(pathName);
                if (child == null)
                {
                    child = new GameObject(objName).transform;
                    child.gameObject.layer = LayerMask.NameToLayer(layer);
                    child.SetParent(root.transform);
                    child.localPosition = localPosition;
                    child.localRotation = localRotation;
                    child.localScale = localScale;
                }

                collider.AddToUnityObject(child.gameObject);
            }
        }

        public void Destroy(GameObject root, bool immediate, string prefix = null)
        {
            if (prefix == null) prefix = string.Empty;

            if (string.IsNullOrWhiteSpace(name) || (prefix + name) == root.name || (prefix + name) == (prefix + root.name))
            {
                collider.RemoveFromUnityObject(root, immediate); 
            }
            else if (!string.IsNullOrWhiteSpace(name))
            {
                var child = root.transform.Find(name); 
                if (child != null)
                {
                    collider.RemoveFromUnityObject(child.gameObject, immediate);
                } 
                else if (!string.IsNullOrEmpty(prefix))
                {
                    var pathName = SwoleUtil.ConvertToDirectorySeparators(name, CharacterRagdoll._transformPathSeparator, false);

                    var hierarchy = Path.GetDirectoryName(pathName);
                    var objName = prefix + Path.GetFileName(pathName);
                    pathName = SwoleUtil.ForceSetDirectorySeparators(Path.Join(hierarchy, objName), CharacterRagdoll._transformPathSeparator);

                    child = root.transform.Find(pathName);
                    if (child != null)
                    {
                        collider.RemoveFromUnityObject(child.gameObject, immediate); 
                    }
                }
            }
        }
    }

    [Serializable]
    public struct UnityRigidbody
    {
        //
        // Summary:
        //     The drag of the object.
        public float drag;

        //
        // Summary:
        //     The angular drag of the object.
        public float angularDrag;

        //
        // Summary:
        //     The mass of the rigidbody.
        public float mass;

        //
        // Summary:
        //     Controls whether gravity affects this rigidbody.
        public bool useGravity;

        //
        // Summary:
        //     Controls whether physics affects the rigidbody.
        public bool isKinematic;

        //
        // Summary:
        //     Controls whether physics will change the rotation of the object.
        public bool freezeRotation;

        //
        // Summary:
        //     Controls which degrees of freedom are allowed for the simulation of this Rigidbody.
        public SwoleRigidbodyConstraints constraints;

        //
        // Summary:
        //     The Rigidbody's collision detection mode.
        public SwoleCollisionDetectionMode collisionDetectionMode;

        //
        // Summary:
        //     Whether or not to calculate the center of mass automatically.
        public bool automaticCenterOfMass;

        //
        // Summary:
        //     The center of mass relative to the transform's origin.
        public float3 centerOfMass;

        //
        // Summary:
        //     Whether or not to calculate the inertia tensor automatically.
        public bool automaticInertiaTensor;

        //
        // Summary:
        //     The rotation of the inertia tensor.
        public quaternion inertiaTensorRotation;

        //
        // Summary:
        //     The inertia tensor of this body, defined as a diagonal matrix in a reference
        //     frame positioned at this body's center of mass and rotated by Rigidbody.inertiaTensorRotation.
        public float3 inertiaTensor;

        //
        // Summary:
        //     Interpolation provides a way to manage the appearance of jitter in the movement
        //     of your Rigidbody GameObjects at run time.
        public SwoleRigidbodyInterpolation interpolation;

        public void ApplyParameters(Rigidbody rigidbody)
        {
            rigidbody.drag = drag;
            rigidbody.angularDrag = angularDrag;
            rigidbody.mass = mass;
            rigidbody.useGravity = useGravity;
            rigidbody.isKinematic = isKinematic;
            rigidbody.freezeRotation = freezeRotation;
            rigidbody.constraints = constraints.AsUnityType();
            rigidbody.collisionDetectionMode = collisionDetectionMode.AsUnityType();
            rigidbody.automaticCenterOfMass = automaticCenterOfMass;
            rigidbody.centerOfMass = centerOfMass;
            rigidbody.automaticInertiaTensor = automaticInertiaTensor;
            rigidbody.inertiaTensorRotation = inertiaTensorRotation;
            rigidbody.inertiaTensor = inertiaTensor;
            rigidbody.interpolation = interpolation.AsUnityType();
        }

        public static UnityRigidbody FromComponent(Rigidbody rigidbody)
        {
            var settings = new UnityRigidbody();

            settings.drag = rigidbody.drag;
            settings.angularDrag = rigidbody.angularDrag;
            settings.mass = rigidbody.mass;
            settings.useGravity = rigidbody.useGravity;
            settings.isKinematic = rigidbody.isKinematic;
            settings.freezeRotation = rigidbody.freezeRotation;
            settings.constraints = rigidbody.constraints.AsSwoleType();
            settings.collisionDetectionMode = rigidbody.collisionDetectionMode.AsSwoleType();
            settings.automaticCenterOfMass = rigidbody.automaticCenterOfMass;
            settings.centerOfMass = rigidbody.centerOfMass;
            settings.automaticInertiaTensor = rigidbody.automaticInertiaTensor;
            settings.inertiaTensorRotation = rigidbody.inertiaTensorRotation;
            settings.inertiaTensor = rigidbody.inertiaTensor;
            settings.interpolation = rigidbody.interpolation.AsSwoleType();

            return settings;
        }
    }
    [Serializable]
    public struct UnityRigidbodyObj
    {
        public string name;
        public string layer;

        public float3 localPosition;
        public quaternion localRotation;
        public float3 localScale;

        public UnityRigidbody rigidbody;
    }

    [Serializable]
    public enum SwoleRigidbodyConstraints
    {
        //
        // Summary:
        //     No constraints.
        None = 0,
        //
        // Summary:
        //     Freeze motion along the X-axis.
        FreezePositionX = 2,
        //
        // Summary:
        //     Freeze motion along the Y-axis.
        FreezePositionY = 4,
        //
        // Summary:
        //     Freeze motion along the Z-axis.
        FreezePositionZ = 8,
        //
        // Summary:
        //     Freeze rotation along the X-axis.
        FreezeRotationX = 16,
        //
        // Summary:
        //     Freeze rotation along the Y-axis.
        FreezeRotationY = 32,
        //
        // Summary:
        //     Freeze rotation along the Z-axis.
        FreezeRotationZ = 64,
        //
        // Summary:
        //     Freeze motion along all axes.
        FreezePosition = 14,
        //
        // Summary:
        //     Freeze rotation along all axes.
        FreezeRotation = 112,
        //
        // Summary:
        //     Freeze rotation and motion along all axes.
        FreezeAll = 126
    }

    //
    // Summary:
    //     The collision detection mode constants used for Rigidbody.collisionDetectionMode.
    [Serializable]
    public enum SwoleCollisionDetectionMode
    {
        //
        // Summary:
        //     Continuous collision detection is off for this Rigidbody.
        Discrete,
        //
        // Summary:
        //     Continuous collision detection is on for colliding with static mesh geometry.
        Continuous,
        //
        // Summary:
        //     Continuous collision detection is on for colliding with static and dynamic geometry.
        ContinuousDynamic,
        //
        // Summary:
        //     Speculative continuous collision detection is on for static and dynamic geometries
        ContinuousSpeculative
    }
    //
    // Summary:
    //     Rigidbody interpolation mode.
    [Serializable]
    public enum SwoleRigidbodyInterpolation
    {
        //
        // Summary:
        //     No Interpolation.
        None,
        //
        // Summary:
        //     Interpolation will always lag a little bit behind but can be smoother than extrapolation.
        Interpolate,
        //
        // Summary:
        //     Extrapolation will predict the position of the rigidbody based on the current
        //     velocity.
        Extrapolate
    }

    [Serializable]
    public enum MuscleMotorGroup
    {
        Hips = 0,
        Spine = 1,
        Head = 2,
        Arm = 3,
        Hand = 4,
        Leg = 5,
        Foot = 6,
        Tail = 7,
        Prop = 8
    }

    public static class CharacterRagdollUtils
    {
        public static string GetNonPrefixedName(string objName, string prefix, Transform root = null, bool usePathNames = true) => GetNonPrefixedName(objName, prefix, root, usePathNames, out _);
        public static string GetNonPrefixedName(string objName, string prefix, Transform root, bool usePathNames, out Transform nonPrefixedObj)
        {
            nonPrefixedObj = null;
            if (string.IsNullOrEmpty(prefix)) return objName;

            objName = SwoleUtil.ConvertToDirectorySeparators(objName, CharacterRagdoll._transformPathSeparator, false);
            var hierarchy = Path.GetDirectoryName(objName);
            var name = Path.GetFileName(objName);

            if (name.StartsWith(prefix) && name.Length > prefix.Length)
            { 
                name = name.Substring(prefix.Length);
            }

            if (root != null)
            {
                nonPrefixedObj = root.FindDeepChild(name); 
                if (nonPrefixedObj != null)
                {
                    if ((hierarchy != null && hierarchy.Length > 0) || usePathNames)
                    {
                        return nonPrefixedObj.GetPathString(true, CharacterRagdoll._transformPathSeparator, root);  
                    } 
                    else
                    { 
                        return nonPrefixedObj.name;
                    }
                } 
            }

            objName = Path.Join(hierarchy, name);
            SwoleUtil.ForceSetDirectorySeparators(objName, CharacterRagdoll._transformPathSeparator);

            return objName;
        }
        public static string GetPrefixedName(string objName, string prefix)
        {
            if (prefix == null) prefix = string.Empty;

            objName = SwoleUtil.ConvertToDirectorySeparators(objName, CharacterRagdoll._transformPathSeparator, false);
            var hierarchy = Path.GetDirectoryName(objName);
            var name = prefix + Path.GetFileName(objName);
            objName = Path.Join(hierarchy, name);
            SwoleUtil.ForceSetDirectorySeparators(objName, CharacterRagdoll._transformPathSeparator);

            return objName;
        }

        public static void FindColliders(Transform root, List<UnityColliderObject> colliders, Transform obj, bool stopAtRigidbody, bool usePathNames = false, string prefix = null)
        {


            if (stopAtRigidbody && obj.GetComponent<Rigidbody>() != null) return;

            bool isRoot = ReferenceEquals(root, obj);

            string name = CharacterRagdollUtils.GetNonPrefixedName(usePathNames ? obj.GetPathString(true, CharacterRagdoll._transformPathSeparator, root) : obj.name, prefix, root, usePathNames, out var realObj);
            if (realObj == null) realObj = obj; 
            var colliderObj = new UnityColliderObject()
            {
                name = name, 
                layer = LayerMask.LayerToName(obj.gameObject.layer),
                localPosition = isRoot ? realObj.localPosition : root.InverseTransformPoint(realObj.position),
                localRotation = isRoot ? realObj.localRotation : (Quaternion.Inverse(root.rotation) * realObj.rotation),
                localScale = realObj.localScale
            };

            var box = obj.GetComponent<BoxCollider>();
            if (box != null)
            {
                colliderObj.collider = new UnityCollider()
                {
                    type = UnityColliderType.Box,
                    material = null, // TODO: Add loadable physics materials
                    center = box.center,
                    size = box.size
                };

                colliders.Add(colliderObj);
            }
            var sphere = obj.GetComponent<SphereCollider>();
            if (sphere != null)
            {
                colliderObj.collider = new UnityCollider()
                {
                    type = UnityColliderType.Sphere,
                    material = null, // TODO: Add loadable physics materials
                    center = sphere.center,
                    radius = sphere.radius
                };

                colliders.Add(colliderObj);
            }
            var capsule = obj.GetComponent<CapsuleCollider>();
            if (capsule != null)
            {
                colliderObj.collider = new UnityCollider()
                {
                    type = UnityColliderType.Capsule,
                    material = null, // TODO: Add loadable physics materials
                    center = capsule.center,
                    radius = capsule.radius,
                    height = capsule.height,
                    direction = (AxisXYZ)capsule.direction
                };

                colliders.Add(colliderObj);
            }

            for (int a = 0; a < obj.childCount; a++)
            {
                var child = obj.GetChild(a);
                FindColliders(root, colliders, child, true, usePathNames, prefix);
            }
        }

        public static ConfigurableJointMotion AsUnityType(this JointMotionType st)
        {
            switch (st)
            {
                case JointMotionType.Locked:
                    return ConfigurableJointMotion.Locked;

                case JointMotionType.Limited:
                    return ConfigurableJointMotion.Limited;

                case JointMotionType.Free:
                    return ConfigurableJointMotion.Free;
            }

            return ConfigurableJointMotion.Locked;
        }
        public static JointMotionType AsSwoleType(this ConfigurableJointMotion ut)
        {
            switch (ut)
            {
                case ConfigurableJointMotion.Locked:
                    return JointMotionType.Locked;

                case ConfigurableJointMotion.Limited:
                    return JointMotionType.Limited;

                case ConfigurableJointMotion.Free:
                    return JointMotionType.Free;
            }

            return JointMotionType.Locked;
        }

        public static RotationDriveMode AsUnityType(this SwoleRotationDriveMode st)
        {
            switch (st)
            {
                case SwoleRotationDriveMode.XYAndZ:
                    return RotationDriveMode.XYAndZ;

                case SwoleRotationDriveMode.Slerp:
                    return RotationDriveMode.Slerp;
            }

            return RotationDriveMode.XYAndZ;
        }
        public static SwoleRotationDriveMode AsSwoleType(this RotationDriveMode ut)
        {
            switch (ut)
            {
                case RotationDriveMode.XYAndZ:
                    return SwoleRotationDriveMode.XYAndZ;

                case RotationDriveMode.Slerp:
                    return SwoleRotationDriveMode.Slerp;
            }

            return SwoleRotationDriveMode.XYAndZ;
        }

        public static JointProjectionMode AsUnityType(this SwoleJointProjectionMode st)
        {
            switch (st)
            {
                case SwoleJointProjectionMode.None:
                    return JointProjectionMode.None;

                case SwoleJointProjectionMode.PositionAndRotation:
                    return JointProjectionMode.PositionAndRotation;
            }

            return JointProjectionMode.None;
        }
        public static SwoleJointProjectionMode AsSwoleType(this JointProjectionMode ut)
        {
            switch (ut)
            {
                case JointProjectionMode.None:
                    return SwoleJointProjectionMode.None;

                case JointProjectionMode.PositionAndRotation:
                    return SwoleJointProjectionMode.PositionAndRotation;
            }

            return SwoleJointProjectionMode.None;
        }

        public static RigidbodyConstraints AsUnityType(this SwoleRigidbodyConstraints st) => (RigidbodyConstraints)(int)st;
        public static SwoleRigidbodyConstraints AsSwoleType(this RigidbodyConstraints ut) => (SwoleRigidbodyConstraints)(int)ut;

        public static CollisionDetectionMode AsUnityType(this SwoleCollisionDetectionMode st) => (CollisionDetectionMode)(int)st;
        public static SwoleCollisionDetectionMode AsSwoleType(this CollisionDetectionMode ut) => (SwoleCollisionDetectionMode)(int)ut;

        public static RigidbodyInterpolation AsUnityType(this SwoleRigidbodyInterpolation st) => (RigidbodyInterpolation)(int)st;
        public static SwoleRigidbodyInterpolation AsSwoleType(this RigidbodyInterpolation ut) => (SwoleRigidbodyInterpolation)(int)ut;

#if BULKOUT_ENV
        public static Muscle.Group AsUnityType(this MuscleMotorGroup st) => (Muscle.Group)(int)st;
        public static MuscleMotorGroup AsSwoleType(this Muscle.Group ut) => (MuscleMotorGroup)(int)ut; 
#endif
    }
}

#endif