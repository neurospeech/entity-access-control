using Microsoft.EntityFrameworkCore;
using NeuroSpeech.EntityAccessControl.Internal;
using NeuroSpeech.EntityAccessControl.Security;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Serialization;

namespace NeuroSpeech.EntityAccessControl
{

    public interface IQueryContext
    {
        IQueryContext<T> OfType<T>();
    } 

    public interface IQueryContext<T>: IQueryContext
    {
        IQueryContext<T> Where(Expression<Func<T, bool>> filter);
        IQueryContext<T1> Set<T1>() where T1: class;

        IQueryable<T> ToQuery();

        IQueryContext<T2> Select<T2>(Expression<Func<T, T2>> expression);

    }

    public readonly struct QueryContext<T>: IQueryContext<T>
    {
        private readonly ISecureRepository db;
        private readonly IQueryable<T> queryable;

        public QueryContext(ISecureRepository db, IQueryable<T> queryable)
        {
            this.db = db;
            this.queryable = queryable;
        }

        public IQueryContext<T1> Set<T1>()
            where T1: class
        {
            return new QueryContext<T1>(db, db.Query<T1>());
        }

        public IQueryContext<T> Where(Expression<Func<T, bool>> filter)
        {
            return new QueryContext<T>(db, queryable.Where(filter));
        }

        public IQueryable<T> ToQuery()
        {
            return queryable;
        }

        public IQueryContext<T1> OfType<T1>()
        {
            return new QueryContext<T1>(db, queryable.OfType<T1>());
        }

        public IQueryContext<T2> Select<T2>(Expression<Func<T, T2>> expression)
        {
            var ne = expression.Body as NewExpression;
            var list = new List<Expression>();
            foreach(var p in ne.Arguments)
            {
                list.Add(Replace(p));
            }
            ne = ne.Update(list);
            expression = Expression.Lambda<Func<T, T2>>(ne, expression.Parameters);
            return new QueryContext<T2>(db, queryable.Select(expression));
        }

        Expression Replace(Expression original)
        {
            if (original is MemberExpression memberExpressiion)
            {
                if (memberExpressiion.Expression is not ParameterExpression)
                {
                    var ne = Replace(memberExpressiion.Expression);
                    if (ne.NodeType == ExpressionType.Constant 
                        && ne is ConstantExpression ce 
                        && ce.Value == null)
                    {
                        return Expression.Constant(null, original.Type);
                    }
                    if (ne != memberExpressiion.Expression)
                    {
                        memberExpressiion = memberExpressiion.Update(ne);
                    }
                }

                var property = (memberExpressiion.Member as PropertyInfo)!;
                var igc = db.GetIgnoreCondition(property);
                if (igc == JsonIgnoreCondition.Always)
                {
                    return Expression.Constant(null, original.Type);
                }

                var nav = db.Model.FindEntityType(property.DeclaringType)
                    .GetNavigations()
                    .FirstOrDefault(x => x.PropertyInfo == property);
                if (nav?.IsCollection ?? false)
                {
                    var itemType = nav.TargetEntityType.ClrType;

                    // apply where...
                    var method = this.GetType().GetMethod(nameof(Apply))!.MakeGenericMethod(itemType);
                    return (Expression)method.Invoke(this, new object[] { memberExpressiion })!;
                }
            }
            return original;
        }

        public Expression Apply<T1>(Expression expression)
            where T1: class
        {
            var qec = new QueryExpressionContext<T1>(new QueryContext<T1>(db, db.Query<T1>()!), expression);
            var r = db.Apply<T1>(qec);
            qec = (QueryExpressionContext<T1>)r;
            return qec.Expression;
        }

    }

    public struct QueryExpressionContext<T> : IQueryContext<T>
    {
        private readonly IQueryContext<T> parent;
        private readonly Expression expression;

        public Expression Expression => expression;

        private static MethodInfo MethodOfType = typeof(Enumerable)
            .GetMethod(nameof(Enumerable.OfType), new Type[] { typeof(IEnumerable<T>) })!;

        private static MethodInfo MethodWhere = typeof(Enumerable)
            .GetStaticMethod(nameof(Enumerable.Where),
                typeof(IEnumerable<T>),
                typeof(Func<T,bool>) )!
            .MakeGenericMethod(typeof(T))!;

        public QueryExpressionContext(IQueryContext<T> parent, Expression expression)
        {
            this.parent = parent;
            this.expression = expression;
        }

        public IQueryContext<T1> OfType<T1>()
        {
            var e = Expression.Call(null, MethodOfType, expression);
            return new QueryExpressionContext<T1>(parent.OfType<T1>(), e);
        }

        public IQueryContext<T2> Select<T2>(Expression<Func<T, T2>> expression)
        {
            return parent.Select(expression);
        }

        public IQueryContext<T1> Set<T1>() where T1 : class
        {
            return parent.Set<T1>();
        }

        public IQueryable<T> ToQuery()
        {
            // should never be called...
            throw new NotImplementedException();
        }

        public IQueryContext<T> Where(Expression<Func<T, bool>> filter)
        {
            var e = Expression.Call(null, MethodWhere, filter);
            return new QueryExpressionContext<T>(parent, e);
        }
    }

    public abstract class BaseSecurityRules<TC>
    {
        private RulesDictionary select = new RulesDictionary();
        private RulesDictionary insert = new RulesDictionary();
        private RulesDictionary update = new RulesDictionary();
        private RulesDictionary delete = new RulesDictionary();
        private RulesDictionary modify = new RulesDictionary();

        private Dictionary<PropertyInfo, JsonIgnoreCondition> ignoreConditions
            = new Dictionary<PropertyInfo, JsonIgnoreCondition>();

        internal IQueryContext<T> Apply<T>(IQueryContext<T> ts, TC client) where T : class
        {
            return select.As<T, TC>()(ts, client);
        }

        internal JsonIgnoreCondition GetIgnoreCondition(PropertyInfo property)
        {
            if (!ignoreConditions.TryGetValue(property, out var v))
            {
                v = property.GetCustomAttribute<JsonIgnoreAttribute>()?.Condition ?? JsonIgnoreCondition.Never;
                ignoreConditions[property] = v;
            }
            return v;
        }

        internal IQueryable<T> ApplyInsert<T>(IQueryContext<T> q, TC client) where T : class
        {
            return insert.As<T, TC>()(q, client).ToQuery();
        }

        internal IQueryable<T> ApplyDelete<T>(IQueryContext<T> q, TC client) where T : class
        {
            return delete.As<T, TC>()(q, client).ToQuery();
        }

        internal IQueryable<T> ApplyUpdate<T>(IQueryContext<T> q, TC client) where T : class
        {
            return update.As<T,TC>()(q, client).ToQuery();
        }

        /**
         * This will disable Json serialization for given property
         */
        public void Ignore<T>(Expression<Func<T,object>> expression,
            JsonIgnoreCondition condition = JsonIgnoreCondition.Always)
        {
            if (expression.Body is NewExpression nme)
            {
                foreach (var m in nme.Arguments)
                {
                    if (m is not MemberExpression member)
                        throw new ArgumentException($"Invalid expression");

                    while (member.Expression is not ParameterExpression pe)
                    {
                        if (member.Expression is not MemberExpression me2)
                            throw new ArgumentException($"Invalid expression");
                        member = me2;
                    }
                    if (member.Member is not PropertyInfo property1)
                        throw new ArgumentException($"Should be a property");
                    ignoreConditions[property1] = condition;
                }
                return;
            }
            if (expression.Body is not MemberExpression me)
                throw new ArgumentException($"Expression {expression} is not a valid member expression");
            if(me.Member is not PropertyInfo property)
                throw new ArgumentException($"Expression {expression} is not a valid property expression");
            ignoreConditions[property] = condition;
        }


        /// <summary>
        /// Set filter for select, insert, update, delete
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="select"></param>
        /// <param name="insert"></param>
        /// <param name="update"></param>
        /// <param name="delete"></param>
        protected void SetFilters<T>(
            Func<IQueryContext<T>, TC, IQueryContext<T>>? select = null,
            Func<IQueryContext<T>, TC, IQueryContext<T>>? insert = null,
            Func<IQueryContext<T>, TC, IQueryContext<T>>? update = null,
            Func<IQueryContext<T>, TC, IQueryContext<T>>? delete = null)
        {
            if (select != null)
                this.select.SetFunc<T, TC>(select);
            if (insert != null)
                this.insert.SetFunc<T, TC>(insert);
            if (update != null)
                this.update.SetFunc<T, TC>(update);
            if (delete != null)
                this.delete.SetFunc<T, TC>(delete);
        }

        /// <summary>
        /// Set one filter for all (select, insert, update, delete)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="all"></param>
        public void SetAllFilters<T>(
            Func<IQueryContext<T>, TC, IQueryContext<T>> all)
        {
            SetFilters<T>(all, all, all, all);
        }


        public static IQueryContext<T> Allow<T>(IQueryContext<T> q, TC c) => q;

        public static IQueryContext<T> Unauthorized<T>(IQueryContext<T> q, TC c)
                   where T : class
                   => throw new UnauthorizedAccessException();

        internal void VerifyModifyMember<T>(PropertyInfo propertyInfo, TC c)
        {
            throw new NotImplementedException();
        }
    }
}
