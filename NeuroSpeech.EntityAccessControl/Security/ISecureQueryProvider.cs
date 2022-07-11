using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace NeuroSpeech.EntityAccessControl
{
    public interface ISecureQueryProvider
    {
        IModel Model { get; }
        bool EnforceSecurity { get; set; }

        string TypeCacheKey { get; }

        IQueryable<T> FilteredQuery<T>() where T : class;

        IEntityEvents? GetEntityEvents(Type type);

        internal IQueryable<T> Set<T>() where T : class;

        IQueryable<DateRange> DateRangeView(DateTime start, DateTime end, string step);

        List<PropertyInfo> GetIgnoredProperties(Type type);

        List<PropertyInfo> GetReadonlyProperties(Type type);
        IQueryContext<T> Apply<T>(IQueryContext<T> qec, bool asInclude = false) where T : class;
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
        Task<object?> FindByKeysAsync(IEntityType t, JsonElement item, CancellationToken cancellation = default);

        Task<(object entity, bool exists)> BuildOrLoadAsync(IEntityType entityType, JsonElement item, CancellationToken cancellation = default);

        void Remove(object entity);

        void Add(object entity);
    }
}
