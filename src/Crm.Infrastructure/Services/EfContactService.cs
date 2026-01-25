namespace Crm.Infrastructure.Services
{
    using System.Text;
    using Crm.Application.Services;
    using Crm.Contracts.Paging;
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

        public async Task<PagedResult<Contact>> SearchAsync(
            PagedRequest request,
            CancellationToken ct = default)
        {
            IQueryable<Contact> q = _db.Contacts.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var s = request.Search.Trim();
                q = q.Where(c =>
                    EF.Functions.Like(c.FirstName, $"%{s}%") ||
                    EF.Functions.Like(c.LastName, $"%{s}%") ||
                    (c.Email != null && EF.Functions.Like(c.Email, $"%{s}%")) ||
                    (c.Phone != null && EF.Functions.Like(c.Phone, $"%{s}%"))
                );
            }

            var page = request.Page <= 0 ? 1 : request.Page;
            var pageSize = request.PageSize <= 0 ? 50 : Math.Min(request.PageSize, 200);
            var sort = string.IsNullOrWhiteSpace(request.SortBy) ? nameof(Contact.LastName) : request.SortBy;
            var desc = string.Equals(request.SortDir, "desc", StringComparison.OrdinalIgnoreCase);

            var ordered = (sort, desc) switch
            {
                (nameof(Contact.LastName), false) => q.OrderBy(c => c.LastName).ThenBy(c => c.FirstName).ThenBy(c => c.Id),
                (nameof(Contact.LastName), true) => q.OrderByDescending(c => c.LastName).ThenByDescending(c => c.FirstName).ThenByDescending(c => c.Id),
                (nameof(Contact.FirstName), false) => q.OrderBy(c => c.FirstName).ThenBy(c => c.LastName).ThenBy(c => c.Id),
                (nameof(Contact.FirstName), true) => q.OrderByDescending(c => c.FirstName).ThenByDescending(c => c.LastName).ThenByDescending(c => c.Id),
                (nameof(Contact.Email), false) => q.OrderBy(c => c.Email).ThenBy(c => c.Id),
                (nameof(Contact.Email), true) => q.OrderByDescending(c => c.Email).ThenByDescending(c => c.Id),
                (nameof(Contact.CreatedAtUtc), false) => q.OrderBy(c => c.CreatedAtUtc).ThenBy(c => c.Id),
                (nameof(Contact.CreatedAtUtc), true) => q.OrderByDescending(c => c.CreatedAtUtc).ThenByDescending(c => c.Id),
                _ => q.OrderBy(c => c.LastName).ThenBy(c => c.FirstName).ThenBy(c => c.Id)
            };
            var total = await ordered.CountAsync(ct);
            var items = await ordered.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
            return new PagedResult<Contact>(items, total, page, pageSize);
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
            _ = await reader.ReadLineAsync();
            var toAdd = new List<Contact>();
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
                toAdd.Add(new Contact { Id = Guid.NewGuid(), FirstName = first, LastName = last, Email = email, Phone = phone, Position = position, Tags = tags });
            }

            if (toAdd.Count > 0)
            {
                await _db.Contacts.AddRangeAsync(toAdd, ct);
                await _db.SaveChangesAsync(ct);
            }

            return toAdd.Count;
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await _db.Contacts.FindAsync(new object?[] { id }, ct);
            if (entity is null)
            {
                return false;
            }

            _db.Contacts.Remove(entity);
            await _db.SaveChangesAsync(ct);

            return true;
        }
    }
}
