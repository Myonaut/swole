using System;

#if SWOLE_ENV
using Miniscript;
#endif

namespace Swole.Script
{

    public interface IVar
    {

        public string Name { get; }

        public object Value { get; set; }
        public object PreviousValue { get; }

        public bool ValueChanged { get; }

        public Type GetValueType();

#if SWOLE_ENV
        public Value ValueMS { get; }
#endif

    }

}
