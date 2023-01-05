using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace NeuroSpeech.EntityAccessControl
{
    internal class ReflectionHelper
    {
        internal class EnumerableClass
        {
            private static MethodInfo? toList;

            public static MethodInfo ToList(Type TSource) =>
                 (toList ??= new Func<IEnumerable<object>, IList<object>>(Enumerable.ToList).GetMethodInfo().GetGenericMethodDefinition())
                  .MakeGenericMethod(TSource);


            private static MethodInfo? s_Where_TSource_2;

            public static MethodInfo Where_TSource_2(Type TSource) =>
                 (s_Where_TSource_2 ??= new Func<IEnumerable<object>, Func<object, bool>, IEnumerable<object>>(Enumerable.Where).GetMethodInfo().GetGenericMethodDefinition())
                  .MakeGenericMethod(TSource);

            private static MethodInfo? s_Where_Index_TSource_2;

            public static MethodInfo Where_Index_TSource_2(Type TSource) =>
                 (s_Where_Index_TSource_2 ??= new Func<IEnumerable<object>, Func<object,int, bool>, IEnumerable<object>>(Enumerable.Where).GetMethodInfo().GetGenericMethodDefinition())
                  .MakeGenericMethod(TSource);
        }

        internal class QueryableClass
        {
            private static MethodInfo? include;

            public static MethodInfo Include(Type type1, Type type2) =>
                (include ??= (new Func<IQueryable<object>, Expression<Func<object, object>>, IIncludableQueryable<object, object>>(EntityFrameworkQueryableExtensions.Include)).GetMethodInfo().GetGenericMethodDefinition())
                    .MakeGenericMethod(type1, type2);

            private static MethodInfo? thenIncludeEnumerable;
            public static MethodInfo ThenIncludeEnumerable(Type type1, Type type2, Type type3) =>
                (thenIncludeEnumerable ??= (new Func<IIncludableQueryable<object, IEnumerable<object>>, Expression<Func<object, object>>, IIncludableQueryable<object, object>>(EntityFrameworkQueryableExtensions.ThenInclude)).GetMethodInfo().GetGenericMethodDefinition())
                    .MakeGenericMethod(type1, type2, type3);

            private static MethodInfo? thenInclude;
            public static MethodInfo ThenInclude(Type type1, Type type2, Type type3) =>
                (thenInclude ??= (new Func<IIncludableQueryable<object, object>, Expression<Func<object, object>>, IIncludableQueryable<object, object>>(EntityFrameworkQueryableExtensions.ThenInclude)).GetMethodInfo().GetGenericMethodDefinition())
                    .MakeGenericMethod(type1, type2, type3);
        }

        internal class ExpressionClass
        {
            private static MethodInfo? lambda;
            public static MethodInfo Lambda(Type TSource) =>
                 (lambda ??= new Func<Expression, ParameterExpression[],Expression<Func<object>>>(Expression.Lambda<Func<object>>).GetMethodInfo().GetGenericMethodDefinition())
                  .MakeGenericMethod(TSource);
        }
    }
}
