#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole
{
    [CreateAssetMenu(fileName = "SelectableTextureGroup", menuName = "Data/SelectableTextureGroup", order = 2)]
    public class SelectableTextureGroup : ScriptableObject, ISelectableAssetCollection
    {
        [Serializable]
        public struct SecondaryTexture
        {
            public string id;
            public Texture2D texture;
        }
        [Serializable]
        public struct SelectableTexture : ISelectableAsset
        {
            public string displayName;
            public string id;
            public Sprite previewSprite;
            public Texture2D texture;
            public SecondaryTexture[] secondaryTextures;

            public string description;
            public string[] attributes;
            public string[] tags;

            public string Name => displayName;
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

            public bool TryGetSecondaryTexture(string id, out Texture2D tex)
            {
                tex = default;
                if (secondaryTextures == null) return false;

                foreach (var selectable in secondaryTextures)
                {
                    if (selectable.id == id)
                    {
                        tex = selectable.texture;
                        return true;
                    }
                }

                return false;
            }
        }

        public SelectableTexture[] selectableTextures;

        public bool TryGetTexture(string id, out SelectableTexture tex)
        {
            tex = default;
            if (selectableTextures == null) return false;

            foreach (var selectable in selectableTextures)
            {
                if (selectable.id == id)
                {
                    tex = selectable;
                    return true;
                }
            }

            return false;
        }
        public SelectableTexture GetTexture(int index)         
        {
            if (index < 0 || index >= Count) return default;
            return selectableTextures[index];
        }

        public int Count => selectableTextures == null ? 0 : selectableTextures.Length;

        public ISelectableAsset this[int index] => GetAsset(index);
        public ISelectableAsset GetAsset(int index) => GetTexture(index);

        public bool TryGetAsset(string assetId, out ISelectableAsset asset)
        {
            bool flag = TryGetTexture(assetId, out var typedAsset);
            asset = typedAsset;
            return flag;
        }

        public ISelectableAsset Find(string assetId)
        {
            if (selectableTextures == null) return default;

            foreach (var selectable in selectableTextures)
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
            if (selectableTextures == null) return -1;

            for (int a = 0; a < selectableTextures.Length; a++)
            {
                if (selectableTextures[a].ID == assetId) return a;
            }

            return -1;
        }
    }
}

#endif