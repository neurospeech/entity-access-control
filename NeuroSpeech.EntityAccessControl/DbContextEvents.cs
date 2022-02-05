using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NeuroSpeech.EntityAccessControl
{

    public class DbContextEvents<T>
        where T: BaseDbContext<T>
    {

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

        public void Register<T1, TE>()
            where TE: DbEntityEvents<T1>
        {
            registrations[typeof(T1)] = typeof(TE);
        }

        public void Register<T1>()
            where T1: IEntityEvents
        {
            var t = typeof(T1);
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
}
