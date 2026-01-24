namespace Crm.Web.Infrastructure
{
    using Crm.Infrastructure.Persistence;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Diagnostics.HealthChecks;
    using Microsoft.Extensions.Hosting;

    public sealed class DbMigrationsHealthCheck : IHealthCheck
    {
        private readonly CrmDbContext _db;
        private readonly IHostEnvironment _env;
        private readonly IConfiguration _config;

        public DbMigrationsHealthCheck(CrmDbContext db, IHostEnvironment env, IConfiguration config)
        {
            _db = db;
            _env = env;
            _config = config;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            if (_db.Database.IsInMemory())
            {
                return HealthCheckResult.Healthy("In-memory database; migrations not applicable.");
            }

            var autoMigrate = _config.GetValue<bool?>("Database:AutoMigrate") ?? false;
            var isDevOrTest = _env.IsDevelopment() || _env.IsEnvironment("Testing");

            try
            {
                if (!await _db.Database.CanConnectAsync(cancellationToken))
                {
                    return HealthCheckResult.Unhealthy("Database connection failed.");
                }

                var pending = await _db.Database.GetPendingMigrationsAsync(cancellationToken);
                if (pending.Any())
                {
                    var details = $"Pending migrations: {string.Join(", ", pending)}";
                    return HealthCheckResult.Unhealthy(isDevOrTest || autoMigrate
                        ? $"{details}"
                        : $"Schema is out of date. {details}");
                }

                return HealthCheckResult.Healthy("Database schema is up to date.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Database schema check failed.", ex);
            }
        }
    }
}
