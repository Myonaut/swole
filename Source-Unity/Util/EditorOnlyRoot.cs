#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole.Unity
{
    [ExecuteInEditMode]
    public class EditorOnlyRoot : MonoBehaviour
    {
        public string editorTag = "EditorOnly";

        public bool applyToChildren;
        public bool applyToTopLevelChildren;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (applyToChildren)
            {
                applyToChildren = false; 
                ApplyTagToChildren();
            }

            if (applyToTopLevelChildren)
            {
                applyToTopLevelChildren = false;
                ApplyTagToTopLevelChildren();
            }
        }
#endif

        public void ApplyTagToChildren()
        {
            var children = GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children) 
            {
                if (child != null)
                {
                    child.gameObject.tag = editorTag;
                }
            }
        }
        public void ApplyTagToTopLevelChildren()
        {
            gameObject.tag = editorTag;

            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child != null)
                {
                    child.gameObject.tag = editorTag;
                }
            }
        }
    }
}

#endif