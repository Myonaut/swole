using System;
using System.Collections.Generic;
using UnityEngine;

namespace Swole.API.Unity
{
    public class ImageAsset : IImageAsset<ImageAsset, ImageAsset.Serialized>
    {

        #region Serialization

        public string AsJSON(bool prettyPrint = false) => AsSerializableStruct().AsJSON(prettyPrint);

        [Serializable]
        public struct Serialized : ISerializableContainer<ImageAsset, ImageAsset.Serialized>
        {

            public ContentInfo contentInfo;
            public int width, height;
            public int format;
            public int mipCount;
            public bool linear;
            public byte[] imageData; 

            public ImageAsset AsOriginalType(PackageInfo packageInfo = default) => new ImageAsset(this, packageInfo);
            public string AsJSON(bool prettyPrint = false) => swole.Engine.ToJson(this, prettyPrint);

            public object AsNonserializableObject(PackageInfo packageInfo = default) => AsOriginalType(packageInfo);

        }

        public static implicit operator Serialized(ImageAsset img)
        {
            Serialized s = new Serialized();

            s.contentInfo = img.contentInfo;
            s.width = img.texture.width;
            s.height = img.texture.height;
            s.format = (int)img.texture.format;
            s.mipCount = img.texture.mipmapCount;
            s.linear = img.isLinear;

            return s;
        }

        public ImageAsset.Serialized AsSerializableStruct() => new ImageAsset.Serialized();
        public object AsSerializableObject() => AsSerializableStruct();

        public ImageAsset(ImageAsset.Serialized serializable, PackageInfo packageInfo = default)
        {
            this.packageInfo = packageInfo;
            this.contentInfo = serializable.contentInfo;

            if (serializable.imageData != null && serializable.imageData.Length > 0)
            {
                texture = new Texture2D(serializable.width, serializable.height, (TextureFormat)serializable.format, serializable.mipCount, serializable.linear);
                texture.LoadRawTextureData(serializable.imageData);
            }

            this.isLinear = serializable.linear;
        }

        #endregion

        #region IImageAsset Implementations

        public ImageAsset Asset => this;

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
        public string Author => contentInfo.author;
        public string CreationDate => contentInfo.creationDate;
        public string LastEditDate => contentInfo.lastEditDate;
        public string Description => contentInfo.description;

        public PackageInfo packageInfo;
        public PackageInfo PackageInfo => packageInfo;

        public ContentInfo contentInfo;
        public ContentInfo ContentInfo => contentInfo;

        public IContent CreateCopyAndReplaceContentInfo(ContentInfo info)
        {
            var content = new ImageAsset(info, texture, packageInfo);
            content.sprite = sprite;

            return content;
        }

        public List<PackageIdentifier> ExtractPackageDependencies(List<PackageIdentifier> dependencies = null)
        {
            if (dependencies == null) dependencies = new List<PackageIdentifier>();
            return dependencies;
        }

        #endregion

        public ImageAsset() {}

        public ImageAsset(ContentInfo contentInfo, Texture2D texture, PackageInfo packageInfo = default, bool isLinear = false)
        {
            this.contentInfo = contentInfo;
            this.packageInfo = packageInfo;

            this.texture = texture;
            this.isLinear = isLinear;
        }
        public ImageAsset(ContentInfo contentInfo, Sprite sprite, PackageInfo packageInfo = default, bool isLinear = false) : this(contentInfo, sprite.texture, packageInfo, isLinear)
        {
            this.sprite = sprite;
        }

        protected bool isLinear;
        public bool IsLinear => isLinear;

        protected Texture2D texture;
        public Texture2D Texture => texture;
        public object Instance => Texture;

        protected Sprite sprite;
        public Sprite Sprite
        {
            get
            {
                if (sprite == null)
                {
                    var tex = Texture;
                    sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                }

                return sprite;
            }
        }

    }
}
