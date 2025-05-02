#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole.API.Unity
{

    [CreateAssetMenu(fileName = "NewResourcesRef", menuName = "Swole/Resources/InternalResources", order = 1)]
    public class InternalResources : ScriptableObject
    {

        public const string _mainResourcePath = "resources_internal";
        public static string _mainPath = $"{_mainResourcePath}.asset";
#if UNITY_EDITOR
        public static string _mainPathEditor => $"Assets/Resources/{_mainPath}";

        private const string _resourcesPrefix = "Resources/";
        public static List<InternalResources.Locator> GetResourcesInFolder<T>(string localFolder, bool includeChildFolders, List<InternalResources.Locator> outputList = null) where T : UnityEngine.Object
        {
            if (outputList == null) outputList = new List<InternalResources.Locator>();

            var assets = UnityEditor.AssetDatabase.FindAssets($"t:{typeof(T).Name}", new string[] { localFolder });
            foreach (var asset in assets) if (!string.IsNullOrWhiteSpace(asset))
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(asset);
                    if (!includeChildFolders && !Path.GetDirectoryName(path).Equals(localFolder)) continue;
                    //path = Path.GetRelativePath("Assets/Resources/", path);
                    int prefixInd = path.IndexOf(_resourcesPrefix);
                    if (prefixInd >= 0)
                    {
                        path = path.Substring(prefixInd + _resourcesPrefix.Length); 
                    }

                    outputList.Add(new InternalResources.Locator() { id = Path.GetFileNameWithoutExtension(path), path = Path.ChangeExtension(path, null) });
                }

            return outputList;
        }
#endif

        [Serializable]
        public struct Locator
        {

            public string id;
            public string path;

        }

        public Texture2D defaultPreviewTexture_Creation;

        #region Tile Collections

        [SerializeField]
        public Locator[] tileCollections;
        public int TileCollectionCount => tileCollections == null ? 0 : tileCollections.Length;
        public TileCollection GetTileCollection(int index)
        {
            if (tileCollections == null || index < 0 || index >= tileCollections.Length) return null;

            var locator = tileCollections[index];
            if (!string.IsNullOrWhiteSpace(locator.path)) return Resources.Load<TileCollection>(locator.path);

            return null;
        }
        public int IndexOfTileCollection(string tileCollectionId)
        {
            if (string.IsNullOrWhiteSpace(tileCollectionId)) return -1;

            tileCollectionId = tileCollectionId.AsID();
            for (int a = 0; a < TileCollectionCount; a++)
            {
                var collection = tileCollections[a];
                if (string.IsNullOrWhiteSpace(collection.id) || collection.id.AsID() != tileCollectionId) continue;
                return a;
            }

            return -1;
        }
        public TileCollection LoadTileCollection(string id)
        {
            int index = IndexOfTileCollection(id);
            if (index < 0) return null;

            return GetTileCollection(index);
        }
        public bool IsExistingTileCollection(string id) => IndexOfTileCollection(id) >= 0;

        public List<TileCollection> GetAllTileCollections(List<TileCollection> list = null)
        {
            if (list == null) list = new List<TileCollection>();

            if (tileCollections != null)
            {
                foreach(var locator in tileCollections)
                {
                    var collection = Resources.Load<TileCollection>(locator.path);
                    if (collection == null || list.Contains(collection)) continue;

                    list.Add(collection);
                }
            }

            return list;
        }

        #endregion

        #region Audio Collections

        [SerializeField]
        public Locator[] audioCollections;
        public int AudioCollectionCount => audioCollections == null ? 0 : audioCollections.Length;
        public AudioCollection GetAudioCollection(int index)
        {
            if (audioCollections == null || index < 0 || index >= audioCollections.Length) return null;

            var locator = audioCollections[index];
            if (!string.IsNullOrWhiteSpace(locator.path)) return Resources.Load<AudioCollection>(locator.path);

            return null;
        }
        public int IndexOfAudioCollection(string collectionId)
        {
            if (string.IsNullOrWhiteSpace(collectionId)) return -1;

            collectionId = collectionId.AsID();
            for (int a = 0; a < AudioCollectionCount; a++)
            {
                var collection = audioCollections[a];
                if (string.IsNullOrWhiteSpace(collection.id) || collection.id.AsID() != collectionId) continue;
                return a;
            }

            return -1;
        }
        public AudioCollection LoadAudioCollection(string id)
        {
            int index = IndexOfAudioCollection(id);
            if (index < 0) return null;

            return GetAudioCollection(index);
        }
        public bool IsExistingAudioCollection(string id) => IndexOfTileCollection(id) >= 0;

        public List<AudioCollection> GetAllAudioCollections(List<AudioCollection> list = null)
        {
            if (list == null) list = new List<AudioCollection>();

            if (audioCollections != null)
            {
                foreach (var locator in audioCollections)
                {
                    var collection = Resources.Load<AudioCollection>(locator.path);
                    if (collection == null || list.Contains(collection)) continue;

                    list.Add(collection);
                }
            }

            return list;
        }

        #endregion

        #region Material Collections

        [SerializeField]
        public Locator[] materialCollections;
        public int MaterialCollectionCount => materialCollections == null ? 0 : materialCollections.Length;
        public MaterialCollection GetMaterialCollection(int index)
        {
            if (materialCollections == null || index < 0 || index >= materialCollections.Length) return null;

            var locator = materialCollections[index];
            if (!string.IsNullOrWhiteSpace(locator.path)) return Resources.Load<MaterialCollection>(locator.path);

            return null;
        }
        public int IndexOfMaterialCollection(string collectionId)
        {
            if (string.IsNullOrWhiteSpace(collectionId)) return -1;

            collectionId = collectionId.AsID();
            for (int a = 0; a < MaterialCollectionCount; a++)
            {
                var collection = materialCollections[a];
                if (string.IsNullOrWhiteSpace(collection.id) || collection.id.AsID() != collectionId) continue;
                return a;
            }

            return -1;
        }
        public MaterialCollection LoadMaterialCollection(string id)
        {
            int index = IndexOfMaterialCollection(id);
            if (index < 0) return null;

            return GetMaterialCollection(index);
        }
        public bool IsExistingMaterialCollection(string id) => IndexOfMaterialCollection(id) >= 0;

        public List<MaterialCollection> GetAllMaterialCollections(List<MaterialCollection> list = null)
        {
            if (list == null) list = new List<MaterialCollection>();

            if (materialCollections != null)
            {
                foreach (var locator in materialCollections)
                {
                    var collection = Resources.Load<MaterialCollection>(locator.path);
                    if (collection == null || list.Contains(collection)) continue;

                    list.Add(collection);
                }
            }

            return list;
        }

        #endregion

        #region Image Collections

        [SerializeField]
        public Locator[] imageCollections;
        public int ImageCollectionCount => imageCollections == null ? 0 : imageCollections.Length;
        public ImageCollection GetImageCollection(int index)
        {
            if (imageCollections == null || index < 0 || index >= imageCollections.Length) return null;

            var locator = imageCollections[index];
            if (!string.IsNullOrWhiteSpace(locator.path)) return Resources.Load<ImageCollection>(locator.path);

            return null;
        }
        public int IndexOfImageCollection(string collectionId)
        {
            if (string.IsNullOrWhiteSpace(collectionId)) return -1;

            collectionId = collectionId.AsID();
            for (int a = 0; a < ImageCollectionCount; a++)
            {
                var collection = imageCollections[a];
                if (string.IsNullOrWhiteSpace(collection.id) || collection.id.AsID() != collectionId) continue;
                return a;
            }

            return -1;
        }
        public ImageCollection LoadImageCollection(string id)
        {
            int index = IndexOfImageCollection(id);
            if (index < 0) return null;

            return GetImageCollection(index);
        }
        public bool IsExistingImageCollection(string id) => IndexOfImageCollection(id) >= 0;

        public List<ImageCollection> GetAllImageCollections(List<ImageCollection> list = null)
        {
            if (list == null) list = new List<ImageCollection>();

            if (imageCollections != null)
            {
                foreach (var locator in imageCollections)
                {
                    var collection = Resources.Load<ImageCollection>(locator.path);
                    if (collection == null || list.Contains(collection)) continue;

                    list.Add(collection);
                }
            }

            return list;
        }

        #endregion

    }

}

#endif