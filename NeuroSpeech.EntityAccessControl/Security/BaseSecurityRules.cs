using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Serialization;

namespace NeuroSpeech.EntityAccessControl
{

    public abstract class BaseSecurityRules<TC>
    {
        private RulesDictionary select = new RulesDictionary();
        private RulesDictionary insert = new RulesDictionary();
        private RulesDictionary update = new RulesDictionary();
        private RulesDictionary delete = new RulesDictionary();
        private RulesDictionary modify = new RulesDictionary();

        private Dictionary<PropertyInfo, JsonIgnoreCondition> ignoreConditions
            = new Dictionary<PropertyInfo, JsonIgnoreCondition>();

        internal IQueryContext<T> Apply<T>(IQueryContext<T> ts, TC client) where T : class
        {
            return select.As<T, TC>()(ts, client);
        }

        internal JsonIgnoreCondition GetIgnoreCondition(PropertyInfo property)
        {
            if (!ignoreConditions.TryGetValue(property, out var v))
            {
                v = property.GetCustomAttribute<JsonIgnoreAttribute>()?.Condition ?? JsonIgnoreCondition.Never;
                ignoreConditions[property] = v;
            }
            return v;
        }

        internal IQueryable<T> ApplyInsert<T>(IQueryContext<T> q, TC client) where T : class
        {
            return insert.As<T, TC>()(q, client).ToQuery();
        }

        internal IQueryable<T> ApplyDelete<T>(IQueryContext<T> q, TC client) where T : class
        {
            return delete.As<T, TC>()(q, client).ToQuery();
        }

        internal IQueryable<T> ApplyUpdate<T>(IQueryContext<T> q, TC client) where T : class
        {
            return update.As<T,TC>()(q, client).ToQuery();
        }

        /**
         * This will disable Json serialization for given property
         */
        public void Ignore<T>(Expression<Func<T,object>> expression,
            JsonIgnoreCondition condition = JsonIgnoreCondition.Always)
        {
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
            if(me.Member is not PropertyInfo property)
                throw new ArgumentException($"Expression {expression} is not a valid property expression");
            ignoreConditions[property] = condition;
        }


        /// <summary>
        /// Set filter for select, insert, update, delete
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="select"></param>
        /// <param name="insert"></param>
        /// <param name="update"></param>
        /// <param name="delete"></param>
        protected void SetFilters<T>(
            Func<IQueryContext<T>, TC, IQueryContext<T>>? select = null,
            Func<IQueryContext<T>, TC, IQueryContext<T>>? insert = null,
            Func<IQueryContext<T>, TC, IQueryContext<T>>? update = null,
            Func<IQueryContext<T>, TC, IQueryContext<T>>? delete = null)
        {
            if (select != null)
                this.select.SetFunc<T, TC>(select);
            if (insert != null)
                this.insert.SetFunc<T, TC>(insert);
            if (update != null)
                this.update.SetFunc<T, TC>(update);
            if (delete != null)
                this.delete.SetFunc<T, TC>(delete);
        }

        /// <summary>
        /// Set one filter for all (select, insert, update, delete)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="all"></param>
        public void SetAllFilters<T>(
            Func<IQueryContext<T>, TC, IQueryContext<T>> all)
        {
            SetFilters<T>(all, all, all, all);
        }


        public static IQueryContext<T> Allow<T>(IQueryContext<T> q, TC c) => q;

        public static IQueryContext<T> Unauthorized<T>(IQueryContext<T> q, TC c)
                   where T : class
                   => throw new UnauthorizedAccessException();

        internal void VerifyModifyMember<T>(PropertyInfo propertyInfo, TC c)
        {
            throw new NotImplementedException();
        }
    }
}
