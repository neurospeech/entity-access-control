using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NeuroSpeech.EntityAccessControl
{
    public class DbContextEvents<T>
        where T: BaseDbContext<T>
    {
        internal abstract class EntityHandler
        {
            public abstract Task Run(DbContext db, object entity);
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

            public override Task Run(DbContext baseDb, object @object)
            {
                var entity = (TEntity)@object;
                var db = (T)baseDb;
                if (baseType != null)
                {
                    if (handlers.TryGetValue(baseType, out var eh))
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
                foreach (var item in tasks)
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
                foreach (var item in tasks)
                {
                    await item(db, entity);
                }
            }
        }

        internal readonly Dictionary<Type, EntityHandler> insertingHandlers = new();
        internal readonly Dictionary<Type, EntityHandler> insertedHandlers = new();

        internal readonly Dictionary<Type, EntityHandler> updatingHandlers = new();
        internal readonly Dictionary<Type, EntityHandler> updatedHandlers = new();

        internal readonly Dictionary<Type, EntityHandler> deletingHandlers = new();
        internal readonly Dictionary<Type, EntityHandler> deletedHandlers = new();

        public IDisposable Register<TEntity>(
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
            if (baseType == typeof(object))
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

            static Action? Setup(Type type, Type? baseType, OnEntityEvent<T, TEntity>? task, Dictionary<Type, EntityHandler> list)
            {
                if (task == null)
                    return null;
                EntityHandler<TEntity> eh;
                lock (list)
                {
                    if (list.TryGetValue(type, out var ehv))
                    {
                        eh = (ehv as EntityHandler<TEntity>)!;
                    }
                    else
                    {
                        eh = new EntityHandler<TEntity>(list, baseType);
                        list.Add(type, eh);
                    }
                }
                return eh.Add(task);
            }
        }
    }
}
