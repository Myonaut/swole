using System;

namespace Swole
{
    public interface ISwoleAsset : IDisposable
    {

        public bool IsInternalAsset { get; set; }

        public string Name { get; }

        /// <summary>
        /// Is the asset valid or has it been unloaded/destroyed?
        /// </summary>
        public bool IsValid { get; }

        public Type AssetType { get; }
        public object Asset { get; }

    }
}
