using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NeuroSpeech.EFCoreAutomaticMigration;
using NeuroSpeech.EntityAccessControl.Security;
using NeuroSpeech.EntityAccessControl.Tests.Model;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeuroSpeech.EntityAccessControl.Tests
{

    public interface IScopeServices: IServiceProvider, IDisposable
    {

    }

    

    public abstract class BaseTest: BaseSqlServerSessionTest
    {

        class ServiceScope : IScopeServices
        {
            private readonly IServiceScope scope;

            public ServiceScope(IServiceScope scope)
            {
                this.scope = scope;
            }

            public void Dispose()
            {
                scope.Dispose();
            }

            public object GetService(Type serviceType)
            {
                return scope.ServiceProvider.GetService(serviceType);
            }
        }

        public BaseTest()
        {

        }

        protected override void Configure(ServiceCollection services)
        {
            services.AddSingleton<ISecureRepository, SecureAppTestDbContext>();
            services.AddSingleton<DbContextEvents<AppDbContext>, AppDbContextEvents>();
            services.AddSingleton<BaseSecurityRules<long>, AppTestDbContextRules>();

            services.AddDbContext<AppDbContext>(options => {
                options.UseSqlServer(ConnectionString);
            });

        }

        protected override void Initialize()
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            // db.Database.ExecuteSqlRaw(db.Database.GenerateCreateScript().Replace("GO;",""));
            db.Database.EnsureCreated();
            db.Database.Migrate();
            // db.MigrationForSqlServer().Migrate();
            Seed(db);
        }

        protected IScopeServices CreateScope()
        {
            var scope = Services.CreateScope();
            return new ServiceScope(scope);
        }

        public System.Text.Json.JsonSerializerOptions jsonSerializerOptions 
            = new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web) { 
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            };

        public string JsonSerialize<T>(T value)
        {
            return System.Text.Json.JsonSerializer.Serialize<T>(value, jsonSerializerOptions);
        }

        protected void Seed<T>(AppDbContext db, params T[] items)
            where T: class
        {
            if (db.Set<T>().Any())
                return;
            foreach(var item in items)
            {
                db.Add<T>(item);
            }
            db.SaveChanges();
        }

        public void Seed(AppDbContext db)
        {
            Seed(db, new Account { 
                // admin
                AccountID = 1
            },new Account { 
                // non admin
                AccountID = 2
            });

            Seed(db, new Tag
            {
                Name = "funny"
            },new Tag
            {
                Name = "public"
            });
        }
    }
}
