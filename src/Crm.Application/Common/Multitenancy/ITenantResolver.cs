namespace Crm.Application.Common.Multitenancy
{
    public sealed class TenantOptions
    {
        public string DefaultTenantSlug { get; set; } = "demo";
        public string DefaultTenantName { get; set; } = "Demo";
        public string[] BaseDomains { get; set; } = Array.Empty<string>();
    }

    public static class TenantContextKeys
    {
        public const string Resolution = "tenant:resolution";
    }

    public sealed record TenantResolution(Guid TenantId, string? TenantName, string? TenantSlug, bool IsResolved, string? FailureReason = null);

    public interface ITenantResolver
    {
        TenantResolution Resolve();
    }
}
