using System.Linq;

namespace NeuroSpeech.EntityAccessControl.Tests.Model
{
    public class AppTestDbContextRules: BaseSecurityRules<long>
    {
        public AppTestDbContextRules()
        {
            SetFilterForAll<Post>((q, u) => q.Where(x => x.AuthorID == u));
            SetFilterForAll<PostTag>((q, u) => q.Where(x => x.Post.AuthorID == u));
            SetFilterForAll<PostContent>((q, u) => q.Where(x => x.Post.AuthorID == u));
            SetFilterForAll<PostContentTag>((q, u) => q.Where(x => x.PostContent.Post.AuthorID == u));

            // we can select all tags but cannot modify it...
            SetAllFilter<Tag>(select: (q, u) => q);

        }
    }
}
