namespace Crm.Infrastructure.Identity
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Crm.Application.Common.Multitenancy;
    using Microsoft.AspNetCore.Identity;

    public sealed class IdentityUserTenantMembership : IUserTenantMembership
    {
        private readonly UserManager<IdentityUser> _users;

        public IdentityUserTenantMembership(UserManager<IdentityUser> users)
        {
            _users = users;
        }

        public async Task<bool> IsMemberAsync(string userId, Guid tenantId, CancellationToken ct = default)
        {
            var user = await _users.FindByIdAsync(userId);
            if (user is null)
            {
                return false;
            }

            var claims = await _users.GetClaimsAsync(user);
            foreach (var claim in claims)
            {
                if (!string.Equals(claim.Type, "tenant", StringComparison.Ordinal))
                {
                    continue;
                }

                if (Guid.TryParse(claim.Value, out var claimedTenantId) && claimedTenantId == tenantId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
