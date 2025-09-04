namespace Crm.Infrastructure.Identity
{
    using Microsoft.AspNetCore.Identity;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    public static class IdentitySeeder
    {
        public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
        {
            using var scope = services.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

            var rolesCfg = configuration.GetSection("Seed:Roles").GetChildren().Select(c => c.Value!).Where(v => !string.IsNullOrWhiteSpace(v));
            var roles = rolesCfg
                .Concat(new[] { "Admin", "Manager", "User" })
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
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
            }

            foreach (var role in adminRolesCfg)
            {
                if (!await userManager.IsInRoleAsync(admin, role))
                {
                    await userManager.AddToRoleAsync(admin, role);
                }
            }
        }
    }
}
