namespace Crm.Web.Tests.Multitenancy
{
    using System.Net;
    using System.Text.Json;
    using Crm.Domain.Entities;
    using Crm.Infrastructure.Persistence;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;

    public class TenantOutputCacheTests
    {
        private sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
        {
            public Guid Tenant1Id { get; } = Guid.Parse("11111111-1111-1111-1111-111111111111");
            public Guid Tenant2Id { get; } = Guid.Parse("22222222-2222-2222-2222-222222222222");
            private readonly string _dbName = $"crm-test-{Guid.NewGuid()}";

            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                builder.UseEnvironment("Testing");

                builder.ConfigureAppConfiguration(cfg =>
                {
                    var settings = new Dictionary<string, string?>
                    {
                        ["Tenancy:DefaultTenantSlug"] = "demo",
                        ["Tenancy:DefaultTenantName"] = "Demo",
                        ["OutputCache:EnableInTesting"] = "true",
                        ["Jwt:Key"] = "TEST_KEY_01234567890123456789012345678901",
                        ["Jwt:Issuer"] = "BlazorCrm",
                        ["Jwt:Audience"] = "BlazorCrmClients"
                    };

                    cfg.AddInMemoryCollection(settings);
                });

                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<DbContextOptions<CrmDbContext>>();
                    services.AddDbContext<CrmDbContext>(o => o.UseInMemoryDatabase(_dbName));
                });
            }
        }

        private static async Task SeedAsync(IServiceProvider services, Guid tenant1Id, Guid tenant2Id)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
            await db.Database.EnsureCreatedAsync();

            db.Tenants.Add(new Tenant { Id = tenant1Id, Name = "Tenant 1", Slug = "tenant1" });
            db.Tenants.Add(new Tenant { Id = tenant2Id, Name = "Tenant 2", Slug = "tenant2" });

            db.Companies.Add(new Company { Id = Guid.NewGuid(), Name = "Tenant1Co", TenantId = tenant1Id });
            db.Companies.Add(new Company { Id = Guid.NewGuid(), Name = "Tenant2Co", TenantId = tenant2Id });
            await db.SaveChangesAsync();
        }

        [Fact]
        public async Task Cached_Companies_Are_Tenant_Isolated()
        {
            var factory = new TestWebApplicationFactory();
            await SeedAsync(factory.Services, factory.Tenant1Id, factory.Tenant2Id);

            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Host = "tenant1.localhost";
            var t1Res = await client.GetAsync("/api/companies?page=1&pageSize=50");
            Assert.Equal(HttpStatusCode.OK, t1Res.StatusCode);
            using var t1Doc = JsonDocument.Parse(await t1Res.Content.ReadAsStringAsync());
            var t1Total = t1Doc.RootElement.GetProperty("total").GetInt32();
            Assert.Equal(1, t1Total);

            client.DefaultRequestHeaders.Host = "tenant2.localhost";
            var t2Res = await client.GetAsync("/api/companies?page=1&pageSize=50");
            Assert.Equal(HttpStatusCode.OK, t2Res.StatusCode);
            using var t2Doc = JsonDocument.Parse(await t2Res.Content.ReadAsStringAsync());
            var t2Total = t2Doc.RootElement.GetProperty("total").GetInt32();
            Assert.Equal(1, t2Total);
        }

        [Fact]
        public async Task Cache_Reused_Within_Same_Tenant()
        {
            var factory = new TestWebApplicationFactory();
            await SeedAsync(factory.Services, factory.Tenant1Id, factory.Tenant2Id);

            var client = factory.CreateClient();
            client.DefaultRequestHeaders.Host = "tenant1.localhost";

            var first = await client.GetAsync("/api/companies?page=1&pageSize=50");
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);

            var second = await client.GetAsync("/api/companies?page=1&pageSize=50");
            Assert.Equal(HttpStatusCode.OK, second.StatusCode);

            // OutputCache adds Age only for cached responses; second request should be cached.
            Assert.True(second.Headers.Age.HasValue);
        }
    }
}
