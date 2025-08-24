namespace Crm.Application.Stages
{
    using FluentValidation;
    using MediatR;
    using Crm.Application.Services;

    public sealed record DeleteStage(Guid Id) : IRequest<bool>;

    public sealed class DeleteStageValidator : AbstractValidator<DeleteStage>
    {
        public DeleteStageValidator() => RuleFor(x => x.Id).NotEmpty();
    }

    public sealed class DeleteStageHandler : IRequestHandler<DeleteStage, bool>
    {
        private readonly IPipelineService _svc;
        public DeleteStageHandler(IPipelineService svc) => _svc = svc;

        public Task<bool> Handle(DeleteStage r, CancellationToken ct) => _svc.DeleteStageAsync(r.Id, ct);
    }
}
