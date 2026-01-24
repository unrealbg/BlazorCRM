namespace Crm.Infrastructure.Multitenancy
{
    using Crm.Application.Common.Multitenancy;
    using Microsoft.AspNetCore.Http;
    using System;

    public sealed class HttpTenantProvider : ITenantProvider
    {
        private readonly ITenantResolver _resolver;
        private readonly IHttpContextAccessor _ctx;
        private readonly ITenantContextAccessor _accessor;

        public HttpTenantProvider(ITenantResolver resolver, IHttpContextAccessor ctx, ITenantContextAccessor accessor)
        {
            _resolver = resolver;
            _ctx = ctx;
            _accessor = accessor;
        }

        public Guid TenantId
        {
            get
            {
                if (_accessor.Current is not null)
                {
                    return _accessor.Current.TenantId;
                }

                if (_ctx.HttpContext?.Items.TryGetValue(TenantContextKeys.Context, out var cached) == true
                    && cached is TenantContext cachedContext)
                {
                    _accessor.Current = cachedContext;
                    return cachedContext.TenantId;
                }

                if (_ctx.HttpContext is null)
                {
                    throw new InvalidOperationException("Tenant context is not available.");
                }

                var resolved = _resolver.ResolveAsync(_ctx.HttpContext).GetAwaiter().GetResult();
                _accessor.Current = resolved;

                return resolved.TenantId;
            }
        }
    }
}
