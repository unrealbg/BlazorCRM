namespace Crm.Application.Pipelines
{
    using FluentValidation;
    using MediatR;
    using Crm.Application.Services;

    public sealed record UpdatePipeline(Guid Id, string Name) : IRequest<bool>;

    public sealed class UpdatePipelineValidator : AbstractValidator<UpdatePipeline>
    {
        public UpdatePipelineValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        }
    }

    public sealed class UpdatePipelineHandler : IRequestHandler<UpdatePipeline, bool>
    {
        private readonly IPipelineService _svc;
        public UpdatePipelineHandler(IPipelineService svc) => _svc = svc;

        public async Task<bool> Handle(UpdatePipeline r, CancellationToken ct)
        {
            await _svc.UpsertPipelineAsync(new Crm.Domain.Entities.Pipeline { Id = r.Id, Name = r.Name }, ct);
            return true;
        }
    }
}
