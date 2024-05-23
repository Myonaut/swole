#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Swole.API.Unity
{

    public class ProxyTransform : MonoBehaviour
    {
         
        public Transform transformToCopy;

        [SerializeField, Tooltip("Probably necessary if the two transforms do not share the same parent.")]
        public bool applyInWorldSpace; 

        [SerializeField]
        protected Vector3 offsetPos;
        public Vector3 OffsetPos => offsetPos;
        [SerializeField]
        protected Vector3 offsetRot;
        public Vector3 OffsetRot => offsetRot;
        [SerializeField, Tooltip("Only works in local space.")]
        protected Vector3 multiplyScale = Vector3.one;
        public Vector3 MultiplyScale => multiplyScale;

        protected Quaternion cachedRot = Quaternion.identity;
        //protected Matrix4x4 cachedMatrix = Matrix4x4.identity;

        public void SetOffsets(Vector3 offsetPos, Vector3 offsetRotEuler, Vector3 multiplyScale)
        {
            cachedRot = Quaternion.Euler(offsetRotEuler);
            this.offsetRot = offsetRotEuler;
            this.offsetPos = offsetPos;
            this.multiplyScale = multiplyScale;

            //cachedMatrix = Matrix4x4.TRS(offsetPos, cachedRot, offsetScale);
        }

        public void Awake()
        {
            SetOffsets(offsetPos, offsetRot, multiplyScale);
        }
        protected bool registered;
        public void Register()
        {
            if (registered) return;
            ProxyTransformSingleton.Register(this);
            registered = true;
        }
        public void Unregister()
        {
            ProxyTransformSingleton.Unregister(this); 
            registered = false;
        }
        public void Start() 
        {
            Register();
        }
        public void OnEnable()
        {
            Register();
        }
        public void OnDisable()
        {
            Unregister();
        }
        public void OnDestroy() => ProxyTransformSingleton.Unregister(this);

        public void Refresh()
        {
            if (transformToCopy == null) return;
            RefreshUnsafe();
        }
        public void RefreshUnsafe()
        {
            var transform = this.transform;
            if (applyInWorldSpace)
            {
                transformToCopy.GetPositionAndRotation(out Vector3 worldPos, out Quaternion worldRot);
                var parent = transformToCopy.parent;
                if (parent != null)
                {
                    var parentRot = parent.rotation;
                    transform.SetPositionAndRotation(worldPos + (parentRot * OffsetPos), worldRot * cachedRot);
                } 
                else
                {
                    transform.SetPositionAndRotation(worldPos + OffsetPos, worldRot * cachedRot);
                }
            } 
            else
            {
                transformToCopy.GetLocalPositionAndRotation(out Vector3 localPos, out Quaternion localRot); 
                transform.SetLocalPositionAndRotation(localPos + OffsetPos, localRot * cachedRot); 
                var copyLs = transformToCopy.localScale;
                var ms = MultiplyScale;
                transform.localScale = new Vector3(copyLs.x * ms.x, copyLs.y * ms.y, copyLs.z * ms.z);
            }
        }

    }

    public class ProxyTransformSingleton : SingletonBehaviour<ProxyTransformSingleton>
    {
        public static int ExecutionPriority => ProxyBoneJobs.ExecutionPriority + 1; // Update after animators and proxy bones
        public override int Priority => ExecutionPriority;

        public override void OnFixedUpdate() {}

        public override void OnLateUpdate()
        {
            bool removeNull = false;
            foreach(var transform in transforms)
            {
                if (transform == null || transform.transformToCopy == null)
                {
                    removeNull = true;
                    continue;
                }

                if (transform.enabled) transform.RefreshUnsafe(); 
            }
            if (removeNull) transforms.RemoveAll(i => i == null || i.transformToCopy == null);
        }

        public override void OnUpdate() {}

        protected List<ProxyTransform> transforms = new List<ProxyTransform>();

        public static void Register(ProxyTransform transform)
        {
            if (transform == null) return;

            var instance = Instance;
            if (instance == null || instance.transforms.Contains(transform)) return;

            instance.transforms.Add(transform);
        }
        public static void Unregister(ProxyTransform transform)
        {
            var instance = Instance;
            if (instance == null) return;

            instance.transforms.RemoveAll(i => i == null || i == transform);
        }

    }

}


#endif