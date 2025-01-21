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
        public bool containerIsRoot;

        [TextArea]
        public string note;

        public string[] bones;

        [Serializable]
        public enum IKBoneType
        {
            Target, BendGoal
        }

        [Serializable]
        public struct IkBone
        {
            public IKBoneType type;
            public string name;
            public int parentIndex;
            public string fkParent;
            public bool usePositionOffsetFK;
            public float3 fkOffsetPosition;
            public bool useRotationOffsetFK;
            public float3 fkOffsetEulerRotation;
        }

        public IkBone[] ikBones;
        public bool IsIkBone(string boneName) => IsIkBone(boneName, out _);
        public bool IsIkBone(string boneName, out IkBone ikBone)
        {
            ikBone = default;
            if (ikBones != null)
            {
                foreach (var ikBone_ in ikBones) if (ikBone_.name == boneName)
                    {
                        ikBone = ikBone_;
                        return true;
                    }

                boneName = boneName.AsID();
                foreach(var ikBone_ in ikBones) if (ikBone_.name.AsID() == boneName)
                    {
                        ikBone = ikBone_;
                        return true;
                    }
            }
            return false;
        }
        public bool IsIkBoneFkEquivalent(string boneName) => IsIkBoneFkEquivalent(boneName, out _);
        public bool IsIkBoneFkEquivalent(string boneName, out IkBone dependentIkBone)
        {
            dependentIkBone = default;
            if (ikBones != null)
            {
                foreach (var ikBone_ in ikBones) if (ikBone_.fkParent == boneName)
                    {
                        dependentIkBone = ikBone_;
                        return true;
                    }

                boneName = boneName.AsID();
                foreach (var ikBone_ in ikBones) if (ikBone_.fkParent.AsID() == boneName)
                    {
                        dependentIkBone = ikBone_;
                        return true;
                    }
            }
            return false;
        }

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

            if (IsIkBone(boneName)) return true;

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
            string boneName_ = boneName;
            boneName = Remap(boneName);

            boneInfo = PoseableRig.BoneInfo.GetDefault(boneName);
            
            if (poseable != null) 
            {
                if (poseable.TryGetBoneInfo(boneName, out boneInfo))
                {
                    return true;
                }
                else if (poseable.IsExplicit && !IsIkBone(boneName)) return false;
            }
            
            return IsPoseableBone(boneName_);
        }

        public PoseableRig.BoneInfo GetNonDefaultParentBoneInfo(Transform bone) => GetNonDefaultParentBoneInfo(bone, out _);
        public PoseableRig.BoneInfo GetNonDefaultParentBoneInfo(Transform bone, out Transform nonDefaultParentBone)
        {
            var nonDefaultParentBoneInfo = PoseableRig.BoneInfo.GetDefault(string.Empty);
            nonDefaultParentBone = null;
            if (bone == null) return nonDefaultParentBoneInfo;

            nonDefaultParentBone = bone.parent;
            while (nonDefaultParentBone != null && nonDefaultParentBoneInfo.isDefault)
            {
                TryGetBoneInfo(nonDefaultParentBone.name, out nonDefaultParentBoneInfo);
                nonDefaultParentBone = nonDefaultParentBone.parent;
            }

            return nonDefaultParentBoneInfo;
        }
        public bool TryGetNonDefaultParentBoneInfo(Transform bone, out PoseableRig.BoneInfo nonDefaultParentBoneInfo) => TryGetNonDefaultParentBoneInfo(bone, out nonDefaultParentBoneInfo, out _);
        public bool TryGetNonDefaultParentBoneInfo(Transform bone, out PoseableRig.BoneInfo nonDefaultParentBoneInfo, out Transform nonDefaultParentBone)
        {
            nonDefaultParentBoneInfo = GetNonDefaultParentBoneInfo(bone, out nonDefaultParentBone);
            return !nonDefaultParentBoneInfo.isDefault; 
        }

        public int BoneGroupCount => poseable == null ? 1 : poseable.BoneGroupCount;
        public PoseableRig.BoneGroup GetBoneGroup(int index)
        {
            if (poseable != null) return poseable.GetBoneGroup(index);
            return PoseableRig.BoneGroup.Default;
        }
        public PoseableRig.BoneGroup GetBoneGroup(Transform bone)
        {
            if (bone != null && TryGetBoneInfo(bone.name, out var boneInfo))
            {
                if (boneInfo.isDefault)
                {
                    var nonDefaultParentBoneInfo = GetNonDefaultParentBoneInfo(bone);
                    if (nonDefaultParentBoneInfo.defaultChildrenToSameGroup) return GetBoneGroup(nonDefaultParentBoneInfo.boneGroup); 
                }

                return GetBoneGroup(boneInfo.boneGroup);
            }

            return GetBoneGroup(0);
        }

        public Transform FindPoseableBone(Transform rootTransform, PoseableRig.BoneInfo boneInfo)
        {
            Transform boneTransform = rootTransform.FindDeepChildLiberal(boneInfo.Name); 
            if (boneTransform == null && boneInfo.id.aliases != null)
            {
                for (int a = 0; a < boneInfo.id.aliases.Length; a++)
                {
                    boneTransform = rootTransform.FindDeepChildLiberal(boneInfo.id.aliases[a]);
                    if (boneTransform != null) break;
                }
            }

            return boneTransform;
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
                        Transform boneTransform = FindPoseableBone(rootTransform, bone); 
                        if (boneTransform != null) outputList.Add(boneTransform);
                    }

                    if (poseable.additiveRig != null)
                    {
                        foreach (var bone in poseable.additiveRig)
                        {
                            if (poseable.ShouldExcludeBone(bone.Name)) continue;
                            Transform boneTransform = FindPoseableBone(rootTransform, bone);
                            if (boneTransform != null) outputList.Add(boneTransform);
                        }
                    }
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
                            Transform boneTransform = FindPoseableBone(rootTransform, bone);
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

            if (ikBones != null)
            {
                foreach(var ikBone in ikBones)
                {
                    Transform boneTransform = rootTransform.FindDeepChildLiberal(ikBone.name);
                    if (boneTransform != null) outputList.Add(boneTransform);
                }
            }

            return outputList;
        }

    }

}

#endif
