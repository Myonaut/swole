#if UNITY_2017_1_OR_NEWER

using UnityEngine;

namespace Swole
{
    public class ForceDiscardRootMotion : MonoBehaviour
    {
        // Keeping this empty forces the Animator to pass bone data 
        // to the script instead of moving the GameObject container.
        void OnAnimatorMove()
        {
            // Do nothing here. 
            // The GameObject stays put, but the bone will finally move!
        }

        public static void Setup(Transform rootTransform)
        {
            Animator animator = rootTransform.GetComponentInChildren<Animator>(true);
            if (animator == null) return;

            rootTransform = animator.transform;

            // Turn on Apply Root Motion so the script can capture it
            animator.applyRootMotion = true;

            // Find the true top-level bone transform
            Transform topBone = FindTopLevelBone(rootTransform);

            if (topBone != null && topBone != rootTransform)
            {
                if (rootTransform.gameObject.GetComponent<ForceDiscardRootMotion>() == null)
                {
                    rootTransform.gameObject.AddComponent<ForceDiscardRootMotion>();
                    Debug.Log($"Successfully attached bypass script to top bone: {topBone.name}"); 
                }
            }
        }

        private static Transform FindTopLevelBone(Transform root)
        {
            SkinnedMeshRenderer[] renderers = root.GetComponentsInChildren<SkinnedMeshRenderer>();
            Transform highestBone = null;

            foreach (var smr in renderers)
            {
                if (smr.bones == null || smr.bones.Length == 0) continue;

                // The rootBone property of a SkinnedMeshRenderer points to its local hierarchy origin
                Transform currentRoot = smr.rootBone;

                if (currentRoot != null)
                {
                    // Fallback check to find the absolute highest parent that isn't the GameObject root
                    while (currentRoot.parent != null && currentRoot.parent != root)
                    {
                        currentRoot = currentRoot.parent;
                    }
                    highestBone = currentRoot;
                    break; // Found the primary hierarchy tree branch
                }
            }

            return highestBone;
        }
    }
}

#endif