#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Animations;

namespace Swole
{
    public class IdenticalTransformConstraintCreator : MonoBehaviour
    {
        public Transform rootObject;

        public List<Transform> transformsToConstrain = new List<Transform>();

        public void Awake()
        {
            if (rootObject != null && transformsToConstrain != null)
            {
                foreach(var t in transformsToConstrain)
                {
                    var toCopy = rootObject.FindDeepChildLiberal(t.name);
                    if (toCopy != null)
                    {
                        var constraint = t.gameObject.AddComponent<ParentConstraint>(); 
                        constraint.AddSource(new ConstraintSource() { sourceTransform = toCopy, weight = 1 });
                        constraint.constraintActive = true;
                    }
                }
            }
        }
    }
}

#endif