using System;
using Miniscript;

namespace Swolescript
{

    public interface IVar
    {

        public string Name { get; }

        public object Value { get; set; }
        public object PreviousValue { get; }

        public bool ValueChanged { get; }

        public Type GetValueType();

        public Value ValueMS { get; }

    }

}
