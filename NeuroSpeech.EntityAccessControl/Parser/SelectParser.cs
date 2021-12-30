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

    public delegate Task<IEnumerable<object>> SelectDelegate<T>(IQueryContext<T> q, params QueryParameter[] args);

    public class SelectParser
    {

        public static SelectParser Instance = new SelectParser();

        private Dictionary<(Type, string, int), Task> cache
            = new Dictionary<(Type, string, int), Task>();

        public async Task<IEnumerable<object>> Parse<T>(IQueryContext<T> q, string select, JsonElement parameters)
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

            var d = await Parse<T>(select, plist.Length);
            return await d(q, plist);
        }

        public Task<SelectDelegate<T>> Parse<T>(string code, int length)
        {
            var key = (typeof(T), code, length);
            if (cache.TryGetValue(key, out var d))
            {
                return (Task<SelectDelegate<T>>)d;
            }
            var t = ParseQuery<T>(code, length);
            cache[key] = t;
            return t;
        }

        private Task<SelectDelegate<T>> ParseQuery<T>(string code, int length)
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
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using System;

public static async Task<IEnumerable<object>> Query(IQueryContext<{type.FullName}> q, params QueryParameter[] args) {{
{sb}
return (await q.Select({code}).ToQuery().ToListAsync()).OfType<object>();
}}

return Query;
";
            return Microsoft.CodeAnalysis.CSharp.Scripting.CSharpScript.EvaluateAsync<SelectDelegate<T>>(code, options);
        }

    }
}
