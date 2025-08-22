namespace Crm.Application.Companies
{
    using FluentValidation;
    using MediatR;
    using Crm.Application.Services;
    using Crm.Domain.Entities;

    public sealed record UpdateCompany(Guid Id, string Name, string? Industry, string[]? Tags) : IRequest<bool>;

    public sealed class UpdateCompanyValidator : AbstractValidator<UpdateCompany>
    {
        public UpdateCompanyValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        }
    }

    public sealed class UpdateCompanyHandler : IRequestHandler<UpdateCompany, bool>
    {
        private readonly ICompanyService _svc;
        public UpdateCompanyHandler(ICompanyService svc) => _svc = svc;

        public async Task<bool> Handle(UpdateCompany r, CancellationToken ct)
        {
            var current = await _svc.GetByIdAsync(r.Id, ct);
            current.Name = r.Name;
            current.Industry = r.Industry;
            current.Tags = (r.Tags ?? Array.Empty<string>()).ToList();
            await _svc.UpsertAsync(current, ct);
            return true;
        }
    }
}
