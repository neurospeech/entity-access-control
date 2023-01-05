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
    public class InsertTest: BaseTest
    {

        [TestMethod]
        public async Task UnauthorizeDeleteAsync()
        {
            try
            {

                using var scope = CreateScope();
                using var db = scope.GetRequiredService<AppDbContext>();

                db.EnforceSecurity = true;
                db.UserID = 2;
                var sdb = db;
                var p = new Post
                {
                    Name = "a"
                };
                sdb.Add(p);

                await sdb.SaveChangesAsync();
                db.UserID = 4;
                db.Remove(p);
                await sdb.SaveChangesAsync();


                throw new InvalidOperationException();
            }
            catch (EntityAccessException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        [TestMethod]
        public async Task UnauthorizeEditAsync()
        {
            try
            {

                using var scope = CreateScope();
                using var db = scope.GetRequiredService<AppDbContext>();

                db.EnforceSecurity = true;
                db.UserID = 2;
                var sdb = db;
                var p = new Post
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
                }
                };
                sdb.Add(p);

                await sdb.SaveChangesAsync();

                db.UserID = 4;
                p.Description = "Hey";
                await sdb.SaveChangesAsync();


                throw new InvalidOperationException();
            }
            catch (EntityAccessException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        [TestMethod]
        public async Task UnauthorizeAsync()
        {
            try
            {

                using var scope = CreateScope();
                using var db = scope.GetRequiredService<AppDbContext>();

                db.EnforceSecurity = true;
                db.UserID = 3;
                var sdb = db;

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

                await sdb.SaveChangesAsync();


                throw new InvalidOperationException();
            } catch (EntityAccessException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }


        [TestMethod]
        public async Task InsertAsync()
        {
            using var scope = CreateScope();
            using var db = scope.GetRequiredService<AppDbContext>();
            db.UserID = 2;
            var sdb = db;
            db.Posts.Include(x => x.Tags);
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

            await sdb.SaveChangesAsync();

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

            await sdb.SaveChangesAsync();

        }

        [TestMethod]
        public async Task InsertAuthorsAsync()
        {
            using var scope = CreateScope();
            using var db = scope.GetRequiredService<AppDbContext>();
            db.UserID = 2;

            var sdb = db;
            // db.EnforceSecurity = true;
            sdb.Add(new Post
            {
                Name = "a",
                AuthorID = 2,
                Tags = new List<PostTag> {
                    new PostTag {
                        Name = "funny"
                    },
                    new PostTag
                    {
                        Name = "public"
                    }
                },
                Authors = new List<PostAuthor> { 
                    new PostAuthor
                    {
                        AccountID = 4
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

            await sdb.SaveChangesAsync();

        }

        [TestMethod]
        public async Task InsertLikeActivityAsync()
        {
            using var scope = CreateScope();
            using var db = scope.GetRequiredService<AppDbContext>();
            db.UserID = 2;
            var sdb = db;

            var post = new Post
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
            };

            sdb.Add(post);

            await sdb.SaveChangesAsync();

            using var scope2 = CreateScope();

            var db2 = scope2.GetRequiredService<AppDbContext>();
            db2.UserID = 4;

            db2.PostActivities.Add(new PostActivity { 
                PostID = post.PostID,
                AccountID = db2.UserID
            });

            db2.RaiseEvents = true;
            db2.EnforceSecurity = true;

            await db2.SaveChangesAsync();
        }


        [TestMethod]
        public async Task UnauthorizedInsertAuthorsAsync()
        {
            try
            {
                using var scope = CreateScope();
                using var db = scope.GetRequiredService<AppDbContext>();
                db.UserID = 2;
                var sdb = db;
                db.EnforceSecurity = true;


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
                    Authors = new List<PostAuthor> {
                    new PostAuthor
                    {
                        AccountID = 3
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

                await sdb.SaveChangesAsync();

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

                await sdb.SaveChangesAsync();
                Assert.Fail();

            } catch (EntityAccessException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }


    }
}
