namespace Crm.Infrastructure.Services
{
    using System.Collections.Concurrent;
    using System.Globalization;

    using Crm.Application.Services;
    using Crm.Contracts.Paging;
    using Crm.Domain.Entities;

    public class InMemoryCompanyService : ICompanyService
    {
        private readonly ConcurrentDictionary<Guid, Company> _store = new();

        public Task<PagedResult<Crm.Application.Companies.Queries.CompanyListItem>> SearchAsync(
            PagedRequest request,
            string? industry,
            CancellationToken ct = default)
        {
            IEnumerable<Company> result = _store.Values;
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var s = request.Search.Trim();
                result = result.Where(c => c.Name.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                                           (c.Industry?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
                                           (c.Address?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
                                           c.Tags.Any(t => t.Contains(s, StringComparison.OrdinalIgnoreCase)));
            }

            if (!string.IsNullOrWhiteSpace(industry))
            {
                result = result.Where(c => string.Equals(c.Industry, industry, StringComparison.OrdinalIgnoreCase));
            }

            var page = request.Page <= 0 ? 1 : request.Page;
            var pageSize = request.PageSize <= 0 ? 10 : Math.Min(request.PageSize, 200);
            var sort = string.IsNullOrWhiteSpace(request.SortBy) ? nameof(Company.Name) : request.SortBy;
            var desc = string.Equals(request.SortDir, "desc", StringComparison.OrdinalIgnoreCase);

            var ordered = (sort, desc) switch
            {
                (nameof(Company.Name), false) => result.OrderBy(c => c.Name).ThenBy(c => c.Id),
                (nameof(Company.Name), true) => result.OrderByDescending(c => c.Name).ThenByDescending(c => c.Id),
                (nameof(Company.Industry), false) => result.OrderBy(c => c.Industry).ThenBy(c => c.Id),
                (nameof(Company.Industry), true) => result.OrderByDescending(c => c.Industry).ThenByDescending(c => c.Id),
                (nameof(Company.CreatedAtUtc), false) => result.OrderBy(c => c.CreatedAtUtc).ThenBy(c => c.Id),
                (nameof(Company.CreatedAtUtc), true) => result.OrderByDescending(c => c.CreatedAtUtc).ThenByDescending(c => c.Id),
                _ => result.OrderBy(c => c.Name).ThenBy(c => c.Id)
            };

            var total = ordered.Count();
            var items = ordered.Skip((page - 1) * pageSize).Take(pageSize)
                .Select(c => new Crm.Application.Companies.Queries.CompanyListItem(c.Id, c.Name, c.Industry))
                .ToList();

            return Task.FromResult(new PagedResult<Crm.Application.Companies.Queries.CompanyListItem>(items, total, page, pageSize));
        }

        public Task<string[]> GetDistinctIndustriesAsync(string? search = null, CancellationToken ct = default)
        {
            IEnumerable<Company> result = _store.Values;
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                result = result.Where(c => c.Name.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                                           (c.Industry?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
                                           (c.Address?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
                                           c.Tags.Any(t => t.Contains(s, StringComparison.OrdinalIgnoreCase)));
            }

            var list = result.Where(c => !string.IsNullOrWhiteSpace(c.Industry))
                .Select(c => c.Industry!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return Task.FromResult(list);
        }

        public Task<Company> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(_store[id]);

        public Task<Company> UpsertAsync(Company company, CancellationToken ct = default)
        {
            if (company.Id == Guid.Empty)
            {
                company.Id = Guid.NewGuid();
            }

            _store[company.Id] = company;
            return Task.FromResult(company);
        }

        public Task<int> BulkAddTagAsync(IEnumerable<Guid> ids, string tag, CancellationToken ct = default)
        {
            var count = 0;
            foreach (var id in ids)
            {
                if (_store.TryGetValue(id, out var c))
                {
                    if (!c.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                    {
                        c.Tags.Add(tag);
                    }

                    count++;
                }
            }

            return Task.FromResult(count);
        }

        public async Task<int> ImportCsvAsync(Stream csvStream, CancellationToken ct = default)
        {
            using var reader = new StreamReader(csvStream);
            _ = await reader.ReadLineAsync();
            var rows = 0;
            while (true)
            {
                var line = await reader.ReadLineAsync();
                if (line is null)
                {
                    break;
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var cols = line.Split(',');
                var name = cols.ElementAtOrDefault(0)?.Trim() ?? string.Empty;
                var industry = cols.ElementAtOrDefault(1)?.Trim();
                var address = cols.ElementAtOrDefault(2)?.Trim();
                var tags = (cols.ElementAtOrDefault(3)?.Split('|', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>()).Select(t => t.Trim()).ToList();
                await UpsertAsync(new Company { Name = name, Industry = industry, Address = address, Tags = tags });
                rows++;
            }

            return rows;
        }

        public Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var ok = _store.TryRemove(id, out _);
            return Task.FromResult(ok);
        }
    }
}
