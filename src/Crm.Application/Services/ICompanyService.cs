namespace Crm.Application.Services
{
    using Crm.Domain.Entities;

    public interface ICompanyService
    {
        Task<Crm.Application.Common.Models.PagedResult<Crm.Application.Companies.Queries.CompanyListItem>> SearchAsync(
            string? search,
            string? industry,
            string sort,
            bool asc,
            int page,
            int pageSize,
            CancellationToken ct = default);

        Task<string[]> GetDistinctIndustriesAsync(string? search = null, CancellationToken ct = default);

        Task<Company> GetByIdAsync(Guid id, CancellationToken ct = default);

        Task<Company> UpsertAsync(Company company, CancellationToken ct = default);

        Task<int> BulkAddTagAsync(IEnumerable<Guid> ids, string tag, CancellationToken ct = default);

        Task<int> ImportCsvAsync(Stream csvStream, CancellationToken ct = default);

        Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    }
}
