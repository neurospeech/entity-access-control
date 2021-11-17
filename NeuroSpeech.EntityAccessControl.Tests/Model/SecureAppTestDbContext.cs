using NeuroSpeech.EntityAccessControl.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NeuroSpeech.EntityAccessControl.Tests.Model
{
    public class SecureAppTestDbContext : BaseSecureRepository<AppTestDbContext, long>
    {
        private readonly long UserID;

        public SecureAppTestDbContext(AppTestDbContext db, long client, AppTestDbContextRules rules)
            : base(db, client, rules)
        {
            this.UserID = client;
        }
        public override bool SecurityDisabled => UserID == 1;
    }

    public class AppTestDbContextRules: BaseSecurityRules<long>
    {
        public AppTestDbContextRules()
        {
            SetFilterForAll<Post>((q, u) => q.Where(x => x.AuthorID == u));
            SetFilterForAll<PostTag>((q, u) => q.Where(x => x.Post.AuthorID == u));
            SetFilterForAll<PostContent>((q, u) => q.Where(x => x.Post.AuthorID == u));
            SetFilterForAll<PostContentTag>((q, u) => q.Where(x => x.PostContent.Post.AuthorID == u));

        }
    }
}
