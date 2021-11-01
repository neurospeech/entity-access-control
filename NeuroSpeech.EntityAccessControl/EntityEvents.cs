using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NeuroSpeech.EntityAccessControl
{
    public delegate Task OnEntityEvent<T, TEntity>(T context, TEntity entity);

    public class BaseDbContext<T> : DbContext
        where T: BaseDbContext<T>
    {

        public BaseDbContext(DbContextOptions<T> options) : base(options)
        {

        }

        abstract class EntityHandler
        {
            public abstract Task Run(BaseDbContext<T> db, object entity);
        }

        class EntityHandler<TEntity> : EntityHandler
        {
            private readonly Dictionary<Type, EntityHandler> handlers;
            private readonly Type? baseType;
            private readonly List<OnEntityEvent<T, TEntity>> tasks;
            public EntityHandler(Dictionary<Type, EntityHandler> handlers, Type? baseType)
            {
                this.handlers = handlers;
                this.baseType = baseType;
                this.tasks = new List<OnEntityEvent<T, TEntity>>();
            }

            public Action Add(OnEntityEvent<T, TEntity> task)
            {
                tasks.Add(task);
                return () => {
                    tasks.Remove(task);
                };
            }

            public override Task Run(BaseDbContext<T> baseDb, object @object)
            {
                var entity = (TEntity)@object;
                var db = (T)baseDb;
                if(baseType != null)
                {
                    if(handlers.TryGetValue(baseType,out var eh))
                    {
                        if (tasks == null)
                            return eh.Run(db, entity);
                        return RunAll(tasks, eh, db, entity);
                    }
                }
                if (tasks == null)
                    return Task.CompletedTask;
                return RunAll(tasks, db, entity);
            }

            private async Task RunAll(List<OnEntityEvent<T, TEntity>> tasks, T db, TEntity entity)
            {
                foreach(var item in tasks)
                {
                    await item(db, entity);
                }
            }

            private async Task RunAll(
                List<OnEntityEvent<T, TEntity>> tasks, 
                EntityHandler eh, 
                T db, TEntity entity)
            {
                await eh.Run(db, entity!);
                foreach(var item in tasks)
                {
                    await item(db, entity);
                }
            }
        }

        private static readonly Dictionary<Type, EntityHandler> insertingHandlers = 
            new();

        private static readonly Dictionary<Type, EntityHandler> insertedHandlers =
            new();

        private static readonly Dictionary<Type, EntityHandler> updatingHandlers = new ();
        private static readonly Dictionary<Type, EntityHandler> updatedHandlers = new();
        private static readonly Dictionary<Type, EntityHandler> deletingHandlers = new();
        private static readonly Dictionary<Type, EntityHandler> deletedHandlers = new();

        public static IDisposable Register<TEntity>(
            OnEntityEvent<T, TEntity>? inserting = null,
            OnEntityEvent<T, TEntity>? inserted = null,
            OnEntityEvent<T, TEntity>? updating = null,
            OnEntityEvent<T, TEntity>? updated = null,
            OnEntityEvent<T, TEntity>? deleting = null,
            OnEntityEvent<T, TEntity>? deleted = null)
            where TEntity : class
        {
            Type t = typeof(TEntity);

            var baseType = t.BaseType;
            if(baseType == typeof(object))
            {
                baseType = null;
            }

            Action? disposables = null;

            disposables += Setup(t, baseType, inserting, insertingHandlers);
            disposables += Setup(t, baseType, inserted, insertedHandlers);
            disposables += Setup(t, baseType, updating, updatingHandlers);
            disposables += Setup(t, baseType, updated, updatedHandlers);
            disposables += Setup(t, baseType, deleting, deletingHandlers);
            disposables += Setup(t, baseType, deleted, deletedHandlers);

            return new DisposableAction(disposables);

            static Action? Setup(Type type, Type? baseType, OnEntityEvent<T, TEntity>? task, Dictionary<Type,EntityHandler> list)
            {
                if (task == null)
                    return null;
                EntityHandler<TEntity> eh;
                lock (list)
                {
                    if (list.TryGetValue(type, out var ehv))
                    {
                        eh = (ehv as EntityHandler<TEntity>)!;
                    } else
                    {
                        eh = new EntityHandler<TEntity>(list, baseType);
                        list.Add(type, eh);
                    }
                }
                return eh.Add(task);
            }
        }

        public bool RaiseEvents { get; set; } = true;

        private Task OnInserting(Type type, object entity)
        {
            if(insertingHandlers.TryGetValue(type, out var eh))
            {
                return eh.Run(this, entity);
            }
            var bt = type.BaseType;
            if (bt == null || bt == typeof(object))
                return Task.CompletedTask;
            return OnInserting(bt, entity);
        }

        private Task OnInserted(Type type, object entity)
        {
            if (insertedHandlers.TryGetValue(type, out var eh))
            {
                return eh.Run(this, entity);
            }
            var bt = type.BaseType;
            if (bt == null || bt == typeof(object))
                return Task.CompletedTask;
            return OnInserted(bt, entity);
        }

        private Task OnUpdating(Type type, object entity)
        {
            if (updatingHandlers.TryGetValue(type, out var eh))
            {
                return eh.Run(this, entity);
            }
            var bt = type.BaseType;
            if (bt == null || bt == typeof(object))
                return Task.CompletedTask;
            return OnUpdating(bt, entity);
        }

        private Task OnUpdated(Type type, object entity)
        {
            if (updatedHandlers.TryGetValue(type, out var eh))
            {
                return eh.Run(this, entity);
            }
            var bt = type.BaseType;
            if (bt == null || bt == typeof(object))
                return Task.CompletedTask;
            return OnUpdated(bt, entity);
        }

        private Task OnDeleting(Type type, object entity)
        {
            if (deletingHandlers.TryGetValue(type, out var eh))
            {
                return eh.Run(this, entity);
            }
            var bt = type.BaseType;
            if (bt == null || bt == typeof(object))
                return Task.CompletedTask;
            return OnDeleting(bt, entity);
        }

        private Task OnDeleted(Type type, object entity)
        {
            if (deletedHandlers.TryGetValue(type, out var eh))
            {
                return eh.Run(this, entity);
            }
            var bt = type.BaseType;
            if (bt == null || bt == typeof(object))
                return Task.CompletedTask;
            return OnDeleted(bt, entity);
        }


        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            if (!RaiseEvents)
            {
                return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            }
            this.ChangeTracker.DetectChanges();
            var pending = new List<(EntityState State, object item, Type type)>();
            foreach(var e in this.ChangeTracker.Entries())
            {
                var item = e.Entity;
                var type = item.GetType();
                switch (e.State)
                {
                    case EntityState.Added:
                        pending.Add((e.State, item, type));
                        await OnInserting(type, item);
                        break;
                    case EntityState.Modified:
                        pending.Add((e.State, item, type));
                        await OnUpdating(type, item);
                        break;
                    case EntityState.Deleted:
                        pending.Add((e.State, item, type));
                        await OnDeleting(type, item);
                        break;
                }
            }
            var r = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            foreach (var (state, item, type) in pending)
            {
                switch (state)
                {
                    case EntityState.Added:
                        await OnInserted(type, item);
                        break;
                    case EntityState.Modified:
                        await OnUpdated(type, item);
                        break;
                    case EntityState.Deleted:
                        await OnDeleted(type, item);
                        break;
                }
            }
            return r;
        }
    }
}
