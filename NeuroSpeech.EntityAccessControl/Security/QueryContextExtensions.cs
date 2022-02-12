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

        public static IQueryContext<T1> Set<T, T1>(this IQueryContext<T> q)
            where T1 : class
            where T : class
        {
            if (q is QueryContext<T> qc)
                return qc.Set<T1>();
            if (q is QueryExpressionContext<T> qec)
                return qec.parent.Set<T, T1>();
            throw new InvalidOperationException();
        }

        public static IQueryable<T> ToQuery<T>(this IQueryContext<T> q) where T : class
        {
            return ((QueryContext<T>)q).ToQuery();
        }

        public static IQueryContext<T> Take<T>(this IQueryContext<T> q, int n) where T : class
        {
            return ((QueryContext<T>)q).Take(n);
        }

        public static IQueryContext<T> Include<T>(this IQueryContext<T> q, string include)
            where T : class
        {
            return  ((QueryContext<T>)q).Include(include);
        }

        public static string ToQueryString<T>(this IQueryContext q) where T : class {
            return ((QueryContext<T>)q).ToQueryString();
        }

        public static IQueryContext<T> AsSplitQuery<T>(this IQueryContext<T> q) where T : class
        {
            return ((QueryContext<T>)q).AsSplitQuery();
        }

        public static Task<int> CountAsync<T>(this IQueryContext<T> q, CancellationToken cancellationToken = default)
            where T : class
        {
            return ((QueryContext<T>)q).CountAsync(cancellationToken);
        }


        public static IQueryContext<T> Skip<T>(this IQueryContext<T> q, int n)
            where T : class
        {
            return ((QueryContext<T>)q).Skip(n);
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
            if (@this is QueryContext<T> q)
                return q.Include(path);
            throw new InvalidOperationException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static async Task<LinqResult> ToPagedListAsync<T>(this IQueryContext<T> @this, LinqMethodOptions options)
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
