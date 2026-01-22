namespace Crm.Infrastructure.Seeding
{
    using Crm.Application.Common.Multitenancy;
    using Crm.Domain.Entities;
    using Crm.Infrastructure.Persistence;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;

    public static class DemoDataSeeder
    {
        private sealed class DemoDataSeederLog { }

        public static async Task SeedAsync(IServiceProvider sp, CancellationToken ct = default)
        {
            var db = sp.GetRequiredService<CrmDbContext>();
            var logger = sp.GetRequiredService<ILogger<DemoDataSeederLog>>();
            var options = sp.GetRequiredService<IOptions<TenantOptions>>().Value;

            if (options.DefaultTenantId == Guid.Empty)
            {
                throw new InvalidOperationException("Demo data seeding requires Tenancy:DefaultTenantId to be configured.");
            }

            var tenantId = options.DefaultTenantId;
            var tenantName = string.IsNullOrWhiteSpace(options.DefaultTenantName) ? "Default" : options.DefaultTenantName;

            if (!await db.Tenants.AnyAsync(t => t.Id == tenantId, ct))
            {
                await db.Tenants.AddAsync(new Tenant
                {
                    Id = tenantId,
                    Name = tenantName,
                    Slug = "default"
                }, ct);
                await db.SaveChangesAsync(ct);
                logger.LogInformation("Seeded tenant {TenantId} ({TenantName}).", tenantId, tenantName);
            }
            else
            {
                logger.LogInformation("Tenant {TenantId} already exists. Skipping tenant seed.", tenantId);
            }

            var pipeline = await db.Pipelines.FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Name == "Sales", ct);
            if (pipeline is null)
            {
                pipeline = new Pipeline { Id = Guid.NewGuid(), Name = "Sales", TenantId = tenantId };
                await db.Pipelines.AddAsync(pipeline, ct);
                await db.SaveChangesAsync(ct);
                logger.LogInformation("Seeded pipeline '{PipelineName}' for tenant {TenantId}.", pipeline.Name, tenantId);
            }
            else
            {
                logger.LogInformation("Pipeline '{PipelineName}' already exists for tenant {TenantId}.", pipeline.Name, tenantId);
            }

            var stageTemplates = new[]
            {
                new { Name = "New", Order = 1 },
                new { Name = "Qualified", Order = 2 },
                new { Name = "Proposal", Order = 3 },
                new { Name = "Won", Order = 4 }
            };

            var stageAdds = new List<Stage>();
            foreach (var tmpl in stageTemplates)
            {
                var exists = await db.Stages.AnyAsync(s => s.TenantId == tenantId && s.PipelineId == pipeline.Id && s.Name == tmpl.Name, ct);
                if (!exists)
                {
                    stageAdds.Add(new Stage
                    {
                        Id = Guid.NewGuid(),
                        Name = tmpl.Name,
                        Order = tmpl.Order,
                        PipelineId = pipeline.Id,
                        TenantId = tenantId
                    });
                }
            }

            if (stageAdds.Count > 0)
            {
                await db.Stages.AddRangeAsync(stageAdds, ct);
                await db.SaveChangesAsync(ct);
                logger.LogInformation("Seeded {StageCount} stages for pipeline '{PipelineName}'.", stageAdds.Count, pipeline.Name);
            }
            else
            {
                logger.LogInformation("Stages already exist for pipeline '{PipelineName}'.", pipeline.Name);
            }

            var companies = new[]
            {
                new { Name = "Acme Corp", Industry = "Manufacturing", Tags = new List<string> { "vip" } },
                new { Name = "Globex", Industry = "Services", Tags = new List<string> { "partner" } }
            };

            var companyAdds = new List<Company>();
            foreach (var c in companies)
            {
                var exists = await db.Companies.AnyAsync(x => x.TenantId == tenantId && x.Name == c.Name, ct);
                if (!exists)
                {
                    companyAdds.Add(new Company { Id = Guid.NewGuid(), Name = c.Name, Industry = c.Industry, Tags = c.Tags, TenantId = tenantId });
                }
            }

            if (companyAdds.Count > 0)
            {
                await db.Companies.AddRangeAsync(companyAdds, ct);
                await db.SaveChangesAsync(ct);
                logger.LogInformation("Seeded {CompanyCount} companies for tenant {TenantId}.", companyAdds.Count, tenantId);
            }
            else
            {
                logger.LogInformation("Companies already exist for tenant {TenantId}.", tenantId);
            }

            var contacts = new[]
            {
                new { FirstName = "John", LastName = "Doe", Email = "john@acme.com" },
                new { FirstName = "Jane", LastName = "Smith", Email = "jane@globex.com" }
            };

            var contactAdds = new List<Contact>();
            foreach (var c in contacts)
            {
                var exists = await db.Contacts.AnyAsync(x => x.TenantId == tenantId && x.Email == c.Email, ct);
                if (!exists)
                {
                    contactAdds.Add(new Contact { Id = Guid.NewGuid(), FirstName = c.FirstName, LastName = c.LastName, Email = c.Email, TenantId = tenantId });
                }
            }

            if (contactAdds.Count > 0)
            {
                await db.Contacts.AddRangeAsync(contactAdds, ct);
                await db.SaveChangesAsync(ct);
                logger.LogInformation("Seeded {ContactCount} contacts for tenant {TenantId}.", contactAdds.Count, tenantId);
            }
            else
            {
                logger.LogInformation("Contacts already exist for tenant {TenantId}.", tenantId);
            }

            var firstStage = await db.Stages
                .Where(s => s.TenantId == tenantId && s.PipelineId == pipeline.Id)
                .OrderBy(s => s.Order)
                .FirstOrDefaultAsync(ct);

            if (firstStage is null)
            {
                throw new InvalidOperationException("No stages found for the seeded pipeline. Cannot seed deals.");
            }

            var companyLookup = await db.Companies
                .Where(c => c.TenantId == tenantId)
                .ToDictionaryAsync(c => c.Name, ct);
            var contactLookup = await db.Contacts
                .Where(c => c.TenantId == tenantId)
                .ToDictionaryAsync(c => c.Email ?? string.Empty, ct);

            var deals = new[]
            {
                new { Title = "Website redesign", Amount = 15000m, Currency = "USD", Probability = 30, Company = "Acme Corp", Contact = "john@acme.com" },
                new { Title = "Factory automation", Amount = 250000m, Currency = "USD", Probability = 20, Company = "Globex", Contact = "jane@globex.com" }
            };

            var dealAdds = new List<Deal>();
            foreach (var d in deals)
            {
                var exists = await db.Deals.AnyAsync(x => x.TenantId == tenantId && x.Title == d.Title, ct);
                if (!exists)
                {
                    companyLookup.TryGetValue(d.Company, out var company);
                    contactLookup.TryGetValue(d.Contact, out var contact);

                    dealAdds.Add(new Deal
                    {
                        Id = Guid.NewGuid(),
                        Title = d.Title,
                        Amount = d.Amount,
                        Currency = d.Currency,
                        Probability = d.Probability,
                        StageId = firstStage.Id,
                        CompanyId = company?.Id,
                        ContactId = contact?.Id,
                        TenantId = tenantId
                    });
                }
            }

            if (dealAdds.Count > 0)
            {
                await db.Deals.AddRangeAsync(dealAdds, ct);
                await db.SaveChangesAsync(ct);
                logger.LogInformation("Seeded {DealCount} deals for tenant {TenantId}.", dealAdds.Count, tenantId);
            }
            else
            {
                logger.LogInformation("Deals already exist for tenant {TenantId}.", tenantId);
            }
        }
    }
}
