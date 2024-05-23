#if (UNITY_STANDALONE || UNITY_EDITOR)

using UnityEngine;

namespace Swole.API.Unity
{

    /// <summary>
    /// Used to place a tile in an editing environment
    /// </summary>
    public class TilePrototype : MonoBehaviour
    {

        public TileSet tileSet;

        public int tileIndex;

    }

}

#endif
