using Microsoft.EntityFrameworkCore.ChangeTracking;
using NeuroSpeech.EntityAccessControl.Security;
using System.Reflection;

namespace NeuroSpeech.EntityAccessControl.Tests.Model.Events
{
    internal class PostActivityEvents : AppEntityEvents<PostActivity>
    {
        public PostActivityEvents(AppDbContext db) : base(db)
        {
        }

        public override IQueryContext<PostActivity> Filter(IQueryContext<PostActivity> q)
        {
            return q.Where(x => x.AccountID == db.UserID);
        }


        protected override IQueryContext ForeignKeyFilter<T>(ForeignKeyInfo<PostActivity> fk)
        {
            if(fk.Is(x => x.PostID))
            {
                return null;
            }
            return base.ForeignKeyFilter<T>(fk);
        }
    }
}