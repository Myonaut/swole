using System;

namespace Swole
{
    public interface ITile : ISwoleAsset, ICloneable
    {

        public ITile Instance { get; }

        public IImageAsset PreviewTexture { get; set; }

        /// <summary>
        /// The id that determines (in shader) which sections of the tileset mesh to make visible, if using the tileset mesh at all.
        /// </summary>
        public SubModelID SubModelId { get; set; }

        /// <summary>
        /// Should the tile be represented by a game object instance?
        /// </summary>
        public bool IsGameObject { get; set; }

        /// <summary>
        /// Can the game object instance part of the tile be toggled off?
        /// </summary>
        public bool CanToggleOffGameObject { get; set; }

        public EngineInternal.Vector3 PositionOffset { get; set; }
        public EngineInternal.Vector3 InitialRotationEuler { get; set; }
        public EngineInternal.Vector3 InitialScale { get; set; }

        public EngineInternal.GameObject PrefabBase { get; set; }

        public bool RenderOnly { get; }

    }
}
