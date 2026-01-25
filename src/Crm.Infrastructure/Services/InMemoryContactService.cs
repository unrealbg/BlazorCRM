namespace Crm.Infrastructure.Services
{
    using System.Collections.Concurrent;
    using Crm.Application.Services;
    using Crm.Contracts.Paging;
    using Crm.Domain.Entities;

    public class InMemoryContactService : IContactService
    {
        private readonly ConcurrentDictionary<Guid, Contact> _store = new();

        public Task<PagedResult<Contact>> SearchAsync(
            PagedRequest request,
            CancellationToken ct = default)
        {
            IEnumerable<Contact> result = _store.Values;
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var s = request.Search.Trim();
                result = result.Where(c =>
                    c.FirstName.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                    c.LastName.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                    (c.Email?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (c.Phone?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    c.Tags.Any(t => t.Contains(s, StringComparison.OrdinalIgnoreCase))
                );
            }

            var page = request.Page <= 0 ? 1 : request.Page;
            var pageSize = request.PageSize <= 0 ? 50 : Math.Min(request.PageSize, 200);
            var sort = string.IsNullOrWhiteSpace(request.SortBy) ? nameof(Contact.LastName) : request.SortBy;
            var desc = string.Equals(request.SortDir, "desc", StringComparison.OrdinalIgnoreCase);

            var ordered = (sort, desc) switch
            {
                (nameof(Contact.LastName), false) => result.OrderBy(c => c.LastName).ThenBy(c => c.FirstName).ThenBy(c => c.Id),
                (nameof(Contact.LastName), true) => result.OrderByDescending(c => c.LastName).ThenByDescending(c => c.FirstName).ThenByDescending(c => c.Id),
                (nameof(Contact.FirstName), false) => result.OrderBy(c => c.FirstName).ThenBy(c => c.LastName).ThenBy(c => c.Id),
                (nameof(Contact.FirstName), true) => result.OrderByDescending(c => c.FirstName).ThenByDescending(c => c.LastName).ThenByDescending(c => c.Id),
                (nameof(Contact.Email), false) => result.OrderBy(c => c.Email).ThenBy(c => c.Id),
                (nameof(Contact.Email), true) => result.OrderByDescending(c => c.Email).ThenByDescending(c => c.Id),
                (nameof(Contact.CreatedAtUtc), false) => result.OrderBy(c => c.CreatedAtUtc).ThenBy(c => c.Id),
                (nameof(Contact.CreatedAtUtc), true) => result.OrderByDescending(c => c.CreatedAtUtc).ThenByDescending(c => c.Id),
                _ => result.OrderBy(c => c.LastName).ThenBy(c => c.FirstName).ThenBy(c => c.Id)
            };
            var total = ordered.Count();
            var items = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToArray();

            return Task.FromResult(new PagedResult<Contact>(items, total, page, pageSize));
        }

        public Task<Contact> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(_store[id]);

        public Task<Contact> UpsertAsync(Contact contact, CancellationToken ct = default)
        {
            if (contact.Id == Guid.Empty)
            {
                contact.Id = Guid.NewGuid();
            }

            _store[contact.Id] = contact;
            return Task.FromResult(contact);
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
                var first = cols.ElementAtOrDefault(0)?.Trim() ?? string.Empty;
                var last = cols.ElementAtOrDefault(1)?.Trim() ?? string.Empty;
                var email = cols.ElementAtOrDefault(2)?.Trim();
                var phone = cols.ElementAtOrDefault(3)?.Trim();
                var position = cols.ElementAtOrDefault(4)?.Trim();
                var tags = (cols.ElementAtOrDefault(5)?.Split('|', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>()).Select(t => t.Trim()).ToList();
                await UpsertAsync(new Contact { FirstName = first, LastName = last, Email = email, Phone = phone, Position = position, Tags = tags });
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
