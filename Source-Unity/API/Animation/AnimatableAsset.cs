#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole.API.Unity
{

    [CreateAssetMenu(fileName = "AnimatableAsset", menuName = "Swole/Animation/AnimatableAsset", order = 1)]
    public class AnimatableAsset : ScriptableObject, IActorAsset
    {

#if UNITY_EDITOR
        public void OnValidate()
        {
            isInternalAsset = true; 
        }
#endif

        public static AnimatableAsset NewInstance() => ScriptableObject.CreateInstance<AnimatableAsset>();
        public static AnimatableAsset NewInstance(ContentInfo contentInfo, GameObject prefab, Sprite icon, PackageInfo packageInfo = default)
        {
            var inst = NewInstance();

            inst.contentInfo = contentInfo;
            inst.packageInfo = packageInfo;

            inst.prefab = prefab;
            inst.icon = icon;

            return inst;
        }
        public static AnimatableAsset NewInstance(string name, string author, DateTime creationDate, DateTime lastEditDate, string description, GameObject prefab, Sprite icon, PackageInfo packageInfo = default) =>
            NewInstance(new ContentInfo() { name = name, author = author, creationDate = creationDate.ToString(IContent.dateFormat), lastEditDate = lastEditDate.ToString(IContent.dateFormat), description = description }, prefab, icon, packageInfo);

        public static AnimatableAsset NewInstance(string name, string author, string creationDate, string lastEditDate, string description, GameObject prefab, Sprite icon, PackageInfo packageInfo = default) =>
            NewInstance(new ContentInfo() { name = name, author = author, creationDate = creationDate, lastEditDate = lastEditDate, description = description }, prefab, icon, packageInfo);


        public System.Type AssetType => typeof(AnimatableAsset);
        public object Asset => this;

        [Serializable]
        public enum ObjectType
        {
            Humanoid, Mechanical, GUI
        }

        public ObjectType type;

        [SerializeField]
        protected GameObject prefab;
        public GameObject Prefab
        {
            get
            {
                return prefab;
            }
        }

        [HideInInspector]
        public string iconId;

        [SerializeField]
        protected Sprite icon;
        public Sprite Icon
        {
            get
            {
                if (icon == null && !string.IsNullOrWhiteSpace(iconId))
                {
                    ResourceLib.ResolveAssetIdString(iconId, out string iconName, out string collectionName, out bool isInPackage);
                    var tex = ResourceLib.FindImage(iconName, collectionName, isInPackage, false, PackageInfo);
                }

                return icon;
            }
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

        public bool IsValid => prefab != null;
        public void Dispose()
        {
            if (!IsInternalAsset)
            {
                if (prefab != null) GameObject.Destroy(prefab);
            }

            prefab = null;
        }
        public void DisposeSelf()
        {
            prefab = null;
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
            GameObject pb = null;

            if (prefab != null)
            {
                pb = prefab == null ? null : GameObject.Instantiate(prefab);
                pb.name = prefab.name;
            }
            var content = NewInstance(info, pb, icon, packageInfo);
            content.iconId = iconId;

            return content;
        }
        public IContent CreateShallowCopyAndReplaceContentInfo(ContentInfo info)
        {
            var content = NewInstance(info, prefab, icon, packageInfo);
            content.iconId = iconId;

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
        public struct Serialized : ISerializableContainer<AnimatableAsset, AnimatableAsset.Serialized>
        {

            public ContentInfo contentInfo;

            public string SerializedName => contentInfo.name;

            public AnimatableAsset AsOriginalType(PackageInfo packageInfo = default) => NewInstance(this, packageInfo);
            public string AsJSON(bool prettyPrint = false) => swole.Engine.ToJson(this, prettyPrint);

            public object AsNonserializableObject(PackageInfo packageInfo = default) => AsOriginalType(packageInfo);

        }

        public static implicit operator Serialized(AnimatableAsset asset)
        {
            Serialized s = new Serialized();

            s.contentInfo = asset.contentInfo;

            return s;
        }

        public AnimatableAsset.Serialized AsSerializableStruct() => this;
        public object AsSerializableObject() => AsSerializableStruct();

        public static AnimatableAsset NewInstance(AnimatableAsset.Serialized serializable, PackageInfo packageInfo = default)
        {
            var inst = NewInstance();

            inst.packageInfo = packageInfo;
            inst.contentInfo = serializable.contentInfo;

            return inst;
        }

        #endregion

    }

}

#endif