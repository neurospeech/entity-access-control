using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NeuroSpeech.EntityAccessControl.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace NeuroSpeech.EntityAccessControl
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class RegisterEventsAttribute: Attribute
    {
        public readonly Type ContextType;

        public RegisterEventsAttribute(Type type)
        {
            this.ContextType = type;
        }
    }


    public class DbContextEvents<T>
        where T: BaseDbContext<T>
    {

        private readonly Dictionary<Type, Type> registrations = new();

        public DbContextEvents()
        {
            Register<DateRangeEvents>();

        }

        /// <summary>
        /// Registers all types decorated with
        /// `RegisterEvents` attribute
        /// </summary>
        /// <param name="assembly"></param>
        public void Register(Assembly? assembly = null)
        {
            var thisType = this.GetType();
            assembly ??= thisType.Assembly;
            var contextType = typeof(T);
            foreach(var type in assembly.GetTypes())
            {
                var register = type.GetCustomAttribute<RegisterEventsAttribute>();
                if (register == null || !contextType.IsAssignableFrom(register.ContextType))
                {
                    continue;
                }
                this.Register(type);
            }
        }

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

        internal IEntityEvents<T1>? GetEvents<T1>(IServiceProvider services)
            where T1 : class
        {
            var type = typeof(T1);
            if (registrations.TryGetValue(type, out var t))
            {
                return services.Build(t) as IEntityEvents<T1>;
            }
            return null;
        }

        public void Register<T1, TE>()
            where T1 : class
            where TE : DbEntityEvents<T1>
        {
            registrations[typeof(T1)] = typeof(TE);
        }

        public void Register<T1>()
            where T1: IEntityEvents
        {
            Register(typeof(T1));
        }

        private void Register(Type t)
        {
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

    internal class DateRangeEvents: DbEntityEvents<DateRange>
    {
        public override IQueryable<DateRange> Filter(IQueryable<DateRange> q)
        {
            return q;
        }
    }
}
