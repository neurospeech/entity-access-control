using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NeuroSpeech.EntityAccessControl
{

    public readonly struct QueryParameter
    {
        private readonly JsonElement element;

        public QueryParameter(System.Text.Json.JsonElement element)
        {
            this.element = element;
        }

        public static implicit operator long (QueryParameter q)
        {
            return q.element.GetInt64();
        }

        public static implicit operator long?(QueryParameter q)
        {
            return q.element.ValueKind == JsonValueKind.Null ? null : q.element.GetInt64();
        }

        public static implicit operator int(QueryParameter q)
        {
            return q.element.GetInt32();
        }

        public static implicit operator int?(QueryParameter q)
        {
            return q.element.ValueKind == JsonValueKind.Null ? null : q.element.GetInt32();
        }

        public static implicit operator bool(QueryParameter q)
        {
            return q.element.GetBoolean();
        }

        public static implicit operator bool?(QueryParameter q)
        {
            return q.element.ValueKind == JsonValueKind.Null ? null : q.element.GetBoolean();
        }

        public static implicit operator string?(QueryParameter q)
        {
            return q.element.ValueKind == JsonValueKind.Null ? null : q.element.GetString();
        }

        public static implicit operator DateTime?(QueryParameter q)
        {
            return q.element.ValueKind == JsonValueKind.Null ? null : DateTime.Parse(q.element.GetString()!);
        }

        public static implicit operator DateTimeOffset?(QueryParameter q)
        {
            return q.element.ValueKind == JsonValueKind.Null ? null : DateTimeOffset.Parse(q.element.GetString()!);
        }

    }

    public delegate IQueryable<T> QueryableDelegate<T>(IQueryable<T> input, params QueryParameter[] plist);

    public static class QueryParserExtensions
    {
        public static Task<IQueryable<T>> WhereLinqAsync<T>(this IQueryable<T> q, string filter, JsonElement parameters)
        {
            return QueryParser.Instance.Parse(q, filter, parameters);
        }

    }

    public class QueryParser
    {



        public static QueryParser Instance = new QueryParser();

        private Dictionary<(Type, string, int), Task> cache = new Dictionary<(Type, string, int), Task>();

        public async Task<IQueryable<T>> Parse<T>(IQueryable<T> q, string filter, JsonElement parameters)
        {
            QueryParameter[] plist = Array.Empty<QueryParameter>();
            if (parameters.ValueKind == JsonValueKind.Array)
            {
                plist = new QueryParameter[parameters.GetArrayLength()];
                int i = 0;
                foreach (var item in parameters.EnumerateArray())
                {
                    plist[i++] = new QueryParameter(item);
                }
            }

            var d = await Parse<T>(filter, plist.Length);
            return d(q, plist);
        }

        public Task<QueryableDelegate<T>> Parse<T>(string code, int length)
        {
            var key = (typeof(T), code, length);
            if(cache.TryGetValue(key, out var d))
            {
                return (Task<QueryableDelegate<T>>)d;
            }
            var t = ParseQuery<T>(code, length);
            cache[key] = t;
            return t;
        }


        private Task<QueryableDelegate<T>> ParseQuery<T>(string code, int length)
        {
            var options = ScriptOptions.Default
                            .AddReferences(typeof(Queryable).Assembly,
                            typeof(Microsoft.EntityFrameworkCore.EF).Assembly,
                            typeof(QueryParser).Assembly,
                            typeof(T).Assembly)
                            .WithOptimizationLevel(OptimizationLevel.Debug);

            var type = typeof(T);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < length; i++)
            {

                var pn = $"@{i}";
                var vn = $"var p{i} = args[{i}];";
                sb.AppendLine(vn);
                code = code.Replace(pn, $"p{i}");

            }

            code = @$"
using NeuroSpeech.EntityAccessControl;
using System.Linq;
using System;

public static IQueryable<{type.FullName}> Query(IQueryable<{type.FullName}> q, params QueryParameter[] args) {{
{sb}
return q.Where({code});
}}

return Query;
";
            return Microsoft.CodeAnalysis.CSharp.Scripting.CSharpScript.EvaluateAsync<QueryableDelegate<T>>(code,options);
        }


    }
}
