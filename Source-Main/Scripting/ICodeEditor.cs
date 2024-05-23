using Swole.UI;

namespace Swole.Script
{

    public interface ICodeEditor : IRebuildable
    {

        public object RootObject { get; set; }

        public object TitleObject { get; set; }

        public void SetTitle(string title);

        public string Code { get; set; }

        public int UndoHistoryCapacity { get; set; }

        /// <summary>
        /// Act as if the editor was closed but don't destroy it.
        /// </summary>
        public void SpoofClose();

        public void Undo();
        public void Redo();

        public delegate void CodeEditCallback(string code);

        public void ClearAllListeners();

        public void ListenForChanges(CodeEditCallback callback);
        public bool StopListeningForChanges(CodeEditCallback callback);
        public void ClearChangeListeners();

        public void ListenForClosure(CodeEditCallback callback);
        public bool StopListeningForClosure(CodeEditCallback callback);
        public void ClearClosureListeners();

    }

}
