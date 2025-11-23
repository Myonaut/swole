#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole
{

    [CreateAssetMenu(fileName = "CharacterAdditionCollection", menuName = "Swole/Character/CharacterAdditionCollection", order = 2)]
    public class CharacterAdditionCollection : ScriptableObject, ISelectableAssetCollection
    {

        public CharacterAddition[] characterAdditions;

        public int Count => characterAdditions == null ? 0 : characterAdditions.Length;

        public bool TryGetAddition(string id, out CharacterAddition addition)
        {
            addition = default;
            if (characterAdditions == null) return false;

            foreach (var selectable in characterAdditions)
            {
                if (selectable.ID == id)
                {
                    addition = selectable;
                    return true;
                }
            }

            return false;
        }

        public CharacterAddition GetAddition(int index)
        {
            if (index < 0 || index >= Count) return default;
            return characterAdditions[index];
        }

        public ISelectableAsset GetAsset(int index) => GetAddition(index);
        public ISelectableAsset this[int index] => GetAsset(index);

        public bool TryGetAsset(string assetId, out ISelectableAsset asset)
        {
            bool flag = TryGetAddition(assetId, out var typedAsset);
            asset = typedAsset;
            return flag;
        }

        public ISelectableAsset Find(string assetId)
        {
            if (characterAdditions == null) return default;

            foreach (var selectable in characterAdditions)
            {
                if (selectable.ID == assetId)
                {
                    return selectable;
                }
            }

            return default;
        }

        public int IndexOf(string assetId)
        {
            if (characterAdditions == null) return -1;

            for (int a = 0; a < characterAdditions.Length; a++)
            {
                if (characterAdditions[a].ID == assetId) return a;
            }

            return -1;
        }

    }

}

#endif