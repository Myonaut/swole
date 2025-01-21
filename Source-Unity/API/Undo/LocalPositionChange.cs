#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Swole.API.Unity.UndoSystem
{
    public struct LocalPositionChange : IRevertableAction
    {
        public bool ReapplyWhenRevertedTo => true;

        public Transform transform;
        public Vector3 oldLocalPosition, newLocalPosition;

        public void Revert() => transform.localPosition = oldLocalPosition;
        public void Reapply() => transform.localPosition = newLocalPosition;
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