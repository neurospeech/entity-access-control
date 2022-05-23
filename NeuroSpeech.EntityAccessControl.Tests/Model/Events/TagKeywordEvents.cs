namespace NeuroSpeech.EntityAccessControl.Tests.Model.Events
{
    internal class TagKeywordEvents: AppEntityEvents<TagKeyword>
    {
        public TagKeywordEvents(AppDbContext db) : base(db)
        {
        }

        public override IQueryContext<TagKeyword> Filter(IQueryContext<TagKeyword> q)
        {
            return q;
        }
    }
}