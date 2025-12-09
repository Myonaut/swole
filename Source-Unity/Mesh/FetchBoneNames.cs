#if (UNITY_EDITOR || UNITY_STANDALONE)

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Swole
{

    [ExecuteInEditMode]
    public class FetchBoneNames : MonoBehaviour
    {

        public bool apply = false; 

        public Transform root;
        public SkinnedMeshRenderer smr;

        public List<string> output;

        public void Update()
        {
            if (apply)
            {
                apply = false;
                Apply();
            }
        }

        public void Apply()
        {
            if (output == null) output = new List<string>();
            output.Clear();

            if (smr == null && root == null) smr = gameObject.GetComponent<SkinnedMeshRenderer>();
            if (smr != null)
            {
                FromSkinnedMeshRenderer(smr, output);
                return;
            }

            FromRoot(root, output);
        }

        public static List<string> FromRoot(Transform rootBone, List<string> list = null)
        {
            if (list == null) list = new List<string>();
            if (rootBone == null) return list;
            var bones = rootBone.GetComponentsInChildren<Transform>();
            foreach (var bone in bones) if (bone != null) list.Add(bone.name);
            return list;
        }

        public static List<string> FromSkinnedMeshRenderer(SkinnedMeshRenderer renderer, List<string> list = null)
        {
            if (list == null) list = new List<string>();
            if (renderer == null) return list;
            var bones = renderer.bones;
            foreach (var bone in bones) if (bone != null) list.Add(bone.name);
            return list;
        }

    }

}

#endif