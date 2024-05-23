#if SWOLE_ENV
using Miniscript;
#endif

namespace Swole.Script
{

    public struct DefaultRuntimeHost : IRuntimeHost
    {

        public PermissionScope Scope { get => PermissionScope.None; set { } } 
        public string identifier;
        public string Identifier => identifier;

        public object data;
        public object HostData => data;

        public IRuntimeEnvironment environment;
        public IRuntimeEnvironment Environment => environment;

        public string EnvironmentName { get => Environment == null ? null : Environment.EnvironmentName; set { if (Environment != null) Environment.EnvironmentName = value; } }

        public bool IsValid => Environment == null ? true : Environment.IsValid;

        public override string ToString() => identifier;

        public bool TryGetEnvironmentVar(string name, out IVar var)
        {
            var = null;
            return false;
        }

#if SWOLE_ENV
        public void SetLocalVar(string identifier, Value value)
        {
            if (Environment == null) return;
            Environment.SetLocalVar(identifier, value);
        }
        public bool TryGetLocalVar(string identifier, out Value value)
        {
            value = null;
            if (Environment == null) return false;
            return Environment.TryGetLocalVar(identifier, out value);
        }
        public Value GetLocalVar(string identifier)
        {
            if (Environment == null) return null;
            return Environment.GetLocalVar(identifier);
        }
        public ExecutionResult RunForPeriod(SwoleLogger logger, Interpreter interpreter, float timeOut = 0.01F, bool restartIfNotRunning = true, bool setGlobalVars = true)
        {
            if (Environment == null) return ExecutionResult.InvalidEnvironment;
            return Environment.RunForPeriod(logger, interpreter, timeOut, restartIfNotRunning, setGlobalVars);
        }
        public ExecutionResult RunUntilCompletion(SwoleLogger logger, Interpreter interpreter, float timeOut = 10, bool restart = true, bool setGlobalVars = true)
        {
            if (Environment == null) return ExecutionResult.InvalidEnvironment;
            return Environment.RunUntilCompletion(logger, interpreter, timeOut, restart, setGlobalVars);
        }
#endif

        #region Events

        public void TrackEventListener(RuntimeEventListener listener)
        {
            if (Environment == null) return;
            environment.TrackEventListener(listener);
        }
        public bool UntrackEventListener(RuntimeEventListener listener)
        {
            if (Environment == null) return false;
            return environment.UntrackEventListener(listener);
        }

        public bool UntrackEventListener(string trackerId)
        {
            if (Environment == null) return false;
            return environment.UntrackEventListener(trackerId);
        }

        public RuntimeEventListener FindPreEventListener(RuntimeEventListenerDelegate _delegate, IRuntimeEventHandler handler)
        {
            if (Environment == null) return null;
            return environment.FindPreEventListener(_delegate, handler);
        }

        public RuntimeEventListener FindPostEventListener(RuntimeEventListenerDelegate _delegate, IRuntimeEventHandler handler)
        {
            if (Environment == null) return null;
            return environment.FindPostEventListener(_delegate, handler);
        }

        public RuntimeEventListener FindPreEventListener(ValFunction function, IRuntimeEventHandler handler)
        {
            if (Environment == null) return null;
            return environment.FindPreEventListener(function, handler);
        }

        public RuntimeEventListener FindPostEventListener(ValFunction function, IRuntimeEventHandler handler)
        {
            if (Environment == null) return null;
            return environment.FindPostEventListener(function, handler);
        }

        public bool HasEventHandler => EventHandler != null;
        public IRuntimeEventHandler EventHandler => null;

        #endregion

        public ContentPackage LocalContent => null;

        public void Dispose()
        {
        }

        public void ListenForQuit(VoidParameterlessDelegate listener)
        {
        }
        public void StopListeningForQuit(VoidParameterlessDelegate listener)
        {
        }

        public bool TryGetReferencePackage(PackageIdentifier pkgId, out ContentPackage package)
        {
            package = ContentManager.FindPackage(pkgId);
            return package != null;
        }
        public bool TryGetReferencePackage(string pkgString, out ContentPackage package)
        {
            if (pkgString != null && pkgString.IndexOf(SwoleScriptSemantics.ssVersionPrefix) >= 0) return TryGetReferencePackage(new PackageIdentifier(pkgString), out package);
            package = ContentManager.FindPackage(pkgString);
            return package != null;
        }

        public SwoleCancellationToken GetNewCancellationToken() => null;

        public void RemoveToken(SwoleCancellationToken token) {}

        public void CancelAllTokens() {}

    }

}
