#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Swole.UI
{
    public class UIRotator : MonoBehaviour
    {

        [SerializeField]
        protected RectTransform target;

        public float degreesPerSecond = 360f;
        public float angleStep;
        public Vector3 axis = Vector3.forward;

        private float angle;
        private float displayAngle;

        void Awake()
        {
            if (target == null) target = gameObject.GetComponent<RectTransform>(); 
        }

        void Update()
        {
            if (target != null)
            {
                angle += degreesPerSecond * Time.deltaTime;
                angle = Maths.NormalizeAngle(angle);
                displayAngle = angleStep <= 0f ? angle : (angleStep * Mathf.Floor(angle / angleStep));
                target.localRotation = Quaternion.AngleAxis(-displayAngle, axis);   
            }
        }
    }
}

#endif