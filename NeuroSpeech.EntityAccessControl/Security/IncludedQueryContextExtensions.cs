using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace NeuroSpeech.EntityAccessControl
{
    public static class IncludedQueryContextExtensions
    {

        public static Task<int> CountAsync<T, TP>(this IIncludableQueryContext<T, TP> q, CancellationToken cancellationToken = default)
            where T : class
        {
            return ((IncludableQueryContext<T, TP>)q).qc.CountAsync(cancellationToken);
        }

        public static Task<List<T>> ToListAsync<T, TP>(this IIncludableQueryContext<T, TP> @this, CancellationToken token = default)
            where T : class
        {
            return ((IncludableQueryContext<T, TP>)@this).qc.ToListAsync(token);
        }

        public static IIncludableQueryContext<TEntity, TProperty>
            ThenInclude<TEntity, TPreviousProperty, TProperty>(
            this IIncludableQueryContext<TEntity, IEnumerable<TPreviousProperty>> @this,
            Expression<Func<TPreviousProperty, TProperty>> path)
            where TEntity : class
            where TPreviousProperty : class
        {
            var q = @this as IIncludableQueryContext<TEntity, IEnumerable<TPreviousProperty>>;
            return q.AsCollectionThenInclude(path);
        }


        public static IIncludableQueryContext<T, TProperty> ThenInclude<T, TP, TProperty>(
            this IIncludableQueryContext<T, TP> @this, Expression<Func<TP, TProperty>> path)
            where T : class
        {
            if (@this is IIncludableQueryContext<T, TP> q)
                return q.ThenInclude(path);
            throw new InvalidOperationException();
        }

        public static IQueryContext<T> Take<T, TP>(this IIncludableQueryContext<T, TP> @this, int n) where T : class
        {
            return ((IncludableQueryContext<T, TP>)@this).qc.Take(n);
        }

        public static IQueryContext<T> Include<T, TP>(this IIncludableQueryContext<T, TP> q, string include)
            where T : class
        {
            return ((IncludableQueryContext<T, TP>)q).qc.Include(include);
        }

        public static IQueryContext<T> AsSplitQuery<T, TP>(this IIncludableQueryContext<T, TP> q) where T : class
        {
            return ((IncludableQueryContext<T, TP>)q).qc.AsSplitQuery();
        }

        public static IQueryContext<T> Skip<T, TP>(this IIncludableQueryContext<T, TP> q, int n)
            where T : class
        {
            return ((IncludableQueryContext<T, TP>)q).qc.Skip(n);
        }

        public static IQueryContext<T2> Select<T, TP, T2>(this IIncludableQueryContext<T, TP> @this, Expression<Func<T, T2>> expression)
            where T : class
            where T2 : class
        {
            return ((IncludableQueryContext<T, TP>)@this).qc.Select(expression);
        }
    }
}
