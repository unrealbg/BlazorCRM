namespace Crm.Application.Activities
{
    using FluentValidation;
    using MediatR;
    using Crm.Application.Services;
    using Crm.Domain.Enums;

    public sealed record UpdateActivity(Guid Id, ActivityType Type, Guid? RelatedId, DateTime? DueAt, ActivityStatus Status, string? Notes) : IRequest<bool>;

    public sealed class UpdateActivityValidator : AbstractValidator<UpdateActivity>
    {
        public UpdateActivityValidator() { RuleFor(x => x.Id).NotEmpty(); }
    }

    public sealed class UpdateActivityHandler : IRequestHandler<UpdateActivity, bool>
    {
        private readonly IActivityService _svc;
        public UpdateActivityHandler(IActivityService svc) => _svc = svc;

        public async Task<bool> Handle(UpdateActivity r, CancellationToken ct)
        {
            var current = await _svc.GetByIdAsync(r.Id, ct);
            current.Type = r.Type;
            current.RelatedId = r.RelatedId;
            current.DueAt = r.DueAt;
            current.Status = r.Status;
            current.Notes = r.Notes;
            await _svc.UpsertAsync(current, ct);
            return true;
        }
    }
}
