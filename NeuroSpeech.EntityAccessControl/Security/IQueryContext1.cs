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


    }

    public interface IIncludableQueryContext<out T, out TP> : IQueryContext<T>
    {
        internal IIncludableQueryContext<T, TProperty> AsCollectionThenInclude<TPreviousProperty, TProperty>(Expression<Func<TPreviousProperty, TProperty>> path) where TPreviousProperty : class;
    }

    public interface IOrderedQueryContext<out T>: IQueryContext<T>
    {
        //IOrderedQueryContext<T> ThenBy(Expression<Func<T, object>> expression);

        //IOrderedQueryContext<T> ThenByDescending(Expression<Func<T, object>> expression);

    }
}
