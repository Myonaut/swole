#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Swole.Script;

namespace Swole.API.Unity
{
    [CreateAssetMenu(fileName = "newMeshAsset", menuName = "Swole/Assets/MeshAsset", order = 1)]
    public class MeshAsset : ScriptableObject, IMeshAsset<MeshAsset, MeshAsset.Serialized>
    {

#if UNITY_EDITOR
        public void OnValidate()
        {
            isInternalAsset = true; 
        }
#endif

        public System.Type AssetType => typeof(Mesh);
        public object Asset => mesh;
        public static implicit operator Mesh(MeshAsset asset) => asset.Mesh;

        public static MeshAsset NewInstance() => ScriptableObject.CreateInstance<MeshAsset>();
        public static MeshAsset NewInstance(ContentInfo contentInfo, Mesh mesh, PackageInfo packageInfo = default)
        {
            var inst = NewInstance();

            inst.contentInfo = contentInfo;
            inst.packageInfo = packageInfo;

            return inst;
        }
        public static MeshAsset NewInstance(string name, string author, DateTime creationDate, DateTime lastEditDate, string description, Mesh mesh, PackageInfo packageInfo = default) =>
            NewInstance(new ContentInfo() { name = name, author = author, creationDate = creationDate.ToString(IContent.dateFormat), lastEditDate = lastEditDate.ToString(IContent.dateFormat), description = description }, mesh, packageInfo);

        public static MeshAsset NewInstance(string name, string author, string creationDate, string lastEditDate, string description, Mesh mesh, PackageInfo packageInfo = default) =>
            NewInstance(new ContentInfo() { name = name, author = author, creationDate = creationDate, lastEditDate = lastEditDate, description = description }, mesh, packageInfo);

        [SerializeField]
        protected Mesh mesh;
        public Mesh Mesh => mesh;

        #region IEngineObject

        public object Instance => Mesh;

        public int InstanceID => mesh == null ? 0 : mesh.GetInstanceID();

        public bool IsDestroyed => mesh == null;

        public bool HasEventHandler => false;

        public IRuntimeEventHandler EventHandler => default;

        public void AdminDestroy(float timeDelay = 0) => swole.Engine.Object_AdminDestroy(this, timeDelay);

        public void Destroy(float timeDelay = 0) => swole.Engine.Object_Destroy(this, timeDelay);

        #endregion

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
        public bool HasCollectionID => !string.IsNullOrWhiteSpace(CollectionID);

        public bool IsValid => mesh != null;
        public void Dispose()
        {
            if (!IsInternalAsset && mesh != null)
            {
                GameObject.Destroy(mesh);
            }

            DisposeSelf();
        }
        public void DisposeSelf()
        {
            mesh = null;
            UnityEngine.Object.Destroy(this);
        }
        public void Delete()
        {
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
            Mesh m = null;
            if (mesh != null)
            {
                m = UnityEngine.Mesh.Instantiate(mesh);
                m.name = mesh.name;
            }
            var content = NewInstance(info, m, packageInfo);

            return content;
        }
        public IContent CreateShallowCopyAndReplaceContentInfo(ContentInfo info)
        {
            var content = NewInstance(info, mesh, packageInfo);

            return content;
        }

        public List<PackageIdentifier> ExtractPackageDependencies(List<PackageIdentifier> dependencies = null)
        {
            if (dependencies == null) dependencies = new List<PackageIdentifier>();
            return dependencies;
        }

        public bool IsIdenticalAsset(ISwoleAsset otherAsset) => ReferenceEquals(this, otherAsset) || (otherAsset is IMeshAsset asset && ReferenceEquals(Instance, asset.Instance));

        #endregion

        #region Serialization

        public string AsJSON(bool prettyPrint = false) => AsSerializableStruct().AsJSON(prettyPrint);

        [Serializable]
        public struct Serialized : ISerializableContainer<MeshAsset, MeshAsset.Serialized>
        {

            public ContentInfo contentInfo;
            public string SerializedName => contentInfo.name;



            public MeshAsset AsOriginalType(PackageInfo packageInfo = default) => NewInstance(this, packageInfo);
            public string AsJSON(bool prettyPrint = false) => swole.Engine.ToJson(this, prettyPrint);

            public object AsNonserializableObject(PackageInfo packageInfo = default) => AsOriginalType(packageInfo);

        }

        public static implicit operator Serialized(MeshAsset asset)
        {
            Serialized s = new Serialized();

            s.contentInfo = asset.contentInfo;



            return s;
        }

        public MeshAsset.Serialized AsSerializableStruct() => this;
        public object AsSerializableObject() => AsSerializableStruct();

        public static MeshAsset NewInstance(MeshAsset.Serialized serializable, PackageInfo packageInfo = default)
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