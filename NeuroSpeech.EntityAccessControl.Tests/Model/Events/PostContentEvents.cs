namespace NeuroSpeech.EntityAccessControl.Tests.Model.Events
{
    internal class PostContentEvents: DbEntityEvents<PostContent>
    {
        private readonly AppDbContext db;

        public PostContentEvents(AppDbContext db)
        {
            this.db = db;
        }

        public override IQueryContext<PostContent> Filter(IQueryContext<PostContent> q)
        {
            return q.Where(x => x.Post.AuthorID == db.UserID);
        }
    }
}