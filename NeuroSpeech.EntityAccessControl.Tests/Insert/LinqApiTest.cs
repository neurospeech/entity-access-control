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
    public class LinqApiTest : BaseTest
    {
        [TestMethod]
        public async Task InsertAsync()
        {
            using var scope = CreateScope();

            await SelectAsync(scope);

        }

        private async Task SelectAsync(IScopeServices services)
        {
            var db = services.GetRequiredService<AppDbContext>();
            var sdb = new SecureAppTestDbContext(db, 2, new AppTestDbContextRules());

            var controller = new TestEntityController(sdb);
            var name = "NeuroSpeech.EntityAccessControl.Tests.Model.Post";

            var r = await controller.Query(name,
                filter: "x => x.PostID > @0",
                parameters: "[1]");
        }
    }
}
