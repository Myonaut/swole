using System;
using System.Collections.Generic;

namespace Swole.Script
{

    public interface IExecutable : IDisposable, IComparable<IExecutable>
    {

        public int Priority { get; }

        public bool IsValid { get; }

        public bool Enabled { get; set; }

        public void Restart(ExecutionLayer layer);

        public void SetHostData(IRuntimeHost hostData);

        public ExecutionResult Execute(ExecutionLayer layer, float timeOut = 0.01f, SwoleLogger logger = null);

        public ExecutionResult ExecuteToCompletion(ExecutionLayer layer, float timeOut = 0.1f, SwoleLogger logger = null);

        public ICollection<PackageIdentifier> Dependencies { get; }

        public void Recompile(ExecutionLayer layer, string source, bool isPreParsed = false, string topAuthor = null, int autoIndentation = SwoleScriptSemantics.ssDefaultAutoIndentation, int startIndentation = SwoleScriptSemantics.ssDefaultStartIndentation, ICollection<SourceScript> localScripts = null);

    }

}
