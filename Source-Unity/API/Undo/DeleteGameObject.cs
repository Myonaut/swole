#if (UNITY_STANDALONE || UNITY_EDITOR)

using UnityEngine;

namespace Swole.API.Unity.UndoSystem
{
    public struct DeleteGameObject : IRevertableAction
    {
        public bool ReapplyWhenRevertedTo => false;

        public GameObject gameObject;
        public Transform parent;

        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector3 localScale;

        public void Revert() 
        {
            if (gameObject == null) return;

            var transform = gameObject.transform;
            if (parent != null) transform.SetParent(parent, false);

            transform.localPosition = localPosition;
            transform.localRotation = localRotation;
            transform.localScale = localScale;

            gameObject.SetActive(true);
        }
        public void Reapply() 
        {
            if (gameObject == null) return;

            gameObject.SetActive(false);

            var transform = gameObject.transform;
            if (parent != null) transform.SetParent(null);
        }

        public void Perpetuate() 
        {
            GameObject.Destroy(gameObject);
        }
        public void PerpetuateUndo() { }

        public bool undoState;
        public bool GetUndoState() => undoState;
        public IRevertableAction SetUndoState(bool undone)
        {
            var newState = this;
            newState.undoState = undone;
            return newState;
        }

        public DeleteGameObject(GameObject obj)
        {
            undoState = false;
            parent = null;
            localPosition = Vector3.zero;
            localRotation = Quaternion.identity;
            localScale = Vector3.one;

            this.gameObject = obj;
            if (obj == null) return;

            var transform = gameObject.transform;
            parent = transform.parent;

            localPosition = transform.localPosition;
            localRotation = transform.localRotation;
            localScale = transform.localScale;

            Reapply();
        }
    }
}

#endif