#if (UNITY_STANDALONE || UNITY_EDITOR)

#if BULKOUT_ENV
using RLD; // Paid Asset Integration https://assetstore.unity.com/packages/tools/modeling/runtime-level-design-52325
#endif

using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole.API.Unity
{

    public class RuntimePrefabHandler : MonoBehaviour
    {

#if BULKOUT_ENV
        private RTPrefabLibDb rtLib; // Paid Asset Integration https://assetstore.unity.com/packages/tools/modeling/runtime-level-design-52325
#endif

        public void Awake()
        {

#if BULKOUT_ENV
            rtLib = GameObject.FindFirstObjectByType<RTPrefabLibDb>(); // Paid Asset Integration https://assetstore.unity.com/packages/tools/modeling/runtime-level-design-52325
            if (rtLib == null) Destroy(this);
#endif

        }

        public void Update()
        {

#if BULKOUT_ENV
            if (rtLib == null) return;
#endif

        }

        public enum AddTileCollectionResult
        {

            UnknownError, Success, NoPrefabLoaderFound, EmptyCollection, CollectionAlreadyPresent, FailedToCreateLib

        }

        public void AddTileCollection(string id)
        {
            AddTileCollectionWithResult(id);
        }

        public AddTileCollectionResult AddTileCollectionWithResult(string id)
        {

            var collection = ResourceLib.FindTileCollection(id);
            if (collection == null || collection.tileSets == null || collection.tileSets.Length == 0) return AddTileCollectionResult.EmptyCollection;

#if BULKOUT_ENV // Paid Asset Integration https://assetstore.unity.com/packages/tools/modeling/runtime-level-design-52325
            var lib = rtLib.GetLib(collection.name); 
            if (lib != null) return AddTileCollectionResult.CollectionAlreadyPresent;
            lib = rtLib.CreateLib(collection.name);
            if (lib == null) return AddTileCollectionResult.FailedToCreateLib;
#else
            return AddTileCollectionResult.NoPrefabLoaderFound;
#endif

            swole.Log($"Loading Tile Collection '{collection.id}'");

            foreach (var tileSet in collection.tileSets)
            {

                if (tileSet == null || tileSet.TileCount <= 0) continue;

                var tileSource = tileSet.Source;

                for (int tileIndex = 0; tileIndex < tileSet.TileCount; tileIndex++)
                {

                    var tile = tileSet.tiles[tileIndex];
                    if (tile == null) continue;

                    var prefabObj = tileSet.CreatePreRuntimeTilePrefab(tileIndex, tileSource);
                    if (prefabObj == null) continue;

                    var prototype = prefabObj.AddOrGetComponent<TilePrototype>();
                    prototype.tileSet = tileSet;
                    prototype.tileIndex = tileIndex;

#if BULKOUT_ENV // Paid Asset Integration https://assetstore.unity.com/packages/tools/modeling/runtime-level-design-52325
                    var prefab = lib.CreatePrefab(prefabObj, tile.previewTexture);
                    if (prefab == null)
                    {
                        prefabObj.SetActive(false);
                        GameObject.Destroy(prefabObj);
                        continue;
                    } 
#else

#endif

                    prefabObj.SetActive(false);

                    swole.Log($"Loaded Tile '{tile.name}'");

                }

            }

            return AddTileCollectionResult.Success;

        }

    }

}

#endif
