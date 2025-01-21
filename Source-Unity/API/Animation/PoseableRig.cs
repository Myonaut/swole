#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Unity.Mathematics;

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

        protected void OnValidate()
        {
            if (boneGroups == null || boneGroups.Length == 0) boneGroups = new BoneGroup[] { BoneGroup.Default };
        }

        [Serializable]
        public struct BoneGroup
        {
            public string name;
            public Color color;
            public string keyword;
            public bool HasKeyword => string.IsNullOrWhiteSpace(keyword);
            public bool BoneHasKeyword(string boneName)
            {
                if (string.IsNullOrWhiteSpace(boneName) || !HasKeyword) return false;

                if (boneName.IndexOf(keyword) >= 0) return true;
                if (boneName.AsID().IndexOf(keyword.AsID()) >= 0) return true;

                return false;
            }
            public bool togglable;

            public static readonly BoneGroup Default = new BoneGroup() { name = "bones", color = new Color(0, 1, 0, 1), togglable = true };
        }

        public BoneGroup[] boneGroups;
        public int BoneGroupCount => boneGroups == null ? 1 : (boneGroups.Length < 1 ? 1 : boneGroups.Length);
        public BoneGroup GetBoneGroup(int index)
        {
            if (boneGroups == null || boneGroups.Length <= 0)
            {
                boneGroups = new BoneGroup[] { BoneGroup.Default };
            }

            if (index < 0 || index >= boneGroups.Length) return boneGroups[0];
            return boneGroups[index];
        }

        [Serializable]
        public struct BoneInfo
        {
            public BoneID id;
            public int boneGroup;
            public bool defaultChildrenToSameGroup;
            public bool dontDrawConnection;
            public bool dontDrawChildConnections;
            public float scale;
            public float childScale;

            public float3 offset; 

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

        [Tooltip("Bones in this array aren't recognized by an avatar automatically. Useful for grouping ik bones.")]
        public BoneInfo[] auxiliaryRig; 

        /// <summary>
        /// Are the rig's bones defined explicity?
        /// </summary>
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
            bool Internal(out BoneInfo info)
            {
                info = BoneInfo.GetDefault(boneName);
                if (string.IsNullOrWhiteSpace(boneName)) return false;

                bool excluded = ShouldExcludeBone(boneName);

                if (TryGetAuxBoneInfo(boneName, out info)) return !excluded;

                string boneNameId = boneName.AsID();
                if (additiveRig != null)
                {
                    foreach (var addBone in additiveRig)
                    {
                        if (!addBone.IsBone(boneName, boneNameId)) continue;

                        info = addBone;
                        return !excluded;
                    }
                }

                if (IsExplicit)
                {
                    foreach (var bone in fullRig)
                    {
                        if (!bone.IsBone(boneName, boneNameId)) continue;

                        info = bone;
                        return !excluded;
                    }
                    return false;
                }

                return !excluded;
            }

            bool excluded = Internal(out info);
            if (boneGroups != null) // Check for any bone group keyword and override index in info if found
            {
                for (int a = 0; a < boneGroups.Length; a++)
                {
                    var group = boneGroups[a]; 
                    if (group.BoneHasKeyword(boneName))
                    {
                        //info.boneGroup = a;
                        break;
                    }
                }
            }
            return excluded;
        }

        protected bool TryGetAuxBoneInfo(string boneName, out BoneInfo info)
        {
            info = BoneInfo.GetDefault(boneName);
            if (auxiliaryRig != null) 
            { 
                string boneNameId = boneName.AsID();
                foreach (var info_ in auxiliaryRig) if (info_.IsBone(boneName, boneNameId)) 
                    { 
                        info = info_;
                        return true;
                    }
            }

            return false;
        }

    }

}

#endif