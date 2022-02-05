using NeuroSpeech.EntityAccessControl;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NeuroSpeech.EntityAccessControl.Internal;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json.Serialization;

namespace NeuroSpeech.EntityAccessControl.Security
{

    public abstract class BaseSecureRepository<TDbContext, TC>: ISecureRepository
        where TDbContext: DbContext
    {
        private readonly TDbContext db;
        public readonly TC AssociatedUser;
        private readonly BaseSecurityRules<TC> rules;

        public abstract bool SecurityDisabled {  get; }

        public IModel Model => db.Model;

        public Task<TransactionExtensions.AsyncTransaction> TransactionAsync() => db.TransactionAsync();

        public BaseSecureRepository(
            TDbContext db, 
            TC client,
            BaseSecurityRules<TC> rules)
        {
            this.db = db;
            this.AssociatedUser = client;
            this.rules = rules;
        }

        public IQueryable<T?> Query<T>()
            where T: class
        {
            if(SecurityDisabled)
                return db.Set<T>();
            return rules.Apply<T>(new QueryContext<T>(this, db.Set<T>(), new ErrorModel()), AssociatedUser).ToQuery();
        }

        public IQueryContext<T?> Apply<T>(IQueryContext<T> q)
            where T: class
        {
            if (SecurityDisabled)
                return q;
            return rules.Apply<T>(q, AssociatedUser)!;
        }

        public IQueryable<T> FromSqlRaw<T>(string sql, params object[] parameters)
            where T: class
        {
            var q = db.Set<T>().FromSqlRaw(sql, parameters);
            if (SecurityDisabled)
            {
                return q;
            }
            return rules.Apply<T>(new QueryContext<T>(this, q, new ErrorModel()), AssociatedUser).ToQuery();
        }

        public JsonIgnoreCondition GetIgnoreCondition(PropertyInfo property)
        {
            if(SecurityDisabled)
            {
                return JsonIgnoreCondition.Never;
            }
            return rules.GetIgnoreCondition(property);
        }

        public void Remove(object entity) => db.Remove(entity);

        public void Attach(object entity) => db.Attach(entity);

        public void Add(object entity) => db.Add(entity);

        public EntityEntry Entry(object entity) => db.Entry(entity);

        public Task<object?> FindByKeysAsync(IEntityType t, JsonElement keys, CancellationToken token = default)
        {
            var method = this.GetInstanceGenericMethod(nameof(FindByKeysGenericAsync), t.ClrType)
                .As<Task<object?>>();
            return method.Invoke(t, keys, token);
        }

        private static Task<object?> nullResult = Task.FromResult<object?>(null);

        public Task<object?> FindByKeysGenericAsync<T>(IEntityType t, JsonElement keys, CancellationToken token = default)
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
                var value = v.DeserializeJsonElement(propertyType);
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
            var q = Query<T>().Where(lambda);
            return q.FirstOrDefaultAsync(token).ContinueAsObject();
        }

        public async Task<int> SaveChangesAsync(CancellationToken token = default) {
            // return db.SaveChangesAsync(token);
            if (SecurityDisabled)
            {
                return await db.SaveChangesAsync();
            }
            await using var tx = await db.Database.BeginTransactionAsync(token);
            var changes = db.ChangeTracker.Entries().ToList();

            var pendingVerifications = new List<PreservedState>();

            // verify delete
            foreach (var entry in changes)
            {
                switch (entry.State)
                {
                    case EntityState.Modified:
                        foreach (var member in entry.Members.Where(x => x.IsModified))
                        {
                            VerifyMemberModify(entry.Entity, member.Metadata.PropertyInfo);
                        }
                        await VerifyAccessAsync(entry);
                        pendingVerifications.Add(entry);
                        break;
                    case EntityState.Deleted:
                        await VerifyAccessAsync(entry);
                        // pendingVerifications.Add(entry);
                        break;
                    case EntityState.Added:
                        pendingVerifications.Add(entry);
                        break;
                }
            }

            int r = await db.SaveChangesAsync();
            foreach (var entry in pendingVerifications)
            {
                await VerifyAccessAsync(entry);
            }

            await tx.CommitAsync();
            return r;
        }

        private void VerifyMemberModify(object entity, PropertyInfo propertyInfo)
        {
            this.GetInstanceGenericMethod(nameof(InternalVerifyMemberModify), entity.GetType())
                .As<bool>()
                .Invoke(propertyInfo);
        }

        public bool InternalVerifyMemberModify<T>(PropertyInfo propertyInfo)
        {
            // rules.VerifyModifyMember<T>(propertyInfo, AssociatedUser);
            return true;
        }

        public readonly struct PreservedState
        {
            public readonly EntityState State;
            public readonly object Entity;

            public static implicit operator PreservedState(EntityEntry entry)
                => new PreservedState(entry.State, entry.Entity);

            public PreservedState(EntityState state, object entry)
            {
                this.State = state;
                this.Entity = entry;
            }
        }

        private Task VerifyAccessAsync(PreservedState entity)
        {
            return this.GetInstanceGenericMethod(nameof(VerifyAccessGenericAsync), entity.Entity.GetType())
                .As<Task>()
                .Invoke(entity);
            //return (this.GetType()
            //    .GetMethod(nameof(VerifyAccessGenericAsync))!
            //    .MakeGenericMethod(entity.Entity.GetType())
            //    .Invoke(this, new object[] { entity }) as Task)!;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public async Task VerifyAccessGenericAsync<T>(PreservedState entry)
            where T : class
        {
            var type = typeof(T);
            var tx = Expression.Parameter(type, "x");
            var t = db.Model.GetEntityTypes().FirstOrDefault(x => x.ClrType == type);
            Expression? start = null;
            var entity = entry.Entity;
            var entityConstant = Expression.Constant(entity);
            var pk = t.FindPrimaryKey();
            if (pk == null)
            {
                throw new EntityAccessException("Cannot modify entity without primary key");
            }
            foreach (var p in pk.Properties)
            {
                var equals = Expression.Equal(
                    Expression.Property(tx, p.PropertyInfo), 
                    Expression.Property(entityConstant, p.PropertyInfo));
                if (start == null)
                {
                    start = equals;
                    continue;
                }
                start = Expression.AndAlso(start, equals);
            }
            var lambda = Expression.Lambda<Func<T, bool>>(start, tx);
            var q = Query<T>().Where(lambda);
            if (!await q.AnyAsync())
                throw new EntityAccessException($"Access denied");
        }

    }

}
