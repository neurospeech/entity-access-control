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

        //public static Func<IQueryContext<T>, TC, IQueryContext<T>> FromBase<T, BT, TC>(
        //    Func<IQueryContext<BT>, TC, IQueryContext<T>> filter)
        //    where T: BT
        //{
        //    Func<IQueryContext<T> , TC, IQueryContext<T>> nf = (q, c) => {
        //        return filter(q.OfType<BT>(), c).OfType<T>();
        //    };
        //    return nf;
        //}

        public Func<IQueryable<T>, TC, IQueryable<T>> As<T, TC>()
            where T: class
        {
            var t = typeof(T);
            if (cached.TryGetValue(t, out var r))
                return (r as Func<IQueryable<T>, TC, IQueryable<T>>)!;

            // setup cache...
            Func<IQueryable<T>, TC, IQueryable<T>>? nf = null;

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
                        nf = asMethod.MakeGenericMethod(t, type, typeof(TC)).Invoke(null, new object[] { tr } ) as Func<IQueryable<T>, TC, IQueryable<T>>;
                    } else
                    {
                        nf += (asMethod.MakeGenericMethod(t, type, typeof(TC)).Invoke(null, new object[] { tr } ) as Func<IQueryable<T>, TC, IQueryable<T>>)!;
                    }
                }
            }

            if(rules.TryGetValue(t, out r))
            {
                nf += (r as Func<IQueryable<T>, TC, IQueryable<T>>)!;
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

        public void SetFunc<T, TC>(Func<IQueryable<T>, TC, IQueryable<T>>? filter)
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

}
