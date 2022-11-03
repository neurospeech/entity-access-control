using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace NeuroSpeech.EntityAccessControl.Security
{
    public readonly ref struct ForeignKeyInfo<T>
        where T : class
    {
        public readonly EntityEntry Entry;
        public readonly T Entity;
        public readonly PropertyInfo Property;
        public readonly object Value;
        private readonly FilterFactory factory;
        public string Name => Property.Name;

        public ForeignKeyInfo(
            EntityEntry entry,
            PropertyInfo property,
            object value,
            FilterFactory factory)
        {
            this.Entry = entry;
            this.Entity = (T)entry.Entity;
            this.Property = property;
            this.Value = value;
            this.factory = factory;
        }

        public bool Is(string name)
        {
            return this.Property.Name == name;
        }

        public bool Is<TR>(Expression<Func<T,TR>> exp)
        {
            if (exp.Body is MemberExpression me)
            {
                if(me.Member is PropertyInfo mp)
                {
                    if (mp == this.Property)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public IQueryContext Set()
        {
            return factory.Set();
        }

        public IQueryContext Filtered()
        {
            return factory.Filtered();
        }


        public IQueryContext<TEntity> Set<TEntity>()
        {
            return factory.Set<TEntity>();
        }

        public IQueryContext<TEntity> Filtered<TEntity>()
        {
            return factory.Filtered<TEntity>();
        }
    }

    public readonly struct FilterFactory
    {
        private readonly Func<IQueryContext> filteredSet;
        private readonly Func<IQueryContext> set;

        internal static FilterFactory From<T>(ISecureQueryProvider db)
            where T: class
        {
            var qc = () => new QueryContext<T>(db, db.Set<T>());
            var fqc = () =>
            {
                var feqc = new QueryContext<T>(db, db.Set<T>());
                var eh = db.GetEntityEvents(typeof(T));
                if (eh == null)
                {
                    throw new EntityAccessException($"Access to {typeof(T).Name} denied");
                }
                return eh.ModifyFilter(feqc);
            };
            return new FilterFactory(fqc, qc);
        }

        internal FilterFactory(Func<IQueryContext> filteredSet, Func<IQueryContext> set)
        {
            this.filteredSet = filteredSet;
            this.set = set;
        }


        public IQueryContext Filtered()
        {
            return this.filteredSet();
        }

        public IQueryContext Set()
        {
            return this.set();
        }

        public IQueryContext<T> Filtered<T>()
        {
            return (this.filteredSet() as IQueryContext<T>)!;
        }

        public IQueryContext<T> Set<T>()
        {
            return (this.set() as IQueryContext<T>)!;
        }
    }
}
