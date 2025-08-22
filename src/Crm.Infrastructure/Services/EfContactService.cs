namespace Crm.Infrastructure.Services
{
    using System.Text;
    using Crm.Application.Services;
    using Crm.Domain.Entities;
    using Crm.Infrastructure.Persistence;
    using Microsoft.EntityFrameworkCore;

    public class EfContactService : IContactService
    {
        private readonly CrmDbContext _db;

        public EfContactService(CrmDbContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<Contact>> GetAllAsync(string? search = null, CancellationToken ct = default)
        {
            IQueryable<Contact> q = _db.Contacts.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                q = q.Where(c =>
                    EF.Functions.Like(c.FirstName, $"%{s}%") ||
                    EF.Functions.Like(c.LastName, $"%{s}%") ||
                    (c.Email != null && EF.Functions.Like(c.Email, $"%{s}%")) ||
                    (c.Phone != null && EF.Functions.Like(c.Phone, $"%{s}%"))
                );
            }
            return await q.OrderBy(c => c.LastName).ThenBy(c => c.FirstName).ToListAsync(ct);
        }

        public async Task<Contact> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await _db.Contacts.FindAsync(new object?[] { id }, ct) ?? throw new KeyNotFoundException("Contact not found");
            return entity;
        }

        public async Task<Contact> UpsertAsync(Contact contact, CancellationToken ct = default)
        {
            if (contact.Id == Guid.Empty)
            {
                contact.Id = Guid.NewGuid();
                await _db.Contacts.AddAsync(contact, ct);
            }
            else
            {
                var existing = await _db.Contacts.AsTracking().FirstOrDefaultAsync(c => c.Id == contact.Id, ct);
                if (existing is null)
                {
                    await _db.Contacts.AddAsync(contact, ct);
                }
                else
                {
                    _db.Entry(existing).CurrentValues.SetValues(contact);
                    existing.Tags = contact.Tags;
                }
            }
            await _db.SaveChangesAsync(ct);
            return contact;
        }

        public async Task<int> BulkAddTagAsync(IEnumerable<Guid> ids, string tag, CancellationToken ct = default)
        {
            var idList = ids.Distinct().ToList();
            var contacts = await _db.Contacts.Where(c => idList.Contains(c.Id)).ToListAsync(ct);
            foreach (var c in contacts)
            {
                if (!c.Tags.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)))
                    c.Tags.Add(tag);
            }
            await _db.SaveChangesAsync(ct);
            return contacts.Count;
        }

        public async Task<int> ImportCsvAsync(Stream csvStream, CancellationToken ct = default)
        {
            using var reader = new StreamReader(csvStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
            // optional header
            _ = await reader.ReadLineAsync();
            var toAdd = new List<Contact>();
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;
                var cols = line.Split(',');
                var first = cols.ElementAtOrDefault(0)?.Trim() ?? string.Empty;
                var last = cols.ElementAtOrDefault(1)?.Trim() ?? string.Empty;
                var email = cols.ElementAtOrDefault(2)?.Trim();
                var phone = cols.ElementAtOrDefault(3)?.Trim();
                var position = cols.ElementAtOrDefault(4)?.Trim();
                var tags = (cols.ElementAtOrDefault(5)?.Split('|', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>()).Select(t => t.Trim()).ToList();
                toAdd.Add(new Contact { Id = Guid.NewGuid(), FirstName = first, LastName = last, Email = email, Phone = phone, Position = position, Tags = tags });
            }

            if (toAdd.Count > 0)
            {
                await _db.Contacts.AddRangeAsync(toAdd, ct);
                await _db.SaveChangesAsync(ct);
            }

            return toAdd.Count;
        }
    }
}
