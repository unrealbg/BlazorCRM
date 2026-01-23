namespace Crm.Infrastructure.Multitenancy
{
    using Crm.Application.Common.Multitenancy;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// Resolves the current tenant from the authenticated principal, with a deterministic default fallback
    /// for unauthenticated flows (e.g., login, seeding).
    /// </summary>
    public sealed class DefaultTenantResolver : ITenantResolver
    {
        private readonly IHttpContextAccessor _ctx;
        public DefaultTenantResolver(IHttpContextAccessor ctx) => _ctx = ctx;

        public TenantResolution Resolve()
        {
            var http = _ctx.HttpContext;
            var claim = http?.User?.FindFirst("tenant")?.Value;
            if (Guid.TryParse(claim, out var tenantId))
            {
                var name = http?.User?.FindFirst("tenant_name")?.Value;
                var slug = http?.User?.FindFirst("tenant_slug")?.Value;
                return new TenantResolution(tenantId, name, slug, true, null);
            }

            return new TenantResolution(Guid.Empty, null, null, false, "Tenant could not be resolved from claims.");
        }
    }
}
