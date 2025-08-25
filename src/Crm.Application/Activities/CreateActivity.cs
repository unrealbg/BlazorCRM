namespace Crm.Application.Activities
{
    using FluentValidation;
    using MediatR;
    using Crm.Application.Services;
    using Crm.Domain.Entities;
    using Crm.Domain.Enums;
    using Crm.Application.Common.Behaviors;
    using Crm.Application.Security;

    [RequiresPermission(Permissions.Activities_Write)]
    public sealed record CreateActivity(ActivityType Type, Guid? RelatedId, DateTime? DueAt, ActivityStatus Status, string? Notes) : IRequest<Guid>;

    public sealed class CreateActivityValidator : AbstractValidator<CreateActivity>
    {
        public CreateActivityValidator()
        {
            RuleFor(x => x.Type).IsInEnum();
        }
    }

    public sealed class CreateActivityHandler : IRequestHandler<CreateActivity, Guid>
    {
        private readonly IActivityService _svc;
        public CreateActivityHandler(IActivityService svc) => _svc = svc;

        public async Task<Guid> Handle(CreateActivity r, CancellationToken ct)
        {
            var a = new Activity { Id = Guid.Empty, Type = r.Type, RelatedId = r.RelatedId, DueAt = r.DueAt, Status = r.Status, Notes = r.Notes };
            var saved = await _svc.UpsertAsync(a, ct);
            return saved.Id;
        }
    }
}
