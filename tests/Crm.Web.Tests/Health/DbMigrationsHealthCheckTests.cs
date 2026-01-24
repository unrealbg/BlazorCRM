namespace Crm.Web.Tests.Health
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Crm.Application.Common.Multitenancy;
    using Crm.Infrastructure.Persistence;
    using Crm.Web.Infrastructure;
    using Microsoft.Data.Sqlite;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Diagnostics.HealthChecks;
    using Microsoft.Extensions.FileProviders;
    using Microsoft.Extensions.Hosting;

    public class DbMigrationsHealthCheckTests
    {
        private sealed class FixedTenantProvider : ITenantProvider
        {
            public Guid TenantId { get; } = Guid.NewGuid();
        }

        private sealed class TestEnv : IHostEnvironment
        {
            public string EnvironmentName { get; set; } = Environments.Production;

            public string ApplicationName { get; set; } = "Crm.Web.Tests";

            public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

            public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        }

        [Fact]
        public async Task Pending_Migrations_Return_Unhealthy()
        {
            await using var conn = new SqliteConnection("DataSource=:memory:");
            await conn.OpenAsync();

            var options = new DbContextOptionsBuilder<CrmDbContext>()
                .UseSqlite(conn)
                .Options;

            await using var db = new CrmDbContext(options, new FixedTenantProvider());

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Database:AutoMigrate"] = "false"
                })
                .Build();

            var healthCheck = new DbMigrationsHealthCheck(db, new TestEnv(), config);
            var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

            Assert.Equal(HealthStatus.Unhealthy, result.Status);
            Assert.Contains("Pending migrations", result.Description, StringComparison.OrdinalIgnoreCase);
        }
    }
}
