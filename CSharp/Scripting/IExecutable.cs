using System;

namespace Swolescript
{

    public interface IExecutable : IDisposable, IComparable<IExecutable>
    {

        public int Priority { get; }

        public bool IsValid { get; }

        public bool Enabled { get; set; }

        public ExecutionResult Execute(ExecutionLayer layer, float timeOut = 0.01f);

        public ExecutionResult ExecuteToCompletion(ExecutionLayer layer, float timeOut = 0.1f);

    }

}
