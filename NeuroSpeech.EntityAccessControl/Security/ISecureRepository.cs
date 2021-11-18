using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NeuroSpeech.EntityAccessControl.Security
{
    public interface ISecureRepository
    {
        EntityEntry Entry(object item);

        Task<object?> FindByKeysAsync(IEntityType t, JsonElement keys, CancellationToken token = default);
        IQueryable<T?> Query<T>() where T : class;

        IModel Model { get; }

        void Add(object e);

        void Remove(object e);

        Task<int> SaveChangesAsync(CancellationToken token = default);
    }
}
