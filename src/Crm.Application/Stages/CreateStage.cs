namespace Crm.Application.Stages
{
    using FluentValidation;
    using MediatR;
    using Crm.Application.Services;
    using Crm.Domain.Entities;

    public sealed record CreateStage(string Name, int Order, Guid PipelineId) : IRequest<Guid>;

    public sealed class CreateStageValidator : AbstractValidator<CreateStage>
    {
        public CreateStageValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.PipelineId).NotEmpty();
        }
    }

    public sealed class CreateStageHandler : IRequestHandler<CreateStage, Guid>
    {
        private readonly IPipelineService _svc;
        public CreateStageHandler(IPipelineService svc) => _svc = svc;

        public async Task<Guid> Handle(CreateStage r, CancellationToken ct)
        {
            var saved = await _svc.UpsertStageAsync(new Stage { Name = r.Name, Order = r.Order, PipelineId = r.PipelineId }, ct);
            return saved.Id;
        }
    }
}
