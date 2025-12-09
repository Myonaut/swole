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
    [CreateAssetMenu(fileName = "CustomAvatar", menuName = "Swole/Rigs/CustomAvatar", order = 0)]
    public class CustomAvatar : ScriptableObject, IAvatarAsset<CustomAvatar, CustomAvatar.Serialized>
    {

#if UNITY_EDITOR
        public void OnValidate()
        {
            isInternalAsset = true;
        }
#endif

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

        public static CustomAvatar NewInstance() => ScriptableObject.CreateInstance<CustomAvatar>();
        public static CustomAvatar NewInstance(ContentInfo contentInfo, string rigContainer, bool containerIsRoot, string note, string[] bones, IkBone[] ikBones, RigRemapping[] remappings, string poseablePath, PackageInfo packageInfo = default)
        {
            var inst = NewInstance();

            inst.contentInfo = contentInfo;
            inst.packageInfo = packageInfo;

            inst.rigContainer = rigContainer;
            inst.containerIsRoot = containerIsRoot;
            inst.note = note;
            inst.bones = bones;
            inst.ikBones = ikBones;
            inst.remappings = remappings;
            inst.poseablePath = poseablePath;

            return inst;
        }
        public static CustomAvatar NewInstance(string name, string author, DateTime creationDate, DateTime lastEditDate, string description, string rigContainer, bool containerIsRoot, string note, string[] bones, IkBone[] ikBones, RigRemapping[] remappings, string poseablePath, PackageInfo packageInfo = default) =>
            NewInstance(new ContentInfo() { name = name, author = author, creationDate = creationDate.ToString(IContent.dateFormat), lastEditDate = lastEditDate.ToString(IContent.dateFormat), description = description }, rigContainer, containerIsRoot, note, bones, ikBones, remappings, poseablePath, packageInfo);

        public static CustomAvatar NewInstance(string name, string author, string creationDate, string lastEditDate, string description, string rigContainer, bool containerIsRoot, string note, string[] bones, IkBone[] ikBones, RigRemapping[] remappings, string poseablePath, PackageInfo packageInfo = default) =>
            NewInstance(new ContentInfo() { name = name, author = author, creationDate = creationDate, lastEditDate = lastEditDate, description = description }, rigContainer, containerIsRoot, note, bones, ikBones, remappings, poseablePath, packageInfo);

        public System.Type AssetType => typeof(CustomAvatar);
        public object Asset => this;

        public string rigContainer;
        public bool containerIsRoot;

        [TextArea]
        public string note;

        public int rootBoneIndex;
        public string RootBone => rootBoneIndex < 0 || containerIsRoot ? rigContainer : bones[rootBoneIndex];

        public int skinnedBonesCount;
        public int SkinnedBonesCount => skinnedBonesCount <= 0f ? (bones == null ? 0 : bones.Length) : skinnedBonesCount;
        public string[] bones;

        public int GetBoneIndex(string boneName)
        {
            if (bones != null)
            {
                for(int a = 0; a < bones.Length; a++)
                {
                    if (bones[a] == boneName) return a;
                }
            }

            return -1;
        }

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

        [SerializeField]
        private PoseableRig poseable;
        [NonSerialized]
        public string poseablePath;
        public PoseableRig Poseable
        {
            get
            {
                if (poseable == null)
                {
                    if (!string.IsNullOrWhiteSpace(poseablePath))
                    {
                        ResourceLib.ResolveAssetIdString(poseablePath, out string assetName, out string collectionName, out bool collectionIsPackage);
                        //poseable = ResourceLib.FindPoseRig(assetName, collectionName, collectionIsPackage); // TODO: Implement pose rigs as loadable assets
                    }
                }

                return poseable;
            }
        }

        public bool IsPoseableBone(string boneName)
        {
            boneName = Remap(boneName);

            if (IsIkBone(boneName)) return true;

            if (Poseable == null || !poseable.IsExplicit) 
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
            
            if (Poseable != null) 
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

        public int BoneGroupCount => Poseable == null ? 1 : poseable.BoneGroupCount;
        public PoseableRig.BoneGroup GetBoneGroup(int index)
        {
            if (Poseable != null) return poseable.GetBoneGroup(index);
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

            if (Poseable != null)
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

        #region IContent

        [SerializeField]
        protected bool isInternalAsset;
        public bool IsInternalAsset
        {
            get => isInternalAsset;
            set => isInternalAsset = value;
        }

        [SerializeField]
        protected string collectionId;
        public string CollectionID
        {
            get => collectionId;
            set => collectionId = value;
        }
        public bool HasCollectionID => !string.IsNullOrWhiteSpace(collectionId);

        public bool IsValid => bones != null && bones.Length > 0;
        public void Dispose()
        {
            if (!IsInternalAsset)
            {
                bones = null;
            }
        }
        public void DisposeSelf()
        {
            if (!IsInternalAsset)
            {
                bones = null;
            }
        }
        public void Delete()
        {
            if (ContentManager.TryFindLocalPackage(packageInfo.GetIdentity(), out var lpkg, false, false) && lpkg.workingDirectory.Exists)
            {
            }

            Dispose();
        }

        [NonSerialized]
        public string originPath;
        public string OriginPath => originPath;
        public IContent SetOriginPath(string path)
        {
            var content = this;
            content.originPath = path;
            return content;
        }

        [NonSerialized]
        public string relativePath;
        public string RelativePath => relativePath;
        public IContent SetRelativePath(string path)
        {
            var content = this;
            content.relativePath = path;
            return content;
        }

        public string Name => contentInfo.name;
        public string Author => contentInfo.author;
        public string CreationDate => contentInfo.creationDate;
        public string LastEditDate => contentInfo.lastEditDate;
        public string Description => contentInfo.description;

        public PackageInfo packageInfo;
        public PackageInfo PackageInfo => packageInfo;

        public ContentInfo contentInfo;
        public ContentInfo ContentInfo => contentInfo;
        public string SerializedName => contentInfo.name;

        public IContent CreateCopyAndReplaceContentInfo(ContentInfo info)
        {
            var content = NewInstance(info, rigContainer, containerIsRoot, note, bones == null ? null : ((string[])bones.Clone()), ikBones == null ? null : ((IkBone[])ikBones.Clone()), remappings == null ? null : ((RigRemapping[])remappings.Clone()), poseablePath, packageInfo);
            return content;
        }
        public IContent CreateShallowCopyAndReplaceContentInfo(ContentInfo info)
        {
            var content = NewInstance(info, rigContainer, containerIsRoot, note, bones, ikBones, remappings, poseablePath, packageInfo);
            return content;
        }

        public List<PackageIdentifier> ExtractPackageDependencies(List<PackageIdentifier> dependencies = null)
        {
            if (dependencies == null) dependencies = new List<PackageIdentifier>();
            return dependencies;
        }

        public bool IsIdenticalAsset(ISwoleAsset otherAsset) => ReferenceEquals(this, otherAsset);

        #endregion

        #region Serialization

        public string AsJSON(bool prettyPrint = false) => AsSerializableStruct().AsJSON(prettyPrint);

        [Serializable]
        public struct Serialized : ISerializableContainer<CustomAvatar, CustomAvatar.Serialized>
        {

            public ContentInfo contentInfo;

            public string rigContainer;
            public bool containerIsRoot;
            public string note;
            public string[] bones;
            public IkBone[] ikBones;
            public RigRemapping.Serialized[] remappings;
            public string poseablePath;

            public string SerializedName => contentInfo.name;

            public CustomAvatar AsOriginalType(PackageInfo packageInfo = default) => NewInstance(this, packageInfo);
            public string AsJSON(bool prettyPrint = false) => swole.Engine.ToJson(this, prettyPrint);

            public object AsNonserializableObject(PackageInfo packageInfo = default) => AsOriginalType(packageInfo);

        }

        public static implicit operator Serialized(CustomAvatar asset)
        {
            Serialized s = new Serialized();

            s.contentInfo = asset.contentInfo;

            s.containerIsRoot = asset.containerIsRoot;
            s.note = asset.note;
            s.bones = asset.bones;
            s.ikBones = asset.ikBones;
            s.poseablePath = asset.poseablePath;

            if (asset.remappings != null)
            {
                s.remappings = new RigRemapping.Serialized[asset.remappings.Length];
                for (int a = 0; a < asset.remappings.Length; a++) s.remappings[a] = asset.remappings[a].AsSerializableStruct();
            }

            return s;
        }

        public CustomAvatar.Serialized AsSerializableStruct() => this;
        public object AsSerializableObject() => AsSerializableStruct();

        public static CustomAvatar NewInstance(CustomAvatar.Serialized serializable, PackageInfo packageInfo = default)
        {
            var inst = NewInstance();

            inst.packageInfo = packageInfo;
            inst.contentInfo = serializable.contentInfo;

            inst.containerIsRoot = serializable.containerIsRoot;
            inst.note = serializable.note;
            inst.bones = serializable.bones;
            inst.ikBones = serializable.ikBones;
            inst.poseablePath = serializable.poseablePath;

            if (serializable.remappings != null)
            {
                inst.remappings = new RigRemapping[serializable.remappings.Length];
                for (int a = 0; a < serializable.remappings.Length; a++) inst.remappings[a] = serializable.remappings[a].AsOriginalType(packageInfo);
            }

            return inst;
        }

        #endregion

    }

}

#endif
