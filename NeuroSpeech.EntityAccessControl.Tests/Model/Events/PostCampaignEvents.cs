using System.Linq;

namespace NeuroSpeech.EntityAccessControl.Tests.Model.Events
{
    internal class PostCampaignEvents : AppEntityEvents<Campaign>
    {
        public PostCampaignEvents(AppDbContext db) : base(db)
        {
        }

        public override IQueryable<Campaign> Filter(IQueryable<Campaign> q)
        {
            return q.Where(x => x.AuthorID == db.UserID);
        }
    }
}