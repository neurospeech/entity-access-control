using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Xml.Linq;

namespace NeuroSpeech.EntityAccessControl
{
    internal class SecureIncludableQueryable<T, TP> : SecureQueryable<T>, IIncludableQueryable<T, TP>
    {
        public SecureIncludableQueryable(ISecureQueryProvider secureQueryProvider, IQueryable<T> start) : base(secureQueryProvider, start)
        {
        }
    }

    internal interface ISecureQueryable
    {
        IQueryable Query { get; }
    }

    internal class SecureQueryable<T> :
        IOrderedQueryable<T>,
        IAsyncEnumerable<T>,
        ISecureQueryable
    {
        internal readonly IQueryable<T> Start;

        public SecureQueryable(ISecureQueryProvider secureQueryProvider, IQueryable<T> start)
        {
            this.Start = start;
            this.Provider = new EntityAccessQueryProvider(secureQueryProvider, (start.Provider as IAsyncQueryProvider)!);
        }

        public Type ElementType => typeof(T);

        public Expression Expression => Start.Expression;

        public IQueryProvider Provider { get; }

        IQueryable ISecureQueryable.Query => Start;

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            if (Start is IAsyncEnumerable<T> en)
                return en.GetAsyncEnumerator(cancellationToken);
            throw new NotSupportedException();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return Start.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Start.GetEnumerator();
        }
    }
}
