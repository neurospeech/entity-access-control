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
            } catch (EntityAccessException)
            {

            }
        }


        [TestMethod]
        public async Task InsertAsync()
        {
            using var scope = CreateScope();
            using var db = scope.GetRequiredService<AppDbContext>();
            db.UserID = 2;
            var sdb = db;

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

            } catch (EntityAccessException)
            {

            }
        }


    }
}
