using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeuroSpeech.EntityAccessControl.Tests.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace NeuroSpeech.EntityAccessControl.Tests.Insert
{
    [TestClass]
    public class LinqApiTest : BaseTest
    {
        [TestMethod]
        public async Task InsertAsync()
        {
            using var scope = CreateScope();

            await SelectAsync(scope);

        }

        [TestMethod]
        public async Task MethodsAsync()
        {
            using var scope = CreateScope();

            await SelectMethodAsync(scope);
        }

        [TestMethod]
        public async Task SelectContainsAsync()
        {
            using var scope = CreateScope();

            var db = scope.GetRequiredService<AppDbContext>();


            var sdb = new SecureAppTestDbContext(db, 2, new AppTestDbContextRules());

            var controller = new TestEntityController(sdb);
            var name = "NeuroSpeech.EntityAccessControl.Tests.Model.Post";

            var m = System.Text.Json.JsonSerializer.Serialize(new object[] {
                new {
                    where = new object[] { "x => x.PostID > @0 && @1.Contains(x.PostID)", 0 , new long[] { 
                        1,2,3,4
                    } }
                },
                new
                {
                    include = new object[] { "Tags" }
                },
                new {
                    select = new object[] { "x => new { x.PostID, x.Tags }" }
                }
            });

            var r = await controller.Methods(name,
                methods: m
                );

            Assert.IsNotNull(r);
        }

        private async Task SelectMethodAsync(IScopeServices services)
        {
            var db = services.GetRequiredService<AppDbContext>();


            var sdb = new SecureAppTestDbContext(db, 2, new AppTestDbContextRules());

            var controller = new TestEntityController(sdb);
            var name = "NeuroSpeech.EntityAccessControl.Tests.Model.Post";

            var m = System.Text.Json.JsonSerializer.Serialize(new object[] {
                new {
                    where = new object[] { "x => x.PostID > @0", 0 }
                },
                new
                {
                    include = new object[] { "Tags" }
                },
                new {
                    select = new object[] { "x => new { x.PostID, x.Tags }" }
                }
            });

            var r = await controller.Methods(name,
                methods: m
                );

            Assert.IsNotNull(r);
        }

        private async Task SelectAsync(IScopeServices services)
        {
            var db = services.GetRequiredService<AppDbContext>();
            var sdb = new SecureAppTestDbContext(db, 2, new AppTestDbContextRules());

            var userId = 2;

            //Expression<Func<Post, object>> s =
            //    (x) => new { 
            //        x.PostID,
            //        Tags = x.Tags.Where(v => v.Post.AuthorID == userId)
            //    };

            var controller = new TestEntityController(sdb);
            var name = "NeuroSpeech.EntityAccessControl.Tests.Model.Post";

            var r = await controller.Query(name,
                filter: "x => x.PostID > @0",
                parameters: "[1]",
                select: "x => new { x.PostID, Tags = x.Tags }"
                );
        }
    }
}
