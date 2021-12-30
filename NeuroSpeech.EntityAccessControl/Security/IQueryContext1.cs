using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace NeuroSpeech.EntityAccessControl
{
    public interface IQueryContext<T>: IQueryContext
    {
        IQueryContext<T> Where(Expression<Func<T, bool>> filter);
        IQueryContext<T1> Set<T1>() where T1: class;

        IQueryable<T> ToQuery();

        IQueryContext<T2> Select<T2>(Expression<Func<T, T2>> expression)
            where T2: class;

        IQueryContext<T> Skip(int n);

        IQueryContext<T> Take(int n);

        IQueryContext<T> Include(string include);

        IQueryContext<T> AsSplitQuery();

        Task<int> CountAsync();

        Task<List<T>> ToListAsync();

        IOrderedQueryContext<T> OrderBy(Expression<Func<T, object>> expression);

        IOrderedQueryContext<T> OrderByDescending(Expression<Func<T, object>> expression);

    }

    public interface IOrderedQueryContext<T>: IQueryContext<T>
    {
        IOrderedQueryContext<T> ThenBy(Expression<Func<T, object>> expression);

        IOrderedQueryContext<T> ThenByDescending(Expression<Func<T, object>> expression);

    }
}
