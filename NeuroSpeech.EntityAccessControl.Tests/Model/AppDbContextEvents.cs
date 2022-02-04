﻿using System;
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

            public override Task InsertingAsync(Post entity)
            {
                entity.AuthorID = db.UserID;
                if (entity.AuthorID == 3)
                {
                    throw NewEntityAccessException("Invalid AuthorID");
                }
                return Task.CompletedTask;
            }
        }

        private void SetupPostEvents()
        {
            Register<PostEvents>();
        }
    }
}
