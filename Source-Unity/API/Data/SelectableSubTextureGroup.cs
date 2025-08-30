#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole
{
    [CreateAssetMenu(fileName = "SelectableSubTextureGroup", menuName = "Data/SelectableSubTextureGroup", order = 3)]
    public class SelectableSubTextureGroup : ScriptableObject
    {
        [Serializable]
        public struct SelectableTexture
        {
            public string id;
            public Sprite previewSprite;       
        }

        public Texture2D atlasTexture;

        public int selectionSizeX = 1;
        public int selectionSizeY = 1;

        public SelectableTexture[] selectableTextures;
        public int SelectionCount => selectableTextures == null ? 0 : selectableTextures.Length;
        public float RegionWidth => 1f / selectionSizeX;
        public float RegionHeight => 1f / selectionSizeY;

        public void GetSubTexture(int index, out SelectableTexture tex, out Vector4 uvRegion)
        {
            tex = default;
            uvRegion = new Vector4(0f, 0f, 1f, 1f);
            if (index < 0 || selectableTextures == null || index >= selectableTextures.Length) return;

            tex = selectableTextures[index];
            float x = index % selectionSizeX;
            float y = index / selectionSizeX;
            uvRegion = new Vector4(x, y, x + RegionWidth, y + RegionHeight);
        }
        public bool TryGetSubTexture(string id, out SelectableTexture tex, out Vector4 uvRegion)
        {
            tex = default;
            uvRegion = new Vector4(0f, 0f, 1f, 1f);
            if (selectableTextures == null) return false;

            for (int a = 0; a < selectableTextures.Length; a++)
            {
                var selectable = selectableTextures[a];

                if (selectable.id == id)
                {
                    tex = selectable;
                    float x = a % selectionSizeX;
                    float y = a / selectionSizeX;
                    uvRegion = new Vector4(x, y, x + RegionWidth, y + RegionHeight);
                    return true;
                }
            }

            return false;
        }
    }
}

#endif