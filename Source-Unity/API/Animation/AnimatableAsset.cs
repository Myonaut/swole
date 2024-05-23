#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole.API.Unity
{

    [CreateAssetMenu(fileName = "AnimatableAsset", menuName = "Animation/AnimatableAsset", order = 1)]
    public class AnimatableAsset : ScriptableObject
    {
         
        [Serializable]
        public enum ObjectType
        {
            Humanoid, Mechanical
        }

        public ObjectType type;

        public GameObject prefab;

    }

}

#endif