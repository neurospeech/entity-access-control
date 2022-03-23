using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace NeuroSpeech
{
    public static class ServiceBuilder
    {
        private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, object>> builders
            = new();

        private static readonly MethodInfo getRequiredService =
            typeof(Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions)
            .GetMethod(nameof(Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService),
                new Type[] { typeof(IServiceProvider) })!;

        public static T Build<T>(this IServiceProvider @this) where T: class
        {
            return (T)Build(@this, typeof(T))!;
        }

        public static object Build(this IServiceProvider services, Type type)
        {
            var f = builders.GetOrAdd(type, (k) => {
                var c = type.GetConstructors()
                    .OrderBy(x => x.GetParameters()?.Length ?? 0)
                    .FirstOrDefault();
                if (c == null)
                {
                    return (s) => throw new ArgumentException($"No public constructor found for type {type.FullName}");
                }
                var p = Expression.Parameter(typeof(IServiceProvider));
                if (c.GetParameters().Length == 0)
                {
                    return Expression.Lambda<Func<IServiceProvider, object>>(Expression.New(c), p).Compile();
                }

                var ps = c.GetParameters()
                    .Select(x => Expression.Call(getRequiredService.MakeGenericMethod(x.ParameterType), p))
                    .ToArray();

                return Expression.Lambda<Func<IServiceProvider, object>>(Expression.New(c, ps), p).Compile();

            });
            return f(services);
        }
    }
}
