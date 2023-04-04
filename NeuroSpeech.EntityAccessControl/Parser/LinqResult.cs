using System.Collections.Generic;

namespace NeuroSpeech.EntityAccessControl
{
    public class LinqResult<T>
    {
        public IEnumerable<T>? Items { get; set; }

        public long Total { get; set; }
    }
}
