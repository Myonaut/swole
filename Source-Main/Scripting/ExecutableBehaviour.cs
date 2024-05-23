using System;
using System.Collections.Generic;
using System.Linq;

namespace Swole.Script
{

    public class ExecutableBehaviour : IExecutable
    {

        public int CompareTo(IExecutable exe) => exe == null ? 1 : Priority.CompareTo(exe.Priority);

        protected readonly List<PackageIdentifier> dependencies = new List<PackageIdentifier>();
        public ICollection<PackageIdentifier> Dependencies
        {
            get
            {
                dependencies.Clear(); 
                void AddDeps(IExecutable ex)
                {
                    if (ex == null) return;
                    var deps = ex.Dependencies;
                    if (deps != null) dependencies.AddRange(deps);
                }

                AddDeps(script_OnLoadExperience);
                AddDeps(script_OnUnloadExperience);
                AddDeps(script_OnBeginExperience);
                AddDeps(script_OnEndExperience);
                AddDeps(script_OnRestartExperience);
                AddDeps(script_OnSaveProgress);
                AddDeps(script_OnLoadProgress);

                AddDeps(script_OnInitialize);
                AddDeps(script_OnEarlyUpdate);
                AddDeps(script_OnUpdate);
                AddDeps(script_OnLateUpdate);
                AddDeps(script_OnFixedUpdate);
                AddDeps(script_OnEnable);
                AddDeps(script_OnDisable);
                AddDeps(script_OnDestroy);
                AddDeps(script_OnCollisionEnter);
                AddDeps(script_OnCollisionStay);
                AddDeps(script_OnCollisionExit);
                AddDeps(script_OnTriggerEnter);
                AddDeps(script_OnTriggerStay);
                AddDeps(script_OnTriggerExit);
                AddDeps(script_OnInteract);

                return dependencies;
            }
        }

        /// <summary>
        /// Automatically dispose this behaviour when an OnDestroy execution call comes through?
        /// </summary>
        public bool autoDisposeOnDestroy = true;
        /// <summary>
        /// Automatically enable/disable the behaviour when an OnEnable or OnDisable execution call comes through?
        /// </summary>
        public bool autoEnableDisable = true;

        protected readonly int priority;
        public int Priority => priority;

        protected readonly string identity;
        protected readonly SwoleLogger logger;

        protected readonly IRuntimeEnvironment environment;

        protected ExecutableScript script_OnLoadExperience;
        protected ExecutableScript script_OnUnloadExperience;
        protected ExecutableScript script_OnBeginExperience;
        protected ExecutableScript script_OnEndExperience;
        protected ExecutableScript script_OnRestartExperience;
        protected ExecutableScript script_OnSaveProgress;
        protected ExecutableScript script_OnLoadProgress;

        protected ExecutableScript script_OnInitialize;
        protected ExecutableScript script_OnEarlyUpdate;
        protected ExecutableScript script_OnUpdate;
        protected ExecutableScript script_OnLateUpdate;
        protected ExecutableScript script_OnFixedUpdate;
        protected ExecutableScript script_OnEnable;
        protected ExecutableScript script_OnDisable;
        protected ExecutableScript script_OnDestroy;
        protected ExecutableScript script_OnCollisionEnter;
        protected ExecutableScript script_OnCollisionStay;
        protected ExecutableScript script_OnCollisionExit;
        protected ExecutableScript script_OnTriggerEnter;
        protected ExecutableScript script_OnTriggerStay;
        protected ExecutableScript script_OnTriggerExit;
        protected ExecutableScript script_OnInteract;

        public void Recompile(ExecutionLayer layer, string source, bool isPreParsed = false, string topAuthor = null, int autoIndentation = SwoleScriptSemantics.ssDefaultAutoIndentation, int startIndentation = SwoleScriptSemantics.ssDefaultStartIndentation, ICollection<SourceScript> localScripts = null)
        {
            if (isDisposed) return;

            // (?) TODO: Strip out all comments in case the source just contains comments

            switch (layer)
            {

                case ExecutionLayer.Load:
                    if (string.IsNullOrWhiteSpace(source)) { script_OnLoadExperience?.Dispose(); script_OnLoadExperience = null; }
                    if (script_OnLoadExperience == null) script_OnLoadExperience = new ExecutableScript($"{identity}_OnLoadExperience", source, 0, logger, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
                    else script_OnLoadExperience.Recompile(source, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
                    return;

                case ExecutionLayer.Unload:
                    if (string.IsNullOrWhiteSpace(source)) { script_OnUnloadExperience?.Dispose(); script_OnUnloadExperience = null; }
                    if (script_OnUnloadExperience == null) script_OnUnloadExperience = new ExecutableScript($"{identity}_OnUnloadExperience", source, 1, logger, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
                    else script_OnUnloadExperience.Recompile(source, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
                    return;

                case ExecutionLayer.Begin:
                    if (string.IsNullOrWhiteSpace(source)) { script_OnBeginExperience?.Dispose(); script_OnBeginExperience = null; }
                    if (script_OnBeginExperience == null) script_OnBeginExperience = new ExecutableScript($"{identity}_OnBeginExperience", source, 0, logger, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
                    else script_OnBeginExperience.Recompile(source, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
                    return;

                case ExecutionLayer.End:
                    if (string.IsNullOrWhiteSpace(source)) { script_OnEndExperience?.Dispose(); script_OnEndExperience = null; }
                    if (script_OnEndExperience == null) script_OnEndExperience = new ExecutableScript($"{identity}_OnEndExperience", source, 1, logger, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
                    else script_OnEndExperience.Recompile(source, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
                    return;

                case ExecutionLayer.Restart:
                    if (string.IsNullOrWhiteSpace(source)) { script_OnRestartExperience?.Dispose(); script_OnRestartExperience = null; }
                    if (script_OnRestartExperience == null) script_OnRestartExperience = new ExecutableScript($"{identity}_OnRestartExperience", source, 2, logger, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
                    else script_OnRestartExperience.Recompile(source, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
                    return;

                case ExecutionLayer.SaveProgress:
                    if (string.IsNullOrWhiteSpace(source)) { script_OnSaveProgress?.Dispose(); script_OnSaveProgress = null; }
                    if (script_OnSaveProgress == null) script_OnSaveProgress = new ExecutableScript($"{identity}_OnSaveProgress", source, 0, logger, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
                    else script_OnSaveProgress.Recompile(source, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
                    return;

                case ExecutionLayer.LoadProgress:
                    if (string.IsNullOrWhiteSpace(source)) { script_OnLoadProgress?.Dispose(); script_OnLoadProgress = null; }
                    if (script_OnLoadProgress == null) script_OnLoadProgress = new ExecutableScript($"{identity}_OnLoadProgress", source, 1, logger, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
                    else script_OnLoadProgress.Recompile(source, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
                    return;

                case ExecutionLayer.Initialization:
                    if (string.IsNullOrWhiteSpace(source)) { script_OnInitialize?.Dispose(); script_OnInitialize = null; }
                    if (script_OnInitialize == null) script_OnInitialize = new ExecutableScript($"{identity}_OnInitialize", source, 0, logger, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
                    else script_OnInitialize.Recompile(source, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
                    return;

                case ExecutionLayer.EarlyUpdate:
                    if (string.IsNullOrWhiteSpace(source)) { script_OnEarlyUpdate?.Dispose(); script_OnEarlyUpdate = null; }
                    if (script_OnEarlyUpdate == null) script_OnEarlyUpdate = new ExecutableScript($"{identity}_OnEarlyUpdate", source, 1, logger, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
                    else script_OnEarlyUpdate.Recompile(source, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
                    return;

                case ExecutionLayer.Update:
                    if (string.IsNullOrWhiteSpace(source)) { script_OnUpdate?.Dispose(); script_OnUpdate = null; }
                    if (script_OnUpdate == null) script_OnUpdate = new ExecutableScript($"{identity}_OnUpdate", source, 2, logger, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
                    else script_OnUpdate.Recompile(source, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
                    return;

                case ExecutionLayer.LateUpdate:
                    if (string.IsNullOrWhiteSpace(source)) { script_OnLateUpdate?.Dispose(); script_OnLateUpdate = null; }
                    if (script_OnLateUpdate == null) script_OnLateUpdate = new ExecutableScript($"{identity}_OnLateUpdate", source, 3, logger, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
                    else script_OnLateUpdate.Recompile(source, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
                    return;

                case ExecutionLayer.FixedUpdate:
                    if (string.IsNullOrWhiteSpace(source)) { script_OnFixedUpdate?.Dispose(); script_OnFixedUpdate = null; }
                    if (script_OnFixedUpdate == null) script_OnFixedUpdate = new ExecutableScript($"{identity}_OnFixedUpdate", source, 4, logger, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
                    else script_OnFixedUpdate.Recompile(source, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
                    return;

                case ExecutionLayer.Enable:
                    if (string.IsNullOrWhiteSpace(source)) { script_OnEnable?.Dispose(); script_OnEnable = null; }
                    if (script_OnEnable == null) script_OnEnable = new ExecutableScript($"{identity}_OnEnable", source, 5, logger, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
                    else script_OnEnable.Recompile(source, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
                    return;

                case ExecutionLayer.Disable:
                    if (string.IsNullOrWhiteSpace(source)) { script_OnDisable?.Dispose(); script_OnDisable = null; }
                    if (script_OnDisable == null) script_OnDisable = new ExecutableScript($"{identity}_OnDisable", source, 6, logger, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
                    else script_OnDisable.Recompile(source, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
                    return;

                case ExecutionLayer.Destroy:
                    if (string.IsNullOrWhiteSpace(source)) { script_OnDestroy?.Dispose(); script_OnDestroy = null; }
                    if (script_OnDestroy == null) script_OnDestroy = new ExecutableScript($"{identity}_OnDestroy", source, 7, logger, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
                    else script_OnDestroy.Recompile(source, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
                    return;

                case ExecutionLayer.CollisionEnter:
                    if (string.IsNullOrWhiteSpace(source)) { script_OnCollisionEnter?.Dispose(); script_OnCollisionEnter = null; }
                    if (script_OnCollisionEnter == null) script_OnCollisionEnter = new ExecutableScript($"{identity}_OnCollisionEnter", source, 8, logger, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
                    else script_OnCollisionEnter.Recompile(source, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
                    return;

                case ExecutionLayer.CollisionStay:
                    if (string.IsNullOrWhiteSpace(source)) { script_OnCollisionStay?.Dispose(); script_OnCollisionStay = null; }
                    if (script_OnCollisionStay == null) script_OnCollisionStay = new ExecutableScript($"{identity}_OnCollisionStay", source, 9, logger, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
                    else script_OnCollisionStay.Recompile(source, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
                    return;

                case ExecutionLayer.CollisionExit:
                    if (string.IsNullOrWhiteSpace(source)) { script_OnCollisionExit?.Dispose(); script_OnCollisionExit = null; }
                    if (script_OnCollisionExit == null) script_OnCollisionExit = new ExecutableScript($"{identity}_OnCollisionExit", source, 10, logger, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
                    else script_OnCollisionExit.Recompile(source, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
                    return;

                case ExecutionLayer.TriggerEnter:
                    if (string.IsNullOrWhiteSpace(source)) { script_OnTriggerEnter?.Dispose(); script_OnTriggerEnter = null; }
                    if (script_OnTriggerEnter == null) script_OnTriggerEnter = new ExecutableScript($"{identity}_OnTriggerEnter", source, 11, logger, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
                    else script_OnTriggerEnter.Recompile(source, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
                    return;

                case ExecutionLayer.TriggerStay:
                    if (string.IsNullOrWhiteSpace(source)) { script_OnTriggerStay?.Dispose(); script_OnTriggerStay = null; }
                    if (script_OnTriggerStay == null) script_OnTriggerStay = new ExecutableScript($"{identity}_OnTriggerStay", source, 12, logger, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
                    else script_OnTriggerStay.Recompile(source, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
                    return;

                case ExecutionLayer.TriggerExit:
                    if (string.IsNullOrWhiteSpace(source)) { script_OnTriggerExit?.Dispose(); script_OnTriggerExit = null; }
                    if (script_OnTriggerExit == null) script_OnTriggerExit = new ExecutableScript($"{identity}_OnTriggerExit", source, 13, logger, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
                    else script_OnTriggerExit.Recompile(source, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
                    return;

                case ExecutionLayer.Interaction:
                    if (string.IsNullOrWhiteSpace(source)) { script_OnInteract?.Dispose(); script_OnInteract = null; }
                    if (script_OnInteract == null) script_OnInteract = new ExecutableScript($"{identity}_OnInteract", source, 14, logger, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
                    else script_OnInteract.Recompile(source, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
                    return;

            }
        }

        public bool AddToStackLayerIfHasScript(ExecutionLayer layer, ExecutionStack stack, bool force = false)
        {
            if (stack == null) return false;
            
            if (layer == ExecutionLayer.EarlyUpdate && (script_OnEarlyUpdate != null || force)) { stack.Insert(layer, this); return true; }
            if (layer == ExecutionLayer.Update && (script_OnUpdate != null || force)) { stack.Insert(layer, this); return true; }
            if (layer == ExecutionLayer.LateUpdate && (script_OnLateUpdate != null || force)) { stack.Insert(layer, this); return true; }

            return false;
        }
        
        public bool RemoveFromStackLayer(ExecutionLayer layer, ExecutionStack stack)
        {
            if (stack == null) return false;

            if (layer == ExecutionLayer.EarlyUpdate /*&& script_OnEarlyUpdate != null*/) return stack.RemoveAll(layer, this);
            if (layer == ExecutionLayer.Update /*&& script_OnUpdate != null*/) return stack.RemoveAll(layer, this);
            if (layer == ExecutionLayer.LateUpdate /*&& script_OnLateUpdate != null*/) return stack.RemoveAll(layer, this);

            return false;
        }

        public ExecutableBehaviour(string identity, int priority, IRuntimeEnvironment environment, SwoleLogger logger = null,
            string source_OnLoadExperience = null,
            string source_OnUnloadExperience = null,
            string source_OnBeginExperience = null,
            string source_OnEndExperience = null,
            string source_OnRestartExperience = null,
            string source_OnSaveProgress = null,
            string source_OnLoadProgress = null,
            string source_OnInitialize = null,
            string source_OnEarlyUpdate = null,
            string source_OnUpdate = null,
            string source_OnLateUpdate = null,
            string source_OnFixedUpdate = null,
            string source_OnEnable = null,
            string source_OnDisable = null,
            string source_OnDestroy = null,
            string source_OnCollisionEnter = null,
            string source_OnCollisionStay = null,
            string source_OnCollisionExit = null,
            string source_OnTriggerEnter = null,
            string source_OnTriggerStay = null,
            string source_OnTriggerExit = null,
            string source_OnInteract = null,
            bool isPreParsed = false, string topAuthor = null, int autoIndentation = SwoleScriptSemantics.ssDefaultAutoIndentation, int startIndentation = SwoleScriptSemantics.ssDefaultStartIndentation, ICollection<SourceScript> localScripts = null)
        {

            this.priority = priority;

            if (string.IsNullOrWhiteSpace(identity))
            {
                identity = $"{nameof(ExecutableBehaviour)}:{GetHashCode()}";
            }
            this.identity = identity;
            this.logger = logger;

            if (environment == null) environment = swole.DefaultEnvironment;
            this.environment = environment;

#if SWOLE_ENV
            Recompile(ExecutionLayer.Load, source_OnLoadExperience, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
            Recompile(ExecutionLayer.Unload, source_OnUnloadExperience, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts); 
            Recompile(ExecutionLayer.Begin, source_OnBeginExperience, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
            Recompile(ExecutionLayer.End, source_OnEndExperience, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
            Recompile(ExecutionLayer.Restart, source_OnRestartExperience, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
            Recompile(ExecutionLayer.SaveProgress, source_OnSaveProgress, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
            Recompile(ExecutionLayer.LoadProgress, source_OnLoadProgress, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);

            Recompile(ExecutionLayer.Initialization, source_OnInitialize, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
            Recompile(ExecutionLayer.EarlyUpdate, source_OnEarlyUpdate, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
            Recompile(ExecutionLayer.Update, source_OnUpdate, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
            Recompile(ExecutionLayer.LateUpdate, source_OnLateUpdate, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
            Recompile(ExecutionLayer.FixedUpdate, source_OnFixedUpdate, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
            Recompile(ExecutionLayer.Enable, source_OnEnable, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
            Recompile(ExecutionLayer.Disable, source_OnDisable, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
            Recompile(ExecutionLayer.Destroy, source_OnDestroy, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
            Recompile(ExecutionLayer.CollisionEnter, source_OnCollisionEnter, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
            Recompile(ExecutionLayer.CollisionStay, source_OnCollisionStay, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
            Recompile(ExecutionLayer.CollisionExit, source_OnCollisionExit, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
            Recompile(ExecutionLayer.TriggerEnter, source_OnTriggerEnter, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
            Recompile(ExecutionLayer.TriggerStay, source_OnTriggerEnter, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
            Recompile(ExecutionLayer.TriggerExit, source_OnTriggerExit, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
            Recompile(ExecutionLayer.Interaction, source_OnInteract, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts); 
#endif

        }

        public ExecutableBehaviour(string identity, int priority, IRuntimeEnvironment environment, SwoleLogger logger = null,
            SourceScript source_OnLoadExperience = default,
            SourceScript source_OnUnloadExperience = default,
            SourceScript source_OnBeginExperience = default,
            SourceScript source_OnEndExperience = default,
            SourceScript source_OnRestartExperience = default,
            SourceScript source_OnSaveProgress = default,
            SourceScript source_OnLoadProgress = default,
            SourceScript source_OnInitialize = default,
            SourceScript source_OnEarlyUpdate = default,
            SourceScript source_OnUpdate = default,
            SourceScript source_OnLateUpdate = default,
            SourceScript source_OnFixedUpdate = default,
            SourceScript source_OnEnable = default,
            SourceScript source_OnDisable = default,
            SourceScript source_OnDestroy = default,
            SourceScript source_OnCollisionEnter = default,
            SourceScript source_OnCollisionStay = default,
            SourceScript source_OnCollisionExit = default,
            SourceScript source_OnTriggerEnter = default,
            SourceScript source_OnTriggerStay = default,
            SourceScript source_OnTriggerExit = default,
            SourceScript source_OnInteract = default,
            bool isPreParsed = false, string topAuthor = null, int autoIndentation = SwoleScriptSemantics.ssDefaultAutoIndentation, int startIndentation = SwoleScriptSemantics.ssDefaultStartIndentation, ICollection<SourceScript> localScripts = null)
            : this(identity, priority, environment, logger,
                  source_OnLoadExperience.source,
                  source_OnUnloadExperience.source,
                  source_OnBeginExperience.source,
                  source_OnEndExperience.source,
                  source_OnRestartExperience.source,
                  source_OnSaveProgress.source,
                  source_OnLoadProgress.source,
                source_OnInitialize.source,
                source_OnEarlyUpdate.source,
                source_OnUpdate.source,
                source_OnLateUpdate.source,
                source_OnFixedUpdate.source,
                source_OnEnable.source,
                source_OnDisable.source,
                source_OnDestroy.source,
                source_OnCollisionEnter.source,
                source_OnCollisionStay.source,
                source_OnCollisionExit.source,
                source_OnTriggerEnter.source,
                source_OnTriggerStay.source,
                source_OnTriggerExit.source,
                source_OnInteract.source,
                isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts)
        { }

        public ExecutableBehaviour(CreationScript creationScript, string identity, int priority, IRuntimeEnvironment environment, SwoleLogger logger = null, 
            bool isPreParsed = false, string topAuthor = null, int autoIndentation = SwoleScriptSemantics.ssDefaultAutoIndentation, int startIndentation = SwoleScriptSemantics.ssDefaultStartIndentation, ICollection<SourceScript> localScripts = null)
            : this(identity, priority, environment, logger,
                creationScript.source_OnLoadExperience,
                creationScript.source_OnUnloadExperience,
                creationScript.source_OnBeginExperience,
                creationScript.source_OnEndExperience,
                creationScript.source_OnRestartExperience,
                creationScript.source_OnSaveProgress,
                creationScript.source_OnLoadProgress,
                creationScript.source_OnInitialize,
                creationScript.source_OnEarlyUpdate,
                creationScript.source_OnUpdate,
                creationScript.source_OnLateUpdate,
                creationScript.source_OnFixedUpdate,
                creationScript.source_OnEnable,
                creationScript.source_OnDisable,
                creationScript.source_OnDestroy,
                creationScript.source_OnCollisionEnter,
                creationScript.source_OnCollisionStay,
                creationScript.source_OnCollisionExit,
                creationScript.source_OnTriggerEnter,
                creationScript.source_OnTriggerStay,
                creationScript.source_OnTriggerExit,
                creationScript.source_OnInteract,
                isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts)
        { }

        protected bool disable;
        public virtual bool Enabled
        {
            get => !disable && !isDisposed;

            set => disable = !value;
        }

        private bool isDisposed;
        public virtual bool IsValid => !isDisposed;

        public virtual void Dispose()
        {

            isDisposed = true;

            script_OnLoadExperience?.Dispose();
            script_OnUnloadExperience?.Dispose();
            script_OnBeginExperience?.Dispose();
            script_OnEndExperience?.Dispose();
            script_OnRestartExperience?.Dispose();
            script_OnSaveProgress?.Dispose();
            script_OnLoadProgress?.Dispose();

            script_OnInitialize?.Dispose();
            script_OnEarlyUpdate?.Dispose();
            script_OnUpdate?.Dispose();
            script_OnLateUpdate?.Dispose();
            script_OnFixedUpdate?.Dispose();
            script_OnEnable?.Dispose();
            script_OnDisable?.Dispose();
            script_OnDestroy?.Dispose();
            script_OnCollisionEnter?.Dispose();
            script_OnCollisionStay?.Dispose();
            script_OnCollisionExit?.Dispose();
            script_OnTriggerEnter?.Dispose();
            script_OnTriggerStay?.Dispose();
            script_OnTriggerExit?.Dispose();
            script_OnInteract?.Dispose();

            OnPreExecute = null;
            OnPostExecute = null;
            OnPreExecuteToCompletion = null;
            OnPostExecuteToCompletion = null; 

        }

        public virtual void Restart(ExecutionLayer layer)
        {
            if (isDisposed) return;

            switch (layer)
            {

                case ExecutionLayer.Load:
                    if (script_OnLoadExperience != null) script_OnLoadExperience.Restart(); 
                    return;

                case ExecutionLayer.Unload:
                    if (script_OnUnloadExperience != null) script_OnUnloadExperience.Restart();
                    return;

                case ExecutionLayer.Begin:
                    if (script_OnBeginExperience != null) script_OnBeginExperience.Restart();
                    return;

                case ExecutionLayer.End:
                    if (script_OnEndExperience != null) script_OnEndExperience.Restart();
                    return;

                case ExecutionLayer.Restart:
                    if (script_OnRestartExperience != null) script_OnRestartExperience.Restart();
                    return;

                case ExecutionLayer.SaveProgress:
                    if (script_OnSaveProgress != null) script_OnSaveProgress.Restart();
                    return;

                case ExecutionLayer.LoadProgress:
                    if (script_OnLoadProgress != null) script_OnLoadProgress.Restart();
                    return;

                case ExecutionLayer.Initialization:
                    if (script_OnInitialize != null) script_OnInitialize.Restart();
                    return;

                case ExecutionLayer.EarlyUpdate:
                    if (script_OnEarlyUpdate != null) script_OnEarlyUpdate.Restart();
                    return;

                case ExecutionLayer.Update:
                    if (script_OnUpdate != null) script_OnUpdate.Restart();
                    return;

                case ExecutionLayer.LateUpdate:
                    if (script_OnLateUpdate != null) script_OnLateUpdate.Restart();
                    return;

                case ExecutionLayer.FixedUpdate:
                    if (script_OnFixedUpdate != null) script_OnFixedUpdate.Restart();
                    return;

                case ExecutionLayer.Enable:
                    if (script_OnEnable != null) script_OnEnable.Restart();
                    return;

                case ExecutionLayer.Disable:
                    if (script_OnDisable != null) script_OnDisable.Restart();
                    return;

                case ExecutionLayer.Destroy:
                    if (script_OnDestroy != null) script_OnDestroy.Restart();
                    return;

                case ExecutionLayer.CollisionEnter:
                    if (script_OnCollisionEnter != null) script_OnCollisionEnter.Restart();
                    return;

                case ExecutionLayer.CollisionStay:
                    if (script_OnCollisionStay != null) script_OnCollisionStay.Restart();
                    return;

                case ExecutionLayer.CollisionExit:
                    if (script_OnCollisionExit != null) script_OnCollisionExit.Restart();
                    return;

                case ExecutionLayer.TriggerEnter:
                    if (script_OnTriggerEnter != null) script_OnTriggerEnter.Restart();
                    return;

                case ExecutionLayer.TriggerStay:
                    if (script_OnTriggerStay != null) script_OnTriggerStay.Restart();
                    return;

                case ExecutionLayer.TriggerExit:
                    if (script_OnTriggerExit != null) script_OnTriggerExit.Restart();
                    return;

                case ExecutionLayer.Interaction:
                    if (script_OnInteract != null) script_OnInteract.Restart();
                    return;

            }
        }

        /// <summary>
        /// Set the interpreter hostData for all layers
        /// </summary>
        public virtual void SetHostData(IRuntimeHost hostData)
        {
            if (isDisposed) return; 

            foreach (var layer in Enum.GetValues(typeof(ExecutionLayer)).Cast<ExecutionLayer>()) SetHostData(layer, hostData);
        }

        /// <summary>
        /// Set the interpreter hostData for a target layer
        /// </summary>
        public virtual void SetHostData(ExecutionLayer layer, IRuntimeHost hostData)
        {
            if (isDisposed) return;

            switch (layer)
            {

                case ExecutionLayer.Load:
                    if (script_OnLoadExperience != null) script_OnLoadExperience.SetHostData(hostData);
                    return;

                case ExecutionLayer.Unload:
                    if (script_OnUnloadExperience != null) script_OnUnloadExperience.SetHostData(hostData);
                    return;

                case ExecutionLayer.Begin:
                    if (script_OnBeginExperience != null) script_OnBeginExperience.SetHostData(hostData);
                    return;

                case ExecutionLayer.End:
                    if (script_OnEndExperience != null) script_OnEndExperience.SetHostData(hostData);
                    return;

                case ExecutionLayer.Restart:
                    if (script_OnRestartExperience != null) script_OnRestartExperience.SetHostData(hostData);
                    return;

                case ExecutionLayer.SaveProgress:
                    if (script_OnSaveProgress != null) script_OnSaveProgress.SetHostData(hostData);
                    return;

                case ExecutionLayer.LoadProgress:
                    if (script_OnLoadProgress != null) script_OnLoadProgress.SetHostData(hostData);
                    return;

                case ExecutionLayer.Initialization:
                    if (script_OnInitialize != null) script_OnInitialize.SetHostData(hostData);
                    return;

                case ExecutionLayer.EarlyUpdate:
                    if (script_OnEarlyUpdate != null) script_OnEarlyUpdate.SetHostData(hostData);
                    return;

                case ExecutionLayer.Update:
                    if (script_OnUpdate != null) script_OnUpdate.SetHostData(hostData);
                    return;

                case ExecutionLayer.LateUpdate:
                    if (script_OnLateUpdate != null) script_OnLateUpdate.SetHostData(hostData);
                    return;

                case ExecutionLayer.FixedUpdate:
                    if (script_OnFixedUpdate != null) script_OnFixedUpdate.SetHostData(hostData);
                    return;

                case ExecutionLayer.Enable:
                    if (script_OnEnable != null) script_OnEnable.SetHostData(hostData);
                    return;

                case ExecutionLayer.Disable:
                    if (script_OnDisable != null) script_OnDisable.SetHostData(hostData);
                    return;

                case ExecutionLayer.Destroy:
                    if (script_OnDestroy != null) script_OnDestroy.SetHostData(hostData);
                    return;

                case ExecutionLayer.CollisionEnter:
                    if (script_OnCollisionEnter != null) script_OnCollisionEnter.SetHostData(hostData);
                    return;

                case ExecutionLayer.CollisionStay:
                    if (script_OnCollisionStay != null) script_OnCollisionStay.SetHostData(hostData);
                    return;

                case ExecutionLayer.CollisionExit:
                    if (script_OnCollisionExit != null) script_OnCollisionExit.SetHostData(hostData);
                    return;

                case ExecutionLayer.TriggerEnter:
                    if (script_OnTriggerEnter != null) script_OnTriggerEnter.SetHostData(hostData);
                    return;

                case ExecutionLayer.TriggerStay:
                    if (script_OnTriggerStay != null) script_OnTriggerStay.SetHostData(hostData);
                    return;

                case ExecutionLayer.TriggerExit:
                    if (script_OnTriggerExit != null) script_OnTriggerExit.SetHostData(hostData);
                    return;

                case ExecutionLayer.Interaction:
                    if (script_OnInteract != null) script_OnInteract.SetHostData(hostData);
                    return;

            }
        }

        public delegate void BehaviourExecutionDelegate(ExecutionLayer layer);

        public event BehaviourExecutionDelegate OnPreExecute;
        public event BehaviourExecutionDelegate OnPostExecute;
        public ExecutionResult Execute(ExecutionLayer layer, float timeOut = ExecutableScript._defaultExecutionTimeout, SwoleLogger logger = null)
        {
            OnPreExecute?.Invoke(layer);
            var res = ExecuteInternal(layer, timeOut, logger);
            OnPostExecute?.Invoke(layer);
            return res;
        }
        protected virtual ExecutionResult ExecuteInternal(ExecutionLayer layer, float timeOut = ExecutableScript._defaultExecutionTimeout, SwoleLogger logger = null)
        {

            if (isDisposed) return ExecutionResult.Disposed;

            ExecutionResult result;
            switch (layer)
            {

                case ExecutionLayer.Load:
                    return script_OnLoadExperience == null ? ExecutionResult.Completed : disable ? ExecutionResult.Disabled : script_OnLoadExperience.Execute(environment, timeOut, logger);

                case ExecutionLayer.Unload:
                    return script_OnUnloadExperience == null ? ExecutionResult.Completed : disable ? ExecutionResult.Disabled : script_OnUnloadExperience.Execute(environment, timeOut, logger);

                case ExecutionLayer.Begin:
                    return script_OnBeginExperience == null ? ExecutionResult.Completed : disable ? ExecutionResult.Disabled : script_OnBeginExperience.Execute(environment, timeOut, logger);

                case ExecutionLayer.End:
                    return script_OnEndExperience == null ? ExecutionResult.Completed : disable ? ExecutionResult.Disabled : script_OnEndExperience.Execute(environment, timeOut, logger);

                case ExecutionLayer.Restart:
                    return script_OnRestartExperience == null ? ExecutionResult.Completed : disable ? ExecutionResult.Disabled : script_OnRestartExperience.Execute(environment, timeOut, logger);

                case ExecutionLayer.SaveProgress:
                    return script_OnSaveProgress == null ? ExecutionResult.Completed : disable ? ExecutionResult.Disabled : script_OnSaveProgress.Execute(environment, timeOut, logger);

                case ExecutionLayer.LoadProgress:
                    return script_OnLoadProgress == null ? ExecutionResult.Completed : disable ? ExecutionResult.Disabled : script_OnLoadProgress.Execute(environment, timeOut, logger);

                case ExecutionLayer.Initialization:
                    return script_OnInitialize == null ? ExecutionResult.Completed : disable ? ExecutionResult.Disabled : script_OnInitialize.Execute(environment, timeOut, logger);

                case ExecutionLayer.EarlyUpdate:
                    return script_OnEarlyUpdate == null ? ExecutionResult.Completed : disable ? ExecutionResult.Disabled : script_OnEarlyUpdate.Execute(environment, timeOut, logger);

                case ExecutionLayer.Update:
                    return script_OnUpdate == null ? ExecutionResult.Completed : disable ? ExecutionResult.Disabled : script_OnUpdate.Execute(environment, timeOut, logger);

                case ExecutionLayer.LateUpdate:
                    return script_OnLateUpdate == null ? ExecutionResult.Completed : disable ? ExecutionResult.Disabled : script_OnLateUpdate.Execute(environment, timeOut, logger);

                case ExecutionLayer.FixedUpdate:
                    return script_OnFixedUpdate == null ? ExecutionResult.Completed : disable ? ExecutionResult.Disabled : script_OnFixedUpdate.Execute(environment, timeOut, logger);

                case ExecutionLayer.Enable:
                    result = script_OnEnable == null ? ExecutionResult.Completed : disable && !autoEnableDisable ? ExecutionResult.Disabled : script_OnEnable.Execute(environment, timeOut, logger);
                    if (autoEnableDisable) Enabled = true;
                    return result;

                case ExecutionLayer.Disable:
                    result = script_OnDisable == null ? ExecutionResult.Completed : disable && !autoEnableDisable ? ExecutionResult.Disabled : script_OnDisable.Execute(environment, timeOut, logger);
                    if (autoEnableDisable) Enabled = false;
                    return result;

                case ExecutionLayer.Destroy:
                    result = script_OnDestroy == null ? ExecutionResult.Completed : script_OnDestroy.Execute(environment, timeOut, logger);
                    if (autoDisposeOnDestroy) Dispose();
                    return result;

                case ExecutionLayer.CollisionEnter:
                    return script_OnCollisionEnter == null ? ExecutionResult.Completed : disable ? ExecutionResult.Disabled : script_OnCollisionEnter.Execute(environment, timeOut, logger);

                case ExecutionLayer.CollisionStay:
                    return script_OnCollisionStay == null ? ExecutionResult.Completed : disable ? ExecutionResult.Disabled : script_OnCollisionStay.Execute(environment, timeOut, logger);

                case ExecutionLayer.CollisionExit:
                    return script_OnCollisionExit == null ? ExecutionResult.Completed : disable ? ExecutionResult.Disabled : script_OnCollisionExit.Execute(environment, timeOut, logger);

                case ExecutionLayer.TriggerEnter:
                    return script_OnTriggerEnter == null ? ExecutionResult.Completed : disable ? ExecutionResult.Disabled : script_OnTriggerEnter.Execute(environment, timeOut, logger);

                case ExecutionLayer.TriggerStay:
                    return script_OnTriggerStay == null ? ExecutionResult.Completed : disable ? ExecutionResult.Disabled : script_OnTriggerStay.Execute(environment, timeOut, logger);

                case ExecutionLayer.TriggerExit:
                    return script_OnTriggerExit == null ? ExecutionResult.Completed : disable ? ExecutionResult.Disabled : script_OnTriggerExit.Execute(environment, timeOut, logger);

                case ExecutionLayer.Interaction:
                    return script_OnInteract == null ? ExecutionResult.Completed : disable ? ExecutionResult.Disabled : script_OnInteract.Execute(environment, timeOut, logger);

            }

            return ExecutionResult.None;

        }

        public event BehaviourExecutionDelegate OnPreExecuteToCompletion;
        public event BehaviourExecutionDelegate OnPostExecuteToCompletion;
        public ExecutionResult ExecuteToCompletion(ExecutionLayer layer, float timeOut = ExecutableScript._defaultCompleteExecutionTimeout, SwoleLogger logger = null)
        {
            OnPreExecuteToCompletion?.Invoke(layer);
            var res = ExecuteToCompletionInternal(layer, timeOut, logger); 
            OnPostExecuteToCompletion?.Invoke(layer);
            return res;
        }
        protected virtual ExecutionResult ExecuteToCompletionInternal(ExecutionLayer layer, float timeOut = ExecutableScript._defaultCompleteExecutionTimeout, SwoleLogger logger = null)
        {

            if (isDisposed) return ExecutionResult.Disposed;

            ExecutionResult result;
            switch (layer)
            {

                case ExecutionLayer.Load:
                    return script_OnLoadExperience == null ? ExecutionResult.Completed : disable ? ExecutionResult.Disabled : script_OnLoadExperience.ExecuteToCompletion(environment, timeOut, logger);

                case ExecutionLayer.Unload:
                    return script_OnUnloadExperience == null ? ExecutionResult.Completed : disable ? ExecutionResult.Disabled : script_OnUnloadExperience.ExecuteToCompletion(environment, timeOut, logger);

                case ExecutionLayer.Begin:
                    return script_OnBeginExperience == null ? ExecutionResult.Completed : disable ? ExecutionResult.Disabled : script_OnBeginExperience.ExecuteToCompletion(environment, timeOut, logger);

                case ExecutionLayer.End:
                    return script_OnEndExperience == null ? ExecutionResult.Completed : disable ? ExecutionResult.Disabled : script_OnEndExperience.ExecuteToCompletion(environment, timeOut, logger);

                case ExecutionLayer.Restart:
                    return script_OnRestartExperience == null ? ExecutionResult.Completed : disable ? ExecutionResult.Disabled : script_OnRestartExperience.ExecuteToCompletion(environment, timeOut, logger);

                case ExecutionLayer.SaveProgress:
                    return script_OnSaveProgress == null ? ExecutionResult.Completed : disable ? ExecutionResult.Disabled : script_OnSaveProgress.ExecuteToCompletion(environment, timeOut, logger);

                case ExecutionLayer.LoadProgress:
                    return script_OnLoadProgress == null ? ExecutionResult.Completed : disable ? ExecutionResult.Disabled : script_OnLoadProgress.ExecuteToCompletion(environment, timeOut, logger);

                case ExecutionLayer.Initialization:
                    return script_OnInitialize == null ? ExecutionResult.Completed : disable ? ExecutionResult.Disabled : script_OnInitialize.ExecuteToCompletion(environment, timeOut, logger);

                case ExecutionLayer.EarlyUpdate:
                    return script_OnEarlyUpdate == null ? ExecutionResult.Completed : disable ? ExecutionResult.Disabled : script_OnEarlyUpdate.ExecuteToCompletion(environment, timeOut, logger);

                case ExecutionLayer.Update:
                    return script_OnUpdate == null ? ExecutionResult.Completed : disable ? ExecutionResult.Disabled : script_OnUpdate.ExecuteToCompletion(environment, timeOut, logger);

                case ExecutionLayer.LateUpdate:
                    return script_OnLateUpdate == null ? ExecutionResult.Completed : disable ? ExecutionResult.Disabled : script_OnLateUpdate.ExecuteToCompletion(environment, timeOut, logger);

                case ExecutionLayer.FixedUpdate:
                    return script_OnFixedUpdate == null ? ExecutionResult.Completed : disable ? ExecutionResult.Disabled : script_OnFixedUpdate.ExecuteToCompletion(environment, timeOut, logger);

                case ExecutionLayer.Enable:
                    result = script_OnEnable == null ? ExecutionResult.Completed : disable && !autoEnableDisable ? ExecutionResult.Disabled : script_OnEnable.ExecuteToCompletion(environment, timeOut, logger);
                    if (autoEnableDisable) Enabled = true;
                    return result;

                case ExecutionLayer.Disable:
                    result = script_OnDisable == null ? ExecutionResult.Completed : disable && !autoEnableDisable ? ExecutionResult.Disabled : script_OnDisable.ExecuteToCompletion(environment, timeOut, logger); 
                    if (autoEnableDisable) Enabled = false;
                    return result;

                case ExecutionLayer.Destroy:
                    result = script_OnDestroy == null ? ExecutionResult.Completed : script_OnDestroy.ExecuteToCompletion(environment, timeOut, logger);
                    if (autoDisposeOnDestroy) Dispose();
                    return result;

                case ExecutionLayer.CollisionEnter:
                    return script_OnCollisionEnter == null ? ExecutionResult.Completed : disable ? ExecutionResult.Disabled : script_OnCollisionEnter.ExecuteToCompletion(environment, timeOut, logger);

                case ExecutionLayer.CollisionStay:
                    return script_OnCollisionStay == null ? ExecutionResult.Completed : disable ? ExecutionResult.Disabled : script_OnCollisionStay.ExecuteToCompletion(environment, timeOut, logger);

                case ExecutionLayer.CollisionExit:
                    return script_OnCollisionExit == null ? ExecutionResult.Completed : disable ? ExecutionResult.Disabled : script_OnCollisionExit.ExecuteToCompletion(environment, timeOut, logger);

                case ExecutionLayer.TriggerEnter:
                    return script_OnTriggerEnter == null ? ExecutionResult.Completed : disable ? ExecutionResult.Disabled : script_OnTriggerEnter.ExecuteToCompletion(environment, timeOut, logger);

                case ExecutionLayer.TriggerStay:
                    return script_OnTriggerStay == null ? ExecutionResult.Completed : disable ? ExecutionResult.Disabled : script_OnTriggerStay.ExecuteToCompletion(environment, timeOut, logger);

                case ExecutionLayer.TriggerExit:
                    return script_OnTriggerExit == null ? ExecutionResult.Completed : disable ? ExecutionResult.Disabled : script_OnTriggerExit.ExecuteToCompletion(environment, timeOut, logger);

                case ExecutionLayer.Interaction:
                    return script_OnInteract == null ? ExecutionResult.Completed : disable ? ExecutionResult.Disabled : script_OnInteract.ExecuteToCompletion(environment, timeOut, logger); 

            }

            return ExecutionResult.None;

        }

    }

}