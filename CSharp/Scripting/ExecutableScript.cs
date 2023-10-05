using Miniscript;

using static Swolescript.SwoleScriptSemantics;

namespace Swolescript
{

    public class ExecutableScript : IExecutable
    {

        public int CompareTo(IExecutable exe) => Priority.CompareTo(exe.Priority);

        protected readonly int priority;
        public int Priority => priority;

        protected readonly Interpreter interpreter;
        public Interpreter Interpreter => interpreter;

        public ExecutableScript(Interpreter interpreter, int priority)
        {
            this.interpreter = interpreter;
            this.priority = priority;
        }
        public ExecutableScript(SourceScript script, int priority, SwoleLogger logger = null) : this((script.packageInfo.NameIsValid ? (script.packageInfo.name + ".") : "") + (script.NameIsValid ? script.name : ""), script.source, priority, logger) { }
        public ExecutableScript(string identity, string source, int priority, SwoleLogger logger = null)
        {
            this.priority = priority;
            if (string.IsNullOrEmpty(source)) return;
            if (logger == null) logger = SwoleScript.Engine.Logger;
            interpreter = new Interpreter();
            interpreter.hostData = new DefaultHostData() { identifier = identity };
            interpreter.standardOutput = interpreter.implicitOutput = (string s, bool ignored) => logger.Log(s);
            interpreter.errorOutput = (string s, bool ignored) => logger.LogError(s);

            interpreter.Reset(source);
            interpreter.Compile();
            if (interpreter.vm == null) // Didn't finish compilation
            {
                 
                logger.LogError($"{ssMsgPrefix_Error} Failed to compile {(string.IsNullOrEmpty(identity) ? "an unidentified script" : ("'" + identity + "'"))}");
                Dispose(); 
            
            } 
        }

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

        public ExecutionResult Execute(ExecutionLayer layer, float timeOut = 0.01f) => Execute(SwoleScript.DefaultEnvironment, timeOut);

        public ExecutionResult Execute(RuntimeEnvironment environment, float timeOut = 0.01f)
        {
            if (disable) return ExecutionResult.Disabled;
            if (isDisposed) return ExecutionResult.Disposed;
            if (environment == null) return ExecutionResult.InvalidEnvironment;
            var result = environment.RunForPeriod(SwoleScript.Engine.Logger, interpreter, timeOut);
            if (result == ExecutionResult.Completed) OnComplete?.Invoke();
            return result;
        }

        public ExecutionResult ExecuteToCompletion(ExecutionLayer layer, float timeOut = 0.1f) => ExecuteToCompletion(SwoleScript.DefaultEnvironment, timeOut);

        public ExecutionResult ExecuteToCompletion(RuntimeEnvironment environment, float timeOut = 0.1f)
        {
            if (disable) return ExecutionResult.Disabled;
            if (isDisposed) return ExecutionResult.Disposed;
            if (environment == null) return ExecutionResult.InvalidEnvironment;
            var result = environment.RunUntilCompletion(SwoleScript.Engine.Logger, interpreter, timeOut); 
            if (result == ExecutionResult.Completed) OnComplete?.Invoke();
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
