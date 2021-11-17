using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeuroSpeech.EntityAccessControl.Tests.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NeuroSpeech.EntityAccessControl.Tests.Insert
{
    public class TestEntityController: BaseEntityController
    {
        public TestEntityController(SecureAppTestDbContext db)
            : base(db)
        {

        }
    }

    [TestClass]
    public class InsertApiTest: BaseTest
    {

        [TestMethod]
        public async Task InsertAsync()
        {

            using var db = CreateContext();

            var sdb = new SecureAppTestDbContext(db, 2, new AppTestDbContextRules());

            var controller = new TestEntityController(sdb);

            var doc = System.Text.Json.JsonDocument.Parse("");
            await controller.Save(doc.RootElement);
        }

    }
}
