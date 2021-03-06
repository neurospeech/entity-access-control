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

            CreateDateRangeView(DBName);
        }

        private void CreateDateRangeView(string dBName)
        {
            var sql = @"CREATE FUNCTION [dbo].[DateRangeView]
(
	@start DateTime2,
	@end DateTime2,
	@step nvarchar(20) = 'Day'
)
RETURNS TABLE
AS
RETURN (
	WITH E1(N) AS (
            SELECT 1 UNION ALL SELECT 1 UNION ALL SELECT 1 UNION ALL 
            SELECT 1 UNION ALL SELECT 1 UNION ALL SELECT 1 UNION ALL 
            SELECT 1 UNION ALL SELECT 1 UNION ALL SELECT 1 UNION ALL SELECT 1
            ),                          -- 1*10^1 or 10 rows
    E2(N) AS (SELECT 1 FROM E1 a, E1 b),
	E4(N) AS (SELECT 1 FROM E2 a, E2 b),
	cteTally10000(N) AS (SELECT TOP (
		CASE
			WHEN @step = 'Year' THEN DATEDIFF(YEAR, @start, @end)
			WHEN @step = 'Quarter' THEN DATEDIFF(QUARTER, @start, @end)
			WHEN @step = 'Month' THEN DATEDIFF(MONTH, @start, @end) 
			WHEN @step = 'Week' THEN DATEDIFF(WEEK, @start, @end)			
			WHEN @step = 'Day' THEN DATEDIFF(DAY, @start, @end) 
			WHEN @step = 'Hour' THEN DATEDIFF(Hour, @start, @end) 
			WHEN @step = 'Year' THEN DATEDIFF(YEAR, @start, @end)
			ELSE DATEDIFF(DAY, @start, @end) 
		END
	) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) FROM E2)
	SELECT StartDate = CASE
			WHEN @step = 'Year' THEN DATEADD(YEAR, N-1,  @start)
			WHEN @step = 'Quarter' THEN DATEADD(QUARTER, N-1,  @start)
			WHEN @step = 'Month' THEN DATEADD(MONTH, N-1,  @start)
			WHEN @step = 'Week' THEN DATEADD(WEEK, N-1,  @start)
			WHEN @step = 'Day' THEN DATEADD(DAY, N-1,  @start)
			WHEN @step = 'Hour' THEN DATEADD(Hour, N-1,  @start)
			WHEN @step = 'Year' THEN DATEADD(YEAR, N-1,  @start)
			ELSE DATEADD(DAY, N-1,  @start)
		END,
	EndDate = CASE
			WHEN @step = 'Year' THEN DATEADD(YEAR, N,  @start)
			WHEN @step = 'Quarter' THEN DATEADD(QUARTER, N,  @start)
			WHEN @step = 'Month' THEN DATEADD(MONTH, N,  @start)
			WHEN @step = 'Week' THEN DATEADD(WEEK, N,  @start)
			WHEN @step = 'Day' THEN DATEADD(DAY, N,  @start)
			WHEN @step = 'Hour' THEN DATEADD(Hour, N,  @start)
			WHEN @step = 'Year' THEN DATEADD(YEAR, N,  @start)
			ELSE DATEADD(DAY, N,  @start)
		END
	FROM cteTally10000)
";
            Execute(sql, dBName);
        }

        private void Execute(string command, string? db = null)
        {
            using (var c = new SqlConnection($"server=(localdb)\\MSSQLLocalDB"))
            {
                if (db != null)
                {
                    c.ConnectionString += ";database=" + db;
                }
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
