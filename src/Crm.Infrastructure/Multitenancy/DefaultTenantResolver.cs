namespace Crm.Infrastructure.Multitenancy
{
    using Crm.Application.Common.Multitenancy;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Resolves the current tenant from the authenticated principal, with a deterministic default fallback
    /// for unauthenticated flows (e.g., login, seeding).
    /// </summary>
    public sealed class DefaultTenantResolver : ITenantResolver
    {
        private readonly IHttpContextAccessor _ctx;
        private readonly IOptions<TenantOptions> _options;

        public DefaultTenantResolver(IHttpContextAccessor ctx, IOptions<TenantOptions> options)
        {
            _ctx = ctx;
            _options = options;
        }

        public TenantResolution Resolve()
        {
            var http = _ctx.HttpContext;
            if (http?.User?.Identity?.IsAuthenticated == true)
            {
                var claim = http.User.FindFirst("tenant")?.Value;
                if (string.IsNullOrWhiteSpace(claim))
                {
                    return new TenantResolution(Guid.Empty, null, false, "Authenticated user is missing tenant claim.");
                }

                if (!Guid.TryParse(claim, out var tenantId))
                {
                    return new TenantResolution(Guid.Empty, null, false, "Tenant claim is not a valid GUID.");
                }

                var name = http.User.FindFirst("tenant_name")?.Value;
                return new TenantResolution(tenantId, name, true, null);
            }

            var defaultId = _options.Value.DefaultTenantId;
            if (defaultId == Guid.Empty)
            {
                return new TenantResolution(Guid.Empty, null, false, "Default tenant is not configured.");
            }

            return new TenantResolution(defaultId, _options.Value.DefaultTenantName, true, null);
        }
    }
}
