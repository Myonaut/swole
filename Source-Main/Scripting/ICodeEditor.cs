namespace Swole.Script
{

    public interface ICodeEditor
    {

        public string Code { get; set; }

        public int UndoHistoryCapacity { get; set; }

        public void Undo();
        public void Redo();

        public delegate void CodeEditCallback(string code);

        public void ListenForChanges(CodeEditCallback callback);

        public bool StopListeningForChanges(CodeEditCallback callback);

        public void ListenForClosure(CodeEditCallback callback);

        public bool StopListeningForClosure(CodeEditCallback callback);

    }

}
