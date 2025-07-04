#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Swole.API.Unity.Animation;

namespace Swole.API.Unity
{
    public class ThirdPersonCamera : ExecutableBehaviourObject
    {
        public static int ExecutionPriority => CustomAnimatorUpdater.FinalAnimationBehaviourPriority + 10;
        public override int Priority => ExecutionPriority;

        protected void OnEnable()
        {
            swole.Register(this);
        }
        protected void OnDisable()
        {
            swole.Unregister(this);
        }
        protected void OnDestroy()
        {
            swole.Unregister(this);
        }

        [SerializeField]
        new protected Camera camera;
        public Camera Camera
        {
            get => camera;
            set => SetCamera(value);
        }
        public void SetCamera(Camera camera)
        {
            this.camera = camera;
        }

        [SerializeField]
        protected Transform target;
        public Transform Target
        {
            get => target;
            set => SetTarget(value);
        }
        public void SetTarget(Transform target)
        {
            this.target = target;
        }

        [SerializeField]
        protected Vector3 targetPosition;
        public Vector3 TargetPosition
        {
            get => targetPosition;
            set => SetTargetPosition(value);
        }
        public void SetTargetPosition(Vector3 targetPosition)
        {
            this.targetPosition = targetPosition;
        }

        [SerializeField]
        protected Vector3 targetOffset;
        public Vector3 TargetOffset
        {
            get => targetOffset;
            set => SetTargetOffset(value);
        }
        public void SetTargetOffset(Vector3 targetOffset)
        {
            this.targetOffset = targetOffset;
        }

        [SerializeField]
        protected Vector3 targetOffsetCameraSpace;
        public Vector3 TargetOffsetCameraSpace
        {
            get => targetOffsetCameraSpace;
            set => SetTargetOffsetCameraSpace(value);
        }
        public void SetTargetOffsetCameraSpace(Vector3 targetOffsetCameraSpace)
        {
            this.targetOffsetCameraSpace = targetOffsetCameraSpace;
        }

        public Vector3 FinalTargetPosition
        {
            get
            {
                Quaternion targetRotation = Quaternion.identity;
                if (target != null)
                {
                    target.GetPositionAndRotation(out targetPosition, out targetRotation);
                }

                return targetPosition + (targetRotation * targetOffset);
            }
        }

        [SerializeField]
        protected Transform pivot;
        public Transform Pivot
        {
            get => pivot;
            set => SetPivot(value);
        }
        public void SetPivot(Transform pivot)
        {
            this.pivot = pivot;
        }

        [SerializeField]
        protected Vector3 pivotPosition;
        public Vector3 PivotPosition
        {
            get => pivotPosition;
            set => SetPivotPosition(value);
        }
        public void SetPivotPosition(Vector3 pivotPosition)
        {
            this.pivotPosition = pivotPosition;
        }

        [SerializeField]
        protected Vector3 pivotOffset;
        public Vector3 PivotOffset
        {
            get => pivotOffset;
            set => SetPivotOffset(value);
        }
        public void SetPivotOffset(Vector3 pivotOffset)
        {
            this.pivotOffset = pivotOffset;
        }

        [SerializeField]
        protected Vector3 pivotOffsetCameraSpace;
        public Vector3 PivotOffsetCameraSpace
        {
            get => pivotOffsetCameraSpace;
            set => SetPivotOffsetCameraSpace(value);
        }
        public void SetPivotOffsetCameraSpace(Vector3 pivotOffsetCameraSpace)
        {
            this.pivotOffsetCameraSpace = pivotOffsetCameraSpace; 
        }

        public Vector3 FinalPivotPosition
        {
            get
            {
                Quaternion pivotRotation = Quaternion.identity;
                if (pivot != null)
                {
                    pivot.GetPositionAndRotation(out pivotPosition, out pivotRotation);
                }

                return pivotPosition + (pivotRotation * pivotOffset); 
            }
        }

        public LayerMask collisionMask;

        public Vector3 initialOrbitRotationEuler;
        public Vector3 worldUp = Vector3.up;

        public float orbitDistance = 2;
        public float orbitYaw = 0;
        [Range(-90, 90)]
        public float orbitPitch = 0;

        public float minOrbitPitch = -75f;
        public float maxOrbitPitch = 75f;

        public float sensitivityMouseX = 1f;
        public float sensitivityMouseY = 1f;

        public float sensitivityGamepadX = 1f;
        public float sensitivityGamepadY = 1f;

        protected void Awake()
        {
            if (camera == null) SetCamera(gameObject.GetComponent<Camera>());
        }

        public override void OnLateUpdate()
        {
            ApplyInput((InputProxy.CursorAxisX * sensitivityMouseX) + (InputProxy.MainRightJoystickHorizontal * sensitivityGamepadX), (-InputProxy.CursorAxisY * sensitivityMouseY) + (-InputProxy.MainRightJoystickVertical * sensitivityGamepadY));
            SyncPositionAndRotation();
        }

        public void ApplyInput(float inputHor, float inputVer)
        {
            orbitYaw = orbitYaw + inputHor;
            orbitPitch = orbitPitch + inputVer;  

            orbitYaw = Maths.NormalizeDegrees(orbitYaw);
            orbitPitch = Mathf.Clamp(orbitPitch, minOrbitPitch, maxOrbitPitch);
        }
        public void SyncPositionAndRotation()
        {
            if (camera == null) return;
            var transform = camera.transform; 

            transform.GetPositionAndRotation(out Vector3 cameraPosition, out Quaternion cameraRotation);

            Quaternion cameraInputRotation = Quaternion.Euler(orbitPitch, orbitYaw, 0) * Quaternion.Euler(initialOrbitRotationEuler);

            Vector3 pivotPosition = FinalPivotPosition;
            Vector3 targetCameraPosition = pivotPosition + (cameraInputRotation * pivotOffsetCameraSpace);
            targetCameraPosition = targetCameraPosition + (cameraInputRotation * Vector3.back * orbitDistance);

            Vector3 targetPosition = FinalTargetPosition + (cameraInputRotation * targetOffsetCameraSpace);

            Vector3 pivotDir = targetCameraPosition - pivotPosition;
            float dist = pivotDir.magnitude;
            if (dist > 0)
            {
                pivotDir = pivotDir / dist;
                if (Physics.Raycast(pivotPosition, pivotDir, out RaycastHit hit, dist, collisionMask))
                {
                    targetCameraPosition = pivotPosition + pivotDir * (hit.distance - 0.001f);
                }
            }

            Quaternion targetCameraRotation = Quaternion.LookRotation(targetPosition - targetCameraPosition, worldUp); 

            cameraPosition = targetCameraPosition;
            cameraRotation = targetCameraRotation;

            transform.SetPositionAndRotation(cameraPosition, cameraRotation);  
        }

    }
}

#endif