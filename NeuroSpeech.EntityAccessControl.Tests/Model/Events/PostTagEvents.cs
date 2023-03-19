using System.Linq;
namespace NeuroSpeech.EntityAccessControl.Tests.Model.Events
{
    internal class PostTagEvents: AppEntityEvents<PostTag>
    {

        public PostTagEvents(AppDbContext db): base(db)
        {
            
        }

        public override IQueryable<PostTag> Filter(IQueryable<PostTag> q)
        {
            return q.Where(x => x.Post.AuthorID == db.UserID);
        }

        public override IQueryable<PostTag> IncludeFilter(IQueryable<PostTag> q)
        {
            return q;
        }
    }
}