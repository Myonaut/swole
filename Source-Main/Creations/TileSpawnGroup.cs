using System;
using System.Collections.Generic;

using static Swole.EngineInternal;

namespace Swole
{

    [Serializable]
    public class TileSpawnGroup
    {

        public TileSpawnGroup(string tileSetName, ICollection<TileSpawner> tileSpawns)
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
                if (cachedTileSet == null) cachedTileSet = Swole.Engine.GetTileSet(TileSetName);
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

    }

}
