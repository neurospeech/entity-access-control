using System.Threading.Tasks;
using System.Linq;
namespace NeuroSpeech.EntityAccessControl.Tests.Model.Events
{
    internal class PostEvents: AppEntityEvents<Post>
    {
        public PostEvents(AppDbContext db): base(db)
        {
            
        }

        public override IQueryable<Post> Filter(IQueryable<Post> q)
        {
            return q.Where(x => x.AuthorID == db.UserID);
        }

        public override Task InsertingAsync(Post entity)
        {
            entity.AuthorID = db.UserID;
            return Task.CompletedTask;
        }

        protected override void OnSetupIgnore(string typeCacheKey)
        {
            Ignore(x => new { 
                x.AdminComments
            });
        }
    }
}
