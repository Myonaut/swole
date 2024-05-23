#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Swole.API.Unity
{

    /// <summary>
    /// Translates bone names from one rig to another. Useful for remapping animations.
    /// </summary>
    [CreateAssetMenu(fileName = "RigRemapping", menuName = "Rigs/Remapping", order = 1)]
    public class RigRemapping : ScriptableObject
    {

        public static RigRemapping Create(string path = null, string fileName = null, bool incrementIfExists = false)
        {

            RigRemapping asset = ScriptableObject.CreateInstance<RigRemapping>();

#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(fileName))
            {
                string fullPath = $"{(path + (path.EndsWith('/') ? "" : "/"))}{fileName}.asset";
                if (incrementIfExists) fullPath = AssetDatabase.GenerateUniqueAssetPath(fullPath);
                AssetDatabase.CreateAsset(asset, fullPath);
                AssetDatabase.SaveAssets();
            }
#endif

            return asset;
        }

        [Serializable]
        public struct Remapping
        {
            public string inputName;
            public string outputName;
        }

        public Remapping[] remappedBones;

        public string Remap(string inputName)
        {
            if (remappedBones == null || string.IsNullOrEmpty(inputName)) return inputName;

            string inputName_ = inputName.Trim();
            foreach(Remapping remapping in remappedBones)
            {
                if (remapping.inputName.Trim() == inputName_) return remapping.outputName;
            }
            inputName_ = inputName.ToLower().Trim();
            foreach (Remapping remapping in remappedBones)
            {
                if (remapping.inputName.ToLower().Trim() == inputName_) return remapping.outputName;
            }

            return inputName;
        }

    }
}

#endif