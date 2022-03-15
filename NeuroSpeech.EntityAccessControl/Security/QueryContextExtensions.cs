using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using NeuroSpeech.EntityAccessControl.Parser;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace NeuroSpeech.EntityAccessControl
{
    public static class QueryContextExtensions {

        public static IQueryContext<T> WithSource<T>(this IQueryContext<T> @this, string source)
            where T : class
        {
            if (@this is QueryContext<T> qc) {
                return qc.WithSource(source);
            }
            throw new NotSupportedException();
        }

        public static IQueryContext<T> Where<T>(this IQueryContext<T> @this, Expression<Func<T,bool>> filter)
            where T: class
        {
            if (@this is QueryContext<T> queryContext)
            {
                return queryContext.Where(filter);
            }
            if (@this is QueryExpressionContext<T> queryExpressionContext)
            {
                return queryExpressionContext.Where(filter);
            }
            return ((QueryContext<T>)@this).Where(filter);
        }

        public static IQueryContext<T> Requires<T>(this IQueryContext<T> @this, Expression<Func<T, bool>> filter)
            where T : class
        {
            return ((QueryContext<T>)@this).Requires(filter);
        }

        public static IQueryContext<T2> Select<T, T2>(this IQueryContext<T> @this, Expression<Func<T, T2>> expression)
            where T : class
            where T2: class
        {
            return ((QueryContext<T>)@this).Select(expression);
        }

        public static IQueryContext<T> OrderBy<T, T2>(this IQueryContext<T> @this, Expression<Func<T, T2>> expression)
            where T : class
        {
            return ((QueryContext<T>)@this).OrderBy(expression);
        }

        public static IQueryContext<T> OrderByDescending<T, T2>(this IQueryContext<T> @this, Expression<Func<T, T2>> expression)
            where T : class
        {
            return ((QueryContext<T>)@this).OrderByDescending(expression);
        }

        public static Task<List<T>> ToListAsync<T>(this IQueryContext<T> @this, CancellationToken token = default)
            where T : class
        {
            return ((QueryContext<T>)@this).ToListAsync(token);
        }

        public static IIncludableQueryContext<T, TP> Include<T, TP>(this IQueryContext<T> @this, Expression<Func<T, TP>> path)
            where T: class
        {
            var q = @this as QueryContext<T>;
            return q.Include(path);
        }

        public static IIncludableQueryContext<TEntity, TProperty> 
            ThenInclude<TEntity, TPreviousProperty, TProperty>(
            this IIncludableQueryContext<TEntity, IEnumerable<TPreviousProperty>> @this, 
            Expression<Func<TPreviousProperty, TProperty>> path)
            where TEntity : class
            where TPreviousProperty: class
        {
            var q = @this as IIncludableQueryContext<TEntity, IEnumerable<TPreviousProperty>>;
            return q.AsCollectionThenInclude(path);
        }

        public static IIncludableQueryContext<T, TProperty> ThenInclude<T, TP, TProperty>(
            this IIncludableQueryContext<T, TP> @this, Expression<Func<TP, TProperty>> path)
            where T : class
        {
            var q = @this as IncludableQueryContext<T, TP>;
            return q.ThenInclude(path);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static async Task<LinqResult> ToPagedListAsync<T>(
            this IQueryContext<T> @this,
            LinqMethodOptions options,
            string source)
            where T: class
        {
            int start = options.Start;
            int size = options.Size;
            var cancellationToken = options.CancelToken;
            var q = @this as IQueryContext<T>;
            if (start > 0)
            {
                q = q.Skip(start);
            }
            if (size > 0)
            {
                q = q.Take(size);
            }
            if (options.SplitInclude)
            {
                q = q.AsSplitQuery();
            }
            if (options.Trace != null)
            {
                string text = source + "\r\n" + q.ToQueryString();
                options.Trace(text);
            }
            if (q != @this)
            {
                return new LinqResult
                {
                    Total = await @this.CountAsync(cancellationToken),
                    Items = (await q.ToListAsync(cancellationToken)).OfType<object>(),
                };
            }
            return new LinqResult
            {
                Items = (await @this.ToListAsync(cancellationToken)).OfType<object>(),
                Total = 0
            };
        }
    }
}
