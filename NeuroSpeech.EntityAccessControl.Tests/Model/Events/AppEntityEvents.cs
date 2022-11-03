using System.Threading.Tasks;

namespace NeuroSpeech.EntityAccessControl.Tests.Model.Events
{
    internal class AppEntityEvents<T>: DbEntityEvents<T>
        where T : class
    {
        protected readonly AppDbContext db;

        public AppEntityEvents(AppDbContext db)
        {
            this.db = db;
        }

        public override Task InsertingAsync(T entity)
        {
            if (db.UserID > 0)
            {
                return Task.CompletedTask;
            }
            return base.InsertingAsync(entity);
        }

        public override Task UpdatingAsync(T entity)
        {
            if (db.UserID > 0)
            {
                return Task.CompletedTask;
            }
            return base.UpdatingAsync(entity);
        }

        public override Task DeletingAsync(T entity)
        {
            if (db.UserID > 0)
            {
                return Task.CompletedTask;
            }
            return base.DeletingAsync(entity);
        }

    }
}