#if (UNITY_STANDALONE || UNITY_EDITOR)

using Swole.Script;
using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Swole.API.Unity
{

    public class BleakCodeEditor : InputField, ICodeEditor
    {
        public string Code { get => text; set => SetTextWithoutNotify(value); }

        public int undoCapacity = 70;
        public int UndoHistoryCapacity { get => undoCapacity; set => undoCapacity = value; }

        protected struct UndoHistoryState
        {
            public string code;
            public int caretStartPos;
            public int caretEndPos;
        }

        protected List<UndoHistoryState> undoHistory = new List<UndoHistoryState>();
        protected int undoPosition;

        protected void AddUndoState(string code)
        {
            if (undoPosition >= 0 && undoPosition < undoHistory.Count && undoHistory[undoPosition].code == code) return;
            undoCapacity = Mathf.Max(2, undoCapacity);
            if (undoPosition < undoHistory.Count - 1) undoHistory.RemoveRange(undoPosition + 1, undoHistory.Count - (undoPosition + 1));
            undoHistory.Add(new UndoHistoryState() { code = code, caretStartPos = selectionAnchorPosition, caretEndPos = selectionFocusPosition });
            undoPosition = undoHistory.Count - 1;
            while (undoHistory.Count > undoCapacity)
            {
                undoHistory.RemoveAt(0);
                undoPosition--;
            }
        }

        protected float undoCooldown;
        public float timeBetweenUndo = 0.05f;
        protected float rapidUndoTime;
        public float timeToRapidUndo = 0.5f;

        public void Undo()
        {
            if (undoHistory == null) return;
            undoPosition = Mathf.Clamp(undoPosition, 0, undoHistory.Count - 1);
            if (undoPosition == undoHistory.Count - 1)
            {
                AddUndoState(Code); // Add redo point
                undoPosition--;
            }
            var state = undoHistory[undoPosition];
            Code = state.code;
            selectionAnchorPosition = state.caretStartPos;
            selectionFocusPosition = state.caretEndPos;
            if (undoPosition > 0) undoPosition--;
        }

        public void Redo()
        {
            if (undoHistory == null) return;
            undoPosition = Mathf.Clamp(undoPosition, 0, undoHistory.Count - 1) + 1;
            if (undoPosition >= undoHistory.Count) return;
            var state = undoHistory[undoPosition];
            Code = state.code;
            selectionAnchorPosition = state.caretStartPos;
            selectionFocusPosition = state.caretEndPos;
        }

        private Dictionary<ICodeEditor.CodeEditCallback, UnityAction<string>> boundActions = new Dictionary<ICodeEditor.CodeEditCallback, UnityAction<string>>();

        [Serializable]
        public class OnEditorCloseEvent : UnityEvent<string> { }

        [SerializeField]
        private OnEditorCloseEvent onEditorClose = new OnEditorCloseEvent();

        protected override void OnDestroy()
        {

            StopListeningForChanges(AddUndoState);

            onEditorClose?.Invoke(Code);
            onEditorClose?.RemoveAllListeners();

            boundActions?.Clear();
            boundActions = null;

            undoHistory.Clear();
            undoHistory = null;

            base.OnDestroy();

        }

        public virtual void ListenForClosure(ICodeEditor.CodeEditCallback callback)
        {
            if (boundActions == null) return;
            if (!boundActions.TryGetValue(callback, out var action))
            {
                action = new UnityAction<string>(callback);
                boundActions[callback] = action;
            }
            onEditorClose?.AddListener(action);
        }

        public virtual bool StopListeningForClosure(ICodeEditor.CodeEditCallback callback)
        {
            if (boundActions == null) return false;
            if (!boundActions.TryGetValue(callback, out var action)) return false;
            onEditorClose?.RemoveListener(action);
            return true;
        }

        public virtual void ListenForChanges(ICodeEditor.CodeEditCallback callback)
        {
            if (boundActions == null) return;
            if (!boundActions.TryGetValue(callback, out var action))
            {
                action = new UnityAction<string>(callback);
                boundActions[callback] = action;
            }
            onValueChanged?.AddListener(action);
            onSubmit?.AddListener(action);
            onEndEdit?.AddListener(action);
        }

        public virtual bool StopListeningForChanges(ICodeEditor.CodeEditCallback callback)
        {
            if (boundActions == null) return false;
            if (!boundActions.TryGetValue(callback, out var action)) return false;
            onValueChanged?.RemoveListener(action);
            onSubmit?.RemoveListener(action);
            onEndEdit?.RemoveListener(action);
            return true;
        }

        protected override void Awake()
        {

            contentType = ContentType.Standard;
            lineType = LineType.MultiLineNewline;

            base.Awake();

            ListenForChanges(AddUndoState);

        }

        public bool testUndo;
        public bool testRedo;

        protected void Update()
        {

            if (isFocused)
            {

                if (rapidUndoTime > timeToRapidUndo)
                {
                    if (undoCooldown <= 0)
                    {
                        if (InputProxy.Modding_RedoKey)
                        {
                            Redo();
                            undoCooldown = timeBetweenUndo;
                        }
                        else if (InputProxy.Modding_UndoKey)
                        {
                            Undo();
                            undoCooldown = timeBetweenUndo;
                        }
                    }
                    else undoCooldown -= Time.deltaTime;
                }
                else
                {
                    if (InputProxy.Modding_UndoKey) rapidUndoTime += Time.deltaTime;

                    if (InputProxy.Modding_RedoKeyDown)
                    {
                        Redo();
                    }
                    else if (InputProxy.Modding_UndoKeyDown)
                    {
                        Undo();
                    }

                }

                if (InputProxy.Modding_UndoKeyUp)
                {
                    undoCooldown = 0;
                    rapidUndoTime = 0;
                }

            }
            else
            {
                undoCooldown = 0;
                rapidUndoTime = 0;
            }

        }

    }

}

#endif
