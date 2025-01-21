using System;
using System.Collections.Generic;

namespace Swole
{

    /// <summary>
    /// TODO: Finish implementing this class into content loading pipeline to allow for half loaded packages
    /// </summary>
    public class ContentLoader<T> : IContent where T : IContent
    {

        public Type AssetType => typeof(T);
        public object Asset => content;

        public static implicit operator T(ContentLoader<T> loader) => loader.Content;

        public delegate void FullyLoadContentDelegate(IContent loadedContent);
        protected FullyLoadContentDelegate onFullyLoad;
        protected T content;
        public T Content
        {
            get
            {
                Load();
                return content;
            }
        }
        protected bool isFullyLoaded;
        public bool IsFullyLoaded => isFullyLoaded;

        public void Load()
        {
            if (invalid || IsFullyLoaded) return;

        }

        public ContentLoader() {}
        public ContentLoader(T content) 
        {
            if (content != null && content.IsValid) isFullyLoaded = true;
            this.content = content;
        }

        #region IContent
        protected PackageInfo packageInfo;
        public PackageInfo PackageInfo => packageInfo;

        protected ContentInfo contentInfo;
        public ContentInfo ContentInfo => contentInfo;

        public string Name => contentInfo.name;

        public string Author => contentInfo.author;

        public string CreationDate => contentInfo.creationDate;

        public string LastEditDate => contentInfo.lastEditDate;

        public string Description => contentInfo.description;

        public string OriginPath => throw new System.NotImplementedException();

        public string RelativePath => throw new System.NotImplementedException();

        public bool IsInternalAsset { get => false; set { } }

        public IContent CreateCopyAndReplaceContentInfo(ContentInfo info)
        {
            throw new System.NotImplementedException();
        }

        public IContent SetOriginPath(string path)
        {
            throw new System.NotImplementedException();
        }

        public IContent SetRelativePath(string path)
        {
            throw new System.NotImplementedException();
        }

        protected bool invalid;
        public void Delete()
        {
            if (IsFullyLoaded && content != null) content.Delete();
            Dispose();
        }
        public void Dispose()
        {
            invalid = true;
            content = default;
            isFullyLoaded = false;
        }
        public bool IsValid => invalid ? false : (IsFullyLoaded ? (content == null ? false : content.IsValid) : true);

        public List<PackageIdentifier> ExtractPackageDependencies(List<PackageIdentifier> dependencies = null)
        {
            throw new System.NotImplementedException();
        }
        #endregion

    }
}
