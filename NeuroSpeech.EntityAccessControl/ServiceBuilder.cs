using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace NeuroSpeech.EntityAccessControl
{
    internal static class ServiceBuilder
    {
        private static Dictionary<Type, Func<IServiceProvider, object>> builders
            = new Dictionary<Type, Func<IServiceProvider, object>>();

        private static MethodInfo getRequiredService =
            typeof(Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions)
            .GetMethod(nameof(Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService),
                new Type[] { typeof(IServiceProvider) })!;

        public static object? Build(this IServiceProvider services, Type type)
        {
            if(!builders.TryGetValue(type, out var f))
            {
                var c = type.GetConstructors()
                    .OrderBy(x => x.GetParameters()?.Length ?? 0)
                    .FirstOrDefault();
                if (c == null)
                {
                    f = (s) => throw new ArgumentException($"No public constructor found for type {type.FullName}");
                }
                else
                {
                    var p = Expression.Parameter(typeof(IServiceProvider));
                    if (c.GetParameters().Length == 0)
                    {
                        f = Expression.Lambda<Func<IServiceProvider, object>>(Expression.New(c), p).Compile();
                    } else
                    {

                        var ps = c.GetParameters()
                            .Select(x => Expression.Call(getRequiredService.MakeGenericMethod(x.ParameterType), p))
                            .ToArray();

                        f = Expression.Lambda<Func<IServiceProvider, object>>(Expression.New(c, ps), p).Compile();
                    }
                }
                builders[type] = f;
            }
            return f(services);
        }
    }
}
