#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole
{

    public class RenderLayerSetter : MonoBehaviour
    {

        [Serializable]
        public struct Setter
        {
            public Shader[] targetShaders;
            public bool HasShader(Shader shader)
            {
                if (targetShaders == null) return false; 

                foreach (var s in targetShaders)
                {
                    if (s == shader) return true;
                }
                return false;
            }

            public UnityRenderLayer layers;

            public bool setObjectLayer;
            public int objectLayer;
        }

        public List<Setter> setters = new List<Setter>();

        public void Apply(Transform root = null)
        {
            if (root == null) root = transform;

            var renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                var mats = renderer.sharedMaterials;
                if (mats == null) continue;

                foreach (var mat in mats)
                {
                    if (mat == null) continue;

                    foreach(var setter in setters)
                    {
                        if (setter.HasShader(mat.shader)) 
                        { 
                            renderer.renderingLayerMask = (uint)setter.layers;
                            if (setter.setObjectLayer)
                            {
                                renderer.gameObject.layer = setter.objectLayer;
                            }
                        }
                    }
                }
            }
        }

        public void ApplyDelayed(float delay, Transform root = null)
        {
            StartCoroutine(ApplyAfterTime(delay, root));
        }
        private IEnumerator ApplyAfterTime(float time, Transform root = null)
        {
            yield return new WaitForSeconds(time); 
            Apply(root);
        }

        public void ApplyFrameDelayed(int frames, Transform root = null)
        {
            StartCoroutine(ApplyAfterFrames(frames, root));
        }
        private IEnumerator ApplyAfterFrames(int frames, Transform root = null)
        {
            int i = 0;
            while (i < frames)
            {
                yield return null;
                i++;
            }

            Apply(root);
        }

        public bool applyOnAwake;
        public bool applyOnStart;
        public float autoApplyDelay;
        public bool delayAsFrames;

        protected void Awake()
        {
            if (applyOnAwake)
            {
                if (autoApplyDelay > 0f)
                {
                    if (delayAsFrames) ApplyFrameDelayed((int)autoApplyDelay); else ApplyDelayed(autoApplyDelay);
                } 
                else
                {
                    Apply();
                }
            }
        }

        protected void Start()
        {
            if (applyOnStart)
            {
                if (autoApplyDelay > 0f)
                {
                    if (delayAsFrames) ApplyFrameDelayed((int)autoApplyDelay); else ApplyDelayed(autoApplyDelay);
                }
                else
                {
                    Apply();
                }
            }
        }

    }

    [Serializable, Flags]
    public enum UnityRenderLayer
    {
        None = 0, Everything = 1, Layer1 = 2, Layer2 = 4, Layer3 = 8, Layer4 = 16, Layer5 = 32, Layer6 = 64, Layer7 = 128
    }

}

#endif