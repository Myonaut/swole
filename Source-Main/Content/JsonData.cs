using System;
using System.Collections.Generic;

namespace Swole
{

    [Serializable]
    public struct JsonData : IContent, ISwoleSerialization<JsonData, JsonData.Serialized>
    {

        public Type AssetType => GetType();
        public object Asset => this;

        public bool IsInternalAsset { get => false; set { } }

        public bool IsValid => true;
        public void Dispose() { }
        public void Delete() => Dispose();

        #region Serialization

        public string AsJSON(bool prettyPrint = false) => AsSerializableStruct().AsJSON(prettyPrint);

        [Serializable]
        public struct Serialized : ISerializableContainer<JsonData, JsonData.Serialized>
        {

            public ContentInfo contentInfo;
            public string SerializedName => contentInfo.name;
            public bool hasMetadata;
            public string data; 

            public JsonData AsOriginalType(PackageInfo packageInfo = default) => new JsonData(this, packageInfo);
            public string AsJSON(bool prettyPrint = false) => swole.Engine.ToJson(this, prettyPrint);

            public object AsNonserializableObject(PackageInfo packageInfo = default) => AsOriginalType(packageInfo);
        }

        public JsonData.Serialized AsSerializableStruct() => new JsonData.Serialized() { contentInfo = contentInfo, hasMetadata = hasMetadata, data = json };
        public object AsSerializableObject() => AsSerializableStruct();

        public JsonData(JsonData.Serialized serializable, PackageInfo packageInfo = default)
        {
            originPath = relativePath = string.Empty;
            this.packageInfo = packageInfo;

            this.contentInfo = serializable.contentInfo;
            this.hasMetadata = serializable.hasMetadata; 
            this.json = serializable.data;

        }

        #endregion

        public string originPath;
        public string OriginPath => originPath;
        public IContent SetOriginPath(string path)
        {
            var content = this;
            content.originPath = path;
            return content;
        }
        public string relativePath;
        public string RelativePath => relativePath;
        public IContent SetRelativePath(string path)
        {
            var content = this;
            content.relativePath = path;
            return content;
        }

        public List<PackageIdentifier> ExtractPackageDependencies(List<PackageIdentifier> dependencies = null)
        {

            if (dependencies == null) dependencies = new List<PackageIdentifier>();

            return dependencies;

        }

        public override string ToString()
        {

            return $"{Name}{(string.IsNullOrEmpty(Author) ? "" : " created by" + Author)}{(this.HasPackage() ? " - imported from '" + packageInfo.GetIdentityString() + "'" : "")}";

        }

        public string Name => contentInfo.name;
        public string SerializedName => Name;

        public string Author => contentInfo.author;
        public string CreationDate => contentInfo.creationDate; 
        public string LastEditDate => contentInfo.lastEditDate;
        public string Description => contentInfo.description;

        public PackageInfo packageInfo;
        public PackageInfo PackageInfo => packageInfo;

        public ContentInfo contentInfo;
        public ContentInfo ContentInfo => contentInfo;

        public bool hasMetadata;

        public string json;

        public JsonData(string name, string author, DateTime creationDate, DateTime lastEditDate, string description, string json, PackageInfo packageInfo = default) : this(new ContentInfo() { name = name, author = author, creationDate = creationDate.ToString(IContent.dateFormat), lastEditDate = lastEditDate.ToString(IContent.dateFormat), description = description }, json, true, packageInfo) { }

        public JsonData(string name, string author, string creationDate, string lastEditDate, string description, string json, PackageInfo packageInfo = default) : this(new ContentInfo() { name = name, author = author, creationDate = creationDate, lastEditDate = lastEditDate, description = description }, json, true, packageInfo) { }

        public JsonData(ContentInfo contentInfo, string json, bool hasMetadata, PackageInfo packageInfo = default)
        {
            originPath = relativePath = string.Empty;
            this.contentInfo = contentInfo;
            this.hasMetadata = hasMetadata;
            this.json = json;
            this.packageInfo = packageInfo;
        }
        public JsonData(string json, PackageInfo packageInfo = default) : this(default, json, false, packageInfo) { }

        public IContent CreateCopyAndReplaceContentInfo(ContentInfo info) => new JsonData(info, json, true, packageInfo);

        public T AsType<T>()
        {
            if (string.IsNullOrWhiteSpace(json)) return default;
            return swole.FromJson<T>(json);
        }

    }

}
