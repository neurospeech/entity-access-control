using Microsoft.EntityFrameworkCore.Query;
using NeuroSpeech.EntityAccessControl.Internal;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace NeuroSpeech.EntityAccessControl
{
    public static class ExpressionPathExtensions
    {
        public static string ToExpressionPath(this Expression expression)
        {
            var p = new ToPathVisitor();
            expression = p.Visit(expression);
            return expression.ToString();
        }

        public class ToPathVisitor : ExpressionVisitor
        {

            private Dictionary<ParameterExpression, ParameterExpression> parameters
                = new();

            protected override Expression VisitParameter(ParameterExpression node)
            {
                if (parameters.TryGetValue(node, out var n))
                {
                    return base.VisitParameter(n);
                }
                return base.VisitParameter(node);
            }

            protected override Expression VisitLambda<T>(Expression<T> node)
            {
                var i = 0;
                var list = new List<ParameterExpression>();
                // create params..
                foreach (var p in node.Parameters)
                {
                    var r = Expression.Parameter(p.Type, $"p_{i}_{p.Type.FullName}");
                    parameters.Add(p, r);
                    list.Add(r);
                }

                var body = Visit(node.Body);

                return node.Update(body, list);
            }

        }
    }

    internal static class DoNotVisitExtensions
    {
        private static System.Runtime.CompilerServices.ConditionalWeakTable<Expression, object> doNotVisitCache = new System.Runtime.CompilerServices.ConditionalWeakTable<Expression, object>();

        public static void DoNotVisit(this Expression expression)
        {
            doNotVisitCache.AddOrUpdate(expression, "");
        }

        public static bool CanVisit(this Expression expression)
        {
            if(expression == null)
            {
                return false;
            }
            if(doNotVisitCache.TryGetValue(expression, out var result))
            {
                return false;
            }
            return true;
        }

        private static System.Runtime.CompilerServices.ConditionalWeakTable<ISecureQueryProvider, List<string>>
            visited = new System.Runtime.CompilerServices.ConditionalWeakTable<ISecureQueryProvider, List<string>>();

        public static bool IncludedAlready(this Expression expression, ISecureQueryProvider provider)
        {
            if(!visited.TryGetValue(provider, out var list))
            {
                list = new ();
                visited.Add(provider, list);
            }
            var path = expression.ToExpressionPath();
            foreach(var item in list)
            {
                if (item.StartsWith(path))
                {
                    return true;
                }
            }
            list.Add(path);
            return false;
        }
    }

    internal class ReplacementStack: Stack<bool>
    {
        public bool CanReplace => Count > 0 ? Peek() : false;

        public IDisposable EnableReplacement()
        {
            Push(true);
            return new DisposableAction(() => Pop());
        }
    }

    internal class InjectFiltersVisitor : ExpressionVisitor
    {
        private readonly ISecureQueryProvider db;
        private readonly IAsyncQueryProvider provider;
        private ReplacementStack replacementStack = new ReplacementStack();

        public InjectFiltersVisitor(ISecureQueryProvider db, IAsyncQueryProvider provider)
        {
            this.db = db;
            this.provider = provider;
        }

        public override Expression Visit(Expression node)
        {
            if (!node.CanVisit())
            {
                return node;
            }
            return base.Visit(node);
        }

        protected override Expression VisitNew(NewExpression node)
        {
            var args = new List<Expression>();
            foreach(var arg in node.Arguments)
            {
                var visited = Visit(arg);
                if (!arg.Type.IsAssignableFrom(visited.Type))
                {
                    visited = Expression.Call(
                        null, ReflectionHelper.EnumerableClass.ToList(visited.Type.GetFirstGenericArgument()),
                        visited);
                }
                // make tolist...
                args.Add(visited);
            }
            return node.Update(args);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            // Include with string must be split
            var isInclude = node.Method.Name == "Include" || node.Method.Name == "ThenInclude";
            if (isInclude)
            {
                if (node.Arguments.First().NodeType == ExpressionType.Constant)
                {
                    throw new NotSupportedException("Include with string literal is not supported");
                }
            }

            bool replace = false;

            if (node.Method.Name == "Select" || node.Method.Name == "SelectMany" || isInclude)
            {
                replace = true;
            }
            var target = node.Object;
            if (target != null)
            {
                target = Visit(target);
            }
            var args = new List<Expression>();

            var en = node.Arguments.GetEnumerator();

            if (target == null)
            {
                if (en.MoveNext())
                {
                    args.Add(Visit(en.Current));
                }
            }

            while(en.MoveNext())
            {
                var arg = en.Current;

                if(!replace)
                {
                    args.Add(Visit(arg));
                    continue;
                }

                if (isInclude)
                {
                    if (arg.IncludedAlready(db))
                    {
                        args.Add(arg);
                        continue;
                    }
                }

                using var a = replacementStack.EnableReplacement();
                args.Add(Visit(arg));
            }

            Expression result;
            if (isInclude || node.Method.Name == "SelectMany")
            {

                var method = node.Method.MatchArguments(target, args);
                result = Expression.Call(target, method, args);

                // var md = node.Method.GetGenericMethodDefinition();
                //if (node.Method.Name == "ThenInclude")
                //{
                //    var targetType = args[0].Type.GetFirstGenericArgument();
                //    var previousType = args[0].Type.GetSecondGenericArgument().GetFirstGenericArgument();
                //    var funcReturnType = args[1].Type.GetFuncReturnType();
                //    var method = md.MakeGenericMethod(targetType, previousType, funcReturnType);
                //    result = Expression.Call(target, method, args);
                //}
                //else
                //{
                //    // we need to change generic method definition...
                //    var targetType = args[0].Type.GetFirstGenericArgument();
                //    var funcReturnType = args[1].Type.GetFuncReturnType();
                //    var method = md.MakeGenericMethod(targetType, funcReturnType);
                //    result = Expression.Call(target, method, args);
                //}
            }
            else
            {
                result = node.Update(target, args);
            }
            return result;
        }

        //private Expression VisitAndConvert(Expression node)
        //{
        //    if (node.NodeType == ExpressionType.Quote && node is UnaryExpression quote)
        //    {
        //        var result = Visit(quote.Operand);
        //        return Expression.Quote(result);
        //    }
        //    return Visit(node);
        //}        

        protected override Expression VisitLambda<T>(Expression<T> node)
        {

            // Expression<Func<Project, List<ProjectRole>>> x;
            // cannot be used for parameter of type '
            // Expression<Func<Project,IEnumerable<ProjectRole>> of method
            // IQueryable<ProjectRole>
            //      SelectMany<Project,ProjectRole>(
            //          IQueryable<Project> @this,
            //          Expression<Func<Project,IEnumerable<ProjectRole>>> arg1)

            var body = Visit(node.Body);
            var type = typeof(T);
            if (type.IsConstructedGenericType
                && type.GenericTypeArguments.Last() is Type rt 
                && rt.IsConstructedGenericType
                && rt.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return Expression.Lambda(typeof(T), body, node.Parameters);
            }
            var result = Expression.Lambda(body, node.Parameters);
            return result;

        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (!replacementStack.CanReplace)
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
                var result = this.InvokeAs(itemType, Apply<object>, node, property);
                result.DoNotVisit();
                return result;
            }
            return base.VisitMember(node);
        }


        public Expression Apply<T1>(Expression expression, PropertyInfo? property)
            where T1 : class
        {
            
            var qec = db.Set<T1>();
            var r = db.Apply<T1>(qec, true, property);
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

       
    } 
    internal class ReplaceExpressionVisitor: ExpressionVisitor {
        
        public static Expression Replace(Expression from, Expression to, Expression body)
        {
            var visitor = new ReplaceExpressionVisitor(from, to);
            return visitor.Visit(body);
        }
        
        private readonly Expression from;
        private readonly Expression to;

        public ReplaceExpressionVisitor(Expression from, Expression to)
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
