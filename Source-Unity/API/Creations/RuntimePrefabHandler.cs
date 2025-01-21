#if (UNITY_STANDALONE || UNITY_EDITOR)

#if BULKOUT_ENV
using RLD; // Paid Asset Integration https://assetstore.unity.com/packages/tools/modeling/runtime-level-design-52325
#endif

using System;

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

        protected readonly Dictionary<string, List<GameObject>> collectionPrefabs = new Dictionary<string, List<GameObject>>();
        protected void ClearCollectionPrefabs()
        {
            foreach (var prefabs in collectionPrefabs.Values)
            {
                if (prefabs == null) continue;

                foreach (var prefab in prefabs) if (prefab != null) GameObject.DestroyImmediate(prefab);
                prefabs.Clear();
            }
            collectionPrefabs.Clear(); 
        }
        protected void AddCollectionPrefab(string collectionName, GameObject prefab)
        {
            if (!collectionPrefabs.TryGetValue(collectionName, out var prefabs))
            {
                prefabs = new List<GameObject>();
                collectionPrefabs[collectionName] = prefabs; 
            }

            prefabs.Add(prefab);
        }

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
                    if (tilePickerWindow != null && CollectionCount <= 0) tilePickerWindow.SetActive(false);  
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

        public int CollectionCount
        {
            get
            {
#if BULKOUT_ENV
                return rtLib.NumLibs;
#else
                return 0;
#endif
            }
        }

        protected readonly Dictionary<RTPrefabLib, PrefabCollectionSource> libSources = new Dictionary<RTPrefabLib, PrefabCollectionSource>();

        public PrefabCollectionSource GetCollectionSource(int libIndex)
        {
#if BULKOUT_ENV
            if (libIndex < 0 || libIndex >= rtLib.NumLibs) return default;
            return GetCollectionSource(rtLib.GetLib(libIndex));
#else
            return default;
#endif
        }
#if BULKOUT_ENV
        public PrefabCollectionSource GetCollectionSource(RTPrefabLib lib)
        {
            if (lib != null && libSources.TryGetValue(lib, out var source)) return source;
            return default;
        }
#endif

        public PrefabCollectionSource GetActiveCollectionSource()
        {
#if BULKOUT_ENV
            if (rtLib != null && rtLib.NumLibs > 0)
            {
                var activeLib = rtLib.ActiveLib;
                if (activeLib == null) return default;
                return GetCollectionSource(activeLib);
            }
#endif
            return default;
        }

        public bool HasActiveCollection()
        {
#if BULKOUT_ENV
            if (rtLib != null && rtLib.NumLibs > 0) return rtLib.ActiveLib != null;
#endif

            return false;
        } 
        public int ActiveCollectionIndex
        {
            get
            {
#if BULKOUT_ENV
                if (rtLib != null && rtLib.NumLibs > 0) return rtLib.ActiveLibIndex;          
#endif
                return -1;
            }
            set
            {
                SetActiveCollection(value);
            }
        }
        public void SetActiveCollection(int index)
        {
#if BULKOUT_ENV
            if (rtLib != null && index >= 0 && index < rtLib.NumLibs) 
            {
                rtLib.SetActiveLib(index);
            }
#endif
        }
        public string GetNameOfActiveCollection()
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
#if BULKOUT_ENV
        public void RemoveCollection(RTPrefabLib lib)
        {
            if (lib == null) return;

            if (collectionPrefabs.TryGetValue(lib.Name, out var prefabs) && prefabs != null)
            {
                foreach (var prefab in prefabs) if (prefab != null) GameObject.DestroyImmediate(prefab);
                prefabs.Clear();
                collectionPrefabs.Remove(lib.Name);
            }

            libSources.Remove(lib);
            rtLib.Remove(lib);
            if (rtLib.NumLibs <= 0)
            {
                rtLib.Clear(); // Force rld to update previews
            }
        }
#endif
        public void RemoveActiveCollection()
        {
#if BULKOUT_ENV
            if (rtLib != null && rtLib.NumLibs > 0)
            {
                var activeLib = rtLib.ActiveLib;
                if (activeLib == null) return;
                int index = rtLib.GetLibIndex(activeLib);
                RemoveCollection(activeLib);
                if (rtLib.NumLibs > 0)
                {
                    rtLib.SetActiveLib(index - 1); // Force rld to update previews
                }
            }
#endif
        }
        public void ClearCollections()
        {
            ClearCollectionPrefabs();

#if BULKOUT_ENV
            if (rtLib != null && rtLib.NumLibs > 0)
            {
                //while (rtLib.NumLibs > 0) RemoveCollection(rtLib.GetLib(0)); //??
                libSources.Clear();
                rtLib.Clear(); 
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

        public void AddTileCollection(string id, bool setAsActiveCollection = true)
        {
            AddTileCollection(ResourceLib.FindTileCollection(id), setAsActiveCollection);
        }
        public AddTileCollectionResult AddTileCollectionWithResult(string id, bool setAsActiveCollection = true)
        {
            return AddTileCollectionWithResult(ResourceLib.FindTileCollection(id), setAsActiveCollection);
        }
        public void AddTileCollection(TileCollection collection, bool setAsActiveCollection = true)
        {
            AddTileCollectionWithResult(collection, setAsActiveCollection);
        }
        public AddTileCollectionResult AddTileCollectionWithResult(TileCollection collection, bool setAsActiveCollection = true)
        {
            if (collection == null || collection.TileSetCount <= 0) return AddTileCollectionResult.EmptyCollection;

#if BULKOUT_ENV // Paid Asset Integration https://assetstore.unity.com/packages/tools/modeling/runtime-level-design-52325
            var lib = rtLib.GetLib(collection.name);
            if (lib != null) 
            {
                if (setAsActiveCollection) rtLib.SetActiveLib(lib);
                return AddTileCollectionResult.CollectionAlreadyPresent;
            }
            lib = rtLib.CreateLib(collection.name);
            if (lib == null) return AddTileCollectionResult.FailedToCreateLib;
#else
            return AddTileCollectionResult.NoPrefabLoaderFound;
#endif

            swole.Log($"Loading Tile Collection '{collection.ID}'");

            for (int a = 0; a < collection.TileSetCount; a++)
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

                    AddCollectionPrefab(lib.Name, prefabObj); 

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

#if BULKOUT_ENV
            if (setAsActiveCollection) rtLib.SetActiveLib(lib);
#endif

            libSources[lib] = new PrefabCollectionSource() { id = collection.ID, isPackage = false };
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

        public void AddPackageContent(string packageIdentityString, bool forceReload = true, bool setAsActiveCollection = true) => AddPackageContent(new PackageIdentifier(packageIdentityString), forceReload, setAsActiveCollection);
        public void AddPackageContent(PackageIdentifier packageIdentity, bool forceReload = true, bool setAsActiveCollection = true) => AddPackageContent(packageIdentity.name, packageIdentity.version, forceReload, setAsActiveCollection);
        public void AddPackageContent(string packageName, string packageVersion, bool forceReload = true, bool setAsActiveCollection = true) => AddPackageContentWithResult(packageName, packageVersion, forceReload, setAsActiveCollection);
        public void AddPackageContent(ContentPackage package, bool forceReload = true, bool setAsActiveCollection = true) => AddPackageContentWithResult(package, forceReload, setAsActiveCollection);
        

        public AddPackageContentResult AddPackageContentWithResult(string packageIdentityString, bool forceReload = true, bool setAsActiveCollection = true) => AddPackageContentWithResult(new PackageIdentifier(packageIdentityString), forceReload, setAsActiveCollection);
        public AddPackageContentResult AddPackageContentWithResult(PackageIdentifier packageIdentity, bool forceReload = true, bool setAsActiveCollection = true) => AddPackageContentWithResult(packageIdentity.name, packageIdentity.version, forceReload, setAsActiveCollection);
        public AddPackageContentResult AddPackageContentWithResult(string packageName, string packageVersion, bool forceReload = true, bool setAsActiveCollection = true) => AddPackageContentWithResult(ContentManager.FindPackage(packageName, packageVersion), forceReload, setAsActiveCollection);
        public AddPackageContentResult AddPackageContentWithResult(ContentPackage package, bool forceReload = true, bool setAsActiveCollection = true)
        {
            if (package == null || package.ContentCount == 0) return AddPackageContentResult.EmptyPackage;

#if BULKOUT_ENV // Paid Asset Integration https://assetstore.unity.com/packages/tools/modeling/runtime-level-design-52325
            var lib = rtLib.GetLib(package.GetIdentityString());
            if (lib != null) 
            { 
                if (!forceReload) 
                {
                    if (setAsActiveCollection) rtLib.SetActiveLib(lib);
                    return AddPackageContentResult.PackageAlreadyPresent;
                }
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
                    prefabObj = CreationBehaviour.CreatePreRuntimeCreationPrefab(creation, false, false); 
                    if (prefabObj == null) continue;

                    var prototype = prefabObj.AddOrGetComponent<CreationPrototype>();
                    prototype.asset = creation;
                } 
                else continue;

                if (prefabObj == null) continue;

                AddCollectionPrefab(lib.Name, prefabObj);

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

#if BULKOUT_ENV
            if (setAsActiveCollection) rtLib.SetActiveLib(lib); 
#endif

            libSources[lib] = new PrefabCollectionSource() { id = package.GetIdentityString(), isPackage = true };  
            return AddPackageContentResult.Success;

        }

    }

}

#endif
