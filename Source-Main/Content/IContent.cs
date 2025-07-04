using System;
using System.Collections.Generic;
using System.Globalization;

namespace Swole
{

    public interface IContent : ISwoleAsset, IPackageDependent
    {

        public const string dateFormat = "MM/dd/yyyy";

        public PackageInfo PackageInfo { get; }

        public ContentInfo ContentInfo { get; }

        public IContent CreateShallowCopyAndReplaceContentInfo(ContentInfo info);
        public IContent CreateCopyAndReplaceContentInfo(ContentInfo info);

        public string Author { get; }

        public string CreationDate { get; }

        public string LastEditDate { get; }

        public string Description { get; }

        public string OriginPath { get; }
        public IContent SetOriginPath(string path);

        public string RelativePath { get; }
        public IContent SetRelativePath(string path);

        public void Delete();

    }

}
