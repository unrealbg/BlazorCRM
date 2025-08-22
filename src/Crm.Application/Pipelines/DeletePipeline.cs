namespace Crm.Application.Pipelines
{
    using FluentValidation;
    using MediatR;
    using Crm.Application.Services;

    public sealed record DeletePipeline(Guid Id) : IRequest<bool>;

    public sealed class DeletePipelineValidator : AbstractValidator<DeletePipeline>
    {
        public DeletePipelineValidator() => RuleFor(x => x.Id).NotEmpty();
    }

    public sealed class DeletePipelineHandler : IRequestHandler<DeletePipeline, bool>
    {
        private readonly IPipelineService _svc;
        public DeletePipelineHandler(IPipelineService svc) => _svc = svc;

        public Task<bool> Handle(DeletePipeline r, CancellationToken ct) => _svc.DeletePipelineAsync(r.Id, ct);
    }
}
