using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
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

        public static IQueryable<TResult> Select<T, TInner, TResult>(
            this (IQueryable<T> entity, IQueryable<TInner> inner) @this,
            Expression<Func<T, IQueryable<TInner>, TResult>> selector)
        {

            // we need to inject parameter at runtime...
            var yPE = selector.Parameters[1];

            var xPE = selector.Parameters[0];

            var query = @this.inner is SecureQueryable<TInner> seq ? seq.Start : @this.inner;

            Expression<Func<IQueryable<TInner>>> exp = () => query;

            var ce = exp.Body;
            var body = ReplaceExpressionVisitor.Replace(yPE, ce, selector.Body);
            var selectorFinal = Expression.Lambda<Func<T, TResult>>(body, xPE);
            return @this.entity.Select(
                selectorFinal);
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

        public static IQueryable<DateRange> DateRange<T>(this IQueryable<T> @this, DateTime start, DateTime end, string step)
            where T : class
        {
            if (@this.Provider is not EntityAccessQueryProvider q1)
            {
                throw new NotSupportedException();
            }
            var db = q1.db;
            return new SecureQueryable<DateRange>(db, db.DateRangeView(start, end, step));
        }

        public static IIncludableQueryable<T, TProperty>
            IncludeSecure<T, TProperty>(this IQueryable<T> queryable, string path)
            where T : class
        {
            throw new NotSupportedException();
        }



        public static IIncludableQueryable<T, TProperty> 
            IncludeSecure<T, TProperty>(this IQueryable<T> queryable, Expression<Func<T,TProperty>> path)
            where T: class
        {
            if (queryable.Provider is EntityAccessQueryProvider qp)
            {
                return new SecureIncludableQueryable<T, TProperty>( qp.db, qp.CreateQuery<T>(Expression.Call(
                    instance: null,
                    method: ReflectionHelper.QueryableClass.Include(typeof(T), typeof(TProperty)),
                    arguments: new[] {
                        queryable.Expression, Expression.Quote(path)
                    })));
            }
            return queryable.Include(path);
        }

        public static IIncludableQueryable<T, TNext>
            ThenIncludeSecure<T, TProperty, TNext>(this IIncludableQueryable<T, IEnumerable<TProperty>> queryable, Expression<Func<TProperty, TNext>> path)
            where T : class
        {
            if (queryable.Provider is EntityAccessQueryProvider qp)
            {
                return new SecureIncludableQueryable<T, TNext>(qp.db, qp.CreateQuery<T>(Expression.Call(
                    instance: null,
                    method: ReflectionHelper.QueryableClass.ThenIncludeEnumerable(typeof(T), typeof(TProperty), typeof(TNext)),
                    arguments: new[] {
                        queryable.Expression, Expression.Quote(path)
                    })));
            }
            return queryable.ThenInclude(path);
        }

        public static IIncludableQueryable<T, TNext>
            ThenIncludeSecure<T, TProperty, TNext>(this IIncludableQueryable<T, TProperty> queryable, Expression<Func<TProperty, TNext>> path)
            where T : class
        {
            if (queryable.Provider is EntityAccessQueryProvider qp)
            {
                return new SecureIncludableQueryable<T, TNext>(qp.db, qp.CreateQuery<T>(Expression.Call(
                    instance: null,
                    method: ReflectionHelper.QueryableClass.ThenInclude(typeof(T), typeof(TProperty), typeof(TNext)),
                    arguments: new[] {
                        queryable.Expression, Expression.Quote(path)
                    })));
            }
            return queryable.ThenInclude(path);
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
            if (options.Trace != null)
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
