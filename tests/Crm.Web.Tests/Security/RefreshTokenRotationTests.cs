namespace Crm.Web.Tests.Security
{
    using System.Net;
    using System.Net.Http.Json;
    using Crm.Contracts.Auth;
    using Crm.Infrastructure.Persistence;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Storage;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;

    public class RefreshTokenRotationTests
    {
        private sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
        {
            private static readonly InMemoryDatabaseRoot DbRoot = new();
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
                        ["Jwt:Key"] = "TEST_KEY_01234567890123456789012345678901",
                        ["Jwt:Issuer"] = "BlazorCrm",
                        ["Jwt:Audience"] = "BlazorCrmClients"
                    };

                    cfg.AddInMemoryCollection(settings);
                });

                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<DbContextOptions<CrmDbContext>>();
                    services.AddDbContext<CrmDbContext>(o => o.UseInMemoryDatabase(_dbName, DbRoot));
                });
            }
        }

        private static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
            await db.Database.EnsureCreatedAsync();

            if (!await db.Tenants.AnyAsync(t => t.Slug == "demo"))
            {
                db.Tenants.Add(new Crm.Domain.Entities.Tenant { Id = Guid.NewGuid(), Name = "Demo", Slug = "demo" });
            }

            if (!await db.Tenants.AnyAsync(t => t.Slug == "other"))
            {
                db.Tenants.Add(new Crm.Domain.Entities.Tenant { Id = Guid.NewGuid(), Name = "Other", Slug = "other" });
            }

            await db.SaveChangesAsync();

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var user = await userManager.FindByEmailAsync("admin@local");
            if (user is null)
            {
                user = new IdentityUser
                {
                    UserName = "admin@local",
                    Email = "admin@local",
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(user, "Admin123$");
            }
        }

        private static HttpClient CreateClientForHost(WebApplicationFactory<Program> factory, string host)
        {
            var client = factory.CreateClient();
            client.DefaultRequestHeaders.Host = host;
            return client;
        }

        [Fact]
        public async Task Refresh_Rotates_And_Revokes_Old()
        {
            var factory = new TestWebApplicationFactory();
            await SeedAsync(factory.Services);

            var client = CreateClientForHost(factory, "demo.localhost");
            var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin@local", "Admin123$"));
            login.EnsureSuccessStatusCode();

            var loginRes = await login.Content.ReadFromJsonAsync<LoginResponse>();
            Assert.NotNull(loginRes);

            var refresh = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(loginRes!.RefreshToken));
            Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);

            var refreshRes = await refresh.Content.ReadFromJsonAsync<LoginResponse>();
            Assert.NotNull(refreshRes);
            Assert.NotEqual(loginRes.RefreshToken, refreshRes!.RefreshToken);

            var reuse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(loginRes.RefreshToken));
            Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);

            var revoked = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(refreshRes.RefreshToken));
            Assert.Equal(HttpStatusCode.Unauthorized, revoked.StatusCode);
        }

        [Fact]
        public async Task Refresh_Fails_On_Tenant_Mismatch()
        {
            var factory = new TestWebApplicationFactory();
            await SeedAsync(factory.Services);

            var demoClient = CreateClientForHost(factory, "demo.localhost");
            var login = await demoClient.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin@local", "Admin123$"));
            login.EnsureSuccessStatusCode();

            var loginRes = await login.Content.ReadFromJsonAsync<LoginResponse>();
            Assert.NotNull(loginRes);

            var otherClient = CreateClientForHost(factory, "other.localhost");
            var refresh = await otherClient.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(loginRes!.RefreshToken));
            Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
        }
    }
}
