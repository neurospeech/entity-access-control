namespace NeuroSpeech.EntityAccessControl.Tests.Model.Events
{
    internal class TagKeywordEvents: DbEntityEvents<TagKeyword>
    {
        public override IQueryContext<TagKeyword> Filter(IQueryContext<TagKeyword> q)
        {
            return q;
        }
    }
}