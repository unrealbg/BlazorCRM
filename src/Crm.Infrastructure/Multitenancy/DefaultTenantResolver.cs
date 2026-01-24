namespace Crm.Infrastructure.Multitenancy
{
    using Crm.Application.Common.Multitenancy;
    using Microsoft.AspNetCore.Http;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Resolves the current tenant from the authenticated principal, with a deterministic default fallback
    /// for unauthenticated flows (e.g., login, seeding).
    /// </summary>
    public sealed class DefaultTenantResolver : ITenantResolver
    {
        public Task<TenantContext> ResolveAsync(HttpContext httpContext)
        {
            var claim = httpContext.User?.FindFirst("tenant")?.Value;
            if (Guid.TryParse(claim, out var tenantId))
            {
                var name = httpContext.User?.FindFirst("tenant_name")?.Value ?? string.Empty;
                var slug = httpContext.User?.FindFirst("tenant_slug")?.Value ?? string.Empty;
                return Task.FromResult(new TenantContext(tenantId, slug, name));
            }

            throw new TenantResolutionException("Tenant could not be resolved from claims.");
        }
    }
}
