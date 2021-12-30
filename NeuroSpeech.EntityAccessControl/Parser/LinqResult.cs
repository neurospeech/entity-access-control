using System.Collections.Generic;

namespace NeuroSpeech.EntityAccessControl
{
    public class LinqResult
    {
        public IEnumerable<object>? Items { get; set; }

        public long Total { get; set; }
    }
}
