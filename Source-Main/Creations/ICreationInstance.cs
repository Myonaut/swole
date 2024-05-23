using Swole.Script;

namespace Swole
{
    public interface ICreationInstance : EngineInternal.IComponent, IRuntimeHost
    {

        public Creation Asset { get; }
        public PackageIdentifier Package { get; }
        public string AssetName { get; }

        public bool IsInitialized { get; }
        public bool IsExecuting { get; }

        public EngineInternal.GameObject Root { get; }
        public ICreationInstance RootCreation { get; }

        public ExecutableBehaviour Behaviour { get; }

        public EngineInternal.GameObject FindGameObject(string name);
        public EngineInternal.SwoleGameObject FindSwoleGameObject(int id);
        public const string _idDelimiter = ".";
        public EngineInternal.SwoleGameObject FindSwoleGameObject(string ids);

        public bool Initialize(bool startExecuting = true);
        public bool StartExecuting();
        public void StopExecuting();

    }
}
