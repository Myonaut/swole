#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using UnityEngine; 

namespace Swole.API.Unity
{

    [Serializable]
    public class Tile : ITile
    {

        #region ICloneable

        public object Clone() => Duplicate();
        public Tile Duplicate()
        {
            var newTile = new Tile();

            newTile.name = name;
            newTile.previewTexture = previewTexture;
            newTile.subModelId = subModelId;
            newTile.isGameObject = isGameObject;
            newTile.canToggleOffGameObject = canToggleOffGameObject;
            newTile.positionOffset = positionOffset;
            newTile.initialRotationEuler = initialRotationEuler;
            newTile.initialScale = initialScale;
            newTile.prefabBase = prefabBase;

            return newTile;
        }

        #endregion

        public ITile Instance => this;

        public string name;
        public string Name
        {
            get => name;
        }

        public Texture2D previewTexture;
        public IImageAsset PreviewTexture
        {
            get => ResourceLib.GetImageAsset(previewTexture);
            set
            {
                if (value is ImageAsset asset)
                {
                    previewTexture = asset.Texture;
                }
            }
        }

        [Tooltip("The id that determines (in shader) which sections of the tileset mesh to make visible, if using the tileset mesh at all.")]
        public SubModelID subModelId;
        public SubModelID SubModelId
        {
            get => subModelId;
            set => subModelId = value;
        }

        [Tooltip("Should the tile be represented by a game object instance?")]
        public bool isGameObject;
        public bool IsGameObject
        {
            get => isGameObject;
            set => isGameObject = value;
        }

        [Tooltip("Can the game object instance part of the tile be toggled off?")]
        public bool canToggleOffGameObject;
        public bool CanToggleOffGameObject
        {
            get => canToggleOffGameObject;
            set => canToggleOffGameObject = value;
        }

        public Vector3 positionOffset;
        public EngineInternal.Vector3 PositionOffset
        {
            get => UnityEngineHook.AsSwoleVector(positionOffset);
            set => positionOffset = UnityEngineHook.AsUnityVector(value);
        }
        public Vector3 initialRotationEuler;
        public EngineInternal.Vector3 InitialRotationEuler
        {
            get => UnityEngineHook.AsSwoleVector(initialRotationEuler);
            set => initialRotationEuler = UnityEngineHook.AsUnityVector(value);
        }
        public Vector3 initialScale = Vector3.one;
        public EngineInternal.Vector3 InitialScale
        {
            get => UnityEngineHook.AsSwoleVector(initialScale);
            set => initialScale = UnityEngineHook.AsUnityVector(value);
        }

        public GameObject prefabBase;
        public EngineInternal.GameObject PrefabBase
        {
            get => UnityEngineHook.AsSwoleGameObject(prefabBase);
            set => prefabBase = UnityEngineHook.AsUnityGameObject(value);
        }

        public bool RenderOnly => prefabBase == null && !isGameObject;

    }

}

#endif