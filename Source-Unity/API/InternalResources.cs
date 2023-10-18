#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole.API.Unity
{

    public class InternalResources : ScriptableObject
    {

        public const string _mainPath = "resources_internal.asset";
#if UNITY_EDITOR
        public static string _mainPathEditor => $"Assets/Resources/{_mainPath}";
#endif

        [Serializable]
        public struct Locator
        {

            public string id;
            public string path;

        }

        public Locator[] tileCollections;

        public bool IsExistingTileCollection(string id)
        {

            if (tileCollections == null) return false;

            id = id.AsID();

            for (int a = 0; a < tileCollections.Length; a++) if (!string.IsNullOrEmpty(tileCollections[a].path) && tileCollections[a].id.AsID() == id) return true;

            return false;

        }

        public TileCollection LoadTileCollection(string id)
        {

            if (tileCollections == null) return null;

            id = id.AsID();

            for (int a = 0; a < tileCollections.Length; a++) if (!string.IsNullOrEmpty(tileCollections[a].path) && tileCollections[a].id.AsID() == id) return Resources.Load<TileCollection>(tileCollections[a].path);

            return null;

        }

    }

}

#endif