using Microsoft.EntityFrameworkCore.Query;
using NeuroSpeech.EntityAccessControl.Internal;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

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
            // Include with string must be split
            var isInclude = node.Method.Name == "Include" || node.Method.Name == "ThenInclude";
            if (isInclude)
            {
                if(node.Arguments.First().NodeType == ExpressionType.Constant)
                {
                    throw new NotSupportedException("Include with string literal is not supported");
                }
            }

            if (node.Method.Name == "Select" || isInclude)
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
}
