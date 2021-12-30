using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.EntityFrameworkCore;
using System;
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

    public delegate Task<LinqResult>
        LinqMethodDelegate<T>(
        IQueryContext<T> query, 
        List<QueryParameter> plist, 
        int start, 
        int size,
        CancellationToken cancelToken);

    public class MethodParser
    {



        public static MethodParser Instance = new MethodParser();

        private Dictionary<string, Task> cache
            = new Dictionary<string, Task>();

        public async Task<LinqResult> Parse<T>(
            IQueryContext<T> q, 
            List<LinqMethod> methods, 
            int start, 
            int size,
            CancellationToken cancelToken)
        {
            var d = await Parse<T>(new LinqSection(typeof(T), methods));
            var list = new List<QueryParameter>();
            foreach(var m in methods)
            {
                foreach(var p in m.Parameters)
                {
                    list.Add(new QueryParameter(p));
                }
            }
            return await d(q, list, start, size, cancelToken);
        }

        public Task<LinqMethodDelegate<T>> Parse<T>(LinqSection methods)
        {
            var key = methods.CacheKey;
            if (cache.TryGetValue(key, out var d))
            {
                return (Task<LinqMethodDelegate<T>>)d;
            }
            var t = ParseQuery<T>(methods.Methods);
            cache[key] = t;
            return t;
        }


        private Task<LinqMethodDelegate<T>> ParseQuery<T>(List<LinqMethod> methods)
        {
            var options = ScriptOptions.Default
                            .AddReferences(typeof(Queryable).Assembly,
                            typeof(Microsoft.EntityFrameworkCore.EF).Assembly,
                            typeof(QueryParser).Assembly,
                            typeof(T).Assembly)
                            .WithOptimizationLevel(OptimizationLevel.Debug);

            var type = typeof(T);

            var resultList = "";

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
