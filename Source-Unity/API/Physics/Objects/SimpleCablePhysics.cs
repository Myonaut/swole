using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole.API.Unity
{
    [ExecuteAlways]
    public class SimpleCablePhysics : MonoBehaviour
    {

        [SerializeField]
        private Transform root;
        public Transform Root
        {
            set => root = value;
            get => root == null ? transform : root;
        }

        [Header("Setup")]
        public bool autoFillChain;
        public bool reverseFill;
        public bool deleteCableBoneChildren;
        public string numberDelimeter = ".";
        public bool initialize;
        public bool autoInitialize;

#if UNITY_EDITOR
        public void Update() 
        {
            if (autoFillChain)
            {
                autoFillChain = false;
                AutoFillTransformChain(Root, reverseFill, deleteCableBoneChildren); 
            }
            if (initialize)
            {
                initialize = false;
                Initialize();
            }
        }
#endif

        public void Awake()
        {
            if (autoInitialize) Initialize();

            if (transformChain != null && transformChain.Length > 1)
            {
                for (int i = 1; i < transformChain.Length; i++)
                {
                    var tA = transformChain[i - 1];
                    var tB = transformChain[i];

                    if (tA == null || tB == null) continue;

                    averageSpacing += (tB.position - tA.position).magnitude;
                }

                averageSpacing = averageSpacing / (transformChain.Length - 1f);
            }

            if (physicsBodies != null && physicsBodies.Length > 0)
            {
                foreach (var body in physicsBodies) if (body != null) body.solverIterations = solverIterations;
            }
            
        }

        public Vector3 lookUpVector = Vector3.up;
        public Vector3 forwardVector = Vector3.forward;
        public bool addColliders;
        public float colliderRadius = 0.01f;
        public Swole.DataStructures.Axis colliderDirection;
        [Range(0f, 1f)]
        public float colliderLengthRatio = 1f;

        public LayerMask collisionMask = ~0;

        protected float averageSpacing;
        public float AverageSpacing => averageSpacing;

        [SerializeField]
        private Transform[] transformChain;
        [SerializeField]
        private Transform[] physicsTransforms;
        [SerializeField]
        private Rigidbody[] physicsBodies; 

        public int ChainLength => transformChain == null ? 0 : transformChain.Length;
        public Transform GetVisualBone(int index) => transformChain[index];
        public Transform GetPhysicsTransform(int index) => physicsTransforms[index];
        public Rigidbody GetPhysicsBody(int index) => physicsBodies[index];

        private int CompareTransformName(Transform a, Transform b)
        {
            int delA = a.name.IndexOf(numberDelimeter);
            int delB = b.name.IndexOf(numberDelimeter);
            if (delA < 0 && delB < 0) return 0; 

            if (delA < 0) return 1;
            if (delB < 0) return -1;

            string numA = delA + 1 >= a.name.Length ? string.Empty : a.name.Substring(delA + 1);
            string numB = delB + 1 >= b.name.Length ? string.Empty : b.name.Substring(delB + 1);

            if (string.IsNullOrWhiteSpace(numA) && string.IsNullOrWhiteSpace(numB)) return 0;

            if (string.IsNullOrWhiteSpace(numA)) return 1;
            if (string.IsNullOrWhiteSpace(numB)) return -1;

            if (float.TryParse(numA, out float fA) && float.TryParse(numB, out float fB))
            {
                return fA.CompareTo(fB);
            }

            return 0;
        }
        public void AutoFillTransformChain(Transform root, bool reverse = false, bool deleteExcessChildren = false)
        {
            List<Transform> transforms = new List<Transform>();
            for(int a = 0; a < root.childCount; a++) transforms.Add(root.GetChild(a));
            transforms.Sort(CompareTransformName);
            if (reverse) transforms.Reverse();

            transformChain = transforms.ToArray();
            if (deleteExcessChildren)
            {
                foreach(var t in transformChain)
                {
                    var children = t.gameObject.GetComponentsInChildren<Transform>(true);
                    foreach(var child in children)
                    {
                        if (child == t || child == null) continue; 
                        GameObject.DestroyImmediate(child.gameObject);
                    }
                }
            }
        }

        public int solverIterations = 24;
        public float segmentMass = 0.01f;
        public float segmentDrag = 0.3f;
        public float segmentAngularDrag = 0.3f;

        [Header("Spring Joint")]
        [SerializeField, Min(1f)] private float springForce = 100;
        [SerializeField] private float springDamper = 0.5f;
        [SerializeField, Min(0f)] private float springMinDistance = 0f;
        [SerializeField, Min(0f)] private float springMaxDistance = 0f;
        public float springTolerance = 0.06f;
        public bool springPreProcessing = false;
        public bool springCollision = false;

        public void SetupSpring(SpringJoint spring, Rigidbody connectedBody) 
        {
            spring.connectedBody = connectedBody;
            spring.spring = springForce;
            spring.damper = springDamper;
            spring.autoConfigureConnectedAnchor = false;
            spring.anchor = Vector3.zero;
            spring.connectedAnchor = Vector3.zero;
            spring.minDistance = springMinDistance;
            spring.maxDistance = springMaxDistance;
            spring.enablePreprocessing = springPreProcessing;
            spring.tolerance = springTolerance;
            spring.enableCollision = springCollision && addColliders;
        }
        public void SetupCollider(CapsuleCollider collider, float distanceBetweenPoints)
        {
            collider.direction = (int)colliderDirection;
            collider.radius = colliderRadius;
            collider.height = distanceBetweenPoints * colliderLengthRatio;
            collider.center = (collider.direction == 0 ? Vector3.right : collider.direction == 1 ? Vector3.up : Vector3.forward) * distanceBetweenPoints * 0.5f;  
            collider.includeLayers = collisionMask;
            collider.excludeLayers = ~collisionMask;
        }

        public void Initialize()
        {
            if (transformChain == null || transformChain.Length <= 1) return;  

            if (physicsTransforms != null)
            {
                foreach(var t in physicsTransforms) if (t != null) GameObject.DestroyImmediate(t.gameObject);
            }
            if (physicsBodies != null)
            {
                foreach (var b in physicsBodies) if (b != null) GameObject.DestroyImmediate(b.gameObject);
            }

            if (physicsTransforms == null || physicsTransforms.Length != transformChain.Length) physicsTransforms = new Transform[transformChain.Length];
            if (physicsBodies == null || physicsBodies.Length != transformChain.Length) physicsBodies = new Rigidbody[transformChain.Length];

            var root = Root;
            for (int i = 1; i < transformChain.Length; i++) 
            {
                var tA = transformChain[i - 1];
                var tB = transformChain[i];

                var pTA = i == 1 ? new GameObject($"physicsTransform_{i - 1}").transform : physicsTransforms[i - 1];
                var pTB = new GameObject($"physicsTransform_{i}").transform;

                pTA.SetParent(root, false);
                pTA.position = tA.position;
                //pTA.rotation = tA.rotation;

                pTB.SetParent(root, false);
                pTB.position = tB.position;
                //pTB.rotation = tB.rotation;

                var rbA = pTA.gameObject.AddOrGetComponent<Rigidbody>();
                var rbB = pTB.gameObject.AddOrGetComponent<Rigidbody>();

                rbA.solverIterations = solverIterations;
                rbB.solverIterations = solverIterations; 

                physicsTransforms[i - 1] = pTA;
                physicsTransforms[i] = pTB;

                physicsBodies[i - 1] = rbA;
                physicsBodies[i] = rbB;

                if (i == 1) rbA.isKinematic = true; 
                if (i == transformChain.Length - 1) rbB.isKinematic = true;

                rbA.drag = segmentDrag;
                rbB.drag = segmentDrag;

                rbA.angularDrag = segmentAngularDrag;
                rbB.angularDrag = segmentAngularDrag;

                rbA.mass = segmentMass;
                rbB.mass = segmentMass;

                rbA.sleepThreshold = 0f;
                rbB.sleepThreshold = 0f;

                rbA.freezeRotation = true;
                rbB.freezeRotation = true;

                var colA = addColliders ? rbA.gameObject.AddOrGetComponent<CapsuleCollider>() : rbA.gameObject.GetComponent<CapsuleCollider>();
                if (addColliders)
                {
                    SetupCollider(colA, (tB.position - tA.position).magnitude); 
                }
                else
                {
                    GameObject.DestroyImmediate(colA);
                }

                var joint = pTB.gameObject.AddOrGetComponent<SpringJoint>(); 
                SetupSpring(joint, rbA); 
            }

            averageSpacing = averageSpacing / (transformChain.Length - 1f);
        }

        protected void FixedUpdate()
        {
            if (transformChain == null || transformChain.Length <= 1 || physicsTransforms == null || physicsTransforms.Length != transformChain.Length) return;

            int chainLength = transformChain.Length;
            int lastIndex = transformChain.Length - 1;
            for (int i = 1; i < transformChain.Length; i++)
            {
                int prevI = i - 1;

                var rbA = physicsBodies[prevI]; 
                var rbB = physicsBodies[i];

                Quaternion prevRot; 
                if (addColliders && rbA != null && rbB != null)
                {
                    prevRot = Quaternion.identity;
                    if (i > 1) prevRot = physicsBodies[i - 2].rotation;

                    rbA.rotation = Quaternion.RotateTowards(rbA.rotation, Quaternion.FromToRotation(prevRot * forwardVector, (rbB.position - rbA.position).normalized) * prevRot, Time.fixedDeltaTime * 360f);
                }

                var tA = transformChain[prevI];
                var tB = transformChain[i];

                var pTA = physicsTransforms[prevI];
                var pTB = physicsTransforms[i];

                prevRot = Quaternion.identity;
                if (i > 1) prevRot = transformChain[i - 2].rotation;

                Vector3 from = prevRot * forwardVector;
                Vector3 to = (pTB.position - pTA.position).normalized; 
                float angle = Vector3.Angle(from, to);
                tA.rotation = Quaternion.RotateTowards(tA.rotation, Quaternion.FromToRotation(from, to) * prevRot, Time.fixedDeltaTime * angle * 15f);  
                tA.position = pTA.position;
                tB.position = pTB.position; 
            }

            transformChain[lastIndex].rotation = transformChain[lastIndex - 1].rotation;
        }

    }
}
