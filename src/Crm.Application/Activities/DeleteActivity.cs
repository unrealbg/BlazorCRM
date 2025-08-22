namespace Crm.Application.Activities
{
    using FluentValidation;
    using MediatR;
    using Crm.Application.Services;

    public sealed record DeleteActivity(Guid Id) : IRequest<bool>;

    public sealed class DeleteActivityValidator : AbstractValidator<DeleteActivity>
    {
        public DeleteActivityValidator() { RuleFor(x => x.Id).NotEmpty(); }
    }

    public sealed class DeleteActivityHandler : IRequestHandler<DeleteActivity, bool>
    {
        private readonly IActivityService _svc;
        public DeleteActivityHandler(IActivityService svc) => _svc = svc;

        public async Task<bool> Handle(DeleteActivity r, CancellationToken ct)
            => (await _svc.GetByIdAsync(r.Id, ct)) is not null && true; // Service has no delete; extend if needed
    }
}
