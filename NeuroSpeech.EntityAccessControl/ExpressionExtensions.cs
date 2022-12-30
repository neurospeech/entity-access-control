using Microsoft.EntityFrameworkCore;
using NeuroSpeech.EntityAccessControl.Parser;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NeuroSpeech.EntityAccessControl
{

    public static class EntityAcccessQueryableExtensions
    {

        internal static ISecureQueryProvider GetSecureQueryProvider(this IQueryable q)
        {
            if (q.Provider is not EntityAccessQueryProvider provider)
                throw new NotSupportedException();
            return provider.db;
        }

        public static IQueryable<WithInner<T,TInner>> Join<T,TInner, TKey>(
            this (IQueryable<T> entity,IQueryable<TInner> inner) @this,
            Expression<Func<T,TKey>> keySelector,
            Expression<Func<TInner,TKey>> joinKeySelector)
        {
            return @this.entity.Join(
                @this.inner,
                keySelector,
                joinKeySelector,
                (x, y) => new WithInner<T, TInner> { Entity = x, Inner = y });
        }

        public static InternalContainer<T> Container<T>(this IQueryable<T> @this)
            where T : class
        {
            return new InternalContainer<T>(@this);
        }

        public struct InternalContainer<T>
            where T : class
        {
            private IQueryable<T> queryContext;

            public InternalContainer(IQueryable<T> @this) : this()
            {
                this.queryContext = @this;
            }

            public (IQueryable<T> entity, IQueryable<TInner> inner) JoinWith<TInner>()
                where TInner : class
            {
                return (queryContext, queryContext.GetSecureQueryProvider().FilteredQuery<TInner>());
            }
        }

        public static IQueryable<DateRangeEntity<T>> JoinDateRange<T>(this IQueryable<T> @this, DateTime start, DateTime end, string step)
            where T : class
        {
            if (@this.Provider is not EntityAccessQueryProvider q1)
            {
                throw new NotSupportedException();
            }
            var db = q1.db;
            var join = @this.Join(db.DateRangeView(start, end, step), x => true, y => true, (x, y) => new DateRangeEntity<T>
            {
                Entity = x,
                Range = y
            });

            return join;
        }


        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Is<T>(this Expression exp, ExpressionType nodeType, out T result)
            where T: Expression
        {
            if (exp.NodeType == nodeType)
            {
                result = (T)exp;
                return true;
            }
            result = null!;
            return false;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static async Task<LinqResult> ToPagedListAsync<T>(
            this IQueryable<T> @this,
            LinqMethodOptions options,
            string source)
            where T : class
        {
            int start = options.Start;
            int size = options.Size;
            var cancellationToken = options.CancelToken;
            var q = @this;
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
