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
            public CreationScript script; 
            public TileSpawnGroup.Serialized[] tileSpawnGroups;

            public Creation AsOriginalType(PackageInfo packageInfo = default) => new Creation(this, packageInfo);
            public string AsJSON(bool prettyPrint = false) => swole.Engine.ToJson(this, prettyPrint);

            public object AsNonserializableObject(PackageInfo packageInfo = default) => AsOriginalType(packageInfo);
        }

        public static implicit operator Serialized(Creation creation)
        {
            Serialized s = new Serialized();

            s.contentInfo = creation.contentInfo;
            s.script = creation.script;
            if (creation.tileSpawnGroups != null)
            {
                s.tileSpawnGroups = new TileSpawnGroup.Serialized[creation.tileSpawnGroups.Length];
                for (int a = 0; a < creation.tileSpawnGroups.Length; a++) s.tileSpawnGroups[a] = creation.tileSpawnGroups[a].AsSerializableStruct();
            }

            return s;
        }

        public override Creation.Serialized AsSerializableStruct() => this;

        public Creation(Creation.Serialized serializable, PackageInfo packageInfo = default) : base(serializable)
        {

            this.packageInfo = packageInfo;

            this.contentInfo = serializable.contentInfo;
            this.script = serializable.script;

            if (serializable.tileSpawnGroups != null)
            {
                this.tileSpawnGroups = new TileSpawnGroup[serializable.tileSpawnGroups.Length];
                for (int a = 0; a < serializable.tileSpawnGroups.Length; a++) this.tileSpawnGroups[a] = serializable.tileSpawnGroups[a].AsOriginalType(packageInfo);
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

            dependencies = script.ExtractPackageDependencies(dependencies);

            return dependencies;

        }

        public Creation(string name, string author, DateTime creationDate, DateTime lastEditDate, string description, CreationScript script, ICollection<TileSpawnGroup> tileSpawnGroups = null, PackageInfo packageInfo = default) : this(new ContentInfo() { name = name, author = author, creationDate = creationDate.ToString(IContent.dateFormat), lastEditDate = lastEditDate.ToString(IContent.dateFormat), description = description }, script, tileSpawnGroups, packageInfo) { }

        public Creation(string name, string author, string creationDate, string lastEditDate, string description, CreationScript script, ICollection<TileSpawnGroup> tileSpawnGroups = null, PackageInfo packageInfo = default) : this(new ContentInfo() { name = name, author = author, creationDate = creationDate, lastEditDate = lastEditDate, description = description }, script, tileSpawnGroups, packageInfo) { }

        public Creation(ContentInfo contentInfo, CreationScript script, ICollection<TileSpawnGroup> tileSpawnGroups = null, PackageInfo packageInfo = default) : base(default)
        {
            this.packageInfo = packageInfo;
            this.contentInfo = contentInfo;

            this.script = script;

            if (tileSpawnGroups != null)
            {
                this.tileSpawnGroups = new TileSpawnGroup[tileSpawnGroups.Count];
                int i = 0;
                foreach (var spawnGroup in tileSpawnGroups)
                {
                    this.tileSpawnGroups[i] = spawnGroup;
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

        protected readonly CreationScript script;
        /// <summary>
        /// Contains all of the source code that is compiled and executed when a new copy of this Creation is spawned.
        /// </summary>
        public CreationScript Script => script;
        public bool HasScripting => !Script.IsEmpty;

        protected readonly TileSpawnGroup[] tileSpawnGroups;

        public GameObject CreateNewInstance(Vector3 position = default, Quaternion rotation = default, List<EngineInternal.TileInstance> tileInstanceOutputList = null)
        {

            if (rotation.x == 0 && rotation.y == 0 && rotation.z == 0 && rotation.w == 0) rotation = Quaternion.identity;

            GameObject root = GameObject.Create(Name);
            Transform rootTransform = root.transform;

            rootTransform.position = position;
            rootTransform.rotation = rotation;
            rootTransform.localScale = Vector3.one;

            if (tileSpawnGroups != null)
            {

                foreach (var spawnGroup in tileSpawnGroups) spawnGroup.Spawn(rootTransform, tileInstanceOutputList);

            }

            return root;

        }

    }

}
