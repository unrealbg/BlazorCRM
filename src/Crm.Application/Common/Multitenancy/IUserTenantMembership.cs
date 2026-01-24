namespace Crm.Application.Common.Multitenancy
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IUserTenantMembership
    {
        Task<bool> IsMemberAsync(string userId, Guid tenantId, CancellationToken ct = default);
    }
}
