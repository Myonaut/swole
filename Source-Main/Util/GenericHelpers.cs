using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace Swole
{

    public static class GenericHelpers
    {

        public static IEnumerable<Type> FindDerivedTypes(this Assembly assembly, Type baseType)
        {
            return assembly.GetTypes().Where(t => baseType.IsAssignableFrom(t));
        }

        public static List<Type> FindDerivedTypes(this Type baseType)
        {

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            List<Type> types = new List<Type>();

            foreach (Assembly assembly in assemblies) types.AddRange(FindDerivedTypes(assembly, baseType));

            return types;

        }

    }

}
