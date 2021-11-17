using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeuroSpeech.EntityAccessControl.Tests.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NeuroSpeech.EntityAccessControl.Tests.Insert
{
    [TestClass]
    public class InsertTest: BaseTest
    {

        [TestMethod]
        public async Task InsertAsync()
        {
            using var scope = CreateScope();
            using var db = scope.GetRequiredService<AppDbContext>();

            var sdb = new SecureAppTestDbContext(db, 2, new AppTestDbContextRules());

            sdb.Add(new Post { 
                Name = "a",
                Tags = new List<PostTag> {
                    new PostTag { 
                        Name = "funny"
                    },
                    new PostTag
                    {
                        Name = "public"
                    }
                },
                Contents = new List<PostContent> { 
                    new PostContent { 
                        Name = "b",
                        Tags = new List<PostContentTag> {
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
            });

            await db.SaveChangesAsync();

            sdb.Add(new Post
            {
                Name = "a",
                Tags = new List<PostTag> {
                    new PostTag {
                        Name = "funny"
                    },
                    new PostTag
                    {
                        Name = "public"
                    }
                },
                Contents = new List<PostContent> {
                    new PostContent {
                        Name = "b",
                        Tags = new List<PostContentTag> {
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
            });

            await db.SaveChangesAsync();

        }

    }
}
