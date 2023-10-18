using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace Swole
{

    /// <summary>
    /// When normal type compatibility implementations aren't enough.
    /// Source: https://stackoverflow.com/questions/32025201/how-can-i-determine-if-an-implicit-cast-exists-in-c
    /// </summary>
    public static class TypeCompatibilityExtensions
    {

        public static bool CanCast(this Type from, Type to)
        {
            if (to.IsAssignableFrom(from))
            {
                return true;
            }
            if (HasImplicitConversion(from, from, to) || HasImplicitConversion(to, from, to))
            {
                return true;
            }
            if (_implicitNumericConversions.TryGetValue(to, out var list) && (list.Contains(from) || list.Any(t => CanCast(from, t))))
            {
                return true;
            }
            if (to.IsEnum)
            {
                return CanCast(from, Enum.GetUnderlyingType(to));
            }
            return Nullable.GetUnderlyingType(to) != null && CanCast(from, Nullable.GetUnderlyingType(to));
        }
         
        private static readonly object[] castParams = new object[1];
        public static T Cast<T>(this object obj)
        {
            if (obj == null) return default;

            Type from = obj.GetType();
            Type to = typeof(T);
            if (to.IsAssignableFrom(from)) return (T)obj; 

            if (from.TryGetImplicitConversion(from, to, out var conversionMethod)) 
            {
                castParams[0] = obj;
                return (T)conversionMethod.Invoke(null, castParams); 
            }
            if (to.TryGetImplicitConversion(from, to, out conversionMethod)) 
            {
                castParams[0] = obj;
                return (T)conversionMethod.Invoke(null, castParams); 
            }

            if (_implicitNumericConversions.TryGetValue(to, out var list) && (list.Contains(from) || list.Any(t => CanCast(from, t)))) return (T)obj;
            if (to.IsEnum && CanCast(from, Enum.GetUnderlyingType(to))) return (T)obj;

            return default;
        }

        // https://msdn.microsoft.com/en-us/library/y5b434w4.aspx
        private static readonly Dictionary<Type, List<Type>> _implicitNumericConversions = new Dictionary<Type, List<Type>>()
        {
        {typeof(short), new List<Type> { typeof(sbyte), typeof(byte) }},
        {typeof(ushort), new List<Type> { typeof(byte), typeof(char) }},
        {typeof(int), new List<Type> { typeof(sbyte), typeof(byte), typeof(char), typeof(short), typeof(ushort) }},
        {typeof(uint), new List<Type> { typeof(byte), typeof(char), typeof(ushort) }},
        {typeof(long), new List<Type> {  typeof(sbyte), typeof(byte), typeof(char), typeof(short), typeof(ushort), typeof(int), typeof(uint) }},
        {typeof(ulong), new List<Type> { typeof(byte), typeof(char), typeof(ushort), typeof(uint) }},
        {typeof(float), new List<Type> { typeof(sbyte), typeof(byte), typeof(char), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong) }},
        {typeof(double), new List<Type> { typeof(sbyte), typeof(byte), typeof(char), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float) }}
        };

        public static bool HasImplicitPrimitiveConversion(this Type from, Type to)
        {
            return _implicitNumericConversions.TryGetValue(to, out var list) && list.Contains(from);
        }

        private static readonly Dictionary<Type, IEnumerable<MethodInfo>> _implicitCastsCache = new Dictionary<Type, IEnumerable<MethodInfo>>();
        private const string implicitOperatorName = "op_Implicit";
        public static IEnumerable<MethodInfo> FetchLocalImplicitCastsFor(Type type)
        {
            if (_implicitCastsCache.TryGetValue(type, out IEnumerable<MethodInfo> casts)) return casts;
            return _implicitCastsCache[type] = type.GetMethods(BindingFlags.Public | BindingFlags.Static).Where(mi => mi.Name == implicitOperatorName);
        }

        public static bool HasImplicitConversion(this Type definedOn, Type from, Type to)
        {
            bool hasPrimitiveConversion = HasImplicitPrimitiveConversion(from, to);
            return FetchLocalImplicitCastsFor(definedOn).Any(mi =>
                {
                    if (!(hasPrimitiveConversion || mi.ReturnType == to)) return false;
                    var pi = mi.GetParameters().FirstOrDefault();
                    return pi != null && (pi.ParameterType == from || HasImplicitPrimitiveConversion(from, pi.ParameterType));
                });
        }

        public static bool TryGetImplicitConversion(this Type definedOn, Type from, Type to, out MethodInfo conversionMethod)
        {
            conversionMethod = null;
            try
            {
                bool hasPrimitiveConversion = HasImplicitPrimitiveConversion(from, to);
                conversionMethod = FetchLocalImplicitCastsFor(definedOn).FirstOrDefault(mi =>
                {
                    if (!(hasPrimitiveConversion || mi.ReturnType == to)) return false;
                    var pi = mi.GetParameters().FirstOrDefault();
                    return pi != null && (pi.ParameterType == from || HasImplicitPrimitiveConversion(from, pi.ParameterType));
                });         
            } catch { }
            return conversionMethod != null;
        }

    }

}
