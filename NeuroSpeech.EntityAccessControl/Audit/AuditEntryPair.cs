using NeuroSpeech.EntityAccessControl;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;

namespace NeuroSpeech.EntityAccessControl
{

    public class AuditEntryPair
    {
        // internal Microsoft.EntityFrameworkCore.Metadata.IProperty? Entry;

        public string? Name { get; set; }

        public object? Value { get; set; }

        public object? OldValue { get; set; }

        
    }
    
}
