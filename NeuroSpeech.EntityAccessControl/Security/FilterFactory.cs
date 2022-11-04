using Microsoft.EntityFrameworkCore.ChangeTracking;
using NeuroSpeech.EntityAccessControl.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        public IQueryContext<T> Where(Expression<Func<T,bool>> filter)
        {
            // careful
            // the type must change... 
            return factory.Set<T>().Where(filter);
        }

        public IQueryContext<TEntity> Set<TEntity>()
            where TEntity: class
        {
            return factory.Set<TEntity>();
        }

        public IQueryContext<TEntity> ModifyFilter<TEntity>()
            where TEntity : class
        {
            return factory.ModifyFilter<TEntity>();
        }

        public IQueryContext<TEntity> Filter<TEntity>()
            where TEntity : class
        {
            return factory.Filter<TEntity>();
        }

        internal IQueryContext ModifyFilter()
        {
            return factory.ModifyFilter();
        }

        internal IQueryContext Filter()
        {
            return factory.Filter();
        }
    }

    //public class FilterFactoryHelper
    //{

    //    public static FilterFactoryHelper Instance = new FilterFactoryHelper();

    //    public IQueryContext Filtered(FilterFactory factory)
    //    {
    //        return this.GetInstanceGenericMethod(nameof(FilteredSet), factory.fkPrimaryEntityType)
    //            .As<IQueryContext>()
    //            .Invoke(factory);
    //    }

    //    public IQueryContext FilteredSet<T>(FilterFactory factory)
    //        where T : class
    //    {
    //        return factory.Filtered<T>();
    //    }
    //}

    public readonly struct FilterFactory
    {
        private readonly ISecureQueryProvider db;
        private readonly Func<IQueryContext> modifyFilter;
        private readonly Func<IQueryContext> filter;

        internal FilterFactory(ISecureQueryProvider db,
            Func<IQueryContext> modifyFilter,
            Func<IQueryContext> filter)
        {
            this.db = db;
            this.modifyFilter = modifyFilter;
            this.filter = filter;
        }

        public IQueryContext ModifyFilter()
        {
            return modifyFilter();
        }

        public IQueryContext Filter()
        {
            return filter();
        }

        public IQueryContext<T> ModifyFilter<T>()
            where T: class
        {
            var feqc = new QueryContext<T>(db, db.Set<T>());
            var eh = db.GetEntityEvents<T>();
            if (eh == null)
            {
                throw new EntityAccessException($"Access to {typeof(T).Name} denied");
            }
            return eh.ModifyFilter(feqc);
        }

        public IQueryContext<T> Filter<T>()
            where T : class
        {
            var feqc = new QueryContext<T>(db, db.Set<T>());
            var eh = db.GetEntityEvents<T>();
            if (eh == null)
            {
                throw new EntityAccessException($"Access to {typeof(T).Name} denied");
            }
            return eh.Filter(feqc);
        }

        public IQueryContext<T> Set<T>()
            where T: class
        {
            return new QueryContext<T>(db, db.Set<T>());
        }
    }
}
