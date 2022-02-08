using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using NeuroSpeech.EntityAccessControl.Parser;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace NeuroSpeech.EntityAccessControl
{
    public interface ISecureQueryProvider
    {
        IModel Model { get; }
        bool EnforceSecurity { get; set; }

        IQueryable<T> Query<T>() where T : class;
        JsonIgnoreCondition GetIgnoreCondition(PropertyInfo property);
        IQueryContext<T> Apply<T>(IQueryContext<T> qec) where T : class;
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
        Task<object?> FindByKeysAsync(IEntityType t, JsonElement item, CancellationToken cancellation = default);
        void Remove(object entity);

        void Add(object entity);
    }

    public readonly struct QueryContext<T>: IOrderedQueryContext<T>
        where T: class
    {
        private readonly ISecureQueryProvider db;
        private readonly IQueryable<T> queryable;
        private readonly ErrorModel? errorModel;

        public QueryContext(ISecureQueryProvider db, IQueryable<T> queryable, ErrorModel? errorModel = null)
        {
            this.db = db;
            this.queryable = queryable;
            this.errorModel = errorModel;
        }

        public IQueryContext<T1> Set<T1>()
            where T1: class
        {
            return new QueryContext<T1>(db, db.Query<T1>(), errorModel);
        }

        public IQueryContext<T> Where(Expression<Func<T, bool>> filter)
        {
            return new QueryContext<T>(db, queryable.Where(filter), errorModel);
        }

        public IQueryContext<T> Requires(Expression<Func<T, bool>> filter, string error)
        {
            errorModel?.Add("Error", error);
            return new QueryContext<T>(db, queryable.Where(filter), errorModel);
        }

        public IQueryable<T> ToQuery()
        {
            return queryable;
        }

        public IQueryContext<T1> OfType<T1>()
            where T1: class
        {
            return new QueryContext<T1>(db, queryable.OfType<T1>(), errorModel);
        }

        public IQueryContext<T2> Select<T2>(Expression<Func<T, T2>> expression)
            where T2: class
        {
            var ne = expression.Body as NewExpression;
            var list = new List<Expression>();
            foreach(var p in ne.Arguments)
            {
                list.Add(Replace(p));
            }
            ne = ne.Update(list);
            expression = Expression.Lambda<Func<T, T2>>(ne, expression.Parameters);
            return new QueryContext<T2>(db, queryable.Select(expression), errorModel);
        }

        Expression Replace(Expression original)
        {
            if (original is MemberExpression memberExpressiion)
            {
                if (memberExpressiion.Expression is not ParameterExpression)
                {
                    var ne = Replace(memberExpressiion.Expression);
                    if (ne.NodeType == ExpressionType.Constant 
                        && ne is ConstantExpression ce 
                        && ce.Value == null)
                    {
                        return Expression.Constant(null, original.Type);
                    }
                    if (ne != memberExpressiion.Expression)
                    {
                        memberExpressiion = memberExpressiion.Update(ne);
                    }
                }

                var property = (memberExpressiion.Member as PropertyInfo)!;
                var igc = db.GetIgnoreCondition(property);
                if (igc == JsonIgnoreCondition.Always)
                {
                    return Expression.Constant(null, original.Type);
                }

                var nav = db.Model.FindEntityType(property.DeclaringType)
                    .GetNavigations()
                    .FirstOrDefault(x => x.PropertyInfo == property);
                if (nav?.IsCollection ?? false)
                {
                    var itemType = nav.TargetEntityType.ClrType;

                    // apply where...
                    var method = this.GetType().GetMethod(nameof(Apply))!.MakeGenericMethod(itemType);
                    return (Expression)method.Invoke(this, new object[] { memberExpressiion })!;
                }
            }
            return original;
        }

        public Expression Apply<T1>(Expression expression)
            where T1: class
        {
            var qec = new QueryExpressionContext<T1>(new QueryContext<T1>(db, db.Query<T1>()!, errorModel), expression);
            var r = db.Apply<T1>(qec);
            qec = (QueryExpressionContext<T1>)r;
            var fe = qec.Expression;
            if (fe.Type != expression.Type)
            {
                // tolist required...
                return QueryExpressionContext<T1>.ToList(fe);
            }
            return fe;
        }

        public IQueryContext<T> Skip(int n)
        {
            return new QueryContext<T>(db, queryable.Skip(n), errorModel);
        }

        public IQueryContext<T> Take(int n)
        {
            return new QueryContext<T>(db, queryable.Take(n), errorModel);
        }

        public Task<int> CountAsync(CancellationToken cancellationToken)
        {
            return queryable.CountAsync(cancellationToken);
        }

        public string ToQueryString()
        {
            return queryable.ToQueryString();
        }

        public Task<List<T>> ToListAsync(CancellationToken cancellationToken)
        {
            return queryable.ToListAsync(cancellationToken);
        }

        public IQueryContext<T> Include(string include)
        {
            return new QueryContext<T>(db, queryable.Include(include), errorModel);
        }

        public IOrderedQueryContext<T> ThenBy(Expression<Func<T, object>> expression)
        {
            return new QueryContext<T>(db, (queryable as IOrderedQueryable<T>).ThenBy(expression), errorModel);
        }

        public IOrderedQueryContext<T> ThenByDescending(Expression<Func<T, object>> expression)
        {
            return new QueryContext<T>(db, (queryable as IOrderedQueryable<T>).ThenByDescending(expression), errorModel);
        }

        public IOrderedQueryContext<T> OrderBy(Expression<Func<T, object>> expression)
        {
            return new QueryContext<T>(db, queryable.OrderBy(expression), errorModel);
        }

        public IOrderedQueryContext<T> OrderByDescending(Expression<Func<T, object>> expression)
        {
            return new QueryContext<T>(db, queryable.OrderByDescending(expression), errorModel);
        }

        public IQueryContext<T> AsSplitQuery()
        {
            return new QueryContext<T>(db, queryable.AsSplitQuery(), errorModel);
        }

    }

    public static class QueryContextExtensions {

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static async Task<LinqResult> ToPagedListAsync<T>(this IQueryContext<T> @this, LinqMethodOptions options)
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
