namespace Crm.Application.Common.Multitenancy
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    public sealed class TenantOptions
    {
        public string DefaultTenantSlug { get; set; } = "demo";
        public string DefaultTenantName { get; set; } = "Demo";
        public string BaseDomain { get; set; } = "crm.yourdomain.com";
        public string DevHostSuffix { get; set; } = "localhost";
    }

    public static class TenantContextKeys
    {
        public const string Context = "TenantContext";
    }

    public sealed record TenantContext(Guid TenantId, string TenantSlug, string TenantName);

    public interface ITenantContextAccessor
    {
        TenantContext? Current { get; set; }
    }

    public sealed class TenantResolutionException : Exception
    {
        public TenantResolutionException(string message) : base(message) { }
    }

    public interface ITenantResolver
    {
        Task<TenantContext> ResolveAsync(HttpContext httpContext);
    }
}
