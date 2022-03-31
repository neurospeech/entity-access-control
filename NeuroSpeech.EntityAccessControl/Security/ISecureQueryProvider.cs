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

        IQueryable<T> Query<T>() where T : class;
        List<JsonIgnoreProperty> GetIgnoreConditions(Type type);
        IQueryContext<T> Apply<T>(IQueryContext<T> qec, bool asInclude = false) where T : class;
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
        Task<object?> FindByKeysAsync(IEntityType t, JsonElement item, CancellationToken cancellation = default);
        void Remove(object entity);

        void Add(object entity);
    }
}
