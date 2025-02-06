using Swole.Script;
using System.Collections.Generic;

#if SWOLE_ENV
using Miniscript;
#endif

namespace Swole
{
    public class RuntimeHandler : SingletonBehaviour<RuntimeHandler>
    {

        public const int _defaultCompletionTimeout = 5;

        public class Experience : IExecutableGameplayExperience
        {

            #region Events
            public void TrackEventListener(RuntimeEventListener listener)
            {
                if (instance == null) return;
                instance.TrackEventListener(listener);
            }
            public bool UntrackEventListener(RuntimeEventListener listener)
            {
                if (instance == null) return false;
                return instance.UntrackEventListener(listener);
            }

            public bool UntrackEventListener(string trackerId)
            {
                if (instance == null) return false;
                return instance.UntrackEventListener(trackerId);
            }

            public RuntimeEventListener FindPreEventListener(RuntimeEventListenerDelegate _delegate, IRuntimeEventHandler handler)
            {
                if (instance == null) return null;
                return instance.FindPreEventListener(_delegate, handler);
            }

            public RuntimeEventListener FindPostEventListener(RuntimeEventListenerDelegate _delegate, IRuntimeEventHandler handler)
            {
                if (instance == null) return null;
                return instance.FindPostEventListener(_delegate, handler);
            }

#if SWOLE_ENV
            public RuntimeEventListener FindPreEventListener(ValFunction function, IRuntimeEventHandler handler)
            {
                if (instance == null) return null;
                return instance.FindPreEventListener(function, handler);
            }

            public RuntimeEventListener FindPostEventListener(ValFunction function, IRuntimeEventHandler handler)
            {
                if (instance == null) return null;
                return instance.FindPostEventListener(function, handler);
            }
#endif

            public bool HasEventHandler => EventHandler != null;
            public IRuntimeEventHandler EventHandler => instance == null ? null : instance.EventHandler;
            #endregion

            protected bool invalid;
            public bool IsValidExperience => !invalid;

            public void Dispose() => Invalidate();
            public void Invalidate()
            {
                if (hasStarted)
                {
                    End();
                }
                if (isLoaded)
                {
                    Unload();
                }
                if (instance != null) instance.Dispose();

                instance = null;
                invalid = true;
                onEndListeners = null;
            }

            protected bool isLoaded;
            public bool IsLoaded => isLoaded;

            protected bool hasStarted;
            public bool HasStarted => hasStarted;

            protected Creation creation;
            public Creation Creation => creation;

            protected ContentPackage homePackage;
            public ContentPackage LocalContent => homePackage;

            public SwoleLogger logger;

            public Experience(Creation creation, SwoleLogger logger = null)
            {
                this.creation = creation;
                if (creation != null) homePackage = ContentManager.FindPackage(creation.PackageInfo); 

                if (logger == null) logger = swole.DefaultLogger;
                this.logger = logger;
            }

            protected ICreationInstance instance;
            public ICreationInstance CreationInstance => instance;

            public string Identifier => instance == null ? null : instance.Identifier;
            public object HostData => instance == null ? null : instance.HostData;

            public IRuntimeEnvironment Environment => instance == null ? null : instance.Environment;
            public string EnvironmentName { get => instance == null ? null : instance.EnvironmentName; set { if (instance != null) instance.EnvironmentName = value; } }
            public bool IsValid => instance == null ? false : instance.IsValid;
#if SWOLE_ENV
            public void SetLocalVar(string identifier, Value value)
            {
                if (instance == null) return;
                instance.SetLocalVar(identifier, value);
            }
            public bool TryGetLocalVar(string identifier, out Value value)
            {
                value = null;
                if (instance == null) return false;
                return instance.TryGetLocalVar(identifier, out value);
            }
            public Value GetLocalVar(string identifier)
            {
                if (instance == null) return null;
                return instance.GetLocalVar(identifier);
            }
            public ExecutionResult RunForPeriod(SwoleLogger logger, Interpreter interpreter, float timeOut = 0.01F, bool restartIfNotRunning = true, bool setGlobalVars = true)
            {
                if (instance == null) return ExecutionResult.InvalidEnvironment;
                return instance.RunForPeriod(logger, interpreter, timeOut, restartIfNotRunning, setGlobalVars);
            }
            public ExecutionResult RunUntilCompletion(SwoleLogger logger, Interpreter interpreter, float timeOut = 10, bool restart = true, bool setGlobalVars = true)
            {
                if (instance == null) return ExecutionResult.InvalidEnvironment;
                return instance.RunUntilCompletion(logger, interpreter, timeOut, restart, setGlobalVars);
            }
#endif

            protected PermissionScope scope = PermissionScope.GameplayExperience;
            public PermissionScope Scope
            {
                get
                {
                    if (instance == null) return scope;
                    return instance.Scope;
                }
                set
                {
                    if (instance == null) return;
                    scope = value;
                    instance.Scope = value;
                }
            }

            public bool TryGetEnvironmentVar(string name, out IVar var) => instance.TryGetEnvironmentVar(name, out var);

            public void Load(EngineInternal.Vector3 positionInWorld = default, EngineInternal.Quaternion rotationInWorld = default, EngineInternal.Vector3 scaleInWorld = default)
            {
                if (invalid || isLoaded) return;
                if (rotationInWorld.IsZero) rotationInWorld = EngineInternal.Quaternion.identity;
                if (scaleInWorld.IsZero) scaleInWorld = EngineInternal.Vector3.one;

                if (instance != null && !instance.IsDestroyed) EngineInternal.GameObject.AdminDestroy(instance.Root);   

                instance = swole.Engine.CreateNewCreationInstance(creation, false, EngineInternal.Vector3.zero, EngineInternal.Quaternion.identity, positionInWorld, rotationInWorld, scaleInWorld, false, logger);
                instance.Scope = scope;

                if (instance.Behaviour != null)
                {
                    instance.Behaviour.ExecuteToCompletion(ExecutionLayer.Load, _defaultCompletionTimeout, logger);
                }

                isLoaded = true;
            }
            public void Unload()
            {
                if (invalid || !isLoaded) return;

                if (hasStarted) End();

                if (instance != null)
                {
                    if (instance.Behaviour != null)
                    {
                        instance.Behaviour.ExecuteToCompletion(ExecutionLayer.Unload, _defaultCompletionTimeout, logger);
                    }

                    if (!instance.IsDestroyed) EngineInternal.GameObject.AdminDestroy(instance.Root); 
                }

                isLoaded = false;
            }

            public void Begin()
            {
                if (invalid || hasStarted) return;

                if (instance != null)
                {
                    if (instance.Behaviour != null)
                    {
                        instance.Behaviour.ExecuteToCompletion(ExecutionLayer.Begin, _defaultCompletionTimeout, logger);
                        instance.Initialize();
                    }
                }

                hasStarted = true;
            }

            protected List<VoidParameterlessDelegate> onEndListeners = new List<VoidParameterlessDelegate>(); 
            public void ListenForQuit(VoidParameterlessDelegate listener)
            {
                if (listener == null || invalid) return;
                onEndListeners?.Add(listener);
            }

            public void StopListeningForQuit(VoidParameterlessDelegate listener)
            {
                if (listener == null || invalid) return;
                onEndListeners?.RemoveAll(i => i == listener); 
            }
            public void End()
            {
                if (invalid || !hasStarted) return;

                CancelAllTokens();
                if (instance != null)
                {
                    if (instance.Behaviour != null)
                    {
                        instance.Behaviour.ExecuteToCompletion(ExecutionLayer.End, _defaultCompletionTimeout, logger);
                    }
                }

                hasStarted = false;

                foreach(var endListener in onEndListeners) endListener();
                onEndListeners.Clear();
                
            }
            public void Restart()
            {
                if (invalid) return;

                if (instance != null)
                {
                    if (instance.Behaviour != null)
                    {
                        instance.Behaviour.ExecuteToCompletion(ExecutionLayer.Restart, _defaultCompletionTimeout, logger);
                    }
                }

                End();
                Begin();
            }

            public const string _serializedProgressIdentifier = "_serializedProgress"; 
            public void SaveProgress(string path)
            {
                if (invalid || !hasStarted || instance == null) return;
                if (instance.Behaviour != null)
                {
#if SWOLE_ENV
                    ValMap data = new ValMap();
                    instance.Environment.SetLocalVar(_serializedProgressIdentifier, data);
#endif
                    instance.Behaviour.ExecuteToCompletion(ExecutionLayer.SaveProgress, _defaultCompletionTimeout, logger);
#if SWOLE_ENV
                    var output = instance.Environment.GetLocalVar(_serializedProgressIdentifier); 
                    if (output is ValMap map) data = map; 
#endif
                }
            }
            public void LoadProgress(string path)
            {
                if (invalid || !hasStarted || instance == null) return;
                if (instance.Behaviour != null)
                {
#if SWOLE_ENV
                    ValMap data = new ValMap();
                    instance.Environment.SetLocalVar(_serializedProgressIdentifier, data);
#endif
                    instance.Behaviour.ExecuteToCompletion(ExecutionLayer.LoadProgress, _defaultCompletionTimeout, logger);
                } 
            }

            public bool TryGetReferencePackage(PackageIdentifier pkgId, out ContentPackage package)
            {
                package = null;
                if (instance == null) return false;

                return instance.TryGetReferencePackage(pkgId, out package);
            }

            public bool TryGetReferencePackage(string pkgString, out ContentPackage package)
            {
                package = null;
                if (instance == null) return false;

                return instance.TryGetReferencePackage(pkgString, out package); 
            }

            public SwoleCancellationToken GetNewCancellationToken()
            {
                if (instance == null) return null;
                return instance.GetNewCancellationToken();
            }

            public void RemoveToken(SwoleCancellationToken token)
            {
                if (instance == null) return;
                instance.RemoveToken(token);
            }

            public void CancelAllTokens()
            {
                if (instance == null) return;
                instance.CancelAllTokens();
            }
        }

        protected readonly List<Experience> loadedExperiences = new List<Experience>();

        public static Experience InitiateNewExperience(Creation creation, SwoleLogger logger = null)
        {
            if (creation == null) return null;
            var instance = Instance;
            if (instance == null) return null;

            var exp = new Experience(creation, logger); 
            instance.loadedExperiences.Add(exp);   
            return exp;
        }

        public static bool IsALoadedExperience(EngineInternal.IEngineObject eo)
        {
            if (eo == null) return false;

            var instance = Instance;
            if (instance == null) return false;

            ICreationInstance inst = null;
            EngineInternal.GameObject go = default;
            if (eo is ICreationInstance) 
            { 
                inst = (ICreationInstance)eo;
            }
            else if (eo.Instance is ICreationInstance) 
            { 
                inst = (ICreationInstance)eo.Instance;
            } 
            else if (eo is EngineInternal.IComponent comp)
            {
                go = comp.baseGameObject;
            } 
            else if (eo is EngineInternal.GameObject)
            {
                go = (EngineInternal.GameObject)eo;
            }

            if (go != null)
            {
                var ci_comp = go.GetComponent(typeof(ICreationInstance));
                if (ci_comp is ICreationInstance) inst = (ICreationInstance)ci_comp;
            }

            if (inst == null) return false;

            foreach (var exp in instance.loadedExperiences) if (exp != null && exp.CreationInstance != null && exp.CreationInstance.Instance == inst.Instance) return true;

            return false;
        }

        public override void OnFixedUpdate()
        {
        }

        public override void OnLateUpdate()
        {
            loadedExperiences.RemoveAll(i => !i.IsValidExperience);
        }

        public override void OnUpdate()
        {

        }

        protected EngineInternal.Camera mainCamera;
        public static EngineInternal.Camera MainCamera
        {
            get
            {
                var instance = Instance;
                if (instance == null) return default;

                if (instance.mainCamera.IsDestroyed) instance.mainCamera = EngineInternal.Camera.main;
                return instance.mainCamera;
            }

            set 
            {
                var instance = Instance; 
                if (instance == null) return;

                instance.mainCamera = value;
            }
        }

        protected int uniqueId = int.MinValue;
        public static int NextUniqueId
        {
            get
            {
                var instance = Instance;
                if (instance == null) return 0;

                if (instance.uniqueId == 0) instance.uniqueId++;
                int id = instance.uniqueId;
                instance.uniqueId++;
                if (instance.uniqueId == 0) instance.uniqueId++;
                return id;
            }
        }
        public static void ResetUniqueIDCounter()
        {
            var instance = Instance;
            if (instance == null) return;

            instance.uniqueId = int.MinValue;
        }

    }
}
