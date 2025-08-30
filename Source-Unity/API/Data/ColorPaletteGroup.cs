#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole
{
    [CreateAssetMenu(fileName = "ColorPaletteGroup", menuName = "Data/ColorPaletteGroup", order = 1)]
    public class ColorPaletteGroup : ScriptableObject, ISelectableAssetCollection
    {
        public ColorPalette[] colorPalettes;
        public int PaletteCount => colorPalettes == null ? 0 : colorPalettes.Length;

        public ColorPalette GetPalette(int index)
        {
            if (colorPalettes == null || index < 0 || index >= PaletteCount) return null;

            return colorPalettes[index];
        }
        public bool TryGetPalette(string id, out ColorPalette palette)
        {
            palette = null;
            if (colorPalettes == null) return false;

            foreach (var colorPalette in colorPalettes)
            {
                if (colorPalette.name == id) 
                { 
                    palette = colorPalette;
                    return true; 
                }
            }

            return false;
        }

        public Color GetColor(string id, float t = 0f)
        {
            if (colorPalettes == null) return Color.clear;

            foreach (var colorPalette in colorPalettes)
            {
                if (colorPalette.TryGetColor(out var col, id, t)) return col;
            }

            return Color.clear;
        }
        public bool TryGetColor(out Color color, string id, float t = 0f)
        {
            color = Color.clear;
            if (colorPalettes == null) return false;

            foreach (var colorPalette in colorPalettes)
            {
                if (colorPalette.TryGetColor(out color, id, t)) return true;
            }

            return false;
        }
        public bool TryGetGradientColor(out Color color, string id, float t)
        {
            color = Color.clear;
            if (colorPalettes == null) return false;

            foreach (var colorPalette in colorPalettes)
            {
                if (colorPalette.TryGetGradientColor(out color, id, t)) return true;
            }

            return false;
        }
        public bool TryGetGradient(string id, out Gradient gradient)
        {
            gradient = null;
            if (colorPalettes == null) return false;

            foreach (var colorPalette in colorPalettes)
            {
                if (colorPalette.TryGetGradient(id, out gradient)) return true;
            }

            return false;
        }

        public int Count => colorPalettes == null ? 0 : colorPalettes.Length;

        public ISelectableAsset this[int index] => GetAsset(index);
        public ISelectableAsset GetAsset(int index) => GetPalette(index);

        public bool TryGetAsset(string assetId, out ISelectableAsset asset)
        {
            bool flag = TryGetPalette(assetId, out var typedAsset);
            asset = typedAsset;
            return flag;
        }

        public ISelectableAsset Find(string assetId)
        {
            if (colorPalettes == null) return default;

            foreach (var selectable in colorPalettes)
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
            if (colorPalettes == null) return -1;

            for (int a = 0; a < colorPalettes.Length; a++)
            {
                if (colorPalettes[a].ID == assetId) return a;
            }

            return -1;
        }

    }
}

#endif