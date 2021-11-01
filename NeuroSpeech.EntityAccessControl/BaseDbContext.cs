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
        private readonly DbContextEvents<T> events;

        public BaseDbContext(
            DbContextOptions<T> options,
            DbContextEvents<T> events) : base(options)
        {
            this.events = events;
        }

        public bool RaiseEvents { get; set; } = true;

        private Task OnInserting(Type type, object entity)
        {
            if(events.insertingHandlers.TryGetValue(type, out var eh))
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
            if (events.insertedHandlers.TryGetValue(type, out var eh))
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
            if (events.updatingHandlers.TryGetValue(type, out var eh))
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
            if (events.updatedHandlers.TryGetValue(type, out var eh))
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
            if (events.deletingHandlers.TryGetValue(type, out var eh))
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
            if (events.deletedHandlers.TryGetValue(type, out var eh))
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
