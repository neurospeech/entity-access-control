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
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class SkipVerificationAttribute: Attribute
    {
    }

    internal class VerificationContext<TContext>
        where TContext : BaseDbContext<TContext>
    {
        private readonly ISecureQueryProvider db;
        private readonly DbContextEvents<TContext> events;
        private readonly IServiceProvider services;
        private List<Expression> selectExpressions = new List<Expression>();

        private Dictionary<(Type, PropertyInfo, object), bool> queryCache = new Dictionary<(Type, PropertyInfo, object), bool>();

        private static Expression emptyString = Expression.Constant("");

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
                if (start == null)
                {
                    start = newExp;
                    continue;
                }
                start = Expression.Call(null, concatMethod, start, newExp);
            }
            var pt = Expression.Parameter(typeof(T), "x");
            var lambda = Expression.Lambda<Func<T, string>>(start, pt);
            var query = q.Select(lambda);
            var text = query.ToQueryString();
#if DEBUG                
            System.Diagnostics.Debug.WriteLine("--------------------------------------------------------------------");
            System.Diagnostics.Debug.WriteLine(text);
            System.Diagnostics.Debug.WriteLine("--------------------------------------------------------------------");
            System.Diagnostics.Debug.WriteLine("");
            System.Diagnostics.Debug.WriteLine("");
#endif
            var result = await query.FirstOrDefaultAsync();
            if (String.IsNullOrWhiteSpace(result))
                return;
            var error = new ErrorModel();
            error.Title = result;
            error.Add("Query", text);
            throw new EntityAccessException(error);
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
           IQueryContext<T> qec,
           FilterContext? fc = null)
           where T : class
        {
            var type = typeof(T);
            var eh = events.GetEvents(services, type);
            if (eh == null)
            {
                throw new EntityAccessException($"Access denied to {type.FullName}");
            }
            if (fc != null)
            {
                return (IQueryContext<T>)eh.ReferenceFilter(qec, fc);
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
            var pType = typeof(T);
            // verify access to this entity first...
            if (e.State != EntityState.Added)
            {
                var keys = new List<(PropertyInfo, object)>();
                foreach(var property in e.Properties)
                {
                    if (property.Metadata.PropertyInfo.GetCustomAttribute<SkipVerificationAttribute>() != null)
                        continue;
                    if (property.Metadata.IsKey())
                    {
                        if(property.IsTemporary)
                        {
                            continue;
                        }
                        keys.Add((property.Metadata.PropertyInfo, property.CurrentValue));
                    }
                }
                if (keys.Count > 0)
                {
                    this.QueueEntityKeyForeignKey<T>(e, keys);
                }
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
                    if (px.Metadata.PropertyInfo.GetCustomAttribute<SkipVerificationAttribute>() != null)
                        continue;
                    if (px.IsTemporary)
                        continue;
                    if (px.CurrentValue == null)
                    {
                        continue;
                    }
                    if (px.OriginalValue != null && px.OriginalValue.Equals(px.CurrentValue))
                    {
                        if (e.State != EntityState.Added)
                        {
                            continue;
                        }
                    }
                    var pKey = principalKey.Properties[0];
                    keys.Add((pKey.PropertyInfo, px.CurrentValue));
                }

                if (keys.Count > 0)
                {
                    var fc = new FilterContext(e, nav);

                    this.GetInstanceGenericMethod(nameof(QueueEntityKeyForeignKey), principalType)
                        .As<int>()
                        .Invoke(e, keys, fc);
                }
            }
            return 0;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public int QueueEntityKeyForeignKey<T>(
            EntityEntry e,
            List<(PropertyInfo,object)> keys,
            FilterContext? fc = null)
            where T: class
        {
            Type type = typeof(T);
            var typeName = type.Name;
            Expression? body = null;
            var pe = Expression.Parameter(type, "x");
            bool cached = true;
            foreach (var (property, value) in keys)
            {
                var cacheKey = (type, property, value);
                if (!queryCache.ContainsKey(cacheKey))
                {
                    cached = false;
                    queryCache[cacheKey] = true;
                }
                Expression<Func<object>> closure = () => value;
                var exp = Expression.Equal(Expression.Property(pe, property), Expression.Convert(closure.Body, property.PropertyType));
                if (body == null)
                {
                    body = exp;
                    continue;
                }
                body = Expression.AndAlso(body, exp);
            }

            if (cached)
            {
                return 0;
            }

            var qc = new QueryContext<T>(db, db.Set<T>());
            var qc1 = ApplyFilter<T>(e.State, qc, fc);
            if (qc == qc1)
            {
#if DEBUG                
                System.Diagnostics.Debug.WriteLine($"No active rule for {type.Name}");
                System.Diagnostics.Debug.WriteLine("");
#endif
                return 0;
            }
            var q = qc1.ToQuery();
            Expression<Func<T, bool>> filter = Expression.Lambda<Func<T,bool>>(body, pe);
            q = q.Where(filter);
            var isSameType = e.Entity.GetType() == type;
            var state = ToState(e.State);
            var error = isSameType
                ? $"Cannot {state} type {typeName}. "
                : $"Cannot {state} type {e.Entity.GetType().Name} without access to type {typeName}. ";
            this.AddErrorExpression<T>(q.Expression, error);
            return 0;
        }

        private string ToState(EntityState state)
        {
            switch(state)
            {
                case EntityState.Added:
                    return "add";
                case EntityState.Modified:
                    return "update";
                case EntityState.Deleted:
                    return "delete";
            }
            return "";
        }

        private void AddErrorExpression<T>(Expression expression, string error)
        {
            if (firstSet == null)
            {
                firstSet = typeof(T);
            }
            Expression<Func<string>> errorExp = () => error;
            selectExpressions.Add(Expression.Condition(
                Expression.Call(null, anyMethod.MakeGenericMethod(typeof(T)), expression),
                emptyString,
                errorExp.Body
                ));

        }
    }
}
