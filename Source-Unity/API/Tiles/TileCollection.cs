#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole.API.Unity
{

    [CreateAssetMenu(fileName = "NewTileCollection", menuName = "Environment/TileCollection", order = 3)]
    public class TileCollection : ScriptableObject
    {

        [SerializeField]
        protected string id;
        public string ID
        {
            get
            {
                if (string.IsNullOrWhiteSpace(id)) return name;
                return id;
            }
        }

        public TileCollectionCategory category;

        //public TileSet[] tileSets;
        [SerializeField]
        protected InternalResources.Locator[] tileSets;
        public int TileSetCount => tileSets == null ? 0 : tileSets.Length;
        public TileSet GetTileSet(int index)
        {
            if (tileSets == null || index < 0 || index >= tileSets.Length) return null;

            var locator = tileSets[index];
            if (!string.IsNullOrWhiteSpace(locator.path)) 
            { 
                var tileSet = Resources.Load<TileSet>(locator.path);
                if (tileSet == null) return null;

                tileSet.collectionId = id; 
                tileSet.IsInternalAsset = true;
                if (tileSet.tiles != null) foreach (var tile in tileSet.tiles) tile.IsInternalAsset = true;

                return tileSet;
            }

            return null;
        }
        public int IndexOf(string tileSetId, bool caseSensitive = false)
        {
            if (string.IsNullOrWhiteSpace(tileSetId)) return -1;

            if (!caseSensitive) tileSetId = tileSetId.AsID();
            for(int a = 0; a < TileSetCount; a++)
            {
                var asset = tileSets[a];
                if (string.IsNullOrWhiteSpace(asset.id) || (caseSensitive ? asset.id : asset.id.AsID()) != tileSetId) continue;
                return a;
            }

            return -1;
        }
        public TileSet LoadTileSet(string id, bool caseSensitive = false)
        {
            int index = IndexOf(id, caseSensitive);
            if (index < 0) return null; 

            return GetTileSet(index);
        }
        public bool HasTileSet(string id, bool caseSensitive = false) => IndexOf(id, caseSensitive) >= 0;

    }

}

#endif
