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

        private void SetupPostEvents()
        {
            Register<Post>(Inserting);

            Task Inserting(AppDbContext db, Post post)
            {
                post.AuthorID = db.UserID;
                return Task.CompletedTask;
            }
        }
    }
}
