#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Swole
{
    [ExecuteAlways]
    public class CopyTransformState : MonoBehaviour
    {    

        public Transform localTransform;
        public Transform transformToCopy;

        public bool localSpace;

        public bool destroyOnAwake = true;
        public bool applyChildren;

        public void Apply() => Apply(applyChildren); 
        public void Apply(bool applyChildren)
        {
            if (localTransform == null) localTransform = transform;

            if (transformToCopy != null)
            {
                if (localSpace)
                {
                    transformToCopy.GetLocalPositionAndRotation(out var p, out var r);
                    localTransform.SetLocalPositionAndRotation(p, r);
                    localTransform.localScale = transformToCopy.localScale;
                }
                else
                {
                    transformToCopy.GetPositionAndRotation(out var p, out var r);
                    localTransform.SetPositionAndRotation(p, r);
                    localTransform.localScale = transformToCopy.localScale; 
                }
            }

            if (applyChildren)
            {
                foreach (Transform child in transform)
                {
                    var copy = child.GetComponent<CopyTransformState>(); 
                    if (copy != null)
                    {
                        copy.Apply(applyChildren);
                    }
                }
            }
        }

#if UNITY_EDITOR
        public bool apply;

        public void OnValidate()
        {
            if (apply)
            {
                Apply();
                apply = false;
            }
        }
#endif

        public void Awake()
        {
            if (Application.isPlaying)
            {
                Apply();

                if (destroyOnAwake)
                {
                    Destroy(this);
                }
            }
        }

    }
}

#endif