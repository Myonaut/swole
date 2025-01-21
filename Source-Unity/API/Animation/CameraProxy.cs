#if (UNITY_STANDALONE || UNITY_EDITOR)

using UnityEngine;

namespace Swole.API.Unity
{
    public class CameraProxy : MonoBehaviour
    {

        public const int _defaultFieldOfView = 60;

        public bool useLocalSpace;

        public bool lockRoll;
        public float rollToLock;

        [SerializeField]
        protected Camera targetCamera;
        protected Transform targetTransform; 

        protected bool nonRelativeMode;
        public bool IsInRelativeMode
        {
            get => !nonRelativeMode;
            set => nonRelativeMode = !value;
        }

        protected Vector3 prevPosition;
        protected Quaternion prevRotation;

        [HideInInspector, AnimatableProperty(true, _defaultFieldOfView)]
        public float fieldOfView = _defaultFieldOfView;
        /// <summary>
        /// Scaled fov using the transform's local scale along the z axis. Makes it easy to animate the fov using scale inside the animation editor.
        /// </summary>
        public float ScaledFieldOfView => fieldOfView * Mathf.Abs(transform.localScale.z); 
        protected float prevFieldOfView;

        protected float lockedRoll;
        protected virtual void Awake()
        {
            SetTargetCamera(targetCamera);
            fieldOfView = _defaultFieldOfView;
            ApplyChanges(false);
        }

        public virtual void ApplyChanges(bool relatively)
        {
            float fov = ScaledFieldOfView;

            if (useLocalSpace)
            {
                transform.GetLocalPositionAndRotation(out Vector3 pos, out Quaternion rot);

                if (targetTransform != null)
                {
                    if (relatively)
                    {
                        targetTransform.GetLocalPositionAndRotation(out Vector3 tPos, out Quaternion tRot);
                        rot = (Quaternion.Inverse(prevRotation) * rot) * tRot;
                        if (lockRoll) rot = Quaternion.Euler(rot.eulerAngles.x, rot.eulerAngles.y, lockedRoll);
                        targetTransform.SetLocalPositionAndRotation(tPos + (pos - prevPosition), rot);
                    }
                    else
                    {
                        if (lockRoll) rot = Quaternion.Euler(rot.eulerAngles.x, rot.eulerAngles.y, lockedRoll);
                        targetTransform.SetLocalPositionAndRotation(pos, rot);
                    }
                }

                prevPosition = pos;
                prevRotation = rot;
            }
            else
            {
                transform.GetPositionAndRotation(out Vector3 pos, out Quaternion rot);

                if (targetTransform != null)
                {
                    if (relatively)
                    {
                        targetTransform.GetPositionAndRotation(out Vector3 tPos, out Quaternion tRot);
                        rot = (Quaternion.Inverse(prevRotation) * rot) * tRot;
                        if (lockRoll) rot = Quaternion.Euler(rot.eulerAngles.x, rot.eulerAngles.y, lockedRoll);
                        targetTransform.SetPositionAndRotation(tPos + (pos - prevPosition), rot);
                    }
                    else
                    {
                        if (lockRoll) rot = Quaternion.Euler(rot.eulerAngles.x, rot.eulerAngles.y, lockedRoll); 
                        targetTransform.SetPositionAndRotation(pos, rot);
                    }
                }

                prevPosition = pos;
                prevRotation = rot;
            }

            if (relatively)
            {
                if (targetCamera != null)
                {
                    targetCamera.fieldOfView = targetCamera.fieldOfView + (fov - prevFieldOfView);
                }
            }
            else
            {
                if (targetCamera != null)
                {
                    targetCamera.fieldOfView = fov;
                }
            }
            prevFieldOfView = fov; 
        }
        public void ApplyChanges() => ApplyChanges(IsInRelativeMode); 

        public virtual void SetTargetCamera(Camera targetCamera)
        {
            this.targetCamera = targetCamera; 

            targetTransform = null;
            if (targetCamera != null ) targetTransform = targetCamera.transform;
        }

    }
}

#endif