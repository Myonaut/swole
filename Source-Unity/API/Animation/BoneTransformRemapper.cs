#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Swole.API.Unity
{

    /// <summary>
    /// Used to swap bone transforms in a skinned rig.
    /// </summary>
    public class BoneTransformRemapper : MonoBehaviour
    {

        public string remapPrefix;

        [Serializable]
        public struct Remapping
        {
            [Tooltip("Will only be used if target bone is not set.")]
            public string targetBoneName;
            [Tooltip("Only bones with that are the target bone instance will get replaced.")]
            public bool onlyByReference;
            public Transform targetBone;
            [Tooltip("If left blank, will search for a transform with the name of the target bone combined with the Remap Prefix.")]
            public Transform newBone;
            [Tooltip("If set, will be used to find the new bone if new bone is not explicitly set.")]
            public Transform rootOverride;
        }

        [SerializeField]
        public Remapping[] remappings;

        [Header("Unity Skinning")]
        public bool applyOnEnable;
        public bool applyToAllIfTargetRenderersIsEmpty;
        public SkinnedMeshRenderer[] targetRenderers;
        public bool ShouldRemap(SkinnedMeshRenderer renderer)
        {
            if ((targetRenderers == null || targetRenderers.Length == 0) && applyToAllIfTargetRenderersIsEmpty) return true;

            for (int a = 0; a < targetRenderers.Length; a++) if (targetRenderers[a] == renderer) return true;
            return false;
        }

        [Header("Custom Skinning")]
        public bool applyToAllIfTargetCustomRenderersIsEmpty;
        public CustomSkinnedMeshRenderer[] targetCustomRenderers;
        public bool ShouldRemap(CustomSkinnedMeshRenderer renderer)
        {
            if ((targetCustomRenderers == null || targetCustomRenderers.Length == 0) && applyToAllIfTargetCustomRenderersIsEmpty) return true;

            for (int a = 0; a < targetCustomRenderers.Length; a++) if (targetCustomRenderers[a] == renderer) return true;
            return false;
        }

        public void OnEnable()
        {
            if (applyOnEnable) Apply();  
        }

        public void Apply()
        {
            if (remappings == null) return;

            if (targetRenderers == null || targetRenderers.Length <= 0)
            {
                if (!applyToAllIfTargetRenderersIsEmpty) return;
                var renderers = GetComponentsInChildren<SkinnedMeshRenderer>(true); 
                foreach (var renderer in renderers) Remap(renderer);
            }
            else
            {
                foreach (var renderer in targetRenderers) Remap(renderer);
            }
        }

        public void Remap(SkinnedMeshRenderer renderer)
        {
            if (renderer == null) return;

            var bones = renderer.bones;
            var root = renderer.rootBone;
            if (root == null) root = renderer.transform;
            if (root != null && root.parent != null) root = root.parent;
            if (root == null) root = transform;
            Remap(bones, root);
            renderer.bones = bones;
        }
        public void Remap(Transform[] bones, Transform root)
        {
            if (bones == null) return;

            foreach(var remapping in remappings)
            {
                var localRoot = root;
                if (remapping.rootOverride != null) localRoot = remapping.rootOverride;

                string boneName = remapping.targetBoneName;
                if (remapping.targetBone != null) boneName = remapping.targetBone.name;

                string newBoneName = boneName;
                Transform newBone = remapping.newBone;
                if (newBone == null)
                {
                    if (localRoot == null) continue;
                    newBoneName = (remapPrefix == null ? string.Empty : remapPrefix) + boneName; 
                    newBone = localRoot.FindDeepChild(newBoneName);
                }

                if (newBone == null) 
                {
                    swole.LogWarning($"Failed to find remap bone target {newBoneName} under {(root == null ? "null" : root.name)}");  
                    continue;
                }
                for(int a = 0; a < bones.Length; a++)
                {
                    var bone = bones[a];
                    if (bone == null) continue;

                    if (bone.name != boneName || (remapping.onlyByReference && bone != remapping.targetBone)) continue;
                    bones[a] = newBone;
                }
            }
        }

    }

}

#endif