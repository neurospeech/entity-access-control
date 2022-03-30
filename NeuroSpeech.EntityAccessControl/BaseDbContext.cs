using NeuroSpeech.EntityAccessControl.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Metadata;

namespace NeuroSpeech.EntityAccessControl
{

    public delegate Task OnEntityEvent<T, TEntity>(T context, TEntity entity);

    public class BaseDbContext<TContext> : DbContext, ISecureQueryProvider
        where TContext: BaseDbContext<TContext>
    {
        private readonly DbContextEvents<TContext> events;
        private readonly IServiceProvider services;

        public BaseDbContext(
            DbContextOptions<TContext> options,
            DbContextEvents<TContext> events,
            IServiceProvider services) : base(options)
        {
            this.events = events;
            this.services = services;
            this.RaiseEvents = events != null;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.HasDbFunction(CastAs.StringMethod)
                .HasTranslation(a =>
                {
                    var p = a.ElementAt(0);
                    if (p is SqlUnaryExpression u)
                        return new SqlUnaryExpression(System.Linq.Expressions.ExpressionType.Convert,
                            u.Operand, typeof(string), new IntTypeMapping("nvarchar(50)", System.Data.DbType.String));
                    return new SqlUnaryExpression(System.Linq.Expressions.ExpressionType.Convert, p, typeof(string), p.TypeMapping);
                });
        }

        public bool RaiseEvents { get; set; }

        public bool EnforceSecurity { get; set; }

        public IQueryable<T> FilteredQuery<T>()
            where T: class
        {
            var q = new QueryContext<T>(this, Set<T>());
            var eh = events.GetEvents(services, typeof(T));
            if (eh == null)
            {
                throw new EntityAccessException($"No security rule defined for entity {typeof(T).Name}");
            }
            eh.EnforceSecurity = EnforceSecurity;
            return ((IQueryContext<T>)eh.Filter(q)).ToQuery();
        }

        private async Task VerifyAccessAsync(Type type, EntityEntry e, object item, bool insert = false)
        {
            if (insert)
            {
                var metdata = e.Metadata;
                var properties = metdata.GetDeclaredProperties();
                foreach(var re in e.References)
                {
                    if (re.Metadata.IsCollection)
                        continue;
                    if(re.Metadata is not INavigation nav)
                        continue;
                    bool isModified = false;
                    foreach(var p in nav.ForeignKey.Properties)
                    {
                        if (!properties.Contains(p))
                            continue;
                        var px = e.Property(p.Name);
                        if (px.IsTemporary)
                            continue;
                        if (px.OriginalValue != px.CurrentValue)
                        {
                            isModified = true;
                            break;
                        }
                    }
                    if (!isModified)
                        continue;
                    await this.GetInstanceGenericMethod(nameof(VerifyFilterAsync), re.Metadata.TargetEntityType.ClrType)
                        .As<Task>()
                        .Invoke(re.Query(), e, item, true);
                }
                return;
            }
            await this.GetInstanceGenericMethod(nameof(VerifyFilterAsync), type)
                .As<Task>()
                .Invoke((IQueryable?)null, e, item, false);
        }

        public async Task VerifyFilterAsync<T>(IQueryable? query, EntityEntry e, object? item, bool insert)
            where T: class
        {
            var type = typeof(T);
            var q = query == null ? Set<T>() : (IQueryable<T>)query;
            if (!insert) {
                var pe = Expression.Parameter(type);
                var ce = Expression.Constant(item, type);
                var pKey = e.Metadata.FindPrimaryKey();
                Expression? body = null;
                foreach(var p in pKey.Properties)
                {
                    var equal = Expression.Equal(
                        Expression.Property(pe, p.PropertyInfo),
                        Expression.Property(ce, p.PropertyInfo));
                    if (body == null)
                    {
                        body = equal;
                        continue;
                    }
                    body = Expression.AndAlso(body, equal);
                }
                q = q.Where(Expression.Lambda<Func<T,bool>>(body, pe));
            }
            q = Apply<T>(new QueryContext<T>(this, q)).ToQuery();
            if (await q.AnyAsync())
                return;
            if (insert)
                throw new EntityAccessException($"Insert denied for {type.FullName}");
            throw new EntityAccessException($"Update/Delete denied for {type.FullName}");
        }

        private async Task OnInsertingAsync(Type type, object entity)
        {
            // call base class events first...
            var bt = type.BaseType;
            if (bt != null && bt != typeof(object))
            {
                await OnInsertingAsync(bt, entity);
            }
            var eh = events.GetEvents(services, type);
            if (eh != null)
            {
                eh.EnforceSecurity = EnforceSecurity;
                await eh.InsertingAsync(entity);
            }
        }

        private async Task OnInsertedAsync(Type type, object entity)
        {
            // call base class events first...
            var bt = type.BaseType;
            if (bt != null && bt != typeof(object))
            {
                await OnInsertedAsync(bt, entity);
            }
            var eh = events.GetEvents(services, type);
            if (eh != null)
            {
                eh.EnforceSecurity = EnforceSecurity;
                await eh.InsertedAsync(entity);
            }
        }

        private async Task OnUpdatingAsync(Type type, object entity)
        {
            // call base class events first...
            var bt = type.BaseType;
            if (bt != null && bt != typeof(object))
            {
                await OnUpdatingAsync(bt, entity);
            }
            var eh = events.GetEvents(services, type);
            if (eh != null)
            {
                eh.EnforceSecurity = EnforceSecurity;
                await eh.UpdatingAsync(entity);
            }
        }

        private async Task OnUpdatedAsync(Type type, object entity)
        {
            // call base class events first...
            var bt = type.BaseType;
            if (bt != null && bt != typeof(object))
            {
                await OnUpdatedAsync(bt, entity);
            }
            var eh = events.GetEvents(services, type);
            if (eh != null)
            {
                eh.EnforceSecurity = EnforceSecurity;
                await eh.UpdatedAsync(entity);
            }
        }


        private async Task OnDeletingAsync(Type type, object entity)
        {
            // call base class events first...
            var bt = type.BaseType;
            if (bt != null && bt != typeof(object))
            {
                await OnDeletingAsync(bt, entity);
            }
            var eh = events.GetEvents(services, type);
            if (eh != null)
            {
                eh.EnforceSecurity = EnforceSecurity;
                await eh.DeletingAsync(entity);
            }
        }

        private async Task OnDeletedAsync(Type type, object entity)
        {
            // call base class events first...
            var bt = type.BaseType;
            if (bt != null && bt != typeof(object))
            {
                await OnDeletedAsync(bt, entity);
            }
            var eh = events.GetEvents(services, type);
            if (eh != null)
            {
                eh.EnforceSecurity = EnforceSecurity;
                await eh.DeletedAsync(entity);
            }
        }

        protected virtual void Validate()
        {
            var entities = from e in ChangeTracker.Entries()
                           where e.State == EntityState.Added
                               || e.State == EntityState.Modified
                           select e.Entity;
            var errors = new List<ValidationResult>();
            foreach (var entity in entities)
            {
                var validationContext = new ValidationContext(entity);
                Validator.TryValidateObject(entity, validationContext, errors);
            }

            if (errors.Any())
            {
                throw new AppValidationException(new AppValidationErrors
                {
                    Message = $"Invalid model { string.Join(", ", errors.Select(x => x.ErrorMessage + ": " + string.Join(", ", x.MemberNames))) }",
                    Errors = errors.Select(x => new AppValidationError
                    {
                        Name = string.Join(", ", x.MemberNames),
                        Error = x.ErrorMessage!
                    })
                });
            }

        }

        /// <summary>
        /// Save changes will validate object after executing all relevant events, so event handlers can set default properties before they are validated.
        /// </summary>
        /// <param name="acceptAllChangesOnSuccess"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            // this.ChangeTracker.DetectChanges();
            if (!RaiseEvents)
            {
                Validate();
                return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            }
            var pending = new List<(EntityState State, object item, Type type)>();
            var errors = new List<ValidationResult>();
            foreach(var e in this.ChangeTracker.Entries())
            {
                var item = e.Entity;
                var type = item.GetType();
                switch (e.State)
                {
                    case EntityState.Added:
                        pending.Add((e.State, item, type));
                        await OnInsertingAsync(type, item);
                        Validator.TryValidateObject(item, new ValidationContext(item), errors);
                        if (EnforceSecurity)
                        {
                            await VerifyAccessAsync(type, e, item, true);
                        }
                        break;
                    case EntityState.Modified:
                        pending.Add((e.State, item, type));
                        await OnUpdatingAsync(type, item);
                        Validator.TryValidateObject(item, new ValidationContext(item), errors);
                        if (EnforceSecurity)
                        {
                            await VerifyAccessAsync(type, e, item);
                        }
                        break;
                    case EntityState.Deleted:
                        pending.Add((e.State, item, type));
                        await OnDeletingAsync(type, item);
                        Validator.TryValidateObject(item, new ValidationContext(item), errors);
                        if (EnforceSecurity)
                        {
                            await VerifyAccessAsync(type, e, item);
                        }
                        break;
                }
            }
            if (errors.Any())
            {
                throw new AppValidationException(new AppValidationErrors
                {
                    Message = $"Invalid model { string.Join(", ", errors.Select(x => x.ErrorMessage + ": " + string.Join(", ", x.MemberNames))) }",
                    Errors = errors.Select(x => new AppValidationError
                    {
                        Name = string.Join(", ", x.MemberNames),
                        Error = x.ErrorMessage!
                    })
                });
            }
            var r = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            foreach (var (state, item, type) in pending)
            {
                switch (state)
                {
                    case EntityState.Added:
                        await OnInsertedAsync(type, item);
                        break;
                    case EntityState.Modified:
                        await OnUpdatedAsync(type, item);
                        break;
                    case EntityState.Deleted:
                        await OnDeletedAsync(type, item);
                        break;
                }
            }
            return r;
        }

        public async Task SaveChangesWithoutEvents(CancellationToken cancellationToken = default)
        {
            try
            {
                this.RaiseEvents = false;
                await this.SaveChangesAsync(cancellationToken);
            }
            finally
            {
                this.RaiseEvents = true;
            }
        }

        IQueryable<T> ISecureQueryProvider.Query<T>()
        {
            return FilteredQuery<T>();
        }

        private readonly Dictionary<PropertyInfo, IEntityEvents> cached
            = new();

        JsonIgnoreCondition ISecureQueryProvider.GetIgnoreCondition(PropertyInfo property)
        {
            var eh =  events.GetEvents(services, property.DeclaringType);
            if (eh == null)
                return property.GetCustomAttribute<JsonIgnoreAttribute>()?.Condition ?? JsonIgnoreCondition.Never;
            return eh.GetIgnoreCondition(property);
        }

        public IQueryContext<T> Apply<T>(IQueryContext<T> qec)
            where T: class
        {
            var type = typeof(T);
            var baseType = type.BaseType;
            if (baseType != null && baseType != typeof(object))
            {
                qec = this.GetInstanceGenericMethod(
                    nameof(ApplyInternal), type, baseType).As<IQueryContext<T>>()
                    .Invoke(qec);
            }
            var eh = events.GetEvents(services, type);
            if (eh == null)
            {
                throw new EntityAccessException($"Access denied to {type.FullName}");
            }
            return (IQueryContext<T>)eh.Filter(qec);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public IQueryContext<RT> ApplyInternal<RT,BT>(IQueryContext<RT> q)
            where RT: class, BT
            where BT: class
        {
            return Apply(q.OfType<BT>()).OfType<RT>();
        }

        Task ISecureQueryProvider.SaveChangesAsync(CancellationToken cancellationToken)
        {
            return SaveChangesAsync(cancellationToken);
        }

        private static readonly Task<object?> nullResult = Task.FromResult<object?>(null);

        Task<object?> ISecureQueryProvider.FindByKeysAsync(
            Microsoft.EntityFrameworkCore.Metadata.IEntityType t,
            JsonElement keys, CancellationToken cancellation)
        {
            return this.GetInstanceGenericMethod(nameof(FindByKeysInternalAsync), t.ClrType)
                .As<Task<object?>>()
                .Invoke(t, keys, cancellation);
        }
        public Task<object?> FindByKeysInternalAsync<T>(
            Microsoft.EntityFrameworkCore.Metadata.IEntityType t,
            JsonElement keys, CancellationToken token)
            where T: class
        {
            var type = typeof(T);
            ParameterExpression? tx = null;// = Expression.Parameter(type, "x");
            Expression? start = null;
            var k = t.FindPrimaryKey();
            var copy = Activator.CreateInstance<T>();
            var copyConst = Expression.Constant(copy);
            foreach (var p in k.Properties)
            {
                if (!keys.TryGetPropertyCaseInsensitive(p.Name, out var v))
                {
                    return nullResult;
                }

                PropertyInfo property = p.PropertyInfo;
                Type propertyType = property.PropertyType;
                var value = property.SaveJsonOrValue(copy, v);
                // var value = v.DeserializeJsonElement(propertyType);
                // check if it is default...
                if (value == null || value.Equals(propertyType.GetDefaultForType()))
                {
                    return nullResult;
                }

                property.SetValue(copy, value);

                tx ??= Expression.Parameter(type, "x");
                var equals = Expression.Equal(
                    Expression.Property(tx, property),
                    Expression.Property(copyConst, property));
                if (start == null)
                {
                    start = equals;
                    continue;
                }
                start = Expression.AndAlso(start, equals);
            }
            var lambda = Expression.Lambda<Func<T?, bool>>(start, tx);
            var q = FilteredQuery<T>().Where(lambda);
            return q.FirstOrDefaultAsync(token).ContinueAsObject();

        }
        void ISecureQueryProvider.Remove(object entity)
        {
            Remove(entity);
        }

        void ISecureQueryProvider.Add(object entity)
        {
            Add(entity);
        }
    }
}
