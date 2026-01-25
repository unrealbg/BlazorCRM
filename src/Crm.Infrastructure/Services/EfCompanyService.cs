namespace Crm.Infrastructure.Services
{
    using System.Text;
    using Crm.Application.Services;
    using Crm.Contracts.Paging;
    using Crm.Domain.Entities;
    using Crm.Infrastructure.Persistence;
    using Microsoft.EntityFrameworkCore;

    public class EfCompanyService : ICompanyService
    {
        private readonly CrmDbContext _db;

        public EfCompanyService(CrmDbContext db)
        {
            _db = db;
        }

        public async Task<PagedResult<Crm.Application.Companies.Queries.CompanyListItem>> SearchAsync(
            PagedRequest request,
            string? industry,
            CancellationToken ct = default)
        {
            IQueryable<Company> q = _db.Companies.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var s = request.Search.Trim();
                q = q.Where(c =>
                    EF.Functions.Like(c.Name, $"%{s}%") ||
                    (c.Industry != null && EF.Functions.Like(c.Industry, $"%{s}%")) ||
                    (c.Address != null && EF.Functions.Like(c.Address, $"%{s}%"))
                );
            }

            if (!string.IsNullOrWhiteSpace(industry))
            {
                q = q.Where(c => c.Industry != null && c.Industry == industry);
            }

            var page = request.Page <= 0 ? 1 : request.Page;
            var pageSize = request.PageSize <= 0 ? 10 : Math.Min(request.PageSize, 200);
            var sort = string.IsNullOrWhiteSpace(request.SortBy) ? nameof(Company.Name) : request.SortBy;
            var desc = string.Equals(request.SortDir, "desc", StringComparison.OrdinalIgnoreCase);

            var ordered = (sort, desc) switch
            {
                (nameof(Company.Name), false) => q.OrderBy(c => c.Name).ThenBy(c => c.Id),
                (nameof(Company.Name), true) => q.OrderByDescending(c => c.Name).ThenByDescending(c => c.Id),
                (nameof(Company.Industry), false) => q.OrderBy(c => c.Industry).ThenBy(c => c.Id),
                (nameof(Company.Industry), true) => q.OrderByDescending(c => c.Industry).ThenByDescending(c => c.Id),
                (nameof(Company.CreatedAtUtc), false) => q.OrderBy(c => c.CreatedAtUtc).ThenBy(c => c.Id),
                (nameof(Company.CreatedAtUtc), true) => q.OrderByDescending(c => c.CreatedAtUtc).ThenByDescending(c => c.Id),
                _ => q.OrderBy(c => c.Name).ThenBy(c => c.Id)
            };

            var total = await ordered.CountAsync(ct);
            var items = await ordered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new Crm.Application.Companies.Queries.CompanyListItem(c.Id, c.Name, c.Industry))
                .ToListAsync(ct);

            return new PagedResult<Crm.Application.Companies.Queries.CompanyListItem>(items, total, page, pageSize);
        }

        public async Task<string[]> GetDistinctIndustriesAsync(string? search = null, CancellationToken ct = default)
        {
            IQueryable<Company> q = _db.Companies.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                q = q.Where(c =>
                    EF.Functions.Like(c.Name, $"%{s}%") ||
                    (c.Industry != null && EF.Functions.Like(c.Industry, $"%{s}%")) ||
                    (c.Address != null && EF.Functions.Like(c.Address, $"%{s}%"))
                );
            }

            return await q.Where(c => c.Industry != null && c.Industry != "")
                .Select(c => c.Industry!)
                .Distinct()
                .OrderBy(x => x)
                .ToArrayAsync(ct);
        }

        public async Task<Company> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await _db.Companies.FindAsync(new object?[] { id }, ct) ?? throw new KeyNotFoundException("Company not found");
            return entity;
        }

        public async Task<Company> UpsertAsync(Company company, CancellationToken ct = default)
        {
            if (company.Id == Guid.Empty)
            {
                company.Id = Guid.NewGuid();
                await _db.Companies.AddAsync(company, ct);
            }
            else
            {
                var existing = await _db.Companies.AsTracking().FirstOrDefaultAsync(c => c.Id == company.Id, ct);
                if (existing is null)
                {
                    await _db.Companies.AddAsync(company, ct);
                }
                else
                {
                    _db.Entry(existing).CurrentValues.SetValues(company);
                    existing.Tags = company.Tags;
                }
            }
            await _db.SaveChangesAsync(ct);

            return company;
        }

        public async Task<int> BulkAddTagAsync(IEnumerable<Guid> ids, string tag, CancellationToken ct = default)
        {
            var idList = ids.Distinct().ToList();
            var companies = await _db.Companies.Where(c => idList.Contains(c.Id)).ToListAsync(ct);
            foreach (var c in companies)
            {
                if (!c.Tags.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)))
                    c.Tags.Add(tag);
            }

            await _db.SaveChangesAsync(ct);

            return companies.Count;
        }

        public async Task<int> ImportCsvAsync(Stream csvStream, CancellationToken ct = default)
        {
            using var reader = new StreamReader(csvStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
            _ = await reader.ReadLineAsync();
            var toAdd = new List<Company>();
            while (true)
            {
                var line = await reader.ReadLineAsync();
                if (line is null) break;
                if (string.IsNullOrWhiteSpace(line)) continue;
                var cols = line.Split(',');
                var name = cols.ElementAtOrDefault(0)?.Trim() ?? string.Empty;
                var industry = cols.ElementAtOrDefault(1)?.Trim();
                var address = cols.ElementAtOrDefault(2)?.Trim();
                var tags = (cols.ElementAtOrDefault(3)?.Split('|', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>()).Select(t => t.Trim()).ToList();
                toAdd.Add(new Company { Id = Guid.NewGuid(), Name = name, Industry = industry, Address = address, Tags = tags });
            }

            if (toAdd.Count > 0)
            {
                await _db.Companies.AddRangeAsync(toAdd, ct);
                await _db.SaveChangesAsync(ct);
            }

            return toAdd.Count;
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await _db.Companies.FindAsync(new object?[] { id }, ct);
            if (entity is null)
            {
                return false;
            }

            _db.Companies.Remove(entity);
            await _db.SaveChangesAsync(ct);

            return true;
        }
    }
}
