namespace Crm.Infrastructure.Multitenancy
{
    using Crm.Application.Common.Multitenancy;

    public sealed class HttpTenantProvider : ITenantProvider
    {
        private readonly ITenantResolver _resolver;
        public HttpTenantProvider(ITenantResolver resolver) => _resolver = resolver;

        public Guid TenantId
        {
            get
            {
                var resolution = _resolver.Resolve();
                if (!resolution.IsResolved)
                {
                    throw new InvalidOperationException(resolution.FailureReason ?? "Tenant could not be resolved.");
                }

                return resolution.TenantId;
            }
        }
    }
}
