namespace Crm.Application.Companies
{
    using FluentValidation;
    using MediatR;
    using Crm.Application.Services;

    public sealed record DeleteCompany(Guid Id) : IRequest<bool>;

    public sealed class DeleteCompanyValidator : AbstractValidator<DeleteCompany>
    {
        public DeleteCompanyValidator() { RuleFor(x => x.Id).NotEmpty(); }
    }

    public sealed class DeleteCompanyHandler : IRequestHandler<DeleteCompany, bool>
    {
        private readonly ICompanyService _svc;
        public DeleteCompanyHandler(ICompanyService svc) => _svc = svc;

        public Task<bool> Handle(DeleteCompany r, CancellationToken ct)
            => _svc.DeleteAsync(r.Id, ct);
    }
}
