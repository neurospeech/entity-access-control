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
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NeuroSpeech.EntityAccessControl
{
    public interface IEntityEvents
    {
        Task InsertingAsync(object entity);
        Task InsertedAsync(object entity);

        Task UpdatingAsync(object entity);

        Task UpdatedAsync(object entity);

        Task DeletingAsync(object entity);
        Task DeletedAsync(object entity);
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

        private Task OnInsertingAsync(Type type, object entity)
        {
            var eh = events.GetEvents(services, type);
            if (eh!=null)
            {
                return eh.InsertingAsync(entity);
            }
            var bt = type.BaseType;
            if (bt == null || bt == typeof(object))
                return Task.CompletedTask;
            return OnInsertingAsync(bt, entity);
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
                        break;
                    case EntityState.Modified:
                        pending.Add((e.State, item, type));
                        await OnUpdatingAsync(type, item);
                        Validator.TryValidateObject(item, new ValidationContext(item), errors);
                        break;
                    case EntityState.Deleted:
                        pending.Add((e.State, item, type));
                        await OnDeletingAsync(type, item);
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
    }
}
