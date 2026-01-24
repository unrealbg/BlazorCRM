namespace Crm.Application.Companies.Queries
{
    using MediatR;
    using Crm.Application.Common.Models;
    using Crm.Application.Services;

    public sealed record CompanyListItem(Guid Id, string Name, string? Industry);

    public sealed record SearchCompanies(string? Search, string? Industry, string Sort, bool Asc, int Page, int PageSize)
        : IRequest<PagedResult<CompanyListItem>>;

    public sealed class SearchCompaniesHandler : IRequestHandler<SearchCompanies, PagedResult<CompanyListItem>>
    {
        private readonly ICompanyService _companies;
        public SearchCompaniesHandler(ICompanyService companies) => _companies = companies;

        public async Task<PagedResult<CompanyListItem>> Handle(SearchCompanies r, CancellationToken ct)
        {
            var page = r.Page <= 0 ? 1 : r.Page;
            var pageSize = r.PageSize is <= 0 or > 200 ? 10 : r.PageSize;
            var sort = string.IsNullOrWhiteSpace(r.Sort) ? nameof(Domain.Entities.Company.Name) : r.Sort;
            var asc = r.Asc || string.IsNullOrWhiteSpace(r.Sort);

            return await _companies.SearchAsync(r.Search, r.Industry, sort, asc, page, pageSize, ct);
        }
    }
}
