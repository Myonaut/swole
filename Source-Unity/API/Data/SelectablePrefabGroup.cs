#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole
{
    [CreateAssetMenu(fileName = "SelectablePrefabGroup", menuName = "Data/SelectablePrefabGroup", order = 4)]
    public class SelectablePrefabGroup : ScriptableObject, ISelectableAssetCollection
    {
        [Serializable]
        public struct SelectablePrefab : ISelectableAsset
        {
            public string id;
            public string description;
            public Sprite previewSprite;
            public GameObject prefab;
            public string[] attributes;
            public string[] tags;

            public string Name => prefab == null ? "null" : prefab.name;
            public string ID => id;

            public string Description => description;

            public int AttributeCount => attributes == null ? 0 : attributes.Length;

            public int TagCount => tags == null ? 0 : tags.Length;

            public string GetAttribute(int index)
            {
                if (index < 0 || index >= attributes.Length) return string.Empty;
                return attributes[index];
            }

            public string GetTag(int index)
            {
                if (index < 0 || index >= tags.Length) return string.Empty;
                return tags[index];
            }

            public bool HasAttribute(string attribute)
            {
                if (attributes == null) return false;
                foreach (var attr in attributes)
                {
                    if (attr == attribute) return true;
                }

                return false;
            }

            public bool HasTag(string tag)
            {
                if (tags == null) return false;
                foreach (var tag_ in tags)
                {
                    if (tag_ == tag) return true;
                }

                return false;
            }
        }

        public SelectablePrefab[] selectablePrefabs;

        public int Count => selectablePrefabs == null ? 0 : selectablePrefabs.Length;

        public ISelectableAsset this[int index] => GetAsset(index);
        public ISelectableAsset GetAsset(int index)
        {
            if (index < 0 || index >= Count) return default;
            return selectablePrefabs[index];
        }

        public bool TryGetPrefab(string id, out SelectablePrefab prefab)
        {
            prefab = default;
            if (selectablePrefabs == null) return false;

            foreach (var selectable in selectablePrefabs)
            {
                if (selectable.id == id)
                {
                    prefab = selectable;
                    return true;
                }
            }

            return false;
        }
        public bool TryGetAsset(string assetId, out ISelectableAsset asset)
        {
            bool flag = TryGetPrefab(assetId, out var typedAsset);
            asset = typedAsset;
            return flag;
        }

        public int IndexOf(string assetId)
        {
            if (selectablePrefabs == null) return -1;

            for(int a = 0; a < selectablePrefabs.Length; a++)
            {
                if (selectablePrefabs[a].id == assetId) return a;
            }

            return -1;
        }

        public ISelectableAsset Find(string assetId)
        {
            if (selectablePrefabs == null) return default;

            foreach (var selectable in selectablePrefabs)
            {
                if (selectable.id == assetId)
                {
                    return selectable;
                }
            }

            return default;
        }
    }
}

#endif
