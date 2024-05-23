#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole.API.Unity
{

    /// <summary>
    /// Used to select other objects when this object is selected in an editor
    /// </summary>
    public class SelectionProxy : MonoBehaviour
    {
        public bool includeSelf;
        public List<GameObject> toSelect;
    }

}

#endif