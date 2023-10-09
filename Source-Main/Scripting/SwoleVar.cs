using System;
using System.Collections.Generic;

using Miniscript;

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

                if (value is T v) SetValue(v);

            }

        }
        public object PreviousValue => prevValue;

        public virtual T GetValue() => value;
        public virtual void SetValue(T val)
        {
            prevValue = val;
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

        protected Value valueMS;

        public Value ValueMS 
        {

            get
            {

                if (valueMS == null || typeof(IVolatile).IsAssignableFrom(typeof(T)) || !typeof(T).IsValueType || HasChangedValue()) valueMS = Scripting.GetValueMS(this, valueMS);

                return valueMS;

            }

        }

    }

}
