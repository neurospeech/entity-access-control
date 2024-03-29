﻿using NeuroSpeech.EntityAccessControl.Internal;
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
using System.ComponentModel.DataAnnotations.Schema;

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

        private List<(int priority,Func<Task> task)>? PostSaveChangesQueue;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.RegisterCompositeKeys();

            modelBuilder.HasDbFunction(CastAs.StringMethod)
                .HasTranslation(a =>
                {
                    var p = a.ElementAt(0);
                    if (p is SqlUnaryExpression u)
                        return new SqlUnaryExpression(System.Linq.Expressions.ExpressionType.Convert,
                            u.Operand, typeof(string), new IntTypeMapping("nvarchar(50)", System.Data.DbType.String));
                    return new SqlUnaryExpression(System.Linq.Expressions.ExpressionType.Convert, p, typeof(string), p.TypeMapping);
                });

            modelBuilder.HasDbFunction(CastAs.DoubleFromFloatNullableMethod)
                .HasTranslation(a => {
                    var p = a.ElementAt(0);
                    if (p is SqlUnaryExpression u)
                        return new SqlUnaryExpression(System.Linq.Expressions.ExpressionType.Convert,
                            u.Operand, typeof(double), new IntTypeMapping("real", System.Data.DbType.Double));
                    return new SqlUnaryExpression(System.Linq.Expressions.ExpressionType.Convert, p, typeof(double), p.TypeMapping);
                });

            modelBuilder.HasDbFunction(CastAs.DoubleFromFloatMethod)
                .HasTranslation(a => {
                    var p = a.ElementAt(0);
                    if (p is SqlUnaryExpression u)
                        return new SqlUnaryExpression(System.Linq.Expressions.ExpressionType.Convert,
                            u.Operand, typeof(double), new IntTypeMapping("real", System.Data.DbType.Double));
                    return new SqlUnaryExpression(System.Linq.Expressions.ExpressionType.Convert, p, typeof(double), p.TypeMapping);
                });

            CastAs.Register(modelBuilder);

            Sql.Register(modelBuilder);

            modelBuilder.HasDbFunction(this.GetType().GetMethod(nameof(DateRangeView), new Type[] {
                typeof(DateTime),
                typeof(DateTime),
                typeof(string)
            })!);

            modelBuilder.HasDbFunction(this.GetType().GetMethod(nameof(JsonIDs), new Type[] {
                typeof(string)
            })!);
            modelBuilder.HasDbFunction(this.GetType().GetMethod(nameof(JsonStringArray), new Type[] {
                typeof(string)
            })!);
        }

        public IQueryable<JsonLongValue> JsonIDs(string json)
        {
            return FromExpression(() => JsonIDs(json));
        }

        public IQueryable<JsonStringValue> JsonStringArray(string json)
        {
            return FromExpression(() => JsonStringArray(json));
        }

        public IQueryable<DateRange> DateRangeView(DateTime start, DateTime end, string step = "Day")
        {
            return FromExpression(() => DateRangeView(start, end, step));
        }

        public bool RaiseEvents { get; set; }

        public bool EnforceSecurity { get; set; }

        public virtual string TypeCacheKey => "Global";

        IQueryable<T> ISecureQueryProvider.Set<T>() => new SecureQueryable<T>(this, this.Set<T>());

        public IQueryable<T> FilteredQuery<T>()
            where T: class
        {
            var q = Set<T>();
            var eh = events.GetEvents<T>(services);
            if (eh == null)
            {
                throw new EntityAccessException($"No security rule defined for entity {typeof(T).Name}");
            }
            eh.EnforceSecurity = EnforceSecurity;
            var fq = eh.Filter(q);
            // fq is already filtered...
            fq.Expression.DoNotVisit();
            return new SecureQueryable<T>(this, fq);
        }

        public IEntityEvents? GetEntityEvents(Type type)
        {
            var eh = events.GetEvents(services, type);
            if (eh != null)
            {
                eh.EnforceSecurity = this.EnforceSecurity;
            }
            return eh;
        }

        public IEntityEvents<T>? GetEntityEvents<T>() where T : class
        {
            var eh = events.GetEvents<T>(services);
            if (eh != null)
            {
                eh.EnforceSecurity = this.EnforceSecurity;
            }
            return eh;
        }

        private Task OnInsertingAsync(Type type, object entity)
        {
            var eh = events.GetEvents(services, type);
            if (eh != null)
            {
                eh.EnforceSecurity = EnforceSecurity;
                return eh.InsertingAsync(entity);
            }
            return Task.CompletedTask;
        }

        private Task OnInsertedAsync(Type type, object entity)
        {
            var eh = events.GetEvents(services, type);
            if (eh != null)
            {
                eh.EnforceSecurity = EnforceSecurity;
                return eh.InsertedAsync(entity);
            }
            return Task.CompletedTask;
        }

        private Task OnUpdatingAsync(Type type, object entity)
        {
            var eh = events.GetEvents(services, type);
            if (eh != null)
            {
                eh.EnforceSecurity = EnforceSecurity;
                return eh.UpdatingAsync(entity);
            }
            return Task.CompletedTask;
        }

        private Task OnUpdatedAsync(Type type, object entity)
        {
            var eh = events.GetEvents(services, type);
            if (eh != null)
            {
                eh.EnforceSecurity = EnforceSecurity;
                return eh.UpdatedAsync(entity);
            }
            return Task.CompletedTask;
        }


        private Task OnDeletingAsync(Type type, object entity)
        {
            var eh = events.GetEvents(services, type);
            if (eh != null)
            {
                eh.EnforceSecurity = EnforceSecurity;
                return eh.DeletingAsync(entity);
            }
            return Task.CompletedTask;
        }

        private Task OnDeletedAsync(Type type, object entity)
        {
            var eh = events.GetEvents(services, type);
            if (eh != null)
            {
                eh.EnforceSecurity = EnforceSecurity;
                return eh.DeletedAsync(entity);
            }
            return Task.CompletedTask;
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

        private IEnumerable<EntityEntry> Entries()
        {
            var all = this.ChangeTracker.Entries().ToList();
            var newChanges = new List<EntityEntry>();            
            EventHandler<EntityTrackedEventArgs> tracker = (s, e) => {
                if (!all.Contains(e.Entry))
                    newChanges.Add(e.Entry);
            };
            this.ChangeTracker.Tracked += tracker;
            do
            {
                foreach (var item in all)
                {
                    yield return item;
                }

                // check if anything was modified...
                all.Clear();
                all.AddRange(newChanges);
                newChanges.Clear();
            } while (all.Count > 0);
            this.ChangeTracker.Tracked -= tracker;
        }

        private bool isChanging = false;
        private bool queueChanges = false;

        /// <summary>
        /// Save changes will validate object after executing all relevant events, so event handlers can set default properties before they are validated.
        /// </summary>
        /// <param name="acceptAllChangesOnSuccess"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            if(isChanging)
            {
                if (!RaiseEvents)
                {
                    this.QueuePostSaveTask(() => this.SaveChangesWithoutEventsAsync(cancellationToken));
                    return 0;
                }
                this.QueuePostSaveTask(() => this.SaveChangesAsync(cancellationToken));
                return 0;
            }

            isChanging = true;

            try
            {

                if (!RaiseEvents)
                {
                    Validate();
                    return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
                }

                var vc = new VerificationContext<TContext>(this, events, services);

                var pending = new List<(EntityState State, object item, Type type)>();
                var errors = new List<ValidationResult>();
                foreach (var e in Entries())
                {
                    var item = e.Entity;
                    var type = item.GetType();
                    switch (e.State)
                    {
                        case EntityState.Added:
                            pending.Add((e.State, item, type));
                            await OnInsertingAsync(type, item);
                            Validator.TryValidateObject(item, new ValidationContext(item), errors);
                            vc.QueueVerification(e);
                            break;
                        case EntityState.Modified:
                            pending.Add((e.State, item, type));
                            await OnUpdatingAsync(type, item);
                            Validator.TryValidateObject(item, new ValidationContext(item), errors);
                            vc.QueueVerification(e);
                            break;
                        case EntityState.Deleted:
                            pending.Add((e.State, item, type));
                            await OnDeletingAsync(type, item);
                            Validator.TryValidateObject(item, new ValidationContext(item), errors);
                            vc.QueueVerification(e);
                            break;
                    }
                }
                if (errors.Any())
                {
                    throw new AppValidationException(new AppValidationErrors
                    {
                        Message = $"Invalid model {string.Join(", ", errors.Select(x => x.ErrorMessage + ": " + string.Join(", ", x.MemberNames)))}",
                        Errors = errors.Select(x => new AppValidationError
                        {
                            Name = string.Join(", ", x.MemberNames),
                            Error = x.ErrorMessage!
                        })
                    });
                }
                await vc.VerifyAsync();
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

                isChanging = false;

                var postSaveChanges = this.PostSaveChangesQueue;
                this.PostSaveChangesQueue = null;
                if (postSaveChanges?.Count > 0)
                {
                    foreach (var (priority, change) in postSaveChanges.OrderBy((x) => x.priority))
                    {
                        await change();
                    }
                }
                return r;
            } finally
            {
                isChanging = false;
            }
        }

        public void QueuePostSaveTask(Func<Task> task, int priority = int.MaxValue)
        {
            this.PostSaveChangesQueue ??= new List<(int priority, Func<Task> task)>();
            this.PostSaveChangesQueue.Add((priority, task));
        }

        public async Task SaveChangesWithoutEventsAsync(CancellationToken cancellationToken = default)
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

        // IQueryable<T> ISecureQueryProvider.Query<T>()
        //{
        //    return FilteredQuery<T>();
        //}

        private readonly Dictionary<PropertyInfo, IEntityEvents> cached
            = new();

        private static List<PropertyInfo> Empty = new ();

        List<PropertyInfo> ISecureQueryProvider.GetIgnoredProperties(Type type)
        {
            var eh = events.GetEvents(services, type);
            if (eh == null)
                return Empty;
            return eh.GetIgnoreConditions(TypeCacheKey);
        }

        private static ConcurrentDictionary<(string key,Type type), List<PropertyInfo>> readOnlyProperties = new();

        List<PropertyInfo> ISecureQueryProvider.GetReadonlyProperties(Type type)
        {
            var key = (TypeCacheKey, type);
            return readOnlyProperties.GetOrAdd(key, (x) =>
            {
                List<PropertyInfo>? all = null;
                var et = this.Model.FindEntityType(type);
                if (et != null)
                {
                    foreach(var p in et.GetProperties()
                        .Where(x => x.PropertyInfo
                            ?.GetCustomAttribute<DatabaseGeneratedAttribute>()?.DatabaseGeneratedOption == DatabaseGeneratedOption.Computed))
                    {
                        all ??= new List<PropertyInfo>();
                        all.Add(p.PropertyInfo);
                    }

                    foreach(var p in et.ClrType.GetProperties().Where(x => !x.CanWrite))
                    {
                        all ??= new List<PropertyInfo>();
                        all.Add(p);
                    }
                }

                var eh = events.GetEvents(services, x.type);
                if (eh == null)
                    return all ?? Empty;
                all ??= new List<PropertyInfo>();
                all.AddRange(eh.GetReadOnlyProperties(TypeCacheKey));
                return all;
            });
        }

        public IQueryable<T> Apply<T>(IQueryable<T> qec, bool asInclude = false, PropertyInfo? property = null)
            where T: class
        {
            return ApplyFilter<T>(EntityState.Unchanged, qec, asInclude, property);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public IQueryable<T> ApplyFilter<T>(
            EntityState state,
            IQueryable<T> qec,
            bool asInclude = false,
            PropertyInfo? propertyInfo = null)
            where T: class
        {
            var type = typeof(T);
            var eh = events.GetEvents<T>(services);
            if (eh == null)
            {
                throw new EntityAccessException($"Access denied to {type.FullName}");
            }
            if (asInclude)
                return eh.IncludeFilter(qec, propertyInfo);
            switch(state)
            {
                case EntityState.Modified:
                case EntityState.Added:
                    return eh.ModifyFilter(qec);
                case EntityState.Deleted:
                    return eh.DeleteFilter(qec);
            }
            return eh.Filter(qec);
        }

        Task ISecureQueryProvider.SaveChangesAsync(CancellationToken cancellationToken)
        {
            return SaveChangesAsync(cancellationToken);
        }

        Task<(object entity, bool exists)> ISecureQueryProvider.BuildOrLoadAsync(
            IEntityType entityType,
            JsonElement item,
            CancellationToken cancellation)
        {
            return this.InvokeAs(entityType.ClrType, InternalBuildOrLoadAsync<object>, entityType, item, cancellation);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public async Task<(object, bool)> InternalBuildOrLoadAsync<T>(
            IEntityType t,
            JsonElement keys,
            CancellationToken token)
            where T : class
        {
            var type = typeof(T);
            ParameterExpression? tx = null;// = Expression.Parameter(type, "x");
            Expression? start = null;
            var k = t.FindPrimaryKey();
            var copy = Activator.CreateInstance<T>();
            var copyConst = Expression.Constant(copy);
            bool hasAllKeys = true;
            foreach (var p in k.Properties)
            {
                if (!keys.TryGetPropertyCaseInsensitive(p.Name, out var v))
                {
                    hasAllKeys = false;
                    continue;
                }

                PropertyInfo property = p.PropertyInfo;
                Type propertyType = property.PropertyType;
                var value = property.SaveJsonOrValue(copy, v);
                // var value = v.DeserializeJsonElement(propertyType);
                // check if it is default...
                if (value == null || value.Equals(propertyType.GetDefaultForType()))
                {
                    hasAllKeys = false;
                    continue;
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
            if (!hasAllKeys)
            {
                return (copy, false);
            }
            var lambda = Expression.Lambda<Func<T?, bool>>(start, tx);
            var querySet = this.EnforceSecurity ? FilteredQuery<T>() : Set<T>();
            var q = querySet.Where(lambda);
            var result = await q.FirstOrDefaultAsync(token);
            return (result ?? copy, result != null);
        }

        private static readonly Task<object?> nullResult = Task.FromResult<object?>(null);

        Task<object?> ISecureQueryProvider.FindByKeysAsync(
            Microsoft.EntityFrameworkCore.Metadata.IEntityType t,
            JsonElement keys, CancellationToken cancellation)
        {
            return this.InvokeAs(t.ClrType, FindByKeysInternalAsync<object>, t, keys, cancellation);
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
