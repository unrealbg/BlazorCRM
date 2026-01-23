namespace Crm.Infrastructure.Multitenancy
{
    using Crm.Application.Common.Multitenancy;
    using System.Linq;
    using Crm.Infrastructure.Persistence;
    using Microsoft.AspNetCore.Http;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Options;

    public sealed class SubdomainTenantResolver : ITenantResolver
    {
        private readonly IHttpContextAccessor _ctx;
        private readonly DbContextOptions<CrmDbContext> _dbOptions;
        private readonly TenantOptions _options;
        private readonly IHostEnvironment _env;

        private sealed class NullTenantProvider : ITenantProvider
        {
            public Guid TenantId => Guid.Empty;
        }

        public SubdomainTenantResolver(
            IHttpContextAccessor ctx,
            DbContextOptions<CrmDbContext> dbOptions,
            IOptions<TenantOptions> options,
            IHostEnvironment env)
        {
            _ctx = ctx;
            _dbOptions = dbOptions;
            _options = options.Value;
            _env = env;
        }

        public TenantResolution Resolve()
        {
            var http = _ctx.HttpContext;
            if (http?.Items.TryGetValue(TenantContextKeys.Resolution, out var cached) == true && cached is TenantResolution cachedResolution)
            {
                return cachedResolution;
            }

            string? slug;

            if (http is null)
            {
                slug = _options.DefaultTenantSlug;
            }
            else
            {
                var host = http.Request.Host.Host;
                slug = ExtractSlug(host, _options.BaseDomains);

                if (string.IsNullOrWhiteSpace(slug) && _env.IsDevelopment())
                {
                    slug = _options.DefaultTenantSlug;
                }
            }

            if (string.IsNullOrWhiteSpace(slug))
            {
                return Cache(http, new TenantResolution(Guid.Empty, null, null, false, "Tenant slug could not be determined from host."));
            }

            using var db = new CrmDbContext(_dbOptions, new NullTenantProvider());
            var tenant = db.Tenants.AsNoTracking().FirstOrDefault(t => t.Slug == slug);
            if (tenant is null)
            {
                return Cache(http, new TenantResolution(Guid.Empty, null, slug, false, $"Tenant '{slug}' not found."));
            }

            return Cache(http, new TenantResolution(tenant.Id, tenant.Name, tenant.Slug, true, null));
        }

        private static TenantResolution Cache(HttpContext? http, TenantResolution resolution)
        {
            if (http is not null)
            {
                http.Items[TenantContextKeys.Resolution] = resolution;
            }

            return resolution;
        }

        private static string? ExtractSlug(string? host, string[] baseDomains)
        {
            if (string.IsNullOrWhiteSpace(host))
            {
                return null;
            }

            if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                || host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
                || host.Equals("::1", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase))
            {
                var sub = host[..^".localhost".Length];
                return sub.Split('.', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
            }

            if (baseDomains is not null && baseDomains.Length > 0)
            {
                foreach (var domain in baseDomains.Where(d => !string.IsNullOrWhiteSpace(d)))
                {
                    if (host.Equals(domain, StringComparison.OrdinalIgnoreCase))
                    {
                        return null;
                    }

                    var suffix = "." + domain;
                    if (host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    {
                        var sub = host[..^suffix.Length];
                        return sub.Split('.', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
                    }
                }
            }

            if (host.Contains('.'))
            {
                return host.Split('.', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            }

            return null;
        }
    }
}
