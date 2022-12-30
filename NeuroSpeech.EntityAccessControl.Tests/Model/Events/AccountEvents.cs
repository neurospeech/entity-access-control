using System.Linq;

namespace NeuroSpeech.EntityAccessControl.Tests.Model.Events
{
    public class AccountEvents: DbEntityEvents<Account>
    {
        private readonly AppDbContext db;

        public AccountEvents(AppDbContext db)
        {
            this.db = db;
        }

        protected override void OnSetupIgnore(string typeCacheKey)
        {
            this.Ignore(x => new { 
                x.Password
            });
        }

        public override IQueryable<Account> Filter(IQueryable<Account> q)
        {
            return q.Where(x => x.AccountID == db.UserID && !x.Banned);
        }
    }
}
