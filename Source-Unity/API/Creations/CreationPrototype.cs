#if (UNITY_STANDALONE || UNITY_EDITOR)

using UnityEngine;

namespace Swole.API.Unity
{

    /// <summary>
    /// Used to place a cretion in an editing environment
    /// </summary>
    public class CreationPrototype : MonoBehaviour
    {

        public Creation asset;

        /// <summary>
        /// if asset is null, this is used
        /// </summary>
        public string packageId;
        /// <summary>
        /// if asset is null, this is used
        /// </summary>
        public string creationId;

    }

}

#endif
