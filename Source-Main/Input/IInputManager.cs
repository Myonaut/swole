using System;
using System.Reflection;

namespace Swole
{
    public interface IInputManager
    {

        public bool IsStatic { get; }

        public PropertyInfo[] InputProperties { get; }

        public FieldInfo[] InputFields { get; }

        public MethodInfo[] InputMethods { get; }
    }
}
