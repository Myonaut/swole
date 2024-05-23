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
    /// Used to identify bone transforms in a transform hierarchy.
    /// </summary>
    [CreateAssetMenu(fileName = "CustomAvatar", menuName = "Rigs/CustomAvatar", order = 0)]
    public class CustomAvatar : ScriptableObject
    {

        public static CustomAvatar Create(string path = null, string fileName = null, bool incrementIfExists = false)
        {

            CustomAvatar asset = ScriptableObject.CreateInstance<CustomAvatar>();

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

        public string rigContainer;

        public string[] bones;

        public RigRemapping[] remappings;

        public string Remap(string inputBoneName)
        {
            if (remappings == null || string.IsNullOrEmpty(inputBoneName)) return inputBoneName;

            foreach(RigRemapping remapping in remappings)
            {
                if (remapping == null) continue;
                string outputBoneName = remapping.Remap(inputBoneName);
                if (inputBoneName != outputBoneName) return outputBoneName;
            }

            return inputBoneName;
        }

        public PoseableRig poseable;

        public bool IsPoseableBone(string boneName)
        {
            boneName = Remap(boneName);
            if (poseable == null || !poseable.IsExplicit) 
            {
                string boneNameId = boneName.AsID();
                if (poseable != null)
                {
                    if (poseable.ShouldExcludeBone(boneName)) return false;
                    if (poseable.additiveRig != null)
                    {
                        foreach (var bone in poseable.additiveRig) if (bone.IsBone(boneName, boneNameId)) return true;
                    }
                } 
                if (bones != null)
                {
                    foreach (var poseableBoneName in bones) if (poseableBoneName == boneName) return true;
                    foreach (var poseableBoneName in bones) if (poseableBoneName.AsID() == boneNameId) return true;
                }
                return false;
            } 
            else
            {
                if (poseable.TryGetBoneInfo(boneName, out _)) return true;
            } 

            return false;
        }

        public bool TryGetBoneInfo(string boneName, out PoseableRig.BoneInfo boneInfo)
        {
            boneInfo = PoseableRig.BoneInfo.GetDefault(boneName);

            if (poseable != null) 
            {
                if (poseable.TryGetBoneInfo(boneName, out boneInfo))
                {
                    return true;
                }
                else if (poseable.IsExplicit) return false;
            }

            return IsPoseableBone(boneName);
        }

        public List<Transform> FindBones(Transform rootTransform, List<Transform> outputList = null)
        {
            if (outputList == null) outputList = new List<Transform>();

            if (poseable != null)
            {
                if (poseable.IsExplicit)
                {
                    foreach(var bone in poseable.fullRig)
                    {
                        if (poseable.ShouldExcludeBone(bone.Name)) continue;
                        Transform boneTransform = rootTransform.FindDeepChildLiberal(bone.Name);
                        if (boneTransform != null) outputList.Add(boneTransform);
                    }

                    if (poseable.additiveRig != null)
                    {
                        foreach (var bone in poseable.additiveRig)
                        {
                            if (poseable.ShouldExcludeBone(bone.Name)) continue;
                            Transform boneTransform = rootTransform.FindDeepChildLiberal(bone.Name);
                            if (boneTransform != null) outputList.Add(boneTransform);
                        }
                    }

                    return outputList;
                }
                else
                {
                    if (bones != null)
                    {
                        foreach (var bone in bones)
                        {
                            if (poseable.ShouldExcludeBone(bone)) continue;
                            Transform boneTransform = rootTransform.FindDeepChildLiberal(bone);
                            if (boneTransform != null) outputList.Add(boneTransform);
                        }
                    }

                    if (poseable.additiveRig != null)
                    {
                        foreach (var bone in poseable.additiveRig)
                        {
                            if (poseable.ShouldExcludeBone(bone.Name)) continue;
                            Transform boneTransform = rootTransform.FindDeepChildLiberal(bone.Name);
                            if (boneTransform != null && !outputList.Contains(boneTransform)) outputList.Add(boneTransform);
                        }
                    }
                }
            } 
            else
            {
                if (bones != null)
                {
                    foreach (var bone in bones)
                    {
                        Transform boneTransform = rootTransform.FindDeepChildLiberal(bone);
                        if (boneTransform != null) outputList.Add(boneTransform);
                    }
                }
            }
           

            return outputList;
        }

    }

}

#endif
