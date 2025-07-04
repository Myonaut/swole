#if (UNITY_EDITOR || UNITY_STANDALONE)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole
{
     
    public static class PhysicsExtensions
    {

        public static bool Contains(this LayerMask layerMask, int layer) => ContainsUnityLayer(layerMask.value, layer);
        public static bool ContainsUnityLayer(this int layerMask, int layer)
        {
            return (layerMask & (1 << layer)) != 0;
        }
        public static bool IsInLayerMask(this GameObject obj, LayerMask mask) => mask.Contains(obj.layer);
        public static bool IsInLayerMask(this GameObject obj, int mask) => mask.ContainsUnityLayer(obj.layer);

        public static Vector3 GetHeightAxis(this CapsuleCollider collider) => collider.direction == 0 ? Vector3.right : collider.direction == 1 ? Vector3.up : Vector3.forward;

        private static Collider[] _colliders = new Collider[256];

        /// <summary>
        /// Does not support scaling! Uses GetComponentsInChildren for every call unless collider components are provided... terribly inefficient.
        /// </summary>
        public static int OverlapColliderNonAlloc(this Rigidbody rigidbody, Collider[] outputColliders, Vector3 worldPosition, Quaternion worldOrientation, int layerMask = ~0, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore, IEnumerable<Collider> localColliders = null)
        {

            if (outputColliders == null) return 0;

            Transform rigidTransform = rigidbody.transform;

            //Vector3 rigidPosition = rigidTransform.position;
            Quaternion rigidRotation = rigidTransform.rotation;
            Quaternion rigidRotationInverse = Quaternion.Inverse(rigidRotation);

            int max = outputColliders.Length;

            int count = 0;

            if (localColliders == null) localColliders = rigidbody.GetComponentsInChildren<Collider>(false);
            foreach (var collider in localColliders)
            {

                Transform transform = collider.transform;

                int count2 = 0;
                if (collider is SphereCollider sphere)
                {
                    count2 = Mathf.Min(Physics.OverlapSphereNonAlloc((worldOrientation * rigidTransform.InverseTransformPoint(transform.TransformPoint(sphere.center))) + worldPosition, sphere.radius, _colliders, layerMask, queryTriggerInteraction), max - count);
                } 
                else if (collider is BoxCollider box)
                {
                    count2 = Mathf.Min(Physics.OverlapBoxNonAlloc((worldOrientation * rigidTransform.InverseTransformPoint(transform.TransformPoint(box.center))) + worldPosition, box.size * 0.5f, _colliders, worldOrientation * (rigidRotationInverse * transform.rotation), layerMask, queryTriggerInteraction), max - count);
                } 
                else if (collider is CapsuleCollider capsule)
                {
                    float halfHeight = Mathf.Max(0, (capsule.height * 0.5f) - capsule.radius);
                    Vector3 axis = capsule.GetHeightAxis();

                    Vector3 point1 = (worldOrientation * rigidTransform.InverseTransformPoint(transform.TransformPoint(capsule.center + axis * halfHeight))) + worldPosition;
                    Vector3 point2 = (worldOrientation * rigidTransform.InverseTransformPoint(transform.TransformPoint(capsule.center - axis * halfHeight))) + worldPosition;

                    /*Debug.DrawLine(point1, point2, Color.blue, 5);
                    Debug.DrawRay(point1, Vector3.up * collider.radius, Color.red, 5);
                    Debug.DrawRay(point2, Vector3.down * collider.radius, Color.red, 5);*/

                    count2 = Mathf.Min(Physics.OverlapCapsuleNonAlloc(point1, point2, capsule.radius, _colliders, layerMask, queryTriggerInteraction), max - count);
                }     

                for (int i = 0; i < count2; i++) outputColliders[i + count] = _colliders[i];

                count += count2;

                if (count >= max) return max;

            }

            return count;

        }

        /// <summary>
        /// Does not support scaling! Uses GetComponentsInChildren for every call unless collider components are provided... terribly inefficient.
        /// </summary>
        public static bool CheckCollider(this Rigidbody rigidbody, Vector3 worldPosition, Quaternion worldOrientation, int layerMask = ~0, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore, IEnumerable<Collider> localColliders = null)
        {

            Transform rigidTransform = rigidbody.transform;

            //Vector3 rigidPosition = rigidTransform.position;
            Quaternion rigidRotation = rigidTransform.rotation;
            Quaternion rigidRotationInverse = Quaternion.Inverse(rigidRotation);

            if (localColliders == null) localColliders = rigidbody.GetComponentsInChildren<Collider>(false);
            foreach (var collider in localColliders)
            {

                Transform transform = collider.transform;

                if (collider is SphereCollider sphere)
                {
                    /*Vector3 pos = (worldOrientation * rigidTransform.InverseTransformPoint(transform.TransformPoint(sphere.center))) + worldPosition;

                    Debug.DrawRay(pos, Vector3.up * sphere.radius, Color.red, 5);
                    Debug.DrawRay(pos, Vector3.down * sphere.radius, Color.red, 5);
                    Debug.DrawRay(pos, Vector3.left * sphere.radius, Color.red, 5);
                    Debug.DrawRay(pos, Vector3.right * sphere.radius, Color.red, 5);
                    Debug.DrawRay(pos, Vector3.forward * sphere.radius, Color.red, 5);
                    Debug.DrawRay(pos, Vector3.back * sphere.radius, Color.red, 5);*/

                    if (Physics.CheckSphere((worldOrientation * rigidTransform.InverseTransformPoint(transform.TransformPoint(sphere.center))) + worldPosition, sphere.radius, layerMask, queryTriggerInteraction)) return true;
                }
                else if (collider is BoxCollider box)
                {
                    if (Physics.CheckBox((worldOrientation * rigidTransform.InverseTransformPoint(transform.TransformPoint(box.center))) + worldPosition, box.size * 0.5f, worldOrientation * (rigidRotationInverse * transform.rotation), layerMask, queryTriggerInteraction)) return true;
                }
                else if (collider is CapsuleCollider capsule)
                {
                    float halfHeight = Mathf.Max(0, (capsule.height * 0.5f) - capsule.radius);
                    Vector3 axis = capsule.GetHeightAxis();

                    Vector3 point1 = (worldOrientation * rigidTransform.InverseTransformPoint(transform.TransformPoint(capsule.center + axis * halfHeight))) + worldPosition;
                    Vector3 point2 = (worldOrientation * rigidTransform.InverseTransformPoint(transform.TransformPoint(capsule.center - axis * halfHeight))) + worldPosition;

                    /*Debug.DrawLine(point1, point2, Color.blue, 5);
                    Debug.DrawRay(point1, Vector3.up * capsule.radius, Color.red, 5);
                    Debug.DrawRay(point2, Vector3.down * capsule.radius, Color.red, 5);*/

                    if (Physics.CheckCapsule(point1, point2, capsule.radius, layerMask, queryTriggerInteraction)) return true;
                }
            }

            return false;

        }

        /// <summary>
        /// Does not support scaling! Uses GetComponentsInChildren for every call unless collider components are provided... terribly inefficient.
        /// </summary>
        public static bool Sweep(this Rigidbody rigidbody, Vector3 worldPosition, Quaternion worldOrientation, Vector3 direction, out RaycastHit hit, float distance, int layerMask = ~0, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore, IEnumerable<Collider> localColliders = null)
        {

            Transform rigidTransform = rigidbody.transform;

            //Vector3 rigidPosition = rigidTransform.position;
            Quaternion rigidRotation = rigidTransform.rotation;
            Quaternion rigidRotationInverse = Quaternion.Inverse(rigidRotation);

            if (localColliders == null) localColliders = rigidbody.GetComponentsInChildren<Collider>(false);
            foreach (var collider in localColliders)
            {

                Transform transform = collider.transform;

                if (collider is SphereCollider sphere)
                {
                    if (Physics.SphereCast((worldOrientation * rigidTransform.InverseTransformPoint(transform.TransformPoint(sphere.center))) + worldPosition, sphere.radius, direction, out hit, distance, layerMask, queryTriggerInteraction)) return true;
                }
                else if (collider is BoxCollider box)
                {
                    if (Physics.BoxCast((worldOrientation * rigidTransform.InverseTransformPoint(transform.TransformPoint(box.center))) + worldPosition, box.size * 0.5f, direction, out hit, worldOrientation * (rigidRotationInverse * transform.rotation), distance, layerMask, queryTriggerInteraction)) return true;
                }
                else if (collider is CapsuleCollider capsule)
                {
                    float halfHeight = Mathf.Max(0, (capsule.height * 0.5f) - capsule.radius);
                    Vector3 axis = capsule.GetHeightAxis();

                    Vector3 point1 = (worldOrientation * rigidTransform.InverseTransformPoint(transform.TransformPoint(capsule.center + axis * halfHeight))) + worldPosition;
                    Vector3 point2 = (worldOrientation * rigidTransform.InverseTransformPoint(transform.TransformPoint(capsule.center - axis * halfHeight))) + worldPosition;

                    /*Debug.DrawLine(point1, point2, Color.blue, 5);
                    Debug.DrawRay(point1, Vector3.up * capsule.radius, Color.red, 5);
                    Debug.DrawRay(point2, Vector3.down * capsule.radius, Color.red, 5);*/

                    if (Physics.CapsuleCast(point1, point2, capsule.radius, direction, out hit, distance, layerMask, queryTriggerInteraction)) return true;
                }
            }

            hit = default;

            return false;

        }

        public static int CompareByDistanceAscending(RaycastHit hitA, RaycastHit hitB) => System.Math.Sign(hitA.distance - hitB.distance);
        public static int CompareByDistanceDescending(RaycastHit hitA, RaycastHit hitB) => System.Math.Sign(hitB.distance - hitA.distance);
        
        public enum RaycastHitSortOrder
        {
            None = 0,
            Ascending = 1,
            Descending = 2
        }
        private static readonly List<RaycastHit> tempHits = new List<RaycastHit>();
        /// <summary>
        /// Sweep and actually take into consideration exclusion layers...
        /// </summary>
        public static bool SweepTestExtended(this Rigidbody rigidbody, Vector3 direction, out RaycastHit hit, float distance, int layerMask = ~0, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore, RaycastHitSortOrder sortOrder = RaycastHitSortOrder.Ascending)
        {

            hit = default;

            var hits = SweepTestAllExtended(rigidbody, direction, distance, layerMask, queryTriggerInteraction);
            if (hits.Count > 0)
            {
                if (hits.Count > 1)
                {
                    switch (sortOrder)
                    {
                        case RaycastHitSortOrder.Ascending:
                            hits.Sort(CompareByDistanceAscending);
                            break;
                        case RaycastHitSortOrder.Descending:
                            hits.Sort(CompareByDistanceDescending);
                            break;
                    }
                }

                hit = hits[0];
                return true;
            }

            return false;

        }
        /// <summary>
        /// Sweep and actually take into consideration exclusion layers...
        /// </summary>
        public static List<RaycastHit> SweepTestAllExtended(this Rigidbody rigidbody, Vector3 direction, float distance, int layerMask = ~0, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore, List<RaycastHit> hitList = null)
        {

            if (hitList == null) hitList = tempHits;
            hitList.Clear();

            var hits = rigidbody.SweepTestAll(direction, distance, queryTriggerInteraction);
            if (hits != null && hits.Length > 0)
            {
                layerMask = layerMask & ~rigidbody.excludeLayers;
                
                foreach (var hit in hits)
                {
                    if (layerMask.ContainsUnityLayer(hit.collider.gameObject.layer))
                    {
                        hitList.Add(hit); 
                    }
                }
            }

            return hitList;

        }

    }

}

#endif