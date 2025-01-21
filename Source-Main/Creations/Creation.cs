#if (UNITY_STANDALONE || UNITY_EDITOR)
#define IS_UNITY
#endif

using System;
using System.Collections;
using System.Collections.Generic;

using Swole.UI;

#if IS_UNITY
using Swole.API.Unity;
#endif

using static Swole.EngineInternal;

namespace Swole
{

    public class Creation : SwoleObject<Creation, Creation.Serialized>, IContent
    {

        protected static readonly List<ICurve> _tempCurves = new List<ICurve>();
#if IS_UNITY
        protected static readonly List<SerializedAnimationCurve> _tempSerializedAnimationCurves = new List<SerializedAnimationCurve>();
#endif
        protected static readonly List<SerializedCurve> _tempSerializedCurves = new List<SerializedCurve>(); 

        public Type AssetType => GetType();
        public object Asset => this;

        public bool IsInternalAsset { get => false; set { } }

        protected bool invalid;
        public bool IsValid => !invalid;
        public void Dispose() 
        { 
            if (previewTextureIsLocal && previewTexture != null)
            {
                swole.Engine.Object_AdminDestroy(previewTexture);  
            }
            previewTexture = null;
            invalid = true;
        }
        public void Delete() => Dispose(); 

        #region Serialization

        public override string AsJSON(bool prettyPrint = false) => AsSerializableStruct().AsJSON(prettyPrint);

        [Serializable]
        public struct Serialized : ISerializableContainer<Creation, Creation.Serialized>
        {

            public ContentInfo contentInfo;
            public string SerializedName => contentInfo.name;
            public string previewTexturePath;
            public int previewTextureWidth, previewTextureHeight;
            public int previewTextureFormat;
            public string encodedPreviewTextureData;

            public ProjectSettings projectSettings; 

            public SerializedCurve[] curves;
#if IS_UNITY
            public SerializedAnimationCurve[] animationCurves;
#endif

            public CreationScript script; 
            public ObjectSpawnGroup.Serialized[] spawnGroups;

            public Creation AsOriginalType(PackageInfo packageInfo = default) => new Creation(this, packageInfo);
            public string AsJSON(bool prettyPrint = false) => swole.Engine.ToJson(this, prettyPrint);

            public object AsNonserializableObject(PackageInfo packageInfo = default) => AsOriginalType(packageInfo);
        }

        public static implicit operator Serialized(Creation creation)
        {
            Serialized s = new Serialized();

            s.contentInfo = creation.contentInfo;

            s.projectSettings = creation.projectSettings;

            _tempSerializedCurves.Clear();
#if IS_UNITY
            _tempSerializedAnimationCurves.Clear();
#endif
            for(int a = 0; a < creation.CurveCount; a++)
            {
                var curve = creation.GetCurve(a);
                if (curve == null) continue;

                var serializedCurve = curve.Serialize();
#if IS_UNITY
                if (serializedCurve is SerializedAnimationCurve sac)
                {
                    _tempSerializedAnimationCurves.Add(sac);
                    continue;
                }
#endif
                if (serializedCurve is SerializedCurve sc)
                {
                    _tempSerializedCurves.Add(sc); 
                }
            }
            s.curves = _tempSerializedCurves.ToArray();
            _tempSerializedCurves.Clear();
#if IS_UNITY
            s.animationCurves = _tempSerializedAnimationCurves.ToArray();
            _tempSerializedAnimationCurves.Clear(); 
#endif

            s.previewTexturePath = creation.previewTexturePath;
            if (string.IsNullOrWhiteSpace(creation.previewTexturePath))
            {
                try
                {
                    if (creation.previewTexture != null)
                    {
#if IS_UNITY
                        if (creation.previewTexture is IImageAsset asset && asset.Instance is UnityEngine.Texture2D tex2D)
                        {
                            s.previewTextureWidth = tex2D.width;
                            s.previewTextureHeight = tex2D.height;
                            s.previewTextureFormat = (int)tex2D.format;
                            s.encodedPreviewTextureData = Convert.ToBase64String(tex2D.GetRawTextureData());
                        }

#endif
                    }
                }
                catch (Exception ex)
                {
                    swole.LogError($"[{nameof(Creation.Serialized)}] Encountered exception while serializing preview texture for creation '{creation.Name}'");
                    swole.LogError(ex);
                }
            }

            s.script = creation.script;
            if (creation.spawnGroups != null)
            {
                s.spawnGroups = new ObjectSpawnGroup.Serialized[creation.spawnGroups.Length];
                for (int a = 0; a < creation.spawnGroups.Length; a++) s.spawnGroups[a] = creation.spawnGroups[a].AsSerializableStruct();
            }

            return s;
        }

        public override Creation.Serialized AsSerializableStruct() => this;

        public Creation(Creation.Serialized serializable, PackageInfo packageInfo = default) : base(serializable)
        {

            this.packageInfo = packageInfo;

            this.contentInfo = serializable.contentInfo;

            this.projectSettings = serializable.projectSettings;

            _tempCurves.Clear();
            if (serializable.curves != null)
            {
                for (int a = 0; a < serializable.curves.Length; a++) _tempCurves.Add(serializable.curves[a].AsOriginalType(packageInfo));
                
            }
#if IS_UNITY
            if (serializable.animationCurves != null)
            {
                for(int a = 0; a < serializable.animationCurves.Length; a++) _tempCurves.Add(serializable.animationCurves[a].AsEditableAnimationCurve(packageInfo)); 
            }
#endif
            this.curves = _tempCurves.ToArray(); 
            _tempCurves.Clear();

            this.previewTexturePath = serializable.previewTexturePath;
            if (string.IsNullOrWhiteSpace(serializable.previewTexturePath))
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(serializable.encodedPreviewTextureData))
                    {
                        previewTextureIsLocal = true;
#if IS_UNITY
                        UnityEngine.Texture2D tex2D = new UnityEngine.Texture2D(serializable.previewTextureWidth, serializable.previewTextureHeight, (UnityEngine.TextureFormat)serializable.previewTextureFormat, false);
                        tex2D.LoadRawTextureData(Convert.FromBase64String(serializable.encodedPreviewTextureData));
                        tex2D.Apply(); 

                        this.previewTexture = new Swole.API.Unity.ImageAsset(this.contentInfo.name + "_preview", this.contentInfo.author, this.contentInfo.creationDate, this.contentInfo.lastEditDate, string.Empty, tex2D, ExternalAssets.CanTextureBeCompressed(serializable.previewTextureWidth, serializable.previewTextureHeight), packageInfo);
#else
#endif
                    }
                }
                catch (Exception ex)
                {
                    swole.LogError($"[{nameof(Creation)}] Encountered exception while deserializing preview texture for creation '{serializable.contentInfo.name}'");
                    swole.LogError(ex);
                }
            }

            this.script = serializable.script;

            if (serializable.spawnGroups != null)
            {
                this.spawnGroups = new ObjectSpawnGroup[serializable.spawnGroups.Length];
                for (int a = 0; a < serializable.spawnGroups.Length; a++) this.spawnGroups[a] = serializable.spawnGroups[a].AsOriginalType(packageInfo);
            }

        }

        #endregion

        [Serializable]
        public struct ObjectPreloadState
        {
            public int id;
            public bool showChildren;
            public bool isHidden;
        }
        [Serializable]
        public struct ProjectSettings
        {
            public EngineInternal.Vector3 cameraPosition;
            public EngineInternal.Quaternion cameraRotation;
            public int selectedCollection;
            public PrefabCollectionSource[] importedCollections;
            public int transformGizmoState;
            public int[] selectedObjects;
            public ObjectPreloadState[] objectStates;
            public ResizableWindowState[] windowStates;
        }
        public ProjectSettings projectSettings;

        private string originPath;
        public string OriginPath => originPath;
        public IContent SetOriginPath(string path)
        {
            originPath = path;
            return this;
        }
        private string relativePath;
        public string RelativePath => relativePath; 
        public IContent SetRelativePath(string path)
        {
            relativePath = path;
            return this;
        }

        public List<PackageIdentifier> ExtractPackageDependencies(List<PackageIdentifier> dependencies = null)
        {
            if (dependencies == null) dependencies = new List<PackageIdentifier>();

            if (spawnGroups != null)
            { 
                foreach(var group in spawnGroups)
                {
                    if (group is CreationSpawnGroup csg) 
                    { 
                        dependencies.Add(csg.PackageIdentity); 
                    } 
                    else if (group is TileSpawnGroup tsg && tsg.IsPackageDependent)
                    {
                        dependencies.Add(tsg.TileCollectionId); 
                    }
                }
            }
            dependencies = script.ExtractPackageDependencies(dependencies);

            return dependencies;
        }

        public Creation(string name, string author, DateTime creationDate, DateTime lastEditDate, string description, CreationScript script, ICollection<ICurve> curves = null, ICollection<ObjectSpawnGroup> spawnGroups = null, string previewTexturePath = null, PackageInfo packageInfo = default) : this(new ContentInfo() { name = name, author = author, creationDate = creationDate.ToString(IContent.dateFormat), lastEditDate = lastEditDate.ToString(IContent.dateFormat), description = description }, script, curves, spawnGroups, previewTexturePath, packageInfo) { }

        public Creation(string name, string author, string creationDate, string lastEditDate, string description, CreationScript script, ICollection<ICurve> curves = null, ICollection<ObjectSpawnGroup> spawnGroups = null, string previewTexturePath = null, PackageInfo packageInfo = default) : this(new ContentInfo() { name = name, author = author, creationDate = creationDate, lastEditDate = lastEditDate, description = description }, script, curves, spawnGroups, previewTexturePath, packageInfo) { }

        public Creation(ContentInfo contentInfo, CreationScript script, ICollection<ICurve> curves = null, ICollection<ObjectSpawnGroup> spawnGroups = null, string previewTexturePath = null, PackageInfo packageInfo = default) : base(default)
        {
            this.packageInfo = packageInfo;
            this.contentInfo = contentInfo;

            this.previewTexturePath = previewTexturePath;

            this.script = script;

            if (curves != null)
            {
                this.curves = new ICurve[curves.Count];  
                int i = 0;
                foreach (var curve in curves)
                {
                    this.curves[i] = curve;
                    i++;
                }
            }

            if (spawnGroups != null)
            {
                this.spawnGroups = new ObjectSpawnGroup[spawnGroups.Count];  
                int i = 0;
                foreach (var spawnGroup in spawnGroups)
                {
                    this.spawnGroups[i] = spawnGroup;
                    i++;
                }
            }
        }

        public PackageInfo PackageInfo => packageInfo;
        public ContentInfo ContentInfo => contentInfo;
        public string Name => contentInfo.name;
        public override string SerializedName => Name;
        public string Author => contentInfo.author;
        public string CreationDate => contentInfo.creationDate;
        public string LastEditDate => contentInfo.lastEditDate;
        public string Description => contentInfo.description;

        protected readonly PackageInfo packageInfo;
        protected readonly ContentInfo contentInfo;

        public const int _defaultPreviewTextureSize = 256;
        protected bool previewTextureIsLocal;
        public string previewTexturePath; 
        protected object previewTexture;
        public object PreviewTextureObject => previewTexture;
        public IImageAsset LocalPreviewTexture
        {
            get
            {
                if (previewTexture == null && !string.IsNullOrWhiteSpace(previewTexturePath))
                {
                    if (ContentManager.TryFindPackage(packageInfo, out var pkg))
                    {
                        if (pkg.TryFindLoader<IImageAsset>(out var asset_, previewTexturePath)) previewTexture = asset_;
                    }
                }
                if (previewTexture is IImageAsset asset && asset.Instance != null) return asset;
                if (previewTexture is ContentLoader<IImageAsset> loader)
                {
                    asset = loader.Content; // Fully loads
                    if (asset != null && asset.Instance != null) return asset;
                }
                return null;
            }
        }
        public IImageAsset PreviewTexture 
        { 
            get
            {
                var local = LocalPreviewTexture;
                if (local != null) return local;
                return Swole.API.Unity.ResourceLib.DefaultPreviewTextureAsset_Creation;
            }
        }

        protected readonly ICurve[] curves;
        public int CurveCount => curves == null ? 0 : curves.Length;
        public ICurve GetCurve(int index) => curves == null || index < 0 || index >= curves.Length ? null : curves[index]; 
        public bool TryGetCurve(string name, out ICurve curve, bool caseSensitive = false)
        {
            curve = null;
            if (curves == null) return false;

            if (!caseSensitive) name = name.AsID();
            for(int a = 0; a < curves.Length; a++)
            {
                var c = curves[a];
                if (c == null || (caseSensitive ? c.Name != name : c.Name.AsID() != name)) continue;  

                curve = c;
                return true;
            }
            return false;;
        } 

        protected readonly CreationScript script;
        /// <summary>
        /// Contains all of the source code that is compiled and executed when a new copy of this Creation is spawned.
        /// </summary>
        public CreationScript Script => script;
        public bool HasScripting => !Script.IsEmpty;

        protected readonly ObjectSpawnGroup[] spawnGroups;
        public int SpawnGroupCount => spawnGroups == null ? 0 : spawnGroups.Length;
        public ObjectSpawnGroup GetSpawnGroup(int index) => spawnGroups == null || index < 0 || index >= spawnGroups.Length ? null : spawnGroups[index];

        public delegate void SpawnTilesDelegate(TileSpawnGroup tsg, EngineInternal.ITransform rootTransform, List<EngineInternal.TileInstance> tileList);
        public delegate void SpawnObjectsDelegate(ObjectSpawnGroup osg, EngineInternal.ITransform rootTransform, List<EngineInternal.ITransform> instanceList);
        public GameObject CreateNewRootAndObjects(bool useRealTransformsOnly, Vector3 position = default, Quaternion rotation = default, List<EngineInternal.ITransform> objectOutputList = null, List<EngineInternal.TileInstance> tileInstanceOutputList = null, SpawnTilesDelegate spawnTiles = null, SpawnObjectsDelegate spawnObjects = null)
        {
            if (rotation.IsZero) rotation = Quaternion.identity;

            GameObject root = GameObject.Create(Name);
            EngineInternal.ITransform rootTransform = root.transform;

            rootTransform.position = position;
            rootTransform.rotation = rotation;
            rootTransform.localScale = Vector3.one;

            CreateNewRootAndObjects(useRealTransformsOnly, rootTransform, objectOutputList, tileInstanceOutputList, spawnTiles, spawnObjects);
            return root;
        }    
        public void CreateNewRootAndObjects(bool useRealTransformsOnly, EngineInternal.ITransform rootTransform, List<EngineInternal.ITransform> objectOutputList = null, List<EngineInternal.TileInstance> tileInstanceOutputList = null, SpawnTilesDelegate spawnTiles = null, SpawnObjectsDelegate spawnObjects = null)
        {
            if (spawnGroups != null)
            { 

                List<EngineInternal.ITransform> _tempObjects = new List<EngineInternal.ITransform>();
                List<EngineInternal.TileInstance> _tempTiles = new List<EngineInternal.TileInstance>();
                List<int> _tempSpawnCounts = new List<int>();
                List<int> _tempSpawnIndices = new List<int>(); // These lists must be instantiated inside this function incase there is recursion happening from nested Creations

                foreach (var spawnGroup in spawnGroups)
                {
                    int startIndex = _tempObjects.Count;
                    if (spawnGroup is TileSpawnGroup tsg)
                    {
                        if (tileInstanceOutputList != null)
                        {
                            if (spawnTiles == null)
                            {
                                tsg.Spawn(rootTransform, useRealTransformsOnly, _tempTiles);
                            }
                            else
                            {
                                spawnTiles(tsg, rootTransform, _tempTiles);
                            }
                            tileInstanceOutputList.AddRange(_tempTiles);
                            for (int a = 0; a < _tempTiles.Count; a++) _tempObjects.Add(_tempTiles[a]);
                            _tempTiles.Clear();
                        }
                        else
                        {
                            if (spawnTiles == null)
                            {
                                tsg.SpawnIntoList(rootTransform, useRealTransformsOnly, _tempObjects);
                            }
                            else
                            {
                                spawnTiles(tsg, rootTransform, _tempTiles);
                                for (int a = 0; a < _tempTiles.Count; a++) _tempObjects.Add(_tempTiles[a]);
                                _tempTiles.Clear();
                            }
                        }
                    }
                    else
                    {
                        if (spawnObjects == null)
                        {
                            spawnGroup.SpawnIntoList(rootTransform, useRealTransformsOnly, _tempObjects);
                        }
                        else
                        {
                            spawnObjects(spawnGroup, rootTransform, _tempObjects);
                        }
                    }
                    _tempSpawnIndices.Add(startIndex);
                    _tempSpawnCounts.Add(_tempObjects.Count - startIndex);
                }

                for (int a = 0; a < spawnGroups.Length; a++) // Handle parenting after all objects are spawned
                {
                    var spawnGroup = spawnGroups[a];

                    int startIndex = _tempSpawnIndices[a];
                    int count = _tempSpawnCounts[a];

                    for (int b = 0; b < count; b++)
                    {
                        var spawner = spawnGroup.GetObjectSpawner(b);
                        if (spawner.parentInstanceIndex <= 0 || spawner.parentSpawnerIndex <= 0) continue;

                        int parentInstanceIndex = spawner.parentInstanceIndex - 1;
                        int parentSpawnerIndex = spawner.parentSpawnerIndex - 1;

                        if (parentSpawnerIndex < 0 || parentSpawnerIndex >= spawnGroups.Length) continue; 
                        //var parentSpawner = spawnGroups[parentSpawnerIndex];
                        int parentStartIndex = _tempSpawnIndices[parentSpawnerIndex];
                        int parentCount = _tempSpawnCounts[parentSpawnerIndex];
                        if (parentInstanceIndex < 0 || parentInstanceIndex >= parentCount) continue;

                        var obj = _tempObjects[startIndex + b];
                        var parentObj = _tempObjects[parentStartIndex + parentInstanceIndex];

                        obj.SetParent(parentObj, true);
                        obj.localScale = spawner.localScale; 

                    }
                }

                if (objectOutputList != null) objectOutputList.AddRange(_tempObjects); 
                _tempObjects.Clear();

            }
        }

        public IContent CreateCopyAndReplaceContentInfo(ContentInfo info) 
        { 
            var copy = new Creation(info, script, curves, spawnGroups, previewTexturePath, packageInfo);  
            copy.previewTexture = previewTexture;
            copy.projectSettings = projectSettings;
            return copy;
        }

        public bool HasIdenticalContentTo(Creation other)
        {

            if (!script.Equals(other.script)) return false;

            if (spawnGroups == null && other.spawnGroups != null) return false;
            if (spawnGroups != null && other.spawnGroups == null) return false;
            if (spawnGroups.Length != other.spawnGroups.Length) return false;

            // TODO: check spawn groups

            return true;

        }

    }

}
