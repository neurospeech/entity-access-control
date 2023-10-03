using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NeuroSpeech.EntityAccessControl.Parser
{

    public delegate Task<object>
        LinqMethodDelegate<T>(IQueryable<T> query, LinqMethodOptions args);

    public class MethodParser
    {



        public static MethodParser Instance = new();

        private readonly ConcurrentDictionary<string, Task> cache
            = new();

        public async Task<object> Parse<T>(IQueryable<T> queryContext, LinqMethodOptions args)
        {
            var d = await Parse<T>(args);
            return await d(queryContext, args);
        }

        private async Task<LinqMethodDelegate<T>> Parse<T>(LinqMethodOptions args)
        {
            var methods = new CacheKeyBuilder(typeof(T), args.Methods);
            var key = methods.CacheKey;
            var task = (Task<LinqMethodDelegate<T>>)cache.GetOrAdd(key, k => ParseQuery<T>(args, methods.Methods));
            try
            {
                return await task;
            } catch (Exception) {
                cache.Remove(key, out var none);
                throw;
            }
        }


        private Task<LinqMethodDelegate<T>> ParseQuery<T>(LinqMethodOptions args, List<LinqMethod> methods)
        {
            var list = new List<Assembly> {typeof(Queryable).Assembly,
                typeof(Microsoft.EntityFrameworkCore.EF).Assembly,
                typeof(QueryParser).Assembly,
                typeof(RelationalQueryableExtensions).Assembly,
                typeof(T).Assembly
            };

            if (!list.Contains(args.Type.Assembly))
            {
                list.Add(args.Type.Assembly);
            }

            var options = ScriptOptions.Default
                            .AddReferences(list)
                            .WithOptimizationLevel(OptimizationLevel.Debug)
                            .WithEmitDebugInformation(true);

            var type = typeof(T);

            StringBuilder sb = new();
            StringBuilder exec = new();
            int index = 0;
            int methodIndex = 0;
            foreach(var m in methods)
            {
                var code = m.Expression;
                if(code == null)
                {
                    exec.AppendLine($".{m.Method}");
                    methodIndex++;
                    continue; ;
                }
                sb.AppendLine($"method = methods.Methods[{methodIndex++}];");
                for (int i = 0; i < m.Parameters.Count; i++)
                {
                    var finalIndex = index++;
                    var pn = $"@{i}";
                    var vn = $"var p{finalIndex} = method.Parameters[{i}];";
                    sb.AppendLine(vn);
                    code = code.Replace(pn, $"p{finalIndex}");
                    code = code.Replace("CastAs.String(", "CastAs.String((int)");
                }
                foreach (var p in args.Names)
                {
                    code = code.Replace("." + p.Key, "." + p.Value, StringComparison.InvariantCultureIgnoreCase);
                }
                exec.AppendLine($".{m.Method}({code})");
            }

            var execString = System.Text.Json.JsonSerializer.Serialize("q" + exec);

            var finalCode = @$"
using NeuroSpeech.EntityAccessControl;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Text;
using System;

public static async Task<object> Query(
    IQueryable<{type.FullName}> q, 
    NeuroSpeech.EntityAccessControl.Parser.LinqMethodOptions methods) {{
    NeuroSpeech.EntityAccessControl.Parser.LinqMethod method;

    {sb}
    var rq = q{exec};
    return await rq.ToPagedListAsync(methods, {execString});
}}

return Query;
";
            try
            {
                return Microsoft.CodeAnalysis.CSharp.Scripting.CSharpScript.EvaluateAsync<LinqMethodDelegate<T>>(finalCode, options);
            }catch (System.Exception ex)
            {
                throw new System.InvalidOperationException($"Failed to parse {finalCode}", ex);
            } finally
            {
                GC.Collect();
            }
        }


    }
}
