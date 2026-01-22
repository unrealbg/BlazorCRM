namespace Crm.Application.Common.Multitenancy
{
    public sealed class TenantOptions
    {
        public Guid DefaultTenantId { get; set; } = Guid.Empty;
        public string DefaultTenantName { get; set; } = "Default";
    }

    public sealed record TenantResolution(Guid TenantId, string? TenantName, bool IsResolved, string? FailureReason = null);

    public interface ITenantResolver
    {
        TenantResolution Resolve();
    }
}
