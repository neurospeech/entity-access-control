namespace NeuroSpeech.EntityAccessControl.Tests.Model.Events
{
    internal class PostTagEvents: AppEntityEvents<PostTag>
    {

        public PostTagEvents(AppDbContext db): base(db)
        {
            
        }

        public override IQueryContext<PostTag> Filter(IQueryContext<PostTag> q)
        {
            return q.Where(x => x.Post.AuthorID == db.UserID);
        }
    }
}