#if UNITY_2017_1_OR_NEWER

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEngine;

namespace Swole
{

    // Created By: Bunny83 (https://discussions.unity.com/u/bunny83)
    // Source: https://discussions.unity.com/t/recover-path-information-for-an-asset-but-at-runtime/157894/2
    /*
        This script is a scriptableobject which is ment to be stored as asset into a resources folder. It adds a menu item to Unity under “Tools” which allows you to create / update the resource database file. It’s automatically created under Assets/Resources/ResourceDB.asset. If you select that file in Unity it should show the custom inspector for this class. There you can see what information is actually stored and it allows you to initiate a manual update or enable automatic updating. When automatic updating is enabled, the integrated AssetPostprocessor will check all assets which got modified (added, moved, deleted) and if they belong to a resourced folder, the postprocessor will trigger an update.

        This script will store the following information for each file inside resources folders:

            The relative path to that asset. This is the same path that is needed in Resources.Load.
            The filename without extension. Again that is needed when you want to use Resources.Load.
            It stores the extension seperately. The extension isn’t used for anything, it’s just there if you need that information.
            It also stores the actual assettype. This is done when the database is updated. It stores the System.Type assembly qualified name of the type. For example this is the type string for the Material class: UnityEngine.Material, UnityEngine, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null. From this string we can reconstruct the actual System.Type object.

        Initially i thought about storing the exact file structure of the resources folders in a custom class hierarchy. However since Unity has a max nesting level of 7 it would limit the user to 7 nested subfolders inside a resources folder. Even almost nobody will reach this depth i like it as robust as possible. That’s why all file information is stored in a flat “master file table” (just like NTFS works). I still rebuild the hierarchy structure at runtime for easier usage.

        The ResourceDB uses a custom class called “ResourceItem”. A ResourceItem can represent both: an actual asset file or a subfolder from a resources folder. The virtual hierarchy is build up with this class. This class has several methods to navigate the hierarchy or to search for a certain asset(s)

            ResourceDB.GetFolder(path) this method return a single ResourceItem for the given relative path. So if you have a folder like this: Assets/Resources/sub1/sub2/folder you can pass the string "sub1/sub2/folder" and you get the ResourceItem for “folder”.
            ResourceDB.GetAllAssets(name, [type]) this method returns an enumeration of multiple ResourceItems which match the given parameters. “name” has to be the exact asset name. It doesn’t support partial names or wildcards but when you pass an empty string it will match any asset. The second optional type parameter allows you to specify a certain asset type. If this parameter is null any asset will match. The type parameter supports inheritance. So passing typeof(Texture) will match any texture asset (Texture2D, RenderTexture, …).

        Besides those static methods of the ResourceDB class, the ResourceItem class also has similar functions:

            GetChild(path, [rType]) This method allows you to access any child of the current ResourceItem. That ResourceItem has to represent a folder. When used on an asset resource it will return null. “path” is again a relative path which can be used to access deeper nested resources. The optional parameter “rType” allows you to specify if you only want assets, folders or both types.
            GetChilds(name, [rType [, sub [, type]]]) This method works similar to the GetAllAssets method above, however relative to the current folder and allows some additional settings. “name” again has to either match the actual filename or has to be an empty string to match any file / folder. With rType you can again specify if you want assets, folders or both. “sub” is a boolean which specifies if the method should only search in the current folder (false, default) or if it should include all sub folders (true). “type” is again a System,Type to filter assets for a certain type.
            Load() This method allows you to directly load the resource represented by this ResourceItem. It simply uses the “ResourcesPath” property of the ResourceItem which returns the complete assetpath needed for Resources.Load.

        GetAllAssets as well as GetChilds return an IEnumerable<ResourceItem>. This collection can either be iterated with a foreach loop, or converted into a List / array by using Linq.

        The ResourceDB class is a singleton which loads itself from the Resources folder. So if you have created an ResourceDB asset in the editor you can use it anywhere in the project.
     */

    // Edited By Nox

    [System.Serializable]
    public class ResourceItem : IResourceItemMetaData
    {
        public enum Type
        {
            Unknown = 0,
            Any = 0,
            Folder = 1,
            Asset = 2,
        }

        [SerializeField]
        private string name;
        [SerializeField]
        private string ext;
        [SerializeField]
        private string path;
        [SerializeField]
        private Type type = Type.Unknown;
        [SerializeField]
        private string objectTypeName;
        private System.Type objectType;
        private ResourceItem parent = null;
        internal Dictionary<string, ResourceItem> childs = null;

        public string Name { get { return name; } }
        public string Ext { get { return ext; } }
        public string Path { get { return path; } }
        public string ResourcesPath { get { return string.IsNullOrWhiteSpace(path) ? name : path + "/" + name; } }
        public Type ResourcesType { get { return type; } }
        public ResourceItem Parent { get { return parent; } }

        public ResourceItem()
        {
            if (type == Type.Folder)
                childs = new Dictionary<string, ResourceItem>();
        }
        public ResourceItem(string aFileName, string aPath, Type aType, string aObjectType, IResourceItemMetaData metadata = null)
        {
            var index = aFileName.LastIndexOf(".");
            if (index > 0)
            {
                name = aFileName.Substring(0, index);
                ext = aFileName.Substring(index + 1);
            }
            else
            {
                name = aFileName;
                ext = "";
            }
            path = aPath;
            type = aType;
            objectTypeName = aObjectType;
            objectType = System.Type.GetType(objectTypeName);
            if (type == Type.Folder)
                childs = new Dictionary<string, ResourceItem>();

            if (metadata != null)
            {
                this.metadata = metadata.ToArray();
            }
        }
        public ResourceItem GetChild(string aPath, Type aResourceType = Type.Any)
        {
            if (type != Type.Folder) // only folders have children
                return null;
            string p = aPath;
            int index = aPath.IndexOf('/');
            if (index > 0)
            {
                p = aPath.Substring(0, index);
                aPath = aPath.Substring(index + 1);
            }
            else
                aPath = string.Empty;

            ResourceItem item = null;
            if (!childs.TryGetValue(p, out item) || item == null)
                return null;
            if (aPath.Length > 0)
                return item.GetChild(aPath, aResourceType);
            if (aResourceType != Type.Unknown && item.type != aResourceType)
                return null;
            return item;
        }
        public IEnumerable<ResourceItem> GetChilds(string aName, Type aResourceType = Type.Any, bool aSearchSubFolders = false, System.Type aAssetType = null)
        {
            if (type == Type.Asset) // assets don't have children
                yield break;
            bool checkName = !string.IsNullOrWhiteSpace(aName);
            bool typeCheck = aAssetType != null;
            var items = childs.Values;
            foreach (var item in items)
            {
                if (aResourceType != Type.Any && item.type != aResourceType)
                    continue;
                if (checkName && aName != item.Name)
                    continue;
                if (typeCheck && !aAssetType.IsAssignableFrom(item.objectType))
                    continue;
                yield return item;
            }
            if (aSearchSubFolders)
            {
                foreach (var folder in items.Where(i => i.type == Type.Folder))
                {
                    foreach (var item in folder.GetChilds(aName, aResourceType, aSearchSubFolders, aAssetType))
                        yield return item;
                }
            }
        }

        public T Load<T>(bool verbose) where T : UnityEngine.Object
        {
            return verbose ? LoadVerbose<T>() : Load<T>();
        }
        public T LoadVerbose<T>() where T : UnityEngine.Object
        {
            var asset = Load<T>();
            if (asset == null) Debug.LogError($"[{nameof(ResourceDB)}] Failed to load resource with path '{ResourcesPath}' and expected type {typeof(T).Name}"); else Debug.Log($"[{nameof(ResourceDB)}] Loaded: " + ResourcesPath + ":" + typeof(T).Name);

            return asset;
        }
        public T Load<T>() where T : UnityEngine.Object
        {
            var path = ResourcesPath;
            if (string.IsNullOrWhiteSpace(path)) return null;

            if (ResourceDB.m_Instance.fullyLoadedItems.TryGetValue(this, out var loadedObject) && loadedObject != null)
            {
                if (typeof(T) != loadedObject.GetType())
                {
                    Resources.UnloadAsset(loadedObject);
                } 
                else
                {
                    return (T)loadedObject;
                }
            }

            var loadedObjectT = Resources.Load<T>(path);
            ResourceDB.m_Instance.fullyLoadedItems[this] = loadedObject;
            return loadedObjectT;
        }
        public bool IsLoaded => ResourceDB.m_Instance.fullyLoadedItems.TryGetValue(this, out var loadedObject) && loadedObject != null;
         
        public bool Unload()
        {
            if (!ResourceDB.m_Instance.fullyLoadedItems.TryGetValue(this, out var loadedObject) || loadedObject == null) return false;

            Resources.UnloadAsset(loadedObject);
            return true;
        }

        internal void OnDeserialize()
        {
            if (string.IsNullOrEmpty(path))
                parent = ResourceDB.Instance.root;
            else
                parent = ResourceDB.GetFolder(path);
            if (parent != null)
            {
                string name_ = name;
                if (parent.childs.ContainsKey(name_))
                {
                    int n = 1;
                    name_ = $"{name} ({n})";
                    while (parent.childs.ContainsKey(name_)) 
                    { 
                        n++;
                        name_ = $"{name} ({n})"; 
                    }
                } 
                
                parent.childs.Add(name_, this);                             
            }
            if (type == Type.Folder)
            {
                childs = new Dictionary<string, ResourceItem>();
            }
            objectType = System.Type.GetType(objectTypeName);
        }

        #region Metadata

        [SerializeField]
        private MetadataPair[] metadata;

        public bool TryGetMetaData(string key, out string value) 
        {
            value = null;
            foreach (var pair in metadata) if (pair.key == key)
                {
                    value = pair.value;
                    return true;
                }

            return false;
        }
        public bool HasMetaData(string key) => TryGetMetaData(key, out _);
        public string GetMetaData(string key)
        {
            if (TryGetMetaData(key, out var value)) return value;
            return string.Empty;
        }

        public IEnumerable<MetadataPair> GetAllMetaData() 
        {
            if (metadata == null || metadata.Length <= 0) yield break;
            for(int i = 0; i < metadata.Length; i++) yield return metadata[i];
        }

        public IEnumerator<MetadataPair> GetEnumerator() => GetAllMetaData().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

    }

    public abstract class ScriptableObjectWithMetaData : ScriptableObject, IResourceItemMetaData
    {
        #region Metadata

        [Header("Metadata")]
        [SerializeField]
        private MetadataPair[] metadata;

        public bool TryGetMetaData(string key, out string value)
        {
            value = null;
            foreach (var pair in metadata) if (pair.key == key)
                {
                    value = pair.value;
                    return true;
                }

            return false;
        }
        public bool HasMetaData(string key) => TryGetMetaData(key, out _);
        public string GetMetaData(string key)
        {
            if (TryGetMetaData(key, out var value)) return value;
            return string.Empty;
        }

        public IEnumerable<MetadataPair> GetAllMetaData()
        {
            if (metadata == null || metadata.Length <= 0) yield break;
            for (int i = 0; i < metadata.Length; i++) yield return metadata[i];
        }

        public IEnumerator<MetadataPair> GetEnumerator() => GetAllMetaData().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }

    [System.Serializable]
    public struct MetadataPair
    {
        public string key;
        public string value;

        public MetadataPair(string key, string value)
        {
            this.key = key;
            this.value = value;
        }
    }

    public interface IResourceItemMetaData : IEnumerable<MetadataPair>
    {
        public bool TryGetMetaData(string key, out string value);
        public bool HasMetaData(string key);
        public string GetMetaData(string key);

        public IEnumerable<MetadataPair> GetAllMetaData();
    }

    public class ResourceDB : ScriptableObject, ISerializationCallbackReceiver
    {
        internal static ResourceDB m_Instance = null;
        public static ResourceDB FindInstance()
        {
            return Resources.Load<ResourceDB>("ResourceDB");
        }
        public ResourceDB()
        {
            m_Instance = this;
        }
        public static ResourceDB Instance
        {
            get
            {
                if (m_Instance != null)
                    return m_Instance;
                m_Instance = FindInstance();
                if (m_Instance != null)
                    return m_Instance;
                m_Instance = CreateInstance<ResourceDB>();
#if UNITY_EDITOR
                var resDir = new DirectoryInfo(Path.Combine(Application.dataPath, "Resources"));
                if (!resDir.Exists)
                    UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");
                UnityEditor.AssetDatabase.CreateAsset(m_Instance, "Assets/Resources/ResourceDB.asset");
                m_Instance = FindInstance();
#endif
                return m_Instance;
            }
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Tools/Update ResourceDB")]
        internal static void TriggerUpdate()
        {
            Instance.UpdateDB(); 
        }
#endif

        [SerializeField, Tooltip("Used by build preprocessor to know if the DB needs to be updated prior to build start.")]
        internal bool isOutdated;
        public bool IsOutdated => isOutdated;

        [SerializeField, HideInInspector, Tooltip("When the database becomes outdated should it print a warning to the console?")]
        internal bool notifyWhenOutdated = true;
        [System.Serializable]
        public enum NotificationType
        {
            LogMessage, LogWarning, LogError
        }
        [SerializeField, HideInInspector]
        internal NotificationType notificationType = NotificationType.LogWarning; 

        public void MarkAsOutdated()
        {
            isOutdated = true;
            if (notifyWhenOutdated)
            {
                switch (notificationType)
                {
                    case NotificationType.LogMessage:
                        Debug.Log($"[{nameof(ResourceDB)}] Database is no longer an accurate reflection of available Resources. Please manually update before entering play mode!");
                        break;

                    case NotificationType.LogWarning:
                        Debug.LogWarning($"[{nameof(ResourceDB)}] Database is no longer an accurate reflection of available Resources. Please manually update before entering play mode!");
                        break;

                    case NotificationType.LogError:
                        Debug.LogError($"[{nameof(ResourceDB)}] Database is no longer an accurate reflection of available Resources. Please manually update before entering play mode!");
                        break;
                }
            }
        }

        [SerializeField]
        internal List<ResourceItem> items = new List<ResourceItem>();
        [SerializeField, HideInInspector]
        private int m_FileCount = 0;
        [SerializeField, HideInInspector]
        private int m_FolderCount = 0;
        [SerializeField, HideInInspector]
        public bool UpdateAutomatically = false;
        internal ResourceItem root = new ResourceItem("", "", ResourceItem.Type.Folder, "");
        public int FileCount { get { return m_FileCount; } }
        public int FolderCount { get { return m_FolderCount; } }

        public static ResourceItem GetFolder(string aPath)
        {
            return Instance.root.GetChild(aPath, ResourceItem.Type.Folder);
        }
        public static IEnumerable<ResourceItem> GetAllShallowAssets(System.Type aAssetType)
        {
            return GetAllShallowAssets(null, aAssetType);
        }
        public static IEnumerable<ResourceItem> GetAllShallowAssets(string aName, System.Type aAssetType = null)
        {
            return Instance.root.GetChilds(aName, ResourceItem.Type.Asset, true, aAssetType);
        }
        public static IEnumerable<ResourceItem> GetAllShallowAssets<T>(string aName = null) where T : UnityEngine.Object
        {
            return GetAllShallowAssets(aName, typeof(T));
        }
        public static IEnumerable<T> GetAllAssets<T>(string aName = null) where T : UnityEngine.Object
        {
            foreach (var item in GetAllShallowAssets(aName, typeof(T))) if (item != null) yield return item.Load<T>();
        }
        public static ResourceItem GetShallowAsset(string aName, System.Type aAssetType = null)
        {
            return Instance.root.GetChilds(aName, ResourceItem.Type.Asset, true, aAssetType).FirstOrDefault();
        }
        public static T GetAsset<T>(string aName) where T : UnityEngine.Object
        {
            var item = GetShallowAsset(aName, typeof(T));
            if (item != null) return item.Load<T>();

            return null;
        }

        internal readonly Dictionary<ResourceItem, UnityEngine.Object> fullyLoadedItems = new Dictionary<ResourceItem, UnityEngine.Object>();
        internal static readonly List<ResourceItem> tempItems = new List<ResourceItem>();

        public static void UnloadAll(string aPath = null)
        {
            if (Instance == null) return;

            tempItems.Clear();
            if (string.IsNullOrWhiteSpace(aPath))
            {
                tempItems.AddRange(m_Instance.fullyLoadedItems.Keys);
                foreach (var item in tempItems) if (tempItems != null) item.Unload(); 
                m_Instance.fullyLoadedItems.Clear();
            } 
            else
            {
                var folder = GetFolder(aPath);
                if (folder != null)
                {
                    tempItems.AddRange(m_Instance.fullyLoadedItems.Keys);
                    foreach (var item in tempItems) if (tempItems != null)
                        {
                            item.Unload();
                            m_Instance.fullyLoadedItems.Remove(item);
                        }
                }
            }
            tempItems.Clear();
        }

        public static string ConvertPath(string aPath)
        {
            return aPath.Replace("\\", "/");
        }

#if UNITY_EDITOR
        void ScanFolder(DirectoryInfo aFolder, List<DirectoryInfo> aList, bool aOnlyTopFolders)
        {
            string n = aFolder.Name.ToLower();
            if (n == "editor") // ignore folders
                return;
            if (n == "resources")
            {
                aList.Add(aFolder);
                if (aOnlyTopFolders)
                    return;
            }
            foreach (var dir in aFolder.GetDirectories())
            {
                ScanFolder(dir, aList, aOnlyTopFolders);
            }
        }
        List<DirectoryInfo> FindResourcesFolders(bool aOnlyTopFolders)
        {
            var assets = new DirectoryInfo(Application.dataPath);
            var list = new List<DirectoryInfo>();
            ScanFolder(assets, list, aOnlyTopFolders);
            return list;
        }

        void AddFileList(DirectoryInfo aFolder, int aPrefix)
        {
            string relFolder = aFolder.FullName;
            if (relFolder.Length < aPrefix)
                relFolder = "";
            else
                relFolder = relFolder.Substring(aPrefix);
            relFolder = ConvertPath(relFolder);
            foreach (var folder in aFolder.GetDirectories())
            {
                items.Add(new ResourceItem(folder.Name, relFolder, ResourceItem.Type.Folder, ""));
                AddFileList(folder, aPrefix);
            }

            foreach (var file in aFolder.GetFiles())
            {
                string ext = file.Extension.ToLower();
                if (ext == ".meta")
                    continue;
                string assetPath = "assets/" + file.FullName.Substring(Application.dataPath.Length + 1);
                assetPath = ConvertPath(assetPath);
                var obj = UnityEditor.AssetDatabase.LoadAssetAtPath(assetPath, typeof(UnityEngine.Object));
                if (obj == null)
                {
                    Debug.LogWarning("ResourceDB: File at path " + assetPath + " couldn't be loaded and is ignored. Probably not an asset?!");
                    continue;
                }
                string type = obj.GetType().AssemblyQualifiedName;
                items.Add(new ResourceItem(file.Name, relFolder, ResourceItem.Type.Asset, type, obj as IResourceItemMetaData));
            }
            Resources.UnloadUnusedAssets();
        }

        public void UpdateDB(bool aSetDirty = false)
        {
            items.Clear();
            root.childs.Clear();
            var topFolders = FindResourcesFolders(true);

            foreach (var folder in topFolders)
            {
                string path = folder.FullName;
                int prefix = path.Length;
                if (!path.EndsWith("/"))
                    prefix++;
                AddFileList(folder, prefix);
            }
            m_FolderCount = 0;
            m_FileCount = 0;
            foreach (var item in items)
            {
                if (item.ResourcesType == ResourceItem.Type.Folder)
                    m_FolderCount++;
                else if (item.ResourcesType == ResourceItem.Type.Asset)
                    m_FileCount++;
            }

            isOutdated = false;

            if (aSetDirty)
            {
                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.AssetDatabase.SaveAssets();
            }
        }
#endif

        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            if (items == null || items.Count == 0)
            {
                UpdateDB();
            }
#endif
        }

        public void OnAfterDeserialize()
        {
            root.childs.Clear();
            foreach (var item in items)
            {
                if (item != null)
                    item.OnDeserialize();
            }
        }
    }


#if UNITY_EDITOR

    public class ResourceDBPostprocessor : UnityEditor.AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (ResourceDB.FindInstance() == null)
                return;
            var files = importedAssets.Concat(deletedAssets).Concat(movedAssets).Concat(movedFromAssetPaths);
            bool update = false;
            foreach (var file in files)
            {
                var fn = file.ToLower();
                if (!fn.Contains("resourcedb.asset") && fn.Contains("/resources/"))
                {
                    update = true; 
                    break;
                }
            }

            if (update)
            {
                if (ResourceDB.Instance.UpdateAutomatically)
                {
                    ResourceDB.Instance.UpdateDB();
                } 
                else
                {
                    ResourceDB.Instance.MarkAsOutdated();
                }                    
            }
        }
    }
#endif

}

#endif