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
    internal class SecureQueryable<T> : IOrderedQueryable<T>, IAsyncEnumerable<T>
    {
        private readonly IQueryable<T> start;

        public SecureQueryable(ISecureQueryProvider secureQueryProvider, IQueryable<T> start)
        {
            this.start = start;
            this.Provider = new EntityAccessQueryProvider(secureQueryProvider, (start.Provider as IAsyncQueryProvider)!);
        }

        public Type ElementType => typeof(T);

        public Expression Expression => start.Expression;

        public IQueryProvider Provider { get; }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            if (start is IAsyncEnumerable<T> en)
                return en.GetAsyncEnumerator(cancellationToken);
            throw new NotSupportedException();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return start.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return start.GetEnumerator();
        }
    }
}
