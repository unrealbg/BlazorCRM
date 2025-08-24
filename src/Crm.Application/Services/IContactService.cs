namespace Crm.Application.Services
{
    using Crm.Domain.Entities;

    public interface IContactService
    {
        Task<IEnumerable<Contact>> GetAllAsync(string? search = null, CancellationToken ct = default);

        Task<Contact> GetByIdAsync(Guid id, CancellationToken ct = default);

        Task<Contact> UpsertAsync(Contact contact, CancellationToken ct = default);

        Task<int> BulkAddTagAsync(IEnumerable<Guid> ids, string tag, CancellationToken ct = default);

        Task<int> ImportCsvAsync(Stream csvStream, CancellationToken ct = default);

        Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    }
}
