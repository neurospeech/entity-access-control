using System;
using System.Linq;
using System.Linq.Expressions;

namespace NeuroSpeech.EntityAccessControl
{
    public interface IQueryContext<T>: IQueryContext
    {
        IQueryContext<T> Where(Expression<Func<T, bool>> filter);
        IQueryContext<T1> Set<T1>() where T1: class;

        IQueryable<T> ToQuery();

        IQueryContext<T2> Select<T2>(Expression<Func<T, T2>> expression);

    }
}
