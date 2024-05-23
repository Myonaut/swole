#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

using TMPro;

using Swole.Script;

namespace Swole.API.Unity
{

    public class BleakCodeEditor : MonoBehaviour, ICodeEditor
    {

        [Header("Settings")]
        public GameObject rootObject;

        public object RootObject
        {

            get => rootObject == null ? gameObject : rootObject;

            set
            {

                if (value is GameObject obj) rootObject = obj; else rootObject = null;

            }

        }

        public InputField inputField;
        public TMP_InputField inputFieldTMP;

        private static readonly char[] _newLineArray = Environment.NewLine.ToCharArray();
        public string Code { get => (inputFieldTMP != null ? inputFieldTMP.text : (inputField == null ? "" : inputField.text)); 
            
            set 
            {
                string source = string.IsNullOrWhiteSpace(value) ? string.Empty : value.TrimEnd(_newLineArray);  
                inputField?.SetTextWithoutNotify(source);
                inputFieldTMP?.SetTextWithoutNotify(source);

            } 
        
        }

        public int undoCapacity = 70;
        public int UndoHistoryCapacity { get => undoCapacity; set => undoCapacity = value; }

        protected struct UndoHistoryState
        {
            public string code;
            public int caretStartPos;
            public int caretEndPos;
        }

        public int SelectionAnchorPosition { 
            
            get => inputFieldTMP != null ? inputFieldTMP.selectionAnchorPosition : (inputField == null ? 0 : inputField.selectionAnchorPosition); 

            set
            {
                if (inputFieldTMP != null) inputFieldTMP.selectionAnchorPosition = value;
                if (inputField != null) inputField.selectionAnchorPosition = value;
            }
        
        }
        public int SelectionFocusPosition { 
            
            get => inputFieldTMP != null ? inputFieldTMP.selectionFocusPosition : (inputField == null ? 0 : inputField.selectionFocusPosition);

            set
            {
                if (inputFieldTMP != null) inputFieldTMP.selectionFocusPosition = value;
                if (inputField != null) inputField.selectionFocusPosition = value;
            }

        }

        protected List<UndoHistoryState> undoHistory = new List<UndoHistoryState>();
        protected int undoPosition;

        protected void AddUndoState(string code)
        {
            if (undoPosition >= 0 && undoPosition < undoHistory.Count && undoHistory[undoPosition].code == code) return;
            undoCapacity = Mathf.Max(2, undoCapacity);
            if (undoPosition < undoHistory.Count - 1) undoHistory.RemoveRange(undoPosition + 1, undoHistory.Count - (undoPosition + 1));
            undoHistory.Add(new UndoHistoryState() { code = code, caretStartPos = SelectionAnchorPosition, caretEndPos = SelectionFocusPosition });
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
            SelectionAnchorPosition = state.caretStartPos;
            SelectionFocusPosition = state.caretEndPos;
            if (undoPosition > 0) undoPosition--;
        }

        public void Redo()
        {
            if (undoHistory == null) return;
            undoPosition = Mathf.Clamp(undoPosition, 0, undoHistory.Count - 1) + 1;
            if (undoPosition >= undoHistory.Count) return;
            var state = undoHistory[undoPosition];
            Code = state.code;
            SelectionAnchorPosition = state.caretStartPos;
            SelectionFocusPosition = state.caretEndPos;
        }

        public void RebuildLayout() { }

        private Dictionary<ICodeEditor.CodeEditCallback, UnityAction<string>> boundActions = new Dictionary<ICodeEditor.CodeEditCallback, UnityAction<string>>();

        [Serializable]
        public class OnEditorCloseEvent : UnityEvent<string> { }

        [SerializeField]
        private OnEditorCloseEvent onEditorClose = new OnEditorCloseEvent();

        /// <summary>
        /// Act as if the editor was closed but don't destroy it.
        /// </summary>
        public void SpoofClose()
        {

            onEditorClose?.Invoke(Code);
            onEditorClose?.RemoveAllListeners();

            if (inputFieldTMP != null)
            {
                inputFieldTMP.onValueChanged?.RemoveAllListeners();
                inputFieldTMP.onSubmit?.RemoveAllListeners();
                inputFieldTMP.onEndEdit?.RemoveAllListeners();
            }
            if (inputField != null)
            {
                inputField.onValueChanged?.RemoveAllListeners();
                inputField.onSubmit?.RemoveAllListeners();
                inputField.onEndEdit?.RemoveAllListeners();
            }

            undoHistory?.Clear();

            SetTitle("");
            Code = "";

            ListenForChanges(AddUndoState);

        } 

        protected void OnDestroy()
        {

            StopListeningForChanges(AddUndoState);

            onEditorClose?.Invoke(Code);
            onEditorClose?.RemoveAllListeners();

            boundActions?.Clear();
            boundActions = null;

            undoHistory?.Clear();
            undoHistory = null;

            if (inputFieldTMP != null && inputFieldTMP.verticalScrollbar != null && scrollBarVisibility != null) inputFieldTMP.verticalScrollbar.onValueChanged.RemoveListener(scrollBarVisibility);

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
            if (inputFieldTMP != null)
            {
                inputFieldTMP.onValueChanged?.AddListener(action);
                inputFieldTMP.onSubmit?.AddListener(action);
                inputFieldTMP.onEndEdit?.AddListener(action);
            }
            if (inputField != null)
            {
                inputField.onValueChanged?.AddListener(action);
                inputField.onSubmit?.AddListener(action);
                inputField.onEndEdit?.AddListener(action);
            }
        }

        public virtual bool StopListeningForChanges(ICodeEditor.CodeEditCallback callback)
        {
            if (boundActions == null) return false;
            if (!boundActions.TryGetValue(callback, out var action)) return false;
            if (inputFieldTMP != null)
            {
                inputFieldTMP.onValueChanged?.RemoveListener(action);
                inputFieldTMP.onSubmit?.RemoveListener(action);
                inputFieldTMP.onEndEdit?.RemoveListener(action);
            }
            if (inputField != null)
            {
                inputField.onValueChanged?.RemoveListener(action);
                inputField.onSubmit?.RemoveListener(action);
                inputField.onEndEdit?.RemoveListener(action);
            }
            return true;
        }

        [Header("UI")]
        public Text titleText;
        public TMP_Text titleTextTMP;

        public object TitleObject
        {

            get => titleTextTMP == null ? titleText : titleTextTMP;

            set
            {

                if (value is Text txt) titleText = txt; else if (value is TMP_Text txtTMP) titleTextTMP = txtTMP;

            }

        }

        public void SetTitle(string title)
        {
            if (titleText != null) titleText.text = title;
            if (titleTextTMP != null) titleTextTMP.SetText(title);
        }

        private UnityAction<float> scrollBarVisibility;

        protected void Awake()
        {

            if (inputFieldTMP != null)
            {
                inputFieldTMP.contentType = TMP_InputField.ContentType.Standard;
                inputFieldTMP.lineType = TMP_InputField.LineType.MultiLineNewline;
                if (inputFieldTMP.textComponent != null)
                {
                    inputFieldTMP.textComponent.enableWordWrapping = false;
                    inputFieldTMP.textComponent.overflowMode = TextOverflowModes.Overflow;
                }
                if (inputFieldTMP.verticalScrollbar != null)
                {
                    var canvasGroup = inputFieldTMP.verticalScrollbar.gameObject.AddOrGetComponent<CanvasGroup>();
                    canvasGroup.interactable = true;
                    canvasGroup.blocksRaycasts = true;
                    if (scrollBarVisibility == null) scrollBarVisibility = new UnityAction<float>((float val) => {

                        if (inputFieldTMP != null && inputFieldTMP.verticalScrollbar != null && canvasGroup != null) canvasGroup.alpha = inputFieldTMP.verticalScrollbar.size < 0.999f ? 1f : 0f;

                    });
                    inputFieldTMP.verticalScrollbar.onValueChanged.AddListener(scrollBarVisibility);
                } 
            }
            if (inputField != null)
            {
                inputField.contentType = InputField.ContentType.Standard;
                inputField.lineType = InputField.LineType.MultiLineNewline;
                if (inputField.textComponent != null)
                {
                    inputField.textComponent.horizontalOverflow = HorizontalWrapMode.Overflow;
                    inputField.textComponent.verticalOverflow = VerticalWrapMode.Overflow;
                }
            }

            ListenForChanges(AddUndoState);

        }

        public bool IsFocused => inputFieldTMP != null ? inputFieldTMP.isFocused : (inputField != null ? inputField.isFocused : false);

        protected void Update()
        {

            if (IsFocused)
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

        public void ClearAllListeners()
        {
            ClearChangeListeners();
            ClearClosureListeners();
        }

        public void ClearChangeListeners()
        {
            if (inputFieldTMP != null)
            {
                inputFieldTMP.onValueChanged?.RemoveAllListeners();
                inputFieldTMP.onSubmit?.RemoveAllListeners();
                inputFieldTMP.onEndEdit?.RemoveAllListeners();
            }
            if (inputField != null)
            {
                inputField.onValueChanged?.RemoveAllListeners();
                inputField.onSubmit?.RemoveAllListeners();
                inputField.onEndEdit?.RemoveAllListeners();
            }
        }

        public void ClearClosureListeners()
        {
            onEditorClose?.RemoveAllListeners();
        }
    }

}

#endif
