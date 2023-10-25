namespace Swole.Script
{

    public class ExecutableBehaviour : IExecutable
    {

        public int CompareTo(IExecutable exe) => exe == null ? 1 : Priority.CompareTo(exe.Priority);

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

        protected readonly RuntimeEnvironment environment;

        protected readonly ExecutableScript script_OnInitialize;
        protected readonly ExecutableScript script_OnEarlyUpdate;
        protected readonly ExecutableScript script_OnUpdate;
        protected readonly ExecutableScript script_OnLateUpdate;
        protected readonly ExecutableScript script_OnFixedUpdate;
        protected readonly ExecutableScript script_OnEnable;
        protected readonly ExecutableScript script_OnDisable;
        protected readonly ExecutableScript script_OnDestroy;
        protected readonly ExecutableScript script_OnCollisionEnter;
        protected readonly ExecutableScript script_OnCollisionStay;
        protected readonly ExecutableScript script_OnCollisionExit;
        protected readonly ExecutableScript script_OnTriggerEnter;
        protected readonly ExecutableScript script_OnTriggerStay;
        protected readonly ExecutableScript script_OnTriggerExit;
        protected readonly ExecutableScript script_OnInteract;

        public bool AddToLayerIfHasScript(ExecutionLayer layer, ExecutionStack stack)
        {
            if (stack == null) return false;

            if (layer == ExecutionLayer.EarlyUpdate && script_OnEarlyUpdate != null) { stack.Insert(layer, this); return true; }
            if (layer == ExecutionLayer.Update && script_OnUpdate != null) { stack.Insert(layer, this); return true; }
            if (layer == ExecutionLayer.LateUpdate && script_OnLateUpdate != null) { stack.Insert(layer, this); return true; }

            return false;
        }

        public bool RemoveFromLayerIfHasScript(ExecutionLayer layer, ExecutionStack stack)
        {
            if (stack == null) return false;

            if (layer == ExecutionLayer.EarlyUpdate && script_OnEarlyUpdate != null) return stack.RemoveAll(layer, this);
            if (layer == ExecutionLayer.Update && script_OnUpdate != null) return stack.RemoveAll(layer, this);
            if (layer == ExecutionLayer.LateUpdate && script_OnLateUpdate != null) return stack.RemoveAll(layer, this);

            return false;
        }

        public ExecutableBehaviour(string identity, int priority, RuntimeEnvironment environment, SwoleLogger logger = null,
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
            string source_OnInteract = null)
        {

            this.priority = priority;

            if (string.IsNullOrEmpty(identity))
            {

                identity = $"{nameof(ExecutableBehaviour)}:{GetHashCode()}";

            }

            if (environment == null) environment = swole.DefaultEnvironment;
            this.environment = environment;

            #if SWOLE_ENV
            if (!string.IsNullOrEmpty(source_OnInitialize)) script_OnInitialize = new ExecutableScript($"{identity}_OnInitialize", source_OnInitialize, 0, logger);
            if (!string.IsNullOrEmpty(source_OnEarlyUpdate)) script_OnEarlyUpdate = new ExecutableScript($"{identity}_OnEarlyUpdate", source_OnEarlyUpdate, 1, logger);
            if (!string.IsNullOrEmpty(source_OnUpdate)) script_OnUpdate = new ExecutableScript($"{identity}_OnUpdate", source_OnUpdate, 2, logger);
            if (!string.IsNullOrEmpty(source_OnLateUpdate)) script_OnLateUpdate = new ExecutableScript($"{identity}_OnLateUpdate", source_OnLateUpdate, 3, logger);
            if (!string.IsNullOrEmpty(source_OnFixedUpdate)) script_OnFixedUpdate = new ExecutableScript($"{identity}_OnFixedUpdate", source_OnFixedUpdate, 4, logger);
            if (!string.IsNullOrEmpty(source_OnEnable)) script_OnEnable = new ExecutableScript($"{identity}_OnEnable", source_OnEnable, 5, logger);
            if (!string.IsNullOrEmpty(source_OnDisable)) script_OnDisable = new ExecutableScript($"{identity}_OnDisable", source_OnDisable, 6, logger);
            if (!string.IsNullOrEmpty(source_OnDestroy)) script_OnDestroy = new ExecutableScript($"{identity}_OnDestroy", source_OnDestroy, 7, logger);
            if (!string.IsNullOrEmpty(source_OnCollisionEnter)) script_OnCollisionEnter = new ExecutableScript($"{identity}_OnCollisionEnter", source_OnCollisionEnter, 8, logger);
            if (!string.IsNullOrEmpty(source_OnCollisionStay)) script_OnCollisionStay = new ExecutableScript($"{identity}_OnCollisionStay", source_OnCollisionStay, 9, logger);
            if (!string.IsNullOrEmpty(source_OnCollisionExit)) script_OnCollisionExit = new ExecutableScript($"{identity}_OnCollisionExit", source_OnCollisionExit, 10, logger);
            if (!string.IsNullOrEmpty(source_OnTriggerEnter)) script_OnTriggerEnter = new ExecutableScript($"{identity}_OnTriggerEnter", source_OnTriggerEnter, 11, logger);
            if (!string.IsNullOrEmpty(source_OnTriggerStay)) script_OnTriggerStay = new ExecutableScript($"{identity}_OnTriggerStay", source_OnTriggerStay, 12, logger);
            if (!string.IsNullOrEmpty(source_OnTriggerExit)) script_OnTriggerExit = new ExecutableScript($"{identity}_OnTriggerExit", source_OnTriggerExit, 13, logger);
            if (!string.IsNullOrEmpty(source_OnInteract)) script_OnInteract = new ExecutableScript($"{identity}_OnInteract", source_OnInteract, 14, logger);
            #endif

        }

        public ExecutableBehaviour(string identity, int priority, RuntimeEnvironment environment, SwoleLogger logger = null,
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
            SourceScript source_OnInteract = default)
            : this(identity, priority, environment, logger,
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
                source_OnInteract.source)
        { }

        public ExecutableBehaviour(CreationScript creationScript, string identity, int priority, RuntimeEnvironment environment, SwoleLogger logger = null)
            : this(identity, priority, environment, logger,
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
                creationScript.source_OnInteract)
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

        }

        public virtual ExecutionResult Execute(ExecutionLayer layer, float timeOut = 0.01f)
        {

            if (isDisposed) return ExecutionResult.Disposed;

            ExecutionResult result;
            switch (layer)
            {

                case ExecutionLayer.Initialization:
                    return script_OnInitialize == null ? ExecutionResult.None : disable ? ExecutionResult.Disabled : script_OnInitialize.Execute(environment, timeOut);

                case ExecutionLayer.EarlyUpdate:
                    return script_OnEarlyUpdate == null ? ExecutionResult.None : disable ? ExecutionResult.Disabled : script_OnEarlyUpdate.Execute(environment, timeOut);

                case ExecutionLayer.Update:
                    return script_OnUpdate == null ? ExecutionResult.None : disable ? ExecutionResult.Disabled : script_OnUpdate.Execute(environment, timeOut);

                case ExecutionLayer.LateUpdate:
                    return script_OnLateUpdate == null ? ExecutionResult.None : disable ? ExecutionResult.Disabled : script_OnLateUpdate.Execute(environment, timeOut);

                case ExecutionLayer.FixedUpdate:
                    return script_OnFixedUpdate == null ? ExecutionResult.None : disable ? ExecutionResult.Disabled : script_OnFixedUpdate.Execute(environment, timeOut);

                case ExecutionLayer.Enable:
                    result = script_OnEnable == null ? ExecutionResult.None : disable && !autoEnableDisable ? ExecutionResult.Disabled : script_OnEnable.Execute(environment, timeOut);
                    if (autoEnableDisable) Enabled = true;
                    return result;

                case ExecutionLayer.Disable:
                    result = script_OnDisable == null ? ExecutionResult.None : disable && !autoEnableDisable ? ExecutionResult.Disabled : script_OnDisable.Execute(environment, timeOut);
                    if (autoEnableDisable) Enabled = false;
                    return result;

                case ExecutionLayer.Destroy:
                    result = script_OnDestroy == null ? ExecutionResult.None : script_OnDestroy.Execute(environment, timeOut);
                    if (autoDisposeOnDestroy) Dispose();
                    return result;

                case ExecutionLayer.CollisionEnter:
                    return script_OnCollisionEnter == null ? ExecutionResult.None : disable ? ExecutionResult.Disabled : script_OnCollisionEnter.Execute(environment, timeOut);

                case ExecutionLayer.CollisionStay:
                    return script_OnCollisionStay == null ? ExecutionResult.None : disable ? ExecutionResult.Disabled : script_OnCollisionStay.Execute(environment, timeOut);

                case ExecutionLayer.CollisionExit:
                    return script_OnCollisionExit == null ? ExecutionResult.None : disable ? ExecutionResult.Disabled : script_OnCollisionExit.Execute(environment, timeOut);

                case ExecutionLayer.TriggerEnter:
                    return script_OnTriggerEnter == null ? ExecutionResult.None : disable ? ExecutionResult.Disabled : script_OnTriggerEnter.Execute(environment, timeOut);

                case ExecutionLayer.TriggerStay:
                    return script_OnTriggerStay == null ? ExecutionResult.None : disable ? ExecutionResult.Disabled : script_OnTriggerStay.Execute(environment, timeOut);

                case ExecutionLayer.TriggerExit:
                    return script_OnTriggerExit == null ? ExecutionResult.None : disable ? ExecutionResult.Disabled : script_OnTriggerExit.Execute(environment, timeOut);

                case ExecutionLayer.Interaction:
                    return script_OnInteract == null ? ExecutionResult.None : disable ? ExecutionResult.Disabled : script_OnInteract.Execute(environment, timeOut);

            }

            return ExecutionResult.None;

        }

        public virtual ExecutionResult ExecuteToCompletion(ExecutionLayer layer, float timeOut = 0.1f)
        {

            if (isDisposed) return ExecutionResult.Disposed;

            ExecutionResult result;
            switch (layer)
            {

                case ExecutionLayer.Initialization:
                    return script_OnInitialize == null ? ExecutionResult.None : disable ? ExecutionResult.Disabled : script_OnInitialize.ExecuteToCompletion(environment, timeOut);

                case ExecutionLayer.EarlyUpdate:
                    return script_OnEarlyUpdate == null ? ExecutionResult.None : disable ? ExecutionResult.Disabled : script_OnEarlyUpdate.ExecuteToCompletion(environment, timeOut);

                case ExecutionLayer.Update:
                    return script_OnUpdate == null ? ExecutionResult.None : disable ? ExecutionResult.Disabled : script_OnUpdate.ExecuteToCompletion(environment, timeOut);

                case ExecutionLayer.LateUpdate:
                    return script_OnLateUpdate == null ? ExecutionResult.None : disable ? ExecutionResult.Disabled : script_OnLateUpdate.ExecuteToCompletion(environment, timeOut);

                case ExecutionLayer.FixedUpdate:
                    return script_OnFixedUpdate == null ? ExecutionResult.None : disable ? ExecutionResult.Disabled : script_OnFixedUpdate.ExecuteToCompletion(environment, timeOut);

                case ExecutionLayer.Enable:
                    result = script_OnEnable == null ? ExecutionResult.None : disable && !autoEnableDisable ? ExecutionResult.Disabled : script_OnEnable.ExecuteToCompletion(environment, timeOut);
                    if (autoEnableDisable) Enabled = true;
                    return result;

                case ExecutionLayer.Disable:
                    result = script_OnDisable == null ? ExecutionResult.None : disable && !autoEnableDisable ? ExecutionResult.Disabled : script_OnDisable.ExecuteToCompletion(environment, timeOut);
                    if (autoEnableDisable) Enabled = false;
                    return result;

                case ExecutionLayer.Destroy:
                    result = script_OnDestroy == null ? ExecutionResult.None : script_OnDestroy.ExecuteToCompletion(environment, timeOut);
                    if (autoDisposeOnDestroy) Dispose();
                    return result;

                case ExecutionLayer.CollisionEnter:
                    return script_OnCollisionEnter == null ? ExecutionResult.None : disable ? ExecutionResult.Disabled : script_OnCollisionEnter.ExecuteToCompletion(environment, timeOut);

                case ExecutionLayer.CollisionStay:
                    return script_OnCollisionStay == null ? ExecutionResult.None : disable ? ExecutionResult.Disabled : script_OnCollisionStay.ExecuteToCompletion(environment, timeOut);

                case ExecutionLayer.CollisionExit:
                    return script_OnCollisionExit == null ? ExecutionResult.None : disable ? ExecutionResult.Disabled : script_OnCollisionExit.ExecuteToCompletion(environment, timeOut);

                case ExecutionLayer.TriggerEnter:
                    return script_OnTriggerEnter == null ? ExecutionResult.None : disable ? ExecutionResult.Disabled : script_OnTriggerEnter.ExecuteToCompletion(environment, timeOut);

                case ExecutionLayer.TriggerStay:
                    return script_OnTriggerStay == null ? ExecutionResult.None : disable ? ExecutionResult.Disabled : script_OnTriggerStay.ExecuteToCompletion(environment, timeOut);

                case ExecutionLayer.TriggerExit:
                    return script_OnTriggerExit == null ? ExecutionResult.None : disable ? ExecutionResult.Disabled : script_OnTriggerExit.ExecuteToCompletion(environment, timeOut);

                case ExecutionLayer.Interaction:
                    return script_OnInteract == null ? ExecutionResult.None : disable ? ExecutionResult.Disabled : script_OnInteract.ExecuteToCompletion(environment, timeOut);

            }

            return ExecutionResult.None;

        }

    }

}