using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole.Morphing
{
    public class CustomizableCharacterDynamicBoxCollider : MonoBehaviour, ICustomizableCharacterDynamicCollider
    {
        [SerializeField]
        protected CustomizableCharacterMeshPointTracker trackerManager;

        public Vector2 referenceDistanceRange;

        public Vector2 sizeRange;

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

        new protected BoxCollider collider;

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

            collider = gameObject.AddOrGetComponent<BoxCollider>();

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

                    float size = sizeRange.x;
                    if (referenceDistanceRange.x != referenceDistanceRange.y)
                    {
                        float t = ((dist - referenceDistanceRange.x) / (referenceDistanceRange.y - referenceDistanceRange.x));
                        if (clampDistanceRange) t = Mathf.Clamp01(t);

                        size = Mathf.LerpUnclamped(sizeRange.x, sizeRange.y, t);
                    }

                    collider.size = new Vector3(size, size, dist); 
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

    public interface ICustomizableCharacterDynamicCollider
    {
        public void UpdatePosition();
    }

    public class CustomizableCharacterDynamicColliderUpdater : SingletonBehaviour<CustomizableCharacterDynamicColliderUpdater>
    {

        public static int ExecutionPriority => CustomizableCharacterMeshPointTrackerUpdater.ExecutionPriority + 1;

        public override int Priority => ExecutionPriority;

        protected readonly List<ICustomizableCharacterDynamicCollider> colliders = new List<ICustomizableCharacterDynamicCollider>();

        public void RegisterLocal(ICustomizableCharacterDynamicCollider collider)
        {
            if (!colliders.Contains(collider)) colliders.Add(collider);
        }

        private readonly List<ICustomizableCharacterDynamicCollider> toRemove = new List<ICustomizableCharacterDynamicCollider>();
        public void UnregisterLocal(ICustomizableCharacterDynamicCollider bone)
        {
            toRemove.Add(bone);
        }

        public static void Register(ICustomizableCharacterDynamicCollider bone)
        {
            var instance = Instance;
            if (instance == null) return;

            instance.RegisterLocal(bone);
        }

        public static void Unregister(ICustomizableCharacterDynamicCollider bone)
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
        }

        public override void OnLateUpdate()
        {
            foreach (var collider in colliders)
            {
                if (collider != null) collider.UpdatePosition();
            }

            if (toRemove.Count > 0)
            {
                foreach (var collider in toRemove) if (collider != null) colliders.Remove(collider);
                toRemove.Clear();

                colliders.RemoveAll(b => b == null);
            }
        }

    }
}
