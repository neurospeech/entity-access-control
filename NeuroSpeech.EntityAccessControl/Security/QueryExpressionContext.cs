using NeuroSpeech.EntityAccessControl.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace NeuroSpeech.EntityAccessControl
{
    public struct QueryExpressionContext<T> : IQueryContext<T>
        where T: class
    {
        private readonly IQueryContext<T> parent;
        private readonly Expression expression;

        public Expression Expression => expression;

        private static MethodInfo MethodOfType = typeof(Enumerable)
            .GetMethod(nameof(Enumerable.OfType), new Type[] { typeof(IEnumerable<T>) })!;

        private static MethodInfo MethodWhere = typeof(Enumerable)
            .GetStaticMethod(nameof(Enumerable.Where),
            2, 
            x => x[1].IsGenericType && x[1].GetGenericTypeDefinition() == typeof(Func<,>))!
            .MakeGenericMethod(typeof(T))!;

        private static MethodInfo MethodToList = typeof(Enumerable)
                    .GetStaticMethod(nameof(Enumerable.ToList),
                    1,
                    x => true)!
                    .MakeGenericMethod(typeof(T))!;

        public static Expression ToList(Expression expression)
        {
            return Expression.Call(null,MethodToList,  expression )!;
        }

        public QueryExpressionContext(IQueryContext<T> parent, Expression expression)
        {
            this.parent = parent;
            this.expression = expression;
        }

        public IQueryContext<T1> OfType<T1>()
            where T1: class
        {
            var e = Expression.Call(null, MethodOfType, expression);
            return new QueryExpressionContext<T1>(parent.OfType<T1>(), e);
        }

        public IQueryContext<T2> Select<T2>(Expression<Func<T, T2>> expression)
            where T2: class
        {
            return parent.Select(expression);
        }

        public IQueryContext<T1> Set<T1>() where T1 : class
        {
            return parent.Set<T1>();
        }

        public IQueryable<T> ToQuery()
        {
            // should never be called...
            throw new NotImplementedException();
        }

        public IQueryContext<T> Where(Expression<Func<T, bool>> filter)
        {
            var e = Expression.Call(null, MethodWhere, expression, filter);
            return new QueryExpressionContext<T>(parent, e);
        }

        public IQueryContext<T> Skip(int n)
        {
            throw new NotImplementedException();
        }

        public IQueryContext<T> Take(int n)
        {
            throw new NotImplementedException();
        }

        public string ToQueryString()
        {
            throw new NotImplementedException();
        }

        public Task<int> CountAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<List<T>> ToListAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public IQueryContext<T> Include(string include)
        {
            throw new NotImplementedException();
        }

        public IOrderedQueryContext<T> OrderBy(Expression<Func<T, object>> expression)
        {
            throw new NotImplementedException();
        }

        public IOrderedQueryContext<T> OrderByDescending(Expression<Func<T, object>> expression)
        {
            throw new NotImplementedException();
        }

        public IQueryContext<T> AsSplitQuery()
        {
            throw new NotImplementedException();
        }

        public IQueryContext<T> Requires(Expression<Func<T, bool>> filter, string errorMessage)
        {
            return Where(filter);
        }

        public IIncludableQueryContext<T, TP> Include<TP>(Expression<Func<T, TP>> path) where TP : class
        {
            throw new NotImplementedException();
        }

        public IIncludableQueryContext<T, TP> Include<TP>(Expression<Func<T, IEnumerable<TP>>> path) where TP : class
        {
            throw new NotImplementedException();
        }

    }
}
