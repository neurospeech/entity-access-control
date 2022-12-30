using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace NeuroSpeech.EntityAccessControl
{
    public static class QueryParserExtensions
    {
        public static Task<IQueryable<T>> WhereLinqAsync<T>(this IQueryable<T> q, string filter, JsonElement parameters)
        {
            return QueryParser.Instance.Parse(q, filter, parameters);
        }

        public static Task<IEnumerable<object>> SelectLinqAsync<T>(this IQueryable<T> q, string filter, JsonElement parameters)
        {
            return SelectParser.Instance.Parse(q, filter, parameters);
        }
    }
}
