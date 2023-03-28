using Microsoft.EntityFrameworkCore;
using NeuroSpeech.EntityAccessControl.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace NeuroSpeech.EntityAccessControl.Extensions
{
    internal class DbFunctionInvoker
    {
        public static IQueryable<T> CallFunction<T>(ISecureQueryProvider db, string function, JsonElement parameters)
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

        private static IQueryable<T> NotFunction<TDb,T>(TDb db, QueryParameter[] args)
            where TDb: DbContext
        {
            throw new MethodAccessException();
        }

        private static IQueryable<T> NotExternalFunction<TDb,T>(TDb db, QueryParameter[] args)
        {
            throw new MethodAccessException($"Method exists but ExternalFunction attribute not applied.");
        }

        static Func<TDb,QueryParameter[],IQueryable<T>> FromExpressionFunction<TDb,T>(MethodInfo method)
            where TDb: DbContext
        {
            IQueryable<T> FromExpression(TDb db, QueryParameter[] args)
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

                return db.FromExpression<T>(exp);
            }
            return FromExpression;
        }

        static Func<TDb, QueryParameter[], IQueryable<T>> Function<TDb, T>(MethodInfo method)
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


            var exp = Expression.Lambda<Func<TDb,QueryParameter[],IQueryable<T>>>(body, dbP, pe);

            return exp.Compile();
        }


        public static IQueryable<T> CallTypedFunction<Db,T>(ISecureQueryProvider sdb, string function, QueryParameter[] list)
            where T : class
            where Db: DbContext
        {
            var db = (Db)sdb;
            var type = db.GetType();
            var fx = type.StaticCacheGetOrCreate(
                $"static-function-{function}",
                () =>
                {
                    var m = type.GetMethod(function);
                    if (m == null)
                    {
                        return NotFunction<Db,T>;
                    }
                    if(m.GetCustomAttribute<ExternalFunctionAttribute>() == null)
                    {
                        return NotExternalFunction<Db,T>;
                    }
                    if(m.GetCustomAttribute<DbFunctionAttribute>() != null)
                    {
                        return FromExpressionFunction<Db,T>(m);
                    }

                    return Function<Db, T>(m);
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
