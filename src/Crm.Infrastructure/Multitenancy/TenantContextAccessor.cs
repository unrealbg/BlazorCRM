namespace Crm.Infrastructure.Multitenancy
{
    using Crm.Application.Common.Multitenancy;

    public sealed class TenantContextAccessor : ITenantContextAccessor
    {
        public TenantContext? Current { get; set; }
    }
}
