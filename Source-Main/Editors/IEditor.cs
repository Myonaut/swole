namespace Swole
{
    public delegate void EditorStateChangeDelegate(IEditorState oldState, IEditorState newState);

    public interface IEditor
    {

        bool IsDirty
        {
            get;
        }
        void SetDirty();

        IEditorState GetEditorState();
        void SetEditorState(IEditorState state);

        void RemoveOnStateChangeListener(EditorStateChangeDelegate listener);
        void RegisterOnStateChangeListener(EditorStateChangeDelegate listener);

    }

    public interface IEditorState
    {
        void Perpetuate();
    }
}
