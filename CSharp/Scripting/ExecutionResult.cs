using System;

namespace Swolescript
{

    [Serializable]
    public enum ExecutionResult
    {

        None, Running, Completed, Error, Invalid, TimedOut, InvalidEnvironment, Disposed, Disabled

    }

}
