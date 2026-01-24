namespace Crm.Web.Tests.Multitenancy
{
    using Crm.Application.Common.Multitenancy;
    using Crm.Domain.Entities;
    using Crm.Infrastructure.Multitenancy;
    using Crm.Infrastructure.Persistence;
    using Microsoft.AspNetCore.Http;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Hosting;

    public class TenantResolutionTests
    {
        private sealed class TestTenantProvider : ITenantProvider
        {
            public Guid TenantId => Guid.Empty;
        }

        private sealed class TestHostEnvironment : IHostEnvironment
        {
            public string EnvironmentName { get; set; } = Environments.Production;
            public string ApplicationName { get; set; } = "Tests";
            public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
            public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = default!;
        }

        private static (SubdomainTenantResolver Resolver, HttpContext Context) CreateResolver(string host, string environment, string defaultSlug = "demo", params Tenant[] tenants)
        {
            var options = new DbContextOptionsBuilder<CrmDbContext>()
                .UseInMemoryDatabase($"tenant-test-{Guid.NewGuid()}")
                .Options;
            var db = new CrmDbContext(options, new TestTenantProvider());
            db.Tenants.AddRange(tenants);
            db.SaveChanges();

            var context = new DefaultHttpContext();
            context.Request.Host = new HostString(host);

            var env = new TestHostEnvironment { EnvironmentName = environment };
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Tenancy:DefaultTenantSlug"] = defaultSlug,
                    ["Tenancy:DefaultTenantName"] = "Demo",
                    ["Tenancy:BaseDomain"] = "crm.yourdomain.com",
                    ["Tenancy:DevHostSuffix"] = "localhost"
                })
                .Build();

            return (new SubdomainTenantResolver(options, config, env), context);
        }

        [Fact]
        public async Task Resolve_Subdomain_Localhost()
        {
            var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Demo", Slug = "demo" };
            var (resolver, context) = CreateResolver("demo.localhost", Environments.Production, tenants: tenant);

            var resolved = await resolver.ResolveAsync(context);
            Assert.Equal(tenant.Id, resolved.TenantId);
            Assert.Equal("demo", resolved.TenantSlug);
        }

        [Fact]
        public async Task Resolve_Subdomain_BaseDomain()
        {
            var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Demo", Slug = "demo" };
            var (resolver, context) = CreateResolver("demo.crm.yourdomain.com", Environments.Production, tenants: tenant);

            var resolved = await resolver.ResolveAsync(context);
            Assert.Equal(tenant.Id, resolved.TenantId);
        }

        [Fact]
        public async Task Resolve_NoSubdomain_Development_UsesDefault()
        {
            var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Demo", Slug = "demo" };
            var (resolver, context) = CreateResolver("localhost", Environments.Development, tenants: tenant);

            var resolved = await resolver.ResolveAsync(context);
            Assert.Equal("demo", resolved.TenantSlug);
        }

        [Fact]
        public async Task Resolve_NoSubdomain_Production_Fails()
        {
            var (resolver, context) = CreateResolver("localhost", Environments.Production);

            await Assert.ThrowsAsync<TenantResolutionException>(() => resolver.ResolveAsync(context));
        }
    }
}
