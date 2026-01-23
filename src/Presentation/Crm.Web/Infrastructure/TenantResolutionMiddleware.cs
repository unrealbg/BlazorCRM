namespace Crm.Web.Infrastructure
{
    using Crm.Application.Common.Multitenancy;

    public sealed class TenantResolutionMiddleware
    {
        private readonly RequestDelegate _next;

        public TenantResolutionMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext ctx, ITenantResolver resolver, ILogger<TenantResolutionMiddleware> logger)
        {
            var resolution = resolver.Resolve();
            if (!resolution.IsResolved)
            {
                logger.LogWarning("Tenant resolution failed for host {Host}: {Reason}", ctx.Request.Host.Host, resolution.FailureReason);
                ctx.Response.StatusCode = StatusCodes.Status404NotFound;
                await ctx.Response.WriteAsJsonAsync(new { error = "tenant_unresolved" });
                return;
            }

            var cacheKey = resolution.TenantSlug ?? resolution.TenantId.ToString("N");
            ctx.Request.Headers["X-Tenant"] = cacheKey;

            await _next(ctx);
        }
    }
}
