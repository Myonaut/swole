using System;

using Swole.Script;

namespace Swole
{
    public interface ITileInstance : EngineInternal.ITransform, IDisposable
    {

        public int SwoleId { set; get; }

        public string TileSetId { get; }
        public int TileIndex { get; }

        public bool IsRenderOnly { get; }

        public bool visible { get; set; }

        [SwoleScriptIgnore]
        public EngineInternal.GameObject Root { get; }

        [SwoleScriptIgnore]
        public void ForceUseRealTransform();
        public void ReevaluateRendering();

        [SwoleScriptIgnore]
        public void SetParent(EngineInternal.ITransform newParent, bool worldPositionStays, bool forceRealTransformConversion);

    }
}
