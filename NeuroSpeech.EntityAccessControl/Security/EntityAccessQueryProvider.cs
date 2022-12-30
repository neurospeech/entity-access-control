using Microsoft.EntityFrameworkCore.Query;
using NeuroSpeech.EntityAccessControl.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using System.Collections;
using Microsoft.AspNetCore.Http.Features;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace NeuroSpeech.EntityAccessControl
{
    internal class ReplaceExpression: ExpressionVisitor
    {
        private readonly ISecureQueryProvider db;
        private readonly IAsyncQueryProvider provider;
        private bool CanReplace;

        public ReplaceExpression(ISecureQueryProvider db, IAsyncQueryProvider provider)
        {
            this.db = db;
            this.provider = provider;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.Name == "Select")
            {
                CanReplace = true;
            }
            var args = node.Arguments.Select(Visit).ToList();
            var target = node.Object;
            if (target != null)
            {
                target = Visit(target);
            }
            var result = node.Update(target, args);
            if (CanReplace) CanReplace = false;
            return result;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (!CanReplace)
            {
                return base.VisitMember(node);
            }
            if (node.Expression is ConstantExpression ce)
            {
                return node;
            }

            var property = (node.Member as PropertyInfo)!;
            if (property == null)
            {
                return node;
            }
            var igc = db.GetIgnoredProperties(property.DeclaringType!);
            if (igc.Contains(property))
            {
                return Expression.Constant(null, node.Type);
            }

            var type = property.DeclaringType!;

            var entityType = db.Model.FindEntityType(type);

            var nav = entityType
                ?.GetNavigations()
                ?.FirstOrDefault(x => x.PropertyInfo == property);
            if (nav?.IsCollection ?? false)
            {
                var itemType = nav.TargetEntityType.ClrType;

                // apply where...
                return this.GetInstanceGenericMethod(nameof(Apply), itemType)
                    .As<Expression>()
                    .Invoke(node);
            }
            return base.VisitMember(node);
        }


        public Expression Apply<T1>(Expression expression)
            where T1 : class
        {
            
            var qec = db.Set<T1>();
            var r = db.Apply<T1>(qec, true);
            if (r == qec) {
                return expression;
            }

            if(r.Expression is not MethodCallExpression method)
            {
                throw new NotSupportedException();
            }

            var arg = method.Arguments[1];

            if (arg is UnaryExpression unary)
            {
                arg = unary.Operand;
            }
            var methodInfo = ReflectionHelper.EnumerableClass.Where_TSource_2(typeof(T1));



            var result = Expression.Call(
                null,
                methodInfo,
                expression,
                arg);

            return result;
        }

        class ReplaceLambdaToFunc: ExpressionVisitor {
            private readonly Expression from;
            private readonly Expression to;

            public ReplaceLambdaToFunc(Expression from, Expression to)
            {
                this.from = from;
                this.to = to;
            }

            public override Expression Visit(Expression node)
            {
                if (node == from)
                {
                    return to;
                }
                return base.Visit(node);
            }
        }
    }

    internal class ReflectionHelper
    {
        internal class EnumerableClass
        {
            private static MethodInfo? s_Where_TSource_2;

            public static MethodInfo Where_TSource_2(Type TSource) =>
                 (s_Where_TSource_2 ??= new Func<IEnumerable<object>, Func<object, bool>, IEnumerable<object>>(Enumerable.Where).GetMethodInfo().GetGenericMethodDefinition())
                  .MakeGenericMethod(TSource);

            private static MethodInfo? s_Where_Index_TSource_2;

            public static MethodInfo Where_Index_TSource_2(Type TSource) =>
                 (s_Where_Index_TSource_2 ??= new Func<IEnumerable<object>, Func<object,int, bool>, IEnumerable<object>>(Enumerable.Where).GetMethodInfo().GetGenericMethodDefinition())
                  .MakeGenericMethod(TSource);
        }
    }

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

            if (expression is MethodCallExpression method && method.Method.Name == "Select") {                 
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
