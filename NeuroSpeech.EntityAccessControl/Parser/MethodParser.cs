using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NeuroSpeech.EntityAccessControl.Parser
{


    public class LinqSection
    {
        public LinqSection(Type type, List<LinqMethod> methods)
        {
            this.Methods = methods;

            var sb = new StringBuilder();
            sb.AppendLine(type.FullName);
            foreach (var m in methods)
            {
                sb.Append(m.Method);
                sb.Append(m.Length.ToString());
                sb.AppendLine(m.Expression);
            }
            CacheKey = sb.ToString();
        }

        public readonly List<LinqMethod> Methods;

        public readonly string CacheKey;

    }

    public class LinqMethod
    {        

        public string Method { get; set; }
        public string Expression { get; set; }
        public List<JsonElement> Parameters { get; } = new List<JsonElement>();


        internal int Length => Parameters.Count;

    }

    public class LinqMethodParameters<T>
    {
        public readonly IQueryContext<T> Query;
        public readonly List<LinqMethod> Methods;
        public readonly int Start;
        public readonly int Size;
        public readonly bool SplitInclude;
        public readonly Action<string>? Trace;
        public readonly CancellationToken CancelToken;

        public LinqMethodParameters(
            IQueryContext<T> query,
            List<LinqMethod> methods,
            int start,
            int size,
            bool splitInclude,
            Action<string>? trace = null,
            CancellationToken cancelToken = default
            )
        {
            this.Query = query;
            this.Methods = methods;
            this.Start = start;
            this.Size = size;
            this.SplitInclude = splitInclude;
            this.Trace = trace;
            this.CancelToken = cancelToken;
        }
    }

    public delegate Task<LinqResult>
        LinqMethodDelegate<T>(LinqMethodParameters<T> args);

    public class MethodParser
    {



        public static MethodParser Instance = new MethodParser();

        private ConcurrentDictionary<string, Task> cache
            = new ConcurrentDictionary<string, Task>();

        public async Task<LinqResult> Parse<T>(LinqMethodParameters<T> args)
        {
            var d = await Parse<T>(new LinqSection(typeof(T), args.Methods));
            var list = new List<QueryParameter>();
            foreach(var m in args.Methods)
            {
                foreach(var p in m.Parameters)
                {
                    list.Add(new QueryParameter(p));
                }
            }
            return await d(q, list, start, size, splitInclude, cancelToken);
        }

        public Task<LinqMethodDelegate<T>> Parse<T>(LinqSection methods)
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
            foreach(var m in methods)
            {
                var code = m.Expression;
                for (int i = 0; i < m.Length; i++)
                {
                    var finalIndex = index++;
                    var pn = $"@{i}";
                    var vn = $"var p{finalIndex} = args[{finalIndex}];";
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
    List<QueryParameter> args,
    int start,
    int size,
    bool splitInclude,
    CancellationToken cancelToken) {{
{sb}
    var rq = q{exec};
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
        total = await oq.CountAsync();
    }}
    if (splitInclude) {{
        rq = rq.AsSplitQuery();
    }}
    var rl = await rq.ToListAsync();
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
