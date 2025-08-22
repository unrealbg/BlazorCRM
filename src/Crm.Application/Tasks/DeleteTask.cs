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

        public async Task<bool> Handle(DeleteTask r, CancellationToken ct)
        {
            var existing = await _svc.GetByIdAsync(r.Id, ct);
            return existing is not null;
        }
    }
}
