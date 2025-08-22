namespace Crm.Infrastructure.Services
{
    using System.Collections.Concurrent;

    using Crm.Application.Services;
    using Crm.Domain.Entities;

    public class InMemoryCompanyService : ICompanyService
    {
        private readonly ConcurrentDictionary<Guid, Company> _store = new();

        public Task<IEnumerable<Company>> GetAllAsync(string? search = null, CancellationToken ct = default)
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

            return Task.FromResult<IEnumerable<Company>>(result.OrderBy(c => c.Name).ToArray());
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
            var header = await reader.ReadLineAsync();
            var rows = 0;

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
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
    }
}
