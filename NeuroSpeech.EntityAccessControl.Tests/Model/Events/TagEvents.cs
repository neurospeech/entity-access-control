namespace NeuroSpeech.EntityAccessControl.Tests.Model.Events
{
    internal class TagEvents : AppEntityEvents<Tag>
    {
        public TagEvents(AppDbContext db) : base(db)
        {
        }

        public override IQueryContext<Tag> Filter(IQueryContext<Tag> q)
        {
            return q;
        }
    }
}