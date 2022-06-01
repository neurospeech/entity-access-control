using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace NeuroSpeech.EntityAccessControl
{
    public class FilterContext
    {
        public readonly EntityEntry Entry;
        public readonly INavigation Navigation;

        public FilterContext(EntityEntry e, INavigation nav)
        {
            this.Entry = e;
            this.Navigation = nav;
        }
    }
}