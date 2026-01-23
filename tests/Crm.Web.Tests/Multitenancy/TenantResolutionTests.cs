namespace Crm.Web.Tests.Multitenancy
{
    using Crm.Application.Common.Multitenancy;
    using Crm.Domain.Entities;
    using Crm.Infrastructure.Multitenancy;
    using Crm.Infrastructure.Persistence;
    using Microsoft.AspNetCore.Http;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Options;

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

        private static SubdomainTenantResolver CreateResolver(string host, string environment, string defaultSlug = "demo", params Tenant[] tenants)
        {
            var options = new DbContextOptionsBuilder<CrmDbContext>()
                .UseInMemoryDatabase($"tenant-test-{Guid.NewGuid()}")
                .Options;
            var db = new CrmDbContext(options, new TestTenantProvider());
            db.Tenants.AddRange(tenants);
            db.SaveChanges();

            var accessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext()
            };
            accessor.HttpContext!.Request.Host = new HostString(host);

            var env = new TestHostEnvironment { EnvironmentName = environment };
            var tenantOptions = Options.Create(new TenantOptions
            {
                DefaultTenantSlug = defaultSlug,
                DefaultTenantName = "Demo",
                BaseDomains = new[] { "crm.example.com" }
            });

            return new SubdomainTenantResolver(accessor, db, tenantOptions, env);
        }

        [Fact]
        public void Resolve_Subdomain_Localhost()
        {
            var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Tenant 1", Slug = "tenant1" };
            var resolver = CreateResolver("tenant1.localhost", Environments.Production, tenants: tenant);

            var resolved = resolver.Resolve();
            Assert.True(resolved.IsResolved);
            Assert.Equal(tenant.Id, resolved.TenantId);
            Assert.Equal("tenant1", resolved.TenantSlug);
        }

        [Fact]
        public void Resolve_Subdomain_BaseDomain()
        {
            var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Tenant 1", Slug = "tenant1" };
            var resolver = CreateResolver("tenant1.crm.example.com", Environments.Production, tenants: tenant);

            var resolved = resolver.Resolve();
            Assert.True(resolved.IsResolved);
            Assert.Equal(tenant.Id, resolved.TenantId);
        }

        [Fact]
        public void Resolve_NoSubdomain_Development_UsesDefault()
        {
            var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Demo", Slug = "demo" };
            var resolver = CreateResolver("localhost", Environments.Development, tenants: tenant);

            var resolved = resolver.Resolve();
            Assert.True(resolved.IsResolved);
            Assert.Equal("demo", resolved.TenantSlug);
        }

        [Fact]
        public void Resolve_NoSubdomain_Production_Fails()
        {
            var resolver = CreateResolver("localhost", Environments.Production);

            var resolved = resolver.Resolve();
            Assert.False(resolved.IsResolved);
        }
    }
}
