namespace Crm.Infrastructure.Multitenancy
{
    using Crm.Application.Common.Multitenancy;
    using Microsoft.AspNetCore.Http;

    public sealed class HttpTenantProvider : ITenantProvider
    {
        private readonly IHttpContextAccessor _ctx;
        public HttpTenantProvider(IHttpContextAccessor ctx) => _ctx = ctx;

        public Guid TenantId
            => Guid.TryParse(_ctx.HttpContext?.User?.FindFirst("tenant")?.Value, out var id) ? id : Guid.Empty;
    }
}
