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

        public bool ReapplyWhenRevertedTo { get; }
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
            bool stepping = steppingThroughHistory; 
            steppingThroughHistory = true;

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

            steppingThroughHistory = stepping;
        }
        public override int HistoryPosition
        {
            get => base.HistoryPosition;
            set
            {
                if (Editor == null) return;

                int prevPosition = historyPosition;
                if (Count <= 0)
                {
                    historyPosition = -1;
                    if (historyPosition != prevPosition) NotifyHistoryPositionListeners(prevPosition, historyPosition);
                    return;
                }

                historyPosition = Math.Clamp(historyPosition, -1, Count - 1);
                int newHistoryPosition = Math.Clamp(value, -1, Count - 1);
                int sign = Math.Sign(newHistoryPosition - HistoryPosition); 
                if (sign == 0) return;

                steppingThroughHistory = true;
                try
                {
                    if (sign > 0)
                    {
                        while (historyPosition != newHistoryPosition)
                        {
                            historyPosition += 1;
#if !UNITY_EDITOR
                    try
                    {
#endif
                            if (historyPosition >= 0) ApplyHistoryPosition(true, historyPosition);
#if !UNITY_EDITOR
                    } 
                    catch(Exception e)
                    {
                        swole.LogError(e);
                    }
#endif
                        }
                    }
                    else if (sign < 0)
                    {
                        while (historyPosition != newHistoryPosition)
                        {
#if !UNITY_EDITOR
                    try
                    {
#endif
                            if (historyPosition >= 0) ApplyHistoryPosition(false, historyPosition);
#if !UNITY_EDITOR
                    } 
                    catch(Exception e)
                    {
                        swole.LogError(e);
                    }
#endif
                            historyPosition -= 1;
                            if (historyPosition >= 0)
                            {
                                var action = history[historyPosition];
                                if (action is IRevertableAction ra && ra.ReapplyWhenRevertedTo) ApplyHistoryPosition(true, historyPosition); 
                            }
                        }
                    }
                      
                    if (historyPosition != prevPosition) NotifyHistoryPositionListeners(prevPosition, historyPosition);
                } 
                catch(Exception ex)
                {
                    steppingThroughHistory = false;
                    throw ex;
                }

                steppingThroughHistory = false;  
            }
        }

    }
}
