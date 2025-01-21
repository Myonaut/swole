#if (UNITY_STANDALONE || UNITY_EDITOR)

using UnityEngine;

namespace Swole.API.Unity.UndoSystem
{
    public struct LocalScaleChange : IRevertableAction
    {
        public bool ReapplyWhenRevertedTo => true;

        public Transform transform;
        public Vector3 oldScale, newScale;

        public void Revert() => transform.localScale = oldScale;
        public void Reapply() => transform.localScale = newScale;
        public void Perpetuate() { }
        public void PerpetuateUndo() { }

        public bool undoState;
        public bool GetUndoState() => undoState;
        public IRevertableAction SetUndoState(bool undone)
        {
            var newState = this;
            newState.undoState = undone;
            return newState;
        }
    }
}

#endif