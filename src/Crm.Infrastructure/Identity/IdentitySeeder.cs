namespace Crm.Infrastructure.Identity
{
    using Crm.Application.Common.Multitenancy;
    using Crm.Domain.Entities;
    using Crm.Infrastructure.Persistence;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    public static class IdentitySeeder
    {
        private sealed class IdentitySeederLog { }
        private sealed class SeedTenantProvider : ITenantProvider
        {
            public Guid TenantId => Guid.Empty;
        }

        public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
        {
            using var scope = services.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<IdentitySeederLog>>();
            var options = scope.ServiceProvider.GetRequiredService<IOptions<TenantOptions>>().Value;
            var db = scope.ServiceProvider.GetRequiredService<CrmDbContext>();

            var tenantSlug = string.IsNullOrWhiteSpace(options.DefaultTenantSlug) ? "demo" : options.DefaultTenantSlug;
            var tenantName = string.IsNullOrWhiteSpace(options.DefaultTenantName) ? "Demo" : options.DefaultTenantName;

            var tenant = await db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Slug == tenantSlug);
            if (tenant is null)
            {
                tenant = new Tenant
                {
                    Id = Guid.NewGuid(),
                    Name = tenantName,
                    Slug = tenantSlug
                };
                var dbOptions = scope.ServiceProvider.GetRequiredService<DbContextOptions<CrmDbContext>>();
                await using var seedDb = new CrmDbContext(dbOptions, new SeedTenantProvider());
                await seedDb.Tenants.AddAsync(tenant);
                await seedDb.SaveChangesAsync();
                logger.LogInformation("Seeded tenant {TenantId} ({TenantName}) for identity.", tenant.Id, tenantName);
            }
            else
            {
                logger.LogInformation("Tenant {TenantId} already exists. Skipping tenant seed for identity.", tenant.Id);
            }

            var rolesCfg = configuration.GetSection("Seed:Roles").GetChildren().Select(c => c.Value!).Where(v => !string.IsNullOrWhiteSpace(v));
            var roles = rolesCfg
                .Concat(new[] { "Admin", "Manager", "User" })
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                    logger.LogInformation("Created role '{Role}'.", role);
                }
                else
                {
                    logger.LogInformation("Role '{Role}' already exists.", role);
                }
            }

            var adminEmail = configuration["Seed:AdminEmail"] ?? "admin@local";
            var adminPassword = configuration["Seed:AdminPassword"] ?? "Admin123$";
            var adminRolesCfg = configuration.GetSection("Seed:AdminRoles").Get<string[]>() ?? new[] { "Admin" };

            var admin = await userManager.FindByEmailAsync(adminEmail);
            if (admin is null)
            {
                admin = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(admin, adminPassword);
                if (!result.Succeeded)
                {
                    var error = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to create admin user: {error}");
                }

                logger.LogInformation("Created admin user '{AdminEmail}'.", adminEmail);
            }
            else
            {
                logger.LogInformation("Admin user '{AdminEmail}' already exists.", adminEmail);
            }

            foreach (var role in adminRolesCfg)
            {
                if (!await userManager.IsInRoleAsync(admin, role))
                {
                    await userManager.AddToRoleAsync(admin, role);
                    logger.LogInformation("Added role '{Role}' to admin user.", role);
                }
                else
                {
                    logger.LogInformation("Admin user already has role '{Role}'.", role);
                }
            }

            var claims = await userManager.GetClaimsAsync(admin);
            if (!claims.Any(c => c.Type == "tenant" && c.Value == tenant.Id.ToString()))
            {
                await userManager.AddClaimAsync(admin, new System.Security.Claims.Claim("tenant", tenant.Id.ToString()));
            }
            if (!claims.Any(c => c.Type == "tenant_slug" && c.Value == tenant.Slug))
            {
                await userManager.AddClaimAsync(admin, new System.Security.Claims.Claim("tenant_slug", tenant.Slug));
            }
        }
    }
}
