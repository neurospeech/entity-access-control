using Microsoft.AspNetCore.Mvc;
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
        public async Task IgnoreAsync()
        {
            using var scope = CreateScope();

            var db = scope.GetRequiredService<AppDbContext>();
            db.UserID = 2;

            var sdb = db;

            var controller = new TestEntityController(sdb);
            var name = "NeuroSpeech.EntityAccessControl.Tests.Model.Post";

            var m = System.Text.Json.JsonSerializer.Serialize(new object[] {
                new object[] {"where", "x => x.PostID > @0", 0 },
                new object[] {"include", "Tags" },
            });

            var r = await controller.Methods(name,
                methods: m
                ) as ContentResult;

            var json = System.Text.Json.JsonDocument.Parse(r.Content).RootElement;
            var items = json.GetProperty("items");
            var item = items[0];
            if(item.TryGetPropertyCaseInsensitive("AdminComments", out var v))
            {
                Assert.Fail("AdminNotes should not be serialized");
            }
            
        }


        [TestMethod]
        public async Task SelectEnumAsync()
        {
            using var scope = CreateScope();

            var db = scope.GetRequiredService<AppDbContext>();

            db.UserID = 2;
            var sdb = db;

            var controller = new TestEntityController(sdb);
            var name = "NeuroSpeech.EntityAccessControl.Tests.Model.Post";

            var m = System.Text.Json.JsonSerializer.Serialize(new object[] {
                new object[] {"where", "x => CastAs.String(x.PostType) == @0", "Page" },
                new object[] {"include", "Tags" },
                new object[] { "orderByDescending", "x => x.PostID"},
                // new object[] { "select", "x => new { x.PostID, x.Tags }" }
            });

            var r = await controller.Methods(name,
                methods: m
                );

            Assert.IsNotNull(r);
        }

        [TestMethod]
        public async Task IncludeTest()
        {
            using var scope = CreateScope();

            var db = scope.GetRequiredService<AppDbContext>();
            db.UserID = 2;

            var sdb = db;

            var controller = new TestEntityController(sdb);
            var name = "NeuroSpeech.EntityAccessControl.Tests.Model.Post";

            var m = System.Text.Json.JsonSerializer.Serialize(new object[] {
                new object[] {"include", "x => x.Tags" },
                new object[] {"thenInclude", "x => x.Tag.Keywords" },
                new object[] {"thenInclude", "x => x.Tag.PostContents" },
                new object[] {"select", "x => new { x.PostID, x.Tags }" }
            });

            var r = await controller.Methods(name,
                methods: m
                );

            Assert.IsNotNull(r);

        }

        [TestMethod]
        public async Task SelectContainsAsync()
        {
            using var scope = CreateScope();

            var db = scope.GetRequiredService<AppDbContext>();
            db.UserID = 2;

            var sdb = db;

            var controller = new TestEntityController(sdb);
            var name = "NeuroSpeech.EntityAccessControl.Tests.Model.Post";

            var m = System.Text.Json.JsonSerializer.Serialize(new object[] {
                new object[] { "where", "x => x.PostID > @0 && @1.Contains(x.PostID)", 0 , new long[] {
                        1,2,3,4
                    } },
                new object[] {"include", "Tags" },
                new object[] {"select", "x => new { x.PostID, x.Tags }" }
            });

            var r = await controller.Methods(name,
                methods: m
                );

            Assert.IsNotNull(r);
        }

        private static async Task<ContentResult> SelectMethodAsync(IScopeServices services)
        {
            var db = services.GetRequiredService<AppDbContext>();
            db.UserID = 2;

            var sdb = db;

            var controller = new TestEntityController(sdb);
            var name = "NeuroSpeech.EntityAccessControl.Tests.Model.Post";

            var m = System.Text.Json.JsonSerializer.Serialize(new object[] {
                new object[] {"where", "x => x.PostID > @0", 0 },
                new object[] {"include", "Tags" },
                new object[] {"select", "x => new { x.PostID, x.Tags }" }
            });

            var r = await controller.Methods(name,
                methods: m
                );

            Assert.IsNotNull(r);
            return r as ContentResult;
        }

        private static async Task SelectAsync(IScopeServices services)
        {
            var db = services.GetRequiredService<AppDbContext>();
            db.UserID = 2;
            var sdb = db;

            // var userId = 2;

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
            Assert.IsNotNull(r);
        }
    }
}
