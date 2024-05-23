using System;
using System.Collections.Generic;

namespace Swole
{

    /// <summary>
    /// A group of spawners that spawn the same creation asset
    /// </summary>
    public class CreationSpawnGroup : ObjectSpawnGroup
    {

        public CreationSpawnGroup(string packageIdentityString, string assetName, ICollection<ObjectSpawner> creationSpawns) : base(new ObjectSpawnGroup.Serialized() { assetStringSecondary = packageIdentityString, assetStringMain = assetName })
        {
            if (creationSpawns != null)
            {
                this.objectSpawns = new ObjectSpawner[creationSpawns.Count];
                int i = 0;
                foreach (var spawn in creationSpawns)
                {
                    this.objectSpawns[i] = spawn;
                    i++;
                }
            }
            else this.objectSpawns = null;

        }

        public string AssetName => AssetStringMain;
        public string PackageIdentityString => AssetStringSecondary;
        public PackageIdentifier PackageIdentity => new PackageIdentifier(PackageIdentityString);

        [NonSerialized]
        protected Creation cachedAsset;
        public Creation Asset
        {
            get
            {
                if (cachedAsset == null) cachedAsset = ContentManager.FindContent<Creation>(AssetName, PackageIdentity);
                return cachedAsset;
            }
        }

        public override void Spawn(EngineInternal.ITransform environmentRoot, bool useRealTransformsOnly) => Spawn(environmentRoot, useRealTransformsOnly, null); 
        public void Spawn(EngineInternal.ITransform environmentRoot, bool useRealTransformsOnly, List<EngineInternal.CreationInstance> creationInstanceOutputList)
        {
            if (Asset == null || objectSpawns == null) return;

            foreach (var spawner in objectSpawns)
            {
                var instance = spawner.CreateNewCreationInstance(cachedAsset, useRealTransformsOnly, environmentRoot);
                if (creationInstanceOutputList != null) creationInstanceOutputList.Add(instance);
            }
        }
        public override void SpawnIntoList(EngineInternal.ITransform environmentRoot, bool useRealTransformsOnly, List<EngineInternal.ITransform> instanceOutputList = null)
        {
            if (Asset == null || objectSpawns == null) return;

            foreach (var spawner in objectSpawns)
            {
                var instance = spawner.CreateNewCreationInstance(cachedAsset, useRealTransformsOnly, environmentRoot); 
                if (instanceOutputList != null) instanceOutputList.Add(instance.Root.transform); 
            }
        }

    }
}
