using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace LinqTestable.Sources.Infrastructure
{
    public static class TypeExtensions
    {
        public static bool IsAnonymous(this Type type)
        {
            bool hasCompilerGeneratedAttribute = type.GetCustomAttributes(typeof(CompilerGeneratedAttribute), false).Any();
            bool nameContainsAnonymousType = type.FullName.Contains("AnonymousType");
            bool isAnonymousType = hasCompilerGeneratedAttribute && nameContainsAnonymousType;
            return isAnonymousType;
        }

        internal static bool IsNotNullableNotBooleanStruct(this Type type)
        {
            if (type == typeof (bool))
                return false;

            if (type.IsInterface)
                return false;

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof (Nullable<>))
                return false;

            for (Type baseType = type.BaseType; baseType != typeof(object); baseType = baseType.BaseType)
            {
                if (baseType == typeof(ValueType))
                    return true;
            }

            return false;
        }

        internal static Type GetNullable(this Type type)
        {
            return typeof(Nullable<>).MakeGenericType(new[] {type});
        }
    }
}