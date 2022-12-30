using System.Linq;
namespace NeuroSpeech.EntityAccessControl.Tests.Model.Events
{
    internal class TagKeywordEvents: AppEntityEvents<TagKeyword>
    {
        public TagKeywordEvents(AppDbContext db) : base(db)
        {
        }

        public override IQueryable<TagKeyword> Filter(IQueryable<TagKeyword> q)
        {
            return q;
        }
    }
}