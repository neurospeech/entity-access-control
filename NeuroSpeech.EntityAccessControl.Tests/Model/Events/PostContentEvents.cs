namespace NeuroSpeech.EntityAccessControl.Tests.Model.Events
{
    internal class PostContentEvents: AppEntityEvents<PostContent>
    {
        public PostContentEvents(AppDbContext db): base(db)
        {

        }

        public override IQueryContext<PostContent> Filter(IQueryContext<PostContent> q)
        {
            return q.Where(x => x.Post.AuthorID == db.UserID);
        }
    }
}