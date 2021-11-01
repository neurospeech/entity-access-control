using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace NeuroSpeech.EntityAccessControl
{

    public abstract class Rules<T, TC>
        where T: class
    {

        private static MethodInfo ofTypeMethod = typeof(Queryable)
            .GetMethod(nameof(Queryable.OfType))!;


        internal static IQueryable<T> Unauthorized(IQueryable<T> q, TC client) =>
           throw new NoSecurityRulesDefinedException($"No security rule defined for {typeof(T).Name}");

        internal static Type? BaseType = typeof(T).BaseType != null
            && typeof(T).BaseType != typeof(object)
            ? typeof(T).BaseType
            : null;

        private static Func<IQueryable<T>, TC, IQueryable<T>>? selectFilter = null;
        private static Func<IQueryable<T>, TC, IQueryable<T>>? insertFilter = null;
        private static Func<IQueryable<T>, TC, IQueryable<T>>? updateFilter = null;
        private static Func<IQueryable<T>, TC, IQueryable<T>>? deleteFilter = null;

        private static Func<IQueryable<T>, TC, IQueryable<T>> InitFilter(
            [NotNull]
            ref Func<IQueryable<T>, TC, IQueryable<T>>? rule, string methodName)
        {
            if (rule == null)
            {
                // try to get base...
                if (BaseType != null)
                {
                    rule = GetBaseRule(BaseType, methodName);
                }
                if (rule == null)
                {
                    rule = Unauthorized;
                }
            }
            return rule;
        }

        public static IQueryable<T> Apply(IQueryable<T> q, TC client)
        {
            return InitFilter(ref selectFilter, nameof(Apply))(q, client);
        }


        private static Func<IQueryable<T>, TC, IQueryable<T>>? GetBaseRule(Type bt, string methodName)
        {
            var peQ = Expression.Parameter(typeof(IQueryable<T>));
            var peTC = Expression.Parameter(typeof(TC));
            var method = typeof(Rules<,>)
                .MakeGenericType(bt, typeof(TC))
                .GetMethod(methodName);

            var call = Expression.Call(null, method,
                   Expression.TypeAs(peQ, typeof(IQueryable<>).MakeGenericType(bt)), peTC);

            var ofType = ofTypeMethod.MakeGenericMethod(typeof(T));

            var body = Expression.Call(
                null,
                ofType,
                call);
            var l = Expression.Lambda<Func<IQueryable<T>, TC, IQueryable<T>>>(body, peQ, peTC);
            return l.Compile();
        }

        public static IQueryable<T> ApplyInsert(IQueryable<T> ts, TC client)
        {
            return InitFilter(ref insertFilter, nameof(ApplyInsert))(ts, client);
        }

        public static IQueryable<T> ApplyUpdate(IQueryable<T> ts, TC client)
        {
            return InitFilter(ref updateFilter, nameof(ApplyUpdate))(ts, client);
        }

        public static IQueryable<T> ApplyDelete(IQueryable<T> ts, TC client)
        {
            return InitFilter(ref deleteFilter, nameof(ApplyDelete))(ts, client);
        }

        public static void SetFilterForAll(
            Func<IQueryable<T>, TC, IQueryable<T>> all)
        {
            selectFilter = all;
            insertFilter = all;
            updateFilter = all;
            deleteFilter = all;
        }

        public static void SetAllFilter(
            Func<IQueryable<T>, TC, IQueryable<T>>? select = null,
            Func<IQueryable<T>, TC, IQueryable<T>>? insert = null,
            Func<IQueryable<T>, TC, IQueryable<T>>? update = null,
            Func<IQueryable<T>, TC, IQueryable<T>>? delete = null)
        {
            selectFilter = select;
            insertFilter = insert;
            updateFilter = update;
            deleteFilter = delete;
        }


    }
}
