using Microsoft.EntityFrameworkCore.ChangeTracking;
using NeuroSpeech.EntityAccessControl.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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

        public IQueryable<T> Where(Expression<Func<T,bool>> filter)
        {
            // careful
            // the type must change... 
            return factory.Set<T>().Where(filter);
        }

        public IQueryable<TEntity> Set<TEntity>()
            where TEntity: class
        {
            return factory.Set<TEntity>();
        }

        public IQueryable<TEntity> ModifyFilter<TEntity>()
            where TEntity : class
        {
            return factory.ModifyFilter<TEntity>();
        }

        public IQueryable<TEntity> Filter<TEntity>()
            where TEntity : class
        {
            return factory.Filter<TEntity>();
        }

        internal IQueryable ModifyFilter()
        {
            return factory.ModifyFilter();
        }

        internal IQueryable Filter()
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
        private readonly Func<IQueryable> modifyFilter;
        private readonly Func<IQueryable> filter;

        internal FilterFactory(ISecureQueryProvider db,
            Func<IQueryable> modifyFilter,
            Func<IQueryable> filter)
        {
            this.db = db;
            this.modifyFilter = modifyFilter;
            this.filter = filter;
        }

        public IQueryable ModifyFilter()
        {
            return modifyFilter();
        }

        public IQueryable Filter()
        {
            return filter();
        }

        public IQueryable<T> ModifyFilter<T>()
            where T: class
        {
            var feqc = db.Set<T>();
            var eh = db.GetEntityEvents<T>();
            if (eh == null)
            {
                throw new EntityAccessException($"Access to {typeof(T).Name} denied");
            }
            return eh.ModifyFilter(feqc);
        }

        public IQueryable<T> Filter<T>()
            where T : class
        {
            var feqc = db.Set<T>();
            var eh = db.GetEntityEvents<T>();
            if (eh == null)
            {
                throw new EntityAccessException($"Access to {typeof(T).Name} denied");
            }
            return eh.Filter(feqc);
        }

        public IQueryable<T> Set<T>()
            where T: class
        {
            return db.Set<T>();
        }
    }
}
