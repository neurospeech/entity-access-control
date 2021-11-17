using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace NeuroSpeech.EntityAccessControl.Tests
{
    public abstract class BaseSqlServerSessionTest : BaseDbSessionTest
    {
        protected override void CreateDatabase(string DBName)
        {
            var DbFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), DBName + ".mdf");
            var LogFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), DBName + ".ldf");

            tempFiles.Add(DbFile);
            tempFiles.Add(LogFile);

            Execute($"CREATE DATABASE [{DBName}] ON PRIMARY (NAME = {DBName}_data, FILENAME='{DbFile}') LOG ON (NAME={DBName}_Log, FILENAME='{LogFile}')");
        }

        private void Execute(string command)
        {
            using (var c = new SqlConnection($"server=(localdb)\\MSSQLLocalDB"))
            {
                c.Open();


                using (var cmd = c.CreateCommand())
                {
                    cmd.CommandText = command;
                    cmd.ExecuteNonQuery();
                }
            }

        }

        protected override void DeleteDatabase(string DBName)
        {
            Execute("USE master;");
            Execute($"ALTER DATABASE [{DBName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;");
            Execute($"DROP DATABASE [{DBName}]");
        }

    }

    public abstract class BaseDbSessionTest: IDisposable
    {
        public readonly string ConnectionString;
        private readonly string DBName;
        public readonly ServiceProvider Services;
        protected List<string> tempFiles = new List<string>();

        public BaseDbSessionTest()
        {
            DBName = "App" + Guid.NewGuid().ToString("N");

            CreateDatabase(DBName);

            ConnectionString = (new SqlConnectionStringBuilder()
            {
                DataSource = "(localdb)\\MSSQLLocalDB",
                //sqlCnstr.AttachDBFilename = t;
                InitialCatalog = DBName,
                IntegratedSecurity = true,
                ApplicationName = "EntityFramework"
            }).ToString();


            ServiceCollection services = new ServiceCollection();

            Configure(services);

            Services = services.BuildServiceProvider();

            Initialize();
        }

        protected abstract void CreateDatabase(string dbName);
        protected abstract void Initialize();

        public void Dispose()
        {
            try
            {
                DeleteDatabase(DBName);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
             
            foreach (var file in tempFiles)
            {
                try {
                    if (System.IO.File.Exists(file))
                        System.IO.File.Delete(file);
                } catch (Exception ex) {
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                }
            }
        }

        protected abstract void DeleteDatabase(string dbName);
        protected abstract void Configure(ServiceCollection services);
    }
}
