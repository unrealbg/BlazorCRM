namespace Crm.Web.Tests.Search
{
    using System.Net;
    using System.Linq;
    using System.Text.Json;
    using System.Text.RegularExpressions;
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

    public class SearchApiTests
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

        private static async Task SeedAsync(IServiceProvider services, Guid tenantId, Guid otherTenantId)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
            await db.Database.EnsureCreatedAsync();

            db.Tenants.Add(new Tenant { Id = tenantId, Name = "Demo", Slug = "demo" });
            db.Tenants.Add(new Tenant { Id = otherTenantId, Name = "Other", Slug = "other" });

            db.Companies.Add(new Company { Id = Guid.NewGuid(), Name = "Acme Corp", Industry = "Manufacturing", TenantId = tenantId });
            db.Companies.Add(new Company { Id = Guid.NewGuid(), Name = "OtherCo", Industry = "Services", TenantId = otherTenantId });

            db.Contacts.Add(new Contact { Id = Guid.NewGuid(), FirstName = "Jane", LastName = "Smith", Email = "jane@acme.com", TenantId = tenantId });
            db.Contacts.Add(new Contact { Id = Guid.NewGuid(), FirstName = "Other", LastName = "User", Email = "other@otherco.com", TenantId = otherTenantId });

            var pipelineId = Guid.NewGuid();
            var stageId = Guid.NewGuid();
            db.Pipelines.Add(new Pipeline { Id = pipelineId, Name = "Sales", TenantId = tenantId });
            db.Stages.Add(new Stage { Id = stageId, Name = "New", Order = 1, PipelineId = pipelineId, TenantId = tenantId });
            db.Deals.Add(new Deal { Id = Guid.NewGuid(), Title = "Website redesign", Currency = "USD", StageId = stageId, TenantId = tenantId });

            await db.SaveChangesAsync();

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var user = new IdentityUser { UserName = "admin@local", Email = "admin@local", EmailConfirmed = true };
            await userManager.CreateAsync(user, "Admin123$");
            await userManager.AddClaimAsync(user, new System.Security.Claims.Claim("tenant", tenantId.ToString()));
            await userManager.AddClaimAsync(user, new System.Security.Claims.Claim("tenant_slug", "demo"));
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

        private static async Task<HttpClient> SignInAsync(TestWebApplicationFactory factory)
        {
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

            var login = await client.PostAsync("/auth/login", new FormUrlEncodedContent(form));
            if (login.StatusCode != HttpStatusCode.Redirect)
            {
                throw new InvalidOperationException("Login failed in test setup.");
            }

            return client;
        }

        [Fact]
        public async Task Search_Returns_Expected_Results()
        {
            var factory = new TestWebApplicationFactory();
            await SeedAsync(factory.Services, factory.DefaultTenantId, factory.OtherTenantId);

            var client = await SignInAsync(factory);
            var res = await client.GetAsync("/api/search?q=Acme");

            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            var json = await res.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var companies = doc.RootElement.GetProperty("companies");
            var contacts = doc.RootElement.GetProperty("contacts");
            var deals = doc.RootElement.GetProperty("deals");

            Assert.True(companies.GetArrayLength() + contacts.GetArrayLength() + deals.GetArrayLength() > 0);
            Assert.True(companies.EnumerateArray().Any(x => x.GetProperty("type").GetString() == "company"));
        }

        [Fact]
        public async Task Search_Is_Tenant_Isolated()
        {
            var factory = new TestWebApplicationFactory();
            await SeedAsync(factory.Services, factory.DefaultTenantId, factory.OtherTenantId);

            var client = await SignInAsync(factory);
            var res = await client.GetAsync("/api/search?q=OtherCo");

            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            var json = await res.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var companies = doc.RootElement.GetProperty("companies");
            var contacts = doc.RootElement.GetProperty("contacts");
            var deals = doc.RootElement.GetProperty("deals");

            Assert.Equal(0, companies.GetArrayLength());
            Assert.Equal(0, contacts.GetArrayLength());
            Assert.Equal(0, deals.GetArrayLength());
        }
    }
}
