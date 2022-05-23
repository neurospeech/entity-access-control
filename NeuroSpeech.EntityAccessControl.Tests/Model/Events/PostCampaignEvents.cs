namespace NeuroSpeech.EntityAccessControl.Tests.Model.Events
{
    internal class PostCampaignEvents : AppEntityEvents<Campaign>
    {
        public PostCampaignEvents(AppDbContext db) : base(db)
        {
        }

        public override IQueryContext<Campaign> Filter(IQueryContext<Campaign> q)
        {
            return q.Where(x => x.AuthorID == db.UserID);
        }
    }
}