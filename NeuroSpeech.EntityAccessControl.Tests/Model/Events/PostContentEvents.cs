using System.Linq;
namespace NeuroSpeech.EntityAccessControl.Tests.Model.Events
{
    internal class PostContentEvents: AppEntityEvents<PostContent>
    {
        public PostContentEvents(AppDbContext db): base(db)
        {

        }

        public override IQueryable<PostContent> Filter(IQueryable<PostContent> q)
        {
            return q.Where(x => x.Post.AuthorID == db.UserID);
        }
    }
}