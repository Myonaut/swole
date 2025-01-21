#if (UNITY_STANDALONE || UNITY_EDITOR)

using UnityEngine;

namespace Swole.API.Unity.UndoSystem
{
    public struct TransformParentChange : IRevertableAction
    {
        public bool ReapplyWhenRevertedTo => true;

        public Transform transform;

        public Transform oldParent;
        public Vector3 oldLocalPosition;
        public Quaternion oldLocalRotation;
        public Vector3 oldLocalScale;

        public Transform newParent;
        public Vector3 newLocalPosition;
        public Quaternion newLocalRotation;
        public Vector3 newLocalScale;

        public void Revert()
        {
            if (transform == null) return;

            transform.SetParent(oldParent, false);
            transform.localPosition = oldLocalPosition;
            transform.localRotation = oldLocalRotation;
            transform.localScale = oldLocalScale;
        }
        public void Reapply()
        {
            if (transform == null) return;

            transform.SetParent(newParent, false);
            transform.localPosition = newLocalPosition;
            transform.localRotation = newLocalRotation;
            transform.localScale = newLocalScale;
        }

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

        public TransformParentChange(Transform targetTransform, Transform newParent, Vector3 newLocalPosition, Quaternion newLocalRotation, Vector3 newLocalScale)
        {
            undoState = false;
            oldParent = this.newParent = null;
            oldLocalPosition = this.newLocalPosition = Vector3.zero;
            oldLocalRotation = this.newLocalRotation = Quaternion.identity;
            oldLocalScale = this.newLocalScale = Vector3.one;

            transform = targetTransform;
            if (targetTransform == null) return;

            oldParent = transform.parent;
            oldLocalPosition = transform.localPosition;
            oldLocalRotation = transform.localRotation;
            oldLocalScale = transform.localScale;

            this.newParent = newParent;
            this.newLocalPosition = newLocalPosition;
            this.newLocalRotation = newLocalRotation;
            this.newLocalScale = newLocalScale;

            Reapply();
        }
    }
}

#endif