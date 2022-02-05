namespace NeuroSpeech.EntityAccessControl.Tests.Model.Events
{
    internal class PostTagEvents: DbEntityEvents<PostTag>
    {
        private readonly AppDbContext db;

        public PostTagEvents(AppDbContext db)
        {
            this.db = db;
        }

        public override IQueryContext<PostTag> Filter(IQueryContext<PostTag> q)
        {
            return q.Where(x => x.Post.AuthorID == db.UserID);
        }
    }
}