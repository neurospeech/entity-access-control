using System;
using System.Collections.Generic;
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

    }
}
