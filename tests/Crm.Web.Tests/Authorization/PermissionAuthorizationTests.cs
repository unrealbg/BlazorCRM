namespace Crm.Web.Tests.Authorization
{
    using System.Net;
    using System.Net.Http.Json;
    using System.Text.RegularExpressions;
    using Crm.Application.Common.Multitenancy;
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

    public class PermissionAuthorizationTests
    {
        private sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
        {
            public Guid DefaultTenantId { get; } = Guid.Parse("11111111-1111-1111-1111-111111111111");
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

        private static async Task SeedTenantAsync(IServiceProvider services, Guid tenantId)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
            await db.Database.EnsureCreatedAsync();

            if (!await db.Tenants.AnyAsync(t => t.Id == tenantId))
            {
                db.Tenants.Add(new Tenant { Id = tenantId, Name = "Demo", Slug = "demo" });
                await db.SaveChangesAsync();
            }
        }

        private static async Task SeedUserAsync(IServiceProvider services, string email, string password, bool isAdmin)
        {
            using var scope = services.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            var user = await userManager.FindByEmailAsync(email);
            if (user is null)
            {
                user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
                await userManager.CreateAsync(user, password);
            }

            if (isAdmin && !await userManager.IsInRoleAsync(user, "Admin"))
            {
                await userManager.AddToRoleAsync(user, "Admin");
            }
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

        private static async Task<HttpClient> SignInAsync(TestWebApplicationFactory factory, string email, string password)
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
                ["Email"] = email,
                ["Password"] = password
            };

            var login = await client.PostAsync("/auth/login", new FormUrlEncodedContent(form));
            if (login.StatusCode != HttpStatusCode.Redirect)
                throw new InvalidOperationException("Login failed in test setup.");

            return client;
        }

        [Fact]
        public async Task Unauthenticated_Request_Returns_401()
        {
            var factory = new TestWebApplicationFactory();
            await SeedTenantAsync(factory.Services, factory.DefaultTenantId);

            var client = factory.CreateClient();
            client.DefaultRequestHeaders.Host = "demo.localhost";
            var res = await client.PostAsJsonAsync("/api/companies", new { Name = "NoAuth" });

            Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        }

        [Fact]
        public async Task Authenticated_Without_Permission_Returns_403()
        {
            var factory = new TestWebApplicationFactory();
            await SeedTenantAsync(factory.Services, factory.DefaultTenantId);
            await SeedUserAsync(factory.Services, "user@local", "User123$", isAdmin: false);

            var client = await SignInAsync(factory, "user@local", "User123$");
            var res = await client.PostAsJsonAsync("/api/companies", new { Name = "Forbidden" });

            Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
        }

        [Fact]
        public async Task Authenticated_With_Permission_Returns_Success()
        {
            var factory = new TestWebApplicationFactory();
            await SeedTenantAsync(factory.Services, factory.DefaultTenantId);
            await SeedUserAsync(factory.Services, "admin@local", "Admin123$", isAdmin: true);

            var client = await SignInAsync(factory, "admin@local", "Admin123$");
            var res = await client.PostAsJsonAsync("/api/companies", new { Name = "Allowed" });

            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        }

        [Fact]
        public async Task Jwt_Authenticated_Without_Permission_Returns_403()
        {
            var factory = new TestWebApplicationFactory();
            await SeedTenantAsync(factory.Services, factory.DefaultTenantId);
            await SeedUserAsync(factory.Services, "user@local", "User123$", isAdmin: false);

            var client = factory.CreateClient();
            client.DefaultRequestHeaders.Host = "demo.localhost";

            var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("user@local", "User123$"));
            login.EnsureSuccessStatusCode();
            var tokens = await login.Content.ReadFromJsonAsync<LoginResponse>();
            Assert.NotNull(tokens);

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokens!.AccessToken);
            var res = await client.PostAsJsonAsync("/api/companies", new { Name = "Forbidden" });

            Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
        }

        [Fact]
        public async Task Jwt_Authenticated_With_Permission_Returns_Success()
        {
            var factory = new TestWebApplicationFactory();
            await SeedTenantAsync(factory.Services, factory.DefaultTenantId);
            await SeedUserAsync(factory.Services, "admin@local", "Admin123$", isAdmin: true);

            var client = factory.CreateClient();
            client.DefaultRequestHeaders.Host = "demo.localhost";

            var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin@local", "Admin123$"));
            login.EnsureSuccessStatusCode();
            var tokens = await login.Content.ReadFromJsonAsync<LoginResponse>();
            Assert.NotNull(tokens);

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokens!.AccessToken);
            var res = await client.PostAsJsonAsync("/api/companies", new { Name = "Allowed" });

            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        }
    }
}
