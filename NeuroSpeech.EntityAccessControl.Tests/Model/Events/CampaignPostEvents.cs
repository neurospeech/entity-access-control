using System.Linq;

namespace NeuroSpeech.EntityAccessControl.Tests.Model.Events
{

    internal class CampaignPostEvents: AppEntityEvents<CampaignPost>
    {
        public CampaignPostEvents(AppDbContext db) : base(db)
        {
        }

        public override IQueryable<CampaignPost> Filter(IQueryable<CampaignPost> q)
        {
            return q.Where(x => x.Campaign.AuthorID == db.UserID && x.Post.AuthorID == db.UserID);
        }

    }
}