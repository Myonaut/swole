using System;
using System.Collections.Generic;

namespace Swole
{

    public class TileSpawnGroup : ObjectSpawnGroup
    {

        public TileSpawnGroup(ObjectSpawnGroup.Serialized serializable) : base(serializable)
        {
        }
        public TileSpawnGroup(string tileSetId, ICollection<ObjectSpawner> tileSpawns, string tileCollectionId = null, bool collectionIsPackage = false) : base(new ObjectSpawnGroup.Serialized() { assetStringMain = tileSetId, assetStringSecondary = tileCollectionId })
        {
            isPackageDependent = collectionIsPackage;

            if (tileSpawns != null)
            {
                this.objectSpawns = new ObjectSpawner[tileSpawns.Count];
                int i = 0;
                foreach (var spawn in tileSpawns)
                {
                    this.objectSpawns[i] = spawn;
                    i++;
                }
            } else this.objectSpawns = null; 

        }

        public string TileSetId => AssetStringMain;
        public string TileCollectionId => AssetStringSecondary;

        /// <summary>
        /// Does the tileset belong to a package? If so, TileCollectionID becomes the package id.
        /// </summary>
        public override bool IsPackageDependent => isPackageDependent; 

        [NonSerialized]
        protected EngineInternal.TileSet cachedTileSet;
        public EngineInternal.TileSet TileSet
        {
            get
            {
                if (cachedTileSet == null) 
                {
                    cachedTileSet = swole.Engine.GetTileSet(TileSetId, TileCollectionId, isPackageDependent);  
                }
                return cachedTileSet;
            }
        }

        public override void Spawn(EngineInternal.ITransform environmentRoot, bool useRealTransforms) => Spawn(environmentRoot, useRealTransforms, null); 
        public void Spawn(EngineInternal.ITransform environmentRoot, bool useRealTransforms, List<EngineInternal.TileInstance> tileInstanceOutputList)
        {
            if (TileSet == null || objectSpawns == null) return;

            foreach (var spawner in objectSpawns)
            {
                var instance = spawner.CreateNewTileInstance(cachedTileSet, environmentRoot, useRealTransforms);
                if (tileInstanceOutputList != null) tileInstanceOutputList.Add(instance); 
            }
        }
        public override void SpawnIntoList(EngineInternal.ITransform environmentRoot, bool useRealTransforms, List<EngineInternal.ITransform> instanceOutputList)
        {
            if (TileSet == null || objectSpawns == null) return; 

            foreach (var spawner in objectSpawns)
            {
                var instance = spawner.CreateNewTileInstance(cachedTileSet, environmentRoot, useRealTransforms);
                if (instanceOutputList != null) instanceOutputList.Add(instance);
            }
        }

    }

}
