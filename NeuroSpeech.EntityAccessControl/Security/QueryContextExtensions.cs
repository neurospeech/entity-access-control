using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using NeuroSpeech.EntityAccessControl.Parser;
using Org.BouncyCastle.Crypto.Modes.Gcm;
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

        // don't know what it is...
        //public static IQueryContext<T> WithSource<T>(this IQueryContext<T> @this, string source)
        //    where T : class
        //{
        //    if (@this is QueryContext<T> qc) {
        //        return qc.WithSource(source);
        //    }
        //    throw new NotSupportedException();
        //}

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

        // public static IQueryContext<DateRange> DateRange<T>(this IQueryContext<T> @this, )

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

        //public static IQueryContext<IGrouping<TKey,T>> GroupBy<T, TKey>(this IQueryContext<T> @this, Expression<Func<T, TKey>> expression)
        //    where T : class
        //{
        //    return ((QueryContext<T>)@this).GroupBy(expression);
        //}

        public static IQueryContext<DateRangeEntity<T>> JoinDateRange<T>(this IQueryContext<T> @this, DateTime start, DateTime end, string step)
            where T:class
        {
            return ((QueryContext<T>)@this).JoinDateRange(start, end, step);
        }

        public static InternalContainer<T> Container<T>(this IQueryContext<T> @this)
            where T: class
        {
            return new InternalContainer<T>(@this);
        }

        public struct InternalContainer<T>
            where T: class
        {
            private IQueryContext<T> queryContext;

            public InternalContainer(IQueryContext<T> @this) : this()
            {
                this.queryContext = @this;
            }

            public (IQueryContext<T> entity, IQueryContext<TInner> inner) JoinWith<TInner>()
                where TInner : class
            {
                return (queryContext, ((QueryContext<T>)queryContext).Set<TInner>());
            }
        }

        public static IQueryContext<WithInner<T,TInner>> Join<T, TInner, TKey>(
            this (IQueryContext<T> entity, IQueryContext<TInner> inner) @this,
            Expression<Func<T, TKey>> keySelector,
            Expression<Func<TInner, TKey>> joinKeySelector
            )
            where T: class
            where TInner: class
        {
            return ((QueryContext<T>)@this.entity).Join(@this.inner, keySelector, joinKeySelector);
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

        public static IQueryContext<T> ThenBy<T, T2>(this IQueryContext<T> @this, Expression<Func<T, T2>> expression)
            where T : class
        {
            return ((QueryContext<T>)@this).ThenBy(expression);
        }

        public static IQueryContext<T> ThenByDescending<T, T2>(this IQueryContext<T> @this, Expression<Func<T, T2>> expression)
            where T : class
        {
            return ((QueryContext<T>)@this).ThenByDescending(expression);
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
            bool hasPaging = false;
            if (start > 0)
            {
                q = q.Skip(start);
                hasPaging = true;
            }
            if (size > 0)
            {
                q = q.Take(size);
                hasPaging = true;
            }
            if (options.SplitInclude)
            {
                q = q.AsSplitQuery();
            }
            // if (options.Trace != null)
            {
                string text = source + "\r\n" + q.ToQueryString();
                options.Trace?.Invoke(text);
            }
            if (hasPaging)
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
