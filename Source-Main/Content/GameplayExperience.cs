using System;
using System.Collections.Generic;

namespace Swole
{
    public class GameplayExperience : IContent, ISwoleSerialization<GameplayExperience, GameplayExperience.Serialized>
    {

        #region Serialization

        public string AsJSON(bool prettyPrint = false) => AsSerializableStruct().AsJSON(prettyPrint); 

        [Serializable]
        public struct Serialized : ISerializableContainer<GameplayExperience, GameplayExperience.Serialized>
        {
            public ContentInfo contentInfo;
            public string creationAssetName;
            public string thumbnailAssetName;
            public string[] screenshotAssetNames;

            public GameplayExperience AsOriginalType(PackageInfo packageInfo = default) => new GameplayExperience(this, packageInfo);
            public string AsJSON(bool prettyPrint = false) => swole.Engine.ToJson(this, prettyPrint);
            public object AsNonserializableObject(PackageInfo packageInfo = default) => AsOriginalType(packageInfo);
        }

        public GameplayExperience.Serialized AsSerializableStruct() => new GameplayExperience.Serialized() { contentInfo = contentInfo, creationAssetName = creationAssetName, thumbnailAssetName = thumbnailAssetName, screenshotAssetNames = screenshotAssetNames };
        public object AsSerializableObject() => AsSerializableStruct();

        public GameplayExperience(GameplayExperience.Serialized serializable, PackageInfo packageInfo = default)
        {
            contentInfo = serializable.contentInfo;
            creationAssetName = serializable.creationAssetName;
            thumbnailAssetName = serializable.thumbnailAssetName;
            screenshotAssetNames = serializable.screenshotAssetNames;
        }

        #endregion

        #region IContent Implementations

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

        public string Name => contentInfo.name;
        public string Author
        {
            get
            {
                var asset = CreationAsset;
                if (asset == null) return contentInfo.author;
                return asset.Author;
            }
        }
        public string CreationDate => contentInfo.creationDate;
        public string LastEditDate => contentInfo.lastEditDate;
        public string Description => contentInfo.description;

        public PackageInfo packageInfo;
        public PackageInfo PackageInfo => packageInfo;

        public ContentInfo contentInfo;
        public ContentInfo ContentInfo => contentInfo;

        public IContent CreateCopyAndReplaceContentInfo(ContentInfo info)
        {
            var copy = new GameplayExperience(creationAssetName, thumbnailAssetName, screenshotAssetNames, packageInfo);
            copy.contentInfo = info;

            return copy;
        }

        public List<PackageIdentifier> ExtractPackageDependencies(List<PackageIdentifier> dependencies = null)
        {
            if (dependencies == null) dependencies = new List<PackageIdentifier>();

            var asset = CreationAsset;
            if (asset != null) asset.ExtractPackageDependencies(dependencies);     

            return dependencies;
        }

        #endregion

        [NonSerialized]
        protected Creation cachedCreationAsset;
        public Creation CreationAsset
        {
            get
            {
                if (cachedCreationAsset == null) cachedCreationAsset = ContentManager.FindContent<Creation>(creationAssetName, packageInfo);              
                return cachedCreationAsset;
            }
        }

        [NonSerialized]
        protected IImageAsset cachedThumbnailAsset;
        public IImageAsset ThumbnailAsset
        {
            get
            {
                if (cachedThumbnailAsset == null) cachedThumbnailAsset = ContentManager.FindContent<IImageAsset>(thumbnailAssetName, packageInfo);
                return cachedThumbnailAsset;
            }
        }

        [NonSerialized]
        protected IImageAsset[] cachedScreenshotAssets;
        public IImageAsset GetScreenshotAsset(int index) 
        {
            if (cachedScreenshotAssets == null && ScreenshotAssetCount > 0)
            {
                var pkg = ContentManager.FindPackage(packageInfo);
                if (pkg != null)
                {
                    cachedScreenshotAssets = new IImageAsset[screenshotAssetNames.Length];
                    for (int a = 0; a < screenshotAssetNames.Length; a++)
                    {
                        var assetName = screenshotAssetNames[a];
                        if (string.IsNullOrWhiteSpace(assetName)) continue;

                        if (pkg.TryFind<IImageAsset>(out IImageAsset asset, assetName))
                        {
                            cachedScreenshotAssets[a] = asset;
                        }
                    }
                }
            }
            
            return cachedScreenshotAssets == null || index < 0 || index >= cachedScreenshotAssets.Length ? null : cachedScreenshotAssets[index];
        }

        public GameplayExperience(string creationAssetName, string thumbnailAssetName, string[] screenshotAssetNames, PackageInfo packageInfo = default)
        {
            this.creationAssetName = creationAssetName;
            this.thumbnailAssetName = thumbnailAssetName;
            this.screenshotAssetNames = screenshotAssetNames;
            this.packageInfo = packageInfo;
        }

        protected string creationAssetName;
        public string CreationAssetName => creationAssetName;

        protected string thumbnailAssetName;
        public string ThumbnailAssetname => thumbnailAssetName;

        protected string[] screenshotAssetNames;
        public int ScreenshotAssetCount => screenshotAssetNames == null ? 0 : screenshotAssetNames.Length;
        public string GetScreenshotAssetName(int index) => screenshotAssetNames == null || index < 0 || index >= screenshotAssetNames.Length ? null : screenshotAssetNames[index];

    }

}
