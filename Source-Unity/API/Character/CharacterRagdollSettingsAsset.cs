#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole.API.Unity.Animation
{
    [CreateAssetMenu(fileName = "newRagdollSettings", menuName = "Swole/Character/RagdollSettings", order = 0)]
    public class CharacterRagdollSettingsAsset : ScriptableObject
    {
        public CharacterRagdollSettings settings;
    }
}

#endif