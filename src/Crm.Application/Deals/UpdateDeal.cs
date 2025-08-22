namespace Crm.Application.Deals
{
    using FluentValidation;
    using MediatR;
    using Crm.Application.Services;
    using Crm.Application.Common.Behaviors;
    using Crm.Application.Security;

    [RequiresPermission(Permissions.Deals_Move)]
    public sealed record UpdateDeal(Guid Id, string Title, decimal Amount, string Currency, int Probability, Guid StageId, Guid? OwnerId, Guid? CompanyId, Guid? ContactId, DateTime? CloseDate) : IRequest<bool>;

    public sealed class UpdateDealValidator : AbstractValidator<UpdateDeal>
    {
        public UpdateDealValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Currency).NotEmpty().MaximumLength(10);
            RuleFor(x => x.Probability).InclusiveBetween(0, 100);
        }
    }

    public sealed class UpdateDealHandler : IRequestHandler<UpdateDeal, bool>
    {
        private readonly IDealService _svc;
        public UpdateDealHandler(IDealService svc) => _svc = svc;

        public async Task<bool> Handle(UpdateDeal r, CancellationToken ct)
        {
            var current = await _svc.GetByIdAsync(r.Id, ct);
            current.Title = r.Title;
            current.Amount = r.Amount;
            current.Currency = r.Currency;
            current.Probability = r.Probability;
            current.StageId = r.StageId;
            current.OwnerId = r.OwnerId;
            current.CompanyId = r.CompanyId;
            current.ContactId = r.ContactId;
            current.CloseDate = r.CloseDate;
            await _svc.UpsertAsync(current, ct);
            return true;
        }
    }
}
