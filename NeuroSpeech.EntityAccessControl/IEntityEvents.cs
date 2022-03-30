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

        IQueryContext Filter(IQueryContext q);

        List<JsonIgnoreProperty> GetIgnoreConditions();

        Task InsertingAsync(object entity);
        Task InsertedAsync(object entity);

        Task UpdatingAsync(object entity);

        Task UpdatedAsync(object entity);

        Task DeletingAsync(object entity);
        Task DeletedAsync(object entity);
    }
}
