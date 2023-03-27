using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;

namespace NeuroSpeech.EntityAccessControl.Parser
{
    public class LinqMethodOptions
    {
        public List<LinqMethod> Methods { get; set; }
        public int Start { get; set; }
        public int Size { get; set; }
        public bool SplitInclude { get; set; }
        public Action<string>? Trace { get; set; }
        public CancellationToken CancelToken { get; set; }
        public string? Function { get; internal set; }
        public JsonElement Parameters { get; internal set; }
        internal Type Type { get; set; }

    }
}
