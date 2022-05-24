using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using NeuroSpeech.EntityAccessControl.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NeuroSpeech.EntityAccessControl
{
    internal class VerificationContext<TContext>
        where TContext : BaseDbContext<TContext>
    {
        private readonly ISecureQueryProvider db;
        private readonly DbContextEvents<TContext> events;
        private readonly IServiceProvider services;
        private List<Expression> selectExpressions;
        

        private static Expression emptyString = Expression.Constant("");

        private Task<string>? validationError = null;
        private Type? firstSet = null;

        private static MethodInfo concatMethod = 
            typeof(string).GetMethod(nameof(string.Concat),
                BindingFlags.Public | BindingFlags.Static, Type.DefaultBinder, new Type[] { typeof(string), typeof(string) }, null)!;

        private static MethodInfo anyMethod =
            typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .First((x) => x.Name == nameof(Enumerable.Any) && x.GetParameters().Length == 1);

        public VerificationContext(
            ISecureQueryProvider db,
            DbContextEvents<TContext> events,
            IServiceProvider services)
        {
            this.db = db;
            this.events = events;
            this.services = services;
            selectExpressions = new List<Expression>();
        }

        public Task VerifyAsync()
        {
            if (firstSet == null)
                return Task.CompletedTask;
            return this.GetInstanceGenericMethod(nameof(VerifyInternalAsync), firstSet)
                .As<Task>()
                .Invoke();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public async Task VerifyInternalAsync<T>()
            where T: class
        {
            var q = db.Set<T>();
            Expression? start = null;
            foreach (var newExp in selectExpressions)
            {
                //var newExp =
                //    Expression.Condition(
                //        Expression.Call(null, anyMethod.MakeGenericMethod(type), qt)
                //        , emptyString,
                //        error);
                // var newExp = error;
                if (start == null)
                {
                    start = newExp;
                    continue;
                }
                start = Expression.Call(null, concatMethod, start, newExp);
            }
            //foreach (var newExp in expressionList)
            //{
            //    if (start == null)
            //    {
            //        start = newExp;
            //        continue;
            //    }
            //    start = Expression.Call(null, concatMethod, start, newExp);
            //}
            var pt = Expression.Parameter(typeof(T), "x");
            var lambda = Expression.Lambda<Func<T, string>>(start, pt);
            var query = q.Select(lambda);
            var text = query.ToQueryString();
            System.Diagnostics.Debug.WriteLine(text);
            var result = await query.FirstOrDefaultAsync();
            if (String.IsNullOrWhiteSpace(result))
                return;
            throw new EntityAccessException(result);
        }

        public void QueueVerification(EntityEntry entry)
        {
            if (!db.EnforceSecurity) {
                return;
            }
            this.GetInstanceGenericMethod(nameof(QueueEntry), entry.Entity.GetType())
                   .As<int>()
                   .Invoke(entry);
        }

        private IQueryContext<T> ApplyFilter<T>(
           EntityState state,
           IQueryContext<T> qec)
           where T : class
        {
            var type = typeof(T);
            var eh = events.GetEvents(services, type);
            if (eh == null)
            {
                throw new EntityAccessException($"Access denied to {type.FullName}");
            }
            switch (state)
            {
                case EntityState.Modified:
                case EntityState.Added:
                    return (IQueryContext<T>)eh.ModifyFilter(qec);
                case EntityState.Deleted:
                    return (IQueryContext<T>)eh.DeleteFilter(qec);
            }
            return (IQueryContext<T>)eh.Filter(qec);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public int QueueEntry<T>(EntityEntry e)
            where T: class
        {
            // verify access to this entity first...
            if (e.State != EntityState.Added)
            {
                if (firstSet == null)
                {
                    firstSet = typeof(T);
                }
                var qc = new QueryContext<T>(db, db.Set<T>());
                var qc1 = ApplyFilter<T>(e.State, qc);
                if (qc == qc1)
                {
                    return 0;
                }
                var q = qc1.ToQuery();
                var typeName = typeof(T).Name;
                var error = "Cannot access type " + typeName;
                Expression<Func<string>> errorExp = () => q.Any() ? "" : error;
                this.selectExpressions.Add(errorExp.Body);
            }

            if (e.State == EntityState.Deleted)
            {
                return 0;
            }

            // verify access to each foreign key
            // in case of insert and update
            var metdata = e.Metadata;
            var properties = metdata.GetDeclaredProperties();

            foreach (var re in e.References)
            {
                if (re.Metadata.IsCollection)
                    continue;
                if (re.Metadata is not INavigation nav)
                    continue;

                // verify current value...
                var principalType = nav.ForeignKey.PrincipalEntityType.ClrType;
                var principalKey = nav.ForeignKey.PrincipalKey;

                List<(PropertyInfo, object)> keys = new List<(PropertyInfo, object)>();

                foreach (var p in nav.ForeignKey.Properties)
                {
                    if (!properties.Contains(p))
                        continue;
                    var px = e.Property(p.Name);
                    if (px.IsTemporary)
                        continue;
                    if (px.CurrentValue == null)
                    {
                        continue;
                    }
                    if (px.OriginalValue != null && !px.OriginalValue.Equals(px.CurrentValue))
                    {
                        continue;
                    }
                    var pKey = principalKey.Properties[0];
                    keys.Add((pKey.PropertyInfo, px.CurrentValue));
                }

                if (keys.Count > 0)
                {
                    this.GetInstanceGenericMethod(nameof(QueueEntityKey), principalType)
                        .As<int>()
                        .Invoke(e, keys);
                }
            }
            return 0;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public int QueueEntityKey<T>(EntityEntry e, List<(PropertyInfo,object)> keys)
            where T: class
        {
            var qc = new QueryContext<T>(db, db.Set<T>());
            var qc1 = ApplyFilter<T>(e.State, qc);
            if (qc == qc1)
            {
                return 0;
            }
            var q = qc1.ToQuery();
            var typeName = typeof(T).Name;
            Expression? body = null;
            var pe = Expression.Parameter(typeof(T), "x");
            foreach(var (property,value) in keys)
            {
                Expression<Func<object>> closure = () => value;
                var exp = Expression.Equal(Expression.Property(pe, property), Expression.Convert(closure.Body, property.PropertyType));
                if(body==null)
                {
                    body = exp;
                    continue;
                }
                body = Expression.AndAlso(body, exp);
            }
            Expression<Func<T, bool>> filter = Expression.Lambda<Func<T,bool>>(body, pe);
            q = q.Where(filter);
            var error = "Cannot access type " + typeName;
            Expression<Func<string>> select = () => q.Any() ? "" : error;
            selectExpressions.Add(select.Body);
            return 0;
        }
    }
}
