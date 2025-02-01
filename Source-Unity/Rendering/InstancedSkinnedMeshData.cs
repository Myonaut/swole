#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole
{
    [CreateAssetMenu(fileName = "InstancedSkinnedMeshData", menuName = "InstancedMeshData/InstancedSkinnedMeshData", order = 1)]
    public class InstancedSkinnedMeshData : InstanceableSkinnedMeshDataBase
    {
        public const string _rigInstanceIDPropertyName = "_RigInstanceID";
        public string rigInstanceIDPropertyNameOverride;
        public string RigInstanceIDPropertyName => string.IsNullOrWhiteSpace(rigInstanceIDPropertyNameOverride) ? _rigInstanceIDPropertyName : rigInstanceIDPropertyNameOverride;
    }
}

#endif