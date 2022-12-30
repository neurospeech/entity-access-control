using System.Linq;
namespace NeuroSpeech.EntityAccessControl.Tests.Model.Events
{
    internal class PostContentTagEvents: AppEntityEvents<PostContentTag>
    {

        public PostContentTagEvents(AppDbContext db): base(db)
        {
            
        }

        public override IQueryable<PostContentTag> Filter(IQueryable<PostContentTag> q)
        {
            return q.Where(x => x.PostContent.Post.AuthorID == db.UserID);
        }
    }
}