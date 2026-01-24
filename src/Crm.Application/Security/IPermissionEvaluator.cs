namespace Crm.Application.Security
{
    using System.Security.Claims;

    public interface IPermissionEvaluator
    {
        bool HasPermission(ClaimsPrincipal user, string permission);
    }
}
