namespace Crm.Web.Tests.Performance
{
    using Crm.Application.Common.Multitenancy;
    using Crm.Contracts.Paging;
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
            var page1 = await svc.SearchAsync(new PagedRequest
            {
                Page = 1,
                PageSize = 10,
                SortBy = nameof(Crm.Domain.Entities.Company.Name),
                SortDir = "asc"
            }, null);
            var page2 = await svc.SearchAsync(new PagedRequest
            {
                Page = 2,
                PageSize = 10,
                SortBy = nameof(Crm.Domain.Entities.Company.Name),
                SortDir = "asc"
            }, null);

            Assert.Equal(25, page1.TotalCount);
            Assert.Equal(10, page1.Items.Count);
            Assert.Equal(10, page2.Items.Count);
            Assert.DoesNotContain(page1.Items[0].Id, page2.Items.Select(x => x.Id));
            Assert.True(string.Compare(page1.Items[0].Name, page1.Items[1].Name, StringComparison.Ordinal) <= 0);
        }

        [Fact]
        public async Task Company_Paging_Is_Deterministic_With_Ties()
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
                    Id = CreateSequentialGuid(i),
                    TenantId = tenantId,
                    Name = "Same Name",
                    Industry = "SaaS",
                    Address = "Address",
                    CreatedAtUtc = new DateTime(2025, 1, 1)
                });
            }

            await db.SaveChangesAsync();

            var svc = new EfCompanyService(db);
            var request = new PagedRequest
            {
                Page = 1,
                PageSize = 10,
                SortBy = nameof(Crm.Domain.Entities.Company.Name),
                SortDir = "asc"
            };

            var page1 = await svc.SearchAsync(request, null);
            var page2 = await svc.SearchAsync(request with { Page = 2 }, null);
            var repeat = await svc.SearchAsync(request, null);

            Assert.Equal(25, page1.TotalCount);
            Assert.Equal(10, page1.Items.Count);
            Assert.Equal(10, page2.Items.Count);
            Assert.Empty(page1.Items.Select(x => x.Id).Intersect(page2.Items.Select(x => x.Id)));
            Assert.Equal(page1.Items.Select(x => x.Id), repeat.Items.Select(x => x.Id));
        }

        [Fact]
        public async Task Company_Sort_Direction_Works()
        {
            var tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var options = new DbContextOptionsBuilder<CrmDbContext>()
                .UseInMemoryDatabase($"crm-test-{Guid.NewGuid()}")
                .Options;

            await using var db = new CrmDbContext(options, new TestTenantProvider { TenantId = tenantId });
            db.Companies.Add(new Crm.Domain.Entities.Company { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Alpha" });
            db.Companies.Add(new Crm.Domain.Entities.Company { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Zulu" });
            await db.SaveChangesAsync();

            var svc = new EfCompanyService(db);
            var asc = await svc.SearchAsync(new PagedRequest
            {
                Page = 1,
                PageSize = 10,
                SortBy = nameof(Crm.Domain.Entities.Company.Name),
                SortDir = "asc"
            }, null);

            var desc = await svc.SearchAsync(new PagedRequest
            {
                Page = 1,
                PageSize = 10,
                SortBy = nameof(Crm.Domain.Entities.Company.Name),
                SortDir = "desc"
            }, null);

            Assert.NotEqual(asc.Items.First().Id, desc.Items.First().Id);
            Assert.Equal("Alpha", asc.Items.First().Name);
            Assert.Equal("Zulu", desc.Items.First().Name);
        }

        private static Guid CreateSequentialGuid(int value)
        {
            var bytes = new byte[16];
            BitConverter.GetBytes(value).CopyTo(bytes, 12);
            return new Guid(bytes);
        }
    }
}
