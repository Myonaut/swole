using System;
using System.Collections.Generic;

#if SWOLE_ENV
using Miniscript;
#endif

namespace Swole.Script
{

    public class SwoleVar<T> : IVar
    {

        public static implicit operator T(SwoleVar<T> v) => v.GetValue();

        protected readonly string name;
        public string Name => name;

        protected readonly T defaultValue;
        public T DefaultValue => defaultValue;

        public SwoleVar(string name, T defaultValue)
        {
            this.name = name;
            this.defaultValue = defaultValue; 
        }

        protected T value;
        protected T prevValue;

        public virtual object Value
        {

            get => GetValue();

            set
            {

                if (value != null) SetValue(value.CastAs<T>());
                 
            }

        }
        public object PreviousValue => prevValue;

        public virtual T GetValue() => value;
        public virtual void SetValue(T val)
        {
            value = val;
        }

        public Type GetValueType() => typeof(T);

        protected bool HasChangedValue(bool resetState = true)
        {
            bool changed = !EqualityComparer<T>.Default.Equals(value, prevValue);
            if (resetState) prevValue = value;
            return changed;
        }
        public bool ValueChanged => HasChangedValue(false);

#if SWOLE_ENV
        protected Value valueMS;

        public Value ValueMS 
        {

            get
            {

                if (valueMS == null || typeof(IVolatile).IsAssignableFrom(typeof(T)) || !typeof(T).IsValueType || HasChangedValue()) valueMS = Scripting.GetValueMS(this, valueMS);

                return valueMS;

            }

        }
#endif

    }

}
