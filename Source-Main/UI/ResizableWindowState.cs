using System;

namespace Swole.UI
{
    [Serializable]
    public struct ResizableWindowState
    {

        public string id;
        public bool visible;
        public EngineInternal.Vector3 positionInCanvas;
        public float width;
        public float height;

        public int dataIntA;

        public string dataStringA;

    }
}
