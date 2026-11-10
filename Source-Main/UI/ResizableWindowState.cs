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

        /// <summary>
        /// 1-based rank of the window among the other tracked windows that share its parent, ordered by sibling index. 0 means no order was saved.
        /// It is a relative rank rather than an absolute sibling index, so scene hierarchy changes do not invalidate old files.
        /// </summary>
        public int relativeOrder;

        public int dataIntA;

        public string dataStringA;

    }
}
