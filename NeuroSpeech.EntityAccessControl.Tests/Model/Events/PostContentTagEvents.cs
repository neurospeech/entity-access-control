namespace NeuroSpeech.EntityAccessControl.Tests.Model.Events
{
    internal class PostContentTagEvents: DbEntityEvents<PostContentTag>
    {
        private readonly AppDbContext db;

        public PostContentTagEvents(AppDbContext db)
        {
            this.db = db;
        }

        public override IQueryContext<PostContentTag> Filter(IQueryContext<PostContentTag> q)
        {
            return q.Where(x => x.PostContent.Post.AuthorID == db.UserID);
        }
    }
}