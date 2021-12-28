using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NeuroSpeech.EntityAccessControl.Tests.Model
{

    public class AppDbContextEvents : DbContextEvents<AppDbContext>
    {
        public AppDbContextEvents()
        {
            SetupPostEvents();
        }

        public class PostEvents: DbEntityEvents<Post>
        {
            private readonly AppDbContext db;

            public PostEvents(AppDbContext db)
            {
                this.db = db;
            }

            public override Task Inserting(Post entity)
            {
                entity.AuthorID = db.UserID;
                return Task.CompletedTask;
            }
        }

        private void SetupPostEvents()
        {
            Register<PostEvents>();
        }
    }
}
