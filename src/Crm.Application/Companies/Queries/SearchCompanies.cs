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

            var all = await _companies.GetAllAsync(r.Search, ct);

            if (!string.IsNullOrWhiteSpace(r.Industry))
                all = all.Where(c => string.Equals(c.Industry, r.Industry, StringComparison.OrdinalIgnoreCase));

            var asc = r.Asc || string.IsNullOrWhiteSpace(r.Sort);
            var q = (r.Sort, asc) switch
            {
                (nameof(Domain.Entities.Company.Name), true) => all.OrderBy(c => c.Name),
                (nameof(Domain.Entities.Company.Name), false) => all.OrderByDescending(c => c.Name),
                (nameof(Domain.Entities.Company.Industry), true) => all.OrderBy(c => c.Industry),
                (nameof(Domain.Entities.Company.Industry), false) => all.OrderByDescending(c => c.Industry),
                _ => all.OrderBy(c => c.Name)
            };

            var total = q.Count();
            var items = q.Skip((page - 1) * pageSize).Take(pageSize)
                .Select(c => new CompanyListItem(c.Id, c.Name, c.Industry))
                .ToList();

            return new PagedResult<CompanyListItem>(items, total);
        }
    }
}
