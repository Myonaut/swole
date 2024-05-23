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
        [SerializeField]
        private RTPrefabLibDb rtLib; // Paid Asset Integration https://assetstore.unity.com/packages/tools/modeling/runtime-level-design-52325
#endif

        public GameObject tilePickerWindow;
        public const string _addCollectionButtonName = "AddCollection";
        public const string _addPackageButtonName = "AddPackage";
        public const string _removeCollectionButtonName = "RemoveCollection";

        public void Awake()
        {

#if BULKOUT_ENV
            if (rtLib == null) rtLib = GameObject.FindFirstObjectByType<RTPrefabLibDb>(); // Paid Asset Integration https://assetstore.unity.com/packages/tools/modeling/runtime-level-design-52325
            if (rtLib == null)
            {
                swole.LogError($"[{nameof(RuntimePrefabHandler)}]: Could not locate {nameof(RTPrefabLibDb)}!");
                Destroy(this);
            }
#endif

            if (tilePickerWindow != null)
            {
                IEnumerator HidePickerOnStart()
                {
                    yield return null;
                    yield return null;
                    if (tilePickerWindow != null) tilePickerWindow.SetActive(false); 
                }
                StartCoroutine(HidePickerOnStart());
            }

        }

        public void Update()
        {

#if BULKOUT_ENV
            if (rtLib == null) return;
#endif

        }

        public bool HasSelectedACollection()
        {
#if BULKOUT_ENV
            if (rtLib != null && rtLib.NumLibs > 0) return rtLib.ActiveLib != null;
#endif

            return false;
        }
        public string GetNameOfSelectedCollection()
        {
#if BULKOUT_ENV
            if (rtLib != null && rtLib.NumLibs > 0) 
            {
                var activeLib = rtLib.ActiveLib;
                if (activeLib != null) return activeLib.Name;
            }
#endif
            return string.Empty;
        }
        public void RemoveSelectedCollection()
        {
#if BULKOUT_ENV
            if (rtLib != null && rtLib.NumLibs > 0)
            {
                var activeLib = rtLib.ActiveLib;
                int index = rtLib.GetLibIndex(activeLib);
                if (activeLib != null) rtLib.Remove(activeLib);
                if (rtLib.NumLibs <= 0)
                {
                    rtLib.Clear(); // Force rld to update previews
                } 
                else
                {
                    rtLib.SetActiveLib(index - 1); // Force rld to update previews
                }
            }
#endif
        }

        public enum AddTileCollectionResult
        {

            UnknownError, Success, NoPrefabLoaderFound, EmptyCollection, CollectionAlreadyPresent, FailedToCreateLib

        }

        public bool ContainsTileCollection(TileCollection collection) => ContainsTileCollection(collection.name);
        public bool ContainsTileCollection(string collectionName)
        {
#if BULKOUT_ENV
            if (rtLib.GetLib(collectionName) != null) return true; 
#endif
            return false;
        }

        public void AddTileCollection(string id)
        {
            AddTileCollection(ResourceLib.FindTileCollection(id));
        }
        public AddTileCollectionResult AddTileCollectionWithResult(string id)
        {
            return AddTileCollectionWithResult(ResourceLib.FindTileCollection(id));
        }
        public void AddTileCollection(TileCollection collection)
        {
            AddTileCollectionWithResult(collection);
        }
        public AddTileCollectionResult AddTileCollectionWithResult(TileCollection collection)
        {
            if (collection == null || collection.TileSetCount <= 0) return AddTileCollectionResult.EmptyCollection;

#if BULKOUT_ENV // Paid Asset Integration https://assetstore.unity.com/packages/tools/modeling/runtime-level-design-52325
            var lib = rtLib.GetLib(collection.name); 
            if (lib != null) return AddTileCollectionResult.CollectionAlreadyPresent;
            lib = rtLib.CreateLib(collection.name);
            if (lib == null) return AddTileCollectionResult.FailedToCreateLib;
#else
            return AddTileCollectionResult.NoPrefabLoaderFound;
#endif

            swole.Log($"Loading Tile Collection '{collection.ID}'"); 

            for(int a = 0; a < collection.TileSetCount; a++)
            {
                var tileSet = collection.GetTileSet(a);
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

        public enum AddPackageContentResult
        {

            UnknownError, Success, NoPrefabLoaderFound, EmptyPackage, PackageAlreadyPresent, FailedToCreateLib

        }

        public bool ContainsPackageContent(ContentPackage package) => ContainsPackageContent(package.GetIdentityString());
        public bool ContainsPackageContent(string packageIdentityString)
        {
#if BULKOUT_ENV
            if (rtLib.GetLib(packageIdentityString) != null) return true;
#endif
            return false;
        }

        public void AddPackageContent(string packageIdentityString, bool forceReload = true) => AddPackageContent(new PackageIdentifier(packageIdentityString), forceReload);
        public void AddPackageContent(PackageIdentifier packageIdentity, bool forceReload = true) => AddPackageContent(packageIdentity.name, packageIdentity.version, forceReload);
        public void AddPackageContent(string packageName, string packageVersion, bool forceReload = true) => AddPackageContentWithResult(packageName, packageVersion, forceReload);
        public void AddPackageContent(ContentPackage package, bool forceReload = true) => AddPackageContentWithResult(package, forceReload);
        

        public AddPackageContentResult AddPackageContentWithResult(string packageIdentityString, bool forceReload = true) => AddPackageContentWithResult(new PackageIdentifier(packageIdentityString), forceReload);
        public AddPackageContentResult AddPackageContentWithResult(PackageIdentifier packageIdentity, bool forceReload = true) => AddPackageContentWithResult(packageIdentity.name, packageIdentity.version, forceReload);
        public AddPackageContentResult AddPackageContentWithResult(string packageName, string packageVersion, bool forceReload = true) => AddPackageContentWithResult(ContentManager.FindPackage(packageName, packageVersion), forceReload);
        public AddPackageContentResult AddPackageContentWithResult(ContentPackage package, bool forceReload = true)
        {
            if (package == null || package.ContentCount == 0) return AddPackageContentResult.EmptyPackage;

#if BULKOUT_ENV // Paid Asset Integration https://assetstore.unity.com/packages/tools/modeling/runtime-level-design-52325
            var lib = rtLib.GetLib(package.GetIdentityString());
            if (lib != null) 
            { 
                if (!forceReload) return AddPackageContentResult.PackageAlreadyPresent;
                rtLib.Remove(lib);
            }
            lib = rtLib.CreateLib(package.GetIdentityString());
            if (lib == null) return AddPackageContentResult.FailedToCreateLib;
#else
            return AddPackageContentResult.NoPrefabLoaderFound;
#endif

            swole.Log($"Loading Package Content  '{package.GetIdentity()}'");

            int count = 0;
            for (int a = 0; a < package.ContentCount; a++)
            {
                var content = package.GetContent(a);
                if (content == null) continue;

#if BULKOUT_ENV
                Texture2D previewTexture = null;
#endif

                GameObject prefabObj = null;
                if (content is Creation creation)
                {
                    prefabObj = CreationBehaviour.CreatePreRuntimeCreationPrefab(creation);
                    if (prefabObj == null) continue;

                    var prototype = prefabObj.AddOrGetComponent<CreationPrototype>();
                    prototype.asset = creation;
                } 
                else continue;

                if (prefabObj == null) continue;

#if BULKOUT_ENV // Paid Asset Integration https://assetstore.unity.com/packages/tools/modeling/runtime-level-design-52325
                var prefab = lib.CreatePrefab(prefabObj, previewTexture);
                if (prefab == null)
                {
                    prefabObj.SetActive(false);
                    GameObject.Destroy(prefabObj);
                    continue;
                }
#else

#endif

                prefabObj.SetActive(false);
                count++;

                swole.Log($"Loaded {content.GetType().Name} '{content.Name}'"); 

            }
            if (count <= 0)
            {
                rtLib.Remove(lib);
                return AddPackageContentResult.EmptyPackage;
            }

            return AddPackageContentResult.Success;

        }

    }

}

#endif
