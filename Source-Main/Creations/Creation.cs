using System;
using System.Collections;
using System.Collections.Generic;

using static Swole.EngineInternal;

namespace Swole
{

    [Serializable]
    public class Creation : IContent
    {

        public Creation(string name, string author, string creationDate, string lastEditDate, string description, CreationScript script, ICollection<TileSpawnGroup> tileSpawnGroups = null, PackageManifest packageInfo = default)
        {
            this.packageInfo = packageInfo;
            this.name = name;
            this.author = author;
            this.creationDate = creationDate;
            this.lastEditDate = lastEditDate;
            this.description = description;

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

        public PackageManifest PackageInfo => packageInfo;
        public string Name => name;
        public string Author => author;
        public string CreationDateString => creationDate;
        public string LastEditDateString => lastEditDate;
        public string Description => description;

        protected readonly PackageManifest packageInfo;
        protected readonly string name;
        protected readonly string author;
        protected readonly string creationDate;
        protected readonly string lastEditDate;
        protected readonly string description;

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
