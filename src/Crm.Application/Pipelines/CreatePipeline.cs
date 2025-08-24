namespace Crm.Application.Pipelines
{
    using FluentValidation;
    using MediatR;
    using Crm.Application.Services;
    using Crm.Domain.Entities;

    public sealed record CreatePipeline(string Name) : IRequest<Guid>;

    public sealed class CreatePipelineValidator : AbstractValidator<CreatePipeline>
    {
        public CreatePipelineValidator() => RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }

    public sealed class CreatePipelineHandler : IRequestHandler<CreatePipeline, Guid>
    {
        private readonly IPipelineService _svc;
        public CreatePipelineHandler(IPipelineService svc) => _svc = svc;

        public async Task<Guid> Handle(CreatePipeline r, CancellationToken ct)
        {
            var saved = await _svc.UpsertPipelineAsync(new Pipeline { Name = r.Name }, ct);
            return saved.Id;
        }
    }
}
