#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement; 

namespace Swole.API.Unity
{

    public class ResourceLib : SingletonBehaviour<ResourceLib>
    {

        // Refrain from update calls
        public override bool ExecuteInStack => false;
        public override void OnUpdate() { }
        public override void OnLateUpdate() { }
        public override void OnFixedUpdate() { }
        //

        public static bool TryFindAsset<T>(out T asset, string assetName, bool caseSensitive = false) where T : ISwoleAsset
        {
            asset = default;
            if (TryFindAsset(out var _asset, assetName, typeof(T), caseSensitive))
            {
                asset = (T)_asset; 
                return true;
            }

            return false;
        }
        public static bool TryFindAsset(out ISwoleAsset asset, string assetName, Type type, bool caseSensitive = false)
        {
            asset = default;   

            var instance = Instance;
            if (instance == null) return false;

            if (typeof(ITileSet).IsAssignableFrom(type))
            {
                var asset_ = FindTileSet(assetName, null, false, caseSensitive);
                if (asset_ != null)
                {
                    asset = asset_;
                    return true;
                }
            }
            else if (typeof(IMaterialAsset).IsAssignableFrom(type))
            {
                var asset_ = FindMaterial(assetName, null, false, caseSensitive);
                if (asset_ != null)
                {
                    asset = asset_;
                    return true;
                }
            }
            else if (typeof(IAudioAsset).IsAssignableFrom(type))
            {
                var asset_ = FindAudioClip(assetName, null, false, caseSensitive);
                if (asset_ != null)
                {
                    asset = asset_;
                    return true;
                }
            }
            else if (typeof(IAudioMixer).IsAssignableFrom(type))
            {
                var asset_ = FindAudioMixer(assetName, null, false, caseSensitive);
                if (asset_ != null)
                {
                    asset = asset_;
                    return true;
                }
            }

            return false;
        }

        public const string _internalAssetCollectionPrefix = "~";
        public static void ResolveAssetIdString(string assetIdString, out string assetName, out string collectionName, out bool collectionIsConfirmedPackage)
        {
            assetName = string.Empty;
            collectionName = null;
            collectionIsConfirmedPackage = false;

            if (string.IsNullOrWhiteSpace(assetIdString)) return;

            assetName = Path.GetFileNameWithoutExtension(assetIdString);
            collectionName = Path.GetDirectoryName(assetIdString); 

            if (!string.IsNullOrWhiteSpace(collectionName) && !assetIdString.StartsWith(_internalAssetCollectionPrefix) && SwoleUtil.IsPackageStringWithVersion(collectionName)) collectionIsConfirmedPackage = true;
        }

        public InternalResources[] resourcesInternal;
        protected InternalResources mainResourceDatabase;
        public InternalResources MainResourceDatabase => mainResourceDatabase;

        public override bool DestroyOnLoad => false;

        protected override void OnInit()
        {

            base.OnInit();

            SceneManager.activeSceneChanged += OnSceneChange;

            resourcesInternal = Resources.LoadAll<InternalResources>("");
            mainResourceDatabase = Resources.Load<InternalResources>(InternalResources._mainResourcePath);
            if (mainResourceDatabase == null && resourcesInternal.Length > 0) mainResourceDatabase = resourcesInternal[0];
        }

        public override void OnDestroyed()
        {

            base.OnDestroyed();

            SceneManager.activeSceneChanged -= OnSceneChange;

        }

        protected void OnSceneChange(Scene oldScene, Scene newScene)
        {

            Resources.UnloadUnusedAssets(); 

        }

        public static Texture2D DefaultPreviewTexture_Creation
        {
            get
            {
                var instance = Instance;
                if (instance == null) return null;

                if (instance.mainResourceDatabase != null && instance.mainResourceDatabase.defaultPreviewTexture_Creation != null) return instance.mainResourceDatabase.defaultPreviewTexture_Creation;

                if (instance.resourcesInternal != null)
                {
                    Texture2D tex = null;
                    foreach (var ri in instance.resourcesInternal)
                    {
                        if (ri.defaultPreviewTexture_Creation == null) continue;
                        tex = ri.defaultPreviewTexture_Creation;
                        break;
                    }
                }

                return null;
            }
        }
        private static ImageAsset _defaultPreviewTextureAsset_Creation;
        public static ImageAsset DefaultPreviewTextureAsset_Creation
        {
            get
            {
                if (_defaultPreviewTextureAsset_Creation != null && _defaultPreviewTextureAsset_Creation.Texture != null) return _defaultPreviewTextureAsset_Creation;
                if (_defaultPreviewTextureAsset_Creation != null) _defaultPreviewTextureAsset_Creation.Dispose();
                _defaultPreviewTextureAsset_Creation = null;

                var tex = DefaultPreviewTexture_Creation;
                if (tex == null) return null;
                _defaultPreviewTextureAsset_Creation = new ImageAsset(tex.name, string.Empty, DateTime.Now, DateTime.Now, string.Empty, tex, ExternalAssets.CanTextureBeCompressed(tex.width, tex.height));
                return _defaultPreviewTextureAsset_Creation;
            }
        }

        #region Tiles

        public static TileCollection FindTileCollection(string id)
        {

            var instance = Instance;
            if (instance == null) return null;

            if (instance.resourcesInternal != null)
            {
                foreach (var ri in instance.resourcesInternal)
                {
                    var collection = ri.LoadTileCollection(id);
                    if (collection != null) return collection;
                }
            }

            //if (instance.resourcesExternal != null) // TODO: Add support for loading user generated tiles
            //{
                /*
                foreach (var re in instance.resourcesExternal)
                {
                    var collection = re.LoadTileCollection(id);
                    if (collection != null) return collection;
                }
                */
            //}

            return null;

        }
        public static List<TileCollection> GetAllTileCollections(List<TileCollection> list = null)
        {
            if (list == null) list = new List<TileCollection>();

            var instance = Instance;
            if (instance == null) return list;

            if (instance.resourcesInternal != null)
            {
                foreach (var ri in instance.resourcesInternal)
                {
                    list = ri.GetAllTileCollections(list);
                }
            }

            //if (instance.resourcesExternal != null) // TODO: Add support for loading user generated tiles
            //{
            /*
            foreach (var re in instance.resourcesExternal)
            {
                list = re.GetAllTileCollections(list);
            }
            */
            //}

            return list;
        }

        public static TileSet FindTileSet(string id, string collectionId = null, bool collectionIsPackage = false, bool caseSensitive = false)
        {
            var instance = Instance;
            if (instance == null) return null;

            if (collectionIsPackage)
            {
                // TODO: search packages for tileset
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(collectionId))
                {

                    var collection = FindTileCollection(collectionId);
                    if (collection == null) return null;

                    int setIndex = collection.IndexOf(id, caseSensitive);

                    if (setIndex >= 0) return collection.GetTileSet(setIndex);
                }
                else
                {
                    if (instance.resourcesInternal != null)
                    {
                        foreach (var ri in instance.resourcesInternal)
                        {
                            for (int a = 0; a < ri.TileCollectionCount; a++)
                            {
                                var collection = ri.GetTileCollection(a);
                                if (collection == null) continue;

                                var index = collection.IndexOf(id, caseSensitive);
                                if (index < 0) continue;

                                return collection.GetTileSet(index);
                            }
                        }
                    }

                    //if (instance.resourcesExternal != null) // TODO: Add support for loading user generated tiles
                    //{
                    /*
                    foreach (var re in instance.resourcesExternal)
                    {
                            for(int a = 0; a < re.TileCollectionCount; a++)
                            {
                                var collection = re.GetTileCollection(a);
                                if (collection == null) continue;

                                var index = collection.IndexOf(id);
                                if (index < 0) continue;

                                return collection.GetTileSet(index);
                            }
                    }
                    */
                    //}
                }
            }

            return null;
        }

        #endregion

        #region Audio

        public static AudioCollection FindAudioCollection(string id)
        {

            var instance = Instance;
            if (instance == null) return null;

            if (instance.resourcesInternal != null)
            {
                foreach (var ri in instance.resourcesInternal)
                {
                    var collection = ri.LoadAudioCollection(id);
                    if (collection != null) return collection;
                }
            }

            //if (instance.resourcesExternal != null) // TODO: Add support for loading user generated audio
            //{
            /*
            foreach (var re in instance.resourcesExternal)
            {
                var collection = re.LoadAudioCollection(id);
                if (collection != null) return collection;
            }
            */
            //}

            return null;

        }
        public static List<AudioCollection> GetAllAudioCollections(List<AudioCollection> list = null)
        {
            if (list == null) list = new List<AudioCollection>();

            var instance = Instance;
            if (instance == null) return list;

            if (instance.resourcesInternal != null)
            {
                foreach (var ri in instance.resourcesInternal)
                {
                    list = ri.GetAllAudioCollections(list);
                }
            }

            //if (instance.resourcesExternal != null) // TODO: Add support for loading user generated audio
            //{
            /*
            foreach (var re in instance.resourcesExternal)
            {
                list = re.GetAllAudioCollections(list);
            }
            */
            //}

            return list;
        }

        public static AudioClipProxy FindAudioClip(string id, string collectionId = null, bool collectionIsPackage = false, bool caseSensitive = false)
        {
            var instance = Instance;
            if (instance == null) return null; 
             
            if (collectionIsPackage)
            {
                // TODO: search packages for audio clip
            }
            else
            {

                if (!string.IsNullOrWhiteSpace(collectionId))
                {

                    var collection = FindAudioCollection(collectionId);
                    if (collection == null) return null;

                    int setIndex = collection.IndexOfClip(id, caseSensitive);

                    if (setIndex >= 0) return collection.GetAudioClip(setIndex);
                }
                else
                {
                    if (instance.resourcesInternal != null)
                    {
                        foreach (var ri in instance.resourcesInternal)
                        {
                            for (int a = 0; a < ri.AudioCollectionCount; a++)
                            {
                                var collection = ri.GetAudioCollection(a);
                                if (collection == null) continue;

                                var index = collection.IndexOfClip(id, caseSensitive);
                                if (index < 0) continue;

                                return collection.GetAudioClip(index);
                            }
                        }
                    }
                }
            }

            return null;
        }

        protected static AudioMixerProxy defaultAudioMixer;
        public const string defaultAudioMixerName = "Default";
        public static AudioMixerProxy DefaultAudioMixer
        {
            get
            {
                if (defaultAudioMixer == null) defaultAudioMixer = FindAudioMixer(defaultAudioMixerName); 

                return defaultAudioMixer;
            }
        }
        public static AudioMixerProxy FindAudioMixer(string id, string collectionId = null, bool collectionIsPackage = false, bool caseSensitive = false)
        {
            var instance = Instance;
            if (instance == null) return null;

            if (collectionIsPackage)
            {
                // TODO: search packages for audio mixer
            }
            else
            {

                if (!string.IsNullOrWhiteSpace(collectionId))
                {

                    var collection = FindAudioCollection(collectionId);
                    if (collection == null) return null;

                    int setIndex = collection.IndexOfMixer(id, caseSensitive);

                    if (setIndex >= 0) return collection.GetAudioMixer(setIndex);
                }
                else
                {
                    if (instance.resourcesInternal != null)
                    {
                        foreach (var ri in instance.resourcesInternal)
                        {
                            for (int a = 0; a < ri.AudioCollectionCount; a++)
                            {
                                var collection = ri.GetAudioCollection(a);
                                if (collection == null) continue;

                                var index = collection.IndexOfMixer(id, caseSensitive); 
                                if (index < 0) continue;

                                return collection.GetAudioMixer(index);
                            }
                        }
                    }
                }
            }

            return null;
        }

        #endregion

        #region Material

        public static MaterialCollection FindMaterialCollection(string id)
        {

            var instance = Instance;
            if (instance == null) return null;

            if (instance.resourcesInternal != null)
            {
                foreach (var ri in instance.resourcesInternal)
                {
                    var collection = ri.LoadMaterialCollection(id);
                    if (collection != null) return collection;
                }
            }

            return null;

        }
        public static List<MaterialCollection> GetAllMaterialCollections(List<MaterialCollection> list = null)
        {
            if (list == null) list = new List<MaterialCollection>();

            var instance = Instance;
            if (instance == null) return list;

            if (instance.resourcesInternal != null)
            {
                foreach (var ri in instance.resourcesInternal)
                {
                    list = ri.GetAllMaterialCollections(list);
                }
            }

            return list;
        }

        public static MaterialAsset FindMaterial(string id, string collectionId = null, bool collectionIsPackage = false, bool caseSensitive = false, PackageIdentifier packageContext = default)
        {
            var instance = Instance;
            if (instance == null) return null; 

            if (collectionIsPackage)
            {
                return ContentManager.FindContent<MaterialAsset>(id, string.IsNullOrWhiteSpace(collectionId) ? packageContext : new PackageIdentifier(collectionId));
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(collectionId))
                {
                    var collection = FindMaterialCollection(collectionId);
                    if (collection != null)
                    {
                        int setIndex = collection.IndexOfMaterial(id, caseSensitive);
                        if (setIndex >= 0) return collection.GetMaterial(setIndex);
                    }

                    return ContentManager.FindContent<MaterialAsset>(id, new PackageIdentifier(collectionId)); // if can't find it in internal assets, try to find it in packages
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(packageContext.name))
                    {
                        if (ContentManager.TryFindContent<MaterialAsset>(id, packageContext, out var asset)) return asset; 
                    }

                    if (instance.resourcesInternal != null)
                    {
                        foreach (var ri in instance.resourcesInternal)
                        {
                            for (int a = 0; a < ri.MaterialCollectionCount; a++)
                            {
                                var collection = ri.GetMaterialCollection(a); 
                                if (collection == null) continue;

                                var index = collection.IndexOfMaterial(id, caseSensitive);
                                if (index < 0) continue;

                                return collection.GetMaterial(index);
                            }
                        }
                    }
                }
            }

            return null;
        }

        #endregion

        #region Images

        private static Dictionary<Texture2D, ImageAsset> _internalImageAssets = new Dictionary<Texture2D, ImageAsset>();
        public static ImageAsset GetImageAsset(Texture2D texture)
        {
            if (texture == null) return null;
            if (_internalImageAssets.TryGetValue(texture, out var asset)) return asset;

            asset = new ImageAsset(new ContentInfo() { name = texture.name }, texture, ExternalAssets.CanTextureBeCompressed(texture.width, texture.height));
            _internalImageAssets[texture] = asset;
            return asset;
        }
        public static ImageAsset GetImageAsset(Sprite sprite)
        {
            if (sprite == null) return null;
            if (_internalImageAssets.TryGetValue(sprite.texture, out var asset)) return asset;

            asset = new ImageAsset(new ContentInfo() { name = sprite.name }, sprite,  ExternalAssets.CanTextureBeCompressed(sprite.texture.width, sprite.texture.height));
            _internalImageAssets[sprite.texture] = asset;
            return asset;
        }

        public static ImageCollection FindImageCollection(string id)
        {

            var instance = Instance;
            if (instance == null) return null;

            if (instance.resourcesInternal != null)
            {
                foreach (var ri in instance.resourcesInternal)
                {
                    var collection = ri.LoadImageCollection(id);
                    if (collection != null) return collection;
                }
            }

            return null;

        }
        public static List<ImageCollection> GetAllImageCollections(List<ImageCollection> list = null)
        {
            if (list == null) list = new List<ImageCollection>();

            var instance = Instance;
            if (instance == null) return list;

            if (instance.resourcesInternal != null)
            {
                foreach (var ri in instance.resourcesInternal)
                {
                    list = ri.GetAllImageCollections(list);
                }
            }

            return list;
        }

        public static ImageAsset FindImage(string id, string collectionId = null, bool collectionIsPackage = false, bool caseSensitive = false, PackageIdentifier packageContext = default)
        {
            var instance = Instance;
            if (instance == null) return null;

            if (collectionIsPackage)
            {
                return ContentManager.FindContent<ImageAsset>(id, string.IsNullOrWhiteSpace(collectionId) ? packageContext : new PackageIdentifier(collectionId));
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(collectionId))
                {
                    var collection = FindImageCollection(collectionId);
                    if (collection != null)
                    {
                        int setIndex = collection.IndexOfImage(id, caseSensitive);
                        if (setIndex >= 0) return collection.GetImage(setIndex);
                    }

                    return ContentManager.FindContent<ImageAsset>(id, new PackageIdentifier(collectionId)); // if can't find it in internal assets, try to find it in packages
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(packageContext.name))
                    {
                        if (ContentManager.TryFindContent<ImageAsset>(id, packageContext, out var asset)) return asset;
                    }

                    if (instance.resourcesInternal != null)
                    {
                        foreach (var ri in instance.resourcesInternal)
                        {
                            for (int a = 0; a < ri.ImageCollectionCount; a++)
                            {
                                var collection = ri.GetImageCollection(a);
                                if (collection == null) continue;

                                var index = collection.IndexOfImage(id, caseSensitive);
                                if (index < 0) continue;

                                return collection.GetImage(index);
                            }
                        }
                    }
                }
            }

            return null;
        }

        #endregion

    }

}

#endif