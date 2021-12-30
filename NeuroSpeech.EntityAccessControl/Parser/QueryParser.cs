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

    public delegate IQueryable<T> QueryableDelegate<T>(IQueryable<T> input, params QueryParameter[] plist);

    public class QueryParser
    {



        public static QueryParser Instance = new QueryParser();

        private Dictionary<(Type, string, string, int), Task> cache
            = new Dictionary<(Type, string, string, int), Task>();

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

        public Task<QueryableDelegate<T>> Parse<T>(string code, int length, string method = "Where")
        {
            var key = (typeof(T), method, code, length);
            if(cache.TryGetValue(key, out var d))
            {
                return (Task<QueryableDelegate<T>>)d;
            }
            var t = ParseQuery<T>(method, code, length);
            cache[key] = t;
            return t;
        }


        private Task<QueryableDelegate<T>> ParseQuery<T>(string method, string code, int length)
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

public static IQueryable<{type.FullName}> Query(IQueryable<{type.FullName}> q, params QueryParameter[] args) {{
{sb}
return q.{method}({code});
}}

return Query;
";
            return Microsoft.CodeAnalysis.CSharp.Scripting.CSharpScript.EvaluateAsync<QueryableDelegate<T>>(code,options);
        }


    }
}
