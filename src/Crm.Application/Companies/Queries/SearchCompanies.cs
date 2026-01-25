namespace Crm.Application.Companies.Queries
{
    using Crm.Contracts.Paging;
    using MediatR;
    using Crm.Application.Services;

    public sealed record CompanyListItem(Guid Id, string Name, string? Industry);

    public sealed record SearchCompanies(PagedRequest Request, string? Industry)
        : IRequest<PagedResult<CompanyListItem>>;

    public sealed class SearchCompaniesHandler : IRequestHandler<SearchCompanies, PagedResult<CompanyListItem>>
    {
        private readonly ICompanyService _companies;
        public SearchCompaniesHandler(ICompanyService companies) => _companies = companies;

        public async Task<PagedResult<CompanyListItem>> Handle(SearchCompanies r, CancellationToken ct)
            => await _companies.SearchAsync(r.Request, r.Industry, ct);
    }
}
