using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeuroSpeech.EntityAccessControl.Tests.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NeuroSpeech.EntityAccessControl.Tests.Insert
{
    public class TestEntityController: BaseEntityController
    {
        public TestEntityController(SecureAppTestDbContext db)
            : base(db)
        {

        }
    }

    [TestClass]
    public class InsertApiTest: BaseTest
    {

        [TestMethod]
        public async Task InsertAsync()
        {
            using var scope = CreateScope();
            
            await InsertPostAsync(scope);
            await InsertPostAsync(scope);
            
        }

        [TestMethod]
        public async Task ReInsertAsync()
        {
            using var scope = CreateScope();

            await ReInsertPostAsync(scope);
            await ReInsertPostAsync(scope);

        }

        [TestMethod]
        public async Task InsertAdminAsync()
        {
            using var scope = CreateScope();
            
            await InsertPostAsync(scope, 1);
            await InsertPostAsync(scope, 1);
            ;
        }


        private async Task ReInsertPostAsync(IServiceProvider services, int userId = 2)
        {
            var db = services.GetRequiredService<AppDbContext>();
            var sdb = new SecureAppTestDbContext(db, userId, new AppTestDbContextRules());

            var controller = new TestEntityController(sdb);

            var doc = System.Text.Json.JsonDocument.Parse(
                JsonSerialize(new Dictionary<string, object>{
                    { "$type", typeof(Post).FullName },
                    { "Name", "a" },
                    { "Tags" , new PostTag[] {
                            new PostTag {
                                Name = "funny",
                                Tag = new Tag {
                                    Name = "funny"
                                }
                            },
                            new PostTag
                            {
                                Name = "public",
                                Tag = new Tag
                                {
                                    Name = "public"
                                }
                            }
                        }
                    },
                    {
                        "Contents", new PostContent[] {
                            new PostContent {
                                Name = "b",
                                Tags = new PostContentTag[] {
                                    new PostContentTag {
                                        Name = "funny"
                                    },
                                    new PostContentTag
                                    {
                                        Name = "public"
                                    }
                                }
                            }
                        }
                    }
            }));
            await controller.Save(doc.RootElement);
        }

        private async Task InsertPostAsync(IServiceProvider services, int userId = 2)
        {
            var db = services.GetRequiredService<AppDbContext>();
            var sdb = new SecureAppTestDbContext(db, userId, new AppTestDbContextRules());

            var controller = new TestEntityController(sdb);

            var doc = System.Text.Json.JsonDocument.Parse(
                JsonSerialize(new Dictionary<string, object>{
                    { "$type", typeof(Post).FullName },
                    { "Name", "a" },
                    { "Tags" , new PostTag[] {
                            new PostTag {
                                Name = "funny"
                            },
                            new PostTag
                            {
                                Name = "public"
                            }
                        }
                    },
                    {
                        "Contents", new PostContent[] {
                            new PostContent {
                                Name = "b",
                                Tags = new PostContentTag[] {
                                    new PostContentTag {
                                        Name = "funny"
                                    },
                                    new PostContentTag
                                    {
                                        Name = "public"
                                    }
                                }
                            }
                        }
                    }
            }));
            await controller.Save(doc.RootElement);
        }
    }
}
