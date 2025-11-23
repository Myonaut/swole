#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole
{
    [CreateAssetMenu(fileName = "ColorPalette", menuName = "Data/ColorPalette", order = 0)]
    public class ColorPalette : ScriptableObject, ISelectableAsset
    {
        public string displayName;

        public NamedColor[] colors;
        public NamedColorGradient[] gradients;

        public Color GetColor(string id, float t = 0f)
        {
            if (colors != null)
            {
                foreach (var col in colors) if (col.id == id) return col.color;
            }
            if (gradients != null)
            {
                foreach (var grad in gradients) if (grad.id == id) return grad.gradient == null ? Color.clear : grad.gradient.Evaluate(t);
            }

            return Color.clear;
        }
        public bool TryGetColor(out Color color, string id, float t = 0f)
        {
            color = Color.clear;

            if (colors != null)
            {
                foreach (var col in colors) if (col.id == id)
                    {
                        color = col.color;
                        return true;
                    }
            }
            if (gradients != null)
            {
                foreach (var grad in gradients) if (grad.id == id)
                    {
                        color = grad.gradient == null ? Color.clear : grad.gradient.Evaluate(t);
                        return true;
                    }
            }

            return false;
        }
        public bool TryGetGradientColor(out Color color, string id, float t)
        {
            color = Color.clear;

            if (gradients != null)
            {
                foreach (var grad in gradients) if (grad.id == id)
                    {
                        color = grad.gradient == null ? Color.clear : grad.gradient.Evaluate(t);
                        return true;
                    }
            }

            return false;
        }
        public bool TryGetGradient(string id, out Gradient gradient)
        {
            gradient = null;

            if (gradients != null)
            {
                foreach (var grad in gradients) if (grad.id == id)
                    {
                        gradient = grad.gradient; 
                        return true;
                    }
            }

            return false;
        }

        public string description;
        public string[] attributes;
        public string[] tags;

        public string Name => displayName;
        public string ID => name;

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
        public bool HasPrefixAttribute(string attributePrefix)
        {
            if (attributes == null) return false;
            foreach (var attr in attributes)
            {
                if (attr.StartsWith(attributePrefix)) return true;
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

    [Serializable]
    public struct NamedColor
    {
        public string id;
        [ColorUsage(true, true)]
        public Color color;
    }

    [Serializable]
    public struct NamedColorGradient
    {
        public string id;
        [GradientUsage(true)]
        public Gradient gradient;
    }
}

#endif