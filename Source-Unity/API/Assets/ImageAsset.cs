#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

namespace Swole.API.Unity
{
    public class ImageAsset : IImageAsset<ImageAsset, ImageAsset.Serialized>
    {

        public System.Type AssetType => typeof(ImageAsset);
        public object Asset => this;  

        /// <summary>
        /// 1 - 100. 1 is lowest quality.
        /// </summary>
        public const int _encodingQuality_JPG = 75;

        public static string GetFileExtension(int format, string path = null)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                if (path.AsID().EndsWith(ContentManager.fileExtension_JPG.AsID())) return ContentManager.fileExtension_JPG;
                if (path.AsID().EndsWith(ContentManager.fileExtension_PNG.AsID())) return ContentManager.fileExtension_PNG;
                if (path.AsID().EndsWith(ContentManager.fileExtension_GenericImage.AsID())) return ContentManager.fileExtension_GenericImage;
            }

            TextureFormat texFormat = (TextureFormat)format;
            if (texFormat == TextureFormat.RGB24 || texFormat == TextureFormat.DXT1 || texFormat == TextureFormat.DXT1Crunched)
            {
                return ContentManager.fileExtension_JPG;
            }
            if (texFormat == TextureFormat.ARGB32 || texFormat == TextureFormat.DXT5 || texFormat == TextureFormat.DXT5Crunched)
            {
                return ContentManager.fileExtension_PNG;
            }

            return ContentManager.fileExtension_GenericImage;
        }

        #region Serialization

        public string AsJSON(bool prettyPrint = false) => AsSerializableStruct().AsJSON(prettyPrint);

        [Serializable]
        public struct Serialized : ISerializableContainer<ImageAsset, ImageAsset.Serialized>
        {

            public ContentInfo contentInfo;
            public string SerializedName => contentInfo.name;
            public int width, height;
            public int format;
            public int mipCount;
            public bool linear;
            public bool compressed;

            public bool hasSprite;
            public float sprite_rectX;
            public float sprite_rectY;
            public float sprite_rectWidth;
            public float sprite_rectHeight;
            public float sprite_pivotX;
            public float sprite_pivotY;
            public float sprite_pixelsPerUnit;
            //public string sprite_meshType;
            public EngineInternal.Vector4 sprite_border;

            [NonSerialized] 
            public byte[] imageData; 
            public string encodedImageData;
            public string imagePath;

            public ImageAsset AsOriginalType(PackageInfo packageInfo = default) => new ImageAsset(this, packageInfo);
            public string AsJSON(bool prettyPrint = false) => swole.Engine.ToJson(this, prettyPrint);

            public object AsNonserializableObject(PackageInfo packageInfo = default) => AsOriginalType(packageInfo);

        }

        public static implicit operator Serialized(ImageAsset img)
        {
            Serialized s = new Serialized();

            s.contentInfo = img.contentInfo;
            s.imagePath = img.imagePath;
            s.linear = img.isLinear;
            s.compressed = img.isCompressed;

            if (img.Texture != null)
            {
                s.width = img.Texture.width;
                s.height = img.Texture.height;
                s.format = (int)img.Texture.format;
                s.mipCount = img.Texture.mipmapCount;

                //s.imageData = img.Texture.GetRawTextureData(); // Absurdly inefficient
                if (string.IsNullOrWhiteSpace(img.imagePath)) // Store the image data in the json file as a last resort. (Very inefficient)
                {
                    var bytes = img.Texture.GetRawTextureData();
                    s.encodedImageData = Convert.ToBase64String(bytes);
                }
            }
            if (img.HasSprite && !img.ignoreSpriteOnSerialization)
            {
                s.hasSprite = true;
                var rect = img.Sprite.rect;
                s.sprite_rectX = rect.x;
                s.sprite_rectY = rect.y;
                s.sprite_rectWidth = rect.width;
                s.sprite_rectHeight = rect.height; 
                var pivot = img.Sprite.pivot;
                s.sprite_pivotX = pivot.x / s.sprite_rectWidth;
                s.sprite_pivotY = pivot.y / s.sprite_rectHeight;  
                s.sprite_pixelsPerUnit = img.Sprite.pixelsPerUnit;
                //s.sprite_meshType = SpriteMeshType.FullRect.ToString();
                s.sprite_border = UnityEngineHook.AsSwoleVector(img.Sprite.border); 
            }
             
            return s; 
        }

        public ImageAsset.Serialized AsSerializableStruct() => this; 
        public object AsSerializableObject() => AsSerializableStruct(); 

        public ImageAsset(ImageAsset.Serialized serializable, PackageInfo packageInfo = default)
        {
            this.packageInfo = packageInfo;
            this.contentInfo = serializable.contentInfo;

            this.isLinear = serializable.linear;
            this.isCompressed = serializable.compressed;

            this.imagePath = serializable.imagePath; // This is used by ContentManager before the asset is deserialized

            if ((serializable.encodedImageData != null && serializable.encodedImageData.Length > 0) || (serializable.imageData != null && serializable.imageData.Length > 0))
            {

                if (serializable.imageData != null)
                {
                    try
                    {
                        /*if (serializable.mipCount > 0)
                        {
                            texture = new Texture2D(serializable.width, serializable.height, (TextureFormat)serializable.format, serializable.mipCount, serializable.linear); 
                        } 
                        else
                        {
                            texture = new Texture2D(2, 2, (TextureFormat)serializable.format, false, serializable.linear);
                        }
                        ImageConversion.LoadImage(texture, serializable.imageData); // Executes .Apply()*/

                        try
                        {
                            var res = ExternalAssets.CreateNewTextureAsset(new ExternalAssets.ImageLoaderContext() { filename = serializable.imagePath, fileExtension = GetFileExtension(serializable.format, serializable.imagePath), options = new ExternalAssets.ImageLoaderOptions() { noMipMaps = serializable.mipCount <= 0, linearColorSpace = serializable.linear } }, serializable.imageData, this.isCompressed);
                            texture = res.texture;   
                        } 
                        catch(Exception) 
                        {
                            // Last resort
                            texture = new Texture2D(serializable.width, serializable.height, (TextureFormat)serializable.format, serializable.mipCount, serializable.linear);
                            texture.LoadRawTextureData(serializable.imageData);   
                            texture.Apply(); 
                        }
                    }  
                    catch(Exception ex)
                    {
                        swole.LogError(ex);
                        if (texture != null) GameObject.Destroy(texture);
                        texture = null;
                    }
                }
                
                if (texture == null && serializable.encodedImageData != null)
                {
                    texture = new Texture2D(serializable.width, serializable.height, (TextureFormat)serializable.format, serializable.mipCount, serializable.linear);
                    //texture.LoadRawTextureData(serializable.imageData);
                    var bytes = Convert.FromBase64String(serializable.encodedImageData);
                    texture.LoadRawTextureData(bytes);
                    texture.Apply(); 
                }

                if (serializable.hasSprite && texture != null)
                {
                    sprite = Sprite.Create(texture, new Rect(serializable.sprite_rectX, serializable.sprite_rectY, serializable.sprite_rectWidth, serializable.sprite_rectHeight), new Vector2(serializable.sprite_pivotX, serializable.sprite_pivotY), serializable.sprite_pixelsPerUnit, 0, SpriteMeshType.FullRect, UnityEngineHook.AsUnityVector(serializable.sprite_border));
                }
            }           
        }

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
        public bool HasCollectionID => !string.IsNullOrWhiteSpace(collectionId);

        public bool IsValid => Texture != null;
        public void Dispose()
        {
            if (!IsInternalAsset)
            {
                if (sprite != null) GameObject.Destroy(sprite);
                if (texture != null) GameObject.Destroy(texture);
            }
            sprite = null;
            texture = null;
        }
        public void DisposeSelf()
        {
            sprite = null;
            texture = null;
        }
        public void Delete()
        {
            if (!string.IsNullOrWhiteSpace(imagePath))
            {
                if (ContentManager.TryFindLocalPackage(packageInfo.GetIdentity(), out var lpkg, false, false) && lpkg.workingDirectory.Exists)
                {
                    DeleteImagePathAsset(lpkg);
                }
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
            var tex = texture == null ? null : new Texture2D(texture.width, texture.height, texture.format, texture.mipmapCount, isLinear);
            var content = new ImageAsset(info, tex, isCompressed, packageInfo, isLinear); 
            
            return content;
        }
        public IContent CreateShallowCopyAndReplaceContentInfo(ContentInfo info)
        {
            var content = new ImageAsset(info, texture, isCompressed, packageInfo, isLinear);
            content.sprite = sprite;
            
            return content;
        }

        public List<PackageIdentifier> ExtractPackageDependencies(List<PackageIdentifier> dependencies = null)
        {
            if (dependencies == null) dependencies = new List<PackageIdentifier>();
            return dependencies;
        }

        public bool IsIdenticalAsset(ISwoleAsset otherAsset) => ReferenceEquals(this, otherAsset) || (otherAsset is IImageAsset asset && ReferenceEquals(Instance, asset.Instance));

        #endregion

        public ImageAsset() {}

        public ImageAsset(ContentInfo contentInfo, Texture2D texture, bool isCompressed, PackageInfo packageInfo = default, bool isLinear = false)
        {
            this.contentInfo = contentInfo;
            this.packageInfo = packageInfo;

            this.texture = texture;
            this.isCompressed = isCompressed;
            this.isLinear = isLinear;
        }
        public ImageAsset(ContentInfo contentInfo, Sprite sprite, bool isCompressed, PackageInfo packageInfo = default, bool isLinear = false) : this(contentInfo, sprite.texture, isCompressed, packageInfo, isLinear)
        {
            this.sprite = sprite;
        }

        public ImageAsset(string name, string author, DateTime creationDate, DateTime lastEditDate, string description, Texture2D texture, bool isCompressed, PackageInfo packageInfo = default, bool isLinear = false)
            : this(new ContentInfo() { name = name, author = author, creationDate = creationDate.ToString(IContent.dateFormat), lastEditDate = lastEditDate.ToString(IContent.dateFormat), description = description }, texture, isCompressed, packageInfo, isLinear) { }
        public ImageAsset(string name, string author, DateTime creationDate, DateTime lastEditDate, string description, Sprite sprite, bool isCompressed, PackageInfo packageInfo = default, bool isLinear = false) 
            : this(new ContentInfo() { name = name, author = author, creationDate = creationDate.ToString(IContent.dateFormat), lastEditDate = lastEditDate.ToString(IContent.dateFormat), description = description }, sprite, isCompressed, packageInfo, isLinear) { }

        public ImageAsset(string name, string author, string creationDate, string lastEditDate, string description, Texture2D texture, bool isCompressed, PackageInfo packageInfo = default, bool isLinear = false)
            : this(new ContentInfo() { name = name, author = author, creationDate = creationDate, lastEditDate = lastEditDate, description = description }, texture, isCompressed, packageInfo, isLinear) { }
        public ImageAsset(string name, string author, string creationDate, string lastEditDate, string description, Sprite sprite, bool isCompressed, PackageInfo packageInfo = default, bool isLinear = false)
            : this(new ContentInfo() { name = name, author = author, creationDate = creationDate, lastEditDate = lastEditDate, description = description }, sprite, isCompressed, packageInfo, isLinear) { }

        protected bool isLinear;
        public bool IsLinear => isLinear;

        protected bool isCompressed;
        public bool IsCompressed => isCompressed;

        protected Texture2D texture;
        public Texture2D Texture => texture == null ? sprite == null ? null : sprite.texture : texture;
        public static implicit operator Texture2D(ImageAsset asset) => asset == null ? null : asset.Texture;
        public static implicit operator Sprite(ImageAsset asset) => asset == null ? null : asset.Sprite; 

        public object Instance => Texture;

        public bool ignoreSpriteOnSerialization;
        protected Sprite sprite;
        public bool HasSprite => sprite != null;
        public Sprite Sprite
        {
            get
            { 
                if (sprite == null && texture != null)
                {
                    sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                }

                return sprite;
            }
        }

        /// <summary>
        /// Can be a package local path or a web url 
        /// </summary>
        public string imagePath;

        public bool DeleteImagePathAsset(ContentManager.LocalPackage localPackage) => DeleteImagePathAsset(localPackage.workingDirectory.FullName);
        public bool DeleteImagePathAsset(string directory)
        {
            if (string.IsNullOrWhiteSpace(imagePath)) return false;

            var fileName = Path.Combine(directory, imagePath);
            if (File.Exists(fileName)) 
            { 
                File.Delete(fileName);
                return true;
            }
            return false; 
        }
        public bool RenameImagePathAsset(ContentManager.LocalPackage localPackage, string newName, bool overwrite = true) => RenameImagePathAsset(localPackage.workingDirectory.FullName, newName, overwrite);
        public bool RenameImagePathAsset(string directory, string newName, bool overwrite = true)
        {
            if (string.IsNullOrWhiteSpace(imagePath)) return false;

            var temp = imagePath;
            var prevFileName = Path.Combine(directory, imagePath); 
            imagePath = newName + Path.GetExtension(imagePath);
            var newFileName = Path.Combine(directory, imagePath);
            if (File.Exists(prevFileName))
            {
                if (File.Exists(newFileName)) 
                {
                    if (overwrite) File.Delete(newFileName); else 
                    {
                        imagePath = temp;
                        return false; 
                    }
                }
                File.Move(prevFileName, newFileName);
                return true;
            }
            return false;
        }
        public bool SaveTextureInPackageDirectory(ContentManager.LocalPackage localPackage, bool overwrite = true, bool incrementIfExists = false)
        {
            if (Texture == null)
            {
                swole.LogError($"Tried to save {nameof(ImageAsset)} '{Name}' but its texture object was null.");
                return false; 
            }

            string fileExtension = ContentManager.fileExtension_GenericImage;
            if (Texture.format == TextureFormat.RGB24/* || Texture.format == TextureFormat.DXT1 || Texture.format == TextureFormat.DXT1Crunched*/)
            {
                fileExtension = ContentManager.fileExtension_JPG;
            }
            else if (Texture.format == TextureFormat.ARGB32/* || Texture.format == TextureFormat.DXT5 || Texture.format == TextureFormat.DXT5Crunched*/)
            {
                fileExtension = ContentManager.fileExtension_PNG; 
            }

            string fileName = $"{Name}.{fileExtension}";
            imagePath = fileName;
            fileName = Path.Combine(localPackage.workingDirectory.FullName, fileName);

            if (File.Exists(fileName)) 
            {
                if (overwrite)
                {
                    File.Delete(fileName);
                }
                else if (incrementIfExists)
                {
                    int i = 2;
                    fileName = $"{Name}_{i}.{fileExtension}";
                    imagePath = fileName;
                    fileName = Path.Combine(localPackage.workingDirectory.FullName, fileName);
                    while (File.Exists(fileName))
                    {
                        i++;
                        fileName = $"{Name}_{i}.{fileExtension}";
                        imagePath = fileName;
                        fileName = Path.Combine(localPackage.workingDirectory.FullName, fileName); 
                    }
                }
                else return false; 
            }

            try
            {
                if (fileExtension == ContentManager.fileExtension_JPG)
                {
                    File.WriteAllBytes(fileName, Texture.EncodeToJPG(ImageAsset._encodingQuality_JPG));
                }
                else if (fileExtension == ContentManager.fileExtension_PNG)
                {
                    File.WriteAllBytes(fileName, Texture.EncodeToPNG());
                }
                else
                {
                    File.WriteAllBytes(fileName, Texture.GetRawTextureData());
                }
            }
            catch (Exception ex)
            {
                swole.LogError($"Tried write to '{fileName}' but there was an error.");
                throw ex;
            }

            return true;
        }

    }
}

#endif