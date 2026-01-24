namespace Crm.Web.Infrastructure
{
    using Crm.Application.Common.Multitenancy;

    public sealed class TenantResolutionMiddleware
    {
        private readonly RequestDelegate _next;

        public TenantResolutionMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext ctx, ITenantResolver resolver, ITenantContextAccessor accessor, ILogger<TenantResolutionMiddleware> logger)
        {
            TenantContext tenantContext;
            try
            {
                tenantContext = await resolver.ResolveAsync(ctx);
            }
            catch (TenantResolutionException ex)
            {
                logger.LogWarning("Tenant resolution failed for host {Host}: {Reason}", ctx.Request.Host.Host, ex.Message);
                ctx.Response.StatusCode = StatusCodes.Status404NotFound;
                await ctx.Response.WriteAsJsonAsync(new { error = "tenant_unresolved" });
                return;
            }

            ctx.Items[TenantContextKeys.Context] = tenantContext;
            accessor.Current = tenantContext;

            await _next(ctx);
        }
    }
}
