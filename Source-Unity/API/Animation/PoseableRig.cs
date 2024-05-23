#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Swole.API.Unity.Animation
{

    /// <summary>
    /// Stores the names of bones in an avatar that can be posed by a user once it's loaded into the Animation Editor.
    /// </summary>
    [CreateAssetMenu(fileName = "PoseableRig", menuName = "Rigs/PoseableRig", order = 2)]
    public class PoseableRig : ScriptableObject
    {
        public static PoseableRig Create(string path = null, string fileName = null, bool incrementIfExists = false)
        {

            PoseableRig asset = ScriptableObject.CreateInstance<PoseableRig>();

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
        public struct BoneInfo
        {
            public BoneID id;
            public bool dontDrawConnection;
            public float scale;
            public float childScale;

            [NonSerialized]
            public bool isDefault;
             
            public string Name => id.name;

            public bool IsBone(string boneName) => id.IsBone(boneName);
            public bool IsBone(string boneName, string boneNameId) => id.IsBone(boneName, boneNameId);

            public static BoneInfo GetDefault(string boneName) => new BoneInfo() { id = new BoneID() { name = boneName }, dontDrawConnection = false, scale = 1, childScale = 1, isDefault = true };
        }
        [Serializable]
        public struct BoneID
        {
            public string name;
            public string[] aliases;

            public bool IsBone(string boneName) => IsBone(boneName, boneName == null ? null : boneName.AsID());
            public bool IsBone(string boneName, string boneNameId)
            {
                if (!string.IsNullOrWhiteSpace(name) && (name == boneName || name.AsID() == boneNameId)) return true;

                if (aliases != null)
                {
                    foreach (var alias in aliases)
                    {
                        if (!string.IsNullOrWhiteSpace(alias) && (alias == boneName || alias.AsID() == boneNameId)) return true;
                    }
                }

                return false;
            }
        }

        [Tooltip("Determines exactly what bones are in the rig. If left empty then all existing bones are considered to be part of the rig, unless otherwise specified.")]
        public BoneInfo[] fullRig;
        public BoneInfo[] additiveRig;
        public BoneID[] subtractiveRig;

        public bool IsExplicit => fullRig != null && fullRig.Length > 0;

        public bool ShouldExcludeBone(string boneName)
        {
            if (subtractiveRig == null || string.IsNullOrWhiteSpace(boneName)) return false;

            string boneNameId = boneName.AsID();
            foreach (var bone in subtractiveRig) if (bone.IsBone(boneName, boneNameId)) return true;

            return false;
        }
        public bool TryGetBoneInfo(string boneName, out BoneInfo info)
        {
            info = BoneInfo.GetDefault(boneName);
            if (string.IsNullOrWhiteSpace(boneName)) return false;

            bool exluded = ShouldExcludeBone(boneName);
             
            string boneNameId = boneName.AsID();
            if (additiveRig != null)
            {
                foreach(var addBone in additiveRig)
                {
                    if (!addBone.IsBone(boneName, boneNameId)) continue;

                    info = addBone;
                    return !exluded;
                }
            }

            if (IsExplicit)
            {
                foreach (var bone in fullRig)
                {
                    if (!bone.IsBone(boneName, boneNameId)) continue;

                    info = bone;
                    return !exluded;
                }
                return false;
            }

            return !exluded;
        }

    }

}

#endif