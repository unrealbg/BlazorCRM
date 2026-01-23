namespace Crm.Infrastructure.Multitenancy
{
    using Crm.Application.Common.Multitenancy;
    using Microsoft.AspNetCore.Http;

    public sealed class HttpTenantProvider : ITenantProvider
    {
        private readonly ITenantResolver _resolver;
        private readonly IHttpContextAccessor _ctx;

        public HttpTenantProvider(ITenantResolver resolver, IHttpContextAccessor ctx)
        {
            _resolver = resolver;
            _ctx = ctx;
        }

        public Guid TenantId
        {
            get
            {
                if (_ctx.HttpContext?.Items.TryGetValue(TenantContextKeys.Resolution, out var cached) == true
                    && cached is TenantResolution cachedResolution
                    && cachedResolution.IsResolved)
                {
                    return cachedResolution.TenantId;
                }

                var resolution = _resolver.Resolve();
                if (!resolution.IsResolved)
                {
                    throw new InvalidOperationException(resolution.FailureReason ?? "Tenant could not be resolved.");
                }

                return resolution.TenantId;
            }
        }
    }
}
