namespace Crm.Application.Deals
{
    using FluentValidation;
    using MediatR;
    using Crm.Application.Services;
    using Crm.Domain.Entities;
    using Crm.Application.Common.Behaviors;
    using Crm.Application.Security;

    [RequiresPermission(Permissions.Deals_Write)]
    public sealed record CreateDeal(string Title, decimal Amount, string Currency, int Probability, Guid StageId, Guid? OwnerId, Guid? CompanyId, Guid? ContactId, DateTime? CloseDate) : IRequest<Guid>;

    public sealed class CreateDealValidator : AbstractValidator<CreateDeal>
    {
        public CreateDealValidator()
        {
            RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Currency).NotEmpty().MaximumLength(10);
            RuleFor(x => x.Probability).InclusiveBetween(0, 100);
        }
    }

    public sealed class CreateDealHandler : IRequestHandler<CreateDeal, Guid>
    {
        private readonly IDealService _svc;
        public CreateDealHandler(IDealService svc) => _svc = svc;

        public async Task<Guid> Handle(CreateDeal r, CancellationToken ct)
        {
            var deal = new Deal { Id = Guid.Empty, Title = r.Title, Amount = r.Amount, Currency = r.Currency, Probability = r.Probability, StageId = r.StageId, OwnerId = r.OwnerId, CompanyId = r.CompanyId, ContactId = r.ContactId, CloseDate = r.CloseDate };
            var saved = await _svc.UpsertAsync(deal, ct);
            return saved.Id;
        }
    }
}
