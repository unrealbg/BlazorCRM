namespace Crm.Application.Companies
{
    using FluentValidation;
    using MediatR;
    using Crm.Application.Services;
    using Crm.Domain.Entities;

    public sealed record CreateCompany(string Name, string? Industry, string[]? Tags) : IRequest<Guid>;

    public sealed class CreateCompanyValidator : AbstractValidator<CreateCompany>
    {
        public CreateCompanyValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        }
    }

    public sealed class CreateCompanyHandler : IRequestHandler<CreateCompany, Guid>
    {
        private readonly ICompanyService _svc;
        public CreateCompanyHandler(ICompanyService svc) => _svc = svc;

        public async Task<Guid> Handle(CreateCompany r, CancellationToken ct)
        {
            var company = new Company { Id = Guid.Empty, Name = r.Name, Industry = r.Industry, Tags = (r.Tags ?? Array.Empty<string>()).ToList() };
            var saved = await _svc.UpsertAsync(company, ct);
            return saved.Id;
        }
    }
}
