using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeuroSpeech.EntityAccessControl.Tests.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NeuroSpeech.EntityAccessControl.Tests.Insert
{
    [TestClass]
    public class FunctionTest : BaseTest
    {

        [TestMethod]
        public async Task SelectFromExpression()
        {
            using var scope = CreateScope();

            var db = scope.GetRequiredService<AppDbContext>();

            db.UserID = 2;
            var sdb = db;

            var controller = new TestEntityController(sdb);
            var name = "NeuroSpeech.EntityAccessControl.Tests.Model.Pair";

            var m = System.Text.Json.JsonSerializer.Serialize(new object[] {
                new object[] { "orderByDescending", "x => x.Value"},
            });

            var parser = JsonDocument.Parse("[1]");

            var methods = JsonDocument.Parse(m);

            var r = await controller.PostMethod(name,
                new BaseEntityController.MethodOptions { 
                    Function = "GetLabelPairs",
                    Args = parser.RootElement,
                    Methods = methods.RootElement
                }
                );

            Assert.IsNotNull(r);
        }

        [TestMethod]
        public async Task SelectExecute()
        {
            using var scope = CreateScope();

            var db = scope.GetRequiredService<AppDbContext>();

            db.UserID = 2;
            var sdb = db;

            var controller = new TestEntityController(sdb);
            var name = "NeuroSpeech.EntityAccessControl.Tests.Model.Pair";

            var m = System.Text.Json.JsonSerializer.Serialize(new object[] {
                new object[] { "orderByDescending", "x => x.Value"},
            });

            var parser = JsonDocument.Parse("[1]");

            var methods = JsonDocument.Parse(m);

            var r = await controller.PostMethod(name,
                new BaseEntityController.MethodOptions
                {
                    Function = "GetLabelPairs2",
                    Args = parser.RootElement,
                    Methods = methods.RootElement
                }
                );

            Assert.IsNotNull(r);
        }

        [TestMethod]
        public async Task SelectEventFunctionExecute()
        {
            using var scope = CreateScope();

            var db = scope.GetRequiredService<AppDbContext>();

            db.UserID = 2;
            var sdb = db;

            var controller = new TestEntityController(sdb);
            var name = typeof(Post).FullName;

            var m = System.Text.Json.JsonSerializer.Serialize(new object[] {
                new object[] { "orderByDescending", "x => x.PostID"},
            });

            var parser = JsonDocument.Parse("[1]");

            var methods = JsonDocument.Parse(m);

            var r = await controller.PostMethod(name,
                new BaseEntityController.MethodOptions
                {
                    Function = "PublicPosts",
                    Args = parser.RootElement,
                    Methods = methods.RootElement
                }
                );

            Assert.IsNotNull(r);
        }
    }

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
                new object[] {"include", "x => x.Tags" },
            });

            var r = await controller.Methods(name,
                methods: m
                ) as JsonResult;

            var content = System.Text.Json.JsonSerializer.Serialize(r.Value, r.SerializerSettings as System.Text.Json.JsonSerializerOptions);

            var json = System.Text.Json.JsonDocument.Parse(content).RootElement;
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
                new object[] {"include", "x => x.Tags" },
                new object[] { "orderByDescending", "x => x.PostID"},
                // new object[] { "select", "x => new { x.PostID, x.Tags }" }
            });

            var r = await controller.Methods(name,
                methods: m
                );

            Assert.IsNotNull(r);
        }

        [TestMethod]
        public async Task SelectFirstOrDefault()
        {
            using var scope = CreateScope();

            var db = scope.GetRequiredService<AppDbContext>();

            db.UserID = 2;
            var sdb = db;

            var controller = new TestEntityController(sdb);
            var name = "NeuroSpeech.EntityAccessControl.Tests.Model.Post";

            var m = System.Text.Json.JsonSerializer.Serialize(new object[] {
                new object[] { "orderByDescending", "x => x.PostID"},
                new object[] { "select", "x => x.Tags.OrderBy(t => t.Name).FirstOrDefault()" }
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
                new object[] {"include", "x => x.Authors" },
                new object[] {"thenInclude", "x => x.Account" },
                new object[] {"select", "x => new { x.PostID, x.Tags }" }
            });

            var r = await controller.Methods(name,
                methods: m
                );

            Assert.IsNotNull(r);

        }

        [TestMethod]
        public async Task MultipleIncludeTest()
        {
            using var scope = CreateScope();

            var db = scope.GetRequiredService<AppDbContext>();
            db.UserID = 2;

            var sdb = db;

            var controller = new TestEntityController(sdb);
            var name = "NeuroSpeech.EntityAccessControl.Tests.Model.Post";

            var m = System.Text.Json.JsonSerializer.Serialize(new object[] {
                new object[] {"include", "x => x.Authors" },
                new object[] {"thenInclude", "x => x.Account" },
                new object[] {"include", "x => x.Authors" },
                    new object[] {"thenInclude", "x => x.Post.Tags" },
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
                new object[] {"include", "x => x.Tags" },
                new object[] {"select", "x => new { x.PostID, x.Tags }" }
            });

            var r = await controller.Methods(name,
                methods: m
                );

            Assert.IsNotNull(r);
        }

        [TestMethod]
        public async Task SelectDateRangeAsync()
        {
            using var scope = CreateScope();

            var db = scope.GetRequiredService<AppDbContext>();
            db.UserID = 2;

            var sdb = db;

            var controller = new TestEntityController(sdb);
            var name = "NeuroSpeech.EntityAccessControl.Tests.Model.Account";

            var end = DateTime.UtcNow;
            var start = end.AddMonths(-1);

            var m = System.Text.Json.JsonSerializer.Serialize(new object[] {
                new object[] {"joinDateRange", "", start, end, "Day"  },
                new object[] { "select", "x => new { count = x.Entity.Posts.Count() }" }
            });

            var r = await controller.Methods(name,
                methods: m
                );

            Assert.IsNotNull(r);
        }

        [TestMethod]
        public async Task SelectManyDateRangeAsync()
        {
            using var scope = CreateScope();

            var db = scope.GetRequiredService<AppDbContext>();
            db.UserID = 2;

            var sdb = db;

            var controller = new TestEntityController(sdb);
            var name = "NeuroSpeech.EntityAccessControl.Tests.Model.Account";

            var end = DateTime.UtcNow;
            var start = end.AddMonths(-1);

            var m = System.Text.Json.JsonSerializer.Serialize(new object[] {
                new object[] {"JoinDateRange", "", start, end, "Day"  },
                new object[] { "Select", @"
                x => new {
                    tags = x.Entity.Posts
                        .SelectMany(p => p.Tags)
                        .Where(x => x.PostID > @0)
                        .Sum(x => x.PostID) 
                }", 5 }
            });

            var r = await controller.Methods(name,
                methods: m
                );

            Assert.IsNotNull(r);
        }

        [TestMethod]
        public async Task SelectJoinAsync()
        {
            using var scope = CreateScope();

            var db = scope.GetRequiredService<AppDbContext>();
            db.UserID = 2;

            var sdb = db;
               

            var controller = new TestEntityController(sdb);
            var name = "NeuroSpeech.EntityAccessControl.Tests.Model.Account";

            var end = DateTime.UtcNow;
            var start = end.AddMonths(-1);

            var m = System.Text.Json.JsonSerializer.Serialize(new object[] {
                new object[] {"join", "NeuroSpeech.EntityAccessControl.Tests.Model.Campaign", "x => x.AccountID", "x => x.AuthorID"  },
                new object[] { "select", "x => new { dateCreated = x.Inner.DateCreated }" }
            });

            var r = await controller.Methods(name,
                methods: m
                );

            Assert.IsNotNull(r);
        }


        [TestMethod]
        public async Task SelectDateRange()
        {
            using var scope = CreateScope();

            var db = scope.GetRequiredService<AppDbContext>();
            db.UserID = 2;

            var sdb = db;

            //var q1 = db.PostActivities;

            //var a = db.Posts.Where(x => x.PostType == PostType.Page)
            //    .Select(x => new
            //    {
            //        all = q1.Count()
            //    });

            //Assert.IsNotNull(a)


            var controller = new TestEntityController(sdb);
            var name = "NeuroSpeech.EntityAccessControl.DateRange";

            var end = DateTime.UtcNow;
            var start = end.AddMonths(-1);

            var m = System.Text.Json.JsonSerializer.Serialize(new object[] {
                new object[] {"dateRange", "", start, end, "Day" },
                new object[] { "selectWith", "NeuroSpeech.EntityAccessControl.Tests.Model.Post" },
                new object[] { "select", "(x,y) => new { count = y.Count(c => c.DateCreated > x.StartDate) }" }
            });

            var r = await controller.Methods(name,
                methods: m
                );

            Assert.IsNotNull(r);
        }
        //[TestMethod]
        //public async Task SelectGroupAsync()
        //{
        //    using var scope = CreateScope();

        //    var db = scope.GetRequiredService<AppDbContext>();
        //    db.UserID = 2;

        //    var sdb = db;
        //    db.EnforceSecurity = false;
        //    var controller = new TestEntityController(sdb);
        //    var name = "NeuroSpeech.EntityAccessControl.Tests.Model.Post";

        //    var m = System.Text.Json.JsonSerializer.Serialize(new object[] {
        //        new object[] { "where", "x => x.PostID > @0 && @1.Contains(x.PostID)", 0 , new long[] {
        //                1,2,3,4
        //            } },
        //        new object[] { "select", "x => new { id = x.PostID, count = x.Tags.Count(), diff = EF.Functions.DateDiffDay(x.DateCreated, DateTime.UtcNow) }" },
        //        new object[] {"groupBy", "x => x.diff" },
        //        new object[] {"select", "x => new { a = x.Key, b = x.Count(), c = x.Sum(y => y.count ) }" }
        //    });

        //    var r = await controller.Methods(name,
        //        methods: m,
        //        size: -1
        //        );

        //    Assert.IsNotNull(r);
        //}

        private static async Task<ContentResult> SelectMethodAsync(IScopeServices services)
        {
            var db = services.GetRequiredService<AppDbContext>();
            db.UserID = 2;

            var sdb = db;

            var controller = new TestEntityController(sdb);
            var name = "NeuroSpeech.EntityAccessControl.Tests.Model.Post";

            var m = System.Text.Json.JsonSerializer.Serialize(new object[] {
                new object[] {"where", "x => x.PostID > @0", 0 },
                new object[] {"include", "x => x.Tags" },
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

            var node = System.Text.Json.JsonSerializer.Serialize(new object[] {
                        new object[] { "where", "x => x.PostID > @0", 1 },
                        new object[] { "select", "x => new { x.PostID, Tags = x.Tags }" }
                    });

            var r = await controller.PostMethod(name,
                new BaseEntityController.MethodOptions {
                    Methods = JsonDocument.Parse(node).RootElement
                });
            Assert.IsNotNull(r);
        }
    }
}
