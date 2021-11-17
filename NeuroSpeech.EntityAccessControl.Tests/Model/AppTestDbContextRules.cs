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
            SetFilterForAll<Tag>((q, u) => q.Where(x => x.PostContents.Any(p => p.Post.AuthorID == u) 
                || x.PostTags.Any(p => p.Post.AuthorID == u)));

        }
    }
}
