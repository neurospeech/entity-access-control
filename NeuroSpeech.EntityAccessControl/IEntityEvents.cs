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

        IQueryContext ModifyFilter(IQueryContext q);
        IQueryContext DeleteFilter(IQueryContext q);

        IQueryContext IncludeFilter(IQueryContext q);

        List<PropertyInfo> GetIgnoreConditions(string typeCacheKey);

        List<PropertyInfo> GetReadOnlyProperties(string typeCacheKey);

        Task InsertingAsync(object entity);

        Task InsertedAsync(object entity);

        Task UpdatingAsync(object entity);

        Task UpdatedAsync(object entity);

        Task DeletingAsync(object entity);

        Task DeletedAsync(object entity);

        IQueryContext ReferenceFilter(IQueryContext qec, FilterContext fc);
    }
}
