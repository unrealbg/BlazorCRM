namespace Crm.Application.Common.Behaviors
{
    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
    public sealed class RequiresPermissionAttribute : System.Attribute
    {
        public RequiresPermissionAttribute(string policy) => Policy = policy;

        public string Policy { get; }
    }
}
