namespace Crm.Web.Tests.Security
{
    using System.IdentityModel.Tokens.Jwt;
    using System.Linq;
    using System.Net;
    using System.Net.Http.Json;
    using System.Security.Claims;
    using Crm.Contracts.Auth;
    using Crm.Domain.Entities;
    using Crm.Infrastructure.Persistence;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Storage;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;

    public class JwtLoginTests
    {
        private sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
        {
            public Guid DefaultTenantId { get; } = Guid.Parse("11111111-1111-1111-1111-111111111111");
            public Guid OtherTenantId { get; } = Guid.Parse("22222222-2222-2222-2222-222222222222");
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

        private static async Task SeedAsync(IServiceProvider services, Guid tenantId, string tenantSlug, bool seedOtherTenant)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
            await db.Database.EnsureCreatedAsync();

            if (!await db.Tenants.AnyAsync(t => t.Id == tenantId))
            {
                db.Tenants.Add(new Tenant { Id = tenantId, Name = "Demo", Slug = tenantSlug });
            }

            if (seedOtherTenant)
            {
                if (!await db.Tenants.AnyAsync(t => t.Slug == "other"))
                {
                    db.Tenants.Add(new Tenant { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Other", Slug = "other" });
                }
            }

            await db.SaveChangesAsync();

            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var user = await userManager.FindByEmailAsync("admin@local");
            if (user is null)
            {
                user = new IdentityUser { UserName = "admin@local", Email = "admin@local", EmailConfirmed = true };
                await userManager.CreateAsync(user, "Admin123$");
            }

            if (!await userManager.IsInRoleAsync(user, "Admin"))
            {
                await userManager.AddToRoleAsync(user, "Admin");
            }

            var claims = await userManager.GetClaimsAsync(user);
            if (!claims.Any(c => c.Type == "tenant" && c.Value == tenantId.ToString()))
            {
                await userManager.AddClaimAsync(user, new Claim("tenant", tenantId.ToString()));
            }

            if (!claims.Any(c => c.Type == "tenant_slug" && c.Value == tenantSlug))
            {
                await userManager.AddClaimAsync(user, new Claim("tenant_slug", tenantSlug));
            }
        }

        [Fact]
        public async Task Jwt_Login_Includes_Tenant_And_Roles()
        {
            var factory = new TestWebApplicationFactory();
            await SeedAsync(factory.Services, factory.DefaultTenantId, "demo", seedOtherTenant: false);

            var client = factory.CreateClient();
            client.DefaultRequestHeaders.Host = "demo.localhost";

            var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin@local", "Admin123$"));
            login.EnsureSuccessStatusCode();
            var tokens = await login.Content.ReadFromJsonAsync<LoginResponse>();
            Assert.NotNull(tokens);

            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(tokens!.AccessToken);

            Assert.Contains(token.Claims, c => c.Type == "tenant" && c.Value == factory.DefaultTenantId.ToString());
            Assert.Contains(token.Claims, c => c.Type == "tenant_slug" && c.Value == "demo");
            Assert.Contains(token.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Admin");
        }

        [Fact]
        public async Task Jwt_Login_Cross_Tenant_Is_Forbidden()
        {
            var factory = new TestWebApplicationFactory();
            await SeedAsync(factory.Services, factory.DefaultTenantId, "demo", seedOtherTenant: true);

            var client = factory.CreateClient();
            client.DefaultRequestHeaders.Host = "other.localhost";

            var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin@local", "Admin123$"));
            Assert.Equal(HttpStatusCode.Forbidden, login.StatusCode);
        }
    }
}
