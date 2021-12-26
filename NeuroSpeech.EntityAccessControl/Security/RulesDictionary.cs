using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NeuroSpeech.EntityAccessControl
{
    internal class RulesDictionary
    {

        private static MethodInfo asMethod = typeof(RulesDictionary).GetMethod("FromBase")!;

        private static MethodInfo asLambdaMethod = typeof(RulesDictionary).GetMethod("FromBaseLambda")!;

        private Dictionary<Type, object> rules = new Dictionary<Type, object>();
        private Dictionary<Type, object> cached = new Dictionary<Type, object>();

        public static Func<IQueryContext<T>, TC, IQueryContext<T>> FromBase<T, BT, TC>(
            Func<IQueryContext<BT>, TC, IQueryContext<T>> filter)
            where T: BT
        {
            Func<IQueryContext<T> , TC, IQueryContext<T>> nf = (q, c) => {
                return filter(q.OfType<BT>(), c).OfType<T>();
            };
            return nf;
        }

        public Func<IQueryContext<T>, TC, IQueryContext<T>> As<T, TC>()
            where T: class
        {
            var t = typeof(T);
            if (cached.TryGetValue(t, out var r))
                return (r as Func<IQueryContext<T>, TC, IQueryContext<T>>)!;

            // setup cache...
            Func<IQueryContext<T>, TC, IQueryContext<T>>? nf = null;

            List<Type> allTypes = new List<Type>();
            var start = t;
            while(start != null && start.BaseType != null)
            {
                allTypes.Insert(0, start.BaseType);
                start = start.BaseType;
            }

            foreach(var type in allTypes) { 
                if(rules.TryGetValue(type, out var tr)) {
                    if (nf == null)
                    {
                        nf = asMethod.MakeGenericMethod(t, type, typeof(TC)).Invoke(null, new object[] { tr } ) as Func<IQueryContext<T>, TC, IQueryContext<T>>;
                    } else
                    {
                        nf += (asMethod.MakeGenericMethod(t, type, typeof(TC)).Invoke(null, new object[] { tr } ) as Func<IQueryContext<T>, TC, IQueryContext<T>>)!;
                    }
                }
            }

            if(rules.TryGetValue(t, out r))
            {
                nf += (r as Func<IQueryContext<T>, TC, IQueryContext<T>>)!;
                cached[t] = nf;
                return nf;
            }

            if (nf == null)
            {
                nf = (q, c) => throw new NoSecurityRulesDefinedException($"No security rule defined for {typeof(T).Name}");
            }
            cached[t] = nf;
            return nf;
        }

        public void SetFunc<T, TC>(Func<IQueryContext<T>, TC, IQueryContext<T>>? filter)
        {
            if (filter == null)
            {
                rules.Remove(typeof(T));
            }
            else
            {
                rules[typeof(T)] = filter;
            }
            cached.Clear();
        }
    }

    //public class Rules<T, TC>
    //    where T: class
    //{

    //    private static MethodInfo ofTypeMethod = typeof(Queryable)
    //        .GetMethod(nameof(Queryable.OfType))!;


    //    internal static IQueryable<T> Unauthorized(IQueryable<T> q, TC client) =>
    //       throw new NoSecurityRulesDefinedException($"No security rule defined for {typeof(T).Name}");

    //    internal static Type? BaseType = typeof(T).BaseType != null
    //        && typeof(T).BaseType != typeof(object)
    //        ? typeof(T).BaseType
    //        : null;

    //    private Func<IQueryContext<T>, TC, IQueryContext<T>>? selectFilter = null;
    //    private Func<IQueryContext<T>, TC, IQueryContext<T>>? insertFilter = null;
    //    private Func<IQueryContext<T>, TC, IQueryContext<T>>? updateFilter = null;
    //    private Func<IQueryContext<T>, TC, IQueryContext<T>>? deleteFilter = null;

    //    //public Rules(
    //    //    Func<IQueryContext<T>, TC, IQueryContext<T>>? selectFilter,
    //    //    Func<IQueryContext<T>, TC, IQueryContext<T>>? insertFilter, 
    //    //    Func<IQueryContext<T>, TC, IQueryContext<T>>? updateFilter,
    //    //    Func<IQueryContext<T>, TC, IQueryContext<T>>? deleteFilter)
    //    //{

    //    //}

    //    private Func<IQueryContext<T>, TC, IQueryContext<T>> InitFilter(
    //        [NotNull]
    //        ref Func<IQueryContext<T>, TC, IQueryContext<T>>? rule, string methodName)
    //    {
    //        if (rule == null)
    //        {
    //            // try to get base...
    //            if (BaseType != null)
    //            {
    //                rule = GetBaseRule(BaseType, methodName);
    //            }
    //            if (rule == null)
    //            {
    //                rule = Unauthorized;
    //            }
    //        }
    //        return rule;
    //    }

    //    public IQueryable<T> Apply(IQueryable<T> q, TC client)
    //    {
    //        return InitFilter(ref selectFilter, nameof(Apply))(q, client);
    //    }


    //    private Func<IQueryContext<T>, TC, IQueryContext<T>>? GetBaseRule(Type bt, string methodName)
    //    {
    //        var peQ = Expression.Parameter(typeof(IQueryable<T>));
    //        var peTC = Expression.Parameter(typeof(TC));
    //        var method = typeof(Rules<,>)
    //            .MakeGenericType(bt, typeof(TC))
    //            .GetMethod(methodName);

    //        var call = Expression.Call(null, method,
    //               Expression.TypeAs(peQ, typeof(IQueryable<>).MakeGenericType(bt)), peTC);

    //        var ofType = ofTypeMethod.MakeGenericMethod(typeof(T));

    //        var body = Expression.Call(
    //            null,
    //            ofType,
    //            call);
    //        var l = Expression.Lambda<Func<IQueryContext<T>, TC, IQueryContext<T>>>(body, peQ, peTC);
    //        return l.Compile();
    //    }

    //    public IQueryable<T> ApplyInsert(IQueryable<T> ts, TC client)
    //    {
    //        return InitFilter(ref insertFilter, nameof(ApplyInsert))(ts, client);
    //    }

    //    public IQueryable<T> ApplyUpdate(IQueryable<T> ts, TC client)
    //    {
    //        return InitFilter(ref updateFilter, nameof(ApplyUpdate))(ts, client);
    //    }

    //    public IQueryable<T> ApplyDelete(IQueryable<T> ts, TC client)
    //    {
    //        return InitFilter(ref deleteFilter, nameof(ApplyDelete))(ts, client);
    //    }

    //    public void SetFilterForAll(
    //        Func<IQueryContext<T>, TC, IQueryContext<T>> all)
    //    {
    //        selectFilter = all;
    //        insertFilter = all;
    //        updateFilter = all;
    //        deleteFilter = all;
    //    }

    //    public void SetAllFilter(
    //        Func<IQueryContext<T>, TC, IQueryContext<T>>? select = null,
    //        Func<IQueryContext<T>, TC, IQueryContext<T>>? insert = null,
    //        Func<IQueryContext<T>, TC, IQueryContext<T>>? update = null,
    //        Func<IQueryContext<T>, TC, IQueryContext<T>>? delete = null)
    //    {
    //        selectFilter = select;
    //        insertFilter = insert;
    //        updateFilter = update;
    //        deleteFilter = delete;
    //    }


    //}
}
