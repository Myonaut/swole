#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering;

namespace Swole.Unity
{
    public class MultiEditRenderers : MonoBehaviour
    {

        public bool apply;

#if UNITY_EDITOR
        protected void OnValidate()
        {
            if (apply)
            {
                apply = false;
                Apply(applyToChildren, includeInactive);
            }
        }
#endif

        [Serializable]
        public struct Replacement
        {
            public Material targetMaterial;
            public Material replacementMaterial;

            public bool setShadowCastingMode;
            public ShadowCastingMode shadowCastingMode;
        }

        public List<Replacement> replacements = new List<Replacement>();

        public bool applyToChildren = true;
        public bool includeInactive = true;

        public bool setGlobalShadowCastingMode;
        public ShadowCastingMode globalShadowCastingMode;

        public List<MeshRenderer> targetMeshRenderers;
        public List<SkinnedMeshRenderer> targetSkinnedRenderers;

        public void Apply(bool applyToChildren, bool includeInactive)
        {
            if (replacements != null) 
            {
                List<MeshRenderer> meshRenderers = new List<MeshRenderer>();
                if (targetMeshRenderers != null) meshRenderers.AddRange(targetMeshRenderers);

                List<SkinnedMeshRenderer> skinnedMeshRenderers = new List<SkinnedMeshRenderer>();
                if (targetSkinnedRenderers != null) skinnedMeshRenderers.AddRange(targetSkinnedRenderers);

                if (applyToChildren)
                {
                    var childMeshRenderers = gameObject.GetComponentsInChildren<MeshRenderer>(true);
                    meshRenderers.AddRange(childMeshRenderers);

                    var childSkinnedRenderers = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                    skinnedMeshRenderers.AddRange(childSkinnedRenderers);
                }


                foreach (var renderer in meshRenderers)
                {
                    if (!includeInactive && (!renderer.enabled || !renderer.gameObject.activeInHierarchy)) continue;

                    if (setGlobalShadowCastingMode) renderer.shadowCastingMode = globalShadowCastingMode;

                    var mats = renderer.sharedMaterials;
                    if (mats != null && mats.Length > 0)
                    {
                        for (int a = 0; a < mats.Length; a++)
                        {
                            var mat = mats[a];
                            foreach (var replacement in replacements)
                            {
                                if (ReferenceEquals(mat, replacement.targetMaterial) || ReferenceEquals(mat, replacement.replacementMaterial))
                                {
                                    mats[a] = replacement.replacementMaterial;
                                    if (replacement.setShadowCastingMode) renderer.shadowCastingMode = replacement.shadowCastingMode;
                                }
                            }
                        }

                        renderer.sharedMaterials = mats;
                    }
                }
                foreach (var renderer in skinnedMeshRenderers)
                {
                    if (!includeInactive && (!renderer.enabled || !renderer.gameObject.activeInHierarchy)) continue;

                    if (setGlobalShadowCastingMode) renderer.shadowCastingMode = globalShadowCastingMode;

                    var mats = renderer.sharedMaterials;
                    if (mats != null && mats.Length > 0)
                    {
                        for (int a = 0; a < mats.Length; a++)
                        {
                            var mat = mats[a];
                            foreach (var replacement in replacements)
                            {
                                if (ReferenceEquals(mat, replacement.targetMaterial) || ReferenceEquals(mat, replacement.replacementMaterial))
                                {
                                    mats[a] = replacement.replacementMaterial;
                                    if (replacement.setShadowCastingMode) renderer.shadowCastingMode = replacement.shadowCastingMode;
                                }
                            }
                        }

                        renderer.sharedMaterials = mats;
                    }
                }
            }
        }

    }
}

#endif