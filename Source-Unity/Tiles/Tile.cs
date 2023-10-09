#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using static Swole.TileSet;

namespace Swole
{

    [Serializable]
    public class Tile
    {

        public string name;

        public Texture2D previewTexture;

        [Tooltip("The id that determines (in shader) which sections of the tileset mesh to make visible, if using the tileset mesh at all.")]
        public SubModelID subModelId;

        [Tooltip("Can the tile change position after being spawned?")]
        public bool isDynamic;

        public Vector3 positionOffset;
        public Vector3 initialRotationEuler;
        public Vector3 initialScale = Vector3.one;

        public GameObject prefabBase;

        public bool RenderOnly => prefabBase == null && !isDynamic;

    }

}

#endif