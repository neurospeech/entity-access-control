using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NeuroSpeech.EntityAccessControl.Parser
{

    public delegate Task<LinqResult>
        LinqMethodDelegate<T>(IQueryContext<T> query, LinqMethodOptions args);

    public class MethodParser
    {



        public static MethodParser Instance = new MethodParser();

        private ConcurrentDictionary<string, Task> cache
            = new ConcurrentDictionary<string, Task>();

        public async Task<LinqResult> Parse<T>(IQueryContext<T> queryContext, LinqMethodOptions args)
        {
            var d = await Parse<T>(new CacheKeyBuilder(typeof(T), args.Methods));
            return await d(queryContext, args);
        }

        public Task<LinqMethodDelegate<T>> Parse<T>(CacheKeyBuilder methods)
        {
            var key = methods.CacheKey;
            return (Task<LinqMethodDelegate<T>>)cache.GetOrAdd(key, k => ParseQuery<T>(methods.Methods));
        }


        private Task<LinqMethodDelegate<T>> ParseQuery<T>(List<LinqMethod> methods)
        {
            var options = ScriptOptions.Default
                            .AddReferences(typeof(Queryable).Assembly,
                            typeof(Microsoft.EntityFrameworkCore.EF).Assembly,
                            typeof(QueryParser).Assembly,
                            typeof(RelationalQueryableExtensions).Assembly,
                            typeof(T).Assembly)
                            .WithOptimizationLevel(OptimizationLevel.Debug);

            var type = typeof(T);

            StringBuilder sb = new StringBuilder();
            StringBuilder exec = new StringBuilder();
            int index = 0;
            int methodIndex = 0;
            foreach(var m in methods)
            {
                var code = m.Expression;
                sb.AppendLine($"method = methods.Methods[{methodIndex++}];");
                for (int i = 0; i < m.Length; i++)
                {
                    var finalIndex = index++;
                    var pn = $"@{i}";
                    var vn = $"var p{finalIndex} = method.Parameters[{i}];";
                    sb.AppendLine(vn);
                    code = code.Replace(pn, $"p{finalIndex}");
                    code = code.Replace("CastAs.String(", "CastAs.String((int)");
                }
                exec.AppendLine($".{m.Method}({code})");
            }

            var finalCode = @$"
using NeuroSpeech.EntityAccessControl;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Text;
using System;

public static async Task<LinqResult> Query(
    IQueryContext<{type.FullName}> q, 
    NeuroSpeech.EntityAccessControl.Parser.LinqMethodOptions methods) {{
    NeuroSpeech.EntityAccessControl.Parser.LinqMethod method;

    var start = methods.Start;
    var size = methods.Size;
    var splitInclude = methods.SplitInclude;
    var trace = methods.Trace;

{sb}
    IQueryContext<{type.FullName}> rq = q{exec};
    var oq = rq;
    var total = 0;
    var loadTotal = false;
    if (start > 0) {{
        loadTotal = true;
        rq = rq.Skip(start);
    }}
    if (size > 0) {{
        loadTotal = true;
        rq = rq.Take(size);
    }}
    if (loadTotal) {{
        total = await oq.CountAsync(methods.CancelToken);
    }}
    if (splitInclude) {{
        rq = rq.AsSplitQuery();
    }}
    
    trace?.Invoke(rq.ToQueryString());
    
    var rl = await rq.ToListAsync(methods.CancelToken);
    return new LinqResult {{
        Items = rl,
        Total = total
    }};
}}

return Query;
";
            return Microsoft.CodeAnalysis.CSharp.Scripting.CSharpScript.EvaluateAsync<LinqMethodDelegate<T>>(finalCode, options);
        }


    }
}
