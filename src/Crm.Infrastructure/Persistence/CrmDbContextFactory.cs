namespace Crm.Infrastructure.Persistence
{
    using Crm.Application.Common.Multitenancy;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Design;

    internal sealed class NullTenantProvider : ITenantProvider
    {
        public Guid TenantId => Guid.Empty;
    }

    public class CrmDbContextFactory : IDesignTimeDbContextFactory<CrmDbContext>
    {
        public CrmDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<CrmDbContext>();
            var connectionString = Environment.GetEnvironmentVariable("CRM_DB_CONNECTION")
                ?? "Host=localhost;Port=5432;Database=blazor_crm;Username=postgres;Password=postgres";

            optionsBuilder.UseNpgsql(connectionString);
            return new CrmDbContext(optionsBuilder.Options, new NullTenantProvider());
        }
    }
}
