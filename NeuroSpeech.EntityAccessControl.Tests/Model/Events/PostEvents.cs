﻿using System.Threading.Tasks;
using System.Linq;
namespace NeuroSpeech.EntityAccessControl.Tests.Model.Events
{
    [RegisterEvents(typeof(AppDbContext))]
    internal class PostEvents: AppEntityEvents<Post>
    {
        public PostEvents(AppDbContext db): base(db)
        {
            
        }

        [ExternalFunction]
        public IQueryable<Post> PublicPosts(long n)
        {
            return db.Posts.Where(x => x.PostID > n);
        }


        [ExternalFunction]
        public async Task<IQueryable<Post>> PublicPosts2(long n)
        {
            return db.Posts.Where(x => x.PostID > n);
        }

        public override IQueryable<Post> Filter(IQueryable<Post> q)
        {
            return q.Where(x => x.AuthorID == db.UserID);
        }

        public override Task InsertingAsync(Post entity)
        {
            entity.AuthorID = db.UserID;
            return Task.CompletedTask;
        }

        protected override void OnSetupIgnore(string typeCacheKey)
        {
            Ignore(x => new { 
                x.AdminComments
            });
        }
    }
}
