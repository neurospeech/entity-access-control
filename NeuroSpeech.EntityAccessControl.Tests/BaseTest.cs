using NeuroSpeech.EFCore.MockSqlServer;
using NeuroSpeech.EFCoreAutomaticMigration;
using NeuroSpeech.EntityAccessControl.Tests.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NeuroSpeech.EntityAccessControl.Tests
{
    public abstract class BaseTest :
            MockSqlDatabaseContext<AppTestDbContext>
    {

        public BaseTest()
        {
        }

        protected override void DumpLogs()
        {
            System.Diagnostics.Debug.WriteLine(base.GeneratedLog);
        }

        public AppTestDbContext CreateContext()
        {
            var db = new AppTestDbContext();
            db.MigrationForSqlServer()
                    .Migrate(preventChangeColumn: true);
            Seed(db);
            return db;
        }

        public void Seed(AppTestDbContext db)
        {
            if (db.Tags.Any())
                return;
            db.Tags.Add(new Tag
            {
                Name = "funny"
            });
            db.Tags.Add(new Tag
            {
                Name = "public"
            });
            db.SaveChanges();
        }
    }
}
