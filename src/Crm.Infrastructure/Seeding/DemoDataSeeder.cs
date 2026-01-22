namespace Crm.Infrastructure.Seeding
{
    using Crm.Application.Common.Multitenancy;
    using Crm.Domain.Entities;
    using Crm.Infrastructure.Persistence;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;

    public static class DemoDataSeeder
    {
        public static async Task SeedAsync(IServiceProvider sp)
        {
            var db = sp.GetRequiredService<CrmDbContext>();
            var tenantResolver = sp.GetRequiredService<ITenantResolver>();
            var resolution = tenantResolver.Resolve();
            if (!resolution.IsResolved)
            {
                throw new InvalidOperationException(resolution.FailureReason ?? "Demo data seeding requires a resolved tenant.");
            }

            var tenantId = resolution.TenantId;
            var tenantName = string.IsNullOrWhiteSpace(resolution.TenantName) ? "Default" : resolution.TenantName;

            if (!await db.Tenants.AnyAsync(t => t.Id == tenantId))
            {
                await db.Tenants.AddAsync(new Tenant
                {
                    Id = tenantId,
                    Name = tenantName,
                    Slug = "default"
                });
                await db.SaveChangesAsync();
            }

            // Ensure at least one pipeline with stages exists
            if (!await db.Pipelines.AnyAsync())
            {
                var pipeline = new Pipeline { Id = Guid.NewGuid(), Name = "Sales", TenantId = tenantId };
                var stages = new[]
                                 {
                                     new Stage { Id = Guid.NewGuid(), Name = "New", Order = 1, PipelineId = pipeline.Id, TenantId = tenantId },
                                     new Stage { Id = Guid.NewGuid(), Name = "Qualified", Order = 2, PipelineId = pipeline.Id, TenantId = tenantId },
                                     new Stage { Id = Guid.NewGuid(), Name = "Proposal", Order = 3, PipelineId = pipeline.Id, TenantId = tenantId },
                                     new Stage { Id = Guid.NewGuid(), Name = "Won", Order = 4, PipelineId = pipeline.Id, TenantId = tenantId },
                                 };
                await db.Pipelines.AddAsync(pipeline);
                await db.Stages.AddRangeAsync(stages);
                await db.SaveChangesAsync();
            }

            if (!await db.Companies.AnyAsync())
            {
                var acme = new Company { Id = Guid.NewGuid(), Name = "Acme Corp", Industry = "Manufacturing", Tags = new() { "vip" }, TenantId = tenantId };
                var globex = new Company { Id = Guid.NewGuid(), Name = "Globex", Industry = "Services", Tags = new() { "partner" }, TenantId = tenantId };
                await db.Companies.AddRangeAsync(acme, globex);
                await db.SaveChangesAsync();
            }

            if (!await db.Contacts.AnyAsync())
            {
                var contacts = new[]
                                   {
                                       new Contact { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe", Email = "john@acme.com", TenantId = tenantId },
                                       new Contact { Id = Guid.NewGuid(), FirstName = "Jane", LastName = "Smith", Email = "jane@globex.com", TenantId = tenantId },
                                   };
                await db.Contacts.AddRangeAsync(contacts);
                await db.SaveChangesAsync();
            }

            if (!await db.Deals.AnyAsync())
            {
                var firstStageId = await db.Stages.OrderBy(s => s.Order).Select(s => s.Id).FirstOrDefaultAsync();
                if (firstStageId == Guid.Empty)
                {
                    // If no stages exist (edge case), create a minimal pipeline and stage
                    var pipeline = new Pipeline { Id = Guid.NewGuid(), Name = "Default", TenantId = tenantId };
                    var stage = new Stage { Id = Guid.NewGuid(), Name = "New", Order = 1, PipelineId = pipeline.Id, TenantId = tenantId };
                    await db.Pipelines.AddAsync(pipeline);
                    await db.Stages.AddAsync(stage);
                    await db.SaveChangesAsync();
                    firstStageId = stage.Id;
                }

                var deals = new[]
                                {
                                    new Deal { Id = Guid.NewGuid(), Title = "Website redesign", Amount = 15000, Currency = "USD", Probability = 30, StageId = firstStageId, TenantId = tenantId },
                                    new Deal { Id = Guid.NewGuid(), Title = "Factory automation", Amount = 250000, Currency = "USD", Probability = 20, StageId = firstStageId, TenantId = tenantId }
                                };
                await db.Deals.AddRangeAsync(deals);
                await db.SaveChangesAsync();
            }
        }
    }
}
