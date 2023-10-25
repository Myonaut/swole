#if SWOLE_ENV
using Miniscript;
using static Swole.Script.SwoleScriptSemantics;
#endif

namespace Swole.Script
{

    public class ExecutableScript : IExecutable
    {

        public int CompareTo(IExecutable exe) => exe == null ? 1 : Priority.CompareTo(exe.Priority);

        protected readonly int priority;
        public int Priority => priority;
        
#if SWOLE_ENV
        protected readonly Interpreter interpreter;
        public Interpreter Interpreter => interpreter;

        public ExecutableScript(Interpreter interpreter, int priority)
        {
            this.interpreter = interpreter;
            this.priority = priority;
        }
        public ExecutableScript(SourceScript script, int priority, SwoleLogger logger = null) : this((script.packageInfo.NameIsValid ? script.packageInfo.name + "." : "") + (script.NameIsValid ? script.Name : ""), script.source, priority, logger) { }
        public ExecutableScript(string identity, string source, int priority, SwoleLogger logger = null)
        {
            this.priority = priority;
            if (string.IsNullOrEmpty(source)) return;
            if (logger == null) logger = swole.Engine.Logger;
            interpreter = new Interpreter();
            interpreter.hostData = new DefaultHostData() { identifier = identity };
            interpreter.standardOutput = interpreter.implicitOutput = (s, ignored) => logger.Log(s);
            interpreter.errorOutput = (s, ignored) => logger.LogError(s);

            interpreter.Reset(source);
            interpreter.Compile();
            if (interpreter.vm == null) // Didn't finish compilation
            {

                logger.LogError($"{ssMsgPrefix_Error} Failed to compile {(string.IsNullOrEmpty(identity) ? "an unidentified script" : "'" + identity + "'")}");
                Dispose();

            }
        }
#endif

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
            OnComplete = null;
        }

        public ExecutionResult Execute(ExecutionLayer layer, float timeOut = 0.01f) => Execute(swole.DefaultEnvironment, timeOut);

        public ExecutionResult Execute(RuntimeEnvironment environment, float timeOut = 0.01f)
        {
            if (disable) return ExecutionResult.Disabled;
            if (isDisposed) return ExecutionResult.Disposed;
            if (environment == null) return ExecutionResult.InvalidEnvironment;
#if SWOLE_ENV
            var result = environment.RunForPeriod(swole.Engine.Logger, interpreter, timeOut);
            if (result == ExecutionResult.Completed) OnComplete?.Invoke();
#else
            var result = ExecutionResult.None;
#endif
            return result;
        }

        public ExecutionResult ExecuteToCompletion(ExecutionLayer layer, float timeOut = 0.1f) => ExecuteToCompletion(swole.DefaultEnvironment, timeOut);

        public ExecutionResult ExecuteToCompletion(RuntimeEnvironment environment, float timeOut = 0.1f)
        {
            if (disable) return ExecutionResult.Disabled;
            if (isDisposed) return ExecutionResult.Disposed;
            if (environment == null) return ExecutionResult.InvalidEnvironment;
#if SWOLE_ENV
            var result = environment.RunUntilCompletion(swole.Engine.Logger, interpreter, timeOut);
            if (result == ExecutionResult.Completed) OnComplete?.Invoke();
#else
            var result = ExecutionResult.None;
#endif
            return result;
        }

        public delegate void CompletionDelegate();

        private event CompletionDelegate OnComplete;

        public void AddCompletionDelegate(CompletionDelegate del)
        {
            if (isDisposed) return;
            OnComplete += del;
        }

        public void RemoveCompletionDelegate(CompletionDelegate del)
        {
            if (isDisposed) return;
            OnComplete -= del;
        }

    }

}
