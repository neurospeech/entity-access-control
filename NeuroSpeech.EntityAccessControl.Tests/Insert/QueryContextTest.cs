using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeuroSpeech.EntityAccessControl.Tests.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeuroSpeech.EntityAccessControl.Tests.Insert
{
    [TestClass]
    public class QueryContextTest: BaseTest
    {

        [TestMethod]
        public async Task Test1()
        {
            using var scope = CreateScope();

            var db = scope.GetRequiredService<AppDbContext>();

            db.UserID = 2;
            var sdb = db;

            var qc = new QueryContext<Post>(db, db.Posts) as IQueryContext<Post>;

            // db.Posts.Include(x => x.Tags).ThenInclude(x => x.Tag.Keywords);

            await qc
                .Include(x => x.Tags)
                .ThenInclude(a => a.Tag.Keywords)
                .ThenInclude(a => a.Tag.PostContents)
                .CountAsync();

        }

        [TestMethod]
        public async Task TestJoin()
        {
            using var scope = CreateScope();

            var db = scope.GetRequiredService<AppDbContext>();

            db.EnforceSecurity = true;
            db.UserID = 2;
            var sdb = db;

            var qc = new QueryContext<Account>(db, db.FilteredQuery<Account>()) as IQueryContext<Account>;

            var end = DateTime.UtcNow;
            var start = end.AddMonths(-1);

            var diff = end - start;
            var qjoin = qc.WithDateRange(start, end, "Day");

            var text = qjoin.ToQueryString();

            var list = await qjoin.CountAsync();

            Assert.AreEqual(diff.TotalDays, list);

        }

    }
}
