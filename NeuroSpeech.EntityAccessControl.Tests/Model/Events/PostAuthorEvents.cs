using System.Linq;
namespace NeuroSpeech.EntityAccessControl.Tests.Model.Events
{
    internal class PostAuthorEvents: AppEntityEvents<PostAuthor>
    {
        public PostAuthorEvents(AppDbContext db) : base(db)
        {
        }

        public override IQueryable<PostAuthor> Filter(IQueryable<PostAuthor> q)
        {
            return q.Where(x => x.AccountID == db.UserID);
        }
    }
}