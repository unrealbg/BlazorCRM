namespace Crm.Web.Tests.Seeding
{
    using Crm.Application.Common.Multitenancy;
    using Crm.Domain.Entities;
    using Crm.Infrastructure.Multitenancy;
    using Crm.Infrastructure.Persistence;
    using Crm.Infrastructure.Seeding;
    using Microsoft.AspNetCore.Http;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    public class DemoDataSeederTests
    {
        private static ServiceProvider BuildServices(Guid tenantId)
        {
            var services = new ServiceCollection();
            services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Debug));
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.Configure<TenantOptions>(o =>
            {
                o.DefaultTenantId = tenantId;
                o.DefaultTenantName = "Default";
            });
            services.AddScoped<ITenantResolver, DefaultTenantResolver>();
            services.AddScoped<ITenantProvider, HttpTenantProvider>();
            services.AddDbContext<CrmDbContext>(o => o.UseInMemoryDatabase($"demo-seed-{Guid.NewGuid()}"));
            return services.BuildServiceProvider();
        }

        [Fact]
        public async Task Seeding_Twice_Does_Not_Duplicate_Data()
        {
            var tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            using var sp = BuildServices(tenantId);

            await DemoDataSeeder.SeedAsync(sp);
            await DemoDataSeeder.SeedAsync(sp);

            var db = sp.GetRequiredService<CrmDbContext>();

            Assert.Equal(1, await db.Pipelines.CountAsync());
            Assert.Equal(4, await db.Stages.CountAsync());
            Assert.Equal(2, await db.Companies.CountAsync());
            Assert.Equal(2, await db.Contacts.CountAsync());
            Assert.Equal(2, await db.Deals.CountAsync());
        }

        [Fact]
        public async Task Seeded_Entities_Share_Same_TenantId()
        {
            var tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            using var sp = BuildServices(tenantId);

            await DemoDataSeeder.SeedAsync(sp);

            var db = sp.GetRequiredService<CrmDbContext>();

            Assert.All(await db.Pipelines.Select(p => p.TenantId).ToListAsync(), id => Assert.Equal(tenantId, id));
            Assert.All(await db.Stages.Select(s => s.TenantId).ToListAsync(), id => Assert.Equal(tenantId, id));
            Assert.All(await db.Companies.Select(c => c.TenantId).ToListAsync(), id => Assert.Equal(tenantId, id));
            Assert.All(await db.Contacts.Select(c => c.TenantId).ToListAsync(), id => Assert.Equal(tenantId, id));
            Assert.All(await db.Deals.Select(d => d.TenantId).ToListAsync(), id => Assert.Equal(tenantId, id));
        }
    }
}
