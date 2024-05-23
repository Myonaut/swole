#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
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
                var asset_ = FindTileSet(assetName, null, caseSensitive);
                if (asset_ != null)
                {
                    asset = asset_;
                    return true;
                }
            }
            else if (typeof(IAudioAsset).IsAssignableFrom(type))
            {
                var asset_ = FindAudioClip(assetName, null, caseSensitive);
                if (asset_ != null)
                {
                    asset = asset_;
                    return true;
                }
            }

            return false;
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

        public static TileSet FindTileSet(string id, string collectionId = null, bool caseSensitive = false)
        {
            var instance = Instance;
            if (instance == null) return null;

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
                        for(int a = 0; a < ri.TileCollectionCount; a++)
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
        public static List<AudioCollection> GetAllAudioCollection(List<AudioCollection> list = null)
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

        public static AudioClipProxy FindAudioClip(string id, string collectionId = null, bool caseSensitive = false)
        {
            var instance = Instance;
            if (instance == null) return null;

            if (!string.IsNullOrWhiteSpace(collectionId))
            {

                var collection = FindAudioCollection(collectionId);
                if (collection == null) return null;

                int setIndex = collection.IndexOf(id, caseSensitive);

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

                            var index = collection.IndexOf(id, caseSensitive);
                            if (index < 0) continue;

                            return collection.GetAudioClip(index);
                        }
                    }
                }

                //if (instance.resourcesExternal != null) // TODO: Add support for loading user generated audio
                //{
                /*
                foreach (var re in instance.resourcesExternal)
                {
                        for(int a = 0; a < re.AudioCollectionCount; a++)
                        {
                            var collection = re.GetAudioCollection(a);
                            if (collection == null) continue;

                            var index = collection.IndexOf(id);
                            if (index < 0) continue;

                            return collection.GetAudioClip(index);
                        }
                }
                */
                //}
            }

            return null;
        }

        #endregion

        private static Dictionary<Texture2D, ImageAsset> _internalImageAssets = new Dictionary<Texture2D, ImageAsset>();
        public static ImageAsset GetImageAsset(Texture2D texture)
        {
            if (texture == null) return null;
            if (_internalImageAssets.TryGetValue(texture, out var asset)) return asset;

            asset = new ImageAsset(new ContentInfo() { name = texture.name }, texture);
            _internalImageAssets[texture] = asset;
            return asset;
        }
        public static ImageAsset GetImageAsset(Sprite sprite)
        {
            if (sprite == null) return null;
            if (_internalImageAssets.TryGetValue(sprite.texture, out var asset)) return asset;

            asset = new ImageAsset(new ContentInfo() { name = sprite.name }, sprite);
            _internalImageAssets[sprite.texture] = asset;
            return asset;
        }

    }

}

#endif