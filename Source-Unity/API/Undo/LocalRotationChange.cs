#if (UNITY_STANDALONE || UNITY_EDITOR)

using UnityEngine;

namespace Swole.API.Unity.UndoSystem
{
    public struct LocalRotationChange : IRevertableAction
    {
        public bool ReapplyWhenRevertedTo => true;

        public Transform transform;
        public Quaternion oldLocalRotation, newLocalRotation;

        public void Revert() => transform.localRotation = oldLocalRotation;
        public void Reapply() => transform.localRotation = newLocalRotation;
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