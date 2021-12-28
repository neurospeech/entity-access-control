using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace NeuroSpeech.EntityAccessControl
{
    public class DbEntityEvents<T> : IEntityEvents
    {
        public virtual Task Deleted(T entity)
        {
            return Task.CompletedTask;
        }

        Task IEntityEvents.Deleted(object entity)
        {
            return Deleted((T)entity);
        }

        public virtual Task Deleting(T entity)
        {
            return Task.CompletedTask;
        }

        Task IEntityEvents.Deleting(object entity)
        {
            return Deleting((T)entity);
        }

        public virtual Task Inserted(T entity)
        {
            return Task.CompletedTask;
        }

        Task IEntityEvents.Inserted(object entity)
        {
            return Inserted((T)entity);
        }

        public virtual Task Inserting(T entity)
        {
            return Task.CompletedTask;
        }

        Task IEntityEvents.Inserting(object entity)
        {
            return Inserting((T)entity);
        }

        public virtual Task Updated(T entity)
        {
            return Task.CompletedTask;
        }

        Task IEntityEvents.Updated(object entity)
        {
            return Updated((T)entity);
        }

        public virtual Task Updating(T entity)
        {
            return Task.CompletedTask;
        }

        Task IEntityEvents.Updating(object entity)
        {
            return Updating((T)entity);
        }
    }

    internal class DbContextDefaultEntityEvents<T>: DbEntityEvents<T>
    {
        public DbContextDefaultEntityEvents()
        {

        }
    }

    public class DbContextEvents<T>
        where T: BaseDbContext<T>
    {

        private static Type entityEventType = typeof(DbEntityEvents<>);

        private Dictionary<Type, Type> registrations = new Dictionary<Type, Type>();

        internal abstract class EntityHandler
        {
            public abstract Task Run(DbContext db, object entity);
        }

        internal IEntityEvents? GetEvents(IServiceProvider services, Type type)
        {
            if(registrations.TryGetValue(type, out var t))
            {
                return services.Build(t) as IEntityEvents;
            }
            return null;
        }

        public void Register<T, TE>()
            where TE: DbEntityEvents<T>
        {
            registrations[typeof(T)] = typeof(TE);
        }

        public void Register<T>()
            where T: IEntityEvents
        {
            var t = typeof(T);
            var start = t;
            while (!start.IsConstructedGenericType)
            {
                start = start.BaseType;
                if (start == null)
                    throw new ArgumentException($"Type is not derived from DbEntityEvents<>");
            }
            var et = start.GenericTypeArguments[0];
            registrations[et] = t;
        }
    }

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
