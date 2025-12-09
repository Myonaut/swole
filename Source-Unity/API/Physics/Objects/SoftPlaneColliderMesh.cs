#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using UnityEngine;
using UnityEngine.Jobs;


#if UNITY_EDITOR
using UnityEditor;
#endif

using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;

using Swole.Unity;
using Swole.DataStructures;

namespace Swole.API.Unity
{
    public class SoftPlaneColliderMesh : MonoBehaviour
    {

#if UNITY_EDITOR
        public void OnDrawGizmos()
        {
            for (int a = 0; a < transform.childCount; a++)
            {
                var child = transform.GetChild(a);
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(child.position, 0.05f); 
            }

            if (registeredMesh != null && registeredMesh.Updater != null)
            {
                Gizmos.color = Color.red;
                for (int a = 0; a < registeredMesh.ColliderTransformCount; a++)
                {
                    var ct = registeredMesh.GetColliderTransform(a);
                    if (ct.jobIndex >= 0 && ct.transform != null)
                    {
                        var sync = registeredMesh.Updater.GetColliderSyncLocal(ct.jobIndex);
                        if (sync.targetTransformIndex >= 0)
                        {
                            var targetTransform = registeredMesh.Updater.GetQueryableTransformLocal(sync.targetTransformIndex);
                            Gizmos.DrawLine(targetTransform.position, targetTransform.TransformPoint(sync.targetOffset));
                            Gizmos.DrawWireSphere(targetTransform.TransformPoint(sync.targetOffset), 0.1f); 
                        }
                    }
                }
            }
        }
#endif

        [Header("Mesh"), Tooltip("Must be a quad mesh and have read write enabled!")]
        public Mesh inputMesh;

        [SerializeField]
        protected Mesh mesh;

        [SerializeField]
        protected Material[] materials;

        public MeshDataTools.GeneratedPlaneOrientation orientation = MeshDataTools.GeneratedPlaneOrientation.XZ;

        public int generatedFaceCountHor = 10;
        public int generatedFaceCountVer = 10;

        public float generatedSizeHor = 10f;
        public float generatedSizeVer = 10f;

        public bool generatedFlipFaces = false;

        public float generatedDepth = 0f;

        [Range(0, 1)]
        public float generatedCenterX = 0.5f;
        [Range(0, 1)]
        public float generatedCenterY = 0.5f;

        public bool generatedFlipUV_u = false;
        public bool generatedFlipUV_v = false;

        [Header("Physics"), Tooltip("It is recommended to set this to a layer that ignores collisions with itself.")]
        public int colliderLayer;
        [Tooltip("If the Collider Layer does not ignore self collision, this should be ticked to prevent collision between all local joints.")]
        public bool setIgnoreCollisionBetweenIndividualLocalColliders = false;
        public CollisionDetectionMode collisionDetectionMode = CollisionDetectionMode.Discrete;

        [Tooltip("Spawn base box colliders along the surface of the mesh. It will try to match the shape of the mesh as accurately as it can. Works well when colliding with large objects. The technique will fall apart when attempting to contain small objects during periods of significant stretching.")]
        public bool createBaseColliders = true;
        [Tooltip("Spawn box colliders along the surface of the mesh that match the position of objects that enter the trigger zone. ")]
        public bool useSurfaceColliders = true;

        [Tooltip("If an object has spawned surface colliders, and the object falls 'beneath' them, this will force the object back above the surface. Prevents objects that have surface colliders from ever passing through the mesh.")] 
        public bool forceObjectsAboveSurfaceColliders = true; 
        [Tooltip("Only applies when using surface colliders. Any base colliders will be ignored by an object if surface colliders have been spawned for it. Prevents small objects from getting stuck under base colliders (which are typically big).")]
        public bool normalSizedObjectsIgnoreCollisionWithBaseColliders = true;


        [Tooltip("Minimum object size required to spawn more than one surface collider. This is useful because small objects should only require one surface collider.")]
        public float bigObjectSurfaceCollisionScaleMin = 2f;
        [Tooltip("Any object that scales larger than this will only collide with the base colliders (if there are base colliders). i.e no surface colliders will be created for objects larger than this value.")]
        public float bigObjectSurfaceCollisionScaleMax = 4f;
        [Tooltip("Determines how many surface colliders should spawn for one object, based on the object's size. The number of surface colliders spawned (per axis) is the object's size divided by this value, with a minimum of 1.")]
        public float bigObjectSurfaceCollisionScaleInterval = 3f;

        [Tooltip("A value that multiplies the size of a base collider.")]
        public float baseColliderScale = 1f;
        [Tooltip("The minimum ratio a base collider can shrink in size along an axis.")]
        public float baseColliderMinScale = 0f;
        [Tooltip("The maximum ratio a base collider can grow in size along an axis.")]
        public float baseColliderMaxScale = 2f;

        [Tooltip("The speed at which a base collider will move and rotate to match the surface of the mesh. A value of zero is instant.")]
        public float baseColliderSyncSpeed = 6f;
        [Tooltip("The speed at which a base collider will grow or shrink to match the surface of the mesh. A value of zero is instant.")]
        public float baseColliderGrowSpeed = 3f;

        public float surfaceColliderSize = 0.3f;
        public float surfaceColliderThickness = 0.1f;

        public float nodeMass = 5f;
        public bool useGravity = false;
        public float drag = 0.15f;
        public float bounciness = 0.15f;
        public float minDistance = 0.001f;
        public float maxDistance = 0.01f;
        public bool useLimitSpring = true;
        public float limitSpring = 1000f;
        public float limitDamper = 1.5f;

        private List<MeshDataTools.MeshQuad> meshQuads;
        private List<int> boundaryIndices;

        private Transform[] vertexTransforms;
        public Transform[] VertexTransforms => vertexTransforms;

        private int[] vertexTriangles;
        public int[] VertexTransformTriangles => vertexTriangles;

        private bool[] vertexBoundaryStates;
        public bool[] VertexBoundaryStates => vertexBoundaryStates;

        [Tooltip("Update the trigger bounds based on the position of joint rigidbodies (more expensive)")]
        public bool useDynamicTriggerBounds = true;
        public bool syncRendererBoundsWithTriggerBounds = true;

        [Tooltip("If not ticked, the trigger bounds will initially be set to the bounds of the mesh")]
        public bool overrideInitialTriggerBounds = false;
        public Bounds initialTriggerBoundsOverride;

        public float triggerExpansionWidth = 5f; 
        private BoxCollider trigger;

        public void SetTriggerBounds(SoftPlaneColliderMeshUpdater.TriggerBounds bounds)
        {
            trigger.center = bounds.center;
            trigger.size = math.abs(bounds.size); 

            if (syncRendererBoundsWithTriggerBounds && renderer != null)  
            {
                renderer.localBounds = new Bounds()
                {
                    center = trigger.center,
                    size = trigger.size
                };
            }
        }

        public string jointPrefix = null;

        new private SkinnedMeshRenderer renderer;

        private Vector3 normalDirection;
        public Vector3 NormalDirectionLocal => normalDirection;
        public Vector3 NormalDirectionWorld => transform.TransformDirection(normalDirection);

        public void Awake()
        {
            if (trigger == null) trigger = gameObject.AddOrGetComponent<BoxCollider>();
            trigger.isTrigger = true;

            if (string.IsNullOrWhiteSpace(jointPrefix)) jointPrefix = $"{gameObject.GetInstanceID().ToString()}_joint";

            SetupMesh();

            if (overrideInitialTriggerBounds)
            {
                trigger.center = initialTriggerBoundsOverride.center;
                trigger.size = math.abs(initialTriggerBoundsOverride.size + new Vector3(triggerExpansionWidth, triggerExpansionWidth, triggerExpansionWidth));
            }
            else if (mesh != null)
            {
                var bounds = mesh.bounds;
                trigger.center = bounds.center;
                trigger.size = math.abs(bounds.size + new Vector3(triggerExpansionWidth, triggerExpansionWidth, triggerExpansionWidth));
            }

            bool flipNormalDir = inputMesh == null && generatedFlipFaces;
            normalDirection = Vector3.forward;
            switch (orientation)
            {
                case MeshDataTools.GeneratedPlaneOrientation.XY:
                case MeshDataTools.GeneratedPlaneOrientation.YX:
                    normalDirection = (flipNormalDir ? Vector3.forward : Vector3.back);
                    break;

                case MeshDataTools.GeneratedPlaneOrientation.XZ:
                case MeshDataTools.GeneratedPlaneOrientation.ZX:
                    normalDirection = (flipNormalDir ? Vector3.down : Vector3.up);
                    break;

                case MeshDataTools.GeneratedPlaneOrientation.ZY:
                case MeshDataTools.GeneratedPlaneOrientation.YZ:
                    normalDirection = (flipNormalDir ? Vector3.left : Vector3.right); 
                    break;
            }
        }

        private bool initialized = false;
        private SoftPlaneColliderMeshUpdater.RegisteredMesh registeredMesh;
        private List<BoxCollider> baseColliders;

        public void SetIgnoreCollisionWithBaseColliders(Collider collider, bool ignore)
        {
            if (baseColliders == null || collider == null) return;

            foreach (var baseCollider in baseColliders) if (baseCollider != collider) Physics.IgnoreCollision(collider, baseCollider, ignore);
        }
        public void IgnoreCollisionWithBaseColliders(Collider collider) => SetIgnoreCollisionWithBaseColliders(collider, true);
        public void StopIgnoringCollisionWithBaseColliders(Collider collider) => SetIgnoreCollisionWithBaseColliders(collider, false);

        public void SetIgnoreCollisionWithSurfaceColliders(Collider collider, bool ignore)
        {
            if (collidersInBounds == null) return;

            foreach (var entry in collidersInBounds)
            {
                var cib = entry.Value;

                if (cib.surfaceCollider != null && cib.surfaceCollider != collider)
                {
                    Physics.IgnoreCollision(collider, cib.surfaceCollider, ignore);
                }
                if (cib.surfaceColliders != null)
                {
                    foreach (var surfaceCollider in cib.surfaceColliders)
                    {
                        if (surfaceCollider != null && surfaceCollider != collider)
                        {
                            Physics.IgnoreCollision(collider, surfaceCollider, ignore);
                        }
                    }
                }
            }
        }
        public void IgnoreCollisionWithSurfaceColliders(Collider collider) => SetIgnoreCollisionWithSurfaceColliders(collider, true);
        public void StopIgnoringCollisionWithSurfaceColliders(Collider collider) => SetIgnoreCollisionWithSurfaceColliders(collider, false);

        public void IgnoreCollisionWithLocalColliders(Collider collider)
        {
            IgnoreCollisionWithBaseColliders(collider);
            IgnoreCollisionWithSurfaceColliders(collider); 
        }
        public void StopIgnoringCollisionWithLocalColliders(Collider collider)
        {
            StopIgnoringCollisionWithBaseColliders(collider);
            StopIgnoringCollisionWithSurfaceColliders(collider);
        }

        private void Register()
        {
            if (registeredMesh != null) return;

            registeredMesh = SoftPlaneColliderMeshUpdater.Register(this);
             
            if (createBaseColliders && meshQuads != null)
            {
                if (baseColliders == null) baseColliders = new List<BoxCollider>();
                foreach (var collider in baseColliders) GameObject.Destroy(collider.gameObject);
                baseColliders.Clear();

                var jointParentTransform = transform;
                foreach(var quad in meshQuads)
                {
                    int4 quadIndices = new int4(vertexTriangles[quad.triA], vertexTriangles[quad.triA + 1], vertexTriangles[quad.triB + 1], vertexTriangles[quad.triA + 2]);
                    var triB0 = vertexTriangles[quad.triB];
                    var triB1 = vertexTriangles[quad.triB + 1];
                    var triB2 = vertexTriangles[quad.triB + 2];

                    if (quadIndices.x != triB0 && quadIndices.y != triB0 && quadIndices.w != triB0) quadIndices.z = triB0;
                    if (quadIndices.x != triB1 && quadIndices.y != triB1 && quadIndices.w != triB1) quadIndices.z = triB1;
                    if (quadIndices.x != triB2 && quadIndices.y != triB2 && quadIndices.w != triB2) quadIndices.z = triB2;

                    var boxCollider = new GameObject($"{jointPrefix}_box").AddComponent<BoxCollider>();
                    boxCollider.gameObject.layer = colliderLayer; 
                    var boxTransform = boxCollider.transform;
                    boxTransform.SetParent(vertexTransforms[quadIndices.x], false);
                    boxTransform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                    boxTransform.localScale = Vector3.one;
                    
                    boxCollider.size = new Vector3(0.01f, 0.01f, 0.01f);

                    if (setIgnoreCollisionBetweenIndividualLocalColliders) IgnoreCollisionWithLocalColliders(boxCollider); 
                    baseColliders.Add(boxCollider);
                    registeredMesh.RegisterLocalColliderTransform(jointParentTransform, boxTransform, boxCollider, quadIndices, surfaceColliderThickness); 
                }
            }
        }

        public void Start()
        {
            initialized = true;
            Register();
        }
        public void OnEnable()
        {
            if (initialized)
            {
                Register();
            }
        }
        public void Unregister()
        {
            var updaterInstance = SoftPlaneColliderMeshUpdater.InstanceOrNull; 
            if (updaterInstance == null) return;

            updaterInstance.UnregisterLocal(this); 
            registeredMesh = null;

            if (collidersInBounds != null)
            {
                foreach(var entry in collidersInBounds)
                {
                    var cib = entry.Value;
                    if (cib.surfaceCollider != null) UnclaimBoxCollider(cib.surfaceCollider);
                    if (cib.surfaceColliders != null)
                    {
                        foreach(var sc in cib.surfaceColliders) UnclaimBoxCollider(cib.surfaceCollider); 
                    }
                }

                collidersInBounds.Clear();
            }
        }
        public void OnDisable()
        {
            if (initialized) 
            {
                Unregister();
            }
        }

        public void SetupMesh()
        {
            int[] triangles = null;
            if (mesh == null) 
            {
                if (inputMesh == null) 
                {
                    mesh = MeshDataTools.GeneratePlaneMesh(generatedFaceCountHor, generatedFaceCountVer, generatedSizeHor, generatedSizeVer, out boundaryIndices, out meshQuads, orientation, generatedFlipFaces, generatedDepth, generatedCenterX, generatedCenterY, generatedFlipUV_u, generatedFlipUV_v);
                } 
                else
                {
                    mesh = MeshDataTools.Duplicate(inputMesh);

                    triangles = mesh.triangles;
                    var openEdgeData = MeshDataTools.GetOpenEdgeData(mesh.vertexCount, triangles, MeshDataTools.WeldVertices(mesh.vertices));
                    if (boundaryIndices == null) boundaryIndices = new List<int>();
                    boundaryIndices.Clear();
                    foreach(var edgeData in openEdgeData) if (edgeData.IsOpenEdge())
                        {
                            foreach(var openEdge in edgeData.openEdges)
                            {
                                if (!boundaryIndices.Contains(openEdge.rootIndex)) boundaryIndices.Add(openEdge.rootIndex);
                            }
                        }

                    if (meshQuads == null) meshQuads = new List<MeshDataTools.MeshQuad>();
                    meshQuads.Clear();
                    meshQuads = MeshDataTools.CalculateMeshQuads(triangles, meshQuads);
                }

                mesh.name = $"{name}_softPlaneMesh";
            }

            if (triangles == null) triangles = mesh.triangles;
            vertexTriangles = triangles;

            var vertices = mesh.vertices;
            vertexTransforms = new Transform[vertices.Length];
            Rigidbody[] rigidbodies = new Rigidbody[vertices.Length];
            for (int a = 0; a < vertices.Length; a++)
            {
                var localPos = vertices[a];

                var obj = new GameObject($"{jointPrefix}_{a}");
                obj.layer = colliderLayer;

                var objT = obj.transform;
                vertexTransforms[a] = objT;

                objT.SetParent(transform);
                objT.localPosition = localPos;
                objT.localRotation = Quaternion.identity;
                objT.localScale = Vector3.one;

                var objRB = obj.AddComponent<Rigidbody>();
                objRB.mass = nodeMass;
                objRB.useGravity = false;
                objRB.isKinematic = false;
                objRB.collisionDetectionMode = collisionDetectionMode;
                objRB.automaticCenterOfMass = false;
                objRB.automaticInertiaTensor = false;
                
                objRB.drag = drag; 
                objRB.angularDrag = 1f;
                objRB.useGravity = useGravity;

                rigidbodies[a] = objRB;
            }

            vertexBoundaryStates = new bool[vertices.Length];
            foreach (var index in boundaryIndices) 
            {
                vertexBoundaryStates[index] = true;
                rigidbodies[index].isKinematic = true;
            }

            Matrix4x4[] bindpose = new Matrix4x4[vertexTransforms.Length];
            for (int a = 0; a < bindpose.Length; a++) bindpose[a] = vertexTransforms[a].worldToLocalMatrix * transform.localToWorldMatrix; 

            mesh.bindposes = bindpose;

            using(var boneWeights = new NativeArray<BoneWeight1>(vertices.Length, Allocator.Persistent))
            {
                using (var boneCounts = new NativeArray<byte>(vertices.Length, Allocator.Persistent))
                {
                    var boneWeights_ = boneWeights;
                    var boneCounts_ = boneCounts;

                    for (int a = 0; a < vertices.Length; a++)
                    {
                        boneWeights_[a] = new BoneWeight1()
                        {
                            boneIndex = a,
                            weight = 1f
                        };
                        boneCounts_[a] = 1; 
                    }

                    mesh.SetBoneWeights(boneCounts_, boneWeights_);  
                }
            }

            renderer = gameObject.AddOrGetComponent<SkinnedMeshRenderer>();
            renderer.bones = vertexTransforms;
            renderer.sharedMesh = mesh;
            if (materials != null && materials.Length > 0) renderer.sharedMaterials = materials;

            if (syncRendererBoundsWithTriggerBounds) renderer.localBounds = new Bounds()
            {
                center = trigger.center,
                size = trigger.size
            };

            void AddConnectionJoint(Rigidbody root, Rigidbody connector)
            {
                var joint = connector.gameObject.AddComponent<ConfigurableJoint>();
                joint.autoConfigureConnectedAnchor = true;
                joint.connectedBody = root;

                joint.angularXMotion = ConfigurableJointMotion.Locked;
                joint.angularYMotion = ConfigurableJointMotion.Locked;
                joint.angularZMotion = ConfigurableJointMotion.Locked;

                joint.xMotion = ConfigurableJointMotion.Limited;
                joint.yMotion = ConfigurableJointMotion.Limited;
                joint.zMotion = ConfigurableJointMotion.Limited;

                if (useLimitSpring)
                {
                    joint.linearLimitSpring = new SoftJointLimitSpring()
                    {
                        spring = limitSpring,
                        damper = limitDamper
                    };
                }

                joint.linearLimit = new SoftJointLimit()
                {
                    bounciness = bounciness,
                    contactDistance = minDistance,
                    limit = maxDistance
                };

                joint.enablePreprocessing = false; 
            }
            foreach(var quad in meshQuads)
            {
                void EvaluateIndex(int index)
                {
                    if (index != quad.sharedEdge0 && index != quad.sharedEdge1)
                    {
                        var i0RB = rigidbodies[index];

                        var i1RB = rigidbodies[quad.sharedEdge0];
                        var i2RB = rigidbodies[quad.sharedEdge1];

                        AddConnectionJoint(i0RB, i1RB);
                        AddConnectionJoint(i0RB, i2RB); 
                    }
                }

                EvaluateIndex(quad.i0);
                EvaluateIndex(quad.i1);
                EvaluateIndex(quad.i2);
                EvaluateIndex(quad.i3);
            }
        }

        public struct ColliderInBounds
        {
            public Collider collider;
            public Transform transform;
            public GameObjectState goState;

            public BoxCollider surfaceCollider;
            public BoxCollider[] surfaceColliders;
        }
        private Dictionary<GameObject, ColliderInBounds> collidersInBounds = new Dictionary<GameObject, ColliderInBounds>();
        protected void OnColliderExit(GameObject obj)
        {
            if (collidersInBounds.TryGetValue(obj, out var colliderInBounds))
            {
                OnColliderExit(colliderInBounds);
            }
        }
        protected void OnColliderExit(ColliderInBounds colliderInBounds)
        {
            if (registeredMesh != null)
            {
                if (colliderInBounds.surfaceCollider != null)
                {
                    registeredMesh.UnregisterColliderTransform(colliderInBounds.surfaceCollider.transform);
                    UnclaimBoxCollider(colliderInBounds.surfaceCollider);
                }
                if (colliderInBounds.surfaceColliders != null)
                {
                    for(int a = 0; a < colliderInBounds.surfaceColliders.Length; a++)
                    {
                        var collider = colliderInBounds.surfaceColliders[a];

                        if (collider != null)
                        {
                            registeredMesh.UnregisterColliderTransform(collider.transform);
                            UnclaimBoxCollider(collider);
                        }
                    }
                }

                registeredMesh.UnregisterTargetTransform(colliderInBounds.transform); 
            }

            collidersInBounds.Remove(colliderInBounds.collider.gameObject);

            StopIgnoringCollisionWithBaseColliders(colliderInBounds.collider);
        }
        protected void UnclaimBoxCollider(BoxCollider collider)
        {
            if (setIgnoreCollisionBetweenIndividualLocalColliders) StopIgnoringCollisionWithLocalColliders(collider);
            SoftPlaneColliderMeshUpdater.UnclaimBoxCollider(collider);
        }

        private static readonly Vector3[] boundingVectors = new Vector3[]
        {
            Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back
        };
        private static readonly List<BoxCollider> tempBoxColliders = new List<BoxCollider>();
        private void OnTriggerEnter(Collider collider)
        {
            if (!useSurfaceColliders || registeredMesh == null || collider.name == SoftPlaneColliderMeshUpdater.boxColliderName || collider.name.StartsWith(jointPrefix)) return;

            var colliderGO = collider.gameObject;
            if (collidersInBounds.ContainsKey(colliderGO)) return;

            var colliderBounds = collider.bounds;
            var colliderCenter = colliderBounds.center;
            var colliderSize = math.abs(colliderBounds.size);
            float maxSize = Mathf.Max(colliderSize.x, colliderSize.y, colliderSize.z);

            if (baseColliders != null && baseColliders.Count > 0)
            {
                if (maxSize > bigObjectSurfaceCollisionScaleMax) return; // don't generate surface colliders for very big objects (only if this component has spawned base colliders)
            }

            var colliderTransform = collider.transform;
            var goState = colliderGO.AddOrGetComponent<GameObjectState>(); 

            goState.ListenOnDisable(OnColliderExit);

            var cib = new ColliderInBounds()
            {
                collider = collider,
                transform = colliderTransform,
                goState = goState
            };

            if (maxSize > bigObjectSurfaceCollisionScaleMin)
            {
                tempBoxColliders.Clear();
                void CreateBoundingCollider(Vector3 initialOffset, Vector3 boundingReferencePosition, bool calculateOffset)
                {
                    BoxCollider boxCollider;
                    if (calculateOffset)
                    {
                        Vector3 offsetWorld = collider.ClosestPoint(boundingReferencePosition);
                        Vector3 offset = cib.transform.InverseTransformPoint(offsetWorld);
                        float offsetMag = offsetWorld.magnitude; 

                        if (offsetMag > 0)
                        {
                            int pointCount = Mathf.Max(1, Mathf.FloorToInt(offsetMag / bigObjectSurfaceCollisionScaleInterval));

                            for (int a = 0; a < pointCount; a++)
                            {
                                float b = (a + 1f) / pointCount;

                                boxCollider = SoftPlaneColliderMeshUpdater.ClaimNewBoxCollider();
                                tempBoxColliders.Add(boxCollider);
                                registeredMesh.RegisterColliderTransform(boxCollider.transform, cib.transform, initialOffset + (offset * b), boxCollider, false);
                                if (setIgnoreCollisionBetweenIndividualLocalColliders) IgnoreCollisionWithLocalColliders(boxCollider);
                            }

                            return;
                        }
                    }

                    boxCollider = SoftPlaneColliderMeshUpdater.ClaimNewBoxCollider();
                    tempBoxColliders.Add(boxCollider);
                    registeredMesh.RegisterColliderTransform(boxCollider.transform, cib.transform, initialOffset, boxCollider, false);
                    if (setIgnoreCollisionBetweenIndividualLocalColliders) IgnoreCollisionWithLocalColliders(boxCollider);
                }

                //CreateBoundingCollider(trigger.bounds.center, true);
                CreateBoundingCollider(cib.transform.InverseTransformPoint(colliderCenter), Vector3.zero, false);  
                for (int a = 0; a < boundingVectors.Length; a++)
                {
                    CreateBoundingCollider(Vector3.zero, colliderCenter + cib.transform.TransformDirection(boundingVectors[a]) * maxSize, true);
                }

                cib.surfaceColliders = tempBoxColliders.ToArray();
                tempBoxColliders.Clear(); 
            } 
            else
            {
                cib.surfaceCollider = SoftPlaneColliderMeshUpdater.ClaimNewBoxCollider(); 
                registeredMesh.RegisterColliderTransform(cib.surfaceCollider.transform, cib.transform, cib.transform.InverseTransformPoint(colliderCenter), cib.surfaceCollider, true);
                if (setIgnoreCollisionBetweenIndividualLocalColliders) IgnoreCollisionWithLocalColliders(cib.surfaceCollider);
            }

            collidersInBounds[colliderGO] = cib;
            if (normalSizedObjectsIgnoreCollisionWithBaseColliders) IgnoreCollisionWithBaseColliders(collider); 
        }

        // Remember: deactivating or destroying a Collider while it is inside a trigger volume will not register an on exit event.
        private void OnTriggerExit(Collider collider) 
        {
            if (registeredMesh == null || collider.name == SoftPlaneColliderMeshUpdater.boxColliderName || collider.name.StartsWith(jointPrefix)) return;
             
            OnColliderExit(collider.gameObject);  
        }
    }

    public class SoftPlaneColliderMeshUpdater : SingletonBehaviour<SoftPlaneColliderMeshUpdater>, IDisposable
    {
        public const string boxColliderName = "surfaceCollider_temp";

        private readonly List<BoxCollider> colliderPool = new List<BoxCollider>();

        public BoxCollider ClaimNewBoxColliderLocal(bool activate = true)
        {
            if (colliderPool.Count <= 0)
            {
                var collider = new GameObject(boxColliderName).AddComponent<BoxCollider>();
                collider.transform.SetParent(transform, false);
                collider.transform.position = new Vector3(0f, -99999f, 0f); 

                return collider;
            }

            var pop = colliderPool[0];
            colliderPool.RemoveAt(0);

            if (activate) pop.gameObject.SetActive(true); 

            return pop;
        }
        public static BoxCollider ClaimNewBoxCollider()
        {
            var instance = Instance;
            if (instance == null) return null;

            return instance.ClaimNewBoxColliderLocal();
        }

        public void UnclaimBoxColliderLocal(BoxCollider boxCollider)
        {
            if (boxCollider == null) return;

            boxCollider.name = boxColliderName;
            boxCollider.gameObject.layer = gameObject.layer; 
            boxCollider.transform.SetParent(transform, false);
            boxCollider.transform.position = new Vector3(0f, -99999f, 0f);
            colliderPool.Add(boxCollider);
            boxCollider.gameObject.SetActive(false);
        }
        public static void UnclaimBoxCollider(BoxCollider boxCollider)
        {
            var instance = InstanceOrNull;
            if (instance == null) return;

            instance.UnclaimBoxColliderLocal(boxCollider);
        }

        public void Dispose()
        {
            EnsureJobCompletion();

            if (registeredMeshes != null)
            {
                foreach(var rm in registeredMeshes)
                {
                    if (rm == null) continue;
                    rm.Dispose(false); // avoid removing data from lists that are about to be disposed
                }

                registeredMeshes.Clear();
                registeredMeshes = null;
            }

            if (triggerBounds.IsCreated)
            {
                triggerBounds.Dispose();
                triggerBounds = default;
            }

            if (triangleIndices.IsCreated)
            {
                triangleIndices.Dispose();
                triangleIndices = default; 
            }

            if (rigidBodiesToQuery != null)
            {
                rigidBodiesToQuery.Clear();
                rigidBodiesToQuery = null;
            }

            if (transformsToQuery.isCreated)
            {
                transformsToQuery.Dispose();
                transformsToQuery = default;
            }

            if (transformStates.IsCreated)
            {
                transformStates.Dispose();
                transformStates = default;
            }

            if (colliderTransforms.isCreated)
            {
                colliderTransforms.Dispose();
                colliderTransforms = default;
            }

            if (colliderSyncs.IsCreated)
            {
                colliderSyncs.Dispose();
                colliderSyncs = default;
            }

            if (localColliderTransforms.isCreated)
            {
                localColliderTransforms.Dispose();
                localColliderTransforms = default;
            }

            if (localColliderSyncs.IsCreated)
            {
                localColliderSyncs.Dispose();
                localColliderSyncs = default;
            }
        }

        protected void OnDestroy()
        {
            Dispose();
        }

        private SubstepPhysicsBehaviourProxy physicsBehaviour;
        protected override void OnAwake()
        {
            base.OnAwake();

            if (registeredMeshes == null) registeredMeshes = new List<RegisteredMesh>();

            triggerBounds = new NativeList<TriggerBounds>(64, Allocator.Persistent);
            triangleIndices = new NativeList<TriangleVertex>(64, Allocator.Persistent);
            rigidBodiesToQuery = new List<Rigidbody>();
            transformsToQuery = new TransformAccessArray(64, -1);
            transformStates = new NativeList<TransformDataWorldLocalAndMatrix>(64, Allocator.Persistent);
            colliderTransforms = new TransformAccessArray(64, -1);
            colliderSyncs = new NativeList<ColliderSync>(64, Allocator.Persistent);
            localColliderTransforms = new TransformAccessArray(64, -1);
            localColliderSyncs = new NativeList<LocalColliderSync>(64, Allocator.Persistent); 
        }

        protected void Start()
        {
            var physicsManager = SubstepPhysicsManager.InstanceOrNull;
            if (physicsManager != null) physicsBehaviour = gameObject.AddOrGetComponent<SubstepPhysicsBehaviourProxy>();
            if (physicsBehaviour != null)
            {
                if (physicsBehaviour.OnEarlyPhysicsUpdate == null) physicsBehaviour.OnEarlyPhysicsUpdate = new UnityEngine.Events.UnityEvent();
                physicsBehaviour.OnEarlyPhysicsUpdate.AddListener(UpdatePhysics);
            }
        }

        public class RegisteredMesh : IDisposable
        {
            protected SoftPlaneColliderMeshUpdater updater;
            public SoftPlaneColliderMeshUpdater Updater => updater;

            protected SoftPlaneColliderMesh owner;
            public SoftPlaneColliderMesh Owner => owner;

            public bool IsValid => updater != null && owner != null;

            protected int2 triangleIndexRange = -1;
            protected int2 vertexTransformIndexRange = -1;

            public class RegisteredTransform 
            {
                public Transform transform;
                public BoxCollider boxCollider;
                public int jobIndex;
            }

            protected List<RegisteredTransform> targetTransforms = new List<RegisteredTransform>();
            protected List<RegisteredTransform> colliderTransforms = new List<RegisteredTransform>();
            protected List<RegisteredTransform> localColliderTransforms = new List<RegisteredTransform>();

            public int TargetTransformCount => targetTransforms == null ? 0 : targetTransforms.Count;
            public RegisteredTransform GetTargetTransform(int index) => targetTransforms[index];

            public int ColliderTransformCount => colliderTransforms == null ? 0 : colliderTransforms.Count;
            public RegisteredTransform GetColliderTransform(int index) => colliderTransforms[index];

            public int LocalColliderTransformCount => localColliderTransforms == null ? 0 : localColliderTransforms.Count;
            public RegisteredTransform GetLocalColliderTransform(int index) => localColliderTransforms[index];

            public int JobIndexOfTargetTransform(Transform t)
            {
                if (targetTransforms == null) return -1;
                for (int a = 0; a < targetTransforms.Count; a++)
                {
                    var rt = targetTransforms[a];
                    if (ReferenceEquals(rt.transform, t)) return rt.jobIndex;
                }
                return -1;
            }
            public int JobIndexOfColliderTransform(Transform t)
            {
                if (colliderTransforms == null) return -1;
                for (int a = 0; a < colliderTransforms.Count; a++)
                {
                    var rt = colliderTransforms[a];
                    if (ReferenceEquals(rt.transform, t)) return rt.jobIndex;
                }
                return -1;
            }
            public int JobIndexOfLocalColliderTransform(Transform t)
            {
                if (localColliderTransforms == null) return -1;
                for (int a = 0; a < localColliderTransforms.Count; a++)
                {
                    var rt = localColliderTransforms[a];
                    if (ReferenceEquals(rt.transform, t)) return rt.jobIndex;
                }
                return -1;
            }

            public int LocalIndexOfTargetTransform(Transform t)
            {
                if (targetTransforms == null) return -1;
                for(int a = 0; a < targetTransforms.Count; a++)
                {
                    var rt = targetTransforms[a];
                    if (ReferenceEquals(rt.transform, t)) return a;
                }
                return -1;
            }
            public int LocalIndexOfColliderTransform(Transform t)
            {
                if (colliderTransforms == null) return -1;
                for (int a = 0; a < colliderTransforms.Count; a++)
                {
                    var rt = colliderTransforms[a];
                    if (ReferenceEquals(rt.transform, t)) return a;
                }
                return -1;
            }
            public int LocalIndexOfLocalColliderTransform(Transform t)
            {
                if (localColliderTransforms == null) return -1;
                for (int a = 0; a < localColliderTransforms.Count; a++)
                {
                    var rt = localColliderTransforms[a];
                    if (ReferenceEquals(rt.transform, t)) return a;
                }
                return -1;
            }

            public bool ContainsTargetTransform(Transform t) => LocalIndexOfTargetTransform(t) >= 0;
            public bool ContainsColliderTransform(Transform t) => LocalIndexOfColliderTransform(t) >= 0;
            public bool ContainsLocalColliderTransform(Transform t) => LocalIndexOfLocalColliderTransform(t) >= 0;

            public void SyncColliders(NativeList<ColliderSync> colliderSyncs, TransformAccessArray targetTransforms, List<Rigidbody> targetRigidbodies)
            {
                if (colliderTransforms != null && owner != null)
                {

                    foreach (var colliderTransform in colliderTransforms)
                    {
                        if (colliderTransform.jobIndex < 0) continue;

                        var sync = colliderSyncs[colliderTransform.jobIndex];

                        if (sync.parentVertexTransformIndex >= 0)
                        {
                            var parent = targetTransforms[sync.parentVertexTransformIndex];
                            if (colliderTransform.transform.parent != parent)
                            {
                                colliderTransform.transform.gameObject.layer = parent.gameObject.layer;
                                colliderTransform.transform.SetParent(parent, true); 

                                if (colliderTransform.boxCollider != null)
                                {
                                    if (!colliderTransform.boxCollider.enabled) 
                                    {
                                        colliderTransform.boxCollider.enabled = true; 
                                        if (owner.setIgnoreCollisionBetweenIndividualLocalColliders) owner.IgnoreCollisionWithLocalColliders(colliderTransform.boxCollider); // apparently Physics.IgnoreCollision gets reset when the collider is disabled
                                    }
                                    colliderTransform.boxCollider.size = math.abs(new float3(owner.surfaceColliderSize, owner.surfaceColliderSize, owner.surfaceColliderThickness));
                                    colliderTransform.boxCollider.center = new Vector3(0, 0, -owner.surfaceColliderThickness * 0.5f);
                                }
                            }
                        } 
                        else if (colliderTransform.transform.parent != null)
                        {
                            colliderTransform.transform.SetParent(null, false);
                            //colliderTransform.transform.position = new Vector3(0, -99999, 0);
                            if (colliderTransform.boxCollider != null) colliderTransform.boxCollider.enabled = false;
                        }
                        
                        if (owner.forceObjectsAboveSurfaceColliders && sync.allowRepositioning && sync.repositionTarget && sync.targetTransformIndex >= 0)  
                        {
                            var targetTransform = targetTransforms[sync.targetTransformIndex];
                            var targetRB = targetRigidbodies[sync.targetTransformIndex];
                            if (targetRB != null)
                            {
                                if (targetTransform.gameObject == targetRB.gameObject)
                                {
                                    targetRB.position = sync.targetPosition;
                                }
                                else
                                {
                                    targetRB.position = targetRB.position + ((Vector3)sync.targetPosition - targetTransform.position); 
                                }
                            }
                        }
                    }
                }
            }

            public void SyncLocalColliders(NativeList<LocalColliderSync> localColliderSyncs, float deltaTime) 
            {
                if (colliderTransforms != null && owner != null)
                {

                    float growSpeed = math.select(1, math.saturate(deltaTime * owner.baseColliderSyncSpeed), owner.baseColliderSyncSpeed > 0);

                    foreach (var localColliderTransform in localColliderTransforms)
                    {
                        if (localColliderTransform.jobIndex < 0) continue;

                        var sync = localColliderSyncs[localColliderTransform.jobIndex];

                        if (localColliderTransform.boxCollider != null)
                        {
                            //localColliderTransform.boxCollider.size = sync.size;
                            localColliderTransform.boxCollider.size = Vector3.LerpUnclamped(localColliderTransform.boxCollider.size, sync.size, growSpeed);

                            // TODO: get scaleResponse of neighbors and use it to influence the grow speed here
                        }
                    }
                }
            }


            private int index = -1;
            public int Index => index;

            private int jobIndex = -1;
            public int JobIndex => jobIndex;

            protected readonly bool useDynamicBounds;
            public bool UseDynamicBounds => useDynamicBounds;

            public void SetTriggerBounds(TriggerBounds bounds)
            {
                if (owner == null) return;
                owner.SetTriggerBounds(bounds);
            }

            public RegisteredMesh(SoftPlaneColliderMeshUpdater updater, SoftPlaneColliderMesh owner, bool useDynamicBounds)
            {
                triangleIndexRange = -1;
                vertexTransformIndexRange = -1;

                this.updater = updater;
                this.owner = owner;

                this.useDynamicBounds = useDynamicBounds;

                var vertexTransforms = owner.VertexTransforms;

                updater.EnsureJobCompletion();

                if (vertexTransforms != null)
                {
                    vertexTransformIndexRange.x = updater.transformsToQuery.length;
                    for (int a = 0; a < vertexTransforms.Length; a++)
                    {
                        var transform = vertexTransforms[a];
                        updater.rigidBodiesToQuery.Add(transform.GetComponentInParent<Rigidbody>(true));
                        updater.transformsToQuery.Add(transform);
                        updater.transformStates.Add(new TransformDataWorldLocalAndMatrix() { toWorld = transform.localToWorldMatrix, position = transform.position, rotation = transform.rotation, localPosition = transform.localPosition, localRotation = transform.localRotation });  
                    }
                    vertexTransformIndexRange.y = updater.transformsToQuery.length - 1;

                    var vertexTransformTriangles = owner.VertexTransformTriangles;
                    var vertexBoundaryStates = owner.VertexBoundaryStates;
                    if (vertexTransformTriangles != null && vertexBoundaryStates != null)
                    {
                        triangleIndexRange.x = updater.triangleIndices.Length;
                        for (int a = 0; a < vertexTransformTriangles.Length; a++)
                        {
                            int localIndex = vertexTransformTriangles[a];
                            updater.triangleIndices.Add(new TriangleVertex() 
                            { 
                                vertexTransformIndex = vertexTransformIndexRange.x + localIndex,
                                isEdge = vertexBoundaryStates[localIndex] 
                            }); 
                        }
                        triangleIndexRange.y = updater.triangleIndices.Length - 1; 
                    }
                }

                jobIndex = -1;
                if (useDynamicBounds)
                {
                    jobIndex = updater.triggerBounds.Length;
                    updater.triggerBounds.Add(new TriggerBounds()
                    {
                        extensionWidth = owner.triggerExpansionWidth,
                        vertexTransformIndexRange = vertexTransformIndexRange 
                    });
                } 
                
                index = updater.registeredMeshes.Count;
                updater.registeredMeshes.Add(this);
            } 

            public int RegisterTargetTransform(Transform transform)
            {
                var index = LocalIndexOfTargetTransform(transform);
                if (index >= 0) return index;

                if (updater == null || targetTransforms == null || transform == null) return -1;

                index = targetTransforms.Count;

                updater.EnsureJobCompletion();

                var rt = new RegisteredTransform();
                rt.transform = transform;
                rt.jobIndex = updater.transformsToQuery.length;
                targetTransforms.Add(rt);
                updater.rigidBodiesToQuery.Add(transform.GetComponentInParent<Rigidbody>(true));
                updater.transformsToQuery.Add(transform);
                updater.transformStates.Add(new TransformDataWorldLocalAndMatrix() { toWorld = transform.localToWorldMatrix, position = transform.position, rotation = transform.rotation, localPosition = transform.localPosition, localRotation = transform.localRotation });

                return index;
            }
            public void UnregisterTargetTransform(Transform transform)
            {
                if (updater == null || targetTransforms == null) return;

                updater.EnsureJobCompletion();

                for (int a = 0; a < targetTransforms.Count; a++)
                {
                    var rt = targetTransforms[a];
                    if (rt.transform != transform) continue;

                    targetTransforms.RemoveAt(a);
                    updater.RemoveTransformIndices(rt.jobIndex, 1);
                    break;
                }
            }
            public int RegisterColliderTransform(Transform colliderTransform, Transform targetTransform, float3 targetOffset, BoxCollider collider, bool allowTargetRepositioning)
            {
                var index = LocalIndexOfColliderTransform(colliderTransform);
                if (index >= 0) return index;

                if (owner == null || updater == null || colliderTransforms == null || colliderTransform == null || targetTransform == null) return -1;

                var targetLocalIndex = RegisterTargetTransform(targetTransform);
                if (targetLocalIndex < 0) return -1;

                var registeredTargetTransform = targetTransforms[targetLocalIndex]; 
                
                index = colliderTransforms.Count;

                updater.EnsureJobCompletion();

                bool isAbove = Vector3.Dot(owner.NormalDirectionWorld, targetTransform.position - owner.transform.position) >= 0;

                var rt = new RegisteredTransform();
                rt.transform = colliderTransform;
                rt.boxCollider = collider;
                rt.jobIndex = updater.colliderTransforms.length;
                colliderTransforms.Add(rt); 
                updater.colliderTransforms.Add(colliderTransform);
                updater.colliderSyncs.Add(new ColliderSync() 
                {
                    allowRepositioning = allowTargetRepositioning,
                    isAbove = isAbove ? 1f : -1f, 
                    targetOffset = targetOffset,
                    targetTransformIndex = registeredTargetTransform.jobIndex,
                    vertexTransformIndexRange = vertexTransformIndexRange,
                    vertexTransformTriangleIndexRange = triangleIndexRange
                });

                return index;
            }
            public void UnregisterColliderTransform(Transform transform)
            {
                if (updater == null || colliderTransforms == null) return;

                updater.EnsureJobCompletion();

                for (int a = 0; a < colliderTransforms.Count; a++)
                {
                    var rt = colliderTransforms[a];
                    if (rt.transform != transform) continue;

                    colliderTransforms.RemoveAt(a);
                    updater.RemoveColliderTransformIndices(rt.jobIndex, 1);
                    break;
                }
            }

            public int RegisterLocalColliderTransform(Transform jointParentTransform, Transform localColliderTransform, BoxCollider collider, int4 quadIndices, float thickness)
            {
                var index = LocalIndexOfLocalColliderTransform(localColliderTransform);
                if (index >= 0) return index;

                if (updater == null || localColliderTransforms == null || localColliderTransform == null) return -1;

                quadIndices = quadIndices + vertexTransformIndexRange.x;

                var p1 = updater.transformsToQuery[quadIndices.x].localPosition;
                var p2 = updater.transformsToQuery[quadIndices.y].localPosition;
                var p3 = updater.transformsToQuery[quadIndices.z].localPosition;
                var p4 = updater.transformsToQuery[quadIndices.w].localPosition;

                var startNormal = (Maths.CalcNormal(p1, p2, p4) + Maths.CalcNormal(p2, p3, p4)).normalized;
                var startUp = ((p1 - p4) + (p2 - p3)).normalized;

                Quaternion rotA = Quaternion.FromToRotation(Vector3.forward, jointParentTransform.TransformDirection(startNormal));
                Quaternion rotB = Quaternion.FromToRotation(rotA * Vector3.up, jointParentTransform.TransformDirection(startUp));
                Quaternion rotC = rotB * rotA;
                localColliderTransform.rotation = rotC;
                var startRot = Quaternion.Inverse(jointParentTransform.rotation) * rotC;  

                index = localColliderTransforms.Count;

                updater.EnsureJobCompletion();

                var rt = new RegisteredTransform();
                rt.transform = localColliderTransform;
                rt.boxCollider = collider;
                rt.jobIndex = updater.localColliderTransforms.length;
                localColliderTransforms.Add(rt);
                updater.localColliderTransforms.Add(localColliderTransform);
                updater.localColliderSyncs.Add(new LocalColliderSync()
                {
                    thickness = thickness,
                    quadIndices = quadIndices,
                    startNormal = startNormal,
                    startUp = startUp,
                    startRotation = startRot,

                    syncSpeed = owner.baseColliderSyncSpeed,
                    growSpeed = owner.baseColliderGrowSpeed,
                    scale = owner.baseColliderScale,
                    minScale = owner.baseColliderMinScale,
                    maxScale = owner.baseColliderMaxScale
                });

                return index;
            }
            public void UnregisterLocalColliderTransform(Transform transform)
            {
                if (updater == null || localColliderTransforms == null) return;

                updater.EnsureJobCompletion();

                for (int a = 0; a < localColliderTransforms.Count; a++)
                {
                    var rt = localColliderTransforms[a];
                    if (rt.transform != transform) continue;

                    localColliderTransforms.RemoveAt(a);
                    updater.RemoveLocalColliderTransformIndices(rt.jobIndex, 1);
                    break;
                }
            }

            public void NotifyMeshRemoval(int meshIndex, int meshJobIndex)
            {
                if (meshIndex >= 0 && index >= meshIndex) index -= 1;
                if (meshJobIndex >= 0 && jobIndex >= meshJobIndex) jobIndex -= 1; 
            }
            public void NotifyTriangleIndicesRemoval(int startIndex, int count)
            {
                if (startIndex < 0) return;

                if (triangleIndexRange.x >= startIndex)
                {
                    triangleIndexRange.x = triangleIndexRange.x - count;
                    triangleIndexRange.y = triangleIndexRange.y - count;
                }
            }
            public void NotifyTransformIndicesRemoval(int startIndex, int count)
            {
                if (startIndex < 0) return;

                if (vertexTransformIndexRange.x >= startIndex)
                {
                    vertexTransformIndexRange.x = vertexTransformIndexRange.x - count;
                    vertexTransformIndexRange.y = vertexTransformIndexRange.y - count;
                }

                for(int a = 0; a < targetTransforms.Count; a++)
                {
                    var index = targetTransforms[a];
                    if (index.jobIndex >= startIndex) index.jobIndex -= count;
                    targetTransforms[a] = index;
                }
            }
            public void NotifyColliderTransformIndicesRemoval(int startIndex, int count)
            {
                if (startIndex < 0) return;

                for (int a = 0; a < colliderTransforms.Count; a++)
                {
                    var index = colliderTransforms[a];
                    if (index.jobIndex >= startIndex) index.jobIndex -= count;
                    colliderTransforms[a] = index;
                }
            }

            public void NotifyLocalColliderTransformIndicesRemoval(int startIndex, int count)
            {
                if (startIndex < 0) return; 

                for (int a = 0; a < localColliderTransforms.Count; a++)
                {
                    var index = localColliderTransforms[a];
                    if (index.jobIndex >= startIndex) index.jobIndex -= count;
                    localColliderTransforms[a] = index;
                }
            }

            public void Dispose(bool removeFromUpdater)
            {
                if (!removeFromUpdater) updater = null;
                Dispose();
            }
            public void Dispose()
            {
                if (updater != null)
                {
                    updater.EnsureJobCompletion();

                    updater.RemoveMesh(this);
                    
                    if (triangleIndexRange.x >= 0)
                    {
                        var triangleCount = (triangleIndexRange.y - triangleIndexRange.x) + 1; 
                        updater.RemoveTriangleIndices(triangleIndexRange.x, triangleCount);    
                    }
                    
                    if (vertexTransformIndexRange.x >= 0) 
                    {
                        var vertexTransformCount = (vertexTransformIndexRange.y - vertexTransformIndexRange.x) + 1;
                        updater.RemoveTransformIndices(vertexTransformIndexRange.x, vertexTransformCount);
                        NotifyTransformIndicesRemoval(vertexTransformIndexRange.x, vertexTransformCount); // make sure indices in targetTransforms list are updated accordingly
                    }

                    if (targetTransforms != null)
                    {
                        for (int a = 0; a < targetTransforms.Count; a++)
                        {
                            var index = targetTransforms[a];
                            updater.RemoveTransformIndices(index.jobIndex, 1);
                            NotifyTransformIndicesRemoval(index.jobIndex, 1); // make sure next indices are updated accordingly
                        }
                    }
                    if (colliderTransforms != null)
                    {
                        for (int a = 0; a < colliderTransforms.Count; a++)
                        {
                            var index = colliderTransforms[a];
                            updater.RemoveColliderTransformIndices(index.jobIndex, 1);  
                            NotifyColliderTransformIndicesRemoval(index.jobIndex, 1); // make sure next indices are updated accordingly
                        }
                    }
                    if (localColliderTransforms != null)
                    {
                        for (int a = 0; a < localColliderTransforms.Count; a++)
                        {
                            var index = localColliderTransforms[a];
                            updater.RemoveLocalColliderTransformIndices(index.jobIndex, 1);
                            NotifyLocalColliderTransformIndicesRemoval(index.jobIndex, 1); // make sure next indices are updated accordingly
                        }
                    }
                }

                updater = null;
                owner = null;

                if (targetTransforms != null)
                {
                    targetTransforms.Clear();
                    targetTransforms = null;
                }

                if (colliderTransforms != null)
                {
                    colliderTransforms.Clear();
                    colliderTransforms = null;
                }

                if (localColliderTransforms != null)
                {
                    localColliderTransforms.Clear();
                    localColliderTransforms = null;
                }

                triangleIndexRange = -1;
                vertexTransformIndexRange = -1;
            }
        }

        protected List<RegisteredMesh> registeredMeshes = new List<RegisteredMesh>();
        public RegisteredMesh GetRegisteredMeshLocal(int index) => registeredMeshes == null ? null : registeredMeshes[index];
        public static RegisteredMesh GetRegisteredMesh(int index)
        {
            var instance = InstanceOrNull;
            if (instance == null) return null;
            return instance.GetRegisteredMeshLocal(index);
        }
        public int GetRegisteredIndexOfLocal(SoftPlaneColliderMesh mesh)
        {
            if (registeredMeshes == null) return -1;
            for(int a = 0; a < registeredMeshes.Count; a++) if (ReferenceEquals(registeredMeshes[a].Owner, mesh)) return a;
            return -1;
        }
        public static int GetRegisteredIndexOf(SoftPlaneColliderMesh mesh)
        {
            var instance = InstanceOrNull;
            if (instance == null) return -1;
            return instance.GetRegisteredIndexOfLocal(mesh);
        }
        public bool IsRegisteredLocal(SoftPlaneColliderMesh mesh) => GetRegisteredIndexOfLocal(mesh) >= 0;
        public static bool IsRegistered(SoftPlaneColliderMesh mesh)
        {
            var instance = InstanceOrNull;
            if (instance == null) return false;
            return instance.IsRegisteredLocal(mesh);
        }

        [Serializable]
        public struct TriggerBounds
        {
            public float extensionWidth;
            public int2 vertexTransformIndexRange;

            public float3 center;
            public float3 size;
        }
        protected NativeList<TriggerBounds> triggerBounds;

        public void RemoveMesh(RegisteredMesh rm)
        {
            if (registeredMeshes == null || rm == null) return;

            if (rm.Index >= 0) RemoveMesh(rm.Index);
            registeredMeshes.RemoveAll(i => ReferenceEquals(rm, i));  
        }
        public void RemoveMesh(int index)
        {
            if (registeredMeshes == null || !triggerBounds.IsCreated || index < 0) return;

            var rm = registeredMeshes[index];

            registeredMeshes.RemoveAt(index);
            if (rm != null && rm.JobIndex >= 0) triggerBounds.RemoveAt(rm.JobIndex); 

            if (registeredMeshes != null)
            {
                foreach (var rm_ in registeredMeshes)
                {
                    rm_.NotifyMeshRemoval(index, rm == null ? -1 : rm.JobIndex); 
                }
            }
        }

        public struct TriangleVertex
        {
            public int vertexTransformIndex;
            public bool isEdge;
        }
        protected NativeList<TriangleVertex> triangleIndices;
        public void RemoveTriangleIndices(int startIndex, int count)
        {
            if (count <= 0 || registeredMeshes == null || !triangleIndices.IsCreated || startIndex < 0 || startIndex >= triangleIndices.Length) return;

            int count_ = math.min(count, triangleIndices.Length - startIndex);
            triangleIndices.RemoveRange(startIndex, count_);

            if (colliderSyncs.IsCreated)
            {
                for (int a = 0; a < colliderSyncs.Length; a++)
                {
                    var sync = colliderSyncs[a];
                    if (sync.vertexTransformTriangleIndexRange.x >= startIndex)
                    {
                        sync.vertexTransformTriangleIndexRange.x = sync.vertexTransformTriangleIndexRange.x - count_;
                        sync.vertexTransformTriangleIndexRange.y = sync.vertexTransformTriangleIndexRange.y - count_;

                        colliderSyncs[a] = sync; 
                    }
                }
            }

            if (registeredMeshes != null)
            {
                foreach (var rm in registeredMeshes)
                {
                    rm.NotifyTriangleIndicesRemoval(startIndex, count_);
                }
            }
        }

        protected TransformAccessArray transformsToQuery;
        protected List<Rigidbody> rigidBodiesToQuery;
        protected NativeList<TransformDataWorldLocalAndMatrix> transformStates;

        public Transform GetQueryableTransformLocal(int index)
        {
            if (index < 0 || !transformsToQuery.isCreated || index >= transformsToQuery.length) return default;
            return transformsToQuery[index];  
        }
        public static Transform GetQueryableTransform(int index)
        {
            var instance = InstanceOrNull;
            if (instance == null) return default;

            return instance.GetQueryableTransformLocal(index);
        }

        public Rigidbody GetQueryableRigidbodyLocal(int index)
        {
            if (index < 0 || rigidBodiesToQuery == null || index >= rigidBodiesToQuery.Count) return default;
            return rigidBodiesToQuery[index];
        }
        public static Rigidbody GetQueryableRigidbody(int index)
        {
            var instance = InstanceOrNull;
            if (instance == null) return default;

            return instance.GetQueryableRigidbodyLocal(index);
        }

        private readonly List<Transform> tempTransforms = new List<Transform>();
        public void RemoveTransformIndices(int startIndex, int count)
        {
            if (count <= 0 || registeredMeshes == null || !transformsToQuery.isCreated || !transformStates.IsCreated || startIndex < 0 || startIndex >= transformsToQuery.length) return;

            tempTransforms.Clear();
            for (int a = 0; a < transformsToQuery.length; a++) tempTransforms.Add(transformsToQuery[a]); 

            int count_ = math.min(count, transformsToQuery.length - startIndex);
            rigidBodiesToQuery.RemoveRange(startIndex, count_);
            tempTransforms.RemoveRange(startIndex, count_);
            transformStates.RemoveRange(startIndex, count_);

            transformsToQuery.SetTransforms(tempTransforms.ToArray());
            tempTransforms.Clear();

            if (triangleIndices.IsCreated)
            {
                for(int a = 0; a < triangleIndices.Length; a++)
                {
                    var tIndex = triangleIndices[a];
                    if (tIndex.vertexTransformIndex >= startIndex)
                    {
                        tIndex.vertexTransformIndex = tIndex.vertexTransformIndex - count_;
                        triangleIndices[a] = tIndex;  
                    }
                }
            }

            if (colliderSyncs.IsCreated)
            {
                for (int a = 0; a < colliderSyncs.Length; a++)
                {
                    var sync = colliderSyncs[a];

                    if (sync.vertexTransformIndexRange.x >= startIndex)
                    {
                        sync.vertexTransformIndexRange.x = sync.vertexTransformIndexRange.x - count_;
                        sync.vertexTransformIndexRange.y = sync.vertexTransformIndexRange.y - count_;
                    }
                    if (sync.targetTransformIndex >= startIndex)
                    {
                        sync.targetTransformIndex = sync.targetTransformIndex - count_;
                    }
                    if (sync.parentVertexTransformIndex >= startIndex)
                    {
                        sync.parentVertexTransformIndex = sync.parentVertexTransformIndex - count_;
                    }

                    colliderSyncs[a] = sync;
                }
            }

            if (localColliderSyncs.IsCreated)
            {
                for (int a = 0; a < localColliderSyncs.Length; a++)
                {
                    var sync = localColliderSyncs[a];

                    if (sync.quadIndices.x >= startIndex)
                    {
                        sync.quadIndices.x = sync.quadIndices.x - count_;
                    }
                    if (sync.quadIndices.y >= startIndex)
                    {
                        sync.quadIndices.y = sync.quadIndices.y - count_;
                    }
                    if (sync.quadIndices.z >= startIndex)
                    {
                        sync.quadIndices.z = sync.quadIndices.z - count_;
                    }
                    if (sync.quadIndices.w >= startIndex)
                    {
                        sync.quadIndices.w = sync.quadIndices.w - count_;
                    }

                    localColliderSyncs[a] = sync;
                }
            }

            if (registeredMeshes != null)
            {
                foreach(var rm in registeredMeshes)
                {
                    rm.NotifyTransformIndicesRemoval(startIndex, count_);
                }
            }
        }

        [Serializable]
        public struct ColliderSync
        {
            public float isAbove;

            public float3 targetOffset;

            public int targetTransformIndex;
            public int2 vertexTransformIndexRange;
            public int2 vertexTransformTriangleIndexRange;

            public int parentVertexTransformIndex;
            public float distanceFromTri;
            public float3 triNormal;

            public bool allowRepositioning;
            public bool repositionTarget;
            public float3 targetPosition;
        }

        [Serializable]
        public struct LocalColliderSync
        {
            public float thickness;
            public int4 quadIndices;
            public float3 startNormal;
            public float3 startUp;
            public quaternion startRotation;

            public float3 startSize;
            public float3 size;

            public float scale;
            public float minScale;
            public float maxScale;

            public float syncSpeed;
            public float growSpeed;

            public float scaleResponse;
        }

        protected TransformAccessArray colliderTransforms;
        protected NativeList<ColliderSync> colliderSyncs;
        public ColliderSync GetColliderSyncLocal(int index)
        {
            if (index < 0 || !colliderSyncs.IsCreated || index >= colliderSyncs.Length) return default;

            EnsureJobCompletion();
            return colliderSyncs[index];
        }
        public static ColliderSync GetColliderSync(int index)
        {
            var instance = InstanceOrNull;
            if (instance == null) return default;

            return instance.GetColliderSyncLocal(index);
        }

        protected TransformAccessArray localColliderTransforms;
        protected NativeList<LocalColliderSync> localColliderSyncs;

        public LocalColliderSync GetLocalColliderSyncLocal(int index)
        {
            if (index < 0 || !localColliderSyncs.IsCreated || index >= localColliderSyncs.Length) return default;

            EnsureJobCompletion();
            return localColliderSyncs[index];
        }
        public static LocalColliderSync GetLocalColliderSync(int index)
        {
            var instance = InstanceOrNull;
            if (instance == null) return default;

            return instance.GetLocalColliderSyncLocal(index);
        }

        public void RemoveColliderTransformIndices(int startIndex, int count)
        {
            if (count <= 0 || registeredMeshes == null || !colliderTransforms.isCreated || !colliderSyncs.IsCreated || startIndex < 0 || startIndex >= colliderTransforms.length) return;

            tempTransforms.Clear();
            for (int a = 0; a < colliderTransforms.length; a++) tempTransforms.Add(colliderTransforms[a]);

            int count_ = math.min(count, colliderTransforms.length - startIndex);
            tempTransforms.RemoveRange(startIndex, count_); 
            colliderSyncs.RemoveRange(startIndex, count_);

            colliderTransforms.SetTransforms(tempTransforms.ToArray());
            tempTransforms.Clear();

            if (registeredMeshes != null)
            {
                foreach (var rm in registeredMeshes)
                {
                    rm.NotifyColliderTransformIndicesRemoval(startIndex, count_); 
                }
            }
        }

        public void RemoveLocalColliderTransformIndices(int startIndex, int count)
        {
            if (count <= 0 || registeredMeshes == null || !localColliderTransforms.isCreated || !localColliderSyncs.IsCreated || startIndex < 0 || startIndex >= localColliderTransforms.length) return;

            tempTransforms.Clear();
            for (int a = 0; a < localColliderTransforms.length; a++) tempTransforms.Add(localColliderTransforms[a]);

            int count_ = math.min(count, localColliderTransforms.length - startIndex);
            tempTransforms.RemoveRange(startIndex, count_);
            localColliderSyncs.RemoveRange(startIndex, count_);

            localColliderTransforms.SetTransforms(tempTransforms.ToArray());
            tempTransforms.Clear();

            if (registeredMeshes != null)
            {
                foreach (var rm in registeredMeshes)
                {
                    rm.NotifyLocalColliderTransformIndicesRemoval(startIndex, count_);
                }
            }
        }

        protected JobHandle currentJobHandle;
        public JobHandle CurrentJobHandle => currentJobHandle;

        public void EnsureJobCompletion()
        {
            currentJobHandle.Complete();
        }
        public void PrepareForNewJob()
        {
            EnsureJobCompletion();
            RemoveNullTransforms();
        }
        private void RemoveNullTransforms()
        {
            bool flag = true;

            if (registeredMeshes != null)
            {
                while (flag)
                {
                    flag = false;
                    foreach (var rm in registeredMeshes) // iterate over registered mesh target transforms. This avoids iterating over vertex transforms (they should never get destroyed unless the registered mesh is also destroyed)
                    {
                        for (int a = 0; a < rm.TargetTransformCount; a++)
                        {
                            var t = rm.GetTargetTransform(a).transform;
                            if (t == null)
                            {
                                flag = true;
                                RemoveTransformIndices(a, 1);
                                break;
                            }
                        }
                    }
                }
            }

            flag = true;
            while (flag)
            {
                flag = false;
                for (int a = 0; a < colliderTransforms.length; a++)
                {
                    var t = colliderTransforms[a];
                    if (t == null)
                    {
                        flag = true;
                        RemoveColliderTransformIndices(a, 1);
                        break;
                    }
                }
            }
        }

        public void UpdatePhysics()
        {
            PrepareForNewJob();

            currentJobHandle = default;

            currentJobHandle = new SharedJobs.FetchTransformDataWithMatrixJob()
            {
                transformData = transformStates.AsArray()
            }.Schedule(transformsToQuery, currentJobHandle);

            currentJobHandle = new CalculateTriggerBoundsJob() 
            {
                transformStates = transformStates,
                triggerBounds = triggerBounds,
            }.Schedule(triggerBounds.Length, 1, currentJobHandle);
            
            if (colliderSyncs.Length > 0)
            {
                currentJobHandle = new PositionSurfaceCollidersJob()
                {
                    transformStates = transformStates,
                    triangleIndices = triangleIndices,
                    colliderSyncs = colliderSyncs
                }.Schedule(colliderTransforms, currentJobHandle);
            }

            if (localColliderSyncs.Length > 0)
            {
                currentJobHandle = new UpdateLocalSurfaceCollidersJob()
                {
                    transformStates = transformStates,
                    colliderSyncs = localColliderSyncs,
                    deltaTime = Time.fixedDeltaTime
                }.Schedule(localColliderTransforms, currentJobHandle);
            }

            currentJobHandle.Complete();

            if (registeredMeshes != null && !isQuitting)
            {
                foreach (var rm in registeredMeshes)
                {
                    if (rm.JobIndex >= 0) rm.SetTriggerBounds(triggerBounds[rm.JobIndex]);
                    rm.SyncColliders(colliderSyncs, transformsToQuery, rigidBodiesToQuery);
                    rm.SyncLocalColliders(localColliderSyncs, Time.fixedDeltaTime); 
                }
            }
        }
        public override void OnPreFixedUpdate()
        {
            if (physicsBehaviour == null) UpdatePhysics();
        }

        public override void OnFixedUpdate() { }

        public override void OnLateUpdate()
        {

        }

        public override void OnUpdate()
        {
        }

        public RegisteredMesh RegisterLocal(SoftPlaneColliderMesh mesh)
        {
            if (registeredMeshes == null) return null;
            int existingIndex = GetRegisteredIndexOfLocal(mesh);
            if (existingIndex >= 0) return GetRegisteredMeshLocal(existingIndex);

            return new RegisteredMesh(this, mesh, mesh.useDynamicTriggerBounds); 
        }
        public static RegisteredMesh Register(SoftPlaneColliderMesh mesh)
        {
            var instance = Instance;
            if (instance == null) return null;

            return instance.RegisterLocal(mesh); 
        }

        public void UnregisterLocal(SoftPlaneColliderMesh mesh)
        {
            var index = GetRegisteredIndexOfLocal(mesh);
            if (index < 0) return;

            var rm = registeredMeshes[index];
            if (rm != null) rm.Dispose(); else RemoveMesh(index); 
        }
        public static void Unregister(SoftPlaneColliderMesh mesh)
        {
            var instance = InstanceOrNull;
            if (instance == null) return;

            instance.UnregisterLocal(mesh); 
        }

        [BurstCompile]
        public struct CalculateTriggerBoundsJob : IJobParallelFor
        {

            [ReadOnly]
            public NativeList<TransformDataWorldLocalAndMatrix> transformStates;

            [NativeDisableParallelForRestriction]
            public NativeList<TriggerBounds> triggerBounds;

            public void Execute(int index)
            {
                var trigger = triggerBounds[index];

                float3 extentsMin = new float3(float.MaxValue, float.MaxValue, float.MaxValue); 
                float3 extentsMax = new float3(float.MinValue, float.MinValue, float.MinValue);

                bool hasTransforms = trigger.vertexTransformIndexRange.x >= 0 && trigger.vertexTransformIndexRange.y >= trigger.vertexTransformIndexRange.x;
                extentsMin = math.select(float3.zero, extentsMin, hasTransforms);
                extentsMax = math.select(float3.zero, extentsMax, hasTransforms);

                int startIndex = math.max(0, trigger.vertexTransformIndexRange.x);
                for (int i = startIndex; i <= trigger.vertexTransformIndexRange.y; i++)
                {
                    var tState = transformStates[i]; 

                    extentsMin = math.min(extentsMin, tState.localPosition);
                    extentsMax = math.max(extentsMax, tState.localPosition); 
                }

                trigger.center = (extentsMin + extentsMax) * 0.5f;
                trigger.size = math.abs((extentsMax - extentsMin) + trigger.extensionWidth);

                triggerBounds[index] = trigger;
            }
        }

        [BurstCompile]
        public struct PositionSurfaceCollidersJob : IJobParallelForTransform
        {

            [ReadOnly]
            public NativeList<TransformDataWorldLocalAndMatrix> transformStates;
            [ReadOnly]
            public NativeList<TriangleVertex> triangleIndices;

            [NativeDisableParallelForRestriction]
            public NativeList<ColliderSync> colliderSyncs;

            private const float _oneThird = 0.333333f;
            private static readonly int3 _triIndexAdd = new int3(0, 1, 2);
            public void Execute(int index, TransformAccess transform)
            {
                var sync = colliderSyncs[index];

                sync.distanceFromTri = 0;
                sync.triNormal = 0;
                sync.repositionTarget = false;

                if (sync.vertexTransformTriangleIndexRange.x >= 0)
                {
                    var referenceState = transformStates[sync.targetTransformIndex];
                    var referencePoint = math.transform(referenceState.toWorld, sync.targetOffset); 
                    //Debug.DrawRay(referencePos, Vector3.up, Color.blue, 0.1f); 

                    float2 closestContainingTri = new float2(-1, float.MaxValue);
                    float2 closestTri = new float2(-1, float.MaxValue);
                    int maxIndex = sync.vertexTransformTriangleIndexRange.y - 2;
                    for (int a = sync.vertexTransformTriangleIndexRange.x; a <= maxIndex; a += 3)
                    {
                        int3 b = a;
                        b = b + _triIndexAdd;

                        var i0 = triangleIndices[b.x];
                        var i1 = triangleIndices[b.y];
                        var i2 = triangleIndices[b.z];

                        var state0 = transformStates[i0.vertexTransformIndex];
                        var state1 = transformStates[i1.vertexTransformIndex];
                        var state2 = transformStates[i2.vertexTransformIndex];

                        var center = (state0.position + state1.position + state2.position) * _oneThird;
                        float dist = math.lengthsq(referencePoint - center);

                        float2 indexBundle = new float2(a, dist);

                        bool isCloserAndContained = dist < closestContainingTri.y;
                        if (isCloserAndContained)
                        {
                            isCloserAndContained = Maths.IsInTriangle(referencePoint, state0.position, state1.position, state2.position, 0.25f); 
                        }
                        closestContainingTri = math.select(closestContainingTri, indexBundle, isCloserAndContained);

                        bool isCloser = dist < closestTri.y;
                        closestTri = math.select(closestContainingTri, indexBundle, isCloser);
                    }

                    // we'll keep old parent and position as last resort...
                    //sync.parentVertexTransformIndex = -1;
                    int3 closestTriIndex = (int)closestContainingTri.x;
                    if (closestTriIndex.x >= 0)
                    {
                        closestTriIndex = closestTriIndex + _triIndexAdd;

                        var i0 = triangleIndices[closestTriIndex.x];
                        var i1 = triangleIndices[closestTriIndex.y];
                        var i2 = triangleIndices[closestTriIndex.z];

                        var state0 = transformStates[i0.vertexTransformIndex];
                        var state1 = transformStates[i1.vertexTransformIndex];
                        var state2 = transformStates[i2.vertexTransformIndex];

                        bool isEdge = math.any(new bool3(i0.isEdge, i1.isEdge, i2.isEdge));

                        var center = (state0.position + state1.position + state2.position) * _oneThird;

                        var state3x3 = new float3x3(state0.position, state1.position, state2.position);
                        var coords = Maths.BarycentricCoords(referencePoint, state0.position, state1.position, state2.position);

                        var finalPosition = math.mul(state3x3, coords);

                        var referenceOffset = referencePoint - finalPosition;
                        var distanceFromTri = math.length(referenceOffset);
                        var referenceDir = math.select(referenceOffset, referenceOffset / distanceFromTri, distanceFromTri > 0);

                        var triNormal = math.normalize(math.cross((state1.position - state0.position), (state2.position - state0.position)));
                        var triNormalBase = triNormal;
                        var triDot = math.dot(referenceDir, triNormal);
                        triNormal = triNormal * math.sign(triDot);
                        var finalRotation = Maths.FromToRotation(new float3(0, 0, 1), triNormal);

                        transform.SetPositionAndRotation(finalPosition, finalRotation);

                        sync.parentVertexTransformIndex = math.select(math.select(i0.vertexTransformIndex, i1.vertexTransformIndex, coords.x > coords.y), i2.vertexTransformIndex, math.all(coords.z > coords.xy));

                        var referenceDot = triDot * sync.isAbove;

                        /*bool isProperlyInTri = Maths.IsInTriangle(coords, 0.02f);*/ 
                        sync.repositionTarget = /*isProperlyInTri &&*/ !isEdge && distanceFromTri > 0f && referenceDot < 0;
                        sync.targetPosition = referenceState.position + triNormalBase * distanceFromTri * -referenceDot;
                         
                        //sync.isAbove = math.select(math.sign(triDot), sync.isAbove, isProperlyInTri); 

                        sync.distanceFromTri = distanceFromTri;
                        sync.triNormal = triNormal;

                    }
                    else if (closestTri.x >= 0) // fall back to closest tri
                    {
                        closestTriIndex = (int)closestTri.x;

                        var i0 = triangleIndices[closestTriIndex.x];
                        var i1 = triangleIndices[closestTriIndex.y];
                        var i2 = triangleIndices[closestTriIndex.z];

                        var state0 = transformStates[i0.vertexTransformIndex];
                        var state1 = transformStates[i1.vertexTransformIndex];
                        var state2 = transformStates[i2.vertexTransformIndex];

                        bool isEdge = math.any(new bool3(i0.isEdge, i1.isEdge, i2.isEdge));

                        var center = (state0.position + state1.position + state2.position) * _oneThird;

                        var finalPosition = center;

                        var distanceFromTri = math.sqrt(closestTri.y);
                        var referenceOffset = referencePoint - finalPosition;
                        var referenceDir = math.select(referenceOffset, referenceOffset / distanceFromTri, distanceFromTri > 0);

                        var triNormal = math.normalizesafe(math.cross((state1.position - state0.position), (state2.position - state0.position)));
                        var triNormalBase = triNormal;
                        var triDot = math.dot(referenceDir, triNormal);
                        triNormal = triNormal * math.sign(triDot);
                        var finalRotation = Maths.FromToRotation(new float3(0, 0, 1), triNormal);

                        transform.SetPositionAndRotation(finalPosition, finalRotation);

                        sync.isAbove = math.select(math.sign(triDot), sync.isAbove, isEdge);  

                        sync.parentVertexTransformIndex = i0.vertexTransformIndex;
                        sync.distanceFromTri = distanceFromTri;
                        sync.triNormal = triNormal;
                    }
                    
                    colliderSyncs[index] = sync;
                }
            }
        }

        [BurstCompile]
        public struct UpdateLocalSurfaceCollidersJob : IJobParallelForTransform
        {

            public float deltaTime;

            [ReadOnly]
            public NativeList<TransformDataWorldLocalAndMatrix> transformStates;

            [NativeDisableParallelForRestriction]
            public NativeList<LocalColliderSync> colliderSyncs;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static float ScaleAlongAxis(float3 offset, float3 axis)
            {
                return math.dot(offset, axis);
            }
            public void Execute(int index, TransformAccess transform)
            {
                var sync = colliderSyncs[index];

                var state0 = transformStates[sync.quadIndices.x];
                var state1 = transformStates[sync.quadIndices.y];
                var state2 = transformStates[sync.quadIndices.z];
                var state3 = transformStates[sync.quadIndices.w];

                var p0 = state0.localPosition;
                var p1 = state1.localPosition;
                var p2 = state2.localPosition;
                var p3 = state3.localPosition;

                float3 center = (p0 + p1 + p2 + p3) * 0.25f;

                float3 offsetP0 = p0 - center;
                float3 offsetP1 = p1 - center;
                float3 offsetP2 = p2 - center;
                float3 offsetP3 = p3 - center;

                var normalA = math.cross((p1 - p0), (p2 - p0));
                var normalB = math.cross((p2 - p1), (p3 - p1));

                var axisNormal = math.normalizesafe(normalA + normalB);
                var axisUp = math.normalizesafe((p0 - p3) + (p1 - p2));
                var axisTangent = math.normalizesafe(math.cross(axisNormal, axisUp));
                 
                float3 mags0 = new float3(
                        ScaleAlongAxis(offsetP0, axisTangent),
                        ScaleAlongAxis(offsetP0, axisUp),
                        ScaleAlongAxis(offsetP0, axisNormal)
                        );

                float3 mags1 = new float3(
                        ScaleAlongAxis(offsetP1, axisTangent),
                        ScaleAlongAxis(offsetP1, axisUp),
                        ScaleAlongAxis(offsetP1, axisNormal)
                        );

                float3 mags2 = new float3(
                        ScaleAlongAxis(offsetP2, axisTangent),
                        ScaleAlongAxis(offsetP2, axisUp),
                        ScaleAlongAxis(offsetP2, axisNormal)
                        );

                float3 mags3 = new float3(
                        ScaleAlongAxis(offsetP3, axisTangent),
                        ScaleAlongAxis(offsetP3, axisUp),
                        ScaleAlongAxis(offsetP3, axisNormal)
                        );
                 
                var offsetMin = math.min(math.min(math.min(mags0, mags1), mags2), mags3);
                var offsetMax = math.max(math.max(math.max(mags0, mags1), mags2), mags3);  

                var offsetSize = offsetMax - offsetMin;
                var offsetCenter = (offsetMin + offsetMax) * 0.5f;

                quaternion rootToLocal = math.inverse(state0.localRotation);

                float syncSpeed = math.select(1, math.saturate(deltaTime * sync.syncSpeed), sync.syncSpeed > 0); 

                quaternion rotA = Maths.FromToRotation(sync.startNormal, axisNormal);
                quaternion rotB = Maths.FromToRotation(math.rotate(rotA, sync.startUp), axisUp);
                var localRot = math.mul(rootToLocal, math.mul(rotB, math.mul(rotA, sync.startRotation)));
                transform.localRotation = math.slerp(transform.localRotation, localRot, syncSpeed);

                var localPos = math.rotate(rootToLocal, (center + (axisTangent * offsetCenter.x) + (axisUp * offsetCenter.y) + (axisNormal * offsetCenter.z)) - state0.localPosition); 
                transform.localPosition = math.lerp(transform.localPosition, localPos, syncSpeed);

                sync.size = new float3(offsetSize.x, offsetSize.y, sync.thickness /*+ offsetSize.z*/);
                sync.startSize = math.select(sync.size, sync.startSize, math.any(new bool3(sync.startSize != 0))); 

                var targetSize = sync.size * sync.scale;
                sync.scaleResponse = math.length(targetSize.xy / sync.startSize.xy);
                targetSize = sync.startSize * math.clamp(targetSize / sync.startSize, sync.minScale, sync.maxScale); 
                sync.size = targetSize;

                colliderSyncs[index] = sync; 
            }
        }

    }
}

#endif