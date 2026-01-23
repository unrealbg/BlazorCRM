namespace Crm.Web.Tests.Multitenancy
{
    using System.Net;
    using System.Net.Http.Headers;
    using System.Text.RegularExpressions;
    using System.Net.Http.Json;
    using System.Text.Json;
    using System.IdentityModel.Tokens.Jwt;
    using Crm.Contracts.Auth;
    using Crm.Domain.Entities;
    using Crm.Infrastructure.Persistence;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;

    public class TenantIntegrationTests
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

            if (!await db.Tenants.AnyAsync(t => t.Id == tenant1Id))
            {
                db.Tenants.Add(new Tenant { Id = tenant1Id, Name = "Tenant 1", Slug = "tenant1" });
            }

            if (!await db.Tenants.AnyAsync(t => t.Id == tenant2Id))
            {
                db.Tenants.Add(new Tenant { Id = tenant2Id, Name = "Tenant 2", Slug = "tenant2" });
            }

            db.Companies.Add(new Company { Id = Guid.NewGuid(), Name = "Tenant1Co", TenantId = tenant1Id });
            db.Companies.Add(new Company { Id = Guid.NewGuid(), Name = "Tenant2Co", TenantId = tenant2Id });
            await db.SaveChangesAsync();

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var user = new IdentityUser
            {
                UserName = "admin@local",
                Email = "admin@local",
                EmailConfirmed = true
            };
            await userManager.CreateAsync(user, "Admin123$");
        }

        private static async Task<string> GetAntiforgeryTokenAsync(HttpClient client)
        {
            var res = await client.GetAsync("/login");
            res.EnsureSuccessStatusCode();

            var html = await res.Content.ReadAsStringAsync();
            var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"", RegexOptions.IgnoreCase);
            if (!match.Success)
                throw new InvalidOperationException("Antiforgery token was not found in the login page.");

            return match.Groups[1].Value;
        }

        [Fact]
        public async Task CookieLogin_SetsTenantClaim_And_AllowsTenantScopedQueries()
        {
            var factory = new TestWebApplicationFactory();
            await SeedAsync(factory.Services, factory.Tenant1Id, factory.Tenant2Id);

            var client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = true
            });
            client.DefaultRequestHeaders.Host = "tenant1.localhost";

            var token = await GetAntiforgeryTokenAsync(client);
            var form = new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["Email"] = "admin@local",
                ["Password"] = "Admin123$"
            };

            var login = await client.PostAsync("/auth/login", new FormUrlEncodedContent(form));
            Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);

            var res = await client.GetAsync("/api/companies?page=1&pageSize=50");
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);

            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            var total = doc.RootElement.GetProperty("total").GetInt32();
            Assert.Equal(1, total);
        }

        [Fact]
        public async Task JwtLogin_IncludesTenantClaim_And_QueryFiltersApply()
        {
            var factory = new TestWebApplicationFactory();
            await SeedAsync(factory.Services, factory.Tenant1Id, factory.Tenant2Id);

            var client = factory.CreateClient();
            client.DefaultRequestHeaders.Host = "tenant1.localhost";

            var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin@local", "Admin123$"));
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);

            var payload = JsonDocument.Parse(await login.Content.ReadAsStringAsync()).RootElement;
            var token = payload.GetProperty("accessToken").GetString();
            Assert.False(string.IsNullOrWhiteSpace(token));

            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token!);
            var tenantClaim = jwt.Claims.FirstOrDefault(c => c.Type == "tenant");
            Assert.NotNull(tenantClaim);
            Assert.Equal(factory.Tenant1Id.ToString(), tenantClaim!.Value);
            var tenantSlug = jwt.Claims.FirstOrDefault(c => c.Type == "tenant_slug");
            Assert.NotNull(tenantSlug);
            Assert.Equal("tenant1", tenantSlug!.Value);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var res = await client.GetAsync("/api/companies?page=1&pageSize=50");
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);

            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            var total = doc.RootElement.GetProperty("total").GetInt32();
            Assert.Equal(1, total);
        }

        [Fact]
        public async Task UnknownTenantHost_Returns404_When_No_Dev_Fallback()
        {
            var factory = new TestWebApplicationFactory();
            await SeedAsync(factory.Services, factory.Tenant1Id, factory.Tenant2Id);

            var client = factory.CreateClient();
            client.DefaultRequestHeaders.Host = "unknown.localhost";

            var res = await client.GetAsync("/api/companies?page=1&pageSize=50");
            Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        }
    }
}
