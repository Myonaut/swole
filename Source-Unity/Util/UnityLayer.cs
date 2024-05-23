#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using UnityEngine;

namespace Swole
{

    [Serializable]
    public struct UnityLayer
    {
        [SerializeField]
        public int layerIndex;

        public int Mask
        {
            get { return 1 << layerIndex; }
        }

        public static implicit operator int(UnityLayer layer) => layer.layerIndex;
    }
}

#endif