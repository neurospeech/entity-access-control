using Microsoft.EntityFrameworkCore.ChangeTracking;
using NeuroSpeech.EntityAccessControl.Security;
using System.Reflection;
using System.Linq;

namespace NeuroSpeech.EntityAccessControl.Tests.Model.Events
{
    internal class PostActivityEvents : AppEntityEvents<PostActivity>
    {
        public PostActivityEvents(AppDbContext db) : base(db)
        {
        }

        public override IQueryable<PostActivity> Filter(IQueryable<PostActivity> q)
        {
            return q.Where(x => x.AccountID == db.UserID);
        }


        protected override IQueryable ForeignKeyFilter(ForeignKeyInfo<PostActivity> fk)
        {
            if(fk.Is(x => x.PostID))
            {
                return null;
            }
            return base.ForeignKeyFilter(fk);
        }
    }
}