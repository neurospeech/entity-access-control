using Microsoft.EntityFrameworkCore.ChangeTracking;
using NeuroSpeech.EntityAccessControl.Internal;
using NeuroSpeech.EntityAccessControl.Security;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NeuroSpeech.EntityAccessControl
{

    public delegate void IgnoreDelegate<T>(Expression<Func<T, object>> expression,
            JsonIgnoreCondition condition = JsonIgnoreCondition.Always);

    public class DbEntityEvents<T> : IEntityEvents<T>
        where T: class
    {
        public bool EnforceSecurity { get; set; }

        public EntityAccessException NewEntityAccessException(string title)
        {
            return new EntityAccessException(new ErrorModel { Title = title });
        }

        private Dictionary<PropertyInfo, JsonIgnoreCondition>? ignoreConditions;
        private List<PropertyInfo>? readOnlyProperties;

        public List<PropertyInfo> GetIgnoreConditions(string typeCacheKey)
        {
            ignoreConditions = new Dictionary<PropertyInfo, JsonIgnoreCondition>();
            OnSetupIgnore(typeCacheKey);
            return ignoreConditions
                .Where(x => x.Value == JsonIgnoreCondition.Always)
                .Select(x => x.Key).ToList();
        }

        public List<PropertyInfo> GetReadOnlyProperties(string typeCacheKey)
        {
            readOnlyProperties = new List<PropertyInfo>();
            OnSetupReadOnly(typeCacheKey);
            var r = readOnlyProperties;
            readOnlyProperties = null;
            return r;
        }

        protected virtual void OnSetupReadOnly(string typeCacheKey)
        {

        }

        protected virtual void OnSetupIgnore(string typeCacheKey)
        {

        }

        /**
        * This will setup specified properties as readonly, 
        */
        protected void SetReadOnly(Expression<Func<T, object>> expression)
        {
            if (readOnlyProperties == null)
                throw new InvalidOperationException($"SetReadOnly must only be called within OnSetupReadOnly method");
            if (expression.Body is NewExpression nme)
            {
                foreach (var m in nme.Arguments)
                {
                    if (m is not MemberExpression member)
                        throw new ArgumentException($"Invalid expression");

                    while (member.Expression is not ParameterExpression pe)
                    {
                        if (member.Expression is not MemberExpression me2)
                            throw new ArgumentException($"Invalid expression");
                        member = me2;
                    }
                    if (member.Member is not PropertyInfo property1)
                        throw new ArgumentException($"Should be a property");
                    readOnlyProperties.Add(property1);
                }
                return;
            }
            if (expression.Body is not MemberExpression me)
                throw new ArgumentException($"Expression {expression} is not a valid member expression");
            if (me.Member is not PropertyInfo property)
                throw new ArgumentException($"Expression {expression} is not a valid property expression");
            readOnlyProperties.Add(property);
        }

        /**
         * This will disable Json serialization for given property
         */
        protected void Ignore(Expression<Func<T, object>> expression,
            JsonIgnoreCondition condition = JsonIgnoreCondition.Always)
        {
            if (ignoreConditions == null)
                throw new InvalidOperationException($"Ignore must only be called within OnSetupIgnore method");
            if (expression.Body is NewExpression nme)
            {
                foreach (var m in nme.Arguments)
                {
                    if (m is not MemberExpression member)
                        throw new ArgumentException($"Invalid expression");

                    while (member.Expression is not ParameterExpression pe)
                    {
                        if (member.Expression is not MemberExpression me2)
                            throw new ArgumentException($"Invalid expression");
                        member = me2;
                    }
                    if (member.Member is not PropertyInfo property1)
                        throw new ArgumentException($"Should be a property");
                    ignoreConditions[property1] = condition;
                }
                return;
            }
            if (expression.Body is not MemberExpression me)
                throw new ArgumentException($"Expression {expression} is not a valid member expression");
            if (me.Member is not PropertyInfo property)
                throw new ArgumentException($"Expression {expression} is not a valid property expression");
            ignoreConditions[property] = condition;
        }


        public virtual IQueryable<T> Filter(IQueryable<T> q)
        {
            if (EnforceSecurity)
                throw new EntityAccessException($"No security rule defined for {typeof(T).Name}");
            return q;
        }

        public virtual IQueryable<T> ModifyFilter(IQueryable<T> q)
        {
            return Filter(q);
        }

        public virtual IQueryable<T> DeleteFilter(IQueryable<T> q)
        {
            return ModifyFilter(q);
        }

        public virtual IQueryable<T> IncludeFilter(IQueryable<T> q)
        {
            return Filter(q);
        }

        //public virtual IQueryContext<T> ReferenceFilter(IQueryContext<T> q, FilterContext fc)
        //{
        //    return ModifyFilter(q);
        //}

        //IQueryContext IEntityEvents.ReferenceFilter(IQueryContext q, FilterContext fc)
        //{
        //    return ReferenceFilter((IQueryContext<T>)q, fc);
        //}


        IQueryable IEntityEvents.Filter(IQueryable q)
        {
            return Filter((IQueryable<T>)q);
        }

        IQueryable IEntityEvents.IncludeFilter(IQueryable q)
        {
            return IncludeFilter((IQueryable<T>)q);
        }


        IQueryable IEntityEvents.ModifyFilter(IQueryable q)
        {
            return ModifyFilter((IQueryable<T>)q);
        }

        IQueryable IEntityEvents.DeleteFilter(IQueryable q)
        {
            return DeleteFilter((IQueryable<T>)q);
        }

        public virtual Task DeletedAsync(T entity)
        {
            return Task.CompletedTask;
        }

        Task IEntityEvents.DeletedAsync(object entity)
        {
            return DeletedAsync((T)entity);
        }

        public virtual Task DeletingAsync(T entity)
        {
            if (EnforceSecurity)
                throw new EntityAccessException($"Deleting Entity {typeof(T).FullName} denied");
            return Task.CompletedTask;
        }

        Task IEntityEvents.DeletingAsync(object entity)
        {
            return DeletingAsync((T)entity);
        }

        public virtual Task InsertedAsync(T entity)
        {
            return Task.CompletedTask;
        }

        Task IEntityEvents.InsertedAsync(object entity)
        {
            return InsertedAsync((T)entity);
        }

        public virtual Task InsertingAsync(T entity)
        {
            if (EnforceSecurity)
                throw new EntityAccessException($"Inserting Entity {typeof(T).FullName} denied");
            return Task.CompletedTask;
        }

        Task IEntityEvents.InsertingAsync(object entity)
        {
            return InsertingAsync((T)entity);
        }

        public virtual Task UpdatedAsync(T entity)
        {
            return Task.CompletedTask;
        }

        IQueryable? IEntityEvents.ForeignKeyFilter(EntityEntry entity, PropertyInfo key, object value, FilterFactory fs)
        {
            if(!EnforceSecurity)
            {
                return null;
            }
            return ForeignKeyFilter(new ForeignKeyInfo<T>(entity, key, value, fs));
        }

        protected virtual IQueryable? ForeignKeyFilter(ForeignKeyInfo<T> fk)
        {
            return fk.ModifyFilter();
        }

        Task IEntityEvents.UpdatedAsync(object entity)
        {
            return UpdatedAsync((T)entity);
        }

        public virtual Task UpdatingAsync(T entity)
        {
            if (EnforceSecurity)
                throw new EntityAccessException($"Updating Entity {typeof(T).FullName} denied");
            return Task.CompletedTask;
        }

        Task IEntityEvents.UpdatingAsync(object entity)
        {
            return UpdatingAsync((T)entity);
        }

    }
}
