namespace Crm.Web.Infrastructure
{
    public sealed class PermissionDeniedException : Exception
    {
        public PermissionDeniedException(string? policy, bool isAuthenticated)
            : base("Permission denied.")
        {
            Policy = policy;
            IsAuthenticated = isAuthenticated;
        }

        public string? Policy { get; }

        public bool IsAuthenticated { get; }
    }
}