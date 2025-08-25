namespace Crm.Application.Deals
{
    using FluentValidation;
    using MediatR;
    using Crm.Application.Services;
    using Crm.Application.Common.Behaviors;
    using Crm.Application.Security;

    [RequiresPermission(Permissions.Deals_Write)]
    public sealed record DeleteDeal(Guid Id) : IRequest<bool>;

    public sealed class DeleteDealValidator : AbstractValidator<DeleteDeal>
    {
        public DeleteDealValidator() { RuleFor(x => x.Id).NotEmpty(); }
    }

    public sealed class DeleteDealHandler : IRequestHandler<DeleteDeal, bool>
    {
        private readonly IDealService _svc;
        public DeleteDealHandler(IDealService svc) => _svc = svc;

        public Task<bool> Handle(DeleteDeal r, CancellationToken ct)
            => _svc.DeleteAsync(r.Id, ct);
    }
}
