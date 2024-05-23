using Swole.Script;

namespace Swole
{
    public interface IExecutableGameplayExperience : IRuntimeHost
    {

        public ICreationInstance CreationInstance { get; }

        public void Load(EngineInternal.Vector3 positionInWorld = default, EngineInternal.Quaternion rotationInWorld = default, EngineInternal.Vector3 scaleInWorld = default);
        public void Unload();

        public void Begin();
        public void End();
        public void Restart();

        public void SaveProgress(string path);
        public void LoadProgress(string path);

    }
}
