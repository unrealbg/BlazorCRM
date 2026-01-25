namespace Crm.Web.Tests.Files
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Text.RegularExpressions;
    using Crm.Application.Files;
    using Crm.Domain.Entities;
    using Crm.Infrastructure.Files;
    using Crm.Infrastructure.Persistence;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Storage;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;

    public class AttachmentDownloadTests
    {
        private sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
        {
            public Guid TenantId { get; } = Guid.Parse("11111111-1111-1111-1111-111111111111");
            private static readonly InMemoryDatabaseRoot DbRoot = new();
            private readonly string _dbName = $"crm-test-{Guid.NewGuid()}";
            private readonly string _uploadsRoot = Path.Combine(Path.GetTempPath(), "blazorcrm-tests", Guid.NewGuid().ToString("N"));

            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                builder.UseEnvironment("Testing");

                builder.ConfigureAppConfiguration(cfg =>
                {
                    var settings = new Dictionary<string, string?>
                    {
                        ["Tenancy:DefaultTenantSlug"] = "demo",
                        ["Tenancy:DefaultTenantName"] = "Demo",
                        ["Tenancy:BaseDomain"] = "crm.yourdomain.com",
                        ["Tenancy:DevHostSuffix"] = "localhost",
                        ["Attachments:UploadsRootPath"] = _uploadsRoot,
                        ["Attachments:MaxUploadBytes"] = "10485760",
                        ["Attachments:AllowedContentTypes:0"] = "text/plain",
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

        private static async Task SeedAsync(IServiceProvider services, Guid tenantId)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
            await db.Database.EnsureCreatedAsync();

            if (!await db.Tenants.AnyAsync(t => t.Id == tenantId))
            {
                db.Tenants.Add(new Tenant { Id = tenantId, Name = "Demo", Slug = "demo" });
                await db.SaveChangesAsync();
            }

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var user = await userManager.FindByEmailAsync("admin@local");
            if (user is null)
            {
                user = new IdentityUser { UserName = "admin@local", Email = "admin@local", EmailConfirmed = true };
                await userManager.CreateAsync(user, "Admin123$");
            }

            var claims = await userManager.GetClaimsAsync(user);
            if (!claims.Any(c => c.Type == "tenant" && c.Value == tenantId.ToString()))
            {
                await userManager.AddClaimAsync(user, new System.Security.Claims.Claim("tenant", tenantId.ToString()));
            }

            if (!claims.Any(c => c.Type == "tenant_slug" && c.Value == "demo"))
            {
                await userManager.AddClaimAsync(user, new System.Security.Claims.Claim("tenant_slug", "demo"));
            }
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
            Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);

            return client;
        }

        [Fact]
        public async Task Download_Returns_ContentType_And_FileName()
        {
            var factory = new TestWebApplicationFactory();
            await SeedAsync(factory.Services, factory.TenantId);

            Guid attachmentId;
            await using (var scope = factory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
                var storage = scope.ServiceProvider.GetRequiredService<IFileStorage>();
                await using var content = new MemoryStream(Encoding.UTF8.GetBytes("hello"));

                var blobRef = await storage.SaveAsync(content, "Report 2026.txt", "text/plain", factory.TenantId, "demo", CancellationToken.None);

                var attachment = new Attachment
                {
                    Id = Guid.NewGuid(),
                    TenantId = factory.TenantId,
                    FileName = "Report 2026.txt",
                    ContentType = "text/plain",
                    BlobRef = blobRef,
                    RelatedTo = Crm.Domain.Enums.RelatedToType.Deal,
                    RelatedId = Guid.NewGuid()
                };

                await db.Attachments.AddAsync(attachment);
                await db.SaveChangesAsync();
                attachmentId = attachment.Id;
            }

            var client = await SignInAsync(factory);
            var res = await client.GetAsync($"/attachments/{attachmentId}");

            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            Assert.Equal("text/plain", res.Content.Headers.ContentType?.MediaType);

            var contentDisposition = res.Content.Headers.ContentDisposition?.ToString();
            if (string.IsNullOrWhiteSpace(contentDisposition) && res.Content.Headers.TryGetValues("Content-Disposition", out var values))
            {
                contentDisposition = values.FirstOrDefault();
            }

            contentDisposition ??= string.Empty;

            Assert.Contains("Report 2026.txt", contentDisposition);
        }
    }
}
