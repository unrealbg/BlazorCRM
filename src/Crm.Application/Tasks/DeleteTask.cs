namespace Crm.Application.Tasks
{
    using FluentValidation;
    using MediatR;
    using Crm.Application.Services;

    public sealed record DeleteTask(Guid Id) : IRequest<bool>;

    public sealed class DeleteTaskValidator : AbstractValidator<DeleteTask>
    {
        public DeleteTaskValidator() { RuleFor(x => x.Id).NotEmpty(); }
    }

    public sealed class DeleteTaskHandler : IRequestHandler<DeleteTask, bool>
    {
        private readonly ITaskService _svc;
        public DeleteTaskHandler(ITaskService svc) => _svc = svc;

        public Task<bool> Handle(DeleteTask r, CancellationToken ct)
            => _svc.DeleteAsync(r.Id, ct);
    }
}
