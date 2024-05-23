#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Swole.API.Unity
{

    [CreateAssetMenu(fileName = "TileSetSource", menuName = "Environment/TileSetSource", order = 1)]
    public class TileSetSource : ScriptableObject
    {

        [Tooltip("The source meshes to be combined into one tile set mesh. Maximum count is 16. Additional meshes will be ignored.")]
        public Mesh[] sourceMeshes;

    }

}

#endif
