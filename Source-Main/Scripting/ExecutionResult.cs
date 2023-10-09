using System;

namespace Swole.Script
{

    [Serializable]
    public enum ExecutionResult
    {

        None, Running, Completed, Error, Invalid, TimedOut, InvalidEnvironment, Disposed, Disabled

    }

}
