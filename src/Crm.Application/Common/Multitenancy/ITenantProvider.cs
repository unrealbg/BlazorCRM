namespace Crm.Application.Common.Multitenancy
{
    public interface ITenantProvider
    {
        Guid TenantId { get; }
    }
}
