namespace Crm.Web.Tests.Performance
{
    using Crm.Application.Common.Multitenancy;
    using Crm.Infrastructure.Persistence;
    using Crm.Infrastructure.Services;
    using Microsoft.EntityFrameworkCore;

    public class PagingTests
    {
        private sealed class TestTenantProvider : ITenantProvider
        {
            public Guid TenantId { get; set; }
        }

        [Fact]
        public async Task Company_Search_Paging_Is_Stable()
        {
            var tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var options = new DbContextOptionsBuilder<CrmDbContext>()
                .UseInMemoryDatabase($"crm-test-{Guid.NewGuid()}")
                .Options;

            await using var db = new CrmDbContext(options, new TestTenantProvider { TenantId = tenantId });

            for (var i = 0; i < 25; i++)
            {
                db.Companies.Add(new Crm.Domain.Entities.Company
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    Name = $"Company {i:D2}",
                    Industry = i % 2 == 0 ? "SaaS" : "Retail",
                    Address = "Address",
                    CreatedAtUtc = new DateTime(2025, 1, 1).AddDays(i)
                });
            }

            await db.SaveChangesAsync();

            var svc = new EfCompanyService(db);
            var page1 = await svc.SearchAsync(null, null, nameof(Crm.Domain.Entities.Company.Name), true, page: 1, pageSize: 10);
            var page2 = await svc.SearchAsync(null, null, nameof(Crm.Domain.Entities.Company.Name), true, page: 2, pageSize: 10);

            Assert.Equal(25, page1.Total);
            Assert.Equal(10, page1.Items.Count);
            Assert.Equal(10, page2.Items.Count);
            Assert.DoesNotContain(page1.Items[0].Id, page2.Items.Select(x => x.Id));
            Assert.True(string.Compare(page1.Items[0].Name, page1.Items[1].Name, StringComparison.Ordinal) <= 0);
        }
    }
}
