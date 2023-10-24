using System;
using System.Collections.Generic;

using static Swole.EngineInternal;

namespace Swole
{

    public class TileSpawnGroup : SwoleObject<TileSpawnGroup, TileSpawnGroup.Serialized>
    {

        #region Serialization

        public override string AsJSON(bool prettyPrint = false) => AsSerializableStruct().AsJSON(prettyPrint);

        [Serializable]
        public struct Serialized : ISerializableContainer<TileSpawnGroup, TileSpawnGroup.Serialized>
        {

            public string tileSetName;
            public TileSpawner[] tileSpawns;

            public TileSpawnGroup AsOriginalType(PackageInfo packageInfo = default) => new TileSpawnGroup(this);
            public string AsJSON(bool prettyPrint = false) => swole.Engine.ToJson(this, prettyPrint);

            public object AsNonserializableObject(PackageInfo packageInfo = default) => AsOriginalType(packageInfo);
        }

        public static implicit operator Serialized(TileSpawnGroup obj) => new TileSpawnGroup.Serialized() { tileSetName = obj.tileSetName, tileSpawns = obj.tileSpawns };

        public override TileSpawnGroup.Serialized AsSerializableStruct() => this;

        public TileSpawnGroup(TileSpawnGroup.Serialized serializable) : base(serializable)
        {

            this.tileSetName = serializable.tileSetName;
            this.tileSpawns = serializable.tileSpawns;

        }

        #endregion

        public TileSpawnGroup(string tileSetName, ICollection<TileSpawner> tileSpawns) : base(default)
        {
            this.tileSetName = tileSetName;
            if (tileSpawns != null)
            {
                this.tileSpawns = new TileSpawner[tileSpawns.Count];
                int i = 0;
                foreach (var spawn in tileSpawns)
                {
                    this.tileSpawns[i] = spawn;
                    i++;
                }
            } else this.tileSpawns = null;

        }

        protected readonly string tileSetName;
        public string TileSetName => tileSetName;

        [NonSerialized]
        protected EngineInternal.TileSet cachedTileSet;
        public EngineInternal.TileSet TileSet
        {
            get
            {
                if (cachedTileSet == null) cachedTileSet = swole.Engine.GetTileSet(TileSetName);
                return cachedTileSet;
            }
        }

        protected readonly TileSpawner[] tileSpawns;

        public void Spawn(Transform environmentRoot, List<EngineInternal.TileInstance> tileInstanceOutputList = null)
        {

            if (TileSet == null || tileSpawns == null) return;

            foreach (var spawner in tileSpawns)
            {

                var instance = spawner.CreateNewInstance(cachedTileSet, environmentRoot);

                if (tileInstanceOutputList != null && instance != null) tileInstanceOutputList.Add(instance);

            }

        }

        public string ToJSON()
        {
            throw new NotImplementedException();
        }
    }

}
