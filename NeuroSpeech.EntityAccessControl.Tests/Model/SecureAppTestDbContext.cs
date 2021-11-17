using NeuroSpeech.EntityAccessControl.Security;
using System;
using System.Collections.Generic;
using System.Text;

namespace NeuroSpeech.EntityAccessControl.Tests.Model
{
    public class SecureAppTestDbContext : BaseSecureRepository<AppDbContext, long>
    {
        private readonly long UserID;

        public SecureAppTestDbContext(AppDbContext db, long client, BaseSecurityRules<long> rules)
            : base(db, client, rules)
        {
            this.UserID = client;
            db.UserID = client;
        }
        public override bool SecurityDisabled => UserID == 1;
    }
}
