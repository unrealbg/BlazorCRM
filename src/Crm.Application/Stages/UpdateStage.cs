namespace Crm.Application.Stages
{
    using FluentValidation;
    using MediatR;
    using Crm.Application.Services;

    public sealed record UpdateStage(Guid Id, string Name, int Order) : IRequest<bool>;

    public sealed class UpdateStageValidator : AbstractValidator<UpdateStage>
    {
        public UpdateStageValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        }
    }

    public sealed class UpdateStageHandler : IRequestHandler<UpdateStage, bool>
    {
        private readonly IPipelineService _svc;
        public UpdateStageHandler(IPipelineService svc) => _svc = svc;

        public async Task<bool> Handle(UpdateStage r, CancellationToken ct)
        {
            await _svc.UpsertStageAsync(new Crm.Domain.Entities.Stage { Id = r.Id, Name = r.Name, Order = r.Order }, ct);
            return true;
        }
    }
}
