using System;

namespace Swole
{
    public interface IRevertableAction : IEditorState
    {
        void Revert();
        void Reapply();

        void PerpetuateUndo();
        bool GetUndoState();
        IRevertableAction SetUndoState(bool undone);
    }

    public class ActionBasedUndoSystem : UndoSystem
    {
         
        protected class InternalEditor : IEditor
        {
            public bool IsDirty => false;
            public void SetDirty() { }
            public void RegisterOnStateChangeListener(EditorStateChangeDelegate listener) {}
            public void RemoveOnStateChangeListener(EditorStateChangeDelegate listener) {}
            public void SetEditorState(IEditorState state) {}
            public IEditorState GetEditorState() => null;
        }
        public override IEditor Editor 
        { 
            get 
            {
                if (editor == null) editor = new InternalEditor();
                return editor;
            }
            
            set { } 
        }

        protected override void Perpetuate(IEditorState state)
        {
            if (state is IRevertableAction revertable)
            {
                if (revertable.GetUndoState())
                {
                    revertable.PerpetuateUndo();
                }
                else
                {
                    revertable.Perpetuate();
                }
            }
            else
            {
                state.Perpetuate();
            }
        }
        protected virtual void ApplyHistoryPosition(bool reapply, int historyPosition)
        {
            var action = history[historyPosition];
            if (action is IRevertableAction revertable)
            {
                if (reapply)
                {
                    revertable.Reapply();
                    revertable = revertable.SetUndoState(false);
                    history[historyPosition] = revertable;
                }
                else
                {
                    revertable.Revert();
                    revertable = revertable.SetUndoState(true);
                    history[historyPosition] = revertable;
                }
            }
            else
            {
                Editor.SetEditorState(action);
            }
        }
        public override int HistoryPosition
        {
            get => base.HistoryPosition;
            set
            {
                if (Editor == null) return;
                if (Count <= 0)
                {
                    historyPosition = -1;
                    return;
                }

                historyPosition = Math.Clamp(historyPosition, -1, Count - 1);
                int newHistoryPosition = Math.Clamp(value, -1, Count - 1);
                int step = Math.Sign(newHistoryPosition - HistoryPosition);
                if (step == 0) return;
                 
                while(historyPosition != newHistoryPosition)
                {
#if !UNITY_EDITOR
                    try
                    {
#endif
                        if (historyPosition >= 0) ApplyHistoryPosition(step > 0, historyPosition);
#if !UNITY_EDITOR
                    } 
                    catch(Exception e)
                    {
                        swole.LogError(e);
                    }
#endif
                    historyPosition += step;
                }

                if (step > 0 || historyPosition < 0) 
                {
                    ApplyHistoryPosition(step > 0, Math.Max(0, historyPosition));  
                }
            }
        }

    }
}
