using System.Collections.Generic;

namespace NeuroSpeech.EntityAccessControl.Parser
{
    public class LinqMethod
    {        

        public string Method { get; set; }
        public string Expression { get; set; }
        public List<QueryParameter> Parameters { get; } = new List<QueryParameter>();


        internal int Length => Parameters.Count;

    }
}
