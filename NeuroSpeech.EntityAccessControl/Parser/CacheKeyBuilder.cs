using System;
using System.Collections.Generic;
using System.Text;

namespace NeuroSpeech.EntityAccessControl.Parser
{
    public class CacheKeyBuilder
    {
        public CacheKeyBuilder(Type type, List<LinqMethod> methods)
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
}
