using System;
using System.Collections.Generic;

#if SWOLE_ENV
using Miniscript;
using static Swole.Script.SwoleScriptSemantics;
#endif

namespace Swole.Script
{

    public class ExecutableScript : IExecutable
    {

        public const float _defaultExecutionTimeout = 0.015f;
        public const float _defaultCompleteExecutionTimeout = 0.1f;

        public int CompareTo(IExecutable exe) => exe == null ? 1 : Priority.CompareTo(exe.Priority);

        protected readonly string identity;

        protected readonly int priority;
        public int Priority => priority;
        
#if SWOLE_ENV
        protected Interpreter interpreter;
        public Interpreter Interpreter => interpreter;

        public ExecutableScript(Interpreter interpreter, int priority)
        {
            this.interpreter = interpreter;
            this.priority = priority;
        }
#endif
        public ExecutableScript(SourceScript script, int priority, SwoleLogger logger = null, bool isPreParsed = false, string topAuthor = null, int autoIndentation = SwoleScriptSemantics.ssDefaultAutoIndentation, int startIndentation = SwoleScriptSemantics.ssDefaultStartIndentation, ICollection<SourceScript> localScripts = null) : this((script.packageInfo.NameIsValid ? script.packageInfo.name + "." : "") + (script.NameIsValid ? script.Name : ""), script.source, priority, logger, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts) { }
        public ExecutableScript(string identity, string source, int priority, SwoleLogger logger = null, bool isPreParsed = false, string topAuthor = null, int autoIndentation = SwoleScriptSemantics.ssDefaultAutoIndentation, int startIndentation = SwoleScriptSemantics.ssDefaultStartIndentation, ICollection<SourceScript> localScripts = null)
        {
            this.identity = identity;
            this.priority = priority;

#if SWOLE_ENV
            if (logger == null) logger = swole.DefaultLogger;
            interpreter = new Interpreter();
            interpreter.hostData = new DefaultRuntimeHost() { identifier = identity };
            interpreter.standardOutput = interpreter.implicitOutput = (s, ignored) => logger.Log(s);
            interpreter.errorOutput = (s, ignored) => logger.LogError(s);
#endif

            Recompile(default, source, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
        }

        protected List<PackageIdentifier> dependencies;
        protected PackageIdentifier[] outputDependencies;
        public ICollection<PackageIdentifier> Dependencies 
        {
            get
            {
                if (dependencies == null) return null;
                if (outputDependencies == null) outputDependencies = dependencies.ToArray();  
                return outputDependencies;
            } 
        }

        public void Recompile(ExecutionLayer layer, string source, bool isPreParsed = false, string topAuthor = null, int autoIndentation = SwoleScriptSemantics.ssDefaultAutoIndentation, int startIndentation = SwoleScriptSemantics.ssDefaultStartIndentation, ICollection<SourceScript> localScripts = null) => Recompile(source, isPreParsed, topAuthor, autoIndentation, startIndentation, localScripts);
        public void Recompile(string source, bool isPreParsed = false, string topAuthor = null, int autoIndentation = SwoleScriptSemantics.ssDefaultAutoIndentation, int startIndentation = SwoleScriptSemantics.ssDefaultStartIndentation, ICollection<SourceScript> localScripts = null)
        {

#if SWOLE_ENV
            if (!isPreParsed) 
            { 
                source = swole.ParseSource(source, ref dependencies, topAuthor, default, autoIndentation, startIndentation, localScripts);
                if (dependencies != null) outputDependencies = dependencies.ToArray();
            }

            if (string.IsNullOrWhiteSpace(source)) // Empty source
            {
                interpreter.Reset(); 
                return;
            }

            //interpreter.Stop(); // might need?
            interpreter.Reset(source);
            interpreter.Compile();
            if (interpreter.vm == null) // Didn't finish compilation
            {
                interpreter.errorOutput($"{ssMsgPrefix_Error} Failed to compile {(string.IsNullOrEmpty(identity) ? "an unidentified script" : "'" + identity + "'")}", false);
                //Dispose();
            }
#endif
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
#if SWOLE_ENV
            interpreter?.Stop();
            interpreter?.Reset(string.Empty);
            interpreter = null;
#endif
        }

        public virtual void Restart() 
        {
#if SWOLE_ENV
            interpreter?.Restart();
#endif
        }
        public void Restart(ExecutionLayer layer) => Restart();

        /// <summary>
        /// Set the interpreter hostData
        /// </summary>
        public virtual void SetHostData(IRuntimeHost hostData)
        {
#if SWOLE_ENV
            if (isDisposed || interpreter == null) return;
            interpreter.hostData = hostData;
#endif
        }

        public ExecutionResult Execute(ExecutionLayer layer, float timeOut = _defaultExecutionTimeout, SwoleLogger logger = null) => Execute(swole.DefaultEnvironment, timeOut, logger);

        public ExecutionResult Execute(IRuntimeEnvironment environment, float timeOut = _defaultExecutionTimeout, SwoleLogger logger = null)
        {
            if (disable) return ExecutionResult.Disabled;
            if (isDisposed) return ExecutionResult.Disposed;
            if (environment == null) return ExecutionResult.InvalidEnvironment;
#if SWOLE_ENV
            var result = environment.RunForPeriod(logger, interpreter, timeOut);
            if (result == ExecutionResult.Completed) OnComplete?.Invoke();
#else
            var result = ExecutionResult.None;
#endif
            return result;
        }

        public ExecutionResult ExecuteToCompletion(ExecutionLayer layer, float timeOut = _defaultCompleteExecutionTimeout, SwoleLogger logger = null) => ExecuteToCompletion(swole.DefaultEnvironment, timeOut, logger);

        public ExecutionResult ExecuteToCompletion(IRuntimeEnvironment environment, float timeOut = _defaultCompleteExecutionTimeout, SwoleLogger logger = null, bool restart = true, bool setVars = true)
        {
            if (disable) return ExecutionResult.Disabled;
            if (isDisposed) return ExecutionResult.Disposed;
            if (environment == null) return ExecutionResult.InvalidEnvironment; 
#if SWOLE_ENV
            if (interpreter.vm == null) return ExecutionResult.Completed; // script is empty or didn't compile
            var result = environment.RunUntilCompletion(logger, interpreter, timeOut, restart, setVars);
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
