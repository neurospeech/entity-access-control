using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NeuroSpeech.EntityAccessControl
{
    internal class ReflectionHelper
    {
        internal class EnumerableClass
        {
            private static MethodInfo? s_Where_TSource_2;

            public static MethodInfo Where_TSource_2(Type TSource) =>
                 (s_Where_TSource_2 ??= new Func<IEnumerable<object>, Func<object, bool>, IEnumerable<object>>(Enumerable.Where).GetMethodInfo().GetGenericMethodDefinition())
                  .MakeGenericMethod(TSource);

            private static MethodInfo? s_Where_Index_TSource_2;

            public static MethodInfo Where_Index_TSource_2(Type TSource) =>
                 (s_Where_Index_TSource_2 ??= new Func<IEnumerable<object>, Func<object,int, bool>, IEnumerable<object>>(Enumerable.Where).GetMethodInfo().GetGenericMethodDefinition())
                  .MakeGenericMethod(TSource);
        }
    }
}
