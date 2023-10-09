#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole
{

    public class TileInstanceGroup : MonoBehaviour
    {

        [NonSerialized]
        protected List<TileInstance> tileInstances;



    }

}

#endif
