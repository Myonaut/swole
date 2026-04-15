using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Swole.Morphing
{
    public class CustomizableCharacterDynamicSphereCollider : MonoBehaviour, ICustomizableCharacterDynamicCollider
    {
        [SerializeField]
        protected CustomizableCharacterMeshPointTracker trackerManager;

        public Vector2 referenceDistanceRange;

        public bool clampDistanceRange;

        [SerializeField]
        protected string pointMeshA;
        [SerializeField]
        protected string pointA;

        [SerializeField]
        protected string pointMeshB;
        [SerializeField]
        protected string pointB;

        protected Vector2Int pointA_index;
        protected Vector2Int pointB_index;

        new protected SphereCollider collider;

        protected Quaternion startRotation;

        protected void Start()
        {
            pointA_index = pointB_index = new Vector2Int(-1, -1);

            if (trackerManager != null)
            {
                if (!string.IsNullOrWhiteSpace(pointMeshA) && !string.IsNullOrWhiteSpace(pointA) && trackerManager.TryGetTracker(pointMeshA, out var trackerMeshA))
                {
                    if (trackerMeshA.TryGetPoint(pointA, out var trackerPointA))
                    {
                        pointA_index = new Vector2Int(trackerMeshA.Index, trackerPointA.IndexInTracker);
                    }
                }

                if (!string.IsNullOrWhiteSpace(pointMeshB) && !string.IsNullOrWhiteSpace(pointB) && trackerManager.TryGetTracker(pointMeshB, out var trackerMeshB))
                {
                    if (trackerMeshB.TryGetPoint(pointB, out var trackerPointB))
                    {
                        pointB_index = new Vector2Int(trackerMeshB.Index, trackerPointB.IndexInTracker);
                    }
                }
            }

            collider = gameObject.AddOrGetComponent<SphereCollider>();

            startRotation = transform.rotation;
        }

        protected void OnEnable()
        {
            CustomizableCharacterDynamicColliderUpdater.Register(this);
        }

        protected void OnDisable()
        {
            CustomizableCharacterDynamicColliderUpdater.Unregister(this);
        }

        protected void OnDestroy()
        {
        }

        public void UpdatePosition()
        {
            if (trackerManager == null || collider == null) return;

            if (pointA_index.x >= 0 && pointA_index.y >= 0)
            {
                Vector3 posA = trackerManager.GetPointPositionUnsafe(pointA_index.x, pointA_index.y);

                if (pointB_index.x >= 0 && pointB_index.y >= 0)
                {
                    Vector3 posB = trackerManager.GetPointPositionUnsafe(pointB_index.x, pointB_index.y);

                    float dist = Vector3.Distance(posA, posB);

                    float radius = dist;
                    if (referenceDistanceRange.x != referenceDistanceRange.y)
                    {
                        if (clampDistanceRange) radius = Mathf.Clamp(radius, referenceDistanceRange.x, referenceDistanceRange.y);
                    }
                    radius = radius * 0.5f;

                    collider.radius = radius;
                    collider.center = new Vector3(collider.center.x, collider.center.y, dist * 0.5f);

                    transform.position = posA;
                    transform.LookAt(posB);
                }
                else
                {
                    var rotA = trackerManager.GetPointRotationOffsetUnsafe(pointA_index.x, pointA_index.y); 
                    transform.SetPositionAndRotation(posA, rotA * startRotation);
                }
            }
        }
    }
}
