using Microsoft.EntityFrameworkCore.ChangeTracking;
using NeuroSpeech.EntityAccessControl.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NeuroSpeech.EntityAccessControl
{

    public interface IEntityEvents
    {

        bool EnforceSecurity { get; set; }

        IQueryable Filter(IQueryable q);

        IQueryable ModifyFilter(IQueryable q);
        IQueryable DeleteFilter(IQueryable q);

        List<PropertyInfo> GetIgnoreConditions(string typeCacheKey);

        List<PropertyInfo> GetReadOnlyProperties(string typeCacheKey);

        Task InsertingAsync(object entity);

        Task InsertedAsync(object entity);

        Task UpdatingAsync(object entity);

        Task UpdatedAsync(object entity);

        Task DeletingAsync(object entity);

        Task DeletedAsync(object entity);

        IQueryable? ForeignKeyFilter(EntityEntry entity, PropertyInfo key, object value, FilterFactory fs);
    }

    public interface IEntityEvents<T> : IEntityEvents
        where T : class
    {
        IQueryable<T> DeleteFilter(IQueryable<T> q);
        IQueryable<T> Filter(IQueryable<T> q);
        IQueryable<T> IncludeFilter(IQueryable<T> q, PropertyInfo? property);
        IQueryable<T> ModifyFilter(IQueryable<T> q);
    }
}
