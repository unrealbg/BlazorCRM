namespace Crm.Application.Tasks
{
    using FluentValidation;
    using MediatR;
    using Crm.Application.Services;
    using Crm.Domain.Entities;
    using Crm.Domain.Enums;

    public sealed record CreateTask(string Title, DateTime? DueAt, Guid? OwnerId, RelatedToType RelatedTo, Guid? RelatedId, TaskPriority Priority, TaskStatus Status) : IRequest<Guid>;

    public sealed class CreateTaskValidator : AbstractValidator<CreateTask>
    {
        public CreateTaskValidator()
        {
            RuleFor(x => x.Title).NotEmpty();
        }
    }

    public sealed class CreateTaskHandler : IRequestHandler<CreateTask, Guid>
    {
        private readonly ITaskService _svc;
        public CreateTaskHandler(ITaskService svc) => _svc = svc;

        public async Task<Guid> Handle(CreateTask r, CancellationToken ct)
        {
            var t = new TaskItem
                        {
                            Title = r.Title,
                            DueAt = r.DueAt,
                            OwnerId = r.OwnerId,
                            RelatedTo = r.RelatedTo,
                            RelatedId = r.RelatedId,
                            Priority = r.Priority,
                            Status = r.Status
                        };
            var saved = await _svc.UpsertAsync(t, ct);
            return saved.Id;
        }
    }
}
