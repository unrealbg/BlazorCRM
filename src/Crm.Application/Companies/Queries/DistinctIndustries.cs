namespace Crm.Application.Companies.Queries
{
    using MediatR;
    using Crm.Application.Services;

    public sealed record DistinctIndustries(string? Search) : IRequest<string[]>;

    public sealed class DistinctIndustriesHandler : IRequestHandler<DistinctIndustries, string[]>
    {
        private readonly ICompanyService _companies;
        public DistinctIndustriesHandler(ICompanyService companies) => _companies = companies;

        public async Task<string[]> Handle(DistinctIndustries r, CancellationToken ct)
        {
            var all = await _companies.GetAllAsync(r.Search, ct);
            return all.Where(c => !string.IsNullOrWhiteSpace(c.Industry))
                      .Select(c => c.Industry!)
                      .Distinct(StringComparer.OrdinalIgnoreCase)
                      .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                      .ToArray();
        }
    }
}
