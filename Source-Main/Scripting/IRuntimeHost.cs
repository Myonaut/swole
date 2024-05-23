namespace Swole.Script
{

    public interface IRuntimeHost : IRuntimeEnvironment
    {

        public PermissionScope Scope { get; set; }

        public string Identifier { get; }

        public object HostData { get; }

        public bool TryGetEnvironmentVar(string name, out IVar envVar);

        public void ListenForQuit(VoidParameterlessDelegate listener);
        public void StopListeningForQuit(VoidParameterlessDelegate listener);

        public ContentPackage LocalContent { get; } 

        public bool TryGetReferencePackage(PackageIdentifier pkgId, out ContentPackage package);
        public bool TryGetReferencePackage(string pkgString, out ContentPackage package);

    }

}
