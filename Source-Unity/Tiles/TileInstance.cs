#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using static Swole.InstancedRendering;

namespace Swole
{

    public class TileInstance
    {

        public string tileSetName;
        public int tileIndex;

        protected RenderingInstance<InstanceDataMatrixAndMotionVectors> renderingInstance;

        public GameObject rootInstance;

        public TileInstance(string tileSetName, int tileIndex, GameObject rootInstance = null)
        {

            this.tileSetName = tileSetName;
            this.tileIndex = tileIndex;
            this.rootInstance = rootInstance;

        }

    }

}

#endif
