#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

#if SWOLE_ENV
using Miniscript;
#endif

using Swole.Script;

namespace Swole.API.Unity
{

    /// <summary>
    /// An in-engine instance of a Creation object.
    /// </summary>
    public class CreationBehaviour : MonoBehaviour, ICreationInstance, IRuntimeEventHandler
    {

        public System.Type EngineComponentType => GetType();

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


        public int InstanceID => GetInstanceID();
        public EngineInternal.GameObject baseGameObject => UnityEngineHook.AsSwoleGameObject(gameObject);
        public bool IsDestroyed => destroyed;

        public void Destroy(float timeDelay = 0) => EngineInternal.EngineObject.Destroy(this, timeDelay);
        public void AdminDestroy(float timeDelay = 0) => EngineInternal.EngineObject.AdminDestroy(this, timeDelay);

        [NonSerialized]
        private SwoleLogger m_logger;
        public SwoleLogger Logger => m_logger;

        [NonSerialized]
        private int m_executionPriority;

        [NonSerialized]
        private Creation m_creation;
        public Creation Creation => m_creation;
        public bool IsEditable
        {
            get
            {
                if (swole.State != swole.RuntimeState.Editor && swole.State != swole.RuntimeState.EditorPlayTest) return false;
                return RuntimeHandler.IsALoadedExperience(this);
            }
        }
        public bool HasScripting => IsEditable || (m_creation == null ? false : m_creation.HasScripting);
        public Creation Asset => Creation;

        protected ContentPackage homePackage;
        public ContentPackage LocalContent => homePackage;

        public PackageIdentifier Package => Asset == null ? default : Asset.PackageInfo;
        public string AssetName => Asset == null ? string.Empty : Asset.Name;

        protected List<EngineInternal.TileInstance> tileInstances;
        protected List<EngineInternal.ITransform> objectInstances;

        public static CreationBehaviour AddToObject(GameObject obj, Creation creation, ICollection<IVar> environmentVars = null, ICollection<EngineInternal.TileInstance> tileInstances = default, ICollection<EngineInternal.ITransform> objectInstances = default, bool autoInitialize = true, int executionPriority = 0, SwoleLogger logger = null)
        {
            if (obj == null) return null;

            var b = obj.AddComponent<CreationBehaviour>(); 

            b.m_creation = creation;
            if (creation != null) b.homePackage = ContentManager.FindPackage(creation.PackageInfo);

            b.m_environmentVars = environmentVars;
            b.dontAutoInitialized = !autoInitialize;
            b.m_executionPriority = executionPriority;
            b.m_logger = logger;

            if (tileInstances != null && tileInstances.Count > 0) b.tileInstances = new List<EngineInternal.TileInstance>(tileInstances); 
            if (objectInstances != null && objectInstances.Count > 0) b.objectInstances = new List<EngineInternal.ITransform>(objectInstances);

            return b; 
        }

        private static readonly List<EngineInternal.TileInstance> _tempTileInstances = new List<EngineInternal.TileInstance>();
        private static readonly List<EngineInternal.ITransform> _tempObjectInstances = new List<EngineInternal.ITransform>();
        // TODO: Load environment vars from creation asset
        public static CreationBehaviour New(Creation creation, bool useRealTransformsOnly, Vector3 position = default, Quaternion rotation = default, ICollection<IVar> environmentVars = null, bool autoInitialize = true, int executionPriority = 0, SwoleLogger logger = null)
        {
            if (creation == null) return null;
            _tempTileInstances.Clear();
            _tempObjectInstances.Clear();
            var b = AddToObject(UnityEngineHook.AsUnityGameObject(creation.CreateNewRootAndObjects(useRealTransformsOnly, UnityEngineHook.AsSwoleVector(position), UnityEngineHook.AsSwoleQuaternion(rotation), _tempObjectInstances, _tempTileInstances)), creation, environmentVars, _tempTileInstances, _tempObjectInstances, autoInitialize, executionPriority, logger);
            _tempTileInstances.Clear();
            _tempObjectInstances.Clear();
            return b;
        }

        /// <summary>
        /// Used to create a non-executing prefab object for creation editors
        /// </summary>
        public static GameObject CreatePreRuntimeCreationPrefab(Creation creation, bool addPrototypeComponents, bool addSwoleGameObjectComponents, GameObject rootPrefab = null, List<EngineInternal.ITransform> outputInstanceList = null, List<EngineInternal.TileInstance> outputTileList = null, bool createPrefabRootIfNotProvided = true)
        {

            if (creation == null) return null;  

            GameObject prefab = rootPrefab == null ? (createPrefabRootIfNotProvided ? new GameObject(creation.Name) : null) : rootPrefab;
            Transform prefabTransform = prefab == null ? null : prefab.transform; 

            void SpawnTiles(TileSpawnGroup tsg, EngineInternal.ITransform rootTransform, List<EngineInternal.TileInstance> tileList)
            {
                var tileSet = BulkOutIntermediaryHook.AsBulkOutTileSet(tsg.TileSet);
                for (int b = 0; b < tsg.ObjectSpawnCount; b++)
                {
                    var spawner = tsg.GetObjectSpawner(b);

                    var tile = tileSet == null ? null : tileSet.CreatePreRuntimeTilePrefab(spawner.index, tileSet.Source);
                    if (tile == null) tile = new GameObject("null_tile");
                    if (!string.IsNullOrEmpty(spawner.name)) tile.name = spawner.name;

                    var tileTransform = tile.transform;
                    if (prefabTransform != null) tileTransform.SetParent(prefabTransform, false);
                    tileTransform.localPosition = UnityEngineHook.AsUnityVector(spawner.positionInRoot);
                    tileTransform.localRotation = UnityEngineHook.AsUnityQuaternion(spawner.rotationInRoot);
                    tileTransform.localScale = UnityEngineHook.AsUnityVector(spawner.localScale);

                    if (addPrototypeComponents)
                    {
                        var prototype = tile.AddOrGetComponent<TilePrototype>();
                        prototype.tileIndex = spawner.index;
                        prototype.tileSet = tileSet;
                        tileList.Add(new EngineInternal.TileInstance(prototype));
                    } 
                    else if (outputTileList != null || outputInstanceList != null)
                    {
                        tileList.Add(new EngineInternal.TileInstance(new TileInstance(tileSet, spawner.index, tile.transform)));
                    }

                    if (addSwoleGameObjectComponents && spawner.ID >= 0)
                    {
                        var sgo = tile.AddOrGetComponent<SwoleGameObject>();
                        sgo.id = spawner.ID;
                    }
                }

            }
            void SpawnObjects(ObjectSpawnGroup osg, EngineInternal.ITransform rootTransform, List<EngineInternal.ITransform> instanceList)
            {
                if (osg is CreationSpawnGroup csg)
                {
                    var childCreation = ContentManager.FindContent<Creation>(csg.AssetName, csg.PackageIdentity);
                    for (int b = 0; b < csg.ObjectSpawnCount; b++)
                    {
                        var spawner = csg.GetObjectSpawner(b);
                        
                        GameObject obj;
                        if (childCreation != null)
                        {
                            obj = CreatePreRuntimeCreationPrefab(childCreation, false, false);
                            if (!string.IsNullOrEmpty(spawner.name)) obj.name = spawner.name;
                        } 
                        else
                        {
                            obj = new GameObject(string.IsNullOrEmpty(spawner.name) ? $"load_fail:{csg.AssetName}" : spawner.name);
                        }

                        var objTransform = obj.transform;
                        if (prefabTransform != null) objTransform.SetParent(prefabTransform, false);
                        objTransform.localPosition = UnityEngineHook.AsUnityVector(spawner.positionInRoot);
                        objTransform.localRotation = UnityEngineHook.AsUnityQuaternion(spawner.rotationInRoot);
                        objTransform.localScale = UnityEngineHook.AsUnityVector(spawner.localScale);

                        if (addPrototypeComponents)
                        {
                            var prototype = obj.AddOrGetComponent<CreationPrototype>();
                            if (childCreation != null) prototype.asset = childCreation;
                            prototype.packageId = csg.PackageIdentityString;
                            prototype.creationId = csg.AssetName;
                        }

                        if (addSwoleGameObjectComponents && spawner.ID >= 0)
                        {
                            var sgo = obj.AddOrGetComponent<SwoleGameObject>();
                            sgo.id = spawner.ID;
                        }

                        instanceList.Add(UnityEngineHook.AsSwoleTransform(obj.transform)); 
                    }
                } 
                else
                {
                    for (int b = 0; b < osg.ObjectSpawnCount; b++)
                    {
                        var spawner = osg.GetObjectSpawner(b);

                        var obj = new GameObject("unknown_obj");
                        if (!string.IsNullOrEmpty(spawner.name)) obj.name = spawner.name;

                        var objTransform = obj.transform;
                        if (prefabTransform != null) objTransform.SetParent(prefabTransform, false);
                        objTransform.localPosition = UnityEngineHook.AsUnityVector(spawner.positionInRoot);
                        objTransform.localRotation = UnityEngineHook.AsUnityQuaternion(spawner.rotationInRoot);
                        objTransform.localScale = UnityEngineHook.AsUnityVector(spawner.localScale); 

                        if (addSwoleGameObjectComponents && spawner.ID >= 0)
                        {
                            var sgo = obj.AddOrGetComponent<SwoleGameObject>();
                            sgo.id = spawner.ID;
                        }

                        instanceList.Add(UnityEngineHook.AsSwoleTransform(obj.transform));
                    }
                }

            }
            
            creation.CreateNewRootAndObjects(true, null, outputInstanceList, outputTileList, SpawnTiles, SpawnObjects);

            return prefab;

        }

        [NonSerialized]
        private RuntimeEnvironment m_environment;
        public IRuntimeEnvironment Environment
        {

            get
            {
                if (m_environment == null) 
                {
                    if (destroyed || m_creation == null || !HasScripting) return null;
                    m_environment = new RuntimeEnvironment(Identifier + environmentSuffix, m_environmentVars);   
                }

                return m_environment;
            }

        }
        public SwoleCancellationToken GetNewCancellationToken()
        {
            if (Environment == null) return null;
            return m_environment.GetNewCancellationToken();
        }

        public void RemoveToken(SwoleCancellationToken token)
        {
            if (m_environment == null) return;
            m_environment.RemoveToken(token);
        }

        public void CancelAllTokens()
        {
            if (m_environment == null) return;
            m_environment.CancelAllTokens();
        }
        public string EnvironmentName
        {
            get
            {
                if (Environment == null) return name;
                return m_environment.EnvironmentName;
            }
            set
            {
                if (Environment == null)
                {
                    name = value;
                }
                else
                {
                    m_environment.name = value;
                }
            }
        }
        public bool IsValid
        {
            get
            {
                if (IsDestroyed) return false;
                return Environment == null ? false : m_environment.IsValid;
            }
        }

#if SWOLE_ENV
        public void SetLocalVar(string identifier, Value value)
        {
            if (Environment == null) return;
            m_environment.SetLocalVar(identifier, value);
        }

        public bool TryGetLocalVar(string identifier, out Value value)
        {
            value = null;
            if (Environment == null) return false;
            return m_environment.TryGetLocalVar(identifier, out value);
        }

        public Value GetLocalVar(string identifier)
        {
            if (Environment == null) return null;
            return m_environment.GetLocalVar(identifier);
        }

        public ExecutionResult RunForPeriod(SwoleLogger logger, Interpreter interpreter, float timeOut = 0.01F, bool restartIfNotRunning = true, bool setGlobalVars = true) 
        {
            if (Environment == null) return ExecutionResult.InvalidEnvironment;
            return m_environment.RunForPeriod(logger, interpreter, timeOut, restartIfNotRunning, setGlobalVars);
        }

        public ExecutionResult RunUntilCompletion(SwoleLogger logger, Interpreter interpreter, float timeOut = 10, bool restart = true, bool setGlobalVars = true)
        {
            if (Environment == null) return ExecutionResult.InvalidEnvironment;
            return m_environment.RunUntilCompletion(logger, interpreter, timeOut, restart, setGlobalVars); 
        }
#endif

        #region Events

        public void TrackEventListener(RuntimeEventListener listener)
        {
            if (Environment == null) return;
            m_environment.TrackEventListener(listener);
        }
        public bool UntrackEventListener(RuntimeEventListener listener)
        {
            if (Environment == null) return false;
            return m_environment.UntrackEventListener(listener);
        }

        public bool UntrackEventListener(string trackerId)
        {
            if (Environment == null) return false;
            return m_environment.UntrackEventListener(trackerId);
        }

        public RuntimeEventListener FindPreEventListener(RuntimeEventListenerDelegate _delegate, IRuntimeEventHandler handler)
        {
            if (Environment == null) return null;
            return m_environment.FindPreEventListener(_delegate, handler);
        }

        public RuntimeEventListener FindPostEventListener(RuntimeEventListenerDelegate _delegate, IRuntimeEventHandler handler)
        {
            if (Environment == null) return null;
            return m_environment.FindPostEventListener(_delegate, handler);
        }

        public RuntimeEventListener FindPreEventListener(ValFunction function, IRuntimeEventHandler handler)
        {
            if (Environment == null) return null;
            return m_environment.FindPreEventListener(function, handler);
        }

        public RuntimeEventListener FindPostEventListener(ValFunction function, IRuntimeEventHandler handler)
        {
            if (Environment == null) return null;
            return m_environment.FindPostEventListener(function, handler);
        }

        public bool HasEventHandler => true;
        public IRuntimeEventHandler EventHandler => this;

        public event RuntimeEventListenerDelegate OnPreRuntimeEvent;
        public event RuntimeEventListenerDelegate OnPostRuntimeEvent;
         
        public void SubscribePreEvent(RuntimeEventListenerDelegate listener)
        {
            if (listener == null) return;
            OnPreRuntimeEvent += listener;
        }

        public void UnsubscribePreEvent(RuntimeEventListenerDelegate listener)
        {
            if (listener == null) return;
            OnPreRuntimeEvent -= listener;
        }

        public void SubscribePostEvent(RuntimeEventListenerDelegate listener)
        {
            if (listener == null) return;
            OnPostRuntimeEvent += listener;
        }

        public void UnsubscribePostEvent(RuntimeEventListenerDelegate listener)
        {
            if (listener == null) return;
            OnPostRuntimeEvent -= listener;
        }

        #endregion

        [NonSerialized]
        private ExecutableBehaviour m_behaviour;
        public ExecutableBehaviour Behaviour
        {

            get
            {
                if (m_behaviour == null)
                {
                    if (destroyed || m_creation == null || !HasScripting) return null;
                    m_behaviour = Creation.Script.NewExecutable(Identifier + behaviourSuffix, m_executionPriority, Environment, m_logger, false, Creation.Author);
                    m_behaviour.OnPreExecute += PreExecute;
                    m_behaviour.OnPreExecuteToCompletion += PreExecute;
                    m_behaviour.OnPreExecute += PostExecute;
                    m_behaviour.OnPreExecuteToCompletion += PostExecute;
                }

                return m_behaviour;
            }

        }

        [NonSerialized]
        private ICollection<IVar> m_environmentVars;

        public bool TryGetEnvironmentVar(string name, out IVar var)
        {
            var = null;

            if (m_environmentVars != null)
            {
                foreach (var v in m_environmentVars) if (v != null && string.Equals(v.Name, name))
                    {
                        var = v;
                        return true;
                    }
                foreach (var v in m_environmentVars) if (v != null && string.Equals(v.Name.AsID(), name.AsID()))
                    {
                        var = v;
                        return true;
                    }
            }

            return false;
        }

        public const string environmentSuffix = "_envCR";
        public const string behaviourSuffix = "_bvrCR";

        public object Instance => this;

        public PermissionScope scope = PermissionScope.ExperienceOnly; 
        public PermissionScope Scope
        {
            get
            {
                return scope;
            }
            set
            {
                scope = value;
            }
        }
        public string Identifier => gameObject.name;

        [NonSerialized]
        public object hostData;
        public object HostData => hostData;

        public EngineInternal.GameObject Root => UnityEngineHook.AsSwoleGameObject(gameObject);

        public ICreationInstance RootCreation
        {
            get
            {
                var parent = gameObject.GetComponentInParent<CreationBehaviour>();
                if (parent == this) return this;

                return parent.RootCreation;
            }
        }

        protected static ValNumber var_deltaTime = new ValNumber(0);
        protected static ValNumber var_fixedDeltaTime = new ValNumber(0);
        public const string varId_deltaTime = "deltaTime";
        public const string varId_fixedDeltaTime = varId_deltaTime;
        protected void PreExecute(ExecutionLayer layer)
        {
            switch(layer)
            {
                default:
                    var_deltaTime.value = Time.deltaTime;
                    m_environment.SetLocalVar(varId_deltaTime, var_deltaTime);
                    break;

                case ExecutionLayer.FixedUpdate:
                    var_fixedDeltaTime.value = Time.fixedDeltaTime;
                    m_environment.SetLocalVar(varId_fixedDeltaTime, var_fixedDeltaTime);
                    break;
            }
            OnPreRuntimeEvent?.Invoke(layer.ToString(), (int)layer, this);
        }
        protected void PostExecute(ExecutionLayer layer)
        {
            OnPostRuntimeEvent?.Invoke(layer.ToString(), (int)layer, this);  
        }

        public bool dontAutoInitialized;
        protected virtual void Start() 
        {
            if (dontAutoInitialized) return;
            Initialize(); 
        }

        protected bool initialized;
        public const int _initializationCompletionTimeout = 1;
        public virtual bool Initialize(bool startExecuting = true) 
        {
            if (!swole.IsInPlayMode || destroyed || m_creation == null || !HasScripting) return false;

            if (homePackage == null) homePackage = ContentManager.FindPackage(m_creation.PackageInfo); 

            Behaviour.SetHostData(this);
            var res = m_behaviour.ExecuteToCompletion(ExecutionLayer.Initialization, _initializationCompletionTimeout, Logger);
            initialized = res == ExecutionResult.Completed || res == ExecutionResult.None || res == ExecutionResult.TimedOut;

            if (startExecuting) StartExecuting();  

            return true;
        }
        public bool IsInitialized => (m_behaviour != null && initialized) || m_creation == null || (m_creation != null && !HasScripting);
        public bool IsExecuting => m_behaviour != null && isExecuting;

        protected bool isExecuting;
        public virtual bool StartExecuting()
        {
            if (!IsInitialized) return false;
            if (IsExecuting) return true;

            isExecuting = true;
            SwoleScriptPlayModeEnvironment.AddBehaviour(m_behaviour, IsEditable); 
            return true;
        }
        public virtual void StopExecuting()
        {
            if (!IsInitialized) return;

            isExecuting = false;
            SwoleScriptPlayModeEnvironment.RemoveBehaviour(m_behaviour);
        }

        protected virtual void OnEnable()
        {
            if (swole.IsInPlayMode) 
            {
                if (IsInitialized) m_behaviour?.ExecuteToCompletion(ExecutionLayer.Enable, ExecutableScript._defaultCompleteExecutionTimeout, Logger);
            }
        }

        protected virtual void OnDisable()
        {
            if (swole.IsInPlayMode) 
            {
                if (IsInitialized) m_behaviour?.ExecuteToCompletion(ExecutionLayer.Disable, ExecutableScript._defaultCompleteExecutionTimeout, Logger); 
            }
        }

        protected List<VoidParameterlessDelegate> onEndListeners = new List<VoidParameterlessDelegate>();
        public void ListenForQuit(VoidParameterlessDelegate listener)
        {
            if (listener == null || destroyed) return;
            onEndListeners?.Add(listener); 
        }

        public void StopListeningForQuit(VoidParameterlessDelegate listener)
        {
            if (listener == null || destroyed) return;
            onEndListeners?.RemoveAll(i => i == listener);
        }

        public void Dispose()
        {
            if (!destroyed) GameObject.Destroy(this); 
            
            CancelAllTokens();         
             
            if (swole.IsInPlayMode)
            {
                try 
                {
                    if (IsInitialized) m_behaviour?.ExecuteToCompletion(ExecutionLayer.Destroy, 1, Logger); 
                }
                catch (Exception ex)
                {
                    swole.LogError(ex);
                }
                OnPreRuntimeEvent = null;
                OnPostRuntimeEvent = null;
            }

            if (onEndListeners != null)
            {
                foreach (var listener in onEndListeners) listener?.Invoke();
                onEndListeners.Clear();
                onEndListeners = null;
            }

            if (tileInstances != null)
            {
                foreach (var tile in tileInstances)
                {
                    try
                    {
                        tile.Dispose();
                    }
                    catch (Exception ex)
                    {
                        swole.LogError(ex);
                    }
                }
                tileInstances.Clear();
                tileInstances = null;
            }
            if (objectInstances != null)
            {
                foreach (var obj in objectInstances)
                {
                    try
                    {
                        if (obj is ITileInstance iti)
                        {
                            iti.Dispose();
                            continue;
                        }

                        if (obj == null) continue;
                        if (obj.Instance is GameObject go)
                        {
                            GameObject.Destroy(go);
                        }
                        else if (obj.Instance is Component comp)
                        {
                            GameObject.Destroy(comp.gameObject);
                        }
                    }
                    catch (Exception ex)
                    {
                        swole.LogError(ex);
                    }
                }
                objectInstances.Clear();
                objectInstances = null;
            }

            initialized = false;
            m_behaviour?.Dispose();  
            m_behaviour = null;
            m_environment?.Dispose();
            m_environment = null;
            m_creation = null;

            sgoCache?.Clear();
            sgoCache = null;
        }
        [NonSerialized]
        private bool destroyed;
        protected virtual void OnDestroy()
        {
            destroyed = true;
            Dispose();
        }

        protected virtual void FixedUpdate()
        {
            if (swole.IsInPlayMode) 
            {
                if (isExecuting) m_behaviour?.Execute(ExecutionLayer.FixedUpdate, ExecutableScript._defaultExecutionTimeout, Logger);
            }
        }

        protected virtual void OnCollisionEnter(Collision col) { }
        protected virtual void OnCollisionStay(Collision col) { }
        protected virtual void OnCollisionExit(Collision col) { }

        protected virtual void OnTriggerEnter(Collider col) { }
        protected virtual void OnTriggerStay(Collider col) { }
        protected virtual void OnTriggerExit(Collider col) { }

        public GameObject FindUnityObject(string name) 
        {
            if (destroyed) return null;

            var t = transform.FindDeepChildLiberal(name);
            if (t == null) return null;
            return t.gameObject;
        }

        public EngineInternal.GameObject FindGameObject(string name)
        {
            if (destroyed) return default;

            var t = transform.FindDeepChildLiberal(name);
            if (t == null) return default;
            return UnityEngineHook.AsSwoleGameObject(t.gameObject);
        }

        [NonSerialized]
        protected Dictionary<int, SwoleGameObject> sgoCache;
        public EngineInternal.SwoleGameObject FindSwoleGameObject(int id)
        {
            if (id < 0 || destroyed) return default;

            if (sgoCache == null) 
            { 
                var temp = gameObject.GetComponentsInChildren<SwoleGameObject>(true);
                sgoCache = new Dictionary<int, SwoleGameObject>();
                foreach(var obj in temp)
                {
                    if (obj == null || obj.gameObject == gameObject) continue; // Don't include this game object (might be a child of a different creation)

                    var rootCreation = obj.GetComponentInParent<CreationBehaviour>();
                    if (rootCreation == null || rootCreation != this) continue; // Children of other creations have ids that pertain to their parent's environment

                    sgoCache[obj.id] = obj;
                }
            }

            if (sgoCache.TryGetValue(id, out var output)) 
            {
                if (output != null) 
                { 
                    return new EngineInternal.SwoleGameObject(UnityEngineHook.AsSwoleGameObject(output.gameObject), id); 
                } 
                else
                {
                    return default;
                }
            }

            if (tileInstances != null)
            {          
                foreach (var tile in tileInstances)
                {
                    if (tile.IsDestroyed || tile.SwoleId != id) continue;
                    var obj = UnityEngineHook.AsSwoleGameObject(tile);
                    if (obj.instance is GameObject go)
                    {
                        var sgo = go.AddOrGetComponent<SwoleGameObject>();
                        if (sgo != null) 
                        {
                            sgo.id = id;
                            sgoCache[sgo.id] = sgo;
                            return new EngineInternal.SwoleGameObject(obj, id);   
                        }
                    }
                }
            }

            if (objectInstances != null)
            {
                foreach (var inst in objectInstances)
                {
                    if (swole.IsNull(inst)) continue;

                    int swoleId = swole.Engine.GetSwoleId(inst); 
                    if (swoleId < 0 || swoleId != id) continue;
                    var obj = inst.baseGameObject;
                    if (obj.instance is GameObject go)
                    {
                        var sgo = go.AddOrGetComponent<SwoleGameObject>();
                        if (sgo != null)
                        {
                            sgo.id = id;
                            sgoCache[sgo.id] = sgo;
                            return new EngineInternal.SwoleGameObject(obj, id);
                        }
                    }
                }
            }

            return default;
        }

        /// <summary>
        /// Find nested swole game objects using a chain of ids, in the form of a delimited string
        /// </summary>
        public EngineInternal.SwoleGameObject FindSwoleGameObject(string ids)
        {
            if (string.IsNullOrWhiteSpace(ids)) return default;

            int i = ids.IndexOf(ICreationInstance._idDelimiter);
            if (i >= 0)
            {
                int id;
                if (!int.TryParse(ids.Substring(0, i), out id)) id = -1;

                var sgo = FindSwoleGameObject(id);

                int nextStartIndex = i + ICreationInstance._idDelimiter.Length;
                string subIds = (nextStartIndex >= ids.Length) ? null : ids.Substring(nextStartIndex);
                if (string.IsNullOrWhiteSpace(subIds)) return sgo;

                if (sgo.instance.instance is GameObject go)
                {
                    var childCreation = go.GetComponent<CreationBehaviour>();
                    if (childCreation != null)
                    {
                        return childCreation.FindSwoleGameObject(subIds);
                    }
                }

            }
            else
            {
                int id;
                if (int.TryParse(ids, out id)) return FindSwoleGameObject(id);
            }

            return default;
        }

    }

}

#endif