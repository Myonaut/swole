#if (UNITY_STANDALONE || UNITY_EDITOR)

using UnityEngine;

using Swole.Animation;

namespace Swole.API.Unity.Animation
{
    [CreateAssetMenu(fileName = "newAvatarMask", menuName = "Swole/Animation/CustomAvatarMask", order = 0)]
    public class CustomAvatarMaskAsset : ScriptableObject
    {
        public WeightedAvatarMask mask;
        public static implicit operator WeightedAvatarMask(CustomAvatarMaskAsset asset) => asset == null ? null : asset.mask;  
    }
}

#endif