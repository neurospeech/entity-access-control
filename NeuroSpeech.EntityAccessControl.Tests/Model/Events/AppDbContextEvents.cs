using System;
using System.Collections.Generic;
using System.Text;

namespace NeuroSpeech.EntityAccessControl.Tests.Model.Events
{

    public class AppDbContextEvents : DbContextEvents<AppDbContext>
    {
        public AppDbContextEvents()
        {
            SetupPostEvents();
            Register();
        }



        private void SetupPostEvents()
        {
            Register<AccountEvents>();
            // Register<PostEvents>();
            Register<PostActivityEvents>();
            Register<PostTagEvents>();
            Register<PostContentEvents>();
            Register<PostAuthorEvents>();
            Register<PostContentTagEvents>();
            Register<TagKeywordEvents>();
            Register<PostCampaignEvents>();
            Register<CampaignPostEvents>();
            Register<TagEvents>();
        }
    }
}
