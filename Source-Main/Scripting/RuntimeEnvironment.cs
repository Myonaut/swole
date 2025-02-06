using System;
using System.Collections;
using System.Collections.Generic;

#if SWOLE_ENV
using Miniscript;
#endif

using static Swole.Script.SwoleScriptSemantics;

namespace Swole.Script
{

    public delegate void RuntimeEventListenerDelegate(string eventName, float val, object sender);
    public delegate void CancelTokenDelegate(SwoleCancellationToken token);
    public class SwoleCancellationToken : IDisposable
    {
        protected event VoidParameterlessDelegate onCancel;
        protected event CancelTokenDelegate onCancel2;
        public void ListenForCancellation(VoidParameterlessDelegate listener)
        {
            if (cancelled) return;
            onCancel += listener;
        }
        public void ListenForCancellation(CancelTokenDelegate listener)
        {
            if (cancelled) return;
            onCancel2 += listener;
        }
        protected bool cancelled;
        public bool Cancel() 
        {
            if (IsCancelled) return false;

            try
            {
                onCancel?.Invoke();
            }
            catch(Exception ex)
            {
                swole.LogError(ex);
            }
            try
            {
                onCancel2?.Invoke(this);
            }
            catch (Exception ex)
            {
                swole.LogError(ex);
            }
            Dispose();
            return true;
        }
        public bool IsCancelled => cancelled;
        public void Dispose()
        {
            cancelled = true;
            onCancel = null;
            onCancel2 = null;
        }
    }

    public interface IRuntimeEventHandler
    {
        public void SubscribePreEvent(RuntimeEventListenerDelegate listener);
        public void UnsubscribePreEvent(RuntimeEventListenerDelegate listener);
        public void SubscribePostEvent(RuntimeEventListenerDelegate listener);
        public void UnsubscribePostEvent(RuntimeEventListenerDelegate listener);
    }
   
    public class RuntimeEventListener : IDisposable
    {

        private const string preStr = "pre";
        private const string postStr = "post";

        protected string id;
        public string ID => id;
#if SWOLE_ENV
        protected ValFunction msFunction;
        public ValFunction MSFunction => msFunction;
#endif
        protected IRuntimeEventHandler handler;
        public IRuntimeEventHandler Handler => handler;
        protected RuntimeEventListenerDelegate _delegate;
        public RuntimeEventListenerDelegate Delegate => _delegate;
        protected bool isPreEvent;
        public bool IsPreEvent => isPreEvent;

        public RuntimeEventListener(bool isPreEvent, IRuntimeEventHandler handler, RuntimeEventListenerDelegate _delegate)
        {
            this.isPreEvent = isPreEvent;
            this.handler = handler;
            this._delegate = _delegate;

            if (handler == null) return;
            if (isPreEvent) handler.SubscribePreEvent(_delegate); else handler.SubscribePostEvent(_delegate); 

            id = $"{(isPreEvent ? preStr : postStr)}_{(handler == null ? string.Empty : handler.GetHashCode())}_{(_delegate == null ? string.Empty : _delegate.Method.GetHashCode())}";
        }

#if SWOLE_ENV
        public RuntimeEventListener(bool isPreEvent, IRuntimeEventHandler handler, RuntimeEventListenerDelegate _delegate, ValFunction msFunction)
        {
            this.isPreEvent = isPreEvent;
            this.handler = handler;
            this._delegate = _delegate;
            this.msFunction = msFunction;

            if (handler == null) return;
            if (isPreEvent) handler.SubscribePreEvent(_delegate); else handler.SubscribePostEvent(_delegate);

            id = $"{(isPreEvent ? preStr : postStr)}_{(handler == null ? string.Empty : handler.GetHashCode())}_{(_delegate == null ? string.Empty : _delegate.Method.GetHashCode())}_{(msFunction == null ? string.Empty : msFunction.GetHashCode())}";
        }
#endif

        public void Dispose()
        {
            try
            {
                if (handler != null)
                {
                    if (isPreEvent) handler.UnsubscribePreEvent(_delegate); else handler.UnsubscribePostEvent(_delegate);
                }
            } 
            catch(Exception ex)
            {
                swole.LogError(ex);
            }

            handler = null;
            _delegate = null;
#if SWOLE_ENV
            msFunction = null;
#endif
        }
    }

    public interface IRuntimeEnvironment : IDisposable
    {
        public IRuntimeEnvironment Environment { get; }

        public string EnvironmentName { get; set; }

        public bool IsValid { get; }

#if SWOLE_ENV
        public void SetLocalVar(string identifier, Value value);
        public bool TryGetLocalVar(string identifier, out Value value);
        public Value GetLocalVar(string identifier);
        public ExecutionResult RunForPeriod(SwoleLogger logger, Interpreter interpreter, float timeOut = 0.01f, bool restartIfNotRunning = true, bool setGlobalVars = true);
        public ExecutionResult RunUntilCompletion(SwoleLogger logger, Interpreter interpreter, float timeOut = 10, bool restart = true, bool setGlobalVars = true);
#endif

        public void TrackEventListener(RuntimeEventListener listener);
        public bool UntrackEventListener(RuntimeEventListener listener);
        public bool UntrackEventListener(string trackerId);

        public RuntimeEventListener FindPreEventListener(RuntimeEventListenerDelegate _delegate, IRuntimeEventHandler handler);
        public RuntimeEventListener FindPostEventListener(RuntimeEventListenerDelegate _delegate, IRuntimeEventHandler handler);

#if SWOLE_ENV
        public RuntimeEventListener FindPreEventListener(ValFunction function, IRuntimeEventHandler handler);
        public RuntimeEventListener FindPostEventListener(ValFunction function, IRuntimeEventHandler handler);
#endif

        public SwoleCancellationToken GetNewCancellationToken();
        public void RemoveToken(SwoleCancellationToken token);
        public void CancelAllTokens();

    }

    /// <summary>
    /// Can execute code from a Miniscript Interpreter and set global variables that can be referenced by the code. 
    /// </summary>
    public class RuntimeEnvironment : IRuntimeEnvironment
    {

        public Dictionary<string, RuntimeEventListener> activeListeners = new Dictionary<string, RuntimeEventListener>();
        public void TrackEventListener(RuntimeEventListener listener)
        {
            if (listener == null || activeListeners == null || string.IsNullOrEmpty(listener.ID)) return;
            activeListeners[listener.ID] = listener;
        }
        public bool UntrackEventListener(RuntimeEventListener listener)
        {
            if (listener == null || activeListeners == null || listener.ID == null) return false;
            return activeListeners.Remove(listener.ID);
        }
        public bool UntrackEventListener(string trackerId)
        {
            if (activeListeners == null || trackerId == null) return false;
            return activeListeners.Remove(trackerId);
        }

        public RuntimeEventListener FindPreEventListener(RuntimeEventListenerDelegate _delegate, IRuntimeEventHandler handler)
        {
            if (activeListeners == null) return null;
            foreach (var pair in activeListeners) if (pair.Value.IsPreEvent && pair.Value.Delegate == _delegate && pair.Value.Handler == handler) return pair.Value;
            return null;
        }
        public RuntimeEventListener FindPostEventListener(RuntimeEventListenerDelegate _delegate, IRuntimeEventHandler handler)
        {
            if (activeListeners == null) return null;
            foreach (var pair in activeListeners) if (!pair.Value.IsPreEvent && pair.Value.Delegate == _delegate && pair.Value.Handler == handler) return pair.Value;
            return null;
        }

#if SWOLE_ENV
        public RuntimeEventListener FindPreEventListener(ValFunction function, IRuntimeEventHandler handler)
        {
            if (activeListeners == null) return null;
            foreach (var pair in activeListeners) if (pair.Value.IsPreEvent && pair.Value.MSFunction == function && pair.Value.Handler == handler) return pair.Value;
            return null;
        }
        public RuntimeEventListener FindPostEventListener(ValFunction function, IRuntimeEventHandler handler) 
        {
            if (activeListeners == null) return null;
            foreach (var pair in activeListeners) if (!pair.Value.IsPreEvent && pair.Value.MSFunction == function && pair.Value.Handler == handler) return pair.Value;
            return null;
        }
#endif

        public IRuntimeEnvironment Environment => this;

        public const string _globalVariablePrefix = "env_";
        public const string _localVarsAccessor = "vars";

        public string name;
        public string EnvironmentName
        {
            get => name;
            set
            {
                name = value;
            }
        }

        protected readonly IVar[] globalVars;

#if SWOLE_ENV
        protected ValMap localVars = new ValMap();
        public ValMap LocalVars
        {
            get
            {
                if (isDisposed) return null;
                if (localVars == null) localVars = new ValMap();
                return localVars;
            }
        }
        public void SetLocalVar(string identifier, Value value)
        {
            var vars = LocalVars;
            if (vars == null) return;
            vars[identifier] = value;
        }
        public bool TryGetLocalVar(string identifier, out Value value)
        {
            value = null;

            var vars = LocalVars;
            if (vars == null) return false;

            if (vars.TryGetValue(identifier, out value)) return true;

            return false;
        }
        public Value GetLocalVar(string identifier)
        {
            TryGetLocalVar(identifier, out Value val);
            return val;
        }
#endif

        public RuntimeEnvironment(string name, ICollection<IVar> globalVars = null)
        {
            this.name = name;
            if (globalVars != null)
            {
                this.globalVars = new IVar[globalVars.Count];
                int i = 0;
                foreach (IVar gb in globalVars)
                {
                    this.globalVars[i] = gb;
                    i++;
                }
            }
            else this.globalVars = null;
        }

        #if SWOLE_ENV
        public ExecutionResult RunForPeriod(SwoleLogger logger, Interpreter interpreter, float timeOut = 0.01f, bool restartIfNotRunning = true, bool setGlobalVars = true)
        {

            if (!IsValid || interpreter == null) return ExecutionResult.Invalid;

            string identifier = "";
            if (interpreter.hostData is IRuntimeHost hostData) identifier = hostData.Identifier;
            identifier = $"(env:'{name}{(string.IsNullOrEmpty(identifier) ? "" : $", id:'{identifier}'")}')";

            try
            {

                if (restartIfNotRunning && !interpreter.Running()) interpreter.Restart();

                if (setGlobalVars)
                {
                    if (globalVars != null) foreach (var globalVar in globalVars) interpreter.SetGlobalValue($"{_globalVariablePrefix}{globalVar.Name}", globalVar.ValueMS);
                }

                interpreter.SetGlobalValue(_localVarsAccessor, localVars);
                interpreter.RunUntilDone(timeOut);
                var localVars_ = interpreter.GetGlobalValue(_localVarsAccessor);
                if (localVars_ is ValMap) localVars = (ValMap)localVars_;

            }
            catch (MiniscriptException ex)
            {
                logger?.LogError($"{ssMsgPrefix_Error} {identifier}: " + ex.Description());
                return ExecutionResult.Error;
            }
            catch (Exception ex)
            {
                logger?.LogError($"{ssMsgPrefix_Error} {identifier}: " + ex.Message);
                return ExecutionResult.Error;
            }
            return interpreter.Running() ? ExecutionResult.Running : ExecutionResult.Completed;

        }

        public ExecutionResult RunUntilCompletion(SwoleLogger logger, Interpreter interpreter, float timeOut = 10, bool restart = true, bool setGlobalVars = true)
        {

            if (!IsValid || interpreter == null) return ExecutionResult.Invalid;

            string identifier = "";
            if (interpreter.hostData is IRuntimeHost hostData) identifier = hostData.Identifier;
            identifier = $"(env:'{name}'{(string.IsNullOrEmpty(identifier) ? "" : $", id:'{identifier}'")})"; 

            try
            {

                if (restart) interpreter.Restart();

                if (setGlobalVars)
                {
                    if (globalVars != null) foreach (var globalVar in globalVars) interpreter.SetGlobalValue($"{_globalVariablePrefix}{globalVar.Name}", globalVar.ValueMS);
                }

                if (interpreter.vm == null) return ExecutionResult.Invalid;
                double startTime = interpreter.vm.runTime;

                while (interpreter.Running())
                {

                    double elapsedTime = interpreter.vm.runTime - startTime;

                    if (elapsedTime > timeOut)
                    {

                        logger?.LogError($"{ssMsgPrefix_Error} (env:'{name}'): Timed out on a {nameof(RunUntilCompletion)} call! [{elapsedTime} ms]");
                        return ExecutionResult.TimedOut;

                    }

                    interpreter.SetGlobalValue(_localVarsAccessor, localVars);
                    interpreter.RunUntilDone(timeOut - elapsedTime);
                    var localVars_ = interpreter.GetGlobalValue(_localVarsAccessor);
                    if (localVars_ is ValMap) localVars = (ValMap)localVars_;

                }

            }
            catch (MiniscriptException ex)
            {
                logger?.LogError($"{ssMsgPrefix_Error} {identifier}: " + ex.Description()); 
                return ExecutionResult.Error;
            }
            catch (Exception ex)
            {
                logger?.LogError($"{ssMsgPrefix_Error} {identifier}: " + ex.Message);
                logger?.LogError(ex, true, true);
                return ExecutionResult.Error;
            }
            return ExecutionResult.Completed;

        }
        #endif

        private bool isDisposed = false;

        public bool IsValid => !isDisposed;

        public void Dispose()
        {
            isDisposed = true; 
            if (globalVars != null) for (int a = 0; a < globalVars.Length; a++) globalVars[a] = null;
#if SWOLE_ENV
            localVars = null;
#endif

            if (activeListeners != null) 
            { 
                foreach (var listener in activeListeners) if (listener.Value != null) listener.Value.Dispose();
                activeListeners.Clear();
            }
            activeListeners = null;

            if (cancellationTokens != null)
            {
                foreach (var token in cancellationTokens) token.Cancel();
                cancellationTokens.Clear();
            }
            cancellationTokens = null;
        }

        protected List<SwoleCancellationToken> cancellationTokens = new List<SwoleCancellationToken>();
        public SwoleCancellationToken GetNewCancellationToken()
        {
            if (isDisposed) return null;

            if (cancellationTokens == null) cancellationTokens = new List<SwoleCancellationToken>();
            var token = new SwoleCancellationToken();
            token.ListenForCancellation(RemoveToken);
            cancellationTokens.Add(token);
            return token; 
        }
        public void RemoveToken(SwoleCancellationToken token)
        {
            if (cancellationTokens == null) return;
            cancellationTokens.RemoveAll(i => i == token);
        }
        private static readonly List<SwoleCancellationToken> toCancel = new List<SwoleCancellationToken>();
        public void CancelAllTokens()
        {
            if (cancellationTokens == null) return;
            toCancel.Clear();
            toCancel.AddRange(cancellationTokens); // avoid collection modification exceptions
            foreach (var token in toCancel) token.Cancel(); 
            cancellationTokens.Clear();
            toCancel.Clear();
        }

    }

}
