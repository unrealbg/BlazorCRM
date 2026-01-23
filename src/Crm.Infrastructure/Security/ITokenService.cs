namespace Crm.Infrastructure.Security
{
    using Crm.Contracts.Auth;

    public interface ITokenService
    {
        LoginResponse CreateToken(string userId, string userName, Guid tenantId, string tenantSlug);
    }
}
