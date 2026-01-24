namespace Crm.Infrastructure.Multitenancy
{
    using Crm.Application.Common.Multitenancy;
    using Crm.Domain.Entities;
    using Crm.Infrastructure.Persistence;
    using Microsoft.AspNetCore.Http;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Hosting;
    using System;
    using System.Threading.Tasks;

    public sealed class SubdomainTenantResolver : ITenantResolver
    {
        private readonly DbContextOptions<CrmDbContext> _dbOptions;
        private readonly string _baseDomain;
        private readonly string _devHostSuffix;
        private readonly string? _defaultTenantSlug;
        private readonly string _defaultTenantName;
        private readonly IHostEnvironment _env;

        private sealed class NullTenantProvider : ITenantProvider
        {
            public Guid TenantId => Guid.Empty;
        }

        public SubdomainTenantResolver(
            DbContextOptions<CrmDbContext> dbOptions,
            IConfiguration configuration,
            IHostEnvironment env)
        {
            _dbOptions = dbOptions;
            _baseDomain = configuration["Tenancy:BaseDomain"] ?? string.Empty;
            _devHostSuffix = configuration["Tenancy:DevHostSuffix"] ?? "localhost";
            _defaultTenantSlug = configuration["Tenancy:DefaultTenantSlug"];
            _defaultTenantName = configuration["Tenancy:DefaultTenantName"] ?? "Demo";
            _env = env;
        }

        public async Task<TenantContext> ResolveAsync(HttpContext httpContext)
        {
            if (httpContext.Items.TryGetValue(TenantContextKeys.Context, out var cached) && cached is TenantContext cachedContext)
            {
                return cachedContext;
            }

            var host = httpContext.Request.Host.Host;
            var isDev = _env.IsDevelopment();
            var slug = ExtractSlug(host);

            if (string.IsNullOrWhiteSpace(slug))
            {
                if (string.Equals(host, _devHostSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    if (!isDev || string.IsNullOrWhiteSpace(_defaultTenantSlug))
                    {
                        throw new TenantResolutionException("Tenant slug could not be determined from host.");
                    }

                    slug = _defaultTenantSlug;
                }
                else
                {
                    throw new TenantResolutionException("Tenant slug could not be determined from host.");
                }
            }

            using var db = new CrmDbContext(_dbOptions, new NullTenantProvider());
            var tenant = await db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Slug == slug);
            if (tenant is null && isDev && !string.IsNullOrWhiteSpace(_defaultTenantSlug)
                && !string.Equals(slug, _defaultTenantSlug, StringComparison.OrdinalIgnoreCase))
            {
                slug = _defaultTenantSlug;
                tenant = await db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Slug == slug);
            }

            if (tenant is null)
            {
                if (_env.IsEnvironment("Testing") && !string.IsNullOrWhiteSpace(_defaultTenantSlug)
                    && string.Equals(slug, _defaultTenantSlug, StringComparison.OrdinalIgnoreCase))
                {
                    tenant = new Tenant
                    {
                        Id = Guid.NewGuid(),
                        Slug = slug,
                        Name = _defaultTenantName
                    };
                    await db.Tenants.AddAsync(tenant);
                    await db.SaveChangesAsync();
                }
            }

            if (tenant is null)
            {
                throw new TenantResolutionException($"Tenant '{slug}' not found.");
            }

            var context = new TenantContext(tenant.Id, tenant.Slug, tenant.Name);
            httpContext.Items[TenantContextKeys.Context] = context;
            return context;
        }

        private string? ExtractSlug(string? host)
        {
            if (string.IsNullOrWhiteSpace(host))
            {
                return null;
            }

            var devSuffix = "." + _devHostSuffix;
            if (!string.IsNullOrWhiteSpace(_devHostSuffix) && host.EndsWith(devSuffix, StringComparison.OrdinalIgnoreCase))
            {
                var sub = host[..^devSuffix.Length];
                return sub.Split('.', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            }

            if (!string.IsNullOrWhiteSpace(_baseDomain))
            {
                var suffix = "." + _baseDomain;
                if (host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    var sub = host[..^suffix.Length];
                    return sub.Split('.', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
                }
            }

            return null;
        }
    }
}
