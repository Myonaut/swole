#if (UNITY_EDITOR || UNITY_STANDALONE)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole
{
     
    public static class PhysicsExtensions
    {

        public static Vector3 GetHeightAxis(this CapsuleCollider collider) => collider.direction == 0 ? Vector3.right : collider.direction == 1 ? Vector3.up : Vector3.forward;

        private static Collider[] _colliders = new Collider[256];

        /// <summary>
        /// Does not support scaling!
        /// </summary>
        public static int OverlapColliderNonAlloc(this Rigidbody rigidbody, Collider[] colliders, Vector3 worldPosition, Quaternion worldOrientation, int layerMask = ~0, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {

            if (colliders == null) return 0;

            Transform rigidTransform = rigidbody.transform;

            //Vector3 rigidPosition = rigidTransform.position;
            Quaternion rigidRotation = rigidTransform.rotation;
            Quaternion rigidRotationInverse = Quaternion.Inverse(rigidRotation);

            int max = colliders.Length;

            int count = 0;

            SphereCollider[] spheres = rigidbody.GetComponentsInChildren<SphereCollider>();

            foreach (var collider in spheres)
            {

                Transform transform = collider.transform;

                int count2 = Mathf.Min(Physics.OverlapSphereNonAlloc((worldOrientation * rigidTransform.InverseTransformPoint(transform.TransformPoint(collider.center))) + worldPosition, collider.radius, _colliders, layerMask, queryTriggerInteraction), max - count);

                for (int i = 0; i < count2; i++) colliders[i + count] = _colliders[i];

                count += count2;

                if (count >= max) return max;

            }

            BoxCollider[] boxes = rigidbody.GetComponentsInChildren<BoxCollider>();

            foreach (var collider in boxes)
            {

                Transform transform = collider.transform;

                int count2 = Mathf.Min(Physics.OverlapBoxNonAlloc((worldOrientation * rigidTransform.InverseTransformPoint(transform.TransformPoint(collider.center))) + worldPosition, collider.size * 0.5f, _colliders, worldOrientation * (rigidRotationInverse * transform.rotation), layerMask, queryTriggerInteraction), max - count);

                for (int i = 0; i < count2; i++) colliders[i + count] = _colliders[i];

                count += count2;

                if (count >= max) return max;

            }

            CapsuleCollider[] capsules = rigidbody.GetComponentsInChildren<CapsuleCollider>();

            foreach (var collider in capsules)
            {

                Transform transform = collider.transform;

                float halfHeight = Mathf.Max(0, (collider.height * 0.5f) - collider.radius);
                Vector3 axis = collider.GetHeightAxis();

                Vector3 point1 = (worldOrientation * rigidTransform.InverseTransformPoint(transform.TransformPoint(collider.center + axis * halfHeight))) + worldPosition;
                Vector3 point2 = (worldOrientation * rigidTransform.InverseTransformPoint(transform.TransformPoint(collider.center - axis * halfHeight))) + worldPosition;

                /*Debug.DrawLine(point1, point2, Color.blue, 5);
                Debug.DrawRay(point1, Vector3.up * collider.radius, Color.red, 5);
                Debug.DrawRay(point2, Vector3.down * collider.radius, Color.red, 5);*/

                int count2 = Mathf.Min(Physics.OverlapCapsuleNonAlloc(point1, point2, collider.radius, _colliders, layerMask, queryTriggerInteraction), max - count);

                for (int i = 0; i < count2; i++) colliders[i + count] = _colliders[i];

                count += count2;

            }

            return count;

        }

        /// <summary>
        /// Does not support scaling!
        /// </summary>
        public static bool CheckCollider(this Rigidbody rigidbody, Vector3 worldPosition, Quaternion worldOrientation, int layerMask = ~0, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {

            Transform rigidTransform = rigidbody.transform;

            //Vector3 rigidPosition = rigidTransform.position;
            Quaternion rigidRotation = rigidTransform.rotation;
            Quaternion rigidRotationInverse = Quaternion.Inverse(rigidRotation);

            SphereCollider[] spheres = rigidbody.GetComponentsInChildren<SphereCollider>();

            foreach (var collider in spheres)
            {

                Transform transform = collider.transform;

                /*Vector3 pos = (worldOrientation * rigidTransform.InverseTransformPoint(transform.TransformPoint(collider.center))) + worldPosition;

                Debug.DrawRay(pos, Vector3.up * collider.radius, Color.red, 5);
                Debug.DrawRay(pos, Vector3.down * collider.radius, Color.red, 5);
                Debug.DrawRay(pos, Vector3.left * collider.radius, Color.red, 5);
                Debug.DrawRay(pos, Vector3.right * collider.radius, Color.red, 5);
                Debug.DrawRay(pos, Vector3.forward * collider.radius, Color.red, 5);
                Debug.DrawRay(pos, Vector3.back * collider.radius, Color.red, 5);*/

                if (Physics.CheckSphere((worldOrientation * rigidTransform.InverseTransformPoint(transform.TransformPoint(collider.center))) + worldPosition, collider.radius, layerMask, queryTriggerInteraction)) return true;

            }

            BoxCollider[] boxes = rigidbody.GetComponentsInChildren<BoxCollider>();

            foreach (var collider in boxes)
            {

                Transform transform = collider.transform;

                if (Physics.CheckBox((worldOrientation * rigidTransform.InverseTransformPoint(transform.TransformPoint(collider.center))) + worldPosition, collider.size * 0.5f, worldOrientation * (rigidRotationInverse * transform.rotation), layerMask, queryTriggerInteraction)) return true;

            }

            CapsuleCollider[] capsules = rigidbody.GetComponentsInChildren<CapsuleCollider>();

            foreach (var collider in capsules)
            {

                Transform transform = collider.transform;

                float halfHeight = Mathf.Max(0, (collider.height * 0.5f) - collider.radius);
                Vector3 axis = collider.GetHeightAxis();

                Vector3 point1 = (worldOrientation * rigidTransform.InverseTransformPoint(transform.TransformPoint(collider.center + axis * halfHeight))) + worldPosition;
                Vector3 point2 = (worldOrientation * rigidTransform.InverseTransformPoint(transform.TransformPoint(collider.center - axis * halfHeight))) + worldPosition;

                /*Debug.DrawLine(point1, point2, Color.blue, 5);
                Debug.DrawRay(point1, Vector3.up * collider.radius, Color.red, 5);
                Debug.DrawRay(point2, Vector3.down * collider.radius, Color.red, 5);*/

                if (Physics.CheckCapsule(point1, point2, collider.radius, layerMask, queryTriggerInteraction)) return true;

            }

            return false;

        }

        /// <summary>
        /// Does not support scaling!
        /// </summary>
        public static bool Sweep(this Rigidbody rigidbody, Vector3 worldPosition, Quaternion worldOrientation, Vector3 direction, out RaycastHit hit, float distance, int layerMask = ~0, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {

            Transform rigidTransform = rigidbody.transform;

            //Vector3 rigidPosition = rigidTransform.position;
            Quaternion rigidRotation = rigidTransform.rotation;
            Quaternion rigidRotationInverse = Quaternion.Inverse(rigidRotation);

            SphereCollider[] spheres = rigidbody.GetComponentsInChildren<SphereCollider>();

            foreach (var collider in spheres)
            {

                Transform transform = collider.transform;

                if (Physics.SphereCast((worldOrientation * rigidTransform.InverseTransformPoint(transform.TransformPoint(collider.center))) + worldPosition, collider.radius, direction, out hit, distance, layerMask, queryTriggerInteraction)) return true;

            }

            BoxCollider[] boxes = rigidbody.GetComponentsInChildren<BoxCollider>();

            foreach (var collider in boxes)
            {

                Transform transform = collider.transform;

                if (Physics.BoxCast((worldOrientation * rigidTransform.InverseTransformPoint(transform.TransformPoint(collider.center))) + worldPosition, collider.size * 0.5f, direction, out hit, worldOrientation * (rigidRotationInverse * transform.rotation), distance, layerMask, queryTriggerInteraction)) return true;

            }

            CapsuleCollider[] capsules = rigidbody.GetComponentsInChildren<CapsuleCollider>();

            foreach (var collider in capsules)
            {

                Transform transform = collider.transform;

                float halfHeight = Mathf.Max(0, (collider.height * 0.5f) - collider.radius);
                Vector3 axis = collider.GetHeightAxis();

                Vector3 point1 = (worldOrientation * rigidTransform.InverseTransformPoint(transform.TransformPoint(collider.center + axis * halfHeight))) + worldPosition;
                Vector3 point2 = (worldOrientation * rigidTransform.InverseTransformPoint(transform.TransformPoint(collider.center - axis * halfHeight))) + worldPosition;

                /*Debug.DrawLine(point1, point2, Color.blue, 5);
                Debug.DrawRay(point1, Vector3.up * collider.radius, Color.red, 5);
                Debug.DrawRay(point2, Vector3.down * collider.radius, Color.red, 5);*/

                if (Physics.CapsuleCast(point1, point2, collider.radius, direction, out hit, distance, layerMask, queryTriggerInteraction)) return true;

            }

            hit = default;

            return false;

        }

    }

}

#endif