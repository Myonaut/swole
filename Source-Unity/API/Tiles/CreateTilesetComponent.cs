#if (UNITY_STANDALONE || UNITY_EDITOR)

#if BULKOUT_ENV
using RLD;
#endif

using UnityEngine;

namespace Swole.API.Unity
{

    [ExecuteInEditMode]
    public class CreateTilesetComponent : MonoBehaviour
    {

        public bool apply;

        public TileSetSource source;
        public Material material;
        public Material outlineMaterial;
        public Tile[] tiles;

#if BULKOUT_ENV
        public PrefabPreviewLookAndFeel lookAndFeel;
#endif

        public string path;
        public string fileName;
        public bool incrementIfExists;
        public bool forceRefreshTilePreviews;

        public TileSet output;

        public void Update()
        {
            if (Application.isPlaying) return;
            if (apply)
            {

                apply = false;

#if BULKOUT_ENV
                output = TileSet.Create(source, material, outlineMaterial, tiles, lookAndFeel, path, fileName, incrementIfExists, forceRefreshTilePreviews, true, true);
#else
                output = TileSet.Create(source, material, outlineMaterial, tiles, path, fileName, incrementIfExists, forceRefreshTilePreviews, true, true);
#endif

            }

        }

    }

}

#endif