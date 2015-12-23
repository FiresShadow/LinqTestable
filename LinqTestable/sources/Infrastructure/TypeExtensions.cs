using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace LinqTestable.Sources.Infrastructure
{
    internal static class TypeExtensions
    {
        internal static bool IsAnonymous(this Type type)
        {
            bool hasCompilerGeneratedAttribute = type.GetCustomAttributes(typeof(CompilerGeneratedAttribute), false).Any();
            bool nameContainsAnonymousType = type.FullName.Contains("AnonymousType");
            bool isAnonymousType = hasCompilerGeneratedAttribute && nameContainsAnonymousType;
            return isAnonymousType;
        }

        internal static bool IsNotNullableNotBooleanStruct(this Type type)
        {
            if (type == typeof(bool) || type == typeof(char))
                return false;

            if (type.IsInterface)
                return false;

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof (Nullable<>))
                return false;

            return type.IsStruct();
        }

        internal static Type GetNullable(this Type type)
        {
            return typeof(Nullable<>).MakeGenericType(new[] {type});
        }

        internal static bool IsStruct(this Type type)
        {
            return type.IsSubclassOf(typeof (ValueType));
        }

        internal static bool IsIEnumerable(this Type type)
        {
            return type.IsRealizedInterface(typeof(IEnumerable<>));
        }

        internal static bool IsIQueryable(this Type type)
        {
            return type.IsRealizedInterface(typeof(IQueryable<>));
        }

        private static bool IsRealizedInterface(this Type type, Type interfaceType)
        {
            if (type == typeof(CompressedObject)) //концептуально упакованный объект не будем считать списком других объектов, хотя упакованный объект и реализован через словарь
                return false;

            if (type.IsGenericType && type.GetGenericTypeDefinition() == interfaceType)
                return true;

            return type.GetInterface(interfaceType.Name) != null;
        }

        internal static Type GetIEnumerableParameter(this Type type)
        {
            var enumerableType = 
                (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)) ? 
                type :
                type.GetInterface(typeof(IEnumerable<>).Name);

            return enumerableType.GetGenericArguments().SingleOrDefault();
        }
    }
}