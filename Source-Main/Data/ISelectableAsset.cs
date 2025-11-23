using System;
using System.Collections;
using System.Collections.Generic;

namespace Swole
{
    public interface ISelectableAsset
    {
        public string Name { get; }
        public string ID { get; }
        public string Description { get; }

        public int AttributeCount { get; }
        public string GetAttribute(int index);
        public bool HasAttribute(string attribute);
        public bool HasPrefixAttribute(string attributePrefix);

        public int TagCount { get; }
        public string GetTag(int index);
        public bool HasTag(string tag);
    }
    public interface ISelectableAssetCollection
    {
        public ISelectableAsset this[int index] { get; }
        public int Count { get; }
        public ISelectableAsset GetAsset(int index);
        public int IndexOf(string assetId);
        public ISelectableAsset Find(string assetId);
        public bool TryGetAsset(string assetId, out ISelectableAsset asset);
    }

    public static class SelectableAssetExtensions
    {
        public static List<ISelectableAsset> GetAssetsWithTag(this ISelectableAssetCollection collection, string tag, List<ISelectableAsset> list = null)
        {
            if (list == null) list = new List<ISelectableAsset>();

            if (collection != null)
            {
                for(int a = 0; a < collection.Count; a++)
                {
                    var asset = collection[a];
                    for(int b = 0; b < asset.TagCount; b++)
                    {
                        var tag_ = asset.GetTag(b);
                        if (tag_ == tag) list.Add(asset);
                    }
                }
            }

            return list;
        }

        public static string GetAttributeValue(this ISelectableAsset asset, string attributePrefix)
        {
            if (asset == null) return string.Empty;

            for(int a = 0; a < asset.AttributeCount; a++)
            {
                var attribute = asset.GetAttribute(a);
                if (string.IsNullOrWhiteSpace(attribute)) continue;

                if (attribute.StartsWith(attributePrefix))
                {
                    return attributePrefix.Length >= attribute.Length ? string.Empty : attribute.Substring(attributePrefix.Length);
                }
            }

            return string.Empty;
        }
    }
}
