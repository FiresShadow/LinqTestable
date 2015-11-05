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
    }
}