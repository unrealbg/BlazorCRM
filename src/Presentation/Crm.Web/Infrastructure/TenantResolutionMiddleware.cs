namespace Crm.Web.Infrastructure
{
    using Crm.Application.Common.Multitenancy;
    using Microsoft.AspNetCore.Authorization;

    public sealed class TenantResolutionMiddleware
    {
        private readonly RequestDelegate _next;

        public TenantResolutionMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext ctx, ITenantResolver resolver, ILogger<TenantResolutionMiddleware> logger)
        {
            var endpoint = ctx.GetEndpoint();
            if (endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() is not null)
            {
                await _next(ctx);
                return;
            }

            if (ctx.User?.Identity?.IsAuthenticated == true)
            {
                var resolution = resolver.Resolve();
                if (!resolution.IsResolved)
                {
                    logger.LogWarning("Tenant resolution failed: {Reason}", resolution.FailureReason);
                    ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await ctx.Response.WriteAsJsonAsync(new { error = "tenant_unresolved" });
                    return;
                }
            }

            await _next(ctx);
        }
    }
}
