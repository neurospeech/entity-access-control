using Microsoft.EntityFrameworkCore;
using NeuroSpeech.EFCore.MockSqlServer;
using System;
using System.Collections.Generic;
using System.Text;

namespace NeuroSpeech.EntityAccessControl.Tests.Model
{
    public class AppTestDbContext: AppDbContext
    {
        public AppTestDbContext()
            : base(Create(MockDatabaseContext.Current.ConnectionString), null)
        {

        }

        public static DbContextOptions<AppDbContext> Create(string connectionString)
        {
            DbContextOptionsBuilder<AppDbContext> builder = new DbContextOptionsBuilder<AppDbContext>();
            builder.UseSqlServer(connectionString);
            return builder.Options;
        }
    }
}
