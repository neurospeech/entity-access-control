namespace NeuroSpeech.EntityAccessControl.Tests.Model.Events
{
    internal class PostAuthorEvents: AppEntityEvents<PostAuthor>
    {
        public PostAuthorEvents(AppDbContext db) : base(db)
        {
        }

        public override IQueryContext<PostAuthor> Filter(IQueryContext<PostAuthor> q)
        {
            return q.Where(x => x.AccountID == db.UserID);
        }
    }
}