namespace Crm.Web.Tests.Security
{
    using System.Net;
    using System.Text.RegularExpressions;
    using Crm.Infrastructure.Persistence;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Storage;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;

    public class LoginAntiforgeryTests
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
                await db.SaveChangesAsync();
            }

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
            {
                throw new InvalidOperationException("Antiforgery token was not found in the login page.");
            }

            return match.Groups[1].Value;
        }

        [Fact]
        public async Task Login_Post_Without_AntiforgeryToken_Fails()
        {
            var factory = new TestWebApplicationFactory();
            await SeedAsync(factory.Services);

            var client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = true
            });
            client.DefaultRequestHeaders.Host = "demo.localhost";

            var form = new Dictionary<string, string>
            {
                ["Email"] = "admin@local",
                ["Password"] = "Admin123$"
            };

            var res = await client.PostAsync("/auth/login", new FormUrlEncodedContent(form));
            Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
            var body = await res.Content.ReadAsStringAsync();
            Assert.Contains("Invalid request", body, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Login_Post_With_AntiforgeryToken_Succeeds()
        {
            var factory = new TestWebApplicationFactory();
            await SeedAsync(factory.Services);

            var client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = true
            });
            client.DefaultRequestHeaders.Host = "demo.localhost";

            var token = await GetAntiforgeryTokenAsync(client);
            var form = new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["Email"] = "admin@local",
                ["Password"] = "Admin123$"
            };

            var res = await client.PostAsync("/auth/login", new FormUrlEncodedContent(form));
            Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        }
    }
}
