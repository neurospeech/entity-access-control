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

            var qc = db.Posts;

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

            var qc = db.FilteredQuery<Account>();

            var end = DateTime.UtcNow;
            var start = end.AddMonths(-1);

            var diff = end - start;
            var qjoin = qc.JoinDateRange(start, end, "Day");

            var text = qjoin.ToQueryString();

            var list = await qjoin.Select((x) => new
            {
                count = x.Entity.Posts.Count()
            }).ToListAsync();

            Assert.AreEqual(diff.TotalDays, list.Count);
            Assert.AreEqual(2, list[0].count);

        }

        //[TestMethod]
        //public async Task TestMany()
        //{
        //    using var scope = CreateScope();

        //    var db = scope.GetRequiredService<AppDbContext>();

        //    db.EnforceSecurity = true;
        //    db.UserID = 2;
        //    var sdb = db;

        //    var qc = db.FilteredQuery<Account>();

        //    var end = DateTime.UtcNow;
        //    var start = end.AddMonths(-1);

        //    var diff = end - start;
        //    var qjoin = qc.JoinDateRange(start, end, "Day");

        //    var text = qjoin.ToQueryString();

        //    var list = await qjoin.Select((x) => new
        //    {
        //        count = x.Entity.Posts.SelectMany(p => p.Contents)
        //            .Where(p => p.PostContentID > 2)
        //            .Sum( p => p.PostID)
        //    }).ToListAsync();

        //    Assert.AreEqual(diff.TotalDays, list.Count);
        //    Assert.AreEqual(2, list[0].count);

        //}

        [TestMethod]
        public async Task TestNullable()
        {
            using var scope = CreateScope();

            var db = scope.GetRequiredService<AppDbContext>();

            db.EnforceSecurity = true;
            db.UserID = 2;
            var sdb = db;

            var qc = db.FilteredQuery<Post>();

            var list = await qc.Select((x) => new
            {
                Nullable = CastAs.Nullable(x.Author.IsAdmin)
            }).ToListAsync();

            Assert.IsNotNull(list.FirstOrDefault());

        }

    }
}
