using System.Linq;
namespace NeuroSpeech.EntityAccessControl.Tests.Model.Events
{
    internal class TagEvents : AppEntityEvents<Tag>
    {
        public TagEvents(AppDbContext db) : base(db)
        {
        }

        public override IQueryable<Tag> Filter(IQueryable<Tag> q)
        {
            return q;
        }
    }
}