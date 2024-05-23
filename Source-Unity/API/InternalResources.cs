#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole.API.Unity
{

    [CreateAssetMenu(fileName = "NewResourcesRef", menuName = "Resources/InternalResources", order = 1)]
    public class InternalResources : ScriptableObject
    {

        public const string _mainResourcePath = "resources_internal";
        public static string _mainPath = $"{_mainResourcePath}.asset";
#if UNITY_EDITOR
        public static string _mainPathEditor => $"Assets/Resources/{_mainPath}";
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

    }

}

#endif