namespace Swole
{
    public interface ITileSet : ISwoleAsset, EngineInternal.IEngineObject
    {

        public int TileCount { get; }
        public ITile this[int tileIndex] { get; }
        public ITile[] Tiles { get; set; } 

        public IMeshAsset TileMesh { get; set; }
        public IMaterialAsset TileMaterial { get; set; }
        public IMaterialAsset TileOutlineMaterial { get; set; }

        public bool IgnoreMeshMasking { get; set; }

    }
}
