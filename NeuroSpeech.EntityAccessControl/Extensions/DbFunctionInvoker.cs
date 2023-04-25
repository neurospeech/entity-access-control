using Microsoft.EntityFrameworkCore;
using NeuroSpeech.EntityAccessControl.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NeuroSpeech.EntityAccessControl.Extensions
{
    internal class DbFunctionInvoker
    {
        public static Task<IQueryable<T>> CallFunction<T>(ISecureQueryProvider db, string function, JsonElement parameters)
            where T : class
        {
            int lenght = parameters.GetArrayLength();
            var list = new QueryParameter[lenght];
            if (parameters.ValueKind == JsonValueKind.Array)
            {
                for (int i = 0; i < lenght; i++)
                {
                    list[i] = new QueryParameter(parameters[i]);
                }
            }
            return Generic.InvokeAs(db.GetType(), typeof(T), CallTypedFunction<DbContext, T>, db, function, list);
        }

        private static Task<IQueryable<T>> NotFunction<TDb,T>(TDb db, QueryParameter[] args)
            where TDb: DbContext
        {
            throw new MethodAccessException();
        }

        private static Task<IQueryable<T>> NotExternalFunction<TDb,T>(TDb db, QueryParameter[] args)
        {
            throw new MethodAccessException($"Method exists but ExternalFunction attribute not applied.");
        }

        static Func<TDb,QueryParameter[],Task<IQueryable<T>>> FromExpressionFunction<TDb,T>(MethodInfo method)
            where TDb: DbContext
        {
            Task<IQueryable<T>> FromExpression(TDb db, QueryParameter[] args)
            {
                var pe = Expression.Constant(args);

                var parameters = method.GetParameters();

                var length = parameters.Length;

                var plist = new Expression[length];

                for (int i = 0; i < length; i++)
                {
                    var pi = parameters[i];
                    plist[i] = Expression.Convert(Expression.ArrayIndex(pe, Expression.Constant(i)), pi.ParameterType);
                }

                Expression body = Expression.Call(Expression.Constant(db), method, plist);


                var exp = Expression.Lambda<Func<IQueryable<T>>>(body);

                return Task.FromResult(db.FromExpression<T>(exp));
            }
            return FromExpression;
        }

        static Func<TDb, QueryParameter[], Task<IQueryable<T>>> Function<TDb, T>(MethodInfo method)
            where TDb : DbContext
        {
            var dbP = Expression.Parameter(typeof(TDb));
            var pe = Expression.Parameter(typeof(QueryParameter[]));

            var parameters = method.GetParameters();

            var length = parameters.Length;

            var plist = new Expression[length];

            for (int i = 0; i < length; i++)
            {
                var pi = parameters[i];
                plist[i] = Expression.Convert(
                    Expression.ArrayIndex(pe, Expression.Constant(i)), pi.ParameterType);
            }

            Expression body = Expression.Call(dbP, method, plist);

            body = ToTask<T>(body);

            var exp = Expression.Lambda<Func<TDb, QueryParameter[], Task<IQueryable<T>>>>(body, dbP, pe);

            return exp.Compile();
        }

        private static Expression ToTask<T>(Expression body)
        {
            if(body.Type.IsConstructedGenericType && body.Type.GetGenericTypeDefinition() == typeof(Task<>))
            {
                return body;
            }
            // create Task.FromResult...
            Func<object, Task<object>> method = Task.FromResult<object>;
            var fromResult = method.Method.GetGenericMethodDefinition().MakeGenericMethod(typeof(IQueryable<T>));
            return body = Expression.Call(null, fromResult, body);
        }

        static Func<TDb, QueryParameter[], Task<IQueryable<T>>> EventFunction<TDb, T>(MethodInfo method)
            where TDb : DbContext
        {
            var dbP = Expression.Parameter(typeof(TDb));
            var pe = Expression.Parameter(typeof(QueryParameter[]));

            var et = method.DeclaringType;

            var etMethod = typeof(ISecureQueryProvider)
                .GetMethod(nameof(ISecureQueryProvider.GetEntityEvents), new Type[] { typeof(Type) });

            var parameters = method.GetParameters();

            var length = parameters.Length;

            var plist = new Expression[length];

            var callee = Expression.Convert(Expression.Call(dbP, etMethod, Expression.Constant(typeof(T))), et);

            for (int i = 0; i < length; i++)
            {
                var pi = parameters[i];
                plist[i] = Expression.Convert(
                    Expression.ArrayIndex(pe, Expression.Constant(i)), pi.ParameterType);
            }

            Expression body = Expression.Call(callee, method, plist);

            body = ToTask<T>(body);

            var exp = Expression.Lambda<Func<TDb, QueryParameter[], Task<IQueryable<T>>>>(body, dbP, pe);
            return exp.Compile();
        }


        public static Task<IQueryable<T>> CallTypedFunction<Db,T>(ISecureQueryProvider sdb, string function, QueryParameter[] list)
            where T : class
            where Db: DbContext
        {
            var db = (Db)sdb;

            var type = db.GetType();

            var entityType = typeof(T);

            var k = ("static-function", entityType, function);

            var fx = type.StaticCacheGetOrCreate(
                k,
                () =>
                {
                    var et = sdb.GetEntityEvents(entityType);
                    var m = et?.GetType().GetMethod(function);
                    if (m != null)
                    {
                        if(m.GetCustomAttribute<ExternalFunctionAttribute>() == null)
                        {
                            return NotExternalFunction<Db, T>;
                        }
                        return EventFunction<Db, T>(m);
                    }

                    m = type.GetMethod(function);
                    if (m != null)
                    {
                        if (m.GetCustomAttribute<ExternalFunctionAttribute>() == null)
                        {
                            return NotExternalFunction<Db, T>;
                        }
                        if (m.GetCustomAttribute<DbFunctionAttribute>() != null)
                        {
                            return FromExpressionFunction<Db, T>(m);
                        }

                        return Function<Db, T>(m);
                    }

                    return NotFunction<Db, T>;
                });

            return fx(db, list);

            //if (fx.method == null)
            //    throw new MethodAccessException();

            //if (fx.external != null)
            //    throw new MethodAccessException($"Method exists but ExternalFunction attribute not applied.");

            //var pe = Expression.Constant(list);

            //var parameters = fx.method.GetParameters();

            //var length = parameters.Length;

            //var plist = new Expression[length];

            //for (int i = 0; i < length; i++)
            //{
            //    var pi = parameters[i];
            //    plist[i] = Expression.Convert(Expression.ArrayIndex(pe, Expression.Constant(i) ), pi.ParameterType);
            //}

            //Expression body = Expression.Call(Expression.Constant(db), fx.method, plist);


            //var exp = Expression.Lambda<Func<IQueryable<T>>>(body);

            //return db.FromExpression<T>(exp);

        }

    }
}
