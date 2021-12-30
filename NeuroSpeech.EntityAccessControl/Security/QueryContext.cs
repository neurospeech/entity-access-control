using Microsoft.EntityFrameworkCore;
using NeuroSpeech.EntityAccessControl.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Serialization;

namespace NeuroSpeech.EntityAccessControl
{
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
            var fe = qec.Expression;
            if (fe.Type != expression.Type)
            {
                // tolist required...
                return QueryExpressionContext<T1>.ToList(fe);
            }
            return fe;
        }

    }
}
