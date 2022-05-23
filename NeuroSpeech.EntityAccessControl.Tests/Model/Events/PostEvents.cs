using System.Threading.Tasks;

namespace NeuroSpeech.EntityAccessControl.Tests.Model.Events
{
    internal class PostEvents: AppEntityEvents<Post>
    {
        public PostEvents(AppDbContext db): base(db)
        {
            
        }

        public override IQueryContext<Post> Filter(IQueryContext<Post> q)
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
