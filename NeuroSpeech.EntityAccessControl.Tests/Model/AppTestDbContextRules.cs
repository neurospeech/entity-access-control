﻿using System.Linq;

namespace NeuroSpeech.EntityAccessControl.Tests.Model
{
    //public class AppTestDbContextRules: BaseSecurityRules<long>
    //{
    //    public AppTestDbContextRules()
    //    {
    //        Ignore<Post>(x => new { 
    //            x.AdminComments
    //        });

    //        SetAllFilters<Account>((q, u) => q.Where(x => x.AccountID == u && !x.Banned));
    //        SetAllFilters<Post>((q, u) => q.Where(x => x.AuthorID == u));
    //        SetAllFilters<PostTag>((q, u) => q.Where(x => x.Post.AuthorID == u));
    //        SetAllFilters<PostContent>((q, u) => q.Where(x => x.Post.AuthorID == u));
    //        SetAllFilters<PostContentTag>((q, u) => q.Where(x => x.PostContent.Post.AuthorID == u));

    //        //SetFilters<Post>(insert: (q, u)
    //        //    => q.Where(x => x.AuthorID == u && !x.Author.Banned));

    //        // we can select all tags but cannot modify it...
    //        SetFilters<Tag>(select: Allow);

    //    }
    //}
}
