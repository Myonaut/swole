using System;
using System.Collections.Generic;

namespace Swole.Script
{

    /// <summary>
    /// A collection of executable actions organized into layers and sorted by priority.
    /// </summary>
    public class ExecutionStack : IDisposable
    {

        protected readonly Dictionary<ExecutionLayer, List<IExecutable>> layers = new Dictionary<ExecutionLayer, List<IExecutable>>();

        /// <summary>
        /// Inserts an executable into a stack layer.
        /// </summary>
        public void Insert(ExecutionLayer layer, IExecutable exe)
        {

            if (!IsValid || (layer != ExecutionLayer.EarlyUpdate && layer != ExecutionLayer.Update && layer != ExecutionLayer.LateUpdate) || exe == null || !exe.IsValid) return;
            if (!layers.TryGetValue(layer, out List<IExecutable> exes))
            {
                exes = new List<IExecutable>();
                layers[layer] = exes;
            }

            exes.Add(exe);
            exes.Sort();

        }

        /// <summary>
        /// Removes all occurances of the executable in a specific stack layer.
        /// </summary>
        public bool RemoveAll(ExecutionLayer layer, IExecutable exe)
        {

            if (!IsValid || (layer != ExecutionLayer.EarlyUpdate && layer != ExecutionLayer.Update && layer != ExecutionLayer.LateUpdate) || exe == null) return false;
            if (!layers.TryGetValue(layer, out List<IExecutable> exes)) return false;

            return exes.RemoveAll(i => i == exe) > 0;

        }

        /// <summary>
        /// Removes all occurances of the executable in the entire stack.
        /// </summary>
        public bool RemoveAll(IExecutable exe)
        {
            if (!IsValid || exe == null) return false;

            bool removed = false;
            foreach (var layer in layers)
            {
                if (layer.Value == null) continue;
                if (layer.Value.RemoveAll(i => i == exe) > 0) removed = true;
            }
            return removed;

        }

        /// <summary>
        /// Removes all executables from the stack.
        /// </summary>
        public void Reset()
        {

            layers.Clear();

        }

        public void ExecuteLayer(ExecutionLayer layer)
        {
            if (!layers.TryGetValue(layer, out List<IExecutable> exes)) return;
            exes.RemoveAll(i => i == null || !i.IsValid);
            foreach (var exe in exes) if (exe.Enabled) exe.Execute(layer);
        }

        public void Evaluate()
        {

            ExecuteLayer(ExecutionLayer.EarlyUpdate);
            ExecuteLayer(ExecutionLayer.Update);

        }

        public void LateEvaluate() => ExecuteLayer(ExecutionLayer.LateUpdate);

        private bool isDisposed;

        public bool IsValid => !isDisposed;

        public void Dispose()
        {

            isDisposed = true;
            layers.Clear();

        }

    }

}
