using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace NeuroSpeech.EntityAccessControl
{
    public delegate IQueryable<T> QueryableDelegate<T>(IQueryable<T> input, params object[] plist);

    public class QueryParser
    {

        public static QueryParser Instance = new QueryParser();
        private ScriptOptions options;

        public QueryParser()
        {
            this.options = ScriptOptions.Default
                            .AddReferences(typeof(Queryable).Assembly, 
                            typeof(Microsoft.EntityFrameworkCore.EF).Assembly)
                            .WithOptimizationLevel(OptimizationLevel.Debug);
        }

        //public static QueryableDelegate<T> Parse<T>(string method, string code, params object[] plist)
        //{
        //    StringBuilder sb = new StringBuilder();
        //    for (int i = 0; i < plist.Length; i++)
        //    {
                
        //        var pn = $"@{i}";
        //        var vn = $"v{i}";
        //        var o = plist[i].JsonToNative();
        //        switch (o)
        //        {
        //            case null:
        //                code = code.Replace(pn, $"null");
        //                break;
        //            case string @text:
        //                sb.Append($"string {vn}");
        //                code = code.Replace(pn, $"{vn} = (string)plist[{i}]");
        //                break;
        //        }
        //        if(o == null)
        //        {
        //            code = code.Replace(pn, "null");
        //            continue;
        //        }
        //        code = code.Replace(pn, o.ToString());
        //    }
        //}
             

    }
}
