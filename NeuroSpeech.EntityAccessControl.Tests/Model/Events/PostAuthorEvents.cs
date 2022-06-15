namespace NeuroSpeech.EntityAccessControl.Tests.Model.Events
{
    internal class PostAuthorEvents: DbEntityEvents<PostAuthor>
    {
        public override IQueryContext<PostAuthor> Filter(IQueryContext<PostAuthor> q)
        {
            return q;
        }
    }
}