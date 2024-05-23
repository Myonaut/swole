using System;
using System.Collections.Generic;

namespace Swole
{
    public class UndoSystem
    {
        public int maxHistorySize = 25;

        protected IEditor editor;
        public virtual IEditor Editor
        {
            get => editor;
            set
            {
                if (editor != null) editor.RemoveOnStateChangeListener(OnEditorStateChange);
                editor = value;
                if (editor != null) editor.RegisterOnStateChangeListener(OnEditorStateChange); // Listen for changes and record them to history
                
            }
        }

        protected readonly List<IEditorState> history = new List<IEditorState>();
        public virtual int Count => history.Count;
        public virtual IEditorState GetHistory(int index)
        {
            if (index < 0 || index >= Count) return default;
            return history[index];
        }
        public virtual IEditorState this[int index]
        {
            get => GetHistory(index);
        }

        protected int historyPosition;
        public virtual int HistoryPosition
        {
            get => historyPosition;
            set
            {
                if (Editor == null) return;
                if (Count <= 0)
                {
                    historyPosition = 0;
                    return;
                }
                historyPosition = Math.Clamp(value, 0, Count - 1);
                editor.SetEditorState(history[historyPosition]);
            }
        }

        public virtual void Undo()
        {
            if (historyPosition >= Count - 1)
            {
                if (Editor != null && editor.IsDirty) AddHistory(editor.GetEditorState());
            }
            HistoryPosition--;
        }
        public virtual void Redo()
        {
            HistoryPosition++;
        }

        public void ClearHistory() 
        {
            foreach (var state in history) Perpetuate(state);
            ClearHistoryWithoutPerpetuation();
        }
        public void ClearHistoryWithoutPerpetuation()
        {
            history.Clear(); 
        }
        protected virtual void Perpetuate(IEditorState state)
        {
            state.Perpetuate();
        }
        /// <param name="startIndex">The start index in the history list (inclusive)</param>
        /// <param name="endIndex">The end index in the history list (inclusive)</param>
        public virtual void RemoveHistory(int startIndex, int endIndex = -1)
        {
            if (endIndex < 0) endIndex = startIndex;
            startIndex = Math.Clamp(startIndex, 0, Count - 1);
            endIndex = Math.Clamp(endIndex, 0, Count - 1);

            int temp = startIndex;
            if (startIndex > endIndex)
            {
                startIndex = endIndex;
                endIndex = temp;
            }

            int count = (endIndex - startIndex) + 1;
            if (count <= 0) return;
            if (count == 1)
            {
                var state = history[startIndex];
                Perpetuate(state);
                history.RemoveAt(startIndex);
                if (historyPosition > startIndex) historyPosition--;
            }
            else
            {
                for (int a = count - 1; a >= 0; a--)
                {
                    var state = history[startIndex + a];
                    Perpetuate(state);
                }
                history.RemoveRange(startIndex, count);
                if (historyPosition > startIndex) historyPosition -= count; 
            }
        }
        public virtual void AddHistory(IEditorState state)
        {
            if (Count - 1 > historyPosition) RemoveHistory(historyPosition + 1, Count - 1);   
            history.Add(state);
            if (Count > maxHistorySize) RemoveHistory(0, Count - 1 - maxHistorySize);
            historyPosition = Count - 1;
        }
        public virtual void OnEditorStateChange(IEditorState oldState, IEditorState newState)
        {
            if (Count <= 0) AddHistory(oldState);
            AddHistory(newState);
        }
    }
}
