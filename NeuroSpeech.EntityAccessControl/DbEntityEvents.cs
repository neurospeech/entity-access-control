using NeuroSpeech.EntityAccessControl.Internal;
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

    public class DbEntityEvents<T> : IEntityEvents
    {
        public bool EnforceSecurity { get; set; }

        public EntityAccessException NewEntityAccessException(string title)
        {
            return new EntityAccessException(new ErrorModel { Title = title });
        }

        private static object lockObject = new object();
        private static List<JsonIgnoreProperty>? ignoreList;
        private static Dictionary<PropertyInfo, JsonIgnoreCondition>? ignoreConditions;

        public List<JsonIgnoreProperty> GetIgnoreConditions()
        {
            lock(lockObject)
            {
                if (ignoreList == null)
                {
                    ignoreConditions = new Dictionary<PropertyInfo, JsonIgnoreCondition>();
                    OnSetupIgnore();
                    ignoreList = ignoreConditions
                        .Select((x) => new JsonIgnoreProperty(x.Key, x.Value)).ToList();
                    ignoreConditions = null;
                }
            }
            return ignoreList;
        }

        protected virtual void OnSetupIgnore()
        {

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


        public virtual IQueryContext<T> Filter(IQueryContext<T> q)
        {
            if (EnforceSecurity)
                throw new EntityAccessException($"No security rule defined for {typeof(T).Name}");
            return q;
        }

        public virtual IQueryContext<T> IncludeFilter(IQueryContext<T> q)
        {
            return Filter(q);
        }

        IQueryContext IEntityEvents.Filter(IQueryContext q)
        {
            return Filter((IQueryContext<T>)q);
        }

        IQueryContext IEntityEvents.IncludeFilter(IQueryContext q)
        {
            return IncludeFilter((IQueryContext<T>)q);
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
