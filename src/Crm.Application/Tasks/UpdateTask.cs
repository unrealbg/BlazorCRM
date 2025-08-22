namespace Crm.Application.Tasks
{
    using FluentValidation;
    using MediatR;
    using Crm.Application.Services;
    using Crm.Domain.Enums;

    public sealed record UpdateTask(Guid Id, string Title, DateTime? DueAt, Guid? OwnerId, RelatedToType RelatedTo, Guid? RelatedId, TaskPriority Priority, TaskStatus Status) : IRequest<bool>;

    public sealed class UpdateTaskValidator : AbstractValidator<UpdateTask>
    {
        public UpdateTaskValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Title).NotEmpty();
        }
    }

    public sealed class UpdateTaskHandler : IRequestHandler<UpdateTask, bool>
    {
        private readonly ITaskService _svc;
        public UpdateTaskHandler(ITaskService svc) => _svc = svc;

        public async Task<bool> Handle(UpdateTask r, CancellationToken ct)
        {
            var current = await _svc.GetByIdAsync(r.Id, ct);
            current.Title = r.Title;
            current.DueAt = r.DueAt;
            current.OwnerId = r.OwnerId;
            current.RelatedTo = r.RelatedTo;
            current.RelatedId = r.RelatedId;
            current.Priority = r.Priority;
            current.Status = r.Status;
            await _svc.UpsertAsync(current, ct);
            return true;
        }
    }
}
