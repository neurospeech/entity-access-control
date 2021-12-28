using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NeuroSpeech.EntityAccessControl
{
    public interface IEntityEvents
    {
        Task Inserting(object entity);
        Task Inserted(object entity);

        Task Updating(object entity);

        Task Updated(object entity);

        Task Deleting(object entity);
        Task Deleted(object entity);
    }

    public delegate Task OnEntityEvent<T, TEntity>(T context, TEntity entity);

    public class BaseDbContext<T> : DbContext
        where T: BaseDbContext<T>
    {
        private readonly DbContextEvents<T> events;
        private readonly IServiceProvider services;

        public BaseDbContext(
            DbContextOptions<T> options,
            DbContextEvents<T> events,
            IServiceProvider services) : base(options)
        {
            this.events = events;
            this.services = services;
            this.RaiseEvents = events != null;
        }

        public bool RaiseEvents { get; set; }

        private Task OnInserting(Type type, object entity)
        {
            var eh = events.GetEvents(services, type);
            if (eh!=null)
            {
                return eh.Inserting(entity);
            }
            var bt = type.BaseType;
            if (bt == null || bt == typeof(object))
                return Task.CompletedTask;
            return OnInserting(bt, entity);
        }

        private async Task OnInserted(Type type, object entity)
        {
            // call base class events first...
            var bt = type.BaseType;
            if (bt != null && bt != typeof(object))
            {
                await OnInserted(bt, entity);
            }
            var eh = events.GetEvents(services, type);
            if (eh != null)
            {
                await eh.Inserted(entity);
            }
        }

        private async Task OnUpdating(Type type, object entity)
        {
            // call base class events first...
            var bt = type.BaseType;
            if (bt != null && bt != typeof(object))
            {
                await OnUpdating(bt, entity);
            }
            var eh = events.GetEvents(services, type);
            if (eh != null)
            {
                await eh.Updating(entity);
            }
        }

        private async Task OnUpdated(Type type, object entity)
        {
            // call base class events first...
            var bt = type.BaseType;
            if (bt != null && bt != typeof(object))
            {
                await OnUpdated(bt, entity);
            }
            var eh = events.GetEvents(services, type);
            if (eh != null)
            {
                await eh.Updated(entity);
            }
        }


        private async Task OnDeleting(Type type, object entity)
        {
            // call base class events first...
            var bt = type.BaseType;
            if (bt != null && bt != typeof(object))
            {
                await OnDeleting(bt, entity);
            }
            var eh = events.GetEvents(services, type);
            if (eh != null)
            {
                await eh.Deleting(entity);
            }
        }

        private async Task OnDeleted(Type type, object entity)
        {
            // call base class events first...
            var bt = type.BaseType;
            if (bt != null && bt != typeof(object))
            {
                await OnDeleted(bt, entity);
            }
            var eh = events.GetEvents(services, type);
            if (eh != null)
            {
                await eh.Deleted(entity);
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
                        await OnInserting(type, item);
                        Validator.TryValidateObject(item, new ValidationContext(item), errors);
                        break;
                    case EntityState.Modified:
                        pending.Add((e.State, item, type));
                        await OnUpdating(type, item);
                        Validator.TryValidateObject(item, new ValidationContext(item), errors);
                        break;
                    case EntityState.Deleted:
                        pending.Add((e.State, item, type));
                        await OnDeleting(type, item);
                        Validator.TryValidateObject(item, new ValidationContext(item), errors);
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
