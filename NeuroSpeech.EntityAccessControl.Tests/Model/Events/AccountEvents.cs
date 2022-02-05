namespace NeuroSpeech.EntityAccessControl.Tests.Model.Events
{
    public class AccountEvents: DbEntityEvents<Account>
    {
        private readonly AppDbContext db;

        public AccountEvents(AppDbContext db)
        {
            this.db = db;
        }

        public override IQueryContext<Account> Filter(IQueryContext<Account> q)
        {
            return q.Where(x => x.AccountID == db.UserID && !x.Banned);
        }
    }
}
