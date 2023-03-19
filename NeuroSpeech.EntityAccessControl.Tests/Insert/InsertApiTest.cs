using Microsoft.EntityFrameworkCore;
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
        public TestEntityController(AppDbContext db)
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

        [TestMethod]
        public async Task Empty()
        {
            using var services = CreateScope();
            ;
            var db = services.GetRequiredService<AppDbContext>();
            var controller = new TestEntityController(db);

            var doc = System.Text.Json.JsonDocument.Parse("[]");
            await controller.Save(doc.RootElement);
        }

        [TestMethod]
        public async Task SchedulePosts()
        {
            var (p, c) = await CreatePostCampaigns(2);

            var (p1, c1) = await CreatePostCampaigns(1);

            using var scope2 = CreateScope();

            var db = scope2.GetRequiredService<AppDbContext>();
            db.UserID = 2;
            db.EnforceSecurity = true;
            p = await db.Posts.FirstOrDefaultAsync(x => x.PostID == p.PostID);
            c = await db.Campaigns.FirstOrDefaultAsync(x => x.CampaignID == c.CampaignID);
            c.DateToSend = DateTime.UtcNow;
            var cp = new CampaignPost
            {
                Post = p,
                CampaignID = c.CampaignID
            };
            c.CampaignPosts = new List<CampaignPost> {  
               cp  
            };

            await db.SaveChangesAsync();

            await Assert.ThrowsExceptionAsync<EntityAccessException>(async () =>
            {
                cp.CampaignID = c1.CampaignID;
                await db.SaveChangesAsync();
            });
        }

        async Task<(Post, Campaign)> CreatePostCampaigns(long userID)
        {
            using var scope = CreateScope();
            var db = scope.GetRequiredService<AppDbContext>();
            db.UserID = userID;
            db.EnforceSecurity = true;
            var p = new Post
            {
                AuthorID = userID,
                Name = "a",
                Tags = new List<PostTag> {
                            new PostTag {
                                Name = "funny",
                            },
                            new PostTag
                            {
                                Name = "public",
                            }
                        }
            };
            db.Add(p);
            var c = new Campaign
            {
                AuthorID = userID,
                DateCreated = DateTime.UtcNow,
                DateToSend = DateTime.UtcNow,
            };
            db.Add(c);
            await db.SaveChangesAsync();
            return (p, c);
        }


        /// <summary>
        /// This method reinserts tags
        /// </summary>
        /// <param name="services"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        private async Task ReInsertPostAsync(IServiceProvider services, int userId = 2)
        {
            var db = services.GetRequiredService<AppDbContext>();
            db.UserID = userId;
            var controller = new TestEntityController(db);

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
            db.UserID = userId;
            var controller = new TestEntityController(db);

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
