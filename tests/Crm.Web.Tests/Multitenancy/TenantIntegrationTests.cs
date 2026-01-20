namespace Crm.Web.Tests.Multitenancy
{
    using System.Net;
    using System.Net.Http.Headers;
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
            public Guid DefaultTenantId { get; } = Guid.Parse("11111111-1111-1111-1111-111111111111");

            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                builder.UseEnvironment("Testing");

                builder.ConfigureAppConfiguration(cfg =>
                {
                    var settings = new Dictionary<string, string?>
                    {
                        ["Tenancy:DefaultTenantId"] = DefaultTenantId.ToString(),
                        ["Tenancy:DefaultTenantName"] = "Default",
                        ["Jwt:Key"] = "TEST_KEY_01234567890123456789012345678901",
                        ["Jwt:Issuer"] = "BlazorCrm",
                        ["Jwt:Audience"] = "BlazorCrmClients"
                    };

                    cfg.AddInMemoryCollection(settings);
                });

                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<DbContextOptions<CrmDbContext>>();
                    services.AddDbContext<CrmDbContext>(o => o.UseInMemoryDatabase($"crm-test-{Guid.NewGuid()}"));
                });
            }
        }

        private static async Task SeedAsync(IServiceProvider services, Guid defaultTenantId)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
            await db.Database.EnsureCreatedAsync();

            if (!await db.Tenants.AnyAsync(t => t.Id == defaultTenantId))
            {
                db.Tenants.Add(new Tenant { Id = defaultTenantId, Name = "Default", Slug = "default" });
            }

            db.Companies.Add(new Company { Id = Guid.NewGuid(), Name = "DefaultCo", TenantId = defaultTenantId });
            db.Companies.Add(new Company { Id = Guid.NewGuid(), Name = "OtherCo", TenantId = Guid.NewGuid() });
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

        [Fact]
        public async Task CookieLogin_SetsTenantClaim_And_AllowsTenantScopedQueries()
        {
            var factory = new TestWebApplicationFactory();
            await SeedAsync(factory.Services, factory.DefaultTenantId);

            var client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = true
            });

            var form = new Dictionary<string, string>
            {
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
            await SeedAsync(factory.Services, factory.DefaultTenantId);

            var client = factory.CreateClient();

            var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin@local", "Admin123$"));
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);

            var payload = JsonDocument.Parse(await login.Content.ReadAsStringAsync()).RootElement;
            var token = payload.GetProperty("accessToken").GetString();
            Assert.False(string.IsNullOrWhiteSpace(token));

            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token!);
            var tenantClaim = jwt.Claims.FirstOrDefault(c => c.Type == "tenant");
            Assert.NotNull(tenantClaim);
            Assert.Equal(factory.DefaultTenantId.ToString(), tenantClaim!.Value);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var res = await client.GetAsync("/api/companies?page=1&pageSize=50");
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);

            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            var total = doc.RootElement.GetProperty("total").GetInt32();
            Assert.Equal(1, total);
        }
    }
}
