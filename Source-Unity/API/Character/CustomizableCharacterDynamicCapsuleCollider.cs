using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Swole.Morphing
{
    public class CustomizableCharacterDynamicCapsuleCollider : MonoBehaviour, ICustomizableCharacterDynamicCollider
    {
        [SerializeField]
        protected CustomizableCharacterMeshPointTracker trackerManager; 

        public Vector2 referenceDistanceRange;

        public Vector2 radiusRange;

        public bool clampDistanceRange;

        [SerializeField]
        protected string pointMeshA;
        [SerializeField]
        protected string pointA;

        [SerializeField]
        protected string pointMeshB;
        [SerializeField]
        protected string pointB;

        [SerializeField]
        protected Vector2Int pointA_index;
        [SerializeField]
        protected Vector2Int pointB_index;

        new protected CapsuleCollider collider;

        protected Quaternion startRotation;

        protected void Start()
        {
            pointA_index = pointB_index = new Vector2Int(-1, -1);

            collider = gameObject.AddOrGetComponent<CapsuleCollider>();

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

                        collider.direction = 2; 
                    } 
                }
            }

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

                    float radius = radiusRange.x;
                    if (referenceDistanceRange.x != referenceDistanceRange.y)
                    {
                        float t = ((dist - referenceDistanceRange.x) / (referenceDistanceRange.y - referenceDistanceRange.x));
                        if (clampDistanceRange) t = Mathf.Clamp01(t);

                        radius = Mathf.LerpUnclamped(radiusRange.x, radiusRange.y, t);
                    }

                    collider.radius = radius;
                    collider.height = dist;
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
