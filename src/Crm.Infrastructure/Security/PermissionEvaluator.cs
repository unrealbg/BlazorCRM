namespace Crm.Infrastructure.Security
{
    using System.Reflection;
    using System.Security.Claims;
    using Crm.Application.Security;

    public sealed class PermissionEvaluator : IPermissionEvaluator
    {
        private static readonly HashSet<string> AllPermissions = typeof(Permissions)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string))
            .Select(f => (string)f.GetValue(null)!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, HashSet<string>> RolePermissions = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Admin"] = AllPermissions,
            ["Manager"] = AllPermissions,
            ["User"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        };

        public bool HasPermission(ClaimsPrincipal user, string permission)
        {
            if (string.IsNullOrWhiteSpace(permission))
            {
                return true;
            }

            if (user is null)
            {
                return false;
            }

            if (user.Claims.Any(c =>
                    (string.Equals(c.Type, "perm", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(c.Type, "permission", StringComparison.OrdinalIgnoreCase)) &&
                    string.Equals(c.Value, permission, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            foreach (var (role, permissions) in RolePermissions)
            {
                if (user.IsInRole(role) && permissions.Contains(permission))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
