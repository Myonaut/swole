using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Unity.Mathematics;

namespace Swole
{
    public class ShakyCamera : MonoBehaviour
    {

        protected Vector3 startPosition;
        protected Quaternion startRotation;

        public bool updateManually;

        [SerializeField, Range(0f, 1f)]
        public float shake;
        public float shakeRotationScale = 2f;
        public float shakeFrequency = 0.05f;

        private float shakeT;
        [SerializeField]
        private float shakeX;
        [SerializeField]
        private float shakeY;
        [SerializeField]
        private float targetShakeX;
        [SerializeField]
        private float targetShakeY;

        public float shakeDecay = 0f;
        public float shakeDecayDelta = 0f;
        public float shakeDecayDeltaRecovery = 1f;

        [Tooltip("In degrees per second")]
        public float offsetDecay = 7f;
        [Tooltip("In degrees per second")]
        public float offsetDecayDelta;
        [Tooltip("In degrees per second")]
        public float offsetDecayDeltaRecovery = 5f;

        private Quaternion offset;

        [Tooltip("In degrees per second")]
        public float offsetAngularVelocityDecay = 2f;

        [SerializeField]
        private Vector3 offsetAngularVelocity;

        private Quaternion customOffset;
        public Quaternion CustomOffset
        {
            get => customOffset;
            set => customOffset = value;
        }

        protected void Awake()
        {
            startPosition = transform.localPosition;
            startRotation = transform.localRotation;

            offset = Quaternion.identity;
            customOffset = Quaternion.identity;
        }

        protected void LateUpdate()
        {
            if (!updateManually) UpdateStep(Time.deltaTime);
        }

        public void UpdateStep(float deltaTime) 
        {
            Vector3 pos = startPosition;
            Quaternion rot = startRotation;

            shakeT -= deltaTime;
            if (shakeT <= 0f)
            {
                shakeT = shakeFrequency;

                float prevTargetNoiseX = targetShakeX;
                float prevTargetNoiseY = targetShakeY;

                targetShakeX = math.remap(0f, 1f, -1f, 1f, UnityEngine.Random.value);
                targetShakeY = math.remap(0f, 1f, -1f, 1f, UnityEngine.Random.value);

                float dot = Vector2.Dot(new Vector2(prevTargetNoiseX, prevTargetNoiseY), new Vector2(targetShakeX, targetShakeY));
                if (dot > 0f)
                {
                    targetShakeX = -targetShakeX;
                    targetShakeY = -targetShakeY;
                }
            }

            float noiseDelta = deltaTime / shakeFrequency;
            shakeX = Mathf.MoveTowards(shakeX, targetShakeX, noiseDelta);
            shakeY = Mathf.MoveTowards(shakeY, targetShakeY, noiseDelta);

            float scale = shakeRotationScale * shake;
            rot = Quaternion.Euler(shakeX * scale, shakeY * scale, 0f) * rot;

            shake = Mathf.MoveTowards(shake, 0f, (shakeDecay + shakeDecayDelta) * deltaTime);
            shakeDecayDelta = Mathf.MoveTowards(shakeDecayDelta, 0f, shakeDecayDeltaRecovery * deltaTime); 


            if (offsetAngularVelocity == Vector3.zero)
            {
                offset = Quaternion.RotateTowards(offset, Quaternion.identity, ((offsetDecay + offsetDecayDelta) * Mathf.Max(1f, Quaternion.Angle(Quaternion.identity, offset))) * deltaTime); 
                offsetDecayDelta = Mathf.MoveTowards(offsetDecayDelta, 0f, offsetDecayDeltaRecovery * deltaTime);
            } 
            else
            {
                var velocity = Quaternion.Euler(offsetAngularVelocity);
                offset = velocity * offset;
                float decay = offsetAngularVelocityDecay * Mathf.Pow(Mathf.Max(1f, Quaternion.Angle(Quaternion.identity, velocity) * 5f), 1.25f) * deltaTime;   
                offsetAngularVelocity = new Vector3(Mathf.MoveTowardsAngle(offsetAngularVelocity.x, 0f, decay), Mathf.MoveTowardsAngle(offsetAngularVelocity.y, 0f, decay), Mathf.MoveTowardsAngle(offsetAngularVelocity.z, 0f, decay)); 
            }

            rot = customOffset * offset * rot;

            transform.SetLocalPositionAndRotation(pos, rot);
        }

    }
}
