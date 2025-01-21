#if (UNITY_STANDALONE || UNITY_EDITOR)

using UnityEngine;

namespace Swole.API.Unity.UndoSystem
{
    public struct WorldPositionChange : IRevertableAction
    {
        public bool ReapplyWhenRevertedTo => true;

        public Transform transform;
        public Vector3 oldPosition, newPosition;

        public void Revert() => transform.position = oldPosition;
        public void Reapply() => transform.position = newPosition;
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