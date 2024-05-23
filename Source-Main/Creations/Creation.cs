using System;
using System.Collections;
using System.Collections.Generic;

using static Swole.EngineInternal;

namespace Swole
{

    public class Creation : SwoleObject<Creation, Creation.Serialized>, IContent
    {

        #region Serialization

        public override string AsJSON(bool prettyPrint = false) => AsSerializableStruct().AsJSON(prettyPrint);

        [Serializable]
        public struct Serialized : ISerializableContainer<Creation, Creation.Serialized>
        {

            public ContentInfo contentInfo;
            public int previewTextureWidth, previewTextureHeight;
            public int previewTextureFormat;
            public byte[] previewTexture;
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

            try
            {
                if (creation.previewTexture != null)
                {
#if BULKOUT_ENV
                    if (creation.previewTexture is UnityEngine.Texture2D tex2D)
                    {
                        s.previewTextureWidth = tex2D.width;
                        s.previewTextureHeight = tex2D.height;
                        s.previewTextureFormat = (int)tex2D.format;
                        s.previewTexture = tex2D.GetRawTextureData();
                    }

#endif
                }
            }
            catch(Exception ex)
            {
                swole.LogError($"[{nameof(Creation.Serialized)}] Encountered exception while serializing preview texture for creation '{creation.Name}'");
                swole.LogError(ex);
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

            try
            {
                if (serializable.previewTexture != null)
                {
#if BULKOUT_ENV
                    UnityEngine.Texture2D tex2D = new UnityEngine.Texture2D(serializable.previewTextureWidth, serializable.previewTextureHeight, (UnityEngine.TextureFormat)serializable.previewTextureFormat, false);
                    tex2D.LoadRawTextureData(serializable.previewTexture);
                    tex2D.Apply();
#else
#endif
                }
            } 
            catch(Exception ex)
            {
                swole.LogError($"[{nameof(Creation)}] Encountered exception while deserializing preview texture for creation '{serializable.contentInfo.name}'");
                swole.LogError(ex);
            }

            this.script = serializable.script;

            if (serializable.spawnGroups != null)
            {
                this.spawnGroups = new ObjectSpawnGroup[serializable.spawnGroups.Length];
                for (int a = 0; a < serializable.spawnGroups.Length; a++) this.spawnGroups[a] = serializable.spawnGroups[a].AsOriginalType(packageInfo);
            }

        }

        #endregion

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
                    if (group is CreationSpawnGroup csg) dependencies.Add(csg.PackageIdentity);              
                }
            }
            dependencies = script.ExtractPackageDependencies(dependencies);

            return dependencies;
        }

        public Creation(string name, string author, DateTime creationDate, DateTime lastEditDate, string description, CreationScript script, ICollection<ObjectSpawnGroup> spawnGroups = null, PackageInfo packageInfo = default) : this(new ContentInfo() { name = name, author = author, creationDate = creationDate.ToString(IContent.dateFormat), lastEditDate = lastEditDate.ToString(IContent.dateFormat), description = description }, script, spawnGroups, packageInfo) { }

        public Creation(string name, string author, string creationDate, string lastEditDate, string description, CreationScript script, ICollection<ObjectSpawnGroup> spawnGroups = null, PackageInfo packageInfo = default) : this(new ContentInfo() { name = name, author = author, creationDate = creationDate, lastEditDate = lastEditDate, description = description }, script, spawnGroups, packageInfo) { }

        public Creation(ContentInfo contentInfo, CreationScript script, ICollection<ObjectSpawnGroup> spawnGroups = null, PackageInfo packageInfo = default) : base(default)
        {
            this.packageInfo = packageInfo;
            this.contentInfo = contentInfo;

            this.script = script;

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
        public string Author => contentInfo.author;
        public string CreationDate => contentInfo.creationDate;
        public string LastEditDate => contentInfo.lastEditDate;
        public string Description => contentInfo.description;

        protected readonly PackageInfo packageInfo;
        protected readonly ContentInfo contentInfo;

        protected object previewTexture;
        public object PreviewTextureObject => previewTexture;
#if BULKOUT_ENV
        public UnityEngine.Texture2D PreviewTexture 
        { 
            get
            {
                if (previewTexture is UnityEngine.Texture2D texture) return texture;
                return Swole.API.Unity.ResourceLib.DefaultPreviewTexture_Creation;
            }
        }
#endif

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

            return root;

        }

        public IContent CreateCopyAndReplaceContentInfo(ContentInfo info) => new Creation(info, script, spawnGroups, packageInfo);

        public bool HasIdenticalContentTo(Creation other)
        {

            if (!script.Equals(other.script)) return false;

            if (spawnGroups == null && other.spawnGroups != null) return false;
            if (spawnGroups != null && other.spawnGroups == null) return false;
            if (spawnGroups.Length != other.spawnGroups.Length) return false;



            return true;

        }

    }

}
