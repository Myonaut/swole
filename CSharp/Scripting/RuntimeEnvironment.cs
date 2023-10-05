using System;
using System.Collections;
using System.Collections.Generic;

using Miniscript;

using static Swolescript.SwoleScriptSemantics;

namespace Swolescript
{

    public class RuntimeEnvironment : IDisposable
    {

        public string name;

        protected readonly IVar[] globalVars;

        public RuntimeEnvironment(string name, ICollection<IVar> globalVars = null)
        {
            this.name = name;
            if (globalVars != null)
            {
                this.globalVars = new IVar[globalVars.Count];
                int i = 0;
                foreach(IVar gb in globalVars)
                {
                    this.globalVars[i] = gb; 
                    i++;
                }
            }
            else this.globalVars = null;
        }

        public ExecutionResult RunForPeriod(SwoleLogger logger, Interpreter interpreter, float timeOut = 0.01f, bool restartIfNotRunning = true, bool setVars = true)
        {

            if (!IsValid || interpreter == null) return ExecutionResult.Invalid;

            string identifier = "";
            if (interpreter.hostData is IHostData hostData) identifier = hostData.Identifier;
            identifier = $"(env:'{name}{(string.IsNullOrEmpty(identifier) ? "" : $", id:'{identifier}'")}')";

            try
            {

                if (restartIfNotRunning && !interpreter.Running()) interpreter.Restart();

                if (setVars)
                {

                    if (globalVars != null) foreach (var globalVar in globalVars) interpreter.SetGlobalValue(globalVar.Name, globalVar.ValueMS);

                }

                interpreter.RunUntilDone(timeOut); 

            } 
            catch(MiniscriptException ex)
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

        public ExecutionResult RunUntilCompletion(SwoleLogger logger, Interpreter interpreter, float timeOut = 10, bool restart = true, bool setVars = true)
        {

            if (!IsValid || interpreter == null) return ExecutionResult.Invalid;

            string identifier = "";
            if (interpreter.hostData is IHostData hostData) identifier = hostData.Identifier;
            identifier = $"(env:'{name}{(string.IsNullOrEmpty(identifier) ? "" : $", id:'{identifier}'")}')";

            try
            {

                if (restart) interpreter.Restart();

                if (setVars)
                {

                    if (globalVars != null) foreach (var globalVar in globalVars) interpreter.SetGlobalValue(globalVar.Name, globalVar.ValueMS);

                }

                if (interpreter.vm == null) return ExecutionResult.Invalid;
                double startTime = interpreter.vm.runTime;

                while (interpreter.Running())
                {

                    double elapsedTime = interpreter.vm.runTime - startTime;

                    if (elapsedTime > timeOut)
                    {

                        logger?.LogError($"{ssMsgPrefix_Error} (env:'{name}'): Failed to complete a {nameof(RunUntilCompletion)} call!");
                        return ExecutionResult.TimedOut;

                    }

                    interpreter.RunUntilDone(timeOut - elapsedTime);

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
                return ExecutionResult.Error;
            }
            return ExecutionResult.Completed;

        }

        private bool isDisposed = false;

        public bool IsValid => !isDisposed;

        public void Dispose()
        {
            isDisposed = true;
            if (globalVars != null) for (int a = 0; a < globalVars.Length; a++) globalVars[a] = null;
        }

    }

}
