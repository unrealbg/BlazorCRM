namespace Crm.Infrastructure.Services
{
    using System.Collections.Concurrent;
    using Crm.Application.Services;
    using Crm.Domain.Entities;

    public class InMemoryContactService : IContactService
    {
        private readonly ConcurrentDictionary<Guid, Contact> _store = new();

        public Task<IEnumerable<Contact>> GetAllAsync(string? search = null, CancellationToken ct = default)
        {
            IEnumerable<Contact> result = _store.Values;
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                result = result.Where(c =>
                    c.FirstName.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                    c.LastName.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                    (c.Email?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (c.Phone?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    c.Tags.Any(t => t.Contains(s, StringComparison.OrdinalIgnoreCase))
                );
            }

            return Task.FromResult<IEnumerable<Contact>>(result.OrderBy(c => c.LastName).ThenBy(c => c.FirstName).ToArray());
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
