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
            return rules.Apply<T>(db.Set<T>(), AssociatedUser);
        }

        public IQueryable<T> FromSqlRaw<T>(string sql, params object[] parameters)
            where T: class
        {
            var q = db.Set<T>().FromSqlRaw(sql, parameters);
            if (SecurityDisabled)
            {
                return q;
            }
            return rules.Apply<T>(q, AssociatedUser);
        }

        public void Remove(object entity) => db.Remove(entity);

        public void Attach(object entity) => db.Attach(entity);

        public void Add(object entity) => db.Add(entity);

        public EntityEntry Entry(object entity) => db.Entry(entity);

        public async Task<object?> FindByKeysAsync(Type t, JsonElement keys, CancellationToken token = default)
        {
            var r = this.GetType()
                .GetMethod(nameof(FindByKeysGenericAsync))!
                .MakeGenericMethod(t)
                .Invoke(this, new object?[] { keys, token}) as Task;
            await r!;
            return r.GetType().GetProperty("Result")!.GetValue(r);
        }

        public Task<T?> FindByKeysGenericAsync<T>(JsonElement keys, CancellationToken token = default)
            where T: class
        {
            var type = typeof(T);
            var tx = Expression.Parameter(type, "x");
            var t = db.Model.GetEntityTypes().FirstOrDefault(x => x.ClrType == type);
            Expression? start = null;
            foreach (var k in t.GetKeys())
            {
                foreach (var p in k.Properties)
                {
                    if (!keys.TryGetPropertyCaseInsensitive(p.Name, out var v))
                        continue;
                    var value = v.DeserializeJsonElement(p.PropertyInfo.PropertyType);
                    // check if it is default...
                    if (value == null || value.Equals(p.PropertyInfo.PropertyType.GetDefaultForType()))
                        continue;
                    var equals = Expression.Equal(Expression.Property(tx, p.PropertyInfo), Expression.Constant(value));
                    if (start == null)
                    {
                        start = equals;
                        continue;
                    }
                    start = Expression.AndAlso(start, equals);
                }
            }
            if (start == null)
                return Task.FromResult<T?>(null);
            var lambda = Expression.Lambda<Func<T?, bool>>(start, tx);
            var q = Query<T>().Where(lambda);
            return q.FirstOrDefaultAsync(token);
        }


        public Task<T?> LoadFromDbGenericAsync<T>(T entity, CancellationToken token = default)
            where T: class
        {
            var type = typeof(T);
            var tx = Expression.Parameter(type, "x");
            var t = db.Model.GetEntityTypes().FirstOrDefault(x => x.ClrType == type);
            Expression? start = null;
            foreach (var p in t.GetKeys().SelectMany(x => x.Properties))
            {
                var equals = Expression.Equal(Expression.Property(tx, p.PropertyInfo), Expression.Constant(p.PropertyInfo.GetValue(entity)));
                if (start == null)
                {
                    start = equals;
                    continue;
                }
                start = Expression.AndAlso(start, equals);
            }
            var lambda = Expression.Lambda<Func<T?, bool>>(start, tx);
            var q = Query<T>().Where(lambda);
            return q.FirstOrDefaultAsync(token);
        }

        public async Task<int> SaveChangesAsync(CancellationToken token = default) {
            if (SecurityDisabled)
            {
                return await db.SaveChangesAsync();
            }
            await using var tx = await db.Database.BeginTransactionAsync(token);
            var changes = db.ChangeTracker.Entries().ToList();

            // verify delete
            foreach(var entry in changes)
            {
                switch (entry.State)
                {
                    case EntityState.Modified:
                    case EntityState.Deleted:
                        await VerifyAccessAsync(entry);
                        break;
                }
            }

            int r = await db.SaveChangesAsync();
            foreach(var entry in changes)
            {
                switch (entry.State)
                {
                    case Microsoft.EntityFrameworkCore.EntityState.Added:
                    case Microsoft.EntityFrameworkCore.EntityState.Modified:
                        await VerifyAccessAsync(entry);
                        break;
                }
            }

            await tx.CommitAsync();
            return r;
        }

        private Task VerifyAccessAsync(EntityEntry entity)
        {
            return (this.GetType()
                .GetMethod(nameof(VerifyAccessGenericAsync), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .MakeGenericMethod(entity.Entity.GetType())
                .Invoke(this, new object[] { entity }) as Task)!;
        }

        private async Task VerifyAccessGenericAsync<T>(EntityEntry<T> entity)
            where T:class
        {
            var type = typeof(T);
            var tx = Expression.Parameter(type, "x");
            var t = db.Model.GetEntityTypes().FirstOrDefault(x => x.ClrType == type);
            Expression? start = null;
            foreach (var p in t.GetKeys().SelectMany(x => x.Properties))
            {
                var equals = Expression.Equal(Expression.Property(tx, p.PropertyInfo), Expression.Constant(p.PropertyInfo.GetValue(entity)));
                if (start == null)
                {
                    start = equals;
                    continue;
                }
                start = Expression.AndAlso(start, equals);
            }
            var lambda = Expression.Lambda<Func<T, bool>>(start, tx);
            var q = db.Set<T>().Where(lambda);
            switch (entity.State)
            {
                case EntityState.Added:
                    q = rules.ApplyInsert<T>(q, AssociatedUser);
                    break;
                case EntityState.Deleted:
                    q = rules.ApplyDelete<T>(q, AssociatedUser);
                    break;
                case EntityState.Modified:
                    q = rules.ApplyUpdate<T>(q, AssociatedUser);
                    break;
                default:
                    return;
            }
            var d = await q.AnyAsync();
            if (!d)
            {
                throw new UnauthorizedAccessException();
            }
        }

    }
}
