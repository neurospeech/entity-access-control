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
        public static IQueryable<T> CallTypedFunction<Db,T>(ISecureQueryProvider sdb, string function, QueryParameter[] list)
            where T : class
            where Db: DbContext
        {
            var db = (Db)sdb;
            var type = db.GetType();
            var fx = type.StaticCacheGetOrCreate($"static-function-{function}", () => type.GetMethod(function));

            if (fx == null)
                throw new MethodAccessException();

            if (fx.GetCustomAttribute<ExternalFunctionAttribute>() == null)
                throw new MethodAccessException($"Method exists but ExternalFunction attribute not applied.");

            var pe = Expression.Constant(list);

            var parameters = fx.GetParameters();

            var length = parameters.Length;

            var plist = new Expression[length];

            for (int i = 0; i < length; i++)
            {
                var pi = parameters[i];
                plist[i] = Expression.Convert(Expression.ArrayIndex(pe, Expression.Constant(i) ), pi.ParameterType);
            }

            Expression body = Expression.Call(Expression.Constant(db), fx, plist);


            var exp = Expression.Lambda<Func<IQueryable<T>>>(body);

            return db.FromExpression<T>(exp);

        }

    }
}
