using NeuroSpeech.EntityAccessControl.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace NeuroSpeech.EntityAccessControl
{
    public interface IQueryContext<out T>: IQueryContext
    {
        // IQueryContext<T> Where(Expression<Func<T, bool>> filter);

        // IQueryContext<T> Requires(Expression<Func<T, bool>> filter, string errorMessage);

        IQueryContext<T1> Set<T1>() where T1: class;

        IQueryable<T> ToQuery();

        //IQueryContext<T2> Select<T2>(Expression<Func<T, T2>> expression)
        //    where T2: class;

        IQueryContext<T> Skip(int n);

        IQueryContext<T> Take(int n);

        IQueryContext<T> Include(string include);

        // IIncludableQueryContext<T, TP> Include<TP>(Expression<Func<T,TP>> path) where TP: class;


        IQueryContext<T> AsSplitQuery();

        string ToQueryString();

        Task<int> CountAsync(CancellationToken cancellationToken = default);

        // Task<List<out T>> ToListAsync(CancellationToken cancellationToken = default);

        //IOrderedQueryContext<T> OrderBy(Expression<Func<T, object>> expression);

        //IOrderedQueryContext<T> OrderByDescending(Expression<Func<T, object>> expression);

    }

    public interface IIncludableQueryContext<out T, out TP> : IQueryContext<T>
    {
        // IIncludableQueryContext<T, TProperty> ThenInclude<TProperty>(Expression<Func<TP, TProperty>> path) where TProperty : class;
    }

    public interface IOrderedQueryContext<out T>: IQueryContext<T>
    {
        //IOrderedQueryContext<T> ThenBy(Expression<Func<T, object>> expression);

        //IOrderedQueryContext<T> ThenByDescending(Expression<Func<T, object>> expression);

    }
}
