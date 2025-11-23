#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole
{
    public class CameraDependentVisibility : MonoBehaviour
    {

#if UNITY_EDITOR
        public void OnDrawGizmosSelected()
        {
            if (visibilityCones != null)
            {
                var pos = ReferenceTransform.position;

                foreach(var cone in visibilityCones)
                {
                    Gizmos.color = Color.green;

                    Vector3 dir = cone.direction.normalized;
                    Gizmos.DrawLine(pos + (dir * (cone.minDistance != cone.maxDistance ? cone.minDistance : 0f)), pos + (dir * (cone.minDistance != cone.maxDistance ? cone.maxDistance : 1f))); 
                }
            }
        }
#endif

        new public Camera camera;

        [SerializeField]
        protected Transform referenceTransform;
        public Transform ReferenceTransform
        {
            set => referenceTransform = value;
            get => referenceTransform == null ? transform : referenceTransform;
        }

        [Serializable]
        public struct VisibilityCone
        {
            public Vector3 direction;
            public float minDot;
            public float maxDot;
            public float minDistance;
            public float maxDistance;
        }

        public List<VisibilityCone> visibilityCones = new List<VisibilityCone>();

        public List<GameObject> objectsToDeactivate = new List<GameObject>();
        public List<Renderer> renderersToDeactivate = new List<Renderer>(); 

        public bool IsVisible()
        {
            if (camera == null) camera = Camera.main;
            if (camera == null) return false;

            if (visibilityCones != null)
            {
                Vector3 pos = ReferenceTransform.position;

                Vector3 camPos = camera.transform.position;
                Vector3 camDir = camera.transform.forward;

                Vector3 offset = pos - camPos;
                float distance = offset.magnitude;

                foreach (var cone in visibilityCones)
                {
                    if (cone.minDistance != cone.maxDistance && (distance < cone.minDistance || distance > cone.maxDistance)) continue;

                    if (cone.minDot == cone.maxDot) 
                    { 
                        return true; 
                    }
                    else
                    {
                        Vector3 coneDir = cone.direction.normalized;
                        float dot = Vector3.Dot(coneDir, -camDir); 

                        if (dot >= cone.minDot && dot <= cone.maxDot) return true;
                    }
                }
            }

            return false;
        }

        public void SetVisible(bool isVisible)
        {
            if (isVisible)
            {
                Show();
            } 
            else
            {
                Hide();
            }
        }
        public void Show()
        {
            isVisible = true;

            if (objectsToDeactivate != null)
            {
                foreach (var obj in objectsToDeactivate) obj.SetActive(true);
            }
            if (renderersToDeactivate != null)
            {
                foreach (var renderer in renderersToDeactivate) renderer.enabled = true;
            }
        }
        public void Hide()
        {
            isVisible = false;

            if (objectsToDeactivate != null)
            {
                foreach (var obj in objectsToDeactivate) obj.SetActive(false);
            }
            if (renderersToDeactivate != null)
            {
                foreach (var renderer in renderersToDeactivate) renderer.enabled = false; 
            }
        }

        protected bool isVisible;

        void LateUpdate()
        {
            if (IsVisible())
            {
                if (!isVisible)
                {
                    Show();
                }
            } 
            else
            {
                if (isVisible)
                {
                    Hide();
                }
            }
        }
    }
}

#endif