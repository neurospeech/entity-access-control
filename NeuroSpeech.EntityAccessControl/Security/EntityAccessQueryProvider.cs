using Microsoft.EntityFrameworkCore.Query;
using NeuroSpeech.EntityAccessControl.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Collections;
using Microsoft.AspNetCore.Http.Features;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace NeuroSpeech.EntityAccessControl
{

    internal class EntityAccessQueryProvider : IAsyncQueryProvider
    {
        public readonly ISecureQueryProvider db;
        private IAsyncQueryProvider provider;

        public EntityAccessQueryProvider(ISecureQueryProvider sep, IAsyncQueryProvider provider)
        {
            this.db = sep;
            this.provider = provider;
            Visitor = new ReplaceExpression(db, provider);
        }

        private ReplaceExpression Visitor { get; }

        public IQueryable CreateQuery(Expression expression)
        {
            throw new NotImplementedException();
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            // check for methods...
            // select
            // include

            if (expression is MethodCallExpression method
                && (method.Method.Name == "Select"
                || method.Method.Name == "Include")) {  
                    expression = Visitor.Visit(expression);
            }

            return new SecureQueryable<TElement>(provider.CreateQuery<TElement>(expression), this);
        }

        public class SecureQueryable<T> : IOrderedQueryable<T>, IAsyncEnumerable<T>
        {
            private IQueryable<T> elements;
            private EntityAccessQueryProvider entityAccessQueryProvider;

            public SecureQueryable(IQueryable<T> elements, EntityAccessQueryProvider entityAccessQueryProvider)
            {
                this.elements = elements;
                this.entityAccessQueryProvider = entityAccessQueryProvider;
            }

            public Type ElementType => elements.ElementType;

            public Expression Expression => elements.Expression;

            public IQueryProvider Provider => entityAccessQueryProvider;

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
                if (elements is IAsyncEnumerable<T> en)
                    return en.GetAsyncEnumerator(cancellationToken);
                throw new NotSupportedException();
            }

            public IEnumerator<T> GetEnumerator()
            {
                return elements.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return elements.GetEnumerator();
            }
        }

        public object? Execute(Expression expression)
        {
            return provider.Execute(expression);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            return provider.Execute<TResult>(expression);
        }

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            return provider.ExecuteAsync<TResult>(expression, cancellationToken);
        }
    }
}
