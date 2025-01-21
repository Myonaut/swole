#if (UNITY_STANDALONE || UNITY_EDITOR)

using UnityEngine;

namespace Swole.API.Unity.UndoSystem
{
    public struct WorldRotationChange : IRevertableAction
    {
        public bool ReapplyWhenRevertedTo => true;

        public Transform transform;
        public Quaternion oldRotation, newRotation;

        public void Revert() => transform.rotation = oldRotation;
        public void Reapply() => transform.rotation = newRotation;
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