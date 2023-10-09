#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole
{

    [CreateAssetMenu(fileName = "NewTileCollection", menuName = "Environment/TileCollection", order = 3)]
    public class TileCollection : ScriptableObject
    {

        public string id;

        public TileSet[] tileSets;

    }

}

#endif
