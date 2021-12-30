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

        IQueryContext<T2> Select<T2>(Expression<Func<T, T2>> expression);

        IQueryContext<T> Skip(int n);

        IQueryContext<T> Take(int n);

        Task<int> CountAsync();

        Task<List<T>> ToListAsync();

    }
}
