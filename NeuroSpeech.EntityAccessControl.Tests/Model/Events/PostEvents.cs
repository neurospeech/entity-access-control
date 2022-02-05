using System.Threading.Tasks;

namespace NeuroSpeech.EntityAccessControl.Tests.Model.Events
{
    public class PostEvents: DbEntityEvents<Post>
    {
        private readonly AppDbContext db;

        public PostEvents(AppDbContext db)
        {
            this.db = db;
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

        public override void OnSetupIgnore()
        {
            Ignore(x => new { 
                x.AdminComments
            });
        }
    }
}
