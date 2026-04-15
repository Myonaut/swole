using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Unity.Mathematics;

using Swole.API.Unity;
using Swole.API.Unity.Animation;

namespace Swole.Cloth
{
    public class CustomizableCharacterClothBone : MonoBehaviour
    {

        [SerializeField]
        protected GameObject boundCharacterMeshGameObject;
        protected ICustomizableCharacter characterMesh; 

        [SerializeField]
        protected int boundVertexIndex;
        [SerializeField]
        protected Vector3 boundOffset;

        [SerializeField]
        protected int boundSkinningVertexIndex;

        public void BindTo(GameObject characterMeshGameObject, int vertexIndex, Vector3 offset, int skinningVertexIndex = -1, Transform initParent = null)
        {
            boundCharacterMeshGameObject = characterMeshGameObject;
            boundVertexIndex = vertexIndex;
            boundSkinningVertexIndex = skinningVertexIndex;
            if (skinningVertexIndex < 0) skinningVertexIndex = vertexIndex;
            boundOffset = offset; 
            characterMesh = boundCharacterMeshGameObject.GetComponent<ICustomizableCharacter>();  
            this.initParent = initParent;
        }

        protected ConfigurableJoint joint;
        public ConfigurableJoint Joint => joint;

        new protected Rigidbody rigidbody;
        public Rigidbody Rigidbody => rigidbody;

        protected GameObject rootObject;
        protected Rigidbody rootBody;
        protected Transform rootTransform;

        protected bool initialized;

        protected Vector3 initPosition;
        protected Quaternion initRotation;

        [SerializeField]
        protected Transform initParent;

        protected Vector3 lastPosition;
        protected Quaternion lastRotation;

        protected Quaternion startRot;

        protected int boneTrackerTransformIndex;

        [Serializable]
        public struct Settings
        {
            public bool resetPositionBeforeAnimation;

            public bool useGravity;

            public float rootMass;
            public float mass;

            public Vector3 axis;
            public Vector3 secondaryAxis;

            public float lowAngleLimitX;
            public float highAngleLimitX;

            public float angleLimitY;
            public float angleLimitZ;

            public float positionSpringForce;
            public float positionSpringDamper;
            public float positionSpringMaxForce;

            public float angularSpringForce;
            public float angularSpringDamper;
            public float angularSpringMaxForce;

            public float linearLimitDistance;

            public int layer;

            public bool useCollider;
            public UnityColliderType colliderType;
            public float colliderHeight;
            public float colliderSize;
            public Vector3 colliderOffset;
            public LayerMask colliderLayerMask;

            public static Settings Default => new Settings
            {
                useGravity = true,

                rootMass = 10f,
                mass = 1f,

                axis = Vector3.up,
                secondaryAxis = Vector3.right,

                lowAngleLimitX = -15f,
                highAngleLimitX = 15f,

                angleLimitY = 15f,
                angleLimitZ = 15f,

                positionSpringForce = 100f,
                positionSpringDamper = 5f,
                positionSpringMaxForce = 500f,

                angularSpringForce = 15f,
                angularSpringDamper = 1f,
                angularSpringMaxForce = 100f,

                linearLimitDistance = 0.03f,

                colliderLayerMask = ~0
            };
        }

        public Settings settings = Settings.Default;

        protected void Awake()
        {
            if (boundCharacterMeshGameObject != null) characterMesh = boundCharacterMeshGameObject.GetComponent<ICustomizableCharacter>();
        }

        protected void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (initParent == null) initParent = transform.parent;
            initPosition = initParent == null ? transform.localPosition : initParent.InverseTransformPoint(transform.position);
            initRotation = initParent == null ? transform.localRotation : (Quaternion.Inverse(initParent.rotation) * transform.rotation);

            transform.SetParent(null, true);

            /*var test = new GameObject($"{name} Test");
            rigidbody = test.AddComponent<Rigidbody>();
            test.transform.position = transform.position;
            transform.SetParent(test.transform, true); */

            rigidbody = gameObject.AddOrGetComponent<Rigidbody>();

            rootObject = new GameObject($"{name} Root");
            rootBody = rootObject.AddComponent<Rigidbody>();
            rootTransform = rootObject.transform;

            rootTransform.position = transform.position;
            rootBody.isKinematic = true;
            rootBody.mass = 10f;

            rigidbody.useGravity = settings.useGravity;
            rigidbody.mass = 1f;
            rigidbody.isKinematic = false;

            joint = rigidbody.gameObject.AddComponent<ConfigurableJoint>();
            joint.connectedBody = rootBody;
            joint.axis = settings.axis;
            joint.secondaryAxis = settings.secondaryAxis;

            joint.autoConfigureConnectedAnchor = false;
            joint.connectedAnchor = Vector3.zero;

            joint.angularXMotion = ConfigurableJointMotion.Limited;
            joint.angularYMotion = ConfigurableJointMotion.Limited;
            joint.angularZMotion = ConfigurableJointMotion.Limited;

            joint.lowAngularXLimit = new SoftJointLimit
            {
                limit = settings.lowAngleLimitX,
                bounciness = 0.1f,
                contactDistance = 0f 
            }; joint.highAngularXLimit = new SoftJointLimit
            {
                limit = settings.highAngleLimitX,
                bounciness = 0.1f,
                contactDistance = 0f
            };

            joint.angularYLimit = new SoftJointLimit
            {
                limit = settings.angleLimitY,
                bounciness = 0.1f,
                contactDistance = 0f 
            };

            joint.angularZLimit = new SoftJointLimit
            {
                limit = settings.angleLimitZ,
                bounciness = 0.1f,
                contactDistance = 0f
            };

            var posDrive = new JointDrive
            {
                positionSpring = settings.positionSpringForce,
                positionDamper = settings.positionSpringDamper,
                maximumForce = settings.positionSpringMaxForce 
            };

            joint.xDrive = posDrive;
            joint.yDrive = posDrive;
            joint.zDrive = posDrive;

            var angDrive = new JointDrive
            {
                positionSpring = settings.angularSpringForce,
                positionDamper = settings.angularSpringDamper,
                maximumForce = settings.angularSpringMaxForce
            };

            joint.angularXDrive = angDrive;
            joint.angularYZDrive = angDrive;

            joint.xMotion = ConfigurableJointMotion.Limited;
            joint.yMotion = ConfigurableJointMotion.Limited;
            joint.zMotion = ConfigurableJointMotion.Limited;

            joint.linearLimit = new SoftJointLimit
            {
                limit = settings.linearLimitDistance,
                bounciness = 0f,
                contactDistance = 0f 
            };

            initialized = true;

            lastPosition = transform.position;
            lastRotation = transform.rotation;

            if (characterMesh != null) boneTrackerTransformIndex = characterMesh.RigSampler.TrackingGroup.IndexOf(transform);

            gameObject.layer = settings.layer;

            if (settings.useCollider) 
            {
                switch(settings.colliderType)
                {
                    case UnityColliderType.Box:
                        var box = gameObject.AddOrGetComponent<BoxCollider>(); 
                        box.size = new Vector3(settings.colliderSize, settings.colliderHeight, settings.colliderSize);
                        box.center = settings.colliderOffset;

                        box.includeLayers = settings.colliderLayerMask;
                        box.excludeLayers = ~settings.colliderLayerMask;
                        break;

                    case UnityColliderType.Sphere:
                        var sphere = gameObject.AddOrGetComponent<SphereCollider>();
                        sphere.radius = settings.colliderSize;
                        sphere.center = settings.colliderOffset;

                        sphere.includeLayers = settings.colliderLayerMask;
                        sphere.excludeLayers = ~settings.colliderLayerMask;
                        break;

                    case UnityColliderType.Capsule:
                        var capsule = gameObject.AddOrGetComponent<CapsuleCollider>();
                        capsule.radius = settings.colliderSize;
                        capsule.center = settings.colliderOffset;
                        capsule.direction = 1;
                        capsule.height = settings.colliderHeight;

                        capsule.includeLayers = settings.colliderLayerMask;
                        capsule.excludeLayers = ~settings.colliderLayerMask;
                        break;
                }
            }
        }

        protected void OnEnable()
        {
            CustomizableCharacterClothBoneUpdater.Register(this);
        }

        protected void OnDisable()
        {
            CustomizableCharacterClothBoneUpdater.Unregister(this);
        }

        protected void OnDestroy()
        {
            if (rootObject != null) Destroy(rootObject);
        }

        public virtual void ResetPosition()
        {
            if (settings.resetPositionBeforeAnimation)
            {
                //transform.GetPositionAndRotation(out lastPosition, out lastRotation);

                Vector3 resetPos = initPosition;
                Quaternion resetRot = initRotation;
                if (initParent != null)
                {
                    resetPos = initParent.TransformPoint(initPosition);
                    resetRot = initParent.rotation * initRotation;
                }
                //transform.SetPositionAndRotation(resetPos, resetRot);

                if (boneTrackerTransformIndex >= 0)
                {
                    characterMesh.RigSampler.TrackingGroup[boneTrackerTransformIndex] = Matrix4x4.TRS(resetPos, resetRot, Vector3.one);   
                }
            }
        }

        public virtual void UpdatePosition()
        {
            if (!initialized) return;

            if (characterMesh == null || (characterMesh is Behaviour b && b == null))  
            {
                GameObject.Destroy(gameObject); 
                return;
            }

            ResetPosition(); 

            var vertexPosition = characterMesh.GetVertexInWorld(0, boundVertexIndex, out var l2w, out var vertexDelta);
            if (boundSkinningVertexIndex >= 0 && boundSkinningVertexIndex != boundVertexIndex)
            {
                vertexDelta = math.rotate(l2w, vertexDelta);
                vertexPosition = characterMesh.GetVertexInWorld(0, boundSkinningVertexIndex, out l2w, out var vertexDelta2); 
                vertexDelta2 = math.rotate(l2w, vertexDelta2);
                vertexPosition += vertexDelta - vertexDelta2; 
            }

            var worldRot = l2w.GetRotation();
            if (startRot.x == 0 && startRot.y == 0 && startRot.z == 0 && startRot.w == 0) startRot = Quaternion.Inverse(worldRot); 
            rootTransform.SetPositionAndRotation(vertexPosition + math.rotate(l2w, boundOffset), worldRot * startRot);
            /*if (settings.resetPositionBeforeAnimation)
            {
                transform.SetLocalPositionAndRotation(lastPosition, lastRotation); 
            }*/
        }
    }

    public class CustomizableCharacterClothBoneUpdater : SingletonBehaviour<CustomizableCharacterClothBoneUpdater>
    {

        public static int ExecutionPriority => CustomAnimatorUpdater.FinalAnimationBehaviourPriority + 10;

        public override int Priority => ExecutionPriority;

        protected readonly List<CustomizableCharacterClothBone> clothBones = new List<CustomizableCharacterClothBone>(); 

        public void RegisterLocal(CustomizableCharacterClothBone bone)
        {
            if (!clothBones.Contains(bone)) clothBones.Add(bone);
        }

        private readonly List<CustomizableCharacterClothBone> toRemove = new List<CustomizableCharacterClothBone>();
        public void UnregisterLocal(CustomizableCharacterClothBone bone)
        {
            toRemove.Add(bone);
        }

        public static void Register(CustomizableCharacterClothBone bone)
        {
            var instance = Instance;
            if (instance == null) return;

            instance.RegisterLocal(bone);
        }

        public static void Unregister(CustomizableCharacterClothBone bone)
        {
            var instance = InstanceOrNull;
            if (instance == null) return;

            instance.UnregisterLocal(bone);
        }

        public override void OnFixedUpdate()
        {
        }

        public override void OnUpdate()
        {
            /*foreach (var bone in clothBones)
            {
                if (bone != null) bone.ResetPosition();
            }*/
        }

        public override void OnLateUpdate()
        {
            foreach (var bone in clothBones) 
            { 
                if (bone != null) bone.UpdatePosition(); 
            }

            if (toRemove.Count > 0)
            {
                foreach (var bone in toRemove) if (bone != null) clothBones.Remove(bone);
                toRemove.Clear(); 

                clothBones.RemoveAll(b => b == null);
            }
        }

    }

}
